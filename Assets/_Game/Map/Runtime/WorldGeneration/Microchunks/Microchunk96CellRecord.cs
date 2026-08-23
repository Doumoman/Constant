using System;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class Microchunk96CellRecord
    {
        public MicrochunkId MicrochunkId { get; }
        public int SourceOrdinal { get; }
        public int RawLocalX { get; }
        public int RawLocalY { get; }
        public int RawX => RawLocalX;
        public int RawY => RawLocalY;
        public MicrochunkTileCell NormalizedTileCell { get; }
        public MicrochunkTileCell TileCell => NormalizedTileCell;
        public bool HasNormalizedTileCell => NormalizedTileCell != null;

        public Microchunk96CellRecord(
            MicrochunkId microchunkId,
            int sourceOrdinal,
            int rawLocalX,
            int rawLocalY,
            MicrochunkTileCell normalizedTileCell = null)
        {
            if (!microchunkId.IsValid)
            {
                throw new ArgumentException("A valid microchunk ID is required.", nameof(microchunkId));
            }

            if (sourceOrdinal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceOrdinal));
            }

            MicrochunkId = microchunkId;
            SourceOrdinal = sourceOrdinal;
            RawLocalX = rawLocalX;
            RawLocalY = rawLocalY;
            NormalizedTileCell = normalizedTileCell;
        }
    }
}
