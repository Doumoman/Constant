using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class SectorSpineGraphBuilder
    {
        public const string ReferenceGraphPublicationLabel = "REFERENCE SPINE GRAPH";

        public static SectorSpineGraphBuildResult Build(SectorSpineEnvelopeBuildRequest request)
        {
            var errors = new List<SectorSpineEnvelopeError>();
            ValidateRequest(request, errors);
            if (request == null || request.Input == null || request.AnchorPlan == null || request.ClusterPlacementPlan == null)
                return Failure(errors);

            var assignments = ValidateAssignments(request, errors);
            if (errors.Count > 0) return Failure(errors);

            var nodes = new List<SectorSpineNode>();
            foreach (var placement in request.ClusterPlacementPlan.Placements.OrderBy(value => value.SectorIndex))
            {
                if (!request.Input.TryGetSector(placement.SectorCoordinate, out var sector) || sector.SectorIndex != placement.SectorIndex)
                {
                    Add(errors, SectorSpineEnvelopeErrorCode.SectorMismatch, Subject(placement.SectorIndex),
                        "Cluster placement does not match a published planner sector.");
                    continue;
                }
                BuildSectorNodes(request, sector, placement, nodes, errors);
            }

            ValidateNodes(request, nodes, errors);
            if (errors.Count > 0) return Failure(errors);

            var edges = BuildEdges(request, assignments, nodes, errors);
            ValidateEdgesAndConnectivity(request, nodes, edges, errors);
            if (errors.Count > 0) return Failure(errors);

            var assignmentDigest = AssignmentDigest(request.Assignments);
            var routeAccessIdentity = RouteAccessIdentity(request.Input);
            var socketIdentity = ExternalSocketIdentity(request.Input);
            var boundaryIdentity = BoundaryIdentity(request.Input);
            var specialIdentity = SpecialIdentity(request.Input);
            var clusterIdentity = ClusterIdentity(request.ClusterPlacementPlan);
            var provisional = new SectorSpineGraph(
                request.GraphPublicationLabel,
                request.Input.CanonicalDigest,
                assignmentDigest,
                request.AnchorPlan.CanonicalDigest,
                request.ClusterPlacementPlan.CanonicalDigest,
                routeAccessIdentity,
                socketIdentity,
                boundaryIdentity,
                specialIdentity,
                clusterIdentity,
                nodes,
                edges,
                string.Empty);
            var digest = SectorSpineEnvelopeCanonicalDigest.ComputeGraph(provisional);
            var graph = new SectorSpineGraph(
                request.GraphPublicationLabel,
                request.Input.CanonicalDigest,
                assignmentDigest,
                request.AnchorPlan.CanonicalDigest,
                request.ClusterPlacementPlan.CanonicalDigest,
                routeAccessIdentity,
                socketIdentity,
                boundaryIdentity,
                specialIdentity,
                clusterIdentity,
                nodes,
                edges,
                digest);
            return new SectorSpineGraphBuildResult(graph, Array.Empty<SectorSpineEnvelopeError>());
        }

        private static void ValidateRequest(SectorSpineEnvelopeBuildRequest request, ICollection<SectorSpineEnvelopeError> errors)
        {
            if (request == null)
            {
                Add(errors, SectorSpineEnvelopeErrorCode.MissingInput, "request", "A spine-envelope build request is required.");
                return;
            }
            if (request.Input == null) Add(errors, SectorSpineEnvelopeErrorCode.MissingInput, "input", "SectorPlannerInput is required.");
            if (request.AnchorPlan == null) Add(errors, SectorSpineEnvelopeErrorCode.MissingAnchorPlan, "anchorPlan", "SectorFixedAnchorPlan is required.");
            if (request.ClusterPlacementPlan == null) Add(errors, SectorSpineEnvelopeErrorCode.MissingClusterPlacementPlan, "clusterPlacementPlan", "SectorClusterPlacementPlan is required.");
            if (!string.Equals(request.GraphPublicationLabel, ReferenceGraphPublicationLabel, StringComparison.Ordinal))
                Add(errors, SectorSpineEnvelopeErrorCode.NonCanonicalPublication, "graphPublicationLabel", "Graph publication must be marked REFERENCE SPINE GRAPH.");
            if (!string.Equals(request.EnvelopePublicationLabel, SectorTraversalEnvelopeBuilder.ReferenceEnvelopePublicationLabel, StringComparison.Ordinal))
                Add(errors, SectorSpineEnvelopeErrorCode.NonCanonicalPublication, "envelopePublicationLabel", "Envelope publication must be marked REFERENCE TRAVERSAL ENVELOPE.");

            if (request.RouteAccessMutationClaim) Add(errors, SectorSpineEnvelopeErrorCode.RouteAccessMutationClaim, "routeAccess", "Spine construction cannot mutate RouteType or AccessClass.");
            if (request.AnchorMutationClaim) Add(errors, SectorSpineEnvelopeErrorCode.AnchorMutationClaim, "anchor", "Spine construction cannot mutate fixed anchors.");
            if (request.ClusterMutationClaim) Add(errors, SectorSpineEnvelopeErrorCode.ClusterMutationClaim, "cluster", "Spine construction cannot mutate cluster placements.");
            if (request.MicroPatternRenderCount != 0) Add(errors, SectorSpineEnvelopeErrorCode.PatternMutationClaim, "pattern", "MicroPattern rendering is outside MAP14_04.");
            if (request.ActivityEventPlacementCount != 0) Add(errors, SectorSpineEnvelopeErrorCode.ActivityMutationClaim, "activityEvent", "Activity/Event placement is outside MAP14_04.");
            if (request.RetryCount != 0 || request.SolverInvocationCount != 0) Add(errors, SectorSpineEnvelopeErrorCode.SolverMutationClaim, "solver", "Retry and solver invocation counts must remain zero.");
            if (request.RandomDrawCount != 0) Add(errors, SectorSpineEnvelopeErrorCode.RngMutationClaim, "rng", "RNG draw count must remain zero.");
            if (request.TileWriteCount != 0) Add(errors, SectorSpineEnvelopeErrorCode.TileMutationClaim, "tile", "Tile write count must remain zero.");
            if (request.CanvasOwnershipWriteCount != 0) Add(errors, SectorSpineEnvelopeErrorCode.CanvasMutationClaim, "canvas", "Final canvas ownership is outside MAP14_04.");
            if (request.SceneMutationCount != 0) Add(errors, SectorSpineEnvelopeErrorCode.SceneMutationClaim, "scene", "Scene/Prefab/Tilemap/GameObject mutation count must remain zero.");
            if (request.PhysicsInvocationCount != 0) Add(errors, SectorSpineEnvelopeErrorCode.PhysicsMutationClaim, "physics", "Live physics invocation is outside MAP14_04.");
            foreach (var fault in request.ReferenceFaults)
                Add(errors, fault, "referenceFault", "Injected invalid REFERENCE SPINE GRAPH evidence must fail atomically.");

            if (request.Input != null && request.AnchorPlan != null
                && !string.Equals(request.Input.CanonicalDigest, request.AnchorPlan.PlannerInputDigest, StringComparison.Ordinal))
                Add(errors, SectorSpineEnvelopeErrorCode.SectorMismatch, "anchorPlan", "Anchor plan input identity must match the planner input.");
            if (request.AnchorPlan != null && request.ClusterPlacementPlan != null
                && !string.Equals(request.AnchorPlan.CanonicalDigest, request.ClusterPlacementPlan.AnchorPlanDigest, StringComparison.Ordinal))
                Add(errors, SectorSpineEnvelopeErrorCode.SectorMismatch, "clusterPlacementPlan", "Cluster placement anchor identity must match the fixed anchor plan.");
            if (request.Input != null && request.ClusterPlacementPlan != null
                && (request.Input.Sectors.Count != request.ClusterPlacementPlan.SectorCount || !request.ClusterPlacementPlan.Map14_04HandoffReady))
                Add(errors, SectorSpineEnvelopeErrorCode.SectorMismatch, "clusterPlacementPlan", "Every planner sector requires one overlap-free MAP14_04-ready cluster placement.");
        }

        private static Dictionary<int, SectorPacingAssignment> ValidateAssignments(
            SectorSpineEnvelopeBuildRequest request,
            ICollection<SectorSpineEnvelopeError> errors)
        {
            var result = new Dictionary<int, SectorPacingAssignment>();
            foreach (var assignment in request.Assignments)
            {
                var index = (assignment.Coordinate.Y * WorldGenConstants.SectorColumns) + assignment.Coordinate.X;
                if (!request.Input.TryGetSector(assignment.Coordinate, out var sector) || sector.SectorIndex != index || result.ContainsKey(index))
                {
                    Add(errors, SectorSpineEnvelopeErrorCode.SectorMismatch, Subject(index), "Pacing assignment coordinate must occur exactly once in planner input.");
                    continue;
                }
                var expected = SectorPacingRolePlanner.Assign(request.Input, assignment.Coordinate);
                if (assignment.PrimaryRole != expected.PrimaryRole
                    || !string.Equals(assignment.SourceIdentityDigest, expected.SourceIdentityDigest, StringComparison.Ordinal)
                    || !string.Equals(assignment.CanonicalDigest, expected.CanonicalDigest, StringComparison.Ordinal))
                {
                    Add(errors, SectorSpineEnvelopeErrorCode.SectorMismatch, Subject(index), "Pacing assignment must preserve the public MAP14_01 publication.");
                    continue;
                }
                result.Add(index, assignment);
            }
            foreach (var sector in request.Input.Sectors)
                if (!result.ContainsKey(sector.SectorIndex)) Add(errors, SectorSpineEnvelopeErrorCode.MissingEndpoint, Subject(sector.SectorIndex), "A pacing assignment is required before spine publication.");
            return result;
        }

        private static void BuildSectorNodes(
            SectorSpineEnvelopeBuildRequest request,
            SectorPlannerSectorSnapshot sector,
            SectorClusterPlacement placement,
            ICollection<SectorSpineNode> nodes,
            ICollection<SectorSpineEnvelopeError> errors)
        {
            var routeType = sector.Route.RouteType;
            var access = sector.Route.AccessClass;
            var sourcePrefix = request.ClusterPlacementPlan.CanonicalDigest + "/" + placement.ClusterId.Value + "/" + placement.VariantId.Value;
            var minX = placement.TileRects.Min(value => value.X);
            var maxX = placement.TileRects.Max(value => value.XMaxExclusive) - 1;
            var minY = placement.TileRects.Min(value => value.Y);
            var maxY = placement.TileRects.Max(value => value.YMaxExclusive) - 1;
            var centerY = minY + ((maxY - minY) / 2);
            nodes.Add(Node(sector, SectorSpineNodeKind.ClusterEntry, SectorSpineEndpointRole.Entry,
                new LocalTileCoord(minX, centerY), "CLUSTER_ENTRY:" + placement.ClusterId.Value, sourcePrefix, routeType, access));
            nodes.Add(Node(sector, SectorSpineNodeKind.ClusterExit, SectorSpineEndpointRole.Exit,
                new LocalTileCoord(maxX, centerY), "CLUSTER_EXIT:" + placement.VariantId.Value, sourcePrefix, routeType, access));

            var anchors = request.AnchorPlan.Anchors.Where(value => value.SectorIndex == sector.SectorIndex).ToArray();
            foreach (var anchor in anchors.Where(value => value.Kind == SectorFixedAnchorKind.ExternalRouteSocket))
            {
                var role = anchor.Side == SectorPlannerSide.Right || anchor.Side == SectorPlannerSide.Down
                    ? SectorSpineEndpointRole.Exit : SectorSpineEndpointRole.Entry;
                nodes.Add(Node(sector, SectorSpineNodeKind.ExternalSocket, role, Center(anchor.Rect), anchor.AnchorId,
                    request.AnchorPlan.CanonicalDigest + "/" + anchor.SourceIdentity, routeType, access));
            }
            foreach (var anchor in anchors.Where(value => value.Kind == SectorFixedAnchorKind.BoundaryFixedSlice))
                nodes.Add(Node(sector, SectorSpineNodeKind.BoundaryBridge, SectorSpineEndpointRole.Evidence, Center(anchor.Rect), anchor.AnchorId,
                    request.AnchorPlan.CanonicalDigest + "/" + anchor.SourceIdentity, routeType, access));
            foreach (var anchor in anchors.Where(value => value.Kind == SectorFixedAnchorKind.SpecialEntryReturn))
            {
                var entry = new LocalTileCoord(anchor.Rect.X, anchor.Rect.Y + Math.Max(0, (anchor.Rect.Height / 2) - 1));
                var returned = new LocalTileCoord(anchor.Rect.XMaxExclusive - 1, anchor.Rect.Y + Math.Min(anchor.Rect.Height - 1, anchor.Rect.Height / 2));
                nodes.Add(Node(sector, SectorSpineNodeKind.SpecialEntry, SectorSpineEndpointRole.Entry, entry, anchor.AnchorId + ":ENTRY",
                    request.AnchorPlan.CanonicalDigest + "/" + anchor.SourceIdentity, routeType, access));
                nodes.Add(Node(sector, SectorSpineNodeKind.SpecialReturn, SectorSpineEndpointRole.Return, returned, anchor.AnchorId + ":RETURN",
                    request.AnchorPlan.CanonicalDigest + "/" + anchor.SourceIdentity, routeType, access));
            }

            var optional = sector.Route.HighRoute || sector.OptionalRegions.Any(value => value.Available && value.DeferredLocal);
            var recovery = sector.Route.RecoveryNeeded || sector.SpecialRegion.Kind == SectorPlannerSpecialRegionKind.Boss || optional;
            var blocking = SectorSpineEnvelopeAnchorUtility.BlockingCells(request.AnchorPlan, sector.SectorIndex);
            if (recovery)
            {
                var coordinate = FindOpen(new LocalTileCoord(24, 25), blocking);
                nodes.Add(Node(sector, SectorSpineNodeKind.RecoveryJoin, SectorSpineEndpointRole.Rejoin, coordinate,
                    "RECOVERY_JOIN", request.Input.CanonicalDigest + "/RECOVERY", routeType, access));
            }
            if (optional)
            {
                var coordinate = FindOpen(new LocalTileCoord(36, 8), blocking);
                nodes.Add(Node(sector, SectorSpineNodeKind.OptionalBranch, SectorSpineEndpointRole.Branch, coordinate,
                    "OPTIONAL_BRANCH", request.Input.CanonicalDigest + "/OPTIONAL", routeType, access));
            }
        }

        private static SectorSpineNode Node(
            SectorPlannerSectorSnapshot sector,
            SectorSpineNodeKind kind,
            SectorSpineEndpointRole role,
            LocalTileCoord coordinate,
            string sourceId,
            string sourceIdentity,
            int routeType,
            AccessClass access)
        {
            var id = sector.SectorIndex.ToString("D3", CultureInfo.InvariantCulture) + ":" + kind.ToString().ToUpperInvariant() + ":" + sourceId;
            return new SectorSpineNode(id, sector.Coordinate, sector.SectorIndex, kind, role, coordinate, routeType, access, sourceId, sourceIdentity);
        }

        private static void ValidateNodes(
            SectorSpineEnvelopeBuildRequest request,
            IReadOnlyList<SectorSpineNode> nodes,
            ICollection<SectorSpineEnvelopeError> errors)
        {
            foreach (var duplicate in nodes.GroupBy(value => value.NodeId, StringComparer.Ordinal).Where(value => value.Count() > 1))
                Add(errors, SectorSpineEnvelopeErrorCode.DuplicateNode, duplicate.Key, "Spine node IDs must be unique.");
            foreach (var node in nodes.Where(value => !Inside(value.Coordinate)))
                Add(errors, SectorSpineEnvelopeErrorCode.NodeOutOfBounds, node.NodeId, "Spine nodes must remain inside 48x32 sector-local tiles.");
            foreach (var placement in request.ClusterPlacementPlan.Placements)
            {
                var local = nodes.Where(value => value.SectorIndex == placement.SectorIndex).ToArray();
                if (local.Count(value => value.Kind == SectorSpineNodeKind.ClusterEntry) != 1
                    || local.Count(value => value.Kind == SectorSpineNodeKind.ClusterExit) != 1)
                    Add(errors, SectorSpineEnvelopeErrorCode.MissingEndpoint, Subject(placement.SectorIndex), "Each placed cluster requires one entry and one exit node.");
                if (request.Input.TryGetSector(placement.SectorCoordinate, out var sector))
                {
                    var expectedSockets = request.AnchorPlan.Anchors.Count(value => value.SectorIndex == placement.SectorIndex && value.Kind == SectorFixedAnchorKind.ExternalRouteSocket);
                    if (sector.Route.ExternalSockets.Count != expectedSockets)
                        Add(errors, SectorSpineEnvelopeErrorCode.MissingEndpoint, Subject(placement.SectorIndex), "Published external socket IDs and fixed socket anchors must match exactly.");
                }
            }
        }

        private static List<SectorSpineEdge> BuildEdges(
            SectorSpineEnvelopeBuildRequest request,
            IReadOnlyDictionary<int, SectorPacingAssignment> assignments,
            IReadOnlyList<SectorSpineNode> nodes,
            ICollection<SectorSpineEnvelopeError> errors)
        {
            var result = new List<SectorSpineEdge>();
            foreach (var group in nodes.GroupBy(value => value.SectorIndex).OrderBy(value => value.Key))
            {
                var local = group.OrderBy(value => value).ToArray();
                var mandatory = local.Where(value => value.Kind == SectorSpineNodeKind.ExternalSocket
                                                     || value.Kind == SectorSpineNodeKind.ClusterEntry
                                                     || value.Kind == SectorSpineNodeKind.ClusterExit
                                                     || value.Kind == SectorSpineNodeKind.SpecialEntry
                                                     || value.Kind == SectorSpineNodeKind.SpecialReturn).ToArray();
                for (var index = 1; index < mandatory.Length; index++)
                {
                    var from = mandatory[index - 1];
                    var to = mandatory[index];
                    var kind = from.Kind == SectorSpineNodeKind.ClusterEntry && to.Kind == SectorSpineNodeKind.ClusterExit
                        ? SectorSpineEdgeKind.ClusterConnector
                        : from.Kind == SectorSpineNodeKind.SpecialEntry || from.Kind == SectorSpineNodeKind.SpecialReturn
                          || to.Kind == SectorSpineNodeKind.SpecialEntry || to.Kind == SectorSpineNodeKind.SpecialReturn
                            ? SectorSpineEdgeKind.MandatorySpecialConnector
                            : SectorSpineEdgeKind.MandatoryLow;
                    AddEdge(request, assignments[group.Key], from, to, kind, result, errors);
                }

                var boundary = local.Where(value => value.Kind == SectorSpineNodeKind.BoundaryBridge).ToArray();
                foreach (var node in boundary)
                {
                    var target = local.Where(value => value.Kind == SectorSpineNodeKind.ExternalSocket).OrderBy(value => Distance(value.Coordinate, node.Coordinate)).ThenBy(value => value.NodeId).FirstOrDefault()
                                 ?? mandatory.FirstOrDefault();
                    if (target != null) AddEdge(request, assignments[group.Key], node, target, SectorSpineEdgeKind.BoundaryConnector, result, errors);
                }

                var recovery = local.FirstOrDefault(value => value.Kind == SectorSpineNodeKind.RecoveryJoin);
                var optional = local.FirstOrDefault(value => value.Kind == SectorSpineNodeKind.OptionalBranch);
                var clusterEntry = local.First(value => value.Kind == SectorSpineNodeKind.ClusterEntry);
                var clusterExit = local.First(value => value.Kind == SectorSpineNodeKind.ClusterExit);
                if (optional != null && recovery != null)
                {
                    AddEdge(request, assignments[group.Key], optional, recovery, SectorSpineEdgeKind.OptionalHigh, result, errors);
                    AddEdge(request, assignments[group.Key], recovery, clusterExit, SectorSpineEdgeKind.Return, result, errors);
                }
                else if (recovery != null)
                {
                    var source = local.FirstOrDefault(value => value.Kind == SectorSpineNodeKind.SpecialReturn) ?? clusterExit;
                    AddEdge(request, assignments[group.Key], source, recovery, SectorSpineEdgeKind.Recovery, result, errors);
                    AddEdge(request, assignments[group.Key], recovery, clusterEntry, SectorSpineEdgeKind.Return, result, errors);
                }
            }
            return result;
        }

        private static void AddEdge(
            SectorSpineEnvelopeBuildRequest request,
            SectorPacingAssignment assignment,
            SectorSpineNode from,
            SectorSpineNode to,
            SectorSpineEdgeKind kind,
            ICollection<SectorSpineEdge> edges,
            ICollection<SectorSpineEnvelopeError> errors)
        {
            var blocked = SectorSpineEnvelopeAnchorUtility.BlockingCells(request.AnchorPlan, from.SectorIndex);
            var path = BuildPath(from.Coordinate, to.Coordinate, blocked);
            if (path.Count == 0)
            {
                Add(errors, SectorSpineEnvelopeErrorCode.MissingMandatoryRoute, from.NodeId + "->" + to.NodeId, "Reference endpoints cannot be connected without crossing a blocking anchor.");
                return;
            }
            var edgeId = from.SectorIndex.ToString("D3", CultureInfo.InvariantCulture) + ":" + kind.ToString().ToUpperInvariant() + ":" + from.NodeId + "->" + to.NodeId;
            var movement = "REFERENCE|" + assignment.PrimaryRole + "|" + string.Join(",", assignment.Reasons.OrderBy(value => value));
            var source = request.ClusterPlacementPlan.CanonicalDigest + "/" + request.AnchorPlan.CanonicalDigest + "/" + from.SourceId + "/" + to.SourceId;
            edges.Add(new SectorSpineEdge(edgeId, from.SectorIndex, kind, from.NodeId, to.NodeId,
                from.AccessClass.ToString(), movement, 1, path, source));
        }

        private static void ValidateEdgesAndConnectivity(
            SectorSpineEnvelopeBuildRequest request,
            IReadOnlyList<SectorSpineNode> nodes,
            IReadOnlyList<SectorSpineEdge> edges,
            ICollection<SectorSpineEnvelopeError> errors)
        {
            foreach (var duplicate in edges.GroupBy(value => value.EdgeId, StringComparer.Ordinal).Where(value => value.Count() > 1))
                Add(errors, SectorSpineEnvelopeErrorCode.DuplicateEdge, duplicate.Key, "Spine edge IDs must be unique.");
            var nodeIds = new HashSet<string>(nodes.Select(value => value.NodeId), StringComparer.Ordinal);
            foreach (var edge in edges)
            {
                if (!nodeIds.Contains(edge.FromNodeId) || !nodeIds.Contains(edge.ToNodeId))
                    Add(errors, SectorSpineEnvelopeErrorCode.MissingEndpoint, edge.EdgeId, "Each spine edge endpoint must reference a published node.");
                if (edge.CenterlineCells.Count == 0 || edge.CenterlineCells.Any(value => !Inside(value)))
                    Add(errors, SectorSpineEnvelopeErrorCode.EdgeOutOfBounds, edge.EdgeId, "Every ordered centerline cell must remain inside 48x32.");
                var blocking = SectorSpineEnvelopeAnchorUtility.BlockingCells(request.AnchorPlan, edge.SectorIndex);
                if (edge.CenterlineCells.Any(blocking.Contains))
                    Add(errors, SectorSpineEnvelopeErrorCode.EdgeCrossesBlockingAnchor, edge.EdgeId, "Centerline cannot cross SpecialFootprint, SiteReservation, or another incompatible anchor.");
            }

            foreach (var group in nodes.GroupBy(value => value.SectorIndex))
            {
                var required = group.Where(value => value.Kind == SectorSpineNodeKind.ExternalSocket
                                                    || value.Kind == SectorSpineNodeKind.ClusterEntry
                                                    || value.Kind == SectorSpineNodeKind.ClusterExit
                                                    || value.Kind == SectorSpineNodeKind.SpecialEntry
                                                    || value.Kind == SectorSpineNodeKind.SpecialReturn).ToArray();
                var adjacency = required.ToDictionary(value => value.NodeId, value => new List<string>(), StringComparer.Ordinal);
                foreach (var edge in edges.Where(value => value.SectorIndex == group.Key && value.Kind != SectorSpineEdgeKind.OptionalHigh && value.Kind != SectorSpineEdgeKind.Recovery))
                {
                    if (adjacency.ContainsKey(edge.FromNodeId) && adjacency.ContainsKey(edge.ToNodeId))
                    {
                        adjacency[edge.FromNodeId].Add(edge.ToNodeId);
                        adjacency[edge.ToNodeId].Add(edge.FromNodeId);
                    }
                }
                if (required.Length > 0)
                {
                    var visited = new HashSet<string>(StringComparer.Ordinal) { required[0].NodeId };
                    var queue = new Queue<string>(); queue.Enqueue(required[0].NodeId);
                    while (queue.Count > 0)
                        foreach (var next in adjacency[queue.Dequeue()].OrderBy(value => value, StringComparer.Ordinal)) if (visited.Add(next)) queue.Enqueue(next);
                    if (visited.Count != required.Length) Add(errors, SectorSpineEnvelopeErrorCode.MissingMandatoryRoute, Subject(group.Key), "All required external, cluster, and Special endpoints must share the mandatory reference component.");
                }

                if (group.Any(value => value.Kind == SectorSpineNodeKind.OptionalBranch))
                {
                    var optionalEdges = edges.Where(value => value.SectorIndex == group.Key && value.Kind == SectorSpineEdgeKind.OptionalHigh).ToArray();
                    if (optionalEdges.Length == 0 || optionalEdges.Any(value => !group.Any(node => node.NodeId == value.ToNodeId && node.Kind == SectorSpineNodeKind.RecoveryJoin)))
                        Add(errors, SectorSpineEnvelopeErrorCode.MissingRecoveryRoute, Subject(group.Key), "Optional/high reference edges must rejoin through RecoveryJoin.");
                }
                if (group.Any(value => value.Kind == SectorSpineNodeKind.RecoveryJoin)
                    && !edges.Any(value => value.SectorIndex == group.Key && (value.Kind == SectorSpineEdgeKind.Recovery || value.Kind == SectorSpineEdgeKind.OptionalHigh)))
                    Add(errors, SectorSpineEnvelopeErrorCode.MissingRecoveryRoute, Subject(group.Key), "Recovery evidence requires a recovery or optional-high edge.");
                if (group.Any(value => value.Kind == SectorSpineNodeKind.SpecialEntry)
                    && !edges.Any(value => value.SectorIndex == group.Key && value.Kind == SectorSpineEdgeKind.MandatorySpecialConnector))
                    Add(errors, SectorSpineEnvelopeErrorCode.MissingSpecialConnector, Subject(group.Key), "Reserved Special entry-return evidence requires a mandatory Special connector.");
            }
        }

        private static IReadOnlyList<LocalTileCoord> BuildPath(LocalTileCoord start, LocalTileCoord end, ISet<LocalTileCoord> blocked)
        {
            if (!Inside(start) || !Inside(end)) return Array.Empty<LocalTileCoord>();
            var queue = new Queue<LocalTileCoord>();
            var previous = new Dictionary<LocalTileCoord, LocalTileCoord>();
            var visited = new HashSet<LocalTileCoord> { start };
            queue.Enqueue(start);
            var directions = new[] { new LocalTileCoord(1, 0), new LocalTileCoord(0, 1), new LocalTileCoord(-1, 0), new LocalTileCoord(0, -1) };
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == end) break;
                foreach (var direction in directions)
                {
                    var next = new LocalTileCoord(current.X + direction.X, current.Y + direction.Y);
                    if (!Inside(next) || (blocked.Contains(next) && next != end) || !visited.Add(next)) continue;
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }
            if (!visited.Contains(end)) return Array.Empty<LocalTileCoord>();
            var path = new List<LocalTileCoord> { end };
            var cursor = end;
            while (cursor != start) { cursor = previous[cursor]; path.Add(cursor); }
            path.Reverse();
            return new ReadOnlyCollection<LocalTileCoord>(path);
        }

        private static LocalTileCoord FindOpen(LocalTileCoord preferred, ISet<LocalTileCoord> blocked)
        {
            for (var radius = 0; radius < WorldGenConstants.SectorWidthTiles + WorldGenConstants.SectorHeightTiles; radius++)
            for (var y = 0; y < WorldGenConstants.SectorHeightTiles; y++)
            for (var x = 0; x < WorldGenConstants.SectorWidthTiles; x++)
            {
                var candidate = new LocalTileCoord(x, y);
                if (Math.Abs(x - preferred.X) + Math.Abs(y - preferred.Y) == radius && !blocked.Contains(candidate)) return candidate;
            }
            return preferred;
        }

        private static LocalTileCoord Center(SectorFixedAnchorRect rect)
            => new LocalTileCoord(rect.X + ((rect.Width - 1) / 2), rect.Y + ((rect.Height - 1) / 2));

        private static bool Inside(LocalTileCoord value)
            => value.X >= 0 && value.X < WorldGenConstants.SectorWidthTiles && value.Y >= 0 && value.Y < WorldGenConstants.SectorHeightTiles;

        private static int Distance(LocalTileCoord left, LocalTileCoord right) => Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);

        private static string AssignmentDigest(IEnumerable<SectorPacingAssignment> assignments)
            => SectorSpineEnvelopeCanonicalDigest.Hash(string.Join("\n", assignments.OrderBy(value => value.Coordinate.Y).ThenBy(value => value.Coordinate.X).Select(value => value.CanonicalDigest)));

        private static string RouteAccessIdentity(SectorPlannerInput input)
            => SectorSpineEnvelopeCanonicalDigest.Hash(string.Join("\n", input.Sectors.Select(value => value.SectorIndex + "|" + value.Route.RouteType + "|" + value.Route.AccessClass)));

        private static string ExternalSocketIdentity(SectorPlannerInput input)
            => SectorSpineEnvelopeCanonicalDigest.Hash(string.Join("\n", input.Sectors.SelectMany(sector => sector.Route.ExternalSockets.Select(socket => sector.SectorIndex + "|" + socket))));

        private static string BoundaryIdentity(SectorPlannerInput input)
            => SectorSpineEnvelopeCanonicalDigest.Hash(string.Join("\n", input.Sectors.SelectMany(sector => sector.Boundaries.Select(value => sector.SectorIndex + "|" + value.PairId + "|" + value.CandidateId))));

        private static string SpecialIdentity(SectorPlannerInput input)
            => SectorSpineEnvelopeCanonicalDigest.Hash(string.Join("\n", input.Sectors.Select(value => value.SectorIndex + "|" + value.SpecialRegion.RegionId + "|" + value.SpecialRegion.Binding)));

        private static string ClusterIdentity(SectorClusterPlacementPlan plan)
            => SectorSpineEnvelopeCanonicalDigest.Hash(string.Join("\n", plan.Placements.OrderBy(value => value.SectorIndex).Select(value => value.SectorIndex + "|" + value.ClusterId.Value + "|" + value.VariantId.Value + "|" + value.Transform + "|" + string.Join(";", value.Cells.Select(cell => cell.ToString())))));

        private static string Subject(int sectorIndex) => sectorIndex.ToString("D3", CultureInfo.InvariantCulture);
        private static void Add(ICollection<SectorSpineEnvelopeError> errors, SectorSpineEnvelopeErrorCode code, string subject, string detail)
            => errors.Add(new SectorSpineEnvelopeError(code, subject, detail));
        private static SectorSpineGraphBuildResult Failure(IEnumerable<SectorSpineEnvelopeError> errors)
            => new SectorSpineGraphBuildResult(null, errors);
    }

    internal static class SectorSpineEnvelopeAnchorUtility
    {
        internal static bool IsCompatible(SectorFixedAnchor anchor)
        {
            return anchor.Kind == SectorFixedAnchorKind.ExternalRouteSocket
                   || anchor.Kind == SectorFixedAnchorKind.BoundaryFixedSlice
                   || anchor.Kind == SectorFixedAnchorKind.BoundaryWarning
                   || anchor.Kind == SectorFixedAnchorKind.SpecialEntryReturn
                   || anchor.Kind == SectorFixedAnchorKind.SpecialApronBuffer;
        }

        internal static HashSet<LocalTileCoord> BlockingCells(SectorFixedAnchorPlan plan, int sectorIndex)
        {
            var result = new HashSet<LocalTileCoord>();
            if (plan == null) return result;
            foreach (var anchor in plan.Anchors.Where(value => value.SectorIndex == sectorIndex && !IsCompatible(value)
                                                               && value.Kind != SectorFixedAnchorKind.ReferenceOnlyMarker))
            for (var y = anchor.Rect.Y; y < anchor.Rect.YMaxExclusive; y++)
            for (var x = anchor.Rect.X; x < anchor.Rect.XMaxExclusive; x++) result.Add(new LocalTileCoord(x, y));
            return result;
        }

        internal static bool Contains(SectorFixedAnchor anchor, LocalTileCoord coordinate)
            => coordinate.X >= anchor.Rect.X && coordinate.X < anchor.Rect.XMaxExclusive
               && coordinate.Y >= anchor.Rect.Y && coordinate.Y < anchor.Rect.YMaxExclusive;
    }
}
