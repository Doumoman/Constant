#if LEGACY_DISABLED
using StarNight.Stage.Rooms;
using StarNight.Stage.Transitions;
using UnityEngine;

namespace StarNight.Stage.Layout
{
    public static class StageLayoutGraphUtility
    {
        public const int PlacementSnapCells = 2;

        public static Vector2Int SnapToPlacementGrid(Vector2Int positionCells)
        {
            return new Vector2Int(Snap(positionCells.x), Snap(positionCells.y));
        }

        public static RectInt GetCellRect(Vector2Int positionCells, Vector2Int sizeCells)
        {
            return new RectInt(positionCells, sizeCells);
        }

        public static bool RoomsOverlap(
            Vector2Int firstPosition,
            Vector2Int firstSize,
            Vector2Int secondPosition,
            Vector2Int secondSize)
        {
            return GetCellRect(firstPosition, firstSize).Overlaps(GetCellRect(secondPosition, secondSize));
        }

        public static bool IsSocketOnBoundary(RoomSocketDefinition socket, Vector2Int roomSize)
        {
            if (socket == null || roomSize.x <= 0 || roomSize.y <= 0)
            {
                return false;
            }

            Vector2Int cell = socket.LocalCell;
            switch (socket.Side)
            {
                case CardinalDirection.Left:
                    return cell.x == 0 && cell.y >= 0 && cell.y < roomSize.y;
                case CardinalDirection.Right:
                    return cell.x == roomSize.x && cell.y >= 0 && cell.y < roomSize.y;
                case CardinalDirection.Up:
                    return cell.y == roomSize.y && cell.x >= 0 && cell.x < roomSize.x;
                case CardinalDirection.Down:
                    return cell.y == 0 && cell.x >= 0 && cell.x < roomSize.x;
                default:
                    return false;
            }
        }

        public static SocketCompatibility GetCompatibility(
            RoomSocketDefinition first,
            RoomSocketDefinition second,
            bool sameRoom = false)
        {
            if (first == null || second == null)
            {
                return SocketCompatibility.MissingSocket;
            }

            if (sameRoom)
            {
                return SocketCompatibility.SameRoom;
            }

            if (!AreOpposite(first.Side, second.Side))
            {
                return SocketCompatibility.DirectionMismatch;
            }

            if (!RoomPortalContract.IsOneCellSocket(first) || !RoomPortalContract.IsOneCellSocket(second))
            {
                return SocketCompatibility.OpeningSizeMismatch;
            }

            if (first.Traversal != second.Traversal)
            {
                return SocketCompatibility.TraversalMismatch;
            }

            if (first.OpeningSizeCells != second.OpeningSizeCells)
            {
                return SocketCompatibility.OpeningSizeMismatch;
            }

            if (first.FloorHeightCell != second.FloorHeightCell)
            {
                return SocketCompatibility.FloorHeightMismatch;
            }

            if (first.SecretOnly != second.SecretOnly)
            {
                return SocketCompatibility.SecretTypeMismatch;
            }

            return SocketCompatibility.Compatible;
        }

        public static bool AreOpposite(CardinalDirection first, CardinalDirection second)
        {
            return (first == CardinalDirection.Left && second == CardinalDirection.Right) ||
                   (first == CardinalDirection.Right && second == CardinalDirection.Left) ||
                   (first == CardinalDirection.Up && second == CardinalDirection.Down) ||
                   (first == CardinalDirection.Down && second == CardinalDirection.Up);
        }

        private static int Snap(int value)
        {
            return Mathf.RoundToInt(value / (float)PlacementSnapCells) * PlacementSnapCells;
        }
    }
}

#endif
