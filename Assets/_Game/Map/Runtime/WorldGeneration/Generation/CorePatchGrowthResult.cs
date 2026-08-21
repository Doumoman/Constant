using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum CorePatchGrowthStatus
    {
        Completed,
        InvalidInput,
        RetryRequired
    }

    public sealed class CorePatchGrowthResult
    {
        private readonly IReadOnlyList<CorePatchGrowthError> errors;

        private CorePatchGrowthResult(
            CorePatchGrowthStatus status,
            CorePatchGrowthPublication publication,
            CorePatchGrowthDiagnostics diagnostics,
            IEnumerable<CorePatchGrowthError> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            var ordered = SortAndDedupe(errors);
            switch (status)
            {
                case CorePatchGrowthStatus.Completed:
                    if (publication == null || diagnostics == null || ordered.Count != 0)
                        throw new ArgumentException("Completed growth requires publication and diagnostics only.");
                    break;
                case CorePatchGrowthStatus.InvalidInput:
                    if (publication != null || diagnostics != null || ordered.Count == 0)
                        throw new ArgumentException("Invalid growth requires structural errors only.");
                    break;
                case CorePatchGrowthStatus.RetryRequired:
                    if (publication != null || diagnostics == null || ordered.Count == 0)
                        throw new ArgumentException("Retry growth requires diagnostics and spatial errors only.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status));
            }

            Status = status;
            Publication = publication;
            Diagnostics = diagnostics;
            this.errors = new ReadOnlyCollection<CorePatchGrowthError>(ordered);
        }

        public CorePatchGrowthStatus Status { get; }
        public bool Succeeded => Status == CorePatchGrowthStatus.Completed;
        public bool RetryRequired => Status == CorePatchGrowthStatus.RetryRequired;
        public CorePatchGrowthPublication Publication { get; }
        public CorePatchGrowthDiagnostics Diagnostics { get; }
        public IReadOnlyList<CorePatchGrowthError> Errors => errors;

        internal static CorePatchGrowthResult Completed(
            CorePatchGrowthPublication publication,
            CorePatchGrowthDiagnostics diagnostics)
        {
            return new CorePatchGrowthResult(
                CorePatchGrowthStatus.Completed,
                publication,
                diagnostics,
                Array.Empty<CorePatchGrowthError>());
        }

        internal static CorePatchGrowthResult Invalid(IEnumerable<CorePatchGrowthError> errors)
        {
            return new CorePatchGrowthResult(
                CorePatchGrowthStatus.InvalidInput,
                null,
                null,
                errors);
        }

        internal static CorePatchGrowthResult Retry(
            CorePatchGrowthDiagnostics diagnostics,
            IEnumerable<CorePatchGrowthError> errors)
        {
            return new CorePatchGrowthResult(
                CorePatchGrowthStatus.RetryRequired,
                null,
                diagnostics,
                errors);
        }

        private static List<CorePatchGrowthError> SortAndDedupe(
            IEnumerable<CorePatchGrowthError> source)
        {
            var values = new List<CorePatchGrowthError>();
            foreach (var value in source) if (value != null) values.Add(value);
            values.Sort(Compare);
            var result = new List<CorePatchGrowthError>();
            foreach (var value in values)
                if (result.Count == 0 || Compare(result[result.Count - 1], value) != 0)
                    result.Add(value);
            return result;
        }

        internal static int Compare(CorePatchGrowthError left, CorePatchGrowthError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = left.SourceReservationId.CompareTo(right.SourceReservationId);
            if (value != 0) return value;
            value = left.OtherSourceReservationId.CompareTo(right.OtherSourceReservationId);
            if (value != 0) return value;
            value = left.PatchId.CompareTo(right.PatchId);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            if (value != 0) return value;
            value = left.RequiredCount.CompareTo(right.RequiredCount);
            if (value != 0) return value;
            value = left.AvailableCount.CompareTo(right.AvailableCount);
            if (value != 0) return value;
            return string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }
    }
}
