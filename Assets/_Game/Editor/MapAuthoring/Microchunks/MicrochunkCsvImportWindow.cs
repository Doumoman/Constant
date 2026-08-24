using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkCsvImportWindow : EditorWindow
    {
        [NonSerialized] private string selectedMicrochunkId = string.Empty;
        [NonSerialized] private MicrochunkCsvImportResult lastResult;
        [NonSerialized] private string lastError = string.Empty;
        [NonSerialized] private Vector2 scrollPosition;

        public string SelectedMicrochunkId => selectedMicrochunkId;
        public MicrochunkCsvImportResult LastResult => lastResult;
        public MicrochunkAuthoringGridViewModel ImportedGrid =>
            lastResult == null ? null : lastResult.GridViewModel;
        public MicrochunkSocketAndSlotEditorViewModel ImportedSocketAndSlotState =>
            lastResult == null ? null : lastResult.EditorState;

        [MenuItem("Tools/Map/Microchunk CSV Import")]
        public static MicrochunkCsvImportWindow Open()
        {
            var window = GetWindow<MicrochunkCsvImportWindow>();
            window.titleContent = new GUIContent("Microchunk CSV Import");
            window.minSize = new Vector2(660f, 420f);
            window.Show();
            return window;
        }

        public MicrochunkCsvImportResult Import(
            MicrochunkCsvImportSource source,
            string microchunkId)
        {
            selectedMicrochunkId = microchunkId ?? string.Empty;
            lastResult = new MicrochunkCsvImporter().Import(
                source,
                new MicrochunkCsvImportRequest(selectedMicrochunkId));
            lastError = string.Empty;
            Repaint();
            return lastResult;
        }

        public MicrochunkCsvImportResult ImportProjectAuthoringCsv(string microchunkId)
        {
            return Import(MicrochunkCsvImportSource.FromProjectAuthoringCsv(), microchunkId);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Read-only Authoring CSV Import", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Select exactly one microchunk ID. Import hydrates detached in-memory grid, socket, band, and slot state only.",
                MessageType.Info);

            selectedMicrochunkId = EditorGUILayout.TextField("Microchunk ID", selectedMicrochunkId);
            if (GUILayout.Button("Import Authoring CSV (Read Only)"))
            {
                TryImportProjectSource();
            }

            if (!string.IsNullOrEmpty(lastError))
            {
                EditorGUILayout.HelpBox(lastError, MessageType.Error);
            }
            if (lastResult == null) return;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                lastResult.Success ? "Import succeeded" : "Import failed",
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Grid cells", lastResult.GridState.CellCount.ToString());
            EditorGUILayout.LabelField(
                "Sockets / Bands / Slots",
                string.Format(
                    "{0} / {1} / {2}",
                    lastResult.EditorState.SocketAuthoring.Sockets.Count,
                    lastResult.EditorState.SocketAuthoring.Bands.Count,
                    lastResult.EditorState.ObjectSlotAuthoring.Rows.Count));
            EditorGUILayout.LabelField("Variant metadata rows", lastResult.Variants.Count.ToString());
            if (lastResult.HasValidationFeedback)
            {
                EditorGUILayout.LabelField(
                    "Validation feedback issues",
                    lastResult.ValidationFeedback.IssueCount.ToString());
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (var issue in lastResult.Issues.Take(200))
            {
                EditorGUILayout.HelpBox(
                    issue.ToString(),
                    issue.IsError ? MessageType.Error : MessageType.Warning);
            }
            EditorGUILayout.EndScrollView();
        }

        private void TryImportProjectSource()
        {
            try
            {
                ImportProjectAuthoringCsv(selectedMicrochunkId);
            }
            catch (Exception exception)
            {
                lastResult = null;
                lastError = exception.Message;
            }
        }
    }
}
