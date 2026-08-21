using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class UpDownConflictResolutionPlan
    {
        private readonly IReadOnlyList<UpDownConflictCandidate> candidates;
        private readonly IReadOnlyList<UpDownConflictResolution> resolutions;
        private readonly IReadOnlyDictionary<UpDownConflictId, UpDownConflictCandidate> candidatesById;
        private readonly IReadOnlyDictionary<UpDownConflictId, UpDownConflictResolution> resolutionsById;

        internal UpDownConflictResolutionPlan(
            VerticalGatewayPlan sourceVerticalGatewayPlan,
            MandatoryRouteMaskLookup sourceRouteMaskLookup,
            SiteReservationSnapshot sourceSiteSnapshot,
            BiomePatchValidationPublication sourceBiomePublication,
            IEnumerable<UpDownConflictCandidate> sourceCandidates,
            IEnumerable<UpDownConflictResolution> sourceResolutions)
        {
            SourceVerticalGatewayPlan = sourceVerticalGatewayPlan ?? throw new ArgumentNullException(nameof(sourceVerticalGatewayPlan));
            SourceRouteMaskLookup = sourceRouteMaskLookup ?? throw new ArgumentNullException(nameof(sourceRouteMaskLookup));
            SourceSiteSnapshot = sourceSiteSnapshot ?? throw new ArgumentNullException(nameof(sourceSiteSnapshot));
            SourceBiomePublication = sourceBiomePublication ?? throw new ArgumentNullException(nameof(sourceBiomePublication));
            var candidateValues = new List<UpDownConflictCandidate>(sourceCandidates ?? throw new ArgumentNullException(nameof(sourceCandidates)));
            var resolutionValues = new List<UpDownConflictResolution>(sourceResolutions ?? throw new ArgumentNullException(nameof(sourceResolutions)));
            candidateValues.Sort((left, right) => left.ConflictId.CompareTo(right.ConflictId));
            resolutionValues.Sort((left, right) => left.ConflictId.CompareTo(right.ConflictId));
            var candidateMap = new Dictionary<UpDownConflictId, UpDownConflictCandidate>();
            foreach (var candidate in candidateValues)
                if (candidate == null || !candidateMap.TryAdd(candidate.ConflictId, candidate)) throw new ArgumentException("Candidate IDs must be unique.", nameof(sourceCandidates));
            var resolutionMap = new Dictionary<UpDownConflictId, UpDownConflictResolution>();
            var totalCost = 0;
            foreach (var resolution in resolutionValues)
            {
                if (resolution == null || !candidateMap.TryGetValue(resolution.ConflictId, out var candidate) || candidate.CanBeType4 ||
                    !resolutionMap.TryAdd(resolution.ConflictId, resolution))
                    throw new ArgumentException("Resolutions must uniquely reference non-Type4 candidates.", nameof(sourceResolutions));
                totalCost = checked(totalCost + resolution.CheckedCost);
            }
            candidates = new ReadOnlyCollection<UpDownConflictCandidate>(candidateValues);
            resolutions = new ReadOnlyCollection<UpDownConflictResolution>(resolutionValues);
            candidatesById = new ReadOnlyDictionary<UpDownConflictId, UpDownConflictCandidate>(candidateMap);
            resolutionsById = new ReadOnlyDictionary<UpDownConflictId, UpDownConflictResolution>(resolutionMap);
            Type4ExpressibleCount = candidateValues.FindAll(value => value.CanBeType4).Count;
            ConflictCount = candidateValues.FindAll(value => value.IsConflict).Count;
            ResolvedCount = resolutionValues.Count;
            UnresolvedCount = ConflictCount - ResolvedCount;
            TotalCost = totalCost;
        }

        public VerticalGatewayPlan SourceVerticalGatewayPlan { get; }
        public MandatoryRouteMaskLookup SourceRouteMaskLookup { get; }
        public SiteReservationSnapshot SourceSiteSnapshot { get; }
        public BiomePatchValidationPublication SourceBiomePublication { get; }
        public IReadOnlyList<UpDownConflictCandidate> Candidates => candidates;
        public IReadOnlyList<UpDownConflictResolution> Resolutions => resolutions;
        public int CandidateCount => candidates.Count;
        public int ConflictCount { get; }
        public int ResolvedCount { get; }
        public int Type4ExpressibleCount { get; }
        public int UnresolvedCount { get; }
        public int TotalCost { get; }
        public bool TryGetCandidate(UpDownConflictId id, out UpDownConflictCandidate candidate) => candidatesById.TryGetValue(id, out candidate);
        public bool TryGetResolution(UpDownConflictId id, out UpDownConflictResolution resolution) => resolutionsById.TryGetValue(id, out resolution);
    }
}
