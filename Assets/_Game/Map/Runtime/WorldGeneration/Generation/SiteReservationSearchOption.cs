using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteReservationSearchOption
    {
        public SiteReservationSearchOption(
            FootprintPlacement placement,
            int futureCoreAvailableSectorCount)
        {
            if (placement == null) throw new ArgumentNullException(nameof(placement));
            if (futureCoreAvailableSectorCount < -1 ||
                futureCoreAvailableSectorCount > WorldGenConstants.SectorCount)
            {
                throw new ArgumentOutOfRangeException(nameof(futureCoreAvailableSectorCount));
            }

            Placement = placement;
            FutureCoreAvailableSectorCount = futureCoreAvailableSectorCount;
        }

        public FootprintPlacement Placement { get; }
        public int FutureCoreAvailableSectorCount { get; }
    }
}
