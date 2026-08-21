using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum PatchCleanupErrorCode
    {
        MissingIntrusionResult,
        IntrusionNotCompleted,
        MissingPublication,
        MissingDiagnostics,
        MissingBiomeTypes,
        MissingPatchRules,
        InvalidSourceSnapshot,
        InvalidDefinition,
        NoSafeCleanupMove,
        CleanupStepLimitExceeded,
        InternalInvariantViolation
    }

    public sealed class PatchCleanupError
    {
        internal PatchCleanupError(
            PatchCleanupErrorCode code,
            string definitionId,
            int sectorIndex,
            int requiredCount,
            int availableCount,
            string message)
        {
            if (!Enum.IsDefined(typeof(PatchCleanupErrorCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            if (definitionId == null) throw new ArgumentNullException(nameof(definitionId));
            if (sectorIndex < -1 || sectorIndex >= Domain.WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (requiredCount < 0 || availableCount < 0)
                throw new ArgumentOutOfRangeException(nameof(requiredCount));
            if (string.IsNullOrEmpty(message)) throw new ArgumentException("A message is required.", nameof(message));

            Code = code;
            DefinitionId = definitionId;
            SectorIndex = sectorIndex;
            RequiredCount = requiredCount;
            AvailableCount = availableCount;
            Message = message;
        }

        public PatchCleanupErrorCode Code { get; }
        public string DefinitionId { get; }
        public int SectorIndex { get; }
        public int RequiredCount { get; }
        public int AvailableCount { get; }
        public string Message { get; }
    }
}
