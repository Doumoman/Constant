using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Boundaries
{
    [Category("MAP08_12")]
    public sealed class MoonpalaceBoundaryCoverageReportTests
    {
        private BoundaryCoverageAuthoringEvidence evidence;
        private MoonpalaceBoundaryCoverageValidator validator;

        public static IEnumerable<TestCaseData> ReportCases
        {
            get
            {
                for (var caseId = 0; caseId < 300; caseId++)
                {
                    yield return new TestCaseData(caseId)
                        .SetName("MoonpalaceBoundaryCoverageReport_" + caseId.ToString("D3"));
                }
            }
        }

        [OneTimeSetUp]
        public void LoadAuthoringEvidence()
        {
            evidence = BoundaryCoverageAuthoringHarness.GetOrCreate();
            validator = new MoonpalaceBoundaryCoverageValidator();
        }

        [TestCaseSource(nameof(ReportCases))]
        public void MoonpalaceBoundaryCoverageReportContract(int caseId)
        {
            var report = evidence.Report;
            var canonical = MoonpalaceBoundaryCoverageRequirement.Canonical;
            var requirement = canonical[caseId % canonical.Count];
            var pairReport = report.GetPairReport(requirement.PairRuleId);

            switch (caseId % 20)
            {
                case 0:
                    Assert.That(report.Accepted, Is.True, JoinIssues(report));
                    Assert.That(report.IssueList, Is.SameAs(report.Issues));
                    break;
                case 1:
                    Assert.That(report.PairReports.Select(value => value.PairRuleId),
                        Is.EqualTo(canonical.Select(value => value.PairRuleId)));
                    Assert.That(report.PairReportCount, Is.EqualTo(canonical.Count));
                    break;
                case 2:
                    Assert.That(new[]
                    {
                        report.CandidateCountTotal,
                        report.MicrochunkCountTotal,
                        report.TileRowCountTotal,
                        report.SocketRowCountTotal,
                    }, Is.EqualTo(new[] { 31, 31, 2976, 62 }));
                    break;
                case 3:
                    Assert.That(pairReport.CandidateCount, Is.EqualTo(requirement.ExpectedCandidateCount));
                    Assert.That(pairReport.MicrochunkCount, Is.EqualTo(requirement.ExpectedMicrochunkCount));
                    Assert.That(pairReport.TileRowCount, Is.EqualTo(requirement.ExpectedTileRowCount));
                    Assert.That(pairReport.SocketRowCount, Is.EqualTo(requirement.ExpectedSocketRowCount));
                    break;
                case 4:
                    Assert.That(report.OrientationCoverage.Count, Is.EqualTo(12));
                    Assert.That(report.OrientationCoverage.Keys.All(value =>
                        value.EndsWith("|HORIZONTAL", StringComparison.Ordinal) ||
                        value.EndsWith("|VERTICAL", StringComparison.Ordinal)), Is.True);
                    break;
                case 5:
                    Assert.That(report.ProfileCoverage.Count, Is.EqualTo(17));
                    Assert.That(pairReport.ProfileCoverage.Keys, Is.EquivalentTo(requirement.AllowedProfileIds));
                    break;
                case 6:
                    Assert.That(report.StableDigest, Does.Match("^[0-9a-f]{64}$"));
                    Assert.That(report.StableDigest, Is.EqualTo(report.StableDigest.ToLowerInvariant()));
                    break;
                case 7:
                    Assert.That(pairReport.StableDigest, Does.Match("^[0-9a-f]{64}$"));
                    Assert.That(pairReport.Accepted, Is.True);
                    break;
                case 8:
                    Assert.That(report.GetPairReport(requirement.PairRuleId), Is.SameAs(pairReport));
                    Assert.That(pairReport.Requirement, Is.SameAs(requirement));
                    break;
                case 9:
                    Assert.Throws<KeyNotFoundException>(() => report.GetPairReport("PAIR_UNKNOWN"));
                    Assert.Throws<ArgumentNullException>(() => report.GetPairReport(null));
                    break;
                case 10:
                    Assert.Throws<NotSupportedException>(() =>
                        ((IDictionary<string, int>)report.OrientationCoverage)["NEW"] = 1);
                    Assert.Throws<NotSupportedException>(() =>
                        ((IDictionary<string, int>)pairReport.ProfileCoverage)["NEW"] = 1);
                    break;
                case 11:
                    Assert.Throws<NotSupportedException>(() =>
                        ((IList<MoonpalaceBoundaryCoveragePairReport>)report.PairReports).Add(pairReport));
                    Assert.Throws<NotSupportedException>(() =>
                        ((IList<MoonpalaceBoundaryCoverageIssue>)report.Issues).Add(null));
                    break;
                case 12:
                    var reordered = validator.Validate(
                        evidence.Requirements.Reverse(), evidence.Candidates.Reverse(), evidence.SourceChain);
                    Assert.That(reordered.StableDigest, Is.EqualTo(report.StableDigest));
                    Assert.That(reordered.PairReports.Select(value => value.StableDigest),
                        Is.EqualTo(report.PairReports.Select(value => value.StableDigest)));
                    break;
                case 13:
                    var malformed = BoundaryCoverageTestMutation.ReplaceCandidate(
                        evidence.Candidates,
                        0,
                        BoundaryCoverageTestMutation.CopyCandidate(
                            evidence.Candidates[0],
                            tileCells: evidence.Candidates[0].TileCells.Take(1),
                            sockets: evidence.Candidates[0].Sockets.Take(1)));
                    var rejected = validator.Validate(evidence.Requirements, malformed, evidence.SourceChain);
                    Assert.That(rejected.Accepted, Is.False);
                    Assert.That(rejected.Issues.Zip(rejected.Issues.Skip(1),
                        (left, right) => left.CompareTo(right) <= 0), Is.All.True);
                    break;
                case 14:
                    var generated = validator.Validate(
                        evidence.Requirements,
                        evidence.Candidates,
                        new MoonpalaceBoundaryCoverageValidator.SourceChain(
                            evidence.SourceChain.AuthoringManifestSha256,
                            evidence.SourceChain.PreviousTaskSha256,
                            1,
                            0));
                    Assert.That(generated.GeneratedCsvCount, Is.EqualTo(1));
                    Assert.That(generated.StableDigest, Is.Not.EqualTo(report.StableDigest));
                    Assert.That(generated.Issues.Select(value => value.Code),
                        Does.Contain(MoonpalaceBoundaryCoverageIssueCode.GeneratedCsvPresent));
                    break;
                case 15:
                    Assert.That(report.AuthoringManifestSha256,
                        Is.EqualTo(MoonpalaceBoundaryCoverageValidator.ExpectedAuthoringManifestSha256));
                    Assert.That(report.GeneratedCsvCount, Is.Zero);
                    break;
                case 16:
                    Assert.That(pairReport.Requirement.ExpectedMatrix.Count,
                        Is.EqualTo(pairReport.CandidateCount));
                    Assert.That(pairReport.OrientationCoverage.Values.Sum(),
                        Is.EqualTo(pairReport.CandidateCount));
                    Assert.That(pairReport.ProfileCoverage.Values.Sum(),
                        Is.EqualTo(pairReport.CandidateCount));
                    break;
                case 17:
                    var mutableTiles = evidence.Candidates[0].TileCells.ToList();
                    var mutableSockets = evidence.Candidates[0].Sockets.ToList();
                    var candidateSnapshot = BoundaryCoverageTestMutation.CopyCandidate(
                        evidence.Candidates[0], tileCells: mutableTiles, sockets: mutableSockets);
                    mutableTiles.Clear();
                    mutableSockets.Clear();
                    Assert.That(candidateSnapshot.TileCells.Count, Is.EqualTo(96));
                    Assert.That(candidateSnapshot.Sockets.Count, Is.EqualTo(2));
                    break;
                case 18:
                    var mutableProfiles = requirement.AllowedProfileIds.ToList();
                    var mutableWeights = requirement.ProfileWeights.ToList();
                    var requirementSnapshot = new MoonpalaceBoundaryCoverageRequirement(
                        requirement.PairOrder,
                        requirement.PairRuleId,
                        requirement.BiomeAId,
                        requirement.BiomeBId,
                        mutableProfiles,
                        mutableWeights,
                        requirement.DefaultProfileId,
                        requirement.ExpectedCandidateCount,
                        requirement.ExpectedMicrochunkCount,
                        requirement.ExpectedTileRowCount,
                        requirement.ExpectedSocketRowCount,
                        requirement.Active);
                    mutableProfiles.Clear();
                    mutableWeights.Clear();
                    Assert.That(requirementSnapshot.AllowedProfileIds, Is.EquivalentTo(requirement.AllowedProfileIds));
                    Assert.That(requirementSnapshot.ProfileWeights, Is.EquivalentTo(requirement.ProfileWeights));
                    break;
                default:
                    Assert.Throws<ArgumentNullException>(() => validator.Validate(
                        null, evidence.Candidates, evidence.SourceChain));
                    Assert.Throws<ArgumentNullException>(() => validator.Validate(
                        evidence.Requirements, null, evidence.SourceChain));
                    Assert.Throws<ArgumentNullException>(() => validator.Validate(
                        evidence.Requirements, evidence.Candidates, null));
                    break;
            }
        }

        private static string JoinIssues(MoonpalaceBoundaryCoverageReport report)
        {
            return string.Join("\n", report.Issues.Select(value => value.ToString()));
        }
    }
}
