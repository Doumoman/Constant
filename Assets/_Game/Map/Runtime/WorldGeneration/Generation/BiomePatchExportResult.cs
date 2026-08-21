using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum BiomePatchExportStatus
    {
        Completed,
        InvalidInput
    }

    public sealed class BiomePatchExportResult
    {
        private readonly IReadOnlyList<BiomePatchExportError> errors;

        private BiomePatchExportResult(
            BiomePatchExportStatus status,
            BiomePatchExportPublication publication,
            IEnumerable<BiomePatchExportError> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            var ordered = SortAndDedupe(errors);
            if (status == BiomePatchExportStatus.Completed)
            {
                if (publication == null || ordered.Count != 0)
                    throw new ArgumentException("Completed export requires one publication and no errors.");
            }
            else if (status == BiomePatchExportStatus.InvalidInput)
            {
                if (publication != null || ordered.Count == 0)
                    throw new ArgumentException("Invalid export requires errors and no publication.");
            }
            else throw new ArgumentOutOfRangeException(nameof(status));

            Status = status;
            Publication = publication;
            this.errors = new ReadOnlyCollection<BiomePatchExportError>(ordered);
        }

        public BiomePatchExportStatus Status { get; }
        public bool Succeeded => Status == BiomePatchExportStatus.Completed;
        public BiomePatchExportPublication Publication { get; }
        public IReadOnlyList<BiomePatchExportError> Errors => errors;

        internal static BiomePatchExportResult Completed(BiomePatchExportPublication publication)
        {
            return new BiomePatchExportResult(
                BiomePatchExportStatus.Completed,
                publication,
                Array.Empty<BiomePatchExportError>());
        }

        internal static BiomePatchExportResult Invalid(IEnumerable<BiomePatchExportError> errors)
        {
            return new BiomePatchExportResult(BiomePatchExportStatus.InvalidInput, null, errors);
        }

        internal static int Compare(BiomePatchExportError left, BiomePatchExportError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = string.Compare(left.DefinitionId, right.DefinitionId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            if (value != 0) return value;
            value = left.RequiredCount.CompareTo(right.RequiredCount);
            if (value != 0) return value;
            value = left.AvailableCount.CompareTo(right.AvailableCount);
            if (value != 0) return value;
            return string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        private static List<BiomePatchExportError> SortAndDedupe(IEnumerable<BiomePatchExportError> source)
        {
            var values = new List<BiomePatchExportError>();
            foreach (var value in source) if (value != null) values.Add(value);
            values.Sort(Compare);
            var result = new List<BiomePatchExportError>();
            foreach (var value in values)
                if (result.Count == 0 || Compare(result[result.Count - 1], value) != 0)
                    result.Add(value);
            return result;
        }
    }
}
