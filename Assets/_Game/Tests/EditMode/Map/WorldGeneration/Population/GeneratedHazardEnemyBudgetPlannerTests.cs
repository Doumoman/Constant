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
    [Category("MAP18_04")]
    public sealed class GeneratedHazardEnemyBudgetPlannerTests
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
        private static GeneratedHazardEnemyPlacementPlan acceptedPlan;

        [Test]
        public void HazardEnemyPlanCreatesLogicalHazardAndEnemyGroups()
        {
            var plan = Plan();
            Assert.That(plan.PoolEntryCount, Is.EqualTo(2));
            Assert.That(plan.LogicalGroupCount, Is.EqualTo(2));
            Assert.That(plan.EntryCount, Is.EqualTo(2));
            Assert.That(plan.HazardEntryCount, Is.EqualTo(1));
            Assert.That(plan.EnemyEntryCount, Is.EqualTo(1));
            Assert.That(plan.UniqueContentKeyCount, Is.EqualTo(2));
            Assert.That(plan.Entry(GeneratedHazardEnemyContentKind.Hazard)
                .SelectedSlot.Address.SourceSlotId, Is.EqualTo("MAP16_SLOT_02"));
            Assert.That(plan.Entry(GeneratedHazardEnemyContentKind.Enemy)
                .SelectedSlot.Address.SourceSlotId, Is.EqualTo("MAP16_SLOT_10"));

            TestContext.WriteLine("MAP18_04_GROUP_EVIDENCE groups=2 entries=2" +
                " hazard=1 enemy=1 pools=2 unique_keys=2" +
                " selected=Hazard:MAP16_SLOT_02,Enemy:MAP16_SLOT_10");
        }

        [Test]
        public void HazardEnemyPlannerConsumesPopulationOccupiedSurfaceAndNeverReusesSlots()
        {
            var plan = Plan();
            var upstream = PopulationPlan().OccupiedSurface;
            Assert.That(upstream.Count, Is.EqualTo(7));
            Assert.That(plan.Entries.Select(value => value.ReservationKey)
                .Intersect(upstream.Select(value => value.ReservationKey),
                    StringComparer.Ordinal), Is.Empty);
            Assert.That(plan.UniqueReservationKeyCount, Is.EqualTo(2));
            Assert.That(plan.UniqueStableSpawnIdCount, Is.EqualTo(2));
            Assert.That(plan.OccupiedSurfaceCount, Is.EqualTo(9));
            Assert.That(plan.RemainingCandidateCount, Is.EqualTo(3));
            Assert.That(plan.OccupiedSurface.Count(value => value.Owner == "MAP18_04"),
                Is.EqualTo(2));
            foreach (var reservation in upstream)
                Assert.That(plan.IsOccupied(reservation.ReservationKey), Is.True);

            TestContext.WriteLine("MAP18_04_OCCUPIED_EXCLUSION_EVIDENCE" +
                " MAP18_03_consumed=7/7 selected=2 occupied_reuse=0" +
                " reservation_collisions=0 stable_id_collisions=0" +
                " MAP18_05_occupied=9 remaining=3");
        }

        [Test]
        public void MandatoryRouteRewardRecoveryAndSafeFloorAreProtected()
        {
            var plan = Plan();
            AssertAcceptedAndRejected(plan.ProtectionProofs, value => value.RouteAccepted);
            AssertAcceptedAndRejected(plan.ProtectionProofs, value => value.RewardAccepted);
            AssertAcceptedAndRejected(plan.ProtectionProofs, value => value.RecoveryAccepted);
            AssertAcceptedAndRejected(plan.ProtectionProofs, value =>
                value.SafeRadiusAccepted);
            AssertAcceptedAndRejected(plan.ProtectionProofs, value =>
                value.NeighborRadiusAccepted);
            AssertAcceptedAndRejected(plan.ProtectionProofs, value =>
                value.OccupiedSurfaceAccepted);
            Assert.That(plan.Entries.All(value => value.ProtectionProof.Accepted), Is.True);
            Assert.That(plan.CriticalRouteViolationCount, Is.Zero);
            Assert.That(plan.CriticalRewardViolationCount, Is.Zero);
            Assert.That(plan.CriticalRecoveryViolationCount, Is.Zero);

            TestContext.WriteLine("MAP18_04_PROTECTION_EVIDENCE evaluations=" +
                plan.ProtectionProofs.Count + " route=" + Counts(plan.ProtectionProofs,
                    value => value.RouteAccepted) + " reward=" +
                Counts(plan.ProtectionProofs, value => value.RewardAccepted) +
                " recovery=" + Counts(plan.ProtectionProofs,
                    value => value.RecoveryAccepted) + " safe=" +
                Counts(plan.ProtectionProofs, value => value.SafeRadiusAccepted) +
                " neighbor=" + Counts(plan.ProtectionProofs,
                    value => value.NeighborRadiusAccepted) + " occupied=" +
                Counts(plan.ProtectionProofs, value => value.OccupiedSurfaceAccepted) +
                " selected_route_reward_recovery_violations=0/0/0");
        }

        [Test]
        public void HierarchicalBudgetsSpendTopDownAndRejectNegativeOrDuplicateSpends()
        {
            var plan = Plan();
            var ledger = plan.BudgetLedger;
            Assert.That(ledger.ScopeCount, Is.EqualTo(5));
            Assert.That(ledger.SpendEntryCount, Is.EqualTo(plan.EntryCount));
            Assert.That(ledger.ScopeSpendRecordCount, Is.EqualTo(10));
            Assert.That(ledger.DuplicateSpendKeyCount, Is.Zero);
            Assert.That(ledger.NegativeRemainingCount, Is.Zero);
            Assert.That(ledger.Spends.All(value => value.ScopeSpends.Select(spend => spend.Scope)
                .SequenceEqual(Enum.GetValues(typeof(GeneratedHazardEnemyBudgetScope))
                    .Cast<GeneratedHazardEnemyBudgetScope>())), Is.True);
            AssertBalance(ledger, GeneratedHazardEnemyBudgetScope.World, 12, 3, 9);
            AssertBalance(ledger, GeneratedHazardEnemyBudgetScope.Patch, 10, 3, 7);
            AssertBalance(ledger, GeneratedHazardEnemyBudgetScope.Sector, 8, 3, 5);
            AssertBalance(ledger, GeneratedHazardEnemyBudgetScope.Cluster, 6, 3, 3);
            AssertBalance(ledger, GeneratedHazardEnemyBudgetScope.Slot, 4, 3, 1);

            var overflow = Place(PopulationPlan(), Pools(), Protections(Index()),
                Enum.GetValues(typeof(GeneratedHazardEnemyBudgetScope))
                    .Cast<GeneratedHazardEnemyBudgetScope>()
                    .Select(value => new GeneratedHazardEnemyBudgetLimit(value, 1)));
            AssertAtomicFailure(overflow,
                GeneratedHazardEnemyPlacementFailureCode.BudgetOverflow);
            Assert.That(overflow.BudgetRollbackCount, Is.EqualTo(1));

            var duplicateKey = plan.Entries[0].BudgetSpend.ScopeSpends[0].SpendKey;
            var duplicate = Place(PopulationPlan(), Pools(), Protections(Index()), Budgets(),
                existingBudgetSpendKeys: new[] { duplicateKey });
            AssertAtomicFailure(duplicate,
                GeneratedHazardEnemyPlacementFailureCode.DuplicateBudgetSpend);
            Assert.That(duplicate.BudgetRollbackCount, Is.EqualTo(1));

            TestContext.WriteLine("MAP18_04_BUDGET_EVIDENCE scopes=5/5" +
                " spend_entries=2 scope_spends=10 duplicate_keys=0 negative=0" +
                " world=12/3/9 patch=10/3/7 sector=8/3/5" +
                " cluster=6/3/3 slot=4/3/1 overflow_probes=1/1" +
                " duplicate_probes=1/1 rollback=2/2 partial_spends=0");
        }

        [Test]
        public void HazardEnemySelectionUsesStableOrderAndDeterministicTicketsWithoutRandom()
        {
            var plan = Plan();
            Assert.That(plan.DeterministicTicketSelectionCount, Is.EqualTo(2));
            Assert.That(plan.Entries.All(value =>
                BakingCanonicalDigest.IsLowerHexSha256(
                    value.ProtectionProof.DeterministicTicket)), Is.True);
            Assert.That(plan.UnityEngineRandomCallCount, Is.Zero);
            Assert.That(plan.RandomRangeCallCount, Is.Zero);
            Assert.That(plan.SystemRandomDirectUsageCount, Is.Zero);
            Assert.That(plan.HiddenRetryLoopCount, Is.Zero);
            Assert.That(plan.ImplicitCandidateCreationCount, Is.Zero);
            Assert.That(plan.CandidateMutationCount, Is.Zero);

            TestContext.WriteLine("MAP18_04_SELECTION_EVIDENCE stable_order=YES" +
                " deterministic_hash_tickets=2 UnityEngine.Random=0 Random.Range=0" +
                " System.Random=0 retries=0 implicit_candidates=0 candidate_mutations=0");
        }

        [Test]
        public void HazardEnemyPlanPublishesOccupiedAndBudgetSurfaceForMap18_05()
        {
            var plan = Plan();
            Assert.That(plan.OccupiedSurfaceCount, Is.EqualTo(9));
            Assert.That(plan.RemainingCandidateCount, Is.EqualTo(3));
            Assert.That(plan.BudgetLedger.ScopeCount, Is.EqualTo(5));
            Assert.That(plan.BudgetLedger.SpendEntryCount, Is.EqualTo(2));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(plan.Digest), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                plan.OccupiedSurfaceDigest), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                plan.BudgetLedger.Digest), Is.True);

            TestContext.WriteLine("MAP18_04_HANDOFF_SURFACE_EVIDENCE" +
                " occupied=9 remaining=3 budget_scopes=5 budget_spends=2" +
                " plan=" + plan.Digest + " occupied_digest=" +
                plan.OccupiedSurfaceDigest + " budget_digest=" +
                plan.BudgetLedger.Digest);
        }

        [Test]
        public void HazardEnemyFailuresAreAtomicAndReportOwnerReasonExpectedActual()
        {
            AssertAtomicFailure(Place(null, Pools(), Protections(Index()), Budgets()),
                GeneratedHazardEnemyPlacementFailureCode.MissingPopulationPlan);
            AssertAtomicFailure(Place(PopulationPlan(), Pools(), Protections(Index()), Budgets(),
                    expectedPopulationPlanDigest: MutateDigest(
                        GeneratedHazardEnemyBudgetPlanner.ExpectedPopulationPlanDigest)),
                GeneratedHazardEnemyPlacementFailureCode.PopulationPlanDigestMismatch);
            AssertAtomicFailure(Place(PopulationPlan(), Pools(), Protections(Index()), Budgets(),
                    expectedOccupiedSurfaceDigest: MutateDigest(
                        GeneratedHazardEnemyBudgetPlanner
                            .ExpectedPopulationOccupiedSurfaceDigest)),
                GeneratedHazardEnemyPlacementFailureCode.OccupiedSurfaceDigestMismatch);
            var accepted = Plan();
            AssertAtomicFailure(Place(PopulationPlan(), Pools(), Protections(Index()), Budgets(),
                    expectedPlanDigest: MutateDigest(accepted.Digest)),
                GeneratedHazardEnemyPlacementFailureCode.PlanDigestMismatch);
            AssertAtomicFailure(Place(PopulationPlan(), Pools(), Protections(Index()), Budgets(),
                    expectedFinalOccupiedSurfaceDigest:
                        MutateDigest(accepted.OccupiedSurfaceDigest)),
                GeneratedHazardEnemyPlacementFailureCode.FinalOccupiedSurfaceDigestMismatch);
            AssertAtomicFailure(Place(PopulationPlan(), Pools(), Protections(Index()), Budgets(),
                    expectedBudgetLedgerDigest:
                        MutateDigest(accepted.BudgetLedger.Digest)),
                GeneratedHazardEnemyPlacementFailureCode.BudgetLedgerDigestMismatch);

            var routeBlocked = ReplaceProtection(Protections(Index()),
                value => IsCombatCandidate(value), value => CopyProtection(value,
                    intersectsMandatoryRouteSpine: true));
            var routeFailure = Place(PopulationPlan(), Pools(), routeBlocked, Budgets());
            AssertAtomicFailure(routeFailure,
                GeneratedHazardEnemyPlacementFailureCode.OccupiedSlotReuse);
            AssertAtomicFailure(routeFailure,
                GeneratedHazardEnemyPlacementFailureCode.RouteProtectionViolation);
            var rewardBlocked = ReplaceProtection(Protections(Index()),
                value => IsCombatCandidate(value), value => CopyProtection(value,
                    intersectsRewardApproachFloor: true));
            AssertAtomicFailure(Place(PopulationPlan(), Pools(), rewardBlocked, Budgets()),
                GeneratedHazardEnemyPlacementFailureCode.RewardApproachProtectionViolation);
            var recoveryBlocked = ReplaceProtection(Protections(Index()),
                value => IsCombatCandidate(value), value => CopyProtection(value,
                    intersectsDropRecoveryFloor: true));
            AssertAtomicFailure(Place(PopulationPlan(), Pools(), recoveryBlocked, Budgets()),
                GeneratedHazardEnemyPlacementFailureCode.RecoveryFloorProtectionViolation);

            var missingHazard = Pools().Where(value => value.Kind !=
                GeneratedHazardEnemyContentKind.Hazard);
            AssertAtomicFailure(Place(PopulationPlan(), missingHazard,
                    Protections(Index()), Budgets()),
                GeneratedHazardEnemyPlacementFailureCode.MissingHazardPool);
            var invalidPool = ReplacePool(Pools(), GeneratedHazardEnemyContentKind.Hazard,
                value => CopyPool(value,
                    poolKey: new GeneratedContentPoolKey(string.Empty, "V1")));
            AssertAtomicFailure(Place(PopulationPlan(), invalidPool,
                    Protections(Index()), Budgets()),
                GeneratedHazardEnemyPlacementFailureCode.InvalidPoolKey);

            var first = Plan().Entries[0];
            AssertAtomicFailure(Place(PopulationPlan(), Pools(), Protections(Index()), Budgets(),
                    existingReservationKeys: new[] { first.ReservationKey }),
                GeneratedHazardEnemyPlacementFailureCode.ReservationKeyCollision);
            AssertAtomicFailure(Place(PopulationPlan(), Pools(), Protections(Index()), Budgets(),
                    existingStableSpawnIds: new[] { first.StableSpawnId.Value }),
                GeneratedHazardEnemyPlacementFailureCode.StableSpawnIdCollision);
            AssertAtomicFailure(Place(PopulationPlan(), Pools(), Protections(Index()), Budgets(),
                    attemptedRuntimeSpawn: true, attemptedDamage: true,
                    attemptedPhysics: true, attemptedEnemyAi: true, attemptedCombat: true),
                GeneratedHazardEnemyPlacementFailureCode
                    .AttemptedRuntimeSpawnDamagePhysicsAiOrCombat);

            var failures = Place(PopulationPlan(), Pools(), routeBlocked, Budgets()).Failures;
            Assert.That(failures.All(value => value.Owner.Length > 0 &&
                value.Reason.Length > 0 && value.Expected.Length > 0 &&
                value.Actual.Length > 0), Is.True);

            TestContext.WriteLine("MAP18_04_FAILURE_EVIDENCE missing_plan=1/1" +
                " digest_mismatches=5/5 occupied_reuse=1/1" +
                " route_reward_recovery=1/1/1 missing_pool=1/1 invalid_pool=1/1" +
                " reservation_collision=1/1 stable_id_collision=1/1" +
                " runtime_spawn_damage_physics_ai_combat=1/1" +
                " atomic_partial_entries_budget_spends_mutations=0/0/0");
        }

        [Test]
        public void HazardEnemyDigestsAreStableAcrossRepeatReverseCultureAndCandidateOrder()
        {
            var baseline = Plan();
            var repeat = Place(PopulationPlan(), Pools(), Protections(Index()), Budgets()).Plan;
            var reverse = Place(PopulationPlan(), Pools().Reverse(),
                Protections(Index()).Reverse(), Budgets().Reverse()).Plan;
            var reorderedIndex = BuildIndex(Sources().Reverse());
            var reorderedMandatory = BuildMandatoryPlan(reorderedIndex);
            var reorderedPopulation = BuildPopulationPlan(reorderedIndex, reorderedMandatory);
            var candidateOrder = Place(reorderedPopulation, Pools(),
                Protections(reorderedIndex).Skip(5).Concat(Protections(reorderedIndex).Take(5)),
                Budgets()).Plan;
            GeneratedHazardEnemyPlacementPlan culture;
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                culture = Place(PopulationPlan(), Pools(), Protections(Index()), Budgets()).Plan;
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }

            AssertSameDigest(new[] { baseline, repeat, reverse, candidateOrder, culture },
                value => value.Digest);
            AssertSameDigest(new[] { baseline, repeat, reverse, candidateOrder, culture },
                value => value.OccupiedSurfaceDigest);
            AssertSameDigest(new[] { baseline, repeat, reverse, candidateOrder, culture },
                value => value.BudgetLedger.Digest);

            var mutatedPools = ReplacePool(Pools(), GeneratedHazardEnemyContentKind.Hazard,
                value => CopyPool(value, contentKey: value.ContentKey + "_MUTATED"));
            var mutated = Place(PopulationPlan(), mutatedPools,
                Protections(Index()), Budgets()).Plan;
            Assert.That(mutated.Digest, Is.Not.EqualTo(baseline.Digest));
            Assert.That(mutated.OccupiedSurfaceDigest,
                Is.Not.EqualTo(baseline.OccupiedSurfaceDigest));
            Assert.That(mutated.BudgetLedger.Digest,
                Is.Not.EqualTo(baseline.BudgetLedger.Digest));

            TestContext.WriteLine("MAP18_04_DIGEST_EVIDENCE plan=" + baseline.Digest +
                " occupied=" + baseline.OccupiedSurfaceDigest + " budget=" +
                baseline.BudgetLedger.Digest +
                " repeat_reverse_culture_candidate_order_mismatches=0/0/0/0" +
                " mutation_sensitivity=3/3");
        }

        [Test]
        public void HazardEnemyPlannerDoesNotSpawnObjectsMutatePhysicsScenesOrRunRegressions()
        {
            var plan = Plan();
            Assert.That(new[]
            {
                plan.RuntimeHazardPlacementCount, plan.RuntimeEnemyPlacementCount,
                plan.ActualDamageExecutionCount, plan.ActualCombatEncounterCount,
                plan.EnemyAiControllerHookupCount, plan.HealthComponentCreationCount,
                plan.DamageComponentCreationCount, plan.HitboxComponentCreationCount,
                plan.HurtboxComponentCreationCount, plan.RuntimeObjectSpawnCount,
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
                plan.PhysicsSimulationCount, plan.NavMeshSetupCount,
                plan.PathfindingSetupCount, plan.SceneMutationCount,
                plan.PrefabMutationCount, plan.TilemapMutationCount,
                plan.CameraReadCount, plan.CameraWriteCount,
                plan.AddressablesLoadCount, plan.ResourcesLoadCount,
                plan.AssetDatabaseLoadCount, plan.AuthoringCsvEditCount,
                plan.GeneratedCsvCommitCount, plan.GeneratedAssetCommitCount,
                plan.ProductionSeedApprovalCount, plan.PriorTaskTestSelectionCount,
                plan.Legacy19347SelectionCount, plan.PlayModeSelectionCount,
                plan.UnfilteredTestSelectionCount, plan.FullRegressionRunCount,
            }, Is.All.Zero);

            TestContext.WriteLine("MAP18_04_SIDE_EFFECT_EVIDENCE" +
                " runtime_hazard_enemy=0/0 damage_combat_ai=0/0/0" +
                " components=0/0/0/0 spawn=0 gameobject=0/0/0/0 system_io=0/0" +
                " disk=0/0 user_platform=0/0 tilemap=0/0/0/0/0" +
                " colliders=0/0/0 rigidbody=0 physics=0/0 navmesh_path=0/0" +
                " scene_prefab_tilemap=0/0/0 camera=0/0 loads=0/0/0" +
                " csv=0 generated=0/0 seeds=0 regressions=0/0/0/0/0");
        }

        [Test]
        public void Map18HandoffKeepsMap18_05Locked()
        {
            var plan = Plan();
            Assert.That(GeneratedHazardEnemyPlacementPlan.DownstreamOwner,
                Is.EqualTo("MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES"));
            Assert.That(GeneratedHazardEnemyPlacementPlan.OpensDownstreamTask, Is.False);
            Assert.That(plan.Map18_05Started, Is.False);

            TestContext.WriteLine("MAP18_04_DECISION_EVIDENCE result=PASS" +
                " downstream=MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES" +
                " started=NO locked=YES");
        }

        private static GeneratedHazardEnemyPlacementPlan Plan()
        {
            if (acceptedPlan != null) return acceptedPlan;
            var result = Place(PopulationPlan(), Pools(), Protections(Index()), Budgets());
            Assert.That(result.Success, Is.True, Describe(result));
            acceptedPlan = result.Plan;
            return acceptedPlan;
        }

        private static GeneratedContentSlotIndex Index()
        {
            if (acceptedIndex != null) return acceptedIndex;
            acceptedIndex = BuildIndex(Sources());
            return acceptedIndex;
        }

        private static GeneratedMandatoryUniquePlacementPlan MandatoryPlan()
        {
            if (acceptedMandatoryPlan != null) return acceptedMandatoryPlan;
            acceptedMandatoryPlan = BuildMandatoryPlan(Index());
            return acceptedMandatoryPlan;
        }

        private static GeneratedPopulationPlacementPlan PopulationPlan()
        {
            if (acceptedPopulationPlan != null) return acceptedPopulationPlan;
            acceptedPopulationPlan = BuildPopulationPlan(Index(), MandatoryPlan());
            Assert.That(acceptedPopulationPlan.Digest,
                Is.EqualTo(GeneratedHazardEnemyBudgetPlanner.ExpectedPopulationPlanDigest));
            Assert.That(acceptedPopulationPlan.OccupiedSurfaceDigest, Is.EqualTo(
                GeneratedHazardEnemyBudgetPlanner.ExpectedPopulationOccupiedSurfaceDigest));
            return acceptedPopulationPlan;
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

        private static GeneratedPopulationPlacementPlan BuildPopulationPlan(
            GeneratedContentSlotIndex index,
            GeneratedMandatoryUniquePlacementPlan mandatoryPlan)
        {
            var request = new GeneratedPopulationPlacementRequest(index, mandatoryPlan,
                GeneratedPopulationPoolCatalog.CreateDefault(Biomes()),
                PopulationContexts(index), Biomes(),
                GeneratedShopResourceMapElementPopulator.ExpectedMandatoryPlanDigest,
                GeneratedShopResourceMapElementPopulator.ExpectedMandatoryStableIdSetDigest,
                GeneratedShopResourceMapElementPopulator.ExpectedMandatoryExclusionCount);
            var result = GeneratedShopResourceMapElementPopulator.Populate(request);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Failures));
            return result.Plan;
        }

        private static GeneratedHazardEnemyPlacementResult Place(
            GeneratedPopulationPlacementPlan populationPlan,
            IEnumerable<GeneratedHazardEnemyPoolEntry> pools,
            IEnumerable<GeneratedHazardEnemyCandidateProtection> protections,
            IEnumerable<GeneratedHazardEnemyBudgetLimit> budgets,
            IEnumerable<string> existingReservationKeys = null,
            IEnumerable<string> existingStableSpawnIds = null,
            IEnumerable<string> existingBudgetSpendKeys = null,
            string expectedPopulationPlanDigest = null,
            string expectedOccupiedSurfaceDigest = null,
            string expectedPlanDigest = null,
            string expectedFinalOccupiedSurfaceDigest = null,
            string expectedBudgetLedgerDigest = null,
            bool attemptedRuntimeSpawn = false,
            bool attemptedDamage = false,
            bool attemptedPhysics = false,
            bool attemptedEnemyAi = false,
            bool attemptedCombat = false)
        {
            var request = new GeneratedHazardEnemyPlacementRequest(populationPlan,
                pools, protections, budgets, Biomes(), expectedPopulationPlanDigest ??
                    GeneratedHazardEnemyBudgetPlanner.ExpectedPopulationPlanDigest,
                expectedOccupiedSurfaceDigest ?? GeneratedHazardEnemyBudgetPlanner
                    .ExpectedPopulationOccupiedSurfaceDigest,
                GeneratedHazardEnemyBudgetPlanner.ExpectedPopulationOccupiedSurfaceCount,
                GeneratedHazardEnemyBudgetPlanner.ExpectedPopulationRemainingCandidateCount,
                existingReservationKeys, existingStableSpawnIds, existingBudgetSpendKeys,
                expectedPlanDigest, expectedFinalOccupiedSurfaceDigest,
                expectedBudgetLedgerDigest, attemptedRuntimeSpawn, attemptedDamage,
                attemptedPhysics, attemptedEnemyAi, attemptedCombat);
            return GeneratedHazardEnemyBudgetPlanner.Place(request);
        }

        private static MicroPatternBiomeProfileCatalog Biomes() =>
            MicroPatternBiomeProfileCatalog.CreateBuiltIn();
        private static IReadOnlyList<GeneratedHazardEnemyPoolEntry> Pools() =>
            GeneratedHazardEnemyPoolCatalog.CreateDefault(Biomes().Profiles
                .Select(value => value.Biome));
        private static IReadOnlyList<GeneratedHazardEnemyBudgetLimit> Budgets() =>
            GeneratedHazardEnemyBudgetCatalog.CreateStarter();

        private static GeneratedHazardEnemyCandidateProtection[] Protections(
            GeneratedContentSlotIndex index) => index.Entries.Select(slot =>
        {
            var ordinal = Ordinal(slot);
            return new GeneratedHazardEnemyCandidateProtection(slot, Biome(ordinal),
                3, ordinal == 9 ? 1 : 4, ordinal == 9 ? 1 : 5,
                intersectsMandatoryRouteSpine: ordinal == 0 || ordinal == 3,
                intersectsTraversalEnvelope: ordinal == 1,
                intersectsRequiredLanding: ordinal == 4,
                intersectsDropRecoveryFloor: ordinal == 6 || ordinal == 9,
                intersectsRewardApproachFloor: ordinal == 5 || ordinal == 9,
                intersectsSpecialVillageEntryBuffer: ordinal == 7,
                intersectsSafePocket: ordinal == 8,
                intersectsCriticalSocketBoundary: ordinal == 11);
        }).ToArray();

        private static GeneratedPopulationCandidateContext[] PopulationContexts(
            GeneratedContentSlotIndex index) => index.Entries.Select(slot =>
        {
            var ordinal = Ordinal(slot);
            var resource = slot.Address.Category == GeneratedContentSlotCategory.Resource ||
                slot.Address.Category == GeneratedContentSlotCategory.Pickup;
            return new GeneratedPopulationCandidateContext(slot, Biome(ordinal),
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

        private static MoonpalaceBiomeId Biome(int ordinal)
        {
            var biomes = new[]
            {
                MoonpalaceBiomeId.MoonCrater,
                MoonpalaceBiomeId.CassiaRoot,
                MoonpalaceBiomeId.AbandonedMill,
                MoonpalaceBiomeId.MoonDough,
            };
            return biomes[ordinal % biomes.Length];
        }

        private static int Ordinal(GeneratedContentSlotIndexEntry slot) => int.Parse(
            slot.Address.SourceSlotId.Substring(slot.Address.SourceSlotId.Length - 2),
            CultureInfo.InvariantCulture);
        private static bool IsCombatCandidate(
            GeneratedHazardEnemyCandidateProtection value) => value.Slot.Address.Category ==
                GeneratedContentSlotCategory.Hazard || value.Slot.Address.Category ==
                GeneratedContentSlotCategory.Enemy;

        private static GeneratedHazardEnemyPoolEntry[] ReplacePool(
            IEnumerable<GeneratedHazardEnemyPoolEntry> source,
            GeneratedHazardEnemyContentKind kind,
            Func<GeneratedHazardEnemyPoolEntry, GeneratedHazardEnemyPoolEntry> replace) =>
            source.Select(value => value.Kind == kind ? replace(value) : value).ToArray();

        private static GeneratedHazardEnemyPoolEntry CopyPool(
            GeneratedHazardEnemyPoolEntry value,
            string contentKey = null,
            GeneratedContentPoolKey poolKey = null,
            IEnumerable<GeneratedContentSlotCategory> categories = null,
            IEnumerable<MoonpalaceBiomeId> biomes = null,
            int? routeClearance = null,
            int? safeRadius = null,
            int? neighborRadius = null,
            int? pressureCost = null,
            int? maximumWorldCount = null) => new GeneratedHazardEnemyPoolEntry(
                value.Kind, contentKey ?? value.ContentKey, poolKey ?? value.PoolKey,
                categories ?? value.CompatibleCategories, biomes ?? value.BiomeAllowlist,
                routeClearance ?? value.RequiredRouteClearance,
                safeRadius ?? value.MinimumSafeRadius,
                neighborRadius ?? value.MinimumNeighborRadius,
                pressureCost ?? value.PressureCost,
                maximumWorldCount ?? value.MaximumWorldCount);

        private static GeneratedHazardEnemyCandidateProtection[] ReplaceProtection(
            IEnumerable<GeneratedHazardEnemyCandidateProtection> source,
            Func<GeneratedHazardEnemyCandidateProtection, bool> predicate,
            Func<GeneratedHazardEnemyCandidateProtection,
                GeneratedHazardEnemyCandidateProtection> replace) => source
                .Select(value => predicate(value) ? replace(value) : value).ToArray();

        private static GeneratedHazardEnemyCandidateProtection CopyProtection(
            GeneratedHazardEnemyCandidateProtection value,
            int? routeClearance = null,
            int? safeRadius = null,
            int? neighborRadius = null,
            bool? intersectsMandatoryRouteSpine = null,
            bool? intersectsTraversalEnvelope = null,
            bool? intersectsRequiredLanding = null,
            bool? intersectsDropRecoveryFloor = null,
            bool? intersectsRewardApproachFloor = null,
            bool? intersectsSpecialVillageEntryBuffer = null,
            bool? intersectsSafePocket = null,
            bool? intersectsCriticalSocketBoundary = null) =>
            new GeneratedHazardEnemyCandidateProtection(value.Slot, value.Biome,
                routeClearance ?? value.RouteClearance,
                safeRadius ?? value.SafeRadius,
                neighborRadius ?? value.NeighborRadius,
                intersectsMandatoryRouteSpine ?? value.IntersectsMandatoryRouteSpine,
                intersectsTraversalEnvelope ?? value.IntersectsTraversalEnvelope,
                intersectsRequiredLanding ?? value.IntersectsRequiredLanding,
                intersectsDropRecoveryFloor ?? value.IntersectsDropRecoveryFloor,
                intersectsRewardApproachFloor ?? value.IntersectsRewardApproachFloor,
                intersectsSpecialVillageEntryBuffer ??
                    value.IntersectsSpecialVillageEntryBuffer,
                intersectsSafePocket ?? value.IntersectsSafePocket,
                intersectsCriticalSocketBoundary ?? value.IntersectsCriticalSocketBoundary);

        private static void AssertAcceptedAndRejected(
            IEnumerable<GeneratedHazardEnemyProtectionProof> source,
            Func<GeneratedHazardEnemyProtectionProof, bool> predicate)
        {
            Assert.That(source.Any(predicate), Is.True);
            Assert.That(source.Any(value => !predicate(value)), Is.True);
        }

        private static string Counts(
            IEnumerable<GeneratedHazardEnemyProtectionProof> source,
            Func<GeneratedHazardEnemyProtectionProof, bool> predicate)
        {
            var values = source.ToArray();
            return values.Count(predicate).ToString(CultureInfo.InvariantCulture) + "/" +
                values.Count(value => !predicate(value)).ToString(
                    CultureInfo.InvariantCulture);
        }

        private static void AssertBalance(
            GeneratedHazardEnemyBudgetLedger ledger,
            GeneratedHazardEnemyBudgetScope scope,
            int initial,
            int spent,
            int remaining)
        {
            var balance = ledger.Balance(scope);
            Assert.That(balance.Initial, Is.EqualTo(initial));
            Assert.That(balance.Spent, Is.EqualTo(spent));
            Assert.That(balance.Remaining, Is.EqualTo(remaining));
        }

        private static void AssertAtomicFailure(
            GeneratedHazardEnemyPlacementResult result,
            GeneratedHazardEnemyPlacementFailureCode code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.Failures.Any(value => value.Code == code), Is.True,
                Describe(result));
            Assert.That(result.PartialEntryCount, Is.Zero);
            Assert.That(result.PartialBudgetSpendCount, Is.Zero);
            Assert.That(result.PartialMutationCount, Is.Zero);
            Assert.That(result.RetryLoopCount, Is.Zero);
        }

        private static void AssertSameDigest(
            IEnumerable<GeneratedHazardEnemyPlacementPlan> source,
            Func<GeneratedHazardEnemyPlacementPlan, string> selector) =>
            Assert.That(source.Select(selector).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(1));
        private static string MutateDigest(string value) =>
            (value[0] == '0' ? "1" : "0") + value.Substring(1);
        private static string Describe(GeneratedHazardEnemyPlacementResult result) =>
            result == null ? "NULL_RESULT" : string.Join("\n",
                result.Failures.Select(value => value.ToString()));
    }
}
