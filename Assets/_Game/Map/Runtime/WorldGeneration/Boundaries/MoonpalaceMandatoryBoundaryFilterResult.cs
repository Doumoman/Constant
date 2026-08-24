using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceMandatoryBoundaryFilterResult
    {
        private readonly IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> acceptedCandidates;
        private readonly IReadOnlyDictionary<MoonpalaceMandatoryBoundaryFilterIssue, int>
            rejectionSummaryByReason;
        private readonly IReadOnlyList<MoonpalaceMandatoryBoundaryFilterIssue> issueList;

        internal MoonpalaceMandatoryBoundaryFilterResult(
            int originalCandidateCount,
            IEnumerable<MoonpalaceBoundaryCandidateDefinition> acceptedCandidates,
            IDictionary<MoonpalaceMandatoryBoundaryFilterIssue, int> rejectionSummaryByReason,
            IEnumerable<MoonpalaceMandatoryBoundaryFilterIssue> issueList,
            MoonpalaceBoundaryCandidateIndex filteredCandidateIndex)
        {
            if (originalCandidateCount < 0) throw new ArgumentOutOfRangeException(nameof(originalCandidateCount));
            if (acceptedCandidates == null) throw new ArgumentNullException(nameof(acceptedCandidates));
            if (rejectionSummaryByReason == null) throw new ArgumentNullException(nameof(rejectionSummaryByReason));
            if (issueList == null) throw new ArgumentNullException(nameof(issueList));

            var candidateCopy = acceptedCandidates.ToArray();
            if (candidateCopy.Any(candidate => candidate == null) || candidateCopy.Length > originalCandidateCount)
            {
                throw new ArgumentException("Accepted candidates must be non-null and within the original count.",
                    nameof(acceptedCandidates));
            }

            var rejectionCopy = new Dictionary<MoonpalaceMandatoryBoundaryFilterIssue, int>();
            foreach (var pair in rejectionSummaryByReason)
            {
                if ((pair.Key != MoonpalaceMandatoryBoundaryFilterIssue.ToolRequired &&
                     pair.Key != MoonpalaceMandatoryBoundaryFilterIssue.MandatoryRouteNotAllowed) ||
                    pair.Value <= 0)
                {
                    throw new ArgumentException("Rejection summaries require positive candidate rejection counts.",
                        nameof(rejectionSummaryByReason));
                }

                rejectionCopy.Add(pair.Key, pair.Value);
            }

            var issueCopy = issueList.ToArray();
            if (issueCopy.Any(issue => issue == MoonpalaceMandatoryBoundaryFilterIssue.None))
            {
                throw new ArgumentException("Issue lists cannot contain None.", nameof(issueList));
            }

            OriginalCandidateCount = originalCandidateCount;
            AcceptedCandidateCount = candidateCopy.Length;
            RejectedCandidateCount = originalCandidateCount - candidateCopy.Length;
            this.acceptedCandidates =
                new ReadOnlyCollection<MoonpalaceBoundaryCandidateDefinition>(candidateCopy);
            this.rejectionSummaryByReason =
                new ReadOnlyDictionary<MoonpalaceMandatoryBoundaryFilterIssue, int>(rejectionCopy);
            this.issueList = new ReadOnlyCollection<MoonpalaceMandatoryBoundaryFilterIssue>(issueCopy);
            FilteredCandidateIndex = filteredCandidateIndex;
        }

        public int OriginalCandidateCount { get; }
        public int AcceptedCandidateCount { get; }
        public int RejectedCandidateCount { get; }
        public IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> AcceptedCandidates => acceptedCandidates;
        public IReadOnlyDictionary<MoonpalaceMandatoryBoundaryFilterIssue, int>
            RejectionSummaryByReason => rejectionSummaryByReason;
        public IReadOnlyList<MoonpalaceMandatoryBoundaryFilterIssue> IssueList => issueList;
        public MoonpalaceBoundaryCandidateIndex FilteredCandidateIndex { get; }
        public bool IsSuccess => issueList.Count == 0;
    }
}
