using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum SiteReservationRejectionReason
    {
        FootprintOverlap,
        BlocksExistingEntryApproach,
        EntryApproachOccupied,
        DistanceConstraint,
        CoreCluster
    }

    public sealed class SitePlacementConflictDetector
    {
        public IReadOnlyList<SiteReservationRejectionReason> Evaluate(
            FootprintPlacement candidate,
            IEnumerable<FootprintPlacement> selectedPlacements)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            if (selectedPlacements == null) throw new ArgumentNullException(nameof(selectedPlacements));

            var selected = new List<FootprintPlacement>(selectedPlacements);
            if (selected.Exists(item => item == null))
                throw new ArgumentException("Selected placements cannot contain null.", nameof(selectedPlacements));

            var reasons = new bool[3];
            var candidateOccupied = SectorSet(candidate.OccupiedSectors);
            var candidateExterior = EntrySet(candidate.Entries);
            foreach (var existing in selected)
            {
                var existingOccupied = SectorSet(existing.OccupiedSectors);
                var existingExterior = EntrySet(existing.Entries);
                if (Intersects(candidateOccupied, existingOccupied)) reasons[0] = true;
                if (Intersects(candidateOccupied, existingExterior)) reasons[1] = true;
                if (Intersects(candidateExterior, existingOccupied)) reasons[2] = true;
            }

            var result = new List<SiteReservationRejectionReason>();
            if (reasons[0]) result.Add(SiteReservationRejectionReason.FootprintOverlap);
            if (reasons[1]) result.Add(SiteReservationRejectionReason.BlocksExistingEntryApproach);
            if (reasons[2]) result.Add(SiteReservationRejectionReason.EntryApproachOccupied);
            return new ReadOnlyCollection<SiteReservationRejectionReason>(result);
        }

        public IReadOnlyList<SiteReservationRejectionReason> Detect(
            FootprintPlacement candidate,
            IEnumerable<FootprintPlacement> selectedPlacements) =>
            Evaluate(candidate, selectedPlacements);

        private static HashSet<int> SectorSet(IEnumerable<SectorCoord> sectors)
        {
            var result = new HashSet<int>();
            foreach (var sector in sectors) result.Add(WorldGridIndex.ToIndex(sector));
            return result;
        }

        private static HashSet<int> EntrySet(IEnumerable<FootprintPlacementEntry> entries)
        {
            var result = new HashSet<int>();
            foreach (var entry in entries) result.Add(WorldGridIndex.ToIndex(entry.ExteriorSector));
            return result;
        }

        private static bool Intersects(HashSet<int> first, HashSet<int> second)
        {
            foreach (var value in first)
            {
                if (second.Contains(value)) return true;
            }
            return false;
        }
    }
}
