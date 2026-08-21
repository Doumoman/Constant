using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum MandatoryTerminalBuildErrorCode
    {
        MissingInput,
        InvalidSiteSnapshot,
        InvalidBiomePublication,
        WorldSeedMismatch,
        SourceSnapshotMismatch,
        ReservationCountMismatch,
        ReservationIdentityMismatch,
        EntryCountMismatch,
        EntryIdentityMismatch,
        EntryOutsideWorld,
        EntryExteriorReserved,
        DuplicateTerminalIdentity
    }

    public sealed class MandatoryTerminalBuildError
    {
        public MandatoryTerminalBuildError(
            MandatoryTerminalBuildErrorCode code,
            string firstId,
            string secondId,
            int sectorIndex,
            string message)
        {
            if (!Enum.IsDefined(typeof(MandatoryTerminalBuildErrorCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            Code = code;
            FirstId = firstId ?? string.Empty;
            SecondId = secondId ?? string.Empty;
            SectorIndex = sectorIndex;
            Message = message ?? string.Empty;
        }

        public MandatoryTerminalBuildErrorCode Code { get; }
        public string FirstId { get; }
        public string SecondId { get; }
        public int SectorIndex { get; }
        public string Message { get; }
    }
}
