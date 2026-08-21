using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalRegionValidationDiagnostics
    {
        public OptionalRegionValidationDiagnostics(
            int worldSectorCount,
            int mandatoryRouteCellCount,
            int optionalRegionCount,
            int type0CellCount,
            int accessAssignmentCount,
            int visibleClueCount,
            int rewardAssignmentCount,
            int mandatoryRewardAssignmentCount,
            int returnAssignmentCount,
            int returnableCellCount,
            int nonReturnableCellCount,
            int inactiveBufferAssignmentCount,
            int decorativeBoundaryCount,
            int interiorInactiveCount,
            int protectedUnionCount,
            int approvedReservedAdapterOverlapCount,
            int openEdgeToInactiveCount,
            int type0LeftRightOpenCount,
            int missingClueCount,
            int missingReturnPolicyCount,
            int issueCount,
            int rngDrawCount,
            int sourceMutationCount)
        {
            var values = new[]
            {
                worldSectorCount, mandatoryRouteCellCount, optionalRegionCount, type0CellCount,
                accessAssignmentCount, visibleClueCount, rewardAssignmentCount,
                mandatoryRewardAssignmentCount, returnAssignmentCount, returnableCellCount,
                nonReturnableCellCount, inactiveBufferAssignmentCount, decorativeBoundaryCount,
                interiorInactiveCount, protectedUnionCount, approvedReservedAdapterOverlapCount,
                openEdgeToInactiveCount, type0LeftRightOpenCount, missingClueCount,
                missingReturnPolicyCount, issueCount, rngDrawCount, sourceMutationCount
            };
            foreach (var value in values)
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(worldSectorCount));
            }

            WorldSectorCount = worldSectorCount;
            MandatoryRouteCellCount = mandatoryRouteCellCount;
            OptionalRegionCount = optionalRegionCount;
            Type0CellCount = type0CellCount;
            AccessAssignmentCount = accessAssignmentCount;
            VisibleClueCount = visibleClueCount;
            RewardAssignmentCount = rewardAssignmentCount;
            MandatoryRewardAssignmentCount = mandatoryRewardAssignmentCount;
            ReturnAssignmentCount = returnAssignmentCount;
            ReturnableCellCount = returnableCellCount;
            NonReturnableCellCount = nonReturnableCellCount;
            InactiveBufferAssignmentCount = inactiveBufferAssignmentCount;
            DecorativeBoundaryCount = decorativeBoundaryCount;
            InteriorInactiveCount = interiorInactiveCount;
            ProtectedUnionCount = protectedUnionCount;
            ApprovedReservedAdapterOverlapCount = approvedReservedAdapterOverlapCount;
            OpenEdgeToInactiveCount = openEdgeToInactiveCount;
            Type0LeftRightOpenCount = type0LeftRightOpenCount;
            MissingClueCount = missingClueCount;
            MissingReturnPolicyCount = missingReturnPolicyCount;
            IssueCount = issueCount;
            RngDrawCount = rngDrawCount;
            SourceMutationCount = sourceMutationCount;
        }

        public int WorldSectorCount { get; }
        public int MandatoryRouteCellCount { get; }
        public int OptionalRegionCount { get; }
        public int Type0CellCount { get; }
        public int AccessAssignmentCount { get; }
        public int VisibleClueCount { get; }
        public int RewardAssignmentCount { get; }
        public int MandatoryRewardAssignmentCount { get; }
        public int ReturnAssignmentCount { get; }
        public int ReturnableCellCount { get; }
        public int NonReturnableCellCount { get; }
        public int InactiveBufferAssignmentCount { get; }
        public int DecorativeBoundaryCount { get; }
        public int InteriorInactiveCount { get; }
        public int ProtectedUnionCount { get; }
        public int ApprovedReservedAdapterOverlapCount { get; }
        public int OpenEdgeToInactiveCount { get; }
        public int Type0LeftRightOpenCount { get; }
        public int MissingClueCount { get; }
        public int MissingReturnPolicyCount { get; }
        public int IssueCount { get; }
        public int RngDrawCount { get; }
        public int SourceMutationCount { get; }
    }
}
