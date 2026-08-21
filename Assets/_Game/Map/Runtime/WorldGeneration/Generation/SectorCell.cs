using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SectorCell
    {
        public int Index { get; }
        public SectorCoord Coordinate { get; }
        public GeneratedSectorRole Role { get; }
        public string PrimaryBiomeId { get; }
        public string SecondaryBiomeId { get; }
        public string PatchId { get; }
        public string RouteMaskId { get; }
        public string SpecialSiteInstanceId { get; }
        public string BoundaryProfileId { get; }
        public string SectorRecipeId { get; }
        public string ReservationId { get; }
        public int ShortestDistanceFromStart { get; }
        public bool MandatoryGraphNode { get; }

        public SectorCell(
            int index,
            SectorCoord coordinate,
            GeneratedSectorRole role,
            string primaryBiomeId,
            string secondaryBiomeId,
            string patchId,
            string routeMaskId,
            string specialSiteInstanceId,
            string boundaryProfileId,
            string sectorRecipeId,
            string reservationId,
            int shortestDistanceFromStart,
            bool mandatoryGraphNode)
        {
            if (index < 0 || index >= WorldGenConstants.SectorCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (!WorldCoordinateUtility.IsValid(coordinate))
            {
                throw new ArgumentOutOfRangeException(nameof(coordinate));
            }

            if (!Enum.IsDefined(typeof(GeneratedSectorRole), role))
            {
                throw new ArgumentOutOfRangeException(nameof(role));
            }

            Index = index;
            Coordinate = coordinate;
            Role = role;
            PrimaryBiomeId = primaryBiomeId ?? throw new ArgumentNullException(nameof(primaryBiomeId));
            SecondaryBiomeId = secondaryBiomeId ?? throw new ArgumentNullException(nameof(secondaryBiomeId));
            PatchId = patchId ?? throw new ArgumentNullException(nameof(patchId));
            RouteMaskId = routeMaskId ?? throw new ArgumentNullException(nameof(routeMaskId));
            SpecialSiteInstanceId = specialSiteInstanceId ?? throw new ArgumentNullException(nameof(specialSiteInstanceId));
            BoundaryProfileId = boundaryProfileId ?? throw new ArgumentNullException(nameof(boundaryProfileId));
            SectorRecipeId = sectorRecipeId ?? throw new ArgumentNullException(nameof(sectorRecipeId));
            ReservationId = reservationId ?? throw new ArgumentNullException(nameof(reservationId));
            ShortestDistanceFromStart = shortestDistanceFromStart;
            MandatoryGraphNode = mandatoryGraphNode;
        }

        public static SectorCell CreateUnassigned(int index, SectorCoord coordinate)
        {
            return new SectorCell(
                index,
                coordinate,
                GeneratedSectorRole.Unassigned,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                -1,
                false);
        }
    }
}
