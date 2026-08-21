using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteLoopCandidate
    {
        private readonly IReadOnlyList<SectorCoord> cells;
        private readonly IReadOnlyList<SectorCoord> waypoints;
        private readonly IReadOnlyList<HorizontalBackboneSegmentId> backboneIds;
        private readonly IReadOnlyList<VerticalGatewayId> gatewayIds;
        private readonly IReadOnlyList<VerticalGatewayJunctionCell> type4Junctions;

        public MandatoryRouteLoopCandidate(
            MandatoryRouteLoopId loopId,
            MandatoryRouteTerminalId startTerminalId,
            MandatoryRouteTerminalId endTerminalId,
            MandatoryConnectorEdgeId sourceConnectorEdgeId,
            IEnumerable<SectorCoord> orderedCells,
            IEnumerable<HorizontalBackboneSegmentId> sourceBackboneSegmentIds,
            IEnumerable<VerticalGatewayId> sourceGatewayIds,
            IEnumerable<VerticalGatewayJunctionCell> sourceType4Junctions,
            string siteIdentity,
            string biomeIdentity,
            int checkedTotalCost,
            int sharedCellCount,
            bool hasReservationIntrusion,
            bool hasInactiveIntrusion,
            bool hasMandatoryPathIntrusion,
            bool isIndependent)
        {
            if (!loopId.IsValid) throw new ArgumentException("Loop identity must be valid.", nameof(loopId));
            if (!startTerminalId.IsValid || !endTerminalId.IsValid || startTerminalId == endTerminalId)
                throw new ArgumentException("Loop terminal identities must be valid and distinct.");
            if (!sourceConnectorEdgeId.IsValid) throw new ArgumentException("Source connector identity must be valid.", nameof(sourceConnectorEdgeId));
            if (siteIdentity == null) throw new ArgumentNullException(nameof(siteIdentity));
            if (biomeIdentity == null) throw new ArgumentNullException(nameof(biomeIdentity));
            if (checkedTotalCost < 0) throw new ArgumentOutOfRangeException(nameof(checkedTotalCost));
            if (sharedCellCount < 0) throw new ArgumentOutOfRangeException(nameof(sharedCellCount));
            var cellValues = new List<SectorCoord>(orderedCells ?? throw new ArgumentNullException(nameof(orderedCells)));
            if (cellValues.Count < 2) throw new ArgumentException("Loop candidates require at least two ordered cells.", nameof(orderedCells));
            for (var index = 1; index < cellValues.Count; index++)
            {
                var dx = Math.Abs(cellValues[index].X - cellValues[index - 1].X);
                var dy = Math.Abs(cellValues[index].Y - cellValues[index - 1].Y);
                if (dx + dy != 1) throw new ArgumentException("Loop cells must be cardinal and contiguous.", nameof(orderedCells));
            }
            if (sharedCellCount > cellValues.Count) throw new ArgumentOutOfRangeException(nameof(sharedCellCount));
            var backboneValues = new List<HorizontalBackboneSegmentId>(sourceBackboneSegmentIds ?? throw new ArgumentNullException(nameof(sourceBackboneSegmentIds)));
            var gatewayValues = new List<VerticalGatewayId>(sourceGatewayIds ?? throw new ArgumentNullException(nameof(sourceGatewayIds)));
            var junctionValues = new List<VerticalGatewayJunctionCell>(sourceType4Junctions ?? throw new ArgumentNullException(nameof(sourceType4Junctions)));
            backboneValues.Sort();
            gatewayValues.Sort();
            junctionValues.Sort((left, right) =>
            {
                var index = WorldGridIndex.ToIndex(left.Coord).CompareTo(WorldGridIndex.ToIndex(right.Coord));
                if (index != 0) return index;
                var l = left.OpensLeft.CompareTo(right.OpensLeft);
                return l != 0 ? l : left.OpensRight.CompareTo(right.OpensRight);
            });
            LoopId = loopId;
            StartTerminalId = startTerminalId;
            EndTerminalId = endTerminalId;
            SourceConnectorEdgeId = sourceConnectorEdgeId;
            SiteIdentity = siteIdentity;
            BiomeIdentity = biomeIdentity;
            CheckedTotalCost = checkedTotalCost;
            SharedCellCount = sharedCellCount;
            UniqueCellCount = cellValues.Count - sharedCellCount;
            HasReservationIntrusion = hasReservationIntrusion;
            HasInactiveIntrusion = hasInactiveIntrusion;
            HasMandatoryPathIntrusion = hasMandatoryPathIntrusion;
            IsInsideWorld = cellValues.TrueForAll(value => value.X >= 0 && value.X < WorldGenConstants.SectorColumns && value.Y >= 0 && value.Y < WorldGenConstants.SectorRows);
            IsIndependent = isIndependent;
            cells = new ReadOnlyCollection<SectorCoord>(cellValues);
            waypoints = new ReadOnlyCollection<SectorCoord>(BuildWaypoints(cellValues));
            backboneIds = new ReadOnlyCollection<HorizontalBackboneSegmentId>(backboneValues);
            gatewayIds = new ReadOnlyCollection<VerticalGatewayId>(gatewayValues);
            type4Junctions = new ReadOnlyCollection<VerticalGatewayJunctionCell>(junctionValues);
            FirstSectorIndex = IsInsideWorld ? WorldGridIndex.ToIndex(cellValues[0]) : int.MaxValue;
        }

        public MandatoryRouteLoopId LoopId { get; }
        public MandatoryRouteTerminalId StartTerminalId { get; }
        public MandatoryRouteTerminalId EndTerminalId { get; }
        public MandatoryConnectorEdgeId SourceConnectorEdgeId { get; }
        public IReadOnlyList<SectorCoord> OrderedCells => cells;
        public IReadOnlyList<SectorCoord> OrderedWaypoints => waypoints;
        public IReadOnlyList<HorizontalBackboneSegmentId> SourceBackboneSegmentIds => backboneIds;
        public IReadOnlyList<VerticalGatewayId> SourceGatewayIds => gatewayIds;
        public IReadOnlyList<VerticalGatewayJunctionCell> SourceType4Junctions => type4Junctions;
        public string SiteIdentity { get; }
        public string BiomeIdentity { get; }
        public int CheckedTotalCost { get; }
        public int SharedCellCount { get; }
        public int UniqueCellCount { get; }
        public int FirstSectorIndex { get; }
        public bool HasReservationIntrusion { get; }
        public bool HasInactiveIntrusion { get; }
        public bool HasMandatoryPathIntrusion { get; }
        public bool IsInsideWorld { get; }
        public bool IsIndependent { get; }
        public bool IsEligible => IsInsideWorld && !HasReservationIntrusion && !HasInactiveIntrusion && !HasMandatoryPathIntrusion && IsIndependent;

        private static List<SectorCoord> BuildWaypoints(IReadOnlyList<SectorCoord> values)
        {
            var result = new List<SectorCoord> { values[0] };
            var previousDx = 0;
            var previousDy = 0;
            for (var index = 1; index < values.Count; index++)
            {
                var dx = values[index].X - values[index - 1].X;
                var dy = values[index].Y - values[index - 1].Y;
                if (index > 1 && (dx != previousDx || dy != previousDy)) result.Add(values[index - 1]);
                previousDx = dx;
                previousDy = dy;
            }
            result.Add(values[values.Count - 1]);
            return result;
        }
    }
}
