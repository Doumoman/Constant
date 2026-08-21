namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class VerticalGatewayDiagnostics
    {
        internal VerticalGatewayDiagnostics(
            int horizontalSegmentCount,
            int pendingSegmentCount,
            int gatewayPairCount,
            int type4JunctionCellCount,
            int conflictPendingCount,
            int totalVerticalSpanCellCount,
            int reservedEndpointCount,
            int totalCost)
        {
            HorizontalSegmentCount = horizontalSegmentCount;
            PendingSegmentCount = pendingSegmentCount;
            GatewayPairCount = gatewayPairCount;
            UpperAnchorCount = gatewayPairCount;
            LowerAnchorCount = gatewayPairCount;
            Type4JunctionCellCount = type4JunctionCellCount;
            ConflictPendingCount = conflictPendingCount;
            TotalVerticalSpanCellCount = totalVerticalSpanCellCount;
            ReservedEndpointCount = reservedEndpointCount;
            ReservedMiddleCellCount = 0;
            WorldBoundsViolationCount = 0;
            OpenUpCount = gatewayPairCount + type4JunctionCellCount;
            OpenDownCount = gatewayPairCount + type4JunctionCellCount;
            RouteGraphEdgeCount = 0;
            GeneratedCsvRowCount = 0;
            SectorRouteMaskWriteCount = 0;
            RngDrawCount = 0;
            SourceMutationCount = 0;
            TotalCost = totalCost;
        }

        public int HorizontalSegmentCount { get; }
        public int PendingSegmentCount { get; }
        public int GatewayPairCount { get; }
        public int UpperAnchorCount { get; }
        public int LowerAnchorCount { get; }
        public int Type4JunctionCellCount { get; }
        public int ConflictPendingCount { get; }
        public int TotalVerticalSpanCellCount { get; }
        public int ReservedEndpointCount { get; }
        public int ReservedMiddleCellCount { get; }
        public int WorldBoundsViolationCount { get; }
        public int OpenUpCount { get; }
        public int OpenDownCount { get; }
        public int RouteGraphEdgeCount { get; }
        public int GeneratedCsvRowCount { get; }
        public int SectorRouteMaskWriteCount { get; }
        public int RngDrawCount { get; }
        public int SourceMutationCount { get; }
        public int TotalCost { get; }
    }
}
