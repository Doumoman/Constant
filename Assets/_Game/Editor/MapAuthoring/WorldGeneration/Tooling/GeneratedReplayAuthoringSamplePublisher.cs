using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Tooling;
using UnityEngine;

namespace StarNight.MapAuthoring.WorldGeneration.Tooling
{
    /// <summary>Fixed read-only inputs and the only writable MAP20_05 output root.</summary>
    public static class GeneratedReplayAuthoringSamplePreconditions
    {
        public const string RelativeOutputRoot = "MapDesign/MCP/GENERATED/MAP20_05";
        public const string Map2004ResultRelativePath =
            "MapDesign/MCP/REPORTS/MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP_RESULT.md";
        public const string Map2004TaskRelativePath =
            "MapDesign/MCP/TASKS/MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP.md";
        public const string Map2004NavigationIndexRelativePath =
            "MapDesign/MCP/GENERATED/MAP20_04/csv_navigation_index.json";
        public const string Map2004JumpSampleRelativePath =
            "MapDesign/MCP/GENERATED/MAP20_04/validation_jump_sample.json";
        public const string Map2004DigestManifestRelativePath =
            "MapDesign/MCP/GENERATED/MAP20_04/source_navigation_digest_manifest.json";
        public const string Map1908FailureBundleRelativePath =
            "MapDesign/MCP/GENERATED/MAP19_08/failure_bundle.json";
        public const string SampleWorldId = "WORLD_MAP20_05_SAMPLE";
        public const string SampleValidationErrorId = "MAP20_04_TILE_SOURCE";
        public const int SampleSeed = 200405;
    }

    public sealed class GeneratedReplayAuthoringReadOnlySample
    {
        internal GeneratedReplayAuthoringReadOnlySample(
            GeneratedValidationJumpTarget navigationTarget,
            GeneratedReplayAuthoringRequest replayRequest,
            GeneratedFailureBrowserIndex failureBrowser,
            GeneratedMap07FixedGeneratedSplit fixedGeneratedSplit,
            GeneratedMap08BoundaryLinkCatalog boundaryLinks,
            GeneratedRuntimeDebugHudState runtimeHud,
            GeneratedSeedBundleExportSample seedBundle,
            string map2006HandoffDigest)
        {
            NavigationTarget = navigationTarget;
            ReplayRequest = replayRequest;
            FailureBrowser = failureBrowser;
            FixedGeneratedSplit = fixedGeneratedSplit;
            BoundaryLinks = boundaryLinks;
            RuntimeHud = runtimeHud;
            SeedBundle = seedBundle;
            Map2006HandoffDigest = map2006HandoffDigest;
        }

        public GeneratedValidationJumpTarget NavigationTarget { get; }
        public GeneratedReplayAuthoringRequest ReplayRequest { get; }
        public GeneratedFailureBrowserIndex FailureBrowser { get; }
        public GeneratedMap07FixedGeneratedSplit FixedGeneratedSplit { get; }
        public GeneratedMap08BoundaryLinkCatalog BoundaryLinks { get; }
        public GeneratedRuntimeDebugHudState RuntimeHud { get; }
        public GeneratedSeedBundleExportSample SeedBundle { get; }
        public string Map2006HandoffDigest { get; }
    }

    public sealed class GeneratedReplayAuthoringPublicationResult
    {
        internal GeneratedReplayAuthoringPublicationResult(string outputDirectory,
            GeneratedReplayAuthoringReadOnlySample sample, string manifestDigest)
        {
            OutputDirectory = outputDirectory;
            Sample = sample;
            ManifestDigest = manifestDigest;
        }

        public string OutputDirectory { get; }
        public GeneratedReplayAuthoringReadOnlySample Sample { get; }
        public string ManifestDigest { get; }
        public string Map2006HandoffDigest => Sample.Map2006HandoffDigest;
    }

    /// <summary>
    /// Reads existing MAP20_04 JSON and optional MAP19 failure JSON. It writes only the five
    /// MAP20_05 sample artifacts and never calls generation, validation, rollback, or replay.
    /// </summary>
    public static class GeneratedReplayAuthoringSamplePublisher
    {
        public const string FailureBrowserFileName = "failure_browser_index.json";
        public const string ReplayRequestFileName = "replay_authoring_request_sample.json";
        public const string RuntimeHudFileName = "runtime_hud_state_sample.json";
        public const string SeedBundleFileName = "seed_bundle_export_sample.json";
        public const string DigestManifestFileName = "replay_authoring_digest_manifest.json";

        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static int publicationInvocationCount;
        private static int artifactWriteCount;
        private static int seedBundleWriteCount;

        public static int PublicationInvocationCount => publicationInvocationCount;
        public static int ArtifactWriteCount => artifactWriteCount;
        public static int SeedBundleWriteCount => seedBundleWriteCount;
        public static int CsvAuthoringWriteCount => 0;
        public static int ReplayExecutionCount => 0;
        public static int GeneratorExecutionCount => 0;
        public static int ValidationExecutionCount => 0;
        public static int RollbackExecutionCount => 0;
        public static int RuntimeObjectSpawnCount => 0;

        public static void ResetDiagnostics()
        {
            publicationInvocationCount = 0;
            artifactWriteCount = 0;
            seedBundleWriteCount = 0;
        }

        public static GeneratedReplayAuthoringReadOnlySample CreateReadOnlySample(
            string projectRoot, string createdUtc, bool includeFocusedFixture = true)
        {
            var root = ResolveProjectRoot(projectRoot);
            var source = ReadMap2004Source(root);
            var actualFailurePath = ProjectPath(root,
                GeneratedReplayAuthoringSamplePreconditions.Map1908FailureBundleRelativePath);
            var actualFailureAvailable = File.Exists(actualFailurePath);
            var failureBundleId = actualFailureAvailable
                ? "MAP19-" + HashFile(actualFailurePath).Substring(0, 20)
                : GeneratedReplayAuthoringPreconditions.MissingDigest;
            var request = GeneratedReplayAuthoringRequest.FromNavigationTarget(
                source.NavigationTarget, GeneratedReplayAuthoringSamplePreconditions.SampleSeed,
                GeneratedReplayAuthoringSamplePreconditions.SampleWorldId, failureBundleId,
                GeneratedReplayRequestedAction.AuthorReplayRequest, createdUtc);
            var failures = CreateFailureRecords(root, request, source.NavigationTarget,
                actualFailureAvailable, includeFocusedFixture);
            var failureBrowser = new GeneratedFailureBrowserIndex(failures, createdUtc);
            var fixedGeneratedSplit = GeneratedMap07FixedGeneratedSplit.CreateDefault();
            var boundaryLinks = GeneratedMap08BoundaryLinkCatalog.CreateDefault();
            var selectedFailure = failureBrowser.Records.First();
            var hud = new GeneratedRuntimeDebugHudState(request.Seed, request.WorldId,
                request.SectorCoordinate, request.CellCoordinate, "MAP20_05_READ_ONLY",
                GeneratedReplayAuthoringPreconditions.ExecutionState,
                request.ValidationErrorId, selectedFailure.FailureRecordId,
                source.NavigationTarget.SourceLocation.CsvFilePath,
                request.SelectionPath, "RequestOnly / no runtime mutation");
            var passDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP20_05_PASS_DIGEST_SUMMARY_V1", source.NavigationIndexDigest,
                source.ValidationJumpSampleDigest,
                GeneratedReplayAuthoringPreconditions.SourceMap2005HandoffDigest,
            });
            var seedBundle = new GeneratedSeedBundleExportSample(request.Seed, request.WorldId,
                request.SectorCoordinate, "MAP20_READ_ONLY_SAMPLE_V1", passDigest,
                source.NavigationIndexDigest, source.ValidationJumpSampleDigest,
                failureBrowser, request, hud, fixedGeneratedSplit, boundaryLinks, createdUtc);
            var map2006Handoff = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP20_06_HANDOFF_V1", failureBrowser.CanonicalDigest,
                request.CanonicalDigest, hud.CanonicalDigest, seedBundle.CanonicalDigest,
                fixedGeneratedSplit.CanonicalDigest, boundaryLinks.CanonicalDigest,
                GeneratedReplayAuthoringPreconditions.SourceMap2005HandoffDigest,
            });
            return new GeneratedReplayAuthoringReadOnlySample(source.NavigationTarget, request,
                failureBrowser, fixedGeneratedSplit, boundaryLinks, hud, seedBundle,
                map2006Handoff);
        }

        public static GeneratedReplayAuthoringPublicationResult PublishSamples(string projectRoot,
            bool focusedMap2005Passed)
        {
            if (!focusedMap2005Passed)
                throw new InvalidOperationException(
                    "MAP20_06 handoff publication requires an explicit focused MAP20_05 pass.");
            var root = ResolveProjectRoot(projectRoot);
            var createdUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var sample = CreateReadOnlySample(root, createdUtc);
            var manifest = CreateManifest(sample, createdUtc);
            var output = OutputDirectory(root);
            Directory.CreateDirectory(output);
            WriteArtifact(output, FailureBrowserFileName, sample.FailureBrowser.Serialize());
            WriteArtifact(output, ReplayRequestFileName, sample.ReplayRequest.Serialize());
            WriteArtifact(output, RuntimeHudFileName, sample.RuntimeHud.Serialize());
            WriteArtifact(output, SeedBundleFileName, sample.SeedBundle.Serialize());
            WriteArtifact(output, DigestManifestFileName, manifest.Serialize());
            publicationInvocationCount++;
            return new GeneratedReplayAuthoringPublicationResult(output, sample,
                manifest.canonical_digest);
        }

        public static string ExportSeedBundleSample(string projectRoot,
            bool focusedMap2005Passed)
        {
            if (!focusedMap2005Passed)
                throw new InvalidOperationException(
                    "Seed bundle sample export requires an explicit focused MAP20_05 pass.");
            var root = ResolveProjectRoot(projectRoot);
            var sample = CreateReadOnlySample(root,
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            var output = OutputDirectory(root);
            Directory.CreateDirectory(output);
            var path = WriteArtifact(output, SeedBundleFileName, sample.SeedBundle.Serialize());
            seedBundleWriteCount++;
            return path;
        }

        public static string OutputDirectory(string projectRoot) => Path.GetFullPath(Path.Combine(
            ResolveProjectRoot(projectRoot),
            GeneratedReplayAuthoringSamplePreconditions.RelativeOutputRoot.Replace('/',
                Path.DirectorySeparatorChar)));

        private static Map2004Source ReadMap2004Source(string projectRoot)
        {
            var resultPath = ProjectPath(projectRoot,
                GeneratedReplayAuthoringSamplePreconditions.Map2004ResultRelativePath);
            var taskPath = ProjectPath(projectRoot,
                GeneratedReplayAuthoringSamplePreconditions.Map2004TaskRelativePath);
            var navigationPath = ProjectPath(projectRoot,
                GeneratedReplayAuthoringSamplePreconditions.Map2004NavigationIndexRelativePath);
            var jumpPath = ProjectPath(projectRoot,
                GeneratedReplayAuthoringSamplePreconditions.Map2004JumpSampleRelativePath);
            var manifestPath = ProjectPath(projectRoot,
                GeneratedReplayAuthoringSamplePreconditions.Map2004DigestManifestRelativePath);
            if (!File.Exists(resultPath) || !File.Exists(taskPath) ||
                !File.Exists(navigationPath) || !File.Exists(jumpPath) || !File.Exists(manifestPath))
                throw new InvalidOperationException("MAP20_04 read-only handoff inputs are missing.");
            if (!string.Equals(HashFile(resultPath),
                    GeneratedReplayAuthoringPreconditions.SourceMap2004ResultDigest,
                    StringComparison.Ordinal) ||
                !string.Equals(HashFile(taskPath),
                    GeneratedReplayAuthoringPreconditions.SourceMap2004TaskDigest,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("MAP20_04 Result/Task digest mismatch.");

            var navigation = JsonUtility.FromJson<Map2004NavigationDocument>(
                File.ReadAllText(navigationPath, StrictUtf8));
            var jump = JsonUtility.FromJson<Map2004JumpSampleDocument>(
                File.ReadAllText(jumpPath, StrictUtf8));
            var manifest = JsonUtility.FromJson<Map2004DigestManifestDocument>(
                File.ReadAllText(manifestPath, StrictUtf8));
            if (navigation == null || jump == null || manifest == null ||
                !BakingCanonicalDigest.IsLowerHexSha256(navigation.canonical_digest) ||
                !BakingCanonicalDigest.IsLowerHexSha256(jump.canonical_digest) ||
                !string.Equals(navigation.canonical_digest,
                    manifest.csv_navigation_index_digest, StringComparison.Ordinal) ||
                !string.Equals(jump.canonical_digest,
                    manifest.validation_jump_sample_digest, StringComparison.Ordinal) ||
                !string.Equals(manifest.MAP20_05_handoff_digest,
                    GeneratedReplayAuthoringPreconditions.SourceMap2005HandoffDigest,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("MAP20_04 JSON handoff digest mismatch.");
            var targetDocument = (jump.sample_jump_targets ??
                    Array.Empty<Map2004JumpTargetDocument>())
                .FirstOrDefault(value => value != null && string.Equals(
                    value.validation_error_id,
                    GeneratedReplayAuthoringSamplePreconditions.SampleValidationErrorId,
                    StringComparison.Ordinal));
            if (targetDocument == null)
                throw new InvalidOperationException("MAP20_04 sample navigation target is missing.");
            return new Map2004Source
            {
                NavigationIndexDigest = navigation.canonical_digest,
                ValidationJumpSampleDigest = jump.canonical_digest,
                NavigationTarget = CreateNavigationTarget(targetDocument),
            };
        }

        private static GeneratedValidationJumpTarget CreateNavigationTarget(
            Map2004JumpTargetDocument value)
        {
            if (value.source_location == null || !Enum.TryParse(value.jump_target_kind, false,
                    out GeneratedValidationJumpTargetKind targetKind) ||
                !Enum.TryParse(value.source_location.source_kind, false,
                    out GeneratedCsvSourceKind sourceKind))
                throw new InvalidOperationException("MAP20_04 navigation target is malformed.");
            var source = new GeneratedCsvSourceLocation(sourceKind,
                value.source_location.csv_file_path, value.source_location.csv_file_digest,
                value.source_location.row_number_1_based,
                value.source_location.column_number_1_based,
                value.source_location.column_name, value.source_location.field_name,
                value.source_location.record_id, value.source_location.record_kind,
                value.source_location.line_digest, value.source_location.source_available,
                value.source_location.missing_reason);
            return new GeneratedValidationJumpTarget(value.validation_error_id, value.severity,
                value.owner, value.message, source, targetKind,
                Coordinate(value.sector_coordinate), Coordinate(value.local_cell_coordinate),
                Coordinate(value.world_cell_coordinate), value.pattern_id, value.cluster_id,
                value.socket_id, value.slot_id, value.inspector_tab_token,
                value.inspector_record_id, value.selection_path, value.replay_reference,
                value.source_available, value.missing_reason);
        }

        private static GeneratedNavigationCoordinate Coordinate(Map2004CoordinateDocument value) =>
            value == null ? null : new GeneratedNavigationCoordinate(value.x, value.y);

        private static IEnumerable<GeneratedFailureBrowserRecord> CreateFailureRecords(
            string projectRoot, GeneratedReplayAuthoringRequest request,
            GeneratedValidationJumpTarget target, bool actualFailureAvailable,
            bool includeFocusedFixture)
        {
            var records = new List<GeneratedFailureBrowserRecord>();
            var failureRelativePath =
                GeneratedReplayAuthoringSamplePreconditions.Map1908FailureBundleRelativePath;
            if (actualFailureAvailable)
            {
                var actualDigest = HashFile(ProjectPath(projectRoot, failureRelativePath));
                records.Add(new GeneratedFailureBrowserRecord(string.Empty,
                    GeneratedFailureSourceKind.ActualFailureBundle, failureRelativePath,
                    actualDigest, "MAP19_08", "FailureBundle", "Error", request.Seed,
                    request.SectorCoordinate, request.CellCoordinate, request.ValidationErrorId,
                    request.SelectionPath, request.RequestId, true, string.Empty, string.Empty));
            }
            else
            {
                records.Add(new GeneratedFailureBrowserRecord("MISSING_MAP19_FAILURE_BUNDLE",
                    GeneratedFailureSourceKind.MissingData, failureRelativePath,
                    GeneratedReplayAuthoringPreconditions.MissingDigest, "MAP19_08",
                    "MissingData", "Info", request.Seed, request.SectorCoordinate,
                    request.CellCoordinate, request.ValidationErrorId, request.SelectionPath,
                    request.RequestId, false, string.Empty,
                    "No actual MAP19 failure bundle is present; no failure was invented."));
            }
            if (includeFocusedFixture)
            {
                var fixtureDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                {
                    "MAP20_05_FOCUSED_FIXTURE_V1", target.ValidationErrorId,
                    target.CanonicalDigest, request.RequestId,
                });
                records.Add(new GeneratedFailureBrowserRecord("FOCUSED_FIXTURE_MAP20_05",
                    GeneratedFailureSourceKind.FocusedFixture, "InMemory/FocusedFixture",
                    fixtureDigest, GeneratedReplayAuthoringPreconditions.TaskId,
                    "FocusedFixture", "Warning", request.Seed, request.SectorCoordinate,
                    request.CellCoordinate, request.ValidationErrorId, request.SelectionPath,
                    request.RequestId, false, "MAP20_05_FocusedFixture", string.Empty));
            }
            return records;
        }

        private static ReplayAuthoringDigestManifestDocument CreateManifest(
            GeneratedReplayAuthoringReadOnlySample sample, string createdUtc)
        {
            var manifest = new ReplayAuthoringDigestManifestDocument
            {
                schema_version = "map20_05.replay_authoring_digest_manifest.v1",
                task_id = GeneratedReplayAuthoringPreconditions.TaskId,
                source_MAP20_04_result_digest =
                    GeneratedReplayAuthoringPreconditions.SourceMap2004ResultDigest,
                source_MAP20_04_task_digest =
                    GeneratedReplayAuthoringPreconditions.SourceMap2004TaskDigest,
                MAP20_05_handoff_digest =
                    GeneratedReplayAuthoringPreconditions.SourceMap2005HandoffDigest,
                failure_browser_index_digest = sample.FailureBrowser.CanonicalDigest,
                replay_authoring_request_digest = sample.ReplayRequest.CanonicalDigest,
                runtime_hud_state_digest = sample.RuntimeHud.CanonicalDigest,
                seed_bundle_export_sample_digest = sample.SeedBundle.CanonicalDigest,
                MAP20_06_handoff_digest = sample.Map2006HandoffDigest,
                created_utc = createdUtc ?? string.Empty,
                created_utc_excluded_from_canonical_digest = true,
            };
            manifest.canonical_digest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                manifest.schema_version, manifest.task_id,
                manifest.source_MAP20_04_result_digest,
                manifest.source_MAP20_04_task_digest, manifest.MAP20_05_handoff_digest,
                manifest.failure_browser_index_digest,
                manifest.replay_authoring_request_digest,
                manifest.runtime_hud_state_digest,
                manifest.seed_bundle_export_sample_digest,
                manifest.MAP20_06_handoff_digest, "created_utc_excluded=true",
            });
            return manifest;
        }

        private static string WriteArtifact(string outputDirectory, string fileName, string text)
        {
            var output = Path.GetFullPath(outputDirectory);
            var path = Path.GetFullPath(Path.Combine(output, fileName));
            if (!string.Equals(Path.GetDirectoryName(path), output,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("MAP20_05 output path escaped its root.");
            File.WriteAllText(path, text ?? string.Empty, StrictUtf8);
            artifactWriteCount++;
            return path;
        }

        private static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(value =>
                    value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static string ResolveProjectRoot(string projectRoot) => Path.GetFullPath(
            string.IsNullOrWhiteSpace(projectRoot)
                ? Path.Combine(Application.dataPath, "..")
                : projectRoot);

        private static string ProjectPath(string projectRoot, string relativePath) =>
            Path.GetFullPath(Path.Combine(projectRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    internal sealed class Map2004Source
    {
        public string NavigationIndexDigest { get; set; }
        public string ValidationJumpSampleDigest { get; set; }
        public GeneratedValidationJumpTarget NavigationTarget { get; set; }
    }

    [Serializable]
    internal sealed class Map2004NavigationDocument
    {
        public string canonical_digest;
    }

    [Serializable]
    internal sealed class Map2004DigestManifestDocument
    {
        public string csv_navigation_index_digest;
        public string validation_jump_sample_digest;
        public string MAP20_05_handoff_digest;
    }

    [Serializable]
    internal sealed class Map2004JumpSampleDocument
    {
        public Map2004JumpTargetDocument[] sample_jump_targets;
        public string canonical_digest;
    }

    [Serializable]
    internal sealed class Map2004CoordinateDocument
    {
        public int x;
        public int y;
    }

    [Serializable]
    internal sealed class Map2004SourceLocationDocument
    {
        public string source_kind;
        public string csv_file_path;
        public string csv_file_digest;
        public int row_number_1_based;
        public int column_number_1_based;
        public string column_name;
        public string field_name;
        public string record_id;
        public string record_kind;
        public string line_digest;
        public bool source_available;
        public string missing_reason;
    }

    [Serializable]
    internal sealed class Map2004JumpTargetDocument
    {
        public string validation_error_id;
        public string severity;
        public string owner;
        public string message;
        public Map2004SourceLocationDocument source_location;
        public string jump_target_kind;
        public Map2004CoordinateDocument sector_coordinate;
        public Map2004CoordinateDocument local_cell_coordinate;
        public Map2004CoordinateDocument world_cell_coordinate;
        public string pattern_id;
        public string cluster_id;
        public string socket_id;
        public string slot_id;
        public string inspector_tab_token;
        public string inspector_record_id;
        public string selection_path;
        public string replay_reference;
        public bool source_available;
        public string missing_reason;
    }

    [Serializable]
    internal sealed class ReplayAuthoringDigestManifestDocument
    {
        public string schema_version;
        public string task_id;
        public string source_MAP20_04_result_digest;
        public string source_MAP20_04_task_digest;
        public string MAP20_05_handoff_digest;
        public string failure_browser_index_digest;
        public string replay_authoring_request_digest;
        public string runtime_hud_state_digest;
        public string seed_bundle_export_sample_digest;
        public string MAP20_06_handoff_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public string Serialize() => JsonUtility.ToJson(this, true) + "\n";
    }
}
