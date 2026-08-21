using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class HorizontalBackboneDiagnostics
    {
        public HorizontalBackboneDiagnostics(
            int treeEdgeCount, int segmentCount, int sameRowSegmentCount, int gatewayPendingSegmentCount,
            int totalHorizontalCellCount, int reservedEndpointCellCount, int forbiddenReservedMiddleCellCount,
            int worldBoundsViolationCount, int openUpDownCount, int routeGraphEdgeCount,
            int generatedCsvRowCount, int rngDrawCount, int sourceMutationCount)
        {
            var values = new[] { treeEdgeCount, segmentCount, sameRowSegmentCount, gatewayPendingSegmentCount,
                totalHorizontalCellCount, reservedEndpointCellCount, forbiddenReservedMiddleCellCount,
                worldBoundsViolationCount, openUpDownCount, routeGraphEdgeCount, generatedCsvRowCount,
                rngDrawCount, sourceMutationCount };
            foreach (var value in values) if (value < 0) throw new ArgumentOutOfRangeException(nameof(values));
            TreeEdgeCount = treeEdgeCount;
            SegmentCount = segmentCount;
            SameRowSegmentCount = sameRowSegmentCount;
            GatewayPendingSegmentCount = gatewayPendingSegmentCount;
            TotalHorizontalCellCount = totalHorizontalCellCount;
            ReservedEndpointCellCount = reservedEndpointCellCount;
            ForbiddenReservedMiddleCellCount = forbiddenReservedMiddleCellCount;
            WorldBoundsViolationCount = worldBoundsViolationCount;
            OpenUpDownCount = openUpDownCount;
            RouteGraphEdgeCount = routeGraphEdgeCount;
            GeneratedCsvRowCount = generatedCsvRowCount;
            RngDrawCount = rngDrawCount;
            SourceMutationCount = sourceMutationCount;
        }

        public int TreeEdgeCount { get; }
        public int SegmentCount { get; }
        public int SameRowSegmentCount { get; }
        public int GatewayPendingSegmentCount { get; }
        public int TotalHorizontalCellCount { get; }
        public int ReservedEndpointCellCount { get; }
        public int ForbiddenReservedMiddleCellCount { get; }
        public int WorldBoundsViolationCount { get; }
        public int OpenUpDownCount { get; }
        public int RouteGraphEdgeCount { get; }
        public int GeneratedCsvRowCount { get; }
        public int RngDrawCount { get; }
        public int SourceMutationCount { get; }
    }
}
