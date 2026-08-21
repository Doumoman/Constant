using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct OptionalAccessClueId : IEquatable<OptionalAccessClueId>, IComparable<OptionalAccessClueId>
    {
        private const string Prefix = "CLUE_OPT_REGION_";
        private readonly string value;

        public OptionalAccessClueId(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!MatchesGrammar(value))
            {
                throw new ArgumentException(
                    "Optional access clue IDs must match ^CLUE_OPT_REGION_[0-9]{4}_[A-Z0-9_]+$.",
                    nameof(value));
            }

            this.value = value;
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => value != null;

        public static bool TryCreate(string value, out OptionalAccessClueId result)
        {
            if (!MatchesGrammar(value))
            {
                result = default(OptionalAccessClueId);
                return false;
            }

            result = new OptionalAccessClueId(value);
            return true;
        }

        public int CompareTo(OptionalAccessClueId other)
        {
            return string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        public bool Equals(OptionalAccessClueId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is OptionalAccessClueId other && Equals(other);
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

        public static bool operator ==(OptionalAccessClueId left, OptionalAccessClueId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OptionalAccessClueId left, OptionalAccessClueId right)
        {
            return !left.Equals(right);
        }

        private static bool MatchesGrammar(string candidate)
        {
            if (string.IsNullOrEmpty(candidate) ||
                candidate.Length <= Prefix.Length + 5 ||
                !candidate.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return false;
            }

            for (var index = Prefix.Length; index < Prefix.Length + 4; index++)
            {
                if (candidate[index] < '0' || candidate[index] > '9')
                {
                    return false;
                }
            }

            if (candidate[Prefix.Length + 4] != '_')
            {
                return false;
            }

            for (var index = Prefix.Length + 5; index < candidate.Length; index++)
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
