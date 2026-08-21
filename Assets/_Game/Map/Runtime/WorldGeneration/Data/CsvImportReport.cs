using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvImportReport
    {
        public const int CurrentSchemaVersion = 1;
        public const string FileName = "CsvImportReport.json";

        private readonly ReadOnlyCollection<CsvImportIssue> issues;

        internal CsvImportReport(
            string attemptId,
            bool published,
            long previousVersion,
            long currentVersion,
            ContentVersionHash previousContentHash,
            ContentVersionHash candidateContentHash,
            ContentVersionHash currentContentHash,
            IEnumerable<CsvImportIssue> sourceIssues)
        {
            SchemaVersion = CurrentSchemaVersion;
            AttemptId = attemptId;
            Published = published;
            PreviousVersion = previousVersion;
            CurrentVersion = currentVersion;
            PreviousContentHash = previousContentHash;
            CandidateContentHash = candidateContentHash;
            CurrentContentHash = currentContentHash;
            issues = new ReadOnlyCollection<CsvImportIssue>(
                new List<CsvImportIssue>(sourceIssues ??
                    throw new ArgumentNullException(nameof(sourceIssues))));
            ErrorCount = issues.Count(item =>
                string.Equals(item.Severity, CsvImportIssue.ErrorSeverity, StringComparison.Ordinal));
            WarningCount = issues.Count(item =>
                string.Equals(item.Severity, CsvImportIssue.WarningSeverity, StringComparison.Ordinal));
        }

        public int SchemaVersion { get; }
        public string AttemptId { get; }
        public bool Published { get; }
        public long PreviousVersion { get; }
        public long CurrentVersion { get; }
        public ContentVersionHash PreviousContentHash { get; }
        public ContentVersionHash CandidateContentHash { get; }
        public ContentVersionHash CurrentContentHash { get; }
        public int ErrorCount { get; }
        public int WarningCount { get; }
        public IReadOnlyList<CsvImportIssue> Issues => issues;
    }
}
