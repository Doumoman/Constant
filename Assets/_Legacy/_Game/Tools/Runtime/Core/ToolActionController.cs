#if LEGACY_DISABLED
using System;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Interaction.Reactions;
using StarNight.Interaction.State;
using StarNight.Interaction.Targeting;
using StarNight.Map;
using UnityEngine;

namespace StarNight.Tools.Core
{
    [DisallowMultipleComponent]
    public sealed class ToolActionController : MonoBehaviour,
        IPlayerMovementSpeedModifier,
        IPlayerMovementOverride
    {
        public const float AirPoundDownwardSpeed = 10f;
        public const float AirPoundCollisionStopSeconds = 0.18f;
        [SerializeField] private PlayerActionLock actionLock;
        [SerializeField] private InteractionProbe interactionProbe;
        [SerializeField] private ProjectPhysicsProfile physicsProfile;
        [SerializeField] private ToolReactionDispatcher reactionDispatcher;
        [SerializeField] private Vector2 gridOrigin;
        [SerializeField, Min(0.01f)] private float cellSize = 1f;

        private IToolReactionWorld reactionWorld;
        private HandToolRuntime activeTool;
        private PlayerHandSlot activeOwner;
        private PlayerActionContext activeContext;
        private ToolActionProfile activeProfile;
        private ToolAimSolution lockedAim;
        private PlayerActionState returnState;
        private float elapsedSeconds;
        private bool impactDispatched;
        private Rigidbody2D playerBody;
        private bool airPoundActive;
        private bool airPoundCollided;
        private bool airPoundUsedSinceGrounded;
        private float airPoundStopElapsed;
        private float airPoundHorizontalVelocity;

        public event Action<HandToolRuntime, ToolAimSolution> ActionStarted;
        public event Action<HandToolRuntime, ToolDispatchReport> ImpactResolved;
        public event Action<HandToolRuntime, bool> ActionCompleted;

        public bool IsUsingTool => activeTool != null;
        public HandToolRuntime ActiveTool => activeTool;
        public ToolAimSolution LockedAim => lockedAim;
        public float ElapsedSeconds => elapsedSeconds;
        public int ImpactDispatchCount { get; private set; }
        public ToolDispatchReport LastReport { get; private set; }
        public float MovementSpeedMultiplier => IsUsingTool && activeProfile != null
            ? Mathf.Clamp01(activeProfile.MovementMultiplier)
            : 1f;
        public bool IsMovementOverrideActive => airPoundActive;
        public bool IsAirPoundActive => airPoundActive;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void Update()
        {
            if (!IsUsingTool && ResolveGrounded())
            {
                airPoundUsedSinceGrounded = false;
            }
            Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            CancelCurrentAction();
        }

        public bool TryStart(
            HandToolRuntime tool,
            PlayerHandSlot owner,
            PlayerActionContext context,
            int facingSign)
        {
            return TryStart(tool, owner, context, facingSign, ResolveGrounded());
        }

        public bool TryStart(
            HandToolRuntime tool,
            PlayerHandSlot owner,
            PlayerActionContext context,
            int facingSign,
            bool grounded)
        {
            ResolveDependencies();
            bool startingAirPound = !grounded && tool != null && tool.SupportsAirPound;
            if (IsUsingTool
                || tool == null
                || owner == null
                || owner.CurrentItem != tool
                || tool.Definition == null
                || !tool.ResourceState.HasUsableResource
                || startingAirPound && (airPoundUsedSinceGrounded || IsInsideVoidRecovery()))
            {
                return false;
            }

            PlayerActionState previous = actionLock != null ? actionLock.State : PlayerActionState.Carrying;
            if (actionLock != null && !actionLock.TryAcquire(context.ActionId, PlayerActionState.UsingTool))
            {
                return false;
            }

            activeTool = tool;
            activeOwner = owner;
            activeContext = context;
            activeProfile = grounded ? tool.Definition.GroundAction : tool.Definition.AirAction;
            returnState = previous == PlayerActionState.Free ? PlayerActionState.Carrying : previous;
            elapsedSeconds = 0f;
            impactDispatched = false;
            airPoundActive = startingAirPound;
            airPoundCollided = false;
            airPoundStopElapsed = 0f;
            if (startingAirPound)
            {
                airPoundUsedSinceGrounded = true;
                airPoundHorizontalVelocity = playerBody != null ? playerBody.linearVelocity.x * 0.25f : 0f;
            }
            else if (grounded)
            {
                airPoundUsedSinceGrounded = false;
            }
            LastReport = default;
            int lockedFacing = interactionProbe != null ? interactionProbe.FacingSign : facingSign;
            Vector2Int originCell = ToolAimResolver.WorldToCell(transform.position, gridOrigin, cellSize);
            lockedAim = ToolAimResolver.Resolve(activeProfile, originCell, lockedFacing, context.LookVertical);

            Animator animator = GetComponentInChildren<Animator>();
            if (animator != null && !string.IsNullOrWhiteSpace(activeProfile?.AnimatorTrigger))
            {
                animator.SetTrigger(activeProfile.AnimatorTrigger);
            }
            ActionStarted?.Invoke(activeTool, lockedAim);
            return true;
        }

        public void TickForTests(float deltaSeconds) => Tick(deltaSeconds);

        public bool NotifyAirPoundCollisionForTests()
        {
            if (!airPoundActive || airPoundCollided)
            {
                return false;
            }
            BeginAirPoundImpact();
            return true;
        }

        public void ApplyMovementOverride(Rigidbody2D body, float fixedDeltaTime)
        {
            if (!airPoundActive || body == null)
            {
                return;
            }
            body.linearVelocity = airPoundCollided
                ? Vector2.zero
                : new Vector2(airPoundHorizontalVelocity, -AirPoundDownwardSpeed);
        }

        public bool CancelCurrentAction(HandToolRuntime expectedTool = null)
        {
            if (!IsUsingTool || expectedTool != null && activeTool != expectedTool)
            {
                return false;
            }

            HandToolRuntime completedTool = activeTool;
            long actionId = activeContext.ActionId;
            ClearActiveAction();
            actionLock?.TryRelease(actionId, PlayerActionState.Carrying);
            ActionCompleted?.Invoke(completedTool, true);
            return true;
        }

        public void ConfigureForTests(
            PlayerActionLock configuredLock,
            IToolReactionWorld configuredWorld,
            Vector2 configuredGridOrigin,
            float configuredCellSize = 1f)
        {
            actionLock = configuredLock;
            reactionWorld = configuredWorld;
            gridOrigin = configuredGridOrigin;
            cellSize = Mathf.Max(0.01f, configuredCellSize);
        }

        private void Tick(float deltaSeconds)
        {
            if (!IsUsingTool || deltaSeconds <= 0f)
            {
                return;
            }

            elapsedSeconds += deltaSeconds;
            if (airPoundActive)
            {
                if (IsInsideVoidRecovery())
                {
                    CancelCurrentAction();
                    return;
                }
                if (!airPoundCollided)
                {
                    if (ResolveGrounded())
                    {
                        BeginAirPoundImpact();
                    }
                    return;
                }

                airPoundStopElapsed += deltaSeconds;
                if (airPoundStopElapsed >= AirPoundCollisionStopSeconds)
                {
                    CompleteCurrentAction();
                }
                return;
            }

            float impactAt = Mathf.Max(
                activeProfile != null ? activeProfile.WindupSeconds : 0f,
                activeProfile != null ? activeProfile.ImpactSeconds : 0f);
            if (!impactDispatched && elapsedSeconds >= impactAt)
            {
                DispatchImpact();
            }

            float total = activeProfile != null ? activeProfile.TotalSeconds : 0f;
            if (elapsedSeconds >= total)
            {
                CompleteCurrentAction();
            }
        }

        private void DispatchImpact()
        {
            impactDispatched = true;
            ImpactDispatchCount++;
            IToolReactionWorld world = reactionWorld ?? reactionDispatcher;
            ToolDispatchRequest request = new ToolDispatchRequest(
                    activeContext.ActionId,
                    activeTool.Definition.ToolTags,
                    lockedAim.OriginCell,
                    lockedAim.TargetCell,
                    lockedAim.Direction,
                    1f,
                    activeTool.gameObject,
                    gameObject,
                    gridOrigin,
                    cellSize);
            LastReport = activeTool.DispatchImpact(world, request);
            activeTool.ResourceState.TryConsumeForSuccessfulReaction(LastReport.ConsumeToolResource);
            ImpactResolved?.Invoke(activeTool, LastReport);
        }

        private void CompleteCurrentAction()
        {
            HandToolRuntime completedTool = activeTool;
            long actionId = activeContext.ActionId;
            PlayerActionState finalState = activeOwner != null && activeOwner.CurrentItem == activeTool
                ? returnState
                : PlayerActionState.Free;
            ClearActiveAction();
            actionLock?.TryRelease(actionId, finalState);
            ActionCompleted?.Invoke(completedTool, false);
        }

        private void ClearActiveAction()
        {
            activeTool = null;
            activeOwner = null;
            activeProfile = null;
            elapsedSeconds = 0f;
            impactDispatched = false;
            airPoundActive = false;
            airPoundCollided = false;
            airPoundStopElapsed = 0f;
        }

        private void BeginAirPoundImpact()
        {
            airPoundCollided = true;
            airPoundStopElapsed = 0f;
            if (playerBody != null)
            {
                playerBody.linearVelocity = Vector2.zero;
            }
            if (!impactDispatched)
            {
                DispatchImpact();
            }
        }

        private bool ResolveGrounded()
        {
            LayerMask mask = physicsProfile != null && physicsProfile.GroundMask.value != 0
                ? physicsProfile.GroundMask
                : LayerMask.GetMask("TerrainSolid", "TerrainOneWay", "DynamicObject");
            if (mask.value == 0)
            {
                return true;
            }
            Collider2D actorCollider = GetComponent<Collider2D>();
            if (actorCollider != null && actorCollider.IsTouchingLayers(mask))
            {
                return true;
            }
            Vector2 origin = actorCollider != null
                ? new Vector2(actorCollider.bounds.center.x, actorCollider.bounds.min.y + 0.02f)
                : (Vector2)transform.position;
            return Physics2D.Raycast(origin, Vector2.down, 0.10f, mask).collider != null;
        }

        private bool IsInsideVoidRecovery()
        {
            LayerMask mask = physicsProfile != null && physicsProfile.VoidRecoveryMask.value != 0
                ? physicsProfile.VoidRecoveryMask
                : LayerMask.GetMask("VoidRecovery");
            return mask.value != 0 && Physics2D.OverlapPoint(transform.position, mask) != null;
        }

        private void ResolveDependencies()
        {
            actionLock = actionLock != null ? actionLock : GetComponent<PlayerActionLock>();
            interactionProbe = interactionProbe != null ? interactionProbe : GetComponent<InteractionProbe>();
            reactionDispatcher = reactionDispatcher != null
                ? reactionDispatcher
                : GetComponent<ToolReactionDispatcher>();
            reactionWorld ??= reactionDispatcher;
            playerBody = playerBody != null ? playerBody : GetComponent<Rigidbody2D>();
        }
    }
}

#endif
