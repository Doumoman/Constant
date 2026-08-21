using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct MandatoryRouteLoopId : IEquatable<MandatoryRouteLoopId>, IComparable<MandatoryRouteLoopId>
    {
        private readonly string value;

        public MandatoryRouteLoopId(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (!IsCanonical(value)) throw new ArgumentException("Loop IDs must match ^LOOP_[0-9]{2}_[A-Z0-9_]+$.", nameof(value));
            this.value = value;
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => value != null;

        public static bool TryCreate(string value, out MandatoryRouteLoopId result)
        {
            if (!IsCanonical(value))
            {
                result = default(MandatoryRouteLoopId);
                return false;
            }
            result = new MandatoryRouteLoopId(value);
            return true;
        }

        public bool Equals(MandatoryRouteLoopId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is MandatoryRouteLoopId other && Equals(other);
        public int CompareTo(MandatoryRouteLoopId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
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
        public static bool operator ==(MandatoryRouteLoopId left, MandatoryRouteLoopId right) => left.Equals(right);
        public static bool operator !=(MandatoryRouteLoopId left, MandatoryRouteLoopId right) => !left.Equals(right);

        private static bool IsCanonical(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 9 || !text.StartsWith("LOOP_", StringComparison.Ordinal)) return false;
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
