namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteValidationSummary
    {
        internal MandatoryRouteValidationSummary(int ruleCount, int passedRuleCount, int failedRuleCount,
            int violationCount, int errorCount, int warningCount, int reachableTerminalCount, int representedLoopCount,
            int type1Count, int type2Count, int type3Count, int type4UdCount, int type4LudCount, int type4RudCount,
            int type4LrudCount, int directedEdgeCount, int undirectedEdgeCount, int generatedSectorCsvByteCount,
            int generatedEdgeCsvByteCount, int generatedEdgeRowCount)
        {
            RuleCount = ruleCount; PassedRuleCount = passedRuleCount; FailedRuleCount = failedRuleCount;
            ViolationCount = violationCount; ErrorCount = errorCount; WarningCount = warningCount;
            ReachableTerminalCount = reachableTerminalCount; RepresentedLoopCount = representedLoopCount;
            Type1Count = type1Count; Type2Count = type2Count; Type3Count = type3Count;
            Type4UdCount = type4UdCount; Type4LudCount = type4LudCount; Type4RudCount = type4RudCount; Type4LrudCount = type4LrudCount;
            DirectedEdgeCount = directedEdgeCount; UndirectedEdgeCount = undirectedEdgeCount;
            GeneratedSectorCsvByteCount = generatedSectorCsvByteCount; GeneratedEdgeCsvByteCount = generatedEdgeCsvByteCount;
            GeneratedEdgeRowCount = generatedEdgeRowCount;
        }

        public int RuleCount { get; }
        public int PassedRuleCount { get; }
        public int FailedRuleCount { get; }
        public int ViolationCount { get; }
        public int ErrorCount { get; }
        public int WarningCount { get; }
        public int ReachableTerminalCount { get; }
        public int RepresentedLoopCount { get; }
        public int Type1Count { get; }
        public int Type2Count { get; }
        public int Type3Count { get; }
        public int Type4UdCount { get; }
        public int Type4LudCount { get; }
        public int Type4RudCount { get; }
        public int Type4LrudCount { get; }
        public int Type4Count => Type4UdCount + Type4LudCount + Type4RudCount + Type4LrudCount;
        public int DirectedEdgeCount { get; }
        public int UndirectedEdgeCount { get; }
        public int GeneratedSectorCsvByteCount { get; }
        public int GeneratedEdgeCsvByteCount { get; }
        public int GeneratedEdgeRowCount { get; }
    }
}
