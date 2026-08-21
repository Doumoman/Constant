#if LEGACY_DISABLED
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Tools.Core;
using UnityEngine;

namespace StarNight.Tools.HookLauncher
{
    public sealed class HookLauncherRuntime : HandToolRuntime,
        IHookHandSlotAction,
        ICancelableHandSlotAction
    {
        public override bool TryPrimaryUse(
            PlayerHandSlot owner,
            PlayerActionContext context,
            int facingSign,
            LayerMask blockMask)
        {
            HookActionController controller = owner != null
                ? owner.GetComponent<HookActionController>()
                : null;
            return controller != null
                && controller.TryFire(this, owner, context, facingSign, context.LookVertical);
        }

        public bool TryPullHook(PlayerActionContext context)
        {
            return GetComponentInParent<HookActionController>()?.TryPull(context) == true;
        }

        public bool TryCancelHandSlotAction()
        {
            return GetComponentInParent<HookActionController>()?.CancelHook() == true;
        }

        public override bool TryPrepareForDrop(PlayerActionContext context)
        {
            CancelActiveAction();
            return true;
        }

        protected override void CancelActiveAction()
        {
            GetComponentInParent<HookActionController>()?.CancelHook();
            base.CancelActiveAction();
        }
    }
}

#endif
