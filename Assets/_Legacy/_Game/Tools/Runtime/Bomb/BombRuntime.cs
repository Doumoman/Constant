#if LEGACY_DISABLED
using System;
using StarNight.Interaction.Carry;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Interaction.State;
using UnityEngine;

namespace StarNight.Tools.Bomb
{
    public enum BombRuntimeState
    {
        World,
        Held,
        Thrown,
        PortalSuspended,
        Exploded,
    }

    public enum BombSimulationMode
    {
        Active,
        Residual,
        Frozen,
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class BombRuntime : HandSlotItemRuntime,
        IHandSlotHudSource,
        IRuntimeRoomStateParticipant,
        IResidualSimulationParticipant
    {
        private static int nextExplosionId = 1;

        [SerializeField] private BombDefinition definition;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D[] bombColliders;
        [SerializeField] private BombExplosionDispatcher explosionDispatcher;
        [SerializeField] private float remainingFuse;
        [SerializeField] private BombRuntimeState runtimeState;
        [SerializeField] private BombSimulationMode simulationMode = BombSimulationMode.Active;
        [SerializeField] private string roomPersistenceId;

        private PlayerHandSlot owningSlot;
        private GameObject instigator;
        private int worldLayer;
        private bool warningRaised;
        private bool armed;
        private long spawnActionId;

        public event Action<BombRuntime> LastWarningStarted;
        public event Action<BombRuntime, BombExplosionReport> Exploded;

        public override string RuntimeItemId => $"bomb:{GetInstanceID()}";
        public override HandSlotItemKind ItemKind => HandSlotItemKind.ArmedBombCarry;
        public override bool CanEnterHandSlot => armed
            && runtimeState != BombRuntimeState.Exploded
            && remainingFuse > PickupMinimumFuse;
        public BombDefinition Definition => definition;
        public Rigidbody2D Body => body;
        public GameObject Instigator => instigator;
        public BombRuntimeState RuntimeState => runtimeState;
        public BombSimulationMode SimulationMode => simulationMode;
        public float RemainingFuse => remainingFuse;
        public bool IsArmed => armed;
        public bool IsExploded => runtimeState == BombRuntimeState.Exploded;
        public long SpawnActionId => spawnActionId;
        public float PickupMinimumFuse => definition != null
            ? definition.PickupMinimumFuseSeconds
            : BombDefinition.ApprovedPickupMinimumFuseSeconds;
        public string StableItemId => "BOMB_ARMED";
        public string DisplayName => "폭탄";
        public Sprite HudIcon => GetComponentInChildren<SpriteRenderer>(true)?.sprite;
        public bool ShowResource => false;
        public int CurrentResource => 0;
        public int MaximumResource => 0;
        public string PrimaryActionLabel => "던지기";
        public bool IsHandTool => false;
        public string RuntimeRoomStateId => string.IsNullOrWhiteSpace(roomPersistenceId)
            ? RuntimeItemId
            : roomPersistenceId;
        public bool HasResidualWork => armed && !IsExploded;

        private void Awake()
        {
            EnsureReferences();
            worldLayer = gameObject.layer;
        }

        private void Update()
        {
            if (simulationMode == BombSimulationMode.Active)
            {
                TickFuse(Time.deltaTime);
            }
        }

        public bool Arm(
            BombDefinition configuredDefinition,
            GameObject configuredInstigator,
            Vector2 velocity,
            long actionId,
            BombExplosionDispatcher dispatcher = null)
        {
            if (configuredDefinition == null || armed || runtimeState == BombRuntimeState.Exploded)
            {
                return false;
            }

            EnsureReferences();
            definition = configuredDefinition;
            instigator = configuredInstigator;
            spawnActionId = actionId;
            explosionDispatcher = dispatcher != null ? dispatcher : explosionDispatcher;
            remainingFuse = definition.FuseSeconds;
            warningRaised = false;
            armed = true;
            runtimeState = velocity.sqrMagnitude > 0.0001f
                ? BombRuntimeState.Thrown
                : BombRuntimeState.World;
            simulationMode = BombSimulationMode.Active;
            body.simulated = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.linearVelocity = velocity;
            body.angularVelocity = 0f;
            SetLayerIfDefined("DynamicObject");
            return true;
        }

        public void TickFuse(float deltaSeconds)
        {
            if (!armed || IsExploded || simulationMode == BombSimulationMode.Frozen || deltaSeconds <= 0f)
            {
                return;
            }

            remainingFuse = Mathf.Max(0f, remainingFuse - deltaSeconds);
            float warningSeconds = definition != null
                ? definition.LastWarningSeconds
                : BombDefinition.ApprovedLastWarningSeconds;
            if (!warningRaised && remainingFuse <= warningSeconds)
            {
                warningRaised = true;
                LastWarningStarted?.Invoke(this);
            }

            if (remainingFuse <= 0f)
            {
                ExplodeNow();
            }
        }

        public bool ReduceFuseForChain()
        {
            if (!armed || IsExploded)
            {
                return false;
            }

            float chainFuse = definition != null
                ? definition.ChainFuseSeconds
                : BombDefinition.ApprovedChainFuseSeconds;
            if (remainingFuse <= chainFuse)
            {
                return false;
            }

            remainingFuse = chainFuse;
            return true;
        }

        public BombExplosionReport ExplodeNow()
        {
            if (!armed || IsExploded)
            {
                return default;
            }

            remainingFuse = 0f;
            runtimeState = BombRuntimeState.Exploded;
            owningSlot?.TryReleaseCurrent(this);
            owningSlot = null;
            transform.SetParent(null, true);
            EnsureReferences();
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
            for (int index = 0; index < bombColliders.Length; index++)
            {
                if (bombColliders[index] != null)
                {
                    bombColliders[index].enabled = false;
                }
            }

            int explosionId = nextExplosionId++;
            if (nextExplosionId <= 0)
            {
                nextExplosionId = 1;
            }
            BombExplosionReport report = explosionDispatcher != null
                ? explosionDispatcher.Dispatch(this, explosionId)
                : default;
            Exploded?.Invoke(this, report);
            return report;
        }

        public override bool CanWorldPickup(Vector2 actorPosition)
        {
            return CanEnterHandSlot
                && runtimeState != BombRuntimeState.Held
                && runtimeState != BombRuntimeState.PortalSuspended
                && Vector2.Distance(actorPosition, transform.position) <= PlayerHandSlot.MaxPickupDistance;
        }

        public override bool TryEnterHandSlot(HandSlotPresenter presenter)
        {
            if (presenter == null || !CanEnterHandSlot)
            {
                return false;
            }

            EnsureReferences();
            if (runtimeState != BombRuntimeState.PortalSuspended)
            {
                worldLayer = gameObject.layer;
            }
            owningSlot = presenter.GetComponentInParent<PlayerHandSlot>();
            runtimeState = BombRuntimeState.Held;
            transform.SetParent(presenter.CarrySocket, true);
            transform.SetPositionAndRotation(presenter.CarrySocket.position, presenter.CarrySocket.rotation);
            body.simulated = false;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            SetLayerIfDefined("HeldObject");
            return true;
        }

        public override void ExitHandSlot(Vector2 worldPosition, bool restorePlayerCollision)
        {
            if (IsExploded)
            {
                return;
            }

            owningSlot = null;
            EnterWorld(worldPosition, Vector2.zero, BombRuntimeState.World);
        }

        public override bool TryPrimaryUse(
            PlayerHandSlot owner,
            PlayerActionContext context,
            int facingSign,
            LayerMask blockMask)
        {
            if (owner == null || owner.CurrentItem != this || IsExploded)
            {
                return false;
            }

            int facing = facingSign < 0 ? -1 : 1;
            Vector2 velocity = context.LookVertical > 0.5f
                ? new Vector2(facing * 1.5f, 6.5f)
                : new Vector2(facing * 5.2f, 1.8f);
            Vector2 worldPosition = (Vector2)owner.transform.position
                + new Vector2(facing * 0.72f, 0.25f);
            if (!owner.TryReleaseCurrent(this))
            {
                return false;
            }

            owningSlot = null;
            EnterWorld(worldPosition, velocity, BombRuntimeState.Thrown);
            return true;
        }

        public override bool CanPassPortal(ICarryPortalClearance clearance) => armed && !IsExploded;

        public override void SuspendForPortal(Transform carrySocket)
        {
            if (IsExploded)
            {
                return;
            }

            EnsureReferences();
            runtimeState = BombRuntimeState.PortalSuspended;
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
            return presenter != null && !IsExploded && TryEnterHandSlot(presenter);
        }

        public void SetSimulationMode(BombSimulationMode mode)
        {
            simulationMode = mode;
            if (mode == BombSimulationMode.Frozen && body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.simulated = false;
            }
        }

        public BombSnapshot CaptureSnapshot()
        {
            EnsureReferences();
            return new BombSnapshot
            {
                RuntimeId = RuntimeItemId,
                Position = transform.position,
                Velocity = body != null ? body.linearVelocity : Vector2.zero,
                RemainingFuse = remainingFuse,
                Exploded = IsExploded,
                Armed = armed,
                RuntimeState = runtimeState,
                SimulationMode = simulationMode,
                Active = gameObject.activeSelf,
            };
        }

        public bool RestoreSnapshot(BombSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return false;
            }

            EnsureReferences();
            gameObject.SetActive(snapshot.Active);
            transform.SetParent(null, true);
            transform.position = snapshot.Position;
            armed = snapshot.Armed;
            remainingFuse = Mathf.Max(0f, snapshot.RemainingFuse);
            runtimeState = snapshot.Exploded ? BombRuntimeState.Exploded : snapshot.RuntimeState;
            simulationMode = snapshot.Exploded
                ? BombSimulationMode.Frozen
                : BombSimulationMode.Active;
            warningRaised = definition != null && remainingFuse <= definition.LastWarningSeconds;
            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Dynamic;
                body.linearVelocity = snapshot.Velocity;
                body.angularVelocity = 0f;
                body.simulated = false;
            }
            bool enableColliders = !snapshot.Exploded;
            for (int index = 0; index < bombColliders.Length; index++)
            {
                if (bombColliders[index] != null)
                {
                    bombColliders[index].enabled = enableColliders;
                }
            }
            SetLayerIfDefined("DynamicObject");
            return true;
        }

        public string CaptureRuntimeRoomState() => JsonUtility.ToJson(CaptureSnapshot());

        public void RestoreRuntimeRoomState(string payload)
        {
            if (!string.IsNullOrWhiteSpace(payload))
            {
                RestoreSnapshot(JsonUtility.FromJson<BombSnapshot>(payload));
            }
        }

        public void BeginResidualSimulation()
        {
            EnsureReferences();
            simulationMode = BombSimulationMode.Residual;
            if (!IsExploded && body != null)
            {
                body.simulated = true;
            }
        }

        public void TickResidualSimulation(float deltaSeconds)
        {
            TickFuse(deltaSeconds);
        }

        public void FreezeResidualSimulation(bool timedOut)
        {
            SetSimulationMode(BombSimulationMode.Frozen);
        }

        public void SetRoomPersistenceId(string persistenceId)
        {
            roomPersistenceId = persistenceId ?? string.Empty;
        }

        public void ConfigureForTests(
            BombDefinition configuredDefinition,
            Rigidbody2D configuredBody,
            BombExplosionDispatcher dispatcher = null)
        {
            definition = configuredDefinition;
            body = configuredBody != null ? configuredBody : GetComponent<Rigidbody2D>();
            bombColliders = GetComponentsInChildren<Collider2D>(true);
            explosionDispatcher = dispatcher;
            worldLayer = gameObject.layer;
        }

        private void EnterWorld(Vector2 worldPosition, Vector2 velocity, BombRuntimeState state)
        {
            EnsureReferences();
            transform.SetParent(null, true);
            transform.position = worldPosition;
            runtimeState = state;
            body.simulated = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.linearVelocity = velocity;
            body.angularVelocity = 0f;
            gameObject.layer = worldLayer;
            SetLayerIfDefined("DynamicObject");
        }

        private void EnsureReferences()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }
            if (bombColliders == null || bombColliders.Length == 0)
            {
                bombColliders = GetComponentsInChildren<Collider2D>(true);
            }
            if (explosionDispatcher == null)
            {
                explosionDispatcher = GetComponent<BombExplosionDispatcher>();
            }
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
