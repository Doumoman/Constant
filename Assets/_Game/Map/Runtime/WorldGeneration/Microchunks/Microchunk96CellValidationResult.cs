using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class Microchunk96CellValidationResult
    {
        private readonly IReadOnlyList<Microchunk96CellValidationViolation> violations;

        public int EvaluatedMicrochunkCount { get; }
        public int EvaluatedRecordCount { get; }
        public int RecordCount => EvaluatedRecordCount;
        public int InRangeUniqueCoordinateCount { get; }
        public int UniqueCoordinateCount => InRangeUniqueCoordinateCount;
        public int MissingCoordinateCount { get; }
        public int MissingCount => MissingCoordinateCount;
        public int DuplicateCoordinateCount { get; }
        public int DuplicateCount => DuplicateCoordinateCount;
        public int OutOfRangeRecordCount { get; }
        public int OutOfRangeCount => OutOfRangeRecordCount;
        public int RowCountMismatchMicrochunkCount { get; }
        public int RowCountMismatchCount => RowCountMismatchMicrochunkCount;
        public int ExpectedRecordCount => EvaluatedMicrochunkCount * MicrochunkConstants.CellCount;
        public int RowCountDelta => EvaluatedRecordCount - ExpectedRecordCount;
        public bool HasRowCountMismatch => RowCountMismatchMicrochunkCount > 0;
        public int IssueCount => violations.Count;
        public bool Success => IssueCount == 0;
        public bool IsValid => Success;
        public IReadOnlyList<Microchunk96CellValidationViolation> Violations => violations;

        public Microchunk96CellValidationResult(
            int evaluatedMicrochunkCount,
            int evaluatedRecordCount,
            int inRangeUniqueCoordinateCount,
            int missingCoordinateCount,
            int duplicateCoordinateCount,
            int outOfRangeRecordCount,
            int rowCountMismatchMicrochunkCount,
            IEnumerable<Microchunk96CellValidationViolation> violations)
        {
            if (evaluatedMicrochunkCount < 0) throw new ArgumentOutOfRangeException(nameof(evaluatedMicrochunkCount));
            if (evaluatedRecordCount < 0) throw new ArgumentOutOfRangeException(nameof(evaluatedRecordCount));
            if (inRangeUniqueCoordinateCount < 0) throw new ArgumentOutOfRangeException(nameof(inRangeUniqueCoordinateCount));
            if (missingCoordinateCount < 0) throw new ArgumentOutOfRangeException(nameof(missingCoordinateCount));
            if (duplicateCoordinateCount < 0) throw new ArgumentOutOfRangeException(nameof(duplicateCoordinateCount));
            if (outOfRangeRecordCount < 0) throw new ArgumentOutOfRangeException(nameof(outOfRangeRecordCount));
            if (rowCountMismatchMicrochunkCount < 0) throw new ArgumentOutOfRangeException(nameof(rowCountMismatchMicrochunkCount));
            if (violations == null) throw new ArgumentNullException(nameof(violations));

            EvaluatedMicrochunkCount = evaluatedMicrochunkCount;
            EvaluatedRecordCount = evaluatedRecordCount;
            InRangeUniqueCoordinateCount = inRangeUniqueCoordinateCount;
            MissingCoordinateCount = missingCoordinateCount;
            DuplicateCoordinateCount = duplicateCoordinateCount;
            OutOfRangeRecordCount = outOfRangeRecordCount;
            RowCountMismatchMicrochunkCount = rowCountMismatchMicrochunkCount;
            var values = new List<Microchunk96CellValidationViolation>();
            foreach (var violation in violations)
            {
                if (violation == null)
                {
                    throw new ArgumentException("Violations cannot contain null.", nameof(violations));
                }
                values.Add(violation);
            }
            values.Sort(CompareViolations);
            this.violations = new ReadOnlyCollection<Microchunk96CellValidationViolation>(values);
        }

        private static int CompareViolations(
            Microchunk96CellValidationViolation left,
            Microchunk96CellValidationViolation right)
        {
            var comparison = left.MicrochunkId.CompareTo(right.MicrochunkId);
            if (comparison != 0) return comparison;

            comparison = ReasonOrder(left.Reason).CompareTo(ReasonOrder(right.Reason));
            if (comparison != 0) return comparison;

            comparison = CoordinateOrder(left).CompareTo(CoordinateOrder(right));
            if (comparison != 0) return comparison;

            return Nullable.Compare(left.SourceOrdinal, right.SourceOrdinal);
        }

        private static int ReasonOrder(string reason)
        {
            if (reason == Microchunk96CellValidationViolation.MissingCellRecordReason) return 0;
            if (reason == Microchunk96CellValidationViolation.DuplicateCellCoordinateReason) return 1;
            if (reason == Microchunk96CellValidationViolation.CellCoordinateOutOfRangeReason) return 2;
            return int.MaxValue;
        }

        private static long CoordinateOrder(Microchunk96CellValidationViolation violation)
        {
            if (violation.NormalizedLocalCoordinate.HasValue)
            {
                return violation.NormalizedLocalCoordinate.Value.RowMajorIndex;
            }

            if (violation.RawLocalX.HasValue && violation.RawLocalY.HasValue)
            {
                return ((long)violation.RawLocalY.Value * int.MaxValue) + violation.RawLocalX.Value;
            }

            return long.MaxValue;
        }
    }
}
