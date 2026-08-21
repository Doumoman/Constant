#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Generation.P6
{
    public enum P6MacroCellKind
    {
        ExistingCorridor,
        AdjacentToCorridor,
        Empty,
        RoomExterior,
        Forbidden
    }

    public sealed class P6MacroCostProfile
    {
        public static P6MacroCostProfile Default { get; } =
            new P6MacroCostProfile(1, 2, 5, 20);

        public P6MacroCostProfile(
            int existingCorridor,
            int adjacentToCorridor,
            int empty,
            int roomExterior)
        {
            ExistingCorridor = existingCorridor;
            AdjacentToCorridor = adjacentToCorridor;
            Empty = empty;
            RoomExterior = roomExterior;
        }

        public int ExistingCorridor { get; }
        public int AdjacentToCorridor { get; }
        public int Empty { get; }
        public int RoomExterior { get; }
        public int Forbidden => int.MaxValue;

        public int Cost(P6MacroCellKind kind)
        {
            switch (kind)
            {
                case P6MacroCellKind.ExistingCorridor: return ExistingCorridor;
                case P6MacroCellKind.AdjacentToCorridor: return AdjacentToCorridor;
                case P6MacroCellKind.Empty: return Empty;
                case P6MacroCellKind.RoomExterior: return RoomExterior;
                default: return Forbidden;
            }
        }
    }

    public sealed class P6MacroRouteGrid
    {
        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up
        };

        private readonly HashSet<Vector2Int> roomCells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> forbiddenCells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> corridorCells = new HashSet<Vector2Int>();

        public P6MacroRouteGrid(RectInt bounds, P6MacroCostProfile costs = null)
        {
            Bounds = bounds;
            Costs = costs ?? P6MacroCostProfile.Default;
        }

        public RectInt Bounds { get; }
        public P6MacroCostProfile Costs { get; }
        public IReadOnlyCollection<Vector2Int> RoomCells => roomCells;
        public IReadOnlyCollection<Vector2Int> ForbiddenCells => forbiddenCells;
        public IReadOnlyCollection<Vector2Int> CorridorCells => corridorCells;

        public void MarkRoom(RectInt bounds)
        {
            foreach (Vector2Int cell in bounds.allPositionsWithin)
                if (Bounds.Contains(cell)) roomCells.Add(cell);
        }

        public void MarkRoomInterior(RectInt bounds)
        {
            foreach (Vector2Int cell in bounds.allPositionsWithin)
                if (Bounds.Contains(cell)) forbiddenCells.Add(cell);
        }

        public void MarkRoomExterior(RectInt bounds)
        {
            foreach (Vector2Int cell in bounds.allPositionsWithin)
                if (Bounds.Contains(cell) && !forbiddenCells.Contains(cell))
                    roomCells.Add(cell);
        }

        public void MarkForbidden(Vector2Int cell)
        {
            if (Bounds.Contains(cell)) forbiddenCells.Add(cell);
        }

        public void AddCorridor(IEnumerable<Vector2Int> cells)
        {
            if (cells == null) return;
            foreach (Vector2Int cell in cells)
                if (Bounds.Contains(cell)) corridorCells.Add(cell);
        }

        public P6MacroCellKind GetCellKind(Vector2Int cell)
        {
            if (!Bounds.Contains(cell) || forbiddenCells.Contains(cell))
                return P6MacroCellKind.Forbidden;
            if (corridorCells.Contains(cell))
                return P6MacroCellKind.ExistingCorridor;
            for (int index = 0; index < Directions.Length; index++)
                if (corridorCells.Contains(cell + Directions[index]))
                    return P6MacroCellKind.AdjacentToCorridor;
            if (roomCells.Contains(cell))
                return P6MacroCellKind.RoomExterior;
            return P6MacroCellKind.Empty;
        }

        public int GetTraversalCost(Vector2Int cell) => Costs.Cost(GetCellKind(cell));
    }

    public sealed class P6MacroRouteResult
    {
        internal P6MacroRouteResult(
            bool succeeded, IReadOnlyList<Vector2Int> cells, int totalCost)
        {
            Succeeded = succeeded;
            Cells = cells ?? Array.Empty<Vector2Int>();
            TotalCost = totalCost;
        }

        public bool Succeeded { get; }
        public IReadOnlyList<Vector2Int> Cells { get; }
        public int TotalCost { get; }
    }

    public static class P6MacroRouter
    {
        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up
        };

        public static P6MacroRouteResult Route(
            P6MacroRouteGrid grid,
            Vector2Int start,
            Vector2Int goal)
        {
            if (grid == null
                || !grid.Bounds.Contains(start)
                || !grid.Bounds.Contains(goal))
                return new P6MacroRouteResult(false, null, 0);

            var open = new List<Vector2Int> { start };
            var closed = new HashSet<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var costs = new Dictionary<Vector2Int, int> { { start, 0 } };

            while (open.Count > 0)
            {
                int bestIndex = 0;
                for (int index = 1; index < open.Count; index++)
                    if (ComesBefore(open[index], open[bestIndex], costs, goal))
                        bestIndex = index;

                Vector2Int current = open[bestIndex];
                open.RemoveAt(bestIndex);
                if (current == goal)
                {
                    var path = Reconstruct(cameFrom, current);
                    return new P6MacroRouteResult(true, path, costs[current]);
                }

                closed.Add(current);
                for (int direction = 0; direction < Directions.Length; direction++)
                {
                    Vector2Int next = current + Directions[direction];
                    if (!grid.Bounds.Contains(next) || closed.Contains(next))
                        continue;

                    int step = next == goal ? 1 : grid.GetTraversalCost(next);
                    if (step == int.MaxValue) continue;
                    int proposed = costs[current] + step;
                    int old;
                    if (costs.TryGetValue(next, out old) && proposed >= old)
                        continue;

                    cameFrom[next] = current;
                    costs[next] = proposed;
                    if (!open.Contains(next)) open.Add(next);
                }
            }

            return new P6MacroRouteResult(false, null, 0);
        }

        private static bool ComesBefore(
            Vector2Int first,
            Vector2Int second,
            Dictionary<Vector2Int, int> costs,
            Vector2Int goal)
        {
            int firstG = costs[first];
            int secondG = costs[second];
            int firstF = firstG + Manhattan(first, goal);
            int secondF = secondG + Manhattan(second, goal);
            if (firstF != secondF) return firstF < secondF;
            if (firstG != secondG) return firstG < secondG;
            if (first.y != second.y) return first.y < second.y;
            return first.x < second.x;
        }

        private static List<Vector2Int> Reconstruct(
            Dictionary<Vector2Int, Vector2Int> cameFrom,
            Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            Vector2Int previous;
            while (cameFrom.TryGetValue(current, out previous))
            {
                current = previous;
                path.Add(current);
            }
            path.Reverse();
            return path;
        }

        private static int Manhattan(Vector2Int first, Vector2Int second) =>
            Math.Abs(first.x - second.x) + Math.Abs(first.y - second.y);
    }
}

#endif
