using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class CoreCapacityRequirement
    {
        public CoreCapacityRequirement(
            SitePlacementKey key,
            FootprintPlacement placement,
            SpecialMapDefinition specialMap,
            BiomeTypeDefinition primaryBiome,
            BiomePatchRuleDefinition corePatchRule)
        {
            Key = key;
            Placement = placement;
            SpecialMap = specialMap;
            PrimaryBiome = primaryBiome;
            CorePatchRule = corePatchRule;
        }

        public SitePlacementKey Key { get; }
        public FootprintPlacement Placement { get; }
        public SpecialMapDefinition SpecialMap { get; }
        public BiomeTypeDefinition PrimaryBiome { get; }
        public BiomePatchRuleDefinition CorePatchRule { get; }
    }
}
