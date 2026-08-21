#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Stages.P5
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class P5MaruBellClock2D : MonoBehaviour
    {
        public const float MoonPalace11FirstBellSeconds = 140f;
        public const float MoonPalace11SecondBellSeconds = 185f;
        public const float MoonPalace11MaruDueSeconds = 215f;

        [SerializeField, Min(0.01f)] private float firstBellSeconds =
            MoonPalace11FirstBellSeconds;
        [SerializeField, Min(0.02f)] private float secondBellSeconds =
            MoonPalace11SecondBellSeconds;
        [SerializeField, Min(0.03f)] private float maruDueSeconds =
            MoonPalace11MaruDueSeconds;
        [SerializeField] private bool autoStart;
        [SerializeField] private P5MaruBellPhase phase =
            P5MaruBellPhase.Calm;

        private bool firstBellRang;
        private bool secondBellRang;
        private bool maruDueRang;

        public event Action<P5MaruBellPhase> PhaseChanged;
        public event Action<P5BellSignal, P5MaruBellPhase> BellRang;

        public float FirstBellSeconds => firstBellSeconds;
        public float SecondBellSeconds => secondBellSeconds;
        public float MaruDueSeconds => maruDueSeconds;
        public float ElapsedSeconds { get; private set; }
        public P5MaruBellPhase Phase => phase;
        public P5MaruBellPhase LastActivePhase { get; private set; } =
            P5MaruBellPhase.Calm;
        public bool HasFirstBellRung => firstBellRang;
        public bool HasSecondBellRung => secondBellRang;
        public bool HasMaruDueBellRung => maruDueRang;
        public bool IsRunning { get; private set; }

        public void Configure(
            float firstBellAt = MoonPalace11FirstBellSeconds,
            float secondBellAt = MoonPalace11SecondBellSeconds,
            float maruDueAt = MoonPalace11MaruDueSeconds,
            bool startAutomatically = false)
        {
            if (!Mathf.Approximately(
                    firstBellAt,
                    MoonPalace11FirstBellSeconds)
                || !Mathf.Approximately(
                    secondBellAt,
                    MoonPalace11SecondBellSeconds)
                || !Mathf.Approximately(
                    maruDueAt,
                    MoonPalace11MaruDueSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(firstBellAt),
                    "Moon Palace 1-1 bells are fixed at 140, 185, and 215 seconds.");
            }

            firstBellSeconds = firstBellAt;
            secondBellSeconds = secondBellAt;
            maruDueSeconds = maruDueAt;
            autoStart = startAutomatically;
            ResetClock();
        }

        private void Start()
        {
            if (autoStart)
            {
                StartClock();
            }
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        public void StartClock(bool reset = true)
        {
            if (reset)
            {
                ResetClock();
            }

            if (phase == P5MaruBellPhase.Stopped)
            {
                SetPhase(ResolvePhaseAt(ElapsedSeconds));
            }

            IsRunning = true;
        }

        public void Advance(float deltaSeconds)
        {
            if (!IsRunning || deltaSeconds <= 0f)
            {
                return;
            }

            ElapsedSeconds += deltaSeconds;
            if (!firstBellRang
                && ElapsedSeconds >= firstBellSeconds)
            {
                firstBellRang = true;
                Ring(P5BellSignal.Short, P5MaruBellPhase.FirstBell);
            }

            if (!secondBellRang
                && ElapsedSeconds >= secondBellSeconds)
            {
                secondBellRang = true;
                Ring(P5BellSignal.Short, P5MaruBellPhase.SecondBell);
            }

            if (!maruDueRang
                && ElapsedSeconds >= maruDueSeconds)
            {
                maruDueRang = true;
                Ring(P5BellSignal.Long, P5MaruBellPhase.MaruDue);
            }
        }

        public void StopClock()
        {
            if (!IsRunning && phase == P5MaruBellPhase.Stopped)
            {
                return;
            }

            IsRunning = false;
            SetPhase(P5MaruBellPhase.Stopped);
        }

        public void ResetClockForTests()
        {
            ResetClock();
        }

        private void ResetClock()
        {
            IsRunning = false;
            ElapsedSeconds = 0f;
            firstBellRang = false;
            secondBellRang = false;
            maruDueRang = false;
            LastActivePhase = P5MaruBellPhase.Calm;
            SetPhase(P5MaruBellPhase.Calm);
        }

        private void Ring(
            P5BellSignal signal,
            P5MaruBellPhase nextPhase)
        {
            SetPhase(nextPhase);
            BellRang?.Invoke(signal, nextPhase);
        }

        private void SetPhase(P5MaruBellPhase next)
        {
            if (phase == next)
            {
                return;
            }

            phase = next;
            if (next != P5MaruBellPhase.Stopped)
            {
                LastActivePhase = next;
            }

            PhaseChanged?.Invoke(phase);
        }

        private P5MaruBellPhase ResolvePhaseAt(float elapsed)
        {
            if (elapsed >= maruDueSeconds)
            {
                return P5MaruBellPhase.MaruDue;
            }

            if (elapsed >= secondBellSeconds)
            {
                return P5MaruBellPhase.SecondBell;
            }

            if (elapsed >= firstBellSeconds)
            {
                return P5MaruBellPhase.FirstBell;
            }

            return P5MaruBellPhase.Calm;
        }
    }
}

#endif
