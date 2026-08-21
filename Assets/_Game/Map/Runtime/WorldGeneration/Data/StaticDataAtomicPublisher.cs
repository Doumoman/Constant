using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class StaticDataAtomicPublisher
    {
        private const string PublisherStage = "PUBLISH";
        private readonly StaticDataRegistryStore store;

        public StaticDataAtomicPublisher(StaticDataRegistryStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public CsvImportReport Publish(StaticDataPublishRequest request)
        {
            lock (store.PublishGate)
            {
                var previous = store.Current;
                var issues = new List<CsvImportIssue>();
                var attemptId = request?.AttemptId;
                var candidateRegistry = request?.CandidateRegistry;
                var candidateHash = request?.CandidateContentHash;

                if (request == null)
                {
                    issues.Add(Error("MISSING_REQUEST", "The publish request is missing."));
                }
                else
                {
                    if (!IsWellFormedUtf16(attemptId))
                    {
                        attemptId = null;
                        issues.Add(Error(
                            "INVALID_ATTEMPT_ID",
                            "The attempt identifier is not valid UTF-16."));
                    }

                    if (!request.IssueSequenceWasSupplied)
                    {
                        issues.Add(Error(
                            "MISSING_ISSUE_SEQUENCE",
                            "The complete issue sequence is missing."));
                    }

                    AddValidatedIssues(request.Issues, issues);
                    if (request.CancellationRequested)
                    {
                        issues.Add(Error(
                            "CANCELLED",
                            "The publish attempt was marked as cancelled."));
                    }

                    if (candidateRegistry == null)
                    {
                        issues.Add(Error(
                            "MISSING_REGISTRY",
                            "The candidate registry is missing."));
                    }

                    if (candidateHash == null)
                    {
                        issues.Add(Error(
                            "MISSING_CONTENT_HASH",
                            "The candidate content hash is missing."));
                    }
                }

                issues.Sort(CsvImportIssue.Compare);
                if (issues.Any(IsError))
                {
                    return Failure(previous, candidateHash, attemptId, issues);
                }

                if (previous != null && previous.Version == long.MaxValue)
                {
                    issues.Add(Error(
                        "VERSION_OVERFLOW",
                        "The published snapshot version cannot be incremented."));
                    issues.Sort(CsvImportIssue.Compare);
                    return Failure(previous, candidateHash, attemptId, issues);
                }

                var nextVersion = previous == null ? 1 : previous.Version + 1;
                var next = new PublishedStaticDataSnapshot(
                    candidateRegistry,
                    candidateHash,
                    nextVersion);
                var report = new CsvImportReport(
                    attemptId,
                    true,
                    previous?.Version ?? 0,
                    next.Version,
                    previous?.ContentHash,
                    candidateHash,
                    next.ContentHash,
                    issues);

                try
                {
                    CsvImportReportJson.SerializeUtf8(report);
                }
                catch (Exception)
                {
                    issues.Add(Error(
                        "REPORT_SERIALIZATION_FAILED",
                        "The import report could not be serialized."));
                    issues.Sort(CsvImportIssue.Compare);
                    return Failure(previous, candidateHash, null, issues);
                }

                store.Exchange(next);
                return report;
            }
        }

        private static void AddValidatedIssues(
            IEnumerable<CsvImportIssue> source,
            ICollection<CsvImportIssue> destination)
        {
            var index = 0;
            foreach (var issue in source)
            {
                if (issue == null)
                {
                    destination.Add(Error(
                        "INVALID_ISSUE",
                        "Issue entry " + index + " is null."));
                }
                else if (!string.Equals(
                             issue.Severity,
                             CsvImportIssue.ErrorSeverity,
                             StringComparison.Ordinal) &&
                         !string.Equals(
                             issue.Severity,
                             CsvImportIssue.WarningSeverity,
                             StringComparison.Ordinal))
                {
                    destination.Add(Error(
                        "INVALID_ISSUE_SEVERITY",
                        "Issue entry " + index + " has an invalid severity."));
                }
                else if (string.IsNullOrEmpty(issue.Stage) ||
                         string.IsNullOrEmpty(issue.Code) ||
                         string.IsNullOrEmpty(issue.Message) ||
                         !AllStringsAreWellFormed(issue))
                {
                    destination.Add(Error(
                        "INVALID_ISSUE",
                        "Issue entry " + index + " is incomplete or contains invalid UTF-16."));
                }
                else
                {
                    destination.Add(issue);
                }

                index++;
            }
        }

        private static bool AllStringsAreWellFormed(CsvImportIssue issue)
        {
            return IsWellFormedUtf16(issue.Stage) &&
                   IsWellFormedUtf16(issue.Severity) &&
                   IsWellFormedUtf16(issue.Code) &&
                   IsWellFormedUtf16(issue.Message) &&
                   IsWellFormedUtf16(issue.SourceFile) &&
                   IsWellFormedUtf16(issue.SourceField) &&
                   IsWellFormedUtf16(issue.TargetFile) &&
                   IsWellFormedUtf16(issue.TargetColumn) &&
                   IsWellFormedUtf16(issue.TargetValue);
        }

        private static bool IsWellFormedUtf16(string value)
        {
            if (value == null) return true;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (char.IsHighSurrogate(character))
                {
                    if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                    {
                        return false;
                    }

                    index++;
                }
                else if (char.IsLowSurrogate(character))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsError(CsvImportIssue issue)
        {
            return string.Equals(
                issue.Severity,
                CsvImportIssue.ErrorSeverity,
                StringComparison.Ordinal);
        }

        private static CsvImportReport Failure(
            PublishedStaticDataSnapshot previous,
            ContentVersionHash candidateHash,
            string attemptId,
            IEnumerable<CsvImportIssue> issues)
        {
            return new CsvImportReport(
                attemptId,
                false,
                previous?.Version ?? 0,
                previous?.Version ?? 0,
                previous?.ContentHash,
                candidateHash,
                previous?.ContentHash,
                issues);
        }

        private static CsvImportIssue Error(string code, string message)
        {
            return new CsvImportIssue(
                PublisherStage,
                CsvImportIssue.ErrorSeverity,
                code,
                message);
        }
    }
}
