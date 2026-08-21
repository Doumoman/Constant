#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using StarNight.Player;
using StarNight.Stages.P5;
using StarNight.Tools;
using UnityEngine;

namespace StarNight.Objects
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputAdapter))]
    public sealed class CarrySystem : MonoBehaviour
    {
        [SerializeField] private PlayerInputAdapter input;
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private Transform holdAnchor;
        [SerializeField] private PlayerToolInventory2D toolInventory;
        [SerializeField, Min(0.1f)] private float pickupRadius = 1.25f;
        [SerializeField, Min(0f)] private float horizontalDropOffset = 0.85f;
        [SerializeField, Range(0f, 1f)] private float verticalAimRatio = 0.65f;
        [SerializeField, Range(0f, 1f)] private float verticalAimThreshold = 0.35f;

        private float facingDirection = 1f;
        private bool interactionLocked;

        public event Action<CarryableObject2D> HeldObjectChanged;
        public event Action<CarryableObject2D> ObjectThrown;

        public CarryableObject2D HeldObject { get; private set; }
        public bool IsCarrying => HeldObject != null && HeldObject.IsHeld;
        public Transform HoldAnchor => holdAnchor;
        public float FacingDirection => facingDirection;
        public PlayerToolInventory2D ToolInventory => toolInventory;
        public bool InteractionLocked => interactionLocked;

        public void Configure(
            PlayerInputAdapter inputAdapter,
            Rigidbody2D targetPlayerBody,
            Transform targetHoldAnchor,
            GridWorld world = null)
        {
            input = inputAdapter;
            playerBody = targetPlayerBody;
            holdAnchor = targetHoldAnchor;
            gridWorld = world;
            EnsureHoldAnchor();
        }

        private void Awake()
        {
            if (input == null)
            {
                input = GetComponent<PlayerInputAdapter>();
            }

            if (playerBody == null)
            {
                playerBody = GetComponent<Rigidbody2D>();
            }

            if (gridWorld == null)
            {
                gridWorld = FindFirstObjectByType<GridWorld>();
            }

            if (toolInventory == null)
            {
                toolInventory = GetComponent<PlayerToolInventory2D>();
            }

            EnsureHoldAnchor();
        }

        private void Update()
        {
            if (input == null || interactionLocked)
            {
                return;
            }

            UpdateFacing(input.Move.x);

            if (input.ConsumeInteractPressed())
            {
                ProcessInteract();
            }

            if (input.ConsumeUseHeldToolPressed())
            {
                ProcessUseHeldTool();
            }

            if (HeldObject != null && !HeldObject.IsHeld)
            {
                HeldObject = null;
                HeldObjectChanged?.Invoke(null);
            }
        }

        private void OnDisable()
        {
            if (HeldObject != null && HeldObject.IsHeld)
            {
                DropHeld();
            }

            toolInventory?.DropHeldTool();
        }

        public bool ProcessInteract()
        {
            if (interactionLocked)
            {
                return false;
            }

            if (P5ContextInteractable2D.TryInteractNearest(
                    transform,
                    this,
                    toolInventory))
            {
                return true;
            }

            if (toolInventory != null && toolInventory.TryPickupNearestTool())
            {
                return true;
            }

            CarryableObject2D candidate = FindNearestCandidate(HeldObject);
            if (candidate != null)
            {
                return TryPickup(candidate);
            }

            if (toolInventory != null && toolInventory.TryContextInteract())
            {
                return true;
            }

            return DropHeld()
                || (toolInventory != null && toolInventory.DropHeldTool());
        }

        public bool ProcessUseHeldTool()
        {
            if (interactionLocked)
            {
                return false;
            }

            if (toolInventory != null && toolInventory.HasHeldTool)
            {
                return toolInventory.TryUseHeldTool();
            }

            return ThrowHeld(CalculateAimDirection());
        }

        public bool ProcessInteractForTests()
        {
            return ProcessInteract();
        }

        public bool ProcessUseHeldToolForTests()
        {
            return ProcessUseHeldTool();
        }

        public bool TryPickup(CarryableObject2D candidate)
        {
            if (candidate == null || candidate == HeldObject || !candidate.CanBeCarried)
            {
                return candidate != null && candidate == HeldObject;
            }

            if (HeldObject != null)
            {
                DropHeld();
            }

            toolInventory?.DropHeldTool();
            EnsureHoldAnchor();
            if (holdAnchor == null || !candidate.TryPickUp(holdAnchor))
            {
                return false;
            }

            HeldObject = candidate;
            HeldObjectChanged?.Invoke(HeldObject);
            return true;
        }

        public bool DropHeld()
        {
            Vector2 releasePosition = CalculateDropPosition();
            Vector2 inheritedVelocity = playerBody != null
                ? playerBody.linearVelocity
                : Vector2.zero;
            return DropHeldAt(releasePosition, inheritedVelocity);
        }

        public bool DropHeldAt(
            Vector2 worldPosition,
            Vector2 inheritedVelocity)
        {
            if (HeldObject == null)
            {
                return false;
            }

            CarryableObject2D releasedObject = HeldObject;
            if (!releasedObject.Drop(worldPosition, inheritedVelocity))
            {
                if (!releasedObject.IsHeld)
                {
                    HeldObject = null;
                    HeldObjectChanged?.Invoke(null);
                }

                return false;
            }

            HeldObject = null;
            HeldObjectChanged?.Invoke(null);
            return true;
        }

        public bool ThrowHeld(Vector2 direction)
        {
            if (HeldObject == null)
            {
                return false;
            }

            CarryableObject2D thrownObject = HeldObject;
            Vector2 inheritedVelocity = playerBody != null
                ? playerBody.linearVelocity
                : Vector2.zero;
            if (!thrownObject.Throw(direction, inheritedVelocity))
            {
                return false;
            }

            HeldObject = null;
            HeldObjectChanged?.Invoke(null);
            ObjectThrown?.Invoke(thrownObject);
            return true;
        }

        public CarryableObject2D FindNearestCandidate(
            CarryableObject2D excludedObject = null)
        {
            Vector2 searchOrigin = holdAnchor != null
                ? (Vector2)holdAnchor.position
                : (Vector2)transform.position;
            float maximumDistanceSquared = pickupRadius * pickupRadius;
            float bestDistanceSquared = maximumDistanceSquared;
            CarryableObject2D bestCandidate = null;

            foreach (CarryableObject2D candidate in CarryableObject2D.ActiveObjects)
            {
                if (candidate == null
                    || candidate == excludedObject
                    || !candidate.CanBeCarried)
                {
                    continue;
                }

                float distanceSquared =
                    ((Vector2)candidate.transform.position - searchOrigin).sqrMagnitude;
                if (distanceSquared > bestDistanceSquared)
                {
                    continue;
                }

                bestDistanceSquared = distanceSquared;
                bestCandidate = candidate;
            }

            return bestCandidate;
        }

        public Vector2 CalculateAimDirection()
        {
            float verticalInput = input != null ? input.Move.y : 0f;
            float vertical = Mathf.Abs(verticalInput) >= verticalAimThreshold
                ? Mathf.Sign(verticalInput) * verticalAimRatio
                : 0f;
            return new Vector2(facingDirection, vertical).normalized;
        }

        public void SetFacingForTests(float direction)
        {
            UpdateFacing(direction);
        }

        public void AttachToolInventory(PlayerToolInventory2D inventory)
        {
            toolInventory = inventory;
        }

        public void SetInteractionLocked(bool locked)
        {
            interactionLocked = locked;
        }

        private void UpdateFacing(float horizontalInput)
        {
            if (Mathf.Abs(horizontalInput) > 0.01f)
            {
                facingDirection = Mathf.Sign(horizontalInput);
            }
        }

        private Vector2 CalculateDropPosition()
        {
            Vector2 origin = playerBody != null
                ? playerBody.position
                : (Vector2)transform.position;
            Vector2 desired = origin + Vector2.right * facingDirection * horizontalDropOffset;

            if (gridWorld == null)
            {
                return desired;
            }

            GridPos desiredCell = gridWorld.WorldToCell(desired);
            if (gridWorld.IsWithinBounds(desiredCell)
                && !gridWorld.IsSolid(desiredCell)
                && !gridWorld.IsHazard(desiredCell))
            {
                return gridWorld.CellToWorldCenter(desiredCell);
            }

            GridPos currentCell = gridWorld.WorldToCell(origin);
            if (gridWorld.IsWithinBounds(currentCell)
                && !gridWorld.IsSolid(currentCell)
                && !gridWorld.IsHazard(currentCell))
            {
                return gridWorld.CellToWorldCenter(currentCell);
            }

            return holdAnchor != null ? (Vector2)holdAnchor.position : origin;
        }

        private void EnsureHoldAnchor()
        {
            if (holdAnchor != null)
            {
                return;
            }

            Transform existing = transform.Find("CarryAnchor");
            if (existing != null)
            {
                holdAnchor = existing;
                return;
            }

            GameObject anchorObject = new GameObject("CarryAnchor");
            holdAnchor = anchorObject.transform;
            holdAnchor.SetParent(transform, false);
            holdAnchor.localPosition = new Vector3(0.62f, 0.15f, 0f);
        }
    }
}

#endif
