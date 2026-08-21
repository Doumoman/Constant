using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum FootprintPlacementErrorCode
    {
        MissingCandidate,
        InvalidCandidate,
        MissingBlockers,
        MissingSpecialMap,
        InvalidSpecialMap,
        SourceIdentityMismatch,
        MissingFootprintCells,
        NullFootprintCell,
        DuplicateFootprintCell,
        InvalidFootprintCell,
        MissingEntrySockets,
        NullEntrySocket,
        DuplicateEntrySocketId,
        InvalidEntrySocket,
        MissingRequiredEntry,
        UnsupportedTransform,
        FootprintOutsideWorld,
        FootprintOverlap,
        BlocksExistingEntryApproach,
        EntryNotOnFootprint,
        DuplicateEntryFace,
        EntryOutsideWorld,
        EntryFacesOwnFootprint,
        EntryApproachOccupied
    }

    public sealed class FootprintPlacementError
    {
        public FootprintPlacementError(
            FootprintPlacementErrorCode code,
            string sourceDefinitionId,
            string entrySocketId,
            int sectorIndex,
            string message)
        {
            if (!Enum.IsDefined(typeof(FootprintPlacementErrorCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            ReservationValidation.RequireCanonicalId(sourceDefinitionId, nameof(sourceDefinitionId), true);
            ReservationValidation.RequireCanonicalId(entrySocketId, nameof(entrySocketId), true);
            if (sectorIndex < -1 || sectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("A stable non-empty message is required.", nameof(message));

            Code = code;
            SourceDefinitionId = sourceDefinitionId;
            EntrySocketId = entrySocketId;
            SectorIndex = sectorIndex;
            Message = message;
        }

        public FootprintPlacementErrorCode Code { get; }
        public string SourceDefinitionId { get; }
        public string EntrySocketId { get; }
        public int SectorIndex { get; }
        public string Message { get; }
    }

    public sealed class FootprintPlacementResult
    {
        private readonly IReadOnlyList<FootprintPlacementError> errors;

        public FootprintPlacementResult(
            FootprintPlacement placement,
            IEnumerable<FootprintPlacementError> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            var snapshot = new List<FootprintPlacementError>(errors);
            foreach (var error in snapshot)
            {
                if (error == null)
                    throw new ArgumentException("Placement errors cannot contain null.", nameof(errors));
            }
            snapshot.Sort(CompareErrors);

            var unique = new List<FootprintPlacementError>(snapshot.Count);
            foreach (var error in snapshot)
            {
                if (unique.Count == 0 || CompareErrors(unique[unique.Count - 1], error) != 0)
                    unique.Add(error);
            }

            if (placement == null && unique.Count == 0)
                throw new ArgumentException("A failed result requires at least one error.", nameof(errors));
            if (placement != null && unique.Count != 0)
                throw new ArgumentException("A successful result cannot contain errors.", nameof(errors));

            Placement = placement;
            this.errors = new ReadOnlyCollection<FootprintPlacementError>(unique);
        }

        public bool Succeeded => Placement != null;
        public FootprintPlacement Placement { get; }
        public IReadOnlyList<FootprintPlacementError> Errors => errors;

        public static FootprintPlacementResult Success(FootprintPlacement placement)
        {
            if (placement == null) throw new ArgumentNullException(nameof(placement));
            return new FootprintPlacementResult(placement, Array.Empty<FootprintPlacementError>());
        }

        public static FootprintPlacementResult Failure(IEnumerable<FootprintPlacementError> errors)
        {
            return new FootprintPlacementResult(null, errors);
        }

        private static int CompareErrors(FootprintPlacementError left, FootprintPlacementError right)
        {
            var code = left.Code.CompareTo(right.Code);
            if (code != 0) return code;
            var source = string.Compare(left.SourceDefinitionId, right.SourceDefinitionId, StringComparison.Ordinal);
            if (source != 0) return source;
            var entry = string.Compare(left.EntrySocketId, right.EntrySocketId, StringComparison.Ordinal);
            if (entry != 0) return entry;
            var sector = left.SectorIndex.CompareTo(right.SectorIndex);
            return sector != 0
                ? sector
                : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }
    }
}
