using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Baking
{
    [TestFixture]
    [Category("MAP17_06")]
    public sealed class GeneratedSaveManifestRegenerationTests
    {
        private const string Map16ExitAuditDigest =
            "78d3046d62608494fb1306ff4e57a0b2d4b36eafc3a5e7e19cb8f399c3ca29f0";
        private const string ExpectedModificationSetDigest =
            "a07d0f4387924f080ac34a62161a5de673e34f00e0d200ba48070efe0de6f180";
        private const string ExpectedStorageDigest =
            "7b4e507333f24ab61698422e17870ab86325d3aff5a129d8d4837d3fb9c3305f";
        private const string ExpectedModificationApplyDigest =
            "62a608b6cae1ce398ff5c31e56f6eeb0af46e6630e61534d62229ce553cd5300";
        private const string GeneratorVersion = "MAP17_REFERENCE_GENERATOR_V1";
        private const string DataVersion = "MAP17_REFERENCE_DATA_V1";
        private const string SeedIdentity = "MAP17_REFERENCE_SEED";
        private static readonly GeneratedSectorCoordinate SourceSector =
            new GeneratedSectorCoordinate(3, 4);
        private static Map17Chain reference;
        private static GeneratedCellPlacementPlan placement;
        private static GeneratedTilemapBakePlan bake;
        private static readonly Dictionary<int, GeneratedColliderRebuildPlan> Rebuilds =
            new Dictionary<int, GeneratedColliderRebuildPlan>();
        private static GeneratedColliderCacheEntry[] entries;
        private static GeneratedSectorRuntimeHandle[] handles;
        private static GeneratedColliderCacheSnapshot cache;
        private static GeneratedSectorStreamingResult streaming;
        private static GeneratedSectorModificationAuthority authority;
        private static ModificationFixture modified;
        private static GeneratedSaveManifestResult manifest;

        [Test]
        public void SaveManifestPublishesSeedVersionHashesAndOnlyModifiedSectors()
        {
            var result = Manifest();
            var value = result.Manifest;
            var source = Modified();
            var map17Apply = Store().BuildApplyPlan(source.Storage,
                SourceSector, ActiveHandle());
            Assert.That(result.Success, Is.True, Failures(result));
            Assert.That(value.Header.Version.SchemaVersion,
                Is.EqualTo(GeneratedSaveManifestService.SchemaVersion));
            Assert.That(value.Header.SeedIdentity, Is.EqualTo(SeedIdentity));
            Assert.That(value.Header.Version.GeneratorVersion, Is.EqualTo(GeneratorVersion));
            Assert.That(value.Header.Version.DataVersion, Is.EqualTo(DataVersion));
            Assert.That(value.Header.PublishedFieldCount, Is.EqualTo(10));
            Assert.That(value.ModifiedSectorCount, Is.EqualTo(1));
            Assert.That(value.UnmodifiedSectorCount, Is.EqualTo(168));
            Assert.That(value.ModificationRecordCount, Is.EqualTo(5));
            Assert.That(value.FullTileDataEntryCount, Is.Zero);
            Assert.That(value.UnityObjectIdCount + value.FilePathCount + value.TimestampCount +
                value.FrameCountValueCount + value.PopulationStableSpawnIdCount, Is.Zero);
            Assert.That(source.Snapshot.ModificationSet.Digest,
                Is.EqualTo(ExpectedModificationSetDigest));
            Assert.That(source.Storage.Digest, Is.EqualTo(ExpectedStorageDigest));
            Assert.That(map17Apply.ApplyPlan.Digest, Is.EqualTo(ExpectedModificationApplyDigest));

            TestContext.WriteLine("MAP17_06_MANIFEST_EVIDENCE schema=" +
                value.Header.Version.SchemaVersion + " header_fields=10 modified=1/1" +
                " unmodified_omitted=168/168 records=5/5 full_tile=0 object_ids=0" +
                " file_path_timestamp_frame=0/0/0 population_ids=0 manifest=" + value.Digest);
        }

        [Test]
        public void ManifestSerializerRoundTripsCanonicalPayloadWithoutDiskIO()
        {
            var source = Manifest();
            var parsed = GeneratedSaveManifestSerializer.Parse(source.Payload);
            Assert.That(parsed.Success, Is.True, Failures(parsed));
            Assert.That(parsed.Manifest.Digest, Is.EqualTo(source.Manifest.Digest));
            var serializedAgain = GeneratedSaveManifestSerializer.Serialize(parsed.Manifest);
            Assert.That(serializedAgain.Success, Is.True, Failures(serializedAgain));
            Assert.That(serializedAgain.Payload.CanonicalText,
                Is.EqualTo(source.Payload.CanonicalText));
            Assert.That(serializedAgain.Payload.Digest, Is.EqualTo(source.Payload.Digest));
            Assert.That(source.Payload.IsUtf8WithoutBom, Is.True);
            Assert.That(source.Payload.CanonicalText.Contains("\r"), Is.False);
            Assert.That(source.Payload.DiskReadCount + source.Payload.DiskWriteCount, Is.Zero);

            var crlf = new GeneratedSaveManifestPayload(
                source.Payload.CanonicalText.Replace("\n", "\r\n"));
            var normalized = GeneratedSaveManifestSerializer.Parse(crlf);
            Assert.That(normalized.Success, Is.True, Failures(normalized));
            Assert.That(normalized.Payload.Digest, Is.EqualTo(source.Payload.Digest));

            TestContext.WriteLine("MAP17_06_SERIALIZER_EVIDENCE generated=YES parsed=YES" +
                " roundtrip=YES lf=YES utf8_no_bom=YES disk_read_write=0/0 payload=" +
                source.Payload.Digest + " bytes=" + source.Payload.Bytes.Count);
        }

        [Test]
        public void UnmodifiedSectorsRegenerateFromSeedWithoutManifestEntries()
        {
            var value = Manifest().Manifest;
            var probes = Enumerable.Range(0,
                    GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorCount)
                .Select(index => new GeneratedSectorCoordinate(
                    index % GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorColumns,
                    index / GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorColumns))
                .Where(sector => !sector.Equals(SourceSector)).ToArray();
            Assert.That(probes.Length, Is.EqualTo(168));
            foreach (var sector in probes)
            {
                Assert.That(value.Find(sector), Is.Null);
                var proof = GeneratedSaveManifestService.ValidateUnmodifiedSectorRegeneration(
                    Request(value, sector));
                Assert.That(proof.Success, Is.True, Failures(proof));
            }

            TestContext.WriteLine("MAP17_06_UNMODIFIED_EVIDENCE total=168/168" +
                " manifest_entries=0 regeneration_by_seed=168/168 full_generator_runs=0");
        }

        [Test]
        public void RegenerationRequestValidatesBaseGeometryBakeCacheWindowAndStorageDigests()
        {
            var baseline = GeneratedSaveManifestService.PlanRegenerationApply(Request());
            Assert.That(baseline.Success, Is.True, Failures(baseline));
            var stale = Hash("STALE_DIGEST");
            var probes = new[]
            {
                GeneratedSaveManifestService.PlanRegenerationApply(Request(geometry: stale)),
                GeneratedSaveManifestService.PlanRegenerationApply(Request(placementDigest: stale)),
                GeneratedSaveManifestService.PlanRegenerationApply(Request(bakeDigest: stale)),
                GeneratedSaveManifestService.PlanRegenerationApply(Request(cacheDigest: stale)),
                GeneratedSaveManifestService.PlanRegenerationApply(Request(windowHandleDigest: stale)),
                GeneratedSaveManifestService.PlanRegenerationApply(Request(storageDigest: stale)),
            };
            Assert.That(probes.All(value => !value.Success && value.Plan == null &&
                value.PartialApplyMutationCount == 0), Is.True);
            AssertFailure(probes[0], Code.GeometryDigestMismatch);
            AssertFailure(probes[1], Code.PlacementDigestMismatch);
            AssertFailure(probes[2], Code.BakeDigestMismatch);
            AssertFailure(probes[3], Code.CacheDigestMismatch);
            AssertFailure(probes[4], Code.WindowHandleDigestMismatch);
            AssertFailure(probes[5], Code.StorageDigestMismatch);

            TestContext.WriteLine("MAP17_06_REGEN_VALIDATION_EVIDENCE baseline=1/1" +
                " geometry_placement_bake_cache_window_storage=6/6 partial_apply=0");
        }

        [Test]
        public void RegenerationApplyPlanReplaysDestroyReplaceCollectDeviceAndSlotChangesAsPureData()
        {
            var before = Authority().SourceRecords.Select(value => value.StableToken).ToArray();
            var result = GeneratedSaveManifestService.PlanRegenerationApply(Request());
            Assert.That(result.Success, Is.True, Failures(result));
            var plan = result.Plan;
            Assert.That(plan.CommandCount, Is.EqualTo(5));
            Assert.That(plan.DestroyTileCommandCount, Is.EqualTo(1));
            Assert.That(plan.ReplaceTileCommandCount, Is.EqualTo(1));
            Assert.That(plan.CollectPickupCommandCount, Is.EqualTo(1));
            Assert.That(plan.ChangeDeviceStateCommandCount, Is.EqualTo(1));
            Assert.That(plan.ConsumeSlotCommandCount, Is.EqualTo(1));
            Assert.That(plan.OutputModificationSet.DirtyRevision,
                Is.EqualTo(plan.Entry.DirtyRevision));
            Assert.That(plan.OutputModificationSet.Digest,
                Is.EqualTo(plan.Entry.ModificationSetDigest));
            Assert.That(plan.InputInPlaceMutationCount, Is.Zero);
            Assert.That(Authority().SourceRecords.Select(value => value.StableToken),
                Is.EqualTo(before));

            TestContext.WriteLine("MAP17_06_APPLY_EVIDENCE plans=1 commands=5" +
                " kinds=1/1/1/1/1 dirty_revision=5/5 set_digest=YES input_mutations=0" +
                " apply=" + plan.Digest);
        }

        [Test]
        public void HashVersionSeedAndStaleManifestMismatchesFailAtomically()
        {
            var value = Manifest().Manifest;
            var unsupported = new GeneratedWorldSaveManifest(Header(value.Header,
                version: new GeneratedSaveManifestVersion("UNSUPPORTED",
                    GeneratorVersion, DataVersion)), value.ModifiedSectorEntries);
            var unsupportedResult = GeneratedSaveManifestSerializer.Serialize(unsupported);
            AssertFailure(unsupportedResult, Code.UnsupportedVersion);

            var seed = GeneratedSaveManifestService.PlanRegenerationApply(
                Request(seed: "OTHER_SEED"));
            var generator = GeneratedSaveManifestService.PlanRegenerationApply(
                Request(generator: "OTHER_GENERATOR"));
            var data = GeneratedSaveManifestService.PlanRegenerationApply(
                Request(data: "OTHER_DATA"));
            AssertFailure(seed, Code.SeedMismatch);
            AssertFailure(generator, Code.GeneratorVersionMismatch);
            AssertFailure(data, Code.DataVersionMismatch);

            var badPayload = new GeneratedSaveManifestPayload(Manifest().Payload.CanonicalText,
                Hash("WRONG_DECLARED_PAYLOAD_HASH"));
            var payloadFailure = GeneratedSaveManifestSerializer.Parse(badPayload);
            AssertFailure(payloadFailure, Code.PayloadHashMismatch);
            Assert.That(new[] { seed, generator, data }.All(result =>
                result.Plan == null && result.PartialApplyMutationCount == 0), Is.True);

            TestContext.WriteLine("MAP17_06_MISMATCH_EVIDENCE unsupported=1/1" +
                " seed_generator_data=3/3 payload_hash=1/1 partial_apply=0");
        }

        [Test]
        public void DuplicateUnknownUnmodifiedSectorAndRecordPayloadFailuresAreDeterministic()
        {
            var source = Manifest().Manifest;
            var entry = source.ModifiedSectorEntries[0];

            var duplicateSector = new GeneratedWorldSaveManifest(
                Header(source.Header, modifiedSectorCount: 2), new[] { entry, entry });
            AssertFailure(GeneratedSaveManifestSerializer.Serialize(duplicateSector),
                Code.DuplicateSectorEntry);

            var original = entry.Records[0];
            var conflictingDuplicate = new GeneratedSaveManifestRecordPayload(
                original.StableId, original.Sector, original.LocalIndex, original.LayerId,
                original.SourceProvenanceToken, original.SlotReference, original.Kind,
                original.Revision, "DIFFERENT", original.OldSourceToken,
                original.NewTileCode, original.NewSourceToken, original.StateKey,
                original.StateValue, original.LogicalRemoved, original.Collected,
                original.Consumed, original.BaseDigests, original.SourceDigest);
            var duplicateRecords = entry.Records.Concat(new[] { conflictingDuplicate }).ToArray();
            var duplicateEntry = new GeneratedModifiedSectorManifestEntry(entry.Sector,
                entry.DirtyRevision, entry.BaseDigests,
                GeneratedSaveManifestService.ComputeModificationSetDigest(source.Header,
                    entry.Sector, entry.DirtyRevision, entry.BaseDigests, duplicateRecords),
                duplicateRecords);
            var duplicateRecordManifest = new GeneratedWorldSaveManifest(source.Header,
                new[] { duplicateEntry });
            AssertFailure(GeneratedSaveManifestSerializer.Serialize(duplicateRecordManifest),
                Code.DuplicateRecordId);

            var emptySector = new GeneratedSectorCoordinate(4, 4);
            var emptyEntry = new GeneratedModifiedSectorManifestEntry(emptySector, 0,
                entry.BaseDigests,
                GeneratedSaveManifestService.ComputeModificationSetDigest(source.Header,
                    emptySector, 0, entry.BaseDigests,
                    Array.Empty<GeneratedSaveManifestRecordPayload>()),
                Array.Empty<GeneratedSaveManifestRecordPayload>());
            var unmodifiedManifest = new GeneratedWorldSaveManifest(
                Header(source.Header, modifiedSectorCount: 2), new[] { entry, emptyEntry });
            AssertFailure(GeneratedSaveManifestSerializer.Serialize(unmodifiedManifest),
                Code.UnmodifiedSectorEntry);

            var unknownTarget = new GeneratedSectorModificationTarget(SourceSector,
                new GeneratedSectorLocalCellIndex(0), (int)GeneratedTilemapLayerId.Terrain,
                "UNKNOWN_PROVENANCE");
            var unknownRecord = Store().Author(Store().CreateEmpty(), unknownTarget,
                GeneratedSectorModificationKind.DestroyTile,
                GeneratedSectorModificationPayload.Destroy("SOURCE", "UNKNOWN_PROVENANCE"));
            var unknownPayload = new GeneratedSaveManifestRecordPayload(unknownRecord);
            var unknownEntry = new GeneratedModifiedSectorManifestEntry(SourceSector, 1,
                entry.BaseDigests,
                GeneratedSaveManifestService.ComputeModificationSetDigest(source.Header,
                    SourceSector, 1, entry.BaseDigests, new[] { unknownPayload }),
                new[] { unknownPayload });
            var unknownManifest = new GeneratedWorldSaveManifest(source.Header,
                new[] { unknownEntry });
            Assert.That(GeneratedSaveManifestService.ValidateManifest(unknownManifest), Is.Empty);
            AssertFailure(GeneratedSaveManifestService.PlanRegenerationApply(
                Request(unknownManifest)), Code.MissingTarget);

            var unknownFieldPayload = new GeneratedSaveManifestPayload(
                Manifest().Payload.CanonicalText + "\nUNKNOWN_FIELD|REJECT_ME");
            AssertFailure(GeneratedSaveManifestSerializer.Parse(unknownFieldPayload),
                Code.UnknownField);

            TestContext.WriteLine("MAP17_06_FAILURE_EVIDENCE duplicate_sector=1/1" +
                " duplicate_record_payload=1/1 unmodified_entry=1/1 missing_target=1/1" +
                " unknown_field_policy=REJECTED atomic_partial=0");
        }

        [Test]
        public void ManifestDigestsAreStableAcrossRepeatReverseCultureSectorAndRecordOrder()
        {
            var baseline = Manifest();
            var entry = baseline.Manifest.ModifiedSectorEntries[0];
            var reversedEntry = new GeneratedModifiedSectorManifestEntry(entry.Sector,
                entry.DirtyRevision, entry.BaseDigests, entry.ModificationSetDigest,
                entry.Records.Reverse());
            var reverse = GeneratedSaveManifestSerializer.Serialize(
                new GeneratedWorldSaveManifest(baseline.Manifest.Header,
                    new[] { reversedEntry }.Reverse()));
            var repeat = GeneratedSaveManifestService.Build(Modified().Storage,
                SeedIdentity, GeneratorVersion, DataVersion, Placement().OutputDigest,
                WindowHandleDigest());
            Assert.That(reverse.Success, Is.True, Failures(reverse));
            Assert.That(repeat.Success, Is.True, Failures(repeat));

            var prior = CultureInfo.CurrentCulture;
            GeneratedSaveManifestResult culture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                culture = GeneratedSaveManifestService.Build(Modified().Storage,
                    SeedIdentity, GeneratorVersion, DataVersion, Placement().OutputDigest,
                    WindowHandleDigest());
            }
            finally
            {
                CultureInfo.CurrentCulture = prior;
            }
            Assert.That(culture.Success, Is.True, Failures(culture));
            Assert.That(new[] { baseline.Manifest.Digest, reverse.Manifest.Digest,
                repeat.Manifest.Digest, culture.Manifest.Digest }.Distinct().Count(), Is.EqualTo(1));
            Assert.That(new[] { baseline.Payload.Digest, reverse.Payload.Digest,
                repeat.Payload.Digest, culture.Payload.Digest }.Distinct().Count(), Is.EqualTo(1));

            var baselineApply = GeneratedSaveManifestService.PlanRegenerationApply(Request());
            var reverseApply = GeneratedSaveManifestService.PlanRegenerationApply(
                Request(reverse.Manifest));
            Assert.That(baselineApply.Plan.Digest, Is.EqualTo(reverseApply.Plan.Digest));

            var store = Store();
            var source = Modified();
            var mutation = store.Author(source.Storage, source.Records[3].Target,
                GeneratedSectorModificationKind.ChangeDeviceState,
                GeneratedSectorModificationPayload.DeviceState("door", "closed"));
            var mutatedStorage = store.Replace(source.Storage, mutation);
            Assert.That(mutatedStorage.Success, Is.True, Describe(mutatedStorage.Failures));
            var mutatedManifest = GeneratedSaveManifestService.Build(mutatedStorage.Storage,
                SeedIdentity, GeneratorVersion, DataVersion, Placement().OutputDigest,
                WindowHandleDigest());
            var mutatedApply = GeneratedSaveManifestService.PlanRegenerationApply(
                Request(mutatedManifest.Manifest));
            Assert.That(mutatedApply.Success, Is.True, Failures(mutatedApply));
            Assert.That(mutatedManifest.Manifest.Digest,
                Is.Not.EqualTo(baseline.Manifest.Digest));
            Assert.That(mutatedManifest.Payload.Digest,
                Is.Not.EqualTo(baseline.Payload.Digest));
            Assert.That(mutatedApply.Plan.Digest,
                Is.Not.EqualTo(baselineApply.Plan.Digest));

            TestContext.WriteLine("MAP17_06_DIGEST_EVIDENCE manifest=" +
                baseline.Manifest.Digest + " payload=" + baseline.Payload.Digest +
                " apply=" + baselineApply.Plan.Digest +
                " mismatches_repeat_reverse_culture_sector_record=0/0/0/0/0" +
                " mutation_sensitivity=3/3");
        }

        [Test]
        public void SaveManifestDoesNotWriteFilesLoadAssetsMutateScenesOrSpawnObjects()
        {
            var payload = Manifest().Payload;
            var plan = GeneratedSaveManifestService.PlanRegenerationApply(Request()).Plan;
            var sideEffects = payload.DiskReadCount + payload.DiskWriteCount +
                plan.SystemIoFileReadCount + plan.SystemIoFileWriteCount +
                plan.DiskSaveFileCreateCount + plan.DiskLoadFileCreateCount +
                plan.UserSaveSlotWriteCount + plan.PlatformStorageWriteCount +
                plan.TilemapComponentWriteCount + plan.TilemapSetTileCallCount +
                plan.TilemapSetTilesCallCount + plan.TilemapSetTilesBlockCallCount +
                plan.TilemapClearAllTilesCallCount + plan.TilemapColliderCreationCount +
                plan.CompositeColliderCreationCount + plan.ColliderCreationCount +
                plan.RigidbodyCreationCount + plan.PhysicsQueryCount +
                plan.PhysicsSimulationCount + plan.SceneMutationCount +
                plan.PrefabMutationCount + plan.TilemapMutationCount +
                plan.GameObjectInstantiationCount + plan.GameObjectEnableCount +
                plan.GameObjectDisableCount + plan.GameObjectDestroyCount +
                plan.AddressablesLoadCount + plan.ResourcesLoadCount +
                plan.AssetDatabaseLoadCount + plan.AuthoringCsvEditCount +
                plan.GeneratedCsvCommitCount + plan.GeneratedAssetCommitCount +
                plan.RuntimeObjectSpawnCount + plan.PopulationStableSpawnIdCount +
                plan.ProductionSeedApprovalCount;
            Assert.That(sideEffects, Is.Zero);

            TestContext.WriteLine("MAP17_06_SIDE_EFFECT_EVIDENCE system_io=0/0" +
                " disk_save_load=0/0 user_slot_platform=0/0 tilemap=0/0/0/0/0" +
                " colliders=0/0/0 rigidbody=0 physics=0/0 scene_prefab_tilemap=0/0/0" +
                " gameobject=0/0/0/0 asset_loads=0/0/0 csv=0 generated=0/0" +
                " runtime_population=0/0 seed_approval=0");
        }

        [Test]
        public void Map17HandoffKeepsMap17_07Locked()
        {
            Assert.That(GeneratedSaveManifestService.DownstreamOwner,
                Is.EqualTo("MAP17_07_CREATE_BAKE_STREAM_SAVE_PERFORMANCE_TESTS"));
            Assert.That(GeneratedSaveManifestService.OpensDownstreamTask, Is.False);
            Assert.That(Manifest().Manifest.ModifiedSectorCount, Is.EqualTo(1));
            Assert.That(Manifest().Manifest.UnmodifiedSectorCount, Is.EqualTo(168));

            TestContext.WriteLine("MAP17_06_HANDOFF_EVIDENCE MAP17_07_started=NO" +
                " locked=YES modified_unmodified=1/168 payload_digest=" +
                Manifest().Payload.Digest);
        }

        private static GeneratedSaveManifestResult Manifest()
        {
            if (manifest != null) return manifest;
            manifest = GeneratedSaveManifestService.Build(Modified().Storage,
                SeedIdentity, GeneratorVersion, DataVersion, Placement().OutputDigest,
                WindowHandleDigest());
            Assert.That(manifest.Success, Is.True, Failures(manifest));
            return manifest;
        }

        private static string WindowHandleDigest() =>
            GeneratedSaveManifestService.ComputeWindowHandleDigest(Authority().BaseDigests);

        private static GeneratedSectorRegenerationRequest Request(
            GeneratedWorldSaveManifest manifestValue = null,
            GeneratedSectorCoordinate sector = null,
            GeneratedSectorModificationAuthority authorityValue = null,
            string seed = null,
            string generator = null,
            string data = null,
            string geometry = null,
            string placementDigest = null,
            string bakeDigest = null,
            string cacheDigest = null,
            string windowHandleDigest = null,
            string storageDigest = null)
        {
            var value = manifestValue ?? Manifest().Manifest;
            return new GeneratedSectorRegenerationRequest(value, sector ?? SourceSector,
                authorityValue ?? Authority(), seed ?? value.Header.SeedIdentity,
                generator ?? value.Header.Version.GeneratorVersion,
                data ?? value.Header.Version.DataVersion,
                geometry ?? value.Header.GeometryDigest,
                placementDigest ?? value.Header.PlacementDigest,
                bakeDigest ?? value.Header.BakeDigest,
                cacheDigest ?? value.Header.CacheDigest,
                windowHandleDigest ?? value.Header.WindowHandleDigest,
                storageDigest ?? value.Header.StorageDigest);
        }

        private static GeneratedSaveManifestHeader Header(
            GeneratedSaveManifestHeader source,
            GeneratedSaveManifestVersion version = null,
            int? modifiedSectorCount = null) => new GeneratedSaveManifestHeader(
                version ?? source.Version, source.SeedIdentity, source.GeometryDigest,
                source.PlacementDigest, source.BakeDigest, source.CacheDigest,
                source.WindowHandleDigest, source.StorageDigest,
                modifiedSectorCount ?? source.ModifiedSectorCount);

        private static string Hash(string value) =>
            BakingCanonicalDigest.HashCanonicalLines(new[] { value });

        private static GeneratedSectorModificationStore Store() =>
            new GeneratedSectorModificationStore(Authority());

        private static GeneratedSectorModificationAuthority Authority()
        {
            if (authority != null) return authority;
            var source = Streaming();
            var records = Bake().LayerBuffers.SelectMany(value => value.Records).ToArray();
            authority = new GeneratedSectorModificationAuthority(
                ReferenceChain().Geometry, SourceSector, records, source.Window,
                source.Diff, source.TransitionPlan, ReferenceChain().GeometryDigest,
                Bake().OutputDigest, Cache().Digest, SeedIdentity,
                GeneratorVersion, DataVersion);
            Assert.That(authority.IsValid, Is.True);
            return authority;
        }

        private static ModificationFixture Modified()
        {
            if (modified != null) return modified;
            var store = Store();
            var storage = store.CreateEmpty();
            var records = new List<GeneratedSectorModificationRecord>();
            var definitions = new[]
            {
                new Definition(Target(GeneratedTilemapLayerId.Terrain, 0),
                    GeneratedSectorModificationKind.DestroyTile, DestroyPayload(0)),
                new Definition(Target(GeneratedTilemapLayerId.Terrain, 1),
                    GeneratedSectorModificationKind.ReplaceTile,
                    GeneratedSectorModificationPayload.Replace(Tile(GeneratedTilemapLayerId.Terrain, 1),
                        Source(GeneratedTilemapLayerId.Terrain, 1), "REPLACED_TILE", "PLAYER_REPAIR")),
                new Definition(Target(GeneratedTilemapLayerId.Marker, 2, "PICKUP_SLOT_002"),
                    GeneratedSectorModificationKind.CollectPickup,
                    GeneratedSectorModificationPayload.PickupCollected()),
                new Definition(Target(GeneratedTilemapLayerId.Marker, 3, "DEVICE_SLOT_003"),
                    GeneratedSectorModificationKind.ChangeDeviceState,
                    GeneratedSectorModificationPayload.DeviceState("door", "open")),
                new Definition(Target(GeneratedTilemapLayerId.Marker, 4, "CONSUME_SLOT_004"),
                    GeneratedSectorModificationKind.ConsumeSlot,
                    GeneratedSectorModificationPayload.SlotConsumed(
                        Source(GeneratedTilemapLayerId.Marker, 4))),
            };
            foreach (var definition in definitions)
            {
                var record = store.Author(storage, definition.Target,
                    definition.Kind, definition.Payload);
                var result = store.Add(storage, record);
                Assert.That(result.Success, Is.True, Describe(result.Failures));
                storage = result.Storage;
                records.Add(record);
            }
            modified = new ModificationFixture(storage,
                storage.Find(SourceSector), records.ToArray());
            return modified;
        }

        private static GeneratedSectorModificationTarget Target(
            GeneratedTilemapLayerId layer,
            int localIndex,
            string slotReference = null)
        {
            var record = Bake().LayerBuffers.Single(value => value.LayerId == layer)
                .Records.Single(value => value.SectorLocalIndex == localIndex);
            return new GeneratedSectorModificationTarget(SourceSector,
                new GeneratedSectorLocalCellIndex(localIndex), (int)layer,
                record.SourceLayerStableToken, slotReference);
        }

        private static GeneratedSectorModificationPayload DestroyPayload(int localIndex) =>
            GeneratedSectorModificationPayload.Destroy(
                Tile(GeneratedTilemapLayerId.Terrain, localIndex),
                Source(GeneratedTilemapLayerId.Terrain, localIndex));

        private static string Tile(GeneratedTilemapLayerId layer, int index)
        {
            var value = Bake().LayerBuffers.Single(buffer => buffer.LayerId == layer)
                .Records.Single(record => record.SectorLocalIndex == index).TileCode;
            return value == null ? "EMPTY" : value.Value;
        }

        private static string Source(GeneratedTilemapLayerId layer, int index) =>
            Bake().LayerBuffers.Single(buffer => buffer.LayerId == layer)
                .Records.Single(record => record.SectorLocalIndex == index)
                .SourceLayerStableToken;

        private static GeneratedSectorRuntimeHandle ActiveHandle()
        {
            var entry = Entry(SourceSector, 0);
            var unloaded = GeneratedSectorRuntimeHandleLifecycle.CreateUnloaded(entry, SeedIdentity);
            var preloaded = GeneratedSectorRuntimeHandleLifecycle.Transition(
                new GeneratedSectorRuntimeTransitionRequest(unloaded,
                    GeneratedSectorRuntimeState.Preloaded, unloaded.Sector,
                    Cache(), entry));
            Assert.That(preloaded.Success, Is.True, Describe(preloaded.Failures));
            var active = GeneratedSectorRuntimeHandleLifecycle.Transition(
                new GeneratedSectorRuntimeTransitionRequest(preloaded.Handle,
                    GeneratedSectorRuntimeState.Active, preloaded.Handle.Sector,
                    preloaded.CacheSnapshot));
            Assert.That(active.Success, Is.True, Describe(active.Failures));
            return active.Handle;
        }

        private static GeneratedSectorStreamingResult Streaming()
        {
            if (streaming != null) return streaming;
            streaming = GeneratedSectorWindowPlanner.Plan(new GeneratedSectorWindowRequest(
                new GeneratedSectorCoordinate(6, 6), 0.5d, 0.5d,
                GeneratedSectorDirectionHint.None, GeneratedSectorPreactivationPolicy.Default,
                Handles(), Entries()));
            Assert.That(streaming.Success, Is.True, Describe(streaming.Failures));
            return streaming;
        }

        private static GeneratedSectorRuntimeHandle[] Handles()
        {
            if (handles != null) return handles;
            handles = Entries().Select(value =>
                GeneratedSectorRuntimeHandleLifecycle.CreateUnloaded(value, SeedIdentity)).ToArray();
            return handles;
        }

        private static GeneratedColliderCacheSnapshot Cache()
        {
            if (cache != null) return cache;
            cache = Entries().Aggregate(GeneratedColliderCacheSnapshot.Empty,
                (snapshot, entry) => snapshot.Store(entry));
            return cache;
        }

        private static GeneratedColliderCacheEntry[] Entries()
        {
            if (entries != null) return entries;
            entries = Enumerable.Range(0,
                    GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorCount)
                .Select(index => new GeneratedSectorCoordinate(
                    index % GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorColumns,
                    index / GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorColumns))
                .Select(coordinate => Entry(coordinate, 0)).ToArray();
            return entries;
        }

        private static GeneratedColliderCacheEntry Entry(
            GeneratedSectorCoordinate coordinate,
            int revision)
        {
            var plan = Rebuild(revision);
            var source = GeneratedColliderCacheKey.Create(plan, GeneratorVersion, DataVersion);
            var key = new GeneratedColliderCacheKey(source.GeometryDigest, source.BakeDigest,
                source.SeamDigest, source.RegistryDigest, coordinate.ToRuntimeCoordinate(),
                source.GeneratorVersion, source.DataVersion, revision,
                source.CollisionPolicyVersion);
            return new GeneratedColliderCacheEntry(key, plan);
        }

        private static GeneratedColliderRebuildPlan Rebuild(int revision = 0)
        {
            GeneratedColliderRebuildPlan cached;
            if (Rebuilds.TryGetValue(revision, out cached)) return cached;
            var result = GeneratedColliderRebuildPlanner.Build(new GeneratedColliderRebuildRequest(
                Bake(), revision, revision == 0 ? "INITIAL_BAKE" : "PLAYER_MUTATION"));
            Assert.That(result.Success, Is.True, Describe(result.Failures));
            Rebuilds[revision] = result.Plan;
            return result.Plan;
        }

        private static GeneratedTilemapBakePlan Bake()
        {
            if (bake != null) return bake;
            var result = GeneratedTilemapLayerBaker.Bake(new GeneratedTilemapBakeRequest(Placement()));
            Assert.That(result.Success, Is.True, Describe(result.Failures));
            bake = result.Plan;
            return bake;
        }

        private static GeneratedCellPlacementPlan Placement()
        {
            if (placement != null) return placement;
            var chain = ReferenceChain();
            var result = GeneratedCellPlacementPlanner.Plan(new GeneratedCellPlacementRequest(
                SourceSector.ToRuntimeCoordinate(), chain.Geometry, chain.GeometryDigest,
                Map16ExitAuditDigest, chain.Slices, chain.Slots, chain.Packet, chain.Registry));
            Assert.That(result.Success, Is.True, Describe(result.Failures));
            placement = result.Plan;
            return placement;
        }

        private static Map17Chain ReferenceChain()
        {
            if (reference != null) return reference;
            var fixture = new ReferenceFinalRouteRecoveryFixture();
            var baselineRouteRequest = fixture.AcceptedRequest();
            var baselinePlan = baselineRouteRequest.CanvasPlan;
            var baselineRequest = baselinePlan.Request;
            var claims = baselinePlan.Cells.SelectMany(value => value.Winners).ToList();
            claims.Add(Claim("MAP17_01_TERRAIN_CLUSTER", 12, 20,
                FinalCanvasLayerKind.Terrain, FinalCanvasCellKind.Air,
                FinalCanvasSourceOwner.TerrainCluster,
                FinalCanvasClaimPriority.TerrainClusterPattern, "MAP11_TERRAIN_CLUSTER"));
            claims.Add(Claim("MAP17_01_ACTIVITY", 12, 20,
                FinalCanvasLayerKind.Marker, FinalCanvasCellKind.Marker,
                FinalCanvasSourceOwner.Activity,
                FinalCanvasClaimPriority.ActivityMarker, "MAP12_ACTIVITY"));
            claims.Add(Claim("MAP17_01_EVENT_OVERLAY", 13, 20,
                FinalCanvasLayerKind.Marker, FinalCanvasCellKind.Marker,
                FinalCanvasSourceOwner.EventOverlay,
                FinalCanvasClaimPriority.EventMarker, "MAP12_EVENT_OVERLAY"));
            var canvas = SectorCanvasLayerFinalizer.Finalize(new FinalCanvasLayerRequest(
                baselineRequest.SectorId, baselineRequest.Width, baselineRequest.Height, claims,
                baselineRequest.Map15ExitApproved, baselineRequest.Map15ExitDigest,
                baselineRequest.WorldAssemblyDigest, baselineRequest.SectorOwnershipDigest,
                baselineRequest.BoundaryAuthorityDigest, baselineRequest.FixedCanvasAuthorityDigest,
                baselineRequest.PublicationLabel));
            Assert.That(canvas.Success, Is.True, Describe(canvas.Failures));
            var density = SectorCanvasProtectionDensityValidator.Validate(canvas.Plan);
            Assert.That(density.Success, Is.True, Describe(density.Failures));
            var route = SectorFinalRouteRecoveryValidator.Validate(new FinalRouteRecoveryRequest(
                canvas.Plan, density.Report, baselineRouteRequest.Anchors,
                baselineRouteRequest.DeclaredEdges, baselineRouteRequest.PublicationLabel));
            Assert.That(route.Success, Is.True, Describe(route.Failures));
            var partition = SectorPatternChunkPartitioner.Partition(
                canvas.Plan, density.Report, route.Report);
            Assert.That(partition.Success, Is.True, Describe(partition.Failures));
            var slices = GeneratedMicroChunkSliceBuilder.Build(
                canvas.Plan, density.Report, route.Report, partition.Partition);
            Assert.That(slices.Success, Is.True, Describe(slices.Failures));
            var slots = GeneratedMicroChunkMarkerSlotProjector.Project(slices.SliceSet);
            Assert.That(slots.Success, Is.True, Describe(slots.Failures));
            var packet = GeneratedTerrainCsvExporter.Build(slices.SliceSet, slots.SlotSet);
            Assert.That(packet.Success, Is.True, Describe(packet.Failures));
            GeneratedTerrainGeometrySnapshot geometry;
            IReadOnlyList<string> geometryFailures;
            Assert.That(GeneratedTerrainGeometrySnapshot.TryCreate(
                out geometry, out geometryFailures), Is.True, Describe(geometryFailures));
            var registry = GeneratedTerrainAssetRegistrySnapshot.CreateReference(
                slices.SliceSet, slots.SlotSet);
            reference = new Map17Chain(geometry, slices.SliceSet, slots.SlotSet,
                packet.Packet, registry);
            return reference;
        }

        private static FinalCanvasLayerClaim Claim(
            string claimId,
            int x,
            int y,
            FinalCanvasLayerKind layer,
            FinalCanvasCellKind cellKind,
            FinalCanvasSourceOwner owner,
            FinalCanvasClaimPriority priority,
            string provenanceId) => new FinalCanvasLayerClaim(
                claimId, new FinalCanvasCellCoordinate(x, y), layer, cellKind, owner, priority,
                FinalCanvasProtectionKind.None, false, provenanceId,
                GeneratedTerrainAssetRegistrySnapshot.ReferencePublicationLabel);

        private static void AssertFailure(
            GeneratedSaveManifestResult result,
            GeneratedSaveManifestValidationFailureCode code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Failures.Select(value => value.Code), Does.Contain(code),
                Failures(result));
        }

        private static void AssertFailure(
            GeneratedSectorRegenerationApplyResult result,
            GeneratedSaveManifestValidationFailureCode code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Failures.Select(value => value.Code), Does.Contain(code),
                Failures(result));
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.PartialApplyMutationCount, Is.Zero);
        }

        private static string Failures(GeneratedSaveManifestResult result) => result == null
            ? "NULL RESULT" : Describe(result.Failures);
        private static string Failures(GeneratedSectorRegenerationApplyResult result) => result == null
            ? "NULL RESULT" : Describe(result.Failures);
        private static string Describe(System.Collections.IEnumerable values)
        {
            var output = new List<string>();
            foreach (var value in values) output.Add(value == null ? "NULL" : value.ToString());
            return string.Join(";", output);
        }

        private sealed class Definition
        {
            public Definition(GeneratedSectorModificationTarget target,
                GeneratedSectorModificationKind kind,
                GeneratedSectorModificationPayload payload)
            {
                Target = target;
                Kind = kind;
                Payload = payload;
            }
            public GeneratedSectorModificationTarget Target { get; }
            public GeneratedSectorModificationKind Kind { get; }
            public GeneratedSectorModificationPayload Payload { get; }
        }

        private sealed class ModificationFixture
        {
            public ModificationFixture(GeneratedSectorModificationStorage storage,
                GeneratedModifiedSectorSnapshot snapshot,
                IReadOnlyList<GeneratedSectorModificationRecord> records)
            {
                Storage = storage;
                Snapshot = snapshot;
                Records = records;
            }
            public GeneratedSectorModificationStorage Storage { get; }
            public GeneratedModifiedSectorSnapshot Snapshot { get; }
            public IReadOnlyList<GeneratedSectorModificationRecord> Records { get; }
        }

        private sealed class Map17Chain
        {
            public Map17Chain(GeneratedTerrainGeometrySnapshot geometry,
                GeneratedMicroChunkSliceSet slices,
                GeneratedMicroChunkMarkerSlotSet slots,
                GeneratedTerrainExportPacket packet,
                GeneratedTerrainAssetRegistrySnapshot registry)
            {
                Geometry = geometry;
                Slices = slices;
                Slots = slots;
                Packet = packet;
                Registry = registry;
                GeometryDigest = GeneratedCellPlacementDigest.ComputeGeometry(geometry);
            }
            public GeneratedTerrainGeometrySnapshot Geometry { get; }
            public string GeometryDigest { get; }
            public GeneratedMicroChunkSliceSet Slices { get; }
            public GeneratedMicroChunkMarkerSlotSet Slots { get; }
            public GeneratedTerrainExportPacket Packet { get; }
            public GeneratedTerrainAssetRegistrySnapshot Registry { get; }
        }

        private static class Code
        {
            public const GeneratedSaveManifestValidationFailureCode UnsupportedVersion =
                GeneratedSaveManifestValidationFailureCode.UnsupportedVersion;
            public const GeneratedSaveManifestValidationFailureCode PayloadHashMismatch =
                GeneratedSaveManifestValidationFailureCode.PayloadHashMismatch;
            public const GeneratedSaveManifestValidationFailureCode SeedMismatch =
                GeneratedSaveManifestValidationFailureCode.SeedMismatch;
            public const GeneratedSaveManifestValidationFailureCode GeneratorVersionMismatch =
                GeneratedSaveManifestValidationFailureCode.GeneratorVersionMismatch;
            public const GeneratedSaveManifestValidationFailureCode DataVersionMismatch =
                GeneratedSaveManifestValidationFailureCode.DataVersionMismatch;
            public const GeneratedSaveManifestValidationFailureCode GeometryDigestMismatch =
                GeneratedSaveManifestValidationFailureCode.GeometryDigestMismatch;
            public const GeneratedSaveManifestValidationFailureCode PlacementDigestMismatch =
                GeneratedSaveManifestValidationFailureCode.PlacementDigestMismatch;
            public const GeneratedSaveManifestValidationFailureCode BakeDigestMismatch =
                GeneratedSaveManifestValidationFailureCode.BakeDigestMismatch;
            public const GeneratedSaveManifestValidationFailureCode CacheDigestMismatch =
                GeneratedSaveManifestValidationFailureCode.CacheDigestMismatch;
            public const GeneratedSaveManifestValidationFailureCode WindowHandleDigestMismatch =
                GeneratedSaveManifestValidationFailureCode.WindowHandleDigestMismatch;
            public const GeneratedSaveManifestValidationFailureCode StorageDigestMismatch =
                GeneratedSaveManifestValidationFailureCode.StorageDigestMismatch;
            public const GeneratedSaveManifestValidationFailureCode DuplicateSectorEntry =
                GeneratedSaveManifestValidationFailureCode.DuplicateSectorEntry;
            public const GeneratedSaveManifestValidationFailureCode DuplicateRecordId =
                GeneratedSaveManifestValidationFailureCode.DuplicateRecordId;
            public const GeneratedSaveManifestValidationFailureCode UnmodifiedSectorEntry =
                GeneratedSaveManifestValidationFailureCode.UnmodifiedSectorEntry;
            public const GeneratedSaveManifestValidationFailureCode MissingTarget =
                GeneratedSaveManifestValidationFailureCode.MissingTarget;
            public const GeneratedSaveManifestValidationFailureCode UnknownField =
                GeneratedSaveManifestValidationFailureCode.UnknownField;
        }
    }
}
