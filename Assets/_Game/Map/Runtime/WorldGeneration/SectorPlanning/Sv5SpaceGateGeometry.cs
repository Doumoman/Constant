using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    /// <summary>The typed SV5 predicate owned by one physical planned boundary.</summary>
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

        internal static Sv5SpaceGatePredicate FromEdge(Sv5WorldGraphEdge edge) => edge == null ? null :
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
        /// <summary>Candidate neck edges outside a protected aperture, in ordered corridor order.
        /// An aperture cell is never a candidate: advance along the corridor until BOTH cells
        /// are outside every aperture and protected AIR. Clearance is not passage.</summary>
        public static IReadOnlyList<Sv5SpaceBoundaryFace> PortBoundaryCandidates(
            IEnumerable<Sv5SpecialWorldPoint> orderedCorridor,
            IEnumerable<Sv5SpecialWorldPoint> selectedAperture,
            IEnumerable<Sv5SpecialWorldPoint> allApertures,
            IEnumerable<Sv5SpecialWorldPoint> protectedAir, int maxDistance = 7)
        {
            var line = orderedCorridor.ToArray();
            var aperture = selectedAperture.ToArray();
            var protectedCells = new HashSet<Sv5SpecialWorldPoint>(allApertures.Concat(protectedAir));
            var result = new List<Sv5SpaceBoundaryFace>();
            for (int i = 1; i < line.Length; i++)
            {
                Sv5SpecialWorldPoint a = line[i - 1], b = line[i];
                if (Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) != 1 ||
                    protectedCells.Contains(a) || protectedCells.Contains(b)) continue;
                if (!aperture.Any(p => Math.Max(Math.Abs(p.X - a.X) + Math.Abs(p.Y - a.Y),
                    Math.Abs(p.X - b.X) + Math.Abs(p.Y - b.Y)) <= maxDistance)) continue;
                result.Add(new Sv5SpaceBoundaryFace(a, b));
            }
            return new ReadOnlyCollection<Sv5SpaceBoundaryFace>(result.Distinct().ToArray());
        }

        internal static IReadOnlyList<Sv5SpaceGate> Build(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections, IEnumerable<Sv5RouteContactPair> sourceContacts)
        {
            var connections = sourceConnections.OrderBy(c => c).ToArray();
            var contacts = sourceContacts.ToArray();
            var edges = core.RouteSource.Graph.Edges.ToDictionary(e => e.EdgeId);
            var protectedAir = core.CoreCells.Where(c => c.Protection == Sv5SpecialProtectionKind.ProtectedAir)
                .Select(c => c.World).ToArray();
            var ports = core.Source.Accesses.SelectMany(a => a.OpenCells).ToArray();
            var gates = new List<Sv5SpaceGate>();
            foreach (var connection in connections.Where(c => c.Kind == Sv5SpaceConnectionKind.CoreProgression &&
                IsGuarded(edges[c.SourceGraphEdgeId])).OrderBy(c => edges[c.SourceGraphEdgeId].RequiresBossComplete ? 3 :
                    edges[c.SourceGraphEdgeId].RequiresSeal ? 2 : 1))
            {
                var edge = edges[connection.SourceGraphEdgeId];
                var foreignEdges = new HashSet<string>(connections.Where(c => c != connection).SelectMany(c =>
                    c.Centerline.Zip(c.Centerline.Skip(1), (a,b) => new Sv5SpaceBoundaryFace(a,b).StableToken)));
                var candidates = new[] { connection.ToPortId, connection.FromPortId }.SelectMany(port =>
                    PortBoundaryCandidates(connection.Centerline, core.Source.Accesses.Single(a => a.Id == port).OpenCells,
                        ports, protectedAir)).GroupBy(f => f.StableToken).Select(g => g.First()).ToArray();
                Sv5SpaceGate accepted = null;
                foreach (var face in candidates)
                {
                    if (foreignEdges.Contains(face.StableToken)) continue;
                    var predicate = Sv5SpaceGatePredicate.FromEdge(edge);
                    string token = Sv5WorldDefinition.Hash(connection.FromPortId + "|" + connection.ToPortId + "|" +
                        predicate.StableToken + "|" + face.StableToken).Substring(0,20).ToUpperInvariant();
                    var ids = contacts.Where(c => Sv5SpaceGraphValidator.ValidateBarrierFixture(c,
                        Array.Empty<Sv5SpecialWorldPoint>(), new[] { face }).Count == 0).Select(c => c.Id).ToArray();
                    if (ids.Length == 0) ids = new[] { "PORTAL|" + face.StableToken };
                    var gate = new Sv5SpaceGate("SV5_GATE_" + token, "SV5_ROUTE_BOUNDARY_" + token,
                        ids, Array.Empty<Sv5SpecialWorldPoint>(), new[] { face }, face.First, face.Second,
                        GeometryDirection(face.First, face.Second), connection.Flow, edge.TraversalCondition,
                        predicate, connection.Id, Sv5SpaceGraphPlanner.RouteKey(connection), connection.FromPortId,
                        connection.ToPortId, Sv5SpaceCrossingKind.ConditionalGate,
                        "SEALED_GLOBAL_WORLD_FACE_CUT_AT_LOCAL_NECK", "OPEN_RESTORES_GLOBAL_WORLD_FACES_AT_LOCAL_NECK");
                    // A local neck must be a global cut on its own. No inherited/distributed faces.
                    if (Sv5SpacePhysicalMovement.Explore(core, connections, new[] { gate }, connection,
                        0, false, false, false, false).TargetPortReachable) continue;
                    var trial = gates.Concat(new[] { gate }).ToArray();
                    if (!Sv5SpacePhysicalProduct.PreservesExpectedOpen(core, connections, trial)) continue;
                    accepted = gate;
                    break;
                }
                if (accepted == null) throw new InvalidOperationException("NO_LEGAL_LOCAL_CORRIDOR_NECK|" +
                    connection.Id + "|candidates=" + candidates.Length);
                gates.Add(accepted);
            }
            return new ReadOnlyCollection<Sv5SpaceGate>(gates.OrderBy(g => g).ToArray());
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
            var protectedAir = new HashSet<Sv5SpecialWorldPoint>(core.CoreCells.Where(value =>
                value.Protection == Sv5SpecialProtectionKind.ProtectedAir).Select(value => value.World));
            var globalPassage = new HashSet<Sv5SpecialWorldPoint>(connections.SelectMany(value =>
                value.Centerline.Concat(value.ApertureCells)));
            var errors = new List<string>();

            foreach (Sv5SpaceConnection connection in connections.Where(value => value.Kind ==
                         Sv5SpaceConnectionKind.CoreProgression && edges.TryGetValue(value.SourceGraphEdgeId,
                             out Sv5WorldGraphEdge edge) && IsGuarded(edge)))
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
                if (gate.BlockingFaces.Any(value => !globalPassage.Contains(value.First) ||
                    !globalPassage.Contains(value.Second)))
                    errors.Add("GATE_FACE_OUTSIDE_GLOBAL_PASSAGE|" + gate.Id);
                if (gate.BlockingCells.Any(protectedAir.Contains))
                    errors.Add("GATE_PROTECTED_AIR_STATE_CONFLICT|" + gate.Id);
                var ownEdges = new HashSet<string>(connection.Centerline.Zip(connection.Centerline.Skip(1),
                    (a,b) => new Sv5SpaceBoundaryFace(a,b).StableToken));
                var foreignEdges = new HashSet<string>(connections.Where(c => c.Id != connection.Id)
                    .SelectMany(c => c.Centerline.Zip(c.Centerline.Skip(1),
                        (a,b) => new Sv5SpaceBoundaryFace(a,b).StableToken)));
                var apertures = core.Source.Accesses.Where(a => a.Id == connection.FromPortId ||
                    a.Id == connection.ToPortId).SelectMany(a => a.OpenCells).ToArray();
                if (gate.BlockingFaces.Count != 1 || gate.BlockingCells.Count != 0)
                    errors.Add("GATE_REMOTE_CUT_RETAINED|" + gate.Id);
                foreach (var face in gate.BlockingFaces)
                {
                    if (!ownEdges.Contains(face.StableToken)) errors.Add("GATE_FACE_OUTSIDE_PORTAL|" + gate.Id + "|" + face.StableToken);
                    if (foreignEdges.Contains(face.StableToken)) errors.Add("GATE_FACE_USED_BY_FOREIGN_SEGMENT|" + gate.Id + "|" + face.StableToken);
                    if (!apertures.Any(p => Math.Max(Math.Abs(p.X-face.First.X)+Math.Abs(p.Y-face.First.Y),
                        Math.Abs(p.X-face.Second.X)+Math.Abs(p.Y-face.Second.Y)) <= 7))
                        errors.Add("GATE_REMOTE_CUT_RETAINED|" + gate.Id + "|" + face.StableToken);
                    if (protectedAir.Contains(face.First) || protectedAir.Contains(face.Second))
                        errors.Add("GATE_PROTECTED_AIR_STATE_CONFLICT|" + gate.Id + "|" + face.StableToken);
                    if (gates.Any(g => g != gate && g.BlockingFaces.Any(f => f.StableToken == face.StableToken)))
                        errors.Add("GATE_FACE_USED_BY_FOREIGN_PORTAL|" + gate.Id + "|" + face.StableToken);
                }
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
            errors.AddRange(Sv5SpacePhysicalMovement.FindStateErrors(core, connections, gates));
            return new ReadOnlyCollection<string>(errors.Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        private static MovementEvidence Explore(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections, IEnumerable<Sv5SpaceGate> sourceGates,
            Sv5SpaceConnection connection, ulong resourceMask, bool forgeMade, bool sealOpen, bool bossComplete,
            bool? targetOpenOverride)
        {
            Sv5SpaceGate[] targetGate = (sourceGates ?? Array.Empty<Sv5SpaceGate>()).Where(value =>
                value != null && value.SourceConnectionId == connection.Id).ToArray();
            Sv5SpacePhysicalReachability value = Sv5SpacePhysicalMovement.Explore(core, sourceConnections,
                targetGate, connection, resourceMask, forgeMade, sealOpen, bossComplete, targetOpenOverride);
            return new MovementEvidence(value.SourceAnchorReachable, value.TargetPortReachable);
        }


        private static IEnumerable<Sv5SpecialWorldPoint> Neighbors(Sv5SpecialWorldPoint point)
        {
            if (point.X > 0) yield return new Sv5SpecialWorldPoint(point.X - 1, point.Y);
            if (point.X < Sv5SpaceGraphPlanner.WorldWidth - 1) yield return new Sv5SpecialWorldPoint(point.X + 1, point.Y);
            if (point.Y > 0) yield return new Sv5SpecialWorldPoint(point.X, point.Y - 1);
            if (point.Y < Sv5SpaceGraphPlanner.WorldHeight - 1) yield return new Sv5SpecialWorldPoint(point.X, point.Y + 1);
        }

        private static bool IsGuarded(Sv5WorldGraphEdge edge) => edge.RequiredResourceMask != 0 ||
            edge.RequiresForge || edge.RequiresSeal || edge.RequiresBossComplete;
        private static string FaceToken(Sv5SpecialWorldPoint first, Sv5SpecialWorldPoint second) =>
            first.CompareTo(second) <= 0 ? first + ">" + second : second + ">" + first;
        private static Sv5WorldGraphDirection GeometryDirection(Sv5SpecialWorldPoint first,
            Sv5SpecialWorldPoint second) => second.X > first.X ? Sv5WorldGraphDirection.Right :
            second.X < first.X ? Sv5WorldGraphDirection.Left :
            second.Y > first.Y ? Sv5WorldGraphDirection.Up : Sv5WorldGraphDirection.Down;

        private sealed class MovementEvidence
        {
            public MovementEvidence(bool sourceAnchorReachable, bool targetPortReachable)
            { SourceAnchorReachable = sourceAnchorReachable; TargetPortReachable = targetPortReachable; }
            public bool SourceAnchorReachable { get; }
            public bool TargetPortReachable { get; }
        }

        private sealed class GateCut
        {
            public GateCut(IEnumerable<Sv5SpaceBoundaryFace> faces, Sv5SpecialWorldPoint sourceAnchor,
                Sv5SpecialWorldPoint targetAnchor)
            {
                Faces = new ReadOnlyCollection<Sv5SpaceBoundaryFace>(faces.OrderBy(value => value).ToArray());
                SourceAnchor = sourceAnchor;
                TargetAnchor = targetAnchor;
            }
            public IReadOnlyList<Sv5SpaceBoundaryFace> Faces { get; }
            public Sv5SpecialWorldPoint SourceAnchor { get; }
            public Sv5SpecialWorldPoint TargetAnchor { get; }
        }

    }
}
