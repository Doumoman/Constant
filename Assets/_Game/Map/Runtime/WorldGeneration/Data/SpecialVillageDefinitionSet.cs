using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class SpecialVillageDefinitionSet
    {
        private static readonly IReadOnlyList<SpecialMapEntrySocketDefinition> EmptyEntrySockets =
            ToList(Array.Empty<SpecialMapEntrySocketDefinition>());
        private static readonly IReadOnlyList<SpecialMapFootprintCellDefinition> EmptyFootprintCells =
            ToList(Array.Empty<SpecialMapFootprintCellDefinition>());
        private static readonly IReadOnlyList<SpecialMapRewardDefinition> EmptyRewards =
            ToList(Array.Empty<SpecialMapRewardDefinition>());
        private static readonly IReadOnlyList<ShopInventoryRuleDefinition> EmptyInventoryRules =
            ToList(Array.Empty<ShopInventoryRuleDefinition>());
        private static readonly IReadOnlyList<VillageLayoutCellDefinition> EmptyLayoutCells =
            ToList(Array.Empty<VillageLayoutCellDefinition>());

        private readonly IReadOnlyDictionary<string, IReadOnlyList<SpecialMapEntrySocketDefinition>> entrySocketsBySpecialMap;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<SpecialMapFootprintCellDefinition>> footprintCellsBySpecialMap;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<SpecialMapRewardDefinition>> rewardsBySpecialMap;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<ShopInventoryRuleDefinition>> inventoryRulesByShop;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<VillageLayoutCellDefinition>> cellsByLayout;

        internal SpecialVillageDefinitionSet(
            IEnumerable<EventActivationRouteDefinition> eventActivationRoutes,
            IEnumerable<SpecialMapDefinition> specialMaps,
            IEnumerable<SpecialMapEntrySocketDefinition> specialMapEntrySockets,
            IEnumerable<SpecialMapFootprintCellDefinition> specialMapFootprintCells,
            IEnumerable<SpecialMapRewardDefinition> specialMapRewards,
            IEnumerable<ShopArchetypeDefinition> shopArchetypes,
            IEnumerable<ShopInventoryRuleDefinition> shopInventoryRules,
            IEnumerable<ShopkeeperSpeciesDefinition> shopkeeperSpecies,
            IEnumerable<VillageFacilityDefinition> villageFacilities,
            IEnumerable<VillageLayoutDefinition> villageLayouts,
            IEnumerable<VillageLayoutCellDefinition> villageLayoutCells,
            IEnumerable<VillageProfileDefinition> villageProfiles)
        {
            EventActivationRoutes = ToDictionary(eventActivationRoutes, item => item.EventRouteId);
            SpecialMaps = ToDictionary(specialMaps, item => item.SpecialMapId);
            ShopArchetypes = ToDictionary(shopArchetypes, item => item.ShopArchetypeId);
            ShopkeeperSpecies = ToDictionary(shopkeeperSpecies, item => item.SpeciesId);
            VillageFacilities = ToDictionary(villageFacilities, item => item.FacilityId);
            VillageLayouts = ToDictionary(villageLayouts, item => item.VillageLayoutId);
            VillageProfiles = ToDictionary(villageProfiles, item => item.VillageProfileId);

            SpecialMapEntrySockets = ToList(specialMapEntrySockets
                .OrderBy(item => item.SpecialMapId, StringComparer.Ordinal)
                .ThenBy(item => item.EntrySocketId, StringComparer.Ordinal));
            SpecialMapFootprintCells = ToList(specialMapFootprintCells
                .OrderBy(item => item.SpecialMapId, StringComparer.Ordinal)
                .ThenBy(item => item.LocalSectorX)
                .ThenBy(item => item.LocalSectorY));
            SpecialMapRewards = ToList(specialMapRewards
                .OrderBy(item => item.SpecialMapId, StringComparer.Ordinal)
                .ThenBy(item => item.RewardOrder));
            ShopInventoryRules = ToList(shopInventoryRules
                .OrderBy(item => item.ShopArchetypeId, StringComparer.Ordinal)
                .ThenBy(item => item.SlotIndex));
            VillageLayoutCells = ToList(villageLayoutCells
                .OrderBy(item => item.VillageLayoutId, StringComparer.Ordinal)
                .ThenBy(item => item.LocalChunkX)
                .ThenBy(item => item.LocalChunkY));

            entrySocketsBySpecialMap = Group(SpecialMapEntrySockets, item => item.SpecialMapId);
            footprintCellsBySpecialMap = Group(SpecialMapFootprintCells, item => item.SpecialMapId);
            rewardsBySpecialMap = Group(SpecialMapRewards, item => item.SpecialMapId);
            inventoryRulesByShop = Group(ShopInventoryRules, item => item.ShopArchetypeId);
            cellsByLayout = Group(VillageLayoutCells, item => item.VillageLayoutId);
        }

        public IReadOnlyDictionary<string, EventActivationRouteDefinition> EventActivationRoutes { get; }
        public IReadOnlyDictionary<string, SpecialMapDefinition> SpecialMaps { get; }
        public IReadOnlyList<SpecialMapEntrySocketDefinition> SpecialMapEntrySockets { get; }
        public IReadOnlyList<SpecialMapFootprintCellDefinition> SpecialMapFootprintCells { get; }
        public IReadOnlyList<SpecialMapRewardDefinition> SpecialMapRewards { get; }
        public IReadOnlyDictionary<string, ShopArchetypeDefinition> ShopArchetypes { get; }
        public IReadOnlyList<ShopInventoryRuleDefinition> ShopInventoryRules { get; }
        public IReadOnlyDictionary<string, ShopkeeperSpeciesDefinition> ShopkeeperSpecies { get; }
        public IReadOnlyDictionary<string, VillageFacilityDefinition> VillageFacilities { get; }
        public IReadOnlyDictionary<string, VillageLayoutDefinition> VillageLayouts { get; }
        public IReadOnlyList<VillageLayoutCellDefinition> VillageLayoutCells { get; }
        public IReadOnlyDictionary<string, VillageProfileDefinition> VillageProfiles { get; }

        public IReadOnlyList<SpecialMapEntrySocketDefinition> GetSpecialMapEntrySockets(
            string specialMapId)
        {
            return Get(entrySocketsBySpecialMap, specialMapId, EmptyEntrySockets);
        }

        public IReadOnlyList<SpecialMapFootprintCellDefinition> GetSpecialMapFootprintCells(
            string specialMapId)
        {
            return Get(footprintCellsBySpecialMap, specialMapId, EmptyFootprintCells);
        }

        public IReadOnlyList<SpecialMapRewardDefinition> GetSpecialMapRewards(string specialMapId)
        {
            return Get(rewardsBySpecialMap, specialMapId, EmptyRewards);
        }

        public IReadOnlyList<ShopInventoryRuleDefinition> GetShopInventoryRules(
            string shopArchetypeId)
        {
            return Get(inventoryRulesByShop, shopArchetypeId, EmptyInventoryRules);
        }

        public IReadOnlyList<VillageLayoutCellDefinition> GetVillageLayoutCells(
            string villageLayoutId)
        {
            return Get(cellsByLayout, villageLayoutId, EmptyLayoutCells);
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
            if (key == null) throw new ArgumentNullException(nameof(key));
            return dictionary.TryGetValue(key, out var values) ? values : empty;
        }
    }
}
