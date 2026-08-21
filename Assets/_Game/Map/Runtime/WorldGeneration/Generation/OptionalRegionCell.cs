using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalRegionCell
    {
        public OptionalRegionCell(
            OptionalRegionId regionId,
            int sectorIndex,
            SectorCoord sector,
            OptionalRegionDepth depth,
            bool isAttachmentCell,
            bool requiresReturnConnection)
        {
            OptionalRegionValidation.RequireValid(regionId, nameof(regionId));
            OptionalRegionValidation.RequireIndexCoordinateIdentity(sectorIndex, sector, nameof(sectorIndex));
            OptionalRegionValidation.RequireValid(depth, nameof(depth));
            if (isAttachmentCell && depth.Value != 1)
            {
                throw new ArgumentException("Attachment cells must have depth 1.", nameof(depth));
            }

            RegionId = regionId;
            SectorIndex = sectorIndex;
            Sector = sector;
            Depth = depth;
            IsAttachmentCell = isAttachmentCell;
            RequiresReturnConnection = requiresReturnConnection;
        }

        public OptionalRegionId RegionId { get; }
        public int SectorIndex { get; }
        public SectorCoord Sector { get; }
        public OptionalRegionDepth Depth { get; }
        public bool IsAttachmentCell { get; }
        public bool RequiresReturnConnection { get; }
    }
}
