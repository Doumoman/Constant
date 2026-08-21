using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class StaticDataPublishRequest
    {
        private readonly ReadOnlyCollection<CsvImportIssue> issues;

        public StaticDataPublishRequest(
            StaticDataRegistry candidateRegistry,
            ContentVersionHash candidateContentHash,
            IEnumerable<CsvImportIssue> sourceIssues,
            string attemptId = null,
            bool cancellationRequested = false)
        {
            CandidateRegistry = candidateRegistry;
            CandidateContentHash = candidateContentHash;
            AttemptId = attemptId;
            CancellationRequested = cancellationRequested;
            IssueSequenceWasSupplied = sourceIssues != null;
            issues = new ReadOnlyCollection<CsvImportIssue>(
                new List<CsvImportIssue>(sourceIssues ?? Array.Empty<CsvImportIssue>()));
        }

        public StaticDataRegistry CandidateRegistry { get; }
        public ContentVersionHash CandidateContentHash { get; }
        public IReadOnlyList<CsvImportIssue> Issues => issues;
        public string AttemptId { get; }
        public bool CancellationRequested { get; }

        internal bool IssueSequenceWasSupplied { get; }
    }
}
