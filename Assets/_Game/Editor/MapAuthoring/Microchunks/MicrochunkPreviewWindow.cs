using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Map.WorldGeneration.Microchunks;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkPreviewWindow : EditorWindow
    {
        [NonSerialized] private string selectedMicrochunkId = string.Empty;
        [NonSerialized] private MicrochunkSocketAndSlotEditorViewModel editorState;
        [NonSerialized] private IReadOnlyList<MicrochunkCsvImportIssue> importIssues =
            Array.Empty<MicrochunkCsvImportIssue>();
        [NonSerialized] private IReadOnlyList<MicrochunkCsvExportIssue> exportIssues =
            Array.Empty<MicrochunkCsvExportIssue>();
        [NonSerialized] private bool showTiles = true;
        [NonSerialized] private bool showSockets = true;
        [NonSerialized] private bool showObjectSlots = true;
        [NonSerialized] private bool showReachability = true;
        [NonSerialized] private bool previewR0 = true;
        [NonSerialized] private bool previewMirrorX = true;
        [NonSerialized] private bool previewMirrorY = true;
        [NonSerialized] private bool previewR180 = true;
        [NonSerialized] private MicrochunkPreviewReport lastReport;
        [NonSerialized] private string lastError = string.Empty;
        [NonSerialized] private Vector2 scrollPosition;
        [NonSerialized] private int selectedTransformIndex;
        [NonSerialized] private MicrochunkLocalCoord? selectedCoordinate;

        public string SelectedMicrochunkId => selectedMicrochunkId;
        public MicrochunkSocketAndSlotEditorViewModel EditorState => editorState;
        public MicrochunkPreviewReport LastReport => lastReport;
        public string LastError => lastError;

        [MenuItem("Tools/Map/Microchunk Preview and Report")]
        public static MicrochunkPreviewWindow Open()
        {
            var window = GetWindow<MicrochunkPreviewWindow>();
            window.titleContent = new GUIContent("Microchunk Preview");
            window.minSize = new Vector2(760f, 620f);
            window.Show();
            return window;
        }

        public void UseDetachedEditorState(
            string microchunkId,
            MicrochunkSocketAndSlotEditorViewModel detachedEditorState,
            IEnumerable<MicrochunkCsvImportIssue> inputImportIssues = null,
            IEnumerable<MicrochunkCsvExportIssue> inputExportIssues = null)
        {
            selectedMicrochunkId = microchunkId ?? string.Empty;
            editorState = detachedEditorState;
            importIssues = (inputImportIssues ?? Enumerable.Empty<MicrochunkCsvImportIssue>()).ToArray();
            exportIssues = (inputExportIssues ?? Enumerable.Empty<MicrochunkCsvExportIssue>()).ToArray();
            lastReport = null;
            lastError = string.Empty;
            selectedCoordinate = null;
            Repaint();
        }

        public MicrochunkPreviewReport Generate(MicrochunkPreviewRequest request)
        {
            lastReport = new MicrochunkPreviewBuilder().Build(
                request ?? throw new ArgumentNullException(nameof(request)));
            selectedMicrochunkId = request.SelectedMicrochunkId;
            editorState = request.EditorState;
            lastError = string.Empty;
            selectedTransformIndex = 0;
            selectedCoordinate = null;
            Repaint();
            return lastReport;
        }

        public bool TryGeneratePreview()
        {
            try
            {
                if (editorState == null)
                    throw new InvalidOperationException("A detached microchunk editor state is required.");
                var transforms = SelectedTransforms();
                var request = new MicrochunkPreviewRequest(
                    selectedMicrochunkId,
                    editorState,
                    transforms,
                    showTiles,
                    showSockets,
                    showObjectSlots,
                    showReachability,
                    MicrochunkPreviewValidationOptions.All,
                    importIssues,
                    exportIssues);
                Generate(request);
                return true;
            }
            catch (Exception exception)
            {
                lastReport = null;
                lastError = exception.Message;
                selectedCoordinate = null;
                Repaint();
                return false;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Microchunk Transform Preview and Validation Report", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Preview generation is explicit and in-memory only. It never saves CSV, scenes, or prefabs.",
                MessageType.Info);

            selectedMicrochunkId = EditorGUILayout.TextField("Selected microchunk ID", selectedMicrochunkId);
            EditorGUILayout.LabelField(
                "Detached editor state",
                editorState == null ? "Not assigned" : "Assigned");

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Transforms", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                previewR0 = GUILayout.Toggle(previewR0, "R0");
                previewMirrorX = GUILayout.Toggle(previewMirrorX, "MIRROR_X");
                previewMirrorY = GUILayout.Toggle(previewMirrorY, "MIRROR_Y");
                previewR180 = GUILayout.Toggle(previewR180, "R180");
            }

            EditorGUILayout.LabelField("Overlays", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                showTiles = GUILayout.Toggle(showTiles, "Tiles");
                showSockets = GUILayout.Toggle(showSockets, "Sockets");
                showObjectSlots = GUILayout.Toggle(showObjectSlots, "Object slots");
                showReachability = GUILayout.Toggle(showReachability, "Reachability");
            }

            if (GUILayout.Button("Generate Preview and Report")) TryGeneratePreview();

            if (!string.IsNullOrEmpty(lastError))
                EditorGUILayout.HelpBox(lastError, MessageType.Error);
            if (lastReport == null) return;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                lastReport.Success ? "Preview report: PASS" : "Preview report: issues found",
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Errors / warnings",
                lastReport.ErrorCount + " / " + lastReport.WarningCount);

            if (lastReport.Transforms.Count > 0)
            {
                var labels = lastReport.Transforms
                    .Select(value => MicrochunkTransformUtility.ToTransformToken(value.Transform))
                    .ToArray();
                selectedTransformIndex = Mathf.Clamp(selectedTransformIndex, 0, labels.Length - 1);
                selectedTransformIndex = GUILayout.Toolbar(selectedTransformIndex, labels);
                DrawGrid(lastReport.Transforms[selectedTransformIndex]);
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (var issue in lastReport.Issues.Take(300))
            {
                EditorGUILayout.HelpBox(
                    issue.ToString(),
                    issue.IsError
                        ? MessageType.Error
                        : issue.Severity == MicrochunkPreviewIssueSeverity.Warning
                            ? MessageType.Warning
                            : MessageType.Info);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawGrid(MicrochunkPreviewTransformReport transform)
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("12 x 8 local-coordinate preview", EditorStyles.boldLabel);
            for (var y = MicrochunkConstants.HeightTiles - 1; y >= 0; y--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(y.ToString(), GUILayout.Width(18f));
                    for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
                    {
                        var cell = transform.GetCell(x, y);
                        var previous = GUI.backgroundColor;
                        GUI.backgroundColor = ColorFor(cell);
                        if (GUILayout.Button(x + "," + y, GUILayout.Width(48f), GUILayout.Height(25f)))
                            selectedCoordinate = cell.Coordinate;
                        GUI.backgroundColor = previous;
                    }
                }
            }

            if (!selectedCoordinate.HasValue) return;
            var selected = transform.GetCell(selectedCoordinate.Value.X, selectedCoordinate.Value.Y);
            EditorGUILayout.LabelField("Coordinate detail", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Coordinate", selected.Coordinate.X + ", " + selected.Coordinate.Y);
            EditorGUILayout.LabelField("Reachability", selected.ReachabilityState.ToString());
            EditorGUILayout.LabelField("Sockets", string.Join(", ", selected.SocketIds));
            EditorGUILayout.LabelField("Object slots", string.Join(", ", selected.ObjectSlotIds));
            if (selected.TileCell != null)
            {
                EditorGUILayout.LabelField(
                    "Tile layers",
                    string.Join(", ", MicrochunkTileLayerOccupancy.FromCell(selected.TileCell).OccupiedLayers));
            }
        }

        private IReadOnlyList<MicrochunkTransform> SelectedTransforms()
        {
            var values = new List<MicrochunkTransform>();
            if (previewR0) values.Add(MicrochunkTransform.R0);
            if (previewMirrorX) values.Add(MicrochunkTransform.MirrorX);
            if (previewMirrorY) values.Add(MicrochunkTransform.MirrorY);
            if (previewR180) values.Add(MicrochunkTransform.R180);
            return values;
        }

        private static Color ColorFor(MicrochunkPreviewCellOverlay cell)
        {
            switch (cell.ReachabilityState)
            {
                case MicrochunkPreviewReachabilityState.BlockedSolid: return new Color(0.32f, 0.32f, 0.32f);
                case MicrochunkPreviewReachabilityState.Unreachable: return new Color(0.72f, 0.25f, 0.25f);
                case MicrochunkPreviewReachabilityState.Reachable: return new Color(0.35f, 0.68f, 0.38f);
                case MicrochunkPreviewReachabilityState.PathWitness: return new Color(0.95f, 0.78f, 0.28f);
                case MicrochunkPreviewReachabilityState.SocketEntry: return new Color(0.28f, 0.62f, 0.92f);
                case MicrochunkPreviewReachabilityState.SocketExit: return new Color(0.72f, 0.42f, 0.90f);
                default: return Color.white;
            }
        }
    }
}
