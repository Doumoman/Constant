using System;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkTraversalNode
    {
        public MicrochunkId MicrochunkId { get; }
        public MicrochunkLocalCoord LocalCoordinate { get; }
        public MicrochunkLocalCoord Coordinate => LocalCoordinate;

        public MicrochunkTraversalNode(
            MicrochunkId microchunkId,
            MicrochunkLocalCoord localCoordinate)
        {
            if (!microchunkId.IsValid)
            {
                throw new ArgumentException("A valid microchunk ID is required.", nameof(microchunkId));
            }

            MicrochunkId = microchunkId;
            LocalCoordinate = localCoordinate;
        }
    }
}
