using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.MoonPalace
{
    public static class MoonPalaceProfilePreconditions
    {
        public const string TaskId = "MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL";
        public const string SourceMap2006ResultDigest =
            "ed73ee40f6f197c9e3d3582acbab7527d25de662a45a8a6eeeb4ae67e52da4bb";
        public const string SourceMap2006TaskDigest =
            "1702533bac5ea69e1bcd687092e2ac6ab02268e1341b5d8a39a0677cd1af3494";
        public const string SourceMap20PhaseExitDigest =
            "552f7f2d8884811977e001c1159be52c218346f21dcc9f96f7df06d169eb7301";
        public const string Map2101HandoffDigest =
            "828b00646fc8ab5936625244471f717e5e25cbf6edf49f8295d60e8b5fec514c";
    }

    public sealed class MoonPalaceMovementProfile
    {
        public MoonPalaceMovementProfile(string profileId, string schemaVersion,
            string sourceMap20PhaseExitDigest, double walkSpeedTilesPerSecond,
            double runSpeedTilesPerSecond, double jumpHeightTiles,
            double jumpDistanceTiles, double maxSafeDropTiles, double playerWidthCells,
            double playerHeightCells, double headClearanceCells,
            double landingClearanceCells, double recoveryTimeSecondsMin,
            double recoveryTimeSecondsMax)
        {
            ProfileId = MoonPalaceProfileValidation.Require(profileId, nameof(profileId));
            SchemaVersion = MoonPalaceProfileValidation.Require(schemaVersion,
                nameof(schemaVersion));
            SourceMap20PhaseExitDigest = MoonPalaceProfileValidation.Digest(
                sourceMap20PhaseExitDigest, nameof(sourceMap20PhaseExitDigest));
            if (!string.Equals(SourceMap20PhaseExitDigest,
                    MoonPalaceProfilePreconditions.SourceMap20PhaseExitDigest,
                    StringComparison.Ordinal))
                throw new ArgumentException("MAP20 phase exit digest mismatch.",
                    nameof(sourceMap20PhaseExitDigest));
            WalkSpeedTilesPerSecond = Positive(walkSpeedTilesPerSecond,
                nameof(walkSpeedTilesPerSecond));
            RunSpeedTilesPerSecond = Positive(runSpeedTilesPerSecond,
                nameof(runSpeedTilesPerSecond));
            JumpHeightTiles = Positive(jumpHeightTiles, nameof(jumpHeightTiles));
            JumpDistanceTiles = Positive(jumpDistanceTiles, nameof(jumpDistanceTiles));
            MaxSafeDropTiles = Positive(maxSafeDropTiles, nameof(maxSafeDropTiles));
            PlayerWidthCells = Positive(playerWidthCells, nameof(playerWidthCells));
            PlayerHeightCells = Positive(playerHeightCells, nameof(playerHeightCells));
            HeadClearanceCells = Positive(headClearanceCells, nameof(headClearanceCells));
            LandingClearanceCells = Positive(landingClearanceCells,
                nameof(landingClearanceCells));
            RecoveryTimeSecondsMin = Positive(recoveryTimeSecondsMin,
                nameof(recoveryTimeSecondsMin));
            RecoveryTimeSecondsMax = Positive(recoveryTimeSecondsMax,
                nameof(recoveryTimeSecondsMax));
            if (RunSpeedTilesPerSecond < WalkSpeedTilesPerSecond)
                throw new ArgumentException("Run speed must be at least walk speed.");
            if (RecoveryTimeSecondsMin > RecoveryTimeSecondsMax)
                throw new ArgumentException("Recovery range must be ordered.");
            TraversalProfileDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MOONPALACE_TRAVERSAL_PROFILE_V1", CanonicalLine,
            });
        }

        public string ProfileId { get; }
        public string SchemaVersion { get; }
        public string SourceMap20PhaseExitDigest { get; }
        public double WalkSpeedTilesPerSecond { get; }
        public double RunSpeedTilesPerSecond { get; }
        public double JumpHeightTiles { get; }
        public double JumpDistanceTiles { get; }
        public double MaxSafeDropTiles { get; }
        public double PlayerWidthCells { get; }
        public double PlayerHeightCells { get; }
        public double HeadClearanceCells { get; }
        public double LandingClearanceCells { get; }
        public double RecoveryTimeSecondsMin { get; }
        public double RecoveryTimeSecondsMax { get; }
        public string TraversalProfileDigest { get; }

        public string CanonicalLine => MoonPalaceCanonical.Join(ProfileId, SchemaVersion,
            SourceMap20PhaseExitDigest, Number(WalkSpeedTilesPerSecond),
            Number(RunSpeedTilesPerSecond), Number(JumpHeightTiles),
            Number(JumpDistanceTiles), Number(MaxSafeDropTiles), Number(PlayerWidthCells),
            Number(PlayerHeightCells), Number(HeadClearanceCells),
            Number(LandingClearanceCells), Number(RecoveryTimeSecondsMin),
            Number(RecoveryTimeSecondsMax));

        private static double Positive(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                throw new ArgumentOutOfRangeException(parameterName);
            return value;
        }

        internal static string Number(double value) => value.ToString(
            "0.################", CultureInfo.InvariantCulture);
    }

    public sealed class MoonPalaceBiomeProfile
    {
        public MoonPalaceBiomeProfile(string biomeId, double densityMin, double densityMax,
            double quietRatioMin, double quietRatioMax, double clusterRatioMin,
            double clusterRatioMax, double activityRatioMin, double activityRatioMax,
            double overlayRatioMin, double overlayRatioMax, string verticalityBand,
            string difficultyBand, string primaryMaterialToken, string ambientAudioToken,
            string backgroundToken)
        {
            BiomeId = MoonPalaceProfileValidation.Require(biomeId, nameof(biomeId));
            DensityMin = Ratio(densityMin, nameof(densityMin));
            DensityMax = Ratio(densityMax, nameof(densityMax));
            QuietRatioMin = Ratio(quietRatioMin, nameof(quietRatioMin));
            QuietRatioMax = Ratio(quietRatioMax, nameof(quietRatioMax));
            ClusterRatioMin = Ratio(clusterRatioMin, nameof(clusterRatioMin));
            ClusterRatioMax = Ratio(clusterRatioMax, nameof(clusterRatioMax));
            ActivityRatioMin = Ratio(activityRatioMin, nameof(activityRatioMin));
            ActivityRatioMax = Ratio(activityRatioMax, nameof(activityRatioMax));
            OverlayRatioMin = Ratio(overlayRatioMin, nameof(overlayRatioMin));
            OverlayRatioMax = Ratio(overlayRatioMax, nameof(overlayRatioMax));
            if (DensityMin > DensityMax || QuietRatioMin > QuietRatioMax ||
                ClusterRatioMin > ClusterRatioMax || ActivityRatioMin > ActivityRatioMax ||
                OverlayRatioMin > OverlayRatioMax)
                throw new ArgumentException("Biome production-anchor ranges must be ordered.");
            VerticalityBand = MoonPalaceProfileValidation.Require(verticalityBand,
                nameof(verticalityBand));
            DifficultyBand = MoonPalaceProfileValidation.Require(difficultyBand,
                nameof(difficultyBand));
            PrimaryMaterialToken = MoonPalaceProfileValidation.Require(primaryMaterialToken,
                nameof(primaryMaterialToken));
            AmbientAudioToken = MoonPalaceProfileValidation.Require(ambientAudioToken,
                nameof(ambientAudioToken));
            BackgroundToken = MoonPalaceProfileValidation.Require(backgroundToken,
                nameof(backgroundToken));
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MOONPALACE_BIOME_PROFILE_V1", CanonicalLine });
        }

        public string BiomeId { get; }
        public double DensityMin { get; }
        public double DensityMax { get; }
        public double QuietRatioMin { get; }
        public double QuietRatioMax { get; }
        public double ClusterRatioMin { get; }
        public double ClusterRatioMax { get; }
        public double ActivityRatioMin { get; }
        public double ActivityRatioMax { get; }
        public double OverlayRatioMin { get; }
        public double OverlayRatioMax { get; }
        public string VerticalityBand { get; }
        public string DifficultyBand { get; }
        public string PrimaryMaterialToken { get; }
        public string AmbientAudioToken { get; }
        public string BackgroundToken { get; }
        public string CanonicalDigest { get; }

        public string CanonicalLine => MoonPalaceCanonical.Join(BiomeId,
            MoonPalaceMovementProfile.Number(DensityMin),
            MoonPalaceMovementProfile.Number(DensityMax),
            MoonPalaceMovementProfile.Number(QuietRatioMin),
            MoonPalaceMovementProfile.Number(QuietRatioMax),
            MoonPalaceMovementProfile.Number(ClusterRatioMin),
            MoonPalaceMovementProfile.Number(ClusterRatioMax),
            MoonPalaceMovementProfile.Number(ActivityRatioMin),
            MoonPalaceMovementProfile.Number(ActivityRatioMax),
            MoonPalaceMovementProfile.Number(OverlayRatioMin),
            MoonPalaceMovementProfile.Number(OverlayRatioMax), VerticalityBand,
            DifficultyBand, PrimaryMaterialToken, AmbientAudioToken, BackgroundToken);

        private static double Ratio(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d || value > 1d)
                throw new ArgumentOutOfRangeException(parameterName);
            return value;
        }
    }

    public sealed class MoonPalaceProductionProfile
    {
        public const string SchemaVersion = "map21_01.moonpalace_production_profile.v1";
        private static readonly string[] RequiredBiomeIds =
            { "MoonCrater", "CassiaRoot", "AbandonedMill", "MoonDough" };
        private readonly ReadOnlyCollection<MoonPalaceBiomeProfile> biomes;

        public MoonPalaceProductionProfile(MoonPalaceMovementProfile movement,
            IEnumerable<MoonPalaceBiomeProfile> sourceBiomes,
            string sourceTraversalProfileDigest, string createdUtc)
        {
            Movement = movement ?? throw new ArgumentNullException(nameof(movement));
            SourceTraversalProfileDigest = MoonPalaceProfileValidation.Digest(
                sourceTraversalProfileDigest, nameof(sourceTraversalProfileDigest));
            var ordered = (sourceBiomes ?? throw new ArgumentNullException(nameof(sourceBiomes)))
                .Where(value => value != null).OrderBy(value => value.BiomeId,
                    StringComparer.Ordinal).ToArray();
            if (ordered.GroupBy(value => value.BiomeId, StringComparer.Ordinal)
                .Any(group => group.Count() != 1))
                throw new ArgumentException("Biome ids must be unique.", nameof(sourceBiomes));
            if (!ordered.Select(value => value.BiomeId).OrderBy(value => value,
                    StringComparer.Ordinal).SequenceEqual(RequiredBiomeIds.OrderBy(value => value,
                    StringComparer.Ordinal), StringComparer.Ordinal))
                throw new ArgumentException("Exactly four MoonPalace biome ids are required.",
                    nameof(sourceBiomes));
            biomes = new ReadOnlyCollection<MoonPalaceBiomeProfile>(ordered);
            BiomeProfileDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MOONPALACE_BIOME_PROFILE_SET_V1" }.Concat(
                biomes.Select(value => value.CanonicalLine)));
            CreatedUtc = createdUtc ?? string.Empty;
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                SchemaVersion, MoonPalaceProfilePreconditions.TaskId,
                Movement.CanonicalLine, Movement.TraversalProfileDigest,
                SourceTraversalProfileDigest, BiomeProfileDigest,
                "created_utc_excluded=true",
            }.Concat(biomes.Select(value => value.CanonicalLine)));
        }

        public MoonPalaceMovementProfile Movement { get; }
        public IReadOnlyList<MoonPalaceBiomeProfile> Biomes => biomes;
        public string SourceTraversalProfileDigest { get; }
        public string BiomeProfileDigest { get; }
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }

        public string Serialize() => MoonPalaceCanonical.ToJson(
            MoonPalaceProductionProfileDocument.From(this));
    }

    public sealed class MoonPalaceProfileDigestManifest
    {
        public const string SchemaVersion = "map21_01.profile_digest_manifest.v1";

        public MoonPalaceProfileDigestManifest(string movementProfileDigest,
            string biomeProfileDigest, string tileShellDigest, string map2102HandoffDigest,
            string createdUtc)
        {
            MovementProfileDigest = MoonPalaceProfileValidation.Digest(movementProfileDigest,
                nameof(movementProfileDigest));
            BiomeProfileDigest = MoonPalaceProfileValidation.Digest(biomeProfileDigest,
                nameof(biomeProfileDigest));
            TileShellDigest = MoonPalaceProfileValidation.Digest(tileShellDigest,
                nameof(tileShellDigest));
            Map2102HandoffDigest = MoonPalaceProfileValidation.Digest(map2102HandoffDigest,
                nameof(map2102HandoffDigest));
            CreatedUtc = createdUtc ?? string.Empty;
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                SchemaVersion, MoonPalaceProfilePreconditions.TaskId,
                MoonPalaceProfilePreconditions.SourceMap2006ResultDigest,
                MoonPalaceProfilePreconditions.SourceMap20PhaseExitDigest,
                MoonPalaceProfilePreconditions.Map2101HandoffDigest,
                MovementProfileDigest, BiomeProfileDigest, TileShellDigest,
                Map2102HandoffDigest, "created_utc_excluded=true",
            });
        }

        public string MovementProfileDigest { get; }
        public string BiomeProfileDigest { get; }
        public string TileShellDigest { get; }
        public string Map2102HandoffDigest { get; }
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }
        public string Serialize() => MoonPalaceCanonical.ToJson(
            MoonPalaceProfileDigestManifestDocument.From(this));
    }

    internal static class MoonPalaceProfileValidation
    {
        public static string Clean(string value) => value == null ? string.Empty : value.Trim();

        public static string Require(string value, string parameterName)
        {
            var result = Clean(value);
            if (result.Length == 0) throw new ArgumentException("Value is required.", parameterName);
            return result;
        }

        public static string Digest(string value, string parameterName)
        {
            if (!BakingCanonicalDigest.IsLowerHexSha256(value))
                throw new ArgumentException("Value must be lower-hex SHA-256.", parameterName);
            return value;
        }
    }

    internal static class MoonPalaceCanonical
    {
        public static string Join(params string[] values) => string.Join("/",
            (values ?? Array.Empty<string>()).Select(value =>
            {
                var text = value ?? string.Empty;
                return text.Length.ToString(CultureInfo.InvariantCulture) + ":" + text;
            }));

        public static string ToJson(object value) =>
            BakingCanonicalDigest.NormalizeLineEndingsToLf(JsonUtility.ToJson(value, true)) + "\n";
    }

    [Serializable]
    internal sealed class MoonPalaceBiomeProfileDocument
    {
        public string biome_id;
        public double density_min;
        public double density_max;
        public double quiet_ratio_min;
        public double quiet_ratio_max;
        public double cluster_ratio_min;
        public double cluster_ratio_max;
        public double activity_ratio_min;
        public double activity_ratio_max;
        public double overlay_ratio_min;
        public double overlay_ratio_max;
        public string verticality_band;
        public string difficulty_band;
        public string primary_material_token;
        public string ambient_audio_token;
        public string background_token;
        public string canonical_digest;

        public static MoonPalaceBiomeProfileDocument From(MoonPalaceBiomeProfile value) =>
            new MoonPalaceBiomeProfileDocument
            {
                biome_id = value.BiomeId,
                density_min = value.DensityMin,
                density_max = value.DensityMax,
                quiet_ratio_min = value.QuietRatioMin,
                quiet_ratio_max = value.QuietRatioMax,
                cluster_ratio_min = value.ClusterRatioMin,
                cluster_ratio_max = value.ClusterRatioMax,
                activity_ratio_min = value.ActivityRatioMin,
                activity_ratio_max = value.ActivityRatioMax,
                overlay_ratio_min = value.OverlayRatioMin,
                overlay_ratio_max = value.OverlayRatioMax,
                verticality_band = value.VerticalityBand,
                difficulty_band = value.DifficultyBand,
                primary_material_token = value.PrimaryMaterialToken,
                ambient_audio_token = value.AmbientAudioToken,
                background_token = value.BackgroundToken,
                canonical_digest = value.CanonicalDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceProductionProfileDocument
    {
        public string profile_id;
        public string schema_version;
        public string task_id;
        public string source_MAP20_phase_exit_digest;
        public double walk_speed_tiles_per_second;
        public double run_speed_tiles_per_second;
        public double jump_height_tiles;
        public double jump_distance_tiles;
        public double max_safe_drop_tiles;
        public double player_width_cells;
        public double player_height_cells;
        public double head_clearance_cells;
        public double landing_clearance_cells;
        public double recovery_time_seconds_min;
        public double recovery_time_seconds_max;
        public string traversal_profile_digest;
        public string source_traversal_profile_digest;
        public MoonPalaceBiomeProfileDocument[] biome_profiles;
        public string biome_profile_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static MoonPalaceProductionProfileDocument From(
            MoonPalaceProductionProfile value) => new MoonPalaceProductionProfileDocument
        {
            profile_id = value.Movement.ProfileId,
            schema_version = MoonPalaceProductionProfile.SchemaVersion,
            task_id = MoonPalaceProfilePreconditions.TaskId,
            source_MAP20_phase_exit_digest = value.Movement.SourceMap20PhaseExitDigest,
            walk_speed_tiles_per_second = value.Movement.WalkSpeedTilesPerSecond,
            run_speed_tiles_per_second = value.Movement.RunSpeedTilesPerSecond,
            jump_height_tiles = value.Movement.JumpHeightTiles,
            jump_distance_tiles = value.Movement.JumpDistanceTiles,
            max_safe_drop_tiles = value.Movement.MaxSafeDropTiles,
            player_width_cells = value.Movement.PlayerWidthCells,
            player_height_cells = value.Movement.PlayerHeightCells,
            head_clearance_cells = value.Movement.HeadClearanceCells,
            landing_clearance_cells = value.Movement.LandingClearanceCells,
            recovery_time_seconds_min = value.Movement.RecoveryTimeSecondsMin,
            recovery_time_seconds_max = value.Movement.RecoveryTimeSecondsMax,
            traversal_profile_digest = value.Movement.TraversalProfileDigest,
            source_traversal_profile_digest = value.SourceTraversalProfileDigest,
            biome_profiles = value.Biomes.Select(MoonPalaceBiomeProfileDocument.From).ToArray(),
            biome_profile_digest = value.BiomeProfileDigest,
            created_utc = value.CreatedUtc,
            created_utc_excluded_from_canonical_digest = true,
            canonical_digest = value.CanonicalDigest,
        };
    }

    [Serializable]
    internal sealed class MoonPalaceProfileDigestManifestDocument
    {
        public string schema_version;
        public string task_id;
        public string source_MAP20_06_result_digest;
        public string source_MAP20_phase_exit_digest;
        public string MAP21_01_handoff_digest;
        public string movement_profile_digest;
        public string biome_profile_digest;
        public string tile_shell_digest;
        public string MAP21_02_handoff_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static MoonPalaceProfileDigestManifestDocument From(
            MoonPalaceProfileDigestManifest value) => new MoonPalaceProfileDigestManifestDocument
        {
            schema_version = MoonPalaceProfileDigestManifest.SchemaVersion,
            task_id = MoonPalaceProfilePreconditions.TaskId,
            source_MAP20_06_result_digest =
                MoonPalaceProfilePreconditions.SourceMap2006ResultDigest,
            source_MAP20_phase_exit_digest =
                MoonPalaceProfilePreconditions.SourceMap20PhaseExitDigest,
            MAP21_01_handoff_digest = MoonPalaceProfilePreconditions.Map2101HandoffDigest,
            movement_profile_digest = value.MovementProfileDigest,
            biome_profile_digest = value.BiomeProfileDigest,
            tile_shell_digest = value.TileShellDigest,
            MAP21_02_handoff_digest = value.Map2102HandoffDigest,
            created_utc = value.CreatedUtc,
            created_utc_excluded_from_canonical_digest = true,
            canonical_digest = value.CanonicalDigest,
        };
    }
}
