using System;
using System.Collections.Generic;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class BatteryProfileDefinition
    {
        internal BatteryProfileDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            BatteryId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "battery_id");
            DisplayNameKo = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "display_name_ko");
            FuelCost = WorldRouteDefinitionValueReader.Int(sourceRecord, 2, "fuel_cost");
            BatteryItemCost = WorldRouteDefinitionValueReader.Int(sourceRecord, 3, "battery_item_cost");
            DeliveryMode = WorldRouteDefinitionValueReader.String(sourceRecord, 4, "delivery_mode");
            BlastRadiusTiles = WorldRouteDefinitionValueReader.Float(sourceRecord, 5, "blast_radius_tiles");
            Damage = WorldRouteDefinitionValueReader.Int(sourceRecord, 6, "damage");
            Knockback = WorldRouteDefinitionValueReader.Float(sourceRecord, 7, "knockback");
            DestroysSoftSoil = WorldRouteDefinitionValueReader.Bool(sourceRecord, 8, "destroys_soft_soil");
            DestroysCrackedTerrain = WorldRouteDefinitionValueReader.Bool(sourceRecord, 9, "destroys_cracked_terrain");
            DestroysHardTerrain = WorldRouteDefinitionValueReader.Bool(sourceRecord, 10, "destroys_hard_terrain");
            DestroysStarstone = WorldRouteDefinitionValueReader.Bool(sourceRecord, 11, "destroys_starstone");
            TerrainDamageEnabled = WorldRouteDefinitionValueReader.Bool(sourceRecord, 12, "terrain_damage_enabled");
            FuseSeconds = WorldRouteDefinitionValueReader.Float(sourceRecord, 13, "fuse_seconds");
            PrefabId = WorldRouteDefinitionValueReader.String(sourceRecord, 14, "prefab_id");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 15, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 16, "notes");
        }

        public string BatteryId { get; }
        public string DisplayNameKo { get; }
        public int FuelCost { get; }
        public int BatteryItemCost { get; }
        public string DeliveryMode { get; }
        public float BlastRadiusTiles { get; }
        public int Damage { get; }
        public float Knockback { get; }
        public bool DestroysSoftSoil { get; }
        public bool DestroysCrackedTerrain { get; }
        public bool DestroysHardTerrain { get; }
        public bool DestroysStarstone { get; }
        public bool TerrainDamageEnabled { get; }
        public float FuseSeconds { get; }
        public string PrefabId { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class ResourceDefinition
    {
        internal ResourceDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            ResourceId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "resource_id");
            DisplayNameKo = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "display_name_ko");
            ResourceCategory = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "resource_category");
            HudDestination = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "hud_destination");
            UniquePerWorld = WorldRouteDefinitionValueReader.Bool(sourceRecord, 4, "unique_per_world");
            MaxQuantity = WorldRouteDefinitionValueReader.Int(sourceRecord, 5, "max_quantity");
            PickupPrefabId = WorldRouteDefinitionValueReader.String(sourceRecord, 6, "pickup_prefab_id");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 7, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 8, "notes");
        }

        public string ResourceId { get; }
        public string DisplayNameKo { get; }
        public string ResourceCategory { get; }
        public string HudDestination { get; }
        public bool UniquePerWorld { get; }
        public int MaxQuantity { get; }
        public string PickupPrefabId { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class ResourceSpawnRuleDefinition
    {
        internal ResourceSpawnRuleDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            SpawnRuleId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "spawn_rule_id");
            ResourceId = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "resource_id");
            BiomeIds = WorldRouteDefinitionValueReader.StringList(sourceRecord, 2, "biome_ids");
            PatchRoles = WorldRouteDefinitionValueReader.StringList(sourceRecord, 3, "patch_roles");
            SectorRouteTypes = WorldRouteDefinitionValueReader.IntList(sourceRecord, 4, "sector_route_types");
            AllowedSlotPoolIds = WorldRouteDefinitionValueReader.StringList(sourceRecord, 5, "allowed_slot_pool_ids");
            WorldMin = WorldRouteDefinitionValueReader.Int(sourceRecord, 6, "world_min");
            WorldMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 7, "world_max");
            PatchMin = WorldRouteDefinitionValueReader.Int(sourceRecord, 8, "patch_min");
            PatchMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 9, "patch_max");
            SpawnWeight = WorldRouteDefinitionValueReader.Int(sourceRecord, 10, "spawn_weight");
            MinDistanceFromSameResourceTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 11, "min_distance_from_same_resource_tiles");
            MandatorySiteId = WorldRouteDefinitionValueReader.String(sourceRecord, 12, "mandatory_site_id");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 13, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 14, "notes");
        }

        public string SpawnRuleId { get; }
        public string ResourceId { get; }
        public IReadOnlyList<string> BiomeIds { get; }
        public IReadOnlyList<string> PatchRoles { get; }
        public IReadOnlyList<int> SectorRouteTypes { get; }
        public IReadOnlyList<string> AllowedSlotPoolIds { get; }
        public int WorldMin { get; }
        public int WorldMax { get; }
        public int PatchMin { get; }
        public int PatchMax { get; }
        public int SpawnWeight { get; }
        public int MinDistanceFromSameResourceTiles { get; }
        public string MandatorySiteId { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class SpecialItemSlotDefinition
    {
        internal SpecialItemSlotDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            SpecialItemSlotId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "special_item_slot_id");
            DisplayNameKo = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "display_name_ko");
            UnknownSpritePrefabId = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "unknown_sprite_prefab_id");
            RevealedSpritePrefabId = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "revealed_sprite_prefab_id");
            StartsRevealed = WorldRouteDefinitionValueReader.Bool(sourceRecord, 4, "starts_revealed");
            MaximumPerWorld = WorldRouteDefinitionValueReader.Int(sourceRecord, 5, "maximum_per_world");
            EffectId = WorldRouteDefinitionValueReader.String(sourceRecord, 6, "effect_id");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 7, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 8, "notes");
        }

        public string SpecialItemSlotId { get; }
        public string DisplayNameKo { get; }
        public string UnknownSpritePrefabId { get; }
        public string RevealedSpritePrefabId { get; }
        public bool StartsRevealed { get; }
        public int MaximumPerWorld { get; }
        public string EffectId { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class ToolUpgradeDefinition
    {
        internal ToolUpgradeDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            ToolId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "tool_id");
            UpgradeLevel = WorldRouteDefinitionValueReader.Int(sourceRecord, 1, "upgrade_level");
            RequiredBlueprintFragments = WorldRouteDefinitionValueReader.Int(sourceRecord, 2, "required_blueprint_fragments");
            GoldCost = WorldRouteDefinitionValueReader.Int(sourceRecord, 3, "gold_cost");
            MaxDurabilityMultiplier = WorldRouteDefinitionValueReader.Float(sourceRecord, 4, "max_durability_multiplier");
            WorkSpeedMultiplier = WorldRouteDefinitionValueReader.Float(sourceRecord, 5, "work_speed_multiplier");
            SpecialEffectId = WorldRouteDefinitionValueReader.String(sourceRecord, 6, "special_effect_id");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 7, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 8, "notes");
        }

        public string ToolId { get; }
        public int UpgradeLevel { get; }
        public int RequiredBlueprintFragments { get; }
        public int GoldCost { get; }
        public float MaxDurabilityMultiplier { get; }
        public float WorkSpeedMultiplier { get; }
        public string SpecialEffectId { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }
}
