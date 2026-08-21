using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class MicrochunkPopulationItemDefinitionSource
    {
        private static readonly IReadOnlyList<string> expectedFileNames =
            new ReadOnlyCollection<string>(new[]
            {
                "battery_profiles.csv",
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
                "spawn_pool_entries.csv",
                "special_item_slots.csv",
                "tile_code_dictionary.csv",
                "tool_upgrade_definitions.csv"
            });

        public MicrochunkPopulationItemDefinitionSource(
            CsvFileSchema schema,
            CsvScalarAndListParseResult parseResult)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            ParseResult = parseResult ?? throw new ArgumentNullException(nameof(parseResult));
        }

        public string FileName => Schema.FileName;
        public static IReadOnlyList<string> ExpectedFileNames => expectedFileNames;
        public CsvFileSchema Schema { get; }
        public CsvScalarAndListParseResult ParseResult { get; }
    }
}
