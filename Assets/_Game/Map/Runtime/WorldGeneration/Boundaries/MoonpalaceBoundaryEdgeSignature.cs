using System;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public readonly struct MoonpalaceBoundaryEdgeSignature :
        IEquatable<MoonpalaceBoundaryEdgeSignature>,
        IComparable<MoonpalaceBoundaryEdgeSignature>
    {
        private readonly string signatureId;

        public MoonpalaceBoundaryEdgeSignature(string signatureId)
        {
            this.signatureId = RequireStableToken(signatureId, nameof(signatureId));
        }

        public bool IsDefined => !string.IsNullOrEmpty(signatureId);

        public string SignatureId
        {
            get
            {
                EnsureDefined();
                return signatureId;
            }
        }

        public int CompareTo(MoonpalaceBoundaryEdgeSignature other)
        {
            EnsureDefined();
            other.EnsureDefined();
            return string.Compare(signatureId, other.signatureId, StringComparison.Ordinal);
        }

        public bool Equals(MoonpalaceBoundaryEdgeSignature other)
        {
            return string.Equals(signatureId, other.signatureId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is MoonpalaceBoundaryEdgeSignature other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StableOrdinalHash(signatureId);
        }

        public override string ToString()
        {
            return SignatureId;
        }

        public static bool operator ==(MoonpalaceBoundaryEdgeSignature left, MoonpalaceBoundaryEdgeSignature right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MoonpalaceBoundaryEdgeSignature left, MoonpalaceBoundaryEdgeSignature right)
        {
            return !left.Equals(right);
        }

        private static string RequireStableToken(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException("Boundary edge signatures cannot be null, empty, whitespace, or padded.", parameterName);
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
            if (!IsDefined) throw new InvalidOperationException("The boundary edge signature is undefined.");
        }
    }
}
