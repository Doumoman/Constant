using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvSchemaCatalogError
    {
        public CsvSchemaCatalogError(
            string code,
            string message,
            int sourceRowNumber,
            string fileName,
            string columnName)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            SourceRowNumber = sourceRowNumber;
            FileName = fileName ?? string.Empty;
            ColumnName = columnName ?? string.Empty;
        }

        public string Code { get; }

        public string Message { get; }

        public int SourceRowNumber { get; }

        public string FileName { get; }

        public string ColumnName { get; }

        internal static int Compare(CsvSchemaCatalogError left, CsvSchemaCatalogError right)
        {
            var comparison = StringComparer.Ordinal.Compare(left.FileName, right.FileName);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = StringComparer.Ordinal.Compare(left.ColumnName, right.ColumnName);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.SourceRowNumber.CompareTo(right.SourceRowNumber);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = StringComparer.Ordinal.Compare(left.Code, right.Code);
            return comparison != 0
                ? comparison
                : StringComparer.Ordinal.Compare(left.Message, right.Message);
        }

        public override string ToString()
        {
            return Code + " row=" + SourceRowNumber + " file=" + FileName +
                   " column=" + ColumnName + ": " + Message;
        }
    }
}
