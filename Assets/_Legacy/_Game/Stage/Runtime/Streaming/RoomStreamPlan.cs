#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Stage.Rooms;

namespace StarNight.Stage.Streaming
{
    public sealed class RoomStreamPlan
    {
        private readonly List<string> neighbors;

        public RoomStreamPlan(
            string roomId,
            int stableRoomSeed,
            IEnumerable<string> neighborRoomIds,
            Func<RoomRuntime> factory,
            int estimatedCellCount = 0)
        {
            RoomId = roomId ?? string.Empty;
            StableRoomSeed = stableRoomSeed;
            neighbors = neighborRoomIds != null
                ? new List<string>(neighborRoomIds)
                : new List<string>();
            Factory = factory;
            EstimatedCellCount = Math.Max(0, estimatedCellCount);
        }

        public string RoomId { get; }
        public int StableRoomSeed { get; }
        public IReadOnlyList<string> NeighborRoomIds => neighbors;
        public Func<RoomRuntime> Factory { get; }
        public int EstimatedCellCount { get; }
    }
}

#endif
