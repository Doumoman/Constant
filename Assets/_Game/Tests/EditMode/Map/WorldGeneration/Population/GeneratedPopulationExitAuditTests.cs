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
    [Category("MAP18_07")]
    public sealed class GeneratedPopulationExitAuditTests
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
        private static GeneratedActivityEventRuntimeStateSurface acceptedRuntimeSurface;

        [Test]
        public void PopulationExitAuditValidatesMap18HandoffDigestChain()
        {
            var audit = Audit();
            Assert.That(audit.Success, Is.True, Describe(audit));
            Assert.That(audit.Status, Is.EqualTo(GeneratedPopulationExitAuditStatus.Pass));
            Assert.That(audit.Surface.AuditedMap18TaskSurfaceCount, Is.EqualTo(6));
            Assert.That(audit.Surface.AuditedUpstreamDigestCount, Is.EqualTo(16));
            Assert.That(audit.Surface.HandoffDigests, Has.Count.EqualTo(16));
            Assert.That(audit.Surface.HandoffDigests.All(value => value.Matches), Is.True);
            Assert.That(audit.Surface.HandoffDigestMismatchCount, Is.Zero);
            AssertLowerHexSha256(audit.ApprovedAuditDigest);
            AssertLowerHexSha256(audit.Map19_01HandoffDigest);

            TestContext.WriteLine("MAP18_07_DIGEST_EVIDENCE surfaces=6 digests=16" +
                " mismatches=0 approved=" + audit.ApprovedAuditDigest +
                " map19_handoff=" + audit.Map19_01HandoffDigest);
        }

        [Test]
        public void PopulationExitAuditVerifiesRequiredUniqueAndCoreResourcePlacements()
        {
            var surface = Audit().Surface;
            var special = surface.SpecialStateExportSurface;
            var mandatory = special.HazardEnemyPlan.PopulationPlan.MandatoryPlan;
            Assert.That(mandatory.SourceIndex.Count, Is.EqualTo(12));
            Assert.That(mandatory.SourceIndex.MandatoryUniqueCandidates(), Has.Count.EqualTo(5));
            Assert.That(mandatory.EntryCount, Is.EqualTo(4));
            Assert.That(mandatory.RequiredTriggerCount, Is.EqualTo(1));
            Assert.That(mandatory.CoreResourceCount, Is.EqualTo(3));
            Assert.That(surface.CoreResourceCanonicalKeyCheckCount, Is.EqualTo(3));
            Assert.That(surface.ReservedRequiredCoreSlotReuseCount, Is.Zero);

            TestContext.WriteLine("MAP18_07_REQUIRED_EVIDENCE slot_records=12" +
                " candidates=5 placements=4 trigger=1 core=3 canonical=3/3" +
                " reserved_reuse=0");
        }

        [Test]
        public void PopulationExitAuditVerifiesShopResourceMapHazardEnemyAndBudgetSurfaces()
        {
            var surface = Audit().Surface;
            var hazard = surface.SpecialStateExportSurface.HazardEnemyPlan;
            var population = hazard.PopulationPlan;
            Assert.That(population.EntryCount, Is.EqualTo(3));
            Assert.That(population.ShopInventoryEntryCount, Is.EqualTo(1));
            Assert.That(population.OptionalResourceEntryCount, Is.EqualTo(1));
            Assert.That(population.NeutralMapElementEntryCount, Is.EqualTo(1));
            Assert.That(hazard.EntryCount, Is.EqualTo(2));
            Assert.That(hazard.HazardEntryCount, Is.EqualTo(1));
            Assert.That(hazard.EnemyEntryCount, Is.EqualTo(1));
            Assert.That(hazard.OccupiedSurfaceCount, Is.EqualTo(9));
            Assert.That(hazard.BudgetLedger.ScopeCount, Is.EqualTo(5));
            Assert.That(hazard.BudgetLedger.DuplicateSpendKeyCount, Is.Zero);
            Assert.That(hazard.BudgetLedger.NegativeRemainingCount, Is.Zero);
            Assert.That(surface.OccupiedSlotReuseCount, Is.Zero);

            TestContext.WriteLine("MAP18_07_POPULATION_EVIDENCE general=3" +
                " shop_resource_map=1/1/1 hazard_enemy=1/1 occupied=9" +
                " budget_scopes=5 duplicate_spend=0 negative=0 slot_reuse=0");
        }

        [Test]
        public void PopulationExitAuditVerifiesActivityEventRuntimeStateAndSaveKeySurface()
        {
            var surface = Audit().Surface;
            var runtime = surface.SpecialStateExportSurface.RuntimeStateSurface;
            Assert.That(runtime.ActivityRuntimeStateRecordCount, Is.EqualTo(2));
            Assert.That(runtime.EventRuntimeStateRecordCount, Is.EqualTo(4));
            Assert.That(runtime.TotalRuntimeStateRecordCount, Is.EqualTo(6));
            Assert.That(runtime.UniqueRuntimeStateIdCount, Is.EqualTo(6));
            Assert.That(runtime.UniqueSaveKeyCount, Is.EqualTo(6));
            Assert.That(surface.RuntimeStateIdDuplicateCount, Is.Zero);
            Assert.That(surface.SaveKeyDuplicateCount, Is.Zero);
            Assert.That(surface.RoundTripMaterial.SaveKeySetDigestAfterRoundTrip,
                Is.EqualTo(runtime.SaveKeySetDigest));
            Assert.That(surface.RoundTripMaterial.RuntimeStateDigestAfterRoundTrip,
                Is.EqualTo(runtime.Digest));

            TestContext.WriteLine("MAP18_07_RUNTIME_EVIDENCE activity_event=2/4" +
                " records=6 runtime_ids=6 save_keys=6 duplicates=0/0" +
                " save_digest=" + runtime.SaveKeySetDigest +
                " runtime_digest=" + runtime.Digest);
        }

        [Test]
        public void PopulationExitAuditVerifiesSpecialExportCsvMaterialAndDebugSnapshotWithoutFileIo()
        {
            var surface = Audit().Surface;
            var special = surface.SpecialStateExportSurface;
            Assert.That(special.TotalExportRowCount, Is.EqualTo(18));
            Assert.That(special.CsvMaterial.RowCount, Is.EqualTo(18));
            Assert.That(special.DebugSnapshot.SectionCount, Is.EqualTo(5));
            Assert.That(surface.ActivePersistenceKeyDuplicateCount, Is.Zero);
            Assert.That(surface.ExportRowKeyDuplicateCount, Is.Zero);
            Assert.That(surface.LegacyShortPersistenceKeyAcceptedCount, Is.Zero);
            Assert.That(surface.RoundTripMaterial.MaterialRowCount, Is.EqualTo(18));
            Assert.That(surface.RoundTripMaterial.ExportRowDigestAfterRoundTrip,
                Is.EqualTo(special.Digest));
            Assert.That(new[]
            {
                surface.RoundTripMaterial.ActualFileWriteCount,
                surface.RoundTripMaterial.ActualFileReadCount,
                surface.RoundTripMaterial.PlayerPrefsWriteCount,
                surface.RoundTripMaterial.PlayerPrefsReadCount,
            }, Is.All.Zero);

            TestContext.WriteLine("MAP18_07_EXPORT_EVIDENCE rows=18 csv_rows=18" +
                " debug_sections=5 persistence_export_duplicates=0/0 legacy=0" +
                " csv_digest=" + special.CsvMaterial.Digest +
                " debug_digest=" + special.DebugSnapshot.Digest +
                " export_digest=" + special.Digest);
        }

        [Test]
        public void PopulationExitAuditRejectsSlotReuseIdentityCollisionLegacyKeyAndDigestMismatch()
        {
            var special = SpecialSurface();
            var occupied = special.OccupiedSurface[0];
            var runtime = special.RuntimeStateSurface.RuntimeStateRecords[0];
            var persistence = special.Rows.First(value => value.HasPersistenceKey);
            var failures = new[]
            {
                GeneratedPopulationExitAuditRunner.Run(
                    new GeneratedPopulationExitAuditRequest(null)),
                Audit(expectedOverrides: new[]
                {
                    Override(GeneratedPopulationExitAuditRunner.SlotIndexDigestInvariant,
                        MutateDigest(GeneratedPopulationExitAuditRunner.ExpectedSlotIndexDigest)),
                }),
                Audit(expectedOverrides: new[]
                {
                    Override(GeneratedPopulationExitAuditRunner.SlotSourceRecordCountInvariant,
                        "11"),
                }),
                Audit(additionalOccupiedReservationKeys: new[]
                    { occupied.ReservationKey }),
                Audit(additionalReservedRequiredCoreKeys: new[]
                    { special.HazardEnemyPlan.PopulationPlan.MandatoryPlan.Exclusions[0]
                        .ReservationKey }),
                Audit(additionalStableSpawnIds: new[]
                    { occupied.StableSpawnId.Value }),
                Audit(additionalRuntimeStateIds: new[]
                    { runtime.RuntimeStateId.Value }),
                Audit(additionalSaveKeys: new[] { runtime.SaveKey.Value }),
                Audit(additionalPersistenceKeys: new[]
                    { persistence.PersistenceKey.Value }),
                Audit(additionalExportRowKeys: new[] { special.Rows[0].RowKey }),
                Audit(additionalPersistenceKeys: new[] { "MOON_CORE_REWARD" }),
            };
            Assert.That(failures, Has.Length.EqualTo(11));
            Assert.That(failures.All(value => !value.Success && value.Surface == null &&
                value.PartialAuditSurfaceCount == 0 &&
                string.IsNullOrEmpty(value.ApprovedAuditDigest) &&
                string.IsNullOrEmpty(value.Map19_01HandoffDigest) &&
                !value.AtomicFailureApprovedDigestPublished), Is.True);
            Assert.That(failures.SelectMany(value => value.Findings).All(value =>
                !string.IsNullOrWhiteSpace(value.Owner) &&
                !string.IsNullOrWhiteSpace(value.Invariant) &&
                !string.IsNullOrWhiteSpace(value.OffendingKey) &&
                !string.IsNullOrWhiteSpace(value.Expected) &&
                !string.IsNullOrWhiteSpace(value.Actual) &&
                !string.IsNullOrWhiteSpace(value.Reason)), Is.True);

            TestContext.WriteLine("MAP18_07_FAILURE_EVIDENCE missing_surface=1" +
                " digest_mismatch=1 required_count=1 identity_collisions=7" +
                " legacy_key=1 partial=0 approved_on_failure=0");
        }

        [Test]
        public void PopulationExitAuditRoundTripsInMemorySaveReloadMaterialWithoutDiskOrPlayerPrefs()
        {
            var accepted = Audit();
            var material = accepted.Surface.RoundTripMaterial;
            Assert.That(material.IsLfNormalized, Is.True);
            Assert.That(material.Text, Does.Not.Contain("\r"));
            Assert.That(material.Text, Does.EndWith("\n"));
            Assert.That(material.MaterialRowCount, Is.EqualTo(18));
            Assert.That(material.RoundTripMismatchCount, Is.Zero);
            AssertLowerHexSha256(material.Digest);
            var corrupt = Audit(corruptRoundTripMaterial: true);
            AssertAtomicFailure(corrupt, "IN_MEMORY_ROUND_TRIP");

            TestContext.WriteLine("MAP18_07_ROUND_TRIP_EVIDENCE rows=18 mismatches=0" +
                " save_digest=" + material.SaveKeySetDigestAfterRoundTrip +
                " runtime_digest=" + material.RuntimeStateDigestAfterRoundTrip +
                " export_digest=" + material.ExportRowDigestAfterRoundTrip +
                " file_io=0/0 playerprefs=0/0 corrupt_failure_probes=1");
        }

        [Test]
        public void PopulationExitAuditDigestsAreStableAcrossRepeatReverseCultureAndCandidateOrder()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var baseline = Audit();
                var repeat = Audit();
                var reverse = Audit(SpecialSurface(CoreResources().Reverse(),
                    DeclaredSources().Reverse()));
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var culture = Audit();
                var candidateOrder = Audit(SpecialSurface(
                    CoreResources().OrderByDescending(value => value.RegionId.Value),
                    DeclaredSources().OrderByDescending(value => value.StableToken)));
                var results = new[]
                    { baseline, repeat, reverse, culture, candidateOrder };
                Assert.That(results.All(value => value.Success), Is.True);
                Assert.That(results.Select(value => value.ApprovedAuditDigest)
                    .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => value.Map19_01HandoffDigest)
                    .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }

            TestContext.WriteLine("MAP18_07_DETERMINISM_EVIDENCE" +
                " repeat_reverse_culture_candidate_order_mismatches=0/0/0/0" +
                " mutation_sensitivity_probes=13");
        }

        [Test]
        public void PopulationExitAuditDoesNotSpawnObjectsMutateScenesOrRunRegressions()
        {
            var surface = Audit().Surface;
            Assert.That(new[]
            {
                surface.RuntimeObjectSpawnCount,
                surface.GameObjectInstantiateCount, surface.GameObjectEnableCount,
                surface.GameObjectDisableCount, surface.GameObjectDestroyCount,
                surface.SystemIoFileWriteCount, surface.SystemIoFileReadCount,
                surface.DiskSaveFileCreateCount, surface.DiskLoadFileCreateCount,
                surface.ActualUserSaveSlotWriteCount,
                surface.PlatformSaveStorageWriteCount,
                surface.ActualCsvFileWriteCount, surface.ActualCsvFileReadCount,
                surface.GeneratedCsvFileCommitCount,
                surface.RuntimeSpecialRegionSpawnCount,
                surface.RuntimeVillageSpawnCount, surface.RuntimeResourceSpawnCount,
                surface.RuntimeForgeSpawnCount, surface.RuntimeBossSpawnCount,
                surface.RuntimeActivityPrefabSpawnCount,
                surface.RuntimeEventPrefabSpawnCount,
                surface.ActualEventActivationCount,
                surface.ActualShopTransactionCount, surface.RewardGrantCount,
                surface.InventoryMutationCount, surface.ResourceMutationCount,
                surface.DamageExecutionCount, surface.CombatExecutionCount,
                surface.EnemyAiExecutionCount, surface.PhysicsExecutionCount,
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
                surface.ProductionSeedApprovalCount,
                surface.PriorTaskTestSelectionCount,
                surface.Legacy19347SelectionCount, surface.PlayModeSelectionCount,
                surface.UnfilteredTestSelectionCount, surface.FullRegressionRunCount,
            }, Is.All.Zero);
            var runtime = Audit(attemptedRuntimeSideEffect: true);
            var csv = Audit(attemptedCsvFileIo: true);
            var save = Audit(attemptedSaveFileIo: true);
            AssertAtomicFailure(runtime, "RUNTIME_SIDE_EFFECT_BOUNDARY");
            AssertAtomicFailure(csv, "RUNTIME_SIDE_EFFECT_BOUNDARY");
            AssertAtomicFailure(save, "RUNTIME_SIDE_EFFECT_BOUNDARY");

            TestContext.WriteLine("MAP18_07_BOUNDARY_EVIDENCE runtime_objects=0" +
                " gameobject=0/0/0/0 systemio=0/0 disk=0/0 csv=0/0" +
                " saves=0 playerprefs=0/0 spawns=0/0/0/0/0/0/0" +
                " shop_reward_inventory_resource=0/0/0/0" +
                " damage_combat_ai_physics=0/0/0/0" +
                " tilemap_calls=0/0/0/0/0 colliders=0/0/0 rigidbody=0" +
                " physics_query_sim=0/0 navmesh_path=0/0 scene_prefab_tilemap=0/0/0" +
                " camera=0/0 loads=0/0/0 seed=0 regressions=0/0/0/0/0" +
                " side_effect_failure_probes=3");
        }

        [Test]
        public void Map18ExitKeepsMap19_01Locked()
        {
            var accepted = Audit();
            Assert.That(GeneratedPopulationExitAuditSurface.DownstreamOwner,
                Is.EqualTo("MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY"));
            Assert.That(GeneratedPopulationExitAuditSurface.OpensDownstreamTask, Is.False);
            Assert.That(accepted.Surface.Map19_01Unlocked, Is.False);
            Assert.That(accepted.Surface.Map19_01Started, Is.False);
            var unlocked = Audit(map19_01Unlocked: true);
            var started = Audit(map19_01Started: true);
            AssertAtomicFailure(unlocked, "MAP19_01_REMAINS_LOCKED");
            AssertAtomicFailure(started, "MAP19_01_REMAINS_LOCKED");

            TestContext.WriteLine("MAP18_07_DECISION_EVIDENCE result=PASS" +
                " downstream=MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY" +
                " unlocked=NO started=NO failure_probes=2");
        }

        private static GeneratedPopulationExitAuditResult Audit(
            GeneratedSpecialStateExportSurface special = null,
            IEnumerable<GeneratedPopulationExitAuditOverride> expectedOverrides = null,
            IEnumerable<string> additionalOccupiedReservationKeys = null,
            IEnumerable<string> additionalReservedRequiredCoreKeys = null,
            IEnumerable<string> additionalStableSpawnIds = null,
            IEnumerable<string> additionalRuntimeStateIds = null,
            IEnumerable<string> additionalSaveKeys = null,
            IEnumerable<string> additionalPersistenceKeys = null,
            IEnumerable<string> additionalExportRowKeys = null,
            bool corruptRoundTripMaterial = false,
            bool attemptedRuntimeSideEffect = false,
            bool attemptedCsvFileIo = false,
            bool attemptedSaveFileIo = false,
            bool map19_01Unlocked = false,
            bool map19_01Started = false)
        {
            var request = new GeneratedPopulationExitAuditRequest(
                special ?? SpecialSurface(), expectedOverrides,
                additionalOccupiedReservationKeys,
                additionalReservedRequiredCoreKeys, additionalStableSpawnIds,
                additionalRuntimeStateIds, additionalSaveKeys,
                additionalPersistenceKeys, additionalExportRowKeys,
                corruptRoundTripMaterial, attemptedRuntimeSideEffect,
                attemptedCsvFileIo, attemptedSaveFileIo, map19_01Unlocked,
                map19_01Started);
            return GeneratedPopulationExitAuditRunner.Run(request);
        }

        private static GeneratedSpecialStateExportSurface SpecialSurface(
            IEnumerable<CoreResourceRegionDefinition> coreResources = null,
            IEnumerable<GeneratedDeclaredSpecialStateSource> declaredSources = null)
        {
            var request = new GeneratedSpecialStateExportRequest(RuntimeSurface(),
                coreResources ?? CoreResources(), declaredSources ?? DeclaredSources(),
                GeneratedSpecialStateExporter.ExpectedRuntimeStateSurfaceDigest,
                GeneratedSpecialStateExporter.ExpectedSaveKeySetDigest,
                GeneratedSpecialStateExporter.ExpectedRuntimeExportSurfaceDigest,
                GeneratedSpecialStateExporter.ExpectedOccupiedSurfaceDigest,
                GeneratedSpecialStateExporter.ExpectedBudgetLedgerDigest,
                GeneratedSpawnStateCsvMaterial.CanonicalHeader,
                expectedSpecialExportSurfaceDigest:
                    GeneratedPopulationExitAuditRunner.ExpectedSpecialExportDigest,
                expectedCsvMaterialDigest:
                    GeneratedPopulationExitAuditRunner.ExpectedCsvMaterialDigest,
                expectedDebugSnapshotDigest:
                    GeneratedPopulationExitAuditRunner.ExpectedDebugSnapshotDigest,
                expectedAuditSurfaceDigest:
                    GeneratedPopulationExitAuditRunner.ExpectedMap18_07AuditSurfaceDigest);
            var result = GeneratedSpecialStateExporter.Export(request);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Failures));
            return result.Surface;
        }

        private static CoreResourceRegionDefinition[] CoreResources() =>
            CoreResourceRegionStarterCatalog.Entries.ToArray();

        private static GeneratedDeclaredSpecialStateSource[] DeclaredSources()
        {
            var forge = SpecialLandmarkRegionStarterCatalog.GetDefinition(
                SpecialLandmarkKind.MoonSealForge);
            var boss = SpecialLandmarkRegionStarterCatalog.GetDefinition(
                SpecialLandmarkKind.BossSealArena);
            var bossMarker = boss.Markers.Single(value =>
                value.Kind == SpecialLandmarkMarkerKind.EncounterPersistence);
            return new[]
            {
                new GeneratedDeclaredSpecialStateSource(
                    GeneratedSpecialStateExportKind.Forge, "MAP13_FORGE",
                    forge.RegionId.Value, forge.RequiredReward.PersistenceKey,
                    GeneratedSpecialStateSourceStatus.Active,
                    "MOON_SEAL_AVAILABLE", forge.CanonicalDigest),
                new GeneratedDeclaredSpecialStateSource(
                    GeneratedSpecialStateExportKind.Boss, "MAP13_BOSS",
                    boss.RegionId.Value, bossMarker.PersistenceKey,
                    GeneratedSpecialStateSourceStatus.Active,
                    "ENCOUNTER_AVAILABLE", boss.CanonicalDigest),
                new GeneratedDeclaredSpecialStateSource(
                    GeneratedSpecialStateExportKind.Village, "MAP13_VILLAGE",
                    "VILLAGE_OPTIONAL_SOURCE", default(SpecialPersistenceKey),
                    GeneratedSpecialStateSourceStatus.AbsentButDeclared,
                    "ABSENT_OPTIONAL", "MAP13_VILLAGE_OPTIONAL_ABSENT_V1"),
            };
        }

        private static GeneratedActivityEventRuntimeStateSurface RuntimeSurface()
        {
            if (acceptedRuntimeSurface != null) return acceptedRuntimeSurface;
            var request = new GeneratedActivityEventRuntimeStateRequest(
                HazardEnemyPlan(), ActivitySources(), EventSources(),
                GeneratedActivityRuntimeTransitionCatalog.CreateAllowed(),
                new[]
                {
                    GeneratedEventRuntimeVariant.Empty,
                    GeneratedEventRuntimeVariant.Active,
                },
                "REFERENCE_SEED_1801", "GENERATOR_V1", "DATA_V1",
                GeneratedActivityEventRuntimeStateInstantiator.ExpectedHazardEnemyPlanDigest,
                GeneratedActivityEventRuntimeStateInstantiator.ExpectedOccupiedSurfaceDigest,
                GeneratedActivityEventRuntimeStateInstantiator.ExpectedBudgetLedgerDigest);
            var result = GeneratedActivityEventRuntimeStateInstantiator.Instantiate(request);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Failures));
            acceptedRuntimeSurface = result.Surface;
            return acceptedRuntimeSurface;
        }

        private static GeneratedActivityRuntimeSource[] ActivitySources() => new[]
        {
            new GeneratedActivityRuntimeSource(Activity("ACTIVITY_ALPHA"),
                new GeneratedSectorCoordinate(0, 0), "ACTIVITY_SOURCE_A_DIGEST"),
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

        private static GeneratedPopulationExitAuditOverride Override(
            string invariant,
            string expected) => new GeneratedPopulationExitAuditOverride(invariant, expected);

        private static void AssertAtomicFailure(
            GeneratedPopulationExitAuditResult result,
            string invariant)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.Not.EqualTo(GeneratedPopulationExitAuditStatus.Pass));
            Assert.That(result.Surface, Is.Null);
            Assert.That(result.PartialAuditSurfaceCount, Is.Zero);
            Assert.That(result.Findings.Any(value => string.Equals(value.Invariant,
                invariant, StringComparison.Ordinal)), Is.True, Describe(result));
            Assert.That(result.ApprovedAuditDigest, Is.Empty);
            Assert.That(result.Map19_01HandoffDigest, Is.Empty);
            Assert.That(result.AtomicFailureApprovedDigestPublished, Is.False);
        }

        private static void AssertLowerHexSha256(string value) =>
            Assert.That(value, Does.Match("^[0-9a-f]{64}$"));

        private static string MutateDigest(string value) =>
            (value[0] == '0' ? "1" : "0") + value.Substring(1);

        private static string Describe(GeneratedPopulationExitAuditResult result) =>
            result == null ? "NULL_RESULT" : string.Join("\n",
                result.Findings.Select(value => value.ToString()));
    }
}
