#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Stages.P5
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class P5StageCoreLoop2D : MonoBehaviour
    {
        [SerializeField] private P5RunState2D runState;
        [SerializeField] private P5MaruBellClock2D bellClock;
        [SerializeField] private P5StageExit2D stageExit;
        [SerializeField] private P5MoonRabbitPestleEvent2D rabbitEvent;
        [SerializeField] private P5MomoShop2D shop;
        [SerializeField] private P5SliceTelemetry2D telemetry;
        [SerializeField, Min(0f)] private float introDuration = 1f;
        [SerializeField] private bool autoBeginAfterIntro = true;
        [SerializeField] private P5CoreLoopState state =
            P5CoreLoopState.Intro;

        private float introElapsed;

        public event Action<P5CoreLoopState> StateChanged;
        public event Action StageBegan;
        public event Action StageDeparted;

        public P5RunState2D RunState => runState;
        public P5MaruBellClock2D BellClock => bellClock;
        public P5StageExit2D StageExit => stageExit;
        public P5MoonRabbitPestleEvent2D RabbitEvent => rabbitEvent;
        public P5MomoShop2D Shop => shop;
        public P5SliceTelemetry2D Telemetry => telemetry;
        public P5CoreLoopState State => state;
        public bool CanAcceptExitInput =>
            state == P5CoreLoopState.Active
            || state == P5CoreLoopState.ExitReached;

        public void Configure(
            P5RunState2D targetRunState,
            P5MaruBellClock2D targetBellClock,
            P5StageExit2D targetExit,
            P5MoonRabbitPestleEvent2D targetRabbitEvent,
            P5MomoShop2D targetShop,
            P5SliceTelemetry2D targetTelemetry,
            float entryIntroDuration = 1f,
            bool autoBegin = true)
        {
            runState = targetRunState;
            bellClock = targetBellClock;
            stageExit = targetExit;
            rabbitEvent = targetRabbitEvent;
            shop = targetShop;
            telemetry = targetTelemetry;
            introDuration = Mathf.Max(0f, entryIntroDuration);
            autoBeginAfterIntro = autoBegin;
            RestartForTests();
        }

        private void Update()
        {
            if (state != P5CoreLoopState.Intro
                || !autoBeginAfterIntro)
            {
                return;
            }

            introElapsed += Time.unscaledDeltaTime;
            if (introElapsed >= introDuration)
            {
                CompleteIntroAndBegin();
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying
                && state != P5CoreLoopState.Departed)
            {
                bellClock?.StopClock();
            }
        }

        public bool CompleteIntroAndBegin()
        {
            if (state != P5CoreLoopState.Intro)
            {
                return false;
            }

            introElapsed = introDuration;
            SetState(P5CoreLoopState.Active);
            telemetry?.MarkStageStarted();
            bellClock?.StartClock();
            StageBegan?.Invoke();
            return true;
        }

        public void NotifyExitReached(P5StageExit2D reachedExit)
        {
            if (reachedExit != stageExit
                || state != P5CoreLoopState.Active)
            {
                return;
            }

            SetState(P5CoreLoopState.ExitReached);
        }

        public void NotifyExitDeparted(P5StageExit2D departedExit)
        {
            if (departedExit != stageExit
                || state == P5CoreLoopState.Departed)
            {
                return;
            }

            bellClock?.StopClock();
            SetState(P5CoreLoopState.Departed);
            StageDeparted?.Invoke();
        }

        public void RestartForTests()
        {
            introElapsed = 0f;
            bellClock?.ResetClockForTests();
            stageExit?.ResetExitForTests();
            telemetry?.ResetTelemetryForTests();
            SetState(P5CoreLoopState.Intro);
        }

        private void SetState(P5CoreLoopState next)
        {
            if (state == next)
            {
                return;
            }

            state = next;
            StateChanged?.Invoke(state);
        }
    }
}

#endif
