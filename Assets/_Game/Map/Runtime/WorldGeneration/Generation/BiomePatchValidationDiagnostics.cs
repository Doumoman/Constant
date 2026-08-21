using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class BiomePatchValidationRuleResult
    {
        internal BiomePatchValidationRuleResult(
            BiomePatchValidationRule rule,
            int checkedCount,
            int violationCount)
        {
            if (!Enum.IsDefined(typeof(BiomePatchValidationRule), rule))
                throw new ArgumentOutOfRangeException(nameof(rule));
            if (checkedCount < 0) throw new ArgumentOutOfRangeException(nameof(checkedCount));
            if (violationCount < 0) throw new ArgumentOutOfRangeException(nameof(violationCount));
            Rule = rule;
            CheckedCount = checkedCount;
            ViolationCount = violationCount;
        }

        public BiomePatchValidationRule Rule { get; }
        public bool Passed => ViolationCount == 0;
        public int CheckedCount { get; }
        public int ViolationCount { get; }
    }

    public sealed class BiomePatchValidationDiagnostics
    {
        private readonly IReadOnlyList<BiomePatchValidationRuleResult> ruleResults;
        private readonly IReadOnlyList<BiomePatchValidationViolation> violations;

        internal BiomePatchValidationDiagnostics(
            ulong worldSeed,
            IEnumerable<BiomePatchValidationRuleResult> ruleResults,
            IEnumerable<BiomePatchValidationViolation> violations,
            int patchCount,
            int corePatchCount,
            int satellitePatchCount,
            int intrusionPatchCount,
            int assignedSectorCount,
            int unassignedSectorCount,
            int patchSectorSum,
            int requiredBiomeCount,
            int coreBindingCount,
            int maxPatchSize,
            int disconnectedPatchCount,
            int overlapCount,
            int orphanCount,
            int unassignedNonReservedCount,
            int siteMisownershipCount,
            int intrusionInvalidCount,
            int patchCsvRowCount,
            int worldCsvRowCount,
            int patchCsvByteCount,
            int worldCsvByteCount,
            ulong rngDrawCount,
            int sourceMutationCount)
        {
            if (ruleResults == null) throw new ArgumentNullException(nameof(ruleResults));
            if (violations == null) throw new ArgumentNullException(nameof(violations));
            var rules = new List<BiomePatchValidationRuleResult>(ruleResults);
            rules.Sort((left, right) => left.Rule.CompareTo(right.Rule));
            if (rules.Count != Enum.GetValues(typeof(BiomePatchValidationRule)).Length)
                throw new ArgumentException("Exactly 15 rule results are required.", nameof(ruleResults));
            for (var index = 0; index < rules.Count; index++)
                if (rules[index] == null || (int)rules[index].Rule != index)
                    throw new ArgumentException("Rule results must cover the exact ordered rule set.", nameof(ruleResults));

            var orderedViolations = new List<BiomePatchValidationViolation>(violations);
            orderedViolations.Sort(BiomePatchValidationResult.CompareViolations);

            WorldSeed = worldSeed;
            this.ruleResults = new ReadOnlyCollection<BiomePatchValidationRuleResult>(rules);
            this.violations = new ReadOnlyCollection<BiomePatchValidationViolation>(orderedViolations);
            PatchCount = RequireNonNegative(patchCount, nameof(patchCount));
            CorePatchCount = RequireNonNegative(corePatchCount, nameof(corePatchCount));
            SatellitePatchCount = RequireNonNegative(satellitePatchCount, nameof(satellitePatchCount));
            IntrusionPatchCount = RequireNonNegative(intrusionPatchCount, nameof(intrusionPatchCount));
            AssignedSectorCount = RequireNonNegative(assignedSectorCount, nameof(assignedSectorCount));
            UnassignedSectorCount = RequireNonNegative(unassignedSectorCount, nameof(unassignedSectorCount));
            PatchSectorSum = RequireNonNegative(patchSectorSum, nameof(patchSectorSum));
            RequiredBiomeCount = RequireNonNegative(requiredBiomeCount, nameof(requiredBiomeCount));
            CoreBindingCount = RequireNonNegative(coreBindingCount, nameof(coreBindingCount));
            MaxPatchSize = RequireNonNegative(maxPatchSize, nameof(maxPatchSize));
            DisconnectedPatchCount = RequireNonNegative(disconnectedPatchCount, nameof(disconnectedPatchCount));
            OverlapCount = RequireNonNegative(overlapCount, nameof(overlapCount));
            OrphanCount = RequireNonNegative(orphanCount, nameof(orphanCount));
            UnassignedNonReservedCount = RequireNonNegative(unassignedNonReservedCount, nameof(unassignedNonReservedCount));
            SiteMisownershipCount = RequireNonNegative(siteMisownershipCount, nameof(siteMisownershipCount));
            IntrusionInvalidCount = RequireNonNegative(intrusionInvalidCount, nameof(intrusionInvalidCount));
            PatchCsvRowCount = RequireNonNegative(patchCsvRowCount, nameof(patchCsvRowCount));
            WorldCsvRowCount = RequireNonNegative(worldCsvRowCount, nameof(worldCsvRowCount));
            PatchCsvByteCount = RequireNonNegative(patchCsvByteCount, nameof(patchCsvByteCount));
            WorldCsvByteCount = RequireNonNegative(worldCsvByteCount, nameof(worldCsvByteCount));
            RngDrawCount = rngDrawCount;
            SourceMutationCount = RequireNonNegative(sourceMutationCount, nameof(sourceMutationCount));
        }

        public ulong WorldSeed { get; }
        public IReadOnlyList<BiomePatchValidationRuleResult> RuleResults => ruleResults;
        public IReadOnlyList<BiomePatchValidationViolation> Violations => violations;
        public int PatchCount { get; }
        public int CorePatchCount { get; }
        public int SatellitePatchCount { get; }
        public int IntrusionPatchCount { get; }
        public int AssignedSectorCount { get; }
        public int UnassignedSectorCount { get; }
        public int PatchSectorSum { get; }
        public int RequiredBiomeCount { get; }
        public int CoreBindingCount { get; }
        public int MaxPatchSize { get; }
        public int DisconnectedPatchCount { get; }
        public int OverlapCount { get; }
        public int OrphanCount { get; }
        public int UnassignedNonReservedCount { get; }
        public int SiteMisownershipCount { get; }
        public int IntrusionInvalidCount { get; }
        public int PatchCsvRowCount { get; }
        public int WorldCsvRowCount { get; }
        public int PatchCsvByteCount { get; }
        public int WorldCsvByteCount { get; }
        public ulong RngDrawCount { get; }
        public int SourceMutationCount { get; }

        private static int RequireNonNegative(int value, string parameterName)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(parameterName);
            return value;
        }
    }
}
