using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum SiteReservationValidationStatus
    {
        Completed,
        ValidationRejected,
        InvalidInput
    }

    public sealed class SiteReservationValidationResult
    {
        private readonly IReadOnlyList<SiteReservationValidationViolation> violations;
        private readonly IReadOnlyList<SiteReservationValidationError> errors;

        public SiteReservationValidationResult(
            SiteReservationValidationStatus status,
            SiteReservationPublication publication,
            SiteReservationValidationDiagnostics diagnostics,
            IEnumerable<SiteReservationValidationViolation> violations,
            IEnumerable<SiteReservationValidationError> errors)
        {
            if (!Enum.IsDefined(typeof(SiteReservationValidationStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));
            this.violations = SnapshotViolations(violations);
            this.errors = SnapshotErrors(errors);

            if (status == SiteReservationValidationStatus.Completed)
            {
                if (publication == null || diagnostics == null ||
                    diagnostics.Rules.Count != 6 || diagnostics.ViolationCount != 0 ||
                    this.violations.Count != 0 || this.errors.Count != 0)
                    throw new ArgumentException("Completed validation requires a clean publication and diagnostics.");
                foreach (var rule in diagnostics.Rules)
                    if (!rule.Passed) throw new ArgumentException("Completed validation requires six passed rules.");
            }
            else if (status == SiteReservationValidationStatus.ValidationRejected)
            {
                if (publication != null || diagnostics == null || this.violations.Count == 0 ||
                    diagnostics.ViolationCount != this.violations.Count || this.errors.Count != 0)
                    throw new ArgumentException("Rejected validation requires diagnostics and violations only.");
            }
            else if (publication != null || diagnostics != null ||
                     this.violations.Count != 0 || this.errors.Count == 0)
            {
                throw new ArgumentException("Invalid input requires errors and no partial validation output.");
            }

            Status = status;
            Publication = publication;
            Diagnostics = diagnostics;
            RetryRequired = status == SiteReservationValidationStatus.ValidationRejected;
        }

        public SiteReservationValidationStatus Status { get; }
        public bool Succeeded => Status == SiteReservationValidationStatus.Completed;
        public bool RetryRequired { get; }
        public SiteReservationPublication Publication { get; }
        public SiteReservationValidationDiagnostics Diagnostics { get; }
        public IReadOnlyList<SiteReservationValidationViolation> Violations => violations;
        public IReadOnlyList<SiteReservationValidationError> Errors => errors;

        private static IReadOnlyList<SiteReservationValidationViolation> SnapshotViolations(
            IEnumerable<SiteReservationValidationViolation> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var values = new List<SiteReservationValidationViolation>(source);
            if (values.Exists(item => item == null))
                throw new ArgumentException("Violations cannot contain null.", nameof(source));
            values.Sort(CompareViolations);
            var unique = new List<SiteReservationValidationViolation>();
            foreach (var value in values)
                if (unique.Count == 0 || CompareViolations(unique[unique.Count - 1], value) != 0)
                    unique.Add(value);
            return new ReadOnlyCollection<SiteReservationValidationViolation>(unique);
        }

        private static IReadOnlyList<SiteReservationValidationError> SnapshotErrors(
            IEnumerable<SiteReservationValidationError> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var values = new List<SiteReservationValidationError>(source);
            if (values.Exists(item => item == null))
                throw new ArgumentException("Errors cannot contain null.", nameof(source));
            values.Sort(CompareErrors);
            var unique = new List<SiteReservationValidationError>();
            foreach (var value in values)
                if (unique.Count == 0 || CompareErrors(unique[unique.Count - 1], value) != 0)
                    unique.Add(value);
            return new ReadOnlyCollection<SiteReservationValidationError>(unique);
        }

        private static int CompareViolations(
            SiteReservationValidationViolation left,
            SiteReservationValidationViolation right)
        {
            var value = left.Rule.CompareTo(right.Rule);
            if (value != 0) return value;
            value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = string.Compare(left.FirstId, right.FirstId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.SecondId, right.SecondId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            if (value != 0) return value;
            value = left.MeasuredValue.CompareTo(right.MeasuredValue);
            if (value != 0) return value;
            value = left.ExpectedValue.CompareTo(right.ExpectedValue);
            return value != 0 ? value : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        private static int CompareErrors(
            SiteReservationValidationError left,
            SiteReservationValidationError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = string.Compare(left.DefinitionId, right.DefinitionId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.ChildId, right.ChildId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            return value != 0 ? value : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }
    }
}
