using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteReservation
    {
        private readonly IReadOnlyList<SiteEntryAnchor> entryAnchors;
        private readonly IReadOnlyList<SectorCoord> occupiedSectors;
        private readonly IReadOnlyDictionary<SectorCoord, SiteFootprintCell> cellsBySector;

        public SiteReservation(
            SiteReservationId reservationId,
            SiteReservationKind kind,
            string sourceDefinitionId,
            SectorCoord origin,
            SiteFootprint footprint,
            string primaryBiomeId,
            int reservationOrder,
            IEnumerable<SiteEntryAnchor> entryAnchors)
        {
            if (!reservationId.IsValid) throw new ArgumentException("Reservation ID must be valid.", nameof(reservationId));
            if (!IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            ReservationValidation.RequireCanonicalId(sourceDefinitionId, nameof(sourceDefinitionId), false);
            if (!IsValidSector(origin)) throw new ArgumentOutOfRangeException(nameof(origin));
            if (footprint == null) throw new ArgumentNullException(nameof(footprint));
            ReservationValidation.RequireCanonicalId(primaryBiomeId, nameof(primaryBiomeId), true);
            if (reservationOrder < 0) throw new ArgumentOutOfRangeException(nameof(reservationOrder));
            if (entryAnchors == null) throw new ArgumentNullException(nameof(entryAnchors));

            var bySector = new Dictionary<SectorCoord, SiteFootprintCell>();
            foreach (var cell in footprint.Cells)
            {
                var sector = new SectorCoord(origin.X + cell.LocalX, origin.Y + cell.LocalY);
                if (!IsValidSector(sector)) throw new ArgumentException("Footprint extends outside the world grid.", nameof(footprint));
                bySector.Add(sector, cell);
            }

            var anchors = new List<SiteEntryAnchor>(entryAnchors);
            var socketIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var anchor in anchors)
            {
                if (anchor == null) throw new ArgumentException("Entry anchors cannot contain null.", nameof(entryAnchors));
                if (anchor.ReservationId != reservationId) throw new ArgumentException("Entry anchor reservation ID must match.", nameof(entryAnchors));
                if (!bySector.ContainsKey(anchor.FootprintSector)) throw new ArgumentException("Entry anchor must belong to the footprint.", nameof(entryAnchors));
                if (!socketIds.Add(anchor.EntrySocketId)) throw new ArgumentException("Entry socket IDs must be unique.", nameof(entryAnchors));
            }
            anchors.Sort((left, right) => string.Compare(left.EntrySocketId, right.EntrySocketId, StringComparison.Ordinal));

            var occupied = new List<SectorCoord>(bySector.Keys);
            occupied.Sort((left, right) => WorldGridIndex.ToIndex(left).CompareTo(WorldGridIndex.ToIndex(right)));

            ReservationId = reservationId;
            Kind = kind;
            SourceDefinitionId = sourceDefinitionId;
            Origin = origin;
            Footprint = footprint;
            PrimaryBiomeId = primaryBiomeId;
            ReservationOrder = reservationOrder;
            this.entryAnchors = new ReadOnlyCollection<SiteEntryAnchor>(anchors);
            occupiedSectors = new ReadOnlyCollection<SectorCoord>(occupied);
            cellsBySector = new ReadOnlyDictionary<SectorCoord, SiteFootprintCell>(bySector);
        }

        public SiteReservationId ReservationId { get; }
        public SiteReservationKind Kind { get; }
        public string SourceDefinitionId { get; }
        public SectorCoord Origin { get; }
        public SiteFootprint Footprint { get; }
        public string PrimaryBiomeId { get; }
        public int ReservationOrder { get; }
        public IReadOnlyList<SiteEntryAnchor> EntryAnchors => entryAnchors;
        public IReadOnlyList<SectorCoord> OccupiedSectors => occupiedSectors;

        public bool TryGetFootprintCell(SectorCoord sector, out SiteFootprintCell cell)
        {
            return cellsBySector.TryGetValue(sector, out cell);
        }

        private static bool IsValidSector(SectorCoord coordinate)
        {
            return coordinate.X >= 0 && coordinate.X < WorldGenConstants.SectorColumns &&
                   coordinate.Y >= 0 && coordinate.Y < WorldGenConstants.SectorRows;
        }

        private static bool IsDefined(SiteReservationKind value)
        {
            return value == SiteReservationKind.Start || value == SiteReservationKind.CoreResource ||
                   value == SiteReservationKind.Forge || value == SiteReservationKind.Boss ||
                   value == SiteReservationKind.Village;
        }
    }
}
