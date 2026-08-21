#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Objects;
using StarNight.Tools;
using UnityEngine;

namespace StarNight.Stages.P5
{
    [DisallowMultipleComponent]
    public abstract class P5ContextInteractable2D : MonoBehaviour
    {
        private static readonly HashSet<P5ContextInteractable2D> ActiveInternal =
            new HashSet<P5ContextInteractable2D>();

        [SerializeField] private Transform interactionPoint;
        [SerializeField, Min(0.1f)] private float interactionRadius = 1.5f;
        [SerializeField] private int interactionPriority;

        public static IReadOnlyCollection<P5ContextInteractable2D> Active =>
            ActiveInternal;

        public Transform InteractionPoint =>
            interactionPoint != null ? interactionPoint : transform;

        public float InteractionRadius => interactionRadius;
        public int InteractionPriority => interactionPriority;

        protected void ConfigureInteraction(
            Transform point,
            float radius,
            int priority)
        {
            interactionPoint = point;
            interactionRadius = Mathf.Max(0.1f, radius);
            interactionPriority = priority;
        }

        protected virtual void OnEnable()
        {
            ActiveInternal.Add(this);
        }

        protected virtual void OnDisable()
        {
            ActiveInternal.Remove(this);
        }

        public bool IsInRange(Vector2 actorPosition)
        {
            float maximumDistanceSquared =
                interactionRadius * interactionRadius;
            return ((Vector2)InteractionPoint.position - actorPosition).sqrMagnitude
                <= maximumDistanceSquared;
        }

        public bool TryInteractForTests(P5PlayerInteractionContext context)
        {
            return IsInRange(context.PlayerPosition)
                && CanInteract(context)
                && TryInteract(context);
        }

        protected abstract bool CanInteract(
            P5PlayerInteractionContext context);

        protected abstract bool TryInteract(
            P5PlayerInteractionContext context);

        public static bool TryInteractNearest(
            Transform playerTransform,
            CarrySystem carrySystem,
            PlayerToolInventory2D toolInventory)
        {
            if (playerTransform == null)
            {
                return false;
            }

            PlayerConsumableTools2D consumables =
                playerTransform.GetComponent<PlayerConsumableTools2D>();
            P5RunState2D runState = FindFirstObjectByType<P5RunState2D>();
            P5PlayerInteractionContext context =
                new P5PlayerInteractionContext(
                    playerTransform,
                    carrySystem,
                    toolInventory,
                    consumables,
                    runState);

            P5ContextInteractable2D best = null;
            float bestDistanceSquared = float.PositiveInfinity;
            int bestPriority = int.MinValue;
            int bestInstanceId = int.MaxValue;

            foreach (P5ContextInteractable2D candidate in ActiveInternal)
            {
                if (candidate == null
                    || !candidate.isActiveAndEnabled
                    || !candidate.IsInRange(context.PlayerPosition)
                    || !candidate.CanInteract(context))
                {
                    continue;
                }

                float distanceSquared =
                    ((Vector2)candidate.InteractionPoint.position
                        - context.PlayerPosition).sqrMagnitude;
                int priority = candidate.InteractionPriority;
                int instanceId = candidate.GetInstanceID();
                bool isBetter = priority > bestPriority
                    || (priority == bestPriority
                        && distanceSquared < bestDistanceSquared)
                    || (priority == bestPriority
                        && Mathf.Approximately(
                            distanceSquared,
                            bestDistanceSquared)
                        && instanceId < bestInstanceId);
                if (!isBetter)
                {
                    continue;
                }

                best = candidate;
                bestDistanceSquared = distanceSquared;
                bestPriority = priority;
                bestInstanceId = instanceId;
            }

            return best != null && best.TryInteract(context);
        }
    }
}

#endif
