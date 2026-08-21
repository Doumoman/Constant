#if LEGACY_DISABLED
using System;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    [InitializeOnLoad]
    public static class MapAuthoringSceneGuard
    {
        private static string lastReportedSignature = string.Empty;

        static MapAuthoringSceneGuard()
        {
            EditorBuildSettings.sceneListChanged -= ReportBuildSettingsViolation;
            EditorBuildSettings.sceneListChanged += ReportBuildSettingsViolation;
            EditorApplication.delayCall += ReportBuildSettingsViolation;
        }

        public static void ReportBuildSettingsViolation()
        {
            var forbiddenScenes = EditorSceneBuildGuard.FindForbiddenScenesInCurrentBuildSettings();
            var signature = string.Join("|", forbiddenScenes);

            if (forbiddenScenes.Length == 0)
            {
                lastReportedSignature = string.Empty;
                return;
            }

            if (string.Equals(signature, lastReportedSignature, StringComparison.Ordinal))
            {
                return;
            }

            lastReportedSignature = signature;
            Debug.LogError(EditorSceneBuildGuard.CreateFailureMessage(forbiddenScenes));
        }
    }
}

#endif
