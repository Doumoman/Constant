using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvForeignKeyReference
    {
        private CsvForeignKeyReference(string targetFileName, string targetColumnName)
        {
            TargetFileName = targetFileName;
            TargetColumnName = targetColumnName;
        }

        public string TargetFileName { get; }

        public string TargetColumnName { get; }

        public static bool TryParse(
            string rawValue,
            out CsvForeignKeyReference reference,
            out string error)
        {
            reference = null;
            error = null;

            if (string.IsNullOrEmpty(rawValue))
            {
                return true;
            }

            var separatorIndex = rawValue.LastIndexOf('.');
            if (separatorIndex <= 0 || separatorIndex == rawValue.Length - 1)
            {
                error = "Foreign key must use <target_file_name>.csv.<target_column_name>.";
                return false;
            }

            var targetFileName = rawValue.Substring(0, separatorIndex);
            var targetColumnName = rawValue.Substring(separatorIndex + 1);
            if (!targetFileName.EndsWith(".csv", StringComparison.Ordinal) ||
                targetFileName.Length == ".csv".Length ||
                string.IsNullOrWhiteSpace(targetColumnName) ||
                !targetFileName.Equals(targetFileName.Trim(), StringComparison.Ordinal) ||
                !targetColumnName.Equals(targetColumnName.Trim(), StringComparison.Ordinal))
            {
                error = "Foreign key must use <target_file_name>.csv.<target_column_name>.";
                return false;
            }

            reference = new CsvForeignKeyReference(targetFileName, targetColumnName);
            return true;
        }

        public override string ToString()
        {
            return TargetFileName + "." + TargetColumnName;
        }
    }
}
