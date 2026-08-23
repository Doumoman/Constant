using System;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public enum MicrochunkEdgeAxis
    {
        HorizontalEdge,
        VerticalEdge,
        Solid
    }

    public sealed class MicrochunkSocketBandDefinition
    {
        public string BandId { get; }
        public MicrochunkEdgeAxis Axis { get; }
        public string AxisToken => ToAxisToken(Axis);
        public int MinimumLocalCoordinate { get; }
        public int MaximumLocalCoordinate { get; }
        public int RecommendedCenter { get; }
        public int MinimumClearanceTiles { get; }
        public string Description { get; }

        public MicrochunkSocketBandDefinition(
            string bandId,
            string axisToken,
            int minimumLocalCoordinate,
            int maximumLocalCoordinate,
            int recommendedCenter,
            int minimumClearanceTiles,
            string description)
            : this(
                bandId,
                ParseAxisToken(axisToken),
                minimumLocalCoordinate,
                maximumLocalCoordinate,
                recommendedCenter,
                minimumClearanceTiles,
                description)
        {
        }

        public MicrochunkSocketBandDefinition(
            string bandId,
            MicrochunkEdgeAxis axis,
            int minimumLocalCoordinate,
            int maximumLocalCoordinate,
            int recommendedCenter,
            int minimumClearanceTiles,
            string description)
        {
            if (string.IsNullOrWhiteSpace(bandId))
            {
                throw new ArgumentException("Band ID is required.", nameof(bandId));
            }

            if (!Enum.IsDefined(typeof(MicrochunkEdgeAxis), axis))
            {
                throw new ArgumentOutOfRangeException(nameof(axis));
            }

            BandId = bandId;
            Axis = axis;
            MinimumLocalCoordinate = minimumLocalCoordinate;
            MaximumLocalCoordinate = maximumLocalCoordinate;
            RecommendedCenter = recommendedCenter;
            MinimumClearanceTiles = minimumClearanceTiles;
            Description = description ?? string.Empty;
        }

        public static bool TryParseAxisToken(string token, out MicrochunkEdgeAxis axis)
        {
            switch (token)
            {
                case "HORIZONTAL_EDGE":
                    axis = MicrochunkEdgeAxis.HorizontalEdge;
                    return true;
                case "VERTICAL_EDGE":
                    axis = MicrochunkEdgeAxis.VerticalEdge;
                    return true;
                case "SOLID":
                    axis = MicrochunkEdgeAxis.Solid;
                    return true;
                default:
                    axis = default;
                    return false;
            }
        }

        public static string ToAxisToken(MicrochunkEdgeAxis axis)
        {
            switch (axis)
            {
                case MicrochunkEdgeAxis.HorizontalEdge: return "HORIZONTAL_EDGE";
                case MicrochunkEdgeAxis.VerticalEdge: return "VERTICAL_EDGE";
                case MicrochunkEdgeAxis.Solid: return "SOLID";
                default: throw new ArgumentOutOfRangeException(nameof(axis));
            }
        }

        private static MicrochunkEdgeAxis ParseAxisToken(string token)
        {
            if (!TryParseAxisToken(token, out var axis))
            {
                throw new ArgumentException(
                    "Axis must be exactly HORIZONTAL_EDGE, VERTICAL_EDGE, or SOLID.",
                    nameof(token));
            }

            return axis;
        }
    }
}
