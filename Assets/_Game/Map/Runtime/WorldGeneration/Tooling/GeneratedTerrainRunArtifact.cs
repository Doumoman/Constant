using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.Tooling
{
    public static class GeneratedTerrainRunPreconditions
    {
        public const string TaskId = "MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK";
        public const string SchemaVersion = "map20_01.generated_terrain_run.v1";
        public const string Map19ExitDigest =
            "0f5bac81722f727dd02772c76323c9fa69ce91ad3d1a3722cef2aa47381ccec2";
        public const string Map2001HandoffDigest =
            "2e69b77f8720d30e6c1d9fcdd7bdeb883ea36540033422e4908367ce60377c3ce";
    }

    [Serializable]
    public sealed class GeneratedTerrainRunRequest
    {
        public GeneratedTerrainRunRequest(string runId, GeneratedTerrainRunScope scope, ulong seed,
            string generatorVersion, string dataVersion, string inputDigest,
            string outputRootRelative, string patternId, int sectorX, int sectorY,
            bool worldConfirmed, bool dryRun)
        {
            RunId = runId ?? string.Empty;
            Scope = scope;
            Seed = seed;
            GeneratorVersion = generatorVersion ?? string.Empty;
            DataVersion = dataVersion ?? string.Empty;
            InputDigest = inputDigest ?? string.Empty;
            OutputRootRelative = NormalizePath(outputRootRelative);
            PatternId = patternId ?? string.Empty;
            SectorX = sectorX;
            SectorY = sectorY;
            WorldConfirmed = worldConfirmed;
            DryRun = dryRun;
        }

        public string RunId { get; }
        public GeneratedTerrainRunScope Scope { get; }
        public ulong Seed { get; }
        public string GeneratorVersion { get; }
        public string DataVersion { get; }
        public string InputDigest { get; }
        public string OutputRootRelative { get; }
        public string PatternId { get; }
        public int SectorX { get; }
        public int SectorY { get; }
        public bool WorldConfirmed { get; }
        public bool DryRun { get; }

        public IReadOnlyList<string> OutputBoundaries =>
            GeneratedTerrainRunScopeCatalog.ResolveOutputBoundaries(
                Scope, RunId, PatternId, SectorX, SectorY);

        public string ReplayReference => string.Join("&", new[]
        {
            "map20://replay?run_id=" + Uri.EscapeDataString(RunId),
            "seed=" + Seed.ToString(CultureInfo.InvariantCulture),
            "scope=" + GeneratedTerrainRunScopeCatalog.Token(Scope),
            "generator_version=" + Uri.EscapeDataString(GeneratorVersion),
            "data_version=" + Uri.EscapeDataString(DataVersion),
            "input_digest=" + InputDigest,
        });

        public static string ComputeInputDigest(ulong seed, GeneratedTerrainRunScope scope,
            string generatorVersion, string dataVersion, string patternId, int sectorX, int sectorY)
            => BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP20_01_INPUT_V1",
                seed.ToString(CultureInfo.InvariantCulture),
                GeneratedTerrainRunScopeCatalog.Token(scope),
                generatorVersion ?? string.Empty,
                dataVersion ?? string.Empty,
                patternId ?? string.Empty,
                sectorX.ToString(CultureInfo.InvariantCulture),
                sectorY.ToString(CultureInfo.InvariantCulture),
                GeneratedTerrainRunPreconditions.Map19ExitDigest,
                GeneratedTerrainRunPreconditions.Map2001HandoffDigest,
            });

        private static string NormalizePath(string value) =>
            (value ?? string.Empty).Replace('\\', '/').Trim('/');
    }

    [Serializable]
    public sealed class GeneratedTerrainOutputMutation
    {
        public GeneratedTerrainOutputMutation(string relativePath, string utf8Content, bool delete)
        {
            RelativePath = Normalize(relativePath);
            Utf8Content = utf8Content ?? string.Empty;
            Delete = delete;
        }

        public string RelativePath { get; }
        public string Utf8Content { get; }
        public bool Delete { get; }

        private static string Normalize(string value) =>
            (value ?? string.Empty).Replace('\\', '/').Trim('/');
    }

    public sealed class GeneratedTerrainRunPlan
    {
        private readonly ReadOnlyCollection<GeneratedTerrainOutputMutation> mutations;

        public GeneratedTerrainRunPlan(IEnumerable<GeneratedTerrainOutputMutation> sourceMutations,
            string failureOwner = "", string failureReason = "", bool failAfterMutation = false,
            string failureBundleReference = "")
        {
            mutations = new ReadOnlyCollection<GeneratedTerrainOutputMutation>((sourceMutations ??
                Array.Empty<GeneratedTerrainOutputMutation>()).Where(value => value != null)
                .OrderBy(value => value.RelativePath, StringComparer.Ordinal).ToArray());
            FailureOwner = failureOwner ?? string.Empty;
            FailureReason = failureReason ?? string.Empty;
            FailAfterMutation = failAfterMutation;
            FailureBundleReference = failureBundleReference ?? string.Empty;
        }

        public IReadOnlyList<GeneratedTerrainOutputMutation> Mutations => mutations;
        public string FailureOwner { get; }
        public string FailureReason { get; }
        public bool FailAfterMutation { get; }
        public string FailureBundleReference { get; }
        public bool IsFailure => !string.IsNullOrEmpty(FailureOwner) || !string.IsNullOrEmpty(FailureReason);

        public static GeneratedTerrainRunPlan DryRun() =>
            new GeneratedTerrainRunPlan(Array.Empty<GeneratedTerrainOutputMutation>());
    }

    [Serializable]
    public sealed class GeneratedTerrainRunArtifact
    {
        public string schema_version;
        public string task_id;
        public string run_id;
        public string scope;
        public string seed;
        public string generator_version;
        public string data_version;
        public string input_digest;
        public string pre_run_output_digest;
        public string post_run_output_digest;
        public string pass_state;
        public string failure_owner;
        public string failure_reason;
        public bool rollback_available;
        public string rollback_snapshot_digest;
        public bool rollback_applied;
        public int changed_path_count;
        public int created_path_count;
        public int deleted_path_count;
        public string[] changed_paths = Array.Empty<string>();
        public string[] created_paths = Array.Empty<string>();
        public string[] deleted_paths = Array.Empty<string>();
        public string replay_reference;
        public string failure_bundle_reference;
        public string MAP19_exit_digest;
        public string MAP20_01_handoff_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static string SchemaDigest => BakingCanonicalDigest.HashCanonicalLines(new[]
        {
            nameof(schema_version), nameof(task_id), nameof(run_id), nameof(scope), nameof(seed),
            nameof(generator_version), nameof(data_version), nameof(input_digest),
            nameof(pre_run_output_digest), nameof(post_run_output_digest), nameof(pass_state),
            nameof(failure_owner), nameof(failure_reason), nameof(rollback_available),
            nameof(rollback_snapshot_digest), nameof(rollback_applied), nameof(changed_path_count),
            nameof(created_path_count), nameof(deleted_path_count), nameof(replay_reference),
            nameof(failure_bundle_reference), nameof(MAP19_exit_digest),
            nameof(MAP20_01_handoff_digest), nameof(created_utc_excluded_from_canonical_digest),
            nameof(canonical_digest),
        });

        public string SealAndSerialize()
        {
            changed_paths = NormalizePaths(changed_paths);
            created_paths = NormalizePaths(created_paths);
            deleted_paths = NormalizePaths(deleted_paths);
            changed_path_count = changed_paths.Length;
            created_path_count = created_paths.Length;
            deleted_path_count = deleted_paths.Length;
            created_utc_excluded_from_canonical_digest = true;
            canonical_digest = ComputeCanonicalDigest();
            return BakingCanonicalDigest.NormalizeLineEndingsToLf(JsonUtility.ToJson(this, true)) + "\n";
        }

        public string ComputeCanonicalDigest() => BakingCanonicalDigest.HashCanonicalLines(new[]
        {
            Pair(nameof(schema_version), schema_version), Pair(nameof(task_id), task_id),
            Pair(nameof(run_id), run_id), Pair(nameof(scope), scope), Pair(nameof(seed), seed),
            Pair(nameof(generator_version), generator_version), Pair(nameof(data_version), data_version),
            Pair(nameof(input_digest), input_digest), Pair(nameof(pre_run_output_digest), pre_run_output_digest),
            Pair(nameof(post_run_output_digest), post_run_output_digest), Pair(nameof(pass_state), pass_state),
            Pair(nameof(failure_owner), failure_owner), Pair(nameof(failure_reason), failure_reason),
            Pair(nameof(rollback_available), rollback_available ? "true" : "false"),
            Pair(nameof(rollback_snapshot_digest), rollback_snapshot_digest),
            Pair(nameof(rollback_applied), rollback_applied ? "true" : "false"),
            Pair(nameof(changed_path_count), NormalizePaths(changed_paths).Length.ToString(CultureInfo.InvariantCulture)),
            Pair(nameof(created_path_count), NormalizePaths(created_paths).Length.ToString(CultureInfo.InvariantCulture)),
            Pair(nameof(deleted_path_count), NormalizePaths(deleted_paths).Length.ToString(CultureInfo.InvariantCulture)),
            Pair(nameof(changed_paths), string.Join("|", NormalizePaths(changed_paths))),
            Pair(nameof(created_paths), string.Join("|", NormalizePaths(created_paths))),
            Pair(nameof(deleted_paths), string.Join("|", NormalizePaths(deleted_paths))),
            Pair(nameof(replay_reference), replay_reference),
            Pair(nameof(failure_bundle_reference), failure_bundle_reference),
            Pair(nameof(MAP19_exit_digest), MAP19_exit_digest),
            Pair(nameof(MAP20_01_handoff_digest), MAP20_01_handoff_digest),
            Pair(nameof(created_utc_excluded_from_canonical_digest), "true"),
        });

        private static string Pair(string name, string value) => name + "=" + (value ?? string.Empty);

        internal static string[] NormalizePaths(IEnumerable<string> paths) => (paths ??
            Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Replace('\\', '/').Trim('/'))
            .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    [Serializable]
    public sealed class GeneratedTerrainRollbackSnapshotEntry
    {
        public string relative_path;
        public string content_digest;
        public long byte_count;
        public string backup_relative_path;
    }

    [Serializable]
    public sealed class GeneratedTerrainRollbackSnapshotManifest
    {
        public string schema_version = "map20_01.rollback_snapshot.v1";
        public string run_id;
        public string scope;
        public string output_root_relative;
        public string[] selected_boundaries = Array.Empty<string>();
        public GeneratedTerrainRollbackSnapshotEntry[] entries =
            Array.Empty<GeneratedTerrainRollbackSnapshotEntry>();
        public string pre_run_output_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest = true;
        public string canonical_digest;

        public string SealAndSerialize()
        {
            selected_boundaries = GeneratedTerrainRunArtifact.NormalizePaths(selected_boundaries);
            entries = (entries ?? Array.Empty<GeneratedTerrainRollbackSnapshotEntry>())
                .Where(value => value != null)
                .OrderBy(value => value.relative_path, StringComparer.Ordinal).ToArray();
            canonical_digest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                schema_version ?? string.Empty, run_id ?? string.Empty, scope ?? string.Empty,
                output_root_relative ?? string.Empty, string.Join("|", selected_boundaries),
                pre_run_output_digest ?? string.Empty,
                string.Join("|", entries.Select(value => string.Join("/", new[]
                {
                    value.relative_path ?? string.Empty,
                    value.content_digest ?? string.Empty,
                    value.byte_count.ToString(CultureInfo.InvariantCulture),
                    value.backup_relative_path ?? string.Empty,
                }))),
                "created_utc_excluded=true",
            });
            return BakingCanonicalDigest.NormalizeLineEndingsToLf(JsonUtility.ToJson(this, true)) + "\n";
        }
    }

    [Serializable]
    public sealed class GeneratedTerrainRollbackResult
    {
        public string schema_version = "map20_01.rollback_result.v1";
        public string run_id;
        public string scope;
        public string pass_state;
        public string reason;
        public int restored_path_count;
        public int removed_path_count;
        public string[] selected_boundaries = Array.Empty<string>();
        public string snapshot_digest;
        public string post_rollback_output_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest = true;
        public string canonical_digest;

        public string SealAndSerialize()
        {
            selected_boundaries = GeneratedTerrainRunArtifact.NormalizePaths(selected_boundaries);
            canonical_digest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                schema_version ?? string.Empty, run_id ?? string.Empty, scope ?? string.Empty,
                pass_state ?? string.Empty, reason ?? string.Empty,
                restored_path_count.ToString(CultureInfo.InvariantCulture),
                removed_path_count.ToString(CultureInfo.InvariantCulture),
                string.Join("|", selected_boundaries), snapshot_digest ?? string.Empty,
                post_rollback_output_digest ?? string.Empty, "created_utc_excluded=true",
            });
            return BakingCanonicalDigest.NormalizeLineEndingsToLf(JsonUtility.ToJson(this, true)) + "\n";
        }
    }
}
