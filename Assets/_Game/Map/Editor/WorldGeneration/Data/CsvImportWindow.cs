using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StarNight.Map.WorldGeneration.Data;
using UnityEditor;
using UnityEngine;

namespace StarNight.Map.Editor.WorldGeneration.Data
{
    public sealed class CsvImportWindow : EditorWindow
    {
        private static readonly string[] SeverityFilters = { "ALL", "ERROR", "WARNING" };

        private CsvImportPipeline pipeline;
        private CsvImportWindowState state;
        private CsvImportNavigation navigation;
        private Vector2 fileScroll;
        private Vector2 issueScroll;
        private string search = string.Empty;
        private int severityFilter;
        private int selectedIssue = -1;
        private string navigationMessage = string.Empty;

        [MenuItem("Tools/Star Night/Map/CSV Import")]
        public static void Open()
        {
            var window = GetWindow<CsvImportWindow>();
            window.titleContent = new GUIContent("CSV Import");
            window.minSize = new Vector2(1040f, 680f);
            window.Focus();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("CSV Import");
            minSize = new Vector2(1040f, 680f);
            pipeline = new CsvImportPipeline();
            navigation = new CsvImportNavigation();
            state = new CsvImportWindowState(pipeline.CreateNotRunResult());
            selectedIssue = -1;
            navigationMessage = string.Empty;
        }

        private void OnGUI()
        {
            EnsureState();
            DrawToolbar();
            DrawSummary();
            DrawFileTable();
            DrawIssueTable();
            DrawNavigationActions();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            using (new EditorGUI.DisabledScope(state.IsRunning))
            {
                if (GUILayout.Button("Reimport All 50 CSV", EditorStyles.toolbarButton,
                        GUILayout.Width(150f)))
                {
                    BeginReimport();
                }
            }

            var reportExists = File.Exists(GetReportFullPath());
            using (new EditorGUI.DisabledScope(!reportExists))
            {
                if (GUILayout.Button("Open Report", EditorStyles.toolbarButton,
                        GUILayout.Width(90f)))
                {
                    EditorUtility.OpenWithDefaultApp(GetReportFullPath());
                }
            }

            GUILayout.Space(12f);
            GUILayout.Label("Search", GUILayout.Width(46f));
            search = GUILayout.TextField(
                search ?? string.Empty,
                EditorStyles.toolbarTextField,
                GUILayout.Width(220f));
            severityFilter = GUILayout.Toolbar(
                severityFilter,
                SeverityFilters,
                EditorStyles.toolbarButton,
                GUILayout.Width(225f));
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                state.IsRunning
                    ? state.Stage + " " + Mathf.RoundToInt(state.Progress * 100f) + "%"
                    : state.LastResult.Stage,
                EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSummary()
        {
            var result = state.LastResult;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(result.Published ? "PUBLISHED" : "NOT PUBLISHED", BoldStatusStyle());
            GUILayout.Space(16f);
            GUILayout.Label(
                "Version " + result.PreviousVersion + " → " + result.CurrentVersion,
                GUILayout.Width(165f));
            GUILayout.Label(
                "Errors " + result.ErrorCount + "  Warnings " + result.WarningCount,
                GUILayout.Width(190f));
            GUILayout.Label(
                "Report: " + (result.ReportWriteSucceeded ? "WRITTEN" : "NOT WRITTEN"),
                GUILayout.Width(165f));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(
                "Global content hash",
                string.IsNullOrEmpty(result.CurrentContentHash)
                    ? "—"
                    : result.CurrentContentHash,
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                "Report path",
                CsvImportReportFileWriter.ReportProjectRelativePath,
                EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(result.ReportWriteError))
            {
                EditorGUILayout.HelpBox(result.ReportWriteError, MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFileTable()
        {
            EditorGUILayout.LabelField("Files (fixed 50)", EditorStyles.boldLabel);
            DrawFileHeader();
            var files = CsvImportWindowState.FilterFiles(
                state.LastResult.Files,
                search,
                SeverityFilters[Mathf.Clamp(severityFilter, 0, SeverityFilters.Length - 1)]);
            fileScroll = EditorGUILayout.BeginScrollView(fileScroll, GUILayout.Height(230f));
            foreach (var file in files)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(file.Category, GUILayout.Width(78f));
                GUILayout.Label(file.FileName, GUILayout.Width(220f));
                GUILayout.Label(file.State, GUILayout.Width(72f));
                GUILayout.Label(file.RowCount.ToString(), GUILayout.Width(48f));
                GUILayout.Label(file.ErrorCount.ToString(), GUILayout.Width(42f));
                GUILayout.Label(file.WarningCount.ToString(), GUILayout.Width(42f));
                GUILayout.Label(file.HadUtf8Bom ? "BOM" : "—", GUILayout.Width(42f));
                GUILayout.Label(
                    string.IsNullOrEmpty(file.RawSha256) ? "—" : file.RawSha256,
                    EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawFileHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("Category", EditorStyles.miniBoldLabel, GUILayout.Width(78f));
            GUILayout.Label("File", EditorStyles.miniBoldLabel, GUILayout.Width(220f));
            GUILayout.Label("State", EditorStyles.miniBoldLabel, GUILayout.Width(72f));
            GUILayout.Label("Rows", EditorStyles.miniBoldLabel, GUILayout.Width(48f));
            GUILayout.Label("Err", EditorStyles.miniBoldLabel, GUILayout.Width(42f));
            GUILayout.Label("Warn", EditorStyles.miniBoldLabel, GUILayout.Width(42f));
            GUILayout.Label("UTF-8", EditorStyles.miniBoldLabel, GUILayout.Width(42f));
            GUILayout.Label("Raw file SHA-256 (diagnostic; not content hash)",
                EditorStyles.miniBoldLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawIssueTable()
        {
            EditorGUILayout.LabelField("Issues", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("Severity / Stage", EditorStyles.miniBoldLabel, GUILayout.Width(155f));
            GUILayout.Label("Source", EditorStyles.miniBoldLabel, GUILayout.Width(270f));
            GUILayout.Label("Message / Target", EditorStyles.miniBoldLabel);
            EditorGUILayout.EndHorizontal();

            var issues = CsvImportWindowState.FilterIssues(
                state.LastResult.Issues,
                search,
                SeverityFilters[Mathf.Clamp(severityFilter, 0, SeverityFilters.Length - 1)]);
            issueScroll = EditorGUILayout.BeginScrollView(issueScroll, GUILayout.Height(190f));
            for (var index = 0; index < issues.Count; index++)
            {
                var issue = issues[index];
                var selected = ReferenceEquals(GetSelectedIssue(), issue);
                var source = FormatSource(issue);
                var target = FormatTarget(issue);
                var label = issue.Severity + " / " + issue.Stage + "\n" +
                            source + "\n" + issue.Code + ": " + issue.Message + target;
                var style = selected ? EditorStyles.helpBox : EditorStyles.textArea;
                if (GUILayout.Button(label, style, GUILayout.MinHeight(42f)))
                {
                    selectedIssue = IndexOfIssue(state.LastResult.Issues, issue);
                    navigationMessage = string.Empty;
                    if (Event.current != null && Event.current.clickCount >= 2)
                    {
                        NavigateToSource();
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawNavigationActions()
        {
            var issue = GetSelectedIssue();
            var sourceReason = issue == null || string.IsNullOrEmpty(issue.SourceFile)
                ? "Select an issue with a source file."
                : string.Empty;
            var targetReason = CsvImportNavigation.GetForeignKeyTargetUnavailableReason(
                issue, state.LastResult.RecordIndex);

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(sourceReason.Length != 0))
            {
                if (GUILayout.Button("Go to Source", GUILayout.Width(120f))) NavigateToSource();
            }

            using (new EditorGUI.DisabledScope(targetReason.Length != 0))
            {
                if (GUILayout.Button("Go to FK Target", GUILayout.Width(135f)))
                {
                    var result = navigation.GoToForeignKeyTarget(
                        issue, state.LastResult.RecordIndex);
                    navigationMessage = result.Success ? "Opened FK target." : result.Reason;
                }
            }

            GUILayout.Space(8f);
            GUILayout.Label(
                targetReason.Length == 0 ? navigationMessage : targetReason,
                EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void BeginReimport()
        {
            if (!state.TryBeginRun()) return;
            selectedIssue = -1;
            navigationMessage = string.Empty;
            Repaint();
            EditorApplication.delayCall += ExecuteReimport;
        }

        private void ExecuteReimport()
        {
            if (state == null || !state.IsRunning) return;
            try
            {
                var result = pipeline.Execute((stage, value) =>
                {
                    state.UpdateProgress(stage, value);
                    Repaint();
                });
                state.Complete(result);
            }
            catch (Exception exception)
            {
                var previous = state.LastResult;
                var issues = previous.Issues.Concat(new[]
                {
                    new CsvImportIssue(
                        "WINDOW",
                        CsvImportIssue.ErrorSeverity,
                        "UNEXPECTED_PIPELINE_EXCEPTION",
                        exception.ToString())
                });
                state.Complete(new CsvImportSessionResult(
                    previous.Files,
                    issues,
                    previous.PublishReport,
                    previous.RecordIndex,
                    "COMPLETE",
                    1f,
                    previous.ReportProjectRelativePath,
                    previous.ReportWriteSucceeded,
                    previous.ReportWriteError));
            }

            Repaint();
        }

        private void NavigateToSource()
        {
            var result = navigation.GoToSource(GetSelectedIssue());
            navigationMessage = result.Success ? "Opened source CSV." : result.Reason;
        }

        private CsvImportIssue GetSelectedIssue()
        {
            return selectedIssue >= 0 && selectedIssue < state.LastResult.Issues.Count
                ? state.LastResult.Issues[selectedIssue]
                : null;
        }

        private static int IndexOfIssue(
            IReadOnlyList<CsvImportIssue> issues,
            CsvImportIssue target)
        {
            for (var index = 0; index < issues.Count; index++)
            {
                if (ReferenceEquals(issues[index], target)) return index;
            }

            return -1;
        }

        private static string FormatSource(CsvImportIssue issue)
        {
            if (string.IsNullOrEmpty(issue.SourceFile)) return "—";
            var value = issue.SourceFile;
            if (issue.RecordNumber.HasValue) value += " record " + issue.RecordNumber.Value;
            if (!string.IsNullOrEmpty(issue.SourceField)) value += " / " + issue.SourceField;
            if (issue.Line.HasValue) value += " line " + issue.Line.Value;
            return value;
        }

        private static string FormatTarget(CsvImportIssue issue)
        {
            return string.IsNullOrEmpty(issue.TargetFile)
                ? string.Empty
                : "\nTarget: " + issue.TargetFile + "." + issue.TargetColumn +
                  " = " + issue.TargetValue;
        }

        private static GUIStyle BoldStatusStyle()
        {
            return new GUIStyle(EditorStyles.boldLabel) { fixedWidth = 115f };
        }

        private static string GetReportFullPath()
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(root, CsvImportReportFileWriter.ReportProjectRelativePath
                .Replace('/', Path.DirectorySeparatorChar));
        }

        private void EnsureState()
        {
            if (pipeline == null) pipeline = new CsvImportPipeline();
            if (navigation == null) navigation = new CsvImportNavigation();
            if (state == null) state = new CsvImportWindowState(pipeline.CreateNotRunResult());
        }
    }
}
