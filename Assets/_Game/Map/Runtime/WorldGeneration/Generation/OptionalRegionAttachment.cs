using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalRegionAttachment
    {
        public OptionalRegionAttachment(
            OptionalRegionId regionId,
            int attachmentOrder,
            int mandatoryRouteSectorIndex,
            SectorCoord mandatoryRouteSector,
            MandatoryRouteGraphNodeId mandatoryRouteNodeId,
            int entrySectorIndex,
            SectorCoord entrySector,
            int entrySideFromMandatoryDx,
            int entrySideFromMandatoryDy,
            OptionalRegionDepth initialDepth)
        {
            OptionalRegionValidation.RequireValid(regionId, nameof(regionId));
            if (attachmentOrder < 0 || attachmentOrder > 9999)
            {
                throw new ArgumentOutOfRangeException(nameof(attachmentOrder));
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
                (entrySideFromMandatoryDx == -1 && entrySideFromMandatoryDy == 0) ||
                (entrySideFromMandatoryDx == 1 && entrySideFromMandatoryDy == 0) ||
                (entrySideFromMandatoryDx == 0 && entrySideFromMandatoryDy == -1) ||
                (entrySideFromMandatoryDx == 0 && entrySideFromMandatoryDy == 1);
            if (!cardinal)
            {
                throw new ArgumentException("Attachment direction must be one cardinal unit vector.");
            }

            if (entrySector.X - mandatoryRouteSector.X != entrySideFromMandatoryDx ||
                entrySector.Y - mandatoryRouteSector.Y != entrySideFromMandatoryDy)
            {
                throw new ArgumentException("Attachment direction must match mandatory-to-entry coordinates.");
            }

            OptionalRegionValidation.RequireValid(initialDepth, nameof(initialDepth));
            if (initialDepth.Value != 1)
            {
                throw new ArgumentException("Initial attachment depth must be exactly 1.", nameof(initialDepth));
            }

            RegionId = regionId;
            AttachmentOrder = attachmentOrder;
            MandatoryRouteSectorIndex = mandatoryRouteSectorIndex;
            MandatoryRouteSector = mandatoryRouteSector;
            MandatoryRouteNodeId = mandatoryRouteNodeId;
            EntrySectorIndex = entrySectorIndex;
            EntrySector = entrySector;
            EntrySideFromMandatoryDx = entrySideFromMandatoryDx;
            EntrySideFromMandatoryDy = entrySideFromMandatoryDy;
            InitialDepth = initialDepth;
        }

        public OptionalRegionId RegionId { get; }
        public int AttachmentOrder { get; }
        public int MandatoryRouteSectorIndex { get; }
        public SectorCoord MandatoryRouteSector { get; }
        public MandatoryRouteGraphNodeId MandatoryRouteNodeId { get; }
        public int EntrySectorIndex { get; }
        public SectorCoord EntrySector { get; }
        public int EntrySideFromMandatoryDx { get; }
        public int EntrySideFromMandatoryDy { get; }
        public OptionalRegionDepth InitialDepth { get; }
    }
}
