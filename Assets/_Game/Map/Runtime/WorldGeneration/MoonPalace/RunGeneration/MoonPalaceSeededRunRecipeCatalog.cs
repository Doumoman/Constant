using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>Stable RUN05 recipe inventory. Recipes differ in topology and dimensions, not merely in seed.</summary>
    public static class MoonPalaceSeededRunRecipeCatalog
    {
        private static readonly IReadOnlyList<MoonPalaceSeededRunRecipe> recipes = new ReadOnlyCollection<MoonPalaceSeededRunRecipe>(new[]
        {
            new MoonPalaceSeededRunRecipe("COMPACT_SPLIT_RUN", 64, 20, 7, 2, 1, "compact route with clear split/rejoin"),
            new MoonPalaceSeededRunRecipe("TALL_LOOP_RUN", 72, 28, 8, 2, 2, "vertical climb/drop and loop-like return"),
            new MoonPalaceSeededRunRecipe("WIDE_BRANCH_RUN", 96, 24, 9, 3, 2, "wide horizontal run with side branches"),
        }.OrderBy(recipe => recipe.RecipeId, StringComparer.Ordinal).ToList());

        public static IReadOnlyList<MoonPalaceSeededRunRecipe> Recipes => recipes;
        public static string CanonicalDigest => BakingCanonicalDigest.HashCanonicalLines(recipes.Select(recipe => recipe.CanonicalDigest));
        public static MoonPalaceSeededRunRecipe Default => Get("WIDE_BRANCH_RUN");
        public static MoonPalaceSeededRunRecipe Get(string recipeId) => recipes.Single(recipe => string.Equals(recipe.RecipeId, recipeId, StringComparison.Ordinal));
    }
}
