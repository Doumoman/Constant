using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public enum StaticDataRegistryBuildErrorCode
    {
        MissingDefinitionSet,
        UnsuccessfulForeignKeyResolution,
        DefinitionRecordMissingFromIndex,
        ForeignKeyGraphMismatch,
        DuplicateTypedDefinitionIdentity
    }

    public sealed class StaticDataRegistryBuildError
    {
        internal StaticDataRegistryBuildError(
            StaticDataRegistryBuildErrorCode errorCode,
            string message,
            string fileName = null,
            int? recordNumber = null,
            string definitionType = null,
            CsvSourceLocation? sourceLocation = null)
        {
            ErrorCode = errorCode;
            Message = message ?? string.Empty;
            FileName = fileName ?? string.Empty;
            RecordNumber = recordNumber;
            DefinitionType = definitionType ?? string.Empty;
            SourceLocation = sourceLocation;
        }

        public StaticDataRegistryBuildErrorCode ErrorCode { get; }
        public string Message { get; }
        public string FileName { get; }
        public int? RecordNumber { get; }
        public string DefinitionType { get; }
        public CsvSourceLocation? SourceLocation { get; }

        internal static int Compare(
            StaticDataRegistryBuildError left,
            StaticDataRegistryBuildError right)
        {
            var comparison = StringComparer.Ordinal.Compare(left.FileName, right.FileName);
            if (comparison != 0) return comparison;
            comparison = CompareNullable(left.RecordNumber, right.RecordNumber);
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(left.DefinitionType, right.DefinitionType);
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
