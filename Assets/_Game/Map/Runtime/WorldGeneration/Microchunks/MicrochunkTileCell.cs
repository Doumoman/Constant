using System;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkTileCell
    {
        public MicrochunkLocalCoord Coordinate { get; }
        public string GroundCode { get; }
        public string OneWayCode { get; }
        public string BreakableCode { get; }
        public string HazardCode { get; }
        public string LiquidCode { get; }
        public string DecorationBackCode { get; }
        public string DecorationFrontCode { get; }
        public string MarkerCode { get; }

        public MicrochunkTileCell(
            MicrochunkLocalCoord coordinate,
            string groundCode,
            string oneWayCode,
            string breakableCode,
            string hazardCode,
            string liquidCode,
            string decorationBackCode,
            string decorationFrontCode,
            string markerCode)
        {
            Coordinate = coordinate;
            GroundCode = RequireCode(groundCode, nameof(groundCode));
            OneWayCode = RequireCode(oneWayCode, nameof(oneWayCode));
            BreakableCode = RequireCode(breakableCode, nameof(breakableCode));
            HazardCode = RequireCode(hazardCode, nameof(hazardCode));
            LiquidCode = RequireCode(liquidCode, nameof(liquidCode));
            DecorationBackCode = RequireCode(decorationBackCode, nameof(decorationBackCode));
            DecorationFrontCode = RequireCode(decorationFrontCode, nameof(decorationFrontCode));
            MarkerCode = RequireCode(markerCode, nameof(markerCode));
        }

        private static string RequireCode(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Tile-code IDs cannot be null, empty, or whitespace.", parameterName);
            }

            return value;
        }
    }
}
