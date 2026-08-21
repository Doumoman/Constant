#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Water
{
    /// <summary>
    /// Resolves a stream without frame timing or physics. The first three cells
    /// travel forward; remaining cells flow down, left, then right.
    /// </summary>
    public static class WaterStreamResolver
    {
        public const int ForwardRange = 3;
        public const int AbsoluteMaxCells = 6;

        private static readonly GridPos[] FlowOffsets =
        {
            new GridPos(0, -1),
            new GridPos(-1, 0),
            new GridPos(1, 0)
        };

        public static IReadOnlyList<GridPos> Resolve(
            GridPos origin,
            GridPos rawDirection,
            RectInt roomBounds,
            Func<GridPos, bool> isBlocked)
        {
            return Resolve(
                origin,
                rawDirection,
                roomBounds,
                isBlocked,
                ForwardRange,
                AbsoluteMaxCells);
        }

        public static IReadOnlyList<GridPos> Resolve(
            GridPos origin,
            GridPos rawDirection,
            RectInt roomBounds,
            Func<GridPos, bool> isBlocked,
            int forwardRange,
            int maxCells)
        {
            GridPos direction = NormalizeCardinal(rawDirection);
            if ((direction.X == 0 && direction.Y == 0)
                || roomBounds.width <= 0
                || roomBounds.height <= 0
                || forwardRange <= 0
                || maxCells <= 0)
            {
                return new GridPos[0];
            }

            int cellLimit = Mathf.Clamp(maxCells, 1, AbsoluteMaxCells);
            int lineLimit = Mathf.Max(1, forwardRange);
            List<GridPos> result = new List<GridPos>(cellLimit);
            HashSet<GridPos> visited = new HashSet<GridPos>();

            GridPos cursor = origin;
            for (int index = 0;
                 index < lineLimit && result.Count < cellLimit;
                 index++)
            {
                GridPos next = cursor + direction;
                if (!Contains(roomBounds, next)
                    || (isBlocked != null && isBlocked(next)))
                {
                    break;
                }

                if (visited.Add(next))
                {
                    result.Add(next);
                }

                cursor = next;
            }

            if (result.Count == 0 || result.Count >= cellLimit)
            {
                return result;
            }

            Queue<GridPos> frontier = new Queue<GridPos>();
            frontier.Enqueue(result[result.Count - 1]);
            while (frontier.Count > 0 && result.Count < cellLimit)
            {
                GridPos current = frontier.Dequeue();
                for (int index = 0;
                     index < FlowOffsets.Length && result.Count < cellLimit;
                     index++)
                {
                    GridPos candidate = current + FlowOffsets[index];
                    if (!Contains(roomBounds, candidate)
                        || visited.Contains(candidate)
                        || (isBlocked != null && isBlocked(candidate)))
                    {
                        continue;
                    }

                    visited.Add(candidate);
                    result.Add(candidate);
                    frontier.Enqueue(candidate);
                }
            }

            return result;
        }

        public static GridPos NormalizeCardinal(GridPos rawDirection)
        {
            int absoluteX = Mathf.Abs(rawDirection.X);
            int absoluteY = Mathf.Abs(rawDirection.Y);
            if (absoluteX == 0 && absoluteY == 0)
            {
                return new GridPos(0, 0);
            }

            if (absoluteX >= absoluteY)
            {
                return new GridPos(rawDirection.X < 0 ? -1 : 1, 0);
            }

            return new GridPos(0, rawDirection.Y < 0 ? -1 : 1);
        }

        private static bool Contains(RectInt bounds, GridPos cell)
        {
            return cell.X >= bounds.xMin
                && cell.Y >= bounds.yMin
                && cell.X < bounds.xMax
                && cell.Y < bounds.yMax;
        }
    }
}

#endif
