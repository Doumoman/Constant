#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using StarNight.Tools;
using UnityEngine;

namespace StarNight.Stages.P5
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HandToolPickup2D))]
    public sealed class P5StoryPestle2D : MonoBehaviour
    {
        [SerializeField] private HandToolPickup2D pickup;
        [SerializeField] private Transform recoveryAnchor;
        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private float minimumWorldY = -8f;
        [SerializeField, Min(1f)] private float maximumRecoveryDistance = 24f;

        private bool subscribed;

        public event Action<P5StoryPestle2D> Discovered;
        public event Action<P5StoryPestle2D> Recovered;
        public event Action<P5StoryPestle2D> Returned;

        public HandToolPickup2D Pickup => pickup;
        public bool WasDiscovered { get; private set; }
        public bool IsReturned { get; private set; }

        public void Configure(
            HandToolPickup2D targetPickup,
            Transform targetRecoveryAnchor,
            GridWorld world,
            float recoveryMinimumY = -8f,
            float recoveryMaximumDistance = 24f)
        {
            Unsubscribe();
            pickup = targetPickup != null
                ? targetPickup
                : GetComponent<HandToolPickup2D>();
            recoveryAnchor = targetRecoveryAnchor;
            gridWorld = world;
            minimumWorldY = recoveryMinimumY;
            maximumRecoveryDistance =
                Mathf.Max(1f, recoveryMaximumDistance);
            WasDiscovered = pickup != null && pickup.IsHeld;
            IsReturned = false;
            Subscribe();
        }

        private void Awake()
        {
            if (pickup == null)
            {
                pickup = GetComponent<HandToolPickup2D>();
            }

            if (gridWorld == null)
            {
                gridWorld = FindFirstObjectByType<GridWorld>();
            }

            if (recoveryAnchor == null)
            {
                recoveryAnchor = transform;
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (IsReturned || pickup == null || pickup.IsHeld)
            {
                return;
            }

            if (ShouldRecover())
            {
                RecoverNow();
            }
        }

        public bool IsHeldBy(PlayerToolInventory2D inventory)
        {
            return inventory != null
                && inventory.HeldTool == pickup
                && pickup != null
                && pickup.IsHeld;
        }

        public bool TryReturn(
            PlayerToolInventory2D inventory,
            Transform returnedDisplayAnchor)
        {
            if (IsReturned
                || returnedDisplayAnchor == null
                || !IsHeldBy(inventory)
                || !inventory.TryPlaceHeldTool(
                    pickup,
                    returnedDisplayAnchor,
                    false))
            {
                return false;
            }

            IsReturned = true;
            Returned?.Invoke(this);
            return true;
        }

        public bool RecoverNow()
        {
            if (IsReturned
                || pickup == null
                || pickup.IsHeld
                || recoveryAnchor == null
                || !pickup.RecoverTo(recoveryAnchor.position))
            {
                return false;
            }

            Recovered?.Invoke(this);
            return true;
        }

        private bool ShouldRecover()
        {
            Vector2 position = pickup.transform.position;
            if (position.y < minimumWorldY)
            {
                return true;
            }

            if (recoveryAnchor != null
                && (position - (Vector2)recoveryAnchor.position).sqrMagnitude
                    > maximumRecoveryDistance * maximumRecoveryDistance)
            {
                return true;
            }

            if (gridWorld == null)
            {
                return false;
            }

            GridPos cell = gridWorld.WorldToCell(position);
            return !gridWorld.IsWithinBounds(cell)
                || gridWorld.IsHazard(cell)
                || gridWorld.IsSolid(cell);
        }

        private void HandlePickedUp(HandToolPickup2D pickedUp)
        {
            if (pickedUp != pickup || WasDiscovered)
            {
                return;
            }

            WasDiscovered = true;
            Discovered?.Invoke(this);
        }

        private void Subscribe()
        {
            if (subscribed || pickup == null)
            {
                return;
            }

            pickup.PickedUp += HandlePickedUp;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || pickup == null)
            {
                return;
            }

            pickup.PickedUp -= HandlePickedUp;
            subscribed = false;
        }
    }
}

#endif
