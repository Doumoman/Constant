using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class CoreCapacitySiteDiagnostics
    {
        internal CoreCapacitySiteDiagnostics(
            SitePlacementKey key,
            int footprintSectorCount,
            int mandatoryBufferSectorCount,
            int outsideTheoreticalBufferCount,
            int blockedMandatoryBufferCount,
            int overlappingMandatoryBufferCount,
            int minimumCoreSectorCount,
            int requiredWitnessSectorCount,
            int floodVisitedSectorCount,
            int availableConnectedSectorCount,
            int witnessSectorCount)
        {
            if (!key.IsValid) throw new ArgumentException("A valid site key is required.", nameof(key));
            var values = new[]
            {
                footprintSectorCount, mandatoryBufferSectorCount, outsideTheoreticalBufferCount,
                blockedMandatoryBufferCount, overlappingMandatoryBufferCount,
                minimumCoreSectorCount, requiredWitnessSectorCount, floodVisitedSectorCount,
                availableConnectedSectorCount, witnessSectorCount
            };
            foreach (var value in values)
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(values));

            Key = key;
            FootprintSectorCount = footprintSectorCount;
            MandatoryBufferSectorCount = mandatoryBufferSectorCount;
            OutsideTheoreticalBufferCount = outsideTheoreticalBufferCount;
            BlockedMandatoryBufferCount = blockedMandatoryBufferCount;
            OverlappingMandatoryBufferCount = overlappingMandatoryBufferCount;
            MinimumCoreSectorCount = minimumCoreSectorCount;
            RequiredWitnessSectorCount = requiredWitnessSectorCount;
            FloodVisitedSectorCount = floodVisitedSectorCount;
            AvailableConnectedSectorCount = availableConnectedSectorCount;
            WitnessSectorCount = witnessSectorCount;
            var actualCapacity = witnessSectorCount > 0
                ? Math.Min(availableConnectedSectorCount, witnessSectorCount)
                : availableConnectedSectorCount;
            CapacityShortfall = Math.Max(0, requiredWitnessSectorCount - actualCapacity);
        }

        public SitePlacementKey Key { get; }
        public int FootprintSectorCount { get; }
        public int MandatoryBufferSectorCount { get; }
        public int OutsideTheoreticalBufferCount { get; }
        public int BlockedMandatoryBufferCount { get; }
        public int OverlappingMandatoryBufferCount { get; }
        public int MinimumCoreSectorCount { get; }
        public int RequiredWitnessSectorCount { get; }
        public int FloodVisitedSectorCount { get; }
        public int AvailableConnectedSectorCount { get; }
        public int WitnessSectorCount { get; }
        public int CapacityShortfall { get; }
    }

    public sealed class CoreCapacityFloodDiagnostics
    {
        private readonly IReadOnlyList<CoreCapacitySiteDiagnostics> sites;

        internal CoreCapacityFloodDiagnostics(
            IEnumerable<CoreCapacitySiteDiagnostics> sites,
            int selectedPlacementCount,
            int reservedFootprintSectorCount)
        {
            if (sites == null) throw new ArgumentNullException(nameof(sites));
            var snapshot = new List<CoreCapacitySiteDiagnostics>(sites);
            if (snapshot.Count != 4 || snapshot.Exists(item => item == null))
                throw new ArgumentException("Capacity diagnostics require exactly four sites.", nameof(sites));
            if (selectedPlacementCount < 0) throw new ArgumentOutOfRangeException(nameof(selectedPlacementCount));
            if (reservedFootprintSectorCount < 0) throw new ArgumentOutOfRangeException(nameof(reservedFootprintSectorCount));

            var flood = 0;
            var witness = 0;
            foreach (var site in snapshot)
            {
                checked
                {
                    flood += site.FloodVisitedSectorCount;
                    witness += site.WitnessSectorCount;
                }
            }
            this.sites = new ReadOnlyCollection<CoreCapacitySiteDiagnostics>(snapshot);
            SelectedPlacementCount = selectedPlacementCount;
            CapacitySiteCount = snapshot.Count;
            ReservedFootprintSectorCount = reservedFootprintSectorCount;
            TotalFloodVisitedSectorCount = flood;
            TotalWitnessSectorCount = witness;
        }

        public IReadOnlyList<CoreCapacitySiteDiagnostics> Sites => sites;
        public int SelectedPlacementCount { get; }
        public int CapacitySiteCount { get; }
        public int ReservedFootprintSectorCount { get; }
        public int TotalFloodVisitedSectorCount { get; }
        public int TotalWitnessSectorCount { get; }
    }
}
