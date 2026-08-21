using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum MandatoryConnectorTreeBuildErrorCode
    {
        MissingInput,
        InvalidTerminalSet,
        InvalidRouteMaskLookup,
        TerminalCountMismatch,
        TerminalIdentityMismatch,
        CandidateEdgeCountMismatch,
        DuplicateEdgeIdentity,
        InvalidEdgeCost,
        TreeEdgeCountMismatch,
        DisconnectedTree,
        CyclicTree,
        MissingTerminalCoverage
    }

    public sealed class MandatoryConnectorTreeBuildError
    {
        public MandatoryConnectorTreeBuildError(MandatoryConnectorTreeBuildErrorCode code, string firstId, string secondId, int sectorIndex, string message)
        {
            if (code < MandatoryConnectorTreeBuildErrorCode.MissingInput || code > MandatoryConnectorTreeBuildErrorCode.MissingTerminalCoverage) throw new ArgumentOutOfRangeException(nameof(code));
            Code = code;
            FirstId = firstId ?? string.Empty;
            SecondId = secondId ?? string.Empty;
            SectorIndex = sectorIndex;
            Message = message ?? string.Empty;
        }

        public MandatoryConnectorTreeBuildErrorCode Code { get; }
        public string FirstId { get; }
        public string SecondId { get; }
        public int SectorIndex { get; }
        public string Message { get; }

        internal static int Compare(MandatoryConnectorTreeBuildError left, MandatoryConnectorTreeBuildError right)
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
