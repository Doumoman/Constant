using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public enum CsvValueParseErrorCode
    {
        InvalidId,
        InvalidInteger,
        InvalidUnsignedInteger,
        InvalidFloat,
        InvalidBoolean,
        InvalidEnum,
        InvalidHex,
        InvalidDateTime,
        EmptyListItem,
        InvalidListItem,
    }

    public sealed class CsvValueParseError
    {
        private readonly ReadOnlyCollection<string> allowedValues;

        internal CsvValueParseError(
            string sourceName,
            string schemaFileName,
            CsvColumnSchema schema,
            CsvValueParseErrorCode errorCode,
            string message,
            CsvSourceLocation location,
            string effectiveValue,
            int? listItemIndex,
            string listItemValue,
            IEnumerable<string> sourceAllowedValues)
        {
            SourceName = sourceName ?? throw new ArgumentNullException(nameof(sourceName));
            SchemaFileName = schemaFileName ??
                             throw new ArgumentNullException(nameof(schemaFileName));
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            ColumnName = schema.ColumnName;
            ColumnOrder = schema.ColumnOrder;
            DataType = schema.DataType;
            ErrorCode = errorCode;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Location = location;
            EffectiveValue = effectiveValue ??
                             throw new ArgumentNullException(nameof(effectiveValue));
            ListItemIndex = listItemIndex;
            ListItemValue = listItemValue ?? string.Empty;
            allowedValues = new ReadOnlyCollection<string>(
                new List<string>(sourceAllowedValues ?? Array.Empty<string>()));
        }

        public string SourceName { get; }

        public string SchemaFileName { get; }

        public string ColumnName { get; }

        public CsvSchemaDataType DataType { get; }

        public CsvValueParseErrorCode ErrorCode { get; }

        public string Message { get; }

        public CsvSourceLocation Location { get; }

        public int RecordNumber => Location.RecordNumber;

        public int FieldNumber => Location.FieldNumber;

        public int PhysicalLine => Location.PhysicalLine;

        public int PhysicalColumn => Location.PhysicalColumn;

        public int CharOffset => Location.CharOffset;

        public string EffectiveValue { get; }

        public int? ListItemIndex { get; }

        public string ListItemValue { get; }

        public IReadOnlyList<string> AllowedValues => allowedValues;

        internal int ColumnOrder { get; }

        internal static int Compare(CsvValueParseError left, CsvValueParseError right)
        {
            var comparison = left.RecordNumber.CompareTo(right.RecordNumber);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.ColumnOrder.CompareTo(right.ColumnOrder);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = Nullable.Compare(left.ListItemIndex, right.ListItemIndex);
            return comparison != 0
                ? comparison
                : left.ErrorCode.CompareTo(right.ErrorCode);
        }
    }
}
