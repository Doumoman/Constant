using System;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class Microchunk96CellValidationViolation
    {
        public const string MissingCellRecordReason = "MISSING_CELL_RECORD";
        public const string DuplicateCellCoordinateReason = "DUPLICATE_CELL_COORDINATE";
        public const string CellCoordinateOutOfRangeReason = "CELL_COORDINATE_OUT_OF_RANGE";

        public MicrochunkId MicrochunkId { get; }
        public int? SourceOrdinal { get; }
        public int? RawLocalX { get; }
        public int? RawLocalY { get; }
        public int? RawX => RawLocalX;
        public int? RawY => RawLocalY;
        public MicrochunkLocalCoord? NormalizedLocalCoordinate { get; }
        public MicrochunkLocalCoord? LocalCoordinate => NormalizedLocalCoordinate;
        public MicrochunkLocalCoord? Coordinate => NormalizedLocalCoordinate;
        public bool HasSourceOrdinal => SourceOrdinal.HasValue;
        public bool HasRawCoordinate => RawLocalX.HasValue && RawLocalY.HasValue;
        public bool HasNormalizedLocalCoordinate => NormalizedLocalCoordinate.HasValue;
        public string Reason { get; }

        public Microchunk96CellValidationViolation(
            MicrochunkId microchunkId,
            int? sourceOrdinal,
            int? rawLocalX,
            int? rawLocalY,
            MicrochunkLocalCoord? normalizedLocalCoordinate,
            string reason)
        {
            if (!microchunkId.IsValid)
            {
                throw new ArgumentException("A valid microchunk ID is required.", nameof(microchunkId));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("A stable violation reason is required.", nameof(reason));
            }

            MicrochunkId = microchunkId;
            SourceOrdinal = sourceOrdinal;
            RawLocalX = rawLocalX;
            RawLocalY = rawLocalY;
            NormalizedLocalCoordinate = normalizedLocalCoordinate;
            Reason = reason;
        }
    }
}
