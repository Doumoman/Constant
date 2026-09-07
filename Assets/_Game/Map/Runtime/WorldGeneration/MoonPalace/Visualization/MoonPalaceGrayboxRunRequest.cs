using System;
using System.Collections.Generic;
using System.Globalization;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.Visualization
{
    public enum MoonPalaceGrayboxRunMode
    {
        DryRun = 0,
        Run = 1,
    }

    /// <summary>
    /// Immutable, deliberately narrow request for the VIS01 one-sector graybox.
    /// This is not a request format for arbitrary seeds or world-sized generation.
    /// </summary>
    public sealed class MoonPalaceGrayboxRunRequest
    {
        public const string RequiredSeedId = "MP_QA_01";
        public const int RequiredSeedValue = 1924737067;
        public const int RequiredSectorX = 6;
        public const int RequiredSectorY = 6;
        public const int RequiredCandidateCount = 500;
        public const string RequiredOutputScenePath =
            "Assets/_Game/Map/Scenes/MoonPalace/VIS01/MoonPalaceGrayboxExample_VIS01.unity";
        public const string RequiredAuthoringOutputRoot =
            "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/VIS01";
        public const string RequiredGeneratedOutputRoot = "MapDesign/MCP/GENERATED/VIS01";

        private MoonPalaceGrayboxRunRequest(MoonPalaceGrayboxRunMode mode, string seedId, int seedValue,
            int sectorX, int sectorY, int candidateCount, string outputScenePath, string authoringOutputRoot,
            string generatedOutputRoot)
        {
            Mode = mode;
            SeedId = seedId ?? string.Empty;
            SeedValue = seedValue;
            SectorX = sectorX;
            SectorY = sectorY;
            CandidateCount = candidateCount;
            OutputScenePath = outputScenePath ?? string.Empty;
            AuthoringOutputRoot = authoringOutputRoot ?? string.Empty;
            GeneratedOutputRoot = generatedOutputRoot ?? string.Empty;
            RequestDigest = ComputeRequestDigest();
        }

        public MoonPalaceGrayboxRunMode Mode { get; }
        public string SeedId { get; }
        public int SeedValue { get; }
        public int SectorX { get; }
        public int SectorY { get; }
        public int CandidateCount { get; }
        public string OutputScenePath { get; }
        public string AuthoringOutputRoot { get; }
        public string GeneratedOutputRoot { get; }
        public string RequestDigest { get; }

        public static MoonPalaceGrayboxRunRequest CreateDefault(MoonPalaceGrayboxRunMode mode)
        {
            return new MoonPalaceGrayboxRunRequest(mode, RequiredSeedId, RequiredSeedValue,
                RequiredSectorX, RequiredSectorY, RequiredCandidateCount, RequiredOutputScenePath,
                RequiredAuthoringOutputRoot, RequiredGeneratedOutputRoot);
        }

        public void Validate()
        {
            if (SeedId != RequiredSeedId || SeedValue != RequiredSeedValue)
                throw new InvalidOperationException("VIS02 accepts only the audited MP_QA_01 seed.");
            if (SectorX != RequiredSectorX || SectorY != RequiredSectorY)
                throw new InvalidOperationException("VIS02 accepts only the audited one-sector coordinate 6,6.");
            if (CandidateCount != RequiredCandidateCount)
                throw new InvalidOperationException("VIS02 accepts exactly 500 structural MicroPattern candidates.");
            if (OutputScenePath != RequiredOutputScenePath || AuthoringOutputRoot != RequiredAuthoringOutputRoot ||
                GeneratedOutputRoot != RequiredGeneratedOutputRoot)
                throw new InvalidOperationException("VIS02 output roots are fixed to the isolated VIS01 contract.");
        }

        private string ComputeRequestDigest()
        {
            return BakingCanonicalDigest.HashCanonicalLines(new List<string>
            {
                "VIS02_REQUEST",
                "mode=" + Mode,
                "seed_id=" + SeedId,
                "seed_value=" + SeedValue.ToString(CultureInfo.InvariantCulture),
                "sector=" + SectorX.ToString(CultureInfo.InvariantCulture) + "," + SectorY.ToString(CultureInfo.InvariantCulture),
                "candidate_count=" + CandidateCount.ToString(CultureInfo.InvariantCulture),
                "output_scene_path=" + OutputScenePath,
                "authoring_output_root=" + AuthoringOutputRoot,
                "generated_output_root=" + GeneratedOutputRoot,
            });
        }
    }
}
