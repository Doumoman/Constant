using System;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkSocketBandAuthoringRow
    {
        public string BandId { get; }
        public string SideToken { get; }
        public int InclusiveStart { get; }
        public int InclusiveEnd { get; }
        public int MinimumClearanceTiles { get; }

        public MicrochunkSocketBandAuthoringRow(
            string bandId,
            string sideToken,
            int inclusiveStart,
            int inclusiveEnd,
            int minimumClearanceTiles = 0)
        {
            BandId = MicrochunkSocketAuthoringRow.RequireCanonicalToken(bandId, nameof(bandId));
            SideToken = RequireSideAndRange(sideToken, inclusiveStart, inclusiveEnd);
            if (minimumClearanceTiles < 0) throw new ArgumentOutOfRangeException(nameof(minimumClearanceTiles));
            InclusiveStart = inclusiveStart;
            InclusiveEnd = inclusiveEnd;
            MinimumClearanceTiles = minimumClearanceTiles;
        }

        public MicrochunkSocketBandAuthoringRow Duplicate(string bandId)
        {
            return new MicrochunkSocketBandAuthoringRow(
                bandId,
                SideToken,
                InclusiveStart,
                InclusiveEnd,
                MinimumClearanceTiles);
        }

        public MicrochunkSocketBandDefinition ToRuntimeDefinition()
        {
            return new MicrochunkSocketBandDefinition(
                BandId,
                ToRuntimeAxis(SideToken),
                InclusiveStart,
                InclusiveEnd,
                InclusiveStart + ((InclusiveEnd - InclusiveStart) / 2),
                MinimumClearanceTiles,
                "In-memory socket-band authoring row for side " + SideToken + ".");
        }

        public static MicrochunkEdgeAxis ToRuntimeAxis(string sideToken)
        {
            switch (sideToken)
            {
                case "L":
                case "R":
                    return MicrochunkEdgeAxis.HorizontalEdge;
                case "D":
                case "U":
                    return MicrochunkEdgeAxis.VerticalEdge;
                default:
                    throw new ArgumentException("Side must be exactly L, R, D, or U.", nameof(sideToken));
            }
        }

        private static string RequireSideAndRange(string sideToken, int start, int end)
        {
            MicrochunkSocketAuthoringRow.ParseSide(sideToken);
            var maximum = sideToken == "L" || sideToken == "R"
                ? MicrochunkConstants.HeightTiles - 1
                : MicrochunkConstants.WidthTiles - 1;
            if (start < 0 || start > end || end > maximum)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(start),
                    "Band range must be inclusive, ordered, and within the selected edge.");
            }
            return sideToken;
        }
    }
}
