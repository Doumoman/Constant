using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalRewardTierCalculationError
    {
        public OptionalRewardTierCalculationError(
            OptionalRewardTierCalculationErrorCode code,
            OptionalRegionId regionId,
            int attachmentOrder,
            string sourceField,
            string message)
        {
            if (!Enum.IsDefined(typeof(OptionalRewardTierCalculationErrorCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            if (attachmentOrder < -1 || attachmentOrder > 9999)
                throw new ArgumentOutOfRangeException(nameof(attachmentOrder));
            if (string.IsNullOrEmpty(sourceField) ||
                !string.Equals(sourceField, sourceField.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Source field must be canonical non-empty text.", nameof(sourceField));
            if (string.IsNullOrEmpty(message) ||
                !string.Equals(message, message.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Error message must be canonical non-empty text.", nameof(message));

            Code = code;
            RegionId = regionId;
            AttachmentOrder = attachmentOrder;
            SourceField = sourceField;
            Message = message;
        }

        public OptionalRewardTierCalculationErrorCode Code { get; }
        public OptionalRegionId RegionId { get; }
        public int AttachmentOrder { get; }
        public string SourceField { get; }
        public string Message { get; }

        internal static int Compare(OptionalRewardTierCalculationError left, OptionalRewardTierCalculationError right)
        {
            var code = left.Code.CompareTo(right.Code);
            if (code != 0) return code;
            var region = left.RegionId.CompareTo(right.RegionId);
            if (region != 0) return region;
            var attachment = left.AttachmentOrder.CompareTo(right.AttachmentOrder);
            if (attachment != 0) return attachment;
            var field = string.Compare(left.SourceField, right.SourceField, StringComparison.Ordinal);
            return field != 0 ? field : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        internal bool SameIdentity(OptionalRewardTierCalculationError other)
        {
            return other != null && Code == other.Code && RegionId == other.RegionId &&
                   AttachmentOrder == other.AttachmentOrder &&
                   string.Equals(SourceField, other.SourceField, StringComparison.Ordinal) &&
                   string.Equals(Message, other.Message, StringComparison.Ordinal);
        }
    }

    public sealed class OptionalRewardTierResult
    {
        private readonly IReadOnlyList<OptionalRewardTierAssignment> assignments;
        private readonly IReadOnlyList<OptionalRewardTierCalculationError> errors;

        internal OptionalRewardTierResult(
            OptionalRewardTierCalculationStatus status,
            IEnumerable<OptionalRewardTierAssignment> sourceAssignments,
            OptionalRewardTierDiagnostics diagnostics,
            IEnumerable<OptionalRewardTierCalculationError> sourceErrors,
            string sourceType0AssignmentDigest,
            string sourceAccessAssignmentDigest,
            string sourceGrowthDigest,
            string canonicalDigest)
        {
            if (!Enum.IsDefined(typeof(OptionalRewardTierCalculationStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));
            if (sourceAssignments == null) throw new ArgumentNullException(nameof(sourceAssignments));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            if (sourceErrors == null) throw new ArgumentNullException(nameof(sourceErrors));

            var assignmentValues = new List<OptionalRewardTierAssignment>(sourceAssignments);
            foreach (var assignment in assignmentValues)
            {
                if (assignment == null)
                    throw new ArgumentException("Assignments cannot contain null.", nameof(sourceAssignments));
            }
            assignmentValues.Sort(CompareAssignments);

            var errorValues = new List<OptionalRewardTierCalculationError>();
            foreach (var error in sourceErrors)
            {
                if (error == null) throw new ArgumentException("Errors cannot contain null.", nameof(sourceErrors));
                errorValues.Add(error);
            }
            errorValues.Sort(OptionalRewardTierCalculationError.Compare);
            for (var index = errorValues.Count - 1; index > 0; index--)
            {
                if (errorValues[index].SameIdentity(errorValues[index - 1])) errorValues.RemoveAt(index);
            }

            var success = status == OptionalRewardTierCalculationStatus.Completed;
            if (success)
            {
                if (errorValues.Count != 0 || assignmentValues.Count != diagnostics.TierAssignmentCount ||
                    !IsLowerHexDigest(sourceType0AssignmentDigest) ||
                    !IsLowerHexDigest(sourceAccessAssignmentDigest) ||
                    !IsLowerHexDigest(sourceGrowthDigest) ||
                    !IsLowerHexDigest(canonicalDigest))
                    throw new ArgumentException("Completed results require complete assignments and source-chain digests.");
            }
            else if (assignmentValues.Count != 0 || !string.IsNullOrEmpty(canonicalDigest) || errorValues.Count == 0)
            {
                throw new ArgumentException("Failed results must be atomic, digest-free, and contain an error.");
            }

            Status = status;
            assignments = new ReadOnlyCollection<OptionalRewardTierAssignment>(assignmentValues);
            errors = new ReadOnlyCollection<OptionalRewardTierCalculationError>(errorValues);
            SourceType0AssignmentDigest = sourceType0AssignmentDigest ?? string.Empty;
            SourceAccessAssignmentDigest = sourceAccessAssignmentDigest ?? string.Empty;
            SourceGrowthDigest = sourceGrowthDigest ?? string.Empty;
            CanonicalDigest = canonicalDigest ?? string.Empty;
            RngDrawCount = diagnostics.RngDrawCount;
        }

        public OptionalRewardTierCalculationStatus Status { get; }
        public IReadOnlyList<OptionalRewardTierAssignment> Assignments => assignments;
        public OptionalRewardTierDiagnostics Diagnostics { get; }
        public IReadOnlyList<OptionalRewardTierCalculationError> Errors => errors;
        public string SourceType0AssignmentDigest { get; }
        public string SourceAccessAssignmentDigest { get; }
        public string SourceGrowthDigest { get; }
        public string CanonicalDigest { get; }
        public int RngDrawCount { get; }
        public bool IsSuccess => Status == OptionalRewardTierCalculationStatus.Completed;

        private static int CompareAssignments(OptionalRewardTierAssignment left, OptionalRewardTierAssignment right)
        {
            var region = left.RegionId.CompareTo(right.RegionId);
            return region != 0 ? region : left.RegionOrdinal.CompareTo(right.RegionOrdinal);
        }

        private static bool IsLowerHexDigest(string value)
        {
            if (value == null || value.Length != 64) return false;
            foreach (var character in value)
            {
                if ((character < '0' || character > '9') &&
                    (character < 'a' || character > 'f')) return false;
            }
            return true;
        }
    }
}
