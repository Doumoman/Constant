using System;
using System.Collections.Generic;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvPrimaryKeyIndexBuilder
    {
        public CsvPrimaryKeyIndexBuildResult Build(
            CsvFileSchema schema,
            CsvHeaderFieldValidationResult validationResult)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            return Build(schema, validationResult, schema.FileName);
        }

        public CsvPrimaryKeyIndexBuildResult Build(
            CsvFileSchema schema,
            CsvHeaderFieldValidationResult validationResult,
            string sourceName)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (validationResult == null)
            {
                throw new ArgumentNullException(nameof(validationResult));
            }

            if (sourceName == null)
            {
                throw new ArgumentNullException(nameof(sourceName));
            }

            if (!validationResult.Success || validationResult.Errors.Count != 0)
            {
                throw new InvalidOperationException(
                    "Primary-key indexing requires a successful header/field validation result.");
            }

            if (schema.PrimaryKeyColumns.Count == 0)
            {
                throw new InvalidOperationException(
                    "A CSV schema must contain at least one primary-key column.");
            }

            var primaryKeyFieldIndexes = GetPrimaryKeyFieldIndexes(schema);
            ValidateMappings(schema, validationResult);

            var occurrencesByKey = new Dictionary<
                CsvPrimaryKey,
                List<CsvPrimaryKeyOccurrence>>();
            foreach (var record in validationResult.Records)
            {
                var primaryKeyFields = new List<CsvValidatedField>(
                    primaryKeyFieldIndexes.Count);
                var components = new List<string>(primaryKeyFieldIndexes.Count);
                foreach (var fieldIndex in primaryKeyFieldIndexes)
                {
                    var field = record.Fields[fieldIndex];
                    if (field.EffectiveValue.Length == 0)
                    {
                        throw new InvalidOperationException(
                            "A CSV primary-key component cannot be empty: " +
                            schema.FileName + "." + field.Schema.ColumnName + ".");
                    }

                    primaryKeyFields.Add(field);
                    components.Add(field.EffectiveValue);
                }

                var key = new CsvPrimaryKey(components);
                var occurrence = new CsvPrimaryKeyOccurrence(
                    sourceName,
                    schema.FileName,
                    key,
                    record,
                    primaryKeyFields);
                if (!occurrencesByKey.TryGetValue(key, out var occurrences))
                {
                    occurrences = new List<CsvPrimaryKeyOccurrence>();
                    occurrencesByKey.Add(key, occurrences);
                }

                occurrences.Add(occurrence);
            }

            var duplicateGroups = new List<CsvDuplicatePrimaryKey>();
            var uniqueOccurrences = new List<CsvPrimaryKeyOccurrence>();
            foreach (var pair in occurrencesByKey)
            {
                pair.Value.Sort(CsvPrimaryKeyOccurrence.CompareSourceOrder);
                if (pair.Value.Count > 1)
                {
                    duplicateGroups.Add(new CsvDuplicatePrimaryKey(
                        schema.FileName,
                        pair.Key,
                        pair.Value));
                }
                else
                {
                    uniqueOccurrences.Add(pair.Value[0]);
                }
            }

            duplicateGroups.Sort((left, right) => left.Key.CompareTo(right.Key));
            if (duplicateGroups.Count > 0)
            {
                return new CsvPrimaryKeyIndexBuildResult(null, duplicateGroups);
            }

            return new CsvPrimaryKeyIndexBuildResult(
                new CsvPrimaryKeyIndex(schema.FileName, uniqueOccurrences),
                Array.Empty<CsvDuplicatePrimaryKey>());
        }

        private static List<int> GetPrimaryKeyFieldIndexes(CsvFileSchema schema)
        {
            var indexes = new List<int>(schema.PrimaryKeyColumns.Count);
            foreach (var primaryKeyColumn in schema.PrimaryKeyColumns)
            {
                var matchedIndex = -1;
                for (var columnIndex = 0; columnIndex < schema.Columns.Count; columnIndex++)
                {
                    if (ReferenceEquals(schema.Columns[columnIndex], primaryKeyColumn))
                    {
                        matchedIndex = columnIndex;
                        break;
                    }
                }

                if (matchedIndex < 0)
                {
                    throw new InvalidOperationException(
                        "A primary-key column is not part of its CSV file schema.");
                }

                indexes.Add(matchedIndex);
            }

            return indexes;
        }

        private static void ValidateMappings(
            CsvFileSchema schema,
            CsvHeaderFieldValidationResult validationResult)
        {
            foreach (var record in validationResult.Records)
            {
                if (record == null ||
                    record.SourceRecord == null ||
                    record.RecordNumber != record.SourceRecord.RecordNumber ||
                    record.Fields.Count != schema.Columns.Count ||
                    record.SourceRecord.Fields.Count != schema.Columns.Count)
                {
                    throw new InvalidOperationException(
                        "Validated CSV record count or source mapping does not match the schema.");
                }

                for (var fieldIndex = 0; fieldIndex < schema.Columns.Count; fieldIndex++)
                {
                    var field = record.Fields[fieldIndex];
                    if (field == null ||
                        !ReferenceEquals(field.Schema, schema.Columns[fieldIndex]) ||
                        !ReferenceEquals(
                            field.SourceField,
                            record.SourceRecord.Fields[fieldIndex]))
                    {
                        throw new InvalidOperationException(
                            "Validated CSV field mapping does not match the supplied schema.");
                    }
                }
            }
        }
    }
}
