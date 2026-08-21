using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class GridInitializationResult
    {
        private readonly IReadOnlyList<SectorNeighborIndices> neighbors;

        public GridInitializationResult(
            GeneratedWorldData worldData,
            IEnumerable<SectorNeighborIndices> neighbors)
        {
            WorldData = worldData ?? throw new ArgumentNullException(nameof(worldData));
            if (neighbors == null)
            {
                throw new ArgumentNullException(nameof(neighbors));
            }

            var snapshot = new List<SectorNeighborIndices>(neighbors);
            if (snapshot.Count != WorldGenConstants.SectorCount)
            {
                throw new ArgumentException(
                    $"Exactly {WorldGenConstants.SectorCount} neighbor entries are required.",
                    nameof(neighbors));
            }

            var byIndex = new Dictionary<int, SectorNeighborIndices>(WorldGenConstants.SectorCount);
            foreach (var entry in snapshot)
            {
                if (entry == null)
                {
                    throw new ArgumentException("Neighbor entries cannot contain null.", nameof(neighbors));
                }

                if (!byIndex.TryAdd(entry.Index, entry))
                {
                    throw new ArgumentException($"Duplicate neighbor index: {entry.Index}.", nameof(neighbors));
                }
            }

            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                if (!byIndex.TryGetValue(index, out var entry))
                {
                    throw new ArgumentException($"Missing neighbor index: {index}.", nameof(neighbors));
                }

                var expectedCoordinate = WorldGridIndex.ToCoordinate(index);
                if (WorldData.GetCell(index).Coordinate != expectedCoordinate)
                {
                    throw new ArgumentException(
                        $"Cell {index} does not match coordinate {expectedCoordinate}.",
                        nameof(worldData));
                }

                if (entry.LeftIndex != WorldGridIndex.GetLeftIndex(index) ||
                    entry.RightIndex != WorldGridIndex.GetRightIndex(index) ||
                    entry.UpIndex != WorldGridIndex.GetUpIndex(index) ||
                    entry.DownIndex != WorldGridIndex.GetDownIndex(index))
                {
                    throw new ArgumentException(
                        $"Neighbor topology does not match grid index {index}.",
                        nameof(neighbors));
                }
            }

            snapshot.Sort((left, right) => left.Index.CompareTo(right.Index));
            this.neighbors = new ReadOnlyCollection<SectorNeighborIndices>(snapshot);
        }

        public GeneratedWorldData WorldData { get; }
        public IReadOnlyList<SectorNeighborIndices> Neighbors => neighbors;

        public SectorNeighborIndices GetNeighbors(int index)
        {
            if (!TryGetNeighbors(index, out var entry))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return entry;
        }

        public SectorNeighborIndices GetNeighbors(SectorCoord coordinate)
        {
            return GetNeighbors(WorldGridIndex.ToIndex(coordinate));
        }

        public bool TryGetNeighbors(int index, out SectorNeighborIndices entry)
        {
            if (index < 0 || index >= neighbors.Count)
            {
                entry = null;
                return false;
            }

            entry = neighbors[index];
            return true;
        }
    }
}
