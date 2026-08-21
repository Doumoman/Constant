using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteEntryAnchor
    {
        private readonly IReadOnlyList<int> allowedRouteTypes;

        public SiteEntryAnchor(
            SiteReservationId reservationId,
            string entrySocketId,
            SectorCoord footprintSector,
            SiteEntrySide side,
            IEnumerable<int> allowedRouteTypes,
            bool required,
            bool returnPathRequired)
        {
            if (!reservationId.IsValid) throw new ArgumentException("Reservation ID must be valid.", nameof(reservationId));
            ReservationValidation.RequireCanonicalId(entrySocketId, nameof(entrySocketId), false);
            if (!IsValidSector(footprintSector)) throw new ArgumentOutOfRangeException(nameof(footprintSector));
            SiteReservationTokenCodec.GetDelta(side, out _, out _);
            if (allowedRouteTypes == null) throw new ArgumentNullException(nameof(allowedRouteTypes));

            var routes = new List<int>(allowedRouteTypes);
            if (routes.Count == 0) throw new ArgumentException("At least one route type is required.", nameof(allowedRouteTypes));
            var seen = new HashSet<int>();
            foreach (var route in routes)
            {
                if (route < 1 || route > 3) throw new ArgumentOutOfRangeException(nameof(allowedRouteTypes));
                if (!seen.Add(route)) throw new ArgumentException("Route types must be unique.", nameof(allowedRouteTypes));
            }
            routes.Sort();

            ReservationId = reservationId;
            EntrySocketId = entrySocketId;
            FootprintSector = footprintSector;
            Side = side;
            this.allowedRouteTypes = new ReadOnlyCollection<int>(routes);
            Required = required;
            ReturnPathRequired = returnPathRequired;
        }

        public SiteReservationId ReservationId { get; }
        public string EntrySocketId { get; }
        public SectorCoord FootprintSector { get; }
        public SiteEntrySide Side { get; }
        public IReadOnlyList<int> AllowedRouteTypes => allowedRouteTypes;
        public bool Required { get; }
        public bool ReturnPathRequired { get; }

        public bool TryGetExteriorSector(out SectorCoord exteriorSector)
        {
            SiteReservationTokenCodec.GetDelta(Side, out var deltaX, out var deltaY);
            var candidate = new SectorCoord(FootprintSector.X + deltaX, FootprintSector.Y + deltaY);
            if (!IsValidSector(candidate))
            {
                exteriorSector = default(SectorCoord);
                return false;
            }

            exteriorSector = candidate;
            return true;
        }

        private static bool IsValidSector(SectorCoord coordinate)
        {
            return coordinate.X >= 0 && coordinate.X < WorldGenConstants.SectorColumns &&
                   coordinate.Y >= 0 && coordinate.Y < WorldGenConstants.SectorRows;
        }
    }
}
