using System;
using System.Collections.Generic;
using System.Globalization;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>
    /// Immutable RUN01 input. Tile dimensions are always derived from the pattern grid and never from a sector contract.
    /// </summary>
    public sealed class MoonPalaceMicroPatternRunConfig
    {
        public const string DefaultRunId = "MP_RUN_01";
        public const string DefaultSeedId = "MP_QA_01";
        public const int DefaultSeedValue = 1924737067;
        public const int RequiredCandidateCount = 500;

        private MoonPalaceMicroPatternRunConfig(string runId, string seedId, int seedValue, int patternWidth,
            int patternHeight, int patternGridWidth, int patternGridHeight, int candidateCount,
            int mainPathMinSteps, int mainPathMaxSteps, int branchMinCount, int branchMaxCount,
            bool allow90DegreeRotation, bool allowSilentCarve)
        {
            RunId = runId ?? string.Empty;
            SeedId = seedId ?? string.Empty;
            SeedValue = seedValue;
            PatternWidth = patternWidth;
            PatternHeight = patternHeight;
            PatternGridWidth = patternGridWidth;
            PatternGridHeight = patternGridHeight;
            CandidateCount = candidateCount;
            MainPathMinSteps = mainPathMinSteps;
            MainPathMaxSteps = mainPathMaxSteps;
            BranchMinCount = branchMinCount;
            BranchMaxCount = branchMaxCount;
            Allow90DegreeRotation = allow90DegreeRotation;
            AllowSilentCarve = allowSilentCarve;
            Validate();
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "RUN01_CONFIG",
                "run_id=" + RunId,
                "seed_id=" + SeedId,
                "seed_value=" + SeedValue.ToString(CultureInfo.InvariantCulture),
                "pattern_size=" + PatternWidth.ToString(CultureInfo.InvariantCulture) + "x" + PatternHeight.ToString(CultureInfo.InvariantCulture),
                "pattern_grid_size=" + PatternGridWidth.ToString(CultureInfo.InvariantCulture) + "x" + PatternGridHeight.ToString(CultureInfo.InvariantCulture),
                "tile_size=" + TileWidth.ToString(CultureInfo.InvariantCulture) + "x" + TileHeight.ToString(CultureInfo.InvariantCulture),
                "candidate_count=" + CandidateCount.ToString(CultureInfo.InvariantCulture),
                "main_path_steps=" + MainPathMinSteps.ToString(CultureInfo.InvariantCulture) + ".." + MainPathMaxSteps.ToString(CultureInfo.InvariantCulture),
                "branch_count=" + BranchMinCount.ToString(CultureInfo.InvariantCulture) + ".." + BranchMaxCount.ToString(CultureInfo.InvariantCulture),
                "allow_90_degree_rotation=" + (Allow90DegreeRotation ? "1" : "0"),
                "allow_silent_carve=" + (AllowSilentCarve ? "1" : "0"),
            });
        }

        public string RunId { get; }
        public string SeedId { get; }
        public int SeedValue { get; }
        public int PatternWidth { get; }
        public int PatternHeight { get; }
        public int PatternGridWidth { get; }
        public int PatternGridHeight { get; }
        public int TileWidth => PatternGridWidth * PatternWidth;
        public int TileHeight => PatternGridHeight * PatternHeight;
        public int CandidateCount { get; }
        public int MainPathMinSteps { get; }
        public int MainPathMaxSteps { get; }
        public int BranchMinCount { get; }
        public int BranchMaxCount { get; }
        public bool Allow90DegreeRotation { get; }
        public bool AllowSilentCarve { get; }
        public string CanonicalDigest { get; }

        public static MoonPalaceMicroPatternRunConfig CreateDefault()
        {
            return Create(DefaultRunId, DefaultSeedId, DefaultSeedValue, 4, 4, 40, 12,
                RequiredCandidateCount, 40, 56, 4, 8, false, false);
        }

        public static MoonPalaceMicroPatternRunConfig Create(string runId, string seedId, int seedValue,
            int patternWidth, int patternHeight, int patternGridWidth, int patternGridHeight, int candidateCount,
            int mainPathMinSteps, int mainPathMaxSteps, int branchMinCount, int branchMaxCount,
            bool allow90DegreeRotation, bool allowSilentCarve)
        {
            return new MoonPalaceMicroPatternRunConfig(runId, seedId, seedValue, patternWidth, patternHeight,
                patternGridWidth, patternGridHeight, candidateCount, mainPathMinSteps, mainPathMaxSteps,
                branchMinCount, branchMaxCount, allow90DegreeRotation, allowSilentCarve);
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(RunId) || string.IsNullOrWhiteSpace(SeedId))
                throw new InvalidOperationException("RUN01 requires non-empty run and seed identifiers.");
            if (PatternWidth != 4 || PatternHeight != 4)
                throw new InvalidOperationException("RUN01 directly composes only 4x4 MicroPatterns.");
            if (PatternGridWidth < 2 || PatternGridHeight < 4)
                throw new InvalidOperationException("RUN01 pattern grid is too small for a reachable run.");
            if (TileWidth != PatternGridWidth * PatternWidth || TileHeight != PatternGridHeight * PatternHeight)
                throw new InvalidOperationException("RUN01 tile size must be derived from pattern_grid_size * pattern_size.");
            if (CandidateCount != RequiredCandidateCount)
                throw new InvalidOperationException("RUN01 requires the audited 500-candidate MicroPattern pool.");
            if (MainPathMinSteps < PatternGridWidth || MainPathMaxSteps < MainPathMinSteps)
                throw new InvalidOperationException("RUN01 main-path bounds must include a left-to-right grid traversal.");
            if (BranchMinCount < 1 || BranchMaxCount < BranchMinCount)
                throw new InvalidOperationException("RUN01 requires a positive, ordered branch range.");
            if (Allow90DegreeRotation)
                throw new InvalidOperationException("RUN01 does not permit 90-degree MicroPattern rotation.");
            if (AllowSilentCarve)
                throw new InvalidOperationException("RUN01 never permits silent mask carving.");
        }
    }
}
