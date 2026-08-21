using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class WorldRouteDefinitionSet
    {
        private static readonly IReadOnlyList<GenerationPassDefinition> EmptyGenerationPasses =
            new ReadOnlyCollection<GenerationPassDefinition>(Array.Empty<GenerationPassDefinition>());
        private static readonly IReadOnlyList<EdgeSignatureCompatibilityDefinition> EmptyCompatibilities =
            new ReadOnlyCollection<EdgeSignatureCompatibilityDefinition>(Array.Empty<EdgeSignatureCompatibilityDefinition>());
        private static readonly IReadOnlyList<SectorRecipeCellDefinition> EmptyCells =
            new ReadOnlyCollection<SectorRecipeCellDefinition>(Array.Empty<SectorRecipeCellDefinition>());
        private static readonly IReadOnlyList<SectorRecipePathDefinition> EmptyPaths =
            new ReadOnlyCollection<SectorRecipePathDefinition>(Array.Empty<SectorRecipePathDefinition>());
        private static readonly IReadOnlyList<SectorExternalSocketDefinition> EmptySockets =
            new ReadOnlyCollection<SectorExternalSocketDefinition>(Array.Empty<SectorExternalSocketDefinition>());
        private static readonly IReadOnlyList<SectorRecipePoolEntryDefinition> EmptyPoolEntries =
            new ReadOnlyCollection<SectorRecipePoolEntryDefinition>(Array.Empty<SectorRecipePoolEntryDefinition>());

        private readonly IReadOnlyDictionary<string, IReadOnlyList<GenerationPassDefinition>> passesByProfile;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<EdgeSignatureCompatibilityDefinition>> compatibilitiesBySignatureA;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<SectorRecipeCellDefinition>> cellsByRecipe;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<SectorRecipePathDefinition>> pathsByRecipe;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<SectorExternalSocketDefinition>> socketsByRecipe;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<SectorRecipePoolEntryDefinition>> entriesByPool;

        internal WorldRouteDefinitionSet(
            IEnumerable<WorldProfileDefinition> worldProfiles,
            IEnumerable<GenerationProfileDefinition> generationProfiles,
            IEnumerable<GenerationPassDefinition> generationPasses,
            IEnumerable<RngStreamDefinition> rngStreams,
            IEnumerable<SectorRouteMaskDefinition> routeMasks,
            IEnumerable<SocketBandDefinition> socketBands,
            IEnumerable<EdgeSignatureDefinition> edgeSignatures,
            IEnumerable<EdgeSignatureCompatibilityDefinition> edgeSignatureCompatibilities,
            IEnumerable<SectorRecipeDefinition> sectorRecipes,
            IEnumerable<SectorRecipeCellDefinition> sectorRecipeCells,
            IEnumerable<SectorRecipePathDefinition> sectorRecipePaths,
            IEnumerable<SectorExternalSocketDefinition> sectorExternalSockets,
            IEnumerable<SectorRecipePoolEntryDefinition> sectorRecipePoolEntries)
        {
            WorldProfiles = ToDictionary(worldProfiles, item => item.WorldProfileId);
            GenerationProfiles = ToDictionary(generationProfiles, item => item.GenerationProfileId);
            RngStreams = ToDictionary(rngStreams, item => item.RngStreamId);
            RouteMasks = ToDictionary(routeMasks, item => item.RouteMaskId);
            SocketBands = ToDictionary(socketBands, item => item.BandId);
            EdgeSignatures = ToDictionary(edgeSignatures, item => item.EdgeSignatureId);
            SectorRecipes = ToDictionary(sectorRecipes, item => item.SectorRecipeId);

            GenerationPasses = ToList(generationPasses
                .OrderBy(item => item.GenerationProfileId, StringComparer.Ordinal)
                .ThenBy(item => item.PassOrder)
                .ThenBy(item => item.PassId, StringComparer.Ordinal));
            EdgeSignatureCompatibilities = ToList(edgeSignatureCompatibilities
                .OrderBy(item => item.SignatureA, StringComparer.Ordinal)
                .ThenBy(item => item.SignatureB, StringComparer.Ordinal));
            SectorRecipeCells = ToList(sectorRecipeCells
                .OrderBy(item => item.SectorRecipeId, StringComparer.Ordinal)
                .ThenBy(item => item.ChunkX)
                .ThenBy(item => item.ChunkY));
            SectorRecipePaths = ToList(sectorRecipePaths
                .OrderBy(item => item.SectorRecipeId, StringComparer.Ordinal)
                .ThenBy(item => item.PathId, StringComparer.Ordinal)
                .ThenBy(item => item.PathOrder));
            SectorExternalSockets = ToList(sectorExternalSockets
                .OrderBy(item => item.SectorRecipeId, StringComparer.Ordinal)
                .ThenBy(item => item.SocketId, StringComparer.Ordinal));
            SectorRecipePoolEntries = ToList(sectorRecipePoolEntries
                .OrderBy(item => item.SectorRecipePoolId, StringComparer.Ordinal)
                .ThenBy(item => item.EntryOrder)
                .ThenBy(item => item.SectorRecipeId, StringComparer.Ordinal));

            passesByProfile = Group(GenerationPasses, item => item.GenerationProfileId);
            compatibilitiesBySignatureA = Group(EdgeSignatureCompatibilities, item => item.SignatureA);
            cellsByRecipe = Group(SectorRecipeCells, item => item.SectorRecipeId);
            pathsByRecipe = Group(SectorRecipePaths, item => item.SectorRecipeId);
            socketsByRecipe = Group(SectorExternalSockets, item => item.SectorRecipeId);
            entriesByPool = Group(SectorRecipePoolEntries, item => item.SectorRecipePoolId);
        }

        public IReadOnlyDictionary<string, WorldProfileDefinition> WorldProfiles { get; }
        public IReadOnlyDictionary<string, GenerationProfileDefinition> GenerationProfiles { get; }
        public IReadOnlyList<GenerationPassDefinition> GenerationPasses { get; }
        public IReadOnlyDictionary<string, RngStreamDefinition> RngStreams { get; }
        public IReadOnlyDictionary<string, SectorRouteMaskDefinition> RouteMasks { get; }
        public IReadOnlyDictionary<string, SocketBandDefinition> SocketBands { get; }
        public IReadOnlyDictionary<string, EdgeSignatureDefinition> EdgeSignatures { get; }
        public IReadOnlyList<EdgeSignatureCompatibilityDefinition> EdgeSignatureCompatibilities { get; }
        public IReadOnlyDictionary<string, SectorRecipeDefinition> SectorRecipes { get; }
        public IReadOnlyList<SectorRecipeCellDefinition> SectorRecipeCells { get; }
        public IReadOnlyList<SectorRecipePathDefinition> SectorRecipePaths { get; }
        public IReadOnlyList<SectorExternalSocketDefinition> SectorExternalSockets { get; }
        public IReadOnlyList<SectorRecipePoolEntryDefinition> SectorRecipePoolEntries { get; }

        public IReadOnlyList<GenerationPassDefinition> GetGenerationPasses(string generationProfileId)
        {
            return Get(passesByProfile, generationProfileId, EmptyGenerationPasses);
        }

        public IReadOnlyList<EdgeSignatureCompatibilityDefinition> GetEdgeSignatureCompatibilities(string signatureA)
        {
            return Get(compatibilitiesBySignatureA, signatureA, EmptyCompatibilities);
        }

        public IReadOnlyList<SectorRecipeCellDefinition> GetSectorRecipeCells(string sectorRecipeId)
        {
            return Get(cellsByRecipe, sectorRecipeId, EmptyCells);
        }

        public IReadOnlyList<SectorRecipePathDefinition> GetSectorRecipePaths(string sectorRecipeId)
        {
            return Get(pathsByRecipe, sectorRecipeId, EmptyPaths);
        }

        public IReadOnlyList<SectorExternalSocketDefinition> GetSectorExternalSockets(string sectorRecipeId)
        {
            return Get(socketsByRecipe, sectorRecipeId, EmptySockets);
        }

        public IReadOnlyList<SectorRecipePoolEntryDefinition> GetSectorRecipePoolEntries(string sectorRecipePoolId)
        {
            return Get(entriesByPool, sectorRecipePoolId, EmptyPoolEntries);
        }

        private static IReadOnlyDictionary<string, T> ToDictionary<T>(
            IEnumerable<T> source,
            Func<T, string> keySelector)
        {
            var dictionary = new SortedDictionary<string, T>(StringComparer.Ordinal);
            foreach (var item in source ?? throw new ArgumentNullException(nameof(source)))
            {
                dictionary.Add(keySelector(item), item);
            }

            return new ReadOnlyDictionary<string, T>(dictionary);
        }

        private static IReadOnlyList<T> ToList<T>(IEnumerable<T> source)
        {
            return new ReadOnlyCollection<T>(new List<T>(
                source ?? throw new ArgumentNullException(nameof(source))));
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<T>> Group<T>(
            IEnumerable<T> source,
            Func<T, string> keySelector)
        {
            var dictionary = new SortedDictionary<string, IReadOnlyList<T>>(StringComparer.Ordinal);
            foreach (var grouping in source.GroupBy(keySelector, StringComparer.Ordinal))
            {
                dictionary.Add(grouping.Key, ToList(grouping));
            }

            return new ReadOnlyDictionary<string, IReadOnlyList<T>>(dictionary);
        }

        private static IReadOnlyList<T> Get<T>(
            IReadOnlyDictionary<string, IReadOnlyList<T>> dictionary,
            string key,
            IReadOnlyList<T> empty)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return dictionary.TryGetValue(key, out var values) ? values : empty;
        }
    }
}
