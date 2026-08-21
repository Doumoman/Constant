using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class MicrochunkPopulationItemDefinitionSet
    {
        private static readonly IReadOnlyList<MapElementInteractionDefinition> EmptyInteractions = ToList(Array.Empty<MapElementInteractionDefinition>());
        private static readonly IReadOnlyList<MicrochunkObjectSlotDefinition> EmptyObjectSlots = ToList(Array.Empty<MicrochunkObjectSlotDefinition>());
        private static readonly IReadOnlyList<MicrochunkPoolEntryDefinition> EmptyMicrochunkPoolEntries = ToList(Array.Empty<MicrochunkPoolEntryDefinition>());
        private static readonly IReadOnlyList<MicrochunkSocketDefinition> EmptySockets = ToList(Array.Empty<MicrochunkSocketDefinition>());
        private static readonly IReadOnlyList<MicrochunkTileCellDefinition> EmptyTileCells = ToList(Array.Empty<MicrochunkTileCellDefinition>());
        private static readonly IReadOnlyList<SpawnPoolEntryDefinition> EmptySpawnPoolEntries = ToList(Array.Empty<SpawnPoolEntryDefinition>());
        private static readonly IReadOnlyList<ToolUpgradeDefinition> EmptyToolUpgrades = ToList(Array.Empty<ToolUpgradeDefinition>());

        private readonly IReadOnlyDictionary<string, IReadOnlyList<MapElementInteractionDefinition>> interactionsBySource;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<MicrochunkObjectSlotDefinition>> objectSlotsByMicrochunk;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<MicrochunkPoolEntryDefinition>> microchunkEntriesByPool;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<MicrochunkSocketDefinition>> socketsByMicrochunk;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<MicrochunkTileCellDefinition>> tileCellsByMicrochunk;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<SpawnPoolEntryDefinition>> spawnEntriesByPool;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<ToolUpgradeDefinition>> upgradesByTool;

        internal MicrochunkPopulationItemDefinitionSet(
            IEnumerable<MapElementDefinition> mapElements,
            IEnumerable<MapElementInteractionDefinition> mapElementInteractions,
            IEnumerable<MicrochunkDefinition> microchunks,
            IEnumerable<MicrochunkObjectSlotDefinition> microchunkObjectSlots,
            IEnumerable<MicrochunkPoolEntryDefinition> microchunkPoolEntries,
            IEnumerable<MicrochunkSocketDefinition> microchunkSockets,
            IEnumerable<MicrochunkTileCellDefinition> microchunkTileCells,
            IEnumerable<MicrochunkVariantRuleDefinition> microchunkVariantRules,
            IEnumerable<PopulationProfileDefinition> populationProfiles,
            IEnumerable<PrefabRegistryDefinition> prefabs,
            IEnumerable<ResourceDefinition> resources,
            IEnumerable<ResourceSpawnRuleDefinition> resourceSpawnRules,
            IEnumerable<SpawnPoolEntryDefinition> spawnPoolEntries,
            IEnumerable<SpecialItemSlotDefinition> specialItemSlots,
            IEnumerable<TileCodeDefinition> tileCodes,
            IEnumerable<ToolUpgradeDefinition> toolUpgrades)
            : this(
                mapElements, mapElementInteractions, microchunks, microchunkObjectSlots,
                microchunkPoolEntries, microchunkSockets, microchunkTileCells,
                microchunkVariantRules, populationProfiles, prefabs, resources,
                resourceSpawnRules, spawnPoolEntries, specialItemSlots, tileCodes,
                toolUpgrades, Array.Empty<BatteryProfileDefinition>())
        {
        }

        internal MicrochunkPopulationItemDefinitionSet(
            IEnumerable<MapElementDefinition> mapElements,
            IEnumerable<MapElementInteractionDefinition> mapElementInteractions,
            IEnumerable<MicrochunkDefinition> microchunks,
            IEnumerable<MicrochunkObjectSlotDefinition> microchunkObjectSlots,
            IEnumerable<MicrochunkPoolEntryDefinition> microchunkPoolEntries,
            IEnumerable<MicrochunkSocketDefinition> microchunkSockets,
            IEnumerable<MicrochunkTileCellDefinition> microchunkTileCells,
            IEnumerable<MicrochunkVariantRuleDefinition> microchunkVariantRules,
            IEnumerable<PopulationProfileDefinition> populationProfiles,
            IEnumerable<PrefabRegistryDefinition> prefabs,
            IEnumerable<ResourceDefinition> resources,
            IEnumerable<ResourceSpawnRuleDefinition> resourceSpawnRules,
            IEnumerable<SpawnPoolEntryDefinition> spawnPoolEntries,
            IEnumerable<SpecialItemSlotDefinition> specialItemSlots,
            IEnumerable<TileCodeDefinition> tileCodes,
            IEnumerable<ToolUpgradeDefinition> toolUpgrades,
            IEnumerable<BatteryProfileDefinition> batteryProfiles)
        {
            BatteryProfiles = ToDictionary(batteryProfiles, item => item.BatteryId);
            MapElements = ToDictionary(mapElements, item => item.MapElementId);
            Microchunks = ToDictionary(microchunks, item => item.MicrochunkId);
            MicrochunkVariantRules = ToDictionary(microchunkVariantRules, item => item.VariantRuleId);
            PopulationProfiles = ToDictionary(populationProfiles, item => item.PopulationProfileId);
            Prefabs = ToDictionary(prefabs, item => item.PrefabId);
            Resources = ToDictionary(resources, item => item.ResourceId);
            ResourceSpawnRules = ToDictionary(resourceSpawnRules, item => item.SpawnRuleId);
            SpecialItemSlots = ToDictionary(specialItemSlots, item => item.SpecialItemSlotId);
            TileCodes = ToDictionary(tileCodes, item => item.TileCode);

            MapElementInteractions = ToList(mapElementInteractions
                .OrderBy(item => item.SourceElementOrToolId, StringComparer.Ordinal)
                .ThenBy(item => item.TargetTag, StringComparer.Ordinal));
            MicrochunkObjectSlots = ToList(microchunkObjectSlots
                .OrderBy(item => item.MicrochunkId, StringComparer.Ordinal)
                .ThenBy(item => item.SlotId, StringComparer.Ordinal));
            MicrochunkPoolEntries = ToList(microchunkPoolEntries
                .OrderBy(item => item.MicrochunkPoolId, StringComparer.Ordinal)
                .ThenBy(item => item.EntryOrder));
            MicrochunkSockets = ToList(microchunkSockets
                .OrderBy(item => item.MicrochunkId, StringComparer.Ordinal)
                .ThenBy(item => item.SocketId, StringComparer.Ordinal));
            MicrochunkTileCells = ToList(microchunkTileCells
                .OrderBy(item => item.MicrochunkId, StringComparer.Ordinal)
                .ThenBy(item => item.LocalX)
                .ThenBy(item => item.LocalY));
            SpawnPoolEntries = ToList(spawnPoolEntries
                .OrderBy(item => item.SpawnPoolId, StringComparer.Ordinal)
                .ThenBy(item => item.EntryOrder));
            ToolUpgrades = ToList(toolUpgrades
                .OrderBy(item => item.ToolId, StringComparer.Ordinal)
                .ThenBy(item => item.UpgradeLevel));

            interactionsBySource = Group(MapElementInteractions, item => item.SourceElementOrToolId);
            objectSlotsByMicrochunk = Group(MicrochunkObjectSlots, item => item.MicrochunkId);
            microchunkEntriesByPool = Group(MicrochunkPoolEntries, item => item.MicrochunkPoolId);
            socketsByMicrochunk = Group(MicrochunkSockets, item => item.MicrochunkId);
            tileCellsByMicrochunk = Group(MicrochunkTileCells, item => item.MicrochunkId);
            spawnEntriesByPool = Group(SpawnPoolEntries, item => item.SpawnPoolId);
            upgradesByTool = Group(ToolUpgrades, item => item.ToolId);
        }

        public IReadOnlyDictionary<string, BatteryProfileDefinition> BatteryProfiles { get; }
        public IReadOnlyDictionary<string, MapElementDefinition> MapElements { get; }
        public IReadOnlyList<MapElementInteractionDefinition> MapElementInteractions { get; }
        public IReadOnlyDictionary<string, MicrochunkDefinition> Microchunks { get; }
        public IReadOnlyList<MicrochunkObjectSlotDefinition> MicrochunkObjectSlots { get; }
        public IReadOnlyList<MicrochunkPoolEntryDefinition> MicrochunkPoolEntries { get; }
        public IReadOnlyList<MicrochunkSocketDefinition> MicrochunkSockets { get; }
        public IReadOnlyList<MicrochunkTileCellDefinition> MicrochunkTileCells { get; }
        public IReadOnlyDictionary<string, MicrochunkVariantRuleDefinition> MicrochunkVariantRules { get; }
        public IReadOnlyDictionary<string, PopulationProfileDefinition> PopulationProfiles { get; }
        public IReadOnlyDictionary<string, PrefabRegistryDefinition> Prefabs { get; }
        public IReadOnlyDictionary<string, ResourceDefinition> Resources { get; }
        public IReadOnlyDictionary<string, ResourceSpawnRuleDefinition> ResourceSpawnRules { get; }
        public IReadOnlyList<SpawnPoolEntryDefinition> SpawnPoolEntries { get; }
        public IReadOnlyDictionary<string, SpecialItemSlotDefinition> SpecialItemSlots { get; }
        public IReadOnlyDictionary<string, TileCodeDefinition> TileCodes { get; }
        public IReadOnlyList<ToolUpgradeDefinition> ToolUpgrades { get; }

        public IReadOnlyList<MapElementInteractionDefinition> GetMapElementInteractions(string sourceElementOrToolId) =>
            Get(interactionsBySource, sourceElementOrToolId, EmptyInteractions);

        public IReadOnlyList<MicrochunkObjectSlotDefinition> GetMicrochunkObjectSlots(string microchunkId) =>
            Get(objectSlotsByMicrochunk, microchunkId, EmptyObjectSlots);

        public IReadOnlyList<MicrochunkPoolEntryDefinition> GetMicrochunkPoolEntries(string microchunkPoolId) =>
            Get(microchunkEntriesByPool, microchunkPoolId, EmptyMicrochunkPoolEntries);

        public IReadOnlyList<MicrochunkSocketDefinition> GetMicrochunkSockets(string microchunkId) =>
            Get(socketsByMicrochunk, microchunkId, EmptySockets);

        public IReadOnlyList<MicrochunkTileCellDefinition> GetMicrochunkTileCells(string microchunkId) =>
            Get(tileCellsByMicrochunk, microchunkId, EmptyTileCells);

        public IReadOnlyList<SpawnPoolEntryDefinition> GetSpawnPoolEntries(string spawnPoolId) =>
            Get(spawnEntriesByPool, spawnPoolId, EmptySpawnPoolEntries);

        public IReadOnlyList<ToolUpgradeDefinition> GetToolUpgrades(string toolId) =>
            Get(upgradesByTool, toolId, EmptyToolUpgrades);

        private static IReadOnlyDictionary<string, T> ToDictionary<T>(IEnumerable<T> source, Func<T, string> keySelector)
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
            return new ReadOnlyCollection<T>(new List<T>(source ?? throw new ArgumentNullException(nameof(source))));
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<T>> Group<T>(IEnumerable<T> source, Func<T, string> keySelector)
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
            if (key == null) throw new ArgumentNullException(nameof(key));
            return dictionary.TryGetValue(key, out var values) ? values : empty;
        }
    }
}
