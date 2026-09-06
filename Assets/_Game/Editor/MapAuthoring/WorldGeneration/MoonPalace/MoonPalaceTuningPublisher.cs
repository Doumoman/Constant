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
    public static class MoonPalaceTuningPublisher
    {
        public const string AuthoringDirectoryRelativePath = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_10";
        public const string GeneratedDirectoryRelativePath = "MapDesign/MCP/GENERATED/MAP21_10";
        public const string DensityWindowsFileName = "moonpalace_density_windows.csv";
        public const string BiomeTargetsFileName = "moonpalace_biome_tuning_targets.csv";
        public const string PacingRolesFileName = "moonpalace_pacing_role_targets.csv";
        public const string RepetitionRulesFileName = "moonpalace_repetition_distance_rules.csv";
        public const string SourceInventoryFileName = "moonpalace_source_inventory_snapshot.csv";
        public const string HandoffFileName = "moonpalace_tuning_handoff.csv";
        public const string TuningProfileManifestFileName = "moonpalace_tuning_profile_manifest.json";
        public const string DensityPacingManifestFileName = "moonpalace_density_pacing_manifest.json";
        public const string RepetitionPolicyManifestFileName = "moonpalace_repetition_policy_manifest.json";
        public const string DigestManifestFileName = "moonpalace_tuning_digest_manifest.json";

        public const string Map2109TaskPath = "MapDesign/MCP/TASKS/MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS.md";

        private static readonly SourceSpec[] SourceSpecs =
        {
            new SourceSpec("MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL", "MapDesign/MCP/REPORTS/MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL_RESULT.md", "MapDesign/MCP/GENERATED/MAP21_01/moonpalace_profile_digest_manifest.json", MoonPalaceTuningPreconditions.SourceMap2101Digest, "BiomeProfile", 4, "NONE", 0),
            new SourceSpec("MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS", "MapDesign/MCP/REPORTS/MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS_RESULT.md", "MapDesign/MCP/GENERATED/MAP21_02/moonpalace_micropattern_digest_manifest.json", MoonPalaceTuningPreconditions.SourceMap2102Digest, "MicroPattern", 24, "CellRecord", 384),
            new SourceSpec("MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS", "MapDesign/MCP/REPORTS/MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS_RESULT.md", "MapDesign/MCP/GENERATED/MAP21_03/moonpalace_crater_root_cluster_digest_manifest.json", MoonPalaceTuningPreconditions.SourceMap2103Digest, "TerrainCluster", 24, "Biome", 2),
            new SourceSpec("MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS", "MapDesign/MCP/REPORTS/MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS_RESULT.md", "MapDesign/MCP/GENERATED/MAP21_04/moonpalace_all_biome_cluster_pool_manifest.json", MoonPalaceTuningPreconditions.SourceMap2104Digest, "TerrainCluster", 48, "Biome", 4),
            new SourceSpec("MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS", "MapDesign/MCP/REPORTS/MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS_RESULT.md", "MapDesign/MCP/GENERATED/MAP21_05/moonpalace_activity_event_digest_manifest.json", MoonPalaceTuningPreconditions.SourceMap2105Digest, "Activity", 7, "EventOverlay", 5),
            new SourceSpec("MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS", "MapDesign/MCP/REPORTS/MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS_RESULT.md", "MapDesign/MCP/GENERATED/MAP21_06/moonpalace_boundary_digest_manifest.json", MoonPalaceTuningPreconditions.SourceMap2106Digest, "BoundaryCandidate", 48, "DirectionalProjection", 96),
            new SourceSpec("MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS", "MapDesign/MCP/REPORTS/MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS_RESULT.md", "MapDesign/MCP/GENERATED/MAP21_07/moonpalace_core_resource_digest_manifest.json", MoonPalaceTuningPreconditions.SourceMap2107Digest, "CoreResourceRegion", 3, "ActiveChunk", 15),
            new SourceSpec("MAP21_08_COMPLETE_MOONPALACE_VILLAGE", "MapDesign/MCP/REPORTS/MAP21_08_COMPLETE_MOONPALACE_VILLAGE_RESULT.md", "MapDesign/MCP/GENERATED/MAP21_08/moonpalace_village_digest_manifest.json", MoonPalaceTuningPreconditions.SourceMap2108Digest, "VillageLayout", 3, "VillageState", 5),
            new SourceSpec("MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS", "MapDesign/MCP/REPORTS/MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS_RESULT.md", "MapDesign/MCP/GENERATED/MAP21_09/moonpalace_landmark_digest_manifest.json", MoonPalaceTuningPreconditions.SourceMap2109Digest, "Landmark", 4, "Marker", 31),
        };

        public static MoonPalaceTuningPublishedSample CreateReadOnlySample(string projectRoot,
            string createdUtc, bool reverseInputOrder = false)
        {
            projectRoot = RequireRoot(projectRoot);
            var upstream = ValidateUpstream(projectRoot);
            var inventory = SourceSpecs.Select(spec => new MoonPalaceTuningSourceInventory(
                spec.TaskId, spec.ResultPath, upstream.Single(x => x.Name == spec.TaskId).Digest,
                spec.ManifestPath, spec.SemanticDigest, spec.PrimaryKind, spec.PrimaryCount,
                spec.SecondaryKind, spec.SecondaryCount)).ToArray();

            var windows = new[]
            {
                new MoonPalaceDensityWindow("DENSITY_QUIET_RATIO", "sector/window", .50, .55, .60, "calm or low-pressure space budget", "SpecialRegion reserved cells"),
                new MoonPalaceDensityWindow("DENSITY_CLUSTER_RATIO", "sector/window", .25, .30, .35, "active terrain cluster budget", "SpecialRegion reserved cells"),
                new MoonPalaceDensityWindow("DENSITY_ACTIVITY_RATIO", "world/window", .06, .09, .12, "strong Activity opportunity frequency", "boundary cells"),
                new MoonPalaceDensityWindow("DENSITY_OVERLAY_RATIO", "world/window", .03, .05, .08, "non-empty EventOverlay frequency", "empty overlay variants"),
            };
            var biomes = new[]
            {
                Target("Biome", "MoonCrater", .51, .34, .10, .06, "more active impact terrain"),
                Target("Biome", "CassiaRoot", .56, .30, .08, .05, "calmer recovery space"),
                Target("Biome", "AbandonedMill", .52, .33, .11, .07, "mechanical activity pressure"),
                Target("Biome", "MoonDough", .58, .27, .07, .04, "soft quiet-heavy traversal"),
            };
            var roles = new[]
            {
                Target("PacingRole", "StartBuffer", .60, .25, .06, .03, "quiet onboarding buffer"),
                Target("PacingRole", "MainRoute", .52, .34, .10, .06, "active primary traversal"),
                Target("PacingRole", "BranchRoute", .53, .32, .11, .07, "optional discovery pressure"),
                Target("PacingRole", "RecoveryRoute", .58, .27, .07, .04, "recovery-biased traversal"),
                Target("PacingRole", "QuietBuffer", .60, .25, .06, .03, "maximum quiet budget"),
                Target("PacingRole", "SpecialApproach", .55, .30, .09, .05, "neutral landmark approach"),
                Target("PacingRole", "BoundarySeam", .54, .31, .08, .05, "transition without activity inflation"),
            };
            var rules = new[]
            {
                Rule("REPEAT_PATTERN_EXACT_ID", "same MicroPattern ID", 3, "sector placements", "per biome sector window", "micro_pattern_id"),
                Rule("REPEAT_PATTERN_MIRROR_FAMILY", "mirrored silhouette family", 2, "sector placements", "per biome sector window", "mirror_family_id", false, true),
                Rule("REPEAT_CLUSTER_EXACT_ID", "same TerrainCluster ID", 6, "sector placements", "per biome sector window", "terrain_cluster_id"),
                Rule("REPEAT_CLUSTER_STRUCTURAL_SIGNATURE", "same cluster structural signature", 4, "sector placements", "per biome sector window", "cluster_structural_signature"),
                Rule("REPEAT_CLUSTER_SILHOUETTE_SIGNATURE", "same cluster silhouette signature", 3, "sector placements", "per biome sector window", "cluster_silhouette_signature"),
                Rule("REPEAT_ACTIVITY_EXACT_ID", "same Activity ID", 8, "sector placements", "per world window", "activity_id"),
                Rule("REPEAT_EVENT_NON_EMPTY_ID", "same non-empty EventOverlay ID", 6, "sector placements", "per world window", "non_empty_event_overlay_id"),
                Rule("REPEAT_BOUNDARY_CANDIDATE_ID", "same boundary candidate ID within same pair/direction", 4, "boundary placements", "per pair and direction window", "boundary_pair_direction_candidate_id"),
            };
            if (reverseInputOrder)
            {
                Array.Reverse(windows); Array.Reverse(biomes); Array.Reverse(roles);
                Array.Reverse(rules); Array.Reverse(inventory);
            }
            var profile = new MoonPalaceTuningProfile(windows, biomes, roles, rules, inventory, createdUtc);
            var profileJson = profile.SerializeTuningProfileManifest();
            var densityJson = profile.SerializeDensityPacingManifest();
            var repetitionJson = profile.SerializeRepetitionPolicyManifest();
            var csv = new[]
            {
                Named(DensityWindowsFileName, profile.SerializeDensityWindowsCsv()),
                Named(BiomeTargetsFileName, profile.SerializeBiomeTargetsCsv()),
                Named(PacingRolesFileName, profile.SerializePacingRoleTargetsCsv()),
                Named(RepetitionRulesFileName, profile.SerializeRepetitionRulesCsv()),
                Named(SourceInventoryFileName, profile.SerializeSourceInventoryCsv()),
                Named(HandoffFileName, profile.SerializeHandoffCsv()),
            };
            var json = new[]
            {
                Named(TuningProfileManifestFileName, profileJson),
                Named(DensityPacingManifestFileName, densityJson),
                Named(RepetitionPolicyManifestFileName, repetitionJson),
            };
            var semantic = SourceSpecs.Select(x => new MoonPalaceNamedDigest(x.TaskId, x.SemanticDigest));
            var digest = new MoonPalaceTuningDigestManifest(profile, upstream, semantic, csv, json, createdUtc);
            return new MoonPalaceTuningPublishedSample(profile, digest,
                MoonPalaceTuningForbiddenOperationCounters.Zero, Array.Empty<string>(), SourcePaths());
        }

        public static MoonPalaceTuningPublishedSample PublishAuthoringAndSamples(
            string projectRoot, bool focusedMap2110Pass)
        {
            if (!focusedMap2110Pass) throw new InvalidOperationException(
                "MAP21_11 handoff requires focused MAP21_10 PASS.");
            projectRoot = RequireRoot(projectRoot);
            var sample = CreateReadOnlySample(projectRoot,
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(Resolve(projectRoot, AuthoringDirectoryRelativePath));
            Directory.CreateDirectory(Resolve(projectRoot, GeneratedDirectoryRelativePath));
            var paths = new[]
            {
                Combine(AuthoringDirectoryRelativePath, DensityWindowsFileName),
                Combine(AuthoringDirectoryRelativePath, BiomeTargetsFileName),
                Combine(AuthoringDirectoryRelativePath, PacingRolesFileName),
                Combine(AuthoringDirectoryRelativePath, RepetitionRulesFileName),
                Combine(AuthoringDirectoryRelativePath, SourceInventoryFileName),
                Combine(AuthoringDirectoryRelativePath, HandoffFileName),
                Combine(GeneratedDirectoryRelativePath, TuningProfileManifestFileName),
                Combine(GeneratedDirectoryRelativePath, DensityPacingManifestFileName),
                Combine(GeneratedDirectoryRelativePath, RepetitionPolicyManifestFileName),
                Combine(GeneratedDirectoryRelativePath, DigestManifestFileName),
            };
            var contents = new[]
            {
                sample.Profile.SerializeDensityWindowsCsv(), sample.Profile.SerializeBiomeTargetsCsv(),
                sample.Profile.SerializePacingRoleTargetsCsv(), sample.Profile.SerializeRepetitionRulesCsv(),
                sample.Profile.SerializeSourceInventoryCsv(), sample.Profile.SerializeHandoffCsv(),
                sample.Profile.SerializeTuningProfileManifest(), sample.Profile.SerializeDensityPacingManifest(),
                sample.Profile.SerializeRepetitionPolicyManifest(), sample.DigestManifest.Serialize(),
            };
            for (var index = 0; index < paths.Length; index++) Write(projectRoot, paths[index], contents[index]);
            return new MoonPalaceTuningPublishedSample(sample.Profile, sample.DigestManifest,
                sample.Counters, paths, sample.SourceReadRelativePaths);
        }

        private static IReadOnlyList<MoonPalaceNamedDigest> ValidateUpstream(string root)
        {
            var observed = new List<MoonPalaceNamedDigest>();
            foreach (var spec in SourceSpecs)
            {
                var result = Resolve(root, spec.ResultPath);
                if (!File.Exists(result)) throw new FileNotFoundException(spec.ResultPath, result);
                var lines = File.ReadAllLines(result);
                if (lines.Count(x => x == "TASK: " + spec.TaskId) != 1 || lines.Count(x => x == "STATUS: PASS") != 1)
                    throw new InvalidDataException("Source Result identity mismatch: " + spec.ResultPath);
                var manifest = Resolve(root, spec.ManifestPath);
                if (!File.Exists(manifest)) throw new FileNotFoundException(spec.ManifestPath, manifest);
                var document = JsonUtility.FromJson<SourceDigestDocument>(File.ReadAllText(manifest));
                var actual = spec.TaskId.StartsWith("MAP21_01", StringComparison.Ordinal) ? document.biome_profile_digest : document.canonical_digest;
                if (actual != spec.SemanticDigest)
                    throw new InvalidDataException("Source semantic digest mismatch: " + spec.ManifestPath);
                if (spec.TaskId.StartsWith("MAP21_09", StringComparison.Ordinal) &&
                    document.MAP21_10_handoff_digest != MoonPalaceTuningPreconditions.StrictMap2110HandoffDigest)
                    throw new InvalidDataException("MAP21_10 handoff strict digest mismatch.");
                observed.Add(new MoonPalaceNamedDigest(spec.TaskId, FileSha(result)));
            }
            var map2109Result = observed.Single(x => x.Name.StartsWith("MAP21_09", StringComparison.Ordinal));
            if (map2109Result.Digest != MoonPalaceTuningPreconditions.StrictMap2109ResultDigest)
                throw new InvalidDataException("MAP21_09 Result strict SHA mismatch.");
            if (FileSha(Resolve(root, Map2109TaskPath)) != MoonPalaceTuningPreconditions.StrictMap2109TaskDigest)
                throw new InvalidDataException("MAP21_09 installed Task strict SHA mismatch.");
            return new ReadOnlyCollection<MoonPalaceNamedDigest>(observed.OrderBy(x => x.Name, StringComparer.Ordinal).ToArray());
        }

        private static MoonPalaceTuningTarget Target(string kind, string id, double quiet,
            double cluster, double activity, double overlay, string note) =>
            new MoonPalaceTuningTarget(kind, id, quiet, cluster, activity, overlay, note);
        private static MoonPalaceRepetitionDistanceRule Rule(string id, string subject, int separation,
            string unit, string scope, string field, bool materialCounts = false, bool mirrorCounts = false) =>
            new MoonPalaceRepetitionDistanceRule(id, subject, separation, unit, scope, field, materialCounts, mirrorCounts);
        private static MoonPalaceNamedDigest Named(string name, string content) =>
            new MoonPalaceNamedDigest(name, BakingCanonicalDigest.HashCanonicalText(content));
        private static string[] SourcePaths() => SourceSpecs.SelectMany(x => new[] { x.ResultPath, x.ManifestPath })
            .Concat(new[] { Map2109TaskPath }).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        private static void Write(string root, string relative, string content) => File.WriteAllText(
            Resolve(root, relative), content, BakingCanonicalDigest.Utf8NoBomEncoding);
        private static string Combine(string left, string right) => left.TrimEnd('/') + "/" + right;
        private static string RequireRoot(string root) { if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException(nameof(root)); return Path.GetFullPath(root); }
        private static string Resolve(string root, string relative) => Path.GetFullPath(Path.Combine(RequireRoot(root), relative.Replace('/', Path.DirectorySeparatorChar)));
        private static string FileSha(string path) { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(stream).Select(x => x.ToString("x2", CultureInfo.InvariantCulture))); }

        private sealed class SourceSpec
        {
            public SourceSpec(string taskId, string resultPath, string manifestPath,
                string semanticDigest, string primaryKind, int primaryCount, string secondaryKind, int secondaryCount)
            { TaskId = taskId; ResultPath = resultPath; ManifestPath = manifestPath; SemanticDigest = semanticDigest;
                PrimaryKind = primaryKind; PrimaryCount = primaryCount; SecondaryKind = secondaryKind; SecondaryCount = secondaryCount; }
            public string TaskId { get; } public string ResultPath { get; } public string ManifestPath { get; }
            public string SemanticDigest { get; } public string PrimaryKind { get; } public int PrimaryCount { get; }
            public string SecondaryKind { get; } public int SecondaryCount { get; }
        }
        [Serializable] private sealed class SourceDigestDocument
        { public string biome_profile_digest; public string canonical_digest; public string MAP21_10_handoff_digest; }
    }

    public sealed class MoonPalaceTuningPublishedSample
    {
        private readonly ReadOnlyCollection<string> written;
        private readonly ReadOnlyCollection<string> sourceReads;
        public MoonPalaceTuningPublishedSample(MoonPalaceTuningProfile profile,
            MoonPalaceTuningDigestManifest digestManifest, MoonPalaceTuningForbiddenOperationCounters counters,
            IEnumerable<string> writtenPaths, IEnumerable<string> sourceReadPaths)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            DigestManifest = digestManifest ?? throw new ArgumentNullException(nameof(digestManifest));
            Counters = counters ?? throw new ArgumentNullException(nameof(counters));
            written = new ReadOnlyCollection<string>((writtenPaths ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal).ToArray());
            sourceReads = new ReadOnlyCollection<string>((sourceReadPaths ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        }
        public MoonPalaceTuningProfile Profile { get; } public MoonPalaceTuningDigestManifest DigestManifest { get; }
        public MoonPalaceTuningForbiddenOperationCounters Counters { get; }
        public IReadOnlyList<string> WrittenRelativePaths => written;
        public IReadOnlyList<string> SourceReadRelativePaths => sourceReads;
    }
}
