using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalRewardTierDiagnostics
    {
        public OptionalRewardTierDiagnostics(
            int sourceRegionCount,
            int sourceType0CellAssignmentCount,
            int sourceAccessAssignmentCount,
            int tierAssignmentCount,
            int lowCount,
            int mediumCount,
            int highCount,
            int uniqueCount,
            int depthContributionTotal,
            int toolContributionTotal,
            int explosiveContributionTotal,
            int hiddenContributionTotal,
            int rewardScoreMinimum,
            int rewardScoreMaximum,
            int rewardPreviewReservationCount,
            int mandatoryRewardSelectionCount,
            int rngDrawCount,
            int sourceMutationCount)
        {
            var values = new[]
            {
                sourceRegionCount, sourceType0CellAssignmentCount, sourceAccessAssignmentCount,
                tierAssignmentCount, lowCount, mediumCount, highCount, uniqueCount,
                depthContributionTotal, toolContributionTotal, explosiveContributionTotal,
                hiddenContributionTotal, rewardScoreMinimum, rewardScoreMaximum,
                rewardPreviewReservationCount, mandatoryRewardSelectionCount,
                rngDrawCount, sourceMutationCount
            };
            foreach (var value in values)
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(sourceRegionCount));
            }
            if (lowCount + mediumCount + highCount + uniqueCount != tierAssignmentCount)
                throw new ArgumentException("Tier counts must equal the published assignment count.");
            if (tierAssignmentCount == 0)
            {
                if (rewardScoreMinimum != 0 || rewardScoreMaximum != 0)
                    throw new ArgumentException("Empty diagnostics require zero score bounds.");
            }
            else if (rewardScoreMinimum > rewardScoreMaximum)
            {
                throw new ArgumentException("Reward score minimum cannot exceed maximum.");
            }

            SourceRegionCount = sourceRegionCount;
            SourceType0CellAssignmentCount = sourceType0CellAssignmentCount;
            SourceAccessAssignmentCount = sourceAccessAssignmentCount;
            TierAssignmentCount = tierAssignmentCount;
            LowCount = lowCount;
            MediumCount = mediumCount;
            HighCount = highCount;
            UniqueCount = uniqueCount;
            DepthContributionTotal = depthContributionTotal;
            ToolContributionTotal = toolContributionTotal;
            ExplosiveContributionTotal = explosiveContributionTotal;
            HiddenContributionTotal = hiddenContributionTotal;
            RewardScoreMinimum = rewardScoreMinimum;
            RewardScoreMaximum = rewardScoreMaximum;
            RewardPreviewReservationCount = rewardPreviewReservationCount;
            MandatoryRewardSelectionCount = mandatoryRewardSelectionCount;
            RngDrawCount = rngDrawCount;
            SourceMutationCount = sourceMutationCount;
        }

        public int SourceRegionCount { get; }
        public int SourceType0CellAssignmentCount { get; }
        public int SourceAccessAssignmentCount { get; }
        public int TierAssignmentCount { get; }
        public int LowCount { get; }
        public int MediumCount { get; }
        public int HighCount { get; }
        public int UniqueCount { get; }
        public int DepthContributionTotal { get; }
        public int ToolContributionTotal { get; }
        public int ExplosiveContributionTotal { get; }
        public int HiddenContributionTotal { get; }
        public int RewardScoreMinimum { get; }
        public int RewardScoreMaximum { get; }
        public int RewardPreviewReservationCount { get; }
        public int MandatoryRewardSelectionCount { get; }
        public int RngDrawCount { get; }
        public int SourceMutationCount { get; }
    }
}
