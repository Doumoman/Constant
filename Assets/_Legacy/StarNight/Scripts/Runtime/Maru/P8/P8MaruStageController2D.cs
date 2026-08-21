#if LEGACY_DISABLED
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Maru.P8
{
    [DefaultExecutionOrder(-180)]
    [DisallowMultipleComponent]
    public sealed class P8MaruStageController2D : MonoBehaviour
    {
        [SerializeField] private P8MaruTimeline2D timeline;
        [SerializeField] private P8MaruPursuer2D pursuer;
        [SerializeField] private P8MaruBiteController2D biteController;
        [SerializeField] private P8MaruTelemetry2D telemetry;
        [SerializeField] private P5StageCoreLoop2D p5CoreLoop;
        [SerializeField] private P5MaruBellClock2D p5BellClock;
        [SerializeField] private P5StageExit2D p5StageExit;

        private bool subscribed;

        public P8MaruTimeline2D Timeline => timeline;
        public P8MaruPursuer2D Pursuer => pursuer;
        public P8MaruBiteController2D BiteController => biteController;
        public P8MaruTelemetry2D Telemetry => telemetry;
        public bool StageActive { get; private set; }
        public bool UsesP5ClockCompatibility => p5BellClock != null;

        public void Configure(
            P8MaruTimeline2D maruTimeline,
            P8MaruPursuer2D maruPursuer,
            P8MaruBiteController2D bite,
            P8MaruTelemetry2D maruTelemetry,
            P5StageCoreLoop2D coreLoop = null,
            P5MaruBellClock2D compatibilityClock = null,
            P5StageExit2D stageExit = null)
        {
            Unsubscribe();
            timeline = maruTimeline;
            pursuer = maruPursuer;
            biteController = bite;
            telemetry = maruTelemetry;
            p5CoreLoop = coreLoop;
            p5BellClock = compatibilityClock;
            p5StageExit = stageExit;
            Subscribe();
            ResetStageForTests();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void BeginStage()
        {
            if (StageActive)
            {
                return;
            }

            StageActive = true;
            biteController?.ResetStage();
            pursuer?.StopHunt();
            if (p5BellClock == null)
            {
                timeline?.StartTimeline();
            }

            telemetry?.MarkStageStarted();
        }

        public void CompleteStage()
        {
            if (!StageActive)
            {
                return;
            }

            StageActive = false;
            timeline?.StopTimeline();
            p5BellClock?.StopClock();
            pursuer?.StopHunt();
            telemetry?.MarkStageCleared();
        }

        public void ResetStageForTests()
        {
            StageActive = false;
            timeline?.ResetTimelineForTests();
            p5BellClock?.ResetClockForTests();
            pursuer?.StopHunt();
            biteController?.ResetStage();
            telemetry?.ResetSession();
        }

        private void HandleTimelinePhase(P8MaruPhase next)
        {
            if (next != P8MaruPhase.Hunting)
            {
                return;
            }

            pursuer?.BeginHunt();
            telemetry?.MarkMaruAppeared();
        }

        private void HandleP5BellPhase(P5MaruBellPhase next)
        {
            if (!UsesP5ClockCompatibility
                || next != P5MaruBellPhase.MaruDue)
            {
                return;
            }

            pursuer?.BeginHunt();
            telemetry?.MarkMaruAppeared();
        }

        private void HandleRunEnded(P8DeathCauseReport report)
        {
            StageActive = false;
            timeline?.StopTimeline();
            p5BellClock?.StopClock();
            pursuer?.StopHunt();
            telemetry?.MarkRunEnded(report);
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            if (timeline != null)
            {
                timeline.PhaseChanged += HandleTimelinePhase;
            }

            if (UsesP5ClockCompatibility)
            {
                p5BellClock.PhaseChanged += HandleP5BellPhase;
            }

            if (p5CoreLoop != null)
            {
                p5CoreLoop.StageBegan += BeginStage;
                p5CoreLoop.StageDeparted += CompleteStage;
            }

            if (p5StageExit != null)
            {
                p5StageExit.Departed += CompleteStage;
            }

            if (biteController != null)
            {
                biteController.RunEnded += HandleRunEnded;
            }

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (timeline != null)
            {
                timeline.PhaseChanged -= HandleTimelinePhase;
            }

            if (UsesP5ClockCompatibility)
            {
                p5BellClock.PhaseChanged -= HandleP5BellPhase;
            }

            if (p5CoreLoop != null)
            {
                p5CoreLoop.StageBegan -= BeginStage;
                p5CoreLoop.StageDeparted -= CompleteStage;
            }

            if (p5StageExit != null)
            {
                p5StageExit.Departed -= CompleteStage;
            }

            if (biteController != null)
            {
                biteController.RunEnded -= HandleRunEnded;
            }

            subscribed = false;
        }
    }
}

#endif
