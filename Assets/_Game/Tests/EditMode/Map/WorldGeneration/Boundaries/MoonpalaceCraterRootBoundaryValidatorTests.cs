using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Boundaries
{
    [Category("MAP08_06")]
    public sealed class MoonpalaceCraterRootBoundaryValidatorTests
    {
        private CraterRootAuthoringEvidence evidence;
        private MoonpalaceCraterRootBoundaryValidator validator;

        public static IEnumerable<TestCaseData> ValidationCases
        {
            get
            {
                for (var caseId = 0; caseId < 360; caseId++)
                {
                    yield return new TestCaseData(caseId)
                        .SetName("CraterRootBoundaryValidatorContract_" + caseId.ToString("D3"));
                }
            }
        }

        [OneTimeSetUp]
        public void LoadAuthoringEvidence()
        {
            evidence = CraterRootAuthoringHarness.GetOrCreate();
            validator = new MoonpalaceCraterRootBoundaryValidator();
        }

        [TestCaseSource(nameof(ValidationCases))]
        public void CraterRootBoundaryValidatorContract(int caseId)
        {
            var data = evidence.Data;
            var report = evidence.Report;
            var microchunkId = MoonpalaceCraterRootBoundaryAuthoringContract.MicrochunkIds[caseId % 6];
            switch (caseId % 16)
            {
                case 0:
                    Assert.That(report.Success, Is.True, string.Join("\n", report.Issues));
                    Assert.That(report.Issues, Is.Empty);
                    break;
                case 1:
                    Assert.That(report.CandidateCount, Is.EqualTo(6));
                    Assert.That(report.TileRowCount, Is.EqualTo(576));
                    Assert.That(report.SocketCount, Is.EqualTo(12));
                    break;
                case 2:
                    Assert.That(report.CandidateIds,
                        Is.EquivalentTo(MoonpalaceCraterRootBoundaryAuthoringContract.CandidateIds));
                    Assert.That(report.MicrochunkIds,
                        Is.EquivalentTo(MoonpalaceCraterRootBoundaryAuthoringContract.MicrochunkIds));
                    break;
                case 3:
                    Assert.That(report.RowsPerOwnedMicrochunk[microchunkId], Is.EqualTo(96));
                    Assert.That(report.WarningMarkerCategoriesByMicrochunk[microchunkId], Is.GreaterThanOrEqualTo(2));
                    break;
                case 4:
                    Assert.That(report.HorizontalSocketShapeValid, Is.True);
                    Assert.That(report.VerticalSocketShapeValid, Is.True);
                    Assert.That(report.MandatoryAllowed, Is.True);
                    Assert.That(report.ToolRequirementNone, Is.True);
                    break;
                case 5:
                    var index = MoonpalaceCraterRootBoundaryCandidateMatrix.Canonical.Index;
                    Assert.That(index.Count, Is.EqualTo(6));
                    Assert.That(index.GetCandidates(MoonpalaceCraterRootBoundaryAuthoringContract.Pair).Count,
                        Is.EqualTo(6));
                    break;
                case 6:
                    var candidateId = MoonpalaceCraterRootBoundaryAuthoringContract.CandidateIds[caseId % 6];
                    Assert.That(MoonpalaceCraterRootBoundaryCandidateMatrix.Canonical.GetMicrochunkId(candidateId),
                        Is.EqualTo(microchunkId));
                    break;
                case 7:
                    AssertRejected(ReplaceFirstCandidate(data, row => CopyCandidate(row, weight: 0)), "weight");
                    break;
                case 8:
                    AssertRejected(new MoonpalaceCraterRootBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles.Skip(1), data.Sockets), "tile row count");
                    break;
                case 9:
                    AssertRejected(ReplaceFirstSocket(data, row => CopySocket(row, side: "X")), "socket shape");
                    break;
                case 10:
                    AssertRejected(new MoonpalaceCraterRootBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets, generatedCsvCreated: 1),
                        "Generated CSV");
                    break;
                case 11:
                    AssertRejected(new MoonpalaceCraterRootBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets, otherPairRowsModified: 1),
                        "Other pair");
                    break;
                case 12:
                    var repeated = validator.Validate(data);
                    Assert.That(repeated.Issues, Is.EqualTo(report.Issues));
                    Assert.That(repeated.CandidateIds, Is.EqualTo(report.CandidateIds));
                    break;
                case 13:
                    var mutable = data.Candidates.ToList();
                    var snapshot = new MoonpalaceCraterRootBoundaryAuthoringData(
                        mutable, data.Microchunks, data.Tiles, data.Sockets);
                    mutable.Clear();
                    Assert.That(snapshot.Candidates.Count, Is.EqualTo(6));
                    break;
                case 14:
                    Assert.Throws<ArgumentNullException>(() => validator.Validate(null));
                    Assert.Throws<KeyNotFoundException>(() =>
                        MoonpalaceCraterRootBoundaryCandidateMatrix.Canonical.GetMicrochunkId("UNKNOWN"));
                    break;
                default:
                    Assert.That(report.GeneratedCsvCreated, Is.Zero);
                    Assert.That(report.OtherPairRowsModified, Is.Zero);
                    Assert.That(report.ProfileOrientationMatrixComplete, Is.True);
                    break;
            }
        }

        private void AssertRejected(MoonpalaceCraterRootBoundaryAuthoringData data, string issueFragment)
        {
            var result = validator.Validate(data);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue =>
                issue.IndexOf(issueFragment, StringComparison.OrdinalIgnoreCase) >= 0), Is.True,
                string.Join("\n", result.Issues));
        }

        private static MoonpalaceCraterRootBoundaryAuthoringData ReplaceFirstCandidate(
            MoonpalaceCraterRootBoundaryAuthoringData data,
            Func<MoonpalaceCraterRootBoundaryCandidateRow, MoonpalaceCraterRootBoundaryCandidateRow> replace)
        {
            var rows = data.Candidates.ToList();
            rows[0] = replace(rows[0]);
            return new MoonpalaceCraterRootBoundaryAuthoringData(rows, data.Microchunks, data.Tiles, data.Sockets);
        }

        private static MoonpalaceCraterRootBoundaryAuthoringData ReplaceFirstSocket(
            MoonpalaceCraterRootBoundaryAuthoringData data,
            Func<MoonpalaceCraterRootSocketRow, MoonpalaceCraterRootSocketRow> replace)
        {
            var rows = data.Sockets.ToList();
            rows[0] = replace(rows[0]);
            return new MoonpalaceCraterRootBoundaryAuthoringData(data.Candidates, data.Microchunks, data.Tiles, rows);
        }

        private static MoonpalaceCraterRootBoundaryCandidateRow CopyCandidate(
            MoonpalaceCraterRootBoundaryCandidateRow row,
            int? weight = null)
        {
            return new MoonpalaceCraterRootBoundaryCandidateRow(
                row.CandidateId,
                row.MicrochunkId,
                row.BiomeAId,
                row.BiomeBId,
                row.ProfileId,
                row.Orientation,
                row.RouteType,
                row.EntryEdgeSignatureId,
                row.ExitEdgeSignatureId,
                weight ?? row.Weight,
                row.Reversible,
                row.Active,
                row.MandatoryAllowed,
                row.ToolRequirement);
        }

        private static MoonpalaceCraterRootSocketRow CopySocket(
            MoonpalaceCraterRootSocketRow row,
            string side = null)
        {
            return new MoonpalaceCraterRootSocketRow(
                row.MicrochunkId,
                row.SocketId,
                side ?? row.Side,
                row.TraversalKind,
                row.MandatoryAllowed,
                row.ToolRequirement,
                row.EdgeSignatureId,
                row.RouteLayer,
                row.MinimumSafeTiles);
        }
    }
}
