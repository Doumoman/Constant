using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.Population
{
    public enum GeneratedContentSlotCategory
    {
        Resource = 1,
        Shop = 2,
        Hazard = 3,
        Enemy = 4,
        Pickup = 5,
        Device = 6,
        Activity = 7,
        Event = 8,
        Special = 9,
    }

    public sealed class GeneratedContentPoolKey :
        IEquatable<GeneratedContentPoolKey>, IComparable<GeneratedContentPoolKey>
    {
        public GeneratedContentPoolKey(string poolNamespace, string version)
        {
            PoolNamespace = Normalize(poolNamespace);
            Version = Normalize(version);
            StableToken = "POOL|" + Part(PoolNamespace) + "|" + Part(Version);
        }

        public string PoolNamespace { get; }
        public string Version { get; }
        public string StableToken { get; }
        public bool IsValid => IsSingleLine(PoolNamespace) && IsSingleLine(Version);

        public int CompareTo(GeneratedContentPoolKey other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        public bool Equals(GeneratedContentPoolKey other) => other != null &&
            string.Equals(StableToken, other.StableToken, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as GeneratedContentPoolKey);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
        public override string ToString() => PoolNamespace + "@" + Version;

        internal static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        internal static bool IsSingleLine(string value) => !string.IsNullOrEmpty(value) &&
            value.IndexOf('\n') < 0;
        internal static string Part(string value)
        {
            var normalized = value ?? string.Empty;
            return normalized.Length.ToString(CultureInfo.InvariantCulture) + ":" + normalized;
        }
    }

    public sealed class GeneratedContentSlotAddress :
        IEquatable<GeneratedContentSlotAddress>, IComparable<GeneratedContentSlotAddress>
    {
        public const string SchemaVersion = "CONTENT_SLOT_ADDRESS_V1";

        public GeneratedContentSlotAddress(
            string worldSeed,
            string generatorVersion,
            string dataVersion,
            GeneratedSectorCoordinate sector,
            int sliceIndex,
            int sectorLocalIndex,
            int sliceLocalIndex,
            GeneratedMarkerSlotOwner sourceOwnerKind,
            string sourceOwnerId,
            string sourceProvenanceToken,
            string sourceSlotId,
            GeneratedContentSlotCategory category,
            GeneratedContentPoolKey poolKey)
        {
            WorldSeed = GeneratedContentPoolKey.Normalize(worldSeed);
            GeneratorVersion = GeneratedContentPoolKey.Normalize(generatorVersion);
            DataVersion = GeneratedContentPoolKey.Normalize(dataVersion);
            Sector = sector;
            SliceIndex = sliceIndex;
            SectorLocalIndex = sectorLocalIndex;
            SliceLocalIndex = sliceLocalIndex;
            SourceOwnerKind = sourceOwnerKind;
            SourceOwnerId = GeneratedContentPoolKey.Normalize(sourceOwnerId);
            SourceProvenanceToken = GeneratedContentPoolKey.Normalize(sourceProvenanceToken);
            SourceSlotId = GeneratedContentPoolKey.Normalize(sourceSlotId);
            Category = category;
            PoolKey = poolKey;
            CanonicalLine = string.Join("|", new[]
            {
                SchemaVersion,
                Field("SEED", WorldSeed),
                Field("GENERATOR", GeneratorVersion),
                Field("DATA", DataVersion),
                "SECTOR=" + (Sector == null ? "MISSING" : Sector.ToString()),
                "SLICE=" + Number(SliceIndex),
                "SECTOR_LOCAL=" + Number(SectorLocalIndex),
                "SLICE_LOCAL=" + Number(SliceLocalIndex),
                "OWNER_KIND=" + SourceOwnerKind.ToString().ToUpperInvariant(),
                Field("OWNER_ID", SourceOwnerId),
                Field("PROVENANCE", SourceProvenanceToken),
                Field("SOURCE_SLOT", SourceSlotId),
                "CATEGORY=" + Category.ToString().ToUpperInvariant(),
                "POOL=" + (PoolKey == null ? "MISSING" : PoolKey.StableToken),
            });
            ReservationCanonicalLine = string.Join("|", new[]
            {
                "CONTENT_SLOT_RESERVATION_V1",
                Field("SEED", WorldSeed),
                Field("GENERATOR", GeneratorVersion),
                Field("DATA", DataVersion),
                "SECTOR=" + (Sector == null ? "MISSING" : Sector.ToString()),
                "SLICE=" + Number(SliceIndex),
                "SECTOR_LOCAL=" + Number(SectorLocalIndex),
                "SLICE_LOCAL=" + Number(SliceLocalIndex),
                "OWNER_KIND=" + SourceOwnerKind.ToString().ToUpperInvariant(),
                Field("OWNER_ID", SourceOwnerId),
                Field("PROVENANCE", SourceProvenanceToken),
                Field("SOURCE_SLOT", SourceSlotId),
            });
        }

        public string WorldSeed { get; }
        public string GeneratorVersion { get; }
        public string DataVersion { get; }
        public GeneratedSectorCoordinate Sector { get; }
        public int SliceIndex { get; }
        public int SectorLocalIndex { get; }
        public int SliceLocalIndex { get; }
        public bool HasSliceLocalIndex => SliceLocalIndex >= 0;
        public GeneratedMarkerSlotOwner SourceOwnerKind { get; }
        public string SourceOwnerId { get; }
        public string SourceProvenanceToken { get; }
        public string SourceSlotId { get; }
        public GeneratedContentSlotCategory Category { get; }
        public GeneratedContentPoolKey PoolKey { get; }
        public string CanonicalLine { get; }
        internal string ReservationCanonicalLine { get; }

        public int CompareTo(GeneratedContentSlotAddress other) => other == null
            ? -1 : string.Compare(CanonicalLine, other.CanonicalLine, StringComparison.Ordinal);
        public bool Equals(GeneratedContentSlotAddress other) => other != null &&
            string.Equals(CanonicalLine, other.CanonicalLine, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as GeneratedContentSlotAddress);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(CanonicalLine);
        public override string ToString() => CanonicalLine;

        private static string Field(string name, string value) =>
            name + "=" + GeneratedContentPoolKey.Part(value);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedContentSlotIndexEntry :
        IComparable<GeneratedContentSlotIndexEntry>
    {
        internal GeneratedContentSlotIndexEntry(
            GeneratedContentSlotAddress address,
            GeneratedStableSpawnId stableSpawnId,
            bool availableForMandatoryUniquePreplacement)
        {
            Address = address;
            StableSpawnId = stableSpawnId;
            AvailableForMandatoryUniquePreplacement = availableForMandatoryUniquePreplacement;
            ReservationKey = BakingCanonicalDigest.HashCanonicalLines(
                new[] { address.ReservationCanonicalLine });
            DeterministicOrderKey = address.CanonicalLine;
            StableToken = string.Join("|", new[]
            {
                "CONTENT_SLOT_INDEX_ENTRY", DeterministicOrderKey,
                "SPAWN_ID=" + stableSpawnId.Value,
                "RESERVATION=" + ReservationKey,
                availableForMandatoryUniquePreplacement ? "MANDATORY_UNIQUE=1" : "MANDATORY_UNIQUE=0",
            });
        }

        public GeneratedContentSlotAddress Address { get; }
        public GeneratedStableSpawnId StableSpawnId { get; }
        public string ReservationKey { get; }
        public string DeterministicOrderKey { get; }
        public bool AvailableForMandatoryUniquePreplacement { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedContentSlotIndexEntry other) => other == null
            ? -1 : string.Compare(DeterministicOrderKey,
                other.DeterministicOrderKey, StringComparison.Ordinal);
    }

    public sealed class GeneratedContentSlotIndex
    {
        private readonly ReadOnlyCollection<GeneratedContentSlotIndexEntry> entries;

        internal GeneratedContentSlotIndex(
            IEnumerable<GeneratedContentSlotIndexEntry> sourceEntries,
            string map17AuditDigest,
            string map17PhaseExitVerdict,
            bool map18HandoffApproved,
            int map17WarningCount)
        {
            entries = new ReadOnlyCollection<GeneratedContentSlotIndexEntry>((sourceEntries ??
                Array.Empty<GeneratedContentSlotIndexEntry>()).OrderBy(value => value).ToArray());
            Map17AuditDigest = map17AuditDigest ?? string.Empty;
            Map17PhaseExitVerdict = map17PhaseExitVerdict ?? string.Empty;
            Map18HandoffApproved = map18HandoffApproved;
            Map17WarningCount = map17WarningCount;
            StableIdSetDigest = BakingCanonicalDigest.HashCanonicalLines(entries
                .Select(value => value.StableSpawnId.Value)
                .OrderBy(value => value, StringComparer.Ordinal));
            Digest = GeneratedContentSlotIndexDigest.Compute(this);
        }

        public const string PolicyVersion = "MAP18_01_CONTENT_SLOT_INDEX_V1";
        public const string DownstreamOwner = "MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT";
        public const bool OpensDownstreamTask = false;

        public IReadOnlyList<GeneratedContentSlotIndexEntry> Entries => entries;
        public int Count => entries.Count;
        public string Map17AuditDigest { get; }
        public string Map17PhaseExitVerdict { get; }
        public bool Map18HandoffApproved { get; }
        public int Map17WarningCount { get; }
        public bool Map17WarningsBlockHandoff => false;
        public string Digest { get; }
        public string StableIdSetDigest { get; }
        public int UniqueAddressCount => entries.Select(value => value.Address.CanonicalLine)
            .Distinct(StringComparer.Ordinal).Count();
        public int UniqueReservationKeyCount => entries.Select(value => value.ReservationKey)
            .Distinct(StringComparer.Ordinal).Count();
        public int UniqueStableSpawnIdCount => entries.Select(value => value.StableSpawnId.Value)
            .Distinct(StringComparer.Ordinal).Count();
        public int CategoryCount => entries.Select(value => value.Address.Category).Distinct().Count();
        public int PoolKeyCount => entries.Select(value => value.Address.PoolKey)
            .Distinct().Count();
        public int SourceOwnerKindCount => entries.Select(value => value.Address.SourceOwnerKind)
            .Distinct().Count();

        public IReadOnlyList<GeneratedContentSlotIndexEntry> BySector(
            GeneratedSectorCoordinate sector) => Snapshot(entries.Where(value =>
                Equals(value.Address.Sector, sector)));
        public IReadOnlyList<GeneratedContentSlotIndexEntry> BySectorAndSlice(
            GeneratedSectorCoordinate sector,
            int sliceIndex) => Snapshot(entries.Where(value =>
                Equals(value.Address.Sector, sector) && value.Address.SliceIndex == sliceIndex));
        public IReadOnlyList<GeneratedContentSlotIndexEntry> ByCategory(
            GeneratedContentSlotCategory category) => Snapshot(entries.Where(value =>
                value.Address.Category == category));
        public IReadOnlyList<GeneratedContentSlotIndexEntry> ByPoolKey(
            GeneratedContentPoolKey poolKey) => Snapshot(entries.Where(value =>
                Equals(value.Address.PoolKey, poolKey)));
        public IReadOnlyList<GeneratedContentSlotIndexEntry> BySourceOwner(
            GeneratedMarkerSlotOwner owner) => Snapshot(entries.Where(value =>
                value.Address.SourceOwnerKind == owner));
        public IReadOnlyList<GeneratedContentSlotIndexEntry> MandatoryUniqueCandidates() =>
            Snapshot(entries.Where(value => value.AvailableForMandatoryUniquePreplacement));
        public bool TryGetByReservationKey(
            string reservationKey,
            out GeneratedContentSlotIndexEntry entry)
        {
            entry = entries.SingleOrDefault(value => string.Equals(value.ReservationKey,
                reservationKey, StringComparison.Ordinal));
            return entry != null;
        }

        public int ActualContentPlacementCount => 0;
        public int WeightedPoolRollCount => 0;
        public int BudgetSpendCount => 0;
        public int RuntimeObjectSpawnCount => 0;
        public int GameObjectInstantiateCount => 0;
        public int GameObjectEnableCount => 0;
        public int GameObjectDisableCount => 0;
        public int GameObjectDestroyCount => 0;
        public int SystemIoFileWriteCount => 0;
        public int SystemIoFileReadCount => 0;
        public int DiskSaveFileCreateCount => 0;
        public int DiskLoadFileCreateCount => 0;
        public int UserSaveSlotWriteCount => 0;
        public int PlatformStorageWriteCount => 0;
        public int TilemapComponentWriteCount => 0;
        public int TilemapSetTileCallCount => 0;
        public int TilemapSetTilesCallCount => 0;
        public int TilemapSetTilesBlockCallCount => 0;
        public int TilemapClearAllTilesCallCount => 0;
        public int TilemapColliderCreationCount => 0;
        public int CompositeColliderCreationCount => 0;
        public int ColliderCreationCount => 0;
        public int RigidbodyCreationCount => 0;
        public int PhysicsQueryCount => 0;
        public int PhysicsSimulationCount => 0;
        public int SceneMutationCount => 0;
        public int PrefabMutationCount => 0;
        public int TilemapMutationCount => 0;
        public int CameraReadCount => 0;
        public int CameraWriteCount => 0;
        public int AddressablesLoadCount => 0;
        public int ResourcesLoadCount => 0;
        public int AssetDatabaseLoadCount => 0;
        public int AuthoringCsvEditCount => 0;
        public int GeneratedCsvCommitCount => 0;
        public int GeneratedAssetCommitCount => 0;
        public int ProductionSeedApprovalCount => 0;
        public int OptimizationRewriteCount => 0;
        public int BroadRefactorCount => 0;
        public int GuidIdentityUsageCount => 0;
        public int RandomIdentityUsageCount => 0;
        public int TimeIdentityUsageCount => 0;
        public int FrameIdentityUsageCount => 0;
        public int ObjectIdentityUsageCount => 0;
        public int FilePathIdentityUsageCount => 0;
        public bool Map18_02Started => false;

        private static IReadOnlyList<GeneratedContentSlotIndexEntry> Snapshot(
            IEnumerable<GeneratedContentSlotIndexEntry> values) =>
            new ReadOnlyCollection<GeneratedContentSlotIndexEntry>(values
                .OrderBy(value => value).ToArray());
    }
}
