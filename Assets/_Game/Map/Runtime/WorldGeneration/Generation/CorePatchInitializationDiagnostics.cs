using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class CorePatchInitializationDiagnostics
    {
        private readonly IReadOnlyList<SiteReservationId> sourceReservationIds;
        private readonly IReadOnlyList<BiomePatchId> corePatchIds;

        internal CorePatchInitializationDiagnostics(
            ulong worldSeed,
            int sourceReservationCount,
            int inputCoreSeedCount,
            int corePatchCount,
            int coreSeedCellCount,
            int coreSiteBindingCount,
            int assignedSectorCount,
            int unassignedSectorCount,
            IEnumerable<SiteReservationId> sourceReservationIds,
            IEnumerable<BiomePatchId> corePatchIds)
        {
            if (sourceReservationCount < 0) throw new ArgumentOutOfRangeException(nameof(sourceReservationCount));
            if (inputCoreSeedCount < 0) throw new ArgumentOutOfRangeException(nameof(inputCoreSeedCount));
            if (corePatchCount < 0) throw new ArgumentOutOfRangeException(nameof(corePatchCount));
            if (coreSeedCellCount < 0) throw new ArgumentOutOfRangeException(nameof(coreSeedCellCount));
            if (coreSiteBindingCount < 0) throw new ArgumentOutOfRangeException(nameof(coreSiteBindingCount));
            if (assignedSectorCount < 0) throw new ArgumentOutOfRangeException(nameof(assignedSectorCount));
            if (unassignedSectorCount < 0) throw new ArgumentOutOfRangeException(nameof(unassignedSectorCount));
            if (sourceReservationIds == null) throw new ArgumentNullException(nameof(sourceReservationIds));
            if (corePatchIds == null) throw new ArgumentNullException(nameof(corePatchIds));

            var sources = new List<SiteReservationId>(sourceReservationIds);
            var patches = new List<BiomePatchId>(corePatchIds);
            sources.Sort();
            patches.Sort();
            if (sources.Count != sourceReservationCount || patches.Count != corePatchCount)
                throw new ArgumentException("Diagnostic ID counts must match their summaries.");

            WorldSeed = worldSeed;
            SourceReservationCount = sourceReservationCount;
            InputCoreSeedCount = inputCoreSeedCount;
            CorePatchCount = corePatchCount;
            CoreSeedCellCount = coreSeedCellCount;
            CoreSiteBindingCount = coreSiteBindingCount;
            AssignedSectorCount = assignedSectorCount;
            UnassignedSectorCount = unassignedSectorCount;
            RngDrawCount = 0;
            this.sourceReservationIds = new ReadOnlyCollection<SiteReservationId>(sources);
            this.corePatchIds = new ReadOnlyCollection<BiomePatchId>(patches);
        }

        public ulong WorldSeed { get; }
        public int SourceReservationCount { get; }
        public int InputCoreSeedCount { get; }
        public int CorePatchCount { get; }
        public int CoreSeedCellCount { get; }
        public int CoreSiteBindingCount { get; }
        public int AssignedSectorCount { get; }
        public int UnassignedSectorCount { get; }
        public int RngDrawCount { get; }
        public IReadOnlyList<SiteReservationId> SourceReservationIds => sourceReservationIds;
        public IReadOnlyList<BiomePatchId> CorePatchIds => corePatchIds;
    }
}
