using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class IntrusionPlacementPublication
    {
        private readonly IReadOnlyList<IntrusionPlacementRecord> intrusions;

        internal IntrusionPlacementPublication(
            MultiSeedBiomeGrowthPublication sourceGrowth,
            BiomePatchSnapshot snapshot,
            IEnumerable<IntrusionPlacementRecord> intrusions)
        {
            SourceGrowth = sourceGrowth ?? throw new ArgumentNullException(nameof(sourceGrowth));
            SourceSiteSnapshot = sourceGrowth.SourceSiteSnapshot ??
                throw new ArgumentException("Source site snapshot is required.", nameof(sourceGrowth));
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            if (intrusions == null) throw new ArgumentNullException(nameof(intrusions));
            if (sourceGrowth.Snapshot == null || sourceGrowth.Snapshot.Seed != SourceSiteSnapshot.Seed ||
                snapshot.Seed != SourceSiteSnapshot.Seed)
                throw new ArgumentException("Source and output seeds must match.", nameof(snapshot));

            var records = new List<IntrusionPlacementRecord>(intrusions);
            records.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
            var recordPatches = new HashSet<BiomePatchId>();
            var recordSectors = new HashSet<int>();
            for (var index = 0; index < records.Count; index++)
            {
                var record = records[index];
                if (record == null || record.Sequence != index ||
                    !recordPatches.Add(record.IntrusionPatchId) || !recordSectors.Add(record.SectorIndex))
                    throw new ArgumentException("Intrusion records must be ordered and unique.", nameof(intrusions));
                if (!snapshot.TryGetPatch(record.IntrusionPatchId, out var patch) ||
                    patch.Role != BiomePatchRole.Intrusion || patch.SectorCount != 1 ||
                    patch.Seeds.Count != 1 || patch.SectorIndices[0] != record.SectorIndex ||
                    patch.Seeds[0].SectorIndex != record.SectorIndex ||
                    patch.Seeds[0].SourceSiteReservationId.HasValue ||
                    !string.Equals(patch.BiomeId, record.IntruderBiomeId, StringComparison.Ordinal) ||
                    !string.Equals(patch.PatchRuleId, record.IntrusionRuleId, StringComparison.Ordinal))
                    throw new ArgumentException("Intrusion record does not match its patch.", nameof(snapshot));
                var ownership = snapshot.GetSector(record.SectorIndex);
                if (!ownership.IsAssigned || !ownership.PatchId.HasValue ||
                    ownership.PatchId.Value != record.IntrusionPatchId ||
                    !string.Equals(ownership.PrimaryBiomeId, record.IntruderBiomeId, StringComparison.Ordinal) ||
                    ownership.SecondaryBiomeId.Length != 0)
                    throw new ArgumentException("Intrusion record does not match ownership.", nameof(snapshot));
            }

            var core = 0;
            var satellite = 0;
            var intrusion = 0;
            foreach (var patch in snapshot.Patches)
            {
                if (patch.Role == BiomePatchRole.Core) core++;
                else if (patch.Role == BiomePatchRole.Satellite) satellite++;
                else if (patch.Role == BiomePatchRole.Intrusion)
                {
                    intrusion++;
                    if (!recordPatches.Contains(patch.Id))
                        throw new ArgumentException("Every Intrusion patch requires one record.", nameof(snapshot));
                }
                else throw new ArgumentException("Patch role is undefined.", nameof(snapshot));
            }
            if (core != 4 || intrusion != records.Count ||
                snapshot.Patches.Count != sourceGrowth.PatchCount + records.Count ||
                snapshot.SiteBindings.Count != sourceGrowth.Snapshot.SiteBindings.Count ||
                snapshot.AssignedSectorCount != sourceGrowth.FinalAssignedSectorCount ||
                snapshot.UnassignedSectorCount != sourceGrowth.FinalUnassignedReservedSectorCount ||
                snapshot.AssignedSectorCount + snapshot.UnassignedSectorCount != WorldGenConstants.SectorCount)
                throw new ArgumentException("Publication conservation is invalid.", nameof(snapshot));

            for (var index = 0; index < snapshot.SiteBindings.Count; index++)
                if (!ReferenceEquals(snapshot.SiteBindings[index], sourceGrowth.Snapshot.SiteBindings[index]))
                    throw new ArgumentException("Core binding objects must be preserved.", nameof(snapshot));

            this.intrusions = new ReadOnlyCollection<IntrusionPlacementRecord>(records);
            CorePatchCount = core;
            SatellitePatchCount = satellite;
            IntrusionPatchCount = intrusion;
            TotalPatchCount = snapshot.Patches.Count;
            CoreSiteBindingCount = snapshot.SiteBindings.Count;
            AssignedSectorCount = snapshot.AssignedSectorCount;
            UnassignedSectorCount = snapshot.UnassignedSectorCount;
        }

        public MultiSeedBiomeGrowthPublication SourceGrowth { get; }
        public SiteReservationSnapshot SourceSiteSnapshot { get; }
        public BiomePatchSnapshot Snapshot { get; }
        public IReadOnlyList<IntrusionPlacementRecord> Intrusions => intrusions;
        public int CorePatchCount { get; }
        public int SatellitePatchCount { get; }
        public int IntrusionPatchCount { get; }
        public int TotalPatchCount { get; }
        public int CoreSiteBindingCount { get; }
        public int AssignedSectorCount { get; }
        public int UnassignedSectorCount { get; }
    }
}
