using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvFileSchema
    {
        private readonly ReadOnlyCollection<CsvColumnSchema> columns;
        private readonly ReadOnlyCollection<CsvColumnSchema> primaryKeyColumns;
        private readonly Dictionary<string, CsvColumnSchema> columnsByName;

        internal CsvFileSchema(string fileName, IEnumerable<CsvColumnSchema> sourceColumns)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));

            var orderedColumns = sourceColumns
                .OrderBy(column => column.ColumnOrder)
                .ToList();
            columns = new ReadOnlyCollection<CsvColumnSchema>(orderedColumns);
            primaryKeyColumns = new ReadOnlyCollection<CsvColumnSchema>(
                orderedColumns
                    .Where(column => column.PrimaryKeyOrder.HasValue)
                    .OrderBy(column => column.PrimaryKeyOrder.Value)
                    .ToList());
            columnsByName = orderedColumns.ToDictionary(
                column => column.ColumnName,
                column => column,
                StringComparer.Ordinal);
        }

        public string FileName { get; }

        public IReadOnlyList<CsvColumnSchema> Columns => columns;

        public IReadOnlyList<CsvColumnSchema> PrimaryKeyColumns => primaryKeyColumns;

        public bool TryGetColumn(string columnName, out CsvColumnSchema column)
        {
            return columnsByName.TryGetValue(columnName, out column);
        }

        public CsvColumnSchema GetColumn(string columnName)
        {
            if (!TryGetColumn(columnName, out var column))
            {
                throw new KeyNotFoundException(
                    "CSV column schema was not found: " + FileName + "." + columnName);
            }

            return column;
        }
    }
}
