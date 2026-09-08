using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Validation;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum RmapWorldGraphRole
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

    public enum RmapWorldGraphDirection { Left, Right, Up, Down }
    public enum RmapWorldGraphVerificationLevel { PlannedSpace, ExistingRmap10Anchor }
    public enum RmapWorldReturnShortcutPolicy { Optional, Required }
    public enum RmapWorldGraphReservationStatus { Planned }

    public sealed class RmapWorldGraphNode : IComparable<RmapWorldGraphNode>
    {
        internal RmapWorldGraphNode(RmapWorldGraphRole role, RmapWorldStableId anchor)
        {
            Role = role;
            AnchorStableId = anchor == null ? throw new ArgumentNullException(nameof(anchor)) : anchor.Value;
            NodeId = RmapWorldGraphIdentity.Hash("RMAP13_NODE_V1", role.ToString(), AnchorStableId);
        }

        public RmapWorldGraphRole Role { get; }
        public string AnchorStableId { get; }
        public string NodeId { get; }
        public int CompareTo(RmapWorldGraphNode other) => other == null ? 1 :
            string.Compare(NodeId, other.NodeId, StringComparison.Ordinal);
    }

    /// <summary>A location-less requirement for a later region/port placement.
    /// It deliberately does not claim a world coordinate or a baked traversal.</summary>
    public sealed class RmapWorldGraphReservation : IComparable<RmapWorldGraphReservation>
    {
        internal RmapWorldGraphReservation(
            RmapWorldGraphNode node,
            RmapWorldGraphDirection entryDirection,
            string releaseCondition)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            EntryDirection = entryDirection;
            ReleaseCondition = RmapWorldGraphIdentity.Require(releaseCondition, nameof(releaseCondition));
            ReservationId = RmapWorldGraphIdentity.Hash("RMAP13_RESERVATION_V1", node.NodeId,
                entryDirection.ToString(), ReleaseCondition);
        }

        public RmapWorldGraphNode Node { get; }
        public string ReservationId { get; }
        public RmapWorldGraphDirection EntryDirection { get; }
        public string ReleaseCondition { get; }
        public RmapWorldGraphReservationStatus Status => RmapWorldGraphReservationStatus.Planned;
        public int CompareTo(RmapWorldGraphReservation other) => other == null ? 1 :
            string.Compare(ReservationId, other.ReservationId, StringComparison.Ordinal);
    }

    /// <summary>One directed planned traversal requirement. A reverse route must
    /// be represented by its own source connection and never appears implicitly.</summary>
    public sealed class RmapWorldGraphEdge : IComparable<RmapWorldGraphEdge>
    {
        public RmapWorldGraphEdge(
            string sourceNodeId,
            string targetNodeId,
            RmapWorldGraphDirection direction,
            string traversalCondition,
            string sourceConnectionId,
            bool sourceConnectionIsOneWay,
            ulong requiredResourceMask = 0,
            bool requiresForge = false,
            bool requiresSeal = false,
            bool requiresBossComplete = false)
        {
            SourceNodeId = RmapWorldGraphIdentity.Require(sourceNodeId, nameof(sourceNodeId));
            TargetNodeId = RmapWorldGraphIdentity.Require(targetNodeId, nameof(targetNodeId));
            Direction = direction;
            TraversalCondition = RmapWorldGraphIdentity.Require(traversalCondition, nameof(traversalCondition));
            SourceConnectionId = RmapWorldGraphIdentity.Require(sourceConnectionId, nameof(sourceConnectionId));
            SourceConnectionIsOneWay = sourceConnectionIsOneWay;
            RequiredResourceMask = requiredResourceMask;
            RequiresForge = requiresForge;
            RequiresSeal = requiresSeal;
            RequiresBossComplete = requiresBossComplete;
            EdgeId = RmapWorldGraphIdentity.Hash("RMAP13_EDGE_V1", SourceNodeId, TargetNodeId, direction.ToString(),
                TraversalCondition, SourceConnectionId, sourceConnectionIsOneWay ? "1" : "0",
                requiredResourceMask.ToString(CultureInfo.InvariantCulture), requiresForge ? "1" : "0",
                requiresSeal ? "1" : "0", requiresBossComplete ? "1" : "0");
        }

        public string EdgeId { get; }
        public string SourceNodeId { get; }
        public string TargetNodeId { get; }
        public RmapWorldGraphDirection Direction { get; }
        public string TraversalCondition { get; }
        public string SourceConnectionId { get; }
        public bool SourceConnectionIsOneWay { get; }
        public ulong RequiredResourceMask { get; }
        public bool RequiresForge { get; }
        public bool RequiresSeal { get; }
        public bool RequiresBossComplete { get; }
        public RmapWorldGraphVerificationLevel VerificationLevel =>
            RmapWorldGraphVerificationLevel.PlannedSpace;

        internal bool CanTraverse(RmapWorldGraphState state) => state != null &&
            (state.ResourceMask & RequiredResourceMask) == RequiredResourceMask &&
            (!RequiresForge || state.ForgeMade) && (!RequiresSeal || state.SealOpen) &&
            (!RequiresBossComplete || state.BossComplete);

        public int CompareTo(RmapWorldGraphEdge other) => other == null ? 1 :
            string.Compare(EdgeId, other.EdgeId, StringComparison.Ordinal);
    }

    /// <summary>Planning state mirrors the existing completion search's
    /// position/resource/forge/seal/boss semantics while retaining the ordered
    /// resource-proof cursor required by this graph stage.</summary>
    public sealed class RmapWorldGraphState : IEquatable<RmapWorldGraphState>, IComparable<RmapWorldGraphState>
    {
        internal RmapWorldGraphState(
            string positionNodeId,
            ulong resourceMask,
            int orderCursor,
            bool forgeMade,
            bool sealOpen,
            bool bossComplete)
        {
            PositionNodeId = RmapWorldGraphIdentity.Require(positionNodeId, nameof(positionNodeId));
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
                "RMAP13_STATE_V1", CompletionSemantics.StableToken,
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
        public bool Equals(RmapWorldGraphState other) => other != null &&
            string.Equals(StableToken, other.StableToken, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as RmapWorldGraphState);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
        public int CompareTo(RmapWorldGraphState other) => other == null ? 1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
    }

    public sealed class RmapWorldGraphFailure : IComparable<RmapWorldGraphFailure>
    {
        internal RmapWorldGraphFailure(string code, RmapWorldGraphState state, string detail)
        {
            Code = RmapWorldGraphIdentity.Require(code, nameof(code));
            State = state;
            Detail = RmapWorldGraphIdentity.Require(detail, nameof(detail));
        }

        public string Code { get; }
        public RmapWorldGraphState State { get; }
        public string Detail { get; }
        public int CompareTo(RmapWorldGraphFailure other) => other == null ? 1 :
            string.Compare(Code + "|" + Detail, other.Code + "|" + other.Detail, StringComparison.Ordinal);
    }

    public sealed class RmapWorldGraphProof
    {
        internal RmapWorldGraphProof(
            IEnumerable<RmapWorldGraphRole> requestedOrder,
            RmapWorldGraphState finalState,
            IEnumerable<string> sourceActions,
            IEnumerable<RmapWorldGraphFailure> sourceFailures)
        {
            RequestedOrder = new ReadOnlyCollection<RmapWorldGraphRole>((requestedOrder ??
                Array.Empty<RmapWorldGraphRole>()).ToArray());
            FinalState = finalState;
            Actions = new ReadOnlyCollection<string>((sourceActions ?? Array.Empty<string>()).ToArray());
            Failures = new ReadOnlyCollection<RmapWorldGraphFailure>((sourceFailures ??
                Array.Empty<RmapWorldGraphFailure>()).OrderBy(value => value).ToArray());
            ProofId = RmapWorldGraphIdentity.Hash("RMAP13_PROOF_V1", string.Join(",", RequestedOrder),
                FinalState == null ? "FAIL" : FinalState.StableToken, string.Join(";", Actions),
                string.Join(";", Failures.Select(value => value.Code + ":" + value.Detail)));
        }

        public IReadOnlyList<RmapWorldGraphRole> RequestedOrder { get; }
        public RmapWorldGraphState FinalState { get; }
        public IReadOnlyList<string> Actions { get; }
        public IReadOnlyList<RmapWorldGraphFailure> Failures { get; }
        public string ProofId { get; }
        public bool Success => FinalState != null && Failures.Count == 0;
    }

    public sealed class RmapWorldGraphPlan
    {
        internal RmapWorldGraphPlan(
            RmapWorldDefinition definition,
            RmapWorldReturnShortcutPolicy policy,
            IEnumerable<RmapWorldGraphNode> sourceNodes,
            IEnumerable<RmapWorldGraphEdge> sourceEdges,
            IEnumerable<RmapWorldGraphReservation> sourceReservations,
            IEnumerable<RmapWorldGraphProof> sourceProofs,
            IEnumerable<RmapWorldGraphFailure> sourceFailures)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Policy = policy;
            Nodes = new ReadOnlyCollection<RmapWorldGraphNode>((sourceNodes ??
                Array.Empty<RmapWorldGraphNode>()).Where(value => value != null)
                .OrderBy(value => value.NodeId, StringComparer.Ordinal).ToArray());
            Edges = new ReadOnlyCollection<RmapWorldGraphEdge>((sourceEdges ??
                Array.Empty<RmapWorldGraphEdge>()).Where(value => value != null)
                .OrderBy(value => value.EdgeId, StringComparer.Ordinal).ToArray());
            Reservations = new ReadOnlyCollection<RmapWorldGraphReservation>((sourceReservations ??
                Array.Empty<RmapWorldGraphReservation>()).Where(value => value != null)
                .OrderBy(value => value.ReservationId, StringComparer.Ordinal).ToArray());
            Proofs = new ReadOnlyCollection<RmapWorldGraphProof>((sourceProofs ??
                Array.Empty<RmapWorldGraphProof>()).Where(value => value != null)
                .OrderBy(value => value.ProofId, StringComparer.Ordinal).ToArray());
            Failures = new ReadOnlyCollection<RmapWorldGraphFailure>((sourceFailures ??
                Array.Empty<RmapWorldGraphFailure>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            Digest = RmapWorldGraphIdentity.Hash("RMAP13_PLAN_V1", definition.Digest, definition.RngBindings[
                RmapWorldRngStream.RunGraph].InitialStateHex, policy.ToString(),
                string.Join(";", Nodes.Select(value => value.NodeId)),
                string.Join(";", Edges.Select(value => value.EdgeId)),
                string.Join(";", Reservations.Select(value => value.ReservationId)),
                string.Join(";", Proofs.Select(value => value.ProofId)));
        }

        public RmapWorldDefinition Definition { get; }
        public RmapWorldReturnShortcutPolicy Policy { get; }
        public IReadOnlyList<RmapWorldGraphNode> Nodes { get; }
        public IReadOnlyList<RmapWorldGraphEdge> Edges { get; }
        public IReadOnlyList<RmapWorldGraphReservation> Reservations { get; }
        public IReadOnlyList<RmapWorldGraphProof> Proofs { get; }
        public IReadOnlyList<RmapWorldGraphFailure> Failures { get; }
        public string Digest { get; }
        public bool Success => Failures.Count == 0 && Proofs.Count == 6 && Proofs.All(value => value.Success);

    }

    internal static class RmapWorldGraphIdentity
    {
        public static string Hash(params string[] lines) =>
            RmapWorldDefinition.Hash(string.Join("\n", lines));

        public static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A graph identity is required.", name);
            return value.Trim();
        }
    }

    public static class RmapWorldGraphPlanner
    {
        private static readonly RmapWorldGraphRole[] ResourceRoles =
        {
            RmapWorldGraphRole.MooncoreOre,
            RmapWorldGraphRole.CondensedCoefficientSap,
            RmapWorldGraphRole.DeepStarYeast,
        };

        public static RmapWorldGraphPlan Plan(
            RmapWorldDefinition definition,
            RmapWorldReturnShortcutPolicy policy = RmapWorldReturnShortcutPolicy.Optional)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!Enum.IsDefined(typeof(RmapWorldReturnShortcutPolicy), policy))
                throw new ArgumentOutOfRangeException(nameof(policy));
            var anchors = definition.GetStableIds(RmapWorldStableIdKind.MicroChunk)
                .OrderBy(value => value.Value, StringComparer.Ordinal).Take(8).ToArray();
            if (anchors.Length != 8) throw new InvalidOperationException(
                "RMAP13 needs eight actual RMAP10 MicroChunk stable-ID anchors.");
            var nodes = Enum.GetValues(typeof(RmapWorldGraphRole)).Cast<RmapWorldGraphRole>()
                .Select((role, index) => new RmapWorldGraphNode(role, anchors[index])).ToArray();
            var byRole = nodes.ToDictionary(value => value.Role, value => value);
            var reservations = nodes.Select(node => new RmapWorldGraphReservation(node,
                EntryDirection(node.Role), ReleaseCondition(node.Role))).ToList();
            var edges = BuildNormalEdges(byRole).ToList();
            if (policy == RmapWorldReturnShortcutPolicy.Required)
                AddRequiredShortcuts(byRole, edges, reservations);
            var failures = Validate(nodes, edges, reservations).ToList();
            var proofs = failures.Count == 0 ? ResourceOrders().Select(order =>
                Evaluate(nodes, edges, order)).ToArray() : Array.Empty<RmapWorldGraphProof>();
            failures.AddRange(proofs.Where(value => !value.Success).SelectMany(value => value.Failures));
            return new RmapWorldGraphPlan(definition, policy, nodes, edges, reservations, proofs, failures);
        }

        /// <summary>Public for focused broken-edge fixtures. It remains a pure
        /// planning search; no tilemap, Player, or runtime state is mutated.</summary>
        public static RmapWorldGraphProof Evaluate(
            IEnumerable<RmapWorldGraphNode> sourceNodes,
            IEnumerable<RmapWorldGraphEdge> sourceEdges,
            IEnumerable<RmapWorldGraphRole> requestedOrder)
        {
            var nodes = (sourceNodes ?? Array.Empty<RmapWorldGraphNode>()).Where(value => value != null)
                .ToDictionary(value => value.NodeId, value => value, StringComparer.Ordinal);
            var roles = nodes.Values.GroupBy(value => value.Role).ToDictionary(value => value.Key,
                value => value.Single());
            var order = (requestedOrder ?? Array.Empty<RmapWorldGraphRole>()).ToArray();
            var failures = ValidateOrder(order).ToList();
            roles.TryGetValue(RmapWorldGraphRole.Start, out var start);
            roles.TryGetValue(RmapWorldGraphRole.Exit, out var exit);
            if (start == null || exit == null)
                failures.Add(new RmapWorldGraphFailure("MISSING_REQUIRED_ROLE", null, "START_OR_EXIT"));
            if (failures.Count != 0) return new RmapWorldGraphProof(order, null,
                Array.Empty<string>(), failures);

            var edges = (sourceEdges ?? Array.Empty<RmapWorldGraphEdge>()).Where(value => value != null)
                .Where(value => nodes.ContainsKey(value.SourceNodeId) && nodes.ContainsKey(value.TargetNodeId))
                .OrderBy(value => value).ToArray();
            var bySource = edges.GroupBy(value => value.SourceNodeId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.OrderBy(edge => edge).ToArray(), StringComparer.Ordinal);
            var initial = new RmapWorldGraphState(start.NodeId, 0, 0, false, false, false);
            var queue = new Queue<RmapWorldGraphState>();
            var visited = new HashSet<RmapWorldGraphState>();
            var predecessor = new Dictionary<RmapWorldGraphState, Previous>();
            queue.Enqueue(initial);
            visited.Add(initial);
            RmapWorldGraphState found = null;
            while (queue.Count != 0)
            {
                RmapWorldGraphState state = queue.Dequeue();
                if (IsGoal(state, exit.NodeId, order.Length)) { found = state; break; }
                foreach (var transition in StateTransitions(state, roles, order))
                    Add(state, transition.State, transition.Action);
                if (!bySource.TryGetValue(state.PositionNodeId, out var outgoing)) continue;
                foreach (var edge in outgoing.Where(edge => edge.CanTraverse(state)))
                    Add(state, new RmapWorldGraphState(edge.TargetNodeId, state.ResourceMask,
                        state.OrderCursor, state.ForgeMade, state.SealOpen, state.BossComplete),
                        "MOVE|" + edge.EdgeId);
            }
            if (found == null)
            {
                RmapWorldGraphState frontier = visited.OrderByDescending(value => value.OrderCursor)
                    .ThenByDescending(value => value.ResourceMask).ThenBy(value => value).FirstOrDefault();
                return new RmapWorldGraphProof(order, null, Array.Empty<string>(), new[]
                {
                    new RmapWorldGraphFailure("GRAPH_GOAL_UNREACHABLE", frontier,
                        MissingCondition(frontier, roles, order)),
                });
            }
            var actions = new List<string>();
            for (var cursor = found; !cursor.Equals(initial); cursor = predecessor[cursor].PreviousState)
                actions.Add(predecessor[cursor].Action);
            actions.Reverse();
            return new RmapWorldGraphProof(order, found, actions, Array.Empty<RmapWorldGraphFailure>());

            void Add(RmapWorldGraphState previous, RmapWorldGraphState next, string action)
            {
                if (!visited.Add(next)) return;
                predecessor.Add(next, new Previous(previous, action));
                queue.Enqueue(next);
            }
        }

        public static IReadOnlyList<RmapWorldGraphFailure> ValidateRequirements(
            IEnumerable<RmapWorldGraphNode> nodes,
            IEnumerable<RmapWorldGraphEdge> edges,
            IEnumerable<RmapWorldGraphReservation> reservations) =>
            new ReadOnlyCollection<RmapWorldGraphFailure>(Validate(nodes, edges, reservations)
                .OrderBy(value => value).ToArray());

        private static IEnumerable<RmapWorldGraphEdge> BuildNormalEdges(
            IReadOnlyDictionary<RmapWorldGraphRole, RmapWorldGraphNode> nodes)
        {
            var start = nodes[RmapWorldGraphRole.Start];
            foreach (var resource in ResourceRoles)
            {
                var node = nodes[resource];
                RmapWorldGraphDirection outDirection = ResourceDirection(resource);
                yield return Edge(start, node, outDirection, "NORMAL_RESOURCE_APPROACH",
                    "RMAP13_NORMAL_" + resource + "_OUT", true);
                yield return Edge(node, start, Opposite(outDirection), "NORMAL_RESOURCE_RETURN",
                    "RMAP13_NORMAL_" + resource + "_RETURN", true);
            }
            yield return Edge(start, nodes[RmapWorldGraphRole.Forge], RmapWorldGraphDirection.Down,
                "NORMAL_FORGE_APPROACH", "RMAP13_NORMAL_FORGE_OUT", true);
            yield return Edge(nodes[RmapWorldGraphRole.Forge], start, RmapWorldGraphDirection.Up,
                "NORMAL_FORGE_RETURN", "RMAP13_NORMAL_FORGE_RETURN", true);
            yield return Edge(nodes[RmapWorldGraphRole.Forge], nodes[RmapWorldGraphRole.Seal],
                RmapWorldGraphDirection.Right, "FORGE_GATED_SEAL_APPROACH", "RMAP13_FORGE_TO_SEAL", true,
                requiresForge: true);
            yield return Edge(nodes[RmapWorldGraphRole.Seal], nodes[RmapWorldGraphRole.Boss],
                RmapWorldGraphDirection.Up, "SEAL_GATED_BOSS_APPROACH", "RMAP13_SEAL_TO_BOSS", true,
                requiresSeal: true);
            yield return Edge(nodes[RmapWorldGraphRole.Boss], nodes[RmapWorldGraphRole.Exit],
                RmapWorldGraphDirection.Right, "BOSS_GATED_EXIT_APPROACH", "RMAP13_BOSS_TO_EXIT", true,
                requiresBossComplete: true);
        }

        private static void AddRequiredShortcuts(
            IReadOnlyDictionary<RmapWorldGraphRole, RmapWorldGraphNode> nodes,
            ICollection<RmapWorldGraphEdge> edges,
            ICollection<RmapWorldGraphReservation> reservations)
        {
            foreach (var resource in ResourceRoles)
            {
                ulong bit = Bit(resource);
                var node = nodes[resource];
                edges.Add(Edge(node, nodes[RmapWorldGraphRole.Forge], RmapWorldGraphDirection.Down,
                    "REQUIRED_RESOURCE_RETURN_SHORTCUT", "RMAP13_SHORTCUT_" + resource, true,
                    requiredResourceMask: bit));
                reservations.Add(new RmapWorldGraphReservation(node, RmapWorldGraphDirection.Down,
                    "AFTER_" + resource + "_ACQUIRED"));
            }
        }

        private static IEnumerable<Transition> StateTransitions(
            RmapWorldGraphState state,
            IReadOnlyDictionary<RmapWorldGraphRole, RmapWorldGraphNode> roles,
            IReadOnlyList<RmapWorldGraphRole> order)
        {
            if (state.OrderCursor < order.Count && roles.TryGetValue(order[state.OrderCursor], out var nextResource) &&
                string.Equals(state.PositionNodeId, nextResource.NodeId, StringComparison.Ordinal))
            {
                ulong bit = Bit(nextResource.Role);
                if ((state.ResourceMask & bit) == 0)
                    yield return new Transition(new RmapWorldGraphState(state.PositionNodeId,
                        state.ResourceMask | bit, state.OrderCursor + 1, state.ForgeMade,
                        state.SealOpen, state.BossComplete), "ACQUIRE|" + nextResource.Role);
            }
            if (At(state, roles, RmapWorldGraphRole.Forge) && !state.ForgeMade &&
                state.ResourceMask == RequiredResourceMask)
                yield return new Transition(new RmapWorldGraphState(state.PositionNodeId,
                    state.ResourceMask, state.OrderCursor, true, state.SealOpen, state.BossComplete), "FORGE|MAKE_SEAL");
            if (At(state, roles, RmapWorldGraphRole.Seal) && state.ForgeMade && !state.SealOpen)
                yield return new Transition(new RmapWorldGraphState(state.PositionNodeId,
                    state.ResourceMask, state.OrderCursor, true, true, state.BossComplete), "SEAL|OPEN");
            if (At(state, roles, RmapWorldGraphRole.Boss) && state.SealOpen && !state.BossComplete)
                yield return new Transition(new RmapWorldGraphState(state.PositionNodeId,
                    state.ResourceMask, state.OrderCursor, true, true, true), "BOSS|PLANNED_COMPLETION_EVENT");
        }

        private static IEnumerable<RmapWorldGraphFailure> Validate(
            IEnumerable<RmapWorldGraphNode> sourceNodes,
            IEnumerable<RmapWorldGraphEdge> sourceEdges,
            IEnumerable<RmapWorldGraphReservation> sourceReservations)
        {
            var nodes = (sourceNodes ?? Array.Empty<RmapWorldGraphNode>()).Where(value => value != null).ToArray();
            foreach (var role in Enum.GetValues(typeof(RmapWorldGraphRole)).Cast<RmapWorldGraphRole>())
                if (nodes.Count(value => value.Role == role) != 1)
                    yield return new RmapWorldGraphFailure("ROLE_CARDINALITY", null, role.ToString());
            foreach (var duplicate in nodes.GroupBy(value => value.NodeId).Where(value => value.Count() != 1))
                yield return new RmapWorldGraphFailure("DUPLICATE_NODE_ID", null, duplicate.Key);
            var nodeIds = new HashSet<string>(nodes.Select(value => value.NodeId), StringComparer.Ordinal);
            var edges = (sourceEdges ?? Array.Empty<RmapWorldGraphEdge>()).Where(value => value != null).ToArray();
            foreach (var edge in edges)
            {
                if (!nodeIds.Contains(edge.SourceNodeId) || !nodeIds.Contains(edge.TargetNodeId))
                    yield return new RmapWorldGraphFailure("DANGLING_EDGE", null, edge.EdgeId);
                if (edge.SourceConnectionIsOneWay && edges.Any(other => other != edge &&
                    string.Equals(other.SourceConnectionId, edge.SourceConnectionId, StringComparison.Ordinal) &&
                    string.Equals(other.SourceNodeId, edge.TargetNodeId, StringComparison.Ordinal) &&
                    string.Equals(other.TargetNodeId, edge.SourceNodeId, StringComparison.Ordinal)))
                    yield return new RmapWorldGraphFailure("REVERSE_OF_ONE_WAY_EDGE", null, edge.SourceConnectionId);
            }
            foreach (var node in nodes)
                if (!(sourceReservations ?? Array.Empty<RmapWorldGraphReservation>()).Any(value => value != null &&
                    string.Equals(value.Node.NodeId, node.NodeId, StringComparison.Ordinal)))
                    yield return new RmapWorldGraphFailure("MISSING_RESERVATION", null, node.NodeId);
        }

        private static IEnumerable<RmapWorldGraphFailure> ValidateOrder(RmapWorldGraphRole[] order)
        {
            if (order.Length != ResourceRoles.Length || order.Distinct().Count() != ResourceRoles.Length ||
                order.Any(role => !ResourceRoles.Contains(role)))
                yield return new RmapWorldGraphFailure("INVALID_RESOURCE_ORDER", null,
                    string.Join(",", order.Select(value => value.ToString())));
        }

        private static bool IsGoal(RmapWorldGraphState state, string exitNodeId, int orderLength) =>
            state != null && state.OrderCursor == orderLength && state.ResourceMask == RequiredResourceMask &&
            state.ForgeMade && state.SealOpen && state.BossComplete &&
            string.Equals(state.PositionNodeId, exitNodeId, StringComparison.Ordinal);

        private static string MissingCondition(
            RmapWorldGraphState state,
            IReadOnlyDictionary<RmapWorldGraphRole, RmapWorldGraphNode> roles,
            IReadOnlyList<RmapWorldGraphRole> order)
        {
            if (state == null) return "MISSING_START_OR_EDGE_RESERVATION";
            if (state.OrderCursor < order.Count) return "RESOURCE_ORDER_OR_RETURN_RESERVATION:" + order[state.OrderCursor];
            if (!state.ForgeMade) return "FORGE_REQUIRES_ALL_RESOURCES";
            if (!state.SealOpen) return "SEAL_REQUIRES_FORGE";
            if (!state.BossComplete) return "BOSS_REQUIRES_SEAL_AND_PLANNED_EVENT";
            return "EXIT_REQUIRES_BOSS_COMPLETE";
        }

        private static IEnumerable<RmapWorldGraphRole[]> ResourceOrders() => ResourceRoles
            .SelectMany(first => ResourceRoles.Where(second => second != first).SelectMany(second =>
                ResourceRoles.Where(third => third != first && third != second).Select(third =>
                    new[] { first, second, third })));

        private static RmapWorldGraphEdge Edge(
            RmapWorldGraphNode source,
            RmapWorldGraphNode target,
            RmapWorldGraphDirection direction,
            string condition,
            string connection,
            bool oneWay,
            ulong requiredResourceMask = 0,
            bool requiresForge = false,
            bool requiresSeal = false,
            bool requiresBossComplete = false) => new RmapWorldGraphEdge(source.NodeId, target.NodeId,
                direction, condition, connection, oneWay, requiredResourceMask, requiresForge,
                requiresSeal, requiresBossComplete);

        private static bool At(RmapWorldGraphState state,
            IReadOnlyDictionary<RmapWorldGraphRole, RmapWorldGraphNode> nodes,
            RmapWorldGraphRole role) => string.Equals(state.PositionNodeId, nodes[role].NodeId,
                StringComparison.Ordinal);
        private static ulong RequiredResourceMask => Bit(RmapWorldGraphRole.MooncoreOre) |
            Bit(RmapWorldGraphRole.CondensedCoefficientSap) | Bit(RmapWorldGraphRole.DeepStarYeast);
        private static ulong Bit(RmapWorldGraphRole role) => role == RmapWorldGraphRole.MooncoreOre ? 1UL :
            role == RmapWorldGraphRole.CondensedCoefficientSap ? 2UL :
            role == RmapWorldGraphRole.DeepStarYeast ? 4UL : 0UL;
        private static RmapWorldGraphDirection ResourceDirection(RmapWorldGraphRole role) =>
            role == RmapWorldGraphRole.MooncoreOre ? RmapWorldGraphDirection.Right :
            role == RmapWorldGraphRole.CondensedCoefficientSap ? RmapWorldGraphDirection.Up :
            RmapWorldGraphDirection.Left;
        private static RmapWorldGraphDirection EntryDirection(RmapWorldGraphRole role) =>
            role == RmapWorldGraphRole.Start ? RmapWorldGraphDirection.Right :
            role == RmapWorldGraphRole.Forge ? RmapWorldGraphDirection.Down :
            role == RmapWorldGraphRole.Seal ? RmapWorldGraphDirection.Right :
            role == RmapWorldGraphRole.Boss ? RmapWorldGraphDirection.Up :
            role == RmapWorldGraphRole.Exit ? RmapWorldGraphDirection.Right : ResourceDirection(role);
        private static RmapWorldGraphDirection Opposite(RmapWorldGraphDirection direction) =>
            direction == RmapWorldGraphDirection.Left ? RmapWorldGraphDirection.Right :
            direction == RmapWorldGraphDirection.Right ? RmapWorldGraphDirection.Left :
            direction == RmapWorldGraphDirection.Up ? RmapWorldGraphDirection.Down : RmapWorldGraphDirection.Up;
        private static string ReleaseCondition(RmapWorldGraphRole role) => role == RmapWorldGraphRole.Start ?
            "WORLD_START" : role == RmapWorldGraphRole.Forge ? "ALL_THREE_RESOURCES" :
            role == RmapWorldGraphRole.Seal ? "FORGE_MADE" : role == RmapWorldGraphRole.Boss ?
            "SEAL_OPEN" : role == RmapWorldGraphRole.Exit ? "BOSS_COMPLETE" : "RESOURCE_ACCESS";
        private static string Hash(params string[] lines) => RmapWorldDefinition.Hash(string.Join("\n", lines));
        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A graph identity is required.", name);
            return value.Trim();
        }

        private sealed class Transition
        {
            public Transition(RmapWorldGraphState state, string action) { State = state; Action = action; }
            public RmapWorldGraphState State { get; }
            public string Action { get; }
        }

        private sealed class Previous
        {
            public Previous(RmapWorldGraphState state, string action) { PreviousState = state; Action = action; }
            public RmapWorldGraphState PreviousState { get; }
            public string Action { get; }
        }
    }

    /// <summary>Pure, sorted RFC4180 evidence material for an editor/reporting
    /// layer. The planner itself performs no file I/O.</summary>
    public static class RmapWorldGraphExport
    {
        public static string NodesCsv(RmapWorldGraphPlan plan) => Lines("node_id,role,anchor_stable_id,verification_level",
            plan.Nodes.OrderBy(value => value.NodeId).Select(value => Row(value.NodeId, value.Role,
                value.AnchorStableId, RmapWorldGraphVerificationLevel.ExistingRmap10Anchor)));
        public static string EdgesCsv(RmapWorldGraphPlan plan) => Lines("edge_id,source_node_id,target_node_id,direction,condition,connection_id,verification_level",
            plan.Edges.OrderBy(value => value.EdgeId).Select(value => Row(value.EdgeId, value.SourceNodeId,
                value.TargetNodeId, value.Direction, value.TraversalCondition, value.SourceConnectionId,
                value.VerificationLevel)));
        public static string ReservationsCsv(RmapWorldGraphPlan plan) => Lines("reservation_id,node_id,anchor_stable_id,entry_direction,release_condition,status",
            plan.Reservations.OrderBy(value => value.ReservationId).Select(value => Row(value.ReservationId,
                value.Node.NodeId, value.Node.AnchorStableId, value.EntryDirection,
                value.ReleaseCondition, value.Status)));
        public static string ProofsCsv(RmapWorldGraphPlan plan) => Lines("proof_id,resource_order,success,actions,final_state",
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
