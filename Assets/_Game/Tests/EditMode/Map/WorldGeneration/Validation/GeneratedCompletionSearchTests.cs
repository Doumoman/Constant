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
    [Category("MAP19_03")]
    public sealed class GeneratedCompletionSearchTests
    {
        private static GeneratedTileMovementGraph cachedGraph;

        [Test]
        public void NakedBfsFindsToolZeroReachableRequiredNodesOnTileGraph()
        {
            var scenario = CreateScenario();
            AssertNakedSuccess(scenario.Naked);
            Assert.That(scenario.Naked.Surface.ToolMask, Is.Zero);
            Assert.That(scenario.Naked.Surface.OptionalMovementSourceCount, Is.Zero);
            Assert.That(scenario.Naked.Surface.ConsumedRuntimePhysics, Is.False);
            Assert.That(scenario.Naked.Surface.Proofs, Has.Count.EqualTo(8));
            Assert.That(scenario.Naked.Surface.ReachedTargetCount, Is.EqualTo(8));
            Assert.That(scenario.Naked.Surface.VisitedNodeCount, Is.GreaterThan(8));
            Assert.That(scenario.Naked.Surface.VisitedEdgeCount, Is.GreaterThan(0));
            Assert.That(scenario.Naked.Surface.Proofs,
                Is.All.Matches<NakedReachabilityProof>(value => value.Found &&
                    value.ShortestEdgeCount > 0 && value.GraphDigest == scenario.Graph.GraphDigest));
        }

        [Test]
        public void CompletionSearchTracksPositionResourceForgeSealBossAndSpecialState()
        {
            var scenario = CreateScenario();
            AssertCompletionSuccess(scenario.Completion);
            var proof = scenario.Completion.Proof;
            Assert.That(proof.StateDimensionCount, Is.EqualTo(6));
            Assert.That(proof.InitialState.ResourceMask, Is.Zero);
            Assert.That(proof.FinalState.ResourceMask, Is.EqualTo(7));
            Assert.That(proof.FinalState.Forge, Is.EqualTo(GeneratedForgeValidationState.Activated));
            Assert.That(proof.FinalState.Seal, Is.EqualTo(GeneratedSealValidationState.Accepted));
            Assert.That(proof.FinalState.Boss, Is.EqualTo(GeneratedBossValidationState.Defeated));
            Assert.That(proof.FinalState.SpecialState,
                Is.EqualTo(GeneratedSpecialValidationState.Exited));
            Assert.That(proof.FinalState.PositionNodeId, Is.EqualTo(scenario.Goal.ExitNodeId));
        }

        [Test]
        public void CompletionSearchConsumesMandatoryPopulationAndSpecialGatesWithoutRuntimeObjects()
        {
            var scenario = CreateScenario();
            AssertCompletionSuccess(scenario.Completion);
            Assert.That(scenario.Transitions.Count(value => value.SourceKind ==
                GeneratedCompletionBindingSourceKind.MandatoryPopulationSlot), Is.EqualTo(3));
            Assert.That(scenario.Transitions.Count(value => value.SourceKind ==
                GeneratedCompletionBindingSourceKind.SpecialRegionState), Is.EqualTo(9));
            Assert.That(scenario.Transitions.Where(value => value.SourceKind ==
                    GeneratedCompletionBindingSourceKind.MandatoryPopulationSlot)
                .Select(value => value.SourceKey), Is.All.Contains("MANDATORY_CONTENT_KEY_V1"));
            Assert.That(scenario.Transitions.Select(value => value.SourceDigest),
                Is.All.Match("^[0-9a-f]{64}$"));
            Assert.That(scenario.Input.ActionAudit.IsZero, Is.True);
        }

        [Test]
        public void CompletionSearchRejectsMissingStartExitTargetsAndDanglingGoalBindings()
        {
            var scenario = CreateScenario();
            var movements = Movements();
            AssertNakedFailure(GeneratedNakedTraversalSearch.Search(new NakedTraversalSearchInput(
                scenario.Graph, new string('a', 64), scenario.Targets, movements)),
                "MISSING_START_NODE");
            AssertNakedFailure(GeneratedNakedTraversalSearch.Search(new NakedTraversalSearchInput(
                scenario.Graph, scenario.StartNodeId,
                scenario.Targets.Append(new string('b', 64)), movements)),
                "MISSING_TARGET_NODE");

            var badGoal = new GeneratedCompletionGoal(new string('c', 64), 7,
                GeneratedForgeValidationState.Activated,
                GeneratedSealValidationState.Accepted,
                GeneratedBossValidationState.Defeated,
                GeneratedSpecialValidationState.Exited);
            AssertCompletionFailure(GeneratedCompletionSearch.Search(
                CompletionInput(scenario, scenario.Transitions, badGoal)), "MISSING_EXIT_TARGET");

            var dangling = new GeneratedCompletionStateTransition(new string('d', 64),
                GeneratedCompletionTransitionKind.MakeForgeAvailable,
                GeneratedCompletionBindingSourceKind.SpecialRegionState,
                "DANGLING_BINDING", new string('d', 64));
            AssertCompletionFailure(GeneratedCompletionSearch.Search(CompletionInput(
                scenario, scenario.Transitions.Append(dangling), scenario.Goal)),
                "DANGLING_COMPLETION_TARGET_BINDING");
        }

        [Test]
        public void CompletionSearchDoesNotUsePhysicsTilemapPlayerControllerOrSeedBatch()
        {
            var scenario = CreateScenario();
            AssertCompletionSuccess(scenario.Completion);
            Assert.That(scenario.Input.ActionAudit.IsZero, Is.True);
            Assert.That(scenario.Naked.Surface.ToolMask, Is.Zero);
            Assert.That(scenario.Naked.Surface.OptionalMovementSourceCount, Is.Zero);
            Assert.That(scenario.Naked.Surface.ConsumedRuntimePhysics, Is.False);
            Assert.That(scenario.Completion.Proof.Map19_04Started, Is.False);
            Assert.That(scenario.Graph, Is.SameAs(scenario.Input.Graph));
        }

        [Test]
        public void CompletionSearchPreservesMap19_02GraphDigestsAndRuleRegistryInputs()
        {
            var scenario = CreateScenario();
            var before = new[]
            {
                scenario.Graph.TraversalProfileDigest, scenario.Graph.RuleRegistryDigest,
                scenario.Graph.MovementEnvelopeMatrixDigest, scenario.Graph.Map19_02IncomingHandoffDigest,
                scenario.Graph.NodeSetDigest, scenario.Graph.EdgeSetDigest,
                scenario.Graph.SocketLinkDigest, scenario.Graph.GraphDigest,
                scenario.Graph.Map19_03HandoffDigest,
            };
            AssertNakedSuccess(scenario.Naked);
            AssertCompletionSuccess(scenario.Completion);
            var after = new[]
            {
                scenario.Graph.TraversalProfileDigest, scenario.Graph.RuleRegistryDigest,
                scenario.Graph.MovementEnvelopeMatrixDigest, scenario.Graph.Map19_02IncomingHandoffDigest,
                scenario.Graph.NodeSetDigest, scenario.Graph.EdgeSetDigest,
                scenario.Graph.SocketLinkDigest, scenario.Graph.GraphDigest,
                scenario.Graph.Map19_03HandoffDigest,
            };
            Assert.That(after, Is.EqualTo(before));
            Assert.That(scenario.Graph.GraphDigest,
                Is.EqualTo(GeneratedNakedTraversalSearch.ExpectedGraphDigest));
            Assert.That(scenario.Graph.Map19_03HandoffDigest,
                Is.EqualTo(GeneratedNakedTraversalSearch.ExpectedIncomingHandoffDigest));
        }

        [Test]
        public void CompletionSearchFailuresAreAtomicAndReportOwnerReasonExpectedActual()
        {
            var scenario = CreateScenario();
            var movements = Movements();
            AssertNakedFailure(GeneratedNakedTraversalSearch.Search(new NakedTraversalSearchInput(
                null, scenario.StartNodeId, scenario.Targets, movements)), "MISSING_GRAPH_INPUT");

            var mismatchGraph = new GeneratedTileMovementGraph(scenario.Graph.Nodes,
                scenario.Graph.Edges, scenario.Graph.SocketLinks, scenario.Graph.Stats,
                scenario.Graph.TraversalProfileDigest, scenario.Graph.RuleRegistryDigest,
                scenario.Graph.MovementEnvelopeMatrixDigest,
                scenario.Graph.Map19_02IncomingHandoffDigest, scenario.Graph.SourceTerrainDigest,
                declaredGraphDigest: new string('0', 64));
            AssertNakedFailure(GeneratedNakedTraversalSearch.Search(new NakedTraversalSearchInput(
                mismatchGraph, scenario.StartNodeId, scenario.Targets, movements)),
                "GRAPH_DIGEST_MISMATCH");

            var unreachable = scenario.Graph.Nodes.First(value => value.Kind ==
                GeneratedTileMovementNodeKind.IntersectorSocket).NodeId;
            AssertNakedFailure(GeneratedNakedTraversalSearch.Search(new NakedTraversalSearchInput(
                scenario.Graph, scenario.StartNodeId, new[] { unreachable }, movements)),
                "UNREACHABLE_MANDATORY_TARGET");

            var unboundNode = Node(scenario.Graph, 9, 0).NodeId;
            var unbound = new GeneratedCompletionStateTransition(unboundNode,
                GeneratedCompletionTransitionKind.CollectMandatoryResource,
                GeneratedCompletionBindingSourceKind.MandatoryPopulationSlot,
                "UNBOUND_POSITION", new string('e', 64), 8);
            AssertCompletionFailure(GeneratedCompletionSearch.Search(CompletionInput(
                scenario, scenario.Transitions.Append(unbound), scenario.Goal)),
                "STATE_TRANSITION_POSITION_MISMATCH");

            AssertCompletionFailure(GeneratedCompletionSearch.Search(CompletionInput(
                scenario, scenario.Transitions.Append(scenario.Transitions[0]), scenario.Goal)),
                "DUPLICATE_STATE_TRANSITION_ID");

            AssertNakedFailure(GeneratedNakedTraversalSearch.Search(new NakedTraversalSearchInput(
                scenario.Graph, scenario.StartNodeId, scenario.Targets,
                movements.Append((TraversalMovementKind)999))), "UNSUPPORTED_MOVEMENT_KIND");

            var forbiddenInput = new GeneratedCompletionSearchInput(scenario.Graph,
                scenario.Naked, scenario.Transitions, scenario.Goal, movements,
                new GeneratedCompletionActionAudit(implicitActionAttempts: 1,
                    externalStateMutationAttempts: 1, terrainMutationAttempts: 1,
                    seedBatchAttempts: 1));
            var forbidden = GeneratedCompletionSearch.Search(forbiddenInput);
            AssertCompletionFailure(forbidden, "IMPLICIT_OR_FORBIDDEN_ACTION_ATTEMPT");
            Assert.That(forbidden.Failures,
                Is.All.Matches<GeneratedCompletionSearchFailure>(value =>
                    value.Owner.Length != 0 && value.Reason.Length != 0 &&
                    value.OffendingKey.Length != 0 && value.Expected.Length != 0 &&
                    value.Actual.Length != 0 && value.SourceDigest.Length != 0));
        }

        [Test]
        public void CompletionSearchDigestIsStableAcrossRepeatReverseCultureAndGoalOrder()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = CreateScenario();
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var reverse = CreateScenario(true, true);
                Assert.That(reverse.Naked.Surface.InputDigest,
                    Is.EqualTo(first.Naked.Surface.InputDigest));
                Assert.That(reverse.Naked.Surface.ProofDigest,
                    Is.EqualTo(first.Naked.Surface.ProofDigest));
                Assert.That(reverse.Completion.Proof.InputDigest,
                    Is.EqualTo(first.Completion.Proof.InputDigest));
                Assert.That(reverse.Completion.Proof.StateSpaceDigest,
                    Is.EqualTo(first.Completion.Proof.StateSpaceDigest));
                Assert.That(reverse.Completion.Proof.ProofDigest,
                    Is.EqualTo(first.Completion.Proof.ProofDigest));
                Assert.That(reverse.Completion.Map19_04HandoffDigest,
                    Is.EqualTo(first.Completion.Map19_04HandoffDigest));

                var mutatedGoal = new GeneratedCompletionGoal(first.Goal.ExitNodeId, 3,
                    first.Goal.Forge, first.Goal.Seal, first.Goal.Boss,
                    first.Goal.SpecialState);
                var mutation = GeneratedCompletionSearch.Search(CompletionInput(
                    first, first.Transitions, mutatedGoal));
                AssertCompletionSuccess(mutation);
                Assert.That(mutation.Proof.InputDigest,
                    Is.Not.EqualTo(first.Completion.Proof.InputDigest));
                Assert.That(mutation.Proof.ProofDigest,
                    Is.Not.EqualTo(first.Completion.Proof.ProofDigest));
                Assert.That(mutation.Map19_04HandoffDigest,
                    Is.Not.EqualTo(first.Completion.Map19_04HandoffDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void CompletionSearchPublishesMap19_04HandoffSurface()
        {
            var scenario = CreateScenario();
            AssertCompletionSuccess(scenario.Completion);
            foreach (var digest in new[]
                     {
                         scenario.Naked.Surface.InputDigest, scenario.Naked.Surface.ProofDigest,
                         scenario.Completion.Proof.InputDigest,
                         scenario.Completion.Proof.StateSpaceDigest,
                         scenario.Completion.Proof.ProofDigest,
                         scenario.Completion.Map19_04HandoffDigest,
                     })
                Assert.That(BakingCanonicalDigest.IsLowerHexSha256(digest), Is.True, digest);
        }

        [Test]
        public void Map19HandoffKeepsMap19_04Locked()
        {
            var scenario = CreateScenario();
            AssertCompletionSuccess(scenario.Completion);
            Assert.That(scenario.Completion.Proof.Map19_04Started, Is.False);
            Assert.That(scenario.Input.ActionAudit.Map19_04Started, Is.False);
            Assert.That(scenario.Completion.Map19_04HandoffDigest, Is.Not.Empty);
        }

        private static Scenario CreateScenario(
            bool reverseTransitions = false,
            bool reverseTargets = false)
        {
            var graph = Graph();
            var start = Node(graph, 0, 0).NodeId;
            var targetNodes = Enumerable.Range(1, 8).Select(x => Node(graph, x, 0).NodeId)
                .ToList();
            if (reverseTargets) targetNodes.Reverse();
            var nakedInput = new NakedTraversalSearchInput(graph, start, targetNodes,
                reverseTargets ? Movements().Reverse() : Movements());
            var naked = GeneratedNakedTraversalSearch.Search(nakedInput);
            AssertNakedSuccess(naked);

            var resources = GeneratedMandatoryContentCatalog.CreateAuthoritative()
                .Where(value => value.IsCoreResource).OrderBy(value => value).ToArray();
            Assert.That(resources, Has.Length.EqualTo(3));
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
                GeneratedCompletionStateTransition.FromDeclaredSpecialState(
                    Node(graph, 4, 0).NodeId, forge,
                    GeneratedCompletionTransitionKind.MakeForgeAvailable),
                GeneratedCompletionStateTransition.FromDeclaredSpecialState(
                    Node(graph, 4, 0).NodeId, forge,
                    GeneratedCompletionTransitionKind.ActivateForge),
                GeneratedCompletionStateTransition.FromDeclaredSpecialState(
                    Node(graph, 5, 0).NodeId, forge,
                    GeneratedCompletionTransitionKind.OpenSeal),
                GeneratedCompletionStateTransition.FromDeclaredSpecialState(
                    Node(graph, 5, 0).NodeId, forge,
                    GeneratedCompletionTransitionKind.AcceptSeal),
            });
            var special = SpecialSource(GeneratedSpecialStateExportKind.ActivityEventRuntime,
                "SITE_REQUIRED_SPECIAL", "SPECIAL_STATE", 'b');
            transitions.AddRange(new[]
            {
                GeneratedCompletionStateTransition.FromDeclaredSpecialState(
                    Node(graph, 6, 0).NodeId, special,
                    GeneratedCompletionTransitionKind.EnterSpecial),
                GeneratedCompletionStateTransition.FromDeclaredSpecialState(
                    Node(graph, 6, 0).NodeId, special,
                    GeneratedCompletionTransitionKind.ResolveSpecial),
                GeneratedCompletionStateTransition.FromDeclaredSpecialState(
                    Node(graph, 6, 0).NodeId, special,
                    GeneratedCompletionTransitionKind.ExitSpecial),
            });
            var boss = SpecialSource(GeneratedSpecialStateExportKind.Boss,
                "SITE_MOON_BOSS_VAULT", "BOSS_STATE", 'c');
            transitions.AddRange(new[]
            {
                GeneratedCompletionStateTransition.FromDeclaredSpecialState(
                    Node(graph, 7, 0).NodeId, boss,
                    GeneratedCompletionTransitionKind.MakeBossAvailable),
                GeneratedCompletionStateTransition.FromDeclaredSpecialState(
                    Node(graph, 7, 0).NodeId, boss,
                    GeneratedCompletionTransitionKind.DefeatBoss),
            });
            if (reverseTransitions) transitions.Reverse();
            var goal = new GeneratedCompletionGoal(Node(graph, 8, 0).NodeId, 7,
                GeneratedForgeValidationState.Activated,
                GeneratedSealValidationState.Accepted,
                GeneratedBossValidationState.Defeated,
                GeneratedSpecialValidationState.Exited);
            var input = new GeneratedCompletionSearchInput(graph, naked, transitions, goal,
                reverseTransitions ? Movements().Reverse() : Movements());
            var completion = GeneratedCompletionSearch.Search(input);
            AssertCompletionSuccess(completion);
            return new Scenario(graph, start, targetNodes, naked, transitions, goal, input,
                completion);
        }

        private static GeneratedCompletionSearchInput CompletionInput(
            Scenario scenario,
            IEnumerable<GeneratedCompletionStateTransition> transitions,
            GeneratedCompletionGoal goal) => new GeneratedCompletionSearchInput(
            scenario.Graph, scenario.Naked, transitions, goal, Movements());

        private static GeneratedDeclaredSpecialStateSource SpecialSource(
            GeneratedSpecialStateExportKind kind,
            string site,
            string state,
            char digestCharacter) => new GeneratedDeclaredSpecialStateSource(kind,
            "MAP18_SPECIAL_STATE_EXPORT", site,
            new SpecialPersistenceKey("SR_STATE_" + site),
            GeneratedSpecialStateSourceStatus.Active, state,
            new string(digestCharacter, 64));

        private static TraversalMovementKind[] Movements() =>
            Enum.GetValues(typeof(TraversalMovementKind)).Cast<TraversalMovementKind>().ToArray();

        private static GeneratedTileMovementNode Node(
            GeneratedTileMovementGraph graph, int x, int y) => graph.Nodes.Single(value =>
            value.Kind == GeneratedTileMovementNodeKind.Stand &&
            value.Cell.X == x && value.Cell.Y == y);

        private static GeneratedTileMovementGraph Graph()
        {
            if (cachedGraph != null) return cachedGraph;
            var cells = CreateCells().ToArray();
            var stamp = new SectorCanvasValidationStamp(SectorCanvasValidationState.Validated,
                V2PassCatalog.StableDigest, GenerationLayerCatalog.StableDigest,
                BakingCanonicalDigest.ComputeSourceArtifactSet(cells),
                BakingCanonicalDigest.ComputeResolvedCells(cells), new string('e', 64));
            var canvas = new SectorCanvasContract(new SectorCanvasId("CANVAS_MAP19_02_FIXTURE"),
                WorldGenConstants.SectorWidthTiles, WorldGenConstants.SectorHeightTiles,
                cells, stamp);
            var canvasValidation = SectorCanvasContractValidator.Validate(canvas);
            Assert.That(canvasValidation.IsValid, Is.True, string.Join("\n", canvasValidation.Errors));
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
                slices.Add(new GeneratedMicroChunkSlice(new GeneratedSliceCoord(sliceX, sliceY),
                    projected, new GeneratedSliceProvenance(canvas.Id,
                        canvasValidation.CanonicalDigest, canvas.ValidationStamp.StableDigest,
                        GeneratedSliceTransform.None)));
            }
            var sliceSet = new GeneratedSliceSet(canvas.Id, slices,
                GeneratedSliceBoundaryRole.GeneratedOutput);
            var lockResult = GeneratedTraversalProfileRuleLock.Lock(
                GeneratedTraversalProfileRuleLock.CreateAcceptedRequest());
            Assert.That(lockResult.Success, Is.True, string.Join("\n", lockResult.Failures));
            var graphResult = GeneratedTileMovementGraphBuilder.Build(
                new GeneratedTileMovementGraphRequest(canvas, sliceSet, lockResult));
            Assert.That(graphResult.Success, Is.True, string.Join("\n", graphResult.Failures));
            cachedGraph = graphResult.Graph;
            return cachedGraph;
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
                        hazard ? ResolvedLayerValue.FromId("HAZARD_BLOCKED") : ResolvedLayerValue.Empty,
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

        private static void AssertNakedSuccess(NakedTraversalSearchResult result)
        {
            Assert.That(result.Success, Is.True, string.Join("\n", result.Failures));
            Assert.That(result.Surface, Is.Not.Null);
            Assert.That(result.SuccessProofDigest, Is.Not.Empty);
        }

        private static void AssertNakedFailure(
            NakedTraversalSearchResult result, string reason)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Surface, Is.Null);
            Assert.That(result.SuccessProofDigest, Is.Empty);
            Assert.That(result.Failures.Select(value => value.Reason), Does.Contain(reason),
                string.Join("\n", result.Failures));
        }

        private static void AssertCompletionSuccess(GeneratedCompletionSearchResult result)
        {
            Assert.That(result.Success, Is.True, string.Join("\n", result.Failures));
            Assert.That(result.Proof, Is.Not.Null);
            Assert.That(result.SuccessProofDigest, Is.Not.Empty);
            Assert.That(result.Map19_04HandoffDigest, Is.Not.Empty);
        }

        private static void AssertCompletionFailure(
            GeneratedCompletionSearchResult result, string reason)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Proof, Is.Null);
            Assert.That(result.SuccessProofDigest, Is.Empty);
            Assert.That(result.Map19_04HandoffDigest, Is.Empty);
            Assert.That(result.Failures.Select(value => value.Reason), Does.Contain(reason),
                string.Join("\n", result.Failures));
        }

        private sealed class Scenario
        {
            public Scenario(
                GeneratedTileMovementGraph graph,
                string startNodeId,
                IReadOnlyList<string> targets,
                NakedTraversalSearchResult naked,
                IReadOnlyList<GeneratedCompletionStateTransition> transitions,
                GeneratedCompletionGoal goal,
                GeneratedCompletionSearchInput input,
                GeneratedCompletionSearchResult completion)
            {
                Graph = graph;
                StartNodeId = startNodeId;
                Targets = targets;
                Naked = naked;
                Transitions = transitions;
                Goal = goal;
                Input = input;
                Completion = completion;
            }
            public GeneratedTileMovementGraph Graph { get; }
            public string StartNodeId { get; }
            public IReadOnlyList<string> Targets { get; }
            public NakedTraversalSearchResult Naked { get; }
            public IReadOnlyList<GeneratedCompletionStateTransition> Transitions { get; }
            public GeneratedCompletionGoal Goal { get; }
            public GeneratedCompletionSearchInput Input { get; }
            public GeneratedCompletionSearchResult Completion { get; }
        }
    }
}
