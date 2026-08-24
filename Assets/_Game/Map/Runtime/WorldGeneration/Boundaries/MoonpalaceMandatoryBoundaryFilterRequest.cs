namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceMandatoryBoundaryFilterRequest
    {
        public MoonpalaceMandatoryBoundaryFilterRequest(
            MoonpalaceBoundaryResolveRequest resolveRequest,
            MoonpalaceBoundaryCandidateIndex candidateIndex,
            bool mandatoryRouteBoundary)
        {
            ResolveRequest = resolveRequest;
            CandidateIndex = candidateIndex;
            MandatoryRouteBoundary = mandatoryRouteBoundary;
        }

        public MoonpalaceBoundaryResolveRequest ResolveRequest { get; }
        public MoonpalaceBoundaryCandidateIndex CandidateIndex { get; }
        public bool MandatoryRouteBoundary { get; }
    }
}
