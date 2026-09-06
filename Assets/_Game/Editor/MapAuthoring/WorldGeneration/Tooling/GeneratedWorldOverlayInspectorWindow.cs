using System;
using System.IO;
using StarNight.Map.WorldGeneration.Tooling;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.WorldGeneration.Tooling
{
    public sealed class GeneratedWorldOverlayInspectorWindow : EditorWindow
    {
        public const string MenuPath = "Tools/MapDesign/Generated World Overlay Inspector";
        public const string WindowTitle = "Generated World Overlay Inspector";

        private static int openInvocationCount;
        private static int externalActionInvocationCount;

        [SerializeField] private string runArtifactPath =
            GeneratedWorldOverlaySamplePublisher.SourceRunArtifactRelative;
        [SerializeField] private int selectedSectorX;
        [SerializeField] private int selectedSectorY;
        [SerializeField] private int selectedCellX;
        [SerializeField] private int selectedCellY;
        [NonSerialized] private bool[] worldLayerEnabled;
        [NonSerialized] private bool[] canvasLayerEnabled;
        [NonSerialized] private GeneratedWorldOverlaySnapshot worldSnapshot;
        [NonSerialized] private GeneratedSectorCanvasInspection sectorInspection;
        [NonSerialized] private Vector2 pageScroll;
        [NonSerialized] private Vector2 canvasScroll;
        [NonSerialized] private int viewStateMutationCount;
        [NonSerialized] private string navigationSelectionPath = string.Empty;

        public static int OpenInvocationCount => openInvocationCount;
        public static int ExternalActionInvocationCount => externalActionInvocationCount;
        public int ViewStateMutationCount => viewStateMutationCount;
        public GeneratedWorldOverlaySnapshot WorldSnapshot => worldSnapshot;
        public GeneratedSectorCanvasInspection SectorInspection => sectorInspection;
        public GeneratedWorldOverlaySectorRecord SelectedSector =>
            worldSnapshot?.Sector(selectedSectorX, selectedSectorY);
        public GeneratedSectorCanvasCellRecord SelectedCell =>
            sectorInspection?.Cell(selectedCellX, selectedCellY);
        public string WorldSnapshotDigest => worldSnapshot?.CanonicalDigest ?? string.Empty;
        public string SectorInspectionDigest => sectorInspection?.CanonicalDigest ?? string.Empty;
        public string RunArtifactPath => runArtifactPath;
        public string NavigationSelectionPath => navigationSelectionPath;

        [MenuItem(MenuPath)]
        public static GeneratedWorldOverlayInspectorWindow Open()
        {
            openInvocationCount++;
            var window = GetWindow<GeneratedWorldOverlayInspectorWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(760f, 640f);
            window.EnsureReadOnlyModels();
            window.Show();
            return window;
        }

        public static void ResetInvocationDiagnostics()
        {
            openInvocationCount = 0;
            externalActionInvocationCount = 0;
        }

        public bool WorldLayerEnabled(string token)
        {
            EnsureLayerState();
            var index = GeneratedWorldOverlayLayerCatalog.IndexOf(token);
            if (index < 0) throw new ArgumentException("Unknown world layer token.", nameof(token));
            return worldLayerEnabled[index];
        }

        public bool CanvasLayerEnabled(string token)
        {
            EnsureLayerState();
            var index = GeneratedSectorCanvasLayerCatalog.IndexOf(token);
            if (index < 0) throw new ArgumentException("Unknown canvas layer token.", nameof(token));
            return canvasLayerEnabled[index];
        }

        public void SetWorldLayerEnabled(string token, bool enabled)
        {
            EnsureLayerState();
            var index = GeneratedWorldOverlayLayerCatalog.IndexOf(token);
            if (index < 0) throw new ArgumentException("Unknown world layer token.", nameof(token));
            if (worldLayerEnabled[index] == enabled) return;
            worldLayerEnabled[index] = enabled;
            viewStateMutationCount++;
            Repaint();
        }

        public void SetCanvasLayerEnabled(string token, bool enabled)
        {
            EnsureLayerState();
            var index = GeneratedSectorCanvasLayerCatalog.IndexOf(token);
            if (index < 0) throw new ArgumentException("Unknown canvas layer token.", nameof(token));
            if (canvasLayerEnabled[index] == enabled) return;
            canvasLayerEnabled[index] = enabled;
            viewStateMutationCount++;
            Repaint();
        }

        public void SelectSector(int sectorX, int sectorY)
        {
            if (sectorX < 0 || sectorX >= GeneratedWorldOverlayLayerCatalog.WorldWidthSectors)
                throw new ArgumentOutOfRangeException(nameof(sectorX));
            if (sectorY < 0 || sectorY >= GeneratedWorldOverlayLayerCatalog.WorldHeightSectors)
                throw new ArgumentOutOfRangeException(nameof(sectorY));
            if (selectedSectorX == sectorX && selectedSectorY == sectorY) return;
            selectedSectorX = sectorX;
            selectedSectorY = sectorY;
            selectedCellX = 0;
            selectedCellY = 0;
            sectorInspection = GeneratedSectorCanvasInspection.CreateMissingDataSample(
                selectedSectorX, selectedSectorY, worldSnapshot?.CreatedUtc ?? string.Empty);
            viewStateMutationCount++;
            Repaint();
        }

        public void SelectCell(int localX, int localY)
        {
            if (localX < 0 || localX >= GeneratedSectorCanvasLayerCatalog.Width)
                throw new ArgumentOutOfRangeException(nameof(localX));
            if (localY < 0 || localY >= GeneratedSectorCanvasLayerCatalog.Height)
                throw new ArgumentOutOfRangeException(nameof(localY));
            if (selectedCellX == localX && selectedCellY == localY) return;
            selectedCellX = localX;
            selectedCellY = localY;
            viewStateMutationCount++;
            Repaint();
        }

        public void ReceiveNavigationSelection(int sectorX, int sectorY, int localX, int localY,
            string selectionPath)
        {
            if (string.IsNullOrWhiteSpace(selectionPath))
                throw new ArgumentException("Navigation selection path is required.",
                    nameof(selectionPath));
            SelectSector(sectorX, sectorY);
            SelectCell(localX, localY);
            if (!string.Equals(navigationSelectionPath, selectionPath, StringComparison.Ordinal))
            {
                navigationSelectionPath = selectionPath;
                viewStateMutationCount++;
            }
            Repaint();
        }

        public void ReloadReadOnlyData()
        {
            EnsureLayerState();
            var sourceDigest = GeneratedWorldOverlaySamplePublisher.ResolveSourceRunArtifactDigest(
                null, runArtifactPath);
            var createdUtc = string.Empty;
            worldSnapshot = GeneratedWorldOverlaySnapshot.CreateMissingDataSample(sourceDigest,
                GeneratedWorldOverlaySamplePublisher.Map2001HandoffDigest, createdUtc);
            sectorInspection = GeneratedSectorCanvasInspection.CreateMissingDataSample(
                selectedSectorX, selectedSectorY, createdUtc);
            Repaint();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = new Vector2(760f, 640f);
            EnsureReadOnlyModels();
        }

        private void EnsureReadOnlyModels()
        {
            EnsureLayerState();
            selectedSectorX = Mathf.Clamp(selectedSectorX, 0,
                GeneratedWorldOverlayLayerCatalog.WorldWidthSectors - 1);
            selectedSectorY = Mathf.Clamp(selectedSectorY, 0,
                GeneratedWorldOverlayLayerCatalog.WorldHeightSectors - 1);
            selectedCellX = Mathf.Clamp(selectedCellX, 0,
                GeneratedSectorCanvasLayerCatalog.Width - 1);
            selectedCellY = Mathf.Clamp(selectedCellY, 0,
                GeneratedSectorCanvasLayerCatalog.Height - 1);
            if (worldSnapshot == null || sectorInspection == null) ReloadReadOnlyData();
        }

        private void EnsureLayerState()
        {
            if (worldLayerEnabled == null ||
                worldLayerEnabled.Length != GeneratedWorldOverlayLayerCatalog.Layers.Count)
            {
                worldLayerEnabled = new bool[GeneratedWorldOverlayLayerCatalog.Layers.Count];
                for (var index = 0; index < worldLayerEnabled.Length; index++)
                    worldLayerEnabled[index] = GeneratedWorldOverlayLayerCatalog.Layers[index]
                        .EnabledByDefault;
            }
            if (canvasLayerEnabled == null ||
                canvasLayerEnabled.Length != GeneratedSectorCanvasLayerCatalog.Layers.Count)
            {
                canvasLayerEnabled = new bool[GeneratedSectorCanvasLayerCatalog.Layers.Count];
                for (var index = 0; index < canvasLayerEnabled.Length; index++)
                    canvasLayerEnabled[index] = GeneratedSectorCanvasLayerCatalog.Layers[index]
                        .EnabledByDefault;
            }
        }

        private void OnGUI()
        {
            EnsureReadOnlyModels();
            pageScroll = EditorGUILayout.BeginScrollView(pageScroll);
            EditorGUILayout.LabelField("Read-only source", EditorStyles.boldLabel);
            runArtifactPath = EditorGUILayout.TextField("Run artifact path", runArtifactPath);
            if (GUILayout.Button("Reload read-only sample", GUILayout.Width(190f)))
                ReloadReadOnlyData();
            EditorGUILayout.SelectableLabel("Source digest  " +
                worldSnapshot.SourceRunArtifactDigest, EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (!string.IsNullOrWhiteSpace(navigationSelectionPath))
                EditorGUILayout.LabelField("Navigation selection", navigationSelectionPath);

            DrawWorldLayerToggles();
            DrawWorldGrid();
            DrawSelectedSectorSummary();
            DrawCanvasLayerToggles();
            DrawSectorCanvas();
            DrawSelectedCellDetail();
            DrawValidationMarkers();
            DrawReadOnlyUtilities();
            EditorGUILayout.EndScrollView();
        }

        private void DrawWorldLayerToggles()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("World overlay layers (view only)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                for (var index = 0; index < GeneratedWorldOverlayLayerCatalog.Layers.Count; index++)
                {
                    var layer = GeneratedWorldOverlayLayerCatalog.Layers[index];
                    var next = EditorGUILayout.ToggleLeft(layer.Token, worldLayerEnabled[index],
                        GUILayout.Width(82f));
                    if (next != worldLayerEnabled[index]) SetWorldLayerEnabled(layer.Token, next);
                }
            }
        }

        private void DrawWorldGrid()
        {
            EditorGUILayout.LabelField("World sectors (" +
                GeneratedWorldOverlayLayerCatalog.WorldWidthSectors + " x " +
                GeneratedWorldOverlayLayerCatalog.WorldHeightSectors + ")",
                EditorStyles.boldLabel);
            for (var y = 0; y < GeneratedWorldOverlayLayerCatalog.WorldHeightSectors; y++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (var x = 0; x < GeneratedWorldOverlayLayerCatalog.WorldWidthSectors; x++)
                    {
                        var label = x + "," + y;
                        using (new EditorGUI.DisabledScope(x == selectedSectorX && y == selectedSectorY))
                            if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.Width(39f)))
                                SelectSector(x, y);
                    }
                }
            }
        }

        private void DrawSelectedSectorSummary()
        {
            var selected = SelectedSector;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected sector",
                selectedSectorX + "," + selectedSectorY, EditorStyles.boldLabel);
            foreach (var fact in selected.LayerFacts)
            {
                if (!WorldLayerEnabled(fact.LayerToken)) continue;
                EditorGUILayout.LabelField(fact.ShortLabel + " / " + fact.OwnerCategory,
                    fact.IsMissingData ? "MissingData: " + fact.Summary : fact.Summary);
            }
        }

        private void DrawCanvasLayerToggles()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sector canvas layers (view only)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                for (var index = 0; index < GeneratedSectorCanvasLayerCatalog.Layers.Count; index++)
                {
                    var layer = GeneratedSectorCanvasLayerCatalog.Layers[index];
                    var next = EditorGUILayout.ToggleLeft(layer.Token, canvasLayerEnabled[index],
                        GUILayout.Width(110f));
                    if (next != canvasLayerEnabled[index]) SetCanvasLayerEnabled(layer.Token, next);
                }
            }
        }

        private void DrawSectorCanvas()
        {
            const float cellSize = 10f;
            var canvasWidth = GeneratedSectorCanvasLayerCatalog.Width * cellSize;
            var canvasHeight = GeneratedSectorCanvasLayerCatalog.Height * cellSize;
            canvasScroll = EditorGUILayout.BeginScrollView(canvasScroll,
                GUILayout.Height(Mathf.Min(canvasHeight + 18f, 350f)));
            var rect = GUILayoutUtility.GetRect(canvasWidth, canvasHeight,
                GUILayout.Width(canvasWidth), GUILayout.Height(canvasHeight));
            for (var y = 0; y < GeneratedSectorCanvasLayerCatalog.Height; y++)
            for (var x = 0; x < GeneratedSectorCanvasLayerCatalog.Width; x++)
            {
                var cell = sectorInspection.Cell(x, y);
                var cellRect = new Rect(rect.x + x * cellSize, rect.y + y * cellSize,
                    cellSize - 1f, cellSize - 1f);
                var color = cell.IsMissingData
                    ? new Color(0.28f, 0.28f, 0.28f)
                    : new Color(0.20f, 0.55f, 0.34f);
                if (x == selectedCellX && y == selectedCellY) color = new Color(1f, 0.65f, 0.1f);
                EditorGUI.DrawRect(cellRect, color);
            }
            var current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && rect.Contains(current.mousePosition))
            {
                var x = Mathf.FloorToInt((current.mousePosition.x - rect.x) / cellSize);
                var y = Mathf.FloorToInt((current.mousePosition.y - rect.y) / cellSize);
                SelectCell(x, y);
                current.Use();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawSelectedCellDetail()
        {
            var cell = SelectedCell;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected cell detail", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Sector coordinate", cell.SectorX + "," + cell.SectorY);
            EditorGUILayout.LabelField("Local cell coordinate", cell.LocalX + "," + cell.LocalY);
            EditorGUILayout.LabelField("World cell coordinate", cell.HasWorldCellCoordinate
                ? cell.WorldX + "," + cell.WorldY
                : "MissingData");
            EditorGUILayout.LabelField("Owner layer", cell.OwnerLayer);
            EditorGUILayout.LabelField("Source owner", cell.SourceOwner);
            EditorGUILayout.LabelField("Provenance id", cell.ProvenanceId);
            EditorGUILayout.LabelField("Spine", cell.SpineMarker + " / " + cell.SpineMovementKind);
            EditorGUILayout.LabelField("Envelope", cell.EnvelopeMarker + " / " + cell.EnvelopeRelation);
            EditorGUILayout.LabelField("Density", cell.DensityMarker);
            EditorGUILayout.LabelField("Validation", cell.ValidationMarker);
            EditorGUILayout.LabelField("Missing data", cell.MissingDataMarker);
        }

        private void DrawValidationMarkers()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation markers (read only)", EditorStyles.boldLabel);
            foreach (var record in worldSnapshot.ValidationRecords)
                EditorGUILayout.HelpBox(record.Owner + " / " + record.State + " / " + record.Marker,
                    MessageType.Info);
        }

        private void DrawReadOnlyUtilities()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open generated MAP20_02 output folder"))
                {
                    var output = GeneratedWorldOverlaySamplePublisher.OutputDirectory(null);
                    if (Directory.Exists(output)) EditorUtility.RevealInFinder(output);
                }
                if (GUILayout.Button("Copy overlay digest"))
                    EditorGUIUtility.systemCopyBuffer = worldSnapshot.CanonicalDigest;
            }
        }
    }
}
