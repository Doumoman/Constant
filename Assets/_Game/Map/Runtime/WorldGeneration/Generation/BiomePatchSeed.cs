using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class BiomePatchSeed
    {
        public BiomePatchSeed(
            int sectorIndex,
            SectorCoord sector,
            BiomePatchRole role,
            SiteReservationId? sourceSiteReservationId)
        {
            BiomePatchModelValidation.ValidateGridIdentity(sectorIndex, sector);
            if (!BiomePatchModelValidation.IsDefined(role))
                throw new ArgumentOutOfRangeException(nameof(role));

            if (role == BiomePatchRole.Core)
            {
                if (!sourceSiteReservationId.HasValue || !sourceSiteReservationId.Value.IsValid)
                    throw new ArgumentException("Core seeds require a valid source site reservation ID.", nameof(sourceSiteReservationId));
            }
            else if (sourceSiteReservationId.HasValue)
            {
                throw new ArgumentException("Non-Core seeds cannot have a source site reservation ID.", nameof(sourceSiteReservationId));
            }

            SectorIndex = sectorIndex;
            Sector = sector;
            Role = role;
            SourceSiteReservationId = sourceSiteReservationId;
        }

        public int SectorIndex { get; }
        public SectorCoord Sector { get; }
        public BiomePatchRole Role { get; }
        public SiteReservationId? SourceSiteReservationId { get; }
    }
}
