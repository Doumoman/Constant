#if LEGACY_DISABLED
using System;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Interaction.Targeting;
using StarNight.Map;
using UnityEngine;

namespace StarNight.Tools.Umbrella
{
    public enum UmbrellaRuntimeState
    {
        Closed,
        Opening,
        Open,
        Closing,
    }

    [DisallowMultipleComponent]
    public sealed class UmbrellaActionController : MonoBehaviour,
        IPlayerAirMovementModifier,
        IBridgeUmbrellaState,
        IPalaceUmbrellaState
    {
        public const float OpenSeconds = 0.15f;
        public const float CloseSeconds = 0.10f;
        public const float MaximumFallSpeedValue = 3.2f;
        public const float MaximumHorizontalSpeedValue = 5f;
        public const float AirAccelerationMultiplierValue = 0.85f;
        public const float WindForceMultiplierValue = 1.8f;
        public const float WaterCurrentMultiplierValue = 0.7f;
        public const float CanopyAngleDegrees = 120f;
        public const float CanopyRadiusCells = 1.25f;
        public const float MaximumDeflectSpeed = 10f;

        [SerializeField] private PlayerActionLock actionLock;
        [SerializeField] private InteractionProbe interactionProbe;
        [SerializeField] private LayerMask projectileMask;

        private UmbrellaRuntimeState state;
        private WindUmbrellaRuntime activeTool;
        private PlayerHandSlot activeOwner;
        private long activeActionId;
        private float elapsedSeconds;

        public event Action<UmbrellaRuntimeState> StateChanged;
        public event Action<IUmbrellaDeflectableProjectile> ProjectileDeflected;

        public UmbrellaRuntimeState State => state;
        public bool IsUmbrellaOpen => state == UmbrellaRuntimeState.Open;
        public bool IsAirMovementModifierActive => IsUmbrellaOpen;
        public float MaximumFallSpeed => MaximumFallSpeedValue;
        public float MaximumHorizontalSpeed => MaximumHorizontalSpeedValue;
        public float AirAccelerationMultiplier => AirAccelerationMultiplierValue;
        public float WindForceMultiplier => IsUmbrellaOpen ? WindForceMultiplierValue : 1f;
        public float WaterCurrentMultiplier => IsUmbrellaOpen ? WaterCurrentMultiplierValue : 1f;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
            if (IsUmbrellaOpen)
            {
                DeflectNearbyProjectiles();
            }
        }

        private void OnDisable()
        {
            ForceClose();
        }

        public bool TryToggle(
            WindUmbrellaRuntime tool,
            PlayerHandSlot owner,
            PlayerActionContext context)
        {
            ResolveDependencies();
            if (state == UmbrellaRuntimeState.Open)
            {
                return TryClose(context);
            }
            if (state == UmbrellaRuntimeState.Opening)
            {
                return ForceClose();
            }
            if (state != UmbrellaRuntimeState.Closed
                || tool == null
                || owner == null
                || owner.CurrentItem != tool
                || actionLock != null
                && !actionLock.TryAcquire(context.ActionId, PlayerActionState.UsingTool))
            {
                return false;
            }

            activeTool = tool;
            activeOwner = owner;
            activeActionId = context.ActionId;
            SetState(UmbrellaRuntimeState.Opening);
            return true;
        }

        public bool TryClose(PlayerActionContext context)
        {
            if (state != UmbrellaRuntimeState.Open)
            {
                return false;
            }

            long openActionId = activeActionId;
            if (actionLock != null)
            {
                actionLock.TryRelease(openActionId, PlayerActionState.Carrying);
                if (!actionLock.TryAcquire(context.ActionId, PlayerActionState.UsingTool))
                {
                    ClearState();
                    return false;
                }
            }
            activeActionId = context.ActionId;
            SetState(UmbrellaRuntimeState.Closing);
            return true;
        }

        public bool ForceClose()
        {
            if (state == UmbrellaRuntimeState.Closed)
            {
                return false;
            }

            long closeActionId = activeActionId;
            ClearState();
            if (actionLock != null && actionLock.ActiveActionId == closeActionId)
            {
                actionLock.TryRelease(closeActionId, PlayerActionState.Carrying);
            }
            return true;
        }

        public void TickForTests(float deltaSeconds) => Tick(deltaSeconds);

        public void ConfigureForTests(
            PlayerActionLock configuredLock,
            InteractionProbe configuredProbe = null,
            LayerMask configuredProjectileMask = default)
        {
            actionLock = configuredLock;
            interactionProbe = configuredProbe;
            projectileMask = configuredProjectileMask;
        }

        public bool TryDeflectCandidateForTests(
            GameObject candidate,
            Vector2 relativePosition,
            int facingSign)
        {
            if (candidate == null)
            {
                return false;
            }
            IUmbrellaDeflectableProjectile projectile = FindProjectile(candidate.transform);
            return projectile != null && TryDeflectProjectile(projectile, relativePosition, facingSign);
        }

        public bool TryDeflectProjectile(
            IUmbrellaDeflectableProjectile projectile,
            Vector2 relativePosition,
            int facingSign)
        {
            if (!IsUmbrellaOpen
                || projectile == null
                || !projectile.CanUmbrellaDeflect
                || relativePosition.sqrMagnitude > CanopyRadiusCells * CanopyRadiusCells
                || relativePosition.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            int facing = facingSign < 0 ? -1 : 1;
            Vector2 canopyCenter = new Vector2(facing, 1f).normalized;
            if (Vector2.Angle(canopyCenter, relativePosition.normalized) > CanopyAngleDegrees * 0.5f)
            {
                return false;
            }

            Vector2 reflectedDirection = new Vector2(facing, 0.25f).normalized;
            if (!projectile.TryDeflect(reflectedDirection, MaximumDeflectSpeed, gameObject))
            {
                return false;
            }
            ProjectileDeflected?.Invoke(projectile);
            return true;
        }

        private void Tick(float deltaSeconds)
        {
            if (state == UmbrellaRuntimeState.Closed || deltaSeconds <= 0f)
            {
                return;
            }
            PlayerActionState expectedState = state == UmbrellaRuntimeState.Open
                ? PlayerActionState.UmbrellaOpen
                : PlayerActionState.UsingTool;
            if (actionLock != null
                && (actionLock.ActiveActionId != activeActionId
                    || actionLock.State != expectedState))
            {
                ClearState();
                return;
            }

            elapsedSeconds += deltaSeconds;
            if (state == UmbrellaRuntimeState.Opening && elapsedSeconds >= OpenSeconds)
            {
                actionLock?.TryTransition(activeActionId, PlayerActionState.UmbrellaOpen);
                SetState(UmbrellaRuntimeState.Open);
            }
            else if (state == UmbrellaRuntimeState.Closing && elapsedSeconds >= CloseSeconds)
            {
                long closedActionId = activeActionId;
                ClearState();
                if (actionLock != null && actionLock.ActiveActionId == closedActionId)
                {
                    actionLock.TryRelease(closedActionId, PlayerActionState.Carrying);
                }
            }
        }

        private void DeflectNearbyProjectiles()
        {
            int mask = projectileMask.value == 0 ? Physics2D.AllLayers : projectileMask.value;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, CanopyRadiusCells, mask);
            int facing = interactionProbe != null ? interactionProbe.FacingSign : 1;
            for (int index = 0; index < colliders.Length; index++)
            {
                Collider2D collider = colliders[index];
                if (collider == null || collider.transform.IsChildOf(transform))
                {
                    continue;
                }
                IUmbrellaDeflectableProjectile projectile = FindProjectile(collider.transform);
                if (projectile != null)
                {
                    TryDeflectProjectile(
                        projectile,
                        collider.bounds.center - transform.position,
                        facing);
                }
            }
        }

        private static IUmbrellaDeflectableProjectile FindProjectile(Transform start)
        {
            for (Transform current = start; current != null; current = current.parent)
            {
                MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
                for (int index = 0; index < behaviours.Length; index++)
                {
                    if (behaviours[index] is IUmbrellaDeflectableProjectile projectile)
                    {
                        return projectile;
                    }
                }
            }
            return null;
        }

        private void SetState(UmbrellaRuntimeState nextState)
        {
            state = nextState;
            elapsedSeconds = 0f;
            StateChanged?.Invoke(state);
        }

        private void ClearState()
        {
            state = UmbrellaRuntimeState.Closed;
            activeTool = null;
            activeOwner = null;
            activeActionId = 0;
            elapsedSeconds = 0f;
            StateChanged?.Invoke(state);
        }

        private void ResolveDependencies()
        {
            actionLock = actionLock != null ? actionLock : GetComponent<PlayerActionLock>();
            interactionProbe = interactionProbe != null ? interactionProbe : GetComponent<InteractionProbe>();
        }
    }
}

#endif
