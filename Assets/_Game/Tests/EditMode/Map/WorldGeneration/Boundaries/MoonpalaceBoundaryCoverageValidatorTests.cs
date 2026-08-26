using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Boundaries
{
    [Category("MAP08_12")]
    public sealed class MoonpalaceBoundaryCoverageValidatorTests
    {
        private BoundaryCoverageAuthoringEvidence evidence;
        private MoonpalaceBoundaryCoverageValidator validator;

        public static IEnumerable<TestCaseData> ValidationCases
        {
            get
            {
                for (var caseId = 0; caseId < 420; caseId++)
                {
                    yield return new TestCaseData(caseId)
                        .SetName("MoonpalaceBoundaryCoverageValidator_" + caseId.ToString("D3"));
                }
            }
        }

        [OneTimeSetUp]
        public void LoadAuthoringEvidence()
        {
            evidence = BoundaryCoverageAuthoringHarness.GetOrCreate();
            validator = new MoonpalaceBoundaryCoverageValidator();
        }

        [TestCaseSource(nameof(ValidationCases))]
        public void MoonpalaceBoundaryCoverageValidatorContract(int caseId)
        {
            var pair = MoonpalaceBoundaryCoverageRequirement.Canonical[caseId % 6];
            var pairReport = evidence.Report.GetPairReport(pair.PairRuleId);
            var candidate = evidence.Candidates[caseId % evidence.Candidates.Count];

            switch (caseId % 30)
            {
                case 0:
                    Assert.That(evidence.Report.Accepted, Is.True, JoinIssues(evidence.Report));
                    Assert.That(evidence.Report.Issues, Is.Empty);
                    break;
                case 1:
                    Assert.That(evidence.Report.PairReportCount, Is.EqualTo(6));
                    Assert.That(evidence.Report.CandidateCountTotal, Is.EqualTo(31));
                    Assert.That(evidence.Report.MicrochunkCountTotal, Is.EqualTo(31));
                    Assert.That(evidence.Report.TileRowCountTotal, Is.EqualTo(2976));
                    Assert.That(evidence.Report.SocketRowCountTotal, Is.EqualTo(62));
                    break;
                case 2:
                    Assert.That(pairReport.Accepted, Is.True, string.Join("\n", pairReport.Issues));
                    Assert.That(pairReport.CandidateCount, Is.EqualTo(pair.ExpectedCandidateCount));
                    Assert.That(pairReport.MicrochunkCount, Is.EqualTo(pair.ExpectedMicrochunkCount));
                    Assert.That(pairReport.TileRowCount, Is.EqualTo(pair.ExpectedTileRowCount));
                    Assert.That(pairReport.SocketRowCount, Is.EqualTo(pair.ExpectedSocketRowCount));
                    break;
                case 3:
                    Assert.That(pairReport.OrientationCoverage["HORIZONTAL"], Is.GreaterThan(0));
                    Assert.That(pairReport.OrientationCoverage["VERTICAL"], Is.GreaterThan(0));
                    break;
                case 4:
                    Assert.That(pair.AllowedProfileIds.All(profile => pairReport.ProfileCoverage[profile] > 0), Is.True);
                    Assert.That(pairReport.ProfileCoverage.Keys, Is.EquivalentTo(pair.AllowedProfileIds));
                    break;
                case 5:
                    Assert.That(pair.ExpectedMatrix.Count, Is.EqualTo(pair.ExpectedCandidateCount));
                    Assert.That(pair.ExpectedMatrix, Does.Not.Contain("BOUND_LAYER|HORIZONTAL"));
                    break;
                case 6:
                    Assert.That(evidence.Candidates.Select(value => value.CandidateId), Is.Unique);
                    Assert.That(evidence.Candidates.All(value => value.Active && value.Reversible && value.Weight > 0), Is.True);
                    break;
                case 7:
                    Assert.That(evidence.Candidates.Select(value => value.MicrochunkId), Is.Unique);
                    Assert.That(evidence.Candidates.All(value => value.WidthTiles == 12 && value.HeightTiles == 8), Is.True);
                    break;
                case 8:
                    Assert.That(candidate.TileCells.Count, Is.EqualTo(96));
                    Assert.That(candidate.TileCells.Select(value => value.CoordinateKey), Is.Unique);
                    Assert.That(candidate.TileCells.All(value => value.LocalX >= 0 && value.LocalX < 12 &&
                                                                  value.LocalY >= 0 && value.LocalY < 8), Is.True);
                    break;
                case 9:
                    Assert.That(candidate.Sockets.Count, Is.EqualTo(2));
                    Assert.That(candidate.Sockets.All(value => value.MandatoryAllowed &&
                                                               value.ToolRequirement == "NONE" &&
                                                               value.RouteLayer == "MANDATORY" &&
                                                               value.MinimumSafeTiles >= 2), Is.True);
                    break;
                case 10:
                    Assert.That(candidate.TileCells.Any(value => value.MarkerCode == "M_ROUTE_MAIN"), Is.True);
                    Assert.That(candidate.TileCells.Any(value => value.MarkerCode == "M_SOCKET"), Is.True);
                    break;
                case 11:
                    var reordered = validator.Validate(
                        evidence.Requirements.Reverse(),
                        evidence.Candidates.Reverse(),
                        evidence.SourceChain);
                    Assert.That(reordered.StableDigest, Is.EqualTo(evidence.Report.StableDigest));
                    Assert.That(reordered.Accepted, Is.True);
                    break;
                case 12:
                    Assert.That(evidence.PreviousTaskSha256,
                        Is.EqualTo(MoonpalaceBoundaryCoverageValidator.ExpectedPreviousTaskSha256));
                    break;
                case 13:
                    Assert.That(evidence.Report.AuthoringManifestSha256,
                        Is.EqualTo(MoonpalaceBoundaryCoverageValidator.ExpectedAuthoringManifestSha256));
                    Assert.That(evidence.Report.GeneratedCsvCount, Is.Zero);
                    break;
                case 14:
                    var mutable = evidence.Candidates.ToList();
                    var snapshot = validator.Validate(evidence.Requirements, mutable, evidence.SourceChain);
                    mutable.Clear();
                    Assert.That(snapshot.CandidateCountTotal, Is.EqualTo(31));
                    Assert.That(snapshot.PairReports.Count, Is.EqualTo(6));
                    break;
                case 15:
                    AssertRejected(evidence.Requirements.Skip(1), evidence.Candidates, evidence.SourceChain,
                        MoonpalaceBoundaryCoverageIssueCode.MissingPair);
                    break;
                case 16:
                    var unexpected = BoundaryCoverageTestMutation.CopyRequirement(
                        evidence.Requirements[0], pairRuleId: "PAIR_UNKNOWN");
                    AssertRejected(evidence.Requirements.Concat(new[] { unexpected }), evidence.Candidates,
                        evidence.SourceChain, MoonpalaceBoundaryCoverageIssueCode.UnexpectedPair);
                    break;
                case 17:
                    AssertRejected(evidence.Requirements.Concat(new[] { evidence.Requirements[0] }), evidence.Candidates,
                        evidence.SourceChain, MoonpalaceBoundaryCoverageIssueCode.DuplicatePair);
                    var inactive = BoundaryCoverageTestMutation.ReplaceRequirement(
                        evidence.Requirements, 0,
                        BoundaryCoverageTestMutation.CopyRequirement(evidence.Requirements[0], active: false));
                    AssertRejected(inactive, evidence.Candidates, evidence.SourceChain,
                        MoonpalaceBoundaryCoverageIssueCode.InactivePair);
                    break;
                case 18:
                    var selfPair = BoundaryCoverageTestMutation.ReplaceRequirement(
                        evidence.Requirements, 0,
                        BoundaryCoverageTestMutation.CopyRequirement(evidence.Requirements[0],
                            biomeBId: evidence.Requirements[0].BiomeAId));
                    AssertRejected(selfPair, evidence.Candidates, evidence.SourceChain,
                        MoonpalaceBoundaryCoverageIssueCode.UnexpectedPair);
                    var unknownBiome = BoundaryCoverageTestMutation.ReplaceRequirement(
                        evidence.Requirements, 0,
                        BoundaryCoverageTestMutation.CopyRequirement(evidence.Requirements[0], biomeAId: "BIO_UNKNOWN"));
                    AssertRejected(unknownBiome, evidence.Candidates, evidence.SourceChain,
                        MoonpalaceBoundaryCoverageIssueCode.UnexpectedPair);
                    break;
                case 19:
                    var noHorizontal = evidence.Candidates.Where(value =>
                        value.PairRuleId != pair.PairRuleId ||
                        value.Orientation != MoonpalaceBoundaryOrientation.Horizontal);
                    AssertRejected(evidence.Requirements, noHorizontal, evidence.SourceChain,
                        MoonpalaceBoundaryCoverageIssueCode.MissingOrientation);
                    break;
                case 20:
                    var profiles = evidence.Requirements[0].AllowedProfileIds.Skip(1).ToArray();
                    var missingProfile = BoundaryCoverageTestMutation.ReplaceRequirement(
                        evidence.Requirements, 0,
                        BoundaryCoverageTestMutation.CopyRequirement(evidence.Requirements[0], allowedProfileIds: profiles));
                    AssertRejected(missingProfile, evidence.Candidates, evidence.SourceChain,
                        MoonpalaceBoundaryCoverageIssueCode.MissingProfile);
                    break;
                case 21:
                    var unexpectedProfile = BoundaryCoverageTestMutation.ReplaceCandidate(
                        evidence.Candidates, 0,
                        BoundaryCoverageTestMutation.CopyCandidate(evidence.Candidates[0], profileId: "BOUND_UNKNOWN"));
                    AssertRejected(evidence.Requirements, unexpectedProfile, evidence.SourceChain,
                        MoonpalaceBoundaryCoverageIssueCode.UnexpectedProfile);
                    var layerIndex = Enumerable.Range(0, evidence.Candidates.Count)
                        .First(index => evidence.Candidates[index].ProfileId == "BOUND_LAYER");
                    var horizontalLayer = BoundaryCoverageTestMutation.ReplaceCandidate(
                        evidence.Candidates, layerIndex,
                        BoundaryCoverageTestMutation.CopyCandidate(evidence.Candidates[layerIndex],
                            orientation: MoonpalaceBoundaryOrientation.Horizontal));
                    AssertRejected(evidence.Requirements, horizontalLayer, evidence.SourceChain,
                        MoonpalaceBoundaryCoverageIssueCode.InvalidProfileOrientation);
                    break;
                case 22:
                    AssertRejected(evidence.Requirements, evidence.Candidates.Skip(1), evidence.SourceChain,
                        MoonpalaceBoundaryCoverageIssueCode.MissingCandidate);
                    break;
                case 23:
                    AssertRejected(evidence.Requirements, evidence.Candidates.Concat(new[] { evidence.Candidates[0] }),
                        evidence.SourceChain, MoonpalaceBoundaryCoverageIssueCode.DuplicateCandidate);
                    break;
                case 24:
                    var missingMicrochunk = BoundaryCoverageTestMutation.ReplaceCandidate(
                        evidence.Candidates, 0,
                        BoundaryCoverageTestMutation.CopyCandidate(evidence.Candidates[0], microchunkId: string.Empty));
                    AssertRejected(evidence.Requirements, missingMicrochunk, evidence.SourceChain,
                        MoonpalaceBoundaryCoverageIssueCode.MissingMicrochunk);
                    var duplicateMicrochunk = BoundaryCoverageTestMutation.ReplaceCandidate(
                        evidence.Candidates, 1,
                        BoundaryCoverageTestMutation.CopyCandidate(evidence.Candidates[1],
                            microchunkId: evidence.Candidates[0].MicrochunkId));
                    AssertRejected(evidence.Requirements, duplicateMicrochunk, evidence.SourceChain,
                        MoonpalaceBoundaryCoverageIssueCode.DuplicateMicrochunk);
                    break;
                case 25:
                    var invalidTiles = BoundaryCoverageTestMutation.ReplaceCandidate(
                        evidence.Candidates, 0,
                        BoundaryCoverageTestMutation.CopyCandidate(evidence.Candidates[0],
                            tileCells: evidence.Candidates[0].TileCells.Skip(1)));
                    AssertRejected(evidence.Requirements, invalidTiles, evidence.SourceChain,
                        MoonpalaceBoundaryCoverageIssueCode.InvalidTileCoverage);
                    break;
                case 26:
                    var missingSocket = BoundaryCoverageTestMutation.ReplaceCandidate(
                        evidence.Candidates, 0,
                        BoundaryCoverageTestMutation.CopyCandidate(evidence.Candidates[0],
                            sockets: evidence.Candidates[0].Sockets.Take(1)));
                    AssertRejected(evidence.Requirements, missingSocket, evidence.SourceChain,
                        MoonpalaceBoundaryCoverageIssueCode.MissingSocket);
                    var badSockets = evidence.Candidates[0].Sockets.Select((value, index) =>
                        index == 0 ? BoundaryCoverageTestMutation.CopySocket(value, side: "X") : value).ToArray();
                    var invalidSocket = BoundaryCoverageTestMutation.ReplaceCandidate(
                        evidence.Candidates, 0,
                        BoundaryCoverageTestMutation.CopyCandidate(evidence.Candidates[0], sockets: badSockets));
                    AssertRejected(evidence.Requirements, invalidSocket, evidence.SourceChain,
                        MoonpalaceBoundaryCoverageIssueCode.InvalidSocket);
                    break;
                case 27:
                    var toolCandidate = BoundaryCoverageTestMutation.ReplaceCandidate(
                        evidence.Candidates, 0,
                        BoundaryCoverageTestMutation.CopyCandidate(evidence.Candidates[0], toolRequirement: "Pickaxe"));
                    AssertRejected(evidence.Requirements, toolCandidate, evidence.SourceChain,
                        MoonpalaceBoundaryCoverageIssueCode.ToolRequired);
                    break;
                case 28:
                    var noEvidenceTiles = evidence.Candidates[0].TileCells.Select(value =>
                        new MoonpalaceBoundaryCoverageCandidateEvidence.TileCell(
                            value.LocalX, value.LocalY, "NONE", "NONE", value.MarkerCode)).ToArray();
                    var noWarning = BoundaryCoverageTestMutation.ReplaceCandidate(
                        evidence.Candidates, 0,
                        BoundaryCoverageTestMutation.CopyCandidate(evidence.Candidates[0], tileCells: noEvidenceTiles));
                    AssertRejected(evidence.Requirements, noWarning, evidence.SourceChain,
                        MoonpalaceBoundaryCoverageIssueCode.MissingWarningEvidence);
                    break;
                default:
                    AssertRejected(evidence.Requirements, evidence.Candidates,
                        new MoonpalaceBoundaryCoverageValidator.SourceChain(
                            evidence.SourceChain.AuthoringManifestSha256,
                            evidence.SourceChain.PreviousTaskSha256,
                            1,
                            0), MoonpalaceBoundaryCoverageIssueCode.GeneratedCsvPresent);
                    AssertRejected(evidence.Requirements, evidence.Candidates,
                        new MoonpalaceBoundaryCoverageValidator.SourceChain(
                            evidence.SourceChain.AuthoringManifestSha256,
                            evidence.SourceChain.PreviousTaskSha256,
                            0,
                            1), MoonpalaceBoundaryCoverageIssueCode.AuthoringMutationDetected);
                    AssertRejected(evidence.Requirements, evidence.Candidates,
                        new MoonpalaceBoundaryCoverageValidator.SourceChain(
                            "BAD", "BAD", 0, 0), MoonpalaceBoundaryCoverageIssueCode.InvalidSourceChain);
                    break;
            }
        }

        private void AssertRejected(
            IEnumerable<MoonpalaceBoundaryCoverageRequirement> requirements,
            IEnumerable<MoonpalaceBoundaryCoverageCandidateEvidence> candidates,
            MoonpalaceBoundaryCoverageValidator.SourceChain sourceChain,
            MoonpalaceBoundaryCoverageIssueCode issueCode)
        {
            var report = validator.Validate(requirements, candidates, sourceChain);
            Assert.That(report.Accepted, Is.False);
            Assert.That(report.Issues.Select(value => value.Code), Does.Contain(issueCode), JoinIssues(report));
        }

        private static string JoinIssues(MoonpalaceBoundaryCoverageReport report)
        {
            return string.Join("\n", report.Issues.Select(value => value.ToString()));
        }
    }

    internal static class BoundaryCoverageAuthoringHarness
    {
        private static readonly object Sync = new object();
        private static BoundaryCoverageAuthoringEvidence cached;

        public static BoundaryCoverageAuthoringEvidence GetOrCreate()
        {
            lock (Sync)
            {
                if (cached == null) cached = Build();
                return cached;
            }
        }

        private static BoundaryCoverageAuthoringEvidence Build()
        {
            var pairRows = ReadCsv("_Game/Map/Data/WorldGeneration/Authoring/Boundary/biome_boundary_pair_rules.csv");
            var profileRows = ReadCsv("_Game/Map/Data/WorldGeneration/Authoring/Boundary/biome_boundary_profiles.csv")
                .ToDictionary(value => value["boundary_profile_id"], StringComparer.Ordinal);
            var boundaryRows = ReadCsv("_Game/Map/Data/WorldGeneration/Authoring/Boundary/boundary_chunk_catalog.csv");
            var microchunkRows = ReadCsv("_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_catalog.csv")
                .ToDictionary(value => value["microchunk_id"], StringComparer.Ordinal);
            var tileRows = ReadCsv("_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_tile_cells.csv")
                .GroupBy(value => value["microchunk_id"], StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
            var socketRows = ReadCsv("_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_sockets.csv")
                .GroupBy(value => value["microchunk_id"], StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

            var requirements = pairRows.Select(row =>
            {
                var expected = MoonpalaceBoundaryCoverageRequirement.Canonical.Single(value =>
                    value.PairRuleId == row["boundary_pair_rule_id"]);
                return new MoonpalaceBoundaryCoverageRequirement(
                    expected.PairOrder,
                    row["boundary_pair_rule_id"],
                    row["biome_a_id"],
                    row["biome_b_id"],
                    Split(row["allowed_boundary_profile_ids"]),
                    Split(row["boundary_profile_weights"]).Select(ParseInt),
                    row["default_boundary_profile_id"],
                    expected.ExpectedCandidateCount,
                    expected.ExpectedMicrochunkCount,
                    expected.ExpectedTileRowCount,
                    expected.ExpectedSocketRowCount,
                    ParseBool(row["active"]));
            }).OrderBy(value => value.PairOrder).ToList();
            var pairByBiomes = requirements.ToDictionary(
                value => value.BiomeAId + "|" + value.BiomeBId,
                value => value,
                StringComparer.Ordinal);

            var candidates = boundaryRows.Select(row =>
            {
                var microchunk = microchunkRows[row["microchunk_id"]];
                var profile = profileRows[row["boundary_profile_id"]];
                var requirement = pairByBiomes[row["biome_a_id"] + "|" + row["biome_b_id"]];
                var tiles = tileRows[row["microchunk_id"]].Select(value =>
                    new MoonpalaceBoundaryCoverageCandidateEvidence.TileCell(
                        ParseInt(value["local_x"]),
                        ParseInt(value["local_y"]),
                        value["ground_code"],
                        value["decor_back_code"],
                        value["marker_code"]));
                var sockets = socketRows[row["microchunk_id"]].Select(value =>
                    new MoonpalaceBoundaryCoverageCandidateEvidence.Socket(
                        value["socket_id"],
                        value["side"],
                        value["traversal_kind"],
                        ParseBool(value["mandatory_allowed"]),
                        value["tool_requirement"],
                        value["edge_signature_id"],
                        value["route_layer"],
                        ParseInt(value["minimum_safe_tiles"])));
                return new MoonpalaceBoundaryCoverageCandidateEvidence(
                    row["boundary_chunk_id"],
                    row["microchunk_id"],
                    requirement.PairRuleId,
                    row["biome_a_id"],
                    row["biome_b_id"],
                    row["boundary_profile_id"],
                    ParseOrientation(row["orientation"]),
                    ParseInt(row["route_type"]),
                    row["entry_edge_signature_id"],
                    row["exit_edge_signature_id"],
                    ParseInt(row["weight"]),
                    ParseBool(row["reversible"]),
                    ParseBool(row["active"]),
                    ParseBool(profile["mandatory_route_allowed"]),
                    profile["tool_requirement"],
                    ParseInt(microchunk["width_tiles"]),
                    ParseInt(microchunk["height_tiles"]),
                    microchunk["usage_class"],
                    microchunk["biome_ids"],
                    microchunk["route_roles"],
                    ParseBool(microchunk["tile_data_complete"]),
                    ParseBool(microchunk["active"]),
                    tiles,
                    sockets);
            }).OrderBy(value => value.PairRuleId, StringComparer.Ordinal)
              .ThenBy(value => value.CandidateId, StringComparer.Ordinal).ToList();

            var previousTaskSha256 = ComputeSha256(Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "MapDesign",
                "MCP",
                "TASKS",
                "MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES.md")));
            var sourceChain = new MoonpalaceBoundaryCoverageValidator.SourceChain(
                MoonpalaceBoundaryCoverageValidator.ExpectedAuthoringManifestSha256,
                previousTaskSha256,
                0,
                0);
            var report = new MoonpalaceBoundaryCoverageValidator().Validate(requirements, candidates, sourceChain);
            return new BoundaryCoverageAuthoringEvidence(
                requirements, candidates, sourceChain, previousTaskSha256, report);
        }

        private static List<Dictionary<string, string>> ReadCsv(string assetsRelativePath)
        {
            var path = Path.Combine(Application.dataPath, assetsRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var lines = File.ReadAllLines(path);
            var headers = lines[0].TrimStart('\uFEFF').Split(',');
            var rows = new List<Dictionary<string, string>>();
            for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                if (string.IsNullOrWhiteSpace(lines[lineIndex])) continue;
                var fields = lines[lineIndex].Split(',');
                if (fields.Length != headers.Length)
                {
                    throw new InvalidDataException(assetsRelativePath + " row width mismatch at " + lineIndex);
                }
                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var index = 0; index < headers.Length; index++) row.Add(headers[index], fields[index]);
                rows.Add(row);
            }
            return rows;
        }

        private static string[] Split(string value)
        {
            return value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool ParseBool(string value)
        {
            if (value == "1") return true;
            if (value == "0") return false;
            throw new FormatException("Unknown bool: " + value);
        }

        private static int ParseInt(string value)
        {
            return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        private static MoonpalaceBoundaryOrientation ParseOrientation(string value)
        {
            if (value == "HORIZONTAL") return MoonpalaceBoundaryOrientation.Horizontal;
            if (value == "VERTICAL") return MoonpalaceBoundaryOrientation.Vertical;
            throw new FormatException("Unknown orientation: " + value);
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(stream).Select(value => value.ToString("x2")));
            }
        }
    }

    internal sealed class BoundaryCoverageAuthoringEvidence
    {
        public BoundaryCoverageAuthoringEvidence(
            IEnumerable<MoonpalaceBoundaryCoverageRequirement> requirements,
            IEnumerable<MoonpalaceBoundaryCoverageCandidateEvidence> candidates,
            MoonpalaceBoundaryCoverageValidator.SourceChain sourceChain,
            string previousTaskSha256,
            MoonpalaceBoundaryCoverageReport report)
        {
            Requirements = new ReadOnlyCollection<MoonpalaceBoundaryCoverageRequirement>(requirements.ToArray());
            Candidates = new ReadOnlyCollection<MoonpalaceBoundaryCoverageCandidateEvidence>(candidates.ToArray());
            SourceChain = sourceChain;
            PreviousTaskSha256 = previousTaskSha256;
            Report = report;
        }

        public IReadOnlyList<MoonpalaceBoundaryCoverageRequirement> Requirements { get; }
        public IReadOnlyList<MoonpalaceBoundaryCoverageCandidateEvidence> Candidates { get; }
        public MoonpalaceBoundaryCoverageValidator.SourceChain SourceChain { get; }
        public string PreviousTaskSha256 { get; }
        public MoonpalaceBoundaryCoverageReport Report { get; }
    }

    internal static class BoundaryCoverageTestMutation
    {
        public static IReadOnlyList<MoonpalaceBoundaryCoverageRequirement> ReplaceRequirement(
            IReadOnlyList<MoonpalaceBoundaryCoverageRequirement> values,
            int index,
            MoonpalaceBoundaryCoverageRequirement replacement)
        {
            var copy = values.ToList();
            copy[index] = replacement;
            return copy;
        }

        public static IReadOnlyList<MoonpalaceBoundaryCoverageCandidateEvidence> ReplaceCandidate(
            IReadOnlyList<MoonpalaceBoundaryCoverageCandidateEvidence> values,
            int index,
            MoonpalaceBoundaryCoverageCandidateEvidence replacement)
        {
            var copy = values.ToList();
            copy[index] = replacement;
            return copy;
        }

        public static MoonpalaceBoundaryCoverageRequirement CopyRequirement(
            MoonpalaceBoundaryCoverageRequirement source,
            string pairRuleId = null,
            string biomeAId = null,
            string biomeBId = null,
            IEnumerable<string> allowedProfileIds = null,
            IEnumerable<int> profileWeights = null,
            string defaultProfileId = null,
            bool? active = null)
        {
            return new MoonpalaceBoundaryCoverageRequirement(
                source.PairOrder,
                pairRuleId ?? source.PairRuleId,
                biomeAId ?? source.BiomeAId,
                biomeBId ?? source.BiomeBId,
                allowedProfileIds ?? source.AllowedProfileIds,
                profileWeights ?? source.ProfileWeights,
                defaultProfileId ?? source.DefaultProfileId,
                source.ExpectedCandidateCount,
                source.ExpectedMicrochunkCount,
                source.ExpectedTileRowCount,
                source.ExpectedSocketRowCount,
                active ?? source.Active);
        }

        public static MoonpalaceBoundaryCoverageCandidateEvidence CopyCandidate(
            MoonpalaceBoundaryCoverageCandidateEvidence source,
            string candidateId = null,
            string microchunkId = null,
            string pairRuleId = null,
            string biomeAId = null,
            string biomeBId = null,
            string profileId = null,
            MoonpalaceBoundaryOrientation? orientation = null,
            int? weight = null,
            bool? reversible = null,
            bool? active = null,
            bool? mandatoryAllowed = null,
            string toolRequirement = null,
            IEnumerable<MoonpalaceBoundaryCoverageCandidateEvidence.TileCell> tileCells = null,
            IEnumerable<MoonpalaceBoundaryCoverageCandidateEvidence.Socket> sockets = null)
        {
            return new MoonpalaceBoundaryCoverageCandidateEvidence(
                candidateId ?? source.CandidateId,
                microchunkId ?? source.MicrochunkId,
                pairRuleId ?? source.PairRuleId,
                biomeAId ?? source.BiomeAId,
                biomeBId ?? source.BiomeBId,
                profileId ?? source.ProfileId,
                orientation ?? source.Orientation,
                source.RouteType,
                source.EntryEdgeSignatureId,
                source.ExitEdgeSignatureId,
                weight ?? source.Weight,
                reversible ?? source.Reversible,
                active ?? source.Active,
                mandatoryAllowed ?? source.MandatoryAllowed,
                toolRequirement ?? source.ToolRequirement,
                source.WidthTiles,
                source.HeightTiles,
                source.UsageClass,
                source.MicrochunkBiomeIds,
                source.RouteRoles,
                source.TileDataComplete,
                source.MicrochunkActive,
                tileCells ?? source.TileCells,
                sockets ?? source.Sockets);
        }

        public static MoonpalaceBoundaryCoverageCandidateEvidence.Socket CopySocket(
            MoonpalaceBoundaryCoverageCandidateEvidence.Socket source,
            string side = null,
            string toolRequirement = null)
        {
            return new MoonpalaceBoundaryCoverageCandidateEvidence.Socket(
                source.SocketId,
                side ?? source.Side,
                source.TraversalKind,
                source.MandatoryAllowed,
                toolRequirement ?? source.ToolRequirement,
                source.EdgeSignatureId,
                source.RouteLayer,
                source.MinimumSafeTiles);
        }
    }
}
