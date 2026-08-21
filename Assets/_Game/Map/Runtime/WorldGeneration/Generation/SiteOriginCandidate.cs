using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteOriginCandidate
    {
        public SiteOriginCandidate(
            SiteReservationKind kind,
            string sourceDefinitionId,
            int requiredInstanceOrdinal,
            SectorCoord origin,
            int originIndex,
            int edgeRing,
            int candidateOrdinal)
        {
            if (!IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            ReservationValidation.RequireCanonicalId(sourceDefinitionId, nameof(sourceDefinitionId), false);
            if (requiredInstanceOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(requiredInstanceOrdinal));
            if (originIndex < 0 || originIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(originIndex));
            if (WorldGridIndex.ToIndex(origin) != originIndex)
                throw new ArgumentException("Origin and origin index must match the world grid.", nameof(originIndex));

            var expectedEdgeRing = CalculateEdgeRing(origin);
            if (edgeRing != expectedEdgeRing)
                throw new ArgumentException("Edge ring must match the origin coordinate.", nameof(edgeRing));
            if (candidateOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(candidateOrdinal));

            Kind = kind;
            SourceDefinitionId = sourceDefinitionId;
            RequiredInstanceOrdinal = requiredInstanceOrdinal;
            Origin = origin;
            OriginIndex = originIndex;
            EdgeRing = edgeRing;
            CandidateOrdinal = candidateOrdinal;
        }

        public SiteReservationKind Kind { get; }
        public string SourceDefinitionId { get; }
        public int RequiredInstanceOrdinal { get; }
        public SectorCoord Origin { get; }
        public int OriginIndex { get; }
        public int EdgeRing { get; }
        public int CandidateOrdinal { get; }

        internal static int CalculateEdgeRing(SectorCoord origin)
        {
            var horizontal = Math.Min(origin.X, WorldGenConstants.SectorColumns - 1 - origin.X);
            var vertical = Math.Min(origin.Y, WorldGenConstants.SectorRows - 1 - origin.Y);
            return Math.Min(horizontal, vertical);
        }

        private static bool IsDefined(SiteReservationKind value)
        {
            return value == SiteReservationKind.Start || value == SiteReservationKind.CoreResource ||
                   value == SiteReservationKind.Forge || value == SiteReservationKind.Boss ||
                   value == SiteReservationKind.Village;
        }
    }
}
