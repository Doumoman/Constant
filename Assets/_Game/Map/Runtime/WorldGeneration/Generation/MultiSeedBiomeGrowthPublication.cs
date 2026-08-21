using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MultiSeedBiomeGrowthPublication
    {
        private readonly IReadOnlyList<MultiSeedBiomeGrowthRecord> growthRecords;

        internal MultiSeedBiomeGrowthPublication(
            SatelliteSeedPlacementPublication sourcePlacement,
            BiomePatchSnapshot snapshot,
            IEnumerable<MultiSeedBiomeGrowthRecord> growthRecords,
            int initialAssignedSectorCount,
            int finalUnassignedReservedSectorCount)
        {
            SourcePlacement = sourcePlacement ?? throw new ArgumentNullException(nameof(sourcePlacement));
            SourceSiteSnapshot = sourcePlacement.SourceSiteSnapshot ??
                throw new ArgumentException("Source site snapshot is required.", nameof(sourcePlacement));
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            if (growthRecords == null) throw new ArgumentNullException(nameof(growthRecords));
            if (sourcePlacement.Snapshot == null || sourcePlacement.Snapshot.Seed != SourceSiteSnapshot.Seed ||
                snapshot.Seed != SourceSiteSnapshot.Seed)
                throw new ArgumentException("Source and output seeds must match.", nameof(snapshot));

            var records = new List<MultiSeedBiomeGrowthRecord>(growthRecords);
            records.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
            for (var index = 0; index < records.Count; index++)
            {
                var record = records[index];
                if (record == null || record.Sequence != index)
                    throw new ArgumentException("Growth records require exact sequence order.", nameof(growthRecords));
                var ownership = snapshot.GetSector(record.SectorIndex);
                if (!ownership.IsAssigned || !ownership.PatchId.HasValue ||
                    ownership.PatchId.Value != record.PatchId ||
                    !string.Equals(ownership.PrimaryBiomeId, record.BiomeId, StringComparison.Ordinal))
                    throw new ArgumentException("Growth record does not match output ownership.", nameof(growthRecords));
            }
            if (initialAssignedSectorCount != sourcePlacement.AssignedSectorCount ||
                snapshot.AssignedSectorCount != checked(initialAssignedSectorCount + records.Count) ||
                snapshot.UnassignedSectorCount != finalUnassignedReservedSectorCount ||
                snapshot.AssignedSectorCount + finalUnassignedReservedSectorCount != WorldGenConstants.SectorCount)
                throw new ArgumentException("Growth publication conservation is invalid.");

            this.growthRecords = new ReadOnlyCollection<MultiSeedBiomeGrowthRecord>(records);
            PatchCount = snapshot.Patches.Count;
            InitialAssignedSectorCount = initialAssignedSectorCount;
            AddedSectorCount = records.Count;
            FinalAssignedSectorCount = snapshot.AssignedSectorCount;
            FinalUnassignedReservedSectorCount = finalUnassignedReservedSectorCount;
        }

        public SatelliteSeedPlacementPublication SourcePlacement { get; }
        public SiteReservationSnapshot SourceSiteSnapshot { get; }
        public BiomePatchSnapshot Snapshot { get; }
        public IReadOnlyList<MultiSeedBiomeGrowthRecord> GrowthRecords => growthRecords;
        public int PatchCount { get; }
        public int InitialAssignedSectorCount { get; }
        public int AddedSectorCount { get; }
        public int FinalAssignedSectorCount { get; }
        public int FinalUnassignedReservedSectorCount { get; }
    }
}
