using System;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkObjectSlotValidationViolation
    {
        public MicrochunkId MicrochunkId { get; }
        public string SlotId { get; }
        public MicrochunkSlotCategory Category { get; }
        public string AllowedPoolId { get; }
        public MicrochunkLocalCoord? Coordinate { get; }
        public bool HasCoordinate => Coordinate.HasValue;
        public string ComparedSlotId { get; }
        public string Reason { get; }

        public MicrochunkObjectSlotValidationViolation(
            MicrochunkId microchunkId,
            string slotId,
            MicrochunkSlotCategory category,
            string allowedPoolId,
            MicrochunkLocalCoord? coordinate,
            string comparedSlotId,
            string reason)
        {
            if (!microchunkId.IsValid) throw new ArgumentException("A valid microchunk ID is required.", nameof(microchunkId));
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Violation reason is required.", nameof(reason));

            MicrochunkId = microchunkId;
            SlotId = slotId ?? string.Empty;
            Category = category;
            AllowedPoolId = allowedPoolId ?? string.Empty;
            Coordinate = coordinate;
            ComparedSlotId = comparedSlotId ?? string.Empty;
            Reason = reason;
        }
    }
}
