using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum VillageReservationErrorCode
    {
        MissingCoreCapacityApproval,
        InvalidCoreCapacityApproval,
        MissingVillageProfile,
        InvalidVillageProfile,
        MissingVillageSpecialMap,
        InvalidVillageSpecialMap,
        MissingEntrySockets,
        NullEntrySocket,
        UnexpectedEntrySocket,
        InvalidEntrySocket,
        MissingLayouts,
        NullLayout,
        DuplicateLayoutId,
        MissingAllowedLayout,
        UnexpectedLayout,
        InvalidLayout,
        InvalidDistanceBuckets,
        MissingSiteRng,
        InvalidSelectedPlacement,
        InvalidCapacityWitness,
        DefinitionIdentityMismatch,
        InternalInvariantViolation
    }

    public sealed class VillageReservationError
    {
        public VillageReservationError(
            VillageReservationErrorCode code,
            string definitionId,
            int sectorIndex,
            string message)
        {
            if (!Enum.IsDefined(typeof(VillageReservationErrorCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            if (definitionId == null ||
                (definitionId.Length != 0 && !SitePlacementKey.IsCanonicalId(definitionId)))
                throw new ArgumentException("Definition ID must be canonical or empty.", nameof(definitionId));
            if (sectorIndex < -1 || sectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("A stable non-empty message is required.", nameof(message));

            Code = code;
            DefinitionId = definitionId;
            SectorIndex = sectorIndex;
            Message = message;
        }

        public VillageReservationErrorCode Code { get; }
        public string DefinitionId { get; }
        public int SectorIndex { get; }
        public string Message { get; }
    }
}
