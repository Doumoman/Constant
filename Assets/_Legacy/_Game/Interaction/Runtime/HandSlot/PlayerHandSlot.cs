#if LEGACY_DISABLED
using System;
using StarNight.Interaction.Carry;
using UnityEngine;

namespace StarNight.Interaction.HandSlot
{
    [DisallowMultipleComponent]
    public sealed class PlayerHandSlot : MonoBehaviour, StarNight.Interaction.Input.IPlayerMovementSpeedModifier
    {
        public const float PickupSeconds = 0.10f;
        public const float MaxPickupDistance = 0.80f;

        [SerializeField] private HandSlotPresenter presenter;
        [SerializeField] private HandSlotItemRuntime currentItem;

        private HandSlotItemRuntime pendingPickup;
        private float pendingPickupCompletesAt;

        public event Action<HandSlotItemRuntime, HandSlotItemRuntime> ItemChanged;

        public HandSlotItemRuntime CurrentItem => currentItem;
        public CarryableObject HeldCarryable => currentItem as CarryableObject;
        public bool IsEmpty => currentItem == null;
        public bool HasPendingPickup => pendingPickup != null;
        public float MovementMultiplier => HeldCarryable != null ? HeldCarryable.Definition.MovementMultiplier : 1f;
        public float MovementSpeedMultiplier => MovementMultiplier;
        public float JumpHeightMultiplier => HeldCarryable != null ? HeldCarryable.Definition.JumpHeightMultiplier : 1f;
        public bool CanClimbRope => HeldCarryable == null || HeldCarryable.Definition.CanClimbRope;

        private void Awake()
        {
            if (presenter == null)
            {
                presenter = GetComponent<HandSlotPresenter>();
            }
        }

        public bool TryBeginPickup(HandSlotItemRuntime candidate, float now)
        {
            if (!IsEmpty || pendingPickup != null || candidate == null || !candidate.CanWorldPickup(transform.position))
            {
                return false;
            }

            pendingPickup = candidate;
            pendingPickupCompletesAt = now + PickupSeconds;
            return true;
        }

        public bool TickPickup(float now)
        {
            if (pendingPickup == null)
            {
                return false;
            }

            if (!pendingPickup.CanWorldPickup(transform.position))
            {
                CancelPendingPickup();
                return false;
            }

            if (now < pendingPickupCompletesAt)
            {
                return false;
            }

            HandSlotItemRuntime candidate = pendingPickup;
            pendingPickup = null;
            pendingPickupCompletesAt = 0f;
            return TryAttach(candidate);
        }

        public void CancelPendingPickup()
        {
            pendingPickup = null;
            pendingPickupCompletesAt = 0f;
        }

        public bool TryAttach(HandSlotItemRuntime item)
        {
            if (!IsEmpty || item == null || !item.CanEnterHandSlot || presenter == null || !presenter.Attach(item))
            {
                return false;
            }

            HandSlotItemRuntime previous = currentItem;
            currentItem = item;
            ItemChanged?.Invoke(previous, currentItem);
            return true;
        }

        public bool TryExchangeCurrent(HandSlotItemRuntime replacement, Vector2 releasedWorldPosition)
        {
            if (currentItem == null)
            {
                return TryAttach(replacement);
            }

            if (replacement == null
                || replacement == currentItem
                || !replacement.CanEnterHandSlot
                || presenter == null
                || !presenter.Attach(replacement))
            {
                return false;
            }

            HandSlotItemRuntime released = currentItem;
            currentItem = replacement;
            released.ExitHandSlot(releasedWorldPosition, true);
            ItemChanged?.Invoke(released, replacement);
            return true;
        }

        public bool TryDropCurrent(Vector2 worldPosition)
        {
            if (currentItem == null)
            {
                return false;
            }

            HandSlotItemRuntime released = currentItem;
            currentItem = null;
            released.ExitHandSlot(worldPosition, true);
            ItemChanged?.Invoke(released, null);
            return true;
        }

        public bool TryThrowCurrent(Vector2 worldPosition, Vector2 velocity, long actionId)
        {
            CarryableObject carryable = HeldCarryable;
            if (carryable == null)
            {
                return false;
            }

            currentItem = null;
            carryable.Throw(worldPosition, velocity, actionId);
            ItemChanged?.Invoke(carryable, null);
            return true;
        }

        public bool TryReleaseCurrent(HandSlotItemRuntime expectedItem)
        {
            if (currentItem == null || currentItem != expectedItem)
            {
                return false;
            }

            HandSlotItemRuntime released = currentItem;
            currentItem = null;
            ItemChanged?.Invoke(released, null);
            return true;
        }

        public bool TryConsumeCurrent()
        {
            if (currentItem == null)
            {
                return false;
            }

            HandSlotItemRuntime consumed = currentItem;
            currentItem = null;
            consumed.transform.SetParent(null, true);
            ItemChanged?.Invoke(consumed, null);
            Destroy(consumed.gameObject);
            return true;
        }

        public bool TrySuspendForPortal(
            ICarryPortalClearance clearance,
            out CarryObjectSnapshot snapshot)
        {
            snapshot = default;
            HandSlotItemRuntime item = currentItem;
            if (item == null)
            {
                return true;
            }

            if (!item.CanPassPortal(clearance))
            {
                return false;
            }

            item.SuspendForPortal(presenter != null ? presenter.CarrySocket : transform);
            if (item is CarryableObject carryable)
            {
                snapshot = CarryObjectSnapshot.Capture(carryable, true);
            }
            return true;
        }

        public bool RestoreAfterPortal()
        {
            HandSlotItemRuntime item = currentItem;
            if (item == null)
            {
                return true;
            }

            return presenter != null && item.RestoreAfterPortal(presenter);
        }

        public void ConfigureForTests(HandSlotPresenter configuredPresenter)
        {
            presenter = configuredPresenter;
        }
    }
}

#endif
