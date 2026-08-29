using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public enum VillageStateKind
    {
        Normal = 1,
        Friendly = 2,
        IndividualHostile = 3,
        AllHostile = 4,
        Evacuation = 5,
    }

    public enum VillageNpcMarkerState
    {
        Normal = 1,
        Friendly = 2,
        Hostile = 3,
        Evacuated = 4,
    }

    public enum VillageInventoryMarkerState
    {
        Standard = 1,
        FriendlyAccess = 2,
        Unavailable = 3,
        Evacuated = 4,
    }

    public enum VillageDoorMarkerState
    {
        Standard = 1,
        Welcome = 2,
        Alert = 3,
        Evacuated = 4,
    }

    public sealed class VillageNpcMarkerDefinition
    {
        public VillageNpcMarkerDefinition(string markerId, string facilityBindingId, string displayText = null)
        {
            MarkerId = markerId ?? string.Empty;
            FacilityBindingId = facilityBindingId ?? string.Empty;
            DisplayText = displayText ?? string.Empty;
        }

        public string MarkerId { get; }
        public string FacilityBindingId { get; }
        public string DisplayText { get; }
    }

    public sealed class VillageInventoryMarkerDefinition
    {
        public VillageInventoryMarkerDefinition(string markerId, string facilityBindingId, string displayText = null)
        {
            MarkerId = markerId ?? string.Empty;
            FacilityBindingId = facilityBindingId ?? string.Empty;
            DisplayText = displayText ?? string.Empty;
        }

        public string MarkerId { get; }
        public string FacilityBindingId { get; }
        public string DisplayText { get; }
    }

    public sealed class VillageDoorMarkerDefinition
    {
        public VillageDoorMarkerDefinition(
            string markerId,
            string facilityBindingId,
            LocalTileCoord sourceDoorRegionTile,
            string displayText = null)
        {
            MarkerId = markerId ?? string.Empty;
            FacilityBindingId = facilityBindingId ?? string.Empty;
            SourceDoorRegionTile = sourceDoorRegionTile;
            DisplayText = displayText ?? string.Empty;
        }

        public string MarkerId { get; }
        public string FacilityBindingId { get; }
        public LocalTileCoord SourceDoorRegionTile { get; }
        public string DisplayText { get; }
    }

    public sealed class VillageStateMarkerSetDefinition
    {
        private readonly ReadOnlyCollection<VillageNpcMarkerDefinition> npcMarkers;
        private readonly ReadOnlyCollection<VillageInventoryMarkerDefinition> inventoryMarkers;
        private readonly ReadOnlyCollection<VillageDoorMarkerDefinition> doorMarkers;
        private readonly ReadOnlyCollection<VillageStateKind> requestedVariants;

        public VillageStateMarkerSetDefinition(
            IEnumerable<VillageNpcMarkerDefinition> npcMarkers,
            IEnumerable<VillageInventoryMarkerDefinition> inventoryMarkers,
            IEnumerable<VillageDoorMarkerDefinition> doorMarkers,
            string individualHostileTargetMarkerId,
            IEnumerable<VillageStateKind> requestedVariants = null,
            string displayText = null)
        {
            this.npcMarkers = Freeze(npcMarkers);
            this.inventoryMarkers = Freeze(inventoryMarkers);
            this.doorMarkers = Freeze(doorMarkers);
            IndividualHostileTargetMarkerId = individualHostileTargetMarkerId ?? string.Empty;
            this.requestedVariants = new ReadOnlyCollection<VillageStateKind>(
                (requestedVariants ?? CanonicalVariants()).ToArray());
            DisplayText = displayText ?? string.Empty;
        }

        public IReadOnlyList<VillageNpcMarkerDefinition> NpcMarkers => npcMarkers;
        public IReadOnlyList<VillageInventoryMarkerDefinition> InventoryMarkers => inventoryMarkers;
        public IReadOnlyList<VillageDoorMarkerDefinition> DoorMarkers => doorMarkers;
        public string IndividualHostileTargetMarkerId { get; }
        public IReadOnlyList<VillageStateKind> RequestedVariants => requestedVariants;
        public string DisplayText { get; }

        internal static VillageStateKind[] CanonicalVariants()
            => new[]
            {
                VillageStateKind.Normal,
                VillageStateKind.Friendly,
                VillageStateKind.IndividualHostile,
                VillageStateKind.AllHostile,
                VillageStateKind.Evacuation,
            };

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> values)
            => new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).ToArray());
    }

    public sealed class VillageNpcMarkerSnapshot
    {
        internal VillageNpcMarkerSnapshot(
            VillageNpcMarkerDefinition definition,
            SpecialRegionPlacedCoordinate sourceCoordinate,
            VillageNpcMarkerState state)
        {
            MarkerId = definition.MarkerId;
            FacilityBindingId = definition.FacilityBindingId;
            SourceCoordinate = sourceCoordinate;
            State = state;
        }

        public string MarkerId { get; }
        public string FacilityBindingId { get; }
        public SpecialRegionPlacedCoordinate SourceCoordinate { get; }
        public VillageNpcMarkerState State { get; }
        public bool OwnsSpawn => false;
        public bool OwnsAi => false;
        public bool OwnsFaction => false;
        public bool OwnsCombat => false;
    }

    public sealed class VillageInventoryMarkerSnapshot
    {
        internal VillageInventoryMarkerSnapshot(
            VillageInventoryMarkerDefinition definition,
            SpecialRegionPlacedCoordinate sourceCoordinate,
            VillageInventoryMarkerState state)
        {
            MarkerId = definition.MarkerId;
            FacilityBindingId = definition.FacilityBindingId;
            SourceCoordinate = sourceCoordinate;
            State = state;
        }

        public string MarkerId { get; }
        public string FacilityBindingId { get; }
        public SpecialRegionPlacedCoordinate SourceCoordinate { get; }
        public VillageInventoryMarkerState State { get; }
        public bool OwnsItems => false;
        public bool OwnsPrice => false;
        public bool OwnsStock => false;
        public bool OwnsInteractionResult => false;
    }

    public sealed class VillageDoorMarkerSnapshot
    {
        internal VillageDoorMarkerSnapshot(
            VillageDoorMarkerDefinition definition,
            SpecialRegionPlacedCoordinate sourceCoordinate,
            VillageDoorMarkerState state)
        {
            MarkerId = definition.MarkerId;
            FacilityBindingId = definition.FacilityBindingId;
            SourceCoordinate = sourceCoordinate;
            State = state;
        }

        public string MarkerId { get; }
        public string FacilityBindingId { get; }
        public SpecialRegionPlacedCoordinate SourceCoordinate { get; }
        public VillageDoorMarkerState State { get; }
        public bool OwnsCollision => false;
        public bool OwnsLock => false;
        public bool OwnsOpenClose => false;
        public bool BlocksPath => false;
        public int CollisionWriteCount => 0;
        public int LockWriteCount => 0;
        public int PathBlockingWriteCount => 0;
    }

    public sealed class VillageStateVariantSnapshot
    {
        private readonly ReadOnlyCollection<VillageNpcMarkerSnapshot> npcMarkers;
        private readonly ReadOnlyCollection<VillageInventoryMarkerSnapshot> inventoryMarkers;
        private readonly ReadOnlyCollection<VillageDoorMarkerSnapshot> doorMarkers;

        internal VillageStateVariantSnapshot(
            VillageStateKind stateKind,
            VillageShellPlan shell,
            string roadWitnessDigest,
            string facilityCoordinateDigest,
            string facilityWitnessDigest,
            string individualHostileTargetMarkerId,
            IEnumerable<VillageNpcMarkerSnapshot> npcMarkers,
            IEnumerable<VillageInventoryMarkerSnapshot> inventoryMarkers,
            IEnumerable<VillageDoorMarkerSnapshot> doorMarkers)
        {
            StateKind = stateKind;
            RegionId = shell.RegionId;
            ReservationId = shell.ReservationId;
            LayoutId = shell.LayoutId;
            Shape = shell.Shape;
            WidthTiles = shell.WidthTiles;
            HeightTiles = shell.HeightTiles;
            VillageShellDigest = shell.CanonicalDigest;
            RoadDigest = shell.RoadDigest;
            FacilityDigest = shell.FacilityDigest;
            AccessDigest = shell.AccessDigest;
            RoadWitnessDigest = roadWitnessDigest ?? string.Empty;
            FacilityCoordinateDigest = facilityCoordinateDigest ?? string.Empty;
            FacilityWitnessDigest = facilityWitnessDigest ?? string.Empty;
            RoadCellCount = shell.RoadCells.Count;
            FacilityBindingCount = shell.FacilityBindings.Count;
            IndividualHostileTargetMarkerId = individualHostileTargetMarkerId ?? string.Empty;
            this.npcMarkers = Freeze(npcMarkers, value => value.MarkerId);
            this.inventoryMarkers = Freeze(inventoryMarkers, value => value.MarkerId);
            this.doorMarkers = Freeze(doorMarkers, value => value.MarkerId);
            MarkerDigest = VillageStateVariantCanonicalDigest.ComputeMarkers(this);
            CanonicalDigest = VillageStateVariantCanonicalDigest.ComputeVariant(this);
        }

        public VillageStateKind StateKind { get; }
        public SpecialRegionId RegionId { get; }
        public SiteReservationId ReservationId { get; }
        public VillageLayoutId LayoutId { get; }
        public VillageLayoutShape Shape { get; }
        public int WidthTiles { get; }
        public int HeightTiles { get; }
        public string VillageShellDigest { get; }
        public string RoadDigest { get; }
        public string FacilityDigest { get; }
        public string AccessDigest { get; }
        public string RoadWitnessDigest { get; }
        public string FacilityCoordinateDigest { get; }
        public string FacilityWitnessDigest { get; }
        public int RoadCellCount { get; }
        public int FacilityBindingCount { get; }
        public string IndividualHostileTargetMarkerId { get; }
        public IReadOnlyList<VillageNpcMarkerSnapshot> NpcMarkers => npcMarkers;
        public IReadOnlyList<VillageInventoryMarkerSnapshot> InventoryMarkers => inventoryMarkers;
        public IReadOnlyList<VillageDoorMarkerSnapshot> DoorMarkers => doorMarkers;
        public string MarkerDigest { get; }
        public string CanonicalDigest { get; }
        public int FixedCollisionWriteCount => 0;
        public int FixedAccessWriteCount => 0;
        public int RoadWriteCount => 0;
        public int PathWriteCount => 0;
        public int CarveWriteCount => 0;
        public int FacilityCoordinateWriteCount => 0;
        public int SlotOccupantWriteCount => 0;
        public int PersistenceWriteCount => 0;
        public int RandomSelectionCount => 0;
        public int WorldMutationCount => 0;
        public int TileMutationCount => 0;
        public int SceneMutationCount => 0;
        public int PrefabMutationCount => 0;

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source, Func<T, string> id)
            => new ReadOnlyCollection<T>(source.OrderBy(id, StringComparer.Ordinal).ToArray());
    }

    public sealed class VillageStateVariantSet
    {
        private readonly ReadOnlyCollection<VillageStateVariantSnapshot> variants;

        internal VillageStateVariantSet(
            VillageShellPlan shell,
            string individualHostileTargetMarkerId,
            IEnumerable<VillageStateVariantSnapshot> variants)
        {
            RegionId = shell.RegionId;
            ReservationId = shell.ReservationId;
            LayoutId = shell.LayoutId;
            Shape = shell.Shape;
            WidthTiles = shell.WidthTiles;
            HeightTiles = shell.HeightTiles;
            VillageShellDigest = shell.CanonicalDigest;
            RoadDigest = shell.RoadDigest;
            FacilityDigest = shell.FacilityDigest;
            AccessDigest = shell.AccessDigest;
            RoadWitnessDigest = VillageStateVariantCanonicalDigest.ComputeRoadWitness(shell);
            FacilityCoordinateDigest = VillageStateVariantCanonicalDigest.ComputeFacilityCoordinates(shell);
            FacilityWitnessDigest = VillageStateVariantCanonicalDigest.ComputeFacilityWitnesses(shell);
            RoadCellCount = shell.RoadCells.Count;
            FacilityBindingCount = shell.FacilityBindings.Count;
            IndividualHostileTargetMarkerId = individualHostileTargetMarkerId ?? string.Empty;
            this.variants = new ReadOnlyCollection<VillageStateVariantSnapshot>(
                variants.OrderBy(value => value.StateKind).ToArray());
            CanonicalDigest = VillageStateVariantCanonicalDigest.Compute(this);
        }

        public SpecialRegionId RegionId { get; }
        public SiteReservationId ReservationId { get; }
        public VillageLayoutId LayoutId { get; }
        public VillageLayoutShape Shape { get; }
        public int WidthTiles { get; }
        public int HeightTiles { get; }
        public string VillageShellDigest { get; }
        public string RoadDigest { get; }
        public string FacilityDigest { get; }
        public string AccessDigest { get; }
        public string RoadWitnessDigest { get; }
        public string FacilityCoordinateDigest { get; }
        public string FacilityWitnessDigest { get; }
        public int RoadCellCount { get; }
        public int FacilityBindingCount { get; }
        public string IndividualHostileTargetMarkerId { get; }
        public IReadOnlyList<VillageStateVariantSnapshot> Variants => variants;
        public string CanonicalDigest { get; }
        public int FixedCollisionWriteCount => 0;
        public int FixedAccessWriteCount => 0;
        public int GeometryWriteCount => 0;
        public int AccessWriteCount => 0;
        public int PersistenceWriteCount => 0;
        public int RandomSelectionCount => 0;
        public int WorldMutationCount => 0;
        public int TileMutationCount => 0;
        public int SceneMutationCount => 0;
        public int PrefabMutationCount => 0;
    }

    public sealed class VillageStateVariantCompileRequest
    {
        public VillageStateVariantCompileRequest(
            SpecialRegionKind sourceRegionKind,
            VillageShellPlan villageShellPlan,
            string expectedVillageShellDigest,
            VillageStateMarkerSetDefinition markerSetDefinition)
        {
            SourceRegionKind = sourceRegionKind;
            VillageShellPlan = villageShellPlan;
            ExpectedVillageShellDigest = expectedVillageShellDigest ?? string.Empty;
            MarkerSetDefinition = markerSetDefinition;
        }

        public SpecialRegionKind SourceRegionKind { get; }
        public VillageShellPlan VillageShellPlan { get; }
        public string ExpectedVillageShellDigest { get; }
        public VillageStateMarkerSetDefinition MarkerSetDefinition { get; }
    }

    public enum VillageStateVariantErrorCode
    {
        MissingInput = 1,
        DigestMismatch = 2,
        NotVillage = 3,
        MissingMarkerKind = 4,
        DuplicateMarker = 5,
        UnknownFacilityBinding = 6,
        DoorBindingMismatch = 7,
        InsufficientNpcMarkers = 8,
        MissingIndividualTarget = 9,
        UnknownIndividualTarget = 10,
        DuplicateVariant = 11,
        MissingVariant = 12,
        VariantMatrixMismatch = 13,
        ShellInvariantViolation = 14,
        NonCanonicalPublication = 15,
    }

    public sealed class VillageStateVariantError :
        IComparable<VillageStateVariantError>, IEquatable<VillageStateVariantError>
    {
        public VillageStateVariantError(VillageStateVariantErrorCode code, string path, string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public VillageStateVariantErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(VillageStateVariantError other)
        {
            if (other == null) return 1;
            var value = Code.CompareTo(other.Code);
            if (value != 0) return value;
            value = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return value != 0 ? value : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(VillageStateVariantError other)
            => other != null && Code == other.Code &&
               string.Equals(Path, other.Path, StringComparison.Ordinal) &&
               string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as VillageStateVariantError);
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

    public sealed class VillageStateVariantResult
    {
        private readonly ReadOnlyCollection<VillageStateVariantError> errors;

        internal VillageStateVariantResult(
            VillageStateVariantSet variantSet,
            IEnumerable<VillageStateVariantError> errors)
        {
            VariantSet = variantSet;
            this.errors = new ReadOnlyCollection<VillageStateVariantError>(
                (errors ?? Array.Empty<VillageStateVariantError>()).Distinct().OrderBy(value => value).ToArray());
            CanonicalDigest = variantSet == null ? string.Empty : variantSet.CanonicalDigest;
        }

        public bool Success => VariantSet != null && errors.Count == 0;
        public VillageStateVariantSet VariantSet { get; }
        public IReadOnlyList<VillageStateVariantError> Errors => errors;
        public string CanonicalDigest { get; }
    }

    public static class VillageStateVariantCanonicalDigest
    {
        public static string Compute(VillageStateVariantSet variantSet)
        {
            if (variantSet == null) throw new ArgumentNullException(nameof(variantSet));
            var value = new StringBuilder();
            AppendInvariant(value, variantSet.RegionId, variantSet.ReservationId.Value, variantSet.LayoutId,
                variantSet.Shape, variantSet.WidthTiles, variantSet.HeightTiles, variantSet.VillageShellDigest,
                variantSet.RoadDigest, variantSet.FacilityDigest, variantSet.AccessDigest,
                variantSet.RoadWitnessDigest, variantSet.FacilityCoordinateDigest,
                variantSet.FacilityWitnessDigest, variantSet.RoadCellCount, variantSet.FacilityBindingCount);
            Append(value, "individualTarget", variantSet.IndividualHostileTargetMarkerId);
            foreach (var variant in variantSet.Variants)
                Append(value, "variant", Number((int)variant.StateKind) + "/" + variant.CanonicalDigest);
            return Sha256(value.ToString());
        }

        public static string ComputeVariant(VillageStateVariantSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var value = new StringBuilder();
            Append(value, "state", Number((int)snapshot.StateKind));
            AppendInvariant(value, snapshot.RegionId, snapshot.ReservationId.Value, snapshot.LayoutId,
                snapshot.Shape, snapshot.WidthTiles, snapshot.HeightTiles, snapshot.VillageShellDigest,
                snapshot.RoadDigest, snapshot.FacilityDigest, snapshot.AccessDigest,
                snapshot.RoadWitnessDigest, snapshot.FacilityCoordinateDigest,
                snapshot.FacilityWitnessDigest, snapshot.RoadCellCount, snapshot.FacilityBindingCount);
            Append(value, "individualTarget", snapshot.IndividualHostileTargetMarkerId);
            Append(value, "markers", snapshot.MarkerDigest);
            return Sha256(value.ToString());
        }

        public static string ComputeMarkers(VillageStateVariantSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var value = new StringBuilder();
            foreach (var marker in snapshot.NpcMarkers.OrderBy(item => item.MarkerId, StringComparer.Ordinal))
                Append(value, "npc", marker.MarkerId + "/" + marker.FacilityBindingId + "/" +
                    Placed(marker.SourceCoordinate) + "/" + Number((int)marker.State));
            foreach (var marker in snapshot.InventoryMarkers.OrderBy(item => item.MarkerId, StringComparer.Ordinal))
                Append(value, "inventory", marker.MarkerId + "/" + marker.FacilityBindingId + "/" +
                    Placed(marker.SourceCoordinate) + "/" + Number((int)marker.State));
            foreach (var marker in snapshot.DoorMarkers.OrderBy(item => item.MarkerId, StringComparer.Ordinal))
                Append(value, "door", marker.MarkerId + "/" + marker.FacilityBindingId + "/" +
                    Placed(marker.SourceCoordinate) + "/" + Number((int)marker.State));
            return Sha256(value.ToString());
        }

        public static string ComputeRoadWitness(VillageShellPlan shell)
        {
            if (shell == null) throw new ArgumentNullException(nameof(shell));
            var value = new StringBuilder();
            foreach (var cell in shell.RoadAccess.Forward) Append(value, "forward", Road(cell));
            foreach (var cell in shell.RoadAccess.Reverse) Append(value, "reverse", Road(cell));
            return Sha256(value.ToString());
        }

        public static string ComputeFacilityCoordinates(VillageShellPlan shell)
        {
            if (shell == null) throw new ArgumentNullException(nameof(shell));
            var value = new StringBuilder();
            foreach (var binding in shell.FacilityBindings.OrderBy(
                         item => item.Definition.DefinitionId, StringComparer.Ordinal))
                Append(value, "facilityCoordinate", binding.Definition.DefinitionId + "/" +
                    binding.Slot.SlotId.Value + "/" + Placed(binding.Slot.Placed) + "/" + Placed(binding.Door));
            return Sha256(value.ToString());
        }

        public static string ComputeFacilityWitnesses(VillageShellPlan shell)
        {
            if (shell == null) throw new ArgumentNullException(nameof(shell));
            var value = new StringBuilder();
            foreach (var binding in shell.FacilityBindings.OrderBy(
                         item => item.Definition.DefinitionId, StringComparer.Ordinal))
            {
                Append(value, "facilityWitness", binding.Definition.DefinitionId + "/" +
                    binding.Witness.WitnessId + "/" + binding.Witness.FacilityDefinitionId);
                foreach (var cell in binding.AccessCells) Append(value, "access", Placed(cell));
                foreach (var cell in binding.ReverseAccessCells) Append(value, "reverseAccess", Placed(cell));
            }
            return Sha256(value.ToString());
        }

        private static void AppendInvariant(
            StringBuilder value,
            SpecialRegionId regionId,
            string reservationId,
            VillageLayoutId layoutId,
            VillageLayoutShape shape,
            int width,
            int height,
            string shellDigest,
            string roadDigest,
            string facilityDigest,
            string accessDigest,
            string roadWitnessDigest,
            string facilityCoordinateDigest,
            string facilityWitnessDigest,
            int roadCount,
            int facilityCount)
        {
            Append(value, "region", regionId.Value);
            Append(value, "reservation", reservationId);
            Append(value, "layout", layoutId.Value);
            Append(value, "shape", Number((int)shape));
            Append(value, "bounds", Coordinate(width, height));
            Append(value, "shell", shellDigest);
            Append(value, "road", roadDigest);
            Append(value, "facility", facilityDigest);
            Append(value, "access", accessDigest);
            Append(value, "roadWitness", roadWitnessDigest);
            Append(value, "facilityCoordinates", facilityCoordinateDigest);
            Append(value, "facilityWitnesses", facilityWitnessDigest);
            Append(value, "counts", Coordinate(roadCount, facilityCount));
        }

        private static string Road(VillageRoadCell cell)
            => Number(cell.Order) + "/" + Coordinate(cell.RegionTile.X, cell.RegionTile.Y) + "/" + Placed(cell.Placed);
        private static string Placed(SpecialRegionPlacedCoordinate value)
            => Coordinate(value.SectorOffset.X, value.SectorOffset.Y) + "/" +
               Coordinate(value.WorldSector.X, value.WorldSector.Y) + "/" +
               Coordinate(value.LocalTile.X, value.LocalTile.Y) + "/" +
               Coordinate(value.RegionTile.X, value.RegionTile.Y);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Coordinate(int x, int y) => Number(x) + "," + Number(y);
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
