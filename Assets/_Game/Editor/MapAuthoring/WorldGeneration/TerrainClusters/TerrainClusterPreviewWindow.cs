using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters.Authoring;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.WorldGeneration.TerrainClusters
{
    public enum TerrainClusterPreviewViewMode
    {
        PatternFree = 1,
        PatternA = 2,
        PatternB = 3,
        Compare = 4,
    }

    public sealed class TerrainClusterPreviewWindow : EditorWindow
    {
        public const string MenuPath = "Tools/MapDesign/TerrainCluster Preview";
        public const string WindowTitle = "TerrainCluster Preview";

        private readonly TerrainClusterPreviewModel model = new TerrainClusterPreviewModel();
        private readonly List<string> allClusterIds = new List<string>();
        private readonly List<string> clusterIds = new List<string>();
        private readonly List<string> variantIds = new List<string>();
        private readonly List<TerrainClusterPreviewSnapshot> compareSnapshots =
            new List<TerrainClusterPreviewSnapshot>();

        private TerrainClusterAuthoringCatalog catalog;
        private string catalogDigest = string.Empty;
        private string patternCatalogDigest = string.Empty;
        private StarNight.Map.WorldGeneration.MicroPatterns.MicroPatternAuthoringCatalog patternCatalog;
        private string selectedClusterId = string.Empty;
        private string selectedVariantId = string.Empty;
        private string selectedBiome = "All";
        private TerrainClusterPreviewViewMode viewMode = TerrainClusterPreviewViewMode.PatternFree;
        private Vector2 scroll;
        private bool showFootprint = true;
        private bool showRolesPorts = true;
        private bool showSpine = true;
        private bool showEnvelope = true;
        private bool showProtected = true;
        private bool showRoutes = true;
        private bool showPattern = true;
        private bool showDensity = true;
        private bool showSector = true;

        public IReadOnlyList<string> AllClusterIds => new ReadOnlyCollection<string>(allClusterIds.ToArray());
        public IReadOnlyList<string> ClusterIds => new ReadOnlyCollection<string>(clusterIds.ToArray());
        public IReadOnlyList<string> VariantIds => new ReadOnlyCollection<string>(variantIds.ToArray());
        public IReadOnlyList<TerrainClusterPreviewSnapshot> CompareSnapshots =>
            new ReadOnlyCollection<TerrainClusterPreviewSnapshot>(compareSnapshots.ToArray());
        public TerrainClusterPreviewSnapshot CurrentSnapshot { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public TerrainClusterPreviewViewMode ViewMode => viewMode;
        public int PanelCount => 5;

        [MenuItem(MenuPath)]
        public static TerrainClusterPreviewWindow Open()
        {
            var window = GetWindow<TerrainClusterPreviewWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(760f, 520f);
            window.Show();
            return window;
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = new Vector2(760f, 520f);
            if (catalog == null) Reload();
        }

        public bool Reload()
        {
            LastError = string.Empty;
            CurrentSnapshot = null;
            compareSnapshots.Clear();
            try
            {
                var clusterImport = model.LoadCatalog();
                var patternImport = model.LoadPatternCatalog();
                if (!clusterImport.Success || !patternImport.Success || !patternImport.Published)
                {
                    LastError = string.Join("\n", clusterImport.Errors.Select(value => value.ToString())
                        .Concat(patternImport.Errors.Select(value => value.ToString())));
                    return false;
                }
                catalog = clusterImport.Catalog;
                catalogDigest = clusterImport.StableDigest;
                patternCatalog = patternImport.Catalog;
                patternCatalogDigest = patternImport.StableDigest;
                allClusterIds.Clear();
                allClusterIds.AddRange(catalog.Entries.Select(value => value.Id.Value)
                    .OrderBy(value => value, StringComparer.Ordinal));
                if (!allClusterIds.Contains(selectedClusterId)) selectedClusterId = allClusterIds.FirstOrDefault() ?? string.Empty;
                ApplyBiomeFilter();
                BindVariants();
                return Rebuild();
            }
            catch (Exception exception)
            {
                LastError = exception.GetType().Name + ": " + exception.Message;
                return false;
            }
        }

        public bool TrySelectBiome(string biome)
        {
            if (catalog == null) return false;
            if (!string.Equals(biome, "All", StringComparison.Ordinal) &&
                !MoonpalaceBiomeId.TryParse(biome, out _)) return false;
            selectedBiome = biome;
            ApplyBiomeFilter();
            if (!clusterIds.Contains(selectedClusterId)) selectedClusterId = clusterIds.FirstOrDefault() ?? string.Empty;
            BindVariants();
            return Rebuild();
        }

        public bool TrySelectCluster(string clusterId)
        {
            if (!clusterIds.Contains(clusterId ?? string.Empty)) return false;
            selectedClusterId = clusterId;
            BindVariants();
            return Rebuild();
        }

        public bool TrySelectVariant(string variantId)
        {
            if (!variantIds.Contains(variantId ?? string.Empty)) return false;
            selectedVariantId = variantId;
            return Rebuild();
        }

        public bool TrySelectViewMode(TerrainClusterPreviewViewMode mode)
        {
            if (!Enum.IsDefined(typeof(TerrainClusterPreviewViewMode), mode)) return false;
            viewMode = mode;
            return Rebuild();
        }

        private void ApplyBiomeFilter()
        {
            clusterIds.Clear();
            var entries = catalog == null ? Array.Empty<TerrainClusterAuthoringEntry>() : catalog.Entries.ToArray();
            if (!string.Equals(selectedBiome, "All", StringComparison.Ordinal) &&
                MoonpalaceBiomeId.TryParse(selectedBiome, out var biome))
                entries = entries.Where(value => value.Biome == biome).ToArray();
            clusterIds.AddRange(entries.Select(value => value.Id.Value).OrderBy(value => value, StringComparer.Ordinal));
        }

        private void BindVariants()
        {
            variantIds.Clear();
            if (catalog == null || !catalog.TryGet(new StarNight.Map.WorldGeneration.TerrainClusters.TerrainClusterId(selectedClusterId), out var entry))
            {
                selectedVariantId = string.Empty;
                return;
            }
            variantIds.AddRange(entry.Contract.Traversal.Variants.Select(value => value.Id.Value)
                .OrderBy(value => value, StringComparer.Ordinal));
            if (!variantIds.Contains(selectedVariantId)) selectedVariantId = entry.BaselineVariantId.Value;
        }

        private bool Rebuild()
        {
            LastError = string.Empty;
            CurrentSnapshot = null;
            compareSnapshots.Clear();
            if (catalog == null || patternCatalog == null || selectedClusterId.Length == 0 || selectedVariantId.Length == 0)
            {
                LastError = "A published catalog, cluster, and variant are required.";
                return false;
            }
            if (viewMode == TerrainClusterPreviewViewMode.Compare)
            {
                foreach (var mode in new[]
                         {
                             TerrainClusterPreviewMode.PatternFree,
                             TerrainClusterPreviewMode.PatternA,
                             TerrainClusterPreviewMode.PatternB,
                         })
                {
                    var result = Build(mode);
                    if (!result.Success)
                    {
                        LastError = ErrorText(result);
                        compareSnapshots.Clear();
                        return false;
                    }
                    compareSnapshots.Add(result.Snapshot);
                }
                CurrentSnapshot = compareSnapshots[0];
                return true;
            }
            var single = Build((TerrainClusterPreviewMode)(int)viewMode);
            if (!single.Success)
            {
                LastError = ErrorText(single);
                return false;
            }
            CurrentSnapshot = single.Snapshot;
            return true;
        }

        private TerrainClusterPreviewBuildResult Build(TerrainClusterPreviewMode mode) =>
            model.Build(new TerrainClusterPreviewRequest(selectedClusterId, selectedVariantId, mode),
                catalog, catalogDigest, patternCatalog, patternCatalogDigest);

        private static string ErrorText(TerrainClusterPreviewBuildResult result) =>
            string.Join("\n", result.Errors.Select(value => value.ToString()));

        private void OnGUI()
        {
            DrawToolbar();
            if (LastError.Length != 0) EditorGUILayout.HelpBox(LastError, MessageType.Error);
            if (CurrentSnapshot == null) return;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawLegend();
            DrawLocalPanel();
            DrawComparePanel();
            DrawSectorPanel();
            DrawDensityPanel();
            DrawDetailPanel();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(72f))) Reload();
                var biomeNames = new[] { "All", "MoonCrater", "CassiaRoot", "AbandonedMill", "MoonDough" };
                var nextBiome = EditorGUILayout.Popup(Array.IndexOf(biomeNames, selectedBiome), biomeNames,
                    EditorStyles.toolbarPopup, GUILayout.Width(125f));
                if (nextBiome >= 0 && !string.Equals(biomeNames[nextBiome], selectedBiome, StringComparison.Ordinal))
                    TrySelectBiome(biomeNames[nextBiome]);
                var clusterIndex = Math.Max(0, clusterIds.IndexOf(selectedClusterId));
                var nextCluster = EditorGUILayout.Popup(clusterIndex, clusterIds.ToArray(),
                    EditorStyles.toolbarPopup, GUILayout.MinWidth(220f));
                if (nextCluster >= 0 && nextCluster < clusterIds.Count && nextCluster != clusterIndex)
                    TrySelectCluster(clusterIds[nextCluster]);
                var variantIndex = Math.Max(0, variantIds.IndexOf(selectedVariantId));
                var nextVariant = EditorGUILayout.Popup(variantIndex, variantIds.ToArray(),
                    EditorStyles.toolbarPopup, GUILayout.Width(110f));
                if (nextVariant >= 0 && nextVariant < variantIds.Count && nextVariant != variantIndex)
                    TrySelectVariant(variantIds[nextVariant]);
                var nextMode = (TerrainClusterPreviewViewMode)EditorGUILayout.EnumPopup(viewMode,
                    EditorStyles.toolbarPopup, GUILayout.Width(100f));
                if (nextMode != viewMode) TrySelectViewMode(nextMode);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                showFootprint = GUILayout.Toggle(showFootprint, "Footprint");
                showRolesPorts = GUILayout.Toggle(showRolesPorts, "Roles/Ports");
                showSpine = GUILayout.Toggle(showSpine, "Spine");
                showEnvelope = GUILayout.Toggle(showEnvelope, "Envelope");
                showProtected = GUILayout.Toggle(showProtected, "AbsoluteProtected");
                showRoutes = GUILayout.Toggle(showRoutes, "Base/High/Recovery");
                showPattern = GUILayout.Toggle(showPattern, "Pattern Diff");
                showDensity = GUILayout.Toggle(showDensity, "Density");
                showSector = GUILayout.Toggle(showSector, "Sector Frame");
            }
        }

        private static void DrawLegend()
        {
            EditorGUILayout.LabelField("Legend", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "EN Entry | EX Exit | B Base | H High | R Recovery | SP Spine | EV Envelope | " +
                "AP AbsoluteProtected | S Solid | A Air | P+ Pattern Add | P- Pattern Carve | " +
                "CH 12x8 chunk boundary | SEC 48x32 sector frame",
                MessageType.None);
        }

        private void DrawLocalPanel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Cluster-local Tile / 12x8 Chunk Panel", EditorStyles.boldLabel);
            if (showFootprint)
                EditorGUILayout.LabelField("Chunks", string.Join("  ", CurrentSnapshot.ChunkCells.Select(value =>
                    value.Coordinate.X + "," + value.Coordinate.Y + ":" + value.State)));
            if (showRolesPorts)
                EditorGUILayout.LabelField("Roles / Ports", string.Join("  ", CurrentSnapshot.Anchors.Select(value =>
                    value.Token + "@" + value.Coordinate.X + "," + value.Coordinate.Y)));
            if (showSpine)
                EditorGUILayout.LabelField("Spine", CurrentSnapshot.Segments.Count(value => value.Token == "SP Spine") + " ordered edges");
            if (showEnvelope)
                EditorGUILayout.LabelField("Envelope", CurrentSnapshot.EnvelopeCoordinates.Count + " cells");
            if (showProtected)
                EditorGUILayout.LabelField("AbsoluteProtected", CurrentSnapshot.AbsoluteProtectedCoordinates.Count + " cells");
            if (showRoutes)
                EditorGUILayout.LabelField("Routes", CurrentSnapshot.BaselineCoordinates.Count + " base / " +
                    CurrentSnapshot.HighRouteCoordinates.Count + " high / " + CurrentSnapshot.RecoveryCoordinates.Count + " recovery coordinates");
        }

        private void DrawComparePanel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("PatternFree / A / B Compare Panel", EditorStyles.boldLabel);
            var snapshots = viewMode == TerrainClusterPreviewViewMode.Compare ? compareSnapshots : new List<TerrainClusterPreviewSnapshot> { CurrentSnapshot };
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var snapshot in snapshots)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MinWidth(180f)))
                    {
                        EditorGUILayout.LabelField(snapshot.Pattern.IsPatternFree ? "PatternFree" : snapshot.Pattern.PatternId, EditorStyles.boldLabel);
                        if (showPattern)
                        {
                            EditorGUILayout.LabelField("Transform / Origin", snapshot.Pattern.Transform + " / " +
                                snapshot.Pattern.Origin.X + "," + snapshot.Pattern.Origin.Y);
                            EditorGUILayout.LabelField("Target / Changed", snapshot.Pattern.TargetCount + " / " + snapshot.Pattern.ChangedCount);
                            EditorGUILayout.LabelField("Protected writes / changes", snapshot.Pattern.ProtectedWriteCount + " / " + snapshot.Pattern.ProtectedValueChangeCount);
                        }
                    }
                }
            }
        }

        private void DrawSectorPanel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("48x32 Sector Frame Panel", EditorStyles.boldLabel);
            if (!showSector) return;
            EditorGUILayout.LabelField("SEC", "48x32 / 4x4 MicroChunks / CH 12x8");
            EditorGUILayout.LabelField("Centered translation", CurrentSnapshot.SectorFrame.OffsetX + "," + CurrentSnapshot.SectorFrame.OffsetY);
            EditorGUILayout.LabelField("Active / Empty", CurrentSnapshot.SectorFrame.ActiveCoordinates.Count + " / " +
                (TerrainClusterSectorFrameSnapshot.Width * TerrainClusterSectorFrameSnapshot.Height - CurrentSnapshot.SectorFrame.ActiveCoordinates.Count) +
                " " + CurrentSnapshot.SectorFrame.EmptySpaceToken);
        }

        private void DrawDensityPanel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Density Panel", EditorStyles.boldLabel);
            if (!showDensity) return;
            var value = CurrentSnapshot.Density;
            EditorGUILayout.LabelField("Active / Solid / Air", value.ActiveCount + " / " + value.SolidCount + " / " + value.AirCount);
            EditorGUILayout.LabelField("Ratios", "Solid " + value.SolidRatio + " | Air " + value.AirRatio + " | AP " + value.ProtectedRatio);
            EditorGUILayout.LabelField("Pattern target / changed", value.PatternTargetCount + " / " + value.PatternChangedCount + " (" + value.PatternChangedRatio + ")");
            EditorGUILayout.LabelField("Per active chunk", string.Join("  ", value.Chunks.Select(chunk =>
                chunk.Chunk.X + "," + chunk.Chunk.Y + " S" + chunk.SolidCount + "/A" + chunk.AirCount)));
        }

        private void DrawDetailPanel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Digest / Route / Quiet Evidence", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel("Snapshot " + CurrentSnapshot.StableDigest, EditorStyles.textField, GUILayout.Height(18f));
            EditorGUILayout.SelectableLabel("Catalog " + CurrentSnapshot.CatalogDigest, EditorStyles.textField, GUILayout.Height(18f));
            EditorGUILayout.SelectableLabel("Traversal " + CurrentSnapshot.TraversalDigest, EditorStyles.textField, GUILayout.Height(18f));
            EditorGUILayout.SelectableLabel("Witness " + CurrentSnapshot.RouteWitnessDigest, EditorStyles.textField, GUILayout.Height(18f));
            foreach (var value in CurrentSnapshot.RouteEvidence) EditorGUILayout.LabelField(value);
            foreach (var value in CurrentSnapshot.QuietEvidence) EditorGUILayout.LabelField(value);
        }
    }
}
