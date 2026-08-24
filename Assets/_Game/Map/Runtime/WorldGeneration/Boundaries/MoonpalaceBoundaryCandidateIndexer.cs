using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryCandidateIndexer
    {
        private readonly MoonpalaceBiomePairCatalog catalog;

        public MoonpalaceBoundaryCandidateIndexer(MoonpalaceBiomePairCatalog catalog)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public static MoonpalaceBoundaryCandidateIndexer Canonical { get; } =
            new MoonpalaceBoundaryCandidateIndexer(MoonpalaceBiomePairCatalog.Canonical);

        public MoonpalaceBoundaryCandidateIndex Build(
            IEnumerable<MoonpalaceBoundaryCandidateDefinition> candidates)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));

            var groups = new Dictionary<MoonpalaceBoundaryCandidateKey,
                List<MoonpalaceBoundaryCandidateDefinition>>();
            var candidateIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var candidate in candidates)
            {
                if (candidate == null)
                {
                    throw new ArgumentException("Candidates cannot contain null.", nameof(candidates));
                }

                if (!candidateIds.Add(candidate.CandidateId))
                {
                    throw new ArgumentException(
                        "Duplicate boundary candidate ID: " + candidate.CandidateId,
                        nameof(candidates));
                }

                if (!catalog.TryGetDefinition(candidate.Pair, out var pairDefinition))
                {
                    throw new ArgumentException(
                        "Unknown Moonpalace biome pair: " + candidate.Pair.PairId,
                        nameof(candidates));
                }

                if (!pairDefinition.Supports(candidate.Orientation))
                {
                    throw new ArgumentException(
                        "The biome pair does not support the candidate orientation.",
                        nameof(candidates));
                }

                if (!groups.TryGetValue(candidate.Key, out var group))
                {
                    group = new List<MoonpalaceBoundaryCandidateDefinition>();
                    groups.Add(candidate.Key, group);
                }

                group.Add(candidate);
            }

            var entries = groups
                .OrderBy(pair => pair.Key)
                .Select(pair => new MoonpalaceBoundaryCandidateIndexEntry(
                    pair.Key,
                    pair.Value
                        .OrderBy(candidate => candidate.CandidateId, StringComparer.Ordinal)
                        .ThenBy(candidate => candidate.Weight)
                        .ThenBy(candidate => candidate.Signature, StringComparer.Ordinal)))
                .ToArray();

            return new MoonpalaceBoundaryCandidateIndex(entries);
        }
    }
}
