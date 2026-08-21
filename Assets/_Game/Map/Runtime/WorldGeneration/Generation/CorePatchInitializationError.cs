using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum CorePatchInitializationErrorCode
    {
        MissingSiteSnapshot,
        InvalidSiteSnapshot,
        InvalidReservationSet,
        InvalidCoreSeedSet,
        NullCoreSeed,
        DuplicateCoreSeedSource,
        MissingRequiredCoreSeed,
        UnexpectedCoreSeed,
        MissingSourceReservation,
        InvalidSourceReservation,
        SeedOutsideSourceFootprint,
        SourceFootprintOverlap,
        MissingBiomeTypes,
        NullBiomeType,
        DuplicateBiomeTypeId,
        MissingRequiredBiomeType,
        InvalidBiomeType,
        MissingPatchRules,
        NullPatchRule,
        DuplicatePatchRuleId,
        MissingRequiredPatchRule,
        InvalidPatchRule,
        DefinitionIdentityMismatch,
        InvalidGeneratedPatchId,
        DuplicateGeneratedPatchId,
        InternalInvariantViolation
    }

    public sealed class CorePatchInitializationError
    {
        internal CorePatchInitializationError(
            CorePatchInitializationErrorCode code,
            string sourceReservationId,
            string biomeId,
            string patchRuleId,
            int sectorIndex,
            string message)
        {
            if (sourceReservationId == null) throw new ArgumentNullException(nameof(sourceReservationId));
            if (biomeId == null) throw new ArgumentNullException(nameof(biomeId));
            if (patchRuleId == null) throw new ArgumentNullException(nameof(patchRuleId));
            if (sectorIndex < -1 || sectorIndex >= Domain.WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (string.IsNullOrEmpty(message)) throw new ArgumentException("Message cannot be empty.", nameof(message));

            Code = code;
            SourceReservationId = sourceReservationId;
            BiomeId = biomeId;
            PatchRuleId = patchRuleId;
            SectorIndex = sectorIndex;
            Message = message;
        }

        public CorePatchInitializationErrorCode Code { get; }
        public string SourceReservationId { get; }
        public string BiomeId { get; }
        public string PatchRuleId { get; }
        public int SectorIndex { get; }
        public string Message { get; }
    }
}
