using System;
using System.Collections.Generic;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class EventActivationRouteDefinition
    {
        internal EventActivationRouteDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            EventRouteId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "event_route_id");
            SpecialMapId = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "special_map_id");
            EventId = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "event_id");
            Mandatory = WorldRouteDefinitionValueReader.Bool(sourceRecord, 3, "mandatory");
            AllowedSectorTypes = WorldRouteDefinitionValueReader.IntList(sourceRecord, 4, "allowed_sector_types");
            RequiresTool = WorldRouteDefinitionValueReader.Bool(sourceRecord, 5, "requires_tool");
            RequiresConsumable = WorldRouteDefinitionValueReader.Bool(sourceRecord, 6, "requires_consumable");
            MinSafeTilesBeforeTrigger = WorldRouteDefinitionValueReader.Int(sourceRecord, 7, "min_safe_tiles_before_trigger");
            ReturnPathRequired = WorldRouteDefinitionValueReader.Bool(sourceRecord, 8, "return_path_required");
            TriggerSlotId = WorldRouteDefinitionValueReader.String(sourceRecord, 9, "trigger_slot_id");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 10, "notes");
        }

        public string EventRouteId { get; }
        public string SpecialMapId { get; }
        public string EventId { get; }
        public bool Mandatory { get; }
        public IReadOnlyList<int> AllowedSectorTypes { get; }
        public bool RequiresTool { get; }
        public bool RequiresConsumable { get; }
        public int MinSafeTilesBeforeTrigger { get; }
        public bool ReturnPathRequired { get; }
        public string TriggerSlotId { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class SpecialMapDefinition
    {
        internal SpecialMapDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            SpecialMapId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "special_map_id");
            DisplayNameKo = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "display_name_ko");
            SiteRole = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "site_role");
            PrimaryBiomeId = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "primary_biome_id");
            FootprintWidthSectors = WorldRouteDefinitionValueReader.Int(sourceRecord, 4, "footprint_width_sectors");
            FootprintHeightSectors = WorldRouteDefinitionValueReader.Int(sourceRecord, 5, "footprint_height_sectors");
            RequiredCount = WorldRouteDefinitionValueReader.Int(sourceRecord, 6, "required_count");
            MinGraphDistanceFromStart = WorldRouteDefinitionValueReader.Int(sourceRecord, 7, "min_graph_distance_from_start");
            MinGraphDistanceToOtherCoreSites = WorldRouteDefinitionValueReader.Int(sourceRecord, 8, "min_graph_distance_to_other_core_sites");
            AllowedEntryRouteTypes = WorldRouteDefinitionValueReader.IntList(sourceRecord, 9, "allowed_entry_route_types");
            RequiresTool = WorldRouteDefinitionValueReader.Bool(sourceRecord, 10, "requires_tool");
            MandatoryRewardId = WorldRouteDefinitionValueReader.String(sourceRecord, 11, "mandatory_reward_id");
            GenerationMode = WorldRouteDefinitionValueReader.String(sourceRecord, 12, "generation_mode");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 13, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 14, "notes");
        }

        public string SpecialMapId { get; }
        public string DisplayNameKo { get; }
        public string SiteRole { get; }
        public string PrimaryBiomeId { get; }
        public int FootprintWidthSectors { get; }
        public int FootprintHeightSectors { get; }
        public int RequiredCount { get; }
        public int MinGraphDistanceFromStart { get; }
        public int MinGraphDistanceToOtherCoreSites { get; }
        public IReadOnlyList<int> AllowedEntryRouteTypes { get; }
        public bool RequiresTool { get; }
        public string MandatoryRewardId { get; }
        public string GenerationMode { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class SpecialMapEntrySocketDefinition
    {
        internal SpecialMapEntrySocketDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            SpecialMapId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "special_map_id");
            EntrySocketId = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "entry_socket_id");
            LocalSectorX = WorldRouteDefinitionValueReader.Int(sourceRecord, 2, "local_sector_x");
            LocalSectorY = WorldRouteDefinitionValueReader.Int(sourceRecord, 3, "local_sector_y");
            Side = WorldRouteDefinitionValueReader.String(sourceRecord, 4, "side");
            AllowedRouteTypes = WorldRouteDefinitionValueReader.IntList(sourceRecord, 5, "allowed_route_types");
            Required = WorldRouteDefinitionValueReader.Bool(sourceRecord, 6, "required");
            ReturnPathRequired = WorldRouteDefinitionValueReader.Bool(sourceRecord, 7, "return_path_required");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 8, "notes");
        }

        public string SpecialMapId { get; }
        public string EntrySocketId { get; }
        public int LocalSectorX { get; }
        public int LocalSectorY { get; }
        public string Side { get; }
        public IReadOnlyList<int> AllowedRouteTypes { get; }
        public bool Required { get; }
        public bool ReturnPathRequired { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class SpecialMapFootprintCellDefinition
    {
        internal SpecialMapFootprintCellDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            SpecialMapId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "special_map_id");
            LocalSectorX = WorldRouteDefinitionValueReader.Int(sourceRecord, 1, "local_sector_x");
            LocalSectorY = WorldRouteDefinitionValueReader.Int(sourceRecord, 2, "local_sector_y");
            LocalRole = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "local_role");
            RequiredPrimaryBiomeId = WorldRouteDefinitionValueReader.String(sourceRecord, 4, "required_primary_biome_id");
            FixedSectorRecipeId = WorldRouteDefinitionValueReader.String(sourceRecord, 5, "fixed_sector_recipe_id");
            RequiredOpenSides = WorldRouteDefinitionValueReader.StringList(sourceRecord, 6, "required_open_sides");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 7, "notes");
        }

        public string SpecialMapId { get; }
        public int LocalSectorX { get; }
        public int LocalSectorY { get; }
        public string LocalRole { get; }
        public string RequiredPrimaryBiomeId { get; }
        public string FixedSectorRecipeId { get; }
        public IReadOnlyList<string> RequiredOpenSides { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class SpecialMapRewardDefinition
    {
        internal SpecialMapRewardDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            SpecialMapId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "special_map_id");
            RewardOrder = WorldRouteDefinitionValueReader.Int(sourceRecord, 1, "reward_order");
            RewardId = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "reward_id");
            RewardKind = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "reward_kind");
            Mandatory = WorldRouteDefinitionValueReader.Bool(sourceRecord, 4, "mandatory");
            SlotId = WorldRouteDefinitionValueReader.String(sourceRecord, 5, "slot_id");
            QuantityMin = WorldRouteDefinitionValueReader.Int(sourceRecord, 6, "quantity_min");
            QuantityMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 7, "quantity_max");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 8, "notes");
        }

        public string SpecialMapId { get; }
        public int RewardOrder { get; }
        public string RewardId { get; }
        public string RewardKind { get; }
        public bool Mandatory { get; }
        public string SlotId { get; }
        public int QuantityMin { get; }
        public int QuantityMax { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }
}
