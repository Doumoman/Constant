using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using StarNight.Map.WorldGeneration.Tooling;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.WorldGeneration.Tooling
{
    /// <summary>Read-only failure browsing and RequestOnly replay-authoring preview.</summary>
    public sealed class GeneratedReplayAuthoringWindow : EditorWindow
    {
        public const string MenuPath =
            "Tools/MapDesign/Replay Authoring Integration HUD and Export";
        public const string WindowTitle = "Replay Authoring Integration HUD and Export";

        private static readonly string[] SourceFilterLabels =
            { "All", "ActualFailureBundle", "FocusedFixture", "MissingData" };
        private static int openInvocationCount;
        private static int forbiddenExecutionInvocationCount;

        [SerializeField] private string failureSearch = string.Empty;
        [SerializeField] private int sourceFilterIndex;
        [SerializeField] private string selectedFailureRecordId = string.Empty;
        [NonSerialized] private GeneratedReplayAuthoringReadOnlySample sample;
        [NonSerialized] private Vector2 pageScroll;
        [NonSerialized] private Vector2 failureScroll;
        [NonSerialized] private int viewStateMutationCount;
        [NonSerialized] private int clipboardActionCount;
        [NonSerialized] private int requestOnlyCreationCount;
        [NonSerialized] private int seedBundleExportCount;

        public static int OpenInvocationCount => openInvocationCount;
        public static int ForbiddenExecutionInvocationCount => forbiddenExecutionInvocationCount;
        public int ViewStateMutationCount => viewStateMutationCount;
        public int ClipboardActionCount => clipboardActionCount;
        public int RequestOnlyCreationCount => requestOnlyCreationCount;
        public int SeedBundleExportCount => seedBundleExportCount;
        public string FailureSearch => failureSearch;
        public string SourceFilter => SourceFilterLabels[sourceFilterIndex];
        public GeneratedFailureBrowserIndex FailureBrowser => sample?.FailureBrowser;
        public GeneratedReplayAuthoringRequest ReplayRequest => sample?.ReplayRequest;
        public GeneratedMap07FixedGeneratedSplit FixedGeneratedSplit =>
            sample?.FixedGeneratedSplit;
        public GeneratedMap08BoundaryLinkCatalog BoundaryLinks => sample?.BoundaryLinks;
        public GeneratedRuntimeDebugHudState RuntimeHud => sample?.RuntimeHud;
        public GeneratedSeedBundleExportSample SeedBundle => sample?.SeedBundle;
        public GeneratedFailureBrowserRecord SelectedFailure => ResolveSelectedFailure();
        public IReadOnlyList<GeneratedFailureBrowserRecord> VisibleFailures =>
            new ReadOnlyCollection<GeneratedFailureBrowserRecord>(FilterFailures().ToArray());

        [MenuItem(MenuPath)]
        public static GeneratedReplayAuthoringWindow Open()
        {
            openInvocationCount++;
            var window = GetWindow<GeneratedReplayAuthoringWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(860f, 720f);
            window.EnsureSample();
            window.Show();
            return window;
        }

        public static void ResetInvocationDiagnostics()
        {
            openInvocationCount = 0;
            forbiddenExecutionInvocationCount = 0;
        }

        public void ReloadReadOnlySources()
        {
            sample = GeneratedReplayAuthoringSamplePublisher.CreateReadOnlySample(null,
                string.Empty);
            EnsureSelectedFailure();
            Repaint();
        }

        public void SetFailureSearch(string value)
        {
            var next = value ?? string.Empty;
            if (string.Equals(failureSearch, next, StringComparison.Ordinal)) return;
            failureSearch = next;
            viewStateMutationCount++;
            EnsureSelectedFailure();
            Repaint();
        }

        public void SetSourceFilter(string token)
        {
            var next = string.IsNullOrWhiteSpace(token) ? "All" : token;
            var index = Array.IndexOf(SourceFilterLabels, next);
            if (index < 0) throw new ArgumentException("Unknown failure source filter.",
                nameof(token));
            if (sourceFilterIndex == index) return;
            sourceFilterIndex = index;
            viewStateMutationCount++;
            EnsureSelectedFailure();
            Repaint();
        }

        public void SelectFailure(string failureRecordId)
        {
            EnsureSample();
            var record = FailureBrowser.Records.FirstOrDefault(value => string.Equals(
                value.FailureRecordId, failureRecordId, StringComparison.Ordinal));
            if (record == null) throw new ArgumentException("Unknown failure record id.",
                nameof(failureRecordId));
            if (string.Equals(selectedFailureRecordId, record.FailureRecordId,
                StringComparison.Ordinal)) return;
            selectedFailureRecordId = record.FailureRecordId;
            viewStateMutationCount++;
            Repaint();
        }

        public GeneratedReplayAuthoringRequest CreateRequestOnlyData()
        {
            EnsureSample();
            requestOnlyCreationCount++;
            return ReplayRequest;
        }

        public string ExportSeedBundleSample()
        {
            var path = GeneratedReplayAuthoringSamplePublisher.ExportSeedBundleSample(null, true);
            seedBundleExportCount++;
            return path;
        }

        public string CopyFailureId()
        {
            var value = RequireSelectedFailure().FailureRecordId;
            EditorGUIUtility.systemCopyBuffer = value;
            clipboardActionCount++;
            return value;
        }

        public string CopyReplayRequestId()
        {
            EnsureSample();
            var value = ReplayRequest.RequestId;
            EditorGUIUtility.systemCopyBuffer = value;
            clipboardActionCount++;
            return value;
        }

        public string CopySeedBundlePath()
        {
            var value = Path.Combine(GeneratedReplayAuthoringSamplePublisher.OutputDirectory(null),
                GeneratedReplayAuthoringSamplePublisher.SeedBundleFileName);
            EditorGUIUtility.systemCopyBuffer = value;
            clipboardActionCount++;
            return value;
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = new Vector2(860f, 720f);
            EnsureSample();
        }

        private void EnsureSample()
        {
            sourceFilterIndex = Mathf.Clamp(sourceFilterIndex, 0,
                SourceFilterLabels.Length - 1);
            if (sample == null) ReloadReadOnlySources();
        }

        private IEnumerable<GeneratedFailureBrowserRecord> FilterFailures()
        {
            EnsureSample();
            GeneratedFailureSourceKind? kind = null;
            if (sourceFilterIndex > 0)
                kind = (GeneratedFailureSourceKind)(sourceFilterIndex - 1);
            return FailureBrowser.Filter(failureSearch, kind);
        }

        private void EnsureSelectedFailure()
        {
            if (sample == null) return;
            var visible = FilterFailures().ToArray();
            if (visible.Length == 0)
            {
                selectedFailureRecordId = string.Empty;
                return;
            }
            if (!visible.Any(value => string.Equals(value.FailureRecordId,
                selectedFailureRecordId, StringComparison.Ordinal)))
                selectedFailureRecordId = visible[0].FailureRecordId;
        }

        private GeneratedFailureBrowserRecord ResolveSelectedFailure()
        {
            if (sample == null || string.IsNullOrWhiteSpace(selectedFailureRecordId)) return null;
            return FailureBrowser.Records.FirstOrDefault(value => string.Equals(
                value.FailureRecordId, selectedFailureRecordId, StringComparison.Ordinal));
        }

        private GeneratedFailureBrowserRecord RequireSelectedFailure() =>
            ResolveSelectedFailure() ?? throw new InvalidOperationException(
                "A failure browser record is not selected.");

        private void OnGUI()
        {
            EnsureSample();
            pageScroll = EditorGUILayout.BeginScrollView(pageScroll);
            EditorGUILayout.LabelField("Failure browser", EditorStyles.boldLabel);
            var nextSearch = EditorGUILayout.TextField("Failure search/filter", failureSearch);
            if (!string.Equals(nextSearch, failureSearch, StringComparison.Ordinal))
                SetFailureSearch(nextSearch);
            var nextFilter = EditorGUILayout.Popup("Actual/fixture/missing source",
                sourceFilterIndex, SourceFilterLabels);
            if (nextFilter != sourceFilterIndex)
                SetSourceFilter(SourceFilterLabels[nextFilter]);
            DrawFailureList();
            DrawFailureDetails();
            DrawReplayPreview();
            DrawFixedGeneratedOrigins();
            DrawBoundaryLinks();
            DrawHudPreview();
            DrawActions();
            EditorGUILayout.EndScrollView();
        }

        private void DrawFailureList()
        {
            EditorGUILayout.LabelField("Failure record list", EditorStyles.boldLabel);
            failureScroll = EditorGUILayout.BeginScrollView(failureScroll,
                GUILayout.Height(110f));
            foreach (var record in FilterFailures())
            {
                var label = record.FailureRecordId + " / " + record.SourceKind;
                using (new EditorGUI.DisabledScope(string.Equals(record.FailureRecordId,
                    selectedFailureRecordId, StringComparison.Ordinal)))
                    if (GUILayout.Button(label, EditorStyles.miniButton))
                        SelectFailure(record.FailureRecordId);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawFailureDetails()
        {
            EditorGUILayout.LabelField("Selected failure details", EditorStyles.boldLabel);
            var record = SelectedFailure;
            if (record == null)
            {
                EditorGUILayout.HelpBox("No matching failure record.", MessageType.Info);
                return;
            }
            Pair("Source", record.SourceKind + " / " + record.SourcePath);
            Pair("Owner / category", record.OwnerTask + " / " + record.FailureCategory);
            Pair("Validation", record.ValidationErrorId);
            Pair("Selection", record.NavigationSelectionPath);
            Pair("Actual available", record.ActualFailureAvailable.ToString());
            Pair("Fixture / missing", record.FixtureKind + " / " + record.MissingReason);
        }

        private void DrawReplayPreview()
        {
            EditorGUILayout.LabelField("Replay authoring request preview",
                EditorStyles.boldLabel);
            Pair("Request id / state", ReplayRequest.RequestId + " / " +
                ReplayRequest.ExecutionState);
            Pair("Action", ReplayRequest.RequestedAction.ToString());
            Pair("Target", ReplayRequest.ValidationErrorId + " / " +
                ReplayRequest.SelectionPath);
        }

        private void DrawFixedGeneratedOrigins()
        {
            EditorGUILayout.LabelField("MAP07 fixed/generated origin panel",
                EditorStyles.boldLabel);
            foreach (var record in FixedGeneratedSplit.Records)
                Pair(record.RecordId, record.Origin + " / " + record.SourceIdentity);
        }

        private void DrawBoundaryLinks()
        {
            EditorGUILayout.LabelField("MAP08 boundary link panel", EditorStyles.boldLabel);
            foreach (var link in BoundaryLinks.Links)
                Pair(link.PairId, link.CandidateId + " / " + link.ProjectionId + " / " +
                    link.SocketId);
        }

        private void DrawHudPreview()
        {
            EditorGUILayout.LabelField("Runtime HUD state preview", EditorStyles.boldLabel);
            foreach (var line in new GeneratedRuntimeDebugHud(RuntimeHud).FormatLines())
                EditorGUILayout.LabelField(line);
            Pair("Runtime mutation count", RuntimeHud.RuntimeMutationCount.ToString());
        }

        private void DrawActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create RequestOnly data")) CreateRequestOnlyData();
                if (GUILayout.Button("Seed bundle export sample")) ExportSeedBundleSample();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy failure id")) CopyFailureId();
                if (GUILayout.Button("Copy replay request id")) CopyReplayRequestId();
                if (GUILayout.Button("Copy seed bundle path")) CopySeedBundlePath();
            }
            if (GUILayout.Button("Open generated MAP20_05 output folder"))
            {
                var output = GeneratedReplayAuthoringSamplePublisher.OutputDirectory(null);
                if (Directory.Exists(output)) EditorUtility.RevealInFinder(output);
            }
        }

        private static void Pair(string label, string value) =>
            EditorGUILayout.LabelField(label, value ?? string.Empty);
    }
}
