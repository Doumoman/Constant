using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct SiteReservationId : IEquatable<SiteReservationId>, IComparable<SiteReservationId>
    {
        private readonly string value;

        public SiteReservationId(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!ReservationValidation.IsCanonicalId(value, false))
            {
                throw new ArgumentException("Reservation IDs must match ^[A-Z0-9_]+$.", nameof(value));
            }

            this.value = value;
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => value != null;

        public static bool TryCreate(string value, out SiteReservationId result)
        {
            if (!ReservationValidation.IsCanonicalId(value, false))
            {
                result = default(SiteReservationId);
                return false;
            }

            result = new SiteReservationId(value);
            return true;
        }

        public bool Equals(SiteReservationId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SiteReservationId other && Equals(other);
        }

        public int CompareTo(SiteReservationId other)
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
                {
                    hash = (hash ^ text[index]) * 16777619;
                }

                return hash;
            }
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(SiteReservationId left, SiteReservationId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SiteReservationId left, SiteReservationId right)
        {
            return !left.Equals(right);
        }
    }

    internal static class ReservationValidation
    {
        public static bool IsCanonicalId(string value, bool allowEmpty)
        {
            if (value == null || (!allowEmpty && value.Length == 0))
            {
                return false;
            }

            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if ((character < 'A' || character > 'Z') &&
                    (character < '0' || character > '9') &&
                    character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        public static void RequireCanonicalId(string value, string parameterName, bool allowEmpty)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (!IsCanonicalId(value, allowEmpty))
            {
                throw new ArgumentException("Value must be an ordinal canonical ID.", parameterName);
            }
        }
    }
}
