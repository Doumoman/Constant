using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class GeneratedWorldData
    {
        private readonly IReadOnlyList<SectorCell> cells;
        private readonly IReadOnlyDictionary<int, SectorCell> cellsByIndex;
        private readonly IReadOnlyDictionary<SectorCoord, SectorCell> cellsByCoordinate;

        public ulong Seed { get; }
        public IReadOnlyList<SectorCell> Cells => cells;

        public GeneratedWorldData(ulong seed, IEnumerable<SectorCell> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            var snapshot = new List<SectorCell>(cells);
            if (snapshot.Count != WorldGenConstants.SectorCount)
            {
                throw new ArgumentException(
                    $"Exactly {WorldGenConstants.SectorCount} cells are required.",
                    nameof(cells));
            }

            var byIndex = new Dictionary<int, SectorCell>(WorldGenConstants.SectorCount);
            var byCoordinate = new Dictionary<SectorCoord, SectorCell>(WorldGenConstants.SectorCount);
            foreach (var cell in snapshot)
            {
                if (cell == null)
                {
                    throw new ArgumentException("Cells cannot contain null.", nameof(cells));
                }

                if (!byIndex.TryAdd(cell.Index, cell))
                {
                    throw new ArgumentException($"Duplicate cell index: {cell.Index}.", nameof(cells));
                }

                if (!byCoordinate.TryAdd(cell.Coordinate, cell))
                {
                    throw new ArgumentException($"Duplicate sector coordinate: {cell.Coordinate}.", nameof(cells));
                }
            }

            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                if (!byIndex.ContainsKey(index))
                {
                    throw new ArgumentException($"Missing cell index: {index}.", nameof(cells));
                }
            }

            for (var y = 0; y < WorldGenConstants.SectorRows; y++)
            {
                for (var x = 0; x < WorldGenConstants.SectorColumns; x++)
                {
                    var coordinate = new SectorCoord(x, y);
                    if (!byCoordinate.ContainsKey(coordinate))
                    {
                        throw new ArgumentException($"Missing sector coordinate: {coordinate}.", nameof(cells));
                    }
                }
            }

            snapshot.Sort((left, right) => left.Index.CompareTo(right.Index));

            Seed = seed;
            this.cells = new ReadOnlyCollection<SectorCell>(snapshot);
            cellsByIndex = new ReadOnlyDictionary<int, SectorCell>(byIndex);
            cellsByCoordinate = new ReadOnlyDictionary<SectorCoord, SectorCell>(byCoordinate);
        }

        public SectorCell GetCell(int index)
        {
            if (!cellsByIndex.TryGetValue(index, out var cell))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return cell;
        }

        public bool TryGetCell(int index, out SectorCell cell)
        {
            return cellsByIndex.TryGetValue(index, out cell);
        }

        public SectorCell GetCell(SectorCoord coordinate)
        {
            if (!cellsByCoordinate.TryGetValue(coordinate, out var cell))
            {
                throw new ArgumentOutOfRangeException(nameof(coordinate));
            }

            return cell;
        }

        public bool TryGetCell(SectorCoord coordinate, out SectorCell cell)
        {
            return cellsByCoordinate.TryGetValue(coordinate, out cell);
        }
    }
}
