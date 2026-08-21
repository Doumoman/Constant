using System;
using System.Collections.Generic;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class ShopArchetypeDefinition
    {
        internal ShopArchetypeDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            ShopArchetypeId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "shop_archetype_id");
            DisplayNameKo = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "display_name_ko");
            ShopType = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "shop_type");
            ItemSlotCountMin = WorldRouteDefinitionValueReader.Int(sourceRecord, 3, "item_slot_count_min");
            ItemSlotCountMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 4, "item_slot_count_max");
            BasePriceMultiplier = WorldRouteDefinitionValueReader.Float(sourceRecord, 5, "base_price_multiplier");
            AllowsReputationReward = WorldRouteDefinitionValueReader.Bool(sourceRecord, 6, "allows_reputation_reward");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 7, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 8, "notes");
        }

        public string ShopArchetypeId { get; }
        public string DisplayNameKo { get; }
        public string ShopType { get; }
        public int ItemSlotCountMin { get; }
        public int ItemSlotCountMax { get; }
        public float BasePriceMultiplier { get; }
        public bool AllowsReputationReward { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class ShopInventoryRuleDefinition
    {
        internal ShopInventoryRuleDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            ShopArchetypeId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "shop_archetype_id");
            SlotIndex = WorldRouteDefinitionValueReader.Int(sourceRecord, 1, "slot_index");
            SpawnPoolId = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "spawn_pool_id");
            Guaranteed = WorldRouteDefinitionValueReader.Bool(sourceRecord, 3, "guaranteed");
            QuantityMin = WorldRouteDefinitionValueReader.Int(sourceRecord, 4, "quantity_min");
            QuantityMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 5, "quantity_max");
            PriceMinGold = WorldRouteDefinitionValueReader.Int(sourceRecord, 6, "price_min_gold");
            PriceMaxGold = WorldRouteDefinitionValueReader.Int(sourceRecord, 7, "price_max_gold");
            RequiredFavorTier = WorldRouteDefinitionValueReader.Int(sourceRecord, 8, "required_favor_tier");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 9, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 10, "notes");
        }

        public string ShopArchetypeId { get; }
        public int SlotIndex { get; }
        public string SpawnPoolId { get; }
        public bool Guaranteed { get; }
        public int QuantityMin { get; }
        public int QuantityMax { get; }
        public int PriceMinGold { get; }
        public int PriceMaxGold { get; }
        public int RequiredFavorTier { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class ShopkeeperSpeciesDefinition
    {
        internal ShopkeeperSpeciesDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            SpeciesId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "species_id");
            DisplayNameKo = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "display_name_ko");
            PrefabId = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "prefab_id");
            DialogueStyleId = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "dialogue_style_id");
            AnimationSetId = WorldRouteDefinitionValueReader.String(sourceRecord, 4, "animation_set_id");
            SelectionWeight = WorldRouteDefinitionValueReader.Int(sourceRecord, 5, "selection_weight");
            AllowedBiomeIds = WorldRouteDefinitionValueReader.StringList(sourceRecord, 6, "allowed_biome_ids");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 7, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 8, "notes");
        }

        public string SpeciesId { get; }
        public string DisplayNameKo { get; }
        public string PrefabId { get; }
        public string DialogueStyleId { get; }
        public string AnimationSetId { get; }
        public int SelectionWeight { get; }
        public IReadOnlyList<string> AllowedBiomeIds { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class VillageFacilityDefinition
    {
        internal VillageFacilityDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            FacilityId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "facility_id");
            DisplayNameKo = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "display_name_ko");
            FacilityGroup = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "facility_group");
            Fixed = WorldRouteDefinitionValueReader.Bool(sourceRecord, 3, "fixed");
            SelectionWeight = WorldRouteDefinitionValueReader.Int(sourceRecord, 4, "selection_weight");
            PrefabId = WorldRouteDefinitionValueReader.String(sourceRecord, 5, "prefab_id");
            ShopArchetypeId = WorldRouteDefinitionValueReader.String(sourceRecord, 6, "shop_archetype_id");
            EvacuatedPrefabId = WorldRouteDefinitionValueReader.String(sourceRecord, 7, "evacuated_prefab_id");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 8, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 9, "notes");
        }

        public string FacilityId { get; }
        public string DisplayNameKo { get; }
        public string FacilityGroup { get; }
        public bool Fixed { get; }
        public int SelectionWeight { get; }
        public string PrefabId { get; }
        public string ShopArchetypeId { get; }
        public string EvacuatedPrefabId { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class VillageLayoutDefinition
    {
        internal VillageLayoutDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            VillageLayoutId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "village_layout_id");
            DisplayNameKo = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "display_name_ko");
            FootprintWidthSectors = WorldRouteDefinitionValueReader.Int(sourceRecord, 2, "footprint_width_sectors");
            FootprintHeightSectors = WorldRouteDefinitionValueReader.Int(sourceRecord, 3, "footprint_height_sectors");
            TargetFacilityCount = WorldRouteDefinitionValueReader.Int(sourceRecord, 4, "target_facility_count");
            EntrySides = WorldRouteDefinitionValueReader.StringList(sourceRecord, 5, "entry_sides");
            SelectionWeight = WorldRouteDefinitionValueReader.Int(sourceRecord, 6, "selection_weight");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 7, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 8, "notes");
        }

        public string VillageLayoutId { get; }
        public string DisplayNameKo { get; }
        public int FootprintWidthSectors { get; }
        public int FootprintHeightSectors { get; }
        public int TargetFacilityCount { get; }
        public IReadOnlyList<string> EntrySides { get; }
        public int SelectionWeight { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class VillageLayoutCellDefinition
    {
        internal VillageLayoutCellDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            VillageLayoutId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "village_layout_id");
            LocalChunkX = WorldRouteDefinitionValueReader.Int(sourceRecord, 1, "local_chunk_x");
            LocalChunkY = WorldRouteDefinitionValueReader.Int(sourceRecord, 2, "local_chunk_y");
            CellRole = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "cell_role");
            FacilitySlotId = WorldRouteDefinitionValueReader.String(sourceRecord, 4, "facility_slot_id");
            FixedMicrochunkId = WorldRouteDefinitionValueReader.String(sourceRecord, 5, "fixed_microchunk_id");
            MicrochunkPoolId = WorldRouteDefinitionValueReader.String(sourceRecord, 6, "microchunk_pool_id");
            RequiredEntrySide = WorldRouteDefinitionValueReader.String(sourceRecord, 7, "required_entry_side");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 8, "notes");
        }

        public string VillageLayoutId { get; }
        public int LocalChunkX { get; }
        public int LocalChunkY { get; }
        public string CellRole { get; }
        public string FacilitySlotId { get; }
        public string FixedMicrochunkId { get; }
        public string MicrochunkPoolId { get; }
        public string RequiredEntrySide { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class VillageProfileDefinition
    {
        internal VillageProfileDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            VillageProfileId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "village_profile_id");
            DisplayNameKo = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "display_name_ko");
            WorldProfileId = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "world_profile_id");
            FacilityCountMin = WorldRouteDefinitionValueReader.Int(sourceRecord, 3, "facility_count_min");
            FacilityCountMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 4, "facility_count_max");
            FixedFacilityIds = WorldRouteDefinitionValueReader.StringList(sourceRecord, 5, "fixed_facility_ids");
            OptionalFacilityIds = WorldRouteDefinitionValueReader.StringList(sourceRecord, 6, "optional_facility_ids");
            AllowedLayoutIds = WorldRouteDefinitionValueReader.StringList(sourceRecord, 7, "allowed_layout_ids");
            StartDistanceBuckets = WorldRouteDefinitionValueReader.String(sourceRecord, 8, "start_distance_buckets");
            MaximumSectorCount = WorldRouteDefinitionValueReader.Int(sourceRecord, 9, "maximum_sector_count");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 10, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 11, "notes");
        }

        public string VillageProfileId { get; }
        public string DisplayNameKo { get; }
        public string WorldProfileId { get; }
        public int FacilityCountMin { get; }
        public int FacilityCountMax { get; }
        public IReadOnlyList<string> FixedFacilityIds { get; }
        public IReadOnlyList<string> OptionalFacilityIds { get; }
        public IReadOnlyList<string> AllowedLayoutIds { get; }
        public string StartDistanceBuckets { get; }
        public int MaximumSectorCount { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }
}
