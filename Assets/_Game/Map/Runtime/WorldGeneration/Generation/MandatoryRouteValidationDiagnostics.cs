namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteValidationDiagnostics
    {
        internal MandatoryRouteValidationDiagnostics(int evaluatedRuleCount, int evaluatedNodeCount, int evaluatedEdgeCount,
            int evaluatedCellCount, int generatedEdgeRowCount, int sourceMutationCount)
        {
            EvaluatedRuleCount = evaluatedRuleCount;
            EvaluatedNodeCount = evaluatedNodeCount;
            EvaluatedEdgeCount = evaluatedEdgeCount;
            EvaluatedCellCount = evaluatedCellCount;
            GeneratedEdgeRowCount = generatedEdgeRowCount;
            SourceMutationCount = sourceMutationCount;
        }

        public int EvaluatedRuleCount { get; }
        public int EvaluatedNodeCount { get; }
        public int EvaluatedEdgeCount { get; }
        public int EvaluatedCellCount { get; }
        public int GeneratedEdgeRowCount { get; }
        public int SourceMutationCount { get; }
        public int RngDrawCount => 0;
        public int FileReadCount => 0;
        public int FileWriteCount => 0;
        public int ClockReadCount => 0;
    }
}
