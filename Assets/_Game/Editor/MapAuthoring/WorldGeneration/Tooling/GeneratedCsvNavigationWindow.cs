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
    /// <summary>Read-only search, copy, and inspector-selection UI for MAP20_04.</summary>
    public sealed class GeneratedCsvNavigationWindow : EditorWindow
    {
        public const string MenuPath = "Tools/MapDesign/CSV Navigation and Validation Jump";
        public const string WindowTitle = "CSV Navigation and Validation Jump";

        private static readonly string[] KindFilterLabels = new[] { "All" }.Concat(Enum
            .GetValues(typeof(GeneratedValidationJumpTargetKind))
            .Cast<GeneratedValidationJumpTargetKind>().OrderBy(value => (int)value)
            .Select(value => value.ToString())).ToArray();
        private static readonly string[] AvailabilityFilterLabels =
            { "All", "Available", "MissingData" };
        private static int openInvocationCount;
        private static int externalActionInvocationCount;

        [SerializeField] private string validationErrorIdSearch = string.Empty;
        [SerializeField] private int kindFilterIndex;
        [SerializeField] private int availabilityFilterIndex;
        [SerializeField] private string selectedValidationErrorId = string.Empty;
        [NonSerialized] private GeneratedCsvNavigationReadOnlySample sample;
        [NonSerialized] private Vector2 pageScroll;
        [NonSerialized] private Vector2 resultScroll;
        [NonSerialized] private int viewStateMutationCount;
        [NonSerialized] private int clipboardActionCount;
        [NonSerialized] private int inspectorSelectionJumpCount;

        public static int OpenInvocationCount => openInvocationCount;
        public static int ExternalActionInvocationCount => externalActionInvocationCount;
        public int ViewStateMutationCount => viewStateMutationCount;
        public int ClipboardActionCount => clipboardActionCount;
        public int InspectorSelectionJumpCount => inspectorSelectionJumpCount;
        public GeneratedCsvNavigationIndex Index => sample?.Index;
        public GeneratedValidationJumpSample JumpSample => sample?.JumpSample;
        public string Map2005HandoffDigest => sample?.Map2005HandoffDigest ?? string.Empty;
        public string ValidationErrorIdSearch => validationErrorIdSearch;
        public string KindFilter => KindFilterLabels[kindFilterIndex];
        public string SourceAvailabilityFilter => AvailabilityFilterLabels[availabilityFilterIndex];
        public GeneratedValidationJumpTarget SelectedTarget => ResolveSelectedTarget();
        public IReadOnlyList<GeneratedValidationJumpTarget> VisibleTargets =>
            new ReadOnlyCollection<GeneratedValidationJumpTarget>(FilterTargets().ToArray());

        [MenuItem(MenuPath)]
        public static GeneratedCsvNavigationWindow Open()
        {
            openInvocationCount++;
            var window = GetWindow<GeneratedCsvNavigationWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(780f, 650f);
            window.EnsureReadOnlySample();
            window.Show();
            return window;
        }

        public static void ResetInvocationDiagnostics()
        {
            openInvocationCount = 0;
            externalActionInvocationCount = 0;
        }

        public void ReloadReadOnlySources()
        {
            sample = GeneratedCsvNavigationSamplePublisher.CreateReadOnlySample(null, string.Empty);
            EnsureSelectedTarget();
            Repaint();
        }

        public void SetValidationErrorIdSearch(string value)
        {
            var next = value ?? string.Empty;
            if (string.Equals(validationErrorIdSearch, next, StringComparison.Ordinal)) return;
            validationErrorIdSearch = next;
            viewStateMutationCount++;
            EnsureSelectedTarget();
            Repaint();
        }

        public void SetKindFilter(string token)
        {
            var index = Array.IndexOf(KindFilterLabels,
                string.IsNullOrWhiteSpace(token) ? "All" : token);
            if (index < 0) throw new ArgumentException("Unknown jump target kind filter.",
                nameof(token));
            if (kindFilterIndex == index) return;
            kindFilterIndex = index;
            viewStateMutationCount++;
            EnsureSelectedTarget();
            Repaint();
        }

        public void SetSourceAvailabilityFilter(string token)
        {
            var index = Array.IndexOf(AvailabilityFilterLabels,
                string.IsNullOrWhiteSpace(token) ? "All" : token);
            if (index < 0) throw new ArgumentException("Unknown source availability filter.",
                nameof(token));
            if (availabilityFilterIndex == index) return;
            availabilityFilterIndex = index;
            viewStateMutationCount++;
            EnsureSelectedTarget();
            Repaint();
        }

        public void SelectValidationError(string validationErrorId)
        {
            EnsureReadOnlySample();
            var target = Index.FindByValidationErrorId(validationErrorId ?? string.Empty);
            if (target == null) throw new ArgumentException("Unknown validation error id.",
                nameof(validationErrorId));
            if (string.Equals(selectedValidationErrorId, target.ValidationErrorId,
                StringComparison.Ordinal)) return;
            selectedValidationErrorId = target.ValidationErrorId;
            viewStateMutationCount++;
            Repaint();
        }

        public string CopyCsvPathRowColumn()
        {
            var value = RequireSelected().SourceLocation.CopyAddress;
            EditorGUIUtility.systemCopyBuffer = value;
            clipboardActionCount++;
            return value;
        }

        public string CopyValidationErrorId()
        {
            var value = RequireSelected().ValidationErrorId;
            EditorGUIUtility.systemCopyBuffer = value;
            clipboardActionCount++;
            return value;
        }

        public string CopyInspectorSelectionPath()
        {
            var value = RequireSelected().SelectionPath;
            EditorGUIUtility.systemCopyBuffer = value;
            clipboardActionCount++;
            return value;
        }

        public string JumpToInspectorSelection()
        {
            var target = RequireSelected();
            if (target.JumpTargetKind == GeneratedValidationJumpTargetKind.Tile)
            {
                if (target.SectorCoordinate == null || target.LocalCellCoordinate == null)
                    return "MissingData: " + target.MissingReason;
                var overlay = GeneratedWorldOverlayInspectorWindow.Open();
                overlay.ReceiveNavigationSelection(target.SectorCoordinate.X,
                    target.SectorCoordinate.Y, target.LocalCellCoordinate.X,
                    target.LocalCellCoordinate.Y, target.SelectionPath);
            }
            else
            {
                var detail = GeneratedDetailInspectorWindow.Open();
                detail.ReceiveNavigationSelection(target.InspectorTabToken,
                    target.InspectorRecordId, target.SelectionPath);
            }
            inspectorSelectionJumpCount++;
            return target.SelectionPath;
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = new Vector2(780f, 650f);
            EnsureReadOnlySample();
        }

        private void EnsureReadOnlySample()
        {
            kindFilterIndex = Mathf.Clamp(kindFilterIndex, 0, KindFilterLabels.Length - 1);
            availabilityFilterIndex = Mathf.Clamp(availabilityFilterIndex, 0,
                AvailabilityFilterLabels.Length - 1);
            if (sample == null) ReloadReadOnlySources();
        }

        private void EnsureSelectedTarget()
        {
            if (sample == null) return;
            var visible = FilterTargets().ToArray();
            if (visible.Length == 0)
            {
                selectedValidationErrorId = string.Empty;
                return;
            }
            if (!visible.Any(value => string.Equals(value.ValidationErrorId,
                selectedValidationErrorId, StringComparison.Ordinal)))
                selectedValidationErrorId = visible[0].ValidationErrorId;
        }

        private IEnumerable<GeneratedValidationJumpTarget> FilterTargets()
        {
            EnsureReadOnlySample();
            var query = Index.JumpTargetRecords.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(validationErrorIdSearch))
                query = query.Where(value => value.ValidationErrorId.IndexOf(
                    validationErrorIdSearch, StringComparison.OrdinalIgnoreCase) >= 0);
            if (kindFilterIndex > 0)
            {
                var token = KindFilterLabels[kindFilterIndex];
                query = query.Where(value => string.Equals(value.JumpTargetKind.ToString(), token,
                    StringComparison.Ordinal));
            }
            if (availabilityFilterIndex == 1)
                query = query.Where(value => value.SourceAvailable);
            else if (availabilityFilterIndex == 2)
                query = query.Where(value => !value.SourceAvailable);
            return query;
        }

        private GeneratedValidationJumpTarget ResolveSelectedTarget()
        {
            if (sample == null || string.IsNullOrWhiteSpace(selectedValidationErrorId)) return null;
            return Index.FindByValidationErrorId(selectedValidationErrorId);
        }

        private GeneratedValidationJumpTarget RequireSelected() => ResolveSelectedTarget() ??
            throw new InvalidOperationException("A validation jump target is not selected.");

        private void OnGUI()
        {
            EnsureReadOnlySample();
            pageScroll = EditorGUILayout.BeginScrollView(pageScroll);
            EditorGUILayout.LabelField("Read-only CSV source navigation", EditorStyles.boldLabel);
            var nextSearch = EditorGUILayout.TextField("Validation error id",
                validationErrorIdSearch);
            if (!string.Equals(nextSearch, validationErrorIdSearch, StringComparison.Ordinal))
                SetValidationErrorIdSearch(nextSearch);
            var nextKind = EditorGUILayout.Popup("Jump target kind", kindFilterIndex,
                KindFilterLabels);
            if (nextKind != kindFilterIndex) SetKindFilter(KindFilterLabels[nextKind]);
            var nextAvailability = EditorGUILayout.Popup("Source availability",
                availabilityFilterIndex, AvailabilityFilterLabels);
            if (nextAvailability != availabilityFilterIndex)
                SetSourceAvailabilityFilter(AvailabilityFilterLabels[nextAvailability]);

            DrawResults();
            DrawSelectedSourceLocation();
            DrawSelectedInspectorTarget();
            DrawActions();
            EditorGUILayout.EndScrollView();
        }

        private void DrawResults()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);
            resultScroll = EditorGUILayout.BeginScrollView(resultScroll, GUILayout.Height(155f));
            foreach (var target in FilterTargets())
            {
                var label = target.ValidationErrorId + " / " + target.JumpTargetKind + " / " +
                            (target.SourceAvailable ? "Available" : "MissingData");
                using (new EditorGUI.DisabledScope(string.Equals(target.ValidationErrorId,
                    selectedValidationErrorId, StringComparison.Ordinal)))
                    if (GUILayout.Button(label, EditorStyles.miniButton))
                        SelectValidationError(target.ValidationErrorId);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawSelectedSourceLocation()
        {
            var target = SelectedTarget;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected source location", EditorStyles.boldLabel);
            if (target == null)
            {
                EditorGUILayout.HelpBox("No matching source location.", MessageType.Info);
                return;
            }
            var source = target.SourceLocation;
            Pair("Source kind", source.SourceKind.ToString());
            Pair("CSV file", source.SourceAvailable ? source.CsvFilePath : "MissingData");
            Pair("Row / column", source.SourceAvailable
                ? source.RowNumber1Based + " / " + source.ColumnNumber1Based
                : "MissingData");
            Pair("Column / field", source.SourceAvailable
                ? source.ColumnName + " / " + source.FieldName : "MissingData");
            Pair("Record", source.RecordKind + " / " + source.RecordId);
            Pair("File / line digest", source.CsvFileDigest + " / " + source.LineDigest);
            if (!source.SourceAvailable)
                EditorGUILayout.HelpBox(source.MissingReason, MessageType.Warning);
        }

        private void DrawSelectedInspectorTarget()
        {
            var target = SelectedTarget;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected inspector target", EditorStyles.boldLabel);
            if (target == null)
            {
                EditorGUILayout.HelpBox("No matching inspector target.", MessageType.Info);
                return;
            }
            Pair("Validation error", target.ValidationErrorId);
            Pair("Severity / owner", target.Severity + " / " + target.Owner);
            Pair("Target kind", target.JumpTargetKind.ToString());
            Pair("Inspector tab / record", target.InspectorTabToken + " / " +
                target.InspectorRecordId);
            Pair("Selection path", target.SelectionPath);
            Pair("Replay reference", target.ReplayReference);
            Pair("Missing reason", target.MissingReason);
        }

        private void DrawActions()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy CSV path:row:column")) CopyCsvPathRowColumn();
                if (GUILayout.Button("Copy validation error id")) CopyValidationErrorId();
                if (GUILayout.Button("Copy inspector selection path"))
                    CopyInspectorSelectionPath();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Jump to inspector selection"))
                    JumpToInspectorSelection();
                if (GUILayout.Button("Open generated MAP20_04 output folder"))
                {
                    var output = GeneratedCsvNavigationSamplePublisher.OutputDirectory(null);
                    if (Directory.Exists(output)) EditorUtility.RevealInFinder(output);
                }
            }
        }

        private static void Pair(string label, string value) =>
            EditorGUILayout.LabelField(label, value ?? string.Empty);
    }
}
