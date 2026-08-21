using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteReservationPublication
    {
        private readonly IReadOnlyList<SiteReservationId> reservationIds;
        private readonly IReadOnlyDictionary<string, SiteReservation> reservationsBySourceId;

        internal SiteReservationPublication(
            VillageReservationApproval sourceApproval,
            SiteReservationSnapshot snapshot)
        {
            SourceApproval = sourceApproval ?? throw new ArgumentNullException(nameof(sourceApproval));
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.Reservations.Count != 7 || snapshot.Sectors.Count != 169 ||
                snapshot.EntryAnchors.Count != 6 || snapshot.CoreBiomeSeeds.Count != 4)
                throw new ArgumentException("A publication requires the completed 7/169/6/4 snapshot.", nameof(snapshot));

            var ids = new List<SiteReservationId>(snapshot.Reservations.Count);
            var lookup = new Dictionary<string, SiteReservation>(StringComparer.Ordinal);
            var reserved = 0;
            foreach (var reservation in snapshot.Reservations)
            {
                ids.Add(reservation.ReservationId);
                lookup.Add(reservation.SourceDefinitionId, reservation);
            }
            foreach (var sector in snapshot.Sectors)
                if (sector.IsReserved) reserved++;

            reservationIds = new ReadOnlyCollection<SiteReservationId>(ids);
            reservationsBySourceId = new ReadOnlyDictionary<string, SiteReservation>(lookup);
            ReservationCount = snapshot.Reservations.Count;
            ReservedSectorCount = reserved;
            EntryAnchorCount = snapshot.EntryAnchors.Count;
            CoreSeedCount = snapshot.CoreBiomeSeeds.Count;
        }

        public VillageReservationApproval SourceApproval { get; }
        public SiteReservationSnapshot Snapshot { get; }
        public IReadOnlyList<SiteReservationId> ReservationIds => reservationIds;
        public int ReservationCount { get; }
        public int ReservedSectorCount { get; }
        public int EntryAnchorCount { get; }
        public int CoreSeedCount { get; }

        public bool TryGetReservationBySourceId(
            string sourceDefinitionId,
            out SiteReservation reservation)
        {
            if (sourceDefinitionId == null) throw new ArgumentNullException(nameof(sourceDefinitionId));
            return reservationsBySourceId.TryGetValue(sourceDefinitionId, out reservation);
        }
    }
}
