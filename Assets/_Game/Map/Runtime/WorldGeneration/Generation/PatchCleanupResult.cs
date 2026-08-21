using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum PatchCleanupStatus
    {
        Completed,
        InvalidInput,
        RetryRequired
    }

    public sealed class PatchCleanupResult
    {
        private readonly IReadOnlyList<PatchCleanupError> errors;

        private PatchCleanupResult(
            PatchCleanupStatus status,
            PatchCleanupPublication publication,
            PatchCleanupDiagnostics diagnostics,
            IEnumerable<PatchCleanupError> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            var ordered = SortAndDedupe(errors);
            if (status == PatchCleanupStatus.Completed)
            {
                if (publication == null || diagnostics == null || ordered.Count != 0 || diagnostics.Rollback)
                    throw new ArgumentException("Completed cleanup requires publication and non-rollback diagnostics only.");
            }
            else if (status == PatchCleanupStatus.InvalidInput)
            {
                if (publication != null || diagnostics != null || ordered.Count == 0)
                    throw new ArgumentException("Invalid cleanup requires structural errors only.");
            }
            else if (status == PatchCleanupStatus.RetryRequired)
            {
                if (publication != null || diagnostics == null || ordered.Count == 0 || !diagnostics.Rollback)
                    throw new ArgumentException("Retry cleanup requires rollback diagnostics and errors only.");
            }
            else throw new ArgumentOutOfRangeException(nameof(status));

            Status = status;
            Publication = publication;
            Diagnostics = diagnostics;
            this.errors = new ReadOnlyCollection<PatchCleanupError>(ordered);
        }

        public PatchCleanupStatus Status { get; }
        public bool Succeeded => Status == PatchCleanupStatus.Completed;
        public bool RetryRequired => Status == PatchCleanupStatus.RetryRequired;
        public PatchCleanupPublication Publication { get; }
        public PatchCleanupDiagnostics Diagnostics { get; }
        public IReadOnlyList<PatchCleanupError> Errors => errors;

        internal static PatchCleanupResult Completed(
            PatchCleanupPublication publication,
            PatchCleanupDiagnostics diagnostics)
        {
            return new PatchCleanupResult(
                PatchCleanupStatus.Completed, publication, diagnostics,
                Array.Empty<PatchCleanupError>());
        }

        internal static PatchCleanupResult Invalid(IEnumerable<PatchCleanupError> errors)
        {
            return new PatchCleanupResult(PatchCleanupStatus.InvalidInput, null, null, errors);
        }

        internal static PatchCleanupResult Retry(
            PatchCleanupDiagnostics diagnostics,
            IEnumerable<PatchCleanupError> errors)
        {
            return new PatchCleanupResult(PatchCleanupStatus.RetryRequired, null, diagnostics, errors);
        }

        internal static int Compare(PatchCleanupError left, PatchCleanupError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = string.Compare(left.DefinitionId, right.DefinitionId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            if (value != 0) return value;
            value = left.RequiredCount.CompareTo(right.RequiredCount);
            if (value != 0) return value;
            value = left.AvailableCount.CompareTo(right.AvailableCount);
            if (value != 0) return value;
            return string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        private static List<PatchCleanupError> SortAndDedupe(IEnumerable<PatchCleanupError> source)
        {
            var values = new List<PatchCleanupError>();
            foreach (var value in source) if (value != null) values.Add(value);
            values.Sort(Compare);
            var result = new List<PatchCleanupError>();
            foreach (var value in values)
                if (result.Count == 0 || Compare(result[result.Count - 1], value) != 0)
                    result.Add(value);
            return result;
        }
    }
}
