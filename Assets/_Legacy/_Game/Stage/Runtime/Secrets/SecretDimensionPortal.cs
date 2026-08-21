#if LEGACY_DISABLED
using StarNight.Interaction.Input;
using StarNight.Interaction.Targeting;
using StarNight.Stage.Transitions;
using UnityEngine;

namespace StarNight.Stage.Secrets
{
    [DisallowMultipleComponent]
    public sealed class SecretDimensionPortal : MonoBehaviour,
        IContextReceiver,
        IWorldInteractionReceiver,
        IInteractionPromptSource
    {
        [SerializeField] private RoomPortal2D roomPortal;
        [SerializeField] private SecretDimensionController controller;
        [SerializeField] private string promptLabel = "별문 들어가기";

        public string PromptLabel => promptLabel;
        public int ContextPriority => 1000;

        public void Configure(
            RoomPortal2D portal,
            SecretDimensionController dimensionController,
            string prompt)
        {
            roomPortal = portal;
            controller = dimensionController;
            promptLabel = prompt ?? string.Empty;
        }

        public bool CanInteract(GameObject actor)
        {
            return actor != null && roomPortal != null && roomPortal.IsReady && controller != null && !controller.IsTransitioning;
        }

        public bool CanReceive(ContextReceiverQuery query)
        {
            return CanInteract(query.Actor);
        }

        public ContextReceiverResult TryReceive(ContextReceiverRequest request)
        {
            bool accepted = CanReceive(new ContextReceiverQuery(request.Actor, request.HandSlotItem))
                && controller.TryUsePortal(roomPortal);
            return accepted
                ? new ContextReceiverResult(true, false, "secret_portal")
                : ContextReceiverResult.Rejected("secret_portal_unavailable");
        }

        public bool TryInteract(PlayerActionContext action, GameObject actor)
        {
            return CanInteract(actor) && controller.TryUsePortal(roomPortal);
        }
    }
}

#endif
