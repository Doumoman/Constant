using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkAuthoringGridPalette
    {
        private static readonly IReadOnlyList<string> DefaultSwatchValues =
            new ReadOnlyCollection<string>(new[]
            {
                "NONE",
                "G_STARSTONE", "G_MOON_ROCK", "G_CASSIA_WOOD", "G_MILL_METAL", "G_DOUGH_SOLID", "G_SOIL",
                "P_ONEWAY",
                "B_CRACKED_ROCK", "B_SOFT_SOIL", "B_DOUGH_WALL",
                "H_SPIKE", "H_HOT_GAS",
                "L_WATER", "L_CASSIA_SAP",
                "DB_CRATER", "DB_ROOT", "DB_MILL", "DB_DOUGH",
                "DF_ROOT_VINE",
                "M_SAFE", "M_ROUTE_MAIN", "M_ROUTE_OPTIONAL", "M_SOCKET", "M_SLOT_RESOURCE",
                "M_SLOT_HAZARD", "M_SLOT_EVENT", "M_NO_SPAWN"
            });

        public MicrochunkTileLayer SelectedLayer { get; private set; }
        public string SelectedTileCode { get; private set; }
        public IReadOnlyList<MicrochunkTileLayer> AvailableLayers => MicrochunkAuthoringGridLayer.OrderedLayers;
        public IReadOnlyList<string> Swatches => DefaultSwatchValues;
        public bool IsErasing => string.Equals(
            SelectedTileCode,
            MicrochunkAuthoringGridCell.EmptyTileCode,
            StringComparison.Ordinal);

        public MicrochunkAuthoringGridPalette()
        {
            SelectedLayer = MicrochunkAuthoringGridLayer.At(0);
            SelectedTileCode = MicrochunkAuthoringGridCell.EmptyTileCode;
        }

        public void SelectLayer(MicrochunkTileLayer layer)
        {
            MicrochunkAuthoringGridLayer.IndexOf(layer);
            SelectedLayer = layer;
        }

        public void SelectTileCode(string tileCode)
        {
            if (string.IsNullOrWhiteSpace(tileCode))
            {
                throw new ArgumentException("Tile code cannot be null, empty, or whitespace.", nameof(tileCode));
            }

            if (!string.Equals(tileCode, tileCode.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException("Tile code must not contain surrounding whitespace.", nameof(tileCode));
            }

            SelectedTileCode = tileCode;
        }

        public void Select(MicrochunkTileLayer layer, string tileCode)
        {
            MicrochunkAuthoringGridLayer.IndexOf(layer);
            SelectTileCode(tileCode);
            SelectedLayer = layer;
        }

        public void SelectErase()
        {
            SelectedTileCode = MicrochunkAuthoringGridCell.EmptyTileCode;
        }
    }
}
