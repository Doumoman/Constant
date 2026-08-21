using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum MandatoryRouteGraphBuildErrorCode
    {
        NullInput = 0,
        SourceIdentityMismatch = 1,
        InvalidCardinality = 2,
        UnresolvedConflict = 3,
        MinimumLoopCountNotMet = 4,
        OutOfBoundsConnection = 5,
        UnsupportedOpenMask = 6,
        InactiveRouteCell = 7,
        ReservedInteriorRouteCell = 8,
        DuplicateDirectedEdge = 9,
        BrokenReciprocity = 10,
        MissingTerminal = 11,
        UnreachableTerminal = 12,
        ArithmeticOverflow = 13
    }

    public sealed class MandatoryRouteGraphBuildError
    {
        public MandatoryRouteGraphBuildError(MandatoryRouteGraphBuildErrorCode code, string sourceId, int sectorIndex, string message)
        {
            if (!Enum.IsDefined(typeof(MandatoryRouteGraphBuildErrorCode), code)) throw new ArgumentOutOfRangeException(nameof(code));
            Code = code;
            SourceId = sourceId ?? string.Empty;
            SectorIndex = sectorIndex;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public MandatoryRouteGraphBuildErrorCode Code { get; }
        public string SourceId { get; }
        public int SectorIndex { get; }
        public string Message { get; }
    }
}
