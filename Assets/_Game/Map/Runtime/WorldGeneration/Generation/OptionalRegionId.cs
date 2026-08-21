using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct OptionalRegionId : IEquatable<OptionalRegionId>, IComparable<OptionalRegionId>
    {
        private readonly string value;

        public OptionalRegionId(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!MatchesGrammar(value))
            {
                throw new ArgumentException("Optional region IDs must match ^[A-Z0-9_]+$.", nameof(value));
            }

            this.value = value;
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => value != null;

        public static bool TryCreate(string value, out OptionalRegionId result)
        {
            if (!MatchesGrammar(value))
            {
                result = default(OptionalRegionId);
                return false;
            }

            result = new OptionalRegionId(value);
            return true;
        }

        public int CompareTo(OptionalRegionId other)
        {
            return string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        public bool Equals(OptionalRegionId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is OptionalRegionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)2166136261;
                var text = Value;
                for (var index = 0; index < text.Length; index++)
                {
                    hash = (hash ^ text[index]) * 16777619;
                }

                return hash;
            }
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(OptionalRegionId left, OptionalRegionId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OptionalRegionId left, OptionalRegionId right)
        {
            return !left.Equals(right);
        }

        private static bool MatchesGrammar(string candidate)
        {
            if (string.IsNullOrEmpty(candidate))
            {
                return false;
            }

            for (var index = 0; index < candidate.Length; index++)
            {
                var character = candidate[index];
                if ((character < 'A' || character > 'Z') &&
                    (character < '0' || character > '9') &&
                    character != '_')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
