using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public enum ContentVersionHashErrorCode
    {
        MissingRegistry,
        MissingSourceSet,
        CatalogMismatch,
        SourceInventoryMismatch,
        RecordIdentityMismatch,
        SchemaMismatch,
        UnsupportedValue,
        DuplicateCanonicalPrimaryKey
    }

    public sealed class ContentVersionHashError
    {
        internal ContentVersionHashError(
            ContentVersionHashErrorCode errorCode,
            string message,
            string fileName = null,
            int? recordNumber = null,
            string fieldName = null,
            CsvSourceLocation? sourceLocation = null)
        {
            ErrorCode = errorCode;
            Message = message ?? string.Empty;
            FileName = fileName ?? string.Empty;
            RecordNumber = recordNumber;
            FieldName = fieldName ?? string.Empty;
            SourceLocation = sourceLocation;
        }

        public ContentVersionHashErrorCode ErrorCode { get; }
        public string Message { get; }
        public string FileName { get; }
        public int? RecordNumber { get; }
        public string FieldName { get; }
        public CsvSourceLocation? SourceLocation { get; }

        internal static int Compare(ContentVersionHashError left, ContentVersionHashError right)
        {
            var comparison = StringComparer.Ordinal.Compare(left.FileName, right.FileName);
            if (comparison != 0) return comparison;
            comparison = CompareNullable(left.RecordNumber, right.RecordNumber);
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(left.FieldName, right.FieldName);
            if (comparison != 0) return comparison;
            comparison = CompareNullable(
                left.SourceLocation.HasValue ? left.SourceLocation.Value.PhysicalLine : (int?)null,
                right.SourceLocation.HasValue ? right.SourceLocation.Value.PhysicalLine : (int?)null);
            return comparison != 0 ? comparison : left.ErrorCode.CompareTo(right.ErrorCode);
        }

        private static int CompareNullable(int? left, int? right)
        {
            if (!left.HasValue) return right.HasValue ? -1 : 0;
            return right.HasValue ? left.Value.CompareTo(right.Value) : 1;
        }
    }
}
