using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class VillageLayoutCandidateDiagnostics
    {
        internal VillageLayoutCandidateDiagnostics(
            string layoutId,
            int selectionWeight,
            int rawEntryEvaluationCount,
            int entryOutsideWorldCount,
            int sourceCandidateCount,
            int footprintOverlapCount,
            int protectedCoreWitnessCount,
            int blocksExistingEntryApproachCount,
            int entryApproachOccupiedCount,
            int otherSiteDistanceTooSmallCount,
            int startDistanceOutsideSelectedBucketCount,
            int viableCandidateCount)
        {
            if (!SitePlacementKey.IsCanonicalId(layoutId))
                throw new ArgumentException("A canonical layout ID is required.", nameof(layoutId));
            if (selectionWeight <= 0) throw new ArgumentOutOfRangeException(nameof(selectionWeight));
            var counts = new[]
            {
                rawEntryEvaluationCount, entryOutsideWorldCount, sourceCandidateCount,
                footprintOverlapCount, protectedCoreWitnessCount,
                blocksExistingEntryApproachCount, entryApproachOccupiedCount,
                otherSiteDistanceTooSmallCount, startDistanceOutsideSelectedBucketCount,
                viableCandidateCount
            };
            foreach (var count in counts)
                if (count < 0) throw new ArgumentOutOfRangeException(nameof(counts));
            if (entryOutsideWorldCount + sourceCandidateCount != rawEntryEvaluationCount ||
                footprintOverlapCount + protectedCoreWitnessCount +
                blocksExistingEntryApproachCount + entryApproachOccupiedCount +
                otherSiteDistanceTooSmallCount + startDistanceOutsideSelectedBucketCount +
                viableCandidateCount != sourceCandidateCount)
                throw new ArgumentException("Layout candidate diagnostics must conserve every evaluation.");

            LayoutId = layoutId;
            SelectionWeight = selectionWeight;
            RawEntryEvaluationCount = rawEntryEvaluationCount;
            EntryOutsideWorldCount = entryOutsideWorldCount;
            SourceCandidateCount = sourceCandidateCount;
            FootprintOverlapCount = footprintOverlapCount;
            ProtectedCoreWitnessCount = protectedCoreWitnessCount;
            BlocksExistingEntryApproachCount = blocksExistingEntryApproachCount;
            EntryApproachOccupiedCount = entryApproachOccupiedCount;
            OtherSiteDistanceTooSmallCount = otherSiteDistanceTooSmallCount;
            StartDistanceOutsideSelectedBucketCount = startDistanceOutsideSelectedBucketCount;
            ViableCandidateCount = viableCandidateCount;
        }

        public string LayoutId { get; }
        public int SelectionWeight { get; }
        public int RawEntryEvaluationCount { get; }
        public int EntryOutsideWorldCount { get; }
        public int SourceCandidateCount { get; }
        public int FootprintOverlapCount { get; }
        public int ProtectedCoreWitnessCount { get; }
        public int BlocksExistingEntryApproachCount { get; }
        public int EntryApproachOccupiedCount { get; }
        public int OtherSiteDistanceTooSmallCount { get; }
        public int StartDistanceOutsideSelectedBucketCount { get; }
        public int ViableCandidateCount { get; }
    }

    public sealed class VillageReservationDiagnostics
    {
        private readonly IReadOnlyList<VillageLayoutCandidateDiagnostics> layouts;

        internal VillageReservationDiagnostics(
            VillageDistanceBucket selectedBucket,
            int bucketRoll,
            IEnumerable<VillageLayoutCandidateDiagnostics> layouts,
            int rngMethodCallCount,
            ulong rngDrawCountBefore,
            ulong rngDrawCountAfter,
            int layoutRoll,
            int candidateRoll)
        {
            SelectedBucket = selectedBucket ?? throw new ArgumentNullException(nameof(selectedBucket));
            if (bucketRoll < selectedBucket.RollMinInclusive || bucketRoll > selectedBucket.RollMaxInclusive)
                throw new ArgumentOutOfRangeException(nameof(bucketRoll));
            if (layouts == null) throw new ArgumentNullException(nameof(layouts));
            var snapshot = new List<VillageLayoutCandidateDiagnostics>(layouts);
            if (snapshot.Count == 0 || snapshot.Exists(item => item == null))
                throw new ArgumentException("At least one layout diagnostic is required.", nameof(layouts));
            snapshot.Sort((left, right) => string.Compare(left.LayoutId, right.LayoutId, StringComparison.Ordinal));
            for (var index = 1; index < snapshot.Count; index++)
                if (string.Equals(snapshot[index - 1].LayoutId, snapshot[index].LayoutId, StringComparison.Ordinal))
                    throw new ArgumentException("Layout diagnostics must have unique IDs.", nameof(layouts));
            if (rngMethodCallCount != 1 && rngMethodCallCount != 3)
                throw new ArgumentOutOfRangeException(nameof(rngMethodCallCount));
            if (rngDrawCountAfter < rngDrawCountBefore)
                throw new ArgumentOutOfRangeException(nameof(rngDrawCountAfter));
            if (rngMethodCallCount == 1 && (layoutRoll != -1 || candidateRoll != -1))
                throw new ArgumentException("A rejected bucket cannot publish later rolls.");
            if (rngMethodCallCount == 3 && (layoutRoll < 0 || candidateRoll < 0))
                throw new ArgumentException("A completed reservation requires all rolls.");

            var raw = 0;
            var source = 0;
            var viableLayouts = 0;
            var viableCandidates = 0;
            foreach (var layout in snapshot)
            {
                checked
                {
                    raw += layout.RawEntryEvaluationCount;
                    source += layout.SourceCandidateCount;
                    viableCandidates += layout.ViableCandidateCount;
                }
                if (layout.ViableCandidateCount > 0) viableLayouts++;
            }

            this.layouts = new ReadOnlyCollection<VillageLayoutCandidateDiagnostics>(snapshot);
            BucketRoll = bucketRoll;
            RawEntryEvaluationCount = raw;
            SourceCandidateCount = source;
            ViableLayoutCount = viableLayouts;
            ViableCandidateCount = viableCandidates;
            RngMethodCallCount = rngMethodCallCount;
            RngDrawCountBefore = rngDrawCountBefore;
            RngDrawCountAfter = rngDrawCountAfter;
            LayoutRoll = layoutRoll;
            CandidateRoll = candidateRoll;
        }

        public VillageDistanceBucket SelectedBucket { get; }
        public int BucketRoll { get; }
        public IReadOnlyList<VillageLayoutCandidateDiagnostics> Layouts => layouts;
        public int RawEntryEvaluationCount { get; }
        public int SourceCandidateCount { get; }
        public int ViableLayoutCount { get; }
        public int ViableCandidateCount { get; }
        public int RngMethodCallCount { get; }
        public ulong RngDrawCountBefore { get; }
        public ulong RngDrawCountAfter { get; }
        public int LayoutRoll { get; }
        public int CandidateRoll { get; }
    }
}
