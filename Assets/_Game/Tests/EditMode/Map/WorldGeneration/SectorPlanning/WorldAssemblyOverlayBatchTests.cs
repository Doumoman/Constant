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
    [Category("MAP15_06")]
    public sealed class WorldAssemblyOverlayBatchTests
    {
        private ReferenceOverlayFixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = ReferenceOverlayFixture.Create();
        }

        [Test]
        public void OverlayExportPublishesTopologyEdgesReservationsPacingRollbackAndDigests()
        {
            var result = fixture.Export();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(result.Export.OverlaySectorCount, Is.EqualTo(169));
            Assert.That(result.Export.OverlayEdgeCount, Is.EqualTo(312));
            Assert.That(result.Export.Edges.Sum(value => value.EndpointCount), Is.EqualTo(624));
            Assert.That(WorldAssemblyOverlayExport.WorldWidthTiles, Is.EqualTo(624));
            Assert.That(WorldAssemblyOverlayExport.WorldHeightTiles, Is.EqualTo(416));
            Assert.That(WorldAssemblyOverlayExport.SectorWidthTiles, Is.EqualTo(48));
            Assert.That(WorldAssemblyOverlayExport.SectorHeightTiles, Is.EqualTo(32));
            Assert.That(WorldAssemblyOverlayExport.SectorColumns, Is.EqualTo(13));
            Assert.That(WorldAssemblyOverlayExport.SectorRows, Is.EqualTo(13));
            Assert.That(result.Export.CoveredHashRecordCount, Is.EqualTo(10));
            Assert.That(result.Export.MissingHashRecordCount, Is.Zero);
            Assert.That(result.InputDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(result.OutputDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(() => ((IList<WorldAssemblyOverlaySector>)result.Export.Sectors)
                    .Add(result.Export.Sectors[0]),
                Throws.TypeOf<NotSupportedException>());

            TestContext.Out.WriteLine("MAP15_06_INPUT_DIGEST=" + result.InputDigest);
            TestContext.Out.WriteLine("MAP15_06_OUTPUT_DIGEST=" + result.OutputDigest);
            TestContext.Out.WriteLine("MAP15_06_COUNTS=SECTORS169;EDGES312;ENDPOINTS624;LAYERS12;HASHES10;CASES4;BOUNDS10");
        }

        [Test]
        public void OverlayLayersCoverPlacementBoundarySpecialPacingActivityRollbackAndFailureEvidence()
        {
            var result = fixture.Export();
            var required = Enum.GetValues(typeof(WorldAssemblyOverlayLayerKind))
                .Cast<WorldAssemblyOverlayLayerKind>().OrderBy(value => value).ToArray();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(result.Export.Layers.Select(value => value.Kind), Is.EqualTo(required));
            Assert.That(result.Export.CoveredLayerCount, Is.EqualTo(12));
            Assert.That(result.Export.MissingLayerCount, Is.Zero);
            Assert.That(result.Export.Layers.All(value => value.IsAvailable || value.HasExplicitUnavailableReason),
                Is.True);
            Assert.That(result.Export.Layers.Single(value =>
                value.Kind == WorldAssemblyOverlayLayerKind.BoundaryPairs).ItemCount, Is.GreaterThan(0));
            Assert.That(result.Export.Layers.Single(value =>
                value.Kind == WorldAssemblyOverlayLayerKind.SpecialReservations).ItemCount, Is.GreaterThan(0));
            Assert.That(result.Export.Layers.Single(value =>
                value.Kind == WorldAssemblyOverlayLayerKind.ActivityEventCaps).ItemCount, Is.EqualTo(2));
            Assert.That(result.Export.Layers.Single(value =>
                value.Kind == WorldAssemblyOverlayLayerKind.RollbackScopes).ItemCount, Is.EqualTo(9));
            Assert.That(result.Export.Layers.Single(value =>
                value.Kind == WorldAssemblyOverlayLayerKind.FailureReports).ItemCount, Is.GreaterThan(0));
        }

        [Test]
        public void OverlayTokensAreStableRowMajorAndEdgeIdOrdered()
        {
            var result = fixture.Export();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(result.Export.Sectors.Select(value => value.SectorId.Value),
                Is.EqualTo(Enumerable.Range(0, 169)));
            Assert.That(result.Export.Sectors.All(value => value.Coordinate.RowMajorId == value.SectorId), Is.True);
            Assert.That(result.Export.Edges.Select(value => value.EdgeId),
                Is.EqualTo(result.Export.Edges.Select(value => value.EdgeId).OrderBy(value => value).ToArray()));
            Assert.That(result.Export.Sectors.Select(value => value.StableToken)
                .Concat(result.Export.Edges.Select(value => value.StableToken))
                .Concat(result.Export.Layers.SelectMany(value => value.Tokens))
                .All(value => !value.Contains("/") && !value.Contains("\\") && !value.Contains("\n")), Is.True);
        }

        [Test]
        public void BatchWorldPlansUseFourFocusedReferenceLabelsWithoutProductionApproval()
        {
            var result = fixture.Export();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(result.Export.BatchReport.Cases.Select(value => value.Label),
                Is.EqualTo(WorldAssemblyOverlayExport.RequiredBatchLabels));
            Assert.That(result.Export.BatchReport.RequiredCaseCount, Is.EqualTo(4));
            Assert.That(result.Export.BatchReport.CoveredCaseCount, Is.EqualTo(4));
            Assert.That(result.Export.BatchReport.MissingCaseCount, Is.Zero);
            Assert.That(result.Export.BatchReport.ProductionSeedApprovalCount, Is.Zero);
            Assert.That(result.Export.BatchReport.Cases.All(value => !value.ProductionSeedApproval), Is.True);
        }

        [Test]
        public void BatchReportValidatesGraphReservationPacingRollbackAndSolverBounds()
        {
            var result = fixture.Export();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(result.Export.BatchReport.Pass, Is.True);
            Assert.That(result.Export.BatchReport.PassingCaseCount, Is.EqualTo(4));
            Assert.That(result.Export.BatchReport.Cases.All(value => value.ConnectedComponentCount == 1), Is.True);
            Assert.That(result.Export.BatchReport.Cases.All(value => value.DuplicateIdCount == 0), Is.True);
            Assert.That(result.Export.BatchReport.Cases.All(value =>
                value.MissingRequiredBoundaryPairCount == 0), Is.True);
            Assert.That(result.Export.BatchReport.Cases.All(value =>
                value.UntypedReservationConflictCount == 0), Is.True);
            Assert.That(result.Export.BatchReport.Cases.All(value =>
                value.AcceptedPacingViolationCount == 0), Is.True);
            Assert.That(result.Export.BatchReport.Cases.All(value =>
                value.MaximumRollbackSectorCount <= 9), Is.True);
        }

        [Test]
        public void SolverUpperBoundReportRejectsLimitOverrunRerandomFallbackCarveAndSilentWidening()
        {
            var valid = fixture.Export();
            var rerandom = WorldAssemblyOverlayExporter.Export(fixture.Request(wholeWorldRerandomCount: 1));
            var carve = WorldAssemblyOverlayExporter.Export(fixture.Request(fallbackCarveCount: 1));
            var widening = WorldAssemblyOverlayExporter.Export(fixture.Request(silentWideningCount: 1));
            var fileWrite = WorldAssemblyOverlayExporter.Export(fixture.Request(generatedFileWriteCount: 1));

            Assert.That(valid.Success, Is.True, Join(valid));
            Assert.That(valid.Export.BatchReport.RequiredUpperBoundCount, Is.EqualTo(10));
            Assert.That(valid.Export.BatchReport.CoveredUpperBoundCount, Is.EqualTo(10));
            Assert.That(valid.Export.BatchReport.UpperBoundViolationCount, Is.Zero);
            Assert.That(valid.Export.BatchReport.SolverUpperBounds.All(value => value.Pass), Is.True);
            Assert.That(new[] { rerandom, carve, widening, fileWrite }
                .All(value => !value.Success && value.Export == null), Is.True);
            Assert.That(rerandom.Failures.Select(value => value.Code),
                Does.Contain(WorldAssemblyOverlayFailureCode.WholeWorldRerandomForbidden));
            Assert.That(carve.Failures.Select(value => value.Code),
                Does.Contain(WorldAssemblyOverlayFailureCode.FallbackCarveForbidden));
            Assert.That(widening.Failures.Select(value => value.Code),
                Does.Contain(WorldAssemblyOverlayFailureCode.SilentWideningForbidden));
            Assert.That(fileWrite.Failures.Select(value => value.Code),
                Does.Contain(WorldAssemblyOverlayFailureCode.FileWriteForbidden));
        }

        [Test]
        public void OverlayExportIsDeterministicAcrossRepeatReverseAndCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = fixture.Export();
                var repeat = fixture.Export();
                var reverse = WorldAssemblyOverlayExporter.Export(fixture.Request(
                    WorldAssemblyOverlayExport.RequiredBatchLabels.Reverse()));
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var culture = fixture.Export();
                var results = new[] { first, repeat, reverse, culture };

                Assert.That(results.All(value => value.Success), Is.True,
                    string.Join(";", results.Select(Join)));
                Assert.That(results.Select(value => value.InputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => value.OutputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => string.Join("\n",
                    value.Export.Sectors.Select(sector => sector.StableToken))).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => string.Join("\n",
                    value.Export.Edges.Select(edge => edge.StableToken))).Distinct().Count(), Is.EqualTo(1));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void InvalidOverlayInputsFailAtomicallyWithoutPartialExport()
        {
            var missingIntersector = new WorldAssemblyOverlayRequest(
                fixture.SolveOrder, null, fixture.ReservationPlan, fixture.PacingPlan, fixture.RollbackPlan,
                WorldAssemblyOverlayExport.RequiredBatchLabels,
                WorldAssemblyOverlayExporter.ReferencePublicationLabel);
            var results = new[]
            {
                WorldAssemblyOverlayExporter.Export(null),
                WorldAssemblyOverlayExporter.Export(missingIntersector),
                WorldAssemblyOverlayExporter.Export(fixture.Request(new[] { "REFERENCE_WORLD_BASELINE" })),
                WorldAssemblyOverlayExporter.Export(fixture.Request(
                    WorldAssemblyOverlayExport.RequiredBatchLabels, tilemapMutationCount: 1)),
            };

            Assert.That(results.All(value => !value.Success), Is.True);
            Assert.That(results.All(value => value.Export == null), Is.True);
            Assert.That(results.All(value => value.InputDigest == string.Empty), Is.True);
            Assert.That(results.All(value => value.OutputDigest == string.Empty), Is.True);
            Assert.That(results.All(value => value.Failures.Count > 0), Is.True);
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldAssemblyOverlayFailureCode.MissingRequest));
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldAssemblyOverlayFailureCode.MissingIntersectorPlan));
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldAssemblyOverlayFailureCode.InvalidBatchLabels));
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldAssemblyOverlayFailureCode.MutationClaim));
        }

        [Test]
        public void OverlayBatchDoesNotMutateWorldPlansAuthoringFilesTilesScenesOrGameplayObjects()
        {
            var worldDigest = fixture.WorldPlan.CanonicalDigest;
            var solveDigest = fixture.SolveOrder.OutputDigest;
            var edgeDigest = fixture.IntersectorPlan.OutputDigest;
            var reservationDigest = fixture.ReservationPlan.OutputDigest;
            var pacingDigest = fixture.PacingPlan.OutputDigest;
            var rollbackDigest = fixture.RollbackPlan.OutputDigest;
            var edgeIds = fixture.IntersectorPlan.Edges.Select(value => value.Id).ToArray();
            var result = fixture.Export();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(fixture.WorldPlan.CanonicalDigest, Is.EqualTo(worldDigest));
            Assert.That(fixture.SolveOrder.OutputDigest, Is.EqualTo(solveDigest));
            Assert.That(fixture.IntersectorPlan.OutputDigest, Is.EqualTo(edgeDigest));
            Assert.That(fixture.ReservationPlan.OutputDigest, Is.EqualTo(reservationDigest));
            Assert.That(fixture.PacingPlan.OutputDigest, Is.EqualTo(pacingDigest));
            Assert.That(fixture.RollbackPlan.OutputDigest, Is.EqualTo(rollbackDigest));
            Assert.That(fixture.IntersectorPlan.Edges.Select(value => value.Id), Is.EqualTo(edgeIds));
            Assert.That(new[]
            {
                result.Export.GeneratedFileWriteCount, result.Export.TilemapMutationCount,
                result.Export.SceneMutationCount, result.Export.PrefabMutationCount,
                result.Export.GameObjectMutationCount, result.Export.GameplaySpawnCount,
                result.Export.AuthoringMutationCount, result.Export.WorldPlanMutationCount,
                result.Export.IntersectorPlanMutationCount, result.Export.ReservationPlanMutationCount,
                result.Export.PacingDensityPlanMutationCount, result.Export.RollbackPlanMutationCount,
                result.Export.ProductionSeedApprovalCount, result.Export.FullRegressionCount,
            }.All(value => value == 0), Is.True);
        }

        [Test]
        public void Map15HandoffKeepsMap15_07Locked()
        {
            var result = fixture.Export();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(WorldAssemblyOverlayExport.DownstreamOwner,
                Is.EqualTo("MAP15_07_MAP15_WORLD_ASSEMBLY_EXIT_AUDIT"));
            Assert.That(WorldAssemblyOverlayExport.OpensDownstreamTask, Is.False);
            Assert.That(result.Export.Request.PublicationLabel,
                Is.EqualTo(WorldAssemblyOverlayExporter.ReferencePublicationLabel));
            Assert.That(result.Export.ProductionSeedApprovalCount, Is.Zero);
            Assert.That(result.Export.FullRegressionCount, Is.Zero);
        }

        private static string Join(WorldAssemblyOverlayResult result) => result == null
            ? "null"
            : string.Join(";", result.Failures.Select(value => value.ToString()));

        private sealed class ReferenceOverlayFixture
        {
            internal const string Map14PhaseExitDigest =
                "5b8ed6a3c8b0a20fe2f2d05eea0b7731522aff30e691ca606c638adcdbd62d82";
            internal const string RetryPatternLabel = "MAP14_RETRY_PATTERN_CANDIDATE";
            internal const string RetryClusterLabel = "MAP14_RETRY_CLUSTER_VARIANT";

            private ReferenceOverlayFixture(
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

            internal static ReferenceOverlayFixture Create()
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

                return new ReferenceOverlayFixture(
                    worldPlan, solveOrder, edgeResult.Plan, reservationResult.Plan,
                    pacingResult.Plan, rollbackResult.Plan);
            }

            internal WorldAssemblyOverlayRequest Request(
                IEnumerable<string> labels = null,
                int wholeWorldRerandomCount = 0,
                int fallbackCarveCount = 0,
                int silentWideningCount = 0,
                int generatedFileWriteCount = 0,
                int tilemapMutationCount = 0) => new WorldAssemblyOverlayRequest(
                    SolveOrder, IntersectorPlan, ReservationPlan, PacingPlan, RollbackPlan,
                    labels ?? WorldAssemblyOverlayExport.RequiredBatchLabels,
                    WorldAssemblyOverlayExporter.ReferencePublicationLabel,
                    wholeWorldRerandomCount: wholeWorldRerandomCount,
                    fallbackCarveCount: fallbackCarveCount,
                    silentWideningCount: silentWideningCount,
                    generatedFileWriteCount: generatedFileWriteCount,
                    tilemapMutationCount: tilemapMutationCount);

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
                bool boundary) => new WorldSocketProjection(
                    sector, side, anchor, false, mandatory, boundary,
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
