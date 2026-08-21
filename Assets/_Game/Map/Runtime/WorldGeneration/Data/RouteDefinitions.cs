using System;
using System.Collections.Generic;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class SectorRouteMaskDefinition
    {
        internal SectorRouteMaskDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            RouteMaskId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "route_mask_id");
            RouteType = WorldRouteDefinitionValueReader.Int(sourceRecord, 1, "route_type");
            OpenL = WorldRouteDefinitionValueReader.Bool(sourceRecord, 2, "open_l");
            OpenR = WorldRouteDefinitionValueReader.Bool(sourceRecord, 3, "open_r");
            OpenU = WorldRouteDefinitionValueReader.Bool(sourceRecord, 4, "open_u");
            OpenD = WorldRouteDefinitionValueReader.Bool(sourceRecord, 5, "open_d");
            MandatoryAllowed = WorldRouteDefinitionValueReader.Bool(sourceRecord, 6, "mandatory_allowed");
            DescriptionKo = WorldRouteDefinitionValueReader.String(sourceRecord, 7, "description_ko");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 8, "active");
        }

        public string RouteMaskId { get; }
        public int RouteType { get; }
        public bool OpenL { get; }
        public bool OpenR { get; }
        public bool OpenU { get; }
        public bool OpenD { get; }
        public bool MandatoryAllowed { get; }
        public string DescriptionKo { get; }
        public bool Active { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class SocketBandDefinition
    {
        internal SocketBandDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            BandId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "band_id");
            Axis = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "axis");
            MinLocalCoord = WorldRouteDefinitionValueReader.Int(sourceRecord, 2, "min_local_coord");
            MaxLocalCoord = WorldRouteDefinitionValueReader.Int(sourceRecord, 3, "max_local_coord");
            RecommendedCenter = WorldRouteDefinitionValueReader.Float(sourceRecord, 4, "recommended_center");
            MinimumClearanceTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 5, "minimum_clearance_tiles");
            DescriptionKo = WorldRouteDefinitionValueReader.String(sourceRecord, 6, "description_ko");
        }

        public string BandId { get; }
        public string Axis { get; }
        public int MinLocalCoord { get; }
        public int MaxLocalCoord { get; }
        public float RecommendedCenter { get; }
        public int MinimumClearanceTiles { get; }
        public string DescriptionKo { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class EdgeSignatureDefinition
    {
        internal EdgeSignatureDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            EdgeSignatureId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "edge_signature_id");
            Axis = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "axis");
            BandId = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "band_id");
            TraversalKind = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "traversal_kind");
            GroundEntryHeight = WorldRouteDefinitionValueReader.Int(sourceRecord, 4, "ground_entry_height");
            ClearanceWidth = WorldRouteDefinitionValueReader.Int(sourceRecord, 5, "clearance_width");
            ClearanceHeight = WorldRouteDefinitionValueReader.Int(sourceRecord, 6, "clearance_height");
            ToolRequirement = WorldRouteDefinitionValueReader.String(sourceRecord, 7, "tool_requirement");
            MandatoryAllowed = WorldRouteDefinitionValueReader.Bool(sourceRecord, 8, "mandatory_allowed");
            Tags = WorldRouteDefinitionValueReader.StringList(sourceRecord, 9, "tags");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 10, "notes");
        }

        public string EdgeSignatureId { get; }
        public string Axis { get; }
        public string BandId { get; }
        public string TraversalKind { get; }
        public int GroundEntryHeight { get; }
        public int ClearanceWidth { get; }
        public int ClearanceHeight { get; }
        public string ToolRequirement { get; }
        public bool MandatoryAllowed { get; }
        public IReadOnlyList<string> Tags { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class EdgeSignatureCompatibilityDefinition
    {
        internal EdgeSignatureCompatibilityDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            SignatureA = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "signature_a");
            SignatureB = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "signature_b");
            Compatible = WorldRouteDefinitionValueReader.Bool(sourceRecord, 2, "compatible");
            AdapterMicrochunkPoolId = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "adapter_microchunk_pool_id");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 4, "notes");
        }

        public string SignatureA { get; }
        public string SignatureB { get; }
        public bool Compatible { get; }
        public string AdapterMicrochunkPoolId { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }
}
