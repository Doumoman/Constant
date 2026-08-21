using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class IntrusionRulePlacementDiagnostics
    {
        internal IntrusionRulePlacementDiagnostics(
            string patchRuleId,
            string biomeId,
            int countRoll,
            int desiredIntrusionCount,
            int attemptedIntrusionCount,
            int acceptedIntrusionCount,
            int candidateMethodCallCount,
            int lastCandidateCount,
            bool exhausted,
            int failedIntrusionOrdinal)
        {
            ReservationValidation.RequireCanonicalId(patchRuleId, nameof(patchRuleId), false);
            ReservationValidation.RequireCanonicalId(biomeId, nameof(biomeId), false);
            if (countRoll < 0 || countRoll != desiredIntrusionCount)
                throw new ArgumentOutOfRangeException(nameof(countRoll));
            if (attemptedIntrusionCount < 0 || attemptedIntrusionCount > desiredIntrusionCount ||
                acceptedIntrusionCount < 0 || acceptedIntrusionCount > attemptedIntrusionCount ||
                candidateMethodCallCount < 0 || candidateMethodCallCount != acceptedIntrusionCount ||
                lastCandidateCount < 0)
                throw new ArgumentOutOfRangeException(nameof(attemptedIntrusionCount));
            if (exhausted)
            {
                if (failedIntrusionOrdinal < 0 || failedIntrusionOrdinal >= desiredIntrusionCount)
                    throw new ArgumentOutOfRangeException(nameof(failedIntrusionOrdinal));
            }
            else if (failedIntrusionOrdinal != -1)
                throw new ArgumentOutOfRangeException(nameof(failedIntrusionOrdinal));

            PatchRuleId = patchRuleId;
            BiomeId = biomeId;
            CountRoll = countRoll;
            DesiredIntrusionCount = desiredIntrusionCount;
            AttemptedIntrusionCount = attemptedIntrusionCount;
            AcceptedIntrusionCount = acceptedIntrusionCount;
            CandidateMethodCallCount = candidateMethodCallCount;
            LastCandidateCount = lastCandidateCount;
            Exhausted = exhausted;
            FailedIntrusionOrdinal = failedIntrusionOrdinal;
        }

        public string PatchRuleId { get; }
        public string BiomeId { get; }
        public int CountRoll { get; }
        public int DesiredIntrusionCount { get; }
        public int AttemptedIntrusionCount { get; }
        public int AcceptedIntrusionCount { get; }
        public int CandidateMethodCallCount { get; }
        public int LastCandidateCount { get; }
        public bool Exhausted { get; }
        public int FailedIntrusionOrdinal { get; }
    }

    public sealed class IntrusionPlacementDiagnostics
    {
        private readonly IReadOnlyList<IntrusionRulePlacementDiagnostics> rules;
        private readonly IReadOnlyList<IntrusionPlacementRecord> records;

        internal IntrusionPlacementDiagnostics(
            ulong worldSeed,
            IEnumerable<IntrusionRulePlacementDiagnostics> rules,
            IEnumerable<IntrusionPlacementRecord> records,
            int initialPatchCount,
            int initialAssignedSectorCount,
            int initialUnassignedSectorCount,
            int desiredIntrusionCount,
            int placedIntrusionCount,
            int finalPatchCount,
            int finalAssignedSectorCount,
            int finalUnassignedSectorCount,
            int countMethodCallCount,
            int candidateMethodCallCount,
            ulong rngDrawCountBefore,
            ulong rngDrawCountAfter,
            int donorMinimumViolationCount,
            int donorDisconnectCount,
            int protectedCellTransferCount,
            int disallowedPairCount,
            int reservationIntrusionCount,
            int patchOverlapCount,
            bool rollback)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (records == null) throw new ArgumentNullException(nameof(records));
            if (initialPatchCount < 0 || initialAssignedSectorCount < 0 || initialUnassignedSectorCount < 0 ||
                desiredIntrusionCount < 0 || placedIntrusionCount < 0 || finalPatchCount < 0 ||
                finalAssignedSectorCount < 0 || finalUnassignedSectorCount < 0 ||
                countMethodCallCount < 0 || candidateMethodCallCount < 0 ||
                donorMinimumViolationCount < 0 || donorDisconnectCount < 0 ||
                protectedCellTransferCount < 0 || disallowedPairCount < 0 ||
                reservationIntrusionCount < 0 || patchOverlapCount < 0 ||
                rngDrawCountAfter < rngDrawCountBefore)
                throw new ArgumentOutOfRangeException(nameof(initialPatchCount));

            var ruleValues = new List<IntrusionRulePlacementDiagnostics>(rules);
            var ruleIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rule in ruleValues)
                if (rule == null || !ruleIds.Add(rule.PatchRuleId))
                    throw new ArgumentException("Rule diagnostics must be non-null and unique.", nameof(rules));
            ruleValues.Sort((left, right) => string.Compare(left.PatchRuleId, right.PatchRuleId, StringComparison.Ordinal));

            var recordValues = new List<IntrusionPlacementRecord>(records);
            recordValues.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
            for (var index = 0; index < recordValues.Count; index++)
                if (recordValues[index] == null || recordValues[index].Sequence != index)
                    throw new ArgumentException("Placement records require exact sequence order.", nameof(records));

            if (countMethodCallCount != ruleValues.Count ||
                desiredIntrusionCount != ruleValues.Sum(value => value.DesiredIntrusionCount) ||
                candidateMethodCallCount != ruleValues.Sum(value => value.CandidateMethodCallCount))
                throw new ArgumentException("Rule and aggregate diagnostics are inconsistent.");
            if (initialAssignedSectorCount + initialUnassignedSectorCount != Domain.WorldGenConstants.SectorCount ||
                finalAssignedSectorCount + finalUnassignedSectorCount != Domain.WorldGenConstants.SectorCount)
                throw new ArgumentException("Sector counts must cover the world.");

            if (rollback)
            {
                if (recordValues.Count != 0 || placedIntrusionCount != 0 ||
                    finalPatchCount != initialPatchCount ||
                    finalAssignedSectorCount != initialAssignedSectorCount ||
                    finalUnassignedSectorCount != initialUnassignedSectorCount)
                    throw new ArgumentException("Retry diagnostics must describe atomic rollback.");
            }
            else if (placedIntrusionCount != recordValues.Count ||
                     finalPatchCount != initialPatchCount + placedIntrusionCount ||
                     finalAssignedSectorCount != initialAssignedSectorCount ||
                     finalUnassignedSectorCount != initialUnassignedSectorCount)
                throw new ArgumentException("Successful transfer conservation is invalid.");

            WorldSeed = worldSeed;
            InitialPatchCount = initialPatchCount;
            InitialAssignedSectorCount = initialAssignedSectorCount;
            InitialUnassignedSectorCount = initialUnassignedSectorCount;
            DesiredIntrusionCount = desiredIntrusionCount;
            PlacedIntrusionCount = placedIntrusionCount;
            FinalPatchCount = finalPatchCount;
            FinalAssignedSectorCount = finalAssignedSectorCount;
            FinalUnassignedSectorCount = finalUnassignedSectorCount;
            CountMethodCallCount = countMethodCallCount;
            CandidateMethodCallCount = candidateMethodCallCount;
            TotalRngMethodCallCount = checked(countMethodCallCount + candidateMethodCallCount);
            RngDrawCountBefore = rngDrawCountBefore;
            RngDrawCountAfter = rngDrawCountAfter;
            DonorMinimumViolationCount = donorMinimumViolationCount;
            DonorDisconnectCount = donorDisconnectCount;
            ProtectedCellTransferCount = protectedCellTransferCount;
            DisallowedPairCount = disallowedPairCount;
            ReservationIntrusionCount = reservationIntrusionCount;
            PatchOverlapCount = patchOverlapCount;
            this.rules = new ReadOnlyCollection<IntrusionRulePlacementDiagnostics>(ruleValues);
            this.records = new ReadOnlyCollection<IntrusionPlacementRecord>(recordValues);
        }

        public ulong WorldSeed { get; }
        public IReadOnlyList<IntrusionRulePlacementDiagnostics> Rules => rules;
        public IReadOnlyList<IntrusionPlacementRecord> Records => records;
        public int InitialPatchCount { get; }
        public int InitialAssignedSectorCount { get; }
        public int InitialUnassignedSectorCount { get; }
        public int DesiredIntrusionCount { get; }
        public int PlacedIntrusionCount { get; }
        public int FinalPatchCount { get; }
        public int FinalAssignedSectorCount { get; }
        public int FinalUnassignedSectorCount { get; }
        public int CountMethodCallCount { get; }
        public int CandidateMethodCallCount { get; }
        public int TotalRngMethodCallCount { get; }
        public ulong RngDrawCountBefore { get; }
        public ulong RngDrawCountAfter { get; }
        public int DonorMinimumViolationCount { get; }
        public int DonorDisconnectCount { get; }
        public int ProtectedCellTransferCount { get; }
        public int DisallowedPairCount { get; }
        public int ReservationIntrusionCount { get; }
        public int PatchOverlapCount { get; }
    }
}
