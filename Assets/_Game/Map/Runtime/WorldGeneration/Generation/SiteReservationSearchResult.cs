using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum SiteReservationSearchStatus
    {
        Completed,
        NoSolution,
        FailedCombinationLimitReached,
        InvalidInput
    }

    public enum SiteReservationSearchErrorCode
    {
        MissingGroups,
        NullGroup,
        DuplicateGroupKey,
        MissingRequiredGroup,
        UnexpectedGroup,
        InvalidGroup,
        EmptyGroup,
        NullOption,
        DuplicateOptionIdentity,
        InvalidOption,
        MissingDistancePolicy,
        PolicyKeyMismatch,
        InvalidDistancePolicy,
        MissingWeights,
        MissingLimits,
        InvalidLimits,
        MissingSiteRng,
        SiteRngAlreadyConsumed,
        CostEvaluationFailed,
        FinalDistanceEvaluationFailed,
        InternalInvariantViolation
    }

    public sealed class SiteReservationSearchError
    {
        public SiteReservationSearchError(
            SiteReservationSearchErrorCode code,
            string groupSourceDefinitionId,
            string candidateSourceDefinitionId,
            int optionOriginIndex,
            string message)
        {
            if (!Enum.IsDefined(typeof(SiteReservationSearchErrorCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            if (!CanonicalOrEmpty(groupSourceDefinitionId))
                throw new ArgumentException("The group source ID must be canonical or empty.", nameof(groupSourceDefinitionId));
            if (!CanonicalOrEmpty(candidateSourceDefinitionId))
                throw new ArgumentException("The candidate source ID must be canonical or empty.", nameof(candidateSourceDefinitionId));
            if (optionOriginIndex < -1 || optionOriginIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(optionOriginIndex));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("A stable non-empty message is required.", nameof(message));

            Code = code;
            GroupSourceDefinitionId = groupSourceDefinitionId;
            CandidateSourceDefinitionId = candidateSourceDefinitionId;
            OptionOriginIndex = optionOriginIndex;
            Message = message;
        }

        public SiteReservationSearchErrorCode Code { get; }
        public string GroupSourceDefinitionId { get; }
        public string CandidateSourceDefinitionId { get; }
        public int OptionOriginIndex { get; }
        public string Message { get; }

        private static bool CanonicalOrEmpty(string value) =>
            value != null && (value.Length == 0 || SitePlacementKey.IsCanonicalId(value));
    }

    public sealed class SiteReservationSearchResult
    {
        private readonly IReadOnlyList<SiteReservationSearchError> errors;

        public SiteReservationSearchResult(
            SiteReservationSearchStatus status,
            SiteReservationSelectionPlan selectionPlan,
            SiteReservationSearchDiagnostics diagnostics,
            IEnumerable<SiteReservationSearchError> errors)
        {
            if (!Enum.IsDefined(typeof(SiteReservationSearchStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            this.errors = SnapshotErrors(errors);

            var completed = status == SiteReservationSearchStatus.Completed;
            var invalid = status == SiteReservationSearchStatus.InvalidInput;
            if (completed != (selectionPlan != null))
                throw new ArgumentException("Only a completed search can publish a selection plan.", nameof(selectionPlan));
            if (invalid != (this.errors.Count != 0))
                throw new ArgumentException("Only invalid input can publish search errors.", nameof(errors));

            Status = status;
            SelectionPlan = selectionPlan;
            RetryRequired = status == SiteReservationSearchStatus.NoSolution ||
                            status == SiteReservationSearchStatus.FailedCombinationLimitReached;
        }

        public SiteReservationSearchStatus Status { get; }
        public bool Succeeded => Status == SiteReservationSearchStatus.Completed;
        public bool RetryRequired { get; }
        public SiteReservationSelectionPlan SelectionPlan { get; }
        public SiteReservationSearchDiagnostics Diagnostics { get; }
        public IReadOnlyList<SiteReservationSearchError> Errors => errors;

        internal static IReadOnlyList<SiteReservationSearchError> SnapshotErrors(
            IEnumerable<SiteReservationSearchError> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var snapshot = new List<SiteReservationSearchError>(source);
            if (snapshot.Exists(item => item == null))
                throw new ArgumentException("Search errors cannot contain null.", nameof(source));
            snapshot.Sort(CompareErrors);
            var unique = new List<SiteReservationSearchError>(snapshot.Count);
            foreach (var error in snapshot)
            {
                if (unique.Count == 0 || CompareErrors(unique[unique.Count - 1], error) != 0)
                    unique.Add(error);
            }
            return new ReadOnlyCollection<SiteReservationSearchError>(unique);
        }

        private static int CompareErrors(
            SiteReservationSearchError left,
            SiteReservationSearchError right)
        {
            var code = left.Code.CompareTo(right.Code);
            if (code != 0) return code;
            var group = string.Compare(left.GroupSourceDefinitionId,
                right.GroupSourceDefinitionId, StringComparison.Ordinal);
            if (group != 0) return group;
            var candidate = string.Compare(left.CandidateSourceDefinitionId,
                right.CandidateSourceDefinitionId, StringComparison.Ordinal);
            if (candidate != 0) return candidate;
            var origin = left.OptionOriginIndex.CompareTo(right.OptionOriginIndex);
            return origin != 0 ? origin : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }
    }
}
