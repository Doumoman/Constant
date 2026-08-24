using System;

namespace StarNight.MapAuthoring.Microchunks
{
    public enum MicrochunkCsvExportIssueSeverity
    {
        Warning,
        Error
    }

    public sealed class MicrochunkCsvExportIssue : IComparable<MicrochunkCsvExportIssue>
    {
        public string FileName { get; }
        public string SelectedMicrochunkId { get; }
        public string ColumnName { get; }
        public string Code { get; }
        public string Message { get; }
        public MicrochunkCsvExportIssueSeverity Severity { get; }
        public bool IsError => Severity == MicrochunkCsvExportIssueSeverity.Error;

        public MicrochunkCsvExportIssue(
            string fileName,
            string selectedMicrochunkId,
            string columnName,
            string code,
            string message,
            MicrochunkCsvExportIssueSeverity severity = MicrochunkCsvExportIssueSeverity.Error)
        {
            FileName = Require(fileName, nameof(fileName));
            SelectedMicrochunkId = Require(selectedMicrochunkId, nameof(selectedMicrochunkId));
            ColumnName = columnName ?? string.Empty;
            Code = Require(code, nameof(code));
            Message = message ?? string.Empty;
            if (!Enum.IsDefined(typeof(MicrochunkCsvExportIssueSeverity), severity))
            {
                throw new ArgumentOutOfRangeException(nameof(severity));
            }

            Severity = severity;
        }

        public int CompareTo(MicrochunkCsvExportIssue other)
        {
            if (other == null) return 1;
            var comparison = string.Compare(FileName, other.FileName, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(
                SelectedMicrochunkId,
                other.SelectedMicrochunkId,
                StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(ColumnName, other.ColumnName, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            return string.Compare(Code, other.Code, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return FileName + ":" + ColumnName + ":" + Code + ": " + Message;
        }

        private static string Require(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A non-blank canonical value is required.", parameterName);
            }

            return value;
        }
    }
}
