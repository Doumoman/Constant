using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalAccessClue
    {
        public OptionalAccessClue(
            OptionalAccessClueId clueId,
            OptionalRegionId regionId,
            OptionalAccessClueKind kind,
            int attachmentOrder,
            bool isPerceptibleFromMandatory,
            bool requiresRewardPreview)
        {
            if (!clueId.IsValid)
            {
                throw new ArgumentException("Clue ID must be valid.", nameof(clueId));
            }

            if (!regionId.IsValid)
            {
                throw new ArgumentException("Region ID must be valid.", nameof(regionId));
            }

            RequireClueKind(kind, nameof(kind));
            if (attachmentOrder < 0 || attachmentOrder > 9999)
            {
                throw new ArgumentOutOfRangeException(nameof(attachmentOrder));
            }

            var expectedPrefix = "CLUE_" + regionId.Value + "_";
            if (!clueId.Value.StartsWith(expectedPrefix, StringComparison.Ordinal))
            {
                throw new ArgumentException("Clue ID must preserve the source region identity.", nameof(clueId));
            }

            if (!isPerceptibleFromMandatory)
            {
                throw new ArgumentException("Optional access clues must be perceptible from the mandatory side.", nameof(isPerceptibleFromMandatory));
            }

            if (requiresRewardPreview != (kind == OptionalAccessClueKind.ExplosiveRewardPreview))
            {
                throw new ArgumentException("Only explosive reward-preview clues reserve a reward preview.", nameof(requiresRewardPreview));
            }

            ClueId = clueId;
            RegionId = regionId;
            Kind = kind;
            AttachmentOrder = attachmentOrder;
            IsPerceptibleFromMandatory = isPerceptibleFromMandatory;
            RequiresRewardPreview = requiresRewardPreview;
        }

        public OptionalAccessClueId ClueId { get; }
        public OptionalRegionId RegionId { get; }
        public OptionalAccessClueKind Kind { get; }
        public int AttachmentOrder { get; }
        public bool IsPerceptibleFromMandatory { get; }
        public bool RequiresRewardPreview { get; }

        internal static void RequireClueKind(OptionalAccessClueKind value, string parameterName)
        {
            switch (value)
            {
                case OptionalAccessClueKind.BasicOpening:
                case OptionalAccessClueKind.ToolSurface:
                case OptionalAccessClueKind.EnvironmentDevice:
                case OptionalAccessClueKind.ExplosiveRewardPreview:
                case OptionalAccessClueKind.HiddenCrack:
                case OptionalAccessClueKind.HiddenLight:
                case OptionalAccessClueKind.HiddenSound:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
