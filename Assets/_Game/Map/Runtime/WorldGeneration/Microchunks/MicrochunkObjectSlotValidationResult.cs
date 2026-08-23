using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkObjectSlotValidationResult
    {
        private readonly IReadOnlyList<MicrochunkObjectSlotValidationViolation> violations;

        public int EvaluatedSlotCount { get; }
        public IReadOnlyList<MicrochunkObjectSlotValidationViolation> Violations => violations;
        public int IssueCount => violations.Count;
        public bool Success => IssueCount == 0;

        public MicrochunkObjectSlotValidationResult(
            int evaluatedSlotCount,
            IEnumerable<MicrochunkObjectSlotValidationViolation> violations)
        {
            if (evaluatedSlotCount < 0) throw new ArgumentOutOfRangeException(nameof(evaluatedSlotCount));
            if (violations == null) throw new ArgumentNullException(nameof(violations));

            var values = new List<MicrochunkObjectSlotValidationViolation>();
            foreach (var violation in violations)
            {
                if (violation == null)
                {
                    throw new ArgumentException("Violations cannot contain null.", nameof(violations));
                }
                values.Add(violation);
            }
            values.Sort(CompareViolations);

            EvaluatedSlotCount = evaluatedSlotCount;
            this.violations = new ReadOnlyCollection<MicrochunkObjectSlotValidationViolation>(values);
        }

        private static int CompareViolations(
            MicrochunkObjectSlotValidationViolation left,
            MicrochunkObjectSlotValidationViolation right)
        {
            var comparison = string.Compare(left.SlotId, right.SlotId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(left.Reason, right.Reason, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = NullableRowMajorIndex(left.Coordinate).CompareTo(NullableRowMajorIndex(right.Coordinate));
            if (comparison != 0) return comparison;
            return string.Compare(left.ComparedSlotId, right.ComparedSlotId, StringComparison.Ordinal);
        }

        private static int NullableRowMajorIndex(MicrochunkLocalCoord? coordinate)
        {
            return coordinate.HasValue ? coordinate.Value.RowMajorIndex : -1;
        }
    }
}
