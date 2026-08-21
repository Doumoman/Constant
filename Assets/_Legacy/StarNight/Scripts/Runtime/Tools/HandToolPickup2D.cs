#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Tools
{
    [DisallowMultipleComponent]
    public sealed class HandToolPickup2D : MonoBehaviour
    {
        private static readonly HashSet<HandToolPickup2D> ActiveInternal =
            new HashSet<HandToolPickup2D>();

        [SerializeField] private HandToolKind kind;
        [SerializeField, Min(0)] private int maximumUses;
        [SerializeField, Min(0)] private int remainingUses;
        [SerializeField] private Collider2D pickupCollider;
        [SerializeField] private SpriteRenderer toolRenderer;

        private Transform originalParent;
        private bool isPickupAvailable = true;

        public static IReadOnlyCollection<HandToolPickup2D> ActivePickups =>
            ActiveInternal;

        public event Action<HandToolPickup2D> PickedUp;
        public event Action<HandToolPickup2D> Dropped;
        public event Action<HandToolPickup2D, int> UsesChanged;

        public HandToolKind Kind => kind;
        public int MaximumUses => maximumUses;
        public int RemainingUses => remainingUses;
        public bool HasFiniteUses => maximumUses > 0;
        public bool HasUsesRemaining => !HasFiniteUses || remainingUses > 0;
        public bool IsHeld { get; private set; }
        public bool IsAvailableForPickup => isPickupAvailable;
        public SpriteRenderer ToolRenderer => toolRenderer;

        public void Configure(
            HandToolKind toolKind,
            int configuredMaximumUses,
            Collider2D targetCollider = null,
            SpriteRenderer targetRenderer = null)
        {
            kind = toolKind;
            maximumUses = Mathf.Max(0, configuredMaximumUses);
            remainingUses = maximumUses;
            pickupCollider = targetCollider != null
                ? targetCollider
                : GetComponent<Collider2D>();
            toolRenderer = targetRenderer != null
                ? targetRenderer
                : GetComponentInChildren<SpriteRenderer>();
            isPickupAvailable = true;
        }

        private void Awake()
        {
            originalParent = transform.parent;
            if (pickupCollider == null)
            {
                pickupCollider = GetComponent<Collider2D>();
            }

            if (toolRenderer == null)
            {
                toolRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            remainingUses = Mathf.Clamp(remainingUses, 0, maximumUses);
            if (maximumUses > 0 && remainingUses == 0)
            {
                remainingUses = maximumUses;
            }
        }

        private void OnEnable()
        {
            ActiveInternal.Add(this);
        }

        private void OnDisable()
        {
            ActiveInternal.Remove(this);
        }

        public bool TryPickUp(Transform holdAnchor)
        {
            if (holdAnchor == null || IsHeld || !isPickupAvailable)
            {
                return false;
            }

            if (originalParent == null)
            {
                originalParent = transform.parent;
            }

            IsHeld = true;
            isPickupAvailable = false;
            transform.SetParent(holdAnchor, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            if (pickupCollider != null)
            {
                pickupCollider.enabled = false;
            }

            PickedUp?.Invoke(this);
            return true;
        }

        public bool Drop(Vector2 worldPosition)
        {
            if (!IsHeld)
            {
                return false;
            }

            IsHeld = false;
            isPickupAvailable = true;
            transform.SetParent(originalParent, true);
            transform.position = worldPosition;
            transform.rotation = Quaternion.identity;
            if (pickupCollider != null)
            {
                pickupCollider.enabled = true;
            }

            Dropped?.Invoke(this);
            return true;
        }

        public bool TryPlaceAt(
            Transform destination,
            bool remainAvailableForPickup = false)
        {
            if (!IsHeld || destination == null)
            {
                return false;
            }

            IsHeld = false;
            isPickupAvailable = remainAvailableForPickup;
            transform.SetParent(destination, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            if (pickupCollider != null)
            {
                pickupCollider.enabled = remainAvailableForPickup;
            }

            Dropped?.Invoke(this);
            return true;
        }

        public bool RecoverTo(Vector2 worldPosition)
        {
            if (IsHeld)
            {
                return false;
            }

            isPickupAvailable = true;
            transform.SetParent(originalParent, true);
            transform.position = worldPosition;
            transform.rotation = Quaternion.identity;
            if (pickupCollider != null)
            {
                pickupCollider.enabled = true;
            }

            return true;
        }

        public bool TryConsumeUse()
        {
            if (!HasUsesRemaining)
            {
                return false;
            }

            if (HasFiniteUses)
            {
                remainingUses--;
                UsesChanged?.Invoke(this, remainingUses);
            }

            return true;
        }

        public void SetRemainingUses(int uses)
        {
            if (!HasFiniteUses)
            {
                return;
            }

            remainingUses = Mathf.Clamp(uses, 0, maximumUses);
            UsesChanged?.Invoke(this, remainingUses);
        }

        public void Recharge()
        {
            SetRemainingUses(maximumUses);
        }
    }
}

#endif
