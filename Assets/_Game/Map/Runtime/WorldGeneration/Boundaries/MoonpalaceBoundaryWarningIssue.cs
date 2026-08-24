namespace StarNight.Map.WorldGeneration.Boundaries
{
    public enum MoonpalaceBoundaryWarningIssue
    {
        InvalidRequest = 0,
        MissingBoundaryProfile = 1,
        InvalidWarningLength = 2,
        InsufficientWarningLength = 3,
        InsufficientMarkerCategories = 4,
        UnknownMarkerCategory = 5,
        DuplicateMarkerCategory = 6,
        TargetBiomeMismatch = 7,
    }
}
