using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct BiomePatchId : IEquatable<BiomePatchId>, IComparable<BiomePatchId>
    {
        private readonly string value;

        public BiomePatchId(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (!ReservationValidation.IsCanonicalId(value, false))
                throw new ArgumentException("Biome patch IDs must match ^[A-Z0-9_]+$.", nameof(value));

            this.value = value;
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => value != null;

        public static bool TryCreate(string value, out BiomePatchId result)
        {
            if (!ReservationValidation.IsCanonicalId(value, false))
            {
                result = default(BiomePatchId);
                return false;
            }

            result = new BiomePatchId(value);
            return true;
        }

        public bool Equals(BiomePatchId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is BiomePatchId other && Equals(other);
        }

        public int CompareTo(BiomePatchId other)
        {
            return string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

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

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(BiomePatchId left, BiomePatchId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BiomePatchId left, BiomePatchId right)
        {
            return !left.Equals(right);
        }
    }
}
