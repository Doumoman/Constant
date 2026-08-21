#if LEGACY_DISABLED
using System;
using StarNight.Generation.P6;
using UnityEngine;

namespace StarNight.Maru.P8
{
    [DefaultExecutionOrder(-120)]
    [DisallowMultipleComponent]
    public sealed class P8MaruTimeline2D : MonoBehaviour
    {
        [SerializeField] private P6StageSlot stageSlot = P6StageSlot.X2;
        [SerializeField, Min(0f)] private float firstBellSeconds =
            P8MaruTimelineProfile.BaseFirstBellSeconds;
        [SerializeField, Min(0f)] private float secondBellSeconds =
            P8MaruTimelineProfile.BaseSecondBellSeconds;
        [SerializeField, Min(0f)] private float naturalMaruDueSeconds =
            P8MaruTimelineProfile.BaseMaruDueSeconds;
        [SerializeField, Min(0f)] private float activeMaruDueSeconds =
            P8MaruTimelineProfile.BaseMaruDueSeconds;
        [SerializeField] private bool pausedForBoss;
        [SerializeField] private bool autoStart;
        [SerializeField] private P8MaruPhase phase = P8MaruPhase.Calm;

        private bool firstBellRang;
        private bool secondBellRang;
        private bool huntBellRang;

        public event Action<P8MaruPhase> PhaseChanged;
        public event Action<P8BellEvent> BellRang;
        public event Action<P8StatueDestructionResult> StatueAccelerated;

        public P6StageSlot StageSlot => stageSlot;
        public float FirstBellSeconds => firstBellSeconds;
        public float SecondBellSeconds => secondBellSeconds;
        public float NaturalMaruDueSeconds => naturalMaruDueSeconds;
        public float ActiveMaruDueSeconds => activeMaruDueSeconds;
        public float ElapsedSeconds { get; private set; }
        public float SecondsAdvanced =>
            Mathf.Max(0f, naturalMaruDueSeconds - activeMaruDueSeconds);
        public float TimeUntilMaru =>
            Mathf.Max(0f, activeMaruDueSeconds - ElapsedSeconds);
        public P8MaruPhase Phase => phase;
        public P8MaruPhase LastActivePhase { get; private set; } =
            P8MaruPhase.Calm;
        public bool PausedForBoss => pausedForBoss;
        public bool IsRunning { get; private set; }
        public bool StatueWasDestroyed { get; private set; }
        public P8StatueImpactKind LastStatueImpact { get; private set; }

        public void Configure(
            P8MaruTimelineProfile profile,
            bool startAutomatically = false)
        {
            stageSlot = profile.StageSlot;
            firstBellSeconds = profile.FirstBellSeconds;
            secondBellSeconds = profile.SecondBellSeconds;
            naturalMaruDueSeconds = profile.MaruDueSeconds;
            activeMaruDueSeconds = profile.MaruDueSeconds;
            pausedForBoss = profile.PausedForBoss;
            autoStart = startAutomatically;
            ResetTimeline();
        }

        private void Start()
        {
            if (autoStart)
            {
                StartTimeline();
            }
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        public void StartTimeline(bool reset = true)
        {
            if (reset)
            {
                ResetTimeline();
            }

            if (pausedForBoss)
            {
                IsRunning = false;
                return;
            }

            IsRunning = true;
        }

        public void StopTimeline()
        {
            IsRunning = false;
            SetPhase(P8MaruPhase.Stopped);
        }

        public void Advance(float deltaSeconds)
        {
            if (!IsRunning
                || pausedForBoss
                || deltaSeconds <= 0f
                || phase == P8MaruPhase.Hunting)
            {
                return;
            }

            ElapsedSeconds += deltaSeconds;
            if (!firstBellRang
                && ElapsedSeconds >= firstBellSeconds)
            {
                firstBellRang = true;
                Ring(
                    P8BellSignal.Short,
                    P8BellCause.NaturalTimeline,
                    P8MaruPhase.FirstBell);
            }

            if (!secondBellRang
                && ElapsedSeconds >= secondBellSeconds)
            {
                secondBellRang = true;
                Ring(
                    P8BellSignal.Short,
                    P8BellCause.NaturalTimeline,
                    P8MaruPhase.SecondBell);
            }

            if (!huntBellRang
                && ElapsedSeconds >= activeMaruDueSeconds)
            {
                huntBellRang = true;
                Ring(
                    P8BellSignal.Long,
                    P8BellCause.NaturalTimeline,
                    P8MaruPhase.Hunting);
            }
        }

        public void RingStatueCrack()
        {
            BellRang?.Invoke(
                new P8BellEvent(
                    P8BellSignal.Short,
                    P8BellCause.StatueCracked,
                    phase,
                    ElapsedSeconds));
        }

        public P8StatueDestructionResult ApplyStatueDestroyed(
            P8StatueImpactKind impactKind)
        {
            if (StatueWasDestroyed || phase == P8MaruPhase.Hunting)
            {
                return new P8StatueDestructionResult(
                    false,
                    activeMaruDueSeconds,
                    SecondsAdvanced);
            }

            StatueWasDestroyed = true;
            LastStatueImpact = impactKind;
            float delay;
            switch (phase)
            {
                case P8MaruPhase.FirstBell:
                    delay = 12f;
                    break;
                case P8MaruPhase.SecondBell:
                    delay = 5f;
                    break;
                default:
                    delay = 20f;
                    break;
            }

            activeMaruDueSeconds = Mathf.Min(
                activeMaruDueSeconds,
                ElapsedSeconds + delay);
            if (phase == P8MaruPhase.Calm)
            {
                firstBellRang = true;
                secondBellRang = true;
                SetPhase(P8MaruPhase.SecondBell);
            }

            ReplayBellPattern(P8BellCause.StatueDestroyed);
            var result = new P8StatueDestructionResult(
                true,
                activeMaruDueSeconds,
                SecondsAdvanced);
            StatueAccelerated?.Invoke(result);
            return result;
        }

        public void ResetTimelineForTests()
        {
            ResetTimeline();
        }

        private void ReplayBellPattern(P8BellCause cause)
        {
            BellRang?.Invoke(
                new P8BellEvent(
                    P8BellSignal.Short,
                    cause,
                    phase,
                    ElapsedSeconds));
            BellRang?.Invoke(
                new P8BellEvent(
                    P8BellSignal.Short,
                    cause,
                    phase,
                    ElapsedSeconds));
            BellRang?.Invoke(
                new P8BellEvent(
                    P8BellSignal.Long,
                    cause,
                    phase,
                    ElapsedSeconds));
        }

        private void Ring(
            P8BellSignal signal,
            P8BellCause cause,
            P8MaruPhase nextPhase)
        {
            SetPhase(nextPhase);
            BellRang?.Invoke(
                new P8BellEvent(
                    signal,
                    cause,
                    nextPhase,
                    ElapsedSeconds));
        }

        private void ResetTimeline()
        {
            IsRunning = false;
            ElapsedSeconds = 0f;
            activeMaruDueSeconds = naturalMaruDueSeconds;
            firstBellRang = false;
            secondBellRang = false;
            huntBellRang = false;
            StatueWasDestroyed = false;
            LastStatueImpact = default;
            LastActivePhase = P8MaruPhase.Calm;
            SetPhase(P8MaruPhase.Calm);
        }

        private void SetPhase(P8MaruPhase next)
        {
            if (phase == next)
            {
                return;
            }

            phase = next;
            if (next != P8MaruPhase.Stopped)
            {
                LastActivePhase = next;
            }

            PhaseChanged?.Invoke(phase);
        }
    }
}

#endif
