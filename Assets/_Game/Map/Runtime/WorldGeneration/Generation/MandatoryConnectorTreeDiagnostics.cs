namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryConnectorTreeDiagnostics
    {
        public MandatoryConnectorTreeDiagnostics(int terminalCount, int startTerminalCount, int siteEntryTerminalCount, int routeMaskCount, int candidateEdgeCount, int treeEdgeCount, int totalTreeCost, int connectedComponentCount, int coveredTerminalCount, int sharedApproachCandidateCount, int rngDrawCount, int sourceMutationCount)
        {
            TerminalCount = terminalCount;
            StartTerminalCount = startTerminalCount;
            SiteEntryTerminalCount = siteEntryTerminalCount;
            RouteMaskCount = routeMaskCount;
            CandidateEdgeCount = candidateEdgeCount;
            TreeEdgeCount = treeEdgeCount;
            TotalTreeCost = totalTreeCost;
            ConnectedComponentCount = connectedComponentCount;
            CoveredTerminalCount = coveredTerminalCount;
            SharedApproachCandidateCount = sharedApproachCandidateCount;
            RngDrawCount = rngDrawCount;
            SourceMutationCount = sourceMutationCount;
        }

        public int TerminalCount { get; }
        public int StartTerminalCount { get; }
        public int SiteEntryTerminalCount { get; }
        public int RouteMaskCount { get; }
        public int CandidateEdgeCount { get; }
        public int TreeEdgeCount { get; }
        public int TotalTreeCost { get; }
        public int ConnectedComponentCount { get; }
        public int CoveredTerminalCount { get; }
        public int SharedApproachCandidateCount { get; }
        public int RngDrawCount { get; }
        public int SourceMutationCount { get; }
    }
}
