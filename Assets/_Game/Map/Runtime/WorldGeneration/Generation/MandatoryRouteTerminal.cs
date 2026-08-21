using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteTerminal
    {
        private readonly IReadOnlyList<int> allowedRouteTypes;

        public MandatoryRouteTerminal(
            MandatoryRouteTerminalId terminalId,
            MandatoryRouteTerminalKind kind,
            int terminalOrder,
            SiteReservationId reservationId,
            SiteReservationKind reservationKind,
            string sourceDefinitionId,
            string entrySocketId,
            SectorCoord anchorSector,
            SectorCoord approachSector,
            SiteEntrySide? entrySide,
            IEnumerable<int> allowedRouteTypes,
            bool required,
            bool returnPathRequired)
        {
            if (!terminalId.IsValid) throw new ArgumentException("Terminal ID must be valid.", nameof(terminalId));
            if (kind != MandatoryRouteTerminalKind.Start && kind != MandatoryRouteTerminalKind.SiteEntry)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (terminalOrder < 0) throw new ArgumentOutOfRangeException(nameof(terminalOrder));
            if (!reservationId.IsValid) throw new ArgumentException("Reservation ID must be valid.", nameof(reservationId));
            if (!IsReservationKindDefined(reservationKind)) throw new ArgumentOutOfRangeException(nameof(reservationKind));
            ReservationValidation.RequireCanonicalId(sourceDefinitionId, nameof(sourceDefinitionId), false);
            if (entrySocketId == null) throw new ArgumentNullException(nameof(entrySocketId));
            if (!IsWorldSector(anchorSector)) throw new ArgumentOutOfRangeException(nameof(anchorSector));
            if (!IsWorldSector(approachSector)) throw new ArgumentOutOfRangeException(nameof(approachSector));
            if (allowedRouteTypes == null) throw new ArgumentNullException(nameof(allowedRouteTypes));

            var routes = CopyRoutes(allowedRouteTypes);
            if (kind == MandatoryRouteTerminalKind.Start)
            {
                if (terminalOrder != 0 || reservationKind != SiteReservationKind.Start ||
                    entrySocketId.Length != 0 || entrySide.HasValue || anchorSector != approachSector)
                    throw new ArgumentException("Start terminal fields are inconsistent.");
            }
            else
            {
                ReservationValidation.RequireCanonicalId(entrySocketId, nameof(entrySocketId), false);
                if (terminalOrder == 0 || reservationKind == SiteReservationKind.Start || !entrySide.HasValue)
                    throw new ArgumentException("Site-entry terminal fields are inconsistent.");
                SiteReservationTokenCodec.GetDelta(entrySide.Value, out var deltaX, out var deltaY);
                if (approachSector.X != anchorSector.X + deltaX || approachSector.Y != anchorSector.Y + deltaY)
                    throw new ArgumentException("Approach sector must be the entry-side exterior sector.");
            }
            if (!required || !returnPathRequired)
                throw new ArgumentException("Mandatory terminals must be required and return-path-required.");

            TerminalId = terminalId;
            Kind = kind;
            TerminalOrder = terminalOrder;
            ReservationId = reservationId;
            ReservationKind = reservationKind;
            SourceDefinitionId = sourceDefinitionId;
            EntrySocketId = entrySocketId;
            AnchorSector = anchorSector;
            ApproachSector = approachSector;
            EntrySide = entrySide;
            this.allowedRouteTypes = new ReadOnlyCollection<int>(routes);
            Required = required;
            ReturnPathRequired = returnPathRequired;
        }

        public MandatoryRouteTerminalId TerminalId { get; }
        public MandatoryRouteTerminalKind Kind { get; }
        public int TerminalOrder { get; }
        public SiteReservationId ReservationId { get; }
        public SiteReservationKind ReservationKind { get; }
        public string SourceDefinitionId { get; }
        public string EntrySocketId { get; }
        public SectorCoord AnchorSector { get; }
        public SectorCoord ApproachSector { get; }
        public SiteEntrySide? EntrySide { get; }
        public IReadOnlyList<int> AllowedRouteTypes => allowedRouteTypes;
        public bool Required { get; }
        public bool ReturnPathRequired { get; }

        private static List<int> CopyRoutes(IEnumerable<int> source)
        {
            var routes = new List<int>(source);
            if (routes.Count == 0) throw new ArgumentException("At least one route type is required.", nameof(source));
            var unique = new HashSet<int>();
            foreach (var route in routes)
            {
                if (route < 1 || route > 3) throw new ArgumentOutOfRangeException(nameof(source));
                if (!unique.Add(route)) throw new ArgumentException("Route types must be unique.", nameof(source));
            }
            routes.Sort();
            return routes;
        }

        private static bool IsWorldSector(SectorCoord value) =>
            value.X >= 0 && value.X < WorldGenConstants.SectorColumns &&
            value.Y >= 0 && value.Y < WorldGenConstants.SectorRows;

        private static bool IsReservationKindDefined(SiteReservationKind value) =>
            value == SiteReservationKind.Start || value == SiteReservationKind.CoreResource ||
            value == SiteReservationKind.Forge || value == SiteReservationKind.Boss ||
            value == SiteReservationKind.Village;
    }
}
