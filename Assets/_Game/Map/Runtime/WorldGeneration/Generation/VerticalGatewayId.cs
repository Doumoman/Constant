using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct VerticalGatewayId : IEquatable<VerticalGatewayId>, IComparable<VerticalGatewayId>
    {
        private readonly string value;

        public VerticalGatewayId(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (!IsCanonical(value)) throw new ArgumentException("Gateway IDs must match ^VGW_[0-9]{2}_[A-Z0-9_]+$.", nameof(value));
            this.value = value;
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => value != null;

        public static bool TryCreate(string value, out VerticalGatewayId result)
        {
            if (!IsCanonical(value))
            {
                result = default(VerticalGatewayId);
                return false;
            }
            result = new VerticalGatewayId(value);
            return true;
        }

        public bool Equals(VerticalGatewayId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is VerticalGatewayId other && Equals(other);
        public int CompareTo(VerticalGatewayId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
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
        public static bool operator ==(VerticalGatewayId left, VerticalGatewayId right) => left.Equals(right);
        public static bool operator !=(VerticalGatewayId left, VerticalGatewayId right) => !left.Equals(right);

        private static bool IsCanonical(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 8 || !text.StartsWith("VGW_", StringComparison.Ordinal)) return false;
            if (!IsDigit(text[4]) || !IsDigit(text[5]) || text[6] != '_') return false;
            for (var index = 7; index < text.Length; index++)
            {
                var character = text[index];
                if ((character < 'A' || character > 'Z') && (character < '0' || character > '9') && character != '_') return false;
            }
            return true;
        }

        private static bool IsDigit(char value) => value >= '0' && value <= '9';
    }
}
