using System;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class VillageReservationSelection
    {
        internal VillageReservationSelection(
            VillageProfileDefinition profile,
            SpecialMapDefinition specialMap,
            SpecialMapEntrySocketDefinition entryTemplate,
            VillageLayoutDefinition layout,
            VillageDistanceBucket distanceBucket,
            VillageReservationCandidate candidate,
            int bucketRoll,
            int layoutRoll,
            int candidateRoll)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            SpecialMap = specialMap ?? throw new ArgumentNullException(nameof(specialMap));
            EntryTemplate = entryTemplate ?? throw new ArgumentNullException(nameof(entryTemplate));
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            DistanceBucket = distanceBucket ?? throw new ArgumentNullException(nameof(distanceBucket));
            Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
            if (bucketRoll < distanceBucket.RollMinInclusive || bucketRoll > distanceBucket.RollMaxInclusive)
                throw new ArgumentOutOfRangeException(nameof(bucketRoll));
            if (layoutRoll < 0) throw new ArgumentOutOfRangeException(nameof(layoutRoll));
            if (candidateRoll < 0) throw new ArgumentOutOfRangeException(nameof(candidateRoll));
            if (!string.Equals(profile.VillageProfileId, candidate.VillageProfileId, StringComparison.Ordinal) ||
                !string.Equals(specialMap.SpecialMapId, candidate.SpecialMapId, StringComparison.Ordinal) ||
                !string.Equals(layout.VillageLayoutId, candidate.LayoutId, StringComparison.Ordinal) ||
                candidate.BucketOrdinal != distanceBucket.BucketOrdinal)
                throw new ArgumentException("Selection definitions must match the selected candidate.");

            BucketRoll = bucketRoll;
            LayoutRoll = layoutRoll;
            CandidateRoll = candidateRoll;
        }

        public VillageProfileDefinition Profile { get; }
        public SpecialMapDefinition SpecialMap { get; }
        public SpecialMapEntrySocketDefinition EntryTemplate { get; }
        public VillageLayoutDefinition Layout { get; }
        public VillageDistanceBucket DistanceBucket { get; }
        public VillageReservationCandidate Candidate { get; }
        public int BucketRoll { get; }
        public int LayoutRoll { get; }
        public int CandidateRoll { get; }
    }

    public sealed class VillageReservationApproval
    {
        internal VillageReservationApproval(
            CoreCapacityApproval coreCapacityApproval,
            VillageReservationSelection village)
        {
            CoreCapacityApproval = coreCapacityApproval ??
                                   throw new ArgumentNullException(nameof(coreCapacityApproval));
            Village = village ?? throw new ArgumentNullException(nameof(village));
            ExistingSiteCount = coreCapacityApproval.SelectionPlan.SelectedCount;
            CapacityWitnessCount = coreCapacityApproval.CapacitySiteCount;
            TotalSelectedSiteCount = ExistingSiteCount + 1;
        }

        public CoreCapacityApproval CoreCapacityApproval { get; }
        public VillageReservationSelection Village { get; }
        public int ExistingSiteCount { get; }
        public int CapacityWitnessCount { get; }
        public int TotalSelectedSiteCount { get; }

        public bool OccupiesSector(int sectorIndex)
        {
            if (sectorIndex < 0 || sectorIndex >= Domain.WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (Contains(Village.Candidate.OccupiedSectorIndices, sectorIndex)) return true;
            foreach (var placement in CoreCapacityApproval.SelectionPlan.SelectedPlacements)
                foreach (var sector in placement.OccupiedSectors)
                    if (WorldGridIndex.ToIndex(sector) == sectorIndex) return true;
            return false;
        }

        private static bool Contains(System.Collections.Generic.IReadOnlyList<int> values, int value)
        {
            var low = 0;
            var high = values.Count - 1;
            while (low <= high)
            {
                var middle = low + ((high - low) / 2);
                if (values[middle] == value) return true;
                if (values[middle] < value) low = middle + 1;
                else high = middle - 1;
            }
            return false;
        }
    }
}
