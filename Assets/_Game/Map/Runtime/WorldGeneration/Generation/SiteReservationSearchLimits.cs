using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteReservationSearchLimits
    {
        public const int ProductionMaximum = 200;

        public SiteReservationSearchLimits(int maxFailedCombinations)
        {
            if (maxFailedCombinations < 1 || maxFailedCombinations > ProductionMaximum)
                throw new ArgumentOutOfRangeException(nameof(maxFailedCombinations));
            MaxFailedCombinations = maxFailedCombinations;
        }

        public int MaxFailedCombinations { get; }

        public static SiteReservationSearchLimits Default { get; } =
            new SiteReservationSearchLimits(ProductionMaximum);
    }
}
