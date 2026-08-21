using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.Editor.WorldGeneration.Data
{
    public sealed class CsvImportWindowState
    {
        public CsvImportWindowState(CsvImportSessionResult initialResult)
        {
            LastResult = initialResult ?? throw new ArgumentNullException(nameof(initialResult));
            Stage = initialResult.Stage;
            Progress = initialResult.Progress;
        }

        public bool IsRunning { get; private set; }
        public string Stage { get; private set; }
        public float Progress { get; private set; }
        public CsvImportSessionResult LastResult { get; private set; }

        public bool TryBeginRun()
        {
            if (IsRunning) return false;
            IsRunning = true;
            Stage = "STARTING";
            Progress = 0f;
            return true;
        }

        public void UpdateProgress(string stage, float progress)
        {
            if (!IsRunning) return;
            Stage = stage ?? string.Empty;
            Progress = Math.Max(0f, Math.Min(1f, progress));
        }

        public void Complete(CsvImportSessionResult result)
        {
            LastResult = result ?? throw new ArgumentNullException(nameof(result));
            Stage = result.Stage;
            Progress = result.Progress;
            IsRunning = false;
        }

        public static IReadOnlyList<CsvImportFileStatus> FilterFiles(
            IEnumerable<CsvImportFileStatus> files,
            string search,
            string severityFilter)
        {
            var query = files ?? Array.Empty<CsvImportFileStatus>();
            var normalized = search ?? string.Empty;
            return query.Where(file =>
                    MatchesFilter(file.State, severityFilter) &&
                    (normalized.Length == 0 ||
                     file.FileName.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) >= 0 ||
                     file.Category.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToArray();
        }

        public static IReadOnlyList<CsvImportIssue> FilterIssues(
            IEnumerable<CsvImportIssue> issues,
            string search,
            string severityFilter)
        {
            var query = issues ?? Array.Empty<CsvImportIssue>();
            var normalized = search ?? string.Empty;
            return query.Where(issue =>
                    MatchesFilter(issue.Severity, severityFilter) &&
                    (normalized.Length == 0 ||
                     Contains(issue.SourceFile, normalized) ||
                     Contains(issue.Message, normalized) ||
                     Contains(issue.Code, normalized) ||
                     Contains(issue.TargetFile, normalized)))
                .ToArray();
        }

        private static bool MatchesFilter(string severity, string filter)
        {
            return string.IsNullOrEmpty(filter) ||
                   string.Equals(filter, "ALL", StringComparison.Ordinal) ||
                   string.Equals(severity, filter, StringComparison.Ordinal);
        }

        private static bool Contains(string value, string search)
        {
            return value != null &&
                   value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
