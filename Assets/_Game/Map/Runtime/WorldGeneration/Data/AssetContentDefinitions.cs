using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class PrefabRegistryDefinition
    {
        internal PrefabRegistryDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            PrefabId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "prefab_id");
            AssetAddress = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "asset_address");
            ContentType = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "content_type");
            ExpectedComponent = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "expected_component");
            PlaceholderAllowed = WorldRouteDefinitionValueReader.Bool(sourceRecord, 4, "placeholder_allowed");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 5, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 6, "notes");
        }

        public string PrefabId { get; }
        public string AssetAddress { get; }
        public string ContentType { get; }
        public string ExpectedComponent { get; }
        public bool PlaceholderAllowed { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class TileCodeDefinition
    {
        internal TileCodeDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            TileCode = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "tile_code");
            Layer = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "layer");
            Semantic = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "semantic");
            CollisionKind = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "collision_kind");
            Destructible = WorldRouteDefinitionValueReader.Bool(sourceRecord, 4, "destructible");
            TileAssetPrefabId = WorldRouteDefinitionValueReader.String(sourceRecord, 5, "tile_asset_prefab_id");
            RuntimeTag = WorldRouteDefinitionValueReader.String(sourceRecord, 6, "runtime_tag");
            DebugGlyph = WorldRouteDefinitionValueReader.String(sourceRecord, 7, "debug_glyph");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 8, "active");
        }

        public string TileCode { get; }
        public string Layer { get; }
        public string Semantic { get; }
        public string CollisionKind { get; }
        public bool Destructible { get; }
        public string TileAssetPrefabId { get; }
        public string RuntimeTag { get; }
        public string DebugGlyph { get; }
        public bool Active { get; }
        public CsvParsedRecord SourceRecord { get; }
    }
}
