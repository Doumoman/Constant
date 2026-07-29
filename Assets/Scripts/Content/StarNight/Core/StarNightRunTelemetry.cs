using System;
using System.Linq;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarNightRunTelemetry : MonoBehaviour
    {
        [SerializeField] private bool logCompletedRun = true;
        [SerializeField] private bool persistCompletedRun = true;
        [SerializeField] private bool tracking;
        [SerializeField] private RunBalanceSnapshot latest = new();

        private StarNightRunState run;
        private ChapterBalanceSample currentChapter;
        private double runStartedAt;
        private double chapterStartedAt;

        public RunBalanceSnapshot Latest => latest;
        public bool Tracking => tracking;

        public void Attach(StarNightRunState owner)
        {
            Detach();
            run = owner;
            if (run == null)
            {
                return;
            }

            run.RunStarted += OnRunStarted;
            run.ChapterStarted += OnChapterStarted;
            run.RunEnded += OnRunEnded;
            run.Actions.Recorded += OnActionRecorded;

            if (run.RunActive)
            {
                BeginSnapshot();
                if (run.Chapter?.Definition != null)
                {
                    OnChapterStarted(run.Chapter.Definition);
                }
            }
        }

        private void OnDestroy()
        {
            Detach();
        }

        private void OnRunStarted()
        {
            BeginSnapshot();
        }

        private void BeginSnapshot()
        {
            tracking = true;
            currentChapter = null;
            runStartedAt = Now();
            chapterStartedAt = runStartedAt;
            latest = new RunBalanceSnapshot
            {
                seed = run != null ? run.Seed : 0,
                endReason = StarRunEndReason.None,
                ending = PolarisEndingType.None
            };
        }

        private void OnChapterStarted(StarChapterDefinition definition)
        {
            if (!tracking || definition == null)
            {
                return;
            }

            FinalizeCurrentChapter();
            chapterStartedAt = Now();
            currentChapter = new ChapterBalanceSample
            {
                chapter = definition.chapter
            };
        }

        private void OnActionRecorded(StarActionRecord record)
        {
            if (!tracking || record == null || currentChapter == null)
            {
                return;
            }

            if (record.actionType == StarActionType.GateContributionAdded &&
                !string.IsNullOrWhiteSpace(record.routeId) &&
                !currentChapter.contributedRoutes.Contains(record.routeId))
            {
                currentChapter.contributedRoutes.Add(record.routeId);
            }
            if (record.actionType == StarActionType.EnteredTemptationRoom ||
                record.actionType == StarActionType.RareRoomEntered ||
                record.actionType == StarActionType.ReturnVaultEntered)
            {
                currentChapter.temptationEntered = true;
            }
            if (record.actionType == StarActionType.BellPhaseChanged)
            {
                currentChapter.highestBell = Mathf.Max(currentChapter.highestBell, record.bellPhase);
            }
        }

        private void OnRunEnded(StarRunEndReason reason)
        {
            if (!tracking)
            {
                return;
            }

            FinalizeCurrentChapter();
            latest.durationSeconds = Mathf.Max(0f, (float)(Now() - runStartedAt));
            latest.endReason = reason;
            latest.ending = ResolveEnding(run);
            latest.informationUnits = StarNightBalanceProfile.CountInformationUnits(run);
            latest.accidentCount = run?.AccidentReport?.Steps.Count ?? 0;
            latest.contextualAccidentCount = run?.AccidentReport?.Steps.Count(step =>
                step.gateActivated || step.bellPhase > 0) ?? 0;
            tracking = false;

            if (logCompletedRun)
            {
                Debug.Log($"[M6 Run QA] {latest.BuildTechnicalReport()}");
            }
            if (persistCompletedRun && Application.isPlaying &&
                run != null && !run.GetFlag("POLARIS_DIRECT_DEBUG_RUN"))
            {
                StarNightTelemetryStore.Record(latest);
            }
        }

        private void FinalizeCurrentChapter()
        {
            if (currentChapter == null)
            {
                return;
            }

            currentChapter.durationSeconds = Mathf.Max(0f, (float)(Now() - chapterStartedAt));
            if (run?.Chapter != null && run.CurrentChapter == currentChapter.chapter)
            {
                currentChapter.highestBell = Mathf.Max(currentChapter.highestBell,
                    (int)run.Chapter.BellPhase);
                currentChapter.exitAlert = run.Chapter.PostGateAlert;
            }
            latest.chapters.Add(currentChapter);
            currentChapter = null;
        }

        public void OverrideDurationForTests(float seconds)
        {
            latest.durationSeconds = Mathf.Max(0f, seconds);
        }

        private void Detach()
        {
            if (run == null)
            {
                return;
            }
            run.RunStarted -= OnRunStarted;
            run.ChapterStarted -= OnChapterStarted;
            run.RunEnded -= OnRunEnded;
            run.Actions.Recorded -= OnActionRecorded;
            run = null;
        }

        private static PolarisEndingType ResolveEnding(StarNightRunState targetRun)
        {
            if (targetRun == null)
            {
                return PolarisEndingType.None;
            }
            foreach (PolarisEndingType ending in Enum.GetValues(typeof(PolarisEndingType)))
            {
                if (ending != PolarisEndingType.None &&
                    targetRun.GetFlag($"ENDING_{ending.ToString().ToUpperInvariant()}"))
                {
                    return ending;
                }
            }
            return PolarisEndingType.None;
        }

        private static double Now() => Time.realtimeSinceStartupAsDouble;
    }
}
