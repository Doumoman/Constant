using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class PatchCleanupPublication
    {
        private readonly IReadOnlyList<PatchCleanupMoveRecord> moves;

        internal PatchCleanupPublication(
            IntrusionPlacementResult sourceIntrusion,
            BiomePatchSnapshot snapshot,
            IEnumerable<PatchCleanupMoveRecord> moves)
        {
            SourceIntrusion = sourceIntrusion ?? throw new ArgumentNullException(nameof(sourceIntrusion));
            if (!sourceIntrusion.Succeeded || sourceIntrusion.Publication == null ||
                sourceIntrusion.Diagnostics == null || sourceIntrusion.Publication.Snapshot == null)
                throw new ArgumentException("A completed Intrusion placement is required.", nameof(sourceIntrusion));
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            if (moves == null) throw new ArgumentNullException(nameof(moves));

            var records = new List<PatchCleanupMoveRecord>(moves);
            records.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
            for (var index = 0; index < records.Count; index++)
                if (records[index] == null || records[index].Sequence != index)
                    throw new ArgumentException("Cleanup moves require exact sequence order.", nameof(moves));

            var source = sourceIntrusion.Publication.Snapshot;
            if (snapshot.Seed != source.Seed || snapshot.Patches.Count != source.Patches.Count ||
                snapshot.AssignedSectorCount != source.AssignedSectorCount ||
                snapshot.UnassignedSectorCount != source.UnassignedSectorCount ||
                snapshot.SiteBindings.Count != source.SiteBindings.Count)
                throw new ArgumentException("Cleanup publication must conserve source counts.", nameof(snapshot));
            for (var index = 0; index < snapshot.SiteBindings.Count; index++)
                if (!ReferenceEquals(snapshot.SiteBindings[index], source.SiteBindings[index]))
                    throw new ArgumentException("Site binding objects must be preserved.", nameof(snapshot));

            var core = 0;
            var satellite = 0;
            var intrusion = 0;
            foreach (var patch in snapshot.Patches)
            {
                if (patch.Role == BiomePatchRole.Core) core++;
                else if (patch.Role == BiomePatchRole.Satellite) satellite++;
                else if (patch.Role == BiomePatchRole.Intrusion) intrusion++;
                else throw new ArgumentException("Patch role is undefined.", nameof(snapshot));
            }
            if (core != sourceIntrusion.Publication.CorePatchCount ||
                satellite != sourceIntrusion.Publication.SatellitePatchCount ||
                intrusion != sourceIntrusion.Publication.IntrusionPatchCount)
                throw new ArgumentException("Cleanup cannot change patch role counts.", nameof(snapshot));

            this.moves = new ReadOnlyCollection<PatchCleanupMoveRecord>(records);
            CorePatchCount = core;
            SatellitePatchCount = satellite;
            IntrusionPatchCount = intrusion;
            TotalPatchCount = snapshot.Patches.Count;
            AssignedSectorCount = snapshot.AssignedSectorCount;
            UnassignedSectorCount = snapshot.UnassignedSectorCount;
        }

        public IntrusionPlacementResult SourceIntrusion { get; }
        public BiomePatchSnapshot Snapshot { get; }
        public IReadOnlyList<PatchCleanupMoveRecord> Moves => moves;
        public int CorePatchCount { get; }
        public int SatellitePatchCount { get; }
        public int IntrusionPatchCount { get; }
        public int TotalPatchCount { get; }
        public int AssignedSectorCount { get; }
        public int UnassignedSectorCount { get; }
    }
}
