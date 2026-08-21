using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum BiomePatchValidationErrorCode
    {
        MissingExportResult,
        ExportNotCompleted,
        MissingExportPublication,
        InvalidExportPublication,
        MissingBiomeTypes,
        MissingPatchRules,
        MissingBoundaryProfiles,
        MissingBoundaryPairRules,
        NullDefinition,
        DuplicateDefinition,
        MissingDefinition,
        UnexpectedDefinition,
        InactiveDefinition,
        InvalidDefinition,
        InvalidShareDefinition,
        InternalInvariantViolation
    }

    public sealed class BiomePatchValidationError
    {
        internal BiomePatchValidationError(
            BiomePatchValidationErrorCode code,
            string definitionId,
            int sectorIndex,
            string message)
        {
            if (!Enum.IsDefined(typeof(BiomePatchValidationErrorCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            Code = code;
            DefinitionId = definitionId ?? throw new ArgumentNullException(nameof(definitionId));
            SectorIndex = sectorIndex;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public BiomePatchValidationErrorCode Code { get; }
        public string DefinitionId { get; }
        public int SectorIndex { get; }
        public string Message { get; }
    }
}
