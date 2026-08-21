using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum SatelliteSeedPlacementErrorCode
    {
        MissingGrowthPublication,
        InvalidGrowthPublication,
        MissingSourceSiteSnapshot,
        InvalidSourceSiteSnapshot,
        MissingGenerationProfile,
        InvalidGenerationProfile,
        MissingBiomeTypes,
        MissingSatelliteRules,
        NullDefinition,
        DuplicateDefinitionId,
        MissingBiomeDefinition,
        UnexpectedBiomeDefinition,
        MissingSatelliteRule,
        UnexpectedSatelliteRule,
        InvalidBiomeDefinition,
        InvalidSatelliteRule,
        DefinitionIdentityMismatch,
        InvalidCorePatchState,
        InvalidReservationState,
        MissingBiomePatchRng,
        InvalidBiomePatchRngState,
        PatchCountLimitExceeded,
        InternalInvariantViolation,
        CandidateAttemptsExhausted
    }

    public enum SatelliteSeedCandidateRejectionReason
    {
        WorldEdgeForbidden,
        SameBiomeDistanceTooSmall
    }

    public sealed class SatelliteSeedPlacementError
    {
        internal SatelliteSeedPlacementError(
            SatelliteSeedPlacementErrorCode code,
            string definitionId,
            string biomeId,
            int satelliteOrdinal,
            int sectorIndex,
            int requiredCount,
            int availableCount,
            string message)
        {
            if (!Enum.IsDefined(typeof(SatelliteSeedPlacementErrorCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            ReservationValidation.RequireCanonicalId(
                definitionId ?? string.Empty, nameof(definitionId), true);
            ReservationValidation.RequireCanonicalId(
                biomeId ?? string.Empty, nameof(biomeId), true);
            if (satelliteOrdinal < -1 || satelliteOrdinal > 99)
                throw new ArgumentOutOfRangeException(nameof(satelliteOrdinal));
            if (sectorIndex < -1 || sectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (requiredCount < 0) throw new ArgumentOutOfRangeException(nameof(requiredCount));
            if (availableCount < 0) throw new ArgumentOutOfRangeException(nameof(availableCount));
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Message cannot be empty.", nameof(message));

            Code = code;
            DefinitionId = definitionId ?? string.Empty;
            BiomeId = biomeId ?? string.Empty;
            SatelliteOrdinal = satelliteOrdinal;
            SectorIndex = sectorIndex;
            RequiredCount = requiredCount;
            AvailableCount = availableCount;
            Shortfall = Math.Max(0, requiredCount - availableCount);
            Message = message;
        }

        public SatelliteSeedPlacementErrorCode Code { get; }
        public string DefinitionId { get; }
        public string BiomeId { get; }
        public int SatelliteOrdinal { get; }
        public int SectorIndex { get; }
        public int RequiredCount { get; }
        public int AvailableCount { get; }
        public int Shortfall { get; }
        public string Message { get; }
    }
}
