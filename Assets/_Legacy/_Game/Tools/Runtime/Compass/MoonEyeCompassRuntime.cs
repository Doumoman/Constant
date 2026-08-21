#if LEGACY_DISABLED
using StarNight.Core.Tools;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Tools.Core;
using UnityEngine;

namespace StarNight.Tools.Compass
{
    public sealed class MoonEyeCompassRuntime : HandToolRuntime
    {
        public const float FocusRangeCells = CompassEquipmentContract.FocusDetectionRangeCells;
        public const float FocusDurationSeconds = CompassEquipmentContract.FocusDurationSeconds;

        public override bool TryPrimaryUse(
            PlayerHandSlot owner,
            PlayerActionContext context,
            int facingSign,
            LayerMask blockMask)
        {
            _ = context;
            _ = facingSign;
            _ = blockMask;
            if (owner == null
                || owner.CurrentItem != this
                || Definition == null
                || !ResourceState.HasUsableResource)
            {
                return false;
            }

            MonoBehaviour[] behaviours = owner.GetComponents<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is ICompassFocusDetector detector
                    && detector.TryFocusNearestSecret(FocusRangeCells, FocusDurationSeconds))
                {
                    ResourceState.TryConsumeForSuccessfulReaction(true);
                    return true;
                }
            }
            return false;
        }
    }
}

#endif
