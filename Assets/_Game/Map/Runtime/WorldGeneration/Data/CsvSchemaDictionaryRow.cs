namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvSchemaDictionaryRow
    {
        public CsvSchemaDictionaryRow(
            string fileName,
            string columnOrder,
            string columnName,
            string dataType,
            string required,
            string primaryKeyOrder,
            string defaultValue,
            string allowedValues,
            string foreignKey,
            string description,
            int sourceRowNumber)
        {
            FileName = fileName;
            ColumnOrder = columnOrder;
            ColumnName = columnName;
            DataType = dataType;
            Required = required;
            PrimaryKeyOrder = primaryKeyOrder;
            DefaultValue = defaultValue;
            AllowedValues = allowedValues;
            ForeignKey = foreignKey;
            Description = description;
            SourceRowNumber = sourceRowNumber;
        }

        public string FileName { get; }

        public string ColumnOrder { get; }

        public string ColumnName { get; }

        public string DataType { get; }

        public string Required { get; }

        public string PrimaryKeyOrder { get; }

        public string DefaultValue { get; }

        public string AllowedValues { get; }

        public string ForeignKey { get; }

        public string Description { get; }

        public int SourceRowNumber { get; }
    }
}
