using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    /// <summary>A coordinate-first verdict for one exported SHARED/FACE contact.</summary>
    public sealed class Sv5SpacePhysicalContactCheck : IComparable<Sv5SpacePhysicalContactCheck>
    {
        internal Sv5SpacePhysicalContactCheck(Sv5SpaceContactDecision source, bool firstTraversable,
            bool secondTraversable, string geometryOwnerId, IEnumerable<string> sourceStates, bool success)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            FirstTraversable = firstTraversable;
            SecondTraversable = secondTraversable;
            GeometryOwnerId = geometryOwnerId ?? string.Empty;
            States = new ReadOnlyCollection<string>((sourceStates ?? Array.Empty<string>())
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Success = success;
        }

        public Sv5SpaceContactDecision Source { get; }
        public bool FirstTraversable { get; }
        public bool SecondTraversable { get; }
        public string GeometryOwnerId { get; }
        public IReadOnlyList<string> States { get; }
        public bool Success { get; }
        public int CompareTo(Sv5SpacePhysicalContactCheck other) => other == null ? 1 :
            string.Compare(Source.Source.Id, other.Source.Source.Id, StringComparison.Ordinal);
    }

    /// <summary>A single global-coordinate reachability verdict with every gate applied at once.</summary>
    public sealed class Sv5SpacePhysicalGateStateCheck : IComparable<Sv5SpacePhysicalGateStateCheck>
    {
        private readonly ReadOnlyCollection<string> closedGateIds;
        private readonly ReadOnlyCollection<string> openGateIds;
        private readonly ReadOnlyCollection<RmapSpecialWorldPoint> witness;

        internal Sv5SpacePhysicalGateStateCheck(string id, string connectionId, string sourcePortId,
            string targetPortId, ulong resourceMask, bool forgeMade, bool sealOpen, bool bossComplete,
            bool expectedReachable, Sv5SpacePhysicalReachability reachability,
            IEnumerable<string> sourceClosedGateIds, IEnumerable<string> sourceOpenGateIds)
        {
            Id = id;
            ConnectionId = connectionId;
            SourcePortId = sourcePortId;
            TargetPortId = targetPortId;
            ResourceMask = resourceMask;
            ForgeMade = forgeMade;
            SealOpen = sealOpen;
            BossComplete = bossComplete;
            ExpectedReachable = expectedReachable;
            Reachable = reachability.TargetPortReachable;
            SourceAnchorReachable = reachability.SourceAnchorReachable;
            CheckedCells = reachability.CheckedCells;
            CheckedFaces = reachability.CheckedFaces;
            closedGateIds = Freeze(sourceClosedGateIds);
            openGateIds = Freeze(sourceOpenGateIds);
            witness = new ReadOnlyCollection<RmapSpecialWorldPoint>(reachability.Witness.ToArray());
        }

        public string Id { get; }
        public string ConnectionId { get; }
        public string SourcePortId { get; }
        public string TargetPortId { get; }
        public ulong ResourceMask { get; }
        public bool ForgeMade { get; }
        public bool SealOpen { get; }
        public bool BossComplete { get; }
        public bool ExpectedReachable { get; }
        public bool Reachable { get; }
        public bool SourceAnchorReachable { get; }
        public int CheckedCells { get; }
        public int CheckedFaces { get; }
        public IReadOnlyList<string> ClosedGateIds => closedGateIds;
        public IReadOnlyList<string> OpenGateIds => openGateIds;
        public IReadOnlyList<RmapSpecialWorldPoint> Witness => witness;
        public bool Success => SourceAnchorReachable && ExpectedReachable == Reachable;
        public int CompareTo(Sv5SpacePhysicalGateStateCheck other) => other == null ? 1 :
            string.Compare(Id, other.Id, StringComparison.Ordinal);

        private static ReadOnlyCollection<string> Freeze(IEnumerable<string> values) =>
            new ReadOnlyCollection<string>((values ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }

    public sealed class Sv5SpacePhysicalMovementPlan
    {
        private readonly ReadOnlyCollection<string> movementSemantics;

        internal Sv5SpacePhysicalMovementPlan(IEnumerable<Sv5SpacePhysicalContactCheck> sourceContacts,
            IEnumerable<Sv5SpacePhysicalGateStateCheck> sourceStates, IEnumerable<string> sourceDiagnostics,
            IEnumerable<string> sourceMovementSemantics)
        {
            ContactChecks = new ReadOnlyCollection<Sv5SpacePhysicalContactCheck>((sourceContacts ??
                Array.Empty<Sv5SpacePhysicalContactCheck>()).OrderBy(value => value).ToArray());
            GateStateChecks = new ReadOnlyCollection<Sv5SpacePhysicalGateStateCheck>((sourceStates ??
                Array.Empty<Sv5SpacePhysicalGateStateCheck>()).OrderBy(value => value).ToArray());
            Diagnostics = new ReadOnlyCollection<string>((sourceDiagnostics ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            movementSemantics = new ReadOnlyCollection<string>((sourceMovementSemantics ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value)).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray());
            SemanticDigest = RmapWorldDefinition.Hash(string.Join("\n", CanonicalLines()));
        }

        public IReadOnlyList<Sv5SpacePhysicalContactCheck> ContactChecks { get; }
        public IReadOnlyList<Sv5SpacePhysicalGateStateCheck> GateStateChecks { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public string SemanticDigest { get; }
        public bool Success => ContactChecks.Count != 0 && ContactChecks.All(value => value.Success) &&
            GateStateChecks.Count == 9 && GateStateChecks.All(value => value.Success) && Diagnostics.Count == 0;

        private IEnumerable<string> CanonicalLines()
        {
            yield return "SV5_PHYSICAL_MOVEMENT_FIX03_V1";
            foreach (string value in movementSemantics) yield return value;
            foreach (Sv5SpacePhysicalContactCheck value in ContactChecks)
                yield return "contact|" + value.Source.Source.Kind + "|" + value.Source.Source.FirstWorld + "|" +
                    value.Source.Source.SecondWorld + "|" + value.Source.Source.FirstKinds + "|" +
                    value.Source.Source.SecondKinds + "|" + value.Source.Source.RouteAKinds + "|" +
                    value.Source.Source.RouteBKinds + "|" + value.Source.Crossing + "|" +
                    value.Source.BoundaryId + "|" + value.GeometryOwnerId + "|" +
                    (value.FirstTraversable ? "1" : "0") + (value.SecondTraversable ? "1" : "0") + "|" +
                    string.Join(";", value.States) + "|" + (value.Success ? "1" : "0");
            foreach (Sv5SpacePhysicalGateStateCheck value in GateStateChecks)
                yield return "state|" + value.Id + "|" + value.ConnectionId + "|" + value.SourcePortId + "|" +
                    value.TargetPortId + "|" + value.ResourceMask.ToString(CultureInfo.InvariantCulture) + "|" +
                    (value.ForgeMade ? "1" : "0") + (value.SealOpen ? "1" : "0") +
                    (value.BossComplete ? "1" : "0") + "|" + (value.ExpectedReachable ? "1" : "0") +
                    (value.Reachable ? "1" : "0") + "|" + string.Join(";", value.ClosedGateIds) + "|" +
                    string.Join(";", value.OpenGateIds) + "|" + string.Join(";", value.Witness) + "|" +
                    value.CheckedCells.ToString(CultureInfo.InvariantCulture) + "|" +
                    value.CheckedFaces.ToString(CultureInfo.InvariantCulture);
            foreach (string value in Diagnostics) yield return "diagnostic|" + value;
        }
    }

    public sealed class Sv5SpacePhysicalReachability
    {
        internal Sv5SpacePhysicalReachability(bool sourceAnchorReachable, bool targetPortReachable,
            int checkedCells, int checkedFaces, IEnumerable<RmapSpecialWorldPoint> sourceWitness)
        {
            SourceAnchorReachable = sourceAnchorReachable;
            TargetPortReachable = targetPortReachable;
            CheckedCells = checkedCells;
            CheckedFaces = checkedFaces;
            Witness = new ReadOnlyCollection<RmapSpecialWorldPoint>((sourceWitness ??
                Array.Empty<RmapSpecialWorldPoint>()).ToArray());
        }
        public bool SourceAnchorReachable { get; }
        public bool TargetPortReachable { get; }
        public int CheckedCells { get; }
        public int CheckedFaces { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> Witness { get; }
    }

    /// <summary>
    /// Production coordinate-first movement validator. Route IDs are diagnostic only: a Passage/aperture world
    /// coordinate is one node, cardinally adjacent nodes share one face, and every closed gate blocks that face or
    /// cell globally. Clearance and INFILL_PENDING never become movement nodes.
    /// </summary>
    public static class Sv5SpacePhysicalMovement
    {
        private static readonly StateCase[] ClosedCases =
        {
            new StateCase("FORGE_CLOSED_INITIAL", "FORGE_GATED_SEAL_APPROACH", 0, false, false, false, false),
            new StateCase("SEAL_CLOSED_INITIAL", "SEAL_GATED_BOSS_APPROACH", 0, false, false, false, false),
            new StateCase("SEAL_CLOSED_FORGE", "SEAL_GATED_BOSS_APPROACH", 7, true, false, false, false),
            new StateCase("EXIT_CLOSED_INITIAL", "BOSS_GATED_EXIT_APPROACH", 0, false, false, false, false),
            new StateCase("EXIT_CLOSED_FORGE", "BOSS_GATED_EXIT_APPROACH", 7, true, false, false, false),
            new StateCase("EXIT_CLOSED_SEAL", "BOSS_GATED_EXIT_APPROACH", 7, true, true, false, false),
        };
        private static readonly StateCase[] OpenCases =
        {
            new StateCase("FORGE_OPEN", "FORGE_GATED_SEAL_APPROACH", 7, true, false, false, true),
            new StateCase("SEAL_OPEN", "SEAL_GATED_BOSS_APPROACH", 7, true, true, false, true),
            new StateCase("EXIT_OPEN", "BOSS_GATED_EXIT_APPROACH", 7, true, true, true, true),
        };

        internal static IReadOnlyList<Sv5SpaceGate> ExpandGlobalCuts(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections, IEnumerable<Sv5SpaceGate> sourceGates)
        {
            Sv5SpaceConnection[] connections = Connections(sourceConnections);
            var gates = (sourceGates ?? Array.Empty<Sv5SpaceGate>()).Where(value => value != null)
                .OrderBy(value => value).ToList();
            foreach (StateCase state in ClosedCases)
            {
                Sv5SpaceConnection connection = connections.Single(value => value.Condition == state.Condition);
                Sv5SpaceGate gate = gates.Single(value => value.SourceConnectionId == connection.Id);
                var iterations = 0;
                while (true)
                {
                    Sv5SpacePhysicalReachability reach = Explore(core, connections, gates, connection,
                        state.ResourceMask, state.ForgeMade, state.SealOpen, state.BossComplete, null);
                    if (!reach.TargetPortReachable) break;
                    if (++iterations > 128)
                        throw new InvalidOperationException("GLOBAL_GATE_CUT_DID_NOT_CONVERGE|" + gate.Id);
                    Sv5SpaceBoundaryFace selected = CandidateFaces(reach.Witness, gate)
                        .FirstOrDefault(candidate => PreservesOpenWitnesses(core, connections, gates, gate, candidate));
                    if (selected == null)
                        throw new InvalidOperationException("GLOBAL_GATE_CUT_CONFLICTS_WITH_OPEN_WITNESS|" + gate.Id);
                    gate = WithAdditionalFace(gate, selected);
                    int index = gates.FindIndex(value => value.SourceConnectionId == gate.SourceConnectionId);
                    gates[index] = gate;
                }
            }
            foreach (StateCase state in OpenCases)
            {
                Sv5SpaceConnection connection = connections.Single(value => value.Condition == state.Condition);
                // A non-representative layout may be returned with a failed physical plan so callers can inspect
                // deterministic diagnostics; Analyze is the authoritative acceptance boundary.
                Explore(core, connections, gates, connection, state.ResourceMask, state.ForgeMade,
                    state.SealOpen, state.BossComplete, null);
            }
            return new ReadOnlyCollection<Sv5SpaceGate>(gates.OrderBy(value => value).ToArray());
        }

        public static Sv5SpacePhysicalMovementPlan Analyze(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections, IEnumerable<Sv5SpaceContactDecision> sourceContacts,
            IEnumerable<Sv5SpaceGate> sourceGates)
        {
            if (core == null) throw new ArgumentNullException(nameof(core));
            Sv5SpaceConnection[] connections = Connections(sourceConnections);
            Sv5SpaceContactDecision[] contacts = (sourceContacts ?? Array.Empty<Sv5SpaceContactDecision>())
                .Where(value => value != null).OrderBy(value => value).ToArray();
            Sv5SpaceGate[] gates = (sourceGates ?? Array.Empty<Sv5SpaceGate>()).Where(value => value != null)
                .OrderBy(value => value).ToArray();
            var passage = PassageCells(connections);
            var gateByBoundary = gates.ToDictionary(value => value.BoundaryId, value => value, StringComparer.Ordinal);
            var contactChecks = new List<Sv5SpacePhysicalContactCheck>();
            var diagnostics = new List<string>();
            foreach (Sv5SpaceContactDecision contact in contacts)
            {
                bool samePredicate = SamePredicate(core, connections, contact.Source.RouteA, contact.Source.RouteB);
                bool first = passage.Contains(contact.Source.FirstWorld);
                bool second = passage.Contains(contact.Source.SecondWorld);
                Sv5SpaceGate owner = null;
                if (!string.IsNullOrEmpty(contact.BoundaryId)) gateByBoundary.TryGetValue(contact.BoundaryId, out owner);
                bool success = samePredicate ? contact.Crossing == Sv5SpaceCrossingKind.Join && owner == null :
                    contact.Crossing == Sv5SpaceCrossingKind.ConditionalGate && owner != null &&
                    owner.ContactIds.Contains(contact.Source.Id);
                if (!success) diagnostics.Add("PHYSICAL_CONTACT_DECISION_INVALID|" + contact.Source.Id);
                contactChecks.Add(new Sv5SpacePhysicalContactCheck(contact, first, second,
                    owner == null ? string.Empty : owner.Id,
                    owner == null ? new[] { "ALL_REACHABLE_STATES" } : StatesFor(owner), success));
            }

            var stateChecks = new List<Sv5SpacePhysicalGateStateCheck>();
            foreach (StateCase state in ClosedCases.Concat(OpenCases))
            {
                Sv5SpaceConnection connection = connections.Single(value => value.Condition == state.Condition);
                Sv5SpacePhysicalReachability reach = Explore(core, connections, gates, connection,
                    state.ResourceMask, state.ForgeMade, state.SealOpen, state.BossComplete, null);
                string[] closed = gates.Where(value => !value.TypedPredicate.IsOpen(state.ResourceMask,
                    state.ForgeMade, state.SealOpen, state.BossComplete)).Select(value => value.Id).ToArray();
                string[] open = gates.Where(value => value.TypedPredicate.IsOpen(state.ResourceMask,
                    state.ForgeMade, state.SealOpen, state.BossComplete)).Select(value => value.Id).ToArray();
                var check = new Sv5SpacePhysicalGateStateCheck(state.Id, connection.Id, connection.FromPortId,
                    connection.ToPortId, state.ResourceMask, state.ForgeMade, state.SealOpen, state.BossComplete,
                    state.ExpectedReachable, reach, closed, open);
                stateChecks.Add(check);
                if (!check.Success) diagnostics.Add("PHYSICAL_GATE_STATE_MISMATCH|" + state.Id);
            }
            return new Sv5SpacePhysicalMovementPlan(contactChecks, stateChecks, diagnostics,
                MovementSemantics(connections, gates, passage));
        }

        public static IReadOnlyList<string> FindStateErrors(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections, IEnumerable<Sv5SpaceGate> sourceGates)
        {
            Sv5SpaceConnection[] connections = Connections(sourceConnections);
            Sv5SpaceGate[] gates = (sourceGates ?? Array.Empty<Sv5SpaceGate>()).Where(value => value != null).ToArray();
            var errors = new List<string>();
            foreach (StateCase state in ClosedCases.Concat(OpenCases))
            {
                Sv5SpaceConnection connection = connections.Single(value => value.Condition == state.Condition);
                bool reached = Explore(core, connections, gates, connection, state.ResourceMask, state.ForgeMade,
                    state.SealOpen, state.BossComplete, null).TargetPortReachable;
                if (reached != state.ExpectedReachable)
                    errors.Add((state.ExpectedReachable ? "GLOBAL_GATE_OPEN_PATH_MISSING|" :
                        "GLOBAL_GATE_STATE_BYPASS|") + state.Id);
            }
            return new ReadOnlyCollection<string>(errors.OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        public static Sv5SpacePhysicalReachability Evaluate(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections, IEnumerable<Sv5SpaceGate> sourceGates,
            string connectionId, ulong resourceMask, bool forgeMade, bool sealOpen, bool bossComplete)
        {
            Sv5SpaceConnection[] connections = Connections(sourceConnections);
            Sv5SpaceConnection connection = connections.Single(value => value.Id == connectionId);
            return Explore(core, connections, sourceGates, connection, resourceMask, forgeMade, sealOpen,
                bossComplete, null);
        }

        internal static Sv5SpacePhysicalReachability Explore(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections, IEnumerable<Sv5SpaceGate> sourceGates,
            Sv5SpaceConnection connection, ulong resourceMask, bool forgeMade, bool sealOpen, bool bossComplete,
            bool? targetOpenOverride)
        {
            if (core == null) throw new ArgumentNullException(nameof(core));
            Sv5SpaceConnection[] connections = Connections(sourceConnections);
            Sv5SpaceGate[] gates = (sourceGates ?? Array.Empty<Sv5SpaceGate>()).Where(value => value != null).ToArray();
            HashSet<RmapSpecialWorldPoint> passage = PassageCells(connections);
            var blockedCells = new HashSet<RmapSpecialWorldPoint>();
            var blockedFaces = new HashSet<string>(StringComparer.Ordinal);
            foreach (Sv5SpaceGate gate in gates)
            {
                bool open = gate.SourceConnectionId == connection.Id && targetOpenOverride.HasValue ?
                    targetOpenOverride.Value : gate.TypedPredicate.IsOpen(resourceMask, forgeMade, sealOpen, bossComplete);
                if (open) continue;
                blockedCells.UnionWith(gate.BlockingCells);
                blockedFaces.UnionWith(gate.BlockingFaces.Select(value => value.StableToken));
            }
            var accesses = core.Source.Accesses.ToDictionary(value => value.Id, value => value, StringComparer.Ordinal);
            RmapSpecialAccess from = accesses[connection.FromPortId];
            RmapSpecialAccess to = accesses[connection.ToPortId];
            Sv5SpaceGate target = gates.Single(value => value.SourceConnectionId == connection.Id);
            var previous = new Dictionary<RmapSpecialWorldPoint, RmapSpecialWorldPoint>();
            var starts = new HashSet<RmapSpecialWorldPoint>();
            var queue = new Queue<RmapSpecialWorldPoint>();
            foreach (RmapSpecialWorldPoint value in from.OpenCells.Where(value => passage.Contains(value) &&
                         !blockedCells.Contains(value)))
                if (starts.Add(value)) queue.Enqueue(value);
            RmapSpecialWorldPoint? reachedGoal = null;
            var goals = new HashSet<RmapSpecialWorldPoint>(to.OpenCells.Where(passage.Contains));
            while (queue.Count != 0)
            {
                RmapSpecialWorldPoint current = queue.Dequeue();
                if (goals.Contains(current)) { reachedGoal = current; break; }
                foreach (RmapSpecialWorldPoint next in Neighbors(current).Where(passage.Contains))
                {
                    if (blockedCells.Contains(next) || blockedFaces.Contains(FaceToken(current, next)) ||
                        starts.Contains(next) || previous.ContainsKey(next)) continue;
                    previous.Add(next, current);
                    queue.Enqueue(next);
                }
            }
            var witness = new List<RmapSpecialWorldPoint>();
            if (reachedGoal.HasValue)
            {
                RmapSpecialWorldPoint cursor = reachedGoal.Value;
                witness.Add(cursor);
                while (!starts.Contains(cursor)) { cursor = previous[cursor]; witness.Add(cursor); }
                witness.Reverse();
            }
            bool sourceAnchor = starts.Count != 0;
            return new Sv5SpacePhysicalReachability(sourceAnchor, reachedGoal.HasValue,
                starts.Count + previous.Count, blockedFaces.Count, witness);
        }

        internal static bool SamePredicate(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections, string leftRoute, string rightRoute)
        {
            var edges = core.RouteSource.Graph.Edges.ToDictionary(value => value.EdgeId, value => value,
                StringComparer.Ordinal);
            var routes = Connections(sourceConnections).ToDictionary(Sv5SpaceGraphPlanner.RouteKey, value => value,
                StringComparer.Ordinal);
            Sv5SpaceGatePredicate left = Predicate(leftRoute), right = Predicate(rightRoute);
            return left != null && left.Equals(right);
            Sv5SpaceGatePredicate Predicate(string route)
            {
                if (!routes.TryGetValue(route, out Sv5SpaceConnection value)) return null;
                return edges.TryGetValue(value.SourceGraphEdgeId, out RmapWorldGraphEdge edge) ?
                    Sv5SpaceGatePredicate.FromEdge(edge) : new Sv5SpaceGatePredicate(0, false, false, false);
            }
        }

        private static bool PreservesOpenWitnesses(Sv5CoreReservationPlan core, Sv5SpaceConnection[] connections,
            IList<Sv5SpaceGate> gates, Sv5SpaceGate changed, Sv5SpaceBoundaryFace candidate)
        {
            Sv5SpaceGate replacement = WithAdditionalFace(changed, candidate);
            Sv5SpaceGate[] trial = gates.Select(value => value.SourceConnectionId == changed.SourceConnectionId ?
                replacement : value).ToArray();
            foreach (StateCase state in OpenCases)
            {
                if (changed.TypedPredicate.IsOpen(state.ResourceMask, state.ForgeMade, state.SealOpen,
                        state.BossComplete)) continue;
                Sv5SpaceConnection connection = connections.Single(value => value.Condition == state.Condition);
                if (!Explore(core, connections, trial, connection, state.ResourceMask, state.ForgeMade,
                        state.SealOpen, state.BossComplete, null).TargetPortReachable) return false;
            }
            return true;
        }

        private static IEnumerable<Sv5SpaceBoundaryFace> CandidateFaces(
            IReadOnlyList<RmapSpecialWorldPoint> witness, Sv5SpaceGate gate)
        {
            return witness.Zip(witness.Skip(1), (first, second) => new Sv5SpaceBoundaryFace(first, second))
                .GroupBy(value => value.StableToken, StringComparer.Ordinal).Select(value => value.First())
                .Where(value => !gate.BlockingFaces.Any(item => item.StableToken == value.StableToken))
                .OrderBy(value => DistanceToGate(value, gate)).ThenBy(value => value.StableToken,
                    StringComparer.Ordinal);
        }

        private static Sv5SpaceGate WithAdditionalFace(Sv5SpaceGate gate, Sv5SpaceBoundaryFace face) =>
            new Sv5SpaceGate(gate.Id, gate.BoundaryId, gate.ContactIds, gate.BlockingCells,
                gate.BlockingFaces.Concat(new[] { face }), gate.SideAAnchor, gate.SideBAnchor, gate.Direction,
                gate.Flow, gate.Predicate, gate.TypedPredicate, gate.SourceConnectionId, gate.SourceRouteId,
                gate.SourcePortId, gate.TargetPortId, gate.Crossing, "SEALED_GLOBAL_WORLD_FACE_CUT",
                "OPEN_RESTORES_GLOBAL_WORLD_FACES");

        private static int DistanceToGate(Sv5SpaceBoundaryFace face, Sv5SpaceGate gate) =>
            Math.Abs(face.First.X + face.Second.X - gate.SideAAnchor.X * 2) +
            Math.Abs(face.First.Y + face.Second.Y - gate.SideAAnchor.Y * 2);
        private static Sv5SpaceConnection[] Connections(IEnumerable<Sv5SpaceConnection> source) =>
            (source ?? Array.Empty<Sv5SpaceConnection>()).Where(value => value != null).OrderBy(value => value).ToArray();
        private static HashSet<RmapSpecialWorldPoint> PassageCells(IEnumerable<Sv5SpaceConnection> source) =>
            new HashSet<RmapSpecialWorldPoint>(Connections(source).SelectMany(value =>
                value.Centerline.Concat(value.ApertureCells)));
        private static IEnumerable<string> MovementSemantics(IEnumerable<Sv5SpaceConnection> connections,
            IEnumerable<Sv5SpaceGate> gates, IEnumerable<RmapSpecialWorldPoint> passage)
        {
            foreach (RmapSpecialWorldPoint point in passage.OrderBy(value => value))
                yield return "movement-cell|" + point;
            foreach (Sv5SpaceConnection value in connections.OrderBy(value => value))
                yield return "connection|" + value.FromPortId + "|" + value.ToPortId + "|" + value.Direction +
                    "|" + value.Flow + "|path=" + string.Join(";", value.Centerline) + "|aperture=" +
                    string.Join(";", value.ApertureCells.OrderBy(point => point));
            foreach (Sv5SpaceGate value in gates.OrderBy(value => value))
                yield return "gate|" + value.Id + "|" + value.BoundaryId + "|cells=" +
                    string.Join(";", value.BlockingCells) + "|faces=" +
                    string.Join(";", value.BlockingFaces.Select(face => face.StableToken)) + "|predicate=" +
                    value.TypedPredicate.StableToken + "|ports=" + value.SourcePortId + ">" + value.TargetPortId +
                    "|flow=" + value.Flow + "|state=" + value.SealedState + ">" + value.OpenState;
        }
        private static IEnumerable<RmapSpecialWorldPoint> Neighbors(RmapSpecialWorldPoint point)
        {
            if (point.X > 0) yield return new RmapSpecialWorldPoint(point.X - 1, point.Y);
            if (point.X < Sv5SpaceGraphPlanner.WorldWidth - 1) yield return new RmapSpecialWorldPoint(point.X + 1, point.Y);
            if (point.Y > 0) yield return new RmapSpecialWorldPoint(point.X, point.Y - 1);
            if (point.Y < Sv5SpaceGraphPlanner.WorldHeight - 1) yield return new RmapSpecialWorldPoint(point.X, point.Y + 1);
        }
        private static string FaceToken(RmapSpecialWorldPoint first, RmapSpecialWorldPoint second) =>
            first.CompareTo(second) <= 0 ? first + ">" + second : second + ">" + first;
        private static IEnumerable<string> StatesFor(Sv5SpaceGate gate) => ClosedCases.Concat(OpenCases)
            .Where(value => gate.TypedPredicate.IsOpen(value.ResourceMask, value.ForgeMade, value.SealOpen,
                value.BossComplete) || !value.ExpectedReachable).Select(value => value.Id);

        private sealed class StateCase
        {
            public StateCase(string id, string condition, ulong resourceMask, bool forgeMade, bool sealOpen,
                bool bossComplete, bool expectedReachable)
            { Id = id; Condition = condition; ResourceMask = resourceMask; ForgeMade = forgeMade;
                SealOpen = sealOpen; BossComplete = bossComplete; ExpectedReachable = expectedReachable; }
            public string Id { get; }
            public string Condition { get; }
            public ulong ResourceMask { get; }
            public bool ForgeMade { get; }
            public bool SealOpen { get; }
            public bool BossComplete { get; }
            public bool ExpectedReachable { get; }
        }
    }
}
