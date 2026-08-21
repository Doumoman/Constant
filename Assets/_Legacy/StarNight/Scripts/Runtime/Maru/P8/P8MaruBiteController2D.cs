#if LEGACY_DISABLED
using System;
using StarNight.Objects;
using StarNight.Player;
using StarNight.Tools;
using UnityEngine;

namespace StarNight.Maru.P8
{
    [DefaultExecutionOrder(-40)]
    [DisallowMultipleComponent]
    public sealed class P8MaruBiteController2D : MonoBehaviour
    {
        public const float EscapeWindowSeconds = 2f;
        public const float RequiredHoldSeconds = 0.75f;
        public const int RequiredMashCount = 3;

        [SerializeField] private Transform player;
        [SerializeField] private Transform returnGate;
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private PlayerInputAdapter input;
        [SerializeField] private PlayerMotor2D motor;
        [SerializeField] private PlayerRecovery recovery;
        [SerializeField] private SafeCellTracker safeCellTracker;
        [SerializeField] private CarrySystem carrySystem;
        [SerializeField] private PlayerToolInventory2D toolInventory;
        [SerializeField] private P8MaruTimeline2D timeline;
        [SerializeField, Min(0.1f)] private float returnCarrySpeed = 5f;
        [SerializeField] private P8BiteState state = P8BiteState.Idle;

        private float escapeElapsed;
        private float heldElapsed;
        private int mashCount;

        public event Action<int> BiteStarted;
        public event Action FirstBiteEscaped;
        public event Action<P8DeathCauseReport> RunEnded;

        public P8BiteState State => state;
        public int BiteCount { get; private set; }
        public float EscapeElapsed => escapeElapsed;
        public float EscapeRemaining =>
            Mathf.Max(0f, EscapeWindowSeconds - escapeElapsed);
        public float HeldProgress01 =>
            Mathf.Clamp01(heldElapsed / RequiredHoldSeconds);
        public int MashCount => mashCount;
        public P8DeathCauseReport LastDeathReport { get; private set; }

        public void Configure(
            Transform playerTransform,
            Transform targetReturnGate,
            P8MaruTimeline2D maruTimeline,
            Rigidbody2D targetBody = null,
            PlayerInputAdapter inputAdapter = null,
            PlayerMotor2D playerMotor = null,
            PlayerRecovery playerRecovery = null,
            SafeCellTracker tracker = null,
            CarrySystem targetCarrySystem = null,
            PlayerToolInventory2D inventory = null)
        {
            player = playerTransform;
            returnGate = targetReturnGate;
            timeline = maruTimeline;
            playerBody = targetBody != null
                ? targetBody
                : player != null
                    ? player.GetComponent<Rigidbody2D>()
                    : null;
            input = inputAdapter != null
                ? inputAdapter
                : player != null
                    ? player.GetComponent<PlayerInputAdapter>()
                    : null;
            motor = playerMotor != null
                ? playerMotor
                : player != null
                    ? player.GetComponent<PlayerMotor2D>()
                    : null;
            recovery = playerRecovery != null
                ? playerRecovery
                : player != null
                    ? player.GetComponent<PlayerRecovery>()
                    : null;
            safeCellTracker = tracker != null
                ? tracker
                : player != null
                    ? player.GetComponent<SafeCellTracker>()
                    : null;
            carrySystem = targetCarrySystem != null
                ? targetCarrySystem
                : player != null
                    ? player.GetComponent<CarrySystem>()
                    : null;
            toolInventory = inventory != null
                ? inventory
                : player != null
                    ? player.GetComponent<PlayerToolInventory2D>()
                    : null;
            ResetStage();
        }

        private void Update()
        {
            if (state != P8BiteState.EscapeWindow)
            {
                return;
            }

            bool pressed =
                input != null && input.ConsumeInteractPressed();
            Tick(
                Time.deltaTime,
                input != null && input.InteractHeld,
                pressed);
        }

        private void OnDisable()
        {
            if (state == P8BiteState.EscapeWindow)
            {
                ReleaseControl(false);
            }
        }

        public bool TryBeginBite()
        {
            if (state == P8BiteState.EscapeWindow
                || state == P8BiteState.RunEnded)
            {
                return false;
            }

            BiteCount++;
            BiteStarted?.Invoke(BiteCount);
            if (BiteCount >= 2)
            {
                EndRun(P8RunEndKind.SecondMaruBite, true);
                return true;
            }

            escapeElapsed = 0f;
            heldElapsed = 0f;
            mashCount = 0;
            state = P8BiteState.EscapeWindow;
            LockControl(true);
            return true;
        }

        public void TickForTests(
            float deltaSeconds,
            bool interactHeld,
            bool interactPressed)
        {
            Tick(deltaSeconds, interactHeld, interactPressed);
        }

        public void ResetStage()
        {
            ReleaseControl(false);
            BiteCount = 0;
            escapeElapsed = 0f;
            heldElapsed = 0f;
            mashCount = 0;
            LastDeathReport = null;
            state = P8BiteState.Idle;
        }

        private void Tick(
            float deltaSeconds,
            bool interactHeld,
            bool interactPressed)
        {
            if (state != P8BiteState.EscapeWindow)
            {
                return;
            }

            float delta = Mathf.Max(0f, deltaSeconds);
            escapeElapsed += delta;
            if (interactHeld)
            {
                heldElapsed += delta;
            }

            if (interactPressed)
            {
                mashCount++;
            }

            MovePlayerTowardReturnGate(delta);
            if (heldElapsed + Mathf.Epsilon >= RequiredHoldSeconds
                || mashCount >= RequiredMashCount)
            {
                EscapeFirstBite();
                return;
            }

            if (escapeElapsed + Mathf.Epsilon >= EscapeWindowSeconds)
            {
                state = P8BiteState.FullyReturned;
                EndRun(P8RunEndKind.FailedFirstBiteEscape, false);
            }
        }

        private void EscapeFirstBite()
        {
            DropOneHeldObject();
            int damage = recovery != null
                ? recovery.ApplyDamage(1)
                : 0;
            if (recovery != null
                && damage > 0
                && recovery.CurrentHealth <= 0)
            {
                EndRun(P8RunEndKind.HealthDepleted, false);
                return;
            }

            state = P8BiteState.Escaped;
            ReleaseControl(false);
            FirstBiteEscaped?.Invoke();
        }

        private void DropOneHeldObject()
        {
            if (carrySystem != null && carrySystem.IsCarrying)
            {
                CarryableObject2D held = carrySystem.HeldObject;
                if (held != null
                    && held.IsImportant
                    && safeCellTracker != null
                    && safeCellTracker.HasSafeCell)
                {
                    Vector2 safePosition =
                        safeCellTracker.LastSafePosition;
                    held.SetRespawnPoint(safePosition);
                    if (carrySystem.DropHeldAt(
                            safePosition,
                            Vector2.zero))
                    {
                        return;
                    }
                }

                carrySystem.DropHeld();
                return;
            }

            if (toolInventory != null && toolInventory.HasHeldTool)
            {
                toolInventory.DropHeldTool();
            }
        }

        private void MovePlayerTowardReturnGate(float deltaSeconds)
        {
            if (player == null || returnGate == null)
            {
                return;
            }

            Vector2 current = playerBody != null
                ? playerBody.position
                : (Vector2)player.position;
            Vector2 next = Vector2.MoveTowards(
                current,
                returnGate.position,
                returnCarrySpeed * deltaSeconds);
            if (playerBody != null)
            {
                playerBody.position = next;
                playerBody.linearVelocity = Vector2.zero;
            }

            player.position = new Vector3(
                next.x,
                next.y,
                player.position.z);
        }

        private void EndRun(P8RunEndKind kind, bool secondBite)
        {
            state = P8BiteState.RunEnded;
            LockControl(false);
            LastDeathReport = new P8DeathCauseReport(
                kind,
                timeline != null
                    ? timeline.LastStatueImpact
                    : default,
                timeline != null
                    ? timeline.SecondsAdvanced
                    : 0f,
                timeline != null && timeline.StatueWasDestroyed,
                secondBite);
            RunEnded?.Invoke(LastDeathReport);
        }

        private void LockControl(bool allowInteract)
        {
            input?.SetGameplaySuppressed(true, allowInteract);
            motor?.SetControlLocked(true);
            carrySystem?.SetInteractionLocked(true);
        }

        private void ReleaseControl(bool keepSuppressed)
        {
            if (!keepSuppressed)
            {
                input?.SetGameplaySuppressed(false);
            }

            motor?.SetControlLocked(false);
            carrySystem?.SetInteractionLocked(false);
        }
    }
}

#endif
