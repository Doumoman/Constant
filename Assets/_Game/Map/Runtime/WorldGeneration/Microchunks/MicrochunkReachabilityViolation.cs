using System;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkReachabilityViolation
    {
        public const string MandatorySocketPairUnreachableReason = "MANDATORY_SOCKET_PAIR_UNREACHABLE";
        public const string MandatorySocketEntryUnreachableReason = "MANDATORY_SOCKET_ENTRY_UNREACHABLE";
        public const string CellCoverageInvalidReason = "CELL_COVERAGE_INVALID";

        public MicrochunkId MicrochunkId { get; }
        public string SocketId { get; }
        public string PairedSocketId { get; }
        public MicrochunkLocalCoord? LocalCoordinate { get; }
        public MicrochunkLocalCoord? Coordinate => LocalCoordinate;
        public bool HasPairedSocketId => !string.IsNullOrEmpty(PairedSocketId);
        public bool HasLocalCoordinate => LocalCoordinate.HasValue;
        public string Reason { get; }

        public MicrochunkReachabilityViolation(
            MicrochunkId microchunkId,
            string socketId,
            string pairedSocketId,
            MicrochunkLocalCoord? localCoordinate,
            string reason)
        {
            if (!microchunkId.IsValid)
            {
                throw new ArgumentException("A valid microchunk ID is required.", nameof(microchunkId));
            }

            if (socketId == null) throw new ArgumentNullException(nameof(socketId));
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("A stable violation reason is required.", nameof(reason));
            }

            MicrochunkId = microchunkId;
            SocketId = socketId;
            PairedSocketId = pairedSocketId ?? string.Empty;
            LocalCoordinate = localCoordinate;
            Reason = reason;
        }
    }
}
