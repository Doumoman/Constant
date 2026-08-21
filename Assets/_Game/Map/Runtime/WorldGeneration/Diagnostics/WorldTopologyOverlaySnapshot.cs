using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public sealed class WorldTopologyOverlaySnapshot
    {
        private readonly IReadOnlyList<WorldTopologyOverlayCell> cells;

        private WorldTopologyOverlaySnapshot(
            ulong seed,
            IReadOnlyList<WorldTopologyOverlayCell> sourceCells)
        {
            Seed = seed;
            cells = sourceCells;
        }

        public ulong Seed { get; }
        public IReadOnlyList<WorldTopologyOverlayCell> Cells => cells;
        public int Count => cells.Count;

        public static WorldTopologyOverlaySnapshot Create(GridInitializationResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.WorldData == null ||
                result.WorldData.Cells == null ||
                result.Neighbors == null ||
                result.WorldData.Cells.Count != WorldGenConstants.SectorCount ||
                result.Neighbors.Count != WorldGenConstants.SectorCount)
            {
                throw new ArgumentException(
                    "An exact 169-cell grid initialization result is required.",
                    nameof(result));
            }

            var copiedCells = new List<WorldTopologyOverlayCell>(WorldGenConstants.SectorCount);
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var sourceCell = result.WorldData.Cells[index];
                var sourceNeighbors = result.Neighbors[index];
                if (sourceCell == null || sourceCell.Index != index)
                {
                    throw new ArgumentException(
                        "World cells must be ordered by exact grid index.",
                        nameof(result));
                }

                if (sourceNeighbors == null || sourceNeighbors.Index != index)
                {
                    throw new ArgumentException(
                        "Neighbor entries must be ordered by exact grid index.",
                        nameof(result));
                }

                copiedCells.Add(new WorldTopologyOverlayCell(sourceCell, sourceNeighbors));
            }

            ValidateReciprocalTopology(copiedCells, nameof(result));
            return new WorldTopologyOverlaySnapshot(
                result.WorldData.Seed,
                new ReadOnlyCollection<WorldTopologyOverlayCell>(copiedCells));
        }

        public WorldTopologyOverlayCell GetCell(int index)
        {
            if (!TryGetCell(index, out var cell))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return cell;
        }

        public WorldTopologyOverlayCell GetCell(SectorCoord coordinate)
        {
            return GetCell(WorldGridIndex.ToIndex(coordinate));
        }

        public bool TryGetCell(int index, out WorldTopologyOverlayCell cell)
        {
            if (index < 0 || index >= cells.Count)
            {
                cell = null;
                return false;
            }

            cell = cells[index];
            return true;
        }

        private static void ValidateReciprocalTopology(
            IReadOnlyList<WorldTopologyOverlayCell> sourceCells,
            string parameterName)
        {
            for (var index = 0; index < sourceCells.Count; index++)
            {
                var cell = sourceCells[index];
                ValidateReciprocal(
                    sourceCells,
                    index,
                    cell.LeftIndex,
                    neighbor => neighbor.RightIndex,
                    parameterName);
                ValidateReciprocal(
                    sourceCells,
                    index,
                    cell.RightIndex,
                    neighbor => neighbor.LeftIndex,
                    parameterName);
                ValidateReciprocal(
                    sourceCells,
                    index,
                    cell.UpIndex,
                    neighbor => neighbor.DownIndex,
                    parameterName);
                ValidateReciprocal(
                    sourceCells,
                    index,
                    cell.DownIndex,
                    neighbor => neighbor.UpIndex,
                    parameterName);
            }
        }

        private static void ValidateReciprocal(
            IReadOnlyList<WorldTopologyOverlayCell> sourceCells,
            int index,
            int neighborIndex,
            Func<WorldTopologyOverlayCell, int> reciprocalSelector,
            string parameterName)
        {
            if (neighborIndex == SectorNeighborIndices.NoNeighbor)
            {
                return;
            }

            if (neighborIndex < 0 ||
                neighborIndex >= sourceCells.Count ||
                reciprocalSelector(sourceCells[neighborIndex]) != index)
            {
                throw new ArgumentException(
                    "Grid neighbor topology must be reciprocal.",
                    parameterName);
            }
        }
    }
}
