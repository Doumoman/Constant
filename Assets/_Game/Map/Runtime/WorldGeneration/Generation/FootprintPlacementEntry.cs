using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class FootprintPlacementEntry
    {
        private readonly IReadOnlyList<int> allowedRouteTypes;

        public FootprintPlacementEntry(
            string entrySocketId,
            int localX,
            int localY,
            SectorCoord footprintSector,
            SiteEntrySide side,
            SectorCoord exteriorSector,
            IEnumerable<int> allowedRouteTypes,
            bool required,
            bool returnPathRequired)
        {
            ReservationValidation.RequireCanonicalId(entrySocketId, nameof(entrySocketId), false);
            if (localX < 0) throw new ArgumentOutOfRangeException(nameof(localX));
            if (localY < 0) throw new ArgumentOutOfRangeException(nameof(localY));
            if (!IsWorldSector(footprintSector)) throw new ArgumentOutOfRangeException(nameof(footprintSector));
            if (!IsWorldSector(exteriorSector)) throw new ArgumentOutOfRangeException(nameof(exteriorSector));
            SiteReservationTokenCodec.GetDelta(side, out var deltaX, out var deltaY);
            if (exteriorSector != new SectorCoord(
                    footprintSector.X + deltaX,
                    footprintSector.Y + deltaY))
                throw new ArgumentException("Exterior sector must be exactly one side step from the footprint sector.", nameof(exteriorSector));
            if (allowedRouteTypes == null) throw new ArgumentNullException(nameof(allowedRouteTypes));

            var routes = new List<int>(allowedRouteTypes);
            if (routes.Count == 0)
                throw new ArgumentException("At least one route type is required.", nameof(allowedRouteTypes));
            var seen = new HashSet<int>();
            foreach (var route in routes)
            {
                if (route < 1 || route > 3) throw new ArgumentOutOfRangeException(nameof(allowedRouteTypes));
                if (!seen.Add(route))
                    throw new ArgumentException("Route types must be unique.", nameof(allowedRouteTypes));
            }
            routes.Sort();

            EntrySocketId = entrySocketId;
            LocalX = localX;
            LocalY = localY;
            FootprintSector = footprintSector;
            Side = side;
            ExteriorSector = exteriorSector;
            this.allowedRouteTypes = new ReadOnlyCollection<int>(routes);
            Required = required;
            ReturnPathRequired = returnPathRequired;
        }

        public string EntrySocketId { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public SectorCoord FootprintSector { get; }
        public SiteEntrySide Side { get; }
        public SectorCoord ExteriorSector { get; }
        public IReadOnlyList<int> AllowedRouteTypes => allowedRouteTypes;
        public bool Required { get; }
        public bool ReturnPathRequired { get; }

        private static bool IsWorldSector(SectorCoord coordinate)
        {
            return coordinate.X >= 0 && coordinate.X < WorldGenConstants.SectorColumns &&
                   coordinate.Y >= 0 && coordinate.Y < WorldGenConstants.SectorRows;
        }
    }
}
