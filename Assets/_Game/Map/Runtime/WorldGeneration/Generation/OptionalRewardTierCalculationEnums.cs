namespace StarNight.Map.WorldGeneration.Generation
{
    public enum OptionalRewardTierCalculationStatus
    {
        Completed,
        InvalidInput,
        InvalidSettings,
        InvalidSource,
        ArithmeticOverflow
    }

    public enum OptionalRewardTierCalculationErrorCode
    {
        NullInput,
        InvalidStatus,
        InvalidDigest,
        SourceMismatch,
        InvalidAccounting,
        MissingRegion,
        DuplicateRegion,
        InvalidDepth,
        InvalidMatrix,
        OpenAttachmentBoundary,
        ArithmeticOverflow
    }
}
