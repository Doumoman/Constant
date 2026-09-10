using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    internal sealed class Sv5SpaceProjectionResult
    {
        public Sv5SpaceProjectionResult(IEnumerable<Sv5SpaceContactDecision> contacts,
            IEnumerable<Sv5SpaceGate> gates, IEnumerable<Sv5SpaceProjectionOrderProof> proofs,
            IEnumerable<string> diagnostics)
        {
            Contacts = new ReadOnlyCollection<Sv5SpaceContactDecision>((contacts ??
                Array.Empty<Sv5SpaceContactDecision>()).OrderBy(value => value).ToArray());
            Gates = new ReadOnlyCollection<Sv5SpaceGate>((gates ?? Array.Empty<Sv5SpaceGate>())
                .OrderBy(value => value).ToArray());
            Proofs = new ReadOnlyCollection<Sv5SpaceProjectionOrderProof>((proofs ??
                Array.Empty<Sv5SpaceProjectionOrderProof>()).OrderBy(value => value).ToArray());
            Diagnostics = new ReadOnlyCollection<string>((diagnostics ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }
        public IReadOnlyList<Sv5SpaceContactDecision> Contacts { get; }
        public IReadOnlyList<Sv5SpaceGate> Gates { get; }
        public IReadOnlyList<Sv5SpaceProjectionOrderProof> Proofs { get; }
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
            IEnumerable<Sv5RouteContactPair> sourceContacts)
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
                value => "SV5_CONTACT_NODE_" + value.Id, StringComparer.Ordinal);
            var analysisNodeIds = new HashSet<string>(splitNodeIds.Values, StringComparer.Ordinal);
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

            List<RmapWorldGraphEdge> projectedEdges = BuildSplitEdges(core, logicalRoutes, contacts,
                splitNodeIds, diagnostics).ToList();

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

            bool checkedState = diagnostics.Count == 0 && proofs.Count == 6 && proofs.All(value => value.Success);
            var decisions = new List<Sv5SpaceContactDecision>();
            var gates = new List<Sv5SpaceGate>();
            foreach (Sv5RouteContactPair contact in contacts)
            {
                LogicalRoute leftRoute = null;
                LogicalRoute rightRoute = null;
                bool known = logicalRoutes.TryGetValue(contact.RouteA, out leftRoute) &&
                    logicalRoutes.TryGetValue(contact.RouteB, out rightRoute);
                RmapWorldGraphEdge left = known ? leftRoute.Edge : null;
                RmapWorldGraphEdge right = known ? rightRoute.Edge : null;
                string predicate = known ? CombinedPredicate(left, right) : "UNKNOWN";
                bool guarded = known && (IsGuarded(left) || IsGuarded(right));
                Sv5SpaceCrossingKind crossing = !known || !checkedState ? Sv5SpaceCrossingKind.Pending :
                    guarded ? Sv5SpaceCrossingKind.ConditionalGate : Sv5SpaceCrossingKind.Join;
                decisions.Add(new Sv5SpaceContactDecision(contact, splitNodeIds[contact.Id], crossing, predicate,
                    known && checkedState, known && checkedState ?
                    "The complete pair is a shared split node evaluated by RMAP13; every outgoing segment repeats its source predicate." :
                    "The contact could not be accepted by the shared finite-state projection."));
                if (crossing == Sv5SpaceCrossingKind.ConditionalGate)
                    gates.Add(new Sv5SpaceGate("SV5_GATE_" + contact.Id, contact.Id, contact.FirstWorld,
                        predicate, crossing, "SEALED_BLOCKS_ROUTE_SWITCH", "OPEN_PRESERVES_PREDICATE_SEGMENTS"));
            }
            return new Sv5SpaceProjectionResult(decisions, gates, proofs, diagnostics);

            void AddOptionalNode(string placeId)
            {
                string node = NodeId(placeId, startNodeId);
                if (!string.Equals(node, startNodeId, StringComparison.Ordinal)) optionalNodeIds.Add(node);
            }
        }

        private static IEnumerable<RmapWorldGraphEdge> BuildSplitEdges(Sv5CoreReservationPlan core,
            IReadOnlyDictionary<string, LogicalRoute> logicalRoutes,
            IEnumerable<Sv5RouteContactPair> contacts, IReadOnlyDictionary<string, string> splitNodeIds,
            ICollection<string> diagnostics)
        {
            foreach (LogicalRoute route in logicalRoutes.Values.OrderBy(value => value.RouteKey, StringComparer.Ordinal))
            {
                RmapWorldGraphEdge edge = route.Edge;
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
                foreach (RouteSplit split in splits.OrderBy(value => value.Ordinal).ThenBy(value => value.ContactId,
                             StringComparer.Ordinal))
                {
                    yield return Segment(edge, route.Connection.Id, segment++, previous, split.NodeId);
                    previous = split.NodeId;
                }
                yield return Segment(edge, route.Connection.Id, segment, previous, edge.TargetNodeId);
            }
        }

        private static RmapWorldGraphEdge Segment(RmapWorldGraphEdge source, string connectionId, int ordinal,
            string from, string to) => new RmapWorldGraphEdge(from, to, source.Direction,
            source.TraversalCondition, connectionId + "|SEGMENT|" + ordinal.ToString(CultureInfo.InvariantCulture), true,
            source.RequiredResourceMask, source.RequiresForge, source.RequiresSeal, source.RequiresBossComplete);

        private static int ContactOrdinal(Sv5CoreReservationPlan core, LogicalRoute route,
            Sv5RouteContactPair contact)
        {
            if (route.IsCore)
            {
                Sv5CoreRouteCellReservation[] candidates = core.RouteCells.Where(value =>
                        value.RouteId == route.RouteKey && (value.World.Equals(contact.FirstWorld) ||
                        value.World.Equals(contact.SecondWorld))).OrderBy(value => value.Ordinal).ToArray();
                if (candidates.Length != 0) return candidates[0].Ordinal + 1;
            }
            for (var index = 0; index < route.Connection.Centerline.Count; index++)
                if (route.Connection.Centerline[index].Equals(contact.FirstWorld) ||
                    route.Connection.Centerline[index].Equals(contact.SecondWorld)) return index;
            return -1;
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
        private static string CombinedPredicate(RmapWorldGraphEdge left, RmapWorldGraphEdge right) =>
            EdgePredicate(left) + " || " + EdgePredicate(right);
        private static string EdgePredicate(RmapWorldGraphEdge edge) => edge.EdgeId + ":mask=" +
            edge.RequiredResourceMask.ToString(CultureInfo.InvariantCulture) + ",forge=" + (edge.RequiresForge ? "1" : "0") +
            ",seal=" + (edge.RequiresSeal ? "1" : "0") + ",boss=" + (edge.RequiresBossComplete ? "1" : "0");
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
