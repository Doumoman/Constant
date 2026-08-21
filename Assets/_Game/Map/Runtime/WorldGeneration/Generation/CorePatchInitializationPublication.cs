using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class CorePatchInitializationPublication
    {
        private readonly IReadOnlyList<BiomePatchId> corePatchIds;

        internal CorePatchInitializationPublication(
            SiteReservationSnapshot sourceSiteSnapshot,
            BiomePatchSnapshot snapshot,
            IEnumerable<BiomePatchId> corePatchIds)
        {
            SourceSiteSnapshot = sourceSiteSnapshot ?? throw new ArgumentNullException(nameof(sourceSiteSnapshot));
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            if (corePatchIds == null) throw new ArgumentNullException(nameof(corePatchIds));
            if (snapshot.Seed != sourceSiteSnapshot.Seed)
                throw new ArgumentException("Source and output seeds must match.", nameof(snapshot));
            if (snapshot.IsComplete)
                throw new ArgumentException("Core initialization must publish a partial snapshot.", nameof(snapshot));

            var ids = new List<BiomePatchId>(corePatchIds);
            ids.Sort();
            var unique = new HashSet<BiomePatchId>();
            foreach (var id in ids)
                if (!id.IsValid || !unique.Add(id))
                    throw new ArgumentException("Core patch IDs must be valid and unique.", nameof(corePatchIds));

            var seedCount = 0;
            foreach (var patch in snapshot.Patches)
            {
                if (patch.Role != BiomePatchRole.Core || !unique.Contains(patch.Id))
                    throw new ArgumentException("Publication may contain Core patches only.", nameof(snapshot));
                seedCount += patch.Seeds.Count;
            }
            if (ids.Count != snapshot.Patches.Count)
                throw new ArgumentException("Core patch ID count must match the snapshot.", nameof(corePatchIds));
            if (snapshot.SiteBindings.Count != ids.Count)
                throw new ArgumentException("Every Core patch requires one site binding.", nameof(snapshot));
            if (snapshot.AssignedSectorCount != seedCount)
                throw new ArgumentException("Every initialized sector requires one Core seed.", nameof(snapshot));
            if (snapshot.AssignedSectorCount + snapshot.UnassignedSectorCount != WorldGenConstants.SectorCount)
                throw new ArgumentException("Sector ownership counts must cover the world.", nameof(snapshot));

            this.corePatchIds = new ReadOnlyCollection<BiomePatchId>(ids);
            CorePatchCount = ids.Count;
            CoreSeedCount = seedCount;
            CoreSiteBindingCount = snapshot.SiteBindings.Count;
            AssignedSectorCount = snapshot.AssignedSectorCount;
            UnassignedSectorCount = snapshot.UnassignedSectorCount;
        }

        public SiteReservationSnapshot SourceSiteSnapshot { get; }
        public BiomePatchSnapshot Snapshot { get; }
        public IReadOnlyList<BiomePatchId> CorePatchIds => corePatchIds;
        public int CorePatchCount { get; }
        public int CoreSeedCount { get; }
        public int CoreSiteBindingCount { get; }
        public int AssignedSectorCount { get; }
        public int UnassignedSectorCount { get; }
    }
}
