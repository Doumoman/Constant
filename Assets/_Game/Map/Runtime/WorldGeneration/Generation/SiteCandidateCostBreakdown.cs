using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteCandidateCostBreakdown
    {
        internal SiteCandidateCostBreakdown(
            SitePlacementKey candidateKey,
            int candidateOriginIndex,
            SiteFootprintTransform transform,
            int altitudeUnits,
            int edgeUnits,
            int distanceUnits,
            int distanceConstraintCountChecked,
            int distanceViolationCount,
            int futureCoreCapacityUnits,
            bool hasFutureCoreCapacityEstimate,
            int requiredCoreSectorCount,
            int futureCoreAvailableSectorCount,
            int coreClusterUnits,
            bool coreClusterDetected,
            int coreWindowWidth,
            int coreWindowHeight,
            SiteCandidateCostWeights weights)
        {
            if (!candidateKey.IsValid) throw new ArgumentException("A valid candidate key is required.", nameof(candidateKey));
            if (candidateOriginIndex < 0 || candidateOriginIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(candidateOriginIndex));
            if (!IsDefined(transform)) throw new ArgumentOutOfRangeException(nameof(transform));
            if (altitudeUnits < 0) throw new ArgumentOutOfRangeException(nameof(altitudeUnits));
            if (edgeUnits < 0) throw new ArgumentOutOfRangeException(nameof(edgeUnits));
            if (distanceUnits < 0) throw new ArgumentOutOfRangeException(nameof(distanceUnits));
            if (distanceConstraintCountChecked < 0)
                throw new ArgumentOutOfRangeException(nameof(distanceConstraintCountChecked));
            if (distanceViolationCount < 0 || distanceViolationCount > distanceConstraintCountChecked)
                throw new ArgumentOutOfRangeException(nameof(distanceViolationCount));
            if (futureCoreCapacityUnits < 0)
                throw new ArgumentOutOfRangeException(nameof(futureCoreCapacityUnits));
            if (requiredCoreSectorCount < 0)
                throw new ArgumentOutOfRangeException(nameof(requiredCoreSectorCount));
            if (hasFutureCoreCapacityEstimate)
            {
                if (futureCoreAvailableSectorCount < 0 ||
                    futureCoreAvailableSectorCount > WorldGenConstants.SectorCount)
                    throw new ArgumentOutOfRangeException(nameof(futureCoreAvailableSectorCount));
            }
            else if (futureCoreAvailableSectorCount != -1 || futureCoreCapacityUnits != 0)
            {
                throw new ArgumentException("Unavailable capacity must use count -1 and zero units.",
                    nameof(futureCoreAvailableSectorCount));
            }
            if (coreClusterUnits < 0 || coreClusterUnits > 1)
                throw new ArgumentOutOfRangeException(nameof(coreClusterUnits));
            if (coreClusterDetected != (coreClusterUnits == 1))
                throw new ArgumentException("Cluster detection must match cluster units.", nameof(coreClusterDetected));
            var windowUnavailable = coreWindowWidth == -1 && coreWindowHeight == -1;
            var windowAvailable = coreWindowWidth > 0 && coreWindowHeight > 0;
            if (!windowUnavailable && !windowAvailable)
                throw new ArgumentException("Core window dimensions must both be available or unavailable.",
                    nameof(coreWindowWidth));
            if (coreClusterDetected && !windowAvailable)
                throw new ArgumentException("A detected cluster requires window dimensions.", nameof(coreWindowWidth));
            if (weights == null) throw new ArgumentNullException(nameof(weights));

            CandidateKey = candidateKey;
            CandidateOriginIndex = candidateOriginIndex;
            Transform = transform;
            AltitudeUnits = altitudeUnits;
            EdgeUnits = edgeUnits;
            DistanceUnits = distanceUnits;
            DistanceConstraintCountChecked = distanceConstraintCountChecked;
            DistanceViolationCount = distanceViolationCount;
            FutureCoreCapacityUnits = futureCoreCapacityUnits;
            HasFutureCoreCapacityEstimate = hasFutureCoreCapacityEstimate;
            RequiredCoreSectorCount = requiredCoreSectorCount;
            FutureCoreAvailableSectorCount = futureCoreAvailableSectorCount;
            CoreClusterUnits = coreClusterUnits;
            CoreClusterDetected = coreClusterDetected;
            CoreWindowWidth = coreWindowWidth;
            CoreWindowHeight = coreWindowHeight;
            HardConstraintsSatisfied = distanceUnits == 0 && coreClusterUnits == 0;

            checked
            {
                AltitudePenalty = (long)altitudeUnits * weights.AltitudePerSector;
                EdgePenalty = (long)edgeUnits * weights.EdgeClearanceDeficit;
                DistanceConstraintPenalty = (long)distanceUnits * weights.DistanceDeficit;
                FutureCoreCapacityPenalty = (long)futureCoreCapacityUnits *
                                            weights.FutureCoreCapacityShortfall;
                QuadrantClusteringPenalty = (long)coreClusterUnits * weights.CoreCluster;
                TotalCost = AltitudePenalty + EdgePenalty + DistanceConstraintPenalty +
                            FutureCoreCapacityPenalty + QuadrantClusteringPenalty;
            }
        }

        public SitePlacementKey CandidateKey { get; }
        public int CandidateOriginIndex { get; }
        public SiteFootprintTransform Transform { get; }
        public int AltitudeUnits { get; }
        public long AltitudePenalty { get; }
        public int EdgeUnits { get; }
        public long EdgePenalty { get; }
        public int DistanceUnits { get; }
        public long DistanceConstraintPenalty { get; }
        public int DistanceConstraintCountChecked { get; }
        public int DistanceViolationCount { get; }
        public int FutureCoreCapacityUnits { get; }
        public long FutureCoreCapacityPenalty { get; }
        public bool HasFutureCoreCapacityEstimate { get; }
        public int RequiredCoreSectorCount { get; }
        public int FutureCoreAvailableSectorCount { get; }
        public int CoreClusterUnits { get; }
        public long QuadrantClusteringPenalty { get; }
        public bool CoreClusterDetected { get; }
        public int CoreWindowWidth { get; }
        public int CoreWindowHeight { get; }
        public bool HardConstraintsSatisfied { get; }
        public long TotalCost { get; }

        private static bool IsDefined(SiteFootprintTransform value) =>
            value == SiteFootprintTransform.R0 || value == SiteFootprintTransform.MirrorX ||
            value == SiteFootprintTransform.MirrorY || value == SiteFootprintTransform.R180;
    }
}
