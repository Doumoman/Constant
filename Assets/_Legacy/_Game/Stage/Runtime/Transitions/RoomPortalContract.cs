#if LEGACY_DISABLED
using StarNight.Core.Player;
using StarNight.Stage.Layout;
using StarNight.Stage.Rooms;
using UnityEngine;

namespace StarNight.Stage.Transitions
{
    public static class RoomPortalContract
    {
        public const int SocketWidthCells = 1;
        public const int SocketHeightCells = 1;
        public const int InteriorClearanceCells = 1;
        public const int EntrySafeFloorWidthCells = 2;
        public const int PortalPaddingCells = 2;
        public const float PlayerColliderWidthCells = PlayerGridContract.ColliderWidth;
        public const float PlayerColliderHeightCells = PlayerGridContract.ColliderHeight;

        public static bool IsOneCellSocket(RoomSocketDefinition socket)
        {
            return socket != null && socket.OpeningSizeCells == Vector2Int.one;
        }

        public static bool AreOpposite(CardinalDirection first, CardinalDirection second)
        {
            return (first == CardinalDirection.Left && second == CardinalDirection.Right) ||
                   (first == CardinalDirection.Right && second == CardinalDirection.Left) ||
                   (first == CardinalDirection.Up && second == CardinalDirection.Down) ||
                   (first == CardinalDirection.Down && second == CardinalDirection.Up);
        }
    }
}

#endif
