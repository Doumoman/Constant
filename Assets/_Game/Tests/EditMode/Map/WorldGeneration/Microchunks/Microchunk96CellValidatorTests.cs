using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.Map.Tests.WorldGeneration.Microchunks
{
    [TestFixture]
    [Category("MAP07_06")]
    public sealed class Microchunk96CellValidatorTests
    {
        private static readonly MicrochunkId DefaultId = new MicrochunkId("MC_96_CELL_TEST");

        public static IEnumerable<TestCaseData> EveryCoordinate
        {
            get
            {
                for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
                {
                    for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
                    {
                        var index = (y * MicrochunkConstants.WidthTiles) + x;
                        yield return new TestCaseData(x, y, index)
                            .SetName($"Coordinate_{index:D2}_{x}_{y}");
                    }
                }
            }
        }

        public static IEnumerable<TestCaseData> ApprovedTransforms => new[]
        {
            new TestCaseData(MicrochunkTransform.R0),
            new TestCaseData(MicrochunkTransform.MirrorX),
            new TestCaseData(MicrochunkTransform.MirrorY),
            new TestCaseData(MicrochunkTransform.R180)
        };

        [TestCaseSource(nameof(EveryCoordinate))]
        public void DimensionsAndExpectedCoordinateSequenceAreExact(int x, int y, int index)
        {
            var coordinate = new MicrochunkLocalCoord(x, y);

            Assert.That(MicrochunkConstants.WidthTiles, Is.EqualTo(12));
            Assert.That(MicrochunkConstants.HeightTiles, Is.EqualTo(8));
            Assert.That(MicrochunkConstants.CellCount, Is.EqualTo(96));
            Assert.That(coordinate.RowMajorIndex, Is.EqualTo(index));
        }

        [TestCaseSource(nameof(EveryCoordinate))]
        public void EverySingleCoordinateOmissionIsReported(int x, int y, int index)
        {
            var records = CompleteRecords()
                .Where(record => record.SourceOrdinal != index)
                .ToList();

            var result = new Microchunk96CellValidator().ValidateRecords(records);

            Assert.That(result.Success, Is.False);
            Assert.That(result.EvaluatedRecordCount, Is.EqualTo(95));
            Assert.That(result.InRangeUniqueCoordinateCount, Is.EqualTo(95));
            Assert.That(result.MissingCoordinateCount, Is.EqualTo(1));
            Assert.That(result.DuplicateCoordinateCount, Is.Zero);
            Assert.That(result.OutOfRangeRecordCount, Is.Zero);
            Assert.That(result.HasRowCountMismatch, Is.True);
            Assert.That(result.Violations, Has.Count.EqualTo(1));
            Assert.That(result.Violations[0].Reason,
                Is.EqualTo(Microchunk96CellValidationViolation.MissingCellRecordReason));
            Assert.That(result.Violations[0].NormalizedLocalCoordinate,
                Is.EqualTo(new MicrochunkLocalCoord(x, y)));
        }

        [TestCaseSource(nameof(EveryCoordinate))]
        public void EveryLegalCoordinateDuplicatedOnceIsDetected(int x, int y, int index)
        {
            var records = CompleteRecords();
            records.Add(Record(DefaultId, MicrochunkConstants.CellCount, x, y));

            var result = new Microchunk96CellValidator().ValidateRecords(records);

            Assert.That(result.Success, Is.False);
            Assert.That(result.EvaluatedRecordCount, Is.EqualTo(97));
            Assert.That(result.InRangeUniqueCoordinateCount, Is.EqualTo(96));
            Assert.That(result.MissingCoordinateCount, Is.Zero);
            Assert.That(result.DuplicateCoordinateCount, Is.EqualTo(1));
            Assert.That(result.HasRowCountMismatch, Is.True);
            Assert.That(result.Violations, Has.Count.EqualTo(1));
            Assert.That(result.Violations[0].Reason,
                Is.EqualTo(Microchunk96CellValidationViolation.DuplicateCellCoordinateReason));
            Assert.That(result.Violations[0].SourceOrdinal, Is.EqualTo(96));
            Assert.That(result.Violations[0].NormalizedLocalCoordinate,
                Is.EqualTo(new MicrochunkLocalCoord(x, y)));
        }

        [TestCaseSource(nameof(EveryCoordinate))]
        public void EveryLegalCoordinateCanBeTheOnlyDraftRow(int x, int y, int index)
        {
            var records = new[] { Record(DefaultId, index, x, y) };

            var result = new Microchunk96CellValidator().ValidateRecords(
                records,
                Microchunk96CellValidationPolicy.Partial);

            Assert.That(result.Success, Is.True);
            Assert.That(result.EvaluatedRecordCount, Is.EqualTo(1));
            Assert.That(result.InRangeUniqueCoordinateCount, Is.EqualTo(1));
            Assert.That(result.MissingCoordinateCount, Is.EqualTo(95));
            Assert.That(result.IssueCount, Is.Zero);
            Assert.That(result.HasRowCountMismatch, Is.True);
            Assert.That(result.Violations, Is.Empty);
        }

        [Test]
        public void RecordPolicyViolationAndResultAreImmutableSnapshots()
        {
            var cell = Cell(2, 3, "NONE");
            var record = new Microchunk96CellRecord(DefaultId, 7, 2, 3, cell);
            var source = CompleteRecords();
            var policy = new Microchunk96CellValidationPolicy(true);
            var result = new Microchunk96CellValidator().ValidateRecords(source, policy);
            var later = new Microchunk96CellValidationViolation(
                DefaultId, 4, 4, 0, new MicrochunkLocalCoord(4, 0),
                Microchunk96CellValidator.DuplicateCellCoordinateReason);
            var earlier = new Microchunk96CellValidationViolation(
                DefaultId, null, null, null, new MicrochunkLocalCoord(2, 0),
                Microchunk96CellValidator.MissingCellRecordReason);
            var violations = new List<Microchunk96CellValidationViolation> { later, earlier };
            var issueResult = new Microchunk96CellValidationResult(1, 96, 95, 1, 1, 0, 0, violations);
            source.Clear();
            violations.Clear();

            Assert.That(record.MicrochunkId, Is.EqualTo(DefaultId));
            Assert.That(record.SourceOrdinal, Is.EqualTo(7));
            Assert.That(record.RawLocalX, Is.EqualTo(2));
            Assert.That(record.RawLocalY, Is.EqualTo(3));
            Assert.That(record.NormalizedTileCell, Is.SameAs(cell));
            Assert.That(record.HasNormalizedTileCell, Is.True);
            Assert.That(policy.RequireCompleteCoverage, Is.True);
            Assert.That(result.EvaluatedRecordCount, Is.EqualTo(96));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Violations, Is.Empty);
            Assert.That(earlier.HasSourceOrdinal, Is.False);
            Assert.That(earlier.HasRawCoordinate, Is.False);
            Assert.That(earlier.HasNormalizedLocalCoordinate, Is.True);
            Assert.That(issueResult.IssueCount, Is.EqualTo(2));
            Assert.That(issueResult.Success, Is.False);
            Assert.That(issueResult.Violations, Is.EqualTo(new[] { earlier, later }));
        }

        [Test]
        public void DefaultAndDefinitionPoliciesSelectExpectedCoverageMode()
        {
            var complete = Definition(CompleteCells(), true);
            var draft = Definition(new[] { Cell(0, 0, "NONE") }, false);

            Assert.That(Microchunk96CellValidationPolicy.Default.RequireCompleteCoverage, Is.True);
            Assert.That(Microchunk96CellValidationPolicy.Complete.RequireCompleteCoverage, Is.True);
            Assert.That(Microchunk96CellValidationPolicy.Partial.RequireCompleteCoverage, Is.False);
            Assert.That(Microchunk96CellValidationPolicy.Draft.RequireCompleteCoverage, Is.False);
            Assert.That(Microchunk96CellValidationPolicy.ForDefinition(complete).RequireCompleteCoverage, Is.True);
            Assert.That(Microchunk96CellValidationPolicy.ForDefinition(draft).RequireCompleteCoverage, Is.False);
        }

        [Test]
        public void CompleteRecordSetHasExactSuccessfulSummary()
        {
            var result = new Microchunk96CellValidator().ValidateRecords(CompleteRecords());

            Assert.That(result.EvaluatedMicrochunkCount, Is.EqualTo(1));
            Assert.That(result.EvaluatedRecordCount, Is.EqualTo(96));
            Assert.That(result.ExpectedRecordCount, Is.EqualTo(96));
            Assert.That(result.RowCountDelta, Is.Zero);
            Assert.That(result.HasRowCountMismatch, Is.False);
            Assert.That(result.InRangeUniqueCoordinateCount, Is.EqualTo(96));
            Assert.That(result.MissingCoordinateCount, Is.Zero);
            Assert.That(result.DuplicateCoordinateCount, Is.Zero);
            Assert.That(result.OutOfRangeRecordCount, Is.Zero);
            Assert.That(result.IssueCount, Is.Zero);
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void CompleteAllNoneRowsAreExplicitValidRecords()
        {
            var records = CompleteRecords("NONE");

            var result = new Microchunk96CellValidator().ValidateRecords(records);

            Assert.That(records, Has.Count.EqualTo(96));
            Assert.That(records.All(record => record.NormalizedTileCell.GroundCode == "NONE"), Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(result.InRangeUniqueCoordinateCount, Is.EqualTo(96));
        }

        [Test]
        public void MultipleMissingCoordinatesAreOrderedRowMajor()
        {
            var omitted = new[] { 95, 13, 2 };
            var records = CompleteRecords()
                .Where(record => !omitted.Contains(record.SourceOrdinal))
                .Reverse()
                .ToList();

            var result = new Microchunk96CellValidator().ValidateRecords(records);

            Assert.That(result.MissingCoordinateCount, Is.EqualTo(3));
            Assert.That(result.Violations.Select(value => value.NormalizedLocalCoordinate.Value.RowMajorIndex),
                Is.EqualTo(new[] { 2, 13, 95 }));
        }

        [Test]
        public void DuplicateDoesNotMaskMissingCoordinate()
        {
            var records = CompleteRecords()
                .Where(record => record.SourceOrdinal != 5)
                .ToList();
            records.Add(Record(DefaultId, 96, 4, 0));

            var result = new Microchunk96CellValidator().ValidateRecords(records);

            Assert.That(result.EvaluatedRecordCount, Is.EqualTo(96));
            Assert.That(result.InRangeUniqueCoordinateCount, Is.EqualTo(95));
            Assert.That(result.MissingCoordinateCount, Is.EqualTo(1));
            Assert.That(result.DuplicateCoordinateCount, Is.EqualTo(1));
            Assert.That(result.Violations.Select(value => value.Reason), Is.EqualTo(new[]
            {
                Microchunk96CellValidationViolation.MissingCellRecordReason,
                Microchunk96CellValidationViolation.DuplicateCellCoordinateReason
            }));
        }

        [TestCase(-1, 0)]
        [TestCase(12, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 8)]
        public void EveryOutOfRangeBoundaryDirectionIsRejected(int rawX, int rawY)
        {
            var records = CompleteRecords();
            records.Add(new Microchunk96CellRecord(DefaultId, 96, rawX, rawY));

            var result = new Microchunk96CellValidator().ValidateRecords(records);

            Assert.That(result.Success, Is.False);
            Assert.That(result.OutOfRangeRecordCount, Is.EqualTo(1));
            Assert.That(result.MissingCoordinateCount, Is.Zero);
            Assert.That(result.HasRowCountMismatch, Is.True);
            Assert.That(result.Violations[0].Reason,
                Is.EqualTo(Microchunk96CellValidationViolation.CellCoordinateOutOfRangeReason));
            Assert.That(result.Violations[0].RawLocalX, Is.EqualTo(rawX));
            Assert.That(result.Violations[0].RawLocalY, Is.EqualTo(rawY));
        }

        [Test]
        public void OutOfRangeRowDoesNotSatisfyMissingCoordinate()
        {
            var records = CompleteRecords()
                .Where(record => record.SourceOrdinal != 0)
                .ToList();
            records.Add(new Microchunk96CellRecord(DefaultId, 96, -1, 0));

            var result = new Microchunk96CellValidator().ValidateRecords(records);

            Assert.That(result.EvaluatedRecordCount, Is.EqualTo(96));
            Assert.That(result.InRangeUniqueCoordinateCount, Is.EqualTo(95));
            Assert.That(result.MissingCoordinateCount, Is.EqualTo(1));
            Assert.That(result.OutOfRangeRecordCount, Is.EqualTo(1));
            Assert.That(result.HasRowCountMismatch, Is.True);
            Assert.That(result.IssueCount, Is.EqualTo(2));
        }

        [Test]
        public void PartialPolicyStillRejectsDuplicateAndOutOfRangeRows()
        {
            var records = new List<Microchunk96CellRecord>
            {
                Record(DefaultId, 0, 0, 0),
                Record(DefaultId, 1, 0, 0),
                new Microchunk96CellRecord(DefaultId, 2, 12, 0)
            };

            var result = new Microchunk96CellValidator().ValidateRecords(
                records,
                Microchunk96CellValidationPolicy.Partial);

            Assert.That(result.Success, Is.False);
            Assert.That(result.MissingCoordinateCount, Is.EqualTo(95));
            Assert.That(result.DuplicateCoordinateCount, Is.EqualTo(1));
            Assert.That(result.OutOfRangeRecordCount, Is.EqualTo(1));
            Assert.That(result.IssueCount, Is.EqualTo(2));
            Assert.That(result.Violations.Any(value =>
                value.Reason == Microchunk96CellValidationViolation.MissingCellRecordReason), Is.False);
        }

        [Test]
        public void CompletePolicyRejectsOtherwiseValidSparseRows()
        {
            var result = new Microchunk96CellValidator().ValidateRecords(
                new[] { Record(DefaultId, 0, 0, 0) },
                Microchunk96CellValidationPolicy.Complete);

            Assert.That(result.Success, Is.False);
            Assert.That(result.MissingCoordinateCount, Is.EqualTo(95));
            Assert.That(result.IssueCount, Is.EqualTo(95));
        }

        [Test]
        public void DefinitionProjectionUsesCompleteOrDraftPolicyWithoutMutation()
        {
            var completeCells = CompleteCells();
            var complete = Definition(completeCells, true);
            var draftCell = Cell(3, 4, "NONE");
            var draft = Definition(new[] { draftCell }, false);

            var completeResult = new Microchunk96CellValidator().ValidateDefinition(complete);
            var draftResult = new Microchunk96CellValidator().ValidateDefinition(draft);

            Assert.That(completeResult.Success, Is.True);
            Assert.That(completeResult.InRangeUniqueCoordinateCount, Is.EqualTo(96));
            Assert.That(draftResult.Success, Is.True);
            Assert.That(draftResult.MissingCoordinateCount, Is.EqualTo(95));
            Assert.That(complete.TileCells, Has.Count.EqualTo(96));
            Assert.That(draft.TileCells, Has.Count.EqualTo(1));
            Assert.That(draft.TileCells[0], Is.SameAs(draftCell));
        }

        [Test]
        public void TileLayerCompatibilityIsDeliberatelyOutOfScope()
        {
            var records = CompleteRecords();
            var incompatible = new MicrochunkTileCell(
                new MicrochunkLocalCoord(0, 0),
                "G_SOLID", "OW_PLATFORM", "B_BREAK", "HZ_SPIKE",
                "L_WATER", "DB_VINE", "DF_FLOWER", "M_ROUTE");
            records[0] = new Microchunk96CellRecord(DefaultId, 0, 0, 0, incompatible);

            var result = new Microchunk96CellValidator().ValidateRecords(records);

            Assert.That(result.Success, Is.True);
            Assert.That(records[0].NormalizedTileCell, Is.SameAs(incompatible));
        }

        [TestCaseSource(nameof(ApprovedTransforms))]
        public void EveryApprovedTransformPreservesCompleteCoverage(MicrochunkTransform transform)
        {
            var source = Definition(CompleteCells(), true);
            var transformed = MicrochunkTransformer.Transform(source, transform).Definition;

            var result = new Microchunk96CellValidator().ValidateDefinition(transformed);

            Assert.That(result.Success, Is.True);
            Assert.That(result.EvaluatedRecordCount, Is.EqualTo(96));
            Assert.That(result.InRangeUniqueCoordinateCount, Is.EqualTo(96));
            Assert.That(source.TileCells, Has.Count.EqualTo(96));
        }

        [Test]
        public void MultipleMicrochunksAndViolationsUseStableOrdering()
        {
            var idB = new MicrochunkId("MC_B");
            var idA = new MicrochunkId("MC_A");
            var records = new[]
            {
                Record(idB, 2, 0, 0),
                new Microchunk96CellRecord(idA, 3, -1, 0),
                Record(idA, 1, 0, 0),
                Record(idA, 2, 0, 0)
            };

            var result = new Microchunk96CellValidator().ValidateRecords(records);

            Assert.That(result.EvaluatedMicrochunkCount, Is.EqualTo(2));
            Assert.That(result.Violations.First().MicrochunkId, Is.EqualTo(idA));
            Assert.That(result.Violations.Last().MicrochunkId, Is.EqualTo(idB));
            Assert.That(result.Violations.Where(value => value.MicrochunkId == idA)
                .Select(value => value.Reason).Distinct(), Is.EqualTo(new[]
            {
                Microchunk96CellValidationViolation.MissingCellRecordReason,
                Microchunk96CellValidationViolation.DuplicateCellCoordinateReason,
                Microchunk96CellValidationViolation.CellCoordinateOutOfRangeReason
            }));
        }

        [Test]
        public void ExplicitMicrochunkOverloadCanValidateAnEmptyDraftGroup()
        {
            var result = new Microchunk96CellValidator().ValidateRecords(
                DefaultId,
                Array.Empty<Microchunk96CellRecord>(),
                Microchunk96CellValidationPolicy.Partial);

            Assert.That(result.EvaluatedMicrochunkCount, Is.EqualTo(1));
            Assert.That(result.EvaluatedRecordCount, Is.Zero);
            Assert.That(result.MissingCoordinateCount, Is.EqualTo(96));
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void InvalidInputsAreRejectedBeforeValidation()
        {
            var validator = new Microchunk96CellValidator();

            Assert.Throws<ArgumentNullException>(() => validator.ValidateRecords(null));
            Assert.Throws<ArgumentNullException>(() => validator.ValidateRecords(CompleteRecords(), null));
            Assert.Throws<ArgumentNullException>(() => validator.ValidateDefinition(null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Microchunk96CellRecord(DefaultId, -1, 0, 0));
            Assert.Throws<ArgumentException>(() => validator.ValidateRecords(
                DefaultId,
                new[] { Record(new MicrochunkId("OTHER"), 0, 0, 0) },
                Microchunk96CellValidationPolicy.Complete));
        }

        private static List<Microchunk96CellRecord> CompleteRecords(string groundCode = "G_TEST")
        {
            var records = new List<Microchunk96CellRecord>(MicrochunkConstants.CellCount);
            for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
            {
                for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
                {
                    var ordinal = (y * MicrochunkConstants.WidthTiles) + x;
                    records.Add(Record(DefaultId, ordinal, x, y, groundCode));
                }
            }
            return records;
        }

        private static List<MicrochunkTileCell> CompleteCells()
        {
            var cells = new List<MicrochunkTileCell>(MicrochunkConstants.CellCount);
            for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
            {
                for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
                {
                    cells.Add(Cell(x, y, "G_TEST"));
                }
            }
            return cells;
        }

        private static Microchunk96CellRecord Record(
            MicrochunkId id,
            int ordinal,
            int x,
            int y,
            string groundCode = "G_TEST")
        {
            return new Microchunk96CellRecord(id, ordinal, x, y, Cell(x, y, groundCode));
        }

        private static MicrochunkTileCell Cell(int x, int y, string groundCode)
        {
            return new MicrochunkTileCell(
                new MicrochunkLocalCoord(x, y),
                groundCode,
                "NONE", "NONE", "NONE", "NONE", "NONE", "NONE", "NONE");
        }

        private static MicrochunkDefinition Definition(
            IEnumerable<MicrochunkTileCell> cells,
            bool complete)
        {
            return new MicrochunkDefinition(
                DefaultId,
                "96 Cell Test",
                MicrochunkConstants.WidthTiles,
                MicrochunkConstants.HeightTiles,
                MicrochunkUsageClass.Traversal,
                new[] { "BIO_TEST" },
                new[] { "MANDATORY" },
                new[]
                {
                    MicrochunkTransform.R0,
                    MicrochunkTransform.MirrorX,
                    MicrochunkTransform.MirrorY,
                    MicrochunkTransform.R180
                },
                100,
                0,
                0,
                0,
                complete,
                "PREFAB_MC_96_CELL_TEST",
                true,
                string.Empty,
                cells,
                Array.Empty<MicrochunkSocketDefinition>(),
                Array.Empty<MicrochunkObjectSlotDefinition>());
        }
    }
}
