#if LEGACY_DISABLED
using StarNight.Map;
using UnityEngine;

namespace StarNight.Tools.Rope
{
    [DisallowMultipleComponent]
    public sealed class RopeAnchorRuntime : MonoBehaviour, IToolReactionReceiver
    {
        private RopeInstallationRuntime installation;
        private int lastActionId;

        public RopeAnchorKind Kind { get; private set; }

        public void Configure(RopeInstallationRuntime owner, RopeAnchorKind kind)
        {
            installation = owner;
            Kind = kind;
        }

        public ToolReactionResult TryReact(ToolReactionContext context)
        {
            if (context.ActionId == lastActionId)
            {
                return ToolReactionResult.Rejected(FeedbackId.DuplicateAction);
            }
            lastActionId = context.ActionId;
            if ((context.Tags & (ToolTag.Bomb | ToolTag.Fire | ToolTag.Cut)) == 0
                || installation == null)
            {
                return ToolReactionResult.Rejected(FeedbackId.None);
            }

            bool changed = installation.BreakAt(0);
            return new ToolReactionResult
            {
                Accepted = changed,
                ChangedState = changed,
                ConsumeToolResource = changed,
                Feedback = changed ? FeedbackId.Break : FeedbackId.None,
            };
        }
    }
}

#endif
