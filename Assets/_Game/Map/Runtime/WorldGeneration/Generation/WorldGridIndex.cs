using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public static class WorldGridIndex
    {
        public static int ToIndex(SectorCoord coordinate)
        {
            if (!WorldCoordinateUtility.IsValid(coordinate))
            {
                throw new ArgumentOutOfRangeException(nameof(coordinate));
            }

            return coordinate.Y * WorldGenConstants.SectorColumns + coordinate.X;
        }

        public static SectorCoord ToCoordinate(int index)
        {
            ValidateIndex(index);
            return new SectorCoord(
                index % WorldGenConstants.SectorColumns,
                index / WorldGenConstants.SectorColumns);
        }

        public static int GetLeftIndex(int index)
        {
            var coordinate = ToCoordinate(index);
            return coordinate.X == 0
                ? SectorNeighborIndices.NoNeighbor
                : index - 1;
        }

        public static int GetRightIndex(int index)
        {
            var coordinate = ToCoordinate(index);
            return coordinate.X == WorldGenConstants.SectorColumns - 1
                ? SectorNeighborIndices.NoNeighbor
                : index + 1;
        }

        public static int GetUpIndex(int index)
        {
            var coordinate = ToCoordinate(index);
            return coordinate.Y == WorldGenConstants.SectorRows - 1
                ? SectorNeighborIndices.NoNeighbor
                : index + WorldGenConstants.SectorColumns;
        }

        public static int GetDownIndex(int index)
        {
            var coordinate = ToCoordinate(index);
            return coordinate.Y == 0
                ? SectorNeighborIndices.NoNeighbor
                : index - WorldGenConstants.SectorColumns;
        }

        private static void ValidateIndex(int index)
        {
            if (index < 0 || index >= WorldGenConstants.SectorCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
