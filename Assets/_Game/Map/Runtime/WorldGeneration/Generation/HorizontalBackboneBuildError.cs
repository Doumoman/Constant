using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum HorizontalBackboneBuildErrorCode
    {
        MissingInput,
        InvalidConnectorTree,
        InvalidRouteMaskLookup,
        InvalidSiteSnapshot,
        InvalidBiomePublication,
        SegmentCountMismatch,
        InvalidSegmentIdentity,
        InvalidHorizontalRun,
        ForbiddenReservationIntrusion,
        WorldBoundsViolation,
        UnsupportedVerticalConnection,
        SourceMutationDetected
    }

    public sealed class HorizontalBackboneBuildError
    {
        public HorizontalBackboneBuildError(HorizontalBackboneBuildErrorCode code, string firstId, string secondId, int sectorIndex, string message)
        {
            if (!Enum.IsDefined(typeof(HorizontalBackboneBuildErrorCode), code)) throw new ArgumentOutOfRangeException(nameof(code));
            Code = code;
            FirstId = firstId ?? throw new ArgumentNullException(nameof(firstId));
            SecondId = secondId ?? throw new ArgumentNullException(nameof(secondId));
            SectorIndex = sectorIndex;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public HorizontalBackboneBuildErrorCode Code { get; }
        public string FirstId { get; }
        public string SecondId { get; }
        public int SectorIndex { get; }
        public string Message { get; }

        internal static int Compare(HorizontalBackboneBuildError left, HorizontalBackboneBuildError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = string.Compare(left.FirstId, right.FirstId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.SecondId, right.SecondId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            return value != 0 ? value : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }
    }
}
