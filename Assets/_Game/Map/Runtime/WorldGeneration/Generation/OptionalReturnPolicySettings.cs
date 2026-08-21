using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalReturnPolicySettings
    {
        public OptionalReturnPolicySettings(
            int maximumBacktrackSectorCount,
            bool requireAllCellsReturnable)
        {
            if (maximumBacktrackSectorCount < 1 || maximumBacktrackSectorCount > 169)
                throw new ArgumentOutOfRangeException(nameof(maximumBacktrackSectorCount));
            if (!requireAllCellsReturnable)
                throw new ArgumentException("All optional cells must be returnable.", nameof(requireAllCellsReturnable));

            MaximumBacktrackSectorCount = maximumBacktrackSectorCount;
            RequireAllCellsReturnable = requireAllCellsReturnable;
        }

        public int MaximumBacktrackSectorCount { get; }
        public bool RequireAllCellsReturnable { get; }
    }
}
