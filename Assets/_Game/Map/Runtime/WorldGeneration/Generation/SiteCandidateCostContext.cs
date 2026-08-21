using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteCandidateCostContext
    {
        private readonly IReadOnlyList<FootprintPlacement> existingPlacements;

        public SiteCandidateCostContext(
            SiteDistancePolicy distancePolicy,
            IEnumerable<FootprintPlacement> existingPlacements,
            int futureCoreAvailableSectorCount)
        {
            if (distancePolicy == null) throw new ArgumentNullException(nameof(distancePolicy));
            if (existingPlacements == null) throw new ArgumentNullException(nameof(existingPlacements));
            if (futureCoreAvailableSectorCount < -1 ||
                futureCoreAvailableSectorCount > WorldGenConstants.SectorCount)
            {
                throw new ArgumentOutOfRangeException(nameof(futureCoreAvailableSectorCount));
            }

            var snapshots = new List<PlacementSnapshot>();
            foreach (var placement in existingPlacements)
            {
                if (placement == null)
                    throw new ArgumentException("Existing placements cannot contain null.", nameof(existingPlacements));

                SitePlacementKey key;
                try
                {
                    key = SitePlacementKey.FromPlacement(placement);
                }
                catch (ArgumentException exception)
                {
                    throw new ArgumentException("An existing placement has invalid identity.",
                        nameof(existingPlacements), exception);
                }

                if (!Contains(distancePolicy, key))
                    throw new ArgumentException("Every existing placement key must exist in the distance policy.",
                        nameof(existingPlacements));
                snapshots.Add(new PlacementSnapshot(key, placement));
            }

            snapshots.Sort((left, right) => left.Key.CompareTo(right.Key));
            for (var index = 1; index < snapshots.Count; index++)
            {
                if (snapshots[index - 1].Key == snapshots[index].Key)
                    throw new ArgumentException("Existing placement keys must be unique.",
                        nameof(existingPlacements));
            }

            var ordered = new List<FootprintPlacement>(snapshots.Count);
            foreach (var snapshot in snapshots) ordered.Add(snapshot.Placement);
            var distanceResult = new SiteDistanceIndexBuilder().Build(ordered);
            if (!distanceResult.Succeeded)
                throw new ArgumentException("Existing placements must be valid and non-overlapping.",
                    nameof(existingPlacements));

            DistancePolicy = distancePolicy;
            this.existingPlacements = new ReadOnlyCollection<FootprintPlacement>(ordered);
            FutureCoreAvailableSectorCount = futureCoreAvailableSectorCount;
        }

        public SiteDistancePolicy DistancePolicy { get; }
        public IReadOnlyList<FootprintPlacement> ExistingPlacements => existingPlacements;
        public int FutureCoreAvailableSectorCount { get; }
        public bool HasFutureCoreCapacityEstimate => FutureCoreAvailableSectorCount >= 0;

        private static bool Contains(SiteDistancePolicy policy, SitePlacementKey key)
        {
            foreach (var policyKey in policy.Keys)
            {
                if (policyKey == key) return true;
            }
            return false;
        }

        private sealed class PlacementSnapshot
        {
            public PlacementSnapshot(SitePlacementKey key, FootprintPlacement placement)
            {
                Key = key;
                Placement = placement;
            }

            public SitePlacementKey Key { get; }
            public FootprintPlacement Placement { get; }
        }
    }
}
