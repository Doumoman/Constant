#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Stage.Guidance
{
    public readonly struct ExitGuidance
    {
        public ExitGuidance(bool valid, string currentRoomId, string nextRoomId, Vector2Int direction, bool exitInCurrentRoom)
        {
            IsValid = valid;
            CurrentRoomId = currentRoomId;
            NextRoomId = nextRoomId;
            Direction = direction;
            ExitInCurrentRoom = exitInCurrentRoom;
        }

        public bool IsValid { get; }
        public string CurrentRoomId { get; }
        public string NextRoomId { get; }
        public Vector2Int Direction { get; }
        public bool ExitInCurrentRoom { get; }
    }

    public readonly struct StageRouteRoom
    {
        public StageRouteRoom(string roomId, Vector2 center)
        {
            RoomId = roomId;
            Center = center;
        }

        public string RoomId { get; }
        public Vector2 Center { get; }
    }

    public readonly struct StageRouteEdge
    {
        public StageRouteEdge(string from, string to, bool isMainRoute = true)
        {
            From = from;
            To = to;
            IsMainRoute = isMainRoute;
        }

        public string From { get; }
        public string To { get; }
        public bool IsMainRoute { get; }
    }

    public sealed class ExitGuidanceService
    {
        private readonly Dictionary<string, Vector2> centers = new Dictionary<string, Vector2>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> mainRoute = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private string exitRoomId;

        public bool ExitDiscovered { get; private set; }
        public string ExitRoomId => exitRoomId;
        public IEnumerable<string> RoomIds => centers.Keys;

        public void Configure(IEnumerable<StageRouteRoom> rooms, IEnumerable<StageRouteEdge> edges, string exitRoom)
        {
            centers.Clear();
            mainRoute.Clear();
            exitRoomId = exitRoom ?? string.Empty;
            ExitDiscovered = false;

            if (rooms != null)
            {
                foreach (StageRouteRoom room in rooms)
                {
                    if (string.IsNullOrWhiteSpace(room.RoomId))
                    {
                        continue;
                    }

                    centers[room.RoomId] = room.Center;
                    mainRoute[room.RoomId] = new List<string>();
                }
            }

            if (edges == null)
            {
                return;
            }

            foreach (StageRouteEdge edge in edges)
            {
                if (!edge.IsMainRoute || !mainRoute.ContainsKey(edge.From) || !mainRoute.ContainsKey(edge.To))
                {
                    continue;
                }

                mainRoute[edge.From].Add(edge.To);
                mainRoute[edge.To].Add(edge.From);
            }
        }

        public bool MarkExitDiscovered()
        {
            if (ExitDiscovered)
            {
                return false;
            }

            ExitDiscovered = true;
            return true;
        }

        public ExitGuidance GetGuidance(string currentRoomId)
        {
            if (string.IsNullOrWhiteSpace(currentRoomId) ||
                !centers.ContainsKey(currentRoomId) ||
                !centers.ContainsKey(exitRoomId))
            {
                return default;
            }

            if (string.Equals(currentRoomId, exitRoomId, StringComparison.Ordinal))
            {
                return new ExitGuidance(true, currentRoomId, currentRoomId, Vector2Int.zero, true);
            }

            string nextRoom = FindNextRoomOnShortestPath(currentRoomId);
            if (string.IsNullOrEmpty(nextRoom))
            {
                return default;
            }

            Vector2 delta = centers[nextRoom] - centers[currentRoomId];
            Vector2Int direction = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                ? new Vector2Int(delta.x >= 0f ? 1 : -1, 0)
                : new Vector2Int(0, delta.y >= 0f ? 1 : -1);
            return new ExitGuidance(true, currentRoomId, nextRoom, direction, false);
        }

        public bool TryGetRoomCenter(string roomId, out Vector2 center)
        {
            return centers.TryGetValue(roomId ?? string.Empty, out center);
        }

        public IReadOnlyList<string> GetMainRouteNeighbors(string roomId)
        {
            return mainRoute.TryGetValue(roomId ?? string.Empty, out List<string> neighbors)
                ? neighbors
                : Array.Empty<string>();
        }

        private string FindNextRoomOnShortestPath(string start)
        {
            var queue = new Queue<string>();
            var visited = new HashSet<string>(StringComparer.Ordinal) { start };
            var firstStep = new Dictionary<string, string>(StringComparer.Ordinal);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                if (!mainRoute.TryGetValue(current, out List<string> neighbors))
                {
                    continue;
                }

                for (int index = 0; index < neighbors.Count; index++)
                {
                    string neighbor = neighbors[index];
                    if (!visited.Add(neighbor))
                    {
                        continue;
                    }

                    firstStep[neighbor] = current == start ? neighbor : firstStep[current];
                    if (neighbor == exitRoomId)
                    {
                        return firstStep[neighbor];
                    }

                    queue.Enqueue(neighbor);
                }
            }

            return string.Empty;
        }
    }
}

#endif
