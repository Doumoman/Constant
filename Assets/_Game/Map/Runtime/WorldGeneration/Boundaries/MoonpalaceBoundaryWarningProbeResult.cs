using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryWarningProbeResult
    {
        private readonly IReadOnlyList<MoonpalaceBoundaryWarningMarkerCategory> observedMarkerCategories;
        private readonly IReadOnlyList<MoonpalaceBoundaryWarningIssue> issueList;

        internal MoonpalaceBoundaryWarningProbeResult(
            MoonpalaceBoundaryWarningProbeRequest probeRequest,
            bool accepted,
            IEnumerable<MoonpalaceBoundaryWarningMarkerCategory> observedMarkerCategories,
            IEnumerable<MoonpalaceBoundaryWarningIssue> issueList)
        {
            if (observedMarkerCategories == null) throw new ArgumentNullException(nameof(observedMarkerCategories));
            if (issueList == null) throw new ArgumentNullException(nameof(issueList));

            ProbeRequest = probeRequest;
            Accepted = accepted;
            var observedCopy = observedMarkerCategories.ToArray();
            var issueCopy = issueList.ToArray();
            this.observedMarkerCategories =
                new ReadOnlyCollection<MoonpalaceBoundaryWarningMarkerCategory>(observedCopy);
            this.issueList = new ReadOnlyCollection<MoonpalaceBoundaryWarningIssue>(issueCopy);
        }

        public MoonpalaceBoundaryWarningProbeRequest ProbeRequest { get; }
        public MoonpalaceBoundaryResolveRequest ResolveRequest => ProbeRequest?.ResolveRequest;
        public MoonpalaceBoundaryCandidateDefinition Candidate => ProbeRequest?.Candidate;
        public MoonpalaceBoundaryWarningRequirement WarningRequirement => ProbeRequest?.WarningRequirement;
        public MoonpalaceBiomeId TargetBiome => ProbeRequest == null ? default : ProbeRequest.TargetBiome;
        public bool Accepted { get; }
        public bool IsSuccess => Accepted;
        public int WarningMicrochunkCount => ProbeRequest?.WarningMicrochunkCount ?? 0;
        public int RequiredWarningMicrochunks => WarningRequirement?.WarningMicrochunksMinimum ?? 0;
        public int ObservedDistinctMarkerCategoryCount => observedMarkerCategories.Count;
        public int RequiredDistinctMarkerCategoryCount =>
            WarningRequirement?.RequiredDistinctMarkerCategories ?? 0;
        public IReadOnlyList<MoonpalaceBoundaryWarningMarkerCategory> ObservedMarkerCategories =>
            observedMarkerCategories;
        public int MissingMarkerCategoryCount => Math.Max(
            0,
            RequiredDistinctMarkerCategoryCount - ObservedDistinctMarkerCategoryCount);
        public IReadOnlyList<MoonpalaceBoundaryWarningIssue> IssueList => issueList;
    }
}
