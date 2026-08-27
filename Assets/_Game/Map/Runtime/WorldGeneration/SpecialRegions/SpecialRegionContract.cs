using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public readonly struct SpecialRegionId : IEquatable<SpecialRegionId>, IComparable<SpecialRegionId>
    {
        private readonly string value;
        public SpecialRegionId(string value) { this.value = value; }
        public string Value => value ?? string.Empty;
        public int CompareTo(SpecialRegionId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(SpecialRegionId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SpecialRegionId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(SpecialRegionId left, SpecialRegionId right) => left.Equals(right);
        public static bool operator !=(SpecialRegionId left, SpecialRegionId right) => !left.Equals(right);
    }

    public readonly struct SpecialRegionSlotId : IEquatable<SpecialRegionSlotId>, IComparable<SpecialRegionSlotId>
    {
        private readonly string value;
        public SpecialRegionSlotId(string value) { this.value = value; }
        public string Value => value ?? string.Empty;
        public int CompareTo(SpecialRegionSlotId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(SpecialRegionSlotId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SpecialRegionSlotId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(SpecialRegionSlotId left, SpecialRegionSlotId right) => left.Equals(right);
        public static bool operator !=(SpecialRegionSlotId left, SpecialRegionSlotId right) => !left.Equals(right);
    }

    public readonly struct SpecialPersistenceKey : IEquatable<SpecialPersistenceKey>, IComparable<SpecialPersistenceKey>
    {
        private readonly string value;
        public SpecialPersistenceKey(string value) { this.value = value; }
        public string Value => value ?? string.Empty;
        public int CompareTo(SpecialPersistenceKey other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(SpecialPersistenceKey other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SpecialPersistenceKey other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(SpecialPersistenceKey left, SpecialPersistenceKey right) => left.Equals(right);
        public static bool operator !=(SpecialPersistenceKey left, SpecialPersistenceKey right) => !left.Equals(right);

        public static SpecialPersistenceKey ForRegion(SpecialRegionId regionId)
        {
            return new SpecialPersistenceKey("SR_STATE_" + TrimPrefix(regionId.Value, "SR_") + "_REGION");
        }

        public static SpecialPersistenceKey ForSlot(
            SpecialRegionId regionId,
            SpecialPersistenceScope scope,
            SpecialRegionSlotId slotId)
        {
            return new SpecialPersistenceKey("SR_STATE_" + TrimPrefix(regionId.Value, "SR_") + "_" +
                ScopeToken(scope) + "_" + TrimPrefix(slotId.Value, "SR_SLOT_"));
        }

        internal static string ScopeToken(SpecialPersistenceScope scope)
        {
            switch (scope)
            {
                case SpecialPersistenceScope.Region: return "REGION";
                case SpecialPersistenceScope.Slot: return "SLOT";
                case SpecialPersistenceScope.Reward: return "REWARD";
                case SpecialPersistenceScope.Encounter: return "ENCOUNTER";
                default: return "INVALID";
            }
        }

        private static string TrimPrefix(string value, string prefix)
        {
            return value != null && value.StartsWith(prefix, StringComparison.Ordinal)
                ? value.Substring(prefix.Length)
                : value ?? string.Empty;
        }
    }

    public enum SpecialRegionKind
    {
        Village = 1,
        CoreResource = 2,
        Forge = 3,
        Boss = 4,
        OptionalLandmark = 5,
    }

    public enum SpecialRegionLayerKind
    {
        FixedShell = 1,
        ReplaceableSlot = 2,
    }

    public enum SpecialRegionSlotKind
    {
        Facility = 1,
        Npc = 2,
        Enemy = 3,
        Event = 4,
        Reward = 5,
        Entry = 6,
        Return = 7,
    }

    public enum SpecialPersistenceScope
    {
        Region = 1,
        Slot = 2,
        Reward = 3,
        Encounter = 4,
    }

    public readonly struct SpecialRegionSectorOffset :
        IEquatable<SpecialRegionSectorOffset>, IComparable<SpecialRegionSectorOffset>
    {
        public SpecialRegionSectorOffset(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
        public int CanonicalIndex => (Y * 2) + X;
        public int CompareTo(SpecialRegionSectorOffset other)
        {
            var y = Y.CompareTo(other.Y);
            return y != 0 ? y : X.CompareTo(other.X);
        }
        public bool Equals(SpecialRegionSectorOffset other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is SpecialRegionSectorOffset other && Equals(other);
        public override int GetHashCode() { unchecked { return (X * 397) ^ Y; } }
        public override string ToString() => X + "," + Y;
        public static bool operator ==(SpecialRegionSectorOffset left, SpecialRegionSectorOffset right) => left.Equals(right);
        public static bool operator !=(SpecialRegionSectorOffset left, SpecialRegionSectorOffset right) => !left.Equals(right);
    }

    public sealed class SpecialRegionFootprint
    {
        private readonly ReadOnlyCollection<SpecialRegionSectorOffset> offsets;

        public SpecialRegionFootprint(IEnumerable<SpecialRegionSectorOffset> offsets)
        {
            var copy = offsets == null ? Array.Empty<SpecialRegionSectorOffset>() : offsets.ToArray();
            Array.Sort(copy);
            this.offsets = new ReadOnlyCollection<SpecialRegionSectorOffset>(copy);
        }

        public IReadOnlyList<SpecialRegionSectorOffset> Offsets => offsets;
    }

    public sealed class SpecialRegionFixedShellCell
    {
        public SpecialRegionFixedShellCell(SpecialRegionSectorOffset sectorOffset, LocalTileCoord tile, string shellId)
        {
            SectorOffset = sectorOffset;
            Tile = tile;
            ShellId = shellId ?? string.Empty;
        }

        public SpecialRegionSectorOffset SectorOffset { get; }
        public LocalTileCoord Tile { get; }
        public string ShellId { get; }
        public SpecialRegionLayerKind LayerKind => SpecialRegionLayerKind.FixedShell;
    }

    public sealed class SpecialRegionSlot
    {
        public SpecialRegionSlot(
            SpecialRegionSlotId id,
            SpecialRegionSlotKind kind,
            SpecialRegionSectorOffset sectorOffset,
            LocalTileCoord tile,
            bool required,
            SpecialPersistenceScope persistenceScope,
            SpecialPersistenceKey persistenceKey)
        {
            Id = id;
            Kind = kind;
            SectorOffset = sectorOffset;
            Tile = tile;
            Required = required;
            PersistenceScope = persistenceScope;
            PersistenceKey = persistenceKey;
        }

        public SpecialRegionSlotId Id { get; }
        public SpecialRegionSlotKind Kind { get; }
        public SpecialRegionSectorOffset SectorOffset { get; }
        public LocalTileCoord Tile { get; }
        public bool Required { get; }
        public SpecialPersistenceScope PersistenceScope { get; }
        public SpecialPersistenceKey PersistenceKey { get; }
        public SpecialRegionLayerKind LayerKind => SpecialRegionLayerKind.ReplaceableSlot;
    }

    public sealed class SpecialRegionPort
    {
        public SpecialRegionPort(
            string portId,
            SpecialRegionSlotId slotId,
            SpecialRegionSlotKind kind,
            SpecialRegionSectorOffset sectorOffset,
            LocalTileCoord tile,
            SiteEntrySide side,
            AccessClass accessClass)
        {
            PortId = portId ?? string.Empty;
            SlotId = slotId;
            Kind = kind;
            SectorOffset = sectorOffset;
            Tile = tile;
            Side = side;
            AccessClass = accessClass;
        }

        public string PortId { get; }
        public SpecialRegionSlotId SlotId { get; }
        public SpecialRegionSlotKind Kind { get; }
        public SpecialRegionSectorOffset SectorOffset { get; }
        public LocalTileCoord Tile { get; }
        public SiteEntrySide Side { get; }
        public AccessClass AccessClass { get; }
    }

    public sealed class SpecialPersistenceBinding
    {
        public SpecialPersistenceBinding(
            SpecialPersistenceKey key,
            SpecialPersistenceScope scope,
            SpecialRegionSlotId slotId,
            string initialMeaning)
        {
            Key = key;
            Scope = scope;
            SlotId = slotId;
            InitialMeaning = initialMeaning ?? string.Empty;
        }

        public SpecialPersistenceKey Key { get; }
        public SpecialPersistenceScope Scope { get; }
        public SpecialRegionSlotId SlotId { get; }
        public string InitialMeaning { get; }
    }

    public sealed class SpecialRegionContract
    {
        private readonly ReadOnlyCollection<SpecialRegionFixedShellCell> fixedShell;
        private readonly ReadOnlyCollection<SpecialRegionSlot> slots;
        private readonly ReadOnlyCollection<SpecialRegionPort> ports;
        private readonly ReadOnlyCollection<SpecialPersistenceBinding> persistence;

        public SpecialRegionContract(
            SpecialRegionId id,
            SpecialRegionKind kind,
            SiteReservationId reservationId,
            SpecialRegionFootprint footprint,
            IEnumerable<SpecialRegionFixedShellCell> fixedShell,
            IEnumerable<SpecialRegionSlot> slots,
            IEnumerable<SpecialRegionPort> ports,
            IEnumerable<SpecialPersistenceBinding> persistence,
            string displayText = "")
        {
            Id = id;
            Kind = kind;
            ReservationId = reservationId;
            Footprint = footprint;
            this.fixedShell = Freeze(fixedShell, CompareShell);
            this.slots = Freeze(slots, (left, right) => left.Id.CompareTo(right.Id));
            this.ports = Freeze(ports, (left, right) => string.Compare(left.PortId, right.PortId, StringComparison.Ordinal));
            this.persistence = Freeze(persistence, (left, right) => left.Key.CompareTo(right.Key));
            DisplayText = displayText ?? string.Empty;
        }

        public SpecialRegionId Id { get; }
        public SpecialRegionKind Kind { get; }
        public SiteReservationId ReservationId { get; }
        public SpecialRegionFootprint Footprint { get; }
        public IReadOnlyList<SpecialRegionFixedShellCell> FixedShell => fixedShell;
        public IReadOnlyList<SpecialRegionSlot> Slots => slots;
        public IReadOnlyList<SpecialRegionPort> Ports => ports;
        public IReadOnlyList<SpecialPersistenceBinding> Persistence => persistence;
        public string DisplayText { get; }

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source, Comparison<T> comparison)
        {
            var copy = source == null ? Array.Empty<T>() : source.ToArray();
            Array.Sort(copy, comparison);
            return new ReadOnlyCollection<T>(copy);
        }

        private static int CompareShell(SpecialRegionFixedShellCell left, SpecialRegionFixedShellCell right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            var sector = left.SectorOffset.CompareTo(right.SectorOffset);
            if (sector != 0) return sector;
            var y = left.Tile.Y.CompareTo(right.Tile.Y);
            return y != 0 ? y : left.Tile.X.CompareTo(right.Tile.X);
        }
    }
}
