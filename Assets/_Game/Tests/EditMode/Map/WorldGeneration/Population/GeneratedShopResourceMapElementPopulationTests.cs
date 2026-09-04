using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Population;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Population
{
    [TestFixture]
    [Category("MAP18_03")]
    public sealed class GeneratedShopResourceMapElementPopulationTests
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
        private static GeneratedMandatoryUniquePlacementPlan acceptedMandatoryPlan;
        private static GeneratedPopulationPlacementPlan acceptedPopulationPlan;

        [Test]
        public void ShopResourceMapElementPopulationCreatesThreeLogicalGroups()
        {
            var plan = Plan();
            Assert.That(plan.PoolEntryCount, Is.EqualTo(3));
            Assert.That(plan.LogicalGroupCount, Is.EqualTo(3));
            Assert.That(plan.EntryCount, Is.EqualTo(3));
            Assert.That(plan.ShopInventoryEntryCount, Is.EqualTo(1));
            Assert.That(plan.OptionalResourceEntryCount, Is.EqualTo(1));
            Assert.That(plan.NeutralMapElementEntryCount, Is.EqualTo(1));
            Assert.That(plan.UniqueContentKeyCount, Is.EqualTo(3));

            TestContext.WriteLine("MAP18_03_GROUP_EVIDENCE groups=3 entries=3" +
                " shop=1 optional_resource=1 neutral_map_element=1 pools=3 unique_keys=3");
        }

        [Test]
        public void PopulationRespectsMandatoryUniqueExclusionsAndReservedSlots()
        {
            var plan = Plan();
            var mandatoryKeys = plan.MandatoryPlan.Exclusions.Select(value =>
                value.ReservationKey).ToArray();
            Assert.That(plan.MandatoryExclusionCount, Is.EqualTo(4));
            Assert.That(plan.Entries.Select(value => value.ReservationKey)
                .Intersect(mandatoryKeys, StringComparer.Ordinal), Is.Empty);
            Assert.That(plan.UniqueReservationKeyCount, Is.EqualTo(3));
            Assert.That(plan.UniqueStableSpawnIdCount, Is.EqualTo(3));
            Assert.That(plan.OccupiedSurfaceCount, Is.EqualTo(7));
            foreach (var exclusion in plan.MandatoryPlan.Exclusions)
                Assert.That(plan.IsOccupied(exclusion.ReservationKey), Is.True);
            Assert.That(plan.Entries.Select(value => value.SelectedSlot.Address.SourceSlotId),
                Has.None.EqualTo("MAP16_SLOT_05"));
            Assert.That(plan.Entries.Select(value => value.SelectedSlot.Address.SourceSlotId),
                Has.None.EqualTo("MAP16_SLOT_07"));
            Assert.That(plan.Entries.Select(value => value.SelectedSlot.Address.SourceSlotId),
                Has.None.EqualTo("MAP16_SLOT_08"));
            Assert.That(plan.Entries.Select(value => value.SelectedSlot.Address.SourceSlotId),
                Has.None.EqualTo("MAP16_SLOT_11"));

            TestContext.WriteLine("MAP18_03_EXCLUSION_EVIDENCE consumed=4/4" +
                " required_core_slots_excluded=4/4 reserved_reuse=0" +
                " reservation_collisions=0 stable_id_collisions=0 occupied=7");
        }

        [Test]
        public void ShopInventoryEntriesAreLogicalAndDoNotMutateEconomyOrInventory()
        {
            var plan = Plan();
            var shop = plan.Entry(GeneratedPopulationContentKind.ShopInventory);
            Assert.That(shop.ContentKey, Is.EqualTo("SHOP_STOCK_GENERAL"));
            Assert.That(shop.PoolEntry.SymbolicPriceTierKey,
                Is.EqualTo("PRICE_TIER_COMMON"));
            Assert.That(shop.PoolEntry.PoolKey.IsValid, Is.True);
            Assert.That(shop.FilterProof.Accepted, Is.True);
            Assert.That(new[]
            {
                plan.ActualShopTransactionCount, plan.PriceExecutionCount,
                plan.WalletCurrencyMutationCount, plan.ItemGrantCount,
                plan.ResourcePickupGrantCount, plan.InventoryMutationCount,
                plan.DeviceExecutionCount, plan.RuntimeObjectSpawnCount,
            }, Is.All.Zero);

            TestContext.WriteLine("MAP18_03_SHOP_BOUNDARY_EVIDENCE logical_stock=1" +
                " stable_stock_keys=1 symbolic_price_tiers=1 transactions=0" +
                " price_executions=0 wallet_currency_mutations=0 item_grants=0" +
                " pickup_grants=0 inventory_mutations=0 merchant_spawns=0");
        }

        [Test]
        public void ResourceAndMapElementEntriesApplyBiomeToolInteractionNeighborAndSafeFilters()
        {
            var plan = Plan();
            AssertAcceptedAndRejected(plan.FilterEvidence, value => value.BiomeAccepted);
            AssertAcceptedAndRejected(plan.FilterEvidence, value => value.ResourceAccepted);
            AssertAcceptedAndRejected(plan.FilterEvidence, value => value.ToolAccepted);
            AssertAcceptedAndRejected(plan.FilterEvidence,
                value => value.InteractionRadiusAccepted);
            AssertAcceptedAndRejected(plan.FilterEvidence, value => value.SafeRadiusAccepted);
            AssertAcceptedAndRejected(plan.FilterEvidence, value => value.NeighborRadiusAccepted);
            AssertAcceptedAndRejected(plan.FilterEvidence,
                value => value.MandatoryExclusionAccepted);
            Assert.That(plan.Entries.All(value => value.FilterProof.Accepted), Is.True);
            Assert.That(plan.Entries.All(value =>
                Biomes().TryGetProfile(value.Candidate.Biome, out _)), Is.True);

            TestContext.WriteLine("MAP18_03_FILTER_EVIDENCE evaluations=" +
                plan.FilterEvidence.Count + " biome=" + Counts(plan.FilterEvidence,
                    value => value.BiomeAccepted) + " resource=" +
                Counts(plan.FilterEvidence, value => value.ResourceAccepted) + " tool=" +
                Counts(plan.FilterEvidence, value => value.ToolAccepted) + " interaction=" +
                Counts(plan.FilterEvidence, value => value.InteractionRadiusAccepted) +
                " safe=" + Counts(plan.FilterEvidence, value => value.SafeRadiusAccepted) +
                " neighbor=" + Counts(plan.FilterEvidence, value => value.NeighborRadiusAccepted) +
                " mandatory_exclusion=" + Counts(plan.FilterEvidence,
                    value => value.MandatoryExclusionAccepted));
        }

        [Test]
        public void PopulationSelectionUsesStableOrderAndDeterministicHashWithoutUnityRandom()
        {
            var plan = Plan();
            Assert.That(plan.DeterministicHashTicketSelectionCount, Is.EqualTo(3));
            Assert.That(plan.Entries.All(value =>
                BakingCanonicalDigest.IsLowerHexSha256(
                    value.FilterProof.DeterministicTicket)), Is.True);
            Assert.That(plan.Entries.All(value => ReferenceEquals(value.SelectedSlot,
                Index().Entries.Single(slot => slot.ReservationKey == value.ReservationKey))),
                Is.True);
            Assert.That(plan.UnityEngineRandomCallCount, Is.Zero);
            Assert.That(plan.RandomRangeCallCount, Is.Zero);
            Assert.That(plan.SystemRandomDirectUsageCount, Is.Zero);
            Assert.That(plan.HiddenRetryLoopCount, Is.Zero);
            Assert.That(plan.ImplicitCandidateCreationCount, Is.Zero);
            Assert.That(plan.CandidateMutationCount, Is.Zero);

            TestContext.WriteLine("MAP18_03_SELECTION_EVIDENCE stable_order=YES" +
                " deterministic_hash_tickets=3 UnityEngine.Random=0 Random.Range=0" +
                " System.Random=0 retries=0 implicit_candidates=0 candidate_mutations=0");
        }

        [Test]
        public void PopulationPlanPublishesOccupiedSurfaceForMap18_04()
        {
            var plan = Plan();
            Assert.That(plan.OccupiedSurfaceCount, Is.EqualTo(7));
            Assert.That(plan.OccupiedSurface.Count(value => value.Owner == "MAP18_02"),
                Is.EqualTo(4));
            Assert.That(plan.OccupiedSurface.Count(value => value.Owner == "MAP18_03"),
                Is.EqualTo(3));
            Assert.That(plan.OccupiedSurface.Select(value => value.ReservationKey)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(7));
            Assert.That(plan.OccupiedSurface.Select(value => value.StableSpawnId.Value)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(7));
            Assert.That(plan.RemainingCandidateCount, Is.EqualTo(5));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                plan.OccupiedSurfaceDigest), Is.True);

            TestContext.WriteLine("MAP18_03_OCCUPIED_SURFACE_EVIDENCE" +
                " mandatory=4 population=3 total=7 remaining=5 digest=" +
                plan.OccupiedSurfaceDigest);
        }

        [Test]
        public void MissingCandidateDigestMismatchFilterAndReservationFailuresAreAtomic()
        {
            var defaults = Pools();
            var missingShopPools = Replace(defaults,
                GeneratedPopulationContentKind.ShopInventory,
                value => Copy(value, allowedBiomes: new[] { MoonpalaceBiomeId.MoonCrater }));
            var missing = Place(Index(), MandatoryPlan(), missingShopPools, Contexts(Index()));
            AssertAtomicFailure(missing,
                GeneratedPopulationPlacementFailureCode.MissingShopInventoryCandidate);
            Assert.That(missing.Failures.Any(value => value.Code ==
                GeneratedPopulationPlacementFailureCode.FilterMismatch), Is.True);

            var mandatoryDigest = Place(Index(), MandatoryPlan(), defaults, Contexts(Index()),
                expectedMandatoryPlanDigest: MutateDigest(MandatoryPlan().Digest));
            AssertAtomicFailure(mandatoryDigest,
                GeneratedPopulationPlacementFailureCode.MandatoryPlanDigestMismatch);
            var mandatoryStable = Place(Index(), MandatoryPlan(), defaults, Contexts(Index()),
                expectedMandatoryStableIdSetDigest:
                    MutateDigest(MandatoryPlan().StableIdSetDigest));
            AssertAtomicFailure(mandatoryStable,
                GeneratedPopulationPlacementFailureCode.MandatoryStableIdSetDigestMismatch);
            var planDigest = Place(Index(), MandatoryPlan(), defaults, Contexts(Index()),
                expectedPopulationPlanDigest: MutateDigest(Plan().Digest));
            AssertAtomicFailure(planDigest,
                GeneratedPopulationPlacementFailureCode.PopulationPlanDigestMismatch);
            var occupiedDigest = Place(Index(), MandatoryPlan(), defaults, Contexts(Index()),
                expectedOccupiedSurfaceDigest: MutateDigest(Plan().OccupiedSurfaceDigest));
            AssertAtomicFailure(occupiedDigest,
                GeneratedPopulationPlacementFailureCode.OccupiedSurfaceDigestMismatch);

            var invalidFilterPools = Replace(defaults,
                GeneratedPopulationContentKind.OptionalResource,
                value => Copy(value, minimumSafeRadius: -1));
            var invalidFilter = Place(Index(), MandatoryPlan(), invalidFilterPools,
                Contexts(Index()));
            AssertAtomicFailure(invalidFilter,
                GeneratedPopulationPlacementFailureCode.InvalidFilterRule);
            var invalidKeyPools = Replace(defaults,
                GeneratedPopulationContentKind.NeutralMapElement,
                value => Copy(value, poolKey: new GeneratedContentPoolKey(string.Empty, "V1")));
            var invalidKey = Place(Index(), MandatoryPlan(), invalidKeyPools, Contexts(Index()));
            AssertAtomicFailure(invalidKey,
                GeneratedPopulationPlacementFailureCode.InvalidPoolKey);

            var reservedPools = Replace(defaults,
                GeneratedPopulationContentKind.ShopInventory,
                value => Copy(value,
                    categories: new[] { GeneratedContentSlotCategory.Event },
                    maximumInteractionRadius: 4));
            var reserved = Place(Index(), MandatoryPlan(), reservedPools, Contexts(Index()));
            AssertAtomicFailure(reserved,
                GeneratedPopulationPlacementFailureCode.ReservedMandatorySlotReuse);
            Assert.That(reserved.Failures.Any(value => value.Code ==
                GeneratedPopulationPlacementFailureCode.FilterMismatch), Is.True);

            var neighborPools = Replace(defaults,
                GeneratedPopulationContentKind.ShopInventory,
                value => Copy(value, minimumNeighborRadius: 100));
            var neighbor = Place(Index(), MandatoryPlan(), neighborPools, Contexts(Index()));
            AssertAtomicFailure(neighbor,
                GeneratedPopulationPlacementFailureCode.NeighborCollision);
            Assert.That(neighbor.Failures.Any(value => value.Code ==
                GeneratedPopulationPlacementFailureCode.FilterMismatch), Is.True);

            var first = Plan().Entries[0];
            var reservation = Place(Index(), MandatoryPlan(), defaults, Contexts(Index()),
                existingReservationKeys: new[] { first.ReservationKey });
            AssertAtomicFailure(reservation,
                GeneratedPopulationPlacementFailureCode.ReservationKeyCollision);
            var stableId = Place(Index(), MandatoryPlan(), defaults, Contexts(Index()),
                existingStableSpawnIds: new[] { first.StableSpawnId.Value });
            AssertAtomicFailure(stableId,
                GeneratedPopulationPlacementFailureCode.StableSpawnIdCollision);
            var sideEffect = Place(Index(), MandatoryPlan(), defaults, Contexts(Index()),
                attemptedRuntimeSpawn: true, attemptedShopTransaction: true);
            AssertAtomicFailure(sideEffect,
                GeneratedPopulationPlacementFailureCode.AttemptedRuntimeSpawnOrShopTransaction);

            TestContext.WriteLine("MAP18_03_FAILURE_EVIDENCE missing_candidate=1/1" +
                " digest_mismatches=4/4 filter_mismatches=3/3 invalid_rules=2/2" +
                " reserved_reuse=1/1" +
                " neighbor_collision=1/1 reservation_collision=1/1 stable_id_collision=1/1" +
                " runtime_spawn_transaction=1/1 atomic_partial_entries_mutations_retries=0/0/0");
        }

        [Test]
        public void PopulationDigestsAreStableAcrossRepeatReverseCultureAndCandidateOrder()
        {
            var baseline = Plan();
            var repeat = Place(Index(), MandatoryPlan(), Pools(), Contexts(Index())).Plan;
            var reverse = Place(Index(), MandatoryPlan(), Pools().Reverse(),
                Contexts(Index()).Reverse()).Plan;
            var reorderedIndex = BuildIndex(Sources().Reverse());
            var reorderedMandatory = BuildMandatoryPlan(reorderedIndex);
            var candidateOrder = Place(reorderedIndex, reorderedMandatory, Pools(),
                Contexts(reorderedIndex).Skip(4).Concat(Contexts(reorderedIndex).Take(4))).Plan;
            GeneratedPopulationPlacementPlan culture;
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                culture = Place(Index(), MandatoryPlan(), Pools(), Contexts(Index())).Plan;
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }

            Assert.That(new[] { baseline.Digest, repeat.Digest, reverse.Digest,
                culture.Digest, candidateOrder.Digest }.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(1));
            Assert.That(new[] { baseline.OccupiedSurfaceDigest,
                repeat.OccupiedSurfaceDigest, reverse.OccupiedSurfaceDigest,
                culture.OccupiedSurfaceDigest, candidateOrder.OccupiedSurfaceDigest }
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));

            var changedPools = Replace(Pools(),
                GeneratedPopulationContentKind.NeutralMapElement,
                value => Copy(value, contentKey: value.ContentKey + "_MUTATED"));
            var changed = Place(Index(), MandatoryPlan(), changedPools, Contexts(Index())).Plan;
            Assert.That(changed.Digest, Is.Not.EqualTo(baseline.Digest));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(baseline.Digest), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                baseline.OccupiedSurfaceDigest), Is.True);

            TestContext.WriteLine("MAP18_03_DIGEST_EVIDENCE plan=" + baseline.Digest +
                " occupied=" + baseline.OccupiedSurfaceDigest +
                " repeat_reverse_culture_candidate_order_mismatches=0/0/0/0" +
                " mutation_sensitivity=1/1");
        }

        [Test]
        public void PopulationDoesNotSpawnObjectsMutateScenesWriteSavesLoadAssetsOrRunRegressions()
        {
            var plan = Plan();
            Assert.That(new[]
            {
                plan.RuntimeContentPlacementCount, plan.HazardPlacementCount,
                plan.EnemyPlacementCount, plan.HierarchicalCombatBudgetSpendCount,
                plan.ActualShopTransactionCount, plan.PriceExecutionCount,
                plan.WalletCurrencyMutationCount, plan.ItemGrantCount,
                plan.ResourcePickupGrantCount, plan.InventoryMutationCount,
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
                plan.CameraReadCount, plan.CameraWriteCount,
                plan.AddressablesLoadCount, plan.ResourcesLoadCount,
                plan.AssetDatabaseLoadCount, plan.AuthoringCsvEditCount,
                plan.GeneratedCsvCommitCount, plan.GeneratedAssetCommitCount,
                plan.ProductionSeedApprovalCount, plan.PriorTaskTestSelectionCount,
                plan.Legacy19347SelectionCount, plan.PlayModeSelectionCount,
                plan.UnfilteredTestSelectionCount, plan.FullRegressionRunCount,
            }, Is.All.Zero);

            TestContext.WriteLine("MAP18_03_SIDE_EFFECT_EVIDENCE" +
                " runtime_hazard_enemy_budget=0/0/0/0 spawn=0 gameobject=0/0/0/0" +
                " system_io=0/0 disk=0/0 user_platform=0/0 tilemap=0/0/0/0/0" +
                " colliders=0/0/0 rigidbody=0 physics=0/0 scene_prefab_tilemap=0/0/0" +
                " camera=0/0 asset_loads=0/0/0 csv=0 generated=0/0 seed=0" +
                " prior_legacy_playmode_unfiltered_full=0/0/0/0/0");
        }

        [Test]
        public void Map18HandoffKeepsMap18_04Locked()
        {
            var plan = Plan();
            Assert.That(GeneratedPopulationPlacementPlan.DownstreamOwner,
                Is.EqualTo("MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS"));
            Assert.That(GeneratedPopulationPlacementPlan.OpensDownstreamTask, Is.False);
            Assert.That(plan.Map18_04Started, Is.False);

            TestContext.WriteLine("MAP18_03_DECISION_EVIDENCE result=PASS" +
                " downstream=MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS" +
                " started=NO locked=YES");
        }

        private static GeneratedContentSlotIndex Index()
        {
            if (acceptedIndex != null) return acceptedIndex;
            acceptedIndex = BuildIndex(Sources());
            Assert.That(acceptedIndex.Digest,
                Is.EqualTo(GeneratedMandatoryUniqueContentPreplacer.ExpectedSlotIndexDigest));
            return acceptedIndex;
        }

        private static GeneratedMandatoryUniquePlacementPlan MandatoryPlan()
        {
            if (acceptedMandatoryPlan != null) return acceptedMandatoryPlan;
            acceptedMandatoryPlan = BuildMandatoryPlan(Index());
            Assert.That(acceptedMandatoryPlan.Digest,
                Is.EqualTo(GeneratedShopResourceMapElementPopulator.ExpectedMandatoryPlanDigest));
            Assert.That(acceptedMandatoryPlan.StableIdSetDigest,
                Is.EqualTo(GeneratedShopResourceMapElementPopulator
                    .ExpectedMandatoryStableIdSetDigest));
            return acceptedMandatoryPlan;
        }

        private static GeneratedPopulationPlacementPlan Plan()
        {
            if (acceptedPopulationPlan != null) return acceptedPopulationPlan;
            var result = Place(Index(), MandatoryPlan(), Pools(), Contexts(Index()));
            Assert.That(result.Success, Is.True, Describe(result));
            acceptedPopulationPlan = result.Plan;
            return acceptedPopulationPlan;
        }

        private static MicroPatternBiomeProfileCatalog Biomes() =>
            MicroPatternBiomeProfileCatalog.CreateBuiltIn();

        private static IReadOnlyList<GeneratedPopulationPoolEntry> Pools() =>
            GeneratedPopulationPoolCatalog.CreateDefault(Biomes());

        private static GeneratedPopulationPlacementResult Place(
            GeneratedContentSlotIndex index,
            GeneratedMandatoryUniquePlacementPlan mandatoryPlan,
            IEnumerable<GeneratedPopulationPoolEntry> pools,
            IEnumerable<GeneratedPopulationCandidateContext> contexts,
            string expectedMandatoryPlanDigest = null,
            string expectedMandatoryStableIdSetDigest = null,
            IEnumerable<string> existingReservationKeys = null,
            IEnumerable<string> existingStableSpawnIds = null,
            string expectedPopulationPlanDigest = null,
            string expectedOccupiedSurfaceDigest = null,
            bool attemptedRuntimeSpawn = false,
            bool attemptedShopTransaction = false)
        {
            var request = new GeneratedPopulationPlacementRequest(index, mandatoryPlan,
                pools, contexts, Biomes(), expectedMandatoryPlanDigest ??
                    GeneratedShopResourceMapElementPopulator.ExpectedMandatoryPlanDigest,
                expectedMandatoryStableIdSetDigest ?? GeneratedShopResourceMapElementPopulator
                    .ExpectedMandatoryStableIdSetDigest,
                GeneratedShopResourceMapElementPopulator.ExpectedMandatoryExclusionCount,
                existingReservationKeys, existingStableSpawnIds,
                expectedPopulationPlanDigest, expectedOccupiedSurfaceDigest,
                attemptedRuntimeSpawn, attemptedShopTransaction);
            return GeneratedShopResourceMapElementPopulator.Populate(request);
        }

        private static GeneratedMandatoryUniquePlacementPlan BuildMandatoryPlan(
            GeneratedContentSlotIndex index)
        {
            var request = new GeneratedMandatoryUniquePlacementRequest(index,
                GeneratedMandatoryUniqueContentPreplacer.CreateDefaultRules(),
                CoreResourceRegionStarterCatalog.Entries,
                GeneratedMandatoryUniqueContentPreplacer.ExpectedSlotIndexDigest,
                GeneratedMandatoryUniqueContentPreplacer.ExpectedStableIdSetDigest,
                GeneratedMandatoryUniqueContentPreplacer.ExpectedSourceRecordCount,
                GeneratedMandatoryUniqueContentPreplacer.ExpectedMandatoryUniqueCandidateCount);
            var result = GeneratedMandatoryUniqueContentPreplacer.Preplace(request);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Failures));
            return result.Plan;
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

        private static GeneratedPopulationCandidateContext[] Contexts(
            GeneratedContentSlotIndex index) => index.Entries.Select(slot =>
        {
            var ordinal = int.Parse(slot.Address.SourceSlotId.Substring(
                slot.Address.SourceSlotId.Length - 2), CultureInfo.InvariantCulture);
            var resource = slot.Address.Category == GeneratedContentSlotCategory.Resource ||
                slot.Address.Category == GeneratedContentSlotCategory.Pickup;
            var biomes = new[]
            {
                MoonpalaceBiomeId.MoonCrater,
                MoonpalaceBiomeId.CassiaRoot,
                MoonpalaceBiomeId.AbandonedMill,
                MoonpalaceBiomeId.MoonDough,
            };
            return new GeneratedPopulationCandidateContext(slot, biomes[ordinal % biomes.Length],
                resource ? "RESOURCE_GENERIC" : string.Empty,
                resource ? GeneratedPopulationToolRequirement.BasicHarvestTool :
                    GeneratedPopulationToolRequirement.None,
                1 + ordinal % 4, 2 + ordinal % 4, 8 + ordinal % 4);
        }).ToArray();

        private static GeneratedContentSlotSourceRecord[] Sources() =>
            Enumerable.Range(0, 12).Select(Source).ToArray();

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

        private static GeneratedPopulationPoolEntry[] Replace(
            IEnumerable<GeneratedPopulationPoolEntry> source,
            GeneratedPopulationContentKind kind,
            Func<GeneratedPopulationPoolEntry, GeneratedPopulationPoolEntry> replace) =>
            source.Select(value => value.Kind == kind ? replace(value) : value).ToArray();

        private static GeneratedPopulationPoolEntry Copy(
            GeneratedPopulationPoolEntry value,
            string contentKey = null,
            GeneratedContentPoolKey poolKey = null,
            IEnumerable<GeneratedContentSlotCategory> categories = null,
            IEnumerable<MoonpalaceBiomeId> allowedBiomes = null,
            string requiredResourceKey = null,
            GeneratedPopulationToolRequirement? requiredTool = null,
            int? minimumInteractionRadius = null,
            int? maximumInteractionRadius = null,
            int? minimumSafeRadius = null,
            int? minimumNeighborRadius = null) => new GeneratedPopulationPoolEntry(
                value.Kind, contentKey ?? value.ContentKey, poolKey ?? value.PoolKey,
                categories ?? value.CompatibleCategories,
                allowedBiomes ?? value.BiomeAllowlist,
                requiredResourceKey ?? value.RequiredResourceKey,
                requiredTool ?? value.RequiredTool,
                minimumInteractionRadius ?? value.MinimumInteractionRadius,
                maximumInteractionRadius ?? value.MaximumInteractionRadius,
                minimumSafeRadius ?? value.MinimumSafeRadius,
                minimumNeighborRadius ?? value.MinimumNeighborRadius,
                value.SymbolicPriceTierKey);

        private static void AssertAcceptedAndRejected(
            IEnumerable<GeneratedPopulationFilterEvidence> evidence,
            Func<GeneratedPopulationFilterEvidence, bool> predicate)
        {
            Assert.That(evidence.Any(predicate), Is.True);
            Assert.That(evidence.Any(value => !predicate(value)), Is.True);
        }

        private static string Counts(
            IEnumerable<GeneratedPopulationFilterEvidence> evidence,
            Func<GeneratedPopulationFilterEvidence, bool> predicate)
        {
            var values = evidence.ToArray();
            return values.Count(predicate).ToString(CultureInfo.InvariantCulture) + "/" +
                values.Count(value => !predicate(value)).ToString(CultureInfo.InvariantCulture);
        }

        private static void AssertAtomicFailure(
            GeneratedPopulationPlacementResult result,
            GeneratedPopulationPlacementFailureCode code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.Failures.Any(value => value.Code == code), Is.True,
                Describe(result));
            Assert.That(result.PartialEntryCount, Is.Zero);
            Assert.That(result.PartialMutationCount, Is.Zero);
            Assert.That(result.RetryLoopCount, Is.Zero);
        }

        private static string MutateDigest(string value) =>
            (value[0] == '0' ? "1" : "0") + value.Substring(1);
        private static string Describe(GeneratedPopulationPlacementResult result) =>
            result == null ? "NULL_RESULT" : string.Join("\n",
                result.Failures.Select(value => value.ToString()));
    }
}
