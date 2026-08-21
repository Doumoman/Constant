using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public sealed class OptionalRegionOverlayCell
    {
        private readonly IReadOnlyList<OptionalRegionOverlayLayer> layers;

        public OptionalRegionOverlayCell(
            int sectorIndex,
            SectorCoord coord,
            OptionalRegionOverlayCellKind kind,
            OptionalRegionId regionId,
            int depth,
            OptionalRegionAccessRule accessRule,
            OptionalRewardTier rewardTier,
            OptionalReturnPolicy returnPolicy,
            InactiveBufferKind inactiveKind,
            OptionalRegionOverlayColorToken colorToken,
            string label,
            IEnumerable<OptionalRegionOverlayLayer> sourceLayers)
        {
            if (sectorIndex < 0 || sectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (coord != WorldGridIndex.ToCoordinate(sectorIndex))
                throw new ArgumentException("Sector index and coordinate must match.", nameof(coord));
            if (!Enum.IsDefined(typeof(OptionalRegionOverlayCellKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (!Enum.IsDefined(typeof(OptionalRegionOverlayColorToken), colorToken))
                throw new ArgumentOutOfRangeException(nameof(colorToken));
            if (kind == OptionalRegionOverlayCellKind.Type0)
            {
                if (!regionId.IsValid) throw new ArgumentException("Type0 cells require a region ID.", nameof(regionId));
                if (depth < 1 || depth > 4) throw new ArgumentOutOfRangeException(nameof(depth));
                if (!Enum.IsDefined(typeof(OptionalRegionAccessRule), accessRule)) throw new ArgumentOutOfRangeException(nameof(accessRule));
                if (rewardTier == OptionalRewardTier.None || !Enum.IsDefined(typeof(OptionalRewardTier), rewardTier))
                    throw new ArgumentOutOfRangeException(nameof(rewardTier));
                if (!Enum.IsDefined(typeof(OptionalReturnPolicy), returnPolicy)) throw new ArgumentOutOfRangeException(nameof(returnPolicy));
            }
            else if (depth != 0 || regionId.IsValid)
            {
                throw new ArgumentException("Non-Type0 cells cannot publish region depth data.");
            }
            if ((kind == OptionalRegionOverlayCellKind.InactiveInterior ||
                 kind == OptionalRegionOverlayCellKind.InactiveDecorative) &&
                !Enum.IsDefined(typeof(InactiveBufferKind), inactiveKind))
                throw new ArgumentOutOfRangeException(nameof(inactiveKind));
            if (string.IsNullOrEmpty(label) || !string.Equals(label, label.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Label must be canonical non-empty text.", nameof(label));
            if (sourceLayers == null) throw new ArgumentNullException(nameof(sourceLayers));

            var values = new List<OptionalRegionOverlayLayer>(sourceLayers);
            var unique = new HashSet<OptionalRegionOverlayLayer>();
            foreach (var layer in values)
            {
                if (!Enum.IsDefined(typeof(OptionalRegionOverlayLayer), layer))
                    throw new ArgumentOutOfRangeException(nameof(sourceLayers));
                if (!unique.Add(layer)) throw new ArgumentException("Overlay layers must be unique.", nameof(sourceLayers));
            }
            values.Sort();
            if (values.Count == 0 || values[0] != OptionalRegionOverlayLayer.BaseRole)
                throw new ArgumentException("Every overlay cell requires the BaseRole layer.", nameof(sourceLayers));

            SectorIndex = sectorIndex;
            Coord = coord;
            Kind = kind;
            RegionId = regionId;
            Depth = depth;
            AccessRule = accessRule;
            RewardTier = rewardTier;
            ReturnPolicy = returnPolicy;
            InactiveKind = inactiveKind;
            ColorToken = colorToken;
            Label = label;
            layers = new ReadOnlyCollection<OptionalRegionOverlayLayer>(values);
        }

        public int SectorIndex { get; }
        public SectorCoord Coord { get; }
        public OptionalRegionOverlayCellKind Kind { get; }
        public OptionalRegionId RegionId { get; }
        public int Depth { get; }
        public OptionalRegionAccessRule AccessRule { get; }
        public OptionalRewardTier RewardTier { get; }
        public OptionalReturnPolicy ReturnPolicy { get; }
        public InactiveBufferKind InactiveKind { get; }
        public OptionalRegionOverlayColorToken ColorToken { get; }
        public string Label { get; }
        public IReadOnlyList<OptionalRegionOverlayLayer> Layers => layers;
    }
}
