#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Stages.P5
{
    [DisallowMultipleComponent]
    public sealed class P5BacktrackProbe2D : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private P5StageExit2D stageExit;
        [SerializeField] private P5SliceTelemetry2D telemetry;
        [SerializeField, Min(0.1f)] private float activationRadius = 1.5f;

        private bool subscribed;
        private bool armed;

        public event Action BacktrackObserved;

        public bool IsArmed => armed;
        public bool WasTriggered { get; private set; }

        public void Configure(
            Transform targetPlayer,
            P5StageExit2D targetExit,
            P5SliceTelemetry2D targetTelemetry,
            float radius = 1.5f)
        {
            Unsubscribe();
            player = targetPlayer;
            stageExit = targetExit;
            telemetry = targetTelemetry;
            activationRadius = Mathf.Max(0.1f, radius);
            armed = stageExit != null
                && stageExit.State != P5StageExitState.Unseen;
            WasTriggered = false;
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (!armed
                || WasTriggered
                || player == null
                || (stageExit != null
                    && stageExit.State == P5StageExitState.Departed))
            {
                return;
            }

            float distanceSquared =
                ((Vector2)player.position - (Vector2)transform.position)
                .sqrMagnitude;
            if (distanceSquared > activationRadius * activationRadius)
            {
                return;
            }

            WasTriggered = true;
            telemetry?.MarkBacktrackAfterExit();
            BacktrackObserved?.Invoke();
        }

        public void TriggerForTests()
        {
            if (!armed || WasTriggered)
            {
                return;
            }

            WasTriggered = true;
            telemetry?.MarkBacktrackAfterExit();
            BacktrackObserved?.Invoke();
        }

        private void HandleExitReached()
        {
            armed = true;
        }

        private void Subscribe()
        {
            if (subscribed || stageExit == null)
            {
                return;
            }

            stageExit.FirstReached += HandleExitReached;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || stageExit == null)
            {
                return;
            }

            stageExit.FirstReached -= HandleExitReached;
            subscribed = false;
        }
    }
}

#endif
