using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBiomePairCatalog
    {
        private static readonly IReadOnlyList<MoonpalaceBiomeId> CanonicalBiomes =
            new ReadOnlyCollection<MoonpalaceBiomeId>(new[]
            {
                MoonpalaceBiomeId.MoonCrater,
                MoonpalaceBiomeId.CassiaRoot,
                MoonpalaceBiomeId.AbandonedMill,
                MoonpalaceBiomeId.MoonDough,
            });

        private readonly IReadOnlyList<MoonpalaceBiomePair> pairs;
        private readonly IReadOnlyList<MoonpalaceBiomePairDefinition> definitions;
        private readonly IReadOnlyDictionary<MoonpalaceBiomePair, MoonpalaceBiomePairDefinition> definitionsByPair;

        public MoonpalaceBiomePairCatalog(IEnumerable<MoonpalaceBiomePairDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));

            var definitionCopy = definitions.ToArray();
            var expectedPairs = CreateCanonicalPairs();
            if (definitionCopy.Length != expectedPairs.Length)
            {
                throw new ArgumentException(
                    "The Moonpalace catalog must contain exactly six pair definitions.",
                    nameof(definitions));
            }

            if (definitionCopy.Any(definition => definition == null))
            {
                throw new ArgumentException("Pair definitions cannot contain null.", nameof(definitions));
            }

            var duplicate = definitionCopy
                .GroupBy(definition => definition.Pair)
                .FirstOrDefault(group => group.Count() != 1);
            if (duplicate != null)
            {
                throw new ArgumentException(
                    "Duplicate Moonpalace pair definition: " + duplicate.Key.PairId,
                    nameof(definitions));
            }

            var sourceByPair = definitionCopy.ToDictionary(definition => definition.Pair);
            var missingPair = expectedPairs.FirstOrDefault(pair => !sourceByPair.ContainsKey(pair));
            if (missingPair.IsDefined)
            {
                throw new ArgumentException(
                    "Missing Moonpalace pair definition: " + missingPair.PairId,
                    nameof(definitions));
            }

            pairs = new ReadOnlyCollection<MoonpalaceBiomePair>(expectedPairs);
            this.definitions = new ReadOnlyCollection<MoonpalaceBiomePairDefinition>(
                expectedPairs.Select(pair => sourceByPair[pair]).ToArray());
            definitionsByPair = new ReadOnlyDictionary<MoonpalaceBiomePair, MoonpalaceBiomePairDefinition>(
                expectedPairs.ToDictionary(pair => pair, pair => sourceByPair[pair]));
        }

        public static MoonpalaceBiomePairCatalog Canonical { get; } =
            new MoonpalaceBiomePairCatalog(
                CreateCanonicalPairs().Select(MoonpalaceBiomePairDefinition.CreateCanonical));

        public IReadOnlyList<MoonpalaceBiomeId> Biomes => CanonicalBiomes;
        public IReadOnlyList<MoonpalaceBiomePair> Pairs => pairs;
        public IReadOnlyList<MoonpalaceBiomePairDefinition> Definitions => definitions;
        public string Signature => string.Join("\n", definitions.Select(definition => definition.Signature));

        public MoonpalaceBiomePairDefinition GetDefinition(MoonpalaceBiomePair pair)
        {
            if (!pair.IsDefined) throw new ArgumentException("Pair is undefined.", nameof(pair));
            if (!definitionsByPair.TryGetValue(pair, out var definition))
            {
                throw new KeyNotFoundException("Unknown Moonpalace biome pair: " + pair.PairId);
            }

            return definition;
        }

        public bool TryGetDefinition(
            MoonpalaceBiomePair pair,
            out MoonpalaceBiomePairDefinition definition)
        {
            if (!pair.IsDefined)
            {
                definition = null;
                return false;
            }

            return definitionsByPair.TryGetValue(pair, out definition);
        }

        private static MoonpalaceBiomePair[] CreateCanonicalPairs()
        {
            var result = new List<MoonpalaceBiomePair>(6);
            for (var first = 0; first < CanonicalBiomes.Count; first++)
            {
                for (var second = first + 1; second < CanonicalBiomes.Count; second++)
                {
                    result.Add(new MoonpalaceBiomePair(
                        CanonicalBiomes[first],
                        CanonicalBiomes[second]));
                }
            }

            return result.ToArray();
        }
    }
}
