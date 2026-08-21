#if LEGACY_DISABLED
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Tools.Core;
using UnityEngine;

namespace StarNight.Tools.Umbrella
{
    public sealed class WindUmbrellaRuntime : HandToolRuntime,
        IUmbrellaHandSlotAction,
        ITraversalAwareHandSlotAction
    {
        public override bool TryPrimaryUse(
            PlayerHandSlot owner,
            PlayerActionContext context,
            int facingSign,
            LayerMask blockMask)
        {
            UmbrellaActionController controller = owner != null
                ? owner.GetComponent<UmbrellaActionController>()
                : null;
            return controller != null && controller.TryToggle(this, owner, context);
        }

        public bool TryCloseUmbrella(PlayerActionContext context)
        {
            return GetComponentInParent<UmbrellaActionController>()?.TryClose(context) == true;
        }

        public void PrepareForTraversal()
        {
            GetComponentInParent<UmbrellaActionController>()?.ForceClose();
        }

        public override bool TryPrepareForDrop(PlayerActionContext context)
        {
            CancelActiveAction();
            return true;
        }

        protected override void CancelActiveAction()
        {
            GetComponentInParent<UmbrellaActionController>()?.ForceClose();
            base.CancelActiveAction();
        }
    }
}

#endif
