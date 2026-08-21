using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct MandatoryRouteTerminalId : IEquatable<MandatoryRouteTerminalId>, IComparable<MandatoryRouteTerminalId>
    {
        private readonly string value;

        public MandatoryRouteTerminalId(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (!IsCanonical(value))
                throw new ArgumentException("Terminal IDs must match ^[A-Z0-9_]+$.", nameof(value));
            this.value = value;
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => value != null;

        public static bool TryCreate(string value, out MandatoryRouteTerminalId result)
        {
            if (!IsCanonical(value))
            {
                result = default(MandatoryRouteTerminalId);
                return false;
            }
            result = new MandatoryRouteTerminalId(value);
            return true;
        }

        public bool Equals(MandatoryRouteTerminalId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is MandatoryRouteTerminalId other && Equals(other);

        public int CompareTo(MandatoryRouteTerminalId other) =>
            string.Compare(Value, other.Value, StringComparison.Ordinal);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)2166136261;
                var text = Value;
                for (var index = 0; index < text.Length; index++)
                    hash = (hash ^ text[index]) * 16777619;
                return hash;
            }
        }

        public override string ToString() => Value;
        public static bool operator ==(MandatoryRouteTerminalId left, MandatoryRouteTerminalId right) => left.Equals(right);
        public static bool operator !=(MandatoryRouteTerminalId left, MandatoryRouteTerminalId right) => !left.Equals(right);

        private static bool IsCanonical(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                if ((character < 'A' || character > 'Z') &&
                    (character < '0' || character > '9') && character != '_')
                    return false;
            }
            return true;
        }
    }
}
