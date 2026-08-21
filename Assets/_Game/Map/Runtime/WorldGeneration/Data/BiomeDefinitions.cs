using System;
using System.Collections.Generic;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class BiomeTypeDefinition
    {
        internal BiomeTypeDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            BiomeId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "biome_id");
            DisplayNameKo = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "display_name_ko");
            StageId = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "stage_id");
            Required = WorldRouteDefinitionValueReader.Bool(sourceRecord, 3, "required");
            MinPatchCount = WorldRouteDefinitionValueReader.Int(sourceRecord, 4, "min_patch_count");
            MaxPatchCount = WorldRouteDefinitionValueReader.Int(sourceRecord, 5, "max_patch_count");
            MinCorePatchCount = WorldRouteDefinitionValueReader.Int(sourceRecord, 6, "min_core_patch_count");
            PreferredAltitudeMinSectorY = WorldRouteDefinitionValueReader.Int(sourceRecord, 7, "preferred_altitude_min_sector_y");
            PreferredAltitudeMaxSectorY = WorldRouteDefinitionValueReader.Int(sourceRecord, 8, "preferred_altitude_max_sector_y");
            GrowthWeight = WorldRouteDefinitionValueReader.Float(sourceRecord, 9, "growth_weight");
            TileThemeId = WorldRouteDefinitionValueReader.String(sourceRecord, 10, "tile_theme_id");
            AudioProfileId = WorldRouteDefinitionValueReader.String(sourceRecord, 11, "audio_profile_id");
            MicrochunkPoolPrefix = WorldRouteDefinitionValueReader.String(sourceRecord, 12, "microchunk_pool_prefix");
            SectorRecipePoolPrefix = WorldRouteDefinitionValueReader.String(sourceRecord, 13, "sector_recipe_pool_prefix");
            CommonResourcePoolId = WorldRouteDefinitionValueReader.String(sourceRecord, 14, "common_resource_pool_id");
            MapElementPoolId = WorldRouteDefinitionValueReader.String(sourceRecord, 15, "map_element_pool_id");
            RequiredSpecialMapIds = WorldRouteDefinitionValueReader.StringList(sourceRecord, 16, "required_special_map_ids");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 17, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 18, "notes");
        }

        public string BiomeId { get; }
        public string DisplayNameKo { get; }
        public string StageId { get; }
        public bool Required { get; }
        public int MinPatchCount { get; }
        public int MaxPatchCount { get; }
        public int MinCorePatchCount { get; }
        public int PreferredAltitudeMinSectorY { get; }
        public int PreferredAltitudeMaxSectorY { get; }
        public float GrowthWeight { get; }
        public string TileThemeId { get; }
        public string AudioProfileId { get; }
        public string MicrochunkPoolPrefix { get; }
        public string SectorRecipePoolPrefix { get; }
        public string CommonResourcePoolId { get; }
        public string MapElementPoolId { get; }
        public IReadOnlyList<string> RequiredSpecialMapIds { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class BiomePatchRuleDefinition
    {
        internal BiomePatchRuleDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            PatchRuleId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "patch_rule_id");
            BiomeId = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "biome_id");
            PatchRole = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "patch_role");
            MinSectorCount = WorldRouteDefinitionValueReader.Int(sourceRecord, 3, "min_sector_count");
            MaxSectorCount = WorldRouteDefinitionValueReader.Int(sourceRecord, 4, "max_sector_count");
            MinSeedDistance = WorldRouteDefinitionValueReader.Int(sourceRecord, 5, "min_seed_distance");
            SeedCountMin = WorldRouteDefinitionValueReader.Int(sourceRecord, 6, "seed_count_min");
            SeedCountMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 7, "seed_count_max");
            SeedWeight = WorldRouteDefinitionValueReader.Float(sourceRecord, 8, "seed_weight");
            CanTouchWorldEdge = WorldRouteDefinitionValueReader.Bool(sourceRecord, 9, "can_touch_world_edge");
            BufferRingSectors = WorldRouteDefinitionValueReader.Int(sourceRecord, 10, "buffer_ring_sectors");
            AllowSingleSector = WorldRouteDefinitionValueReader.Bool(sourceRecord, 11, "allow_single_sector");
            MaxWorldShare = WorldRouteDefinitionValueReader.Float(sourceRecord, 12, "max_world_share");
            DistanceWeight = WorldRouteDefinitionValueReader.Float(sourceRecord, 13, "distance_weight");
            AltitudeWeight = WorldRouteDefinitionValueReader.Float(sourceRecord, 14, "altitude_weight");
            NoiseWeight = WorldRouteDefinitionValueReader.Float(sourceRecord, 15, "noise_weight");
            CompactnessWeight = WorldRouteDefinitionValueReader.Float(sourceRecord, 16, "compactness_weight");
            BranchinessTarget = WorldRouteDefinitionValueReader.Float(sourceRecord, 17, "branchiness_target");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 18, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 19, "notes");
        }

        public string PatchRuleId { get; }
        public string BiomeId { get; }
        public string PatchRole { get; }
        public int MinSectorCount { get; }
        public int MaxSectorCount { get; }
        public int MinSeedDistance { get; }
        public int SeedCountMin { get; }
        public int SeedCountMax { get; }
        public float SeedWeight { get; }
        public bool CanTouchWorldEdge { get; }
        public int BufferRingSectors { get; }
        public bool AllowSingleSector { get; }
        public float MaxWorldShare { get; }
        public float DistanceWeight { get; }
        public float AltitudeWeight { get; }
        public float NoiseWeight { get; }
        public float CompactnessWeight { get; }
        public float BranchinessTarget { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }
}
