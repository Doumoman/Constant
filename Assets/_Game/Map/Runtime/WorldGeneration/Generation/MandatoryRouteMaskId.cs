using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct MandatoryRouteMaskId : IEquatable<MandatoryRouteMaskId>, IComparable<MandatoryRouteMaskId>
    {
        private readonly string value;

        public MandatoryRouteMaskId(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (!IsCanonical(value)) throw new ArgumentException("Route mask IDs must match ^[A-Z0-9_]+$.", nameof(value));
            this.value = value;
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => value != null;

        public static bool TryCreate(string value, out MandatoryRouteMaskId result)
        {
            if (!IsCanonical(value))
            {
                result = default(MandatoryRouteMaskId);
                return false;
            }
            result = new MandatoryRouteMaskId(value);
            return true;
        }

        public bool Equals(MandatoryRouteMaskId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is MandatoryRouteMaskId other && Equals(other);
        public int CompareTo(MandatoryRouteMaskId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
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
        public static bool operator ==(MandatoryRouteMaskId left, MandatoryRouteMaskId right) => left.Equals(right);
        public static bool operator !=(MandatoryRouteMaskId left, MandatoryRouteMaskId right) => !left.Equals(right);

        private static bool IsCanonical(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                if ((character < 'A' || character > 'Z') && (character < '0' || character > '9') && character != '_') return false;
            }
            return true;
        }
    }
}
