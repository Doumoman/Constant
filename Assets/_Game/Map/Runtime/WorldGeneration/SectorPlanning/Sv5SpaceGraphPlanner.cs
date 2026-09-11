using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class Sv5SpaceGraphPlanner
    {
        public const int WorldWidth = 624;
        public const int WorldHeight = 416;
        public const int MicroChunkWidth = 12;
        public const int MicroChunkHeight = 8;
        public const int PatternSize = 4;

        public static Sv5SpaceGraphPlan PlanWithInfill(Sv5CoreReservationPlan core, ulong seed,
            Sv5SpaceGraphAuthoringProfile profile = null, Sv5DiversityProfile diversity = null, Sv5InfillProfile infill = null)
        {
            var baseline = Plan(core,seed,profile,diversity);
            infill = infill ?? new Sv5InfillProfile();
            if (!infill.Enabled) return baseline;
            // The historical graph is captured once at this boundary. The infill generator consumes
            // only immutable source facts and owns its success criteria independently.
            var source = Sv5SpaceInfill.Capture(baseline);
            var payload = Sv5SpaceInfill.Build(source,infill);
            return AttachInfill(baseline,source,payload);
        }

        public static Sv5SpaceGraphPlan AttachInfill(Sv5SpaceGraphPlan baseline, Sv5InfillPlan payload)
            => AttachInfill(baseline,Sv5SpaceInfill.Capture(baseline),payload);

        private static Sv5SpaceGraphPlan AttachInfill(Sv5SpaceGraphPlan baseline, Sv5InfillSource source, Sv5InfillPlan payload)
        {
            if (baseline.Digest != payload.BaselineDigest) throw new ArgumentException("Infill baseline digest mismatch.");
            var diagnostics = new List<string>(Sv5SpaceInfill.ValidatePayload(source,payload));
            var connections = baseline.Connections.Select(c =>
            {
                var added = payload.Cells.Where(cell => cell.Value == Sv5InfillCellValue.Air &&
                    cell.Host.Split(';').Contains(c.Id)).Select(cell => cell.World).ToArray();
                if (added.Length == 0) return c;
                // Existing centerline is unchanged; explicitly carry its known AIR into the port-connected aperture union.
                return new Sv5SpaceConnection(c.Id,c.Kind,c.FromPortId,c.ToPortId,c.FromPlaceId,c.ToPlaceId,c.Direction,
                    c.Flow,c.Condition,c.SourceGraphEdgeId,c.SelectionState,c.Centerline,c.Envelope.Concat(added),
                    c.ApertureCells.Concat(c.Centerline).Concat(added));
            }).ToArray();
            var routeCells = BuildRouteContactCells(baseline.Core,connections).ToArray();
            var pairs = Sv5RouteStatePolicy.EnumerateContactPairs(routeCells);
            var coverage = Sv5SpaceGraphValidator.FindContactCoverageErrors(routeCells,pairs);
            diagnostics.AddRange(coverage);
            var projection = Sv5SpaceGraphStateProjection.Project(baseline.Core,connections,pairs,coverage.Count == 0);
            diagnostics.AddRange(projection.Diagnostics);
            foreach (var gate in baseline.Gates)
            {
                var current = projection.Gates.SingleOrDefault(g => g.Id == gate.Id);
                if (current == null || current.TypedPredicate.StableToken != gate.TypedPredicate.StableToken ||
                    !current.BlockingCells.SequenceEqual(gate.BlockingCells) ||
                    !current.BlockingFaces.Select(f=>f.StableToken).SequenceEqual(gate.BlockingFaces.Select(f=>f.StableToken)))
                    diagnostics.Add("INFILL_CHANGED_PRESERVED_GATE|"+gate.Id);
            }
            var reservations = BuildReservations(baseline.Core,baseline.Places,connections,projection.Gates).ToArray();
            Validate(baseline.Core,baseline.Places,baseline.Ports,connections,reservations,projection,diagnostics);
            return new Sv5SpaceGraphPlan(baseline.Core,baseline.Seed,baseline.Profile,baseline.Places,baseline.Ports,
                connections,projection.Gates,reservations,projection.Contacts,projection.Proofs,projection.GateStateChecks,
                diagnostics,baseline.Diversity.Profile,baseline.Diversity.Decisions,payload);
        }

        public static Sv5SpaceGraphPlan Plan(Sv5CoreReservationPlan core, ulong seed,
            Sv5SpaceGraphAuthoringProfile profile = null, Sv5DiversityProfile diversity = null)
        {
            if (core == null) throw new ArgumentNullException(nameof(core));
            if (core.RouteSource.Definition.Request.Seed != seed)
                throw new ArgumentException("The explicit layout seed must equal the preserved world definition seed.", nameof(seed));
            if (core.Sites.Count != 8 || core.CoreCells.Count != 2432 || !core.RouteSource.Graph.Success)
                throw new ArgumentException("A passing SV5_04/RMAP13 source is required.", nameof(core));
            profile = profile ?? Sv5SpaceGraphAuthoringProfile.RepresentativeV1();
            diversity = diversity ?? new Sv5DiversityProfile();

            var diagnostics = new List<string>();
            var diversityDecisions = new List<Sv5DiversityDecision>();
            List<Sv5SpacePlace> places = BuildCorePlaces(core).ToList();
            var placementBlocked = new HashSet<RmapSpecialWorldPoint>(core.CoreCells.Select(value => value.World));
            placementBlocked.UnionWith(core.RouteCells.Select(value => value.World));
            foreach (Sv5SpaceFamilySpec spec in profile.Families)
            {
                Sv5SpacePlace place = PlaceFamily(spec, seed, placementBlocked, places, diversity, diversityDecisions);
                if (place == null)
                {
                    diagnostics.Add("PLACE_UNAVAILABLE|" + spec.Family);
                    continue;
                }
                places.Add(place);
                AddBounds(placementBlocked, place.Bounds, 2);
            }

            List<Sv5SpacePort> ports = BuildCorePorts(core).ToList();
            foreach (Sv5SpacePlace place in places.Where(value => value.Kind != Sv5SpacePlaceKind.Core))
                ports.AddRange(BuildGeneralPorts(place));
            var byPort = ports.ToDictionary(value => value.Id, value => value, StringComparer.Ordinal);
            List<Sv5SpaceConnection> connections = BuildCoreConnections(core, byPort, diagnostics).ToList();

            var hardBlocked = new HashSet<RmapSpecialWorldPoint>(core.CoreCells.Where(value =>
                value.Protection == RmapSpecialProtectionKind.FixedSolid).Select(value => value.World));
            hardBlocked.UnionWith(core.RouteCells.Where(value => value.RequiredBaseCell == RmapPatternBaseCell.Solid)
                .Select(value => value.World));
            var routingBlocked = new HashSet<RmapSpecialWorldPoint>(hardBlocked);
            foreach (Sv5SpacePlace place in places.Where(value => value.Kind != Sv5SpacePlaceKind.Core))
                AddBounds(routingBlocked, place.Bounds, 2);
            BuildOptionalCircuit(core, seed, places, byPort, connections, hardBlocked, routingBlocked, diagnostics);
            connections = RepairGateCorridors(core, places, byPort, connections, hardBlocked);

            List<Sv5RouteContactCell> routeCells = BuildRouteContactCells(core, connections).ToList();
            IReadOnlyList<Sv5RouteContactPair> contactPairs = Sv5RouteStatePolicy.EnumerateContactPairs(routeCells);
            string[] coverageErrors = Sv5SpaceGraphValidator.FindContactCoverageErrors(routeCells, contactPairs).ToArray();
            diagnostics.AddRange(coverageErrors);
            Sv5SpaceProjectionResult projection = Sv5SpaceGraphStateProjection.Project(core, connections, contactPairs,
                coverageErrors.Length == 0);
            diagnostics.AddRange(projection.Diagnostics);

            List<Sv5SpaceReservationCell> reservations = BuildReservations(core, places, connections,
                projection.Gates).ToList();
            Validate(core, places, ports, connections, reservations, projection, diagnostics);
            return new Sv5SpaceGraphPlan(core, seed, profile, places, ports, connections, projection.Gates,
                reservations, projection.Contacts, projection.Proofs, projection.GateStateChecks, diagnostics,
                diversity, diversityDecisions);
        }

        private static IEnumerable<Sv5SpacePlace> BuildCorePlaces(Sv5CoreReservationPlan core)
        {
            foreach (RmapSpecialSite site in core.Sites)
                yield return new Sv5SpacePlace(site.Id, site.TemplateId, Sv5SpacePlaceKind.Core,
                    new Sv5SpaceBounds(site.Origin.X, site.Origin.Y, site.WidthTiles, site.HeightTiles), site.Id,
                    "PRESERVED_RMAP15", Sector(site.Origin.X + site.WidthTiles / 2, site.Origin.Y + site.HeightTiles / 2));
        }

        private static Sv5SpacePlace PlaceFamily(Sv5SpaceFamilySpec spec, ulong seed,
            ISet<RmapSpecialWorldPoint> blocked, IEnumerable<Sv5SpacePlace> placed,
            Sv5DiversityProfile diversity, ICollection<Sv5DiversityDecision> decisions)
        {
            int preferred = (spec.Ordinal + (int)(seed % 16UL)) % 16;
            var candidates = new List<Sv5PlacementCandidate>();
            for (var sectorOffset = 0; sectorOffset < 16; sectorOffset++)
            {
                int sector = (preferred + sectorOffset) % 16;
                int sx = sector % 4;
                int sy = sector / 4;
                int minX = sx * 156 + 4;
                int maxX = (sx + 1) * 156 - spec.Width - 4;
                int minY = sy * 104 + 4;
                int maxY = (sy + 1) * 104 - spec.Height - 4;
                for (int y = Align4(minY); y <= maxY; y += 4)
                for (int x = Align4(minX); x <= maxX; x += 4)
                    candidates.Add(new Sv5PlacementCandidate(new Sv5SpaceBounds(x, y, spec.Width, spec.Height), sector, sectorOffset,
                        StableRank(seed, spec.Ordinal, x, y)));
            }
            var selection = Sv5SpaceDiversity.Select(spec, seed, candidates, placed.Select(Sv5FormationPart.FromPlace),
                diversity, candidate => !BoundsCells(candidate.Bounds, 2).Any(blocked.Contains));
            foreach (var decision in selection.Trace) decisions.Add(decision);
            var chosen = selection.Chosen;
            return chosen == null ? null : new Sv5SpacePlace(Sv5SpaceDiversity.PlaceId(spec, seed, chosen.Bounds),
                spec.Family, spec.Kind, chosen.Bounds, string.Empty, spec.FutureOwner, chosen.Sector);
        }

        private static IEnumerable<Sv5SpacePort> BuildCorePorts(Sv5CoreReservationPlan core)
        {
            foreach (RmapSpecialAccess access in core.Source.Accesses)
            {
                RmapSpecialWorldPoint anchor = access.OpenCells[access.OpenCells.Count / 2];
                string status = access.Id == "RMAP15_SITE_START_PORT_EXIT" ?
                    "UNUSED_WITH_REASON:RMAP16_PROTECTED_APPROACH_HAS_NO_DISTINCT_SAFE_BRANCH" :
                    access.Id == "RMAP15_SITE_START_PORT_ENTRY" ? "PRESERVED_CORE_BINDING_AND_OPTIONAL_CIRCUIT" :
                    access.SiteId == "RMAP15_SITE_VILLAGE" ? "USED_BY_SV5_06_OPTIONAL_RETURN" : "PRESERVED_CORE_BINDING";
                yield return new Sv5SpacePort(access.Id, access.SiteId, access.OpenCells, anchor, access.Side,
                    access.Flow.ToString().ToUpperInvariant(), access.Condition, access.Id, access.SourceNodeId, status);
            }
        }

        private static IEnumerable<Sv5SpacePort> BuildGeneralPorts(Sv5SpacePlace place)
        {
            int y = place.Bounds.Y + place.Bounds.Height / 2;
            var left = new RmapSpecialWorldPoint(place.Bounds.X, y);
            var right = new RmapSpecialWorldPoint(place.Bounds.MaxXExclusive - 1, y);
            yield return new Sv5SpacePort(place.Id + "_PORT_IN", place.Id, new[] { left }, left,
                RmapWorldGraphDirection.Left, "IN", "OPTIONAL_ACTIONLESS", string.Empty, place.Id, "PLANNED");
            yield return new Sv5SpacePort(place.Id + "_PORT_OUT", place.Id, new[] { right }, right,
                RmapWorldGraphDirection.Right, "OUT", "OPTIONAL_ACTIONLESS", string.Empty, place.Id, "PLANNED");
        }

        private static IEnumerable<Sv5SpaceConnection> BuildCoreConnections(Sv5CoreReservationPlan core,
            IReadOnlyDictionary<string, Sv5SpacePort> ports, ICollection<string> diagnostics)
        {
            var graphEdges = core.RouteSource.Graph.Edges.ToDictionary(value => value.EdgeId, value => value,
                StringComparer.Ordinal);
            foreach (Sv5CoreRouteReservation route in core.Routes)
            {
                if (!graphEdges.TryGetValue(route.RouteId, out RmapWorldGraphEdge edge) ||
                    !ports.TryGetValue(route.FromPortId, out Sv5SpacePort from) ||
                    !ports.TryGetValue(route.ToPortId, out Sv5SpacePort to))
                {
                    diagnostics.Add("CORE_CONNECTION_BINDING_MISSING|" + route.RouteId);
                    continue;
                }
                RmapSpecialWorldPoint[] attached = AttachToPorts(route.Cells, from, to);
                if (!from.BoundaryCells.Contains(attached.First()) || !to.BoundaryCells.Contains(attached.Last()))
                    diagnostics.Add("CORE_CONNECTION_ENDPOINT_MISMATCH|" + route.RouteId);
                yield return new Sv5SpaceConnection("SV5_CORE_CONN_" + route.RouteId.Substring(0, 16),
                    Sv5SpaceConnectionKind.CoreProgression, from.Id, to.Id, from.PlaceId, to.PlaceId,
                    edge.Direction, "ONE_WAY", edge.TraversalCondition, edge.EdgeId, "REQUIRED_CORE",
                    attached, core.RouteCells.Where(value => value.RouteId == route.RouteId &&
                        (value.Kind == Sv5CoreRouteReservationKind.Passage ||
                         value.Kind == Sv5CoreRouteReservationKind.Clearance)).Select(value => value.World)
                        .Concat(attached).Concat(from.BoundaryCells).Concat(to.BoundaryCells),
                    from.BoundaryCells.Concat(to.BoundaryCells));
            }
        }

        private static List<Sv5SpaceConnection> RepairGateCorridors(Sv5CoreReservationPlan core,
            IReadOnlyList<Sv5SpacePlace> places, IReadOnlyDictionary<string, Sv5SpacePort> ports,
            IReadOnlyList<Sv5SpaceConnection> source, ISet<RmapSpecialWorldPoint> hardBlocked)
        {
            var edges = core.RouteSource.Graph.Edges.ToDictionary(e => e.EdgeId);
            bool Guarded(Sv5SpaceConnection c) => c.Kind == Sv5SpaceConnectionKind.CoreProgression &&
                (edges[c.SourceGraphEdgeId].RequiresForge || edges[c.SourceGraphEdgeId].RequiresSeal ||
                 edges[c.SourceGraphEdgeId].RequiresBossComplete);
            var guarded = source.Where(Guarded).ToArray();
            var normalAdapters = new HashSet<RmapSpecialWorldPoint>(Envelope(Envelope(source.Where(c => !Guarded(c))
                .SelectMany(c => c.ApertureCells))));
            var result = new List<Sv5SpaceConnection>();
            // Reserve the Seal and Boss corridors first. Normal traffic routes around their
            // actual narrow geometry, not around artificial global coordinate partitions.
            foreach (var c in guarded.Where(c => !edges[c.SourceGraphEdgeId].RequiresForge)
                .OrderBy(c => edges[c.SourceGraphEdgeId].RequiresSeal ? 0 : 1))
                AddGuarded(c);
            var isolated = new HashSet<RmapSpecialWorldPoint>(result.SelectMany(c => c.Centerline.Concat(c.ApertureCells)));
            bool Reserved(RmapSpecialWorldPoint p) => isolated.Contains(p) || Neighbors(p).Any(isolated.Contains);
            foreach (var c in source.Where(c => !Guarded(c)).OrderBy(c => c))
                result.Add(c.Centerline.Concat(c.ApertureCells).Any(Reserved) ? Route(c, Reserved) : c);
            foreach (var c in guarded.Where(c => edges[c.SourceGraphEdgeId].RequiresForge)) AddGuarded(c);

            void AddGuarded(Sv5SpaceConnection c)
            {
                var ownPorts = ports[c.FromPortId].BoundaryCells.Concat(ports[c.ToPortId].BoundaryCells).ToArray();
                // Forge's source-side corridor may join normal traffic. It may not touch the
                // other gated corridors except at its declared shared Seal entry aperture.
                var foreign = new HashSet<RmapSpecialWorldPoint>(result.Where(r =>
                    !edges[c.SourceGraphEdgeId].RequiresForge || Guarded(r))
                    .SelectMany(r => r.Centerline.Concat(r.ApertureCells)));
                bool Forbidden(RmapSpecialWorldPoint p) =>
                    (!edges[c.SourceGraphEdgeId].RequiresForge && normalAdapters.Contains(p)) ||
                    (!ownPorts.Any(q => Distance(p,q) <= 3) &&
                     (foreign.Contains(p) || Neighbors(p).Any(foreign.Contains)));
                result.Add(Route(c, Forbidden));
            }
            return result.OrderBy(c => c).ToList();

            Sv5SpaceConnection Route(Sv5SpaceConnection c, Func<RmapSpecialWorldPoint, bool> forbidden)
            {
                var from = ports[c.FromPortId]; var to = ports[c.ToPortId];
                var own = new HashSet<RmapSpecialWorldPoint>(c.ApertureCells);
                var blocked = new HashSet<RmapSpecialWorldPoint>(hardBlocked);
                foreach (var cell in core.CoreCells) if (!own.Contains(cell.World)) blocked.Add(cell.World);
                foreach (var place in places.Where(p => p.Kind != Sv5SpacePlaceKind.Core &&
                    p.Id != c.FromPlaceId && p.Id != c.ToPlaceId)) AddBounds(blocked, place.Bounds, 1);
                var path = FindPath(c.Centerline.First(), c.Centerline.Last(),
                    p => !blocked.Contains(p) && !forbidden(p) && (c.Kind == Sv5SpaceConnectionKind.CoreProgression ||
                        own.Contains(p) || Envelope(new[] { p }).All(q => !blocked.Contains(q) || own.Contains(q))));
                if (path.Count == 0) throw new InvalidOperationException("CORRIDOR_REROUTE_UNAVAILABLE|" + c.Id +
                    "|seed=" + core.RouteSource.Definition.Request.Seed + "|from=" + c.Centerline.First() + "|to=" + c.Centerline.Last());
                return new Sv5SpaceConnection(c.Id, c.Kind, c.FromPortId, c.ToPortId, c.FromPlaceId, c.ToPlaceId,
                    c.Kind == Sv5SpaceConnectionKind.CoreProgression ? c.Direction : Direction(path[0],path[1]),
                    c.Flow, c.Condition, c.SourceGraphEdgeId, c.SelectionState, path,
                    Envelope(path.Where(p => !own.Contains(p))).Concat(c.ApertureCells).Concat(path), c.ApertureCells);
            }
        }

        private static void BuildOptionalCircuit(Sv5CoreReservationPlan core, ulong seed,
            IEnumerable<Sv5SpacePlace> sourcePlaces, IReadOnlyDictionary<string, Sv5SpacePort> ports,
            ICollection<Sv5SpaceConnection> connections, ISet<RmapSpecialWorldPoint> hardBlocked,
            ISet<RmapSpecialWorldPoint> routingBlocked,
            ICollection<string> diagnostics)
        {
            Sv5SpacePort startEntry = ports["RMAP15_SITE_START_PORT_ENTRY"];
            Sv5SpacePort villageEntry = ports["RMAP15_SITE_VILLAGE_PORT_ENTRY"];
            Sv5SpacePort villageExit = ports["RMAP15_SITE_VILLAGE_PORT_EXIT"];
            var remaining = sourcePlaces.Where(value => value.Kind != Sv5SpacePlaceKind.Core).ToList();
            var ordered = new List<Sv5SpacePlace>();
            RmapSpecialWorldPoint cursor = startEntry.Anchor;
            while (remaining.Count != 0)
            {
                Sv5SpacePlace next = remaining.OrderBy(value => Distance(cursor, ports[value.Id + "_PORT_IN"].Anchor))
                    .ThenBy(value => StableRank(seed, value.DistributionSector, value.Bounds.X, value.Bounds.Y))
                    .ThenBy(value => value.Id, StringComparer.Ordinal).First();
                ordered.Add(next);
                remaining.Remove(next);
                cursor = ports[next.Id + "_PORT_OUT"].Anchor;
            }

            Sv5SpacePort from = startEntry;
            var ordinal = 0;
            foreach (Sv5SpacePlace place in ordered)
            {
                Sv5SpacePort to = ports[place.Id + "_PORT_IN"];
                AddRouted(from, to, Sv5SpaceConnectionKind.OptionalBranch, "OPTIONAL_ACTIONLESS",
                    "SV5_OPTIONAL_" + ordinal.ToString("00", CultureInfo.InvariantCulture));
                from = ports[place.Id + "_PORT_OUT"];
                ordinal++;
            }
            AddRouted(from, villageEntry, Sv5SpaceConnectionKind.OptionalBranch, "OPTIONAL_VILLAGE",
                "SV5_OPTIONAL_TO_VILLAGE");

            HashSet<RmapSpecialWorldPoint> villageAir = new HashSet<RmapSpecialWorldPoint>(core.CoreCells.Where(value =>
                    value.SiteId == "RMAP15_SITE_VILLAGE" && value.BaseCell == RmapPatternBaseCell.Air)
                .Select(value => value.World));
            villageAir.UnionWith(villageEntry.BoundaryCells);
            villageAir.UnionWith(villageExit.BoundaryCells);
            IReadOnlyList<RmapSpecialWorldPoint> interior = FindPath(villageEntry.Anchor, villageExit.Anchor,
                point => villageAir.Contains(point));
            if (interior.Count == 0)
                diagnostics.Add("VILLAGE_INTERIOR_RETURN_UNAVAILABLE");
            else
                connections.Add(new Sv5SpaceConnection("SV5_VILLAGE_INTERIOR", Sv5SpaceConnectionKind.VillageInterior,
                    villageEntry.Id, villageExit.Id, villageEntry.PlaceId, villageExit.PlaceId,
                    RmapWorldGraphDirection.Right, "BIDIRECTIONAL", "OPTIONAL_VILLAGE_NO_STATE_ACTION", string.Empty,
                    "OPTIONAL_SELECTED", interior, interior.Concat(villageEntry.BoundaryCells)
                        .Concat(villageExit.BoundaryCells), interior.Concat(villageEntry.BoundaryCells)
                        .Concat(villageExit.BoundaryCells)));

            AddRouted(villageExit, startEntry, Sv5SpaceConnectionKind.OptionalBranch, "OPTIONAL_RETURN_NO_STATE_ACTION",
                "SV5_OPTIONAL_RETURN_TO_START");

            void AddRouted(Sv5SpacePort routeFrom, Sv5SpacePort routeTo, Sv5SpaceConnectionKind kind,
                string condition, string id)
            {
                RmapSpecialWorldPoint[] fromApproach = BuildPortAdapter(routeFrom, hardBlocked, routingBlocked);
                RmapSpecialWorldPoint[] toApproach = BuildPortAdapter(routeTo, hardBlocked, routingBlocked);
                if (fromApproach.Length == 0 || toApproach.Length == 0)
                {
                    diagnostics.Add("PORT_WIDTH_TRANSITION_UNAVAILABLE|" + id);
                    return;
                }
                var aperture = new HashSet<RmapSpecialWorldPoint>(fromApproach);
                aperture.UnionWith(toApproach);
                aperture.UnionWith(routeFrom.BoundaryCells);
                aperture.UnionWith(routeTo.BoundaryCells);
                IReadOnlyList<RmapSpecialWorldPoint> middle = FindPath(fromApproach.Last(), toApproach.Last(),
                    point => Envelope(new[] { point }).All(cell => !hardBlocked.Contains(cell) &&
                        (!routingBlocked.Contains(cell) || aperture.Contains(cell))));
                if (middle.Count == 0)
                {
                    diagnostics.Add("OPTIONAL_ROUTE_UNAVAILABLE|" + id);
                    return;
                }
                var path = new List<RmapSpecialWorldPoint>(fromApproach);
                path.AddRange(middle.Skip(1));
                path.AddRange(toApproach.Reverse().Skip(1));
                RmapWorldGraphDirection direction = Direction(path[0], path[1]);
                var envelope = new HashSet<RmapSpecialWorldPoint>(aperture);
                envelope.UnionWith(Envelope(middle));
                envelope.UnionWith(path);
                RmapSpecialWorldPoint[] conflicts = envelope.Where(hardBlocked.Contains).OrderBy(value => value).ToArray();
                if (conflicts.Length != 0)
                {
                    diagnostics.Add("OPTIONAL_REQUIRED_WIDTH_BLOCKED|" + id + "|" + conflicts[0]);
                    return;
                }
                connections.Add(new Sv5SpaceConnection(id, kind, routeFrom.Id, routeTo.Id, routeFrom.PlaceId,
                    routeTo.PlaceId, direction, "BIDIRECTIONAL", condition, string.Empty, "OPTIONAL_SELECTED", path,
                    envelope, aperture));
            }
        }

        internal static string RouteKey(Sv5SpaceConnection connection) => connection.Kind ==
            Sv5SpaceConnectionKind.CoreProgression ? connection.SourceGraphEdgeId : connection.Id;

        internal static IEnumerable<Sv5RouteContactCell> BuildRouteContactCells(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> sourceConnections)
        {
            var output = new Dictionary<string, Sv5RouteContactCell>(StringComparer.Ordinal);
            // Historical route reservations remain preserved in Core/Reservations, but contacts
            // describe the current connector geometry, not a union with abandoned centerlines.
            foreach (Sv5SpaceConnection connection in (sourceConnections ?? Array.Empty<Sv5SpaceConnection>())
                         .Where(value => value != null))
            {
                var passage = new HashSet<RmapSpecialWorldPoint>(connection.Centerline);
                passage.UnionWith(connection.ApertureCells);
                foreach (RmapSpecialWorldPoint point in connection.Envelope)
                    Add(RouteKey(connection), point, passage.Contains(point) ? Sv5RouteContactCellKind.Passage :
                        Sv5RouteContactCellKind.Clearance);
            }
            return output.Values.OrderBy(value => value.World).ThenBy(value => value.RouteId, StringComparer.Ordinal)
                .ThenBy(value => value.Kind).ToArray();

            void Add(string route, RmapSpecialWorldPoint point, Sv5RouteContactCellKind kind)
            {
                string key = route + "|" + point + "|" + kind;
                if (!output.ContainsKey(key)) output.Add(key, new Sv5RouteContactCell(route, point, kind));
            }
        }

        private static IEnumerable<Sv5SpaceReservationCell> BuildReservations(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpacePlace> places, IEnumerable<Sv5SpaceConnection> connections,
            IEnumerable<Sv5SpaceGate> gates)
        {
            foreach (RmapSpecialWorldCell cell in core.CoreCells)
                yield return new Sv5SpaceReservationCell(cell.World, Sv5SpaceReservationKind.CoreProtected,
                    cell.SiteId, cell.Protection + ":" + cell.BaseCell);
            foreach (Sv5CoreRouteCellReservation cell in core.RouteCells)
                yield return new Sv5SpaceReservationCell(cell.World, Sv5SpaceReservationKind.CoreRoute,
                    cell.RouteId, cell.Kind + ":" + cell.RequiredBaseCell);
            foreach (Sv5SpacePlace place in places.Where(value => value.Kind != Sv5SpacePlaceKind.Core))
            foreach (RmapSpecialWorldPoint point in BoundsCells(place.Bounds, 0))
                yield return new Sv5SpaceReservationCell(point, Sv5SpaceReservationKind.PlannedFootprint,
                    place.Id, "PLANNED_SHELL_NOT_FINAL_TILE");
            foreach (Sv5SpaceConnection connection in connections.Where(value => value.Kind !=
                         Sv5SpaceConnectionKind.CoreProgression))
            {
                var center = new HashSet<RmapSpecialWorldPoint>(connection.Centerline);
                var aperture = new HashSet<RmapSpecialWorldPoint>(connection.ApertureCells);
                foreach (RmapSpecialWorldPoint point in connection.Envelope)
                    yield return new Sv5SpaceReservationCell(point, aperture.Contains(point) ?
                        Sv5SpaceReservationKind.PortAperture : center.Contains(point) ?
                        Sv5SpaceReservationKind.CorridorCenterline : Sv5SpaceReservationKind.CorridorClearance,
                        connection.Id, aperture.Contains(point) ? "EXPLICIT_PORT_WIDTH_TRANSITION" :
                        center.Contains(point) ? "PASSAGE_SUPPORT_LAYOUT_PENDING" : "REQUIRED_CLEARANCE");
            }
            foreach (Sv5SpaceGate gate in gates)
            foreach (RmapSpecialWorldPoint point in gate.BlockingCells)
                yield return new Sv5SpaceReservationCell(point, Sv5SpaceReservationKind.ConditionalGate,
                    gate.Id, "CELL_BLOCK|SEALED:" + gate.TypedPredicate.StableToken + "|OPEN:PASSAGE");
            foreach (Sv5SpaceGate gate in gates)
            foreach (Sv5SpaceBoundaryFace face in gate.BlockingFaces)
            {
                yield return new Sv5SpaceReservationCell(face.First, Sv5SpaceReservationKind.ConditionalGate,
                    gate.Id, "FACE_ONLY_STATE_BOUNDARY|" + face.StableToken + "|SEALED:" +
                    gate.TypedPredicate.StableToken + "|OPEN:PASSAGE");
                yield return new Sv5SpaceReservationCell(face.Second, Sv5SpaceReservationKind.ConditionalGate,
                    gate.Id, "FACE_ONLY_STATE_BOUNDARY|" + face.StableToken + "|SEALED:" +
                    gate.TypedPredicate.StableToken + "|OPEN:PASSAGE");
            }
        }

        private static void Validate(Sv5CoreReservationPlan core, IReadOnlyList<Sv5SpacePlace> places,
            IReadOnlyList<Sv5SpacePort> ports, IReadOnlyList<Sv5SpaceConnection> connections,
            IReadOnlyList<Sv5SpaceReservationCell> reservations, Sv5SpaceProjectionResult projection,
            ICollection<string> diagnostics)
        {
            if (places.Select(value => value.Id).Distinct(StringComparer.Ordinal).Count() != places.Count)
                diagnostics.Add("DUPLICATE_PLACE_ID");
            if (ports.Select(value => value.Id).Distinct(StringComparer.Ordinal).Count() != ports.Count)
                diagnostics.Add("DUPLICATE_PORT_ID");
            if (connections.Select(value => value.Id).Distinct(StringComparer.Ordinal).Count() != connections.Count)
                diagnostics.Add("DUPLICATE_CONNECTION_ID");
            for (var first = 0; first < places.Count; first++)
            for (var second = first + 1; second < places.Count; second++)
                if (Overlaps(places[first].Bounds, places[second].Bounds))
                    diagnostics.Add("PLACE_OVERLAP|" + places[first].Id + "|" + places[second].Id);
            Sv5SpacePort startExit = ports.Single(value => value.Id == "RMAP15_SITE_START_PORT_EXIT");
            if (connections.Any(value => value.FromPortId == startExit.Id || value.ToPortId == startExit.Id) ||
                !startExit.Status.StartsWith("UNUSED_WITH_REASON:", StringComparison.Ordinal))
                diagnostics.Add("START_EXIT_DECISION_INVALID");
            if (!connections.Any(value => value.ToPortId == "RMAP15_SITE_VILLAGE_PORT_ENTRY") ||
                !connections.Any(value => value.FromPortId == "RMAP15_SITE_VILLAGE_PORT_EXIT"))
                diagnostics.Add("VILLAGE_OPTIONAL_RETURN_MISSING");
            if (projection.Contacts.Any(value => !value.LogicalStateTransitionChecked ||
                !value.CoverageChecked || value.Crossing == Sv5SpaceCrossingKind.Pending))
                diagnostics.Add("UNRESOLVED_CONTACT");
            foreach (string error in Sv5SpaceGraphValidator.FindConnectionErrors(ports, connections))
                diagnostics.Add(error);
            foreach (string error in Sv5SpaceGraphValidator.FindReservationConflicts(core, reservations))
                diagnostics.Add(error);
            foreach (string error in Sv5SpaceGraphValidator.FindGateErrors(projection.Contacts, projection.Gates))
                diagnostics.Add(error);
            foreach (string error in Sv5SpaceGateGeometry.FindStateErrors(core, connections, projection.Gates,
                         projection.GateStateChecks)) diagnostics.Add(error);
            if (reservations.Any(value => value.World.X < 0 || value.World.X >= WorldWidth || value.World.Y < 0 ||
                value.World.Y >= WorldHeight)) diagnostics.Add("RESERVATION_OUT_OF_WORLD");
            if (!ReferenceEquals(core.Source, core.RouteSource.SpecialPlan) ||
                !ReferenceEquals(core.RouteSource.Definition, core.Source.BiomePlan.Definition))
                diagnostics.Add("SOURCE_IDENTITY_MISMATCH");
        }

        private static IReadOnlyList<RmapSpecialWorldPoint> FindPath(RmapSpecialWorldPoint start,
            RmapSpecialWorldPoint goal, Func<RmapSpecialWorldPoint, bool> allowed)
        {
            int count = WorldWidth * WorldHeight;
            var previous = new int[count];
            for (var index = 0; index < count; index++) previous[index] = -2;
            var queue = new int[count];
            int head = 0, tail = 0;
            int startIndex = Index(start), goalIndex = Index(goal);
            previous[startIndex] = -1;
            queue[tail++] = startIndex;
            while (head < tail && previous[goalIndex] == -2)
            {
                int currentIndex = queue[head++];
                RmapSpecialWorldPoint current = Point(currentIndex);
                foreach (RmapSpecialWorldPoint next in Neighbors(current))
                {
                    if (!InWorld(next) || !allowed(next)) continue;
                    int nextIndex = Index(next);
                    if (previous[nextIndex] != -2) continue;
                    previous[nextIndex] = currentIndex;
                    queue[tail++] = nextIndex;
                }
            }
            if (previous[goalIndex] == -2) return Array.Empty<RmapSpecialWorldPoint>();
            var result = new List<RmapSpecialWorldPoint>();
            for (int cursor = goalIndex; cursor >= 0; cursor = previous[cursor]) result.Add(Point(cursor));
            result.Reverse();
            return result;
        }

        private static IEnumerable<RmapSpecialWorldPoint> Envelope(IEnumerable<RmapSpecialWorldPoint> source)
        {
            var output = new HashSet<RmapSpecialWorldPoint>();
            foreach (RmapSpecialWorldPoint point in source ?? Array.Empty<RmapSpecialWorldPoint>())
            {
                if (InWorld(point)) output.Add(point);
                foreach (RmapSpecialWorldPoint neighbor in Neighbors(point)) if (InWorld(neighbor)) output.Add(neighbor);
            }
            return output.OrderBy(value => value);
        }

        private static RmapSpecialWorldPoint[] BuildPortAdapter(Sv5SpacePort port,
            ISet<RmapSpecialWorldPoint> hardBlocked, ISet<RmapSpecialWorldPoint> routingBlocked)
        {
            var exceptions = new HashSet<RmapSpecialWorldPoint>(PortApproach(port, 4));
            int count = WorldWidth * WorldHeight;
            var previous = new int[count];
            for (var index = 0; index < count; index++) previous[index] = -2;
            var queue = new int[count];
            int head = 0, tail = 0;
            int start = Index(port.Anchor), goal = -1;
            previous[start] = -1;
            queue[tail++] = start;
            while (head < tail && goal < 0)
            {
                int currentIndex = queue[head++];
                RmapSpecialWorldPoint current = Point(currentIndex);
                if (Distance(port.Anchor, current) >= 2 && OutwardDistance(port, current) > 0 &&
                    !routingBlocked.Contains(current) && Envelope(new[] { current }).All(value =>
                        !hardBlocked.Contains(value) && !routingBlocked.Contains(value)))
                { goal = currentIndex; break; }
                foreach (RmapSpecialWorldPoint next in Neighbors(current))
                {
                    if (!InWorld(next) || hardBlocked.Contains(next) ||
                        (routingBlocked.Contains(next) && !exceptions.Contains(next))) continue;
                    int nextIndex = Index(next);
                    if (previous[nextIndex] != -2) continue;
                    previous[nextIndex] = currentIndex;
                    queue[tail++] = nextIndex;
                }
            }
            if (goal < 0) return Array.Empty<RmapSpecialWorldPoint>();
            var output = new List<RmapSpecialWorldPoint>();
            for (int cursor = goal; cursor >= 0; cursor = previous[cursor]) output.Add(Point(cursor));
            output.Reverse();
            return output.ToArray();
        }

        private static RmapSpecialWorldPoint[] AttachToPorts(IReadOnlyList<RmapSpecialWorldPoint> route,
            Sv5SpacePort from, Sv5SpacePort to)
        {
            if (route == null || route.Count == 0) return Array.Empty<RmapSpecialWorldPoint>();
            RmapSpecialWorldPoint first = from.BoundaryCells.Where(value => Distance(value, route[0]) == 1)
                .OrderBy(value => value).FirstOrDefault();
            RmapSpecialWorldPoint last = to.BoundaryCells.Where(value => Distance(value, route[route.Count - 1]) == 1)
                .OrderBy(value => value).FirstOrDefault();
            var output = new List<RmapSpecialWorldPoint>();
            if (from.BoundaryCells.Contains(first) && Distance(first, route[0]) == 1) output.Add(first);
            output.AddRange(route);
            if (to.BoundaryCells.Contains(last) && Distance(last, route[route.Count - 1]) == 1) output.Add(last);
            return output.ToArray();
        }

        private static IEnumerable<RmapSpecialWorldPoint> BoundsCells(Sv5SpaceBounds bounds, int padding)
        {
            for (int y = Math.Max(0, bounds.Y - padding); y < Math.Min(WorldHeight, bounds.MaxYExclusive + padding); y++)
            for (int x = Math.Max(0, bounds.X - padding); x < Math.Min(WorldWidth, bounds.MaxXExclusive + padding); x++)
                yield return new RmapSpecialWorldPoint(x, y);
        }

        private static void AddBounds(ISet<RmapSpecialWorldPoint> output, Sv5SpaceBounds bounds, int padding)
        { foreach (RmapSpecialWorldPoint point in BoundsCells(bounds, padding)) output.Add(point); }

        private static IEnumerable<RmapSpecialWorldPoint> Neighbors(RmapSpecialWorldPoint point)
        {
            yield return new RmapSpecialWorldPoint(point.X - 1, point.Y);
            yield return new RmapSpecialWorldPoint(point.X + 1, point.Y);
            yield return new RmapSpecialWorldPoint(point.X, point.Y - 1);
            yield return new RmapSpecialWorldPoint(point.X, point.Y + 1);
        }

        private static RmapSpecialWorldPoint Outward(Sv5SpacePort port) => port.Direction == RmapWorldGraphDirection.Left ?
            new RmapSpecialWorldPoint(port.Anchor.X - 1, port.Anchor.Y) : port.Direction == RmapWorldGraphDirection.Right ?
            new RmapSpecialWorldPoint(port.Anchor.X + 1, port.Anchor.Y) : port.Direction == RmapWorldGraphDirection.Up ?
            new RmapSpecialWorldPoint(port.Anchor.X, port.Anchor.Y + 1) : new RmapSpecialWorldPoint(port.Anchor.X, port.Anchor.Y - 1);
        private static IEnumerable<RmapSpecialWorldPoint> PortApproach(Sv5SpacePort port, int length)
        {
            RmapSpecialWorldPoint point = port.Anchor;
            yield return point;
            for (var index = 0; index < length; index++)
            {
                point = port.Direction == RmapWorldGraphDirection.Left ? new RmapSpecialWorldPoint(point.X - 1, point.Y) :
                    port.Direction == RmapWorldGraphDirection.Right ? new RmapSpecialWorldPoint(point.X + 1, point.Y) :
                    port.Direction == RmapWorldGraphDirection.Up ? new RmapSpecialWorldPoint(point.X, point.Y + 1) :
                    new RmapSpecialWorldPoint(point.X, point.Y - 1);
                if (InWorld(point)) yield return point;
            }
        }
        private static int OutwardDistance(Sv5SpacePort port, RmapSpecialWorldPoint point) =>
            port.Direction == RmapWorldGraphDirection.Left ? port.Anchor.X - point.X :
            port.Direction == RmapWorldGraphDirection.Right ? point.X - port.Anchor.X :
            port.Direction == RmapWorldGraphDirection.Up ? point.Y - port.Anchor.Y : port.Anchor.Y - point.Y;
        private static RmapWorldGraphDirection Direction(RmapSpecialWorldPoint first, RmapSpecialWorldPoint second) =>
            second.X > first.X ? RmapWorldGraphDirection.Right : second.X < first.X ? RmapWorldGraphDirection.Left :
            second.Y > first.Y ? RmapWorldGraphDirection.Up : RmapWorldGraphDirection.Down;
        private static bool InWorld(RmapSpecialWorldPoint point) => point.X >= 0 && point.X < WorldWidth && point.Y >= 0 && point.Y < WorldHeight;
        private static int Index(RmapSpecialWorldPoint point) => point.Y * WorldWidth + point.X;
        private static RmapSpecialWorldPoint Point(int index) => new RmapSpecialWorldPoint(index % WorldWidth, index / WorldWidth);
        private static int Distance(RmapSpecialWorldPoint left, RmapSpecialWorldPoint right) => Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);
        private static int Align4(int value) => ((value + 3) / 4) * 4;
        private static int Sector(int x, int y) => Math.Min(3, Math.Max(0, x / 156)) +
            Math.Min(3, Math.Max(0, y / 104)) * 4;
        private static bool Overlaps(Sv5SpaceBounds left, Sv5SpaceBounds right) => left.X < right.MaxXExclusive &&
            left.MaxXExclusive > right.X && left.Y < right.MaxYExclusive && left.MaxYExclusive > right.Y;
        private static ulong StableRank(ulong seed, int ordinal, int x, int y)
        {
            ulong value = 1469598103934665603UL ^ seed;
            foreach (int item in new[] { ordinal, x, y })
            {
                value ^= (uint)item;
                value *= 1099511628211UL;
            }
            return value;
        }

        private sealed class Candidate
        {
            public Candidate(int x, int y, int sector, int sectorOffset, ulong rank)
            { X = x; Y = y; Sector = sector; SectorOffset = sectorOffset; Rank = rank; }
            public int X { get; }
            public int Y { get; }
            public int Sector { get; }
            public int SectorOffset { get; }
            public ulong Rank { get; }
        }
    }

    public sealed class Sv5SpaceReservationProbe
    {
        public Sv5SpaceReservationProbe(RmapSpecialWorldPoint world, Sv5SpaceReservationKind kind, string ownerId)
            : this(world, kind, ownerId, string.Empty) { }
        public Sv5SpaceReservationProbe(RmapSpecialWorldPoint world, Sv5SpaceReservationKind kind, string ownerId,
            string semantics)
        { World = world; Kind = kind; OwnerId = ownerId ?? string.Empty; Semantics = semantics ?? string.Empty; }
        public RmapSpecialWorldPoint World { get; }
        public Sv5SpaceReservationKind Kind { get; }
        public string OwnerId { get; }
        public string Semantics { get; }
    }

    /// <summary>Production validation used by planning and by FIX01 negative fixtures.</summary>
    public static class Sv5SpaceGraphValidator
    {
        public static IReadOnlyList<Sv5RouteContactCell> AcceptedContactCells(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceConnection> connections)
        {
            if (core == null) throw new ArgumentNullException(nameof(core));
            return new ReadOnlyCollection<Sv5RouteContactCell>(Sv5SpaceGraphPlanner
                .BuildRouteContactCells(core, connections).ToArray());
        }

        public static IReadOnlyList<string> FindContactCoverageErrors(IEnumerable<Sv5RouteContactCell> sourceCells,
            IEnumerable<Sv5RouteContactPair> sourcePairs)
        {
            Sv5RouteContactCell[] cells = (sourceCells ?? Array.Empty<Sv5RouteContactCell>())
                .Where(value => value != null).ToArray();
            var groups = cells.GroupBy(value => value.World).ToDictionary(value => value.Key,
                value => value.Select(item => item.RouteId).Distinct(StringComparer.Ordinal)
                    .OrderBy(item => item, StringComparer.Ordinal).ToArray());
            var expected = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<RmapSpecialWorldPoint, string[]> entry in groups)
            {
                AddPairs("SHARED", entry.Key, entry.Key, entry.Value, entry.Value, true);
                foreach (RmapSpecialWorldPoint neighbor in new[]
                {
                    new RmapSpecialWorldPoint(entry.Key.X + 1, entry.Key.Y),
                    new RmapSpecialWorldPoint(entry.Key.X, entry.Key.Y + 1),
                })
                    if (groups.TryGetValue(neighbor, out string[] other))
                        AddPairs("FACE", entry.Key, neighbor, entry.Value, other, false);
            }
            Sv5RouteContactPair[] pairs = (sourcePairs ?? Array.Empty<Sv5RouteContactPair>())
                .Where(value => value != null).ToArray();
            var actual = new HashSet<string>(pairs.Select(ContactKey), StringComparer.Ordinal);
            var errors = expected.Except(actual).OrderBy(value => value, StringComparer.Ordinal)
                .Select(value => "CONTACT_COVERAGE_MISSING|" + value).ToList();
            errors.AddRange(actual.Except(expected).OrderBy(value => value, StringComparer.Ordinal)
                .Select(value => "CONTACT_COVERAGE_EXTRA|" + value));
            errors.AddRange(pairs.GroupBy(ContactKey, StringComparer.Ordinal).Where(value => value.Count() != 1)
                .Select(value => "CONTACT_COVERAGE_DUPLICATE|" + value.Key));
            return new ReadOnlyCollection<string>(errors.ToArray());

            void AddPairs(string kind, RmapSpecialWorldPoint first, RmapSpecialWorldPoint second,
                string[] left, string[] right, bool sameCell)
            {
                for (var a = 0; a < left.Length; a++)
                for (var b = sameCell ? a + 1 : 0; b < right.Length; b++)
                {
                    if (string.Equals(left[a], right[b], StringComparison.Ordinal)) continue;
                    string routeA = string.Compare(left[a], right[b], StringComparison.Ordinal) < 0 ? left[a] : right[b];
                    string routeB = string.Equals(routeA, left[a], StringComparison.Ordinal) ? right[b] : left[a];
                    expected.Add(ContactKey(kind, first, second, routeA, routeB));
                }
            }
        }

        public static IReadOnlyList<string> FindConnectionErrors(IEnumerable<Sv5SpacePort> sourcePorts,
            IEnumerable<Sv5SpaceConnection> sourceConnections)
        {
            var errors = new List<string>();
            var ports = (sourcePorts ?? Array.Empty<Sv5SpacePort>()).Where(value => value != null)
                .GroupBy(value => value.Id, StringComparer.Ordinal).ToDictionary(value => value.Key,
                    value => value.First(), StringComparer.Ordinal);
            foreach (Sv5SpaceConnection connection in (sourceConnections ?? Array.Empty<Sv5SpaceConnection>())
                         .Where(value => value != null))
            {
                if (!ports.TryGetValue(connection.FromPortId, out Sv5SpacePort from) ||
                    !ports.TryGetValue(connection.ToPortId, out Sv5SpacePort to))
                {
                    errors.Add("CONNECTION_UNKNOWN_PORT|" + connection.Id);
                    continue;
                }
                if (!string.Equals(from.PlaceId, connection.FromPlaceId, StringComparison.Ordinal) ||
                    !string.Equals(to.PlaceId, connection.ToPlaceId, StringComparison.Ordinal))
                    errors.Add("CONNECTION_PLACE_PORT_MISMATCH|" + connection.Id);
                if (!from.BoundaryCells.Contains(connection.Centerline.First()) ||
                    !to.BoundaryCells.Contains(connection.Centerline.Last()))
                    errors.Add("CONNECTION_ENDPOINT_MISMATCH|" + connection.Id);
                if (!string.Equals(connection.Flow, "ONE_WAY", StringComparison.Ordinal) &&
                    !string.Equals(connection.Flow, "BIDIRECTIONAL", StringComparison.Ordinal))
                    errors.Add("CONNECTION_UNKNOWN_FLOW|" + connection.Id);
                if (connection.Kind != Sv5SpaceConnectionKind.CoreProgression &&
                    !string.Equals(connection.Flow, "BIDIRECTIONAL", StringComparison.Ordinal))
                    errors.Add("CONNECTION_OPTIONAL_NOT_BIDIRECTIONAL|" + connection.Id);
                if (connection.Kind != Sv5SpaceConnectionKind.CoreProgression &&
                    GeometryDirection(connection.Centerline[0], connection.Centerline[1]) != connection.Direction)
                    errors.Add("CONNECTION_DIRECTION_MISMATCH|" + connection.Id);
                if (connection.Kind != Sv5SpaceConnectionKind.CoreProgression && connection.Centerline.Count > 1 &&
                    GeometryDirection(connection.Centerline[0], connection.Centerline[1]) != connection.Direction)
                    errors.Add("CONNECTION_DIRECTION_MISMATCH|" + connection.Id);
                var envelope = new HashSet<RmapSpecialWorldPoint>(connection.Envelope);
                if (connection.Centerline.Any(value => !envelope.Contains(value)) ||
                    connection.ApertureCells.Any(value => !envelope.Contains(value)))
                    errors.Add("CONNECTION_ENVELOPE_INCOMPLETE|" + connection.Id);
                var aperture = new HashSet<RmapSpecialWorldPoint>(connection.ApertureCells);
                var apertureReachable = new HashSet<RmapSpecialWorldPoint>(from.BoundaryCells
                    .Concat(to.BoundaryCells).Where(aperture.Contains));
                var apertureQueue = new Queue<RmapSpecialWorldPoint>(apertureReachable);
                while (apertureQueue.Count != 0)
                {
                    RmapSpecialWorldPoint current = apertureQueue.Dequeue();
                    foreach (RmapSpecialWorldPoint next in CardinalNeighbors(current))
                        if (aperture.Contains(next) && apertureReachable.Add(next)) apertureQueue.Enqueue(next);
                }
                if (aperture.Count != 0 && apertureReachable.Count != aperture.Count)
                    errors.Add("CONNECTION_APERTURE_NOT_PORT_REACHABLE|" + connection.Id);
                if (connection.Kind != Sv5SpaceConnectionKind.CoreProgression)
                    foreach (RmapSpecialWorldPoint point in connection.Centerline.Where(value => !aperture.Contains(value)))
                        if (!CardinalCross(point).All(envelope.Contains))
                        { errors.Add("CONNECTION_REQUIRED_WIDTH_NARROW|" + connection.Id + "|" + point); break; }
            }
            return new ReadOnlyCollection<string>(errors.Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        public static IReadOnlyList<string> FindReservationConflicts(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceReservationCell> sourceReservations)
        {
            return FindRequiredReservationConflicts(core, (sourceReservations ??
                Array.Empty<Sv5SpaceReservationCell>()).Where(value => value != null)
                .Select(value => new Sv5SpaceReservationProbe(value.World, value.Kind, value.OwnerId,
                    value.Semantics)));
        }

        public static IReadOnlyList<string> FindRequiredReservationConflicts(Sv5CoreReservationPlan core,
            IEnumerable<Sv5SpaceReservationProbe> sourceReservations)
        {
            if (core == null) throw new ArgumentNullException(nameof(core));
            var fixedSolid = new HashSet<RmapSpecialWorldPoint>(core.CoreCells.Where(value =>
                value.Protection == RmapSpecialProtectionKind.FixedSolid).Select(value => value.World));
            var routeSolid = new HashSet<RmapSpecialWorldPoint>(core.RouteCells.Where(value =>
                value.RequiredBaseCell == RmapPatternBaseCell.Solid).Select(value => value.World));
            var protectedAir = new HashSet<RmapSpecialWorldPoint>(core.CoreCells.Where(value =>
                value.Protection == RmapSpecialProtectionKind.ProtectedAir).Select(value => value.World));
            var errors = new List<string>();
            foreach (Sv5SpaceReservationProbe value in (sourceReservations ?? Array.Empty<Sv5SpaceReservationProbe>())
                         .Where(value => value != null && (value.Kind == Sv5SpaceReservationKind.CorridorCenterline ||
                             value.Kind == Sv5SpaceReservationKind.CorridorClearance ||
                             value.Kind == Sv5SpaceReservationKind.PortAperture ||
                             value.Kind == Sv5SpaceReservationKind.ConditionalGate)))
            {
                if (value.Kind != Sv5SpaceReservationKind.ConditionalGate)
                {
                    if (fixedSolid.Contains(value.World)) errors.Add("RESERVATION_FIXED_SOLID_CONFLICT|" + value.World + "|" + value.OwnerId);
                    if (routeSolid.Contains(value.World)) errors.Add("RESERVATION_ROUTE_SUPPORT_CONFLICT|" + value.World + "|" + value.OwnerId);
                }
                if (value.Kind == Sv5SpaceReservationKind.ConditionalGate && protectedAir.Contains(value.World) &&
                    !value.Semantics.StartsWith("FACE_ONLY_STATE_BOUNDARY|", StringComparison.Ordinal))
                    errors.Add("RESERVATION_PROTECTED_AIR_STATE_CONFLICT|" + value.World + "|" + value.OwnerId);
            }
            return new ReadOnlyCollection<string>(errors.Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        public static IReadOnlyList<string> ValidatePortTransitionFixture(IEnumerable<Sv5SpacePort> sourcePorts,
            string fromPortId, string toPortId, string fromPlaceId, string toPlaceId, string flow,
            RmapWorldGraphDirection direction,
            IEnumerable<RmapSpecialWorldPoint> sourceCenterline,
            IEnumerable<RmapSpecialWorldPoint> sourceEnvelope,
            IEnumerable<RmapSpecialWorldPoint> sourceAperture)
        {
            try
            {
                var connection = new Sv5SpaceConnection("FIXTURE", Sv5SpaceConnectionKind.OptionalBranch,
                    fromPortId, toPortId, fromPlaceId, toPlaceId, direction, flow, "FIXTURE", string.Empty,
                    "FIXTURE", sourceCenterline, sourceEnvelope, sourceAperture);
                return FindConnectionErrors(sourcePorts, new[] { connection });
            }
            catch (ArgumentException error)
            {
                return new ReadOnlyCollection<string>(new[] { "CONNECTION_MALFORMED|FIXTURE|" + error.ParamName });
            }
        }

        public static IReadOnlyList<string> ValidateBarrierFixture(Sv5RouteContactPair contact,
            IEnumerable<RmapSpecialWorldPoint> sourceBlockingCells,
            IEnumerable<Sv5SpaceBoundaryFace> sourceBlockingFaces)
        {
            if (contact == null) throw new ArgumentNullException(nameof(contact));
            var cells = new HashSet<RmapSpecialWorldPoint>(sourceBlockingCells ?? Array.Empty<RmapSpecialWorldPoint>());
            var faces = new HashSet<string>((sourceBlockingFaces ?? Array.Empty<Sv5SpaceBoundaryFace>())
                .Where(value => value != null).Select(value => value.StableToken), StringComparer.Ordinal);
            var errors = new List<string>();
            if (contact.Kind == "SHARED" && !cells.Contains(contact.FirstWorld))
                errors.Add("GATE_SHARED_CELL_BYPASS|" + contact.Id);
            if (contact.Kind == "FACE" && !faces.Contains(FaceToken(contact.FirstWorld, contact.SecondWorld)))
                errors.Add("GATE_FACE_BYPASS|" + contact.Id);
            return new ReadOnlyCollection<string>(errors.ToArray());
        }

        public static IReadOnlyList<string> FindGateErrors(IEnumerable<Sv5SpaceContactDecision> sourceContacts,
            IEnumerable<Sv5SpaceGate> sourceGates)
        {
            Sv5SpaceContactDecision[] contacts = (sourceContacts ?? Array.Empty<Sv5SpaceContactDecision>())
                .Where(value => value != null).ToArray();
            Sv5SpaceGate[] gates = (sourceGates ?? Array.Empty<Sv5SpaceGate>()).Where(value => value != null).ToArray();
            var errors = new List<string>();
            foreach (Sv5SpaceContactDecision contact in contacts)
            {
                if (contact.Crossing != Sv5SpaceCrossingKind.ConditionalGate)
                {
                    if (contact.Crossing != Sv5SpaceCrossingKind.Join || !string.IsNullOrEmpty(contact.BoundaryId) ||
                        gates.Any(g => ValidateBarrierFixture(contact.Source,g.BlockingCells,g.BlockingFaces).Count == 0))
                        errors.Add("CONTACT_LOCAL_BARRIER_DECISION_MISMATCH|" + contact.Source.Id);
                    continue;
                }
                Sv5SpaceGate gate = gates.SingleOrDefault(value => string.Equals(value.BoundaryId,
                    contact.BoundaryId, StringComparison.Ordinal));
                if (gate == null || !gate.ContactIds.Contains(contact.Source.Id))
                { errors.Add("GATE_CONTACT_UNCOVERED|" + contact.Source.Id); continue; }
                if (string.IsNullOrWhiteSpace(contact.BoundaryId) ||
                    gate.BlockingFaces.Count + gate.BlockingCells.Count == 0)
                    errors.Add("GATE_GLOBAL_BOUNDARY_MISSING|" + contact.Source.Id);
                errors.AddRange(ValidateBarrierFixture(contact.Source, gate.BlockingCells, gate.BlockingFaces));
            }
            foreach (Sv5SpaceGate gate in gates)
                if (!gate.PlannedBarrierVerified) errors.Add("GATE_GEOMETRY_INVALID|" + gate.Id);
            return new ReadOnlyCollection<string>(errors.Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        private static IEnumerable<RmapSpecialWorldPoint> CardinalCross(RmapSpecialWorldPoint point)
        {
            yield return point;
            if (point.X > 0) yield return new RmapSpecialWorldPoint(point.X - 1, point.Y);
            if (point.X < Sv5SpaceGraphPlanner.WorldWidth - 1) yield return new RmapSpecialWorldPoint(point.X + 1, point.Y);
            if (point.Y > 0) yield return new RmapSpecialWorldPoint(point.X, point.Y - 1);
            if (point.Y < Sv5SpaceGraphPlanner.WorldHeight - 1) yield return new RmapSpecialWorldPoint(point.X, point.Y + 1);
        }
        private static IEnumerable<RmapSpecialWorldPoint> CardinalNeighbors(RmapSpecialWorldPoint point)
        {
            if (point.X > 0) yield return new RmapSpecialWorldPoint(point.X - 1, point.Y);
            if (point.X < Sv5SpaceGraphPlanner.WorldWidth - 1) yield return new RmapSpecialWorldPoint(point.X + 1, point.Y);
            if (point.Y > 0) yield return new RmapSpecialWorldPoint(point.X, point.Y - 1);
            if (point.Y < Sv5SpaceGraphPlanner.WorldHeight - 1) yield return new RmapSpecialWorldPoint(point.X, point.Y + 1);
        }
        private static string ContactKey(Sv5RouteContactPair value) => ContactKey(value.Kind, value.FirstWorld,
            value.SecondWorld, value.RouteA, value.RouteB);
        private static string ContactKey(string kind, RmapSpecialWorldPoint first, RmapSpecialWorldPoint second,
            string routeA, string routeB) => kind + "|" + first + "|" + second + "|" + routeA + "|" + routeB;
        private static string FaceToken(RmapSpecialWorldPoint first, RmapSpecialWorldPoint second) =>
            first.CompareTo(second) <= 0 ? first + ">" + second : second + ">" + first;
        private static RmapWorldGraphDirection GeometryDirection(RmapSpecialWorldPoint first,
            RmapSpecialWorldPoint second) => second.X > first.X ? RmapWorldGraphDirection.Right :
            second.X < first.X ? RmapWorldGraphDirection.Left :
            second.Y > first.Y ? RmapWorldGraphDirection.Up : RmapWorldGraphDirection.Down;
    }
}
