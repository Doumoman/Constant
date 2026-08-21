using System;

namespace StarNight.Map.Editor.WorldGeneration.Data
{
    public sealed class CsvImportFileStatus
    {
        public const string NotRun = "NOT_RUN";
        public const string Success = "SUCCESS";
        public const string Warning = "WARNING";
        public const string Error = "ERROR";

        public CsvImportFileStatus(
            string fileName,
            string category,
            string projectRelativePath,
            string state,
            long byteCount,
            int rowCount,
            int errorCount,
            int warningCount,
            string rawSha256,
            bool hadUtf8Bom)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Category = category ?? string.Empty;
            ProjectRelativePath = projectRelativePath ?? string.Empty;
            State = state ?? throw new ArgumentNullException(nameof(state));
            RawSha256 = rawSha256 ?? string.Empty;
            ByteCount = byteCount;
            RowCount = rowCount;
            ErrorCount = errorCount;
            WarningCount = warningCount;
            HadUtf8Bom = hadUtf8Bom;
        }

        public string FileName { get; }
        public string Category { get; }
        public string ProjectRelativePath { get; }
        public string State { get; }
        public long ByteCount { get; }
        public int RowCount { get; }
        public int ErrorCount { get; }
        public int WarningCount { get; }
        public string RawSha256 { get; }
        public bool HadUtf8Bom { get; }
    }
}
