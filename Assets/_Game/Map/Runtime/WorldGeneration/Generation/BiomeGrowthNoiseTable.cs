using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class BiomeGrowthNoiseTable
    {
        private readonly IReadOnlyList<BiomePatchId> patchIds;
        private readonly IReadOnlyList<int> sectorIndices;
        private readonly IReadOnlyList<int> values;
        private readonly Dictionary<BiomePatchId, int> patchOrdinals;
        private readonly Dictionary<int, int> sectorOrdinals;

        internal BiomeGrowthNoiseTable(
            IEnumerable<BiomePatchId> patchIds,
            IEnumerable<int> sectorIndices,
            IEnumerable<int> values,
            int methodCallCount,
            ulong rngDrawCountBefore,
            ulong rngDrawCountAfter)
        {
            if (patchIds == null) throw new ArgumentNullException(nameof(patchIds));
            if (sectorIndices == null) throw new ArgumentNullException(nameof(sectorIndices));
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (methodCallCount < 0) throw new ArgumentOutOfRangeException(nameof(methodCallCount));
            if (rngDrawCountAfter < rngDrawCountBefore)
                throw new ArgumentOutOfRangeException(nameof(rngDrawCountAfter));

            var patches = new List<BiomePatchId>(patchIds);
            var sectors = new List<int>(sectorIndices);
            var copiedValues = new List<int>(values);
            patches.Sort();
            sectors.Sort();
            if (copiedValues.Count != checked(patches.Count * sectors.Count) ||
                methodCallCount != copiedValues.Count)
                throw new ArgumentException("Noise table dimensions are inconsistent.");

            patchOrdinals = new Dictionary<BiomePatchId, int>();
            for (var index = 0; index < patches.Count; index++)
                if (!patches[index].IsValid || !patchOrdinals.TryAdd(patches[index], index))
                    throw new ArgumentException("Noise patch IDs must be valid and unique.", nameof(patchIds));
            sectorOrdinals = new Dictionary<int, int>();
            for (var index = 0; index < sectors.Count; index++)
                if (sectors[index] < 0 || !sectorOrdinals.TryAdd(sectors[index], index))
                    throw new ArgumentException("Noise sector indices must be valid and unique.", nameof(sectorIndices));
            foreach (var value in copiedValues)
                if (value < 0 || value > 1000)
                    throw new ArgumentOutOfRangeException(nameof(values));

            this.patchIds = new ReadOnlyCollection<BiomePatchId>(patches);
            this.sectorIndices = new ReadOnlyCollection<int>(sectors);
            this.values = new ReadOnlyCollection<int>(copiedValues);
            MethodCallCount = methodCallCount;
            RngDrawCountBefore = rngDrawCountBefore;
            RngDrawCountAfter = rngDrawCountAfter;
            Checksum = ComputeChecksum(patches, sectors, copiedValues);
        }

        public IReadOnlyList<BiomePatchId> PatchIds => patchIds;
        public IReadOnlyList<int> SectorIndices => sectorIndices;
        public IReadOnlyList<int> Values => values;
        public int PatchCount => patchIds.Count;
        public int SectorCount => sectorIndices.Count;
        public int ValueCount => values.Count;
        public int MethodCallCount { get; }
        public ulong RngDrawCountBefore { get; }
        public ulong RngDrawCountAfter { get; }
        public ulong Checksum { get; }

        public int GetNoise(BiomePatchId patchId, int sectorIndex)
        {
            if (!patchOrdinals.TryGetValue(patchId, out var patchOrdinal))
                throw new KeyNotFoundException("Noise patch ID was not found: " + patchId);
            if (!sectorOrdinals.TryGetValue(sectorIndex, out var sectorOrdinal))
                throw new KeyNotFoundException("Noise sector index was not found: " + sectorIndex);
            return values[(patchOrdinal * sectorIndices.Count) + sectorOrdinal];
        }

        private static ulong ComputeChecksum(
            IReadOnlyList<BiomePatchId> patches,
            IReadOnlyList<int> sectors,
            IReadOnlyList<int> sourceValues)
        {
            unchecked
            {
                var hash = 14695981039346656037UL;
                foreach (var patch in patches)
                {
                    foreach (var character in patch.Value)
                    {
                        hash ^= character;
                        hash *= 1099511628211UL;
                    }
                    hash ^= 0xFF;
                    hash *= 1099511628211UL;
                }
                foreach (var sector in sectors)
                {
                    hash ^= (uint)sector;
                    hash *= 1099511628211UL;
                }
                foreach (var value in sourceValues)
                {
                    hash ^= (uint)value;
                    hash *= 1099511628211UL;
                }
                return hash;
            }
        }
    }
}
