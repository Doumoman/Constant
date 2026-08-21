using System;
using System.Collections.Generic;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class MapElementDefinition
    {
        internal MapElementDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            MapElementId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "map_element_id");
            DisplayNameKo = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "display_name_ko");
            Category = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "category");
            PrefabId = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "prefab_id");
            FootprintWidthTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 4, "footprint_width_tiles");
            FootprintHeightTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 5, "footprint_height_tiles");
            Threat = WorldRouteDefinitionValueReader.Int(sourceRecord, 6, "threat");
            Utility = WorldRouteDefinitionValueReader.Int(sourceRecord, 7, "utility");
            Cognitive = WorldRouteDefinitionValueReader.Int(sourceRecord, 8, "cognitive");
            Chain = WorldRouteDefinitionValueReader.Int(sourceRecord, 9, "chain");
            TelegraphSeconds = WorldRouteDefinitionValueReader.Float(sourceRecord, 10, "telegraph_seconds");
            InteractionTags = WorldRouteDefinitionValueReader.StringList(sourceRecord, 11, "interaction_tags");
            ForbiddenNearTags = WorldRouteDefinitionValueReader.StringList(sourceRecord, 12, "forbidden_near_tags");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 13, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 14, "notes");
        }

        public string MapElementId { get; }
        public string DisplayNameKo { get; }
        public string Category { get; }
        public string PrefabId { get; }
        public int FootprintWidthTiles { get; }
        public int FootprintHeightTiles { get; }
        public int Threat { get; }
        public int Utility { get; }
        public int Cognitive { get; }
        public int Chain { get; }
        public float TelegraphSeconds { get; }
        public IReadOnlyList<string> InteractionTags { get; }
        public IReadOnlyList<string> ForbiddenNearTags { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class MapElementInteractionDefinition
    {
        internal MapElementInteractionDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            SourceElementOrToolId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "source_element_or_tool_id");
            TargetTag = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "target_tag");
            InteractionResult = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "interaction_result");
            Magnitude = WorldRouteDefinitionValueReader.Float(sourceRecord, 3, "magnitude");
            ConsumesSource = WorldRouteDefinitionValueReader.Bool(sourceRecord, 4, "consumes_source");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 5, "notes");
        }

        public string SourceElementOrToolId { get; }
        public string TargetTag { get; }
        public string InteractionResult { get; }
        public float Magnitude { get; }
        public bool ConsumesSource { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class PopulationProfileDefinition
    {
        internal PopulationProfileDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            PopulationProfileId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "population_profile_id");
            BiomeId = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "biome_id");
            SectorRole = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "sector_role");
            ResourcePoolIds = WorldRouteDefinitionValueReader.StringList(sourceRecord, 3, "resource_pool_ids");
            ElementPoolIds = WorldRouteDefinitionValueReader.StringList(sourceRecord, 4, "element_pool_ids");
            EnemyPoolIds = WorldRouteDefinitionValueReader.StringList(sourceRecord, 5, "enemy_pool_ids");
            RewardPoolIds = WorldRouteDefinitionValueReader.StringList(sourceRecord, 6, "reward_pool_ids");
            BudgetProfileId = WorldRouteDefinitionValueReader.String(sourceRecord, 7, "budget_profile_id");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 8, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 9, "notes");
        }

        public string PopulationProfileId { get; }
        public string BiomeId { get; }
        public string SectorRole { get; }
        public IReadOnlyList<string> ResourcePoolIds { get; }
        public IReadOnlyList<string> ElementPoolIds { get; }
        public IReadOnlyList<string> EnemyPoolIds { get; }
        public IReadOnlyList<string> RewardPoolIds { get; }
        public string BudgetProfileId { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class SpawnPoolEntryDefinition
    {
        internal SpawnPoolEntryDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            SpawnPoolId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "spawn_pool_id");
            EntryOrder = WorldRouteDefinitionValueReader.Int(sourceRecord, 1, "entry_order");
            EntryKind = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "entry_kind");
            EntryId = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "entry_id");
            Weight = WorldRouteDefinitionValueReader.Int(sourceRecord, 4, "weight");
            QuantityMin = WorldRouteDefinitionValueReader.Int(sourceRecord, 5, "quantity_min");
            QuantityMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 6, "quantity_max");
            RequiredTags = WorldRouteDefinitionValueReader.StringList(sourceRecord, 7, "required_tags");
            ForbiddenTags = WorldRouteDefinitionValueReader.StringList(sourceRecord, 8, "forbidden_tags");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 9, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 10, "notes");
        }

        public string SpawnPoolId { get; }
        public int EntryOrder { get; }
        public string EntryKind { get; }
        public string EntryId { get; }
        public int Weight { get; }
        public int QuantityMin { get; }
        public int QuantityMax { get; }
        public IReadOnlyList<string> RequiredTags { get; }
        public IReadOnlyList<string> ForbiddenTags { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }
}
