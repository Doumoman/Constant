using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class CoreBiomeSeed
    {
        public CoreBiomeSeed(
            SiteReservationId sourceReservationId,
            string biomeId,
            string corePatchRuleId,
            SectorCoord seedSector,
            int minimumCoreSectorCount,
            int bufferRingSectors)
        {
            if (!sourceReservationId.IsValid) throw new ArgumentException("Source reservation ID must be valid.", nameof(sourceReservationId));
            ReservationValidation.RequireCanonicalId(biomeId, nameof(biomeId), false);
            ReservationValidation.RequireCanonicalId(corePatchRuleId, nameof(corePatchRuleId), false);
            if (seedSector.X < 0 || seedSector.X >= WorldGenConstants.SectorColumns ||
                seedSector.Y < 0 || seedSector.Y >= WorldGenConstants.SectorRows)
                throw new ArgumentOutOfRangeException(nameof(seedSector));
            if (minimumCoreSectorCount < 1) throw new ArgumentOutOfRangeException(nameof(minimumCoreSectorCount));
            if (bufferRingSectors < 0) throw new ArgumentOutOfRangeException(nameof(bufferRingSectors));

            SourceReservationId = sourceReservationId;
            BiomeId = biomeId;
            CorePatchRuleId = corePatchRuleId;
            SeedSector = seedSector;
            MinimumCoreSectorCount = minimumCoreSectorCount;
            BufferRingSectors = bufferRingSectors;
        }

        public SiteReservationId SourceReservationId { get; }
        public string BiomeId { get; }
        public string CorePatchRuleId { get; }
        public SectorCoord SeedSector { get; }
        public int MinimumCoreSectorCount { get; }
        public int BufferRingSectors { get; }
    }
}
