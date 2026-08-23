using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkSocketEdgeValidationResult
    {
        private readonly IReadOnlyList<MicrochunkSocketEdgeValidationViolation> violations;

        public int EvaluatedSocketCount { get; }
        public int IssueCount => violations.Count;
        public bool Success => IssueCount == 0;
        public IReadOnlyList<MicrochunkSocketEdgeValidationViolation> Violations => violations;

        public MicrochunkSocketEdgeValidationResult(
            int evaluatedSocketCount,
            IEnumerable<MicrochunkSocketEdgeValidationViolation> violations)
        {
            if (evaluatedSocketCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(evaluatedSocketCount));
            }

            if (violations == null) throw new ArgumentNullException(nameof(violations));

            var values = new List<MicrochunkSocketEdgeValidationViolation>();
            foreach (var violation in violations)
            {
                if (violation == null)
                {
                    throw new ArgumentException("Violations cannot contain null.", nameof(violations));
                }

                values.Add(violation);
            }

            values.Sort(CompareViolations);
            EvaluatedSocketCount = evaluatedSocketCount;
            this.violations = new ReadOnlyCollection<MicrochunkSocketEdgeValidationViolation>(values);
        }

        private static int CompareViolations(
            MicrochunkSocketEdgeValidationViolation left,
            MicrochunkSocketEdgeValidationViolation right)
        {
            var comparison = string.Compare(left.SocketId, right.SocketId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;

            comparison = string.Compare(left.Reason, right.Reason, StringComparison.Ordinal);
            if (comparison != 0) return comparison;

            if (left.HasCoordinate != right.HasCoordinate)
            {
                return left.HasCoordinate ? 1 : -1;
            }

            if (left.HasCoordinate)
            {
                comparison = left.Coordinate.Value.CompareTo(right.Coordinate.Value);
                if (comparison != 0) return comparison;
            }

            comparison = left.Side.CompareTo(right.Side);
            if (comparison != 0) return comparison;

            comparison = string.Compare(left.BandId, right.BandId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;

            return string.Compare(
                left.EdgeSignatureId,
                right.EdgeSignatureId,
                StringComparison.Ordinal);
        }
    }
}
