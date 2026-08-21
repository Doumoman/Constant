using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteCandidateCostWeights
    {
        public SiteCandidateCostWeights(
            int altitudePerSector,
            int edgeClearanceDeficit,
            int distanceDeficit,
            int futureCoreCapacityShortfall,
            int coreCluster)
        {
            if (altitudePerSector < 0) throw new ArgumentOutOfRangeException(nameof(altitudePerSector));
            if (edgeClearanceDeficit < 0) throw new ArgumentOutOfRangeException(nameof(edgeClearanceDeficit));
            if (distanceDeficit < 0) throw new ArgumentOutOfRangeException(nameof(distanceDeficit));
            if (futureCoreCapacityShortfall < 0)
                throw new ArgumentOutOfRangeException(nameof(futureCoreCapacityShortfall));
            if (coreCluster < 0) throw new ArgumentOutOfRangeException(nameof(coreCluster));

            AltitudePerSector = altitudePerSector;
            EdgeClearanceDeficit = edgeClearanceDeficit;
            DistanceDeficit = distanceDeficit;
            FutureCoreCapacityShortfall = futureCoreCapacityShortfall;
            CoreCluster = coreCluster;
        }

        public int AltitudePerSector { get; }
        public int EdgeClearanceDeficit { get; }
        public int DistanceDeficit { get; }
        public int FutureCoreCapacityShortfall { get; }
        public int CoreCluster { get; }

        public static SiteCandidateCostWeights Default { get; } =
            new SiteCandidateCostWeights(10, 25, 1000, 100, 10000);
    }
}
