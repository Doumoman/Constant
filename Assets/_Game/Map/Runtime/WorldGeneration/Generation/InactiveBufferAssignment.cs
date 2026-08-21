using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class InactiveBufferAssignment
    {
        private readonly IReadOnlyList<int> protectedNeighborSectorIndices;
        private readonly IReadOnlyList<int> inactiveNeighborSectorIndices;

        internal InactiveBufferAssignment(
            int sectorIndex,
            SectorCoord coord,
            GeneratedSectorRole role,
            InactiveBufferKind kind,
            IEnumerable<int> sourceProtectedNeighborSectorIndices,
            IEnumerable<int> sourceInactiveNeighborSectorIndices,
            bool touchesWorldEdge)
        {
            if (sectorIndex < 0 || sectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (coord != WorldGridIndex.ToCoordinate(sectorIndex))
                throw new ArgumentException("Sector index and coordinate must match.", nameof(coord));
            if (role != GeneratedSectorRole.InactiveBuffer)
                throw new ArgumentException("Inactive assignments must project InactiveBuffer.", nameof(role));
            if (!Enum.IsDefined(typeof(InactiveBufferKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (sourceProtectedNeighborSectorIndices == null)
                throw new ArgumentNullException(nameof(sourceProtectedNeighborSectorIndices));
            if (sourceInactiveNeighborSectorIndices == null)
                throw new ArgumentNullException(nameof(sourceInactiveNeighborSectorIndices));

            var protectedValues = FreezeNeighbors(
                sectorIndex, sourceProtectedNeighborSectorIndices,
                nameof(sourceProtectedNeighborSectorIndices));
            var inactiveValues = FreezeNeighbors(
                sectorIndex, sourceInactiveNeighborSectorIndices,
                nameof(sourceInactiveNeighborSectorIndices));
            var all = new HashSet<int>(protectedValues);
            foreach (var value in inactiveValues)
            {
                if (!all.Add(value))
                    throw new ArgumentException("Protected and inactive neighbor sets must be disjoint.");
            }

            if ((protectedValues.Count > 0) != (kind == InactiveBufferKind.DecorativeBoundary))
                throw new ArgumentException("Classification must match protected cardinal adjacency.", nameof(kind));
            var expectedWorldEdge = coord.X == 0 || coord.X == WorldGenConstants.SectorColumns - 1 ||
                                    coord.Y == 0 || coord.Y == WorldGenConstants.SectorRows - 1;
            if (touchesWorldEdge != expectedWorldEdge)
                throw new ArgumentException("World-edge classification must match the coordinate.", nameof(touchesWorldEdge));

            SectorIndex = sectorIndex;
            Coord = coord;
            Role = role;
            Kind = kind;
            protectedNeighborSectorIndices = protectedValues;
            inactiveNeighborSectorIndices = inactiveValues;
            TouchesWorldEdge = touchesWorldEdge;
        }

        public int SectorIndex { get; }
        public SectorCoord Coord { get; }
        public GeneratedSectorRole Role { get; }
        public InactiveBufferKind Kind { get; }
        public IReadOnlyList<int> ProtectedNeighborSectorIndices => protectedNeighborSectorIndices;
        public IReadOnlyList<int> InactiveNeighborSectorIndices => inactiveNeighborSectorIndices;
        public bool TouchesWorldEdge { get; }

        private static IReadOnlyList<int> FreezeNeighbors(
            int sectorIndex,
            IEnumerable<int> source,
            string parameterName)
        {
            var values = new List<int>(source);
            var unique = new HashSet<int>();
            var previousRank = -1;
            var coord = WorldGridIndex.ToCoordinate(sectorIndex);
            foreach (var value in values)
            {
                if (value < 0 || value >= WorldGenConstants.SectorCount)
                    throw new ArgumentOutOfRangeException(parameterName);
                if (!unique.Add(value))
                    throw new ArgumentException("Neighbor indices must be unique.", parameterName);
                var neighbor = WorldGridIndex.ToCoordinate(value);
                var dx = neighbor.X - coord.X;
                var dy = neighbor.Y - coord.Y;
                var rank = DirectionRank(dx, dy);
                if (rank < 0 || rank <= previousRank)
                    throw new ArgumentException("Neighbors must be cardinal and ordered L, R, U, D.", parameterName);
                previousRank = rank;
            }
            return new ReadOnlyCollection<int>(values);
        }

        private static int DirectionRank(int dx, int dy)
        {
            if (dx == -1 && dy == 0) return 0;
            if (dx == 1 && dy == 0) return 1;
            if (dx == 0 && dy == 1) return 2;
            if (dx == 0 && dy == -1) return 3;
            return -1;
        }
    }
}
