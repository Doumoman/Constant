using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class VerticalGatewayAnchor
    {
        public VerticalGatewayAnchor(
            SectorCoord coord,
            bool isUpperAnchor,
            bool opensDown,
            bool opensUp,
            bool isEndpointAdapter,
            bool isReserved,
            int stepCost)
        {
            if (coord.X < 0 || coord.X >= WorldGenConstants.SectorColumns || coord.Y < 0 || coord.Y >= WorldGenConstants.SectorRows)
                throw new ArgumentOutOfRangeException(nameof(coord));
            if (isUpperAnchor ? (!opensDown || opensUp) : (!opensUp || opensDown))
                throw new ArgumentException("Upper anchors must open down only and lower anchors must open up only.");
            if (isReserved && !isEndpointAdapter) throw new ArgumentException("Reserved anchors must be endpoint adapters.");
            if (stepCost != 1 && stepCost != 2 && stepCost != 4 && stepCost != 8)
                throw new ArgumentOutOfRangeException(nameof(stepCost));
            Coord = coord;
            IsUpperAnchor = isUpperAnchor;
            OpensDown = opensDown;
            OpensUp = opensUp;
            IsEndpointAdapter = isEndpointAdapter;
            IsReserved = isReserved;
            StepCost = stepCost;
        }

        public SectorCoord Coord { get; }
        public bool IsUpperAnchor { get; }
        public bool OpensDown { get; }
        public bool OpensUp { get; }
        public bool IsEndpointAdapter { get; }
        public bool IsReserved { get; }
        public int StepCost { get; }
    }
}
