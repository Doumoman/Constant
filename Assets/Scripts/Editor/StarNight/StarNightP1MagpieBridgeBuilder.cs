#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StarFetchingNight;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarFetchingNightEditor
{
    public static class StarNightP1MagpieBridgeBuilder
    {
        private const string SceneFolder = "Assets/Scenes/StarNight";
        private const string ScenePath = SceneFolder + "/StarNight_MagpieBridge.unity";
        private const string SquarePath = "Assets/Resources/Sprites/Square.png";
        private const string FontPath = "Assets/TextMesh Pro/Fonts/NeoDunggeunmoPro-Regular.asset";

        private const string Forest = "Assets/2D Fantasy sprite bundle/Forest  V2.0/Prefabs/";
        private const string Crystal = "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/Crystal prefabs/";
        private const string Station = "Assets/2D Fantasy sprite bundle/Abandoned station/Prefabs/";
        private const string Island = "Assets/2D Fantasy sprite bundle/Island pack/Prefabs/";
        private const string Chains = "Assets/2D Fantasy sprite bundle/Bonus/Climbing elements/Chains/";

        private static Sprite square;
        private static TMP_FontAsset font;
        private static Transform world;
        private static Transform artRoot;
        private static Transform collisionRoot;
        private static Transform gameplayRoot;
        private static Transform labelRoot;

        private sealed class Room
        {
            public readonly string id;
            public readonly string label;
            public readonly float x;
            public readonly float y;
            public readonly bool optional;

            public Room(string id, string label, float x, float y, bool optional = false)
            {
                this.id = id;
                this.label = label;
                this.x = x;
                this.y = y;
                this.optional = optional;
            }
        }

        [MenuItem("Tools/Star Night/Build P1 Magpie Bridge")]
        public static void Build()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath && active.isDirty)
            {
                EditorSceneManager.SaveScene(active);
            }

            EnsureFolder(SceneFolder);
            square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (square == null || font == null)
            {
                Debug.LogError("[Star Night P1] Square sprite or Korean TMP font is missing.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "StarNight_MagpieBridge";
            world = new GameObject("WORLD · 까치다리 정거장").transform;
            artRoot = ChildRoot("ART · 2D Fantasy Station", world);
            collisionRoot = ChildRoot("COLLISION · Stable Bridge Route", world);
            gameplayRoot = ChildRoot("GAMEPLAY · Red Thread Puzzles", world);
            labelRoot = ChildRoot("ROOM TITLES", world);

            Camera camera = CreateCamera();
            CreateMainLight();
            CreateBackdrop();
            List<Room> rooms = CreateRooms();
            CreateMainRoute(rooms);
            CreateStarLadderBranch();
            CreateOldBridgeShortcut();
            CreateStationLandmarks();
            GameObject player = CreatePlayer();
            CreateSystems(camera, player.transform);
            CreatePuzzles();
            CreateHaechiChoice();
            CreateStations();
            CreateCheckpoints();
            CreateGuide();
            CreateWorldBounds();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            Selection.activeGameObject = player;
            SceneView.lastActiveSceneView?.FrameSelected();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Star Night M3-1] Magpie Bridge built: 15 main rooms, 3 gate routes, manual star gate, Haechi event, gated star ladder.");
        }

        private static Transform ChildRoot(string name, Transform parent)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent);
            return child;
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(3f, 1f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.8f;
            camera.backgroundColor = new Color(0.012f, 0.024f, 0.075f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateMainLight()
        {
            GameObject lightObject = new("Galaxy Light · Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.72f, 0.8f, 1f);
            light.intensity = 0.66f;
            lightObject.transform.rotation = Quaternion.Euler(38f, -30f, 0f);
        }

        private static void CreateBackdrop()
        {
            for (int i = 0; i < 3; i++)
            {
                InstantiateArt(Forest + "Mounts and sky.prefab", $"GalaxySky_{i}",
                    new Vector3(i * 58f, 1f, 8f), 1.45f, -55,
                    new Color(0.35f, 0.48f, 0.9f));
            }
            InstantiateArt(Crystal + "Background blur.prefab", "MilkyWayBlur",
                new Vector3(74f, -1f, 6f), 2.3f, -48, new Color(0.52f, 0.7f, 1f));
            InstantiateArt(Crystal + "Stars Particle.prefab", "BridgeStars",
                new Vector3(68f, 3f, 0f), 1.7f, -20, new Color(1f, 0.55f, 0.72f));
            InstantiateArt(Island + "Wind.prefab", "GalaxyWind",
                new Vector3(95f, 3f, 1f), 1.35f, -18, new Color(0.75f, 0.9f, 1f));
        }

        private static List<Room> CreateRooms()
        {
            return new List<Room>
            {
                new("arrival", "달배 도착 승강장", 0f, 0f),
                new("supply_lift", "달토끼 물류 승강기", 9f, 0f),
                new("first_knot", "첫 매듭 교실", 18f, 0.8f),
                new("cargo_rail", "흔들상자 선로", 27f, 0f),
                new("anchor_a", "제1 닻 · 안전한 연결", 36f, 0.8f),
                new("magpie_nest", "까치 휴게 둥지", 45f, 1.8f),
                new("tension_stairs", "장력 계단", 54f, 3f),
                new("anchor_b", "제2 닻 · 무게 공유", 63f, 2f),
                new("transfer_hall", "은하수 환승 홀", 72f, 0f),
                new("old_bridge", "옛 물류 다리", 81f, -1f),
                new("anchor_c", "제3 닻 · 팽팽한 사고", 90f, 0.8f),
                new("haechi_gate", "해치의 출항문", 99f, 1.8f),
                new("rani_record", "라니 통신 기록실", 108f, 0.8f, true),
                new("storm_edge", "은하수 폭풍 전조", 117f, 1.8f),
                new("star_train", "별기차 선착장", 126f, 0.8f)
            };
        }

        private static void CreateMainRoute(List<Room> rooms)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                Room room = rooms[i];
                float floorY = room.y - 2.65f;
                CreateCollisionPlatform($"Floor_{room.id}", new Vector2(room.x, floorY), new Vector2(7.7f, 0.55f), collisionRoot);
                string art = i % 3 == 0 ? "Platform with ropes.prefab" : i % 2 == 0 ? "Platform B.prefab" : "Platform A.prefab";
                InstantiateArt(Station + art, $"FloorArt_{room.id}", new Vector3(room.x, floorY + 0.28f, 0f),
                    0.66f, -6, room.optional ? new Color(0.78f, 0.55f, 1f) : new Color(0.72f, 0.88f, 1f));
                WorldText(room.label, new Vector3(room.x, room.y + 2.65f, 0f), 1.35f,
                    room.optional ? new Color(0.94f, 0.55f, 1f) : new Color(1f, 0.78f, 0.3f), 52, labelRoot);
                CreateDiscovery(room.id, room.label, new Vector2(room.x, room.y), room.optional);

                if (i == rooms.Count - 1)
                {
                    continue;
                }

                Room next = rooms[i + 1];
                float midpointX = (room.x + next.x) * 0.5f;
                float midpointY = Mathf.Min(floorY, next.y - 2.65f) + 0.35f;
                CreateCollisionPlatform($"Connector_{i:00}", new Vector2(midpointX, midpointY),
                    new Vector2(2.1f, 0.42f), collisionRoot);
                InstantiateArt(Station + "Platform B.prefab", $"ConnectorArt_{i:00}",
                    new Vector3(midpointX, midpointY + 0.18f, 0f), 0.33f, -5,
                    new Color(0.62f, 0.78f, 1f));
            }
        }

        private static void CreateStarLadderBranch()
        {
            Transform branch = ChildRoot("BRANCH · 까마득한 별사다리", world);
            Vector2[] points =
            {
                new(66f, 4.8f), new(69f, 7.2f), new(66.5f, 9.7f),
                new(70f, 12.1f), new(73f, 14.2f)
            };
            for (int i = 0; i < points.Length; i++)
            {
                CreateCollisionPlatform($"StarLadder_{i}", points[i], new Vector2(3.1f, 0.42f), branch);
                InstantiateArt(i % 2 == 0 ? Island + "platform wt.prefab" : Station + "Platform with ropes.prefab",
                    $"StarLadderArt_{i}", new Vector3(points[i].x, points[i].y + 0.18f, 0f),
                    i % 2 == 0 ? 0.42f : 0.34f, 5, new Color(0.88f, 0.55f, 1f), branch);
                InstantiateArt(Chains + "Crystal chain.prefab", $"LadderChain_{i}",
                    new Vector3(points[i].x, points[i].y + 3.2f, 0f), 0.42f, 4,
                    new Color(1f, 0.22f, 0.42f), branch);
            }

            GameObject knot = SpriteBlock("EndlessKnot · 끊어지지 않는 매듭",
                new Vector3(73f, 15.3f, 0f), new Vector2(0.85f, 0.85f),
                new Color(1f, 0.16f, 0.4f), 38, branch);
            CircleCollider2D trigger = knot.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.1f;
            knot.AddComponent<MagpieThreadUpgrade>();
            WorldText("유혹 · 연결 한도 +1", new Vector3(73f, 16.3f, 0f), 1.2f,
                new Color(1f, 0.45f, 0.7f), 55, branch);
            CreateDiscovery("star_ladder", "까마득한 별사다리", new Vector2(69f, 11f), true);
        }

        private static void CreateOldBridgeShortcut()
        {
            Transform branch = ChildRoot("BRANCH · 끊을 수 있는 옛 물류길", world);
            for (int i = 0; i < 5; i++)
            {
                Vector2 point = new(66f + i * 7f, -5.6f + Mathf.Sin(i) * 0.4f);
                CreateCollisionPlatform($"OldRoute_{i}", point, new Vector2(5.4f, 0.45f), branch);
                InstantiateArt(Station + "Platform with ropes.prefab", $"OldRouteArt_{i}",
                    new Vector3(point.x, point.y + 0.2f, 0f), 0.46f, -2,
                    new Color(0.46f, 0.58f, 0.8f), branch);
            }

            GameObject barrier = SpriteBlock("OldBridgeBarrier", new Vector3(82f, -3.2f, 0f),
                new Vector2(0.7f, 4.4f), new Color(0.55f, 0.12f, 0.22f), 20, branch);
            barrier.layer = 7;
            barrier.AddComponent<BoxCollider2D>();
            SpriteRenderer rope = SpriteBlock("OldBridgeRopeVisual", new Vector3(82f, -1.4f, 0f),
                new Vector2(0.15f, 7f), new Color(0.92f, 0.12f, 0.3f), 21, branch).GetComponent<SpriteRenderer>();
            GameObject switchObject = SpriteBlock("OldBridgeSwitch · 물류 실 절단기",
                new Vector3(78.8f, -4.7f, 0f), new Vector2(0.85f, 1.1f),
                new Color(1f, 0.68f, 0.18f), 30, branch);
            CircleCollider2D trigger = switchObject.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.3f;
            MagpieOldBridgeSwitch bridgeSwitch = switchObject.AddComponent<MagpieOldBridgeSwitch>();
            bridgeSwitch.Configure(barrier, rope);
            GateRouteObjective oldBridgeObjective = switchObject.AddComponent<GateRouteObjective>();
            oldBridgeObjective.Configure("CH2_ROUTE_OLD_BRIDGE");
            bridgeSwitch.ConfigureRouteObjective(oldBridgeObjective);
            WorldText("C 빠름·전용 · 옛 닻 확보 · GateReady 전 대체 닻 가능", new Vector3(80f, -7f, 0f),
                1.15f, new Color(1f, 0.55f, 0.45f), 54, branch);
            CreateDiscovery("old_route", "끊어진 옛 물류길", new Vector2(80f, -4.6f), true);
        }

        private static void CreateStationLandmarks()
        {
            InstantiateArt(Station + "Columns A.prefab", "ArrivalColumns", new Vector3(2f, -2.3f, 0f),
                0.9f, 2, new Color(0.6f, 0.82f, 1f));
            InstantiateArt(Station + "Columns B.prefab", "TransferColumns", new Vector3(73f, -2.3f, 0f),
                1.1f, 2, new Color(0.62f, 0.75f, 1f));
            InstantiateArt(Station + "Cables A.prefab", "LongRedCables", new Vector3(54f, 5f, 0f),
                1.2f, 3, new Color(1f, 0.25f, 0.42f));
            InstantiateArt(Station + "Mashinery dump.prefab", "BridgeMachinery", new Vector3(94f, -1.6f, 0f),
                0.78f, 4, new Color(0.72f, 0.82f, 1f));
            InstantiateArt(Station + "Door.prefab", "HaechiDepartureDoor", new Vector3(100f, -0.9f, 0f),
                0.72f, 10, new Color(0.85f, 0.58f, 1f));
            InstantiateArt(Island + "platform wt.prefab", "StarTrainPlatform", new Vector3(127f, -1.1f, 0f),
                0.95f, 11, new Color(0.55f, 0.88f, 1f));
            for (int i = 0; i < 7; i++)
            {
                InstantiateArt(Station + "Lamp.prefab", $"StationLamp_{i}",
                    new Vector3(12f + i * 17f, 1.4f + (i % 2) * 1.3f, 0f),
                    0.42f, 18, i % 2 == 0 ? new Color(1f, 0.48f, 0.62f) : new Color(0.48f, 0.78f, 1f));
            }
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = SpriteBlock("Player · 별을 줍는 아이", new Vector3(-2f, -1.45f, 0f),
                new Vector2(0.72f, 1.2f), new Color(1f, 0.78f, 0.3f), 40, world);
            player.layer = 31;
            GameObject scarf = SpriteBlock("RedThreadScarf", Vector3.zero, new Vector2(0.9f, 0.12f),
                new Color(0.95f, 0.12f, 0.32f), 42, player.transform);
            scarf.transform.localPosition = new Vector3(-0.5f, 0.05f, 0f);
            GameObject umbrella = SpriteBlock("Umbrella", Vector3.zero, new Vector2(1.05f, 0.16f),
                new Color(0.35f, 0.78f, 1f), 39, player.transform);
            umbrella.transform.localPosition = new Vector3(0.5f, 0.2f, 0f);
            umbrella.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 3.2f;
            body.freezeRotation = true;
            CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.82f, 1.1f);
            player.AddComponent<StarNightInventory>();
            player.AddComponent<StarNightSimpleMotor>();
            player.AddComponent<StarNightPlayerAgent>();
            player.AddComponent<StarNightJourneyNavigation>();
            return player;
        }

        private static void CreateSystems(Camera camera, Transform player)
        {
            GameObject systems = new("@STAR NIGHT P1 · 두 동사 결합");
            MagpieBridgeChapterBootstrap bootstrap = systems.AddComponent<MagpieBridgeChapterBootstrap>();
            bootstrap.ConfigureGateLoop(true);
            systems.AddComponent<StarNightCombinationResolver>();
            systems.AddComponent<ChapterPlaytestTelemetry>();

            GameObject hudObject = new("@HUD · 라니의 편향 기록");
            StarNightHUD hud = hudObject.AddComponent<StarNightHUD>();
            hud.SetFont(font);

            GameObject atmosphereObject = new("@ATMOSPHERE · 은하수 장력");
            StarNightAtmosphere atmosphere = atmosphereObject.AddComponent<StarNightAtmosphere>();
            atmosphere.Configure(camera, player, square);
            atmosphere.SetWorldBounds(new Vector2(-12f, 142f), new Vector2(-11f, 20f), 170);

            GameObject maru = SpriteBlock("Maru · 실을 따라오는 개", new Vector3(137f, 8f, 0f),
                new Vector2(1.55f, 1.2f), new Color(1f, 0.24f, 0.48f), 48, world);
            CreateEar(maru.transform, -0.42f);
            CreateEar(maru.transform, 0.42f);
            MaruDirector director = systems.AddComponent<MaruDirector>();
            director.Configure(maru.transform, new Vector3(137f, 8f, 0f));

            GameObject firstBellTrace = SpriteBlock("Bell 1 · 한 방향으로 당겨지는 실",
                new Vector3(117f, 4.8f, 0f), new Vector2(8f, 0.16f),
                new Color(1f, 0.25f, 0.45f, 0.62f), 31, world);
            firstBellTrace.SetActive(false);
            GameObject secondBellPresence = SpriteBlock("Bell 2 · 빛나는 짐을 찾는 그림자",
                new Vector3(110f, 3.9f, 0f), new Vector2(6.2f, 0.3f),
                new Color(1f, 0.18f, 0.42f, 0.7f), 32, world);
            secondBellPresence.SetActive(false);
            GameObject gateClosingVisual = SpriteBlock("Bell 3 · 흔들리며 닫히는 까치 별문",
                new Vector3(119f, 0.1f, 0f), new Vector2(0.4f, 5.6f),
                new Color(1f, 0.15f, 0.4f, 0.78f), 39, world);
            gateClosingVisual.SetActive(false);
            BellChasePresenter presenter = systems.AddComponent<BellChasePresenter>();
            presenter.Configure(director, firstBellTrace, secondBellPresence, gateClosingVisual);
        }

        private static void CreatePuzzles()
        {
            CreateAnchorPair("A", new Vector2(37f, -0.75f), new Vector2(31.5f, 0.65f),
                "떠도는 새 닻", new Color(0.35f, 0.82f, 1f), 1.1f, "CH2_ROUTE_NEW_ANCHOR");
            CreateAnchorPair("B", new Vector2(64f, 0.45f), new Vector2(58.3f, 4.3f),
                "폭풍탑 예비 닻", new Color(0.96f, 0.55f, 0.2f), 2.8f, "CH2_ROUTE_STORM_ANCHOR");
            CreateAnchorPair("C", new Vector2(91f, -0.7f), new Vector2(85.3f, 3.1f),
                "폭풍 흔들판", new Color(0.85f, 0.38f, 1f), 1.8f);

            CreateFable("counterweight", "안전추", new Vector2(20f, -0.9f), new Vector2(0.9f, 1.25f),
                new Color(0.48f, 0.6f, 0.75f), FableTraits.Linkable | FableTraits.Resizable, 3.5f, false);
            CreateFable("cargo_box", "달떡 물류상자", new Vector2(25.5f, -1f), new Vector2(1.25f, 0.95f),
                new Color(0.8f, 0.52f, 0.25f), FableTraits.Linkable | FableTraits.Resizable | FableTraits.Carryable,
                1.4f, false);
            CreateFable("pop_star_fruit", "톡톡별 열매", new Vector2(113f, 0f), new Vector2(0.78f, 0.78f),
                new Color(1f, 0.28f, 0.38f),
                FableTraits.Linkable | FableTraits.Resizable | FableTraits.Explosive | FableTraits.Carryable,
                0.75f, true);
        }

        private static void CreateAnchorPair(string id, Vector2 anchorPosition, Vector2 piecePosition,
            string pieceName, Color pieceColor, float pieceMass, string routeId = null)
        {
            FableObject socket = CreateFable($"bridge_anchor_{id.ToLowerInvariant()}", $"제{id} 닻 고정점",
                anchorPosition, new Vector2(0.8f, 1.5f), new Color(1f, 0.12f, 0.34f),
                FableTraits.Linkable | FableTraits.BridgeAnchor, 0f, false, true);
            FableObject piece = CreateFable($"bridge_piece_{id.ToLowerInvariant()}", pieceName,
                piecePosition, new Vector2(1.6f, 0.58f), pieceColor,
                FableTraits.Linkable | FableTraits.Resizable | FableTraits.Bouncy, pieceMass, false);
            MagpieBridgeAnchor anchor = socket.gameObject.AddComponent<MagpieBridgeAnchor>();
            anchor.Configure(id, socket, piece);
            if (!string.IsNullOrWhiteSpace(routeId))
            {
                GateRouteObjective objective = socket.gameObject.AddComponent<GateRouteObjective>();
                objective.Configure(routeId);
                anchor.ConfigureRouteObjective(objective);
            }
            WorldText($"제{id} 닻", anchorPosition + Vector2.up * 1.5f, 1.15f,
                new Color(1f, 0.38f, 0.55f), 56, gameplayRoot);
        }

        private static FableObject CreateFable(string id, string label, Vector2 position, Vector2 size,
            Color color, FableTraits traits, float mass, bool bouncy, bool staticAnchor = false)
        {
            GameObject item = SpriteBlock($"{label} [{id}]", position, size, color, 34, gameplayRoot);
            BoxCollider2D collider = item.AddComponent<BoxCollider2D>();
            collider.isTrigger = staticAnchor;
            if (!staticAnchor)
            {
                Rigidbody2D body = item.AddComponent<Rigidbody2D>();
                body.mass = Mathf.Max(0.1f, mass);
                body.gravityScale = 2.1f;
                body.freezeRotation = false;
                body.angularDamping = bouncy ? 0.35f : 1.2f;
            }

            FableObject fable = item.AddComponent<FableObject>();
            fable.Configure(id, label, StarItemKind.General, traits, Mathf.Max(0.5f, mass * 0.45f));
            return fable;
        }

        private static void CreateHaechiChoice()
        {
            FableObject haechi = CreateFable("haechi", "어린 까치 해치", new Vector2(100f, 0.15f),
                new Vector2(0.82f, 1.05f), new Color(0.94f, 0.9f, 1f),
                FableTraits.Linkable | FableTraits.Living | FableTraits.Resizable, 0.8f, false);
            CreateWing(haechi.transform, -0.5f);
            CreateWing(haechi.transform, 0.5f);
            FableObject tether = CreateFable("haechi_tether", "정거장 안전 말뚝", new Vector2(97.4f, -0.25f),
                new Vector2(0.45f, 1.65f), new Color(0.92f, 0.12f, 0.3f),
                FableTraits.Linkable | FableTraits.BridgeAnchor, 0f, false, true);
            MagpieHaechiLinkWatcher watcher = haechi.gameObject.AddComponent<MagpieHaechiLinkWatcher>();
            watcher.Configure(haechi, tether);
            haechi.gameObject.AddComponent<MaruNpcTarget>().Configure("Haechi", "어린 까치 해치", 15f);

            GameObject lockLever = InteractionBlock("LockLever · 출항문 자물쇠", new Vector2(103f, 0f),
                new Color(1f, 0.35f, 0.42f));
            MagpieHaechiDecision lockDecision = lockLever.AddComponent<MagpieHaechiDecision>();
            lockDecision.Configure(HaechiDecisionMode.LockDepartureDoor);
            GameObject openLever = InteractionBlock("OpenLever · 열린 길 표지", new Vector2(106f, 0f),
                new Color(0.35f, 0.88f, 1f));
            MagpieHaechiDecision openDecision = openLever.AddComponent<MagpieHaechiDecision>();
            openDecision.Configure(HaechiDecisionMode.LeaveDepartureOpen);
            WorldText("문을 잠근다 / 실로 묶는다 / 길을 열어 둔다", new Vector3(102f, 4.2f, 0f),
                1.2f, new Color(1f, 0.78f, 0.3f), 57, labelRoot);
        }

        private static void CreateStations()
        {
            GameObject supply = InteractionBlock("MoonMillSupply · 달토끼 물류상자", new Vector2(7f, -1.1f),
                new Color(0.85f, 0.62f, 0.28f));
            supply.AddComponent<MoonMillSupportCrate>();
            InstantiateArt(Station + "Small Box.prefab", "SupplyCrateArt", new Vector3(7f, -1.1f, 0f),
                0.65f, 20, new Color(1f, 0.78f, 0.35f));

            GameObject bell = InteractionBlock("EmergencyBell · 긴급 까치 방울", new Vector2(47f, 0f),
                new Color(1f, 0.65f, 0.16f));
            bell.AddComponent<MagpieCallBell>();
            WorldText("빠른 도움에는 피로가 남는다", new Vector3(47f, 3.8f, 0f), 1.1f,
                new Color(1f, 0.65f, 0.3f), 56, labelRoot);

            GameObject starGate = InteractionBlock("StarGateHub · 까치다리 별문", new Vector2(119f, 0f),
                new Color(1f, 0.52f, 0.28f), new Vector2(1.55f, 2.1f));
            starGate.AddComponent<StarGateController>();
            TMP_Text gateStatus = WorldText("까치다리 별문 · 닻 0/2", new Vector3(119f, 2.2f, 0f), 1.65f,
                new Color(1f, 0.78f, 0.3f), 58, labelRoot);
            starGate.AddComponent<StarGateWorldStatus>().Configure(gateStatus, "까치다리 별문", "닻");

            GameObject ladderBarrier = SpriteBlock("GateActive Barrier · 별사다리 봉인",
                new Vector3(66f, 4.15f, 0f), new Vector2(5f, 0.46f),
                new Color(0.84f, 0.18f, 0.52f, 0.76f), 34, gameplayRoot);
            ladderBarrier.layer = 7;
            ladderBarrier.AddComponent<BoxCollider2D>();
            GameObject ladderGate = InteractionBlock("TemptationGate · 까마득한 별사다리",
                new Vector2(67f, 3.2f), new Color(0.9f, 0.28f, 0.62f), new Vector2(1.1f, 1.5f));
            ladderGate.AddComponent<MagpieStarLadderTemptation>().Configure(ladderBarrier);
            WorldText("선택 · 별문 가동 후 출항을 미루고 오르는 위험한 별사다리",
                new Vector3(68f, 6.1f, 0f), 1.15f, new Color(1f, 0.48f, 0.72f), 58, labelRoot);

            GameObject workers = SpriteBlock("MagpieWorkers · 지친 까치들", new Vector3(45f, 0.1f, 0f),
                new Vector2(1.2f, 0.8f), new Color(0.92f, 0.9f, 1f), 35, gameplayRoot);
            workers.AddComponent<MaruNpcTarget>().Configure("MagpieWorkers", "지친 까치들", 13f);

            GameObject departure = InteractionBlock("StarTrainGate · 별기차 출항문", new Vector2(127f, 0f),
                new Color(0.35f, 0.9f, 1f), new Vector2(1.4f, 1.8f));
            departure.AddComponent<MagpieBridgeDepartureGate>();
            WorldText("별문 가동 후 즉시 출항 가능", new Vector3(127f, 3.6f, 0f), 1.35f,
                new Color(0.55f, 0.9f, 1f), 57, labelRoot);
        }

        private static GameObject InteractionBlock(string name, Vector2 position, Color color, Vector2? size = null)
        {
            GameObject station = SpriteBlock(name, position, size ?? new Vector2(0.9f, 1.2f),
                color, 36, gameplayRoot);
            CircleCollider2D trigger = station.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.25f;
            return station;
        }

        private static void CreateCheckpoints()
        {
            (string label, Vector2 position)[] checkpoints =
            {
                ("도착 승강장 등불", new Vector2(1f, -1f)),
                ("첫 매듭 등불", new Vector2(35f, -0.5f)),
                ("환승 홀 등불", new Vector2(72f, -1.1f)),
                ("해치의 문 등불", new Vector2(99f, 0.3f)),
                ("별기차 등불", new Vector2(124f, 0.1f))
            };
            foreach ((string label, Vector2 position) in checkpoints)
            {
                GameObject checkpoint = InteractionBlock($"Checkpoint · {label}", position,
                    new Color(1f, 0.72f, 0.28f), new Vector2(0.38f, 1.05f));
                StarNightCheckpoint component = checkpoint.AddComponent<StarNightCheckpoint>();
                component.Configure(label);
                InstantiateArt(Forest + "light.prefab", $"{label} Art", position, 0.38f, 24,
                    new Color(1f, 0.55f, 0.68f));
            }
        }

        private static void CreateDiscovery(string id, string label, Vector2 position, bool optional)
        {
            GameObject zone = new($"Discovery · {label}");
            zone.transform.SetParent(gameplayRoot);
            zone.transform.position = position;
            BoxCollider2D trigger = zone.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(6.5f, 6.5f);
            StarNightDiscoveryZone discovery = zone.AddComponent<StarNightDiscoveryZone>();
            discovery.Configure(id, label, optional);
        }

        private static void CreateGuide()
        {
            WorldText("제2장 · 까치다리 정거장의 꺼진 별문", new Vector3(0f, 7.2f, 0f), 2.6f,
                new Color(1f, 0.78f, 0.3f), 60, labelRoot);
            WorldText("목표 · 세 경로 중 서로 다른 다리 닻 2개를 별문에 연결", new Vector3(0f, 6.1f, 0f), 1.45f,
                new Color(0.72f, 0.85f, 1f), 59, labelRoot);
            WorldText("A 안전·협력 새 닻 · B 위험·탐색 폭풍탑 · C 빠름·전용 옛 다리",
                new Vector3(0f, 5.15f, 0f), 1.2f, new Color(1f, 0.62f, 0.42f), 59, labelRoot);
            WorldText("해치의 선택은 닻과 별개 · R 도구 · E 연결 · X 상호작용", new Vector3(0f, 4.2f, 0f),
                1.15f, new Color(0.88f, 0.92f, 1f), 59, labelRoot);
        }

        private static void CreateWorldBounds()
        {
            CreateCollisionPlatform("WorldBottom", new Vector2(66f, -12.6f), new Vector2(155f, 1f), collisionRoot);
            CreateCollisionPlatform("LeftBoundary", new Vector2(-7f, 2f), new Vector2(0.6f, 32f), collisionRoot);
            CreateCollisionPlatform("RightBoundary", new Vector2(136f, 2f), new Vector2(0.6f, 32f), collisionRoot);
        }

        private static GameObject CreateCollisionPlatform(string name, Vector2 position, Vector2 size, Transform parent)
        {
            GameObject platform = new(name);
            platform.transform.SetParent(parent);
            platform.transform.position = position;
            platform.layer = 7;
            BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
            collider.size = size;
            return platform;
        }

        private static GameObject SpriteBlock(string name, Vector3 position, Vector2 size, Color color,
            int sortingOrder, Transform parent)
        {
            GameObject block = new(name);
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return block;
        }

        private static TMP_Text WorldText(string text, Vector3 position, float size, Color color,
            int sortingOrder, Transform parent)
        {
            GameObject label = new($"Text · {text}");
            label.transform.SetParent(parent);
            label.transform.position = position;
            TextMeshPro tmp = label.AddComponent<TextMeshPro>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = sortingOrder;
            tmp.rectTransform.sizeDelta = new Vector2(10f, 2f);
            return tmp;
        }

        private static GameObject InstantiateArt(string path, string name, Vector3 position, float scale,
            int sortingOffset, Color tint, Transform parent = null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[Star Night P1] Missing bundle art: {path}");
                return new GameObject($"MISSING · {name}");
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = name;
            instance.transform.SetParent(parent != null ? parent : artRoot, true);
            instance.transform.position = position;
            instance.transform.localScale = Vector3.one * scale;

            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
            }
            foreach (Collider2D collider in instance.GetComponentsInChildren<Collider2D>(true))
            {
                Object.DestroyImmediate(collider);
            }
            foreach (Rigidbody2D body in instance.GetComponentsInChildren<Rigidbody2D>(true))
            {
                Object.DestroyImmediate(body);
            }
            foreach (SpriteRenderer renderer in instance.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.sortingOrder += sortingOffset;
                renderer.color = Multiply(renderer.color, tint);
            }
            foreach (ParticleSystemRenderer renderer in instance.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                renderer.sortingOrder += sortingOffset;
            }
            return instance;
        }

        private static Color Multiply(Color a, Color b) =>
            new(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);

        private static void CreateEar(Transform parent, float x)
        {
            GameObject ear = SpriteBlock("Ear", Vector3.zero, new Vector2(0.34f, 0.62f),
                new Color(1f, 0.24f, 0.48f), 49, parent);
            ear.transform.localPosition = new Vector3(x, 0.66f, 0f);
            ear.transform.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? -18f : 18f);
        }

        private static void CreateWing(Transform parent, float x)
        {
            GameObject wing = SpriteBlock("Wing", Vector3.zero, new Vector2(0.52f, 0.3f),
                new Color(0.62f, 0.68f, 0.86f), 35, parent);
            wing.transform.localPosition = new Vector3(x, 0f, 0f);
            wing.transform.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? 24f : -24f);
        }

        private static void AddToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(scene => scene.path != path))
            {
                int moonMillIndex = scenes.FindIndex(scene =>
                    scene.path == "Assets/Scenes/StarNight/StarNight_MoonMill.unity");
                int insert = moonMillIndex >= 0 ? moonMillIndex + 1 : scenes.Count;
                scenes.Insert(insert, new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        private static void EnsureFolder(string path)
        {
            if (Directory.Exists(path))
            {
                return;
            }
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }
}
#endif
