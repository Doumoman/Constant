using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    public enum MoonPalaceRunVariantTopology
    {
        BranchingLowland = 0,
        VerticalLoop = 1,
        MultiRoomSplit = 2,
    }

    /// <summary>Immutable RUN02 input. Tile dimensions derive only from the direct 4x4 pattern grid.</summary>
    public sealed class MoonPalaceRunVariantConfig
    {
        public const int PatternSize = 4;
        public const int RequiredCandidateCount = 500;
        public const string DefaultSeedId = "MP_QA_01";
        public const int DefaultSeedValue = 1924737067;

        private MoonPalaceRunVariantConfig(string variantId, string seedId, int seedValue, int patternGridWidth,
            int patternGridHeight, int mainPathMinSteps, int mainPathMaxSteps, int branchMinCount,
            int branchMaxCount, int splitRejoinMinCount, int splitRejoinMaxCount,
            MoonPalaceRunVariantTopology topology, bool allow90DegreeRotation, bool allowSilentCarve)
        {
            VariantId = variantId ?? string.Empty;
            SeedId = seedId ?? string.Empty;
            SeedValue = seedValue;
            PatternWidth = PatternSize;
            PatternHeight = PatternSize;
            PatternGridWidth = patternGridWidth;
            PatternGridHeight = patternGridHeight;
            MainPathMinSteps = mainPathMinSteps;
            MainPathMaxSteps = mainPathMaxSteps;
            BranchMinCount = branchMinCount;
            BranchMaxCount = branchMaxCount;
            SplitRejoinMinCount = splitRejoinMinCount;
            SplitRejoinMaxCount = splitRejoinMaxCount;
            Topology = topology;
            Allow90DegreeRotation = allow90DegreeRotation;
            AllowSilentCarve = allowSilentCarve;
            Validate();
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "RUN02_CONFIG", "variant_id=" + VariantId, "seed_id=" + SeedId,
                "seed_value=" + SeedValue.ToString(CultureInfo.InvariantCulture),
                "pattern_size=" + PatternWidth.ToString(CultureInfo.InvariantCulture) + "x" + PatternHeight.ToString(CultureInfo.InvariantCulture),
                "pattern_grid_size=" + PatternGridWidth.ToString(CultureInfo.InvariantCulture) + "x" + PatternGridHeight.ToString(CultureInfo.InvariantCulture),
                "tile_size=" + TileWidth.ToString(CultureInfo.InvariantCulture) + "x" + TileHeight.ToString(CultureInfo.InvariantCulture),
                "candidate_count=" + RequiredCandidateCount.ToString(CultureInfo.InvariantCulture),
                "main_path_steps=" + MainPathMinSteps.ToString(CultureInfo.InvariantCulture) + ".." + MainPathMaxSteps.ToString(CultureInfo.InvariantCulture),
                "branch_count=" + BranchMinCount.ToString(CultureInfo.InvariantCulture) + ".." + BranchMaxCount.ToString(CultureInfo.InvariantCulture),
                "split_rejoin_count=" + SplitRejoinMinCount.ToString(CultureInfo.InvariantCulture) + ".." + SplitRejoinMaxCount.ToString(CultureInfo.InvariantCulture),
                "topology=" + Topology, "allow_90_degree_rotation=" + (Allow90DegreeRotation ? "1" : "0"),
                "allow_silent_carve=" + (AllowSilentCarve ? "1" : "0"),
            });
        }

        public string VariantId { get; }
        public string SeedId { get; }
        public int SeedValue { get; }
        public int PatternWidth { get; }
        public int PatternHeight { get; }
        public int PatternGridWidth { get; }
        public int PatternGridHeight { get; }
        public int TileWidth => PatternGridWidth * PatternWidth;
        public int TileHeight => PatternGridHeight * PatternHeight;
        public int MainPathMinSteps { get; }
        public int MainPathMaxSteps { get; }
        public int BranchMinCount { get; }
        public int BranchMaxCount { get; }
        public int SplitRejoinMinCount { get; }
        public int SplitRejoinMaxCount { get; }
        public MoonPalaceRunVariantTopology Topology { get; }
        public bool Allow90DegreeRotation { get; }
        public bool AllowSilentCarve { get; }
        public string CanonicalDigest { get; }

        public static IReadOnlyList<MoonPalaceRunVariantConfig> CreateDefaults()
        {
            var defaults = new[]
            {
                Create("MP_RUN_02_A_BRANCHING_LOWLAND", DefaultSeedId, DefaultSeedValue, 48, 14, 48, 80, 6, 10, 0, 0,
                    MoonPalaceRunVariantTopology.BranchingLowland, false, false),
                Create("MP_RUN_02_B_VERTICAL_LOOP", DefaultSeedId, DefaultSeedValue, 42, 18, 64, 110, 8, 12, 2, 4,
                    MoonPalaceRunVariantTopology.VerticalLoop, false, false),
                Create("MP_RUN_02_C_MULTI_ROOM_SPLIT", DefaultSeedId, DefaultSeedValue, 60, 16, 60, 110, 10, 14, 2, 4,
                    MoonPalaceRunVariantTopology.MultiRoomSplit, false, false),
            };
            if (defaults.Select(config => config.VariantId).Distinct(StringComparer.Ordinal).Count() != defaults.Length)
                throw new InvalidOperationException("RUN02 rejects duplicate variant IDs.");
            return new ReadOnlyCollection<MoonPalaceRunVariantConfig>(defaults);
        }

        public static MoonPalaceRunVariantConfig Create(string variantId, string seedId, int seedValue,
            int patternGridWidth, int patternGridHeight, int mainPathMinSteps, int mainPathMaxSteps,
            int branchMinCount, int branchMaxCount, int splitRejoinMinCount, int splitRejoinMaxCount,
            MoonPalaceRunVariantTopology topology, bool allow90DegreeRotation, bool allowSilentCarve)
        {
            return new MoonPalaceRunVariantConfig(variantId, seedId, seedValue, patternGridWidth, patternGridHeight,
                mainPathMinSteps, mainPathMaxSteps, branchMinCount, branchMaxCount, splitRejoinMinCount,
                splitRejoinMaxCount, topology, allow90DegreeRotation, allowSilentCarve);
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(VariantId) || string.IsNullOrWhiteSpace(SeedId))
                throw new InvalidOperationException("RUN02 requires non-empty variant and seed identifiers.");
            if (PatternWidth != 4 || PatternHeight != 4)
                throw new InvalidOperationException("RUN02 directly composes only 4x4 MicroPatterns.");
            if (PatternGridWidth < 24 || PatternGridHeight < 12)
                throw new InvalidOperationException("RUN02 pattern grid is too small for the required room and branch topology.");
            if (TileWidth != PatternGridWidth * 4 || TileHeight != PatternGridHeight * 4)
                throw new InvalidOperationException("RUN02 tile size must be derived from pattern_grid_size * 4.");
            if (TileWidth == 48 && TileHeight == 32)
                throw new InvalidOperationException("RUN02 rejects a 48x32 sector dimension copied into a direct MicroPattern variant.");
            if (MainPathMinSteps < PatternGridWidth || MainPathMaxSteps < MainPathMinSteps)
                throw new InvalidOperationException("RUN02 main-path bounds must include the horizontal start-to-exit route.");
            if (BranchMinCount < 1 || BranchMaxCount < BranchMinCount)
                throw new InvalidOperationException("RUN02 requires a valid branch range.");
            if (SplitRejoinMinCount < 0 || SplitRejoinMaxCount < SplitRejoinMinCount)
                throw new InvalidOperationException("RUN02 requires a valid split-rejoin range.");
            if (Topology == MoonPalaceRunVariantTopology.VerticalLoop && (SplitRejoinMinCount < 2 || PatternGridHeight < 18))
                throw new InvalidOperationException("RUN02 vertical-loop variant requires two loops and an 18-row pattern grid.");
            if (Topology == MoonPalaceRunVariantTopology.MultiRoomSplit && (SplitRejoinMinCount < 2 || PatternGridWidth < 60))
                throw new InvalidOperationException("RUN02 multi-room split requires two choices and a 60-column pattern grid.");
            if (Allow90DegreeRotation)
                throw new InvalidOperationException("RUN02 forbids 90-degree MicroPattern rotation.");
            if (AllowSilentCarve)
                throw new InvalidOperationException("RUN02 forbids silent mask carve or repair.");
        }
    }
}
