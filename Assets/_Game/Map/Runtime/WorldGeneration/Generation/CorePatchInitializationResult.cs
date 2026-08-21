using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum CorePatchInitializationStatus
    {
        Completed,
        InvalidInput
    }

    public sealed class CorePatchInitializationResult
    {
        private readonly IReadOnlyList<CorePatchInitializationError> errors;

        private CorePatchInitializationResult(
            CorePatchInitializationStatus status,
            CorePatchInitializationPublication publication,
            CorePatchInitializationDiagnostics diagnostics,
            IEnumerable<CorePatchInitializationError> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            var ordered = SortAndDedupe(errors);
            if (status == CorePatchInitializationStatus.Completed)
            {
                if (publication == null || diagnostics == null || ordered.Count != 0)
                    throw new ArgumentException("Completed results require publication and diagnostics only.");
            }
            else if (status == CorePatchInitializationStatus.InvalidInput)
            {
                if (publication != null || diagnostics != null || ordered.Count == 0)
                    throw new ArgumentException("Invalid input results require errors only.");
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            Status = status;
            Publication = publication;
            Diagnostics = diagnostics;
            this.errors = new ReadOnlyCollection<CorePatchInitializationError>(ordered);
        }

        public CorePatchInitializationStatus Status { get; }
        public bool Succeeded => Status == CorePatchInitializationStatus.Completed;
        public bool RetryRequired => false;
        public CorePatchInitializationPublication Publication { get; }
        public CorePatchInitializationDiagnostics Diagnostics { get; }
        public IReadOnlyList<CorePatchInitializationError> Errors => errors;

        internal static CorePatchInitializationResult Completed(
            CorePatchInitializationPublication publication,
            CorePatchInitializationDiagnostics diagnostics)
        {
            return new CorePatchInitializationResult(
                CorePatchInitializationStatus.Completed,
                publication,
                diagnostics,
                Array.Empty<CorePatchInitializationError>());
        }

        internal static CorePatchInitializationResult Invalid(
            IEnumerable<CorePatchInitializationError> errors)
        {
            return new CorePatchInitializationResult(
                CorePatchInitializationStatus.InvalidInput,
                null,
                null,
                errors);
        }

        private static List<CorePatchInitializationError> SortAndDedupe(
            IEnumerable<CorePatchInitializationError> source)
        {
            var values = new List<CorePatchInitializationError>();
            foreach (var value in source)
                if (value != null) values.Add(value);
            values.Sort(Compare);

            var result = new List<CorePatchInitializationError>();
            foreach (var value in values)
                if (result.Count == 0 || Compare(result[result.Count - 1], value) != 0)
                    result.Add(value);
            return result;
        }

        private static int Compare(
            CorePatchInitializationError left,
            CorePatchInitializationError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = string.Compare(left.SourceReservationId, right.SourceReservationId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.BiomeId, right.BiomeId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.PatchRuleId, right.PatchRuleId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            if (value != 0) return value;
            return string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }
    }
}
