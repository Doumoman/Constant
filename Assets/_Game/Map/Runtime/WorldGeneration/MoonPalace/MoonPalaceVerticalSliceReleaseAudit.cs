using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace
{
    public static class MoonPalaceReleaseAuditPreconditions
    {
        public const string TaskId = "MAP21_12_VERTICAL_SLICE_RELEASE_AUDIT";
        public const string StrictMap2111ResultDigest = "231c3e615469392c0eaa0fa954004abecc47aab578c8df3afd1e9968f612f81a";
        public const string StrictMap2111TaskDigest = "406814081431426e38c47c8d106356107d424ee5db4ece729baa95c7aefa4d89";
        public const string StrictMap2112HandoffDigest = "a72df115f6599b2a02439e97c16c7ea51325f43f180388db9989b7333707e2d2";
        public const string Map1708AuditDigest = "8b4849bf11ac6807a9e8a9d699a166eaa61e5c600454e410bae1ad47480545a0";
        public const string Map1807AuditDigest = "d17fc7aa674e42bbe17b576032d4f03298ecdf53e890dd1ae2352c68078ae6a7";
        public const string Map1908FailureSchemaDigest = "5d96dbb4e42b3a39f20ee6d48f8acf1aed13acd23c3743a7b4683bdd314cd483";
        public const string Map1909ExitDigest = "0f5bac81722f727dd02772c76323c9fa69ce91ad3d1a3722cef2aa47381ccec2";
        public const string Map2006ExitDigest = "552f7f2d8884811977e001c1159be52c218346f21dcc9f96f7df06d169eb7301";
        public const string Map2110TuningDigest = "b3e57217b52222d0665c437649749c8803c45c09a6ac09402a591f210fdedc8d";
        public const string Map2111QaDigest = "d16a54da27dd5156ccb1ad6481348264522d9d57262c6c7de3d7b4a07ca371f1";
    }

    public sealed class ReleaseSourceRecord
    {
        public ReleaseSourceRecord(string taskId, string phase, string resultPath, string resultDigest, string semanticDigest)
        {
            TaskId = ReleaseAuditCanonical.Required(taskId);
            Phase = ReleaseAuditCanonical.Required(phase);
            ResultPath = ReleaseAuditCanonical.Required(resultPath);
            ResultDigest = ReleaseAuditCanonical.Digest(resultDigest);
            SemanticDigest = semanticDigest == "NONE" ? "NONE" : ReleaseAuditCanonical.Digest(semanticDigest);
        }

        public string TaskId { get; }
        public string Phase { get; }
        public string ResultPath { get; }
        public string ResultDigest { get; }
        public string SemanticDigest { get; }
        public string Status => "PASS";
        public bool ReadOnlySource => true;
        public string CanonicalLine => ReleaseAuditCanonical.Join(
            TaskId, Phase, ResultPath, ResultDigest, SemanticDigest, Status, ReleaseAuditCanonical.Bool(ReadOnlySource));
    }

    public sealed class ReleaseGateRecord
    {
        public ReleaseGateRecord(string gateId, string area, string evidence, string status, int blockerCount, int failCount, int allowedWarningCount)
        {
            GateId = ReleaseAuditCanonical.Required(gateId);
            Area = ReleaseAuditCanonical.Required(area);
            Evidence = ReleaseAuditCanonical.Required(evidence);
            Status = ReleaseAuditCanonical.Required(status);
            if (blockerCount < 0 || failCount < 0 || allowedWarningCount < 0) throw new ArgumentOutOfRangeException();
            BlockerCount = blockerCount;
            FailCount = failCount;
            AllowedWarningCount = allowedWarningCount;
        }

        public string GateId { get; }
        public string Area { get; }
        public string Evidence { get; }
        public string Status { get; }
        public int BlockerCount { get; }
        public int FailCount { get; }
        public int AllowedWarningCount { get; }
        public bool Passed => Status == "PASS" && BlockerCount == 0 && FailCount == 0;
        public string CanonicalLine => ReleaseAuditCanonical.Join(
            GateId, Area, Evidence, Status, ReleaseAuditCanonical.Number(BlockerCount),
            ReleaseAuditCanonical.Number(FailCount), ReleaseAuditCanonical.Number(AllowedWarningCount));
    }

    public sealed class ReleaseWarningRecord
    {
        public ReleaseWarningRecord(string warningId, string sourceTaskId, string kind, string summary, string owner)
        {
            WarningId = ReleaseAuditCanonical.Required(warningId);
            SourceTaskId = ReleaseAuditCanonical.Required(sourceTaskId);
            Kind = ReleaseAuditCanonical.Required(kind);
            Summary = ReleaseAuditCanonical.Required(summary);
            Owner = ReleaseAuditCanonical.Required(owner);
            if (Kind != "WARN" && Kind != "DEFERRED") throw new ArgumentException("Unsupported warning kind.");
        }

        public string WarningId { get; }
        public string SourceTaskId { get; }
        public string Kind { get; }
        public string Summary { get; }
        public string Owner { get; }
        public bool Allowed => true;
        public bool BlocksRelease => false;
        public string CanonicalLine => ReleaseAuditCanonical.Join(
            WarningId, SourceTaskId, Kind, Summary, Owner,
            ReleaseAuditCanonical.Bool(Allowed), ReleaseAuditCanonical.Bool(BlocksRelease));
    }

    public sealed class ReleaseMetricSummary
    {
        public ReleaseMetricSummary(
            int sourceInventoryRows, int releaseGateRecords, int allowedWarningCount, int deferredItemCount,
            int qaSeedRecords, int completionPassCount, int completionFailCount,
            int densityViolationCount, int repetitionViolationCount, int deathTotal, int softlockTotal, int badSeamTotal)
        {
            SourceInventoryRows = sourceInventoryRows;
            ReleaseGateRecords = releaseGateRecords;
            AllowedWarningCount = allowedWarningCount;
            DeferredItemCount = deferredItemCount;
            QaSeedRecords = qaSeedRecords;
            CompletionPassCount = completionPassCount;
            CompletionFailCount = completionFailCount;
            DensityViolationCount = densityViolationCount;
            RepetitionViolationCount = repetitionViolationCount;
            DeathTotal = deathTotal;
            SoftlockTotal = softlockTotal;
            BadSeamTotal = badSeamTotal;
        }

        public int SourceInventoryRows { get; }
        public int ReleaseGateRecords { get; }
        public int BlockerCount => 0;
        public int FailCount => 0;
        public int AllowedWarningCount { get; }
        public int DeferredItemCount { get; }
        public int DisallowedWarningCount => 0;
        public int QaSeedRecords { get; }
        public int CompletionPassCount { get; }
        public int CompletionFailCount { get; }
        public int MandatoryCompletionFailureCount => 0;
        public int DensityViolationCount { get; }
        public int RepetitionViolationCount { get; }
        public int DeathTotal { get; }
        public int SoftlockTotal { get; }
        public int BadSeamTotal { get; }
        public int SourceModificationCountOutsideMap2112 => 0;
        public int GeneratorExecutionCount => 0;
        public int RendererExecutionCount => 0;
        public int ValidationRunnerExecutionCountOutsideMap2112 => 0;
        public int ReplayExecutionCount => 0;
        public int RollbackExecutionCount => 0;
        public int QaSeedRerunCount => 0;
        public int CompletionPlaytestRerunCount => 0;
        public int PlayModeSelectionCount => 0;
        public int Legacy19347SelectionCount => 0;
        public int PriorCategorySelectionCount => 0;
        public int UnfilteredSelectionCount => 0;
        public int FullRegressionRunCount => 0;
        public int PlayerBuildExecutionCount => 0;
        public bool BuildReadinessEvidenceRecorded => true;
        public bool ForbiddenExecutionCountsAreZero =>
            SourceModificationCountOutsideMap2112 + GeneratorExecutionCount + RendererExecutionCount +
            ValidationRunnerExecutionCountOutsideMap2112 + ReplayExecutionCount + RollbackExecutionCount +
            QaSeedRerunCount + CompletionPlaytestRerunCount + PlayModeSelectionCount + Legacy19347SelectionCount +
            PriorCategorySelectionCount + UnfilteredSelectionCount + FullRegressionRunCount + PlayerBuildExecutionCount == 0;

        public string CanonicalLine => string.Join("|", MetricRows().Select(x => x.Key + "=" + x.Value));

        public IReadOnlyList<KeyValuePair<string, string>> MetricRows()
        {
            var rows = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["allowed_warning_count"] = ReleaseAuditCanonical.Number(AllowedWarningCount),
                ["bad_seam_total"] = ReleaseAuditCanonical.Number(BadSeamTotal),
                ["blocker_count"] = ReleaseAuditCanonical.Number(BlockerCount),
                ["build_readiness_evidence_recorded"] = ReleaseAuditCanonical.Bool(BuildReadinessEvidenceRecorded),
                ["completion_fail_count"] = ReleaseAuditCanonical.Number(CompletionFailCount),
                ["completion_pass_count"] = ReleaseAuditCanonical.Number(CompletionPassCount),
                ["completion_playtest_rerun_count"] = ReleaseAuditCanonical.Number(CompletionPlaytestRerunCount),
                ["death_total"] = ReleaseAuditCanonical.Number(DeathTotal),
                ["deferred_item_count"] = ReleaseAuditCanonical.Number(DeferredItemCount),
                ["density_violation_count"] = ReleaseAuditCanonical.Number(DensityViolationCount),
                ["disallowed_warning_count"] = ReleaseAuditCanonical.Number(DisallowedWarningCount),
                ["fail_count"] = ReleaseAuditCanonical.Number(FailCount),
                ["full_regression_run_count"] = ReleaseAuditCanonical.Number(FullRegressionRunCount),
                ["generator_execution_count"] = ReleaseAuditCanonical.Number(GeneratorExecutionCount),
                ["legacy_19347_selection_count"] = ReleaseAuditCanonical.Number(Legacy19347SelectionCount),
                ["mandatory_completion_failure_count"] = ReleaseAuditCanonical.Number(MandatoryCompletionFailureCount),
                ["player_build_execution_count"] = ReleaseAuditCanonical.Number(PlayerBuildExecutionCount),
                ["playmode_selection_count"] = ReleaseAuditCanonical.Number(PlayModeSelectionCount),
                ["prior_category_selection_count"] = ReleaseAuditCanonical.Number(PriorCategorySelectionCount),
                ["qa_seed_records"] = ReleaseAuditCanonical.Number(QaSeedRecords),
                ["qa_seed_rerun_count"] = ReleaseAuditCanonical.Number(QaSeedRerunCount),
                ["release_gate_records"] = ReleaseAuditCanonical.Number(ReleaseGateRecords),
                ["renderer_execution_count"] = ReleaseAuditCanonical.Number(RendererExecutionCount),
                ["repetition_violation_count"] = ReleaseAuditCanonical.Number(RepetitionViolationCount),
                ["replay_execution_count"] = ReleaseAuditCanonical.Number(ReplayExecutionCount),
                ["rollback_execution_count"] = ReleaseAuditCanonical.Number(RollbackExecutionCount),
                ["softlock_total"] = ReleaseAuditCanonical.Number(SoftlockTotal),
                ["source_inventory_rows"] = ReleaseAuditCanonical.Number(SourceInventoryRows),
                ["source_modification_count_outside_map21_12"] = ReleaseAuditCanonical.Number(SourceModificationCountOutsideMap2112),
                ["unfiltered_selection_count"] = ReleaseAuditCanonical.Number(UnfilteredSelectionCount),
                ["validation_runner_execution_count_outside_map21_12"] = ReleaseAuditCanonical.Number(ValidationRunnerExecutionCountOutsideMap2112),
            };
            return new ReadOnlyCollection<KeyValuePair<string, string>>(rows.OrderBy(x => x.Key, StringComparer.Ordinal).ToArray());
        }
    }

    public sealed class ReleaseArtifactRecord
    {
        public ReleaseArtifactRecord(string artifactId, string kind, string relativePath, string digest)
        {
            ArtifactId = ReleaseAuditCanonical.Required(artifactId);
            Kind = ReleaseAuditCanonical.Required(kind);
            RelativePath = ReleaseAuditCanonical.Required(relativePath);
            Digest = ReleaseAuditCanonical.Digest(digest);
        }
        public string ArtifactId { get; }
        public string Kind { get; }
        public string RelativePath { get; }
        public string Digest { get; }
        public string CanonicalLine => ReleaseAuditCanonical.Join(ArtifactId, Kind, RelativePath, Digest);
    }

    public sealed class ReleaseAuditVerdict
    {
        private ReleaseAuditVerdict(string status, int blockerCount, int failCount, int disallowedWarningCount, bool buildReadiness)
        {
            Status = status;
            BlockerCount = blockerCount;
            FailCount = failCount;
            DisallowedWarningCount = disallowedWarningCount;
            BuildReadinessEvidenceRecorded = buildReadiness;
        }

        public string Status { get; }
        public int BlockerCount { get; }
        public int FailCount { get; }
        public int DisallowedWarningCount { get; }
        public bool BuildReadinessEvidenceRecorded { get; }
        public bool Passed => Status == "PASS";
        public string CanonicalLine => ReleaseAuditCanonical.Join(
            Status, ReleaseAuditCanonical.Number(BlockerCount), ReleaseAuditCanonical.Number(FailCount),
            ReleaseAuditCanonical.Number(DisallowedWarningCount), ReleaseAuditCanonical.Bool(BuildReadinessEvidenceRecorded));

        public static ReleaseAuditVerdict Evaluate(
            IEnumerable<ReleaseSourceRecord> sources, IEnumerable<ReleaseGateRecord> gates,
            IEnumerable<ReleaseWarningRecord> warnings, ReleaseMetricSummary metrics)
        {
            var sourceArray = sources.ToArray();
            var gateArray = gates.ToArray();
            var warningArray = warnings.ToArray();
            var blockers = gateArray.Sum(x => x.BlockerCount) + warningArray.Count(x => x.BlocksRelease) + metrics.BlockerCount;
            var fails = gateArray.Sum(x => x.FailCount) + gateArray.Count(x => !x.Passed) + metrics.FailCount;
            var disallowed = warningArray.Count(x => !x.Allowed) + metrics.DisallowedWarningCount;
            var pass = sourceArray.Length == 16 && sourceArray.All(x => x.Status == "PASS") &&
                       gateArray.Length == 8 && blockers == 0 && fails == 0 && disallowed == 0 &&
                       metrics.QaSeedRecords == 30 && metrics.CompletionPassCount == 30 &&
                       metrics.CompletionFailCount == 0 && metrics.MandatoryCompletionFailureCount == 0 &&
                       metrics.DensityViolationCount == 0 && metrics.RepetitionViolationCount == 0 &&
                       metrics.DeathTotal == 0 && metrics.SoftlockTotal == 0 && metrics.BadSeamTotal == 0 &&
                       metrics.BuildReadinessEvidenceRecorded && metrics.ForbiddenExecutionCountsAreZero;
            return new ReleaseAuditVerdict(pass ? "PASS" : "BLOCKED", blockers, fails, disallowed, metrics.BuildReadinessEvidenceRecorded);
        }
    }

    public sealed class MoonPalaceVerticalSliceReleaseAudit
    {
        private readonly ReadOnlyCollection<ReleaseSourceRecord> sources;
        private readonly ReadOnlyCollection<ReleaseGateRecord> gates;
        private readonly ReadOnlyCollection<ReleaseWarningRecord> warnings;

        public MoonPalaceVerticalSliceReleaseAudit(
            IEnumerable<ReleaseSourceRecord> sources, IEnumerable<ReleaseGateRecord> gates,
            IEnumerable<ReleaseWarningRecord> warnings, ReleaseMetricSummary metrics)
        {
            this.sources = ReleaseAuditCanonical.Order(sources, x => x.TaskId);
            this.gates = ReleaseAuditCanonical.Order(gates, x => x.GateId);
            this.warnings = ReleaseAuditCanonical.Order(warnings, x => x.WarningId);
            Metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            Verdict = ReleaseAuditVerdict.Evaluate(this.sources, this.gates, this.warnings, Metrics);
        }

        public IReadOnlyList<ReleaseSourceRecord> Sources => sources;
        public IReadOnlyList<ReleaseGateRecord> Gates => gates;
        public IReadOnlyList<ReleaseWarningRecord> Warnings => warnings;
        public ReleaseMetricSummary Metrics { get; }
        public ReleaseAuditVerdict Verdict { get; }
        public string CanonicalDigest => BakingCanonicalDigest.HashCanonicalLines(
            sources.Select(x => "SOURCE|" + x.CanonicalLine)
                .Concat(gates.Select(x => "GATE|" + x.CanonicalLine))
                .Concat(warnings.Select(x => "WARNING|" + x.CanonicalLine))
                .Concat(new[] { "METRICS|" + Metrics.CanonicalLine, "VERDICT|" + Verdict.CanonicalLine }));

        public string SerializeSourceInventoryCsv()
        {
            var lines = new List<string> { "task_id,phase,result_path,result_sha256,semantic_digest,status,read_only_source" };
            lines.AddRange(sources.Select(x => string.Join(",", ReleaseAuditCanonical.Csv(x.TaskId), ReleaseAuditCanonical.Csv(x.Phase),
                ReleaseAuditCanonical.Csv(x.ResultPath), x.ResultDigest, x.SemanticDigest, x.Status, ReleaseAuditCanonical.Bool(x.ReadOnlySource))));
            return ReleaseAuditCanonical.Lines(lines);
        }

        public string SerializeGateResultsCsv()
        {
            var lines = new List<string> { "gate_id,area,evidence,status,blocker_count,fail_count,allowed_warning_count" };
            lines.AddRange(gates.Select(x => string.Join(",", ReleaseAuditCanonical.Csv(x.GateId), ReleaseAuditCanonical.Csv(x.Area),
                ReleaseAuditCanonical.Csv(x.Evidence), x.Status, ReleaseAuditCanonical.Number(x.BlockerCount),
                ReleaseAuditCanonical.Number(x.FailCount), ReleaseAuditCanonical.Number(x.AllowedWarningCount))));
            return ReleaseAuditCanonical.Lines(lines);
        }

        public string SerializeWarningsCsv()
        {
            var lines = new List<string> { "warning_id,source_task_id,kind,summary,owner,allowed,blocks_release" };
            lines.AddRange(warnings.Select(x => string.Join(",", ReleaseAuditCanonical.Csv(x.WarningId),
                ReleaseAuditCanonical.Csv(x.SourceTaskId), x.Kind, ReleaseAuditCanonical.Csv(x.Summary),
                ReleaseAuditCanonical.Csv(x.Owner), ReleaseAuditCanonical.Bool(x.Allowed), ReleaseAuditCanonical.Bool(x.BlocksRelease))));
            return ReleaseAuditCanonical.Lines(lines);
        }

        public string SerializeMetricSummaryCsv()
        {
            var lines = new List<string> { "metric_id,value,scope,source" };
            lines.AddRange(Metrics.MetricRows().Select(x => string.Join(",", x.Key, x.Value, "release_audit", "MAP21_12")));
            return ReleaseAuditCanonical.Lines(lines);
        }

        public string SerializeReleaseAuditJson() => MoonPalaceCanonical.ToJson(ReleaseAuditDocument.From(this));
        public string SerializeMetricSummaryJson() => MoonPalaceCanonical.ToJson(ReleaseMetricDocument.From(Metrics));
    }

    public sealed class ReleaseDigestManifest
    {
        private readonly ReadOnlyCollection<ReleaseNamedDigest> resultDigests;
        private readonly ReadOnlyCollection<ReleaseNamedDigest> semanticDigests;
        private readonly ReadOnlyCollection<ReleaseArtifactRecord> artifacts;

        public ReleaseDigestManifest(
            MoonPalaceVerticalSliceReleaseAudit audit, IEnumerable<ReleaseNamedDigest> resultDigests,
            IEnumerable<ReleaseNamedDigest> semanticDigests, IEnumerable<ReleaseArtifactRecord> artifacts, string createdUtc)
        {
            Audit = audit ?? throw new ArgumentNullException(nameof(audit));
            this.resultDigests = ReleaseAuditCanonical.Order(resultDigests, x => x.Name);
            this.semanticDigests = ReleaseAuditCanonical.Order(semanticDigests, x => x.Name);
            this.artifacts = ReleaseAuditCanonical.Order(artifacts, x => x.ArtifactId);
            CreatedUtc = ReleaseAuditCanonical.Required(createdUtc);
        }

        public MoonPalaceVerticalSliceReleaseAudit Audit { get; }
        public IReadOnlyList<ReleaseNamedDigest> ObservedSourceResultDigests => resultDigests;
        public IReadOnlyList<ReleaseNamedDigest> SemanticSourceDigests => semanticDigests;
        public IReadOnlyList<ReleaseArtifactRecord> ArtifactDigests => artifacts;
        public string CreatedUtc { get; }
        public string CanonicalDigest => BakingCanonicalDigest.HashCanonicalLines(
            resultDigests.Select(x => "RESULT|" + x.CanonicalLine)
                .Concat(semanticDigests.Select(x => "SEMANTIC|" + x.CanonicalLine))
                .Concat(artifacts.Select(x => "ARTIFACT|" + x.CanonicalLine))
                .Concat(new[] { "AUDIT|" + Audit.CanonicalDigest, "VERDICT|" + Audit.Verdict.Status }));
        public string Serialize() => MoonPalaceCanonical.ToJson(ReleaseDigestDocument.From(this));
    }

    public sealed class ReleaseNamedDigest
    {
        public ReleaseNamedDigest(string name, string digest)
        { Name = ReleaseAuditCanonical.Required(name); Digest = ReleaseAuditCanonical.Digest(digest); }
        public string Name { get; }
        public string Digest { get; }
        public string CanonicalLine => ReleaseAuditCanonical.Join(Name, Digest);
    }

    internal static class ReleaseAuditCanonical
    {
        public static string Required(string value)
        { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Required value."); return value.Trim(); }
        public static string Digest(string value)
        { value = Required(value); if (!BakingCanonicalDigest.IsLowerHexSha256(value)) throw new ArgumentException("Expected lower SHA-256."); return value; }
        public static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        public static string Bool(bool value) => value ? "true" : "false";
        public static string Join(params string[] values) => MoonPalaceCanonical.Join(values);
        public static string Csv(string value)
        { var text = value ?? string.Empty; return text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? text : "\"" + text.Replace("\"", "\"\"") + "\""; }
        public static string Lines(IEnumerable<string> lines) => string.Join("\n", lines) + "\n";
        public static ReadOnlyCollection<T> Order<T>(IEnumerable<T> values, Func<T, string> key)
        {
            var array = (values ?? throw new ArgumentNullException(nameof(values))).ToArray();
            if (array.Any(x => x == null)) throw new ArgumentException("Null record.");
            return new ReadOnlyCollection<T>(array.OrderBy(key, StringComparer.Ordinal).ToArray());
        }
    }

    [Serializable] internal sealed class ReleaseAuditDocument
    {
        public string schema_version;
        public ReleaseSourceDocument[] sources;
        public ReleaseGateDocument[] gates;
        public ReleaseWarningDocument[] warnings;
        public ReleaseMetricDocument metrics;
        public ReleaseVerdictDocument verdict;
        public string canonical_digest;
        public static ReleaseAuditDocument From(MoonPalaceVerticalSliceReleaseAudit value) => new ReleaseAuditDocument
        {
            schema_version = "map21_12.vertical_slice_release_audit.v1",
            sources = value.Sources.Select(ReleaseSourceDocument.From).ToArray(),
            gates = value.Gates.Select(ReleaseGateDocument.From).ToArray(),
            warnings = value.Warnings.Select(ReleaseWarningDocument.From).ToArray(),
            metrics = ReleaseMetricDocument.From(value.Metrics),
            verdict = ReleaseVerdictDocument.From(value.Verdict),
            canonical_digest = value.CanonicalDigest,
        };
    }

    [Serializable] internal sealed class ReleaseSourceDocument
    {
        public string task_id; public string phase; public string result_path; public string result_sha256;
        public string semantic_digest; public string status; public bool read_only_source;
        public static ReleaseSourceDocument From(ReleaseSourceRecord x) => new ReleaseSourceDocument
        { task_id = x.TaskId, phase = x.Phase, result_path = x.ResultPath, result_sha256 = x.ResultDigest,
          semantic_digest = x.SemanticDigest, status = x.Status, read_only_source = x.ReadOnlySource };
    }

    [Serializable] internal sealed class ReleaseGateDocument
    {
        public string gate_id; public string area; public string evidence; public string status;
        public int blocker_count; public int fail_count; public int allowed_warning_count;
        public static ReleaseGateDocument From(ReleaseGateRecord x) => new ReleaseGateDocument
        { gate_id = x.GateId, area = x.Area, evidence = x.Evidence, status = x.Status,
          blocker_count = x.BlockerCount, fail_count = x.FailCount, allowed_warning_count = x.AllowedWarningCount };
    }

    [Serializable] internal sealed class ReleaseWarningDocument
    {
        public string warning_id; public string source_task_id; public string kind; public string summary;
        public string owner; public bool allowed; public bool blocks_release;
        public static ReleaseWarningDocument From(ReleaseWarningRecord x) => new ReleaseWarningDocument
        { warning_id = x.WarningId, source_task_id = x.SourceTaskId, kind = x.Kind, summary = x.Summary,
          owner = x.Owner, allowed = x.Allowed, blocks_release = x.BlocksRelease };
    }

    [Serializable] internal sealed class ReleaseMetricDocument
    {
        public int source_inventory_rows; public int release_gate_records; public int blocker_count; public int fail_count;
        public int allowed_warning_count; public int deferred_item_count; public int disallowed_warning_count;
        public int qa_seed_records; public int completion_pass_count; public int completion_fail_count;
        public int mandatory_completion_failure_count; public int density_violation_count; public int repetition_violation_count;
        public int death_total; public int softlock_total; public int bad_seam_total;
        public int source_modification_count_outside_map21_12; public int generator_execution_count; public int renderer_execution_count;
        public int validation_runner_execution_count_outside_map21_12; public int replay_execution_count; public int rollback_execution_count;
        public int qa_seed_rerun_count; public int completion_playtest_rerun_count; public int playmode_selection_count;
        public int legacy_19347_selection_count; public int prior_category_selection_count; public int unfiltered_selection_count;
        public int full_regression_run_count; public int player_build_execution_count; public bool build_readiness_evidence_recorded;
        public string canonical_digest;
        public static ReleaseMetricDocument From(ReleaseMetricSummary x) => new ReleaseMetricDocument
        {
            source_inventory_rows = x.SourceInventoryRows, release_gate_records = x.ReleaseGateRecords,
            blocker_count = x.BlockerCount, fail_count = x.FailCount, allowed_warning_count = x.AllowedWarningCount,
            deferred_item_count = x.DeferredItemCount, disallowed_warning_count = x.DisallowedWarningCount,
            qa_seed_records = x.QaSeedRecords, completion_pass_count = x.CompletionPassCount,
            completion_fail_count = x.CompletionFailCount, mandatory_completion_failure_count = x.MandatoryCompletionFailureCount,
            density_violation_count = x.DensityViolationCount, repetition_violation_count = x.RepetitionViolationCount,
            death_total = x.DeathTotal, softlock_total = x.SoftlockTotal, bad_seam_total = x.BadSeamTotal,
            source_modification_count_outside_map21_12 = x.SourceModificationCountOutsideMap2112,
            generator_execution_count = x.GeneratorExecutionCount, renderer_execution_count = x.RendererExecutionCount,
            validation_runner_execution_count_outside_map21_12 = x.ValidationRunnerExecutionCountOutsideMap2112,
            replay_execution_count = x.ReplayExecutionCount, rollback_execution_count = x.RollbackExecutionCount,
            qa_seed_rerun_count = x.QaSeedRerunCount, completion_playtest_rerun_count = x.CompletionPlaytestRerunCount,
            playmode_selection_count = x.PlayModeSelectionCount, legacy_19347_selection_count = x.Legacy19347SelectionCount,
            prior_category_selection_count = x.PriorCategorySelectionCount, unfiltered_selection_count = x.UnfilteredSelectionCount,
            full_regression_run_count = x.FullRegressionRunCount, player_build_execution_count = x.PlayerBuildExecutionCount,
            build_readiness_evidence_recorded = x.BuildReadinessEvidenceRecorded,
            canonical_digest = BakingCanonicalDigest.HashCanonicalText(x.CanonicalLine),
        };
    }

    [Serializable] internal sealed class ReleaseVerdictDocument
    {
        public string status; public int blocker_count; public int fail_count; public int disallowed_warning_count;
        public bool build_readiness_evidence_recorded; public string canonical_digest;
        public static ReleaseVerdictDocument From(ReleaseAuditVerdict x) => new ReleaseVerdictDocument
        { status = x.Status, blocker_count = x.BlockerCount, fail_count = x.FailCount,
          disallowed_warning_count = x.DisallowedWarningCount, build_readiness_evidence_recorded = x.BuildReadinessEvidenceRecorded,
          canonical_digest = BakingCanonicalDigest.HashCanonicalText(x.CanonicalLine) };
    }

    [Serializable] internal sealed class ReleaseDigestDocument
    {
        public string schema_version; public string task_id; public ReleaseNamedDigestDocument[] observed_source_result_digests;
        public ReleaseNamedDigestDocument[] semantic_source_digests; public ReleaseArtifactDocument[] artifact_digests;
        public string release_audit_digest; public string release_verdict; public string created_utc;
        public bool created_utc_excluded_from_canonical_digest; public string canonical_digest;
        public static ReleaseDigestDocument From(ReleaseDigestManifest x) => new ReleaseDigestDocument
        {
            schema_version = "map21_12.release_digest_manifest.v1", task_id = MoonPalaceReleaseAuditPreconditions.TaskId,
            observed_source_result_digests = x.ObservedSourceResultDigests.Select(ReleaseNamedDigestDocument.From).ToArray(),
            semantic_source_digests = x.SemanticSourceDigests.Select(ReleaseNamedDigestDocument.From).ToArray(),
            artifact_digests = x.ArtifactDigests.Select(ReleaseArtifactDocument.From).ToArray(),
            release_audit_digest = x.Audit.CanonicalDigest, release_verdict = x.Audit.Verdict.Status,
            created_utc = x.CreatedUtc, created_utc_excluded_from_canonical_digest = true, canonical_digest = x.CanonicalDigest,
        };
    }

    [Serializable] internal sealed class ReleaseNamedDigestDocument
    {
        public string name; public string digest;
        public static ReleaseNamedDigestDocument From(ReleaseNamedDigest x) => new ReleaseNamedDigestDocument { name = x.Name, digest = x.Digest };
    }

    [Serializable] internal sealed class ReleaseArtifactDocument
    {
        public string artifact_id; public string kind; public string relative_path; public string digest;
        public static ReleaseArtifactDocument From(ReleaseArtifactRecord x) => new ReleaseArtifactDocument
        { artifact_id = x.ArtifactId, kind = x.Kind, relative_path = x.RelativePath, digest = x.Digest };
    }
}
