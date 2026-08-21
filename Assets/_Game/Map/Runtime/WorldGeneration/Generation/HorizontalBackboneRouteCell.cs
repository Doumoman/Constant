using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class HorizontalBackboneRouteCell
    {
        public HorizontalBackboneRouteCell(
            SectorCoord coord,
            int ordinal,
            bool opensLeft,
            bool opensRight,
            bool isEndpoint,
            bool isReserved,
            bool requiresVerticalGateway,
            int stepCost)
        {
            if (coord.X < 0 || coord.X >= WorldGenConstants.SectorColumns || coord.Y < 0 || coord.Y >= WorldGenConstants.SectorRows)
                throw new ArgumentOutOfRangeException(nameof(coord));
            if (ordinal < 0) throw new ArgumentOutOfRangeException(nameof(ordinal));
            if (!opensLeft || !opensRight) throw new ArgumentException("Horizontal backbone cells must preserve L/R openings.");
            if (isReserved && !isEndpoint) throw new ArgumentException("Reserved cells are only valid as endpoint adapters.");
            if (stepCost != 1 && stepCost != 2 && stepCost != 4 && stepCost != 8)
                throw new ArgumentOutOfRangeException(nameof(stepCost));
            Coord = coord;
            Ordinal = ordinal;
            OpensLeft = opensLeft;
            OpensRight = opensRight;
            IsEndpoint = isEndpoint;
            IsReserved = isReserved;
            RequiresVerticalGateway = requiresVerticalGateway;
            StepCost = stepCost;
        }

        public SectorCoord Coord { get; }
        public int Ordinal { get; }
        public bool OpensLeft { get; }
        public bool OpensRight { get; }
        public bool IsEndpoint { get; }
        public bool IsReserved { get; }
        public bool RequiresVerticalGateway { get; }
        public int StepCost { get; }
    }
}
