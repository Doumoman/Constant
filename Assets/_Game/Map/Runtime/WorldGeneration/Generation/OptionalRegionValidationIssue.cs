using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalRegionValidationIssue
    {
        public OptionalRegionValidationIssue(
            OptionalRegionValidationIssueCode code,
            OptionalRegionId regionId,
            int sectorIndex,
            string source,
            string field,
            string message)
        {
            if (!Enum.IsDefined(typeof(OptionalRegionValidationIssueCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            if (sectorIndex < -1 || sectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            RequireCanonical(source, nameof(source));
            RequireCanonical(field, nameof(field));
            RequireCanonical(message, nameof(message));

            Code = code;
            RegionId = regionId;
            SectorIndex = sectorIndex;
            Source = source;
            Field = field;
            Message = message;
        }

        public OptionalRegionValidationIssueCode Code { get; }
        public OptionalRegionId RegionId { get; }
        public int SectorIndex { get; }
        public string Source { get; }
        public string Field { get; }
        public string Message { get; }

        internal static int Compare(OptionalRegionValidationIssue left, OptionalRegionValidationIssue right)
        {
            var code = left.Code.CompareTo(right.Code);
            if (code != 0) return code;
            var region = left.RegionId.CompareTo(right.RegionId);
            if (region != 0) return region;
            var sector = left.SectorIndex.CompareTo(right.SectorIndex);
            if (sector != 0) return sector;
            var source = string.Compare(left.Source, right.Source, StringComparison.Ordinal);
            if (source != 0) return source;
            var field = string.Compare(left.Field, right.Field, StringComparison.Ordinal);
            return field != 0 ? field : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        internal bool SameIdentity(OptionalRegionValidationIssue other)
        {
            return other != null && Code == other.Code && RegionId == other.RegionId &&
                   SectorIndex == other.SectorIndex &&
                   string.Equals(Source, other.Source, StringComparison.Ordinal) &&
                   string.Equals(Field, other.Field, StringComparison.Ordinal) &&
                   string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        private static void RequireCanonical(string value, string parameterName)
        {
            if (string.IsNullOrEmpty(value) ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Value must be canonical non-empty text.", parameterName);
        }
    }
}
