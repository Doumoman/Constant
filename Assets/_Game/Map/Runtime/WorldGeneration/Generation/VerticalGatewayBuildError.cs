using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum VerticalGatewayBuildErrorCode
    {
        MissingInput,
        InvalidHorizontalBackbonePlan,
        InvalidRouteMaskLookup,
        InvalidSiteSnapshot,
        InvalidBiomePublication,
        PendingSegmentCountMismatch,
        GatewayPairCountMismatch,
        InvalidGatewayIdentity,
        InvalidAnchorOrientation,
        InvalidColumnAlignment,
        ForbiddenReservationIntrusion,
        WorldBoundsViolation,
        InvalidType4Junction,
        Type4ReservationIntrusion,
        UnsupportedSameRowGateway,
        ConflictResolutionAttempted,
        SourceMutationDetected
    }

    public sealed class VerticalGatewayBuildError : IComparable<VerticalGatewayBuildError>
    {
        public VerticalGatewayBuildError(VerticalGatewayBuildErrorCode code, string firstId, string secondId, int sectorIndex, string message)
        {
            FirstId = firstId ?? throw new ArgumentNullException(nameof(firstId));
            SecondId = secondId ?? throw new ArgumentNullException(nameof(secondId));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Code = code;
            SectorIndex = sectorIndex;
        }

        public VerticalGatewayBuildErrorCode Code { get; }
        public string FirstId { get; }
        public string SecondId { get; }
        public int SectorIndex { get; }
        public string Message { get; }

        public int CompareTo(VerticalGatewayBuildError other)
        {
            if (other == null) return 1;
            var result = Code.CompareTo(other.Code);
            if (result != 0) return result;
            result = string.Compare(FirstId, other.FirstId, StringComparison.Ordinal);
            if (result != 0) return result;
            result = string.Compare(SecondId, other.SecondId, StringComparison.Ordinal);
            if (result != 0) return result;
            result = SectorIndex.CompareTo(other.SectorIndex);
            return result != 0 ? result : string.Compare(Message, other.Message, StringComparison.Ordinal);
        }

        internal string Key => ((int)Code) + "\n" + FirstId + "\n" + SecondId + "\n" + SectorIndex + "\n" + Message;
    }
}
