using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum Type0RouteMaskAssignmentStatus
    {
        Completed,
        InvalidInput,
        InvalidCatalog,
        UnsupportedTopology
    }

    public sealed class Type0RouteMaskAssignmentError
    {
        public Type0RouteMaskAssignmentError(
            string code,
            OptionalRegionId regionId,
            int sectorIndex,
            Type0RouteMaskId maskId,
            string message)
        {
            if (string.IsNullOrEmpty(code) ||
                !string.Equals(code, code.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException("Error codes must be canonical non-empty tokens.", nameof(code));
            }

            if (sectorIndex < -1 || sectorIndex >= WorldGenConstants.SectorCount)
            {
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            }

            if (string.IsNullOrEmpty(message) ||
                !string.Equals(message, message.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException("Error messages must be canonical non-empty text.", nameof(message));
            }

            Code = code;
            RegionId = regionId;
            SectorIndex = sectorIndex;
            MaskId = maskId;
            Message = message;
        }

        public string Code { get; }
        public OptionalRegionId RegionId { get; }
        public int SectorIndex { get; }
        public Type0RouteMaskId MaskId { get; }
        public string Message { get; }

        internal static int Compare(Type0RouteMaskAssignmentError left, Type0RouteMaskAssignmentError right)
        {
            var code = string.Compare(left.Code, right.Code, StringComparison.Ordinal);
            if (code != 0) return code;
            var region = left.RegionId.CompareTo(right.RegionId);
            if (region != 0) return region;
            var sector = left.SectorIndex.CompareTo(right.SectorIndex);
            if (sector != 0) return sector;
            var mask = left.MaskId.CompareTo(right.MaskId);
            return mask != 0 ? mask : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        internal bool SameIdentity(Type0RouteMaskAssignmentError other)
        {
            return other != null &&
                   string.Equals(Code, other.Code, StringComparison.Ordinal) &&
                   RegionId == other.RegionId &&
                   SectorIndex == other.SectorIndex &&
                   MaskId == other.MaskId &&
                   string.Equals(Message, other.Message, StringComparison.Ordinal);
        }
    }

    public sealed class Type0RouteMaskAssignmentResult
    {
        private readonly IReadOnlyList<Type0RouteMaskRecord> registeredMasks;
        private readonly IReadOnlyList<Type0RouteMaskAssignment> assignments;
        private readonly IReadOnlyList<Type0RouteMaskAssignmentError> errors;

        internal Type0RouteMaskAssignmentResult(
            Type0RouteMaskAssignmentStatus status,
            OptionalRegionSnapshot sourceSnapshot,
            IEnumerable<Type0RouteMaskRecord> sourceRegisteredMasks,
            IEnumerable<Type0RouteMaskAssignment> sourceAssignments,
            Type0RouteMaskAssignmentDiagnostics diagnostics,
            IEnumerable<Type0RouteMaskAssignmentError> sourceErrors,
            string sourceGrowthDigest,
            string sourceRouteMaskCatalogDigest,
            string canonicalDigest)
        {
            if (!Enum.IsDefined(typeof(Type0RouteMaskAssignmentStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));
            if (sourceRegisteredMasks == null) throw new ArgumentNullException(nameof(sourceRegisteredMasks));
            if (sourceAssignments == null) throw new ArgumentNullException(nameof(sourceAssignments));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            if (sourceErrors == null) throw new ArgumentNullException(nameof(sourceErrors));

            var maskValues = new List<Type0RouteMaskRecord>(sourceRegisteredMasks);
            var assignmentValues = new List<Type0RouteMaskAssignment>(sourceAssignments);
            var errorValues = new List<Type0RouteMaskAssignmentError>();
            foreach (var error in sourceErrors)
            {
                if (error == null) throw new ArgumentException("Errors cannot contain null.", nameof(sourceErrors));
                errorValues.Add(error);
            }
            errorValues.Sort(Type0RouteMaskAssignmentError.Compare);
            for (var index = errorValues.Count - 1; index > 0; index--)
            {
                if (errorValues[index].SameIdentity(errorValues[index - 1]))
                    errorValues.RemoveAt(index);
            }

            var success = status == Type0RouteMaskAssignmentStatus.Completed;
            if (success)
            {
                if (sourceSnapshot == null || errorValues.Count != 0 ||
                    string.IsNullOrEmpty(sourceGrowthDigest) ||
                    string.IsNullOrEmpty(sourceRouteMaskCatalogDigest) ||
                    string.IsNullOrEmpty(canonicalDigest) ||
                    assignmentValues.Count != diagnostics.AssignmentCount)
                {
                    throw new ArgumentException("Completed results require complete source, digest, and assignment publication.");
                }
            }
            else if (assignmentValues.Count != 0 || !string.IsNullOrEmpty(canonicalDigest) || errorValues.Count == 0)
            {
                throw new ArgumentException("Failed results must be atomic, digest-free, and contain an error.");
            }

            Status = status;
            SourceSnapshot = sourceSnapshot;
            registeredMasks = new ReadOnlyCollection<Type0RouteMaskRecord>(maskValues);
            assignments = new ReadOnlyCollection<Type0RouteMaskAssignment>(assignmentValues);
            errors = new ReadOnlyCollection<Type0RouteMaskAssignmentError>(errorValues);
            SourceGrowthDigest = sourceGrowthDigest ?? string.Empty;
            SourceRouteMaskCatalogDigest = sourceRouteMaskCatalogDigest ?? string.Empty;
            CanonicalDigest = canonicalDigest ?? string.Empty;
            RngDrawCount = diagnostics.RngDrawCount;
        }

        public Type0RouteMaskAssignmentStatus Status { get; }
        public OptionalRegionSnapshot SourceSnapshot { get; }
        public IReadOnlyList<Type0RouteMaskRecord> RegisteredMasks => registeredMasks;
        public IReadOnlyList<Type0RouteMaskAssignment> Assignments => assignments;
        public Type0RouteMaskAssignmentDiagnostics Diagnostics { get; }
        public IReadOnlyList<Type0RouteMaskAssignmentError> Errors => errors;
        public string SourceGrowthDigest { get; }
        public string SourceRouteMaskCatalogDigest { get; }
        public string CanonicalDigest { get; }
        public int RngDrawCount { get; }
        public bool IsSuccess => Status == Type0RouteMaskAssignmentStatus.Completed;
    }
}
