#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace StarNight.MapAuthoring.Editor
{
    public sealed class EditorSceneBuildGuard : IPreprocessBuildWithReport
    {
        public const string MapElementLabPath =
            "Assets/_Game/Editor/MapAuthoring/Scenes/00_MapElementLab.unity";

        public const string StageLayoutLabPath =
            "Assets/_Game/Editor/MapAuthoring/Scenes/01_StageLayoutLab.unity";

        public const string GeneratedPreviewFolder =
            "Assets/_Game/Editor/MapAuthoring/GeneratedPreviews/";

        private static readonly string[] ForbiddenScenePaths =
        {
            MapElementLabPath,
            StageLayoutLabPath,
        };

        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            ValidateCurrentBuildSettingsOrThrow();
        }

        public static string[] FindForbiddenScenePaths(IEnumerable<string> scenePaths)
        {
            if (scenePaths == null)
            {
                return Array.Empty<string>();
            }

            return scenePaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(NormalizePath)
                .Where(path => ForbiddenScenePaths.Any(forbidden =>
                                   string.Equals(path, forbidden, StringComparison.OrdinalIgnoreCase)) ||
                               path.StartsWith(GeneratedPreviewFolder, StringComparison.OrdinalIgnoreCase) &&
                               path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static string[] FindForbiddenScenesInCurrentBuildSettings()
        {
            return FindForbiddenScenePaths(EditorBuildSettings.scenes.Select(scene => scene.path));
        }

        public static void ValidateCurrentBuildSettingsOrThrow()
        {
            var forbiddenScenes = FindForbiddenScenesInCurrentBuildSettings();
            if (forbiddenScenes.Length == 0)
            {
                return;
            }

            throw new BuildFailedException(CreateFailureMessage(forbiddenScenes));
        }

        public static string CreateFailureMessage(IReadOnlyCollection<string> forbiddenScenes)
        {
            return "Map authoring scenes are editor-only and cannot be present in Build Settings:\n- " +
                   string.Join("\n- ", forbiddenScenes);
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/').Trim();
        }
    }
}

#endif
