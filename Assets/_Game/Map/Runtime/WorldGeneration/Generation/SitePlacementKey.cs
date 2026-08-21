using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct SitePlacementKey :
        IEquatable<SitePlacementKey>, IComparable<SitePlacementKey>
    {
        private readonly string sourceDefinitionId;

        public SitePlacementKey(
            SiteReservationKind kind,
            string sourceDefinitionId,
            int requiredInstanceOrdinal)
        {
            if (!IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (!IsCanonicalId(sourceDefinitionId))
                throw new ArgumentException("A canonical source definition ID is required.", nameof(sourceDefinitionId));
            if (requiredInstanceOrdinal < 0)
                throw new ArgumentOutOfRangeException(nameof(requiredInstanceOrdinal));

            Kind = kind;
            this.sourceDefinitionId = sourceDefinitionId;
            RequiredInstanceOrdinal = requiredInstanceOrdinal;
        }

        public SiteReservationKind Kind { get; }
        public string SourceDefinitionId => sourceDefinitionId ?? string.Empty;
        public int RequiredInstanceOrdinal { get; }
        public int PlacementPriority => Priority(Kind);
        public bool IsValid =>
            IsDefined(Kind) && IsCanonicalId(sourceDefinitionId) && RequiredInstanceOrdinal >= 0;

        public static SitePlacementKey FromPlacement(FootprintPlacement placement)
        {
            if (placement == null) throw new ArgumentNullException(nameof(placement));
            if (placement.Candidate == null)
                throw new ArgumentException("The placement requires a candidate.", nameof(placement));
            return new SitePlacementKey(
                placement.Candidate.Kind,
                placement.Candidate.SourceDefinitionId,
                placement.Candidate.RequiredInstanceOrdinal);
        }

        public int CompareTo(SitePlacementKey other)
        {
            var priority = PlacementPriority.CompareTo(other.PlacementPriority);
            if (priority != 0) return priority;
            var source = string.Compare(SourceDefinitionId, other.SourceDefinitionId, StringComparison.Ordinal);
            return source != 0
                ? source
                : RequiredInstanceOrdinal.CompareTo(other.RequiredInstanceOrdinal);
        }

        public bool Equals(SitePlacementKey other)
        {
            return Kind == other.Kind &&
                   RequiredInstanceOrdinal == other.RequiredInstanceOrdinal &&
                   string.Equals(SourceDefinitionId, other.SourceDefinitionId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SitePlacementKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                uint hash = 2166136261;
                hash = (hash ^ (uint)Kind) * 16777619;
                foreach (var character in SourceDefinitionId)
                {
                    hash = (hash ^ character) * 16777619;
                }
                hash = (hash ^ (uint)RequiredInstanceOrdinal) * 16777619;
                return (int)hash;
            }
        }

        public static bool operator ==(SitePlacementKey left, SitePlacementKey right) => left.Equals(right);
        public static bool operator !=(SitePlacementKey left, SitePlacementKey right) => !left.Equals(right);
        public static bool operator <(SitePlacementKey left, SitePlacementKey right) => left.CompareTo(right) < 0;
        public static bool operator >(SitePlacementKey left, SitePlacementKey right) => left.CompareTo(right) > 0;

        internal static bool IsCanonicalId(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            foreach (var character in value)
            {
                if ((character < 'A' || character > 'Z') &&
                    (character < '0' || character > '9') && character != '_')
                {
                    return false;
                }
            }
            return true;
        }

        private static int Priority(SiteReservationKind kind)
        {
            switch (kind)
            {
                case SiteReservationKind.Start: return 0;
                case SiteReservationKind.Boss: return 10;
                case SiteReservationKind.Forge: return 20;
                case SiteReservationKind.CoreResource: return 30;
                case SiteReservationKind.Village: return 40;
                default: return int.MaxValue;
            }
        }

        private static bool IsDefined(SiteReservationKind kind)
        {
            return kind == SiteReservationKind.Start || kind == SiteReservationKind.Boss ||
                   kind == SiteReservationKind.Forge || kind == SiteReservationKind.CoreResource ||
                   kind == SiteReservationKind.Village;
        }
    }
}
