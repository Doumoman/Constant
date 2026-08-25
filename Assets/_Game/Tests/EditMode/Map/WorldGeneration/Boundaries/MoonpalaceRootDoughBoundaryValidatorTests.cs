using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Boundaries
{
    [Category("MAP08_10")]
    public sealed class MoonpalaceRootDoughBoundaryValidatorTests
    {
        private RootDoughAuthoringEvidence evidence;
        private MoonpalaceRootDoughBoundaryValidator validator;

        public static IEnumerable<TestCaseData> ValidationCases
        {
            get
            {
                for (var caseId = 0; caseId < 360; caseId++)
                {
                    yield return new TestCaseData(caseId)
                        .SetName("RootDoughBoundaryValidatorContract_" + caseId.ToString("D3"));
                }
            }
        }

        [OneTimeSetUp]
        public void LoadAuthoringEvidence()
        {
            evidence = RootDoughAuthoringHarness.GetOrCreate();
            validator = new MoonpalaceRootDoughBoundaryValidator();
        }

        [TestCaseSource(nameof(ValidationCases))]
        public void RootDoughBoundaryValidatorContract(int caseId)
        {
            var data = evidence.Data;
            var report = evidence.Report;
            var microchunkId = MoonpalaceRootDoughBoundaryAuthoringContract.MicrochunkIds[caseId % MoonpalaceRootDoughBoundaryAuthoringContract.CandidateCount];
            switch (caseId % 20)
            {
                case 0:
                    Assert.That(report.Success, Is.True, string.Join("\n", report.Issues));
                    Assert.That(report.Issues, Is.Empty);
                    break;
                case 1:
                    Assert.That(report.CandidateCount, Is.EqualTo(5));
                    Assert.That(report.TileRowCount, Is.EqualTo(480));
                    Assert.That(report.SocketCount, Is.EqualTo(10));
                    break;
                case 2:
                    Assert.That(report.CandidateIds,
                        Is.EquivalentTo(MoonpalaceRootDoughBoundaryAuthoringContract.CandidateIds));
                    Assert.That(report.MicrochunkIds,
                        Is.EquivalentTo(MoonpalaceRootDoughBoundaryAuthoringContract.MicrochunkIds));
                    break;
                case 3:
                    Assert.That(report.RowsPerOwnedMicrochunk[microchunkId], Is.EqualTo(96));
                    Assert.That(report.WarningMarkerCategoriesByMicrochunk[microchunkId], Is.EqualTo(2));
                    break;
                case 4:
                    Assert.That(report.HorizontalSocketShapeValid, Is.True);
                    Assert.That(report.VerticalSocketShapeValid, Is.True);
                    Assert.That(report.MandatoryAllowed, Is.True);
                    Assert.That(report.ToolRequirementNone, Is.True);
                    break;
                case 5:
                    var index = MoonpalaceRootDoughBoundaryCandidateMatrix.Canonical.Index;
                    Assert.That(index.Count, Is.EqualTo(5));
                    Assert.That(index.GetCandidates(MoonpalaceRootDoughBoundaryAuthoringContract.Pair).Count,
                        Is.EqualTo(5));
                    break;
                case 6:
                    var candidateId = MoonpalaceRootDoughBoundaryAuthoringContract.CandidateIds[caseId % MoonpalaceRootDoughBoundaryAuthoringContract.CandidateCount];
                    Assert.That(MoonpalaceRootDoughBoundaryCandidateMatrix.Canonical.GetMicrochunkId(candidateId),
                        Is.EqualTo(microchunkId));
                    break;
                case 7:
                    AssertRejected(ReplaceFirstCandidate(data, row => CopyCandidate(row, weight: 0)), "weight");
                    break;
                case 8:
                    AssertRejected(new MoonpalaceRootDoughBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles.Skip(1), data.Sockets), "tile row count");
                    break;
                case 9:
                    AssertRejected(ReplaceFirstSocket(data, row => CopySocket(row, side: "X")), "socket shape");
                    break;
                case 10:
                    AssertRejected(new MoonpalaceRootDoughBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets, generatedCsvCreated: 1),
                        "Generated CSV");
                    break;
                case 11:
                    AssertRejected(new MoonpalaceRootDoughBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets, otherPairRowsModified: 1),
                        "Other pair");
                    break;
                case 12:
                    AssertRejected(new MoonpalaceRootDoughBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets,
                        existingRowsModified: 1), "Existing rows");
                    break;
                case 13:
                    AssertRejected(new MoonpalaceRootDoughBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets,
                        craterRootRowsModified: 1), "Crater/Root");
                    break;
                case 14:
                    AssertRejected(new MoonpalaceRootDoughBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets,
                        craterMillRowsModified: 1), "Crater/Mill");
                    break;
                case 15:
                    AssertRejected(new MoonpalaceRootDoughBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets,
                        craterDoughRowsModified: 1), "Crater/Dough");
                    break;
                case 16:
                    AssertRejected(new MoonpalaceRootDoughBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets,
                        rootMillRowsModified: 1), "Root/Mill");
                    break;
                case 17:
                    var mutable = data.Candidates.ToList();
                    var snapshot = new MoonpalaceRootDoughBoundaryAuthoringData(
                        mutable, data.Microchunks, data.Tiles, data.Sockets);
                    mutable.Clear();
                    Assert.That(snapshot.Candidates.Count, Is.EqualTo(5));
                    break;
                case 18:
                    Assert.Throws<ArgumentNullException>(() => validator.Validate(null));
                    Assert.Throws<KeyNotFoundException>(() =>
                        MoonpalaceRootDoughBoundaryCandidateMatrix.Canonical.GetMicrochunkId("UNKNOWN"));
                    break;
                default:
                    Assert.That(report.GeneratedCsvCreated, Is.Zero);
                    Assert.That(report.ExistingRowsModified, Is.Zero);
                    Assert.That(report.OtherPairRowsModified, Is.Zero);
                    Assert.That(report.CraterRootRowsModified, Is.Zero);
                    Assert.That(report.CraterMillRowsModified, Is.Zero);
                    Assert.That(report.CraterDoughRowsModified, Is.Zero);
                    Assert.That(report.RootMillRowsModified, Is.Zero);
                    Assert.That(report.ProfileOrientationMatrixComplete, Is.True);
                    break;
            }
        }

        private void AssertRejected(MoonpalaceRootDoughBoundaryAuthoringData data, string issueFragment)
        {
            var result = validator.Validate(data);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue =>
                issue.IndexOf(issueFragment, StringComparison.OrdinalIgnoreCase) >= 0), Is.True,
                string.Join("\n", result.Issues));
        }

        private static MoonpalaceRootDoughBoundaryAuthoringData ReplaceFirstCandidate(
            MoonpalaceRootDoughBoundaryAuthoringData data,
            Func<MoonpalaceRootDoughBoundaryCandidateRow, MoonpalaceRootDoughBoundaryCandidateRow> replace)
        {
            var rows = data.Candidates.ToList();
            rows[0] = replace(rows[0]);
            return new MoonpalaceRootDoughBoundaryAuthoringData(rows, data.Microchunks, data.Tiles, data.Sockets);
        }

        private static MoonpalaceRootDoughBoundaryAuthoringData ReplaceFirstSocket(
            MoonpalaceRootDoughBoundaryAuthoringData data,
            Func<MoonpalaceRootDoughSocketRow, MoonpalaceRootDoughSocketRow> replace)
        {
            var rows = data.Sockets.ToList();
            rows[0] = replace(rows[0]);
            return new MoonpalaceRootDoughBoundaryAuthoringData(data.Candidates, data.Microchunks, data.Tiles, rows);
        }

        private static MoonpalaceRootDoughBoundaryCandidateRow CopyCandidate(
            MoonpalaceRootDoughBoundaryCandidateRow row,
            int? weight = null)
        {
            return new MoonpalaceRootDoughBoundaryCandidateRow(
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

        private static MoonpalaceRootDoughSocketRow CopySocket(
            MoonpalaceRootDoughSocketRow row,
            string side = null)
        {
            return new MoonpalaceRootDoughSocketRow(
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
