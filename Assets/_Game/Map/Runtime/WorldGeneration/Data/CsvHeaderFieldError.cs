using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvHeaderFieldError
    {
        internal CsvHeaderFieldError(
            string sourceName,
            string schemaFileName,
            CsvHeaderFieldErrorCode errorCode,
            string message,
            CsvSourceLocation location,
            string expectedValue,
            string actualValue)
        {
            SourceName = sourceName ?? throw new ArgumentNullException(nameof(sourceName));
            SchemaFileName = schemaFileName ?? throw new ArgumentNullException(nameof(schemaFileName));
            ErrorCode = errorCode;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Location = location;
            ExpectedValue = expectedValue ?? string.Empty;
            ActualValue = actualValue ?? string.Empty;
        }

        public string SourceName { get; }

        public string SchemaFileName { get; }

        public CsvHeaderFieldErrorCode ErrorCode { get; }

        public string Message { get; }

        public CsvSourceLocation Location { get; }

        public int RecordNumber => Location.RecordNumber;

        public int FieldNumber => Location.FieldNumber;

        public int PhysicalLine => Location.PhysicalLine;

        public int PhysicalColumn => Location.PhysicalColumn;

        public int CharOffset => Location.CharOffset;

        public string ExpectedValue { get; }

        public string ActualValue { get; }

        public override string ToString()
        {
            return SourceName + " schema=" + SchemaFileName + " (" + Location + ") " +
                   ErrorCode + ": " + Message + " expected='" + ExpectedValue +
                   "' actual='" + ActualValue + "'";
        }
    }
}
