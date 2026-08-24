using System;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public readonly struct MoonpalaceBoundaryProfileId :
        IEquatable<MoonpalaceBoundaryProfileId>,
        IComparable<MoonpalaceBoundaryProfileId>
    {
        private readonly string canonicalId;

        public MoonpalaceBoundaryProfileId(string canonicalId)
        {
            this.canonicalId = RequireStableToken(canonicalId, nameof(canonicalId));
        }

        public bool IsDefined => !string.IsNullOrEmpty(canonicalId);

        public string CanonicalId
        {
            get
            {
                EnsureDefined();
                return canonicalId;
            }
        }

        public int CompareTo(MoonpalaceBoundaryProfileId other)
        {
            EnsureDefined();
            other.EnsureDefined();
            return string.Compare(canonicalId, other.canonicalId, StringComparison.Ordinal);
        }

        public bool Equals(MoonpalaceBoundaryProfileId other)
        {
            return string.Equals(canonicalId, other.canonicalId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is MoonpalaceBoundaryProfileId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StableOrdinalHash(canonicalId);
        }

        public override string ToString()
        {
            return CanonicalId;
        }

        public static bool operator ==(MoonpalaceBoundaryProfileId left, MoonpalaceBoundaryProfileId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MoonpalaceBoundaryProfileId left, MoonpalaceBoundaryProfileId right)
        {
            return !left.Equals(right);
        }

        private static string RequireStableToken(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException("Boundary profile IDs cannot be null, empty, whitespace, or padded.", parameterName);
            }

            return value;
        }

        private static int StableOrdinalHash(string value)
        {
            if (value == null) return 0;
            unchecked
            {
                var hash = 17;
                for (var index = 0; index < value.Length; index++) hash = (hash * 31) + value[index];
                return hash;
            }
        }

        private void EnsureDefined()
        {
            if (!IsDefined) throw new InvalidOperationException("The boundary profile ID is undefined.");
        }
    }
}
