using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>Immutable RUN03 input. All tile dimensions derive from 4x4 pattern coordinates.</summary>
    public sealed class MoonPalaceCameraRoomCourseConfig
    {
        public const int PatternSize = 4;
        public const int RequiredCandidateCount = 500;
        public const string DefaultCourseId = "MP_CAMERA_RUN_01";
        public const string DefaultSeedId = "MP_QA_01";
        public const int DefaultSeedValue = 1924737067;

        private MoonPalaceCameraRoomCourseConfig(string courseId, string seedId, int seedValue, int gridWidth, int gridHeight,
            int roomMin, int roomMax, int mainMin, int mainMax, int branchMin, int branchMax, int splitMin, int splitMax,
            bool allow90DegreeRotation, bool allowSilentCarve)
        {
            CourseId = courseId ?? string.Empty;
            SeedId = seedId ?? string.Empty;
            SeedValue = seedValue;
            PatternWidth = PatternSize;
            PatternHeight = PatternSize;
            CoursePatternGridWidth = gridWidth;
            CoursePatternGridHeight = gridHeight;
            CameraRoomMinCount = roomMin;
            CameraRoomMaxCount = roomMax;
            RequiredMainRoomMinCount = mainMin;
            RequiredMainRoomMaxCount = mainMax;
            OptionalBranchRoomMinCount = branchMin;
            OptionalBranchRoomMaxCount = branchMax;
            SplitRejoinMinCount = splitMin;
            SplitRejoinMaxCount = splitMax;
            Allow90DegreeRotation = allow90DegreeRotation;
            AllowSilentCarve = allowSilentCarve;
            Validate();
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "RUN03_CONFIG",
                "course_id=" + CourseId,
                "seed_id=" + SeedId,
                "seed_value=" + SeedValue.ToString(CultureInfo.InvariantCulture),
                "pattern_size=" + PatternWidth.ToString(CultureInfo.InvariantCulture) + "x" + PatternHeight.ToString(CultureInfo.InvariantCulture),
                "course_pattern_grid=" + CoursePatternGridWidth.ToString(CultureInfo.InvariantCulture) + "x" + CoursePatternGridHeight.ToString(CultureInfo.InvariantCulture),
                "course_tile_size=" + TileWidth.ToString(CultureInfo.InvariantCulture) + "x" + TileHeight.ToString(CultureInfo.InvariantCulture),
                "candidate_count=" + RequiredCandidateCount.ToString(CultureInfo.InvariantCulture),
                "camera_rooms=" + CameraRoomMinCount.ToString(CultureInfo.InvariantCulture) + ".." + CameraRoomMaxCount.ToString(CultureInfo.InvariantCulture),
                "main_rooms=" + RequiredMainRoomMinCount.ToString(CultureInfo.InvariantCulture) + ".." + RequiredMainRoomMaxCount.ToString(CultureInfo.InvariantCulture),
                "branch_rooms=" + OptionalBranchRoomMinCount.ToString(CultureInfo.InvariantCulture) + ".." + OptionalBranchRoomMaxCount.ToString(CultureInfo.InvariantCulture),
                "split_rejoin=" + SplitRejoinMinCount.ToString(CultureInfo.InvariantCulture) + ".." + SplitRejoinMaxCount.ToString(CultureInfo.InvariantCulture),
                "allow_90_degree_rotation=" + (Allow90DegreeRotation ? "1" : "0"),
                "allow_silent_carve=" + (AllowSilentCarve ? "1" : "0"),
            });
        }

        public string CourseId { get; }
        public string SeedId { get; }
        public int SeedValue { get; }
        public int PatternWidth { get; }
        public int PatternHeight { get; }
        public int CoursePatternGridWidth { get; }
        public int CoursePatternGridHeight { get; }
        public int TileWidth => CoursePatternGridWidth * PatternWidth;
        public int TileHeight => CoursePatternGridHeight * PatternHeight;
        public int CandidateCount => RequiredCandidateCount;
        public int CameraRoomMinCount { get; }
        public int CameraRoomMaxCount { get; }
        public int RequiredMainRoomMinCount { get; }
        public int RequiredMainRoomMaxCount { get; }
        public int OptionalBranchRoomMinCount { get; }
        public int OptionalBranchRoomMaxCount { get; }
        public int SplitRejoinMinCount { get; }
        public int SplitRejoinMaxCount { get; }
        public bool Allow90DegreeRotation { get; }
        public bool AllowSilentCarve { get; }
        public string CanonicalDigest { get; }

        public static MoonPalaceCameraRoomCourseConfig CreateDefault()
        {
            return Create(DefaultCourseId, DefaultSeedId, DefaultSeedValue, 80, 24, 9, 12, 7, 9, 2, 4, 2, 3, false, false);
        }

        public static MoonPalaceCameraRoomCourseConfig Create(string courseId, string seedId, int seedValue, int gridWidth, int gridHeight,
            int roomMin, int roomMax, int mainMin, int mainMax, int branchMin, int branchMax, int splitMin, int splitMax,
            bool allow90DegreeRotation, bool allowSilentCarve)
        {
            return new MoonPalaceCameraRoomCourseConfig(courseId, seedId, seedValue, gridWidth, gridHeight,
                roomMin, roomMax, mainMin, mainMax, branchMin, branchMax, splitMin, splitMax,
                allow90DegreeRotation, allowSilentCarve);
        }

        public static IReadOnlyList<MoonPalaceCameraRoomCourseConfig> CreateDefaults()
        {
            var defaults = new[] { CreateDefault() };
            if (defaults.Select(item => item.CourseId).Distinct(StringComparer.Ordinal).Count() != defaults.Length)
                throw new InvalidOperationException("RUN03 rejects duplicate course IDs.");
            return new ReadOnlyCollection<MoonPalaceCameraRoomCourseConfig>(defaults);
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(CourseId) || string.IsNullOrWhiteSpace(SeedId))
                throw new InvalidOperationException("RUN03 requires non-empty course and seed identifiers.");
            if (PatternWidth != PatternSize || PatternHeight != PatternSize)
                throw new InvalidOperationException("RUN03 directly composes only 4x4 MicroPatterns.");
            if (CoursePatternGridWidth != 80 || CoursePatternGridHeight != 24)
                throw new InvalidOperationException("RUN03 requires its direct 80x24 camera-room course pattern grid.");
            if (TileWidth != CoursePatternGridWidth * 4 || TileHeight != CoursePatternGridHeight * 4)
                throw new InvalidOperationException("RUN03 tile size must be pattern_grid_size * 4.");
            if (TileWidth == 48 && TileHeight == 32)
                throw new InvalidOperationException("RUN03 rejects a copied 48x32 sector size.");
            if (CameraRoomMinCount != 9 || CameraRoomMaxCount != 12 || RequiredMainRoomMinCount != 7 || RequiredMainRoomMaxCount != 9 ||
                OptionalBranchRoomMinCount != 2 || OptionalBranchRoomMaxCount != 4 || SplitRejoinMinCount != 2 || SplitRejoinMaxCount != 3)
                throw new InvalidOperationException("RUN03 requires its reviewed camera-room/main/branch/split count ranges.");
            if (Allow90DegreeRotation)
                throw new InvalidOperationException("RUN03 forbids 90-degree MicroPattern rotation.");
            if (AllowSilentCarve)
                throw new InvalidOperationException("RUN03 forbids fallback carve and silent repair.");
        }

        public void ValidateRoomBounds(IEnumerable<CameraRoomBoundsPattern> rooms)
        {
            var values = (rooms ?? Array.Empty<CameraRoomBoundsPattern>()).ToList();
            if (values.Any(room => room == null || !room.IsInside(CoursePatternGridWidth, CoursePatternGridHeight)))
                throw new InvalidOperationException("RUN03 rejects room bounds outside the direct course grid.");
            if (values.Select(room => room.RoomId).Distinct(StringComparer.Ordinal).Count() != values.Count)
                throw new InvalidOperationException("RUN03 rejects duplicate camera-room identifiers.");
            for (var left = 0; left < values.Count; left++)
            for (var right = left + 1; right < values.Count; right++)
                if (values[left].Overlaps(values[right]))
                    throw new InvalidOperationException("RUN03 rejects overlapping room bounds unless an explicit shared connector band exists.");
        }
    }
}
