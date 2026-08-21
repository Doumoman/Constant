#if LEGACY_DISABLED
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using StarNight.Interaction.Input;
using StarNight.Player.Motor;
using StarNight.Player.Safety;
using StarNight.Stage.Lab;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.ToolAuthoring
{
    [DisallowMultipleComponent]
    public sealed class Tool13AcceptanceRunner : MonoBehaviour
    {
        private const int WarmupFrameCount = 120;
        private const int RecorderSettleFrameCount = 30;
        private const int SampleFrameCount = 300;
        private const double CpuFrameBudgetMilliseconds = 8d;
        private const long TransitionAllocationBudgetBytes = 1024L;
        private const double MinimumNonTransitionFps = 50d;
        private const int MaximumCommonAi = 8;
        private const string PerformanceReportFileName = "TOOL-13_PerformanceReport.json";
        private const string ManualReportFileName = "TOOL-13_ManualReport.json";

        private static readonly string[] ManualScenarios =
        {
            "A  Parcel: receiver X / field X / down+X safety",
            "B  Carry: pickup, upward throw, chain hit, portal support",
            "C  Rope: ceiling/open placement, StarKnot, heavy restriction, revisit",
            "D  Pickaxe: empty swing cost 0, valid hit cost 1, repick repair",
            "E  Hook: player pull, object pull, wall stop",
            "F  Umbrella: fall, wind, projectile reflect, laser pass-through",
            "G  Portal: crate/tool/bomb round-trip, hook block, residual bomb, revisit",
        };

        private readonly ManualCheckResult[] manualResults = new ManualCheckResult[ManualScenarios.Length];
        private Vector2 manualScroll;
        private bool performanceRunning;
        private string statusMessage = "Run each scenario, then record PASS or FAIL.";
        private Tool13PerformanceReport lastPerformanceReport;

        private enum ManualCheckResult
        {
            Pending,
            Passed,
            Failed,
        }

        [Serializable]
        private sealed class Tool13ManualReport
        {
            public string BuildTag;
            public string CapturedAtUtc;
            public string[] Scenarios;
            public string[] Results;
            public bool Passed;
        }

        [Serializable]
        public sealed class Tool13PerformanceReport
        {
            public string BuildTag;
            public string CapturedAtUtc;
            public string UnityVersion;
            public bool DevelopmentBuild;
            public int RequestedWidth;
            public int RequestedHeight;
            public int ActualWidth;
            public int ActualHeight;
            public int WarmupFrames;
            public int SampleFrames;
            public int ValidCpuSamples;
            public bool CpuTimingAvailable;
            public double AverageCpuMainThreadMs;
            public double P95CpuMainThreadMs;
            public double MaximumCpuMainThreadMs;
            public double MinimumObservedNonTransitionFps;
            public long MaximumTransitionGcAllocatedBytes;
            public bool ForwardTransitionCompleted;
            public bool BackwardTransitionCompleted;
            public int ActiveCommonAiCount;
            public bool ResolutionPassed;
            public bool CpuFramePassed;
            public bool TransitionGcPassed;
            public bool NonTransitionFpsPassed;
            public bool CommonAiPassed;
            public bool Passed;
            public string ReportPath;
        }

        private IEnumerator Start()
        {
            if (!Application.isEditor && Debug.isDebugBuild)
            {
                yield return RunPerformanceCapture(true);
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || performanceRunning)
            {
                return;
            }

            float width = Mathf.Min(650f, Screen.width - 32f);
            float height = Mathf.Min(430f, Screen.height - 32f);
            GUILayout.BeginArea(new Rect(Screen.width - width - 16f, 16f, width, height), GUI.skin.box);
            GUILayout.Label("TOOL-13  A-G Manual Approval");
            GUILayout.Label(statusMessage);
            manualScroll = GUILayout.BeginScrollView(manualScroll);
            for (int index = 0; index < ManualScenarios.Length; index++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(ManualScenarios[index], GUILayout.Width(width - 225f));
                if (GUILayout.Button("PASS", GUILayout.Width(55f))) manualResults[index] = ManualCheckResult.Passed;
                if (GUILayout.Button("FAIL", GUILayout.Width(55f))) manualResults[index] = ManualCheckResult.Failed;
                if (GUILayout.Button("RESET", GUILayout.Width(60f))) manualResults[index] = ManualCheckResult.Pending;
                GUILayout.Label(manualResults[index].ToString(), GUILayout.Width(60f));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Export manual result")) ExportManualReport();
            if (GUILayout.Button("Run one performance capture")) StartCoroutine(RunPerformanceCapture(false));
            GUILayout.EndHorizontal();
            if (lastPerformanceReport != null)
            {
                GUILayout.Label(
                    $"PERF {(lastPerformanceReport.Passed ? "PASS" : "FAIL")} | " +
                    $"CPU p95 {lastPerformanceReport.P95CpuMainThreadMs:F3}ms | " +
                    $"GC {lastPerformanceReport.MaximumTransitionGcAllocatedBytes}B | " +
                    $"min {lastPerformanceReport.MinimumObservedNonTransitionFps:F1}fps");
            }
            GUILayout.EndArea();
        }

        private void ExportManualReport()
        {
            var report = new Tool13ManualReport
            {
                BuildTag = "TOOL-13",
                CapturedAtUtc = DateTime.UtcNow.ToString("O"),
                Scenarios = (string[])ManualScenarios.Clone(),
                Results = new string[manualResults.Length],
                Passed = true,
            };
            for (int index = 0; index < manualResults.Length; index++)
            {
                report.Results[index] = manualResults[index].ToString();
                report.Passed &= manualResults[index] == ManualCheckResult.Passed;
            }

            string path = GetReportPath(ManualReportFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Application.persistentDataPath);
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            statusMessage = $"Manual report {(report.Passed ? "PASS" : "INCOMPLETE/FAIL")}: {path}";
            Debug.Log(statusMessage, this);
        }

        private IEnumerator RunPerformanceCapture(bool quitWhenComplete)
        {
            if (performanceRunning)
            {
                yield break;
            }

            performanceRunning = true;
            statusMessage = "Performance capture is running.";
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            DisableToolLabSiblings();

            Core04TwoRoomLab lab = CreatePerformanceLab();
            yield return null;
            yield return new WaitForFixedUpdate();

            var waitForFrame = new WaitForEndOfFrame();
            for (int index = 0; index < WarmupFrameCount; index++)
            {
                FrameTimingManager.CaptureFrameTimings();
                yield return waitForFrame;
            }

            // Prime transition-owned caches before the measured round trip.
            lab.TransitionController.CommitImmediate(lab.PortalAtoB);
            lab.TransitionController.CommitImmediate(lab.PortalBtoA);
            yield return null;

            var cpuSamples = new List<double>(SampleFrameCount);
            double minimumFps = double.MaxValue;
            var timing = new FrameTiming[1];
            using (ProfilerRecorder mainThreadRecorder = ProfilerRecorder.StartNew(
                       ProfilerCategory.Internal,
                       "Main Thread",
                       1))
            {
                for (int index = 0; index < RecorderSettleFrameCount; index++)
                {
                    yield return waitForFrame;
                }

                for (int index = 0; index < SampleFrameCount; index++)
                {
                    FrameTimingManager.CaptureFrameTimings();
                    yield return waitForFrame;

                    double fps = Time.unscaledDeltaTime > 0f ? 1d / Time.unscaledDeltaTime : double.MaxValue;
                    minimumFps = Math.Min(minimumFps, fps);

                    uint timingCount = FrameTimingManager.GetLatestTimings(1, timing);
                    double cpuMilliseconds = timingCount > 0
                        ? Math.Max(0d, timing[0].cpuMainThreadFrameTime - timing[0].cpuMainThreadPresentWaitTime)
                        : 0d;
                    if (cpuMilliseconds <= 0d && mainThreadRecorder.Valid)
                    {
                        cpuMilliseconds = mainThreadRecorder.LastValue / 1_000_000d;
                    }
                    if (cpuMilliseconds > 0d)
                    {
                        cpuSamples.Add(cpuMilliseconds);
                    }
                }
            }

            long beforeForward = GC.GetAllocatedBytesForCurrentThread();
            bool forwardCompleted = lab.TransitionController.CommitImmediate(lab.PortalAtoB);
            long forwardBytes = Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - beforeForward);
            long beforeBackward = GC.GetAllocatedBytesForCurrentThread();
            bool backwardCompleted = lab.TransitionController.CommitImmediate(lab.PortalBtoA);
            long backwardBytes = Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - beforeBackward);

            cpuSamples.Sort();
            double averageCpu = Average(cpuSamples);
            double p95Cpu = Percentile(cpuSamples, 0.95d);
            const double unavailableCpuMilliseconds = 999999d;
            double maximumCpu = cpuSamples.Count > 0 ? cpuSamples[cpuSamples.Count - 1] : unavailableCpuMilliseconds;
            int commonAiCount = CountActiveCommonAi(lab);
            string reportPath = GetReportPath(PerformanceReportFileName);
            var report = new Tool13PerformanceReport
            {
                BuildTag = "TOOL-13",
                CapturedAtUtc = DateTime.UtcNow.ToString("O"),
                UnityVersion = Application.unityVersion,
                DevelopmentBuild = Debug.isDebugBuild,
                RequestedWidth = 1920,
                RequestedHeight = 1080,
                ActualWidth = Screen.width,
                ActualHeight = Screen.height,
                WarmupFrames = WarmupFrameCount,
                SampleFrames = SampleFrameCount,
                ValidCpuSamples = cpuSamples.Count,
                CpuTimingAvailable = cpuSamples.Count > 0,
                AverageCpuMainThreadMs = cpuSamples.Count > 0 ? averageCpu : unavailableCpuMilliseconds,
                P95CpuMainThreadMs = cpuSamples.Count > 0 ? p95Cpu : unavailableCpuMilliseconds,
                MaximumCpuMainThreadMs = maximumCpu,
                MinimumObservedNonTransitionFps = minimumFps,
                MaximumTransitionGcAllocatedBytes = Math.Max(forwardBytes, backwardBytes),
                ForwardTransitionCompleted = forwardCompleted,
                BackwardTransitionCompleted = backwardCompleted,
                ActiveCommonAiCount = commonAiCount,
                ResolutionPassed = Screen.width == 1920 && Screen.height == 1080,
                CpuFramePassed = cpuSamples.Count > 0 && p95Cpu <= CpuFrameBudgetMilliseconds,
                TransitionGcPassed = forwardCompleted && backwardCompleted &&
                                     Math.Max(forwardBytes, backwardBytes) <= TransitionAllocationBudgetBytes,
                NonTransitionFpsPassed = minimumFps >= MinimumNonTransitionFps,
                CommonAiPassed = commonAiCount <= MaximumCommonAi,
                ReportPath = reportPath,
            };
            report.Passed = report.DevelopmentBuild && report.ResolutionPassed && report.CpuFramePassed &&
                            report.TransitionGcPassed && report.NonTransitionFpsPassed && report.CommonAiPassed;

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? Application.persistentDataPath);
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            lastPerformanceReport = report;
            performanceRunning = false;
            statusMessage = $"Performance {(report.Passed ? "PASS" : "FAIL")}: {reportPath}";
            Debug.Log(
                $"TOOL13_PERFORMANCE {(report.Passed ? "PASS" : "FAIL")} " +
                $"cpuP95={report.P95CpuMainThreadMs:F3}ms gc={report.MaximumTransitionGcAllocatedBytes}B " +
                $"minFps={report.MinimumObservedNonTransitionFps:F1} report={reportPath}",
                this);

            if (quitWhenComplete)
            {
                yield return null;
                Application.Quit();
            }
        }

        private void DisableToolLabSiblings()
        {
            Transform parent = transform.parent;
            if (parent == null)
            {
                return;
            }

            for (int index = 0; index < parent.childCount; index++)
            {
                GameObject sibling = parent.GetChild(index).gameObject;
                if (sibling != gameObject)
                {
                    sibling.SetActive(false);
                }
            }
        }

        private static Core04TwoRoomLab CreatePerformanceLab()
        {
            GameObject cameraObject = new GameObject("Tool13PerformanceCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            GameObject playerObject = new GameObject("Tool13PerformancePlayer");
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0) playerObject.layer = playerLayer;
            Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            playerObject.AddComponent<CapsuleCollider2D>();
            playerObject.AddComponent<PlayerActionLock>();
            PlayerMotor2D player = playerObject.AddComponent<PlayerMotor2D>();
            int groundMask = LayerMask.GetMask("TerrainSolid", "TerrainOneWay");
            player.ConfigureForTests(groundMask);
            playerObject.AddComponent<PlayerOutOfBoundsGuard>();

            GameObject labObject = new GameObject("Tool13PerformanceTwoRoomLab");
            Core04TwoRoomLab lab = labObject.AddComponent<Core04TwoRoomLab>();
            lab.BuildIfNeeded();
            lab.InitializePlayerAndCamera(player, camera);
            return lab;
        }

        private static int CountActiveCommonAi(Core04TwoRoomLab lab)
        {
            if (lab == null || lab.TransitionController == null || lab.TransitionController.CurrentRoom == null)
            {
                return 0;
            }

            // No common-AI runtime is registered yet. Keeping the count explicit makes the cap visible
            // in the approval report and allows later AI components to opt in by using the Enemy layer.
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer < 0)
            {
                return 0;
            }

            int count = 0;
            MonoBehaviour[] behaviours = lab.TransitionController.CurrentRoom.GetComponentsInChildren<MonoBehaviour>(false);
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] != null && behaviours[index].gameObject.layer == enemyLayer &&
                    behaviours[index].isActiveAndEnabled)
                {
                    count++;
                }
            }
            return count;
        }

        private static double Average(List<double> samples)
        {
            if (samples.Count == 0) return double.PositiveInfinity;
            double sum = 0d;
            for (int index = 0; index < samples.Count; index++) sum += samples[index];
            return sum / samples.Count;
        }

        private static double Percentile(List<double> sortedSamples, double percentile)
        {
            if (sortedSamples.Count == 0) return double.PositiveInfinity;
            int index = Mathf.Clamp(
                Mathf.CeilToInt((float)(sortedSamples.Count * percentile)) - 1,
                0,
                sortedSamples.Count - 1);
            return sortedSamples[index];
        }

        private static string GetReportPath(string fileName)
        {
            if (!Application.isEditor)
            {
                return Path.Combine(Application.persistentDataPath, fileName);
            }

            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "_Game/Editor/ToolAuthoring/Reports",
                fileName));
        }
    }
}

#endif
