#if LEGACY_DISABLED
using StarNight.Interaction.Input;
using UnityEngine;

namespace StarNight.Interaction.Targeting
{
    public readonly struct ContextReceiverQuery
    {
        public ContextReceiverQuery(GameObject actor, Object handSlotItem)
        {
            Actor = actor;
            HandSlotItem = handSlotItem;
        }

        public GameObject Actor { get; }
        public Object HandSlotItem { get; }
        public bool HasHandSlotItem => HandSlotItem != null;
    }

    public readonly struct ContextReceiverRequest
    {
        public ContextReceiverRequest(
            PlayerActionContext action,
            GameObject actor,
            Object handSlotItem)
        {
            Action = action;
            Actor = actor;
            HandSlotItem = handSlotItem;
        }

        public PlayerActionContext Action { get; }
        public GameObject Actor { get; }
        public Object HandSlotItem { get; }
    }

    public readonly struct ContextReceiverResult
    {
        public ContextReceiverResult(bool accepted, bool consumeHandSlotItem, string feedbackId = "")
        {
            Accepted = accepted;
            ConsumeHandSlotItem = consumeHandSlotItem;
            FeedbackId = feedbackId ?? string.Empty;
        }

        public bool Accepted { get; }
        public bool ConsumeHandSlotItem { get; }
        public string FeedbackId { get; }

        public static ContextReceiverResult Rejected(string feedbackId = "")
        {
            return new ContextReceiverResult(false, false, feedbackId);
        }
    }

    public interface IContextReceiver
    {
        int ContextPriority { get; }
        bool CanReceive(ContextReceiverQuery query);
        ContextReceiverResult TryReceive(ContextReceiverRequest request);
    }

    public interface IWorldInteractionReceiver
    {
        bool CanInteract(GameObject actor);
        bool TryInteract(PlayerActionContext action, GameObject actor);
    }

    public interface IInteractionPromptSource
    {
        string PromptLabel { get; }
    }

    public static class ContextReceiverResolver
    {
        public static IContextReceiver Resolve(GameObject target, ContextReceiverQuery query)
        {
            if (target == null)
            {
                return null;
            }

            MonoBehaviour[] behaviours = target.GetComponentsInParent<MonoBehaviour>(true);
            IContextReceiver best = null;
            int bestPriority = int.MinValue;
            int bestInstanceId = int.MaxValue;
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (!(behaviours[index] is IContextReceiver receiver)
                    || !receiver.CanReceive(query))
                {
                    continue;
                }

                int priority = receiver.ContextPriority;
                int instanceId = behaviours[index].GetInstanceID();
                if (priority > bestPriority
                    || (priority == bestPriority && instanceId < bestInstanceId))
                {
                    best = receiver;
                    bestPriority = priority;
                    bestInstanceId = instanceId;
                }
            }

            return best;
        }

        public static IWorldInteractionReceiver ResolveWorldInteraction(GameObject target, GameObject actor)
        {
            if (target == null)
            {
                return null;
            }

            MonoBehaviour[] behaviours = target.GetComponentsInParent<MonoBehaviour>(true);
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IWorldInteractionReceiver receiver
                    && receiver.CanInteract(actor))
                {
                    return receiver;
                }
            }

            return null;
        }
    }
}

#endif
