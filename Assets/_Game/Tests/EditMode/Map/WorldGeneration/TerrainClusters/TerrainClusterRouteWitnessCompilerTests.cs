using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.TerrainClusters.Tests
{
    [TestFixture]
    [Category("MAP11_04")]
    public sealed class TerrainClusterRouteWitnessCompilerTests
    {
        [Test]
        public void Compile_ValidIntent_PublishesExactShellAndAllWitnesses()
        {
            var fixture = BuildFixture();
            var result = Compile(fixture, CreateIntent(fixture));

            Assert.That(result.IsSuccess, Is.True, ErrorText(result));
            var active = fixture.Canvas.TileCells.Where(value => value.State == ClusterChunkMaskState.Active).ToArray();
            var inactive = fixture.Canvas.TileCells.Where(value => value.State != ClusterChunkMaskState.Active).ToArray();
            Assert.That(result.StaticShell.Cells.Count, Is.EqualTo(active.Length));
            Assert.That(result.StaticShell.Cells.Select(value => value.CompiledCoordinate).Distinct().Count(), Is.EqualTo(active.Length));
            Assert.That(inactive.All(value => !result.StaticShell.TryGetCell(value.Coordinate, out _)), Is.True);
            Assert.That(result.StaticShell.Cells.Any(value => value.Occupancy == TerrainClusterShellOccupancy.Solid), Is.True);
            Assert.That(result.StaticShell.Cells.Any(value => value.Occupancy == TerrainClusterShellOccupancy.Air && value.Provenance.Count == 0), Is.True);
            Assert.That(result.StaticShell.Cells.Where(value => value.IsProtectedOpen)
                .All(value => value.Occupancy == TerrainClusterShellOccupancy.Air), Is.True);
            Assert.That(result.StaticShell.PatternOperationCount, Is.Zero);

            Assert.That(result.BaselineRoute.VariantId, Is.EqualTo(new SpineVariantId("SPINE_BASELINE")));
            Assert.That(result.BaselineRoute.OrderedEdgeIds(), Is.EqualTo(new[]
            {
                "EDGE_01_ENTRY", "EDGE_BASE_A1", "EDGE_BASE_A2", "EDGE_04_CORE",
                "EDGE_05_RECOVERY",
            }));
            Assert.That(result.BaselineRoute.EntryPortId, Is.EqualTo("PORT_ENTRY"));
            Assert.That(result.BaselineRoute.ExitPortId, Is.EqualTo("PORT_EXIT"));
            Assert.That(result.BaselineRoute.PreservedMandatoryRoles, Is.EqualTo(new[]
            {
                ClusterRoleKind.BuildUp, ClusterRoleKind.Core, ClusterRoleKind.Recovery,
            }));
            Assert.That(result.BaselineRoute.PatternOperationCount, Is.Zero);

            Assert.That(result.HighRoutes, Has.Count.EqualTo(1));
            Assert.That(result.HighRoutes[0].OrderedEdgeIds(), Is.EqualTo(new[]
            {
                "EDGE_HIGH_01", "EDGE_HIGH_02", "EDGE_HIGH_03",
            }));
            Assert.That(result.HighRoutes[0].OrderedNodeIds, Does.Contain("NODE_HIGH"));
            Assert.That(result.HighRoutes[0].BenefitIds, Has.Count.EqualTo(2));
            Assert.That(result.RecoveryRoutes, Has.Count.EqualTo(1));
            Assert.That(result.RecoveryRoutes[0].FailureNodeId, Is.EqualTo("NODE_HIGH"));
            Assert.That(result.RecoveryRoutes[0].TargetBaselineNodeId, Is.EqualTo("NODE_RECOVERY"));
            Assert.That(result.RecoveryRoutes[0].TargetsRecoveryRole, Is.True);
            Assert.That(result.RecoveryRoutes[0].TotalEstimatedDurationMilliseconds, Is.EqualTo(2000));
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        [Test]
        public void Compile_ReversedInputsAndCulture_KeepCanonicalDigest()
        {
            var first = BuildFixture();
            var second = BuildFixture(reverseInput: true);
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                var firstResult = Compile(first, CreateIntent(first));
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                var secondResult = Compile(second, CreateIntent(second, reverseInput: true));
                Assert.That(firstResult.IsSuccess, Is.True, ErrorText(firstResult));
                Assert.That(secondResult.IsSuccess, Is.True, ErrorText(secondResult));
                Assert.That(secondResult.CanonicalDigest, Is.EqualTo(firstResult.CanonicalDigest));
                Assert.That(secondResult.BaselineRoute.OrderedEdgeIds(), Is.EqualTo(firstResult.BaselineRoute.OrderedEdgeIds()));
            }
            finally { CultureInfo.CurrentCulture = originalCulture; }
        }

        [Test]
        public void Compile_SemanticDurationChange_ChangesDigest()
        {
            var fixture = BuildFixture();
            var first = Compile(fixture, CreateIntent(fixture));
            var changed = Compile(fixture, CreateIntent(fixture, durationDeltaEdgeId: "EDGE_HIGH_02"));
            Assert.That(first.IsSuccess, Is.True, ErrorText(first));
            Assert.That(changed.IsSuccess, Is.True, ErrorText(changed));
            Assert.That(changed.CanonicalDigest, Is.Not.EqualTo(first.CanonicalDigest));
        }

        [Test]
        public void Compile_WrongBaselineVariant_FailsAtomically()
        {
            var fixture = BuildFixture();
            var intent = CreateIntent(fixture, baselineId: new SpineVariantId("SPINE_ALTERNATE"));
            AssertFailure(Compile(fixture, intent), TerrainClusterRouteWitnessCompileErrorCode.InvalidBaselineVariant);
        }

        [Test]
        public void Compile_SolidAirConflict_FailsAtomically()
        {
            var fixture = BuildFixture(shellConflict: true);
            AssertFailure(Compile(fixture, CreateIntent(fixture)), TerrainClusterRouteWitnessCompileErrorCode.StaticShellConflict);
        }

        [TestCase(2000)]
        [TestCase(5000)]
        public void Compile_RecoveryInclusiveBounds_Succeed(int recoveryMilliseconds)
        {
            var fixture = BuildFixture();
            var result = Compile(fixture, CreateIntent(fixture, recoveryMilliseconds: recoveryMilliseconds));
            Assert.That(result.IsSuccess, Is.True, ErrorText(result));
            Assert.That(result.RecoveryRoutes.Single().TotalEstimatedDurationMilliseconds, Is.EqualTo(recoveryMilliseconds));
        }

        [TestCase(1999, TerrainClusterRouteWitnessCompileErrorCode.RecoveryTooShort)]
        [TestCase(5001, TerrainClusterRouteWitnessCompileErrorCode.RecoveryTooLong)]
        public void Compile_RecoveryOutsideBounds_FailsAtomically(
            int recoveryMilliseconds,
            TerrainClusterRouteWitnessCompileErrorCode expected)
        {
            var fixture = BuildFixture();
            AssertFailure(Compile(fixture, CreateIntent(fixture, recoveryMilliseconds: recoveryMilliseconds)), expected);
        }

        [Test]
        public void Compile_MissingDurationEvidence_FailsAtomically()
        {
            var fixture = BuildFixture();
            var valid = CreateIntent(fixture);
            var intent = new TerrainClusterRouteWitnessIntent(valid.BaselineVariantId, valid.HighRoutes,
                valid.EdgeDurationEvidence.Skip(1));
            AssertFailure(Compile(fixture, intent), TerrainClusterRouteWitnessCompileErrorCode.InvalidDurationEvidence);
        }

        [Test]
        public void Compile_DuplicateAndUnknownDurationEvidence_AccumulatesAndFailsAtomically()
        {
            var fixture = BuildFixture();
            var valid = CreateIntent(fixture);
            var evidence = valid.EdgeDurationEvidence.Concat(new[]
            {
                valid.EdgeDurationEvidence[0],
                new TraversalEdgeDurationEvidence(new SpineVariantId("SPINE_ALTERNATE"), "EDGE_UNKNOWN", 400, "RULESET_ROUTE_V1"),
            });
            var result = Compile(fixture, new TerrainClusterRouteWitnessIntent(valid.BaselineVariantId, valid.HighRoutes, evidence));
            Assert.That(result.Errors.Count(value => value.Code == TerrainClusterRouteWitnessCompileErrorCode.InvalidDurationEvidence), Is.GreaterThanOrEqualTo(2));
            AssertAtomicFailure(result);
        }

        [Test]
        public void Compile_InvalidHighPointBenefitsAndFailure_AccumulatesAndFailsAtomically()
        {
            var fixture = BuildFixture();
            var valid = CreateIntent(fixture);
            var broken = new TerrainClusterHighRouteDefinition(
                "HIGH_ROUTE_ONE", new SpineVariantId("SPINE_ALTERNATE"), "NODE_BUILD_UP",
                new[] { "EDGE_HIGH_01", "EDGE_HIGH_02", "EDGE_HIGH_03" }, "NODE_CORE",
                "NODE_NOT_ON_PATH", new[] { "BENEFIT_ONE", "bad" }, new[] { "NODE_ENTRY" });
            var result = Compile(fixture, new TerrainClusterRouteWitnessIntent(
                valid.BaselineVariantId, new[] { broken }, valid.EdgeDurationEvidence));
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(TerrainClusterRouteWitnessCompileErrorCode.InvalidHighPoint));
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(TerrainClusterRouteWitnessCompileErrorCode.InsufficientHighRouteBenefits));
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(TerrainClusterRouteWitnessCompileErrorCode.InvalidFailureNode));
            AssertAtomicFailure(result);
        }

        [Test]
        public void Compile_HighRouteSameAsBaselineSubpath_IsRejected()
        {
            var fixture = BuildFixture();
            var valid = CreateIntent(fixture);
            var same = new TerrainClusterHighRouteDefinition(
                "HIGH_ROUTE_SAME", new SpineVariantId("SPINE_ALTERNATE"), "NODE_BUILD_UP",
                new[] { "EDGE_BASE_A1", "EDGE_BASE_A2" }, "NODE_CORE", "NODE_STEP_A",
                new[] { "BENEFIT_ONE", "BENEFIT_TWO" }, new[] { "NODE_STEP_A" });
            var intent = new TerrainClusterRouteWitnessIntent(valid.BaselineVariantId, new[] { same }, valid.EdgeDurationEvidence);
            AssertFailure(Compile(fixture, intent), TerrainClusterRouteWitnessCompileErrorCode.HighRouteNotDistinct);
        }

        [Test]
        public void Intent_DefensivelyCopiesMutableInputs()
        {
            var fixture = BuildFixture();
            var durations = CreateDurations(fixture, 2000).ToList();
            var edges = new List<string> { "EDGE_HIGH_01", "EDGE_HIGH_02", "EDGE_HIGH_03" };
            var benefits = new List<string> { "BENEFIT_HEIGHT", "BENEFIT_ACCESS" };
            var failures = new List<string> { "NODE_HIGH" };
            var definition = new TerrainClusterHighRouteDefinition(
                "HIGH_ROUTE_ONE", new SpineVariantId("SPINE_ALTERNATE"), "NODE_BUILD_UP",
                edges, "NODE_CORE", "NODE_HIGH", benefits, failures);
            var definitions = new List<TerrainClusterHighRouteDefinition> { definition };
            var intent = new TerrainClusterRouteWitnessIntent(new SpineVariantId("SPINE_BASELINE"), definitions, durations);
            edges.Clear(); benefits.Clear(); failures.Clear(); definitions.Clear(); durations.Clear();

            Assert.That(intent.HighRoutes, Has.Count.EqualTo(1));
            Assert.That(intent.HighRoutes[0].OrderedEdgeIds, Has.Count.EqualTo(3));
            Assert.That(intent.HighRoutes[0].BenefitIds, Has.Count.EqualTo(2));
            Assert.That(intent.HighRoutes[0].FailureNodeIds, Has.Count.EqualTo(1));
            Assert.That(intent.EdgeDurationEvidence, Is.Not.Empty);
            Assert.That(Compile(fixture, intent).IsSuccess, Is.True);
        }

        [Test]
        public void Compile_ArtifactDigestMismatch_FailsAtomically()
        {
            var fixture = BuildFixture();
            var request = new TerrainClusterRouteWitnessCompileRequest(
                fixture.Canvas, fixture.Canvas.CanonicalDigest,
                fixture.RoleSocket, fixture.RoleSocket.CanonicalDigest,
                fixture.Traversal, "wrong", CreateIntent(fixture));
            AssertFailure(TerrainClusterRouteWitnessCompiler.Compile(request),
                TerrainClusterRouteWitnessCompileErrorCode.ArtifactDigestMismatch);
        }

        [Test]
        public void RuntimeSources_ContainNoForbiddenPhysicsPatternStarterOrSectorSymbols()
        {
            var root = Path.Combine("Assets", "_Game", "Map", "Runtime", "WorldGeneration", "TerrainClusters");
            var source = string.Join("\n", new[]
            {
                "TerrainClusterStaticShell.cs", "TerrainClusterRouteWitness.cs", "TerrainClusterRouteWitnessCompiler.cs",
            }.Select(value => File.ReadAllText(Path.Combine(root, value))));
            var forbidden = new[]
            {
                "UnityEditor", "StageMapGenerator", "GridWorld", "RoomTemplate", "RoomGridTransform",
                "TileMutationService", "SectorRecipeResolver", "System.Random", "UnityEngine.Random",
                "Time.deltaTime", "MicroPatternPlanner", "MicroPatternOrderedRenderer", "WorldGenerationRoot",
            };
            foreach (var symbol in forbidden) Assert.That(source, Does.Not.Contain(symbol), symbol);
        }

        private static Fixture BuildFixture(bool reverseInput = false, bool shellConflict = false)
        {
            var contract = CreateContract(reverseInput, shellConflict);
            var validation = TerrainClusterContractValidator.Validate(contract);
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors.Select(value => value.ToString())));
            var canvasResult = TerrainClusterFootprintCompiler.Compile(
                new TerrainClusterFootprintCompileRequest(contract, ClusterFootprintTransform.R0));
            Assert.That(canvasResult.IsSuccess, Is.True, string.Join("\n", canvasResult.Errors.Select(value => value.ToString())));
            var canvas = canvasResult.LocalCanvas;
            var roleResult = TerrainClusterRoleSocketCompiler.Compile(
                new TerrainClusterRoleSocketCompileRequest(contract, validation.CanonicalDigest, canvas,
                    canvas.CanonicalDigest, SocketEvidence()));
            Assert.That(roleResult.IsSuccess, Is.True, string.Join("\n", roleResult.Errors.Select(value => value.ToString())));
            var traversalResult = TerrainClusterTraversalCompiler.Compile(
                new TerrainClusterTraversalCompileRequest(contract, validation.CanonicalDigest, canvas,
                    canvas.CanonicalDigest, roleResult.Contract, roleResult.CanonicalDigest));
            Assert.That(traversalResult.IsSuccess, Is.True, string.Join("\n", traversalResult.Errors.Select(value => value.ToString())));
            return new Fixture(canvas, roleResult.Contract, traversalResult.Compilation);
        }

        private static TerrainClusterContract CreateContract(bool reverseInput, bool shellConflict)
        {
            var roles = new[]
            {
                new ClusterRoleAnchor("ANCHOR_ENTRY", ClusterRoleKind.Entry, new LocalTileCoord(0, 1), "NODE_ENTRY"),
                new ClusterRoleAnchor("ANCHOR_BUILD_UP", ClusterRoleKind.BuildUp, new LocalTileCoord(5, 1), "NODE_BUILD_UP"),
                new ClusterRoleAnchor("ANCHOR_CORE", ClusterRoleKind.Core, new LocalTileCoord(12, 1), "NODE_CORE"),
                new ClusterRoleAnchor("ANCHOR_RECOVERY", ClusterRoleKind.Recovery, new LocalTileCoord(25, 1), "NODE_RECOVERY"),
                new ClusterRoleAnchor("ANCHOR_REWARD", ClusterRoleKind.Reward, new LocalTileCoord(30, 1), "NODE_REWARD"),
                new ClusterRoleAnchor("ANCHOR_EXIT", ClusterRoleKind.Exit, new LocalTileCoord(35, 1), "NODE_EXIT"),
            };
            var commonNodes = roles.Select(value => new TraversalNode(value.TraversalNodeId, value.Tile,
                value.Role != ClusterRoleKind.Reward, value.AnchorId)).Concat(new[]
            {
                new TraversalNode("NODE_STEP_A", new LocalTileCoord(8, 1), true, string.Empty),
                new TraversalNode("NODE_STEP_B", new LocalTileCoord(7, 1), false, string.Empty),
            }).ToArray();
            var alternateNodes = commonNodes.Concat(new[]
            {
                new TraversalNode("NODE_HIGH", new LocalTileCoord(8, 3), false, string.Empty),
                new TraversalNode("NODE_HIGH_END", new LocalTileCoord(10, 3), false, string.Empty),
            }).ToArray();
            var commonById = commonNodes.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
            var alternateById = alternateNodes.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
            var baseEdges = new[]
            {
                CreateEdge("EDGE_01_ENTRY", commonById["NODE_ENTRY"], commonById["NODE_BUILD_UP"], true),
                CreateEdge("EDGE_BASE_A1", commonById["NODE_BUILD_UP"], commonById["NODE_STEP_A"], true,
                    shellConflict ? new LocalTileCoord(7, 1) : (LocalTileCoord?)null),
                CreateEdge("EDGE_BASE_A2", commonById["NODE_STEP_A"], commonById["NODE_CORE"], true),
                CreateEdge("EDGE_BASE_B1", commonById["NODE_BUILD_UP"], commonById["NODE_STEP_B"], false),
                CreateEdge("EDGE_BASE_B2", commonById["NODE_STEP_B"], commonById["NODE_CORE"], false),
                CreateEdge("EDGE_04_CORE", commonById["NODE_CORE"], commonById["NODE_RECOVERY"], true),
                CreateEdge("EDGE_05_RECOVERY", commonById["NODE_RECOVERY"], commonById["NODE_EXIT"], true),
            };
            var alternateEdges = baseEdges.Select(edge => CopyEdge(edge, alternateById)).Concat(new[]
            {
                CreateEdge("EDGE_HIGH_01", alternateById["NODE_BUILD_UP"], alternateById["NODE_HIGH"], false),
                CreateEdge("EDGE_HIGH_02", alternateById["NODE_HIGH"], alternateById["NODE_HIGH_END"], false),
                CreateEdge("EDGE_HIGH_03", alternateById["NODE_HIGH_END"], alternateById["NODE_CORE"], false),
                CreateEdge("EDGE_RECOVER", alternateById["NODE_HIGH"], alternateById["NODE_RECOVERY"], false),
            }).ToArray();
            var variants = new[]
            {
                new SpineVariant(new SpineVariantId("SPINE_BASELINE"), true, TraversalGraphKind.Traversal,
                    reverseInput ? commonNodes.Reverse() : commonNodes, reverseInput ? baseEdges.Reverse() : baseEdges),
                new SpineVariant(new SpineVariantId("SPINE_ALTERNATE"), false, TraversalGraphKind.Traversal,
                    reverseInput ? alternateNodes.Reverse() : alternateNodes, reverseInput ? alternateEdges.Reverse() : alternateEdges),
            };
            var ports = new[]
            {
                new ClusterPort("PORT_ENTRY", ClusterPortKind.Entry, true, "ANCHOR_ENTRY",
                    new LocalTileCoord(0, 1), ClusterPortSide.L, new[] { 0, 1, 2, 3, 4 }),
                new ClusterPort("PORT_EXIT", ClusterPortKind.Exit, true, "ANCHOR_EXIT",
                    new LocalTileCoord(35, 1), ClusterPortSide.R, new[] { 1, 2, 3, 4 }),
            };
            return new TerrainClusterContract(
                new TerrainClusterId("TC_ROUTE_WITNESS"),
                new ClusterFootprint(new[]
                {
                    new ClusterChunkCoord(0, 0), new ClusterChunkCoord(1, 0),
                    new ClusterChunkCoord(2, 0), new ClusterChunkCoord(0, 1),
                }),
                reverseInput ? roles.Reverse() : roles,
                reverseInput ? ports.Reverse() : ports,
                new TerrainClusterTraversalContract(reverseInput ? variants.Reverse() : variants),
                reverseInput ? "표시문 역순" : "display text");
        }

        private static TraversalEdge CreateEdge(
            string id, TraversalNode from, TraversalNode to, bool mandatory, LocalTileCoord? floorOverride = null)
        {
            var envelope = new TraversalEnvelope(
                new[] { from.Tile, to.Tile },
                new[] { floorOverride ?? new LocalTileCoord(from.Tile.X, 0) },
                new[] { new LocalTileCoord(from.Tile.X, 5) },
                Array.Empty<LocalTileCoord>(), Array.Empty<LocalTileCoord>(),
                new[] { to.Tile }, new[] { to.Tile });
            return new TraversalEdge(id, from.NodeId, to.NodeId, TraversalMovementKind.Walk,
                from.Tile, to.Tile, 1, 2, to.Tile, to.Tile, mandatory, envelope);
        }

        private static TraversalEdge CopyEdge(TraversalEdge edge, IDictionary<string, TraversalNode> nodes)
        {
            return CreateEdge(edge.EdgeId, nodes[edge.FromNodeId], nodes[edge.ToNodeId], edge.IsMandatory,
                edge.Envelope.Floor[0]);
        }

        private static TerrainClusterRouteWitnessIntent CreateIntent(
            Fixture fixture,
            bool reverseInput = false,
            int recoveryMilliseconds = 2000,
            SpineVariantId? baselineId = null,
            string durationDeltaEdgeId = null)
        {
            var high = new TerrainClusterHighRouteDefinition(
                "HIGH_ROUTE_ONE", new SpineVariantId("SPINE_ALTERNATE"), "NODE_BUILD_UP",
                reverseInput ? new[] { "EDGE_HIGH_01", "EDGE_HIGH_02", "EDGE_HIGH_03" } :
                    new[] { "EDGE_HIGH_01", "EDGE_HIGH_02", "EDGE_HIGH_03" },
                "NODE_CORE", "NODE_HIGH",
                reverseInput ? new[] { "BENEFIT_REWARD_ACCESS", "BENEFIT_HEIGHT_ADVANTAGE" } :
                    new[] { "BENEFIT_HEIGHT_ADVANTAGE", "BENEFIT_REWARD_ACCESS" },
                new[] { "NODE_HIGH" });
            var durations = CreateDurations(fixture, recoveryMilliseconds, durationDeltaEdgeId);
            if (reverseInput) durations = durations.Reverse().ToArray();
            return new TerrainClusterRouteWitnessIntent(
                baselineId ?? new SpineVariantId("SPINE_BASELINE"), new[] { high }, durations);
        }

        private static TraversalEdgeDurationEvidence[] CreateDurations(
            Fixture fixture, int recoveryMilliseconds, string durationDeltaEdgeId = null)
        {
            return fixture.Traversal.Edges.Select(edge => new TraversalEdgeDurationEvidence(
                edge.VariantId, edge.EdgeId,
                edge.EdgeId == "EDGE_RECOVER" ? recoveryMilliseconds :
                    3000 + (edge.EdgeId == durationDeltaEdgeId ? 1 : 0),
                "RULESET_ROUTE_V1")).ToArray();
        }

        private static TerrainClusterRouteWitnessCompileResult Compile(
            Fixture fixture, TerrainClusterRouteWitnessIntent intent)
        {
            return TerrainClusterRouteWitnessCompiler.Compile(new TerrainClusterRouteWitnessCompileRequest(
                fixture.Canvas, fixture.Canvas.CanonicalDigest,
                fixture.RoleSocket, fixture.RoleSocket.CanonicalDigest,
                fixture.Traversal, fixture.Traversal.CanonicalDigest, intent));
        }

        private static ClusterSectorSocketEvidence[] SocketEvidence()
        {
            return new[]
            {
                new ClusterSectorSocketEvidence("SR_EXIT", "SOCKET_EXIT", ClusterPortSide.R, 3, true, ClusterPortKind.Exit),
                new ClusterSectorSocketEvidence("SR_ENTRY", "SOCKET_ENTRY", ClusterPortSide.L, 2, true, ClusterPortKind.Entry),
            };
        }

        private static void AssertFailure(
            TerrainClusterRouteWitnessCompileResult result,
            TerrainClusterRouteWitnessCompileErrorCode expected)
        {
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(expected), ErrorText(result));
            AssertAtomicFailure(result);
        }

        private static void AssertAtomicFailure(TerrainClusterRouteWitnessCompileResult result)
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Report, Is.Null);
            Assert.That(result.StaticShell, Is.Null);
            Assert.That(result.BaselineRoute, Is.Null);
            Assert.That(result.HighRoutes, Is.Empty);
            Assert.That(result.RecoveryRoutes, Is.Empty);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors, Is.Ordered);
            Assert.That(result.Errors.Distinct().Count(), Is.EqualTo(result.Errors.Count));
        }

        private static string ErrorText(TerrainClusterRouteWitnessCompileResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }

        private sealed class Fixture
        {
            public Fixture(TerrainClusterLocalCanvas canvas, TerrainClusterRoleSocketContract roleSocket,
                TerrainClusterTraversalCompilation traversal)
            {
                Canvas = canvas; RoleSocket = roleSocket; Traversal = traversal;
            }
            public TerrainClusterLocalCanvas Canvas { get; }
            public TerrainClusterRoleSocketContract RoleSocket { get; }
            public TerrainClusterTraversalCompilation Traversal { get; }
        }
    }

    internal static class TerrainClusterRouteWitnessTestExtensions
    {
        public static string[] OrderedEdgeIds(this TerrainClusterBaselineRouteWitness witness)
        {
            return witness.OrderedEdges.Select(value => value.EdgeId).ToArray();
        }

        public static string[] OrderedEdgeIds(this TerrainClusterHighRouteWitness witness)
        {
            return witness.OrderedEdges.Select(value => value.EdgeId).ToArray();
        }
    }
}
