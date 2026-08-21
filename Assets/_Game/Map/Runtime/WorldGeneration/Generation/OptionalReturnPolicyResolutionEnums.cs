namespace StarNight.Map.WorldGeneration.Generation
{
    public enum OptionalReturnPolicyResolutionStatus
    {
        Completed,
        InvalidInput,
        InvalidSettings,
        InvalidSource,
        InvalidTopology,
        UnsupportedReturnRequirement
    }

    public enum OptionalReturnPolicyResolutionErrorCode
    {
        NullInput,
        InvalidStatus,
        InvalidDigest,
        SourceMismatch,
        InvalidAccounting,
        MissingRegion,
        DuplicateRegion,
        InvalidAttachment,
        InvalidBaseEdge,
        NonReciprocalBaseEdge,
        UnreachableCell,
        PathLimitExceeded,
        UnsupportedReturnRequirement
    }
}
