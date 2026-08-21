namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteLoopDiagnostics
    {
        internal MandatoryRouteLoopDiagnostics(
            int terminalCount,
            int treeEdgeCount,
            int candidateCount,
            int eligibleCandidateCount,
            int acceptedLoopCount,
            int independentLoopCount,
            int boundsRejectedCount,
            int reservationRejectedCount,
            int inactiveRejectedCount,
            int mandatoryPathRejectedCount,
            int overlapRejectedCount,
            int unresolvedLoopCount)
        {
            TerminalCount = terminalCount;
            TreeEdgeCount = treeEdgeCount;
            CandidateCount = candidateCount;
            EligibleCandidateCount = eligibleCandidateCount;
            AcceptedLoopCount = acceptedLoopCount;
            IndependentLoopCount = independentLoopCount;
            BoundsRejectedCount = boundsRejectedCount;
            ReservationRejectedCount = reservationRejectedCount;
            InactiveRejectedCount = inactiveRejectedCount;
            MandatoryPathRejectedCount = mandatoryPathRejectedCount;
            OverlapRejectedCount = overlapRejectedCount;
            UnresolvedLoopCount = unresolvedLoopCount;
        }

        public int TerminalCount { get; }
        public int TreeEdgeCount { get; }
        public int CandidateCount { get; }
        public int EligibleCandidateCount { get; }
        public int AcceptedLoopCount { get; }
        public int IndependentLoopCount { get; }
        public int BoundsRejectedCount { get; }
        public int ReservationRejectedCount { get; }
        public int InactiveRejectedCount { get; }
        public int MandatoryPathRejectedCount { get; }
        public int OverlapRejectedCount { get; }
        public int UnresolvedLoopCount { get; }
        public int RngDrawCount => 0;
        public int FileWriteCount => 0;
        public int GraphWriteCount => 0;
        public int GeneratedCsvRowCount => 0;
        public int RouteMaskWriteCount => 0;
        public int SourceMutationCount => 0;
    }
}
