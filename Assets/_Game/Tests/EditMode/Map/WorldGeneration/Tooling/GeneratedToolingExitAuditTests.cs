using System;
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
    public sealed class GeneratedToolingExitAuditTests
    {
        private const string CategoryName = "MAP20_06";
        private const string EditorAssemblyName = "MapAuthoring.Editor";
        private const string PublisherTypeName =
            "StarNight.MapAuthoring.WorldGeneration.Tooling.GeneratedToolingExitAuditPublisher";

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        [Test]
        public void ToolingExitAuditLoadsMap20_01Through05ArtifactsWithoutRegenerating()
        {
            var before = HashUpstreamArtifacts();
            var bundle = Bundle("2026-09-06T00:00:00Z");
            var after = HashUpstreamArtifacts();

            Assert.That(bundle.ResultFilesRead, Is.EqualTo(5));
            Assert.That(bundle.GeneratedArtifactRootsRead, Is.EqualTo(5));
            Assert.That(bundle.RegeneratedArtifactRoots, Is.Zero);
            Assert.That(bundle.Audit.Map20ResultChain.Count, Is.EqualTo(5));
            Assert.That(bundle.Audit.Map20GeneratedArtifactChain.Count, Is.EqualTo(17));
            Assert.That(bundle.Audit.Map20ResultChain.All(value => value.ReadOnly), Is.True);
            Assert.That(bundle.Audit.Map20GeneratedArtifactChain.All(value => value.ReadOnly),
                Is.True);
            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void CoordinateTraceabilityConnectsWorldSectorCellSourceReplayAndHud()
        {
            var records = Bundle("2026-09-06T00:00:00Z").Audit.CoordinateAuditRecords;

            Assert.That(records.Count, Is.EqualTo(5));
            Assert.That(records.Select(value => value.OwnerTask), Is.EquivalentTo(new[]
                { "MAP20_01", "MAP20_02", "MAP20_03", "MAP20_04", "MAP20_05" }));
            Assert.That(records.Where(value => value.MissingDataIsolated)
                .All(value => value.MissingReason.Length > 0), Is.True);
            var source = records.Single(value => value.OwnerTask == "MAP20_04");
            var replay = records.Single(value => value.OwnerTask == "MAP20_05");
            Assert.That(source.WorldCoordinate, Is.EqualTo("(312,208)"));
            Assert.That(source.SectorCoordinate, Is.EqualTo("(6,6)"));
            Assert.That(source.CellCoordinate, Is.EqualTo("(24,16)"));
            Assert.That(source.SourceLocation, Does.EndWith(":2:4"));
            Assert.That(replay.SelectionPath, Is.EqualTo(source.SelectionPath));
            Assert.That(records.All(value =>
                BakingCanonicalDigest.IsLowerHexSha256(value.EvidenceDigest)), Is.True);
        }

        [Test]
        public void RollbackScopeRecordsAreExplicitBoundedAndNeverExecuted()
        {
            var records = Bundle("2026-09-06T00:00:00Z").Audit.RollbackScopeRecords;

            Assert.That(records.Count, Is.EqualTo(4));
            Assert.That(records.Select(value => value.ScopeToken), Is.EquivalentTo(new[]
                { "Pattern", "Sector", "OneRing", "World" }));
            Assert.That(records.All(value => value.BoundedTargetCount >= 1 &&
                value.BoundedTargetCount <= value.MaximumTargetCount), Is.True);
            Assert.That(records.Single(value => value.ScopeToken == "OneRing")
                .MaximumTargetCount, Is.EqualTo(9));
            Assert.That(records.Single(value => value.ScopeToken == "World")
                .ExplicitWorldConfirmation, Is.True);
            Assert.That(records.Where(value => value.ScopeToken != "World")
                .All(value => !value.ExplicitWorldConfirmation), Is.True);
            Assert.That(records.All(value => !value.WholeProjectExpansionAllowed &&
                value.RollbackExecutionCount == 0 && value.AuditState == "ReadOnlyRequest"),
                Is.True);
        }

        [Test]
        public void SourceNavigationPreservesExactCsvLocationAndInspectorTargets()
        {
            var records = Bundle("2026-09-06T00:00:00Z").Audit.SourceNavigationRecords;

            Assert.That(records.Count, Is.EqualTo(6));
            Assert.That(records.Select(value => value.JumpTargetKind).Distinct(),
                Is.EquivalentTo(new[] { "Tile", "Pattern", "Cluster", "Socket", "Slot" }));
            Assert.That(records.Where(value => value.SourceAvailable).Count(), Is.EqualTo(5));
            Assert.That(records.Where(value => value.SourceAvailable).All(value =>
                value.CsvFilePath.EndsWith(".csv", StringComparison.Ordinal) &&
                value.RowNumber1Based > 0 && value.ColumnNumber1Based > 0 &&
                value.ColumnName.Length > 0 && value.FieldName.Length > 0 &&
                value.SelectionPath.Length > 0), Is.True);
            var missing = records.Single(value => !value.SourceAvailable);
            Assert.That(missing.JumpTargetKind, Is.EqualTo("Pattern"));
            Assert.That(missing.RowNumber1Based, Is.Zero);
            Assert.That(missing.ColumnNumber1Based, Is.Zero);
            Assert.That(missing.MissingReason, Is.Not.Empty);
        }

        [Test]
        public void ReplayAndSeedBundleHashesAreRequestOnlyAndDeterministic()
        {
            var records = Bundle("2026-09-06T00:00:00Z").Audit.ReplayHashRecords;

            Assert.That(records.Count, Is.EqualTo(4));
            Assert.That(records.Single(value => value.RecordId == "ReplayRequest")
                .ExecutionState, Is.EqualTo("RequestOnly"));
            Assert.That(records.Single(value => value.RecordId == "SeedBundle")
                .ExecutionState, Is.EqualTo("ReadOnlyExportSample"));
            Assert.That(records.All(value => value.RepeatStable && value.CultureStable &&
                value.InputOrderStable && value.ReplayExecutionCount == 0), Is.True);
            Assert.That(records.All(value =>
                BakingCanonicalDigest.IsLowerHexSha256(value.CanonicalDigest)), Is.True);
        }

        [Test]
        public void RuntimeHudExitProofShowsDisplayOnlyAndNoWiring()
        {
            var record = Bundle("2026-09-06T00:00:00Z").Audit.HudStateRecords.Single();

            Assert.That(record.StateKind, Is.EqualTo("DisplayOnly"));
            Assert.That(record.AutoSpawnPathCount, Is.Zero);
            Assert.That(record.RuntimeMutationCount, Is.Zero);
            Assert.That(record.ScenePrefabWiringCount, Is.Zero);
            Assert.That(record.SelectedValidationErrorId, Is.EqualTo("MAP20_04_TILE_SOURCE"));
            Assert.That(record.SelectedFailureRecordId, Is.Not.Empty);
            Assert.That(record.SelectedSelectionPath, Does.Contain("Sector(6,6)/Cell(24,16)"));
        }

        [Test]
        public void ThreeClickAccessPathsReachAllPrimaryToolingSurfaces()
        {
            var access = Bundle("2026-09-06T00:00:00Z").AccessAudit;
            var required = new[]
            {
                "GeneratorWindowToPassArtifact", "GeneratorWindowToRollbackScopePreview",
                "WorldOverlayToSectorCell", "SectorCanvasToDetailInspector",
                "ValidationErrorToCsvSource", "ValidationErrorToInspectorTarget",
                "FailureBrowserToReplayRequest", "ReplayRequestToSeedBundle",
                "HudPreviewToSelectedFailureOrValidation", "SeedBundleToDigestManifest",
            };

            Assert.That(access.AccessPathRecords.Count, Is.EqualTo(10));
            Assert.That(access.AccessPathRecords.Select(value => value.AccessPathId),
                Is.EquivalentTo(required));
            Assert.That(access.MaximumUserActionCount, Is.LessThanOrEqualTo(3));
            Assert.That(access.AccessPathRecords.All(value => value.PassesThreeClickLimit &&
                value.UserActionCount == value.Actions.Count), Is.True);
        }

        [Test]
        public void ToolingExitAuditRejectsUnsafeRunFixApproveActions()
        {
            var publisher = EditorType(PublisherTypeName);
            var audit = Bundle("2026-09-06T00:00:00Z").Audit;
            var unusedRoot = Path.Combine(Path.GetTempPath(),
                "map20_06_no_publish_" + Guid.NewGuid().ToString("N"));

            Assert.Throws<InvalidOperationException>(() =>
                InvokeStatic(publisher, "PublishAudit", unusedRoot, false));
            Assert.That(Directory.Exists(unusedRoot), Is.False);
            Assert.That(audit.UnsafeActionButtonCount, Is.Zero);
            Assert.That(audit.GeneratorExecutionCount, Is.Zero);
            Assert.That(audit.ValidationRunnerExecutionCount, Is.Zero);
            Assert.That(audit.ReplayExecutionCount, Is.Zero);
            Assert.That(audit.RollbackExecutionCount, Is.Zero);
            Assert.That(audit.CsvAuthoringWriteCount, Is.Zero);
            Assert.That(audit.RuntimeObjectSpawnCount, Is.Zero);
            Assert.That(StaticInt(publisher, "UnsafeActionButtonCount"), Is.Zero);
            Assert.That(StaticInt(publisher, "ExternalProcessLaunchCount"), Is.Zero);
        }

        [Test]
        public void DigestChainMatchesMap20_05PreconditionsAndPublishesMap21_01Handoff()
        {
            var bundle = Bundle("2026-09-06T00:00:00Z");

            Assert.That(bundle.Audit.SourceMap2005ResultDigest,
                Is.EqualTo(GeneratedToolingExitAuditPreconditions.SourceMap2005ResultDigest));
            Assert.That(bundle.Audit.SourceMap2005TaskDigest,
                Is.EqualTo(GeneratedToolingExitAuditPreconditions.SourceMap2005TaskDigest));
            Assert.That(bundle.Audit.SourceMap2006HandoffDigest,
                Is.EqualTo(GeneratedToolingExitAuditPreconditions.SourceMap2006HandoffDigest));
            Assert.That(bundle.Manifest.ToolingExitAuditDigest,
                Is.EqualTo(bundle.Audit.CanonicalDigest));
            Assert.That(bundle.Manifest.AccessPathAuditDigest,
                Is.EqualTo(bundle.AccessAudit.CanonicalDigest));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                bundle.Manifest.Map20PhaseExitDigest), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                bundle.Manifest.Map2101HandoffDigest), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                bundle.Manifest.CanonicalDigest), Is.True);
        }

        [Test]
        public void ToolingExitAuditSerializesDeterministicallyAcrossRepeatCultureAndInputOrder()
        {
            var original = Bundle("2026-09-06T00:00:00Z", false);
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var repeated = Bundle("DIFFERENT-CREATED-UTC", true);

                Assert.That(repeated.Audit.CanonicalDigest,
                    Is.EqualTo(original.Audit.CanonicalDigest));
                Assert.That(repeated.AccessAudit.CanonicalDigest,
                    Is.EqualTo(original.AccessAudit.CanonicalDigest));
                Assert.That(repeated.Manifest.Map20PhaseExitDigest,
                    Is.EqualTo(original.Manifest.Map20PhaseExitDigest));
                Assert.That(repeated.Manifest.Map2101HandoffDigest,
                    Is.EqualTo(original.Manifest.Map2101HandoffDigest));
                Assert.That(repeated.Manifest.CanonicalDigest,
                    Is.EqualTo(original.Manifest.CanonicalDigest));
                Assert.That(repeated.Audit.Serialize(), Does.Contain(
                    "\"created_utc_excluded_from_canonical_digest\": true"));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [Test]
        public void ToolingExitAuditRecordsAllExitInvariantsWithOwnerAndEvidence()
        {
            var records = Bundle("2026-09-06T00:00:00Z").Audit.ExitInvariantRecords;
            var required = new[]
            {
                "CoordinateTraceability", "BoundedRollbackScope",
                "SourceNavigationExactLocation", "InspectorJumpTargetCoverage",
                "ReplayRequestHashDeterminism", "SeedBundleHashDeterminism",
                "HudDisplayOnly", "NoRuntimeOrSceneMutation", "NoAuthoringCsvMutation",
                "ThreeClickAccessCoverage", "NoPriorArtifactRegeneration",
                "NoLegacyRegressionSelection",
            };

            Assert.That(records.Count, Is.EqualTo(12));
            Assert.That(records.Select(value => value.InvariantId), Is.EquivalentTo(required));
            Assert.That(records.All(value => value.Status == "PASS" &&
                value.OwnerTask.Length > 0 && value.EvidenceSource.Length > 0 &&
                BakingCanonicalDigest.IsLowerHexSha256(value.EvidenceDigest)), Is.True);
        }

        [Test]
        public void ToolingExitAuditDoesNotSelectLegacyRegressionPriorCategoriesPlayModeOrFullRegression()
        {
            var publisher = EditorType(PublisherTypeName);
            var productionPaths = new[]
            {
                ProjectPath("Assets/_Game/Map/Runtime/WorldGeneration/Tooling/GeneratedToolingExitAudit.cs"),
                ProjectPath("Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedToolingExitAuditPublisher.cs"),
            };
            var combined = string.Join("\n", productionPaths.Select(File.ReadAllText));

            Assert.That(combined, Does.Not.Contain("TestRunnerApi"));
            Assert.That(combined, Does.Not.Contain("run_tests"));
            Assert.That(combined, Does.Not.Contain("ExecuteMenuItem"));
            Assert.That(combined, Does.Not.Contain("System.Diagnostics.Process"));
            Assert.That(combined, Does.Not.Contain("Instantiate("));
            Assert.That(combined, Does.Not.Contain("Destroy("));
            Assert.That(StaticInt(publisher, "PriorTaskTestSelectionCount"), Is.Zero);
            Assert.That(StaticInt(publisher, "LegacyRegressionSelectionCount"), Is.Zero);
            Assert.That(StaticInt(publisher, "PlayModeSelectionCount"), Is.Zero);
            Assert.That(StaticInt(publisher, "UnfilteredTestSelectionCount"), Is.Zero);
            Assert.That(StaticInt(publisher, "FullRegressionRunCount"), Is.Zero);
            Assert.That(StaticInt(publisher, "Map1909ScaleAuditRerunCount"), Is.Zero);
            Assert.That(GetType().GetMethods().Count(method => method
                .GetCustomAttributes(typeof(TestAttribute), false).Length == 1), Is.EqualTo(12));
        }

        private static BundleView Bundle(string createdUtc, bool reverseInputOrder = false)
        {
            var bundle = InvokeStatic(EditorType(PublisherTypeName), "CreateReadOnlyAudit",
                ProjectRoot, createdUtc, reverseInputOrder);
            var type = bundle.GetType();
            return new BundleView(
                (GeneratedToolingExitAudit)type.GetProperty("Audit").GetValue(bundle),
                (GeneratedToolingAccessPathAudit)type.GetProperty("AccessAudit").GetValue(bundle),
                (GeneratedToolingDigestChainManifest)type.GetProperty("Manifest").GetValue(bundle),
                (int)type.GetProperty("ResultFilesRead").GetValue(bundle),
                (int)type.GetProperty("GeneratedArtifactRootsRead").GetValue(bundle),
                (int)type.GetProperty("RegeneratedArtifactRoots").GetValue(bundle));
        }

        private static string[] HashUpstreamArtifacts() => new[]
            { "MAP20_01", "MAP20_02", "MAP20_03", "MAP20_04", "MAP20_05" }
            .SelectMany(root => Directory.GetFiles(ProjectPath(
                "MapDesign/MCP/GENERATED/" + root), "*.json", SearchOption.AllDirectories))
            .OrderBy(value => value, StringComparer.Ordinal)
            .Select(path => path + "=" + HashFile(path)).ToArray();

        private static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(value =>
                    value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static int StaticInt(Type type, string property) =>
            (int)type.GetProperty(property, BindingFlags.Public | BindingFlags.Static)
                .GetValue(null);

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

        private sealed class BundleView
        {
            public BundleView(GeneratedToolingExitAudit audit,
                GeneratedToolingAccessPathAudit accessAudit,
                GeneratedToolingDigestChainManifest manifest, int resultFilesRead,
                int generatedArtifactRootsRead, int regeneratedArtifactRoots)
            {
                Audit = audit;
                AccessAudit = accessAudit;
                Manifest = manifest;
                ResultFilesRead = resultFilesRead;
                GeneratedArtifactRootsRead = generatedArtifactRootsRead;
                RegeneratedArtifactRoots = regeneratedArtifactRoots;
            }

            public GeneratedToolingExitAudit Audit { get; }
            public GeneratedToolingAccessPathAudit AccessAudit { get; }
            public GeneratedToolingDigestChainManifest Manifest { get; }
            public int ResultFilesRead { get; }
            public int GeneratedArtifactRootsRead { get; }
            public int RegeneratedArtifactRoots { get; }
        }
    }
}
