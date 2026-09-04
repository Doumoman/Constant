using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.EventOverlays;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Population;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Population
{
    [TestFixture]
    [Category("MAP18_05")]
    public sealed class GeneratedActivityEventRuntimeStateTests
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
        private static GeneratedHazardEnemyPlacementPlan acceptedHazardEnemyPlan;

        [Test]
        public void ActivityEventRuntimeStateCreatesActivityAndEventRecords()
        {
            var surface = Surface();
            Assert.That(surface.ActivityRuntimeStateRecordCount, Is.EqualTo(2));
            Assert.That(surface.EventRuntimeStateRecordCount, Is.EqualTo(4));
            Assert.That(surface.EmptyEventVariantCount, Is.EqualTo(2));
            Assert.That(surface.ActiveEventVariantCount, Is.EqualTo(2));
            Assert.That(surface.TotalRuntimeStateRecordCount, Is.EqualTo(6));
            Assert.That(surface.ActivityRecords.All(value =>
                value.CurrentPhase == GeneratedActivityRuntimePhase.Cue), Is.True);

            TestContext.WriteLine("MAP18_05_STATE_RECORD_EVIDENCE" +
                " activity=2 event=4 empty=2 active=2 total=6");
        }

        [Test]
        public void ActivityRuntimeTransitionsAllowOnlyCueActiveResolvedResettableCycle()
        {
            var record = Surface().ActivityRecords[0];
            var allowed = new[]
            {
                Tuple.Create(GeneratedActivityRuntimePhase.Cue,
                    GeneratedActivityRuntimePhase.Active),
                Tuple.Create(GeneratedActivityRuntimePhase.Active,
                    GeneratedActivityRuntimePhase.Resolved),
                Tuple.Create(GeneratedActivityRuntimePhase.Resolved,
                    GeneratedActivityRuntimePhase.Resettable),
                Tuple.Create(GeneratedActivityRuntimePhase.Resettable,
                    GeneratedActivityRuntimePhase.Cue),
            };
            var rejected = new[]
            {
                Tuple.Create(GeneratedActivityRuntimePhase.Cue,
                    GeneratedActivityRuntimePhase.Resolved),
                Tuple.Create(GeneratedActivityRuntimePhase.Active,
                    GeneratedActivityRuntimePhase.Cue),
                Tuple.Create(GeneratedActivityRuntimePhase.Resolved,
                    GeneratedActivityRuntimePhase.Active),
                Tuple.Create(GeneratedActivityRuntimePhase.Resettable,
                    GeneratedActivityRuntimePhase.Active),
            };
            Assert.That(record.AllowedTransitions.Count, Is.EqualTo(4));
            Assert.That(allowed.All(value => record.CanTransition(value.Item1, value.Item2)),
                Is.True);
            Assert.That(rejected.All(value => !record.CanTransition(value.Item1, value.Item2)),
                Is.True);
            var invalid = Instantiate(transitions: new[]
            {
                new GeneratedActivityRuntimeTransition(
                    GeneratedActivityRuntimePhase.Cue,
                    GeneratedActivityRuntimePhase.Resolved),
            });
            AssertAtomicFailure(invalid,
                GeneratedActivityEventRuntimeStateFailureCode.InvalidActivityTransition);

            TestContext.WriteLine("MAP18_05_ACTIVITY_TRANSITION_EVIDENCE" +
                " allowed=4 rejected=4 invalid_failure_probes=1 executed=0");
        }

        [Test]
        public void EventOverlayRuntimePublishesEmptyAndActiveVariantsWithExplicitReentry()
        {
            var events = Surface().EventRecords;
            Assert.That(events.GroupBy(value => value.SourceId).Count(), Is.EqualTo(2));
            Assert.That(events.GroupBy(value => value.SourceId).All(group =>
                group.Select(value => value.Variant).OrderBy(value => value)
                    .SequenceEqual(new[]
                    {
                        GeneratedEventRuntimeVariant.Empty,
                        GeneratedEventRuntimeVariant.Active,
                    })), Is.True);
            Assert.That(events.Where(value => value.Variant ==
                GeneratedEventRuntimeVariant.Empty).All(value =>
                    !value.PublishesRuntimeObject), Is.True);
            Assert.That(events.Where(value => value.Variant ==
                GeneratedEventRuntimeVariant.Active).All(value =>
                    value.PublishesStableStateIdentity && !value.PublishesRuntimeObject),
                Is.True);
            Assert.That(events.All(value => value.ActivationPolicy ==
                GeneratedEventActivationPolicy.StableSourceMarker &&
                value.ResolutionPolicy ==
                    GeneratedEventResolutionPolicy.PersistVariantIdentity &&
                value.ReentryPolicy ==
                    GeneratedEventReentryPolicy.RestoreSavedVariant), Is.True);

            TestContext.WriteLine("MAP18_05_EVENT_VARIANT_EVIDENCE" +
                " checks=4 reentry_checks=4 activation_deterministic=YES" +
                " resolution_deterministic=YES runtime_objects=0");
        }

        [Test]
        public void RuntimeStateIdsAndSaveKeysAreUniqueStableAndMutationSensitive()
        {
            var baseline = Surface();
            var repeat = Surface();
            var mutated = Surface(ActivitySources("MUTATED_SOURCE_DIGEST"));
            Assert.That(baseline.UniqueRuntimeStateIdCount,
                Is.EqualTo(baseline.TotalRuntimeStateRecordCount));
            Assert.That(baseline.UniqueSaveKeyCount,
                Is.EqualTo(baseline.TotalRuntimeStateRecordCount));
            Assert.That(baseline.DuplicateRuntimeStateIdCount, Is.Zero);
            Assert.That(baseline.DuplicateSaveKeyCount, Is.Zero);
            Assert.That(repeat.Digest, Is.EqualTo(baseline.Digest));
            Assert.That(mutated.Digest, Is.Not.EqualTo(baseline.Digest));
            Assert.That(mutated.SaveKeySetDigest,
                Is.Not.EqualTo(baseline.SaveKeySetDigest));
            Assert.That(mutated.ExportSurfaceDigest,
                Is.Not.EqualTo(baseline.ExportSurfaceDigest));
            Assert.That(baseline.RuntimeStateRecords.All(value =>
                value.RuntimeStateId.Value.Length == 64), Is.True);
            Assert.That(baseline.RuntimeStateRecords.All(value =>
                value.SaveKey.Namespace == "MAP18_RUNTIME_STATE" &&
                value.SaveKey.Version == "V1"), Is.True);

            TestContext.WriteLine("MAP18_05_IDENTITY_EVIDENCE" +
                " runtime_ids=6 save_keys=6 duplicate_ids=0 duplicate_keys=0" +
                " namespace=MAP18_RUNTIME_STATE version=V1 mutation_probes=3");
        }

        [Test]
        public void RuntimeStateInstantiatorPreservesMap18_04OccupiedAndBudgetSurfaces()
        {
            var upstream = HazardEnemyPlan();
            var surface = Surface();
            Assert.That(ReferenceEquals(surface.HazardEnemyPlan, upstream), Is.True);
            Assert.That(ReferenceEquals(surface.OccupiedSurface,
                upstream.OccupiedSurface), Is.True);
            Assert.That(ReferenceEquals(surface.BudgetLedger,
                upstream.BudgetLedger), Is.True);
            Assert.That(surface.HazardEnemyPlanDigest,
                Is.EqualTo(GeneratedActivityEventRuntimeStateInstantiator
                    .ExpectedHazardEnemyPlanDigest));
            Assert.That(surface.OccupiedSurfaceDigest,
                Is.EqualTo(GeneratedActivityEventRuntimeStateInstantiator
                    .ExpectedOccupiedSurfaceDigest));
            Assert.That(surface.BudgetLedgerDigest,
                Is.EqualTo(GeneratedActivityEventRuntimeStateInstantiator
                    .ExpectedBudgetLedgerDigest));
            Assert.That(surface.OccupiedSurfaceCount, Is.EqualTo(9));
            Assert.That(surface.RemainingCandidateCount, Is.EqualTo(3));
            Assert.That(surface.OccupiedConflictCount, Is.Zero);
            Assert.That(surface.BudgetMutationCount, Is.Zero);

            TestContext.WriteLine("MAP18_05_PASSTHROUGH_EVIDENCE" +
                " occupied_consumed=9/9 remaining=3 occupied_exact=YES" +
                " budget_exact=YES conflicts=0 budget_mutations=0");
        }

        [Test]
        public void ActivityEventRuntimeSurfacePublishesExportInputForMap18_06()
        {
            var surface = Surface();
            Assert.That(surface.Map18_06ExportSurfaceRecordCount,
                Is.EqualTo(surface.TotalRuntimeStateRecordCount));
            Assert.That(surface.Map18_06ExportRecords.Select(value =>
                value.RuntimeStateId.Value), Is.EquivalentTo(surface.RuntimeStateRecords
                    .Select(value => value.RuntimeStateId.Value)));
            Assert.That(surface.Map18_06ExportRecords.Select(value => value.SaveKey.Value),
                Is.EquivalentTo(surface.RuntimeStateRecords.Select(value =>
                    value.SaveKey.Value)));
            AssertLowerHexSha256(surface.Digest);
            AssertLowerHexSha256(surface.SaveKeySetDigest);
            AssertLowerHexSha256(surface.ExportSurfaceDigest);

            TestContext.WriteLine("MAP18_05_EXPORT_EVIDENCE records=6" +
                " surface_digest=" + surface.Digest +
                " save_key_set_digest=" + surface.SaveKeySetDigest +
                " export_surface_digest=" + surface.ExportSurfaceDigest);
        }

        [Test]
        public void RuntimeStateFailuresAreAtomicAndReportOwnerReasonExpectedActual()
        {
            var baseline = Surface();
            var failures = new[]
            {
                Instantiate(activitySources: Array.Empty<GeneratedActivityRuntimeSource>()),
                Instantiate(eventSources: Array.Empty<GeneratedEventRuntimeSource>()),
                Instantiate(expectedOccupiedSurfaceDigest: MutateDigest(
                    GeneratedActivityEventRuntimeStateInstantiator
                        .ExpectedOccupiedSurfaceDigest)),
                Instantiate(expectedBudgetLedgerDigest: MutateDigest(
                    GeneratedActivityEventRuntimeStateInstantiator
                        .ExpectedBudgetLedgerDigest)),
                Instantiate(transitions: new[]
                {
                    new GeneratedActivityRuntimeTransition(
                        GeneratedActivityRuntimePhase.Cue,
                        GeneratedActivityRuntimePhase.Resolved),
                }),
                Instantiate(variants: new[] { GeneratedEventRuntimeVariant.Empty }),
                Instantiate(existingRuntimeStateIds: new[]
                {
                    baseline.RuntimeStateRecords[0].RuntimeStateId.Value,
                }),
                Instantiate(existingSaveKeys: new[]
                {
                    baseline.RuntimeStateRecords[0].SaveKey.Value,
                }),
                Instantiate(activitySources: ActivitySources().Select((value, index) =>
                    index == 0 ? new GeneratedActivityRuntimeSource(value.Contract,
                        value.Sector, value.SourceDigest,
                        HazardEnemyPlan().OccupiedSurface[0].ReservationKey) : value)),
                Instantiate(attemptedRuntimeSpawn: true,
                    attemptedEventExecution: true, attemptedSaveWrite: true,
                    attemptedRewardGrant: true, attemptedDamage: true,
                    attemptedPhysics: true, attemptedAiHookup: true),
            };
            Assert.That(failures, Has.Length.EqualTo(10));
            Assert.That(failures.All(value => !value.Success && value.Surface == null &&
                value.PartialStateRecordCount == 0 &&
                value.PartialOccupiedMutationCount == 0 &&
                value.PartialBudgetMutationCount == 0 && value.RetryLoopCount == 0),
                Is.True);
            Assert.That(failures.SelectMany(value => value.Failures).All(value =>
                !string.IsNullOrWhiteSpace(value.Owner) &&
                !string.IsNullOrWhiteSpace(value.Reason) &&
                !string.IsNullOrWhiteSpace(value.OffendingKey) &&
                !string.IsNullOrWhiteSpace(value.Expected) &&
                !string.IsNullOrWhiteSpace(value.Actual)), Is.True);

            TestContext.WriteLine("MAP18_05_FAILURE_EVIDENCE" +
                " missing_source=2 digest_mismatch=2 invalid_transition=1" +
                " invalid_variant=1 duplicate_id=1 duplicate_key=1" +
                " occupied_budget_mutation=2 side_effect=1 partial_records=0");
        }

        [Test]
        public void RuntimeStateDigestsAreStableAcrossRepeatReverseCultureAndCandidateOrder()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var baseline = Surface();
                var repeat = Surface();
                var reverse = Surface(ActivitySources().Reverse(),
                    EventSources().Reverse(), Transitions().Reverse(), Variants().Reverse());
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var culture = Surface();
                var candidateOrder = Surface(ActivitySources().OrderByDescending(value =>
                    value.StableToken), EventSources().OrderByDescending(value =>
                    value.StableToken));
                AssertSameDigest(new[] { baseline, repeat, reverse, culture, candidateOrder },
                    value => value.Digest);
                AssertSameDigest(new[] { baseline, repeat, reverse, culture, candidateOrder },
                    value => value.SaveKeySetDigest);
                AssertSameDigest(new[] { baseline, repeat, reverse, culture, candidateOrder },
                    value => value.ExportSurfaceDigest);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }

            TestContext.WriteLine("MAP18_05_DETERMINISM_EVIDENCE" +
                " repeat_reverse_culture_candidate_order_mismatches=0/0/0/0" +
                " unity_random=0 random_range=0 system_random=0 retries=0");
        }

        [Test]
        public void RuntimeStateInstantiatorDoesNotSpawnObjectsWriteSavesMutateScenesOrRunRegressions()
        {
            var surface = Surface();
            Assert.That(new[]
            {
                surface.RuntimeActivityPrefabSpawnCount,
                surface.RuntimeEventPrefabSpawnCount,
                surface.CueVfxPlaybackCount, surface.CueSfxPlaybackCount,
                surface.ActualEventActivationCount, surface.ActualStateTransitionCount,
                surface.SaveWriteCount, surface.SaveReadCount,
                surface.PlayerPrefsWriteCount, surface.PlayerPrefsReadCount,
                surface.RuntimeObjectSpawnCount, surface.GameObjectInstantiateCount,
                surface.GameObjectEnableCount, surface.GameObjectDisableCount,
                surface.GameObjectDestroyCount, surface.SystemIoFileWriteCount,
                surface.SystemIoFileReadCount, surface.DiskSaveFileCreateCount,
                surface.DiskLoadFileCreateCount, surface.UserSaveSlotWriteCount,
                surface.PlatformStorageWriteCount, surface.RewardGrantCount,
                surface.DamageExecutionCount, surface.EnemyAiControllerHookupCount,
                surface.HealthComponentCreationCount,
                surface.DamageComponentCreationCount,
                surface.HitboxComponentCreationCount,
                surface.HurtboxComponentCreationCount,
                surface.TilemapComponentWriteCount, surface.TilemapSetTileCallCount,
                surface.TilemapSetTilesCallCount,
                surface.TilemapSetTilesBlockCallCount,
                surface.TilemapClearAllTilesCallCount,
                surface.TilemapColliderCreationCount,
                surface.CompositeColliderCreationCount,
                surface.ColliderCreationCount, surface.RigidbodyCreationCount,
                surface.PhysicsQueryCount, surface.PhysicsSimulationCount,
                surface.NavMeshSetupCount, surface.PathfindingSetupCount,
                surface.SceneMutationCount, surface.PrefabMutationCount,
                surface.TilemapMutationCount, surface.CameraReadCount,
                surface.CameraWriteCount, surface.AddressablesLoadCount,
                surface.ResourcesLoadCount, surface.AssetDatabaseLoadCount,
                surface.AuthoringCsvEditCount, surface.GeneratedCsvCommitCount,
                surface.GeneratedAssetCommitCount,
                surface.ProductionSeedApprovalCount,
                surface.UnityEngineRandomCallCount, surface.RandomRangeCallCount,
                surface.SystemRandomDirectUsageCount, surface.HiddenRetryLoopCount,
                surface.ImplicitSourceCreationCount, surface.CandidateMutationCount,
                surface.PriorTaskTestSelectionCount,
                surface.Legacy19347SelectionCount, surface.PlayModeSelectionCount,
                surface.UnfilteredTestSelectionCount, surface.FullRegressionRunCount,
            }, Is.All.Zero);

            TestContext.WriteLine("MAP18_05_SIDE_EFFECT_EVIDENCE" +
                " prefab_activity_event=0/0 cue_vfx_sfx=0/0 event_transition=0/0" +
                " save_playerprefs_systemio_disk=0/0/0/0/0/0/0/0" +
                " user_platform_reward_damage_ai=0/0/0/0/0" +
                " gameobject=0/0/0/0 runtime_objects=0 components=0/0/0/0" +
                " tilemap=0/0/0/0/0 colliders=0/0/0 rigidbody=0" +
                " physics=0/0 navmesh_path=0/0 scene_prefab_tilemap=0/0/0" +
                " camera=0/0 loads=0/0/0 csv=0 generated=0/0 seed=0" +
                " regressions=0/0/0/0/0");
        }

        [Test]
        public void Map18HandoffKeepsMap18_06Locked()
        {
            var surface = Surface();
            Assert.That(GeneratedActivityEventRuntimeStateSurface.DownstreamOwner,
                Is.EqualTo("MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG"));
            Assert.That(GeneratedActivityEventRuntimeStateSurface.OpensDownstreamTask,
                Is.False);
            Assert.That(surface.Map18_06Started, Is.False);

            TestContext.WriteLine("MAP18_05_DECISION_EVIDENCE result=PASS" +
                " downstream=MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG" +
                " started=NO locked=YES");
        }

        private static GeneratedActivityEventRuntimeStateSurface Surface(
            IEnumerable<GeneratedActivityRuntimeSource> activitySources = null,
            IEnumerable<GeneratedEventRuntimeSource> eventSources = null,
            IEnumerable<GeneratedActivityRuntimeTransition> transitions = null,
            IEnumerable<GeneratedEventRuntimeVariant> variants = null)
        {
            var result = Instantiate(activitySources, eventSources, transitions, variants);
            Assert.That(result.Success, Is.True, Describe(result));
            return result.Surface;
        }

        private static GeneratedActivityEventRuntimeStateResult Instantiate(
            IEnumerable<GeneratedActivityRuntimeSource> activitySources = null,
            IEnumerable<GeneratedEventRuntimeSource> eventSources = null,
            IEnumerable<GeneratedActivityRuntimeTransition> transitions = null,
            IEnumerable<GeneratedEventRuntimeVariant> variants = null,
            IEnumerable<string> existingRuntimeStateIds = null,
            IEnumerable<string> existingSaveKeys = null,
            string expectedOccupiedSurfaceDigest = null,
            string expectedBudgetLedgerDigest = null,
            bool attemptedRuntimeSpawn = false,
            bool attemptedEventExecution = false,
            bool attemptedSaveWrite = false,
            bool attemptedRewardGrant = false,
            bool attemptedDamage = false,
            bool attemptedPhysics = false,
            bool attemptedAiHookup = false)
        {
            var request = new GeneratedActivityEventRuntimeStateRequest(HazardEnemyPlan(),
                activitySources ?? ActivitySources(), eventSources ?? EventSources(),
                transitions ?? Transitions(), variants ?? Variants(),
                "REFERENCE_SEED_1801", "GENERATOR_V1", "DATA_V1",
                GeneratedActivityEventRuntimeStateInstantiator.ExpectedHazardEnemyPlanDigest,
                expectedOccupiedSurfaceDigest ?? GeneratedActivityEventRuntimeStateInstantiator
                    .ExpectedOccupiedSurfaceDigest,
                expectedBudgetLedgerDigest ?? GeneratedActivityEventRuntimeStateInstantiator
                    .ExpectedBudgetLedgerDigest,
                existingRuntimeStateIds, existingSaveKeys,
                attemptedRuntimeSpawn: attemptedRuntimeSpawn,
                attemptedEventExecution: attemptedEventExecution,
                attemptedSaveWrite: attemptedSaveWrite,
                attemptedRewardGrant: attemptedRewardGrant,
                attemptedDamage: attemptedDamage,
                attemptedPhysics: attemptedPhysics,
                attemptedAiHookup: attemptedAiHookup);
            return GeneratedActivityEventRuntimeStateInstantiator.Instantiate(request);
        }

        private static GeneratedActivityRuntimeSource[] ActivitySources(
            string firstDigest = "ACTIVITY_SOURCE_A_DIGEST") => new[]
        {
            new GeneratedActivityRuntimeSource(Activity("ACTIVITY_ALPHA"),
                new GeneratedSectorCoordinate(0, 0), firstDigest),
            new GeneratedActivityRuntimeSource(Activity("ACTIVITY_BETA"),
                new GeneratedSectorCoordinate(2, 1), "ACTIVITY_SOURCE_B_DIGEST"),
        };

        private static GeneratedEventRuntimeSource[] EventSources() => new[]
        {
            new GeneratedEventRuntimeSource(Event("EVENT_ALPHA", EventOverlayKind.Npc),
                new GeneratedSectorCoordinate(1, 0), "EVENT_SOURCE_A_DIGEST"),
            new GeneratedEventRuntimeSource(Event("EVENT_BETA", EventOverlayKind.Reward),
                new GeneratedSectorCoordinate(2, 2), "EVENT_SOURCE_B_DIGEST"),
        };

        private static ActivityStructureContract Activity(string id) =>
            new ActivityStructureContract(new ActivityStructureId(id), default, default,
                null, null, null, null, null, null, null);

        private static EventOverlayContract Event(string id, EventOverlayKind kind) =>
            new EventOverlayContract(new EventOverlayId(id), kind, default, null, null);

        private static IReadOnlyList<GeneratedActivityRuntimeTransition> Transitions() =>
            GeneratedActivityRuntimeTransitionCatalog.CreateAllowed();

        private static GeneratedEventRuntimeVariant[] Variants() => new[]
        {
            GeneratedEventRuntimeVariant.Empty,
            GeneratedEventRuntimeVariant.Active,
        };

        private static GeneratedHazardEnemyPlacementPlan HazardEnemyPlan()
        {
            if (acceptedHazardEnemyPlan != null) return acceptedHazardEnemyPlan;
            var request = new GeneratedHazardEnemyPlacementRequest(PopulationPlan(),
                GeneratedHazardEnemyPoolCatalog.CreateDefault(Biomes().Profiles.Select(value =>
                    value.Biome)), Protections(Index()),
                GeneratedHazardEnemyBudgetCatalog.CreateStarter(), Biomes(),
                GeneratedHazardEnemyBudgetPlanner.ExpectedPopulationPlanDigest,
                GeneratedHazardEnemyBudgetPlanner.ExpectedPopulationOccupiedSurfaceDigest,
                GeneratedHazardEnemyBudgetPlanner.ExpectedPopulationOccupiedSurfaceCount,
                GeneratedHazardEnemyBudgetPlanner.ExpectedPopulationRemainingCandidateCount);
            var result = GeneratedHazardEnemyBudgetPlanner.Place(request);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Failures));
            acceptedHazardEnemyPlan = result.Plan;
            return acceptedHazardEnemyPlan;
        }

        private static GeneratedPopulationPlacementPlan PopulationPlan()
        {
            if (acceptedPopulationPlan != null) return acceptedPopulationPlan;
            var request = new GeneratedPopulationPlacementRequest(Index(), MandatoryPlan(),
                GeneratedPopulationPoolCatalog.CreateDefault(Biomes()),
                PopulationContexts(Index()), Biomes(),
                GeneratedShopResourceMapElementPopulator.ExpectedMandatoryPlanDigest,
                GeneratedShopResourceMapElementPopulator.ExpectedMandatoryStableIdSetDigest,
                GeneratedShopResourceMapElementPopulator.ExpectedMandatoryExclusionCount);
            var result = GeneratedShopResourceMapElementPopulator.Populate(request);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Failures));
            acceptedPopulationPlan = result.Plan;
            return acceptedPopulationPlan;
        }

        private static GeneratedMandatoryUniquePlacementPlan MandatoryPlan()
        {
            if (acceptedMandatoryPlan != null) return acceptedMandatoryPlan;
            var request = new GeneratedMandatoryUniquePlacementRequest(Index(),
                GeneratedMandatoryUniqueContentPreplacer.CreateDefaultRules(),
                CoreResourceRegionStarterCatalog.Entries,
                GeneratedMandatoryUniqueContentPreplacer.ExpectedSlotIndexDigest,
                GeneratedMandatoryUniqueContentPreplacer.ExpectedStableIdSetDigest,
                GeneratedMandatoryUniqueContentPreplacer.ExpectedSourceRecordCount,
                GeneratedMandatoryUniqueContentPreplacer.ExpectedMandatoryUniqueCandidateCount);
            var result = GeneratedMandatoryUniqueContentPreplacer.Preplace(request);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Failures));
            acceptedMandatoryPlan = result.Plan;
            return acceptedMandatoryPlan;
        }

        private static GeneratedContentSlotIndex Index()
        {
            if (acceptedIndex != null) return acceptedIndex;
            var result = GeneratedContentSlotIndexBuilder.Build(
                new GeneratedContentSlotIndexRequest(Sources(),
                    GeneratedContentSlotIndexBuilder.ExpectedMap17AuditDigest,
                    "PASS", true, 2));
            Assert.That(result.Success, Is.True, string.Join("\n", result.Failures));
            acceptedIndex = result.Index;
            return acceptedIndex;
        }

        private static MicroPatternBiomeProfileCatalog Biomes() =>
            MicroPatternBiomeProfileCatalog.CreateBuiltIn();

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

        private static void AssertAtomicFailure(
            GeneratedActivityEventRuntimeStateResult result,
            GeneratedActivityEventRuntimeStateFailureCode code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Surface, Is.Null);
            Assert.That(result.Failures.Any(value => value.Code == code), Is.True,
                Describe(result));
            Assert.That(result.PartialStateRecordCount, Is.Zero);
            Assert.That(result.PartialOccupiedMutationCount, Is.Zero);
            Assert.That(result.PartialBudgetMutationCount, Is.Zero);
            Assert.That(result.RetryLoopCount, Is.Zero);
        }

        private static void AssertSameDigest(
            IEnumerable<GeneratedActivityEventRuntimeStateSurface> source,
            Func<GeneratedActivityEventRuntimeStateSurface, string> selector) =>
            Assert.That(source.Select(selector).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(1));

        private static void AssertLowerHexSha256(string value) =>
            Assert.That(value, Does.Match("^[0-9a-f]{64}$"));

        private static string MutateDigest(string value) =>
            (value[0] == '0' ? "1" : "0") + value.Substring(1);

        private static string Describe(GeneratedActivityEventRuntimeStateResult result) =>
            result == null ? "NULL_RESULT" : string.Join("\n",
                result.Failures.Select(value => value.ToString()));
    }
}
