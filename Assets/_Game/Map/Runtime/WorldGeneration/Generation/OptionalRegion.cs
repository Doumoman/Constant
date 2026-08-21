using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalRegion
    {
        private readonly IReadOnlyList<OptionalRegionCell> cells;

        public OptionalRegion(
            OptionalRegionId regionId,
            OptionalRegionAttachment attachment,
            OptionalRegionAccessRule accessRule,
            OptionalRewardTier rewardTier,
            OptionalReturnPolicy returnPolicy,
            IEnumerable<OptionalRegionCell> cells,
            OptionalRegionDepth maxDepth)
        {
            OptionalRegionValidation.RequireValid(regionId, nameof(regionId));
            if (attachment == null)
            {
                throw new ArgumentNullException(nameof(attachment));
            }

            if (attachment.RegionId != regionId)
            {
                throw new ArgumentException("Attachment region ID must match the aggregate.", nameof(attachment));
            }

            OptionalRegionValidation.RequireAccessRule(accessRule, nameof(accessRule));
            OptionalRegionValidation.RequireRewardTier(rewardTier, nameof(rewardTier));
            OptionalRegionValidation.RequireReturnPolicy(returnPolicy, nameof(returnPolicy));
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            OptionalRegionValidation.RequireValid(maxDepth, nameof(maxDepth));
            var values = new List<OptionalRegionCell>(cells);
            if (values.Count == 0)
            {
                throw new ArgumentException("Optional regions require at least one cell.", nameof(cells));
            }

            var sectorIndices = new HashSet<int>();
            var observedMaxDepth = 0;
            var attachmentCellCount = 0;
            foreach (var cell in values)
            {
                if (cell == null)
                {
                    throw new ArgumentException("Optional region cells cannot contain null.", nameof(cells));
                }

                if (cell.RegionId != regionId)
                {
                    throw new ArgumentException("Every cell region ID must match the aggregate.", nameof(cells));
                }

                if (!sectorIndices.Add(cell.SectorIndex))
                {
                    throw new ArgumentException("Optional region sector indices must be unique.", nameof(cells));
                }

                if (cell.Depth.Value > observedMaxDepth)
                {
                    observedMaxDepth = cell.Depth.Value;
                }

                if (cell.IsAttachmentCell)
                {
                    attachmentCellCount++;
                    if (cell.SectorIndex != attachment.EntrySectorIndex ||
                        cell.Sector != attachment.EntrySector)
                    {
                        throw new ArgumentException("Attachment cell must match the attachment entry sector.", nameof(cells));
                    }
                }
            }

            if (attachmentCellCount != 1)
            {
                throw new ArgumentException("Exactly one attachment cell is required.", nameof(cells));
            }

            if (observedMaxDepth != maxDepth.Value)
            {
                throw new ArgumentException("Max depth must match the deepest cell.", nameof(maxDepth));
            }

            values.Sort(CompareCells);
            RegionId = regionId;
            Attachment = attachment;
            AccessRule = accessRule;
            RewardTier = rewardTier;
            ReturnPolicy = returnPolicy;
            this.cells = new ReadOnlyCollection<OptionalRegionCell>(values);
            MaxDepth = maxDepth;
        }

        public OptionalRegionId RegionId { get; }
        public OptionalRegionAttachment Attachment { get; }
        public OptionalRegionAccessRule AccessRule { get; }
        public OptionalRewardTier RewardTier { get; }
        public OptionalReturnPolicy ReturnPolicy { get; }
        public IReadOnlyList<OptionalRegionCell> Cells => cells;
        public OptionalRegionDepth MaxDepth { get; }

        internal static int CompareCells(OptionalRegionCell left, OptionalRegionCell right)
        {
            var sector = left.SectorIndex.CompareTo(right.SectorIndex);
            if (sector != 0) return sector;
            var depth = left.Depth.CompareTo(right.Depth);
            if (depth != 0) return depth;
            var x = left.Sector.X.CompareTo(right.Sector.X);
            return x != 0 ? x : left.Sector.Y.CompareTo(right.Sector.Y);
        }
    }
}
