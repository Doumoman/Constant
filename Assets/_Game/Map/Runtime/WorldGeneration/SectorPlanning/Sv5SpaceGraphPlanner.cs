using System;
using System.Collections.Generic;
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

        public static Sv5SpaceGraphPlan Plan(Sv5CoreReservationPlan core, ulong seed,
            Sv5SpaceGraphAuthoringProfile profile = null)
        {
            if (core == null) throw new ArgumentNullException(nameof(core));
            if (core.RouteSource.Definition.Request.Seed != seed)
                throw new ArgumentException("The explicit layout seed must equal the preserved world definition seed.", nameof(seed));
            if (core.Sites.Count != 8 || core.CoreCells.Count != 2432 || !core.RouteSource.Graph.Success)
                throw new ArgumentException("A passing SV5_04/RMAP13 source is required.", nameof(core));
            profile = profile ?? Sv5SpaceGraphAuthoringProfile.RepresentativeV1();

            var diagnostics = new List<string>();
            List<Sv5SpacePlace> places = BuildCorePlaces(core).ToList();
            var placementBlocked = new HashSet<RmapSpecialWorldPoint>(core.CoreCells.Select(value => value.World));
            placementBlocked.UnionWith(core.RouteCells.Select(value => value.World));
            foreach (Sv5SpaceFamilySpec spec in profile.Families)
            {
                Sv5SpacePlace place = PlaceFamily(spec, seed, placementBlocked);
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

            var routingBlocked = new HashSet<RmapSpecialWorldPoint>(core.CoreCells.Select(value => value.World));
            var passageCells = new HashSet<RmapSpecialWorldPoint>(core.RouteCells.Where(value =>
                value.Kind == Sv5CoreRouteReservationKind.Passage).Select(value => value.World));
            routingBlocked.UnionWith(core.RouteCells.Where(value => value.Kind != Sv5CoreRouteReservationKind.Passage &&
                !passageCells.Contains(value.World)).Select(value => value.World));
            foreach (Sv5SpacePlace place in places.Where(value => value.Kind != Sv5SpacePlaceKind.Core))
                AddBounds(routingBlocked, place.Bounds, 2);
            BuildOptionalCircuit(core, seed, places, byPort, connections, routingBlocked, diagnostics);

            var routeCells = core.RouteCells.Where(value =>
                    value.Kind == Sv5CoreRouteReservationKind.Passage ||
                    value.Kind == Sv5CoreRouteReservationKind.Clearance)
                .Select(value => new Sv5RouteContactCell(value.RouteId, value.World,
                    value.Kind == Sv5CoreRouteReservationKind.Passage ? Sv5RouteContactCellKind.Passage :
                    Sv5RouteContactCellKind.Clearance)).ToList();
            routeCells.AddRange(connections.SelectMany(connection => connection.Centerline.Select(point =>
                new Sv5RouteContactCell(RouteKey(connection), point, Sv5RouteContactCellKind.Passage))));
            IReadOnlyList<Sv5RouteContactPair> contactPairs = Sv5RouteStatePolicy.EnumerateContactPairs(routeCells);
            Sv5SpaceProjectionResult projection = Sv5SpaceGraphStateProjection.Project(core, connections, contactPairs);
            diagnostics.AddRange(projection.Diagnostics);

            List<Sv5SpaceReservationCell> reservations = BuildReservations(core, places, connections,
                projection.Gates).ToList();
            Validate(core, places, ports, connections, reservations, projection, diagnostics);
            return new Sv5SpaceGraphPlan(core, seed, profile, places, ports, connections, projection.Gates,
                reservations, projection.Contacts, projection.Proofs, diagnostics);
        }

        private static IEnumerable<Sv5SpacePlace> BuildCorePlaces(Sv5CoreReservationPlan core)
        {
            foreach (RmapSpecialSite site in core.Sites)
                yield return new Sv5SpacePlace(site.Id, site.TemplateId, Sv5SpacePlaceKind.Core,
                    new Sv5SpaceBounds(site.Origin.X, site.Origin.Y, site.WidthTiles, site.HeightTiles), site.Id,
                    "PRESERVED_RMAP15", Sector(site.Origin.X + site.WidthTiles / 2, site.Origin.Y + site.HeightTiles / 2));
        }

        private static Sv5SpacePlace PlaceFamily(Sv5SpaceFamilySpec spec, ulong seed,
            ISet<RmapSpecialWorldPoint> blocked)
        {
            int preferred = (spec.Ordinal + (int)(seed % 16UL)) % 16;
            var candidates = new List<Candidate>();
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
                    candidates.Add(new Candidate(x, y, sector, sectorOffset,
                        StableRank(seed, spec.Ordinal, x, y)));
            }
            foreach (Candidate candidate in candidates.OrderBy(value => value.SectorOffset)
                         .ThenBy(value => value.Rank).ThenBy(value => value.Y).ThenBy(value => value.X))
            {
                var bounds = new Sv5SpaceBounds(candidate.X, candidate.Y, spec.Width, spec.Height);
                if (BoundsCells(bounds, 2).Any(blocked.Contains)) continue;
                string id = "SV5_PLACE_" + spec.Ordinal.ToString("00", CultureInfo.InvariantCulture) + "_" +
                    RmapWorldDefinition.Hash(seed.ToString(CultureInfo.InvariantCulture) + "|" + spec.StableToken + "|" + bounds)
                        .Substring(0, 16).ToUpperInvariant();
                return new Sv5SpacePlace(id, spec.Family, spec.Kind, bounds, string.Empty, spec.FutureOwner,
                    candidate.Sector);
            }
            return null;
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
                    attached, Envelope(attached));
            }
        }

        private static void BuildOptionalCircuit(Sv5CoreReservationPlan core, ulong seed,
            IEnumerable<Sv5SpacePlace> sourcePlaces, IReadOnlyDictionary<string, Sv5SpacePort> ports,
            ICollection<Sv5SpaceConnection> connections, ISet<RmapSpecialWorldPoint> routingBlocked,
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
                    RmapWorldGraphDirection.Right, "ONE_WAY", "OPTIONAL_VILLAGE_NO_STATE_ACTION", string.Empty,
                    "OPTIONAL_SELECTED", interior, interior));

            AddRouted(villageExit, startEntry, Sv5SpaceConnectionKind.OptionalBranch, "OPTIONAL_RETURN_NO_STATE_ACTION",
                "SV5_OPTIONAL_RETURN_TO_START");

            void AddRouted(Sv5SpacePort routeFrom, Sv5SpacePort routeTo, Sv5SpaceConnectionKind kind,
                string condition, string id)
            {
                var exceptions = new HashSet<RmapSpecialWorldPoint>(PortApproach(routeFrom, 4));
                exceptions.UnionWith(PortApproach(routeTo, 4));
                IReadOnlyList<RmapSpecialWorldPoint> path = FindPath(routeFrom.Anchor, routeTo.Anchor,
                    point => exceptions.Contains(point) || !routingBlocked.Contains(point));
                if (path.Count == 0)
                {
                    diagnostics.Add("OPTIONAL_ROUTE_UNAVAILABLE|" + id);
                    return;
                }
                RmapWorldGraphDirection direction = Direction(path[0], path[1]);
                RmapSpecialWorldPoint[] envelope = Envelope(path).ToArray();
                connections.Add(new Sv5SpaceConnection(id, kind, routeFrom.Id, routeTo.Id, routeFrom.PlaceId,
                    routeTo.PlaceId, direction, "ONE_WAY", condition, string.Empty, "OPTIONAL_SELECTED", path, envelope));
            }
        }

        internal static string RouteKey(Sv5SpaceConnection connection) => connection.Kind ==
            Sv5SpaceConnectionKind.CoreProgression ? connection.SourceGraphEdgeId : connection.Id;

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
                foreach (RmapSpecialWorldPoint point in connection.Envelope)
                    yield return new Sv5SpaceReservationCell(point, center.Contains(point) ?
                        Sv5SpaceReservationKind.CorridorCenterline : Sv5SpaceReservationKind.CorridorClearance,
                        connection.Id, center.Contains(point) ? "SUPPORT_LAYOUT_PENDING" : "CLEARANCE_RESERVED");
            }
            foreach (Sv5SpaceGate gate in gates)
                yield return new Sv5SpaceReservationCell(gate.World, Sv5SpaceReservationKind.ConditionalGate,
                    gate.Id, gate.Predicate);
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
                value.Crossing == Sv5SpaceCrossingKind.Pending))
                diagnostics.Add("UNRESOLVED_CONTACT");
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
}
