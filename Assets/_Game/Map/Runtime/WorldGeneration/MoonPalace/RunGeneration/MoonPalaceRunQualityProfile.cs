using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>Visible-shape thresholds, distinct per already-structural RUN05 recipe. These thresholds do not change terrain.</summary>
    public sealed class MoonPalaceRunQualityProfile
    {
        public MoonPalaceRunQualityProfile(string recipeId, int maxSameRowMainRooms, double maxOpenRatio, int maxNoiseFragments, int minBranchLength, double maxBranchDepthRatio, int minConnectorSpacing, int minVerticalSpan, int minSplitSeparation)
        {
            RecipeId = recipeId ?? string.Empty; MaxSameRowMainRooms = maxSameRowMainRooms; MaxOpenRatio = maxOpenRatio; MaxNoiseFragments = maxNoiseFragments; MinBranchLength = minBranchLength; MaxBranchDepthRatio = maxBranchDepthRatio; MinConnectorSpacing = minConnectorSpacing; MinVerticalSpan = minVerticalSpan; MinSplitSeparation = minSplitSeparation;
            if (string.IsNullOrWhiteSpace(RecipeId) || MaxSameRowMainRooms < 1 || MaxOpenRatio <= 0d || MaxNoiseFragments < 0 || MinBranchLength < 1 || MaxBranchDepthRatio <= 0d || MinConnectorSpacing < 0 || MinVerticalSpan < 0 || MinSplitSeparation < 1) throw new ArgumentException("RUN06 quality profile values are invalid.");
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "RUN06_QUALITY_PROFILE", "recipe=" + RecipeId, "same_row=" + I(MaxSameRowMainRooms), "open_ratio=" + D(MaxOpenRatio), "noise=" + I(MaxNoiseFragments), "branch_min=" + I(MinBranchLength), "branch_ratio=" + D(MaxBranchDepthRatio), "connector_spacing=" + I(MinConnectorSpacing), "vertical_span=" + I(MinVerticalSpan), "split_separation=" + I(MinSplitSeparation),
            });
        }

        public string RecipeId { get; }
        public int MaxSameRowMainRooms { get; }
        public double MaxOpenRatio { get; }
        public int MaxNoiseFragments { get; }
        public int MinBranchLength { get; }
        public double MaxBranchDepthRatio { get; }
        public int MinConnectorSpacing { get; }
        public int MinVerticalSpan { get; }
        public int MinSplitSeparation { get; }
        public string CanonicalDigest { get; }
        private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string D(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    public static class MoonPalaceRunQualityProfileCatalog
    {
        private static readonly IReadOnlyList<MoonPalaceRunQualityProfile> profiles = new ReadOnlyCollection<MoonPalaceRunQualityProfile>(new[]
        {
            // Wide's nine consecutive main-room frames demonstrate the corridor issue; its branch and split structure remains visible as rejected evidence.
            new MoonPalaceRunQualityProfile("WIDE_BRANCH_RUN", 6, 0.80d, 2600, 4, 0.24d, 4, 0, 4),
            new MoonPalaceRunQualityProfile("TALL_LOOP_RUN", 4, 0.80d, 2200, 4, 0.28d, 4, 10, 4),
            new MoonPalaceRunQualityProfile("COMPACT_SPLIT_RUN", 7, 0.80d, 1500, 4, 0.24d, 4, 0, 4),
        }.OrderBy(profile => profile.RecipeId, StringComparer.Ordinal).ToList());

        public static IReadOnlyList<MoonPalaceRunQualityProfile> Profiles => profiles;
        public static string CanonicalDigest => BakingCanonicalDigest.HashCanonicalLines(new[] { "RUN06_QUALITY_PROFILE_CATALOG" }.Concat(profiles.Select(profile => profile.CanonicalDigest)));
        public static MoonPalaceRunQualityProfile Get(string recipeId) => profiles.Single(profile => string.Equals(profile.RecipeId, recipeId, StringComparison.Ordinal));
    }
}
