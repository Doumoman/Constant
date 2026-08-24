using System;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryResolveResult
    {
        private MoonpalaceBoundaryResolveResult(
            MoonpalaceBoundaryResolveIssue issue,
            MoonpalaceBoundaryResolvedCandidate resolvedCandidate)
        {
            Issue = issue;
            ResolvedCandidate = resolvedCandidate;
        }

        public MoonpalaceBoundaryResolveIssue Issue { get; }
        public MoonpalaceBoundaryResolvedCandidate ResolvedCandidate { get; }
        public bool IsSuccess =>
            Issue == MoonpalaceBoundaryResolveIssue.None && ResolvedCandidate != null;

        public static MoonpalaceBoundaryResolveResult Success(
            MoonpalaceBoundaryResolvedCandidate resolvedCandidate)
        {
            if (resolvedCandidate == null) throw new ArgumentNullException(nameof(resolvedCandidate));
            return new MoonpalaceBoundaryResolveResult(
                MoonpalaceBoundaryResolveIssue.None,
                resolvedCandidate);
        }

        public static MoonpalaceBoundaryResolveResult Failure(MoonpalaceBoundaryResolveIssue issue)
        {
            if (issue == MoonpalaceBoundaryResolveIssue.None ||
                !Enum.IsDefined(typeof(MoonpalaceBoundaryResolveIssue), issue))
            {
                throw new ArgumentOutOfRangeException(nameof(issue));
            }

            return new MoonpalaceBoundaryResolveResult(issue, null);
        }
    }
}
