#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using StarFetchingNight;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarFetchingNightEditor
{
    public static class StarNightM6QAAuditor
    {
        private const string ReportPath = "docs/star-night/M6_AUTOMATED_QA_REPORT.md";

        private static readonly string[] JourneyScenes =
        {
            "Assets/Scenes/StarNight/StarNight_Prologue.unity",
            "Assets/Scenes/StarNight/StarNight_MoonMill.unity",
            "Assets/Scenes/StarNight/StarNight_MagpieBridge.unity",
            "Assets/Scenes/StarNight/StarNight_CloudWhaleRanch.unity",
            "Assets/Scenes/StarNight/StarNight_StarPostOffice.unity",
            "Assets/Scenes/StarNight/StarNight_SleepingSunGarden.unity",
            "Assets/Scenes/StarNight/StarNight_PolarisObservatory.unity"
        };

        [MenuItem("Tools/Star Night/Run M6 Balance & Regression Audit")]
        public static void RunAudit()
        {
            List<AuditCheck> checks = new();
            List<SceneAudit> scenes = new();
            AuditBuildOrder(checks);
            AuditBalanceProfile(checks);

            foreach (string path in JourneyScenes)
            {
                SceneAudit scene = AuditScene(path);
                scenes.Add(scene);
                checks.Add(new AuditCheck(
                    $"씬 무결성 · {scene.name}",
                    scene.exists && scene.missingScripts == 0 && scene.cameraCount > 0 &&
                    scene.directionalLightCount > 0,
                    scene.exists
                        ? $"GameObject {scene.gameObjectCount}, Camera {scene.cameraCount}, " +
                          $"Directional Light {scene.directionalLightCount}, Missing Script {scene.missingScripts}"
                        : "씬 파일 없음"));
            }

            AuditGameplayCounts(scenes, checks);
            StarNightBalanceAggregate aggregate = StarNightTelemetryStore.Load();
            string markdown = BuildReport(checks, scenes, aggregate);
            string absolute = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ReportPath));
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? string.Empty);
            File.WriteAllText(absolute, markdown, new UTF8Encoding(false));

            int failures = checks.Count(check => !check.passed);
            Debug.Log($"[Star Night M6] QA audit {(failures == 0 ? "PASS" : "FAIL")} · " +
                      $"{checks.Count - failures}/{checks.Count} checks · {ReportPath}");
        }

        [MenuItem("Tools/Star Night/Clear M6 Playtest Aggregate")]
        public static void ClearAggregate()
        {
            if (!EditorUtility.DisplayDialog("M6 플레이테스트 표본 초기화",
                    "저장된 M6 런·경로·유혹 구역 통계를 삭제할까요?", "삭제", "취소"))
            {
                return;
            }
            StarNightTelemetryStore.Clear();
            Debug.Log("[Star Night M6] Persistent playtest aggregate cleared.");
        }

        private static void AuditBuildOrder(List<AuditCheck> checks)
        {
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            bool correct = buildScenes.Length >= JourneyScenes.Length;
            for (int i = 0; i < JourneyScenes.Length && correct; i++)
            {
                correct = buildScenes[i].enabled && buildScenes[i].path == JourneyScenes[i];
            }
            checks.Add(new AuditCheck("전체 여정 빌드 순서", correct,
                string.Join(" → ", JourneyScenes.Select(Path.GetFileNameWithoutExtension))));
        }

        private static void AuditBalanceProfile(List<AuditCheck> checks)
        {
            checks.Add(new AuditCheck("일반 엔딩 시간 목표", true,
                $"{StarNightBalanceProfile.GeneralRunMinimumMinutes:0}~" +
                $"{StarNightBalanceProfile.GeneralRunMaximumMinutes:0}분"));
            checks.Add(new AuditCheck("별길 엔딩 시간 목표", true,
                $"{StarNightBalanceProfile.StarRoadMinimumMinutes:0}~" +
                $"{StarNightBalanceProfile.StarRoadMaximumMinutes:0}분"));
            checks.Add(new AuditCheck("챕터 시간 예산 7구간",
                StarNightBalanceProfile.ChapterTargets.Count == 7,
                string.Join(", ", StarNightBalanceProfile.ChapterTargets.Select(target =>
                    $"{target.chapter} {target.minimumMinutes:0}~{target.maximumMinutes:0}m"))));
            checks.Add(new AuditCheck("방울 Alert 곡선",
                Mathf.Approximately(StarGateAlertRules.SecondBellThreshold, 30f) &&
                Mathf.Approximately(StarGateAlertRules.ThirdBellThreshold, 60f) &&
                Mathf.Approximately(StarGateAlertRules.SecondsToSecondBellWithoutActions, 90f) &&
                Mathf.Approximately(StarGateAlertRules.SecondsToThirdBellWithoutActions, 180f),
                $"GateActive 즉시 1차 → {StarGateAlertRules.SecondsToSecondBellWithoutActions:0}s 2차 → " +
                $"{StarGateAlertRules.SecondsToThirdBellWithoutActions:0}s 3차"));
            checks.Add(new AuditCheck("일반/별길 정보량 분리",
                StarNightBalanceProfile.GeneralEndingInformationUnits == 3 &&
                StarNightBalanceProfile.StarRoadInformationUnits == 7,
                $"일반 {StarNightBalanceProfile.GeneralEndingInformationUnits}, " +
                $"별길 {StarNightBalanceProfile.StarRoadInformationUnits} 정보 단위"));

            foreach (ChapterBalanceTarget target in StarNightBalanceProfile.ChapterTargets
                         .Where(target => target.gateLoopChapter))
            {
                checks.Add(new AuditCheck($"{target.chapter} Route A/B/C 프로파일",
                    target.routeIds.Count == 3 && target.routeIds.Distinct().Count() == 3,
                    string.Join(", ", target.routeIds)));
            }
        }

        private static SceneAudit AuditScene(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                return new SceneAudit { path = path, name = Path.GetFileNameWithoutExtension(path) };
            }

            Scene scene = SceneManager.GetSceneByPath(path);
            bool openedForAudit = !scene.IsValid() || !scene.isLoaded;
            if (openedForAudit)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            List<GameObject> gameObjects = new();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                gameObjects.AddRange(root.GetComponentsInChildren<Transform>(true)
                    .Select(transform => transform.gameObject));
            }

            SceneAudit audit = new()
            {
                path = path,
                name = scene.name,
                exists = true,
                gameObjectCount = gameObjects.Count,
                missingScripts = gameObjects.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount),
                cameraCount = gameObjects.Sum(gameObject => gameObject.GetComponents<Camera>().Length),
                directionalLightCount = gameObjects.Sum(gameObject =>
                    gameObject.GetComponents<Light>().Count(light => light.type == LightType.Directional)),
                gateRoutes = gameObjects.Sum(gameObject => gameObject.GetComponents<GateRouteObjective>().Length),
                telemetry = gameObjects.Sum(gameObject =>
                    gameObject.GetComponents<ChapterPlaytestTelemetry>().Length),
                prologueBeats = gameObjects.Sum(gameObject =>
                    gameObject.GetComponents<PrologueJourneyBeat>().Length),
                polarisRecords = gameObjects.Sum(gameObject =>
                    gameObject.GetComponents<PolarisRecordEcho>().Length),
                polarisTools = gameObjects.Sum(gameObject =>
                    gameObject.GetComponents<PolarisFinalToolNode>().Length),
                polarisEndings = gameObjects.Sum(gameObject =>
                    gameObject.GetComponents<PolarisEndingChoice>().Length)
            };

            if (openedForAudit)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
            return audit;
        }

        private static void AuditGameplayCounts(List<SceneAudit> scenes, List<AuditCheck> checks)
        {
            SceneAudit prologue = scenes.FirstOrDefault(scene =>
                scene.path.EndsWith("StarNight_Prologue.unity", StringComparison.Ordinal));
            checks.Add(new AuditCheck("프롤로그 필수 사건",
                prologue != null && prologue.prologueBeats >= 6,
                $"PrologueJourneyBeat {prologue?.prologueBeats ?? 0}/6"));

            foreach (SceneAudit scene in scenes.Where(scene =>
                         scene.path.Contains("MoonMill") ||
                         scene.path.Contains("MagpieBridge") ||
                         scene.path.Contains("CloudWhaleRanch") ||
                         scene.path.Contains("StarPostOffice") ||
                         scene.path.Contains("SleepingSunGarden")))
            {
                checks.Add(new AuditCheck($"공통 별문 루프 · {scene.name}",
                    scene.gateRoutes >= 3 && scene.telemetry >= 1,
                    $"Route objective {scene.gateRoutes}/3, Chapter telemetry {scene.telemetry}/1"));
            }

            SceneAudit polaris = scenes.FirstOrDefault(scene =>
                scene.path.EndsWith("StarNight_PolarisObservatory.unity", StringComparison.Ordinal));
            checks.Add(new AuditCheck("북극성 최종전 상호작용",
                polaris != null && polaris.polarisRecords == 5 &&
                polaris.polarisTools == 5 && polaris.polarisEndings == 4,
                $"기록 {polaris?.polarisRecords ?? 0}/5, 도구 {polaris?.polarisTools ?? 0}/5, " +
                $"엔딩 {polaris?.polarisEndings ?? 0}/4"));
        }

        private static string BuildReport(IReadOnlyList<AuditCheck> checks,
            IReadOnlyList<SceneAudit> scenes, StarNightBalanceAggregate aggregate)
        {
            int failures = checks.Count(check => !check.passed);
            StringBuilder builder = new();
            builder.AppendLine("# M6 자동 밸런스·회귀 QA 리포트");
            builder.AppendLine();
            builder.AppendLine($"- 구조 감사: **{(failures == 0 ? "PASS" : "FAIL")}** " +
                               $"({checks.Count - failures}/{checks.Count})");
            builder.AppendLine($"- 누적 플레이 표본: **{aggregate.totalRuns}런**");
            builder.AppendLine("- 이 리포트의 구조 검증은 사람 플레이테스트의 이해도·체감 표본을 대신하지 않는다.");
            builder.AppendLine();

            builder.AppendLine("## 자동 구조 감사");
            builder.AppendLine();
            builder.AppendLine("| 결과 | 항목 | 근거 |");
            builder.AppendLine("|---|---|---|");
            foreach (AuditCheck check in checks)
            {
                builder.AppendLine($"| {(check.passed ? "PASS" : "FAIL")} | {check.name} | {check.detail} |");
            }
            builder.AppendLine();

            builder.AppendLine("## 전체 런 밸런스 표본");
            builder.AppendLine();
            builder.AppendLine($"- {aggregate.BuildTechnicalReport()}");
            builder.AppendLine($"- 유혹 구역 목표: {StarNightBalanceProfile.TemptationRateMinimum:P0}~" +
                               $"{StarNightBalanceProfile.TemptationRateMaximum:P0}");
            builder.AppendLine($"- 일반 엔딩 목표: {StarNightBalanceProfile.GeneralRunMinimumMinutes:0}~" +
                               $"{StarNightBalanceProfile.GeneralRunMaximumMinutes:0}분");
            builder.AppendLine($"- 별길 엔딩 목표: {StarNightBalanceProfile.StarRoadMinimumMinutes:0}~" +
                               $"{StarNightBalanceProfile.StarRoadMaximumMinutes:0}분");
            builder.AppendLine();

            builder.AppendLine("### Route 선택률");
            builder.AppendLine();
            builder.AppendLine("| 챕터 | Route | 선택 횟수 | 챕터 내 비율 | 판정 |");
            builder.AppendLine("|---|---|---:|---:|---|");
            foreach (ChapterBalanceTarget target in StarNightBalanceProfile.ChapterTargets
                         .Where(target => target.gateLoopChapter))
            {
                foreach (string routeId in target.routeIds)
                {
                    int count = aggregate.routeSelections
                        .Where(stat => stat.chapter == target.chapter && stat.routeId == routeId)
                        .Sum(stat => stat.selectedCount);
                    float share = aggregate.GetRouteShare(target.chapter, routeId);
                    string status = count == 0 && aggregate.totalRuns == 0
                        ? "표본 대기"
                        : share < 0.1f ? "10% 미만" : "정상";
                    builder.AppendLine($"| {target.chapter} | {routeId} | {count} | {share:P0} | {status} |");
                }
            }
            builder.AppendLine();

            builder.AppendLine("## 씬 규모");
            builder.AppendLine();
            builder.AppendLine("| 씬 | GameObject | Camera | Directional Light | Missing Script |");
            builder.AppendLine("|---|---:|---:|---:|---:|");
            foreach (SceneAudit scene in scenes)
            {
                builder.AppendLine($"| {scene.name} | {scene.gameObjectCount} | {scene.cameraCount} | " +
                                   $"{scene.directionalLightCount} | {scene.missingScripts} |");
            }
            builder.AppendLine();

            builder.AppendLine("## 수동 확인 잔여 항목");
            builder.AppendLine();
            builder.AppendLine("- 일반 엔딩 5세션과 별길 엔딩 5세션의 실제 시간 분포");
            builder.AppendLine("- 한 Route의 선택률이 10% 미만인지 여부");
            builder.AppendLine("- 유혹 구역 진입률이 55~70%인지 여부");
            builder.AppendLine("- 강제 귀가 원인 이해도와 라니·마루 관계 해석");
            return builder.ToString();
        }

        private sealed class AuditCheck
        {
            public readonly string name;
            public readonly bool passed;
            public readonly string detail;

            public AuditCheck(string name, bool passed, string detail)
            {
                this.name = name;
                this.passed = passed;
                this.detail = detail;
            }
        }

        private sealed class SceneAudit
        {
            public string path;
            public string name;
            public bool exists;
            public int gameObjectCount;
            public int missingScripts;
            public int cameraCount;
            public int directionalLightCount;
            public int gateRoutes;
            public int telemetry;
            public int prologueBeats;
            public int polarisRecords;
            public int polarisTools;
            public int polarisEndings;
        }
    }
}
#endif
