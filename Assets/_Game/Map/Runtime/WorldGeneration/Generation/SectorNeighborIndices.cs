using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SectorNeighborIndices
    {
        public const int NoNeighbor = -1;

        public SectorNeighborIndices(
            int index,
            int leftIndex,
            int rightIndex,
            int upIndex,
            int downIndex)
        {
            ValidateIndex(index, nameof(index));
            ValidateNeighbor(leftIndex, nameof(leftIndex));
            ValidateNeighbor(rightIndex, nameof(rightIndex));
            ValidateNeighbor(upIndex, nameof(upIndex));
            ValidateNeighbor(downIndex, nameof(downIndex));

            var neighbors = new[] { leftIndex, rightIndex, upIndex, downIndex };
            for (var first = 0; first < neighbors.Length; first++)
            {
                if (neighbors[first] == NoNeighbor)
                {
                    continue;
                }

                if (neighbors[first] == index)
                {
                    throw new ArgumentException("A sector cannot be its own neighbor.");
                }

                for (var second = first + 1; second < neighbors.Length; second++)
                {
                    if (neighbors[first] == neighbors[second])
                    {
                        throw new ArgumentException("Valid neighbor indices must be unique.");
                    }
                }
            }

            Index = index;
            LeftIndex = leftIndex;
            RightIndex = rightIndex;
            UpIndex = upIndex;
            DownIndex = downIndex;
            ValidNeighborCount = CountValid(neighbors);
        }

        public int Index { get; }
        public int LeftIndex { get; }
        public int RightIndex { get; }
        public int UpIndex { get; }
        public int DownIndex { get; }
        public int ValidNeighborCount { get; }

        private static void ValidateIndex(int index, string parameterName)
        {
            if (index < 0 || index >= WorldGenConstants.SectorCount)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private static void ValidateNeighbor(int index, string parameterName)
        {
            if (index != NoNeighbor && (index < 0 || index >= WorldGenConstants.SectorCount))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private static int CountValid(int[] neighbors)
        {
            var count = 0;
            foreach (var neighbor in neighbors)
            {
                if (neighbor != NoNeighbor)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
