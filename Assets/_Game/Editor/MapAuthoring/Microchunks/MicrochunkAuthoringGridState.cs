using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkAuthoringGridState
    {
        private readonly MicrochunkAuthoringGridCell[] cells;
        private readonly IReadOnlyList<MicrochunkAuthoringGridCell> readOnlyCells;

        public int Width => MicrochunkConstants.WidthTiles;
        public int Height => MicrochunkConstants.HeightTiles;
        public int CellCount => cells.Length;
        public IReadOnlyList<MicrochunkAuthoringGridCell> Cells => readOnlyCells;

        public MicrochunkAuthoringGridState()
        {
            cells = new MicrochunkAuthoringGridCell[MicrochunkConstants.CellCount];
            for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
            {
                for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
                {
                    var coordinate = new MicrochunkLocalCoord(x, y);
                    cells[coordinate.RowMajorIndex] = new MicrochunkAuthoringGridCell(coordinate);
                }
            }

            readOnlyCells = new ReadOnlyCollection<MicrochunkAuthoringGridCell>(cells);
        }

        public MicrochunkAuthoringGridCell GetCell(int x, int y)
        {
            return GetCell(new MicrochunkLocalCoord(x, y));
        }

        public MicrochunkAuthoringGridCell GetCell(MicrochunkLocalCoord coordinate)
        {
            return cells[coordinate.RowMajorIndex];
        }

        public string GetTileCode(int x, int y, MicrochunkTileLayer layer)
        {
            return GetCell(x, y).GetTileCode(layer);
        }

        public void PaintCell(int x, int y, MicrochunkTileLayer layer, string tileCode)
        {
            GetCell(x, y).SetTileCode(layer, tileCode);
        }

        public IReadOnlyList<MicrochunkLocalCoord> PaintRectangle(
            int minimumX,
            int minimumY,
            int maximumX,
            int maximumY,
            MicrochunkTileLayer layer,
            string tileCode)
        {
            var minimum = new MicrochunkLocalCoord(minimumX, minimumY);
            var maximum = new MicrochunkLocalCoord(maximumX, maximumY);
            MicrochunkAuthoringGridLayer.IndexOf(layer);
            if (minimum.X > maximum.X) throw new ArgumentException("Minimum X must not exceed maximum X.", nameof(minimumX));
            if (minimum.Y > maximum.Y) throw new ArgumentException("Minimum Y must not exceed maximum Y.", nameof(minimumY));

            var applied = new List<MicrochunkLocalCoord>();
            for (var y = minimum.Y; y <= maximum.Y; y++)
            {
                for (var x = minimum.X; x <= maximum.X; x++)
                {
                    PaintCell(x, y, layer, tileCode);
                    applied.Add(new MicrochunkLocalCoord(x, y));
                }
            }

            return new ReadOnlyCollection<MicrochunkLocalCoord>(applied);
        }

        public void ClearLayer(MicrochunkTileLayer layer)
        {
            MicrochunkAuthoringGridLayer.IndexOf(layer);
            for (var index = 0; index < cells.Length; index++)
            {
                cells[index].SetTileCode(layer, MicrochunkAuthoringGridCell.EmptyTileCode);
            }
        }

        public void ClearAllLayers()
        {
            foreach (var layer in MicrochunkAuthoringGridLayer.OrderedLayers)
            {
                ClearLayer(layer);
            }
        }
    }
}
