using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteGraphCell
    {
        internal MandatoryRouteGraphCell(SectorCell sourceCell, SectorCell stampedCell, MandatoryRouteMaskFamily.Entry mask,
            bool openLeft, bool openRight, bool openUp, bool openDown, bool approvedReservedAdapter)
        {
            SourceCell = sourceCell ?? throw new ArgumentNullException(nameof(sourceCell));
            StampedCell = stampedCell ?? throw new ArgumentNullException(nameof(stampedCell));
            Mask = mask ?? throw new ArgumentNullException(nameof(mask));
            OpenLeft = openLeft; OpenRight = openRight; OpenUp = openUp; OpenDown = openDown;
            IsApprovedReservedAdapter = approvedReservedAdapter;
        }

        public SectorCell SourceCell { get; }
        public SectorCell StampedCell { get; }
        public MandatoryRouteMaskFamily.Entry Mask { get; }
        public int SectorIndex => StampedCell.Index;
        public SectorCoord Coordinate => StampedCell.Coordinate;
        public string RouteMaskId => StampedCell.RouteMaskId;
        public bool MandatoryGraphNode => StampedCell.MandatoryGraphNode;
        public int ShortestDistanceFromStart => StampedCell.ShortestDistanceFromStart;
        public bool OpenLeft { get; }
        public bool OpenRight { get; }
        public bool OpenUp { get; }
        public bool OpenDown { get; }
        public bool IsApprovedReservedAdapter { get; }
    }
}
