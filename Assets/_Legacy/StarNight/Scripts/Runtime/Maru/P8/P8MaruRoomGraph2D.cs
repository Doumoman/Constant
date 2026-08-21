#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Maru.P8
{
    [Serializable]
    public struct P8MaruRoomNode
    {
        [SerializeField] private int nodeId;
        [SerializeField] private Rect worldBounds;
        [SerializeField] private Vector2 center;
        [SerializeField] private int[] neighbourNodeIds;

        public P8MaruRoomNode(
            int id,
            Rect bounds,
            Vector2 roomCenter,
            int[] neighbours)
        {
            nodeId = id;
            worldBounds = bounds;
            center = roomCenter;
            neighbourNodeIds = neighbours ?? Array.Empty<int>();
        }

        public int NodeId => nodeId;
        public Rect WorldBounds => worldBounds;
        public Vector2 Center => center;
        public IReadOnlyList<int> NeighbourNodeIds =>
            neighbourNodeIds ?? Array.Empty<int>();
    }

    [DisallowMultipleComponent]
    public sealed class P8MaruRoomGraph2D : MonoBehaviour
    {
        [SerializeField] private P8MaruRoomNode[] nodes =
            Array.Empty<P8MaruRoomNode>();

        private readonly Dictionary<int, int> indexByNodeId =
            new Dictionary<int, int>();

        public IReadOnlyList<P8MaruRoomNode> Nodes => nodes;
        public int NodeCount => nodes?.Length ?? 0;

        public void Configure(P8MaruRoomNode[] roomNodes)
        {
            nodes = roomNodes ?? Array.Empty<P8MaruRoomNode>();
            RebuildLookup();
            Validate();
        }

        private void Awake()
        {
            RebuildLookup();
        }

        public int FindNodeId(Vector2 worldPosition)
        {
            if (nodes == null || nodes.Length == 0)
            {
                return -1;
            }

            int nearestId = nodes[0].NodeId;
            float nearestDistance =
                (nodes[0].Center - worldPosition).sqrMagnitude;
            for (int index = 0; index < nodes.Length; index++)
            {
                if (nodes[index].WorldBounds.Contains(worldPosition))
                {
                    return nodes[index].NodeId;
                }

                float distance =
                    (nodes[index].Center - worldPosition).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestId = nodes[index].NodeId;
                }
            }

            return nearestId;
        }

        public Vector2 NextWaypoint(
            Vector2 maruPosition,
            Vector2 targetPosition)
        {
            EnsureLookup();
            int origin = FindNodeId(maruPosition);
            int destination = FindNodeId(targetPosition);
            if (origin < 0
                || destination < 0
                || origin == destination)
            {
                return targetPosition;
            }

            int next = FindNextNode(origin, destination);
            return indexByNodeId.TryGetValue(next, out int nextIndex)
                ? nodes[nextIndex].Center
                : targetPosition;
        }

        public int Distance(int originNodeId, int destinationNodeId)
        {
            EnsureLookup();
            if (originNodeId == destinationNodeId)
            {
                return 0;
            }

            var queue = new Queue<(int NodeId, int Distance)>();
            var visited = new HashSet<int> { originNodeId };
            queue.Enqueue((originNodeId, 0));
            while (queue.Count > 0)
            {
                (int current, int distance) = queue.Dequeue();
                if (!indexByNodeId.TryGetValue(
                        current,
                        out int currentIndex))
                {
                    continue;
                }

                IReadOnlyList<int> neighbours =
                    nodes[currentIndex].NeighbourNodeIds;
                for (int index = 0; index < neighbours.Count; index++)
                {
                    int next = neighbours[index];
                    if (!visited.Add(next))
                    {
                        continue;
                    }

                    if (next == destinationNodeId)
                    {
                        return distance + 1;
                    }

                    queue.Enqueue((next, distance + 1));
                }
            }

            return -1;
        }

        public void Validate()
        {
            if (nodes == null || nodes.Length == 0)
            {
                throw new InvalidOperationException(
                    "Maru room graph requires at least one room.");
            }

            var ids = new HashSet<int>();
            for (int index = 0; index < nodes.Length; index++)
            {
                P8MaruRoomNode node = nodes[index];
                if (!ids.Add(node.NodeId))
                {
                    throw new InvalidOperationException(
                        $"Duplicate Maru room node {node.NodeId}.");
                }
            }

            for (int index = 0; index < nodes.Length; index++)
            {
                IReadOnlyList<int> neighbours =
                    nodes[index].NeighbourNodeIds;
                for (int other = 0; other < neighbours.Count; other++)
                {
                    if (!ids.Contains(neighbours[other]))
                    {
                        throw new InvalidOperationException(
                            $"Maru room {nodes[index].NodeId} references "
                            + $"missing neighbour {neighbours[other]}.");
                    }
                }
            }
        }

        private int FindNextNode(int origin, int destination)
        {
            var queue = new Queue<int>();
            var visited = new HashSet<int> { origin };
            var previous = new Dictionary<int, int>();
            queue.Enqueue(origin);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                if (!indexByNodeId.TryGetValue(
                        current,
                        out int currentIndex))
                {
                    continue;
                }

                IReadOnlyList<int> neighbours =
                    nodes[currentIndex].NeighbourNodeIds;
                for (int index = 0; index < neighbours.Count; index++)
                {
                    int next = neighbours[index];
                    if (!visited.Add(next))
                    {
                        continue;
                    }

                    previous[next] = current;
                    if (next == destination)
                    {
                        int step = destination;
                        while (previous.TryGetValue(
                                   step,
                                   out int parent)
                               && parent != origin)
                        {
                            step = parent;
                        }

                        return step;
                    }

                    queue.Enqueue(next);
                }
            }

            return destination;
        }

        private void RebuildLookup()
        {
            indexByNodeId.Clear();
            if (nodes == null)
            {
                return;
            }

            for (int index = 0; index < nodes.Length; index++)
            {
                indexByNodeId[nodes[index].NodeId] = index;
            }
        }

        private void EnsureLookup()
        {
            int expectedCount = nodes?.Length ?? 0;
            if (indexByNodeId.Count != expectedCount)
            {
                RebuildLookup();
            }
        }
    }
}

#endif
