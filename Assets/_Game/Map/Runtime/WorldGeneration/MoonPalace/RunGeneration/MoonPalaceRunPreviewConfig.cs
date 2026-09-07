using System;
using System.Globalization;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>Immutable RUN04 inspection-harness contract. It derives only from the RUN03 camera-room course.</summary>
    public sealed class MoonPalaceRunPreviewConfig
    {
        public const string DefaultPreviewId = "MP_RUN_PREVIEW_01";
        public const string DefaultSourceCourseId = "MP_CAMERA_RUN_01";
        public const string DefaultSourceTaskId = "RUN03_CONNECT_RUN_VARIANTS_TO_CAMERA_ROOM_TRANSITIONS";
        public const string DefaultSeedId = "MP_QA_01";
        public const int DefaultSeedValue = 1924737067;
        public const int PatternSize = 4;
        public const int DefaultPatternGridWidth = 80;
        public const int DefaultPatternGridHeight = 24;
        public const int RequiredRoomCount = 10;
        public const int RequiredConnectorCount = 9;

        private MoonPalaceRunPreviewConfig(string previewId, string sourceCourseId, string sourceTaskId, string seedId, int seedValue,
            int patternWidth, int patternHeight, int patternGridWidth, int patternGridHeight, int expectedRoomCount, int expectedConnectorCount,
            bool autoRouteGhostEnabled, bool allowSectorPrimaryInput, bool allowFallbackCarve, bool allowSilentRepair, bool allow90DegreeRotation)
        {
            PreviewId = previewId ?? string.Empty;
            SourceCourseId = sourceCourseId ?? string.Empty;
            SourceTaskId = sourceTaskId ?? string.Empty;
            SeedId = seedId ?? string.Empty;
            SeedValue = seedValue;
            PatternWidth = patternWidth;
            PatternHeight = patternHeight;
            PatternGridWidth = patternGridWidth;
            PatternGridHeight = patternGridHeight;
            ExpectedRoomCount = expectedRoomCount;
            ExpectedConnectorCount = expectedConnectorCount;
            AutoRouteGhostEnabled = autoRouteGhostEnabled;
            AllowSectorPrimaryInput = allowSectorPrimaryInput;
            AllowFallbackCarve = allowFallbackCarve;
            AllowSilentRepair = allowSilentRepair;
            Allow90DegreeRotation = allow90DegreeRotation;
            Validate();
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "RUN04_CONFIG",
                "preview_id=" + PreviewId,
                "source_course_id=" + SourceCourseId,
                "source_task=" + SourceTaskId,
                "seed_id=" + SeedId,
                "seed_value=" + SeedValue.ToString(CultureInfo.InvariantCulture),
                "pattern_size=" + PatternWidth.ToString(CultureInfo.InvariantCulture) + "x" + PatternHeight.ToString(CultureInfo.InvariantCulture),
                "pattern_grid=" + PatternGridWidth.ToString(CultureInfo.InvariantCulture) + "x" + PatternGridHeight.ToString(CultureInfo.InvariantCulture),
                "tile_grid=" + TileWidth.ToString(CultureInfo.InvariantCulture) + "x" + TileHeight.ToString(CultureInfo.InvariantCulture),
                "expected_rooms=" + ExpectedRoomCount.ToString(CultureInfo.InvariantCulture),
                "expected_connectors=" + ExpectedConnectorCount.ToString(CultureInfo.InvariantCulture),
                "movement_mode=TileStepOpenCell",
                "camera_mode=RoomBoundsSnapOrShortLerp",
                "auto_route_ghost=" + (AutoRouteGhostEnabled ? "1" : "0"),
                "sector_primary_input=" + (AllowSectorPrimaryInput ? "1" : "0"),
                "fallback_carve=" + (AllowFallbackCarve ? "1" : "0"),
                "silent_repair=" + (AllowSilentRepair ? "1" : "0"),
                "allow_90_degree_rotation=" + (Allow90DegreeRotation ? "1" : "0"),
            });
        }

        public string PreviewId { get; }
        public string SourceCourseId { get; }
        public string SourceTaskId { get; }
        public string SeedId { get; }
        public int SeedValue { get; }
        public int PatternWidth { get; }
        public int PatternHeight { get; }
        public int PatternGridWidth { get; }
        public int PatternGridHeight { get; }
        public int TileWidth => PatternGridWidth * PatternWidth;
        public int TileHeight => PatternGridHeight * PatternHeight;
        public int ExpectedRoomCount { get; }
        public int ExpectedConnectorCount { get; }
        public string MovementMode => "TileStepOpenCell";
        public string CameraMode => "RoomBoundsSnapOrShortLerp";
        public bool AutoRouteGhostEnabled { get; }
        public bool AllowSectorPrimaryInput { get; }
        public bool AllowFallbackCarve { get; }
        public bool AllowSilentRepair { get; }
        public bool Allow90DegreeRotation { get; }
        public string CanonicalDigest { get; }

        public static MoonPalaceRunPreviewConfig CreateDefault()
        {
            return Create(DefaultPreviewId, DefaultSourceCourseId, DefaultSourceTaskId, DefaultSeedId, DefaultSeedValue, PatternSize, PatternSize,
                DefaultPatternGridWidth, DefaultPatternGridHeight, RequiredRoomCount, RequiredConnectorCount, true, false, false, false, false);
        }

        public static MoonPalaceRunPreviewConfig Create(string previewId, string sourceCourseId, string sourceTaskId, string seedId, int seedValue,
            int patternWidth, int patternHeight, int patternGridWidth, int patternGridHeight, int expectedRoomCount, int expectedConnectorCount,
            bool autoRouteGhostEnabled, bool allowSectorPrimaryInput, bool allowFallbackCarve, bool allowSilentRepair, bool allow90DegreeRotation)
        {
            return new MoonPalaceRunPreviewConfig(previewId, sourceCourseId, sourceTaskId, seedId, seedValue, patternWidth, patternHeight,
                patternGridWidth, patternGridHeight, expectedRoomCount, expectedConnectorCount, autoRouteGhostEnabled, allowSectorPrimaryInput,
                allowFallbackCarve, allowSilentRepair, allow90DegreeRotation);
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(PreviewId) || SourceCourseId != MoonPalaceCameraRoomCourseConfig.DefaultCourseId || SourceTaskId != "RUN03_CONNECT_RUN_VARIANTS_TO_CAMERA_ROOM_TRANSITIONS" || string.IsNullOrWhiteSpace(SeedId))
                throw new InvalidOperationException("RUN04 accepts only the reviewed RUN03 camera-room course source.");
            if (SeedId != DefaultSeedId || SeedValue != DefaultSeedValue || PatternWidth != PatternSize || PatternHeight != PatternSize || PatternGridWidth != 80 || PatternGridHeight != 24)
                throw new InvalidOperationException("RUN04 requires MP_QA_01 and the RUN03 4x4 / 80x24 course contract.");
            if (TileWidth != PatternGridWidth * PatternWidth || TileHeight != PatternGridHeight * PatternHeight || TileWidth != 320 || TileHeight != 96)
                throw new InvalidOperationException("RUN04 tile dimensions must be derived from RUN03's 80x24 4x4 pattern grid.");
            if (ExpectedRoomCount != 10 || ExpectedConnectorCount != 9)
                throw new InvalidOperationException("RUN04 requires the reviewed ten rooms and nine connectors.");
            if (AllowSectorPrimaryInput || (TileWidth == 48 && TileHeight == 32) || AllowFallbackCarve || AllowSilentRepair || Allow90DegreeRotation)
                throw new InvalidOperationException("RUN04 rejects sector-primary input, fallback carve, silent repair, and 90-degree rotation.");
        }
    }
}
