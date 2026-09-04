using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public sealed class GeneratedSectorModificationBaseDigests :
        IEquatable<GeneratedSectorModificationBaseDigests>
    {
        public GeneratedSectorModificationBaseDigests(
            string geometryDigest,
            string bakeDigest,
            string cacheDigest,
            string windowDigest,
            string windowDiffDigest,
            string transitionPlanDigest)
        {
            GeometryDigest = geometryDigest ?? string.Empty;
            BakeDigest = bakeDigest ?? string.Empty;
            CacheDigest = cacheDigest ?? string.Empty;
            WindowDigest = windowDigest ?? string.Empty;
            WindowDiffDigest = windowDiffDigest ?? string.Empty;
            TransitionPlanDigest = transitionPlanDigest ?? string.Empty;
        }

        public string GeometryDigest { get; }
        public string BakeDigest { get; }
        public string CacheDigest { get; }
        public string WindowDigest { get; }
        public string WindowDiffDigest { get; }
        public string TransitionPlanDigest { get; }
        public bool IsValid => BakingCanonicalDigest.IsLowerHexSha256(GeometryDigest) &&
            BakingCanonicalDigest.IsLowerHexSha256(BakeDigest) &&
            BakingCanonicalDigest.IsLowerHexSha256(CacheDigest) &&
            BakingCanonicalDigest.IsLowerHexSha256(WindowDigest) &&
            BakingCanonicalDigest.IsLowerHexSha256(WindowDiffDigest) &&
            BakingCanonicalDigest.IsLowerHexSha256(TransitionPlanDigest);
        public string StableToken => string.Join("|", new[]
        {
            "SECTOR_MODIFICATION_BASE", GeometryDigest, BakeDigest, CacheDigest,
            WindowDigest, WindowDiffDigest, TransitionPlanDigest,
        });
        public bool Equals(GeneratedSectorModificationBaseDigests other) => other != null &&
            string.Equals(StableToken, other.StableToken, StringComparison.Ordinal);
        public override bool Equals(object obj) =>
            Equals(obj as GeneratedSectorModificationBaseDigests);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
    }

    public sealed class GeneratedSectorModificationAuthority
    {
        private readonly ReadOnlyCollection<GeneratedTilemapCellBakeRecord> sourceRecords;

        public GeneratedSectorModificationAuthority(
            GeneratedTerrainGeometrySnapshot geometry,
            GeneratedSectorCoordinate sector,
            IEnumerable<GeneratedTilemapCellBakeRecord> logicalBakeRecords,
            GeneratedSectorStreamingWindow streamingWindow,
            GeneratedSectorWindowDiff windowDiff,
            GeneratedSectorHandleTransitionPlan transitionPlan,
            string geometryDigest,
            string bakeDigest,
            string cacheDigest,
            string seedIdentity,
            string generatorVersion,
            string dataVersion)
        {
            Geometry = geometry;
            Sector = sector;
            StreamingWindow = streamingWindow;
            WindowDiff = windowDiff;
            TransitionPlan = transitionPlan;
            SeedIdentity = Normalize(seedIdentity);
            GeneratorVersion = Normalize(generatorVersion);
            DataVersion = Normalize(dataVersion);
            sourceRecords = new ReadOnlyCollection<GeneratedTilemapCellBakeRecord>(
                (logicalBakeRecords ?? Array.Empty<GeneratedTilemapCellBakeRecord>())
                .Where(value => value != null).OrderBy(value => value).ToArray());
            BaseDigests = new GeneratedSectorModificationBaseDigests(
                geometryDigest, bakeDigest, cacheDigest,
                streamingWindow == null ? string.Empty : streamingWindow.Digest,
                windowDiff == null ? string.Empty : windowDiff.Digest,
                transitionPlan == null ? string.Empty : transitionPlan.Digest);
            SourceRecordDigest = BakingCanonicalDigest.HashCanonicalLines(sourceRecords
                .Select(value => value.StableToken));
        }

        public GeneratedTerrainGeometrySnapshot Geometry { get; }
        public GeneratedSectorCoordinate Sector { get; }
        public GeneratedSectorStreamingWindow StreamingWindow { get; }
        public GeneratedSectorWindowDiff WindowDiff { get; }
        public GeneratedSectorHandleTransitionPlan TransitionPlan { get; }
        public GeneratedSectorModificationBaseDigests BaseDigests { get; }
        public string SeedIdentity { get; }
        public string GeneratorVersion { get; }
        public string DataVersion { get; }
        public IReadOnlyList<GeneratedTilemapCellBakeRecord> SourceRecords => sourceRecords;
        public int SourceRecordCount => sourceRecords.Count;
        public int SourceSectorCellCount => sourceRecords.Select(value =>
            value.SectorLocalIndex).Distinct().Count();
        public int SourceLayerCount => sourceRecords.Select(value => value.LayerId).Distinct().Count();
        public string SourceRecordDigest { get; }
        public bool IsValid => Geometry != null && Sector != null && Sector.IsInWorld &&
            SourceRecordCount == Geometry.SectorLayerRecordCount &&
            SourceSectorCellCount == Geometry.SectorCellCount &&
            SourceLayerCount == Geometry.LayersPerFinalCanvasCell &&
            sourceRecords.All(value => value.IsCoordinateValid(Geometry)) &&
            StreamingWindow != null && StreamingWindow.ContainsPreload(Sector) &&
            WindowDiff != null && TransitionPlan != null && BaseDigests.IsValid &&
            !string.IsNullOrEmpty(SeedIdentity) && !string.IsNullOrEmpty(GeneratorVersion) &&
            !string.IsNullOrEmpty(DataVersion) &&
            BakingCanonicalDigest.IsLowerHexSha256(SourceRecordDigest);

        public bool ContainsTarget(GeneratedSectorModificationTarget target) =>
            target != null && target.LocalIndex != null && target.LocalIndex.IsValid &&
            target.IsLayerValid && !string.IsNullOrEmpty(target.SourceProvenanceToken) &&
            sourceRecords.Any(value => value.SectorLocalIndex == target.LocalIndex.Value &&
                (int)value.LayerId == target.LayerId &&
                (string.Equals(value.SourceLayerStableToken,
                     target.SourceProvenanceToken, StringComparison.Ordinal) ||
                 string.Equals(value.SourceCellToken,
                     target.SourceProvenanceToken, StringComparison.Ordinal) ||
                 string.Equals(value.ProvenanceId,
                     target.SourceProvenanceToken, StringComparison.Ordinal)));

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
    }

    public sealed class GeneratedModifiedSectorSnapshot :
        IComparable<GeneratedModifiedSectorSnapshot>
    {
        internal GeneratedModifiedSectorSnapshot(
            GeneratedSectorModificationSet modificationSet,
            GeneratedSectorRuntimeState sourceHandleState,
            GeneratedSectorRuntimeState targetHandleState)
        {
            ModificationSet = modificationSet ?? throw new ArgumentNullException(nameof(modificationSet));
            SourceHandleState = sourceHandleState;
            TargetHandleState = targetHandleState;
            Digest = GeneratedSectorModificationDigest.ComputeSectorSnapshot(this);
        }

        public GeneratedSectorModificationSet ModificationSet { get; }
        public GeneratedSectorCoordinate Sector => ModificationSet.Sector;
        public int DirtyRevision => ModificationSet.DirtyRevision;
        public GeneratedSectorModificationBaseDigests BaseDigests => ModificationSet.BaseDigests;
        public IReadOnlyList<GeneratedSectorModificationRecord> Records => ModificationSet.Records;
        public int RecordCount => ModificationSet.RecordCount;
        public GeneratedSectorRuntimeState SourceHandleState { get; }
        public GeneratedSectorRuntimeState TargetHandleState { get; }
        public string Digest { get; }
        public int CompareTo(GeneratedModifiedSectorSnapshot other) => other == null
            ? -1 : Sector.CompareTo(other.Sector);
    }

    public sealed class GeneratedSectorModificationStorage
    {
        private readonly ReadOnlyCollection<GeneratedModifiedSectorSnapshot> sectors;

        internal GeneratedSectorModificationStorage(
            IEnumerable<GeneratedModifiedSectorSnapshot> sourceSectors)
        {
            sectors = new ReadOnlyCollection<GeneratedModifiedSectorSnapshot>((sourceSectors ??
                Array.Empty<GeneratedModifiedSectorSnapshot>()).Where(value => value != null)
                .GroupBy(value => value.Sector).Select(group => group.Last())
                .OrderBy(value => value).ToArray());
            Digest = GeneratedSectorModificationDigest.ComputeStorage(this);
        }

        public IReadOnlyList<GeneratedModifiedSectorSnapshot> ModifiedSectors => sectors;
        public int ModifiedSectorCount => sectors.Count;
        public int TotalRecordCount => sectors.Sum(value => value.RecordCount);
        public string Digest { get; }
        public int DurableSaveWriteCount => 0;
        public int SaveManifestFileCount => 0;
        public int RegenerationApplyExecutionCount => 0;
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
        public int GameObjectInstantiationCount => 0;
        public int GameObjectEnableCount => 0;
        public int GameObjectDisableCount => 0;
        public int GameObjectDestroyCount => 0;
        public int AddressablesLoadCount => 0;
        public int ResourcesLoadCount => 0;
        public int AssetDatabaseLoadCount => 0;
        public int AuthoringCsvEditCount => 0;
        public int GeneratedCsvCommitCount => 0;
        public int GeneratedAssetCommitCount => 0;
        public int RuntimeObjectSpawnCount => 0;
        public int PopulationStableSpawnIdCount => 0;
        public int ProductionSeedApprovalCount => 0;

        public GeneratedModifiedSectorSnapshot Find(GeneratedSectorCoordinate sector) =>
            sector == null ? null : sectors.SingleOrDefault(value => value.Sector.Equals(sector));
    }

    public sealed class GeneratedSectorModificationApplyCommand :
        IComparable<GeneratedSectorModificationApplyCommand>
    {
        internal GeneratedSectorModificationApplyCommand(
            int ordinal,
            GeneratedSectorModificationRecord record)
        {
            Ordinal = ordinal;
            Record = record ?? throw new ArgumentNullException(nameof(record));
            StableToken = "SECTOR_MODIFICATION_APPLY|" + Number(Ordinal) + "|" +
                Record.StableToken;
        }

        public int Ordinal { get; }
        public GeneratedSectorModificationRecord Record { get; }
        public GeneratedSectorModificationKind Kind => Record.Kind;
        public GeneratedSectorModificationTarget Target => Record.Target;
        public bool AffectsLogicalLayer => Kind == GeneratedSectorModificationKind.DestroyTile ||
            Kind == GeneratedSectorModificationKind.ReplaceTile;
        public bool AffectsSlotState => Kind == GeneratedSectorModificationKind.CollectPickup ||
            Kind == GeneratedSectorModificationKind.ConsumeSlot;
        public bool AffectsDeviceState => Kind == GeneratedSectorModificationKind.ChangeDeviceState;
        public string StableToken { get; }
        public int CompareTo(GeneratedSectorModificationApplyCommand other) => other == null
            ? -1 : Ordinal.CompareTo(other.Ordinal);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedSectorModificationApplyPlan
    {
        private readonly ReadOnlyCollection<GeneratedSectorModificationApplyCommand> commands;

        internal GeneratedSectorModificationApplyPlan(
            GeneratedSectorModificationStorage storage,
            GeneratedModifiedSectorSnapshot sectorSnapshot,
            IEnumerable<GeneratedSectorModificationApplyCommand> sourceCommands,
            GeneratedSectorRuntimeHandle sourceHandle,
            GeneratedSectorRuntimeHandle sleepingModifiedHandle,
            string sourceLogicalRecordsDigest)
        {
            Storage = storage;
            SectorSnapshot = sectorSnapshot;
            commands = new ReadOnlyCollection<GeneratedSectorModificationApplyCommand>((sourceCommands ??
                Array.Empty<GeneratedSectorModificationApplyCommand>()).OrderBy(value => value).ToArray());
            SourceHandle = sourceHandle;
            SleepingModifiedHandle = sleepingModifiedHandle;
            SourceLogicalRecordsDigest = sourceLogicalRecordsDigest ?? string.Empty;
            Digest = GeneratedSectorModificationDigest.ComputeApplyPlan(this);
        }

        public GeneratedSectorModificationStorage Storage { get; }
        public GeneratedModifiedSectorSnapshot SectorSnapshot { get; }
        public IReadOnlyList<GeneratedSectorModificationApplyCommand> Commands => commands;
        public GeneratedSectorRuntimeHandle SourceHandle { get; }
        public GeneratedSectorRuntimeHandle SleepingModifiedHandle { get; }
        public string SourceLogicalRecordsDigest { get; }
        public int CommandCount => commands.Count;
        public int LogicalLayerCommandCount => commands.Count(value => value.AffectsLogicalLayer);
        public int SlotStateCommandCount => commands.Count(value => value.AffectsSlotState);
        public int DeviceStateCommandCount => commands.Count(value => value.AffectsDeviceState);
        public int InPlaceInputMutationCount => 0;
        public int DurableSaveWriteCount => 0;
        public int TilemapWriteCount => 0;
        public int GameObjectChangeCount => 0;
        public string Digest { get; }
    }

    public static class GeneratedSectorModificationDigest
    {
        public static string ComputeBase(GeneratedSectorModificationBaseDigests value) =>
            value == null ? string.Empty : BakingCanonicalDigest.HashCanonicalLines(
                new[] { value.StableToken });

        public static string ComputeSet(GeneratedSectorModificationSet value)
        {
            if (value == null) return string.Empty;
            var lines = new List<string>
            {
                "MODIFICATION_SCHEMA|" + GeneratedSectorModificationStore.SchemaVersion,
                "SECTOR|" + (value.Sector == null ? "MISSING" : value.Sector.ToString()),
                "DIRTY_REVISION|" + Number(value.DirtyRevision),
                "BASE|" + ComputeBase(value.BaseDigests),
            };
            lines.AddRange(value.Records.OrderBy(record => record)
                .Select(record => record.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        public static string ComputeSectorSnapshot(GeneratedModifiedSectorSnapshot value) =>
            value == null ? string.Empty : BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MODIFIED_SECTOR|" + value.Sector,
                "SET|" + value.ModificationSet.Digest,
                "HANDLE_STATE|" + value.SourceHandleState.ToString().ToUpperInvariant() + "|" +
                    value.TargetHandleState.ToString().ToUpperInvariant(),
            });

        public static string ComputeStorage(GeneratedSectorModificationStorage value)
        {
            if (value == null) return string.Empty;
            var lines = new List<string>
            {
                "MODIFICATION_STORAGE|" + GeneratedSectorModificationStore.SchemaVersion,
                "COUNTS|" + Number(value.ModifiedSectorCount) + "|" + Number(value.TotalRecordCount),
                "NO_SIDE_EFFECTS|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0",
            };
            lines.AddRange(value.ModifiedSectors.OrderBy(sector => sector)
                .Select(sector => sector.Digest));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        public static string ComputeApplyPlan(GeneratedSectorModificationApplyPlan value)
        {
            if (value == null) return string.Empty;
            var lines = new List<string>
            {
                "MODIFICATION_APPLY_PLAN|" + GeneratedSectorModificationStore.SchemaVersion,
                "STORAGE|" + (value.Storage == null ? string.Empty : value.Storage.Digest),
                "SECTOR|" + (value.SectorSnapshot == null ? string.Empty : value.SectorSnapshot.Digest),
                "SOURCE_RECORDS|" + value.SourceLogicalRecordsDigest,
                "HANDLE|" + (value.SleepingModifiedHandle == null
                    ? string.Empty : value.SleepingModifiedHandle.Digest),
                "COUNTS|" + Number(value.CommandCount) + "|" +
                    Number(value.LogicalLayerCommandCount) + "|" +
                    Number(value.SlotStateCommandCount) + "|" +
                    Number(value.DeviceStateCommandCount),
                "NO_SIDE_EFFECTS|0|0|0",
            };
            lines.AddRange(value.Commands.OrderBy(command => command)
                .Select(command => command.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        public static bool IsLowerHexSha256(string value) =>
            BakingCanonicalDigest.IsLowerHexSha256(value);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
