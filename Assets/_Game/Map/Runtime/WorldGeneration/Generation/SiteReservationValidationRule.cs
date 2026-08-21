using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum SiteReservationValidationRule
    {
        RequiredSiteCounts,
        WorldBounds,
        FootprintOverlap,
        DistanceConstraints,
        EntryAnchors,
        CoreCapacity
    }

    public sealed class SiteReservationRuleResult
    {
        public SiteReservationRuleResult(
            SiteReservationValidationRule rule,
            bool passed,
            int violationCount,
            int measuredCount,
            int expectedCount,
            string message)
        {
            if (!Enum.IsDefined(typeof(SiteReservationValidationRule), rule))
                throw new ArgumentOutOfRangeException(nameof(rule));
            if (violationCount < 0) throw new ArgumentOutOfRangeException(nameof(violationCount));
            if (measuredCount < 0) throw new ArgumentOutOfRangeException(nameof(measuredCount));
            if (expectedCount < 0) throw new ArgumentOutOfRangeException(nameof(expectedCount));
            if (passed != (violationCount == 0))
                throw new ArgumentException("Passed must match the violation count.", nameof(passed));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("A stable non-empty message is required.", nameof(message));

            Rule = rule;
            Passed = passed;
            ViolationCount = violationCount;
            MeasuredCount = measuredCount;
            ExpectedCount = expectedCount;
            Message = message;
        }

        public SiteReservationValidationRule Rule { get; }
        public bool Passed { get; }
        public int ViolationCount { get; }
        public int MeasuredCount { get; }
        public int ExpectedCount { get; }
        public string Message { get; }
    }
}
