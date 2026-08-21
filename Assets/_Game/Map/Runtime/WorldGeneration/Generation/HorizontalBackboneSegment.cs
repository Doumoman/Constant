using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class HorizontalBackboneSegment
    {
        private readonly IReadOnlyList<HorizontalBackboneRouteCell> cells;

        public HorizontalBackboneSegment(
            HorizontalBackboneSegmentId segmentId,
            MandatoryConnectorEdgeId sourceTreeEdgeId,
            MandatoryRouteTerminalId fromTerminalId,
            MandatoryRouteTerminalId toTerminalId,
            IEnumerable<HorizontalBackboneRouteCell> cells,
            SectorCoord fromApproachSector,
            SectorCoord toApproachSector,
            bool isSameRow,
            bool requiresVerticalGateway,
            int horizontalDistance,
            int totalCost)
        {
            if (!segmentId.IsValid) throw new ArgumentException("Segment ID must be valid.", nameof(segmentId));
            if (!sourceTreeEdgeId.IsValid) throw new ArgumentException("Source tree edge ID must be valid.", nameof(sourceTreeEdgeId));
            if (!fromTerminalId.IsValid || !toTerminalId.IsValid || fromTerminalId == toTerminalId)
                throw new ArgumentException("Terminal identities must be distinct and valid.");
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            var values = new List<HorizontalBackboneRouteCell>(cells);
            if (values.Count == 0) throw new ArgumentException("A segment must contain route cells.", nameof(cells));
            var seen = new HashSet<SectorCoord>();
            var cost = 0;
            var gatewayCount = 0;
            for (var index = 0; index < values.Count; index++)
            {
                var cell = values[index];
                if (cell == null || cell.Ordinal != index) throw new ArgumentException("Cell ordinals must be exact and sorted.", nameof(cells));
                if (!seen.Add(cell.Coord)) throw new ArgumentException("A segment cannot contain duplicate sectors.", nameof(cells));
                if (!cell.OpensLeft || !cell.OpensRight) throw new ArgumentException("Every route cell must preserve L/R.", nameof(cells));
                if (cell.IsReserved && !cell.IsEndpoint) throw new ArgumentException("Reserved middle cells are forbidden.", nameof(cells));
                if (cell.RequiresVerticalGateway) gatewayCount++;
                cost = checked(cost + cell.StepCost);
            }
            if (isSameRow != (fromApproachSector.Y == toApproachSector.Y)) throw new ArgumentException("Same-row flag does not match endpoints.");
            if (isSameRow && (requiresVerticalGateway || gatewayCount != 0)) throw new ArgumentException("Same-row runs cannot require a gateway.");
            if (!isSameRow && (!requiresVerticalGateway || gatewayCount != 2)) throw new ArgumentException("Different-row runs require exactly two pending gateway anchors.");
            if (horizontalDistance < 0 || totalCost != cost) throw new ArgumentException("Distance or total cost is inconsistent.");
            if (!seen.Contains(fromApproachSector) || !seen.Contains(toApproachSector)) throw new ArgumentException("Both approach sectors must be included.");

            SegmentId = segmentId;
            SourceTreeEdgeId = sourceTreeEdgeId;
            FromTerminalId = fromTerminalId;
            ToTerminalId = toTerminalId;
            this.cells = new ReadOnlyCollection<HorizontalBackboneRouteCell>(values);
            FromApproachSector = fromApproachSector;
            ToApproachSector = toApproachSector;
            IsSameRow = isSameRow;
            RequiresVerticalGateway = requiresVerticalGateway;
            HorizontalDistance = horizontalDistance;
            TotalCost = totalCost;
        }

        public HorizontalBackboneSegmentId SegmentId { get; }
        public MandatoryConnectorEdgeId SourceTreeEdgeId { get; }
        public MandatoryRouteTerminalId FromTerminalId { get; }
        public MandatoryRouteTerminalId ToTerminalId { get; }
        public IReadOnlyList<HorizontalBackboneRouteCell> Cells => cells;
        public SectorCoord FromApproachSector { get; }
        public SectorCoord ToApproachSector { get; }
        public bool IsSameRow { get; }
        public bool RequiresVerticalGateway { get; }
        public int HorizontalDistance { get; }
        public int TotalCost { get; }
    }
}
