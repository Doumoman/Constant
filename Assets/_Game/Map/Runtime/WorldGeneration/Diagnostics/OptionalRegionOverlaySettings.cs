namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public sealed class OptionalRegionOverlaySettings
    {
        public OptionalRegionOverlaySettings(
            bool showAccessRuleColors,
            bool showDepthLabels,
            bool showAttachmentContacts,
            bool showReturnWitness,
            bool showRewardTierMarkers,
            bool showInactiveKinds,
            bool showValidationIssues,
            bool requireValidReport)
        {
            ShowAccessRuleColors = showAccessRuleColors;
            ShowDepthLabels = showDepthLabels;
            ShowAttachmentContacts = showAttachmentContacts;
            ShowReturnWitness = showReturnWitness;
            ShowRewardTierMarkers = showRewardTierMarkers;
            ShowInactiveKinds = showInactiveKinds;
            ShowValidationIssues = showValidationIssues;
            RequireValidReport = requireValidReport;
        }

        public bool ShowAccessRuleColors { get; }
        public bool ShowDepthLabels { get; }
        public bool ShowAttachmentContacts { get; }
        public bool ShowReturnWitness { get; }
        public bool ShowRewardTierMarkers { get; }
        public bool ShowInactiveKinds { get; }
        public bool ShowValidationIssues { get; }
        public bool RequireValidReport { get; }

        public bool IsApproved =>
            ShowAccessRuleColors && ShowDepthLabels && ShowAttachmentContacts &&
            ShowReturnWitness && ShowRewardTierMarkers && ShowInactiveKinds &&
            ShowValidationIssues && RequireValidReport;

        public static OptionalRegionOverlaySettings CreateApproved()
        {
            return new OptionalRegionOverlaySettings(true, true, true, true, true, true, true, true);
        }
    }
}
