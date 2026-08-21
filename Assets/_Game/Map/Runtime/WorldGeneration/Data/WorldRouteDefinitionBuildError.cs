using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public enum WorldRouteDefinitionBuildErrorCode
    {
        MissingSource,
        UnexpectedSource,
        DuplicateSource,
        UnsuccessfulParse,
        SchemaMismatch,
        FieldMappingFailed
    }

    public sealed class WorldRouteDefinitionBuildError
    {
        internal WorldRouteDefinitionBuildError(
            string fileName,
            WorldRouteDefinitionBuildErrorCode errorCode,
            string message,
            int? recordNumber = null,
            int? columnOrder = null,
            string columnName = null,
            CsvSourceLocation? location = null)
        {
            FileName = fileName ?? string.Empty;
            ErrorCode = errorCode;
            Message = message ?? string.Empty;
            RecordNumber = recordNumber;
            ColumnOrder = columnOrder;
            ColumnName = columnName ?? string.Empty;
            Location = location;
        }

        public string FileName { get; }
        public WorldRouteDefinitionBuildErrorCode ErrorCode { get; }
        public string Message { get; }
        public int? RecordNumber { get; }
        public int? ColumnOrder { get; }
        public string ColumnName { get; }
        public CsvSourceLocation? Location { get; }
        public int? FieldNumber => Location?.FieldNumber;
        public int? PhysicalLine => Location?.PhysicalLine;
        public int? PhysicalColumn => Location?.PhysicalColumn;
        public int? CharOffset => Location?.CharOffset;

        internal static int Compare(
            WorldRouteDefinitionBuildError left,
            WorldRouteDefinitionBuildError right)
        {
            var result = string.Compare(left.FileName, right.FileName, StringComparison.Ordinal);
            if (result != 0)
            {
                return result;
            }

            result = CompareNullable(left.RecordNumber, right.RecordNumber);
            if (result != 0)
            {
                return result;
            }

            result = CompareNullable(left.ColumnOrder, right.ColumnOrder);
            return result != 0 ? result : left.ErrorCode.CompareTo(right.ErrorCode);
        }

        private static int CompareNullable(int? left, int? right)
        {
            if (!left.HasValue)
            {
                return right.HasValue ? -1 : 0;
            }

            return right.HasValue ? left.Value.CompareTo(right.Value) : 1;
        }
    }
}
