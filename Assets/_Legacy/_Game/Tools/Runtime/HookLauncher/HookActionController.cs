#if LEGACY_DISABLED
using System;
using StarNight.Interaction.Carry;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Interaction.State;
using StarNight.Interaction.Targeting;
using StarNight.Map;
using UnityEngine;

namespace StarNight.Tools.HookLauncher
{
    [DisallowMultipleComponent]
    public sealed class HookActionController : MonoBehaviour, IPlayerMovementOverride
    {
        public const float MaximumRangeCells = 7f;
        public const float MaximumLatchDistanceCells = 8f;
        public const float FireSeconds = 0.12f;
        public const float MissRetractSeconds = 0.15f;
        public const float CooldownSeconds = 0.25f;
        public const float PlayerPullSpeed = 12f;
        public const float PlayerStopDistance = 0.65f;
        public const float LightPullSpeed = 10f;
        public const float MediumPullSpeed = 7f;
        public const float HeavyPullSpeed = 4f;

        [SerializeField] private PlayerActionLock actionLock;
        [SerializeField] private InteractionProbe interactionProbe;
        [SerializeField] private ProjectPhysicsProfile physicsProfile;
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private CapsuleCollider2D playerCapsule;
        [SerializeField] private RectInt roomBounds;
        [SerializeField] private Vector2 gridOrigin;
        [SerializeField, Min(0.01f)] private float cellSize = 1f;

        private IHookWorld hookWorld;
        private HookLauncherRuntime activeTool;
        private PlayerHandSlot activeOwner;
        private HookLatch latch;
        private HookRuntimeState state;
        private Vector2 fireDirection;
        private long activeActionId;
        private float elapsedSeconds;

        public event Action<HookRuntimeState> StateChanged;

        public HookRuntimeState State => state;
        public HookLatch CurrentLatch => latch;
        public bool IsMovementOverrideActive => state == HookRuntimeState.PullingPlayer;
        public bool IsActive => state != HookRuntimeState.Idle;
        public FeedbackId LastFeedback { get; private set; }

        private void Awake()
        {
            ResolveDependencies();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            TickObjectPull(Time.fixedDeltaTime);
        }

        private void OnDisable()
        {
            CancelHook();
        }

        public bool TryFire(
            HookLauncherRuntime tool,
            PlayerHandSlot owner,
            PlayerActionContext context,
            int facingSign,
            float lookVertical)
        {
            ResolveDependencies();
            if (state != HookRuntimeState.Idle
                || tool == null
                || owner == null
                || owner.CurrentItem != tool
                || tool.Definition == null
                || actionLock != null
                && !actionLock.TryAcquire(context.ActionId, PlayerActionState.UsingTool))
            {
                return false;
            }

            activeTool = tool;
            activeOwner = owner;
            activeActionId = context.ActionId;
            fireDirection = lookVertical > 0.5f
                ? Vector2.up
                : new Vector2(facingSign < 0 ? -1f : 1f, 0f);
            LastFeedback = FeedbackId.None;
            SetState(HookRuntimeState.Firing);
            return true;
        }

        public bool TryPull(PlayerActionContext context)
        {
            if (state != HookRuntimeState.LatchedWorld
                && state != HookRuntimeState.LatchedObject)
            {
                return false;
            }

            long latchActionId = activeActionId;
            if (actionLock != null)
            {
                actionLock.TryRelease(latchActionId, PlayerActionState.Carrying);
                if (!actionLock.TryAcquire(context.ActionId, PlayerActionState.HookPulling))
                {
                    ClearState();
                    return false;
                }
            }
            activeActionId = context.ActionId;

            switch (latch.Response)
            {
                case HookResponse.PullToPlayer:
                    SetState(HookRuntimeState.PullingObject);
                    return true;
                case HookResponse.PullPlayerToTarget:
                    SetState(HookRuntimeState.PullingPlayer);
                    return true;
                case HookResponse.Trigger:
                    bool triggered = latch.Trigger != null
                        && latch.Trigger.TryTriggerHook(context.ActionId, gameObject);
                    LastFeedback = triggered ? FeedbackId.Activate : FeedbackId.Accepted;
                    BeginCooldown();
                    return true;
                default:
                    LastFeedback = FeedbackId.MetalFail;
                    BeginCooldown();
                    return false;
            }
        }

        public bool CancelHook()
        {
            if (state == HookRuntimeState.Idle)
            {
                return false;
            }

            long cancelledActionId = activeActionId;
            ClearState();
            if (actionLock != null && actionLock.ActiveActionId == cancelledActionId)
            {
                actionLock.TryRelease(cancelledActionId, PlayerActionState.Carrying);
            }
            return true;
        }

        public void ApplyMovementOverride(Rigidbody2D body, float fixedDeltaTime)
        {
            if (state != HookRuntimeState.PullingPlayer || body == null || fixedDeltaTime <= 0f)
            {
                return;
            }
            if (!ValidateLatchDistance())
            {
                body.linearVelocity = Vector2.zero;
                CancelHook();
                return;
            }

            Vector2 current = body.position;
            Vector2 towardTarget = latch.Position - current;
            float distance = towardTarget.magnitude;
            if (distance <= PlayerStopDistance + 0.02f)
            {
                body.linearVelocity = Vector2.zero;
                BeginCooldown();
                return;
            }

            Vector2 stopPosition = latch.Position - towardTarget.normalized * PlayerStopDistance;
            Vector2 desired = Vector2.MoveTowards(current, stopPosition, PlayerPullSpeed * fixedDeltaTime);
            Vector2 capsuleSize = playerCapsule != null ? playerCapsule.size : new Vector2(0.72f, 0.92f);
            if (!(hookWorld ??= new PhysicsHookWorld(physicsProfile)).TryResolveStep(
                current,
                desired,
                capsuleSize,
                gameObject,
                latch.Target,
                out Vector2 resolved))
            {
                body.position = resolved;
                transform.position = resolved;
                body.linearVelocity = Vector2.zero;
                BeginCooldown();
                return;
            }

            body.linearVelocity = (resolved - current) / fixedDeltaTime;
        }

        public void TickForTests(float deltaSeconds) => Tick(deltaSeconds);
        public void FixedTickForTests(float fixedDeltaTime) => TickObjectPull(fixedDeltaTime);

        public void ConfigureForTests(
            PlayerActionLock configuredLock,
            Rigidbody2D configuredBody,
            CapsuleCollider2D configuredCapsule,
            IHookWorld configuredWorld,
            RectInt configuredRoomBounds = default)
        {
            actionLock = configuredLock;
            playerBody = configuredBody;
            playerCapsule = configuredCapsule;
            hookWorld = configuredWorld;
            roomBounds = configuredRoomBounds;
        }

        private void Tick(float deltaSeconds)
        {
            if (state == HookRuntimeState.Idle || deltaSeconds <= 0f)
            {
                return;
            }

            if (actionLock != null
                && (actionLock.ActiveActionId != activeActionId
                    || actionLock.State != ExpectedActionState()))
            {
                ClearState();
                return;
            }

            if ((state == HookRuntimeState.LatchedWorld
                    || state == HookRuntimeState.LatchedObject
                    || state == HookRuntimeState.PullingPlayer
                    || state == HookRuntimeState.PullingObject)
                && !ValidateLatchDistance())
            {
                CancelHook();
                return;
            }

            elapsedSeconds += deltaSeconds;
            switch (state)
            {
                case HookRuntimeState.Firing when elapsedSeconds >= FireSeconds:
                    ResolveFire();
                    break;
                case HookRuntimeState.MissRetract when elapsedSeconds >= MissRetractSeconds:
                    BeginCooldown();
                    break;
                case HookRuntimeState.Cooldown when elapsedSeconds >= CooldownSeconds:
                    FinishCooldown();
                    break;
            }
        }

        private void ResolveFire()
        {
            HookFireQuery query = new HookFireQuery(
                transform.position,
                fireDirection,
                MaximumRangeCells * cellSize,
                gameObject,
                roomBounds,
                gridOrigin,
                cellSize);
            if (!(hookWorld ??= new PhysicsHookWorld(physicsProfile)).TryAcquire(query, out latch)
                || !latch.IsValid)
            {
                SetState(HookRuntimeState.MissRetract);
                return;
            }
            if (latch.Response == HookResponse.Reject)
            {
                LastFeedback = FeedbackId.MetalFail;
                SetState(HookRuntimeState.MissRetract);
                return;
            }

            actionLock?.TryTransition(activeActionId, PlayerActionState.HookLatched);
            SetState(latch.TargetBody == null && latch.Response == HookResponse.PullPlayerToTarget
                ? HookRuntimeState.LatchedWorld
                : HookRuntimeState.LatchedObject);
        }

        private void TickObjectPull(float fixedDeltaTime)
        {
            if (state != HookRuntimeState.PullingObject
                || fixedDeltaTime <= 0f
                || latch.TargetBody == null)
            {
                return;
            }
            if (!ValidateLatchDistance())
            {
                CancelHook();
                return;
            }

            float speed = ResolveObjectPullSpeed(latch.Target);
            Vector2 current = latch.TargetBody.position;
            Vector2 desired = Vector2.MoveTowards(current, transform.position, speed * fixedDeltaTime);
            Vector2 size = ResolveTargetSize(latch.Target);
            if (!(hookWorld ??= new PhysicsHookWorld(physicsProfile)).TryResolveStep(
                current,
                desired,
                size,
                latch.Target,
                gameObject,
                out Vector2 resolved))
            {
                latch.TargetBody.linearVelocity = Vector2.zero;
                BeginCooldown();
                return;
            }

            latch.TargetBody.MovePosition(resolved);
            if (Vector2.Distance(resolved, transform.position) <= 0.80f)
            {
                latch.TargetBody.linearVelocity = Vector2.zero;
                BeginCooldown();
            }
        }

        private float ResolveObjectPullSpeed(GameObject target)
        {
            CarryableObject carryable = target != null ? target.GetComponentInParent<CarryableObject>() : null;
            CarryWeightClass weight = carryable != null && carryable.Definition != null
                ? carryable.Definition.WeightClass
                : CarryWeightClass.Light;
            return weight switch
            {
                CarryWeightClass.Medium => MediumPullSpeed,
                CarryWeightClass.Heavy => HeavyPullSpeed,
                CarryWeightClass.Fixed => 0f,
                _ => LightPullSpeed,
            };
        }

        private static Vector2 ResolveTargetSize(GameObject target)
        {
            Collider2D collider = target != null ? target.GetComponentInChildren<Collider2D>() : null;
            return collider != null ? collider.bounds.size : Vector2.one * 0.8f;
        }

        private bool ValidateLatchDistance()
        {
            return latch.IsValid
                && Vector2.Distance(transform.position, latch.Position)
                <= MaximumLatchDistanceCells * cellSize;
        }

        private void BeginCooldown()
        {
            if (state == HookRuntimeState.Idle)
            {
                return;
            }
            if (actionLock != null && actionLock.ActiveActionId == activeActionId)
            {
                actionLock.TryTransition(activeActionId, PlayerActionState.UsingTool);
            }
            SetState(HookRuntimeState.Cooldown);
        }

        private PlayerActionState ExpectedActionState()
        {
            return state switch
            {
                HookRuntimeState.LatchedWorld => PlayerActionState.HookLatched,
                HookRuntimeState.LatchedObject => PlayerActionState.HookLatched,
                HookRuntimeState.PullingPlayer => PlayerActionState.HookPulling,
                HookRuntimeState.PullingObject => PlayerActionState.HookPulling,
                _ => PlayerActionState.UsingTool,
            };
        }

        private void FinishCooldown()
        {
            long completedActionId = activeActionId;
            ClearState();
            if (actionLock != null && actionLock.ActiveActionId == completedActionId)
            {
                actionLock.TryRelease(completedActionId, PlayerActionState.Carrying);
            }
        }

        private void SetState(HookRuntimeState nextState)
        {
            state = nextState;
            elapsedSeconds = 0f;
            StateChanged?.Invoke(state);
        }

        private void ClearState()
        {
            state = HookRuntimeState.Idle;
            activeTool = null;
            activeOwner = null;
            latch = default;
            activeActionId = 0;
            elapsedSeconds = 0f;
            fireDirection = Vector2.zero;
            StateChanged?.Invoke(state);
        }

        private void ResolveDependencies()
        {
            actionLock = actionLock != null ? actionLock : GetComponent<PlayerActionLock>();
            interactionProbe = interactionProbe != null ? interactionProbe : GetComponent<InteractionProbe>();
            playerBody = playerBody != null ? playerBody : GetComponent<Rigidbody2D>();
            playerCapsule = playerCapsule != null ? playerCapsule : GetComponent<CapsuleCollider2D>();
            hookWorld ??= new PhysicsHookWorld(physicsProfile);
        }
    }
}

#endif
