using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    public enum MoonPalaceRunQualitySeverity { Info, Warning, Reject }

    /// <summary>RUN06 quality-rule identifiers. A result is curated, never modified, after these rules are measured.</summary>
    public static class MoonPalaceRunQualityRule
    {
        public const string RouteTooStraight = "ROUTE_TOO_STRAIGHT";
        public const string RoomTooEmpty = "ROOM_TOO_EMPTY";
        public const string RoomTooNoisy = "ROOM_TOO_NOISY";
        public const string BranchTooShort = "BRANCH_TOO_SHORT";
        public const string BranchTooDeep = "BRANCH_TOO_DEEP";
        public const string ConnectorCrowding = "CONNECTOR_CROWDING";
        public const string VerticalVariationLow = "VERTICAL_VARIATION_LOW";
        public const string BacktrackShapeBad = "BACKTRACK_SHAPE_BAD";
        public const string OpenIslandRequired = "OPEN_ISLAND_REQUIRED";
        public const string RepairPolicyViolation = "REPAIR_POLICY_VIOLATION";
        public static readonly IReadOnlyList<string> RequiredIds = new ReadOnlyCollection<string>(new[]
        {
            RouteTooStraight, RoomTooEmpty, RoomTooNoisy, BranchTooShort, BranchTooDeep,
            ConnectorCrowding, VerticalVariationLow, BacktrackShapeBad, OpenIslandRequired, RepairPolicyViolation,
        });
    }

    public sealed class MoonPalaceRunQualityFinding
    {
        public MoonPalaceRunQualityFinding(string ruleId, MoonPalaceRunQualitySeverity severity, double measuredValue, double threshold, IEnumerable<string> affectedIds, string reason)
        {
            RuleId = ruleId ?? string.Empty; Severity = severity; MeasuredValue = measuredValue; Threshold = threshold;
            AffectedIds = new ReadOnlyCollection<string>((affectedIds ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToList());
            Reason = reason ?? string.Empty;
            if (string.IsNullOrWhiteSpace(RuleId) || string.IsNullOrWhiteSpace(Reason)) throw new ArgumentException("RUN06 quality findings require an explicit rule id and human-readable reason.");
        }

        public string RuleId { get; }
        public MoonPalaceRunQualitySeverity Severity { get; }
        public double MeasuredValue { get; }
        public double Threshold { get; }
        public IReadOnlyList<string> AffectedIds { get; }
        public string Reason { get; }
        public string CanonicalLine => RuleId + "|" + Severity + "|" + MeasuredValue.ToString("0.######", CultureInfo.InvariantCulture) + "|" + Threshold.ToString("0.######", CultureInfo.InvariantCulture) + "|" + string.Join(";", AffectedIds) + "|" + Reason;
    }

    public sealed class MoonPalaceRunQualityAnalysis
    {
        internal MoonPalaceRunQualityAnalysis(MoonPalaceSeededRunResult result, MoonPalaceRunQualityProfile profile, IEnumerable<MoonPalaceRunQualityFinding> findings)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result)); Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Findings = new ReadOnlyCollection<MoonPalaceRunQualityFinding>((findings ?? Array.Empty<MoonPalaceRunQualityFinding>()).OrderBy(finding => RuleOrder(finding.RuleId)).ToList());
            if (Findings.Select(finding => finding.RuleId).Distinct(StringComparer.Ordinal).Count() != MoonPalaceRunQualityRule.RequiredIds.Count || MoonPalaceRunQualityRule.RequiredIds.Any(id => Findings.All(finding => finding.RuleId != id))) throw new InvalidOperationException("RUN06 requires exactly one explicit finding for every quality rule.");
            PrimaryFinding = Findings.FirstOrDefault(finding => finding.Severity == MoonPalaceRunQualitySeverity.Reject) ?? Findings.FirstOrDefault(finding => finding.Severity == MoonPalaceRunQualitySeverity.Warning) ?? Findings.First();
            IsAccepted = Findings.All(finding => finding.Severity != MoonPalaceRunQualitySeverity.Reject);
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "RUN06_QUALITY_ANALYSIS", Result.CourseDigest, Profile.CanonicalDigest, "accepted=" + (IsAccepted ? "1" : "0") }.Concat(Findings.Select(finding => finding.CanonicalLine)));
        }

        public MoonPalaceSeededRunResult Result { get; }
        public MoonPalaceRunQualityProfile Profile { get; }
        public IReadOnlyList<MoonPalaceRunQualityFinding> Findings { get; }
        public MoonPalaceRunQualityFinding PrimaryFinding { get; }
        public bool IsAccepted { get; }
        public string CanonicalDigest { get; }
        private static int RuleOrder(string id) { for (var index = 0; index < MoonPalaceRunQualityRule.RequiredIds.Count; index++) if (MoonPalaceRunQualityRule.RequiredIds[index] == id) return index; return int.MaxValue; }
    }
}
