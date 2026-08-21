#if LEGACY_DISABLED
using StarNight.Interaction.Input;
using StarNight.Player.Motor;
using UnityEngine;

namespace StarNight.Stage.Exit
{
    [DisallowMultipleComponent]
    public sealed class StagePlayerActionExecutor : MonoBehaviour, IPlayerActionExecutor
    {
        private PlayerMotor2D player;
        private StageExitDoor exitDoor;

        public bool HasHandSlotItem => false;

        public void Configure(PlayerMotor2D playerMotor, StageExitDoor door)
        {
            player = playerMotor;
            exitDoor = door;
        }

        public bool TryDropHandSlot(PlayerActionContext context) => false;
        public bool TryContextAction(PlayerActionContext context) => false;
        public bool TryHandSlotPrimaryUse(PlayerActionContext context) => false;
        public bool TryPlaceBomb(PlayerActionContext context) => false;
        public bool TryPlaceRope(PlayerActionContext context) => false;

        public bool TryWorldInteraction(PlayerActionContext context)
        {
            if (player == null || exitDoor == null)
            {
                return false;
            }

            return exitDoor.TryBeginHold(context.ActionId);
        }
    }
}

#endif
