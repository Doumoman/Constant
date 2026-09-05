using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.Validation;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Validation
{
    [TestFixture]
    [Category("MAP19_02")]
    public sealed class GeneratedTileMovementGraphBuilderTests
    {
        [Test]
        public void TileMovementGraphCreatesStandClimbBounceAndSocketNodes()
        {
            var graph = Build().Graph;
            Assert.That(graph.Nodes.Select(value => value.Kind).Distinct(), Is.EquivalentTo(
                Enum.GetValues(typeof(GeneratedTileMovementNodeKind))));
            Assert.That(graph.Nodes.Count(value => value.Kind == GeneratedTileMovementNodeKind.Stand),
                Is.EqualTo(1534));
            Assert.That(graph.Nodes.Count(value => value.Kind == GeneratedTileMovementNodeKind.Climb),
                Is.EqualTo(16));
            Assert.That(graph.Nodes.Count(value => value.Kind == GeneratedTileMovementNodeKind.Bounce),
                Is.EqualTo(16));
            Assert.That(graph.Nodes.Count(value => value.Kind == GeneratedTileMovementNodeKind.IntersectorSocket),
                Is.EqualTo(48));
            Assert.That(graph.Nodes.Select(value => value.NodeId), Is.All.Match("^[0-9a-f]{64}$"));
            Assert.That(graph.Nodes, Is.All.Matches<GeneratedTileMovementNode>(value =>
                value.Sector.Value.Length != 0 && value.SourceOwner.Length != 0 &&
                BakingCanonicalDigest.IsLowerHexSha256(value.SourceDigest) &&
                BakingCanonicalDigest.IsLowerHexSha256(value.ClearanceProtectionProof)));
        }

        [Test]
        public void TileMovementGraphBuildsWalkJumpDropClimbSlideBounceEdgesFromLockedProfile()
        {
            var result = Build();
            var graph = result.Graph;
            Assert.That(graph.Edges.Select(value => value.MovementKind).Distinct(), Is.EquivalentTo(
                Enum.GetValues(typeof(TraversalMovementKind))));
            var matrix = Lock().Surface.RuleRegistry.MovementEnvelopeMatrix;
            foreach (var edge in graph.Edges)
            {
                var expected = matrix.Single(value => value.MovementKind == edge.MovementKind);
                Assert.That(edge.RequiredEnvelopeKinds, Is.EqualTo(expected.RequiredEnvelopeKinds));
                Assert.That(edge.ProvenEnvelopeKinds, Is.EqualTo(expected.RequiredEnvelopeKinds));
                Assert.That(edge.RuleIds, Is.Not.Empty);
                Assert.That(edge.Cost, Is.GreaterThanOrEqualTo(0m));
            }
        }

        [Test]
        public void TileMovementGraphCreatesIntersectorSocketLinksWithoutCompletionSearch()
        {
            var graph = Build().Graph;
            var nodeIds = graph.Nodes.Select(value => value.NodeId).ToHashSet(StringComparer.Ordinal);
            Assert.That(graph.SocketLinks, Has.Count.EqualTo(24));
            Assert.That(graph.SocketLinks, Is.All.Matches<GeneratedIntersectorSocketLink>(value =>
                nodeIds.Contains(value.FromSocketNodeId) && nodeIds.Contains(value.ToSocketNodeId) &&
                BakingCanonicalDigest.IsLowerHexSha256(value.ProvenanceDigest)));
            Assert.That(graph.SocketLinks.SelectMany(value => new[]
                { value.FromDirection, value.ToDirection }).Distinct(), Is.EquivalentTo(
                Enum.GetValues(typeof(GeneratedIntersectorSocketDirection))));
        }

        [Test]
        public void TileMovementGraphRejectsSolidBlockedClearanceMissingAndDanglingEdges()
        {
            var fixture = CreateFixture();
            var result = GeneratedTileMovementGraphBuilder.Build(fixture.Request);
            AssertSuccess(result);
            Assert.That(result.Graph.Stats.SolidBlockedNodeRejections, Is.EqualTo(2));
            Assert.That(result.Graph.Stats.ClearanceMissingEdgeRejections, Is.GreaterThan(0));
            Assert.That(result.Graph.Stats.ProtectedEnvelopeEdgeRejections, Is.GreaterThan(0));

            var edge = result.Graph.Edges[0];
            var dangling = new GeneratedTileMovementEdge(edge.FromNodeId, new string('f', 64),
                edge.MovementKind, edge.RequiredEnvelopeKinds, edge.ProvenEnvelopeKinds,
                edge.RuleIds, edge.Cost, edge.FailurePolicy);
            var invalid = Copy(result.Graph, edges: result.Graph.Edges.Skip(1).Append(dangling));
            AssertFailure(GeneratedTileMovementGraphBuilder.ValidateCandidate(fixture.Request, invalid),
                "DANGLING_EDGE_REFERENCE");
        }

        [Test]
        public void TileMovementGraphUsesRuleRegistryThresholdsWithoutDuplicatingTraversalTuning()
        {
            var result = Build();
            var stats = result.Graph.Stats;
            Assert.That(stats.RuleRegistryThresholdLookups, Is.EqualTo(6));
            Assert.That(stats.HardcodedDuplicateTraversalThresholds, Is.Zero);
            Assert.That(stats.MatrixLocalCopyMismatches, Is.Zero);
            Assert.That(stats.UnsupportedMovementKinds, Is.Zero);
            Assert.That(stats.UnsupportedEnvelopeKinds, Is.Zero);
            var registry = Lock().Surface.RuleRegistry;
            foreach (var edge in result.Graph.Edges)
                Assert.That(registry.Rules.Any(rule => rule.TargetMovementKinds.Contains(edge.MovementKind) &&
                    rule.Threshold.MaximumValue == edge.Cost), Is.True, edge.EdgeId);
        }

        [Test]
        public void TileMovementGraphDigestsAreStableAcrossRepeatReverseCultureAndCellOrder()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = Build().Graph;
                var repeat = Build().Graph;
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var reverse = GeneratedTileMovementGraphBuilder.Build(CreateFixture(true).Request).Graph;
                var edgeOrder = Copy(first, edges: first.Edges.Reverse());
                Assert.That(repeat.GraphDigest, Is.EqualTo(first.GraphDigest));
                Assert.That(reverse.GraphDigest, Is.EqualTo(first.GraphDigest));
                Assert.That(edgeOrder.GraphDigest, Is.EqualTo(first.GraphDigest));

                var node = first.Nodes[0];
                var mutatedNode = new GeneratedTileMovementNode(node.Kind, node.Sector, node.Slice,
                    node.Cell, node.SurfaceId + "_MUTATED", node.SourceOwner, node.SourceDigest,
                    node.ClearanceProtectionProof);
                var mutation = Copy(first, nodes: first.Nodes.Skip(1).Append(mutatedNode));
                Assert.That(mutation.NodeSetDigest, Is.Not.EqualTo(first.NodeSetDigest));
                Assert.That(mutation.GraphDigest, Is.Not.EqualTo(first.GraphDigest));
                Assert.That(mutation.Map19_03HandoffDigest, Is.Not.EqualTo(first.Map19_03HandoffDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void TileMovementGraphFailuresAreAtomicAndReportOwnerReasonExpectedActual()
        {
            var fixture = CreateFixture();
            var valid = GeneratedTileMovementGraphBuilder.Build(fixture.Request).Graph;
            var missingSource = GeneratedTileMovementGraphBuilder.Build(
                new GeneratedTileMovementGraphRequest(null, null, Lock()));
            AssertAtomicFailure(missingSource, "GeneratedTerrainSource");
            var missingLock = GeneratedTileMovementGraphBuilder.Build(
                new GeneratedTileMovementGraphRequest(fixture.Canvas, fixture.Slices, null));
            AssertAtomicFailure(missingLock, "MAP19_01");

            var digestMismatch = Copy(valid, declaredGraphDigest: new string('0', 64));
            AssertAtomicFailure(GeneratedTileMovementGraphBuilder.ValidateCandidate(
                fixture.Request, digestMismatch), "DigestIntegrity");
            var upstreamMismatch = new GeneratedTileMovementGraph(valid.Nodes, valid.Edges,
                valid.SocketLinks, valid.Stats, new string('0', 64), valid.RuleRegistryDigest,
                valid.MovementEnvelopeMatrixDigest, valid.Map19_02IncomingHandoffDigest,
                valid.SourceTerrainDigest);
            AssertAtomicFailure(GeneratedTileMovementGraphBuilder.ValidateCandidate(
                fixture.Request, upstreamMismatch), "DigestCompatibility");

            var sourceNode = valid.Nodes[0];
            var invalidNode = new GeneratedTileMovementNode(sourceNode.Kind, sourceNode.Sector,
                sourceNode.Slice, sourceNode.Cell, sourceNode.SurfaceId, sourceNode.SourceOwner,
                sourceNode.SourceDigest, sourceNode.ClearanceProtectionProof,
                declaredNodeId: "NOT_A_DIGEST");
            AssertAtomicFailure(GeneratedTileMovementGraphBuilder.ValidateCandidate(fixture.Request,
                Copy(valid, nodes: valid.Nodes.Skip(1).Append(invalidNode))), "GraphIntegrity");

            var missingNodeKind = Copy(valid, nodes: valid.Nodes.Where(value =>
                value.Kind != GeneratedTileMovementNodeKind.Bounce));
            AssertAtomicFailure(GeneratedTileMovementGraphBuilder.ValidateCandidate(
                fixture.Request, missingNodeKind), "GraphIntegrity");
            var missingEdgeKind = Copy(valid, edges: valid.Edges.Where(value =>
                value.MovementKind != TraversalMovementKind.Bounce));
            AssertAtomicFailure(GeneratedTileMovementGraphBuilder.ValidateCandidate(
                fixture.Request, missingEdgeKind), "GraphIntegrity");

            var firstEdge = valid.Edges[0];
            var unsupported = new GeneratedTileMovementEdge(firstEdge.FromNodeId, firstEdge.ToNodeId,
                (TraversalMovementKind)999,
                new[] { (CompiledTraversalEnvelopeSetKind)999 },
                new[] { (CompiledTraversalEnvelopeSetKind)999 }, firstEdge.RuleIds,
                firstEdge.Cost, firstEdge.FailurePolicy);
            AssertAtomicFailure(GeneratedTileMovementGraphBuilder.ValidateCandidate(fixture.Request,
                Copy(valid, edges: valid.Edges.Append(unsupported))), "MovementKindSupport");

            AssertAtomicFailure(GeneratedTileMovementGraphBuilder.ValidateCandidate(fixture.Request,
                Copy(valid, nodes: valid.Nodes.Append(valid.Nodes[0]),
                    edges: valid.Edges.Append(valid.Edges[0]))), "GraphIntegrity");

            var firstLink = valid.SocketLinks[0];
            var danglingLink = new GeneratedIntersectorSocketLink(new string('a', 64),
                firstLink.ToSocketNodeId, firstLink.FromDirection, firstLink.ToDirection,
                firstLink.ProvenanceDigest);
            AssertAtomicFailure(GeneratedTileMovementGraphBuilder.ValidateCandidate(fixture.Request,
                Copy(valid, socketLinks: valid.SocketLinks.Skip(1).Append(danglingLink))),
                "SocketIntegrity");

            var noProof = new GeneratedTileMovementEdge(firstEdge.FromNodeId, firstEdge.ToNodeId,
                firstEdge.MovementKind, firstEdge.RequiredEnvelopeKinds,
                Array.Empty<CompiledTraversalEnvelopeSetKind>(), firstEdge.RuleIds,
                firstEdge.Cost, firstEdge.FailurePolicy);
            AssertAtomicFailure(GeneratedTileMovementGraphBuilder.ValidateCandidate(fixture.Request,
                Copy(valid, edges: valid.Edges.Skip(1).Append(noProof))), "EnvelopeSupport");

            var hardcodedStats = new GeneratedTileMovementGraphStats(valid.Stats.SourceSectors,
                valid.Stats.SourceSlices, valid.Stats.SourceCells,
                valid.Stats.SolidBlockedNodeRejections,
                valid.Stats.ClearanceMissingEdgeRejections,
                valid.Stats.ProtectedEnvelopeEdgeRejections,
                valid.Stats.RuleRegistryThresholdLookups,
                hardcodedDuplicateTraversalThresholds: 1);
            var hardcodedThreshold = new GeneratedTileMovementGraph(valid.Nodes, valid.Edges,
                valid.SocketLinks, hardcodedStats, valid.TraversalProfileDigest,
                valid.RuleRegistryDigest, valid.MovementEnvelopeMatrixDigest,
                valid.Map19_02IncomingHandoffDigest, valid.SourceTerrainDigest);
            AssertAtomicFailure(GeneratedTileMovementGraphBuilder.ValidateCandidate(
                fixture.Request, hardcodedThreshold), "RuleRegistry");

            var forbidden = GeneratedTileMovementGraphBuilder.Build(new GeneratedTileMovementGraphRequest(
                fixture.Canvas, fixture.Slices, Lock(), new GeneratedTileMovementBoundaryAudit(
                    bfsRuns: 1, completionSearches: 1, seedBatchRuns: 1,
                    physics2DQueries: 1, playerTuningChanges: 1)));
            AssertAtomicFailure(forbidden, "ForbiddenWork");
            Assert.That(forbidden.Failures, Is.All.Matches<GeneratedTileMovementGraphFailure>(failure =>
                failure.Owner.Length != 0 && failure.Reason.Length != 0 &&
                failure.OffendingKey.Length != 0 && failure.ExpectedValue.Length != 0 &&
                failure.ActualValue.Length != 0));
        }

        [Test]
        public void TileMovementGraphDoesNotRunBfsSeedBatchPhysicsOrMutateScenes()
        {
            var fixture = CreateFixture();
            var result = GeneratedTileMovementGraphBuilder.Build(fixture.Request);
            AssertSuccess(result);
            Assert.That(fixture.Request.BoundaryAudit.IsZero, Is.True);
            Assert.That(fixture.Request.BoundaryAudit.BfsRuns, Is.Zero);
            Assert.That(fixture.Request.BoundaryAudit.CompletionSearches, Is.Zero);
            Assert.That(fixture.Request.BoundaryAudit.SeedBatchRuns, Is.Zero);
            Assert.That(fixture.Request.BoundaryAudit.Physics2DQueries, Is.Zero);
            Assert.That(fixture.Request.BoundaryAudit.RuntimeObjectSpawns, Is.Zero);
            Assert.That(fixture.Request.BoundaryAudit.ScenePrefabTilemapMutations, Is.Zero);
        }

        [Test]
        public void TileMovementGraphPublishesMap19_03HandoffSurface()
        {
            var graph = Build().Graph;
            foreach (var digest in new[]
                     {
                         graph.TraversalProfileDigest, graph.RuleRegistryDigest,
                         graph.MovementEnvelopeMatrixDigest, graph.Map19_02IncomingHandoffDigest,
                         graph.NodeSetDigest, graph.EdgeSetDigest, graph.SocketLinkDigest,
                         graph.GraphDigest, graph.Map19_03HandoffDigest,
                     })
                Assert.That(BakingCanonicalDigest.IsLowerHexSha256(digest), Is.True, digest);
            Assert.That(graph.TraversalProfileDigest,
                Is.EqualTo(GeneratedTileMovementGraphBuilder.ExpectedTraversalProfileDigest));
            Assert.That(graph.RuleRegistryDigest,
                Is.EqualTo(GeneratedTileMovementGraphBuilder.ExpectedRuleRegistryDigest));
            Assert.That(graph.MovementEnvelopeMatrixDigest,
                Is.EqualTo(GeneratedTileMovementGraphBuilder.ExpectedMovementEnvelopeMatrixDigest));
            Assert.That(graph.Map19_02IncomingHandoffDigest,
                Is.EqualTo(GeneratedTileMovementGraphBuilder.ExpectedMap19_02IncomingHandoffDigest));
        }

        [Test]
        public void Map19HandoffKeepsMap19_03Locked()
        {
            var result = Build();
            Assert.That(result.Graph.Map19_03Started, Is.False);
            Assert.That(result.Map19_03HandoffDigest, Is.Not.Empty);
            Assert.That(result.Graph.Map19_03HandoffDigest, Is.EqualTo(result.Map19_03HandoffDigest));
        }

        private static GeneratedTileMovementGraphResult Build()
        {
            var fixture = CreateFixture();
            var result = GeneratedTileMovementGraphBuilder.Build(fixture.Request);
            AssertSuccess(result);
            return result;
        }

        private static Fixture CreateFixture(bool reverseInput = false)
        {
            var cells = CreateCells().ToList();
            if (reverseInput) cells.Reverse();
            var stamp = new SectorCanvasValidationStamp(SectorCanvasValidationState.Validated,
                V2PassCatalog.StableDigest, GenerationLayerCatalog.StableDigest,
                BakingCanonicalDigest.ComputeSourceArtifactSet(cells),
                BakingCanonicalDigest.ComputeResolvedCells(cells), new string('e', 64));
            var canvas = new SectorCanvasContract(new SectorCanvasId("CANVAS_MAP19_02_FIXTURE"),
                WorldGenConstants.SectorWidthTiles, WorldGenConstants.SectorHeightTiles, cells, stamp);
            var canvasValidation = SectorCanvasContractValidator.Validate(canvas);
            Assert.That(canvasValidation.IsValid, Is.True, Join(canvasValidation.Errors));

            var slices = new List<GeneratedMicroChunkSlice>();
            for (var sliceY = 0; sliceY < WorldGenConstants.MicroChunkRowsPerSector; sliceY++)
            for (var sliceX = 0; sliceX < WorldGenConstants.MicroChunkColumnsPerSector; sliceX++)
            {
                var sliceCells = new List<GeneratedSliceCell>();
                for (var localY = 0; localY < WorldGenConstants.MicroChunkHeightTiles; localY++)
                for (var localX = 0; localX < WorldGenConstants.MicroChunkWidthTiles; localX++)
                {
                    var globalX = sliceX * WorldGenConstants.MicroChunkWidthTiles + localX;
                    var globalY = sliceY * WorldGenConstants.MicroChunkHeightTiles + localY;
                    var source = canvas.Cells[globalY * WorldGenConstants.SectorWidthTiles + globalX];
                    sliceCells.Add(new GeneratedSliceCell(new LocalTileCoord(localX, localY),
                        source.Layers, source.Provenance));
                }
                if (reverseInput) sliceCells.Reverse();
                slices.Add(new GeneratedMicroChunkSlice(new GeneratedSliceCoord(sliceX, sliceY),
                    sliceCells, new GeneratedSliceProvenance(canvas.Id,
                        canvasValidation.CanonicalDigest, canvas.ValidationStamp.StableDigest,
                        GeneratedSliceTransform.None)));
            }
            if (reverseInput) slices.Reverse();
            var sliceSet = new GeneratedSliceSet(canvas.Id, slices,
                GeneratedSliceBoundaryRole.GeneratedOutput);
            var sliceValidation = GeneratedSliceContractValidator.Validate(sliceSet, canvas);
            Assert.That(sliceValidation.IsValid, Is.True, Join(sliceValidation.Errors));
            return new Fixture(canvas, sliceSet, new GeneratedTileMovementGraphRequest(
                canvas, sliceSet, Lock(), new GeneratedTileMovementBoundaryAudit()));
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
                var blockedSolid = x == 5 && y == 3;
                var hazardous = x == 7 && y == 3;
                var protectedEnvelope = x == 5 && y == 1;
                var clearanceBlocked = x == 4 && y == 1;
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
                var marker = clearanceBlocked
                    ? ResolvedLayerValue.FromId("CLEARANCE_BLOCKED")
                    : SocketMarker(localX, localY, sliceX, sliceY);
                yield return new SectorCanvasCell(new LocalTileCoord(x, y),
                    new SectorCanvasLayerSnapshot(
                        ResolvedLayerValue.FromId(blockedSolid ? "SOLID_BLOCKED" : "SOLID_STONE"),
                        background, ResolvedLayerValue.FromId("SURFACE_STONE"), affordance,
                        ResolvedLayerValue.FromId("MAT_STONE"),
                        hazardous ? ResolvedLayerValue.FromId("HAZARD_BLOCKED") : ResolvedLayerValue.Empty,
                        marker, ResolvedLayerValue.FromId("TC_MAP19_02_OWNER")),
                    new SectorCanvasProvenance(sources));
            }
        }

        private static ResolvedLayerValue SocketMarker(int localX, int localY, int sliceX, int sliceY)
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

        private static GeneratedTraversalProfileRuleLockResult Lock()
        {
            var result = GeneratedTraversalProfileRuleLock.Lock(
                GeneratedTraversalProfileRuleLock.CreateAcceptedRequest());
            Assert.That(result.Success, Is.True, string.Join("\n", result.Failures));
            return result;
        }

        private static GeneratedTileMovementGraph Copy(
            GeneratedTileMovementGraph graph,
            IEnumerable<GeneratedTileMovementNode> nodes = null,
            IEnumerable<GeneratedTileMovementEdge> edges = null,
            IEnumerable<GeneratedIntersectorSocketLink> socketLinks = null,
            string declaredGraphDigest = null) => new GeneratedTileMovementGraph(
            nodes ?? graph.Nodes, edges ?? graph.Edges, socketLinks ?? graph.SocketLinks,
            graph.Stats, graph.TraversalProfileDigest, graph.RuleRegistryDigest,
            graph.MovementEnvelopeMatrixDigest, graph.Map19_02IncomingHandoffDigest,
            graph.SourceTerrainDigest, declaredGraphDigest: declaredGraphDigest);

        private static void AssertSuccess(GeneratedTileMovementGraphResult result)
        {
            Assert.That(result.Success, Is.True, string.Join("\n", result.Failures));
            Assert.That(result.Graph, Is.Not.Null);
            Assert.That(result.Map19_03HandoffDigest, Is.Not.Empty);
        }

        private static void AssertFailure(GeneratedTileMovementGraphResult result, string reason)
        {
            AssertAtomicFailure(result, null);
            Assert.That(result.Failures.Select(value => value.Reason), Does.Contain(reason),
                string.Join("\n", result.Failures));
        }

        private static void AssertAtomicFailure(GeneratedTileMovementGraphResult result, string owner)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Graph, Is.Null);
            Assert.That(result.Map19_03HandoffDigest, Is.Empty);
            Assert.That(result.Failures, Is.Not.Empty);
            if (owner != null)
                Assert.That(result.Failures.Select(value => value.Owner), Does.Contain(owner),
                    string.Join("\n", result.Failures));
        }

        private static string Join<T>(IEnumerable<T> values) => string.Join("\n", values);

        private sealed class Fixture
        {
            public Fixture(SectorCanvasContract canvas, GeneratedSliceSet slices,
                GeneratedTileMovementGraphRequest request)
            {
                Canvas = canvas;
                Slices = slices;
                Request = request;
            }

            public SectorCanvasContract Canvas { get; }
            public GeneratedSliceSet Slices { get; }
            public GeneratedTileMovementGraphRequest Request { get; }
        }
    }
}
