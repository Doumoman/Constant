using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum CorePatchGrowthErrorCode
    {
        MissingInitialization,
        InvalidInitialization,
        MissingSourceSiteSnapshot,
        InvalidSourceSiteSnapshot,
        MissingBiomeTypes,
        MissingPatchRules,
        NullDefinition,
        DuplicateDefinitionId,
        MissingBiomeDefinition,
        MissingCorePatchRule,
        InvalidBiomeDefinition,
        InvalidCorePatchRule,
        DefinitionIdentityMismatch,
        MissingCorePatch,
        MissingCoreBinding,
        InvalidCorePatch,
        InvalidCoreBinding,
        InvalidCoreSeed,
        InvalidOwnership,
        UnexpectedAssignedSector,
        TargetExceedsMaximum,
        InternalInvariantViolation,
        BufferOutsideWorld,
        BufferBlockedByReservation,
        MandatoryBufferConflict,
        InsufficientUnreservedCapacity
    }

    public sealed class CorePatchGrowthError
    {
        internal CorePatchGrowthError(
            CorePatchGrowthErrorCode code,
            BiomePatchId patchId,
            SiteReservationId sourceReservationId,
            SiteReservationId otherSourceReservationId,
            int sectorIndex,
            int requiredCount,
            int availableCount,
            string message)
        {
            if (!Enum.IsDefined(typeof(CorePatchGrowthErrorCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            if (sectorIndex < -1 || sectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (requiredCount < 0) throw new ArgumentOutOfRangeException(nameof(requiredCount));
            if (availableCount < 0) throw new ArgumentOutOfRangeException(nameof(availableCount));
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Message cannot be empty.", nameof(message));

            Code = code;
            PatchId = patchId;
            SourceReservationId = sourceReservationId;
            OtherSourceReservationId = otherSourceReservationId;
            SectorIndex = sectorIndex;
            RequiredCount = requiredCount;
            AvailableCount = availableCount;
            Shortfall = Math.Max(0, requiredCount - availableCount);
            Message = message;
        }

        public CorePatchGrowthErrorCode Code { get; }
        public BiomePatchId PatchId { get; }
        public SiteReservationId SourceReservationId { get; }
        public SiteReservationId OtherSourceReservationId { get; }
        public int SectorIndex { get; }
        public int RequiredCount { get; }
        public int AvailableCount { get; }
        public int Shortfall { get; }
        public string Message { get; }
    }
}
