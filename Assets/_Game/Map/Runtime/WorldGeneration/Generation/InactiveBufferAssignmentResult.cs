using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class InactiveBufferAssignmentError
    {
        public InactiveBufferAssignmentError(
            InactiveBufferAssignmentErrorCode code,
            int sectorIndex,
            string sourceOwner,
            string sourceField,
            string message)
        {
            if (!Enum.IsDefined(typeof(InactiveBufferAssignmentErrorCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            if (sectorIndex < -1 || sectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            RequireCanonical(sourceOwner, nameof(sourceOwner));
            RequireCanonical(sourceField, nameof(sourceField));
            RequireCanonical(message, nameof(message));
            Code = code;
            SectorIndex = sectorIndex;
            SourceOwner = sourceOwner;
            SourceField = sourceField;
            Message = message;
        }

        public InactiveBufferAssignmentErrorCode Code { get; }
        public int SectorIndex { get; }
        public string SourceOwner { get; }
        public string SourceField { get; }
        public string Message { get; }

        internal static int Compare(InactiveBufferAssignmentError left, InactiveBufferAssignmentError right)
        {
            var code = left.Code.CompareTo(right.Code);
            if (code != 0) return code;
            var sector = left.SectorIndex.CompareTo(right.SectorIndex);
            if (sector != 0) return sector;
            var owner = string.Compare(left.SourceOwner, right.SourceOwner, StringComparison.Ordinal);
            if (owner != 0) return owner;
            var field = string.Compare(left.SourceField, right.SourceField, StringComparison.Ordinal);
            return field != 0 ? field : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        internal bool SameIdentity(InactiveBufferAssignmentError other)
        {
            return other != null && Code == other.Code && SectorIndex == other.SectorIndex &&
                   string.Equals(SourceOwner, other.SourceOwner, StringComparison.Ordinal) &&
                   string.Equals(SourceField, other.SourceField, StringComparison.Ordinal) &&
                   string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        private static void RequireCanonical(string value, string parameterName)
        {
            if (string.IsNullOrEmpty(value) ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Value must be canonical non-empty text.", parameterName);
        }
    }

    public sealed class InactiveBufferAssignmentResult
    {
        private readonly IReadOnlyList<InactiveBufferAssignment> assignments;
        private readonly IReadOnlyList<InactiveBufferAssignmentError> errors;

        internal InactiveBufferAssignmentResult(
            InactiveBufferAssignmentStatus status,
            IEnumerable<InactiveBufferAssignment> sourceAssignments,
            InactiveBufferAssignmentDiagnostics diagnostics,
            IEnumerable<InactiveBufferAssignmentError> sourceErrors,
            string sourceMandatoryGraphDigest,
            string sourceType0AssignmentDigest,
            string sourceGrowthDigest,
            string sourceReturnPolicyDigest,
            string canonicalDigest)
        {
            if (!Enum.IsDefined(typeof(InactiveBufferAssignmentStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));
            if (sourceAssignments == null) throw new ArgumentNullException(nameof(sourceAssignments));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            if (sourceErrors == null) throw new ArgumentNullException(nameof(sourceErrors));

            var assignmentValues = new List<InactiveBufferAssignment>(sourceAssignments);
            foreach (var assignment in assignmentValues)
            {
                if (assignment == null) throw new ArgumentException("Assignments cannot contain null.", nameof(sourceAssignments));
            }
            assignmentValues.Sort((left, right) => left.SectorIndex.CompareTo(right.SectorIndex));
            for (var index = 1; index < assignmentValues.Count; index++)
            {
                if (assignmentValues[index - 1].SectorIndex == assignmentValues[index].SectorIndex)
                    throw new ArgumentException("Assignment sector indices must be unique.", nameof(sourceAssignments));
            }

            var errorValues = new List<InactiveBufferAssignmentError>();
            foreach (var error in sourceErrors)
            {
                if (error == null) throw new ArgumentException("Errors cannot contain null.", nameof(sourceErrors));
                errorValues.Add(error);
            }
            errorValues.Sort(InactiveBufferAssignmentError.Compare);
            for (var index = errorValues.Count - 1; index > 0; index--)
            {
                if (errorValues[index].SameIdentity(errorValues[index - 1])) errorValues.RemoveAt(index);
            }

            var success = status == InactiveBufferAssignmentStatus.Completed;
            if (success)
            {
                if (errorValues.Count != 0 || assignmentValues.Count != diagnostics.AssignmentCount ||
                    diagnostics.WorldSectorCount != WorldGenConstants.SectorCount ||
                    diagnostics.ProtectedUnionCount + diagnostics.AssignmentCount != diagnostics.WorldSectorCount ||
                    diagnostics.UnassignedSectorCount != 0 || diagnostics.IllegalOwnershipOverlapCount != 0 ||
                    diagnostics.DuplicateSectorCount != 0 || diagnostics.OpenEdgeToInactiveCount != 0 ||
                    diagnostics.RngDrawCount != 0 || diagnostics.SourceMutationCount != 0 ||
                    !IsCanonicalIdentity(sourceMandatoryGraphDigest) ||
                    !IsLowerHexDigest(sourceType0AssignmentDigest) ||
                    !IsLowerHexDigest(sourceGrowthDigest) ||
                    !IsLowerHexDigest(sourceReturnPolicyDigest) ||
                    !IsLowerHexDigest(canonicalDigest))
                    throw new ArgumentException("Completed results require exact accounting and canonical source-chain digests.");
            }
            else if (assignmentValues.Count != 0 || !string.IsNullOrEmpty(canonicalDigest) ||
                     errorValues.Count == 0 || diagnostics.AssignmentCount != 0 ||
                     diagnostics.UnassignedSectorCount != 0 || diagnostics.RngDrawCount != 0 ||
                     diagnostics.SourceMutationCount != 0)
            {
                throw new ArgumentException("Failed results must be atomic, digest-free, and side-effect free.");
            }

            Status = status;
            assignments = new ReadOnlyCollection<InactiveBufferAssignment>(assignmentValues);
            errors = new ReadOnlyCollection<InactiveBufferAssignmentError>(errorValues);
            SourceMandatoryGraphDigest = sourceMandatoryGraphDigest ?? string.Empty;
            SourceType0AssignmentDigest = sourceType0AssignmentDigest ?? string.Empty;
            SourceGrowthDigest = sourceGrowthDigest ?? string.Empty;
            SourceReturnPolicyDigest = sourceReturnPolicyDigest ?? string.Empty;
            CanonicalDigest = canonicalDigest ?? string.Empty;
            RngDrawCount = diagnostics.RngDrawCount;
        }

        public InactiveBufferAssignmentStatus Status { get; }
        public IReadOnlyList<InactiveBufferAssignment> Assignments => assignments;
        public InactiveBufferAssignmentDiagnostics Diagnostics { get; }
        public IReadOnlyList<InactiveBufferAssignmentError> Errors => errors;
        public string SourceMandatoryGraphDigest { get; }
        public string SourceType0AssignmentDigest { get; }
        public string SourceGrowthDigest { get; }
        public string SourceReturnPolicyDigest { get; }
        public string CanonicalDigest { get; }
        public int RngDrawCount { get; }
        public bool IsSuccess => Status == InactiveBufferAssignmentStatus.Completed;

        internal static bool IsLowerHexDigest(string value)
        {
            if (value == null || value.Length != 64) return false;
            foreach (var character in value)
            {
                if ((character < '0' || character > '9') &&
                    (character < 'a' || character > 'f')) return false;
            }
            return true;
        }

        internal static bool IsCanonicalIdentity(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                   string.Equals(value, value.Trim(), StringComparison.Ordinal);
        }
    }
}
