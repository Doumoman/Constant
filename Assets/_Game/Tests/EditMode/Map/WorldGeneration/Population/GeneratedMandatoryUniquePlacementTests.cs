using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Population;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Population
{
    [TestFixture]
    [Category("MAP18_02")]
    public sealed class GeneratedMandatoryUniquePlacementTests
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

        private static GeneratedContentSlotIndex acceptedIndex;
        private static GeneratedMandatoryUniquePlacementPlan acceptedPlan;

        [Test]
        public void MandatoryUniquePreplacementCreatesRequiredTriggerAndThreeCoreResources()
        {
            var plan = Plan();
            Assert.That(plan.EntryCount, Is.EqualTo(4));
            Assert.That(plan.RequiredTriggerCount, Is.EqualTo(1));
            Assert.That(plan.CoreResourceCount, Is.EqualTo(3));
            Assert.That(plan.UniqueContentKeyCount, Is.EqualTo(4));
            foreach (GeneratedMandatoryContentKind kind in Enum.GetValues(
                         typeof(GeneratedMandatoryContentKind)))
                Assert.That(plan.Entries.Count(value => value.ContentKey.Kind == kind),
                    Is.EqualTo(1));

            TestContext.WriteLine("MAP18_02_PLACEMENT_EVIDENCE entries=4 trigger=1" +
                " core_resources=3 moon=1 cassia=1 nuruk=1 unique_keys=4");
        }

        [Test]
        public void PreplacementUsesSlotIndexStableIdsAndReservationKeysWithoutPoolRolls()
        {
            var plan = Plan();
            Assert.That(plan.UniqueStableSpawnIdCount, Is.EqualTo(4));
            Assert.That(plan.UniqueReservationKeyCount, Is.EqualTo(4));
            Assert.That(plan.Entries.All(value => ReferenceEquals(value.SelectedSlot,
                Index().Entries.Single(source => source.ReservationKey == value.ReservationKey))),
                Is.True);
            Assert.That(plan.Entries.All(value => value.StableSpawnId ==
                value.SelectedSlot.StableSpawnId), Is.True);
            Assert.That(plan.Entries.All(value => value.Rule.MaxWorldCount == 1 &&
                value.Rule.Required && value.Rule.ExactlyOne && value.Rule.WorldUnique), Is.True);
            Assert.That(plan.WeightedPoolRollCount, Is.Zero);
            Assert.That(plan.BudgetSpendCount, Is.Zero);

            TestContext.WriteLine("MAP18_02_IDENTITY_EVIDENCE source_slot_ids=4/4" +
                " stable_ids=4 reservations=4 collisions=0 max_world_count=1" +
                " pool_rolls=0 budget_spends=0 stable_id_set=" + plan.StableIdSetDigest);
        }

        [Test]
        public void CoreResourceKeysMatchMap13AuthoritativeRewardDefinitions()
        {
            var plan = Plan();
            foreach (var resource in new[]
                     {
                         CoreResourceKind.MoonCore,
                         CoreResourceKind.CassiaSap,
                         CoreResourceKind.StarNuruk,
                     })
            {
                var definition = CoreResourceRegionStarterCatalog.GetDefinition(resource);
                var expected = SpecialPersistenceKey.ForSlot(definition.RegionId,
                    SpecialPersistenceScope.Reward, definition.RequiredReward.SlotId);
                var entry = plan.Entries.Single(value => value.ContentKey.CoreResource == resource);
                Assert.That(entry.ContentKey.AuthoritativePersistenceKey, Is.EqualTo(expected));
                Assert.That(entry.ContentKey.AuthoritativePersistenceKey,
                    Is.EqualTo(definition.RequiredReward.PersistenceKey));
                Assert.That(entry.ContentKey.AuthoritativePersistenceKey.Value,
                    Does.StartWith("SR_STATE_").And.Contain("_REWARD_"));
            }

            var authoritative = GeneratedMandatoryContentCatalog.CreateAuthoritative();
            var moon = authoritative.Single(value =>
                value.Kind == GeneratedMandatoryContentKind.MoonCore);
            var legacyMoon = new GeneratedMandatoryContentKey(moon.Kind, moon.Value,
                moon.CoreResource, new SpecialPersistenceKey("MOON_CORE"));
            var rules = Rules().Select(value => value.ContentKey.Kind == moon.Kind
                ? GeneratedMandatoryUniqueRule.CreateDefault(legacyMoon) : value).ToArray();
            var rejected = Place(Index(), rules);
            AssertAtomicFailure(rejected,
                GeneratedMandatoryUniquePlacementFailureCode.LegacyShortPersistenceKeyAccepted);
            Assert.That(rejected.Failures.Any(value => value.Code ==
                GeneratedMandatoryUniquePlacementFailureCode.CoreResourceAuthoritativeIdentityMismatch),
                Is.True);

            TestContext.WriteLine("MAP18_02_MAP13_KEY_EVIDENCE moon=" +
                plan.Entry(GeneratedMandatoryContentKind.MoonCore)
                    .ContentKey.AuthoritativePersistenceKey.Value + " cassia=" +
                plan.Entry(GeneratedMandatoryContentKind.CassiaSap)
                    .ContentKey.AuthoritativePersistenceKey.Value + " nuruk=" +
                plan.Entry(GeneratedMandatoryContentKind.StarNuruk)
                    .ContentKey.AuthoritativePersistenceKey.Value +
                " authoritative_matches=3/3 legacy_short_keys_accepted=0/1");
        }

        [Test]
        public void WorldUniqueAndMaxCountRulesRejectDuplicatesAtomically()
        {
            var defaults = Rules();
            var duplicate = Place(Index(), defaults.Concat(new[] { defaults[0] }));
            AssertAtomicFailure(duplicate,
                GeneratedMandatoryUniquePlacementFailureCode.DuplicateUniqueContentKey);

            var maxZero = defaults.Select(value => value.ContentKey.Kind ==
                    GeneratedMandatoryContentKind.RequiredProgressTrigger
                ? new GeneratedMandatoryUniqueRule(value.ContentKey,
                    value.CategoryPreference, 0) : value).ToArray();
            var maximum = Place(Index(), maxZero);
            AssertAtomicFailure(maximum,
                GeneratedMandatoryUniquePlacementFailureCode.MaxWorldCountExceeded);

            TestContext.WriteLine("MAP18_02_UNIQUE_RULE_EVIDENCE duplicate_rejections=1/1" +
                " max_count_rejections=1/1 atomic=YES partial_entries=0 mutations=0 retries=0");
        }

        [Test]
        public void PreplacementIsStableAcrossRepeatReverseCultureAndCandidateOrder()
        {
            var baseline = Plan();
            var repeat = Place(Index(), Rules()).Plan;
            var reverseRules = Place(Index(), Rules().Reverse()).Plan;
            var reverseCandidates = Place(BuildIndex(Sources().Reverse()), Rules()).Plan;
            GeneratedMandatoryUniquePlacementPlan culture;
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                culture = Place(BuildIndex(Sources().Skip(4).Concat(Sources().Take(4))),
                    Rules()).Plan;
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }

            Assert.That(new[] { baseline.Digest, repeat.Digest, reverseRules.Digest,
                reverseCandidates.Digest, culture.Digest }
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
            Assert.That(new[] { baseline.StableIdSetDigest, repeat.StableIdSetDigest,
                reverseRules.StableIdSetDigest, reverseCandidates.StableIdSetDigest,
                culture.StableIdSetDigest }.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));

            var changedRules = Rules().Select(value => value.ContentKey.Kind ==
                    GeneratedMandatoryContentKind.RequiredProgressTrigger
                ? new GeneratedMandatoryUniqueRule(value.ContentKey, new[]
                {
                    GeneratedContentSlotCategory.Activity,
                    GeneratedContentSlotCategory.Event,
                    GeneratedContentSlotCategory.Device,
                    GeneratedContentSlotCategory.Special,
                }) : value).ToArray();
            var changed = Place(Index(), changedRules).Plan;
            Assert.That(changed.Digest, Is.Not.EqualTo(baseline.Digest));
            Assert.That(changed.Entry(GeneratedMandatoryContentKind.RequiredProgressTrigger)
                .SelectedSlot.Address.SourceSlotId, Is.EqualTo("MAP16_SLOT_06"));

            TestContext.WriteLine("MAP18_02_DIGEST_EVIDENCE plan=" + baseline.Digest +
                " stable_ids=" + baseline.StableIdSetDigest +
                " repeat_reverse_rule_reverse_candidate_culture_mismatches=0/0/0/0" +
                " preference_mutation_changes=1/1");
        }

        [Test]
        public void SelectionUsesStableSlotOrderAndDoesNotInventSlots()
        {
            var plan = Plan();
            var expected = new Dictionary<GeneratedMandatoryContentKind, string>
            {
                { GeneratedMandatoryContentKind.RequiredProgressTrigger, "MAP16_SLOT_07" },
                { GeneratedMandatoryContentKind.MoonCore, "MAP16_SLOT_08" },
                { GeneratedMandatoryContentKind.CassiaSap, "MAP16_SLOT_11" },
                { GeneratedMandatoryContentKind.StarNuruk, "MAP16_SLOT_05" },
            };
            foreach (var pair in expected)
                Assert.That(plan.Entry(pair.Key).SelectedSlot.Address.SourceSlotId,
                    Is.EqualTo(pair.Value));
            Assert.That(plan.RemainingCandidateCount, Is.EqualTo(1));
            Assert.That(plan.RemainingCandidates[0].Address.SourceSlotId,
                Is.EqualTo("MAP16_SLOT_06"));
            Assert.That(plan.ImplicitSlotCreationCount, Is.Zero);
            Assert.That(plan.CandidateMutationCount, Is.Zero);
            Assert.That(plan.RetryLoopCount, Is.Zero);
            Assert.That(plan.SourceIndex.Digest, Is.EqualTo(Index().Digest));

            TestContext.WriteLine("MAP18_02_SELECTION_EVIDENCE trigger=MAP16_SLOT_07" +
                " moon=MAP16_SLOT_08 cassia=MAP16_SLOT_11 nuruk=MAP16_SLOT_05" +
                " remaining=MAP16_SLOT_06 implicit_slots=0 candidate_mutations=0 retries=0");
        }

        [Test]
        public void MissingCandidateDigestMismatchAndReservationCollisionFailAtomically()
        {
            var belowMinimum = Place(BuildIndex(Sources(3)), Rules());
            AssertAtomicFailure(belowMinimum,
                GeneratedMandatoryUniquePlacementFailureCode.MandatoryCandidateCountBelowMinimum);

            var slotDigestMismatch = Place(Index(), Rules(),
                expectedSlotIndexDigest: MutateDigest(Index().Digest));
            AssertAtomicFailure(slotDigestMismatch,
                GeneratedMandatoryUniquePlacementFailureCode.SlotIndexDigestMismatch);

            var stableDigestMismatch = Place(Index(), Rules(),
                expectedStableIdSetDigest: MutateDigest(Index().StableIdSetDigest));
            AssertAtomicFailure(stableDigestMismatch,
                GeneratedMandatoryUniquePlacementFailureCode.StableIdSetDigestMismatch);

            var noMoon = Rules().Select(value => value.ContentKey.Kind ==
                    GeneratedMandatoryContentKind.MoonCore
                ? new GeneratedMandatoryUniqueRule(value.ContentKey,
                    new[] { GeneratedContentSlotCategory.Resource }) : value).ToArray();
            var missingMoon = Place(Index(), noMoon);
            AssertAtomicFailure(missingMoon,
                GeneratedMandatoryUniquePlacementFailureCode.MissingMoonCoreCandidate);

            var first = Plan().Entries[0];
            var reservationCollision = Place(Index(), Rules(),
                existingReservationKeys: new[] { first.ReservationKey });
            AssertAtomicFailure(reservationCollision,
                GeneratedMandatoryUniquePlacementFailureCode.ReservationKeyCollision);
            var stableCollision = Place(Index(), Rules(),
                existingStableSpawnIds: new[] { first.StableSpawnId.Value });
            AssertAtomicFailure(stableCollision,
                GeneratedMandatoryUniquePlacementFailureCode.StableSpawnIdCollision);
            var planDigestMismatch = Place(Index(), Rules(),
                expectedPlanDigest: MutateDigest(Plan().Digest));
            AssertAtomicFailure(planDigestMismatch,
                GeneratedMandatoryUniquePlacementFailureCode.PlanDigestMismatch);

            TestContext.WriteLine("MAP18_02_FAILURE_EVIDENCE below_minimum=1/1" +
                " slot_digest=1/1 stable_set_digest=1/1 missing_moon=1/1" +
                " reservation_collision=1/1 stable_id_collision=1/1 plan_digest=1/1" +
                " atomic=YES partial_entries=0 mutations=0 retries=0");
        }

        [Test]
        public void PreplacementDoesNotSpawnObjectsMutateScenesWriteSavesOrLoadAssets()
        {
            var plan = Plan();
            Assert.That(new[]
            {
                plan.RuntimeContentPlacementCount, plan.WeightedPoolRollCount,
                plan.BudgetSpendCount, plan.RewardGrantCount, plan.InventoryMutationCount,
                plan.DeviceExecutionCount, plan.RuntimeObjectSpawnCount,
                plan.GameObjectInstantiateCount, plan.GameObjectEnableCount,
                plan.GameObjectDisableCount, plan.GameObjectDestroyCount,
                plan.SystemIoFileWriteCount, plan.SystemIoFileReadCount,
                plan.DiskSaveFileCreateCount, plan.DiskLoadFileCreateCount,
                plan.UserSaveSlotWriteCount, plan.PlatformStorageWriteCount,
                plan.TilemapComponentWriteCount, plan.TilemapSetTileCallCount,
                plan.TilemapSetTilesCallCount, plan.TilemapSetTilesBlockCallCount,
                plan.TilemapClearAllTilesCallCount, plan.TilemapColliderCreationCount,
                plan.CompositeColliderCreationCount, plan.ColliderCreationCount,
                plan.RigidbodyCreationCount, plan.PhysicsQueryCount,
                plan.PhysicsSimulationCount, plan.SceneMutationCount,
                plan.PrefabMutationCount, plan.TilemapMutationCount,
                plan.CameraReadCount, plan.CameraWriteCount, plan.AddressablesLoadCount,
                plan.ResourcesLoadCount, plan.AssetDatabaseLoadCount,
                plan.AuthoringCsvEditCount, plan.GeneratedCsvCommitCount,
                plan.GeneratedAssetCommitCount, plan.ProductionSeedApprovalCount,
                plan.ImplicitSlotCreationCount, plan.CandidateMutationCount,
                plan.RetryLoopCount,
            }, Is.All.Zero);

            TestContext.WriteLine("MAP18_02_SIDE_EFFECT_EVIDENCE logical_entries=4" +
                " placement_roll_budget_reward_inventory_device_spawn=0/0/0/0/0/0/0" +
                " gameobject=0/0/0/0 system_io=0/0 disk_save_load=0/0" +
                " user_platform=0/0 tilemap=0/0/0/0/0 colliders=0/0/0 rigidbody=0" +
                " physics=0/0 scene_prefab_tilemap=0/0/0 camera=0/0" +
                " asset_loads=0/0/0 csv=0 generated=0/0 seed=0");
        }

        [Test]
        public void PreplacementReportsExclusionSurfaceForMap18_03()
        {
            var plan = Plan();
            Assert.That(plan.ExclusionCount, Is.EqualTo(4));
            Assert.That(plan.Exclusions.Select(value => value.ReservationKey),
                Is.Ordered.Using<string>(StringComparer.Ordinal));
            foreach (var entry in plan.Entries)
            {
                Assert.That(plan.IsReserved(entry.ReservationKey), Is.True);
                Assert.That(plan.TryGetExclusion(entry.ReservationKey, out var exclusion), Is.True);
                Assert.That(exclusion.ContentKey, Is.SameAs(entry.ContentKey));
                Assert.That(exclusion.StableSpawnId, Is.EqualTo(entry.StableSpawnId));
            }
            Assert.That(plan.RemainingCandidateCount, Is.EqualTo(1));

            TestContext.WriteLine("MAP18_02_EXCLUSION_EVIDENCE exclusions=4" +
                " reserved_lookup=4/4 remaining_candidates=1" +
                " downstream=MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS");
        }

        [Test]
        public void Map18HandoffKeepsMap18_03Locked()
        {
            var plan = Plan();
            Assert.That(GeneratedMandatoryUniquePlacementPlan.DownstreamOwner,
                Is.EqualTo("MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS"));
            Assert.That(GeneratedMandatoryUniquePlacementPlan.OpensDownstreamTask, Is.False);
            Assert.That(plan.Map18_03Started, Is.False);

            TestContext.WriteLine("MAP18_02_DECISION_EVIDENCE result=PASS" +
                " downstream=MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS" +
                " started=NO locked=YES");
        }

        private static GeneratedContentSlotIndex Index()
        {
            if (acceptedIndex != null) return acceptedIndex;
            acceptedIndex = BuildIndex(Sources());
            Assert.That(acceptedIndex.Digest,
                Is.EqualTo(GeneratedMandatoryUniqueContentPreplacer.ExpectedSlotIndexDigest));
            Assert.That(acceptedIndex.StableIdSetDigest,
                Is.EqualTo(GeneratedMandatoryUniqueContentPreplacer.ExpectedStableIdSetDigest));
            return acceptedIndex;
        }

        private static GeneratedMandatoryUniquePlacementPlan Plan()
        {
            if (acceptedPlan != null) return acceptedPlan;
            var result = Place(Index(), Rules());
            Assert.That(result.Success, Is.True, Describe(result));
            acceptedPlan = result.Plan;
            return acceptedPlan;
        }

        private static IReadOnlyList<GeneratedMandatoryUniqueRule> Rules() =>
            GeneratedMandatoryUniqueContentPreplacer.CreateDefaultRules();

        private static GeneratedMandatoryUniquePlacementResult Place(
            GeneratedContentSlotIndex index,
            IEnumerable<GeneratedMandatoryUniqueRule> rules,
            string expectedSlotIndexDigest = null,
            string expectedStableIdSetDigest = null,
            IEnumerable<string> existingReservationKeys = null,
            IEnumerable<string> existingStableSpawnIds = null,
            string expectedPlanDigest = null)
        {
            var request = new GeneratedMandatoryUniquePlacementRequest(index, rules,
                CoreResourceRegionStarterCatalog.Entries,
                expectedSlotIndexDigest ?? GeneratedMandatoryUniqueContentPreplacer.ExpectedSlotIndexDigest,
                expectedStableIdSetDigest ?? GeneratedMandatoryUniqueContentPreplacer.ExpectedStableIdSetDigest,
                GeneratedMandatoryUniqueContentPreplacer.ExpectedSourceRecordCount,
                GeneratedMandatoryUniqueContentPreplacer.ExpectedMandatoryUniqueCandidateCount,
                existingReservationKeys, existingStableSpawnIds, expectedPlanDigest);
            return GeneratedMandatoryUniqueContentPreplacer.Preplace(request);
        }

        private static GeneratedContentSlotIndex BuildIndex(
            IEnumerable<GeneratedContentSlotSourceRecord> sources)
        {
            var result = GeneratedContentSlotIndexBuilder.Build(
                new GeneratedContentSlotIndexRequest(sources,
                    GeneratedContentSlotIndexBuilder.ExpectedMap17AuditDigest,
                    "PASS", true, 2));
            Assert.That(result.Success, Is.True, string.Join("\n", result.Failures));
            return result.Index;
        }

        private static GeneratedContentSlotSourceRecord[] Sources(int eligibleLimit = 5)
        {
            var eligibleOrdinal = 0;
            return Enumerable.Range(0, 12).Select(ordinal =>
            {
                var source = Source(ordinal);
                var eligible = source.AvailableForMandatoryUniquePreplacement &&
                    eligibleOrdinal++ < eligibleLimit;
                return new GeneratedContentSlotSourceRecord(source.Address, eligible);
            }).ToArray();
        }

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

        private static void AssertAtomicFailure(
            GeneratedMandatoryUniquePlacementResult result,
            GeneratedMandatoryUniquePlacementFailureCode code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.Failures.Any(value => value.Code == code), Is.True,
                Describe(result));
            Assert.That(result.PartialPlacementEntryCount, Is.Zero);
            Assert.That(result.PartialMutationCount, Is.Zero);
            Assert.That(result.RetryLoopCount, Is.Zero);
        }

        private static string MutateDigest(string value) =>
            (value[0] == '0' ? "1" : "0") + value.Substring(1);

        private static string Describe(GeneratedMandatoryUniquePlacementResult result) =>
            result == null ? "NULL_RESULT" : string.Join("\n", result.Failures.Select(
                value => value.ToString()));
    }
}
