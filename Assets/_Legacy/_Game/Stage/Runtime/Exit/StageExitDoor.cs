#if LEGACY_DISABLED
using System;
using StarNight.Interaction.Input;
using StarNight.Player.Motor;
using StarNight.Player.Presentation;
using StarNight.Stage.Flow;
using StarNight.Stage.Visuals;
using StarNight.Stage.CameraSystem;
using UnityEngine;

namespace StarNight.Stage.Exit
{
    [DisallowMultipleComponent]
    public sealed class StageExitDoor : MonoBehaviour
    {
        public const float InteractionDistance = 0.8f;
        public const float HoldSeconds = 0.5f;
        public const float ExitAnimationSeconds = 0.45f;
        public const string PromptText = "[X] 출항하기";

        private PlayerMotor2D player;
        private GameplayInputReader inputReader;
        private StageFlowController flow;
        private Camera worldCamera;
        private SpriteRenderer doorRenderer;
        private long activeActionId;
        private float holdElapsed;

        public event Action HoldStarted;
        public event Action HoldCanceled;
        public event Action HoldCompleted;

        public bool IsHolding => activeActionId != 0;
        public float HoldProgress => Mathf.Clamp01(holdElapsed / HoldSeconds);
        public bool IsPlayerInRange => player != null && Vector2.Distance(player.Body.position, transform.position) <= InteractionDistance;

        private void Update()
        {
            TryDiscoverFromCamera();
            if (IsHolding)
            {
                AdvanceHold(Time.unscaledDeltaTime, inputReader != null && inputReader.PrimaryHeld);
            }
        }

        public void Configure(PlayerMotor2D playerMotor, GameplayInputReader reader, StageFlowController stageFlow, Camera camera)
        {
            player = playerMotor;
            inputReader = reader;
            flow = stageFlow;
            worldCamera = camera;
            EnsureVisual();
        }

        public bool TryBeginHold(long actionId)
        {
            if (actionId <= 0 || !IsPlayerInRange || (flow != null && !flow.CanCommitExit))
            {
                return false;
            }

            if (IsHolding)
            {
                return activeActionId == actionId;
            }

            activeActionId = actionId;
            holdElapsed = 0f;
            HoldStarted?.Invoke();
            return true;
        }

        public bool AdvanceHold(float unscaledDeltaTime, bool primaryHeld)
        {
            if (!IsHolding)
            {
                return false;
            }

            if (!primaryHeld || !IsPlayerInRange || (flow != null && !flow.CanCommitExit))
            {
                CancelHold();
                return false;
            }

            holdElapsed += Mathf.Max(0f, unscaledDeltaTime);
            if (holdElapsed + 0.0001f < HoldSeconds)
            {
                return false;
            }

            activeActionId = 0;
            holdElapsed = HoldSeconds;
            HoldCompleted?.Invoke();
            flow?.RequestExit();
            return true;
        }

        public void CancelHold()
        {
            if (!IsHolding)
            {
                return;
            }

            activeActionId = 0;
            holdElapsed = 0f;
            HoldCanceled?.Invoke();
        }

        private void TryDiscoverFromCamera()
        {
            if (flow == null || flow.RuntimeState == null || flow.RuntimeState.exitDiscovered || worldCamera == null)
            {
                return;
            }

            Vector3 viewport = worldCamera.WorldToViewportPoint(transform.position);
            if (viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f)
            {
                flow.MarkExitDiscovered();
            }
        }

        private void EnsureVisual()
        {
            doorRenderer = GetComponent<SpriteRenderer>();
            if (doorRenderer == null)
            {
                doorRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            doorRenderer.sprite = PrototypeSpriteFactory.GetWhitePixel();
            doorRenderer.color = new Color(0.95f, 0.72f, 0.24f, 1f);
            doorRenderer.sortingOrder = 12;
            transform.localScale = new Vector3(0.65f, 1.7f, 1f);

            BoxCollider2D trigger = GetComponent<BoxCollider2D>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<BoxCollider2D>();
            }

            trigger.isTrigger = true;
            trigger.size = Vector2.one;

            GameplayClearZone clearZone = GetComponent<GameplayClearZone>();
            if (clearZone == null)
            {
                clearZone = gameObject.AddComponent<GameplayClearZone>();
            }
            clearZone.Configure(new Vector2(2.4f, 3f));

            CameraCriticalTarget criticalTarget = GetComponent<CameraCriticalTarget>();
            if (criticalTarget == null)
            {
                criticalTarget = gameObject.AddComponent<CameraCriticalTarget>();
            }
            criticalTarget.Configure(CameraCriticalTargetKind.Exit);
        }
    }
}

#endif
