using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkAuthoringGridCell
    {
        public const string EmptyTileCode = "NONE";

        private readonly string[] tileCodes;

        public MicrochunkLocalCoord Coordinate { get; }

        public IReadOnlyList<string> TileCodes =>
            new ReadOnlyCollection<string>((string[])tileCodes.Clone());

        public MicrochunkAuthoringGridCell(int x, int y)
            : this(new MicrochunkLocalCoord(x, y))
        {
        }

        public MicrochunkAuthoringGridCell(MicrochunkLocalCoord coordinate)
        {
            Coordinate = coordinate;
            tileCodes = new string[MicrochunkConstants.LayerCount];
            for (var index = 0; index < tileCodes.Length; index++)
            {
                tileCodes[index] = EmptyTileCode;
            }
        }

        public string GetTileCode(MicrochunkTileLayer layer)
        {
            return tileCodes[MicrochunkAuthoringGridLayer.IndexOf(layer)];
        }

        internal void SetTileCode(MicrochunkTileLayer layer, string tileCode)
        {
            tileCodes[MicrochunkAuthoringGridLayer.IndexOf(layer)] = RequireTileCode(tileCode);
        }

        public MicrochunkTileCell ToRuntimeCell()
        {
            return new MicrochunkTileCell(
                Coordinate,
                tileCodes[0],
                tileCodes[1],
                tileCodes[2],
                tileCodes[3],
                tileCodes[4],
                tileCodes[5],
                tileCodes[6],
                tileCodes[7]);
        }

        private static string RequireTileCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Tile code cannot be null, empty, or whitespace.", nameof(value));
            }

            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException("Tile code must not contain surrounding whitespace.", nameof(value));
            }

            return value;
        }
    }
}
