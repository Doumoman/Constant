namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public enum OptionalRegionOverlayStatus
    {
        Completed,
        InvalidInput,
        InvalidSettings,
        InvalidSource,
        InvalidValidationReport
    }

    public enum OptionalRegionOverlayLayer
    {
        BaseRole,
        AccessRule,
        Depth,
        AttachmentContact,
        ReturnWitness,
        RewardTier,
        InactiveKind,
        ValidationIssue
    }

    public enum OptionalRegionOverlayCellKind
    {
        Mandatory,
        ReservedSite,
        Type0,
        InactiveInterior,
        InactiveDecorative
    }

    public enum OptionalRegionOverlayConnectionKind
    {
        AttachmentContact,
        ReturnWitness
    }

    public enum OptionalRegionOverlayColorToken
    {
        Mandatory,
        ReservedSite,
        Type0Basic,
        Type0Tool,
        Type0Environment,
        Type0Explosive,
        Type0Hidden,
        RewardLow,
        RewardMedium,
        RewardHigh,
        RewardUnique,
        ReturnBacktrack,
        InactiveInterior,
        InactiveDecorative,
        ValidationIssue
    }
}
