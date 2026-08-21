#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Maru.P8
{
    [DefaultExecutionOrder(20)]
    [DisallowMultipleComponent]
    public sealed class P8MaruPursuer2D : MonoBehaviour
    {
        public const float DefaultMoveSpeed = 2.2f;
        public const float DefaultDashSpeed = 4.4f;

        [SerializeField] private P8MaruRoomGraph2D roomGraph;
        [SerializeField] private P8ReturnPile2D returnPile;
        [SerializeField] private P8MaruBiteController2D biteController;
        [SerializeField] private GameObject huntVisual;
        [SerializeField, Min(0.1f)] private float moveSpeed =
            DefaultMoveSpeed;
        [SerializeField, Min(0.1f)] private float dashSpeed =
            DefaultDashSpeed;
        [SerializeField, Min(0.1f)] private float captureRadius = 0.65f;
        [SerializeField, Min(0.1f)] private float alignedDashDelay = 1.5f;
        [SerializeField, Min(0.1f)] private float dashDuration = 0.6f;
        [SerializeField, Min(0.1f)] private float dashCooldown = 3f;
        [SerializeField] private bool hunting;

        private float alignedElapsed;
        private float dashRemaining;
        private float dashCooldownRemaining;
        private float catchCooldownRemaining;
        private bool subscribed;

        public event Action HuntBegan;
        public event Action HuntStopped;
        public event Action<P8MaruTarget2D> TargetChanged;
        public event Action<P8MaruTarget2D> TargetReturned;

        public P8MaruRoomGraph2D RoomGraph => roomGraph;
        public P8ReturnPile2D ReturnPile => returnPile;
        public P8MaruBiteController2D BiteController => biteController;
        public P8MaruTarget2D CurrentTarget { get; private set; }
        public bool IsHunting => hunting;
        public bool IsDashing => dashRemaining > 0f;
        public float MoveSpeed => moveSpeed;

        public void Configure(
            P8MaruRoomGraph2D graph,
            P8ReturnPile2D pile,
            P8MaruBiteController2D bite,
            GameObject visual = null,
            float configuredMoveSpeed = DefaultMoveSpeed)
        {
            Unsubscribe();
            roomGraph = graph;
            returnPile = pile;
            biteController = bite;
            huntVisual = visual;
            moveSpeed = Mathf.Max(0.1f, configuredMoveSpeed);
            Subscribe();
            StopHunt();
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
            Tick(Time.deltaTime);
        }

        public void BeginHunt()
        {
            if (hunting)
            {
                return;
            }

            hunting = true;
            SetVisualActive(true);
            HuntBegan?.Invoke();
        }

        public void StopHunt()
        {
            if (!hunting)
            {
                SetVisualActive(false);
                return;
            }

            hunting = false;
            CurrentTarget = null;
            alignedElapsed = 0f;
            dashRemaining = 0f;
            dashCooldownRemaining = 0f;
            catchCooldownRemaining = 0f;
            SetVisualActive(false);
            HuntStopped?.Invoke();
        }

        public void TickForTests(float deltaSeconds)
        {
            Tick(deltaSeconds);
        }

        public P8MaruTarget2D SelectTarget()
        {
            P8MaruTarget2D best = null;
            int bestPriority = int.MaxValue;
            int bestGraphDistance = int.MaxValue;
            float bestWorldDistance = float.MaxValue;
            foreach (P8MaruTarget2D candidate
                     in P8MaruTarget2D.ActiveTargets)
            {
                if (candidate == null || !candidate.IsAvailable)
                {
                    continue;
                }

                int priority = (int)candidate.TargetKind;
                int graphDistance = roomGraph != null
                    ? roomGraph.Distance(
                        roomGraph.FindNodeId(transform.position),
                        roomGraph.FindNodeId(candidate.transform.position))
                    : 0;
                if (graphDistance < 0)
                {
                    graphDistance = int.MaxValue - 1;
                }

                float worldDistance =
                    ((Vector2)candidate.transform.position
                        - (Vector2)transform.position).sqrMagnitude;
                bool better =
                    priority < bestPriority
                    || (priority == bestPriority
                        && graphDistance < bestGraphDistance)
                    || (priority == bestPriority
                        && graphDistance == bestGraphDistance
                        && worldDistance < bestWorldDistance)
                    || (priority == bestPriority
                        && graphDistance == bestGraphDistance
                        && Mathf.Approximately(
                            worldDistance,
                            bestWorldDistance)
                        && (best == null
                            || candidate.StableOrder < best.StableOrder));
                if (!better)
                {
                    continue;
                }

                best = candidate;
                bestPriority = priority;
                bestGraphDistance = graphDistance;
                bestWorldDistance = worldDistance;
            }

            return best;
        }

        private void Tick(float deltaSeconds)
        {
            if (!hunting || deltaSeconds <= 0f)
            {
                return;
            }

            float delta = Mathf.Max(0f, deltaSeconds);
            dashCooldownRemaining =
                Mathf.Max(0f, dashCooldownRemaining - delta);
            catchCooldownRemaining =
                Mathf.Max(0f, catchCooldownRemaining - delta);
            if (biteController != null
                && biteController.State == P8BiteState.EscapeWindow)
            {
                return;
            }

            P8MaruTarget2D selected = SelectTarget();
            if (CurrentTarget != selected)
            {
                CurrentTarget = selected;
                TargetChanged?.Invoke(CurrentTarget);
            }

            if (CurrentTarget == null)
            {
                return;
            }

            Vector2 targetPosition = CurrentTarget.transform.position;
            Vector2 waypoint = roomGraph != null
                ? roomGraph.NextWaypoint(
                    transform.position,
                    targetPosition)
                : targetPosition;
            UpdateDash(targetPosition, delta);
            float speed = dashRemaining > 0f ? dashSpeed : moveSpeed;
            transform.position = Vector2.MoveTowards(
                transform.position,
                waypoint,
                speed * delta);

            if (((Vector2)transform.position - targetPosition).sqrMagnitude
                > captureRadius * captureRadius)
            {
                return;
            }

            if (CurrentTarget.TargetKind == P8MaruTargetKind.Player)
            {
                if (catchCooldownRemaining <= 0f)
                {
                    biteController?.TryBeginBite();
                }

                return;
            }

            P8MaruTarget2D returned = CurrentTarget;
            if (returnPile != null && returnPile.Deposit(returned))
            {
                TargetReturned?.Invoke(returned);
                CurrentTarget = null;
            }
        }

        private void UpdateDash(Vector2 targetPosition, float delta)
        {
            if (dashRemaining > 0f)
            {
                dashRemaining = Mathf.Max(0f, dashRemaining - delta);
                if (dashRemaining <= 0f)
                {
                    dashCooldownRemaining = dashCooldown;
                }

                return;
            }

            Vector2 deltaToTarget =
                targetPosition - (Vector2)transform.position;
            bool aligned =
                Mathf.Abs(deltaToTarget.y) <= 0.5f
                && Mathf.Abs(deltaToTarget.x) >= 2.5f;
            alignedElapsed = aligned
                ? alignedElapsed + delta
                : 0f;
            if (alignedElapsed >= alignedDashDelay
                && dashCooldownRemaining <= 0f)
            {
                dashRemaining = dashDuration;
                alignedElapsed = 0f;
            }
        }

        private void HandleFirstBiteEscaped()
        {
            catchCooldownRemaining = 3f;
            transform.position += Vector3.left * 1.5f;
        }

        private void HandleRunEnded(P8DeathCauseReport report)
        {
            StopHunt();
        }

        private void Subscribe()
        {
            if (subscribed || biteController == null)
            {
                return;
            }

            biteController.FirstBiteEscaped += HandleFirstBiteEscaped;
            biteController.RunEnded += HandleRunEnded;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || biteController == null)
            {
                return;
            }

            biteController.FirstBiteEscaped -= HandleFirstBiteEscaped;
            biteController.RunEnded -= HandleRunEnded;
            subscribed = false;
        }

        private void SetVisualActive(bool active)
        {
            if (huntVisual != null && huntVisual.activeSelf != active)
            {
                huntVisual.SetActive(active);
            }
        }
    }
}

#endif
