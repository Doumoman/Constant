using System;
using System.Globalization;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public readonly struct MoonpalaceBoundaryCandidateKey :
        IEquatable<MoonpalaceBoundaryCandidateKey>,
        IComparable<MoonpalaceBoundaryCandidateKey>
    {
        public MoonpalaceBoundaryCandidateKey(
            MoonpalaceBiomePair pair,
            MoonpalaceBoundaryProfileId profile,
            MoonpalaceBoundaryOrientation orientation,
            MoonpalaceBoundaryRouteRole routeRole,
            MoonpalaceBoundaryEdgeSignature edgeSignature)
        {
            if (!pair.IsDefined) throw new ArgumentException("Pair is undefined.", nameof(pair));
            if (!profile.IsDefined) throw new ArgumentException("Profile is undefined.", nameof(profile));
            if (orientation != MoonpalaceBoundaryOrientation.Horizontal &&
                orientation != MoonpalaceBoundaryOrientation.Vertical)
            {
                throw new ArgumentOutOfRangeException(nameof(orientation));
            }

            if (!routeRole.IsDefined) throw new ArgumentException("Route role is undefined.", nameof(routeRole));
            if (!edgeSignature.IsDefined) throw new ArgumentException("Edge signature is undefined.", nameof(edgeSignature));

            Pair = new MoonpalaceBiomePair(pair.First, pair.Second);
            Profile = profile;
            Orientation = orientation;
            RouteRole = routeRole;
            EdgeSignature = edgeSignature;
        }

        public MoonpalaceBiomePair Pair { get; }
        public MoonpalaceBoundaryProfileId Profile { get; }
        public MoonpalaceBoundaryOrientation Orientation { get; }
        public MoonpalaceBoundaryRouteRole RouteRole { get; }
        public MoonpalaceBoundaryEdgeSignature EdgeSignature { get; }

        public bool IsDefined =>
            Pair.IsDefined &&
            Profile.IsDefined &&
            (Orientation == MoonpalaceBoundaryOrientation.Horizontal ||
             Orientation == MoonpalaceBoundaryOrientation.Vertical) &&
            RouteRole.IsDefined &&
            EdgeSignature.IsDefined;

        public string Signature
        {
            get
            {
                EnsureDefined();
                return string.Join("|", new[]
                {
                    Pair.PairId,
                    Profile.CanonicalId,
                    Orientation == MoonpalaceBoundaryOrientation.Horizontal ? "Horizontal" : "Vertical",
                    RouteRole.CanonicalId,
                    EdgeSignature.SignatureId,
                });
            }
        }

        public int CompareTo(MoonpalaceBoundaryCandidateKey other)
        {
            EnsureDefined();
            other.EnsureDefined();

            var comparison = Pair.CompareTo(other.Pair);
            if (comparison != 0) return comparison;
            comparison = Profile.CompareTo(other.Profile);
            if (comparison != 0) return comparison;
            comparison = ((int)Orientation).CompareTo((int)other.Orientation);
            if (comparison != 0) return comparison;
            comparison = RouteRole.CompareTo(other.RouteRole);
            return comparison != 0 ? comparison : EdgeSignature.CompareTo(other.EdgeSignature);
        }

        public bool Equals(MoonpalaceBoundaryCandidateKey other)
        {
            return Pair == other.Pair &&
                   Profile == other.Profile &&
                   Orientation == other.Orientation &&
                   RouteRole == other.RouteRole &&
                   EdgeSignature == other.EdgeSignature;
        }

        public override bool Equals(object obj)
        {
            return obj is MoonpalaceBoundaryCandidateKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Pair.GetHashCode();
                hash = (hash * 397) ^ Profile.GetHashCode();
                hash = (hash * 397) ^ (int)Orientation;
                hash = (hash * 397) ^ RouteRole.GetHashCode();
                return (hash * 397) ^ EdgeSignature.GetHashCode();
            }
        }

        public override string ToString()
        {
            return Signature;
        }

        public static bool operator ==(MoonpalaceBoundaryCandidateKey left, MoonpalaceBoundaryCandidateKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MoonpalaceBoundaryCandidateKey left, MoonpalaceBoundaryCandidateKey right)
        {
            return !left.Equals(right);
        }

        private void EnsureDefined()
        {
            if (!IsDefined)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, "The boundary candidate key is undefined ({0}).", GetHashCode()));
            }
        }
    }
}
