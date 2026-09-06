using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MoonPalace;
using UnityEngine;

namespace StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace
{
    public static class MoonPalaceVerticalSliceReleaseAuditPublisher
    {
        public const string AuthoringDirectoryRelativePath = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_12";
        public const string GeneratedDirectoryRelativePath = "MapDesign/MCP/GENERATED/MAP21_12";
        public const string SourceInventoryFileName = "moonpalace_release_source_inventory.csv";
        public const string GateResultsFileName = "moonpalace_release_gate_results.csv";
        public const string WarningItemsFileName = "moonpalace_release_warn_deferred_items.csv";
        public const string MetricSummaryCsvFileName = "moonpalace_release_metric_summary.csv";
        public const string ArtifactManifestFileName = "moonpalace_release_artifact_manifest.csv";
        public const string ReleaseAuditFileName = "moonpalace_vertical_slice_release_audit.json";
        public const string MetricSummaryJsonFileName = "moonpalace_release_metric_summary.json";
        public const string DigestManifestFileName = "moonpalace_release_digest_manifest.json";

        private const string Map2111TaskPath = "MapDesign/MCP/TASKS/MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS.md";
        private const string Map1909SummaryPath = "MapDesign/MCP/GENERATED/MAP19_09/scale_audit_summary.json";
        private const string Map2006DigestPath = "MapDesign/MCP/GENERATED/MAP20_06/map20_digest_chain_manifest.json";
        private const string Map2110DigestPath = "MapDesign/MCP/GENERATED/MAP21_10/moonpalace_tuning_digest_manifest.json";
        private const string Map2111DigestPath = "MapDesign/MCP/GENERATED/MAP21_11/moonpalace_qa_digest_manifest.json";
        private const string Map2111FailurePath = "MapDesign/MCP/GENERATED/MAP21_11/moonpalace_qa_failure_bundle_index.json";
        private const string Map2111SeedPath = "MapDesign/MCP/GENERATED/MAP21_11/moonpalace_qa_seed_lock_manifest.json";
        private const string Map2111CompletionPath = "MapDesign/MCP/GENERATED/MAP21_11/moonpalace_completion_playtest_manifest.json";
        private const string Map2111TelemetryPath = "MapDesign/MCP/GENERATED/MAP21_11/moonpalace_completion_telemetry_summary.json";
        private const string Map2111DensityPath = "MapDesign/MCP/GENERATED/MAP21_11/moonpalace_density_repetition_measurement.json";

        private static readonly ResultSpec[] ResultSpecs =
        {
            R("MAP17_08_MAP17_RUNTIME_EXIT_AUDIT", "MAP17"),
            R("MAP18_07_MAP18_POPULATION_EXIT_TESTS", "MAP18"),
            R("MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER", "MAP19"),
            R("MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT", "MAP19"),
            R("MAP20_06_MAP20_TOOLING_EXIT_TESTS", "MAP20"),
            R("MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL", "MAP21"),
            R("MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS", "MAP21"),
            R("MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS", "MAP21"),
            R("MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS", "MAP21"),
            R("MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS", "MAP21"),
            R("MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS", "MAP21"),
            R("MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS", "MAP21"),
            R("MAP21_08_COMPLETE_MOONPALACE_VILLAGE", "MAP21"),
            R("MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS", "MAP21"),
            R("MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING", "MAP21"),
            R("MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS", "MAP21"),
        };

        public static IReadOnlyList<string> GetSourceReadRelativePaths()
        {
            return new ReadOnlyCollection<string>(ResultSpecs.Select(x => x.ResultPath)
                .Concat(new[]
                {
                    Map2111TaskPath, Map1909SummaryPath, Map2006DigestPath, Map2110DigestPath, Map2111DigestPath,
                    Map2111FailurePath, Map2111SeedPath, Map2111CompletionPath, Map2111TelemetryPath, Map2111DensityPath,
                })
                .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        }

        public static MoonPalaceReleasePublishedSample BuildSnapshot(
            string projectRoot, string createdUtc, bool reverseSourceInput)
        {
            var root = RequireRoot(projectRoot);
            var texts = GetSourceReadRelativePaths().ToDictionary(x => x, x => Read(root, x), StringComparer.Ordinal);
            var strictResult = ResultSpecs.Single(x => x.TaskId == "MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS");
            Require(FileSha(Resolve(root, strictResult.ResultPath)) == MoonPalaceReleaseAuditPreconditions.StrictMap2111ResultDigest,
                "MAP21_11 Result strict SHA mismatch.");
            Require(FileSha(Resolve(root, Map2111TaskPath)) == MoonPalaceReleaseAuditPreconditions.StrictMap2111TaskDigest,
                "MAP21_11 installed Task strict SHA mismatch.");

            var semanticByTask = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["MAP17_08_MAP17_RUNTIME_EXIT_AUDIT"] = MoonPalaceReleaseAuditPreconditions.Map1708AuditDigest,
                ["MAP18_07_MAP18_POPULATION_EXIT_TESTS"] = MoonPalaceReleaseAuditPreconditions.Map1807AuditDigest,
                ["MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER"] = MoonPalaceReleaseAuditPreconditions.Map1908FailureSchemaDigest,
                ["MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT"] = MoonPalaceReleaseAuditPreconditions.Map1909ExitDigest,
                ["MAP20_06_MAP20_TOOLING_EXIT_TESTS"] = MoonPalaceReleaseAuditPreconditions.Map2006ExitDigest,
                ["MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING"] = MoonPalaceReleaseAuditPreconditions.Map2110TuningDigest,
                ["MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS"] = MoonPalaceReleaseAuditPreconditions.Map2111QaDigest,
            };

            var sources = ResultSpecs.Select(spec =>
            {
                var text = texts[spec.ResultPath];
                RequireExactLine(text, "TASK: " + spec.TaskId);
                RequireExactLine(text, "STATUS: PASS");
                var digest = FileSha(Resolve(root, spec.ResultPath));
                return new ReleaseSourceRecord(spec.TaskId, spec.Phase, spec.ResultPath, digest,
                    semanticByTask.TryGetValue(spec.TaskId, out var semantic) ? semantic : "NONE");
            }).ToArray();

            RequireContains(texts[ResultSpecs[0].ResultPath], MoonPalaceReleaseAuditPreconditions.Map1708AuditDigest);
            RequireContains(texts[ResultSpecs[1].ResultPath], MoonPalaceReleaseAuditPreconditions.Map1807AuditDigest);
            RequireContains(texts[ResultSpecs[2].ResultPath], MoonPalaceReleaseAuditPreconditions.Map1908FailureSchemaDigest);

            var map1909 = JsonUtility.FromJson<Map1909Document>(texts[Map1909SummaryPath]);
            Require(map1909.status == "Pass" && map1909.executedSeeds == 100000 && map1909.passedSeeds == 100000 &&
                    map1909.failureBundleCount == 0 && map1909.map19ExitDigest == MoonPalaceReleaseAuditPreconditions.Map1909ExitDigest,
                "MAP19 scale readiness mismatch.");
            var map2006 = JsonUtility.FromJson<Map2006Document>(texts[Map2006DigestPath]);
            Require(map2006.map20_phase_exit_digest == MoonPalaceReleaseAuditPreconditions.Map2006ExitDigest,
                "MAP20 exit digest mismatch.");
            var map2110 = JsonUtility.FromJson<Map2110Document>(texts[Map2110DigestPath]);
            Require(map2110.canonical_digest == MoonPalaceReleaseAuditPreconditions.Map2110TuningDigest,
                "MAP21_10 tuning digest mismatch.");
            var map2111 = JsonUtility.FromJson<Map2111DigestDocument>(texts[Map2111DigestPath]);
            Require(map2111.canonical_digest == MoonPalaceReleaseAuditPreconditions.Map2111QaDigest &&
                    map2111.MAP21_12_handoff_digest == MoonPalaceReleaseAuditPreconditions.StrictMap2112HandoffDigest,
                "MAP21_11 QA digest or handoff mismatch.");
            var failures = JsonUtility.FromJson<Map2111FailureDocument>(texts[Map2111FailurePath]);
            var seeds = JsonUtility.FromJson<Map2111SeedDocument>(texts[Map2111SeedPath]);
            var completion = JsonUtility.FromJson<Map2111CompletionDocument>(texts[Map2111CompletionPath]);
            var telemetry = JsonUtility.FromJson<Map2111TelemetryDocument>(texts[Map2111TelemetryPath]);
            var density = JsonUtility.FromJson<Map2111DensityDocument>(texts[Map2111DensityPath]);
            Require(failures.failure_bundle_count == 0 && seeds.generated_seed_count == 30 &&
                    seeds.production_seed_approval_count == 30 && seeds.unique_seed_value_count == 30 &&
                    seeds.unique_content_hash_count == 30 && !seeds.manual_seed_substitution_allowed,
                "MAP21_11 seed/failure evidence mismatch.");
            Require(completion.scenario_count == 30 && completion.completion_executed_count == 30 &&
                    completion.completion_pass_count == 30 && completion.completion_fail_count == 0 &&
                    completion.missing_mandatory_checkpoint_count == 0,
                "MAP21_11 completion evidence mismatch.");
            Require(telemetry.death_count_total == 0 && telemetry.softlock_count_total == 0 &&
                    telemetry.bad_seam_count_total == 0 && density.density_window_violation_count == 0 &&
                    density.repetition_rule_violation_count == 0,
                "MAP21_11 telemetry evidence mismatch.");

            ValidateWarningProvenance(texts);
            var warnings = CreateWarnings();
            var metrics = new ReleaseMetricSummary(16, 8, warnings.Count(x => x.Kind == "WARN"),
                warnings.Count(x => x.Kind == "DEFERRED"), 30, completion.completion_pass_count,
                completion.completion_fail_count, density.density_window_violation_count,
                density.repetition_rule_violation_count, telemetry.death_count_total,
                telemetry.softlock_count_total, telemetry.bad_seam_count_total);
            var gates = CreateGates();
            if (reverseSourceInput)
            {
                Array.Reverse(sources);
                Array.Reverse(gates);
                Array.Reverse(warnings);
            }

            var audit = new MoonPalaceVerticalSliceReleaseAudit(sources, gates, warnings, metrics);
            Require(audit.Verdict.Passed, "Release audit verdict did not pass.");

            var contents = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [Combine(AuthoringDirectoryRelativePath, SourceInventoryFileName)] = audit.SerializeSourceInventoryCsv(),
                [Combine(AuthoringDirectoryRelativePath, GateResultsFileName)] = audit.SerializeGateResultsCsv(),
                [Combine(AuthoringDirectoryRelativePath, WarningItemsFileName)] = audit.SerializeWarningsCsv(),
                [Combine(AuthoringDirectoryRelativePath, MetricSummaryCsvFileName)] = audit.SerializeMetricSummaryCsv(),
                [Combine(GeneratedDirectoryRelativePath, ReleaseAuditFileName)] = audit.SerializeReleaseAuditJson(),
                [Combine(GeneratedDirectoryRelativePath, MetricSummaryJsonFileName)] = audit.SerializeMetricSummaryJson(),
            };
            var artifacts = contents.OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select((x, index) => new ReleaseArtifactRecord(
                    "ARTIFACT_" + (index + 1).ToString("D2", CultureInfo.InvariantCulture),
                    x.Key.EndsWith(".csv", StringComparison.Ordinal) ? "CSV" : "JSON", x.Key,
                    BakingCanonicalDigest.HashCanonicalText(x.Value))).ToList();
            var artifactCsv = SerializeArtifactManifestCsv(artifacts);
            var artifactPath = Combine(AuthoringDirectoryRelativePath, ArtifactManifestFileName);
            contents[artifactPath] = artifactCsv;
            artifacts.Add(new ReleaseArtifactRecord("ARTIFACT_07", "CSV", artifactPath,
                BakingCanonicalDigest.HashCanonicalText(artifactCsv)));

            var resultDigests = sources.Select(x => new ReleaseNamedDigest(x.TaskId, x.ResultDigest));
            var semanticDigests = semanticByTask.Select(x => new ReleaseNamedDigest(x.Key, x.Value));
            var digest = new ReleaseDigestManifest(audit, resultDigests, semanticDigests, artifacts, createdUtc);
            contents[Combine(GeneratedDirectoryRelativePath, DigestManifestFileName)] = digest.Serialize();
            return new MoonPalaceReleasePublishedSample(audit, digest, contents, GetSourceReadRelativePaths());
        }

        public static MoonPalaceReleasePublishedSample Publish(string projectRoot)
        {
            var root = RequireRoot(projectRoot);
            var sample = BuildSnapshot(root, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), false);
            Directory.CreateDirectory(Resolve(root, AuthoringDirectoryRelativePath));
            Directory.CreateDirectory(Resolve(root, GeneratedDirectoryRelativePath));
            foreach (var output in sample.OutputContents)
                File.WriteAllText(Resolve(root, output.Key), output.Value, BakingCanonicalDigest.Utf8NoBomEncoding);
            return sample;
        }

        private static ReleaseGateRecord[] CreateGates() => new[]
        {
            G("GATE_01_RUNTIME_BAKE_STREAM_SAVE", "Runtime bake/stream/save readiness", "MAP17_08 PASS; audit digest; 14 PASS/2 allowed WARN/0 BLOCK/0 FAIL", 2),
            G("GATE_02_POPULATION_SPECIAL_STATE", "Population/special state readiness", "MAP18_07 PASS; approved audit digest; mandatory blockers 0", 1),
            G("GATE_03_VALIDATION_SCALE", "Validation scale readiness", "MAP19_09 PASS; 100000/100000; failure bundles 0; exit digest", 1),
            G("GATE_04_FAILURE_SCHEMA", "Failure bundle schema readiness", "MAP19_08 PASS; MAP19_FAILURE_BUNDLE_V1 semantic digest available", 0),
            G("GATE_05_TOOLING_DEBUG", "Tooling/debug readiness", "MAP20_06 PASS; phase exit digest; execution not repeated", 0),
            G("GATE_06_PRODUCTION_CONTENT", "Production content readiness", "MAP21_01..09 PASS; MAP21_10 tuning digest bound", 0),
            G("GATE_07_QA_COMPLETION", "QA completion readiness", "MAP21_11 PASS; 30/30 completion; zero telemetry violations", 1),
            G("GATE_08_RELEASE_BOUNDARY", "Release boundary readiness", "MAP21_12 read-only audit; generator/seed/playtest/build/legacy/full executions 0", 0),
        };

        private static ReleaseWarningRecord[] CreateWarnings() => new[]
        {
            W("DEFER_01_MAP17_LIVE_TRAVERSAL", "MAP17_08_MAP17_RUNTIME_EXIT_AUDIT", "DEFERRED", "Actual live player traversal proof remains outside this static release audit.", "LATER_PLAYMODE_LIVE_INTEGRATION_TASK"),
            W("DEFER_02_MAP17_DISK_SAVE", "MAP17_08_MAP17_RUNTIME_EXIT_AUDIT", "DEFERRED", "Actual disk save slot and platform storage remain outside this static release audit.", "LATER_SAVE_SYSTEM_INTEGRATION_TASK"),
            W("DEFER_03_MAP17_OPTIMIZATION", "MAP17_08_MAP17_RUNTIME_EXIT_AUDIT", "DEFERRED", "Observed layer bake spike optimization is explicitly deferred and did not block phase exit.", "LATER_APPROVED_OPTIMIZATION_TASK"),
            W("DEFER_04_MAP17_FIXTURE_CLEANUP", "MAP17_08_MAP17_RUNTIME_EXIT_AUDIT", "DEFERRED", "Shared fixture consolidation is cleanup-only and did not block phase exit.", "LATER_APPROVED_CLEANUP_TASK"),
            W("DEFER_05_MAP18_SHARED_FIXTURE", "MAP18_07_MAP18_POPULATION_EXIT_TESTS", "DEFERRED", "Private fixture duplication may be consolidated only by a separate cleanup task.", "LATER_APPROVED_CLEANUP_TASK"),
            W("DEFER_06_MAP21_OPTIONAL_LANDMARK_RUNTIME", "MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS", "DEFERRED", "Merchant and Maru remain optional deferred-local content and are not completion blockers.", "LATER_OPTIONAL_RUNTIME_TASK"),
            W("WARN_01_MAP17_LAYER_BAKE_SPIKE", "MAP17_08_MAP17_RUNTIME_EXIT_AUDIT", "WARN", "Layer bake max 3358.202900 ms had no strict millisecond gate and no digest mismatch.", "LATER_APPROVED_OPTIMIZATION_TASK"),
            W("WARN_02_MAP17_DUPLICATION_BUDGET", "MAP17_08_MAP17_RUNTIME_EXIT_AUDIT", "WARN", "One duplicate fixture adapter and named budget constants were accepted as non-blocking risk.", "LATER_APPROVED_CLEANUP_TASK"),
            W("WARN_03_MAP18_INPUT_MANAGER", "MAP18_07_MAP18_POPULATION_EXIT_TESTS", "WARN", "Pre-existing legacy Input Manager deprecation warning was unrelated and non-blocking.", "PROJECT_INFRASTRUCTURE"),
            W("WARN_04_MAP19_EMPTY_ASMDEF", "MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT", "WARN", "Pre-existing empty legacy asmdef warning was unchanged and out of scope.", "PROJECT_INFRASTRUCTURE"),
            W("WARN_05_MAP21_11_CLEANUP_VERIFIER", "MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS", "WARN", "Cleanup verifier reported intentional permanent MAP21_11 output files; focused tests passed.", "UNITY_TEST_INFRASTRUCTURE"),
        };

        private static void ValidateWarningProvenance(IReadOnlyDictionary<string, string> texts)
        {
            RequireContains(texts[ResultSpecs[0].ResultPath], "14 PASS / 2 WARN / 0 BLOCK / 0 FAIL");
            RequireContains(texts[ResultSpecs[0].ResultPath], "LATER_PLAYMODE_LIVE_INTEGRATION_TASK");
            RequireContains(texts[ResultSpecs[0].ResultPath], "LATER_SAVE_SYSTEM_INTEGRATION_TASK");
            RequireContains(texts[ResultSpecs[0].ResultPath], "LATER_APPROVED_OPTIMIZATION_TASK");
            RequireContains(texts[ResultSpecs[0].ResultPath], "LATER_APPROVED_CLEANUP_TASK");
            RequireContains(texts[ResultSpecs[1].ResultPath], "legacy Input Manager deprecation; no task failure");
            RequireContains(texts[ResultSpecs[3].ResultPath], "pre-existing empty legacy asmdef warning; unchanged and out of scope");
            RequireContains(texts[ResultSpecs[13].ResultPath], "deferred optional records: 2");
            RequireContains(texts[ResultSpecs[15].ResultPath], "Files generated by test without cleanup");
        }

        private static string SerializeArtifactManifestCsv(IEnumerable<ReleaseArtifactRecord> artifacts)
        {
            var lines = new List<string> { "artifact_id,kind,relative_path,sha256,self_reference_policy" };
            lines.AddRange(artifacts.OrderBy(x => x.ArtifactId, StringComparer.Ordinal)
                .Select(x => string.Join(",", x.ArtifactId, x.Kind, Csv(x.RelativePath), x.Digest, "digest_manifest_self_excluded")));
            return string.Join("\n", lines) + "\n";
        }

        private static ResultSpec R(string taskId, string phase) => new ResultSpec(taskId, phase,
            "MapDesign/MCP/REPORTS/" + taskId + "_RESULT.md");
        private static ReleaseGateRecord G(string id, string area, string evidence, int warnings) =>
            new ReleaseGateRecord(id, area, evidence, "PASS", 0, 0, warnings);
        private static ReleaseWarningRecord W(string id, string task, string kind, string summary, string owner) =>
            new ReleaseWarningRecord(id, task, kind, summary, owner);
        private static string Csv(string value)
        { return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? value : "\"" + value.Replace("\"", "\"\"") + "\""; }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static void RequireContains(string text, string value) => Require(text.Contains(value), "Missing semantic evidence: " + value);
        private static void RequireExactLine(string text, string line) =>
            Require(text.Replace("\r\n", "\n").Split('\n').Count(x => x == line) == 1, "Expected exact line: " + line);
        private static string Combine(string left, string right) => left.TrimEnd('/') + "/" + right;
        private static string Read(string root, string relative) => File.ReadAllText(Resolve(root, relative));
        private static string RequireRoot(string root)
        { if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException(nameof(root)); return Path.GetFullPath(root); }
        private static string Resolve(string root, string relative) =>
            Path.GetFullPath(Path.Combine(RequireRoot(root), relative.Replace('/', Path.DirectorySeparatorChar)));
        private static string FileSha(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(stream).Select(x => x.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private sealed class ResultSpec
        {
            public ResultSpec(string taskId, string phase, string resultPath)
            { TaskId = taskId; Phase = phase; ResultPath = resultPath; }
            public string TaskId { get; }
            public string Phase { get; }
            public string ResultPath { get; }
        }

        [Serializable] private sealed class Map1909Document
        { public string status; public int executedSeeds; public int passedSeeds; public int failureBundleCount; public string map19ExitDigest; }
        [Serializable] private sealed class Map2006Document { public string map20_phase_exit_digest; }
        [Serializable] private sealed class Map2110Document { public string canonical_digest; }
        [Serializable] private sealed class Map2111DigestDocument { public string canonical_digest; public string MAP21_12_handoff_digest; }
        [Serializable] private sealed class Map2111FailureDocument { public int failure_bundle_count; }
        [Serializable] private sealed class Map2111SeedDocument
        {
            public int generated_seed_count; public int production_seed_approval_count; public int unique_seed_value_count;
            public int unique_content_hash_count; public bool manual_seed_substitution_allowed;
        }
        [Serializable] private sealed class Map2111CompletionDocument
        {
            public int scenario_count; public int completion_executed_count; public int completion_pass_count;
            public int completion_fail_count; public int missing_mandatory_checkpoint_count;
        }
        [Serializable] private sealed class Map2111TelemetryDocument
        { public int death_count_total; public int softlock_count_total; public int bad_seam_count_total; }
        [Serializable] private sealed class Map2111DensityDocument
        { public int density_window_violation_count; public int repetition_rule_violation_count; }
    }

    public sealed class MoonPalaceReleasePublishedSample
    {
        private readonly ReadOnlyDictionary<string, string> outputs;
        private readonly ReadOnlyCollection<string> sources;
        public MoonPalaceReleasePublishedSample(
            MoonPalaceVerticalSliceReleaseAudit audit, ReleaseDigestManifest digest,
            IDictionary<string, string> outputs, IEnumerable<string> sources)
        {
            Audit = audit ?? throw new ArgumentNullException(nameof(audit));
            DigestManifest = digest ?? throw new ArgumentNullException(nameof(digest));
            this.outputs = new ReadOnlyDictionary<string, string>(
                new SortedDictionary<string, string>(outputs, StringComparer.Ordinal));
            this.sources = new ReadOnlyCollection<string>(sources.OrderBy(x => x, StringComparer.Ordinal).ToArray());
        }
        public MoonPalaceVerticalSliceReleaseAudit Audit { get; }
        public ReleaseDigestManifest DigestManifest { get; }
        public IReadOnlyDictionary<string, string> OutputContents => outputs;
        public IReadOnlyList<string> SourceReadRelativePaths => sources;
    }
}
