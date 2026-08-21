namespace StarNight.Map.WorldGeneration.Generation
{
    public enum OptionalRegionValidationStatus
    {
        Valid,
        InvalidInput,
        InvalidSettings,
        InvalidSource,
        InvalidTopology,
        InvalidAccounting,
        InvalidRules
    }

    public enum OptionalRegionValidationIssueCode
    {
        NullInput,
        InvalidStatus,
        InvalidDigest,
        SourceMismatch,
        InvalidWorld,
        InvalidMandatoryGraph,
        InvalidOptionalRegionSnapshot,
        InvalidType0Assignment,
        InvalidAccessAssignment,
        InvalidRewardAssignment,
        InvalidReturnPolicy,
        InvalidInactiveBufferAssignment,
        MissingAccessRule,
        MissingVisibleClue,
        MissingRewardTier,
        MissingReturnPolicy,
        NonReturnableOptionalCell,
        MandatoryRewardAssigned,
        Type0LeftRightOpen,
        OpenEdgeToInactive,
        InactiveAccountingMismatch,
        ReservedAdapterMismatch,
        DuplicateRegion,
        DuplicateSector,
        RegionIdentityMismatch,
        RngConsumed,
        SourceMutation
    }
}
