using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum OptionalAccessAssignmentStatus
    {
        Completed,
        InvalidInput,
        InvalidSettings,
        InvalidBoundary,
        InvalidAssignment
    }

    public sealed class OptionalAccessAssignmentError
    {
        public OptionalAccessAssignmentError(
            string code,
            OptionalRegionId regionId,
            int attachmentOrder,
            OptionalAccessClueId clueId,
            string message)
        {
            if (string.IsNullOrEmpty(code) || !string.Equals(code, code.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Error codes must be canonical non-empty tokens.", nameof(code));
            if (attachmentOrder < -1 || attachmentOrder > 9999)
                throw new ArgumentOutOfRangeException(nameof(attachmentOrder));
            if (string.IsNullOrEmpty(message) || !string.Equals(message, message.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Error messages must be canonical non-empty text.", nameof(message));

            Code = code;
            RegionId = regionId;
            AttachmentOrder = attachmentOrder;
            ClueId = clueId;
            Message = message;
        }

        public string Code { get; }
        public OptionalRegionId RegionId { get; }
        public int AttachmentOrder { get; }
        public OptionalAccessClueId ClueId { get; }
        public string Message { get; }

        internal static int Compare(OptionalAccessAssignmentError left, OptionalAccessAssignmentError right)
        {
            var code = string.Compare(left.Code, right.Code, StringComparison.Ordinal);
            if (code != 0) return code;
            var region = left.RegionId.CompareTo(right.RegionId);
            if (region != 0) return region;
            var attachment = left.AttachmentOrder.CompareTo(right.AttachmentOrder);
            if (attachment != 0) return attachment;
            var clue = left.ClueId.CompareTo(right.ClueId);
            return clue != 0 ? clue : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        internal bool SameIdentity(OptionalAccessAssignmentError other)
        {
            return other != null &&
                   string.Equals(Code, other.Code, StringComparison.Ordinal) &&
                   RegionId == other.RegionId &&
                   AttachmentOrder == other.AttachmentOrder &&
                   ClueId == other.ClueId &&
                   string.Equals(Message, other.Message, StringComparison.Ordinal);
        }
    }

    public sealed class OptionalAccessAssignmentResult
    {
        private readonly IReadOnlyList<OptionalAccessAssignment> assignments;
        private readonly IReadOnlyList<OptionalAccessClue> clues;
        private readonly IReadOnlyList<OptionalAccessAssignmentError> errors;

        internal OptionalAccessAssignmentResult(
            OptionalAccessAssignmentStatus status,
            IEnumerable<OptionalAccessAssignment> sourceAssignments,
            IEnumerable<OptionalAccessClue> sourceClues,
            OptionalAccessAssignmentDiagnostics diagnostics,
            IEnumerable<OptionalAccessAssignmentError> sourceErrors,
            string sourceType0AssignmentDigest,
            string sourceGrowthDigest,
            string canonicalDigest)
        {
            if (!Enum.IsDefined(typeof(OptionalAccessAssignmentStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));
            if (sourceAssignments == null) throw new ArgumentNullException(nameof(sourceAssignments));
            if (sourceClues == null) throw new ArgumentNullException(nameof(sourceClues));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            if (sourceErrors == null) throw new ArgumentNullException(nameof(sourceErrors));

            var assignmentValues = new List<OptionalAccessAssignment>(sourceAssignments);
            var clueValues = new List<OptionalAccessClue>(sourceClues);
            var errorValues = new List<OptionalAccessAssignmentError>();
            foreach (var error in sourceErrors)
            {
                if (error == null) throw new ArgumentException("Errors cannot contain null.", nameof(sourceErrors));
                errorValues.Add(error);
            }

            assignmentValues.Sort(CompareAssignments);
            clueValues.Sort((left, right) => left.ClueId.CompareTo(right.ClueId));
            errorValues.Sort(OptionalAccessAssignmentError.Compare);
            for (var index = errorValues.Count - 1; index > 0; index--)
            {
                if (errorValues[index].SameIdentity(errorValues[index - 1])) errorValues.RemoveAt(index);
            }

            var success = status == OptionalAccessAssignmentStatus.Completed;
            if (success)
            {
                if (errorValues.Count != 0 ||
                    assignmentValues.Count != diagnostics.AssignmentCount ||
                    clueValues.Count != diagnostics.ClueCount ||
                    assignmentValues.Count != clueValues.Count ||
                    !IsLowerHexDigest(sourceType0AssignmentDigest) ||
                    !IsLowerHexDigest(sourceGrowthDigest) ||
                    !IsLowerHexDigest(canonicalDigest))
                    throw new ArgumentException("Completed results require complete assignments, clues, and digests.");
            }
            else if (assignmentValues.Count != 0 || clueValues.Count != 0 ||
                     !string.IsNullOrEmpty(canonicalDigest) || errorValues.Count == 0)
            {
                throw new ArgumentException("Failed results must be atomic, digest-free, and contain an error.");
            }

            Status = status;
            assignments = new ReadOnlyCollection<OptionalAccessAssignment>(assignmentValues);
            clues = new ReadOnlyCollection<OptionalAccessClue>(clueValues);
            errors = new ReadOnlyCollection<OptionalAccessAssignmentError>(errorValues);
            SourceType0AssignmentDigest = sourceType0AssignmentDigest ?? string.Empty;
            SourceGrowthDigest = sourceGrowthDigest ?? string.Empty;
            CanonicalDigest = canonicalDigest ?? string.Empty;
            RngDrawCount = diagnostics.RngDrawCount;
        }

        public OptionalAccessAssignmentStatus Status { get; }
        public IReadOnlyList<OptionalAccessAssignment> Assignments => assignments;
        public IReadOnlyList<OptionalAccessClue> Clues => clues;
        public OptionalAccessAssignmentDiagnostics Diagnostics { get; }
        public IReadOnlyList<OptionalAccessAssignmentError> Errors => errors;
        public string SourceType0AssignmentDigest { get; }
        public string SourceGrowthDigest { get; }
        public string CanonicalDigest { get; }
        public int RngDrawCount { get; }
        public bool IsSuccess => Status == OptionalAccessAssignmentStatus.Completed;

        private static int CompareAssignments(OptionalAccessAssignment left, OptionalAccessAssignment right)
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
