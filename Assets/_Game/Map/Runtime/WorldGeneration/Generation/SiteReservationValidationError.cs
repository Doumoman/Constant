using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum SiteReservationValidationErrorCode
    {
        MissingApproval,
        InvalidApproval,
        MissingSpecialMaps,
        NullSpecialMap,
        DuplicateSpecialMapId,
        MissingRequiredSpecialMap,
        UnexpectedSpecialMap,
        InvalidSpecialMap,
        MissingFootprintCells,
        NullFootprintCell,
        DuplicateFootprintCell,
        MissingRequiredFootprintCell,
        UnexpectedFootprintCell,
        InvalidFootprintCell,
        MissingEntrySockets,
        NullEntrySocket,
        DuplicateEntrySocket,
        MissingRequiredEntrySocket,
        UnexpectedEntrySocket,
        InvalidEntrySocket,
        SelectionIdentityMismatch,
        VillageIdentityMismatch,
        CapacityIdentityMismatch,
        DefinitionIdentityMismatch,
        InternalInvariantViolation
    }

    public sealed class SiteReservationValidationError
    {
        public SiteReservationValidationError(
            SiteReservationValidationErrorCode code,
            string definitionId,
            string childId,
            int sectorIndex,
            string message)
        {
            if (!Enum.IsDefined(typeof(SiteReservationValidationErrorCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            if (!CanonicalOrEmpty(definitionId))
                throw new ArgumentException("Definition ID must be canonical or empty.", nameof(definitionId));
            if (!CanonicalOrEmpty(childId))
                throw new ArgumentException("Child ID must be canonical or empty.", nameof(childId));
            if (sectorIndex < -1 || sectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("A stable non-empty message is required.", nameof(message));

            Code = code;
            DefinitionId = definitionId;
            ChildId = childId;
            SectorIndex = sectorIndex;
            Message = message;
        }

        public SiteReservationValidationErrorCode Code { get; }
        public string DefinitionId { get; }
        public string ChildId { get; }
        public int SectorIndex { get; }
        public string Message { get; }

        private static bool CanonicalOrEmpty(string value) =>
            value != null && (value.Length == 0 || SitePlacementKey.IsCanonicalId(value));
    }
}
