using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Boundaries
{
    [Category("MAP08_09")]
    public sealed class MoonpalaceRootMillBoundaryValidatorTests
    {
        private RootMillAuthoringEvidence evidence;
        private MoonpalaceRootMillBoundaryValidator validator;

        public static IEnumerable<TestCaseData> ValidationCases
        {
            get
            {
                for (var caseId = 0; caseId < 360; caseId++)
                {
                    yield return new TestCaseData(caseId)
                        .SetName("RootMillBoundaryValidatorContract_" + caseId.ToString("D3"));
                }
            }
        }

        [OneTimeSetUp]
        public void LoadAuthoringEvidence()
        {
            evidence = RootMillAuthoringHarness.GetOrCreate();
            validator = new MoonpalaceRootMillBoundaryValidator();
        }

        [TestCaseSource(nameof(ValidationCases))]
        public void RootMillBoundaryValidatorContract(int caseId)
        {
            var data = evidence.Data;
            var report = evidence.Report;
            var microchunkId = MoonpalaceRootMillBoundaryAuthoringContract.MicrochunkIds[caseId % 6];
            switch (caseId % 20)
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
                        Is.EquivalentTo(MoonpalaceRootMillBoundaryAuthoringContract.CandidateIds));
                    Assert.That(report.MicrochunkIds,
                        Is.EquivalentTo(MoonpalaceRootMillBoundaryAuthoringContract.MicrochunkIds));
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
                    var index = MoonpalaceRootMillBoundaryCandidateMatrix.Canonical.Index;
                    Assert.That(index.Count, Is.EqualTo(6));
                    Assert.That(index.GetCandidates(MoonpalaceRootMillBoundaryAuthoringContract.Pair).Count,
                        Is.EqualTo(6));
                    break;
                case 6:
                    var candidateId = MoonpalaceRootMillBoundaryAuthoringContract.CandidateIds[caseId % 6];
                    Assert.That(MoonpalaceRootMillBoundaryCandidateMatrix.Canonical.GetMicrochunkId(candidateId),
                        Is.EqualTo(microchunkId));
                    break;
                case 7:
                    AssertRejected(ReplaceFirstCandidate(data, row => CopyCandidate(row, weight: 0)), "weight");
                    break;
                case 8:
                    AssertRejected(new MoonpalaceRootMillBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles.Skip(1), data.Sockets), "tile row count");
                    break;
                case 9:
                    AssertRejected(ReplaceFirstSocket(data, row => CopySocket(row, side: "X")), "socket shape");
                    break;
                case 10:
                    AssertRejected(new MoonpalaceRootMillBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets, generatedCsvCreated: 1),
                        "Generated CSV");
                    break;
                case 11:
                    AssertRejected(new MoonpalaceRootMillBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets, otherPairRowsModified: 1),
                        "Other pair");
                    break;
                case 12:
                    AssertRejected(new MoonpalaceRootMillBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets,
                        existingRowsModified: 1), "Existing rows");
                    break;
                case 13:
                    AssertRejected(new MoonpalaceRootMillBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets,
                        craterRootRowsModified: 1), "Crater/Root");
                    break;
                case 14:
                    AssertRejected(new MoonpalaceRootMillBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets,
                        craterMillRowsModified: 1), "Crater/Mill");
                    break;
                case 15:
                    AssertRejected(new MoonpalaceRootMillBoundaryAuthoringData(
                        data.Candidates, data.Microchunks, data.Tiles, data.Sockets,
                        craterDoughRowsModified: 1), "Crater/Dough");
                    break;
                case 16:
                    var repeated = validator.Validate(data);
                    Assert.That(repeated.Issues, Is.EqualTo(report.Issues));
                    Assert.That(repeated.CandidateIds, Is.EqualTo(report.CandidateIds));
                    break;
                case 17:
                    var mutable = data.Candidates.ToList();
                    var snapshot = new MoonpalaceRootMillBoundaryAuthoringData(
                        mutable, data.Microchunks, data.Tiles, data.Sockets);
                    mutable.Clear();
                    Assert.That(snapshot.Candidates.Count, Is.EqualTo(6));
                    break;
                case 18:
                    Assert.Throws<ArgumentNullException>(() => validator.Validate(null));
                    Assert.Throws<KeyNotFoundException>(() =>
                        MoonpalaceRootMillBoundaryCandidateMatrix.Canonical.GetMicrochunkId("UNKNOWN"));
                    break;
                default:
                    Assert.That(report.GeneratedCsvCreated, Is.Zero);
                    Assert.That(report.ExistingRowsModified, Is.Zero);
                    Assert.That(report.OtherPairRowsModified, Is.Zero);
                    Assert.That(report.CraterRootRowsModified, Is.Zero);
                    Assert.That(report.CraterMillRowsModified, Is.Zero);
                    Assert.That(report.CraterDoughRowsModified, Is.Zero);
                    Assert.That(report.ProfileOrientationMatrixComplete, Is.True);
                    break;
            }
        }

        private void AssertRejected(MoonpalaceRootMillBoundaryAuthoringData data, string issueFragment)
        {
            var result = validator.Validate(data);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue =>
                issue.IndexOf(issueFragment, StringComparison.OrdinalIgnoreCase) >= 0), Is.True,
                string.Join("\n", result.Issues));
        }

        private static MoonpalaceRootMillBoundaryAuthoringData ReplaceFirstCandidate(
            MoonpalaceRootMillBoundaryAuthoringData data,
            Func<MoonpalaceRootMillBoundaryCandidateRow, MoonpalaceRootMillBoundaryCandidateRow> replace)
        {
            var rows = data.Candidates.ToList();
            rows[0] = replace(rows[0]);
            return new MoonpalaceRootMillBoundaryAuthoringData(rows, data.Microchunks, data.Tiles, data.Sockets);
        }

        private static MoonpalaceRootMillBoundaryAuthoringData ReplaceFirstSocket(
            MoonpalaceRootMillBoundaryAuthoringData data,
            Func<MoonpalaceRootMillSocketRow, MoonpalaceRootMillSocketRow> replace)
        {
            var rows = data.Sockets.ToList();
            rows[0] = replace(rows[0]);
            return new MoonpalaceRootMillBoundaryAuthoringData(data.Candidates, data.Microchunks, data.Tiles, rows);
        }

        private static MoonpalaceRootMillBoundaryCandidateRow CopyCandidate(
            MoonpalaceRootMillBoundaryCandidateRow row,
            int? weight = null)
        {
            return new MoonpalaceRootMillBoundaryCandidateRow(
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

        private static MoonpalaceRootMillSocketRow CopySocket(
            MoonpalaceRootMillSocketRow row,
            string side = null)
        {
            return new MoonpalaceRootMillSocketRow(
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
