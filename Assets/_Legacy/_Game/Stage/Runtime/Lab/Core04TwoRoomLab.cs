#if LEGACY_DISABLED
using System;
using System.Collections;
using System.Collections.Generic;
using StarNight.Core.State;
using StarNight.Interaction.Carry;
using StarNight.Interaction.Input;
using StarNight.Player.Motor;
using StarNight.Player.Presentation;
using StarNight.Player.Safety;
using StarNight.Stage.Data;
using StarNight.Stage.Rooms;
using StarNight.Stage.Secrets;
using StarNight.Stage.Streaming;
using StarNight.Stage.Maru;
using StarNight.Stage.Transitions;
using StarNight.Stage.Validation;
using StarNight.Stage.Visuals;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace StarNight.Stage.Lab
{
    [DisallowMultipleComponent]
    public sealed class Core04TwoRoomLab : MonoBehaviour
    {
        public const int RoomWidth = 24;
        public const int RoomHeight = 8;
        public const float FloorTopLocalY = 1f;

        private static readonly Color RoomAColor = new Color(0.035f, 0.22f, 0.27f, 1f);
        private static readonly Color RoomBColor = new Color(0.18f, 0.10f, 0.31f, 1f);
        private static readonly Color RoomCColor = new Color(0.07f, 0.15f, 0.31f, 1f);
        private static readonly Color RoomDColor = new Color(0.25f, 0.08f, 0.20f, 1f);
        private static readonly Color TerrainColor = new Color(0.10f, 0.48f, 0.48f, 1f);
        private static readonly Color BoundaryColor = new Color(0.06f, 0.25f, 0.31f, 1f);
        private static readonly Color PortalColor = new Color(0.93f, 0.68f, 0.26f, 0.32f);

        [SerializeField] private bool buildOnAwake = true;
        [SerializeField, Range(2, 4)] private int prototypeRoomCount = 2;
        [SerializeField, Range(0, 3)] private int exitRoomIndex = 1;

        private bool explicitlyInitialized;
        private readonly List<RoomRuntime> rooms = new();
        private readonly List<RoomPortal2D> portals = new();

        public RoomRuntime RoomA { get; private set; }
        public RoomRuntime RoomB { get; private set; }
        public RoomRuntime RoomC => rooms.Count > 2 ? rooms[2] : null;
        public RoomRuntime RoomD => rooms.Count > 3 ? rooms[3] : null;
        public RoomPortal2D PortalAtoB { get; private set; }
        public RoomPortal2D PortalBtoA { get; private set; }
        public RoomTransitionController TransitionController { get; private set; }
        public RoomCameraController CameraController { get; private set; }
        public RoomStreamingManager StreamingManager { get; private set; }
        public SecretDimensionController SecretDimensionController { get; private set; }
        public SecretAnchor PrototypeSecretAnchor { get; private set; }
        public Transform RuntimeRoot { get; private set; }
        public RegionArtProfile ActiveArtProfile { get; private set; }
        public IReadOnlyList<RoomRuntime> Rooms => rooms;
        public IReadOnlyList<RoomPortal2D> Portals => portals;
        public int PrototypeRoomCount => Mathf.Clamp(prototypeRoomCount, 2, 4);
        public RoomRuntime ExitRoom => rooms.Count == 0 ? null : rooms[Mathf.Clamp(exitRoomIndex, 0, rooms.Count - 1)];

        private void Awake()
        {
            if (FeatureFlag.NewStageArchitecture)
            {
                enabled = false;
                return;
            }

            if (buildOnAwake)
            {
                BuildIfNeeded();
            }
        }

        private IEnumerator Start()
        {
            if (FeatureFlag.NewStageArchitecture)
            {
                yield break;
            }

            if (!buildOnAwake)
            {
                yield break;
            }

            yield return null;
            if (explicitlyInitialized)
            {
                yield break;
            }

            PlayerMotor2D player = UnityEngine.Object.FindFirstObjectByType<PlayerMotor2D>();
            Camera camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (player != null && camera != null)
            {
                InitializePlayerAndCamera(player, camera);
            }
        }

        public void BuildIfNeeded()
        {
            if (FeatureFlag.NewStageArchitecture)
            {
                return;
            }

            Transform existing = transform.Find("Core04TwoRoomRuntime");
            if (existing != null)
            {
                RuntimeRoot = existing;
                CacheRuntimeReferences(existing);
                return;
            }

            GameObject runtimeRoot = new GameObject("Core04TwoRoomRuntime");
            runtimeRoot.transform.SetParent(transform, false);
            RuntimeRoot = runtimeRoot.transform;

            rooms.Clear();
            portals.Clear();
            int count = PrototypeRoomCount;
            var leftPortals = new RoomPortal2D[count];
            var rightPortals = new RoomPortal2D[count];
            Color[] colors = { RoomAColor, RoomBColor, RoomCColor, RoomDColor };
            for (int index = 0; index < count; index++)
            {
                bool opensLeft = index > 0;
                bool opensRight = index < count - 1;
                string roomId = "Room_" + (char)('A' + index);
                Vector2 origin = new Vector2((index - 1) * RoomWidth, -RoomHeight * 0.5f);
                RoomRuntime room = BuildPrototypeRoom(
                    RuntimeRoot,
                    roomId,
                    origin,
                    colors[index],
                    opensLeft,
                    opensRight,
                    out RoomPortal2D primaryPortal);
                rooms.Add(room);
                if (opensLeft)
                {
                    leftPortals[index] = primaryPortal;
                }
                else if (opensRight)
                {
                    rightPortals[index] = primaryPortal;
                }

                if (opensLeft && opensRight)
                {
                    rightPortals[index] = CreatePortal(room, room.PortalRoot, CardinalDirection.Right);
                }
            }

            bool approved = true;
            for (int index = 0; index < rooms.Count; index++)
            {
                approved &= RoomGeometryValidator.ValidateAndApply(rooms[index]).IsApproved;
                rooms[index].SetSimulationState(index == 0
                    ? RoomSimulationState.Active
                    : index == 1 ? RoomSimulationState.NeighborPreview : RoomSimulationState.Dormant);
            }

            for (int index = 0; index < count - 1; index++)
            {
                RoomPortal2D forward = rightPortals[index];
                RoomPortal2D backward = leftPortals[index + 1];
                forward.Link(backward);
                backward.Link(forward);
                portals.Add(forward);
                portals.Add(backward);
                approved &= RoomGeometryValidator.ValidateConnection(forward, backward).IsApproved;
            }

            RoomA = rooms[0];
            RoomB = rooms[1];
            PortalAtoB = rightPortals[0];
            PortalBtoA = leftPortals[1];
            if (!approved)
            {
                Debug.LogError("CORE room lab failed geometry validation.", this);
            }
        }

        public void ConfigurePrototypeLayout(int roomCount, int designatedExitRoomIndex)
        {
            prototypeRoomCount = Mathf.Clamp(roomCount, 2, 4);
            exitRoomIndex = Mathf.Clamp(designatedExitRoomIndex, 0, prototypeRoomCount - 1);
        }

        public RoomPortal2D GetPortal(string ownerRoomId, string destinationRoomId)
        {
            for (int index = 0; index < portals.Count; index++)
            {
                RoomPortal2D candidate = portals[index];
                if (candidate != null && candidate.Owner != null && candidate.Destination != null &&
                    string.Equals(candidate.Owner.RoomId, ownerRoomId, StringComparison.Ordinal) &&
                    string.Equals(candidate.Destination.RoomId, destinationRoomId, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }
            return null;
        }

        public void ApplyArtProfile(RegionArtProfile profile)
        {
            BuildIfNeeded();
            bool alreadyApplied = ActiveArtProfile == profile;
            for (int index = 0; alreadyApplied && index < rooms.Count; index++)
            {
                alreadyApplied = rooms[index].GetComponent<RoomVisualBuilder>()?.Profile == profile;
            }
            if (alreadyApplied)
            {
                return;
            }

            ActiveArtProfile = profile;
            for (int index = 0; index < rooms.Count; index++)
            {
                rooms[index].GetComponent<RoomVisualBuilder>()?.ApplyProfile(profile);
            }
        }

        public void InitializePlayerAndCamera(PlayerMotor2D player, Camera camera)
        {
            explicitlyInitialized = true;
            BuildIfNeeded();
            if (player == null || camera == null)
            {
                return;
            }

            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.backgroundColor = new Color(0.018f, 0.028f, 0.075f, 1f);

            CameraController = GetComponent<RoomCameraController>();
            if (CameraController == null)
            {
                CameraController = gameObject.AddComponent<RoomCameraController>();
            }
            CameraController.Configure(camera);

            TransitionController = GetComponent<RoomTransitionController>();
            if (TransitionController == null)
            {
                TransitionController = gameObject.AddComponent<RoomTransitionController>();
            }

            StreamingManager = GetComponent<RoomStreamingManager>();
            if (StreamingManager == null)
            {
                StreamingManager = gameObject.AddComponent<RoomStreamingManager>();
            }
            StreamingManager.ConfigureExistingRooms(rooms);

            PlayerActionLock actionLock = player.GetComponent<PlayerActionLock>();
            PlayerOutOfBoundsGuard guard = player.GetComponent<PlayerOutOfBoundsGuard>();
            TransitionController.Configure(CameraController, player, actionLock, guard, null, StreamingManager);
            SecretDimensionController = GetComponent<SecretDimensionController>();
            if (SecretDimensionController == null)
            {
                SecretDimensionController = gameObject.AddComponent<SecretDimensionController>();
            }
            SecretDimensionController.Configure(this, TransitionController, StreamingManager);
            EnsureSecretPrototype(player);
            for (int index = 0; index < portals.Count; index++)
            {
                portals[index]?.Bind(TransitionController);
            }
            TransitionController.Begin(RoomA);
        }

        private void EnsureSecretPrototype(PlayerMotor2D player)
        {
            if (RoomA == null || RoomA.DynamicRoot == null)
            {
                return;
            }

            Transform anchorTransform = RoomA.DynamicRoot.Find("SecretAnchorPrototype");
            if (anchorTransform == null)
            {
                var anchorObject = new GameObject("SecretAnchorPrototype");
                anchorObject.layer = LayerMask.NameToLayer("Interaction");
                anchorObject.transform.SetParent(RoomA.DynamicRoot, false);
                anchorObject.transform.localPosition = new Vector3(8f, 2f, 0f);
                BoxCollider2D collider = anchorObject.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one * 0.9f;
                collider.isTrigger = true;
                anchorTransform = anchorObject.transform;
            }

            Transform safeCell = RoomA.SafeCellRoot.Find("SecretReturnSafeCell");
            if (safeCell == null)
            {
                var safeObject = new GameObject("SecretReturnSafeCell");
                safeObject.transform.SetParent(RoomA.SafeCellRoot, false);
                safeObject.transform.localPosition = new Vector3(
                    7f,
                    FloorTopLocalY + PlayerMotor2D.ColliderHeight * 0.5f + 0.005f,
                    0f);
                safeCell = safeObject.transform;
            }

            PrototypeSecretAnchor = anchorTransform.GetComponent<SecretAnchor>();
            if (PrototypeSecretAnchor == null)
            {
                PrototypeSecretAnchor = anchorTransform.gameObject.AddComponent<SecretAnchor>();
            }
            PrototypeSecretAnchor.Configure(
                "ROOM_A_SECRET_01",
                0x51A7,
                "Secret_Room_A_01",
                RoomA,
                safeCell,
                SecretDimensionController);

            if (player != null)
            {
                SecretDetectorController detector = player.GetComponent<SecretDetectorController>();
                if (detector == null)
                {
                    detector = player.gameObject.AddComponent<SecretDetectorController>();
                }
            }
        }

        private void CacheRuntimeReferences(Transform existing)
        {
            rooms.Clear();
            portals.Clear();
            RoomRuntime[] foundRooms = existing.GetComponentsInChildren<RoomRuntime>(true);
            Array.Sort(foundRooms, (left, right) => string.CompareOrdinal(left.RoomId, right.RoomId));
            rooms.AddRange(foundRooms);
            RoomPortal2D[] foundPortals = existing.GetComponentsInChildren<RoomPortal2D>(true);
            portals.AddRange(foundPortals);
            RoomA = rooms.Count > 0 ? rooms[0] : null;
            RoomB = rooms.Count > 1 ? rooms[1] : null;
            PortalAtoB = GetPortal("Room_A", "Room_B");
            PortalBtoA = GetPortal("Room_B", "Room_A");
        }

        public static RoomRuntime BuildPrototypeRoom(
            Transform parent,
            string roomId,
            Vector2 origin,
            Color backgroundColor,
            bool portalOnLeft,
            bool portalOnRight,
            out RoomPortal2D portal)
        {
            GameObject roomObject = new GameObject(roomId);
            roomObject.transform.SetParent(parent, false);
            roomObject.transform.position = new Vector3(origin.x, origin.y, 0f);
            RoomRuntime room = roomObject.AddComponent<RoomRuntime>();

            Transform metadata = CreateNode(roomObject.transform, "Metadata");
            Transform gridLogic = CreateNode(roomObject.transform, "GridLogic");
            Grid grid = gridLogic.gameObject.AddComponent<Grid>();
            grid.cellSize = Vector3.one;
            gridLogic.gameObject.AddComponent<RoomGridTransform>();

            Transform terrainTilemap = CreateTilemap(gridLogic, "TerrainCollisionTilemap", true);
            Transform oneWayTilemap = CreateTilemap(gridLogic, "OneWayCollisionTilemap", false);
            Transform boundaryTilemap = CreateTilemap(gridLogic, "UnbreakableBoundaryTilemap", false);
            CreateTilemap(gridLogic, "HazardLogicTilemap", false);
            CreateTilemap(gridLogic, "InteractionLogicTilemap", false);

            Transform gridVisual = CreateNode(roomObject.transform, "GridVisual");
            Transform backgroundRoot = CreateNode(roomObject.transform, "BackgroundRoot");
            Transform propRoot = CreateNode(roomObject.transform, "PropRoot");
            Transform actorRoot = CreateNode(roomObject.transform, "ActorRoot");
            Transform vfxRoot = CreateNode(roomObject.transform, "VFXRoot");
            Transform foregroundRoot = CreateNode(roomObject.transform, "ForegroundRoot");

            CreateCollisionBox(terrainTilemap, "TerrainFloor", new Vector2(RoomWidth * 0.5f, 0.5f), new Vector2(RoomWidth, 1f), true);
            CreateOneWayPlatform(oneWayTilemap, new Vector2(RoomWidth * 0.5f, 3f));
            CreateBoundary(boundaryTilemap, portalOnLeft, portalOnRight);

            Transform cameraBounds = CreateNode(roomObject.transform, "CameraBounds");
            cameraBounds.localPosition = new Vector3(RoomWidth * 0.5f, RoomHeight * 0.5f, 0f);
            BoxCollider2D cameraBoundsCollider = cameraBounds.gameObject.AddComponent<BoxCollider2D>();
            cameraBoundsCollider.size = new Vector2(RoomWidth, RoomHeight);
            cameraBoundsCollider.isTrigger = true;

            Transform cameraAnchors = CreateNode(roomObject.transform, "CameraAnchors");
            Transform cameraAnchor = CreateNode(cameraAnchors, "EntryAnchor");
            cameraAnchor.localPosition = new Vector3(portalOnRight ? 3f : RoomWidth - 3f, RoomHeight * 0.5f, 0f);

            Transform portalRoot = CreateNode(roomObject.transform, "PortalRoot");
            Transform spawnRoot = CreateNode(roomObject.transform, "SpawnRoot");
            Transform spawnPoint = CreateNode(spawnRoot, "EntryPoint");
            spawnPoint.localPosition = new Vector3(portalOnRight ? 3f : RoomWidth - 3f, FloorTopLocalY + PlayerMotor2D.ColliderHeight * 0.5f + 0.005f, 0f);

            Transform dynamicRoot = CreateNode(roomObject.transform, "DynamicRoot");
            CreatePersistentCrate(dynamicRoot, roomId + "_Crate", new Vector2(RoomWidth * 0.5f, 1.65f));
            CreateNode(roomObject.transform, "ElementSlotRoot");
            CreateNode(roomObject.transform, "SignalLinkRoot");

            Transform safeCellRoot = CreateNode(roomObject.transform, "SafeCellRoot");
            Transform safeCell = CreateNode(safeCellRoot, "ObjectRecoveryCell_0");
            safeCell.localPosition = spawnPoint.localPosition;
            safeCell.gameObject.AddComponent<ObjectRecoveryCell>();
            safeCell.gameObject.AddComponent<CriticalObjectAnchor>();

            Transform voidRecoveryRoot = CreateNode(roomObject.transform, "VoidRecoveryRoot");
            Transform voidZone = CreateNode(voidRecoveryRoot, "VoidRecoveryZone");
            voidZone.localPosition = new Vector3(RoomWidth * 0.5f, -0.5f, 0f);
            BoxCollider2D voidCollider = voidZone.gameObject.AddComponent<BoxCollider2D>();
            voidCollider.size = new Vector2(RoomWidth, 1f);
            voidCollider.isTrigger = true;

            Transform failSafe = CreateNode(voidRecoveryRoot, "HardFailSafePlane");
            failSafe.localPosition = new Vector3(RoomWidth * 0.5f, -3f, 0f);
            BoxCollider2D failSafeCollider = failSafe.gameObject.AddComponent<BoxCollider2D>();
            failSafeCollider.size = new Vector2(RoomWidth, 0.5f);
            failSafeCollider.isTrigger = true;

            Transform maruLaneRoot = CreateNode(roomObject.transform, "MaruLaneRoot");
            CreateNode(roomObject.transform, "AudioZone");
            CreateNode(roomObject.transform, "DebugRoot");

            room.Configure(
                roomId,
                new Vector2Int(RoomWidth, RoomHeight),
                RoomCameraMode.BoundedX,
                gridLogic,
                gridVisual,
                portalRoot,
                dynamicRoot,
                safeCellRoot,
                voidRecoveryRoot,
                cameraAnchor,
                spawnPoint,
                cameraBoundsCollider,
                voidCollider,
                failSafeCollider);

            MaruLane maruLane = maruLaneRoot.gameObject.AddComponent<MaruLane>();
            maruLane.Configure(room);

            CardinalDirection side = portalOnLeft ? CardinalDirection.Left : CardinalDirection.Right;
            portal = CreatePortal(room, portalRoot, side);

            RoomVisualBuilder visualBuilder = roomObject.AddComponent<RoomVisualBuilder>();
            visualBuilder.Configure(room, gridVisual, backgroundRoot, propRoot, actorRoot, vfxRoot, foregroundRoot, backgroundColor);
            visualBuilder.ApplyProfile(null);
            metadata.gameObject.name = "Metadata";
            return room;
        }

        public static RoomPortal2D CreatePortal(RoomRuntime room, Transform portalRoot, CardinalDirection side)
        {
            bool left = side == CardinalDirection.Left;
            float boundaryX = left ? 0f : RoomWidth;
            float previewX = left ? 2f : RoomWidth - 2f;
            float commitX = left ? 0.6f : RoomWidth - 0.6f;
            float entryX = left ? 1.5f : RoomWidth - 1.5f;

            Transform portalObject = CreateNode(portalRoot, left ? "Portal_Left" : "Portal_Right");
            RoomPortal2D portal = portalObject.gameObject.AddComponent<RoomPortal2D>();

            Transform boundary = CreateNode(portalObject, "PortalBoundary");
            boundary.localPosition = new Vector3(boundaryX, 2.5f, 0f);
            boundary.localScale = new Vector3(0.15f, 3f, 1f);

            Transform safeFloor = CreateNode(portalObject, "EntrySafeFloor");
            safeFloor.localPosition = new Vector3(entryX, FloorTopLocalY - 0.05f, 0f);
            safeFloor.localScale = new Vector3(2f, 0.1f, 1f);

            Transform clearZoneObject = CreateNode(portalObject, "GameplayClearZone");
            clearZoneObject.localPosition = new Vector3(entryX, 2.2f, 0f);
            GameplayClearZone clearZone = clearZoneObject.gameObject.AddComponent<GameplayClearZone>();
            clearZone.Configure(new Vector2(3f, 3.6f));

            Transform entryAnchor = CreateNode(portalObject, "EntryAnchor");
            entryAnchor.localPosition = new Vector3(entryX, FloorTopLocalY + PlayerMotor2D.ColliderHeight * 0.5f + 0.005f, 0f);

            Transform previewLine = CreatePortalLine(portalObject, "PortalPreviewLine", previewX);
            Transform commitLine = CreatePortalLine(portalObject, "CommitLine", commitX);
            portal.Configure(room.RoomId + (left ? "_L" : "_R"), side, 1, room, entryAnchor, previewLine, commitLine, boundary, safeFloor);
            return portal;
        }

        private static Transform CreatePortalLine(Transform parent, string name, float localX)
        {
            Transform line = CreateNode(parent, name);
            line.localPosition = new Vector3(localX, 2.5f, 0f);
            BoxCollider2D trigger = line.gameObject.AddComponent<BoxCollider2D>();
            trigger.size = new Vector2(0.18f, 3f);
            trigger.isTrigger = true;
            CreateSprite(line, "Marker", Vector2.zero, new Vector2(0.08f, 3f), PortalColor, 8, false);
            return line;
        }

        private static Transform CreateTilemap(Transform parent, string name, bool terrain)
        {
            Transform tilemapRoot = CreateNode(parent, name);
            tilemapRoot.gameObject.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapRoot.gameObject.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = 0;
            renderer.enabled = false;
            if (terrain)
            {
                tilemapRoot.gameObject.AddComponent<TilemapCollider2D>();
                Rigidbody2D body = tilemapRoot.gameObject.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Static;
                tilemapRoot.gameObject.AddComponent<CompositeCollider2D>();
            }
            return tilemapRoot;
        }

        private static void CreateBoundary(Transform parent, bool openingOnLeft, bool openingOnRight)
        {
            CreateCollisionBox(parent, "Boundary_Ceiling", new Vector2(RoomWidth * 0.5f, RoomHeight - 0.5f), new Vector2(RoomWidth, 1f), true);
            if (openingOnLeft)
            {
                CreateCollisionBox(parent, "Boundary_LeftUpper", new Vector2(0.5f, 6f), new Vector2(1f, 4f), true);
            }
            else
            {
                CreateCollisionBox(parent, "Boundary_Left", new Vector2(0.5f, RoomHeight * 0.5f), new Vector2(1f, RoomHeight), true);
            }

            if (openingOnRight)
            {
                CreateCollisionBox(parent, "Boundary_RightUpper", new Vector2(RoomWidth - 0.5f, 6f), new Vector2(1f, 4f), true);
            }
            else
            {
                CreateCollisionBox(parent, "Boundary_Right", new Vector2(RoomWidth - 0.5f, RoomHeight * 0.5f), new Vector2(1f, RoomHeight), true);
            }
        }

        private static void CreateOneWayPlatform(Transform parent, Vector2 localPosition)
        {
            GameObject platform = CreateCollisionBox(parent, "OneWayPlatform", localPosition, new Vector2(3f, 0.2f), false);
            BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(3f, 0.2f);
            collider.usedByEffector = true;
            PlatformEffector2D effector = platform.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
        }

        private static GameObject CreateCollisionBox(
            Transform parent,
            string name,
            Vector2 localPosition,
            Vector2 size,
            bool addCollider)
        {
            GameObject item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            item.layer = LayerMask.NameToLayer("Ground");
            if (addCollider)
            {
                BoxCollider2D collider = item.AddComponent<BoxCollider2D>();
                collider.size = size;
            }
            return item;
        }

        private static void CreatePersistentCrate(Transform parent, string persistenceId, Vector2 localPosition)
        {
            GameObject crate = CreateSprite(parent, "PersistentCrate", localPosition, new Vector2(0.8f, 0.8f), new Color(0.82f, 0.58f, 0.24f, 1f), 4, false);
            crate.AddComponent<BoxCollider2D>();
            Rigidbody2D body = crate.AddComponent<Rigidbody2D>();
            body.gravityScale = 1f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            RoomPersistentTransform2D persistent = crate.AddComponent<RoomPersistentTransform2D>();
            persistent.Configure(persistenceId);
        }

        private static Transform CreateNode(Transform parent, string name)
        {
            GameObject node = new GameObject(name);
            node.transform.SetParent(parent, false);
            return node.transform;
        }

        private static GameObject CreateSprite(
            Transform parent,
            string name,
            Vector2 localPosition,
            Vector2 size,
            Color color,
            int sortingOrder,
            bool collision)
        {
            GameObject item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            item.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypeSpriteFactory.GetWhitePixel();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            if (collision)
            {
                item.layer = LayerMask.NameToLayer("Ground");
                BoxCollider2D collider = item.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one;
            }

            return item;
        }

    }
}

#endif
