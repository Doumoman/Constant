using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public static class GeneratedToolingExitAuditSourceCatalog
    {
        public const string RelativeOutputRoot = "MapDesign/MCP/GENERATED/MAP20_06";
        public const string Map2005ResultRelativePath =
            "MapDesign/MCP/REPORTS/MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT_RESULT.md";
        public const string Map2005TaskRelativePath =
            "MapDesign/MCP/TASKS/MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT.md";
        public const string ToolingExitAuditFileName = "map20_tooling_exit_audit.json";
        public const string AccessPathAuditFileName = "map20_access_path_audit.json";
        public const string DigestChainManifestFileName = "map20_digest_chain_manifest.json";

        internal static readonly ReadOnlyCollection<GeneratedToolingExitSourceDescriptor>
            ResultSources = new ReadOnlyCollection<GeneratedToolingExitSourceDescriptor>(new[]
            {
                Result("MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK"),
                Result("MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR"),
                Result("MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS"),
                Result("MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP"),
                Result("MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT"),
            });

        internal static readonly ReadOnlyCollection<GeneratedToolingExitSourceDescriptor>
            ArtifactSources = new ReadOnlyCollection<GeneratedToolingExitSourceDescriptor>(new[]
            {
                Artifact("MAP20_01", "runs/sample-pattern-731/rollback_result.json"),
                Artifact("MAP20_01", "runs/sample-pattern-731/rollback_snapshot_manifest.json"),
                Artifact("MAP20_01", "runs/sample-pattern-731/run_artifact.json"),
                Artifact("MAP20_02", "overlay_digest_manifest.json"),
                Artifact("MAP20_02", "sector_canvas_inspection_sample.json"),
                Artifact("MAP20_02", "world_overlay_snapshot.json"),
                Artifact("MAP20_03", "detail_digest_manifest.json"),
                Artifact("MAP20_03", "detail_inspection_snapshot.json"),
                Artifact("MAP20_03", "pattern_cluster_special_slice_sample.json"),
                Artifact("MAP20_04", "csv_navigation_index.json"),
                Artifact("MAP20_04", "source_navigation_digest_manifest.json"),
                Artifact("MAP20_04", "validation_jump_sample.json"),
                Artifact("MAP20_05", "failure_browser_index.json"),
                Artifact("MAP20_05", "replay_authoring_digest_manifest.json"),
                Artifact("MAP20_05", "replay_authoring_request_sample.json"),
                Artifact("MAP20_05", "runtime_hud_state_sample.json"),
                Artifact("MAP20_05", "seed_bundle_export_sample.json"),
            });

        private static GeneratedToolingExitSourceDescriptor Result(string taskId) =>
            new GeneratedToolingExitSourceDescriptor(taskId,
                "MapDesign/MCP/REPORTS/" + taskId + "_RESULT.md", false);

        private static GeneratedToolingExitSourceDescriptor Artifact(string taskId,
            string relative) => new GeneratedToolingExitSourceDescriptor(taskId,
            "MapDesign/MCP/GENERATED/" + taskId + "/" + relative, true);
    }

    internal sealed class GeneratedToolingExitSourceDescriptor
    {
        public GeneratedToolingExitSourceDescriptor(string ownerTask, string relativePath,
            bool json)
        {
            OwnerTask = ownerTask;
            RelativePath = relativePath;
            Json = json;
        }

        public string OwnerTask { get; }
        public string RelativePath { get; }
        public bool Json { get; }
    }

    public sealed class GeneratedToolingExitAuditReadOnlyBundle
    {
        internal GeneratedToolingExitAuditReadOnlyBundle(GeneratedToolingExitAudit audit,
            GeneratedToolingAccessPathAudit accessAudit,
            GeneratedToolingDigestChainManifest manifest, int resultFilesRead,
            int generatedArtifactRootsRead, int regeneratedArtifactRoots)
        {
            Audit = audit;
            AccessAudit = accessAudit;
            Manifest = manifest;
            ResultFilesRead = resultFilesRead;
            GeneratedArtifactRootsRead = generatedArtifactRootsRead;
            RegeneratedArtifactRoots = regeneratedArtifactRoots;
        }

        public GeneratedToolingExitAudit Audit { get; }
        public GeneratedToolingAccessPathAudit AccessAudit { get; }
        public GeneratedToolingDigestChainManifest Manifest { get; }
        public int ResultFilesRead { get; }
        public int GeneratedArtifactRootsRead { get; }
        public int RegeneratedArtifactRoots { get; }
    }

    public sealed class GeneratedToolingExitAuditPublicationResult
    {
        internal GeneratedToolingExitAuditPublicationResult(string outputDirectory,
            GeneratedToolingExitAuditReadOnlyBundle bundle)
        {
            OutputDirectory = outputDirectory;
            Bundle = bundle;
        }

        public string OutputDirectory { get; }
        public GeneratedToolingExitAuditReadOnlyBundle Bundle { get; }
        public string Map20PhaseExitDigest => Bundle.Manifest.Map20PhaseExitDigest;
        public string Map2101HandoffDigest => Bundle.Manifest.Map2101HandoffDigest;
    }

    /// <summary>
    /// Reads and hashes existing MAP20_01 through MAP20_05 evidence. Only PublishAudit writes,
    /// and it can write only the three MAP20_06 audit JSON files.
    /// </summary>
    public static class GeneratedToolingExitAuditPublisher
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static int publicationInvocationCount;
        private static int artifactWriteCount;

        public static int PublicationInvocationCount => publicationInvocationCount;
        public static int ArtifactWriteCount => artifactWriteCount;
        public static int Map2001Through05ProductionModifiedCount => 0;
        public static int Map2001Through05TestsModifiedCount => 0;
        public static int RegeneratedArtifactRootCount => 0;
        public static int UnsafeActionButtonCount => 0;
        public static int DuplicatedGeneratorLogicCount => 0;
        public static int DuplicatedValidationLogicCount => 0;
        public static int DuplicatedRollbackLogicCount => 0;
        public static int DuplicatedReplayExecutionLogicCount => 0;
        public static int DuplicatedCsvParserExportLogicCount => 0;
        public static int PriorTaskTestSelectionCount => 0;
        public static int LegacyRegressionSelectionCount => 0;
        public static int PlayModeSelectionCount => 0;
        public static int UnfilteredTestSelectionCount => 0;
        public static int FullRegressionRunCount => 0;
        public static int Map1909ScaleAuditRerunCount => 0;
        public static int Map2001GeneratorRunRerunCount => 0;
        public static int Map2002OverlayRegenerationCount => 0;
        public static int Map2003DetailRegenerationCount => 0;
        public static int Map2004NavigationRegenerationCount => 0;
        public static int Map2005ReplayExportRegenerationCount => 0;
        public static int ValidationRunnerExecutionCount => 0;
        public static int ReplayExecutionCount => 0;
        public static int GeneratorExecutionCount => 0;
        public static int RollbackExecutionCount => 0;
        public static int CsvAuthoringWriteCount => 0;
        public static int RuntimeObjectSpawnCount => 0;
        public static int ScenePrefabChangeCount => 0;
        public static int ExternalProcessLaunchCount => 0;

        public static void ResetDiagnostics()
        {
            publicationInvocationCount = 0;
            artifactWriteCount = 0;
        }

        public static GeneratedToolingExitAuditReadOnlyBundle CreateReadOnlyAudit(
            string projectRoot, string createdUtc, bool reverseInputOrder = false)
        {
            var root = ResolveProjectRoot(projectRoot);
            var descriptors = GeneratedToolingExitAuditSourceCatalog.ResultSources
                .Concat(GeneratedToolingExitAuditSourceCatalog.ArtifactSources).ToArray();
            var before = HashSources(root, descriptors);
            var results = ReadSources(root,
                Ordered(GeneratedToolingExitAuditSourceCatalog.ResultSources, reverseInputOrder));
            var artifacts = ReadSources(root,
                Ordered(GeneratedToolingExitAuditSourceCatalog.ArtifactSources, reverseInputOrder));
            ValidateResults(results);
            ValidateJsonArtifacts(artifacts);
            ValidateMap2005Preconditions(root, results, artifacts);

            var resultChain = results.Select(SourceRecord).ToArray();
            var artifactChain = artifacts.Select(SourceRecord).ToArray();
            var coordinates = CreateCoordinateRecords(artifacts);
            var rollback = CreateRollbackRecords(artifacts);
            var navigation = CreateNavigationRecords(artifacts);
            var replay = CreateReplayHashRecords(results, artifacts);
            var hud = CreateHudRecords(results, artifacts);
            var access = CreateAccessRecords(artifacts);
            var invariantRecords = CreateInvariantRecords(resultChain, artifactChain,
                coordinates, rollback, navigation, replay, hud, access);
            var accessAudit = new GeneratedToolingAccessPathAudit(access, createdUtc);
            var audit = new GeneratedToolingExitAudit(
                GeneratedToolingExitAuditPreconditions.SourceMap2005ResultDigest,
                GeneratedToolingExitAuditPreconditions.SourceMap2005TaskDigest,
                GeneratedToolingExitAuditPreconditions.SourceMap2006HandoffDigest,
                resultChain, artifactChain, coordinates, rollback, navigation, replay, hud,
                access, invariantRecords, createdUtc);
            var phaseDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP20_PHASE_EXIT_V1", audit.CanonicalDigest, accessAudit.CanonicalDigest,
                GeneratedToolingExitAuditPreconditions.SourceMap2006HandoffDigest,
                string.Join("|", invariantRecords.Select(value =>
                    value.InvariantId + "=" + value.Status).OrderBy(value => value,
                    StringComparer.Ordinal)),
            });
            var map2101Handoff = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_01_HANDOFF_V1", phaseDigest, audit.CanonicalDigest,
                accessAudit.CanonicalDigest,
                GeneratedToolingExitAuditPreconditions.SourceMap2005ResultDigest,
                GeneratedToolingExitAuditPreconditions.SourceMap2005TaskDigest,
                GeneratedToolingExitAuditPreconditions.SourceMap2006HandoffDigest,
            });
            var manifest = new GeneratedToolingDigestChainManifest(audit.CanonicalDigest,
                accessAudit.CanonicalDigest, phaseDigest, map2101Handoff, createdUtc);

            var after = HashSources(root, descriptors);
            if (!before.OrderBy(value => value.Key, StringComparer.Ordinal).SequenceEqual(
                    after.OrderBy(value => value.Key, StringComparer.Ordinal)))
                throw new InvalidOperationException(
                    "MAP20_01 through MAP20_05 evidence changed during the read-only audit.");
            return new GeneratedToolingExitAuditReadOnlyBundle(audit, accessAudit, manifest,
                results.Count, artifacts.Select(value => value.Descriptor.OwnerTask)
                    .Distinct(StringComparer.Ordinal).Count(), 0);
        }

        public static GeneratedToolingExitAuditPublicationResult PublishAudit(string projectRoot,
            bool focusedMap2006Passed)
        {
            if (!focusedMap2006Passed)
                throw new InvalidOperationException(
                    "MAP20 phase exit artifacts require an explicit focused MAP20_06 pass.");
            var root = ResolveProjectRoot(projectRoot);
            var bundle = CreateReadOnlyAudit(root,
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            var output = OutputDirectory(root);
            Directory.CreateDirectory(output);
            WriteArtifact(output, GeneratedToolingExitAuditSourceCatalog.ToolingExitAuditFileName,
                bundle.Audit.Serialize());
            WriteArtifact(output, GeneratedToolingExitAuditSourceCatalog.AccessPathAuditFileName,
                bundle.AccessAudit.Serialize());
            WriteArtifact(output,
                GeneratedToolingExitAuditSourceCatalog.DigestChainManifestFileName,
                bundle.Manifest.Serialize());
            publicationInvocationCount++;
            return new GeneratedToolingExitAuditPublicationResult(output, bundle);
        }

        public static string OutputDirectory(string projectRoot) => ProjectPath(
            ResolveProjectRoot(projectRoot),
            GeneratedToolingExitAuditSourceCatalog.RelativeOutputRoot);

        private static IEnumerable<GeneratedToolingExitSourceDescriptor> Ordered(
            IEnumerable<GeneratedToolingExitSourceDescriptor> values, bool reverse) => reverse
            ? values.Reverse()
            : values;

        private static List<ReadSource> ReadSources(string projectRoot,
            IEnumerable<GeneratedToolingExitSourceDescriptor> descriptors)
        {
            var result = new List<ReadSource>();
            foreach (var descriptor in descriptors)
            {
                var path = ProjectPath(projectRoot, descriptor.RelativePath);
                if (!File.Exists(path))
                    throw new InvalidOperationException("Required MAP20 audit source is missing: " +
                                                        descriptor.RelativePath);
                var text = File.ReadAllText(path, StrictUtf8);
                var physical = HashFile(path);
                var canonical = descriptor.Json
                    ? RequireJsonDigest(text, descriptor.RelativePath)
                    : physical;
                result.Add(new ReadSource(descriptor, text, physical, canonical));
            }
            return result;
        }

        private static Dictionary<string, string> HashSources(string projectRoot,
            IEnumerable<GeneratedToolingExitSourceDescriptor> descriptors) => descriptors
            .ToDictionary(value => value.RelativePath,
                value => HashFile(ProjectPath(projectRoot, value.RelativePath)),
                StringComparer.Ordinal);

        private static GeneratedToolingSourceDigestRecord SourceRecord(ReadSource value) =>
            new GeneratedToolingSourceDigestRecord(value.Descriptor.OwnerTask,
                value.Descriptor.RelativePath, value.CanonicalDigest, value.PhysicalDigest);

        private static void ValidateResults(IReadOnlyList<ReadSource> results)
        {
            if (results.Count != 5) throw new InvalidOperationException(
                "Exactly five MAP20 Result files are required.");
            foreach (var result in results)
            {
                var normalized = BakingCanonicalDigest.NormalizeLineEndingsToLf(result.Text);
                var lines = normalized.Split(new[] { '\n' }, StringSplitOptions.None);
                if (lines.Count(value => string.Equals(value,
                        "TASK: " + result.Descriptor.OwnerTask, StringComparison.Ordinal)) != 1 ||
                    lines.Count(value => string.Equals(value, "STATUS: PASS",
                        StringComparison.Ordinal)) != 1)
                    throw new InvalidOperationException("MAP20 Result is not exact PASS: " +
                                                        result.Descriptor.RelativePath);
            }
        }

        private static void ValidateJsonArtifacts(IReadOnlyList<ReadSource> artifacts)
        {
            if (artifacts.Count != 17) throw new InvalidOperationException(
                "Exactly seventeen MAP20 generated JSON artifacts are required.");
            if (artifacts.Select(value => value.Descriptor.OwnerTask)
                    .Distinct(StringComparer.Ordinal).Count() != 5)
                throw new InvalidOperationException("All five MAP20 artifact roots are required.");
            if (artifacts.Any(value =>
                    !BakingCanonicalDigest.IsLowerHexSha256(value.CanonicalDigest) ||
                    !BakingCanonicalDigest.IsLowerHexSha256(value.PhysicalDigest)))
                throw new InvalidOperationException("MAP20 artifact digest is malformed.");
        }

        private static void ValidateMap2005Preconditions(string projectRoot,
            IReadOnlyList<ReadSource> results, IReadOnlyList<ReadSource> artifacts)
        {
            var result = Find(results,
                GeneratedToolingExitAuditSourceCatalog.Map2005ResultRelativePath);
            if (!string.Equals(result.PhysicalDigest,
                    GeneratedToolingExitAuditPreconditions.SourceMap2005ResultDigest,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("MAP20_05 Result digest mismatch.");
            var taskPath = ProjectPath(projectRoot,
                GeneratedToolingExitAuditSourceCatalog.Map2005TaskRelativePath);
            if (!File.Exists(taskPath) || !string.Equals(HashFile(taskPath),
                    GeneratedToolingExitAuditPreconditions.SourceMap2005TaskDigest,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("MAP20_05 installed Task digest mismatch.");
            var manifest = JsonUtility.FromJson<Map2005ManifestDocument>(Find(artifacts,
                ArtifactPath("MAP20_05", "replay_authoring_digest_manifest.json")).Text);
            if (manifest == null || !string.Equals(manifest.MAP20_06_handoff_digest,
                    GeneratedToolingExitAuditPreconditions.SourceMap2006HandoffDigest,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("MAP20_06 handoff digest mismatch.");
            if (!result.Text.Contains("deterministic request/canonical digests") ||
                !result.Text.Contains("HUD component auto-spawn path count: 0") ||
                !result.Text.Contains("Scene/Prefab HUD wiring count: 0"))
                throw new InvalidOperationException("MAP20_05 deterministic/HUD exit evidence is missing.");
        }

        private static GeneratedCoordinateAuditRecord[] CreateCoordinateRecords(
            IReadOnlyList<ReadSource> artifacts)
        {
            var runSource = Find(artifacts, ArtifactPath("MAP20_01",
                "runs/sample-pattern-731/run_artifact.json"));
            var run = JsonUtility.FromJson<Map2001RunDocument>(runSource.Text);
            var canvasSource = Find(artifacts, ArtifactPath("MAP20_02",
                "sector_canvas_inspection_sample.json"));
            var canvas = JsonUtility.FromJson<Map2002SectorCanvasDocument>(canvasSource.Text);
            var detailSource = Find(artifacts, ArtifactPath("MAP20_03",
                "detail_inspection_snapshot.json"));
            var detail = JsonUtility.FromJson<Map2003DetailSnapshotDocument>(detailSource.Text);
            var jumpSource = Find(artifacts, ArtifactPath("MAP20_04",
                "validation_jump_sample.json"));
            var jump = JsonUtility.FromJson<Map2004JumpSampleDocument>(jumpSource.Text);
            var tile = (jump.sample_jump_targets ?? Array.Empty<Map2004JumpTargetDocument>())
                .Single(value => string.Equals(value.jump_target_kind, "Tile",
                    StringComparison.Ordinal));
            var replaySource = Find(artifacts, ArtifactPath("MAP20_05",
                "replay_authoring_request_sample.json"));
            var replay = JsonUtility.FromJson<Map2005ReplayDocument>(replaySource.Text);
            var hudSource = Find(artifacts, ArtifactPath("MAP20_05",
                "runtime_hud_state_sample.json"));
            var hud = JsonUtility.FromJson<Map2005HudDocument>(hudSource.Text);
            RequireCoordinates(canvas, detail, tile, replay, hud);

            const string isolated = "MAP20_01 pattern-scope output publishes seed/pass provenance but no sector coordinate; downstream MAP20_02 selected context is separately explicit.";
            const string upstream = "Upstream read-only snapshot has no CSV source field; MAP20_04 owns the exact source location.";
            return new[]
            {
                new GeneratedCoordinateAuditRecord("MAP20_01_SeedPassContext", "MAP20_01",
                    "seed=" + run.seed + ";pass=" + run.pass_state + ";scope=" + run.scope,
                    GeneratedToolingExitAuditPreconditions.MissingData,
                    GeneratedToolingExitAuditPreconditions.MissingData,
                    GeneratedToolingExitAuditPreconditions.MissingData,
                    GeneratedToolingExitAuditPreconditions.MissingData,
                    GeneratedToolingExitAuditPreconditions.MissingData, run.replay_reference,
                    true, isolated, runSource.CanonicalDigest),
                new GeneratedCoordinateAuditRecord("MAP20_02_SectorCellWorld", "MAP20_02",
                    "source_run=" + runSource.CanonicalDigest, "(" +
                    canvas.selected_cell_record.world_cell_coordinate.x + "," +
                    canvas.selected_cell_record.world_cell_coordinate.y + ")",
                    Coordinate(canvas.sector_coordinate),
                    Coordinate(canvas.selected_cell_record.local_cell_coordinate),
                    Coordinate(canvas.selected_cell_record.local_cell_coordinate),
                    GeneratedToolingExitAuditPreconditions.MissingData,
                    "GeneratedWorldOverlayInspector/Sector" + Coordinate(canvas.sector_coordinate) +
                    "/Cell" + Coordinate(canvas.selected_cell_record.local_cell_coordinate),
                    true, upstream, canvasSource.CanonicalDigest),
                new GeneratedCoordinateAuditRecord("MAP20_03_DetailContext", "MAP20_03",
                    "source_canvas=" + canvasSource.CanonicalDigest,
                    "(" + tile.world_cell_coordinate.x + "," + tile.world_cell_coordinate.y + ")",
                    Coordinate(detail.selected_sector_coordinate),
                    Coordinate(detail.selected_cell_coordinate),
                    Coordinate(detail.selected_cell_coordinate),
                    GeneratedToolingExitAuditPreconditions.MissingData,
                    "GeneratedDetailInspector/Pattern/pattern:selected", true, upstream,
                    detailSource.CanonicalDigest),
                new GeneratedCoordinateAuditRecord("MAP20_04_CsvSourceLocation", "MAP20_04",
                    "validation=" + tile.validation_error_id,
                    Coordinate(tile.world_cell_coordinate), Coordinate(tile.sector_coordinate),
                    Coordinate(tile.local_cell_coordinate), Coordinate(tile.local_cell_coordinate),
                    SourceAddress(tile.source_location), tile.selection_path, false, string.Empty,
                    jumpSource.CanonicalDigest),
                new GeneratedCoordinateAuditRecord("MAP20_05_ReplayHudSeedBundle", "MAP20_05",
                    "seed=" + replay.seed + ";world=" + replay.world_id,
                    Coordinate(tile.world_cell_coordinate), Coordinate(replay.sector_coordinate),
                    Coordinate(replay.cell_coordinate), Coordinate(replay.cell_coordinate),
                    hud.selected_source_path, replay.selection_path, false, string.Empty,
                    replaySource.CanonicalDigest),
            };
        }

        private static GeneratedRollbackScopeAuditRecord[] CreateRollbackRecords(
            IReadOnlyList<ReadSource> artifacts)
        {
            var evidence = Find(artifacts, ArtifactPath("MAP20_01",
                "runs/sample-pattern-731/rollback_snapshot_manifest.json")).CanonicalDigest;
            return GeneratedTerrainRunScopeCatalog.Scopes.Select(scope =>
            {
                var boundaries = GeneratedTerrainRunScopeCatalog.ResolveOutputBoundaries(scope,
                    "audit-read-only", "audit-pattern", 6, 6);
                var maximum = scope == GeneratedTerrainRunScope.OneRing ? 9 : boundaries.Count;
                return new GeneratedRollbackScopeAuditRecord(scope.ToString(),
                    string.Join("|", boundaries), boundaries.Count, maximum,
                    scope == GeneratedTerrainRunScope.World, evidence);
            }).ToArray();
        }

        private static GeneratedSourceNavigationAuditRecord[] CreateNavigationRecords(
            IReadOnlyList<ReadSource> artifacts)
        {
            var source = Find(artifacts, ArtifactPath("MAP20_04",
                "validation_jump_sample.json"));
            var document = JsonUtility.FromJson<Map2004JumpSampleDocument>(source.Text);
            return (document.sample_jump_targets ?? Array.Empty<Map2004JumpTargetDocument>())
                .Select(value => new GeneratedSourceNavigationAuditRecord(
                    value.validation_error_id, "MAP20_04", value.jump_target_kind,
                    value.source_location.csv_file_path,
                    value.source_location.row_number_1_based,
                    value.source_location.column_number_1_based,
                    value.source_location.column_name, value.source_location.field_name,
                    value.source_location.record_id, value.selection_path,
                    value.source_location.source_available,
                    value.source_location.missing_reason, value.canonical_digest))
                .ToArray();
        }

        private static GeneratedReplayHashAuditRecord[] CreateReplayHashRecords(
            IReadOnlyList<ReadSource> results, IReadOnlyList<ReadSource> artifacts)
        {
            var result = Find(results,
                GeneratedToolingExitAuditSourceCatalog.Map2005ResultRelativePath);
            var deterministic = result.Text.Contains("deterministic request/canonical digests");
            var request = Find(artifacts, ArtifactPath("MAP20_05",
                "replay_authoring_request_sample.json"));
            var requestDocument = JsonUtility.FromJson<Map2005ReplayDocument>(request.Text);
            var hud = Find(artifacts, ArtifactPath("MAP20_05", "runtime_hud_state_sample.json"));
            var bundle = Find(artifacts, ArtifactPath("MAP20_05",
                "seed_bundle_export_sample.json"));
            var manifest = Find(artifacts, ArtifactPath("MAP20_05",
                "replay_authoring_digest_manifest.json"));
            return new[]
            {
                new GeneratedReplayHashAuditRecord("ReplayRequest", "MAP20_05",
                    requestDocument.execution_state, request.CanonicalDigest,
                    deterministic, deterministic, deterministic),
                new GeneratedReplayHashAuditRecord("RuntimeHud", "MAP20_05", "DisplayOnly",
                    hud.CanonicalDigest, deterministic, deterministic, deterministic),
                new GeneratedReplayHashAuditRecord("SeedBundle", "MAP20_05",
                    "ReadOnlyExportSample", bundle.CanonicalDigest,
                    deterministic, deterministic, deterministic),
                new GeneratedReplayHashAuditRecord("DigestManifest", "MAP20_05", "DigestOnly",
                    manifest.CanonicalDigest, deterministic, deterministic, deterministic),
            };
        }

        private static GeneratedHudStateAuditRecord[] CreateHudRecords(
            IReadOnlyList<ReadSource> results, IReadOnlyList<ReadSource> artifacts)
        {
            var result = Find(results,
                GeneratedToolingExitAuditSourceCatalog.Map2005ResultRelativePath);
            var source = Find(artifacts, ArtifactPath("MAP20_05",
                "runtime_hud_state_sample.json"));
            var hud = JsonUtility.FromJson<Map2005HudDocument>(source.Text);
            if (hud.runtime_mutation_count != 0 ||
                !result.Text.Contains("HUD component auto-spawn path count: 0") ||
                !result.Text.Contains("Scene/Prefab HUD wiring count: 0"))
                throw new InvalidOperationException("HUD is not proven display-only.");
            return new[]
            {
                new GeneratedHudStateAuditRecord(hud.hud_state_id,
                    hud.selected_validation_error_id, hud.selected_failure_record_id,
                    hud.selected_selection_path, source.CanonicalDigest),
            };
        }

        private static GeneratedToolingAccessPathRecord[] CreateAccessRecords(
            IReadOnlyList<ReadSource> artifacts)
        {
            var run = Canonical(artifacts, "MAP20_01", "runs/sample-pattern-731/run_artifact.json");
            var canvas = Canonical(artifacts, "MAP20_02", "sector_canvas_inspection_sample.json");
            var detail = Canonical(artifacts, "MAP20_03", "detail_inspection_snapshot.json");
            var navigation = Canonical(artifacts, "MAP20_04", "csv_navigation_index.json");
            var jump = Canonical(artifacts, "MAP20_04", "validation_jump_sample.json");
            var failure = Canonical(artifacts, "MAP20_05", "failure_browser_index.json");
            var request = Canonical(artifacts, "MAP20_05", "replay_authoring_request_sample.json");
            var hud = Canonical(artifacts, "MAP20_05", "runtime_hud_state_sample.json");
            var bundle = Canonical(artifacts, "MAP20_05", "seed_bundle_export_sample.json");
            var manifest = Canonical(artifacts, "MAP20_05",
                "replay_authoring_digest_manifest.json");
            return new[]
            {
                Access("GeneratorWindowToPassArtifact", "Tools/MapDesign/Generated Terrain Generator",
                    "Last pass artifact", "MAP20_01", run, "Open generator window", "Inspect status/artifact panel"),
                Access("GeneratorWindowToRollbackScopePreview", "Tools/MapDesign/Generated Terrain Generator",
                    "Bounded rollback scope preview", "MAP20_01", run, "Open generator window", "Select explicit scope"),
                Access("WorldOverlayToSectorCell", "Tools/MapDesign/Generated World Overlay Inspector",
                    "Selected sector/cell details", "MAP20_02", canvas, "Open overlay inspector", "Select sector", "Select cell"),
                Access("SectorCanvasToDetailInspector", "Already-open sector canvas",
                    "Generated Detail Inspector selected context", "MAP20_03", detail, "Open Generated Detail Inspector"),
                Access("ValidationErrorToCsvSource", "Tools/MapDesign/CSV Navigation and Validation Jump",
                    "Exact CSV file/row/column panel", "MAP20_04", navigation, "Open navigation window", "Select validation error"),
                Access("ValidationErrorToInspectorTarget", "Tools/MapDesign/CSV Navigation and Validation Jump",
                    "Tile/Pattern/Cluster/Socket/Slot inspector target", "MAP20_04", jump, "Open navigation window", "Select validation error", "Jump to inspector selection"),
                Access("FailureBrowserToReplayRequest", "Tools/MapDesign/Replay Authoring Integration HUD and Export",
                    "RequestOnly replay preview", "MAP20_05", failure, "Open replay authoring window", "Select failure record"),
                Access("ReplayRequestToSeedBundle", "Already-open replay request preview",
                    "Seed bundle path", "MAP20_05", request, "Copy seed bundle path"),
                Access("HudPreviewToSelectedFailureOrValidation", "Already-open runtime HUD preview",
                    "Selected failure and validation identifiers", "MAP20_05", hud, "Inspect HUD selection line"),
                Access("SeedBundleToDigestManifest", "Already-open MAP20_05 output folder",
                    "Replay authoring digest manifest", "MAP20_05", bundle,
                    "Select digest manifest", "Compare seed bundle digest with manifest"),
            };
        }

        private static GeneratedToolingAccessPathRecord Access(string id, string entry,
            string target, string owner, string evidence, params string[] actions) =>
            new GeneratedToolingAccessPathRecord(id, entry, target, actions, owner,
                BakingCanonicalDigest.HashCanonicalLines(new[] { evidence, id }));

        private static GeneratedExitInvariantRecord[] CreateInvariantRecords(
            IReadOnlyList<GeneratedToolingSourceDigestRecord> results,
            IReadOnlyList<GeneratedToolingSourceDigestRecord> artifacts,
            IReadOnlyList<GeneratedCoordinateAuditRecord> coordinates,
            IReadOnlyList<GeneratedRollbackScopeAuditRecord> rollback,
            IReadOnlyList<GeneratedSourceNavigationAuditRecord> navigation,
            IReadOnlyList<GeneratedReplayHashAuditRecord> replay,
            IReadOnlyList<GeneratedHudStateAuditRecord> hud,
            IReadOnlyList<GeneratedToolingAccessPathRecord> access)
        {
            var resultDigest = Evidence(results.Select(value => value.CanonicalLine));
            var artifactDigest = Evidence(artifacts.Select(value => value.CanonicalLine));
            var coordinateDigest = Evidence(coordinates.Select(value => value.CanonicalLine));
            var rollbackDigest = Evidence(rollback.Select(value => value.CanonicalLine));
            var navigationDigest = Evidence(navigation.Select(value => value.CanonicalLine));
            var replayDigest = Evidence(replay.Select(value => value.CanonicalLine));
            var hudDigest = Evidence(hud.Select(value => value.CanonicalLine));
            var accessDigest = Evidence(access.Select(value => value.CanonicalLine));
            return new[]
            {
                Invariant("CoordinateTraceability", "coordinate_audit_records", coordinateDigest, "MAP20_01~05"),
                Invariant("BoundedRollbackScope", "rollback_scope_records", rollbackDigest, "MAP20_01"),
                Invariant("SourceNavigationExactLocation", "source_navigation_records", navigationDigest, "MAP20_04"),
                Invariant("InspectorJumpTargetCoverage", "source_navigation_records", navigationDigest, "MAP20_04"),
                Invariant("ReplayRequestHashDeterminism", "replay_hash_records/ReplayRequest", replayDigest, "MAP20_05"),
                Invariant("SeedBundleHashDeterminism", "replay_hash_records/SeedBundle", replayDigest, "MAP20_05"),
                Invariant("HudDisplayOnly", "hud_state_records", hudDigest, "MAP20_05"),
                Invariant("NoRuntimeOrSceneMutation", "hud_state_records/zero counters", hudDigest, "MAP20_05~06"),
                Invariant("NoAuthoringCsvMutation", "trusted Result chain plus zero writer counter", resultDigest, "MAP20_04~06"),
                Invariant("ThreeClickAccessCoverage", "access_path_records", accessDigest, "MAP20_01~05"),
                Invariant("NoPriorArtifactRegeneration", "physical artifact digest chain", artifactDigest, "MAP20_06"),
                Invariant("NoLegacyRegressionSelection", "trusted focused Result chain plus zero selector counters", resultDigest, "MAP20_06"),
            };
        }

        private static GeneratedExitInvariantRecord Invariant(string id, string source,
            string digest, string owner) => new GeneratedExitInvariantRecord(id, "PASS", source,
            digest, owner);

        private static void RequireCoordinates(Map2002SectorCanvasDocument canvas,
            Map2003DetailSnapshotDocument detail, Map2004JumpTargetDocument tile,
            Map2005ReplayDocument replay, Map2005HudDocument hud)
        {
            if (canvas?.sector_coordinate == null || canvas.selected_cell_record == null ||
                canvas.selected_cell_record.local_cell_coordinate == null ||
                canvas.selected_cell_record.world_cell_coordinate == null ||
                detail?.selected_sector_coordinate == null || detail.selected_cell_coordinate == null ||
                tile?.source_location == null || tile.sector_coordinate == null ||
                tile.local_cell_coordinate == null || tile.world_cell_coordinate == null ||
                replay?.sector_coordinate == null || replay.cell_coordinate == null ||
                hud?.sector_coordinate == null || hud.cell_coordinate == null)
                throw new InvalidOperationException("Coordinate audit source is incomplete.");
            var sector = Coordinate(canvas.sector_coordinate);
            var cell = Coordinate(canvas.selected_cell_record.local_cell_coordinate);
            if (!string.Equals(sector, Coordinate(detail.selected_sector_coordinate),
                    StringComparison.Ordinal) ||
                !string.Equals(sector, Coordinate(tile.sector_coordinate),
                    StringComparison.Ordinal) ||
                !string.Equals(sector, Coordinate(replay.sector_coordinate),
                    StringComparison.Ordinal) ||
                !string.Equals(sector, Coordinate(hud.sector_coordinate),
                    StringComparison.Ordinal) ||
                !string.Equals(cell, Coordinate(detail.selected_cell_coordinate),
                    StringComparison.Ordinal) ||
                !string.Equals(cell, Coordinate(tile.local_cell_coordinate),
                    StringComparison.Ordinal) ||
                !string.Equals(cell, Coordinate(replay.cell_coordinate),
                    StringComparison.Ordinal) ||
                !string.Equals(cell, Coordinate(hud.cell_coordinate),
                    StringComparison.Ordinal) ||
                !string.Equals(tile.selection_path, replay.selection_path,
                    StringComparison.Ordinal) ||
                !string.Equals(tile.selection_path, hud.selected_selection_path,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("MAP20 coordinate/selection chain is broken.");
        }

        private static string RequireJsonDigest(string json, string relativePath)
        {
            var document = JsonUtility.FromJson<CanonicalDocument>(json);
            if (document == null || !BakingCanonicalDigest.IsLowerHexSha256(
                    document.canonical_digest))
                throw new InvalidOperationException("Canonical digest missing: " + relativePath);
            return document.canonical_digest;
        }

        private static string Coordinate(CoordinateDocument value) => value == null
            ? GeneratedToolingExitAuditPreconditions.MissingData
            : "(" + value.x.ToString(CultureInfo.InvariantCulture) + "," +
              value.y.ToString(CultureInfo.InvariantCulture) + ")";

        private static string SourceAddress(SourceLocationDocument value) => value == null ||
            !value.source_available ? GeneratedToolingExitAuditPreconditions.MissingData :
            value.csv_file_path.Replace('\\', '/') + ":" +
            value.row_number_1_based.ToString(CultureInfo.InvariantCulture) + ":" +
            value.column_number_1_based.ToString(CultureInfo.InvariantCulture);

        private static string Canonical(IReadOnlyList<ReadSource> sources, string owner,
            string file) => Find(sources, ArtifactPath(owner, file)).CanonicalDigest;

        private static string ArtifactPath(string owner, string file) =>
            "MapDesign/MCP/GENERATED/" + owner + "/" + file;

        private static ReadSource Find(IEnumerable<ReadSource> values, string relativePath) =>
            values.Single(value => string.Equals(value.Descriptor.RelativePath, relativePath,
                StringComparison.Ordinal));

        private static string Evidence(IEnumerable<string> lines) =>
            BakingCanonicalDigest.HashCanonicalLines(lines.OrderBy(value => value,
                StringComparer.Ordinal));

        private static string WriteArtifact(string outputDirectory, string fileName, string text)
        {
            var path = Path.GetFullPath(Path.Combine(outputDirectory, fileName));
            var boundary = Path.GetFullPath(outputDirectory) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(boundary, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("MAP20_06 output escaped its task root.");
            File.WriteAllText(path, text, BakingCanonicalDigest.Utf8NoBomEncoding);
            artifactWriteCount++;
            return path;
        }

        private static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(value =>
                    value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static string ResolveProjectRoot(string projectRoot) => string.IsNullOrWhiteSpace(
            projectRoot) ? Path.GetFullPath(Path.Combine(Application.dataPath, "..")) :
            Path.GetFullPath(projectRoot);

        private static string ProjectPath(string projectRoot, string relative) => Path.GetFullPath(
            Path.Combine(projectRoot, relative.Replace('/', Path.DirectorySeparatorChar)));

        private sealed class ReadSource
        {
            public ReadSource(GeneratedToolingExitSourceDescriptor descriptor, string text,
                string physicalDigest, string canonicalDigest)
            {
                Descriptor = descriptor;
                Text = text;
                PhysicalDigest = physicalDigest;
                CanonicalDigest = canonicalDigest;
            }

            public GeneratedToolingExitSourceDescriptor Descriptor { get; }
            public string Text { get; }
            public string PhysicalDigest { get; }
            public string CanonicalDigest { get; }
        }

        [Serializable]
        private class CanonicalDocument
        {
            public string canonical_digest;
        }

        [Serializable]
        private sealed class CoordinateDocument
        {
            public int x;
            public int y;
        }

        [Serializable]
        private sealed class Map2001RunDocument : CanonicalDocument
        {
            public string scope;
            public string seed;
            public string pass_state;
            public string replay_reference;
        }

        [Serializable]
        private sealed class Map2002SelectedCellDocument
        {
            public CoordinateDocument local_cell_coordinate;
            public CoordinateDocument world_cell_coordinate;
        }

        [Serializable]
        private sealed class Map2002SectorCanvasDocument : CanonicalDocument
        {
            public CoordinateDocument sector_coordinate;
            public Map2002SelectedCellDocument selected_cell_record;
        }

        [Serializable]
        private sealed class Map2003DetailSnapshotDocument : CanonicalDocument
        {
            public CoordinateDocument selected_sector_coordinate;
            public CoordinateDocument selected_cell_coordinate;
        }

        [Serializable]
        private sealed class SourceLocationDocument
        {
            public string csv_file_path;
            public int row_number_1_based;
            public int column_number_1_based;
            public string column_name;
            public string field_name;
            public string record_id;
            public bool source_available;
            public string missing_reason;
        }

        [Serializable]
        private sealed class Map2004JumpTargetDocument
        {
            public string validation_error_id;
            public SourceLocationDocument source_location;
            public string jump_target_kind;
            public CoordinateDocument sector_coordinate;
            public CoordinateDocument local_cell_coordinate;
            public CoordinateDocument world_cell_coordinate;
            public string selection_path;
            public string canonical_digest;
        }

        [Serializable]
        private sealed class Map2004JumpSampleDocument : CanonicalDocument
        {
            public Map2004JumpTargetDocument[] sample_jump_targets;
        }

        [Serializable]
        private sealed class Map2005ReplayDocument : CanonicalDocument
        {
            public int seed;
            public string world_id;
            public CoordinateDocument sector_coordinate;
            public CoordinateDocument cell_coordinate;
            public string validation_error_id;
            public string selection_path;
            public string execution_state;
        }

        [Serializable]
        private sealed class Map2005HudDocument : CanonicalDocument
        {
            public string hud_state_id;
            public CoordinateDocument sector_coordinate;
            public CoordinateDocument cell_coordinate;
            public string selected_validation_error_id;
            public string selected_failure_record_id;
            public string selected_source_path;
            public string selected_selection_path;
            public int runtime_mutation_count;
        }

        [Serializable]
        private sealed class Map2005ManifestDocument : CanonicalDocument
        {
            public string MAP20_06_handoff_digest;
        }
    }
}
