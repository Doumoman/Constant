using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public readonly struct VillageLayoutId : IEquatable<VillageLayoutId>, IComparable<VillageLayoutId>
    {
        private readonly string value;

        public VillageLayoutId(string value) { this.value = value; }
        public string Value => value ?? string.Empty;
        public int CompareTo(VillageLayoutId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(VillageLayoutId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is VillageLayoutId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(VillageLayoutId left, VillageLayoutId right) => left.Equals(right);
        public static bool operator !=(VillageLayoutId left, VillageLayoutId right) => !left.Equals(right);
    }

    public enum VillageLayoutShape
    {
        OneByOne = 1,
        TwoByOne = 2,
        OneByTwo = 3,
    }

    public enum VillageFacilityKind
    {
        Kitchen = 1,
        Repair = 2,
        Optional = 3,
    }

    public enum VillageFacilityRequirement
    {
        Required = 1,
        Optional = 2,
    }

    public sealed class VillageRoadCell
    {
        public VillageRoadCell(int order, LocalTileCoord regionTile)
            : this(order, regionTile, default(SpecialRegionPlacedCoordinate), false) { }

        internal VillageRoadCell(
            int order,
            LocalTileCoord regionTile,
            SpecialRegionPlacedCoordinate placed,
            bool hasProjection)
        {
            Order = order;
            RegionTile = regionTile;
            Placed = placed;
            HasProjection = hasProjection;
        }

        public int Order { get; }
        public LocalTileCoord RegionTile { get; }
        public SpecialRegionPlacedCoordinate Placed { get; }
        public bool HasProjection { get; }
        public SectorCoord WorldSector => Placed.WorldSector;
        public LocalTileCoord LocalTile => Placed.LocalTile;
    }

    public sealed class VillageFacilityDefinition
    {
        public VillageFacilityDefinition(
            string definitionId,
            VillageFacilityKind kind,
            VillageFacilityRequirement requirement,
            SpecialRegionSlotId slotId,
            string occupantId,
            LocalTileCoord doorRegionTile,
            string displayText = null)
        {
            DefinitionId = definitionId ?? string.Empty;
            Kind = kind;
            Requirement = requirement;
            SlotId = slotId;
            OccupantId = occupantId ?? string.Empty;
            DoorRegionTile = doorRegionTile;
            DisplayText = displayText ?? string.Empty;
        }

        public string DefinitionId { get; }
        public VillageFacilityKind Kind { get; }
        public VillageFacilityRequirement Requirement { get; }
        public SpecialRegionSlotId SlotId { get; }
        public string OccupantId { get; }
        public LocalTileCoord DoorRegionTile { get; }
        public string DisplayText { get; }
        public bool IsExplicitlyEmpty => OccupantId.Length == 0;
    }

    public sealed class VillageFacilityAccessWitness
    {
        private readonly ReadOnlyCollection<LocalTileCoord> cells;

        public VillageFacilityAccessWitness(
            string witnessId,
            string facilityDefinitionId,
            IEnumerable<LocalTileCoord> cells)
        {
            WitnessId = witnessId ?? string.Empty;
            FacilityDefinitionId = facilityDefinitionId ?? string.Empty;
            this.cells = new ReadOnlyCollection<LocalTileCoord>(
                (cells ?? Array.Empty<LocalTileCoord>()).ToArray());
        }

        public string WitnessId { get; }
        public string FacilityDefinitionId { get; }
        public IReadOnlyList<LocalTileCoord> Cells => cells;
    }

    public sealed class VillageShellDefinition
    {
        private readonly ReadOnlyCollection<VillageRoadCell> roadCells;
        private readonly ReadOnlyCollection<VillageFacilityDefinition> facilities;
        private readonly ReadOnlyCollection<VillageFacilityAccessWitness> accessWitnesses;

        public VillageShellDefinition(
            VillageLayoutId layoutId,
            VillageLayoutShape shape,
            IEnumerable<VillageRoadCell> roadCells,
            IEnumerable<VillageFacilityDefinition> facilities,
            IEnumerable<VillageFacilityAccessWitness> accessWitnesses,
            string displayText = null)
        {
            LayoutId = layoutId;
            Shape = shape;
            this.roadCells = Freeze(roadCells);
            this.facilities = Freeze(facilities);
            this.accessWitnesses = Freeze(accessWitnesses);
            DisplayText = displayText ?? string.Empty;
        }

        public VillageLayoutId LayoutId { get; }
        public VillageLayoutShape Shape { get; }
        public IReadOnlyList<VillageRoadCell> RoadCells => roadCells;
        public IReadOnlyList<VillageFacilityDefinition> Facilities => facilities;
        public IReadOnlyList<VillageFacilityAccessWitness> AccessWitnesses => accessWitnesses;
        public string DisplayText { get; }

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source)
            => new ReadOnlyCollection<T>((source ?? Array.Empty<T>()).ToArray());
    }

    public sealed class VillageRoadAccess
    {
        private readonly ReadOnlyCollection<VillageRoadCell> forward;
        private readonly ReadOnlyCollection<VillageRoadCell> reverse;

        internal VillageRoadAccess(IEnumerable<VillageRoadCell> canonicalRoad)
        {
            var values = canonicalRoad.ToArray();
            forward = new ReadOnlyCollection<VillageRoadCell>(values);
            reverse = new ReadOnlyCollection<VillageRoadCell>(values.Reverse().ToArray());
        }

        public IReadOnlyList<VillageRoadCell> Forward => forward;
        public IReadOnlyList<VillageRoadCell> Reverse => reverse;
        public AccessClass AccessClass => AccessClass.MandatoryNoTool;
        public bool IsBidirectional => forward.Count > 0 && forward.Count == reverse.Count;
        public int ToolRequirementCount => 0;
        public int SyntheticEdgeCount => 0;
        public int TeleportCount => 0;
        public int CarveCount => 0;
        public bool ClaimsRuntimePhysics => false;
    }

    public sealed class VillageFacilityBinding
    {
        private readonly ReadOnlyCollection<SpecialRegionPlacedCoordinate> accessCells;
        private readonly ReadOnlyCollection<SpecialRegionPlacedCoordinate> reverseAccessCells;

        internal VillageFacilityBinding(
            VillageFacilityDefinition definition,
            SpecialRegionReplaceableSlotBinding slot,
            SpecialRegionPlacedCoordinate door,
            VillageFacilityAccessWitness witness,
            IEnumerable<SpecialRegionPlacedCoordinate> accessCells)
        {
            Definition = definition;
            Slot = slot;
            Door = door;
            Witness = witness;
            var values = accessCells.ToArray();
            this.accessCells = new ReadOnlyCollection<SpecialRegionPlacedCoordinate>(values);
            reverseAccessCells = new ReadOnlyCollection<SpecialRegionPlacedCoordinate>(values.Reverse().ToArray());
        }

        public VillageFacilityDefinition Definition { get; }
        public SpecialRegionReplaceableSlotBinding Slot { get; }
        public SpecialRegionPlacedCoordinate Door { get; }
        public VillageFacilityAccessWitness Witness { get; }
        public IReadOnlyList<SpecialRegionPlacedCoordinate> AccessCells => accessCells;
        public IReadOnlyList<SpecialRegionPlacedCoordinate> ReverseAccessCells => reverseAccessCells;
        public AccessClass AccessClass => AccessClass.MandatoryNoTool;
        public bool IsAssigned => Slot.IsAssigned;
        public bool IsExplicitlyEmpty => !Slot.IsAssigned;
        public bool IsMarkerOnly => true;
        public bool OwnsSolid => false;
        public bool OwnsCollision => false;
        public bool OwnsRoute => false;
        public bool OwnsAccess => false;
        public bool OwnsPersistence => false;
        public int ToolRequirementCount => 0;
        public int SyntheticEdgeCount => 0;
        public int TeleportCount => 0;
        public int CarveCount => 0;
        public bool ClaimsRuntimePhysics => false;
    }

    public sealed class VillageShellPlan
    {
        private readonly ReadOnlyCollection<VillageRoadCell> roadCells;
        private readonly ReadOnlyCollection<VillageFacilityBinding> facilityBindings;

        internal VillageShellPlan(
            SpecialRegionSiteBridge bridge,
            SpecialRegionEntryBufferPlan entryBuffer,
            SpecialRegionFixedSlotLayerPlan fixedSlots,
            VillageShellDefinition definition,
            IEnumerable<VillageRoadCell> roadCells,
            IEnumerable<VillageFacilityBinding> facilityBindings)
        {
            RegionId = bridge.RegionId;
            ReservationId = bridge.ReservationId;
            LayoutId = definition.LayoutId;
            Shape = definition.Shape;
            WidthTiles = bridge.Width * WorldGenConstants.SectorWidthTiles;
            HeightTiles = bridge.Height * WorldGenConstants.SectorHeightTiles;
            BridgeDigest = bridge.CanonicalDigest;
            EntryBufferDigest = entryBuffer.CanonicalDigest;
            FixedSlotDigest = fixedSlots.CanonicalDigest;
            this.roadCells = new ReadOnlyCollection<VillageRoadCell>(
                roadCells.OrderBy(value => value.Order).ToArray());
            this.facilityBindings = new ReadOnlyCollection<VillageFacilityBinding>(
                facilityBindings.OrderBy(value => value.Definition.Kind)
                    .ThenBy(value => value.Definition.DefinitionId, StringComparer.Ordinal).ToArray());
            RoadAccess = new VillageRoadAccess(this.roadCells);
            RoadDigest = VillageShellCanonicalDigest.ComputeRoad(this.roadCells);
            FacilityDigest = VillageShellCanonicalDigest.ComputeFacilities(this.facilityBindings);
            AccessDigest = VillageShellCanonicalDigest.ComputeAccess(this.facilityBindings);
            CanonicalDigest = VillageShellCanonicalDigest.Compute(this);
        }

        public SpecialRegionId RegionId { get; }
        public SiteReservationId ReservationId { get; }
        public VillageLayoutId LayoutId { get; }
        public VillageLayoutShape Shape { get; }
        public int WidthTiles { get; }
        public int HeightTiles { get; }
        public IReadOnlyList<VillageRoadCell> RoadCells => roadCells;
        public VillageRoadAccess RoadAccess { get; }
        public IReadOnlyList<VillageFacilityBinding> FacilityBindings => facilityBindings;
        public string BridgeDigest { get; }
        public string EntryBufferDigest { get; }
        public string FixedSlotDigest { get; }
        public string RoadDigest { get; }
        public string FacilityDigest { get; }
        public string AccessDigest { get; }
        public string CanonicalDigest { get; }
        public int PlacementWriteCount => 0;
        public int SpawnCount => 0;
        public int DespawnCount => 0;
        public int TileMutationCount => 0;
        public int RandomSelectionCount => 0;
        public int WorldMutationCount => 0;
    }

    public sealed class VillageShellCompileRequest
    {
        public VillageShellCompileRequest(
            SpecialRegionSiteBridge bridge,
            string expectedBridgeDigest,
            SpecialRegionEntryBufferPlan entryBufferPlan,
            string expectedEntryBufferDigest,
            SpecialRegionFixedSlotLayerPlan fixedSlotPlan,
            string expectedFixedSlotDigest,
            VillageShellDefinition definition)
        {
            Bridge = bridge;
            ExpectedBridgeDigest = expectedBridgeDigest ?? string.Empty;
            EntryBufferPlan = entryBufferPlan;
            ExpectedEntryBufferDigest = expectedEntryBufferDigest ?? string.Empty;
            FixedSlotPlan = fixedSlotPlan;
            ExpectedFixedSlotDigest = expectedFixedSlotDigest ?? string.Empty;
            Definition = definition;
        }

        public SpecialRegionSiteBridge Bridge { get; }
        public string ExpectedBridgeDigest { get; }
        public SpecialRegionEntryBufferPlan EntryBufferPlan { get; }
        public string ExpectedEntryBufferDigest { get; }
        public SpecialRegionFixedSlotLayerPlan FixedSlotPlan { get; }
        public string ExpectedFixedSlotDigest { get; }
        public VillageShellDefinition Definition { get; }
    }

    public enum VillageShellErrorCode
    {
        MissingInput = 1,
        DigestMismatch = 2,
        NotVillage = 3,
        UnsupportedShape = 4,
        ShapeMismatch = 5,
        CoordinateOutOfRange = 6,
        InvalidRoad = 7,
        DisconnectedRoad = 8,
        MissingApronConnection = 9,
        MissingSectorCoverage = 10,
        MissingSeamCrossing = 11,
        RoadCollision = 12,
        MissingKitchen = 13,
        MissingRepair = 14,
        InvalidOptionalCount = 15,
        DuplicateFacility = 16,
        FacilitySlotMismatch = 17,
        RequiredFacilityClear = 18,
        InvalidDoor = 19,
        InvalidAccessWitness = 20,
        FacilityCannotReturnToRoad = 21,
        NonCanonicalPublication = 22,
    }

    public sealed class VillageShellError : IComparable<VillageShellError>, IEquatable<VillageShellError>
    {
        public VillageShellError(VillageShellErrorCode code, string path, string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public VillageShellErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(VillageShellError other)
        {
            if (other == null) return 1;
            var value = Code.CompareTo(other.Code);
            if (value != 0) return value;
            value = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return value != 0 ? value : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(VillageShellError other)
            => other != null && Code == other.Code &&
               string.Equals(Path, other.Path, StringComparison.Ordinal) &&
               string.Equals(Detail, other.Detail, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as VillageShellError);
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Code;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Path);
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
            }
        }

        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }

    public sealed class VillageShellResult
    {
        private readonly ReadOnlyCollection<VillageShellError> errors;

        internal VillageShellResult(VillageShellPlan plan, IEnumerable<VillageShellError> errors)
        {
            Plan = plan;
            this.errors = new ReadOnlyCollection<VillageShellError>(
                (errors ?? Array.Empty<VillageShellError>()).Distinct().OrderBy(value => value).ToArray());
            CanonicalDigest = plan == null ? string.Empty : plan.CanonicalDigest;
        }

        public bool Success => Plan != null && errors.Count == 0;
        public VillageShellPlan Plan { get; }
        public IReadOnlyList<VillageShellError> Errors => errors;
        public string CanonicalDigest { get; }
    }

    public static class VillageShellCanonicalDigest
    {
        public static string Compute(VillageShellPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var value = new StringBuilder();
            Append(value, "region", plan.RegionId.Value);
            Append(value, "reservation", plan.ReservationId.Value);
            Append(value, "layout", plan.LayoutId.Value);
            Append(value, "shape", Number((int)plan.Shape));
            Append(value, "bounds", Coordinate(plan.WidthTiles, plan.HeightTiles));
            Append(value, "bridge", plan.BridgeDigest);
            Append(value, "entry", plan.EntryBufferDigest);
            Append(value, "fixedSlots", plan.FixedSlotDigest);
            Append(value, "road", plan.RoadDigest);
            Append(value, "facilities", plan.FacilityDigest);
            Append(value, "access", plan.AccessDigest);
            return Sha256(value.ToString());
        }

        public static string ComputeRoad(IEnumerable<VillageRoadCell> roadCells)
        {
            if (roadCells == null) throw new ArgumentNullException(nameof(roadCells));
            var value = new StringBuilder();
            foreach (var cell in roadCells.Where(item => item != null).OrderBy(item => item.Order))
                Append(value, "road", Number(cell.Order) + "/" + Tile(cell.RegionTile) + "/" + Placed(cell.Placed));
            return Sha256(value.ToString());
        }

        public static string ComputeFacilities(IEnumerable<VillageFacilityBinding> bindings)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            var value = new StringBuilder();
            foreach (var binding in bindings.Where(item => item != null)
                         .OrderBy(item => item.Definition.Kind)
                         .ThenBy(item => item.Definition.DefinitionId, StringComparer.Ordinal))
                Append(value, "facility", binding.Definition.DefinitionId + "/" +
                    Number((int)binding.Definition.Kind) + "/" + Number((int)binding.Definition.Requirement) + "/" +
                    binding.Slot.SlotId.Value + "/" + Number((int)binding.Slot.Operation) + "/" +
                    binding.Slot.OccupantId + "/" + Placed(binding.Slot.Placed) + "/" + Placed(binding.Door));
            return Sha256(value.ToString());
        }

        public static string ComputeAccess(IEnumerable<VillageFacilityBinding> bindings)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            var value = new StringBuilder();
            foreach (var binding in bindings.Where(item => item != null)
                         .OrderBy(item => item.Definition.Kind)
                         .ThenBy(item => item.Definition.DefinitionId, StringComparer.Ordinal))
            {
                Append(value, "witness", binding.Witness.WitnessId + "/" +
                    binding.Witness.FacilityDefinitionId + "/" + Number((int)binding.AccessClass));
                foreach (var cell in binding.AccessCells) Append(value, "accessCell", Placed(cell));
                foreach (var cell in binding.ReverseAccessCells) Append(value, "returnCell", Placed(cell));
            }
            return Sha256(value.ToString());
        }

        private static string Placed(SpecialRegionPlacedCoordinate value)
            => Coordinate(value.SectorOffset.X, value.SectorOffset.Y) + "/" +
               Coordinate(value.WorldSector.X, value.WorldSector.Y) + "/" +
               Tile(value.LocalTile) + "/" + Tile(value.RegionTile);

        private static string Tile(LocalTileCoord value) => Coordinate(value.X, value.Y);
        private static string Coordinate(int x, int y) => Number(x) + "," + Number(y);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static void Append(StringBuilder value, string name, string field)
            => value.Append(name).Append('=').Append(field ?? string.Empty).Append('\n');

        private static string Sha256(string material)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(new UTF8Encoding(false).GetBytes(material))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
