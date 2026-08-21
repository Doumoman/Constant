namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class StaticDataRegistryInput
    {
        public StaticDataRegistryInput(
            WorldRouteDefinitionSet worldRouteDefinitions,
            BiomeBoundaryDefinitionSet biomeBoundaryDefinitions,
            SpecialVillageDefinitionSet specialVillageDefinitions,
            MicrochunkPopulationItemDefinitionSet microchunkPopulationItemDefinitions,
            ForeignKeyResolutionResult foreignKeyResolution)
        {
            WorldRouteDefinitions = worldRouteDefinitions;
            BiomeBoundaryDefinitions = biomeBoundaryDefinitions;
            SpecialVillageDefinitions = specialVillageDefinitions;
            MicrochunkPopulationItemDefinitions = microchunkPopulationItemDefinitions;
            ForeignKeyResolution = foreignKeyResolution;
        }

        public WorldRouteDefinitionSet WorldRouteDefinitions { get; }
        public BiomeBoundaryDefinitionSet BiomeBoundaryDefinitions { get; }
        public SpecialVillageDefinitionSet SpecialVillageDefinitions { get; }
        public MicrochunkPopulationItemDefinitionSet MicrochunkPopulationItemDefinitions { get; }
        public ForeignKeyResolutionResult ForeignKeyResolution { get; }
    }
}
