using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteDistanceViolation
    {
        public SiteDistanceViolation(
            SiteDistanceRuleKind ruleKind,
            SitePlacementKey first,
            SitePlacementKey second,
            int actualDistance,
            int minimumDistance,
            SectorCoord firstClosestSector,
            SectorCoord secondClosestSector)
        {
            if (ruleKind != SiteDistanceRuleKind.StartToRequiredSite &&
                ruleKind != SiteDistanceRuleKind.RequiredSiteToRequiredSite)
                throw new ArgumentOutOfRangeException(nameof(ruleKind));
            if (!first.IsValid || !second.IsValid || first.CompareTo(second) >= 0)
                throw new ArgumentException("Violation keys must be valid and canonical.");
            if (actualDistance < 1 || actualDistance > 24)
                throw new ArgumentOutOfRangeException(nameof(actualDistance));
            if (minimumDistance < 1 || minimumDistance > 24 || actualDistance >= minimumDistance)
                throw new ArgumentOutOfRangeException(nameof(minimumDistance));

            RuleKind = ruleKind;
            First = first;
            Second = second;
            ActualDistance = actualDistance;
            MinimumDistance = minimumDistance;
            Deficit = minimumDistance - actualDistance;
            FirstClosestSector = firstClosestSector;
            SecondClosestSector = secondClosestSector;
        }

        public SiteDistanceRuleKind RuleKind { get; }
        public SitePlacementKey First { get; }
        public SitePlacementKey Second { get; }
        public int ActualDistance { get; }
        public int MinimumDistance { get; }
        public int Deficit { get; }
        public SectorCoord FirstClosestSector { get; }
        public SectorCoord SecondClosestSector { get; }
    }

    public sealed class SiteDistanceEvaluationResult
    {
        private readonly IReadOnlyList<SiteDistanceViolation> violations;
        private readonly IReadOnlyList<SiteDistanceError> errors;

        internal SiteDistanceEvaluationResult(
            bool succeeded,
            IEnumerable<SiteDistanceViolation> violations,
            IEnumerable<SiteDistanceError> errors)
        {
            var violationSnapshot = new List<SiteDistanceViolation>(
                violations ?? throw new ArgumentNullException(nameof(violations)));
            if (violationSnapshot.Exists(item => item == null))
                throw new ArgumentException("Violations cannot contain null.", nameof(violations));
            violationSnapshot.Sort(CompareViolations);
            var unique = new List<SiteDistanceViolation>(violationSnapshot.Count);
            foreach (var violation in violationSnapshot)
            {
                if (unique.Count == 0 || CompareViolations(unique[unique.Count - 1], violation) != 0)
                    unique.Add(violation);
            }
            this.violations = new ReadOnlyCollection<SiteDistanceViolation>(unique);
            this.errors = SiteDistanceResultUtility.SnapshotErrors(errors);
            if (succeeded && this.errors.Count != 0)
                throw new ArgumentException("A successful evaluation cannot contain errors.", nameof(errors));
            if (!succeeded && (this.errors.Count == 0 || this.violations.Count != 0))
                throw new ArgumentException("A failed evaluation requires errors and no violations.");
            Succeeded = succeeded;
        }

        public bool Succeeded { get; }
        public bool Satisfied => Succeeded && violations.Count == 0;
        public IReadOnlyList<SiteDistanceViolation> Violations => violations;
        public IReadOnlyList<SiteDistanceError> Errors => errors;

        internal static SiteDistanceEvaluationResult Success(
            IEnumerable<SiteDistanceViolation> violations) =>
            new SiteDistanceEvaluationResult(true, violations, Array.Empty<SiteDistanceError>());
        internal static SiteDistanceEvaluationResult Failure(
            IEnumerable<SiteDistanceError> errors) =>
            new SiteDistanceEvaluationResult(false, Array.Empty<SiteDistanceViolation>(), errors);

        private static int CompareViolations(SiteDistanceViolation left, SiteDistanceViolation right)
        {
            var rule = left.RuleKind.CompareTo(right.RuleKind);
            if (rule != 0) return rule;
            var first = left.First.CompareTo(right.First);
            if (first != 0) return first;
            var second = left.Second.CompareTo(right.Second);
            if (second != 0) return second;
            var actual = left.ActualDistance.CompareTo(right.ActualDistance);
            return actual != 0 ? actual : left.MinimumDistance.CompareTo(right.MinimumDistance);
        }
    }
}
