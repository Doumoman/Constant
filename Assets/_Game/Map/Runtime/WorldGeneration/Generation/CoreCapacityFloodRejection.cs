using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum CoreCapacityFloodRejectionReason
    {
        BufferOutsideWorld,
        BufferBlockedBySelectedFootprint,
        MandatoryBufferOverlap,
        InsufficientConnectedCapacity,
        InsufficientDisjointCapacity
    }

    public sealed class CoreCapacityFloodRejection
    {
        public CoreCapacityFloodRejection(
            CoreCapacityFloodRejectionReason reason,
            SitePlacementKey key,
            SitePlacementKey otherKey,
            int sectorIndex,
            int requiredCount,
            int availableCount,
            string message)
        {
            if (!Enum.IsDefined(typeof(CoreCapacityFloodRejectionReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            if (!key.IsValid) throw new ArgumentException("A valid site key is required.", nameof(key));
            if (sectorIndex < -1 || sectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (requiredCount < 0) throw new ArgumentOutOfRangeException(nameof(requiredCount));
            if (availableCount < 0) throw new ArgumentOutOfRangeException(nameof(availableCount));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("A stable non-empty message is required.", nameof(message));

            Reason = reason;
            Key = key;
            OtherKey = otherKey;
            SectorIndex = sectorIndex;
            RequiredCount = requiredCount;
            AvailableCount = availableCount;
            Shortfall = Math.Max(0, requiredCount - availableCount);
            Message = message;
        }

        public CoreCapacityFloodRejectionReason Reason { get; }
        public SitePlacementKey Key { get; }
        public SitePlacementKey OtherKey { get; }
        public int SectorIndex { get; }
        public int RequiredCount { get; }
        public int AvailableCount { get; }
        public int Shortfall { get; }
        public string Message { get; }
    }
}
