using System;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.MapAuthoring.Boundaries
{
    public sealed class MoonpalaceBoundaryPreviewSelection
    {
        public const string HorizontalToken = "HORIZONTAL";
        public const string VerticalToken = "VERTICAL";

        public MoonpalaceBoundaryPreviewSelection(
            string pairRuleId,
            string orientationToken,
            string profileId,
            int candidateIndex,
            MoonpalaceBoundaryRequestDirection direction)
        {
            PairRuleId = pairRuleId ?? string.Empty;
            OrientationToken = orientationToken ?? string.Empty;
            ProfileId = profileId ?? string.Empty;
            CandidateIndex = candidateIndex;
            Direction = direction;
        }

        public string PairRuleId { get; }
        public string OrientationToken { get; }
        public string ProfileId { get; }
        public int CandidateIndex { get; }
        public MoonpalaceBoundaryRequestDirection Direction { get; }

        public bool TryGetOrientation(out MoonpalaceBoundaryOrientation orientation)
        {
            if (string.Equals(OrientationToken, HorizontalToken, StringComparison.Ordinal))
            {
                orientation = MoonpalaceBoundaryOrientation.Horizontal;
                return true;
            }
            if (string.Equals(OrientationToken, VerticalToken, StringComparison.Ordinal))
            {
                orientation = MoonpalaceBoundaryOrientation.Vertical;
                return true;
            }

            orientation = default;
            return false;
        }

        public MoonpalaceBoundaryPreviewSelection WithPair(string pairRuleId)
        {
            return new MoonpalaceBoundaryPreviewSelection(
                pairRuleId, OrientationToken, string.Empty, -1, Direction);
        }

        public MoonpalaceBoundaryPreviewSelection WithOrientation(string orientationToken)
        {
            return new MoonpalaceBoundaryPreviewSelection(
                PairRuleId, orientationToken, ProfileId, -1, Direction);
        }

        public MoonpalaceBoundaryPreviewSelection WithProfile(string profileId)
        {
            return new MoonpalaceBoundaryPreviewSelection(
                PairRuleId, OrientationToken, profileId, -1, Direction);
        }

        public MoonpalaceBoundaryPreviewSelection WithCandidateIndex(int candidateIndex)
        {
            return new MoonpalaceBoundaryPreviewSelection(
                PairRuleId, OrientationToken, ProfileId, candidateIndex, Direction);
        }

        public MoonpalaceBoundaryPreviewSelection WithDirection(
            MoonpalaceBoundaryRequestDirection direction)
        {
            return new MoonpalaceBoundaryPreviewSelection(
                PairRuleId, OrientationToken, ProfileId, CandidateIndex, direction);
        }

        public static string ToToken(MoonpalaceBoundaryOrientation orientation)
        {
            if (orientation == MoonpalaceBoundaryOrientation.Horizontal) return HorizontalToken;
            if (orientation == MoonpalaceBoundaryOrientation.Vertical) return VerticalToken;
            return "UNKNOWN";
        }
    }
}
