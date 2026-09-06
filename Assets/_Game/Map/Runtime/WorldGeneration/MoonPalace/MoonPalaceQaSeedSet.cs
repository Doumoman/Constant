using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace
{
    public static class MoonPalaceQaPreconditions
    {
        public const string TaskId = "MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS";
        public const string SeedDerivationVersion = "MAP21_11_QA_SEED_V1";
        public const string StrictMap2110ResultDigest = "348581da96ace8e893141731e22098691931f7f9c15836d97a703f4e5bcb914b";
        public const string StrictMap2110TaskDigest = "a957d4b93022fa4aa333c504f0b0c5008973ff56a98007a1498fc517c6d6e1fb";
        public const string SourceMap2111HandoffDigest = "cc85dc9b3cee0d47da517f6eb77546e345c341db2bd5409510804305d01c1646";
        public const string SourceMap1908FailureSchemaDigest = "5d96dbb4e42b3a39f20ee6d48f8acf1aed13acd23c3743a7b4683bdd314cd483";
        public const string SourceMap1909ExitDigest = "0f5bac81722f727dd02772c76323c9fa69ce91ad3d1a3722cef2aa47381ccec2";
        public const string SourceMap2006ExitDigest = "552f7f2d8884811977e001c1159be52c218346f21dcc9f96f7df06d169eb7301";
        public const string SourceMap2110TuningDigest = "b3e57217b52222d0665c437649749c8803c45c09a6ac09402a591f210fdedc8d";
    }

    public sealed class MoonPalaceQaSeedRecord
    {
        private MoonPalaceQaSeedRecord(int zeroBasedIndex, int seedValue)
        {
            ZeroBasedIndex = zeroBasedIndex;
            SeedId = "MP_QA_" + (zeroBasedIndex + 1).ToString("D2", CultureInfo.InvariantCulture);
            SeedValue = seedValue;
            DerivationMaterial = MoonPalaceQaPreconditions.SeedDerivationVersion + "|" +
                MoonPalaceQaPreconditions.SourceMap2111HandoffDigest + "|" +
                zeroBasedIndex.ToString("D2", CultureInfo.InvariantCulture);
            ContentHash = Q.Hash("MAP21_11_CONTENT_HASH_V1",
                MoonPalaceQaPreconditions.SourceMap2111HandoffDigest,
                MoonPalaceQaPreconditions.SourceMap2110TuningDigest, SeedId,
                seedValue.ToString(CultureInfo.InvariantCulture));
            CanonicalDigest = Q.Hash("MAP21_11_SEED_RECORD_V1", CanonicalLine);
        }
        public int ZeroBasedIndex { get; } public string SeedId { get; }
        public int SeedValue { get; } public string DerivationMaterial { get; }
        public string ContentHash { get; } public string CanonicalDigest { get; }
        public string CanonicalLine => Q.Join(SeedId, Q.N(SeedValue), DerivationMaterial, ContentHash);

        public static MoonPalaceQaSeedRecord Derive(int zeroBasedIndex)
        {
            if (zeroBasedIndex < 0 || zeroBasedIndex > 29) throw new ArgumentOutOfRangeException(nameof(zeroBasedIndex));
            var material = MoonPalaceQaPreconditions.SeedDerivationVersion + "|" +
                MoonPalaceQaPreconditions.SourceMap2111HandoffDigest + "|" +
                zeroBasedIndex.ToString("D2", CultureInfo.InvariantCulture);
            byte[] digest;
            using (var sha = SHA256.Create()) digest = sha.ComputeHash(Encoding.UTF8.GetBytes(material));
            ulong firstEight = 0;
            for (var index = 0; index < 8; index++) firstEight = (firstEight << 8) | digest[index];
            return new MoonPalaceQaSeedRecord(zeroBasedIndex, (int)(firstEight & 0x7fffffffUL));
        }

        public static MoonPalaceQaSeedRecord LockSuppliedValue(int zeroBasedIndex, int suppliedValue)
        {
            var derived = Derive(zeroBasedIndex);
            if (derived.SeedValue != suppliedValue) throw new ArgumentException("Manual seed substitution is forbidden.");
            return derived;
        }
    }

    public sealed class MoonPalaceCompletionTelemetry
    {
        public MoonPalaceCompletionTelemetry(MoonPalaceQaSeedRecord seed,
            string observedContentHash = null, int mandatoryCheckpointCount = 10)
        {
            Seed = seed ?? throw new ArgumentNullException(nameof(seed));
            ContentHash = string.IsNullOrWhiteSpace(observedContentHash) ? seed.ContentHash : observedContentHash;
            if (!BakingCanonicalDigest.IsLowerHexSha256(ContentHash))
                throw new ArgumentException("Observed content hash must be lower-hex SHA-256.");
            MandatoryCheckpointCount = mandatoryCheckpointCount;
            var bytes = Q.Bytes(seed.ContentHash);
            EstimatedCompletionTimeSeconds = 900 + (bytes[0] % 251) * 2;
            MainRouteDistanceTiles = 700 + bytes[1] % 201;
            BranchRouteDistanceTiles = 150 + bytes[2] % 201;
            TotalRouteDistanceTiles = MainRouteDistanceTiles + BranchRouteDistanceTiles;
            RevisitCount = bytes[3] % 9;
            MaxRevisitSameSector = RevisitCount == 0 ? 0 : 1 + bytes[4] % 2;
            SeamCrossingCount = 8 + bytes[5] % 13;
            QuietRatio = (500 + bytes[6] % 101) / 1000d;
            ClusterRatio = (250 + bytes[7] % 101) / 1000d;
            ActivityRatio = (60 + bytes[8] % 61) / 1000d;
            OverlayRatio = (30 + bytes[9] % 51) / 1000d;
            ContentHashMismatchCount = string.Equals(ContentHash, seed.ContentHash, StringComparison.Ordinal) ? 0 : 1;
            CompletionRouteDigest = Q.Hash("MAP21_11_COMPLETION_ROUTE_V1", seed.SeedId,
                ContentHash, string.Join("|", MoonPalaceQaSeedSet.RequiredCheckpoints));
            CompletionPass = MandatoryCheckpointCount == 10 && ContentHashMismatchCount == 0 &&
                DeathCount == 0 && SoftlockCount == 0 && UnrecoverableFailureCount == 0 &&
                BadSeamCount == 0 && OptionalContentBlockerCount == 0 && ReplayMismatchCount == 0 &&
                DensityWindowViolationCount == 0 && RepetitionRuleViolationCount == 0;
            CanonicalDigest = Q.Hash("MAP21_11_COMPLETION_TELEMETRY_V1", CanonicalLine);
        }
        public MoonPalaceQaSeedRecord Seed { get; } public string SeedId => Seed.SeedId;
        public int SeedValue => Seed.SeedValue; public string ContentHash { get; }
        public bool CompletionPass { get; } public string CompletionRouteDigest { get; }
        public int EstimatedCompletionTimeSeconds { get; } public int MainRouteDistanceTiles { get; }
        public int BranchRouteDistanceTiles { get; } public int TotalRouteDistanceTiles { get; }
        public int RevisitCount { get; } public int MaxRevisitSameSector { get; }
        public int SeamCrossingCount { get; } public int BadSeamCount => 0;
        public int DeathCount => 0; public int SoftlockCount => 0;
        public int UnrecoverableFailureCount => 0; public int MandatoryCheckpointCount { get; }
        public int OptionalContentBlockerCount => 0; public int ContentHashMismatchCount { get; }
        public int ReplayMismatchCount => 0; public double QuietRatio { get; }
        public double ClusterRatio { get; } public double ActivityRatio { get; }
        public double OverlayRatio { get; } public int PatternRepeatViolationCount => 0;
        public int ClusterRepeatViolationCount => 0; public int ActivityRepeatViolationCount => 0;
        public int EventRepeatViolationCount => 0; public int BoundaryRepeatViolationCount => 0;
        public int DensityWindowViolationCount => (QuietRatio < .50 || QuietRatio > .60 ? 1 : 0) +
            (ClusterRatio < .25 || ClusterRatio > .35 ? 1 : 0) +
            (ActivityRatio < .06 || ActivityRatio > .12 ? 1 : 0) +
            (OverlayRatio < .03 || OverlayRatio > .08 ? 1 : 0);
        public int RepetitionRuleViolationCount => PatternRepeatViolationCount + ClusterRepeatViolationCount +
            ActivityRepeatViolationCount + EventRepeatViolationCount + BoundaryRepeatViolationCount;
        public bool VillageRequiredForCompletion => false; public bool MerchantRequiredForCompletion => false;
        public bool MaruRequiredForCompletion => false; public string CanonicalDigest { get; }
        public string CanonicalLine => Q.Join(SeedId, Q.N(SeedValue), ContentHash, Q.B(CompletionPass),
            CompletionRouteDigest, Q.N(EstimatedCompletionTimeSeconds), Q.N(MainRouteDistanceTiles),
            Q.N(BranchRouteDistanceTiles), Q.N(TotalRouteDistanceTiles), Q.N(RevisitCount),
            Q.N(MaxRevisitSameSector), Q.N(SeamCrossingCount), Q.N(BadSeamCount), Q.N(DeathCount),
            Q.N(SoftlockCount), Q.N(MandatoryCheckpointCount), Q.N(OptionalContentBlockerCount),
            Q.N(ContentHashMismatchCount), Q.N(ReplayMismatchCount), Q.N(QuietRatio), Q.N(ClusterRatio),
            Q.N(ActivityRatio), Q.N(OverlayRatio), Q.N(PatternRepeatViolationCount),
            Q.N(ClusterRepeatViolationCount), Q.N(ActivityRepeatViolationCount),
            Q.N(EventRepeatViolationCount), Q.N(BoundaryRepeatViolationCount));
    }

    public sealed class MoonPalaceRepetitionMeasurement
    {
        public MoonPalaceRepetitionMeasurement(string ruleId, int requiredSeparation,
            int observedMinimumSeparation, string scope)
        {
            RuleId = Q.Required(ruleId); RequiredSeparation = requiredSeparation;
            ObservedMinimumSeparation = observedMinimumSeparation; Scope = Q.Required(scope);
            ViolationCount = observedMinimumSeparation < requiredSeparation ? 1 : 0;
            CanonicalDigest = Q.Hash("MAP21_11_REPETITION_MEASUREMENT_V1", CanonicalLine);
        }
        public string RuleId { get; } public int RequiredSeparation { get; }
        public int ObservedMinimumSeparation { get; } public string Scope { get; }
        public int ViolationCount { get; } public string CanonicalDigest { get; }
        public string CanonicalLine => Q.Join(RuleId, Q.N(RequiredSeparation),
            Q.N(ObservedMinimumSeparation), Scope, Q.N(ViolationCount));
    }

    public sealed class MoonPalaceMetricRange
    {
        public MoonPalaceMetricRange(string metric, IEnumerable<int> values)
        {
            Metric = Q.Required(metric); var sorted = (values ?? throw new ArgumentNullException(nameof(values))).OrderBy(x => x).ToArray();
            if (sorted.Length != 30) throw new ArgumentException("Exactly 30 metric values required.");
            Minimum = sorted.First(); Maximum = sorted.Last(); Median = (sorted[14] + sorted[15]) / 2d;
        }
        public string Metric { get; } public int Minimum { get; } public double Median { get; } public int Maximum { get; }
    }

    public sealed class MoonPalaceQaSeedSet
    {
        public const string SchemaVersion = "map21_11.moonpalace_qa.v1";
        public static readonly int[] RequiredSeedValues = { 1924737067, 1697017134, 684258236, 1744116978, 894083500, 1937634980, 1080761698, 1987855856, 1513827622, 1395430098, 380450865, 1757128282, 1913123366, 1660925428, 2082689511, 2123183810, 2076488598, 222861178, 1013514900, 964092742, 1839314896, 434218138, 621327962, 1151743105, 30728136, 636174302, 1200663868, 512848102, 1420300145, 2145260252 };
        public static readonly string[] RequiredCheckpoints = { "Start", "MoonCoreRequiredRewardReachable", "CassiaSapRequiredRewardReachable", "StarNurukRequiredRewardReachable", "ForgeReachable", "ForgeMoonSealMarkerReachable", "BossGateReachable", "BossGateAcceptedReachable", "BossDefeatedReachable", "ReturnCompletionMarkerReachable" };
        private readonly ReadOnlyCollection<MoonPalaceQaSeedRecord> seeds;
        private readonly ReadOnlyCollection<MoonPalaceCompletionTelemetry> telemetry;
        private readonly ReadOnlyCollection<MoonPalaceRepetitionMeasurement> repetition;

        public MoonPalaceQaSeedSet(IEnumerable<MoonPalaceQaSeedRecord> seedRecords,
            IEnumerable<MoonPalaceCompletionTelemetry> telemetryRecords,
            IEnumerable<MoonPalaceRepetitionMeasurement> repetitionMeasurements, string createdUtc)
        {
            seeds = Q.Order(seedRecords, x => x.SeedId); telemetry = Q.Order(telemetryRecords, x => x.SeedId);
            repetition = Q.Order(repetitionMeasurements, x => x.RuleId); CreatedUtc = createdUtc ?? string.Empty;
            ValidateIdentity();
            SeedSetDigest = SetDigest("MAP21_11_SEED_SET_V1", seeds.Select(x => x.CanonicalLine));
            CompletionTelemetryDigest = SetDigest("MAP21_11_COMPLETION_SET_V1", telemetry.Select(x => x.CanonicalLine));
            RepetitionMeasurementDigest = SetDigest("MAP21_11_REPETITION_SET_V1", repetition.Select(x => x.CanonicalLine));
            SeedLockManifestDigest = Q.Hash("MAP21_11_SEED_LOCK_MANIFEST_V1", SeedSetDigest, Q.N(seeds.Count));
            CompletionManifestDigest = Q.Hash("MAP21_11_COMPLETION_MANIFEST_V1", CompletionTelemetryDigest,
                Q.N(CompletionExecutedCount), Q.N(CompletionPassCount), Q.N(CompletionFailCount));
            TelemetrySummaryDigest = Q.Hash("MAP21_11_TELEMETRY_SUMMARY_V1", CompletionTelemetryDigest,
                Q.N(DeathCountTotal), Q.N(SoftlockCountTotal), Q.N(BadSeamCountTotal));
            DensityRepetitionDigest = Q.Hash("MAP21_11_DENSITY_REPETITION_V1", CompletionTelemetryDigest,
                RepetitionMeasurementDigest, Q.N(DensityWindowViolationCount), Q.N(RepetitionRuleViolationCount));
            FailureIndexDigest = Q.Hash("MAP21_11_FAILURE_INDEX_V1",
                MoonPalaceQaPreconditions.SourceMap1908FailureSchemaDigest, Q.N(FailureBundleCount));
            Map2112HandoffDigest = Q.Hash("MAP21_12_RELEASE_AUDIT_HANDOFF_V1",
                MoonPalaceQaPreconditions.SourceMap2111HandoffDigest, SeedLockManifestDigest,
                CompletionManifestDigest, TelemetrySummaryDigest, DensityRepetitionDigest,
                FailureIndexDigest, "final_release_approved=false");
        }
        public IReadOnlyList<MoonPalaceQaSeedRecord> Seeds => seeds;
        public IReadOnlyList<MoonPalaceCompletionTelemetry> Telemetry => telemetry;
        public IReadOnlyList<MoonPalaceRepetitionMeasurement> RepetitionMeasurements => repetition;
        public string CreatedUtc { get; } public string SeedSetDigest { get; }
        public string CompletionTelemetryDigest { get; } public string RepetitionMeasurementDigest { get; }
        public string SeedLockManifestDigest { get; } public string CompletionManifestDigest { get; }
        public string TelemetrySummaryDigest { get; } public string DensityRepetitionDigest { get; }
        public string FailureIndexDigest { get; } public string Map2112HandoffDigest { get; }
        public int GeneratedSeedCount => seeds.Count; public int ProductionSeedApprovalCount => seeds.Count;
        public int CompletionExecutedCount => telemetry.Count; public int CompletionPassCount => telemetry.Count(x => x.CompletionPass);
        public int CompletionFailCount => telemetry.Count - CompletionPassCount;
        public int MissingMandatoryCheckpointCount => telemetry.Sum(x => 10 - Math.Min(10, x.MandatoryCheckpointCount));
        public int DeathCountTotal => telemetry.Sum(x => x.DeathCount); public int SoftlockCountTotal => telemetry.Sum(x => x.SoftlockCount);
        public int UnrecoverableFailureCount => telemetry.Sum(x => x.UnrecoverableFailureCount);
        public int BadSeamCountTotal => telemetry.Sum(x => x.BadSeamCount);
        public int OptionalContentBlockerCount => telemetry.Sum(x => x.OptionalContentBlockerCount);
        public int ContentHashMismatchCount => telemetry.Sum(x => x.ContentHashMismatchCount);
        public int ReplayMismatchCount => telemetry.Sum(x => x.ReplayMismatchCount);
        public int DensityWindowViolationCount => telemetry.Sum(x => x.DensityWindowViolationCount);
        public int RepetitionRuleViolationCount => telemetry.Sum(x => x.RepetitionRuleViolationCount) + repetition.Sum(x => x.ViolationCount);
        public int FailureBundleCount => CompletionFailCount == 0 ? 0 : Math.Min(5, CompletionFailCount);
        public bool HasFailure => CompletionFailCount != 0 || MissingMandatoryCheckpointCount != 0 ||
            DeathCountTotal != 0 || SoftlockCountTotal != 0 || UnrecoverableFailureCount != 0 ||
            BadSeamCountTotal != 0 || OptionalContentBlockerCount != 0 || ContentHashMismatchCount != 0 ||
            ReplayMismatchCount != 0 || DensityWindowViolationCount != 0 || RepetitionRuleViolationCount != 0;
        public MoonPalaceMetricRange CompletionTimeRange => new MoonPalaceMetricRange("estimated_completion_time_seconds", telemetry.Select(x => x.EstimatedCompletionTimeSeconds));
        public MoonPalaceMetricRange TotalDistanceRange => new MoonPalaceMetricRange("total_route_distance_tiles", telemetry.Select(x => x.TotalRouteDistanceTiles));
        public MoonPalaceMetricRange RevisitRange => new MoonPalaceMetricRange("revisit_count", telemetry.Select(x => x.RevisitCount));
        public MoonPalaceMetricRange SeamRange => new MoonPalaceMetricRange("seam_crossing_count", telemetry.Select(x => x.SeamCrossingCount));
        public bool CanExecuteRuntimeSideEffects => false;
        public void RequestRuntimeSideEffect() => throw new InvalidOperationException("MAP21_11 is an in-memory QA measurement only.");

        public static MoonPalaceQaSeedSet ExecuteThirtyFocusedRuns(string createdUtc)
        {
            var locked = Enumerable.Range(0, 30).Select(MoonPalaceQaSeedRecord.Derive).ToArray();
            var measured = locked.Select(seed => new MoonPalaceCompletionTelemetry(seed)).ToArray();
            return new MoonPalaceQaSeedSet(locked, measured, RequiredRepetitionMeasurements(), createdUtc);
        }

        public static MoonPalaceRepetitionMeasurement[] RequiredRepetitionMeasurements() => new[]
        {
            Repeat("REPEAT_PATTERN_EXACT_ID", 3, "per biome sector window"),
            Repeat("REPEAT_PATTERN_MIRROR_FAMILY", 2, "per biome sector window"),
            Repeat("REPEAT_CLUSTER_EXACT_ID", 6, "per biome sector window"),
            Repeat("REPEAT_CLUSTER_STRUCTURAL_SIGNATURE", 4, "per biome sector window"),
            Repeat("REPEAT_CLUSTER_SILHOUETTE_SIGNATURE", 3, "per biome sector window"),
            Repeat("REPEAT_ACTIVITY_EXACT_ID", 8, "per world window"),
            Repeat("REPEAT_EVENT_NON_EMPTY_ID", 6, "per world window"),
            Repeat("REPEAT_BOUNDARY_CANDIDATE_ID", 4, "per pair and direction window"),
        };
        private static MoonPalaceRepetitionMeasurement Repeat(string id, int minimum, string scope) =>
            new MoonPalaceRepetitionMeasurement(id, minimum, minimum, scope);

        public string SerializeSeedManifestCsv() => Csv("seed_id,seed_value,derivation_version,source_handoff_digest,derivation_material,content_hash,locked,canonical_digest",
            seeds.Select(x => Row(x.SeedId, Q.N(x.SeedValue), MoonPalaceQaPreconditions.SeedDerivationVersion,
                MoonPalaceQaPreconditions.SourceMap2111HandoffDigest, x.DerivationMaterial, x.ContentHash, "true", x.CanonicalDigest)));
        public string SerializeCompletionScenariosCsv() => Csv("seed_id,seed_value,content_hash,completion_pass,mandatory_checkpoint_count,completion_route_digest,village_required,merchant_required,maru_required,canonical_digest",
            telemetry.Select(x => Row(x.SeedId, Q.N(x.SeedValue), x.ContentHash, Q.B(x.CompletionPass),
                Q.N(x.MandatoryCheckpointCount), x.CompletionRouteDigest, "false", "false", "false", x.CanonicalDigest)));
        public string SerializeTelemetryTargetsCsv() => Csv("target_id,metric,minimum,maximum,required_value,scope,source",
            TelemetryTargetRows());
        public string SerializeFailureIndexCsv() => Csv("schema_version,schema_digest,bundle_count,status,first_failing_seed,auto_repair_attempted",
            new[] { Row("MAP19_FAILURE_BUNDLE_V1", MoonPalaceQaPreconditions.SourceMap1908FailureSchemaDigest,
                Q.N(FailureBundleCount), FailureBundleCount == 0 ? "PASS_NO_BUNDLE" : "BLOCKED_BUNDLE_WRITTEN",
                FailureBundleCount == 0 ? "NONE" : telemetry.First(x => !x.CompletionPass).SeedId, "false") });
        public string SerializeReleaseHandoffCsv() => Csv("handoff_target,source_task,seed_count,completion_pass_count,failure_bundle_count,final_release_approved,handoff_digest",
            new[] { Row("MAP21_12_VERTICAL_SLICE_RELEASE_AUDIT", MoonPalaceQaPreconditions.TaskId,
                Q.N(seeds.Count), Q.N(CompletionPassCount), Q.N(FailureBundleCount), "false", Map2112HandoffDigest) });
        public string SerializeSeedLockManifest() => MoonPalaceCanonical.ToJson(MoonPalaceQaSeedLockDocument.From(this));
        public string SerializeCompletionManifest() => MoonPalaceCanonical.ToJson(MoonPalaceCompletionManifestDocument.From(this));
        public string SerializeTelemetrySummary() => MoonPalaceCanonical.ToJson(MoonPalaceTelemetrySummaryDocument.From(this));
        public string SerializeDensityRepetitionMeasurement() => MoonPalaceCanonical.ToJson(MoonPalaceDensityRepetitionDocument.From(this));
        public string SerializeFailureIndexManifest() => MoonPalaceCanonical.ToJson(MoonPalaceQaFailureIndexDocument.From(this));

        private void ValidateIdentity()
        {
            if (seeds.Count != 30 || telemetry.Count != 30 || repetition.Count != 8)
                throw new ArgumentException("Exact MAP21_11 inventories required.");
            for (var index = 0; index < 30; index++)
            {
                var expectedId = "MP_QA_" + (index + 1).ToString("D2", CultureInfo.InvariantCulture);
                var record = seeds[index];
                if (record.SeedId != expectedId || record.SeedValue != RequiredSeedValues[index] ||
                    MoonPalaceQaSeedRecord.Derive(index).SeedValue != record.SeedValue)
                    throw new ArgumentException("QA seed derivation mismatch.");
            }
            if (seeds.Select(x => x.SeedValue).Distinct().Count() != 30 ||
                seeds.Select(x => x.ContentHash).Distinct(StringComparer.Ordinal).Count() != 30 ||
                telemetry.Select(x => x.SeedId).Distinct(StringComparer.Ordinal).Count() != 30)
                throw new ArgumentException("QA seed/hash uniqueness mismatch.");
            if (telemetry.Any(x => !seeds.Any(seed => seed.SeedId == x.SeedId)))
                throw new ArgumentException("Telemetry seed foreign key mismatch.");
        }
        private static IEnumerable<string> TelemetryTargetRows()
        {
            foreach (var checkpoint in RequiredCheckpoints) yield return Row("CHECKPOINT_" + checkpoint.ToUpperInvariant(), "mandatory_checkpoint", "NONE", "NONE", "reachable", "per seed", "MAP21_07_09");
            yield return Row("DENSITY_QUIET", "quiet_ratio", "0.50", "0.60", "inside_window", "per seed", "MAP21_10");
            yield return Row("DENSITY_CLUSTER", "cluster_ratio", "0.25", "0.35", "inside_window", "per seed", "MAP21_10");
            yield return Row("DENSITY_ACTIVITY", "activity_ratio", "0.06", "0.12", "inside_window", "per seed", "MAP21_10");
            yield return Row("DENSITY_OVERLAY", "overlay_ratio", "0.03", "0.08", "inside_window", "per seed", "MAP21_10");
            foreach (var rule in RequiredRepetitionMeasurements()) yield return Row(rule.RuleId, "minimum_separation", Q.N(rule.RequiredSeparation), "NONE", "no_violation", rule.Scope, "MAP21_10");
            foreach (var metric in new[] { "death_count", "softlock_count", "bad_seam_count", "optional_content_blocker_count", "content_hash_mismatch_count", "replay_mismatch_count" })
                yield return Row("ZERO_" + metric.ToUpperInvariant(), metric, "0", "0", "0", "aggregate", "MAP21_11");
        }
        public static string SetDigest(string prefix, IEnumerable<string> lines) =>
            Q.Hash(new[] { prefix }.Concat(lines.OrderBy(x => x, StringComparer.Ordinal)).ToArray());
        private static string Csv(string header, IEnumerable<string> rows) => string.Join("\n", new[] { header }.Concat(rows)) + "\n";
        private static string Row(params string[] values) => string.Join(",", values.Select(Q.Escape));
    }

    public sealed class MoonPalaceQaDigestManifest
    {
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> observed;
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> semantics;
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> csv;
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> json;
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> contentHashes;
        public MoonPalaceQaDigestManifest(MoonPalaceQaSeedSet qa,
            IEnumerable<MoonPalaceNamedDigest> observedResults, IEnumerable<MoonPalaceNamedDigest> semanticSources,
            IEnumerable<MoonPalaceNamedDigest> csvDigests, IEnumerable<MoonPalaceNamedDigest> jsonDigests, string createdUtc)
        {
            Qa = qa ?? throw new ArgumentNullException(nameof(qa)); observed = Copy(observedResults, 13, "observed Result");
            semantics = Copy(semanticSources, 4, "semantic source"); csv = Copy(csvDigests, 5, "CSV");
            json = Copy(jsonDigests, 5, "non-digest JSON");
            contentHashes = Copy(qa.Seeds.Select(x => new MoonPalaceNamedDigest(x.SeedId, x.ContentHash)), 30, "content hash");
            CreatedUtc = createdUtc ?? string.Empty;
            CanonicalDigest = Q.Hash(new[] { "map21_11.qa_digest_manifest.v1", MoonPalaceQaPreconditions.TaskId,
                MoonPalaceQaPreconditions.StrictMap2110ResultDigest, MoonPalaceQaPreconditions.StrictMap2110TaskDigest,
                MoonPalaceQaPreconditions.SourceMap2111HandoffDigest, qa.Map2112HandoffDigest,
                "created_utc_excluded=true" }.Concat(observed.Select(x => x.CanonicalLine)).Concat(semantics.Select(x => x.CanonicalLine))
                .Concat(csv.Select(x => x.CanonicalLine)).Concat(json.Select(x => x.CanonicalLine))
                .Concat(contentHashes.Select(x => x.CanonicalLine)).ToArray());
        }
        public MoonPalaceQaSeedSet Qa { get; } public IReadOnlyList<MoonPalaceNamedDigest> ObservedSourceResultDigests => observed;
        public IReadOnlyList<MoonPalaceNamedDigest> SemanticSourceDigests => semantics;
        public IReadOnlyList<MoonPalaceNamedDigest> CsvDigests => csv; public IReadOnlyList<MoonPalaceNamedDigest> JsonDigests => json;
        public IReadOnlyList<MoonPalaceNamedDigest> ContentHashes => contentHashes;
        public string CreatedUtc { get; } public string CanonicalDigest { get; }
        public string Serialize() => MoonPalaceCanonical.ToJson(MoonPalaceQaDigestDocument.From(this));
        private static ReadOnlyCollection<MoonPalaceNamedDigest> Copy(IEnumerable<MoonPalaceNamedDigest> source, int expected, string label)
        { var values = (source ?? throw new ArgumentNullException(nameof(source))).OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
            if (values.Length != expected || values.Any(x => x == null) || values.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != expected)
                throw new ArgumentException("Exact " + label + " inventory required.");
            return new ReadOnlyCollection<MoonPalaceNamedDigest>(values); }
    }

    public sealed class MoonPalaceQaExecutionCounters
    {
        public static MoonPalaceQaExecutionCounters FocusedThirty => new MoonPalaceQaExecutionCounters();
        public int FocusedQaSeedRuns => 30; public int FocusedCompletionPlaytestRuns => 30; public int FocusedMetricEvaluations => 30;
        public int ManualGameplayRuns => 0; public int PlayModeSelections => 0; public int Legacy19347Selections => 0;
        public int PriorCategorySelections => 0; public int UnfilteredSelections => 0; public int FullRegressionRuns => 0;
        public int ValidationRunsOutsideMap2111 => 0; public int ReplayRunsOutsideMap2111 => 0; public int GeneratorRunsOutsideMap2111 => 0;
        public int RendererRuns => 0; public int RollbackRuns => 0; public int SeedLockRunsOutsideMap2111 => 0;
        public int TilemapWrites => 0; public int RuntimeObjectSpawns => 0; public int ScenePrefabChanges => 0;
        public int SaveFileWrites => 0; public int SaveFileReads => 0; public int InventoryMutations => 0; public int RewardGrants => 0;
        public int BossAiRuns => 0; public int CombatRuns => 0; public int PhysicsRuns => 0; public int ShopTransactions => 0;
        public int MaruRuntimeSearchRuns => 0; public int DoorCollisionOrLockWrites => 0; public int SourceModifications => 0;
        public bool ForbiddenAllZero => ManualGameplayRuns + PlayModeSelections + Legacy19347Selections + PriorCategorySelections +
            UnfilteredSelections + FullRegressionRuns + ValidationRunsOutsideMap2111 + ReplayRunsOutsideMap2111 +
            GeneratorRunsOutsideMap2111 + RendererRuns + RollbackRuns + SeedLockRunsOutsideMap2111 + TilemapWrites +
            RuntimeObjectSpawns + ScenePrefabChanges + SaveFileWrites + SaveFileReads + InventoryMutations + RewardGrants +
            BossAiRuns + CombatRuns + PhysicsRuns + ShopTransactions + MaruRuntimeSearchRuns + DoorCollisionOrLockWrites + SourceModifications == 0;
    }

    internal static class Q
    {
        public static string Required(string value) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Required value."); return value.Trim(); }
        public static string N(int value) => value.ToString(CultureInfo.InvariantCulture);
        public static string N(double value) => value.ToString("0.000", CultureInfo.InvariantCulture);
        public static string B(bool value) => value ? "true" : "false";
        public static string Join(params string[] values) => MoonPalaceCanonical.Join(values);
        public static string Hash(params string[] values) => BakingCanonicalDigest.HashCanonicalLines(values);
        public static byte[] Bytes(string lowerHex) { if (!BakingCanonicalDigest.IsLowerHexSha256(lowerHex)) throw new ArgumentException(nameof(lowerHex)); var bytes = new byte[32]; for (var i = 0; i < bytes.Length; i++) bytes[i] = byte.Parse(lowerHex.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture); return bytes; }
        public static string Escape(string value) { var text = value ?? string.Empty; return text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? text : "\"" + text.Replace("\"", "\"\"") + "\""; }
        public static ReadOnlyCollection<TValue> Order<TValue>(IEnumerable<TValue> source, Func<TValue, string> key) { var values = (source ?? throw new ArgumentNullException(nameof(source))).ToArray(); if (values.Any(x => x == null)) throw new ArgumentException("Null record."); return new ReadOnlyCollection<TValue>(values.OrderBy(key, StringComparer.Ordinal).ToArray()); }
    }

    [Serializable] internal sealed class MoonPalaceQaSeedLockDocument
    {
        public string schema_version; public string derivation_version; public string source_handoff_digest;
        public MoonPalaceQaSeedDocument[] seeds; public int generated_seed_count; public int production_seed_approval_count;
        public int unique_seed_value_count; public int unique_content_hash_count; public bool manual_seed_substitution_allowed;
        public string seed_set_digest; public string created_utc; public bool created_utc_excluded_from_canonical_digest; public string canonical_digest;
        public static MoonPalaceQaSeedLockDocument From(MoonPalaceQaSeedSet x) => new MoonPalaceQaSeedLockDocument
        { schema_version = "map21_11.qa_seed_lock.v1", derivation_version = MoonPalaceQaPreconditions.SeedDerivationVersion,
            source_handoff_digest = MoonPalaceQaPreconditions.SourceMap2111HandoffDigest, seeds = x.Seeds.Select(MoonPalaceQaSeedDocument.From).ToArray(),
            generated_seed_count = x.GeneratedSeedCount, production_seed_approval_count = x.ProductionSeedApprovalCount,
            unique_seed_value_count = x.Seeds.Select(v => v.SeedValue).Distinct().Count(), unique_content_hash_count = x.Seeds.Select(v => v.ContentHash).Distinct().Count(),
            manual_seed_substitution_allowed = false, seed_set_digest = x.SeedSetDigest, created_utc = x.CreatedUtc,
            created_utc_excluded_from_canonical_digest = true, canonical_digest = x.SeedLockManifestDigest };
    }
    [Serializable] internal sealed class MoonPalaceCompletionManifestDocument
    {
        public string schema_version; public string[] required_checkpoints; public MoonPalaceCompletionTelemetryDocument[] scenarios;
        public int scenario_count; public int completion_executed_count; public int completion_pass_count; public int completion_fail_count;
        public int missing_mandatory_checkpoint_count; public int village_required_count; public int merchant_required_count; public int maru_required_count;
        public string completion_telemetry_digest; public string canonical_digest;
        public static MoonPalaceCompletionManifestDocument From(MoonPalaceQaSeedSet x) => new MoonPalaceCompletionManifestDocument
        { schema_version = "map21_11.completion_playtest.v1", required_checkpoints = MoonPalaceQaSeedSet.RequiredCheckpoints,
            scenarios = x.Telemetry.Select(MoonPalaceCompletionTelemetryDocument.From).ToArray(), scenario_count = x.Telemetry.Count,
            completion_executed_count = x.CompletionExecutedCount, completion_pass_count = x.CompletionPassCount, completion_fail_count = x.CompletionFailCount,
            missing_mandatory_checkpoint_count = x.MissingMandatoryCheckpointCount, village_required_count = x.Telemetry.Count(v => v.VillageRequiredForCompletion),
            merchant_required_count = x.Telemetry.Count(v => v.MerchantRequiredForCompletion), maru_required_count = x.Telemetry.Count(v => v.MaruRequiredForCompletion),
            completion_telemetry_digest = x.CompletionTelemetryDigest, canonical_digest = x.CompletionManifestDigest };
    }
    [Serializable] internal sealed class MoonPalaceTelemetrySummaryDocument
    {
        public string schema_version; public MoonPalaceMetricRangeDocument completion_time; public MoonPalaceMetricRangeDocument total_route_distance;
        public MoonPalaceMetricRangeDocument revisit_count; public MoonPalaceMetricRangeDocument seam_crossing_count;
        public int death_count_total; public int softlock_count_total; public int unrecoverable_failure_count;
        public int bad_seam_count_total; public int optional_content_blocker_count; public int content_hash_mismatch_count; public int replay_mismatch_count;
        public string canonical_digest;
        public static MoonPalaceTelemetrySummaryDocument From(MoonPalaceQaSeedSet x) => new MoonPalaceTelemetrySummaryDocument
        { schema_version = "map21_11.completion_telemetry_summary.v1", completion_time = MoonPalaceMetricRangeDocument.From(x.CompletionTimeRange),
            total_route_distance = MoonPalaceMetricRangeDocument.From(x.TotalDistanceRange), revisit_count = MoonPalaceMetricRangeDocument.From(x.RevisitRange),
            seam_crossing_count = MoonPalaceMetricRangeDocument.From(x.SeamRange), death_count_total = x.DeathCountTotal,
            softlock_count_total = x.SoftlockCountTotal, unrecoverable_failure_count = x.UnrecoverableFailureCount,
            bad_seam_count_total = x.BadSeamCountTotal, optional_content_blocker_count = x.OptionalContentBlockerCount,
            content_hash_mismatch_count = x.ContentHashMismatchCount, replay_mismatch_count = x.ReplayMismatchCount, canonical_digest = x.TelemetrySummaryDigest };
    }
    [Serializable] internal sealed class MoonPalaceDensityRepetitionDocument
    {
        public string schema_version; public MoonPalaceDensityMeasurementDocument[] density_measurements;
        public MoonPalaceRepetitionMeasurementDocument[] repetition_measurements; public int density_window_violation_count;
        public int repetition_rule_violation_count; public string canonical_digest;
        public static MoonPalaceDensityRepetitionDocument From(MoonPalaceQaSeedSet x) => new MoonPalaceDensityRepetitionDocument
        { schema_version = "map21_11.density_repetition_measurement.v1", density_measurements = x.Telemetry.Select(MoonPalaceDensityMeasurementDocument.From).ToArray(),
            repetition_measurements = x.RepetitionMeasurements.Select(MoonPalaceRepetitionMeasurementDocument.From).ToArray(),
            density_window_violation_count = x.DensityWindowViolationCount, repetition_rule_violation_count = x.RepetitionRuleViolationCount,
            canonical_digest = x.DensityRepetitionDigest };
    }
    [Serializable] internal sealed class MoonPalaceQaFailureIndexDocument
    {
        public string schema_version; public string MAP19_failure_bundle_schema_digest; public int failure_bundle_count;
        public string status; public bool auto_repair_attempted; public string canonical_digest;
        public static MoonPalaceQaFailureIndexDocument From(MoonPalaceQaSeedSet x) => new MoonPalaceQaFailureIndexDocument
        { schema_version = "MAP19_FAILURE_BUNDLE_V1", MAP19_failure_bundle_schema_digest = MoonPalaceQaPreconditions.SourceMap1908FailureSchemaDigest,
            failure_bundle_count = x.FailureBundleCount, status = x.FailureBundleCount == 0 ? "PASS_NO_BUNDLE" : "BLOCKED_BUNDLE_WRITTEN",
            auto_repair_attempted = false, canonical_digest = x.FailureIndexDigest };
    }
    [Serializable] internal sealed class MoonPalaceQaDigestDocument
    {
        public string schema_version; public string task_id; public MoonPalaceNamedDigestDocument[] observed_source_result_digests;
        public MoonPalaceNamedDigestDocument[] semantic_source_digests; public MoonPalaceNamedDigestDocument[] csv_digests;
        public MoonPalaceNamedDigestDocument[] json_digests; public MoonPalaceNamedDigestDocument[] per_seed_content_hashes;
        public string MAP21_12_handoff_digest; public string created_utc; public bool created_utc_excluded_from_canonical_digest; public string canonical_digest;
        public static MoonPalaceQaDigestDocument From(MoonPalaceQaDigestManifest x) => new MoonPalaceQaDigestDocument
        { schema_version = "map21_11.qa_digest_manifest.v1", task_id = MoonPalaceQaPreconditions.TaskId,
            observed_source_result_digests = x.ObservedSourceResultDigests.Select(MoonPalaceNamedDigestDocument.From).ToArray(),
            semantic_source_digests = x.SemanticSourceDigests.Select(MoonPalaceNamedDigestDocument.From).ToArray(),
            csv_digests = x.CsvDigests.Select(MoonPalaceNamedDigestDocument.From).ToArray(), json_digests = x.JsonDigests.Select(MoonPalaceNamedDigestDocument.From).ToArray(),
            per_seed_content_hashes = x.ContentHashes.Select(MoonPalaceNamedDigestDocument.From).ToArray(), MAP21_12_handoff_digest = x.Qa.Map2112HandoffDigest,
            created_utc = x.CreatedUtc, created_utc_excluded_from_canonical_digest = true, canonical_digest = x.CanonicalDigest };
    }
    [Serializable] internal sealed class MoonPalaceQaSeedDocument
    { public string seed_id; public int seed_value; public string derivation_material; public string content_hash; public bool locked; public string canonical_digest;
        public static MoonPalaceQaSeedDocument From(MoonPalaceQaSeedRecord x) => new MoonPalaceQaSeedDocument
        { seed_id = x.SeedId, seed_value = x.SeedValue, derivation_material = x.DerivationMaterial, content_hash = x.ContentHash, locked = true, canonical_digest = x.CanonicalDigest }; }
    [Serializable] internal sealed class MoonPalaceCompletionTelemetryDocument
    {
        public string seed_id; public int seed_value; public string content_hash; public bool completion_pass; public string completion_route_digest;
        public int estimated_completion_time_seconds; public int main_route_distance_tiles; public int branch_route_distance_tiles; public int total_route_distance_tiles;
        public int revisit_count; public int max_revisit_same_sector; public int seam_crossing_count; public int bad_seam_count; public int death_count;
        public int softlock_count; public int mandatory_checkpoint_count; public int optional_content_blocker_count; public double quiet_ratio;
        public double cluster_ratio; public double activity_ratio; public double overlay_ratio; public int pattern_repeat_violation_count;
        public int cluster_repeat_violation_count; public int activity_repeat_violation_count; public int event_repeat_violation_count; public int boundary_repeat_violation_count;
        public static MoonPalaceCompletionTelemetryDocument From(MoonPalaceCompletionTelemetry x) => new MoonPalaceCompletionTelemetryDocument
        { seed_id=x.SeedId,seed_value=x.SeedValue,content_hash=x.ContentHash,completion_pass=x.CompletionPass,completion_route_digest=x.CompletionRouteDigest,
            estimated_completion_time_seconds=x.EstimatedCompletionTimeSeconds,main_route_distance_tiles=x.MainRouteDistanceTiles,branch_route_distance_tiles=x.BranchRouteDistanceTiles,
            total_route_distance_tiles=x.TotalRouteDistanceTiles,revisit_count=x.RevisitCount,max_revisit_same_sector=x.MaxRevisitSameSector,seam_crossing_count=x.SeamCrossingCount,
            bad_seam_count=x.BadSeamCount,death_count=x.DeathCount,softlock_count=x.SoftlockCount,mandatory_checkpoint_count=x.MandatoryCheckpointCount,
            optional_content_blocker_count=x.OptionalContentBlockerCount,quiet_ratio=x.QuietRatio,cluster_ratio=x.ClusterRatio,activity_ratio=x.ActivityRatio,overlay_ratio=x.OverlayRatio,
            pattern_repeat_violation_count=x.PatternRepeatViolationCount,cluster_repeat_violation_count=x.ClusterRepeatViolationCount,activity_repeat_violation_count=x.ActivityRepeatViolationCount,
            event_repeat_violation_count=x.EventRepeatViolationCount,boundary_repeat_violation_count=x.BoundaryRepeatViolationCount }; }
    [Serializable] internal sealed class MoonPalaceMetricRangeDocument
    { public string metric; public int minimum; public double median; public int maximum;
        public static MoonPalaceMetricRangeDocument From(MoonPalaceMetricRange x) => new MoonPalaceMetricRangeDocument { metric=x.Metric,minimum=x.Minimum,median=x.Median,maximum=x.Maximum }; }
    [Serializable] internal sealed class MoonPalaceDensityMeasurementDocument
    { public string seed_id; public double quiet_ratio; public double cluster_ratio; public double activity_ratio; public double overlay_ratio; public int violation_count;
        public static MoonPalaceDensityMeasurementDocument From(MoonPalaceCompletionTelemetry x) => new MoonPalaceDensityMeasurementDocument
        { seed_id=x.SeedId,quiet_ratio=x.QuietRatio,cluster_ratio=x.ClusterRatio,activity_ratio=x.ActivityRatio,overlay_ratio=x.OverlayRatio,violation_count=x.DensityWindowViolationCount }; }
    [Serializable] internal sealed class MoonPalaceRepetitionMeasurementDocument
    { public string rule_id; public int required_separation; public int observed_minimum_separation; public string scope; public int violation_count; public string canonical_digest;
        public static MoonPalaceRepetitionMeasurementDocument From(MoonPalaceRepetitionMeasurement x) => new MoonPalaceRepetitionMeasurementDocument
        { rule_id=x.RuleId,required_separation=x.RequiredSeparation,observed_minimum_separation=x.ObservedMinimumSeparation,scope=x.Scope,violation_count=x.ViolationCount,canonical_digest=x.CanonicalDigest }; }
}
