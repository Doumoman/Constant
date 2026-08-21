using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalRegionGrowthSettings
    {
        private readonly IReadOnlyList<OptionalRegionDepth> targetDepthPattern;

        public OptionalRegionGrowthSettings(
            int maxRegions,
            int maxCellsPerRegion,
            IEnumerable<OptionalRegionDepth> targetDepthPattern)
        {
            if (maxRegions < 1 || maxRegions > 9999)
                throw new ArgumentOutOfRangeException(nameof(maxRegions));
            if (maxCellsPerRegion < 1 || maxCellsPerRegion > 16)
                throw new ArgumentOutOfRangeException(nameof(maxCellsPerRegion));
            if (targetDepthPattern == null)
                throw new ArgumentNullException(nameof(targetDepthPattern));

            var values = new List<OptionalRegionDepth>(targetDepthPattern);
            if (values.Count == 0)
                throw new ArgumentException("Target depth pattern cannot be empty.", nameof(targetDepthPattern));

            var maximumDepth = 0;
            foreach (var depth in values)
            {
                OptionalRegionValidation.RequireValid(depth, nameof(targetDepthPattern));
                if (depth.Value > maximumDepth) maximumDepth = depth.Value;
            }
            if (maxCellsPerRegion < maximumDepth)
                throw new ArgumentException("Max cells must be at least the greatest target depth.", nameof(maxCellsPerRegion));

            MaxRegions = maxRegions;
            MaxCellsPerRegion = maxCellsPerRegion;
            this.targetDepthPattern = new ReadOnlyCollection<OptionalRegionDepth>(values);
        }

        public int MaxRegions { get; }
        public int MaxCellsPerRegion { get; }
        public IReadOnlyList<OptionalRegionDepth> TargetDepthPattern => targetDepthPattern;

        public OptionalRegionDepth GetTargetDepth(int attachmentOrder)
        {
            if (attachmentOrder < 0) throw new ArgumentOutOfRangeException(nameof(attachmentOrder));
            return targetDepthPattern[attachmentOrder % targetDepthPattern.Count];
        }
    }
}
