#if LEGACY_DISABLED
using StarNight.Map;
using UnityEngine;

namespace StarNight.Interaction.Reactions
{
    public readonly struct ToolDamageEvent
    {
        public ToolDamageEvent(
            long actionId,
            int damage,
            Vector2 knockback,
            ToolTag tags,
            GameObject source,
            GameObject instigator)
        {
            ActionId = actionId;
            Damage = Mathf.Max(0, damage);
            Knockback = knockback;
            Tags = tags;
            Source = source;
            Instigator = instigator;
        }

        public long ActionId { get; }
        public int Damage { get; }
        public Vector2 Knockback { get; }
        public ToolTag Tags { get; }
        public GameObject Source { get; }
        public GameObject Instigator { get; }
    }

    public interface IToolDamageReceiver
    {
        bool TryReceiveToolDamage(ToolDamageEvent damageEvent);
    }

    public readonly struct ToolDispatchRequest
    {
        public ToolDispatchRequest(
            long actionId,
            ToolTag tags,
            Vector2Int originCell,
            Vector2Int targetCell,
            Vector2Int direction,
            float magnitude,
            GameObject source,
            GameObject instigator,
            Vector2 gridOrigin,
            float cellSize)
        {
            ActionId = actionId;
            Tags = tags;
            OriginCell = originCell;
            TargetCell = targetCell;
            Direction = direction;
            Magnitude = magnitude;
            Source = source;
            Instigator = instigator;
            GridOrigin = gridOrigin;
            CellSize = Mathf.Max(0.01f, cellSize);
        }

        public long ActionId { get; }
        public ToolTag Tags { get; }
        public Vector2Int OriginCell { get; }
        public Vector2Int TargetCell { get; }
        public Vector2Int Direction { get; }
        public float Magnitude { get; }
        public GameObject Source { get; }
        public GameObject Instigator { get; }
        public Vector2 GridOrigin { get; }
        public float CellSize { get; }
        public Vector2 TargetWorldCenter => GridOrigin + (Vector2)TargetCell * CellSize;
    }

    public readonly struct ToolDispatchReport
    {
        public ToolDispatchReport(
            bool mapAccepted,
            bool entityAccepted,
            bool consumeToolResource,
            FeedbackId feedback,
            int mapReceiverCount,
            int entityReceiverCount,
            bool blocksPropagation = false)
        {
            MapAccepted = mapAccepted;
            EntityAccepted = entityAccepted;
            ConsumeToolResource = consumeToolResource;
            Feedback = feedback;
            MapReceiverCount = mapReceiverCount;
            EntityReceiverCount = entityReceiverCount;
            BlocksPropagation = blocksPropagation;
        }

        public bool MapAccepted { get; }
        public bool EntityAccepted { get; }
        public bool Accepted => MapAccepted || EntityAccepted;
        public bool ConsumeToolResource { get; }
        public FeedbackId Feedback { get; }
        public int MapReceiverCount { get; }
        public int EntityReceiverCount { get; }
        public bool BlocksPropagation { get; }

        public static ToolDispatchReport Rejected(FeedbackId feedback = FeedbackId.None) =>
            new ToolDispatchReport(false, false, false, feedback, 0, 0);
    }

    public interface IToolReactionWorld
    {
        ToolDispatchReport Dispatch(ToolDispatchRequest request);
    }

    [DisallowMultipleComponent]
    public sealed class ToolReactionDispatcher : MonoBehaviour, IToolReactionWorld
    {
        private readonly TileMutationService tileMutationService = new TileMutationService();

        [SerializeField] private LayerMask targetMask;
        [SerializeField, Range(0.1f, 1f)] private float querySizeRatio = 0.82f;

        public ToolDispatchReport Dispatch(ToolDispatchRequest request)
        {
            Vector2 size = Vector2.one * request.CellSize * Mathf.Clamp(querySizeRatio, 0.1f, 1f);
            Collider2D[] overlaps = targetMask.value == 0
                ? Physics2D.OverlapBoxAll(request.TargetWorldCenter, size, 0f)
                : Physics2D.OverlapBoxAll(request.TargetWorldCenter, size, 0f, targetMask);

            IToolReactionReceiver mapReceiver = null;
            IToolDamageReceiver damageReceiver = null;
            bool immutableBoundary = false;
            bool blocksPropagation = false;
            for (int index = 0; index < overlaps.Length; index++)
            {
                Collider2D overlap = overlaps[index];
                if (overlap == null || overlap.transform.IsChildOf(request.Instigator?.transform))
                {
                    continue;
                }

                int layer = overlap.gameObject.layer;
                immutableBoundary |= layer == LayerMask.NameToLayer("UnbreakableBoundary")
                    || layer == LayerMask.NameToLayer("PortalBoundary");
                blocksPropagation |= layer == LayerMask.NameToLayer("TerrainSolid")
                    || layer == LayerMask.NameToLayer("UnbreakableBoundary")
                    || layer == LayerMask.NameToLayer("PortalBoundary");
                mapReceiver ??= FindInterfaceInParents<IToolReactionReceiver>(overlap.transform);
                damageReceiver ??= FindInterfaceInParents<IToolDamageReceiver>(overlap.transform);
            }

            return DispatchDirect(request, mapReceiver, damageReceiver, immutableBoundary, blocksPropagation);
        }

        public ToolDispatchReport DispatchDirect(
            ToolDispatchRequest request,
            IToolReactionReceiver mapReceiver,
            IToolDamageReceiver damageReceiver,
            bool immutableBoundary = false,
            bool blocksPropagation = false)
        {
            if (immutableBoundary)
            {
                return new ToolDispatchReport(false, false, false, FeedbackId.MetalFail, 0, 0, true);
            }

            ToolReactionResult mapResult = mapReceiver != null
                ? tileMutationService.TryApply(mapReceiver, new ToolReactionContext
                {
                    ActionId = ToReactionActionId(request.ActionId),
                    Tags = request.Tags,
                    OriginCell = new GridCell(request.OriginCell),
                    TargetCell = new GridCell(request.TargetCell),
                    Direction = request.Direction,
                    Magnitude = request.Magnitude,
                    Source = request.Source,
                    Instigator = request.Instigator,
                })
                : ToolReactionResult.Rejected(FeedbackId.None);

            if (mapResult.Feedback == FeedbackId.MetalFail)
            {
                return new ToolDispatchReport(false, false, false, FeedbackId.MetalFail, 1, 0, true);
            }

            bool canDamageEntity = (request.Tags & (
                ToolTag.Bomb |
                ToolTag.Pickaxe |
                ToolTag.Shovel |
                ToolTag.Pound |
                ToolTag.LightImpact |
                ToolTag.HeavyImpact |
                ToolTag.Fire |
                ToolTag.Projectile |
                ToolTag.Cut)) != 0;
            bool entityAccepted = canDamageEntity
                && damageReceiver != null
                && damageReceiver.TryReceiveToolDamage(
                new ToolDamageEvent(
                    request.ActionId,
                    1,
                    (request.Tags & ToolTag.HeavyImpact) != 0
                        ? new Vector2(request.Direction.x * 5f, 2f)
                        : new Vector2(request.Direction.x * 2f, 1f),
                    request.Tags,
                    request.Source,
                    request.Instigator));
            bool consume = mapResult.Accepted && mapResult.ConsumeToolResource || entityAccepted;
            FeedbackId feedback = mapResult.Feedback != FeedbackId.None
                ? mapResult.Feedback
                : entityAccepted ? FeedbackId.Hit : FeedbackId.None;
            return new ToolDispatchReport(
                mapResult.Accepted,
                entityAccepted,
                consume,
                feedback,
                mapReceiver != null ? 1 : 0,
                damageReceiver != null ? 1 : 0,
                blocksPropagation);
        }

        public void ConfigureForTests(LayerMask configuredMask, float sizeRatio = 0.82f)
        {
            targetMask = configuredMask;
            querySizeRatio = sizeRatio;
        }

        private static int ToReactionActionId(long actionId)
        {
            int value = unchecked((int)actionId);
            return value == 0 ? 1 : value;
        }

        private static T FindInterfaceInParents<T>(Transform start) where T : class
        {
            for (Transform current = start; current != null; current = current.parent)
            {
                MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
                for (int index = 0; index < behaviours.Length; index++)
                {
                    if (behaviours[index] is T match)
                    {
                        return match;
                    }
                }
            }
            return null;
        }
    }
}

#endif
