#if LEGACY_DISABLED
using StarNight.Interaction.Carry;
using StarNight.Interaction.Input;
using StarNight.Interaction.State;
using StarNight.Interaction.Targeting;
using UnityEngine;

namespace StarNight.Interaction.HandSlot
{
    public interface ICarryPrimaryUse
    {
        bool TryUse(PlayerActionContext context);
    }

    [DisallowMultipleComponent]
    public sealed class HandSlotTransferService : MonoBehaviour,
        IPlayerActionExecutor,
        IPlayerInventoryActionExecutor,
        IPlayerSpecialActionExecutor,
        IPlayerSpecialActionCancelHandler,
        IPlayerTraversalStateHandler
    {
        [SerializeField] private PlayerHandSlot handSlot;
        [SerializeField] private InteractionProbe interactionProbe;
        [SerializeField] private PlayerActionLock actionLock;
        [SerializeField] private ProjectPhysicsProfile physicsProfile;
        [SerializeField] private RectInt roomBounds;
        [SerializeField] private Vector2 gridOrigin;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private int facingSign = 1;
        [SerializeField] private MonoBehaviour equipmentInventoryComponent;

        private readonly CarryPlacementResolver placementResolver = new CarryPlacementResolver();
        private readonly ThrowResolver throwResolver = new ThrowResolver();
        private ICarryPlacementWorld placementWorldOverride;
        private IEquipmentInventoryBridge equipmentInventory;
        private long pendingPickupActionId;

        public bool HasHandSlotItem => handSlot != null && !handSlot.IsEmpty;
        public bool HasPhysicalCarryItem => handSlot != null
            && handSlot.CurrentItem != null
            && (equipmentInventory == null || !equipmentInventory.IsInventoryItem(handSlot.CurrentItem));
        public bool HasSelectedEquipment => equipmentInventory?.SelectedRuntime != null;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            if (handSlot != null)
            {
                handSlot.ItemChanged += HandleItemChanged;
                HandleItemChanged(null, handSlot.CurrentItem);
            }
        }

        private void OnDisable()
        {
            if (handSlot != null)
            {
                handSlot.ItemChanged -= HandleItemChanged;
            }
        }

        private void Update()
        {
            if (interactionProbe != null)
            {
                facingSign = interactionProbe.FacingSign;
            }

            if (handSlot == null || !handSlot.HasPendingPickup)
            {
                return;
            }

            bool completed = handSlot.TickPickup(Time.unscaledTime);
            if (completed)
            {
                actionLock?.TryRelease(pendingPickupActionId, PlayerActionState.Carrying);
                pendingPickupActionId = 0;
            }
            else if (!handSlot.HasPendingPickup && pendingPickupActionId > 0)
            {
                actionLock?.TryRelease(pendingPickupActionId, PlayerActionState.Free);
                pendingPickupActionId = 0;
                equipmentInventory?.TryRestoreSelected();
            }
        }

        public bool TryDropHandSlot(PlayerActionContext context)
        {
            HandSlotItemRuntime item = handSlot != null ? handSlot.CurrentItem : null;
            if (item == null)
            {
                return false;
            }

            if (actionLock != null && !actionLock.TryAcquire(context.ActionId, PlayerActionState.Placing))
            {
                return false;
            }

            if (!TryResolvePlacement(item, out CarryPlacementResult result))
            {
                actionLock?.TryRelease(context.ActionId, PlayerActionState.Carrying);
                return false;
            }

            bool dropped = equipmentInventory != null && equipmentInventory.IsInventoryItem(item)
                ? equipmentInventory.TryDropSelected(result.WorldPosition)
                : handSlot.TryDropCurrent(result.WorldPosition);
            actionLock?.TryRelease(
                context.ActionId,
                dropped ? PlayerActionState.Free : PlayerActionState.Carrying);
            return dropped;
        }

        public bool TryExchangeCurrent(HandSlotItemRuntime replacement)
        {
            if (handSlot == null || replacement == null)
            {
                return false;
            }

            if (handSlot.IsEmpty)
            {
                return handSlot.TryAttach(replacement);
            }

            HandSlotItemRuntime current = handSlot.CurrentItem;
            return TryResolvePlacement(current, out CarryPlacementResult result)
                && handSlot.TryExchangeCurrent(replacement, result.WorldPosition);
        }

        public bool TryContextAction(PlayerActionContext context)
        {
            if (handSlot == null || handSlot.CurrentItem == null || interactionProbe == null)
            {
                return false;
            }

            InteractionCandidate candidate = interactionProbe.SelectedCandidate;
            var query = new ContextReceiverQuery(gameObject, handSlot.CurrentItem);
            IContextReceiver receiver = candidate != null
                ? ContextReceiverResolver.Resolve(candidate.gameObject, query)
                : null;
            if (receiver == null)
            {
                return false;
            }

            ContextReceiverResult result = receiver.TryReceive(
                new ContextReceiverRequest(context, gameObject, handSlot.CurrentItem));
            if (!result.Accepted)
            {
                return false;
            }

            if (result.ConsumeHandSlotItem)
            {
                handSlot.TryConsumeCurrent();
            }
            return true;
        }

        public bool TryHandSlotPrimaryUse(PlayerActionContext context)
        {
            HandSlotItemRuntime item = handSlot != null ? handSlot.CurrentItem : null;
            if (item == null)
            {
                return false;
            }

            if (!(item is CarryableObject))
            {
                if (item.ItemKind == HandSlotItemKind.HandTool)
                {
                    return item.TryPrimaryUse(
                        handSlot,
                        context,
                        facingSign,
                        physicsProfile != null ? physicsProfile.ToolTargetMask : 0);
                }

                if (actionLock != null && !actionLock.TryAcquire(context.ActionId, PlayerActionState.Throwing))
                {
                    return false;
                }

                LayerMask itemBlockMask = physicsProfile != null ? physicsProfile.DropBlockMask : 0;
                bool used = item.TryPrimaryUse(handSlot, context, facingSign, itemBlockMask);
                actionLock?.TryRelease(
                    context.ActionId,
                    used ? PlayerActionState.Free : PlayerActionState.Carrying);
                return used;
            }

            CarryableObject carryable = handSlot != null ? handSlot.HeldCarryable : null;
            CarryObjectDefinition definition = carryable != null ? carryable.Definition : null;
            if (definition == null)
            {
                return false;
            }

            if (definition.PrimaryUseMode == PrimaryUseMode.ContextualOnly)
            {
                return false;
            }

            if (definition.PrimaryUseMode == PrimaryUseMode.Throw)
            {
                ThrowResolution resolution = throwResolver.Resolve(
                    definition.WeightClass,
                    facingSign,
                    context.LookVertical > 0.5f);
                if (resolution.ShouldDropInstead)
                {
                    return TryDropHandSlot(context);
                }

                if (actionLock != null && !actionLock.TryAcquire(context.ActionId, PlayerActionState.Throwing))
                {
                    return false;
                }

                LayerMask blockMask = physicsProfile != null ? physicsProfile.DropBlockMask : 0;
                bool thrown = throwResolver.TryThrow(handSlot, context, facingSign, blockMask);
                actionLock?.TryRelease(
                    context.ActionId,
                    thrown ? PlayerActionState.Free : PlayerActionState.Carrying);
                return thrown;
            }

            MonoBehaviour[] behaviours = carryable.GetComponents<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is ICarryPrimaryUse primaryUse && primaryUse.TryUse(context))
                {
                    if (definition.PrimaryUseMode == PrimaryUseMode.Consume)
                    {
                        handSlot.TryConsumeCurrent();
                    }
                    return true;
                }
            }
            return false;
        }

        public bool TryWorldInteraction(PlayerActionContext context)
        {
            if (handSlot == null || interactionProbe == null)
            {
                return false;
            }

            InteractionCandidate candidate = interactionProbe.SelectedCandidate;
            if (candidate == null)
            {
                return false;
            }

            HandSlotItemRuntime pickupItem = candidate.GetComponentInParent<HandSlotItemRuntime>();
            if (pickupItem != null && pickupItem.CanWorldPickup(transform.position))
            {
                if (pickupItem.ItemKind == HandSlotItemKind.HandTool && equipmentInventory != null)
                {
                    return equipmentInventory.TryPickupEquipment(pickupItem);
                }

                if (!handSlot.IsEmpty)
                {
                    if (equipmentInventory == null
                        || !equipmentInventory.IsInventoryItem(handSlot.CurrentItem)
                        || !equipmentInventory.TryStowSelected())
                    {
                        return false;
                    }
                }

                if (!handSlot.TryBeginPickup(pickupItem, Time.unscaledTime))
                {
                    equipmentInventory?.TryRestoreSelected();
                    return false;
                }

                pendingPickupActionId = context.ActionId;
                actionLock?.TryAcquire(context.ActionId, PlayerActionState.PickingUp);
                return true;
            }

            IWorldInteractionReceiver receiver =
                ContextReceiverResolver.ResolveWorldInteraction(candidate.gameObject, gameObject);
            return receiver != null && receiver.TryInteract(context, gameObject);
        }

        public bool TryPlaceBomb(PlayerActionContext context) => false;
        public bool TryPlaceRope(PlayerActionContext context) => false;
        public bool TryPrepareHandSlotDrop(PlayerActionContext context)
        {
            HandSlotItemRuntime item = handSlot != null ? handSlot.CurrentItem : null;
            return item is not IHandSlotDropPreparation preparation
                || preparation.TryPrepareForDrop(context);
        }
        public bool TryPullHook(PlayerActionContext context)
        {
            return handSlot?.CurrentItem is IHookHandSlotAction hook && hook.TryPullHook(context);
        }

        public bool TryCloseUmbrella(PlayerActionContext context)
        {
            return handSlot?.CurrentItem is IUmbrellaHandSlotAction umbrella
                && umbrella.TryCloseUmbrella(context);
        }

        public bool TryCancelSpecialAction()
        {
            return handSlot?.CurrentItem is ICancelableHandSlotAction cancelable
                && cancelable.TryCancelHandSlotAction();
        }

        public void PrepareForTraversal()
        {
            if (handSlot?.CurrentItem is ITraversalAwareHandSlotAction traversalAware)
            {
                traversalAware.PrepareForTraversal();
            }
        }

        public void SetFacing(int sign)
        {
            facingSign = sign < 0 ? -1 : 1;
            interactionProbe?.SetFacing(facingSign);
        }

        public void ConfigureForTests(
            PlayerHandSlot slot,
            InteractionProbe probe,
            PlayerActionLock configuredLock,
            ICarryPlacementWorld placementWorld = null)
        {
            handSlot = slot;
            interactionProbe = probe;
            actionLock = configuredLock;
            placementWorldOverride = placementWorld;
            ResolveEquipmentInventory();
        }

        private void ResolveDependencies()
        {
            if (handSlot == null)
            {
                handSlot = GetComponent<PlayerHandSlot>();
            }
            if (interactionProbe == null)
            {
                interactionProbe = GetComponent<InteractionProbe>();
            }
            if (actionLock == null)
            {
                actionLock = GetComponent<PlayerActionLock>();
            }
            ResolveEquipmentInventory();
        }

        private void HandleItemChanged(HandSlotItemRuntime previous, HandSlotItemRuntime current)
        {
            interactionProbe?.SetHandSlotItem(current);
        }

        private void ResolveEquipmentInventory()
        {
            equipmentInventory = equipmentInventoryComponent as IEquipmentInventoryBridge;
            if (equipmentInventory != null)
            {
                return;
            }

            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IEquipmentInventoryBridge inventory)
                {
                    equipmentInventory = inventory;
                    equipmentInventoryComponent = behaviours[index];
                    return;
                }
            }
        }

        private Vector2Int WorldToCell(Vector2 worldPosition)
        {
            Vector2 local = (worldPosition - gridOrigin) / Mathf.Max(0.01f, cellSize);
            return new Vector2Int(Mathf.RoundToInt(local.x), Mathf.RoundToInt(local.y));
        }

        private bool TryResolvePlacement(
            HandSlotItemRuntime item,
            out CarryPlacementResult result)
        {
            if (item == null)
            {
                result = default;
                return false;
            }

            var request = new CarryPlacementRequest(
                WorldToCell(transform.position),
                facingSign,
                item.PlacementFootprint,
                gridOrigin,
                cellSize);
            ICarryPlacementWorld world = placementWorldOverride
                ?? new PhysicsCarryPlacementWorld(roomBounds, gridOrigin, cellSize, physicsProfile);
            return placementResolver.TryResolve(request, world, out result);
        }
    }
}

#endif
