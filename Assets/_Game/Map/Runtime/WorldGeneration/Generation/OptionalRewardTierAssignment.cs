using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalRewardTierAssignment
    {
        internal OptionalRewardTierAssignment(
            OptionalRegionId regionId,
            int regionOrdinal,
            int attachmentOrder,
            OptionalAccessClueId clueId,
            OptionalRegionAccessRule accessRule,
            int maxDepth,
            int toolCostTier,
            int explosiveFuelCost,
            int hiddenClueDifficulty,
            int depthScore,
            int toolCostScore,
            int explosiveFuelScore,
            int hiddenClueScore,
            int rewardScore,
            OptionalRewardTier rewardTier,
            bool requiresPartialRewardPreview)
        {
            if (!regionId.IsValid) throw new ArgumentException("Region ID must be valid.", nameof(regionId));
            if (regionOrdinal < 0 || regionOrdinal > 9999) throw new ArgumentOutOfRangeException(nameof(regionOrdinal));
            if (attachmentOrder < 0 || attachmentOrder > 9999) throw new ArgumentOutOfRangeException(nameof(attachmentOrder));
            if (!clueId.IsValid) throw new ArgumentException("Clue ID must be valid.", nameof(clueId));
            if (maxDepth < 1 || maxDepth > 4) throw new ArgumentOutOfRangeException(nameof(maxDepth));
            if (depthScore < 0 || toolCostScore < 0 || explosiveFuelScore < 0 || hiddenClueScore < 0)
                throw new ArgumentOutOfRangeException(nameof(depthScore));
            if (rewardTier == OptionalRewardTier.None ||
                !Enum.IsDefined(typeof(OptionalRewardTier), rewardTier))
                throw new ArgumentOutOfRangeException(nameof(rewardTier));

            ValidateMatrix(
                accessRule,
                toolCostTier,
                explosiveFuelCost,
                hiddenClueDifficulty,
                toolCostScore,
                explosiveFuelScore,
                hiddenClueScore,
                requiresPartialRewardPreview);

            int expectedScore;
            checked
            {
                expectedScore = depthScore + toolCostScore + explosiveFuelScore + hiddenClueScore;
            }
            if (rewardScore != expectedScore)
                throw new ArgumentException("Reward score must equal the sum of all score components.", nameof(rewardScore));

            RegionId = regionId;
            RegionOrdinal = regionOrdinal;
            AttachmentOrder = attachmentOrder;
            ClueId = clueId;
            AccessRule = accessRule;
            MaxDepth = maxDepth;
            ToolCostTier = toolCostTier;
            ExplosiveFuelCost = explosiveFuelCost;
            HiddenClueDifficulty = hiddenClueDifficulty;
            DepthScore = depthScore;
            ToolCostScore = toolCostScore;
            ExplosiveFuelScore = explosiveFuelScore;
            HiddenClueScore = hiddenClueScore;
            RewardScore = rewardScore;
            RewardTier = rewardTier;
            RequiresPartialRewardPreview = requiresPartialRewardPreview;
        }

        public OptionalRegionId RegionId { get; }
        public int RegionOrdinal { get; }
        public int AttachmentOrder { get; }
        public OptionalAccessClueId ClueId { get; }
        public OptionalRegionAccessRule AccessRule { get; }
        public int MaxDepth { get; }
        public int ToolCostTier { get; }
        public int ExplosiveFuelCost { get; }
        public int HiddenClueDifficulty { get; }
        public int DepthScore { get; }
        public int ToolCostScore { get; }
        public int ExplosiveFuelScore { get; }
        public int HiddenClueScore { get; }
        public int RewardScore { get; }
        public OptionalRewardTier RewardTier { get; }
        public bool RequiresPartialRewardPreview { get; }

        private static void ValidateMatrix(
            OptionalRegionAccessRule accessRule,
            int toolCostTier,
            int explosiveFuelCost,
            int hiddenClueDifficulty,
            int toolCostScore,
            int explosiveFuelScore,
            int hiddenClueScore,
            bool preview)
        {
            switch (accessRule)
            {
                case OptionalRegionAccessRule.Basic:
                case OptionalRegionAccessRule.Environment:
                    Require(toolCostTier == 0 && explosiveFuelCost == 0 && hiddenClueDifficulty == 0 &&
                            toolCostScore == 0 && explosiveFuelScore == 0 && hiddenClueScore == 0 && !preview);
                    return;
                case OptionalRegionAccessRule.Tool:
                    Require(toolCostTier >= 1 && toolCostTier <= 4 && explosiveFuelCost == 0 && hiddenClueDifficulty == 0 &&
                            toolCostScore == toolCostTier && explosiveFuelScore == 0 && hiddenClueScore == 0 && !preview);
                    return;
                case OptionalRegionAccessRule.Explosive:
                    Require(toolCostTier == 0 && explosiveFuelCost >= 1 && explosiveFuelCost <= 100 && hiddenClueDifficulty == 0 &&
                            toolCostScore == 0 && explosiveFuelScore >= 0 && hiddenClueScore == 0 && preview);
                    return;
                case OptionalRegionAccessRule.Hidden:
                    Require(toolCostTier == 0 && explosiveFuelCost == 0 && hiddenClueDifficulty >= 1 && hiddenClueDifficulty <= 4 &&
                            toolCostScore == 0 && explosiveFuelScore == 0 && hiddenClueScore == hiddenClueDifficulty && !preview);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(accessRule));
            }
        }

        private static void Require(bool condition)
        {
            if (!condition)
                throw new ArgumentException("Optional reward assignment does not preserve the access-cost matrix.");
        }
    }
}
