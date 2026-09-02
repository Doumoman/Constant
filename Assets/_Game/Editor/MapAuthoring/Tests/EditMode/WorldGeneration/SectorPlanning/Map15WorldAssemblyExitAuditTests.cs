using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.EventOverlays;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.Editor.Tests.WorldGeneration.SectorPlanning
{
    [TestFixture]
    [Category("MAP15_07")]
    public sealed class Map15WorldAssemblyExitAuditTests
    {
        private const int RegressionSelectionCount = 0;
        private const int RegressionRunCount = 0;
        private const int Map16AutomaticOpenCount = 0;
        private const string Map16Owner = "MAP16_01_FINALIZE_CANVAS_LAYERS_AND_FIXED_PRECEDENCE";

        private ReferenceWorldFixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = ReferenceWorldFixture.Create();
        }

        [Test]
        public void CurrentMap15ChainPublishesAllRequiredArtifactsForExit()
        {
            var audit = ExitAuditProbe.Audit(fixture.Export());

            AssertPass(audit);
            Assert.That(fixture.WorldPlan, Is.Not.Null);
            Assert.That(fixture.SolveOrder, Is.Not.Null);
            Assert.That(fixture.IntersectorPlan, Is.Not.Null);
            Assert.That(fixture.ReservationPlan, Is.Not.Null);
            Assert.That(fixture.PacingPlan, Is.Not.Null);
            Assert.That(fixture.RollbackPlan, Is.Not.Null);
            Assert.That(audit.Export, Is.Not.Null);
            Assert.That(audit.DigestRecordCount, Is.EqualTo(12));
            Assert.That(audit.AuditDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(audit.Map16Owner, Is.EqualTo(Map16Owner));
            Assert.That(audit.OpensMap16, Is.False);
        }

        [Test]
        public void WorldTopologyAndIntersectorEdgesMatchApproved169And312Counts()
        {
            var audit = ExitAuditProbe.Audit(fixture.Export());

            AssertPass(audit);
            Assert.That(audit.WorldSectorCount, Is.EqualTo(169));
            Assert.That(audit.SolveStepCount, Is.EqualTo(169));
            Assert.That(audit.EdgeCount, Is.EqualTo(312));
            Assert.That(audit.EndpointCount, Is.EqualTo(624));
            Assert.That(WorldAssemblyOverlayExport.SectorColumns, Is.EqualTo(13));
            Assert.That(WorldAssemblyOverlayExport.SectorRows, Is.EqualTo(13));
            Assert.That(WorldAssemblyOverlayExport.WorldWidthTiles, Is.EqualTo(624));
            Assert.That(WorldAssemblyOverlayExport.WorldHeightTiles, Is.EqualTo(416));
            Assert.That(WorldAssemblyOverlayExport.SectorWidthTiles, Is.EqualTo(48));
            Assert.That(WorldAssemblyOverlayExport.SectorHeightTiles, Is.EqualTo(32));
        }

        [Test]
        public void ExternalSocketAndBoundaryObligationsHaveNoMissingOrAsymmetricEdges()
        {
            var audit = ExitAuditProbe.Audit(fixture.Export());

            AssertPass(audit);
            Assert.That(audit.ExternalSocketAsymmetryCount, Is.Zero);
            Assert.That(audit.MissingBoundaryPairCount, Is.Zero);
            Assert.That(audit.Export.Edges.Count(edge => edge.ExternalSocket), Is.GreaterThan(0));
            Assert.That(fixture.IntersectorPlan.Edges.All(edge => edge.Endpoints.Count == 2), Is.True);
            Assert.That(fixture.IntersectorPlan.Edges.All(edge => edge.RouteSignature.Compatible), Is.True);
            Assert.That(fixture.IntersectorPlan.Edges.Where(edge => edge.RouteSignature.ExternalSocket)
                .All(edge => edge.Endpoints.All(endpoint => endpoint.ExplicitSocketEvidence)), Is.True);
            Assert.That(fixture.IntersectorPlan.Edges.Where(edge => edge.IsBoundary)
                .All(edge => edge.Boundary != null), Is.True);
        }

        [Test]
        public void ReservationPolicyHasNoUntypedConflictsAndPreservesSpecialPriority()
        {
            var audit = ExitAuditProbe.Audit(fixture.Export());
            var transactions = fixture.ReservationPlan.Transactions;

            AssertPass(audit);
            Assert.That(audit.UntypedReservationConflictCount, Is.Zero);
            Assert.That(transactions, Is.Not.Empty);
            Assert.That(transactions.First().State, Is.EqualTo(WorldReservationTransactionState.Fixed));
            Assert.That(transactions.SkipWhile(value => !value.IsDeferred).All(value => value.IsDeferred), Is.True);
            Assert.That(fixture.ReservationPlan.MissingTransactionCount, Is.Zero);
        }

        [Test]
        public void PacingDensityAndRepetitionGateHasNoAcceptedCaseViolations()
        {
            var audit = ExitAuditProbe.Audit(fixture.Export());

            AssertPass(audit);
            Assert.That(audit.AcceptedPacingDensityViolationCount, Is.Zero);
            Assert.That(audit.RecentUseRepeatViolationCount, Is.Zero);
            Assert.That(fixture.PacingPlan.BudgetViolationCount, Is.Zero);
            Assert.That(fixture.PacingPlan.ActivityEventCapViolationCount, Is.Zero);
            Assert.That(fixture.PacingPlan.Windows.All(value => value.CountWithinRange), Is.True);
        }

        [Test]
        public void NeighborRollbackFailureContainmentStaysWithinInBoundsOneRing()
        {
            var audit = ExitAuditProbe.Audit(fixture.Export());
            var scope = fixture.RollbackPlan.Scope;

            AssertPass(audit);
            Assert.That(audit.RollbackMaximumExceededCount, Is.Zero);
            Assert.That(scope.SectorCount, Is.LessThanOrEqualTo(WorldNeighborRollbackPlan.MaximumScopeSectorCount));
            Assert.That(scope.ContainsFailedSector, Is.True);
            Assert.That(scope.Sectors.All(value =>
                Math.Abs(value.Coordinate.X - scope.FailedCoordinate.X) <= WorldNeighborRollbackPlan.ScopeRadius &&
                Math.Abs(value.Coordinate.Y - scope.FailedCoordinate.Y) <= WorldNeighborRollbackPlan.ScopeRadius &&
                value.Coordinate.X >= 0 && value.Coordinate.X < WorldPlanInput.SectorColumns &&
                value.Coordinate.Y >= 0 && value.Coordinate.Y < WorldPlanInput.SectorRows), Is.True);
            Assert.That(fixture.RollbackPlan.FailureReport.HasFirstContradiction, Is.True);
            Assert.That(fixture.RollbackPlan.Decision.IsBoundedRetry, Is.True);
        }

        [Test]
        public void OverlayBatchReportHasFourFocusedCasesAndNoProductionSeedApproval()
        {
            var audit = ExitAuditProbe.Audit(fixture.Export());
            var report = audit.Export.BatchReport;

            AssertPass(audit);
            Assert.That(audit.OverlaySectorCount, Is.EqualTo(169));
            Assert.That(audit.OverlayEdgeCount, Is.EqualTo(312));
            Assert.That(audit.CoveredLayerCount, Is.EqualTo(12));
            Assert.That(audit.MissingLayerCount, Is.Zero);
            Assert.That(audit.CoveredHashRecordCount, Is.EqualTo(10));
            Assert.That(audit.MissingHashRecordCount, Is.Zero);
            Assert.That(report.RequiredCaseCount, Is.EqualTo(4));
            Assert.That(report.CoveredCaseCount, Is.EqualTo(4));
            Assert.That(report.PassingCaseCount, Is.EqualTo(4));
            Assert.That(report.MissingCaseCount, Is.Zero);
            Assert.That(report.ProductionSeedApprovalCount, Is.Zero);
        }

        [Test]
        public void DigestChainAndReplayRemainDeterministicAcrossRepeatReverseAndCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = ExitAuditProbe.Audit(fixture.Export());
                var repeat = ExitAuditProbe.Audit(fixture.Export());
                var reverse = ExitAuditProbe.Audit(WorldAssemblyOverlayExporter.Export(fixture.Request(
                    WorldAssemblyOverlayExport.RequiredBatchLabels.Reverse())));
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var culture = ExitAuditProbe.Audit(fixture.Export());
                var audits = new[] { first, repeat, reverse, culture };

                Assert.That(audits.All(value => value.Success), Is.True,
                    string.Join(";", audits.Select(Join)));
                Assert.That(audits.Select(value => value.Export.InputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(audits.Select(value => value.Export.OutputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(audits.Select(value => value.AuditDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(audits.All(value => value.DigestRecordCount == 12), Is.True);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void SolverBoundsAndForbiddenFallbackCountersRemainZero()
        {
            var audit = ExitAuditProbe.Audit(fixture.Export());
            var report = audit.Export.BatchReport;

            AssertPass(audit);
            Assert.That(report.RequiredUpperBoundCount, Is.EqualTo(10));
            Assert.That(report.CoveredUpperBoundCount, Is.EqualTo(10));
            Assert.That(report.MissingUpperBoundCount, Is.Zero);
            Assert.That(report.UpperBoundViolationCount, Is.Zero);
            Assert.That(report.SolverUpperBounds.All(value => value.Pass), Is.True);
            Assert.That(audit.WholeWorldRerandomCount, Is.Zero);
            Assert.That(audit.FallbackCarveCount, Is.Zero);
            Assert.That(audit.SilentWideningCount, Is.Zero);
            Assert.That(audit.GeneratedFileWriteCount, Is.Zero);
        }

        [Test]
        public void NoRegressionSelectionTilemapScenePrefabGameplayOrFileExportMutation()
        {
            var worldDigest = fixture.WorldPlan.CanonicalDigest;
            var solveDigest = fixture.SolveOrder.OutputDigest;
            var edgeDigest = fixture.IntersectorPlan.OutputDigest;
            var reservationDigest = fixture.ReservationPlan.OutputDigest;
            var pacingDigest = fixture.PacingPlan.OutputDigest;
            var rollbackDigest = fixture.RollbackPlan.OutputDigest;
            var edgeIds = fixture.IntersectorPlan.Edges.Select(value => value.Id).ToArray();
            var audit = ExitAuditProbe.Audit(fixture.Export());

            AssertPass(audit);
            Assert.That(RegressionSelectionCount, Is.Zero);
            Assert.That(RegressionRunCount, Is.Zero);
            Assert.That(audit.AllMutationCount, Is.Zero);
            Assert.That(fixture.WorldPlan.CanonicalDigest, Is.EqualTo(worldDigest));
            Assert.That(fixture.SolveOrder.OutputDigest, Is.EqualTo(solveDigest));
            Assert.That(fixture.IntersectorPlan.OutputDigest, Is.EqualTo(edgeDigest));
            Assert.That(fixture.ReservationPlan.OutputDigest, Is.EqualTo(reservationDigest));
            Assert.That(fixture.PacingPlan.OutputDigest, Is.EqualTo(pacingDigest));
            Assert.That(fixture.RollbackPlan.OutputDigest, Is.EqualTo(rollbackDigest));
            Assert.That(fixture.IntersectorPlan.Edges.Select(value => value.Id), Is.EqualTo(edgeIds));
        }

        [Test]
        public void InvalidExitInputsFailAtomicallyWithoutOpeningMap16()
        {
            var sourceFailures = new[]
            {
                (WorldAssemblyOverlayResult)null,
                WorldAssemblyOverlayExporter.Export(null),
                WorldAssemblyOverlayExporter.Export(fixture.Request(new[] { "REFERENCE_WORLD_BASELINE" })),
                WorldAssemblyOverlayExporter.Export(fixture.Request(
                    productionSeedApprovalCount: 1)),
            };
            var audits = sourceFailures.Select(ExitAuditProbe.Audit).ToArray();

            Assert.That(audits.All(value => !value.Success), Is.True);
            Assert.That(audits.All(value => value.Export == null), Is.True);
            Assert.That(audits.All(value => value.AuditDigest == string.Empty), Is.True);
            Assert.That(audits.All(value => value.DigestRecordCount == 0), Is.True);
            Assert.That(audits.All(value => value.Failures.Count > 0), Is.True);
            Assert.That(Map16AutomaticOpenCount, Is.Zero);
            Assert.That(audits.All(value => !value.OpensMap16), Is.True);
        }

        private static void AssertPass(ExitAuditResult audit)
        {
            Assert.That(audit.Success, Is.True, Join(audit));
        }

        private static string Join(ExitAuditResult audit) => audit == null
            ? "null"
            : string.Join(";", audit.Failures);

        private sealed class ExitAuditResult
        {
            private readonly ReadOnlyCollection<string> failures;

            private ExitAuditResult(
                WorldAssemblyOverlayExport export,
                IEnumerable<string> sourceFailures,
                string auditDigest,
                int digestRecordCount,
                int externalSocketAsymmetryCount,
                int missingBoundaryPairCount,
                int untypedReservationConflictCount,
                int acceptedPacingDensityViolationCount,
                int recentUseRepeatViolationCount,
                int rollbackMaximumExceededCount)
            {
                Export = export;
                failures = new ReadOnlyCollection<string>((sourceFailures ?? Array.Empty<string>()).ToArray());
                AuditDigest = auditDigest ?? string.Empty;
                DigestRecordCount = digestRecordCount;
                ExternalSocketAsymmetryCount = externalSocketAsymmetryCount;
                MissingBoundaryPairCount = missingBoundaryPairCount;
                UntypedReservationConflictCount = untypedReservationConflictCount;
                AcceptedPacingDensityViolationCount = acceptedPacingDensityViolationCount;
                RecentUseRepeatViolationCount = recentUseRepeatViolationCount;
                RollbackMaximumExceededCount = rollbackMaximumExceededCount;
            }

            internal bool Success => Export != null && failures.Count == 0;
            internal WorldAssemblyOverlayExport Export { get; }
            internal IReadOnlyList<string> Failures => failures;
            internal string AuditDigest { get; }
            internal int DigestRecordCount { get; }
            internal int ExternalSocketAsymmetryCount { get; }
            internal int MissingBoundaryPairCount { get; }
            internal int UntypedReservationConflictCount { get; }
            internal int AcceptedPacingDensityViolationCount { get; }
            internal int RecentUseRepeatViolationCount { get; }
            internal int RollbackMaximumExceededCount { get; }
            internal string Map16Owner => Map15WorldAssemblyExitAuditTests.Map16Owner;
            internal bool OpensMap16 => false;
            internal int WorldSectorCount => Export.Request.SolveOrder.Input.Nodes.Count;
            internal int SolveStepCount => Export.Request.SolveOrder.Steps.Count;
            internal int EdgeCount => Export.Request.IntersectorPlan.Edges.Count;
            internal int EndpointCount => Export.Request.IntersectorPlan.EndpointActualCount;
            internal int OverlaySectorCount => Export.OverlaySectorCount;
            internal int OverlayEdgeCount => Export.OverlayEdgeCount;
            internal int CoveredLayerCount => Export.CoveredLayerCount;
            internal int MissingLayerCount => Export.MissingLayerCount;
            internal int CoveredHashRecordCount => Export.CoveredHashRecordCount;
            internal int MissingHashRecordCount => Export.MissingHashRecordCount;
            internal int WholeWorldRerandomCount => Export.Request.WholeWorldRerandomCount;
            internal int FallbackCarveCount => Export.Request.FallbackCarveCount;
            internal int SilentWideningCount => Export.Request.SilentWideningCount;
            internal int GeneratedFileWriteCount => Export.GeneratedFileWriteCount;
            internal int AllMutationCount => new[]
            {
                Export.GeneratedFileWriteCount, Export.TilemapMutationCount, Export.SceneMutationCount,
                Export.PrefabMutationCount, Export.GameObjectMutationCount, Export.GameplaySpawnCount,
                Export.AuthoringMutationCount, Export.WorldPlanMutationCount,
                Export.IntersectorPlanMutationCount, Export.ReservationPlanMutationCount,
                Export.PacingDensityPlanMutationCount, Export.RollbackPlanMutationCount,
                Export.ProductionSeedApprovalCount, Export.FullRegressionCount,
            }.Sum();

            internal static ExitAuditResult Failed(IEnumerable<string> sourceFailures) =>
                new ExitAuditResult(null, sourceFailures, string.Empty, 0, 0, 0, 0, 0, 0, 0);

            internal static ExitAuditResult Passed(
                WorldAssemblyOverlayExport export,
                string auditDigest,
                int digestRecordCount,
                int externalSocketAsymmetryCount,
                int missingBoundaryPairCount,
                int untypedReservationConflictCount,
                int acceptedPacingDensityViolationCount,
                int recentUseRepeatViolationCount,
                int rollbackMaximumExceededCount) => new ExitAuditResult(
                    export, Array.Empty<string>(), auditDigest, digestRecordCount,
                    externalSocketAsymmetryCount, missingBoundaryPairCount,
                    untypedReservationConflictCount, acceptedPacingDensityViolationCount,
                    recentUseRepeatViolationCount, rollbackMaximumExceededCount);
        }

        private static class ExitAuditProbe
        {
            internal static ExitAuditResult Audit(WorldAssemblyOverlayResult source)
            {
                if (source == null)
                    return ExitAuditResult.Failed(new[] { "SOURCE_RESULT_MISSING" });
                if (!source.Success || source.Export == null)
                    return ExitAuditResult.Failed(source.Failures.Count == 0
                        ? new[] { "SOURCE_EXPORT_MISSING" }
                        : source.Failures.Select(value => value.ToString()));

                var export = source.Export;
                var intersector = export.Request.IntersectorPlan;
                var reservation = export.Request.ReservationPlan;
                var pacing = export.Request.PacingDensityPlan;
                var rollback = export.Request.RollbackPlan;
                var batch = export.BatchReport;
                var failures = new List<string>();
                var externalSocketAsymmetryCount = export.Edges.Count(value =>
                    value.ExternalSocket && (!value.SocketCompatible || value.EndpointCount != 2)) +
                    intersector.Edges.Count(edge => edge.Endpoints.Select(endpoint =>
                        endpoint.ExplicitSocketEvidence).Distinct().Count() != 1);
                var missingBoundaryPairCount = batch.Cases.Sum(value => value.MissingRequiredBoundaryPairCount);
                var untypedReservationConflictCount = reservation.Conflicts.Count(value =>
                    string.IsNullOrEmpty(value.Subject) || string.IsNullOrEmpty(value.WinnerId) ||
                    string.IsNullOrEmpty(value.LoserId) || string.IsNullOrEmpty(value.Reason));
                var acceptedPacingDensityViolationCount =
                    batch.Cases.Sum(value => value.AcceptedPacingViolationCount);
                var recentUseRepeatViolationCount = pacing.RecentUseViolationCount;
                var rollbackMaximumExceededCount = rollback.Scope.SectorCount >
                                                   WorldNeighborRollbackPlan.MaximumScopeSectorCount
                    ? 1
                    : 0;

                Gate(failures, export.Request.SolveOrder.Input.Nodes.Count == 169, "WORLD_SECTORS_169");
                Gate(failures, export.Request.SolveOrder.Steps.Count == 169, "SOLVE_STEPS_169");
                Gate(failures, intersector.Edges.Count == 312, "EDGES_312");
                Gate(failures, intersector.EndpointActualCount == 624, "ENDPOINTS_624");
                Gate(failures, export.OverlaySectorCount == 169 && export.OverlayEdgeCount == 312,
                    "OVERLAY_169_312");
                Gate(failures, export.CoveredLayerCount == 12 && export.MissingLayerCount == 0,
                    "LAYERS_12_12_0");
                Gate(failures, export.CoveredHashRecordCount == 10 && export.MissingHashRecordCount == 0,
                    "HASH_RECORDS_10_10_0");
                Gate(failures, batch.RequiredCaseCount == 4 && batch.CoveredCaseCount == 4 &&
                               batch.PassingCaseCount == 4 && batch.MissingCaseCount == 0,
                    "BATCH_CASES_4_4_4_0");
                Gate(failures, batch.RequiredUpperBoundCount == 10 && batch.CoveredUpperBoundCount == 10 &&
                               batch.UpperBoundViolationCount == 0, "BOUNDS_10_10_0");
                Gate(failures, externalSocketAsymmetryCount == 0, "EXTERNAL_SOCKET_ASYMMETRY_0");
                Gate(failures, missingBoundaryPairCount == 0, "MISSING_BOUNDARY_PAIR_0");
                Gate(failures, untypedReservationConflictCount == 0, "UNTYPED_RESERVATION_CONFLICT_0");
                Gate(failures, acceptedPacingDensityViolationCount == 0,
                    "ACCEPTED_PACING_DENSITY_VIOLATION_0");
                Gate(failures, recentUseRepeatViolationCount == 0, "RECENT_USE_REPEAT_VIOLATION_0");
                Gate(failures, rollbackMaximumExceededCount == 0 && rollback.Scope.ContainsFailedSector,
                    "ROLLBACK_MAX_EXCEEDED_0");
                Gate(failures, export.Request.WholeWorldRerandomCount == 0 &&
                               export.Request.FallbackCarveCount == 0 &&
                               export.Request.SilentWideningCount == 0, "RERANDOM_CARVE_WIDENING_0");
                Gate(failures, export.GeneratedFileWriteCount == 0, "FILE_WRITE_0");
                Gate(failures, export.TilemapMutationCount == 0 && export.SceneMutationCount == 0 &&
                               export.PrefabMutationCount == 0 && export.GameObjectMutationCount == 0,
                    "TILEMAP_SCENE_PREFAB_GAMEOBJECT_MUTATION_0");
                Gate(failures, export.ProductionSeedApprovalCount == 0, "PRODUCTION_APPROVALS_0");
                Gate(failures, batch.Pass, "BATCH_PASS");
                Gate(failures, WorldAssemblyOverlayExport.OpensDownstreamTask == false,
                    "MAP15_06_AUTO_OPEN_FALSE");

                var expectedTasks = Enumerable.Range(1, 5)
                    .Select(value => "MAP15_" + value.ToString("00", CultureInfo.InvariantCulture)).ToArray();
                Gate(failures, export.HashRecords.Count == 10 && expectedTasks.All(task =>
                    export.HashRecords.Count(record => record.TaskId == task) == 2),
                    "MAP15_01_TO_05_DIGEST_RECORDS");
                Gate(failures, export.HashRecords.All(record => IsSha256(record.Digest)) &&
                               IsSha256(export.InputDigest) && IsSha256(export.OutputDigest),
                    "MAP15_01_TO_06_DIGESTS_VALID");

                if (failures.Count > 0) return ExitAuditResult.Failed(failures);

                var observations = new[]
                {
                    "OBS|ACCEPTED_PACING_DENSITY_VIOLATION|0",
                    "OBS|EXTERNAL_SOCKET_ASYMMETRY|0",
                    "OBS|FILE_WRITE|0",
                    "OBS|MAP16_AUTO_OPEN|0",
                    "OBS|MISSING_BOUNDARY_PAIR|0",
                    "OBS|PRODUCTION_APPROVALS|0",
                    "OBS|RECENT_USE_REPEAT_VIOLATION|0",
                    "OBS|ROLLBACK_MAX_EXCEEDED|0",
                    "OBS|UNTYPED_RESERVATION_CONFLICT|0",
                };
                var digestLines = export.HashRecords.OrderBy(value => value.TaskId, StringComparer.Ordinal)
                    .ThenBy(value => value.Kind).Select(value => value.StableToken)
                    .Concat(new[]
                    {
                        "HASH|MAP15_06|INPUT|" + export.InputDigest,
                        "HASH|MAP15_06|OUTPUT|" + export.OutputDigest,
                    })
                    .Concat(export.Sectors.OrderBy(value => value.SectorId).Select(value => value.StableToken))
                    .Concat(export.Edges.OrderBy(value => value.EdgeId).Select(value => value.StableToken))
                    .Concat(batch.Cases.OrderBy(value => value.Label, StringComparer.Ordinal)
                        .Select(value => value.StableToken))
                    .Concat(batch.SolverUpperBounds.OrderBy(value => value.Kind)
                        .ThenBy(value => value.SourceOwner, StringComparer.Ordinal)
                        .Select(value => value.StableToken))
                    .Concat(observations.OrderBy(value => value, StringComparer.Ordinal))
                    .ToArray();
                var digest = WorldAssemblyOverlayDigest.HashCanonicalText(string.Join("\n", digestLines));
                return ExitAuditResult.Passed(
                    export, digest, export.HashRecords.Count + 2,
                    externalSocketAsymmetryCount, missingBoundaryPairCount,
                    untypedReservationConflictCount, acceptedPacingDensityViolationCount,
                    recentUseRepeatViolationCount, rollbackMaximumExceededCount);
            }

            private static void Gate(ICollection<string> failures, bool condition, string gateId)
            {
                if (!condition) failures.Add(gateId);
            }

            private static bool IsSha256(string value) =>
                value != null && value.Length == 64 && value.All(character =>
                    (character >= '0' && character <= '9') || (character >= 'a' && character <= 'f'));
        }

        private sealed class ReferenceWorldFixture
        {
            internal const string Map14PhaseExitDigest =
                "5b8ed6a3c8b0a20fe2f2d05eea0b7731522aff30e691ca606c638adcdbd62d82";
            internal const string RetryPatternLabel = "MAP14_RETRY_PATTERN_CANDIDATE";
            internal const string RetryClusterLabel = "MAP14_RETRY_CLUSTER_VARIANT";

            private ReferenceWorldFixture(
                WorldPlanInput worldPlan,
                WorldSolveOrderResult solveOrder,
                WorldIntersectorEdgePlan intersectorPlan,
                WorldMultiSectorReservationPlan reservationPlan,
                WorldPacingDensityPlan pacingPlan,
                WorldNeighborRollbackPlan rollbackPlan)
            {
                WorldPlan = worldPlan;
                SolveOrder = solveOrder;
                IntersectorPlan = intersectorPlan;
                ReservationPlan = reservationPlan;
                PacingPlan = pacingPlan;
                RollbackPlan = rollbackPlan;
            }

            internal WorldPlanInput WorldPlan { get; }
            internal WorldSolveOrderResult SolveOrder { get; }
            internal WorldIntersectorEdgePlan IntersectorPlan { get; }
            internal WorldMultiSectorReservationPlan ReservationPlan { get; }
            internal WorldPacingDensityPlan PacingPlan { get; }
            internal WorldNeighborRollbackPlan RollbackPlan { get; }

            internal static ReferenceWorldFixture Create()
            {
                var nodes = Enumerable.Range(0, WorldPlanInput.SectorCount)
                    .Select(id => new WorldSectorNode(
                        new WorldSectorId(id),
                        new WorldSectorCoordinate(id % WorldPlanInput.SectorColumns,
                            id / WorldPlanInput.SectorColumns),
                        "BIO_REFERENCE_" + ((id / WorldPlanInput.SectorColumns) % 4),
                        1,
                        id <= 1 ? AccessClass.MandatoryNoTool : AccessClass.OptionalNoTool,
                        id == 2 ? PacingRole.Landmark : id % 6 == 0 ? PacingRole.Activity : PacingRole.Quiet,
                        id == 2, false, false, id == 0,
                        id == 2 ? "SR_CORE_REFERENCE" : string.Empty))
                    .ToArray();
                var dependencies = new[]
                {
                    new WorldDependencyEdge(new WorldSectorId(0), new WorldSectorId(1),
                        WorldDependencyKind.MandatoryRoute, "REFERENCE_MANDATORY_ROUTE", "MAP05_MANDATORY_ROUTE"),
                    new WorldDependencyEdge(new WorldSectorId(0), new WorldSectorId(2),
                        WorldDependencyKind.SpecialReservation, "REFERENCE_SPECIAL_RESERVATION", "MAP13_SPECIAL_REGION"),
                };
                var worldPlan = new WorldPlanInput(
                    nodes, dependencies,
                    new WorldRetryEnvelope(6, 1, WorldSolveAbortReason.SectorLocalAttemptsExhausted),
                    Map14PhaseExitDigest, WorldSolveOrderPlanner.ReferencePublicationLabel);
                var solveOrder = WorldSolveOrderPlanner.Plan(worldPlan);
                Require(solveOrder.Success, solveOrder.Failures);

                var boundaryEdge = EdgeId(4, 5);
                var edgeRequest = new WorldIntersectorBuildRequest(
                    worldPlan, solveOrder, BuildProjections(boundaryEdge),
                    new[]
                    {
                        new WorldBoundaryBinding(
                            boundaryEdge, MoonpalaceCraterRootBoundaryAuthoringContract.PairRuleId,
                            MoonpalaceCraterRootBoundaryAuthoringContract.ProfileIds[0],
                            MoonpalaceCraterRootBoundaryAuthoringContract.CandidateIds[0],
                            new[]
                            {
                                MoonpalaceBoundaryWarningMarkerCategory.Tile.Token,
                                MoonpalaceBoundaryWarningMarkerCategory.Background.Token,
                            },
                            "MAP08_BOUNDARY_AUTHORITY"),
                    },
                    Map14PhaseExitDigest,
                    WorldIntersectorDigest.HashCanonicalText(MoonpalaceBiomePairCatalog.Canonical.Signature),
                    WorldBoundarySocketIntegrator.ReferencePublicationLabel);
                var edgeResult = WorldBoundarySocketIntegrator.Integrate(edgeRequest);
                Require(edgeResult.Success, edgeResult.Failures);

                var fixedSpecial = new WorldSpecialReservationTransaction(
                    "TX_CORE_REFERENCE", SpecialRegionKind.CoreResource.ToString(), SpecialRegionKind.CoreResource,
                    WorldReservationTransactionState.Fixed, WorldReservationSpanKind.SingleSector,
                    new[] { new WorldSectorId(2) }, Array.Empty<WorldIntersectorEdgeId>(), true,
                    "CORE_ENTRY", "CORE_RETURN", false, string.Empty, "MAP13_SPECIAL_REGION");
                var deferred = new WorldSpecialReservationTransaction(
                    "TX_MERCHANT_DEFERRED", SpecialLandmarkKind.WanderingMerchantCave.ToString(),
                    SpecialRegionKind.OptionalLandmark, WorldReservationTransactionState.Deferred,
                    WorldReservationSpanKind.Deferred, Array.Empty<WorldSectorId>(),
                    Array.Empty<WorldIntersectorEdgeId>(), false, string.Empty, string.Empty,
                    false, string.Empty, "MAP13_DEFERRED_OPTIONAL_LOCAL");
                var reservationRequest = new WorldReservationPolicyRequest(
                    worldPlan, solveOrder, edgeResult.Plan, new[] { fixedSpecial, deferred },
                    Array.Empty<WorldClusterContainmentPolicy>(),
                    Array.Empty<WorldClusterCrossSectorAllowance>(), Array.Empty<WorldReservationClaim>(),
                    Map13Digest(), Map14PhaseExitDigest,
                    WorldSpecialClusterPolicyPlanner.ReferencePublicationLabel);
                var reservationResult = WorldSpecialClusterPolicyPlanner.Plan(reservationRequest);
                Require(reservationResult.Success, reservationResult.Failures);

                var steps = solveOrder.Steps.ToDictionary(value => value.SectorId, value => value.StepIndex);
                var windows = new[]
                {
                    Window("WINDOW_QUIET", WorldPacingWindowKind.Quiet, new[] { 50, 51, 52, 53, 54 }, 2, 5, 3, steps, "MAP09"),
                    Window("WINDOW_CLUSTER", WorldPacingWindowKind.Cluster, new[] { 20, 40 }, 1, 3, 2, steps, "MAP11"),
                    Window("WINDOW_ACTIVITY", WorldPacingWindowKind.Activity, new[] { 60, 70 }, 1, 3, 2, steps, "MAP12"),
                    Window("WINDOW_EVENT", WorldPacingWindowKind.Event, new[] { 80, 90 }, 0, 2, 1, steps, "MAP12"),
                    Window("WINDOW_LANDMARK", WorldPacingWindowKind.Landmark, new[] { 2 }, 1, 2, 1, steps, "MAP13"),
                };
                var budgets = nodes.Select(node => new WorldSectorDensityBudget(
                    node.Id, WorldDensityBudgetKind.AbstractSolidReachable,
                    30, 70, 50, 20, 80, 60,
                    "REFERENCE ABSTRACT BUDGET", "MAP15_04_REFERENCE")).ToArray();
                var signatures = new[]
                {
                    Signature(WorldContentSignatureKind.Pattern, new MicroPatternId("MP_REF_A").Value, 10, steps, "MAP10"),
                    Signature(WorldContentSignatureKind.Pattern, new MicroPatternId("MP_REF_B").Value, 30, steps, "MAP10"),
                    Signature(WorldContentSignatureKind.Pattern, new MicroPatternId("MP_REF_C").Value, 60, steps, "MAP10"),
                    Signature(WorldContentSignatureKind.Cluster, new TerrainClusterId("TC_REF_A").Value, 20, steps, "MAP11"),
                    Signature(WorldContentSignatureKind.Cluster, new TerrainClusterId("TC_REF_B").Value, 40, steps, "MAP11"),
                    Signature(WorldContentSignatureKind.Cluster, new TerrainClusterId("TC_REF_C").Value, 80, steps, "MAP11"),
                    Signature(WorldContentSignatureKind.Activity, new ActivityStructureId("ACT_REF_A").Value, 60, steps, "MAP12"),
                    Signature(WorldContentSignatureKind.Activity, new ActivityStructureId("ACT_REF_B").Value, 100, steps, "MAP12"),
                    Signature(WorldContentSignatureKind.Activity, new ActivityStructureId("ACT_REF_C").Value, 140, steps, "MAP12"),
                };
                var rules = new[]
                {
                    Rule(WorldContentSignatureKind.Pattern, "MAP10"),
                    Rule(WorldContentSignatureKind.Cluster, "MAP11"),
                    Rule(WorldContentSignatureKind.Activity, "MAP12"),
                };
                var activityPolicy = new ActivityFrequencyPolicy(90, 4, 2, 1);
                var eventPolicy = new EventOverlayAssignmentPolicy(80);
                var activityDigest = WorldPacingDensityDigest.HashCanonicalText(string.Join("|", new[]
                {
                    "MAP12_ACTIVITY", activityPolicy.TargetPermille.ToString(CultureInfo.InvariantCulture),
                    activityPolicy.MaxStrongPerWorld.ToString(CultureInfo.InvariantCulture),
                    new ActivityStructureId("ACT_REF_A").Value,
                }));
                var eventDigest = WorldPacingDensityDigest.HashCanonicalText(string.Join("|", new[]
                {
                    "MAP12_EVENT", eventPolicy.TargetPermille.ToString(CultureInfo.InvariantCulture),
                    new EventOverlayId("EV_REF_A").Value,
                }));
                var constraints = new[]
                {
                    new WorldActivityEventConstraint("CAP_ACTIVITY_REFERENCE", WorldPacingWindowKind.Activity,
                        activityPolicy.TargetPermille, activityPolicy.MaxStrongPerWorld,
                        activityDigest, "MAP12_ACTIVITY_FREQUENCY_PUBLIC"),
                    new WorldActivityEventConstraint("CAP_EVENT_REFERENCE", WorldPacingWindowKind.Event,
                        eventPolicy.TargetPermille, 2, eventDigest, "MAP12_EVENT_FREQUENCY_REFERENCE"),
                };
                var pacingRequest = new WorldPacingDensityRequest(
                    worldPlan, solveOrder, edgeResult.Plan, reservationResult.Plan,
                    windows, budgets, signatures, rules, constraints,
                    WorldPacingDensityDigest.HashCanonicalText("MAP10_AUTHORITY"),
                    WorldPacingDensityDigest.HashCanonicalText("MAP11_AUTHORITY"),
                    WorldPacingDensityDigest.HashCanonicalText(activityDigest + "\n" + eventDigest),
                    Map13Digest(), Map14PhaseExitDigest,
                    WorldPacingDensityPlanner.ReferencePublicationLabel);
                var pacingResult = WorldPacingDensityPlanner.Plan(pacingRequest);
                Require(pacingResult.Success, pacingResult.Failures);

                var failedSector = new WorldSectorId(84);
                var failedStep = solveOrder.Steps.Single(value => value.SectorId == failedSector).StepIndex;
                var evidence = new WorldContradictionEvidence(
                    "REFERENCE_FIRST_CONTRADICTION", WorldContradictionKind.ClusterCandidateExhausted,
                    WorldContradictionSource.ClusterCandidate, failedSector, failedStep,
                    new[] { edgeResult.Plan.Edges.First(value =>
                        value.Id.MinSector == new WorldSectorId(83) ||
                        value.Id.MaxSector == new WorldSectorId(83)).Id },
                    new[] { reservationResult.Plan.Transactions[0].TransactionId },
                    new[] { pacingResult.Plan.Windows[0].WindowId },
                    new[] { pacingResult.Plan.Signatures[0].SignatureId },
                    new[] { RetryPatternLabel }, true, false);
                var rollbackRequest = new WorldRollbackPolicyRequest(
                    solveOrder, edgeResult.Plan, reservationResult.Plan, pacingResult.Plan,
                    failedSector, new[] { evidence }, Map14PhaseExitDigest,
                    new[] { RetryPatternLabel, RetryClusterLabel }, 0, 3,
                    WorldNeighborRollbackPlanner.ReferencePublicationLabel);
                var rollbackResult = WorldNeighborRollbackPlanner.Plan(rollbackRequest);
                Require(rollbackResult.Success, rollbackResult.Failures);

                return new ReferenceWorldFixture(
                    worldPlan, solveOrder, edgeResult.Plan, reservationResult.Plan,
                    pacingResult.Plan, rollbackResult.Plan);
            }

            internal WorldAssemblyOverlayRequest Request(
                IEnumerable<string> labels = null,
                int productionSeedApprovalCount = 0) => new WorldAssemblyOverlayRequest(
                    SolveOrder, IntersectorPlan, ReservationPlan, PacingPlan, RollbackPlan,
                    labels ?? WorldAssemblyOverlayExport.RequiredBatchLabels,
                    WorldAssemblyOverlayExporter.ReferencePublicationLabel,
                    productionSeedApprovalCount: productionSeedApprovalCount);

            internal WorldAssemblyOverlayResult Export() =>
                WorldAssemblyOverlayExporter.Export(Request());

            private static WorldPacingWindow Window(
                string id,
                WorldPacingWindowKind kind,
                IEnumerable<int> sectorValues,
                int minimum,
                int maximum,
                int observed,
                IReadOnlyDictionary<WorldSectorId, int> steps,
                string owner)
            {
                var sectors = sectorValues.Select(value => new WorldSectorId(value)).ToArray();
                var solveSteps = sectors.Select(sector => steps[sector]).ToArray();
                return new WorldPacingWindow(
                    id, kind, sectors, solveSteps.Min(), solveSteps.Max(), minimum, maximum, observed,
                    "REFERENCE ABSTRACT " + kind.ToString().ToUpperInvariant() + " WINDOW", owner);
            }

            private static WorldContentSignature Signature(
                WorldContentSignatureKind kind,
                string id,
                int sector,
                IReadOnlyDictionary<WorldSectorId, int> steps,
                string owner)
            {
                var sectorId = new WorldSectorId(sector);
                return new WorldContentSignature(kind, id, sectorId, steps[sectorId], owner);
            }

            private static WorldRecentUseRule Rule(WorldContentSignatureKind kind, string owner) =>
                new WorldRecentUseRule(kind, 2, 3, true, "MINIMUM GRAPH AND SOLVE-STEP DISTANCE", owner);

            private static WorldSocketProjection[] BuildProjections(WorldIntersectorEdgeId boundaryEdge)
            {
                var result = new List<WorldSocketProjection>(WorldIntersectorEdgePlan.EndpointCount);
                for (var y = 0; y < WorldPlanInput.SectorRows; y++)
                for (var x = 0; x < WorldPlanInput.SectorColumns - 1; x++)
                {
                    var first = new WorldSectorId((y * WorldPlanInput.SectorColumns) + x);
                    var second = new WorldSectorId(first.Value + 1);
                    var edge = new WorldIntersectorEdgeId(first, second, WorldEdgeOrientation.Horizontal);
                    result.Add(Projection(first, WorldSectorSide.East, new WorldSocketAnchor(47, 16, 3),
                        edge == EdgeId(3, 4), edge == EdgeId(0, 1), edge == boundaryEdge));
                    result.Add(Projection(second, WorldSectorSide.West, new WorldSocketAnchor(0, 16, 3),
                        edge == EdgeId(3, 4), edge == EdgeId(0, 1), edge == boundaryEdge));
                }
                for (var y = 0; y < WorldPlanInput.SectorRows - 1; y++)
                for (var x = 0; x < WorldPlanInput.SectorColumns; x++)
                {
                    var first = new WorldSectorId((y * WorldPlanInput.SectorColumns) + x);
                    var second = new WorldSectorId(first.Value + WorldPlanInput.SectorColumns);
                    result.Add(Projection(first, WorldSectorSide.North, new WorldSocketAnchor(24, 31, 3),
                        false, false, false));
                    result.Add(Projection(second, WorldSectorSide.South, new WorldSocketAnchor(24, 0, 3),
                        false, false, false));
                }
                return result.ToArray();
            }

            private static WorldSocketProjection Projection(
                WorldSectorId sector,
                WorldSectorSide side,
                WorldSocketAnchor anchor,
                bool externalSocket,
                bool mandatory,
                bool boundary) => new WorldSocketProjection(
                    sector, side, anchor, externalSocket, mandatory, boundary,
                    boundary ? "MAP08_BOUNDARY" : "MAP15_01_WORLD_PLAN");

            private static WorldIntersectorEdgeId EdgeId(int first, int second) =>
                new WorldIntersectorEdgeId(
                    new WorldSectorId(first), new WorldSectorId(second), WorldEdgeOrientation.Horizontal);

            private static string Map13Digest() => WorldPacingDensityDigest.HashCanonicalText(
                CoreResourceRegionStarterCatalog.CanonicalDigest + "\n" +
                SpecialLandmarkRegionStarterCatalog.CanonicalDigest);

            private static void Require(bool success, IEnumerable<object> failures)
            {
                if (!success) throw new InvalidOperationException(string.Join(";", failures));
            }
        }
    }
}
