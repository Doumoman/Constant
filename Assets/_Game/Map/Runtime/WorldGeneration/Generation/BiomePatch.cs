using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class BiomePatch
    {
        private readonly IReadOnlyList<BiomePatchSeed> seeds;
        private readonly IReadOnlyList<int> sectorIndices;
        private readonly HashSet<int> sectorIndexSet;

        public BiomePatch(
            BiomePatchId id,
            string biomeId,
            string patchRuleId,
            BiomePatchRole role,
            IEnumerable<BiomePatchSeed> seeds,
            IEnumerable<int> sectorIndices)
        {
            if (!id.IsValid)
                throw new ArgumentException("Patch ID must be valid.", nameof(id));
            ReservationValidation.RequireCanonicalId(biomeId, nameof(biomeId), false);
            ReservationValidation.RequireCanonicalId(patchRuleId, nameof(patchRuleId), false);
            if (!BiomePatchModelValidation.IsDefined(role))
                throw new ArgumentOutOfRangeException(nameof(role));
            if (seeds == null)
                throw new ArgumentNullException(nameof(seeds));
            if (sectorIndices == null)
                throw new ArgumentNullException(nameof(sectorIndices));

            var sectors = new List<int>(sectorIndices);
            var sectorSet = new HashSet<int>();
            foreach (var index in sectors)
            {
                if (index < 0 || index >= WorldGenConstants.SectorCount)
                    throw new ArgumentOutOfRangeException(nameof(sectorIndices));
                if (!sectorSet.Add(index))
                    throw new ArgumentException("Patch sector indices must be unique.", nameof(sectorIndices));
            }
            sectors.Sort();

            var seedList = new List<BiomePatchSeed>(seeds);
            if (seedList.Count == 0)
                throw new ArgumentException("Every patch requires at least one seed.", nameof(seeds));
            var seedIndices = new HashSet<int>();
            foreach (var seed in seedList)
            {
                if (seed == null)
                    throw new ArgumentException("Seeds cannot contain null.", nameof(seeds));
                if (!seedIndices.Add(seed.SectorIndex))
                    throw new ArgumentException("Seed sector indices must be unique.", nameof(seeds));
                if (seed.Role != role)
                    throw new ArgumentException("Seed role must match patch role.", nameof(seeds));
                if (!sectorSet.Contains(seed.SectorIndex))
                    throw new ArgumentException("Every seed must belong to the patch.", nameof(seeds));
                if (role == BiomePatchRole.Core)
                {
                    if (!seed.SourceSiteReservationId.HasValue || !seed.SourceSiteReservationId.Value.IsValid)
                        throw new ArgumentException("Core seeds require source sites.", nameof(seeds));
                }
                else if (seed.SourceSiteReservationId.HasValue)
                {
                    throw new ArgumentException("Non-Core seeds cannot have source sites.", nameof(seeds));
                }
            }
            seedList.Sort((left, right) => left.SectorIndex.CompareTo(right.SectorIndex));

            Id = id;
            BiomeId = biomeId;
            PatchRuleId = patchRuleId;
            Role = role;
            this.seeds = new ReadOnlyCollection<BiomePatchSeed>(seedList);
            this.sectorIndices = new ReadOnlyCollection<int>(sectors);
            sectorIndexSet = sectorSet;
        }

        public BiomePatchId Id { get; }
        public string BiomeId { get; }
        public string PatchRuleId { get; }
        public BiomePatchRole Role { get; }
        public IReadOnlyList<BiomePatchSeed> Seeds => seeds;
        public IReadOnlyList<int> SectorIndices => sectorIndices;
        public int SectorCount => sectorIndices.Count;

        public bool ContainsSector(int sectorIndex)
        {
            return sectorIndexSet.Contains(sectorIndex);
        }
    }
}
