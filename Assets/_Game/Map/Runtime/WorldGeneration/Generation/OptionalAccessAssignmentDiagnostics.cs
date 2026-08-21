using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalAccessAssignmentDiagnostics
    {
        public OptionalAccessAssignmentDiagnostics(
            int sourceRegionCount,
            int sourceCellCount,
            int sourceType0AssignmentCount,
            int assignmentCount,
            int clueCount,
            int basicCount,
            int toolCount,
            int environmentCount,
            int explosiveCount,
            int hiddenCount,
            int pickaxeCount,
            int shovelCount,
            int ropeCount,
            int hiddenCrackCount,
            int hiddenLightCount,
            int hiddenSoundCount,
            int perceptibleClueCount,
            int rewardPreviewReservationCount,
            int attachmentBoundaryBaseOpenCount,
            int rngDrawCount,
            int sourceMutationCount)
        {
            var values = new[]
            {
                sourceRegionCount, sourceCellCount, sourceType0AssignmentCount,
                assignmentCount, clueCount, basicCount, toolCount, environmentCount,
                explosiveCount, hiddenCount, pickaxeCount, shovelCount, ropeCount,
                hiddenCrackCount, hiddenLightCount, hiddenSoundCount, perceptibleClueCount,
                rewardPreviewReservationCount, attachmentBoundaryBaseOpenCount,
                rngDrawCount, sourceMutationCount
            };
            foreach (var value in values)
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(sourceRegionCount));
            }

            SourceRegionCount = sourceRegionCount;
            SourceCellCount = sourceCellCount;
            SourceType0AssignmentCount = sourceType0AssignmentCount;
            AssignmentCount = assignmentCount;
            ClueCount = clueCount;
            BasicCount = basicCount;
            ToolCount = toolCount;
            EnvironmentCount = environmentCount;
            ExplosiveCount = explosiveCount;
            HiddenCount = hiddenCount;
            PickaxeCount = pickaxeCount;
            ShovelCount = shovelCount;
            RopeCount = ropeCount;
            HiddenCrackCount = hiddenCrackCount;
            HiddenLightCount = hiddenLightCount;
            HiddenSoundCount = hiddenSoundCount;
            PerceptibleClueCount = perceptibleClueCount;
            RewardPreviewReservationCount = rewardPreviewReservationCount;
            AttachmentBoundaryBaseOpenCount = attachmentBoundaryBaseOpenCount;
            RngDrawCount = rngDrawCount;
            SourceMutationCount = sourceMutationCount;
        }

        public int SourceRegionCount { get; }
        public int SourceCellCount { get; }
        public int SourceType0AssignmentCount { get; }
        public int AssignmentCount { get; }
        public int ClueCount { get; }
        public int BasicCount { get; }
        public int ToolCount { get; }
        public int EnvironmentCount { get; }
        public int ExplosiveCount { get; }
        public int HiddenCount { get; }
        public int PickaxeCount { get; }
        public int ShovelCount { get; }
        public int RopeCount { get; }
        public int HiddenCrackCount { get; }
        public int HiddenLightCount { get; }
        public int HiddenSoundCount { get; }
        public int PerceptibleClueCount { get; }
        public int RewardPreviewReservationCount { get; }
        public int AttachmentBoundaryBaseOpenCount { get; }
        public int RngDrawCount { get; }
        public int SourceMutationCount { get; }
    }
}
