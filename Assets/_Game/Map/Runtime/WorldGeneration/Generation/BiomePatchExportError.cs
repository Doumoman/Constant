using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum BiomePatchExportErrorCode
    {
        MissingCleanupResult,
        CleanupNotCompleted,
        MissingCleanupPublication,
        MissingCleanupDiagnostics,
        MissingSourceWorld,
        SeedMismatch,
        InvalidPatchSnapshot,
        InvalidSourceWorld,
        ConflictingExistingBiomeAssignment,
        SerializationFailure,
        InternalInvariantViolation
    }

    public sealed class BiomePatchExportError
    {
        internal BiomePatchExportError(
            BiomePatchExportErrorCode code,
            string definitionId,
            int sectorIndex,
            int requiredCount,
            int availableCount,
            string message)
        {
            if (!Enum.IsDefined(typeof(BiomePatchExportErrorCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            if (definitionId == null) throw new ArgumentNullException(nameof(definitionId));
            if (message == null) throw new ArgumentNullException(nameof(message));

            Code = code;
            DefinitionId = definitionId;
            SectorIndex = sectorIndex;
            RequiredCount = requiredCount;
            AvailableCount = availableCount;
            Message = message;
        }

        public BiomePatchExportErrorCode Code { get; }
        public string DefinitionId { get; }
        public int SectorIndex { get; }
        public int RequiredCount { get; }
        public int AvailableCount { get; }
        public string Message { get; }
    }
}
