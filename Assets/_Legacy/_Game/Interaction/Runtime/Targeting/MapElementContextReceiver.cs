#if LEGACY_DISABLED
using StarNight.Map;
using UnityEngine;

namespace StarNight.Interaction.Targeting
{
    [DisallowMultipleComponent]
    public sealed class MapElementContextReceiver : MonoBehaviour, IContextReceiver
    {
        [SerializeField] private bool dispatchReactionWhenInteractionRejects;

        public int ContextPriority => 150;

        public bool CanReceive(ContextReceiverQuery query)
        {
            return query.HasHandSlotItem &&
                   (FindInteractionReceiver() != null ||
                    dispatchReactionWhenInteractionRejects && FindToolReactionReceiver() != null);
        }

        public ContextReceiverResult TryReceive(ContextReceiverRequest request)
        {
            var source = ResolveGameObject(request.HandSlotItem);
            var interactionReceiver = FindInteractionReceiver();
            if (interactionReceiver != null && interactionReceiver.TryInteract(source))
            {
                return new ContextReceiverResult(true, false, "MapElementContextAccepted");
            }

            if (!dispatchReactionWhenInteractionRejects)
            {
                return ContextReceiverResult.Rejected("MapElementContextRejected");
            }

            var reactionReceiver = FindToolReactionReceiver();
            if (reactionReceiver == null)
            {
                return ContextReceiverResult.Rejected("MapElementContextRejected");
            }

            var result = reactionReceiver.TryReact(new ToolReactionContext
            {
                ActionId = unchecked((int)request.Action.ActionId),
                Tags = ToolTag.Context,
                Direction = Vector2Int.zero,
                Magnitude = 1f,
                Source = source,
                Instigator = request.Actor,
            });
            return result.Accepted
                ? new ContextReceiverResult(true, false, result.Feedback.ToString())
                : ContextReceiverResult.Rejected(result.Feedback.ToString());
        }

        public void ConfigureForTests(bool allowReactionFallback)
        {
            dispatchReactionWhenInteractionRejects = allowReactionFallback;
        }

        private IMapElementInteractionReceiver FindInteractionReceiver()
        {
            var behaviours = GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IMapElementInteractionReceiver receiver &&
                    !string.IsNullOrWhiteSpace(receiver.InteractionPrompt))
                {
                    return receiver;
                }
            }

            return null;
        }

        private IToolReactionReceiver FindToolReactionReceiver()
        {
            var behaviours = GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IToolReactionReceiver receiver)
                {
                    return receiver;
                }
            }

            return null;
        }

        private static GameObject ResolveGameObject(Object value)
        {
            if (value is GameObject gameObject)
            {
                return gameObject;
            }

            return value is Component component ? component.gameObject : null;
        }
    }
}

#endif
