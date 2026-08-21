using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum IntrusionPlacementStatus
    {
        Completed,
        InvalidInput,
        RetryRequired
    }

    public sealed class IntrusionPlacementResult
    {
        private readonly IReadOnlyList<IntrusionPlacementError> errors;

        private IntrusionPlacementResult(
            IntrusionPlacementStatus status,
            IntrusionPlacementPublication publication,
            IntrusionPlacementDiagnostics diagnostics,
            IEnumerable<IntrusionPlacementError> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            var ordered = SortAndDedupe(errors);
            if (status == IntrusionPlacementStatus.Completed)
            {
                if (publication == null || diagnostics == null || ordered.Count != 0)
                    throw new ArgumentException("Completed placement requires publication and diagnostics only.");
            }
            else if (status == IntrusionPlacementStatus.InvalidInput)
            {
                if (publication != null || diagnostics != null || ordered.Count == 0)
                    throw new ArgumentException("Invalid placement requires structural errors only.");
            }
            else if (status == IntrusionPlacementStatus.RetryRequired)
            {
                if (publication != null || diagnostics == null || ordered.Count == 0)
                    throw new ArgumentException("Retry placement requires diagnostics and errors only.");
            }
            else throw new ArgumentOutOfRangeException(nameof(status));

            Status = status;
            Publication = publication;
            Diagnostics = diagnostics;
            this.errors = new ReadOnlyCollection<IntrusionPlacementError>(ordered);
        }

        public IntrusionPlacementStatus Status { get; }
        public bool Succeeded => Status == IntrusionPlacementStatus.Completed;
        public bool RetryRequired => Status == IntrusionPlacementStatus.RetryRequired;
        public IntrusionPlacementPublication Publication { get; }
        public IntrusionPlacementDiagnostics Diagnostics { get; }
        public IReadOnlyList<IntrusionPlacementError> Errors => errors;

        internal static IntrusionPlacementResult Completed(
            IntrusionPlacementPublication publication,
            IntrusionPlacementDiagnostics diagnostics)
        {
            return new IntrusionPlacementResult(
                IntrusionPlacementStatus.Completed, publication, diagnostics,
                Array.Empty<IntrusionPlacementError>());
        }

        internal static IntrusionPlacementResult Invalid(IEnumerable<IntrusionPlacementError> errors)
        {
            return new IntrusionPlacementResult(
                IntrusionPlacementStatus.InvalidInput, null, null, errors);
        }

        internal static IntrusionPlacementResult Retry(
            IntrusionPlacementDiagnostics diagnostics,
            IEnumerable<IntrusionPlacementError> errors)
        {
            return new IntrusionPlacementResult(
                IntrusionPlacementStatus.RetryRequired, null, diagnostics, errors);
        }

        internal static int Compare(IntrusionPlacementError left, IntrusionPlacementError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = string.Compare(left.DefinitionId, right.DefinitionId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.IntruderBiomeId, right.IntruderBiomeId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.HostBiomeId, right.HostBiomeId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = left.IntrusionOrdinal.CompareTo(right.IntrusionOrdinal);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            if (value != 0) return value;
            value = left.RequiredCount.CompareTo(right.RequiredCount);
            if (value != 0) return value;
            value = left.AvailableCount.CompareTo(right.AvailableCount);
            if (value != 0) return value;
            return string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        private static List<IntrusionPlacementError> SortAndDedupe(
            IEnumerable<IntrusionPlacementError> source)
        {
            var values = new List<IntrusionPlacementError>();
            foreach (var value in source) if (value != null) values.Add(value);
            values.Sort(Compare);
            var result = new List<IntrusionPlacementError>();
            foreach (var value in values)
                if (result.Count == 0 || Compare(result[result.Count - 1], value) != 0)
                    result.Add(value);
            return result;
        }
    }
}
