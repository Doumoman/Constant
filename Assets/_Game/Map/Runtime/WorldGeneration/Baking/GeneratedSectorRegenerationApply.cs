using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public sealed class GeneratedSectorRegenerationRequest
    {
        public GeneratedSectorRegenerationRequest(
            GeneratedWorldSaveManifest manifest,
            GeneratedSectorCoordinate sector,
            GeneratedSectorModificationAuthority regeneratedAuthority,
            string seedIdentity,
            string generatorVersion,
            string dataVersion,
            string geometryDigest,
            string placementDigest,
            string bakeDigest,
            string cacheDigest,
            string windowHandleDigest,
            string storageDigest)
        {
            Manifest = manifest;
            Sector = sector;
            RegeneratedAuthority = regeneratedAuthority;
            SeedIdentity = seedIdentity ?? string.Empty;
            GeneratorVersion = generatorVersion ?? string.Empty;
            DataVersion = dataVersion ?? string.Empty;
            GeometryDigest = geometryDigest ?? string.Empty;
            PlacementDigest = placementDigest ?? string.Empty;
            BakeDigest = bakeDigest ?? string.Empty;
            CacheDigest = cacheDigest ?? string.Empty;
            WindowHandleDigest = windowHandleDigest ?? string.Empty;
            StorageDigest = storageDigest ?? string.Empty;
        }

        public GeneratedWorldSaveManifest Manifest { get; }
        public GeneratedSectorCoordinate Sector { get; }
        public GeneratedSectorModificationAuthority RegeneratedAuthority { get; }
        public string SeedIdentity { get; }
        public string GeneratorVersion { get; }
        public string DataVersion { get; }
        public string GeometryDigest { get; }
        public string PlacementDigest { get; }
        public string BakeDigest { get; }
        public string CacheDigest { get; }
        public string WindowHandleDigest { get; }
        public string StorageDigest { get; }
        public GeneratedModifiedSectorManifestEntry Entry => Manifest == null
            ? null : Manifest.Find(Sector);
    }

    public sealed class GeneratedSectorRegenerationApplyPlan
    {
        private readonly ReadOnlyCollection<GeneratedSectorModificationApplyCommand> commands;

        internal GeneratedSectorRegenerationApplyPlan(
            GeneratedWorldSaveManifest manifest,
            GeneratedModifiedSectorManifestEntry entry,
            IEnumerable<GeneratedSectorModificationApplyCommand> sourceCommands,
            GeneratedSectorModificationSet outputModificationSet,
            string sourceLogicalRecordDigest)
        {
            Manifest = manifest;
            Entry = entry;
            commands = new ReadOnlyCollection<GeneratedSectorModificationApplyCommand>((sourceCommands ??
                Array.Empty<GeneratedSectorModificationApplyCommand>()).OrderBy(value => value).ToArray());
            OutputModificationSet = outputModificationSet;
            SourceLogicalRecordDigest = sourceLogicalRecordDigest ?? string.Empty;
            Digest = GeneratedSaveManifestDigest.ComputeRegenerationApply(this);
        }

        public GeneratedWorldSaveManifest Manifest { get; }
        public GeneratedModifiedSectorManifestEntry Entry { get; }
        public IReadOnlyList<GeneratedSectorModificationApplyCommand> Commands => commands;
        public GeneratedSectorModificationSet OutputModificationSet { get; }
        public string SourceLogicalRecordDigest { get; }
        public int CommandCount => commands.Count;
        public int DestroyTileCommandCount => Count(GeneratedSectorModificationKind.DestroyTile);
        public int ReplaceTileCommandCount => Count(GeneratedSectorModificationKind.ReplaceTile);
        public int CollectPickupCommandCount => Count(GeneratedSectorModificationKind.CollectPickup);
        public int ChangeDeviceStateCommandCount => Count(GeneratedSectorModificationKind.ChangeDeviceState);
        public int ConsumeSlotCommandCount => Count(GeneratedSectorModificationKind.ConsumeSlot);
        public int InputInPlaceMutationCount => 0;
        public int SystemIoFileReadCount => 0;
        public int SystemIoFileWriteCount => 0;
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
        public string Digest { get; }

        private int Count(GeneratedSectorModificationKind kind) =>
            commands.Count(value => value.Kind == kind);
    }

    public sealed class GeneratedSectorRegenerationApplyResult
    {
        private readonly ReadOnlyCollection<GeneratedSaveManifestValidationFailure> failures;

        internal GeneratedSectorRegenerationApplyResult(
            GeneratedSectorRegenerationApplyPlan plan,
            IEnumerable<GeneratedSaveManifestValidationFailure> sourceFailures)
        {
            Plan = plan;
            failures = new ReadOnlyCollection<GeneratedSaveManifestValidationFailure>(
                (sourceFailures ?? Array.Empty<GeneratedSaveManifestValidationFailure>())
                .Distinct().OrderBy(value => value).ToArray());
        }

        public bool Success => Plan != null && failures.Count == 0;
        public GeneratedSectorRegenerationApplyPlan Plan { get; }
        public IReadOnlyList<GeneratedSaveManifestValidationFailure> Failures => failures;
        public int PartialApplyMutationCount => 0;
    }
}
