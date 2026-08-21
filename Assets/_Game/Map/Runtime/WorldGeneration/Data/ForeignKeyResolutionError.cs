using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public enum ForeignKeyResolutionErrorCode
    {
        MissingSource,
        UnexpectedSource,
        DuplicateSource,
        UnsuccessfulParse,
        SchemaMismatch,
        InvalidForeignKeyDeclaration,
        MissingTargetRecord
    }

    public sealed class ForeignKeyResolutionError
    {
        internal ForeignKeyResolutionError(
            ForeignKeyResolutionErrorCode errorCode,
            string message,
            string sourceFileName,
            int? sourceRecordNumber = null,
            int? sourceColumnOrder = null,
            string sourceColumnName = null,
            CsvSourceLocation? sourceLocation = null,
            int? listIndex = null,
            string rawValue = null,
            string targetFileName = null,
            string targetColumnName = null,
            string targetValue = null,
            ForeignKeyRecordIdentity sourceIdentity = null,
            CsvParsedField sourceField = null)
        {
            ErrorCode = errorCode;
            Message = message ?? string.Empty;
            SourceFileName = sourceFileName ?? string.Empty;
            SourceRecordNumber = sourceRecordNumber;
            SourceColumnOrder = sourceColumnOrder;
            SourceColumnName = sourceColumnName ?? string.Empty;
            SourceLocation = sourceLocation;
            ListIndex = listIndex;
            RawValue = rawValue ?? string.Empty;
            TargetFileName = targetFileName ?? string.Empty;
            TargetColumnName = targetColumnName ?? string.Empty;
            TargetValue = targetValue ?? string.Empty;
            SourceIdentity = sourceIdentity;
            SourceField = sourceField;
        }

        public ForeignKeyResolutionErrorCode ErrorCode { get; }
        public string Message { get; }
        public string SourceFileName { get; }
        public int? SourceRecordNumber { get; }
        public int? SourceColumnOrder { get; }
        public string SourceColumnName { get; }
        public CsvSourceLocation? SourceLocation { get; }
        public int? ListIndex { get; }
        public string RawValue { get; }
        public string TargetFileName { get; }
        public string TargetColumnName { get; }
        public string TargetValue { get; }
        public ForeignKeyRecordIdentity SourceIdentity { get; }
        public CsvParsedField SourceField { get; }

        internal static int Compare(
            ForeignKeyResolutionError left,
            ForeignKeyResolutionError right)
        {
            var comparison = StringComparer.Ordinal.Compare(
                left.SourceFileName,
                right.SourceFileName);
            if (comparison != 0) return comparison;
            comparison = CompareNullable(left.SourceRecordNumber, right.SourceRecordNumber);
            if (comparison != 0) return comparison;
            comparison = CompareNullable(left.SourceColumnOrder, right.SourceColumnOrder);
            if (comparison != 0) return comparison;
            comparison = CompareNullable(left.ListIndex, right.ListIndex);
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(
                left.TargetFileName,
                right.TargetFileName);
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(
                left.TargetColumnName,
                right.TargetColumnName);
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(
                left.TargetValue,
                right.TargetValue);
            return comparison != 0
                ? comparison
                : left.ErrorCode.CompareTo(right.ErrorCode);
        }

        private static int CompareNullable(int? left, int? right)
        {
            if (!left.HasValue) return right.HasValue ? -1 : 0;
            return right.HasValue ? left.Value.CompareTo(right.Value) : 1;
        }
    }
}
