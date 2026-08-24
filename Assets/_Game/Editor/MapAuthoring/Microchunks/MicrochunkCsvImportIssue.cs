using System;

namespace StarNight.MapAuthoring.Microchunks
{
    public enum MicrochunkCsvImportIssueSeverity
    {
        Warning,
        Error
    }

    public sealed class MicrochunkCsvImportIssue : IComparable<MicrochunkCsvImportIssue>
    {
        public string FileName { get; }
        public string SelectedMicrochunkId { get; }
        public int RowNumber { get; }
        public string ColumnName { get; }
        public string Code { get; }
        public string Message { get; }
        public MicrochunkCsvImportIssueSeverity Severity { get; }
        public bool IsError => Severity == MicrochunkCsvImportIssueSeverity.Error;

        public MicrochunkCsvImportIssue(
            string fileName,
            string selectedMicrochunkId,
            int rowNumber,
            string columnName,
            string code,
            string message,
            MicrochunkCsvImportIssueSeverity severity = MicrochunkCsvImportIssueSeverity.Error)
        {
            FileName = Require(fileName, nameof(fileName));
            SelectedMicrochunkId = Require(selectedMicrochunkId, nameof(selectedMicrochunkId));
            if (rowNumber < 0) throw new ArgumentOutOfRangeException(nameof(rowNumber));
            RowNumber = rowNumber;
            ColumnName = columnName ?? string.Empty;
            Code = Require(code, nameof(code));
            Message = message ?? string.Empty;
            if (!Enum.IsDefined(typeof(MicrochunkCsvImportIssueSeverity), severity))
            {
                throw new ArgumentOutOfRangeException(nameof(severity));
            }
            Severity = severity;
        }

        public int CompareTo(MicrochunkCsvImportIssue other)
        {
            if (other == null) return 1;
            var comparison = string.Compare(FileName, other.FileName, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(
                SelectedMicrochunkId,
                other.SelectedMicrochunkId,
                StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = RowNumber.CompareTo(other.RowNumber);
            if (comparison != 0) return comparison;
            comparison = string.Compare(ColumnName, other.ColumnName, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            return string.Compare(Code, other.Code, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return FileName + ":" + RowNumber + ":" + ColumnName + ":" + Code + ": " + Message;
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
