using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Baking
{
    [TestFixture]
    [Category("MAP17_08")]
    public sealed class GeneratedMap17RuntimeExitAuditTests
    {
        private static GeneratedTerrainPerformanceReport performance;
        private static GeneratedMap17ExitAuditResult audit;

        [Test]
        public void Map17ExitAuditApprovesAssetPlacementBakeAndSeamContracts()
        {
            var value = Audit().Report;
            AssertPass(value, "asset_resolution");
            AssertPass(value, "placement");
            AssertPass(value, "logical_bake");
            AssertPass(value, "seam_validation");
            Assert.That(value.Item("placement").Actual, Is.EqualTo("1536/10752"));
            Assert.That(value.Item("logical_bake").Actual, Is.EqualTo("7/0/0/0"));
            Assert.That(value.Item("seam_validation").Actual, Is.EqualTo("688/240/448"));

            TestContext.WriteLine("MAP17_08_BAKE_READINESS asset=PASS placement=1536/10752" +
                " logical_bake=7/0/0/0 seam=688/240/448");
        }

        [Test]
        public void Map17ExitAuditApprovesColliderHandleAndStreamingContracts()
        {
            var value = Audit().Report;
            AssertPass(value, "collider_cache");
            AssertPass(value, "runtime_handle_lifecycle");
            AssertPass(value, "stream_window");
            AssertPass(value, "active_subset_preload");
            Assert.That(value.Item("collider_cache").Actual, Is.EqualTo("1/1/1/1"));
            Assert.That(value.Item("runtime_handle_lifecycle").Actual, Is.EqualTo("4"));
            Assert.That(value.Item("stream_window").Actual,
                Is.EqualTo("49/25|28/15|16/9"));
            Assert.That(value.Item("active_subset_preload").Actual, Is.EqualTo("YES"));

            TestContext.WriteLine("MAP17_08_RUNTIME_READINESS collider=1/1/1/1" +
                " lifecycle=Unloaded/Preloaded/Active/SleepingModified" +
                " stream=49/25|28/15|16/9 active_subset=YES");
        }

        [Test]
        public void Map17ExitAuditApprovesModificationManifestAndRegenerationContracts()
        {
            var value = Audit().Report;
            AssertPass(value, "modification_storage");
            AssertPass(value, "save_manifest");
            AssertPass(value, "regeneration_apply");
            AssertPass(value, "hash_mismatch");
            AssertPass(value, "performance_report");
            Assert.That(value.Item("modification_storage").Actual, Is.EqualTo("1/5/5"));
            Assert.That(value.Item("save_manifest").Actual, Is.EqualTo("1/0/5|168"));
            Assert.That(value.Item("regeneration_apply").Actual, Is.EqualTo("1/5/0"));
            Assert.That(value.Item("hash_mismatch").Actual, Is.EqualTo("6/0/0"));
            Assert.That(value.Item("performance_report").Actual,
                Is.EqualTo("10|" + GeneratedMap17ExitAuditService.ExpectedPerformanceReportDigest));

            TestContext.WriteLine("MAP17_08_SAVE_READINESS modification=1/5/5" +
                " manifest=1/0/5|omitted168 regeneration=1/5/0" +
                " hash_mismatch=6/0/0 performance_groups=10");
        }

        [Test]
        public void Map17ExitAuditClassifiesPerformanceSpikeWithoutOptimizationRewrite()
        {
            var value = Audit().Report;
            var item = value.Item("performance_spike");
            var risk = value.Risk("layer_bake_diagnostic_spike");
            Assert.That(item.Severity, Is.EqualTo(GeneratedMap17ExitAuditSeverity.Warning));
            Assert.That(item.Actual, Is.EqualTo("3358.202900 ms"));
            Assert.That(risk.Severity, Is.EqualTo(GeneratedMap17ExitAuditSeverity.Warning));
            Assert.That(risk.BlocksMap18Handoff, Is.False);
            Assert.That(value.StructuralCountMismatchCount, Is.Zero);
            Assert.That(value.HiddenRetryLoopCount, Is.Zero);
            Assert.That(value.OptimizationRewriteCount, Is.Zero);
            Assert.That(value.IsPhaseReady, Is.True);

            TestContext.WriteLine("MAP17_08_PERFORMANCE_RISK spike_ms=3358.202900" +
                " classification=WARN blocks_MAP18_01=NO digest_mismatch=0/0/0/0" +
                " structural_mismatch=0 side_effects=0 retry_loops=0 strict_ms_gate=NO" +
                " optimization_rewrites=0");
        }

        [Test]
        public void Map17ExitAuditCarriesDuplicationAndHardcodingRisksWithoutCleanup()
        {
            var value = Audit().Report;
            var item = value.Item("duplication_hardcoding");
            var duplicate = value.Risk("duplicate_fixture_adapter");
            var constants = value.Risk("named_budget_constants");
            Assert.That(item.Severity, Is.EqualTo(GeneratedMap17ExitAuditSeverity.Warning));
            Assert.That(item.Actual, Is.EqualTo("1/22/1"));
            Assert.That(duplicate.Owner, Is.EqualTo("LATER_APPROVED_CLEANUP_TASK"));
            Assert.That(duplicate.BlocksMap18Handoff, Is.False);
            Assert.That(constants.Severity, Is.EqualTo(GeneratedMap17ExitAuditSeverity.Info));
            Assert.That(value.CleanupRefactorCount, Is.Zero);

            TestContext.WriteLine("MAP17_08_DUPLICATION_RISK duplicate_helpers=1" +
                " owner=LATER_APPROVED_CLEANUP_TASK hardcoded_constants=22" +
                " named_budget_constants=YES consolidation_candidates=1" +
                " blocks_MAP18_01=NO cleanup_refactor=0");
        }

        [Test]
        public void Map17ExitAuditRejectsMissingOrMismatchedUpstreamEvidenceAtomically()
        {
            var missingRequest = GeneratedMap17ExitAuditService.Audit(null);
            var missingPerformance = GeneratedMap17ExitAuditService.Audit(
                MissingPerformanceRequest());
            var digest = GeneratedMap17ExitAuditService.Audit(Request(
                expectedDigest: Hash("STALE_PERFORMANCE_REPORT")));
            var timing = GeneratedMap17ExitAuditService.Audit(Request(layerBakeMax: 1d));
            var duplication = GeneratedMap17ExitAuditService.Audit(Request(duplicateHelpers: 2));
            var lifecycle = GeneratedMap17ExitAuditService.Audit(Request(
                states: new[] { GeneratedSectorRuntimeState.Unloaded }));
            var owners = GeneratedMap17ExitAuditService.RequiredDeferredOwners.ToDictionary(
                value => value.Key, value => value.Value, StringComparer.Ordinal);
            owners["population_stable_spawn_id"] = "WRONG_OWNER";
            var deferred = GeneratedMap17ExitAuditService.Audit(Request(owners: owners));
            var probes = new[] { missingRequest, missingPerformance, digest, timing,
                duplication, lifecycle, deferred };
            Assert.That(probes.All(value => !value.Success && value.Report == null &&
                value.PartialMutationCount == 0), Is.True);
            AssertFailure(missingRequest, GeneratedMap17ExitAuditFailureCode.MissingRequest);
            AssertFailure(missingPerformance,
                GeneratedMap17ExitAuditFailureCode.MissingPerformanceReport);
            AssertFailure(digest, GeneratedMap17ExitAuditFailureCode.UpstreamDigestMismatch);
            AssertFailure(timing, GeneratedMap17ExitAuditFailureCode.PerformanceEvidenceMismatch);
            AssertFailure(duplication,
                GeneratedMap17ExitAuditFailureCode.DuplicationEvidenceMismatch);
            AssertFailure(lifecycle,
                GeneratedMap17ExitAuditFailureCode.LifecycleEvidenceMismatch);
            AssertFailure(deferred, GeneratedMap17ExitAuditFailureCode.DeferredOwnerMismatch);

            TestContext.WriteLine("MAP17_08_REJECTION_EVIDENCE missing_request_report=2/2" +
                " digest_timing_duplication_lifecycle_deferred=5/5 partial_mutations=0");
        }

        [Test]
        public void Map17ExitAuditReportsDeferredOwnershipForPopulationRuntimeAndDiskSave()
        {
            var value = Audit().Report;
            AssertPass(value, "deferred_ownership");
            Assert.That(GeneratedMap17ExitAuditService.RequiredDeferredOwners.Count, Is.EqualTo(9));
            Assert.That(value.Risk("deferred_population_stable_spawn_id").Owner,
                Is.EqualTo("MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS"));
            Assert.That(value.Risk("deferred_mandatory_unique_content").Owner,
                Is.EqualTo("MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT"));
            Assert.That(value.Risk("deferred_actual_population").Owner,
                Is.EqualTo("MAP18_03_TO_MAP18_04"));
            Assert.That(value.Risk("deferred_activity_event_runtime_state").Owner,
                Is.EqualTo("MAP18_05_INSTANTIATE_ACTIVITY_EVENT_RUNTIME_STATE"));
            Assert.That(value.Risk("deferred_special_state_export_debug").Owner,
                Is.EqualTo("MAP18_06_EXPORT_SPECIAL_STATE_AND_DEBUG"));
            Assert.That(value.Risk("deferred_actual_live_traversal").Owner,
                Is.EqualTo("LATER_PLAYMODE_LIVE_INTEGRATION_TASK"));
            Assert.That(value.Risk("deferred_actual_disk_save").Owner,
                Is.EqualTo("LATER_SAVE_SYSTEM_INTEGRATION_TASK"));
            Assert.That(value.Risk("deferred_optimization").Owner,
                Is.EqualTo("LATER_APPROVED_OPTIMIZATION_TASK"));
            Assert.That(value.Risk("deferred_fixture_consolidation").Owner,
                Is.EqualTo("LATER_APPROVED_CLEANUP_TASK"));
            Assert.That(value.Risks.All(risk => !risk.BlocksMap18Handoff), Is.True);

            TestContext.WriteLine("MAP17_08_DEFERRED_EVIDENCE owners=9/9" +
                " population=MAP18_01 unique=MAP18_02 population_runtime=MAP18_03_TO_04" +
                " activity_state=MAP18_05 special_debug=MAP18_06 live=LATER_PLAYMODE" +
                " disk=LATER_SAVE optimization=LATER_OPTIMIZATION fixture=LATER_CLEANUP");
        }

        [Test]
        public void Map17ExitAuditDigestIsStableAcrossRepeatReverseCultureAndRiskOrder()
        {
            var baseline = Audit().Report;
            var repeat = GeneratedMap17ExitAuditService.Audit(Request()).Report;
            var reversePerformance = GeneratedTerrainPerformanceHarness.RebuildReport(
                Performance(), Performance().Samples.Reverse());
            var reverse = GeneratedMap17ExitAuditService.Audit(Request(reversePerformance)).Report;
            GeneratedMap17ExitAuditReport culture;
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                culture = GeneratedMap17ExitAuditService.Audit(Request()).Report;
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
            var riskOrder = new GeneratedMap17ExitAuditReport(
                baseline.Items.Reverse(), baseline.Risks.Reverse());

            Assert.That(new[] { baseline.Digest, repeat.Digest, reverse.Digest,
                culture.Digest, riskOrder.Digest }.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(baseline.Digest), Is.True);
            Assert.That(baseline.ItemCount, Is.EqualTo(16));
            Assert.That(new[] { baseline.PassCount, baseline.WarningCount,
                baseline.BlockerCount, baseline.FailureCount }, Is.EqualTo(new[] { 14, 2, 0, 0 }));
            Assert.That(baseline.Risks.Count, Is.EqualTo(12));

            TestContext.WriteLine("MAP17_08_DIGEST_EVIDENCE report=" + baseline.Digest +
                " repeat_reverse_culture_risk_order_mismatches=0/0/0/0" +
                " items=16 pass_warn_block_fail=14/2/0/0 risks=12");
        }

        [Test]
        public void Map17ExitAuditDoesNotMutateScenesWriteFilesLoadAssetsOrRunRegressions()
        {
            var value = Audit().Report;
            Assert.That(new[] { value.SystemIoFileReadCount, value.SystemIoFileWriteCount,
                value.DiskSaveFileCreateCount, value.DiskLoadFileCreateCount,
                value.UserSaveSlotWriteCount, value.PlatformStorageWriteCount,
                value.TilemapComponentWriteCount, value.TilemapSetTileCallCount,
                value.TilemapSetTilesCallCount, value.TilemapSetTilesBlockCallCount,
                value.TilemapClearAllTilesCallCount, value.TilemapColliderCreationCount,
                value.CompositeColliderCreationCount, value.ColliderCreationCount,
                value.RigidbodyCreationCount, value.PhysicsQueryCount,
                value.PhysicsSimulationCount, value.SceneMutationCount,
                value.PrefabMutationCount, value.TilemapMutationCount,
                value.GameObjectInstantiationCount, value.GameObjectEnableCount,
                value.GameObjectDisableCount, value.GameObjectDestroyCount,
                value.CameraReadCount, value.CameraWriteCount, value.AddressablesLoadCount,
                value.ResourcesLoadCount, value.AssetDatabaseLoadCount,
                value.AuthoringCsvEditCount, value.GeneratedCsvCommitCount,
                value.GeneratedAssetCommitCount, value.RuntimeObjectSpawnCount,
                value.PopulationStableSpawnIdCount, value.ProductionSeedApprovalCount,
                value.HiddenRetryLoopCount, value.OptimizationRewriteCount,
                value.CleanupRefactorCount }, Is.All.Zero);

            TestContext.WriteLine("MAP17_08_SIDE_EFFECT_EVIDENCE system_io=0/0 disk=0/0" +
                " user_slot_platform=0/0 tilemap=0/0/0/0/0 colliders=0/0/0" +
                " rigidbody=0 physics=0/0 scene_prefab_tilemap=0/0/0" +
                " gameobject=0/0/0/0 camera=0/0 asset_loads=0/0/0 csv=0" +
                " generated=0/0 runtime_population=0/0 seed=0 regressions=0");
        }

        [Test]
        public void Map17HandoffKeepsMap18_01LockedUntilReviewedPass()
        {
            var value = Audit().Report;
            Assert.That(value.Verdict, Is.EqualTo("PASS"));
            Assert.That(value.IsPhaseReady, Is.True);
            Assert.That(value.Map18HandoffApproved, Is.True);
            Assert.That(value.Map18Started, Is.False);
            Assert.That(GeneratedMap17ExitAuditReport.DownstreamOwner,
                Is.EqualTo("MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS"));
            Assert.That(GeneratedMap17ExitAuditReport.OpensDownstreamTask, Is.False);

            TestContext.WriteLine("MAP17_08_DECISION_EVIDENCE verdict=PASS handoff_approved=YES" +
                " MAP18_01_started=NO locked_until_reviewed_pass=YES");
        }

        private static GeneratedMap17ExitAuditResult Audit()
        {
            if (audit != null) return audit;
            audit = GeneratedMap17ExitAuditService.Audit(Request());
            Assert.That(audit.Success, Is.True, Describe(audit.Failures));
            return audit;
        }

        private static GeneratedTerrainPerformanceReport Performance()
        {
            if (performance != null) return performance;
            performance = GeneratedTerrainPerformanceHarness.Run();
            Assert.That(performance.Success, Is.True, string.Join(";", performance.Failures));
            Assert.That(performance.Digest,
                Is.EqualTo(GeneratedMap17ExitAuditService.ExpectedPerformanceReportDigest));
            return performance;
        }

        private static GeneratedMap17ExitAuditRequest Request(
            GeneratedTerrainPerformanceReport performanceReport = null,
            string expectedDigest = null,
            double? layerBakeMax = null,
            int? duplicateHelpers = null,
            int? hardcodedConstants = null,
            int? consolidationCandidates = null,
            IEnumerable<GeneratedSectorRuntimeState> states = null,
            IEnumerable<KeyValuePair<string, string>> owners = null) =>
            new GeneratedMap17ExitAuditRequest(performanceReport ?? Performance(),
                expectedDigest ?? GeneratedMap17ExitAuditService.ExpectedPerformanceReportDigest,
                layerBakeMax ?? GeneratedMap17ExitAuditService.ExpectedLayerBakeMaximumMilliseconds,
                duplicateHelpers ?? GeneratedMap17ExitAuditService.ExpectedDuplicateHelperCount,
                hardcodedConstants ?? GeneratedMap17ExitAuditService.ExpectedHardcodedCountConstantCount,
                consolidationCandidates ??
                GeneratedMap17ExitAuditService.ExpectedConsolidationCandidateCount,
                states ?? LifecycleStates(),
                owners ?? GeneratedMap17ExitAuditService.RequiredDeferredOwners);

        private static GeneratedMap17ExitAuditRequest MissingPerformanceRequest() =>
            new GeneratedMap17ExitAuditRequest(null,
                GeneratedMap17ExitAuditService.ExpectedPerformanceReportDigest,
                GeneratedMap17ExitAuditService.ExpectedLayerBakeMaximumMilliseconds,
                GeneratedMap17ExitAuditService.ExpectedDuplicateHelperCount,
                GeneratedMap17ExitAuditService.ExpectedHardcodedCountConstantCount,
                GeneratedMap17ExitAuditService.ExpectedConsolidationCandidateCount,
                LifecycleStates(), GeneratedMap17ExitAuditService.RequiredDeferredOwners);

        private static GeneratedSectorRuntimeState[] LifecycleStates() => new[]
        {
            GeneratedSectorRuntimeState.Unloaded,
            GeneratedSectorRuntimeState.Preloaded,
            GeneratedSectorRuntimeState.Active,
            GeneratedSectorRuntimeState.SleepingModified,
        };

        private static void AssertPass(GeneratedMap17ExitAuditReport value, string key)
        {
            var item = value.Item(key);
            Assert.That(item.Severity, Is.EqualTo(GeneratedMap17ExitAuditSeverity.Pass),
                item.ToString());
            Assert.That(item.IsApproved, Is.True);
        }

        private static void AssertFailure(GeneratedMap17ExitAuditResult value,
            GeneratedMap17ExitAuditFailureCode code) => Assert.That(
                value.Failures.Select(failure => failure.Code), Does.Contain(code), Describe(value.Failures));

        private static string Hash(string value) =>
            BakingCanonicalDigest.HashCanonicalLines(new[] { value });

        private static string Describe(IEnumerable<GeneratedMap17ExitAuditFailure> failures) =>
            string.Join(";", (failures ?? Array.Empty<GeneratedMap17ExitAuditFailure>())
                .Select(value => value.ToString()));
    }
}
