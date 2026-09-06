using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.MoonPalace;
using StarNight.Map.WorldGeneration.Validation;

namespace StarNight.MapAuthoring.Editor.WorldGeneration.MoonPalace
{
    public static class MoonPalaceProfilePublisher
    {
        public const string MovementCsvRelativePath =
            "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_01/moonpalace_movement_profile.csv";
        public const string BiomeCsvRelativePath =
            "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_01/moonpalace_biome_profiles.csv";
        public const string TileShellCsvRelativePath =
            "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_01/moonpalace_tile_shell.csv";
        public const string OutputDirectoryRelativePath =
            "MapDesign/MCP/GENERATED/MAP21_01";
        public const string ProfileManifestFileName = "moonpalace_profile_manifest.json";
        public const string TileShellManifestFileName = "moonpalace_tile_shell_manifest.json";
        public const string DigestManifestFileName =
            "moonpalace_profile_digest_manifest.json";

        private const string Map2006ResultRelativePath =
            "MapDesign/MCP/REPORTS/MAP20_06_MAP20_TOOLING_EXIT_TESTS_RESULT.md";
        private const string Map2006TaskRelativePath =
            "MapDesign/MCP/TASKS/MAP20_06_MAP20_TOOLING_EXIT_TESTS.md";
        private const string Map2006DigestManifestRelativePath =
            "MapDesign/MCP/GENERATED/MAP20_06/map20_digest_chain_manifest.json";

        public static MoonPalacePublishedSample CreateReadOnlySample(string projectRoot,
            string createdUtc, bool reverseInputOrder = false)
        {
            projectRoot = RequireProjectRoot(projectRoot);
            ValidateUpstreamPreconditions(projectRoot);

            var movementRows = ReadRows(projectRoot, MovementCsvRelativePath);
            if (movementRows.Count != 1)
                throw new InvalidDataException("Movement CSV must contain exactly one row.");
            var movement = Movement(movementRows[0]);
            var biomeRows = ReadRows(projectRoot, BiomeCsvRelativePath);
            var tileRows = ReadRows(projectRoot, TileShellCsvRelativePath);
            if (reverseInputOrder)
            {
                biomeRows.Reverse();
                tileRows.Reverse();
            }

            var traversal = GeneratedTraversalProfileCatalog.Create();
            ValidateTraversalCompatibility(movement, traversal);
            var profile = new MoonPalaceProductionProfile(movement,
                biomeRows.Select(Biome), traversal.Digest, createdUtc);
            var tileShell = new MoonPalaceTileShell(tileRows.Select(Tile), createdUtc);
            var map2102HandoffDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_02_MOONPALACE_PROFILE_TILE_SHELL_HANDOFF_V1",
                MoonPalaceProfilePreconditions.Map2101HandoffDigest,
                movement.TraversalProfileDigest,
                profile.BiomeProfileDigest,
                tileShell.CanonicalDigest,
                traversal.Digest,
            });
            var digestManifest = new MoonPalaceProfileDigestManifest(
                movement.TraversalProfileDigest, profile.BiomeProfileDigest,
                tileShell.CanonicalDigest, map2102HandoffDigest, createdUtc);
            return new MoonPalacePublishedSample(profile, tileShell, digestManifest,
                MoonPalaceForbiddenOperationCounters.Zero);
        }

        public static MoonPalacePublishedSample PublishSamples(string projectRoot,
            bool focusedMap2101Pass)
        {
            if (!focusedMap2101Pass)
                throw new InvalidOperationException(
                    "MAP21_02 handoff publication requires focused MAP21_01 PASS.");
            var sample = CreateReadOnlySample(projectRoot,
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            var outputDirectory = Resolve(projectRoot, OutputDirectoryRelativePath);
            Directory.CreateDirectory(outputDirectory);
            File.WriteAllText(Path.Combine(outputDirectory, ProfileManifestFileName),
                sample.Profile.Serialize(), BakingCanonicalDigest.Utf8NoBomEncoding);
            File.WriteAllText(Path.Combine(outputDirectory, TileShellManifestFileName),
                sample.TileShell.Serialize(), BakingCanonicalDigest.Utf8NoBomEncoding);
            File.WriteAllText(Path.Combine(outputDirectory, DigestManifestFileName),
                sample.DigestManifest.Serialize(), BakingCanonicalDigest.Utf8NoBomEncoding);
            return sample;
        }

        private static MoonPalaceMovementProfile Movement(
            IReadOnlyDictionary<string, string> row) => new MoonPalaceMovementProfile(
                Value(row, "profile_id"), Value(row, "schema_version"),
                Value(row, "source_MAP20_phase_exit_digest"),
                Number(row, "walk_speed_tiles_per_second"),
                Number(row, "run_speed_tiles_per_second"),
                Number(row, "jump_height_tiles"), Number(row, "jump_distance_tiles"),
                Number(row, "max_safe_drop_tiles"), Number(row, "player_width_cells"),
                Number(row, "player_height_cells"), Number(row, "head_clearance_cells"),
                Number(row, "landing_clearance_cells"),
                Number(row, "recovery_time_seconds_min"),
                Number(row, "recovery_time_seconds_max"));

        private static MoonPalaceBiomeProfile Biome(IReadOnlyDictionary<string, string> row) =>
            new MoonPalaceBiomeProfile(Value(row, "biome_id"),
                Number(row, "density_min"), Number(row, "density_max"),
                Number(row, "quiet_ratio_min"), Number(row, "quiet_ratio_max"),
                Number(row, "cluster_ratio_min"), Number(row, "cluster_ratio_max"),
                Number(row, "activity_ratio_min"), Number(row, "activity_ratio_max"),
                Number(row, "overlay_ratio_min"), Number(row, "overlay_ratio_max"),
                Value(row, "verticality_band"), Value(row, "difficulty_band"),
                Value(row, "primary_material_token"),
                Value(row, "ambient_audio_token"), Value(row, "background_token"));

        private static MoonPalaceTileShellRecord Tile(
            IReadOnlyDictionary<string, string> row) => new MoonPalaceTileShellRecord(
                Value(row, "tile_code"), EnumValue<MoonPalaceTileRole>(row, "tile_role"),
                Value(row, "layer_token"),
                EnumValue<MoonPalaceCollisionKind>(row, "collision_kind"),
                Value(row, "material_token"), Value(row, "footstep_audio_token"),
                Value(row, "impact_audio_token"), Value(row, "background_token"),
                Value(row, "biome_allowlist").Split(new[] { ';' },
                    StringSplitOptions.RemoveEmptyEntries),
                EnumValue<MoonPalaceAssetReferenceKind>(row, "asset_reference_kind"),
                Value(row, "asset_reference", false), Value(row, "fallback_tile_code"),
                Value(row, "missing_reason"), Integer(row, "render_priority"));

        private static List<IReadOnlyDictionary<string, string>> ReadRows(
            string projectRoot, string relativePath)
        {
            var path = Resolve(projectRoot, relativePath);
            var read = new Rfc4180CsvReader().Read(File.ReadAllBytes(path), relativePath);
            if (!read.Success)
                throw new InvalidDataException("RFC4180 CSV read failed for " + relativePath);
            if (read.Records.Count < 2)
                throw new InvalidDataException("CSV requires a header and data: " + relativePath);
            var headers = read.Records[0].Fields.Select(field => field.Value.Trim()).ToArray();
            if (headers.Any(value => value.Length == 0) ||
                headers.Distinct(StringComparer.Ordinal).Count() != headers.Length)
                throw new InvalidDataException("CSV headers must be unique and non-empty.");
            var rows = new List<IReadOnlyDictionary<string, string>>();
            foreach (var record in read.Records.Skip(1))
            {
                if (record.Fields.Count != headers.Length)
                    throw new InvalidDataException("CSV row width mismatch in " + relativePath);
                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var index = 0; index < headers.Length; index++)
                    row.Add(headers[index], record.Fields[index].Value.Trim());
                rows.Add(row);
            }
            return rows;
        }

        private static void ValidateUpstreamPreconditions(string projectRoot)
        {
            if (!string.Equals(FileSha256(Resolve(projectRoot, Map2006ResultRelativePath)),
                    MoonPalaceProfilePreconditions.SourceMap2006ResultDigest,
                    StringComparison.Ordinal))
                throw new InvalidDataException("MAP20_06 Result SHA-256 mismatch.");
            if (!string.Equals(FileSha256(Resolve(projectRoot, Map2006TaskRelativePath)),
                    MoonPalaceProfilePreconditions.SourceMap2006TaskDigest,
                    StringComparison.Ordinal))
                throw new InvalidDataException("MAP20_06 task SHA-256 mismatch.");
            var exitManifest = File.ReadAllText(Resolve(projectRoot,
                Map2006DigestManifestRelativePath));
            if (!exitManifest.Contains(MoonPalaceProfilePreconditions.SourceMap20PhaseExitDigest) ||
                !exitManifest.Contains(MoonPalaceProfilePreconditions.Map2101HandoffDigest))
                throw new InvalidDataException("MAP20 exit handoff digests are absent.");
        }

        private static void ValidateTraversalCompatibility(MoonPalaceMovementProfile movement,
            GeneratedTraversalProfile traversal)
        {
            var values = traversal.Capabilities.ToDictionary(value => value.Key,
                StringComparer.Ordinal);
            Exact(values, "collider.footprint_width_tiles", movement.PlayerWidthCells);
            Exact(values, "collider.footprint_height_tiles", movement.PlayerHeightCells);
            Exact(values, "collider.clearance_height_tiles", movement.HeadClearanceCells);
            Range(values, "jump.horizontal_range_tiles", movement.JumpDistanceTiles);
            Range(values, "jump.vertical_rise_tiles", movement.JumpHeightTiles);
            Range(values, "fall.safe_drop_tiles", movement.MaxSafeDropTiles);
            var landing = values["jump.minimum_landing_width_tiles"];
            if ((decimal)movement.LandingClearanceCells < landing.MinimumValue)
                throw new InvalidDataException("Landing clearance is below MAP19 lock.");
        }

        private static void Exact(
            IReadOnlyDictionary<string, GeneratedTraversalCapabilityValue> values,
            string key, double actual)
        {
            var expected = values[key];
            if ((decimal)actual != expected.MinimumValue ||
                expected.MinimumValue != expected.MaximumValue)
                throw new InvalidDataException(key + " conflicts with MAP19 traversal lock.");
        }

        private static void Range(
            IReadOnlyDictionary<string, GeneratedTraversalCapabilityValue> values,
            string key, double actual)
        {
            var expected = values[key];
            if ((decimal)actual < expected.MinimumValue ||
                (decimal)actual > expected.MaximumValue)
                throw new InvalidDataException(key + " is outside MAP19 traversal lock.");
        }

        private static string RequireProjectRoot(string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new ArgumentException("Project root is required.", nameof(projectRoot));
            return Path.GetFullPath(projectRoot);
        }

        private static string Resolve(string projectRoot, string relativePath) =>
            Path.GetFullPath(Path.Combine(RequireProjectRoot(projectRoot),
                relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private static string Value(IReadOnlyDictionary<string, string> row, string field,
            bool required = true)
        {
            if (!row.TryGetValue(field, out var value))
                throw new InvalidDataException("Missing CSV field: " + field);
            if (required && string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("Empty CSV field: " + field);
            return value;
        }

        private static double Number(IReadOnlyDictionary<string, string> row, string field)
        {
            if (!double.TryParse(Value(row, field), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var value))
                throw new InvalidDataException("Invalid number: " + field);
            return value;
        }

        private static int Integer(IReadOnlyDictionary<string, string> row, string field)
        {
            if (!int.TryParse(Value(row, field), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var value))
                throw new InvalidDataException("Invalid integer: " + field);
            return value;
        }

        private static T EnumValue<T>(IReadOnlyDictionary<string, string> row, string field)
            where T : struct
        {
            if (!Enum.TryParse(Value(row, field), false, out T value) ||
                !Enum.IsDefined(typeof(T), value))
                throw new InvalidDataException("Invalid enum token: " + field);
            return value;
        }

        private static string FileSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(stream);
                var text = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes)
                    text.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return text.ToString();
            }
        }
    }

    public sealed class MoonPalacePublishedSample
    {
        public MoonPalacePublishedSample(MoonPalaceProductionProfile profile,
            MoonPalaceTileShell tileShell, MoonPalaceProfileDigestManifest digestManifest,
            MoonPalaceForbiddenOperationCounters counters)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            TileShell = tileShell ?? throw new ArgumentNullException(nameof(tileShell));
            DigestManifest = digestManifest ?? throw new ArgumentNullException(
                nameof(digestManifest));
            Counters = counters ?? throw new ArgumentNullException(nameof(counters));
        }

        public MoonPalaceProductionProfile Profile { get; }
        public MoonPalaceTileShell TileShell { get; }
        public MoonPalaceProfileDigestManifest DigestManifest { get; }
        public MoonPalaceForbiddenOperationCounters Counters { get; }
    }

    public sealed class MoonPalaceForbiddenOperationCounters
    {
        public static MoonPalaceForbiddenOperationCounters Zero =>
            new MoonPalaceForbiddenOperationCounters();

        public int GenerationRunnerExecutions => 0;
        public int ValidationRunnerExecutions => 0;
        public int ReplayExecutions => 0;
        public int RollbackExecutions => 0;
        public int PlayModeSelections => 0;
        public int LegacyRegressionExecutions => 0;
        public int PriorCategoryExecutions => 0;
        public int UnfilteredOrFullRegressionExecutions => 0;
        public int RuntimeObjectSpawns => 0;
        public bool AllZero => GenerationRunnerExecutions == 0 &&
            ValidationRunnerExecutions == 0 && ReplayExecutions == 0 &&
            RollbackExecutions == 0 && PlayModeSelections == 0 &&
            LegacyRegressionExecutions == 0 && PriorCategoryExecutions == 0 &&
            UnfilteredOrFullRegressionExecutions == 0 && RuntimeObjectSpawns == 0;
    }
}
