using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Boundaries
{
    public sealed class MoonpalaceBoundaryPreviewPairView
    {
        public MoonpalaceBoundaryPreviewPairView(MoonpalaceBoundaryCoveragePairReport source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            var requirement = source.Requirement;
            PairRuleId = requirement.PairRuleId;
            BiomeAId = requirement.BiomeAId;
            BiomeBId = requirement.BiomeBId;
            ForwardTransition = BiomeAId + " -> " + BiomeBId;
            ReverseTransition = BiomeBId + " -> " + BiomeAId;
            Profiles = new ReadOnlyCollection<string>(requirement.AllowedProfileIds.ToArray());
            ProfileDisplay = string.Join(", ", Profiles);
            OrientationDisplay = "H=" + GetCoverage(source, "HORIZONTAL") +
                                 " / V=" + GetCoverage(source, "VERTICAL");
            RouteRequirement = "TYPE_1 / MANDATORY / TOOL_NONE";
            EdgeSignatureDisplay = "H=" + MoonpalaceBoundaryCoverageValidator.HorizontalEdgeSignatureId +
                                   " / V=" + MoonpalaceBoundaryCoverageValidator.VerticalEdgeSignatureId;
            CandidateCount = source.CandidateCount;
            MicrochunkCount = source.MicrochunkCount;
            TileRowCount = source.TileRowCount;
            SocketRowCount = source.SocketRowCount;
            CoverageState = source.Accepted ? "ACCEPTED" : "REJECTED";
            IssueCount = source.Issues.Count;
        }

        public MoonpalaceBoundaryCoveragePairReport Source { get; }
        public string PairRuleId { get; }
        public string BiomeAId { get; }
        public string BiomeBId { get; }
        public string ForwardTransition { get; }
        public string ReverseTransition { get; }
        public IReadOnlyList<string> Profiles { get; }
        public string ProfileDisplay { get; }
        public string OrientationDisplay { get; }
        public string RouteRequirement { get; }
        public string EdgeSignatureDisplay { get; }
        public int CandidateCount { get; }
        public int MicrochunkCount { get; }
        public int TileRowCount { get; }
        public int SocketRowCount { get; }
        public string CoverageState { get; }
        public int IssueCount { get; }
        public string CountDisplay => CandidateCount + "/" + MicrochunkCount + "/" +
                                      TileRowCount + "/" + SocketRowCount;

        private static int GetCoverage(MoonpalaceBoundaryCoveragePairReport report, string key)
        {
            return report.OrientationCoverage.TryGetValue(key, out var value) ? value : 0;
        }
    }

    public sealed class MoonpalaceBoundaryPreviewCandidateView
    {
        public MoonpalaceBoundaryPreviewCandidateView(
            int sourceIndex,
            MoonpalaceBoundaryCoverageCandidateEvidence source,
            MoonpalaceBoundaryPreviewPairView pair,
            bool enabled,
            string disabledReason,
            MoonpalaceBoundaryRequestDirection direction)
        {
            SourceIndex = sourceIndex;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Pair = pair ?? throw new ArgumentNullException(nameof(pair));
            Enabled = enabled;
            DisabledReason = disabledReason ?? string.Empty;
            Direction = direction;
            var transform = MoonpalaceBoundaryTransformPolicy.Create(direction, source.Orientation);
            TransformDirection = transform.Signature;
            MirrorState = MicrochunkTransformUtility.ToTransformToken(transform.Transform);
        }

        public int SourceIndex { get; }
        public MoonpalaceBoundaryCoverageCandidateEvidence Source { get; }
        public MoonpalaceBoundaryPreviewPairView Pair { get; }
        public bool Enabled { get; }
        public string DisabledReason { get; }
        public MoonpalaceBoundaryRequestDirection Direction { get; }
        public string CandidateId => Source.CandidateId;
        public string SourceMicrochunkId => Source.MicrochunkId;
        public string SourceCatalogRowId => Source.CandidateId;
        public string ProfileId => Source.ProfileId;
        public string OrientationToken => MoonpalaceBoundaryPreviewSelection.ToToken(Source.Orientation);
        public string ForwardTransition => Pair.ForwardTransition;
        public string ReverseTransition => Pair.ReverseTransition;
        public string SelectedTransition => Direction == MoonpalaceBoundaryRequestDirection.Forward
            ? ForwardTransition
            : ReverseTransition;
        public string TransformDirection { get; }
        public string MirrorState { get; }
        public string RouteRequirement => "TYPE_" + Source.RouteType + " / MANDATORY / " + Source.ToolRequirement;
        public string EdgeSignature => Source.EntryEdgeSignatureId + " -> " + Source.ExitEdgeSignatureId;
        public int TileRowCount => Source.TileCells.Count;
        public int SocketRowCount => Source.Sockets.Count;
    }

    public sealed class MoonpalaceBoundaryPreviewReport
    {
        internal MoonpalaceBoundaryPreviewReport(
            MoonpalaceBoundaryCoverageReport coverageReport,
            IEnumerable<MoonpalaceBoundaryPreviewPairView> pairRows,
            IEnumerable<MoonpalaceBoundaryPreviewCandidateView> candidateRows,
            MoonpalaceBoundaryPreviewCandidateView selectedCandidate,
            IEnumerable<MoonpalaceBoundaryPreviewCell> cells,
            IEnumerable<MoonpalaceBoundaryPreviewIssueView> issues,
            MoonpalaceBoundaryPreviewSelection selection,
            MoonpalaceBoundaryPreviewOverlayToggle overlays)
        {
            CoverageReport = coverageReport;
            PairRows = Freeze(pairRows);
            CandidateRows = Freeze(candidateRows);
            SelectedCandidate = selectedCandidate;
            Cells = Freeze(cells).OrderBy(value => value.RowMajorIndex).ToArray();
            Issues = Freeze(issues).OrderBy(value => value).ToArray();
            Selection = selection ?? throw new ArgumentNullException(nameof(selection));
            Overlays = overlays;
            Summary = BuildSummary();
        }

        public MoonpalaceBoundaryCoverageReport CoverageReport { get; }
        public bool HasCoverageReport => CoverageReport != null;
        public bool Accepted => CoverageReport != null && CoverageReport.Accepted;
        public IReadOnlyList<MoonpalaceBoundaryPreviewPairView> PairRows { get; }
        public IReadOnlyList<MoonpalaceBoundaryPreviewCandidateView> CandidateRows { get; }
        public MoonpalaceBoundaryPreviewCandidateView SelectedCandidate { get; }
        public IReadOnlyList<MoonpalaceBoundaryPreviewCell> Cells { get; }
        public IReadOnlyList<MoonpalaceBoundaryPreviewIssueView> Issues { get; }
        public MoonpalaceBoundaryPreviewSelection Selection { get; }
        public MoonpalaceBoundaryPreviewOverlayToggle Overlays { get; }
        public string StableDigest => CoverageReport == null ? string.Empty : CoverageReport.StableDigest;
        public string AuthoringManifestSha256 => CoverageReport == null
            ? string.Empty
            : CoverageReport.AuthoringManifestSha256;
        public string Summary { get; }

        private string BuildSummary()
        {
            if (CoverageReport == null) return "Moonpalace boundary coverage report: unavailable";
            var lines = new List<string>
            {
                "Moonpalace boundary coverage: " + (CoverageReport.Accepted ? "ACCEPTED" : "REJECTED"),
                "Digest: " + CoverageReport.StableDigest,
                "Manifest: " + CoverageReport.AuthoringManifestSha256,
                "Pairs: " + CoverageReport.PairReportCount,
                "Candidates/microchunks/tile rows/socket rows: " +
                CoverageReport.CandidateCountTotal + "/" + CoverageReport.MicrochunkCountTotal + "/" +
                CoverageReport.TileRowCountTotal + "/" + CoverageReport.SocketRowCountTotal,
                "Issues: " + CoverageReport.Issues.Count,
            };
            lines.AddRange(PairRows.Select(value => value.PairRuleId + " " + value.CountDisplay +
                                                    " " + value.CoverageState));
            return string.Join("\n", lines);
        }

        private static IReadOnlyList<T> Freeze<T>(IEnumerable<T> source)
        {
            return new ReadOnlyCollection<T>((source ?? Enumerable.Empty<T>()).ToArray());
        }
    }
}
