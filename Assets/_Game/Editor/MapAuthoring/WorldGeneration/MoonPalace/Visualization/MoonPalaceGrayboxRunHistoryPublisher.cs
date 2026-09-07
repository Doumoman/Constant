using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MoonPalace.Visualization;

namespace StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.Visualization
{
    /// <summary>
    /// Publishes only VIS02 operation evidence. It never writes MAP21 authoring data, build settings,
    /// or any scene/prefab outside the explicit VIS01 refresh surface.
    /// </summary>
    public static class MoonPalaceGrayboxRunHistoryPublisher
    {
        public const string GeneratedDirectoryRelativePath = "MapDesign/MCP/GENERATED/VIS02";
        public const string DryRunResultRelativePath = GeneratedDirectoryRelativePath + "/moonpalace_vis02_dry_run_result.json";
        public const string RunResultRelativePath = GeneratedDirectoryRelativePath + "/moonpalace_vis02_run_result.json";
        public const string RunHistoryRelativePath = GeneratedDirectoryRelativePath + "/moonpalace_vis02_run_history.csv";
        public const string WriteScopeManifestRelativePath = GeneratedDirectoryRelativePath + "/moonpalace_vis02_write_scope_manifest.json";
        public const string DigestManifestRelativePath = GeneratedDirectoryRelativePath + "/moonpalace_vis02_digest_manifest.json";

        public static IReadOnlyList<string> GetAllVis02RelativePaths()
        {
            return new[]
            {
                DryRunResultRelativePath, RunResultRelativePath, RunHistoryRelativePath,
                WriteScopeManifestRelativePath, DigestManifestRelativePath,
            };
        }

        public static void Publish(string projectRoot, MoonPalaceGrayboxRunResult result,
            IReadOnlyList<MoonPalaceGrayboxRunResult> history)
        {
            if (string.IsNullOrEmpty(projectRoot)) throw new ArgumentException("Project root is required.", nameof(projectRoot));
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (history == null) throw new ArgumentNullException(nameof(history));

            Directory.CreateDirectory(Resolve(projectRoot, GeneratedDirectoryRelativePath));
            if (result.Mode == MoonPalaceGrayboxRunMode.DryRun)
            {
                Write(projectRoot, DryRunResultRelativePath, SerializeResult(result));
                return;
            }

            Write(projectRoot, DryRunResultRelativePath, SerializeResult(history.LastOrDefault(item =>
                item.Mode == MoonPalaceGrayboxRunMode.DryRun) ?? result));
            Write(projectRoot, RunResultRelativePath, SerializeResult(result));
            Write(projectRoot, RunHistoryRelativePath, SerializeHistory(history));
            Write(projectRoot, WriteScopeManifestRelativePath, SerializeWriteScope(result));
            Write(projectRoot, DigestManifestRelativePath, SerializeDigestManifest(projectRoot, result));
        }

        private static string SerializeResult(MoonPalaceGrayboxRunResult result)
        {
            var validation = result.ValidationSummary;
            var lines = new List<string>
            {
                "{",
                Pair("task_id", "VIS02_GENERATOR_WINDOW_ACTUAL_RUN_AND_REGENERATE_SCENE", true),
                Pair("mode", result.Mode.ToString(), true),
                Bool("success", result.Success, true),
                Bool("scene_written", result.SceneWritten, true),
                Bool("scene_deleted_before_write", result.SceneDeletedBeforeWrite, true),
                Bool("scene_recreated", result.SceneRecreated, true),
                Pair("seed_id", result.Request.SeedId, true),
                Number("seed_value", result.Request.SeedValue, true),
                Pair("sector", result.Request.SectorX.ToString(CultureInfo.InvariantCulture) + "," + result.Request.SectorY.ToString(CultureInfo.InvariantCulture), true),
                Number("candidate_count", result.Request.CandidateCount, true),
                Pair("request_digest", result.Request.RequestDigest, true),
                Pair("catalog_digest", result.CatalogDigest, true),
                Pair("candidate_digest", result.CandidateDigest, true),
                Pair("logical_map_digest", result.LogicalMapDigest, true),
                Pair("scene_manifest_digest", result.SceneManifestDigest, true),
                Number("csv_count", result.CsvCount, true),
                Number("json_count", result.JsonCount, true),
                Number("scene_count", result.SceneCount, true),
                Number("route_failures", validation == null ? -1 : validation.RouteFailureCount, true),
                Number("recovery_failures", validation == null ? -1 : validation.RecoveryFailureCount, true),
                Number("seam_failures", validation == null ? -1 : validation.SeamFailureCount, true),
                Number("unreachable_microchunks", validation == null ? -1 : validation.UnreachableMicroChunkCount, true),
                Number("fallback_carve_count", validation == null ? -1 : validation.FallbackCarveCount, true),
                Number("silent_auto_repair_count", validation == null ? -1 : validation.SilentAutoRepairCount, true),
                Pair("non_execution_summary", result.NonExecutionSummary, true),
                Pair("failure_reason", result.FailureReason, false),
                "}",
            };
            return string.Join("\n", lines) + "\n";
        }

        private static string SerializeHistory(IReadOnlyList<MoonPalaceGrayboxRunResult> history)
        {
            var lines = new List<string>
            {
                "sequence,mode,success,request_digest,catalog_digest,candidate_digest,logical_map_digest,scene_manifest_digest,scene_written,scene_recreated,route_failures,recovery_failures,seam_failures,unreachable_microchunks"
            };
            for (var index = 0; index < history.Count; index++) lines.Add(history[index].ToHistoryRow(index + 1));
            return string.Join("\n", lines) + "\n";
        }

        private static string SerializeWriteScope(MoonPalaceGrayboxRunResult result)
        {
            var entries = new List<KeyValuePair<string, string>>();
            entries.AddRange(result.WriteScopeSummary.Vis01RefreshPaths.Select(path =>
                new KeyValuePair<string, string>("VIS01_REFRESH", path)));
            entries.AddRange(result.WriteScopeSummary.Vis02HistoryPaths.Select(path =>
                new KeyValuePair<string, string>("VIS02_HISTORY", path)));
            entries.AddRange(result.WriteScopeSummary.TaskStatusOrReportPaths.Select(path =>
                new KeyValuePair<string, string>("TASK_STATUS_OR_REPORT", path)));
            var lines = new List<string>
            {
                "{",
                "  \"entries\": [",
            };
            for (var index = 0; index < entries.Count; index++)
            {
                var suffix = index + 1 == entries.Count ? string.Empty : ",";
                lines.Add("    {\"classification\":\"" + Escape(entries[index].Key) + "\",\"relative_path\":\"" +
                    Escape(entries[index].Value) + "\"}" + suffix);
            }
            lines.Add("  ],");
            lines.Add("  \"existing_scene_prefab_mutations_outside_vis01\": 0,");
            lines.Add("  \"build_settings_mutations\": 0,");
            lines.Add("  \"addressables_mutations\": 0,");
            lines.Add("  \"full_world_output_writes\": 0,");
            lines.Add("  \"full_world_runs\": 0");
            lines.Add("}");
            return string.Join("\n", lines) + "\n";
        }

        private static string SerializeDigestManifest(string projectRoot, MoonPalaceGrayboxRunResult result)
        {
            var artifactPaths = GetAllVis02RelativePaths().Where(path => path != DigestManifestRelativePath).ToList();
            var lines = new List<string>
            {
                "{",
                Pair("request_digest", result.Request.RequestDigest, true),
                Pair("catalog_digest", result.CatalogDigest, true),
                Pair("candidate_digest", result.CandidateDigest, true),
                Pair("logical_map_digest", result.LogicalMapDigest, true),
                Pair("scene_manifest_digest", result.SceneManifestDigest, true),
                "  \"artifacts\": [",
            };
            for (var index = 0; index < artifactPaths.Count; index++)
            {
                var path = artifactPaths[index];
                var text = File.ReadAllText(Resolve(projectRoot, path), BakingCanonicalDigest.Utf8NoBomEncoding);
                var suffix = index + 1 == artifactPaths.Count ? string.Empty : ",";
                lines.Add("    {\"relative_path\":\"" + Escape(path) + "\",\"sha256\":\"" +
                    BakingCanonicalDigest.HashCanonicalText(text) + "\"}" + suffix);
            }
            lines.Add("  ]");
            lines.Add("}");
            return string.Join("\n", lines) + "\n";
        }

        private static void Write(string projectRoot, string relativePath, string content)
        {
            File.WriteAllText(Resolve(projectRoot, relativePath), content.Replace("\r\n", "\n").Replace("\r", "\n"),
                BakingCanonicalDigest.Utf8NoBomEncoding);
        }

        private static string Resolve(string projectRoot, string relativePath)
        {
            return Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string Pair(string name, string value, bool trailingComma)
        {
            return "  \"" + Escape(name) + "\": \"" + Escape(value) + "\"" + (trailingComma ? "," : string.Empty);
        }
        private static string Number(string name, int value, bool trailingComma)
        {
            return "  \"" + Escape(name) + "\": " + value.ToString(CultureInfo.InvariantCulture) + (trailingComma ? "," : string.Empty);
        }
        private static string Bool(string name, bool value, bool trailingComma)
        {
            return "  \"" + Escape(name) + "\": " + (value ? "true" : "false") + (trailingComma ? "," : string.Empty);
        }
        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        }
    }
}
