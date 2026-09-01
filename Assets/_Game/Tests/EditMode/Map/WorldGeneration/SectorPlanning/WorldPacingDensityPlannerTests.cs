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
    [Category("MAP15_04")]
    public sealed class WorldPacingDensityPlannerTests
    {
        private ReferencePacingFixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = ReferencePacingFixture.Create();
        }

        [Test]
        public void PacingDensityPlanPublishesWindowsBudgetsAndDigests()
        {
            var result = fixture.Plan();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(result.Plan.ObservedWorldSectorCount, Is.EqualTo(169));
            Assert.That(result.Plan.ObservedInternalEdgeCount, Is.EqualTo(312));
            Assert.That(result.Plan.ReservationPlanObserved, Is.True);
            Assert.That(result.Plan.ReservationPlanDigest, Is.EqualTo(fixture.ReservationPlan.OutputDigest));
            Assert.That(result.Plan.Windows.Count, Is.EqualTo(5));
            Assert.That(result.Plan.Budgets.Count, Is.EqualTo(169));
            Assert.That(result.Plan.Signatures.Count, Is.EqualTo(9));
            Assert.That(result.Plan.RecentUseRules.Count, Is.EqualTo(3));
            Assert.That(result.Plan.Observations.Count, Is.EqualTo(6));
            Assert.That(result.Plan.Violations, Is.Empty);
            Assert.That(result.Plan.InputDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(result.Plan.OutputDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(() => ((IList<WorldPacingWindow>)result.Plan.Windows).Add(result.Plan.Windows[0]),
                Throws.TypeOf<NotSupportedException>());

            TestContext.Progress.WriteLine("MAP15_04_INPUT_DIGEST=" + result.Plan.InputDigest);
            TestContext.Progress.WriteLine("MAP15_04_OUTPUT_DIGEST=" + result.Plan.OutputDigest);
            TestContext.Progress.WriteLine("MAP15_04_COUNTS=" + string.Join(",", new[]
            {
                result.Plan.Windows.Count, result.Plan.Budgets.Count, result.Plan.Signatures.Count,
                result.Plan.RecentUseRules.Count, result.Plan.Observations.Count, result.Plan.Violations.Count,
            }));
        }

        [Test]
        public void AllRequiredWindowKindsAreCoveredWithoutOpeningMap15_05()
        {
            var result = fixture.Plan();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(result.Plan.CoveredWindowKindCount,
                Is.EqualTo(WorldPacingDensityPlan.RequiredWindowKindCount));
            Assert.That(result.Plan.MissingWindowKindCount, Is.Zero);
            Assert.That(result.Plan.Windows.Select(value => value.Kind).Distinct(), Is.EquivalentTo(new[]
            {
                WorldPacingWindowKind.Quiet,
                WorldPacingWindowKind.Cluster,
                WorldPacingWindowKind.Activity,
                WorldPacingWindowKind.Event,
                WorldPacingWindowKind.Landmark,
            }));
            Assert.That(WorldPacingDensityPlan.DownstreamOwner,
                Is.EqualTo("MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT"));
            Assert.That(WorldPacingDensityPlan.OpensDownstreamTask, Is.False);
        }

        [Test]
        public void DensityBudgetsCoverAllWorldSectorsWithoutTileBake()
        {
            var result = fixture.Plan();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(result.Plan.DensityBudgetSectorCount, Is.EqualTo(169));
            Assert.That(result.Plan.Budgets.Select(value => value.SectorId),
                Is.EqualTo(fixture.WorldPlan.Nodes.Select(value => value.Id).OrderBy(value => value)));
            Assert.That(result.Plan.Budgets.All(value => value.SolidWithinRange && value.ReachableWithinRange),
                Is.True);
            Assert.That(result.Plan.Budgets.All(value =>
                value.Verdict == WorldDensityBudgetVerdict.WithinRange), Is.True);
            Assert.That(result.Plan.BudgetViolationCount, Is.Zero);
            Assert.That(result.Plan.TilemapMutationCount, Is.Zero);
            Assert.That(result.Plan.SectorRerenderCount, Is.Zero);
        }

        [Test]
        public void SpecialAndLandmarkWindowsPreserveReservationPriority()
        {
            var beforeDigest = fixture.ReservationPlan.OutputDigest;
            var beforeTransactions = fixture.ReservationPlan.Transactions
                .Select(value => value.TransactionId + "|" + string.Join(",", value.SectorIds)).ToArray();
            var result = fixture.Plan();
            var landmarkSectors = result.Plan.Windows.Where(value => value.Kind == WorldPacingWindowKind.Landmark)
                .SelectMany(value => value.SectorIds).Distinct().ToArray();
            var fixedSectors = fixture.ReservationPlan.Transactions.Where(value => !value.IsDeferred)
                .SelectMany(value => value.SectorIds).Distinct().ToArray();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(fixedSectors.All(landmarkSectors.Contains), Is.True);
            Assert.That(fixture.ReservationPlan.Transactions.Count(value => value.IsDeferred), Is.EqualTo(1));
            Assert.That(fixture.ReservationPlan.OutputDigest, Is.EqualTo(beforeDigest));
            Assert.That(fixture.ReservationPlan.Transactions
                .Select(value => value.TransactionId + "|" + string.Join(",", value.SectorIds)),
                Is.EqualTo(beforeTransactions));
            Assert.That(result.Plan.ReservationPlanMutationCount, Is.Zero);
        }

        [Test]
        public void PatternClusterActivityRecentUseRulesDetectRepeats()
        {
            var accepted = fixture.Plan();
            var repeated = WorldPacingDensityPlanner.Plan(fixture.Request(
                signatures: fixture.RepeatingSignatures()));

            Assert.That(accepted.Success, Is.True, Join(accepted));
            Assert.That(accepted.Plan.CoveredSignatureKindCount,
                Is.EqualTo(WorldPacingDensityPlan.RequiredSignatureKindCount));
            Assert.That(accepted.Plan.CoveredRecentUseRuleCount,
                Is.EqualTo(WorldPacingDensityPlan.RequiredRecentUseRuleCount));
            Assert.That(accepted.Plan.AcceptedRecentUseObservationCount, Is.EqualTo(6));
            Assert.That(accepted.Plan.RecentUseViolationCount, Is.Zero);

            Assert.That(repeated.Success, Is.True, Join(repeated));
            Assert.That(repeated.Plan.RecentUseViolationCount, Is.EqualTo(3));
            Assert.That(repeated.Plan.Violations.Select(value => value.ViolationType), Is.EquivalentTo(new[]
            {
                WorldPacingDensityViolationType.RecentPatternRepeat,
                WorldPacingDensityViolationType.RecentClusterRepeat,
                WorldPacingDensityViolationType.RecentActivityRepeat,
            }));
            Assert.That(repeated.Plan.Observations.All(value => value.GraphDistanceAvailable), Is.True);
            Assert.That(repeated.Plan.Observations.All(value => value.GraphDistance == 1), Is.True);
        }

        [Test]
        public void ActivityEventCapsAndFrequencyWindowsRemainAbstractAndBounded()
        {
            var accepted = fixture.Plan();
            var eventWindow = fixture.Windows.Single(value => value.Kind == WorldPacingWindowKind.Event);
            var overCap = CopyWindow(eventWindow, observedCount: 3);
            var synthetic = WorldPacingDensityPlanner.Plan(fixture.Request(
                windows: fixture.Windows.Where(value => value != eventWindow).Concat(new[] { overCap })));

            Assert.That(accepted.Success, Is.True, Join(accepted));
            Assert.That(accepted.Plan.ActivityEventCapViolationCount, Is.Zero);
            Assert.That(fixture.Constraints.Count, Is.EqualTo(2));
            Assert.That(fixture.Constraints.All(value => value.TargetPermille > 0 &&
                                                         value.TargetPermille <= 1000), Is.True);
            Assert.That(fixture.Constraints.All(value => value.AuthorityDigest.Length == 64), Is.True);
            Assert.That(accepted.Plan.Windows.Where(value => value.Kind == WorldPacingWindowKind.Activity ||
                                                             value.Kind == WorldPacingWindowKind.Event)
                .All(value => value.ObservedCount <= value.MaximumCount), Is.True);

            Assert.That(synthetic.Success, Is.True, Join(synthetic));
            Assert.That(synthetic.Plan.ActivityEventCapViolationCount, Is.EqualTo(1));
            Assert.That(synthetic.Plan.Violations.Any(value =>
                value.ViolationType == WorldPacingDensityViolationType.EventCapExceeded), Is.True);
            Assert.That(synthetic.Plan.NewRngDrawCount, Is.Zero);
        }

        [Test]
        public void PacingDensityPolicyIsDeterministicAcrossRepeatReverseAndCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = fixture.Plan();
                var repeat = fixture.Plan();
                var reverse = WorldPacingDensityPlanner.Plan(fixture.Request(
                    windows: fixture.Windows.Reverse(), budgets: fixture.Budgets.Reverse(),
                    signatures: fixture.Signatures.Reverse(), rules: fixture.Rules.Reverse(),
                    constraints: fixture.Constraints.Reverse()));
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var culture = fixture.Plan();
                var results = new[] { first, repeat, reverse, culture };

                Assert.That(results.All(value => value.Success), Is.True,
                    string.Join(";", results.Select(Join)));
                Assert.That(results.Select(value => value.InputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => value.OutputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => string.Join(",", value.Plan.Observations
                    .Select(item => item.ObservationId))).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => string.Join(",", value.Plan.Budgets
                    .Select(item => item.SectorId))).Distinct().Count(), Is.EqualTo(1));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void InvalidPacingDensityInputsFailAtomicallyWithoutPartialPlan()
        {
            var firstBudget = fixture.Budgets[0];
            var invalidBudget = new WorldSectorDensityBudget(
                firstBudget.SectorId, firstBudget.Kind, 80, 20, 50, 20, 80, 60,
                firstBudget.Reason, firstBudget.SourceOwner);
            var invalidSignature = new WorldContentSignature(
                WorldContentSignatureKind.Pattern, "MP_INVALID", new WorldSectorId(999), 0, "MAP10");
            var invalidRule = new WorldRecentUseRule(
                WorldContentSignatureKind.Pattern, 0, 3, true, "INVALID", "MAP10");
            var eventConstraint = fixture.Constraints.Single(value => value.Kind == WorldPacingWindowKind.Event);
            var invalidConstraint = new WorldActivityEventConstraint(
                eventConstraint.ConstraintId, eventConstraint.Kind, eventConstraint.TargetPermille, 1,
                eventConstraint.AuthorityDigest, eventConstraint.SourceOwner);

            var results = new[]
            {
                WorldPacingDensityPlanner.Plan(null),
                WorldPacingDensityPlanner.Plan(fixture.Request(
                    windows: fixture.Windows.Where(value => value.Kind != WorldPacingWindowKind.Event))),
                WorldPacingDensityPlanner.Plan(fixture.Request(budgets: fixture.Budgets.Skip(1))),
                WorldPacingDensityPlanner.Plan(fixture.Request(
                    budgets: fixture.Budgets.Skip(1).Concat(new[] { invalidBudget }))),
                WorldPacingDensityPlanner.Plan(fixture.Request(
                    signatures: fixture.Signatures.Concat(new[] { invalidSignature }))),
                WorldPacingDensityPlanner.Plan(fixture.Request(
                    rules: fixture.Rules.Where(value => value.Kind != WorldContentSignatureKind.Pattern)
                        .Concat(new[] { invalidRule }))),
                WorldPacingDensityPlanner.Plan(fixture.Request(
                    constraints: fixture.Constraints.Where(value => value.Kind != WorldPacingWindowKind.Event)
                        .Concat(new[] { invalidConstraint }))),
                WorldPacingDensityPlanner.Plan(fixture.Request(map10Digest: "INVALID")),
                WorldPacingDensityPlanner.Plan(fixture.Request(tilemapMutationCount: 1)),
            };

            Assert.That(results.All(value => !value.Success), Is.True);
            Assert.That(results.All(value => value.Plan == null), Is.True);
            Assert.That(results.All(value => value.InputDigest == string.Empty), Is.True);
            Assert.That(results.All(value => value.OutputDigest == string.Empty), Is.True);
            Assert.That(results.All(value => value.Failures.Count > 0), Is.True);
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldPacingDensityFailureCode.MissingWindowKind));
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldPacingDensityFailureCode.MissingBudgetSector));
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldPacingDensityFailureCode.InvalidRecentUseRule));
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldPacingDensityFailureCode.MutationClaim));
        }

        [Test]
        public void WorldPacingDensityDoesNotMutateReservationEdgeWorldOrAuthoringAssets()
        {
            var worldDigest = fixture.WorldPlan.CanonicalDigest;
            var solveDigest = fixture.SolveOrder.OutputDigest;
            var edgeDigest = fixture.IntersectorPlan.OutputDigest;
            var reservationDigest = fixture.ReservationPlan.OutputDigest;
            var authorityDigests = fixture.AuthorityDigests.ToArray();
            var result = fixture.Plan();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(fixture.WorldPlan.CanonicalDigest, Is.EqualTo(worldDigest));
            Assert.That(fixture.SolveOrder.OutputDigest, Is.EqualTo(solveDigest));
            Assert.That(fixture.IntersectorPlan.OutputDigest, Is.EqualTo(edgeDigest));
            Assert.That(fixture.ReservationPlan.OutputDigest, Is.EqualTo(reservationDigest));
            Assert.That(fixture.AuthorityDigests, Is.EqualTo(authorityDigests));
            Assert.That(result.Plan.NewRngDrawCount, Is.Zero);
            Assert.That(result.Plan.FallbackCarveCount, Is.Zero);
            Assert.That(result.Plan.SectorRerenderCount, Is.Zero);
            Assert.That(result.Plan.GeneratedFileWriteCount, Is.Zero);
            Assert.That(result.Plan.TilemapMutationCount, Is.Zero);
            Assert.That(result.Plan.SceneMutationCount, Is.Zero);
            Assert.That(result.Plan.PrefabMutationCount, Is.Zero);
            Assert.That(result.Plan.GameObjectMutationCount, Is.Zero);
            Assert.That(result.Plan.GameplaySpawnCount, Is.Zero);
            Assert.That(result.Plan.AuthoringMutationCount, Is.Zero);
            Assert.That(result.Plan.WorldPlanMutationCount, Is.Zero);
            Assert.That(result.Plan.IntersectorPlanMutationCount, Is.Zero);
            Assert.That(result.Plan.ReservationPlanMutationCount, Is.Zero);
        }

        [Test]
        public void Map15HandoffKeepsMap15_05Locked()
        {
            var result = fixture.Plan();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(WorldPacingDensityPlan.DownstreamOwner,
                Is.EqualTo("MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT"));
            Assert.That(WorldPacingDensityPlan.OpensDownstreamTask, Is.False);
            Assert.That(result.Plan.Request.PublicationLabel,
                Is.EqualTo(WorldPacingDensityPlanner.ReferencePublicationLabel));
            Assert.That(result.Plan.MissingWindowKindCount, Is.Zero);
            Assert.That(result.Plan.MissingSignatureKindCount, Is.Zero);
            Assert.That(result.Plan.MissingRecentUseRuleCount, Is.Zero);
        }

        private static WorldPacingWindow CopyWindow(WorldPacingWindow source, int observedCount) =>
            new WorldPacingWindow(
                source.WindowId, source.Kind, source.SectorIds, source.FirstSolveStep, source.LastSolveStep,
                source.MinimumCount, source.MaximumCount, observedCount, source.Reason, source.SourceOwner);

        private static string Join(WorldPacingDensityResult result) => result == null
            ? "null"
            : string.Join(";", result.Failures.Select(value => value.ToString()));

        private sealed class ReferencePacingFixture
        {
            internal const string Map14PhaseExitDigest =
                "5b8ed6a3c8b0a20fe2f2d05eea0b7731522aff30e691ca606c638adcdbd62d82";

            private ReferencePacingFixture(
                WorldPlanInput worldPlan,
                WorldSolveOrderResult solveOrder,
                WorldIntersectorEdgePlan intersectorPlan,
                WorldMultiSectorReservationPlan reservationPlan,
                WorldPacingWindow[] windows,
                WorldSectorDensityBudget[] budgets,
                WorldContentSignature[] signatures,
                WorldRecentUseRule[] rules,
                WorldActivityEventConstraint[] constraints,
                string[] authorityDigests)
            {
                WorldPlan = worldPlan;
                SolveOrder = solveOrder;
                IntersectorPlan = intersectorPlan;
                ReservationPlan = reservationPlan;
                Windows = windows;
                Budgets = budgets;
                Signatures = signatures;
                Rules = rules;
                Constraints = constraints;
                AuthorityDigests = authorityDigests;
            }

            internal WorldPlanInput WorldPlan { get; }
            internal WorldSolveOrderResult SolveOrder { get; }
            internal WorldIntersectorEdgePlan IntersectorPlan { get; }
            internal WorldMultiSectorReservationPlan ReservationPlan { get; }
            internal WorldPacingWindow[] Windows { get; }
            internal WorldSectorDensityBudget[] Budgets { get; }
            internal WorldContentSignature[] Signatures { get; }
            internal WorldRecentUseRule[] Rules { get; }
            internal WorldActivityEventConstraint[] Constraints { get; }
            internal string[] AuthorityDigests { get; }

            internal static ReferencePacingFixture Create()
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
                        id == 2,
                        false,
                        false,
                        id == 0,
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
                    Window("WINDOW_QUIET", WorldPacingWindowKind.Quiet, new[] { 50, 51, 52, 53, 54 },
                        2, 5, 3, steps, "MAP09_PACING_ROLE"),
                    Window("WINDOW_CLUSTER", WorldPacingWindowKind.Cluster, new[] { 20, 40 },
                        1, 3, 2, steps, "MAP11_TERRAIN_CLUSTER"),
                    Window("WINDOW_ACTIVITY", WorldPacingWindowKind.Activity, new[] { 60, 70 },
                        1, 3, 2, steps, "MAP12_ACTIVITY_FREQUENCY"),
                    Window("WINDOW_EVENT", WorldPacingWindowKind.Event, new[] { 80, 90 },
                        0, 2, 1, steps, "MAP12_EVENT_FREQUENCY"),
                    Window("WINDOW_LANDMARK", WorldPacingWindowKind.Landmark, new[] { 2 },
                        1, 2, 1, steps, "MAP13_MAP15_03_FIXED_SPECIAL"),
                };
                var budgets = nodes.Select(node => new WorldSectorDensityBudget(
                    node.Id, WorldDensityBudgetKind.AbstractSolidReachable,
                    30, 70, 50, 20, 80, 60,
                    "ABSTRACT SECTOR ENVELOPE; NOT BAKED TILE COUNTS", "MAP15_04_REFERENCE")).ToArray();
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
                    Rule(WorldContentSignatureKind.Pattern, "MAP10_RECENT_PATTERN"),
                    Rule(WorldContentSignatureKind.Cluster, "MAP11_RECENT_CLUSTER"),
                    Rule(WorldContentSignatureKind.Activity, "MAP12_RECENT_ACTIVITY"),
                };
                var activityPolicy = new ActivityFrequencyPolicy(90, 4, 2, 1);
                var eventPolicy = new EventOverlayAssignmentPolicy(80);
                var activityDigest = WorldPacingDensityDigest.HashCanonicalText(string.Join("|", new[]
                {
                    "MAP12_ACTIVITY", activityPolicy.TargetPermille.ToString(CultureInfo.InvariantCulture),
                    activityPolicy.MaxStrongPerWorld.ToString(CultureInfo.InvariantCulture),
                    activityPolicy.MaxStrongPerPatch.ToString(CultureInfo.InvariantCulture),
                    activityPolicy.MaxStrongPerSector.ToString(CultureInfo.InvariantCulture),
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
                var authorityDigests = new[]
                {
                    WorldPacingDensityDigest.HashCanonicalText(new MicroPatternId("MP_REF_A").Value),
                    WorldPacingDensityDigest.HashCanonicalText(
                        new TerrainClusterId("TC_REF_A").Value + "|" + new SpineVariantId("SPINE_REF_A").Value),
                    WorldPacingDensityDigest.HashCanonicalText(activityDigest + "\n" + eventDigest),
                    Map13Digest(),
                };
                return new ReferencePacingFixture(
                    worldPlan, solveOrder, edgeResult.Plan, reservationResult.Plan,
                    windows, budgets, signatures, rules, constraints, authorityDigests);
            }

            internal WorldPacingDensityRequest Request(
                IEnumerable<WorldPacingWindow> windows = null,
                IEnumerable<WorldSectorDensityBudget> budgets = null,
                IEnumerable<WorldContentSignature> signatures = null,
                IEnumerable<WorldRecentUseRule> rules = null,
                IEnumerable<WorldActivityEventConstraint> constraints = null,
                string map10Digest = null,
                int tilemapMutationCount = 0) =>
                new WorldPacingDensityRequest(
                    WorldPlan, SolveOrder, IntersectorPlan, ReservationPlan,
                    windows ?? Windows, budgets ?? Budgets, signatures ?? Signatures,
                    rules ?? Rules, constraints ?? Constraints,
                    map10Digest ?? AuthorityDigests[0], AuthorityDigests[1], AuthorityDigests[2],
                    AuthorityDigests[3], Map14PhaseExitDigest,
                    WorldPacingDensityPlanner.ReferencePublicationLabel,
                    tilemapMutationCount: tilemapMutationCount);

            internal WorldPacingDensityResult Plan() => WorldPacingDensityPlanner.Plan(Request());

            internal WorldContentSignature[] RepeatingSignatures()
            {
                var steps = SolveOrder.Steps.ToDictionary(value => value.SectorId, value => value.StepIndex);
                return new[]
                {
                    Signature(WorldContentSignatureKind.Pattern, "MP_REPEAT", 10, steps, "MAP10"),
                    Signature(WorldContentSignatureKind.Pattern, "MP_REPEAT", 11, steps, "MAP10"),
                    Signature(WorldContentSignatureKind.Cluster, "TC_REPEAT", 30, steps, "MAP11"),
                    Signature(WorldContentSignatureKind.Cluster, "TC_REPEAT", 31, steps, "MAP11"),
                    Signature(WorldContentSignatureKind.Activity, "ACT_REPEAT", 60, steps, "MAP12"),
                    Signature(WorldContentSignatureKind.Activity, "ACT_REPEAT", 61, steps, "MAP12"),
                };
            }

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
                new WorldRecentUseRule(kind, 2, 3, true,
                    "MINIMUM WORLD GRAPH AND SOLVE-STEP DISTANCE", owner);

            private static WorldSocketProjection[] BuildProjections(WorldIntersectorEdgeId boundaryEdge)
            {
                var result = new List<WorldSocketProjection>(WorldIntersectorEdgePlan.EndpointCount);
                for (var y = 0; y < WorldPlanInput.SectorRows; y++)
                for (var x = 0; x < WorldPlanInput.SectorColumns - 1; x++)
                {
                    var first = new WorldSectorId((y * WorldPlanInput.SectorColumns) + x);
                    var second = new WorldSectorId(first.Value + 1);
                    var edge = new WorldIntersectorEdgeId(first, second, WorldEdgeOrientation.Horizontal);
                    var mandatory = edge == EdgeId(0, 1);
                    var boundary = edge == boundaryEdge;
                    result.Add(Projection(first, WorldSectorSide.East, new WorldSocketAnchor(47, 16, 3),
                        mandatory, boundary));
                    result.Add(Projection(second, WorldSectorSide.West, new WorldSocketAnchor(0, 16, 3),
                        mandatory, boundary));
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
