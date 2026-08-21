using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MultiSeedBiomeGrowthDiagnostics
    {
        private readonly IReadOnlyDictionary<BiomePatchId, int> patchSectorCounts;
        private readonly IReadOnlyDictionary<string, int> biomeSectorCounts;

        internal MultiSeedBiomeGrowthDiagnostics(
            ulong worldSeed,
            int initialPatchCount,
            int initialAssignedSectorCount,
            int targetUnassignedSectorCount,
            int hardBlockedReservedSectorCount,
            int targetOwnedSectorCount,
            int aggregateLegalCapacity,
            int minimumPhaseClaimCount,
            int competitiveClaimCount,
            int finalAssignedSectorCount,
            int finalUnassignedSectorCount,
            BiomeGrowthNoiseTable noiseTable,
            ulong rngDrawCountBefore,
            ulong rngDrawCountAfter,
            int reservationPenaltyClaimCount,
            int patchOverlapCount,
            int disconnectedPatchCount,
            IEnumerable<KeyValuePair<BiomePatchId, int>> patchSectorCounts,
            IEnumerable<KeyValuePair<string, int>> biomeSectorCounts)
        {
            if (initialPatchCount < 0 || initialAssignedSectorCount < 0 ||
                targetUnassignedSectorCount < 0 || hardBlockedReservedSectorCount < 0 ||
                targetOwnedSectorCount < 0 || aggregateLegalCapacity < 0 ||
                minimumPhaseClaimCount < 0 || competitiveClaimCount < 0 ||
                finalAssignedSectorCount < 0 || finalUnassignedSectorCount < 0 ||
                reservationPenaltyClaimCount < 0 || patchOverlapCount < 0 ||
                disconnectedPatchCount < 0)
                throw new ArgumentOutOfRangeException(nameof(initialPatchCount));
            if (rngDrawCountAfter < rngDrawCountBefore)
                throw new ArgumentOutOfRangeException(nameof(rngDrawCountAfter));
            if (patchSectorCounts == null) throw new ArgumentNullException(nameof(patchSectorCounts));
            if (biomeSectorCounts == null) throw new ArgumentNullException(nameof(biomeSectorCounts));

            var patchValues = new SortedDictionary<BiomePatchId, int>();
            foreach (var pair in patchSectorCounts)
            {
                if (!pair.Key.IsValid || pair.Value < 0 || !patchValues.TryAdd(pair.Key, pair.Value))
                    throw new ArgumentException("Patch diagnostics must be unique and non-negative.", nameof(patchSectorCounts));
            }
            var biomeValues = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in biomeSectorCounts)
            {
                ReservationValidation.RequireCanonicalId(pair.Key, nameof(biomeSectorCounts), false);
                if (pair.Value < 0 || !biomeValues.TryAdd(pair.Key, pair.Value))
                    throw new ArgumentException("Biome diagnostics must be unique and non-negative.", nameof(biomeSectorCounts));
            }

            WorldSeed = worldSeed;
            InitialPatchCount = initialPatchCount;
            InitialAssignedSectorCount = initialAssignedSectorCount;
            TargetUnassignedSectorCount = targetUnassignedSectorCount;
            HardBlockedReservedSectorCount = hardBlockedReservedSectorCount;
            TargetOwnedSectorCount = targetOwnedSectorCount;
            AggregateLegalCapacity = aggregateLegalCapacity;
            MinimumPhaseClaimCount = minimumPhaseClaimCount;
            CompetitiveClaimCount = competitiveClaimCount;
            TotalClaimCount = checked(minimumPhaseClaimCount + competitiveClaimCount);
            FinalAssignedSectorCount = finalAssignedSectorCount;
            FinalUnassignedSectorCount = finalUnassignedSectorCount;
            NoiseTable = noiseTable;
            NoiseValueCount = noiseTable == null ? 0 : noiseTable.ValueCount;
            NoiseMethodCallCount = noiseTable == null ? 0 : noiseTable.MethodCallCount;
            RngDrawCountBefore = rngDrawCountBefore;
            RngDrawCountAfter = rngDrawCountAfter;
            NoiseChecksum = noiseTable == null ? 0UL : noiseTable.Checksum;
            ReservationPenaltyClaimCount = reservationPenaltyClaimCount;
            PatchOverlapCount = patchOverlapCount;
            DisconnectedPatchCount = disconnectedPatchCount;
            this.patchSectorCounts = new ReadOnlyDictionary<BiomePatchId, int>(patchValues);
            this.biomeSectorCounts = new ReadOnlyDictionary<string, int>(biomeValues);
        }

        public ulong WorldSeed { get; }
        public int InitialPatchCount { get; }
        public int InitialAssignedSectorCount { get; }
        public int TargetUnassignedSectorCount { get; }
        public int HardBlockedReservedSectorCount { get; }
        public int TargetOwnedSectorCount { get; }
        public int AggregateLegalCapacity { get; }
        public int MinimumPhaseClaimCount { get; }
        public int CompetitiveClaimCount { get; }
        public int TotalClaimCount { get; }
        public int FinalAssignedSectorCount { get; }
        public int FinalUnassignedSectorCount { get; }
        public BiomeGrowthNoiseTable NoiseTable { get; }
        public int NoiseValueCount { get; }
        public int NoiseMethodCallCount { get; }
        public ulong RngDrawCountBefore { get; }
        public ulong RngDrawCountAfter { get; }
        public ulong NoiseChecksum { get; }
        public int ReservationPenaltyClaimCount { get; }
        public int PatchOverlapCount { get; }
        public int DisconnectedPatchCount { get; }
        public IReadOnlyDictionary<BiomePatchId, int> PatchSectorCounts => patchSectorCounts;
        public IReadOnlyDictionary<string, int> BiomeSectorCounts => biomeSectorCounts;
    }
}
