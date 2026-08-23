using System;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkSocketEdgeValidationViolation
    {
        public MicrochunkId MicrochunkId { get; }
        public string SocketId { get; }
        public MicrochunkSide Side { get; }
        public string BandId { get; }
        public string EdgeSignatureId { get; }
        public MicrochunkLocalCoord? Coordinate { get; }
        public bool HasCoordinate => Coordinate.HasValue;
        public string Reason { get; }

        public MicrochunkSocketEdgeValidationViolation(
            MicrochunkId microchunkId,
            string socketId,
            MicrochunkSide side,
            string bandId,
            string edgeSignatureId,
            MicrochunkLocalCoord? coordinate,
            string reason)
        {
            if (!microchunkId.IsValid)
            {
                throw new ArgumentException("A valid microchunk ID is required.", nameof(microchunkId));
            }

            if (string.IsNullOrWhiteSpace(socketId))
            {
                throw new ArgumentException("Socket ID is required.", nameof(socketId));
            }

            if (!Enum.IsDefined(typeof(MicrochunkSide), side))
            {
                throw new ArgumentOutOfRangeException(nameof(side));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("A stable violation reason is required.", nameof(reason));
            }

            MicrochunkId = microchunkId;
            SocketId = socketId;
            Side = side;
            BandId = bandId ?? string.Empty;
            EdgeSignatureId = edgeSignatureId ?? string.Empty;
            Coordinate = coordinate;
            Reason = reason;
        }
    }
}
