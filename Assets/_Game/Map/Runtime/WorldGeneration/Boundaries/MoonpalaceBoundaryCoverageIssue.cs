using System;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public enum MoonpalaceBoundaryCoverageIssueCode
    {
        MissingPair = 0,
        UnexpectedPair = 1,
        DuplicatePair = 2,
        InactivePair = 3,
        MissingOrientation = 4,
        MissingProfile = 5,
        UnexpectedProfile = 6,
        InvalidProfileOrientation = 7,
        MissingCandidate = 8,
        DuplicateCandidate = 9,
        InvalidCandidate = 10,
        MissingMicrochunk = 11,
        DuplicateMicrochunk = 12,
        InvalidTileCoverage = 13,
        MissingSocket = 14,
        InvalidSocket = 15,
        ToolRequired = 16,
        MissingWarningEvidence = 17,
        GeneratedCsvPresent = 18,
        AuthoringMutationDetected = 19,
        InvalidSourceChain = 20,
    }

    public sealed class MoonpalaceBoundaryCoverageIssue : IComparable<MoonpalaceBoundaryCoverageIssue>
    {
        public MoonpalaceBoundaryCoverageIssue(
            MoonpalaceBoundaryCoverageIssueCode code,
            int pairOrder,
            string pairRuleId,
            MoonpalaceBoundaryOrientation orientation,
            int profileOrder,
            string profileId,
            string candidateId,
            string microchunkId,
            string message)
        {
            Code = code;
            PairOrder = pairOrder;
            PairRuleId = pairRuleId ?? string.Empty;
            Orientation = orientation;
            ProfileOrder = profileOrder;
            ProfileId = profileId ?? string.Empty;
            CandidateId = candidateId ?? string.Empty;
            MicrochunkId = microchunkId ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public MoonpalaceBoundaryCoverageIssueCode Code { get; }
        public int PairOrder { get; }
        public string PairRuleId { get; }
        public MoonpalaceBoundaryOrientation Orientation { get; }
        public int ProfileOrder { get; }
        public string ProfileId { get; }
        public string CandidateId { get; }
        public string MicrochunkId { get; }
        public string Message { get; }

        public string StableKey => string.Join("|", new[]
        {
            PairOrder.ToString("D4"),
            OrientationOrder(Orientation).ToString("D2"),
            ProfileOrder.ToString("D4"),
            CandidateId,
            MicrochunkId,
            ((int)Code).ToString("D3"),
            PairRuleId,
            ProfileId,
            Message,
        });

        public int CompareTo(MoonpalaceBoundaryCoverageIssue other)
        {
            if (ReferenceEquals(other, null)) return 1;
            var comparison = PairOrder.CompareTo(other.PairOrder);
            if (comparison != 0) return comparison;
            comparison = OrientationOrder(Orientation).CompareTo(OrientationOrder(other.Orientation));
            if (comparison != 0) return comparison;
            comparison = ProfileOrder.CompareTo(other.ProfileOrder);
            if (comparison != 0) return comparison;
            comparison = string.Compare(CandidateId, other.CandidateId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(MicrochunkId, other.MicrochunkId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(PairRuleId, other.PairRuleId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(ProfileId, other.ProfileId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            return string.Compare(Message, other.Message, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return Code + ": " + Message + " [" + PairRuleId + "/" + CandidateId + "/" + MicrochunkId + "]";
        }

        private static int OrientationOrder(MoonpalaceBoundaryOrientation orientation)
        {
            if (orientation == MoonpalaceBoundaryOrientation.Horizontal) return 0;
            if (orientation == MoonpalaceBoundaryOrientation.Vertical) return 1;
            return 2;
        }
    }
}
