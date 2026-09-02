using System;
using System.Collections.Generic;
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

namespace StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
{
    [TestFixture]
    [Category("MAP15_05")]
    public sealed class WorldNeighborRollbackPlannerTests
    {
        private ReferenceRollbackFixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = ReferenceRollbackFixture.Create();
        }

        [Test]
        public void RollbackPlanPublishesScopeReportDecisionAndDigests()
        {
            var result = fixture.Plan();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(result.Plan.ObservedWorldSectorCount, Is.EqualTo(169));
            Assert.That(result.Plan.ObservedInternalEdgeCount, Is.EqualTo(312));
            Assert.That(result.Plan.ReservationPlanIdentity, Is.EqualTo(fixture.ReservationPlan.OutputDigest));
            Assert.That(result.Plan.PacingDensityPlanIdentity, Is.EqualTo(fixture.PacingPlan.OutputDigest));
            Assert.That(result.Plan.Scope.Kind, Is.EqualTo(WorldRollbackScopeKind.Interior));
            Assert.That(result.Plan.Scope.SectorCount, Is.EqualTo(9));
            Assert.That(result.Plan.Scope.Sectors[0].IsFailedSector, Is.True);
            Assert.That(result.Plan.FailureReport.HasFirstContradiction, Is.True);
            Assert.That(result.Plan.Decision.Kind, Is.EqualTo(WorldRollbackDecisionKind.BoundedRetry));
            Assert.That(result.InputDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(result.OutputDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(() => ((IList<WorldRollbackSector>)result.Plan.Scope.Sectors)
                    .Add(result.Plan.Scope.Sectors[0]),
                Throws.TypeOf<NotSupportedException>());

            TestContext.Out.WriteLine("MAP15_05_INPUT_DIGEST=" + result.InputDigest);
            TestContext.Out.WriteLine("MAP15_05_OUTPUT_DIGEST=" + result.OutputDigest);
            TestContext.Out.WriteLine("MAP15_05_SCOPE_COUNTS=4,6,9;MAX=9");
        }

        [Test]
        public void CornerEdgeAndInteriorFailuresUseOnlyInBoundsOneRingScopes()
        {
            var corner = fixture.Plan(0);
            var edge = fixture.Plan(6);
            var interior = fixture.Plan(84);

            Assert.That(new[] { corner, edge, interior }.All(value => value.Success), Is.True,
                string.Join(";", new[] { corner, edge, interior }.Select(Join)));
            Assert.That(corner.Plan.Scope.Kind, Is.EqualTo(WorldRollbackScopeKind.Corner));
            Assert.That(corner.Plan.Scope.SectorCount, Is.EqualTo(4));
            Assert.That(edge.Plan.Scope.Kind, Is.EqualTo(WorldRollbackScopeKind.Edge));
            Assert.That(edge.Plan.Scope.SectorCount, Is.EqualTo(6));
            Assert.That(interior.Plan.Scope.Kind, Is.EqualTo(WorldRollbackScopeKind.Interior));
            Assert.That(interior.Plan.Scope.SectorCount, Is.EqualTo(9));
            Assert.That(new[] { corner, edge, interior }.SelectMany(value => value.Plan.Scope.Sectors)
                .All(value => value.Coordinate.IsInBounds), Is.True);
        }

        [Test]
        public void RollbackScopeNeverExceedsFailedSectorPlusOneRing()
        {
            foreach (var failed in new[] { 0, 6, 84, 168 })
            {
                var result = fixture.Plan(failed);
                Assert.That(result.Success, Is.True, Join(result));
                Assert.That(result.Plan.Scope.SectorCount, Is.LessThanOrEqualTo(9));
                Assert.That(result.Plan.Scope.ContainsFailedSector, Is.True);
                Assert.That(result.Plan.Scope.Sectors[0].SectorId, Is.EqualTo(new WorldSectorId(failed)));
                Assert.That(result.Plan.Scope.Sectors.All(value =>
                    Math.Abs(value.Coordinate.X - result.Plan.Scope.FailedCoordinate.X) <= 1 &&
                    Math.Abs(value.Coordinate.Y - result.Plan.Scope.FailedCoordinate.Y) <= 1), Is.True);
                var expectedTail = result.Plan.Scope.Sectors.Skip(1)
                    .OrderBy(value => value.SolveStepIndex).ThenBy(value => value.SectorId).ToArray();
                Assert.That(result.Plan.Scope.Sectors.Skip(1), Is.EqualTo(expectedTail));
            }
        }

        [Test]
        public void FirstContradictionIsChosenBySolveStepAndSourcePriority()
        {
            var observations = new[]
            {
                fixture.Evidence("LATE_SPECIAL", WorldContradictionKind.SpecialConflict,
                    WorldContradictionSource.Special, 84),
                fixture.Evidence("EARLY_RETRY", WorldContradictionKind.RetryExhausted,
                    WorldContradictionSource.Retry, 0),
                fixture.Evidence("EARLY_SPECIAL_Z", WorldContradictionKind.SpecialConflict,
                    WorldContradictionSource.Special, 0),
                fixture.Evidence("EARLY_SPECIAL_A", WorldContradictionKind.SpecialConflict,
                    WorldContradictionSource.Special, 0),
            };
            var forward = WorldNeighborRollbackPlanner.Plan(fixture.Request(observations: observations));
            var reverse = WorldNeighborRollbackPlanner.Plan(fixture.Request(observations: observations.Reverse()));

            Assert.That(forward.Success, Is.True, Join(forward));
            Assert.That(reverse.Success, Is.True, Join(reverse));
            Assert.That(forward.Plan.FailureReport.FirstContradiction.StableContradictionId,
                Is.EqualTo("EARLY_SPECIAL_A"));
            Assert.That(forward.Plan.FailureReport.FirstContradiction.SolveStepIndex,
                Is.EqualTo(fixture.Step(0)));
            Assert.That(forward.OutputDigest, Is.EqualTo(reverse.OutputDigest));
        }

        [Test]
        public void FailureReportLinksEdgesReservationsPacingCandidatesAndRetryEvidence()
        {
            var edgeId = fixture.IntersectorPlan.Edges[0].Id;
            var reservationId = fixture.ReservationPlan.Transactions[0].TransactionId;
            var pacingId = fixture.PacingPlan.Windows[0].WindowId;
            var candidateId = fixture.PacingPlan.Signatures[0].SignatureId;
            var evidence = fixture.Evidence(
                "FULL_LINKED_EVIDENCE", WorldContradictionKind.ClusterCandidateExhausted,
                WorldContradictionSource.ClusterCandidate, 84,
                new[] { edgeId }, new[] { reservationId }, new[] { pacingId },
                new[] { candidateId }, new[] { ReferenceRollbackFixture.RetryPatternLabel });
            var result = WorldNeighborRollbackPlanner.Plan(fixture.Request(observations: new[] { evidence }));

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(result.Plan.FailureReport.RelatedEdgeIds, Is.EqualTo(new[] { edgeId }));
            Assert.That(result.Plan.FailureReport.RelatedReservationIds, Is.EqualTo(new[] { reservationId }));
            Assert.That(result.Plan.FailureReport.RelatedPacingEvidenceIds, Is.EqualTo(new[] { pacingId }));
            Assert.That(result.Plan.FailureReport.RelatedCandidateIds, Is.EqualTo(new[] { candidateId }));
            Assert.That(result.Plan.FailureReport.RetryLabels,
                Is.EqualTo(new[] { ReferenceRollbackFixture.RetryPatternLabel }));
        }

        [Test]
        public void RollbackDecisionRejectsWholeWorldRerandomFallbackCarveAndSilentWidening()
        {
            var bounded = fixture.Plan();
            var aborted = WorldNeighborRollbackPlanner.Plan(fixture.Request(retryAttemptCount: 3, retryCap: 3));
            var blockedEvidence = fixture.Evidence(
                "UPSTREAM_OWNER", WorldContradictionKind.ReservationConflict,
                WorldContradictionSource.Reservation, 84, requiresUpstreamRepair: true);
            var blocked = WorldNeighborRollbackPlanner.Plan(fixture.Request(
                observations: new[] { blockedEvidence }));
            var rerandom = WorldNeighborRollbackPlanner.Plan(fixture.Request(wholeWorldRerandomCount: 1));
            var carve = WorldNeighborRollbackPlanner.Plan(fixture.Request(fallbackCarveCount: 1));
            var widened = WorldNeighborRollbackPlanner.Plan(fixture.Request(silentWideningCount: 1));

            Assert.That(bounded.Plan.Decision.Kind, Is.EqualTo(WorldRollbackDecisionKind.BoundedRetry));
            Assert.That(aborted.Plan.Decision.Kind, Is.EqualTo(WorldRollbackDecisionKind.Abort));
            Assert.That(blocked.Plan.Decision.Kind, Is.EqualTo(WorldRollbackDecisionKind.BlockedOwner));
            Assert.That(new[] { rerandom, carve, widened }.All(value => !value.Success && value.Plan == null), Is.True);
            Assert.That(rerandom.Failures.Select(value => value.Code),
                Does.Contain(WorldNeighborRollbackFailureCode.WholeWorldRerandomForbidden));
            Assert.That(carve.Failures.Select(value => value.Code),
                Does.Contain(WorldNeighborRollbackFailureCode.FallbackCarveForbidden));
            Assert.That(widened.Failures.Select(value => value.Code),
                Does.Contain(WorldNeighborRollbackFailureCode.SilentWideningForbidden));
        }

        [Test]
        public void RollbackPolicyIsDeterministicAcrossRepeatReverseAndCulture()
        {
            var observations = new[]
            {
                fixture.FullEvidence(84),
                fixture.Evidence("BOUNDARY_SECOND", WorldContradictionKind.BoundaryConflict,
                    WorldContradictionSource.Boundary, 85),
                fixture.Evidence("RETRY_THIRD", WorldContradictionKind.RetryExhausted,
                    WorldContradictionSource.Retry, 83),
            };
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = WorldNeighborRollbackPlanner.Plan(fixture.Request(observations: observations));
                var repeat = WorldNeighborRollbackPlanner.Plan(fixture.Request(observations: observations));
                var reverse = WorldNeighborRollbackPlanner.Plan(fixture.Request(observations: observations.Reverse()));
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var culture = WorldNeighborRollbackPlanner.Plan(fixture.Request(observations: observations));
                var results = new[] { first, repeat, reverse, culture };

                Assert.That(results.All(value => value.Success), Is.True,
                    string.Join(";", results.Select(Join)));
                Assert.That(results.Select(value => value.InputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => value.OutputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => value.Plan.FailureReport.FirstContradiction.StableContradictionId)
                    .Distinct().Count(), Is.EqualTo(1));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void InvalidRollbackInputsFailAtomicallyWithoutPartialPlan()
        {
            var unknownEdge = new WorldIntersectorEdgeId(
                new WorldSectorId(0), new WorldSectorId(168), WorldEdgeOrientation.Horizontal);
            var unknownEvidence = fixture.Evidence(
                "UNKNOWN_EVIDENCE", WorldContradictionKind.IntersectorSocketConflict,
                WorldContradictionSource.IntersectorSocket, 84,
                new[] { unknownEdge }, new[] { "TX_UNKNOWN" }, new[] { "WINDOW_UNKNOWN" },
                new[] { "CANDIDATE_UNKNOWN" }, new[] { "RETRY_UNKNOWN" });
            var missingIntersectorRequest = new WorldRollbackPolicyRequest(
                fixture.SolveOrder, null, fixture.ReservationPlan, fixture.PacingPlan,
                new WorldSectorId(84), new[] { fixture.FullEvidence(84) },
                ReferenceRollbackFixture.Map14PhaseExitDigest, fixture.RetryLabels, 0, 3,
                WorldNeighborRollbackPlanner.ReferencePublicationLabel);
            var results = new[]
            {
                WorldNeighborRollbackPlanner.Plan(null),
                WorldNeighborRollbackPlanner.Plan(missingIntersectorRequest),
                WorldNeighborRollbackPlanner.Plan(fixture.Request(failedSector: 999)),
                WorldNeighborRollbackPlanner.Plan(fixture.Request(observations: new[] { unknownEvidence })),
                WorldNeighborRollbackPlanner.Plan(fixture.Request(map14Digest: "INVALID")),
                WorldNeighborRollbackPlanner.Plan(fixture.Request(tilemapMutationCount: 1)),
            };

            Assert.That(results.All(value => !value.Success), Is.True);
            Assert.That(results.All(value => value.Plan == null), Is.True);
            Assert.That(results.All(value => value.InputDigest == string.Empty), Is.True);
            Assert.That(results.All(value => value.OutputDigest == string.Empty), Is.True);
            Assert.That(results.All(value => value.Failures.Count > 0), Is.True);
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldNeighborRollbackFailureCode.FailedSectorOutOfBounds));
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldNeighborRollbackFailureCode.UnknownEdgeEvidence));
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldNeighborRollbackFailureCode.InvalidDigest));
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldNeighborRollbackFailureCode.MutationClaim));
        }

        [Test]
        public void WorldRollbackDoesNotMutatePacingReservationEdgeWorldOrAuthoringAssets()
        {
            var worldDigest = fixture.WorldPlan.CanonicalDigest;
            var solveDigest = fixture.SolveOrder.OutputDigest;
            var edgeDigest = fixture.IntersectorPlan.OutputDigest;
            var reservationDigest = fixture.ReservationPlan.OutputDigest;
            var pacingDigest = fixture.PacingPlan.OutputDigest;
            var edgeIds = fixture.IntersectorPlan.Edges.Select(value => value.Id).ToArray();
            var transactionIds = fixture.ReservationPlan.Transactions.Select(value => value.TransactionId).ToArray();
            var result = fixture.Plan();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(fixture.WorldPlan.CanonicalDigest, Is.EqualTo(worldDigest));
            Assert.That(fixture.SolveOrder.OutputDigest, Is.EqualTo(solveDigest));
            Assert.That(fixture.IntersectorPlan.OutputDigest, Is.EqualTo(edgeDigest));
            Assert.That(fixture.ReservationPlan.OutputDigest, Is.EqualTo(reservationDigest));
            Assert.That(fixture.PacingPlan.OutputDigest, Is.EqualTo(pacingDigest));
            Assert.That(fixture.IntersectorPlan.Edges.Select(value => value.Id), Is.EqualTo(edgeIds));
            Assert.That(fixture.ReservationPlan.Transactions.Select(value => value.TransactionId),
                Is.EqualTo(transactionIds));
            Assert.That(new[]
            {
                result.Plan.WholeWorldRerandomCount, result.Plan.FallbackCarveCount,
                result.Plan.SilentWideningCount, result.Plan.NewRngDrawCount, result.Plan.SectorRerenderCount,
                result.Plan.GeneratedFileWriteCount, result.Plan.TilemapMutationCount, result.Plan.SceneMutationCount,
                result.Plan.PrefabMutationCount, result.Plan.GameObjectMutationCount, result.Plan.GameplaySpawnCount,
                result.Plan.AuthoringMutationCount, result.Plan.WorldPlanMutationCount,
                result.Plan.IntersectorPlanMutationCount, result.Plan.ReservationPlanMutationCount,
                result.Plan.PacingDensityPlanMutationCount,
            }.All(value => value == 0), Is.True);
        }

        [Test]
        public void Map15HandoffKeepsMap15_06Locked()
        {
            var result = fixture.Plan();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(WorldNeighborRollbackPlan.DownstreamOwner,
                Is.EqualTo("MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS"));
            Assert.That(WorldNeighborRollbackPlan.OpensDownstreamTask, Is.False);
            Assert.That(result.Plan.Request.PublicationLabel,
                Is.EqualTo(WorldNeighborRollbackPlanner.ReferencePublicationLabel));
            Assert.That(WorldNeighborRollbackPlan.ScopeRadius, Is.EqualTo(1));
            Assert.That(WorldNeighborRollbackPlan.MaximumScopeSectorCount, Is.EqualTo(9));
        }

        private static string Join(WorldNeighborRollbackResult result) => result == null
            ? "null"
            : string.Join(";", result.Failures.Select(value => value.ToString()));

        private sealed class ReferenceRollbackFixture
        {
            internal const string Map14PhaseExitDigest =
                "5b8ed6a3c8b0a20fe2f2d05eea0b7731522aff30e691ca606c638adcdbd62d82";
            internal const string RetryPatternLabel = "MAP14_RETRY_PATTERN_CANDIDATE";
            internal const string RetryClusterLabel = "MAP14_RETRY_CLUSTER_VARIANT";

            private ReferenceRollbackFixture(
                WorldPlanInput worldPlan,
                WorldSolveOrderResult solveOrder,
                WorldIntersectorEdgePlan intersectorPlan,
                WorldMultiSectorReservationPlan reservationPlan,
                WorldPacingDensityPlan pacingPlan)
            {
                WorldPlan = worldPlan;
                SolveOrder = solveOrder;
                IntersectorPlan = intersectorPlan;
                ReservationPlan = reservationPlan;
                PacingPlan = pacingPlan;
                RetryLabels = new[] { RetryPatternLabel, RetryClusterLabel };
            }

            internal WorldPlanInput WorldPlan { get; }
            internal WorldSolveOrderResult SolveOrder { get; }
            internal WorldIntersectorEdgePlan IntersectorPlan { get; }
            internal WorldMultiSectorReservationPlan ReservationPlan { get; }
            internal WorldPacingDensityPlan PacingPlan { get; }
            internal string[] RetryLabels { get; }

            internal static ReferenceRollbackFixture Create()
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
                if (!solveOrder.Success) throw new InvalidOperationException(string.Join(";", solveOrder.Failures));

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
                if (!edgeResult.Success) throw new InvalidOperationException(string.Join(";", edgeResult.Failures));

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
                if (!reservationResult.Success)
                    throw new InvalidOperationException(string.Join(";", reservationResult.Failures));

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
                if (!pacingResult.Success)
                    throw new InvalidOperationException(string.Join(";", pacingResult.Failures));

                return new ReferenceRollbackFixture(
                    worldPlan, solveOrder, edgeResult.Plan, reservationResult.Plan, pacingResult.Plan);
            }

            internal int Step(int sector) => SolveOrder.Steps.Single(value =>
                value.SectorId == new WorldSectorId(sector)).StepIndex;

            internal WorldContradictionEvidence Evidence(
                string id,
                WorldContradictionKind kind,
                WorldContradictionSource source,
                int sector,
                IEnumerable<WorldIntersectorEdgeId> edges = null,
                IEnumerable<string> reservations = null,
                IEnumerable<string> pacing = null,
                IEnumerable<string> candidates = null,
                IEnumerable<string> retries = null,
                bool retryable = true,
                bool requiresUpstreamRepair = false) =>
                new WorldContradictionEvidence(
                    id, kind, source, new WorldSectorId(sector), Step(sector),
                    edges, reservations, pacing, candidates, retries,
                    retryable, requiresUpstreamRepair);

            internal WorldContradictionEvidence FullEvidence(int sector)
            {
                return Evidence(
                    "REFERENCE_FIRST_CONTRADICTION", WorldContradictionKind.ClusterCandidateExhausted,
                    WorldContradictionSource.ClusterCandidate, sector,
                    new[] { IntersectorPlan.Edges.First(value =>
                        value.Id.MinSector == new WorldSectorId(83) || value.Id.MaxSector == new WorldSectorId(83)).Id },
                    new[] { ReservationPlan.Transactions[0].TransactionId },
                    new[] { PacingPlan.Windows[0].WindowId },
                    new[] { PacingPlan.Signatures[0].SignatureId },
                    new[] { RetryPatternLabel });
            }

            internal WorldRollbackPolicyRequest Request(
                int failedSector = 84,
                IEnumerable<WorldContradictionEvidence> observations = null,
                string map14Digest = null,
                int retryAttemptCount = 0,
                int retryCap = 3,
                int wholeWorldRerandomCount = 0,
                int fallbackCarveCount = 0,
                int silentWideningCount = 0,
                int tilemapMutationCount = 0) =>
                new WorldRollbackPolicyRequest(
                    SolveOrder, IntersectorPlan, ReservationPlan, PacingPlan,
                    new WorldSectorId(failedSector), observations ?? new[]
                    {
                        FullEvidence(failedSector >= 0 && failedSector < WorldPlanInput.SectorCount
                            ? failedSector
                            : 84),
                    },
                    map14Digest ?? Map14PhaseExitDigest, RetryLabels,
                    retryAttemptCount, retryCap, WorldNeighborRollbackPlanner.ReferencePublicationLabel,
                    wholeWorldRerandomCount: wholeWorldRerandomCount,
                    fallbackCarveCount: fallbackCarveCount,
                    silentWideningCount: silentWideningCount,
                    tilemapMutationCount: tilemapMutationCount);

            internal WorldNeighborRollbackResult Plan(int failedSector = 84) =>
                WorldNeighborRollbackPlanner.Plan(Request(failedSector));

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
                        edge == EdgeId(0, 1), edge == boundaryEdge));
                    result.Add(Projection(second, WorldSectorSide.West, new WorldSocketAnchor(0, 16, 3),
                        edge == EdgeId(0, 1), edge == boundaryEdge));
                }
                for (var y = 0; y < WorldPlanInput.SectorRows - 1; y++)
                for (var x = 0; x < WorldPlanInput.SectorColumns; x++)
                {
                    var first = new WorldSectorId((y * WorldPlanInput.SectorColumns) + x);
                    var second = new WorldSectorId(first.Value + WorldPlanInput.SectorColumns);
                    result.Add(Projection(first, WorldSectorSide.North, new WorldSocketAnchor(24, 31, 3),
                        false, false));
                    result.Add(Projection(second, WorldSectorSide.South, new WorldSocketAnchor(24, 0, 3),
                        false, false));
                }
                return result.ToArray();
            }

            private static WorldSocketProjection Projection(
                WorldSectorId sector,
                WorldSectorSide side,
                WorldSocketAnchor anchor,
                bool mandatory,
                bool boundary) =>
                new WorldSocketProjection(
                    sector, side, anchor, false, mandatory, boundary,
                    boundary ? "MAP08_BOUNDARY" : "MAP15_01_WORLD_PLAN");

            private static WorldIntersectorEdgeId EdgeId(int first, int second) =>
                new WorldIntersectorEdgeId(
                    new WorldSectorId(first), new WorldSectorId(second), WorldEdgeOrientation.Horizontal);

            private static string Map13Digest() => WorldPacingDensityDigest.HashCanonicalText(
                CoreResourceRegionStarterCatalog.CanonicalDigest + "\n" +
                SpecialLandmarkRegionStarterCatalog.CanonicalDigest);
        }
    }
}
