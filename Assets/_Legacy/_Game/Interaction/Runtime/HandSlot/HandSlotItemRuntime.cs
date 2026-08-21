#if LEGACY_DISABLED
using StarNight.Interaction.Carry;
using StarNight.Interaction.Input;
using UnityEngine;

namespace StarNight.Interaction.HandSlot
{
    public interface IHookHandSlotAction
    {
        bool TryPullHook(PlayerActionContext context);
    }

    public interface IUmbrellaHandSlotAction
    {
        bool TryCloseUmbrella(PlayerActionContext context);
    }

    public interface ICancelableHandSlotAction
    {
        bool TryCancelHandSlotAction();
    }

    public interface ITraversalAwareHandSlotAction
    {
        void PrepareForTraversal();
    }

    public interface IHandSlotDropPreparation
    {
        bool TryPrepareForDrop(PlayerActionContext context);
    }

    public enum HandSlotItemKind
    {
        HandTool,
        CarryObject,
        ArmedBombCarry,
    }

    public abstract class HandSlotItemRuntime : MonoBehaviour
    {
        public abstract string RuntimeItemId { get; }
        public abstract HandSlotItemKind ItemKind { get; }
        public virtual bool CanEnterHandSlot => true;
        public virtual Vector2Int PlacementFootprint => Vector2Int.one;

        public abstract bool TryEnterHandSlot(HandSlotPresenter presenter);
        public abstract void ExitHandSlot(Vector2 worldPosition, bool restorePlayerCollision);

        public virtual bool CanWorldPickup(Vector2 actorPosition) => false;

        public virtual bool TryPrimaryUse(
            PlayerHandSlot owner,
            PlayerActionContext context,
            int facingSign,
            LayerMask blockMask) => false;

        public virtual bool CanPassPortal(ICarryPortalClearance clearance) => false;
        public virtual void SuspendForPortal(Transform carrySocket) { }
        public virtual bool RestoreAfterPortal(HandSlotPresenter presenter) => false;
    }
}

#endif
