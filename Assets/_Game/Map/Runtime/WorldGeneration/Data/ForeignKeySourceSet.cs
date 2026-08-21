using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class ForeignKeySourceSet
    {
        private static readonly ReadOnlyCollection<string> expectedFileNames =
            new ReadOnlyCollection<string>(new[]
            {
                "battery_profiles.csv",
                "biome_boundary_pair_rules.csv",
                "biome_boundary_profiles.csv",
                "biome_patch_rules.csv",
                "biome_types.csv",
                "boundary_chunk_catalog.csv",
                "content_budget_profiles.csv",
                "edge_signature_compatibility.csv",
                "edge_signatures.csv",
                "event_activation_routes.csv",
                "generation_passes.csv",
                "generation_profiles.csv",
                "map_element_definitions.csv",
                "map_element_interactions.csv",
                "microchunk_catalog.csv",
                "microchunk_object_slots.csv",
                "microchunk_pool_entries.csv",
                "microchunk_sockets.csv",
                "microchunk_tile_cells.csv",
                "microchunk_variant_rules.csv",
                "population_profiles.csv",
                "prefab_registry.csv",
                "resource_definitions.csv",
                "resource_spawn_rules.csv",
                "rng_streams.csv",
                "sector_external_sockets.csv",
                "sector_recipe_catalog.csv",
                "sector_recipe_cells.csv",
                "sector_recipe_paths.csv",
                "sector_recipe_pool_entries.csv",
                "sector_route_masks.csv",
                "shop_archetypes.csv",
                "shop_inventory_rules.csv",
                "shopkeeper_species.csv",
                "socket_band_definitions.csv",
                "spawn_pool_entries.csv",
                "special_item_slots.csv",
                "special_map_catalog.csv",
                "special_map_entry_sockets.csv",
                "special_map_footprint_cells.csv",
                "special_map_rewards.csv",
                "tile_code_dictionary.csv",
                "tool_upgrade_definitions.csv",
                "validation_rules.csv",
                "village_facilities.csv",
                "village_layout_catalog.csv",
                "village_layout_cells.csv",
                "village_profiles.csv",
                "world_profiles.csv"
            });

        private readonly ReadOnlyCollection<Source> sources;

        public ForeignKeySourceSet(
            CsvSchemaCatalog schemaCatalog,
            IEnumerable<Source> sourceEntries)
        {
            SchemaCatalog = schemaCatalog;
            sources = new ReadOnlyCollection<Source>(
                new List<Source>(sourceEntries ?? Array.Empty<Source>()));
        }

        public static IReadOnlyList<string> ExpectedFileNames => expectedFileNames;

        public CsvSchemaCatalog SchemaCatalog { get; }

        public IReadOnlyList<Source> Sources => sources;

        public sealed class Source
        {
            public Source(
                CsvFileSchema schema,
                CsvScalarAndListParseResult parseResult)
            {
                Schema = schema;
                ParseResult = parseResult;
            }

            public string FileName => Schema?.FileName ?? string.Empty;

            public CsvFileSchema Schema { get; }

            public CsvScalarAndListParseResult ParseResult { get; }
        }
    }
}
