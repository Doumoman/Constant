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
    /// RMAP15's eight physical sites.  Seal and Boss deliberately share one
    /// site while retaining separate graph bindings and local points.
    /// </summary>
    public enum RmapSpecialPhysicalRole
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

    public enum RmapSpecialSlotKind
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

    public enum RmapSpecialProtectionKind
    {
        FixedSolid = 1,
        ProtectedAir = 2,
    }

    public enum RmapSpecialAccessFlow { In = 1, Out = 2, Both = 3 }
    public enum RmapSpecialTerrainReservationDecisionKind { Allowed = 1, RejectedProtectedCell = 2 }

    public readonly struct RmapSpecialWorldPoint : IEquatable<RmapSpecialWorldPoint>, IComparable<RmapSpecialWorldPoint>
    {
        public RmapSpecialWorldPoint(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
        public int CompareTo(RmapSpecialWorldPoint other)
        {
            int value = Y.CompareTo(other.Y);
            return value != 0 ? value : X.CompareTo(other.X);
        }
        public bool Equals(RmapSpecialWorldPoint other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is RmapSpecialWorldPoint other && Equals(other);
        public override int GetHashCode() { unchecked { return (X * 397) ^ Y; } }
        public override string ToString() => X.ToString(CultureInfo.InvariantCulture) + "," +
            Y.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class RmapSpecialPlacementAttempt : IComparable<RmapSpecialPlacementAttempt>
    {
        internal RmapSpecialPlacementAttempt(int ordinal, string candidateId, string outcome)
        {
            Ordinal = ordinal;
            CandidateId = RmapSpecialReservationIdentity.Require(candidateId, nameof(candidateId));
            Outcome = RmapSpecialReservationIdentity.Require(outcome, nameof(outcome));
        }

        public int Ordinal { get; }
        public string CandidateId { get; }
        public string Outcome { get; }
        public int CompareTo(RmapSpecialPlacementAttempt other) => other == null ? 1 : Ordinal.CompareTo(other.Ordinal);
    }

    public sealed class RmapSpecialWorldCell : IComparable<RmapSpecialWorldCell>
    {
        internal RmapSpecialWorldCell(
            string siteId,
            int localX,
            int localY,
            int worldX,
            int worldY,
            string patchId,
            RmapPatternBaseCell baseCell)
        {
            SiteId = RmapSpecialReservationIdentity.Require(siteId, nameof(siteId));
            if (!RmapSpecialReservationPlanner.IsInWorld(worldX, worldY))
                throw new ArgumentOutOfRangeException(nameof(worldX));
            if (!Enum.IsDefined(typeof(RmapPatternBaseCell), baseCell))
                throw new ArgumentOutOfRangeException(nameof(baseCell));
            LocalX = localX;
            LocalY = localY;
            World = new RmapSpecialWorldPoint(worldX, worldY);
            PatchId = RmapSpecialReservationIdentity.Require(patchId, nameof(patchId));
            BaseCell = baseCell;
            Protection = baseCell == RmapPatternBaseCell.Air
                ? RmapSpecialProtectionKind.ProtectedAir
                : RmapSpecialProtectionKind.FixedSolid;
        }

        public string SiteId { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public RmapSpecialWorldPoint World { get; }
        public string PatchId { get; }
        public RmapPatternBaseCell BaseCell { get; }
        public RmapSpecialProtectionKind Protection { get; }
        public int CompareTo(RmapSpecialWorldCell other)
        {
            if (other == null) return 1;
            int value = string.Compare(SiteId, other.SiteId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = World.CompareTo(other.World);
            return value != 0 ? value : LocalX.CompareTo(other.LocalX);
        }
    }

    public sealed class RmapSpecialSlot : IComparable<RmapSpecialSlot>
    {
        internal RmapSpecialSlot(
            string id,
            string siteId,
            RmapSpecialSlotKind kind,
            RmapSpecialWorldPoint local,
            RmapSpecialWorldPoint world,
            RmapWorldStableId stableId)
        {
            Id = RmapSpecialReservationIdentity.Require(id, nameof(id));
            SiteId = RmapSpecialReservationIdentity.Require(siteId, nameof(siteId));
            Kind = kind;
            Local = local;
            World = world;
            StableId = stableId ?? throw new ArgumentNullException(nameof(stableId));
        }

        public string Id { get; }
        public string SiteId { get; }
        public RmapSpecialSlotKind Kind { get; }
        public RmapSpecialWorldPoint Local { get; }
        public RmapSpecialWorldPoint World { get; }
        public RmapWorldStableId StableId { get; }
        public int OccupancyWidthTiles => 1;
        public int OccupancyHeightTiles => 1;
        public int CompareTo(RmapSpecialSlot other) => other == null ? 1 :
            string.Compare(Id, other.Id, StringComparison.Ordinal);
    }

    public sealed class RmapSpecialAccess : IComparable<RmapSpecialAccess>
    {
        private readonly ReadOnlyCollection<RmapSpecialWorldPoint> openCells;

        internal RmapSpecialAccess(
            string id,
            string siteId,
            RmapWorldGraphDirection side,
            RmapSpecialAccessFlow flow,
            bool required,
            string condition,
            string sourceNodeId,
            IEnumerable<RmapSpecialWorldPoint> cells)
        {
            Id = RmapSpecialReservationIdentity.Require(id, nameof(id));
            SiteId = RmapSpecialReservationIdentity.Require(siteId, nameof(siteId));
            Side = side;
            Flow = flow;
            Required = required;
            Condition = RmapSpecialReservationIdentity.Require(condition, nameof(condition));
            SourceNodeId = sourceNodeId ?? string.Empty;
            var copy = (cells ?? Array.Empty<RmapSpecialWorldPoint>()).Distinct().OrderBy(value => value).ToArray();
            if (copy.Length == 0) throw new ArgumentException("An access port needs open cells.", nameof(cells));
            openCells = new ReadOnlyCollection<RmapSpecialWorldPoint>(copy);
        }

        public string Id { get; }
        public string SiteId { get; }
        public RmapWorldGraphDirection Side { get; }
        public RmapSpecialAccessFlow Flow { get; }
        public bool Required { get; }
        public string Condition { get; }
        public string SourceNodeId { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> OpenCells => openCells;
        public int CompareTo(RmapSpecialAccess other) => other == null ? 1 :
            string.Compare(Id, other.Id, StringComparison.Ordinal);
    }

    public sealed class RmapSpecialGraphBinding : IComparable<RmapSpecialGraphBinding>
    {
        internal RmapSpecialGraphBinding(
            RmapWorldGraphReservation reservation,
            string siteId,
            RmapSpecialWorldPoint localPoint,
            RmapSpecialWorldPoint worldPoint)
        {
            Reservation = reservation ?? throw new ArgumentNullException(nameof(reservation));
            SiteId = RmapSpecialReservationIdentity.Require(siteId, nameof(siteId));
            LocalPoint = localPoint;
            WorldPoint = worldPoint;
        }

        public RmapWorldGraphReservation Reservation { get; }
        public string ReservationId => Reservation.ReservationId;
        public string NodeId => Reservation.Node.NodeId;
        public RmapWorldGraphRole Role => Reservation.Node.Role;
        public string SiteId { get; }
        public RmapSpecialWorldPoint LocalPoint { get; }
        public RmapSpecialWorldPoint WorldPoint { get; }
        public int CompareTo(RmapSpecialGraphBinding other) => other == null ? 1 :
            string.Compare(ReservationId, other.ReservationId, StringComparison.Ordinal);
    }

    public sealed class RmapSpecialStateGeometryCell : IComparable<RmapSpecialStateGeometryCell>
    {
        internal RmapSpecialStateGeometryCell(
            string siteId,
            string state,
            RmapSpecialWorldPoint world,
            RmapPatternBaseCell baseCell,
            string reason)
        {
            SiteId = RmapSpecialReservationIdentity.Require(siteId, nameof(siteId));
            State = RmapSpecialReservationIdentity.Require(state, nameof(state));
            World = world;
            BaseCell = baseCell;
            Reason = RmapSpecialReservationIdentity.Require(reason, nameof(reason));
        }

        public string SiteId { get; }
        public string State { get; }
        public RmapSpecialWorldPoint World { get; }
        public RmapPatternBaseCell BaseCell { get; }
        public string Reason { get; }
        public int CompareTo(RmapSpecialStateGeometryCell other)
        {
            if (other == null) return 1;
            int value = string.Compare(SiteId, other.SiteId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(State, other.State, StringComparison.Ordinal);
            return value != 0 ? value : World.CompareTo(other.World);
        }
    }

    public sealed class RmapSpecialTerrainReservationDecision
    {
        internal RmapSpecialTerrainReservationDecision(
            RmapSpecialTerrainReservationDecisionKind kind,
            IEnumerable<RmapSpecialWorldCell> protectedCells)
        {
            Kind = kind;
            ProtectedCells = new ReadOnlyCollection<RmapSpecialWorldCell>((protectedCells ??
                Array.Empty<RmapSpecialWorldCell>()).Distinct().OrderBy(value => value).ToArray());
        }

        public RmapSpecialTerrainReservationDecisionKind Kind { get; }
        public IReadOnlyList<RmapSpecialWorldCell> ProtectedCells { get; }
        public bool IsAllowed => Kind == RmapSpecialTerrainReservationDecisionKind.Allowed;
    }

    public sealed class RmapSpecialSite : IComparable<RmapSpecialSite>
    {
        private readonly ReadOnlyCollection<string> eligiblePatchIds;
        private readonly ReadOnlyCollection<string> occupiedPatchIds;
        private readonly ReadOnlyCollection<RmapSpecialPlacementAttempt> attempts;

        internal RmapSpecialSite(
            string id,
            RmapSpecialPhysicalRole role,
            string templateId,
            string templateVersion,
            RmapSpecialWorldPoint origin,
            int widthTiles,
            int heightTiles,
            IEnumerable<string> eligible,
            IEnumerable<string> occupied,
            IEnumerable<RmapSpecialPlacementAttempt> selectionAttempts,
            RmapWorldStableId stableId)
        {
            Id = RmapSpecialReservationIdentity.Require(id, nameof(id));
            Role = role;
            TemplateId = RmapSpecialReservationIdentity.Require(templateId, nameof(templateId));
            TemplateVersion = RmapSpecialReservationIdentity.Require(templateVersion, nameof(templateVersion));
            Origin = origin;
            if (widthTiles < 4 || heightTiles < 4) throw new ArgumentOutOfRangeException(nameof(widthTiles));
            WidthTiles = widthTiles;
            HeightTiles = heightTiles;
            eligiblePatchIds = Freeze(eligible);
            occupiedPatchIds = Freeze(occupied);
            attempts = new ReadOnlyCollection<RmapSpecialPlacementAttempt>((selectionAttempts ??
                Array.Empty<RmapSpecialPlacementAttempt>()).OrderBy(value => value).ToArray());
            if (eligiblePatchIds.Count == 0 || occupiedPatchIds.Count == 0 || attempts.Count == 0)
                throw new ArgumentException("A site needs candidates, occupied patches, and diagnostics.");
            StableId = stableId ?? throw new ArgumentNullException(nameof(stableId));
        }

        public string Id { get; }
        public RmapSpecialPhysicalRole Role { get; }
        public string TemplateId { get; }
        public string TemplateVersion { get; }
        public string Transform => "R0";
        public RmapSpecialWorldPoint Origin { get; }
        public int WidthTiles { get; }
        public int HeightTiles { get; }
        public RmapWorldStableId StableId { get; }
        public IReadOnlyList<string> EligiblePatchIds => eligiblePatchIds;
        public IReadOnlyList<string> OccupiedPatchIds => occupiedPatchIds;
        public IReadOnlyList<RmapSpecialPlacementAttempt> Attempts => attempts;
        public int CompareTo(RmapSpecialSite other) => other == null ? 1 :
            string.Compare(Id, other.Id, StringComparison.Ordinal);

        private static ReadOnlyCollection<string> Freeze(IEnumerable<string> values) =>
            new ReadOnlyCollection<string>((values ?? Array.Empty<string>()).Where(value =>
                !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }

    public sealed class RmapSpecialReservationPlan
    {
        private readonly ReadOnlyCollection<RmapSpecialSite> sites;
        private readonly ReadOnlyCollection<RmapSpecialWorldCell> cells;
        private readonly ReadOnlyCollection<RmapSpecialSlot> slots;
        private readonly ReadOnlyCollection<RmapSpecialAccess> accesses;
        private readonly ReadOnlyCollection<RmapSpecialGraphBinding> graphBindings;
        private readonly ReadOnlyCollection<RmapSpecialStateGeometryCell> stateGeometry;
        private readonly IReadOnlyDictionary<string, RmapSpecialWorldCell> cellsByCoordinate;

        internal RmapSpecialReservationPlan(
            RmapWorldBiomePlan biomePlan,
            ulong specialReservationInitialState,
            IEnumerable<RmapSpecialSite> values,
            IEnumerable<RmapSpecialWorldCell> fixedCells,
            IEnumerable<RmapSpecialSlot> siteSlots,
            IEnumerable<RmapSpecialAccess> portBindings,
            IEnumerable<RmapSpecialGraphBinding> bindings,
            IEnumerable<RmapSpecialStateGeometryCell> geometry)
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
                throw new ArgumentException("RMAP15 requires exactly eight physical sites.", nameof(values));
            if (cells.Count == 0 || cells.Select(value => value.World).Distinct().Count() != cells.Count)
                throw new ArgumentException("Every fixed world cell needs one owner.", nameof(fixedCells));
            if (graphBindings.Count != 8 || graphBindings.Select(value => value.ReservationId).Distinct().Count() != 8 ||
                !graphBindings.Any(value => value.Role == RmapWorldGraphRole.Seal) ||
                !graphBindings.Any(value => value.Role == RmapWorldGraphRole.Boss))
                throw new ArgumentException("All eight RMAP13 reservations must be bound exactly once.", nameof(bindings));
            if (graphBindings.Single(value => value.Role == RmapWorldGraphRole.Seal).SiteId !=
                graphBindings.Single(value => value.Role == RmapWorldGraphRole.Boss).SiteId)
                throw new ArgumentException("Seal and Boss must share one physical RMAP15 site.", nameof(bindings));
            cellsByCoordinate = new ReadOnlyDictionary<string, RmapSpecialWorldCell>(cells.ToDictionary(
                value => Key(value.World), value => value, StringComparer.Ordinal));
            ValidateReferences();
            Digest = RmapSpecialReservationIdentity.Hash(CanonicalLines());
        }

        public RmapWorldBiomePlan BiomePlan { get; }
        public ulong SpecialReservationInitialState { get; }
        public IReadOnlyList<RmapSpecialSite> Sites => sites;
        public IReadOnlyList<RmapSpecialWorldCell> Cells => cells;
        public IReadOnlyList<RmapSpecialSlot> Slots => slots;
        public IReadOnlyList<RmapSpecialAccess> Accesses => accesses;
        public IReadOnlyList<RmapSpecialGraphBinding> GraphBindings => graphBindings;
        public IReadOnlyList<RmapSpecialStateGeometryCell> StateGeometry => stateGeometry;
        public string Digest { get; }
        public bool Success => Sites.Count == 8 && Cells.Count != 0 && GraphBindings.Count == 8;

        /// <summary>Direct RMAP16/RMAP17 handoff: any fixed SOLID, ONE_WAY,
        /// protected AIR, port, or slot cell is unavailable to general terrain.</summary>
        public RmapSpecialTerrainReservationDecision EvaluateTerrainCells(
            IEnumerable<RmapSpecialWorldPoint> proposedCells)
        {
            var conflicts = (proposedCells ?? Array.Empty<RmapSpecialWorldPoint>()).Distinct()
                .Where(value => cellsByCoordinate.ContainsKey(Key(value)))
                .Select(value => cellsByCoordinate[Key(value)]).OrderBy(value => value).ToArray();
            return new RmapSpecialTerrainReservationDecision(conflicts.Length == 0
                ? RmapSpecialTerrainReservationDecisionKind.Allowed
                : RmapSpecialTerrainReservationDecisionKind.RejectedProtectedCell, conflicts);
        }

        public bool TryGetCell(RmapSpecialWorldPoint world, out RmapSpecialWorldCell cell) =>
            cellsByCoordinate.TryGetValue(Key(world), out cell);

        private void ValidateReferences()
        {
            var siteIds = new HashSet<string>(sites.Select(value => value.Id), StringComparer.Ordinal);
            if (cells.Any(value => !siteIds.Contains(value.SiteId)) || slots.Any(value => !siteIds.Contains(value.SiteId)) ||
                accesses.Any(value => !siteIds.Contains(value.SiteId)) || graphBindings.Any(value => !siteIds.Contains(value.SiteId)))
                throw new ArgumentException("RMAP15 publication references an unknown site.");
            if (slots.Select(value => value.Id).Distinct(StringComparer.Ordinal).Count() != slots.Count)
                throw new ArgumentException("RMAP15 slot IDs must be stable and unique.");
            foreach (RmapSpecialSlot slot in slots)
            {
                if (!TryGetCell(slot.World, out RmapSpecialWorldCell cell) || cell.BaseCell != RmapPatternBaseCell.Air ||
                    !TryGetCell(new RmapSpecialWorldPoint(slot.World.X, slot.World.Y - 1), out RmapSpecialWorldCell support) ||
                    (support.BaseCell != RmapPatternBaseCell.Solid && support.BaseCell != RmapPatternBaseCell.OneWayPlatform) ||
                    !TryGetCell(new RmapSpecialWorldPoint(slot.World.X, slot.World.Y + 1), out RmapSpecialWorldCell headroom) ||
                    headroom.BaseCell != RmapPatternBaseCell.Air)
                    throw new ArgumentException("Each RMAP15 slot must have AIR, support, and headroom.");
            }
            foreach (RmapSpecialAccess access in accesses)
            foreach (RmapSpecialWorldPoint point in access.OpenCells)
            {
                if (!TryGetCell(point, out RmapSpecialWorldCell cell) || cell.BaseCell != RmapPatternBaseCell.Air)
                    throw new ArgumentException("Every access cell must be protected AIR.");
            }
            foreach (RmapSpecialWorldCell cell in cells)
            {
                RmapWorldBiomeCell biome = BiomePlan.GetCell(cell.World.X / RmapWorldBiomePlanner.MicroChunkWidthTiles,
                    cell.World.Y / RmapWorldBiomePlanner.MicroChunkHeightTiles);
                if (!string.Equals(cell.PatchId, biome.PatchId, StringComparison.Ordinal))
                    throw new ArgumentException("Fixed cell patch ownership must be sourced from RMAP14.");
            }
        }

        private IEnumerable<string> CanonicalLines()
        {
            yield return "RMAP15_SPECIAL_RESERVATION_PLAN_V1";
            yield return BiomePlan.Digest;
            yield return SpecialReservationInitialState.ToString("x16", CultureInfo.InvariantCulture);
            foreach (RmapSpecialSite site in sites) yield return "site|" + site.Id + "|" + site.Role + "|" +
                site.Origin + "|" + site.WidthTiles + "x" + site.HeightTiles + "|" + site.StableId.Value;
            foreach (RmapSpecialWorldCell cell in cells) yield return "cell|" + cell.SiteId + "|" + cell.World +
                "|" + cell.BaseCell + "|" + cell.PatchId + "|" + cell.Protection;
            foreach (RmapSpecialSlot slot in slots) yield return "slot|" + slot.Id + "|" + slot.SiteId + "|" +
                slot.Kind + "|" + slot.World + "|" + slot.StableId.Value;
            foreach (RmapSpecialGraphBinding binding in graphBindings) yield return "graph|" + binding.ReservationId +
                "|" + binding.SiteId + "|" + binding.WorldPoint;
            foreach (RmapSpecialAccess access in accesses) yield return "access|" + access.Id + "|" + access.SiteId +
                "|" + access.Side + "|" + access.Flow + "|" + string.Join(";", access.OpenCells);
            foreach (RmapSpecialStateGeometryCell state in stateGeometry) yield return "state|" + state.SiteId + "|" +
                state.State + "|" + state.World + "|" + state.BaseCell;
        }

        private static ReadOnlyCollection<T> Read<T>(IEnumerable<T> values) where T : IComparable<T> =>
            new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).Where(value => value != null).OrderBy(value => value).ToArray());
        private static string Key(RmapSpecialWorldPoint point) => point.X.ToString(CultureInfo.InvariantCulture) + "," +
            point.Y.ToString(CultureInfo.InvariantCulture);
    }

    public static class RmapSpecialReservationPlanner
    {
        public const int WorldWidthTiles = RmapWorldBiomePlanner.WorldWidthTiles;
        public const int WorldHeightTiles = RmapWorldBiomePlanner.WorldHeightTiles;
        public const int MaxPlacementAttempts = 32;
        public const string TemplateVersion = "RMAP15_FIXED_SHELL_V1";

        public static RmapSpecialReservationPlan Plan(RmapWorldBiomePlan biomePlan, WorldGenerationRngStreams rngStreams)
        {
            if (biomePlan == null) throw new ArgumentNullException(nameof(biomePlan));
            if (!biomePlan.Success) throw new ArgumentException("RMAP14 input must pass before RMAP15 placement.", nameof(biomePlan));
            if (rngStreams == null) throw new ArgumentNullException(nameof(rngStreams));
            if (biomePlan.Definition.RngBindings[RmapWorldRngStream.SpecialReservation].SourceStreamId !=
                WorldGenerationRngStreams.WorldSiteStreamId)
                throw new ArgumentException("RMAP15 requires RMAP12's SpecialReservation stream.", nameof(biomePlan));

            var rng = RmapWorldDataGenerator.CreateStream(biomePlan.Definition.Request, rngStreams,
                RmapWorldRngStream.SpecialReservation);
            var used = new HashSet<RmapSpecialWorldPoint>();
            var sites = new List<RmapSpecialSite>();
            var cells = new List<RmapSpecialWorldCell>();
            var slots = new List<RmapSpecialSlot>();
            var accesses = new List<RmapSpecialAccess>();
            var graphBindings = new List<RmapSpecialGraphBinding>();
            var stateGeometry = new List<RmapSpecialStateGeometryCell>();

            AddRegularSite(biomePlan, rng, used, sites, cells, slots, accesses, graphBindings,
                RmapSpecialPhysicalRole.Start, "START_SHELL", 16, 12, RmapWorldGraphRole.Start,
                new[] { new SlotDraft("SPAWN", RmapSpecialSlotKind.Spawn, 3, 2) }, "NONE", "START");
            AddRegularSite(biomePlan, rng, used, sites, cells, slots, accesses, graphBindings,
                RmapSpecialPhysicalRole.MooncoreOre, "MOONCORE_RESOURCE_SHELL", 20, 14, RmapWorldGraphRole.MooncoreOre,
                new[] { new SlotDraft("MOONCORE_ORE", RmapSpecialSlotKind.Resource, 4, 2) }, "NONE", "MOONCORE");
            AddRegularSite(biomePlan, rng, used, sites, cells, slots, accesses, graphBindings,
                RmapSpecialPhysicalRole.CondensedCoefficientSap, "SAP_RESOURCE_SHELL", 20, 14,
                RmapWorldGraphRole.CondensedCoefficientSap,
                new[] { new SlotDraft("COEFFICIENT_SAP", RmapSpecialSlotKind.Resource, 4, 2) }, "NONE", "SAP");
            AddRegularSite(biomePlan, rng, used, sites, cells, slots, accesses, graphBindings,
                RmapSpecialPhysicalRole.DeepStarYeast, "YEAST_RESOURCE_SHELL", 20, 14, RmapWorldGraphRole.DeepStarYeast,
                new[] { new SlotDraft("STAR_YEAST", RmapSpecialSlotKind.Resource, 4, 2) }, "NONE", "YEAST");
            AddVillageSite(biomePlan, rng, used, sites, cells, slots, accesses);
            AddRegularSite(biomePlan, rng, used, sites, cells, slots, accesses, graphBindings,
                RmapSpecialPhysicalRole.Forge, "FORGE_SHELL", 20, 16, RmapWorldGraphRole.Forge,
                new[] { new SlotDraft("FORGE", RmapSpecialSlotKind.Forge, 5, 2) }, "ALL_RESOURCES", "FORGE");
            AddSharedSealBossSite(biomePlan, rng, used, sites, cells, slots, accesses, graphBindings, stateGeometry);
            AddRegularSite(biomePlan, rng, used, sites, cells, slots, accesses, graphBindings,
                RmapSpecialPhysicalRole.Exit, "EXIT_SHELL", 16, 12, RmapWorldGraphRole.Exit,
                new[] { new SlotDraft("EXIT", RmapSpecialSlotKind.Exit, 11, 2) }, "BOSS_COMPLETE", "EXIT");

            return new RmapSpecialReservationPlan(biomePlan, rng.InitialState, sites, cells, slots, accesses,
                graphBindings, stateGeometry);
        }

        public static bool IsInWorld(int x, int y) => x >= 0 && x < WorldWidthTiles && y >= 0 && y < WorldHeightTiles;

        private static void AddRegularSite(
            RmapWorldBiomePlan plan,
            DeterministicRngStream rng,
            ISet<RmapSpecialWorldPoint> used,
            ICollection<RmapSpecialSite> sites,
            ICollection<RmapSpecialWorldCell> cells,
            ICollection<RmapSpecialSlot> slots,
            ICollection<RmapSpecialAccess> accesses,
            ICollection<RmapSpecialGraphBinding> graphBindings,
            RmapSpecialPhysicalRole physicalRole,
            string templateId,
            int width,
            int height,
            RmapWorldGraphRole graphRole,
            IEnumerable<SlotDraft> slotDrafts,
            string requiredCondition,
            string token)
        {
            RmapWorldBiomeReservationInput input = InputFor(plan, graphRole);
            Placement placement = ChooseWithinPatches(plan, rng, used, input.CandidatePatches, width, height, token);
            RmapSpecialSite site = BuildSite(plan, physicalRole, templateId, width, height, placement,
                input.CandidatePatches.Select(value => value.PatchId));
            List<SlotDraft> drafts = (slotDrafts ?? Array.Empty<SlotDraft>()).ToList();
            List<PortDraft> ports = StandardPorts(width, height, graphRole == RmapWorldGraphRole.Start ?
                RmapSpecialAccessFlow.Both : RmapSpecialAccessFlow.In, graphRole == RmapWorldGraphRole.Exit ?
                (RmapSpecialAccessFlow?)null : RmapSpecialAccessFlow.Out, requiredCondition, input.Reservation.Node.NodeId);
            PublishSite(plan, site, drafts, ports, cells, slots, accesses, used);
            RmapSpecialSlot graphSlot = slots.Single(value => value.SiteId == site.Id && IsGraphSlot(graphRole, value.Kind));
            graphBindings.Add(new RmapSpecialGraphBinding(input.Reservation, site.Id, graphSlot.Local, graphSlot.World));
            sites.Add(site);
        }

        private static void AddVillageSite(
            RmapWorldBiomePlan plan,
            DeterministicRngStream rng,
            ISet<RmapSpecialWorldPoint> used,
            ICollection<RmapSpecialSite> sites,
            ICollection<RmapSpecialWorldCell> cells,
            ICollection<RmapSpecialSlot> slots,
            ICollection<RmapSpecialAccess> accesses)
        {
            // Village is intentionally a ninth logical concern but an eighth physical site:
            // it has its own stable identity and does not consume/replace a graph reservation.
            var eligible = plan.Patches.Where(value => value.Biome.CanonicalId == "MoonCrater").OrderBy(value => value).ToArray();
            Placement placement = ChooseWithinPatches(plan, rng, used, eligible, 24, 16, "VILLAGE");
            RmapSpecialSite site = BuildSite(plan, RmapSpecialPhysicalRole.Village, "VILLAGE_SHELL", 24, 16,
                placement, eligible.Select(value => value.PatchId));
            PublishSite(plan, site, new[]
            {
                new SlotDraft("NPC", RmapSpecialSlotKind.Npc, 5, 2),
                new SlotDraft("SHOP", RmapSpecialSlotKind.Shop, 9, 2),
            }, StandardPorts(24, 16, RmapSpecialAccessFlow.In, RmapSpecialAccessFlow.Out, "OPTIONAL_VILLAGE",
                "VILLAGE_OPTIONAL"),
            cells, slots, accesses, used);
            sites.Add(site);
        }

        private static void AddSharedSealBossSite(
            RmapWorldBiomePlan plan,
            DeterministicRngStream rng,
            ISet<RmapSpecialWorldPoint> used,
            ICollection<RmapSpecialSite> sites,
            ICollection<RmapSpecialWorldCell> cells,
            ICollection<RmapSpecialSlot> slots,
            ICollection<RmapSpecialAccess> accesses,
            ICollection<RmapSpecialGraphBinding> graphBindings,
            ICollection<RmapSpecialStateGeometryCell> stateGeometry)
        {
            RmapWorldBiomeReservationInput seal = InputFor(plan, RmapWorldGraphRole.Seal);
            RmapWorldBiomeReservationInput boss = InputFor(plan, RmapWorldGraphRole.Boss);
            SharedPlacement shared = ChooseSharedBoundary(plan, rng, used, seal, boss, 28, 18);
            var eligible = seal.CandidatePatches.Concat(boss.CandidatePatches).Select(value => value.PatchId);
            RmapSpecialSite site = BuildSite(plan, RmapSpecialPhysicalRole.SealBoss, "SEAL_BOSS_BOUNDARY_SHELL", 28, 18,
                shared.Placement, eligible);
            var drafts = new[]
            {
                new SlotDraft("SEAL", RmapSpecialSlotKind.Seal, shared.SealLocal.X, shared.SealLocal.Y),
                new SlotDraft("BOSS", RmapSpecialSlotKind.Boss, shared.BossLocal.X, shared.BossLocal.Y),
            };
            List<PortDraft> ports = StandardPorts(28, 18, RmapSpecialAccessFlow.In, RmapSpecialAccessFlow.Out,
                "ALL_RESOURCES_AND_FORGE", seal.Reservation.Node.NodeId + "|" + boss.Reservation.Node.NodeId);
            PublishSite(plan, site, drafts, ports, cells, slots, accesses, used);
            RmapSpecialSlot sealSlot = slots.Single(value => value.SiteId == site.Id && value.Kind == RmapSpecialSlotKind.Seal);
            RmapSpecialSlot bossSlot = slots.Single(value => value.SiteId == site.Id && value.Kind == RmapSpecialSlotKind.Boss);
            graphBindings.Add(new RmapSpecialGraphBinding(seal.Reservation, site.Id, sealSlot.Local, sealSlot.World));
            graphBindings.Add(new RmapSpecialGraphBinding(boss.Reservation, site.Id, bossSlot.Local, bossSlot.World));
            RmapSpecialAccess gate = accesses.Single(value => value.SiteId == site.Id && value.Flow == RmapSpecialAccessFlow.Out);
            foreach (RmapSpecialWorldPoint point in gate.OpenCells)
            {
                stateGeometry.Add(new RmapSpecialStateGeometryCell(site.Id, "SEALED", point,
                    RmapPatternBaseCell.Solid, "Seal gate blocks boss/exit approach until the seal state opens."));
                stateGeometry.Add(new RmapSpecialStateGeometryCell(site.Id, "OPEN", point,
                    RmapPatternBaseCell.Air, "Open state restores the protected fixed-air exit port."));
            }
            sites.Add(site);
        }

        private static RmapSpecialSite BuildSite(
            RmapWorldBiomePlan plan,
            RmapSpecialPhysicalRole role,
            string templateId,
            int width,
            int height,
            Placement placement,
            IEnumerable<string> eligible)
        {
            string id = "RMAP15_SITE_" + role.ToString().ToUpperInvariant();
            var stable = new RmapWorldStableId(RmapWorldStableIdKind.SpecialRegionTrigger,
                "RMAP15|" + plan.Definition.Digest + "|" + id + "|" + placement.Origin,
                "SITE");
            return new RmapSpecialSite(id, role, templateId, TemplateVersion, placement.Origin, width, height,
                eligible, placement.OccupiedPatchIds, placement.Attempts, stable);
        }

        private static void PublishSite(
            RmapWorldBiomePlan plan,
            RmapSpecialSite site,
            IEnumerable<SlotDraft> slotDrafts,
            IEnumerable<PortDraft> portDrafts,
            ICollection<RmapSpecialWorldCell> cells,
            ICollection<RmapSpecialSlot> slots,
            ICollection<RmapSpecialAccess> accesses,
            ISet<RmapSpecialWorldPoint> used)
        {
            var drafts = (slotDrafts ?? Array.Empty<SlotDraft>()).ToArray();
            var ports = (portDrafts ?? Array.Empty<PortDraft>()).ToArray();
            RmapPatternBaseCell[,] baseCells = BuildShell(site.WidthTiles, site.HeightTiles, drafts, ports);
            for (int y = 0; y < site.HeightTiles; y++)
            for (int x = 0; x < site.WidthTiles; x++)
            {
                int worldX = site.Origin.X + x;
                int worldY = site.Origin.Y + y;
                RmapWorldBiomeCell owner = plan.GetCell(worldX / RmapWorldBiomePlanner.MicroChunkWidthTiles,
                    worldY / RmapWorldBiomePlanner.MicroChunkHeightTiles);
                cells.Add(new RmapSpecialWorldCell(site.Id, x, y, worldX, worldY, owner.PatchId, baseCells[x, y]));
                used.Add(new RmapSpecialWorldPoint(worldX, worldY));
            }
            foreach (SlotDraft draft in drafts)
            {
                RmapSpecialWorldPoint local = new RmapSpecialWorldPoint(draft.X, draft.Y);
                RmapSpecialWorldPoint world = new RmapSpecialWorldPoint(site.Origin.X + draft.X, site.Origin.Y + draft.Y);
                RmapWorldStableId stable = new RmapWorldStableId(StableKind(draft.Kind), site.StableId.Value, draft.Id);
                slots.Add(new RmapSpecialSlot(site.Id + "_SLOT_" + draft.Id, site.Id, draft.Kind, local, world, stable));
            }
            foreach (PortDraft draft in ports)
            {
                RmapSpecialWorldPoint[] open = draft.LocalCells.Select(value =>
                    new RmapSpecialWorldPoint(site.Origin.X + value.X, site.Origin.Y + value.Y)).ToArray();
                accesses.Add(new RmapSpecialAccess(site.Id + "_PORT_" + draft.Id, site.Id, draft.Side, draft.Flow,
                    true, draft.Condition, draft.SourceNodeId, open));
            }
        }

        private static Placement ChooseWithinPatches(
            RmapWorldBiomePlan plan,
            DeterministicRngStream rng,
            ISet<RmapSpecialWorldPoint> used,
            IEnumerable<RmapWorldBiomePatch> patches,
            int width,
            int height,
            string token)
        {
            RmapWorldBiomePatch[] candidates = (patches ?? Array.Empty<RmapWorldBiomePatch>()).Where(value => value != null)
                .Distinct().OrderBy(value => value).ToArray();
            if (candidates.Length == 0) throw new InvalidOperationException("RMAP15 has no eligible patch for " + token + ".");
            var attempts = new List<RmapSpecialPlacementAttempt>();
            for (int attempt = 1; attempt <= MaxPlacementAttempts; attempt++)
            {
                RmapWorldBiomePatch patch = candidates[rng.NextInt(candidates.Length)];
                int availableWidth = patch.WidthMicroChunks * RmapWorldBiomePlanner.MicroChunkWidthTiles - width - 8;
                int availableHeight = patch.HeightMicroChunks * RmapWorldBiomePlanner.MicroChunkHeightTiles - height - 8;
                if (availableWidth < 0 || availableHeight < 0)
                {
                    attempts.Add(new RmapSpecialPlacementAttempt(attempt, patch.PatchId, "REJECT_TEMPLATE_EXCEEDS_PATCH"));
                    continue;
                }
                var origin = new RmapSpecialWorldPoint(patch.MinTileX + 4 + rng.NextInt(availableWidth + 1),
                    patch.MinTileY + 4 + rng.NextInt(availableHeight + 1));
                if (Intersects(used, origin, width, height))
                {
                    attempts.Add(new RmapSpecialPlacementAttempt(attempt, patch.PatchId, "REJECT_RESERVED_COLLISION"));
                    continue;
                }
                attempts.Add(new RmapSpecialPlacementAttempt(attempt, patch.PatchId, "SELECTED"));
                return new Placement(origin, new[] { patch.PatchId }, attempts);
            }
            throw new InvalidOperationException("RMAP15 bounded placement exhausted for " + token + ".");
        }

        private static SharedPlacement ChooseSharedBoundary(
            RmapWorldBiomePlan plan,
            DeterministicRngStream rng,
            ISet<RmapSpecialWorldPoint> used,
            RmapWorldBiomeReservationInput seal,
            RmapWorldBiomeReservationInput boss,
            int width,
            int height)
        {
            var candidates = plan.Boundaries.Where(value =>
                (value.Source.Biome.CanonicalId == seal.AllowedBiome.CanonicalId && value.Target.Biome.CanonicalId == boss.AllowedBiome.CanonicalId) ||
                (value.Source.Biome.CanonicalId == boss.AllowedBiome.CanonicalId && value.Target.Biome.CanonicalId == seal.AllowedBiome.CanonicalId))
                .Where(value => value.Direction == RmapWorldGraphDirection.Right || value.Direction == RmapWorldGraphDirection.Up)
                .Where(value => value.Direction == RmapWorldGraphDirection.Right
                    ? value.Source.Y == value.Source.Patch.MinMicroY + (value.Source.Patch.HeightMicroChunks / 2)
                    : value.Source.X == value.Source.Patch.MinMicroX + (value.Source.Patch.WidthMicroChunks / 2))
                .OrderBy(value => value.BoundaryId, StringComparer.Ordinal).ToArray();
            if (candidates.Length == 0) throw new InvalidOperationException("RMAP15 has no Seal/Boss boundary candidate.");
            var attempts = new List<RmapSpecialPlacementAttempt>();
            for (int attempt = 1; attempt <= MaxPlacementAttempts; attempt++)
            {
                RmapWorldBiomeBoundary boundary = candidates[rng.NextInt(candidates.Length)];
                bool sealIsSource = boundary.Source.Biome.CanonicalId == seal.AllowedBiome.CanonicalId;
                RmapSpecialWorldPoint origin;
                RmapSpecialWorldPoint sealLocal;
                RmapSpecialWorldPoint bossLocal;
                if (boundary.Direction == RmapWorldGraphDirection.Right)
                {
                    int boundaryX = (boundary.Source.X + 1) * RmapWorldBiomePlanner.MicroChunkWidthTiles;
                    origin = new RmapSpecialWorldPoint(boundaryX - (width / 2), boundary.Source.Y * RmapWorldBiomePlanner.MicroChunkHeightTiles - 5);
                    sealLocal = new RmapSpecialWorldPoint(sealIsSource ? width / 4 : (width * 3) / 4, 2);
                    bossLocal = new RmapSpecialWorldPoint(sealIsSource ? (width * 3) / 4 : width / 4, 2);
                }
                else
                {
                    int boundaryY = (boundary.Source.Y + 1) * RmapWorldBiomePlanner.MicroChunkHeightTiles;
                    origin = new RmapSpecialWorldPoint(boundary.Source.X * RmapWorldBiomePlanner.MicroChunkWidthTiles - 7,
                        boundaryY - (height / 2));
                    sealLocal = new RmapSpecialWorldPoint(width / 4, sealIsSource ? height / 4 : (height * 3) / 4);
                    bossLocal = new RmapSpecialWorldPoint((width * 3) / 4, sealIsSource ? (height * 3) / 4 : height / 4);
                }
                if (!FitsWorld(origin, width, height) || Intersects(used, origin, width, height))
                {
                    attempts.Add(new RmapSpecialPlacementAttempt(attempt, boundary.BoundaryId, "REJECT_RESERVED_COLLISION"));
                    continue;
                }
                string[] owned = FootprintPatchIds(plan, origin, width, height);
                string[] allowed = seal.CandidatePatches.Concat(boss.CandidatePatches).Select(value => value.PatchId)
                    .Distinct(StringComparer.Ordinal).ToArray();
                if (owned.Any(value => !allowed.Contains(value, StringComparer.Ordinal)) || owned.Length != 2)
                {
                    attempts.Add(new RmapSpecialPlacementAttempt(attempt, boundary.BoundaryId, "REJECT_PATCH_CONTRACT"));
                    continue;
                }
                attempts.Add(new RmapSpecialPlacementAttempt(attempt, boundary.BoundaryId, "SELECTED_SHARED_BOUNDARY"));
                return new SharedPlacement(new Placement(origin, owned, attempts), sealLocal, bossLocal);
            }
            throw new InvalidOperationException("RMAP15 bounded shared Seal/Boss placement exhausted.");
        }

        private static RmapPatternBaseCell[,] BuildShell(
            int width,
            int height,
            IEnumerable<SlotDraft> slots,
            IEnumerable<PortDraft> ports)
        {
            var portCells = new HashSet<RmapSpecialWorldPoint>((ports ?? Array.Empty<PortDraft>()).SelectMany(value => value.LocalCells));
            var result = new RmapPatternBaseCell[width, height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                bool edgeWall = (x == 0 || x == width - 1) && !portCells.Contains(new RmapSpecialWorldPoint(x, y));
                result[x, y] = y <= 1 || edgeWall ? RmapPatternBaseCell.Solid : RmapPatternBaseCell.Air;
            }
            // A fixed one-way bridge is a typed terrain input, not a material-name
            // hint. It remains protected, uses only its upper collision face, and
            // is never advertised as a Grab support.
            for (int x = (width / 2) - 1; x <= (width / 2) + 1; x++)
                result[x, height / 2] = RmapPatternBaseCell.OneWayPlatform;
            foreach (SlotDraft slot in slots ?? Array.Empty<SlotDraft>())
            {
                if (slot.X < 2 || slot.X >= width - 2 || slot.Y < 2 || slot.Y >= height - 2)
                    throw new ArgumentOutOfRangeException(nameof(slots), "Slot must fit inside the fixed shell.");
                if (slot.Y > 2)
                for (int x = slot.X - 1; x <= slot.X + 1; x++) result[x, slot.Y - 1] = RmapPatternBaseCell.OneWayPlatform;
                result[slot.X, slot.Y] = RmapPatternBaseCell.Air;
                result[slot.X, slot.Y + 1] = RmapPatternBaseCell.Air;
            }
            return result;
        }

        private static List<PortDraft> StandardPorts(
            int width,
            int height,
            RmapSpecialAccessFlow entry,
            RmapSpecialAccessFlow? exit,
            string condition,
            string sourceNodeId)
        {
            int middle = height / 2;
            var result = new List<PortDraft>
            {
                new PortDraft("ENTRY", RmapWorldGraphDirection.Left, entry, condition, sourceNodeId,
                    PortCells(0, middle)),
            };
            if (exit.HasValue)
                result.Add(new PortDraft("EXIT", RmapWorldGraphDirection.Right, exit.Value, condition, sourceNodeId,
                    PortCells(width - 1, middle)));
            return result;
        }

        private static IEnumerable<RmapSpecialWorldPoint> PortCells(int x, int middle) =>
            Enumerable.Range(middle - 1, 3).Select(y => new RmapSpecialWorldPoint(x, y));

        private static RmapWorldBiomeReservationInput InputFor(RmapWorldBiomePlan plan, RmapWorldGraphRole role) =>
            plan.ReservationInputs.Single(value => value.Role == role);

        private static bool IsGraphSlot(RmapWorldGraphRole role, RmapSpecialSlotKind kind)
        {
            switch (role)
            {
                case RmapWorldGraphRole.Start: return kind == RmapSpecialSlotKind.Spawn;
                case RmapWorldGraphRole.MooncoreOre:
                case RmapWorldGraphRole.CondensedCoefficientSap:
                case RmapWorldGraphRole.DeepStarYeast: return kind == RmapSpecialSlotKind.Resource;
                case RmapWorldGraphRole.Forge: return kind == RmapSpecialSlotKind.Forge;
                case RmapWorldGraphRole.Exit: return kind == RmapSpecialSlotKind.Exit;
                default: return false;
            }
        }

        private static RmapWorldStableIdKind StableKind(RmapSpecialSlotKind kind)
        {
            switch (kind)
            {
                case RmapSpecialSlotKind.Resource: return RmapWorldStableIdKind.RewardChest;
                case RmapSpecialSlotKind.Npc:
                case RmapSpecialSlotKind.Shop: return RmapWorldStableIdKind.NPCShopSlot;
                case RmapSpecialSlotKind.Boss: return RmapWorldStableIdKind.MonsterSpawnSlot;
                default: return RmapWorldStableIdKind.SpecialRegionTrigger;
            }
        }

        private static bool FitsWorld(RmapSpecialWorldPoint origin, int width, int height) =>
            IsInWorld(origin.X, origin.Y) && IsInWorld(origin.X + width - 1, origin.Y + height - 1);

        private static bool Intersects(ISet<RmapSpecialWorldPoint> used, RmapSpecialWorldPoint origin, int width, int height)
        {
            for (int y = origin.Y; y < origin.Y + height; y++)
            for (int x = origin.X; x < origin.X + width; x++)
                if (used.Contains(new RmapSpecialWorldPoint(x, y))) return true;
            return false;
        }

        private static string[] FootprintPatchIds(RmapWorldBiomePlan plan, RmapSpecialWorldPoint origin, int width, int height) =>
            Enumerable.Range(origin.Y, height).SelectMany(y => Enumerable.Range(origin.X, width).Select(x =>
                plan.GetCell(x / RmapWorldBiomePlanner.MicroChunkWidthTiles,
                    y / RmapWorldBiomePlanner.MicroChunkHeightTiles).PatchId)).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();

        private sealed class Placement
        {
            public Placement(RmapSpecialWorldPoint origin, IEnumerable<string> occupiedPatchIds,
                IEnumerable<RmapSpecialPlacementAttempt> attempts)
            {
                Origin = origin;
                OccupiedPatchIds = (occupiedPatchIds ?? Array.Empty<string>()).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray();
                Attempts = (attempts ?? Array.Empty<RmapSpecialPlacementAttempt>()).ToArray();
            }
            public RmapSpecialWorldPoint Origin { get; }
            public IReadOnlyList<string> OccupiedPatchIds { get; }
            public IReadOnlyList<RmapSpecialPlacementAttempt> Attempts { get; }
        }

        private sealed class SharedPlacement
        {
            public SharedPlacement(Placement placement, RmapSpecialWorldPoint sealLocal, RmapSpecialWorldPoint bossLocal)
            {
                Placement = placement;
                SealLocal = sealLocal;
                BossLocal = bossLocal;
            }
            public Placement Placement { get; }
            public RmapSpecialWorldPoint SealLocal { get; }
            public RmapSpecialWorldPoint BossLocal { get; }
        }

        private sealed class SlotDraft
        {
            public SlotDraft(string id, RmapSpecialSlotKind kind, int x, int y)
            {
                Id = id;
                Kind = kind;
                X = x;
                Y = y;
            }
            public string Id { get; }
            public RmapSpecialSlotKind Kind { get; }
            public int X { get; }
            public int Y { get; }
        }

        private sealed class PortDraft
        {
            public PortDraft(string id, RmapWorldGraphDirection side, RmapSpecialAccessFlow flow,
                string condition, string sourceNodeId, IEnumerable<RmapSpecialWorldPoint> localCells)
            {
                Id = id;
                Side = side;
                Flow = flow;
                Condition = condition;
                SourceNodeId = sourceNodeId ?? string.Empty;
                LocalCells = (localCells ?? Array.Empty<RmapSpecialWorldPoint>()).ToArray();
            }
            public string Id { get; }
            public RmapWorldGraphDirection Side { get; }
            public RmapSpecialAccessFlow Flow { get; }
            public string Condition { get; }
            public string SourceNodeId { get; }
            public IReadOnlyList<RmapSpecialWorldPoint> LocalCells { get; }
        }
    }

    public static class RmapSpecialReservationExport
    {
        public static string ManifestJson(RmapSpecialReservationPlan plan)
        {
            RequirePlan(plan);
            return "{\n" +
                "  \"format\": \"RMAP15_SPECIAL_RESERVATION_V1\",\n" +
                "  \"seed\": " + plan.BiomePlan.Definition.Request.Seed.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"definition_digest\": \"" + plan.BiomePlan.Definition.Digest + "\",\n" +
                "  \"rmap14_biome_digest\": \"" + plan.BiomePlan.Digest + "\",\n" +
                "  \"special_reservation_initial_state\": \"" + plan.SpecialReservationInitialState.ToString("x16", CultureInfo.InvariantCulture) + "\",\n" +
                "  \"site_count\": " + plan.Sites.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"fixed_cell_count\": " + plan.Cells.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"graph_binding_count\": " + plan.GraphBindings.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"consumer\": \"RmapSpecialReservationPlan.EvaluateTerrainCells\",\n" +
                "  \"full_world_bake\": \"PENDING_RMAP17\",\n" +
                "  \"digest\": \"" + plan.Digest + "\"\n}" + "\n";
        }

        public static string SitesCsv(RmapSpecialReservationPlan plan) => Lines(
            "site_id,role,stable_id,origin_x,origin_y,width_tiles,height_tiles,template_id,template_version,transform,eligible_patch_ids,occupied_patch_ids,attempts",
            plan.Sites.Select(value => Row(value.Id, value.Role, value.StableId.Value, value.Origin.X, value.Origin.Y,
                value.WidthTiles, value.HeightTiles, value.TemplateId, value.TemplateVersion, value.Transform,
                string.Join("|", value.EligiblePatchIds), string.Join("|", value.OccupiedPatchIds),
                string.Join("|", value.Attempts.Select(attempt => attempt.Ordinal + ":" + attempt.CandidateId + ":" + attempt.Outcome)))));

        public static string GraphBindingsCsv(RmapSpecialReservationPlan plan) => Lines(
            "reservation_id,node_id,role,release_condition,site_id,local_x,local_y,world_x,world_y",
            plan.GraphBindings.Select(value => Row(value.ReservationId, value.NodeId, value.Role,
                value.Reservation.ReleaseCondition, value.SiteId, value.LocalPoint.X, value.LocalPoint.Y,
                value.WorldPoint.X, value.WorldPoint.Y)));

        public static string FixedCellsCsv(RmapSpecialReservationPlan plan) => Lines(
            "site_id,local_x,local_y,world_x,world_y,patch_id,base,protection,collision_meaning",
            plan.Cells.Select(value => Row(value.SiteId, value.LocalX, value.LocalY, value.World.X, value.World.Y,
                value.PatchId, Token(value.BaseCell), value.Protection, CollisionMeaning(value.BaseCell))));

        public static string AccessCsv(RmapSpecialReservationPlan plan) => Lines(
            "access_id,site_id,side,flow,required,condition,source_node_id,world_x,world_y",
            plan.Accesses.SelectMany(access => access.OpenCells.Select(point => Row(access.Id, access.SiteId,
                access.Side, access.Flow, access.Required, access.Condition, access.SourceNodeId, point.X, point.Y))));

        public static string SlotsCsv(RmapSpecialReservationPlan plan) => Lines(
            "slot_id,site_id,kind,stable_id,local_x,local_y,world_x,world_y,occupancy_width,occupancy_height",
            plan.Slots.Select(value => Row(value.Id, value.SiteId, value.Kind, value.StableId.Value, value.Local.X,
                value.Local.Y, value.World.X, value.World.Y, value.OccupancyWidthTiles, value.OccupancyHeightTiles)));

        public static string OwnershipCsv(RmapSpecialReservationPlan plan) => Lines(
            "site_id,reserved_cells,fixed_solid_cells,protected_air_cells,occupied_patch_ids,terrain_intrusion_policy",
            plan.Sites.Select(site =>
            {
                RmapSpecialWorldCell[] cells = plan.Cells.Where(value => value.SiteId == site.Id).ToArray();
                return Row(site.Id, cells.Length, cells.Count(value => value.Protection == RmapSpecialProtectionKind.FixedSolid),
                    cells.Count(value => value.Protection == RmapSpecialProtectionKind.ProtectedAir),
                    string.Join("|", site.OccupiedPatchIds), "REJECT_EVALUATE_TERRAIN_CELLS");
            }));

        public static string StateGeometryCsv(RmapSpecialReservationPlan plan) => Lines(
            "site_id,state,world_x,world_y,base,reason",
            plan.StateGeometry.Select(value => Row(value.SiteId, value.State, value.World.X, value.World.Y,
                Token(value.BaseCell), value.Reason)));

        private static void RequirePlan(RmapSpecialReservationPlan plan)
        {
            if (plan == null || !plan.Success) throw new ArgumentException("A passing RMAP15 plan is required.", nameof(plan));
        }

        private static string Lines(string header, IEnumerable<string> rows) => header + "\n" +
            string.Join("\n", rows ?? Array.Empty<string>()) + "\n";

        private static string Row(params object[] values) => string.Join(",", (values ?? Array.Empty<object>()).Select(Csv));

        private static string Csv(object value)
        {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        private static string Token(RmapPatternBaseCell value)
        {
            switch (value)
            {
                case RmapPatternBaseCell.Air: return "A";
                case RmapPatternBaseCell.Solid: return "S";
                case RmapPatternBaseCell.OneWayPlatform: return "O";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private static string CollisionMeaning(RmapPatternBaseCell value) => value == RmapPatternBaseCell.Air
            ? "PROTECTED_AIR_NO_GENERAL_FILL" : value == RmapPatternBaseCell.Solid
            ? "FIXED_SOLID_COLLIDER" : "FIXED_ONE_WAY_TOP_COLLIDER_NO_GRAB";
    }

    internal static class RmapSpecialReservationIdentity
    {
        public static string Hash(IEnumerable<string> values) => RmapWorldDefinition.Hash(string.Join("\n", values ?? Array.Empty<string>()));
        public static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A stable value is required.", name);
            return value.Trim();
        }
    }
}
