#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Stage.Layout;
using UnityEngine;

namespace StarNight.Stage.Rooms
{
    public sealed class StageRoomGraph
    {
        private readonly Dictionary<string, RoomRuntime> rooms = new(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<string>> adjacency = new(StringComparer.Ordinal);
        private readonly Dictionary<string, RoomEdge> edges = new(StringComparer.Ordinal);

        public string StartRoomId { get; private set; } = string.Empty;
        public int RoomCount => rooms.Count;
        public int EdgeCount => edges.Count;

        public void Clear()
        {
            rooms.Clear();
            adjacency.Clear();
            edges.Clear();
            StartRoomId = string.Empty;
        }

        public bool AddRoom(RoomRuntime room, bool isStart = false)
        {
            if (room == null || string.IsNullOrWhiteSpace(room.RoomId) || rooms.ContainsKey(room.RoomId))
            {
                return false;
            }

            rooms.Add(room.RoomId, room);
            adjacency.Add(room.RoomId, new HashSet<string>(StringComparer.Ordinal));
            if (isStart || string.IsNullOrEmpty(StartRoomId))
            {
                StartRoomId = room.RoomId;
            }
            return true;
        }

        public bool ConnectBidirectional(string firstRoomId, string secondRoomId)
        {
            string edgeId = string.CompareOrdinal(firstRoomId, secondRoomId) <= 0
                ? $"{firstRoomId}<->{secondRoomId}"
                : $"{secondRoomId}<->{firstRoomId}";
            return Connect(new RoomEdge
            {
                EdgeId = edgeId,
                FromNodeId = firstRoomId,
                ToNodeId = secondRoomId,
                Bidirectional = true,
                EdgeType = RoomEdgeType.PortalPair,
            });
        }

        public bool Connect(RoomEdge edge)
        {
            if (edge == null || !edge.IsValid || edges.ContainsKey(edge.EdgeId) ||
                !adjacency.TryGetValue(edge.FromNodeId, out HashSet<string> from) ||
                !adjacency.TryGetValue(edge.ToNodeId, out HashSet<string> to))
            {
                return false;
            }
            edges.Add(edge.EdgeId, edge);
            from.Add(edge.ToNodeId);
            if (edge.Bidirectional)
            {
                to.Add(edge.FromNodeId);
            }
            return true;
        }

        public bool TryGetEdge(string edgeId, out RoomEdge edge)
        {
            return edges.TryGetValue(edgeId ?? string.Empty, out edge);
        }

        public bool AreAdjacent(string firstRoomId, string secondRoomId)
        {
            return adjacency.TryGetValue(firstRoomId ?? string.Empty, out HashSet<string> neighbors) &&
                   neighbors.Contains(secondRoomId ?? string.Empty);
        }

        public bool TryGetRoom(string roomId, out RoomRuntime room)
        {
            return rooms.TryGetValue(roomId ?? string.Empty, out room);
        }

        public IReadOnlyCollection<string> GetNeighbors(string roomId)
        {
            return adjacency.TryGetValue(roomId ?? string.Empty, out HashSet<string> neighbors)
                ? neighbors
                : Array.Empty<string>();
        }

        public string GetNextStepToward(string fromRoomId, string targetRoomId)
        {
            if (string.Equals(fromRoomId, targetRoomId, StringComparison.Ordinal))
            {
                return fromRoomId;
            }
            if (!adjacency.ContainsKey(fromRoomId ?? string.Empty) || !adjacency.ContainsKey(targetRoomId ?? string.Empty))
            {
                return string.Empty;
            }

            var queue = new Queue<string>();
            var previous = new Dictionary<string, string>(StringComparer.Ordinal);
            queue.Enqueue(fromRoomId);
            previous[fromRoomId] = string.Empty;
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                foreach (string neighbor in adjacency[current])
                {
                    if (previous.ContainsKey(neighbor))
                    {
                        continue;
                    }
                    previous[neighbor] = current;
                    if (string.Equals(neighbor, targetRoomId, StringComparison.Ordinal))
                    {
                        queue.Clear();
                        break;
                    }
                    queue.Enqueue(neighbor);
                }
            }

            if (!previous.ContainsKey(targetRoomId))
            {
                return string.Empty;
            }

            string step = targetRoomId;
            while (previous.TryGetValue(step, out string parent) &&
                   !string.IsNullOrEmpty(parent) &&
                   !string.Equals(parent, fromRoomId, StringComparison.Ordinal))
            {
                step = parent;
            }
            return AreAdjacent(fromRoomId, step) ? step : string.Empty;
        }

        public Vector2Int GetDirection(string fromRoomId, string toRoomId)
        {
            if (!TryGetRoom(fromRoomId, out RoomRuntime from) || !TryGetRoom(toRoomId, out RoomRuntime to))
            {
                return Vector2Int.zero;
            }

            Vector2 delta = to.WorldBounds.center - from.WorldBounds.center;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                return new Vector2Int(delta.x < 0f ? -1 : 1, 0);
            }
            return new Vector2Int(0, delta.y < 0f ? -1 : 1);
        }
    }
}

#endif
