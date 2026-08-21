using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class FootprintPlacementBlockers
    {
        private static readonly FootprintPlacementBlockers EmptyValue =
            new FootprintPlacementBlockers(Array.Empty<int>(), Array.Empty<int>());

        private readonly IReadOnlyList<int> occupiedSectorIndices;
        private readonly IReadOnlyList<int> protectedEntryApproachSectorIndices;
        private readonly HashSet<int> occupiedLookup;
        private readonly HashSet<int> protectedLookup;

        public FootprintPlacementBlockers(
            IEnumerable<int> occupiedSectorIndices,
            IEnumerable<int> protectedEntryApproachSectorIndices)
        {
            if (occupiedSectorIndices == null) throw new ArgumentNullException(nameof(occupiedSectorIndices));
            if (protectedEntryApproachSectorIndices == null)
                throw new ArgumentNullException(nameof(protectedEntryApproachSectorIndices));

            var occupied = Snapshot(occupiedSectorIndices, nameof(occupiedSectorIndices));
            var protectedApproaches = Snapshot(
                protectedEntryApproachSectorIndices,
                nameof(protectedEntryApproachSectorIndices));
            var occupiedSet = new HashSet<int>(occupied);
            foreach (var index in protectedApproaches)
            {
                if (occupiedSet.Contains(index))
                    throw new ArgumentException(
                        "A sector cannot be both occupied and a protected entry approach.",
                        nameof(protectedEntryApproachSectorIndices));
            }

            this.occupiedSectorIndices = new ReadOnlyCollection<int>(occupied);
            this.protectedEntryApproachSectorIndices =
                new ReadOnlyCollection<int>(protectedApproaches);
            occupiedLookup = occupiedSet;
            protectedLookup = new HashSet<int>(protectedApproaches);
        }

        public IReadOnlyList<int> OccupiedSectorIndices => occupiedSectorIndices;
        public IReadOnlyList<int> ProtectedEntryApproachSectorIndices =>
            protectedEntryApproachSectorIndices;
        public static FootprintPlacementBlockers Empty => EmptyValue;

        public static FootprintPlacementBlockers FromReservations(
            IEnumerable<SiteReservation> reservations)
        {
            if (reservations == null) throw new ArgumentNullException(nameof(reservations));

            var reservationIds = new HashSet<SiteReservationId>();
            var occupied = new HashSet<int>();
            var protectedApproaches = new HashSet<int>();
            foreach (var reservation in reservations)
            {
                if (reservation == null)
                    throw new ArgumentException("Reservations cannot contain null.", nameof(reservations));
                if (!reservationIds.Add(reservation.ReservationId))
                    throw new ArgumentException("Reservation IDs must be unique.", nameof(reservations));

                foreach (var sector in reservation.OccupiedSectors)
                {
                    var index = WorldGridIndex.ToIndex(sector);
                    if (!occupied.Add(index))
                        throw new ArgumentException("Reservation footprints cannot overlap.", nameof(reservations));
                }

                foreach (var entry in reservation.EntryAnchors)
                {
                    if (!entry.TryGetExteriorSector(out var exterior))
                        throw new ArgumentException("Reservation entry exterior must be inside the world.", nameof(reservations));
                    protectedApproaches.Add(WorldGridIndex.ToIndex(exterior));
                }
            }

            return new FootprintPlacementBlockers(occupied, protectedApproaches);
        }

        public bool IsOccupied(int sectorIndex)
        {
            ValidateIndex(sectorIndex, nameof(sectorIndex));
            return occupiedLookup.Contains(sectorIndex);
        }

        public bool IsProtectedEntryApproach(int sectorIndex)
        {
            ValidateIndex(sectorIndex, nameof(sectorIndex));
            return protectedLookup.Contains(sectorIndex);
        }

        private static List<int> Snapshot(IEnumerable<int> source, string parameterName)
        {
            var result = new List<int>();
            var seen = new HashSet<int>();
            foreach (var index in source)
            {
                ValidateIndex(index, parameterName);
                if (!seen.Add(index))
                    throw new ArgumentException("Sector indices must be unique.", parameterName);
                result.Add(index);
            }
            result.Sort();
            return result;
        }

        private static void ValidateIndex(int sectorIndex, string parameterName)
        {
            if (sectorIndex < 0 || sectorIndex >= Domain.WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
