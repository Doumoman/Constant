using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Boundaries
{
    [Category("MAP08_06")]
    public sealed class MoonpalaceCraterRootBoundaryAuthoringTests
    {
        private CraterRootAuthoringEvidence evidence;

        public static IEnumerable<TestCaseData> ContractCases
        {
            get
            {
                for (var caseId = 0; caseId < 360; caseId++)
                {
                    yield return new TestCaseData(caseId)
                        .SetName("CraterRootBoundaryAuthoringContract_" + caseId.ToString("D3"));
                }
            }
        }

        [OneTimeSetUp]
        public void LoadAuthoringEvidence()
        {
            evidence = CraterRootAuthoringHarness.GetOrCreate();
        }

        [TestCaseSource(nameof(ContractCases))]
        public void CraterRootBoundaryAuthoringContract(int caseId)
        {
            var candidateIndex = caseId % MoonpalaceCraterRootBoundaryAuthoringContract.CandidateCount;
            var candidate = evidence.Data.Candidates[candidateIndex];
            var microchunkId = MoonpalaceCraterRootBoundaryAuthoringContract.MicrochunkIds[candidateIndex];
            var tiles = evidence.Data.Tiles.Where(row => row.MicrochunkId == microchunkId).ToList();
            var sockets = evidence.Data.Sockets.Where(row => row.MicrochunkId == microchunkId).ToList();

            switch (caseId % 18)
            {
                case 0:
                    Assert.That(evidence.Data.Candidates.Count, Is.EqualTo(6));
                    Assert.That(evidence.Data.Candidates.Select(row => row.CandidateId), Is.Unique);
                    break;
                case 1:
                    Assert.That(evidence.Data.Candidates.Select(row => row.CandidateId),
                        Is.EqualTo(MoonpalaceCraterRootBoundaryAuthoringContract.CandidateIds));
                    break;
                case 2:
                    Assert.That(evidence.Data.Microchunks.Select(row => row.MicrochunkId),
                        Is.EquivalentTo(MoonpalaceCraterRootBoundaryAuthoringContract.MicrochunkIds));
                    break;
                case 3:
                    Assert.That(evidence.Report.ProfileOrientationMatrixComplete, Is.True);
                    Assert.That(evidence.MatrixPairs.Count, Is.EqualTo(6));
                    break;
                case 4:
                    Assert.That(candidate.BiomeAId, Is.EqualTo("BIO_MOON_CRATER"));
                    Assert.That(candidate.BiomeBId, Is.EqualTo("BIO_CASSIA_ROOT"));
                    Assert.That(candidate.Weight, Is.GreaterThan(0));
                    Assert.That(candidate.Active && candidate.Reversible, Is.True);
                    break;
                case 5:
                    Assert.That(evidence.PairRule["boundary_pair_rule_id"], Is.EqualTo("PAIR_CRATER_ROOT"));
                    Assert.That(evidence.PairRule["default_boundary_profile_id"], Is.EqualTo("BOUND_SOFT_BLEND"));
                    Assert.That(evidence.PairRule["allowed_boundary_profile_ids"],
                        Is.EqualTo("BOUND_SOFT_BLEND|BOUND_CLIFF|BOUND_TUNNEL"));
                    break;
                case 6:
                    Assert.That(evidence.PairRule["boundary_profile_weights"], Is.EqualTo("50|25|25"));
                    Assert.That(evidence.Profiles.Values.All(row => row["mandatory_route_allowed"] == "1"), Is.True);
                    Assert.That(evidence.Profiles.Values.All(row => row["tool_requirement"] == "NONE"), Is.True);
                    break;
                case 7:
                    Assert.That(evidence.Data.Tiles.Count, Is.EqualTo(576));
                    Assert.That(tiles.Count, Is.EqualTo(96));
                    break;
                case 8:
                    Assert.That(tiles.Select(row => row.LocalY * 12 + row.LocalX), Is.Unique);
                    Assert.That(tiles.Min(row => row.LocalX), Is.Zero);
                    Assert.That(tiles.Max(row => row.LocalX), Is.EqualTo(11));
                    Assert.That(tiles.Min(row => row.LocalY), Is.Zero);
                    Assert.That(tiles.Max(row => row.LocalY), Is.EqualTo(7));
                    break;
                case 9:
                    Assert.That(tiles.Any(row => row.GroundCode == "G_MOON_ROCK"), Is.True);
                    Assert.That(tiles.Any(row => row.GroundCode == "G_CASSIA_WOOD"), Is.True);
                    break;
                case 10:
                    Assert.That(tiles.Any(row => row.DecorBackCode == "DB_CRATER"), Is.True);
                    Assert.That(tiles.Any(row => row.DecorBackCode == "DB_ROOT"), Is.True);
                    Assert.That(evidence.Report.WarningMarkerCategoriesByMicrochunk[microchunkId],
                        Is.GreaterThanOrEqualTo(2));
                    break;
                case 11:
                    Assert.That(tiles.Any(row => row.MarkerCode == "M_ROUTE_MAIN"), Is.True);
                    Assert.That(tiles.Any(row => row.MarkerCode == "M_SOCKET"), Is.True);
                    break;
                case 12:
                    Assert.That(evidence.Data.Sockets.Count, Is.EqualTo(12));
                    Assert.That(sockets.Count, Is.EqualTo(2));
                    Assert.That(sockets.All(row => row.MandatoryAllowed), Is.True);
                    break;
                case 13:
                    if (candidate.Orientation == MoonpalaceBoundaryOrientation.Horizontal)
                    {
                        Assert.That(sockets.Select(row => row.Side), Is.EquivalentTo(new[] { "L", "R" }));
                        Assert.That(sockets.All(row => row.TraversalKind == "WALK"), Is.True);
                    }
                    else
                    {
                        Assert.That(sockets.Select(row => row.Side), Is.EquivalentTo(new[] { "U", "D" }));
                        Assert.That(sockets.All(row => row.TraversalKind == "CLIMB"), Is.True);
                    }
                    break;
                case 14:
                    Assert.That(sockets.All(row => row.ToolRequirement == "NONE"), Is.True);
                    Assert.That(sockets.All(row => row.RouteLayer == "MANDATORY"), Is.True);
                    Assert.That(sockets.All(row => row.MinimumSafeTiles >= 2), Is.True);
                    break;
                case 15:
                    Assert.That(evidence.AllOwnedCsvFilesHaveUtf8Bom, Is.True);
                    Assert.That(evidence.NonOwnedBoundaryCandidateCount, Is.EqualTo(25));
                    break;
                case 16:
                    var matrixCandidate = MoonpalaceCraterRootBoundaryCandidateMatrix.Canonical
                        .Candidates.Single(value => value.CandidateId == candidate.CandidateId);
                    Assert.That(matrixCandidate.Profile.CanonicalId, Is.EqualTo(candidate.ProfileId));
                    Assert.That(matrixCandidate.Orientation, Is.EqualTo(candidate.Orientation));
                    Assert.That(matrixCandidate.WarningMarkers,
                        Is.EqualTo(MoonpalaceBoundaryWarningMarker.Tile |
                                   MoonpalaceBoundaryWarningMarker.Background));
                    break;
                default:
                    Assert.That(evidence.Report.Success, Is.True,
                        string.Join("\n", evidence.Report.Issues));
                    Assert.That(evidence.Data.GeneratedCsvCreated, Is.Zero);
                    Assert.That(evidence.Data.OtherPairRowsModified, Is.Zero);
                    break;
            }
        }
    }

    internal static class CraterRootAuthoringHarness
    {
        private static readonly object Sync = new object();
        private static CraterRootAuthoringEvidence cached;

        public static CraterRootAuthoringEvidence GetOrCreate()
        {
            lock (Sync)
            {
                if (cached == null) cached = Build();
                return cached;
            }
        }

        private static CraterRootAuthoringEvidence Build()
        {
            var boundaryRows = ReadCsv("_Game/Map/Data/WorldGeneration/Authoring/Boundary/boundary_chunk_catalog.csv");
            var catalogRows = ReadCsv("_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_catalog.csv");
            var tileRows = ReadCsv("_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_tile_cells.csv");
            var socketRows = ReadCsv("_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_sockets.csv");
            var pairRules = ReadCsv("_Game/Map/Data/WorldGeneration/Authoring/Boundary/biome_boundary_pair_rules.csv");
            var profiles = ReadCsv("_Game/Map/Data/WorldGeneration/Authoring/Boundary/biome_boundary_profiles.csv");

            var pairRule = pairRules.Single(row => row["boundary_pair_rule_id"] == "PAIR_CRATER_ROOT");
            var profileMap = profiles
                .Where(row => MoonpalaceCraterRootBoundaryAuthoringContract.ProfileIds.Contains(
                    row["boundary_profile_id"], StringComparer.Ordinal))
                .ToDictionary(row => row["boundary_profile_id"], StringComparer.Ordinal);

            var candidates = boundaryRows
                .Where(row => MoonpalaceCraterRootBoundaryAuthoringContract.IsOwnedCandidate(row["boundary_chunk_id"]))
                .Select(row =>
                {
                    var profile = profileMap[row["boundary_profile_id"]];
                    return new MoonpalaceCraterRootBoundaryCandidateRow(
                        row["boundary_chunk_id"],
                        row["microchunk_id"],
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
                        profile["tool_requirement"]);
                })
                .ToList();

            var microchunks = catalogRows
                .Where(row => MoonpalaceCraterRootBoundaryAuthoringContract.IsOwnedMicrochunk(row["microchunk_id"]))
                .Select(row => new MoonpalaceCraterRootMicrochunkRow(
                    row["microchunk_id"],
                    ParseInt(row["width_tiles"]),
                    ParseInt(row["height_tiles"]),
                    row["usage_class"],
                    row["biome_ids"],
                    row["route_roles"],
                    ParseBool(row["tile_data_complete"]),
                    ParseBool(row["active"])))
                .ToList();

            var tiles = tileRows
                .Where(row => MoonpalaceCraterRootBoundaryAuthoringContract.IsOwnedMicrochunk(row["microchunk_id"]))
                .Select(row => new MoonpalaceCraterRootTileRow(
                    row["microchunk_id"],
                    ParseInt(row["local_x"]),
                    ParseInt(row["local_y"]),
                    row["ground_code"],
                    row["decor_back_code"],
                    row["marker_code"]))
                .ToList();

            var sockets = socketRows
                .Where(row => MoonpalaceCraterRootBoundaryAuthoringContract.IsOwnedMicrochunk(row["microchunk_id"]))
                .Select(row => new MoonpalaceCraterRootSocketRow(
                    row["microchunk_id"],
                    row["socket_id"],
                    row["side"],
                    row["traversal_kind"],
                    ParseBool(row["mandatory_allowed"]),
                    row["tool_requirement"],
                    row["edge_signature_id"],
                    row["route_layer"],
                    ParseInt(row["minimum_safe_tiles"])))
                .ToList();

            var data = new MoonpalaceCraterRootBoundaryAuthoringData(
                candidates,
                microchunks,
                tiles,
                sockets);
            var report = new MoonpalaceCraterRootBoundaryValidator().Validate(data);
            var matrixPairs = new HashSet<string>(candidates.Select(row =>
                row.ProfileId + "|" + (row.Orientation == MoonpalaceBoundaryOrientation.Horizontal
                    ? "HORIZONTAL"
                    : "VERTICAL")), StringComparer.Ordinal);
            var files = new[]
            {
                "_Game/Map/Data/WorldGeneration/Authoring/Boundary/boundary_chunk_catalog.csv",
                "_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_catalog.csv",
                "_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_tile_cells.csv",
                "_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_sockets.csv",
            };
            return new CraterRootAuthoringEvidence(
                data,
                report,
                pairRule,
                profileMap,
                matrixPairs,
                files.All(HasUtf8Bom),
                boundaryRows.Count(row => !MoonpalaceCraterRootBoundaryAuthoringContract.IsOwnedCandidate(
                    row["boundary_chunk_id"])));
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

        private static bool HasUtf8Bom(string assetsRelativePath)
        {
            var path = Path.Combine(Application.dataPath, assetsRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var bytes = File.ReadAllBytes(path);
            return bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        }

        private static MoonpalaceBoundaryOrientation ParseOrientation(string value)
        {
            if (value == "HORIZONTAL") return MoonpalaceBoundaryOrientation.Horizontal;
            if (value == "VERTICAL") return MoonpalaceBoundaryOrientation.Vertical;
            throw new FormatException("Unknown orientation: " + value);
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
    }

    internal sealed class CraterRootAuthoringEvidence
    {
        public CraterRootAuthoringEvidence(
            MoonpalaceCraterRootBoundaryAuthoringData data,
            MoonpalaceCraterRootBoundaryContentReport report,
            IReadOnlyDictionary<string, string> pairRule,
            IDictionary<string, Dictionary<string, string>> profiles,
            ISet<string> matrixPairs,
            bool allOwnedCsvFilesHaveUtf8Bom,
            int nonOwnedBoundaryCandidateCount)
        {
            Data = data;
            Report = report;
            PairRule = pairRule;
            Profiles = new ReadOnlyDictionary<string, Dictionary<string, string>>(
                new Dictionary<string, Dictionary<string, string>>(profiles, StringComparer.Ordinal));
            MatrixPairs = new ReadOnlyCollection<string>(matrixPairs.OrderBy(value => value, StringComparer.Ordinal).ToList());
            AllOwnedCsvFilesHaveUtf8Bom = allOwnedCsvFilesHaveUtf8Bom;
            NonOwnedBoundaryCandidateCount = nonOwnedBoundaryCandidateCount;
        }

        public MoonpalaceCraterRootBoundaryAuthoringData Data { get; }
        public MoonpalaceCraterRootBoundaryContentReport Report { get; }
        public IReadOnlyDictionary<string, string> PairRule { get; }
        public IReadOnlyDictionary<string, Dictionary<string, string>> Profiles { get; }
        public IReadOnlyList<string> MatrixPairs { get; }
        public bool AllOwnedCsvFilesHaveUtf8Bom { get; }
        public int NonOwnedBoundaryCandidateCount { get; }
    }
}
