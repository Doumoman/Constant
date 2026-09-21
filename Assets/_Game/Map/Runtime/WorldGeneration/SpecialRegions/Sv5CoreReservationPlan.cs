using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public enum Sv5CoreRouteReservationKind
    {
        Passage = 1,
        Clearance = 2,
        Support = 3,
    }

    public enum Sv5CoreReservationDiagnosticCode
    {
        OutOfWorld = 1,
        DuplicateCandidate = 2,
        StateGeometry = 3,
        ProtectedCoreCell = 4,
        RoutePassageBlocked = 5,
        RouteClearanceBlocked = 6,
        RouteSupportRemoved = 7,
    }

    public sealed class Sv5CoreTerrainCandidate
    {
        public Sv5CoreTerrainCandidate(string consumerId, Sv5SpecialWorldPoint world, Sv5PatternBaseCell baseCell)
        {
            if (string.IsNullOrWhiteSpace(consumerId)) throw new ArgumentException("A consumer id is required.", nameof(consumerId));
            if (!Enum.IsDefined(typeof(Sv5PatternBaseCell), baseCell)) throw new ArgumentOutOfRangeException(nameof(baseCell));
            ConsumerId = consumerId.Trim();
            World = world;
            BaseCell = baseCell;
        }

        public string ConsumerId { get; }
        public Sv5SpecialWorldPoint World { get; }
        public Sv5PatternBaseCell BaseCell { get; }
    }

    public sealed class Sv5CoreReservationDiagnostic : IComparable<Sv5CoreReservationDiagnostic>
    {
        internal Sv5CoreReservationDiagnostic(Sv5CoreReservationDiagnosticCode code, Sv5SpecialWorldPoint world,
            string ownerId, string reason)
        {
            Code = code;
            World = world;
            OwnerId = ownerId ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public Sv5CoreReservationDiagnosticCode Code { get; }
        public Sv5SpecialWorldPoint World { get; }
        public string OwnerId { get; }
        public string Reason { get; }

        public int CompareTo(Sv5CoreReservationDiagnostic other)
        {
            if (other == null) return 1;
            int value = World.CompareTo(other.World);
            if (value != 0) return value;
            value = Code.CompareTo(other.Code);
            return value != 0 ? value : string.Compare(OwnerId, other.OwnerId, StringComparison.Ordinal);
        }
    }

    public sealed class Sv5CoreReservationDecision
    {
        internal Sv5CoreReservationDecision(IEnumerable<Sv5CoreReservationDiagnostic> values)
        {
            Diagnostics = new ReadOnlyCollection<Sv5CoreReservationDiagnostic>((values ??
                Array.Empty<Sv5CoreReservationDiagnostic>()).OrderBy(value => value).ToArray());
        }

        public IReadOnlyList<Sv5CoreReservationDiagnostic> Diagnostics { get; }
        public bool IsAllowed => Diagnostics.Count == 0;
    }

    public sealed class Sv5CoreRouteCellReservation : IComparable<Sv5CoreRouteCellReservation>
    {
        internal Sv5CoreRouteCellReservation(string routeId, int ordinal, Sv5CoreRouteReservationKind kind,
            Sv5SpecialWorldPoint world, Sv5PatternBaseCell requiredBaseCell, string sourceId)
        {
            RouteId = Sv5CoreReservationPlan.Require(routeId, nameof(routeId));
            if (ordinal < 0) throw new ArgumentOutOfRangeException(nameof(ordinal));
            if (!Enum.IsDefined(typeof(Sv5CoreRouteReservationKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (!Enum.IsDefined(typeof(Sv5PatternBaseCell), requiredBaseCell)) throw new ArgumentOutOfRangeException(nameof(requiredBaseCell));
            Ordinal = ordinal;
            Kind = kind;
            World = world;
            RequiredBaseCell = requiredBaseCell;
            SourceId = Sv5CoreReservationPlan.Require(sourceId, nameof(sourceId));
        }

        public string RouteId { get; }
        public int Ordinal { get; }
        public Sv5CoreRouteReservationKind Kind { get; }
        public Sv5SpecialWorldPoint World { get; }
        public Sv5PatternBaseCell RequiredBaseCell { get; }
        public string SourceId { get; }

        public int CompareTo(Sv5CoreRouteCellReservation other)
        {
            if (other == null) return 1;
            int value = string.Compare(RouteId, other.RouteId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = Ordinal.CompareTo(other.Ordinal);
            if (value != 0) return value;
            return Kind.CompareTo(other.Kind);
        }
    }

    public sealed class Sv5CoreRouteReservation : IComparable<Sv5CoreRouteReservation>
    {
        internal Sv5CoreRouteReservation(Sv5Route source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public Sv5Route Source { get; }
        public string RouteId => Source.EdgeId;
        public string FromPortId => Source.FromPortId;
        public string ToPortId => Source.ToPortId;
        public string Condition => Source.Condition;
        public IReadOnlyList<Sv5SpecialWorldPoint> Cells => Source.Cells;

        public int CompareTo(Sv5CoreRouteReservation other) => other == null ? 1 :
            string.Compare(RouteId, other.RouteId, StringComparison.Ordinal);
    }

    public sealed class Sv5CoreAccessBinding : IComparable<Sv5CoreAccessBinding>
    {
        private readonly ReadOnlyCollection<string> routeIds;

        internal Sv5CoreAccessBinding(Sv5SpecialAccess source, IEnumerable<string> values, string status)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            routeIds = new ReadOnlyCollection<string>((values ?? Array.Empty<string>()).Where(value =>
                !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).OrderBy(value => value,
                StringComparer.Ordinal).ToArray());
            Status = Sv5CoreReservationPlan.Require(status, nameof(status));
        }

        public Sv5SpecialAccess Source { get; }
        public IReadOnlyList<string> RouteIds => routeIds;
        public string Status { get; }
        public bool IsGraphRouteBound => routeIds.Count != 0;

        public int CompareTo(Sv5CoreAccessBinding other) => other == null ? 1 :
            string.Compare(Source.Id, other.Source.Id, StringComparison.Ordinal);
    }

    /// <summary>
    /// SV5_04's read-only reservation adapter. It retains the exact SV5
    /// core object and the exact SV5 route object; it does not place sites,
    /// create RNG streams, or regenerate terrain.
    /// </summary>
    public sealed class Sv5CoreReservationPlan
    {
        private readonly ReadOnlyCollection<Sv5CoreRouteReservation> routes;
        private readonly ReadOnlyCollection<Sv5CoreRouteCellReservation> routeCells;
        private readonly ReadOnlyCollection<Sv5CoreAccessBinding> accessBindings;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<Sv5CoreRouteCellReservation>> routeCellsByCoordinate;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<Sv5SpecialStateGeometryCell>> stateByCoordinate;

        internal Sv5CoreReservationPlan(Sv5SpecialReservationPlan source, Sv5ClusterAssemblyPlan routeSource,
            IEnumerable<Sv5CoreRouteReservation> routeValues, IEnumerable<Sv5CoreRouteCellReservation> routeCellValues,
            IEnumerable<Sv5CoreAccessBinding> accessValues)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            RouteSource = routeSource ?? throw new ArgumentNullException(nameof(routeSource));
            routes = Freeze(routeValues);
            routeCells = Freeze(routeCellValues);
            accessBindings = Freeze(accessValues);
            if (Source.Sites.Count != 8 || Source.Cells.Count != 2432)
                throw new ArgumentException("SV5_04 requires the representative SV5 core source.", nameof(source));
            if (routes.Count != RouteSource.Graph.Edges.Count || routes.Any(value => !value.Source.Evidence.IsValid))
                throw new ArgumentException("Every current graph edge needs valid SV5 route evidence.", nameof(routeValues));
            if (accessBindings.Count != Source.Accesses.Count)
                throw new ArgumentException("Every physical SV5 access must be recorded.", nameof(accessValues));
            routeCellsByCoordinate = BuildRouteCellIndex(routeCells);
            stateByCoordinate = BuildStateIndex(Source.StateGeometry);
            Digest = Sv5WorldDefinition.Hash(string.Join("\n", CanonicalLines()));
        }

        public Sv5SpecialReservationPlan Source { get; }
        public Sv5ClusterAssemblyPlan RouteSource { get; }
        public IReadOnlyList<Sv5SpecialSite> Sites => Source.Sites;
        public IReadOnlyList<Sv5SpecialWorldCell> CoreCells => Source.Cells;
        public IReadOnlyList<Sv5SpecialSlot> Slots => Source.Slots;
        public IReadOnlyList<Sv5SpecialGraphBinding> GraphBindings => Source.GraphBindings;
        public IReadOnlyList<Sv5SpecialStateGeometryCell> StateGeometry => Source.StateGeometry;
        public IReadOnlyList<Sv5CoreRouteReservation> Routes => routes;
        public IReadOnlyList<Sv5CoreRouteCellReservation> RouteCells => routeCells;
        public IReadOnlyList<Sv5CoreAccessBinding> AccessBindings => accessBindings;
        public string Digest { get; }

        public bool TryGetRouteCells(Sv5SpecialWorldPoint world, out IReadOnlyList<Sv5CoreRouteCellReservation> values) =>
            routeCellsByCoordinate.TryGetValue(Key(world), out values);

        /// <summary>Public consumer gate for future terrain writers. A caller must
        /// submit its intended bases before writing; no mutation occurs here.</summary>
        public Sv5CoreReservationDecision EvaluateTerrainCandidates(IEnumerable<Sv5CoreTerrainCandidate> candidates)
        {
            var diagnostics = new List<Sv5CoreReservationDiagnostic>();
            Sv5CoreTerrainCandidate[] ordered = (candidates ?? Array.Empty<Sv5CoreTerrainCandidate>()).Where(value =>
                value != null).OrderBy(value => value.World).ThenBy(value => value.ConsumerId, StringComparer.Ordinal).ToArray();
            foreach (IGrouping<Sv5SpecialWorldPoint, Sv5CoreTerrainCandidate> group in ordered.GroupBy(value => value.World).OrderBy(value => value.Key))
            {
                Sv5CoreTerrainCandidate[] values = group.ToArray();
                if (!Sv5SpecialReservationPlanner.IsInWorld(group.Key.X, group.Key.Y))
                {
                    diagnostics.Add(new Sv5CoreReservationDiagnostic(Sv5CoreReservationDiagnosticCode.OutOfWorld,
                        group.Key, values[0].ConsumerId, "Candidate coordinate is outside the 624x416 world."));
                    continue;
                }
                if (values.Select(value => value.BaseCell).Distinct().Count() != 1)
                {
                    diagnostics.Add(new Sv5CoreReservationDiagnostic(Sv5CoreReservationDiagnosticCode.DuplicateCandidate,
                        group.Key, values[0].ConsumerId, "Candidates at one coordinate require incompatible base cells."));
                    continue;
                }
                Sv5PatternBaseCell proposed = values[0].BaseCell;
                if (stateByCoordinate.TryGetValue(Key(group.Key), out IReadOnlyList<Sv5SpecialStateGeometryCell> states))
                {
                    diagnostics.Add(new Sv5CoreReservationDiagnostic(Sv5CoreReservationDiagnosticCode.StateGeometry,
                        group.Key, states[0].SiteId, "Conditional state geometry is owned by its existing state authority."));
                    continue;
                }
                Sv5SpecialTerrainReservationDecision coreDecision = Source.EvaluateTerrainCells(new[] { group.Key });
                if (!coreDecision.IsAllowed)
                {
                    Sv5SpecialWorldCell cell = coreDecision.ProtectedCells[0];
                    diagnostics.Add(new Sv5CoreReservationDiagnostic(Sv5CoreReservationDiagnosticCode.ProtectedCoreCell,
                        group.Key, cell.SiteId, "SV5 " + cell.Protection + " cannot be changed by general terrain."));
                    continue;
                }
                if (!routeCellsByCoordinate.TryGetValue(Key(group.Key), out IReadOnlyList<Sv5CoreRouteCellReservation> reservations)) continue;
                Sv5CoreRouteCellReservation support = reservations.FirstOrDefault(value => value.Kind == Sv5CoreRouteReservationKind.Support);
                if (support != null && proposed != support.RequiredBaseCell)
                {
                    diagnostics.Add(new Sv5CoreReservationDiagnostic(Sv5CoreReservationDiagnosticCode.RouteSupportRemoved,
                        group.Key, support.RouteId, "Route support must retain " + support.RequiredBaseCell + "."));
                    continue;
                }
                Sv5CoreRouteCellReservation passage = reservations.FirstOrDefault(value => value.Kind == Sv5CoreRouteReservationKind.Passage);
                if (passage != null && proposed != Sv5PatternBaseCell.Air)
                {
                    diagnostics.Add(new Sv5CoreReservationDiagnostic(Sv5CoreReservationDiagnosticCode.RoutePassageBlocked,
                        group.Key, passage.RouteId, "Route body must retain AIR."));
                    continue;
                }
                Sv5CoreRouteCellReservation clearance = reservations.FirstOrDefault(value => value.Kind == Sv5CoreRouteReservationKind.Clearance);
                if (clearance != null && proposed != Sv5PatternBaseCell.Air)
                    diagnostics.Add(new Sv5CoreReservationDiagnostic(Sv5CoreReservationDiagnosticCode.RouteClearanceBlocked,
                        group.Key, clearance.RouteId, "Route headroom must retain AIR."));
            }
            return new Sv5CoreReservationDecision(diagnostics);
        }

        private IEnumerable<string> CanonicalLines()
        {
            yield return "SV5_CORE_RESERVATION_V1";
            yield return Source.Digest;
            yield return RouteSource.Digest;
            foreach (Sv5CoreRouteReservation route in routes) yield return "route|" + route.RouteId + "|" +
                route.FromPortId + "|" + route.ToPortId + "|" + route.Condition + "|" + route.Cells.Count;
            foreach (Sv5CoreRouteCellReservation cell in routeCells) yield return "routeCell|" + cell.RouteId + "|" +
                cell.Ordinal + "|" + cell.Kind + "|" + cell.World + "|" + cell.RequiredBaseCell;
            foreach (Sv5CoreAccessBinding access in accessBindings) yield return "access|" + access.Source.Id + "|" +
                access.Status + "|" + string.Join(";", access.RouteIds);
        }

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> values) where T : IComparable<T> =>
            new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).Where(value => value != null).OrderBy(value => value).ToArray());

        private static IReadOnlyDictionary<string, IReadOnlyList<Sv5CoreRouteCellReservation>> BuildRouteCellIndex(
            IEnumerable<Sv5CoreRouteCellReservation> values)
        {
            var map = new Dictionary<string, IReadOnlyList<Sv5CoreRouteCellReservation>>(StringComparer.Ordinal);
            foreach (IGrouping<string, Sv5CoreRouteCellReservation> group in (values ??
                Array.Empty<Sv5CoreRouteCellReservation>()).GroupBy(value => Key(value.World), StringComparer.Ordinal))
                map.Add(group.Key, new ReadOnlyCollection<Sv5CoreRouteCellReservation>(group.OrderBy(value => value).ToArray()));
            return new ReadOnlyDictionary<string, IReadOnlyList<Sv5CoreRouteCellReservation>>(map);
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<Sv5SpecialStateGeometryCell>> BuildStateIndex(
            IEnumerable<Sv5SpecialStateGeometryCell> values)
        {
            var map = new Dictionary<string, IReadOnlyList<Sv5SpecialStateGeometryCell>>(StringComparer.Ordinal);
            foreach (IGrouping<string, Sv5SpecialStateGeometryCell> group in (values ??
                Array.Empty<Sv5SpecialStateGeometryCell>()).GroupBy(value => Key(value.World), StringComparer.Ordinal))
                map.Add(group.Key, new ReadOnlyCollection<Sv5SpecialStateGeometryCell>(group.OrderBy(value => value).ToArray()));
            return new ReadOnlyDictionary<string, IReadOnlyList<Sv5SpecialStateGeometryCell>>(map);
        }

        internal static string Key(Sv5SpecialWorldPoint point) => point.X.ToString(CultureInfo.InvariantCulture) + "," +
            point.Y.ToString(CultureInfo.InvariantCulture);
        internal static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A stable value is required.", name);
            return value.Trim();
        }
    }

    public static class Sv5CoreReservationPlanner
    {
        public static Sv5CoreReservationPlan Plan(Sv5ClusterAssemblyPlan routeSource)
        {
            if (routeSource == null) throw new ArgumentNullException(nameof(routeSource));
            return Plan(routeSource.SpecialPlan, routeSource);
        }

        public static Sv5CoreReservationPlan Plan(Sv5SpecialReservationPlan coreSource,
            Sv5ClusterAssemblyPlan routeSource)
        {
            if (coreSource == null) throw new ArgumentNullException(nameof(coreSource));
            if (routeSource == null) throw new ArgumentNullException(nameof(routeSource));
            if (!routeSource.Success) throw new ArgumentException("A passing SV5 source is required.", nameof(routeSource));
            if (!ReferenceEquals(coreSource, routeSource.SpecialPlan))
                throw new ArgumentException("Core and route sources must be the same SV5 plan object.", nameof(coreSource));
            if (!ReferenceEquals(coreSource.BiomePlan.Definition, routeSource.Definition) ||
                !string.Equals(coreSource.BiomePlan.Digest, routeSource.BiomePlan.Digest, StringComparison.Ordinal))
                throw new ArgumentException("Core and route sources must share definition and biome provenance.", nameof(routeSource));
            if (routeSource.Routes.Count != routeSource.Graph.Edges.Count)
                throw new ArgumentException("Every SV5 edge must have one SV5 route.", nameof(routeSource));

            var routes = routeSource.Routes.Select(value => new Sv5CoreRouteReservation(value)).ToArray();
            ValidateRoutes(coreSource, routes);
            Sv5CoreRouteCellReservation[] routeCells = BuildRouteCells(routeSource, routes).ToArray();
            Sv5CoreAccessBinding[] accesses = BuildAccessBindings(coreSource, routes).ToArray();
            return new Sv5CoreReservationPlan(coreSource, routeSource, routes, routeCells, accesses);
        }

        private static void ValidateRoutes(Sv5SpecialReservationPlan source, IEnumerable<Sv5CoreRouteReservation> routes)
        {
            var accessIds = new HashSet<string>(source.Accesses.Select(value => value.Id), StringComparer.Ordinal);
            foreach (Sv5CoreRouteReservation route in routes ?? Array.Empty<Sv5CoreRouteReservation>())
            {
                if (!accessIds.Contains(route.FromPortId) || !accessIds.Contains(route.ToPortId) || !route.Source.Evidence.IsValid)
                    throw new ArgumentException("Every route must bind valid source ports and evidence.", nameof(routes));
                if (route.Cells.Count < 2) throw new ArgumentException("A route needs at least two cells.", nameof(routes));
                for (int index = 1; index < route.Cells.Count; index++)
                {
                    Sv5SpecialWorldPoint before = route.Cells[index - 1];
                    Sv5SpecialWorldPoint current = route.Cells[index];
                    if (Math.Abs(before.X - current.X) + Math.Abs(before.Y - current.Y) != 1)
                        throw new ArgumentException("A route must be cardinally contiguous.", nameof(routes));
                }
            }
        }

        private static IEnumerable<Sv5CoreRouteCellReservation> BuildRouteCells(Sv5ClusterAssemblyPlan source,
            IEnumerable<Sv5CoreRouteReservation> routes)
        {
            foreach (Sv5CoreRouteReservation route in routes ?? Array.Empty<Sv5CoreRouteReservation>())
            for (int ordinal = 0; ordinal < route.Cells.Count; ordinal++)
            {
                Sv5SpecialWorldPoint point = route.Cells[ordinal];
                Sv5TerrainCell passage = source.GetCell(point.X, point.Y);
                if (passage.BaseCell != Sv5PatternBaseCell.Air)
                    throw new ArgumentException("SV5 route passage must be AIR.", nameof(source));
                yield return new Sv5CoreRouteCellReservation(route.RouteId, ordinal,
                    Sv5CoreRouteReservationKind.Passage, point, Sv5PatternBaseCell.Air, passage.SourceId);

                Sv5SpecialWorldPoint clearancePoint = new Sv5SpecialWorldPoint(point.X, point.Y + 1);
                if (Sv5SpecialReservationPlanner.IsInWorld(clearancePoint.X, clearancePoint.Y))
                {
                    Sv5TerrainCell clearance = source.GetCell(clearancePoint.X, clearancePoint.Y);
                    if (clearance.BaseCell != Sv5PatternBaseCell.Air)
                        throw new ArgumentException("SV5 route clearance must be AIR.", nameof(source));
                    yield return new Sv5CoreRouteCellReservation(route.RouteId, ordinal,
                        Sv5CoreRouteReservationKind.Clearance, clearancePoint, Sv5PatternBaseCell.Air, clearance.SourceId);
                }

                Sv5SpecialWorldPoint supportPoint = new Sv5SpecialWorldPoint(point.X, point.Y - 1);
                if (!Sv5SpecialReservationPlanner.IsInWorld(supportPoint.X, supportPoint.Y)) continue;
                Sv5TerrainCell support = source.GetCell(supportPoint.X, supportPoint.Y);
                if (support.SourceKind == Sv5TerrainSourceKind.Route && string.Equals(support.SourceId,
                    route.RouteId + "_SUPPORT", StringComparison.Ordinal))
                {
                    if (support.BaseCell != Sv5PatternBaseCell.Solid && support.BaseCell != Sv5PatternBaseCell.OneWayPlatform)
                        throw new ArgumentException("SV5 route support must be SOLID or ONE_WAY.", nameof(source));
                    yield return new Sv5CoreRouteCellReservation(route.RouteId, ordinal,
                        Sv5CoreRouteReservationKind.Support, supportPoint, support.BaseCell, support.SourceId);
                }
            }
        }

        private static IEnumerable<Sv5CoreAccessBinding> BuildAccessBindings(Sv5SpecialReservationPlan source,
            IEnumerable<Sv5CoreRouteReservation> routes)
        {
            Sv5CoreRouteReservation[] values = (routes ?? Array.Empty<Sv5CoreRouteReservation>()).ToArray();
            var graphNodeIds = new HashSet<string>(source.GraphBindings.Select(value => value.NodeId), StringComparer.Ordinal);
            foreach (Sv5SpecialAccess access in source.Accesses)
            {
                string[] ids = values.Where(value => string.Equals(value.FromPortId, access.Id, StringComparison.Ordinal) ||
                    string.Equals(value.ToPortId, access.Id, StringComparison.Ordinal)).Select(value => value.RouteId).ToArray();
                bool hasGraphNode = (access.SourceNodeId ?? string.Empty).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .Any(graphNodeIds.Contains);
                string status = ids.Length != 0 ? "GRAPH_ROUTE_BOUND" : hasGraphNode
                    ? "PRESERVED_NOT_SELECTED_BY_CURRENT_ROUTE_PLAN" : "PRESERVED_NO_GRAPH_RESERVATION";
                yield return new Sv5CoreAccessBinding(access, ids, status);
            }
        }
    }

    public static class Sv5CoreReservationExport
    {
        public static string CoreSitesCsv(Sv5CoreReservationPlan plan) => Lines(
            "site_id,role,stable_id,origin_x,origin_y,width_tiles,height_tiles,template_id,occupied_patch_ids,source_special_digest",
            Require(plan).Sites.Select(value => Row(value.Id, value.Role, value.StableId.Value, value.Origin.X, value.Origin.Y,
                value.WidthTiles, value.HeightTiles, value.TemplateId, string.Join("|", value.OccupiedPatchIds), plan.Source.Digest)));

        public static string CoreCellsCsv(Sv5CoreReservationPlan plan) => Lines(
            "site_id,local_x,local_y,world_x,world_y,patch_id,base,protection,owner,source_special_digest",
            Require(plan).CoreCells.Select(value => Row(value.SiteId, value.LocalX, value.LocalY, value.World.X, value.World.Y,
                value.PatchId, Token(value.BaseCell), value.Protection, value.SiteId, plan.Source.Digest)));

        public static string AccessBindingsCsv(Sv5CoreReservationPlan plan) => Lines(
            "access_id,site_id,side,flow,required,condition,source_node_id,open_cells,route_ids,status",
            Require(plan).AccessBindings.Select(value => Row(value.Source.Id, value.Source.SiteId, value.Source.Side,
                value.Source.Flow, value.Source.Required, value.Source.Condition, value.Source.SourceNodeId,
                string.Join("|", value.Source.OpenCells), string.Join("|", value.RouteIds), value.Status)));

        public static string RouteCellsCsv(Sv5CoreReservationPlan plan) => Lines(
            "route_id,ordinal,reservation_kind,world_x,world_y,required_base,from_port_id,to_port_id,condition,source_id",
            Require(plan).RouteCells.Select(value =>
            {
                Sv5CoreRouteReservation route = plan.Routes.Single(routeValue => routeValue.RouteId == value.RouteId);
                return Row(value.RouteId, value.Ordinal, value.Kind, value.World.X, value.World.Y, Token(value.RequiredBaseCell),
                    route.FromPortId, route.ToPortId, route.Condition, value.SourceId);
            }));

        public static string StateGeometryCsv(Sv5CoreReservationPlan plan) => Lines(
            "site_id,state,world_x,world_y,base,reason,compatibility",
            Require(plan).StateGeometry.Select(value => Row(value.SiteId, value.State, value.World.X, value.World.Y,
                Token(value.BaseCell), value.Reason, "GENERAL_TERRAIN_REJECTED")));

        public static string ManifestJson(Sv5CoreReservationPlan plan)
        {
            Require(plan);
            int bound = plan.AccessBindings.Count(value => value.IsGraphRouteBound);
            return "{\n" +
                "  \"format\": \"SV5_04_CORE_RESERVATION_V1\",\n" +
                "  \"definition_digest\": \"" + plan.RouteSource.Definition.Digest + "\",\n" +
                "  \"sv5_biome_digest\": \"" + plan.Source.BiomePlan.Digest + "\",\n" +
                "  \"sv5_special_digest\": \"" + plan.Source.Digest + "\",\n" +
                "  \"sv5_route_source_digest\": \"" + plan.RouteSource.Digest + "\",\n" +
                "  \"core_reservation_digest\": \"" + plan.Digest + "\",\n" +
                "  \"physical_site_count\": " + plan.Sites.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"source_core_cell_count\": " + plan.CoreCells.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"slot_count\": " + plan.Slots.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"physical_port_cell_count\": " + plan.Source.Accesses.Sum(value => value.OpenCells.Count).ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"state_geometry_count\": " + plan.StateGeometry.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"graph_route_count\": " + plan.Routes.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"graph_route_bound_access_count\": " + bound.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"preserved_unrouted_access_count\": " + (plan.AccessBindings.Count - bound).ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"route_reservation_row_count\": " + plan.RouteCells.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"consumer\": \"Sv5CoreReservationPlan.EvaluateTerrainCandidates\",\n" +
                "  \"rng\": \"NONE_REUSED_SV515_SV516_OBJECTS\",\n" +
                "  \"full_world_bake\": \"NOT_RUN\"\n" +
                "}\n";
        }

        private static Sv5CoreReservationPlan Require(Sv5CoreReservationPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            return plan;
        }

        private static string Lines(string header, IEnumerable<string> rows) => header + "\n" +
            string.Join("\n", rows ?? Array.Empty<string>()) + "\n";
        private static string Row(params object[] values) => string.Join(",", (values ?? Array.Empty<object>()).Select(Csv));
        private static string Csv(object value)
        {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
        private static string Token(Sv5PatternBaseCell value) => value == Sv5PatternBaseCell.Air ? "A" :
            value == Sv5PatternBaseCell.Solid ? "S" : value == Sv5PatternBaseCell.OneWayPlatform ? "O" :
            throw new ArgumentOutOfRangeException(nameof(value));
    }
}
