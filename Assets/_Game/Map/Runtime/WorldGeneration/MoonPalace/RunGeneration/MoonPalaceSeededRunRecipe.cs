using System;
using System.Globalization;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>Immutable RUN05 topology contract. Dimensions are direct 4x4 MicroPattern grid dimensions, never sector dimensions.</summary>
    public sealed class MoonPalaceSeededRunRecipe
    {
        public MoonPalaceSeededRunRecipe(string recipeId, int patternGridWidth, int patternGridHeight, int mainRoomTarget, int branchTarget, int splitRejoinTarget, string shapeDescription)
        {
            RecipeId = recipeId ?? string.Empty;
            PatternGridWidth = patternGridWidth;
            PatternGridHeight = patternGridHeight;
            MainRoomTarget = mainRoomTarget;
            BranchTarget = branchTarget;
            SplitRejoinTarget = splitRejoinTarget;
            ShapeDescription = shapeDescription ?? string.Empty;
            Validate();
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "RUN05_RECIPE",
                "recipe_id=" + RecipeId,
                "pattern_grid=" + PatternGridWidth.ToString(CultureInfo.InvariantCulture) + "x" + PatternGridHeight.ToString(CultureInfo.InvariantCulture),
                "tile_grid=" + TileWidth.ToString(CultureInfo.InvariantCulture) + "x" + TileHeight.ToString(CultureInfo.InvariantCulture),
                "main_rooms=" + MainRoomTarget.ToString(CultureInfo.InvariantCulture),
                "branches=" + BranchTarget.ToString(CultureInfo.InvariantCulture),
                "split_rejoin=" + SplitRejoinTarget.ToString(CultureInfo.InvariantCulture),
                "shape=" + ShapeDescription,
            });
        }

        public string RecipeId { get; }
        public int PatternWidth => 4;
        public int PatternHeight => 4;
        public int PatternGridWidth { get; }
        public int PatternGridHeight { get; }
        public int TileWidth => PatternGridWidth * PatternWidth;
        public int TileHeight => PatternGridHeight * PatternHeight;
        public int MainRoomTarget { get; }
        public int BranchTarget { get; }
        public int SplitRejoinTarget { get; }
        public string ShapeDescription { get; }
        public string CanonicalDigest { get; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(RecipeId) || PatternGridWidth < 32 || PatternGridHeight < 16 || MainRoomTarget < 7 || BranchTarget < 2 || SplitRejoinTarget < 1)
                throw new InvalidOperationException("RUN05 requires a named, structurally complete direct MicroPattern recipe.");
            if (TileWidth != PatternGridWidth * 4 || TileHeight != PatternGridHeight * 4 || (TileWidth == 48 && TileHeight == 32))
                throw new InvalidOperationException("RUN05 recipe tile dimensions must derive from its pattern grid and cannot use a 48x32 sector.");
        }
    }
}
