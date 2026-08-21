using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvImportIssue
    {
        public const string ErrorSeverity = "ERROR";
        public const string WarningSeverity = "WARNING";

        public CsvImportIssue(
            string stage,
            string severity,
            string code,
            string message,
            string sourceFile = null,
            int? recordNumber = null,
            string sourceField = null,
            int? line = null,
            int? column = null,
            int? offset = null,
            string targetFile = null,
            string targetColumn = null,
            string targetValue = null)
        {
            Stage = stage;
            Severity = severity;
            Code = code;
            Message = message;
            SourceFile = sourceFile;
            RecordNumber = recordNumber;
            SourceField = sourceField;
            Line = line;
            Column = column;
            Offset = offset;
            TargetFile = targetFile;
            TargetColumn = targetColumn;
            TargetValue = targetValue;
        }

        public string Stage { get; }
        public string Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public string SourceFile { get; }
        public int? RecordNumber { get; }
        public string SourceField { get; }
        public int? Line { get; }
        public int? Column { get; }
        public int? Offset { get; }
        public string TargetFile { get; }
        public string TargetColumn { get; }
        public string TargetValue { get; }

        internal static int Compare(CsvImportIssue left, CsvImportIssue right)
        {
            var comparison = SeverityRank(left.Severity).CompareTo(SeverityRank(right.Severity));
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(left.Stage, right.Stage);
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(left.SourceFile, right.SourceFile);
            if (comparison != 0) return comparison;
            comparison = CompareNullable(left.RecordNumber, right.RecordNumber);
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(left.SourceField, right.SourceField);
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(left.TargetFile, right.TargetFile);
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(left.TargetColumn, right.TargetColumn);
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(left.TargetValue, right.TargetValue);
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(left.Code, right.Code);
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(left.Message, right.Message);
            if (comparison != 0) return comparison;
            comparison = CompareNullable(left.Line, right.Line);
            if (comparison != 0) return comparison;
            comparison = CompareNullable(left.Column, right.Column);
            return comparison != 0 ? comparison : CompareNullable(left.Offset, right.Offset);
        }

        private static int SeverityRank(string severity)
        {
            return string.Equals(severity, ErrorSeverity, StringComparison.Ordinal) ? 0 : 1;
        }

        private static int CompareNullable(int? left, int? right)
        {
            if (!left.HasValue) return right.HasValue ? -1 : 0;
            return right.HasValue ? left.Value.CompareTo(right.Value) : 1;
        }
    }
}
