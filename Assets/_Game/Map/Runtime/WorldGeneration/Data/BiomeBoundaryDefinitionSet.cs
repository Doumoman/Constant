using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class BiomeBoundaryDefinitionSet
    {
        private static readonly IReadOnlyList<BiomePatchRuleDefinition> EmptyPatchRules =
            ToList(Array.Empty<BiomePatchRuleDefinition>());
        private static readonly IReadOnlyList<BiomeBoundaryPairRuleDefinition> EmptyPairRules =
            ToList(Array.Empty<BiomeBoundaryPairRuleDefinition>());
        private static readonly IReadOnlyList<BoundaryChunkDefinition> EmptyChunks =
            ToList(Array.Empty<BoundaryChunkDefinition>());

        private readonly IReadOnlyDictionary<string, IReadOnlyList<BiomePatchRuleDefinition>> patchRulesByBiome;
        private readonly IReadOnlyDictionary<DirectedBiomePair, IReadOnlyList<BiomeBoundaryPairRuleDefinition>> pairRulesByPair;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<BoundaryChunkDefinition>> chunksByProfile;
        private readonly IReadOnlyDictionary<DirectedBiomePair, IReadOnlyList<BoundaryChunkDefinition>> chunksByPair;

        internal BiomeBoundaryDefinitionSet(
            IEnumerable<BiomeTypeDefinition> biomeTypes,
            IEnumerable<BiomePatchRuleDefinition> biomePatchRules,
            IEnumerable<BiomeBoundaryProfileDefinition> boundaryProfiles,
            IEnumerable<BiomeBoundaryPairRuleDefinition> boundaryPairRules,
            IEnumerable<BoundaryChunkDefinition> boundaryChunks)
        {
            BiomeTypes = ToDictionary(biomeTypes, item => item.BiomeId);
            BiomePatchRules = ToDictionary(biomePatchRules, item => item.PatchRuleId);
            BoundaryProfiles = ToDictionary(boundaryProfiles, item => item.BoundaryProfileId);
            BoundaryPairRules = ToDictionary(boundaryPairRules, item => item.BoundaryPairRuleId);
            BoundaryChunks = ToDictionary(boundaryChunks, item => item.BoundaryChunkId);

            patchRulesByBiome = GroupByString(
                BiomePatchRules.Values,
                item => item.BiomeId);
            pairRulesByPair = GroupByPair(
                BoundaryPairRules.Values,
                item => new DirectedBiomePair(item.BiomeAId, item.BiomeBId));
            chunksByProfile = GroupByString(
                BoundaryChunks.Values,
                item => item.BoundaryProfileId);
            chunksByPair = GroupByPair(
                BoundaryChunks.Values,
                item => new DirectedBiomePair(item.BiomeAId, item.BiomeBId));
        }

        public IReadOnlyDictionary<string, BiomeTypeDefinition> BiomeTypes { get; }
        public IReadOnlyDictionary<string, BiomePatchRuleDefinition> BiomePatchRules { get; }
        public IReadOnlyDictionary<string, BiomeBoundaryProfileDefinition> BoundaryProfiles { get; }
        public IReadOnlyDictionary<string, BiomeBoundaryPairRuleDefinition> BoundaryPairRules { get; }
        public IReadOnlyDictionary<string, BoundaryChunkDefinition> BoundaryChunks { get; }

        public IReadOnlyList<BiomePatchRuleDefinition> GetBiomePatchRules(string biomeId)
        {
            return Get(patchRulesByBiome, biomeId, EmptyPatchRules);
        }

        public IReadOnlyList<BiomeBoundaryPairRuleDefinition> GetBoundaryPairRules(
            string biomeAId,
            string biomeBId)
        {
            return Get(pairRulesByPair, Pair(biomeAId, biomeBId), EmptyPairRules);
        }

        public IReadOnlyList<BoundaryChunkDefinition> GetBoundaryChunksByProfile(
            string boundaryProfileId)
        {
            return Get(chunksByProfile, boundaryProfileId, EmptyChunks);
        }

        public IReadOnlyList<BoundaryChunkDefinition> GetBoundaryChunks(
            string biomeAId,
            string biomeBId)
        {
            return Get(chunksByPair, Pair(biomeAId, biomeBId), EmptyChunks);
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

        private static IReadOnlyDictionary<string, IReadOnlyList<T>> GroupByString<T>(
            IEnumerable<T> source,
            Func<T, string> keySelector)
        {
            var dictionary = new SortedDictionary<string, IReadOnlyList<T>>(StringComparer.Ordinal);
            foreach (var group in source.GroupBy(keySelector, StringComparer.Ordinal))
            {
                dictionary.Add(group.Key, ToList(group));
            }

            return new ReadOnlyDictionary<string, IReadOnlyList<T>>(dictionary);
        }

        private static IReadOnlyDictionary<DirectedBiomePair, IReadOnlyList<T>> GroupByPair<T>(
            IEnumerable<T> source,
            Func<T, DirectedBiomePair> keySelector)
        {
            var dictionary = new Dictionary<DirectedBiomePair, IReadOnlyList<T>>();
            foreach (var group in source.GroupBy(keySelector))
            {
                dictionary.Add(group.Key, ToList(group));
            }

            return new ReadOnlyDictionary<DirectedBiomePair, IReadOnlyList<T>>(dictionary);
        }

        private static IReadOnlyList<T> Get<T>(
            IReadOnlyDictionary<string, IReadOnlyList<T>> dictionary,
            string key,
            IReadOnlyList<T> empty)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return dictionary.TryGetValue(key, out var values) ? values : empty;
        }

        private static IReadOnlyList<T> Get<T>(
            IReadOnlyDictionary<DirectedBiomePair, IReadOnlyList<T>> dictionary,
            DirectedBiomePair key,
            IReadOnlyList<T> empty)
        {
            return dictionary.TryGetValue(key, out var values) ? values : empty;
        }

        private static DirectedBiomePair Pair(string biomeAId, string biomeBId)
        {
            if (biomeAId == null) throw new ArgumentNullException(nameof(biomeAId));
            if (biomeBId == null) throw new ArgumentNullException(nameof(biomeBId));
            return new DirectedBiomePair(biomeAId, biomeBId);
        }

        private readonly struct DirectedBiomePair : IEquatable<DirectedBiomePair>
        {
            public DirectedBiomePair(string biomeAId, string biomeBId)
            {
                BiomeAId = biomeAId;
                BiomeBId = biomeBId;
            }

            private string BiomeAId { get; }
            private string BiomeBId { get; }

            public bool Equals(DirectedBiomePair other)
            {
                return string.Equals(BiomeAId, other.BiomeAId, StringComparison.Ordinal) &&
                       string.Equals(BiomeBId, other.BiomeBId, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is DirectedBiomePair other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (StringComparer.Ordinal.GetHashCode(BiomeAId) * 397) ^
                           StringComparer.Ordinal.GetHashCode(BiomeBId);
                }
            }
        }
    }
}
