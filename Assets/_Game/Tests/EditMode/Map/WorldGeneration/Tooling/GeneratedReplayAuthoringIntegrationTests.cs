using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Tooling;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Tooling
{
    [Category(CategoryName)]
    public sealed class GeneratedReplayAuthoringIntegrationTests
    {
        private const string CategoryName = "MAP20_05";
        private const string EditorAssemblyName = "MapAuthoring.Editor";
        private const string PublisherTypeName =
            "StarNight.MapAuthoring.WorldGeneration.Tooling.GeneratedReplayAuthoringSamplePublisher";
        private const string WindowTypeName =
            "StarNight.MapAuthoring.WorldGeneration.Tooling.GeneratedReplayAuthoringWindow";

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string OutputRoot => ProjectPath("MapDesign/MCP/GENERATED/MAP20_05");

        [Test]
        public void ReplayAuthoringBuildsRequestFromNavigationTargetWithoutExecutingReplay()
        {
            var sample = Sample("2026-09-06T00:00:00Z");
            var request = sample.ReplayRequest;

            Assert.That(request.ValidationErrorId,
                Is.EqualTo(sample.NavigationTarget.ValidationErrorId));
            Assert.That(request.JumpTargetKind,
                Is.EqualTo(sample.NavigationTarget.JumpTargetKind));
            Assert.That(request.SelectionPath,
                Is.EqualTo(sample.NavigationTarget.SelectionPath));
            Assert.That(request.ExecutionState, Is.EqualTo("RequestOnly"));
            Assert.That(request.RequestedAction,
                Is.EqualTo(GeneratedReplayRequestedAction.AuthorReplayRequest));
            Assert.That(request.ReplayExecutionCount, Is.Zero);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(request.CanonicalDigest), Is.True);
            Assert.That(request.Serialize(), Does.Contain("\"execution_state\": \"RequestOnly\""));

            var windowType = EditorType(WindowTypeName);
            InvokeStatic(windowType, "ResetInvocationDiagnostics");
            var window = InvokeStatic(windowType, "Open");
            var preview = (GeneratedReplayAuthoringRequest)Invoke(window,
                "CreateRequestOnlyData");
            Assert.That(preview.RequestId, Is.EqualTo(request.RequestId));
            Assert.That((int)windowType.GetProperty("ForbiddenExecutionInvocationCount")
                .GetValue(null), Is.Zero);
            Invoke(window, "Close");
        }

        [Test]
        public void FailureBrowserSeparatesActualEmptyMissingAndFocusedFixtureRecords()
        {
            var browser = Sample("2026-09-06T00:00:00Z").FailureBrowser;

            Assert.That(browser.SourceKindTokens, Is.EqualTo(new[]
                { "ActualFailureBundle", "FocusedFixture", "MissingData" }));
            Assert.That(browser.ActualFailureCount, Is.Zero);
            Assert.That(browser.FocusedFixtureCount, Is.EqualTo(1));
            Assert.That(browser.MissingDataCount, Is.EqualTo(1));
            Assert.That(browser.Records.Single(value => value.SourceKind ==
                GeneratedFailureSourceKind.FocusedFixture).ActualFailureAvailable, Is.False);
            var missing = browser.Records.Single(value => value.SourceKind ==
                GeneratedFailureSourceKind.MissingData);
            Assert.That(missing.SourceDigest, Is.EqualTo("NONE"));
            Assert.That(missing.MissingReason, Is.Not.Empty);
            Assert.That(browser.RepairRerunReplayActionCount, Is.Zero);
            Assert.Throws<ArgumentException>(() => new GeneratedFailureBrowserRecord(
                "bad", GeneratedFailureSourceKind.FocusedFixture, "InMemory/Test",
                BakingCanonicalDigest.HashCanonicalLines(new[] { "fixture" }), "MAP20_05",
                "Fixture", "Error", 1, new GeneratedNavigationCoordinate(0, 0),
                new GeneratedNavigationCoordinate(0, 0), "ERR", "Selection", "REQ",
                true, "Focused", string.Empty));
        }

        [Test]
        public void SeedBundleExportIncludesSeedVersionHashesPassesNavigationFailuresAndHud()
        {
            var bundle = Sample("2026-09-06T00:00:00Z").SeedBundle;
            var json = bundle.Serialize();
            var requiredFields = new[]
            {
                "schema_version", "task_id", "source_MAP20_04_result_digest",
                "source_MAP20_04_task_digest", "source_MAP20_05_handoff_digest", "seed",
                "world_id", "sector_coordinate", "generator_version", "pass_digest_summary",
                "csv_navigation_index_digest", "validation_jump_sample_digest",
                "failure_browser_digest", "replay_request_digest", "runtime_hud_digest",
                "map07_fixed_generated_split_digest", "map08_boundary_link_digest",
                "created_utc_excluded_from_canonical_digest", "canonical_digest",
            };

            Assert.That(requiredFields.All(field => json.Contains("\"" + field + "\"")),
                Is.True);
            Assert.That(bundle.SourceMap2004ResultDigest,
                Is.EqualTo(GeneratedReplayAuthoringPreconditions.SourceMap2004ResultDigest));
            Assert.That(bundle.SourceMap2004TaskDigest,
                Is.EqualTo(GeneratedReplayAuthoringPreconditions.SourceMap2004TaskDigest));
            Assert.That(bundle.SourceMap2005HandoffDigest,
                Is.EqualTo(GeneratedReplayAuthoringPreconditions.SourceMap2005HandoffDigest));
            Assert.That(bundle.ProductionSeedApprovalCount, Is.Zero);
            Assert.That(bundle.GeneratorValidatorReplayRollbackExecutionCount, Is.Zero);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(bundle.CanonicalDigest), Is.True);
        }

        [Test]
        public void Map07FixedGeneratedSplitUsesExactOriginTokensWithoutSourceRewrites()
        {
            var split = Sample("2026-09-06T00:00:00Z").FixedGeneratedSplit;

            Assert.That(split.OriginTokens, Is.EqualTo(new[]
            {
                "FixedMap07", "GeneratedMap16Slice", "GeneratedMap17Runtime",
                "GeneratedMap18Population", "MissingData",
            }));
            Assert.That(split.FixedOriginCount, Is.EqualTo(1));
            Assert.That(split.GeneratedOriginCount, Is.EqualTo(3));
            Assert.That(split.MissingOriginCount, Is.EqualTo(1));
            Assert.That(split.SourceFilesRewrittenCount, Is.Zero);
            Assert.That(split.Records.Where(value => value.Origin !=
                GeneratedContentOrigin.MissingData).All(value =>
                BakingCanonicalDigest.IsLowerHexSha256(value.SourceDigest)), Is.True);
        }

        [Test]
        public void Map08BoundaryLinksPreserveApprovedPairCandidateProjectionIdentity()
        {
            var catalog = Sample("2026-09-06T00:00:00Z").BoundaryLinks;
            var expectedPairs = new[]
            {
                MoonpalaceCraterRootBoundaryAuthoringContract.PairRuleId,
                MoonpalaceCraterMillBoundaryAuthoringContract.PairRuleId,
                MoonpalaceCraterDoughBoundaryAuthoringContract.PairRuleId,
                MoonpalaceRootMillBoundaryAuthoringContract.PairRuleId,
                MoonpalaceRootDoughBoundaryAuthoringContract.PairRuleId,
                MoonpalaceMillDoughBoundaryAuthoringContract.PairRuleId,
            }.OrderBy(value => value, StringComparer.Ordinal).ToArray();

            Assert.That(catalog.ApprovedPairCapacity, Is.EqualTo(6));
            Assert.That(catalog.Links.Select(value => value.PairId), Is.EqualTo(expectedPairs));
            Assert.That(catalog.Links.All(value =>
                !string.IsNullOrWhiteSpace(value.CandidateId) &&
                !string.IsNullOrWhiteSpace(value.ProjectionId) &&
                !string.IsNullOrWhiteSpace(value.SocketId) &&
                BakingCanonicalDigest.IsLowerHexSha256(value.SourceDigest) &&
                BakingCanonicalDigest.IsLowerHexSha256(value.CanonicalDigest)), Is.True);
            Assert.That(catalog.BoundaryRegenerationExecutionCount, Is.Zero);
            Assert.That(catalog.PdfFourPairSubsetUsedAsSourceOfTruth, Is.False);
        }

        [Test]
        public void RuntimeHudStateFormatsSeedSectorPassAndSelectionWithoutRuntimeMutation()
        {
            var state = Sample("2026-09-06T00:00:00Z").RuntimeHud;
            var hud = new GeneratedRuntimeDebugHud(state);
            var json = state.Serialize();

            Assert.That(hud.FormatLines().Count, Is.EqualTo(5));
            Assert.That(hud.FormatLines()[0], Does.Contain(state.Seed.ToString(
                CultureInfo.InvariantCulture)));
            Assert.That(hud.FormatLines()[1], Does.Contain("Sector"));
            Assert.That(hud.FormatLines()[2], Does.Contain(state.ActivePass));
            Assert.That(hud.AutoSpawnPathCount, Is.Zero);
            Assert.That(hud.ScenePrefabWiringCount, Is.Zero);
            Assert.That(hud.RuntimeMutationCount, Is.Zero);
            Assert.That(json, Does.Contain("\"runtime_mutation_count\": 0"));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(state.CanonicalDigest), Is.True);
        }

        [Test]
        public void SeedBundleExporterWritesOnlyUnderMap20_05GeneratedOutput()
        {
            var publisher = EditorType(PublisherTypeName);
            var path = (string)InvokeStatic(publisher, "ExportSeedBundleSample",
                ProjectRoot, true);
            var output = Path.GetFullPath(OutputRoot) + Path.DirectorySeparatorChar;

            Assert.That(Path.GetFullPath(path).StartsWith(output,
                StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(Path.GetFileName(path), Is.EqualTo("seed_bundle_export_sample.json"));
            Assert.That(File.Exists(path), Is.True);
            Assert.That(Directory.GetFiles(OutputRoot, "*.csv", SearchOption.AllDirectories),
                Is.Empty);
            Assert.That((int)publisher.GetProperty("CsvAuthoringWriteCount").GetValue(null),
                Is.Zero);
            Assert.That((int)publisher.GetProperty("GeneratorExecutionCount").GetValue(null),
                Is.Zero);
            Assert.That((int)publisher.GetProperty("ValidationExecutionCount").GetValue(null),
                Is.Zero);
            Assert.That((int)publisher.GetProperty("ReplayExecutionCount").GetValue(null),
                Is.Zero);
            Assert.That((int)publisher.GetProperty("RollbackExecutionCount").GetValue(null),
                Is.Zero);
        }

        [Test]
        public void ReplayAuthoringDigestsAreStableAcrossRepeatCultureAndInputOrder()
        {
            var original = Sample("2026-09-06T00:00:00Z");
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var repeated = Sample("DIFFERENT-CREATED-UTC");
                var reverseBrowser = new GeneratedFailureBrowserIndex(
                    original.FailureBrowser.Records.Reverse(), "DIFFERENT-CREATED-UTC");
                var reverseSplit = new GeneratedMap07FixedGeneratedSplit(
                    original.FixedGeneratedSplit.Records.Reverse());
                var reverseLinks = new GeneratedMap08BoundaryLinkCatalog(
                    original.BoundaryLinks.Links.Reverse());

                Assert.That(repeated.ReplayRequest.CanonicalDigest,
                    Is.EqualTo(original.ReplayRequest.CanonicalDigest));
                Assert.That(repeated.FailureBrowser.CanonicalDigest,
                    Is.EqualTo(original.FailureBrowser.CanonicalDigest));
                Assert.That(repeated.RuntimeHud.CanonicalDigest,
                    Is.EqualTo(original.RuntimeHud.CanonicalDigest));
                Assert.That(repeated.SeedBundle.CanonicalDigest,
                    Is.EqualTo(original.SeedBundle.CanonicalDigest));
                Assert.That(reverseBrowser.CanonicalDigest,
                    Is.EqualTo(original.FailureBrowser.CanonicalDigest));
                Assert.That(reverseSplit.CanonicalDigest,
                    Is.EqualTo(original.FixedGeneratedSplit.CanonicalDigest));
                Assert.That(reverseLinks.CanonicalDigest,
                    Is.EqualTo(original.BoundaryLinks.CanonicalDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [Test]
        public void ReplayAuthoringPublishesMap20_06HandoffOnlyAfterFocusedPass()
        {
            var publisher = EditorType(PublisherTypeName);
            InvokeStatic(publisher, "ResetDiagnostics");
            var unusedRoot = Path.Combine(Path.GetTempPath(),
                "map20_05_no_publish_" + Guid.NewGuid().ToString("N"));
            Assert.Throws<InvalidOperationException>(() =>
                InvokeStatic(publisher, "PublishSamples", unusedRoot, false));
            Assert.That(Directory.Exists(unusedRoot), Is.False);
            var authoringBefore = HashAuthoringCsvFiles();

            var publication = InvokeStatic(publisher, "PublishSamples", ProjectRoot, true);
            var type = publication.GetType();
            var handoff = (string)type.GetProperty("Map2006HandoffDigest").GetValue(publication);
            var manifestDigest = (string)type.GetProperty("ManifestDigest").GetValue(publication);

            Assert.That(Directory.GetFiles(OutputRoot).Select(Path.GetFileName)
                .OrderBy(value => value, StringComparer.Ordinal), Is.EqualTo(new[]
                {
                    "failure_browser_index.json", "replay_authoring_digest_manifest.json",
                    "replay_authoring_request_sample.json", "runtime_hud_state_sample.json",
                    "seed_bundle_export_sample.json",
                }));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(handoff), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(manifestDigest), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(OutputRoot,
                "replay_authoring_digest_manifest.json")), Does.Contain(
                "\"MAP20_06_handoff_digest\": \"" + handoff + "\""));
            Assert.That((int)publisher.GetProperty("PublicationInvocationCount").GetValue(null),
                Is.EqualTo(1));
            Assert.That((int)publisher.GetProperty("ArtifactWriteCount").GetValue(null),
                Is.EqualTo(5));
            Assert.That(HashAuthoringCsvFiles(), Is.EqualTo(authoringBefore));
        }

        [Test]
        public void ReplayAuthoringDoesNotSelectLegacyRegressionPriorCategoriesPlayModeOrFullRegression()
        {
            var productionPaths = new[]
            {
                ProjectPath("Assets/_Game/Map/Runtime/WorldGeneration/Tooling/GeneratedReplayAuthoringIntegration.cs"),
                ProjectPath("Assets/_Game/Map/Runtime/WorldGeneration/Tooling/GeneratedRuntimeDebugHud.cs"),
                ProjectPath("Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedReplayAuthoringWindow.cs"),
                ProjectPath("Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedReplayAuthoringSamplePublisher.cs"),
            };
            var combined = string.Join("\n", productionPaths.Select(File.ReadAllText));
            var publisher = File.ReadAllText(productionPaths[3]);
            var window = File.ReadAllText(productionPaths[2]);

            Assert.That(combined, Does.Not.Contain("19347"));
            Assert.That(combined, Does.Not.Contain("TestRunnerApi"));
            Assert.That(combined, Does.Not.Contain("run_tests"));
            Assert.That(combined, Does.Not.Contain("RunMap19_09ScaleAudit"));
            Assert.That(combined, Does.Not.Contain("GeneratedTerrainRunCoordinator"));
            Assert.That(combined, Does.Not.Contain("GeneratedWorldOverlaySamplePublisher"));
            Assert.That(combined, Does.Not.Contain("GeneratedDetailInspectorSamplePublisher"));
            Assert.That(combined, Does.Not.Contain("GeneratedCsvNavigationSamplePublisher"));
            Assert.That(combined, Does.Not.Contain("Rfc4180CsvReader"));
            Assert.That(combined, Does.Not.Contain("System.Diagnostics.Process"));
            Assert.That(combined, Does.Not.Contain("Instantiate("));
            Assert.That(combined, Does.Not.Contain("Destroy("));
            Assert.That(window, Does.Not.Contain("File.Write"));
            Assert.That(publisher, Does.Not.Contain(".csv\""));
            Assert.That(GetType().GetMethods().Count(method => method
                .GetCustomAttributes(typeof(TestAttribute), false).Length == 1), Is.EqualTo(10));
        }

        private static SampleView Sample(string createdUtc)
        {
            var sample = InvokeStatic(EditorType(PublisherTypeName), "CreateReadOnlySample",
                ProjectRoot, createdUtc, true);
            var type = sample.GetType();
            return new SampleView(
                (GeneratedValidationJumpTarget)type.GetProperty("NavigationTarget").GetValue(sample),
                (GeneratedReplayAuthoringRequest)type.GetProperty("ReplayRequest").GetValue(sample),
                (GeneratedFailureBrowserIndex)type.GetProperty("FailureBrowser").GetValue(sample),
                (GeneratedMap07FixedGeneratedSplit)type.GetProperty("FixedGeneratedSplit").GetValue(sample),
                (GeneratedMap08BoundaryLinkCatalog)type.GetProperty("BoundaryLinks").GetValue(sample),
                (GeneratedRuntimeDebugHudState)type.GetProperty("RuntimeHud").GetValue(sample),
                (GeneratedSeedBundleExportSample)type.GetProperty("SeedBundle").GetValue(sample),
                (string)type.GetProperty("Map2006HandoffDigest").GetValue(sample));
        }

        private static string[] HashAuthoringCsvFiles()
        {
            var root = ProjectPath("Assets/_Game/Map/Data/WorldGeneration/Authoring");
            return Directory.GetFiles(root, "*.csv", SearchOption.AllDirectories)
                .OrderBy(value => value, StringComparer.Ordinal)
                .Select(path => path + "=" + HashFile(path)).ToArray();
        }

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
                return target.GetType().GetMethod(method,
                    BindingFlags.Public | BindingFlags.Instance).Invoke(target, arguments);
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
            Assert.That(type, Is.Not.Null, fullName + " must compile into " +
                EditorAssemblyName + ".");
            return type;
        }

        private static string ProjectPath(string relative) => Path.GetFullPath(Path.Combine(
            ProjectRoot, relative.Replace('/', Path.DirectorySeparatorChar)));

        private sealed class SampleView
        {
            public SampleView(GeneratedValidationJumpTarget navigationTarget,
                GeneratedReplayAuthoringRequest replayRequest,
                GeneratedFailureBrowserIndex failureBrowser,
                GeneratedMap07FixedGeneratedSplit fixedGeneratedSplit,
                GeneratedMap08BoundaryLinkCatalog boundaryLinks,
                GeneratedRuntimeDebugHudState runtimeHud,
                GeneratedSeedBundleExportSample seedBundle, string map2006HandoffDigest)
            {
                NavigationTarget = navigationTarget;
                ReplayRequest = replayRequest;
                FailureBrowser = failureBrowser;
                FixedGeneratedSplit = fixedGeneratedSplit;
                BoundaryLinks = boundaryLinks;
                RuntimeHud = runtimeHud;
                SeedBundle = seedBundle;
                Map2006HandoffDigest = map2006HandoffDigest;
            }

            public GeneratedValidationJumpTarget NavigationTarget { get; }
            public GeneratedReplayAuthoringRequest ReplayRequest { get; }
            public GeneratedFailureBrowserIndex FailureBrowser { get; }
            public GeneratedMap07FixedGeneratedSplit FixedGeneratedSplit { get; }
            public GeneratedMap08BoundaryLinkCatalog BoundaryLinks { get; }
            public GeneratedRuntimeDebugHudState RuntimeHud { get; }
            public GeneratedSeedBundleExportSample SeedBundle { get; }
            public string Map2006HandoffDigest { get; }
        }
    }
}
