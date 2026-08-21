#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Objects
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class CarryableObject2D : MonoBehaviour
    {
        private static readonly HashSet<CarryableObject2D> ActiveObjectsInternal =
            new HashSet<CarryableObject2D>();

        [Header("References")]
        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D objectCollider;

        [Header("Object rules")]
        [SerializeField] private WorldObjectTraits traits =
            WorldObjectTraits.Carryable | WorldObjectTraits.Pullable;
        [SerializeField, Min(0.05f)] private float objectMass = 1f;
        [SerializeField, Min(0f)] private float throwImpulse = 6.5f;
        [SerializeField] private Vector2 footprint = new Vector2(0.82f, 0.82f);

        [Header("Cell settling")]
        [SerializeField, Min(0f)] private float restSpeed = 0.12f;
        [SerializeField, Min(0f)] private float restDuration = 0.25f;
        [SerializeField, Min(0f)] private float snapSpeed = 2.5f;
        [SerializeField, Min(0f)] private float maximumSnapDistance = 0.32f;

        [Header("Important item recovery")]
        [SerializeField] private bool respawnWhenOutOfBounds;
        [SerializeField, Min(0f)] private float outOfBoundsMargin = 1f;

        private Transform holdAnchor;
        private Transform originalParent;
        private Vector2 respawnPosition;
        private float restTimer;
        private GridPos occupiedCell;
        private bool hasOccupiedCell;
        private bool respawnPointInitialized;

        public static IReadOnlyCollection<CarryableObject2D> ActiveObjects => ActiveObjectsInternal;

        public event Action<CarryableObject2D> PickedUp;
        public event Action<CarryableObject2D> Dropped;
        public event Action<CarryableObject2D, Vector2> Thrown;
        public event Action<CarryableObject2D, Vector2> ImportantItemRespawned;

        public static event Action<CarryableObject2D, Vector2> AnyImportantItemRespawned;

        public Rigidbody2D Body => body;
        public Collider2D ObjectCollider => objectCollider;
        public WorldObjectTraits Traits => traits;
        public bool IsHeld { get; private set; }
        public bool CanBeCarried =>
            enabled
            && gameObject.activeInHierarchy
            && !IsHeld
            && (traits & WorldObjectTraits.Carryable) != 0;
        public bool IsImportant =>
            respawnWhenOutOfBounds || (traits & WorldObjectTraits.Sacred) != 0;
        public float ThrowImpulse => throwImpulse;
        public Vector2 RespawnPosition => respawnPosition;

        public void Configure(
            GridWorld world,
            Rigidbody2D targetBody,
            Collider2D targetCollider,
            WorldObjectTraits configuredTraits,
            float configuredMass = 1f,
            float configuredThrowImpulse = 6.5f,
            bool importantItem = false)
        {
            gridWorld = world;
            body = targetBody;
            objectCollider = targetCollider;
            traits = configuredTraits;
            objectMass = Mathf.Max(0.05f, configuredMass);
            throwImpulse = Mathf.Max(0f, configuredThrowImpulse);
            respawnWhenOutOfBounds = importantItem;
            InitializePhysics();
            SetRespawnPoint(body != null ? body.position : (Vector2)transform.position);
        }

        public void Configure(
            GridWorld world,
            WorldObjectTraits configuredTraits,
            float configuredMass = 1f,
            float configuredThrowImpulse = 6.5f,
            bool importantItem = false)
        {
            Configure(
                world,
                GetComponent<Rigidbody2D>(),
                GetComponent<Collider2D>(),
                configuredTraits,
                configuredMass,
                configuredThrowImpulse,
                importantItem);
        }

        public void SetRespawnPoint(Vector2 worldPosition)
        {
            respawnPosition = worldPosition;
            respawnPointInitialized = true;
        }

        private void Awake()
        {
            if (gridWorld == null)
            {
                gridWorld = FindFirstObjectByType<GridWorld>();
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (objectCollider == null)
            {
                objectCollider = GetComponent<Collider2D>();
            }

            originalParent = transform.parent;
            InitializePhysics();

            if (!respawnPointInitialized)
            {
                SetRespawnPoint(body != null ? body.position : (Vector2)transform.position);
            }
        }

        private void OnEnable()
        {
            ActiveObjectsInternal.Add(this);
        }

        private void OnDisable()
        {
            ActiveObjectsInternal.Remove(this);
            ReleaseOccupiedCell();
            IsHeld = false;
            holdAnchor = null;
        }

        private void FixedUpdate()
        {
            if (body == null || IsHeld)
            {
                return;
            }

            if (IsImportant && IsOutsideWorld())
            {
                RespawnImportantItem();
                return;
            }

            if (body.bodyType != RigidbodyType2D.Dynamic || !body.simulated)
            {
                return;
            }

            float restSpeedSquared = restSpeed * restSpeed;
            bool resting = body.linearVelocity.sqrMagnitude <= restSpeedSquared
                && Mathf.Abs(body.angularVelocity) <= restSpeed;

            if (!resting)
            {
                restTimer = 0f;
                ReleaseOccupiedCell();
                return;
            }

            restTimer += Time.fixedDeltaTime;
            if (restTimer < restDuration)
            {
                return;
            }

            TrySnapTowardCell(Time.fixedDeltaTime);
        }

        public bool TryPickUp(Transform targetHoldAnchor)
        {
            if (!CanBeCarried || targetHoldAnchor == null || body == null)
            {
                return false;
            }

            ReleaseOccupiedCell();
            restTimer = 0f;
            holdAnchor = targetHoldAnchor;
            IsHeld = true;

            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
            transform.SetParent(targetHoldAnchor, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            PickedUp?.Invoke(this);
            return true;
        }

        public bool PickUp(Transform targetHoldAnchor)
        {
            return TryPickUp(targetHoldAnchor);
        }

        public bool Drop(Vector2 worldPosition, Vector2 inheritedVelocity)
        {
            if (!IsHeld || body == null)
            {
                return false;
            }

            transform.SetParent(originalParent, true);
            transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            holdAnchor = null;
            IsHeld = false;

            body.simulated = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.position = worldPosition;
            body.linearVelocity = inheritedVelocity;
            body.angularVelocity = 0f;
            body.WakeUp();
            Physics2D.SyncTransforms();

            restTimer = 0f;
            Dropped?.Invoke(this);
            return true;
        }

        public bool Throw(Vector2 direction, Vector2 inheritedVelocity)
        {
            if (!IsHeld || direction.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            Vector2 releasePosition = holdAnchor != null
                ? (Vector2)holdAnchor.position
                : (Vector2)transform.position;
            Vector2 normalizedDirection = direction.normalized;

            if (!Drop(releasePosition, inheritedVelocity))
            {
                return false;
            }

            body.AddForce(normalizedDirection * throwImpulse, ForceMode2D.Impulse);
            Thrown?.Invoke(this, normalizedDirection);
            return true;
        }

        public bool RespawnImportantItem()
        {
            if (!IsImportant || body == null)
            {
                return false;
            }

            ReleaseOccupiedCell();
            if (IsHeld)
            {
                transform.SetParent(originalParent, true);
            }

            IsHeld = false;
            holdAnchor = null;
            transform.position = new Vector3(
                respawnPosition.x,
                respawnPosition.y,
                transform.position.z);
            body.simulated = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.position = respawnPosition;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.WakeUp();
            restTimer = 0f;
            Physics2D.SyncTransforms();

            ImportantItemRespawned?.Invoke(this, respawnPosition);
            AnyImportantItemRespawned?.Invoke(this, respawnPosition);
            return true;
        }

        public bool RelocateTo(Vector2 worldPosition)
        {
            if (IsHeld || body == null)
            {
                return false;
            }

            ReleaseOccupiedCell();
            transform.SetParent(originalParent, true);
            transform.position = new Vector3(
                worldPosition.x,
                worldPosition.y,
                transform.position.z);
            body.simulated = true;
            body.position = worldPosition;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.WakeUp();
            restTimer = 0f;
            Physics2D.SyncTransforms();
            return true;
        }

        private void InitializePhysics()
        {
            if (body == null)
            {
                return;
            }

            body.mass = Mathf.Max(0.05f, objectMass);
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.freezeRotation = true;
        }

        private void TrySnapTowardCell(float deltaTime)
        {
            if (gridWorld == null || body == null)
            {
                return;
            }

            GridPos cell = gridWorld.WorldToCell(body.position);
            if (!gridWorld.CanStandAt(cell, footprint))
            {
                ReleaseOccupiedCell();
                return;
            }

            Vector2 target = gridWorld.CellToWorldCenter(cell);
            float distance = Vector2.Distance(body.position, target);
            if (distance > maximumSnapDistance)
            {
                ReleaseOccupiedCell();
                return;
            }

            if ((!hasOccupiedCell || occupiedCell != cell)
                && !gridWorld.TryOccupy(cell, this))
            {
                return;
            }

            if (hasOccupiedCell && occupiedCell != cell)
            {
                gridWorld.Release(occupiedCell, this);
            }

            occupiedCell = cell;
            hasOccupiedCell = true;
            Vector2 snappedPosition = Vector2.MoveTowards(
                body.position,
                target,
                snapSpeed * deltaTime);
            body.MovePosition(snappedPosition);

            if ((snappedPosition - target).sqrMagnitude <= 0.0001f)
            {
                body.position = target;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
        }

        private bool IsOutsideWorld()
        {
            if (gridWorld == null || body == null)
            {
                return false;
            }

            Rect bounds = gridWorld.WorldBounds;
            bounds.xMin -= outOfBoundsMargin;
            bounds.xMax += outOfBoundsMargin;
            bounds.yMin -= outOfBoundsMargin;
            bounds.yMax += outOfBoundsMargin;
            return !bounds.Contains(body.position);
        }

        private void ReleaseOccupiedCell()
        {
            if (!hasOccupiedCell)
            {
                return;
            }

            if (gridWorld != null)
            {
                gridWorld.Release(occupiedCell, this);
            }

            hasOccupiedCell = false;
        }
    }
}

#endif
