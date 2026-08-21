using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum MultiSeedBiomeGrowthStatus
    {
        Completed,
        InvalidInput,
        RetryRequired
    }

    public sealed class MultiSeedBiomeGrowthResult
    {
        private readonly IReadOnlyList<MultiSeedBiomeGrowthError> errors;

        private MultiSeedBiomeGrowthResult(
            MultiSeedBiomeGrowthStatus status,
            MultiSeedBiomeGrowthPublication publication,
            MultiSeedBiomeGrowthDiagnostics diagnostics,
            IEnumerable<MultiSeedBiomeGrowthError> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            var ordered = SortAndDedupe(errors);
            switch (status)
            {
                case MultiSeedBiomeGrowthStatus.Completed:
                    if (publication == null || diagnostics == null || ordered.Count != 0)
                        throw new ArgumentException("Completed growth requires publication and diagnostics only.");
                    break;
                case MultiSeedBiomeGrowthStatus.InvalidInput:
                    if (publication != null || diagnostics != null || ordered.Count == 0)
                        throw new ArgumentException("Invalid growth requires structural errors only.");
                    break;
                case MultiSeedBiomeGrowthStatus.RetryRequired:
                    if (publication != null || diagnostics == null || ordered.Count == 0)
                        throw new ArgumentException("Retry growth requires diagnostics and errors only.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status));
            }
            Status = status;
            Publication = publication;
            Diagnostics = diagnostics;
            this.errors = new ReadOnlyCollection<MultiSeedBiomeGrowthError>(ordered);
        }

        public MultiSeedBiomeGrowthStatus Status { get; }
        public bool Succeeded => Status == MultiSeedBiomeGrowthStatus.Completed;
        public bool RetryRequired => Status == MultiSeedBiomeGrowthStatus.RetryRequired;
        public MultiSeedBiomeGrowthPublication Publication { get; }
        public MultiSeedBiomeGrowthDiagnostics Diagnostics { get; }
        public IReadOnlyList<MultiSeedBiomeGrowthError> Errors => errors;

        internal static MultiSeedBiomeGrowthResult Completed(
            MultiSeedBiomeGrowthPublication publication,
            MultiSeedBiomeGrowthDiagnostics diagnostics)
        {
            return new MultiSeedBiomeGrowthResult(
                MultiSeedBiomeGrowthStatus.Completed, publication, diagnostics,
                Array.Empty<MultiSeedBiomeGrowthError>());
        }

        internal static MultiSeedBiomeGrowthResult Invalid(IEnumerable<MultiSeedBiomeGrowthError> errors)
        {
            return new MultiSeedBiomeGrowthResult(
                MultiSeedBiomeGrowthStatus.InvalidInput, null, null, errors);
        }

        internal static MultiSeedBiomeGrowthResult Retry(
            MultiSeedBiomeGrowthDiagnostics diagnostics,
            IEnumerable<MultiSeedBiomeGrowthError> errors)
        {
            return new MultiSeedBiomeGrowthResult(
                MultiSeedBiomeGrowthStatus.RetryRequired, null, diagnostics, errors);
        }

        internal static int Compare(MultiSeedBiomeGrowthError left, MultiSeedBiomeGrowthError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = string.Compare(left.DefinitionId, right.DefinitionId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.BiomeId, right.BiomeId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = ComparePatch(left.PatchId, right.PatchId);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            if (value != 0) return value;
            value = left.RequiredCount.CompareTo(right.RequiredCount);
            if (value != 0) return value;
            value = left.AvailableCount.CompareTo(right.AvailableCount);
            if (value != 0) return value;
            return string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        private static int ComparePatch(BiomePatchId? left, BiomePatchId? right)
        {
            if (!left.HasValue) return right.HasValue ? -1 : 0;
            return right.HasValue ? left.Value.CompareTo(right.Value) : 1;
        }

        private static List<MultiSeedBiomeGrowthError> SortAndDedupe(
            IEnumerable<MultiSeedBiomeGrowthError> source)
        {
            var values = new List<MultiSeedBiomeGrowthError>();
            foreach (var value in source) if (value != null) values.Add(value);
            values.Sort(Compare);
            var result = new List<MultiSeedBiomeGrowthError>();
            foreach (var value in values)
                if (result.Count == 0 || Compare(result[result.Count - 1], value) != 0)
                    result.Add(value);
            return result;
        }
    }
}
