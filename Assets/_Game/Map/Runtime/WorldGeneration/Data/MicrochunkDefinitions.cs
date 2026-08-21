using System;
using System.Collections.Generic;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class MicrochunkDefinition
    {
        internal MicrochunkDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            MicrochunkId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "microchunk_id");
            DisplayNameKo = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "display_name_ko");
            WidthTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 2, "width_tiles");
            HeightTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 3, "height_tiles");
            UsageClass = WorldRouteDefinitionValueReader.String(sourceRecord, 4, "usage_class");
            BiomeIds = WorldRouteDefinitionValueReader.StringList(sourceRecord, 5, "biome_ids");
            RouteRoles = WorldRouteDefinitionValueReader.StringList(sourceRecord, 6, "route_roles");
            AllowedTransforms = WorldRouteDefinitionValueReader.StringList(sourceRecord, 7, "allowed_transforms");
            SelectionWeight = WorldRouteDefinitionValueReader.Int(sourceRecord, 8, "selection_weight");
            Threat = WorldRouteDefinitionValueReader.Int(sourceRecord, 9, "threat");
            Cognitive = WorldRouteDefinitionValueReader.Int(sourceRecord, 10, "cognitive");
            Chain = WorldRouteDefinitionValueReader.Int(sourceRecord, 11, "chain");
            TileDataComplete = WorldRouteDefinitionValueReader.Bool(sourceRecord, 12, "tile_data_complete");
            PrefabId = WorldRouteDefinitionValueReader.String(sourceRecord, 13, "prefab_id");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 14, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 15, "notes");
        }

        public string MicrochunkId { get; }
        public string DisplayNameKo { get; }
        public int WidthTiles { get; }
        public int HeightTiles { get; }
        public string UsageClass { get; }
        public IReadOnlyList<string> BiomeIds { get; }
        public IReadOnlyList<string> RouteRoles { get; }
        public IReadOnlyList<string> AllowedTransforms { get; }
        public int SelectionWeight { get; }
        public int Threat { get; }
        public int Cognitive { get; }
        public int Chain { get; }
        public bool TileDataComplete { get; }
        public string PrefabId { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class MicrochunkObjectSlotDefinition
    {
        internal MicrochunkObjectSlotDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            MicrochunkId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "microchunk_id");
            SlotId = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "slot_id");
            LocalX = WorldRouteDefinitionValueReader.Int(sourceRecord, 2, "local_x");
            LocalY = WorldRouteDefinitionValueReader.Int(sourceRecord, 3, "local_y");
            SlotCategory = WorldRouteDefinitionValueReader.String(sourceRecord, 4, "slot_category");
            AllowedPoolId = WorldRouteDefinitionValueReader.String(sourceRecord, 5, "allowed_pool_id");
            Required = WorldRouteDefinitionValueReader.Bool(sourceRecord, 6, "required");
            Orientation = WorldRouteDefinitionValueReader.String(sourceRecord, 7, "orientation");
            VisibleFromRoute = WorldRouteDefinitionValueReader.Bool(sourceRecord, 8, "visible_from_route");
            ForbiddenRadiusTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 9, "forbidden_radius_tiles");
            RequiredMarkerCode = WorldRouteDefinitionValueReader.String(sourceRecord, 10, "required_marker_code");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 11, "notes");
        }

        public string MicrochunkId { get; }
        public string SlotId { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public string SlotCategory { get; }
        public string AllowedPoolId { get; }
        public bool Required { get; }
        public string Orientation { get; }
        public bool VisibleFromRoute { get; }
        public int ForbiddenRadiusTiles { get; }
        public string RequiredMarkerCode { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class MicrochunkPoolEntryDefinition
    {
        internal MicrochunkPoolEntryDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            MicrochunkPoolId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "microchunk_pool_id");
            EntryOrder = WorldRouteDefinitionValueReader.Int(sourceRecord, 1, "entry_order");
            MicrochunkId = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "microchunk_id");
            Weight = WorldRouteDefinitionValueReader.Int(sourceRecord, 3, "weight");
            RequiredTags = WorldRouteDefinitionValueReader.StringList(sourceRecord, 4, "required_tags");
            ForbiddenTags = WorldRouteDefinitionValueReader.StringList(sourceRecord, 5, "forbidden_tags");
            MinRepeatDistanceChunks = WorldRouteDefinitionValueReader.Int(sourceRecord, 6, "min_repeat_distance_chunks");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 7, "active");
        }

        public string MicrochunkPoolId { get; }
        public int EntryOrder { get; }
        public string MicrochunkId { get; }
        public int Weight { get; }
        public IReadOnlyList<string> RequiredTags { get; }
        public IReadOnlyList<string> ForbiddenTags { get; }
        public int MinRepeatDistanceChunks { get; }
        public bool Active { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class MicrochunkSocketDefinition
    {
        internal MicrochunkSocketDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            MicrochunkId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "microchunk_id");
            SocketId = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "socket_id");
            Side = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "side");
            BandId = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "band_id");
            TraversalKind = WorldRouteDefinitionValueReader.String(sourceRecord, 4, "traversal_kind");
            Direction = WorldRouteDefinitionValueReader.String(sourceRecord, 5, "direction");
            MandatoryAllowed = WorldRouteDefinitionValueReader.Bool(sourceRecord, 6, "mandatory_allowed");
            ToolRequirement = WorldRouteDefinitionValueReader.String(sourceRecord, 7, "tool_requirement");
            EdgeSignatureId = WorldRouteDefinitionValueReader.String(sourceRecord, 8, "edge_signature_id");
            RouteLayer = WorldRouteDefinitionValueReader.String(sourceRecord, 9, "route_layer");
            MinimumSafeTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 10, "minimum_safe_tiles");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 11, "notes");
        }

        public string MicrochunkId { get; }
        public string SocketId { get; }
        public string Side { get; }
        public string BandId { get; }
        public string TraversalKind { get; }
        public string Direction { get; }
        public bool MandatoryAllowed { get; }
        public string ToolRequirement { get; }
        public string EdgeSignatureId { get; }
        public string RouteLayer { get; }
        public int MinimumSafeTiles { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class MicrochunkTileCellDefinition
    {
        internal MicrochunkTileCellDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            MicrochunkId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "microchunk_id");
            LocalX = WorldRouteDefinitionValueReader.Int(sourceRecord, 1, "local_x");
            LocalY = WorldRouteDefinitionValueReader.Int(sourceRecord, 2, "local_y");
            GroundCode = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "ground_code");
            OneWayCode = WorldRouteDefinitionValueReader.String(sourceRecord, 4, "one_way_code");
            BreakableCode = WorldRouteDefinitionValueReader.String(sourceRecord, 5, "breakable_code");
            HazardCode = WorldRouteDefinitionValueReader.String(sourceRecord, 6, "hazard_code");
            LiquidCode = WorldRouteDefinitionValueReader.String(sourceRecord, 7, "liquid_code");
            DecorBackCode = WorldRouteDefinitionValueReader.String(sourceRecord, 8, "decor_back_code");
            DecorFrontCode = WorldRouteDefinitionValueReader.String(sourceRecord, 9, "decor_front_code");
            MarkerCode = WorldRouteDefinitionValueReader.String(sourceRecord, 10, "marker_code");
        }

        public string MicrochunkId { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public string GroundCode { get; }
        public string OneWayCode { get; }
        public string BreakableCode { get; }
        public string HazardCode { get; }
        public string LiquidCode { get; }
        public string DecorBackCode { get; }
        public string DecorFrontCode { get; }
        public string MarkerCode { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class MicrochunkVariantRuleDefinition
    {
        internal MicrochunkVariantRuleDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            VariantRuleId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "variant_rule_id");
            MicrochunkId = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "microchunk_id");
            VariantId = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "variant_id");
            Weight = WorldRouteDefinitionValueReader.Int(sourceRecord, 3, "weight");
            RequiredWorldTags = WorldRouteDefinitionValueReader.StringList(sourceRecord, 4, "required_world_tags");
            ForbiddenWorldTags = WorldRouteDefinitionValueReader.StringList(sourceRecord, 5, "forbidden_world_tags");
            ReplaceSlotPoolPairs = WorldRouteDefinitionValueReader.String(sourceRecord, 6, "replace_slot_pool_pairs");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 7, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 8, "notes");
        }

        public string VariantRuleId { get; }
        public string MicrochunkId { get; }
        public string VariantId { get; }
        public int Weight { get; }
        public IReadOnlyList<string> RequiredWorldTags { get; }
        public IReadOnlyList<string> ForbiddenWorldTags { get; }
        public string ReplaceSlotPoolPairs { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }
}
