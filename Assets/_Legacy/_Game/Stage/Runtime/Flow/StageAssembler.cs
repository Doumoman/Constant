#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Stage.Lab;
using StarNight.Stage.Rooms;

namespace StarNight.Stage.Flow
{
    public readonly struct StageAssemblyResult
    {
        public StageAssemblyResult(RoomRuntime startRoom, RoomRuntime exitRoom, IReadOnlyList<RoomRuntime> rooms)
        {
            StartRoom = startRoom;
            ExitRoom = exitRoom;
            Rooms = rooms;
        }

        public RoomRuntime StartRoom { get; }
        public RoomRuntime ExitRoom { get; }
        public IReadOnlyList<RoomRuntime> Rooms { get; }
        public bool IsValid => StartRoom != null && ExitRoom != null && Rooms != null && Rooms.Count > 0;
    }

    public sealed class StageAssembler
    {
        public StageAssemblyResult Assemble(Core04TwoRoomLab lab)
        {
            if (lab == null)
            {
                throw new ArgumentNullException(nameof(lab));
            }

            lab.BuildIfNeeded();
            return new StageAssemblyResult(lab.RoomA, lab.ExitRoom, lab.Rooms);
        }

        public StageAssemblyResult AssembleFixedTwoRoom(Core04TwoRoomLab lab) => Assemble(lab);
    }
}

#endif
