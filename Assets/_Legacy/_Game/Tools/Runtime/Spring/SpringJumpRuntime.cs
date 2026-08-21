#if LEGACY_DISABLED
using StarNight.Core.Tools;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Tools.Core;
using UnityEngine;

namespace StarNight.Tools.Spring
{
    public sealed class SpringJumpRuntime : HandToolRuntime, ISelectedEquipmentJumpModifier
    {
        public const float SpringJumpVelocity = SpringEquipmentContract.JumpVelocity;
        public const float RequiredHeadClearance = SpringEquipmentContract.RequiredHeadClearanceCells;

        public override bool TryPrimaryUse(
            PlayerHandSlot owner,
            PlayerActionContext context,
            int facingSign,
            LayerMask blockMask)
        {
            _ = context;
            _ = facingSign;
            _ = blockMask;
            _ = owner;
            return false;
        }

        public bool TryExecuteSelectedJump(PlayerHandSlot owner)
        {
            if (owner == null
                || owner.CurrentItem != this
                || Definition == null
                || Definition.UseCategory != StarNight.Tools.Items.ItemUseCategory.JumpModifier
                || !ResourceState.HasUsableResource)
            {
                return false;
            }

            MonoBehaviour[] behaviours = owner.GetComponents<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IPlayerSpecialJumpExecutor jumpExecutor
                    && jumpExecutor.TryLaunchSpecialJump(SpringJumpVelocity, RequiredHeadClearance))
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
