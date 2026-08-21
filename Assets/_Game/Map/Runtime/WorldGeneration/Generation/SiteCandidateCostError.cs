using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum SiteCandidateCostErrorCode
    {
        MissingCandidate,
        InvalidCandidate,
        MissingContext,
        MissingWeights,
        InvalidWeights,
        MissingDistancePolicy,
        InvalidExistingPlacement,
        DuplicateExistingPlacementKey,
        CandidateAlreadyPlaced,
        OverlappingPlacement,
        MissingSpecialMap,
        UnexpectedSpecialMap,
        InvalidSpecialMap,
        SourceIdentityMismatch,
        MissingPrimaryBiome,
        UnexpectedPrimaryBiome,
        InvalidPrimaryBiome,
        MissingCorePatchRule,
        UnexpectedCorePatchRule,
        InvalidCorePatchRule,
        MissingPolicyKey,
        UnexpectedExistingKey,
        MissingDistanceConstraint,
        InvalidFutureCapacityEstimate,
        InvalidCoreResourceSet,
        CostOverflow
    }

    public sealed class SiteCandidateCostError
    {
        public SiteCandidateCostError(
            SiteCandidateCostErrorCode code,
            string candidateSourceDefinitionId,
            string existingSourceDefinitionId,
            int sectorIndex,
            string message)
        {
            if (!Enum.IsDefined(typeof(SiteCandidateCostErrorCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            if (!IsCanonicalOrEmpty(candidateSourceDefinitionId))
                throw new ArgumentException("The candidate source ID must be canonical or empty.",
                    nameof(candidateSourceDefinitionId));
            if (!IsCanonicalOrEmpty(existingSourceDefinitionId))
                throw new ArgumentException("The existing source ID must be canonical or empty.",
                    nameof(existingSourceDefinitionId));
            if (sectorIndex < -1 || sectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("A stable non-empty message is required.", nameof(message));

            Code = code;
            CandidateSourceDefinitionId = candidateSourceDefinitionId;
            ExistingSourceDefinitionId = existingSourceDefinitionId;
            SectorIndex = sectorIndex;
            Message = message;
        }

        public SiteCandidateCostErrorCode Code { get; }
        public string CandidateSourceDefinitionId { get; }
        public string ExistingSourceDefinitionId { get; }
        public int SectorIndex { get; }
        public string Message { get; }

        private static bool IsCanonicalOrEmpty(string value) =>
            value != null && (value.Length == 0 || SitePlacementKey.IsCanonicalId(value));
    }
}
