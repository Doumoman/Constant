using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalRegionValidationReport
    {
        private readonly IReadOnlyList<OptionalRegionValidationIssue> issues;

        internal OptionalRegionValidationReport(
            OptionalRegionValidationStatus status,
            OptionalRegionValidationDiagnostics diagnostics,
            IEnumerable<OptionalRegionValidationIssue> sourceIssues,
            string sourceMandatoryGraphDigest,
            string sourceGrowthDigest,
            string sourceType0AssignmentDigest,
            string sourceAccessAssignmentDigest,
            string sourceRewardTierDigest,
            string sourceReturnPolicyDigest,
            string sourceInactiveAssignmentDigest,
            string canonicalDigest)
        {
            if (!Enum.IsDefined(typeof(OptionalRegionValidationStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            if (sourceIssues == null) throw new ArgumentNullException(nameof(sourceIssues));

            var values = new List<OptionalRegionValidationIssue>();
            foreach (var issue in sourceIssues)
            {
                if (issue == null) throw new ArgumentException("Issues cannot contain null.", nameof(sourceIssues));
                values.Add(issue);
            }
            values.Sort(OptionalRegionValidationIssue.Compare);
            for (var index = values.Count - 1; index > 0; index--)
            {
                if (values[index].SameIdentity(values[index - 1])) values.RemoveAt(index);
            }
            if (values.Count != diagnostics.IssueCount)
                throw new ArgumentException("Issue count must match the deduplicated issue publication.", nameof(sourceIssues));

            var valid = status == OptionalRegionValidationStatus.Valid;
            if (valid)
            {
                if (values.Count != 0 || diagnostics.RngDrawCount != 0 || diagnostics.SourceMutationCount != 0 ||
                    !IsCanonicalIdentity(sourceMandatoryGraphDigest) ||
                    !IsLowerHexDigest(sourceGrowthDigest) ||
                    !IsLowerHexDigest(sourceType0AssignmentDigest) ||
                    !IsLowerHexDigest(sourceAccessAssignmentDigest) ||
                    !IsLowerHexDigest(sourceRewardTierDigest) ||
                    !IsLowerHexDigest(sourceReturnPolicyDigest) ||
                    !IsLowerHexDigest(sourceInactiveAssignmentDigest) ||
                    !IsLowerHexDigest(canonicalDigest))
                    throw new ArgumentException("Valid reports require a complete side-effect-free digest publication.");
            }
            else if (values.Count == 0 || !string.IsNullOrEmpty(canonicalDigest) ||
                     diagnostics.RngDrawCount != 0 || diagnostics.SourceMutationCount != 0)
            {
                throw new ArgumentException("Invalid reports must be atomic, digest-free, and side-effect free.");
            }

            Status = status;
            issues = new ReadOnlyCollection<OptionalRegionValidationIssue>(values);
            SourceMandatoryGraphDigest = sourceMandatoryGraphDigest ?? string.Empty;
            SourceGrowthDigest = sourceGrowthDigest ?? string.Empty;
            SourceType0AssignmentDigest = sourceType0AssignmentDigest ?? string.Empty;
            SourceAccessAssignmentDigest = sourceAccessAssignmentDigest ?? string.Empty;
            SourceRewardTierDigest = sourceRewardTierDigest ?? string.Empty;
            SourceReturnPolicyDigest = sourceReturnPolicyDigest ?? string.Empty;
            SourceInactiveAssignmentDigest = sourceInactiveAssignmentDigest ?? string.Empty;
            CanonicalDigest = canonicalDigest ?? string.Empty;
            RngDrawCount = diagnostics.RngDrawCount;
        }

        public OptionalRegionValidationStatus Status { get; }
        public OptionalRegionValidationDiagnostics Diagnostics { get; }
        public IReadOnlyList<OptionalRegionValidationIssue> Issues => issues;
        public string SourceMandatoryGraphDigest { get; }
        public string SourceGrowthDigest { get; }
        public string SourceType0AssignmentDigest { get; }
        public string SourceAccessAssignmentDigest { get; }
        public string SourceRewardTierDigest { get; }
        public string SourceReturnPolicyDigest { get; }
        public string SourceInactiveAssignmentDigest { get; }
        public string CanonicalDigest { get; }
        public int RngDrawCount { get; }
        public bool IsValid => Status == OptionalRegionValidationStatus.Valid;

        internal static bool IsLowerHexDigest(string value)
        {
            if (value == null || value.Length != 64) return false;
            foreach (var character in value)
            {
                if ((character < '0' || character > '9') &&
                    (character < 'a' || character > 'f')) return false;
            }
            return true;
        }

        internal static bool IsCanonicalIdentity(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                   string.Equals(value, value.Trim(), StringComparison.Ordinal);
        }
    }
}
