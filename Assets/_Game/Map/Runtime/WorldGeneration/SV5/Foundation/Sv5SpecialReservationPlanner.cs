using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Biomes;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    /// <summary>
    /// SV5's eight physical sites.  Seal and Boss deliberately share one
    /// site while retaining separate graph bindings and local points.
    /// </summary>
    public enum Sv5SpecialPhysicalRole
    {
        Start = 1,
        MooncoreOre = 2,
        CondensedCoefficientSap = 3,
        DeepStarYeast = 4,
        Village = 5,
        Forge = 6,
        SealBoss = 7,
        Exit = 8,
    }

    public enum Sv5SpecialSlotKind
    {
        Spawn = 1,
        Resource = 2,
        Npc = 3,
        Shop = 4,
        Forge = 5,
        Seal = 6,
        Boss = 7,
        Exit = 8,
    }

    public enum Sv5SpecialProtectionKind
    {
        FixedSolid = 1,
        ProtectedAir = 2,
    }

    public enum Sv5SpecialAccessFlow { In = 1, Out = 2, Both = 3 }
    public enum Sv5SpecialTerrainReservationDecisionKind { Allowed = 1, RejectedProtectedCell = 2 }

    public readonly struct Sv5SpecialWorldPoint : IEquatable<Sv5SpecialWorldPoint>, IComparable<Sv5SpecialWorldPoint>
    {
        public Sv5SpecialWorldPoint(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
        public int CompareTo(Sv5SpecialWorldPoint other)
        {
            int value = Y.CompareTo(other.Y);
            return value != 0 ? value : X.CompareTo(other.X);
        }
        public bool Equals(Sv5SpecialWorldPoint other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Sv5SpecialWorldPoint other && Equals(other);
        public override int GetHashCode() { unchecked { return (X * 397) ^ Y; } }
        public override string ToString() => X.ToString(CultureInfo.InvariantCulture) + "," +
            Y.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class Sv5SpecialPlacementAttempt : IComparable<Sv5SpecialPlacementAttempt>
    {
        internal Sv5SpecialPlacementAttempt(int ordinal, string candidateId, string outcome)
        {
            Ordinal = ordinal;
            CandidateId = Sv5SpecialReservationIdentity.Require(candidateId, nameof(candidateId));
            Outcome = Sv5SpecialReservationIdentity.Require(outcome, nameof(outcome));
        }

        public int Ordinal { get; }
        public string CandidateId { get; }
        public string Outcome { get; }
        public int CompareTo(Sv5SpecialPlacementAttempt other) => other == null ? 1 : Ordinal.CompareTo(other.Ordinal);
    }

    public sealed class Sv5SpecialWorldCell : IComparable<Sv5SpecialWorldCell>
    {
        internal Sv5SpecialWorldCell(
            string siteId,
            int localX,
            int localY,
            int worldX,
            int worldY,
            string patchId,
            Sv5PatternBaseCell baseCell)
        {
            SiteId = Sv5SpecialReservationIdentity.Require(siteId, nameof(siteId));
            if (!Sv5SpecialReservationPlanner.IsInWorld(worldX, worldY))
                throw new ArgumentOutOfRangeException(nameof(worldX));
            if (!Enum.IsDefined(typeof(Sv5PatternBaseCell), baseCell))
                throw new ArgumentOutOfRangeException(nameof(baseCell));
            LocalX = localX;
            LocalY = localY;
            World = new Sv5SpecialWorldPoint(worldX, worldY);
            PatchId = Sv5SpecialReservationIdentity.Require(patchId, nameof(patchId));
            BaseCell = baseCell;
            Protection = baseCell == Sv5PatternBaseCell.Air
                ? Sv5SpecialProtectionKind.ProtectedAir
                : Sv5SpecialProtectionKind.FixedSolid;
        }

        public string SiteId { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public Sv5SpecialWorldPoint World { get; }
        public string PatchId { get; }
        public Sv5PatternBaseCell BaseCell { get; }
        public Sv5SpecialProtectionKind Protection { get; }
        public int CompareTo(Sv5SpecialWorldCell other)
        {
            if (other == null) return 1;
            int value = string.Compare(SiteId, other.SiteId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = World.CompareTo(other.World);
            return value != 0 ? value : LocalX.CompareTo(other.LocalX);
        }
    }

    public sealed class Sv5SpecialSlot : IComparable<Sv5SpecialSlot>
    {
        internal Sv5SpecialSlot(
            string id,
            string siteId,
            Sv5SpecialSlotKind kind,
            Sv5SpecialWorldPoint local,
            Sv5SpecialWorldPoint world,
            Sv5WorldStableId stableId)
        {
            Id = Sv5SpecialReservationIdentity.Require(id, nameof(id));
            SiteId = Sv5SpecialReservationIdentity.Require(siteId, nameof(siteId));
            Kind = kind;
            Local = local;
            World = world;
            StableId = stableId ?? throw new ArgumentNullException(nameof(stableId));
        }

        public string Id { get; }
        public string SiteId { get; }
        public Sv5SpecialSlotKind Kind { get; }
        public Sv5SpecialWorldPoint Local { get; }
        public Sv5SpecialWorldPoint World { get; }
        public Sv5WorldStableId StableId { get; }
        public int OccupancyWidthTiles => 1;
        public int OccupancyHeightTiles => 1;
        public int CompareTo(Sv5SpecialSlot other) => other == null ? 1 :
            string.Compare(Id, other.Id, StringComparison.Ordinal);
    }

    public sealed class Sv5SpecialAccess : IComparable<Sv5SpecialAccess>
    {
        private readonly ReadOnlyCollection<Sv5SpecialWorldPoint> openCells;

        internal Sv5SpecialAccess(
            string id,
            string siteId,
            Sv5WorldGraphDirection side,
            Sv5SpecialAccessFlow flow,
            bool required,
            string condition,
            string sourceNodeId,
            IEnumerable<Sv5SpecialWorldPoint> cells)
        {
            Id = Sv5SpecialReservationIdentity.Require(id, nameof(id));
            SiteId = Sv5SpecialReservationIdentity.Require(siteId, nameof(siteId));
            Side = side;
            Flow = flow;
            Required = required;
            Condition = Sv5SpecialReservationIdentity.Require(condition, nameof(condition));
            SourceNodeId = sourceNodeId ?? string.Empty;
            var copy = (cells ?? Array.Empty<Sv5SpecialWorldPoint>()).Distinct().OrderBy(value => value).ToArray();
            if (copy.Length == 0) throw new ArgumentException("An access port needs open cells.", nameof(cells));
            openCells = new ReadOnlyCollection<Sv5SpecialWorldPoint>(copy);
        }

        public string Id { get; }
        public string SiteId { get; }
        public Sv5WorldGraphDirection Side { get; }
        public Sv5SpecialAccessFlow Flow { get; }
        public bool Required { get; }
        public string Condition { get; }
        public string SourceNodeId { get; }
        public IReadOnlyList<Sv5SpecialWorldPoint> OpenCells => openCells;
        public int CompareTo(Sv5SpecialAccess other) => other == null ? 1 :
            string.Compare(Id, other.Id, StringComparison.Ordinal);
    }

    public sealed class Sv5SpecialGraphBinding : IComparable<Sv5SpecialGraphBinding>
    {
        internal Sv5SpecialGraphBinding(
            Sv5WorldGraphReservation reservation,
            string siteId,
            Sv5SpecialWorldPoint localPoint,
            Sv5SpecialWorldPoint worldPoint)
        {
            Reservation = reservation ?? throw new ArgumentNullException(nameof(reservation));
            SiteId = Sv5SpecialReservationIdentity.Require(siteId, nameof(siteId));
            LocalPoint = localPoint;
            WorldPoint = worldPoint;
        }

        public Sv5WorldGraphReservation Reservation { get; }
        public string ReservationId => Reservation.ReservationId;
        public string NodeId => Reservation.Node.NodeId;
        public Sv5WorldGraphRole Role => Reservation.Node.Role;
        public string SiteId { get; }
        public Sv5SpecialWorldPoint LocalPoint { get; }
        public Sv5SpecialWorldPoint WorldPoint { get; }
        public int CompareTo(Sv5SpecialGraphBinding other) => other == null ? 1 :
            string.Compare(ReservationId, other.ReservationId, StringComparison.Ordinal);
    }

    public sealed class Sv5SpecialStateGeometryCell : IComparable<Sv5SpecialStateGeometryCell>
    {
        internal Sv5SpecialStateGeometryCell(
            string siteId,
            string state,
            Sv5SpecialWorldPoint world,
            Sv5PatternBaseCell baseCell,
            string reason)
        {
            SiteId = Sv5SpecialReservationIdentity.Require(siteId, nameof(siteId));
            State = Sv5SpecialReservationIdentity.Require(state, nameof(state));
            World = world;
            BaseCell = baseCell;
            Reason = Sv5SpecialReservationIdentity.Require(reason, nameof(reason));
        }

        public string SiteId { get; }
        public string State { get; }
        public Sv5SpecialWorldPoint World { get; }
        public Sv5PatternBaseCell BaseCell { get; }
        public string Reason { get; }
        public int CompareTo(Sv5SpecialStateGeometryCell other)
        {
            if (other == null) return 1;
            int value = string.Compare(SiteId, other.SiteId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(State, other.State, StringComparison.Ordinal);
            return value != 0 ? value : World.CompareTo(other.World);
        }
    }

    public sealed class Sv5SpecialTerrainReservationDecision
    {
        internal Sv5SpecialTerrainReservationDecision(
            Sv5SpecialTerrainReservationDecisionKind kind,
            IEnumerable<Sv5SpecialWorldCell> protectedCells)
        {
            Kind = kind;
            ProtectedCells = new ReadOnlyCollection<Sv5SpecialWorldCell>((protectedCells ??
                Array.Empty<Sv5SpecialWorldCell>()).Distinct().OrderBy(value => value).ToArray());
        }

        public Sv5SpecialTerrainReservationDecisionKind Kind { get; }
        public IReadOnlyList<Sv5SpecialWorldCell> ProtectedCells { get; }
        public bool IsAllowed => Kind == Sv5SpecialTerrainReservationDecisionKind.Allowed;
    }

    public sealed class Sv5SpecialSite : IComparable<Sv5SpecialSite>
    {
        private readonly ReadOnlyCollection<string> eligiblePatchIds;
        private readonly ReadOnlyCollection<string> occupiedPatchIds;
        private readonly ReadOnlyCollection<Sv5SpecialPlacementAttempt> attempts;

        internal Sv5SpecialSite(
            string id,
            Sv5SpecialPhysicalRole role,
            string templateId,
            string templateVersion,
            Sv5SpecialWorldPoint origin,
            int widthTiles,
            int heightTiles,
            IEnumerable<string> eligible,
            IEnumerable<string> occupied,
            IEnumerable<Sv5SpecialPlacementAttempt> selectionAttempts,
            Sv5WorldStableId stableId)
        {
            Id = Sv5SpecialReservationIdentity.Require(id, nameof(id));
            Role = role;
            TemplateId = Sv5SpecialReservationIdentity.Require(templateId, nameof(templateId));
            TemplateVersion = Sv5SpecialReservationIdentity.Require(templateVersion, nameof(templateVersion));
            Origin = origin;
            if (widthTiles < 4 || heightTiles < 4) throw new ArgumentOutOfRangeException(nameof(widthTiles));
            WidthTiles = widthTiles;
            HeightTiles = heightTiles;
            eligiblePatchIds = Freeze(eligible);
            occupiedPatchIds = Freeze(occupied);
            attempts = new ReadOnlyCollection<Sv5SpecialPlacementAttempt>((selectionAttempts ??
                Array.Empty<Sv5SpecialPlacementAttempt>()).OrderBy(value => value).ToArray());
            if (eligiblePatchIds.Count == 0 || occupiedPatchIds.Count == 0 || attempts.Count == 0)
                throw new ArgumentException("A site needs candidates, occupied patches, and diagnostics.");
            StableId = stableId ?? throw new ArgumentNullException(nameof(stableId));
        }

        public string Id { get; }
        public Sv5SpecialPhysicalRole Role { get; }
        public string TemplateId { get; }
        public string TemplateVersion { get; }
        public string Transform => "R0";
        public Sv5SpecialWorldPoint Origin { get; }
        public int WidthTiles { get; }
        public int HeightTiles { get; }
        public Sv5WorldStableId StableId { get; }
        public IReadOnlyList<string> EligiblePatchIds => eligiblePatchIds;
        public IReadOnlyList<string> OccupiedPatchIds => occupiedPatchIds;
        public IReadOnlyList<Sv5SpecialPlacementAttempt> Attempts => attempts;
        public int CompareTo(Sv5SpecialSite other) => other == null ? 1 :
            string.Compare(Id, other.Id, StringComparison.Ordinal);

        private static ReadOnlyCollection<string> Freeze(IEnumerable<string> values) =>
            new ReadOnlyCollection<string>((values ?? Array.Empty<string>()).Where(value =>
                !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }

    public sealed class Sv5SpecialReservationPlan
    {
        private readonly ReadOnlyCollection<Sv5SpecialSite> sites;
        private readonly ReadOnlyCollection<Sv5SpecialWorldCell> cells;
        private readonly ReadOnlyCollection<Sv5SpecialSlot> slots;
        private readonly ReadOnlyCollection<Sv5SpecialAccess> accesses;
        private readonly ReadOnlyCollection<Sv5SpecialGraphBinding> graphBindings;
        private readonly ReadOnlyCollection<Sv5SpecialStateGeometryCell> stateGeometry;
        private readonly IReadOnlyDictionary<string, Sv5SpecialWorldCell> cellsByCoordinate;

        internal Sv5SpecialReservationPlan(
            Sv5WorldBiomePlan biomePlan,
            ulong specialReservationInitialState,
            IEnumerable<Sv5SpecialSite> values,
            IEnumerable<Sv5SpecialWorldCell> fixedCells,
            IEnumerable<Sv5SpecialSlot> siteSlots,
            IEnumerable<Sv5SpecialAccess> portBindings,
            IEnumerable<Sv5SpecialGraphBinding> bindings,
            IEnumerable<Sv5SpecialStateGeometryCell> geometry)
        {
            BiomePlan = biomePlan ?? throw new ArgumentNullException(nameof(biomePlan));
            SpecialReservationInitialState = specialReservationInitialState;
            sites = Read(values);
            cells = Read(fixedCells);
            slots = Read(siteSlots);
            accesses = Read(portBindings);
            graphBindings = Read(bindings);
            stateGeometry = Read(geometry);
            if (sites.Count != 8 || sites.Select(value => value.Role).Distinct().Count() != 8)
                throw new ArgumentException("SV5 requires exactly eight physical sites.", nameof(values));
            if (cells.Count == 0 || cells.Select(value => value.World).Distinct().Count() != cells.Count)
                throw new ArgumentException("Every fixed world cell needs one owner.", nameof(fixedCells));
            if (graphBindings.Count != 8 || graphBindings.Select(value => value.ReservationId).Distinct().Count() != 8 ||
                !graphBindings.Any(value => value.Role == Sv5WorldGraphRole.Seal) ||
                !graphBindings.Any(value => value.Role == Sv5WorldGraphRole.Boss))
                throw new ArgumentException("All eight SV5 reservations must be bound exactly once.", nameof(bindings));
            if (graphBindings.Single(value => value.Role == Sv5WorldGraphRole.Seal).SiteId !=
                graphBindings.Single(value => value.Role == Sv5WorldGraphRole.Boss).SiteId)
                throw new ArgumentException("Seal and Boss must share one physical SV5 site.", nameof(bindings));
            cellsByCoordinate = new ReadOnlyDictionary<string, Sv5SpecialWorldCell>(cells.ToDictionary(
                value => Key(value.World), value => value, StringComparer.Ordinal));
            ValidateReferences();
            Digest = Sv5SpecialReservationIdentity.Hash(CanonicalLines());
        }

        public Sv5WorldBiomePlan BiomePlan { get; }
        public ulong SpecialReservationInitialState { get; }
        public IReadOnlyList<Sv5SpecialSite> Sites => sites;
        public IReadOnlyList<Sv5SpecialWorldCell> Cells => cells;
        public IReadOnlyList<Sv5SpecialSlot> Slots => slots;
        public IReadOnlyList<Sv5SpecialAccess> Accesses => accesses;
        public IReadOnlyList<Sv5SpecialGraphBinding> GraphBindings => graphBindings;
        public IReadOnlyList<Sv5SpecialStateGeometryCell> StateGeometry => stateGeometry;
        public string Digest { get; }
        public bool Success => Sites.Count == 8 && Cells.Count != 0 && GraphBindings.Count == 8;

        /// <summary>Direct SV5/SV5 handoff: any fixed SOLID, ONE_WAY,
        /// protected AIR, port, or slot cell is unavailable to general terrain.</summary>
        public Sv5SpecialTerrainReservationDecision EvaluateTerrainCells(
            IEnumerable<Sv5SpecialWorldPoint> proposedCells)
        {
            var conflicts = (proposedCells ?? Array.Empty<Sv5SpecialWorldPoint>()).Distinct()
                .Where(value => cellsByCoordinate.ContainsKey(Key(value)))
                .Select(value => cellsByCoordinate[Key(value)]).OrderBy(value => value).ToArray();
            return new Sv5SpecialTerrainReservationDecision(conflicts.Length == 0
                ? Sv5SpecialTerrainReservationDecisionKind.Allowed
                : Sv5SpecialTerrainReservationDecisionKind.RejectedProtectedCell, conflicts);
        }

        public bool TryGetCell(Sv5SpecialWorldPoint world, out Sv5SpecialWorldCell cell) =>
            cellsByCoordinate.TryGetValue(Key(world), out cell);

        private void ValidateReferences()
        {
            var siteIds = new HashSet<string>(sites.Select(value => value.Id), StringComparer.Ordinal);
            if (cells.Any(value => !siteIds.Contains(value.SiteId)) || slots.Any(value => !siteIds.Contains(value.SiteId)) ||
                accesses.Any(value => !siteIds.Contains(value.SiteId)) || graphBindings.Any(value => !siteIds.Contains(value.SiteId)))
                throw new ArgumentException("SV5 publication references an unknown site.");
            if (slots.Select(value => value.Id).Distinct(StringComparer.Ordinal).Count() != slots.Count)
                throw new ArgumentException("SV5 slot IDs must be stable and unique.");
            foreach (Sv5SpecialSlot slot in slots)
            {
                if (!TryGetCell(slot.World, out Sv5SpecialWorldCell cell) || cell.BaseCell != Sv5PatternBaseCell.Air ||
                    !TryGetCell(new Sv5SpecialWorldPoint(slot.World.X, slot.World.Y - 1), out Sv5SpecialWorldCell support) ||
                    (support.BaseCell != Sv5PatternBaseCell.Solid && support.BaseCell != Sv5PatternBaseCell.OneWayPlatform) ||
                    !TryGetCell(new Sv5SpecialWorldPoint(slot.World.X, slot.World.Y + 1), out Sv5SpecialWorldCell headroom) ||
                    headroom.BaseCell != Sv5PatternBaseCell.Air)
                    throw new ArgumentException("Each SV5 slot must have AIR, support, and headroom.");
            }
            foreach (Sv5SpecialAccess access in accesses)
            foreach (Sv5SpecialWorldPoint point in access.OpenCells)
            {
                if (!TryGetCell(point, out Sv5SpecialWorldCell cell) || cell.BaseCell != Sv5PatternBaseCell.Air)
                    throw new ArgumentException("Every access cell must be protected AIR.");
            }
            foreach (Sv5SpecialWorldCell cell in cells)
            {
                Sv5WorldBiomeCell biome = BiomePlan.GetCell(cell.World.X / Sv5WorldBiomePlanner.MicroChunkWidthTiles,
                    cell.World.Y / Sv5WorldBiomePlanner.MicroChunkHeightTiles);
                if (!string.Equals(cell.PatchId, biome.PatchId, StringComparison.Ordinal))
                    throw new ArgumentException("Fixed cell patch ownership must be sourced from SV5.");
            }
        }

        private IEnumerable<string> CanonicalLines()
        {
            yield return "SV5_SPECIAL_RESERVATION_PLAN_V1";
            yield return BiomePlan.Digest;
            yield return SpecialReservationInitialState.ToString("x16", CultureInfo.InvariantCulture);
            foreach (Sv5SpecialSite site in sites) yield return "site|" + site.Id + "|" + site.Role + "|" +
                site.Origin + "|" + site.WidthTiles + "x" + site.HeightTiles + "|" + site.StableId.Value;
            foreach (Sv5SpecialWorldCell cell in cells) yield return "cell|" + cell.SiteId + "|" + cell.World +
                "|" + cell.BaseCell + "|" + cell.PatchId + "|" + cell.Protection;
            foreach (Sv5SpecialSlot slot in slots) yield return "slot|" + slot.Id + "|" + slot.SiteId + "|" +
                slot.Kind + "|" + slot.World + "|" + slot.StableId.Value;
            foreach (Sv5SpecialGraphBinding binding in graphBindings) yield return "graph|" + binding.ReservationId +
                "|" + binding.SiteId + "|" + binding.WorldPoint;
            foreach (Sv5SpecialAccess access in accesses) yield return "access|" + access.Id + "|" + access.SiteId +
                "|" + access.Side + "|" + access.Flow + "|" + string.Join(";", access.OpenCells);
            foreach (Sv5SpecialStateGeometryCell state in stateGeometry) yield return "state|" + state.SiteId + "|" +
                state.State + "|" + state.World + "|" + state.BaseCell;
        }

        private static ReadOnlyCollection<T> Read<T>(IEnumerable<T> values) where T : IComparable<T> =>
            new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).Where(value => value != null).OrderBy(value => value).ToArray());
        private static string Key(Sv5SpecialWorldPoint point) => point.X.ToString(CultureInfo.InvariantCulture) + "," +
            point.Y.ToString(CultureInfo.InvariantCulture);
    }

    public static class Sv5SpecialReservationPlanner
    {
        public const int WorldWidthTiles = Sv5WorldBiomePlanner.WorldWidthTiles;
        public const int WorldHeightTiles = Sv5WorldBiomePlanner.WorldHeightTiles;
        public const int MaxPlacementAttempts = 32;
        public const string TemplateVersion = "SV5_FIXED_SHELL_V1";

        public static Sv5SpecialReservationPlan Plan(Sv5WorldBiomePlan biomePlan, WorldGenerationRngStreams rngStreams)
        {
            if (biomePlan == null) throw new ArgumentNullException(nameof(biomePlan));
            if (!biomePlan.Success) throw new ArgumentException("SV5 input must pass before SV5 placement.", nameof(biomePlan));
            if (rngStreams == null) throw new ArgumentNullException(nameof(rngStreams));
            if (biomePlan.Definition.RngBindings[Sv5WorldRngStream.SpecialReservation].SourceStreamId !=
                WorldGenerationRngStreams.WorldSiteStreamId)
                throw new ArgumentException("SV5 requires SV5's SpecialReservation stream.", nameof(biomePlan));

            var rng = Sv5WorldDataGenerator.CreateStream(biomePlan.Definition.Request, rngStreams,
                Sv5WorldRngStream.SpecialReservation);
            var used = new HashSet<Sv5SpecialWorldPoint>();
            var sites = new List<Sv5SpecialSite>();
            var cells = new List<Sv5SpecialWorldCell>();
            var slots = new List<Sv5SpecialSlot>();
            var accesses = new List<Sv5SpecialAccess>();
            var graphBindings = new List<Sv5SpecialGraphBinding>();
            var stateGeometry = new List<Sv5SpecialStateGeometryCell>();

            AddRegularSite(biomePlan, rng, used, sites, cells, slots, accesses, graphBindings,
                Sv5SpecialPhysicalRole.Start, "START_SHELL", 16, 12, Sv5WorldGraphRole.Start,
                new[] { new SlotDraft("SPAWN", Sv5SpecialSlotKind.Spawn, 3, 2) }, "NONE", "START");
            AddRegularSite(biomePlan, rng, used, sites, cells, slots, accesses, graphBindings,
                Sv5SpecialPhysicalRole.MooncoreOre, "MOONCORE_RESOURCE_SHELL", 20, 14, Sv5WorldGraphRole.MooncoreOre,
                new[] { new SlotDraft("MOONCORE_ORE", Sv5SpecialSlotKind.Resource, 4, 2) }, "NONE", "MOONCORE");
            AddRegularSite(biomePlan, rng, used, sites, cells, slots, accesses, graphBindings,
                Sv5SpecialPhysicalRole.CondensedCoefficientSap, "SAP_RESOURCE_SHELL", 20, 14,
                Sv5WorldGraphRole.CondensedCoefficientSap,
                new[] { new SlotDraft("COEFFICIENT_SAP", Sv5SpecialSlotKind.Resource, 4, 2) }, "NONE", "SAP");
            AddRegularSite(biomePlan, rng, used, sites, cells, slots, accesses, graphBindings,
                Sv5SpecialPhysicalRole.DeepStarYeast, "YEAST_RESOURCE_SHELL", 20, 14, Sv5WorldGraphRole.DeepStarYeast,
                new[] { new SlotDraft("STAR_YEAST", Sv5SpecialSlotKind.Resource, 4, 2) }, "NONE", "YEAST");
            AddVillageSite(biomePlan, rng, used, sites, cells, slots, accesses);
            AddRegularSite(biomePlan, rng, used, sites, cells, slots, accesses, graphBindings,
                Sv5SpecialPhysicalRole.Forge, "FORGE_SHELL", 20, 16, Sv5WorldGraphRole.Forge,
                new[] { new SlotDraft("FORGE", Sv5SpecialSlotKind.Forge, 5, 2) }, "ALL_RESOURCES", "FORGE");
            AddSharedSealBossSite(biomePlan, rng, used, sites, cells, slots, accesses, graphBindings, stateGeometry);
            AddRegularSite(biomePlan, rng, used, sites, cells, slots, accesses, graphBindings,
                Sv5SpecialPhysicalRole.Exit, "EXIT_SHELL", 16, 12, Sv5WorldGraphRole.Exit,
                new[] { new SlotDraft("EXIT", Sv5SpecialSlotKind.Exit, 11, 2) }, "BOSS_COMPLETE", "EXIT");

            return new Sv5SpecialReservationPlan(biomePlan, rng.InitialState, sites, cells, slots, accesses,
                graphBindings, stateGeometry);
        }

        public static bool IsInWorld(int x, int y) => x >= 0 && x < WorldWidthTiles && y >= 0 && y < WorldHeightTiles;

        private static void AddRegularSite(
            Sv5WorldBiomePlan plan,
            DeterministicRngStream rng,
            ISet<Sv5SpecialWorldPoint> used,
            ICollection<Sv5SpecialSite> sites,
            ICollection<Sv5SpecialWorldCell> cells,
            ICollection<Sv5SpecialSlot> slots,
            ICollection<Sv5SpecialAccess> accesses,
            ICollection<Sv5SpecialGraphBinding> graphBindings,
            Sv5SpecialPhysicalRole physicalRole,
            string templateId,
            int width,
            int height,
            Sv5WorldGraphRole graphRole,
            IEnumerable<SlotDraft> slotDrafts,
            string requiredCondition,
            string token)
        {
            Sv5WorldBiomeReservationInput input = InputFor(plan, graphRole);
            Placement placement = ChooseWithinPatches(plan, rng, used, input.CandidatePatches, width, height, token);
            Sv5SpecialSite site = BuildSite(plan, physicalRole, templateId, width, height, placement,
                input.CandidatePatches.Select(value => value.PatchId));
            List<SlotDraft> drafts = (slotDrafts ?? Array.Empty<SlotDraft>()).ToList();
            List<PortDraft> ports = StandardPorts(width, height, graphRole == Sv5WorldGraphRole.Start ?
                Sv5SpecialAccessFlow.Both : Sv5SpecialAccessFlow.In, graphRole == Sv5WorldGraphRole.Exit ?
                (Sv5SpecialAccessFlow?)null : Sv5SpecialAccessFlow.Out, requiredCondition, input.Reservation.Node.NodeId);
            PublishSite(plan, site, drafts, ports, cells, slots, accesses, used);
            Sv5SpecialSlot graphSlot = slots.Single(value => value.SiteId == site.Id && IsGraphSlot(graphRole, value.Kind));
            graphBindings.Add(new Sv5SpecialGraphBinding(input.Reservation, site.Id, graphSlot.Local, graphSlot.World));
            sites.Add(site);
        }

        private static void AddVillageSite(
            Sv5WorldBiomePlan plan,
            DeterministicRngStream rng,
            ISet<Sv5SpecialWorldPoint> used,
            ICollection<Sv5SpecialSite> sites,
            ICollection<Sv5SpecialWorldCell> cells,
            ICollection<Sv5SpecialSlot> slots,
            ICollection<Sv5SpecialAccess> accesses)
        {
            // Village is intentionally a ninth logical concern but an eighth physical site:
            // it has its own stable identity and does not consume/replace a graph reservation.
            var eligible = plan.Patches.Where(value => value.Biome.CanonicalId == "MoonCrater").OrderBy(value => value).ToArray();
            Placement placement = ChooseWithinPatches(plan, rng, used, eligible, 24, 16, "VILLAGE");
            Sv5SpecialSite site = BuildSite(plan, Sv5SpecialPhysicalRole.Village, "VILLAGE_SHELL", 24, 16,
                placement, eligible.Select(value => value.PatchId));
            PublishSite(plan, site, new[]
            {
                new SlotDraft("NPC", Sv5SpecialSlotKind.Npc, 5, 2),
                new SlotDraft("SHOP", Sv5SpecialSlotKind.Shop, 9, 2),
            }, StandardPorts(24, 16, Sv5SpecialAccessFlow.In, Sv5SpecialAccessFlow.Out, "OPTIONAL_VILLAGE",
                "VILLAGE_OPTIONAL"),
            cells, slots, accesses, used);
            sites.Add(site);
        }

        private static void AddSharedSealBossSite(
            Sv5WorldBiomePlan plan,
            DeterministicRngStream rng,
            ISet<Sv5SpecialWorldPoint> used,
            ICollection<Sv5SpecialSite> sites,
            ICollection<Sv5SpecialWorldCell> cells,
            ICollection<Sv5SpecialSlot> slots,
            ICollection<Sv5SpecialAccess> accesses,
            ICollection<Sv5SpecialGraphBinding> graphBindings,
            ICollection<Sv5SpecialStateGeometryCell> stateGeometry)
        {
            Sv5WorldBiomeReservationInput seal = InputFor(plan, Sv5WorldGraphRole.Seal);
            Sv5WorldBiomeReservationInput boss = InputFor(plan, Sv5WorldGraphRole.Boss);
            SharedPlacement shared = ChooseSharedBoundary(plan, rng, used, seal, boss, 28, 18);
            var eligible = seal.CandidatePatches.Concat(boss.CandidatePatches).Select(value => value.PatchId);
            Sv5SpecialSite site = BuildSite(plan, Sv5SpecialPhysicalRole.SealBoss, "SEAL_BOSS_BOUNDARY_SHELL", 28, 18,
                shared.Placement, eligible);
            var drafts = new[]
            {
                new SlotDraft("SEAL", Sv5SpecialSlotKind.Seal, shared.SealLocal.X, shared.SealLocal.Y),
                new SlotDraft("BOSS", Sv5SpecialSlotKind.Boss, shared.BossLocal.X, shared.BossLocal.Y),
            };
            List<PortDraft> ports = StandardPorts(28, 18, Sv5SpecialAccessFlow.In, Sv5SpecialAccessFlow.Out,
                "ALL_RESOURCES_AND_FORGE", seal.Reservation.Node.NodeId + "|" + boss.Reservation.Node.NodeId);
            PublishSite(plan, site, drafts, ports, cells, slots, accesses, used);
            Sv5SpecialSlot sealSlot = slots.Single(value => value.SiteId == site.Id && value.Kind == Sv5SpecialSlotKind.Seal);
            Sv5SpecialSlot bossSlot = slots.Single(value => value.SiteId == site.Id && value.Kind == Sv5SpecialSlotKind.Boss);
            graphBindings.Add(new Sv5SpecialGraphBinding(seal.Reservation, site.Id, sealSlot.Local, sealSlot.World));
            graphBindings.Add(new Sv5SpecialGraphBinding(boss.Reservation, site.Id, bossSlot.Local, bossSlot.World));
            Sv5SpecialAccess gate = accesses.Single(value => value.SiteId == site.Id && value.Flow == Sv5SpecialAccessFlow.Out);
            foreach (Sv5SpecialWorldPoint point in gate.OpenCells)
            {
                stateGeometry.Add(new Sv5SpecialStateGeometryCell(site.Id, "SEALED", point,
                    Sv5PatternBaseCell.Solid, "Seal gate blocks boss/exit approach until the seal state opens."));
                stateGeometry.Add(new Sv5SpecialStateGeometryCell(site.Id, "OPEN", point,
                    Sv5PatternBaseCell.Air, "Open state restores the protected fixed-air exit port."));
            }
            sites.Add(site);
        }

        private static Sv5SpecialSite BuildSite(
            Sv5WorldBiomePlan plan,
            Sv5SpecialPhysicalRole role,
            string templateId,
            int width,
            int height,
            Placement placement,
            IEnumerable<string> eligible)
        {
            string id = "SV5_SITE_" + role.ToString().ToUpperInvariant();
            var stable = new Sv5WorldStableId(Sv5WorldStableIdKind.SpecialRegionTrigger,
                "SV5|" + plan.Definition.Digest + "|" + id + "|" + placement.Origin,
                "SITE");
            return new Sv5SpecialSite(id, role, templateId, TemplateVersion, placement.Origin, width, height,
                eligible, placement.OccupiedPatchIds, placement.Attempts, stable);
        }

        private static void PublishSite(
            Sv5WorldBiomePlan plan,
            Sv5SpecialSite site,
            IEnumerable<SlotDraft> slotDrafts,
            IEnumerable<PortDraft> portDrafts,
            ICollection<Sv5SpecialWorldCell> cells,
            ICollection<Sv5SpecialSlot> slots,
            ICollection<Sv5SpecialAccess> accesses,
            ISet<Sv5SpecialWorldPoint> used)
        {
            var drafts = (slotDrafts ?? Array.Empty<SlotDraft>()).ToArray();
            var ports = (portDrafts ?? Array.Empty<PortDraft>()).ToArray();
            Sv5PatternBaseCell[,] baseCells = BuildShell(site.WidthTiles, site.HeightTiles, drafts, ports);
            for (int y = 0; y < site.HeightTiles; y++)
            for (int x = 0; x < site.WidthTiles; x++)
            {
                int worldX = site.Origin.X + x;
                int worldY = site.Origin.Y + y;
                Sv5WorldBiomeCell owner = plan.GetCell(worldX / Sv5WorldBiomePlanner.MicroChunkWidthTiles,
                    worldY / Sv5WorldBiomePlanner.MicroChunkHeightTiles);
                cells.Add(new Sv5SpecialWorldCell(site.Id, x, y, worldX, worldY, owner.PatchId, baseCells[x, y]));
                used.Add(new Sv5SpecialWorldPoint(worldX, worldY));
            }
            foreach (SlotDraft draft in drafts)
            {
                Sv5SpecialWorldPoint local = new Sv5SpecialWorldPoint(draft.X, draft.Y);
                Sv5SpecialWorldPoint world = new Sv5SpecialWorldPoint(site.Origin.X + draft.X, site.Origin.Y + draft.Y);
                Sv5WorldStableId stable = new Sv5WorldStableId(StableKind(draft.Kind), site.StableId.Value, draft.Id);
                slots.Add(new Sv5SpecialSlot(site.Id + "_SLOT_" + draft.Id, site.Id, draft.Kind, local, world, stable));
            }
            foreach (PortDraft draft in ports)
            {
                Sv5SpecialWorldPoint[] open = draft.LocalCells.Select(value =>
                    new Sv5SpecialWorldPoint(site.Origin.X + value.X, site.Origin.Y + value.Y)).ToArray();
                accesses.Add(new Sv5SpecialAccess(site.Id + "_PORT_" + draft.Id, site.Id, draft.Side, draft.Flow,
                    true, draft.Condition, draft.SourceNodeId, open));
            }
        }

        private static Placement ChooseWithinPatches(
            Sv5WorldBiomePlan plan,
            DeterministicRngStream rng,
            ISet<Sv5SpecialWorldPoint> used,
            IEnumerable<Sv5WorldBiomePatch> patches,
            int width,
            int height,
            string token)
        {
            Sv5WorldBiomePatch[] candidates = (patches ?? Array.Empty<Sv5WorldBiomePatch>()).Where(value => value != null)
                .Distinct().OrderBy(value => value).ToArray();
            if (candidates.Length == 0) throw new InvalidOperationException("SV5 has no eligible patch for " + token + ".");
            var attempts = new List<Sv5SpecialPlacementAttempt>();
            for (int attempt = 1; attempt <= MaxPlacementAttempts; attempt++)
            {
                Sv5WorldBiomePatch patch = candidates[rng.NextInt(candidates.Length)];
                int availableWidth = patch.WidthMicroChunks * Sv5WorldBiomePlanner.MicroChunkWidthTiles - width - 8;
                int availableHeight = patch.HeightMicroChunks * Sv5WorldBiomePlanner.MicroChunkHeightTiles - height - 8;
                if (availableWidth < 0 || availableHeight < 0)
                {
                    attempts.Add(new Sv5SpecialPlacementAttempt(attempt, patch.PatchId, "REJECT_TEMPLATE_EXCEEDS_PATCH"));
                    continue;
                }
                var origin = new Sv5SpecialWorldPoint(patch.MinTileX + 4 + rng.NextInt(availableWidth + 1),
                    patch.MinTileY + 4 + rng.NextInt(availableHeight + 1));
                if (Intersects(used, origin, width, height))
                {
                    attempts.Add(new Sv5SpecialPlacementAttempt(attempt, patch.PatchId, "REJECT_RESERVED_COLLISION"));
                    continue;
                }
                attempts.Add(new Sv5SpecialPlacementAttempt(attempt, patch.PatchId, "SELECTED"));
                return new Placement(origin, new[] { patch.PatchId }, attempts);
            }
            throw new InvalidOperationException("SV5 bounded placement exhausted for " + token + ".");
        }

        private static SharedPlacement ChooseSharedBoundary(
            Sv5WorldBiomePlan plan,
            DeterministicRngStream rng,
            ISet<Sv5SpecialWorldPoint> used,
            Sv5WorldBiomeReservationInput seal,
            Sv5WorldBiomeReservationInput boss,
            int width,
            int height)
        {
            var candidates = plan.Boundaries.Where(value =>
                (value.Source.Biome.CanonicalId == seal.AllowedBiome.CanonicalId && value.Target.Biome.CanonicalId == boss.AllowedBiome.CanonicalId) ||
                (value.Source.Biome.CanonicalId == boss.AllowedBiome.CanonicalId && value.Target.Biome.CanonicalId == seal.AllowedBiome.CanonicalId))
                .Where(value => value.Direction == Sv5WorldGraphDirection.Right || value.Direction == Sv5WorldGraphDirection.Up)
                .Where(value => value.Direction == Sv5WorldGraphDirection.Right
                    ? value.Source.Y == value.Source.Patch.MinMicroY + (value.Source.Patch.HeightMicroChunks / 2)
                    : value.Source.X == value.Source.Patch.MinMicroX + (value.Source.Patch.WidthMicroChunks / 2))
                .OrderBy(value => value.BoundaryId, StringComparer.Ordinal).ToArray();
            if (candidates.Length == 0) throw new InvalidOperationException("SV5 has no Seal/Boss boundary candidate.");
            var attempts = new List<Sv5SpecialPlacementAttempt>();
            for (int attempt = 1; attempt <= MaxPlacementAttempts; attempt++)
            {
                Sv5WorldBiomeBoundary boundary = candidates[rng.NextInt(candidates.Length)];
                bool sealIsSource = boundary.Source.Biome.CanonicalId == seal.AllowedBiome.CanonicalId;
                Sv5SpecialWorldPoint origin;
                Sv5SpecialWorldPoint sealLocal;
                Sv5SpecialWorldPoint bossLocal;
                if (boundary.Direction == Sv5WorldGraphDirection.Right)
                {
                    int boundaryX = (boundary.Source.X + 1) * Sv5WorldBiomePlanner.MicroChunkWidthTiles;
                    origin = new Sv5SpecialWorldPoint(boundaryX - (width / 2), boundary.Source.Y * Sv5WorldBiomePlanner.MicroChunkHeightTiles - 5);
                    sealLocal = new Sv5SpecialWorldPoint(sealIsSource ? width / 4 : (width * 3) / 4, 2);
                    bossLocal = new Sv5SpecialWorldPoint(sealIsSource ? (width * 3) / 4 : width / 4, 2);
                }
                else
                {
                    int boundaryY = (boundary.Source.Y + 1) * Sv5WorldBiomePlanner.MicroChunkHeightTiles;
                    origin = new Sv5SpecialWorldPoint(boundary.Source.X * Sv5WorldBiomePlanner.MicroChunkWidthTiles - 7,
                        boundaryY - (height / 2));
                    sealLocal = new Sv5SpecialWorldPoint(width / 4, sealIsSource ? height / 4 : (height * 3) / 4);
                    bossLocal = new Sv5SpecialWorldPoint((width * 3) / 4, sealIsSource ? (height * 3) / 4 : height / 4);
                }
                if (!FitsWorld(origin, width, height) || Intersects(used, origin, width, height))
                {
                    attempts.Add(new Sv5SpecialPlacementAttempt(attempt, boundary.BoundaryId, "REJECT_RESERVED_COLLISION"));
                    continue;
                }
                string[] owned = FootprintPatchIds(plan, origin, width, height);
                string[] allowed = seal.CandidatePatches.Concat(boss.CandidatePatches).Select(value => value.PatchId)
                    .Distinct(StringComparer.Ordinal).ToArray();
                if (owned.Any(value => !allowed.Contains(value, StringComparer.Ordinal)) || owned.Length != 2)
                {
                    attempts.Add(new Sv5SpecialPlacementAttempt(attempt, boundary.BoundaryId, "REJECT_PATCH_CONTRACT"));
                    continue;
                }
                attempts.Add(new Sv5SpecialPlacementAttempt(attempt, boundary.BoundaryId, "SELECTED_SHARED_BOUNDARY"));
                return new SharedPlacement(new Placement(origin, owned, attempts), sealLocal, bossLocal);
            }
            throw new InvalidOperationException("SV5 bounded shared Seal/Boss placement exhausted.");
        }

        private static Sv5PatternBaseCell[,] BuildShell(
            int width,
            int height,
            IEnumerable<SlotDraft> slots,
            IEnumerable<PortDraft> ports)
        {
            var portCells = new HashSet<Sv5SpecialWorldPoint>((ports ?? Array.Empty<PortDraft>()).SelectMany(value => value.LocalCells));
            var result = new Sv5PatternBaseCell[width, height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                bool edgeWall = (x == 0 || x == width - 1) && !portCells.Contains(new Sv5SpecialWorldPoint(x, y));
                result[x, y] = y <= 1 || edgeWall ? Sv5PatternBaseCell.Solid : Sv5PatternBaseCell.Air;
            }
            // A fixed one-way bridge is a typed terrain input, not a material-name
            // hint. It remains protected, uses only its upper collision face, and
            // is never advertised as a Grab support.
            for (int x = (width / 2) - 1; x <= (width / 2) + 1; x++)
                result[x, height / 2] = Sv5PatternBaseCell.OneWayPlatform;
            foreach (SlotDraft slot in slots ?? Array.Empty<SlotDraft>())
            {
                if (slot.X < 2 || slot.X >= width - 2 || slot.Y < 2 || slot.Y >= height - 2)
                    throw new ArgumentOutOfRangeException(nameof(slots), "Slot must fit inside the fixed shell.");
                if (slot.Y > 2)
                for (int x = slot.X - 1; x <= slot.X + 1; x++) result[x, slot.Y - 1] = Sv5PatternBaseCell.OneWayPlatform;
                result[slot.X, slot.Y] = Sv5PatternBaseCell.Air;
                result[slot.X, slot.Y + 1] = Sv5PatternBaseCell.Air;
            }
            return result;
        }

        private static List<PortDraft> StandardPorts(
            int width,
            int height,
            Sv5SpecialAccessFlow entry,
            Sv5SpecialAccessFlow? exit,
            string condition,
            string sourceNodeId)
        {
            int middle = height / 2;
            var result = new List<PortDraft>
            {
                new PortDraft("ENTRY", Sv5WorldGraphDirection.Left, entry, condition, sourceNodeId,
                    PortCells(0, middle)),
            };
            if (exit.HasValue)
                result.Add(new PortDraft("EXIT", Sv5WorldGraphDirection.Right, exit.Value, condition, sourceNodeId,
                    PortCells(width - 1, middle)));
            return result;
        }

        private static IEnumerable<Sv5SpecialWorldPoint> PortCells(int x, int middle) =>
            Enumerable.Range(middle - 1, 3).Select(y => new Sv5SpecialWorldPoint(x, y));

        private static Sv5WorldBiomeReservationInput InputFor(Sv5WorldBiomePlan plan, Sv5WorldGraphRole role) =>
            plan.ReservationInputs.Single(value => value.Role == role);

        private static bool IsGraphSlot(Sv5WorldGraphRole role, Sv5SpecialSlotKind kind)
        {
            switch (role)
            {
                case Sv5WorldGraphRole.Start: return kind == Sv5SpecialSlotKind.Spawn;
                case Sv5WorldGraphRole.MooncoreOre:
                case Sv5WorldGraphRole.CondensedCoefficientSap:
                case Sv5WorldGraphRole.DeepStarYeast: return kind == Sv5SpecialSlotKind.Resource;
                case Sv5WorldGraphRole.Forge: return kind == Sv5SpecialSlotKind.Forge;
                case Sv5WorldGraphRole.Exit: return kind == Sv5SpecialSlotKind.Exit;
                default: return false;
            }
        }

        private static Sv5WorldStableIdKind StableKind(Sv5SpecialSlotKind kind)
        {
            switch (kind)
            {
                case Sv5SpecialSlotKind.Resource: return Sv5WorldStableIdKind.RewardChest;
                case Sv5SpecialSlotKind.Npc:
                case Sv5SpecialSlotKind.Shop: return Sv5WorldStableIdKind.NPCShopSlot;
                case Sv5SpecialSlotKind.Boss: return Sv5WorldStableIdKind.MonsterSpawnSlot;
                default: return Sv5WorldStableIdKind.SpecialRegionTrigger;
            }
        }

        private static bool FitsWorld(Sv5SpecialWorldPoint origin, int width, int height) =>
            IsInWorld(origin.X, origin.Y) && IsInWorld(origin.X + width - 1, origin.Y + height - 1);

        private static bool Intersects(ISet<Sv5SpecialWorldPoint> used, Sv5SpecialWorldPoint origin, int width, int height)
        {
            for (int y = origin.Y; y < origin.Y + height; y++)
            for (int x = origin.X; x < origin.X + width; x++)
                if (used.Contains(new Sv5SpecialWorldPoint(x, y))) return true;
            return false;
        }

        private static string[] FootprintPatchIds(Sv5WorldBiomePlan plan, Sv5SpecialWorldPoint origin, int width, int height) =>
            Enumerable.Range(origin.Y, height).SelectMany(y => Enumerable.Range(origin.X, width).Select(x =>
                plan.GetCell(x / Sv5WorldBiomePlanner.MicroChunkWidthTiles,
                    y / Sv5WorldBiomePlanner.MicroChunkHeightTiles).PatchId)).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();

        private sealed class Placement
        {
            public Placement(Sv5SpecialWorldPoint origin, IEnumerable<string> occupiedPatchIds,
                IEnumerable<Sv5SpecialPlacementAttempt> attempts)
            {
                Origin = origin;
                OccupiedPatchIds = (occupiedPatchIds ?? Array.Empty<string>()).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray();
                Attempts = (attempts ?? Array.Empty<Sv5SpecialPlacementAttempt>()).ToArray();
            }
            public Sv5SpecialWorldPoint Origin { get; }
            public IReadOnlyList<string> OccupiedPatchIds { get; }
            public IReadOnlyList<Sv5SpecialPlacementAttempt> Attempts { get; }
        }

        private sealed class SharedPlacement
        {
            public SharedPlacement(Placement placement, Sv5SpecialWorldPoint sealLocal, Sv5SpecialWorldPoint bossLocal)
            {
                Placement = placement;
                SealLocal = sealLocal;
                BossLocal = bossLocal;
            }
            public Placement Placement { get; }
            public Sv5SpecialWorldPoint SealLocal { get; }
            public Sv5SpecialWorldPoint BossLocal { get; }
        }

        private sealed class SlotDraft
        {
            public SlotDraft(string id, Sv5SpecialSlotKind kind, int x, int y)
            {
                Id = id;
                Kind = kind;
                X = x;
                Y = y;
            }
            public string Id { get; }
            public Sv5SpecialSlotKind Kind { get; }
            public int X { get; }
            public int Y { get; }
        }

        private sealed class PortDraft
        {
            public PortDraft(string id, Sv5WorldGraphDirection side, Sv5SpecialAccessFlow flow,
                string condition, string sourceNodeId, IEnumerable<Sv5SpecialWorldPoint> localCells)
            {
                Id = id;
                Side = side;
                Flow = flow;
                Condition = condition;
                SourceNodeId = sourceNodeId ?? string.Empty;
                LocalCells = (localCells ?? Array.Empty<Sv5SpecialWorldPoint>()).ToArray();
            }
            public string Id { get; }
            public Sv5WorldGraphDirection Side { get; }
            public Sv5SpecialAccessFlow Flow { get; }
            public string Condition { get; }
            public string SourceNodeId { get; }
            public IReadOnlyList<Sv5SpecialWorldPoint> LocalCells { get; }
        }
    }

    public static class Sv5SpecialReservationExport
    {
        public static string ManifestJson(Sv5SpecialReservationPlan plan)
        {
            RequirePlan(plan);
            return "{\n" +
                "  \"format\": \"SV5_SPECIAL_RESERVATION_V1\",\n" +
                "  \"seed\": " + plan.BiomePlan.Definition.Request.Seed.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"definition_digest\": \"" + plan.BiomePlan.Definition.Digest + "\",\n" +
                "  \"sv5_biome_digest\": \"" + plan.BiomePlan.Digest + "\",\n" +
                "  \"special_reservation_initial_state\": \"" + plan.SpecialReservationInitialState.ToString("x16", CultureInfo.InvariantCulture) + "\",\n" +
                "  \"site_count\": " + plan.Sites.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"fixed_cell_count\": " + plan.Cells.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"graph_binding_count\": " + plan.GraphBindings.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"consumer\": \"Sv5SpecialReservationPlan.EvaluateTerrainCells\",\n" +
                "  \"full_world_bake\": \"PENDING_SV517\",\n" +
                "  \"digest\": \"" + plan.Digest + "\"\n}" + "\n";
        }

        public static string SitesCsv(Sv5SpecialReservationPlan plan) => Lines(
            "site_id,role,stable_id,origin_x,origin_y,width_tiles,height_tiles,template_id,template_version,transform,eligible_patch_ids,occupied_patch_ids,attempts",
            plan.Sites.Select(value => Row(value.Id, value.Role, value.StableId.Value, value.Origin.X, value.Origin.Y,
                value.WidthTiles, value.HeightTiles, value.TemplateId, value.TemplateVersion, value.Transform,
                string.Join("|", value.EligiblePatchIds), string.Join("|", value.OccupiedPatchIds),
                string.Join("|", value.Attempts.Select(attempt => attempt.Ordinal + ":" + attempt.CandidateId + ":" + attempt.Outcome)))));

        public static string GraphBindingsCsv(Sv5SpecialReservationPlan plan) => Lines(
            "reservation_id,node_id,role,release_condition,site_id,local_x,local_y,world_x,world_y",
            plan.GraphBindings.Select(value => Row(value.ReservationId, value.NodeId, value.Role,
                value.Reservation.ReleaseCondition, value.SiteId, value.LocalPoint.X, value.LocalPoint.Y,
                value.WorldPoint.X, value.WorldPoint.Y)));

        public static string FixedCellsCsv(Sv5SpecialReservationPlan plan) => Lines(
            "site_id,local_x,local_y,world_x,world_y,patch_id,base,protection,collision_meaning",
            plan.Cells.Select(value => Row(value.SiteId, value.LocalX, value.LocalY, value.World.X, value.World.Y,
                value.PatchId, Token(value.BaseCell), value.Protection, CollisionMeaning(value.BaseCell))));

        public static string AccessCsv(Sv5SpecialReservationPlan plan) => Lines(
            "access_id,site_id,side,flow,required,condition,source_node_id,world_x,world_y",
            plan.Accesses.SelectMany(access => access.OpenCells.Select(point => Row(access.Id, access.SiteId,
                access.Side, access.Flow, access.Required, access.Condition, access.SourceNodeId, point.X, point.Y))));

        public static string SlotsCsv(Sv5SpecialReservationPlan plan) => Lines(
            "slot_id,site_id,kind,stable_id,local_x,local_y,world_x,world_y,occupancy_width,occupancy_height",
            plan.Slots.Select(value => Row(value.Id, value.SiteId, value.Kind, value.StableId.Value, value.Local.X,
                value.Local.Y, value.World.X, value.World.Y, value.OccupancyWidthTiles, value.OccupancyHeightTiles)));

        public static string OwnershipCsv(Sv5SpecialReservationPlan plan) => Lines(
            "site_id,reserved_cells,fixed_solid_cells,protected_air_cells,occupied_patch_ids,terrain_intrusion_policy",
            plan.Sites.Select(site =>
            {
                Sv5SpecialWorldCell[] cells = plan.Cells.Where(value => value.SiteId == site.Id).ToArray();
                return Row(site.Id, cells.Length, cells.Count(value => value.Protection == Sv5SpecialProtectionKind.FixedSolid),
                    cells.Count(value => value.Protection == Sv5SpecialProtectionKind.ProtectedAir),
                    string.Join("|", site.OccupiedPatchIds), "REJECT_EVALUATE_TERRAIN_CELLS");
            }));

        public static string StateGeometryCsv(Sv5SpecialReservationPlan plan) => Lines(
            "site_id,state,world_x,world_y,base,reason",
            plan.StateGeometry.Select(value => Row(value.SiteId, value.State, value.World.X, value.World.Y,
                Token(value.BaseCell), value.Reason)));

        private static void RequirePlan(Sv5SpecialReservationPlan plan)
        {
            if (plan == null || !plan.Success) throw new ArgumentException("A passing SV5 plan is required.", nameof(plan));
        }

        private static string Lines(string header, IEnumerable<string> rows) => header + "\n" +
            string.Join("\n", rows ?? Array.Empty<string>()) + "\n";

        private static string Row(params object[] values) => string.Join(",", (values ?? Array.Empty<object>()).Select(Csv));

        private static string Csv(object value)
        {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        private static string Token(Sv5PatternBaseCell value)
        {
            switch (value)
            {
                case Sv5PatternBaseCell.Air: return "A";
                case Sv5PatternBaseCell.Solid: return "S";
                case Sv5PatternBaseCell.OneWayPlatform: return "O";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private static string CollisionMeaning(Sv5PatternBaseCell value) => value == Sv5PatternBaseCell.Air
            ? "PROTECTED_AIR_NO_GENERAL_FILL" : value == Sv5PatternBaseCell.Solid
            ? "FIXED_SOLID_COLLIDER" : "FIXED_ONE_WAY_TOP_COLLIDER_NO_GRAB";
    }

    internal static class Sv5SpecialReservationIdentity
    {
        public static string Hash(IEnumerable<string> values) => Sv5WorldDefinition.Hash(string.Join("\n", values ?? Array.Empty<string>()));
        public static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A stable value is required.", name);
            return value.Trim();
        }
    }
}
