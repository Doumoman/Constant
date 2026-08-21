#if LEGACY_DISABLED
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class MapAuthoringMenu
    {
        private const string BuildCommonCatalogMenu =
            "Tools/Star Night/Map E05/Build Common Element Catalog";
        private const string BuildMaruCatalogMenu =
            "Tools/Star Night/Map E06/Build Maru Element Catalog";
        private const string BuildMoonCatalogMenu =
            "Tools/Star Night/Map E07/Build Moon Element Catalog";
        private const string BuildBridgeCatalogMenu =
            "Tools/Star Night/Map E07/Build Bridge Element Catalog";
        private const string BuildPalaceCatalogMenu =
            "Tools/Star Night/Map E07/Build Palace Element Catalog";
        private const string BuildPostCatalogMenu =
            "Tools/Star Night/Map E07/Build Post Element Catalog";
        private const string BuildSunCatalogMenu =
            "Tools/Star Night/Map E07/Build Sun Element Catalog";
        private const string BuildPolarisCatalogMenu =
            "Tools/Star Night/Map E07/Build Polaris Element Catalog";
        private const string OpenMapElementLabMenu =
            "Tools/별을 물어오는 밤/Map Element Lab 열기";

        private const string RebuildMapElementLabMenu =
            "Tools/별을 물어오는 밤/Map Element Lab 재생성";

        private const string ValidateBuildSettingsMenu =
            "Tools/별을 물어오는 밤/Build Settings의 Editor Scene 검사";

        private const string ValidateSelectedElementMenu =
            "Tools/별을 물어오는 밤/선택한 Map Element 검증";

        private const string ValidateAllMapDataMenu =
            "Tools/별을 물어오는 밤/전체 Map Data 재검증";

        [MenuItem(OpenMapElementLabMenu, priority = 100)]
        private static void OpenMapElementLab()
        {
            MapElementLabBuilder.OpenOrCreateLab();
        }

        [MenuItem(BuildCommonCatalogMenu, priority = 90)]
        private static void BuildCommonCatalog()
        {
            var report = CommonElementCatalogFactory.BakeCatalog(overwriteExisting: true);
            if (report.Success)
            {
                Debug.Log($"[MAP-E05] Common catalog bake complete: {report.SuccessCount}/{report.Results.Count}");
            }
            else
            {
                Debug.LogError($"[MAP-E05] Common catalog bake failed: {report.SuccessCount}/{report.Results.Count}");
                for (var index = 0; index < report.Results.Count; index++)
                {
                    var result = report.Results[index];
                    if (result.Success)
                    {
                        continue;
                    }

                    var id = index < report.Definitions.Count && report.Definitions[index] != null
                        ? report.Definitions[index].ElementId
                        : $"Index {index}";
                    var issues = result.Validation != null
                        ? string.Join(" | ", result.Validation.Issues
                            .Where(issue => issue.Severity == ValidationSeverity.Error)
                            .Select(issue => issue.ToString()))
                        : result.Message;
                    Debug.LogError($"[MAP-E05] {id}: {issues}");
                }
            }
        }

        [MenuItem(BuildMaruCatalogMenu, priority = 91)]
        private static void BuildMaruCatalog()
        {
            var report = MaruElementCatalogFactory.BakeCatalog();
            if (report.Success)
            {
                var galleryCount = MapElementLabBuilder.RefreshMaruElementGallery();
                Debug.Log($"[MAP-E06] Maru catalog bake complete: {report.SuccessCount}/{report.Results.Count}, Lab {galleryCount}");
            }
            else
            {
                Debug.LogError($"[MAP-E06] Maru catalog bake failed: {report.SuccessCount}/{report.Results.Count}");
            }
        }

        [MenuItem(BuildMoonCatalogMenu, priority = 92)]
        private static void BuildMoonCatalog()
        {
            var report = MoonElementCatalogFactory.BakeCatalog(overwriteExisting: true);
            if (report.Success)
            {
                var galleryCount = MapElementLabBuilder.RefreshMoonElementGallery();
                Debug.Log($"[MAP-E07/Moon] Moon catalog bake complete: {report.SuccessCount}/{report.Results.Count}, Lab {galleryCount}");
            }
            else
            {
                Debug.LogError($"[MAP-E07/Moon] Moon catalog bake failed: {report.SuccessCount}/{report.Results.Count}");
            }
        }

        [MenuItem(BuildBridgeCatalogMenu, priority = 93)]
        private static void BuildBridgeCatalog()
        {
            var report = BridgeElementCatalogFactory.BakeCatalog(overwriteExisting: true);
            if (report.Success)
            {
                var galleryCount = MapElementLabBuilder.RefreshBridgeElementGallery();
                Debug.Log($"[MAP-E07/Bridge] Bridge catalog bake complete: {report.SuccessCount}/{report.Results.Count}, Lab {galleryCount}");
            }
            else
            {
                Debug.LogError($"[MAP-E07/Bridge] Bridge catalog bake failed: {report.SuccessCount}/{report.Results.Count}");
            }
        }

        [MenuItem(BuildPalaceCatalogMenu, priority = 94)]
        private static void BuildPalaceCatalog()
        {
            var report = PalaceElementCatalogFactory.BakeCatalog(overwriteExisting: true);
            if (report.Success)
            {
                var galleryCount = MapElementLabBuilder.RefreshPalaceElementGallery();
                Debug.Log($"[MAP-E07/Palace] Palace catalog bake complete: {report.SuccessCount}/{report.Results.Count}, Lab {galleryCount}");
            }
            else
            {
                Debug.LogError($"[MAP-E07/Palace] Palace catalog bake failed: {report.SuccessCount}/{report.Results.Count}");
            }
        }

        [MenuItem(BuildPostCatalogMenu, priority = 95)]
        private static void BuildPostCatalog()
        {
            var report = PostElementCatalogFactory.BakeCatalog(overwriteExisting: true);
            if (report.Success)
            {
                var galleryCount = MapElementLabBuilder.RefreshPostElementGallery();
                Debug.Log($"[MAP-E07/Post] Post catalog bake complete: {report.SuccessCount}/{report.Results.Count}, Lab {galleryCount}");
            }
            else
            {
                Debug.LogError($"[MAP-E07/Post] Post catalog bake failed: {report.SuccessCount}/{report.Results.Count}");
            }
        }

        [MenuItem(BuildSunCatalogMenu, priority = 96)]
        private static void BuildSunCatalog()
        {
            var report = SunElementCatalogFactory.BakeCatalog(overwriteExisting: true);
            if (report.Success)
            {
                var galleryCount = MapElementLabBuilder.RefreshSunElementGallery();
                Debug.Log($"[MAP-E07/Sun] Sun catalog bake complete: {report.SuccessCount}/{report.Results.Count}, Lab {galleryCount}");
            }
            else
            {
                Debug.LogError($"[MAP-E07/Sun] Sun catalog bake failed: {report.SuccessCount}/{report.Results.Count}");
            }
        }

        [MenuItem(BuildPolarisCatalogMenu, priority = 97)]
        private static void BuildPolarisCatalog()
        {
            var report = PolarisElementCatalogFactory.BakeCatalog(overwriteExisting: true);
            if (report.Success)
            {
                var galleryCount = MapElementLabBuilder.RefreshPolarisElementGallery();
                Debug.Log($"[MAP-E07/Polaris] Polaris catalog bake complete: {report.SuccessCount}/{report.Results.Count}, Lab {galleryCount}");
            }
            else
            {
                Debug.LogError($"[MAP-E07/Polaris] Polaris catalog bake failed: {report.SuccessCount}/{report.Results.Count}");
            }
        }

        [MenuItem(RebuildMapElementLabMenu, priority = 101)]
        private static void RebuildMapElementLab()
        {
            MapElementLabBuilder.RebuildLabScene();
        }

        [MenuItem(ValidateSelectedElementMenu, priority = 200)]
        private static void ValidateSelectedElement()
        {
            var definition = Selection.activeObject as StarNight.Map.MapElementDefinition ??
                             MapElementAuthoringSession.SelectedDefinition;
            var sourceRoot = GameObject.Find("ActiveAuthoringElement");
            var report = MapElementValidator.ValidateSourceForBake(definition, sourceRoot);
            ValidationReportWindow.ShowReport(report);
            Debug.Log($"[MAP-E04] {report.CreateSummary()}");
        }

        [MenuItem(ValidateSelectedElementMenu, true)]
        private static bool CanValidateSelectedElement()
        {
            return Selection.activeObject is StarNight.Map.MapElementDefinition ||
                   MapElementAuthoringSession.SelectedDefinition != null;
        }

        [MenuItem(ValidateAllMapDataMenu, priority = 201)]
        private static void ValidateAllMapData()
        {
            var report = MapElementValidator.ValidateAllDefinitions();
            ValidationReportWindow.ShowReport(report);
            Debug.Log($"[MAP-E04] {report.CreateSummary()}");
        }

        [MenuItem(ValidateBuildSettingsMenu, priority = 500)]
        private static void ValidateBuildSettings()
        {
            var forbiddenScenes = EditorSceneBuildGuard.FindForbiddenScenesInCurrentBuildSettings();
            if (forbiddenScenes.Length == 0)
            {
                Debug.Log("[MAP-E00] Build Settings 검사 통과: Editor 전용 Lab 씬이 없습니다.");
                return;
            }

            Debug.LogError(EditorSceneBuildGuard.CreateFailureMessage(forbiddenScenes));
        }
    }
}

#endif
