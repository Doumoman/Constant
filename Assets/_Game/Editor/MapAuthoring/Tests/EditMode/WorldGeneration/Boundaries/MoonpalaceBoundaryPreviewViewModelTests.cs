using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.MapAuthoring.Boundaries;

namespace StarNight.MapAuthoring.Tests.WorldGeneration.Boundaries
{
    [Category("MAP08_13")]
    public sealed class MoonpalaceBoundaryPreviewViewModelTests
    {
        private const string ExpectedDigest =
            "f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68";
        private const string ExpectedManifest =
            "f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb";
        private static readonly string[] PairIds =
        {
            "PAIR_CRATER_ROOT",
            "PAIR_CRATER_MILL",
            "PAIR_CRATER_DOUGH",
            "PAIR_ROOT_MILL",
            "PAIR_ROOT_DOUGH",
            "PAIR_MILL_DOUGH",
        };
        private static readonly int[][] PairCounts =
        {
            new[] { 6, 6, 576, 12 },
            new[] { 4, 4, 384, 8 },
            new[] { 5, 5, 480, 10 },
            new[] { 6, 6, 576, 12 },
            new[] { 5, 5, 480, 10 },
            new[] { 5, 5, 480, 10 },
        };

        public static IEnumerable<TestCaseData> ContractCases
        {
            get
            {
                for (var caseId = 0; caseId < 420; caseId++)
                {
                    yield return new TestCaseData(caseId)
                        .SetName("MoonpalaceBoundaryPreviewViewModelContract_" + caseId.ToString("D3"));
                }
            }
        }

        [TestCaseSource(nameof(ContractCases))]
        public void MoonpalaceBoundaryPreviewViewModelContract(int caseId)
        {
            var variant = caseId / 21;
            switch (caseId % 21)
            {
                case 0: AssertApprovedReportAccepted(); break;
                case 1: AssertCanonicalSixPairRows(); break;
                case 2: AssertAggregateDigestPreserved(); break;
                case 3: AssertAggregateCounts(); break;
                case 4: AssertExactPairCounts(variant); break;
                case 5: AssertTransitions(variant); break;
                case 6: AssertOrientationAndProfiles(variant); break;
                case 7: AssertDeterministicFilter(variant); break;
                case 8: AssertTwelveByEightCells(variant); break;
                case 9: AssertOverlayProjection(variant); break;
                case 10: AssertReverseMirrorUsesRuntimePolicy(variant); break;
                case 11: AssertDisabledCandidatesRemainVisible(variant); break;
                case 12: AssertEmptyReportState(); break;
                case 13: AssertRejectedReportState(); break;
                case 14: AssertMissingPairState(); break;
                case 15: AssertMissingCandidateState(); break;
                case 16: AssertInvalidIndexState(variant); break;
                case 17: AssertUnknownProfileState(variant); break;
                case 18: AssertUnknownOrientationState(variant); break;
                case 19: AssertSummaryAndManifest(); break;
                case 20: AssertReadOnlyCommandSurface(); break;
            }
        }

        private static MoonpalaceBoundaryPreviewViewModel Approved()
        {
            var value = MoonpalaceBoundaryPreviewViewModel.LoadApprovedAuthoring();
            Assert.That(value.CurrentReport.HasCoverageReport, Is.True,
                string.Join("\n", value.CurrentReport.Issues.Select(issue => issue.ToString())));
            return value;
        }

        private static void SelectValid(MoonpalaceBoundaryPreviewViewModel value, int variant)
        {
            var pair = value.CoverageReport.PairReports[variant % PairIds.Length];
            value.SelectPair(pair.PairRuleId);
            value.SelectOrientation(variant % 2 == 0
                ? MoonpalaceBoundaryPreviewSelection.HorizontalToken
                : MoonpalaceBoundaryPreviewSelection.VerticalToken);
            value.SelectProfile(pair.Requirement.DefaultProfileId);
        }

        private static void AssertApprovedReportAccepted()
        {
            var value = Approved();
            Assert.That(value.Accepted, Is.True);
            Assert.That(value.CoverageReport.Issues, Is.Empty);
            Assert.That(value.CurrentReport.Issues, Is.Empty);
        }

        private static void AssertCanonicalSixPairRows()
        {
            Assert.That(Approved().CurrentReport.PairRows.Select(value => value.PairRuleId), Is.EqualTo(PairIds));
        }

        private static void AssertAggregateDigestPreserved()
        {
            Assert.That(Approved().CurrentReport.StableDigest, Is.EqualTo(ExpectedDigest));
        }

        private static void AssertAggregateCounts()
        {
            var report = Approved().CoverageReport;
            Assert.That(new[]
            {
                report.PairReportCount,
                report.CandidateCountTotal,
                report.MicrochunkCountTotal,
                report.TileRowCountTotal,
                report.SocketRowCountTotal,
                report.Issues.Count,
            }, Is.EqualTo(new[] { 6, 31, 31, 2976, 62, 0 }));
        }

        private static void AssertExactPairCounts(int variant)
        {
            var index = variant % PairIds.Length;
            var pair = Approved().CurrentReport.PairRows[index];
            Assert.That(new[]
            {
                pair.CandidateCount,
                pair.MicrochunkCount,
                pair.TileRowCount,
                pair.SocketRowCount,
            }, Is.EqualTo(PairCounts[index]));
        }

        private static void AssertTransitions(int variant)
        {
            var pair = Approved().CurrentReport.PairRows[variant % PairIds.Length];
            Assert.That(pair.ForwardTransition, Is.EqualTo(pair.BiomeAId + " -> " + pair.BiomeBId));
            Assert.That(pair.ReverseTransition, Is.EqualTo(pair.BiomeBId + " -> " + pair.BiomeAId));
        }

        private static void AssertOrientationAndProfiles(int variant)
        {
            var pair = Approved().CurrentReport.PairRows[variant % PairIds.Length];
            Assert.That(pair.OrientationDisplay, Does.Contain("H=").And.Contain("V="));
            Assert.That(pair.Profiles, Is.Not.Empty);
            Assert.That(pair.RouteRequirement, Is.EqualTo("TYPE_1 / MANDATORY / TOOL_NONE"));
            Assert.That(pair.EdgeSignatureDisplay, Does.Contain("EDGE_H_MID_WALK").And.Contain("EDGE_V_CENTER_CLIMB"));
        }

        private static void AssertDeterministicFilter(int variant)
        {
            var value = Approved();
            SelectValid(value, variant);
            Assert.That(value.CurrentReport.CandidateRows.Count(value2 => value2.Enabled), Is.EqualTo(1));
            Assert.That(value.CurrentReport.SelectedCandidate, Is.Not.Null);
            Assert.That(value.CurrentReport.SelectedCandidate.ProfileId, Is.EqualTo(value.Selection.ProfileId));
            Assert.That(value.CurrentReport.SelectedCandidate.OrientationToken, Is.EqualTo(value.Selection.OrientationToken));
        }

        private static void AssertTwelveByEightCells(int variant)
        {
            var value = Approved();
            SelectValid(value, variant);
            Assert.That(value.CurrentReport.Cells.Count, Is.EqualTo(96));
            Assert.That(value.CurrentReport.Cells.Select(cell => cell.RowMajorIndex).Distinct().Count(), Is.EqualTo(96));
            Assert.That(value.CurrentReport.Cells.Min(cell => cell.X), Is.Zero);
            Assert.That(value.CurrentReport.Cells.Max(cell => cell.X), Is.EqualTo(11));
            Assert.That(value.CurrentReport.Cells.Min(cell => cell.Y), Is.Zero);
            Assert.That(value.CurrentReport.Cells.Max(cell => cell.Y), Is.EqualTo(7));
        }

        private static void AssertOverlayProjection(int variant)
        {
            var value = Approved();
            SelectValid(value, variant);
            Assert.That(value.CurrentReport.Cells.Any(cell => cell.ShowForeground || cell.ShowBackground), Is.True);
            Assert.That(value.CurrentReport.Cells.Any(cell => cell.ShowRoute), Is.True);
            Assert.That(value.CurrentReport.Cells.Any(cell => cell.ShowSocket), Is.True);
            value.SetOverlay(MoonpalaceBoundaryPreviewOverlayToggle.Foreground, false);
            Assert.That(value.CurrentReport.Cells.All(cell => !cell.ShowForeground), Is.True);
        }

        private static void AssertReverseMirrorUsesRuntimePolicy(int variant)
        {
            var value = Approved();
            SelectValid(value, variant);
            value.SelectDirection(MoonpalaceBoundaryRequestDirection.Reverse);
            Assert.That(
                value.CurrentReport.SelectedCandidate.MirrorState,
                Is.EqualTo(value.Selection.OrientationToken == MoonpalaceBoundaryPreviewSelection.HorizontalToken
                    ? "MIRROR_X"
                    : "MIRROR_Y"));
        }

        private static void AssertDisabledCandidatesRemainVisible(int variant)
        {
            var value = Approved();
            SelectValid(value, variant);
            Assert.That(value.CurrentReport.CandidateRows.Count, Is.EqualTo(
                value.CurrentReport.PairRows.Single(pair => pair.PairRuleId == value.Selection.PairRuleId).CandidateCount));
            Assert.That(value.CurrentReport.CandidateRows.Any(candidate => !candidate.Enabled), Is.True);
            Assert.That(value.CurrentReport.CandidateRows.Where(candidate => !candidate.Enabled)
                .All(candidate => !string.IsNullOrEmpty(candidate.DisabledReason)), Is.True);
        }

        private static void AssertEmptyReportState()
        {
            var value = new MoonpalaceBoundaryPreviewViewModel(
                null, Array.Empty<MoonpalaceBoundaryCoverageCandidateEvidence>());
            Assert.That(value.CurrentReport.Issues.Select(issue => issue.Code), Does.Contain("REPORT_NOT_AVAILABLE"));
        }

        private static void AssertRejectedReportState()
        {
            var approved = Approved();
            var rejected = new MoonpalaceBoundaryCoverageValidator().Validate(
                MoonpalaceBoundaryCoverageRequirement.Canonical,
                approved.SourceCandidates,
                new MoonpalaceBoundaryCoverageValidator.SourceChain(
                    MoonpalaceBoundaryCoverageValidator.ExpectedAuthoringManifestSha256,
                    MoonpalaceBoundaryCoverageValidator.ExpectedPreviousTaskSha256,
                    1,
                    0));
            var value = new MoonpalaceBoundaryPreviewViewModel(rejected, approved.SourceCandidates);
            Assert.That(value.CurrentReport.Issues.Select(issue => issue.Code), Does.Contain("REPORT_REJECTED"));
        }

        private static void AssertMissingPairState()
        {
            var value = Approved();
            value.SelectPair("PAIR_MISSING");
            Assert.That(value.CurrentReport.Issues.Select(issue => issue.Code), Does.Contain("PAIR_NOT_FOUND"));
        }

        private static void AssertMissingCandidateState()
        {
            var approved = Approved();
            var value = new MoonpalaceBoundaryPreviewViewModel(
                approved.CoverageReport, Array.Empty<MoonpalaceBoundaryCoverageCandidateEvidence>());
            Assert.That(value.CurrentReport.Issues.Select(issue => issue.Code),
                Does.Contain("CANDIDATE_EVIDENCE_MISSING"));
        }

        private static void AssertInvalidIndexState(int variant)
        {
            var value = Approved();
            SelectValid(value, variant);
            Assert.That(value.SelectCandidateIndex(999), Is.False);
            Assert.That(value.CurrentReport.Issues.Select(issue => issue.Code),
                Does.Contain("CANDIDATE_INDEX_INVALID"));
        }

        private static void AssertUnknownProfileState(int variant)
        {
            var value = Approved();
            value.SelectPair(PairIds[variant % PairIds.Length]);
            value.SelectProfile("BOUND_UNKNOWN");
            Assert.That(value.CurrentReport.Issues.Select(issue => issue.Code), Does.Contain("PROFILE_UNKNOWN"));
        }

        private static void AssertUnknownOrientationState(int variant)
        {
            var value = Approved();
            value.SelectPair(PairIds[variant % PairIds.Length]);
            value.SelectOrientation("DIAGONAL");
            Assert.That(value.CurrentReport.Issues.Select(issue => issue.Code), Does.Contain("ORIENTATION_UNKNOWN"));
        }

        private static void AssertSummaryAndManifest()
        {
            var report = Approved().CurrentReport;
            Assert.That(report.AuthoringManifestSha256, Is.EqualTo(ExpectedManifest));
            Assert.That(report.Summary, Does.Contain(ExpectedDigest));
            Assert.That(report.Summary, Does.Contain("31/31/2976/62"));
            Assert.That(report.Summary, Does.Contain("Issues: 0"));
        }

        private static void AssertReadOnlyCommandSurface()
        {
            var methodNames = typeof(MoonpalaceBoundaryPreviewViewModel)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Select(method => method.Name)
                .ToArray();
            Assert.That(methodNames.Any(name =>
                name.StartsWith("Save", StringComparison.Ordinal) ||
                name.StartsWith("Write", StringComparison.Ordinal) ||
                name.StartsWith("Export", StringComparison.Ordinal)), Is.False);
            Assert.That(Approved().CoverageReport.GeneratedCsvCount, Is.Zero);
        }
    }
}
