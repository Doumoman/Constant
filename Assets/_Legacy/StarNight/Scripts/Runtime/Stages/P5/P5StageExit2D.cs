#if LEGACY_DISABLED
using System;
using StarNight.Player;
using UnityEngine;

namespace StarNight.Stages.P5
{
    [DisallowMultipleComponent]
    public sealed class P5StageExit2D : MonoBehaviour
    {
        public const float RequiredHoldSeconds = 0.5f;

        [SerializeField] private PlayerInputAdapter input;
        [SerializeField] private Transform player;
        [SerializeField] private P5StageCoreLoop2D coreLoop;
        [SerializeField] private P5StageBacktrackCue2D firstReachCue;
        [SerializeField] private Transform interactionPoint;
        [SerializeField] private SpriteRenderer glowRenderer;
        [SerializeField, Min(0.01f)] private float holdDuration =
            RequiredHoldSeconds;
        [SerializeField, Min(0.1f)] private float activationRadius = 1.35f;
        [SerializeField] private P5StageExitState state =
            P5StageExitState.Unseen;

        private Color baseGlowColor = Color.white;
        private bool firstReachRaised;

        public event Action<P5StageExitState> StateChanged;
        public event Action FirstReached;
        public event Action Departed;
        public event Action<float> HoldProgressChanged;

        public P5StageExitState State => state;
        public float HoldDuration => holdDuration;
        public float HoldElapsedSeconds { get; private set; }
        public float HoldProgress01 => holdDuration > 0f
            ? Mathf.Clamp01(HoldElapsedSeconds / holdDuration)
            : 0f;
        public bool IsPlayerInRange { get; private set; }
        public Transform InteractionPoint =>
            interactionPoint != null ? interactionPoint : transform;

        public void Configure(
            PlayerInputAdapter inputAdapter,
            Transform playerTransform,
            P5StageCoreLoop2D targetCoreLoop,
            P5StageBacktrackCue2D targetFirstReachCue,
            Transform targetInteractionPoint = null,
            SpriteRenderer targetGlowRenderer = null,
            float requiredHoldSeconds = RequiredHoldSeconds,
            float radius = 1.35f)
        {
            if (!Mathf.Approximately(
                    requiredHoldSeconds,
                    RequiredHoldSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredHoldSeconds),
                    requiredHoldSeconds,
                    "The P5 exit requires a continuous 0.5 second hold.");
            }

            input = inputAdapter;
            player = playerTransform;
            coreLoop = targetCoreLoop;
            firstReachCue = targetFirstReachCue;
            interactionPoint = targetInteractionPoint;
            glowRenderer = targetGlowRenderer;
            holdDuration = RequiredHoldSeconds;
            activationRadius = Mathf.Max(0.1f, radius);
            baseGlowColor = glowRenderer != null
                ? glowRenderer.color
                : Color.white;
            if (glowRenderer != null)
            {
                glowRenderer.gameObject.SetActive(true);
            }

            ResetExitForTests();
        }

        private void Update()
        {
            if (player == null
                || input == null
                || (coreLoop != null && !coreLoop.CanAcceptExitInput))
            {
                return;
            }

            bool inRange =
                ((Vector2)InteractionPoint.position
                    - (Vector2)player.position).sqrMagnitude
                <= activationRadius * activationRadius;
            Tick(Time.deltaTime, inRange, input.InteractHeld);
        }

        public void TickForTests(
            float deltaSeconds,
            bool playerInRange,
            bool interactHeld)
        {
            Tick(deltaSeconds, playerInRange, interactHeld);
        }

        public void ResetExitForTests()
        {
            state = P5StageExitState.Unseen;
            HoldElapsedSeconds = 0f;
            IsPlayerInRange = false;
            firstReachRaised = false;
            SetGlow(false);
            HoldProgressChanged?.Invoke(0f);
        }

        private void Tick(
            float deltaSeconds,
            bool playerInRange,
            bool interactHeld)
        {
            if (state == P5StageExitState.Departed)
            {
                return;
            }

            IsPlayerInRange = playerInRange;
            if (!playerInRange)
            {
                ResetConfirmation();
                return;
            }

            if (!firstReachRaised)
            {
                firstReachRaised = true;
                SetState(P5StageExitState.Reached);
                SetGlow(true);
                firstReachCue?.PlayOnce();
                FirstReached?.Invoke();
                coreLoop?.NotifyExitReached(this);
            }

            if (!interactHeld)
            {
                ResetConfirmation();
                return;
            }

            if (state != P5StageExitState.Confirming)
            {
                SetState(P5StageExitState.Confirming);
            }

            HoldElapsedSeconds += Mathf.Max(0f, deltaSeconds);
            HoldProgressChanged?.Invoke(HoldProgress01);
            if (HoldElapsedSeconds + Mathf.Epsilon < holdDuration)
            {
                return;
            }

            HoldElapsedSeconds = holdDuration;
            HoldProgressChanged?.Invoke(1f);
            SetState(P5StageExitState.Departed);
            Departed?.Invoke();
            coreLoop?.NotifyExitDeparted(this);
        }

        private void ResetConfirmation()
        {
            if (HoldElapsedSeconds > 0f)
            {
                HoldElapsedSeconds = 0f;
                HoldProgressChanged?.Invoke(0f);
            }

            if (firstReachRaised
                && state == P5StageExitState.Confirming)
            {
                SetState(P5StageExitState.Reached);
            }
        }

        private void SetState(P5StageExitState next)
        {
            if (state == next)
            {
                return;
            }

            state = next;
            StateChanged?.Invoke(state);
        }

        private void SetGlow(bool lit)
        {
            if (glowRenderer == null)
            {
                return;
            }

            Color color = baseGlowColor;
            color.a = lit
                ? Mathf.Max(baseGlowColor.a, 0.95f)
                : Mathf.Min(baseGlowColor.a, 0.35f);
            glowRenderer.color = color;
        }
    }
}

#endif
