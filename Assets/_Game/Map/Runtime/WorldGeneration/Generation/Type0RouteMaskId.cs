using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct Type0RouteMaskId : IEquatable<Type0RouteMaskId>, IComparable<Type0RouteMaskId>
    {
        private const string Prefix = "ROUTE_T0_";
        private readonly string value;

        public Type0RouteMaskId(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!MatchesGrammar(value))
            {
                throw new ArgumentException("Type0 route mask IDs must match ^ROUTE_T0_[A-Z0-9_]+$.", nameof(value));
            }

            this.value = value;
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => value != null;

        public static bool TryCreate(string value, out Type0RouteMaskId result)
        {
            if (!MatchesGrammar(value))
            {
                result = default(Type0RouteMaskId);
                return false;
            }

            result = new Type0RouteMaskId(value);
            return true;
        }

        public int CompareTo(Type0RouteMaskId other)
        {
            return string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        public bool Equals(Type0RouteMaskId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is Type0RouteMaskId other && Equals(other);
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

        public static bool operator ==(Type0RouteMaskId left, Type0RouteMaskId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Type0RouteMaskId left, Type0RouteMaskId right)
        {
            return !left.Equals(right);
        }

        private static bool MatchesGrammar(string candidate)
        {
            if (string.IsNullOrEmpty(candidate) ||
                candidate.Length <= Prefix.Length ||
                !candidate.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return false;
            }

            for (var index = Prefix.Length; index < candidate.Length; index++)
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
