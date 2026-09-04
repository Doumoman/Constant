using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Baking
{
    [TestFixture]
    [Category("MAP17_05")]
    public sealed class GeneratedSectorModificationStoreTests
    {
        private const string Map16ExitAuditDigest =
            "78d3046d62608494fb1306ff4e57a0b2d4b36eafc3a5e7e19cb8f399c3ca29f0";
        private const string ExpectedWindowDigest =
            "cb3bd4d7037ced7745cb7080e2e80c35057770e9fa2278743360f659373be07a";
        private const string ExpectedWindowDiffDigest =
            "fa5e1f6ddedc374a0399b6fd5c04d5cfb2939e24bc2c03f4f49a91713c47ec2b";
        private const string ExpectedTransitionDigest =
            "4276889b5ba3af471505d26181b902d471e4a6198392afce9c5890b684333489";
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

        [Test]
        public void SectorModificationTargetsAddressCellsByLocalIndexLayerAndProvenance()
        {
            var geometry = Authority().Geometry;
            Assert.That(geometry.SectorCellCount, Is.EqualTo(1536));
            AssertIndex(0, 0, 0);
            AssertIndex(47, 47, 0);
            AssertIndex(48, 0, 1);
            AssertIndex(1535, 47, 31);
            Assert.That(Authority().SourceSectorCellCount, Is.EqualTo(1536));
            Assert.That(Authority().SourceRecordCount, Is.EqualTo(10752));
            Assert.That(Authority().SourceLayerCount, Is.EqualTo(7));

            var store = Store();
            var empty = store.CreateEmpty();
            AssertFailure(store.Add(empty, store.Author(empty,
                CloneTarget(Target(GeneratedTilemapLayerId.Terrain, 0), localIndex: -1),
                GeneratedSectorModificationKind.DestroyTile, DestroyPayload(0))),
                GeneratedSectorModificationFailureCode.InvalidLocalIndex);
            AssertFailure(store.Add(empty, store.Author(empty,
                CloneTarget(Target(GeneratedTilemapLayerId.Terrain, 0), localIndex: 1536),
                GeneratedSectorModificationKind.DestroyTile, DestroyPayload(0))),
                GeneratedSectorModificationFailureCode.InvalidLocalIndex);
            AssertFailure(store.Add(empty, store.Author(empty,
                CloneTarget(Target(GeneratedTilemapLayerId.Terrain, 0), layerId: 8),
                GeneratedSectorModificationKind.DestroyTile, DestroyPayload(0))),
                GeneratedSectorModificationFailureCode.InvalidLayer);
            AssertFailure(store.Add(empty, store.Author(empty,
                CloneTarget(Target(GeneratedTilemapLayerId.Terrain, 0),
                    sector: new GeneratedSectorCoordinate(4, 4)),
                GeneratedSectorModificationKind.DestroyTile, DestroyPayload(0))),
                GeneratedSectorModificationFailureCode.CrossSectorMismatch);

            TestContext.WriteLine("MAP17_05_TARGET_EVIDENCE range=0..1535 probes=" +
                "0:0,0;47:47,0;48:0,1;1535:47,31 invalid_index=2/2" +
                " invalid_layer=1/1 cross_sector=1/1 layers=7 records=10752 cells=1536");
        }

        [Test]
        public void StableModificationIdsAreDeterministicAndSeparateFromPopulationSpawnIds()
        {
            var store = Store();
            var empty = store.CreateEmpty();
            var target = Target(GeneratedTilemapLayerId.Terrain, 0);
            var same = new[]
            {
                store.Author(empty, target, GeneratedSectorModificationKind.DestroyTile,
                    DestroyPayload(0)),
                store.Author(empty, target, GeneratedSectorModificationKind.DestroyTile,
                    DestroyPayload(0)),
            };
            Assert.That(same[0].Id.Value, Is.EqualTo(same[1].Id.Value));
            Assert.That(same.All(value => value.Id.IsValid &&
                GeneratedSectorModificationDigest.IsLowerHexSha256(value.Id.Value)), Is.True);

            var variants = new[]
            {
                same[0],
                store.Author(empty, Target(GeneratedTilemapLayerId.Terrain, 1),
                    GeneratedSectorModificationKind.DestroyTile, DestroyPayload(1)),
                store.Author(empty, Target(GeneratedTilemapLayerId.Affordance, 0),
                    GeneratedSectorModificationKind.DestroyTile,
                    GeneratedSectorModificationPayload.Destroy("SOURCE", "SOURCE")),
                store.Author(empty, CloneTarget(target, sector: new GeneratedSectorCoordinate(4, 4)),
                    GeneratedSectorModificationKind.DestroyTile, DestroyPayload(0)),
                store.Author(empty, CloneTarget(target, slotReference: "SLOT_VARIANT"),
                    GeneratedSectorModificationKind.DestroyTile, DestroyPayload(0)),
            };
            Assert.That(variants.Select(value => value.Id.Value).Distinct().Count(),
                Is.EqualTo(variants.Length));
            Assert.That(variants.All(value => value.Id.Namespace == "SECTOR_MODIFICATION"), Is.True);
            Assert.That(GeneratedSectorModificationStore.PopulationStableSpawnIdCount, Is.Zero);

            TestContext.WriteLine("MAP17_05_ID_EVIDENCE lower_hex=YES same_target_same_kind=YES" +
                " variants=5 collisions=0 random_guid=0 population_spawn_ids=0 namespace=" +
                same[0].Id.Namespace);
        }

        [Test]
        public void ModificationStoragePublishesDirtyRevisionSnapshotsAndDigests()
        {
            var fixture = Modified();
            Assert.That(fixture.Storage.ModifiedSectorCount, Is.EqualTo(1));
            Assert.That(fixture.Storage.TotalRecordCount, Is.EqualTo(5));
            Assert.That(fixture.Snapshot.DirtyRevision, Is.EqualTo(5));
            Assert.That(fixture.Snapshot.Records.Select(value => value.Revision),
                Is.EquivalentTo(new[] { 1, 2, 3, 4, 5 }));
            Assert.That(fixture.Snapshot.BaseDigests.Equals(Authority().BaseDigests), Is.True);
            Assert.That(GeneratedSectorModificationDigest.IsLowerHexSha256(
                fixture.Snapshot.ModificationSet.Digest), Is.True);
            Assert.That(GeneratedSectorModificationDigest.IsLowerHexSha256(
                fixture.Storage.Digest), Is.True);

            TestContext.WriteLine("MAP17_05_STORAGE_EVIDENCE sectors=1 records=5" +
                " dirty_revision=5 increments=5 set_digest=" +
                fixture.Snapshot.ModificationSet.Digest + " storage_digest=" +
                fixture.Storage.Digest);
        }

        [Test]
        public void DestroyReplaceCollectDeviceAndConsumeSlotRecordsApplyAsPureData()
        {
            var fixture = Modified();
            var sourceTokens = Authority().SourceRecords.Select(value => value.StableToken).ToArray();
            var result = Store().BuildApplyPlan(fixture.Storage, SourceSector, ActiveHandle());
            Assert.That(result.Success, Is.True, Failures(result));
            Assert.That(result.ApplyPlan.CommandCount, Is.EqualTo(5));
            Assert.That(result.ApplyPlan.Commands.Select(value => value.Kind), Is.EquivalentTo(
                Enum.GetValues(typeof(GeneratedSectorModificationKind))
                    .Cast<GeneratedSectorModificationKind>()));
            Assert.That(result.ApplyPlan.LogicalLayerCommandCount, Is.EqualTo(2));
            Assert.That(result.ApplyPlan.SlotStateCommandCount, Is.EqualTo(2));
            Assert.That(result.ApplyPlan.DeviceStateCommandCount, Is.EqualTo(1));
            Assert.That(Authority().SourceRecords.Select(value => value.StableToken),
                Is.EqualTo(sourceTokens));
            Assert.That(result.ApplyPlan.InPlaceInputMutationCount, Is.Zero);

            TestContext.WriteLine("MAP17_05_APPLY_EVIDENCE commands=5 kinds=5/5" +
                " logical_layer=2 slot_state=2 device_state=1 input_mutations=0" +
                " apply_digest=" + result.ApplyPlan.Digest);
        }

        [Test]
        public void DuplicateConflictingOutOfBoundsUnknownAndStaleMutationsFailAtomically()
        {
            var fixture = Modified();
            var store = Store();
            var duplicate = store.Add(fixture.Storage, fixture.Records[0]);
            Assert.That(duplicate.Success, Is.True, Failures(duplicate));
            Assert.That(duplicate.WasIdempotent, Is.True);
            Assert.That(duplicate.Storage.TotalRecordCount, Is.EqualTo(5));

            var conflict = store.Author(fixture.Storage, fixture.Records[0].Target,
                fixture.Records[0].Kind,
                GeneratedSectorModificationPayload.Destroy("CHANGED", "CHANGED"),
                revision: fixture.Records[0].Revision);
            var conflictResult = store.Add(fixture.Storage, conflict);
            Assert.That(conflictResult.Success, Is.False);
            Assert.That(conflictResult.Storage.Digest, Is.EqualTo(fixture.Storage.Digest));

            var unknownTarget = CloneTarget(fixture.Records[0].Target,
                sourceToken: "UNKNOWN_PROVENANCE");
            AssertFailure(store.Add(fixture.Storage, store.Author(fixture.Storage,
                unknownTarget, GeneratedSectorModificationKind.DestroyTile,
                GeneratedSectorModificationPayload.Destroy("SOURCE", "UNKNOWN_PROVENANCE"))),
                GeneratedSectorModificationFailureCode.UnknownTarget);

            var stale = Authority().BaseDigests;
            var staleDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "STALE" });
            var staleBases = new[]
            {
                new GeneratedSectorModificationBaseDigests(stale.GeometryDigest,
                    staleDigest, stale.CacheDigest, stale.WindowDigest,
                    stale.WindowDiffDigest, stale.TransitionPlanDigest),
                new GeneratedSectorModificationBaseDigests(stale.GeometryDigest,
                    stale.BakeDigest, staleDigest, stale.WindowDigest,
                    stale.WindowDiffDigest, stale.TransitionPlanDigest),
                new GeneratedSectorModificationBaseDigests(stale.GeometryDigest,
                    stale.BakeDigest, stale.CacheDigest, staleDigest,
                    stale.WindowDiffDigest, stale.TransitionPlanDigest),
            };
            foreach (var staleBase in staleBases)
                AssertFailure(store.Add(fixture.Storage, store.Author(fixture.Storage,
                    fixture.Records[0].Target, GeneratedSectorModificationKind.DestroyTile,
                    DestroyPayload(0), baseDigests: staleBase)),
                    GeneratedSectorModificationFailureCode.StaleDigest);

            var validNext = store.Author(fixture.Storage, fixture.Records[0].Target,
                GeneratedSectorModificationKind.DestroyTile,
                GeneratedSectorModificationPayload.Destroy("UPDATED", "UPDATED"));
            var invalidAfter = store.Author(fixture.Storage, unknownTarget,
                GeneratedSectorModificationKind.DestroyTile,
                GeneratedSectorModificationPayload.Destroy("SOURCE", "UNKNOWN_PROVENANCE"),
                revision: 7);
            var atomic = store.Merge(fixture.Storage, new[] { validNext, invalidAfter });
            Assert.That(atomic.Success, Is.False);
            Assert.That(atomic.Storage.Digest, Is.EqualTo(fixture.Storage.Digest));

            TestContext.WriteLine("MAP17_05_FAILURE_EVIDENCE duplicate_records=0" +
                " conflict=1/1 unknown=1/1 stale_bake_cache_window=3/3 out_of_bounds=2/2" +
                " invalid_layer=1/1 cross_sector=1/1 partial_mutations=0");
        }

        [Test]
        public void SleepingModifiedHandleReceivesDirtyRevisionWithoutDurableSave()
        {
            var fixture = Modified();
            var active = ActiveHandle();
            var result = Store().BuildApplyPlan(fixture.Storage, SourceSector, active);
            Assert.That(result.Success, Is.True, Failures(result));
            Assert.That(active.State, Is.EqualTo(GeneratedSectorRuntimeState.Active));
            Assert.That(active.MutationRevision, Is.Zero);
            Assert.That(result.ApplyPlan.SleepingModifiedHandle.State,
                Is.EqualTo(GeneratedSectorRuntimeState.SleepingModified));
            Assert.That(result.ApplyPlan.SleepingModifiedHandle.MutationRevision,
                Is.EqualTo(fixture.Snapshot.DirtyRevision));
            Assert.That(result.ApplyPlan.SleepingModifiedHandle.IsDirty, Is.True);
            Assert.That(result.ApplyPlan.SleepingModifiedHandle.DirtyReason,
                Is.EqualTo("SECTOR_MODIFICATION_STORAGE"));
            Assert.That(result.ApplyPlan.DurableSaveWriteCount, Is.Zero);
            Assert.That(result.ApplyPlan.SleepingModifiedHandle.DurableSaveWriteCount, Is.Zero);

            TestContext.WriteLine("MAP17_05_HANDLE_EVIDENCE Active->SleepingModified" +
                " revision=0->5 dirty=YES reason=SECTOR_MODIFICATION_STORAGE" +
                " durable_save_writes=0");
        }

        [Test]
        public void ModifiedSectorStorageCompactsAndQueriesRecordsDeterministically()
        {
            var fixture = Modified();
            var compact = Store().Compact(fixture.Storage);
            Assert.That(compact.Success, Is.True, Failures(compact));
            Assert.That(compact.WasIdempotent, Is.True);
            Assert.That(compact.Storage.Digest, Is.EqualTo(fixture.Storage.Digest));
            var query = Store().Query(compact.Storage, SourceSector);
            Assert.That(query.Success, Is.True, Failures(query));
            Assert.That(query.SectorSnapshot.RecordCount, Is.EqualTo(5));
            Assert.That(query.SectorSnapshot.Records.Select(value => value.Id.Value), Is.Ordered);
            Assert.That(query.SectorSnapshot.Digest, Is.EqualTo(fixture.Snapshot.Digest));

            TestContext.WriteLine("MAP17_05_COMPACT_EVIDENCE final_state_preserved=YES" +
                " query_order=DETERMINISTIC records=5 compact_digest=" +
                compact.Storage.Digest);
        }

        [Test]
        public void ModificationDigestsAreStableAcrossRepeatReverseCultureAndRecordOrder()
        {
            var store = Store();
            var fixture = Modified();
            var repeat = store.Merge(store.CreateEmpty(), fixture.Records);
            var reverse = store.Merge(store.CreateEmpty(), fixture.Records.Reverse());
            Assert.That(repeat.Success, Is.True, Failures(repeat));
            Assert.That(reverse.Success, Is.True, Failures(reverse));

            var previousCulture = CultureInfo.CurrentCulture;
            GeneratedSectorModificationResult culture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                culture = store.Merge(store.CreateEmpty(), fixture.Records.Reverse());
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
            Assert.That(culture.Success, Is.True, Failures(culture));
            var compact = store.Compact(reverse.Storage);
            Assert.That(compact.Success, Is.True, Failures(compact));

            var digests = new[]
            {
                fixture.Storage.Digest, repeat.Storage.Digest, reverse.Storage.Digest,
                culture.Storage.Digest, compact.Storage.Digest,
            };
            Assert.That(digests.Distinct().Count(), Is.EqualTo(1));
            Assert.That(fixture.Snapshot.ModificationSet.Digest,
                Is.EqualTo(repeat.SectorSnapshot.ModificationSet.Digest));

            var baselinePlan = store.BuildApplyPlan(fixture.Storage,
                SourceSector, ActiveHandle());
            var reversePlan = store.BuildApplyPlan(reverse.Storage,
                SourceSector, ActiveHandle());
            Assert.That(baselinePlan.Success, Is.True, Failures(baselinePlan));
            Assert.That(reversePlan.Success, Is.True, Failures(reversePlan));
            Assert.That(baselinePlan.ApplyPlan.Digest,
                Is.EqualTo(reversePlan.ApplyPlan.Digest));

            var mutation = store.Author(fixture.Storage, fixture.Records[3].Target,
                GeneratedSectorModificationKind.ChangeDeviceState,
                GeneratedSectorModificationPayload.DeviceState("door", "closed"));
            var mutated = store.Replace(fixture.Storage, mutation);
            Assert.That(mutated.Success, Is.True, Failures(mutated));
            var mutatedPlan = store.BuildApplyPlan(mutated.Storage,
                SourceSector, ActiveHandle());
            Assert.That(mutatedPlan.Success, Is.True, Failures(mutatedPlan));
            Assert.That(mutated.Storage.Digest, Is.Not.EqualTo(fixture.Storage.Digest));
            Assert.That(mutated.SectorSnapshot.ModificationSet.Digest,
                Is.Not.EqualTo(fixture.Snapshot.ModificationSet.Digest));
            Assert.That(mutatedPlan.ApplyPlan.Digest,
                Is.Not.EqualTo(baselinePlan.ApplyPlan.Digest));

            TestContext.WriteLine("MAP17_05_DIGEST_EVIDENCE set=" +
                fixture.Snapshot.ModificationSet.Digest + " storage=" +
                fixture.Storage.Digest + " apply=" + baselinePlan.ApplyPlan.Digest +
                " mismatches_repeat_reverse_culture_record_compact=0/0/0/0/0" +
                " mutation_sensitivity=3/3");
        }

        [Test]
        public void ModificationStorageDoesNotWriteFilesCreateObjectsSpawnContentOrMutateScenes()
        {
            var fixture = Modified();
            var plan = Store().BuildApplyPlan(fixture.Storage, SourceSector, ActiveHandle()).ApplyPlan;
            var sideEffects = fixture.Storage.DurableSaveWriteCount +
                fixture.Storage.SaveManifestFileCount +
                fixture.Storage.RegenerationApplyExecutionCount +
                fixture.Storage.TilemapComponentWriteCount +
                fixture.Storage.TilemapSetTileCallCount +
                fixture.Storage.TilemapSetTilesCallCount +
                fixture.Storage.TilemapSetTilesBlockCallCount +
                fixture.Storage.TilemapClearAllTilesCallCount +
                fixture.Storage.TilemapColliderCreationCount +
                fixture.Storage.CompositeColliderCreationCount +
                fixture.Storage.ColliderCreationCount +
                fixture.Storage.RigidbodyCreationCount +
                fixture.Storage.PhysicsQueryCount +
                fixture.Storage.PhysicsSimulationCount +
                fixture.Storage.SceneMutationCount + fixture.Storage.PrefabMutationCount +
                fixture.Storage.TilemapMutationCount +
                fixture.Storage.GameObjectInstantiationCount +
                fixture.Storage.GameObjectEnableCount +
                fixture.Storage.GameObjectDisableCount +
                fixture.Storage.GameObjectDestroyCount +
                fixture.Storage.AddressablesLoadCount + fixture.Storage.ResourcesLoadCount +
                fixture.Storage.AssetDatabaseLoadCount + fixture.Storage.AuthoringCsvEditCount +
                fixture.Storage.GeneratedCsvCommitCount +
                fixture.Storage.GeneratedAssetCommitCount +
                fixture.Storage.RuntimeObjectSpawnCount +
                fixture.Storage.PopulationStableSpawnIdCount +
                fixture.Storage.ProductionSeedApprovalCount +
                plan.InPlaceInputMutationCount + plan.DurableSaveWriteCount +
                plan.TilemapWriteCount + plan.GameObjectChangeCount;
            Assert.That(sideEffects, Is.Zero);

            TestContext.WriteLine("MAP17_05_SIDE_EFFECT_EVIDENCE save_manifest_regen=0/0/0" +
                " tilemap_calls=0/0/0/0/0 colliders=0/0/0 rigidbody=0 physics=0/0" +
                " scene_prefab_tilemap=0/0/0 gameobject=0/0/0/0 asset_loads=0/0/0" +
                " csv_edits=0 generated_csv_assets=0/0 runtime_spawn=0 seed_approval=0");
        }

        [Test]
        public void Map17HandoffKeepsMap17_06Locked()
        {
            var source = Streaming();
            Assert.That(source.Window.Digest, Is.EqualTo(ExpectedWindowDigest));
            Assert.That(source.Diff.Digest, Is.EqualTo(ExpectedWindowDiffDigest));
            Assert.That(source.TransitionPlan.Digest, Is.EqualTo(ExpectedTransitionDigest));
            Assert.That(source.Window.PreloadCount, Is.EqualTo(49));
            Assert.That(source.Window.ActiveCount, Is.EqualTo(25));
            Assert.That(source.Request.Handles.Count, Is.EqualTo(169));
            Assert.That(source.Request.CacheEntries.Count, Is.EqualTo(169));
            Assert.That(Enum.GetValues(typeof(GeneratedSectorRuntimeState)).Length, Is.EqualTo(4));
            Assert.That(GeneratedSectorModificationStore.DownstreamOwner,
                Is.EqualTo("MAP17_06_IMPLEMENT_SAVE_MANIFEST_REGENERATION_AND_APPLY"));
            Assert.That(GeneratedSectorModificationStore.OpensDownstreamTask, Is.False);

            TestContext.WriteLine("MAP17_05_HANDOFF_EVIDENCE window=" +
                source.Window.Digest + " diff=" + source.Diff.Digest +
                " transition=" + source.TransitionPlan.Digest +
                " world_sectors=169/169 membership_preload_active=49/25 source_states=" +
                "Unloaded/Preloaded/Active/SleepingModified MAP17_06_started=NO locked=YES");
        }

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
                Assert.That(result.Success, Is.True, Failures(result));
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

        private static GeneratedSectorModificationTarget CloneTarget(
            GeneratedSectorModificationTarget source,
            int? localIndex = null,
            int? layerId = null,
            GeneratedSectorCoordinate sector = null,
            string sourceToken = null,
            string slotReference = null) => new GeneratedSectorModificationTarget(
                sector ?? source.Sector,
                new GeneratedSectorLocalCellIndex(localIndex ?? source.LocalIndex.Value),
                layerId ?? source.LayerId,
                sourceToken ?? source.SourceProvenanceToken,
                slotReference ?? source.SlotReference);

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

        private static void AssertIndex(int value, int x, int y)
        {
            var index = new GeneratedSectorLocalCellIndex(value);
            Assert.That(index.IsValid, Is.True);
            Assert.That(index.SectorLocalX, Is.EqualTo(x));
            Assert.That(index.SectorLocalY, Is.EqualTo(y));
        }

        private static void AssertFailure(
            GeneratedSectorModificationResult result,
            GeneratedSectorModificationFailureCode code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Failures.Select(value => value.Code), Does.Contain(code),
                Failures(result));
        }

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

        private static string Failures(GeneratedSectorModificationResult result) => result == null
            ? "NULL RESULT" : Describe(result.Failures);

        private static string Describe(System.Collections.IEnumerable values)
        {
            var output = new List<string>();
            foreach (var value in values) output.Add(value == null ? "NULL" : value.ToString());
            return string.Join(";", output);
        }

        private sealed class Definition
        {
            public Definition(
                GeneratedSectorModificationTarget target,
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
            public ModificationFixture(
                GeneratedSectorModificationStorage storage,
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
            public Map17Chain(
                GeneratedTerrainGeometrySnapshot geometry,
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
    }
}
