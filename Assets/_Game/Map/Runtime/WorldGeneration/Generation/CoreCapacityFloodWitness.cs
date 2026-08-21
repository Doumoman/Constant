using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class CoreCapacityFloodWitness
    {
        private readonly IReadOnlyList<int> footprintSectorIndices;
        private readonly IReadOnlyList<int> mandatoryBufferSectorIndices;
        private readonly IReadOnlyList<int> reachableSectorIndices;
        private readonly IReadOnlyList<int> witnessSectorIndices;

        internal CoreCapacityFloodWitness(
            SitePlacementKey key,
            string biomeId,
            string corePatchRuleId,
            int seedSectorIndex,
            int minimumCoreSectorCount,
            int bufferRingSectors,
            bool canTouchWorldEdge,
            int requiredWitnessSectorCount,
            int availableConnectedSectorCount,
            IEnumerable<int> footprintSectorIndices,
            IEnumerable<int> mandatoryBufferSectorIndices,
            IEnumerable<int> reachableSectorIndices,
            IEnumerable<int> witnessSectorIndices)
        {
            if (!key.IsValid) throw new ArgumentException("A valid site key is required.", nameof(key));
            if (!SitePlacementKey.IsCanonicalId(biomeId))
                throw new ArgumentException("A canonical biome ID is required.", nameof(biomeId));
            if (!SitePlacementKey.IsCanonicalId(corePatchRuleId))
                throw new ArgumentException("A canonical Core rule ID is required.", nameof(corePatchRuleId));
            if (seedSectorIndex < 0) throw new ArgumentOutOfRangeException(nameof(seedSectorIndex));
            if (minimumCoreSectorCount < 1) throw new ArgumentOutOfRangeException(nameof(minimumCoreSectorCount));
            if (bufferRingSectors < 0) throw new ArgumentOutOfRangeException(nameof(bufferRingSectors));
            if (requiredWitnessSectorCount < minimumCoreSectorCount)
                throw new ArgumentOutOfRangeException(nameof(requiredWitnessSectorCount));
            if (availableConnectedSectorCount < requiredWitnessSectorCount)
                throw new ArgumentOutOfRangeException(nameof(availableConnectedSectorCount));

            this.footprintSectorIndices = Snapshot(footprintSectorIndices, nameof(footprintSectorIndices));
            this.mandatoryBufferSectorIndices = Snapshot(mandatoryBufferSectorIndices, nameof(mandatoryBufferSectorIndices));
            this.reachableSectorIndices = Snapshot(reachableSectorIndices, nameof(reachableSectorIndices));
            this.witnessSectorIndices = Snapshot(witnessSectorIndices, nameof(witnessSectorIndices));
            if (this.footprintSectorIndices.Count == 0 ||
                seedSectorIndex != this.footprintSectorIndices[0] ||
                this.witnessSectorIndices.Count != requiredWitnessSectorCount ||
                !ContainsAll(this.mandatoryBufferSectorIndices, this.footprintSectorIndices) ||
                !ContainsAll(this.reachableSectorIndices, this.mandatoryBufferSectorIndices) ||
                !ContainsAll(this.witnessSectorIndices, this.mandatoryBufferSectorIndices))
            {
                throw new ArgumentException("Capacity witness sets violate their inclusion contract.");
            }

            Key = key;
            BiomeId = biomeId;
            CorePatchRuleId = corePatchRuleId;
            SeedSectorIndex = seedSectorIndex;
            MinimumCoreSectorCount = minimumCoreSectorCount;
            BufferRingSectors = bufferRingSectors;
            CanTouchWorldEdge = canTouchWorldEdge;
            RequiredWitnessSectorCount = requiredWitnessSectorCount;
            AvailableConnectedSectorCount = availableConnectedSectorCount;
            AdditionalClaimedSectorCount = this.witnessSectorIndices.Count -
                                           this.mandatoryBufferSectorIndices.Count;
        }

        public SitePlacementKey Key { get; }
        public string BiomeId { get; }
        public string CorePatchRuleId { get; }
        public int SeedSectorIndex { get; }
        public int MinimumCoreSectorCount { get; }
        public int BufferRingSectors { get; }
        public bool CanTouchWorldEdge { get; }
        public int RequiredWitnessSectorCount { get; }
        public int AvailableConnectedSectorCount { get; }
        public IReadOnlyList<int> FootprintSectorIndices => footprintSectorIndices;
        public IReadOnlyList<int> MandatoryBufferSectorIndices => mandatoryBufferSectorIndices;
        public IReadOnlyList<int> ReachableSectorIndices => reachableSectorIndices;
        public IReadOnlyList<int> WitnessSectorIndices => witnessSectorIndices;
        public int AdditionalClaimedSectorCount { get; }

        public bool ContainsWitnessSector(int sectorIndex)
        {
            var low = 0;
            var high = witnessSectorIndices.Count - 1;
            while (low <= high)
            {
                var middle = low + ((high - low) / 2);
                var comparison = witnessSectorIndices[middle].CompareTo(sectorIndex);
                if (comparison == 0) return true;
                if (comparison < 0) low = middle + 1;
                else high = middle - 1;
            }
            return false;
        }

        private static IReadOnlyList<int> Snapshot(IEnumerable<int> source, string parameterName)
        {
            if (source == null) throw new ArgumentNullException(parameterName);
            var values = new List<int>(source);
            values.Sort();
            for (var index = 0; index < values.Count; index++)
            {
                if (values[index] < 0 || values[index] >= WorldGenConstants.SectorCount)
                    throw new ArgumentOutOfRangeException(parameterName);
                if (index > 0 && values[index - 1] == values[index])
                    throw new ArgumentException("Sector indices must be unique.", parameterName);
            }
            return new ReadOnlyCollection<int>(values);
        }

        private static bool ContainsAll(IReadOnlyList<int> superset, IReadOnlyList<int> subset)
        {
            var first = 0;
            var second = 0;
            while (first < superset.Count && second < subset.Count)
            {
                if (superset[first] < subset[second]) first++;
                else if (superset[first] == subset[second]) { first++; second++; }
                else return false;
            }
            return second == subset.Count;
        }
    }
}
