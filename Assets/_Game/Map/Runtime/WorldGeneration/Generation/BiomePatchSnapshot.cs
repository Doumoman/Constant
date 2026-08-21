using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class BiomePatchSnapshot
    {
        private readonly IReadOnlyList<BiomePatch> patches;
        private readonly IReadOnlyList<BiomeSectorOwnership> sectors;
        private readonly IReadOnlyList<BiomePatchSiteBinding> siteBindings;
        private readonly IReadOnlyDictionary<BiomePatchId, BiomePatch> patchesById;
        private readonly IReadOnlyDictionary<SiteReservationId, BiomePatchSiteBinding> bindingsBySiteId;

        public BiomePatchSnapshot(
            ulong seed,
            IEnumerable<BiomePatch> patches,
            IEnumerable<BiomeSectorOwnership> sectors,
            IEnumerable<BiomePatchSiteBinding> siteBindings)
        {
            if (patches == null) throw new ArgumentNullException(nameof(patches));
            if (sectors == null) throw new ArgumentNullException(nameof(sectors));
            if (siteBindings == null) throw new ArgumentNullException(nameof(siteBindings));

            var patchList = new List<BiomePatch>(patches);
            var patchLookup = new Dictionary<BiomePatchId, BiomePatch>();
            foreach (var patch in patchList)
            {
                if (patch == null) throw new ArgumentException("Patches cannot contain null.", nameof(patches));
                if (!patchLookup.TryAdd(patch.Id, patch))
                    throw new ArgumentException("Patch IDs must be unique.", nameof(patches));
            }
            patchList.Sort((left, right) => left.Id.CompareTo(right.Id));

            var sectorList = new List<BiomeSectorOwnership>(sectors);
            if (sectorList.Count != WorldGenConstants.SectorCount)
                throw new ArgumentException("Exactly 169 sector ownership rows are required.", nameof(sectors));
            var sectorLookup = new Dictionary<int, BiomeSectorOwnership>();
            var coordinateSet = new HashSet<SectorCoord>();
            foreach (var ownership in sectorList)
            {
                if (ownership == null)
                    throw new ArgumentException("Sector ownership rows cannot contain null.", nameof(sectors));
                if (!sectorLookup.TryAdd(ownership.SectorIndex, ownership))
                    throw new ArgumentException("Sector indices must be unique.", nameof(sectors));
                if (!coordinateSet.Add(ownership.Sector))
                    throw new ArgumentException("Sector coordinates must be unique.", nameof(sectors));
                if (ownership.Sector != WorldGridIndex.ToCoordinate(ownership.SectorIndex))
                    throw new ArgumentException("Sector index and coordinate must match.", nameof(sectors));
            }
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
                if (!sectorLookup.ContainsKey(index))
                    throw new ArgumentException("Sector index set must be exact 0..168.", nameof(sectors));
            sectorList.Sort((left, right) => left.SectorIndex.CompareTo(right.SectorIndex));

            var bindingList = new List<BiomePatchSiteBinding>(siteBindings);
            var bindingLookup = new Dictionary<SiteReservationId, BiomePatchSiteBinding>();
            foreach (var binding in bindingList)
            {
                if (binding == null)
                    throw new ArgumentException("Site bindings cannot contain null.", nameof(siteBindings));
                if (!bindingLookup.TryAdd(binding.SiteReservationId, binding))
                    throw new ArgumentException("Site binding IDs must be unique.", nameof(siteBindings));
            }
            bindingList.Sort((left, right) => left.SiteReservationId.CompareTo(right.SiteReservationId));

            ValidatePatchOwnership(patchList, patchLookup, sectorLookup);
            ValidateSiteBindings(patchList, patchLookup, sectorLookup, bindingList, bindingLookup);

            var assigned = 0;
            foreach (var ownership in sectorList)
                if (ownership.IsAssigned) assigned++;

            Seed = seed;
            AssignedSectorCount = assigned;
            UnassignedSectorCount = WorldGenConstants.SectorCount - assigned;
            IsComplete = UnassignedSectorCount == 0;
            this.patches = new ReadOnlyCollection<BiomePatch>(patchList);
            this.sectors = new ReadOnlyCollection<BiomeSectorOwnership>(sectorList);
            this.siteBindings = new ReadOnlyCollection<BiomePatchSiteBinding>(bindingList);
            patchesById = new ReadOnlyDictionary<BiomePatchId, BiomePatch>(patchLookup);
            bindingsBySiteId = new ReadOnlyDictionary<SiteReservationId, BiomePatchSiteBinding>(bindingLookup);
        }

        public ulong Seed { get; }
        public IReadOnlyList<BiomePatch> Patches => patches;
        public IReadOnlyList<BiomeSectorOwnership> Sectors => sectors;
        public IReadOnlyList<BiomePatchSiteBinding> SiteBindings => siteBindings;
        public int AssignedSectorCount { get; }
        public int UnassignedSectorCount { get; }
        public bool IsComplete { get; }

        public BiomeSectorOwnership GetSector(int index)
        {
            if (index < 0 || index >= sectors.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return sectors[index];
        }

        public bool TryGetSector(SectorCoord sector, out BiomeSectorOwnership ownership)
        {
            if (sector.X < 0 || sector.X >= WorldGenConstants.SectorColumns ||
                sector.Y < 0 || sector.Y >= WorldGenConstants.SectorRows)
            {
                ownership = null;
                return false;
            }
            ownership = sectors[WorldGridIndex.ToIndex(sector)];
            return true;
        }

        public bool TryGetPatch(BiomePatchId id, out BiomePatch patch)
        {
            return patchesById.TryGetValue(id, out patch);
        }

        public bool TryGetSiteBinding(SiteReservationId id, out BiomePatchSiteBinding binding)
        {
            return bindingsBySiteId.TryGetValue(id, out binding);
        }

        private static void ValidatePatchOwnership(
            IReadOnlyList<BiomePatch> patchList,
            IReadOnlyDictionary<BiomePatchId, BiomePatch> patchLookup,
            IReadOnlyDictionary<int, BiomeSectorOwnership> sectorLookup)
        {
            foreach (var ownership in sectorLookup.Values)
            {
                if (!ownership.IsAssigned)
                {
                    if (ownership.PrimaryBiomeId.Length != 0 || ownership.SecondaryBiomeId.Length != 0 || ownership.PatchId.HasValue)
                        throw new ArgumentException("Unassigned ownership rows cannot contain partial state.", nameof(sectorLookup));
                    continue;
                }

                if (!ownership.PatchId.HasValue || !patchLookup.TryGetValue(ownership.PatchId.Value, out var patch))
                    throw new ArgumentException("Assigned ownership must reference an existing patch.", nameof(sectorLookup));
                if (!string.Equals(ownership.PrimaryBiomeId, patch.BiomeId, StringComparison.Ordinal))
                    throw new ArgumentException("Ownership biome must match its patch.", nameof(sectorLookup));
                if (!patch.ContainsSector(ownership.SectorIndex))
                    throw new ArgumentException("Ownership sector must be present in its patch.", nameof(sectorLookup));
            }

            foreach (var patch in patchList)
            {
                foreach (var sectorIndex in patch.SectorIndices)
                {
                    var ownership = sectorLookup[sectorIndex];
                    if (!ownership.IsAssigned || !ownership.PatchId.HasValue ||
                        ownership.PatchId.Value != patch.Id ||
                        !string.Equals(ownership.PrimaryBiomeId, patch.BiomeId, StringComparison.Ordinal))
                        throw new ArgumentException("Every patch sector must have exact matching ownership.", nameof(patchList));
                }
            }
        }

        private static void ValidateSiteBindings(
            IReadOnlyList<BiomePatch> patchList,
            IReadOnlyDictionary<BiomePatchId, BiomePatch> patchLookup,
            IReadOnlyDictionary<int, BiomeSectorOwnership> sectorLookup,
            IReadOnlyList<BiomePatchSiteBinding> bindingList,
            IReadOnlyDictionary<SiteReservationId, BiomePatchSiteBinding> bindingLookup)
        {
            foreach (var binding in bindingList)
            {
                if (!patchLookup.TryGetValue(binding.PatchId, out var patch))
                    throw new ArgumentException("Site binding patch must exist.", nameof(bindingList));
                if (patch.Role != BiomePatchRole.Core)
                    throw new ArgumentException("Site bindings require Core patches.", nameof(bindingList));
                if (!string.Equals(binding.BiomeId, patch.BiomeId, StringComparison.Ordinal))
                    throw new ArgumentException("Site binding biome must match its patch.", nameof(bindingList));

                foreach (var sectorIndex in binding.OccupiedSectorIndices)
                {
                    var ownership = sectorLookup[sectorIndex];
                    if (!patch.ContainsSector(sectorIndex) || !ownership.IsAssigned ||
                        !ownership.PatchId.HasValue || ownership.PatchId.Value != patch.Id ||
                        !string.Equals(ownership.PrimaryBiomeId, binding.BiomeId, StringComparison.Ordinal))
                        throw new ArgumentException("Bound site sectors must be owned by the Core patch.", nameof(bindingList));

                    var matches = 0;
                    foreach (var seed in patch.Seeds)
                        if (seed.SectorIndex == sectorIndex && seed.SourceSiteReservationId.HasValue &&
                            seed.SourceSiteReservationId.Value == binding.SiteReservationId)
                            matches++;
                    if (matches != 1)
                        throw new ArgumentException("Each bound sector requires exactly one matching Core seed.", nameof(bindingList));
                }
            }

            foreach (var patch in patchList)
            {
                if (patch.Role != BiomePatchRole.Core) continue;
                foreach (var seed in patch.Seeds)
                {
                    var sourceId = seed.SourceSiteReservationId.Value;
                    if (!bindingLookup.TryGetValue(sourceId, out var binding) ||
                        binding.PatchId != patch.Id ||
                        !string.Equals(binding.BiomeId, patch.BiomeId, StringComparison.Ordinal) ||
                        !Contains(binding.OccupiedSectorIndices, seed.SectorIndex))
                        throw new ArgumentException("Every Core seed requires one exact reverse site binding.", nameof(patchList));
                }
            }
        }

        private static bool Contains(IReadOnlyList<int> values, int value)
        {
            for (var index = 0; index < values.Count; index++)
                if (values[index] == value) return true;
            return false;
        }
    }
}
