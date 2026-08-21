using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteReservationValidationDiagnostics
    {
        private readonly IReadOnlyList<SiteReservationRuleResult> rules;

        public SiteReservationValidationDiagnostics(
            IEnumerable<SiteReservationRuleResult> rules,
            int reservationCount,
            int reservedSectorCount,
            int unreservedSectorCount,
            int entryAnchorCount,
            int requiredEntryCount,
            int nonVillageDistanceConstraintCount,
            int villageDistanceCheckCount,
            int coreClusterCheckCount,
            int coreWitnessCount,
            int coreWitnessSectorCount,
            int coreSeedCount,
            int violationCount)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            var snapshot = new List<SiteReservationRuleResult>(rules);
            if (snapshot.Count != 6) throw new ArgumentException("Exactly six rule results are required.", nameof(rules));
            var violationTotal = 0;
            for (var index = 0; index < snapshot.Count; index++)
            {
                if (snapshot[index] == null) throw new ArgumentException("Rule results cannot contain null.", nameof(rules));
                if ((int)snapshot[index].Rule != index) throw new ArgumentException("Rule results must use frozen enum order.", nameof(rules));
                checked { violationTotal += snapshot[index].ViolationCount; }
            }

            var counts = new[]
            {
                reservationCount, reservedSectorCount, unreservedSectorCount,
                entryAnchorCount, requiredEntryCount, nonVillageDistanceConstraintCount,
                villageDistanceCheckCount, coreClusterCheckCount, coreWitnessCount,
                coreWitnessSectorCount, coreSeedCount, violationCount
            };
            foreach (var count in counts)
                if (count < 0) throw new ArgumentOutOfRangeException(nameof(counts));
            if (reservedSectorCount + unreservedSectorCount != WorldGenConstants.SectorCount)
                throw new ArgumentException("Reserved and unreserved counts must cover the world grid.");
            if (violationCount != violationTotal)
                throw new ArgumentException("Rule and diagnostic violation totals must match.", nameof(violationCount));

            this.rules = new ReadOnlyCollection<SiteReservationRuleResult>(snapshot);
            ReservationCount = reservationCount;
            ReservedSectorCount = reservedSectorCount;
            UnreservedSectorCount = unreservedSectorCount;
            EntryAnchorCount = entryAnchorCount;
            RequiredEntryCount = requiredEntryCount;
            NonVillageDistanceConstraintCount = nonVillageDistanceConstraintCount;
            VillageDistanceCheckCount = villageDistanceCheckCount;
            CoreClusterCheckCount = coreClusterCheckCount;
            CoreWitnessCount = coreWitnessCount;
            CoreWitnessSectorCount = coreWitnessSectorCount;
            CoreSeedCount = coreSeedCount;
            ViolationCount = violationCount;
        }

        public IReadOnlyList<SiteReservationRuleResult> Rules => rules;
        public int ReservationCount { get; }
        public int ReservedSectorCount { get; }
        public int UnreservedSectorCount { get; }
        public int EntryAnchorCount { get; }
        public int RequiredEntryCount { get; }
        public int NonVillageDistanceConstraintCount { get; }
        public int VillageDistanceCheckCount { get; }
        public int CoreClusterCheckCount { get; }
        public int CoreWitnessCount { get; }
        public int CoreWitnessSectorCount { get; }
        public int CoreSeedCount { get; }
        public int ViolationCount { get; }
    }
}
