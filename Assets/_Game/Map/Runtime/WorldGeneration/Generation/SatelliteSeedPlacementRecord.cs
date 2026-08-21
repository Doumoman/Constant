using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SatelliteSeedPlacementRecord
    {
        internal SatelliteSeedPlacementRecord(
            string patchRuleId,
            string biomeId,
            int satelliteOrdinal,
            BiomePatchId patchId,
            int sectorIndex,
            SectorCoord sector,
            int sameBiomeDistance,
            int minimumSeedDistance,
            int candidateRoll,
            int attemptCount,
            int edgeRejectionCount,
            int distanceRejectionCount)
        {
            ReservationValidation.RequireCanonicalId(patchRuleId, nameof(patchRuleId), false);
            ReservationValidation.RequireCanonicalId(biomeId, nameof(biomeId), false);
            if (satelliteOrdinal < 0 || satelliteOrdinal > 99)
                throw new ArgumentOutOfRangeException(nameof(satelliteOrdinal));
            if (!patchId.IsValid) throw new ArgumentException("Patch ID must be valid.", nameof(patchId));
            BiomePatchModelValidation.ValidateGridIdentity(sectorIndex, sector);
            if (sameBiomeDistance < 0 || minimumSeedDistance < 0 ||
                sameBiomeDistance < minimumSeedDistance)
                throw new ArgumentOutOfRangeException(nameof(sameBiomeDistance));
            if (candidateRoll < 0) throw new ArgumentOutOfRangeException(nameof(candidateRoll));
            if (attemptCount < 1 || edgeRejectionCount < 0 || distanceRejectionCount < 0 ||
                edgeRejectionCount + distanceRejectionCount + 1 != attemptCount)
                throw new ArgumentOutOfRangeException(nameof(attemptCount));

            PatchRuleId = patchRuleId;
            BiomeId = biomeId;
            SatelliteOrdinal = satelliteOrdinal;
            PatchId = patchId;
            SectorIndex = sectorIndex;
            Sector = sector;
            SameBiomeDistance = sameBiomeDistance;
            MinimumSeedDistance = minimumSeedDistance;
            CandidateRoll = candidateRoll;
            AttemptCount = attemptCount;
            EdgeRejectionCount = edgeRejectionCount;
            DistanceRejectionCount = distanceRejectionCount;
        }

        public string PatchRuleId { get; }
        public string BiomeId { get; }
        public int SatelliteOrdinal { get; }
        public BiomePatchId PatchId { get; }
        public int SectorIndex { get; }
        public SectorCoord Sector { get; }
        public int SameBiomeDistance { get; }
        public int MinimumSeedDistance { get; }
        public int CandidateRoll { get; }
        public int AttemptCount { get; }
        public int EdgeRejectionCount { get; }
        public int DistanceRejectionCount { get; }
    }
}
