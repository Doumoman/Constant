using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum VillageCandidateRejectionReason
    {
        EntryOutsideWorld,
        FootprintOverlap,
        ProtectedCoreWitness,
        BlocksExistingEntryApproach,
        EntryApproachOccupied,
        OtherSiteDistanceTooSmall,
        StartDistanceOutsideSelectedBucket
    }

    public enum VillageReservationRejectionReason
    {
        SelectedBucketHasNoViableCandidate
    }

    public sealed class VillageReservationRejection
    {
        public VillageReservationRejection(
            VillageReservationRejectionReason reason,
            int bucketOrdinal,
            int minDistanceInclusive,
            int maxDistanceInclusive,
            int sourceCandidateCount,
            int viableCandidateCount,
            string message)
        {
            if (!Enum.IsDefined(typeof(VillageReservationRejectionReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            if (bucketOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(bucketOrdinal));
            if (minDistanceInclusive < 0 || maxDistanceInclusive < minDistanceInclusive)
                throw new ArgumentOutOfRangeException(nameof(minDistanceInclusive));
            if (sourceCandidateCount < 0) throw new ArgumentOutOfRangeException(nameof(sourceCandidateCount));
            if (viableCandidateCount < 0 || viableCandidateCount > sourceCandidateCount)
                throw new ArgumentOutOfRangeException(nameof(viableCandidateCount));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("A stable non-empty message is required.", nameof(message));

            Reason = reason;
            BucketOrdinal = bucketOrdinal;
            MinDistanceInclusive = minDistanceInclusive;
            MaxDistanceInclusive = maxDistanceInclusive;
            SourceCandidateCount = sourceCandidateCount;
            ViableCandidateCount = viableCandidateCount;
            Message = message;
        }

        public VillageReservationRejectionReason Reason { get; }
        public int BucketOrdinal { get; }
        public int MinDistanceInclusive { get; }
        public int MaxDistanceInclusive { get; }
        public int SourceCandidateCount { get; }
        public int ViableCandidateCount { get; }
        public string Message { get; }
    }
}
