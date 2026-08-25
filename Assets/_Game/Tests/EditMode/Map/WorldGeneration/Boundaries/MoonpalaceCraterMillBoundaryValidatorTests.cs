using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Boundaries
{
    [Category("MAP08_07")]
    public sealed class MoonpalaceCraterMillBoundaryValidatorTests
    {
        private CraterMillAuthoringEvidence evidence;
        private MoonpalaceCraterMillBoundaryValidator validator;

        public static IEnumerable<TestCaseData> ValidationCases
        {
            get
            {
                for (var caseId = 0; caseId < 360; caseId++)
                {
                    yield return new TestCaseData(caseId)
                        .SetName("CraterMillBoundaryValidatorContract_" + caseId.ToString("D3"));
                }
            }
        }

        [OneTimeSetUp]
        public void LoadAuthoringEvidence()
        {
            evidence = CraterMillAuthoringHarness.GetOrCreate();
            validator = new MoonpalaceCraterMillBoundaryValidator();
        }

        [TestCaseSource(nameof(ValidationCases))]
        public void CraterMillBoundaryValidatorContract(int caseId)
        {
            var data = evidence.Data;
            var report = evidence.Report;
            var microchunkId = MoonpalaceCraterMillBoundaryAuthoringContract.MicrochunkIds[caseId % 4];
            switch (caseId % 16)
            {
                case 0:
                    Assert.That(report.Success, Is.True, string.Join("\n", report.Issues));
                    Assert.That(report.Issues, Is.Empty);
                    break;
                case 1:
                    Assert.That(report.CandidateCount, Is.EqualTo(4));
                    Assert.That(report.TileRowCount, Is.EqualTo(384));
                    Assert.That(report.SocketCount, Is.EqualTo(8));
                    break;
                case 2:
                    Assert.That(report.CandidateIds,
                        Is.EquivalentTo(MoonpalaceCraterMillBoundaryAuthoringContract.CandidateIds));
                    Assert.That(report.MicrochunkIds,
                        Is.EquivalentTo(MoonpalaceCraterMillBoundaryAuthoringContract.MicrochunkIds));
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
                    var index = MoonpalaceCraterMillBoundaryCandidateMatrix.Canonical.Index;
                    Assert.That(index.Count, Is.EqualTo(4));
                    Assert.That(index.GetCandidates(MoonpalaceCraterMillBoundaryAuthoringContract.Pair).Count,
                        Is.EqualTo(4));
                    break;
                case 6:
                    var candidateId = MoonpalaceCraterMillBoundaryAuthoringContract.CandidateIds[caseId % 4];
                    Assert.That(MoonpalaceCraterMillBoundaryCandidateMatrix.Canonical.GetMicrochunkId(candidateId),
                        Is.EqualTo(microchunkId));
                    break;
                case 7:
                    AssertRejected(ReplaceFirstCandidate(data, row => CopyCandidate(row, weight: 0)), "weight");
                    break;
                case 8:
                    AssertRejected(new MoonpalaceCraterMillBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles.Skip(1), data.Sockets), "tile row count");
                    break;
                case 9:
                    AssertRejected(ReplaceFirstSocket(data, row => CopySocket(row, side: "X")), "socket shape");
                    break;
                case 10:
                    AssertRejected(new MoonpalaceCraterMillBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets, generatedCsvCreated: 1),
                        "Generated CSV");
                    break;
                case 11:
                    AssertRejected(new MoonpalaceCraterMillBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets, otherPairRowsModified: 1),
                        "Other pair");
                    break;
                case 12:
                    AssertRejected(new MoonpalaceCraterMillBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets,
                        craterRootRowsModified: 1), "Crater/Root");
                    break;
                case 13:
                    var repeated = validator.Validate(data);
                    Assert.That(repeated.Issues, Is.EqualTo(report.Issues));
                    Assert.That(repeated.CandidateIds, Is.EqualTo(report.CandidateIds));
                    break;
                case 14:
                    var mutable = data.Candidates.ToList();
                    var snapshot = new MoonpalaceCraterMillBoundaryAuthoringData(
                        mutable, data.Microchunks, data.Tiles, data.Sockets);
                    mutable.Clear();
                    Assert.That(snapshot.Candidates.Count, Is.EqualTo(4));
                    break;
                case 15:
                    Assert.Throws<ArgumentNullException>(() => validator.Validate(null));
                    Assert.Throws<KeyNotFoundException>(() =>
                        MoonpalaceCraterMillBoundaryCandidateMatrix.Canonical.GetMicrochunkId("UNKNOWN"));
                    break;
                default:
                    Assert.That(report.GeneratedCsvCreated, Is.Zero);
                    Assert.That(report.OtherPairRowsModified, Is.Zero);
                    Assert.That(report.CraterRootRowsModified, Is.Zero);
                    Assert.That(report.ProfileOrientationMatrixComplete, Is.True);
                    break;
            }
        }

        private void AssertRejected(MoonpalaceCraterMillBoundaryAuthoringData data, string issueFragment)
        {
            var result = validator.Validate(data);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue =>
                issue.IndexOf(issueFragment, StringComparison.OrdinalIgnoreCase) >= 0), Is.True,
                string.Join("\n", result.Issues));
        }

        private static MoonpalaceCraterMillBoundaryAuthoringData ReplaceFirstCandidate(
            MoonpalaceCraterMillBoundaryAuthoringData data,
            Func<MoonpalaceCraterMillBoundaryCandidateRow, MoonpalaceCraterMillBoundaryCandidateRow> replace)
        {
            var rows = data.Candidates.ToList();
            rows[0] = replace(rows[0]);
            return new MoonpalaceCraterMillBoundaryAuthoringData(rows, data.Microchunks, data.Tiles, data.Sockets);
        }

        private static MoonpalaceCraterMillBoundaryAuthoringData ReplaceFirstSocket(
            MoonpalaceCraterMillBoundaryAuthoringData data,
            Func<MoonpalaceCraterMillSocketRow, MoonpalaceCraterMillSocketRow> replace)
        {
            var rows = data.Sockets.ToList();
            rows[0] = replace(rows[0]);
            return new MoonpalaceCraterMillBoundaryAuthoringData(data.Candidates, data.Microchunks, data.Tiles, rows);
        }

        private static MoonpalaceCraterMillBoundaryCandidateRow CopyCandidate(
            MoonpalaceCraterMillBoundaryCandidateRow row,
            int? weight = null)
        {
            return new MoonpalaceCraterMillBoundaryCandidateRow(
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

        private static MoonpalaceCraterMillSocketRow CopySocket(
            MoonpalaceCraterMillSocketRow row,
            string side = null)
        {
            return new MoonpalaceCraterMillSocketRow(
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
