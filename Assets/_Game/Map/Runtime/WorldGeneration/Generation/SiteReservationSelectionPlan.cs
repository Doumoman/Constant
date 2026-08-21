using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteReservationSelectionStep
    {
        internal SiteReservationSelectionStep(
            int depth,
            SitePlacementKey key,
            SiteReservationSearchOption option,
            SiteCandidateCostBreakdown incrementalCost,
            ulong randomTieBreak,
            int canonicalOptionOrdinal)
        {
            if (depth < 0 || depth >= 6) throw new ArgumentOutOfRangeException(nameof(depth));
            if (!key.IsValid) throw new ArgumentException("A valid placement key is required.", nameof(key));
            if (option == null) throw new ArgumentNullException(nameof(option));
            if (incrementalCost == null) throw new ArgumentNullException(nameof(incrementalCost));
            if (canonicalOptionOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(canonicalOptionOrdinal));
            if (SitePlacementKey.FromPlacement(option.Placement) != key ||
                incrementalCost.CandidateKey != key || !incrementalCost.HardConstraintsSatisfied ||
                incrementalCost.DistanceConstraintCountChecked != depth)
            {
                throw new ArgumentException("The selection step does not match its evaluated option.");
            }

            Depth = depth;
            Key = key;
            Option = option;
            IncrementalCost = incrementalCost;
            RandomTieBreak = randomTieBreak;
            CanonicalOptionOrdinal = canonicalOptionOrdinal;
        }

        public int Depth { get; }
        public SitePlacementKey Key { get; }
        public SiteReservationSearchOption Option { get; }
        public SiteCandidateCostBreakdown IncrementalCost { get; }
        public ulong RandomTieBreak { get; }
        public int CanonicalOptionOrdinal { get; }
    }

    public sealed class SiteReservationSelectionPlan
    {
        private readonly IReadOnlyList<SiteReservationSelectionStep> steps;
        private readonly IReadOnlyList<FootprintPlacement> selectedPlacements;

        internal SiteReservationSelectionPlan(IEnumerable<SiteReservationSelectionStep> steps)
        {
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            var snapshot = new List<SiteReservationSelectionStep>(steps);
            if (snapshot.Count != 6)
                throw new ArgumentException("A completed selection plan requires exactly six steps.", nameof(steps));

            var placements = new List<FootprintPlacement>(snapshot.Count);
            long total = 0;
            var distanceChecks = 0;
            for (var index = 0; index < snapshot.Count; index++)
            {
                var step = snapshot[index] ?? throw new ArgumentException(
                    "Selection steps cannot contain null.", nameof(steps));
                if (step.Depth != index || (index > 0 &&
                    snapshot[index - 1].Key.CompareTo(step.Key) >= 0))
                    throw new ArgumentException("Selection steps must use exact depth and key order.", nameof(steps));
                checked
                {
                    total += step.IncrementalCost.TotalCost;
                    distanceChecks += step.IncrementalCost.DistanceConstraintCountChecked;
                }
                placements.Add(step.Option.Placement);
            }
            if (distanceChecks != 15)
                throw new ArgumentException("A completed selection must check exactly fifteen distances.", nameof(steps));

            this.steps = new ReadOnlyCollection<SiteReservationSelectionStep>(snapshot);
            selectedPlacements = new ReadOnlyCollection<FootprintPlacement>(placements);
            TotalCost = total;
        }

        public IReadOnlyList<SiteReservationSelectionStep> Steps => steps;
        public IReadOnlyList<FootprintPlacement> SelectedPlacements => selectedPlacements;
        public int SelectedCount => selectedPlacements.Count;
        public long TotalCost { get; }
    }
}
