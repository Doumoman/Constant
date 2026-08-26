using System;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.MapAuthoring.Boundaries
{
    public enum MoonpalaceBoundaryPreviewIssueSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }

    public sealed class MoonpalaceBoundaryPreviewIssueView : IComparable<MoonpalaceBoundaryPreviewIssueView>
    {
        public MoonpalaceBoundaryPreviewIssueView(
            MoonpalaceBoundaryPreviewIssueSeverity severity,
            string code,
            string message,
            string pairRuleId = "",
            string candidateId = "",
            string microchunkId = "")
        {
            Severity = severity;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            PairRuleId = pairRuleId ?? string.Empty;
            CandidateId = candidateId ?? string.Empty;
            MicrochunkId = microchunkId ?? string.Empty;
        }

        public MoonpalaceBoundaryPreviewIssueSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public string PairRuleId { get; }
        public string CandidateId { get; }
        public string MicrochunkId { get; }
        public bool IsError => Severity == MoonpalaceBoundaryPreviewIssueSeverity.Error;

        public string StableKey => string.Join("|", new[]
        {
            ((int)Severity).ToString("D2"),
            PairRuleId,
            CandidateId,
            MicrochunkId,
            Code,
            Message,
        });

        public int CompareTo(MoonpalaceBoundaryPreviewIssueView other)
        {
            return other == null
                ? 1
                : string.Compare(StableKey, other.StableKey, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return Severity + ":" + Code + ": " + Message;
        }

        public static MoonpalaceBoundaryPreviewIssueView FromCoverageIssue(
            MoonpalaceBoundaryCoverageIssue issue)
        {
            if (issue == null) throw new ArgumentNullException(nameof(issue));
            return new MoonpalaceBoundaryPreviewIssueView(
                MoonpalaceBoundaryPreviewIssueSeverity.Error,
                issue.Code.ToString(),
                issue.Message,
                issue.PairRuleId,
                issue.CandidateId,
                issue.MicrochunkId);
        }
    }
}
