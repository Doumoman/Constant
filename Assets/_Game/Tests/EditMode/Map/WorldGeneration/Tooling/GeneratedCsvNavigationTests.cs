using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Tooling;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Tooling
{
    [Category(CategoryName)]
    public sealed class GeneratedCsvNavigationTests
    {
        private const string CategoryName = "MAP20_04";
        private const string EditorAssemblyName = "MapAuthoring.Editor";
        private const string WindowTypeName =
            "StarNight.MapAuthoring.WorldGeneration.Tooling.GeneratedCsvNavigationWindow";
        private const string DetailWindowTypeName =
            "StarNight.MapAuthoring.WorldGeneration.Tooling.GeneratedDetailInspectorWindow";
        private const string PublisherTypeName =
            "StarNight.MapAuthoring.WorldGeneration.Tooling.GeneratedCsvNavigationSamplePublisher";

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string OutputRoot => ProjectPath("MapDesign/MCP/GENERATED/MAP20_04");

        [Test]
        public void CsvNavigationIndexMapsErrorIdToExactFileRowColumnFieldAndRecord()
        {
            var sample = Sample("2026-09-06T00:00:00Z");
            var index = sample.Index;
            var tile = index.FindByValidationErrorId("MAP20_04_TILE_SOURCE");

            Assert.That(tile, Is.Not.Null);
            Assert.That(tile.SourceLocation.SourceKind,
                Is.EqualTo(GeneratedCsvSourceKind.AuthoringCsv));
            Assert.That(tile.SourceLocation.CsvFilePath, Does.EndWith(
                "MicroPattern/micro_pattern_cells_v2.csv"));
            Assert.That(tile.SourceLocation.RowNumber1Based, Is.EqualTo(2));
            Assert.That(tile.SourceLocation.ColumnNumber1Based, Is.EqualTo(4));
            Assert.That(tile.SourceLocation.ColumnName, Is.EqualTo("operation"));
            Assert.That(tile.SourceLocation.FieldName, Is.EqualTo("operation"));
            Assert.That(tile.SourceLocation.RecordId, Is.EqualTo("MP_CRATER_BOWL/0/0"));
            Assert.That(tile.SourceLocation.RecordKind, Is.EqualTo("Tile"));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                tile.SourceLocation.CsvFileDigest), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                tile.SourceLocation.LineDigest), Is.True);
            Assert.That(tile.SourceLocation.CopyAddress,
                Is.EqualTo(tile.SourceLocation.CsvFilePath + ":2:4"));
            Assert.That(index.JumpTargetRecords.All(value => ReferenceEquals(
                value, index.FindByValidationErrorId(value.ValidationErrorId))), Is.True);
            Assert.That(index.SourceKindTokens, Is.EqualTo(new[]
            {
                "AuthoringCsv", "GeneratedCsv", "GeneratedJson", "InMemoryOnly", "MissingData",
            }));
        }

        [Test]
        public void ValidationJumpTargetsUseExactTilePatternClusterSocketAndSlotKinds()
        {
            var sample = Sample("2026-09-06T00:00:00Z");
            var kinds = sample.Index.JumpTargetRecords.Select(value => value.JumpTargetKind)
                .Distinct().OrderBy(value => (int)value).Select(value => value.ToString()).ToArray();

            Assert.That(kinds, Is.EqualTo(new[]
                { "Tile", "Pattern", "Cluster", "Socket", "Slot" }));
            Assert.That(sample.JumpSample.JumpTargetKindTokens, Is.EqualTo(kinds));
            Assert.That(sample.Index.JumpTargetRecords.All(value =>
                value.SourceLocation != null && !string.IsNullOrWhiteSpace(value.ValidationErrorId) &&
                !string.IsNullOrWhiteSpace(value.Severity) &&
                !string.IsNullOrWhiteSpace(value.Owner) &&
                !string.IsNullOrWhiteSpace(value.Message) &&
                !string.IsNullOrWhiteSpace(value.InspectorTabToken) &&
                !string.IsNullOrWhiteSpace(value.InspectorRecordId) &&
                !string.IsNullOrWhiteSpace(value.SelectionPath) &&
                BakingCanonicalDigest.IsLowerHexSha256(value.CanonicalDigest)), Is.True);
            Assert.That(sample.Index.FindByValidationErrorId("MAP20_04_PATTERN_SOURCE").PatternId,
                Is.EqualTo("MP_CRATER_BOWL"));
            Assert.That(sample.Index.FindByValidationErrorId("MAP20_04_CLUSTER_SOURCE").ClusterId,
                Is.EqualTo("TC_CRATER_BOWL_ASCENT"));
            Assert.That(sample.Index.FindByValidationErrorId("MAP20_04_SOCKET_SOURCE").SocketId,
                Is.EqualTo("SOCK_L"));
            Assert.That(sample.Index.FindByValidationErrorId("MAP20_04_SLOT_SOURCE").SlotId,
                Is.EqualTo("SLOT_CRATER_BOULDER_CHAIN_CUE"));
        }

        [Test]
        public void CsvNavigationReportsMissingSourceWithoutInventingRowColumnOrInspectorData()
        {
            var sample = Sample("2026-09-06T00:00:00Z");
            var missing = sample.Index.FindByValidationErrorId(
                "MAP20_04_PATTERN_MISSING_DATA");

            Assert.That(missing, Is.Not.Null);
            Assert.That(missing.SourceAvailable, Is.False);
            Assert.That(missing.SourceLocation.SourceKind,
                Is.EqualTo(GeneratedCsvSourceKind.MissingData));
            Assert.That(missing.SourceLocation.RowNumber1Based, Is.Zero);
            Assert.That(missing.SourceLocation.ColumnNumber1Based, Is.Zero);
            Assert.That(missing.SourceLocation.CsvFilePath, Is.Empty);
            Assert.That(missing.SourceLocation.CsvFileDigest,
                Is.EqualTo(GeneratedCsvSourceLocation.MissingDigest));
            Assert.That(missing.SourceLocation.LineDigest,
                Is.EqualTo(GeneratedCsvSourceLocation.MissingDigest));
            Assert.That(missing.PatternId, Is.Empty);
            Assert.That(missing.MissingReason, Is.Not.Empty);
            Assert.That(missing.SelectionPath, Does.Contain("Pattern"));
            Assert.That(sample.Index.MissingDataRecords.Count, Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() => new GeneratedCsvSourceLocation(
                GeneratedCsvSourceKind.MissingData, string.Empty,
                GeneratedCsvSourceLocation.MissingDigest, 1, 1, string.Empty, string.Empty,
                "missing", "Pattern", GeneratedCsvSourceLocation.MissingDigest, false,
                "source missing"));
        }

        [Test]
        public void CsvNavigationWindowOpensWithoutValidationGenerationRollbackReplayOrCsvWrite()
        {
            var publisher = EditorType(PublisherTypeName);
            InvokeStatic(publisher, "ResetDiagnostics");
            var before = HashExistingOutputFiles();
            var type = EditorType(WindowTypeName);
            InvokeStatic(type, "ResetInvocationDiagnostics");
            var window = InvokeStatic(type, "Open");

            Assert.That((int)type.GetProperty("OpenInvocationCount").GetValue(null), Is.EqualTo(1));
            Assert.That((int)type.GetProperty("ExternalActionInvocationCount").GetValue(null),
                Is.Zero);
            Assert.That(type.GetProperty("Index").GetValue(window), Is.Not.Null);
            Assert.That(type.GetProperty("JumpSample").GetValue(window), Is.Not.Null);
            Assert.That((int)publisher.GetProperty("PublicationInvocationCount").GetValue(null),
                Is.Zero);
            Assert.That((int)publisher.GetProperty("ArtifactWriteCount").GetValue(null), Is.Zero);
            Assert.That((int)publisher.GetProperty("CsvAuthoringWriteCount").GetValue(null), Is.Zero);
            Assert.That(HashExistingOutputFiles(), Is.EqualTo(before));
            Invoke(window, "Close");
        }

        [Test]
        public void JumpToInspectorSelectionChangesOnlyViewState()
        {
            var sourceHashes = HashAuthoringSources(Sample("2026-09-06T00:00:00Z").Index);
            var detailType = EditorType(DetailWindowTypeName);
            var detail = InvokeStatic(detailType, "Open");
            var snapshotDigest = (string)detailType.GetProperty("SnapshotDigest").GetValue(detail);
            var combinedDigest = (string)detailType.GetProperty("CombinedDetailDigest").GetValue(detail);
            var navigationType = EditorType(WindowTypeName);
            var navigation = InvokeStatic(navigationType, "Open");
            Invoke(navigation, "SelectValidationError", "MAP20_04_PATTERN_SOURCE");
            var indexDigest = ((GeneratedCsvNavigationIndex)navigationType.GetProperty("Index")
                .GetValue(navigation)).CanonicalDigest;

            var path = (string)Invoke(navigation, "JumpToInspectorSelection");

            Assert.That(path, Is.EqualTo((string)detailType.GetProperty("NavigationSelectionPath")
                .GetValue(detail)));
            Assert.That((string)detailType.GetProperty("NavigationRecordId").GetValue(detail),
                Is.EqualTo("MP_CRATER_BOWL"));
            Assert.That((bool)detailType.GetProperty("NavigationSelectionResolved").GetValue(detail),
                Is.False, "MAP20_03 explicitly contains MissingData instead of a fabricated record.");
            Assert.That((string)detailType.GetProperty("SnapshotDigest").GetValue(detail),
                Is.EqualTo(snapshotDigest));
            Assert.That((string)detailType.GetProperty("CombinedDetailDigest").GetValue(detail),
                Is.EqualTo(combinedDigest));
            Assert.That(((GeneratedCsvNavigationIndex)navigationType.GetProperty("Index")
                .GetValue(navigation)).CanonicalDigest, Is.EqualTo(indexDigest));
            Assert.That((int)navigationType.GetProperty("InspectorSelectionJumpCount")
                .GetValue(navigation), Is.EqualTo(1));
            Assert.That(HashAuthoringSources(Sample("2026-09-06T00:00:00Z").Index),
                Is.EqualTo(sourceHashes));
            Invoke(navigation, "Close");
            Invoke(detail, "Close");
        }

        [Test]
        public void CsvNavigationArtifactsSerializeDeterministicallyAcrossRepeatCultureAndInputOrder()
        {
            var original = Sample("2026-09-06T00:00:00Z");
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var reorderedIndex = new GeneratedCsvNavigationIndex(
                    original.Index.SourceMap2003DetailSnapshotDigest,
                    original.Index.SourceMap2003CombinedDetailDigest,
                    original.Index.Map2004HandoffDigest,
                    original.Index.SourceRecords.Reverse(),
                    original.Index.JumpTargetRecords.Reverse(), "2026-09-06T00:00:00Z");
                var reorderedSample = new GeneratedValidationJumpSample(
                    original.JumpSample.SampleJumpTargets.Reverse(), "2026-09-06T00:00:00Z");

                Assert.That(reorderedIndex.CanonicalDigest,
                    Is.EqualTo(original.Index.CanonicalDigest));
                Assert.That(reorderedSample.CanonicalDigest,
                    Is.EqualTo(original.JumpSample.CanonicalDigest));
                Assert.That(reorderedIndex.Serialize(), Is.EqualTo(original.Index.Serialize()));
                Assert.That(reorderedSample.Serialize(),
                    Is.EqualTo(original.JumpSample.Serialize()));
                Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                    original.Map2005HandoffDigest), Is.True);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [Test]
        public void CsvNavigationPublishesMap20_05HandoffOnlyAfterFocusedPass()
        {
            var publisher = EditorType(PublisherTypeName);
            InvokeStatic(publisher, "ResetDiagnostics");
            var unusedRoot = Path.Combine(Path.GetTempPath(),
                "map20_04_no_publish_" + Guid.NewGuid().ToString("N"));
            Assert.Throws<InvalidOperationException>(() =>
                InvokeStatic(publisher, "PublishSamples", unusedRoot, false));
            Assert.That(Directory.Exists(unusedRoot), Is.False);
            Assert.That((int)publisher.GetProperty("PublicationInvocationCount").GetValue(null),
                Is.Zero);

            var sourceSample = Sample("2026-09-06T00:00:00Z");
            var authoringBefore = HashAuthoringSources(sourceSample.Index);
            var map2003Before = HashFiles(new[]
            {
                ProjectPath("MapDesign/MCP/GENERATED/MAP20_03/detail_digest_manifest.json"),
                ProjectPath("MapDesign/MCP/GENERATED/MAP20_03/detail_inspection_snapshot.json"),
                ProjectPath("MapDesign/MCP/GENERATED/MAP20_03/pattern_cluster_special_slice_sample.json"),
            });
            var publication = InvokeStatic(publisher, "PublishSamples", ProjectRoot, true);
            var publicationType = publication.GetType();
            var manifestDigest = (string)publicationType.GetProperty("ManifestDigest")
                .GetValue(publication);
            var handoffDigest = (string)publicationType.GetProperty("Map2005HandoffDigest")
                .GetValue(publication);

            Assert.That(Directory.GetFiles(OutputRoot).Select(Path.GetFileName).OrderBy(value => value),
                Is.EqualTo(new[]
                {
                    "csv_navigation_index.json", "source_navigation_digest_manifest.json",
                    "validation_jump_sample.json",
                }));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(manifestDigest), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(handoffDigest), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(OutputRoot,
                "source_navigation_digest_manifest.json")), Does.Contain(
                "\"MAP20_05_handoff_digest\": \"" + handoffDigest + "\""));
            Assert.That((int)publisher.GetProperty("PublicationInvocationCount").GetValue(null),
                Is.EqualTo(1));
            Assert.That((int)publisher.GetProperty("ArtifactWriteCount").GetValue(null),
                Is.EqualTo(3));
            Assert.That((int)publisher.GetProperty("CsvAuthoringWriteCount").GetValue(null), Is.Zero);
            Assert.That(HashAuthoringSources(sourceSample.Index), Is.EqualTo(authoringBefore));
            Assert.That(HashFiles(new[]
            {
                ProjectPath("MapDesign/MCP/GENERATED/MAP20_03/detail_digest_manifest.json"),
                ProjectPath("MapDesign/MCP/GENERATED/MAP20_03/detail_inspection_snapshot.json"),
                ProjectPath("MapDesign/MCP/GENERATED/MAP20_03/pattern_cluster_special_slice_sample.json"),
            }), Is.EqualTo(map2003Before));
        }

        [Test]
        public void CsvNavigationDoesNotSelectLegacyRegressionPriorCategoriesPlayModeOrFullRegression()
        {
            var productionPaths = new[]
            {
                ProjectPath("Assets/_Game/Map/Runtime/WorldGeneration/Tooling/GeneratedCsvNavigationIndex.cs"),
                ProjectPath("Assets/_Game/Map/Runtime/WorldGeneration/Tooling/GeneratedValidationJumpTarget.cs"),
                ProjectPath("Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedCsvNavigationWindow.cs"),
                ProjectPath("Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedCsvNavigationSamplePublisher.cs"),
            };
            var combined = string.Join("\n", productionPaths.Select(File.ReadAllText));
            var window = File.ReadAllText(productionPaths[2]);
            var publisher = File.ReadAllText(productionPaths[3]);

            Assert.That(combined, Does.Not.Contain("19347"));
            Assert.That(combined, Does.Not.Contain("TestRunnerApi"));
            Assert.That(combined, Does.Not.Contain("run_tests"));
            Assert.That(combined, Does.Not.Contain("RunMap19_09ScaleAudit"));
            Assert.That(combined, Does.Not.Contain("GeneratedTerrainRunCoordinator"));
            Assert.That(combined, Does.Not.Contain("GeneratedWorldOverlaySamplePublisher.Publish"));
            Assert.That(combined, Does.Not.Contain("GeneratedDetailInspectorSamplePublisher"));
            Assert.That(combined, Does.Not.Contain("System.Diagnostics.Process"));
            Assert.That(window, Does.Not.Contain("File.Write"));
            Assert.That(window, Does.Not.Contain("Tilemap"));
            Assert.That(window, Does.Not.Contain("Instantiate("));
            Assert.That(window, Does.Not.Contain("Destroy("));
            Assert.That(publisher.Split(new[] { "new Rfc4180CsvReader()" },
                StringSplitOptions.None).Length - 1, Is.EqualTo(1));
            Assert.That(GetType().GetMethods().Count(method => method
                .GetCustomAttributes(typeof(TestAttribute), false).Length == 1), Is.EqualTo(8));
        }

        private static SampleView Sample(string createdUtc)
        {
            var sample = InvokeStatic(EditorType(PublisherTypeName),
                "CreateReadOnlySample", ProjectRoot, createdUtc);
            var type = sample.GetType();
            return new SampleView(
                (GeneratedCsvNavigationIndex)type.GetProperty("Index").GetValue(sample),
                (GeneratedValidationJumpSample)type.GetProperty("JumpSample").GetValue(sample),
                (string)type.GetProperty("Map2005HandoffDigest").GetValue(sample));
        }

        private static string[] HashAuthoringSources(GeneratedCsvNavigationIndex index) =>
            HashFiles(index.SourceRecords.Where(value => value.SourceAvailable)
                .Select(value => ProjectPath(value.CsvFilePath)));

        private static string[] HashExistingOutputFiles() => Directory.Exists(OutputRoot)
            ? HashFiles(Directory.GetFiles(OutputRoot).OrderBy(value => value,
                StringComparer.Ordinal))
            : Array.Empty<string>();

        private static string[] HashFiles(IEnumerable<string> paths) => paths
            .Select(path => path + "=" + HashFile(path)).ToArray();

        private static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(value =>
                    value.ToString("x2", CultureInfo.InvariantCulture)));
        }

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

        private sealed class SampleView
        {
            public SampleView(GeneratedCsvNavigationIndex index,
                GeneratedValidationJumpSample jumpSample, string map2005HandoffDigest)
            {
                Index = index;
                JumpSample = jumpSample;
                Map2005HandoffDigest = map2005HandoffDigest;
            }

            public GeneratedCsvNavigationIndex Index { get; }
            public GeneratedValidationJumpSample JumpSample { get; }
            public string Map2005HandoffDigest { get; }
        }
    }
}
