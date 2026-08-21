#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Grid;

namespace StarNight.Explosions
{
    public readonly struct ExplosionChainNode
    {
        public ExplosionChainNode(int id, GridPos cell)
        {
            Id = id;
            Cell = cell;
        }

        public int Id { get; }
        public GridPos Cell { get; }
    }

    public sealed class ExplosionChainResolution
    {
        private readonly int[] processingOrder;

        internal ExplosionChainResolution(
            int seedCount,
            IReadOnlyList<int> order,
            int hardCap,
            int queuedWhenCapped)
        {
            SeedCount = seedCount;
            processingOrder = new int[order.Count];
            for (int index = 0; index < order.Count; index++)
            {
                processingOrder[index] = order[index];
            }

            HardCap = hardCap;
            QueuedWhenCapped = queuedWhenCapped;
        }

        public int SeedCount { get; }
        public int ProcessedCount => processingOrder.Length;
        public int HardCap { get; }
        public int QueuedWhenCapped { get; }
        public bool HardCapReached => QueuedWhenCapped > 0;
        public IReadOnlyList<int> ProcessingOrder => processingOrder;
    }

    public static class ExplosionChainResolver
    {
        public static ExplosionChainResolution Resolve(
            IEnumerable<ExplosionChainNode> nodes,
            IEnumerable<int> startingIds,
            int hardCap = ExplosionConstants.DefaultChainHardCap)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (startingIds == null)
            {
                throw new ArgumentNullException(nameof(startingIds));
            }

            if (hardCap < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(hardCap), "Hard cap must be at least one.");
            }

            List<ExplosionChainNode> sortedNodes = new List<ExplosionChainNode>(nodes);
            sortedNodes.Sort(CompareNodes);

            Dictionary<int, ExplosionChainNode> nodesById =
                new Dictionary<int, ExplosionChainNode>(sortedNodes.Count);
            for (int index = 0; index < sortedNodes.Count; index++)
            {
                ExplosionChainNode node = sortedNodes[index];
                if (nodesById.ContainsKey(node.Id))
                {
                    throw new ArgumentException(
                        $"Explosion chain node id {node.Id} is duplicated.",
                        nameof(nodes));
                }

                nodesById.Add(node.Id, node);
            }

            List<int> sortedSeeds = new List<int>(startingIds);
            sortedSeeds.Sort();

            Queue<int> pending = new Queue<int>();
            HashSet<int> queued = new HashSet<int>();
            for (int index = 0; index < sortedSeeds.Count; index++)
            {
                int seedId = sortedSeeds[index];
                if (nodesById.ContainsKey(seedId) && queued.Add(seedId))
                {
                    pending.Enqueue(seedId);
                }
            }

            int seedCount = pending.Count;
            List<int> processingOrder = new List<int>(
                Math.Min(nodesById.Count, hardCap));

            while (pending.Count > 0 && processingOrder.Count < hardCap)
            {
                int currentId = pending.Dequeue();
                ExplosionChainNode current = nodesById[currentId];
                processingOrder.Add(currentId);

                for (int index = 0; index < sortedNodes.Count; index++)
                {
                    ExplosionChainNode candidate = sortedNodes[index];
                    if (!queued.Contains(candidate.Id)
                        && ExplosionMask3x3.Contains(current.Cell, candidate.Cell))
                    {
                        queued.Add(candidate.Id);
                        pending.Enqueue(candidate.Id);
                    }
                }
            }

            return new ExplosionChainResolution(
                seedCount,
                processingOrder,
                hardCap,
                pending.Count);
        }

        private static int CompareNodes(ExplosionChainNode left, ExplosionChainNode right)
        {
            return left.Id.CompareTo(right.Id);
        }
    }
}

#endif
