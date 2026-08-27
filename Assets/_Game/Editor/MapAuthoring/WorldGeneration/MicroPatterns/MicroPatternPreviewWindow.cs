using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Map.WorldGeneration.MicroPatterns;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.WorldGeneration.MicroPatterns
{
    public sealed class MicroPatternPreviewWindow : EditorWindow
    {
        public const string MenuPath = "Tools/MapDesign/MicroPattern Preview";
        public const string WindowTitle = "MicroPattern Preview";

        private readonly List<string> biomeOptions = new List<string>();
        private readonly List<MicroPatternDefinition> visibleDefinitions =
            new List<MicroPatternDefinition>();
        private readonly List<string> importErrors = new List<string>();

        [NonSerialized] private MicroPatternPreviewModel model;
        [NonSerialized] private MicroPatternAuthoringCatalog catalog;
        [NonSerialized] private MicroPatternPreviewSnapshot currentSnapshot;
        [NonSerialized] private Vector2 scroll;
        [NonSerialized] private string selectedBiome = "All";
        [NonSerialized] private string selectedPatternId = string.Empty;
        [NonSerialized] private MicroPatternTransform selectedTransform = MicroPatternTransform.R0;
        [NonSerialized] private MicroPatternPreviewFixtureKind selectedFixture =
            MicroPatternPreviewFixtureKind.Clean;

        public MicroPatternPreviewSnapshot CurrentSnapshot => currentSnapshot;
        public MicroPatternAuthoringCatalog Catalog => catalog;
        public IReadOnlyList<string> PatternIds => visibleDefinitions
            .Select(value => value.Id.Value).ToArray();
        public IReadOnlyList<MicroPatternTransform> AvailableTransforms
        {
            get
            {
                var selected = FindSelectedDefinition();
                return selected == null
                    ? Array.Empty<MicroPatternTransform>()
                    : selected.AllowedTransforms.ToArray();
            }
        }
        public string SelectedBiome => selectedBiome;
        public string SelectedPatternId => selectedPatternId;
        public MicroPatternTransform SelectedTransform => selectedTransform;
        public MicroPatternPreviewFixtureKind SelectedFixture => selectedFixture;
        public int PanelCount => 5;
        public string LastError => importErrors.Count == 0
            ? string.Empty
            : string.Join("\n", importErrors);

        [MenuItem(MenuPath)]
        public static MicroPatternPreviewWindow Open()
        {
            var window = GetWindow<MicroPatternPreviewWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(760f, 520f);
            window.Reload();
            window.Show();
            return window;
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = new Vector2(760f, 520f);
            if (model == null || catalog == null) Reload();
        }

        public bool Reload()
        {
            model = new MicroPatternPreviewModel();
            importErrors.Clear();
            var result = model.LoadCatalog();
            if (!result.Success || !result.Published || result.Catalog == null)
            {
                catalog = null;
                currentSnapshot = null;
                visibleDefinitions.Clear();
                biomeOptions.Clear();
                importErrors.AddRange(result.Errors.Select(value => value.ToString()));
                if (importErrors.Count == 0)
                    importErrors.Add("Physical MicroPattern importer did not publish a catalog.");
                Repaint();
                return false;
            }

            catalog = result.Catalog;
            biomeOptions.Clear();
            biomeOptions.Add("All");
            biomeOptions.AddRange(catalog.Definitions
                .Select(value => value.AllowedBiomes.Single().CanonicalId)
                .Distinct()
                .OrderBy(value => value, StringComparer.Ordinal));
            if (!biomeOptions.Contains(selectedBiome)) selectedBiome = "All";
            RefreshVisibleDefinitions();
            return BuildCurrent();
        }

        public bool TrySelectBiome(string biomeId)
        {
            var requested = string.IsNullOrEmpty(biomeId) ? "All" : biomeId;
            if (!biomeOptions.Contains(requested)) return false;
            selectedBiome = requested;
            RefreshVisibleDefinitions();
            return BuildCurrent();
        }

        public bool TrySelectPattern(string patternId)
        {
            var definition = visibleDefinitions.FirstOrDefault(value =>
                string.Equals(value.Id.Value, patternId, StringComparison.Ordinal));
            if (definition == null) return false;
            selectedPatternId = definition.Id.Value;
            if (!definition.AllowedTransforms.Contains(selectedTransform))
                selectedTransform = definition.AllowedTransforms[0];
            return BuildCurrent();
        }

        public bool TrySelectTransform(MicroPatternTransform transform)
        {
            var definition = FindSelectedDefinition();
            if (definition == null || !definition.AllowedTransforms.Contains(transform)) return false;
            selectedTransform = transform;
            return BuildCurrent();
        }

        public bool TrySelectFixture(MicroPatternPreviewFixtureKind fixture)
        {
            if (fixture < MicroPatternPreviewFixtureKind.Clean ||
                fixture > MicroPatternPreviewFixtureKind.SameLayerConflict) return false;
            selectedFixture = fixture;
            return BuildCurrent();
        }

        private void RefreshVisibleDefinitions()
        {
            visibleDefinitions.Clear();
            if (catalog == null) return;
            visibleDefinitions.AddRange(catalog.Definitions
                .Where(value => selectedBiome == "All" ||
                    value.AllowedBiomes.Any(biome =>
                        string.Equals(biome.CanonicalId, selectedBiome, StringComparison.Ordinal)))
                .OrderBy(value => value.Id.Value, StringComparer.Ordinal));
            if (!visibleDefinitions.Any(value =>
                string.Equals(value.Id.Value, selectedPatternId, StringComparison.Ordinal)))
                selectedPatternId = visibleDefinitions.Count == 0
                    ? string.Empty
                    : visibleDefinitions[0].Id.Value;
            var definition = FindSelectedDefinition();
            if (definition != null && !definition.AllowedTransforms.Contains(selectedTransform))
                selectedTransform = definition.AllowedTransforms[0];
        }

        private bool BuildCurrent()
        {
            importErrors.Clear();
            currentSnapshot = null;
            if (model == null || catalog == null || string.IsNullOrEmpty(selectedPatternId))
            {
                importErrors.Add("Select a published MicroPattern definition.");
                Repaint();
                return false;
            }

            var result = model.Build(new MicroPatternPreviewRequest(
                selectedPatternId, selectedTransform, selectedFixture), catalog);
            if (!result.Success)
            {
                importErrors.AddRange(result.Errors.Select(value => value.ToString()));
                if (importErrors.Count == 0) importErrors.Add("Preview build did not publish a snapshot.");
                Repaint();
                return false;
            }

            currentSnapshot = result.Snapshot;
            Repaint();
            return true;
        }

        private MicroPatternDefinition FindSelectedDefinition()
        {
            return visibleDefinitions.FirstOrDefault(value =>
                string.Equals(value.Id.Value, selectedPatternId, StringComparison.Ordinal));
        }

        private void OnGUI()
        {
            DrawToolbar();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (importErrors.Count != 0)
            {
                EditorGUILayout.HelpBox(LastError, MessageType.Error);
            }
            else if (currentSnapshot == null)
            {
                EditorGUILayout.HelpBox("No preview snapshot is available.", MessageType.Info);
            }
            else
            {
                DrawHeader(currentSnapshot);
                DrawPanels(currentSnapshot);
                DrawAudit(currentSnapshot);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                    Reload();
                DrawBiomeSelector();
                DrawPatternSelector();
                DrawTransformSelector();
                DrawFixtureSelector();
            }
        }

        private void DrawBiomeSelector()
        {
            if (biomeOptions.Count == 0) return;
            var index = Mathf.Max(0, biomeOptions.IndexOf(selectedBiome));
            var next = EditorGUILayout.Popup(index, biomeOptions.ToArray(), GUILayout.Width(150f));
            if (next != index) TrySelectBiome(biomeOptions[next]);
        }

        private void DrawPatternSelector()
        {
            if (visibleDefinitions.Count == 0) return;
            var ids = visibleDefinitions.Select(value => value.Id.Value).ToArray();
            var index = Mathf.Max(0, Array.IndexOf(ids, selectedPatternId));
            var next = EditorGUILayout.Popup(index, ids, GUILayout.MinWidth(210f));
            if (next != index) TrySelectPattern(ids[next]);
        }

        private void DrawTransformSelector()
        {
            var transforms = AvailableTransforms.ToArray();
            if (transforms.Length == 0) return;
            var labels = transforms.Select(value => value.ToString()).ToArray();
            var index = Mathf.Max(0, Array.IndexOf(transforms, selectedTransform));
            var next = EditorGUILayout.Popup(index, labels, GUILayout.Width(80f));
            if (next != index) TrySelectTransform(transforms[next]);
        }

        private void DrawFixtureSelector()
        {
            var fixtures = (MicroPatternPreviewFixtureKind[])Enum.GetValues(
                typeof(MicroPatternPreviewFixtureKind));
            var labels = fixtures.Select(value => value.ToString()).ToArray();
            var index = Mathf.Max(0, Array.IndexOf(fixtures, selectedFixture));
            var next = EditorGUILayout.Popup(index, labels, GUILayout.Width(150f));
            if (next != index) TrySelectFixture(fixtures[next]);
        }

        private static void DrawHeader(MicroPatternPreviewSnapshot snapshot)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(snapshot.PatternId, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Biome / Role / Weight",
                snapshot.BiomeId + " / " + snapshot.RoleGroup + " / " + snapshot.Weight);
            EditorGUILayout.LabelField("Policy / Transform / Fixture",
                snapshot.ProtectedPolicy + " / " + snapshot.SelectedTransform + " / " +
                snapshot.FixtureKind);
            EditorGUILayout.LabelField("Allowed transforms",
                string.Join(", ", snapshot.AllowedTransforms.Select(value => value.ToString())));
        }

        private static void DrawPanels(MicroPatternPreviewSnapshot snapshot)
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawGrid("Original", snapshot.OriginalCells);
                DrawGrid("Transformed", snapshot.TransformedCells);
                DrawGrid("Protected-Effective", snapshot.ProtectedEffectiveCells);
                DrawGrid("Before", snapshot.BeforeCells);
                DrawGrid("After", snapshot.AfterCells);
            }
        }

        private static void DrawGrid(string title, IReadOnlyList<MicroPatternPreviewCell> cells)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(142f)))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                var byCoordinate = cells.ToDictionary(
                    value => value.Coordinate.X + "," + value.Coordinate.Y,
                    StringComparer.Ordinal);
                for (var y = 0; y < 4; y++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        for (var x = 0; x < 4; x++)
                        {
                            MicroPatternPreviewCell cell;
                            var found = byCoordinate.TryGetValue(x + "," + y, out cell);
                            var token = found ? cell.CompactToken : "·";
                            if (found && cell.IsProtected) token = "P " + token;
                            var tooltip = found ? string.Join("\n", cell.Details) : "NoChange";
                            GUILayout.Label(new GUIContent(token, tooltip), EditorStyles.miniButton,
                                GUILayout.Width(30f), GUILayout.Height(27f));
                        }
                    }
                }
            }
        }

        private static void DrawAudit(MicroPatternPreviewSnapshot snapshot)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stage-ordered writes", EditorStyles.boldLabel);
            foreach (var write in snapshot.Writes)
            {
                EditorGUILayout.LabelField(
                    ((int)write.Stage) + " " + write.Stage + "  (" +
                    write.TargetCoordinate.X + "," + write.TargetCoordinate.Y + ") " +
                    write.Layer + " " + write.Operation + " " + write.SemanticValue);
            }
            if (snapshot.Writes.Count == 0) EditorGUILayout.LabelField("No published writes.");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Changed cell/layer diff", EditorStyles.boldLabel);
            foreach (var diff in snapshot.Diffs)
            {
                EditorGUILayout.LabelField(
                    ((int)diff.Stage) + " (" + diff.TargetCoordinate.X + "," +
                    diff.TargetCoordinate.Y + ") " + diff.Layer + ": " +
                    Display(diff.BeforeValue) + " → " + Display(diff.AfterValue));
            }
            if (snapshot.Diffs.Count == 0) EditorGUILayout.LabelField("No published diff.");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Digests and pipeline evidence", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel("catalog " + snapshot.CatalogDigest,
                EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.SelectableLabel("definition " + snapshot.DefinitionDigest,
                EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.SelectableLabel("transform " + snapshot.TransformDigest,
                EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.SelectableLabel("plan " + Display(snapshot.PlanDigest),
                EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.SelectableLabel("render " + Display(snapshot.RenderDigest),
                EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.SelectableLabel("silhouette " + Display(snapshot.SilhouetteDigest),
                EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.SelectableLabel("preview " + snapshot.StableDigest,
                EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.LabelField("Silhouette masks",
                snapshot.SilhouetteAddSolidMask + " / " + snapshot.SilhouetteCarveAirMask);
            EditorGUILayout.LabelField("Protected hits", snapshot.ProtectedHitCount.ToString());
            DrawEvidence("Protected provenance", snapshot.ProtectedProvenance);
            DrawEvidence("Pipeline errors", snapshot.PipelineErrors);
            DrawEvidence("Conflicts", snapshot.ConflictEvidence);
        }

        private static void DrawEvidence(string title, IReadOnlyList<string> values)
        {
            if (values.Count == 0) return;
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            foreach (var value in values) EditorGUILayout.LabelField(value, EditorStyles.wordWrappedLabel);
        }

        private static string Display(string value) => string.IsNullOrEmpty(value) ? "<none>" : value;
    }
}
