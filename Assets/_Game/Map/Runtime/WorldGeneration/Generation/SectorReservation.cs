using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SectorReservation
    {
        private SectorReservation(
            int index,
            SectorCoord coordinate,
            bool isReserved,
            SiteReservationId? reservationId,
            SiteReservationKind? kind,
            int localX,
            int localY,
            string localRole)
        {
            ValidateGridIdentity(index, coordinate);
            Index = index;
            Coordinate = coordinate;
            IsReserved = isReserved;
            ReservationId = reservationId;
            Kind = kind;
            LocalX = localX;
            LocalY = localY;
            LocalRole = localRole;
        }

        public int Index { get; }
        public SectorCoord Coordinate { get; }
        public bool IsReserved { get; }
        public SiteReservationId? ReservationId { get; }
        public SiteReservationKind? Kind { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public string LocalRole { get; }

        public static SectorReservation CreateUnreserved(int index, SectorCoord coordinate)
        {
            return new SectorReservation(index, coordinate, false, null, null, -1, -1, string.Empty);
        }

        public static SectorReservation CreateReserved(
            int index,
            SectorCoord coordinate,
            SiteReservationId reservationId,
            SiteReservationKind kind,
            int localX,
            int localY,
            string localRole)
        {
            if (!reservationId.IsValid) throw new ArgumentException("Reservation ID must be valid.", nameof(reservationId));
            if (!IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (localX < 0) throw new ArgumentOutOfRangeException(nameof(localX));
            if (localY < 0) throw new ArgumentOutOfRangeException(nameof(localY));
            ReservationValidation.RequireCanonicalId(localRole, nameof(localRole), false);
            return new SectorReservation(index, coordinate, true, reservationId, kind, localX, localY, localRole);
        }

        private static void ValidateGridIdentity(int index, SectorCoord coordinate)
        {
            if (index < 0 || index >= WorldGenConstants.SectorCount) throw new ArgumentOutOfRangeException(nameof(index));
            if (coordinate != WorldGridIndex.ToCoordinate(index)) throw new ArgumentException("Index and coordinate must match the world grid.", nameof(coordinate));
        }

        private static bool IsDefined(SiteReservationKind value)
        {
            return value == SiteReservationKind.Start || value == SiteReservationKind.CoreResource ||
                   value == SiteReservationKind.Forge || value == SiteReservationKind.Boss ||
                   value == SiteReservationKind.Village;
        }
    }
}
