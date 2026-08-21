using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.Editor.WorldGeneration.Data
{
    public sealed class CsvImportSessionResult
    {
        private readonly ReadOnlyCollection<CsvImportFileStatus> files;
        private readonly ReadOnlyCollection<CsvImportIssue> issues;

        public CsvImportSessionResult(
            IEnumerable<CsvImportFileStatus> sourceFiles,
            IEnumerable<CsvImportIssue> sourceIssues,
            CsvImportReport publishReport,
            ForeignKeyRecordIndex recordIndex,
            string stage,
            float progress,
            string reportProjectRelativePath,
            bool reportWriteSucceeded,
            string reportWriteError)
        {
            files = new ReadOnlyCollection<CsvImportFileStatus>(
                new List<CsvImportFileStatus>(sourceFiles ??
                    throw new ArgumentNullException(nameof(sourceFiles))));
            issues = new ReadOnlyCollection<CsvImportIssue>(
                new List<CsvImportIssue>(sourceIssues ??
                    throw new ArgumentNullException(nameof(sourceIssues))));
            PublishReport = publishReport;
            RecordIndex = recordIndex;
            Stage = stage ?? string.Empty;
            Progress = progress;
            ReportProjectRelativePath = reportProjectRelativePath ?? string.Empty;
            ReportWriteSucceeded = reportWriteSucceeded;
            ReportWriteError = reportWriteError ?? string.Empty;
            ErrorCount = issues.Count(item => string.Equals(
                item.Severity, CsvImportIssue.ErrorSeverity, StringComparison.Ordinal));
            WarningCount = issues.Count(item => string.Equals(
                item.Severity, CsvImportIssue.WarningSeverity, StringComparison.Ordinal));
        }

        public IReadOnlyList<CsvImportFileStatus> Files => files;
        public IReadOnlyList<CsvImportIssue> Issues => issues;
        public CsvImportReport PublishReport { get; }
        public ForeignKeyRecordIndex RecordIndex { get; }
        public string Stage { get; }
        public float Progress { get; }
        public int ErrorCount { get; }
        public int WarningCount { get; }
        public string ReportProjectRelativePath { get; }
        public bool ReportWriteSucceeded { get; }
        public string ReportWriteError { get; }
        public bool Published => PublishReport != null && PublishReport.Published;
        public long PreviousVersion => PublishReport?.PreviousVersion ?? 0;
        public long CurrentVersion => PublishReport?.CurrentVersion ?? 0;
        public string CandidateContentHash => PublishReport?.CandidateContentHash?.Hex ?? string.Empty;
        public string CurrentContentHash => PublishReport?.CurrentContentHash?.Hex ?? string.Empty;
    }
}
