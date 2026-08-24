using System;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceMandatoryBoundaryFilterPolicy
    {
        public const string RejectionPrioritySignature =
            "MandatoryRouteNotAllowed>ToolRequired";

        public static MoonpalaceMandatoryBoundaryFilterPolicy Default { get; } =
            new MoonpalaceMandatoryBoundaryFilterPolicy();

        public MoonpalaceMandatoryBoundaryFilterIssue GetRejectionReason(
            MoonpalaceBoundaryCandidateDefinition candidate,
            bool mandatoryRouteBoundary)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            if (!mandatoryRouteBoundary) return MoonpalaceMandatoryBoundaryFilterIssue.None;

            if (!candidate.MandatoryRouteAllowed)
            {
                return MoonpalaceMandatoryBoundaryFilterIssue.MandatoryRouteNotAllowed;
            }

            return candidate.ToolRequirement == MoonpalaceBoundaryToolRequirement.None
                ? MoonpalaceMandatoryBoundaryFilterIssue.None
                : MoonpalaceMandatoryBoundaryFilterIssue.ToolRequired;
        }
    }
}
