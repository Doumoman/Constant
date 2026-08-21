#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Map
{
    public sealed class TileMutationService
    {
        public ToolReactionResult TryApply(
            IToolReactionReceiver receiver,
            ToolReactionContext context)
        {
            if (receiver == null)
            {
                return ToolReactionResult.Rejected(FeedbackId.None);
            }

            ToolReactionResult result = receiver.TryReact(context);
            if (result.Accepted && result.ChangedState)
            {
                Physics2D.SyncTransforms();
            }
            return result;
        }
    }
}

#endif
