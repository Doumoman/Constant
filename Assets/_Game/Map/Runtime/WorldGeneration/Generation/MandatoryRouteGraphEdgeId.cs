using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct MandatoryRouteGraphEdgeId : IEquatable<MandatoryRouteGraphEdgeId>, IComparable<MandatoryRouteGraphEdgeId>
    {
        public MandatoryRouteGraphEdgeId(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (!MatchesGrammar(value)) throw new ArgumentException("Edge ID must match EDGE_[0-9]{3}_[LRUD]_[A-Z0-9_]+.", nameof(value));
            Value = value;
        }

        public string Value { get; }
        public bool IsValid => MatchesGrammar(Value);
        public int CompareTo(MandatoryRouteGraphEdgeId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(MandatoryRouteGraphEdgeId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is MandatoryRouteGraphEdgeId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(MandatoryRouteGraphEdgeId left, MandatoryRouteGraphEdgeId right) => left.Equals(right);
        public static bool operator !=(MandatoryRouteGraphEdgeId left, MandatoryRouteGraphEdgeId right) => !left.Equals(right);

        public static bool TryCreate(string value, out MandatoryRouteGraphEdgeId id)
        {
            if (!MatchesGrammar(value)) { id = default; return false; }
            id = new MandatoryRouteGraphEdgeId(value); return true;
        }

        private static bool MatchesGrammar(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 12 || !value.StartsWith("EDGE_", StringComparison.Ordinal) || value[8] != '_' || value[10] != '_') return false;
            for (var index = 5; index < 8; index++) if (value[index] < '0' || value[index] > '9') return false;
            if (value[9] != 'L' && value[9] != 'R' && value[9] != 'U' && value[9] != 'D') return false;
            for (var index = 11; index < value.Length; index++)
            {
                var c = value[index];
                if (!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')) return false;
            }
            return true;
        }
    }
}
