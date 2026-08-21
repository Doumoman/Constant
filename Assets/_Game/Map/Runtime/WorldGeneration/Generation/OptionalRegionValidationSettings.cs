namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalRegionValidationSettings
    {
        public OptionalRegionValidationSettings(
            bool requireMandatoryGraphIdentity,
            bool requireSourceDigests,
            bool requireRegionIdentity,
            bool requireType0NoLeftRight,
            bool requireReturnability,
            bool requireVisibleClues,
            bool forbidMandatoryRewards,
            bool requireInactiveFullAccounting,
            bool requireNoRngOrSourceMutation)
        {
            RequireMandatoryGraphIdentity = requireMandatoryGraphIdentity;
            RequireSourceDigests = requireSourceDigests;
            RequireRegionIdentity = requireRegionIdentity;
            RequireType0NoLeftRight = requireType0NoLeftRight;
            RequireReturnability = requireReturnability;
            RequireVisibleClues = requireVisibleClues;
            ForbidMandatoryRewards = forbidMandatoryRewards;
            RequireInactiveFullAccounting = requireInactiveFullAccounting;
            RequireNoRngOrSourceMutation = requireNoRngOrSourceMutation;
        }

        public bool RequireMandatoryGraphIdentity { get; }
        public bool RequireSourceDigests { get; }
        public bool RequireRegionIdentity { get; }
        public bool RequireType0NoLeftRight { get; }
        public bool RequireReturnability { get; }
        public bool RequireVisibleClues { get; }
        public bool ForbidMandatoryRewards { get; }
        public bool RequireInactiveFullAccounting { get; }
        public bool RequireNoRngOrSourceMutation { get; }

        internal bool IsApproved =>
            RequireMandatoryGraphIdentity && RequireSourceDigests && RequireRegionIdentity &&
            RequireType0NoLeftRight && RequireReturnability && RequireVisibleClues &&
            ForbidMandatoryRewards && RequireInactiveFullAccounting && RequireNoRngOrSourceMutation;
    }
}
