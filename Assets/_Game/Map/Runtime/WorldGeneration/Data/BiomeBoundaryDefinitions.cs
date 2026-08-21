using System;
using System.Collections.Generic;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class BiomeBoundaryProfileDefinition
    {
        internal BiomeBoundaryProfileDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            BoundaryProfileId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "boundary_profile_id");
            DisplayNameKo = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "display_name_ko");
            BoundaryType = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "boundary_type");
            AllowedOrientations = WorldRouteDefinitionValueReader.StringList(sourceRecord, 3, "allowed_orientations");
            WidthMicrochunksMin = WorldRouteDefinitionValueReader.Int(sourceRecord, 4, "width_microchunks_min");
            WidthMicrochunksMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 5, "width_microchunks_max");
            WarningMicrochunksMin = WorldRouteDefinitionValueReader.Int(sourceRecord, 6, "warning_microchunks_min");
            MandatoryRouteAllowed = WorldRouteDefinitionValueReader.Bool(sourceRecord, 7, "mandatory_route_allowed");
            ToolRequirement = WorldRouteDefinitionValueReader.String(sourceRecord, 8, "tool_requirement");
            HardBorder = WorldRouteDefinitionValueReader.Bool(sourceRecord, 9, "hard_border");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 10, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 11, "notes");
        }

        public string BoundaryProfileId { get; }
        public string DisplayNameKo { get; }
        public string BoundaryType { get; }
        public IReadOnlyList<string> AllowedOrientations { get; }
        public int WidthMicrochunksMin { get; }
        public int WidthMicrochunksMax { get; }
        public int WarningMicrochunksMin { get; }
        public bool MandatoryRouteAllowed { get; }
        public string ToolRequirement { get; }
        public bool HardBorder { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class BiomeBoundaryPairRuleDefinition
    {
        internal BiomeBoundaryPairRuleDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            BoundaryPairRuleId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "boundary_pair_rule_id");
            BiomeAId = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "biome_a_id");
            BiomeBId = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "biome_b_id");
            AllowedBoundaryProfileIds = WorldRouteDefinitionValueReader.StringList(sourceRecord, 3, "allowed_boundary_profile_ids");
            BoundaryProfileWeights = WorldRouteDefinitionValueReader.IntList(sourceRecord, 4, "boundary_profile_weights");
            DefaultBoundaryProfileId = WorldRouteDefinitionValueReader.String(sourceRecord, 5, "default_boundary_profile_id");
            TransitionResourcePoolId = WorldRouteDefinitionValueReader.String(sourceRecord, 6, "transition_resource_pool_id");
            TransitionElementPoolId = WorldRouteDefinitionValueReader.String(sourceRecord, 7, "transition_element_pool_id");
            MinSharedEdgeCount = WorldRouteDefinitionValueReader.Int(sourceRecord, 8, "min_shared_edge_count");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 9, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 10, "notes");
        }

        public string BoundaryPairRuleId { get; }
        public string BiomeAId { get; }
        public string BiomeBId { get; }
        public IReadOnlyList<string> AllowedBoundaryProfileIds { get; }
        public IReadOnlyList<int> BoundaryProfileWeights { get; }
        public string DefaultBoundaryProfileId { get; }
        public string TransitionResourcePoolId { get; }
        public string TransitionElementPoolId { get; }
        public int MinSharedEdgeCount { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class BoundaryChunkDefinition
    {
        internal BoundaryChunkDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            BoundaryChunkId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "boundary_chunk_id");
            MicrochunkId = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "microchunk_id");
            BiomeAId = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "biome_a_id");
            BiomeBId = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "biome_b_id");
            BoundaryProfileId = WorldRouteDefinitionValueReader.String(sourceRecord, 4, "boundary_profile_id");
            Orientation = WorldRouteDefinitionValueReader.String(sourceRecord, 5, "orientation");
            RouteType = WorldRouteDefinitionValueReader.Int(sourceRecord, 6, "route_type");
            EntryEdgeSignatureId = WorldRouteDefinitionValueReader.String(sourceRecord, 7, "entry_edge_signature_id");
            ExitEdgeSignatureId = WorldRouteDefinitionValueReader.String(sourceRecord, 8, "exit_edge_signature_id");
            Weight = WorldRouteDefinitionValueReader.Int(sourceRecord, 9, "weight");
            Reversible = WorldRouteDefinitionValueReader.Bool(sourceRecord, 10, "reversible");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 11, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 12, "notes");
        }

        public string BoundaryChunkId { get; }
        public string MicrochunkId { get; }
        public string BiomeAId { get; }
        public string BiomeBId { get; }
        public string BoundaryProfileId { get; }
        public string Orientation { get; }
        public int RouteType { get; }
        public string EntryEdgeSignatureId { get; }
        public string ExitEdgeSignatureId { get; }
        public int Weight { get; }
        public bool Reversible { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }
}
