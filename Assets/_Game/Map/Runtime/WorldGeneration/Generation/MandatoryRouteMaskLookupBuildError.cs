using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum MandatoryRouteMaskLookupBuildErrorCode
    {
        MissingInput,
        MissingRequiredMask,
        DuplicateMaskId,
        DuplicateRouteType,
        DuplicateOpenMask,
        InactiveRequiredMask,
        MandatoryNotAllowed,
        UnexpectedMandatoryMask,
        InvalidRouteType,
        InvalidOpenMask,
        UnsupportedVerticalPair
    }

    public sealed class MandatoryRouteMaskLookupBuildError
    {
        public MandatoryRouteMaskLookupBuildError(MandatoryRouteMaskLookupBuildErrorCode code, string firstId, string secondId, int routeType, string message)
        {
            if (code < MandatoryRouteMaskLookupBuildErrorCode.MissingInput || code > MandatoryRouteMaskLookupBuildErrorCode.UnsupportedVerticalPair)
                throw new ArgumentOutOfRangeException(nameof(code));
            Code = code;
            FirstId = firstId ?? string.Empty;
            SecondId = secondId ?? string.Empty;
            RouteType = routeType;
            Message = message ?? string.Empty;
        }
        public MandatoryRouteMaskLookupBuildErrorCode Code { get; }
        public string FirstId { get; }
        public string SecondId { get; }
        public int RouteType { get; }
        public string Message { get; }

        internal static int Compare(MandatoryRouteMaskLookupBuildError left, MandatoryRouteMaskLookupBuildError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = left.RouteType.CompareTo(right.RouteType);
            if (value != 0) return value;
            value = string.Compare(left.FirstId, right.FirstId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.SecondId, right.SecondId, StringComparison.Ordinal);
            if (value != 0) return value;
            return string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }
    }

    public enum MandatoryRouteMaskLookupBuildStatus
    {
        Completed,
        InvalidInput
    }

    public sealed class MandatoryRouteMaskLookupBuildResult
    {
        private readonly IReadOnlyList<MandatoryRouteMaskLookupBuildError> errors;

        internal MandatoryRouteMaskLookupBuildResult(MandatoryRouteMaskLookupBuildStatus status, MandatoryRouteMaskLookup lookup,
            MandatoryRouteMaskLookupDiagnostics diagnostics, IEnumerable<MandatoryRouteMaskLookupBuildError> sourceErrors)
        {
            Status = status;
            Lookup = lookup;
            Diagnostics = diagnostics;
            errors = new ReadOnlyCollection<MandatoryRouteMaskLookupBuildError>(new List<MandatoryRouteMaskLookupBuildError>(sourceErrors ?? throw new ArgumentNullException(nameof(sourceErrors))));
            if ((status == MandatoryRouteMaskLookupBuildStatus.Completed) != (lookup != null && diagnostics != null && errors.Count == 0))
                throw new ArgumentException("Build status and payload are inconsistent.");
            if (status == MandatoryRouteMaskLookupBuildStatus.InvalidInput && (lookup != null || diagnostics != null || errors.Count == 0))
                throw new ArgumentException("Invalid input must publish errors only.");
        }
        public MandatoryRouteMaskLookupBuildStatus Status { get; }
        public bool Success => Status == MandatoryRouteMaskLookupBuildStatus.Completed;
        public MandatoryRouteMaskLookup Lookup { get; }
        public MandatoryRouteMaskLookupDiagnostics Diagnostics { get; }
        public IReadOnlyList<MandatoryRouteMaskLookupBuildError> Errors => errors;
        public bool RetryRequired => false;
    }
}
