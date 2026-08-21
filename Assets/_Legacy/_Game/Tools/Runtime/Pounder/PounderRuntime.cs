#if LEGACY_DISABLED
using StarNight.Interaction.Reactions;
using StarNight.Map;
using StarNight.Tools.Core;
using UnityEngine;

namespace StarNight.Tools.Pounder
{
    public sealed class PounderRuntime : HandToolRuntime
    {
        public override bool SupportsAirPound => true;

        public override ToolDispatchReport DispatchImpact(
            IToolReactionWorld world,
            ToolDispatchRequest request)
        {
            if (world == null)
            {
                return ToolDispatchReport.Rejected();
            }
            if (request.Direction != Vector2Int.down)
            {
                return base.DispatchImpact(world, request);
            }

            ToolDispatchReport center = world.Dispatch(request);
            ToolDispatchReport left = DispatchSide(world, request, Vector2Int.left);
            ToolDispatchReport right = DispatchSide(world, request, Vector2Int.right);
            FeedbackId feedback = center.Feedback != FeedbackId.None
                ? center.Feedback
                : left.Feedback != FeedbackId.None ? left.Feedback : right.Feedback;
            return new ToolDispatchReport(
                center.MapAccepted || left.MapAccepted || right.MapAccepted,
                center.EntityAccepted || left.EntityAccepted || right.EntityAccepted,
                center.ConsumeToolResource,
                feedback,
                center.MapReceiverCount + left.MapReceiverCount + right.MapReceiverCount,
                center.EntityReceiverCount + left.EntityReceiverCount + right.EntityReceiverCount,
                center.BlocksPropagation);
        }

        private static ToolDispatchReport DispatchSide(
            IToolReactionWorld world,
            ToolDispatchRequest center,
            Vector2Int side)
        {
            return world.Dispatch(new ToolDispatchRequest(
                center.ActionId,
                ToolTag.LightImpact,
                center.OriginCell,
                center.TargetCell + side,
                Vector2Int.down,
                0.5f,
                center.Source,
                center.Instigator,
                center.GridOrigin,
                center.CellSize));
        }
    }
}

#endif
