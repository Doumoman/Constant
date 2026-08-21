using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class MicrochunkPopulationItemDefinitionBuilder
    {
        private static readonly FileContract[] Contracts =
        {
            Contract("battery_profiles.csv",
                Exact("battery_id", CsvSchemaDataType.Id, true, 1),
                Exact("display_name_ko", CsvSchemaDataType.String, true),
                Exact("fuel_cost", CsvSchemaDataType.Int, true),
                Exact("battery_item_cost", CsvSchemaDataType.Int, true),
                Exact("delivery_mode", CsvSchemaDataType.Enum, true,
                    allowedValues: new[] { "PLACE", "THROW", "BLAST_CONE" }),
                Exact("blast_radius_tiles", CsvSchemaDataType.Float, true),
                Exact("damage", CsvSchemaDataType.Int, true),
                Exact("knockback", CsvSchemaDataType.Float, true),
                Exact("destroys_soft_soil", CsvSchemaDataType.Bool, true),
                Exact("destroys_cracked_terrain", CsvSchemaDataType.Bool, true),
                Exact("destroys_hard_terrain", CsvSchemaDataType.Bool, true),
                Exact("destroys_starstone", CsvSchemaDataType.Bool, true),
                Exact("terrain_damage_enabled", CsvSchemaDataType.Bool, true),
                Exact("fuse_seconds", CsvSchemaDataType.Float, true),
                Exact("prefab_id", CsvSchemaDataType.Id, true,
                    foreignKeyFileName: "prefab_registry.csv", foreignKeyColumnName: "prefab_id"),
                Exact("active", CsvSchemaDataType.Bool, true, defaultValue: "1"),
                Exact("notes", CsvSchemaDataType.String, false)),
            Contract("map_element_definitions.csv",
                Id("map_element_id"), String("display_name_ko"), Enum("category"), Id("prefab_id"),
                Int("footprint_width_tiles"), Int("footprint_height_tiles"), Int("threat"),
                Int("utility"), Int("cognitive"), Int("chain"), Float("telegraph_seconds"),
                IdList("interaction_tags"), IdList("forbidden_near_tags"), Bool("active"), String("notes")),
            Contract("map_element_interactions.csv",
                Id("source_element_or_tool_id"), Id("target_tag"), Enum("interaction_result"),
                Float("magnitude"), Bool("consumes_source"), String("notes")),
            Contract("microchunk_catalog.csv",
                Id("microchunk_id"), String("display_name_ko"), Int("width_tiles"), Int("height_tiles"),
                Enum("usage_class"), IdList("biome_ids"), IdList("route_roles"),
                EnumList("allowed_transforms"), Int("selection_weight"), Int("threat"),
                Int("cognitive"), Int("chain"), Bool("tile_data_complete"), Id("prefab_id"),
                Bool("active"), String("notes")),
            Contract("microchunk_object_slots.csv",
                Id("microchunk_id"), Id("slot_id"), Int("local_x"), Int("local_y"),
                Enum("slot_category"), Id("allowed_pool_id"), Bool("required"), Enum("orientation"),
                Bool("visible_from_route"), Int("forbidden_radius_tiles"),
                Id("required_marker_code"), String("notes")),
            Contract("microchunk_pool_entries.csv",
                Id("microchunk_pool_id"), Int("entry_order"), Id("microchunk_id"), Int("weight"),
                IdList("required_tags"), IdList("forbidden_tags"),
                Int("min_repeat_distance_chunks"), Bool("active")),
            Contract("microchunk_sockets.csv",
                Id("microchunk_id"), Id("socket_id"), Enum("side"), Id("band_id"),
                Enum("traversal_kind"), Enum("direction"), Bool("mandatory_allowed"),
                Enum("tool_requirement"), Id("edge_signature_id"), Enum("route_layer"),
                Int("minimum_safe_tiles"), String("notes")),
            Contract("microchunk_tile_cells.csv",
                Id("microchunk_id"), Int("local_x"), Int("local_y"), Id("ground_code"),
                Id("one_way_code"), Id("breakable_code"), Id("hazard_code"), Id("liquid_code"),
                Id("decor_back_code"), Id("decor_front_code"), Id("marker_code")),
            Contract("microchunk_variant_rules.csv",
                Id("variant_rule_id"), Id("microchunk_id"), Id("variant_id"), Int("weight"),
                IdList("required_world_tags"), IdList("forbidden_world_tags"),
                String("replace_slot_pool_pairs"), Bool("active"), String("notes")),
            Contract("population_profiles.csv",
                Id("population_profile_id"), Id("biome_id"), Enum("sector_role"),
                IdList("resource_pool_ids"), IdList("element_pool_ids"), IdList("enemy_pool_ids"),
                IdList("reward_pool_ids"), Id("budget_profile_id"), Bool("active"), String("notes")),
            Contract("prefab_registry.csv",
                Id("prefab_id"), String("asset_address"), Enum("content_type"),
                String("expected_component"), Bool("placeholder_allowed"), Bool("active"), String("notes")),
            Contract("resource_definitions.csv",
                Id("resource_id"), String("display_name_ko"), Enum("resource_category"),
                Enum("hud_destination"), Bool("unique_per_world"), Int("max_quantity"),
                Id("pickup_prefab_id"), Bool("active"), String("notes")),
            Contract("resource_spawn_rules.csv",
                Id("spawn_rule_id"), Id("resource_id"), IdList("biome_ids"),
                EnumList("patch_roles"), IntList("sector_route_types"),
                IdList("allowed_slot_pool_ids"), Int("world_min"), Int("world_max"),
                Int("patch_min"), Int("patch_max"), Int("spawn_weight"),
                Int("min_distance_from_same_resource_tiles"), Id("mandatory_site_id"),
                Bool("active"), String("notes")),
            Contract("spawn_pool_entries.csv",
                Id("spawn_pool_id"), Int("entry_order"), Enum("entry_kind"), Id("entry_id"),
                Int("weight"), Int("quantity_min"), Int("quantity_max"),
                IdList("required_tags"), IdList("forbidden_tags"), Bool("active"), String("notes")),
            Contract("special_item_slots.csv",
                Id("special_item_slot_id"), String("display_name_ko"), Id("unknown_sprite_prefab_id"),
                Id("revealed_sprite_prefab_id"), Bool("starts_revealed"), Int("maximum_per_world"),
                Id("effect_id"), Bool("active"), String("notes")),
            Contract("tile_code_dictionary.csv",
                Id("tile_code"), Enum("layer"), String("semantic"), Enum("collision_kind"),
                Bool("destructible"), Id("tile_asset_prefab_id"), Id("runtime_tag"),
                String("debug_glyph"), Bool("active")),
            Contract("tool_upgrade_definitions.csv",
                Id("tool_id"), Int("upgrade_level"), Int("required_blueprint_fragments"),
                Int("gold_cost"), Float("max_durability_multiplier"), Float("work_speed_multiplier"),
                Id("special_effect_id"), Bool("active"), String("notes"))
        };

        private static readonly IReadOnlyDictionary<string, FileContract> ContractsByFileName =
            Contracts.ToDictionary(item => item.FileName, item => item, StringComparer.Ordinal);

        public MicrochunkPopulationItemDefinitionBuildResult Build(
            IEnumerable<MicrochunkPopulationItemDefinitionSource> sourceDefinitions)
        {
            if (sourceDefinitions == null) throw new ArgumentNullException(nameof(sourceDefinitions));

            var errors = new List<MicrochunkPopulationItemDefinitionBuildError>();
            var sourcesByFile = CollectSources(sourceDefinitions, errors);
            foreach (var contract in Contracts)
            {
                if (!sourcesByFile.TryGetValue(contract.FileName, out var sources))
                {
                    errors.Add(Error(contract.FileName,
                        MicrochunkPopulationItemDefinitionBuildErrorCode.MissingSource,
                        "Required definition source is missing."));
                }
                else if (sources.Count == 1)
                {
                    ValidateSource(contract, sources[0], errors);
                }
            }

            errors.Sort(MicrochunkPopulationItemDefinitionBuildError.Compare);
            if (errors.Count > 0)
            {
                return new MicrochunkPopulationItemDefinitionBuildResult(null, errors);
            }

            try
            {
                return new MicrochunkPopulationItemDefinitionBuildResult(BuildSet(sourcesByFile), errors);
            }
            catch (Exception exception)
            {
                errors.Add(Error(string.Empty,
                    MicrochunkPopulationItemDefinitionBuildErrorCode.FieldMappingFailed,
                    "Definition materialization failed: " + exception.Message));
                return new MicrochunkPopulationItemDefinitionBuildResult(null, errors);
            }
        }

        private static Dictionary<string, List<MicrochunkPopulationItemDefinitionSource>> CollectSources(
            IEnumerable<MicrochunkPopulationItemDefinitionSource> sourceDefinitions,
            ICollection<MicrochunkPopulationItemDefinitionBuildError> errors)
        {
            var sourcesByFile = new Dictionary<string, List<MicrochunkPopulationItemDefinitionSource>>(StringComparer.Ordinal);
            foreach (var source in sourceDefinitions)
            {
                if (source == null)
                {
                    errors.Add(Error(string.Empty,
                        MicrochunkPopulationItemDefinitionBuildErrorCode.MissingSource,
                        "Definition source cannot be null."));
                    continue;
                }

                if (!ContractsByFileName.ContainsKey(source.FileName))
                {
                    errors.Add(Error(source.FileName,
                        MicrochunkPopulationItemDefinitionBuildErrorCode.UnexpectedSource,
                        "Definition source filename is not part of the exact 17-source contract."));
                    continue;
                }

                if (!sourcesByFile.TryGetValue(source.FileName, out var matches))
                {
                    matches = new List<MicrochunkPopulationItemDefinitionSource>();
                    sourcesByFile.Add(source.FileName, matches);
                }

                matches.Add(source);
                if (matches.Count > 1)
                {
                    errors.Add(Error(source.FileName,
                        MicrochunkPopulationItemDefinitionBuildErrorCode.DuplicateSource,
                        "Definition source filename occurs more than once."));
                }
            }

            return sourcesByFile;
        }

        private static void ValidateSource(
            FileContract contract,
            MicrochunkPopulationItemDefinitionSource source,
            ICollection<MicrochunkPopulationItemDefinitionBuildError> errors)
        {
            if (!source.ParseResult.Success || source.ParseResult.Errors.Count != 0)
            {
                errors.Add(Error(source.FileName,
                    MicrochunkPopulationItemDefinitionBuildErrorCode.UnsuccessfulParse,
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
            ICollection<MicrochunkPopulationItemDefinitionBuildError> errors)
        {
            var valid = true;
            if (!string.Equals(schema.FileName, contract.FileName, StringComparison.Ordinal))
            {
                errors.Add(Error(contract.FileName,
                    MicrochunkPopulationItemDefinitionBuildErrorCode.SchemaMismatch,
                    "Schema filename does not match the source contract."));
                valid = false;
            }

            if (schema.Columns.Count != contract.Columns.Length)
            {
                errors.Add(Error(contract.FileName,
                    MicrochunkPopulationItemDefinitionBuildErrorCode.SchemaMismatch,
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
                    actual.DataType != expected.DataType ||
                    !MetadataMatches(actual, expected))
                {
                    errors.Add(Error(contract.FileName,
                        MicrochunkPopulationItemDefinitionBuildErrorCode.SchemaMismatch,
                        "Schema column inventory, order, or data type does not match the source contract.",
                        null, index + 1, expected.ColumnName));
                    valid = false;
                }
            }

            return valid;
        }

        private static bool MetadataMatches(CsvColumnSchema actual, ColumnContract expected)
        {
            if (!expected.ValidateMetadata) return true;

            var foreignKeyMatches = expected.ForeignKeyFileName == null
                ? actual.ForeignKey == null
                : actual.ForeignKey != null &&
                  string.Equals(actual.ForeignKey.TargetFileName, expected.ForeignKeyFileName, StringComparison.Ordinal) &&
                  string.Equals(actual.ForeignKey.TargetColumnName, expected.ForeignKeyColumnName, StringComparison.Ordinal);
            return actual.IsRequired == expected.IsRequired &&
                   actual.PrimaryKeyOrder == expected.PrimaryKeyOrder &&
                   string.Equals(actual.DefaultValue, expected.DefaultValue, StringComparison.Ordinal) &&
                   actual.AllowedValues.SequenceEqual(expected.AllowedValues, StringComparer.Ordinal) &&
                   foreignKeyMatches;
        }

        private static void ValidateRecord(
            CsvFileSchema schema,
            CsvParsedRecord record,
            ICollection<MicrochunkPopulationItemDefinitionBuildError> errors)
        {
            if (record == null)
            {
                errors.Add(Error(schema.FileName,
                    MicrochunkPopulationItemDefinitionBuildErrorCode.FieldMappingFailed,
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
                errors.Add(Error(schema.FileName,
                    MicrochunkPopulationItemDefinitionBuildErrorCode.FieldMappingFailed,
                    "Parsed record source identity or field inventory is inconsistent.",
                    record.RecordNumber, null, null, sourceRecord?.StartLocation));
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
                    errors.Add(Error(schema.FileName,
                        MicrochunkPopulationItemDefinitionBuildErrorCode.FieldMappingFailed,
                        "Parsed field schema/source identity or value type is inconsistent.",
                        record.RecordNumber, column.ColumnOrder, column.ColumnName, location));
                }
            }
        }

        private static MicrochunkPopulationItemDefinitionSet BuildSet(
            IReadOnlyDictionary<string, List<MicrochunkPopulationItemDefinitionSource>> sourcesByFile)
        {
            return new MicrochunkPopulationItemDefinitionSet(
                Records(sourcesByFile, "map_element_definitions.csv").Select(item => new MapElementDefinition(item)),
                Records(sourcesByFile, "map_element_interactions.csv").Select(item => new MapElementInteractionDefinition(item)),
                Records(sourcesByFile, "microchunk_catalog.csv").Select(item => new MicrochunkDefinition(item)),
                Records(sourcesByFile, "microchunk_object_slots.csv").Select(item => new MicrochunkObjectSlotDefinition(item)),
                Records(sourcesByFile, "microchunk_pool_entries.csv").Select(item => new MicrochunkPoolEntryDefinition(item)),
                Records(sourcesByFile, "microchunk_sockets.csv").Select(item => new MicrochunkSocketDefinition(item)),
                Records(sourcesByFile, "microchunk_tile_cells.csv").Select(item => new MicrochunkTileCellDefinition(item)),
                Records(sourcesByFile, "microchunk_variant_rules.csv").Select(item => new MicrochunkVariantRuleDefinition(item)),
                Records(sourcesByFile, "population_profiles.csv").Select(item => new PopulationProfileDefinition(item)),
                Records(sourcesByFile, "prefab_registry.csv").Select(item => new PrefabRegistryDefinition(item)),
                Records(sourcesByFile, "resource_definitions.csv").Select(item => new ResourceDefinition(item)),
                Records(sourcesByFile, "resource_spawn_rules.csv").Select(item => new ResourceSpawnRuleDefinition(item)),
                Records(sourcesByFile, "spawn_pool_entries.csv").Select(item => new SpawnPoolEntryDefinition(item)),
                Records(sourcesByFile, "special_item_slots.csv").Select(item => new SpecialItemSlotDefinition(item)),
                Records(sourcesByFile, "tile_code_dictionary.csv").Select(item => new TileCodeDefinition(item)),
                Records(sourcesByFile, "tool_upgrade_definitions.csv").Select(item => new ToolUpgradeDefinition(item)),
                Records(sourcesByFile, "battery_profiles.csv").Select(item => new BatteryProfileDefinition(item)));
        }

        private static IReadOnlyList<CsvParsedRecord> Records(
            IReadOnlyDictionary<string, List<MicrochunkPopulationItemDefinitionSource>> sourcesByFile,
            string fileName) => sourcesByFile[fileName][0].ParseResult.Records;

        private static MicrochunkPopulationItemDefinitionBuildError Error(
            string fileName,
            MicrochunkPopulationItemDefinitionBuildErrorCode code,
            string message,
            int? recordNumber = null,
            int? columnOrder = null,
            string columnName = null,
            CsvSourceLocation? location = null) =>
            new MicrochunkPopulationItemDefinitionBuildError(
                fileName, code, message, recordNumber, columnOrder, columnName, location);

        private static FileContract Contract(string fileName, params ColumnContract[] columns) =>
            new FileContract(fileName, columns);

        private static ColumnContract String(string name) => Column(name, CsvSchemaDataType.String);
        private static ColumnContract Id(string name) => Column(name, CsvSchemaDataType.Id);
        private static ColumnContract Int(string name) => Column(name, CsvSchemaDataType.Int);
        private static ColumnContract Float(string name) => Column(name, CsvSchemaDataType.Float);
        private static ColumnContract Bool(string name) => Column(name, CsvSchemaDataType.Bool);
        private static ColumnContract Enum(string name) => Column(name, CsvSchemaDataType.Enum);
        private static ColumnContract IdList(string name) => Column(name, CsvSchemaDataType.IdList);
        private static ColumnContract EnumList(string name) => Column(name, CsvSchemaDataType.EnumList);
        private static ColumnContract IntList(string name) => Column(name, CsvSchemaDataType.IntList);
        private static ColumnContract Column(string name, CsvSchemaDataType dataType) => new ColumnContract(name, dataType);
        private static ColumnContract Exact(
            string name,
            CsvSchemaDataType dataType,
            bool isRequired,
            int? primaryKeyOrder = null,
            string defaultValue = "",
            string[] allowedValues = null,
            string foreignKeyFileName = null,
            string foreignKeyColumnName = null) =>
            new ColumnContract(
                name, dataType, isRequired, primaryKeyOrder, defaultValue,
                allowedValues, foreignKeyFileName, foreignKeyColumnName);

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
                IsRequired = false;
                DefaultValue = string.Empty;
                AllowedValues = Array.Empty<string>();
            }

            public ColumnContract(
                string columnName,
                CsvSchemaDataType dataType,
                bool isRequired,
                int? primaryKeyOrder,
                string defaultValue,
                string[] allowedValues,
                string foreignKeyFileName,
                string foreignKeyColumnName)
            {
                ColumnName = columnName;
                DataType = dataType;
                ValidateMetadata = true;
                IsRequired = isRequired;
                PrimaryKeyOrder = primaryKeyOrder;
                DefaultValue = defaultValue ?? string.Empty;
                AllowedValues = allowedValues ?? Array.Empty<string>();
                ForeignKeyFileName = foreignKeyFileName;
                ForeignKeyColumnName = foreignKeyColumnName;
            }

            public string ColumnName { get; }
            public CsvSchemaDataType DataType { get; }
            public bool ValidateMetadata { get; }
            public bool IsRequired { get; }
            public int? PrimaryKeyOrder { get; }
            public string DefaultValue { get; }
            public IReadOnlyList<string> AllowedValues { get; }
            public string ForeignKeyFileName { get; }
            public string ForeignKeyColumnName { get; }
        }
    }
}
