using System;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public sealed class OptionalRegionOverlayConnection
    {
        public OptionalRegionOverlayConnection(
            OptionalRegionOverlayConnectionKind kind,
            OptionalRegionId regionId,
            int fromSectorIndex,
            int toSectorIndex,
            string label,
            OptionalRegionAccessRule accessRule,
            OptionalReturnPolicy returnPolicy)
        {
            if (!Enum.IsDefined(typeof(OptionalRegionOverlayConnectionKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (!regionId.IsValid) throw new ArgumentException("Connection requires a region ID.", nameof(regionId));
            if (fromSectorIndex < 0 || fromSectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(fromSectorIndex));
            if (toSectorIndex < 0 || toSectorIndex >= WorldGenConstants.SectorCount || toSectorIndex == fromSectorIndex)
                throw new ArgumentOutOfRangeException(nameof(toSectorIndex));
            if (string.IsNullOrEmpty(label) || !string.Equals(label, label.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Label must be canonical non-empty text.", nameof(label));
            if (!Enum.IsDefined(typeof(OptionalRegionAccessRule), accessRule)) throw new ArgumentOutOfRangeException(nameof(accessRule));
            if (!Enum.IsDefined(typeof(OptionalReturnPolicy), returnPolicy)) throw new ArgumentOutOfRangeException(nameof(returnPolicy));

            Kind = kind;
            RegionId = regionId;
            FromSectorIndex = fromSectorIndex;
            ToSectorIndex = toSectorIndex;
            Label = label;
            AccessRule = accessRule;
            ReturnPolicy = returnPolicy;
        }

        public OptionalRegionOverlayConnectionKind Kind { get; }
        public OptionalRegionId RegionId { get; }
        public int FromSectorIndex { get; }
        public int ToSectorIndex { get; }
        public string Label { get; }
        public OptionalRegionAccessRule AccessRule { get; }
        public OptionalReturnPolicy ReturnPolicy { get; }
    }
}
