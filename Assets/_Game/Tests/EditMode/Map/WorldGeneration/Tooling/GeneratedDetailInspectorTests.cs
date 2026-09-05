using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Tooling;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Tooling
{
    [Category(CategoryName)]
    public sealed class GeneratedDetailInspectorTests
    {
        private const string CategoryName = "MAP20_03";
        private const string EditorAssemblyName = "MapAuthoring.Editor";
        private const string WindowTypeName =
            "StarNight.MapAuthoring.WorldGeneration.Tooling.GeneratedDetailInspectorWindow";
        private const string PublisherTypeName =
            "StarNight.MapAuthoring.WorldGeneration.Tooling.GeneratedDetailInspectorSamplePublisher";

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string OutputRoot => ProjectPath("MapDesign/MCP/GENERATED/MAP20_03");

        [Test]
        public void DetailInspectorCatalogContainsExactFourReadOnlyTabs()
        {
            Assert.That(GeneratedDetailTabCatalog.Tokens,
                Is.EqualTo(new[] { "Pattern", "Cluster", "Special", "Slice" }));
            Assert.That(GeneratedDetailTabCatalog.Tabs.Count, Is.EqualTo(4));
            Assert.That(GeneratedDetailTabCatalog.Tabs.All(value => value.IsReadOnly &&
                !string.IsNullOrWhiteSpace(value.ShortLabel) &&
                !string.IsNullOrWhiteSpace(value.OwnerCategory)), Is.True);
        }

        [Test]
        public void PatternInspectorReportsFourByFourCandidateAcceptanceRejectionAndMissingData()
        {
            var accepted = Pattern(true, string.Empty);
            var rejected = Pattern(false, "protected-mask-overlap");
            var missing = GeneratedPatternDetailRecord.Missing(6, 6, 7, 9,
                "source detail unavailable");

            Assert.That(accepted.PatternWidth, Is.EqualTo(MicroPatternDefinition.RequiredWidth));
            Assert.That(accepted.PatternHeight, Is.EqualTo(MicroPatternDefinition.RequiredHeight));
            Assert.That(accepted.State, Is.EqualTo(GeneratedPatternCandidateState.Accepted));
            Assert.That(accepted.RejectionReason, Is.Empty);
            Assert.That(rejected.State, Is.EqualTo(GeneratedPatternCandidateState.Rejected));
            Assert.That(rejected.RejectionReason, Is.EqualTo("protected-mask-overlap"));
            Assert.That(missing.State, Is.EqualTo(GeneratedPatternCandidateState.MissingData));
            Assert.That(missing.IsMissingData, Is.True);
            Assert.That(new[] { accepted.AddSolidCount, accepted.CarveAirCount,
                accepted.ProtectedMaskOverlapCount, accepted.AffectedCellCount },
                Is.EqualTo(new[] { 5, 3, 1, 9 }));
        }

        [Test]
        public void ClusterInspectorReportsFootprintPathSlotAndSiteBindingWithoutRecomputing()
        {
            var value = new GeneratedClusterDetailRecord(6, 6, "cluster-a", "Mandatory",
                "spine-a", 1, 2, 7, 8, 24, 9, 8, 3, 4, 1,
                GeneratedTerrainRunPreconditions.Map19ExitDigest, string.Empty);

            Assert.That(value.ClusterId, Is.EqualTo("cluster-a"));
            Assert.That(new[] { value.FootprintMinX, value.FootprintMinY,
                value.FootprintMaxX, value.FootprintMaxY, value.FootprintCellCount },
                Is.EqualTo(new[] { 1, 2, 7, 8, 24 }));
            Assert.That(new[] { value.PathNodeCount, value.PathEdgeCount,
                value.SocketBindingCount, value.ActivitySlotBindingCount,
                value.SpecialSiteBindingCount }, Is.EqualTo(new[] { 9, 8, 3, 4, 1 }));
            Assert.That(value.OwnerProvenanceDigest,
                Is.EqualTo(GeneratedTerrainRunPreconditions.Map19ExitDigest));
            Assert.That(value.IsMissingData, Is.False);
        }

        [Test]
        public void SpecialInspectorDistinguishesAbsentSpecialFromEmptyValidSpecial()
        {
            var absent = GeneratedSpecialDetailRecord.Absent(6, 6,
                "special detail source unavailable");
            var empty = GeneratedSpecialDetailRecord.EmptyValid(6, 6,
                GeneratedTerrainRunPreconditions.Map19ExitDigest);

            Assert.That(absent.IsAbsentData, Is.True);
            Assert.That(absent.IsMissingData, Is.True);
            Assert.That(absent.MissingDataMarker, Is.Not.Empty);
            Assert.That(empty.IsEmptyValidData, Is.True);
            Assert.That(empty.IsAbsentData, Is.False);
            Assert.That(empty.IsMissingData, Is.False);
            Assert.That(empty.MissingDataMarker, Is.Empty);
            Assert.That(new[] { empty.FootprintCellCount, empty.EntryMarkerCount,
                empty.ReturnMarkerCount, empty.FixedShellMarkerCount,
                empty.FacilityMarkerCount, empty.RequiredRewardMarkerCount,
                empty.OptionalMarkerCount }, Is.All.Zero);
        }

        [Test]
        public void SliceInspectorReportsSixteenTwelveByEightSlicesCellsSocketsAndProvenance()
        {
            var details = Details("2026-09-06T00:00:00Z");

            Assert.That(details.SliceRecords.Count,
                Is.EqualTo(GeneratedMicroChunkSliceSet.ChunkCount));
            Assert.That(details.SliceRecords.Select(value => value.SliceIndex),
                Is.EqualTo(Enumerable.Range(0, GeneratedMicroChunkSliceSet.ChunkCount)));
            Assert.That(details.SliceRecords.All(value =>
                value.Width == GeneratedMicroChunkSliceSet.MicroChunkWidth &&
                value.Height == GeneratedMicroChunkSliceSet.MicroChunkHeight &&
                value.CellCount == GeneratedMicroChunkSliceSet.MicroChunkCellCount), Is.True);
            Assert.That(details.SliceRecords.All(value => value.SocketBands.Count > 0 &&
                value.MarkerSlots.Count > 0 && value.ProvenanceRecords.Count > 0), Is.True);
            Assert.That(details.SliceRecords.SelectMany(value => value.Cells).All(value =>
                value.HasWorldCoordinate && value.IsMissingData), Is.True);
            Assert.That(details.SliceRecords.All(value => value.IsMissingData), Is.True);
        }

        [Test]
        public void DetailInspectorWindowOpensWithoutGenerationRollbackValidationCsvJumpOrExport()
        {
            var type = EditorType(WindowTypeName);
            type.GetMethod("ResetInvocationDiagnostics", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, null);
            var window = type.GetMethod("Open", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, null);

            Assert.That((int)type.GetProperty("OpenInvocationCount").GetValue(null), Is.EqualTo(1));
            Assert.That((int)type.GetProperty("ExternalActionInvocationCount").GetValue(null),
                Is.EqualTo(0));
            Assert.That(type.GetProperty("Snapshot").GetValue(window), Is.Not.Null);
            Assert.That(type.GetProperty("Details").GetValue(window), Is.Not.Null);
            type.GetMethod("Close", BindingFlags.Public | BindingFlags.Instance).Invoke(window, null);
        }

        [Test]
        public void DetailTabAndRecordSelectionChangeOnlyViewState()
        {
            var type = EditorType(WindowTypeName);
            var window = type.GetMethod("Open", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, null);
            var snapshotDigest = (string)type.GetProperty("SnapshotDigest").GetValue(window);
            var detailDigest = (string)type.GetProperty("CombinedDetailDigest").GetValue(window);

            Invoke(window, "SetTab", GeneratedDetailTabCatalog.Tabs[3].Token);
            Invoke(window, "SelectRecord", 5);

            Assert.That((string)type.GetProperty("SelectedTabToken").GetValue(window),
                Is.EqualTo(GeneratedDetailTabCatalog.Tabs[3].Token));
            Assert.That((int)type.GetProperty("SelectedRecordIndex").GetValue(window), Is.EqualTo(5));
            Assert.That((string)type.GetProperty("SnapshotDigest").GetValue(window),
                Is.EqualTo(snapshotDigest));
            Assert.That((string)type.GetProperty("CombinedDetailDigest").GetValue(window),
                Is.EqualTo(detailDigest));
            Assert.That((int)type.GetProperty("ViewStateMutationCount").GetValue(window),
                Is.EqualTo(2));
            Assert.That((int)type.GetProperty("ExternalActionInvocationCount").GetValue(null),
                Is.EqualTo(0));
            type.GetMethod("Close", BindingFlags.Public | BindingFlags.Instance).Invoke(window, null);
        }

        [Test]
        public void DetailSnapshotsSerializeDeterministicallyAcrossRepeatCultureAndInputOrder()
        {
            var details = Details("2026-09-06T00:00:00Z");
            var snapshot = Snapshot(details, "2026-09-06T00:00:00Z");
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var reorderedDetails = new GeneratedPatternClusterSpecialSliceInspection(
                    details.PatternRecords.Reverse(), details.ClusterRecords.Reverse(),
                    details.SpecialRecords.Reverse(), details.SliceRecords.Reverse(),
                    details.MissingDataRecords.Reverse(), "2099-12-31T23:59:59Z");
                var reorderedSnapshot = new GeneratedDetailInspectionSnapshot(
                    snapshot.SourceWorldOverlayDigest, snapshot.SourceSectorCanvasDigest,
                    snapshot.Map2003HandoffDigest, snapshot.SelectedSectorX,
                    snapshot.SelectedSectorY, snapshot.SelectedCellX, snapshot.SelectedCellY,
                    snapshot.TabSummaries.Reverse(), snapshot.MissingDataRecords.Reverse(),
                    snapshot.ValidationRecords.Reverse(), "2099-12-31T23:59:59Z");

                Assert.That(reorderedDetails.CanonicalDigest, Is.EqualTo(details.CanonicalDigest));
                Assert.That(reorderedSnapshot.CanonicalDigest, Is.EqualTo(snapshot.CanonicalDigest));
                Assert.That(details.Serialize(), Is.EqualTo(details.Serialize()));
                Assert.That(snapshot.Serialize(), Is.EqualTo(snapshot.Serialize()));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [Test]
        public void DetailInspectorReusesMap20_02ContextAndPublishesMap20_04HandoffOnlyOnPass()
        {
            var sourcePaths = new[]
            {
                ProjectPath("MapDesign/MCP/GENERATED/MAP20_02/world_overlay_snapshot.json"),
                ProjectPath("MapDesign/MCP/GENERATED/MAP20_02/sector_canvas_inspection_sample.json"),
                ProjectPath("MapDesign/MCP/GENERATED/MAP20_02/overlay_digest_manifest.json"),
            };
            var before = sourcePaths.Select(HashFile).ToArray();
            var publication = InvokeStatic(EditorType(PublisherTypeName),
                "PublishSamples", ProjectRoot);
            var publicationType = publication.GetType();
            var sample = publicationType.GetProperty("Sample").GetValue(publication);
            var snapshot = (GeneratedDetailInspectionSnapshot)sample.GetType()
                .GetProperty("Snapshot").GetValue(sample);

            Assert.That(sourcePaths.Select(HashFile), Is.EqualTo(before));
            Assert.That(snapshot.SourceWorldOverlayDigest,
                Is.EqualTo(ReadJsonString(sourcePaths[0], "canonical_digest")));
            Assert.That(snapshot.SourceSectorCanvasDigest,
                Is.EqualTo(ReadJsonString(sourcePaths[1], "canonical_digest")));
            Assert.That(snapshot.Map2003HandoffDigest,
                Is.EqualTo(GeneratedDetailInspectionPreconditions.Map2003HandoffDigest));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256((string)publicationType
                .GetProperty("ManifestDigest").GetValue(publication)), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256((string)publicationType
                .GetProperty("Map2004HandoffDigest").GetValue(publication)), Is.True);
            Assert.That(File.Exists(Path.Combine(OutputRoot, "detail_inspection_snapshot.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(OutputRoot,
                "pattern_cluster_special_slice_sample.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(OutputRoot, "detail_digest_manifest.json")), Is.True);
        }

        [Test]
        public void DetailInspectorDoesNotSelectLegacyRegressionPriorCategoriesPlayModeOrFullRegression()
        {
            var windowSource = File.ReadAllText(ProjectPath(
                "Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedDetailInspectorWindow.cs"));
            var publisherSource = File.ReadAllText(ProjectPath(
                "Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedDetailInspectorSamplePublisher.cs"));
            var combined = windowSource + "\n" + publisherSource;

            Assert.That(combined, Does.Not.Contain("19347"));
            Assert.That(combined, Does.Not.Contain("PlayMode"));
            Assert.That(combined, Does.Not.Contain("TestRunnerApi"));
            Assert.That(combined, Does.Not.Contain("run_tests"));
            Assert.That(combined, Does.Not.Contain("RunMap19_09ScaleAudit"));
            Assert.That(combined, Does.Not.Contain("GeneratedTerrainRunCoordinator"));
            Assert.That(combined, Does.Not.Contain("GeneratedWorldOverlaySamplePublisher.Publish"));
            Assert.That(windowSource, Does.Not.Contain("File.Write"));
            Assert.That(windowSource, Does.Not.Contain("Tilemap"));
            Assert.That(windowSource, Does.Not.Contain("Instantiate("));
            Assert.That(windowSource, Does.Not.Contain("Destroy("));
        }

        private static GeneratedPatternDetailRecord Pattern(bool accepted, string rejection) =>
            GeneratedPatternDetailRecord.Projection(6, 6, 1, 2, 3, 0, "pattern-a",
                "biome-profile-a", 2, "MirrorX", accepted, rejection, 5, 3, 1, 9,
                GeneratedTerrainRunPreconditions.Map19ExitDigest);

        private static GeneratedPatternClusterSpecialSliceInspection Details(string createdUtc) =>
            GeneratedPatternClusterSpecialSliceInspection.CreateMissingDataSample(6, 6, 7, 9,
                createdUtc);

        private static GeneratedDetailInspectionSnapshot Snapshot(
            GeneratedPatternClusterSpecialSliceInspection details, string createdUtc) =>
            GeneratedDetailInspectionSnapshot.Create(
                GeneratedTerrainRunPreconditions.Map19ExitDigest,
                GeneratedWorldOverlayPreconditions.Map2001HandoffDigest,
                6, 6, 7, 9, details, createdUtc);

        private static object Invoke(object target, string method, params object[] arguments)
        {
            try
            {
                return target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(target, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static object InvokeStatic(Type type, string method, params object[] arguments)
        {
            try
            {
                return type.GetMethod(method, BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static Type EditorType(string fullName)
        {
            var type = Type.GetType(fullName + ", " + EditorAssemblyName, false);
            Assert.That(type, Is.Not.Null, fullName + " must compile into " + EditorAssemblyName + ".");
            return type;
        }

        private static string ProjectPath(string relative) => Path.GetFullPath(Path.Combine(
            ProjectRoot, relative.Replace('/', Path.DirectorySeparatorChar)));

        private static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(File.ReadAllBytes(path))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static string ReadJsonString(string path, string field)
        {
            var marker = "\"" + field + "\": \"";
            var text = File.ReadAllText(path);
            var start = text.IndexOf(marker, StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0));
            start += marker.Length;
            var end = text.IndexOf('"', start);
            return text.Substring(start, end - start);
        }
    }
}
