#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.State;
using StarNight.Map;
using UnityEngine;

namespace StarNight.Interaction.Carry
{
    public enum CarryRuntimeState
    {
        World,
        Held,
        Thrown,
        PortalSuspended,
        Recovering,
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CarryableObject : HandSlotItemRuntime,
        IMapElementWeightSource,
        IHandSlotHudSource,
        IRuntimeRoomStateParticipant,
        IResidualSimulationParticipant
    {
        public const float MaxPickupSpeed = 2.5f;
        public const float PlayerCollisionRestoreDistance = 0.35f;
        public const float PlayerCollisionRestoreSeconds = 0.08f;
        public const float MaxThrowSpeed = 10f;

        [SerializeField] private CarryObjectDefinition definition;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D[] objectColliders;
        [SerializeField] private string roomPersistenceId;

        private readonly List<Collider2D> ignoredPlayerColliders = new List<Collider2D>();
        private CarryRuntimeState runtimeState = CarryRuntimeState.World;
        private int worldLayer;
        private Vector2 throwOrigin;
        private float restorePlayerCollisionAt;
        private long lastActionId;

        public override string RuntimeItemId => definition != null ? definition.ObjectId : string.Empty;
        public override HandSlotItemKind ItemKind => HandSlotItemKind.CarryObject;
        public override bool CanEnterHandSlot => definition != null && definition.CanHandCarry;
        public override Vector2Int PlacementFootprint => definition != null
            ? definition.Footprint
            : Vector2Int.one;
        public CarryObjectDefinition Definition => definition;
        public Rigidbody2D Body => body;
        public CarryRuntimeState RuntimeState => runtimeState;
        public long LastActionId => lastActionId;
        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;
        public int PressureWeight => definition != null ? definition.PlateWeight : 1;
        public string StableItemId => definition != null ? definition.ObjectId : string.Empty;
        public string DisplayName => StableItemId;
        public Sprite HudIcon => GetComponentInChildren<SpriteRenderer>(true)?.sprite;
        public bool ShowResource => false;
        public int CurrentResource => 0;
        public int MaximumResource => 0;
        public string PrimaryActionLabel => "던지기";
        public bool IsHandTool => false;
        public string RuntimeRoomStateId => string.IsNullOrWhiteSpace(roomPersistenceId)
            ? RuntimeItemId + ":" + gameObject.name
            : roomPersistenceId;
        public bool HasResidualWork => runtimeState == CarryRuntimeState.Thrown
            || runtimeState == CarryRuntimeState.World && Velocity.sqrMagnitude > 0.01f;

        private void Awake()
        {
            EnsureReferences();
            worldLayer = gameObject.layer;
            ApplyDefinitionPhysics();
        }

        private void FixedUpdate()
        {
            if (runtimeState != CarryRuntimeState.Thrown)
            {
                return;
            }

            float distance = Vector2.Distance(throwOrigin, transform.position);
            if (distance >= PlayerCollisionRestoreDistance || Time.time >= restorePlayerCollisionAt)
            {
                RestorePlayerCollisions();
            }
        }

        public bool CanPickUp(Vector2 actorPosition)
        {
            return CanEnterHandSlot
                && runtimeState != CarryRuntimeState.Held
                && runtimeState != CarryRuntimeState.PortalSuspended
                && Vector2.Distance(actorPosition, transform.position) <= PlayerHandSlot.MaxPickupDistance
                && Velocity.magnitude < MaxPickupSpeed;
        }

        public override bool CanWorldPickup(Vector2 actorPosition) => CanPickUp(actorPosition);

        public override bool TryEnterHandSlot(HandSlotPresenter presenter)
        {
            if (presenter == null || !CanEnterHandSlot)
            {
                return false;
            }

            EnsureReferences();
            worldLayer = gameObject.layer;
            runtimeState = CarryRuntimeState.Held;
            transform.SetParent(presenter.CarrySocket, true);
            transform.position = presenter.CarrySocket.position;
            transform.rotation = presenter.CarrySocket.rotation;
            body.simulated = true;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            SetLayerIfDefined("HeldObject");
            IgnorePlayerCollisions(presenter.PlayerColliders);
            return true;
        }

        public override void ExitHandSlot(Vector2 worldPosition, bool restorePlayerCollision)
        {
            EnsureReferences();
            transform.SetParent(null, true);
            transform.position = worldPosition;
            runtimeState = CarryRuntimeState.World;
            body.simulated = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            gameObject.layer = worldLayer;
            SetLayerIfDefined("DynamicObject");
            if (restorePlayerCollision)
            {
                RestorePlayerCollisions();
            }
        }

        public void Throw(Vector2 worldPosition, Vector2 velocity, long actionId)
        {
            ExitHandSlot(worldPosition, false);
            runtimeState = CarryRuntimeState.Thrown;
            lastActionId = actionId;
            throwOrigin = worldPosition;
            restorePlayerCollisionAt = Time.time + PlayerCollisionRestoreSeconds;
            body.linearVelocity = Vector2.ClampMagnitude(velocity, MaxThrowSpeed);
        }

        public override bool CanPassPortal(ICarryPortalClearance clearance)
        {
            if (definition == null)
            {
                return false;
            }

            bool needsClearance = definition.WeightClass == CarryWeightClass.Heavy
                || definition.Footprint.y > 1;
            return !needsClearance || clearance != null && clearance.Allows(definition);
        }

        public override void SuspendForPortal(Transform carrySocket)
        {
            EnsureReferences();
            runtimeState = CarryRuntimeState.PortalSuspended;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
            if (carrySocket != null)
            {
                transform.SetParent(carrySocket, true);
                transform.position = carrySocket.position;
            }
        }

        public override bool RestoreAfterPortal(HandSlotPresenter presenter)
        {
            if (presenter == null)
            {
                return false;
            }

            return TryEnterHandSlot(presenter);
        }

        public void BeginRecovery()
        {
            EnsureReferences();
            runtimeState = CarryRuntimeState.Recovering;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }

        public void RecoverTo(Vector2 worldPosition)
        {
            EnsureReferences();
            transform.SetParent(null, true);
            transform.position = worldPosition;
            runtimeState = CarryRuntimeState.World;
            body.simulated = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            SetLayerIfDefined("DynamicObject");
            RestorePlayerCollisions();
        }

        public void RestoreIgnoredPlayerCollisionsForTests()
        {
            RestorePlayerCollisions();
        }

        public void ConfigureForTests(CarryObjectDefinition objectDefinition, Rigidbody2D configuredBody = null)
        {
            definition = objectDefinition;
            body = configuredBody != null ? configuredBody : GetComponent<Rigidbody2D>();
            objectColliders = GetComponentsInChildren<Collider2D>(true);
            worldLayer = gameObject.layer;
            ApplyDefinitionPhysics();
        }

        public string CaptureRuntimeRoomState()
        {
            return JsonUtility.ToJson(CarryObjectSnapshot.Capture(this, runtimeState == CarryRuntimeState.Held));
        }

        public void RestoreRuntimeRoomState(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            CarryObjectSnapshot snapshot = JsonUtility.FromJson<CarryObjectSnapshot>(payload);
            if (definition != null
                && !string.Equals(snapshot.ObjectId, definition.ObjectId, System.StringComparison.Ordinal))
            {
                return;
            }

            EnsureReferences();
            gameObject.SetActive(snapshot.Active);
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(
                snapshot.Position,
                Quaternion.Euler(0f, 0f, snapshot.Rotation));
            runtimeState = snapshot.HeldInHandSlot
                ? CarryRuntimeState.World
                : snapshot.RuntimeState;
            lastActionId = snapshot.LastActionId;
            if (body != null)
            {
                body.bodyType = definition != null && definition.WeightClass == CarryWeightClass.Fixed
                    ? RigidbodyType2D.Static
                    : RigidbodyType2D.Dynamic;
                body.linearVelocity = snapshot.Velocity;
                body.angularVelocity = 0f;
                body.simulated = false;
            }
            SetLayerIfDefined("DynamicObject");
        }

        public void BeginResidualSimulation()
        {
            EnsureReferences();
            if (HasResidualWork && body != null)
            {
                body.simulated = true;
            }
        }

        public void TickResidualSimulation(float deltaSeconds)
        {
        }

        public void FreezeResidualSimulation(bool timedOut)
        {
            EnsureReferences();
            if (body != null)
            {
                if (timedOut)
                {
                    body.linearVelocity = Vector2.zero;
                    body.angularVelocity = 0f;
                }
                body.simulated = false;
            }
        }

        public void SetRoomPersistenceId(string persistenceId)
        {
            roomPersistenceId = persistenceId ?? string.Empty;
        }

        private void EnsureReferences()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (objectColliders == null || objectColliders.Length == 0)
            {
                objectColliders = GetComponentsInChildren<Collider2D>(true);
            }
        }

        private void ApplyDefinitionPhysics()
        {
            if (body == null || definition == null)
            {
                return;
            }

            if (definition.WeightClass == CarryWeightClass.Fixed)
            {
                body.bodyType = RigidbodyType2D.Static;
                return;
            }

            body.mass = definition.Mass;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void IgnorePlayerCollisions(Collider2D[] playerColliders)
        {
            ignoredPlayerColliders.Clear();
            if (playerColliders == null)
            {
                return;
            }

            for (int objectIndex = 0; objectIndex < objectColliders.Length; objectIndex++)
            {
                Collider2D objectCollider = objectColliders[objectIndex];
                if (objectCollider == null)
                {
                    continue;
                }

                for (int playerIndex = 0; playerIndex < playerColliders.Length; playerIndex++)
                {
                    Collider2D playerCollider = playerColliders[playerIndex];
                    if (playerCollider == null)
                    {
                        continue;
                    }

                    Physics2D.IgnoreCollision(objectCollider, playerCollider, true);
                    if (!ignoredPlayerColliders.Contains(playerCollider))
                    {
                        ignoredPlayerColliders.Add(playerCollider);
                    }
                }
            }
        }

        private void RestorePlayerCollisions()
        {
            if (ignoredPlayerColliders.Count == 0)
            {
                return;
            }

            for (int objectIndex = 0; objectIndex < objectColliders.Length; objectIndex++)
            {
                Collider2D objectCollider = objectColliders[objectIndex];
                if (objectCollider == null)
                {
                    continue;
                }

                for (int playerIndex = 0; playerIndex < ignoredPlayerColliders.Count; playerIndex++)
                {
                    Collider2D playerCollider = ignoredPlayerColliders[playerIndex];
                    if (playerCollider != null)
                    {
                        Physics2D.IgnoreCollision(objectCollider, playerCollider, false);
                    }
                }
            }

            ignoredPlayerColliders.Clear();
        }

        private void SetLayerIfDefined(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
            {
                gameObject.layer = layer;
            }
        }
    }
}

#endif
