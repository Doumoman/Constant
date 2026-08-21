using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum CoreCapacityFloodStatus
    {
        Completed,
        CapacityRejected,
        InvalidInput
    }

    public sealed class CoreCapacityFloodResult
    {
        private readonly IReadOnlyList<CoreCapacityFloodRejection> rejections;
        private readonly IReadOnlyList<CoreCapacityFloodError> errors;

        public CoreCapacityFloodResult(
            CoreCapacityFloodStatus status,
            CoreCapacityApproval approval,
            CoreCapacityFloodDiagnostics diagnostics,
            IEnumerable<CoreCapacityFloodRejection> rejections,
            IEnumerable<CoreCapacityFloodError> errors)
        {
            if (!Enum.IsDefined(typeof(CoreCapacityFloodStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));
            this.rejections = SnapshotRejections(rejections);
            this.errors = SnapshotErrors(errors);

            if (status == CoreCapacityFloodStatus.Completed)
            {
                if (approval == null || diagnostics == null ||
                    this.rejections.Count != 0 || this.errors.Count != 0)
                    throw new ArgumentException("Completed capacity requires approval and clean diagnostics.");
            }
            else if (status == CoreCapacityFloodStatus.CapacityRejected)
            {
                if (approval != null || diagnostics == null ||
                    this.rejections.Count == 0 || this.errors.Count != 0)
                    throw new ArgumentException("Capacity rejection requires diagnostics and rejections only.");
            }
            else if (approval != null || diagnostics != null ||
                     this.rejections.Count != 0 || this.errors.Count == 0)
            {
                throw new ArgumentException("Invalid input requires errors and no capacity output.");
            }

            Status = status;
            Approval = approval;
            Diagnostics = diagnostics;
            RetryRequired = status == CoreCapacityFloodStatus.CapacityRejected;
        }

        public CoreCapacityFloodStatus Status { get; }
        public bool Succeeded => Status == CoreCapacityFloodStatus.Completed;
        public bool RetryRequired { get; }
        public CoreCapacityApproval Approval { get; }
        public CoreCapacityFloodDiagnostics Diagnostics { get; }
        public IReadOnlyList<CoreCapacityFloodRejection> Rejections => rejections;
        public IReadOnlyList<CoreCapacityFloodError> Errors => errors;

        internal static IReadOnlyList<CoreCapacityFloodRejection> SnapshotRejections(
            IEnumerable<CoreCapacityFloodRejection> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var snapshot = new List<CoreCapacityFloodRejection>(source);
            if (snapshot.Exists(item => item == null))
                throw new ArgumentException("Capacity rejections cannot contain null.", nameof(source));
            snapshot.Sort(CompareRejections);
            var unique = new List<CoreCapacityFloodRejection>();
            foreach (var item in snapshot)
                if (unique.Count == 0 || CompareRejections(unique[unique.Count - 1], item) != 0)
                    unique.Add(item);
            return new ReadOnlyCollection<CoreCapacityFloodRejection>(unique);
        }

        internal static IReadOnlyList<CoreCapacityFloodError> SnapshotErrors(
            IEnumerable<CoreCapacityFloodError> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var snapshot = new List<CoreCapacityFloodError>(source);
            if (snapshot.Exists(item => item == null))
                throw new ArgumentException("Capacity errors cannot contain null.", nameof(source));
            snapshot.Sort(CompareErrors);
            var unique = new List<CoreCapacityFloodError>();
            foreach (var item in snapshot)
                if (unique.Count == 0 || CompareErrors(unique[unique.Count - 1], item) != 0)
                    unique.Add(item);
            return new ReadOnlyCollection<CoreCapacityFloodError>(unique);
        }

        private static int CompareRejections(
            CoreCapacityFloodRejection left,
            CoreCapacityFloodRejection right)
        {
            var value = left.Reason.CompareTo(right.Reason);
            if (value != 0) return value;
            value = left.Key.CompareTo(right.Key);
            if (value != 0) return value;
            value = CompareOptionalKeys(left.OtherKey, right.OtherKey);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            if (value != 0) return value;
            value = left.RequiredCount.CompareTo(right.RequiredCount);
            if (value != 0) return value;
            value = left.AvailableCount.CompareTo(right.AvailableCount);
            return value != 0 ? value : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        private static int CompareErrors(CoreCapacityFloodError left, CoreCapacityFloodError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = string.Compare(left.SiteSourceDefinitionId,
                right.SiteSourceDefinitionId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.BiomeId, right.BiomeId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.CorePatchRuleId,
                right.CorePatchRuleId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            return value != 0 ? value : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        private static int CompareOptionalKeys(SitePlacementKey left, SitePlacementKey right)
        {
            if (left.IsValid && right.IsValid) return left.CompareTo(right);
            if (left.IsValid) return 1;
            if (right.IsValid) return -1;
            return 0;
        }
    }
}
