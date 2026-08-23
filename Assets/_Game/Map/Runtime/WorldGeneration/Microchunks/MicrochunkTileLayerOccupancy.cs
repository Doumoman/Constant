using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkTileLayerOccupancy
    {
        private readonly IReadOnlyList<MicrochunkTileLayer> occupiedLayers;
        private readonly string[] layerCodes;

        public MicrochunkLocalCoord Coordinate { get; }
        public IReadOnlyList<MicrochunkTileLayer> OccupiedLayers => occupiedLayers;
        public int Count => occupiedLayers.Count;

        private MicrochunkTileLayerOccupancy(MicrochunkTileCell cell)
        {
            Coordinate = cell.Coordinate;
            layerCodes = new[]
            {
                cell.GroundCode,
                cell.OneWayCode,
                cell.BreakableCode,
                cell.HazardCode,
                cell.LiquidCode,
                cell.DecorationBackCode,
                cell.DecorationFrontCode,
                cell.MarkerCode
            };

            var occupied = new List<MicrochunkTileLayer>(MicrochunkConstants.LayerCount);
            for (var index = 0; index < layerCodes.Length; index++)
            {
                if (IsOccupiedCode(layerCodes[index]))
                {
                    occupied.Add((MicrochunkTileLayer)index);
                }
            }

            occupiedLayers = new ReadOnlyCollection<MicrochunkTileLayer>(occupied);
        }

        public static MicrochunkTileLayerOccupancy FromCell(MicrochunkTileCell cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            return new MicrochunkTileLayerOccupancy(cell);
        }

        public bool IsOccupied(MicrochunkTileLayer layer)
        {
            ValidateLayer(layer);
            return IsOccupiedCode(layerCodes[(int)layer]);
        }

        public string GetCode(MicrochunkTileLayer layer)
        {
            ValidateLayer(layer);
            return layerCodes[(int)layer];
        }

        private static bool IsOccupiedCode(string code)
        {
            return !string.IsNullOrEmpty(code) &&
                   !string.Equals(code, "NONE", StringComparison.Ordinal);
        }

        private static void ValidateLayer(MicrochunkTileLayer layer)
        {
            if (!Enum.IsDefined(typeof(MicrochunkTileLayer), layer))
            {
                throw new ArgumentOutOfRangeException(nameof(layer));
            }
        }
    }
}
