using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteCandidateGroup
    {
        private readonly IReadOnlyList<SiteOriginCandidate> candidates;
        private readonly IReadOnlyDictionary<SectorCoord, SiteOriginCandidate> candidatesByOrigin;

        public SiteCandidateGroup(
            SiteReservationKind kind,
            string sourceDefinitionId,
            int requiredInstanceOrdinal,
            IEnumerable<SiteOriginCandidate> candidates)
        {
            PlacementPriority = GetPlacementPriority(kind);
            ReservationValidation.RequireCanonicalId(sourceDefinitionId, nameof(sourceDefinitionId), false);
            if (requiredInstanceOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(requiredInstanceOrdinal));
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));

            var snapshot = new List<SiteOriginCandidate>(candidates);
            if (snapshot.Count == 0)
                throw new ArgumentException("A candidate group requires at least one candidate.", nameof(candidates));
            snapshot.Sort((left, right) =>
            {
                if (ReferenceEquals(left, right)) return 0;
                if (left == null) return -1;
                if (right == null) return 1;
                return left.OriginIndex.CompareTo(right.OriginIndex);
            });

            var byOrigin = new Dictionary<SectorCoord, SiteOriginCandidate>();
            var originIndices = new HashSet<int>();
            for (var index = 0; index < snapshot.Count; index++)
            {
                var candidate = snapshot[index];
                if (candidate == null)
                    throw new ArgumentException("Candidate groups cannot contain null.", nameof(candidates));
                if (candidate.Kind != kind ||
                    !string.Equals(candidate.SourceDefinitionId, sourceDefinitionId, StringComparison.Ordinal) ||
                    candidate.RequiredInstanceOrdinal != requiredInstanceOrdinal)
                    throw new ArgumentException("Candidate identity must match its group.", nameof(candidates));
                if (!originIndices.Add(candidate.OriginIndex) || !byOrigin.TryAdd(candidate.Origin, candidate))
                    throw new ArgumentException("Candidate origins and indices must be unique.", nameof(candidates));
                if (candidate.CandidateOrdinal != index)
                    throw new ArgumentException("Candidate ordinal must match origin-index order.", nameof(candidates));
            }

            Kind = kind;
            SourceDefinitionId = sourceDefinitionId;
            RequiredInstanceOrdinal = requiredInstanceOrdinal;
            this.candidates = new ReadOnlyCollection<SiteOriginCandidate>(snapshot);
            candidatesByOrigin = new ReadOnlyDictionary<SectorCoord, SiteOriginCandidate>(byOrigin);
        }

        public SiteReservationKind Kind { get; }
        public string SourceDefinitionId { get; }
        public int RequiredInstanceOrdinal { get; }
        public int PlacementPriority { get; }
        public IReadOnlyList<SiteOriginCandidate> Candidates => candidates;
        public int Count => candidates.Count;

        public SiteOriginCandidate GetCandidate(int candidateOrdinal)
        {
            if (candidateOrdinal < 0 || candidateOrdinal >= candidates.Count)
                throw new ArgumentOutOfRangeException(nameof(candidateOrdinal));
            return candidates[candidateOrdinal];
        }

        public bool TryGetCandidateByOrigin(SectorCoord origin, out SiteOriginCandidate candidate)
        {
            return candidatesByOrigin.TryGetValue(origin, out candidate);
        }

        private static int GetPlacementPriority(SiteReservationKind kind)
        {
            switch (kind)
            {
                case SiteReservationKind.Start: return 0;
                case SiteReservationKind.Boss: return 10;
                case SiteReservationKind.Forge: return 20;
                case SiteReservationKind.CoreResource: return 30;
                case SiteReservationKind.Village:
                    throw new ArgumentException("Village candidates are not enumerated by this catalog.", nameof(kind));
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
