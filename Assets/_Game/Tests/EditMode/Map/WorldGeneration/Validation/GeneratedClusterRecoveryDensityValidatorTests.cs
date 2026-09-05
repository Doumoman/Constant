using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.Population;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.Validation;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Validation
{
    [TestFixture]
    [Category("MAP19_04")]
    public sealed class GeneratedClusterRecoveryDensityValidatorTests
    {
        private static GraphFixture cachedGraph;
        private static TerrainClusterRouteWitnessReport cachedRouteWitness;

        [Test]
        public void ClusterRecoveryValidatorPassesBaseHighAndExplicitRecoveryCases()
        {
            var scenario = CreateScenario();
            AssertSuccess(scenario.Result);
            var recovery = scenario.Result.Surface.Recovery;
            Assert.That(recovery.ClustersChecked, Is.EqualTo(1));
            Assert.That(recovery.RecoveryCasesChecked, Is.EqualTo(7));
            Assert.That(recovery.BaseRouteRecoveryCases, Is.EqualTo(1));
            Assert.That(recovery.HighRouteRecoveryCases, Is.EqualTo(1));
            Assert.That(recovery.ExplicitRecoveryCases, Is.EqualTo(1));
            Assert.That(recovery.TimedRecoveryCases, Is.EqualTo(1));
            Assert.That(recovery.RecoveryCasesPassed, Is.EqualTo(7));
            Assert.That(recovery.RecoveryCasesFailed, Is.Zero);
            Assert.That(recovery.Proofs.Select(value => value.GraphDigest),
                Is.All.EqualTo(scenario.Graph.GraphDigest));
            var density = scenario.Result.Surface.Density;
            TestContext.Out.WriteLine(string.Join("|", new[]
            {
                "MAP19_04_EVIDENCE_V1",
                "RECOVERY_INPUT=" + recovery.InputDigest,
                "RECOVERY_PROOF=" + recovery.ProofDigest,
                "RECOVERY_AVERAGE_EDGES=" + recovery.AverageRecoveryEdgeCount.ToString(
                    CultureInfo.InvariantCulture),
                "RECOVERY_MAX_EDGES=" + recovery.MaxRecoveryEdgeCount.ToString(
                    CultureInfo.InvariantCulture),
                "DENSITY_INPUT=" + density.InputDigest,
                "DENSITY_DIGEST=" + density.ValidationDigest,
                "DENSITY_WINDOWS=" + density.EightBySixAirWindowsFound.ToString(
                    CultureInfo.InvariantCulture),
                "COMBINED=" + scenario.Result.Surface.CombinedDigest,
                "MAP19_05_HANDOFF=" + scenario.Result.Surface.Map19_05HandoffDigest,
            }));
        }

        [Test]
        public void ClusterRecoveryValidatorEnforcesTwoToFiveSecondRecoveryBounds()
        {
            var scenario = CreateScenario();
            AssertSuccess(scenario.Result);
            var timed = scenario.Result.Surface.Recovery.Proofs.Single(value =>
                value.Kind == ClusterRecoveryCaseKind.TimedRecoveryWindow);
            Assert.That(timed.RecoveryCostMilliseconds, Is.EqualTo(2000));

            var cases = RecoveryCases(scenario.Graph, scenario.RouteWitness).ToList();
            var original = cases.Single(value => value.Kind ==
                ClusterRecoveryCaseKind.TimedRecoveryWindow);
            cases.Remove(original);
            cases.Add(Case(scenario.RouteWitness, "CASE_TIMED_OUT_OF_BOUND",
                ClusterRecoveryCaseKind.TimedRecoveryWindow, Node(scenario.Graph, 0, 0).NodeId,
                Node(scenario.Graph, 4, 0).NodeId, 6, "MAP11_ROUTE_WITNESS_2_TO_5_SECONDS",
                5001, 2000, 5000));
            AssertFailure(Validate(scenario, cases: cases),
                "RECOVERY_COST_EXCEEDS_2_TO_5S_BOUND");
        }

        [Test]
        public void ClusterDensityValidatorFindsRequiredEightBySixAirWindows()
        {
            var scenario = CreateScenario();
            AssertSuccess(scenario.Result);
            var density = scenario.Result.Surface.Density;
            Assert.That(density.ClustersChecked, Is.EqualTo(1));
            Assert.That(density.RequiredEightBySixAirWindows, Is.EqualTo(1));
            Assert.That(density.EightBySixAirWindowsFound, Is.GreaterThan(0));
            Assert.That(density.SolidRatioMinimumBasisPoints, Is.EqualTo(13));
            Assert.That(density.SolidRatioMaximumBasisPoints, Is.EqualTo(13));
            Assert.That(density.ReachableAirRatioMinimumBasisPoints, Is.EqualTo(10000));
            Assert.That(density.ReachableAirRatioMaximumBasisPoints, Is.EqualTo(10000));
            Assert.That(density.UnreachablePockets, Is.Zero);
            Assert.That(density.HeadSnagCandidates, Is.Zero);
            Assert.That(density.OneWayPitCandidates, Is.Zero);
            Assert.That(density.HardSolidObstructions, Is.Zero);
            Assert.That(density.OverprotectedEnvelopeObstructions, Is.Zero);
            Assert.That(density.BoundarySocketObstructions, Is.Zero);
        }

        [Test]
        public void ClusterDensityValidatorRejectsUnreachablePocketsHeadSnagsAndOneWayPits()
        {
            var scenario = CreateScenario();
            var missingWindowCanvas = MutateCanvas(scenario.Canvas, cell =>
                cell.Coordinate == new LocalTileCoord(5, 3)
                    ? CopyCell(cell, ResolvedLayerValue.FromId("SOLID_BLOCKED"),
                        cell.Layers.Hazard, cell.Layers.Marker)
                    : cell);
            var missingWindowRecovery = new ClusterRecoveryValidationInput(scenario.Graph,
                scenario.Naked, scenario.Completion, scenario.RecoveryInput.Cases);
            var missingWindowDensity = new ClusterDensityValidationInput(scenario.Graph,
                scenario.Naked, scenario.Completion, new[]
                {
                    new ClusterDensitySource(scenario.RouteWitness, "MAP11_ROUTE_WITNESS",
                        missingWindowCanvas, Slices(missingWindowCanvas), 4, 2, 11, 7, 1,
                        new[] { Node(scenario.Graph, 8, 0).NodeId }),
                });
            AssertFailure(GeneratedClusterRecoveryDensityValidator.Validate(
                missingWindowRecovery, missingWindowDensity), "MISSING_8X6_AIR_WINDOW");

            var pocketWalls = new HashSet<LocalTileCoord>
            {
                new LocalTileCoord(9, 10),
                new LocalTileCoord(10, 9),
                new LocalTileCoord(10, 11),
                new LocalTileCoord(11, 10),
            };
            var pocketCanvas = MutateCanvas(scenario.Canvas, cell =>
                pocketWalls.Contains(cell.Coordinate)
                    ? CopyCell(cell, ResolvedLayerValue.FromId("SOLID_BLOCKED"),
                        ResolvedLayerValue.Empty, cell.Layers.Marker)
                    : cell);
            AssertFailure(Validate(scenario, canvas: pocketCanvas),
                "UNREACHABLE_AIR_POCKET");

            var snagSolids = new HashSet<LocalTileCoord>
            {
                new LocalTileCoord(5, 3),
                new LocalTileCoord(5, 5),
            };
            var snagCanvas = MutateCanvas(scenario.Canvas, cell =>
                snagSolids.Contains(cell.Coordinate)
                    ? CopyCell(cell, ResolvedLayerValue.FromId("HARD_SOLID_BLOCKED"),
                        cell.Layers.Hazard, cell.Layers.Marker)
                    : cell);
            AssertFailure(Validate(scenario, canvas: snagCanvas), "HEAD_SNAG_CANDIDATE");

            var drop = scenario.Graph.Edges.First(value =>
                value.MovementKind == TraversalMovementKind.Drop &&
                scenario.Graph.Nodes.Any(node => node.NodeId == value.ToNodeId &&
                    node.Kind == GeneratedTileMovementNodeKind.Stand && node.Cell.X > 0));
            var dropCell = scenario.Graph.Nodes.Single(value =>
                value.NodeId == drop.ToNodeId).Cell;
            var pitCanvas = MutateCanvas(scenario.Canvas, cell =>
                cell.Coordinate == dropCell
                    ? CopyCell(cell, cell.Layers.Solid, cell.Layers.Hazard,
                        ResolvedLayerValue.FromId("ONE_WAY_PIT_CANDIDATE"))
                    : cell);
            AssertFailure(Validate(scenario, canvas: pitCanvas,
                densityRecoveryTargets: new[] { Node(scenario.Graph, 0, 0).NodeId }),
                "ONE_WAY_PIT_CANDIDATE");
        }

        [Test]
        public void ClusterRecoveryDensityValidatorConsumesMap19_02GraphAndMap19_03ProofReadOnly()
        {
            var scenario = CreateScenario();
            var before = GraphAndProofDigests(scenario);
            AssertSuccess(scenario.Result);
            var after = GraphAndProofDigests(scenario);
            Assert.That(after, Is.EqualTo(before));
            Assert.That(scenario.RecoveryInput.Graph, Is.SameAs(scenario.Graph));
            Assert.That(scenario.DensityInput.Graph, Is.SameAs(scenario.Graph));
            Assert.That(scenario.RecoveryInput.NakedSearch, Is.SameAs(scenario.Naked));
            Assert.That(scenario.RecoveryInput.CompletionSearch,
                Is.SameAs(scenario.Completion));
            Assert.That(scenario.Graph.GraphDigest,
                Is.EqualTo(GeneratedClusterRecoveryDensityValidator.ExpectedGraphDigest));
            Assert.That(scenario.Completion.Proof.ProofDigest,
                Is.EqualTo(GeneratedClusterRecoveryDensityValidator.ExpectedCompletionProofDigest));
            Assert.That(scenario.Completion.Map19_04HandoffDigest,
                Is.EqualTo(GeneratedClusterRecoveryDensityValidator.ExpectedIncomingHandoffDigest));
        }

        [Test]
        public void ClusterRecoveryDensityValidatorRejectsMissingBindingsAndDigestMismatches()
        {
            var scenario = CreateScenario();
            var missingProofRecovery = new ClusterRecoveryValidationInput(scenario.Graph, null,
                null, scenario.RecoveryInput.Cases);
            var missingProofDensity = new ClusterDensityValidationInput(scenario.Graph, null,
                null, scenario.DensityInput.Clusters);
            AssertFailure(GeneratedClusterRecoveryDensityValidator.Validate(
                missingProofRecovery, missingProofDensity), "MISSING_MAP19_03_PROOF_INPUT");

            var noCases = new ClusterRecoveryValidationInput(scenario.Graph, scenario.Naked,
                scenario.Completion, Array.Empty<ClusterRecoveryCase>());
            AssertFailure(GeneratedClusterRecoveryDensityValidator.Validate(noCases,
                scenario.DensityInput), "MISSING_CLUSTER_BINDING");

            var mismatch = new ClusterRecoveryValidationInput(scenario.Graph, scenario.Naked,
                scenario.Completion, scenario.RecoveryInput.Cases, null, null,
                new string('0', 64));
            AssertFailure(GeneratedClusterRecoveryDensityValidator.Validate(mismatch,
                scenario.DensityInput), "MAP19_03_HANDOFF_DIGEST_MISMATCH");
        }

        [Test]
        public void ClusterRecoveryDensityFailuresAreAtomicAndReportOwnerReasonExpectedActual()
        {
            var scenario = CreateScenario();
            var missingTarget = RecoveryCases(scenario.Graph, scenario.RouteWitness).ToList();
            missingTarget[0] = Case(scenario.RouteWitness, "CASE_MISSING_TARGET",
                ClusterRecoveryCaseKind.BaseRouteRejoin,
                Node(scenario.Graph, 0, 0).NodeId, new string('d', 64), 4, "FINITE");
            AssertFailure(Validate(scenario, cases: missingTarget),
                "MISSING_RECOVERY_TARGET");

            var unreachable = RecoveryCases(scenario.Graph, scenario.RouteWitness).ToList();
            unreachable[0] = Case(scenario.RouteWitness, "CASE_UNREACHABLE",
                ClusterRecoveryCaseKind.BaseRouteRejoin,
                Node(scenario.Graph, 0, 0).NodeId,
                scenario.Graph.Nodes.First(value => value.Kind ==
                    GeneratedTileMovementNodeKind.IntersectorSocket).NodeId,
                4, "FINITE", movements: new[] { TraversalMovementKind.Walk });
            AssertFailure(Validate(scenario, cases: unreachable),
                "UNREACHABLE_RECOVERY_TARGET");

            var shortBound = RecoveryCases(scenario.Graph, scenario.RouteWitness).ToList();
            shortBound[0] = Case(scenario.RouteWitness, "CASE_EDGE_BOUND",
                ClusterRecoveryCaseKind.BaseRouteRejoin,
                Node(scenario.Graph, 0, 0).NodeId, Node(scenario.Graph, 8, 0).NodeId,
                1, "FINITE");
            AssertFailure(Validate(scenario, cases: shortBound),
                "RECOVERY_EDGE_COST_EXCEEDS_BOUND");

            var hardCanvas = MutateCanvas(scenario.Canvas, cell =>
                cell.Coordinate == new LocalTileCoord(1, 0)
                    ? CopyCell(cell, ResolvedLayerValue.FromId("HARD_SOLID_BLOCKED"),
                        cell.Layers.Hazard, cell.Layers.Marker)
                    : cell);
            AssertFailure(Validate(scenario, canvas: hardCanvas),
                "HARD_SOLID_OBSTRUCTION");

            var overprotectedCanvas = MutateCanvas(scenario.Canvas, cell =>
                cell.Coordinate == new LocalTileCoord(10, 10)
                    ? new SectorCanvasCell(cell.Coordinate, cell.Layers,
                        new SectorCanvasProvenance(cell.Provenance.Sources.Concat(new[]
                        {
                            new CanvasSourceRef(CanvasSourceKind.Boundary,
                                "BOUNDARY_MAP19_04_OVERPROTECTED", 40, true,
                                new[] { SectorCanvasLayerKind.Solid }),
                        }), cell.Provenance.PersistenceKeys))
                    : cell);
            AssertFailure(Validate(scenario, canvas: overprotectedCanvas),
                "OVERPROTECTED_ENVELOPE_OBSTRUCTION");

            var socketCell = scenario.Graph.Nodes.First(value =>
                value.Kind == GeneratedTileMovementNodeKind.IntersectorSocket).Cell;
            var boundaryCanvas = MutateCanvas(scenario.Canvas, cell =>
                cell.Coordinate == socketCell
                    ? CopyCell(cell, ResolvedLayerValue.FromId("HARD_SOLID_BLOCKED"),
                        cell.Layers.Hazard, cell.Layers.Marker)
                    : cell);
            AssertFailure(Validate(scenario, canvas: boundaryCanvas),
                "BOUNDARY_SOCKET_OBSTRUCTION");

            var forbiddenRecovery = new ClusterRecoveryValidationInput(scenario.Graph,
                scenario.Naked, scenario.Completion, scenario.RecoveryInput.Cases,
                new GeneratedClusterValidationActionAudit(graphMutationAttempts: 1,
                    terrainMutationAttempts: 1, seedBatchAttempts: 1));
            AssertFailure(GeneratedClusterRecoveryDensityValidator.Validate(forbiddenRecovery,
                scenario.DensityInput), "FORBIDDEN_API_OR_MUTATION_ATTEMPT");
        }

        [Test]
        public void ClusterRecoveryDensityDigestIsStableAcrossRepeatReverseCultureAndCellOrder()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = CreateScenario();
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var reverse = CreateScenario(true, true);
                AssertSuccess(first.Result);
                AssertSuccess(reverse.Result);
                Assert.That(reverse.Result.Surface.Recovery.InputDigest,
                    Is.EqualTo(first.Result.Surface.Recovery.InputDigest));
                Assert.That(reverse.Result.Surface.Recovery.ProofDigest,
                    Is.EqualTo(first.Result.Surface.Recovery.ProofDigest));
                Assert.That(reverse.Result.Surface.Density.InputDigest,
                    Is.EqualTo(first.Result.Surface.Density.InputDigest));
                Assert.That(reverse.Result.Surface.Density.ValidationDigest,
                    Is.EqualTo(first.Result.Surface.Density.ValidationDigest));
                Assert.That(reverse.Result.Surface.CombinedDigest,
                    Is.EqualTo(first.Result.Surface.CombinedDigest));
                Assert.That(reverse.Result.Map19_05HandoffDigest,
                    Is.EqualTo(first.Result.Map19_05HandoffDigest));

                var mutatedCases = RecoveryCases(first.Graph, first.RouteWitness,
                    extraEdgeAllowance: 1);
                var mutation = Validate(first, cases: mutatedCases);
                AssertSuccess(mutation);
                Assert.That(mutation.Surface.Recovery.InputDigest,
                    Is.Not.EqualTo(first.Result.Surface.Recovery.InputDigest));
                Assert.That(mutation.Surface.Recovery.ProofDigest,
                    Is.Not.EqualTo(first.Result.Surface.Recovery.ProofDigest));
                Assert.That(mutation.Surface.CombinedDigest,
                    Is.Not.EqualTo(first.Result.Surface.CombinedDigest));
                Assert.That(mutation.Map19_05HandoffDigest,
                    Is.Not.EqualTo(first.Result.Map19_05HandoffDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void ClusterRecoveryDensityPublishesMap19_05HandoffSurface()
        {
            var scenario = CreateScenario();
            AssertSuccess(scenario.Result);
            foreach (var digest in new[]
                     {
                         scenario.Result.Surface.Recovery.InputDigest,
                         scenario.Result.Surface.Recovery.ProofDigest,
                         scenario.Result.Surface.Density.InputDigest,
                         scenario.Result.Surface.Density.ValidationDigest,
                         scenario.Result.Surface.CombinedDigest,
                         scenario.Result.Map19_05HandoffDigest,
                     })
                Assert.That(BakingCanonicalDigest.IsLowerHexSha256(digest), Is.True, digest);
        }

        [Test]
        public void Map19HandoffKeepsMap19_05Locked()
        {
            var scenario = CreateScenario();
            AssertSuccess(scenario.Result);
            Assert.That(scenario.Result.Surface.Map19_05Started, Is.False);
            Assert.That(scenario.RecoveryInput.ActionAudit.Map19_05Started, Is.False);
            Assert.That(scenario.DensityInput.ActionAudit.Map19_05Started, Is.False);
            Assert.That(scenario.Result.Map19_05HandoffDigest, Is.Not.Empty);
        }

        private static Scenario CreateScenario(
            bool reverseCases = false,
            bool reverseCells = false)
        {
            var graphFixture = Graph();
            var proofFixture = Proofs(graphFixture.Graph);
            var witness = RouteWitness();
            var cases = RecoveryCases(graphFixture.Graph, witness).ToList();
            if (reverseCases) cases.Reverse();
            var canvas = reverseCells
                ? CanvasFromCells(graphFixture.Canvas.Cells.Reverse())
                : graphFixture.Canvas;
            var slices = reverseCells
                ? Slices(canvas, true)
                : graphFixture.Slices;
            var recovery = new ClusterRecoveryValidationInput(graphFixture.Graph,
                proofFixture.Naked, proofFixture.Completion, cases);
            var density = new ClusterDensityValidationInput(graphFixture.Graph,
                proofFixture.Naked, proofFixture.Completion,
                new[] { Density(witness, canvas, slices, graphFixture.Graph) });
            var result = GeneratedClusterRecoveryDensityValidator.Validate(recovery, density);
            return new Scenario(graphFixture.Graph, canvas, slices, witness,
                proofFixture.Naked, proofFixture.Completion, recovery, density, result);
        }

        private static ClusterRecoveryValidationResult Validate(
            Scenario scenario,
            IEnumerable<ClusterRecoveryCase> cases = null,
            SectorCanvasContract canvas = null,
            IEnumerable<string> densityRecoveryTargets = null)
        {
            var selectedCases = cases ?? scenario.RecoveryInput.Cases;
            var recovery = new ClusterRecoveryValidationInput(scenario.Graph, scenario.Naked,
                scenario.Completion, selectedCases);
            var selectedCanvas = canvas ?? scenario.Canvas;
            var density = new ClusterDensityValidationInput(scenario.Graph, scenario.Naked,
                scenario.Completion, new[]
                {
                    Density(scenario.RouteWitness, selectedCanvas, Slices(selectedCanvas),
                        scenario.Graph, densityRecoveryTargets),
                });
            return GeneratedClusterRecoveryDensityValidator.Validate(recovery, density);
        }

        private static ClusterDensitySource Density(
            TerrainClusterRouteWitnessReport witness,
            SectorCanvasContract canvas,
            GeneratedSliceSet slices,
            GeneratedTileMovementGraph graph,
            IEnumerable<string> recoveryTargets = null) => new ClusterDensitySource(witness,
            "MAP11_ROUTE_WITNESS", canvas, slices, 0, 0, canvas.Width - 1,
            canvas.Height - 1, 1, recoveryTargets ?? new[] { Node(graph, 8, 0).NodeId });

        private static IEnumerable<ClusterRecoveryCase> RecoveryCases(
            GeneratedTileMovementGraph graph,
            TerrainClusterRouteWitnessReport witness,
            int extraEdgeAllowance = 0)
        {
            yield return Case(witness, "CASE_BASE_REJOIN",
                ClusterRecoveryCaseKind.BaseRouteRejoin, Node(graph, 1, 0).NodeId,
                Node(graph, 3, 0).NodeId, 3 + extraEdgeAllowance, "FINITE");
            yield return Case(witness, "CASE_HIGH_RECOVERY",
                ClusterRecoveryCaseKind.HighRouteRecovery, Node(graph, 2, 0).NodeId,
                Node(graph, 4, 0).NodeId, 3 + extraEdgeAllowance, "FINITE");
            yield return Case(witness, "CASE_EXPLICIT_RECOVERY",
                ClusterRecoveryCaseKind.ExplicitRecoveryRoute, Node(graph, 3, 0).NodeId,
                Node(graph, 5, 0).NodeId, 3 + extraEdgeAllowance, "FINITE");
            yield return Case(witness, "CASE_TIMED_RECOVERY",
                ClusterRecoveryCaseKind.TimedRecoveryWindow, Node(graph, 0, 0).NodeId,
                Node(graph, 4, 0).NodeId, 5 + extraEdgeAllowance,
                "MAP11_ROUTE_WITNESS_2_TO_5_SECONDS",
                witness.RecoveryRoutes.Single().TotalEstimatedDurationMilliseconds, 2000, 5000);
            yield return Case(witness, "CASE_ENTRY_TO_SPINE",
                ClusterRecoveryCaseKind.ClusterEntryToPrimarySpine,
                Node(graph, 0, 0).NodeId, Node(graph, 2, 0).NodeId,
                3 + extraEdgeAllowance, "FINITE");
            yield return Case(witness, "CASE_SPINE_TO_EXIT",
                ClusterRecoveryCaseKind.PrimarySpineToClusterExit,
                Node(graph, 2, 0).NodeId, Node(graph, 8, 0).NodeId,
                7 + extraEdgeAllowance, "FINITE");
            yield return Case(witness, "CASE_BOUNDARY_SOCKET_RECOVERY",
                ClusterRecoveryCaseKind.BoundarySocketRecovery,
                Node(graph, 6, 0).NodeId, Node(graph, 8, 0).NodeId,
                3 + extraEdgeAllowance, "FINITE");
        }

        private static ClusterRecoveryCase Case(
            TerrainClusterRouteWitnessReport witness,
            string id,
            ClusterRecoveryCaseKind kind,
            string start,
            string target,
            int maxEdges,
            string window,
            int cost = 0,
            int minimumCost = 0,
            int maximumCost = 0,
            IEnumerable<TraversalMovementKind> movements = null) => new ClusterRecoveryCase(
            witness, "MAP11_ROUTE_WITNESS", id, kind, start, new[] { target },
            movements ?? Movements(), maxEdges, window, cost, minimumCost, maximumCost);

        private static string[] GraphAndProofDigests(Scenario scenario) => new[]
        {
            scenario.Graph.NodeSetDigest, scenario.Graph.EdgeSetDigest,
            scenario.Graph.SocketLinkDigest, scenario.Graph.GraphDigest,
            scenario.Graph.Map19_03HandoffDigest, scenario.Naked.Surface.InputDigest,
            scenario.Naked.Surface.ProofDigest, scenario.Completion.Proof.InputDigest,
            scenario.Completion.Proof.StateSpaceDigest, scenario.Completion.Proof.ProofDigest,
            scenario.Completion.Map19_04HandoffDigest,
        };

        private static ProofFixture Proofs(GeneratedTileMovementGraph graph)
        {
            var start = Node(graph, 0, 0).NodeId;
            var targets = Enumerable.Range(1, 8).Select(x => Node(graph, x, 0).NodeId).ToArray();
            var naked = GeneratedNakedTraversalSearch.Search(new NakedTraversalSearchInput(
                graph, start, targets, Movements()));
            Assert.That(naked.Success, Is.True, string.Join("\n", naked.Failures));
            var resources = GeneratedMandatoryContentCatalog.CreateAuthoritative()
                .Where(value => value.IsCoreResource).OrderBy(value => value).ToArray();
            var transitions = new List<GeneratedCompletionStateTransition>
            {
                GeneratedCompletionStateTransition.FromMandatoryPopulation(
                    Node(graph, 1, 0).NodeId, resources[0], 1),
                GeneratedCompletionStateTransition.FromMandatoryPopulation(
                    Node(graph, 2, 0).NodeId, resources[1], 2),
                GeneratedCompletionStateTransition.FromMandatoryPopulation(
                    Node(graph, 3, 0).NodeId, resources[2], 4),
            };
            var forge = SpecialSource(GeneratedSpecialStateExportKind.Forge,
                "SITE_MOON_SEAL_FORGE", "FORGE_STATE", 'a');
            transitions.AddRange(new[]
            {
                SpecialTransition(graph, 4, forge,
                    GeneratedCompletionTransitionKind.MakeForgeAvailable),
                SpecialTransition(graph, 4, forge,
                    GeneratedCompletionTransitionKind.ActivateForge),
                SpecialTransition(graph, 5, forge,
                    GeneratedCompletionTransitionKind.OpenSeal),
                SpecialTransition(graph, 5, forge,
                    GeneratedCompletionTransitionKind.AcceptSeal),
            });
            var special = SpecialSource(GeneratedSpecialStateExportKind.ActivityEventRuntime,
                "SITE_REQUIRED_SPECIAL", "SPECIAL_STATE", 'b');
            transitions.AddRange(new[]
            {
                SpecialTransition(graph, 6, special,
                    GeneratedCompletionTransitionKind.EnterSpecial),
                SpecialTransition(graph, 6, special,
                    GeneratedCompletionTransitionKind.ResolveSpecial),
                SpecialTransition(graph, 6, special,
                    GeneratedCompletionTransitionKind.ExitSpecial),
            });
            var boss = SpecialSource(GeneratedSpecialStateExportKind.Boss,
                "SITE_MOON_BOSS_VAULT", "BOSS_STATE", 'c');
            transitions.AddRange(new[]
            {
                SpecialTransition(graph, 7, boss,
                    GeneratedCompletionTransitionKind.MakeBossAvailable),
                SpecialTransition(graph, 7, boss,
                    GeneratedCompletionTransitionKind.DefeatBoss),
            });
            var goal = new GeneratedCompletionGoal(Node(graph, 8, 0).NodeId, 7,
                GeneratedForgeValidationState.Activated,
                GeneratedSealValidationState.Accepted,
                GeneratedBossValidationState.Defeated,
                GeneratedSpecialValidationState.Exited);
            var completion = GeneratedCompletionSearch.Search(new GeneratedCompletionSearchInput(
                graph, naked, transitions, goal, Movements()));
            Assert.That(completion.Success, Is.True, string.Join("\n", completion.Failures));
            return new ProofFixture(naked, completion);
        }

        private static GeneratedCompletionStateTransition SpecialTransition(
            GeneratedTileMovementGraph graph,
            int x,
            GeneratedDeclaredSpecialStateSource source,
            GeneratedCompletionTransitionKind kind) =>
            GeneratedCompletionStateTransition.FromDeclaredSpecialState(
                Node(graph, x, 0).NodeId, source, kind);

        private static GeneratedDeclaredSpecialStateSource SpecialSource(
            GeneratedSpecialStateExportKind kind,
            string site,
            string state,
            char digestCharacter) => new GeneratedDeclaredSpecialStateSource(kind,
            "MAP18_SPECIAL_STATE_EXPORT", site,
            new SpecialPersistenceKey("SR_STATE_" + site),
            GeneratedSpecialStateSourceStatus.Active, state,
            new string(digestCharacter, 64));

        private static GraphFixture Graph()
        {
            if (cachedGraph != null) return cachedGraph;
            var canvas = CanvasFromCells(CreateCells());
            var slices = Slices(canvas);
            var profile = GeneratedTraversalProfileRuleLock.Lock(
                GeneratedTraversalProfileRuleLock.CreateAcceptedRequest());
            Assert.That(profile.Success, Is.True, string.Join("\n", profile.Failures));
            var result = GeneratedTileMovementGraphBuilder.Build(
                new GeneratedTileMovementGraphRequest(canvas, slices, profile));
            Assert.That(result.Success, Is.True, string.Join("\n", result.Failures));
            Assert.That(result.Graph.GraphDigest,
                Is.EqualTo(GeneratedClusterRecoveryDensityValidator.ExpectedGraphDigest));
            cachedGraph = new GraphFixture(result.Graph, canvas, slices);
            return cachedGraph;
        }

        private static SectorCanvasContract CanvasFromCells(
            IEnumerable<SectorCanvasCell> sourceCells)
        {
            var cells = sourceCells.ToArray();
            var stamp = new SectorCanvasValidationStamp(SectorCanvasValidationState.Validated,
                V2PassCatalog.StableDigest, GenerationLayerCatalog.StableDigest,
                BakingCanonicalDigest.ComputeSourceArtifactSet(cells),
                BakingCanonicalDigest.ComputeResolvedCells(cells), new string('e', 64));
            var canvas = new SectorCanvasContract(new SectorCanvasId("CANVAS_MAP19_02_FIXTURE"),
                WorldGenConstants.SectorWidthTiles, WorldGenConstants.SectorHeightTiles,
                cells, stamp);
            var validation = SectorCanvasContractValidator.Validate(canvas);
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
            return canvas;
        }

        private static GeneratedSliceSet Slices(
            SectorCanvasContract canvas,
            bool reverse = false)
        {
            var validation = SectorCanvasContractValidator.Validate(canvas);
            var slices = new List<GeneratedMicroChunkSlice>();
            for (var sliceY = 0; sliceY < WorldGenConstants.MicroChunkRowsPerSector; sliceY++)
            for (var sliceX = 0; sliceX < WorldGenConstants.MicroChunkColumnsPerSector; sliceX++)
            {
                var projected = new List<GeneratedSliceCell>();
                for (var localY = 0; localY < WorldGenConstants.MicroChunkHeightTiles; localY++)
                for (var localX = 0; localX < WorldGenConstants.MicroChunkWidthTiles; localX++)
                {
                    var x = sliceX * WorldGenConstants.MicroChunkWidthTiles + localX;
                    var y = sliceY * WorldGenConstants.MicroChunkHeightTiles + localY;
                    var source = canvas.Cells[y * WorldGenConstants.SectorWidthTiles + x];
                    projected.Add(new GeneratedSliceCell(new LocalTileCoord(localX, localY),
                        source.Layers, source.Provenance));
                }
                if (reverse) projected.Reverse();
                slices.Add(new GeneratedMicroChunkSlice(new GeneratedSliceCoord(sliceX, sliceY),
                    projected, new GeneratedSliceProvenance(canvas.Id,
                        validation.CanonicalDigest, canvas.ValidationStamp.StableDigest,
                        GeneratedSliceTransform.None)));
            }
            if (reverse) slices.Reverse();
            return new GeneratedSliceSet(canvas.Id, slices,
                GeneratedSliceBoundaryRole.GeneratedOutput);
        }

        private static IEnumerable<SectorCanvasCell> CreateCells()
        {
            for (var y = 0; y < WorldGenConstants.SectorHeightTiles; y++)
            for (var x = 0; x < WorldGenConstants.SectorWidthTiles; x++)
            {
                var localX = x % WorldGenConstants.MicroChunkWidthTiles;
                var localY = y % WorldGenConstants.MicroChunkHeightTiles;
                var sliceX = x / WorldGenConstants.MicroChunkWidthTiles;
                var sliceY = y / WorldGenConstants.MicroChunkHeightTiles;
                var blocked = x == 5 && y == 3;
                var hazard = x == 7 && y == 3;
                var protectedEnvelope = x == 5 && y == 1;
                var sources = new List<CanvasSourceRef> { OwnerSource() };
                var background = ResolvedLayerValue.Empty;
                if (protectedEnvelope)
                {
                    sources.Add(new CanvasSourceRef(CanvasSourceKind.Boundary,
                        "BOUNDARY_MAP19_02_PROTECTED", 20, true,
                        new[] { SectorCanvasLayerKind.Background }));
                    background = ResolvedLayerValue.FromId("BG_PROTECTED_ENVELOPE");
                }
                var affordance = localX == 1 && localY == 1
                    ? ResolvedLayerValue.FromId("TRAVERSAL_CLIMB")
                    : localX == 2 && localY == 1
                        ? ResolvedLayerValue.FromId("TRAVERSAL_BOUNCE")
                        : ResolvedLayerValue.Empty;
                var marker = x == 4 && y == 1
                    ? ResolvedLayerValue.FromId("CLEARANCE_BLOCKED")
                    : SocketMarker(localX, localY, sliceX, sliceY);
                yield return new SectorCanvasCell(new LocalTileCoord(x, y),
                    new SectorCanvasLayerSnapshot(
                        ResolvedLayerValue.FromId(blocked ? "SOLID_BLOCKED" : "SOLID_STONE"),
                        background, ResolvedLayerValue.FromId("SURFACE_STONE"), affordance,
                        ResolvedLayerValue.FromId("MAT_STONE"),
                        hazard ? ResolvedLayerValue.FromId("HAZARD_BLOCKED") :
                            ResolvedLayerValue.Empty,
                        marker, ResolvedLayerValue.FromId("TC_MAP19_02_OWNER")),
                    new SectorCanvasProvenance(sources));
            }
        }

        private static ResolvedLayerValue SocketMarker(
            int localX, int localY, int sliceX, int sliceY)
        {
            if (localX == WorldGenConstants.MicroChunkWidthTiles - 1 && sliceX < 3 && localY == 4)
                return ResolvedLayerValue.FromId("INTERSECTOR_SOCKET_RIGHT");
            if (localX == 0 && sliceX > 0 && localY == 4)
                return ResolvedLayerValue.FromId("INTERSECTOR_SOCKET_LEFT");
            if (localY == WorldGenConstants.MicroChunkHeightTiles - 1 && sliceY < 3 && localX == 6)
                return ResolvedLayerValue.FromId("INTERSECTOR_SOCKET_UP");
            if (localY == 0 && sliceY > 0 && localX == 6)
                return ResolvedLayerValue.FromId("INTERSECTOR_SOCKET_DOWN");
            return ResolvedLayerValue.Empty;
        }

        private static CanvasSourceRef OwnerSource() => new CanvasSourceRef(
            CanvasSourceKind.TerrainCluster, "TC_MAP19_02_OWNER", 30, true,
            new[] { SectorCanvasLayerKind.Solid, SectorCanvasLayerKind.Owner });

        private static SectorCanvasContract MutateCanvas(
            SectorCanvasContract source,
            Func<SectorCanvasCell, SectorCanvasCell> mutation) =>
            CanvasFromCells(source.Cells.Select(mutation));

        private static SectorCanvasCell CopyCell(
            SectorCanvasCell source,
            ResolvedLayerValue solid,
            ResolvedLayerValue hazard,
            ResolvedLayerValue marker) => new SectorCanvasCell(source.Coordinate,
            new SectorCanvasLayerSnapshot(solid, source.Layers.Background,
                source.Layers.Surface, source.Layers.Affordance, source.Layers.Material,
                hazard, marker, source.Layers.Owner), source.Provenance);

        private static TerrainClusterRouteWitnessReport RouteWitness()
        {
            if (cachedRouteWitness != null) return cachedRouteWitness;
            var contract = RouteContract();
            var validation = TerrainClusterContractValidator.Validate(contract);
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
            var canvas = TerrainClusterFootprintCompiler.Compile(
                new TerrainClusterFootprintCompileRequest(contract, ClusterFootprintTransform.R0));
            Assert.That(canvas.IsSuccess, Is.True, string.Join("\n", canvas.Errors));
            var sockets = TerrainClusterRoleSocketCompiler.Compile(
                new TerrainClusterRoleSocketCompileRequest(contract, validation.CanonicalDigest,
                    canvas.LocalCanvas, canvas.CanonicalDigest, new[]
                    {
                        new ClusterSectorSocketEvidence("SR_EXIT", "SOCKET_EXIT",
                            ClusterPortSide.R, 3, true, ClusterPortKind.Exit),
                        new ClusterSectorSocketEvidence("SR_ENTRY", "SOCKET_ENTRY",
                            ClusterPortSide.L, 2, true, ClusterPortKind.Entry),
                    }));
            Assert.That(sockets.IsSuccess, Is.True, string.Join("\n", sockets.Errors));
            var traversal = TerrainClusterTraversalCompiler.Compile(
                new TerrainClusterTraversalCompileRequest(contract, validation.CanonicalDigest,
                    canvas.LocalCanvas, canvas.CanonicalDigest, sockets.Contract,
                    sockets.CanonicalDigest));
            Assert.That(traversal.IsSuccess, Is.True, string.Join("\n", traversal.Errors));
            var high = new TerrainClusterHighRouteDefinition("HIGH_ROUTE_ONE",
                new SpineVariantId("SPINE_ALTERNATE"), "NODE_BUILD_UP",
                new[] { "EDGE_HIGH_01", "EDGE_HIGH_02", "EDGE_HIGH_03" },
                "NODE_CORE", "NODE_HIGH",
                new[] { "BENEFIT_HEIGHT_ADVANTAGE", "BENEFIT_REWARD_ACCESS" },
                new[] { "NODE_HIGH" });
            var durations = traversal.Compilation.Edges.Select(edge =>
                new TraversalEdgeDurationEvidence(edge.VariantId, edge.EdgeId,
                    edge.EdgeId == "EDGE_RECOVER" ? 2000 : 3000,
                    "RULESET_ROUTE_V1"));
            var witness = TerrainClusterRouteWitnessCompiler.Compile(
                new TerrainClusterRouteWitnessCompileRequest(canvas.LocalCanvas,
                    canvas.CanonicalDigest, sockets.Contract, sockets.CanonicalDigest,
                    traversal.Compilation, traversal.CanonicalDigest,
                    new TerrainClusterRouteWitnessIntent(
                        new SpineVariantId("SPINE_BASELINE"), new[] { high }, durations)));
            Assert.That(witness.IsSuccess, Is.True, string.Join("\n", witness.Errors));
            cachedRouteWitness = witness.Report;
            return cachedRouteWitness;
        }

        private static TerrainClusterContract RouteContract()
        {
            var roles = new[]
            {
                new ClusterRoleAnchor("ANCHOR_ENTRY", ClusterRoleKind.Entry,
                    new LocalTileCoord(0, 1), "NODE_ENTRY"),
                new ClusterRoleAnchor("ANCHOR_BUILD_UP", ClusterRoleKind.BuildUp,
                    new LocalTileCoord(5, 1), "NODE_BUILD_UP"),
                new ClusterRoleAnchor("ANCHOR_CORE", ClusterRoleKind.Core,
                    new LocalTileCoord(12, 1), "NODE_CORE"),
                new ClusterRoleAnchor("ANCHOR_RECOVERY", ClusterRoleKind.Recovery,
                    new LocalTileCoord(25, 1), "NODE_RECOVERY"),
                new ClusterRoleAnchor("ANCHOR_REWARD", ClusterRoleKind.Reward,
                    new LocalTileCoord(30, 1), "NODE_REWARD"),
                new ClusterRoleAnchor("ANCHOR_EXIT", ClusterRoleKind.Exit,
                    new LocalTileCoord(35, 1), "NODE_EXIT"),
            };
            var common = roles.Select(value => new TraversalNode(value.TraversalNodeId,
                value.Tile, value.Role != ClusterRoleKind.Reward, value.AnchorId)).Concat(new[]
            {
                new TraversalNode("NODE_STEP_A", new LocalTileCoord(8, 1), true, string.Empty),
                new TraversalNode("NODE_STEP_B", new LocalTileCoord(7, 1), false, string.Empty),
            }).ToArray();
            var alternate = common.Concat(new[]
            {
                new TraversalNode("NODE_HIGH", new LocalTileCoord(8, 3), false, string.Empty),
                new TraversalNode("NODE_HIGH_END", new LocalTileCoord(10, 3), false, string.Empty),
            }).ToArray();
            var commonById = common.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
            var alternateById = alternate.ToDictionary(value => value.NodeId,
                StringComparer.Ordinal);
            var baselineEdges = new[]
            {
                RouteEdge("EDGE_01_ENTRY", commonById["NODE_ENTRY"], commonById["NODE_BUILD_UP"], true),
                RouteEdge("EDGE_BASE_A1", commonById["NODE_BUILD_UP"], commonById["NODE_STEP_A"], true),
                RouteEdge("EDGE_BASE_A2", commonById["NODE_STEP_A"], commonById["NODE_CORE"], true),
                RouteEdge("EDGE_BASE_B1", commonById["NODE_BUILD_UP"], commonById["NODE_STEP_B"], false),
                RouteEdge("EDGE_BASE_B2", commonById["NODE_STEP_B"], commonById["NODE_CORE"], false),
                RouteEdge("EDGE_04_CORE", commonById["NODE_CORE"], commonById["NODE_RECOVERY"], true),
                RouteEdge("EDGE_05_RECOVERY", commonById["NODE_RECOVERY"], commonById["NODE_EXIT"], true),
            };
            var alternateEdges = baselineEdges.Select(edge => RouteEdge(edge.EdgeId,
                alternateById[edge.FromNodeId], alternateById[edge.ToNodeId], edge.IsMandatory))
                .Concat(new[]
                {
                    RouteEdge("EDGE_HIGH_01", alternateById["NODE_BUILD_UP"], alternateById["NODE_HIGH"], false),
                    RouteEdge("EDGE_HIGH_02", alternateById["NODE_HIGH"], alternateById["NODE_HIGH_END"], false),
                    RouteEdge("EDGE_HIGH_03", alternateById["NODE_HIGH_END"], alternateById["NODE_CORE"], false),
                    RouteEdge("EDGE_RECOVER", alternateById["NODE_HIGH"], alternateById["NODE_RECOVERY"], false),
                }).ToArray();
            return new TerrainClusterContract(new TerrainClusterId("TC_ROUTE_WITNESS"),
                new ClusterFootprint(new[]
                {
                    new ClusterChunkCoord(0, 0), new ClusterChunkCoord(1, 0),
                    new ClusterChunkCoord(2, 0), new ClusterChunkCoord(0, 1),
                }), roles, new[]
                {
                    new ClusterPort("PORT_ENTRY", ClusterPortKind.Entry, true, "ANCHOR_ENTRY",
                        new LocalTileCoord(0, 1), ClusterPortSide.L, new[] { 0, 1, 2, 3, 4 }),
                    new ClusterPort("PORT_EXIT", ClusterPortKind.Exit, true, "ANCHOR_EXIT",
                        new LocalTileCoord(35, 1), ClusterPortSide.R, new[] { 1, 2, 3, 4 }),
                }, new TerrainClusterTraversalContract(new[]
                {
                    new SpineVariant(new SpineVariantId("SPINE_BASELINE"), true,
                        TraversalGraphKind.Traversal, common, baselineEdges),
                    new SpineVariant(new SpineVariantId("SPINE_ALTERNATE"), false,
                        TraversalGraphKind.Traversal, alternate, alternateEdges),
                }), "MAP19_04 focused route witness");
        }

        private static TraversalEdge RouteEdge(
            string id,
            TraversalNode from,
            TraversalNode to,
            bool mandatory)
        {
            var envelope = new TraversalEnvelope(new[] { from.Tile, to.Tile },
                new[] { new LocalTileCoord(from.Tile.X, 0) },
                new[] { new LocalTileCoord(from.Tile.X, 5) },
                Array.Empty<LocalTileCoord>(), Array.Empty<LocalTileCoord>(),
                new[] { to.Tile }, new[] { to.Tile });
            return new TraversalEdge(id, from.NodeId, to.NodeId, TraversalMovementKind.Walk,
                from.Tile, to.Tile, 1, 2, to.Tile, to.Tile, mandatory, envelope);
        }

        private static GeneratedTileMovementNode Node(
            GeneratedTileMovementGraph graph,
            int x,
            int y) => graph.Nodes.Single(value =>
            value.Kind == GeneratedTileMovementNodeKind.Stand &&
            value.Cell.X == x && value.Cell.Y == y);

        private static TraversalMovementKind[] Movements() =>
            Enum.GetValues(typeof(TraversalMovementKind)).Cast<TraversalMovementKind>().ToArray();

        private static void AssertSuccess(ClusterRecoveryValidationResult result)
        {
            Assert.That(result.Success, Is.True, string.Join("\n", result.Failures));
            Assert.That(result.Surface, Is.Not.Null);
            Assert.That(result.RecoverySuccessDigest, Is.Not.Empty);
            Assert.That(result.DensitySuccessDigest, Is.Not.Empty);
            Assert.That(result.CombinedSuccessDigest, Is.Not.Empty);
            Assert.That(result.Map19_05HandoffDigest, Is.Not.Empty);
        }

        private static void AssertFailure(
            ClusterRecoveryValidationResult result,
            string reason)
        {
            Assert.That(result.Success, Is.False,
                "Expected atomic failure for " + reason);
            Assert.That(result.Surface, Is.Null);
            Assert.That(result.RecoverySuccessDigest, Is.Empty);
            Assert.That(result.DensitySuccessDigest, Is.Empty);
            Assert.That(result.CombinedSuccessDigest, Is.Empty);
            Assert.That(result.Map19_05HandoffDigest, Is.Empty);
            Assert.That(result.Failures.Select(value => value.Reason), Does.Contain(reason),
                string.Join("\n", result.Failures));
            Assert.That(result.Failures, Is.All.Matches<ClusterRecoveryFailure>(value =>
                value.Owner.Length != 0 && value.Reason.Length != 0 &&
                value.ClusterId.Length != 0 && value.CaseId.Length != 0 &&
                value.OffendingKey.Length != 0 && value.Expected.Length != 0 &&
                value.Actual.Length != 0 && value.SourceDigest.Length != 0));
        }

        private sealed class GraphFixture
        {
            public GraphFixture(GeneratedTileMovementGraph graph, SectorCanvasContract canvas,
                GeneratedSliceSet slices)
            {
                Graph = graph;
                Canvas = canvas;
                Slices = slices;
            }
            public GeneratedTileMovementGraph Graph { get; }
            public SectorCanvasContract Canvas { get; }
            public GeneratedSliceSet Slices { get; }
        }

        private sealed class ProofFixture
        {
            public ProofFixture(NakedTraversalSearchResult naked,
                GeneratedCompletionSearchResult completion)
            {
                Naked = naked;
                Completion = completion;
            }
            public NakedTraversalSearchResult Naked { get; }
            public GeneratedCompletionSearchResult Completion { get; }
        }

        private sealed class Scenario
        {
            public Scenario(GeneratedTileMovementGraph graph, SectorCanvasContract canvas,
                GeneratedSliceSet slices, TerrainClusterRouteWitnessReport routeWitness,
                NakedTraversalSearchResult naked, GeneratedCompletionSearchResult completion,
                ClusterRecoveryValidationInput recoveryInput,
                ClusterDensityValidationInput densityInput,
                ClusterRecoveryValidationResult result)
            {
                Graph = graph;
                Canvas = canvas;
                Slices = slices;
                RouteWitness = routeWitness;
                Naked = naked;
                Completion = completion;
                RecoveryInput = recoveryInput;
                DensityInput = densityInput;
                Result = result;
            }
            public GeneratedTileMovementGraph Graph { get; }
            public SectorCanvasContract Canvas { get; }
            public GeneratedSliceSet Slices { get; }
            public TerrainClusterRouteWitnessReport RouteWitness { get; }
            public NakedTraversalSearchResult Naked { get; }
            public GeneratedCompletionSearchResult Completion { get; }
            public ClusterRecoveryValidationInput RecoveryInput { get; }
            public ClusterDensityValidationInput DensityInput { get; }
            public ClusterRecoveryValidationResult Result { get; }
        }
    }
}
