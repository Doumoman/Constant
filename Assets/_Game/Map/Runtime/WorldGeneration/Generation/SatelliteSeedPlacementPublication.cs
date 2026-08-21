using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SatelliteSeedPlacementPublication
    {
        private readonly IReadOnlyList<SatelliteSeedPlacementRecord> satelliteSeeds;

        internal SatelliteSeedPlacementPublication(
            CorePatchGrowthPublication sourceGrowth,
            BiomePatchSnapshot snapshot,
            IEnumerable<SatelliteSeedPlacementRecord> satelliteSeeds)
        {
            SourceGrowth = sourceGrowth ?? throw new ArgumentNullException(nameof(sourceGrowth));
            SourceSiteSnapshot = sourceGrowth.SourceSiteSnapshot ??
                throw new ArgumentException("Source site snapshot is required.", nameof(sourceGrowth));
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            if (satelliteSeeds == null) throw new ArgumentNullException(nameof(satelliteSeeds));
            if (sourceGrowth.Snapshot == null ||
                sourceGrowth.Snapshot.Seed != SourceSiteSnapshot.Seed ||
                snapshot.Seed != SourceSiteSnapshot.Seed)
                throw new ArgumentException("Source, input, and output seeds must match.", nameof(snapshot));
            if (snapshot.IsComplete)
                throw new ArgumentException("Satellite placement must publish a partial snapshot.", nameof(snapshot));

            var records = new List<SatelliteSeedPlacementRecord>(satelliteSeeds);
            records.Sort(CompareRecords);
            var recordPatches = new HashSet<BiomePatchId>();
            var recordSectors = new HashSet<int>();
            foreach (var record in records)
            {
                if (record == null || !recordPatches.Add(record.PatchId) ||
                    !recordSectors.Add(record.SectorIndex))
                    throw new ArgumentException("Satellite records must be non-null and unique.", nameof(satelliteSeeds));
                if (!snapshot.TryGetPatch(record.PatchId, out var patch) ||
                    patch.Role != BiomePatchRole.Satellite ||
                    !string.Equals(patch.BiomeId, record.BiomeId, StringComparison.Ordinal) ||
                    !string.Equals(patch.PatchRuleId, record.PatchRuleId, StringComparison.Ordinal) ||
                    patch.SectorCount != 1 || patch.Seeds.Count != 1 ||
                    patch.SectorIndices[0] != record.SectorIndex ||
                    patch.Seeds[0].SectorIndex != record.SectorIndex ||
                    patch.Seeds[0].Role != BiomePatchRole.Satellite ||
                    patch.Seeds[0].SourceSiteReservationId.HasValue)
                    throw new ArgumentException("A Satellite record does not match its one-cell patch.", nameof(satelliteSeeds));
                var ownership = snapshot.GetSector(record.SectorIndex);
                if (!ownership.IsAssigned || !ownership.PatchId.HasValue ||
                    ownership.PatchId.Value != record.PatchId ||
                    !string.Equals(ownership.PrimaryBiomeId, record.BiomeId, StringComparison.Ordinal) ||
                    ownership.SecondaryBiomeId.Length != 0)
                    throw new ArgumentException("A Satellite record does not match ownership.", nameof(snapshot));
            }

            var coreCount = 0;
            var satelliteCount = 0;
            foreach (var patch in snapshot.Patches)
            {
                if (patch.Role == BiomePatchRole.Core)
                {
                    coreCount++;
                    if (!sourceGrowth.Snapshot.TryGetPatch(patch.Id, out var inputPatch) ||
                        !ReferenceEquals(patch, inputPatch))
                        throw new ArgumentException("Core patch objects must be preserved.", nameof(snapshot));
                }
                else if (patch.Role == BiomePatchRole.Satellite)
                {
                    satelliteCount++;
                    if (!recordPatches.Contains(patch.Id))
                        throw new ArgumentException("Every Satellite patch requires one record.", nameof(snapshot));
                }
                else
                {
                    throw new ArgumentException("Intrusion patches are outside this publication.", nameof(snapshot));
                }
            }

            if (coreCount != sourceGrowth.CorePatchCount ||
                satelliteCount != records.Count ||
                snapshot.SiteBindings.Count != sourceGrowth.CoreSiteBindingCount)
                throw new ArgumentException("Publication patch or binding counts are inconsistent.", nameof(snapshot));
            for (var index = 0; index < snapshot.SiteBindings.Count; index++)
                if (!ReferenceEquals(snapshot.SiteBindings[index], sourceGrowth.Snapshot.SiteBindings[index]))
                    throw new ArgumentException("Core site binding objects must be preserved.", nameof(snapshot));
            if (snapshot.AssignedSectorCount != sourceGrowth.AssignedSectorCount + records.Count ||
                snapshot.AssignedSectorCount + snapshot.UnassignedSectorCount != WorldGenConstants.SectorCount)
                throw new ArgumentException("Publication ownership conservation is invalid.", nameof(snapshot));

            this.satelliteSeeds = new ReadOnlyCollection<SatelliteSeedPlacementRecord>(records);
            CorePatchCount = coreCount;
            SatellitePatchCount = satelliteCount;
            TotalPatchCount = snapshot.Patches.Count;
            CoreSiteBindingCount = snapshot.SiteBindings.Count;
            AssignedSectorCount = snapshot.AssignedSectorCount;
            UnassignedSectorCount = snapshot.UnassignedSectorCount;
        }

        public CorePatchGrowthPublication SourceGrowth { get; }
        public SiteReservationSnapshot SourceSiteSnapshot { get; }
        public BiomePatchSnapshot Snapshot { get; }
        public IReadOnlyList<SatelliteSeedPlacementRecord> SatelliteSeeds => satelliteSeeds;
        public int CorePatchCount { get; }
        public int SatellitePatchCount { get; }
        public int TotalPatchCount { get; }
        public int CoreSiteBindingCount { get; }
        public int AssignedSectorCount { get; }
        public int UnassignedSectorCount { get; }

        private static int CompareRecords(
            SatelliteSeedPlacementRecord left,
            SatelliteSeedPlacementRecord right)
        {
            var value = string.Compare(left.PatchRuleId, right.PatchRuleId, StringComparison.Ordinal);
            return value != 0 ? value : left.SatelliteOrdinal.CompareTo(right.SatelliteOrdinal);
        }
    }
}
