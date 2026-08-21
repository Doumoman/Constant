using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalReturnPolicyDiagnostics
    {
        public OptionalReturnPolicyDiagnostics(
            int sourceRegionCount,
            int sourceType0CellAssignmentCount,
            int sourceAccessAssignmentCount,
            int sourceRewardTierAssignmentCount,
            int assignmentCount,
            int backtrackCount,
            int returnGateCount,
            int safeExitCount,
            int returnableCellCount,
            int nonReturnableCellCount,
            int internalUndirectedBaseEdgeCount,
            int criticalWitnessSectorCountTotal,
            int criticalWitnessEdgeCountTotal,
            int maximumCriticalWitnessSectorCount,
            int sameOpenedAttachmentReturnCount,
            int returnDeviceReservationCount,
            int extraSafeExitReservationCount,
            int attachmentBoundaryBaseOpenCount,
            int rngDrawCount,
            int sourceMutationCount)
        {
            var values = new[]
            {
                sourceRegionCount, sourceType0CellAssignmentCount, sourceAccessAssignmentCount,
                sourceRewardTierAssignmentCount, assignmentCount, backtrackCount, returnGateCount,
                safeExitCount, returnableCellCount, nonReturnableCellCount, internalUndirectedBaseEdgeCount,
                criticalWitnessSectorCountTotal, criticalWitnessEdgeCountTotal,
                maximumCriticalWitnessSectorCount, sameOpenedAttachmentReturnCount,
                returnDeviceReservationCount, extraSafeExitReservationCount,
                attachmentBoundaryBaseOpenCount, rngDrawCount, sourceMutationCount
            };
            foreach (var value in values)
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(sourceRegionCount));
            }
            if (backtrackCount + returnGateCount + safeExitCount != assignmentCount)
                throw new ArgumentException("Return policy counts must equal the assignment count.");
            if (criticalWitnessEdgeCountTotal + assignmentCount != criticalWitnessSectorCountTotal)
                throw new ArgumentException("Witness edge and sector totals are inconsistent.");
            if (assignmentCount == 0 && maximumCriticalWitnessSectorCount != 0)
                throw new ArgumentException("Empty diagnostics require a zero maximum witness count.");

            SourceRegionCount = sourceRegionCount;
            SourceType0CellAssignmentCount = sourceType0CellAssignmentCount;
            SourceAccessAssignmentCount = sourceAccessAssignmentCount;
            SourceRewardTierAssignmentCount = sourceRewardTierAssignmentCount;
            AssignmentCount = assignmentCount;
            BacktrackCount = backtrackCount;
            ReturnGateCount = returnGateCount;
            SafeExitCount = safeExitCount;
            ReturnableCellCount = returnableCellCount;
            NonReturnableCellCount = nonReturnableCellCount;
            InternalUndirectedBaseEdgeCount = internalUndirectedBaseEdgeCount;
            CriticalWitnessSectorCountTotal = criticalWitnessSectorCountTotal;
            CriticalWitnessEdgeCountTotal = criticalWitnessEdgeCountTotal;
            MaximumCriticalWitnessSectorCount = maximumCriticalWitnessSectorCount;
            SameOpenedAttachmentReturnCount = sameOpenedAttachmentReturnCount;
            ReturnDeviceReservationCount = returnDeviceReservationCount;
            ExtraSafeExitReservationCount = extraSafeExitReservationCount;
            AttachmentBoundaryBaseOpenCount = attachmentBoundaryBaseOpenCount;
            RngDrawCount = rngDrawCount;
            SourceMutationCount = sourceMutationCount;
        }

        public int SourceRegionCount { get; }
        public int SourceType0CellAssignmentCount { get; }
        public int SourceAccessAssignmentCount { get; }
        public int SourceRewardTierAssignmentCount { get; }
        public int AssignmentCount { get; }
        public int BacktrackCount { get; }
        public int ReturnGateCount { get; }
        public int SafeExitCount { get; }
        public int ReturnableCellCount { get; }
        public int NonReturnableCellCount { get; }
        public int InternalUndirectedBaseEdgeCount { get; }
        public int CriticalWitnessSectorCountTotal { get; }
        public int CriticalWitnessEdgeCountTotal { get; }
        public int MaximumCriticalWitnessSectorCount { get; }
        public int SameOpenedAttachmentReturnCount { get; }
        public int ReturnDeviceReservationCount { get; }
        public int ExtraSafeExitReservationCount { get; }
        public int AttachmentBoundaryBaseOpenCount { get; }
        public int RngDrawCount { get; }
        public int SourceMutationCount { get; }
    }
}
