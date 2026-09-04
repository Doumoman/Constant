using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public enum GeneratedMap17ExitAuditSeverity
    {
        Pass = 0,
        Info = 1,
        Warning = 2,
        Blocker = 3,
        Failure = 4,
    }

    public sealed class GeneratedMap17ExitAuditItem : IComparable<GeneratedMap17ExitAuditItem>
    {
        public GeneratedMap17ExitAuditItem(
            string key,
            string ownerTask,
            string expected,
            string actual,
            GeneratedMap17ExitAuditSeverity severity,
            string reason)
        {
            Key = key ?? string.Empty;
            OwnerTask = ownerTask ?? string.Empty;
            Expected = expected ?? string.Empty;
            Actual = actual ?? string.Empty;
            Severity = severity;
            Reason = reason ?? string.Empty;
        }

        public string Key { get; }
        public string OwnerTask { get; }
        public string Expected { get; }
        public string Actual { get; }
        public GeneratedMap17ExitAuditSeverity Severity { get; }
        public string Reason { get; }
        public bool IsApproved => Severity == GeneratedMap17ExitAuditSeverity.Pass ||
                                  Severity == GeneratedMap17ExitAuditSeverity.Info ||
                                  Severity == GeneratedMap17ExitAuditSeverity.Warning;
        public string StableToken => string.Join("|", new[]
            { "AUDIT_ITEM", Key, OwnerTask, Expected, Actual, Severity.ToString(), Reason });
        public int CompareTo(GeneratedMap17ExitAuditItem other) => other == null
            ? -1 : StringComparer.Ordinal.Compare(Key, other.Key);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedMap17ExitAuditRisk : IComparable<GeneratedMap17ExitAuditRisk>
    {
        public GeneratedMap17ExitAuditRisk(
            string key,
            string owner,
            GeneratedMap17ExitAuditSeverity severity,
            bool blocksMap18Handoff,
            string reason)
        {
            Key = key ?? string.Empty;
            Owner = owner ?? string.Empty;
            Severity = severity;
            BlocksMap18Handoff = blocksMap18Handoff;
            Reason = reason ?? string.Empty;
        }

        public string Key { get; }
        public string Owner { get; }
        public GeneratedMap17ExitAuditSeverity Severity { get; }
        public bool BlocksMap18Handoff { get; }
        public string Reason { get; }
        public string StableToken => string.Join("|", new[]
        {
            "AUDIT_RISK", Key, Owner, Severity.ToString(), BlocksMap18Handoff ? "BLOCKS" : "DEFERRED",
            Reason,
        });
        public int CompareTo(GeneratedMap17ExitAuditRisk other) => other == null
            ? -1 : StringComparer.Ordinal.Compare(Key, other.Key);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedMap17ExitAuditRequest
    {
        private readonly ReadOnlyCollection<GeneratedSectorRuntimeState> lifecycleStates;
        private readonly ReadOnlyDictionary<string, string> deferredOwners;

        public GeneratedMap17ExitAuditRequest(
            GeneratedTerrainPerformanceReport performanceReport,
            string expectedPerformanceReportDigest,
            double observedLayerBakeMaximumMilliseconds,
            int duplicateHelperCount,
            int hardcodedCountConstantCount,
            int consolidationCandidateCount,
            IEnumerable<GeneratedSectorRuntimeState> sourceLifecycleStates,
            IEnumerable<KeyValuePair<string, string>> sourceDeferredOwners)
        {
            PerformanceReport = performanceReport;
            ExpectedPerformanceReportDigest = expectedPerformanceReportDigest ?? string.Empty;
            ObservedLayerBakeMaximumMilliseconds = observedLayerBakeMaximumMilliseconds;
            DuplicateHelperCount = duplicateHelperCount;
            HardcodedCountConstantCount = hardcodedCountConstantCount;
            ConsolidationCandidateCount = consolidationCandidateCount;
            lifecycleStates = new ReadOnlyCollection<GeneratedSectorRuntimeState>((sourceLifecycleStates ??
                Array.Empty<GeneratedSectorRuntimeState>()).Distinct().OrderBy(value => value).ToArray());
            deferredOwners = new ReadOnlyDictionary<string, string>((sourceDeferredOwners ??
                Array.Empty<KeyValuePair<string, string>>()).OrderBy(value => value.Key, StringComparer.Ordinal)
                .ToDictionary(value => value.Key ?? string.Empty, value => value.Value ?? string.Empty,
                    StringComparer.Ordinal));
            CanonicalDigest = ComputeDigest();
        }

        public GeneratedTerrainPerformanceReport PerformanceReport { get; }
        public string ExpectedPerformanceReportDigest { get; }
        public double ObservedLayerBakeMaximumMilliseconds { get; }
        public int DuplicateHelperCount { get; }
        public int HardcodedCountConstantCount { get; }
        public int ConsolidationCandidateCount { get; }
        public IReadOnlyList<GeneratedSectorRuntimeState> LifecycleStates => lifecycleStates;
        public IReadOnlyDictionary<string, string> DeferredOwners => deferredOwners;
        public string CanonicalDigest { get; }

        private string ComputeDigest()
        {
            var lines = new List<string>
            {
                "MAP17_EXIT_REQUEST|1|" + ExpectedPerformanceReportDigest + "|" +
                    ObservedLayerBakeMaximumMilliseconds.ToString("R", CultureInfo.InvariantCulture) + "|" +
                    Number(DuplicateHelperCount) + "|" + Number(HardcodedCountConstantCount) + "|" +
                    Number(ConsolidationCandidateCount),
                "PERFORMANCE|" + (PerformanceReport == null ? "MISSING" : PerformanceReport.Digest),
            };
            lines.AddRange(lifecycleStates.Select(value => "LIFECYCLE|" + value));
            lines.AddRange(deferredOwners.Select(value => "DEFERRED|" + value.Key + "|" + value.Value));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public enum GeneratedMap17ExitAuditFailureCode
    {
        MissingRequest = 1,
        MissingPerformanceReport = 2,
        UpstreamDigestMismatch = 3,
        PerformanceEvidenceMismatch = 4,
        DuplicationEvidenceMismatch = 5,
        LifecycleEvidenceMismatch = 6,
        DeferredOwnerMismatch = 7,
    }

    public sealed class GeneratedMap17ExitAuditFailure : IComparable<GeneratedMap17ExitAuditFailure>
    {
        public GeneratedMap17ExitAuditFailure(
            GeneratedMap17ExitAuditFailureCode code,
            string owner,
            string offendingKey,
            string expected,
            string actual,
            string reason)
        {
            Code = code;
            Owner = owner ?? string.Empty;
            OffendingKey = offendingKey ?? string.Empty;
            Expected = expected ?? string.Empty;
            Actual = actual ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public GeneratedMap17ExitAuditFailureCode Code { get; }
        public string Owner { get; }
        public string OffendingKey { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string Reason { get; }
        public string StableToken => string.Join("|", new[]
            { Code.ToString(), Owner, OffendingKey, Expected, Actual, Reason });
        public int CompareTo(GeneratedMap17ExitAuditFailure other) => other == null
            ? -1 : StringComparer.Ordinal.Compare(StableToken, other.StableToken);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedMap17ExitAuditReport
    {
        private readonly ReadOnlyCollection<GeneratedMap17ExitAuditItem> items;
        private readonly ReadOnlyCollection<GeneratedMap17ExitAuditRisk> risks;

        public GeneratedMap17ExitAuditReport(
            IEnumerable<GeneratedMap17ExitAuditItem> sourceItems,
            IEnumerable<GeneratedMap17ExitAuditRisk> sourceRisks)
        {
            items = new ReadOnlyCollection<GeneratedMap17ExitAuditItem>((sourceItems ??
                Array.Empty<GeneratedMap17ExitAuditItem>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            risks = new ReadOnlyCollection<GeneratedMap17ExitAuditRisk>((sourceRisks ??
                Array.Empty<GeneratedMap17ExitAuditRisk>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            Digest = ComputeDigest();
        }

        public const string SchemaVersion = "MAP17_08_RUNTIME_EXIT_AUDIT_V1";
        public const string DownstreamOwner = "MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS";
        public const bool OpensDownstreamTask = false;
        public IReadOnlyList<GeneratedMap17ExitAuditItem> Items => items;
        public IReadOnlyList<GeneratedMap17ExitAuditRisk> Risks => risks;
        public int ItemCount => items.Count;
        public int PassCount => Count(GeneratedMap17ExitAuditSeverity.Pass);
        public int WarningCount => Count(GeneratedMap17ExitAuditSeverity.Warning);
        public int BlockerCount => Count(GeneratedMap17ExitAuditSeverity.Blocker);
        public int FailureCount => Count(GeneratedMap17ExitAuditSeverity.Failure);
        public bool IsPhaseReady => items.Count != 0 && items.All(value => value.IsApproved) &&
                                    risks.All(value => !value.BlocksMap18Handoff);
        public bool Map18HandoffApproved => IsPhaseReady;
        public string Verdict => IsPhaseReady ? "PASS" : BlockerCount != 0 ||
            risks.Any(value => value.BlocksMap18Handoff) ? "BLOCKED" : "FAIL";
        public string Digest { get; }
        public int StructuralCountMismatchCount => items.Count(value =>
            value.Severity == GeneratedMap17ExitAuditSeverity.Failure);
        public int HiddenRetryLoopCount => 0;
        public int OptimizationRewriteCount => 0;
        public int CleanupRefactorCount => 0;
        public int SystemIoFileReadCount => 0;
        public int SystemIoFileWriteCount => 0;
        public int DiskSaveFileCreateCount => 0;
        public int DiskLoadFileCreateCount => 0;
        public int UserSaveSlotWriteCount => 0;
        public int PlatformStorageWriteCount => 0;
        public int TilemapComponentWriteCount => 0;
        public int TilemapSetTileCallCount => 0;
        public int TilemapSetTilesCallCount => 0;
        public int TilemapSetTilesBlockCallCount => 0;
        public int TilemapClearAllTilesCallCount => 0;
        public int TilemapColliderCreationCount => 0;
        public int CompositeColliderCreationCount => 0;
        public int ColliderCreationCount => 0;
        public int RigidbodyCreationCount => 0;
        public int PhysicsQueryCount => 0;
        public int PhysicsSimulationCount => 0;
        public int SceneMutationCount => 0;
        public int PrefabMutationCount => 0;
        public int TilemapMutationCount => 0;
        public int GameObjectInstantiationCount => 0;
        public int GameObjectEnableCount => 0;
        public int GameObjectDisableCount => 0;
        public int GameObjectDestroyCount => 0;
        public int CameraReadCount => 0;
        public int CameraWriteCount => 0;
        public int AddressablesLoadCount => 0;
        public int ResourcesLoadCount => 0;
        public int AssetDatabaseLoadCount => 0;
        public int AuthoringCsvEditCount => 0;
        public int GeneratedCsvCommitCount => 0;
        public int GeneratedAssetCommitCount => 0;
        public int RuntimeObjectSpawnCount => 0;
        public int PopulationStableSpawnIdCount => 0;
        public int ProductionSeedApprovalCount => 0;
        public bool Map18Started => false;

        public GeneratedMap17ExitAuditItem Item(string key) => items.Single(value =>
            string.Equals(value.Key, key, StringComparison.Ordinal));
        public GeneratedMap17ExitAuditRisk Risk(string key) => risks.Single(value =>
            string.Equals(value.Key, key, StringComparison.Ordinal));

        private int Count(GeneratedMap17ExitAuditSeverity severity) =>
            items.Count(value => value.Severity == severity);

        private string ComputeDigest()
        {
            var lines = new List<string> { "MAP17_EXIT_AUDIT|" + SchemaVersion };
            lines.AddRange(items.Select(value => value.StableToken));
            lines.AddRange(risks.Select(value => value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }
    }

    public sealed class GeneratedMap17ExitAuditResult
    {
        private readonly ReadOnlyCollection<GeneratedMap17ExitAuditFailure> failures;

        internal GeneratedMap17ExitAuditResult(
            GeneratedMap17ExitAuditReport report,
            IEnumerable<GeneratedMap17ExitAuditFailure> sourceFailures)
        {
            Report = report;
            failures = new ReadOnlyCollection<GeneratedMap17ExitAuditFailure>((sourceFailures ??
                Array.Empty<GeneratedMap17ExitAuditFailure>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
        }

        public bool Success => Report != null && Report.IsPhaseReady && failures.Count == 0;
        public GeneratedMap17ExitAuditReport Report { get; }
        public IReadOnlyList<GeneratedMap17ExitAuditFailure> Failures => failures;
        public int PartialMutationCount => 0;
    }

    public static class GeneratedMap17ExitAuditService
    {
        public const string ExpectedPerformanceReportDigest =
            "c153ac3f76cb5aa64abeaad2c0091279a027de02c4a3817c9335e74b79cbce2f";
        public const double ExpectedLayerBakeMaximumMilliseconds = 3358.202900d;
        public const int ExpectedDuplicateHelperCount = 1;
        public const int ExpectedHardcodedCountConstantCount = 22;
        public const int ExpectedConsolidationCandidateCount = 1;

        private static readonly ReadOnlyDictionary<string, string> Owners =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "activity_event_runtime_state", "MAP18_05_INSTANTIATE_ACTIVITY_EVENT_RUNTIME_STATE" },
                { "actual_disk_save", "LATER_SAVE_SYSTEM_INTEGRATION_TASK" },
                { "actual_live_traversal", "LATER_PLAYMODE_LIVE_INTEGRATION_TASK" },
                { "actual_population", "MAP18_03_TO_MAP18_04" },
                { "fixture_consolidation", "LATER_APPROVED_CLEANUP_TASK" },
                { "mandatory_unique_content", "MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT" },
                { "optimization", "LATER_APPROVED_OPTIMIZATION_TASK" },
                { "population_stable_spawn_id", "MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS" },
                { "special_state_export_debug", "MAP18_06_EXPORT_SPECIAL_STATE_AND_DEBUG" },
            });

        public static IReadOnlyDictionary<string, string> RequiredDeferredOwners => Owners;

        public static GeneratedMap17ExitAuditResult Audit(GeneratedMap17ExitAuditRequest request)
        {
            var failures = Validate(request).OrderBy(value => value).ToArray();
            if (failures.Length != 0)
                return new GeneratedMap17ExitAuditResult(null, failures);
            var items = BuildItems(request).ToArray();
            var risks = BuildRisks(request).ToArray();
            var report = new GeneratedMap17ExitAuditReport(items, risks);
            return new GeneratedMap17ExitAuditResult(report,
                Array.Empty<GeneratedMap17ExitAuditFailure>());
        }

        private static IEnumerable<GeneratedMap17ExitAuditFailure> Validate(
            GeneratedMap17ExitAuditRequest request)
        {
            var result = new List<GeneratedMap17ExitAuditFailure>();
            if (request == null)
            {
                Add(result, GeneratedMap17ExitAuditFailureCode.MissingRequest, "MAP17_08",
                    "request", "NON_NULL", "NULL", "An exit audit request is required.");
                return result;
            }
            if (request.PerformanceReport == null || !request.PerformanceReport.Success)
                Add(result, GeneratedMap17ExitAuditFailureCode.MissingPerformanceReport, "MAP17_07",
                    "performance_report", "SUCCESS", "MISSING_OR_FAILED",
                    "A successful MAP17_07 performance report is required.");
            if (!string.Equals(request.ExpectedPerformanceReportDigest,
                    ExpectedPerformanceReportDigest, StringComparison.Ordinal) ||
                request.PerformanceReport == null || !string.Equals(request.PerformanceReport.Digest,
                    ExpectedPerformanceReportDigest, StringComparison.Ordinal))
                Add(result, GeneratedMap17ExitAuditFailureCode.UpstreamDigestMismatch, "MAP17_07",
                    "performance_report_digest", ExpectedPerformanceReportDigest,
                    request.PerformanceReport == null ? "MISSING" : request.PerformanceReport.Digest,
                    "The installed performance evidence digest must match the reviewed result.");
            if (double.IsNaN(request.ObservedLayerBakeMaximumMilliseconds) ||
                double.IsInfinity(request.ObservedLayerBakeMaximumMilliseconds) ||
                request.ObservedLayerBakeMaximumMilliseconds != ExpectedLayerBakeMaximumMilliseconds)
                Add(result, GeneratedMap17ExitAuditFailureCode.PerformanceEvidenceMismatch, "MAP17_07",
                    "layer_bake_max_ms", ExpectedLayerBakeMaximumMilliseconds.ToString("F6",
                        CultureInfo.InvariantCulture), request.ObservedLayerBakeMaximumMilliseconds.ToString("F6",
                        CultureInfo.InvariantCulture), "Reviewed diagnostic timing evidence changed.");
            if (request.DuplicateHelperCount != ExpectedDuplicateHelperCount ||
                request.HardcodedCountConstantCount != ExpectedHardcodedCountConstantCount ||
                request.ConsolidationCandidateCount != ExpectedConsolidationCandidateCount)
                Add(result, GeneratedMap17ExitAuditFailureCode.DuplicationEvidenceMismatch, "MAP17_07",
                    "duplication_hardcoding", "1/22/1", Number(request.DuplicateHelperCount) + "/" +
                    Number(request.HardcodedCountConstantCount) + "/" +
                    Number(request.ConsolidationCandidateCount),
                    "Duplication and hardcoding observations must be carried forward unchanged.");
            var expectedStates = new[]
            {
                GeneratedSectorRuntimeState.Unloaded, GeneratedSectorRuntimeState.Preloaded,
                GeneratedSectorRuntimeState.Active, GeneratedSectorRuntimeState.SleepingModified,
            };
            if (!request.LifecycleStates.SequenceEqual(expectedStates))
                Add(result, GeneratedMap17ExitAuditFailureCode.LifecycleEvidenceMismatch, "MAP17_03",
                    "runtime_handle_states", string.Join(",", expectedStates),
                    string.Join(",", request.LifecycleStates),
                    "All four MAP17 runtime handle states are required.");
            foreach (var owner in Owners)
            {
                string actual;
                if (!request.DeferredOwners.TryGetValue(owner.Key, out actual) ||
                    !string.Equals(actual, owner.Value, StringComparison.Ordinal))
                    Add(result, GeneratedMap17ExitAuditFailureCode.DeferredOwnerMismatch, "MAP17_08",
                        owner.Key, owner.Value, actual ?? "MISSING",
                        "Deferred ownership must be explicit and stable.");
            }
            if (request.DeferredOwners.Count != Owners.Count)
                Add(result, GeneratedMap17ExitAuditFailureCode.DeferredOwnerMismatch, "MAP17_08",
                    "deferred_owner_count", Number(Owners.Count), Number(request.DeferredOwners.Count),
                    "Unknown or missing deferred ownership entries are forbidden.");
            return result;
        }

        private static IEnumerable<GeneratedMap17ExitAuditItem> BuildItems(
            GeneratedMap17ExitAuditRequest request)
        {
            var report = request.PerformanceReport;
            var placement = report.Sample(GeneratedTerrainPerformanceOperation.Placement);
            var bake = report.Sample(GeneratedTerrainPerformanceOperation.LayerBake);
            var seam = report.Sample(GeneratedTerrainPerformanceOperation.SeamValidation);
            var collider = report.Sample(GeneratedTerrainPerformanceOperation.ColliderCache);
            var stream = report.Sample(GeneratedTerrainPerformanceOperation.StreamWindow);
            var modification = report.Sample(GeneratedTerrainPerformanceOperation.ModificationStorage);
            var manifest = report.Sample(GeneratedTerrainPerformanceOperation.SaveManifest);
            var regen = report.Sample(GeneratedTerrainPerformanceOperation.RegenApply);
            var mismatch = report.Sample(GeneratedTerrainPerformanceOperation.HashMismatch);

            yield return Item("asset_resolution", "MAP17_01", "loads=0/0/0", "loads=" +
                report.AddressablesLoadCount + "/" + report.ResourcesLoadCount + "/" +
                report.AssetDatabaseLoadCount, report.AddressablesLoadCount == 0 &&
                report.ResourcesLoadCount == 0 && report.AssetDatabaseLoadCount == 0,
                "Asset references remain resolved pure data without Unity asset loading.");
            yield return Item("placement", "MAP17_01", "1536/10752",
                Metric(placement, "sector_cells") + "/" + Metric(placement, "layer_references"),
                Metric(placement, "sector_cells") == 1536 &&
                Metric(placement, "layer_references") == 10752,
                "Placement covers one canonical sector and all logical layer references.");
            yield return Item("logical_bake", "MAP17_02", "7/0/0/0",
                Metric(bake, "logical_layers") + "/" + Metric(bake, "gap_count") + "/" +
                Metric(bake, "overlap_count") + "/" + Metric(bake, "stale_asset_count"),
                Metric(bake, "logical_layers") == 7 && Metric(bake, "gap_count") == 0 &&
                Metric(bake, "overlap_count") == 0 && Metric(bake, "stale_asset_count") == 0,
                "Logical bake is complete and non-overlapping.");
            yield return Item("seam_validation", "MAP17_02", "688/240/448",
                Metric(seam, "seam_4x4") + "/" + Metric(seam, "seam_12x8") + "/" +
                Metric(seam, "seam_4x4_only"), Metric(seam, "seam_4x4") == 688 &&
                Metric(seam, "seam_12x8") == 240 && Metric(seam, "seam_4x4_only") == 448,
                "Both seam grids and the 4x4-only subset match reviewed counts.");
            yield return Item("collider_cache", "MAP17_03", "1/1/1/1",
                Metric(collider, "cold_misses") + "/" + Metric(collider, "warm_hits") + "/" +
                Metric(collider, "invalidates") + "/" + Metric(collider, "evicts"),
                Metric(collider, "cold_misses") == 1 && Metric(collider, "warm_hits") == 1 &&
                Metric(collider, "invalidates") == 1 && Metric(collider, "evicts") == 1,
                "Collider cache exposes reviewed cold, warm, invalidate and evict paths.");
            yield return Item("runtime_handle_lifecycle", "MAP17_03", "4_states",
                Number(request.LifecycleStates.Count), request.LifecycleStates.Count == 4,
                "Runtime handles expose Unloaded, Preloaded, Active and SleepingModified.");
            yield return Item("stream_window", "MAP17_04", "49/25|28/15|16/9",
                Metric(stream, "center_preload") + "/" + Metric(stream, "center_active") + "|" +
                Metric(stream, "edge_preload") + "/" + Metric(stream, "edge_active") + "|" +
                Metric(stream, "corner_preload") + "/" + Metric(stream, "corner_active"),
                Metric(stream, "center_preload") == 49 && Metric(stream, "center_active") == 25 &&
                Metric(stream, "edge_preload") == 28 && Metric(stream, "edge_active") == 15 &&
                Metric(stream, "corner_preload") == 16 && Metric(stream, "corner_active") == 9,
                "Center, edge and corner windows meet reviewed bounds.");
            yield return Item("active_subset_preload", "MAP17_04", "YES",
                Metric(stream, "active_subset_preload") == 1 ? "YES" : "NO",
                Metric(stream, "active_subset_preload") == 1,
                "Every active member remains inside the preload window.");
            yield return Item("modification_storage", "MAP17_05", "1/5/5",
                Metric(modification, "modified_sectors") + "/" +
                Metric(modification, "modification_records") + "/" +
                Metric(modification, "dirty_revision"),
                Metric(modification, "modified_sectors") == 1 &&
                Metric(modification, "modification_records") == 5 &&
                Metric(modification, "dirty_revision") == 5,
                "Modified-only storage retains five ordered mutations and revision five.");
            yield return Item("save_manifest", "MAP17_06", "1/0/5|168",
                Metric(manifest, "modified_manifest_entries") + "/" +
                Metric(manifest, "unmodified_manifest_entries") + "/" +
                Metric(manifest, "serialized_records") + "|" +
                Metric(manifest, "unmodified_sectors_omitted"),
                Metric(manifest, "modified_manifest_entries") == 1 &&
                Metric(manifest, "unmodified_manifest_entries") == 0 &&
                Metric(manifest, "serialized_records") == 5 &&
                Metric(manifest, "unmodified_sectors_omitted") == 168,
                "Save manifest serializes modified sectors only.");
            yield return Item("regeneration_apply", "MAP17_06", "1/5/0",
                Metric(regen, "modified_sector_plans") + "/" + Metric(regen, "regen_commands") +
                "/" + Metric(regen, "in_place_mutations"),
                Metric(regen, "modified_sector_plans") == 1 &&
                Metric(regen, "regen_commands") == 5 && Metric(regen, "in_place_mutations") == 0,
                "Regeneration replays all five records without mutating its input.");
            yield return Item("hash_mismatch", "MAP17_06", "6/0/0",
                Metric(mismatch, "hash_mismatch_failures") + "/" + Metric(mismatch, "retry_loops") +
                "/" + Metric(mismatch, "partial_apply_mutations"),
                Metric(mismatch, "hash_mismatch_failures") == 6 &&
                Metric(mismatch, "retry_loops") == 0 &&
                Metric(mismatch, "partial_apply_mutations") == 0,
                "Hash mismatch failures remain atomic without retry loops.");
            yield return Item("performance_report", "MAP17_07", "10|" +
                ExpectedPerformanceReportDigest, report.OperationGroupCount + "|" + report.Digest,
                report.OperationGroupCount == 10 && string.Equals(report.Digest,
                    ExpectedPerformanceReportDigest, StringComparison.Ordinal),
                "Performance report covers all reviewed operation groups with a stable digest.");
            yield return new GeneratedMap17ExitAuditItem("performance_spike", "MAP17_07",
                "INFO_OR_WARN_NON_BLOCKING", request.ObservedLayerBakeMaximumMilliseconds.ToString("F6",
                    CultureInfo.InvariantCulture) + " ms", GeneratedMap17ExitAuditSeverity.Warning,
                "Diagnostic layer-bake spike needs follow-up before live integration; structural evidence is stable.");
            yield return new GeneratedMap17ExitAuditItem("duplication_hardcoding", "MAP17_07",
                "1/22/1", request.DuplicateHelperCount + "/" +
                request.HardcodedCountConstantCount + "/" + request.ConsolidationCandidateCount,
                GeneratedMap17ExitAuditSeverity.Warning,
                "Private fixture duplication is carried forward; all count constants are named budget constants.");
            yield return Item("deferred_ownership", "MAP17_08", Number(Owners.Count),
                Number(request.DeferredOwners.Count), request.DeferredOwners.Count == Owners.Count,
                "Runtime, population, save, optimization and cleanup owners are explicit.");
        }

        private static IEnumerable<GeneratedMap17ExitAuditRisk> BuildRisks(
            GeneratedMap17ExitAuditRequest request)
        {
            yield return new GeneratedMap17ExitAuditRisk("layer_bake_diagnostic_spike",
                Owners["optimization"], GeneratedMap17ExitAuditSeverity.Warning, false,
                "3358.202900 ms is diagnostic only: no strict ms gate, count mismatch, side effect or retry loop.");
            yield return new GeneratedMap17ExitAuditRisk("duplicate_fixture_adapter",
                Owners["fixture_consolidation"], GeneratedMap17ExitAuditSeverity.Warning, false,
                "One private reference-chain fixture adapter remains a later cleanup candidate.");
            yield return new GeneratedMap17ExitAuditRisk("named_budget_constants", "MAP17_07",
                GeneratedMap17ExitAuditSeverity.Info, false,
                "Twenty-two hardcoded counts are named reviewed budget constants.");
            foreach (var owner in Owners)
                yield return new GeneratedMap17ExitAuditRisk("deferred_" + owner.Key, owner.Value,
                    GeneratedMap17ExitAuditSeverity.Info, false,
                    "Responsibility is deferred outside MAP17 and does not block MAP18 data ownership.");
        }

        private static GeneratedMap17ExitAuditItem Item(
            string key, string owner, string expected, string actual, bool passed, string reason) =>
            new GeneratedMap17ExitAuditItem(key, owner, expected, actual,
                passed ? GeneratedMap17ExitAuditSeverity.Pass :
                    GeneratedMap17ExitAuditSeverity.Failure, reason);

        private static int Metric(GeneratedTerrainPerformanceSample sample, string name) =>
            sample.Metric(name);

        private static void Add(ICollection<GeneratedMap17ExitAuditFailure> target,
            GeneratedMap17ExitAuditFailureCode code, string owner, string key,
            string expected, string actual, string reason) => target.Add(
                new GeneratedMap17ExitAuditFailure(code, owner, key, expected, actual, reason));

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
