using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkCsvExportWindow : EditorWindow
    {
        [NonSerialized] private string selectedMicrochunkId = string.Empty;
        [NonSerialized] private MicrochunkCsvImportResult importedState;
        [NonSerialized] private MicrochunkCsvExportPlan lastPlan;
        [NonSerialized] private MicrochunkCsvExportResult lastResult;
        [NonSerialized] private string lastError = string.Empty;
        [NonSerialized] private Vector2 scrollPosition;

        public string SelectedMicrochunkId => selectedMicrochunkId;
        public MicrochunkCsvImportResult ImportedState => importedState;
        public MicrochunkCsvExportPlan LastPlan => lastPlan;
        public MicrochunkCsvExportResult LastResult => lastResult;

        [MenuItem("Tools/Map/Microchunk CSV Export")]
        public static MicrochunkCsvExportWindow Open()
        {
            var window = GetWindow<MicrochunkCsvExportWindow>();
            window.titleContent = new GUIContent("Microchunk CSV Export");
            window.minSize = new Vector2(700f, 460f);
            window.Show();
            return window;
        }

        public void UseImportedState(MicrochunkCsvImportResult importResult)
        {
            importedState = importResult ?? throw new ArgumentNullException(nameof(importResult));
            selectedMicrochunkId = importResult.Request.SelectedMicrochunkId;
            lastPlan = null;
            lastResult = null;
            lastError = string.Empty;
            Repaint();
        }

        public MicrochunkCsvExportPlan Preflight(
            MicrochunkCsvImportSource source,
            MicrochunkCsvExportRequest request)
        {
            lastPlan = new MicrochunkCsvExporter().BuildPlan(source, request);
            selectedMicrochunkId = request.SelectedMicrochunkId;
            lastResult = null;
            lastError = string.Empty;
            Repaint();
            return lastPlan;
        }

        public MicrochunkCsvExportPlan PreflightImportedState(MicrochunkCsvImportSource source)
        {
            if (importedState == null)
            {
                throw new InvalidOperationException("A detached imported editor state is required.");
            }
            if (!string.Equals(
                    importedState.Request.SelectedMicrochunkId,
                    selectedMicrochunkId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The selected ID must exactly match the detached imported state.");
            }

            return Preflight(source, MicrochunkCsvExportRequest.FromImportResult(importedState));
        }

        public MicrochunkCsvExportResult Execute(
            MicrochunkCsvExportPlan plan,
            string authoringRoot)
        {
            lastResult = new MicrochunkCsvExporter().ApplyPlan(plan, authoringRoot);
            lastPlan = plan;
            lastError = string.Empty;
            Repaint();
            return lastResult;
        }

        public MicrochunkCsvExportResult ExecuteProjectAuthoringPlan(MicrochunkCsvExportPlan plan)
        {
            lastResult = new MicrochunkCsvExporter().ApplyProjectAuthoringPlan(plan);
            lastPlan = plan;
            lastError = string.Empty;
            Repaint();
            return lastResult;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Selected Microchunk Authoring CSV Export", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Preflight is side-effect-free. Authoring CSV is written only by the explicit Execute button.",
                MessageType.Info);

            selectedMicrochunkId = EditorGUILayout.TextField("Microchunk ID", selectedMicrochunkId);
            using (new EditorGUI.DisabledScope(importedState == null))
            {
                if (GUILayout.Button("Preflight Imported State"))
                {
                    TryPreflightProjectSource();
                }
            }
            using (new EditorGUI.DisabledScope(lastPlan == null || !lastPlan.Success))
            {
                if (GUILayout.Button("Execute Authoring CSV Export"))
                {
                    TryExecuteProjectPlan();
                }
            }

            if (!string.IsNullOrEmpty(lastError))
            {
                EditorGUILayout.HelpBox(lastError, MessageType.Error);
            }
            if (lastPlan == null) return;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                lastPlan.Success ? "Preflight succeeded" : "Preflight failed",
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Changed files", lastPlan.ChangedFileCount.ToString());
            EditorGUILayout.LabelField(
                "Removed / inserted rows",
                lastPlan.TotalRemovedRows + " / " + lastPlan.TotalInsertedRows);
            if (lastPlan.HasValidationFeedback)
            {
                EditorGUILayout.LabelField(
                    "Validator feedback issues",
                    lastPlan.ValidationFeedback.IssueCount.ToString());
            }
            if (lastResult != null)
            {
                EditorGUILayout.LabelField(
                    "Last execution",
                    lastResult.Success
                        ? "PASS (" + lastResult.WrittenFileCount + " files)"
                        : "FAILED / originals restored");
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (var file in lastPlan.Files)
            {
                EditorGUILayout.LabelField(
                    file.FileName,
                    file.RemovedRowCount + " removed, " + file.InsertedRowCount + " inserted, " +
                    file.BeforeSha256.Substring(0, 8) + " -> " + file.AfterSha256.Substring(0, 8));
            }
            foreach (var issue in lastPlan.Issues.Take(200))
            {
                EditorGUILayout.HelpBox(
                    issue.ToString(),
                    issue.IsError ? MessageType.Error : MessageType.Warning);
            }
            EditorGUILayout.EndScrollView();
        }

        private void TryPreflightProjectSource()
        {
            try
            {
                PreflightImportedState(MicrochunkCsvImportSource.FromProjectAuthoringCsv());
            }
            catch (Exception exception)
            {
                lastPlan = null;
                lastResult = null;
                lastError = exception.Message;
            }
        }

        private void TryExecuteProjectPlan()
        {
            try
            {
                ExecuteProjectAuthoringPlan(lastPlan);
            }
            catch (Exception exception)
            {
                lastResult = null;
                lastError = exception.Message;
            }
        }
    }
}
