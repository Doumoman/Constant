using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum MultiSeedBiomeGrowthErrorCode
    {
        MissingPlacementResult,
        PlacementNotCompleted,
        MissingPlacementPublication,
        MissingPlacementDiagnostics,
        InvalidPlacementPublication,
        InvalidSourceSiteSnapshot,
        MissingGenerationProfile,
        InvalidGenerationProfile,
        MissingBiomeTypes,
        MissingPatchRules,
        NullDefinition,
        DuplicateDefinitionId,
        MissingBiomeDefinition,
        UnexpectedBiomeDefinition,
        MissingPatchRule,
        UnexpectedPatchRule,
        InvalidBiomeDefinition,
        InvalidPatchRule,
        DefinitionIdentityMismatch,
        InvalidPatchState,
        InvalidReservationState,
        MissingBiomePatchRng,
        InvalidBiomePatchRngState,
        InternalInvariantViolation,
        InsufficientAggregateCapacity,
        MinimumGrowthBlocked,
        GrowthFrontierExhausted
    }

    public sealed class MultiSeedBiomeGrowthError
    {
        internal MultiSeedBiomeGrowthError(
            MultiSeedBiomeGrowthErrorCode code,
            string definitionId,
            string biomeId,
            BiomePatchId? patchId,
            int sectorIndex,
            int requiredCount,
            int availableCount,
            string message)
        {
            if (!Enum.IsDefined(typeof(MultiSeedBiomeGrowthErrorCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            ReservationValidation.RequireCanonicalId(definitionId ?? string.Empty, nameof(definitionId), true);
            ReservationValidation.RequireCanonicalId(biomeId ?? string.Empty, nameof(biomeId), true);
            if (patchId.HasValue && !patchId.Value.IsValid)
                throw new ArgumentException("Patch ID must be valid.", nameof(patchId));
            if (sectorIndex < -1 || sectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (requiredCount < 0) throw new ArgumentOutOfRangeException(nameof(requiredCount));
            if (availableCount < 0) throw new ArgumentOutOfRangeException(nameof(availableCount));
            if (string.IsNullOrEmpty(message)) throw new ArgumentException("Message cannot be empty.", nameof(message));

            Code = code;
            DefinitionId = definitionId ?? string.Empty;
            BiomeId = biomeId ?? string.Empty;
            PatchId = patchId;
            SectorIndex = sectorIndex;
            RequiredCount = requiredCount;
            AvailableCount = availableCount;
            Shortfall = Math.Max(0, requiredCount - availableCount);
            Message = message;
        }

        public MultiSeedBiomeGrowthErrorCode Code { get; }
        public string DefinitionId { get; }
        public string BiomeId { get; }
        public BiomePatchId? PatchId { get; }
        public int SectorIndex { get; }
        public int RequiredCount { get; }
        public int AvailableCount { get; }
        public int Shortfall { get; }
        public string Message { get; }
    }
}
