using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalAttachmentCandidate
    {
        public OptionalAttachmentCandidate(
            OptionalAttachmentCandidateId candidateId,
            int attachmentOrder,
            int mandatoryRouteSectorIndex,
            SectorCoord mandatoryRouteSector,
            MandatoryRouteGraphNodeId mandatoryRouteNodeId,
            int entrySectorIndex,
            SectorCoord entrySector,
            int directionDx,
            int directionDy,
            OptionalRegionDepth initialDepth)
        {
            if (!candidateId.IsValid)
            {
                throw new ArgumentException("Candidate ID must be valid.", nameof(candidateId));
            }

            if (!candidateId.TryGetOrdinal(out var ordinal) || attachmentOrder != ordinal)
            {
                throw new ArgumentException("Attachment order must match the candidate ID ordinal.", nameof(attachmentOrder));
            }

            OptionalRegionValidation.RequireIndexCoordinateIdentity(
                mandatoryRouteSectorIndex,
                mandatoryRouteSector,
                nameof(mandatoryRouteSectorIndex));
            if (!mandatoryRouteNodeId.IsValid)
            {
                throw new ArgumentException("Mandatory route node ID must be valid.", nameof(mandatoryRouteNodeId));
            }

            OptionalRegionValidation.RequireIndexCoordinateIdentity(
                entrySectorIndex,
                entrySector,
                nameof(entrySectorIndex));

            var cardinal =
                (directionDx == -1 && directionDy == 0) ||
                (directionDx == 1 && directionDy == 0) ||
                (directionDx == 0 && directionDy == 1) ||
                (directionDx == 0 && directionDy == -1);
            if (!cardinal)
            {
                throw new ArgumentException("Direction must be one cardinal unit vector.");
            }

            if (entrySector.X - mandatoryRouteSector.X != directionDx ||
                entrySector.Y - mandatoryRouteSector.Y != directionDy)
            {
                throw new ArgumentException("Direction must match mandatory-to-entry coordinates.");
            }

            OptionalRegionValidation.RequireValid(initialDepth, nameof(initialDepth));
            if (initialDepth.Value != 1)
            {
                throw new ArgumentException("Initial candidate depth must be exactly 1.", nameof(initialDepth));
            }

            CandidateId = candidateId;
            AttachmentOrder = attachmentOrder;
            MandatoryRouteSectorIndex = mandatoryRouteSectorIndex;
            MandatoryRouteSector = mandatoryRouteSector;
            MandatoryRouteNodeId = mandatoryRouteNodeId;
            EntrySectorIndex = entrySectorIndex;
            EntrySector = entrySector;
            DirectionDx = directionDx;
            DirectionDy = directionDy;
            InitialDepth = initialDepth;
        }

        public OptionalAttachmentCandidateId CandidateId { get; }
        public int AttachmentOrder { get; }
        public int MandatoryRouteSectorIndex { get; }
        public SectorCoord MandatoryRouteSector { get; }
        public MandatoryRouteGraphNodeId MandatoryRouteNodeId { get; }
        public int EntrySectorIndex { get; }
        public SectorCoord EntrySector { get; }
        public int DirectionDx { get; }
        public int DirectionDy { get; }
        public OptionalRegionDepth InitialDepth { get; }
    }
}
