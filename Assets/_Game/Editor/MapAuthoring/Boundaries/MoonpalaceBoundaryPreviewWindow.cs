using System;
using System.Linq;
using StarNight.Map.WorldGeneration.Boundaries;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Boundaries
{
    public sealed class MoonpalaceBoundaryPreviewWindow : EditorWindow
    {
        public const string MenuPath = "Tools/Map/Moonpalace Boundary Preview";

        [NonSerialized] private MoonpalaceBoundaryPreviewViewModel viewModel;
        [NonSerialized] private Vector2 pairScroll;
        [NonSerialized] private Vector2 candidateScroll;
        [NonSerialized] private Vector2 issueScroll;
        [NonSerialized] private MoonpalaceBoundaryPreviewCell selectedCell;
        [NonSerialized] private string lastError = string.Empty;

        public MoonpalaceBoundaryPreviewViewModel ViewModel => viewModel;
        public MoonpalaceBoundaryPreviewReport LastReport => viewModel == null ? null : viewModel.CurrentReport;
        public string LastError => lastError;

        [MenuItem(MenuPath)]
        public static MoonpalaceBoundaryPreviewWindow Open()
        {
            var window = GetWindow<MoonpalaceBoundaryPreviewWindow>();
            window.titleContent = new GUIContent("Boundary Preview");
            window.minSize = new Vector2(980f, 680f);
            if (window.viewModel == null) window.RefreshFromAuthoring();
            window.Show();
            return window;
        }

        public void UseViewModel(MoonpalaceBoundaryPreviewViewModel value)
        {
            viewModel = value;
            selectedCell = null;
            lastError = string.Empty;
            Repaint();
        }

        public bool RefreshFromAuthoring()
        {
            try
            {
                viewModel = MoonpalaceBoundaryPreviewViewModel.LoadApprovedAuthoring(true);
                selectedCell = null;
                lastError = viewModel.CurrentReport.HasCoverageReport
                    ? string.Empty
                    : viewModel.CurrentReport.Issues.FirstOrDefault()?.Message ?? "Coverage report unavailable.";
                Repaint();
                return viewModel.CurrentReport.HasCoverageReport;
            }
            catch (Exception exception)
            {
                lastError = exception.Message;
                Repaint();
                return false;
            }
        }

        public bool CopyStableDigest()
        {
            if (LastReport == null || string.IsNullOrEmpty(LastReport.StableDigest)) return false;
            EditorGUIUtility.systemCopyBuffer = LastReport.StableDigest;
            return true;
        }

        public bool CopyReportSummary()
        {
            if (LastReport == null || string.IsNullOrEmpty(LastReport.Summary)) return false;
            EditorGUIUtility.systemCopyBuffer = LastReport.Summary;
            return true;
        }

        public bool TrySelectCandidate(int candidateIndex)
        {
            if (viewModel == null) return false;
            var selected = viewModel.SelectCandidateIndex(candidateIndex);
            selectedCell = null;
            Repaint();
            return selected;
        }

        private void OnEnable()
        {
            if (viewModel == null)
            {
                viewModel = MoonpalaceBoundaryPreviewViewModel.LoadApprovedAuthoring();
                lastError = viewModel.CurrentReport.HasCoverageReport
                    ? string.Empty
                    : viewModel.CurrentReport.Issues.FirstOrDefault()?.Message ?? "Coverage report unavailable.";
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            if (viewModel == null)
            {
                EditorGUILayout.HelpBox("Boundary preview view model is unavailable.", MessageType.Error);
                return;
            }

            var report = viewModel.CurrentReport;
            DrawSourceSummary(report);
            DrawSelectionControls(report);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(390f)))
                {
                    DrawPairMatrix(report);
                    DrawCandidateList(report);
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    DrawGrid(report);
                    DrawSelectedCell();
                }
            }
            DrawIssues(report);
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Moonpalace Boundary Coverage Preview", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    RefreshFromAuthoring();
                using (new EditorGUI.DisabledScope(LastReport == null || string.IsNullOrEmpty(LastReport.StableDigest)))
                {
                    if (GUILayout.Button("Copy Digest", EditorStyles.toolbarButton, GUILayout.Width(88f)))
                        CopyStableDigest();
                    if (GUILayout.Button("Copy Summary", EditorStyles.toolbarButton, GUILayout.Width(96f)))
                        CopyReportSummary();
                }
            }
            EditorGUILayout.HelpBox(
                "Read-only MAP08_12 coverage projection. Refresh and clipboard commands never write project assets.",
                MessageType.Info);
            if (!string.IsNullOrEmpty(lastError)) EditorGUILayout.HelpBox(lastError, MessageType.Error);
        }

        private static void DrawSourceSummary(MoonpalaceBoundaryPreviewReport report)
        {
            if (!report.HasCoverageReport)
            {
                EditorGUILayout.HelpBox("No coverage report available.", MessageType.Error);
                return;
            }
            var source = report.CoverageReport;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    source.Accepted ? "Coverage: ACCEPTED" : "Coverage: REJECTED",
                    EditorStyles.boldLabel,
                    GUILayout.Width(180f));
                EditorGUILayout.LabelField("Pairs", source.PairReportCount.ToString(), GUILayout.Width(100f));
                EditorGUILayout.LabelField(
                    "C/M/T/S",
                    source.CandidateCountTotal + "/" + source.MicrochunkCountTotal + "/" +
                    source.TileRowCountTotal + "/" + source.SocketRowCountTotal,
                    GUILayout.Width(230f));
                EditorGUILayout.LabelField("Issues", source.Issues.Count.ToString(), GUILayout.Width(90f));
            }
            EditorGUILayout.SelectableLabel("Digest: " + source.StableDigest, GUILayout.Height(18f));
            EditorGUILayout.SelectableLabel(
                "Authoring manifest: " + source.AuthoringManifestSha256,
                GUILayout.Height(18f));
        }

        private void DrawSelectionControls(MoonpalaceBoundaryPreviewReport report)
        {
            if (report.PairRows.Count == 0) return;
            EditorGUILayout.Space(3f);
            using (new EditorGUILayout.HorizontalScope())
            {
                var pairLabels = report.PairRows.Select(value => value.PairRuleId).ToArray();
                var pairIndex = Array.FindIndex(pairLabels, value =>
                    string.Equals(value, viewModel.Selection.PairRuleId, StringComparison.Ordinal));
                pairIndex = Mathf.Max(0, pairIndex);
                var nextPair = EditorGUILayout.Popup("Pair", pairIndex, pairLabels, GUILayout.Width(330f));
                if (nextPair != pairIndex)
                {
                    viewModel.SelectPair(pairLabels[nextPair]);
                    selectedCell = null;
                    report = viewModel.CurrentReport;
                }

                var orientationIndex = viewModel.Selection.OrientationToken ==
                                       MoonpalaceBoundaryPreviewSelection.VerticalToken ? 1 : 0;
                var nextOrientation = GUILayout.Toolbar(
                    orientationIndex,
                    new[] { "Horizontal", "Vertical" },
                    GUILayout.Width(190f));
                if (nextOrientation != orientationIndex)
                {
                    viewModel.SelectOrientation(nextOrientation == 0
                        ? MoonpalaceBoundaryPreviewSelection.HorizontalToken
                        : MoonpalaceBoundaryPreviewSelection.VerticalToken);
                    selectedCell = null;
                    report = viewModel.CurrentReport;
                }

                var directionIndex = viewModel.Selection.Direction ==
                                     MoonpalaceBoundaryRequestDirection.Forward ? 0 : 1;
                var nextDirection = GUILayout.Toolbar(
                    directionIndex,
                    new[] { "A -> B", "B -> A" },
                    GUILayout.Width(170f));
                if (nextDirection != directionIndex)
                {
                    viewModel.SelectDirection(nextDirection == 0
                        ? MoonpalaceBoundaryRequestDirection.Forward
                        : MoonpalaceBoundaryRequestDirection.Reverse);
                    selectedCell = null;
                    report = viewModel.CurrentReport;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var profiles = viewModel.AvailableProfiles.ToArray();
                if (profiles.Length > 0)
                {
                    var profileIndex = Array.FindIndex(profiles, value =>
                        string.Equals(value, viewModel.Selection.ProfileId, StringComparison.Ordinal));
                    profileIndex = Mathf.Max(0, profileIndex);
                    var nextProfile = EditorGUILayout.Popup("Profile", profileIndex, profiles, GUILayout.Width(350f));
                    if (nextProfile != profileIndex)
                    {
                        viewModel.SelectProfile(profiles[nextProfile]);
                        selectedCell = null;
                        report = viewModel.CurrentReport;
                    }
                }
                GUILayout.Label("Overlays", GUILayout.Width(60f));
                DrawOverlayToggle("FG", MoonpalaceBoundaryPreviewOverlayToggle.Foreground);
                DrawOverlayToggle("BG", MoonpalaceBoundaryPreviewOverlayToggle.Background);
                DrawOverlayToggle("Route", MoonpalaceBoundaryPreviewOverlayToggle.Route);
                DrawOverlayToggle("Sockets", MoonpalaceBoundaryPreviewOverlayToggle.Sockets);
                DrawOverlayToggle("Warnings", MoonpalaceBoundaryPreviewOverlayToggle.Warnings);
                DrawOverlayToggle("Boundary", MoonpalaceBoundaryPreviewOverlayToggle.BoundaryLayer);
                DrawOverlayToggle("Issues", MoonpalaceBoundaryPreviewOverlayToggle.Issues);
            }
        }

        private void DrawOverlayToggle(string label, MoonpalaceBoundaryPreviewOverlayToggle toggle)
        {
            var enabled = (viewModel.Overlays & toggle) == toggle;
            var next = GUILayout.Toggle(enabled, label, "Button");
            if (next != enabled)
            {
                viewModel.SetOverlay(toggle, next);
                selectedCell = null;
            }
        }

        private void DrawPairMatrix(MoonpalaceBoundaryPreviewReport report)
        {
            EditorGUILayout.LabelField("Canonical pair matrix", EditorStyles.boldLabel);
            pairScroll = EditorGUILayout.BeginScrollView(pairScroll, GUILayout.Height(185f));
            foreach (var pair in report.PairRows)
            {
                var selected = string.Equals(
                    pair.PairRuleId, viewModel.Selection.PairRuleId, StringComparison.Ordinal);
                using (new EditorGUILayout.VerticalScope(selected ? "SelectionRect" : "box"))
                {
                    EditorGUILayout.LabelField(pair.PairRuleId + "  " + pair.CountDisplay, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(pair.ForwardTransition + " | " + pair.ReverseTransition);
                    EditorGUILayout.LabelField(pair.OrientationDisplay + " | " + pair.ProfileDisplay);
                    EditorGUILayout.LabelField(pair.RouteRequirement + " | " + pair.EdgeSignatureDisplay);
                    EditorGUILayout.LabelField(pair.CoverageState + " | issues=" + pair.IssueCount);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawCandidateList(MoonpalaceBoundaryPreviewReport report)
        {
            EditorGUILayout.LabelField("Candidates (invalid and filtered rows remain visible)", EditorStyles.boldLabel);
            candidateScroll = EditorGUILayout.BeginScrollView(candidateScroll, GUILayout.Height(260f));
            foreach (var candidate in report.CandidateRows)
            {
                using (new EditorGUILayout.VerticalScope("box"))
                using (new EditorGUI.DisabledScope(!candidate.Enabled))
                {
                    var selected = report.SelectedCandidate != null &&
                                   report.SelectedCandidate.SourceIndex == candidate.SourceIndex;
                    if (GUILayout.Toggle(selected, candidate.CandidateId, "Button") && !selected)
                        TrySelectCandidate(candidate.SourceIndex);
                    EditorGUILayout.LabelField(candidate.ProfileId + " / " + candidate.OrientationToken);
                    EditorGUILayout.LabelField(candidate.ForwardTransition + " | " + candidate.ReverseTransition);
                    EditorGUILayout.LabelField(candidate.RouteRequirement + " | " + candidate.EdgeSignature);
                    EditorGUILayout.LabelField(candidate.TransformDirection + " | mirror=" + candidate.MirrorState);
                    EditorGUILayout.LabelField(
                        "microchunk=" + candidate.SourceMicrochunkId + " | catalog=" + candidate.SourceCatalogRowId);
                    if (!candidate.Enabled)
                        EditorGUILayout.HelpBox(candidate.DisabledReason, MessageType.None);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawGrid(MoonpalaceBoundaryPreviewReport report)
        {
            EditorGUILayout.LabelField("Selected candidate 12 x 8 preview", EditorStyles.boldLabel);
            if (report.SelectedCandidate == null)
            {
                EditorGUILayout.HelpBox("Select an enabled candidate.", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField(
                report.SelectedCandidate.SelectedTransition + " | " +
                report.SelectedCandidate.TransformDirection + " | mirror=" + report.SelectedCandidate.MirrorState);
            for (var y = 7; y >= 0; y--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(y.ToString(), GUILayout.Width(16f));
                    for (var x = 0; x < 12; x++)
                    {
                        var cell = report.Cells.FirstOrDefault(value => value.X == x && value.Y == y);
                        if (cell == null)
                        {
                            GUILayout.Box("?", GUILayout.Width(42f), GUILayout.Height(28f));
                            continue;
                        }
                        var previous = GUI.backgroundColor;
                        GUI.backgroundColor = ColorFor(cell);
                        if (GUILayout.Button(CellLabel(cell), GUILayout.Width(42f), GUILayout.Height(28f)))
                            selectedCell = cell;
                        GUI.backgroundColor = previous;
                    }
                }
            }
        }

        private void DrawSelectedCell()
        {
            if (selectedCell == null) return;
            EditorGUILayout.LabelField("Cell detail", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Preview/source",
                selectedCell.X + "," + selectedCell.Y + " / " +
                selectedCell.SourceX + "," + selectedCell.SourceY);
            EditorGUILayout.LabelField("Foreground", selectedCell.ForegroundCode);
            EditorGUILayout.LabelField("Background", selectedCell.BackgroundCode);
            EditorGUILayout.LabelField("Marker", selectedCell.MarkerCode);
            EditorGUILayout.LabelField("Overlays", selectedCell.OverlaySummary);
        }

        private void DrawIssues(MoonpalaceBoundaryPreviewReport report)
        {
            EditorGUILayout.LabelField("Issues: " + report.Issues.Count, EditorStyles.boldLabel);
            issueScroll = EditorGUILayout.BeginScrollView(issueScroll, GUILayout.Height(100f));
            foreach (var issue in report.Issues.Take(200))
            {
                EditorGUILayout.HelpBox(
                    issue.ToString(),
                    issue.IsError ? MessageType.Error :
                    issue.Severity == MoonpalaceBoundaryPreviewIssueSeverity.Warning
                        ? MessageType.Warning
                        : MessageType.Info);
            }
            EditorGUILayout.EndScrollView();
        }

        private static string CellLabel(MoonpalaceBoundaryPreviewCell cell)
        {
            if (cell.ShowIssue) return "!";
            if (cell.ShowSocket) return "S";
            if (cell.ShowRoute) return "R";
            if (cell.ShowBoundaryLayer) return "B";
            if (cell.ShowWarning) return "W";
            return cell.X + "," + cell.Y;
        }

        private static Color ColorFor(MoonpalaceBoundaryPreviewCell cell)
        {
            if (cell.ShowIssue) return new Color(0.85f, 0.28f, 0.28f);
            if (cell.ShowSocket) return new Color(0.28f, 0.62f, 0.92f);
            if (cell.ShowRoute) return new Color(0.95f, 0.78f, 0.28f);
            if (cell.ShowBoundaryLayer) return new Color(0.72f, 0.42f, 0.90f);
            if (cell.ShowWarning) return new Color(0.78f, 0.61f, 0.25f);
            if (cell.ShowForeground) return new Color(0.38f, 0.68f, 0.42f);
            if (cell.ShowBackground) return new Color(0.35f, 0.48f, 0.68f);
            return new Color(0.38f, 0.38f, 0.38f);
        }
    }
}
