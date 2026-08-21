using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct HorizontalBackboneSegmentId : IEquatable<HorizontalBackboneSegmentId>, IComparable<HorizontalBackboneSegmentId>
    {
        private readonly string value;

        public HorizontalBackboneSegmentId(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (!IsCanonical(value)) throw new ArgumentException("Segment IDs must match ^HSEG_[0-9]{2}_[A-Z0-9_]+$.", nameof(value));
            this.value = value;
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => value != null;

        public static bool TryCreate(string value, out HorizontalBackboneSegmentId result)
        {
            if (!IsCanonical(value))
            {
                result = default(HorizontalBackboneSegmentId);
                return false;
            }
            result = new HorizontalBackboneSegmentId(value);
            return true;
        }

        public bool Equals(HorizontalBackboneSegmentId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is HorizontalBackboneSegmentId other && Equals(other);
        public int CompareTo(HorizontalBackboneSegmentId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
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
        public static bool operator ==(HorizontalBackboneSegmentId left, HorizontalBackboneSegmentId right) => left.Equals(right);
        public static bool operator !=(HorizontalBackboneSegmentId left, HorizontalBackboneSegmentId right) => !left.Equals(right);

        private static bool IsCanonical(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 9 || !text.StartsWith("HSEG_", StringComparison.Ordinal)) return false;
            if (!IsDigit(text[5]) || !IsDigit(text[6]) || text[7] != '_') return false;
            for (var index = 8; index < text.Length; index++)
            {
                var character = text[index];
                if ((character < 'A' || character > 'Z') && (character < '0' || character > '9') && character != '_') return false;
            }
            return true;
        }

        private static bool IsDigit(char value) => value >= '0' && value <= '9';
    }
}
