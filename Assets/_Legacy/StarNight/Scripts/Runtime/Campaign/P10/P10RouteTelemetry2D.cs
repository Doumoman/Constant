#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Folklore.P9;
using UnityEngine;

namespace StarNight.Campaign.P10
{
    [DisallowMultipleComponent]
    public sealed class P10RouteTelemetry2D : MonoBehaviour
    {
        public const float NormalRouteTargetMinSeconds = 30f * 60f;
        public const float NormalRouteTargetMaxSeconds = 40f * 60f;

        [SerializeField] private bool routeActive;
        [SerializeField] private bool routeCompleted;
        [SerializeField] private float activeGameplaySeconds;
        [SerializeField] private float stageSeconds;
        [SerializeField] private float transitionSeconds;
        [SerializeField] private float shopSeconds;
        [SerializeField] private float backtrackSeconds;
        [SerializeField] private float crossRouteSeconds;
        [SerializeField] private P9BranchKind firstBranch;
        [SerializeField] private P9BranchKind crossBranch;
        [SerializeField] private List<P10StageId> enteredStages =
            new List<P10StageId>();
        [SerializeField] private int branchFeelSurveyResponses;
        [SerializeField] private int branchFeelClearlyDifferentResponses;

        public bool RouteActive => routeActive;
        public bool RouteCompleted => routeCompleted;
        public float ActiveGameplaySeconds => activeGameplaySeconds;
        public float StageSeconds => stageSeconds;
        public float TransitionSeconds => transitionSeconds;
        public float ShopSeconds => shopSeconds;
        public float BacktrackSeconds => backtrackSeconds;
        public float CrossRouteSeconds => crossRouteSeconds;
        public P9BranchKind FirstBranch => firstBranch;
        public P9BranchKind CrossBranch => crossBranch;
        public IReadOnlyList<P10StageId> EnteredStages =>
            enteredStages;
        public int BranchFeelSurveyResponses =>
            branchFeelSurveyResponses;
        public float BranchFeelClearlyDifferentRate =>
            branchFeelSurveyResponses > 0
                ? (float)branchFeelClearlyDifferentResponses
                    / branchFeelSurveyResponses
                : 0f;
        public bool HasHumanTimingSample => routeCompleted;
        public bool LastNormalRouteWithinTarget =>
            routeCompleted
            && IsWithinNormalRouteTarget(activeGameplaySeconds);
        public bool HumanTimingGateRequiresPlaytest => true;
        public bool HumanBranchFeelGateRequiresPlaytest => true;
        public bool InstrumentationReady =>
            Mathf.Approximately(
                NormalRouteTargetMinSeconds,
                1800f)
            && Mathf.Approximately(
                NormalRouteTargetMaxSeconds,
                2400f);

        private void Update()
        {
            if (routeActive)
            {
                activeGameplaySeconds += Time.unscaledDeltaTime;
                if (crossBranch != P9BranchKind.None)
                {
                    crossRouteSeconds += Time.unscaledDeltaTime;
                }
            }
        }

        public void BeginNormalRoute()
        {
            ResetTelemetryForTests();
            routeActive = true;
        }

        public void MarkStageEntered(P10StageId stageId)
        {
            if (!routeActive)
            {
                routeActive = true;
            }

            if (!enteredStages.Contains(stageId))
            {
                enteredStages.Add(stageId);
            }
        }

        public void MarkStageCompleted(P10StageId stageId)
        {
        }

        public void MarkFirstBranchChosen(P9BranchKind branch)
        {
            firstBranch = branch;
        }

        public void MarkCrossRouteOpened(P9BranchKind branch)
        {
            crossBranch = branch;
        }

        public void RecordBreakdownForTests(
            float stages,
            float transitions,
            float shops,
            float backtracking,
            float crossRoute = 0f)
        {
            stageSeconds = Mathf.Max(0f, stages);
            transitionSeconds = Mathf.Max(0f, transitions);
            shopSeconds = Mathf.Max(0f, shops);
            backtrackSeconds = Mathf.Max(0f, backtracking);
            crossRouteSeconds = Mathf.Max(0f, crossRoute);
            activeGameplaySeconds =
                stageSeconds
                + transitionSeconds
                + shopSeconds
                + backtrackSeconds;
        }

        public void CompleteNormalRouteAtCommonEntry()
        {
            routeActive = false;
            routeCompleted = true;
        }

        public void RecordBranchFeelSurvey(bool clearlyDifferent)
        {
            branchFeelSurveyResponses++;
            if (clearlyDifferent)
            {
                branchFeelClearlyDifferentResponses++;
            }
        }

        public void ResetTelemetryForTests()
        {
            routeActive = false;
            routeCompleted = false;
            activeGameplaySeconds = 0f;
            stageSeconds = 0f;
            transitionSeconds = 0f;
            shopSeconds = 0f;
            backtrackSeconds = 0f;
            crossRouteSeconds = 0f;
            firstBranch = P9BranchKind.None;
            crossBranch = P9BranchKind.None;
            enteredStages.Clear();
            branchFeelSurveyResponses = 0;
            branchFeelClearlyDifferentResponses = 0;
        }

        public static bool IsWithinNormalRouteTarget(float seconds)
        {
            return seconds >= NormalRouteTargetMinSeconds
                && seconds <= NormalRouteTargetMaxSeconds;
        }
    }
}

#endif
