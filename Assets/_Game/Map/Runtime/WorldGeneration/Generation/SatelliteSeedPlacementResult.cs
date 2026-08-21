using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum SatelliteSeedPlacementStatus
    {
        Completed,
        InvalidInput,
        RetryRequired
    }

    public sealed class SatelliteSeedPlacementResult
    {
        private readonly IReadOnlyList<SatelliteSeedPlacementError> errors;

        private SatelliteSeedPlacementResult(
            SatelliteSeedPlacementStatus status,
            SatelliteSeedPlacementPublication publication,
            SatelliteSeedPlacementDiagnostics diagnostics,
            IEnumerable<SatelliteSeedPlacementError> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            var ordered = SortAndDedupe(errors);
            switch (status)
            {
                case SatelliteSeedPlacementStatus.Completed:
                    if (publication == null || diagnostics == null || ordered.Count != 0)
                        throw new ArgumentException("Completed placement requires publication and diagnostics only.");
                    break;
                case SatelliteSeedPlacementStatus.InvalidInput:
                    if (publication != null || diagnostics != null || ordered.Count == 0)
                        throw new ArgumentException("Invalid placement requires structural errors only.");
                    break;
                case SatelliteSeedPlacementStatus.RetryRequired:
                    if (publication != null || diagnostics == null || ordered.Count == 0)
                        throw new ArgumentException("Retry placement requires diagnostics and errors only.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status));
            }

            Status = status;
            Publication = publication;
            Diagnostics = diagnostics;
            this.errors = new ReadOnlyCollection<SatelliteSeedPlacementError>(ordered);
        }

        public SatelliteSeedPlacementStatus Status { get; }
        public bool Succeeded => Status == SatelliteSeedPlacementStatus.Completed;
        public bool RetryRequired => Status == SatelliteSeedPlacementStatus.RetryRequired;
        public SatelliteSeedPlacementPublication Publication { get; }
        public SatelliteSeedPlacementDiagnostics Diagnostics { get; }
        public IReadOnlyList<SatelliteSeedPlacementError> Errors => errors;

        internal static SatelliteSeedPlacementResult Completed(
            SatelliteSeedPlacementPublication publication,
            SatelliteSeedPlacementDiagnostics diagnostics)
        {
            return new SatelliteSeedPlacementResult(
                SatelliteSeedPlacementStatus.Completed,
                publication,
                diagnostics,
                Array.Empty<SatelliteSeedPlacementError>());
        }

        internal static SatelliteSeedPlacementResult Invalid(
            IEnumerable<SatelliteSeedPlacementError> errors)
        {
            return new SatelliteSeedPlacementResult(
                SatelliteSeedPlacementStatus.InvalidInput,
                null,
                null,
                errors);
        }

        internal static SatelliteSeedPlacementResult Retry(
            SatelliteSeedPlacementDiagnostics diagnostics,
            IEnumerable<SatelliteSeedPlacementError> errors)
        {
            return new SatelliteSeedPlacementResult(
                SatelliteSeedPlacementStatus.RetryRequired,
                null,
                diagnostics,
                errors);
        }

        internal static int Compare(
            SatelliteSeedPlacementError left,
            SatelliteSeedPlacementError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = string.Compare(left.DefinitionId, right.DefinitionId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.BiomeId, right.BiomeId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = left.SatelliteOrdinal.CompareTo(right.SatelliteOrdinal);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            if (value != 0) return value;
            value = left.RequiredCount.CompareTo(right.RequiredCount);
            if (value != 0) return value;
            value = left.AvailableCount.CompareTo(right.AvailableCount);
            if (value != 0) return value;
            return string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        private static List<SatelliteSeedPlacementError> SortAndDedupe(
            IEnumerable<SatelliteSeedPlacementError> source)
        {
            var values = new List<SatelliteSeedPlacementError>();
            foreach (var value in source) if (value != null) values.Add(value);
            values.Sort(Compare);
            var result = new List<SatelliteSeedPlacementError>();
            foreach (var value in values)
                if (result.Count == 0 || Compare(result[result.Count - 1], value) != 0)
                    result.Add(value);
            return result;
        }
    }
}
