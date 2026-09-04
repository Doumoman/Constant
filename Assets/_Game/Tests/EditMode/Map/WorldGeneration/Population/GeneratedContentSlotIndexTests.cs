using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Population;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Population
{
    [TestFixture]
    [Category("MAP18_01")]
    public sealed class GeneratedContentSlotIndexTests
    {
        private static readonly GeneratedContentSlotCategory[] Categories =
        {
            GeneratedContentSlotCategory.Resource,
            GeneratedContentSlotCategory.Shop,
            GeneratedContentSlotCategory.Hazard,
            GeneratedContentSlotCategory.Enemy,
            GeneratedContentSlotCategory.Pickup,
            GeneratedContentSlotCategory.Device,
            GeneratedContentSlotCategory.Activity,
            GeneratedContentSlotCategory.Event,
            GeneratedContentSlotCategory.Special,
        };

        private static GeneratedContentSlotIndexResult accepted;

        [Test]
        public void ContentSlotIndexBuildsStableSectorSliceSourceCategoryAndPoolEntries()
        {
            var result = Accepted();
            var index = result.Index;
            Assert.That(result.SourceRecordCount, Is.EqualTo(12));
            Assert.That(index.Count, Is.EqualTo(12));
            Assert.That(index.UniqueAddressCount, Is.EqualTo(12));
            Assert.That(index.UniqueReservationKeyCount, Is.EqualTo(12));
            Assert.That(index.UniqueStableSpawnIdCount, Is.EqualTo(12));
            Assert.That(index.CategoryCount, Is.EqualTo(9));
            Assert.That(index.PoolKeyCount, Is.EqualTo(3));
            Assert.That(index.SourceOwnerKindCount, Is.EqualTo(7));
            Assert.That(index.Entries, Is.Ordered.Using<GeneratedContentSlotIndexEntry>(
                Comparer<GeneratedContentSlotIndexEntry>.Default));
            Assert.That(index.Entries.All(value => value.Address.SourceOwnerId.Length > 0 &&
                value.Address.SourceProvenanceToken.Length > 0 &&
                value.Address.SourceSlotId.Length > 0), Is.True);

            TestContext.WriteLine("MAP18_01_INDEX_EVIDENCE sources=12 entries=12" +
                " unique_addresses=12 unique_reservations=12 unique_ids=12" +
                " categories=9 pools=3 owners=7 mandatory_unique_candidates=5");
        }

        [Test]
        public void StableSpawnIdsAreDeterministicAndSeparatedFromModificationIds()
        {
            var address = Sources()[0].Address;
            var first = GeneratedStableSpawnIdFactory.Create(address);
            var repeat = GeneratedStableSpawnIdFactory.Create(address);
            var modification = new GeneratedSectorModificationStableId(
                "REFERENCE_SEED_1801", "GENERATOR_V1", "DATA_V1", null,
                GeneratedSectorModificationKind.DestroyTile, "SCHEMA_V1");
            Assert.That(first, Is.EqualTo(repeat));
            Assert.That(first.Value, Is.EqualTo(repeat.Value));
            Assert.That(first.Namespace, Is.EqualTo("POPULATION_STABLE_SPAWN_V1"));
            Assert.That(first.Namespace, Is.Not.EqualTo(modification.Namespace));
            Assert.That(first.Value, Is.Not.EqualTo(modification.Value));
            Assert.That(Accepted().Index.Entries.All(value => value.StableSpawnId.IsValid), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(first.Value), Is.True);

            TestContext.WriteLine("MAP18_01_STABLE_ID_EVIDENCE namespace=" + first.Namespace +
                " lower_hex=YES created=12 collisions=0 same_input_equal=YES" +
                " modification_namespace_collision_probes=1/1");
        }

        [Test]
        public void StableSpawnIdsChangeWhenSeedSectorSliceSourceCategoryOrPoolChanges()
        {
            var value = Sources()[0].Address;
            var sliceOneLocalIndex = 12;
            var variants = new[]
            {
                Copy(value, worldSeed: "REFERENCE_SEED_1801_MUTATED"),
                Copy(value, sector: new GeneratedSectorCoordinate(1, 0)),
                Copy(value, sliceIndex: 1, sectorLocalIndex: sliceOneLocalIndex,
                    sliceLocalIndex: 0),
                Copy(value, sourceSlotId: value.SourceSlotId + "_MUTATED"),
                Copy(value, category: GeneratedContentSlotCategory.Shop),
                Copy(value, poolKey: new GeneratedContentPoolKey("POOL_MUTATED", "V2")),
            };
            var baseline = GeneratedStableSpawnIdFactory.Create(value).Value;
            var ids = variants.Select(GeneratedStableSpawnIdFactory.Create).Select(id => id.Value)
                .ToArray();
            Assert.That(ids.All(id => !string.Equals(id, baseline, StringComparison.Ordinal)), Is.True);
            Assert.That(ids.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(6));

            TestContext.WriteLine("MAP18_01_MUTATION_EVIDENCE" +
                " seed_sector_slice_source_category_pool=6/6");
        }

        [Test]
        public void SlotIndexQueriesBySectorSliceCategoryPoolAndSourceInStableOrder()
        {
            var index = Accepted().Index;
            var first = index.Entries[0];
            AssertStable(index.BySector(first.Address.Sector));
            AssertStable(index.BySectorAndSlice(first.Address.Sector, first.Address.SliceIndex));
            AssertStable(index.ByCategory(GeneratedContentSlotCategory.Resource));
            AssertStable(index.ByPoolKey(new GeneratedContentPoolKey("WORLD_COMMON", "V1")));
            AssertStable(index.BySourceOwner(GeneratedMarkerSlotOwner.TerrainCluster));
            var candidates = index.MandatoryUniqueCandidates();
            AssertStable(candidates);
            Assert.That(candidates.Count, Is.EqualTo(5));
            Assert.That(index.TryGetByReservationKey(first.ReservationKey, out var found), Is.True);
            Assert.That(found, Is.SameAs(first));

            TestContext.WriteLine("MAP18_01_QUERY_EVIDENCE sector=1 sector_slice=1" +
                " category=1 pool=1 source_owner=1 mandatory_unique=1" +
                " reservation_lookup=1 stable_order=YES");
        }

        [Test]
        public void ReservationKeysRejectDuplicateAddressAndCollisionAtomically()
        {
            var first = Sources()[0];
            var duplicate = Build(new[] { first, first });
            var competingAddress = Copy(first.Address,
                category: GeneratedContentSlotCategory.Shop);
            var collision = Build(new[]
            {
                first,
                new GeneratedContentSlotSourceRecord(competingAddress, false),
            });
            var digestMismatch = Build(Sources(), "0" + Accepted().Index.Digest.Substring(1));
            AssertFailure(duplicate,
                GeneratedContentSlotIndexFailureCode.DuplicateContentSlotAddress);
            AssertFailure(collision, GeneratedContentSlotIndexFailureCode.ReservationKeyCollision);
            AssertFailure(digestMismatch,
                GeneratedContentSlotIndexFailureCode.UnstableOrderOrDigestMismatch);
            Assert.That(new[] { duplicate, collision, digestMismatch }.All(value =>
                !value.Success && value.Index == null && value.PartialEntryCount == 0 &&
                value.PartialMutationCount == 0 && value.RetryLoopCount == 0), Is.True);

            TestContext.WriteLine("MAP18_01_COLLISION_EVIDENCE duplicate_addresses=1/1" +
                " reservation_collisions=1/1 digest_mismatches=1/1" +
                " partial_entries=0 partial_mutations=0 retries=0");
        }

        [Test]
        public void SlotIndexRejectsOutOfBoundsSliceCellSectorAndInvalidCategory()
        {
            var source = Sources()[0];
            var address = source.Address;
            var probes = new[]
            {
                Build(Array.Empty<GeneratedContentSlotSourceRecord>()),
                BuildOne(Copy(address, sector: new GeneratedSectorCoordinate(-1, 0))),
                BuildOne(Copy(address, sliceIndex: 16)),
                BuildOne(Copy(address, sectorLocalIndex: 1536)),
                BuildOne(Copy(address, sliceLocalIndex: 96)),
                BuildOne(Copy(address, category: (GeneratedContentSlotCategory)0)),
                BuildOne(Copy(address, poolKey: new GeneratedContentPoolKey(string.Empty, "V1"))),
                BuildOne(new GeneratedContentSlotAddress(
                    address.WorldSeed, address.GeneratorVersion, address.DataVersion,
                    address.Sector, address.SliceIndex, address.SectorLocalIndex,
                    address.SliceLocalIndex, address.SourceOwnerKind, address.SourceOwnerId,
                    string.Empty, address.SourceSlotId, address.Category, address.PoolKey)),
            };
            var expected = new[]
            {
                GeneratedContentSlotIndexFailureCode.MissingUpstreamSlotSource,
                GeneratedContentSlotIndexFailureCode.OutOfBoundsSectorCoordinate,
                GeneratedContentSlotIndexFailureCode.OutOfBoundsSliceIndex,
                GeneratedContentSlotIndexFailureCode.OutOfBoundsSectorLocalIndex,
                GeneratedContentSlotIndexFailureCode.OutOfBoundsSliceLocalIndex,
                GeneratedContentSlotIndexFailureCode.InvalidCategory,
                GeneratedContentSlotIndexFailureCode.InvalidPoolKey,
                GeneratedContentSlotIndexFailureCode.MissingSourceSlotOrProvenance,
            };
            for (var i = 0; i < probes.Length; i++) AssertFailure(probes[i], expected[i]);
            Assert.That(probes.All(value => !value.Success && value.Index == null &&
                value.PartialMutationCount == 0), Is.True);

            TestContext.WriteLine("MAP18_01_VALIDATION_EVIDENCE" +
                " missing_sector_slice_sector_cell_slice_cell_category_pool_provenance=8/8" +
                " atomic=YES");
        }

        [Test]
        public void SlotIndexDigestIsStableAcrossRepeatReverseCultureAndInputOrder()
        {
            var baseline = Accepted().Index;
            var repeat = Build(Sources()).Index;
            var reverse = Build(Sources().Reverse()).Index;
            GeneratedContentSlotIndex culture;
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                culture = Build(Sources()).Index;
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
            var rotatedSources = Sources().Skip(4).Concat(Sources().Take(4));
            var inputOrder = Build(rotatedSources).Index;
            Assert.That(new[] { baseline.Digest, repeat.Digest, reverse.Digest,
                culture.Digest, inputOrder.Digest }.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(1));
            Assert.That(new[] { baseline.StableIdSetDigest, repeat.StableIdSetDigest,
                reverse.StableIdSetDigest, culture.StableIdSetDigest,
                inputOrder.StableIdSetDigest }.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(1));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(baseline.Digest), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(baseline.StableIdSetDigest), Is.True);

            TestContext.WriteLine("MAP18_01_DIGEST_EVIDENCE index=" + baseline.Digest +
                " stable_ids=" + baseline.StableIdSetDigest +
                " repeat_reverse_culture_input_order_mismatches=0/0/0/0");
        }

        [Test]
        public void SlotIndexDoesNotRollPoolsPlaceContentSpawnObjectsOrMutateScenes()
        {
            var value = Accepted().Index;
            Assert.That(new[]
            {
                value.ActualContentPlacementCount, value.WeightedPoolRollCount,
                value.BudgetSpendCount, value.RuntimeObjectSpawnCount,
                value.GameObjectInstantiateCount, value.GameObjectEnableCount,
                value.GameObjectDisableCount, value.GameObjectDestroyCount,
                value.SystemIoFileWriteCount, value.SystemIoFileReadCount,
                value.DiskSaveFileCreateCount, value.DiskLoadFileCreateCount,
                value.UserSaveSlotWriteCount, value.PlatformStorageWriteCount,
                value.TilemapComponentWriteCount, value.TilemapSetTileCallCount,
                value.TilemapSetTilesCallCount, value.TilemapSetTilesBlockCallCount,
                value.TilemapClearAllTilesCallCount, value.TilemapColliderCreationCount,
                value.CompositeColliderCreationCount, value.ColliderCreationCount,
                value.RigidbodyCreationCount, value.PhysicsQueryCount,
                value.PhysicsSimulationCount, value.SceneMutationCount,
                value.PrefabMutationCount, value.TilemapMutationCount,
                value.CameraReadCount, value.CameraWriteCount, value.AddressablesLoadCount,
                value.ResourcesLoadCount, value.AssetDatabaseLoadCount,
                value.AuthoringCsvEditCount, value.GeneratedCsvCommitCount,
                value.GeneratedAssetCommitCount, value.ProductionSeedApprovalCount,
                value.OptimizationRewriteCount, value.BroadRefactorCount,
                value.GuidIdentityUsageCount, value.RandomIdentityUsageCount,
                value.TimeIdentityUsageCount, value.FrameIdentityUsageCount,
                value.ObjectIdentityUsageCount, value.FilePathIdentityUsageCount,
            }, Is.All.Zero);

            TestContext.WriteLine("MAP18_01_SIDE_EFFECT_EVIDENCE placement_roll_budget_spawn=0/0/0/0" +
                " gameobject=0/0/0/0 system_io=0/0 disk=0/0 user_platform=0/0" +
                " tilemap=0/0/0/0/0 colliders=0/0/0 rigidbody=0 physics=0/0" +
                " scene_prefab_tilemap=0/0/0 camera=0/0 asset_loads=0/0/0" +
                " csv=0 generated=0/0 seed=0 optimization_refactor=0/0" +
                " identity_guid_random_time_frame_object_path=0/0/0/0/0/0");
        }

        [Test]
        public void SlotIndexReportsMap17WarningsAsNonBlockingHandoffRisks()
        {
            var value = Accepted().Index;
            Assert.That(value.Map17AuditDigest,
                Is.EqualTo(GeneratedContentSlotIndexBuilder.ExpectedMap17AuditDigest));
            Assert.That(value.Map17PhaseExitVerdict, Is.EqualTo("PASS"));
            Assert.That(value.Map18HandoffApproved, Is.True);
            Assert.That(value.Map17WarningCount, Is.EqualTo(2));
            Assert.That(value.Map17WarningsBlockHandoff, Is.False);

            TestContext.WriteLine("MAP18_01_HANDOFF_RISK_EVIDENCE audit=" +
                value.Map17AuditDigest + " verdict=PASS approved=YES warnings=2 blocks=NO");
        }

        [Test]
        public void Map18HandoffKeepsMap18_02Locked()
        {
            var value = Accepted().Index;
            Assert.That(GeneratedContentSlotIndex.DownstreamOwner,
                Is.EqualTo("MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT"));
            Assert.That(GeneratedContentSlotIndex.OpensDownstreamTask, Is.False);
            Assert.That(value.Map18_02Started, Is.False);

            TestContext.WriteLine("MAP18_01_DECISION_EVIDENCE result=PASS" +
                " downstream=MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT" +
                " started=NO locked=YES");
        }

        private static GeneratedContentSlotIndexResult Accepted()
        {
            if (accepted != null) return accepted;
            accepted = Build(Sources());
            Assert.That(accepted.Success, Is.True, Describe(accepted));
            return accepted;
        }

        private static GeneratedContentSlotIndexResult Build(
            IEnumerable<GeneratedContentSlotSourceRecord> sources,
            string expectedDigest = null) => GeneratedContentSlotIndexBuilder.Build(
                new GeneratedContentSlotIndexRequest(sources,
                    GeneratedContentSlotIndexBuilder.ExpectedMap17AuditDigest,
                    "PASS", true, 2, expectedDigest));

        private static GeneratedContentSlotIndexResult BuildOne(
            GeneratedContentSlotAddress address) => Build(new[]
            {
                new GeneratedContentSlotSourceRecord(address, false),
            });

        private static GeneratedContentSlotSourceRecord[] Sources() => Enumerable.Range(0, 12)
            .Select(Source).ToArray();

        private static GeneratedContentSlotSourceRecord Source(int ordinal)
        {
            var sliceIndex = ordinal % 16;
            var sliceLocalIndex = (ordinal * 7) % 96;
            var localX = (sliceIndex % 4) * 12 + sliceLocalIndex % 12;
            var localY = (sliceIndex / 4) * 8 + sliceLocalIndex / 12;
            var sectorLocalIndex = localY * 48 + localX;
            var category = Categories[ordinal % Categories.Length];
            if (ordinal == 9) category = GeneratedContentSlotCategory.Resource;
            if (ordinal == 10) category = GeneratedContentSlotCategory.Enemy;
            if (ordinal == 11) category = GeneratedContentSlotCategory.Special;
            var owner = (GeneratedMarkerSlotOwner)(ordinal % 7 + 1);
            var pool = ordinal % 3 == 0
                ? new GeneratedContentPoolKey("WORLD_COMMON", "V1")
                : ordinal % 3 == 1
                    ? new GeneratedContentPoolKey("MANDATORY", "V2")
                    : new GeneratedContentPoolKey("UNIQUE", "V1");
            var address = new GeneratedContentSlotAddress(
                "REFERENCE_SEED_1801", "GENERATOR_V1", "DATA_V1",
                new GeneratedSectorCoordinate(ordinal % 3, ordinal / 3),
                sliceIndex, sectorLocalIndex, sliceLocalIndex, owner,
                "MAP16_OWNER_" + owner.ToString().ToUpperInvariant(),
                "MAP16_PROVENANCE_" + ordinal.ToString("D2", CultureInfo.InvariantCulture),
                "MAP16_SLOT_" + ordinal.ToString("D2", CultureInfo.InvariantCulture),
                category, pool);
            var available = category == GeneratedContentSlotCategory.Device ||
                category == GeneratedContentSlotCategory.Activity ||
                category == GeneratedContentSlotCategory.Event ||
                category == GeneratedContentSlotCategory.Special;
            return new GeneratedContentSlotSourceRecord(address, available);
        }

        private static GeneratedContentSlotAddress Copy(
            GeneratedContentSlotAddress value,
            string worldSeed = null,
            GeneratedSectorCoordinate sector = null,
            int? sliceIndex = null,
            int? sectorLocalIndex = null,
            int? sliceLocalIndex = null,
            string sourceProvenanceToken = null,
            string sourceSlotId = null,
            GeneratedContentSlotCategory? category = null,
            GeneratedContentPoolKey poolKey = null) => new GeneratedContentSlotAddress(
                worldSeed ?? value.WorldSeed, value.GeneratorVersion, value.DataVersion,
                sector ?? value.Sector, sliceIndex ?? value.SliceIndex,
                sectorLocalIndex ?? value.SectorLocalIndex,
                sliceLocalIndex ?? value.SliceLocalIndex, value.SourceOwnerKind,
                value.SourceOwnerId, sourceProvenanceToken ?? value.SourceProvenanceToken,
                sourceSlotId ?? value.SourceSlotId, category ?? value.Category,
                poolKey ?? value.PoolKey);

        private static void AssertStable(IReadOnlyList<GeneratedContentSlotIndexEntry> values)
        {
            Assert.That(values.Count, Is.GreaterThan(0));
            Assert.That(values, Is.Ordered.Using<GeneratedContentSlotIndexEntry>(
                Comparer<GeneratedContentSlotIndexEntry>.Default));
        }

        private static void AssertFailure(
            GeneratedContentSlotIndexResult result,
            GeneratedContentSlotIndexFailureCode code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Index, Is.Null);
            Assert.That(result.Failures.Select(value => value.Code), Does.Contain(code),
                Describe(result));
        }

        private static string Describe(GeneratedContentSlotIndexResult result) => result == null
            ? "MISSING_RESULT"
            : string.Join(";", result.Failures.Select(value => value.ToString()));
    }
}
