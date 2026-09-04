using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public sealed class GeneratedSectorLocalCellIndex :
        IEquatable<GeneratedSectorLocalCellIndex>, IComparable<GeneratedSectorLocalCellIndex>
    {
        public GeneratedSectorLocalCellIndex(int value)
        {
            Value = value;
        }

        public int Value { get; }
        public bool IsValid => Value >= 0 &&
            Value < GeneratedTerrainGeometrySnapshot.CanonicalSectorCellCount;
        public int SectorLocalX => IsValid
            ? Value % GeneratedTerrainGeometrySnapshot.CanonicalSectorWidth : -1;
        public int SectorLocalY => IsValid
            ? Value / GeneratedTerrainGeometrySnapshot.CanonicalSectorWidth : -1;
        public string StableToken => IsValid
            ? Number(Value) + "|" + Number(SectorLocalX) + "|" + Number(SectorLocalY)
            : "INVALID|" + Number(Value);

        public int CompareTo(GeneratedSectorLocalCellIndex other) => other == null
            ? -1 : Value.CompareTo(other.Value);
        public bool Equals(GeneratedSectorLocalCellIndex other) => other != null &&
            Value == other.Value;
        public override bool Equals(object obj) => Equals(obj as GeneratedSectorLocalCellIndex);
        public override int GetHashCode() => Value;
        public override string ToString() => Number(Value);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedSectorModificationTarget :
        IEquatable<GeneratedSectorModificationTarget>,
        IComparable<GeneratedSectorModificationTarget>
    {
        public GeneratedSectorModificationTarget(
            GeneratedSectorCoordinate sector,
            GeneratedSectorLocalCellIndex localIndex,
            int layerId,
            string sourceProvenanceToken,
            string slotReference = null)
        {
            Sector = sector;
            LocalIndex = localIndex;
            LayerId = layerId;
            SourceProvenanceToken = Normalize(sourceProvenanceToken);
            SlotReference = Normalize(slotReference);
            StableToken = string.Join("|", new[]
            {
                "SECTOR_MODIFICATION_TARGET",
                Sector == null ? "MISSING" : Sector.ToString(),
                LocalIndex == null ? "MISSING" : LocalIndex.StableToken,
                Number(LayerId), SourceProvenanceToken,
                string.IsNullOrEmpty(SlotReference) ? "NONE" : SlotReference,
            });
        }

        public GeneratedSectorCoordinate Sector { get; }
        public GeneratedSectorLocalCellIndex LocalIndex { get; }
        public int LayerId { get; }
        public string SourceProvenanceToken { get; }
        public string SlotReference { get; }
        public bool HasSlotReference => !string.IsNullOrEmpty(SlotReference);
        public bool IsLayerValid => Enum.IsDefined(typeof(GeneratedTilemapLayerId), LayerId);
        public string StableToken { get; }

        public int CompareTo(GeneratedSectorModificationTarget other)
        {
            if (other == null) return -1;
            var comparison = Sector == null
                ? (other.Sector == null ? 0 : 1)
                : Sector.CompareTo(other.Sector);
            if (comparison != 0) return comparison;
            comparison = LocalIndex == null
                ? (other.LocalIndex == null ? 0 : 1)
                : LocalIndex.CompareTo(other.LocalIndex);
            if (comparison != 0) return comparison;
            comparison = LayerId.CompareTo(other.LayerId);
            if (comparison != 0) return comparison;
            comparison = string.Compare(SourceProvenanceToken,
                other.SourceProvenanceToken, StringComparison.Ordinal);
            return comparison != 0 ? comparison :
                string.Compare(SlotReference, other.SlotReference, StringComparison.Ordinal);
        }

        public bool Equals(GeneratedSectorModificationTarget other) => other != null &&
            string.Equals(StableToken, other.StableToken, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as GeneratedSectorModificationTarget);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
        public override string ToString() => StableToken;
        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public enum GeneratedSectorModificationKind
    {
        DestroyTile = 1,
        ReplaceTile = 2,
        CollectPickup = 3,
        ChangeDeviceState = 4,
        ConsumeSlot = 5,
    }

    public sealed class GeneratedSectorModificationPayload
    {
        public GeneratedSectorModificationPayload(
            string oldTileCode,
            string oldSourceToken,
            string newTileCode,
            string newSourceToken,
            string stateKey,
            string stateValue,
            bool logicalRemoved,
            bool collected,
            bool consumed)
        {
            OldTileCode = Normalize(oldTileCode);
            OldSourceToken = Normalize(oldSourceToken);
            NewTileCode = Normalize(newTileCode);
            NewSourceToken = Normalize(newSourceToken);
            StateKey = Normalize(stateKey);
            StateValue = Normalize(stateValue);
            LogicalRemoved = logicalRemoved;
            Collected = collected;
            Consumed = consumed;
            StableToken = string.Join("|", new[]
            {
                "SECTOR_MODIFICATION_PAYLOAD", OldTileCode, OldSourceToken,
                NewTileCode, NewSourceToken, StateKey, StateValue,
                LogicalRemoved ? "1" : "0", Collected ? "1" : "0",
                Consumed ? "1" : "0",
            });
        }

        public string OldTileCode { get; }
        public string OldSourceToken { get; }
        public string NewTileCode { get; }
        public string NewSourceToken { get; }
        public string StateKey { get; }
        public string StateValue { get; }
        public bool LogicalRemoved { get; }
        public bool Collected { get; }
        public bool Consumed { get; }
        public string StableToken { get; }

        public bool IsValidFor(GeneratedSectorModificationKind kind)
        {
            switch (kind)
            {
                case GeneratedSectorModificationKind.DestroyTile:
                    return LogicalRemoved && !string.IsNullOrEmpty(OldSourceToken);
                case GeneratedSectorModificationKind.ReplaceTile:
                    return !string.IsNullOrEmpty(OldSourceToken) &&
                           !string.IsNullOrEmpty(NewTileCode) &&
                           !string.IsNullOrEmpty(NewSourceToken);
                case GeneratedSectorModificationKind.CollectPickup:
                    return Collected;
                case GeneratedSectorModificationKind.ChangeDeviceState:
                    return !string.IsNullOrEmpty(StateKey) &&
                           !string.IsNullOrEmpty(StateValue);
                case GeneratedSectorModificationKind.ConsumeSlot:
                    return Consumed && !string.IsNullOrEmpty(OldSourceToken);
                default:
                    return false;
            }
        }

        public static GeneratedSectorModificationPayload Destroy(
            string oldTileCode,
            string oldSourceToken) => new GeneratedSectorModificationPayload(
                oldTileCode, oldSourceToken, string.Empty, string.Empty,
                string.Empty, string.Empty, true, false, false);

        public static GeneratedSectorModificationPayload Replace(
            string oldTileCode,
            string oldSourceToken,
            string newTileCode,
            string newSourceToken) => new GeneratedSectorModificationPayload(
                oldTileCode, oldSourceToken, newTileCode, newSourceToken,
                string.Empty, string.Empty, false, false, false);

        public static GeneratedSectorModificationPayload PickupCollected() =>
            new GeneratedSectorModificationPayload(string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty,
                false, true, false);

        public static GeneratedSectorModificationPayload DeviceState(
            string key,
            string value) => new GeneratedSectorModificationPayload(
                string.Empty, string.Empty, string.Empty, string.Empty,
                key, value, false, false, false);

        public static GeneratedSectorModificationPayload SlotConsumed(string sourceOwner) =>
            new GeneratedSectorModificationPayload(string.Empty, sourceOwner,
                string.Empty, string.Empty, string.Empty, string.Empty,
                false, false, true);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
    }

    public sealed class GeneratedSectorModificationStableId :
        IEquatable<GeneratedSectorModificationStableId>,
        IComparable<GeneratedSectorModificationStableId>
    {
        public GeneratedSectorModificationStableId(
            string seedIdentity,
            string generatorVersion,
            string dataVersion,
            GeneratedSectorModificationTarget target,
            GeneratedSectorModificationKind kind,
            string schemaVersion)
        {
            Namespace = "SECTOR_MODIFICATION";
            StableToken = string.Join("|", new[]
            {
                Namespace, Normalize(seedIdentity), Normalize(generatorVersion),
                Normalize(dataVersion), target == null ? "MISSING" : target.StableToken,
                kind.ToString().ToUpperInvariant(), Normalize(schemaVersion),
            });
            Value = BakingCanonicalDigest.HashCanonicalLines(new[] { StableToken });
        }

        public string Namespace { get; }
        public string StableToken { get; }
        public string Value { get; }
        public bool IsValid => Namespace == "SECTOR_MODIFICATION" &&
            BakingCanonicalDigest.IsLowerHexSha256(Value);
        public int CompareTo(GeneratedSectorModificationStableId other) => other == null
            ? -1 : string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(GeneratedSectorModificationStableId other) => other != null &&
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as GeneratedSectorModificationStableId);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
    }

    public sealed class GeneratedSectorModificationRecord :
        IComparable<GeneratedSectorModificationRecord>
    {
        public GeneratedSectorModificationRecord(
            GeneratedSectorModificationStableId id,
            GeneratedSectorModificationTarget target,
            GeneratedSectorModificationKind kind,
            int revision,
            GeneratedSectorModificationPayload payload,
            GeneratedSectorModificationBaseDigests baseDigests)
        {
            Id = id;
            Target = target;
            Kind = kind;
            Revision = revision;
            Payload = payload;
            BaseDigests = baseDigests;
            SourceDigest = GeneratedSectorModificationDigest.ComputeBase(baseDigests);
            StableToken = string.Join("|", new[]
            {
                "SECTOR_MODIFICATION_RECORD", Id == null ? "MISSING" : Id.Value,
                Target == null ? "MISSING" : Target.StableToken,
                Kind.ToString().ToUpperInvariant(), Number(Revision),
                Payload == null ? "MISSING" : Payload.StableToken, SourceDigest,
            });
        }

        public GeneratedSectorModificationStableId Id { get; }
        public GeneratedSectorModificationTarget Target { get; }
        public GeneratedSectorModificationKind Kind { get; }
        public int Revision { get; }
        public GeneratedSectorModificationPayload Payload { get; }
        public GeneratedSectorModificationBaseDigests BaseDigests { get; }
        public string SourceDigest { get; }
        public string StableToken { get; }

        public int CompareTo(GeneratedSectorModificationRecord other)
        {
            if (other == null) return -1;
            var comparison = Id == null
                ? (other.Id == null ? 0 : 1)
                : Id.CompareTo(other.Id);
            return comparison != 0 ? comparison : Revision.CompareTo(other.Revision);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedSectorModificationSet
    {
        private readonly ReadOnlyCollection<GeneratedSectorModificationRecord> records;

        internal GeneratedSectorModificationSet(
            GeneratedSectorCoordinate sector,
            int dirtyRevision,
            GeneratedSectorModificationBaseDigests baseDigests,
            IEnumerable<GeneratedSectorModificationRecord> sourceRecords)
        {
            Sector = sector;
            DirtyRevision = dirtyRevision;
            BaseDigests = baseDigests;
            records = new ReadOnlyCollection<GeneratedSectorModificationRecord>((sourceRecords ??
                Array.Empty<GeneratedSectorModificationRecord>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            Digest = GeneratedSectorModificationDigest.ComputeSet(this);
        }

        public GeneratedSectorCoordinate Sector { get; }
        public int DirtyRevision { get; }
        public GeneratedSectorModificationBaseDigests BaseDigests { get; }
        public IReadOnlyList<GeneratedSectorModificationRecord> Records => records;
        public int RecordCount => records.Count;
        public string Digest { get; }
    }
}
