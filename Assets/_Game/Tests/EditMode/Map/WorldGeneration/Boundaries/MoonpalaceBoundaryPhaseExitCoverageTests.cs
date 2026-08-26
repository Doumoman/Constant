using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Boundaries
{
    [Category("MAP08_14")]
    [Category("MAP08_14_COVERAGE")]
    public sealed class MoonpalaceBoundaryPhaseExitCoverageTests
    {
        private const string ExpectedDigest =
            "f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68";

        private MoonpalaceBoundaryPhaseExitFixture fixture;

        public static IEnumerable<TestCaseData> CoverageCases
        {
            get
            {
                for (var caseId = 0; caseId < 300; caseId++)
                {
                    yield return new TestCaseData(caseId)
                        .SetName("MoonpalaceBoundaryPhaseExitCoverage_" + caseId.ToString("D3"));
                }
            }
        }

        [OneTimeSetUp]
        public void LoadApprovedEvidence()
        {
            fixture = MoonpalaceBoundaryPhaseExitFixture.GetOrCreate();
        }

        [TestCaseSource(nameof(CoverageCases))]
        public void MoonpalaceBoundaryPhaseExitCoverageContract(int caseId)
        {
            var report = fixture.Evidence.Report;
            var requirement = fixture.Evidence.Requirements[(caseId / 15) % 6];
            var pairReport = report.GetPairReport(requirement.PairRuleId);

            switch (caseId % 15)
            {
                case 0:
                    Assert.That(report.Accepted, Is.True, fixture.JoinIssues());
                    Assert.That(report.Issues.Count, Is.Zero);
                    break;
                case 1:
                    Assert.That(report.PairReportCount, Is.EqualTo(6));
                    Assert.That(report.CandidateCountTotal, Is.EqualTo(31));
                    Assert.That(report.MicrochunkCountTotal, Is.EqualTo(31));
                    Assert.That(report.TileRowCountTotal, Is.EqualTo(2976));
                    Assert.That(report.SocketRowCountTotal, Is.EqualTo(62));
                    break;
                case 2:
                    Assert.That(pairReport.Accepted, Is.True);
                    Assert.That(pairReport.CandidateCount, Is.EqualTo(requirement.ExpectedCandidateCount));
                    Assert.That(pairReport.MicrochunkCount, Is.EqualTo(requirement.ExpectedMicrochunkCount));
                    Assert.That(pairReport.TileRowCount, Is.EqualTo(requirement.ExpectedTileRowCount));
                    Assert.That(pairReport.SocketRowCount, Is.EqualTo(requirement.ExpectedSocketRowCount));
                    break;
                case 3:
                    Assert.That(pairReport.OrientationCoverage["HORIZONTAL"], Is.GreaterThan(0));
                    Assert.That(pairReport.OrientationCoverage["VERTICAL"], Is.GreaterThan(0));
                    Assert.That(pairReport.ProfileCoverage.Keys.OrderBy(value => value, StringComparer.Ordinal),
                        Is.EqualTo(requirement.AllowedProfileIds.OrderBy(value => value, StringComparer.Ordinal)));
                    break;
                case 4:
                    AssertCanonicalPairMatrix(report);
                    break;
                case 5:
                    AssertCompleteNinetySixCellEvidence();
                    break;
                case 6:
                    Assert.That(fixture.Evidence.Requirements.Sum(value => value.ExpectedCandidateCount),
                        Is.EqualTo(31));
                    Assert.That(fixture.Evidence.Requirements.SelectMany(value => value.ExpectedMatrix).Count(),
                        Is.EqualTo(31));
                    Assert.That(fixture.Evidence.Requirements.SelectMany(value => value.ExpectedMatrix),
                        Does.Not.Contain("BOUND_LAYER|HORIZONTAL"));
                    break;
                case 7:
                    Assert.That(fixture.Evidence.Candidates.All(value =>
                        value.Active && value.MicrochunkActive && value.Reversible && value.Weight > 0), Is.True);
                    Assert.That(fixture.Evidence.Candidates.All(value =>
                        value.MandatoryAllowed && value.RouteType == 1 && value.ToolRequirement == "NONE"), Is.True);
                    break;
                case 8:
                    AssertOrientationSpecificSignatures();
                    break;
                case 9:
                    AssertSocketTotalsAndMandatoryContract();
                    break;
                case 10:
                    AssertEnteringBiomeEvidenceForEveryCandidate();
                    break;
                case 11:
                    Assert.That(report.StableDigest, Is.EqualTo(ExpectedDigest));
                    Assert.That(report.AuthoringManifestSha256,
                        Is.EqualTo(MoonpalaceBoundaryCoverageValidator.ExpectedAuthoringManifestSha256));
                    break;
                case 12:
                    var reordered = new MoonpalaceBoundaryCoverageValidator().Validate(
                        fixture.Evidence.Requirements.Reverse(),
                        fixture.Evidence.Candidates.Reverse(),
                        fixture.Evidence.SourceChain);
                    Assert.That(reordered.Accepted, Is.True);
                    Assert.That(reordered.StableDigest, Is.EqualTo(ExpectedDigest));
                    break;
                case 13:
                    Assert.That(fixture.Evidence.SourceChain.GeneratedCsvCount, Is.Zero);
                    Assert.That(fixture.Evidence.SourceChain.AuthoringMutationCount, Is.Zero);
                    Assert.That(report.GeneratedCsvCount, Is.Zero);
                    Assert.That(fixture.Evidence.SourceChain.AuthoringManifestSha256,
                        Is.EqualTo(MoonpalaceBoundaryCoverageValidator.ExpectedAuthoringManifestSha256));
                    break;
                case 14:
                    Assert.That(report.PairReports.Select(value => value.PairRuleId), Is.EqualTo(new[]
                    {
                        "PAIR_CRATER_ROOT",
                        "PAIR_CRATER_MILL",
                        "PAIR_CRATER_DOUGH",
                        "PAIR_ROOT_MILL",
                        "PAIR_ROOT_DOUGH",
                        "PAIR_MILL_DOUGH",
                    }));
                    Assert.That(report.PairReports.Sum(value => value.Issues.Count), Is.Zero);
                    break;
                default:
                    Assert.Fail("Unexpected MAP08_14 coverage contract case.");
                    break;
            }
        }

        private static void AssertCanonicalPairMatrix(MoonpalaceBoundaryCoverageReport report)
        {
            var expected = new[]
            {
                new[] { 6, 6, 576, 12 },
                new[] { 4, 4, 384, 8 },
                new[] { 5, 5, 480, 10 },
                new[] { 6, 6, 576, 12 },
                new[] { 5, 5, 480, 10 },
                new[] { 5, 5, 480, 10 },
            };
            for (var index = 0; index < expected.Length; index++)
            {
                var pair = report.PairReports[index];
                Assert.That(new[]
                {
                    pair.CandidateCount,
                    pair.MicrochunkCount,
                    pair.TileRowCount,
                    pair.SocketRowCount,
                }, Is.EqualTo(expected[index]), pair.PairRuleId);
            }
        }

        private void AssertCompleteNinetySixCellEvidence()
        {
            foreach (var candidate in fixture.Evidence.Candidates)
            {
                Assert.That(candidate.WidthTiles, Is.EqualTo(12), candidate.CandidateId);
                Assert.That(candidate.HeightTiles, Is.EqualTo(8), candidate.CandidateId);
                Assert.That(candidate.TileDataComplete, Is.True, candidate.CandidateId);
                Assert.That(candidate.TileCells.Count, Is.EqualTo(96), candidate.CandidateId);
                Assert.That(candidate.TileCells.Select(value => value.CoordinateKey).Distinct().Count(),
                    Is.EqualTo(96), candidate.CandidateId);
                Assert.That(candidate.TileCells.All(value =>
                    value.LocalX >= 0 && value.LocalX < 12 && value.LocalY >= 0 && value.LocalY < 8),
                    Is.True, candidate.CandidateId);
            }
        }

        private void AssertOrientationSpecificSignatures()
        {
            foreach (var candidate in fixture.Evidence.Candidates)
            {
                var expected = candidate.Orientation == MoonpalaceBoundaryOrientation.Horizontal
                    ? MoonpalaceBoundaryCoverageValidator.HorizontalEdgeSignatureId
                    : MoonpalaceBoundaryCoverageValidator.VerticalEdgeSignatureId;
                Assert.That(candidate.EntryEdgeSignatureId, Is.EqualTo(expected), candidate.CandidateId);
                Assert.That(candidate.ExitEdgeSignatureId, Is.EqualTo(expected), candidate.CandidateId);
            }
        }

        private void AssertSocketTotalsAndMandatoryContract()
        {
            foreach (var candidate in fixture.Evidence.Candidates)
            {
                Assert.That(candidate.Sockets.Count, Is.EqualTo(2), candidate.CandidateId);
                Assert.That(candidate.Sockets.All(value =>
                    value.MandatoryAllowed && value.ToolRequirement == "NONE" &&
                    value.RouteLayer == "MANDATORY" && value.MinimumSafeTiles >= 2),
                    Is.True, candidate.CandidateId);
            }
        }

        private void AssertEnteringBiomeEvidenceForEveryCandidate()
        {
            foreach (var candidate in fixture.Evidence.Candidates)
            {
                Assert.That(fixture.CountEnteringBiomeEvidenceCategories(candidate, candidate.BiomeAId),
                    Is.EqualTo(2), candidate.CandidateId + " A");
                Assert.That(fixture.CountEnteringBiomeEvidenceCategories(candidate, candidate.BiomeBId),
                    Is.EqualTo(2), candidate.CandidateId + " B");
            }
        }
    }

    internal sealed class MoonpalaceBoundaryPhaseExitFixture
    {
        private static readonly object Sync = new object();
        private static MoonpalaceBoundaryPhaseExitFixture cached;

        private readonly IReadOnlyDictionary<string, MoonpalaceBoundaryCandidateDefinition> definitionsById;

        private MoonpalaceBoundaryPhaseExitFixture()
        {
            Evidence = BoundaryCoverageAuthoringHarness.GetOrCreate();
            var definitions = Evidence.Candidates.Select(CreateDefinition).ToArray();
            CandidateDefinitions = new ReadOnlyCollection<MoonpalaceBoundaryCandidateDefinition>(definitions);
            definitionsById = new ReadOnlyDictionary<string, MoonpalaceBoundaryCandidateDefinition>(
                definitions.ToDictionary(value => value.CandidateId, StringComparer.Ordinal));
            CandidateIndex = MoonpalaceBoundaryCandidateIndexer.Canonical.Build(definitions);
        }

        public BoundaryCoverageAuthoringEvidence Evidence { get; }
        public IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> CandidateDefinitions { get; }
        public MoonpalaceBoundaryCandidateIndex CandidateIndex { get; }

        public static MoonpalaceBoundaryPhaseExitFixture GetOrCreate()
        {
            lock (Sync)
            {
                if (cached == null) cached = new MoonpalaceBoundaryPhaseExitFixture();
                return cached;
            }
        }

        public MoonpalaceBoundaryCandidateDefinition GetDefinition(
            MoonpalaceBoundaryCoverageCandidateEvidence evidence)
        {
            return definitionsById[evidence.CandidateId];
        }

        public MoonpalaceBoundaryResolveRequest CreateRequest(
            MoonpalaceBoundaryCoverageCandidateEvidence evidence,
            bool reverse,
            ulong seed = 0UL)
        {
            var first = ParseBiome(evidence.BiomeAId);
            var second = ParseBiome(evidence.BiomeBId);
            return new MoonpalaceBoundaryResolveRequest(
                reverse ? second : first,
                reverse ? first : second,
                new MoonpalaceBoundaryProfileId(evidence.ProfileId),
                evidence.Orientation,
                new MoonpalaceBoundaryRouteRole("MANDATORY"),
                new MoonpalaceBoundaryEdgeSignature(evidence.EntryEdgeSignatureId),
                seed);
        }

        public MoonpalaceBoundaryResolveResult Resolve(
            MoonpalaceBoundaryCoverageCandidateEvidence evidence,
            bool reverse,
            ulong seed = 0UL)
        {
            return new MoonpalaceBoundaryChunkResolver().Resolve(
                CandidateIndex,
                CreateRequest(evidence, reverse, seed));
        }

        public MoonpalaceBoundaryWarningProbeResult ProbeWarning(
            MoonpalaceBoundaryCoverageCandidateEvidence evidence,
            bool reverse)
        {
            var request = CreateRequest(evidence, reverse);
            var definition = GetDefinition(evidence);
            var requirement = MoonpalaceBoundaryWarningRequirement.Create(request, definition);
            return new MoonpalaceBoundaryWarningProbe().Evaluate(
                new MoonpalaceBoundaryWarningProbeRequest(
                    request,
                    definition,
                    requirement,
                    2,
                    new[] { "Tile", "Background" },
                    request.ToBiome));
        }

        public int CountEnteringBiomeEvidenceCategories(
            MoonpalaceBoundaryCoverageCandidateEvidence candidate,
            string biomeId)
        {
            GetBiomeEvidence(biomeId, out var foreground, out var background);
            var tile = candidate.TileCells.Any(value => value.GroundCode == foreground);
            var decor = candidate.TileCells.Any(value => value.DecorBackCode == background);
            return (tile ? 1 : 0) + (decor ? 1 : 0);
        }

        public string JoinIssues()
        {
            return string.Join("\n", Evidence.Report.Issues.Select(value => value.ToString()));
        }

        private static MoonpalaceBoundaryCandidateDefinition CreateDefinition(
            MoonpalaceBoundaryCoverageCandidateEvidence value)
        {
            return new MoonpalaceBoundaryCandidateDefinition(
                value.CandidateId,
                new MoonpalaceBiomePair(ParseBiome(value.BiomeAId), ParseBiome(value.BiomeBId)),
                new MoonpalaceBoundaryProfileId(value.ProfileId),
                value.Orientation,
                new MoonpalaceBoundaryRouteRole("MANDATORY"),
                new MoonpalaceBoundaryEdgeSignature(value.EntryEdgeSignatureId),
                value.Weight,
                value.MandatoryAllowed,
                value.ToolRequirement,
                MoonpalaceBoundaryWarningMarker.Tile | MoonpalaceBoundaryWarningMarker.Background);
        }

        private static MoonpalaceBiomeId ParseBiome(string value)
        {
            switch (value)
            {
                case "BIO_MOON_CRATER": return MoonpalaceBiomeId.MoonCrater;
                case "BIO_CASSIA_ROOT": return MoonpalaceBiomeId.CassiaRoot;
                case "BIO_ABANDONED_MILL": return MoonpalaceBiomeId.AbandonedMill;
                case "BIO_MOON_DOUGH": return MoonpalaceBiomeId.MoonDough;
                default: throw new ArgumentException("Unknown Moonpalace biome token: " + value, nameof(value));
            }
        }

        private static void GetBiomeEvidence(string biomeId, out string foreground, out string background)
        {
            switch (biomeId)
            {
                case "BIO_MOON_CRATER":
                    foreground = "G_MOON_ROCK";
                    background = "DB_CRATER";
                    return;
                case "BIO_CASSIA_ROOT":
                    foreground = "G_CASSIA_WOOD";
                    background = "DB_ROOT";
                    return;
                case "BIO_ABANDONED_MILL":
                    foreground = "G_MILL_METAL";
                    background = "DB_MILL";
                    return;
                case "BIO_MOON_DOUGH":
                    foreground = "G_DOUGH_SOLID";
                    background = "DB_DOUGH";
                    return;
                default:
                    throw new ArgumentException("Unknown Moonpalace biome token: " + biomeId, nameof(biomeId));
            }
        }
    }
}
