using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteReservationSnapshot
    {
        private readonly IReadOnlyList<SiteReservation> reservations;
        private readonly IReadOnlyList<SectorReservation> sectors;
        private readonly IReadOnlyList<SiteEntryAnchor> entryAnchors;
        private readonly IReadOnlyList<CoreBiomeSeed> coreBiomeSeeds;
        private readonly IReadOnlyDictionary<SiteReservationId, SiteReservation> reservationsById;

        public SiteReservationSnapshot(
            ulong seed,
            IEnumerable<SiteReservation> reservations,
            IEnumerable<SectorReservation> sectors,
            IEnumerable<CoreBiomeSeed> coreBiomeSeeds)
        {
            if (reservations == null) throw new ArgumentNullException(nameof(reservations));
            if (sectors == null) throw new ArgumentNullException(nameof(sectors));
            if (coreBiomeSeeds == null) throw new ArgumentNullException(nameof(coreBiomeSeeds));

            var reservationList = new List<SiteReservation>(reservations);
            if (reservationList.Count == 0) throw new ArgumentException("At least one reservation is required.", nameof(reservations));
            var byId = new Dictionary<SiteReservationId, SiteReservation>();
            var orders = new HashSet<int>();
            SiteReservation start = null;
            foreach (var reservation in reservationList)
            {
                if (reservation == null) throw new ArgumentException("Reservations cannot contain null.", nameof(reservations));
                if (!byId.TryAdd(reservation.ReservationId, reservation)) throw new ArgumentException("Reservation IDs must be unique.", nameof(reservations));
                if (!orders.Add(reservation.ReservationOrder)) throw new ArgumentException("Reservation orders must be unique.", nameof(reservations));
                if (reservation.Kind == SiteReservationKind.Start)
                {
                    if (start != null) throw new ArgumentException("Exactly one Start reservation is required.", nameof(reservations));
                    start = reservation;
                }
            }
            if (start == null) throw new ArgumentException("Exactly one Start reservation is required.", nameof(reservations));
            reservationList.Sort((left, right) =>
            {
                var order = left.ReservationOrder.CompareTo(right.ReservationOrder);
                return order != 0 ? order : left.ReservationId.CompareTo(right.ReservationId);
            });

            var occupied = new Dictionary<SectorCoord, OccupiedBinding>();
            foreach (var reservation in reservationList)
            {
                foreach (var sector in reservation.OccupiedSectors)
                {
                    if (!reservation.TryGetFootprintCell(sector, out var cell)) throw new InvalidOperationException("Reservation footprint lookup is inconsistent.");
                    if (!occupied.TryAdd(sector, new OccupiedBinding(reservation, cell)))
                        throw new ArgumentException("Site footprints cannot overlap.", nameof(reservations));
                }
            }

            var sectorList = new List<SectorReservation>(sectors);
            if (sectorList.Count != WorldGenConstants.SectorCount)
                throw new ArgumentException("Exactly 169 sector entries are required.", nameof(sectors));
            var byIndex = new Dictionary<int, SectorReservation>();
            foreach (var sector in sectorList)
            {
                if (sector == null) throw new ArgumentException("Sector entries cannot contain null.", nameof(sectors));
                if (!byIndex.TryAdd(sector.Index, sector)) throw new ArgumentException("Sector indices must be unique.", nameof(sectors));
                if (sector.Coordinate != WorldGridIndex.ToCoordinate(sector.Index)) throw new ArgumentException("Sector index and coordinate must match.", nameof(sectors));
                if (occupied.TryGetValue(sector.Coordinate, out var binding))
                {
                    if (!sector.IsReserved || !sector.ReservationId.HasValue || !sector.Kind.HasValue ||
                        sector.ReservationId.Value != binding.Reservation.ReservationId ||
                        sector.Kind.Value != binding.Reservation.Kind ||
                        sector.LocalX != binding.Cell.LocalX || sector.LocalY != binding.Cell.LocalY ||
                        !string.Equals(sector.LocalRole, binding.Cell.LocalRole, StringComparison.Ordinal))
                        throw new ArgumentException("Reserved sector entry does not match its footprint.", nameof(sectors));
                }
                else if (sector.IsReserved)
                {
                    throw new ArgumentException("Reserved sector entry has no owning footprint.", nameof(sectors));
                }
            }
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                if (!byIndex.ContainsKey(index)) throw new ArgumentException("Sector index set must be exact 0..168.", nameof(sectors));
            }
            sectorList.Sort((left, right) => left.Index.CompareTo(right.Index));

            var flattenedAnchors = new List<SiteEntryAnchor>();
            foreach (var reservation in reservationList) flattenedAnchors.AddRange(reservation.EntryAnchors);
            flattenedAnchors.Sort((left, right) =>
            {
                var id = left.ReservationId.CompareTo(right.ReservationId);
                return id != 0 ? id : string.Compare(left.EntrySocketId, right.EntrySocketId, StringComparison.Ordinal);
            });

            var seedList = new List<CoreBiomeSeed>(coreBiomeSeeds);
            var seededReservations = new HashSet<SiteReservationId>();
            foreach (var coreSeed in seedList)
            {
                if (coreSeed == null) throw new ArgumentException("Core biome seeds cannot contain null.", nameof(coreBiomeSeeds));
                if (!byId.TryGetValue(coreSeed.SourceReservationId, out var source))
                    throw new ArgumentException("Core biome seed source must exist.", nameof(coreBiomeSeeds));
                if (source.Kind != SiteReservationKind.CoreResource && source.Kind != SiteReservationKind.Forge)
                    throw new ArgumentException("Core biome seed source kind must be CoreResource or Forge.", nameof(coreBiomeSeeds));
                if (!seededReservations.Add(coreSeed.SourceReservationId))
                    throw new ArgumentException("A reservation can own at most one core biome seed.", nameof(coreBiomeSeeds));
            }
            seedList.Sort((left, right) => left.SourceReservationId.CompareTo(right.SourceReservationId));

            Seed = seed;
            StartReservation = start;
            StartAnchor = start.Origin;
            this.reservations = new ReadOnlyCollection<SiteReservation>(reservationList);
            this.sectors = new ReadOnlyCollection<SectorReservation>(sectorList);
            entryAnchors = new ReadOnlyCollection<SiteEntryAnchor>(flattenedAnchors);
            this.coreBiomeSeeds = new ReadOnlyCollection<CoreBiomeSeed>(seedList);
            reservationsById = new ReadOnlyDictionary<SiteReservationId, SiteReservation>(byId);
        }

        public ulong Seed { get; }
        public SiteReservation StartReservation { get; }
        public SectorCoord StartAnchor { get; }
        public IReadOnlyList<SiteReservation> Reservations => reservations;
        public IReadOnlyList<SectorReservation> Sectors => sectors;
        public IReadOnlyList<SiteEntryAnchor> EntryAnchors => entryAnchors;
        public IReadOnlyList<CoreBiomeSeed> CoreBiomeSeeds => coreBiomeSeeds;

        public SectorReservation GetSector(int index)
        {
            if (index < 0 || index >= sectors.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return sectors[index];
        }

        public SectorReservation GetSector(SectorCoord coordinate)
        {
            return GetSector(WorldGridIndex.ToIndex(coordinate));
        }

        public bool TryGetReservation(SiteReservationId id, out SiteReservation reservation)
        {
            return reservationsById.TryGetValue(id, out reservation);
        }

        private sealed class OccupiedBinding
        {
            public OccupiedBinding(SiteReservation reservation, SiteFootprintCell cell)
            {
                Reservation = reservation;
                Cell = cell;
            }

            public SiteReservation Reservation { get; }
            public SiteFootprintCell Cell { get; }
        }
    }
}
