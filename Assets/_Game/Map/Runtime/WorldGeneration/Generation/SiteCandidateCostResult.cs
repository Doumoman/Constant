using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteCandidateCostResult
    {
        private readonly IReadOnlyList<SiteCandidateCostError> errors;

        public SiteCandidateCostResult(
            SiteCandidateCostBreakdown breakdown,
            IEnumerable<SiteCandidateCostError> errors)
        {
            this.errors = SnapshotErrors(errors);
            if (breakdown == null && this.errors.Count == 0)
                throw new ArgumentException("A failed result requires an error.", nameof(errors));
            if (breakdown != null && this.errors.Count != 0)
                throw new ArgumentException("A successful result cannot contain errors.", nameof(errors));
            Breakdown = breakdown;
        }

        public bool Succeeded => Breakdown != null;
        public SiteCandidateCostBreakdown Breakdown { get; }
        public IReadOnlyList<SiteCandidateCostError> Errors => errors;

        internal static SiteCandidateCostResult Success(SiteCandidateCostBreakdown breakdown) =>
            new SiteCandidateCostResult(
                breakdown ?? throw new ArgumentNullException(nameof(breakdown)),
                Array.Empty<SiteCandidateCostError>());

        internal static SiteCandidateCostResult Failure(IEnumerable<SiteCandidateCostError> errors) =>
            new SiteCandidateCostResult(null, errors);

        private static IReadOnlyList<SiteCandidateCostError> SnapshotErrors(
            IEnumerable<SiteCandidateCostError> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var snapshot = new List<SiteCandidateCostError>(source);
            if (snapshot.Exists(item => item == null))
                throw new ArgumentException("Errors cannot contain null.", nameof(source));
            snapshot.Sort(CompareErrors);
            var unique = new List<SiteCandidateCostError>(snapshot.Count);
            foreach (var error in snapshot)
            {
                if (unique.Count == 0 || CompareErrors(unique[unique.Count - 1], error) != 0)
                    unique.Add(error);
            }
            return new ReadOnlyCollection<SiteCandidateCostError>(unique);
        }

        private static int CompareErrors(SiteCandidateCostError left, SiteCandidateCostError right)
        {
            var code = left.Code.CompareTo(right.Code);
            if (code != 0) return code;
            var candidate = string.Compare(left.CandidateSourceDefinitionId,
                right.CandidateSourceDefinitionId, StringComparison.Ordinal);
            if (candidate != 0) return candidate;
            var existing = string.Compare(left.ExistingSourceDefinitionId,
                right.ExistingSourceDefinitionId, StringComparison.Ordinal);
            if (existing != 0) return existing;
            var sector = left.SectorIndex.CompareTo(right.SectorIndex);
            return sector != 0
                ? sector
                : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }
    }
}
