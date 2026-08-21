using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class VillageDistanceBucket
    {
        internal VillageDistanceBucket(
            int bucketOrdinal,
            int minDistanceInclusive,
            int maxDistanceInclusive,
            int weight,
            int rollMinInclusive,
            int rollMaxInclusive)
        {
            if (bucketOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(bucketOrdinal));
            if (minDistanceInclusive < 0 || maxDistanceInclusive < minDistanceInclusive)
                throw new ArgumentOutOfRangeException(nameof(minDistanceInclusive));
            if (weight <= 0) throw new ArgumentOutOfRangeException(nameof(weight));
            if (rollMinInclusive < 0 || rollMaxInclusive < rollMinInclusive ||
                rollMaxInclusive - rollMinInclusive + 1 != weight)
                throw new ArgumentOutOfRangeException(nameof(rollMinInclusive));

            BucketOrdinal = bucketOrdinal;
            MinDistanceInclusive = minDistanceInclusive;
            MaxDistanceInclusive = maxDistanceInclusive;
            Weight = weight;
            RollMinInclusive = rollMinInclusive;
            RollMaxInclusive = rollMaxInclusive;
        }

        public int BucketOrdinal { get; }
        public int MinDistanceInclusive { get; }
        public int MaxDistanceInclusive { get; }
        public int Weight { get; }
        public int RollMinInclusive { get; }
        public int RollMaxInclusive { get; }

        public bool Contains(int distance) =>
            distance >= MinDistanceInclusive && distance <= MaxDistanceInclusive;
    }

    public sealed class VillageDistanceBucketCatalog
    {
        private const string StarterValue = "2-3:20|4-6:50|7-10:30";
        private readonly IReadOnlyList<VillageDistanceBucket> buckets;

        private VillageDistanceBucketCatalog(IEnumerable<VillageDistanceBucket> buckets)
        {
            var snapshot = new List<VillageDistanceBucket>(buckets);
            this.buckets = new ReadOnlyCollection<VillageDistanceBucket>(snapshot);
            var total = 0;
            foreach (var bucket in snapshot) checked { total += bucket.Weight; }
            TotalWeight = total;
        }

        public IReadOnlyList<VillageDistanceBucket> Buckets => buckets;
        public int TotalWeight { get; }

        public VillageDistanceBucket SelectByRoll(int roll)
        {
            if (roll < 0 || roll >= TotalWeight) throw new ArgumentOutOfRangeException(nameof(roll));
            foreach (var bucket in buckets)
                if (roll >= bucket.RollMinInclusive && roll <= bucket.RollMaxInclusive)
                    return bucket;
            throw new InvalidOperationException("The roll is not covered by a distance bucket.");
        }

        public static bool TryParse(
            string value,
            out VillageDistanceBucketCatalog catalog,
            out string error)
        {
            catalog = null;
            error = string.Empty;
            if (value == null)
            {
                error = "Distance bucket text is missing.";
                return false;
            }
            if (value.Length == 0)
            {
                error = "Distance bucket text is empty.";
                return false;
            }

            var parts = value.Split('|');
            if (parts.Length != 3)
            {
                error = "Distance bucket text must contain exactly three ranges.";
                return false;
            }

            var parsed = new List<VillageDistanceBucket>(3);
            var roll = 0;
            var previousMaximum = -1;
            for (var index = 0; index < parts.Length; index++)
            {
                var part = parts[index];
                var dash = part.IndexOf('-');
                var colon = part.IndexOf(':');
                if (dash <= 0 || colon <= dash + 1 || colon == part.Length - 1 ||
                    part.IndexOf('-', dash + 1) >= 0 || part.IndexOf(':', colon + 1) >= 0)
                {
                    error = "Each distance bucket must use exact min-max:weight grammar.";
                    return false;
                }
                if (!TryParseAscii(part, 0, dash, out var minimum) ||
                    !TryParseAscii(part, dash + 1, colon - dash - 1, out var maximum) ||
                    !TryParseAscii(part, colon + 1, part.Length - colon - 1, out var weight))
                {
                    error = "Distance bucket numbers must be canonical ASCII decimals.";
                    return false;
                }
                if (minimum > maximum || weight <= 0)
                {
                    error = "Distance bucket ranges and weights must be positive and ordered.";
                    return false;
                }
                if (index > 0 && minimum != previousMaximum + 1)
                {
                    error = "Distance bucket ranges must be strictly ascending and contiguous.";
                    return false;
                }
                if (roll > int.MaxValue - weight)
                {
                    error = "Distance bucket total weight overflowed.";
                    return false;
                }
                parsed.Add(new VillageDistanceBucket(
                    index, minimum, maximum, weight, roll, roll + weight - 1));
                previousMaximum = maximum;
                roll += weight;
            }

            if (roll != 100 || !string.Equals(value, StarterValue, StringComparison.Ordinal))
            {
                error = "Distance buckets must equal the frozen starter profile.";
                return false;
            }

            catalog = new VillageDistanceBucketCatalog(parsed);
            return true;
        }

        private static bool TryParseAscii(string value, int start, int length, out int result)
        {
            result = 0;
            if (length <= 0 || (length > 1 && value[start] == '0')) return false;
            for (var index = start; index < start + length; index++)
            {
                var character = value[index];
                if (character < '0' || character > '9') return false;
                var digit = character - '0';
                if (result > (int.MaxValue - digit) / 10) return false;
                result = result * 10 + digit;
            }
            return true;
        }
    }
}
