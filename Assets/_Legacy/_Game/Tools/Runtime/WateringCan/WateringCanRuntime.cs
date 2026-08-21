#if LEGACY_DISABLED
using StarNight.Interaction.Reactions;
using StarNight.Map;
using StarNight.Tools.Core;
using UnityEngine;

namespace StarNight.Tools.Watering
{
    public sealed class WateringCanRuntime : HandToolRuntime
    {
        public const int FacingRangeCells = 3;
        public const int UpRangeCells = 2;

        public int LastSprayedCellCount { get; private set; }

        public override ToolDispatchReport DispatchImpact(
            IToolReactionWorld world,
            ToolDispatchRequest request)
        {
            LastSprayedCellCount = 0;
            if (world == null)
            {
                return ToolDispatchReport.Rejected();
            }

            int range = request.Direction == Vector2Int.up ? UpRangeCells : FacingRangeCells;
            bool mapAccepted = false;
            bool entityAccepted = false;
            bool consume = false;
            bool blocked = false;
            int mapCount = 0;
            int entityCount = 0;
            FeedbackId feedback = FeedbackId.None;
            for (int distance = 1; distance <= range; distance++)
            {
                Vector2Int targetCell = request.OriginCell + request.Direction * distance;
                ToolDispatchReport result = world.Dispatch(new ToolDispatchRequest(
                    request.ActionId,
                    request.Tags,
                    request.OriginCell,
                    targetCell,
                    request.Direction,
                    request.Magnitude,
                    request.Source,
                    request.Instigator,
                    request.GridOrigin,
                    request.CellSize));
                LastSprayedCellCount++;
                mapAccepted |= result.MapAccepted;
                entityAccepted |= result.EntityAccepted;
                consume |= result.ConsumeToolResource;
                blocked |= result.BlocksPropagation;
                mapCount += result.MapReceiverCount;
                entityCount += result.EntityReceiverCount;
                if (feedback == FeedbackId.None && result.Feedback != FeedbackId.None)
                {
                    feedback = result.Feedback;
                }
                if (result.BlocksPropagation)
                {
                    break;
                }
            }

            return new ToolDispatchReport(
                mapAccepted,
                entityAccepted,
                consume,
                feedback,
                mapCount,
                entityCount,
                blocked);
        }
    }
}

#endif
