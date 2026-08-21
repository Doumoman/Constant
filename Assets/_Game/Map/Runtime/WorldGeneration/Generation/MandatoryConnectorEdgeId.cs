using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct MandatoryConnectorEdgeId : IEquatable<MandatoryConnectorEdgeId>, IComparable<MandatoryConnectorEdgeId>
    {
        private readonly string value;

        public MandatoryConnectorEdgeId(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (!IsCanonical(value))
                throw new ArgumentException("Edge IDs must match ^EDGE_[0-9]{2}_[A-Z0-9_]+__TO__[A-Z0-9_]+$ with ordinal endpoint order.", nameof(value));
            this.value = value;
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => value != null;

        public static bool TryCreate(string value, out MandatoryConnectorEdgeId result)
        {
            if (!IsCanonical(value))
            {
                result = default(MandatoryConnectorEdgeId);
                return false;
            }
            result = new MandatoryConnectorEdgeId(value);
            return true;
        }

        public bool Equals(MandatoryConnectorEdgeId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is MandatoryConnectorEdgeId other && Equals(other);
        public int CompareTo(MandatoryConnectorEdgeId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)2166136261;
                for (var index = 0; index < Value.Length; index++) hash = (hash ^ Value[index]) * 16777619;
                return hash;
            }
        }
        public override string ToString() => Value;
        public static bool operator ==(MandatoryConnectorEdgeId left, MandatoryConnectorEdgeId right) => left.Equals(right);
        public static bool operator !=(MandatoryConnectorEdgeId left, MandatoryConnectorEdgeId right) => !left.Equals(right);

        private static bool IsCanonical(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 18 || !text.StartsWith("EDGE_", StringComparison.Ordinal)) return false;
            if (!IsDigit(text[5]) || !IsDigit(text[6]) || text[7] != '_') return false;
            var separator = text.IndexOf("__TO__", 8, StringComparison.Ordinal);
            if (separator <= 8 || separator != text.LastIndexOf("__TO__", StringComparison.Ordinal) || separator + 6 >= text.Length) return false;
            var first = text.Substring(8, separator - 8);
            var second = text.Substring(separator + 6);
            return IsToken(first) && IsToken(second) && string.Compare(first, second, StringComparison.Ordinal) < 0;
        }

        private static bool IsDigit(char value) => value >= '0' && value <= '9';
        private static bool IsToken(string text)
        {
            if (text.Length == 0) return false;
            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                if ((character < 'A' || character > 'Z') && (character < '0' || character > '9') && character != '_') return false;
            }
            return true;
        }
    }
}
