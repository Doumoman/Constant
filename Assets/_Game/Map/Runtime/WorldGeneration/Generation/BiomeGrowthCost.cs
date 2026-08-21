using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class BiomeGrowthCost
    {
        public const int WeightScale = 1000;
        public const int ReservationPenaltyValue2 = 10000000;

        public BiomeGrowthCost(
            int graphDistance,
            int altitudeDistance2,
            int noisePermille,
            int samePatchNeighborCount,
            bool hasReservationPenalty,
            int distanceWeightMilli,
            int altitudeWeightMilli,
            int noiseWeightMilli,
            int compactnessWeightMilli)
        {
            if (graphDistance < 0) throw new ArgumentOutOfRangeException(nameof(graphDistance));
            if (altitudeDistance2 < 0) throw new ArgumentOutOfRangeException(nameof(altitudeDistance2));
            if (noisePermille < 0 || noisePermille > 1000)
                throw new ArgumentOutOfRangeException(nameof(noisePermille));
            if (samePatchNeighborCount < 0 || samePatchNeighborCount > 4)
                throw new ArgumentOutOfRangeException(nameof(samePatchNeighborCount));
            if (distanceWeightMilli < 0 || altitudeWeightMilli < 0 ||
                noiseWeightMilli < 0 || compactnessWeightMilli < 0)
                throw new ArgumentOutOfRangeException(nameof(distanceWeightMilli));

            GraphDistance = graphDistance;
            AltitudeDistance2 = altitudeDistance2;
            NoisePermille = noisePermille;
            SamePatchNeighborCount = samePatchNeighborCount;
            ExposedPerimeterDelta = 4 - (2 * samePatchNeighborCount);
            DistanceWeightMilli = distanceWeightMilli;
            AltitudeWeightMilli = altitudeWeightMilli;
            NoiseWeightMilli = noiseWeightMilli;
            CompactnessWeightMilli = compactnessWeightMilli;

            checked
            {
                GraphTerm2 = 2 * graphDistance * distanceWeightMilli;
                AltitudeTerm2 = altitudeDistance2 * altitudeWeightMilli;
                NoiseTerm2 = (int)(((2L * noisePermille * noiseWeightMilli) + 500L) / 1000L);
                PerimeterTerm2 = 2 * ExposedPerimeterDelta * compactnessWeightMilli;
                ReservationTerm2 = hasReservationPenalty ? ReservationPenaltyValue2 : 0;
                TotalCost2 = GraphTerm2 + AltitudeTerm2 + NoiseTerm2 +
                             PerimeterTerm2 + ReservationTerm2;
            }
        }

        public int GraphDistance { get; }
        public int AltitudeDistance2 { get; }
        public int NoisePermille { get; }
        public int SamePatchNeighborCount { get; }
        public int ExposedPerimeterDelta { get; }
        public int DistanceWeightMilli { get; }
        public int AltitudeWeightMilli { get; }
        public int NoiseWeightMilli { get; }
        public int CompactnessWeightMilli { get; }
        public int GraphTerm2 { get; }
        public int AltitudeTerm2 { get; }
        public int NoiseTerm2 { get; }
        public int PerimeterTerm2 { get; }
        public int ReservationTerm2 { get; }
        public int TotalCost2 { get; }

        public static bool TryQuantizeWeight(float value, out int milli)
        {
            milli = 0;
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                return false;
            var scaled = (double)value * WeightScale;
            var rounded = Math.Round(scaled, MidpointRounding.AwayFromZero);
            if (Math.Abs((double)value - (rounded / WeightScale)) > 0.000001d ||
                rounded > int.MaxValue)
                return false;
            milli = (int)rounded;
            return true;
        }
    }
}
