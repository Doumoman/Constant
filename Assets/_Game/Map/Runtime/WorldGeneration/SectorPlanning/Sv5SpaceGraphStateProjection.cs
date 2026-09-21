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
    /// Projects canonical FSM transitions through the accepted global coordinate graph.
    /// A typed predicate applies at its physical neck, never to a whole route by label.
    /// </summary>
    public static class Sv5SpaceGraphStateProjection
    {
        private const ulong AllResources = 7;

        internal static Sv5SpaceProjectionResult Project(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections,
            IEnumerable<Sv5RouteContactPair> sourceContacts, bool contactCoverageVerified)
        {
            if (core == null) throw new ArgumentNullException(nameof(core));
            var connections = sourceConnections.OrderBy(c => c).ToArray();
            var contacts = sourceContacts.OrderBy(c => c).ToArray();
            var gates = Sv5SpaceGateGeometry.Build(core, connections, contacts);
            var product = Sv5SpacePhysicalProduct.Analyze(core, connections, gates);
            var checks = Sv5SpaceGateGeometry.BuildChecks(core, connections, gates);
            var diagnostics = new List<string>(product.Diagnostics);
            diagnostics.AddRange(Sv5SpaceGateGeometry.FindStateErrors(core, connections, gates, checks));
            var decisions = new List<Sv5SpaceContactDecision>();
            var edges = core.RouteSource.Graph.Edges.ToDictionary(e => e.EdgeId);
            var byRoute = connections.ToDictionary(Sv5SpaceGraphPlanner.RouteKey);
            foreach (var contact in contacts)
            {
                var local = gates.Where(g => Sv5SpaceGraphValidator.ValidateBarrierFixture(contact,
                    g.BlockingCells, g.BlockingFaces).Count == 0).ToArray();
                if (local.Length > 1) diagnostics.Add("CONTACT_MULTIPLE_LOCAL_PREDICATES|" + contact.Id);
                bool known = byRoute.ContainsKey(contact.RouteA) && byRoute.ContainsKey(contact.RouteB);
                if (!known) diagnostics.Add("UNKNOWN_CONTACT_ROUTE|" + contact.Id);
                string Predicate(string route) => byRoute.TryGetValue(route, out var c) &&
                    edges.TryGetValue(c.SourceGraphEdgeId, out var edge) ?
                    Sv5SpaceGatePredicate.FromEdge(edge).StableToken : "ACTIONLESS";
                // A connection predicate is NOT a predicate on every cell along that connection.
                // Without an exact local barrier this is an ordinary coordinate join. Its safety
                // is established by the full physical product, including every closed bypass check.
                var crossing = local.Length == 1 ? Sv5SpaceCrossingKind.ConditionalGate : Sv5SpaceCrossingKind.Join;
                decisions.Add(new Sv5SpaceContactDecision(contact, ContactNodeId(contact), crossing,
                    Predicate(contact.RouteA) + "|" + Predicate(contact.RouteB),
                    local.Length == 1 ? local[0].BoundaryId : string.Empty, contactCoverageVerified,
                    product.Success && known, local.Length == 1 ? "EXACT_LOCAL_PORTAL" :
                    "GLOBAL_COORDINATE_JOIN;PREDICATE_APPLIES_AT_PORTAL_ONLY;CLEARANCE_IS_NOT_PASSAGE"));
            }
            return new Sv5SpaceProjectionResult(decisions, gates, product.Proofs, checks, diagnostics);
        }


        public static IReadOnlyList<string> ValidateAcceptedProjection(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections)
        {
            if (core == null) throw new ArgumentNullException(nameof(core));
            Sv5SpaceConnection[] connections = (sourceConnections ?? Array.Empty<Sv5SpaceConnection>())
                .Where(value => value != null).ToArray();
            string[] missing = core.RouteSource.Graph.Edges.Where(e => !connections.Any(c =>
                c.Kind == Sv5SpaceConnectionKind.CoreProgression && c.SourceGraphEdgeId == e.EdgeId))
                .Select(e => "MISSING_ACTUAL_CORE_CONNECTOR|" + e.EdgeId).ToArray();
            if (missing.Length != 0) return new ReadOnlyCollection<string>(missing);
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

        private static string ContactNodeId(Sv5RouteContactPair contact) => "SV5_CONTACT_NODE_" +
            Sv5WorldDefinition.Hash(contact.Kind + "|" + contact.FirstWorld + "|" + contact.SecondWorld)
                .Substring(0, 20).ToUpperInvariant();
    }
}
