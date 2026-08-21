using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvSchemaCatalogBuildResult
    {
        private readonly ReadOnlyCollection<CsvSchemaCatalogError> errors;

        internal CsvSchemaCatalogBuildResult(
            CsvSchemaCatalog catalog,
            IEnumerable<CsvSchemaCatalogError> errors)
        {
            Catalog = catalog;
            this.errors = new ReadOnlyCollection<CsvSchemaCatalogError>(
                new List<CsvSchemaCatalogError>(errors ?? Array.Empty<CsvSchemaCatalogError>()));
        }

        public bool Success => Catalog != null && errors.Count == 0;

        public CsvSchemaCatalog Catalog { get; }

        public IReadOnlyList<CsvSchemaCatalogError> Errors => errors;
    }

    public sealed class CsvSchemaCatalogBuilder
    {
        public CsvSchemaCatalogBuildResult Build(IEnumerable<CsvSchemaDictionaryRow> sourceRows)
        {
            if (sourceRows == null)
            {
                throw new ArgumentNullException(nameof(sourceRows));
            }

            var errors = new List<CsvSchemaCatalogError>();
            var parsedRows = new List<ParsedRow>();
            foreach (var row in sourceRows)
            {
                if (row == null)
                {
                    errors.Add(new CsvSchemaCatalogError(
                        "NULL_ROW",
                        "Schema dictionary row cannot be null.",
                        0,
                        string.Empty,
                        string.Empty));
                    continue;
                }

                var parsedRow = ParseRow(row, errors);
                if (parsedRow != null)
                {
                    parsedRows.Add(parsedRow);
                }
            }

            ValidateFiles(parsedRows, errors);
            errors.Sort(CsvSchemaCatalogError.Compare);
            if (errors.Count > 0)
            {
                return new CsvSchemaCatalogBuildResult(null, errors);
            }

            var files = parsedRows
                .GroupBy(row => row.Source.FileName, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new CsvFileSchema(
                    group.Key,
                    group.OrderBy(row => row.ColumnOrder)
                        .Select(ToColumnSchema)))
                .ToArray();
            return new CsvSchemaCatalogBuildResult(new CsvSchemaCatalog(files), errors);
        }

        private static ParsedRow ParseRow(
            CsvSchemaDictionaryRow row,
            ICollection<CsvSchemaCatalogError> errors)
        {
            var errorCountBefore = errors.Count;
            if (string.IsNullOrWhiteSpace(row.FileName))
            {
                AddError(errors, row, "EMPTY_FILE_NAME", "file_name cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(row.ColumnName))
            {
                AddError(errors, row, "EMPTY_COLUMN_NAME", "column_name cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(row.DataType))
            {
                AddError(errors, row, "EMPTY_DATA_TYPE", "data_type cannot be empty.");
            }

            if (!TryParsePositiveInteger(row.ColumnOrder, out var columnOrder))
            {
                AddError(
                    errors,
                    row,
                    "INVALID_COLUMN_ORDER",
                    "column_order must be a positive invariant integer.");
            }

            bool isRequired;
            if (row.Required == "0")
            {
                isRequired = false;
            }
            else if (row.Required == "1")
            {
                isRequired = true;
            }
            else
            {
                isRequired = false;
                AddError(errors, row, "INVALID_REQUIRED", "required must be exactly 0 or 1.");
            }

            int? primaryKeyOrder = null;
            if (!string.IsNullOrEmpty(row.PrimaryKeyOrder))
            {
                if (TryParsePositiveInteger(row.PrimaryKeyOrder, out var parsedPrimaryKeyOrder))
                {
                    primaryKeyOrder = parsedPrimaryKeyOrder;
                }
                else
                {
                    AddError(
                        errors,
                        row,
                        "INVALID_PRIMARY_KEY_ORDER",
                        "primary_key_order must be empty or a positive invariant integer.");
                }
            }

            if (!CsvSchemaDataTypes.TryParse(row.DataType, out var dataType))
            {
                AddError(
                    errors,
                    row,
                    "INVALID_DATA_TYPE",
                    "Unknown case-sensitive data_type token: " + row.DataType);
            }

            var allowedValues = ParseAllowedValues(row, errors);
            if (!CsvForeignKeyReference.TryParse(
                    row.ForeignKey,
                    out var foreignKey,
                    out var foreignKeyError))
            {
                AddError(errors, row, "INVALID_FOREIGN_KEY", foreignKeyError);
            }

            if (errors.Count != errorCountBefore)
            {
                return null;
            }

            return new ParsedRow(
                row,
                columnOrder,
                dataType,
                isRequired,
                primaryKeyOrder,
                allowedValues,
                foreignKey);
        }

        private static string[] ParseAllowedValues(
            CsvSchemaDictionaryRow row,
            ICollection<CsvSchemaCatalogError> errors)
        {
            if (string.IsNullOrEmpty(row.AllowedValues))
            {
                return Array.Empty<string>();
            }

            var values = row.AllowedValues.Split('|');
            var trimmedValues = new string[values.Length];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < values.Length; index++)
            {
                var value = values[index].Trim();
                trimmedValues[index] = value;
                if (value.Length == 0)
                {
                    AddError(
                        errors,
                        row,
                        "EMPTY_ALLOWED_VALUE",
                        "allowed_values contains an empty item.");
                }
                else if (!seen.Add(value))
                {
                    AddError(
                        errors,
                        row,
                        "DUPLICATE_ALLOWED_VALUE",
                        "allowed_values contains an ordinal duplicate: " + value);
                }
            }

            return trimmedValues;
        }

        private static void ValidateFiles(
            IReadOnlyCollection<ParsedRow> rows,
            ICollection<CsvSchemaCatalogError> errors)
        {
            foreach (var fileRows in rows
                         .GroupBy(row => row.Source.FileName, StringComparer.Ordinal)
                         .OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                var orderedRows = fileRows
                    .OrderBy(row => row.Source.SourceRowNumber)
                    .ThenBy(row => row.Source.ColumnName, StringComparer.Ordinal)
                    .ToArray();

                foreach (var duplicate in orderedRows
                             .GroupBy(row => row.Source.ColumnName, StringComparer.Ordinal)
                             .Where(group => group.Count() > 1))
                {
                    foreach (var row in duplicate.Skip(1))
                    {
                        AddError(
                            errors,
                            row.Source,
                            "DUPLICATE_COLUMN_NAME",
                            "Duplicate ordinal column name: " + duplicate.Key);
                    }
                }

                foreach (var duplicate in orderedRows
                             .GroupBy(row => row.ColumnOrder)
                             .Where(group => group.Count() > 1))
                {
                    foreach (var row in duplicate.Skip(1))
                    {
                        AddError(
                            errors,
                            row.Source,
                            "DUPLICATE_COLUMN_ORDER",
                            "Duplicate column_order: " + duplicate.Key);
                    }
                }

                var columnOrders = orderedRows
                    .Select(row => row.ColumnOrder)
                    .Distinct()
                    .OrderBy(value => value)
                    .ToArray();
                if (!IsContiguousFromOne(columnOrders))
                {
                    AddFileError(
                        errors,
                        orderedRows,
                        "NON_CONTIGUOUS_COLUMN_ORDER",
                        "column_order must be contiguous from 1.");
                }

                var primaryKeyRows = orderedRows
                    .Where(row => row.PrimaryKeyOrder.HasValue)
                    .ToArray();
                if (primaryKeyRows.Length == 0)
                {
                    AddFileError(
                        errors,
                        orderedRows,
                        "MISSING_PRIMARY_KEY",
                        "Every schema file must declare at least one primary key column.");
                }

                foreach (var primaryKeyRow in primaryKeyRows.Where(row => !row.IsRequired))
                {
                    AddError(
                        errors,
                        primaryKeyRow.Source,
                        "PRIMARY_KEY_NOT_REQUIRED",
                        "Primary key columns must be required.");
                }

                foreach (var duplicate in primaryKeyRows
                             .GroupBy(row => row.PrimaryKeyOrder.Value)
                             .Where(group => group.Count() > 1))
                {
                    foreach (var row in duplicate.Skip(1))
                    {
                        AddError(
                            errors,
                            row.Source,
                            "DUPLICATE_PRIMARY_KEY_ORDER",
                            "Duplicate primary_key_order: " + duplicate.Key);
                    }
                }

                var primaryKeyOrders = primaryKeyRows
                    .Select(row => row.PrimaryKeyOrder.Value)
                    .Distinct()
                    .OrderBy(value => value)
                    .ToArray();
                if (primaryKeyRows.Length > 0 && !IsContiguousFromOne(primaryKeyOrders))
                {
                    AddFileError(
                        errors,
                        orderedRows,
                        "NON_CONTIGUOUS_PRIMARY_KEY_ORDER",
                        "primary_key_order must be contiguous from 1.");
                }
            }
        }

        private static bool IsContiguousFromOne(IReadOnlyList<int> values)
        {
            if (values.Count == 0)
            {
                return false;
            }

            for (var index = 0; index < values.Count; index++)
            {
                if (values[index] != index + 1)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryParsePositiveInteger(string rawValue, out int value)
        {
            return int.TryParse(
                       rawValue,
                       NumberStyles.None,
                       CultureInfo.InvariantCulture,
                       out value) &&
                   value > 0;
        }

        private static CsvColumnSchema ToColumnSchema(ParsedRow row)
        {
            return new CsvColumnSchema(
                row.Source.FileName,
                row.ColumnOrder,
                row.Source.ColumnName,
                row.DataType,
                row.IsRequired,
                row.PrimaryKeyOrder,
                row.Source.DefaultValue,
                row.AllowedValues,
                row.ForeignKey,
                row.Source.Description,
                row.Source.SourceRowNumber);
        }

        private static void AddFileError(
            ICollection<CsvSchemaCatalogError> errors,
            IReadOnlyList<ParsedRow> rows,
            string code,
            string message)
        {
            var firstRow = rows[0].Source;
            errors.Add(new CsvSchemaCatalogError(
                code,
                message,
                firstRow.SourceRowNumber,
                firstRow.FileName,
                string.Empty));
        }

        private static void AddError(
            ICollection<CsvSchemaCatalogError> errors,
            CsvSchemaDictionaryRow row,
            string code,
            string message)
        {
            errors.Add(new CsvSchemaCatalogError(
                code,
                message,
                row.SourceRowNumber,
                row.FileName,
                row.ColumnName));
        }

        private sealed class ParsedRow
        {
            public ParsedRow(
                CsvSchemaDictionaryRow source,
                int columnOrder,
                CsvSchemaDataType dataType,
                bool isRequired,
                int? primaryKeyOrder,
                string[] allowedValues,
                CsvForeignKeyReference foreignKey)
            {
                Source = source;
                ColumnOrder = columnOrder;
                DataType = dataType;
                IsRequired = isRequired;
                PrimaryKeyOrder = primaryKeyOrder;
                AllowedValues = allowedValues;
                ForeignKey = foreignKey;
            }

            public CsvSchemaDictionaryRow Source { get; }

            public int ColumnOrder { get; }

            public CsvSchemaDataType DataType { get; }

            public bool IsRequired { get; }

            public int? PrimaryKeyOrder { get; }

            public string[] AllowedValues { get; }

            public CsvForeignKeyReference ForeignKey { get; }
        }
    }
}
