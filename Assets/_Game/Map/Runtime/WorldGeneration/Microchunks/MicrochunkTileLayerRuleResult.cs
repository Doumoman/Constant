using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkTileLayerRuleResult
    {
        private readonly IReadOnlyList<MicrochunkTileLayerRuleViolation> violations;

        public int TotalEvaluatedCells { get; }
        public IReadOnlyList<MicrochunkTileLayerRuleViolation> Violations => violations;
        public int ViolationCount => violations.Count;
        public bool Success => violations.Count == 0;

        public MicrochunkTileLayerRuleResult(
            int totalEvaluatedCells,
            IEnumerable<MicrochunkTileLayerRuleViolation> violations)
        {
            if (totalEvaluatedCells < 0) throw new ArgumentOutOfRangeException(nameof(totalEvaluatedCells));
            if (violations == null) throw new ArgumentNullException(nameof(violations));

            var ordered = new List<MicrochunkTileLayerRuleViolation>();
            foreach (var violation in violations)
            {
                if (violation == null) throw new ArgumentException("Violations cannot contain null.", nameof(violations));
                ordered.Add(violation);
            }

            ordered.Sort(CompareViolations);
            TotalEvaluatedCells = totalEvaluatedCells;
            this.violations = new ReadOnlyCollection<MicrochunkTileLayerRuleViolation>(ordered);
        }

        private static int CompareViolations(
            MicrochunkTileLayerRuleViolation left,
            MicrochunkTileLayerRuleViolation right)
        {
            var comparison = left.Coordinate.RowMajorIndex.CompareTo(right.Coordinate.RowMajorIndex);
            if (comparison != 0) return comparison;

            comparison = left.FirstLayer.CompareTo(right.FirstLayer);
            if (comparison != 0) return comparison;

            comparison = left.SecondLayer.CompareTo(right.SecondLayer);
            if (comparison != 0) return comparison;

            return string.Compare(left.Reason, right.Reason, StringComparison.Ordinal);
        }
    }
}
