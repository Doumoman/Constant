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
    [Category("MAP18_06")]
    public sealed class GeneratedSpecialStateExporterTests
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
        public void SpecialStateExporterCreatesResourceForgeBossVillageRuntimeAndSpawnRows()
        {
            var surface = Surface();
            Assert.That(surface.ExportGroupCount, Is.EqualTo(6));
            Assert.That(surface.CoreResourceExportRowCount, Is.EqualTo(3));
            Assert.That(surface.ForgeExportRowCount, Is.EqualTo(1));
            Assert.That(surface.BossExportRowCount, Is.EqualTo(1));
            Assert.That(surface.VillageExportRowCount, Is.EqualTo(1));
            Assert.That(surface.ActivityEventRuntimeExportRowCount, Is.EqualTo(6));
            Assert.That(surface.SpawnStateExportRowCount, Is.EqualTo(6));
            Assert.That(surface.TotalExportRowCount, Is.EqualTo(18));
            Assert.That(surface.AbsentOptionalSpecialSourceCount, Is.EqualTo(1));

            TestContext.WriteLine("MAP18_06_ROW_EVIDENCE groups=6" +
                " core=3 forge=1 boss=1 village=1 runtime=6 spawn=6" +
                " total=18 absent_optional=1");
        }

        [Test]
        public void SpecialStateExportUsesAuthoritativePersistenceKeysAndRejectsLegacyShortKeys()
        {
            var rows = Surface().Rows.Where(value =>
                value.Kind == GeneratedSpecialStateExportKind.CoreResource).ToArray();
            Assert.That(rows.Select(value => value.PersistenceKey.Value), Is.EquivalentTo(
                new[]
                {
                    "SR_STATE_MOON_CORE_SITE_5_REWARD_MOON_CORE_REWARD",
                    "SR_STATE_CASSIA_SAP_SITE_5_REWARD_CASSIA_SAP_REWARD",
                    "SR_STATE_STAR_NURUK_SITE_5_REWARD_STAR_NURUK_REWARD",
                }));
            Assert.That(Surface().Rows.Where(value => value.HasPersistenceKey).All(value =>
                value.PersistenceKey.Value.StartsWith("SR_STATE_",
                    StringComparison.Ordinal)), Is.True);

            var missing = Export(coreResources: CoreResources().Take(2));
            AssertAtomicFailure(missing,
                GeneratedSpecialStateExportFailureCode.MissingCoreResourcePersistenceKey);
            var sources = DeclaredSources();
            var legacy = Export(declaredSources: sources.Select(value =>
                value.Kind == GeneratedSpecialStateExportKind.Forge
                    ? new GeneratedDeclaredSpecialStateSource(value.Kind,
                        value.SourceOwner, value.RegionSiteId,
                        new SpecialPersistenceKey("FORGE_REWARD"), value.Status,
                        value.StateKind, value.SourceDigest)
                    : value));
            AssertAtomicFailure(legacy,
                GeneratedSpecialStateExportFailureCode.LegacyShortPersistenceKey);

            TestContext.WriteLine("MAP18_06_PERSISTENCE_EVIDENCE" +
                " moon_core=YES cassia_sap=YES star_nuruk=YES" +
                " legacy_accepted=0 missing_core_failure_probes=1" +
                " legacy_failure_probes=1");
        }

        [Test]
        public void GeneratedSpawnStateCsvMaterialIsDeterministicLfUtf8AndDoesNotWriteFiles()
        {
            var csv = Surface().CsvMaterial;
            Assert.That(csv.Header,
                Is.EqualTo(GeneratedSpawnStateCsvMaterial.CanonicalHeader));
            Assert.That(csv.HeaderColumnCount, Is.EqualTo(11));
            Assert.That(csv.RowCount, Is.EqualTo(18));
            Assert.That(csv.IsLfNormalized, Is.True);
            Assert.That(csv.Text, Does.Not.Contain("\r"));
            Assert.That(csv.Text, Does.EndWith("\n"));
            Assert.That(csv.HasUtf8Bom, Is.False);
            Assert.That(csv.Utf8Bytes, Is.EqualTo(new System.Text.UTF8Encoding(false)
                .GetBytes(csv.Text)));
            AssertLowerHexSha256(csv.Digest);
            Assert.That(new[]
            {
                csv.ActualFileWriteCount, csv.ActualFileReadCount,
                csv.GeneratedCsvFileCommitCount,
            }, Is.All.Zero);

            TestContext.WriteLine("MAP18_06_CSV_EVIDENCE columns=11 rows=18" +
                " lf=YES utf8_no_bom=YES writes_reads=0/0 committed=0" +
                " digest=" + csv.Digest);
        }

        [Test]
        public void SelectionBudgetDebugSnapshotIncludesRequiredSectionsAndUpstreamDigests()
        {
            var snapshot = Surface().DebugSnapshot;
            Assert.That(snapshot.SectionCount, Is.EqualTo(5));
            Assert.That(new[]
            {
                "Selection", "Occupied", "Budget", "RuntimeState", "Persistence",
            }.All(snapshot.HasSection), Is.True);
            Assert.That(snapshot.UpstreamDigestCount, Is.EqualTo(9));
            Assert.That(snapshot.DigestReferences.Any(value => value.Contains(
                MandatoryPlan().Digest)), Is.True);
            Assert.That(snapshot.DigestReferences.Any(value => value.Contains(
                PopulationPlan().Digest)), Is.True);
            Assert.That(snapshot.DigestReferences.Any(value => value.Contains(
                HazardEnemyPlan().Digest)), Is.True);
            Assert.That(snapshot.DigestReferences.Any(value => value.Contains(
                RuntimeSurface().Digest)), Is.True);
            AssertLowerHexSha256(snapshot.Digest);

            TestContext.WriteLine("MAP18_06_DEBUG_EVIDENCE" +
                " sections=Selection,Occupied,Budget,RuntimeState,Persistence" +
                " section_count=5 digest_references=9 digest=" + snapshot.Digest);
        }

        [Test]
        public void SpecialStateExporterPreservesMap18_05RuntimeSurfaceAndMap18_04BudgetReferences()
        {
            var surface = Surface();
            var runtime = RuntimeSurface();
            Assert.That(ReferenceEquals(surface.RuntimeStateSurface, runtime), Is.True);
            Assert.That(ReferenceEquals(surface.HazardEnemyPlan,
                runtime.HazardEnemyPlan), Is.True);
            Assert.That(ReferenceEquals(surface.OccupiedSurface,
                runtime.OccupiedSurface), Is.True);
            Assert.That(ReferenceEquals(surface.BudgetLedger,
                runtime.BudgetLedger), Is.True);
            Assert.That(runtime.Digest,
                Is.EqualTo(GeneratedSpecialStateExporter.ExpectedRuntimeStateSurfaceDigest));
            Assert.That(runtime.SaveKeySetDigest,
                Is.EqualTo(GeneratedSpecialStateExporter.ExpectedSaveKeySetDigest));
            Assert.That(runtime.ExportSurfaceDigest,
                Is.EqualTo(GeneratedSpecialStateExporter.ExpectedRuntimeExportSurfaceDigest));
            Assert.That(runtime.OccupiedSurfaceDigest,
                Is.EqualTo(GeneratedSpecialStateExporter.ExpectedOccupiedSurfaceDigest));
            Assert.That(runtime.BudgetLedgerDigest,
                Is.EqualTo(GeneratedSpecialStateExporter.ExpectedBudgetLedgerDigest));

            TestContext.WriteLine("MAP18_06_PASSTHROUGH_EVIDENCE" +
                " runtime_records=6 occupied=9 runtime_exact=YES" +
                " occupied_exact=YES budget_exact=YES");
        }

        [Test]
        public void SpecialStateExportIdsSaveKeysAndRowsAreUniqueStableAndMutationSensitive()
        {
            var baseline = Surface();
            var repeat = Surface();
            var mutated = Surface(declaredSources: DeclaredSources("ABSENT_OPTIONAL_MUTATED"));
            Assert.That(baseline.UniqueExportRowKeyCount, Is.EqualTo(18));
            Assert.That(baseline.UniquePersistenceKeyCount, Is.EqualTo(5));
            Assert.That(baseline.UniqueRuntimeStateIdCount, Is.EqualTo(6));
            Assert.That(baseline.UniqueSaveKeyCount, Is.EqualTo(6));
            Assert.That(baseline.UniqueStableSpawnIdCount, Is.EqualTo(9));
            Assert.That(new[]
            {
                baseline.DuplicateExportRowKeyCount,
                baseline.DuplicatePersistenceKeyCount,
                baseline.DuplicateRuntimeStateIdCount,
                baseline.DuplicateSaveKeyCount,
                baseline.DuplicateStableSpawnIdCount,
            }, Is.All.Zero);
            Assert.That(repeat.Digest, Is.EqualTo(baseline.Digest));
            Assert.That(mutated.Digest, Is.Not.EqualTo(baseline.Digest));
            Assert.That(mutated.CsvMaterial.Digest,
                Is.Not.EqualTo(baseline.CsvMaterial.Digest));
            Assert.That(mutated.DebugSnapshot.Digest,
                Is.Not.EqualTo(baseline.DebugSnapshot.Digest));
            Assert.That(mutated.Map18_07AuditSurfaceDigest,
                Is.Not.EqualTo(baseline.Map18_07AuditSurfaceDigest));
            AssertLowerHexSha256(baseline.Digest);
            AssertLowerHexSha256(baseline.Map18_07AuditSurfaceDigest);

            TestContext.WriteLine("MAP18_06_IDENTITY_EVIDENCE" +
                " rows=18 persistence=5 runtime_ids=6 save_keys=6 spawn_ids=9" +
                " duplicates=0/0/0/0/0 mutation_probes=4" +
                " surface_digest=" + baseline.Digest +
                " audit_digest=" + baseline.Map18_07AuditSurfaceDigest);
        }

        [Test]
        public void SpecialStateExportFailuresAreAtomicAndReportOwnerReasonExpectedActual()
        {
            var baseline = Surface();
            var failures = new[]
            {
                Export(missingRuntimeSurface: true),
                Export(expectedRuntimeStateSurfaceDigest: MutateDigest(
                    GeneratedSpecialStateExporter.ExpectedRuntimeStateSurfaceDigest)),
                Export(expectedSaveKeySetDigest: MutateDigest(
                    GeneratedSpecialStateExporter.ExpectedSaveKeySetDigest)),
                Export(expectedRuntimeExportSurfaceDigest: MutateDigest(
                    GeneratedSpecialStateExporter.ExpectedRuntimeExportSurfaceDigest)),
                Export(expectedOccupiedSurfaceDigest: MutateDigest(
                    GeneratedSpecialStateExporter.ExpectedOccupiedSurfaceDigest)),
                Export(expectedBudgetLedgerDigest: MutateDigest(
                    GeneratedSpecialStateExporter.ExpectedBudgetLedgerDigest)),
                Export(coreResources: CoreResources().Take(2)),
                Export(declaredSources: LegacyDeclaredSources()),
                Export(existingRowKeys: new[] { baseline.Rows[0].RowKey }),
                Export(existingPersistenceKeys: new[]
                    { baseline.Rows.First(value => value.HasPersistenceKey)
                        .PersistenceKey.Value }),
                Export(existingRuntimeStateIds: new[]
                    { baseline.Rows.First(value => value.HasRuntimeStateId)
                        .RuntimeStateId }),
                Export(existingSaveKeys: new[]
                    { baseline.Rows.First(value => value.HasSaveKey).SaveKey }),
                Export(existingStableSpawnIds: new[]
                    { baseline.Rows.First(value => value.HasStableSpawnId)
                        .StableSpawnId }),
                Export(expectedCsvHeader: "bad,header"),
                Export(attemptedCsvFileWrite: true, attemptedCsvFileRead: true,
                    attemptedSaveWrite: true, attemptedSaveRead: true,
                    attemptedRuntimeSpawn: true, attemptedRewardGrant: true,
                    attemptedDamage: true, attemptedPhysics: true,
                    attemptedAiHookup: true, attemptedEventExecution: true),
            };
            Assert.That(failures, Has.Length.EqualTo(15));
            Assert.That(failures.All(value => !value.Success && value.Surface == null &&
                value.PartialExportRowCount == 0 &&
                value.PartialCsvMaterialCount == 0 &&
                value.PartialDebugSnapshotCount == 0 && value.RetryLoopCount == 0),
                Is.True);
            Assert.That(failures.SelectMany(value => value.Failures).All(value =>
                !string.IsNullOrWhiteSpace(value.Owner) &&
                !string.IsNullOrWhiteSpace(value.Reason) &&
                !string.IsNullOrWhiteSpace(value.OffendingKey) &&
                !string.IsNullOrWhiteSpace(value.Expected) &&
                !string.IsNullOrWhiteSpace(value.Actual)), Is.True);

            TestContext.WriteLine("MAP18_06_FAILURE_EVIDENCE" +
                " missing_source=1 digest_mismatch=5 missing_core=1 legacy=1" +
                " duplicate_row_persistence_runtime_save_spawn=1/1/1/1/1" +
                " csv_shape=1 side_effect_flags=10 partial_rows_csv_debug=0/0/0");
        }

        [Test]
        public void SpecialStateExportDigestsAreStableAcrossRepeatReverseCultureAndCandidateOrder()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var baseline = Surface();
                var repeat = Surface();
                var reverse = Surface(CoreResources().Reverse(),
                    DeclaredSources().Reverse());
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var culture = Surface();
                var candidateOrder = Surface(CoreResources().OrderByDescending(value =>
                    value.RegionId.Value), DeclaredSources().OrderByDescending(value =>
                    value.StableToken));
                var surfaces = new[]
                    { baseline, repeat, reverse, culture, candidateOrder };
                AssertSameDigest(surfaces, value => value.Digest);
                AssertSameDigest(surfaces, value => value.CsvMaterial.Digest);
                AssertSameDigest(surfaces, value => value.DebugSnapshot.Digest);
                AssertSameDigest(surfaces, value => value.Map18_07AuditSurfaceDigest);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }

            TestContext.WriteLine("MAP18_06_DETERMINISM_EVIDENCE" +
                " repeat_reverse_culture_candidate_order_mismatches=0/0/0/0" +
                " unity_random=0 random_range=0 system_random=0 retries=0");
        }

        [Test]
        public void SpecialStateExporterDoesNotSpawnObjectsWriteSavesMutateScenesOrRunRegressions()
        {
            var surface = Surface();
            Assert.That(new[]
            {
                surface.ActualCsvFileWriteCount, surface.ActualCsvFileReadCount,
                surface.GeneratedCsvFileCommitCount, surface.SaveWriteCount,
                surface.SaveReadCount, surface.PlayerPrefsWriteCount,
                surface.PlayerPrefsReadCount, surface.RuntimeSpecialRegionSpawnCount,
                surface.RuntimeVillageSpawnCount, surface.RuntimeResourceSpawnCount,
                surface.RuntimeForgeSpawnCount, surface.RuntimeBossSpawnCount,
                surface.RuntimeActivityPrefabSpawnCount,
                surface.RuntimeEventPrefabSpawnCount,
                surface.ActualEventActivationCount, surface.RewardGrantCount,
                surface.InventoryMutationCount, surface.ResourceMutationCount,
                surface.DamageExecutionCount, surface.EnemyAiControllerHookupCount,
                surface.RuntimeObjectSpawnCount, surface.GameObjectInstantiateCount,
                surface.GameObjectEnableCount, surface.GameObjectDisableCount,
                surface.GameObjectDestroyCount, surface.SystemIoFileWriteCount,
                surface.SystemIoFileReadCount, surface.DiskSaveFileCreateCount,
                surface.DiskLoadFileCreateCount, surface.TilemapComponentWriteCount,
                surface.TilemapSetTileCallCount, surface.TilemapSetTilesCallCount,
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
                surface.UnityEngineRandomCallCount, surface.RandomRangeCallCount,
                surface.SystemRandomDirectUsageCount, surface.HiddenRetryLoopCount,
                surface.ImplicitSpecialSourceCreationCount,
                surface.PriorTaskTestSelectionCount,
                surface.Legacy19347SelectionCount, surface.PlayModeSelectionCount,
                surface.UnfilteredTestSelectionCount, surface.FullRegressionRunCount,
            }, Is.All.Zero);

            TestContext.WriteLine("MAP18_06_SIDE_EFFECT_EVIDENCE" +
                " csv=0/0 committed=0 save=0/0 playerprefs=0/0" +
                " special_village_resource_forge_boss=0/0/0/0/0" +
                " activity_event=0/0 event_activation=0 reward=0" +
                " inventory_resource=0/0 damage_ai=0/0 runtime_objects=0" +
                " gameobject=0/0/0/0 systemio=0/0 disk=0/0" +
                " tilemap=0/0/0/0/0 colliders=0/0/0 rigidbody=0" +
                " physics=0/0 navmesh_path=0/0 scene_prefab_tilemap=0/0/0" +
                " camera=0/0 loads=0/0/0 seed=0 regressions=0/0/0/0/0");
        }

        [Test]
        public void Map18HandoffKeepsMap18_07Locked()
        {
            var surface = Surface();
            Assert.That(GeneratedSpecialStateExportSurface.DownstreamOwner,
                Is.EqualTo("MAP18_07_MAP18_POPULATION_EXIT_TESTS"));
            Assert.That(GeneratedSpecialStateExportSurface.OpensDownstreamTask,
                Is.False);
            Assert.That(surface.Map18_07Started, Is.False);

            TestContext.WriteLine("MAP18_06_DECISION_EVIDENCE result=PASS" +
                " downstream=MAP18_07_MAP18_POPULATION_EXIT_TESTS" +
                " started=NO locked=YES");
        }

        private static GeneratedSpecialStateExportSurface Surface(
            IEnumerable<CoreResourceRegionDefinition> coreResources = null,
            IEnumerable<GeneratedDeclaredSpecialStateSource> declaredSources = null)
        {
            var result = Export(coreResources: coreResources,
                declaredSources: declaredSources);
            Assert.That(result.Success, Is.True, Describe(result));
            return result.Surface;
        }

        private static GeneratedSpecialStateExportResult Export(
            bool missingRuntimeSurface = false,
            IEnumerable<CoreResourceRegionDefinition> coreResources = null,
            IEnumerable<GeneratedDeclaredSpecialStateSource> declaredSources = null,
            string expectedRuntimeStateSurfaceDigest = null,
            string expectedSaveKeySetDigest = null,
            string expectedRuntimeExportSurfaceDigest = null,
            string expectedOccupiedSurfaceDigest = null,
            string expectedBudgetLedgerDigest = null,
            string expectedCsvHeader = null,
            IEnumerable<string> existingRowKeys = null,
            IEnumerable<string> existingPersistenceKeys = null,
            IEnumerable<string> existingRuntimeStateIds = null,
            IEnumerable<string> existingSaveKeys = null,
            IEnumerable<string> existingStableSpawnIds = null,
            bool attemptedCsvFileWrite = false,
            bool attemptedCsvFileRead = false,
            bool attemptedSaveWrite = false,
            bool attemptedSaveRead = false,
            bool attemptedRuntimeSpawn = false,
            bool attemptedRewardGrant = false,
            bool attemptedDamage = false,
            bool attemptedPhysics = false,
            bool attemptedAiHookup = false,
            bool attemptedEventExecution = false)
        {
            var request = new GeneratedSpecialStateExportRequest(
                missingRuntimeSurface ? null : RuntimeSurface(),
                coreResources ?? CoreResources(), declaredSources ?? DeclaredSources(),
                expectedRuntimeStateSurfaceDigest ??
                    GeneratedSpecialStateExporter.ExpectedRuntimeStateSurfaceDigest,
                expectedSaveKeySetDigest ??
                    GeneratedSpecialStateExporter.ExpectedSaveKeySetDigest,
                expectedRuntimeExportSurfaceDigest ??
                    GeneratedSpecialStateExporter.ExpectedRuntimeExportSurfaceDigest,
                expectedOccupiedSurfaceDigest ??
                    GeneratedSpecialStateExporter.ExpectedOccupiedSurfaceDigest,
                expectedBudgetLedgerDigest ??
                    GeneratedSpecialStateExporter.ExpectedBudgetLedgerDigest,
                expectedCsvHeader ?? GeneratedSpawnStateCsvMaterial.CanonicalHeader,
                existingRowKeys, existingPersistenceKeys, existingRuntimeStateIds,
                existingSaveKeys, existingStableSpawnIds,
                attemptedCsvFileWrite: attemptedCsvFileWrite,
                attemptedCsvFileRead: attemptedCsvFileRead,
                attemptedSaveWrite: attemptedSaveWrite,
                attemptedSaveRead: attemptedSaveRead,
                attemptedRuntimeSpawn: attemptedRuntimeSpawn,
                attemptedRewardGrant: attemptedRewardGrant,
                attemptedDamage: attemptedDamage,
                attemptedPhysics: attemptedPhysics,
                attemptedAiHookup: attemptedAiHookup,
                attemptedEventExecution: attemptedEventExecution);
            return GeneratedSpecialStateExporter.Export(request);
        }

        private static CoreResourceRegionDefinition[] CoreResources() =>
            CoreResourceRegionStarterCatalog.Entries.ToArray();

        private static GeneratedDeclaredSpecialStateSource[] DeclaredSources(
            string villageState = "ABSENT_OPTIONAL")
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
                    villageState, "MAP13_VILLAGE_OPTIONAL_ABSENT_V1"),
            };
        }

        private static GeneratedDeclaredSpecialStateSource[] LegacyDeclaredSources() =>
            DeclaredSources().Select(value => value.Kind ==
                GeneratedSpecialStateExportKind.Forge
                ? new GeneratedDeclaredSpecialStateSource(value.Kind,
                    value.SourceOwner, value.RegionSiteId,
                    new SpecialPersistenceKey("FORGE_REWARD"), value.Status,
                    value.StateKind, value.SourceDigest)
                : value).ToArray();

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

        private static void AssertAtomicFailure(
            GeneratedSpecialStateExportResult result,
            GeneratedSpecialStateExportFailureCode code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Surface, Is.Null);
            Assert.That(result.Failures.Any(value => value.Code == code), Is.True,
                Describe(result));
            Assert.That(result.PartialExportRowCount, Is.Zero);
            Assert.That(result.PartialCsvMaterialCount, Is.Zero);
            Assert.That(result.PartialDebugSnapshotCount, Is.Zero);
            Assert.That(result.RetryLoopCount, Is.Zero);
        }

        private static void AssertSameDigest(
            IEnumerable<GeneratedSpecialStateExportSurface> source,
            Func<GeneratedSpecialStateExportSurface, string> selector) =>
            Assert.That(source.Select(selector).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(1));

        private static void AssertLowerHexSha256(string value) =>
            Assert.That(value, Does.Match("^[0-9a-f]{64}$"));

        private static string MutateDigest(string value) =>
            (value[0] == '0' ? "1" : "0") + value.Substring(1);

        private static string Describe(GeneratedSpecialStateExportResult result) =>
            result == null ? "NULL_RESULT" : string.Join("\n",
                result.Failures.Select(value => value.ToString()));
    }
}
