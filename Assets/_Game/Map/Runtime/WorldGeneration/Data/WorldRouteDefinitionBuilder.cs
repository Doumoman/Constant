using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class WorldRouteDefinitionBuilder
    {
        private static readonly FileContract[] Contracts =
        {
            Contract("world_profiles.csv",
                Id("world_profile_id"), String("display_name_ko"), Int("width_tiles"), Int("height_tiles"),
                Int("sector_width_tiles"), Int("sector_height_tiles"), Int("sector_cols"), Int("sector_rows"),
                Int("micro_width_tiles"), Int("micro_height_tiles"), Int("micro_cols_per_sector"),
                Int("micro_rows_per_sector"), Int("min_completion_distance_tiles"),
                Int("max_shortest_completion_distance_tiles"), Int("normal_completion_min_tiles"),
                Int("normal_completion_max_tiles"), Int("optional_completion_max_tiles"),
                Float("max_revisit_ratio"), Int("required_village_count"), Bool("active"), String("notes")),
            Contract("generation_profiles.csv",
                Id("generation_profile_id"), Id("world_profile_id"),
                Int("mandatory_sector_min"), Int("mandatory_sector_max"),
                Int("type0_sector_min"), Int("type0_sector_max"),
                Int("reserved_sector_min"), Int("reserved_sector_max"),
                Int("inactive_sector_min"), Int("inactive_sector_max"),
                Int("start_edge_ring_min"), Int("start_edge_ring_max"),
                Int("mandatory_loop_min"), Int("mandatory_loop_max"),
                Int("optional_region_depth_min"), Int("optional_region_depth_max"),
                Int("optional_region_count_min"), Int("optional_region_count_max"),
                Int("site_reservation_retry_max"), Int("biome_retry_max"), Int("route_retry_max"),
                Int("sector_solve_retry_max"), Bool("active"), String("notes")),
            Contract("generation_passes.csv",
                Id("generation_profile_id"), Int("pass_order"), Id("pass_id"), String("class_name"),
                Id("rng_stream_id"), IdList("input_artifacts"), IdList("output_artifacts"),
                Enum("failure_policy"), Int("max_retry_count"), Bool("enabled"), String("notes")),
            Contract("rng_streams.csv",
                Id("rng_stream_id"), Hex("salt_hex"), Enum("reset_scope"), String("description_ko"), Bool("active")),
            Contract("sector_route_masks.csv",
                Id("route_mask_id"), Int("route_type"), Bool("open_l"), Bool("open_r"), Bool("open_u"),
                Bool("open_d"), Bool("mandatory_allowed"), String("description_ko"), Bool("active")),
            Contract("socket_band_definitions.csv",
                Id("band_id"), Enum("axis"), Int("min_local_coord"), Int("max_local_coord"),
                Float("recommended_center"), Int("minimum_clearance_tiles"), String("description_ko")),
            Contract("edge_signatures.csv",
                Id("edge_signature_id"), Enum("axis"), Id("band_id"), Enum("traversal_kind"),
                Int("ground_entry_height"), Int("clearance_width"), Int("clearance_height"),
                Enum("tool_requirement"), Bool("mandatory_allowed"), IdList("tags"), String("notes")),
            Contract("edge_signature_compatibility.csv",
                Id("signature_a"), Id("signature_b"), Bool("compatible"),
                Id("adapter_microchunk_pool_id"), String("notes")),
            Contract("sector_recipe_catalog.csv",
                Id("sector_recipe_id"), String("display_name_ko"), Int("route_type"), Id("route_mask_id"),
                Id("primary_biome_id"), Id("secondary_biome_id"), Id("boundary_profile_id"),
                Enum("recipe_kind"), Id("microchunk_budget_profile_id"), Int("selection_weight"),
                Bool("supports_special_entry"), Bool("supports_village_entry"), Bool("active"), String("notes")),
            Contract("sector_recipe_cells.csv",
                Id("sector_recipe_id"), Int("chunk_x"), Int("chunk_y"), Enum("cell_role"),
                Id("fixed_microchunk_id"), Id("microchunk_pool_id"), EnumList("required_usage_class"),
                IdList("required_route_roles"), IdList("required_biome_ids"),
                Id("required_signature_l"), Id("required_signature_r"), Id("required_signature_u"),
                Id("required_signature_d"), EnumList("transform_policy"), String("notes")),
            Contract("sector_recipe_paths.csv",
                Id("sector_recipe_id"), Id("path_id"), Int("path_order"), Int("chunk_x"), Int("chunk_y"),
                Enum("enter_side"), Enum("exit_side"), Bool("mandatory"), Enum("traversal_kind"),
                Int("max_jump_tiles"), String("notes")),
            Contract("sector_external_sockets.csv",
                Id("sector_recipe_id"), Id("socket_id"), Enum("side"), Int("edge_chunk_index"),
                Id("band_id"), Enum("traversal_kind"), Bool("mandatory_allowed"),
                Id("edge_signature_id"), String("notes")),
            Contract("sector_recipe_pool_entries.csv",
                Id("sector_recipe_pool_id"), Int("entry_order"), Id("sector_recipe_id"), Int("weight"),
                Int("min_repeat_distance_sectors"), Enum("required_patch_role"), Bool("active"))
        };

        private static readonly IReadOnlyDictionary<string, FileContract> ContractsByFileName =
            Contracts.ToDictionary(item => item.FileName, item => item, StringComparer.Ordinal);

        public WorldRouteDefinitionBuildResult Build(
            IEnumerable<WorldRouteDefinitionSource> sourceDefinitions)
        {
            if (sourceDefinitions == null)
            {
                throw new ArgumentNullException(nameof(sourceDefinitions));
            }

            var errors = new List<WorldRouteDefinitionBuildError>();
            var sourcesByFile = CollectSources(sourceDefinitions, errors);
            foreach (var contract in Contracts)
            {
                if (!sourcesByFile.TryGetValue(contract.FileName, out var sources))
                {
                    errors.Add(Error(
                        contract.FileName,
                        WorldRouteDefinitionBuildErrorCode.MissingSource,
                        "Required definition source is missing."));
                    continue;
                }

                if (sources.Count != 1)
                {
                    continue;
                }

                ValidateSource(contract, sources[0], errors);
            }

            errors.Sort(WorldRouteDefinitionBuildError.Compare);
            if (errors.Count > 0)
            {
                return new WorldRouteDefinitionBuildResult(null, errors);
            }

            try
            {
                return new WorldRouteDefinitionBuildResult(BuildSet(sourcesByFile), errors);
            }
            catch (Exception exception)
            {
                errors.Add(Error(
                    string.Empty,
                    WorldRouteDefinitionBuildErrorCode.FieldMappingFailed,
                    "Definition materialization failed: " + exception.Message));
                return new WorldRouteDefinitionBuildResult(null, errors);
            }
        }

        private static Dictionary<string, List<WorldRouteDefinitionSource>> CollectSources(
            IEnumerable<WorldRouteDefinitionSource> sourceDefinitions,
            ICollection<WorldRouteDefinitionBuildError> errors)
        {
            var sourcesByFile = new Dictionary<string, List<WorldRouteDefinitionSource>>(StringComparer.Ordinal);
            foreach (var source in sourceDefinitions)
            {
                if (source == null)
                {
                    errors.Add(Error(
                        string.Empty,
                        WorldRouteDefinitionBuildErrorCode.MissingSource,
                        "Definition source cannot be null."));
                    continue;
                }

                if (!ContractsByFileName.ContainsKey(source.FileName))
                {
                    errors.Add(Error(
                        source.FileName,
                        WorldRouteDefinitionBuildErrorCode.UnexpectedSource,
                        "Definition source filename is not part of the exact 13-source contract."));
                    continue;
                }

                if (!sourcesByFile.TryGetValue(source.FileName, out var matches))
                {
                    matches = new List<WorldRouteDefinitionSource>();
                    sourcesByFile.Add(source.FileName, matches);
                }

                matches.Add(source);
                if (matches.Count > 1)
                {
                    errors.Add(Error(
                        source.FileName,
                        WorldRouteDefinitionBuildErrorCode.DuplicateSource,
                        "Definition source filename occurs more than once."));
                }
            }

            return sourcesByFile;
        }

        private static void ValidateSource(
            FileContract contract,
            WorldRouteDefinitionSource source,
            ICollection<WorldRouteDefinitionBuildError> errors)
        {
            if (!source.ParseResult.Success || source.ParseResult.Errors.Count != 0)
            {
                errors.Add(Error(
                    source.FileName,
                    WorldRouteDefinitionBuildErrorCode.UnsuccessfulParse,
                    "Definition source parse result must be successful and contain zero errors."));
                return;
            }

            if (!ValidateSchema(contract, source.Schema, errors))
            {
                return;
            }

            foreach (var record in source.ParseResult.Records)
            {
                ValidateRecord(source.Schema, record, errors);
            }
        }

        private static bool ValidateSchema(
            FileContract contract,
            CsvFileSchema schema,
            ICollection<WorldRouteDefinitionBuildError> errors)
        {
            var valid = true;
            if (!string.Equals(schema.FileName, contract.FileName, StringComparison.Ordinal))
            {
                errors.Add(Error(
                    contract.FileName,
                    WorldRouteDefinitionBuildErrorCode.SchemaMismatch,
                    "Schema filename does not match the source contract."));
                valid = false;
            }

            if (schema.Columns.Count != contract.Columns.Length)
            {
                errors.Add(Error(
                    contract.FileName,
                    WorldRouteDefinitionBuildErrorCode.SchemaMismatch,
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
                        WorldRouteDefinitionBuildErrorCode.SchemaMismatch,
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
            ICollection<WorldRouteDefinitionBuildError> errors)
        {
            if (record == null)
            {
                errors.Add(Error(
                    schema.FileName,
                    WorldRouteDefinitionBuildErrorCode.FieldMappingFailed,
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
                    WorldRouteDefinitionBuildErrorCode.FieldMappingFailed,
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
                        WorldRouteDefinitionBuildErrorCode.FieldMappingFailed,
                        "Parsed field schema/source identity or value type is inconsistent.",
                        record.RecordNumber,
                        column.ColumnOrder,
                        column.ColumnName,
                        location));
                }
            }
        }

        private static WorldRouteDefinitionSet BuildSet(
            IReadOnlyDictionary<string, List<WorldRouteDefinitionSource>> sourcesByFile)
        {
            return new WorldRouteDefinitionSet(
                Records(sourcesByFile, "world_profiles.csv").Select(item => new WorldProfileDefinition(item)),
                Records(sourcesByFile, "generation_profiles.csv").Select(item => new GenerationProfileDefinition(item)),
                Records(sourcesByFile, "generation_passes.csv").Select(item => new GenerationPassDefinition(item)),
                Records(sourcesByFile, "rng_streams.csv").Select(item => new RngStreamDefinition(item)),
                Records(sourcesByFile, "sector_route_masks.csv").Select(item => new SectorRouteMaskDefinition(item)),
                Records(sourcesByFile, "socket_band_definitions.csv").Select(item => new SocketBandDefinition(item)),
                Records(sourcesByFile, "edge_signatures.csv").Select(item => new EdgeSignatureDefinition(item)),
                Records(sourcesByFile, "edge_signature_compatibility.csv").Select(item => new EdgeSignatureCompatibilityDefinition(item)),
                Records(sourcesByFile, "sector_recipe_catalog.csv").Select(item => new SectorRecipeDefinition(item)),
                Records(sourcesByFile, "sector_recipe_cells.csv").Select(item => new SectorRecipeCellDefinition(item)),
                Records(sourcesByFile, "sector_recipe_paths.csv").Select(item => new SectorRecipePathDefinition(item)),
                Records(sourcesByFile, "sector_external_sockets.csv").Select(item => new SectorExternalSocketDefinition(item)),
                Records(sourcesByFile, "sector_recipe_pool_entries.csv").Select(item => new SectorRecipePoolEntryDefinition(item)));
        }

        private static IReadOnlyList<CsvParsedRecord> Records(
            IReadOnlyDictionary<string, List<WorldRouteDefinitionSource>> sourcesByFile,
            string fileName)
        {
            return sourcesByFile[fileName][0].ParseResult.Records;
        }

        private static WorldRouteDefinitionBuildError Error(
            string fileName,
            WorldRouteDefinitionBuildErrorCode code,
            string message,
            int? recordNumber = null,
            int? columnOrder = null,
            string columnName = null,
            CsvSourceLocation? location = null)
        {
            return new WorldRouteDefinitionBuildError(
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
        private static ColumnContract Hex(string name) => Column(name, CsvSchemaDataType.Hex);

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
