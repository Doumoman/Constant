using System;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public sealed class OptionalRegionOverlayLegendEntry
    {
        public OptionalRegionOverlayLegendEntry(
            int order,
            OptionalRegionOverlayLayer layer,
            OptionalRegionOverlayColorToken colorToken,
            string label)
        {
            if (order < 0) throw new ArgumentOutOfRangeException(nameof(order));
            if (!Enum.IsDefined(typeof(OptionalRegionOverlayLayer), layer)) throw new ArgumentOutOfRangeException(nameof(layer));
            if (!Enum.IsDefined(typeof(OptionalRegionOverlayColorToken), colorToken)) throw new ArgumentOutOfRangeException(nameof(colorToken));
            if (string.IsNullOrEmpty(label) || !string.Equals(label, label.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Legend label must be canonical non-empty text.", nameof(label));
            Order = order;
            Layer = layer;
            ColorToken = colorToken;
            Label = label;
        }

        public int Order { get; }
        public OptionalRegionOverlayLayer Layer { get; }
        public OptionalRegionOverlayColorToken ColorToken { get; }
        public string Label { get; }
    }
}
