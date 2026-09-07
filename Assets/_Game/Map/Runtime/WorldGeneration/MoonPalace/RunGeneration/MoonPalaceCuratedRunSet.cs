using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    public enum MoonPalaceRunCurationStatus { Accepted, Rejected }

    public sealed class MoonPalaceCuratedRunRecord
    {
        internal MoonPalaceCuratedRunRecord(MoonPalaceRunQualityAnalysis analysis)
        {
            Analysis = analysis ?? throw new ArgumentNullException(nameof(analysis)); Status = analysis.IsAccepted ? MoonPalaceRunCurationStatus.Accepted : MoonPalaceRunCurationStatus.Rejected; PrimaryFinding = analysis.PrimaryFinding;
        }
        public MoonPalaceRunQualityAnalysis Analysis { get; }
        public MoonPalaceSeededRunResult Result => Analysis.Result;
        public MoonPalaceRunCurationStatus Status { get; }
        public MoonPalaceRunQualityFinding PrimaryFinding { get; }
        public string RecipeId => Result.Recipe.RecipeId;
        public int Seed => Result.Seed;
        public string CanonicalLine => RecipeId + "|" + Seed.ToString(CultureInfo.InvariantCulture) + "|" + Status + "|" + PrimaryFinding.CanonicalLine + "|" + Result.CourseDigest + "|" + Analysis.CanonicalDigest;
    }

    public sealed class MoonPalaceRecipeTuningDelta
    {
        public MoonPalaceRecipeTuningDelta(string recipeId, string field, string oldValue, string newValue, string reason, IEnumerable<string> motivatingRuns)
        {
            RecipeId = recipeId ?? string.Empty; Field = field ?? string.Empty; OldValue = oldValue ?? string.Empty; NewValue = newValue ?? string.Empty; Reason = reason ?? string.Empty;
            MotivatingRuns = new ReadOnlyCollection<string>((motivatingRuns ?? Array.Empty<string>()).OrderBy(value => value, StringComparer.Ordinal).ToList());
        }
        public string RecipeId { get; }
        public string Field { get; }
        public string OldValue { get; }
        public string NewValue { get; }
        public string Reason { get; }
        public IReadOnlyList<string> MotivatingRuns { get; }
        public bool IsEmpty => string.IsNullOrEmpty(Field);
        public string CanonicalLine => IsEmpty ? "NO_RECIPE_TUNING" : RecipeId + "|" + Field + "|" + OldValue + "|" + NewValue + "|" + Reason + "|" + string.Join(";", MotivatingRuns);
        public static MoonPalaceRecipeTuningDelta None => new MoonPalaceRecipeTuningDelta(string.Empty, string.Empty, string.Empty, string.Empty, "No RUN05 recipe field changed: curation retains observed weak wide-corridor examples instead of silently changing or repairing them.", Array.Empty<string>());
    }

    /// <summary>Deterministic RUN06 curation output. Rejected records are retained with all findings and never discarded.</summary>
    public sealed class MoonPalaceCuratedRunSet
    {
        internal MoonPalaceCuratedRunSet(IEnumerable<MoonPalaceCuratedRunRecord> records, MoonPalaceRecipeTuningDelta tuning)
        {
            Records = new ReadOnlyCollection<MoonPalaceCuratedRunRecord>((records ?? Array.Empty<MoonPalaceCuratedRunRecord>()).OrderBy(record => record.RecipeId, StringComparer.Ordinal).ThenBy(record => record.Seed).ToList());
            TuningDelta = tuning ?? MoonPalaceRecipeTuningDelta.None;
            Accepted = new ReadOnlyCollection<MoonPalaceCuratedRunRecord>(Records.Where(record => record.Status == MoonPalaceRunCurationStatus.Accepted).ToList());
            Rejected = new ReadOnlyCollection<MoonPalaceCuratedRunRecord>(Records.Where(record => record.Status == MoonPalaceRunCurationStatus.Rejected).ToList());
            if (Records.Count != 36 || Accepted.Count < 9 || Rejected.Count < 6) throw new InvalidOperationException("RUN06 requires a 36-course batch with visible accepted and rejected evidence.");
            Recommended = Accepted.OrderByDescending(record => RecommendationScore(record)).ThenBy(record => record.RecipeId, StringComparer.Ordinal).ThenBy(record => record.Seed).First();
            CurationDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "RUN06_CURATION_BATCH", MoonPalaceRunQualityProfileCatalog.CanonicalDigest, TuningDelta.CanonicalLine, "recommended=" + Recommended.RecipeId + ":" + Recommended.Seed.ToString(CultureInfo.InvariantCulture) }.Concat(Records.Select(record => record.CanonicalLine)));
        }
        public IReadOnlyList<MoonPalaceCuratedRunRecord> Records { get; }
        public IReadOnlyList<MoonPalaceCuratedRunRecord> Accepted { get; }
        public IReadOnlyList<MoonPalaceCuratedRunRecord> Rejected { get; }
        public MoonPalaceRecipeTuningDelta TuningDelta { get; }
        public MoonPalaceCuratedRunRecord Recommended { get; }
        public string CurationDigest { get; }
        public int RecipeCount => Records.Select(record => record.RecipeId).Distinct(StringComparer.Ordinal).Count();
        public int GeneratedCount => Records.Count;
        public string TopRejectionReasons => string.Join("; ", Rejected.GroupBy(record => record.PrimaryFinding.RuleId, StringComparer.Ordinal).OrderByDescending(group => group.Count()).ThenBy(group => group.Key, StringComparer.Ordinal).Select(group => group.Key + "=" + group.Count().ToString(CultureInfo.InvariantCulture)));
        private static int RecommendationScore(MoonPalaceCuratedRunRecord record)
        {
            var vertical = record.Analysis.Findings.Single(finding => finding.RuleId == MoonPalaceRunQualityRule.VerticalVariationLow).MeasuredValue;
            var split = record.Analysis.Findings.Single(finding => finding.RuleId == MoonPalaceRunQualityRule.BacktrackShapeBad).MeasuredValue;
            var branches = record.Result.Edges.Count(edge => edge.Ownership == "BRANCH");
            return (int)(vertical * 1000d + split * 100d + branches);
        }
    }
}
