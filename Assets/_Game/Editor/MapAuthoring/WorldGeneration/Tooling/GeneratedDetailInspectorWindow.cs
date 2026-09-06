using System;
using System.IO;
using StarNight.Map.WorldGeneration.Tooling;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.WorldGeneration.Tooling
{
    public sealed class GeneratedDetailInspectorWindow : EditorWindow
    {
        public const string MenuPath = "Tools/MapDesign/Generated Detail Inspector";
        public const string WindowTitle = "Generated Detail Inspector";

        private static int openInvocationCount;
        private static int externalActionInvocationCount;

        [SerializeField] private int selectedTabIndex;
        [SerializeField] private int selectedRecordIndex;
        [NonSerialized] private GeneratedDetailReadOnlySample sample;
        [NonSerialized] private Vector2 pageScroll;
        [NonSerialized] private Vector2 recordScroll;
        [NonSerialized] private int viewStateMutationCount;
        [NonSerialized] private string navigationSelectionPath = string.Empty;
        [NonSerialized] private string navigationRecordId = string.Empty;
        [NonSerialized] private bool navigationSelectionResolved;

        public static int OpenInvocationCount => openInvocationCount;
        public static int ExternalActionInvocationCount => externalActionInvocationCount;
        public int ViewStateMutationCount => viewStateMutationCount;
        public GeneratedDetailInspectionSnapshot Snapshot => sample?.Snapshot;
        public GeneratedPatternClusterSpecialSliceInspection Details => sample?.Details;
        public string SelectedTabToken => GeneratedDetailTabCatalog.Tabs[selectedTabIndex].Token;
        public int SelectedRecordIndex => selectedRecordIndex;
        public string SnapshotDigest => Snapshot?.CanonicalDigest ?? string.Empty;
        public string CombinedDetailDigest => Details?.CanonicalDigest ?? string.Empty;
        public string NavigationSelectionPath => navigationSelectionPath;
        public string NavigationRecordId => navigationRecordId;
        public bool NavigationSelectionResolved => navigationSelectionResolved;

        [MenuItem(MenuPath)]
        public static GeneratedDetailInspectorWindow Open()
        {
            openInvocationCount++;
            var window = GetWindow<GeneratedDetailInspectorWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(760f, 620f);
            window.EnsureReadOnlySample();
            window.Show();
            return window;
        }

        public static void ResetInvocationDiagnostics()
        {
            openInvocationCount = 0;
            externalActionInvocationCount = 0;
        }

        public void ReloadReadOnlySample()
        {
            sample = GeneratedDetailInspectorSamplePublisher.LoadReadOnlySample(null, string.Empty);
            selectedTabIndex = Mathf.Clamp(selectedTabIndex, 0,
                GeneratedDetailTabCatalog.Tabs.Count - 1);
            selectedRecordIndex = Mathf.Clamp(selectedRecordIndex, 0,
                Mathf.Max(0, RecordCount(selectedTabIndex) - 1));
            Repaint();
        }

        public void SetTab(string tabToken)
        {
            var index = GeneratedDetailTabCatalog.IndexOf(tabToken);
            if (index < 0) throw new ArgumentException("Unknown detail tab token.", nameof(tabToken));
            if (selectedTabIndex == index) return;
            selectedTabIndex = index;
            selectedRecordIndex = 0;
            viewStateMutationCount++;
            Repaint();
        }

        public void SelectRecord(int index)
        {
            var count = RecordCount(selectedTabIndex);
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
            if (selectedRecordIndex == index) return;
            selectedRecordIndex = index;
            viewStateMutationCount++;
            Repaint();
        }

        public int RecordCountForTab(string tabToken)
        {
            var index = GeneratedDetailTabCatalog.IndexOf(tabToken);
            if (index < 0) throw new ArgumentException("Unknown detail tab token.", nameof(tabToken));
            return RecordCount(index);
        }

        public void ReceiveNavigationSelection(string tabToken, string recordId,
            string selectionPath)
        {
            EnsureReadOnlySample();
            var tabIndex = GeneratedDetailTabCatalog.IndexOf(tabToken);
            if (tabIndex < 0)
                throw new ArgumentException("Unknown detail tab token.", nameof(tabToken));
            if (string.IsNullOrWhiteSpace(recordId))
                throw new ArgumentException("Navigation record id is required.", nameof(recordId));
            if (string.IsNullOrWhiteSpace(selectionPath))
                throw new ArgumentException("Navigation selection path is required.",
                    nameof(selectionPath));

            var recordIndex = FindRecordIndex(tabIndex, recordId);
            var resolved = recordIndex >= 0;
            var nextRecordIndex = resolved ? recordIndex : 0;
            if (selectedTabIndex == tabIndex && selectedRecordIndex == nextRecordIndex &&
                string.Equals(navigationRecordId, recordId, StringComparison.Ordinal) &&
                string.Equals(navigationSelectionPath, selectionPath, StringComparison.Ordinal) &&
                navigationSelectionResolved == resolved) return;
            selectedTabIndex = tabIndex;
            selectedRecordIndex = nextRecordIndex;
            navigationRecordId = recordId;
            navigationSelectionPath = selectionPath;
            navigationSelectionResolved = resolved;
            viewStateMutationCount++;
            Repaint();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = new Vector2(760f, 620f);
            EnsureReadOnlySample();
        }

        private void EnsureReadOnlySample()
        {
            if (sample == null) ReloadReadOnlySample();
        }

        private void OnGUI()
        {
            EnsureReadOnlySample();
            pageScroll = EditorGUILayout.BeginScrollView(pageScroll);
            EditorGUILayout.LabelField("Read-only source", EditorStyles.boldLabel);
            EditorGUILayout.Popup("Source sample", 0, new[] { "Current MAP20_02 inspection sample" });
            if (GUILayout.Button("Reload read-only context", GUILayout.Width(190f)))
                ReloadReadOnlySample();
            EditorGUILayout.LabelField("Selected sector",
                Snapshot.SelectedSectorX + "," + Snapshot.SelectedSectorY);
            EditorGUILayout.LabelField("Selected cell",
                Snapshot.SelectedCellX + "," + Snapshot.SelectedCellY);
            if (!string.IsNullOrWhiteSpace(navigationSelectionPath))
            {
                EditorGUILayout.LabelField("Navigation selection",
                    navigationSelectionPath);
                EditorGUILayout.LabelField("Navigation record state",
                    navigationSelectionResolved ? "Selected" : "MissingData");
            }

            EditorGUILayout.Space();
            var labels = new string[GeneratedDetailTabCatalog.Tabs.Count];
            for (var index = 0; index < labels.Length; index++)
                labels[index] = GeneratedDetailTabCatalog.Tabs[index].Token;
            var nextTab = GUILayout.Toolbar(selectedTabIndex, labels);
            if (nextTab != selectedTabIndex)
                SetTab(GeneratedDetailTabCatalog.Tabs[nextTab].Token);

            var summary = Snapshot.Summary(SelectedTabToken);
            EditorGUILayout.LabelField("Records", summary.SelectedRecordCount.ToString());
            EditorGUILayout.LabelField("MissingData", summary.MissingDataCount.ToString());
            EditorGUILayout.SelectableLabel("Digest  " + summary.CanonicalDigestContribution,
                EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

            DrawRecordList();
            DrawSelectedRecord();
            DrawUtilities();
            EditorGUILayout.EndScrollView();
        }

        private void DrawRecordList()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Records (read only)", EditorStyles.boldLabel);
            recordScroll = EditorGUILayout.BeginScrollView(recordScroll, GUILayout.Height(150f));
            var count = RecordCount(selectedTabIndex);
            for (var index = 0; index < count; index++)
            {
                using (new EditorGUI.DisabledScope(index == selectedRecordIndex))
                    if (GUILayout.Button(RecordLabel(selectedTabIndex, index), EditorStyles.miniButton))
                        SelectRecord(index);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawSelectedRecord()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected record detail", EditorStyles.boldLabel);
            switch (selectedTabIndex)
            {
                case 0: DrawPattern(Details.PatternRecords[selectedRecordIndex]); break;
                case 1: DrawCluster(Details.ClusterRecords[selectedRecordIndex]); break;
                case 2: DrawSpecial(Details.SpecialRecords[selectedRecordIndex]); break;
                case 3: DrawSlice(Details.SliceRecords[selectedRecordIndex]); break;
            }
        }

        private static void DrawPattern(GeneratedPatternDetailRecord value)
        {
            Pair("Pattern", value.PatternId + " / " + value.State);
            Pair("Biome/profile", value.BiomeProfileId);
            Pair("Zone / local", value.PatternZoneX + "," + value.PatternZoneY + " / " +
                value.PatternLocalX + "," + value.PatternLocalY);
            Pair("Transform / ordinal", value.TransformToken + " / " + value.CandidateOrdinal);
            Pair("Add / carve / protected / affected", value.AddSolidCount + " / " +
                value.CarveAirCount + " / " + value.ProtectedMaskOverlapCount + " / " +
                value.AffectedCellCount);
            Pair("Rejection", value.RejectionReason);
            Pair("Source / MissingData", value.SourceDigest + " / " + value.MissingDataMarker);
        }

        private static void DrawCluster(GeneratedClusterDetailRecord value)
        {
            Pair("Cluster / role / spine", value.ClusterId + " / " + value.ClusterRole + " / " +
                value.SpineVariantId);
            Pair("Footprint bounds / cells", value.FootprintMinX + "," + value.FootprintMinY + ".." +
                value.FootprintMaxX + "," + value.FootprintMaxY + " / " + value.FootprintCellCount);
            Pair("Path nodes / edges", value.PathNodeCount + " / " + value.PathEdgeCount);
            Pair("Socket / activity slot / special site", value.SocketBindingCount + " / " +
                value.ActivitySlotBindingCount + " / " + value.SpecialSiteBindingCount);
            Pair("Owner / MissingData", value.OwnerProvenanceDigest + " / " + value.MissingDataMarker);
        }

        private static void DrawSpecial(GeneratedSpecialDetailRecord value)
        {
            Pair("State / id", value.State + " / " + value.SpecialRegionId);
            Pair("Site / category", value.SiteKind + " / " + value.RegionCategory);
            Pair("Footprint bounds / cells", value.FootprintMinX + "," + value.FootprintMinY + ".." +
                value.FootprintMaxX + "," + value.FootprintMaxY + " / " + value.FootprintCellCount);
            Pair("Entry / return / fixed shell", value.EntryMarkerCount + " / " +
                value.ReturnMarkerCount + " / " + value.FixedShellMarkerCount);
            Pair("Facility / required / optional", value.FacilityMarkerCount + " / " +
                value.RequiredRewardMarkerCount + " / " + value.OptionalMarkerCount);
            Pair("Binding / MissingData", value.SiteBindingDigest + " / " + value.MissingDataMarker);
        }

        private static void DrawSlice(GeneratedSliceDetailRecord value)
        {
            Pair("Slice / chunk", value.SliceIndex + " / " + value.ChunkX + "," + value.ChunkY);
            Pair("Dimensions / cells", value.Width + "x" + value.Height + " / " + value.CellCount);
            Pair("Socket bands", value.SocketBands.Count.ToString());
            Pair("Marker slots", value.MarkerSlots.Count.ToString());
            Pair("Provenance records", value.ProvenanceRecords.Count.ToString());
            Pair("Owner / MissingData", value.OwnerProvenanceDigest + " / " + value.MissingDataMarker);
            var cell = value.Cells[0];
            Pair("First cell local / sector / world", cell.CellLocalX + "," + cell.CellLocalY + " / " +
                cell.SectorLocalX + "," + cell.SectorLocalY + " / " + cell.WorldX + "," + cell.WorldY);
        }

        private static void Pair(string label, string value) =>
            EditorGUILayout.LabelField(label, value ?? string.Empty);

        private void DrawUtilities()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open generated MAP20_03 output folder"))
                {
                    var output = GeneratedDetailInspectorSamplePublisher.OutputDirectory(null);
                    if (Directory.Exists(output)) EditorUtility.RevealInFinder(output);
                }
                if (GUILayout.Button("Copy detail digest"))
                    EditorGUIUtility.systemCopyBuffer = Snapshot.CanonicalDigest;
            }
        }

        private int RecordCount(int tabIndex)
        {
            EnsureReadOnlySample();
            switch (tabIndex)
            {
                case 0: return Details.PatternRecords.Count;
                case 1: return Details.ClusterRecords.Count;
                case 2: return Details.SpecialRecords.Count;
                case 3: return Details.SliceRecords.Count;
                default: throw new ArgumentOutOfRangeException(nameof(tabIndex));
            }
        }

        private string RecordLabel(int tabIndex, int index)
        {
            switch (tabIndex)
            {
                case 0:
                    var pattern = Details.PatternRecords[index];
                    return pattern.PatternId + " / " + pattern.State;
                case 1:
                    var cluster = Details.ClusterRecords[index];
                    return cluster.ClusterId + " / " + cluster.ClusterRole;
                case 2:
                    var special = Details.SpecialRecords[index];
                    return special.State + " / " + special.SpecialRegionId;
                case 3:
                    var slice = Details.SliceRecords[index];
                    return "Slice " + slice.SliceIndex + " / chunk " + slice.ChunkX + "," + slice.ChunkY;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tabIndex));
            }
        }

        private int FindRecordIndex(int tabIndex, string recordId)
        {
            switch (tabIndex)
            {
                case 0:
                    for (var index = 0; index < Details.PatternRecords.Count; index++)
                        if (string.Equals(Details.PatternRecords[index].PatternId, recordId,
                            StringComparison.Ordinal)) return index;
                    break;
                case 1:
                    for (var index = 0; index < Details.ClusterRecords.Count; index++)
                        if (string.Equals(Details.ClusterRecords[index].ClusterId, recordId,
                            StringComparison.Ordinal)) return index;
                    break;
                case 2:
                    for (var index = 0; index < Details.SpecialRecords.Count; index++)
                        if (string.Equals(Details.SpecialRecords[index].SpecialRegionId, recordId,
                            StringComparison.Ordinal)) return index;
                    break;
                case 3:
                    for (var index = 0; index < Details.SliceRecords.Count; index++)
                        if (string.Equals("slice:" + Details.SliceRecords[index].SliceIndex,
                            recordId, StringComparison.Ordinal)) return index;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tabIndex));
            }
            return -1;
        }
    }
}
