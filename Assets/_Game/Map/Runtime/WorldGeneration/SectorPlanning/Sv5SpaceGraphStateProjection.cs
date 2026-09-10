using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    internal sealed class Sv5SpaceProjectionResult
    {
        public Sv5SpaceProjectionResult(IEnumerable<Sv5SpaceContactDecision> contacts,
            IEnumerable<Sv5SpaceGate> gates, IEnumerable<Sv5SpaceProjectionOrderProof> proofs,
            IEnumerable<Sv5SpaceGateStateCheck> gateStateChecks, IEnumerable<string> diagnostics)
        {
            Contacts = new ReadOnlyCollection<Sv5SpaceContactDecision>((contacts ??
                Array.Empty<Sv5SpaceContactDecision>()).OrderBy(value => value).ToArray());
            Gates = new ReadOnlyCollection<Sv5SpaceGate>((gates ?? Array.Empty<Sv5SpaceGate>())
                .OrderBy(value => value).ToArray());
            Proofs = new ReadOnlyCollection<Sv5SpaceProjectionOrderProof>((proofs ??
                Array.Empty<Sv5SpaceProjectionOrderProof>()).OrderBy(value => value).ToArray());
            GateStateChecks = new ReadOnlyCollection<Sv5SpaceGateStateCheck>((gateStateChecks ??
                Array.Empty<Sv5SpaceGateStateCheck>()).OrderBy(value => value).ToArray());
            Diagnostics = new ReadOnlyCollection<string>((diagnostics ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }
        public IReadOnlyList<Sv5SpaceContactDecision> Contacts { get; }
        public IReadOnlyList<Sv5SpaceGate> Gates { get; }
        public IReadOnlyList<Sv5SpaceProjectionOrderProof> Proofs { get; }
        public IReadOnlyList<Sv5SpaceGateStateCheck> GateStateChecks { get; }
        public IReadOnlyList<string> Diagnostics { get; }
    }

    /// <summary>
    /// Projects actual SV5_06 connectors into the existing RMAP13 finite-state
    /// machine.  Every mid-route contact becomes an action-less split node and
    /// every split segment repeats the source edge predicate, so an endpoint
    /// guard can never be bypassed by entering halfway through a route.
    /// </summary>
    public static class Sv5SpaceGraphStateProjection
    {
        private const ulong AllResources = 7;

        internal static Sv5SpaceProjectionResult Project(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections,
            IEnumerable<Sv5RouteContactPair> sourceContacts, bool contactCoverageVerified)
        {
            if (core == null) throw new ArgumentNullException(nameof(core));
            RmapWorldGraphPlan graph = core.RouteSource.Graph;
            Sv5SpaceConnection[] connections = (sourceConnections ?? Array.Empty<Sv5SpaceConnection>())
                .Where(value => value != null).OrderBy(value => value).ToArray();
            Sv5RouteContactPair[] contacts = (sourceContacts ?? Array.Empty<Sv5RouteContactPair>())
                .Where(value => value != null).OrderBy(value => value).ToArray();
            var diagnostics = new List<string>();
            var coreConnections = connections.Where(value => value.Kind == Sv5SpaceConnectionKind.CoreProgression)
                .ToDictionary(value => value.SourceGraphEdgeId, value => value, StringComparer.Ordinal);
            foreach (RmapWorldGraphEdge edge in graph.Edges)
                if (!coreConnections.ContainsKey(edge.EdgeId)) diagnostics.Add("MISSING_ACTUAL_CORE_CONNECTOR|" + edge.EdgeId);

            var splitNodeIds = contacts.ToDictionary(value => value.Id,
                value => ContactNodeId(value), StringComparer.Ordinal);
            var analysisNodeIds = new HashSet<string>(StringComparer.Ordinal);
            var optionalNodeIds = new HashSet<string>(StringComparer.Ordinal);
            string startNodeId = graph.Nodes.Single(value => value.Role == RmapWorldGraphRole.Start).NodeId;
            foreach (Sv5SpaceConnection connection in connections.Where(value => value.Kind !=
                         Sv5SpaceConnectionKind.CoreProgression))
            {
                AddOptionalNode(connection.FromPlaceId);
                AddOptionalNode(connection.ToPlaceId);
            }
            analysisNodeIds.UnionWith(optionalNodeIds);

            var logicalRoutes = new Dictionary<string, LogicalRoute>(StringComparer.Ordinal);
            foreach (RmapWorldGraphEdge edge in graph.Edges)
                if (coreConnections.TryGetValue(edge.EdgeId, out Sv5SpaceConnection connection))
                    logicalRoutes.Add(edge.EdgeId, new LogicalRoute(edge.EdgeId, connection, edge, true));
            foreach (Sv5SpaceConnection connection in connections.Where(value => value.Kind !=
                         Sv5SpaceConnectionKind.CoreProgression))
            {
                string from = NodeId(connection.FromPlaceId, startNodeId);
                string to = NodeId(connection.ToPlaceId, startNodeId);
                if (!string.Equals(from, startNodeId, StringComparison.Ordinal) && !analysisNodeIds.Contains(from))
                    diagnostics.Add("UNKNOWN_OPTIONAL_FROM|" + connection.Id);
                if (!string.Equals(to, startNodeId, StringComparison.Ordinal) && !analysisNodeIds.Contains(to))
                    diagnostics.Add("UNKNOWN_OPTIONAL_TO|" + connection.Id);
                logicalRoutes.Add(connection.Id, new LogicalRoute(connection.Id, connection,
                    new RmapWorldGraphEdge(from, to, connection.Direction, "SV5_06_" + connection.Condition,
                        connection.Id, true), false));
            }
            foreach (Sv5RouteContactPair contact in contacts)
                if (!logicalRoutes.ContainsKey(contact.RouteA) || !logicalRoutes.ContainsKey(contact.RouteB))
                    diagnostics.Add("UNKNOWN_CONTACT_ROUTE|" + contact.Id);
            Sv5RouteContactPair[] joinedContacts = contacts.Where(value =>
                logicalRoutes.TryGetValue(value.RouteA, out LogicalRoute left) &&
                logicalRoutes.TryGetValue(value.RouteB, out LogicalRoute right) &&
                SamePredicate(left.Edge, right.Edge)).ToArray();
            analysisNodeIds.UnionWith(joinedContacts.Select(value => splitNodeIds[value.Id]));

            IReadOnlyList<Sv5SpaceGate> gates = Sv5SpaceGateGeometry.Build(core, connections, contacts);
            IReadOnlyList<Sv5SpaceGateStateCheck> gateStateChecks = Sv5SpaceGateGeometry.BuildChecks(core,
                connections, gates);
            diagnostics.AddRange(Sv5SpaceGateGeometry.FindStateErrors(core, connections, gates, gateStateChecks));
            var gateByConnection = gates.ToDictionary(value => value.SourceConnectionId, value => value,
                StringComparer.Ordinal);
            List<RmapWorldGraphEdge> projectedEdges = BuildSplitEdges(core, logicalRoutes, joinedContacts,
                splitNodeIds, gateByConnection, diagnostics).ToList();

            RmapWorldGraphRole[][] orders = ResourceOrders().ToArray();
            var proofs = new List<Sv5SpaceProjectionOrderProof>();
            foreach (RmapWorldGraphRole[] order in orders)
            {
                RmapWorldGraphProof goalProof = RmapWorldGraphPlanner.EvaluateWithAnalysisNodes(graph.Nodes,
                    analysisNodeIds, projectedEdges, order);
                RmapWorldGraphExploration exploration = RmapWorldGraphPlanner.ExploreWithAnalysisNodes(graph.Nodes,
                    analysisNodeIds, projectedEdges, order);
                HashSet<RmapWorldGraphState> goals = new HashSet<RmapWorldGraphState>(exploration.States.Where(value =>
                    IsGoal(graph, value, order.Length)));
                HashSet<RmapWorldGraphState> reverse = ReverseReachable(exploration, goals);
                string[] deadEnds = exploration.States.Where(value => !reverse.Contains(value))
                    .Select(value => value.StableToken).OrderBy(value => value, StringComparer.Ordinal).ToArray();
                proofs.Add(new Sv5SpaceProjectionOrderProof(goalProof, exploration.States.Count,
                    exploration.Transitions.Count, reverse.Count, deadEnds));
            }

            bool checkedState = contactCoverageVerified && diagnostics.Count == 0 && proofs.Count == 6 &&
                proofs.All(value => value.Success);
            var decisions = new List<Sv5SpaceContactDecision>();
            foreach (Sv5RouteContactPair contact in contacts)
            {
                LogicalRoute leftRoute = null;
                LogicalRoute rightRoute = null;
                bool known = logicalRoutes.TryGetValue(contact.RouteA, out leftRoute) &&
                    logicalRoutes.TryGetValue(contact.RouteB, out rightRoute);
                RmapWorldGraphEdge left = known ? leftRoute.Edge : null;
                RmapWorldGraphEdge right = known ? rightRoute.Edge : null;
                string predicate = known ? PredicatePair(left, right) : "UNKNOWN";
                bool samePredicate = known && SamePredicate(left, right);
                Sv5SpaceGate boundaryGate = known && !samePredicate ? SelectBoundaryGate(contact, gates) : null;
                Sv5SpaceCrossingKind crossing = !known || !checkedState ? Sv5SpaceCrossingKind.Pending :
                    samePredicate ? Sv5SpaceCrossingKind.Join : boundaryGate != null ?
                    Sv5SpaceCrossingKind.ConditionalGate : Sv5SpaceCrossingKind.Pending;
                string boundaryId = boundaryGate == null ? string.Empty : boundaryGate.BoundaryId;
                decisions.Add(new Sv5SpaceContactDecision(contact, splitNodeIds[contact.Id], crossing, predicate,
                    boundaryId, contactCoverageVerified, known && checkedState, known && checkedState ?
                    (crossing == Sv5SpaceCrossingKind.Join ?
                    "The compatible pair is one global world-cell/face join; Clearance remains non-traversable." :
                    crossing == Sv5SpaceCrossingKind.ConditionalGate ?
                    "The incompatible pair remains visible in the global coordinate graph and is covered by the typed global face-cut boundary " + boundaryId + "." :
                    "The incompatible pair has no verifiable global boundary.") :
                    "The contact could not be accepted by the shared finite-state projection."));
            }
            return new Sv5SpaceProjectionResult(decisions, gates, proofs, gateStateChecks, diagnostics);

            void AddOptionalNode(string placeId)
            {
                string node = NodeId(placeId, startNodeId);
                if (!string.Equals(node, startNodeId, StringComparison.Ordinal)) optionalNodeIds.Add(node);
            }

            Sv5SpaceGate SelectBoundaryGate(Sv5RouteContactPair contact, IEnumerable<Sv5SpaceGate> sourceGates)
            {
                return (sourceGates ?? Array.Empty<Sv5SpaceGate>()).Where(value =>
                        string.Equals(value.SourceRouteId, contact.RouteA, StringComparison.Ordinal) ||
                        string.Equals(value.SourceRouteId, contact.RouteB, StringComparison.Ordinal))
                    .OrderByDescending(value => PredicateRank(value.TypedPredicate))
                    .ThenBy(value => value.SideAAnchor).ThenBy(value => value.Id, StringComparer.Ordinal)
                    .FirstOrDefault();
            }
        }

        private static IEnumerable<RmapWorldGraphEdge> BuildSplitEdges(Sv5CoreReservationPlan core,
            IReadOnlyDictionary<string, LogicalRoute> logicalRoutes,
            IEnumerable<Sv5RouteContactPair> contacts, IReadOnlyDictionary<string, string> splitNodeIds,
            IReadOnlyDictionary<string, Sv5SpaceGate> gateByConnection, ICollection<string> diagnostics)
        {
            foreach (LogicalRoute route in logicalRoutes.Values.OrderBy(value => value.RouteKey, StringComparer.Ordinal))
            {
                RmapWorldGraphEdge edge = route.Edge;
                gateByConnection.TryGetValue(route.Connection.Id, out Sv5SpaceGate routeGate);
                var splits = new List<RouteSplit>();
                foreach (Sv5RouteContactPair contact in contacts.Where(value => value.RouteA == route.RouteKey ||
                             value.RouteB == route.RouteKey))
                {
                    int ordinal = ContactOrdinal(core, route, contact);
                    if (ordinal < 0)
                    {
                        diagnostics.Add("CONTACT_NOT_ON_ROUTE|" + contact.Id + "|" + route.RouteKey);
                        continue;
                    }
                    splits.Add(new RouteSplit(ordinal, contact.Id, splitNodeIds[contact.Id]));
                }
                string previous = edge.SourceNodeId;
                var segment = 0;
                RouteSplit[] ordered = splits.GroupBy(value => value.NodeId, StringComparer.Ordinal)
                    .Select(value => value.OrderBy(item => item.Ordinal).ThenBy(item => item.ContactId,
                        StringComparer.Ordinal).First()).OrderBy(value => value.Ordinal)
                    .ThenBy(value => value.ContactId, StringComparer.Ordinal).ToArray();
                foreach (RouteSplit split in ordered)
                {
                    yield return Segment(edge, route.Connection.Id, routeGate, segment++, previous, split.NodeId);
                    previous = split.NodeId;
                }
                yield return Segment(edge, route.Connection.Id, routeGate, segment, previous, edge.TargetNodeId);
                if (string.Equals(route.Connection.Flow, "BIDIRECTIONAL", StringComparison.Ordinal))
                {
                    previous = edge.TargetNodeId;
                    segment = 0;
                    foreach (RouteSplit split in ordered.Reverse())
                    {
                        yield return Segment(edge, route.Connection.Id + "|REVERSE", routeGate, segment++, previous, split.NodeId,
                            Opposite(source: edge.Direction));
                        previous = split.NodeId;
                    }
                    yield return Segment(edge, route.Connection.Id + "|REVERSE", routeGate, segment, previous,
                        edge.SourceNodeId, Opposite(source: edge.Direction));
                }
            }
        }

        private static RmapWorldGraphEdge Segment(RmapWorldGraphEdge source, string connectionId,
            Sv5SpaceGate routeGate, int ordinal,
            string from, string to, RmapWorldGraphDirection? direction = null) => new RmapWorldGraphEdge(from, to,
            direction ?? source.Direction,
            source.TraversalCondition, connectionId + (routeGate == null ? string.Empty : "|GATE|" + routeGate.Id) +
            "|SEGMENT|" + ordinal.ToString(CultureInfo.InvariantCulture), true,
            source.RequiredResourceMask, source.RequiresForge, source.RequiresSeal, source.RequiresBossComplete);

        private static int ContactOrdinal(Sv5CoreReservationPlan core, LogicalRoute route,
            Sv5RouteContactPair contact)
        {
            RmapSpecialWorldPoint world = string.Equals(contact.RouteA, route.RouteKey,
                StringComparison.Ordinal) ? contact.RouteAWorld : contact.RouteBWorld;
            if (!route.Connection.Envelope.Contains(world)) return -1;
            int best = -1, bestDistance = int.MaxValue;
            for (var index = 0; index < route.Connection.Centerline.Count; index++)
            {
                int distance = Math.Abs(route.Connection.Centerline[index].X - world.X) +
                    Math.Abs(route.Connection.Centerline[index].Y - world.Y);
                if (distance < bestDistance) { best = index; bestDistance = distance; }
            }
            if (bestDistance <= 1) return best;
            return -1;
        }

        public static IReadOnlyList<string> ValidateAcceptedProjection(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections)
        {
            if (core == null) throw new ArgumentNullException(nameof(core));
            Sv5SpaceConnection[] connections = (sourceConnections ?? Array.Empty<Sv5SpaceConnection>())
                .Where(value => value != null).ToArray();
            IReadOnlyList<Sv5RouteContactCell> cells = Sv5SpaceGraphValidator.AcceptedContactCells(core, connections);
            IReadOnlyList<Sv5RouteContactPair> contacts = Sv5RouteStatePolicy.EnumerateContactPairs(cells);
            bool covered = Sv5SpaceGraphValidator.FindContactCoverageErrors(cells, contacts).Count == 0;
            Sv5SpaceProjectionResult result = Project(core, connections, contacts, covered);
            var errors = new List<string>(result.Diagnostics);
            if (result.Proofs.Count != 6 || result.Proofs.Any(value => !value.Success))
                errors.Add("PROJECTION_STATE_SAFETY_FAILED");
            errors.AddRange(Sv5SpaceGraphValidator.FindGateErrors(result.Contacts, result.Gates));
            errors.AddRange(Sv5SpaceGateGeometry.FindStateErrors(core, connections, result.Gates,
                result.GateStateChecks));
            return new ReadOnlyCollection<string>(errors.Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        private static HashSet<RmapWorldGraphState> ReverseReachable(RmapWorldGraphExploration exploration,
            IEnumerable<RmapWorldGraphState> sourceGoals)
        {
            var reverse = exploration.Transitions.GroupBy(value => value.After)
                .ToDictionary(value => value.Key, value => value.Select(item => item.Before).Distinct().ToArray());
            var output = new HashSet<RmapWorldGraphState>(sourceGoals ?? Array.Empty<RmapWorldGraphState>());
            var queue = new Queue<RmapWorldGraphState>(output);
            while (queue.Count != 0)
            {
                RmapWorldGraphState state = queue.Dequeue();
                if (!reverse.TryGetValue(state, out RmapWorldGraphState[] previous)) continue;
                foreach (RmapWorldGraphState value in previous) if (output.Add(value)) queue.Enqueue(value);
            }
            return output;
        }

        private static bool IsGoal(RmapWorldGraphPlan graph, RmapWorldGraphState state, int orderLength) => state != null &&
            string.Equals(state.PositionNodeId, graph.Nodes.Single(value => value.Role == RmapWorldGraphRole.Exit).NodeId,
                StringComparison.Ordinal) && state.ResourceMask == AllResources && state.OrderCursor == orderLength &&
            state.ForgeMade && state.SealOpen && state.BossComplete;

        private static string NodeId(string placeId, string startNodeId) => placeId == "RMAP15_SITE_START" ? startNodeId :
            placeId == "RMAP15_SITE_VILLAGE" ? "SV5_ANALYSIS_VILLAGE" : placeId;
        private static bool IsGuarded(RmapWorldGraphEdge edge) => edge.RequiredResourceMask != 0 || edge.RequiresForge ||
            edge.RequiresSeal || edge.RequiresBossComplete;
        private static string PredicatePair(RmapWorldGraphEdge left, RmapWorldGraphEdge right)
        {
            string first = EdgePredicate(left), second = EdgePredicate(right);
            return string.Equals(first, second, StringComparison.Ordinal) ? first : "routeA{" + first +
                "};routeB{" + second + "}";
        }
        private static bool SamePredicate(RmapWorldGraphEdge left, RmapWorldGraphEdge right) => left != null &&
            right != null && left.RequiredResourceMask == right.RequiredResourceMask &&
            left.RequiresForge == right.RequiresForge && left.RequiresSeal == right.RequiresSeal &&
            left.RequiresBossComplete == right.RequiresBossComplete;
        private static int PredicateRank(Sv5SpaceGatePredicate value) => value.RequiresBossComplete ? 4 :
            value.RequiresSeal ? 3 : value.RequiresForge ? 2 : value.RequiredResourceMask != 0 ? 1 : 0;
        private static string EdgePredicate(RmapWorldGraphEdge edge) => edge.EdgeId + ":mask=" +
            edge.RequiredResourceMask.ToString(CultureInfo.InvariantCulture) + ",forge=" + (edge.RequiresForge ? "1" : "0") +
            ",seal=" + (edge.RequiresSeal ? "1" : "0") + ",boss=" + (edge.RequiresBossComplete ? "1" : "0");
        private static string ContactNodeId(Sv5RouteContactPair contact) => "SV5_CONTACT_NODE_" +
            RmapWorldDefinition.Hash(contact.Kind + "|" + contact.FirstWorld + "|" + contact.SecondWorld)
                .Substring(0, 20).ToUpperInvariant();
        private static RmapWorldGraphDirection Opposite(RmapWorldGraphDirection source) =>
            source == RmapWorldGraphDirection.Left ? RmapWorldGraphDirection.Right :
            source == RmapWorldGraphDirection.Right ? RmapWorldGraphDirection.Left :
            source == RmapWorldGraphDirection.Up ? RmapWorldGraphDirection.Down : RmapWorldGraphDirection.Up;
        private static IEnumerable<RmapWorldGraphRole[]> ResourceOrders()
        {
            RmapWorldGraphRole[] resources =
            {
                RmapWorldGraphRole.MooncoreOre,
                RmapWorldGraphRole.CondensedCoefficientSap,
                RmapWorldGraphRole.DeepStarYeast,
            };
            return resources.SelectMany(first => resources.Where(second => second != first).SelectMany(second =>
                resources.Where(third => third != first && third != second).Select(third => new[] { first, second, third })));
        }

        private sealed class RouteSplit
        {
            public RouteSplit(int ordinal, string contactId, string nodeId)
            { Ordinal = ordinal; ContactId = contactId; NodeId = nodeId; }
            public int Ordinal { get; }
            public string ContactId { get; }
            public string NodeId { get; }
        }

        private sealed class LogicalRoute
        {
            public LogicalRoute(string routeKey, Sv5SpaceConnection connection, RmapWorldGraphEdge edge, bool isCore)
            { RouteKey = routeKey; Connection = connection; Edge = edge; IsCore = isCore; }
            public string RouteKey { get; }
            public Sv5SpaceConnection Connection { get; }
            public RmapWorldGraphEdge Edge { get; }
            public bool IsCore { get; }
        }
    }
}
