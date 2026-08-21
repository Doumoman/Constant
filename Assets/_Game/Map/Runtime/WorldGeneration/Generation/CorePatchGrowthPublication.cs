using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class CorePatchGrowthPublication
    {
        private readonly IReadOnlyList<CorePatchGrowthRecord> records;

        internal CorePatchGrowthPublication(
            CorePatchInitializationPublication sourceInitialization,
            BiomePatchSnapshot snapshot,
            IEnumerable<CorePatchGrowthRecord> records)
        {
            SourceInitialization = sourceInitialization ??
                throw new ArgumentNullException(nameof(sourceInitialization));
            SourceSiteSnapshot = sourceInitialization.SourceSiteSnapshot ??
                throw new ArgumentException("Source site snapshot is required.", nameof(sourceInitialization));
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            if (records == null) throw new ArgumentNullException(nameof(records));
            if (SourceSiteSnapshot.Seed != sourceInitialization.Snapshot.Seed ||
                snapshot.Seed != SourceSiteSnapshot.Seed)
                throw new ArgumentException("Source, input, and output seeds must match.", nameof(snapshot));
            if (snapshot.IsComplete)
                throw new ArgumentException("Core growth must publish a partial snapshot.", nameof(snapshot));

            var values = new List<CorePatchGrowthRecord>(records);
            values.Sort((left, right) => left.SourceReservationId.CompareTo(right.SourceReservationId));
            if (values.Count != sourceInitialization.CorePatchCount ||
                snapshot.Patches.Count != sourceInitialization.CorePatchCount ||
                snapshot.SiteBindings.Count != sourceInitialization.CoreSiteBindingCount)
                throw new ArgumentException("Core publication counts must be preserved.", nameof(records));

            var patches = new Dictionary<BiomePatchId, BiomePatch>();
            foreach (var patch in snapshot.Patches)
            {
                if (patch == null || patch.Role != BiomePatchRole.Core || !patches.TryAdd(patch.Id, patch))
                    throw new ArgumentException("Output may contain unique Core patches only.", nameof(snapshot));
            }

            var sources = new HashSet<SiteReservationId>();
            var recordPatches = new HashSet<BiomePatchId>();
            foreach (var record in values)
            {
                if (record == null || !sources.Add(record.SourceReservationId) || !recordPatches.Add(record.PatchId))
                    throw new ArgumentException("Growth records must be unique and non-null.", nameof(records));
                if (!patches.TryGetValue(record.PatchId, out var patch) ||
                    !string.Equals(patch.BiomeId, record.BiomeId, StringComparison.Ordinal) ||
                    !string.Equals(patch.PatchRuleId, record.CorePatchRuleId, StringComparison.Ordinal) ||
                    !SequenceEqual(patch.SectorIndices, record.FinalSectorIndices))
                    throw new ArgumentException("Growth records must match output patches.", nameof(records));
                if (!snapshot.TryGetSiteBinding(record.SourceReservationId, out var binding) ||
                    binding.PatchId != record.PatchId ||
                    !SequenceEqual(binding.OccupiedSectorIndices, record.FootprintSectorIndices))
                    throw new ArgumentException("Growth records must match source bindings.", nameof(records));
            }

            foreach (var inputPatchId in sourceInitialization.CorePatchIds)
                if (!patches.ContainsKey(inputPatchId))
                    throw new ArgumentException("Input Core patch identity must be preserved.", nameof(snapshot));

            var seedCount = 0;
            foreach (var patch in snapshot.Patches) seedCount += patch.Seeds.Count;
            if (seedCount != sourceInitialization.CoreSeedCount)
                throw new ArgumentException("Core seed identity count must be preserved.", nameof(snapshot));
            if (snapshot.AssignedSectorCount + snapshot.UnassignedSectorCount != WorldGenConstants.SectorCount)
                throw new ArgumentException("Output ownership must cover the world.", nameof(snapshot));

            this.records = new ReadOnlyCollection<CorePatchGrowthRecord>(values);
            CorePatchCount = snapshot.Patches.Count;
            CoreSeedCount = seedCount;
            CoreSiteBindingCount = snapshot.SiteBindings.Count;
            AssignedSectorCount = snapshot.AssignedSectorCount;
            UnassignedSectorCount = snapshot.UnassignedSectorCount;
        }

        public CorePatchInitializationPublication SourceInitialization { get; }
        public SiteReservationSnapshot SourceSiteSnapshot { get; }
        public BiomePatchSnapshot Snapshot { get; }
        public IReadOnlyList<CorePatchGrowthRecord> Records => records;
        public int CorePatchCount { get; }
        public int CoreSeedCount { get; }
        public int CoreSiteBindingCount { get; }
        public int AssignedSectorCount { get; }
        public int UnassignedSectorCount { get; }

        private static bool SequenceEqual(IReadOnlyList<int> left, IReadOnlyList<int> right)
        {
            if (left.Count != right.Count) return false;
            for (var index = 0; index < left.Count; index++)
                if (left[index] != right[index]) return false;
            return true;
        }
    }
}
