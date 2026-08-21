#if LEGACY_DISABLED
using System;
using StarNight.Interaction.Carry;
using StarNight.Map;
using UnityEngine;

namespace StarNight.Tools.HookLauncher
{
    public enum HookRuntimeState
    {
        Idle,
        Firing,
        MissRetract,
        LatchedWorld,
        LatchedObject,
        PullingPlayer,
        PullingObject,
        Cooldown,
    }

    public readonly struct HookFireQuery
    {
        public HookFireQuery(
            Vector2 origin,
            Vector2 direction,
            float maximumDistance,
            GameObject instigator,
            RectInt roomBounds,
            Vector2 gridOrigin,
            float cellSize)
        {
            Origin = origin;
            Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            MaximumDistance = Mathf.Max(0f, maximumDistance);
            Instigator = instigator;
            RoomBounds = roomBounds;
            GridOrigin = gridOrigin;
            CellSize = Mathf.Max(0.01f, cellSize);
        }

        public Vector2 Origin { get; }
        public Vector2 Direction { get; }
        public float MaximumDistance { get; }
        public GameObject Instigator { get; }
        public RectInt RoomBounds { get; }
        public Vector2 GridOrigin { get; }
        public float CellSize { get; }
    }

    public readonly struct HookLatch
    {
        public HookLatch(
            GameObject target,
            HookResponse response,
            Rigidbody2D targetBody = null,
            IHookTrigger trigger = null)
        {
            Target = target;
            Response = response;
            TargetBody = targetBody;
            Trigger = trigger;
        }

        public GameObject Target { get; }
        public HookResponse Response { get; }
        public Rigidbody2D TargetBody { get; }
        public IHookTrigger Trigger { get; }
        public bool IsValid => Target != null;
        public Vector2 Position => Target != null ? Target.transform.position : Vector2.zero;
    }

    public interface IHookTrigger
    {
        bool TryTriggerHook(long actionId, GameObject instigator);
    }

    public interface IHookWorld
    {
        bool TryAcquire(HookFireQuery query, out HookLatch latch);
        bool TryResolveStep(
            Vector2 current,
            Vector2 desired,
            Vector2 capsuleSize,
            GameObject mover,
            GameObject ignoredTarget,
            out Vector2 resolvedPosition);
    }

}

#endif
