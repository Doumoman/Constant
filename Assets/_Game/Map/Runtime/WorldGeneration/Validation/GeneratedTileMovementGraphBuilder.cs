using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Validation
{
    public static class GeneratedTileMovementGraphBuilder
    {
        public const string ExpectedTraversalProfileDigest =
            "12415531bfa37bc8db427f672fefe47c92fee65bf61fccd94887af09669c2d68";
        public const string ExpectedRuleRegistryDigest =
            "556f6885caa0baeb92073fa3410d6a8df36a2000626f1737de5a1bcc65f3e747";
        public const string ExpectedMovementEnvelopeMatrixDigest =
            "f65ab5389cdbedf57b8d0084c0abc1386d3f30700cdf220fbeaced825a5e521f";
        public const string ExpectedMap19_02IncomingHandoffDigest =
            "09f54b43e12dd6cdcf102d84f5d70b5b78ba3ff29954d721a94fbe5bf5197963";

        private const string ClimbAffordance = "TRAVERSAL_CLIMB";
        private const string BounceAffordance = "TRAVERSAL_BOUNCE";
        private const string BlockedSolid = "SOLID_BLOCKED";
        private const string ClearanceBlockedMarker = "CLEARANCE_BLOCKED";

        public static GeneratedTileMovementGraphResult Build(GeneratedTileMovementGraphRequest request)
        {
            var failures = new List<GeneratedTileMovementGraphFailure>();
            if (!ValidateRequest(request, failures))
                return Failure(failures);

            var surface = request.ProfileRuleLock.Surface;
            var registry = surface.RuleRegistry;
            var canvasValidation = SectorCanvasContractValidator.Validate(request.Canvas);
            var sliceValidation = GeneratedSliceContractValidator.Validate(request.Slices, request.Canvas);
            if (!canvasValidation.IsValid)
                Add(failures, "GeneratedTerrainSource", "INVALID_SECTOR_CANVAS", "canvas",
                    "VALIDATED_CANVAS", JoinErrors(canvasValidation.Errors));
            if (!sliceValidation.IsValid)
                Add(failures, "GeneratedTerrainSource", "INVALID_GENERATED_SLICE_SET", "slices",
                    "VALIDATED_GENERATED_OUTPUT", JoinErrors(sliceValidation.Errors));
            if (failures.Count != 0)
                return Failure(failures);

            var sourceTerrainDigest = ComputeSourceTerrainDigest(
                request.Canvas, request.Slices, canvasValidation.CanonicalDigest);
            var nodes = new List<GeneratedTileMovementNode>();
            var solidBlockedRejections = 0;
            var cellByGlobal = new Dictionary<CellKey, GeneratedSliceCell>();
            var sliceByGlobal = new Dictionary<CellKey, GeneratedSliceCoord>();
            foreach (var slice in request.Slices.Slices)
            foreach (var cell in slice.Cells)
            {
                var global = Global(slice.Coordinate, cell.LocalCoordinate);
                var key = new CellKey(global.X, global.Y);
                cellByGlobal[key] = cell;
                sliceByGlobal[key] = slice.Coordinate;
                if (IsSolidBlocked(cell))
                {
                    solidBlockedRejections++;
                    continue;
                }

                var owner = SourceOwner(cell);
                var sourceDigest = SourceDigest(request.Canvas.Id, slice.Coordinate, global, cell);
                var proof = ClearanceProof(cell);
                nodes.Add(Node(GeneratedTileMovementNodeKind.Stand, request.Canvas.Id,
                    slice.Coordinate, global, cell.Layers.Solid.StableId, owner, sourceDigest, proof));
                if (string.Equals(cell.Layers.Affordance.StableId, ClimbAffordance,
                    StringComparison.Ordinal))
                    nodes.Add(Node(GeneratedTileMovementNodeKind.Climb, request.Canvas.Id,
                        slice.Coordinate, global, cell.Layers.Affordance.StableId, owner, sourceDigest, proof));
                if (string.Equals(cell.Layers.Affordance.StableId, BounceAffordance,
                    StringComparison.Ordinal))
                    nodes.Add(Node(GeneratedTileMovementNodeKind.Bounce, request.Canvas.Id,
                        slice.Coordinate, global, cell.Layers.Affordance.StableId, owner, sourceDigest, proof));
                if (TryDirection(cell.Layers.Marker.StableId, out _))
                    nodes.Add(Node(GeneratedTileMovementNodeKind.IntersectorSocket, request.Canvas.Id,
                        slice.Coordinate, global, cell.Layers.Marker.StableId, owner, sourceDigest, proof));
            }

            var nodeByCellAndKind = nodes.ToDictionary(
                value => new NodeKey(value.Cell.X, value.Cell.Y, value.Kind), value => value);
            var edges = new List<GeneratedTileMovementEdge>();
            var clearanceRejections = 0;
            var protectedRejections = 0;
            var ruleLookups = new Dictionary<TraversalMovementKind, GeneratedTraversalValidationRule>();
            foreach (TraversalMovementKind movement in Enum.GetValues(typeof(TraversalMovementKind)))
                ruleLookups[movement] = SelectCostRule(registry, movement);

            foreach (var node in nodes.Where(value => value.Kind == GeneratedTileMovementNodeKind.Stand))
            {
                TryAdd(node, GeneratedTileMovementNodeKind.Stand, 1, 0, TraversalMovementKind.Walk);
                TryAdd(node, GeneratedTileMovementNodeKind.Stand, 1, 0, TraversalMovementKind.Slide);
                TryAdd(node, GeneratedTileMovementNodeKind.Stand, 1, 1, TraversalMovementKind.Jump);
                TryAdd(node, GeneratedTileMovementNodeKind.Stand, 0, -1, TraversalMovementKind.Drop);
            }
            foreach (var node in nodes.Where(value => value.Kind == GeneratedTileMovementNodeKind.Climb))
                TryAdd(node, GeneratedTileMovementNodeKind.Stand, 0, 1, TraversalMovementKind.Climb);
            foreach (var node in nodes.Where(value => value.Kind == GeneratedTileMovementNodeKind.Bounce))
                TryAdd(node, GeneratedTileMovementNodeKind.Stand, 1, 1, TraversalMovementKind.Bounce);

            var socketLinks = BuildSocketLinks(nodes);
            var stats = new GeneratedTileMovementGraphStats(
                1, request.Slices.Slices.Count, cellByGlobal.Count, solidBlockedRejections,
                clearanceRejections, protectedRejections, ruleLookups.Count);
            var graph = new GeneratedTileMovementGraph(nodes, edges, socketLinks, stats,
                surface.TraversalProfileDigest, surface.RuleRegistryDigest,
                surface.MovementEnvelopeMatrixDigest, surface.Map19_02HandoffDigest,
                sourceTerrainDigest);
            var integrity = ValidateCandidate(request, graph);
            return integrity.Success ? integrity : Failure(integrity.Failures);

            void TryAdd(
                GeneratedTileMovementNode from,
                GeneratedTileMovementNodeKind targetKind,
                int deltaX,
                int deltaY,
                TraversalMovementKind movement)
            {
                var targetKey = new NodeKey(from.Cell.X + deltaX, from.Cell.Y + deltaY, targetKind);
                if (!nodeByCellAndKind.TryGetValue(targetKey, out var to))
                    return;
                if (!sliceByGlobal.TryGetValue(new CellKey(to.Cell.X, to.Cell.Y), out var targetSlice) ||
                    targetSlice != from.Slice)
                    return;
                var targetCell = cellByGlobal[new CellKey(to.Cell.X, to.Cell.Y)];
                if (IsClearanceBlocked(targetCell))
                {
                    clearanceRejections++;
                    return;
                }
                if (HasProtectedEnvelopeConflict(targetCell))
                {
                    protectedRejections++;
                    return;
                }
                var matrix = registry.MovementEnvelopeMatrix.Single(value => value.MovementKind == movement);
                var rule = ruleLookups[movement];
                var consumedRuleIds = registry.Rules
                    .Where(value => value.TargetMovementKinds.Contains(movement))
                    .Select(value => value.RuleId);
                edges.Add(new GeneratedTileMovementEdge(from.NodeId, to.NodeId, movement,
                    matrix.RequiredEnvelopeKinds, matrix.RequiredEnvelopeKinds, consumedRuleIds,
                    rule.Threshold.MaximumValue, rule.FailurePolicy));
            }
        }

        public static GeneratedTileMovementGraphResult ValidateCandidate(
            GeneratedTileMovementGraphRequest request,
            GeneratedTileMovementGraph graph)
        {
            var failures = new List<GeneratedTileMovementGraphFailure>();
            if (!ValidateRequest(request, failures))
                return Failure(failures);
            if (graph == null)
            {
                Add(failures, "GraphIntegrity", "MISSING_GRAPH", "graph", "NON_NULL", "NULL");
                return Failure(failures);
            }

            var surface = request.ProfileRuleLock.Surface;
            Digest(failures, "DigestCompatibility", "PROFILE_DIGEST_MISMATCH",
                graph.TraversalProfileDigest, surface.TraversalProfileDigest);
            Digest(failures, "DigestCompatibility", "RULE_REGISTRY_DIGEST_MISMATCH",
                graph.RuleRegistryDigest, surface.RuleRegistryDigest);
            Digest(failures, "DigestCompatibility", "MATRIX_DIGEST_MISMATCH",
                graph.MovementEnvelopeMatrixDigest, surface.MovementEnvelopeMatrixDigest);
            Digest(failures, "DigestCompatibility", "MAP19_02_HANDOFF_DIGEST_MISMATCH",
                graph.Map19_02IncomingHandoffDigest, surface.Map19_02HandoffDigest);
            var canvasValidation = SectorCanvasContractValidator.Validate(request.Canvas);
            if (canvasValidation.IsValid)
                Digest(failures, "GeneratedTerrainSource", "SOURCE_TERRAIN_DIGEST_MISMATCH",
                    graph.SourceTerrainDigest, ComputeSourceTerrainDigest(
                        request.Canvas, request.Slices, canvasValidation.CanonicalDigest));

            Duplicates(failures, graph.Nodes.Select(value => value.NodeId), "DUPLICATE_NODE_ID");
            Duplicates(failures, graph.Edges.Select(value => value.EdgeId), "DUPLICATE_EDGE_ID");
            Duplicates(failures, graph.SocketLinks.Select(value => value.LinkId), "DUPLICATE_SOCKET_LINK_ID");
            var nodeIds = new HashSet<string>(graph.Nodes.Select(value => value.NodeId), StringComparer.Ordinal);
            var socketIds = new HashSet<string>(graph.Nodes
                .Where(value => value.Kind == GeneratedTileMovementNodeKind.IntersectorSocket)
                .Select(value => value.NodeId), StringComparer.Ordinal);
            var sourceCells = request.Slices.Slices.SelectMany(slice => slice.Cells.Select(cell =>
                    new { Key = new CellKey(Global(slice.Coordinate, cell.LocalCoordinate).X,
                        Global(slice.Coordinate, cell.LocalCoordinate).Y), Slice = slice.Coordinate, Cell = cell }))
                .ToDictionary(value => value.Key, value => value);
            foreach (var node in graph.Nodes)
            {
                if (!Enum.IsDefined(typeof(GeneratedTileMovementNodeKind), node.Kind))
                    Add(failures, "GraphIntegrity", "UNSUPPORTED_NODE_KIND", node.NodeId,
                        "DEFINED_NODE_KIND", Number((int)node.Kind));
                if (!BakingCanonicalDigest.IsLowerHexSha256(node.NodeId))
                    Add(failures, "GraphIntegrity", "INVALID_NODE_ID", node.NodeId,
                        "LOWER_HEX_SHA256", node.NodeId);
                var expectedNodeId = new GeneratedTileMovementNode(node.Kind, node.Sector, node.Slice,
                    node.Cell, node.SurfaceId, node.SourceOwner, node.SourceDigest,
                    node.ClearanceProtectionProof).NodeId;
                if (!string.Equals(node.NodeId, expectedNodeId, StringComparison.Ordinal))
                    Add(failures, "GraphIntegrity", "NODE_ID_SEMANTIC_MISMATCH", node.NodeId,
                        expectedNodeId, node.NodeId);
                if (!BakingCanonicalDigest.IsLowerHexSha256(node.SourceDigest) ||
                    !BakingCanonicalDigest.IsLowerHexSha256(node.ClearanceProtectionProof))
                    Add(failures, "GeneratedTerrainSource", "INVALID_NODE_PROVENANCE_PROOF", node.NodeId,
                        "TWO_LOWER_HEX_SHA256_VALUES", node.SourceDigest + "/" + node.ClearanceProtectionProof);
                if (!sourceCells.TryGetValue(new CellKey(node.Cell.X, node.Cell.Y), out var source) ||
                    source.Slice != node.Slice || node.Sector != request.Canvas.Id)
                    Add(failures, "GeneratedTerrainSource", "NODE_SOURCE_COORDINATE_MISMATCH", node.NodeId,
                        "EXISTING_SECTOR_SLICE_CELL", node.Sector.Value + "/" + node.Slice + "/" + node.Cell);
                else if (IsSolidBlocked(source.Cell))
                    Add(failures, "GraphIntegrity", "NODE_INSIDE_HARD_SOLID", node.NodeId,
                        "NON_BLOCKED_GENERATED_CELL", source.Cell.Layers.Solid.StableId);
            }
            foreach (var edge in graph.Edges)
            {
                if (!nodeIds.Contains(edge.FromNodeId) || !nodeIds.Contains(edge.ToNodeId))
                    Add(failures, "GraphIntegrity", "DANGLING_EDGE_REFERENCE", edge.EdgeId,
                        "EXISTING_FROM_AND_TO_NODE", edge.FromNodeId + "->" + edge.ToNodeId);
                if (!Enum.IsDefined(typeof(TraversalMovementKind), edge.MovementKind))
                    Add(failures, "MovementKindSupport", "UNSUPPORTED_MOVEMENT_KIND", edge.EdgeId,
                        "Walk|Jump|Drop|Climb|Slide|Bounce", Number((int)edge.MovementKind));
                var expectedEdgeId = new GeneratedTileMovementEdge(edge.FromNodeId, edge.ToNodeId,
                    edge.MovementKind, edge.RequiredEnvelopeKinds, edge.ProvenEnvelopeKinds,
                    edge.RuleIds, edge.Cost, edge.FailurePolicy).EdgeId;
                if (!BakingCanonicalDigest.IsLowerHexSha256(edge.EdgeId) ||
                    !string.Equals(edge.EdgeId, expectedEdgeId, StringComparison.Ordinal))
                    Add(failures, "GraphIntegrity", "EDGE_ID_SEMANTIC_MISMATCH", edge.EdgeId,
                        expectedEdgeId, edge.EdgeId);
                var matrix = surface.RuleRegistry.MovementEnvelopeMatrix
                    .SingleOrDefault(value => value.MovementKind == edge.MovementKind);
                if (matrix == null || !edge.RequiredEnvelopeKinds.SequenceEqual(matrix.RequiredEnvelopeKinds) ||
                    !edge.ProvenEnvelopeKinds.SequenceEqual(matrix.RequiredEnvelopeKinds))
                    Add(failures, "EnvelopeSupport", "MISSING_REQUIRED_ENVELOPE_PROOF", edge.EdgeId,
                        matrix == null ? "SUPPORTED_MATRIX_ENTRY" : matrix.StableToken,
                        string.Join(",", edge.ProvenEnvelopeKinds));
                foreach (var envelope in edge.RequiredEnvelopeKinds.Concat(edge.ProvenEnvelopeKinds)
                             .Where(value => !Enum.IsDefined(typeof(CompiledTraversalEnvelopeSetKind), value)))
                    Add(failures, "EnvelopeSupport", "UNSUPPORTED_ENVELOPE_KIND", edge.EdgeId,
                        "DEFINED_ENVELOPE", Number((int)envelope));
                foreach (var ruleId in edge.RuleIds.Where(ruleId => !surface.RuleRegistry.Rules.Any(
                             rule => string.Equals(rule.RuleId, ruleId, StringComparison.Ordinal))))
                    Add(failures, "RuleRegistry", "MISSING_RULE_REFERENCE", edge.EdgeId,
                        "REGISTERED_RULE_ID", ruleId);
            }
            foreach (var link in graph.SocketLinks)
            {
                if (!socketIds.Contains(link.FromSocketNodeId) || !socketIds.Contains(link.ToSocketNodeId))
                    Add(failures, "SocketIntegrity", "DANGLING_SOCKET_LINK_REFERENCE", link.LinkId,
                        "EXISTING_SOCKET_FROM_AND_TO", link.FromSocketNodeId + "->" + link.ToSocketNodeId);
                if (!IsOpposite(link.FromDirection, link.ToDirection))
                    Add(failures, "SocketIntegrity", "INVALID_SOCKET_DIRECTION_PAIR", link.LinkId,
                        "OPPOSITE_DIRECTIONS", link.FromDirection + "->" + link.ToDirection);
                if (!BakingCanonicalDigest.IsLowerHexSha256(link.ProvenanceDigest))
                    Add(failures, "SocketIntegrity", "INVALID_SOCKET_PROVENANCE", link.LinkId,
                        "LOWER_HEX_SHA256", link.ProvenanceDigest);
                var expectedLinkId = new GeneratedIntersectorSocketLink(link.FromSocketNodeId,
                    link.ToSocketNodeId, link.FromDirection, link.ToDirection,
                    link.ProvenanceDigest).LinkId;
                if (!BakingCanonicalDigest.IsLowerHexSha256(link.LinkId) ||
                    !string.Equals(link.LinkId, expectedLinkId, StringComparison.Ordinal))
                    Add(failures, "SocketIntegrity", "SOCKET_LINK_ID_SEMANTIC_MISMATCH", link.LinkId,
                        expectedLinkId, link.LinkId);
            }
            foreach (GeneratedTileMovementNodeKind kind in Enum.GetValues(typeof(GeneratedTileMovementNodeKind)))
                if (!graph.Nodes.Any(value => value.Kind == kind))
                    Add(failures, "GraphIntegrity", "MISSING_REQUIRED_NODE_KIND", kind.ToString(),
                        "PRESENT", "MISSING");
            foreach (TraversalMovementKind movement in Enum.GetValues(typeof(TraversalMovementKind)))
                if (!graph.Edges.Any(value => value.MovementKind == movement))
                    Add(failures, "GraphIntegrity", "MISSING_REQUIRED_MOVEMENT_KIND", movement.ToString(),
                        "PRESENT", "MISSING");

            Digest(failures, "DigestIntegrity", "NODE_SET_DIGEST_MISMATCH",
                graph.NodeSetDigest, graph.ComputeNodeSetDigest());
            Digest(failures, "DigestIntegrity", "EDGE_SET_DIGEST_MISMATCH",
                graph.EdgeSetDigest, graph.ComputeEdgeSetDigest());
            Digest(failures, "DigestIntegrity", "SOCKET_LINK_DIGEST_MISMATCH",
                graph.SocketLinkDigest, graph.ComputeSocketLinkDigest());
            Digest(failures, "DigestIntegrity", "GRAPH_DIGEST_MISMATCH",
                graph.GraphDigest, graph.ComputeGraphDigest());
            Digest(failures, "DigestIntegrity", "MAP19_03_HANDOFF_DIGEST_MISMATCH",
                graph.Map19_03HandoffDigest, graph.ComputeMap19_03HandoffDigest());

            if (graph.Stats == null)
                Add(failures, "GraphIntegrity", "MISSING_GRAPH_STATS", "stats", "NON_NULL", "NULL");
            else
            {
                Zero(failures, "RuleRegistry", "HARDCODED_TRAVERSAL_THRESHOLD_DUPLICATION",
                    graph.Stats.HardcodedDuplicateTraversalThresholds);
                Zero(failures, "EnvelopeSupport", "MATRIX_LOCAL_COPY_MISMATCH",
                    graph.Stats.MatrixLocalCopyMismatches);
                Zero(failures, "MovementKindSupport", "UNSUPPORTED_MOVEMENT_COUNT",
                    graph.Stats.UnsupportedMovementKinds);
                Zero(failures, "EnvelopeSupport", "UNSUPPORTED_ENVELOPE_COUNT",
                    graph.Stats.UnsupportedEnvelopeKinds);
            }

            return failures.Count == 0
                ? new GeneratedTileMovementGraphResult(graph, failures)
                : Failure(failures);
        }

        private static bool ValidateRequest(
            GeneratedTileMovementGraphRequest request,
            ICollection<GeneratedTileMovementGraphFailure> failures)
        {
            if (request == null)
            {
                Add(failures, "GraphBuilder", "MISSING_REQUEST", "request", "NON_NULL", "NULL");
                return false;
            }
            if (request.ProfileRuleLock == null || !request.ProfileRuleLock.Success ||
                request.ProfileRuleLock.Surface == null)
                Add(failures, "MAP19_01", "MISSING_LOCKED_PROFILE_RULE_REGISTRY", "profile_rule_lock",
                    "SUCCESSFUL_LOCK_SURFACE", "MISSING_OR_FAILED");
            else
            {
                Digest(failures, "MAP19_01", "PROFILE_DIGEST_MISMATCH",
                    request.ProfileRuleLock.Surface.TraversalProfileDigest, ExpectedTraversalProfileDigest);
                Digest(failures, "MAP19_01", "RULE_REGISTRY_DIGEST_MISMATCH",
                    request.ProfileRuleLock.Surface.RuleRegistryDigest, ExpectedRuleRegistryDigest);
                Digest(failures, "MAP19_01", "MATRIX_DIGEST_MISMATCH",
                    request.ProfileRuleLock.Surface.MovementEnvelopeMatrixDigest,
                    ExpectedMovementEnvelopeMatrixDigest);
                Digest(failures, "MAP19_01", "MAP19_02_HANDOFF_DIGEST_MISMATCH",
                    request.ProfileRuleLock.Surface.Map19_02HandoffDigest,
                    ExpectedMap19_02IncomingHandoffDigest);
            }
            if (request.Canvas == null)
                Add(failures, "GeneratedTerrainSource", "MISSING_SECTOR_CANVAS", "canvas",
                    "NON_NULL", "NULL");
            if (request.Slices == null)
                Add(failures, "GeneratedTerrainSource", "MISSING_GENERATED_SLICE_SET", "slices",
                    "NON_NULL", "NULL");
            if (request.BoundaryAudit == null || !request.BoundaryAudit.IsZero)
                Add(failures, "ForbiddenWork", "FORBIDDEN_SIDE_EFFECT_OR_SEARCH_ATTEMPT", "boundary_audit",
                    "ALL_ZERO_AND_MAP19_03_NOT_STARTED", request.BoundaryAudit == null ? "NULL" : "NON_ZERO");
            return failures.Count == 0;
        }

        private static List<GeneratedIntersectorSocketLink> BuildSocketLinks(
            IEnumerable<GeneratedTileMovementNode> nodes)
        {
            var sockets = nodes.Where(value => value.Kind == GeneratedTileMovementNodeKind.IntersectorSocket)
                .ToDictionary(value => new CellKey(value.Cell.X, value.Cell.Y), value => value);
            var links = new List<GeneratedIntersectorSocketLink>();
            foreach (var socket in sockets.Values.OrderBy(value => value.NodeId, StringComparer.Ordinal))
            {
                TryDirection(socket.SurfaceId, out var direction);
                var delta = Delta(direction);
                if (!sockets.TryGetValue(new CellKey(socket.Cell.X + delta.Item1,
                        socket.Cell.Y + delta.Item2), out var other))
                    continue;
                TryDirection(other.SurfaceId, out var otherDirection);
                if (!IsOpposite(direction, otherDirection) ||
                    string.Compare(socket.NodeId, other.NodeId, StringComparison.Ordinal) >= 0)
                    continue;
                var provenance = BakingCanonicalDigest.HashCanonicalLines(new[]
                {
                    "MAP19_02_INTERSECTOR_SOCKET_PROVENANCE_V1", socket.SourceDigest,
                    other.SourceDigest, socket.Sector.Value, Number(socket.Slice.X), Number(socket.Slice.Y),
                    Number(other.Slice.X), Number(other.Slice.Y),
                });
                links.Add(new GeneratedIntersectorSocketLink(socket.NodeId, other.NodeId,
                    direction, otherDirection, provenance));
            }
            return links;
        }

        private static GeneratedTraversalValidationRule SelectCostRule(
            GeneratedTraversalRuleRegistry registry,
            TraversalMovementKind movement)
        {
            GeneratedTraversalRuleOwner owner;
            switch (movement)
            {
                case TraversalMovementKind.Jump: owner = GeneratedTraversalRuleOwner.JumpArcEnvelope; break;
                case TraversalMovementKind.Drop: owner = GeneratedTraversalRuleOwner.FallLimit; break;
                case TraversalMovementKind.Climb: owner = GeneratedTraversalRuleOwner.ClimbReach; break;
                case TraversalMovementKind.Bounce: owner = GeneratedTraversalRuleOwner.BounceArc; break;
                default: owner = GeneratedTraversalRuleOwner.ChaseWidth; break;
            }
            return registry.Rules.Single(value => value.Owner == owner);
        }

        private static GeneratedTileMovementNode Node(
            GeneratedTileMovementNodeKind kind,
            SectorCanvasId sector,
            GeneratedSliceCoord slice,
            LocalTileCoord cell,
            string surface,
            string owner,
            string sourceDigest,
            string proof) => new GeneratedTileMovementNode(kind, sector, slice, cell, surface,
            owner, sourceDigest, proof);

        private static LocalTileCoord Global(GeneratedSliceCoord slice, LocalTileCoord local) =>
            new LocalTileCoord(slice.X * WorldGenConstants.MicroChunkWidthTiles + local.X,
                slice.Y * WorldGenConstants.MicroChunkHeightTiles + local.Y);

        private static string ComputeSourceTerrainDigest(
            SectorCanvasContract canvas,
            GeneratedSliceSet slices,
            string canvasDigest) => BakingCanonicalDigest.HashCanonicalLines(new[]
        {
            "MAP19_02_GENERATED_TERRAIN_SOURCE_V1", canvas.Id.Value, canvasDigest,
            canvas.ValidationStamp.StableDigest, slices.SourceCanvasId.Value,
            Number(slices.Slices.Count),
        });

        private static bool IsSolidBlocked(GeneratedSliceCell cell) =>
            string.Equals(cell.Layers.Solid.StableId, BlockedSolid, StringComparison.Ordinal) ||
            !cell.Layers.Hazard.IsExplicitEmpty;
        private static bool IsClearanceBlocked(GeneratedSliceCell cell) =>
            string.Equals(cell.Layers.Marker.StableId, ClearanceBlockedMarker, StringComparison.Ordinal);
        private static bool HasProtectedEnvelopeConflict(GeneratedSliceCell cell) =>
            cell.Provenance != null && cell.Provenance.Sources.Any(value => value != null &&
                value.IsProtected && value.Kind != CanvasSourceKind.TerrainCluster);
        private static string SourceOwner(GeneratedSliceCell cell) => cell.Provenance?.Sources
            .Where(value => value != null && value.OwnedLayers.Contains(SectorCanvasLayerKind.Owner))
            .Select(value => value.StableId).FirstOrDefault() ?? "MISSING";
        private static string SourceDigest(
            SectorCanvasId sector,
            GeneratedSliceCoord slice,
            LocalTileCoord global,
            GeneratedSliceCell cell) => BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_02_TILE_SOURCE_PROVENANCE_V1", sector.Value, Number(slice.X), Number(slice.Y),
                Number(global.X), Number(global.Y), cell.Layers.Solid.StableId,
                cell.Layers.Affordance.StableId, cell.Layers.Marker.StableId,
                SourceOwner(cell), string.Join(",", (cell.Provenance?.Sources ??
                    Array.Empty<CanvasSourceRef>()).Select(value => value.StableId)),
            });
        private static string ClearanceProof(GeneratedSliceCell cell) =>
            BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_02_CLEARANCE_PROTECTION_PROOF_V1",
                IsClearanceBlocked(cell) ? "CLEARANCE_BLOCKED" : "CLEARANCE_AVAILABLE",
                HasProtectedEnvelopeConflict(cell) ? "PROTECTED_CONFLICT" : "PROTECTED_CLEAR",
                cell.Layers.Hazard.IsExplicitEmpty ? "HAZARD_CLEAR" : "HAZARD_PRESENT",
            });

        private static bool TryDirection(string marker, out GeneratedIntersectorSocketDirection direction)
        {
            const string prefix = "INTERSECTOR_SOCKET_";
            direction = default(GeneratedIntersectorSocketDirection);
            return marker != null && marker.StartsWith(prefix, StringComparison.Ordinal) &&
                Enum.TryParse(marker.Substring(prefix.Length), true, out direction) &&
                Enum.IsDefined(typeof(GeneratedIntersectorSocketDirection), direction);
        }
        private static Tuple<int, int> Delta(GeneratedIntersectorSocketDirection direction)
        {
            switch (direction)
            {
                case GeneratedIntersectorSocketDirection.Left: return Tuple.Create(-1, 0);
                case GeneratedIntersectorSocketDirection.Right: return Tuple.Create(1, 0);
                case GeneratedIntersectorSocketDirection.Down: return Tuple.Create(0, -1);
                default: return Tuple.Create(0, 1);
            }
        }
        private static bool IsOpposite(
            GeneratedIntersectorSocketDirection left,
            GeneratedIntersectorSocketDirection right) =>
            (left == GeneratedIntersectorSocketDirection.Left && right == GeneratedIntersectorSocketDirection.Right) ||
            (left == GeneratedIntersectorSocketDirection.Right && right == GeneratedIntersectorSocketDirection.Left) ||
            (left == GeneratedIntersectorSocketDirection.Down && right == GeneratedIntersectorSocketDirection.Up) ||
            (left == GeneratedIntersectorSocketDirection.Up && right == GeneratedIntersectorSocketDirection.Down);

        private static void Duplicates(
            ICollection<GeneratedTileMovementGraphFailure> failures,
            IEnumerable<string> ids,
            string reason)
        {
            foreach (var duplicate in ids.GroupBy(value => value, StringComparer.Ordinal)
                         .Where(value => value.Count() > 1))
                Add(failures, "GraphIntegrity", reason, duplicate.Key, "1",
                    duplicate.Count().ToString(CultureInfo.InvariantCulture));
        }
        private static void Zero(
            ICollection<GeneratedTileMovementGraphFailure> failures,
            string owner,
            string reason,
            int actual)
        {
            if (actual != 0)
                Add(failures, owner, reason, reason.ToLowerInvariant(), "0", Number(actual));
        }
        private static void Digest(
            ICollection<GeneratedTileMovementGraphFailure> failures,
            string owner,
            string reason,
            string actual,
            string expected)
        {
            if (!BakingCanonicalDigest.IsLowerHexSha256(actual) ||
                !string.Equals(actual, expected, StringComparison.Ordinal))
                Add(failures, owner, reason, reason.ToLowerInvariant(), expected,
                    string.IsNullOrEmpty(actual) ? "MISSING" : actual);
        }
        private static GeneratedTileMovementGraphResult Failure(
            IEnumerable<GeneratedTileMovementGraphFailure> failures) =>
            new GeneratedTileMovementGraphResult(null, failures);
        private static void Add(
            ICollection<GeneratedTileMovementGraphFailure> failures,
            string owner,
            string reason,
            string key,
            string expected,
            string actual) => failures.Add(new GeneratedTileMovementGraphFailure(
            owner, reason, key, expected, actual));
        private static string JoinErrors<T>(IEnumerable<T> errors) =>
            string.Join(";", errors.Select(value => value == null ? "NULL" : value.ToString()));
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private readonly struct CellKey : IEquatable<CellKey>
        {
            public CellKey(int x, int y) { X = x; Y = y; }
            public int X { get; }
            public int Y { get; }
            public bool Equals(CellKey other) => X == other.X && Y == other.Y;
            public override bool Equals(object obj) => obj is CellKey other && Equals(other);
            public override int GetHashCode() { unchecked { return (X * 397) ^ Y; } }
        }

        private readonly struct NodeKey : IEquatable<NodeKey>
        {
            public NodeKey(int x, int y, GeneratedTileMovementNodeKind kind)
            { X = x; Y = y; Kind = kind; }
            public int X { get; }
            public int Y { get; }
            public GeneratedTileMovementNodeKind Kind { get; }
            public bool Equals(NodeKey other) => X == other.X && Y == other.Y && Kind == other.Kind;
            public override bool Equals(object obj) => obj is NodeKey other && Equals(other);
            public override int GetHashCode()
            { unchecked { return (((X * 397) ^ Y) * 397) ^ (int)Kind; } }
        }
    }
}
