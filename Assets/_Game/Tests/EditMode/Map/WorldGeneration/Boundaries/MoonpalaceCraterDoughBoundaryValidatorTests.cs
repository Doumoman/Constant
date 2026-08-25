using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Boundaries
{
    [Category("MAP08_08")]
    public sealed class MoonpalaceCraterDoughBoundaryValidatorTests
    {
        private CraterDoughAuthoringEvidence evidence;
        private MoonpalaceCraterDoughBoundaryValidator validator;

        public static IEnumerable<TestCaseData> ValidationCases
        {
            get
            {
                for (var caseId = 0; caseId < 360; caseId++)
                {
                    yield return new TestCaseData(caseId)
                        .SetName("CraterDoughBoundaryValidatorContract_" + caseId.ToString("D3"));
                }
            }
        }

        [OneTimeSetUp]
        public void LoadAuthoringEvidence()
        {
            evidence = CraterDoughAuthoringHarness.GetOrCreate();
            validator = new MoonpalaceCraterDoughBoundaryValidator();
        }

        [TestCaseSource(nameof(ValidationCases))]
        public void CraterDoughBoundaryValidatorContract(int caseId)
        {
            var data = evidence.Data;
            var report = evidence.Report;
            var microchunkId = MoonpalaceCraterDoughBoundaryAuthoringContract.MicrochunkIds[
                caseId % MoonpalaceCraterDoughBoundaryAuthoringContract.CandidateCount];
            switch (caseId % 16)
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
                        Is.EquivalentTo(MoonpalaceCraterDoughBoundaryAuthoringContract.CandidateIds));
                    Assert.That(report.MicrochunkIds,
                        Is.EquivalentTo(MoonpalaceCraterDoughBoundaryAuthoringContract.MicrochunkIds));
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
                    var index = MoonpalaceCraterDoughBoundaryCandidateMatrix.Canonical.Index;
                    Assert.That(index.Count, Is.EqualTo(5));
                    Assert.That(index.GetCandidates(MoonpalaceCraterDoughBoundaryAuthoringContract.Pair).Count,
                        Is.EqualTo(5));
                    break;
                case 6:
                    var candidateId = MoonpalaceCraterDoughBoundaryAuthoringContract.CandidateIds[
                        caseId % MoonpalaceCraterDoughBoundaryAuthoringContract.CandidateCount];
                    Assert.That(MoonpalaceCraterDoughBoundaryCandidateMatrix.Canonical.GetMicrochunkId(candidateId),
                        Is.EqualTo(microchunkId));
                    break;
                case 7:
                    AssertRejected(ReplaceFirstCandidate(data, row => CopyCandidate(row, weight: 0)), "weight");
                    break;
                case 8:
                    AssertRejected(new MoonpalaceCraterDoughBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles.Skip(1), data.Sockets), "tile row count");
                    break;
                case 9:
                    AssertRejected(ReplaceFirstSocket(data, row => CopySocket(row, side: "X")), "socket shape");
                    break;
                case 10:
                    AssertRejected(new MoonpalaceCraterDoughBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets, generatedCsvCreated: 1),
                        "Generated CSV");
                    break;
                case 11:
                    AssertRejected(new MoonpalaceCraterDoughBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets, otherPairRowsModified: 1),
                        "Other pair");
                    break;
                case 12:
                    AssertRejected(new MoonpalaceCraterDoughBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets,
                        craterRootRowsModified: 1), "Crater/Root");
                    break;
                case 13:
                    AssertRejected(new MoonpalaceCraterDoughBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets,
                        craterMillRowsModified: 1), "Crater/Mill");
                    break;
                case 14:
                    var invalidLayerRows = data.Candidates.ToList();
                    invalidLayerRows[2] = CopyCandidate(
                        invalidLayerRows[2], orientation: MoonpalaceBoundaryOrientation.Horizontal);
                    AssertRejected(new MoonpalaceCraterDoughBoundaryAuthoringData(
                        invalidLayerRows, data.Microchunks, data.Tiles, data.Sockets),
                        "BOUND_LAYER/HORIZONTAL");
                    break;
                case 15:
                    var mutable = data.Candidates.ToList();
                    var snapshot = new MoonpalaceCraterDoughBoundaryAuthoringData(
                        mutable, data.Microchunks, data.Tiles, data.Sockets);
                    mutable.Clear();
                    Assert.That(snapshot.Candidates.Count, Is.EqualTo(5));
                    Assert.Throws<ArgumentNullException>(() => validator.Validate(null));
                    Assert.Throws<KeyNotFoundException>(() =>
                        MoonpalaceCraterDoughBoundaryCandidateMatrix.Canonical.GetMicrochunkId("UNKNOWN"));
                    Assert.That(report.CraterRootRowsModified, Is.Zero);
                    Assert.That(report.CraterMillRowsModified, Is.Zero);
                    Assert.That(report.InvalidLayerHorizontalCandidateCount, Is.Zero);
                    break;
                default:
                    Assert.That(report.GeneratedCsvCreated, Is.Zero);
                    Assert.That(report.OtherPairRowsModified, Is.Zero);
                    Assert.That(report.CraterRootRowsModified, Is.Zero);
                    Assert.That(report.CraterMillRowsModified, Is.Zero);
                    Assert.That(report.InvalidLayerHorizontalCandidateCount, Is.Zero);
                    Assert.That(report.ProfileOrientationMatrixComplete, Is.True);
                    break;
            }
        }

        private void AssertRejected(MoonpalaceCraterDoughBoundaryAuthoringData data, string issueFragment)
        {
            var result = validator.Validate(data);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue =>
                issue.IndexOf(issueFragment, StringComparison.OrdinalIgnoreCase) >= 0), Is.True,
                string.Join("\n", result.Issues));
        }

        private static MoonpalaceCraterDoughBoundaryAuthoringData ReplaceFirstCandidate(
            MoonpalaceCraterDoughBoundaryAuthoringData data,
            Func<MoonpalaceCraterDoughBoundaryCandidateRow, MoonpalaceCraterDoughBoundaryCandidateRow> replace)
        {
            var rows = data.Candidates.ToList();
            rows[0] = replace(rows[0]);
            return new MoonpalaceCraterDoughBoundaryAuthoringData(rows, data.Microchunks, data.Tiles, data.Sockets);
        }

        private static MoonpalaceCraterDoughBoundaryAuthoringData ReplaceFirstSocket(
            MoonpalaceCraterDoughBoundaryAuthoringData data,
            Func<MoonpalaceCraterDoughSocketRow, MoonpalaceCraterDoughSocketRow> replace)
        {
            var rows = data.Sockets.ToList();
            rows[0] = replace(rows[0]);
            return new MoonpalaceCraterDoughBoundaryAuthoringData(data.Candidates, data.Microchunks, data.Tiles, rows);
        }

        private static MoonpalaceCraterDoughBoundaryCandidateRow CopyCandidate(
            MoonpalaceCraterDoughBoundaryCandidateRow row,
            int? weight = null,
            MoonpalaceBoundaryOrientation? orientation = null)
        {
            return new MoonpalaceCraterDoughBoundaryCandidateRow(
                row.CandidateId,
                row.MicrochunkId,
                row.BiomeAId,
                row.BiomeBId,
                row.ProfileId,
                orientation ?? row.Orientation,
                row.RouteType,
                row.EntryEdgeSignatureId,
                row.ExitEdgeSignatureId,
                weight ?? row.Weight,
                row.Reversible,
                row.Active,
                row.MandatoryAllowed,
                row.ToolRequirement);
        }

        private static MoonpalaceCraterDoughSocketRow CopySocket(
            MoonpalaceCraterDoughSocketRow row,
            string side = null)
        {
            return new MoonpalaceCraterDoughSocketRow(
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
