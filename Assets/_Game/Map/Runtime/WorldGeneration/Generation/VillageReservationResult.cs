using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum VillageReservationStatus
    {
        Completed,
        ReservationRejected,
        InvalidInput
    }

    public sealed class VillageReservationResult
    {
        private readonly IReadOnlyList<VillageReservationRejection> rejections;
        private readonly IReadOnlyList<VillageReservationError> errors;

        public VillageReservationResult(
            VillageReservationStatus status,
            VillageReservationApproval approval,
            VillageReservationDiagnostics diagnostics,
            IEnumerable<VillageReservationRejection> rejections,
            IEnumerable<VillageReservationError> errors)
        {
            if (!Enum.IsDefined(typeof(VillageReservationStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));
            this.rejections = SnapshotRejections(rejections);
            this.errors = SnapshotErrors(errors);

            if (status == VillageReservationStatus.Completed)
            {
                if (approval == null || diagnostics == null ||
                    this.rejections.Count != 0 || this.errors.Count != 0)
                    throw new ArgumentException("Completed reservation requires approval and clean diagnostics.");
            }
            else if (status == VillageReservationStatus.ReservationRejected)
            {
                if (approval != null || diagnostics == null ||
                    this.rejections.Count != 1 || this.errors.Count != 0)
                    throw new ArgumentException("Rejected reservation requires one rejection and diagnostics only.");
            }
            else if (approval != null || diagnostics != null ||
                     this.rejections.Count != 0 || this.errors.Count == 0)
            {
                throw new ArgumentException("Invalid input requires errors and no reservation output.");
            }

            Status = status;
            Approval = approval;
            Diagnostics = diagnostics;
            RetryRequired = status == VillageReservationStatus.ReservationRejected;
        }

        public VillageReservationStatus Status { get; }
        public bool Succeeded => Status == VillageReservationStatus.Completed;
        public bool RetryRequired { get; }
        public VillageReservationApproval Approval { get; }
        public VillageReservationDiagnostics Diagnostics { get; }
        public IReadOnlyList<VillageReservationRejection> Rejections => rejections;
        public IReadOnlyList<VillageReservationError> Errors => errors;

        private static IReadOnlyList<VillageReservationRejection> SnapshotRejections(
            IEnumerable<VillageReservationRejection> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var values = new List<VillageReservationRejection>(source);
            if (values.Exists(item => item == null))
                throw new ArgumentException("Rejections cannot contain null.", nameof(source));
            values.Sort(CompareRejections);
            var unique = new List<VillageReservationRejection>();
            foreach (var value in values)
                if (unique.Count == 0 || CompareRejections(unique[unique.Count - 1], value) != 0)
                    unique.Add(value);
            return new ReadOnlyCollection<VillageReservationRejection>(unique);
        }

        private static IReadOnlyList<VillageReservationError> SnapshotErrors(
            IEnumerable<VillageReservationError> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var values = new List<VillageReservationError>(source);
            if (values.Exists(item => item == null))
                throw new ArgumentException("Errors cannot contain null.", nameof(source));
            values.Sort(CompareErrors);
            var unique = new List<VillageReservationError>();
            foreach (var value in values)
                if (unique.Count == 0 || CompareErrors(unique[unique.Count - 1], value) != 0)
                    unique.Add(value);
            return new ReadOnlyCollection<VillageReservationError>(unique);
        }

        private static int CompareRejections(
            VillageReservationRejection left,
            VillageReservationRejection right)
        {
            var value = left.Reason.CompareTo(right.Reason);
            if (value != 0) return value;
            value = left.BucketOrdinal.CompareTo(right.BucketOrdinal);
            if (value != 0) return value;
            value = left.MinDistanceInclusive.CompareTo(right.MinDistanceInclusive);
            if (value != 0) return value;
            value = left.MaxDistanceInclusive.CompareTo(right.MaxDistanceInclusive);
            if (value != 0) return value;
            value = left.SourceCandidateCount.CompareTo(right.SourceCandidateCount);
            if (value != 0) return value;
            value = left.ViableCandidateCount.CompareTo(right.ViableCandidateCount);
            return value != 0 ? value : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        private static int CompareErrors(VillageReservationError left, VillageReservationError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = string.Compare(left.DefinitionId, right.DefinitionId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            return value != 0 ? value : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }
    }
}
