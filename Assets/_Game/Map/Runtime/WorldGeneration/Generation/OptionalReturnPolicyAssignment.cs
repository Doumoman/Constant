using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalReturnPolicyAssignment
    {
        private readonly IReadOnlyList<int> criticalReturnPathSectorIndices;

        internal OptionalReturnPolicyAssignment(
            OptionalRegionId regionId,
            int regionOrdinal,
            int attachmentOrder,
            OptionalRegionAccessRule accessRule,
            OptionalRewardTier rewardTier,
            OptionalReturnPolicy returnPolicy,
            int criticalSourceSectorIndex,
            OptionalRegionDepth criticalSourceDepth,
            int attachmentEntrySectorIndex,
            int returnDestinationMandatorySectorIndex,
            IEnumerable<int> sourceCriticalReturnPathSectorIndices,
            int criticalReturnEdgeCount,
            int returnableCellCount,
            bool usesSameOpenedAttachmentBoundary,
            bool requiresReturnDevice)
        {
            OptionalRegionValidation.RequireValid(regionId, nameof(regionId));
            if (regionOrdinal < 0 || regionOrdinal > 9999) throw new ArgumentOutOfRangeException(nameof(regionOrdinal));
            if (attachmentOrder < 0 || attachmentOrder > 9999) throw new ArgumentOutOfRangeException(nameof(attachmentOrder));
            OptionalRegionValidation.RequireAccessRule(accessRule, nameof(accessRule));
            if (rewardTier == OptionalRewardTier.None) throw new ArgumentOutOfRangeException(nameof(rewardTier));
            OptionalRegionValidation.RequireRewardTier(rewardTier, nameof(rewardTier));
            if (returnPolicy != OptionalReturnPolicy.BacktrackToAttachment)
                throw new ArgumentException("Return assignments must backtrack to the attachment.", nameof(returnPolicy));
            OptionalRegionValidation.RequireValid(criticalSourceDepth, nameof(criticalSourceDepth));
            RequireSectorIndex(criticalSourceSectorIndex, nameof(criticalSourceSectorIndex));
            RequireSectorIndex(attachmentEntrySectorIndex, nameof(attachmentEntrySectorIndex));
            RequireSectorIndex(returnDestinationMandatorySectorIndex, nameof(returnDestinationMandatorySectorIndex));
            if (sourceCriticalReturnPathSectorIndices == null)
                throw new ArgumentNullException(nameof(sourceCriticalReturnPathSectorIndices));

            var path = new List<int>(sourceCriticalReturnPathSectorIndices);
            if (path.Count == 0 || path[0] != criticalSourceSectorIndex ||
                path[path.Count - 1] != attachmentEntrySectorIndex)
                throw new ArgumentException("The critical path must run from its source to the attachment entry.", nameof(sourceCriticalReturnPathSectorIndices));
            var unique = new HashSet<int>();
            foreach (var sectorIndex in path)
            {
                RequireSectorIndex(sectorIndex, nameof(sourceCriticalReturnPathSectorIndices));
                if (!unique.Add(sectorIndex))
                    throw new ArgumentException("The critical return path cannot repeat a sector.", nameof(sourceCriticalReturnPathSectorIndices));
            }
            if (criticalReturnEdgeCount != path.Count - 1)
                throw new ArgumentException("Return edge count must equal path sector count minus one.", nameof(criticalReturnEdgeCount));
            if (returnableCellCount < 1 || returnableCellCount > WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(returnableCellCount));
            if (!usesSameOpenedAttachmentBoundary || requiresReturnDevice)
                throw new ArgumentException("Backtrack assignments must reuse the opened attachment without a return device.");

            RegionId = regionId;
            RegionOrdinal = regionOrdinal;
            AttachmentOrder = attachmentOrder;
            AccessRule = accessRule;
            RewardTier = rewardTier;
            ReturnPolicy = returnPolicy;
            CriticalSourceSectorIndex = criticalSourceSectorIndex;
            CriticalSourceDepth = criticalSourceDepth;
            AttachmentEntrySectorIndex = attachmentEntrySectorIndex;
            ReturnDestinationMandatorySectorIndex = returnDestinationMandatorySectorIndex;
            criticalReturnPathSectorIndices = new ReadOnlyCollection<int>(path);
            CriticalReturnEdgeCount = criticalReturnEdgeCount;
            ReturnableCellCount = returnableCellCount;
            UsesSameOpenedAttachmentBoundary = usesSameOpenedAttachmentBoundary;
            RequiresReturnDevice = requiresReturnDevice;
        }

        public OptionalRegionId RegionId { get; }
        public int RegionOrdinal { get; }
        public int AttachmentOrder { get; }
        public OptionalRegionAccessRule AccessRule { get; }
        public OptionalRewardTier RewardTier { get; }
        public OptionalReturnPolicy ReturnPolicy { get; }
        public int CriticalSourceSectorIndex { get; }
        public OptionalRegionDepth CriticalSourceDepth { get; }
        public int AttachmentEntrySectorIndex { get; }
        public int ReturnDestinationMandatorySectorIndex { get; }
        public IReadOnlyList<int> CriticalReturnPathSectorIndices => criticalReturnPathSectorIndices;
        public int CriticalReturnEdgeCount { get; }
        public int ReturnableCellCount { get; }
        public bool UsesSameOpenedAttachmentBoundary { get; }
        public bool RequiresReturnDevice { get; }

        private static void RequireSectorIndex(int value, string parameterName)
        {
            if (value < 0 || value >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
