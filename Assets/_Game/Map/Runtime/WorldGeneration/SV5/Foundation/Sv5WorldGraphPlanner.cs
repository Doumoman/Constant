using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Validation;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum Sv5WorldGraphRole
    {
        Start,
        MooncoreOre,
        CondensedCoefficientSap,
        DeepStarYeast,
        Forge,
        Seal,
        Boss,
        Exit
    }

    public enum Sv5WorldGraphDirection { Left, Right, Up, Down }
    public enum Sv5WorldGraphVerificationLevel { PlannedSpace, ExistingSv510Anchor }
    public enum Sv5WorldReturnShortcutPolicy { Optional, Required }
    public enum Sv5WorldGraphReservationStatus { Planned }

    public sealed class Sv5WorldGraphNode : IComparable<Sv5WorldGraphNode>
    {
        internal Sv5WorldGraphNode(Sv5WorldGraphRole role, Sv5WorldStableId anchor)
        {
            Role = role;
            AnchorStableId = anchor == null ? throw new ArgumentNullException(nameof(anchor)) : anchor.Value;
            NodeId = Sv5WorldGraphIdentity.Hash("SV5_NODE_V1", role.ToString(), AnchorStableId);
        }

        public Sv5WorldGraphRole Role { get; }
        public string AnchorStableId { get; }
        public string NodeId { get; }
        public int CompareTo(Sv5WorldGraphNode other) => other == null ? 1 :
            string.Compare(NodeId, other.NodeId, StringComparison.Ordinal);
    }

    /// <summary>A location-less requirement for a later region/port placement.
    /// It deliberately does not claim a world coordinate or a baked traversal.</summary>
    public sealed class Sv5WorldGraphReservation : IComparable<Sv5WorldGraphReservation>
    {
        internal Sv5WorldGraphReservation(
            Sv5WorldGraphNode node,
            Sv5WorldGraphDirection entryDirection,
            string releaseCondition)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            EntryDirection = entryDirection;
            ReleaseCondition = Sv5WorldGraphIdentity.Require(releaseCondition, nameof(releaseCondition));
            ReservationId = Sv5WorldGraphIdentity.Hash("SV5_RESERVATION_V1", node.NodeId,
                entryDirection.ToString(), ReleaseCondition);
        }

        public Sv5WorldGraphNode Node { get; }
        public string ReservationId { get; }
        public Sv5WorldGraphDirection EntryDirection { get; }
        public string ReleaseCondition { get; }
        public Sv5WorldGraphReservationStatus Status => Sv5WorldGraphReservationStatus.Planned;
        public int CompareTo(Sv5WorldGraphReservation other) => other == null ? 1 :
            string.Compare(ReservationId, other.ReservationId, StringComparison.Ordinal);
    }

    /// <summary>One directed planned traversal requirement. A reverse route must
    /// be represented by its own source connection and never appears implicitly.</summary>
    public sealed class Sv5WorldGraphEdge : IComparable<Sv5WorldGraphEdge>
    {
        public Sv5WorldGraphEdge(
            string sourceNodeId,
            string targetNodeId,
            Sv5WorldGraphDirection direction,
            string traversalCondition,
            string sourceConnectionId,
            bool sourceConnectionIsOneWay,
            ulong requiredResourceMask = 0,
            bool requiresForge = false,
            bool requiresSeal = false,
            bool requiresBossComplete = false)
        {
            SourceNodeId = Sv5WorldGraphIdentity.Require(sourceNodeId, nameof(sourceNodeId));
            TargetNodeId = Sv5WorldGraphIdentity.Require(targetNodeId, nameof(targetNodeId));
            Direction = direction;
            TraversalCondition = Sv5WorldGraphIdentity.Require(traversalCondition, nameof(traversalCondition));
            SourceConnectionId = Sv5WorldGraphIdentity.Require(sourceConnectionId, nameof(sourceConnectionId));
            SourceConnectionIsOneWay = sourceConnectionIsOneWay;
            RequiredResourceMask = requiredResourceMask;
            RequiresForge = requiresForge;
            RequiresSeal = requiresSeal;
            RequiresBossComplete = requiresBossComplete;
            EdgeId = Sv5WorldGraphIdentity.Hash("SV5_EDGE_V1", SourceNodeId, TargetNodeId, direction.ToString(),
                TraversalCondition, SourceConnectionId, sourceConnectionIsOneWay ? "1" : "0",
                requiredResourceMask.ToString(CultureInfo.InvariantCulture), requiresForge ? "1" : "0",
                requiresSeal ? "1" : "0", requiresBossComplete ? "1" : "0");
        }

        public string EdgeId { get; }
        public string SourceNodeId { get; }
        public string TargetNodeId { get; }
        public Sv5WorldGraphDirection Direction { get; }
        public string TraversalCondition { get; }
        public string SourceConnectionId { get; }
        public bool SourceConnectionIsOneWay { get; }
        public ulong RequiredResourceMask { get; }
        public bool RequiresForge { get; }
        public bool RequiresSeal { get; }
        public bool RequiresBossComplete { get; }
        public Sv5WorldGraphVerificationLevel VerificationLevel =>
            Sv5WorldGraphVerificationLevel.PlannedSpace;

        internal bool CanTraverse(Sv5WorldGraphState state) => state != null &&
            (state.ResourceMask & RequiredResourceMask) == RequiredResourceMask &&
            (!RequiresForge || state.ForgeMade) && (!RequiresSeal || state.SealOpen) &&
            (!RequiresBossComplete || state.BossComplete);

        public int CompareTo(Sv5WorldGraphEdge other) => other == null ? 1 :
            string.Compare(EdgeId, other.EdgeId, StringComparison.Ordinal);
    }

    /// <summary>Planning state mirrors the existing completion search's
    /// position/resource/forge/seal/boss semantics while retaining the ordered
    /// resource-proof cursor required by this graph stage.</summary>
    public sealed class Sv5WorldGraphState : IEquatable<Sv5WorldGraphState>, IComparable<Sv5WorldGraphState>
    {
        internal Sv5WorldGraphState(
            string positionNodeId,
            ulong resourceMask,
            int orderCursor,
            bool forgeMade,
            bool sealOpen,
            bool bossComplete)
        {
            PositionNodeId = Sv5WorldGraphIdentity.Require(positionNodeId, nameof(positionNodeId));
            ResourceMask = resourceMask;
            OrderCursor = orderCursor;
            ForgeMade = forgeMade;
            SealOpen = sealOpen;
            BossComplete = bossComplete;
            CompletionSemantics = new GeneratedCompletionStateKey(PositionNodeId, resourceMask,
                forgeMade ? GeneratedForgeValidationState.Activated : GeneratedForgeValidationState.NotReached,
                sealOpen ? GeneratedSealValidationState.Accepted : GeneratedSealValidationState.NotReached,
                bossComplete ? GeneratedBossValidationState.Defeated : GeneratedBossValidationState.NotReached,
                GeneratedSpecialValidationState.None);
            StableToken = string.Join("|", new[]
            {
                "SV5_STATE_V1", CompletionSemantics.StableToken,
                orderCursor.ToString(CultureInfo.InvariantCulture),
            });
        }

        public string PositionNodeId { get; }
        public ulong ResourceMask { get; }
        public int OrderCursor { get; }
        public bool ForgeMade { get; }
        public bool SealOpen { get; }
        public bool BossComplete { get; }
        public GeneratedCompletionStateKey CompletionSemantics { get; }
        public string StableToken { get; }
        public bool Equals(Sv5WorldGraphState other) => other != null &&
            string.Equals(StableToken, other.StableToken, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as Sv5WorldGraphState);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
        public int CompareTo(Sv5WorldGraphState other) => other == null ? 1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
    }

    public sealed class Sv5WorldGraphFailure : IComparable<Sv5WorldGraphFailure>
    {
        internal Sv5WorldGraphFailure(string code, Sv5WorldGraphState state, string detail)
        {
            Code = Sv5WorldGraphIdentity.Require(code, nameof(code));
            State = state;
            Detail = Sv5WorldGraphIdentity.Require(detail, nameof(detail));
        }

        public string Code { get; }
        public Sv5WorldGraphState State { get; }
        public string Detail { get; }
        public int CompareTo(Sv5WorldGraphFailure other) => other == null ? 1 :
            string.Compare(Code + "|" + Detail, other.Code + "|" + other.Detail, StringComparison.Ordinal);
    }

    public sealed class Sv5WorldGraphProof
    {
        internal Sv5WorldGraphProof(
            IEnumerable<Sv5WorldGraphRole> requestedOrder,
            Sv5WorldGraphState finalState,
            IEnumerable<string> sourceActions,
            IEnumerable<Sv5WorldGraphFailure> sourceFailures)
        {
            RequestedOrder = new ReadOnlyCollection<Sv5WorldGraphRole>((requestedOrder ??
                Array.Empty<Sv5WorldGraphRole>()).ToArray());
            FinalState = finalState;
            Actions = new ReadOnlyCollection<string>((sourceActions ?? Array.Empty<string>()).ToArray());
            Failures = new ReadOnlyCollection<Sv5WorldGraphFailure>((sourceFailures ??
                Array.Empty<Sv5WorldGraphFailure>()).OrderBy(value => value).ToArray());
            ProofId = Sv5WorldGraphIdentity.Hash("SV5_PROOF_V1", string.Join(",", RequestedOrder),
                FinalState == null ? "FAIL" : FinalState.StableToken, string.Join(";", Actions),
                string.Join(";", Failures.Select(value => value.Code + ":" + value.Detail)));
        }

        public IReadOnlyList<Sv5WorldGraphRole> RequestedOrder { get; }
        public Sv5WorldGraphState FinalState { get; }
        public IReadOnlyList<string> Actions { get; }
        public IReadOnlyList<Sv5WorldGraphFailure> Failures { get; }
        public string ProofId { get; }
        public bool Success => FinalState != null && Failures.Count == 0;
    }

    /// <summary>One legal transition observed while exploring the existing
    /// SV5 finite state machine. This is analysis evidence only.</summary>
    public sealed class Sv5WorldGraphTransition : IComparable<Sv5WorldGraphTransition>
    {
        internal Sv5WorldGraphTransition(Sv5WorldGraphState before, Sv5WorldGraphState after, string action)
        {
            Before = before ?? throw new ArgumentNullException(nameof(before));
            After = after ?? throw new ArgumentNullException(nameof(after));
            Action = Sv5WorldGraphIdentity.Require(action, nameof(action));
        }

        public Sv5WorldGraphState Before { get; }
        public Sv5WorldGraphState After { get; }
        public string Action { get; }
        public int CompareTo(Sv5WorldGraphTransition other) => other == null ? 1 :
            string.Compare(Before.StableToken + "|" + Action + "|" + After.StableToken,
                other.Before.StableToken + "|" + other.Action + "|" + other.After.StableToken,
                StringComparison.Ordinal);
    }

    /// <summary>Complete reachable-state evidence from the existing SV5
    /// transitions. It does not mutate a graph, world, Player, or scene.</summary>
    public sealed class Sv5WorldGraphExploration
    {
        internal Sv5WorldGraphExploration(IEnumerable<Sv5WorldGraphState> sourceStates,
            IEnumerable<Sv5WorldGraphTransition> sourceTransitions)
        {
            States = new ReadOnlyCollection<Sv5WorldGraphState>((sourceStates ?? Array.Empty<Sv5WorldGraphState>())
                .Where(value => value != null).OrderBy(value => value).ToArray());
            Transitions = new ReadOnlyCollection<Sv5WorldGraphTransition>((sourceTransitions ??
                Array.Empty<Sv5WorldGraphTransition>()).Where(value => value != null).Distinct()
                .OrderBy(value => value).ToArray());
        }

        public IReadOnlyList<Sv5WorldGraphState> States { get; }
        public IReadOnlyList<Sv5WorldGraphTransition> Transitions { get; }
    }

    public sealed class Sv5WorldGraphPlan
    {
        internal Sv5WorldGraphPlan(
            Sv5WorldDefinition definition,
            Sv5WorldReturnShortcutPolicy policy,
            IEnumerable<Sv5WorldGraphNode> sourceNodes,
            IEnumerable<Sv5WorldGraphEdge> sourceEdges,
            IEnumerable<Sv5WorldGraphReservation> sourceReservations,
            IEnumerable<Sv5WorldGraphProof> sourceProofs,
            IEnumerable<Sv5WorldGraphFailure> sourceFailures)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Policy = policy;
            Nodes = new ReadOnlyCollection<Sv5WorldGraphNode>((sourceNodes ??
                Array.Empty<Sv5WorldGraphNode>()).Where(value => value != null)
                .OrderBy(value => value.NodeId, StringComparer.Ordinal).ToArray());
            Edges = new ReadOnlyCollection<Sv5WorldGraphEdge>((sourceEdges ??
                Array.Empty<Sv5WorldGraphEdge>()).Where(value => value != null)
                .OrderBy(value => value.EdgeId, StringComparer.Ordinal).ToArray());
            Reservations = new ReadOnlyCollection<Sv5WorldGraphReservation>((sourceReservations ??
                Array.Empty<Sv5WorldGraphReservation>()).Where(value => value != null)
                .OrderBy(value => value.ReservationId, StringComparer.Ordinal).ToArray());
            Proofs = new ReadOnlyCollection<Sv5WorldGraphProof>((sourceProofs ??
                Array.Empty<Sv5WorldGraphProof>()).Where(value => value != null)
                .OrderBy(value => value.ProofId, StringComparer.Ordinal).ToArray());
            Failures = new ReadOnlyCollection<Sv5WorldGraphFailure>((sourceFailures ??
                Array.Empty<Sv5WorldGraphFailure>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            Digest = Sv5WorldGraphIdentity.Hash("SV5_PLAN_V1", definition.Digest, definition.RngBindings[
                Sv5WorldRngStream.RunGraph].InitialStateHex, policy.ToString(),
                string.Join(";", Nodes.Select(value => value.NodeId)),
                string.Join(";", Edges.Select(value => value.EdgeId)),
                string.Join(";", Reservations.Select(value => value.ReservationId)),
                string.Join(";", Proofs.Select(value => value.ProofId)));
        }

        public Sv5WorldDefinition Definition { get; }
        public Sv5WorldReturnShortcutPolicy Policy { get; }
        public IReadOnlyList<Sv5WorldGraphNode> Nodes { get; }
        public IReadOnlyList<Sv5WorldGraphEdge> Edges { get; }
        public IReadOnlyList<Sv5WorldGraphReservation> Reservations { get; }
        public IReadOnlyList<Sv5WorldGraphProof> Proofs { get; }
        public IReadOnlyList<Sv5WorldGraphFailure> Failures { get; }
        public string Digest { get; }
        public bool Success => Failures.Count == 0 && Proofs.Count == 6 && Proofs.All(value => value.Success);

    }

    internal static class Sv5WorldGraphIdentity
    {
        public static string Hash(params string[] lines) =>
            Sv5WorldDefinition.Hash(string.Join("\n", lines));

        public static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A graph identity is required.", name);
            return value.Trim();
        }
    }

    public static class Sv5WorldGraphPlanner
    {
        private static readonly Sv5WorldGraphRole[] ResourceRoles =
        {
            Sv5WorldGraphRole.MooncoreOre,
            Sv5WorldGraphRole.CondensedCoefficientSap,
            Sv5WorldGraphRole.DeepStarYeast,
        };

        public static Sv5WorldGraphPlan Plan(
            Sv5WorldDefinition definition,
            Sv5WorldReturnShortcutPolicy policy = Sv5WorldReturnShortcutPolicy.Optional)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!Enum.IsDefined(typeof(Sv5WorldReturnShortcutPolicy), policy))
                throw new ArgumentOutOfRangeException(nameof(policy));
            var anchors = definition.GetStableIds(Sv5WorldStableIdKind.MicroChunk)
                .OrderBy(value => value.Value, StringComparer.Ordinal).Take(8).ToArray();
            if (anchors.Length != 8) throw new InvalidOperationException(
                "SV5 needs eight actual SV5 MicroChunk stable-ID anchors.");
            var nodes = Enum.GetValues(typeof(Sv5WorldGraphRole)).Cast<Sv5WorldGraphRole>()
                .Select((role, index) => new Sv5WorldGraphNode(role, anchors[index])).ToArray();
            var byRole = nodes.ToDictionary(value => value.Role, value => value);
            var reservations = nodes.Select(node => new Sv5WorldGraphReservation(node,
                EntryDirection(node.Role), ReleaseCondition(node.Role))).ToList();
            var edges = BuildNormalEdges(byRole).ToList();
            if (policy == Sv5WorldReturnShortcutPolicy.Required)
                AddRequiredShortcuts(byRole, edges, reservations);
            var failures = Validate(nodes, edges, reservations).ToList();
            var proofs = failures.Count == 0 ? ResourceOrders().Select(order =>
                Evaluate(nodes, edges, order)).ToArray() : Array.Empty<Sv5WorldGraphProof>();
            failures.AddRange(proofs.Where(value => !value.Success).SelectMany(value => value.Failures));
            return new Sv5WorldGraphPlan(definition, policy, nodes, edges, reservations, proofs, failures);
        }

        /// <summary>Public for focused broken-edge fixtures. It remains a pure
        /// planning search; no tilemap, Player, or runtime state is mutated.</summary>
        public static Sv5WorldGraphProof Evaluate(
            IEnumerable<Sv5WorldGraphNode> sourceNodes,
            IEnumerable<Sv5WorldGraphEdge> sourceEdges,
            IEnumerable<Sv5WorldGraphRole> requestedOrder) =>
            EvaluateWithAnalysisNodes(sourceNodes, Array.Empty<string>(), sourceEdges, requestedOrder);

        /// <summary>
        /// Evaluates the existing SV5 state machine with explicitly declared,
        /// action-less analysis nodes. This is a pure candidate-analysis surface:
        /// it neither changes the source graph nor grants a resource, Forge, Seal,
        /// Boss, or Exit transition at an added node.
        /// </summary>
        public static Sv5WorldGraphProof EvaluateWithAnalysisNodes(
            IEnumerable<Sv5WorldGraphNode> sourceNodes,
            IEnumerable<string> sourceAnalysisNodeIds,
            IEnumerable<Sv5WorldGraphEdge> sourceEdges,
            IEnumerable<Sv5WorldGraphRole> requestedOrder)
        {
            var nodes = (sourceNodes ?? Array.Empty<Sv5WorldGraphNode>()).Where(value => value != null)
                .ToDictionary(value => value.NodeId, value => value, StringComparer.Ordinal);
            var analysisNodeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string sourceId in sourceAnalysisNodeIds ?? Array.Empty<string>())
            {
                string id = Sv5WorldGraphIdentity.Require(sourceId, nameof(sourceAnalysisNodeIds));
                if (nodes.ContainsKey(id) || !analysisNodeIds.Add(id))
                    throw new ArgumentException("Analysis node IDs must be distinct from source graph nodes.",
                        nameof(sourceAnalysisNodeIds));
            }
            var roles = nodes.Values.GroupBy(value => value.Role).ToDictionary(value => value.Key,
                value => value.Single());
            var order = (requestedOrder ?? Array.Empty<Sv5WorldGraphRole>()).ToArray();
            var failures = ValidateOrder(order).ToList();
            roles.TryGetValue(Sv5WorldGraphRole.Start, out var start);
            roles.TryGetValue(Sv5WorldGraphRole.Exit, out var exit);
            if (start == null || exit == null)
                failures.Add(new Sv5WorldGraphFailure("MISSING_REQUIRED_ROLE", null, "START_OR_EXIT"));
            if (failures.Count != 0) return new Sv5WorldGraphProof(order, null,
                Array.Empty<string>(), failures);

            var knownNodeIds = new HashSet<string>(nodes.Keys, StringComparer.Ordinal);
            knownNodeIds.UnionWith(analysisNodeIds);
            var edges = (sourceEdges ?? Array.Empty<Sv5WorldGraphEdge>()).Where(value => value != null)
                .Where(value => knownNodeIds.Contains(value.SourceNodeId) && knownNodeIds.Contains(value.TargetNodeId))
                .OrderBy(value => value).ToArray();
            var bySource = edges.GroupBy(value => value.SourceNodeId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.OrderBy(edge => edge).ToArray(), StringComparer.Ordinal);
            var initial = new Sv5WorldGraphState(start.NodeId, 0, 0, false, false, false);
            var queue = new Queue<Sv5WorldGraphState>();
            var visited = new HashSet<Sv5WorldGraphState>();
            var predecessor = new Dictionary<Sv5WorldGraphState, Previous>();
            queue.Enqueue(initial);
            visited.Add(initial);
            Sv5WorldGraphState found = null;
            while (queue.Count != 0)
            {
                Sv5WorldGraphState state = queue.Dequeue();
                if (IsGoal(state, exit.NodeId, order.Length)) { found = state; break; }
                foreach (var transition in StateTransitions(state, roles, order))
                    Add(state, transition.State, transition.Action);
                if (!bySource.TryGetValue(state.PositionNodeId, out var outgoing)) continue;
                foreach (var edge in outgoing.Where(edge => edge.CanTraverse(state)))
                    Add(state, new Sv5WorldGraphState(edge.TargetNodeId, state.ResourceMask,
                        state.OrderCursor, state.ForgeMade, state.SealOpen, state.BossComplete),
                        "MOVE|" + edge.EdgeId);
            }
            if (found == null)
            {
                Sv5WorldGraphState frontier = visited.OrderByDescending(value => value.OrderCursor)
                    .ThenByDescending(value => value.ResourceMask).ThenBy(value => value).FirstOrDefault();
                return new Sv5WorldGraphProof(order, null, Array.Empty<string>(), new[]
                {
                    new Sv5WorldGraphFailure("GRAPH_GOAL_UNREACHABLE", frontier,
                        MissingCondition(frontier, roles, order)),
                });
            }
            var actions = new List<string>();
            for (var cursor = found; !cursor.Equals(initial); cursor = predecessor[cursor].PreviousState)
                actions.Add(predecessor[cursor].Action);
            actions.Reverse();
            return new Sv5WorldGraphProof(order, found, actions, Array.Empty<Sv5WorldGraphFailure>());

            void Add(Sv5WorldGraphState previous, Sv5WorldGraphState next, string action)
            {
                if (!visited.Add(next)) return;
                predecessor.Add(next, new Previous(previous, action));
                queue.Enqueue(next);
            }
        }

        /// <summary>
        /// Enumerates every reachable legal state and transition through the
        /// same SV5 transition function used by Evaluate. Declared analysis
        /// nodes remain action-less and are never persisted to the source graph.
        /// </summary>
        public static Sv5WorldGraphExploration ExploreWithAnalysisNodes(
            IEnumerable<Sv5WorldGraphNode> sourceNodes,
            IEnumerable<string> sourceAnalysisNodeIds,
            IEnumerable<Sv5WorldGraphEdge> sourceEdges,
            IEnumerable<Sv5WorldGraphRole> requestedOrder)
        {
            var nodes = (sourceNodes ?? Array.Empty<Sv5WorldGraphNode>()).Where(value => value != null)
                .ToDictionary(value => value.NodeId, value => value, StringComparer.Ordinal);
            var analysisNodeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string sourceId in sourceAnalysisNodeIds ?? Array.Empty<string>())
            {
                string id = Sv5WorldGraphIdentity.Require(sourceId, nameof(sourceAnalysisNodeIds));
                if (nodes.ContainsKey(id) || !analysisNodeIds.Add(id))
                    throw new ArgumentException("Analysis node IDs must be distinct from source graph nodes.",
                        nameof(sourceAnalysisNodeIds));
            }
            var roles = nodes.Values.GroupBy(value => value.Role).ToDictionary(value => value.Key,
                value => value.Single());
            var order = (requestedOrder ?? Array.Empty<Sv5WorldGraphRole>()).ToArray();
            Sv5WorldGraphFailure orderFailure = ValidateOrder(order).FirstOrDefault();
            if (orderFailure != null) throw new ArgumentException(orderFailure.Detail, nameof(requestedOrder));
            if (!roles.TryGetValue(Sv5WorldGraphRole.Start, out Sv5WorldGraphNode start) ||
                !roles.ContainsKey(Sv5WorldGraphRole.Exit))
                throw new ArgumentException("START_OR_EXIT", nameof(sourceNodes));

            var knownNodeIds = new HashSet<string>(nodes.Keys, StringComparer.Ordinal);
            knownNodeIds.UnionWith(analysisNodeIds);
            var bySource = (sourceEdges ?? Array.Empty<Sv5WorldGraphEdge>()).Where(value => value != null)
                .Where(value => knownNodeIds.Contains(value.SourceNodeId) && knownNodeIds.Contains(value.TargetNodeId))
                .OrderBy(value => value).GroupBy(value => value.SourceNodeId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.OrderBy(edge => edge).ToArray(), StringComparer.Ordinal);
            var initial = new Sv5WorldGraphState(start.NodeId, 0, 0, false, false, false);
            var queue = new Queue<Sv5WorldGraphState>();
            var visited = new HashSet<Sv5WorldGraphState>();
            var transitions = new List<Sv5WorldGraphTransition>();
            queue.Enqueue(initial);
            visited.Add(initial);
            while (queue.Count != 0)
            {
                Sv5WorldGraphState state = queue.Dequeue();
                foreach (Transition transition in StateTransitions(state, roles, order))
                    Add(state, transition.State, transition.Action);
                if (!bySource.TryGetValue(state.PositionNodeId, out Sv5WorldGraphEdge[] outgoing)) continue;
                foreach (Sv5WorldGraphEdge edge in outgoing.Where(edge => edge.CanTraverse(state)))
                    Add(state, new Sv5WorldGraphState(edge.TargetNodeId, state.ResourceMask, state.OrderCursor,
                        state.ForgeMade, state.SealOpen, state.BossComplete), "MOVE|" + edge.EdgeId);
            }
            return new Sv5WorldGraphExploration(visited, transitions);

            void Add(Sv5WorldGraphState before, Sv5WorldGraphState after, string action)
            {
                transitions.Add(new Sv5WorldGraphTransition(before, after, action));
                if (visited.Add(after)) queue.Enqueue(after);
            }
        }

        public static IReadOnlyList<Sv5WorldGraphFailure> ValidateRequirements(
            IEnumerable<Sv5WorldGraphNode> nodes,
            IEnumerable<Sv5WorldGraphEdge> edges,
            IEnumerable<Sv5WorldGraphReservation> reservations) =>
            new ReadOnlyCollection<Sv5WorldGraphFailure>(Validate(nodes, edges, reservations)
                .OrderBy(value => value).ToArray());

        private static IEnumerable<Sv5WorldGraphEdge> BuildNormalEdges(
            IReadOnlyDictionary<Sv5WorldGraphRole, Sv5WorldGraphNode> nodes)
        {
            var start = nodes[Sv5WorldGraphRole.Start];
            foreach (var resource in ResourceRoles)
            {
                var node = nodes[resource];
                Sv5WorldGraphDirection outDirection = ResourceDirection(resource);
                yield return Edge(start, node, outDirection, "NORMAL_RESOURCE_APPROACH",
                    "SV5_NORMAL_" + resource + "_OUT", true);
                yield return Edge(node, start, Opposite(outDirection), "NORMAL_RESOURCE_RETURN",
                    "SV5_NORMAL_" + resource + "_RETURN", true);
            }
            yield return Edge(start, nodes[Sv5WorldGraphRole.Forge], Sv5WorldGraphDirection.Down,
                "NORMAL_FORGE_APPROACH", "SV5_NORMAL_FORGE_OUT", true);
            yield return Edge(nodes[Sv5WorldGraphRole.Forge], start, Sv5WorldGraphDirection.Up,
                "NORMAL_FORGE_RETURN", "SV5_NORMAL_FORGE_RETURN", true);
            yield return Edge(nodes[Sv5WorldGraphRole.Forge], nodes[Sv5WorldGraphRole.Seal],
                Sv5WorldGraphDirection.Right, "FORGE_GATED_SEAL_APPROACH", "SV5_FORGE_TO_SEAL", true,
                requiresForge: true);
            yield return Edge(nodes[Sv5WorldGraphRole.Seal], nodes[Sv5WorldGraphRole.Boss],
                Sv5WorldGraphDirection.Up, "SEAL_GATED_BOSS_APPROACH", "SV5_SEAL_TO_BOSS", true,
                requiresSeal: true);
            yield return Edge(nodes[Sv5WorldGraphRole.Boss], nodes[Sv5WorldGraphRole.Exit],
                Sv5WorldGraphDirection.Right, "BOSS_GATED_EXIT_APPROACH", "SV5_BOSS_TO_EXIT", true,
                requiresBossComplete: true);
        }

        private static void AddRequiredShortcuts(
            IReadOnlyDictionary<Sv5WorldGraphRole, Sv5WorldGraphNode> nodes,
            ICollection<Sv5WorldGraphEdge> edges,
            ICollection<Sv5WorldGraphReservation> reservations)
        {
            foreach (var resource in ResourceRoles)
            {
                ulong bit = Bit(resource);
                var node = nodes[resource];
                edges.Add(Edge(node, nodes[Sv5WorldGraphRole.Forge], Sv5WorldGraphDirection.Down,
                    "REQUIRED_RESOURCE_RETURN_SHORTCUT", "SV5_SHORTCUT_" + resource, true,
                    requiredResourceMask: bit));
                reservations.Add(new Sv5WorldGraphReservation(node, Sv5WorldGraphDirection.Down,
                    "AFTER_" + resource + "_ACQUIRED"));
            }
        }

        private static IEnumerable<Transition> StateTransitions(
            Sv5WorldGraphState state,
            IReadOnlyDictionary<Sv5WorldGraphRole, Sv5WorldGraphNode> roles,
            IReadOnlyList<Sv5WorldGraphRole> order)
        {
            if (state.OrderCursor < order.Count && roles.TryGetValue(order[state.OrderCursor], out var nextResource) &&
                string.Equals(state.PositionNodeId, nextResource.NodeId, StringComparison.Ordinal))
            {
                ulong bit = Bit(nextResource.Role);
                if ((state.ResourceMask & bit) == 0)
                    yield return new Transition(new Sv5WorldGraphState(state.PositionNodeId,
                        state.ResourceMask | bit, state.OrderCursor + 1, state.ForgeMade,
                        state.SealOpen, state.BossComplete), "ACQUIRE|" + nextResource.Role);
            }
            if (At(state, roles, Sv5WorldGraphRole.Forge) && !state.ForgeMade &&
                state.ResourceMask == RequiredResourceMask)
                yield return new Transition(new Sv5WorldGraphState(state.PositionNodeId,
                    state.ResourceMask, state.OrderCursor, true, state.SealOpen, state.BossComplete), "FORGE|MAKE_SEAL");
            if (At(state, roles, Sv5WorldGraphRole.Seal) && state.ForgeMade && !state.SealOpen)
                yield return new Transition(new Sv5WorldGraphState(state.PositionNodeId,
                    state.ResourceMask, state.OrderCursor, true, true, state.BossComplete), "SEAL|OPEN");
            if (At(state, roles, Sv5WorldGraphRole.Boss) && state.SealOpen && !state.BossComplete)
                yield return new Transition(new Sv5WorldGraphState(state.PositionNodeId,
                    state.ResourceMask, state.OrderCursor, true, true, true), "BOSS|PLANNED_COMPLETION_EVENT");
        }

        private static IEnumerable<Sv5WorldGraphFailure> Validate(
            IEnumerable<Sv5WorldGraphNode> sourceNodes,
            IEnumerable<Sv5WorldGraphEdge> sourceEdges,
            IEnumerable<Sv5WorldGraphReservation> sourceReservations)
        {
            var nodes = (sourceNodes ?? Array.Empty<Sv5WorldGraphNode>()).Where(value => value != null).ToArray();
            foreach (var role in Enum.GetValues(typeof(Sv5WorldGraphRole)).Cast<Sv5WorldGraphRole>())
                if (nodes.Count(value => value.Role == role) != 1)
                    yield return new Sv5WorldGraphFailure("ROLE_CARDINALITY", null, role.ToString());
            foreach (var duplicate in nodes.GroupBy(value => value.NodeId).Where(value => value.Count() != 1))
                yield return new Sv5WorldGraphFailure("DUPLICATE_NODE_ID", null, duplicate.Key);
            var nodeIds = new HashSet<string>(nodes.Select(value => value.NodeId), StringComparer.Ordinal);
            var edges = (sourceEdges ?? Array.Empty<Sv5WorldGraphEdge>()).Where(value => value != null).ToArray();
            foreach (var edge in edges)
            {
                if (!nodeIds.Contains(edge.SourceNodeId) || !nodeIds.Contains(edge.TargetNodeId))
                    yield return new Sv5WorldGraphFailure("DANGLING_EDGE", null, edge.EdgeId);
                if (edge.SourceConnectionIsOneWay && edges.Any(other => other != edge &&
                    string.Equals(other.SourceConnectionId, edge.SourceConnectionId, StringComparison.Ordinal) &&
                    string.Equals(other.SourceNodeId, edge.TargetNodeId, StringComparison.Ordinal) &&
                    string.Equals(other.TargetNodeId, edge.SourceNodeId, StringComparison.Ordinal)))
                    yield return new Sv5WorldGraphFailure("REVERSE_OF_ONE_WAY_EDGE", null, edge.SourceConnectionId);
            }
            foreach (var node in nodes)
                if (!(sourceReservations ?? Array.Empty<Sv5WorldGraphReservation>()).Any(value => value != null &&
                    string.Equals(value.Node.NodeId, node.NodeId, StringComparison.Ordinal)))
                    yield return new Sv5WorldGraphFailure("MISSING_RESERVATION", null, node.NodeId);
        }

        private static IEnumerable<Sv5WorldGraphFailure> ValidateOrder(Sv5WorldGraphRole[] order)
        {
            if (order.Length != ResourceRoles.Length || order.Distinct().Count() != ResourceRoles.Length ||
                order.Any(role => !ResourceRoles.Contains(role)))
                yield return new Sv5WorldGraphFailure("INVALID_RESOURCE_ORDER", null,
                    string.Join(",", order.Select(value => value.ToString())));
        }

        private static bool IsGoal(Sv5WorldGraphState state, string exitNodeId, int orderLength) =>
            state != null && state.OrderCursor == orderLength && state.ResourceMask == RequiredResourceMask &&
            state.ForgeMade && state.SealOpen && state.BossComplete &&
            string.Equals(state.PositionNodeId, exitNodeId, StringComparison.Ordinal);

        private static string MissingCondition(
            Sv5WorldGraphState state,
            IReadOnlyDictionary<Sv5WorldGraphRole, Sv5WorldGraphNode> roles,
            IReadOnlyList<Sv5WorldGraphRole> order)
        {
            if (state == null) return "MISSING_START_OR_EDGE_RESERVATION";
            if (state.OrderCursor < order.Count) return "RESOURCE_ORDER_OR_RETURN_RESERVATION:" + order[state.OrderCursor];
            if (!state.ForgeMade) return "FORGE_REQUIRES_ALL_RESOURCES";
            if (!state.SealOpen) return "SEAL_REQUIRES_FORGE";
            if (!state.BossComplete) return "BOSS_REQUIRES_SEAL_AND_PLANNED_EVENT";
            return "EXIT_REQUIRES_BOSS_COMPLETE";
        }

        private static IEnumerable<Sv5WorldGraphRole[]> ResourceOrders() => ResourceRoles
            .SelectMany(first => ResourceRoles.Where(second => second != first).SelectMany(second =>
                ResourceRoles.Where(third => third != first && third != second).Select(third =>
                    new[] { first, second, third })));

        private static Sv5WorldGraphEdge Edge(
            Sv5WorldGraphNode source,
            Sv5WorldGraphNode target,
            Sv5WorldGraphDirection direction,
            string condition,
            string connection,
            bool oneWay,
            ulong requiredResourceMask = 0,
            bool requiresForge = false,
            bool requiresSeal = false,
            bool requiresBossComplete = false) => new Sv5WorldGraphEdge(source.NodeId, target.NodeId,
                direction, condition, connection, oneWay, requiredResourceMask, requiresForge,
                requiresSeal, requiresBossComplete);

        private static bool At(Sv5WorldGraphState state,
            IReadOnlyDictionary<Sv5WorldGraphRole, Sv5WorldGraphNode> nodes,
            Sv5WorldGraphRole role) => string.Equals(state.PositionNodeId, nodes[role].NodeId,
                StringComparison.Ordinal);
        private static ulong RequiredResourceMask => Bit(Sv5WorldGraphRole.MooncoreOre) |
            Bit(Sv5WorldGraphRole.CondensedCoefficientSap) | Bit(Sv5WorldGraphRole.DeepStarYeast);
        private static ulong Bit(Sv5WorldGraphRole role) => role == Sv5WorldGraphRole.MooncoreOre ? 1UL :
            role == Sv5WorldGraphRole.CondensedCoefficientSap ? 2UL :
            role == Sv5WorldGraphRole.DeepStarYeast ? 4UL : 0UL;
        private static Sv5WorldGraphDirection ResourceDirection(Sv5WorldGraphRole role) =>
            role == Sv5WorldGraphRole.MooncoreOre ? Sv5WorldGraphDirection.Right :
            role == Sv5WorldGraphRole.CondensedCoefficientSap ? Sv5WorldGraphDirection.Up :
            Sv5WorldGraphDirection.Left;
        private static Sv5WorldGraphDirection EntryDirection(Sv5WorldGraphRole role) =>
            role == Sv5WorldGraphRole.Start ? Sv5WorldGraphDirection.Right :
            role == Sv5WorldGraphRole.Forge ? Sv5WorldGraphDirection.Down :
            role == Sv5WorldGraphRole.Seal ? Sv5WorldGraphDirection.Right :
            role == Sv5WorldGraphRole.Boss ? Sv5WorldGraphDirection.Up :
            role == Sv5WorldGraphRole.Exit ? Sv5WorldGraphDirection.Right : ResourceDirection(role);
        private static Sv5WorldGraphDirection Opposite(Sv5WorldGraphDirection direction) =>
            direction == Sv5WorldGraphDirection.Left ? Sv5WorldGraphDirection.Right :
            direction == Sv5WorldGraphDirection.Right ? Sv5WorldGraphDirection.Left :
            direction == Sv5WorldGraphDirection.Up ? Sv5WorldGraphDirection.Down : Sv5WorldGraphDirection.Up;
        private static string ReleaseCondition(Sv5WorldGraphRole role) => role == Sv5WorldGraphRole.Start ?
            "WORLD_START" : role == Sv5WorldGraphRole.Forge ? "ALL_THREE_RESOURCES" :
            role == Sv5WorldGraphRole.Seal ? "FORGE_MADE" : role == Sv5WorldGraphRole.Boss ?
            "SEAL_OPEN" : role == Sv5WorldGraphRole.Exit ? "BOSS_COMPLETE" : "RESOURCE_ACCESS";
        private static string Hash(params string[] lines) => Sv5WorldDefinition.Hash(string.Join("\n", lines));
        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A graph identity is required.", name);
            return value.Trim();
        }

        private sealed class Transition
        {
            public Transition(Sv5WorldGraphState state, string action) { State = state; Action = action; }
            public Sv5WorldGraphState State { get; }
            public string Action { get; }
        }

        private sealed class Previous
        {
            public Previous(Sv5WorldGraphState state, string action) { PreviousState = state; Action = action; }
            public Sv5WorldGraphState PreviousState { get; }
            public string Action { get; }
        }
    }

    /// <summary>Pure, sorted RFC4180 evidence material for an editor/reporting
    /// layer. The planner itself performs no file I/O.</summary>
    public static class Sv5WorldGraphExport
    {
        public static string NodesCsv(Sv5WorldGraphPlan plan) => Lines("node_id,role,anchor_stable_id,verification_level",
            plan.Nodes.OrderBy(value => value.NodeId).Select(value => Row(value.NodeId, value.Role,
                value.AnchorStableId, Sv5WorldGraphVerificationLevel.ExistingSv510Anchor)));
        public static string EdgesCsv(Sv5WorldGraphPlan plan) => Lines("edge_id,source_node_id,target_node_id,direction,condition,connection_id,verification_level",
            plan.Edges.OrderBy(value => value.EdgeId).Select(value => Row(value.EdgeId, value.SourceNodeId,
                value.TargetNodeId, value.Direction, value.TraversalCondition, value.SourceConnectionId,
                value.VerificationLevel)));
        public static string ReservationsCsv(Sv5WorldGraphPlan plan) => Lines("reservation_id,node_id,anchor_stable_id,entry_direction,release_condition,status",
            plan.Reservations.OrderBy(value => value.ReservationId).Select(value => Row(value.ReservationId,
                value.Node.NodeId, value.Node.AnchorStableId, value.EntryDirection,
                value.ReleaseCondition, value.Status)));
        public static string ProofsCsv(Sv5WorldGraphPlan plan) => Lines("proof_id,resource_order,success,actions,final_state",
            plan.Proofs.OrderBy(value => value.ProofId).Select(value => Row(value.ProofId,
                string.Join(">", value.RequestedOrder), value.Success, string.Join(";", value.Actions),
                value.FinalState == null ? string.Empty : value.FinalState.StableToken)));

        private static string Lines(string header, IEnumerable<string> rows) => header + "\n" +
            string.Join("\n", rows ?? Array.Empty<string>()) + "\n";
        private static string Row(params object[] values) => string.Join(",", values.Select(value => Escape(
            value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture))));
        private static string Escape(string value) => '"' + (value ?? string.Empty).Replace("\"", "\"\"") + '"';
    }
}
