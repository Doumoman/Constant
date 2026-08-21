using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum SiteDistanceErrorCode
    {
        MissingPlacements,
        NullPlacement,
        InvalidPlacement,
        DuplicatePlacementKey,
        InvalidOccupiedSector,
        OverlappingPlacements,
        MissingStartSourceId,
        InvalidStartSourceId,
        MissingSpecialMapInput,
        NullSpecialMap,
        DuplicateSpecialMapId,
        MissingRequiredSite,
        UnexpectedRequiredSite,
        InactiveRequiredSite,
        SiteRoleMismatch,
        InvalidRequiredCount,
        InvalidDistanceRule,
        MissingPolicy,
        MissingPolicyKey,
        UnexpectedIndexKey,
        MissingDistanceRecord
    }

    public sealed class SiteDistanceError
    {
        public SiteDistanceError(
            SiteDistanceErrorCode code,
            string firstSourceDefinitionId,
            string secondSourceDefinitionId,
            int sectorIndex,
            string message)
        {
            if (!Enum.IsDefined(typeof(SiteDistanceErrorCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            if (!IsCanonicalOrEmpty(firstSourceDefinitionId))
                throw new ArgumentException("The first source ID must be canonical or empty.", nameof(firstSourceDefinitionId));
            if (!IsCanonicalOrEmpty(secondSourceDefinitionId))
                throw new ArgumentException("The second source ID must be canonical or empty.", nameof(secondSourceDefinitionId));
            if (sectorIndex < -1 || sectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("A stable non-empty message is required.", nameof(message));

            Code = code;
            FirstSourceDefinitionId = firstSourceDefinitionId;
            SecondSourceDefinitionId = secondSourceDefinitionId;
            SectorIndex = sectorIndex;
            Message = message;
        }

        public SiteDistanceErrorCode Code { get; }
        public string FirstSourceDefinitionId { get; }
        public string SecondSourceDefinitionId { get; }
        public int SectorIndex { get; }
        public string Message { get; }

        private static bool IsCanonicalOrEmpty(string value) =>
            value != null && (value.Length == 0 || SitePlacementKey.IsCanonicalId(value));
    }

    public sealed class SiteDistanceIndexResult
    {
        private readonly IReadOnlyList<SiteDistanceError> errors;

        public SiteDistanceIndexResult(
            SiteDistanceIndex index,
            IEnumerable<SiteDistanceError> errors)
        {
            this.errors = SiteDistanceResultUtility.SnapshotErrors(errors);
            if (index == null && this.errors.Count == 0)
                throw new ArgumentException("A failed result requires an error.", nameof(errors));
            if (index != null && this.errors.Count != 0)
                throw new ArgumentException("A successful result cannot contain errors.", nameof(errors));
            Index = index;
        }

        public bool Succeeded => Index != null;
        public SiteDistanceIndex Index { get; }
        public IReadOnlyList<SiteDistanceError> Errors => errors;

        internal static SiteDistanceIndexResult Success(SiteDistanceIndex index) =>
            new SiteDistanceIndexResult(index ?? throw new ArgumentNullException(nameof(index)),
                Array.Empty<SiteDistanceError>());
        internal static SiteDistanceIndexResult Failure(IEnumerable<SiteDistanceError> errors) =>
            new SiteDistanceIndexResult(null, errors);
    }

    public sealed class SiteDistancePolicyResult
    {
        private readonly IReadOnlyList<SiteDistanceError> errors;

        public SiteDistancePolicyResult(
            SiteDistancePolicy policy,
            IEnumerable<SiteDistanceError> errors)
        {
            this.errors = SiteDistanceResultUtility.SnapshotErrors(errors);
            if (policy == null && this.errors.Count == 0)
                throw new ArgumentException("A failed result requires an error.", nameof(errors));
            if (policy != null && this.errors.Count != 0)
                throw new ArgumentException("A successful result cannot contain errors.", nameof(errors));
            Policy = policy;
        }

        public bool Succeeded => Policy != null;
        public SiteDistancePolicy Policy { get; }
        public IReadOnlyList<SiteDistanceError> Errors => errors;

        internal static SiteDistancePolicyResult Success(SiteDistancePolicy policy) =>
            new SiteDistancePolicyResult(policy ?? throw new ArgumentNullException(nameof(policy)),
                Array.Empty<SiteDistanceError>());
        internal static SiteDistancePolicyResult Failure(IEnumerable<SiteDistanceError> errors) =>
            new SiteDistancePolicyResult(null, errors);
    }

    internal static class SiteDistanceResultUtility
    {
        public static IReadOnlyList<SiteDistanceError> SnapshotErrors(
            IEnumerable<SiteDistanceError> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            var snapshot = new List<SiteDistanceError>(errors);
            if (snapshot.Exists(item => item == null))
                throw new ArgumentException("Errors cannot contain null.", nameof(errors));
            snapshot.Sort(CompareErrors);
            var unique = new List<SiteDistanceError>(snapshot.Count);
            foreach (var error in snapshot)
            {
                if (unique.Count == 0 || CompareErrors(unique[unique.Count - 1], error) != 0)
                    unique.Add(error);
            }
            return new ReadOnlyCollection<SiteDistanceError>(unique);
        }

        public static int CompareErrors(SiteDistanceError left, SiteDistanceError right)
        {
            var code = left.Code.CompareTo(right.Code);
            if (code != 0) return code;
            var first = string.Compare(left.FirstSourceDefinitionId, right.FirstSourceDefinitionId,
                StringComparison.Ordinal);
            if (first != 0) return first;
            var second = string.Compare(left.SecondSourceDefinitionId, right.SecondSourceDefinitionId,
                StringComparison.Ordinal);
            if (second != 0) return second;
            var sector = left.SectorIndex.CompareTo(right.SectorIndex);
            return sector != 0
                ? sector
                : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }
    }
}
