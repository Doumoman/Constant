using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class SpecialVillageDefinitionBuilder
    {
        private static readonly FileContract[] Contracts =
        {
            Contract("event_activation_routes.csv",
                Id("event_route_id"), Id("special_map_id"), Id("event_id"), Bool("mandatory"),
                IntList("allowed_sector_types"), Bool("requires_tool"), Bool("requires_consumable"),
                Int("min_safe_tiles_before_trigger"), Bool("return_path_required"),
                Id("trigger_slot_id"), String("notes")),
            Contract("special_map_catalog.csv",
                Id("special_map_id"), String("display_name_ko"), Enum("site_role"),
                Id("primary_biome_id"), Int("footprint_width_sectors"),
                Int("footprint_height_sectors"), Int("required_count"),
                Int("min_graph_distance_from_start"), Int("min_graph_distance_to_other_core_sites"),
                IntList("allowed_entry_route_types"), Bool("requires_tool"),
                Id("mandatory_reward_id"), Enum("generation_mode"), Bool("active"), String("notes")),
            Contract("special_map_entry_sockets.csv",
                Id("special_map_id"), Id("entry_socket_id"), Int("local_sector_x"),
                Int("local_sector_y"), Enum("side"), IntList("allowed_route_types"),
                Bool("required"), Bool("return_path_required"), String("notes")),
            Contract("special_map_footprint_cells.csv",
                Id("special_map_id"), Int("local_sector_x"), Int("local_sector_y"),
                Enum("local_role"), Id("required_primary_biome_id"), Id("fixed_sector_recipe_id"),
                EnumList("required_open_sides"), String("notes")),
            Contract("special_map_rewards.csv",
                Id("special_map_id"), Int("reward_order"), Id("reward_id"), Enum("reward_kind"),
                Bool("mandatory"), Id("slot_id"), Int("quantity_min"), Int("quantity_max"),
                String("notes")),
            Contract("shop_archetypes.csv",
                Id("shop_archetype_id"), String("display_name_ko"), Enum("shop_type"),
                Int("item_slot_count_min"), Int("item_slot_count_max"),
                Float("base_price_multiplier"), Bool("allows_reputation_reward"),
                Bool("active"), String("notes")),
            Contract("shop_inventory_rules.csv",
                Id("shop_archetype_id"), Int("slot_index"), Id("spawn_pool_id"),
                Bool("guaranteed"), Int("quantity_min"), Int("quantity_max"),
                Int("price_min_gold"), Int("price_max_gold"), Int("required_favor_tier"),
                Bool("active"), String("notes")),
            Contract("shopkeeper_species.csv",
                Id("species_id"), String("display_name_ko"), Id("prefab_id"),
                Id("dialogue_style_id"), Id("animation_set_id"), Int("selection_weight"),
                IdList("allowed_biome_ids"), Bool("active"), String("notes")),
            Contract("village_facilities.csv",
                Id("facility_id"), String("display_name_ko"), Enum("facility_group"),
                Bool("fixed"), Int("selection_weight"), Id("prefab_id"),
                Id("shop_archetype_id"), Id("evacuated_prefab_id"), Bool("active"),
                String("notes")),
            Contract("village_layout_catalog.csv",
                Id("village_layout_id"), String("display_name_ko"),
                Int("footprint_width_sectors"), Int("footprint_height_sectors"),
                Int("target_facility_count"), EnumList("entry_sides"),
                Int("selection_weight"), Bool("active"), String("notes")),
            Contract("village_layout_cells.csv",
                Id("village_layout_id"), Int("local_chunk_x"), Int("local_chunk_y"),
                Enum("cell_role"), Id("facility_slot_id"), Id("fixed_microchunk_id"),
                Id("microchunk_pool_id"), Enum("required_entry_side"), String("notes")),
            Contract("village_profiles.csv",
                Id("village_profile_id"), String("display_name_ko"), Id("world_profile_id"),
                Int("facility_count_min"), Int("facility_count_max"),
                IdList("fixed_facility_ids"), IdList("optional_facility_ids"),
                IdList("allowed_layout_ids"), String("start_distance_buckets"),
                Int("maximum_sector_count"), Bool("active"), String("notes"))
        };

        private static readonly IReadOnlyDictionary<string, FileContract> ContractsByFileName =
            Contracts.ToDictionary(item => item.FileName, item => item, StringComparer.Ordinal);

        public SpecialVillageDefinitionBuildResult Build(
            IEnumerable<SpecialVillageDefinitionSource> sourceDefinitions)
        {
            if (sourceDefinitions == null) throw new ArgumentNullException(nameof(sourceDefinitions));

            var errors = new List<SpecialVillageDefinitionBuildError>();
            var sourcesByFile = CollectSources(sourceDefinitions, errors);
            foreach (var contract in Contracts)
            {
                if (!sourcesByFile.TryGetValue(contract.FileName, out var sources))
                {
                    errors.Add(Error(
                        contract.FileName,
                        SpecialVillageDefinitionBuildErrorCode.MissingSource,
                        "Required definition source is missing."));
                    continue;
                }

                if (sources.Count == 1)
                {
                    ValidateSource(contract, sources[0], errors);
                }
            }

            errors.Sort(SpecialVillageDefinitionBuildError.Compare);
            if (errors.Count > 0)
            {
                return new SpecialVillageDefinitionBuildResult(null, errors);
            }

            try
            {
                return new SpecialVillageDefinitionBuildResult(BuildSet(sourcesByFile), errors);
            }
            catch (Exception exception)
            {
                errors.Add(Error(
                    string.Empty,
                    SpecialVillageDefinitionBuildErrorCode.FieldMappingFailed,
                    "Definition materialization failed: " + exception.Message));
                return new SpecialVillageDefinitionBuildResult(null, errors);
            }
        }

        private static Dictionary<string, List<SpecialVillageDefinitionSource>> CollectSources(
            IEnumerable<SpecialVillageDefinitionSource> sourceDefinitions,
            ICollection<SpecialVillageDefinitionBuildError> errors)
        {
            var sourcesByFile =
                new Dictionary<string, List<SpecialVillageDefinitionSource>>(StringComparer.Ordinal);
            foreach (var source in sourceDefinitions)
            {
                if (source == null)
                {
                    errors.Add(Error(
                        string.Empty,
                        SpecialVillageDefinitionBuildErrorCode.MissingSource,
                        "Definition source cannot be null."));
                    continue;
                }

                if (!ContractsByFileName.ContainsKey(source.FileName))
                {
                    errors.Add(Error(
                        source.FileName,
                        SpecialVillageDefinitionBuildErrorCode.UnexpectedSource,
                        "Definition source filename is not part of the exact 12-source contract."));
                    continue;
                }

                if (!sourcesByFile.TryGetValue(source.FileName, out var matches))
                {
                    matches = new List<SpecialVillageDefinitionSource>();
                    sourcesByFile.Add(source.FileName, matches);
                }

                matches.Add(source);
                if (matches.Count > 1)
                {
                    errors.Add(Error(
                        source.FileName,
                        SpecialVillageDefinitionBuildErrorCode.DuplicateSource,
                        "Definition source filename occurs more than once."));
                }
            }

            return sourcesByFile;
        }

        private static void ValidateSource(
            FileContract contract,
            SpecialVillageDefinitionSource source,
            ICollection<SpecialVillageDefinitionBuildError> errors)
        {
            if (!source.ParseResult.Success || source.ParseResult.Errors.Count != 0)
            {
                errors.Add(Error(
                    source.FileName,
                    SpecialVillageDefinitionBuildErrorCode.UnsuccessfulParse,
                    "Definition source parse result must be successful and contain zero errors."));
                return;
            }

            if (!ValidateSchema(contract, source.Schema, errors)) return;
            foreach (var record in source.ParseResult.Records)
            {
                ValidateRecord(source.Schema, record, errors);
            }
        }

        private static bool ValidateSchema(
            FileContract contract,
            CsvFileSchema schema,
            ICollection<SpecialVillageDefinitionBuildError> errors)
        {
            var valid = true;
            if (!string.Equals(schema.FileName, contract.FileName, StringComparison.Ordinal))
            {
                errors.Add(Error(
                    contract.FileName,
                    SpecialVillageDefinitionBuildErrorCode.SchemaMismatch,
                    "Schema filename does not match the source contract."));
                valid = false;
            }

            if (schema.Columns.Count != contract.Columns.Length)
            {
                errors.Add(Error(
                    contract.FileName,
                    SpecialVillageDefinitionBuildErrorCode.SchemaMismatch,
                    "Schema column count does not match the source contract."));
                valid = false;
            }

            var count = Math.Min(schema.Columns.Count, contract.Columns.Length);
            for (var index = 0; index < count; index++)
            {
                var actual = schema.Columns[index];
                var expected = contract.Columns[index];
                if (actual.ColumnOrder != index + 1 ||
                    !string.Equals(actual.FileName, contract.FileName, StringComparison.Ordinal) ||
                    !string.Equals(actual.ColumnName, expected.ColumnName, StringComparison.Ordinal) ||
                    actual.DataType != expected.DataType)
                {
                    errors.Add(Error(
                        contract.FileName,
                        SpecialVillageDefinitionBuildErrorCode.SchemaMismatch,
                        "Schema column inventory, order, or data type does not match the source contract.",
                        null,
                        index + 1,
                        expected.ColumnName));
                    valid = false;
                }
            }

            return valid;
        }

        private static void ValidateRecord(
            CsvFileSchema schema,
            CsvParsedRecord record,
            ICollection<SpecialVillageDefinitionBuildError> errors)
        {
            if (record == null)
            {
                errors.Add(Error(
                    schema.FileName,
                    SpecialVillageDefinitionBuildErrorCode.FieldMappingFailed,
                    "Parsed record cannot be null."));
                return;
            }

            var validatedRecord = record.ValidatedRecord;
            var sourceRecord = record.SourceRecord;
            if (validatedRecord == null || sourceRecord == null ||
                !ReferenceEquals(sourceRecord, validatedRecord.SourceRecord) ||
                record.RecordNumber != validatedRecord.RecordNumber ||
                record.RecordNumber != sourceRecord.RecordNumber ||
                record.Fields.Count != schema.Columns.Count ||
                validatedRecord.Fields.Count != schema.Columns.Count ||
                sourceRecord.Fields.Count != schema.Columns.Count)
            {
                errors.Add(Error(
                    schema.FileName,
                    SpecialVillageDefinitionBuildErrorCode.FieldMappingFailed,
                    "Parsed record source identity or field inventory is inconsistent.",
                    record.RecordNumber,
                    null,
                    null,
                    sourceRecord?.StartLocation));
                return;
            }

            for (var index = 0; index < schema.Columns.Count; index++)
            {
                var column = schema.Columns[index];
                var parsedField = record.Fields[index];
                var validatedField = validatedRecord.Fields[index];
                var location = sourceRecord.Fields[index].StartLocation;
                if (parsedField == null || parsedField.Value == null ||
                    !ReferenceEquals(parsedField.Schema, column) ||
                    !ReferenceEquals(parsedField.ValidatedField, validatedField) ||
                    !ReferenceEquals(validatedField.Schema, column) ||
                    !ReferenceEquals(validatedField.SourceField, sourceRecord.Fields[index]) ||
                    parsedField.Value.DataType != column.DataType)
                {
                    errors.Add(Error(
                        schema.FileName,
                        SpecialVillageDefinitionBuildErrorCode.FieldMappingFailed,
                        "Parsed field schema/source identity or value type is inconsistent.",
                        record.RecordNumber,
                        column.ColumnOrder,
                        column.ColumnName,
                        location));
                }
            }
        }

        private static SpecialVillageDefinitionSet BuildSet(
            IReadOnlyDictionary<string, List<SpecialVillageDefinitionSource>> sourcesByFile)
        {
            return new SpecialVillageDefinitionSet(
                Records(sourcesByFile, "event_activation_routes.csv").Select(item => new EventActivationRouteDefinition(item)),
                Records(sourcesByFile, "special_map_catalog.csv").Select(item => new SpecialMapDefinition(item)),
                Records(sourcesByFile, "special_map_entry_sockets.csv").Select(item => new SpecialMapEntrySocketDefinition(item)),
                Records(sourcesByFile, "special_map_footprint_cells.csv").Select(item => new SpecialMapFootprintCellDefinition(item)),
                Records(sourcesByFile, "special_map_rewards.csv").Select(item => new SpecialMapRewardDefinition(item)),
                Records(sourcesByFile, "shop_archetypes.csv").Select(item => new ShopArchetypeDefinition(item)),
                Records(sourcesByFile, "shop_inventory_rules.csv").Select(item => new ShopInventoryRuleDefinition(item)),
                Records(sourcesByFile, "shopkeeper_species.csv").Select(item => new ShopkeeperSpeciesDefinition(item)),
                Records(sourcesByFile, "village_facilities.csv").Select(item => new VillageFacilityDefinition(item)),
                Records(sourcesByFile, "village_layout_catalog.csv").Select(item => new VillageLayoutDefinition(item)),
                Records(sourcesByFile, "village_layout_cells.csv").Select(item => new VillageLayoutCellDefinition(item)),
                Records(sourcesByFile, "village_profiles.csv").Select(item => new VillageProfileDefinition(item)));
        }

        private static IReadOnlyList<CsvParsedRecord> Records(
            IReadOnlyDictionary<string, List<SpecialVillageDefinitionSource>> sourcesByFile,
            string fileName)
        {
            return sourcesByFile[fileName][0].ParseResult.Records;
        }

        private static SpecialVillageDefinitionBuildError Error(
            string fileName,
            SpecialVillageDefinitionBuildErrorCode code,
            string message,
            int? recordNumber = null,
            int? columnOrder = null,
            string columnName = null,
            CsvSourceLocation? location = null)
        {
            return new SpecialVillageDefinitionBuildError(
                fileName, code, message, recordNumber, columnOrder, columnName, location);
        }

        private static FileContract Contract(string fileName, params ColumnContract[] columns)
        {
            return new FileContract(fileName, columns);
        }

        private static ColumnContract String(string name) => Column(name, CsvSchemaDataType.String);
        private static ColumnContract Id(string name) => Column(name, CsvSchemaDataType.Id);
        private static ColumnContract Int(string name) => Column(name, CsvSchemaDataType.Int);
        private static ColumnContract Float(string name) => Column(name, CsvSchemaDataType.Float);
        private static ColumnContract Bool(string name) => Column(name, CsvSchemaDataType.Bool);
        private static ColumnContract Enum(string name) => Column(name, CsvSchemaDataType.Enum);
        private static ColumnContract IdList(string name) => Column(name, CsvSchemaDataType.IdList);
        private static ColumnContract EnumList(string name) => Column(name, CsvSchemaDataType.EnumList);
        private static ColumnContract IntList(string name) => Column(name, CsvSchemaDataType.IntList);

        private static ColumnContract Column(string name, CsvSchemaDataType dataType)
        {
            return new ColumnContract(name, dataType);
        }

        private sealed class FileContract
        {
            public FileContract(string fileName, ColumnContract[] columns)
            {
                FileName = fileName;
                Columns = columns;
            }

            public string FileName { get; }
            public ColumnContract[] Columns { get; }
        }

        private sealed class ColumnContract
        {
            public ColumnContract(string columnName, CsvSchemaDataType dataType)
            {
                ColumnName = columnName;
                DataType = dataType;
            }

            public string ColumnName { get; }
            public CsvSchemaDataType DataType { get; }
        }
    }
}
