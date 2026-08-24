namespace StarNight.Map.WorldGeneration.Boundaries
{
    public enum MoonpalaceBoundaryResolveIssue
    {
        None = 0,
        MissingIndex,
        MissingRequest,
        InvalidFromBiome,
        InvalidToBiome,
        SelfPair,
        InvalidProfile,
        InvalidOrientation,
        InvalidRouteRole,
        InvalidEdgeSignature,
        NoCandidates,
    }
}
