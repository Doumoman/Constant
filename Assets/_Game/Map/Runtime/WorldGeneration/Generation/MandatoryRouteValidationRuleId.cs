using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct MandatoryRouteValidationRuleId : IEquatable<MandatoryRouteValidationRuleId>, IComparable<MandatoryRouteValidationRuleId>
    {
        public MandatoryRouteValidationRuleId(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (!MatchesGrammar(value)) throw new ArgumentException("Rule ID must match VAL_ROUTE_[A-Z0-9_]+.", nameof(value));
            Value = value;
        }

        public string Value { get; }
        public bool IsValid => MatchesGrammar(Value);
        public int CompareTo(MandatoryRouteValidationRuleId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(MandatoryRouteValidationRuleId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is MandatoryRouteValidationRuleId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(MandatoryRouteValidationRuleId left, MandatoryRouteValidationRuleId right) => left.Equals(right);
        public static bool operator !=(MandatoryRouteValidationRuleId left, MandatoryRouteValidationRuleId right) => !left.Equals(right);

        public static bool TryCreate(string value, out MandatoryRouteValidationRuleId id)
        {
            if (!MatchesGrammar(value)) { id = default; return false; }
            id = new MandatoryRouteValidationRuleId(value);
            return true;
        }

        private static bool MatchesGrammar(string value)
        {
            const string prefix = "VAL_ROUTE_";
            if (string.IsNullOrEmpty(value) || value.Length <= prefix.Length || !value.StartsWith(prefix, StringComparison.Ordinal)) return false;
            for (var index = prefix.Length; index < value.Length; index++)
            {
                var c = value[index];
                if (!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')) return false;
            }
            return true;
        }
    }
}
