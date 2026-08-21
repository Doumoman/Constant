using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SatelliteRulePlacementDiagnostics
    {
        internal SatelliteRulePlacementDiagnostics(
            string patchRuleId,
            string biomeId,
            int countRoll,
            int desiredSeedCount,
            int acceptedSeedCount,
            int candidateMethodCallCount,
            int candidateAttemptCount,
            int edgeRejectionCount,
            int distanceRejectionCount,
            bool exhausted,
            int failedSatelliteOrdinal)
        {
            ReservationValidation.RequireCanonicalId(patchRuleId, nameof(patchRuleId), false);
            ReservationValidation.RequireCanonicalId(biomeId, nameof(biomeId), false);
            if (countRoll < 0 || countRoll != desiredSeedCount)
                throw new ArgumentOutOfRangeException(nameof(countRoll));
            if (acceptedSeedCount < 0 || acceptedSeedCount > desiredSeedCount)
                throw new ArgumentOutOfRangeException(nameof(acceptedSeedCount));
            if (candidateMethodCallCount < 0 || candidateAttemptCount < 0 ||
                candidateMethodCallCount != candidateAttemptCount)
                throw new ArgumentOutOfRangeException(nameof(candidateMethodCallCount));
            if (edgeRejectionCount < 0 || distanceRejectionCount < 0 ||
                edgeRejectionCount + distanceRejectionCount + acceptedSeedCount > candidateAttemptCount)
                throw new ArgumentOutOfRangeException(nameof(edgeRejectionCount));
            if (exhausted)
            {
                if (failedSatelliteOrdinal < 0 || failedSatelliteOrdinal >= desiredSeedCount)
                    throw new ArgumentOutOfRangeException(nameof(failedSatelliteOrdinal));
            }
            else if (failedSatelliteOrdinal != -1)
            {
                throw new ArgumentOutOfRangeException(nameof(failedSatelliteOrdinal));
            }

            PatchRuleId = patchRuleId;
            BiomeId = biomeId;
            CountRoll = countRoll;
            DesiredSeedCount = desiredSeedCount;
            AcceptedSeedCount = acceptedSeedCount;
            CandidateMethodCallCount = candidateMethodCallCount;
            CandidateAttemptCount = candidateAttemptCount;
            EdgeRejectionCount = edgeRejectionCount;
            DistanceRejectionCount = distanceRejectionCount;
            Exhausted = exhausted;
            FailedSatelliteOrdinal = failedSatelliteOrdinal;
        }

        public string PatchRuleId { get; }
        public string BiomeId { get; }
        public int CountRoll { get; }
        public int DesiredSeedCount { get; }
        public int AcceptedSeedCount { get; }
        public int CandidateMethodCallCount { get; }
        public int CandidateAttemptCount { get; }
        public int EdgeRejectionCount { get; }
        public int DistanceRejectionCount { get; }
        public bool Exhausted { get; }
        public int FailedSatelliteOrdinal { get; }
    }

    public sealed class SatelliteSeedPlacementDiagnostics
    {
        private readonly IReadOnlyList<SatelliteRulePlacementDiagnostics> rules;
        private readonly IReadOnlyList<SatelliteSeedPlacementRecord> records;

        internal SatelliteSeedPlacementDiagnostics(
            ulong worldSeed,
            IEnumerable<SatelliteRulePlacementDiagnostics> rules,
            IEnumerable<SatelliteSeedPlacementRecord> records,
            int rawCandidateSectorCount,
            int countMethodCallCount,
            int candidateMethodCallCount,
            ulong rngDrawCountBefore,
            ulong rngDrawCountAfter,
            int desiredSatelliteSeedCount,
            int placedSatelliteSeedCount,
            int initialPatchCount,
            int initialAssignedSectorCount,
            int finalPatchCount,
            int finalAssignedSectorCount,
            int finalUnassignedSectorCount,
            int reservationIntrusionCount,
            int patchOverlapCount,
            bool rollback)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (records == null) throw new ArgumentNullException(nameof(records));
            if (rawCandidateSectorCount < 0 || countMethodCallCount < 0 ||
                candidateMethodCallCount < 0 || desiredSatelliteSeedCount < 0 ||
                placedSatelliteSeedCount < 0 || initialPatchCount < 0 ||
                initialAssignedSectorCount < 0 || finalPatchCount < 0 ||
                finalAssignedSectorCount < 0 || finalUnassignedSectorCount < 0 ||
                reservationIntrusionCount < 0 || patchOverlapCount < 0)
                throw new ArgumentOutOfRangeException(nameof(rawCandidateSectorCount));
            if (rngDrawCountAfter < rngDrawCountBefore)
                throw new ArgumentOutOfRangeException(nameof(rngDrawCountAfter));

            var ruleValues = new List<SatelliteRulePlacementDiagnostics>(rules);
            var ruleIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rule in ruleValues)
            {
                if (rule == null || !ruleIds.Add(rule.PatchRuleId))
                    throw new ArgumentException("Rule diagnostics must be non-null and unique.", nameof(rules));
            }
            ruleValues.Sort((left, right) =>
                string.Compare(left.PatchRuleId, right.PatchRuleId, StringComparison.Ordinal));

            var recordValues = new List<SatelliteSeedPlacementRecord>(records);
            var patchIds = new HashSet<BiomePatchId>();
            var sectors = new HashSet<int>();
            foreach (var record in recordValues)
            {
                if (record == null || !patchIds.Add(record.PatchId) || !sectors.Add(record.SectorIndex))
                    throw new ArgumentException("Placement records must be non-null and unique.", nameof(records));
            }
            recordValues.Sort(CompareRecords);

            if (countMethodCallCount != ruleValues.Count ||
                desiredSatelliteSeedCount != ruleValues.Sum(value => value.DesiredSeedCount) ||
                candidateMethodCallCount != ruleValues.Sum(value => value.CandidateMethodCallCount))
                throw new ArgumentException("Rule and aggregate RNG diagnostics are inconsistent.");

            if (rollback)
            {
                if (recordValues.Count != 0 || placedSatelliteSeedCount != 0 ||
                    finalPatchCount != initialPatchCount ||
                    finalAssignedSectorCount != initialAssignedSectorCount)
                    throw new ArgumentException("Retry diagnostics must describe atomic rollback.");
            }
            else
            {
                if (placedSatelliteSeedCount != recordValues.Count ||
                    placedSatelliteSeedCount != ruleValues.Sum(value => value.AcceptedSeedCount) ||
                    finalPatchCount != initialPatchCount + placedSatelliteSeedCount ||
                    finalAssignedSectorCount != initialAssignedSectorCount + placedSatelliteSeedCount)
                    throw new ArgumentException("Successful placement conservation is invalid.");
            }

            if (finalAssignedSectorCount + finalUnassignedSectorCount != WorldGenConstants.SectorCount)
                throw new ArgumentException("Final sector counts must cover the world.");

            WorldSeed = worldSeed;
            RawCandidateSectorCount = rawCandidateSectorCount;
            CountMethodCallCount = countMethodCallCount;
            CandidateMethodCallCount = candidateMethodCallCount;
            TotalRngMethodCallCount = checked(countMethodCallCount + candidateMethodCallCount);
            RngDrawCountBefore = rngDrawCountBefore;
            RngDrawCountAfter = rngDrawCountAfter;
            DesiredSatelliteSeedCount = desiredSatelliteSeedCount;
            PlacedSatelliteSeedCount = placedSatelliteSeedCount;
            InitialPatchCount = initialPatchCount;
            InitialAssignedSectorCount = initialAssignedSectorCount;
            FinalPatchCount = finalPatchCount;
            FinalAssignedSectorCount = finalAssignedSectorCount;
            FinalUnassignedSectorCount = finalUnassignedSectorCount;
            ReservationIntrusionCount = reservationIntrusionCount;
            PatchOverlapCount = patchOverlapCount;
            this.rules = new ReadOnlyCollection<SatelliteRulePlacementDiagnostics>(ruleValues);
            this.records = new ReadOnlyCollection<SatelliteSeedPlacementRecord>(recordValues);
        }

        public ulong WorldSeed { get; }
        public IReadOnlyList<SatelliteRulePlacementDiagnostics> Rules => rules;
        public IReadOnlyList<SatelliteSeedPlacementRecord> Records => records;
        public int RawCandidateSectorCount { get; }
        public int CountMethodCallCount { get; }
        public int CandidateMethodCallCount { get; }
        public int TotalRngMethodCallCount { get; }
        public ulong RngDrawCountBefore { get; }
        public ulong RngDrawCountAfter { get; }
        public int DesiredSatelliteSeedCount { get; }
        public int PlacedSatelliteSeedCount { get; }
        public int InitialPatchCount { get; }
        public int InitialAssignedSectorCount { get; }
        public int FinalPatchCount { get; }
        public int FinalAssignedSectorCount { get; }
        public int FinalUnassignedSectorCount { get; }
        public int ReservationIntrusionCount { get; }
        public int PatchOverlapCount { get; }

        private static int CompareRecords(
            SatelliteSeedPlacementRecord left,
            SatelliteSeedPlacementRecord right)
        {
            var value = string.Compare(left.PatchRuleId, right.PatchRuleId, StringComparison.Ordinal);
            return value != 0 ? value : left.SatelliteOrdinal.CompareTo(right.SatelliteOrdinal);
        }
    }
}
