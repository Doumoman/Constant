using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class Type0RouteMaskAssignment
    {
        internal Type0RouteMaskAssignment(OptionalRegionCell sourceCell, Type0RouteMaskRecord mask)
        {
            if (sourceCell == null) throw new ArgumentNullException(nameof(sourceCell));
            Mask = mask ?? throw new ArgumentNullException(nameof(mask));

            RegionId = sourceCell.RegionId;
            SectorIndex = sourceCell.SectorIndex;
            Sector = sourceCell.Sector;
            Depth = sourceCell.Depth;
            IsAttachmentCell = sourceCell.IsAttachmentCell;
            MaskId = mask.MaskId;
            OpenMask = mask.OpenMask;
        }

        public OptionalRegionId RegionId { get; }
        public int SectorIndex { get; }
        public SectorCoord Sector { get; }
        public OptionalRegionDepth Depth { get; }
        public bool IsAttachmentCell { get; }
        public Type0RouteMaskRecord Mask { get; }
        public Type0RouteMaskId MaskId { get; }
        public Type0RouteOpenMask OpenMask { get; }
    }
}
