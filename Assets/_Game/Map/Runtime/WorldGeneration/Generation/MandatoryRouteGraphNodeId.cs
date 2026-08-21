using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct MandatoryRouteGraphNodeId : IEquatable<MandatoryRouteGraphNodeId>, IComparable<MandatoryRouteGraphNodeId>
    {
        public MandatoryRouteGraphNodeId(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (!MatchesGrammar(value)) throw new ArgumentException("Node ID must match NODE_[0-9]{3}_[A-Z0-9_]+.", nameof(value));
            Value = value;
        }

        public string Value { get; }
        public bool IsValid => MatchesGrammar(Value);
        public int CompareTo(MandatoryRouteGraphNodeId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(MandatoryRouteGraphNodeId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is MandatoryRouteGraphNodeId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(MandatoryRouteGraphNodeId left, MandatoryRouteGraphNodeId right) => left.Equals(right);
        public static bool operator !=(MandatoryRouteGraphNodeId left, MandatoryRouteGraphNodeId right) => !left.Equals(right);

        public static bool TryCreate(string value, out MandatoryRouteGraphNodeId id)
        {
            if (!MatchesGrammar(value)) { id = default; return false; }
            id = new MandatoryRouteGraphNodeId(value); return true;
        }

        private static bool MatchesGrammar(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 10 || !value.StartsWith("NODE_", StringComparison.Ordinal) || value[8] != '_') return false;
            for (var index = 5; index < 8; index++) if (value[index] < '0' || value[index] > '9') return false;
            for (var index = 9; index < value.Length; index++)
            {
                var c = value[index];
                if (!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')) return false;
            }
            return true;
        }
    }
}
