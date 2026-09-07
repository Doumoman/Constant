using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>Bounded 3×12 RUN06 batch. There is no reroll, acceptance seed list, repair, or deletion of rejected evidence.</summary>
    public static class MoonPalaceRunCurationBatch
    {
        public const int FirstSeed = 1924737067;
        public const int LastSeed = 1924737078;
        public const int SeedsPerRecipe = 12;
        public static MoonPalaceCuratedRunSet LastBuilt { get; private set; }

        public static MoonPalaceCuratedRunSet Build()
        {
            var records = new List<MoonPalaceCuratedRunRecord>();
            foreach (var recipe in MoonPalaceSeededRunRecipeCatalog.Recipes.OrderBy(recipe => recipe.RecipeId, StringComparer.Ordinal))
            for (var seed = FirstSeed; seed <= LastSeed; seed++)
            {
                var generated = MoonPalaceSeededRunGenerator.Generate(recipe, seed);
                var analysis = MoonPalaceRunQualityAnalyzer.Analyze(generated);
                records.Add(new MoonPalaceCuratedRunRecord(analysis));
            }
            if (records.Count != MoonPalaceSeededRunRecipeCatalog.Recipes.Count * SeedsPerRecipe) throw new InvalidOperationException("RUN06 batch must generate each configured recipe and seed exactly once.");
            LastBuilt = new MoonPalaceCuratedRunSet(records, MoonPalaceRecipeTuningDelta.None);
            return LastBuilt;
        }
    }
}
