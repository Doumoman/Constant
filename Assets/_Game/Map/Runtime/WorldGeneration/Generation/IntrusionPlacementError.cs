using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum IntrusionPlacementErrorCode
    {
        MissingGrowthResult,
        GrowthNotCompleted,
        MissingGrowthPublication,
        MissingGrowthDiagnostics,
        InvalidGrowthPublication,
        InvalidSourceSiteSnapshot,
        MissingGenerationProfile,
        InvalidGenerationProfile,
        MissingBiomeTypes,
        MissingPatchRules,
        MissingBoundaryProfiles,
        MissingBoundaryPairRules,
        NullDefinition,
        DuplicateDefinitionId,
        MissingBiomeDefinition,
        UnexpectedBiomeDefinition,
        MissingPatchRule,
        UnexpectedPatchRule,
        MissingBoundaryProfile,
        UnexpectedBoundaryProfile,
        MissingBoundaryPairRule,
        UnexpectedBoundaryPairRule,
        InvalidBiomeDefinition,
        InvalidPatchRule,
        InvalidBoundaryProfile,
        InvalidBoundaryPairRule,
        DefinitionIdentityMismatch,
        InvalidPatchState,
        InvalidReservationState,
        MissingBiomePatchRng,
        InvalidBiomePatchRngState,
        InternalInvariantViolation,
        NoLegalIntrusionCandidate
    }

    public enum IntrusionCandidateRejectionReason
    {
        ReservedSector,
        WorldEdgeForbidden,
        ProtectedSeedSector,
        ProtectedSiteBindingSector,
        SameBiomeHost,
        DisallowedBoundaryPair,
        MissingIntruderSharedEdge,
        DonorBelowMinimum,
        DonorDisconnected,
        IntrusionSeedDistanceTooSmall,
        BiomeShareExceeded,
        IntrusionShareExceeded
    }

    public sealed class IntrusionPlacementError
    {
        internal IntrusionPlacementError(
            IntrusionPlacementErrorCode code,
            string definitionId,
            string intruderBiomeId,
            string hostBiomeId,
            int intrusionOrdinal,
            int sectorIndex,
            int requiredCount,
            int availableCount,
            string message)
        {
            if (!Enum.IsDefined(typeof(IntrusionPlacementErrorCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            ReservationValidation.RequireCanonicalId(definitionId ?? string.Empty, nameof(definitionId), true);
            ReservationValidation.RequireCanonicalId(intruderBiomeId ?? string.Empty, nameof(intruderBiomeId), true);
            ReservationValidation.RequireCanonicalId(hostBiomeId ?? string.Empty, nameof(hostBiomeId), true);
            if (intrusionOrdinal < -1 || intrusionOrdinal > 99)
                throw new ArgumentOutOfRangeException(nameof(intrusionOrdinal));
            if (sectorIndex < -1 || sectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (requiredCount < 0) throw new ArgumentOutOfRangeException(nameof(requiredCount));
            if (availableCount < 0) throw new ArgumentOutOfRangeException(nameof(availableCount));
            if (string.IsNullOrEmpty(message)) throw new ArgumentException("Message cannot be empty.", nameof(message));

            Code = code;
            DefinitionId = definitionId ?? string.Empty;
            IntruderBiomeId = intruderBiomeId ?? string.Empty;
            HostBiomeId = hostBiomeId ?? string.Empty;
            IntrusionOrdinal = intrusionOrdinal;
            SectorIndex = sectorIndex;
            RequiredCount = requiredCount;
            AvailableCount = availableCount;
            Shortfall = Math.Max(0, requiredCount - availableCount);
            Message = message;
        }

        public IntrusionPlacementErrorCode Code { get; }
        public string DefinitionId { get; }
        public string IntruderBiomeId { get; }
        public string HostBiomeId { get; }
        public int IntrusionOrdinal { get; }
        public int SectorIndex { get; }
        public int RequiredCount { get; }
        public int AvailableCount { get; }
        public int Shortfall { get; }
        public string Message { get; }
    }
}
