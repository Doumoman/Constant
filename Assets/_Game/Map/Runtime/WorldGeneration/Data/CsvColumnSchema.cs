using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvColumnSchema
    {
        private readonly ReadOnlyCollection<string> allowedValues;

        internal CsvColumnSchema(
            string fileName,
            int columnOrder,
            string columnName,
            CsvSchemaDataType dataType,
            bool isRequired,
            int? primaryKeyOrder,
            string defaultValue,
            IEnumerable<string> allowedValues,
            CsvForeignKeyReference foreignKey,
            string description,
            int sourceRowNumber)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            ColumnOrder = columnOrder;
            ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
            DataType = dataType;
            IsRequired = isRequired;
            PrimaryKeyOrder = primaryKeyOrder;
            DefaultValue = defaultValue ?? string.Empty;
            this.allowedValues = new ReadOnlyCollection<string>(
                new List<string>(allowedValues ?? Array.Empty<string>()));
            ForeignKey = foreignKey;
            Description = description ?? string.Empty;
            SourceRowNumber = sourceRowNumber;
        }

        public string FileName { get; }

        public int ColumnOrder { get; }

        public string ColumnName { get; }

        public CsvSchemaDataType DataType { get; }

        public bool IsRequired { get; }

        public int? PrimaryKeyOrder { get; }

        public string DefaultValue { get; }

        public IReadOnlyList<string> AllowedValues => allowedValues;

        public CsvForeignKeyReference ForeignKey { get; }

        public string Description { get; }

        public int SourceRowNumber { get; }
    }
}
