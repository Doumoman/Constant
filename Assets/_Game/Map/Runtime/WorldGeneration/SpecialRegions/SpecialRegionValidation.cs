using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public enum SpecialRegionValidationErrorCode
    {
        InvalidId = 1,
        MissingReservation = 2,
        FootprintMismatch = 3,
        InvalidFootprint = 4,
        InvalidPort = 5,
        InvalidFixedShell = 6,
        InvalidSlot = 7,
        SlotShellOverlap = 8,
        MissingPersistenceKey = 9,
        DuplicatePersistenceKey = 10,
    }

    public sealed class SpecialRegionValidationError :
        IEquatable<SpecialRegionValidationError>, IComparable<SpecialRegionValidationError>
    {
        public SpecialRegionValidationError(SpecialRegionValidationErrorCode code, string path, string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public SpecialRegionValidationErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(SpecialRegionValidationError other)
        {
            if (other == null) return -1;
            var code = ((int)Code).CompareTo((int)other.Code);
            if (code != 0) return code;
            var path = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return path != 0 ? path : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(SpecialRegionValidationError other)
        {
            return other != null && Code == other.Code &&
                   string.Equals(Path, other.Path, StringComparison.Ordinal) &&
                   string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => Equals(obj as SpecialRegionValidationError);
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

    public sealed class SpecialRegionValidationResult
    {
        private readonly ReadOnlyCollection<SpecialRegionValidationError> errors;

        internal SpecialRegionValidationResult(
            SpecialRegionContract contract,
            IEnumerable<SpecialRegionValidationError> errors,
            string canonicalDigest)
        {
            var copy = errors.Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.errors = new ReadOnlyCollection<SpecialRegionValidationError>(copy);
            Contract = copy.Length == 0 ? contract : null;
            CanonicalDigest = copy.Length == 0 ? canonicalDigest ?? string.Empty : string.Empty;
        }

        public bool IsValid => Contract != null && errors.Count == 0;
        public SpecialRegionContract Contract { get; }
        public IReadOnlyList<SpecialRegionValidationError> Errors => errors;
        public string CanonicalDigest { get; }
    }

    public static class SpecialRegionValidator
    {
        public static SpecialRegionValidationResult Validate(
            SpecialRegionContract contract,
            SiteReservation reservation,
            IEnumerable<SpecialRegionContract> knownRegions = null)
        {
            var errors = new List<SpecialRegionValidationError>();
            if (contract == null)
            {
                Add(errors, SpecialRegionValidationErrorCode.InvalidId, "contract", "Contract is required.");
                return new SpecialRegionValidationResult(null, errors, string.Empty);
            }

            ValidateIdentity(contract, errors);
            var footprint = ValidateFootprint(contract, reservation, errors);
            var shell = ValidateShell(contract, footprint, errors);
            var slots = ValidateSlots(contract, footprint, shell, errors);
            ValidatePorts(contract, reservation, footprint, slots, errors);
            ValidatePersistence(contract, slots, knownRegions, errors);

            return errors.Count == 0
                ? new SpecialRegionValidationResult(contract, errors, SpecialRegionCanonicalDigest.Compute(contract))
                : new SpecialRegionValidationResult(null, errors, string.Empty);
        }

        private static void ValidateIdentity(
            SpecialRegionContract contract,
            ICollection<SpecialRegionValidationError> errors)
        {
            if (!IsStableId(contract.Id.Value, "SR_"))
                Add(errors, SpecialRegionValidationErrorCode.InvalidId, "id", contract.Id.Value);
            if (!Enum.IsDefined(typeof(SpecialRegionKind), contract.Kind))
                Add(errors, SpecialRegionValidationErrorCode.InvalidId, "kind", contract.Kind.ToString());
            if (!contract.ReservationId.IsValid)
                Add(errors, SpecialRegionValidationErrorCode.MissingReservation, "reservationId", "A stable reservation ID is required.");
        }

        private static HashSet<SpecialRegionSectorOffset> ValidateFootprint(
            SpecialRegionContract contract,
            SiteReservation reservation,
            ICollection<SpecialRegionValidationError> errors)
        {
            var result = new HashSet<SpecialRegionSectorOffset>();
            var offsets = contract.Footprint == null
                ? Array.Empty<SpecialRegionSectorOffset>()
                : contract.Footprint.Offsets.ToArray();

            foreach (var offset in offsets)
            {
                if (!result.Add(offset))
                    Add(errors, SpecialRegionValidationErrorCode.InvalidFootprint, "footprint", "Duplicate offset " + offset);
            }

            if (result.Count == 0 || result.Any(value => value.X < 0 || value.Y < 0) ||
                result.Min(value => value.X) != 0 || result.Min(value => value.Y) != 0)
            {
                Add(errors, SpecialRegionValidationErrorCode.InvalidFootprint, "footprint", "Offsets must be non-negative and normalized to 0,0.");
            }

            var width = result.Count == 0 ? 0 : result.Max(value => value.X) + 1;
            var height = result.Count == 0 ? 0 : result.Max(value => value.Y) + 1;
            var supported = (width == 1 && height == 1 && result.Count == 1) ||
                            (width == 2 && height == 1 && result.Count == 2) ||
                            (width == 1 && height == 2 && result.Count == 2);
            if (!supported || !IsConnected(result))
                Add(errors, SpecialRegionValidationErrorCode.InvalidFootprint, "footprint", "Only connected 1x1, 2x1, and 1x2 footprints are supported.");

            if (reservation == null || reservation.ReservationId != contract.ReservationId)
            {
                Add(errors, SpecialRegionValidationErrorCode.MissingReservation, "reservation", "The referenced reservation was not supplied.");
                return result;
            }

            var reservationOffsets = new HashSet<SpecialRegionSectorOffset>(reservation.Footprint.Cells.Select(
                value => new SpecialRegionSectorOffset(value.LocalX, value.LocalY)));
            if (!reservationOffsets.SetEquals(result))
                Add(errors, SpecialRegionValidationErrorCode.FootprintMismatch, "footprint", "Authored and reserved footprints must match exactly.");
            if (!ReservationKindMatches(contract.Kind, reservation.Kind))
                Add(errors, SpecialRegionValidationErrorCode.MissingReservation, "reservation.kind", "Reservation kind is incompatible with the special region.");

            return result;
        }

        private static HashSet<string> ValidateShell(
            SpecialRegionContract contract,
            ISet<SpecialRegionSectorOffset> footprint,
            ICollection<SpecialRegionValidationError> errors)
        {
            var positions = new HashSet<string>(StringComparer.Ordinal);
            foreach (var cell in contract.FixedShell)
            {
                if (cell == null)
                {
                    Add(errors, SpecialRegionValidationErrorCode.InvalidFixedShell, "fixedShell", "Null cell.");
                    continue;
                }

                var position = Position(cell.SectorOffset, cell.Tile);
                if (!footprint.Contains(cell.SectorOffset) || !IsSectorTile(cell.Tile) ||
                    !IsStableToken(cell.ShellId) || !positions.Add(position))
                {
                    Add(errors, SpecialRegionValidationErrorCode.InvalidFixedShell, "fixedShell/" + position,
                        "Fixed shell cells require a unique in-footprint tile and stable shell ID.");
                }
            }
            return positions;
        }

        private static Dictionary<SpecialRegionSlotId, SpecialRegionSlot> ValidateSlots(
            SpecialRegionContract contract,
            ISet<SpecialRegionSectorOffset> footprint,
            ISet<string> shell,
            ICollection<SpecialRegionValidationError> errors)
        {
            var byId = new Dictionary<SpecialRegionSlotId, SpecialRegionSlot>();
            var positions = new HashSet<string>(StringComparer.Ordinal);
            foreach (var slot in contract.Slots)
            {
                if (slot == null)
                {
                    Add(errors, SpecialRegionValidationErrorCode.InvalidSlot, "slots", "Null slot.");
                    continue;
                }

                var position = Position(slot.SectorOffset, slot.Tile);
                var valid = IsStableId(slot.Id.Value, "SR_SLOT_") &&
                            Enum.IsDefined(typeof(SpecialRegionSlotKind), slot.Kind) &&
                            footprint.Contains(slot.SectorOffset) && IsSectorTile(slot.Tile) &&
                            byId.TryAdd(slot.Id, slot) && positions.Add(position);
                if (!valid)
                    Add(errors, SpecialRegionValidationErrorCode.InvalidSlot, "slots/" + slot.Id.Value,
                        "Slots require unique IDs and unique in-footprint coordinates.");
                if (shell.Contains(position))
                    Add(errors, SpecialRegionValidationErrorCode.SlotShellOverlap, "slots/" + slot.Id.Value, position);
            }

            if (!byId.Values.Any(value => value.Kind == SpecialRegionSlotKind.Entry))
                Add(errors, SpecialRegionValidationErrorCode.InvalidSlot, "slots.entry", "At least one Entry slot is required.");
            if (!byId.Values.Any(value => value.Kind == SpecialRegionSlotKind.Return))
                Add(errors, SpecialRegionValidationErrorCode.InvalidSlot, "slots.return", "At least one Return slot is required.");
            return byId;
        }

        private static void ValidatePorts(
            SpecialRegionContract contract,
            SiteReservation reservation,
            ISet<SpecialRegionSectorOffset> footprint,
            IReadOnlyDictionary<SpecialRegionSlotId, SpecialRegionSlot> slots,
            ICollection<SpecialRegionValidationError> errors)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var entryCount = 0;
            var returnCount = 0;
            foreach (var port in contract.Ports)
            {
                if (port == null)
                {
                    Add(errors, SpecialRegionValidationErrorCode.InvalidPort, "ports", "Null port.");
                    continue;
                }

                if (port.Kind == SpecialRegionSlotKind.Entry) entryCount++;
                if (port.Kind == SpecialRegionSlotKind.Return) returnCount++;
                slots.TryGetValue(port.SlotId, out var slot);
                var matchesSlot = slot != null && slot.Kind == port.Kind &&
                                  slot.SectorOffset == port.SectorOffset && slot.Tile == port.Tile;
                var sideValid = Enum.IsDefined(typeof(SiteEntrySide), port.Side);
                var valid = IsStableId(port.PortId, "SR_PORT_") && ids.Add(port.PortId) &&
                            (port.Kind == SpecialRegionSlotKind.Entry || port.Kind == SpecialRegionSlotKind.Return) &&
                            footprint.Contains(port.SectorOffset) && IsSectorTile(port.Tile) &&
                            AccessClassTokenCodec.IsPublished(port.AccessClass) && sideValid && matchesSlot;

                if (valid && reservation != null && reservation.ReservationId == contract.ReservationId)
                {
                    var sector = new SectorCoord(
                        reservation.Origin.X + port.SectorOffset.X,
                        reservation.Origin.Y + port.SectorOffset.Y);
                    valid = reservation.EntryAnchors.Any(anchor => anchor.FootprintSector == sector &&
                        anchor.Side == port.Side &&
                        (port.Kind == SpecialRegionSlotKind.Entry || anchor.ReturnPathRequired));
                }

                if (!valid)
                    Add(errors, SpecialRegionValidationErrorCode.InvalidPort, "ports/" + port.PortId,
                        "Port must match its slot, reservation anchor, side, and AccessClass.");
            }

            if (entryCount == 0)
                Add(errors, SpecialRegionValidationErrorCode.InvalidPort, "ports.entry", "At least one Entry port is required.");
            if (returnCount == 0)
                Add(errors, SpecialRegionValidationErrorCode.InvalidPort, "ports.return", "At least one Return port is required.");
        }

        private static void ValidatePersistence(
            SpecialRegionContract contract,
            IReadOnlyDictionary<SpecialRegionSlotId, SpecialRegionSlot> slots,
            IEnumerable<SpecialRegionContract> knownRegions,
            ICollection<SpecialRegionValidationError> errors)
        {
            var byKey = new Dictionary<SpecialPersistenceKey, SpecialPersistenceBinding>();
            foreach (var binding in contract.Persistence)
            {
                if (binding == null)
                {
                    Add(errors, SpecialRegionValidationErrorCode.MissingPersistenceKey, "persistence", "Null binding.");
                    continue;
                }

                var keyValid = IsStableId(binding.Key.Value, "SR_STATE_");
                var scopeValid = Enum.IsDefined(typeof(SpecialPersistenceScope), binding.Scope);
                var meaningValid = IsStableToken(binding.InitialMeaning);
                var expected = default(SpecialPersistenceKey);
                var identityValid = false;
                if (binding.Scope == SpecialPersistenceScope.Region && binding.SlotId.Value.Length == 0)
                {
                    expected = SpecialPersistenceKey.ForRegion(contract.Id);
                    identityValid = true;
                }
                else if (slots.ContainsKey(binding.SlotId))
                {
                    expected = SpecialPersistenceKey.ForSlot(contract.Id, binding.Scope, binding.SlotId);
                    identityValid = true;
                }

                if (!keyValid || !scopeValid || !meaningValid || !identityValid || binding.Key != expected)
                    Add(errors, SpecialRegionValidationErrorCode.MissingPersistenceKey,
                        "persistence/" + binding.Key.Value, "Key must be deterministically bound to region, scope, and slot identity.");
                if (!byKey.TryAdd(binding.Key, binding))
                    Add(errors, SpecialRegionValidationErrorCode.DuplicatePersistenceKey,
                        "persistence/" + binding.Key.Value, "Duplicate key inside region.");
            }

            foreach (var slot in slots.Values)
            {
                var requiresKey = slot.Required && slot.Kind == SpecialRegionSlotKind.Reward;
                if (requiresKey && slot.PersistenceKey.Value.Length == 0)
                    Add(errors, SpecialRegionValidationErrorCode.MissingPersistenceKey,
                        "slots/" + slot.Id.Value, "Required Reward slots need a persistence key.");

                if (slot.PersistenceKey.Value.Length != 0)
                {
                    if (!byKey.TryGetValue(slot.PersistenceKey, out var binding) ||
                        binding.SlotId != slot.Id || binding.Scope != slot.PersistenceScope ||
                        !ScopeMatchesSlot(slot.PersistenceScope, slot.Kind))
                    {
                        Add(errors, SpecialRegionValidationErrorCode.MissingPersistenceKey,
                            "slots/" + slot.Id.Value, "Slot persistence must match one canonical binding.");
                    }
                }
            }

            if (knownRegions == null) return;
            foreach (var other in knownRegions.Where(value => value != null && value.Id != contract.Id))
            foreach (var binding in other.Persistence.Where(value => value != null))
            {
                if (byKey.ContainsKey(binding.Key))
                    Add(errors, SpecialRegionValidationErrorCode.DuplicatePersistenceKey,
                        "persistence/" + binding.Key.Value, "Persistence key collides with region " + other.Id.Value + ".");
            }
        }

        private static bool IsConnected(ISet<SpecialRegionSectorOffset> offsets)
        {
            if (offsets.Count == 0) return false;
            var visited = new HashSet<SpecialRegionSectorOffset>();
            var pending = new Queue<SpecialRegionSectorOffset>();
            pending.Enqueue(offsets.First());
            while (pending.Count != 0)
            {
                var current = pending.Dequeue();
                if (!visited.Add(current)) continue;
                foreach (var candidate in new[]
                         {
                             new SpecialRegionSectorOffset(current.X - 1, current.Y),
                             new SpecialRegionSectorOffset(current.X + 1, current.Y),
                             new SpecialRegionSectorOffset(current.X, current.Y - 1),
                             new SpecialRegionSectorOffset(current.X, current.Y + 1),
                         })
                    if (offsets.Contains(candidate) && !visited.Contains(candidate)) pending.Enqueue(candidate);
            }
            return visited.Count == offsets.Count;
        }

        private static bool ReservationKindMatches(SpecialRegionKind region, SiteReservationKind reservation)
        {
            switch (region)
            {
                case SpecialRegionKind.Village: return reservation == SiteReservationKind.Village;
                case SpecialRegionKind.CoreResource: return reservation == SiteReservationKind.CoreResource;
                case SpecialRegionKind.Forge: return reservation == SiteReservationKind.Forge;
                case SpecialRegionKind.Boss: return reservation == SiteReservationKind.Boss;
                case SpecialRegionKind.OptionalLandmark: return reservation != SiteReservationKind.Start;
                default: return false;
            }
        }

        private static bool ScopeMatchesSlot(SpecialPersistenceScope scope, SpecialRegionSlotKind kind)
        {
            if (kind == SpecialRegionSlotKind.Reward) return scope == SpecialPersistenceScope.Reward;
            if (kind == SpecialRegionSlotKind.Enemy || kind == SpecialRegionSlotKind.Event)
                return scope == SpecialPersistenceScope.Encounter;
            return scope == SpecialPersistenceScope.Slot;
        }

        private static bool IsSectorTile(LocalTileCoord tile)
        {
            return tile.X >= 0 && tile.X < WorldGenConstants.SectorWidthTiles &&
                   tile.Y >= 0 && tile.Y < WorldGenConstants.SectorHeightTiles;
        }

        private static string Position(SpecialRegionSectorOffset sector, LocalTileCoord tile)
            => sector.X + "," + sector.Y + "/" + tile.X + "," + tile.Y;

        internal static bool IsStableId(string value, string prefix)
            => value != null && value.StartsWith(prefix, StringComparison.Ordinal) && IsStableToken(value);

        internal static bool IsStableToken(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            foreach (var character in value)
                if ((character < 'A' || character > 'Z') &&
                    (character < '0' || character > '9') && character != '_') return false;
            return true;
        }

        private static void Add(
            ICollection<SpecialRegionValidationError> errors,
            SpecialRegionValidationErrorCode code,
            string path,
            string detail)
            => errors.Add(new SpecialRegionValidationError(code, path, detail));
    }
}
