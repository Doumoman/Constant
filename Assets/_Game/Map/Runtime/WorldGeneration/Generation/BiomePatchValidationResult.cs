using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum BiomePatchValidationStatus
    {
        Completed,
        ValidationRejected,
        InvalidInput
    }

    public sealed class BiomePatchValidationResult
    {
        private readonly IReadOnlyList<BiomePatchValidationViolation> violations;
        private readonly IReadOnlyList<BiomePatchValidationError> errors;

        private BiomePatchValidationResult(
            BiomePatchValidationStatus status,
            BiomePatchValidationPublication publication,
            BiomePatchValidationDiagnostics diagnostics,
            IEnumerable<BiomePatchValidationViolation> violations,
            IEnumerable<BiomePatchValidationError> errors)
        {
            if (violations == null) throw new ArgumentNullException(nameof(violations));
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            var orderedViolations = SortAndDedupeViolations(violations);
            var orderedErrors = SortAndDedupeErrors(errors);
            if (status == BiomePatchValidationStatus.Completed)
            {
                if (publication == null || diagnostics == null || orderedViolations.Count != 0 || orderedErrors.Count != 0)
                    throw new ArgumentException("Completed validation requires publication and zero issues.");
            }
            else if (status == BiomePatchValidationStatus.ValidationRejected)
            {
                if (publication != null || diagnostics == null || orderedViolations.Count == 0 || orderedErrors.Count != 0)
                    throw new ArgumentException("Rejected validation requires diagnostics and violations only.");
            }
            else if (status == BiomePatchValidationStatus.InvalidInput)
            {
                if (publication != null || diagnostics != null || orderedViolations.Count != 0 || orderedErrors.Count == 0)
                    throw new ArgumentException("Invalid input requires structural errors only.");
            }
            else throw new ArgumentOutOfRangeException(nameof(status));

            Status = status;
            Publication = publication;
            Diagnostics = diagnostics;
            this.violations = new ReadOnlyCollection<BiomePatchValidationViolation>(orderedViolations);
            this.errors = new ReadOnlyCollection<BiomePatchValidationError>(orderedErrors);
        }

        public BiomePatchValidationStatus Status { get; }
        public bool Succeeded => Status == BiomePatchValidationStatus.Completed;
        public bool RetryRequired => Status == BiomePatchValidationStatus.ValidationRejected;
        public BiomePatchValidationPublication Publication { get; }
        public BiomePatchValidationDiagnostics Diagnostics { get; }
        public IReadOnlyList<BiomePatchValidationViolation> Violations => violations;
        public IReadOnlyList<BiomePatchValidationError> Errors => errors;

        internal static BiomePatchValidationResult Completed(
            BiomePatchValidationPublication publication,
            BiomePatchValidationDiagnostics diagnostics)
        {
            return new BiomePatchValidationResult(
                BiomePatchValidationStatus.Completed, publication, diagnostics,
                Array.Empty<BiomePatchValidationViolation>(), Array.Empty<BiomePatchValidationError>());
        }

        internal static BiomePatchValidationResult Rejected(
            BiomePatchValidationDiagnostics diagnostics,
            IEnumerable<BiomePatchValidationViolation> violations)
        {
            return new BiomePatchValidationResult(
                BiomePatchValidationStatus.ValidationRejected, null, diagnostics,
                violations, Array.Empty<BiomePatchValidationError>());
        }

        internal static BiomePatchValidationResult Invalid(IEnumerable<BiomePatchValidationError> errors)
        {
            return new BiomePatchValidationResult(
                BiomePatchValidationStatus.InvalidInput, null, null,
                Array.Empty<BiomePatchValidationViolation>(), errors);
        }

        internal static int CompareViolations(
            BiomePatchValidationViolation left,
            BiomePatchValidationViolation right)
        {
            var value = left.Rule.CompareTo(right.Rule);
            if (value != 0) return value;
            value = string.Compare(left.BiomeId, right.BiomeId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.PatchId, right.PatchId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            if (value != 0) return value;
            value = string.Compare(left.Expected, right.Expected, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.Actual, right.Actual, StringComparison.Ordinal);
            if (value != 0) return value;
            return string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        internal static List<BiomePatchValidationViolation> SortAndDedupeViolations(
            IEnumerable<BiomePatchValidationViolation> source)
        {
            var values = new List<BiomePatchValidationViolation>();
            foreach (var value in source) if (value != null) values.Add(value);
            values.Sort(CompareViolations);
            var result = new List<BiomePatchValidationViolation>();
            foreach (var value in values)
                if (result.Count == 0 || CompareViolations(result[result.Count - 1], value) != 0)
                    result.Add(value);
            return result;
        }

        private static List<BiomePatchValidationError> SortAndDedupeErrors(
            IEnumerable<BiomePatchValidationError> source)
        {
            var values = new List<BiomePatchValidationError>();
            foreach (var value in source) if (value != null) values.Add(value);
            values.Sort(CompareErrors);
            var result = new List<BiomePatchValidationError>();
            foreach (var value in values)
                if (result.Count == 0 || CompareErrors(result[result.Count - 1], value) != 0)
                    result.Add(value);
            return result;
        }

        private static int CompareErrors(BiomePatchValidationError left, BiomePatchValidationError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = string.Compare(left.DefinitionId, right.DefinitionId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            if (value != 0) return value;
            return string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }
    }
}
