using System;
using System.Collections.Generic;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class SectorRecipeDefinition
    {
        internal SectorRecipeDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            SectorRecipeId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "sector_recipe_id");
            DisplayNameKo = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "display_name_ko");
            RouteType = WorldRouteDefinitionValueReader.Int(sourceRecord, 2, "route_type");
            RouteMaskId = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "route_mask_id");
            PrimaryBiomeId = WorldRouteDefinitionValueReader.String(sourceRecord, 4, "primary_biome_id");
            SecondaryBiomeId = WorldRouteDefinitionValueReader.String(sourceRecord, 5, "secondary_biome_id");
            BoundaryProfileId = WorldRouteDefinitionValueReader.String(sourceRecord, 6, "boundary_profile_id");
            RecipeKind = WorldRouteDefinitionValueReader.String(sourceRecord, 7, "recipe_kind");
            MicrochunkBudgetProfileId = WorldRouteDefinitionValueReader.String(sourceRecord, 8, "microchunk_budget_profile_id");
            SelectionWeight = WorldRouteDefinitionValueReader.Int(sourceRecord, 9, "selection_weight");
            SupportsSpecialEntry = WorldRouteDefinitionValueReader.Bool(sourceRecord, 10, "supports_special_entry");
            SupportsVillageEntry = WorldRouteDefinitionValueReader.Bool(sourceRecord, 11, "supports_village_entry");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 12, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 13, "notes");
        }

        public string SectorRecipeId { get; }
        public string DisplayNameKo { get; }
        public int RouteType { get; }
        public string RouteMaskId { get; }
        public string PrimaryBiomeId { get; }
        public string SecondaryBiomeId { get; }
        public string BoundaryProfileId { get; }
        public string RecipeKind { get; }
        public string MicrochunkBudgetProfileId { get; }
        public int SelectionWeight { get; }
        public bool SupportsSpecialEntry { get; }
        public bool SupportsVillageEntry { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class SectorRecipeCellDefinition
    {
        internal SectorRecipeCellDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            SectorRecipeId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "sector_recipe_id");
            ChunkX = WorldRouteDefinitionValueReader.Int(sourceRecord, 1, "chunk_x");
            ChunkY = WorldRouteDefinitionValueReader.Int(sourceRecord, 2, "chunk_y");
            CellRole = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "cell_role");
            FixedMicrochunkId = WorldRouteDefinitionValueReader.String(sourceRecord, 4, "fixed_microchunk_id");
            MicrochunkPoolId = WorldRouteDefinitionValueReader.String(sourceRecord, 5, "microchunk_pool_id");
            RequiredUsageClass = WorldRouteDefinitionValueReader.StringList(sourceRecord, 6, "required_usage_class");
            RequiredRouteRoles = WorldRouteDefinitionValueReader.StringList(sourceRecord, 7, "required_route_roles");
            RequiredBiomeIds = WorldRouteDefinitionValueReader.StringList(sourceRecord, 8, "required_biome_ids");
            RequiredSignatureL = WorldRouteDefinitionValueReader.String(sourceRecord, 9, "required_signature_l");
            RequiredSignatureR = WorldRouteDefinitionValueReader.String(sourceRecord, 10, "required_signature_r");
            RequiredSignatureU = WorldRouteDefinitionValueReader.String(sourceRecord, 11, "required_signature_u");
            RequiredSignatureD = WorldRouteDefinitionValueReader.String(sourceRecord, 12, "required_signature_d");
            TransformPolicy = WorldRouteDefinitionValueReader.StringList(sourceRecord, 13, "transform_policy");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 14, "notes");
        }

        public string SectorRecipeId { get; }
        public int ChunkX { get; }
        public int ChunkY { get; }
        public string CellRole { get; }
        public string FixedMicrochunkId { get; }
        public string MicrochunkPoolId { get; }
        public IReadOnlyList<string> RequiredUsageClass { get; }
        public IReadOnlyList<string> RequiredRouteRoles { get; }
        public IReadOnlyList<string> RequiredBiomeIds { get; }
        public string RequiredSignatureL { get; }
        public string RequiredSignatureR { get; }
        public string RequiredSignatureU { get; }
        public string RequiredSignatureD { get; }
        public IReadOnlyList<string> TransformPolicy { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class SectorRecipePathDefinition
    {
        internal SectorRecipePathDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            SectorRecipeId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "sector_recipe_id");
            PathId = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "path_id");
            PathOrder = WorldRouteDefinitionValueReader.Int(sourceRecord, 2, "path_order");
            ChunkX = WorldRouteDefinitionValueReader.Int(sourceRecord, 3, "chunk_x");
            ChunkY = WorldRouteDefinitionValueReader.Int(sourceRecord, 4, "chunk_y");
            EnterSide = WorldRouteDefinitionValueReader.String(sourceRecord, 5, "enter_side");
            ExitSide = WorldRouteDefinitionValueReader.String(sourceRecord, 6, "exit_side");
            Mandatory = WorldRouteDefinitionValueReader.Bool(sourceRecord, 7, "mandatory");
            TraversalKind = WorldRouteDefinitionValueReader.String(sourceRecord, 8, "traversal_kind");
            MaxJumpTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 9, "max_jump_tiles");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 10, "notes");
        }

        public string SectorRecipeId { get; }
        public string PathId { get; }
        public int PathOrder { get; }
        public int ChunkX { get; }
        public int ChunkY { get; }
        public string EnterSide { get; }
        public string ExitSide { get; }
        public bool Mandatory { get; }
        public string TraversalKind { get; }
        public int MaxJumpTiles { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class SectorExternalSocketDefinition
    {
        internal SectorExternalSocketDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            SectorRecipeId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "sector_recipe_id");
            SocketId = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "socket_id");
            Side = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "side");
            EdgeChunkIndex = WorldRouteDefinitionValueReader.Int(sourceRecord, 3, "edge_chunk_index");
            BandId = WorldRouteDefinitionValueReader.String(sourceRecord, 4, "band_id");
            TraversalKind = WorldRouteDefinitionValueReader.String(sourceRecord, 5, "traversal_kind");
            MandatoryAllowed = WorldRouteDefinitionValueReader.Bool(sourceRecord, 6, "mandatory_allowed");
            EdgeSignatureId = WorldRouteDefinitionValueReader.String(sourceRecord, 7, "edge_signature_id");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 8, "notes");
        }

        public string SectorRecipeId { get; }
        public string SocketId { get; }
        public string Side { get; }
        public int EdgeChunkIndex { get; }
        public string BandId { get; }
        public string TraversalKind { get; }
        public bool MandatoryAllowed { get; }
        public string EdgeSignatureId { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class SectorRecipePoolEntryDefinition
    {
        internal SectorRecipePoolEntryDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            SectorRecipePoolId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "sector_recipe_pool_id");
            EntryOrder = WorldRouteDefinitionValueReader.Int(sourceRecord, 1, "entry_order");
            SectorRecipeId = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "sector_recipe_id");
            Weight = WorldRouteDefinitionValueReader.Int(sourceRecord, 3, "weight");
            MinRepeatDistanceSectors = WorldRouteDefinitionValueReader.Int(sourceRecord, 4, "min_repeat_distance_sectors");
            RequiredPatchRole = WorldRouteDefinitionValueReader.String(sourceRecord, 5, "required_patch_role");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 6, "active");
        }

        public string SectorRecipePoolId { get; }
        public int EntryOrder { get; }
        public string SectorRecipeId { get; }
        public int Weight { get; }
        public int MinRepeatDistanceSectors { get; }
        public string RequiredPatchRole { get; }
        public bool Active { get; }
        public CsvParsedRecord SourceRecord { get; }
    }
}
