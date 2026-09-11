using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum Sv5SpaceSegmentKind { REGION = 1, PORTAL = 2 }

    /// <summary>Immutable corridor ownership record used by FIX04's portal-first topology.</summary>
    public sealed class Sv5SpaceSegment : IComparable<Sv5SpaceSegment>
    {
        internal Sv5SpaceSegment(string id, string connectionId, string regionId,
            Sv5SpaceSegmentKind kind, IEnumerable<RmapSpecialWorldPoint> centerline,
            IEnumerable<RmapSpecialWorldPoint> aperture, RmapSpecialWorldPoint source,
            RmapSpecialWorldPoint target, string gateId, Sv5SpaceGatePredicate predicate)
        {
            Id = id; ConnectionId = connectionId; RegionId = regionId; Kind = kind;
            Centerline = new ReadOnlyCollection<RmapSpecialWorldPoint>((centerline ?? Array.Empty<RmapSpecialWorldPoint>()).ToArray());
            ApertureCells = new ReadOnlyCollection<RmapSpecialWorldPoint>((aperture ?? Array.Empty<RmapSpecialWorldPoint>()).Distinct().OrderBy(v => v).ToArray());
            Source = source; Target = target; GateId = gateId ?? string.Empty; Predicate = predicate;
        }
        public string Id { get; }
        public string ConnectionId { get; }
        public string RegionId { get; }
        public Sv5SpaceSegmentKind Kind { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> Centerline { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> ApertureCells { get; }
        public RmapSpecialWorldPoint Source { get; }
        public RmapSpecialWorldPoint Target { get; }
        public string GateId { get; }
        public Sv5SpaceGatePredicate Predicate { get; }
        public int CompareTo(Sv5SpaceSegment other) => other == null ? 1 : string.Compare(Id, other.Id, StringComparison.Ordinal);
    }

    public sealed class Sv5SpacePhysicalTransition
    {
        internal Sv5SpacePhysicalTransition(string order, RmapWorldGraphState state,
            Sv5SpaceConnection connection, RmapWorldGraphEdge edge, bool reverse,
            Sv5SpacePhysicalReachability reach, IEnumerable<Sv5SpaceGate> gates)
        {
            ResourceOrder = order; State = state; Connection = connection; Edge = edge; Reverse = reverse;
            ExpectedOpen = edge.CanTraverse(state); Reachability = reach;
            ClosedGateIds = Freeze(gates.Where(g => !g.TypedPredicate.IsOpen(state.ResourceMask,
                state.ForgeMade, state.SealOpen, state.BossComplete)).Select(g => g.Id));
            OpenGateIds = Freeze(gates.Where(g => g.TypedPredicate.IsOpen(state.ResourceMask,
                state.ForgeMade, state.SealOpen, state.BossComplete)).Select(g => g.Id));
            WitnessId = reach.WitnessId;
        }
        public string ResourceOrder { get; }
        public RmapWorldGraphState State { get; }
        public Sv5SpaceConnection Connection { get; }
        public RmapWorldGraphEdge Edge { get; }
        public bool Reverse { get; }
        public bool ExpectedOpen { get; }
        public Sv5SpacePhysicalReachability Reachability { get; }
        public IReadOnlyList<string> ClosedGateIds { get; }
        public IReadOnlyList<string> OpenGateIds { get; }
        public string WitnessId { get; }
        public bool Success => Reachability.SourceAnchorReachable &&
            ExpectedOpen == Reachability.TargetPortReachable;
        internal string StableToken => ResourceOrder + "|" + State.StableToken + "|" +
            Connection.FromPortId + ">" + Connection.ToPortId + "|" + Edge.Direction + "|" + Connection.Flow +
            "|" + Reverse + "|" + Sv5SpaceGatePredicate.FromEdge(Edge).StableToken + "|" + ExpectedOpen +
            "|" + Reachability.TargetPortReachable + "|" + string.Join(";", ClosedGateIds) + "|" +
            string.Join(";", OpenGateIds) + "|" + WitnessId;
        private static ReadOnlyCollection<string> Freeze(IEnumerable<string> values) =>
            new ReadOnlyCollection<string>(values.OrderBy(v => v, StringComparer.Ordinal).ToArray());
    }

    public sealed class Sv5SpacePhysicalProductPlan
    {
        internal Sv5SpacePhysicalProductPlan(IEnumerable<Sv5SpacePhysicalTransition> matrix,
            IEnumerable<Sv5SpaceProjectionOrderProof> proofs, IEnumerable<string> diagnostics,
            string graphDigest)
        {
            Matrix = new ReadOnlyCollection<Sv5SpacePhysicalTransition>(matrix.OrderBy(v => v.ResourceOrder,
                StringComparer.Ordinal).ThenBy(v => v.State).ThenBy(v => v.Connection).ThenBy(v => v.Reverse).ToArray());
            Proofs = new ReadOnlyCollection<Sv5SpaceProjectionOrderProof>(proofs.OrderBy(v => v).ToArray());
            Diagnostics = new ReadOnlyCollection<string>(diagnostics.Distinct(StringComparer.Ordinal)
                .OrderBy(v => v, StringComparer.Ordinal).ToArray());
            SemanticDigest = RmapWorldDefinition.Hash("SV5_PHYSICAL_FSM_PRODUCT_FIX04_V1\n" + graphDigest + "\n" +
                string.Join("\n", Matrix.Select(v => v.StableToken)) + "\n" +
                string.Join("\n", Proofs.Select(p => p.GoalProof.ProofId + "|" + p.ReachableStates + "|" +
                    p.Transitions + "|" + p.ReverseReachableStates + "|" + string.Join(";", p.DeadEnds))) + "\n" +
                string.Join("\n", Diagnostics));
        }
        public IReadOnlyList<Sv5SpacePhysicalTransition> Matrix { get; }
        public IReadOnlyList<Sv5SpaceProjectionOrderProof> Proofs { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public string SemanticDigest { get; }
        public bool Success => Matrix.Count > 0 && Matrix.All(v => v.Success) &&
            Proofs.Count == 6 && Proofs.All(v => v.Success) && Diagnostics.Count == 0;
    }

    /// <summary>RMAP13 supplies every action and legal state. Movement transitions survive only when
    /// the same state's global coordinate graph, with all gates active, supplies a physical witness.</summary>
    public static class Sv5SpacePhysicalProduct
    {
        public static IReadOnlyList<Sv5SpaceSegment> BuildSegments(
            IEnumerable<Sv5SpaceConnection> sourceConnections, IEnumerable<Sv5SpaceGate> sourceGates)
        {
            Sv5SpaceGate[] gates = (sourceGates ?? Array.Empty<Sv5SpaceGate>()).Where(v => v != null).ToArray();
            var output = new List<Sv5SpaceSegment>();
            foreach (Sv5SpaceConnection connection in (sourceConnections ?? Array.Empty<Sv5SpaceConnection>()).Where(v => v != null).OrderBy(v => v))
            {
                Sv5SpaceGate gate = gates.FirstOrDefault(v => v.SourceConnectionId == connection.Id);
                var line = connection.Centerline.ToArray();
                if (gate == null) Add(0, line, Sv5SpaceSegmentKind.REGION, null);
                else
                {
                    var cuts = Enumerable.Range(1, line.Length - 1).Where(i => gate.BlockingFaces.Any(f =>
                        f.StableToken == new Sv5SpaceBoundaryFace(line[i - 1], line[i]).StableToken)).ToArray();
                    if (cuts.Length != 1 || gate.BlockingFaces.Count != 1 || gate.BlockingCells.Count != 0)
                        throw new InvalidOperationException("GATE_FACE_OUTSIDE_PORTAL|" + gate.Id);
                    int cut = cuts[0];
                    Add(0, line.Take(cut).ToArray(), Sv5SpaceSegmentKind.REGION, null);
                    Add(1, new[] { line[cut - 1], line[cut] }, Sv5SpaceSegmentKind.PORTAL, gate);
                    Add(2, line.Skip(cut).ToArray(), Sv5SpaceSegmentKind.REGION, null);
                }
                void Add(int index, RmapSpecialWorldPoint[] cells, Sv5SpaceSegmentKind kind, Sv5SpaceGate owner)
                {
                    output.Add(new Sv5SpaceSegment(connection.Id + "|SEG_" + index,
                        connection.Id, kind == Sv5SpaceSegmentKind.PORTAL ? "EXACT_NECK" : "CORRIDOR",
                        kind, cells, connection.ApertureCells.Where(cells.Contains),
                        cells.First(), cells.Last(), owner?.Id, owner?.TypedPredicate));
                }
            }
            return new ReadOnlyCollection<Sv5SpaceSegment>(output.OrderBy(v => v).ToArray());
        }

        public static Sv5SpacePhysicalProductPlan Analyze(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections, IEnumerable<Sv5SpaceGate> sourceGates)
        {
            if (core == null) throw new ArgumentNullException(nameof(core));
            Sv5SpaceConnection[] connections = sourceConnections.Where(v => v != null).OrderBy(v => v).ToArray();
            Sv5SpaceGate[] gates = sourceGates.Where(v => v != null).OrderBy(v => v).ToArray();
            RmapWorldGraphPlan graph = core.RouteSource.Graph;
            var diagnostics = new List<string>();
            var bindings = new List<Binding>();
            var analysisNodes = new HashSet<string>(StringComparer.Ordinal);
            string start = graph.Nodes.Single(n => n.Role == RmapWorldGraphRole.Start).NodeId;
            string exit = graph.Nodes.Single(n => n.Role == RmapWorldGraphRole.Exit).NodeId;
            foreach (RmapWorldGraphEdge edge in graph.Edges)
            {
                Sv5SpaceConnection[] matches = connections.Where(c => c.Kind == Sv5SpaceConnectionKind.CoreProgression &&
                    c.SourceGraphEdgeId == edge.EdgeId).ToArray();
                if (matches.Length != 1)
                    diagnostics.Add("MISSING_ACTUAL_CORE_CONNECTOR|" + edge.EdgeId);
                else bindings.Add(new Binding(matches[0], edge, false));
            }
            foreach (Sv5SpaceConnection connection in connections.Where(c => c.Kind != Sv5SpaceConnectionKind.CoreProgression))
            {
                string from = Node(connection.FromPlaceId), to = Node(connection.ToPlaceId);
                if (from != start) analysisNodes.Add(from);
                if (to != start) analysisNodes.Add(to);
                bindings.Add(new Binding(connection, new RmapWorldGraphEdge(from, to, connection.Direction,
                    connection.Condition, connection.Id, true), false));
                if (connection.Flow == "BIDIRECTIONAL")
                    bindings.Add(new Binding(connection, new RmapWorldGraphEdge(to, from, Opposite(connection.Direction),
                        connection.Condition, connection.Id + "|REVERSE", true), true));
                else diagnostics.Add("CONNECTION_OPTIONAL_NOT_BIDIRECTIONAL|" + connection.Id);
            }
            var cache = new Dictionary<string, Sv5SpacePhysicalReachability>(StringComparer.Ordinal);
            var matrix = new List<Sv5SpacePhysicalTransition>();
            var proofs = new List<Sv5SpaceProjectionOrderProof>();
            var bindingByEdge = bindings.ToDictionary(b => b.Edge.EdgeId, StringComparer.Ordinal);
            foreach (RmapWorldGraphRole[] order in ResourceOrders())
            {
                string orderId = string.Join(">", order.Select(v => v.ToString()));
                // RMAP13 alone supplies legal actions, their ordering and canonical movement.
                // Filter its transition graph with physical witnesses, then search the product
                // from INITIAL. Never import a logical proof as a physical completion proof.
                RmapWorldGraphExploration logical = RmapWorldGraphPlanner.ExploreWithAnalysisNodes(
                    graph.Nodes, analysisNodes, bindings.Select(b => b.Edge), order);
                var outgoing = logical.Transitions.GroupBy(t => t.Before)
                    .ToDictionary(g => g.Key, g => g.OrderBy(t => t).ToArray());
                var initial = new RmapWorldGraphState(start, 0, 0, false, false, false);
                var visited = new HashSet<RmapWorldGraphState> { initial };
                var previous = new Dictionary<RmapWorldGraphState, RmapWorldGraphTransition>();
                var queue = new Queue<RmapWorldGraphState>();
                var transitions = new List<RmapWorldGraphTransition>();
                // Test all canonically reachable states, including states a defective
                // physical cut would prevent this candidate from reaching.
                foreach (var state in logical.States)
                foreach (Binding binding in bindings)
                {
                    var row = new Sv5SpacePhysicalTransition(orderId, state, binding.Connection,
                        binding.Edge, binding.Reverse, Reach(binding, state), gates);
                    matrix.Add(row);
                    if (!row.Success) diagnostics.Add((row.ExpectedOpen ?
                        "PRODUCT_OPEN_TRANSITION_BLOCKED|" : "PRODUCT_CLOSED_TRANSITION_BYPASS|") +
                        state.StableToken + "|" + binding.Connection.Id + "|" + binding.Reverse);
                }
                queue.Enqueue(initial);
                while (queue.Count != 0)
                {
                    RmapWorldGraphState state = queue.Dequeue();
                    if (!outgoing.TryGetValue(state, out RmapWorldGraphTransition[] next)) continue;
                    foreach (RmapWorldGraphTransition transition in next)
                    {
                        if (transition.Action.StartsWith("MOVE|", StringComparison.Ordinal))
                        {
                            Binding binding = bindingByEdge[transition.Action.Substring(5)];
                            if (!Reach(binding, state).TargetPortReachable) continue;
                        }
                        transitions.Add(transition);
                        if (!visited.Add(transition.After)) continue;
                        previous.Add(transition.After, transition);
                        queue.Enqueue(transition.After);
                    }
                }
                var goals = new HashSet<RmapWorldGraphState>(visited.Where(s => s.PositionNodeId == exit &&
                    s.ResourceMask == 7 && s.OrderCursor == order.Length && s.ForgeMade && s.SealOpen && s.BossComplete));
                var reverse = new HashSet<RmapWorldGraphState>(goals);
                var incoming = transitions.GroupBy(t => t.After).ToDictionary(g => g.Key, g => g.ToArray());
                foreach (RmapWorldGraphState goal in goals.OrderBy(s => s)) queue.Enqueue(goal);
                while (queue.Count != 0)
                {
                    RmapWorldGraphState state = queue.Dequeue();
                    if (!incoming.TryGetValue(state, out RmapWorldGraphTransition[] before)) continue;
                    foreach (var transition in before)
                        if (reverse.Add(transition.Before)) queue.Enqueue(transition.Before);
                }
                string[] deadEnds = visited.Where(s => !reverse.Contains(s)).Select(s => s.StableToken).ToArray();
                RmapWorldGraphState final = goals.OrderBy(s => s).FirstOrDefault();
                var actions = new List<string>();
                for (var cursor = final; cursor != null && previous.TryGetValue(cursor, out var step); cursor = step.Before)
                    actions.Add(step.Action);
                actions.Reverse();
                var failures = final == null ? new[] { new RmapWorldGraphFailure(
                    "PHYSICAL_GOAL_UNREACHABLE", initial, orderId) } : Array.Empty<RmapWorldGraphFailure>();
                var proof = new RmapWorldGraphProof(order, final, actions, failures);
                proofs.Add(new Sv5SpaceProjectionOrderProof(proof, visited.Count, transitions.Count, reverse.Count, deadEnds));
                if (deadEnds.Length != 0) diagnostics.Add("PRODUCT_UNINTENDED_DEAD_END|" + orderId + "|" + deadEnds.Length);
            }
            return new Sv5SpacePhysicalProductPlan(matrix, proofs, diagnostics, graph.Digest);

            string Node(string place) => place == "RMAP15_SITE_START" ? start :
                place == "RMAP15_SITE_VILLAGE" ? "SV5_ANALYSIS_VILLAGE" : place;
            Sv5SpacePhysicalReachability Reach(Binding binding, RmapWorldGraphState state)
            {
                string key = binding.Connection.Id + "|" + binding.Reverse + "|" +
                    string.Join(";", gates.Where(g => !g.TypedPredicate.IsOpen(state.ResourceMask,
                        state.ForgeMade, state.SealOpen, state.BossComplete)).Select(g => g.Id));
                if (!cache.TryGetValue(key, out Sv5SpacePhysicalReachability reach))
                {
                    reach = Sv5SpacePhysicalMovement.Evaluate(core, connections, gates, binding.Connection.Id,
                        state.ResourceMask, state.ForgeMade, state.SealOpen, state.BossComplete, binding.Reverse);
                    cache.Add(key, reach);
                }
                return reach;
            }
        }

        // Candidate rejection is expected search behavior, never a relaxation of acceptance.
        public static bool PreservesExpectedOpen(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> connections, IEnumerable<Sv5SpaceGate> gates) =>
            Analyze(core, connections, gates).Matrix.All(row => !row.ExpectedOpen ||
                (row.Reachability.SourceAnchorReachable && row.Reachability.TargetPortReachable));

        private static IEnumerable<RmapWorldGraphRole[]> ResourceOrders()
        {
            var resources = new[] { RmapWorldGraphRole.MooncoreOre,
                RmapWorldGraphRole.CondensedCoefficientSap, RmapWorldGraphRole.DeepStarYeast };
            return resources.SelectMany(a => resources.Where(b => b != a).SelectMany(b =>
                resources.Where(c => c != a && c != b).Select(c => new[] { a, b, c })));
        }
        private static RmapWorldGraphDirection Opposite(RmapWorldGraphDirection value) =>
            value == RmapWorldGraphDirection.Left ? RmapWorldGraphDirection.Right :
            value == RmapWorldGraphDirection.Right ? RmapWorldGraphDirection.Left :
            value == RmapWorldGraphDirection.Up ? RmapWorldGraphDirection.Down : RmapWorldGraphDirection.Up;
        private sealed class Binding
        {
            public Binding(Sv5SpaceConnection connection, RmapWorldGraphEdge edge, bool reverse)
            { Connection = connection; Edge = edge; Reverse = reverse; }
            public Sv5SpaceConnection Connection { get; }
            public RmapWorldGraphEdge Edge { get; }
            public bool Reverse { get; }
        }
    }
}
