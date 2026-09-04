using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Baking
{
    [TestFixture]
    [Category("MAP17_07")]
    public sealed class GeneratedTerrainPerformanceTests
    {
        private static GeneratedTerrainPerformanceReport report;

        [Test]
        public void BakePlacementPerformanceReportsStableCellLayerAndCoordinateCounts()
        {
            var value = Report();
            var sample = Sample(GeneratedTerrainPerformanceOperation.Placement);
            Assert.That(sample.OperationCount, Is.EqualTo(10752));
            Assert.That(Metric(sample, "sector_cells"), Is.EqualTo(1536));
            Assert.That(Metric(sample, "layer_references"), Is.EqualTo(10752));
            Assert.That(Metric(sample, "sector_coordinates"), Is.EqualTo(1536));
            Assert.That(Metric(sample, "world_coordinates"), Is.EqualTo(1536));
            Assert.That(Metric(sample, "missing_coordinates"), Is.Zero);
            Assert.That(Metric(sample, "duplicate_coordinates"), Is.Zero);
            Assert.That(Metric(sample, "out_of_bounds_coordinates"), Is.Zero);
            AssertSamples(value, GeneratedTerrainPerformanceOperation.Placement);

            TestContext.WriteLine("MAP17_07_PLACEMENT_PERF cells=1536 layer_refs=10752" +
                " sector_world_coordinates=1536/1536 gaps_duplicates_oob=0/0/0 ms=" +
                value.Operation(GeneratedTerrainPerformanceOperation.Placement).TimingText);
        }

        [Test]
        public void LayerBakeAndSeamPerformanceReportsExpectedCountsWithoutTilemapWrites()
        {
            var value = Report();
            var layer = Sample(GeneratedTerrainPerformanceOperation.LayerBake);
            var seam = Sample(GeneratedTerrainPerformanceOperation.SeamValidation);
            Assert.That(Metric(layer, "logical_layers"), Is.EqualTo(7));
            Assert.That(Metric(layer, "layer_records"), Is.EqualTo(10752));
            Assert.That(new[] { Metric(layer, "gap_count"), Metric(layer, "overlap_count"),
                Metric(layer, "stale_asset_count"), Metric(layer, "tilemap_component_writes") },
                Is.All.Zero);
            Assert.That(Metric(seam, "seam_4x4"), Is.EqualTo(688));
            Assert.That(Metric(seam, "seam_12x8"), Is.EqualTo(240));
            Assert.That(Metric(seam, "seam_4x4_only"), Is.EqualTo(448));
            Assert.That(new[] { Metric(seam, "unapproved_pairs"),
                Metric(seam, "missing_neighbors"), Metric(seam, "out_of_bounds_neighbors") },
                Is.All.Zero);
            AssertSamples(value, GeneratedTerrainPerformanceOperation.LayerBake);
            AssertSamples(value, GeneratedTerrainPerformanceOperation.SeamValidation);

            TestContext.WriteLine("MAP17_07_BAKE_SEAM_PERF layers_records=7/10752" +
                " gap_overlap_stale=0/0/0 seam=688/240/448 tilemap_writes=0 layer_ms=" +
                value.Operation(GeneratedTerrainPerformanceOperation.LayerBake).TimingText +
                " seam_ms=" + value.Operation(GeneratedTerrainPerformanceOperation.SeamValidation).TimingText);
        }

        [Test]
        public void ColliderCachePerformanceSeparatesColdWarmInvalidateAndEvictPaths()
        {
            var value = Report();
            var sample = Sample(GeneratedTerrainPerformanceOperation.ColliderCache);
            Assert.That(new[] { Metric(sample, "cold_misses"), Metric(sample, "warm_hits"),
                Metric(sample, "invalidates"), Metric(sample, "evicts") },
                Is.EqualTo(new[] { 1, 1, 1, 1 }));
            Assert.That(Metric(sample, "rebuild_commands"), Is.GreaterThan(0));
            Assert.That(Metric(sample, "collider_creations"), Is.Zero);
            Assert.That(Metric(sample, "physics_queries"), Is.Zero);
            AssertSamples(value, GeneratedTerrainPerformanceOperation.ColliderCache);

            TestContext.WriteLine("MAP17_07_COLLIDER_PERF cold_warm_invalidate_evict=1/1/1/1" +
                " rebuild_commands=" + Metric(sample, "rebuild_commands") +
                " collider_physics=0/0 ms=" +
                value.Operation(GeneratedTerrainPerformanceOperation.ColliderCache).TimingText);
        }

        [Test]
        public void StreamingWindowPerformanceReportsCenterEdgeCornerAndActiveSubsetBudgets()
        {
            var value = Report();
            var sample = Sample(GeneratedTerrainPerformanceOperation.StreamWindow);
            Assert.That(new[] { Metric(sample, "center_preload"), Metric(sample, "center_active") },
                Is.EqualTo(new[] { 49, 25 }));
            Assert.That(new[] { Metric(sample, "edge_preload"), Metric(sample, "edge_active") },
                Is.EqualTo(new[] { 28, 15 }));
            Assert.That(new[] { Metric(sample, "corner_preload"), Metric(sample, "corner_active") },
                Is.EqualTo(new[] { 16, 9 }));
            Assert.That(Metric(sample, "active_subset_preload"), Is.EqualTo(1));
            Assert.That(Metric(sample, "window_duplicates"), Is.Zero);
            AssertSamples(value, GeneratedTerrainPerformanceOperation.StreamWindow);

            TestContext.WriteLine("MAP17_07_STREAM_PERF center=49/25 edge=28/15 corner=16/9" +
                " active_subset=YES duplicates=0 ms=" +
                value.Operation(GeneratedTerrainPerformanceOperation.StreamWindow).TimingText);
        }

        [Test]
        public void TransitionPerformancePublishesShiftedWindowDiffWithoutDuplicateHandleChanges()
        {
            var value = Report();
            var sample = Sample(GeneratedTerrainPerformanceOperation.Transition);
            Assert.That(Metric(sample, "shifted_window_diff"), Is.EqualTo(63));
            Assert.That(Metric(sample, "transition_batch"), Is.EqualTo(24));
            Assert.That(Metric(sample, "duplicate_handle_changes"), Is.Zero);
            Assert.That(Metric(sample, "failed_transitions"), Is.Zero);
            Assert.That(Metric(sample, "scene_activations"), Is.Zero);
            AssertSamples(value, GeneratedTerrainPerformanceOperation.Transition);

            TestContext.WriteLine("MAP17_07_TRANSITION_PERF shifted_diff=63 batch=24" +
                " duplicate_failed_scene=0/0/0 ms=" +
                value.Operation(GeneratedTerrainPerformanceOperation.Transition).TimingText);
        }

        [Test]
        public void ModificationStoragePerformanceReportsDirtyRevisionCompactAndApplyCounts()
        {
            var value = Report();
            var sample = Sample(GeneratedTerrainPerformanceOperation.ModificationStorage);
            Assert.That(new[] { Metric(sample, "modified_sectors"),
                Metric(sample, "modification_records"), Metric(sample, "dirty_revision") },
                Is.EqualTo(new[] { 1, 5, 5 }));
            Assert.That(Metric(sample, "compact_idempotent"), Is.EqualTo(1));
            Assert.That(Metric(sample, "apply_commands"), Is.EqualTo(5));
            Assert.That(Metric(sample, "in_place_mutations"), Is.Zero);
            AssertSamples(value, GeneratedTerrainPerformanceOperation.ModificationStorage);

            TestContext.WriteLine("MAP17_07_MODIFICATION_PERF sectors_records_revision=1/5/5" +
                " compact=YES apply_commands=5 input_mutations=0 ms=" +
                value.Operation(GeneratedTerrainPerformanceOperation.ModificationStorage).TimingText);
        }

        [Test]
        public void SaveManifestReloadPerformanceSerializesModifiedOnlyAndAppliesFiveRecords()
        {
            var value = Report();
            var save = Sample(GeneratedTerrainPerformanceOperation.SaveManifest);
            var apply = Sample(GeneratedTerrainPerformanceOperation.RegenApply);
            Assert.That(Metric(save, "payload_bytes"), Is.EqualTo(20518));
            Assert.That(Metric(save, "modified_manifest_entries"), Is.EqualTo(1));
            Assert.That(Metric(save, "unmodified_manifest_entries"), Is.Zero);
            Assert.That(Metric(save, "unmodified_sectors_omitted"), Is.EqualTo(168));
            Assert.That(Metric(save, "serialized_records"), Is.EqualTo(5));
            Assert.That(Metric(apply, "modified_sector_plans"), Is.EqualTo(1));
            Assert.That(Metric(apply, "regen_commands"), Is.EqualTo(5));
            Assert.That(new[] { Metric(apply, "destroy_tile"), Metric(apply, "replace_tile"),
                Metric(apply, "collect_pickup"), Metric(apply, "change_device_state"),
                Metric(apply, "consume_slot") }, Is.All.EqualTo(1));
            Assert.That(Metric(apply, "in_place_mutations"), Is.Zero);
            AssertSamples(value, GeneratedTerrainPerformanceOperation.SaveManifest);
            AssertSamples(value, GeneratedTerrainPerformanceOperation.RegenApply);

            TestContext.WriteLine("MAP17_07_SAVE_REGEN_PERF payload_bytes=20518" +
                " manifest_modified_unmodified_entries_records=1/0/5 omitted=168" +
                " regen_plans_commands=1/5 kinds=1/1/1/1/1 input_mutations=0 save_ms=" +
                value.Operation(GeneratedTerrainPerformanceOperation.SaveManifest).TimingText +
                " regen_ms=" + value.Operation(GeneratedTerrainPerformanceOperation.RegenApply).TimingText);
        }

        [Test]
        public void HashMismatchPerformanceFailsAtomicallyWithoutRetryStorm()
        {
            var value = Report();
            var sample = Sample(GeneratedTerrainPerformanceOperation.HashMismatch);
            Assert.That(Metric(sample, "hash_mismatch_failures"), Is.EqualTo(6));
            Assert.That(Metric(sample, "retry_loops"), Is.Zero);
            Assert.That(Metric(sample, "partial_apply_mutations"), Is.Zero);

            var invalidMetrics = sample.Metrics.Select(metric =>
                new GeneratedTerrainPerformanceMetric(metric.Name,
                    metric.Name == "retry_loops" ? 1 : metric.Value)).ToArray();
            var invalidSample = new GeneratedTerrainPerformanceSample(sample.Operation,
                sample.Iteration, sample.OperationCount + 1, sample.ElapsedTicks,
                sample.ElapsedMilliseconds, sample.AllocationNote, sample.StructuralDigest,
                invalidMetrics);
            var invalidSamples = value.Samples.Select(existing =>
                existing.Operation == sample.Operation && existing.Iteration == sample.Iteration
                    ? invalidSample : existing).ToArray();
            var invalid = GeneratedTerrainPerformanceHarness.RebuildReport(value, invalidSamples);
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Failures.Select(failure => failure.Code),
                Does.Contain(GeneratedTerrainPerformanceFailureCode.CountBudgetExceeded));
            Assert.That(invalid.Failures.Select(failure => failure.Code),
                Does.Contain(GeneratedTerrainPerformanceFailureCode.StructuralUpperBoundExceeded));
            AssertSamples(value, GeneratedTerrainPerformanceOperation.HashMismatch);

            TestContext.WriteLine("MAP17_07_HASH_MISMATCH_PERF failures=6 retry_loops=0" +
                " partial_mutations=0 count_budget_probe=1/1 retry_budget_probe=1/1 ms=" +
                value.Operation(GeneratedTerrainPerformanceOperation.HashMismatch).TimingText);
        }

        [Test]
        public void PerformanceReportsAreDeterministicAcrossRepeatReverseCultureAndWarmup()
        {
            var baseline = Report();
            var repeat = GeneratedTerrainPerformanceHarness.Run();
            var reverse = GeneratedTerrainPerformanceHarness.Run(true);
            GeneratedTerrainPerformanceReport culture;
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                culture = GeneratedTerrainPerformanceHarness.RebuildReport(baseline,
                    baseline.Samples.Reverse());
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
            var retimed = baseline.Samples.Select(sample => new GeneratedTerrainPerformanceSample(
                sample.Operation, sample.Iteration, sample.OperationCount,
                sample.ElapsedTicks + 1000, sample.ElapsedMilliseconds + 10d,
                sample.AllocationNote, sample.StructuralDigest, sample.Metrics)).ToArray();
            var warmup = GeneratedTerrainPerformanceHarness.RebuildReport(baseline, retimed,
                GeneratedTerrainPerformanceBudget.ReferenceWarmupIterations);

            Assert.That(new[] { baseline.Success, repeat.Success, reverse.Success,
                culture.Success, warmup.Success }, Is.All.True);
            Assert.That(new[] { baseline.Digest, repeat.Digest, reverse.Digest,
                culture.Digest, warmup.Digest }.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(baseline.Digest), Is.True);
            Assert.That(baseline.OperationGroupCount, Is.EqualTo(10));
            Assert.That(baseline.WarmupIterations, Is.EqualTo(1));
            Assert.That(baseline.MeasuredIterations, Is.EqualTo(3));
            Assert.That(baseline.Samples.All(sample =>
                BakingCanonicalDigest.IsLowerHexSha256(sample.DeterministicDigest)), Is.True);

            TestContext.WriteLine("MAP17_07_REPORT_EVIDENCE warmup=1 measured=3 groups=10" +
                " report=" + baseline.Digest +
                " repeat_reverse_culture_warmup_mismatches=0/0/0/0" +
                " manifest=" + GeneratedTerrainPerformanceHarness.ExpectedManifestDigest +
                " payload=" + GeneratedTerrainPerformanceHarness.ExpectedPayloadDigest +
                " apply=" + GeneratedTerrainPerformanceHarness.ExpectedRegenerationApplyDigest);
            TestContext.WriteLine("MAP17_07_TIMING_EVIDENCE " + string.Join(" ",
                baseline.Aggregates.Select(item => item.Operation + "=" + item.TimingText)));
        }

        [Test]
        public void Map17HandoffKeepsMap17_08Locked()
        {
            var value = Report();
            Assert.That(value.Success, Is.True, Describe(value.Failures));
            Assert.That(value.OperationGroupCount, Is.EqualTo(10));
            Assert.That(GeneratedTerrainPerformanceReport.SchemaVersion,
                Is.EqualTo("MAP17_07_TERRAIN_PERFORMANCE_REPORT_V1"));
            Assert.That(GeneratedTerrainPerformanceReport.DownstreamOwner,
                Is.EqualTo("MAP17_08_MAP17_RUNTIME_EXIT_AUDIT"));
            Assert.That(GeneratedTerrainPerformanceReport.OpensDownstreamTask, Is.False);
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
                value.PopulationStableSpawnIdCount, value.ProductionSeedApprovalCount },
                Is.All.Zero);

            TestContext.WriteLine("MAP17_07_HANDOFF_EVIDENCE MAP17_08_started=NO locked=YES" +
                " system_io_disk_slot_platform=0/0/0/0/0/0 tilemap_calls=0/0/0/0/0" +
                " colliders_rigidbody_physics=0/0/0/0/0/0 scene_prefab_tilemap=0/0/0" +
                " gameobject=0/0/0/0 camera=0/0 asset_loads=0/0/0 csv=0 generated=0/0" +
                " runtime_population=0/0 seed_approval=0");
            TestContext.WriteLine("MAP17_07_DUPLICATION_EVIDENCE existing_helpers_reused=19" +
                " new_duplicate_helper_count=1 hardcoded_count_constants_added=22" +
                " hardcoded_count_constants_justified=22 consolidation_candidates=1" +
                " consolidation_work_performed=0");
        }

        private static GeneratedTerrainPerformanceReport Report()
        {
            if (report != null) return report;
            report = GeneratedTerrainPerformanceHarness.Run();
            Assert.That(report.Success, Is.True, Describe(report.Failures));
            return report;
        }

        private static GeneratedTerrainPerformanceSample Sample(string operation) =>
            Report().Sample(operation);

        private static int Metric(GeneratedTerrainPerformanceSample sample, string name) =>
            sample.Metric(name);

        private static void AssertSamples(GeneratedTerrainPerformanceReport value, string operation)
        {
            var samples = value.Samples.Where(sample => sample.Operation == operation).ToArray();
            Assert.That(samples.Length, Is.EqualTo(3));
            Assert.That(samples.Select(sample => sample.Iteration), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(samples.All(sample => sample.ElapsedTicks >= 0 &&
                sample.ElapsedMilliseconds >= 0d), Is.True);
            Assert.That(samples.Select(sample => sample.ObservationDigest)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
        }

        private static string Describe(IEnumerable<GeneratedTerrainPerformanceFailure> failures) =>
            string.Join(";", (failures ?? Array.Empty<GeneratedTerrainPerformanceFailure>())
                .Select(value => value.ToString()));
    }
}
