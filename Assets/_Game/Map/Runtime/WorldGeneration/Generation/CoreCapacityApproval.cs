using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class CoreCapacityApproval
    {
        private readonly IReadOnlyList<CoreCapacityFloodWitness> witnesses;

        internal CoreCapacityApproval(
            SiteReservationSelectionPlan selectionPlan,
            IEnumerable<CoreCapacityFloodWitness> witnesses)
        {
            SelectionPlan = selectionPlan ?? throw new ArgumentNullException(nameof(selectionPlan));
            if (witnesses == null) throw new ArgumentNullException(nameof(witnesses));
            var snapshot = new List<CoreCapacityFloodWitness>(witnesses);
            if (snapshot.Count != 4 || snapshot.Exists(item => item == null))
                throw new ArgumentException("A capacity approval requires exactly four witnesses.", nameof(witnesses));

            var keys = new HashSet<SitePlacementKey>();
            var total = 0;
            foreach (var witness in snapshot)
            {
                if (!keys.Add(witness.Key))
                    throw new ArgumentException("Capacity witness keys must be unique.", nameof(witnesses));
                checked { total += witness.WitnessSectorIndices.Count; }
            }
            this.witnesses = new ReadOnlyCollection<CoreCapacityFloodWitness>(snapshot);
            TotalWitnessSectorCount = total;
        }

        public SiteReservationSelectionPlan SelectionPlan { get; }
        public IReadOnlyList<CoreCapacityFloodWitness> Witnesses => witnesses;
        public int CapacitySiteCount => witnesses.Count;
        public int TotalWitnessSectorCount { get; }

        public bool TryGetWitness(
            SitePlacementKey key,
            out CoreCapacityFloodWitness witness)
        {
            foreach (var candidate in witnesses)
            {
                if (candidate.Key == key)
                {
                    witness = candidate;
                    return true;
                }
            }
            witness = null;
            return false;
        }
    }
}
