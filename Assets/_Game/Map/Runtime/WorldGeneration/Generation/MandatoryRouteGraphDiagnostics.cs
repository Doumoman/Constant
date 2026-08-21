namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteGraphDiagnostics
    {
        internal MandatoryRouteGraphDiagnostics(int terminalCount, int treeEdgeCount, int backboneSegmentCount, int gatewayPairCount,
            int conflictResolutionCount, int acceptedLoopCount, int nodeCount, int directedEdgeCount, int cellCount,
            int type1Count, int type2Count, int type3Count, int type4UdCount, int type4LudCount, int type4RudCount,
            int type4LrudCount, int reachableTerminalCount, int generatedEdgeRowCount, int generatedSectorCsvByteCount,
            int generatedEdgeCsvByteCount)
        {
            TerminalCount = terminalCount; TreeEdgeCount = treeEdgeCount; BackboneSegmentCount = backboneSegmentCount;
            GatewayPairCount = gatewayPairCount; ConflictResolutionCount = conflictResolutionCount; AcceptedLoopCount = acceptedLoopCount;
            NodeCount = nodeCount; DirectedEdgeCount = directedEdgeCount; CellCount = cellCount;
            Type1Count = type1Count; Type2Count = type2Count; Type3Count = type3Count;
            Type4UdCount = type4UdCount; Type4LudCount = type4LudCount; Type4RudCount = type4RudCount; Type4LrudCount = type4LrudCount;
            ReachableTerminalCount = reachableTerminalCount; GeneratedEdgeRowCount = generatedEdgeRowCount;
            GeneratedSectorCsvByteCount = generatedSectorCsvByteCount; GeneratedEdgeCsvByteCount = generatedEdgeCsvByteCount;
        }

        public int TerminalCount { get; }
        public int TreeEdgeCount { get; }
        public int BackboneSegmentCount { get; }
        public int GatewayPairCount { get; }
        public int ConflictResolutionCount { get; }
        public int AcceptedLoopCount { get; }
        public int NodeCount { get; }
        public int DirectedEdgeCount { get; }
        public int UndirectedEdgeCount => DirectedEdgeCount / 2;
        public int CellCount { get; }
        public int Type1Count { get; }
        public int Type2Count { get; }
        public int Type3Count { get; }
        public int Type4UdCount { get; }
        public int Type4LudCount { get; }
        public int Type4RudCount { get; }
        public int Type4LrudCount { get; }
        public int Type4Count => Type4UdCount + Type4LudCount + Type4RudCount + Type4LrudCount;
        public int ReachableTerminalCount { get; }
        public int GeneratedEdgeRowCount { get; }
        public int GeneratedSectorCsvByteCount { get; }
        public int GeneratedEdgeCsvByteCount { get; }
        public int RngDrawCount => 0;
        public int FileWriteCount => 0;
        public int ClockReadCount => 0;
        public int SourceMutationCount => 0;
    }
}
