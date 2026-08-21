namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class UpDownConflictDiagnostics
    {
        internal UpDownConflictDiagnostics(
            int gatewayPairCount,
            int candidateCount,
            int type4ExpressibleCount,
            int conflictCount,
            int resolvedCount,
            int unresolvedCount,
            int adjacentCandidateEvaluationCount,
            int preservedLeftCount,
            int preservedRightCount)
        {
            GatewayPairCount = gatewayPairCount;
            CandidateCount = candidateCount;
            Type4ExpressibleCount = type4ExpressibleCount;
            ConflictCount = conflictCount;
            ResolvedCount = resolvedCount;
            UnresolvedCount = unresolvedCount;
            AdjacentCandidateEvaluationCount = adjacentCandidateEvaluationCount;
            PreservedLeftCount = preservedLeftCount;
            PreservedRightCount = preservedRightCount;
        }

        public int GatewayPairCount { get; }
        public int CandidateCount { get; }
        public int Type4ExpressibleCount { get; }
        public int ConflictCount { get; }
        public int ResolvedCount { get; }
        public int UnresolvedCount { get; }
        public int AdjacentCandidateEvaluationCount { get; }
        public int PreservedLeftCount { get; }
        public int PreservedRightCount { get; }
        public int RngDrawCount => 0;
        public int FileWriteCount => 0;
        public int RouteMaskWriteCount => 0;
        public int GraphWriteCount => 0;
        public int SourceMutationCount => 0;
    }
}
