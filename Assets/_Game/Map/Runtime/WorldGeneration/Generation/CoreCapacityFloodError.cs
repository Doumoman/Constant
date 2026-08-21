using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum CoreCapacityFloodErrorCode
    {
        MissingSelectionPlan,
        InvalidSelectionPlan,
        MissingRequirements,
        NullRequirement,
        DuplicateRequirementKey,
        MissingRequiredRequirement,
        UnexpectedRequirement,
        InvalidRequirement,
        PlacementNotSelected,
        PlacementIdentityMismatch,
        MissingSpecialMap,
        InvalidSpecialMap,
        MissingPrimaryBiome,
        InvalidPrimaryBiome,
        MissingCorePatchRule,
        InvalidCorePatchRule,
        DefinitionIdentityMismatch,
        InvalidFootprint,
        InternalInvariantViolation
    }

    public sealed class CoreCapacityFloodError
    {
        public CoreCapacityFloodError(
            CoreCapacityFloodErrorCode code,
            string siteSourceDefinitionId,
            string biomeId,
            string corePatchRuleId,
            int sectorIndex,
            string message)
        {
            if (!Enum.IsDefined(typeof(CoreCapacityFloodErrorCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            if (!CanonicalOrEmpty(siteSourceDefinitionId))
                throw new ArgumentException("The site ID must be canonical or empty.", nameof(siteSourceDefinitionId));
            if (!CanonicalOrEmpty(biomeId))
                throw new ArgumentException("The biome ID must be canonical or empty.", nameof(biomeId));
            if (!CanonicalOrEmpty(corePatchRuleId))
                throw new ArgumentException("The Core rule ID must be canonical or empty.", nameof(corePatchRuleId));
            if (sectorIndex < -1 || sectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("A stable non-empty message is required.", nameof(message));

            Code = code;
            SiteSourceDefinitionId = siteSourceDefinitionId;
            BiomeId = biomeId;
            CorePatchRuleId = corePatchRuleId;
            SectorIndex = sectorIndex;
            Message = message;
        }

        public CoreCapacityFloodErrorCode Code { get; }
        public string SiteSourceDefinitionId { get; }
        public string BiomeId { get; }
        public string CorePatchRuleId { get; }
        public int SectorIndex { get; }
        public string Message { get; }

        private static bool CanonicalOrEmpty(string value) =>
            value != null && (value.Length == 0 || SitePlacementKey.IsCanonicalId(value));
    }
}
