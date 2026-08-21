using System;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public readonly struct MicrochunkId : IEquatable<MicrochunkId>, IComparable<MicrochunkId>
    {
        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public MicrochunkId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Microchunk ID cannot be null, empty, or whitespace.", nameof(value));
            }

            Value = value;
        }

        public static bool TryCreate(string value, out MicrochunkId id)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                id = default;
                return false;
            }

            id = new MicrochunkId(value);
            return true;
        }

        public int CompareTo(MicrochunkId other)
        {
            return string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        public bool Equals(MicrochunkId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is MicrochunkId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(MicrochunkId left, MicrochunkId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MicrochunkId left, MicrochunkId right)
        {
            return !left.Equals(right);
        }
    }
}
