using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    /// <summary>The typed RMAP13 predicate owned by one physical planned boundary.</summary>
    public sealed class Sv5SpaceGatePredicate : IEquatable<Sv5SpaceGatePredicate>
    {
        public Sv5SpaceGatePredicate(ulong requiredResourceMask, bool requiresForge, bool requiresSeal,
            bool requiresBossComplete)
        {
            RequiredResourceMask = requiredResourceMask;
            RequiresForge = requiresForge;
            RequiresSeal = requiresSeal;
            RequiresBossComplete = requiresBossComplete;
        }

        public ulong RequiredResourceMask { get; }
        public bool RequiresForge { get; }
        public bool RequiresSeal { get; }
        public bool RequiresBossComplete { get; }
        public string StableToken => "mask=" + RequiredResourceMask.ToString(CultureInfo.InvariantCulture) +
            ",forge=" + (RequiresForge ? "1" : "0") + ",seal=" + (RequiresSeal ? "1" : "0") +
            ",boss=" + (RequiresBossComplete ? "1" : "0");

        public bool IsOpen(ulong resourceMask, bool forgeMade, bool sealOpen, bool bossComplete) =>
            (resourceMask & RequiredResourceMask) == RequiredResourceMask &&
            (!RequiresForge || forgeMade) && (!RequiresSeal || sealOpen) &&
            (!RequiresBossComplete || bossComplete);

        internal static Sv5SpaceGatePredicate FromEdge(RmapWorldGraphEdge edge) => edge == null ? null :
            new Sv5SpaceGatePredicate(edge.RequiredResourceMask, edge.RequiresForge, edge.RequiresSeal,
                edge.RequiresBossComplete);

        public bool Equals(Sv5SpaceGatePredicate other) => other != null &&
            RequiredResourceMask == other.RequiredResourceMask && RequiresForge == other.RequiresForge &&
            RequiresSeal == other.RequiresSeal && RequiresBossComplete == other.RequiresBossComplete;
        public override bool Equals(object obj) => Equals(obj as Sv5SpaceGatePredicate);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = RequiredResourceMask.GetHashCode();
                hash = hash * 397 ^ RequiresForge.GetHashCode();
                hash = hash * 397 ^ RequiresSeal.GetHashCode();
                return hash * 397 ^ RequiresBossComplete.GetHashCode();
            }
        }
        public override string ToString() => StableToken;
    }

    public sealed class Sv5SpaceGateStateCheck : IComparable<Sv5SpaceGateStateCheck>
    {
        internal Sv5SpaceGateStateCheck(string id, Sv5SpaceGate gate, ulong resourceMask, bool forgeMade,
            bool sealOpen, bool bossComplete, bool expectedOpen, bool actualOpen, bool sourceAnchorReachable,
            bool targetPortReachable, bool sealedCutVerified, bool openPathVerified, int checkedCells,
            int checkedFaces, string evidence)
        {
            Id = id;
            GateId = gate.Id;
            ConnectionId = gate.SourceConnectionId;
            SourcePortId = gate.SourcePortId;
            TargetPortId = gate.TargetPortId;
            ResourceMask = resourceMask;
            ForgeMade = forgeMade;
            SealOpen = sealOpen;
            BossComplete = bossComplete;
            ExpectedOpen = expectedOpen;
            ActualOpen = actualOpen;
            SourceAnchorReachable = sourceAnchorReachable;
            TargetPortReachable = targetPortReachable;
            SealedCutVerified = sealedCutVerified;
            OpenPathVerified = openPathVerified;
            CheckedCells = checkedCells;
            CheckedFaces = checkedFaces;
            Evidence = evidence ?? string.Empty;
        }

        public string Id { get; }
        public string GateId { get; }
        public string ConnectionId { get; }
        public string SourcePortId { get; }
        public string TargetPortId { get; }
        public ulong ResourceMask { get; }
        public bool ForgeMade { get; }
        public bool SealOpen { get; }
        public bool BossComplete { get; }
        public bool ExpectedOpen { get; }
        public bool ActualOpen { get; }
        public bool SourceAnchorReachable { get; }
        public bool TargetPortReachable { get; }
        public bool SealedCutVerified { get; }
        public bool OpenPathVerified { get; }
        public int CheckedCells { get; }
        public int CheckedFaces { get; }
        public string Evidence { get; }
        public bool Success => ExpectedOpen == ActualOpen && SourceAnchorReachable && SealedCutVerified &&
            OpenPathVerified && TargetPortReachable == ExpectedOpen;
        public int CompareTo(Sv5SpaceGateStateCheck other) => other == null ? 1 :
            string.Compare(Id, other.Id, StringComparison.Ordinal);
    }

    /// <summary>
    /// Builds and validates route-owned physical cuts before the projected FSM is searched.
    /// INFILL_PENDING is never added to this movement graph: only the accepted connection envelope is consumed.
    /// </summary>
    public static class Sv5SpaceGateGeometry
    {
        internal static IReadOnlyList<Sv5SpaceGate> Build(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections, IEnumerable<Sv5RouteContactPair> sourceContacts)
        {
            if (core == null) throw new ArgumentNullException(nameof(core));
            Sv5SpaceConnection[] connections = (sourceConnections ?? Array.Empty<Sv5SpaceConnection>())
                .Where(value => value != null).OrderBy(value => value).ToArray();
            Sv5RouteContactPair[] contacts = (sourceContacts ?? Array.Empty<Sv5RouteContactPair>())
                .Where(value => value != null).ToArray();
            var edges = core.RouteSource.Graph.Edges.ToDictionary(value => value.EdgeId, value => value,
                StringComparer.Ordinal);
            var output = new List<Sv5SpaceGate>();
            foreach (Sv5SpaceConnection connection in connections.Where(value => value.Kind ==
                         Sv5SpaceConnectionKind.CoreProgression && edges.TryGetValue(value.SourceGraphEdgeId,
                             out RmapWorldGraphEdge edge) && IsGuarded(edge)))
            {
                RmapWorldGraphEdge edge = edges[connection.SourceGraphEdgeId];
                GateCut cut = FullWidthCut(core, connection);
                Sv5SpaceBoundaryFace[] faces = cut.Faces.ToArray();
                Sv5SpaceGatePredicate predicate = Sv5SpaceGatePredicate.FromEdge(edge);
                string routeKey = Sv5SpaceGraphPlanner.RouteKey(connection);
                string boundaryId = "SV5_ROUTE_BOUNDARY_" + RmapWorldDefinition.Hash(connection.Id + "|" +
                    predicate.StableToken + "|" + string.Join(";", faces.Select(value => value.StableToken)))
                    .Substring(0, 20).ToUpperInvariant();
                string[] contactIds = contacts.Where(value => value.RouteA == routeKey || value.RouteB == routeKey)
                    .Select(value => value.Id).Distinct(StringComparer.Ordinal).OrderBy(value => value,
                        StringComparer.Ordinal).ToArray();
                if (contactIds.Length == 0) contactIds = new[] { "ROUTE_SOURCE|" + routeKey };
                output.Add(new Sv5SpaceGate("SV5_GATE_" + boundaryId.Substring("SV5_ROUTE_BOUNDARY_".Length),
                    boundaryId, contactIds, Array.Empty<RmapSpecialWorldPoint>(), faces,
                    cut.SourceAnchor, cut.TargetAnchor,
                    GeometryDirection(cut.SourceAnchor, cut.TargetAnchor), connection.Flow,
                    edge.TraversalCondition, predicate, connection.Id, routeKey, connection.FromPortId,
                    connection.ToPortId, Sv5SpaceCrossingKind.ConditionalGate,
                    "SEALED_BLOCKS_ROUTE_OWNED_FULL_WIDTH_FACES",
                    "OPEN_RESTORES_SOURCE_EDGE_DIRECTION"));
            }
            return new ReadOnlyCollection<Sv5SpaceGate>(output.OrderBy(value => value).ToArray());
        }

        internal static IReadOnlyList<Sv5SpaceGateStateCheck> BuildChecks(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections, IEnumerable<Sv5SpaceGate> sourceGates)
        {
            Sv5SpaceConnection[] connections = (sourceConnections ?? Array.Empty<Sv5SpaceConnection>())
                .Where(value => value != null).ToArray();
            Sv5SpaceGate[] gates = (sourceGates ?? Array.Empty<Sv5SpaceGate>()).Where(value => value != null).ToArray();
            var output = new List<Sv5SpaceGateStateCheck>();
            foreach (Sv5SpaceGate gate in gates)
            {
                Sv5SpaceConnection connection = connections.Single(value => value.Id == gate.SourceConnectionId);
                Add("SEALED_" + gate.Id, gate, connection, 0, false, false, false, false);
                Add("OPEN_" + gate.Id, gate, connection, gate.TypedPredicate.RequiredResourceMask,
                    gate.TypedPredicate.RequiresForge, gate.TypedPredicate.RequiresSeal,
                    gate.TypedPredicate.RequiresBossComplete, true);
            }

            AddWitness("W01_SEAL_ENTRY_REACHABLE", "FORGE_GATED_SEAL_APPROACH", 7, true, false, false, true);
            AddWitness("W02_BOSS_APPROACH_REACHABLE", "SEAL_GATED_BOSS_APPROACH", 7, true, true, false, true);
            AddWitness("W02_EXIT_REMAINS_SEALED", "BOSS_GATED_EXIT_APPROACH", 7, true, true, false, false);
            return new ReadOnlyCollection<Sv5SpaceGateStateCheck>(output.OrderBy(value => value).ToArray());

            void AddWitness(string id, string condition, ulong mask, bool forge, bool seal, bool boss,
                bool expectedOpen)
            {
                Sv5SpaceConnection connection = connections.Single(value => value.Condition == condition);
                Sv5SpaceGate gate = gates.Single(value => value.SourceConnectionId == connection.Id);
                Add(id, gate, connection, mask, forge, seal, boss, expectedOpen);
            }

            void Add(string id, Sv5SpaceGate gate, Sv5SpaceConnection connection, ulong mask, bool forge,
                bool seal, bool boss, bool expectedOpen)
            {
                bool actualOpen = gate.TypedPredicate.IsOpen(mask, forge, seal, boss);
                MovementEvidence sealedEvidence = Explore(core, connections, gates, connection, mask, forge,
                    seal, boss, false);
                MovementEvidence openEvidence = Explore(core, connections, gates, connection, mask, forge,
                    seal, boss, true);
                output.Add(new Sv5SpaceGateStateCheck(id, gate, mask, forge, seal, boss, expectedOpen, actualOpen,
                    sealedEvidence.SourceAnchorReachable, actualOpen ? openEvidence.TargetPortReachable :
                    sealedEvidence.TargetPortReachable, !sealedEvidence.TargetPortReachable,
                    openEvidence.TargetPortReachable, connection.Envelope.Count, gate.BlockingFaces.Count,
                    "accepted-envelope;sealed-cut=" + (!sealedEvidence.TargetPortReachable ? "1" : "0") +
                    ";open-path=" + (openEvidence.TargetPortReachable ? "1" : "0")));
            }
        }

        public static IReadOnlyList<string> FindStateErrors(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections, IEnumerable<Sv5SpaceGate> sourceGates,
            IEnumerable<Sv5SpaceGateStateCheck> sourceChecks)
        {
            if (core == null) throw new ArgumentNullException(nameof(core));
            Sv5SpaceConnection[] connections = (sourceConnections ?? Array.Empty<Sv5SpaceConnection>())
                .Where(value => value != null).ToArray();
            Sv5SpaceGate[] gates = (sourceGates ?? Array.Empty<Sv5SpaceGate>()).Where(value => value != null).ToArray();
            Sv5SpaceGateStateCheck[] checks = (sourceChecks ?? Array.Empty<Sv5SpaceGateStateCheck>())
                .Where(value => value != null).ToArray();
            var edges = core.RouteSource.Graph.Edges.ToDictionary(value => value.EdgeId, value => value,
                StringComparer.Ordinal);
            var protectedAir = new HashSet<RmapSpecialWorldPoint>(core.CoreCells.Where(value =>
                value.Protection == RmapSpecialProtectionKind.ProtectedAir).Select(value => value.World));
            var errors = new List<string>();

            foreach (Sv5SpaceConnection connection in connections.Where(value => value.Kind ==
                         Sv5SpaceConnectionKind.CoreProgression && edges.TryGetValue(value.SourceGraphEdgeId,
                             out RmapWorldGraphEdge edge) && IsGuarded(edge)))
            {
                Sv5SpaceGate[] owned = gates.Where(value => value.SourceConnectionId == connection.Id).ToArray();
                if (owned.Length != 1)
                { errors.Add("GATE_CONNECTION_CARDINALITY|" + connection.Id + "|" + owned.Length); continue; }
                Sv5SpaceGate gate = owned[0];
                if (!gate.TypedPredicate.Equals(Sv5SpaceGatePredicate.FromEdge(edges[connection.SourceGraphEdgeId])))
                    errors.Add("GATE_PREDICATE_EDGE_MISMATCH|" + gate.Id);
                if (gate.SourceRouteId != Sv5SpaceGraphPlanner.RouteKey(connection) ||
                    gate.SourcePortId != connection.FromPortId || gate.TargetPortId != connection.ToPortId)
                    errors.Add("GATE_SOURCE_BINDING_MISMATCH|" + gate.Id);
                var envelope = new HashSet<RmapSpecialWorldPoint>(connection.Envelope);
                if (gate.BlockingFaces.Any(value => !envelope.Contains(value.First) || !envelope.Contains(value.Second)))
                    errors.Add("GATE_FACE_OUTSIDE_ACCEPTED_ENVELOPE|" + gate.Id);
                if (gate.BlockingCells.Any(protectedAir.Contains))
                    errors.Add("GATE_PROTECTED_AIR_STATE_CONFLICT|" + gate.Id);
                MovementEvidence sealedEvidence = Explore(core, connections, gates, connection, 0, false,
                    false, false, false);
                MovementEvidence openEvidence = Explore(core, connections, gates, connection,
                    gate.TypedPredicate.RequiredResourceMask, gate.TypedPredicate.RequiresForge,
                    gate.TypedPredicate.RequiresSeal, gate.TypedPredicate.RequiresBossComplete, true);
                if (!sealedEvidence.SourceAnchorReachable) errors.Add("GATE_SOURCE_PORT_BLOCKED|" + gate.Id);
                if (sealedEvidence.TargetPortReachable) errors.Add("GATE_STATE_SIDE_BYPASS|" + gate.Id);
                if (!openEvidence.TargetPortReachable) errors.Add("GATE_OPEN_PATH_MISSING|" + gate.Id);
            }
            foreach (Sv5SpaceGate gate in gates)
                if (!gate.PlannedBarrierVerified) errors.Add("GATE_GEOMETRY_INVALID|" + gate.Id);
            foreach (Sv5SpaceGateStateCheck check in checks)
                if (!check.Success) errors.Add("GATE_STATE_CHECK_FAILED|" + check.Id);
            foreach (string required in new[]
            {
                "W01_SEAL_ENTRY_REACHABLE", "W02_BOSS_APPROACH_REACHABLE", "W02_EXIT_REMAINS_SEALED",
            })
                if (checks.Count(value => value.Id == required && value.Success) != 1)
                    errors.Add("GATE_REQUIRED_WITNESS_MISSING|" + required);
            return new ReadOnlyCollection<string>(errors.Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        private static MovementEvidence Explore(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections, IEnumerable<Sv5SpaceGate> sourceGates,
            Sv5SpaceConnection connection, ulong resourceMask, bool forgeMade, bool sealOpen, bool bossComplete,
            bool? targetOpenOverride)
        {
            var accesses = core.Source.Accesses.ToDictionary(value => value.Id, value => value,
                StringComparer.Ordinal);
            RmapSpecialAccess from = accesses[connection.FromPortId];
            RmapSpecialAccess to = accesses[connection.ToPortId];
            Sv5SpaceConnection[] connections = (sourceConnections ?? Array.Empty<Sv5SpaceConnection>())
                .Where(value => value != null).ToArray();
            var allowed = connections.GroupBy(Sv5SpaceGraphPlanner.RouteKey, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => new HashSet<RmapSpecialWorldPoint>(value.SelectMany(
                    item => item.Envelope)), StringComparer.Ordinal);
            Sv5SpaceGate[] sealedGates = (sourceGates ?? Array.Empty<Sv5SpaceGate>()).Where(value => value != null &&
                !(value.SourceConnectionId == connection.Id && targetOpenOverride.HasValue ?
                    targetOpenOverride.Value : value.TypedPredicate.IsOpen(resourceMask, forgeMade, sealOpen,
                        bossComplete))).ToArray();
            var blockedCells = sealedGates.GroupBy(value => value.SourceRouteId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => new HashSet<RmapSpecialWorldPoint>(value.SelectMany(
                    item => item.BlockingCells)), StringComparer.Ordinal);
            var blockedFaces = sealedGates.GroupBy(value => value.SourceRouteId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => new HashSet<string>(value.SelectMany(item =>
                    item.BlockingFaces).Select(item => item.StableToken), StringComparer.Ordinal),
                    StringComparer.Ordinal);
            IReadOnlyList<Sv5RouteContactCell> contactCells = Sv5SpaceGraphValidator.AcceptedContactCells(core,
                connections);
            Sv5RouteContactPair[] contacts = Sv5RouteStatePolicy.EnumerateContactPairs(contactCells)
                .Where(value => SamePredicate(core, connections, value.RouteA, value.RouteB)).ToArray();
            var crossings = new Dictionary<RoutePoint, List<RoutePoint>>();
            foreach (Sv5RouteContactPair contact in contacts)
            {
                AddCrossing(new RoutePoint(contact.RouteA, contact.RouteAWorld),
                    new RoutePoint(contact.RouteB, contact.RouteBWorld));
                AddCrossing(new RoutePoint(contact.RouteB, contact.RouteBWorld),
                    new RoutePoint(contact.RouteA, contact.RouteAWorld));
            }
            string sourceRoute = Sv5SpaceGraphPlanner.RouteKey(connection);
            var reached = new HashSet<RoutePoint>();
            var queue = new Queue<RoutePoint>();
            foreach (RmapSpecialWorldPoint point in from.OpenCells.Where(value => allowed[sourceRoute].Contains(value) &&
                         !IsBlocked(sourceRoute, value)))
            {
                var start = new RoutePoint(sourceRoute, point);
                if (reached.Add(start)) queue.Enqueue(start);
            }
            while (queue.Count != 0)
            {
                RoutePoint current = queue.Dequeue();
                foreach (RmapSpecialWorldPoint nextWorld in Neighbors(current.World))
                {
                    if (!allowed[current.Route].Contains(nextWorld) || IsBlocked(current.Route, nextWorld) ||
                        IsFaceBlocked(current.Route, current.World, nextWorld)) continue;
                    var next = new RoutePoint(current.Route, nextWorld);
                    if (!reached.Add(next)) continue;
                    queue.Enqueue(next);
                }
                if (!crossings.TryGetValue(current, out List<RoutePoint> adjacent)) continue;
                foreach (RoutePoint next in adjacent)
                    if (allowed[next.Route].Contains(next.World) && !IsBlocked(next.Route, next.World) &&
                        reached.Add(next)) queue.Enqueue(next);
            }
            Sv5SpaceGate target = (sourceGates ?? Array.Empty<Sv5SpaceGate>()).Single(value =>
                value.SourceConnectionId == connection.Id);
            return new MovementEvidence(reached.Contains(new RoutePoint(sourceRoute, target.SideAAnchor)),
                to.OpenCells.Any(value => reached.Contains(new RoutePoint(sourceRoute, value))));

            bool IsBlocked(string route, RmapSpecialWorldPoint world) =>
                blockedCells.TryGetValue(route, out HashSet<RmapSpecialWorldPoint> values) && values.Contains(world);
            bool IsFaceBlocked(string route, RmapSpecialWorldPoint first, RmapSpecialWorldPoint second) =>
                blockedFaces.TryGetValue(route, out HashSet<string> values) && values.Contains(FaceToken(first, second));
            void AddCrossing(RoutePoint first, RoutePoint second)
            {
                if (!crossings.TryGetValue(first, out List<RoutePoint> values))
                    crossings.Add(first, values = new List<RoutePoint>());
                values.Add(second);
            }
        }

        private static bool SamePredicate(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections, string leftRoute, string rightRoute)
        {
            var edges = core.RouteSource.Graph.Edges.ToDictionary(value => value.EdgeId, value => value,
                StringComparer.Ordinal);
            var routes = (sourceConnections ?? Array.Empty<Sv5SpaceConnection>()).Where(value => value != null)
                .ToDictionary(Sv5SpaceGraphPlanner.RouteKey, value => value, StringComparer.Ordinal);
            if (!routes.ContainsKey(leftRoute) || !routes.ContainsKey(rightRoute)) return false;
            Sv5SpaceGatePredicate left = Predicate(leftRoute), right = Predicate(rightRoute);
            return left.Equals(right);

            Sv5SpaceGatePredicate Predicate(string route)
            {
                Sv5SpaceConnection value = routes[route];
                return edges.TryGetValue(value.SourceGraphEdgeId, out RmapWorldGraphEdge edge) ?
                    Sv5SpaceGatePredicate.FromEdge(edge) : new Sv5SpaceGatePredicate(0, false, false, false);
            }
        }

        private static GateCut FullWidthCut(Sv5CoreReservationPlan core, Sv5SpaceConnection connection)
        {
            var accesses = core.Source.Accesses.ToDictionary(value => value.Id, value => value,
                StringComparer.Ordinal);
            var envelope = new HashSet<RmapSpecialWorldPoint>(connection.Envelope);
            var distance = new Dictionary<RmapSpecialWorldPoint, int>();
            var queue = new Queue<RmapSpecialWorldPoint>();
            foreach (RmapSpecialWorldPoint point in accesses[connection.FromPortId].OpenCells.Where(
                         envelope.Contains))
            {
                if (distance.ContainsKey(point)) continue;
                distance.Add(point, 0);
                queue.Enqueue(point);
            }
            while (queue.Count != 0)
            {
                RmapSpecialWorldPoint current = queue.Dequeue();
                foreach (RmapSpecialWorldPoint next in Neighbors(current).Where(envelope.Contains))
                {
                    if (distance.ContainsKey(next)) continue;
                    distance.Add(next, distance[current] + 1);
                    queue.Enqueue(next);
                }
            }
            int targetDistance = accesses[connection.ToPortId].OpenCells.Where(distance.ContainsKey)
                .Select(value => distance[value]).DefaultIfEmpty(-1).Min();
            if (targetDistance < 1)
                throw new InvalidOperationException("A guarded connection needs a reachable target port: " +
                    connection.Id);
            int threshold = Math.Max(0, targetDistance / 2);
            var faces = new List<Sv5SpaceBoundaryFace>();
            RmapSpecialWorldPoint sourceAnchor = default(RmapSpecialWorldPoint);
            RmapSpecialWorldPoint targetAnchor = default(RmapSpecialWorldPoint);
            bool hasAnchor = false;
            foreach (RmapSpecialWorldPoint first in envelope.OrderBy(value => value))
            foreach (RmapSpecialWorldPoint second in Neighbors(first).Where(envelope.Contains))
            {
                if (first.CompareTo(second) >= 0 || !distance.ContainsKey(first) || !distance.ContainsKey(second) ||
                    (distance[first] <= threshold) == (distance[second] <= threshold)) continue;
                faces.Add(new Sv5SpaceBoundaryFace(first, second));
                RmapSpecialWorldPoint source = distance[first] <= threshold ? first : second;
                RmapSpecialWorldPoint target = source.Equals(first) ? second : first;
                if (hasAnchor && !connection.Centerline.Contains(source)) continue;
                sourceAnchor = source;
                targetAnchor = target;
                hasAnchor = true;
            }
            if (!hasAnchor || faces.Count == 0)
                throw new InvalidOperationException("A guarded connection needs a full-width cut: " + connection.Id);
            return new GateCut(faces, sourceAnchor, targetAnchor);
        }

        private static IEnumerable<RmapSpecialWorldPoint> Neighbors(RmapSpecialWorldPoint point)
        {
            if (point.X > 0) yield return new RmapSpecialWorldPoint(point.X - 1, point.Y);
            if (point.X < Sv5SpaceGraphPlanner.WorldWidth - 1) yield return new RmapSpecialWorldPoint(point.X + 1, point.Y);
            if (point.Y > 0) yield return new RmapSpecialWorldPoint(point.X, point.Y - 1);
            if (point.Y < Sv5SpaceGraphPlanner.WorldHeight - 1) yield return new RmapSpecialWorldPoint(point.X, point.Y + 1);
        }

        private static bool IsGuarded(RmapWorldGraphEdge edge) => edge.RequiredResourceMask != 0 ||
            edge.RequiresForge || edge.RequiresSeal || edge.RequiresBossComplete;
        private static string FaceToken(RmapSpecialWorldPoint first, RmapSpecialWorldPoint second) =>
            first.CompareTo(second) <= 0 ? first + ">" + second : second + ">" + first;
        private static RmapWorldGraphDirection GeometryDirection(RmapSpecialWorldPoint first,
            RmapSpecialWorldPoint second) => second.X > first.X ? RmapWorldGraphDirection.Right :
            second.X < first.X ? RmapWorldGraphDirection.Left :
            second.Y > first.Y ? RmapWorldGraphDirection.Up : RmapWorldGraphDirection.Down;

        private sealed class MovementEvidence
        {
            public MovementEvidence(bool sourceAnchorReachable, bool targetPortReachable)
            { SourceAnchorReachable = sourceAnchorReachable; TargetPortReachable = targetPortReachable; }
            public bool SourceAnchorReachable { get; }
            public bool TargetPortReachable { get; }
        }

        private sealed class GateCut
        {
            public GateCut(IEnumerable<Sv5SpaceBoundaryFace> faces, RmapSpecialWorldPoint sourceAnchor,
                RmapSpecialWorldPoint targetAnchor)
            {
                Faces = new ReadOnlyCollection<Sv5SpaceBoundaryFace>(faces.OrderBy(value => value).ToArray());
                SourceAnchor = sourceAnchor;
                TargetAnchor = targetAnchor;
            }
            public IReadOnlyList<Sv5SpaceBoundaryFace> Faces { get; }
            public RmapSpecialWorldPoint SourceAnchor { get; }
            public RmapSpecialWorldPoint TargetAnchor { get; }
        }

        private sealed class RoutePoint : IEquatable<RoutePoint>
        {
            public RoutePoint(string route, RmapSpecialWorldPoint world) { Route = route; World = world; }
            public string Route { get; }
            public RmapSpecialWorldPoint World { get; }
            public bool Equals(RoutePoint other) => other != null && World.Equals(other.World) &&
                string.Equals(Route, other.Route, StringComparison.Ordinal);
            public override bool Equals(object obj) => Equals(obj as RoutePoint);
            public override int GetHashCode()
            {
                unchecked { return ((Route != null ? Route.GetHashCode() : 0) * 397) ^ World.GetHashCode(); }
            }
        }
    }
}
