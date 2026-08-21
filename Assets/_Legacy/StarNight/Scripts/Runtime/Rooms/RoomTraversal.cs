#if LEGACY_DISABLED
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Rooms
{
    public sealed class RoomTraversalSnapshot
    {
        private readonly HashSet<Vector2Int> terrain = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> oneWay = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> hazards = new HashSet<Vector2Int>();

        public RoomTraversalSnapshot(Vector2Int size)
        {
            Size = size;
            Bounds = new RectInt(Vector2Int.zero, size);
        }

        public Vector2Int Size { get; }
        public RectInt Bounds { get; }

        public static RoomTraversalSnapshot Capture(RoomTemplate2D template)
        {
            if (template == null)
            {
                return null;
            }

            RoomTraversalSnapshot snapshot =
                new RoomTraversalSnapshot(template.LogicalSize);
            for (int x = 0; x < template.LogicalSize.x; x++)
            {
                for (int y = 0; y < template.LogicalSize.y; y++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    Vector2Int logicalCell = new Vector2Int(x, y);
                    if (template.Terrain != null && template.Terrain.HasTile(cell))
                    {
                        snapshot.terrain.Add(logicalCell);
                    }

                    if (template.OneWay != null && template.OneWay.HasTile(cell))
                    {
                        snapshot.oneWay.Add(logicalCell);
                    }

                    if (template.Hazard != null && template.Hazard.HasTile(cell))
                    {
                        snapshot.hazards.Add(logicalCell);
                    }
                }
            }

            return snapshot;
        }

        public void AddTerrain(Vector2Int cell)
        {
            if (Bounds.Contains(cell))
            {
                terrain.Add(cell);
            }
        }

        public void AddOneWay(Vector2Int cell)
        {
            if (Bounds.Contains(cell))
            {
                oneWay.Add(cell);
            }
        }

        public void AddHazard(Vector2Int cell)
        {
            if (Bounds.Contains(cell))
            {
                hazards.Add(cell);
            }
        }

        public bool IsTerrain(Vector2Int cell)
        {
            return terrain.Contains(cell);
        }

        public bool IsSupport(Vector2Int cell)
        {
            return terrain.Contains(cell) || oneWay.Contains(cell);
        }

        public bool IsOneWay(Vector2Int cell)
        {
            return oneWay.Contains(cell);
        }

        public bool IsHazard(Vector2Int cell)
        {
            return hazards.Contains(cell);
        }

        public bool IsStandable(Vector2Int cell)
        {
            return Bounds.Contains(cell)
                && IsBodyClearAt(new Vector2(cell.x + 0.5f, cell.y + 0.45f), false)
                && IsSupport(cell + Vector2Int.down);
        }

        public bool IsBodyClearAt(Vector2 center, bool descending)
        {
            const float HalfWidth = 0.36f;
            const float HalfHeight = 0.45f;
            const float Epsilon = 0.002f;
            int minX = Mathf.FloorToInt(center.x - HalfWidth + Epsilon);
            int maxX = Mathf.FloorToInt(center.x + HalfWidth - Epsilon);
            int minY = Mathf.FloorToInt(center.y - HalfHeight + Epsilon);
            int maxY = Mathf.FloorToInt(center.y + HalfHeight - Epsilon);
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!Bounds.Contains(cell)
                        || IsTerrain(cell)
                        || IsHazard(cell)
                        || (descending && IsOneWay(cell)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public int HazardCellCount => hazards.Count;

        public int CountStandableLayers()
        {
            int layers = 0;
            for (int y = Bounds.yMin; y < Bounds.yMax; y++)
            {
                bool anyStandable = false;
                for (int x = Bounds.xMin; x < Bounds.xMax; x++)
                {
                    if (IsStandable(new Vector2Int(x, y)))
                    {
                        anyStandable = true;
                        break;
                    }
                }

                if (anyStandable)
                {
                    layers++;
                }
            }

            return layers;
        }
    }

    public static class RoomTraversalSolver
    {
        private const int MaximumJumpDistance = 3;
        private const int MaximumJumpRise = 2;
        private const int MaximumDropDistance = 12;
        private const float MaximumRunSpeed = 3.75f;
        private const float Gravity = 24f;
        private const float MaximumFallSpeed = 18f;
        private const float JumpSpeed = 10.276186f;

        public static bool CanReach(
            RoomTraversalSnapshot snapshot,
            Vector2Int start,
            Vector2Int destination)
        {
            return TryFindPath(snapshot, start, destination, out _);
        }

        public static bool TryFindPath(
            RoomTraversalSnapshot snapshot,
            Vector2Int start,
            Vector2Int destination,
            out IReadOnlyList<Vector2Int> path)
        {
            path = System.Array.Empty<Vector2Int>();
            if (snapshot == null
                || !snapshot.IsStandable(start)
                || !snapshot.IsStandable(destination))
            {
                return false;
            }

            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            Dictionary<Vector2Int, Vector2Int> cameFrom =
                new Dictionary<Vector2Int, Vector2Int>();
            frontier.Enqueue(start);
            cameFrom.Add(start, start);

            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();
                if (current == destination)
                {
                    path = Reconstruct(cameFrom, start, destination);
                    return true;
                }

                for (int deltaX = -MaximumJumpDistance;
                     deltaX <= MaximumJumpDistance;
                     deltaX++)
                {
                    for (int deltaY = -MaximumDropDistance;
                         deltaY <= MaximumJumpRise;
                         deltaY++)
                    {
                        if (deltaX == 0 && deltaY == 0)
                        {
                            continue;
                        }

                        Vector2Int candidate =
                            current + new Vector2Int(deltaX, deltaY);
                        if (cameFrom.ContainsKey(candidate)
                            || !snapshot.IsStandable(candidate)
                            || !CanTraverse(snapshot, current, candidate))
                        {
                            continue;
                        }

                        cameFrom.Add(candidate, current);
                        frontier.Enqueue(candidate);
                    }
                }
            }

            return false;
        }

        private static bool CanTraverse(
            RoomTraversalSnapshot snapshot,
            Vector2Int from,
            Vector2Int to)
        {
            int deltaX = to.x - from.x;
            int deltaY = to.y - from.y;
            int horizontalDistance = Mathf.Abs(deltaX);

            if (horizontalDistance == 1 && deltaY == 0)
            {
                return HasClearLine(snapshot, from, to);
            }

            if (deltaY < 0
                && horizontalDistance <= 1
                && -deltaY <= MaximumDropDistance)
            {
                return HasClearFall(snapshot, from, to);
            }

            return horizontalDistance <= MaximumJumpDistance
                && deltaY >= -2
                && deltaY <= MaximumJumpRise
                && HasClearJumpArc(snapshot, from, to);
        }

        private static bool HasClearLine(
            RoomTraversalSnapshot snapshot,
            Vector2Int from,
            Vector2Int to)
        {
            const int Steps = 4;
            for (int step = 1; step <= Steps; step++)
            {
                float t = step / (float)Steps;
                Vector2 center = new Vector2(
                    Mathf.Lerp(from.x + 0.5f, to.x + 0.5f, t),
                    Mathf.Lerp(from.y + 0.45f, to.y + 0.45f, t));
                if (!snapshot.IsBodyClearAt(center, false))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasClearFall(
            RoomTraversalSnapshot snapshot,
            Vector2Int from,
            Vector2Int to)
        {
            if (from.x != to.x)
            {
                const int LedgeSteps = 6;
                for (int step = 1; step <= LedgeSteps; step++)
                {
                    float ledgeT = step / (float)LedgeSteps;
                    Vector2 ledgeCenter = new Vector2(
                        Mathf.Lerp(
                            from.x + 0.5f,
                            to.x + 0.5f,
                            ledgeT),
                        from.y + 0.45f);
                    if (!snapshot.IsBodyClearAt(ledgeCenter, false))
                    {
                        return false;
                    }
                }
            }

            float fallDistance = from.y - to.y;
            float terminalTime = MaximumFallSpeed / Gravity;
            float terminalDistance =
                0.5f * Gravity * terminalTime * terminalTime;
            float fallTime = fallDistance <= terminalDistance
                ? Mathf.Sqrt(2f * fallDistance / Gravity)
                : terminalTime
                    + (fallDistance - terminalDistance) / MaximumFallSpeed;
            int steps = Mathf.Max(4, Mathf.CeilToInt(fallTime * 60f));
            for (int step = 1; step < steps; step++)
            {
                float time = fallTime * step / steps;
                float normalized = step / (float)steps;
                float fallVelocity = Mathf.Min(Gravity * time, MaximumFallSpeed);
                float verticalTravel = fallVelocity >= MaximumFallSpeed
                    ? (MaximumFallSpeed * MaximumFallSpeed) / (2f * Gravity)
                        + MaximumFallSpeed
                        * (time - MaximumFallSpeed / Gravity)
                    : 0.5f * Gravity * time * time;
                Vector2 center = new Vector2(
                    to.x + 0.5f,
                    from.y + 0.45f - verticalTravel);
                if (!snapshot.IsBodyClearAt(center, true))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasClearJumpArc(
            RoomTraversalSnapshot snapshot,
            Vector2Int from,
            Vector2Int to)
        {
            float deltaY = to.y - from.y;
            float discriminant = JumpSpeed * JumpSpeed - 2f * Gravity * deltaY;
            if (discriminant < 0f)
            {
                return false;
            }

            float flightTime =
                (JumpSpeed + Mathf.Sqrt(discriminant)) / Gravity;
            float horizontalVelocity =
                Mathf.Abs(to.x - from.x) / flightTime;
            if (horizontalVelocity > MaximumRunSpeed + 0.001f)
            {
                return false;
            }

            int steps = Mathf.Max(8, Mathf.CeilToInt(flightTime * 60f));
            for (int step = 1; step < steps; step++)
            {
                float time = flightTime * step / steps;
                float normalized = step / (float)steps;
                float verticalVelocity = JumpSpeed - Gravity * time;
                Vector2 center = new Vector2(
                    Mathf.Lerp(from.x + 0.5f, to.x + 0.5f, normalized),
                    from.y + 0.45f
                        + JumpSpeed * time
                        - 0.5f * Gravity * time * time);
                if (!snapshot.IsBodyClearAt(center, verticalVelocity <= 0f))
                {
                    return false;
                }
            }

            return true;
        }

        private static IReadOnlyList<Vector2Int> Reconstruct(
            IReadOnlyDictionary<Vector2Int, Vector2Int> cameFrom,
            Vector2Int start,
            Vector2Int destination)
        {
            List<Vector2Int> reversed = new List<Vector2Int> { destination };
            Vector2Int current = destination;
            while (current != start)
            {
                current = cameFrom[current];
                reversed.Add(current);
            }

            reversed.Reverse();
            return reversed;
        }
    }
}

#endif
