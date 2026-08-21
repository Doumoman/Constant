using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class WorldGenerationRngStreams
    {
        public const string WorldSiteStreamId = "RNG_WORLD_SITE";
        public const string BiomePatchStreamId = "RNG_BIOME_PATCH";
        public const string RouteStreamId = "RNG_ROUTE";
        public const string Type0StreamId = "RNG_TYPE0";
        public const string SectorRecipeStreamId = "RNG_SECTOR_RECIPE";
        public const string PopulationStreamId = "RNG_POPULATION";

        private static readonly IReadOnlyDictionary<string, RngResetScope> Catalog = CreateCatalog();
        private readonly DeterministicRngStreamFactory factory;

        public WorldGenerationRngStreams(WorldRouteDefinitionSet definitions)
        {
            factory = new DeterministicRngStreamFactory(definitions);
            ValidateCatalog();
        }

        public WorldGenerationRngStreams(StaticDataRegistry registry)
        {
            factory = new DeterministicRngStreamFactory(registry);
            ValidateCatalog();
        }

        public static IReadOnlyDictionary<string, RngResetScope> RequiredCatalog => Catalog;
        public WorldRouteDefinitionSet Definitions => factory.Definitions;

        public DeterministicRngStream Create(
            string streamId,
            ulong worldSeed,
            RngStreamScope scope)
        {
            return factory.Create(streamId, worldSeed, scope);
        }

        public DeterministicRngStream CreateWorldSite(ulong worldSeed, int attemptOrdinal = 0)
        {
            return factory.Create(WorldSiteStreamId, worldSeed, RngStreamScope.World(attemptOrdinal));
        }

        public DeterministicRngStream CreateBiomePatch(
            ulong worldSeed,
            string passId,
            int attemptOrdinal = 0)
        {
            return factory.Create(BiomePatchStreamId, worldSeed, RngStreamScope.Pass(passId, attemptOrdinal));
        }

        public DeterministicRngStream CreateRoute(
            ulong worldSeed,
            string passId,
            int attemptOrdinal = 0)
        {
            return factory.Create(RouteStreamId, worldSeed, RngStreamScope.Pass(passId, attemptOrdinal));
        }

        public DeterministicRngStream CreateType0(
            ulong worldSeed,
            string passId,
            int attemptOrdinal = 0)
        {
            return factory.Create(Type0StreamId, worldSeed, RngStreamScope.Pass(passId, attemptOrdinal));
        }

        public DeterministicRngStream CreateSectorRecipe(
            ulong worldSeed,
            SectorCoord coordinate,
            int attemptOrdinal = 0)
        {
            return factory.Create(
                SectorRecipeStreamId,
                worldSeed,
                RngStreamScope.Sector(coordinate, attemptOrdinal));
        }

        public DeterministicRngStream CreatePopulation(
            ulong worldSeed,
            string spawnScopeId,
            int attemptOrdinal = 0)
        {
            return factory.Create(
                PopulationStreamId,
                worldSeed,
                RngStreamScope.Spawn(spawnScopeId, attemptOrdinal));
        }

        private static IReadOnlyDictionary<string, RngResetScope> CreateCatalog()
        {
            var values = new SortedDictionary<string, RngResetScope>(StringComparer.Ordinal)
            {
                { WorldSiteStreamId, RngResetScope.World },
                { BiomePatchStreamId, RngResetScope.Pass },
                { RouteStreamId, RngResetScope.Pass },
                { Type0StreamId, RngResetScope.Pass },
                { SectorRecipeStreamId, RngResetScope.Sector },
                { PopulationStreamId, RngResetScope.Spawn }
            };
            return new ReadOnlyDictionary<string, RngResetScope>(values);
        }

        private void ValidateCatalog()
        {
            foreach (var requirement in Catalog)
            {
                var definition = factory.GetDefinition(requirement.Key);
                var actualScope = DeterministicRngSeedDeriver.ValidateDefinition(definition);
                if (actualScope != requirement.Value)
                {
                    throw new ArgumentException(
                        "Required RNG stream has the wrong reset scope: " + requirement.Key,
                        nameof(factory));
                }
            }
        }
    }
}
