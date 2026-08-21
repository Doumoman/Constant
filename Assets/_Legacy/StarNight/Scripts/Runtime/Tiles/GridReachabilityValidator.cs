#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tiles
{
    public static class GridReachabilityValidator
    {
        private static readonly GridPos[] WalkAndStepOffsets =
        {
            new GridPos(1, 0),
            new GridPos(-1, 0),
            new GridPos(1, 1),
            new GridPos(-1, 1),
            new GridPos(1, -1),
            new GridPos(-1, -1)
        };

        public static bool CanReach(
            RectInt bounds,
            GridPos start,
            GridPos exit,
            Func<GridPos, bool> isSolid)
        {
            if (isSolid == null
                || !IsStandable(bounds, start, isSolid)
                || !IsStandable(bounds, exit, isSolid))
            {
                return false;
            }

            Queue<GridPos> frontier = new Queue<GridPos>();
            HashSet<GridPos> visited = new HashSet<GridPos>();
            frontier.Enqueue(start);
            visited.Add(start);

            while (frontier.Count > 0)
            {
                GridPos current = frontier.Dequeue();
                if (current == exit)
                {
                    return true;
                }

                for (int index = 0; index < WalkAndStepOffsets.Length; index++)
                {
                    TryEnqueue(
                        current,
                        current + WalkAndStepOffsets[index],
                        bounds,
                        isSolid,
                        visited,
                        frontier);
                }

                for (int deltaX = -3; deltaX <= 3; deltaX++)
                {
                    if (deltaX == 0)
                    {
                        continue;
                    }

                    for (int deltaY = -2; deltaY <= 2; deltaY++)
                    {
                        if (Mathf.Abs(deltaX) <= 1 && Mathf.Abs(deltaY) <= 1)
                        {
                            continue;
                        }

                        GridPos landing = new GridPos(
                            current.X + deltaX,
                            current.Y + deltaY);
                        if (!HasClearJumpArc(current, landing, bounds, isSolid))
                        {
                            continue;
                        }

                        TryEnqueue(
                            current,
                            landing,
                            bounds,
                            isSolid,
                            visited,
                            frontier);
                    }
                }
            }

            return false;
        }

        public static bool IsStandable(
            RectInt bounds,
            GridPos cell,
            Func<GridPos, bool> isSolid)
        {
            return Contains(bounds, cell)
                && !isSolid(cell)
                && isSolid(new GridPos(cell.X, cell.Y - 1));
        }

        private static void TryEnqueue(
            GridPos from,
            GridPos candidate,
            RectInt bounds,
            Func<GridPos, bool> isSolid,
            HashSet<GridPos> visited,
            Queue<GridPos> frontier)
        {
            if (visited.Contains(candidate)
                || !IsStandable(bounds, candidate, isSolid)
                || !HasClearBodyPath(from, candidate, bounds, isSolid))
            {
                return;
            }

            visited.Add(candidate);
            frontier.Enqueue(candidate);
        }

        private static bool HasClearBodyPath(
            GridPos from,
            GridPos to,
            RectInt bounds,
            Func<GridPos, bool> isSolid)
        {
            int steps = Mathf.Max(Mathf.Abs(to.X - from.X), Mathf.Abs(to.Y - from.Y));
            for (int step = 1; step <= steps; step++)
            {
                float t = step / (float)steps;
                GridPos sample = new GridPos(
                    Mathf.RoundToInt(Mathf.Lerp(from.X, to.X, t)),
                    Mathf.RoundToInt(Mathf.Lerp(from.Y, to.Y, t)));
                if (!Contains(bounds, sample) || isSolid(sample))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasClearJumpArc(
            GridPos from,
            GridPos to,
            RectInt bounds,
            Func<GridPos, bool> isSolid)
        {
            int steps = Mathf.Max(2, Mathf.Abs(to.X - from.X) * 2);
            float arcHeight = Mathf.Max(1f, Mathf.Abs(to.X - from.X) * 0.45f);
            for (int step = 1; step < steps; step++)
            {
                float t = step / (float)steps;
                float arc = 4f * arcHeight * t * (1f - t);
                GridPos sample = new GridPos(
                    Mathf.RoundToInt(Mathf.Lerp(from.X, to.X, t)),
                    Mathf.FloorToInt(Mathf.Lerp(from.Y, to.Y, t) + arc));
                if (!Contains(bounds, sample) || isSolid(sample))
                {
                    return false;
                }
            }

            return true;
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
