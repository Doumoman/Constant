using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class BiomeBoundaryDefinitionBuilder
    {
        private static readonly FileContract[] Contracts =
        {
            Contract("biome_types.csv",
                Id("biome_id"), String("display_name_ko"), Id("stage_id"), Bool("required"),
                Int("min_patch_count"), Int("max_patch_count"), Int("min_core_patch_count"),
                Int("preferred_altitude_min_sector_y"), Int("preferred_altitude_max_sector_y"),
                Float("growth_weight"), Id("tile_theme_id"), Id("audio_profile_id"),
                Id("microchunk_pool_prefix"), Id("sector_recipe_pool_prefix"),
                Id("common_resource_pool_id"), Id("map_element_pool_id"),
                IdList("required_special_map_ids"), Bool("active"), String("notes")),
            Contract("biome_patch_rules.csv",
                Id("patch_rule_id"), Id("biome_id"), Enum("patch_role"),
                Int("min_sector_count"), Int("max_sector_count"), Int("min_seed_distance"),
                Int("seed_count_min"), Int("seed_count_max"), Float("seed_weight"),
                Bool("can_touch_world_edge"), Int("buffer_ring_sectors"), Bool("allow_single_sector"),
                Float("max_world_share"), Float("distance_weight"), Float("altitude_weight"),
                Float("noise_weight"), Float("compactness_weight"), Float("branchiness_target"),
                Bool("active"), String("notes")),
            Contract("biome_boundary_profiles.csv",
                Id("boundary_profile_id"), String("display_name_ko"), Enum("boundary_type"),
                EnumList("allowed_orientations"), Int("width_microchunks_min"),
                Int("width_microchunks_max"), Int("warning_microchunks_min"),
                Bool("mandatory_route_allowed"), Enum("tool_requirement"), Bool("hard_border"),
                Bool("active"), String("notes")),
            Contract("biome_boundary_pair_rules.csv",
                Id("boundary_pair_rule_id"), Id("biome_a_id"), Id("biome_b_id"),
                IdList("allowed_boundary_profile_ids"), IntList("boundary_profile_weights"),
                Id("default_boundary_profile_id"), Id("transition_resource_pool_id"),
                Id("transition_element_pool_id"), Int("min_shared_edge_count"), Bool("active"),
                String("notes")),
            Contract("boundary_chunk_catalog.csv",
                Id("boundary_chunk_id"), Id("microchunk_id"), Id("biome_a_id"), Id("biome_b_id"),
                Id("boundary_profile_id"), Enum("orientation"), Int("route_type"),
                Id("entry_edge_signature_id"), Id("exit_edge_signature_id"), Int("weight"),
                Bool("reversible"), Bool("active"), String("notes"))
        };

        private static readonly IReadOnlyDictionary<string, FileContract> ContractsByFileName =
            Contracts.ToDictionary(item => item.FileName, item => item, StringComparer.Ordinal);

        public BiomeBoundaryDefinitionBuildResult Build(
            IEnumerable<BiomeBoundaryDefinitionSource> sourceDefinitions)
        {
            if (sourceDefinitions == null) throw new ArgumentNullException(nameof(sourceDefinitions));

            var errors = new List<BiomeBoundaryDefinitionBuildError>();
            var sourcesByFile = CollectSources(sourceDefinitions, errors);
            foreach (var contract in Contracts)
            {
                if (!sourcesByFile.TryGetValue(contract.FileName, out var sources))
                {
                    errors.Add(Error(contract.FileName,
                        BiomeBoundaryDefinitionBuildErrorCode.MissingSource,
                        "Required definition source is missing."));
                    continue;
                }

                if (sources.Count == 1)
                {
                    ValidateSource(contract, sources[0], errors);
                }
            }

            errors.Sort(BiomeBoundaryDefinitionBuildError.Compare);
            if (errors.Count > 0)
            {
                return new BiomeBoundaryDefinitionBuildResult(null, errors);
            }

            try
            {
                return new BiomeBoundaryDefinitionBuildResult(BuildSet(sourcesByFile), errors);
            }
            catch (Exception exception)
            {
                errors.Add(Error(string.Empty,
                    BiomeBoundaryDefinitionBuildErrorCode.FieldMappingFailed,
                    "Definition materialization failed: " + exception.Message));
                return new BiomeBoundaryDefinitionBuildResult(null, errors);
            }
        }

        private static Dictionary<string, List<BiomeBoundaryDefinitionSource>> CollectSources(
            IEnumerable<BiomeBoundaryDefinitionSource> sourceDefinitions,
            ICollection<BiomeBoundaryDefinitionBuildError> errors)
        {
            var sourcesByFile = new Dictionary<string, List<BiomeBoundaryDefinitionSource>>(StringComparer.Ordinal);
            foreach (var source in sourceDefinitions)
            {
                if (source == null)
                {
                    errors.Add(Error(string.Empty,
                        BiomeBoundaryDefinitionBuildErrorCode.MissingSource,
                        "Definition source cannot be null."));
                    continue;
                }

                if (!ContractsByFileName.ContainsKey(source.FileName))
                {
                    errors.Add(Error(source.FileName,
                        BiomeBoundaryDefinitionBuildErrorCode.UnexpectedSource,
                        "Definition source filename is not part of the exact 5-source contract."));
                    continue;
                }

                if (!sourcesByFile.TryGetValue(source.FileName, out var matches))
                {
                    matches = new List<BiomeBoundaryDefinitionSource>();
                    sourcesByFile.Add(source.FileName, matches);
                }

                matches.Add(source);
                if (matches.Count > 1)
                {
                    errors.Add(Error(source.FileName,
                        BiomeBoundaryDefinitionBuildErrorCode.DuplicateSource,
                        "Definition source filename occurs more than once."));
                }
            }

            return sourcesByFile;
        }

        private static void ValidateSource(
            FileContract contract,
            BiomeBoundaryDefinitionSource source,
            ICollection<BiomeBoundaryDefinitionBuildError> errors)
        {
            if (!source.ParseResult.Success || source.ParseResult.Errors.Count != 0)
            {
                errors.Add(Error(source.FileName,
                    BiomeBoundaryDefinitionBuildErrorCode.UnsuccessfulParse,
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
            ICollection<BiomeBoundaryDefinitionBuildError> errors)
        {
            var valid = true;
            if (!string.Equals(schema.FileName, contract.FileName, StringComparison.Ordinal))
            {
                errors.Add(Error(contract.FileName,
                    BiomeBoundaryDefinitionBuildErrorCode.SchemaMismatch,
                    "Schema filename does not match the source contract."));
                valid = false;
            }

            if (schema.Columns.Count != contract.Columns.Length)
            {
                errors.Add(Error(contract.FileName,
                    BiomeBoundaryDefinitionBuildErrorCode.SchemaMismatch,
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
                    errors.Add(Error(contract.FileName,
                        BiomeBoundaryDefinitionBuildErrorCode.SchemaMismatch,
                        "Schema column inventory, order, or data type does not match the source contract.",
                        null, index + 1, expected.ColumnName));
                    valid = false;
                }
            }

            return valid;
        }

        private static void ValidateRecord(
            CsvFileSchema schema,
            CsvParsedRecord record,
            ICollection<BiomeBoundaryDefinitionBuildError> errors)
        {
            if (record == null)
            {
                errors.Add(Error(schema.FileName,
                    BiomeBoundaryDefinitionBuildErrorCode.FieldMappingFailed,
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
                    BiomeBoundaryDefinitionBuildErrorCode.FieldMappingFailed,
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
                        BiomeBoundaryDefinitionBuildErrorCode.FieldMappingFailed,
                        "Parsed field schema/source identity or value type is inconsistent.",
                        record.RecordNumber, column.ColumnOrder, column.ColumnName, location));
                }
            }
        }

        private static BiomeBoundaryDefinitionSet BuildSet(
            IReadOnlyDictionary<string, List<BiomeBoundaryDefinitionSource>> sourcesByFile)
        {
            return new BiomeBoundaryDefinitionSet(
                Records(sourcesByFile, "biome_types.csv").Select(item => new BiomeTypeDefinition(item)),
                Records(sourcesByFile, "biome_patch_rules.csv").Select(item => new BiomePatchRuleDefinition(item)),
                Records(sourcesByFile, "biome_boundary_profiles.csv").Select(item => new BiomeBoundaryProfileDefinition(item)),
                Records(sourcesByFile, "biome_boundary_pair_rules.csv").Select(item => new BiomeBoundaryPairRuleDefinition(item)),
                Records(sourcesByFile, "boundary_chunk_catalog.csv").Select(item => new BoundaryChunkDefinition(item)));
        }

        private static IReadOnlyList<CsvParsedRecord> Records(
            IReadOnlyDictionary<string, List<BiomeBoundaryDefinitionSource>> sourcesByFile,
            string fileName)
        {
            return sourcesByFile[fileName][0].ParseResult.Records;
        }

        private static BiomeBoundaryDefinitionBuildError Error(
            string fileName,
            BiomeBoundaryDefinitionBuildErrorCode code,
            string message,
            int? recordNumber = null,
            int? columnOrder = null,
            string columnName = null,
            CsvSourceLocation? location = null)
        {
            return new BiomeBoundaryDefinitionBuildError(
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
