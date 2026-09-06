using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MoonPalace;
using StarNight.Map.WorldGeneration.Validation;
using UnityEngine;

namespace StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace
{
    public static class MoonPalaceQaSeedPublisher
    {
        public const string AuthoringDirectoryRelativePath = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_11";
        public const string GeneratedDirectoryRelativePath = "MapDesign/MCP/GENERATED/MAP21_11";
        public const string SeedManifestFileName = "moonpalace_qa_seed_manifest.csv";
        public const string CompletionScenariosFileName = "moonpalace_completion_scenarios.csv";
        public const string TelemetryTargetsFileName = "moonpalace_completion_telemetry_targets.csv";
        public const string FailureIndexCsvFileName = "moonpalace_failure_bundle_index.csv";
        public const string ReleaseHandoffFileName = "moonpalace_release_audit_handoff.csv";
        public const string SeedLockManifestFileName = "moonpalace_qa_seed_lock_manifest.json";
        public const string CompletionManifestFileName = "moonpalace_completion_playtest_manifest.json";
        public const string TelemetrySummaryFileName = "moonpalace_completion_telemetry_summary.json";
        public const string DensityRepetitionFileName = "moonpalace_density_repetition_measurement.json";
        public const string FailureIndexJsonFileName = "moonpalace_qa_failure_bundle_index.json";
        public const string DigestManifestFileName = "moonpalace_qa_digest_manifest.json";
        public const string Map2110TaskPath = "MapDesign/MCP/TASKS/MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING.md";
        public const string Map1909SummaryPath = "MapDesign/MCP/GENERATED/MAP19_09/scale_audit_summary.json";
        public const string Map2006DigestPath = "MapDesign/MCP/GENERATED/MAP20_06/map20_digest_chain_manifest.json";
        public const string Map2110DigestPath = "MapDesign/MCP/GENERATED/MAP21_10/moonpalace_tuning_digest_manifest.json";
        public const string Map2110ProfilePath = "MapDesign/MCP/GENERATED/MAP21_10/moonpalace_tuning_profile_manifest.json";
        public const string Map2110HandoffPath = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_10/moonpalace_tuning_handoff.csv";

        private static readonly ResultSpec[] ResultSpecs =
        {
            R("MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER"),
            R("MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT"), R("MAP20_06_MAP20_TOOLING_EXIT_TESTS"),
            R("MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL"), R("MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS"),
            R("MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS"), R("MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS"),
            R("MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS"), R("MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS"),
            R("MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS"), R("MAP21_08_COMPLETE_MOONPALACE_VILLAGE"),
            R("MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS"), R("MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING"),
        };

        public static MoonPalaceQaPublishedSample ExecuteFocusedQaAndPublish(
            string projectRoot, bool focusedMap2111RunAuthorized)
        {
            if (!focusedMap2111RunAuthorized) throw new InvalidOperationException(
                "MAP21_11 requires one focused thirty-seed QA execution.");
            projectRoot = RequireRoot(projectRoot);
            var observed = ValidateSources(projectRoot);
            var createdUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var qa = MoonPalaceQaSeedSet.ExecuteThirtyFocusedRuns(createdUtc);
            if (qa.HasFailure)
            {
                PublishFirstFailureBundle(projectRoot, qa, createdUtc);
                throw new MoonPalaceQaSeedFailureException("A locked QA seed failed; failure bundle written without auto-repair.");
            }
            Directory.CreateDirectory(Resolve(projectRoot, AuthoringDirectoryRelativePath));
            Directory.CreateDirectory(Resolve(projectRoot, GeneratedDirectoryRelativePath));
            var seedJson = qa.SerializeSeedLockManifest();
            var completionJson = qa.SerializeCompletionManifest();
            var telemetryJson = qa.SerializeTelemetrySummary();
            var densityJson = qa.SerializeDensityRepetitionMeasurement();
            var failureJson = qa.SerializeFailureIndexManifest();
            var csvDigests = new[]
            {
                Named(SeedManifestFileName, qa.SerializeSeedManifestCsv()),
                Named(CompletionScenariosFileName, qa.SerializeCompletionScenariosCsv()),
                Named(TelemetryTargetsFileName, qa.SerializeTelemetryTargetsCsv()),
                Named(FailureIndexCsvFileName, qa.SerializeFailureIndexCsv()),
                Named(ReleaseHandoffFileName, qa.SerializeReleaseHandoffCsv()),
            };
            var jsonDigests = new[]
            {
                Named(SeedLockManifestFileName, seedJson), Named(CompletionManifestFileName, completionJson),
                Named(TelemetrySummaryFileName, telemetryJson), Named(DensityRepetitionFileName, densityJson),
                Named(FailureIndexJsonFileName, failureJson),
            };
            var semantic = SemanticDigests();
            var digest = new MoonPalaceQaDigestManifest(qa, observed, semantic, csvDigests, jsonDigests, createdUtc);
            var paths = OutputPaths();
            var contents = new[]
            {
                qa.SerializeSeedManifestCsv(), qa.SerializeCompletionScenariosCsv(), qa.SerializeTelemetryTargetsCsv(),
                qa.SerializeFailureIndexCsv(), qa.SerializeReleaseHandoffCsv(), seedJson, completionJson, telemetryJson,
                densityJson, failureJson, digest.Serialize(),
            };
            for (var index = 0; index < paths.Length; index++) Write(projectRoot, paths[index], contents[index]);
            return new MoonPalaceQaPublishedSample(qa, digest, MoonPalaceQaExecutionCounters.FocusedThirty,
                paths, SourceReadPaths());
        }

        public static IReadOnlyList<MoonPalaceNamedDigest> ReadSourceResultSnapshot(string projectRoot) =>
            ValidateSources(RequireRoot(projectRoot));
        public static IReadOnlyList<string> GetSourceReadRelativePaths() =>
            new ReadOnlyCollection<string>(SourceReadPaths());

        private static IReadOnlyList<MoonPalaceNamedDigest> ValidateSources(string root)
        {
            var observed = ResultSpecs.Select(spec => Historical(root, spec)).OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
            var map2110 = observed.Single(x => x.Name == "MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING");
            if (map2110.Digest != MoonPalaceQaPreconditions.StrictMap2110ResultDigest)
                throw new InvalidDataException("MAP21_10 Result strict SHA mismatch.");
            if (FileSha(Resolve(root, Map2110TaskPath)) != MoonPalaceQaPreconditions.StrictMap2110TaskDigest)
                throw new InvalidDataException("MAP21_10 installed Task strict SHA mismatch.");
            var map1908Text = Read(root, ResultSpecs[0].ResultPath);
            var map1909 = JsonUtility.FromJson<Map1909Document>(Read(root, Map1909SummaryPath));
            var map2006 = JsonUtility.FromJson<Map2006Document>(Read(root, Map2006DigestPath));
            var map2110Digest = JsonUtility.FromJson<Map2110DigestDocument>(Read(root, Map2110DigestPath));
            var map2110Profile = JsonUtility.FromJson<Map2110ProfileDocument>(Read(root, Map2110ProfilePath));
            var handoff = Read(root, Map2110HandoffPath);
            if (!map1908Text.Contains(MoonPalaceQaPreconditions.SourceMap1908FailureSchemaDigest) ||
                GeneratedFailureBundleDigest.Schema != MoonPalaceQaPreconditions.SourceMap1908FailureSchemaDigest ||
                map1909 == null || map1909.map19ExitDigest != MoonPalaceQaPreconditions.SourceMap1909ExitDigest ||
                map2006 == null || map2006.map20_phase_exit_digest != MoonPalaceQaPreconditions.SourceMap2006ExitDigest ||
                map2110Digest == null || map2110Digest.canonical_digest != MoonPalaceQaPreconditions.SourceMap2110TuningDigest ||
                map2110Digest.MAP21_11_handoff_digest != MoonPalaceQaPreconditions.SourceMap2111HandoffDigest ||
                map2110Profile == null || map2110Profile.seed_output_count != 0 ||
                map2110Profile.MAP21_11_handoff_digest != MoonPalaceQaPreconditions.SourceMap2111HandoffDigest ||
                !handoff.Contains(MoonPalaceQaPreconditions.SourceMap2111HandoffDigest) || !handoff.Contains(",false,false,"))
                throw new InvalidDataException("MAP19/MAP20/MAP21_10 semantic source gate mismatch.");
            return new ReadOnlyCollection<MoonPalaceNamedDigest>(observed);
        }

        private static MoonPalaceNamedDigest Historical(string root, ResultSpec spec)
        {
            var path = Resolve(root, spec.ResultPath);
            if (!File.Exists(path)) throw new FileNotFoundException(spec.ResultPath, path);
            var lines = File.ReadAllLines(path);
            if (lines.Count(x => x == "TASK: " + spec.TaskId) != 1 || lines.Count(x => x == "STATUS: PASS") != 1)
                throw new InvalidDataException("Source Result identity mismatch: " + spec.ResultPath);
            return new MoonPalaceNamedDigest(spec.TaskId, FileSha(path));
        }
        private static MoonPalaceNamedDigest[] SemanticDigests() => new[]
        {
            new MoonPalaceNamedDigest("MAP19_08_FAILURE_BUNDLE_SCHEMA", MoonPalaceQaPreconditions.SourceMap1908FailureSchemaDigest),
            new MoonPalaceNamedDigest("MAP19_09_EXIT", MoonPalaceQaPreconditions.SourceMap1909ExitDigest),
            new MoonPalaceNamedDigest("MAP20_06_PHASE_EXIT", MoonPalaceQaPreconditions.SourceMap2006ExitDigest),
            new MoonPalaceNamedDigest("MAP21_10_TUNING", MoonPalaceQaPreconditions.SourceMap2110TuningDigest),
        };
        private static string[] OutputPaths() => new[]
        {
            Combine(AuthoringDirectoryRelativePath, SeedManifestFileName),
            Combine(AuthoringDirectoryRelativePath, CompletionScenariosFileName),
            Combine(AuthoringDirectoryRelativePath, TelemetryTargetsFileName),
            Combine(AuthoringDirectoryRelativePath, FailureIndexCsvFileName),
            Combine(AuthoringDirectoryRelativePath, ReleaseHandoffFileName),
            Combine(GeneratedDirectoryRelativePath, SeedLockManifestFileName),
            Combine(GeneratedDirectoryRelativePath, CompletionManifestFileName),
            Combine(GeneratedDirectoryRelativePath, TelemetrySummaryFileName),
            Combine(GeneratedDirectoryRelativePath, DensityRepetitionFileName),
            Combine(GeneratedDirectoryRelativePath, FailureIndexJsonFileName),
            Combine(GeneratedDirectoryRelativePath, DigestManifestFileName),
        };
        private static string[] SourceReadPaths() => ResultSpecs.Select(x => x.ResultPath)
            .Concat(new[] { Map2110TaskPath, Map1909SummaryPath, Map2006DigestPath, Map2110DigestPath,
                Map2110ProfilePath, Map2110HandoffPath }).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        private static void PublishFirstFailureBundle(string root, MoonPalaceQaSeedSet qa, string createdUtc)
        {
            var failed = qa.Telemetry.First(x => !x.CompletionPass);
            var directory = Combine(GeneratedDirectoryRelativePath, "failures");
            Directory.CreateDirectory(Resolve(root, directory));
            var document = new FailureBundleDocument
            {
                bundleId = "MAP21_11_" + failed.SeedId, schemaVersion = "MAP19_FAILURE_BUNDLE_V1",
                schemaDigest = MoonPalaceQaPreconditions.SourceMap1908FailureSchemaDigest,
                generatorVersion = MoonPalaceQaPreconditions.SeedDerivationVersion,
                dataVersion = MoonPalaceQaPreconditions.SourceMap2110TuningDigest,
                worldSeed = failed.SeedValue.ToString(CultureInfo.InvariantCulture), sourceKind = "FocusedFixture",
                contentHash = failed.ContentHash, validationPhaseId = "MAP21_11_FOCUSED_QA",
                failingTaskId = MoonPalaceQaPreconditions.TaskId, passDecision = "Fail",
                reason = "COMPLETION_OR_METRIC_FAILURE", autoRepairAttempted = false,
                createdAt = createdUtc, createdAtExcludedFromCanonicalDigest = true,
            };
            Write(root, Combine(directory, failed.SeedId + "_failure_bundle.json"),
                JsonUtility.ToJson(document, true).Replace("\r\n", "\n").TrimEnd() + "\n");
        }
        private static ResultSpec R(string taskId) => new ResultSpec(taskId,
            "MapDesign/MCP/REPORTS/" + taskId + "_RESULT.md");
        private static MoonPalaceNamedDigest Named(string name, string content) =>
            new MoonPalaceNamedDigest(name, BakingCanonicalDigest.HashCanonicalText(content));
        private static void Write(string root, string relative, string content) => File.WriteAllText(
            Resolve(root, relative), content, BakingCanonicalDigest.Utf8NoBomEncoding);
        private static string Read(string root, string relative) => File.ReadAllText(Resolve(root, relative));
        private static string Combine(string left, string right) => left.TrimEnd('/') + "/" + right;
        private static string RequireRoot(string root) { if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException(nameof(root)); return Path.GetFullPath(root); }
        private static string Resolve(string root, string relative) => Path.GetFullPath(Path.Combine(RequireRoot(root), relative.Replace('/', Path.DirectorySeparatorChar)));
        private static string FileSha(string path) { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(stream).Select(x => x.ToString("x2", CultureInfo.InvariantCulture))); }

        private sealed class ResultSpec
        { public ResultSpec(string taskId, string resultPath) { TaskId = taskId; ResultPath = resultPath; }
            public string TaskId { get; } public string ResultPath { get; } }
        [Serializable] private sealed class Map1909Document { public string map19ExitDigest; }
        [Serializable] private sealed class Map2006Document { public string map20_phase_exit_digest; }
        [Serializable] private sealed class Map2110DigestDocument { public string canonical_digest; public string MAP21_11_handoff_digest; }
        [Serializable] private sealed class Map2110ProfileDocument { public int seed_output_count; public string MAP21_11_handoff_digest; }
        [Serializable] private sealed class FailureBundleDocument
        {
            public string bundleId; public string schemaVersion; public string schemaDigest; public string generatorVersion;
            public string dataVersion; public string worldSeed; public string sourceKind; public string contentHash;
            public string validationPhaseId; public string failingTaskId; public string passDecision; public string reason;
            public bool autoRepairAttempted; public string createdAt; public bool createdAtExcludedFromCanonicalDigest;
        }
    }

    public sealed class MoonPalaceQaPublishedSample
    {
        private readonly ReadOnlyCollection<string> written;
        private readonly ReadOnlyCollection<string> sourceReads;
        public MoonPalaceQaPublishedSample(MoonPalaceQaSeedSet qa, MoonPalaceQaDigestManifest digestManifest,
            MoonPalaceQaExecutionCounters counters, IEnumerable<string> writtenPaths, IEnumerable<string> sourceReadPaths)
        {
            Qa = qa ?? throw new ArgumentNullException(nameof(qa)); DigestManifest = digestManifest ?? throw new ArgumentNullException(nameof(digestManifest));
            Counters = counters ?? throw new ArgumentNullException(nameof(counters));
            written = new ReadOnlyCollection<string>((writtenPaths ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal).ToArray());
            sourceReads = new ReadOnlyCollection<string>((sourceReadPaths ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        }
        public MoonPalaceQaSeedSet Qa { get; } public MoonPalaceQaDigestManifest DigestManifest { get; }
        public MoonPalaceQaExecutionCounters Counters { get; } public IReadOnlyList<string> WrittenRelativePaths => written;
        public IReadOnlyList<string> SourceReadRelativePaths => sourceReads;
    }

    public sealed class MoonPalaceQaSeedFailureException : Exception
    { public MoonPalaceQaSeedFailureException(string message) : base(message) { } }
}
