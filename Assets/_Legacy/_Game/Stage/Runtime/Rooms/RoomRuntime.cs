#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Interaction.Carry;
using StarNight.Interaction.State;
using StarNight.Map;
using StarNight.Player.Safety;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace StarNight.Stage.Rooms
{
    [DisallowMultipleComponent]
    public sealed class RoomRuntime : MonoBehaviour
    {
        public const float MaximumResidualSimulationSeconds = 3f;

        [SerializeField] private string roomId;
        [SerializeField] private RoomDimension dimension = RoomDimension.Main;
        [SerializeField] private Vector2Int sizeCells = new Vector2Int(24, 8);
        [SerializeField] private RoomCameraMode cameraMode = RoomCameraMode.Fixed;
        [SerializeField] private RoomSimulationState simulationState = RoomSimulationState.Dormant;
        [SerializeField] private Transform gridLogic;
        [SerializeField] private Transform gridVisual;
        [SerializeField] private Transform portalRoot;
        [SerializeField] private Transform dynamicRoot;
        [SerializeField] private Transform safeCellRoot;
        [SerializeField] private Transform voidRecoveryRoot;
        [SerializeField] private Transform cameraAnchor;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Collider2D cameraBoundsCollider;
        [SerializeField] private Collider2D voidRecoveryZone;
        [SerializeField] private Collider2D hardFailSafePlane;
        [SerializeField] private bool geometryApproved;
        [SerializeField] private LayerMask recoveryBlockMask;

        private readonly RoomPersistentState persistentState = new RoomPersistentState();
        private readonly List<IRoomPersistentParticipant> persistentParticipants = new List<IRoomPersistentParticipant>();
        private readonly List<IRoomSimulationParticipant> simulationParticipants = new List<IRoomSimulationParticipant>();
        private readonly List<IMapElementPersistentParticipant> mapPersistentParticipants = new List<IMapElementPersistentParticipant>();
        private readonly List<IMapElementSimulationParticipant> mapSimulationParticipants = new List<IMapElementSimulationParticipant>();
        private readonly List<IRuntimeRoomStateParticipant> runtimePersistentParticipants = new List<IRuntimeRoomStateParticipant>();
        private readonly List<IResidualSimulationParticipant> residualParticipants = new List<IResidualSimulationParticipant>();
        private readonly List<Rigidbody2D> dynamicBodies = new List<Rigidbody2D>();
        private readonly Dictionary<Rigidbody2D, Vector2> originalDynamicPositions = new Dictionary<Rigidbody2D, Vector2>();
        private readonly Collider2D[] recoveryOverlapBuffer = new Collider2D[32];
        private bool participantsDiscovered;
        private Rect worldBounds;
        private float residualElapsedSeconds;

        public event Action<RoomRuntime, RoomSimulationState, RoomSimulationState> SimulationStateChanged;

        public string RoomId => roomId;
        public RoomDimension Dimension => dimension;
        public Vector2Int SizeCells => sizeCells;
        public RoomCameraMode CameraMode => cameraMode;
        public RoomSimulationState SimulationState => simulationState;
        public RoomPersistentState PersistentState => persistentState;
        public Rect WorldBounds => worldBounds;
        public Transform CameraAnchor => cameraAnchor;
        public Transform CameraAnchorsRoot => cameraAnchor != null && cameraAnchor.parent != null
            ? cameraAnchor.parent
            : cameraAnchor;
        public Transform SpawnPoint => spawnPoint;
        public Transform SafeCellRoot => safeCellRoot;
        public Transform GridLogic => gridLogic;
        public Transform GridVisual => gridVisual;
        public Transform PortalRoot => portalRoot;
        public Transform DynamicRoot => dynamicRoot;
        public Collider2D VoidRecoveryZone => voidRecoveryZone;
        public Collider2D HardFailSafePlane => hardFailSafePlane;
        public bool GeometryApproved => geometryApproved;
        public bool IsInitialized { get; private set; }
        public float ResidualElapsedSeconds => residualElapsedSeconds;

        private void Awake()
        {
            RehydrateSerializedConfiguration();
        }

        private void Update()
        {
            if (simulationState == RoomSimulationState.ResidualSimulation)
            {
                TickResidualSimulation(Time.deltaTime);
            }
        }

        public void RehydrateSerializedConfiguration()
        {
            worldBounds = new Rect(transform.position.x, transform.position.y, sizeCells.x, sizeCells.y);
            EnsureRecoveryMask();
            ConfigureRecoveryContracts();
            participantsDiscovered = false;
            IsInitialized = !string.IsNullOrWhiteSpace(roomId) &&
                            sizeCells.x > 0 && sizeCells.y > 0 &&
                            gridLogic != null && portalRoot != null && dynamicRoot != null &&
                            safeCellRoot != null && voidRecoveryRoot != null &&
                            spawnPoint != null && cameraBoundsCollider != null &&
                            voidRecoveryZone != null && hardFailSafePlane != null;
        }

        public void Configure(
            string id,
            Vector2Int roomSizeCells,
            RoomCameraMode roomCameraMode,
            Transform logicRoot,
            Transform visualRoot,
            Transform portals,
            Transform dynamics,
            Transform safeCells,
            Transform recoveryRoot,
            Transform roomCameraAnchor,
            Transform entrySpawnPoint,
            Collider2D roomCameraBounds,
            Collider2D recoveryZone,
            Collider2D failSafePlane)
        {
            roomId = id;
            sizeCells = roomSizeCells;
            cameraMode = roomCameraMode;
            gridLogic = logicRoot;
            gridVisual = visualRoot;
            portalRoot = portals;
            dynamicRoot = dynamics;
            safeCellRoot = safeCells;
            voidRecoveryRoot = recoveryRoot;
            cameraAnchor = roomCameraAnchor;
            spawnPoint = entrySpawnPoint;
            cameraBoundsCollider = roomCameraBounds;
            voidRecoveryZone = recoveryZone;
            hardFailSafePlane = failSafePlane;
            worldBounds = new Rect(transform.position.x, transform.position.y, sizeCells.x, sizeCells.y);
            EnsureRecoveryMask();
            ConfigureRecoveryContracts();
            participantsDiscovered = false;
            IsInitialized = true;
        }

        public void SetGeometryApproval(bool approved)
        {
            geometryApproved = approved;
        }

        public void SetDimension(RoomDimension roomDimension)
        {
            dimension = roomDimension;
        }

        public void TickResidualForTests(float deltaSeconds)
        {
            TickResidualSimulation(deltaSeconds);
        }

        public void SetSimulationState(RoomSimulationState nextState)
        {
            if (!IsInitialized)
            {
                return;
            }

            DiscoverParticipants(true);
            RoomSimulationState previousState = simulationState;
            if (previousState == nextState)
            {
                ApplySimulationState(nextState);
                return;
            }

            if (previousState == RoomSimulationState.Active
                && nextState != RoomSimulationState.Active
                && nextState != RoomSimulationState.ResidualSimulation)
            {
                CapturePersistentState();
            }

            if (previousState == RoomSimulationState.ResidualSimulation
                && nextState != RoomSimulationState.ResidualSimulation)
            {
                FreezeResidualParticipants(false);
                CapturePersistentState();
            }

            bool activating = nextState == RoomSimulationState.Active;
            if (activating && dynamicRoot != null)
            {
                dynamicRoot.gameObject.SetActive(true);
            }
            if (activating && gridLogic != null)
            {
                gridLogic.gameObject.SetActive(true);
            }

            if (activating)
            {
                RestorePersistentState();
            }

            simulationState = nextState;
            ApplySimulationState(nextState);
            if (nextState == RoomSimulationState.ResidualSimulation)
            {
                BeginResidualSimulation();
            }
            SimulationStateChanged?.Invoke(this, previousState, nextState);
        }

        public void CapturePersistentState()
        {
            DiscoverParticipants(true);
            for (int index = 0; index < persistentParticipants.Count; index++)
            {
                IRoomPersistentParticipant participant = persistentParticipants[index];
                if (participant == null || string.IsNullOrWhiteSpace(participant.PersistenceId))
                {
                    continue;
                }

                persistentState.StoreObject(participant.PersistenceId, participant.CaptureRoomState());
            }

            for (int index = 0; index < mapPersistentParticipants.Count; index++)
            {
                IMapElementPersistentParticipant participant = mapPersistentParticipants[index];
                if (participant == null || string.IsNullOrWhiteSpace(participant.PersistenceId))
                {
                    continue;
                }

                persistentState.StoreObject(participant.PersistenceId, participant.CaptureMapElementState());
            }

            for (int index = 0; index < runtimePersistentParticipants.Count; index++)
            {
                IRuntimeRoomStateParticipant participant = runtimePersistentParticipants[index];
                if (participant == null
                    || string.IsNullOrWhiteSpace(participant.RuntimeRoomStateId)
                    || !IsOwnedByThisRoom(participant))
                {
                    continue;
                }
                persistentState.StoreObject(
                    "runtime:" + participant.RuntimeRoomStateId,
                    participant.CaptureRuntimeRoomState());
            }

            persistentState.CommitRevision();
        }

        public void RestorePersistentState()
        {
            DiscoverParticipants(true);
            RefreshTilemapColliders();

            for (int index = 0; index < mapPersistentParticipants.Count; index++)
            {
                IMapElementPersistentParticipant participant = mapPersistentParticipants[index];
                if (participant != null && persistentState.TryGetObject(participant.PersistenceId, out string payload))
                {
                    participant.RestoreMapElementState(payload);
                }
            }

            RefreshTilemapColliders();
            for (int index = 0; index < persistentParticipants.Count; index++)
            {
                IRoomPersistentParticipant participant = persistentParticipants[index];
                if (participant != null && persistentState.TryGetObject(participant.PersistenceId, out string payload))
                {
                    participant.RestoreRoomState(payload);
                }
            }

            for (int index = 0; index < runtimePersistentParticipants.Count; index++)
            {
                IRuntimeRoomStateParticipant participant = runtimePersistentParticipants[index];
                if (participant != null
                    && persistentState.TryGetObject("runtime:" + participant.RuntimeRoomStateId, out string payload))
                {
                    participant.RestoreRuntimeRoomState(payload);
                }
            }

            Physics2D.SyncTransforms();
            StabilizeRestoredDynamicObjects();
            Physics2D.SyncTransforms();
        }

        public Vector2 GetPrimarySafePosition()
        {
            if (safeCellRoot != null && safeCellRoot.childCount > 0)
            {
                return safeCellRoot.GetChild(0).position;
            }

            if (spawnPoint != null)
            {
                return spawnPoint.position;
            }

            return worldBounds.center;
        }

        public bool Contains(Vector2 worldPosition, float tolerance = 0f)
        {
            Rect expanded = new Rect(
                worldBounds.xMin - tolerance,
                worldBounds.yMin - tolerance,
                worldBounds.width + tolerance * 2f,
                worldBounds.height + tolerance * 2f);
            return expanded.Contains(worldPosition);
        }

        public bool TryGetCameraAnchorBounds(out Rect bounds)
        {
            Transform root = CameraAnchorsRoot;
            if (root == null)
            {
                bounds = default;
                return false;
            }

            bool hasAnchor = false;
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            if (root == cameraAnchor)
            {
                IncludeAnchor(cameraAnchor.position, ref hasAnchor, ref minX, ref minY, ref maxX, ref maxY);
            }
            else
            {
                for (int index = 0; index < root.childCount; index++)
                {
                    IncludeAnchor(root.GetChild(index).position,
                        ref hasAnchor, ref minX, ref minY, ref maxX, ref maxY);
                }
            }

            bounds = hasAnchor
                ? Rect.MinMaxRect(minX, minY, maxX, maxY)
                : default;
            return hasAnchor;
        }

        private static void IncludeAnchor(
            Vector2 position,
            ref bool hasAnchor,
            ref float minX,
            ref float minY,
            ref float maxX,
            ref float maxY)
        {
            hasAnchor = true;
            minX = Mathf.Min(minX, position.x);
            minY = Mathf.Min(minY, position.y);
            maxX = Mathf.Max(maxX, position.x);
            maxY = Mathf.Max(maxY, position.y);
        }

        private void DiscoverParticipants(bool force = false)
        {
            if (participantsDiscovered && !force)
            {
                return;
            }

            persistentParticipants.Clear();
            simulationParticipants.Clear();
            mapPersistentParticipants.Clear();
            mapSimulationParticipants.Clear();
            runtimePersistentParticipants.Clear();
            residualParticipants.Clear();
            dynamicBodies.Clear();

            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IRoomPersistentParticipant persistent)
                {
                    persistentParticipants.Add(persistent);
                }

                if (behaviours[index] is IRoomSimulationParticipant simulation)
                {
                    simulationParticipants.Add(simulation);
                }

                if (behaviours[index] is IMapElementPersistentParticipant mapPersistent)
                {
                    mapPersistentParticipants.Add(mapPersistent);
                }

                if (behaviours[index] is IMapElementSimulationParticipant mapSimulation)
                {
                    mapSimulationParticipants.Add(mapSimulation);
                }


                if (behaviours[index] is IRuntimeRoomStateParticipant runtimePersistent)
                {
                    runtimePersistentParticipants.Add(runtimePersistent);
                }

                if (behaviours[index] is IResidualSimulationParticipant residual)
                {
                    residualParticipants.Add(residual);
                }
            }

            if (dynamicRoot != null)
            {
                dynamicBodies.AddRange(dynamicRoot.GetComponentsInChildren<Rigidbody2D>(true));
                for (int index = 0; index < dynamicBodies.Count; index++)
                {
                    Rigidbody2D body = dynamicBodies[index];
                    if (body != null && !originalDynamicPositions.ContainsKey(body))
                    {
                        originalDynamicPositions.Add(body, body.position);
                    }
                }
                ConfigureCriticalCarryAnchors();
            }

            participantsDiscovered = true;
        }

        private void ApplySimulationState(RoomSimulationState state)
        {
            bool logicalVisible = state == RoomSimulationState.Active ||
                               state == RoomSimulationState.TransitionTarget ||
                               state == RoomSimulationState.ResidualSimulation;
            bool visualVisible = state == RoomSimulationState.Active ||
                                 state == RoomSimulationState.TransitionTarget;
            bool active = state == RoomSimulationState.Active;
            bool residual = state == RoomSimulationState.ResidualSimulation;

            if (gridLogic != null)
            {
                // Logical collision remains available for the active/preview transition pair,
                // but never depends on the selected visual profile.
                gridLogic.gameObject.SetActive(logicalVisible);
            }

            if (gridVisual != null)
            {
                gridVisual.gameObject.SetActive(visualVisible);
            }

            if (portalRoot != null)
            {
                portalRoot.gameObject.SetActive(active);
            }

            for (int index = 0; index < dynamicBodies.Count; index++)
            {
                if (dynamicBodies[index] != null)
                {
                    dynamicBodies[index].simulated = active;
                }
            }

            for (int index = 0; index < simulationParticipants.Count; index++)
            {
                simulationParticipants[index]?.SetRoomSimulationState(state);
            }

            MapRoomState mapRoomState = ConvertMapRoomState(state);
            for (int index = 0; index < mapSimulationParticipants.Count; index++)
            {
                mapSimulationParticipants[index]?.SetMapRoomState(mapRoomState);
            }

            if (dynamicRoot != null)
            {
                dynamicRoot.gameObject.SetActive(active || residual);
            }

            if (voidRecoveryRoot != null)
            {
                voidRecoveryRoot.gameObject.SetActive(logicalVisible);
            }
        }

        private static MapRoomState ConvertMapRoomState(RoomSimulationState state)
        {
            switch (state)
            {
                case RoomSimulationState.NeighborPreview:
                    return MapRoomState.NeighborPreview;
                case RoomSimulationState.TransitionTarget:
                    return MapRoomState.TransitionTarget;
                case RoomSimulationState.Active:
                    return MapRoomState.Active;
                case RoomSimulationState.ResidualSimulation:
                    return MapRoomState.Frozen;
                case RoomSimulationState.Frozen:
                    return MapRoomState.Frozen;
                default:
                    return MapRoomState.Dormant;
            }
        }

        private void BeginResidualSimulation()
        {
            residualElapsedSeconds = 0f;
            for (int index = 0; index < residualParticipants.Count; index++)
            {
                residualParticipants[index]?.BeginResidualSimulation();
            }
            if (!HasResidualWork())
            {
                FinishResidualSimulation(false);
            }
        }

        private void TickResidualSimulation(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }
            residualElapsedSeconds += deltaSeconds;
            for (int index = 0; index < residualParticipants.Count; index++)
            {
                residualParticipants[index]?.TickResidualSimulation(deltaSeconds);
            }

            if (!HasResidualWork())
            {
                FinishResidualSimulation(false);
            }
            else if (residualElapsedSeconds >= MaximumResidualSimulationSeconds)
            {
                FinishResidualSimulation(true);
            }
        }

        private bool HasResidualWork()
        {
            for (int index = 0; index < residualParticipants.Count; index++)
            {
                if (residualParticipants[index] != null && residualParticipants[index].HasResidualWork)
                {
                    return true;
                }
            }
            return false;
        }

        private void FreezeResidualParticipants(bool timedOut)
        {
            for (int index = 0; index < residualParticipants.Count; index++)
            {
                residualParticipants[index]?.FreezeResidualSimulation(timedOut);
            }
        }

        private void FinishResidualSimulation(bool timedOut)
        {
            if (simulationState != RoomSimulationState.ResidualSimulation)
            {
                return;
            }
            FreezeResidualParticipants(timedOut);
            CapturePersistentState();
            RoomSimulationState previous = simulationState;
            simulationState = RoomSimulationState.Frozen;
            ApplySimulationState(simulationState);
            SimulationStateChanged?.Invoke(this, previous, simulationState);
        }

        private void RefreshTilemapColliders()
        {
            TilemapCollider2D[] colliders = GetComponentsInChildren<TilemapCollider2D>(true);
            for (int index = 0; index < colliders.Length; index++)
            {
                if (colliders[index] != null && colliders[index].hasTilemapChanges)
                {
                    colliders[index].ProcessTilemapChanges();
                }
            }
            Physics2D.SyncTransforms();
        }

        private void StabilizeRestoredDynamicObjects()
        {
            ObjectRecoveryCell[] recoveryCells = safeCellRoot != null
                ? safeCellRoot.GetComponentsInChildren<ObjectRecoveryCell>(true)
                : Array.Empty<ObjectRecoveryCell>();
            for (int index = 0; index < dynamicBodies.Count; index++)
            {
                Rigidbody2D body = dynamicBodies[index];
                if (body == null || !body.gameObject.activeSelf || !OverlapsRecoveryBlocker(body))
                {
                    continue;
                }

                if (TryFindRecoveryPosition(body, recoveryCells, out Vector2 recoveryPosition))
                {
                    MoveRecoveredBody(body, recoveryPosition);
                    continue;
                }

                Vector2 fallback = originalDynamicPositions.TryGetValue(body, out Vector2 original)
                    ? original
                    : spawnPoint != null ? (Vector2)spawnPoint.position : worldBounds.center;
                MoveRecoveredBody(body, fallback);
                Debug.LogError(
                    $"Room '{roomId}' could not find an ObjectRecoveryCell for '{body.name}'. Returned to spawn anchor.",
                    body);
            }
        }

        private bool TryFindRecoveryPosition(
            Rigidbody2D body,
            ObjectRecoveryCell[] cells,
            out Vector2 result)
        {
            result = default;
            if (cells == null || cells.Length == 0)
            {
                return false;
            }

            Collider2D bodyCollider = body.GetComponentInChildren<Collider2D>(true);
            Vector2 size = bodyCollider != null
                ? bodyCollider.bounds.size * 0.96f
                : Vector2.one * 0.9f;
            float bestDistance = float.PositiveInfinity;
            bool found = false;
            ContactFilter2D filter = CreateRecoveryFilter();
            for (int index = 0; index < cells.Length; index++)
            {
                ObjectRecoveryCell cell = cells[index];
                if (cell == null)
                {
                    continue;
                }
                int overlapCount = Physics2D.OverlapBox(cell.Position, size, 0f, filter, recoveryOverlapBuffer);
                if (overlapCount > 0)
                {
                    continue;
                }
                float distance = ((Vector2)body.position - cell.Position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    result = cell.Position;
                    found = true;
                }
            }
            return found;
        }

        private bool OverlapsRecoveryBlocker(Rigidbody2D body)
        {
            Collider2D[] colliders = body.GetComponentsInChildren<Collider2D>(true);
            ContactFilter2D filter = CreateRecoveryFilter();
            for (int index = 0; index < colliders.Length; index++)
            {
                Collider2D collider = colliders[index];
                if (collider != null
                    && collider.enabled
                    && Physics2D.OverlapCollider(collider, filter, recoveryOverlapBuffer) > 0)
                {
                    return true;
                }
            }
            return false;
        }

        private ContactFilter2D CreateRecoveryFilter()
        {
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(recoveryBlockMask);
            return filter;
        }

        private static void MoveRecoveredBody(Rigidbody2D body, Vector2 position)
        {
            body.position = position;
            body.transform.position = position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        private void EnsureRecoveryMask()
        {
            if (recoveryBlockMask.value == 0)
            {
                recoveryBlockMask = LayerMask.GetMask(
                    "TerrainSolid",
                    "UnbreakableBoundary",
                    "PortalBoundary");
            }
        }

        private void ConfigureRecoveryContracts()
        {
            if (voidRecoveryZone != null
                && voidRecoveryZone.GetComponent<CarryVoidRecoveryRelay>() == null)
            {
                voidRecoveryZone.gameObject.AddComponent<CarryVoidRecoveryRelay>();
            }

            ConfigurePlayerRecoveryRelay(
                voidRecoveryZone,
                PlayerRecoveryCause.VoidRecoveryZone);
            ConfigurePlayerRecoveryRelay(
                hardFailSafePlane,
                PlayerRecoveryCause.HardFailSafePlane);

            if (safeCellRoot == null)
            {
                return;
            }

            for (int index = 0; index < safeCellRoot.childCount; index++)
            {
                Transform anchor = safeCellRoot.GetChild(index);
                PlayerSafeCell2D safeCell = anchor.GetComponent<PlayerSafeCell2D>();
                if (safeCell == null)
                {
                    safeCell = anchor.gameObject.AddComponent<PlayerSafeCell2D>();
                }
                safeCell.Configure(anchor.position);
            }
        }

        private static void ConfigurePlayerRecoveryRelay(
            Collider2D zone,
            PlayerRecoveryCause cause)
        {
            if (zone == null)
            {
                return;
            }

            PlayerRecoveryZoneRelay relay = zone.GetComponent<PlayerRecoveryZoneRelay>();
            if (relay == null)
            {
                relay = zone.gameObject.AddComponent<PlayerRecoveryZoneRelay>();
            }
            relay.Configure(cause);
        }

        private void ConfigureCriticalCarryAnchors()
        {
            CriticalObjectAnchor[] anchors = GetComponentsInChildren<CriticalObjectAnchor>(true);
            CarryObjectOutOfBoundsGuard[] guards = dynamicRoot.GetComponentsInChildren<CarryObjectOutOfBoundsGuard>(true);
            for (int guardIndex = 0; guardIndex < guards.Length; guardIndex++)
            {
                CarryObjectOutOfBoundsGuard guard = guards[guardIndex];
                if (guard == null || anchors.Length == 0)
                {
                    continue;
                }
                CriticalObjectAnchor nearest = anchors[0];
                float nearestDistance = ((Vector2)guard.transform.position - nearest.Position).sqrMagnitude;
                for (int anchorIndex = 1; anchorIndex < anchors.Length; anchorIndex++)
                {
                    CriticalObjectAnchor candidate = anchors[anchorIndex];
                    float distance = ((Vector2)guard.transform.position - candidate.Position).sqrMagnitude;
                    if (distance < nearestDistance)
                    {
                        nearest = candidate;
                        nearestDistance = distance;
                    }
                }
                guard.SetLastCriticalObjectAnchor(nearest.transform);
            }
        }

        private bool IsOwnedByThisRoom(object participant)
        {
            return participant is not MonoBehaviour behaviour
                || behaviour != null && behaviour.transform.IsChildOf(transform);
        }
    }
}

#endif
