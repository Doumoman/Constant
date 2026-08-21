#if LEGACY_DISABLED
using System;
using System.Collections.Generic;

namespace StarNight.Explosions
{
    [Serializable]
    public sealed class ExplosionChainReport
    {
        private static readonly int[] emptyOrder = Array.Empty<int>();

        private readonly int[] processingOrder;

        public ExplosionChainReport(
            int seedCount,
            int mutationRequestCount,
            int impulsedBodyCount,
            int hardCap,
            int suppressedBombCount,
            IReadOnlyList<int> order)
        {
            SeedCount = seedCount;
            MutationRequestCount = mutationRequestCount;
            ImpulsedBodyCount = impulsedBodyCount;
            HardCap = hardCap;
            SuppressedBombCount = suppressedBombCount;

            processingOrder = new int[order.Count];
            for (int index = 0; index < order.Count; index++)
            {
                processingOrder[index] = order[index];
            }
        }

        private ExplosionChainReport()
        {
            processingOrder = emptyOrder;
            HardCap = ExplosionConstants.DefaultChainHardCap;
        }

        public static ExplosionChainReport Empty { get; } = new ExplosionChainReport();

        public int SeedCount { get; }
        public int ProcessedBombCount => processingOrder.Length;
        public int MutationRequestCount { get; }
        public int ImpulsedBodyCount { get; }
        public int HardCap { get; }
        public int SuppressedBombCount { get; }
        public bool HardCapReached => SuppressedBombCount > 0;
        public IReadOnlyList<int> ProcessingOrder => processingOrder;

        public override string ToString()
        {
            return $"Explosion chain: seeds={SeedCount}, processed={ProcessedBombCount}, "
                + $"mutations={MutationRequestCount}, bodies={ImpulsedBodyCount}, "
                + $"capped={HardCapReached}, suppressed={SuppressedBombCount}.";
        }
    }
}

#endif
