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
    public static class StarNightP2CloudWhaleRanchBuilder
    {
        private const string SceneFolder = "Assets/Scenes/StarNight";
        private const string ScenePath = SceneFolder + "/StarNight_CloudWhaleRanch.unity";
        private const string SquarePath = "Assets/Resources/Sprites/Square.png";
        private const string FontPath = "Assets/TextMesh Pro/Fonts/NeoDunggeunmoPro-Regular.asset";

        private const string Island = "Assets/2D Fantasy sprite bundle/Island pack/Prefabs/";
        private const string Desert = "Assets/2D Fantasy sprite bundle/Desert pack/Prefabs/";
        private const string Spring = "Assets/2D Fantasy sprite bundle/Spring forest/Prefabs/";
        private const string Crystal = "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/Crystal prefabs/";
        private const string Station = "Assets/2D Fantasy sprite bundle/Abandoned station/Prefabs/";
        private const string Underwater = "Assets/2D Fantasy sprite bundle/Underwater area pack/Prefabs/";
        private const string Mount = "Assets/2D Fantasy sprite bundle/Mount pack/Prefabs/";

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

        [MenuItem("Tools/Star Night/Build P2 Cloud Whale Ranch")]
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
                Debug.LogError("[Star Night P2] Square sprite or Korean TMP font is missing.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "StarNight_CloudWhaleRanch";
            world = new GameObject("WORLD · 구름고래 목장").transform;
            artRoot = ChildRoot("ART · 2D Fantasy Sky Ranch", world);
            collisionRoot = ChildRoot("COLLISION · Stable Cloud Route", world);
            gameplayRoot = ChildRoot("GAMEPLAY · Conserved Weight Puzzles", world);
            labelRoot = ChildRoot("ROOM TITLES", world);

            Camera camera = CreateCamera();
            CreateMainLight();
            CreateBackdrop();
            List<Room> rooms = CreateRooms();
            CreateMainRoute(rooms);
            CreateRainbowBranch();
            CreateStormBranch();
            CreateRanchLandmarks();
            CreateCalfReturnStory();
            GameObject player = CreatePlayer();
            CreateSystems(camera, player.transform);
            CreateWeightPuzzles();
            CreateGuruStory();
            CreateInheritedConsequences();
            CreateDeparture();
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
            Debug.Log("[Star Night M3-3] Cloud Whale Ranch built: 16 rooms, 3 wind routes, manual star gate, optional rainbow ranch.");
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
            camera.backgroundColor = new Color(0.04f, 0.11f, 0.24f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateMainLight()
        {
            GameObject lightObject = new("Cloudlight · Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.86f, 0.94f, 1f);
            light.intensity = 0.72f;
            lightObject.transform.rotation = Quaternion.Euler(30f, -26f, 0f);
        }

        private static void CreateBackdrop()
        {
            for (int i = 0; i < 4; i++)
            {
                InstantiateArt(Island + "mounts and sky.prefab", $"SkyIslandBackdrop_{i}",
                    new Vector3(i * 52f, 1f, 8f), 1.45f, -62,
                    new Color(0.55f, 0.78f, 1f));
                InstantiateArt(Desert + "Clouds.prefab", $"LongCloudBank_{i}",
                    new Vector3(18f + i * 46f, 4f + (i % 2) * 2f, 6f), 1.4f, -48,
                    new Color(0.72f, 0.9f, 1f));
            }
            InstantiateArt(Spring + "Backgrounds.prefab", "RainPastureBackground",
                new Vector3(78f, -1f, 7f), 1.8f, -54, new Color(0.5f, 0.78f, 0.9f));
            InstantiateArt(Crystal + "Stars Particle.prefab", "HighCloudStars",
                new Vector3(96f, 4f, 0f), 1.6f, -24, new Color(0.95f, 0.75f, 1f));
            InstantiateArt(Underwater + "Particle Bubbles.prefab", "RainBubbles",
                new Vector3(47f, 2f, 0f), 1.1f, -22, new Color(0.65f, 0.88f, 1f));
        }

        private static List<Room> CreateRooms()
        {
            return new List<Room>
            {
                new("arrival", "까치 화물 도착장", 0f, 0f),
                new("empty_supply", "마른 까치 보급고", 10f, 0.4f),
                new("weight_school", "무게 보존 교실", 20f, 1.2f),
                new("cloud_a", "첫 비구름 논", 30f, 0f),
                new("wind_barn", "몽실의 바람 헛간", 40f, 1.3f),
                new("dock_a", "낮은 구름 수차", 50f, 0f),
                new("mooncake_path", "떠오르는 달떡 길", 60f, 2.2f),
                new("cloud_b", "두 번째 비구름 목책", 70f, 1f),
                new("guru_anchor", "구루의 닻터", 80f, 0f),
                new("guru_back", "고래등 낙서 언덕", 90f, 2f, true),
                new("rain_workshop", "이동식 비구름 작업장", 100f, 0.5f),
                new("storm_hall", "폭풍 하중 회랑", 110f, 2f),
                new("cloud_c", "세 번째 비구름 절벽", 120f, 1f),
                new("damage_deck", "폭풍 사고 관측대", 130f, 0f, true),
                new("wind_charge", "출항 풍차 충전실", 140f, 1.4f),
                new("departure", "별 우체국행 바람선", 150f, 0.3f)
            };
        }

        private static void CreateMainRoute(List<Room> rooms)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                Room room = rooms[i];
                float floorY = room.y - 2.7f;
                CreateCollisionPlatform($"Floor_{room.id}", new Vector2(room.x, floorY),
                    new Vector2(8.4f, 0.6f), collisionRoot);

                string art = i % 3 == 0
                    ? Island + "platform wt.prefab"
                    : i % 3 == 1 ? Island + "Smal platform.prefab" : Mount + "simple platform.prefab";
                InstantiateArt(art, $"FloorArt_{room.id}", new Vector3(room.x, floorY + 0.28f, 0f),
                    i % 3 == 1 ? 0.72f : 0.62f, -6,
                    room.optional ? new Color(0.85f, 0.65f, 1f) : new Color(0.68f, 0.92f, 1f));

                WorldText(room.label, new Vector3(room.x, room.y + 2.8f, 0f), 1.25f,
                    room.optional ? new Color(0.95f, 0.58f, 1f) : new Color(1f, 0.84f, 0.34f),
                    54, labelRoot);
                CreateDiscovery(room.id, room.label, new Vector2(room.x, room.y), room.optional);

                if (i >= rooms.Count - 1)
                {
                    continue;
                }

                Room next = rooms[i + 1];
                float nextFloor = next.y - 2.7f;
                Vector2 connector = new((room.x + next.x) * 0.5f, Mathf.Min(floorY, nextFloor) + 0.45f);
                CreateCollisionPlatform($"Connector_{i:00}", connector, new Vector2(2.8f, 0.45f), collisionRoot);
                InstantiateArt(Island + "Smal platform.prefab", $"ConnectorArt_{i:00}",
                    new Vector3(connector.x, connector.y + 0.18f, 0f), 0.42f, -5,
                    new Color(0.7f, 0.9f, 1f));
            }
        }

        private static void CreateRainbowBranch()
        {
            Transform branch = ChildRoot("BRANCH · 무지개 위쪽 목장", world);
            Vector2[] points =
            {
                new(72f, 5.2f), new(76f, 7.8f), new(81f, 10.2f),
                new(86f, 12.6f), new(91f, 15f)
            };
            for (int i = 0; i < points.Length; i++)
            {
                CreateCollisionPlatform($"RainbowStep_{i}", points[i], new Vector2(3.8f, 0.42f), branch);
                InstantiateArt(i % 2 == 0 ? Crystal + "Crystal platform A.prefab" : Island + "platform wt.prefab",
                    $"RainbowArt_{i}", new Vector3(points[i].x, points[i].y + 0.18f, 0f),
                    i % 2 == 0 ? 0.32f : 0.42f, 6,
                    new Color(0.92f, 0.6f + i * 0.06f, 1f), branch);
            }

            GameObject upgrade = InteractionBlock("RainbowBottle · 바람을 담는 큰 구름병",
                new Vector2(91f, 16.1f), new Color(0.55f, 0.9f, 1f), branch);
            upgrade.AddComponent<CloudBottleUpgrade>();
            GameObject barrier = SpriteBlock("RainbowRanchBarrier · GateActive 선택 봉인",
                new Vector3(70.2f, 4.4f, 0f), new Vector2(0.55f, 5.2f),
                new Color(0.52f, 0.18f, 0.72f, 0.88f), 38, branch);
            barrier.layer = 7;
            barrier.AddComponent<BoxCollider2D>();
            GameObject entrance = InteractionBlock("RainbowRanchChoice · 무지개 목장 입구",
                new Vector2(68f, 1.3f), new Color(0.95f, 0.48f, 1f), branch);
            entrance.AddComponent<CloudRainbowRanchTemptation>().Configure(barrier);
            WorldText("유혹 · 구름병 용량 +2 / 높은 별냄새", new Vector3(88f, 17.2f, 0f),
                1.1f, new Color(1f, 0.55f, 0.9f), 58, branch);
            CreateDiscovery("rainbow_ranch", "무지개 위쪽 목장", new Vector2(83f, 12f), true);
        }

        private static void CreateStormBranch()
        {
            Transform branch = ChildRoot("BRANCH · 폭풍 아래 사고 회랑", world);
            for (int i = 0; i < 6; i++)
            {
                Vector2 point = new(102f + i * 6.5f, -5.5f + Mathf.Sin(i * 0.8f) * 0.7f);
                CreateCollisionPlatform($"StormDeck_{i}", point, new Vector2(4.7f, 0.45f), branch);
                InstantiateArt(Station + "Platform with ropes.prefab", $"StormDeckArt_{i}",
                    new Vector3(point.x, point.y + 0.22f, 0f), 0.4f, 2,
                    new Color(0.44f, 0.62f, 0.9f), branch);
            }

            CreateWindVolume("StormWindLeft", new Vector2(111f, -2.2f), new Vector2(12f, 6f),
                new Vector2(-5.5f, 2f), branch);
            CreateWindVolume("StormWindRight", new Vector2(124f, -2.2f), new Vector2(12f, 6f),
                new Vector2(6.5f, 2.5f), branch);
            InstantiateArt(Island + "Wind.prefab", "StormWindVisualA", new Vector3(111f, -2f, 0f),
                1.2f, 8, new Color(0.68f, 0.82f, 1f), branch);
            InstantiateArt(Island + "Wind Clone.prefab", "StormWindVisualB", new Vector3(124f, -2f, 0f),
                1.2f, 8, new Color(0.9f, 0.65f, 1f), branch);
            CreateDiscovery("storm_underpass", "폭풍 아래 사고 회랑", new Vector2(117f, -4f), true);
        }

        private static void CreateRanchLandmarks()
        {
            InstantiateArt(Spring + "Tree.prefab", "MongsilWindTree", new Vector3(40f, -0.7f, 0f),
                0.7f, -2, new Color(0.65f, 0.95f, 0.85f));
            InstantiateArt(Spring + "grass field.prefab", "RainFieldA", new Vector3(32f, -2f, 0f),
                0.8f, -3, new Color(0.55f, 0.9f, 0.7f));
            InstantiateArt(Spring + "grass field.prefab", "RainFieldB", new Vector3(51f, -2f, 0f),
                0.8f, -3, new Color(0.55f, 0.9f, 0.7f));
            InstantiateArt(Mount + "Mound and clouds B.prefab", "GuruCloudHill",
                new Vector3(84f, -0.5f, 0f), 1.1f, -4, new Color(0.65f, 0.82f, 1f));
            InstantiateArt(Spring + "SunLight.prefab", "RanchSunbeams",
                new Vector3(68f, 5f, 1f), 1.15f, -18, new Color(0.95f, 0.9f, 0.65f));
        }

        private static void CreateCalfReturnStory()
        {
            FableObject calf = CreateFable("cloud_calf", "길 잃은 새끼 구름고래",
                new Vector2(5f, 1.8f), new Vector2(1.45f, 0.75f),
                new Color(0.68f, 0.9f, 1f),
                FableTraits.Floatable | FableTraits.Linkable | FableTraits.Living |
                FableTraits.CloudWhale,
                0.9f, -0.04f);
            CreateWhaleFeatures(calf.transform);
            GameObject motherSide = SpriteBlock("MotherCloudWhaleSide · 어미 곁",
                new Vector3(13f, 0.2f, 0f), new Vector2(2.6f, 1.2f),
                new Color(0.76f, 0.88f, 1f, 0.7f), 25, gameplayRoot);
            CreateWhaleFeatures(motherSide.transform);
            GameObject story = new("Mandatory Story · 마루가 새끼 고래를 돌려놓음");
            story.transform.SetParent(gameplayRoot);
            story.AddComponent<CloudCalfReturnStory>().Configure(calf, motherSide.transform);
            WorldText("필수 장면 · 마루는 길 잃은 새끼를 어미 곁으로 돌려놓는다",
                new Vector3(10f, 4.5f, 0f), 0.98f,
                new Color(1f, 0.72f, 0.82f), 62, labelRoot);
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = SpriteBlock("Player · 별을 줍는 아이", new Vector3(-2f, -1.4f, 0f),
                new Vector2(0.72f, 1.2f), new Color(1f, 0.78f, 0.3f), 42, world);
            player.layer = 31;
            GameObject scarf = SpriteBlock("RedThreadScarf", Vector3.zero, new Vector2(0.9f, 0.12f),
                new Color(0.95f, 0.12f, 0.32f), 44, player.transform);
            scarf.transform.localPosition = new Vector3(-0.5f, 0.05f, 0f);
            GameObject bottle = SpriteBlock("CloudBottle", Vector3.zero, new Vector2(0.24f, 0.48f),
                new Color(0.38f, 0.9f, 1f), 45, player.transform);
            bottle.transform.localPosition = new Vector3(0.5f, -0.05f, 0f);

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
            GameObject systems = new("@STAR NIGHT P2 · 보존되는 무게");
            CloudWhaleChapterBootstrap bootstrap =
                systems.AddComponent<CloudWhaleChapterBootstrap>();
            bootstrap.ConfigureGateLoop(true);
            systems.AddComponent<StarNightCombinationResolver>();
            systems.AddComponent<ChapterPlaytestTelemetry>();

            GameObject hudObject = new("@HUD · 구름병과 라니 기록");
            StarNightHUD hud = hudObject.AddComponent<StarNightHUD>();
            hud.SetFont(font);

            GameObject atmosphereObject = new("@ATMOSPHERE · 비와 부유");
            StarNightAtmosphere atmosphere = atmosphereObject.AddComponent<StarNightAtmosphere>();
            atmosphere.Configure(camera, player, square);
            atmosphere.SetWorldBounds(new Vector2(-14f, 166f), new Vector2(-11f, 21f), 190);

            GameObject maru = SpriteBlock("Maru · 공중 냄새를 쫓는 개", new Vector3(160f, 9f, 0f),
                new Vector2(1.6f, 1.15f), new Color(1f, 0.24f, 0.48f), 50, world);
            CreateEar(maru.transform, -0.42f);
            CreateEar(maru.transform, 0.42f);
            MaruDirector director = systems.AddComponent<MaruDirector>();
            director.Configure(maru.transform, new Vector3(160f, 9f, 0f));

            GameObject firstBellTrace = SpriteBlock("Bell 1 · 한쪽으로 밀리는 구름",
                new Vector3(117f, 5f, 0f), new Vector2(11f, 0.22f),
                new Color(0.55f, 0.8f, 1f, 0.65f), 31, world);
            firstBellTrace.SetActive(false);
            GameObject secondBellPresence = SpriteBlock("Bell 2 · 새끼 고래를 찾는 마루 그림자",
                new Vector3(132f, 4.1f, 0f), new Vector2(6.5f, 0.32f),
                new Color(1f, 0.2f, 0.48f, 0.72f), 32, world);
            secondBellPresence.SetActive(false);
            GameObject gateClosingVisual = SpriteBlock("Bell 3 · 뒤집힌 전체 풍향",
                new Vector3(145f, 0.2f, 0f), new Vector2(0.45f, 6.2f),
                new Color(1f, 0.16f, 0.4f, 0.82f), 39, world);
            gateClosingVisual.SetActive(false);
            BellChasePresenter presenter = systems.AddComponent<BellChasePresenter>();
            presenter.Configure(director, firstBellTrace, secondBellPresence, gateClosingVisual);
        }

        private static void CreateWeightPuzzles()
        {
            CreateRainPuzzle("A", new Vector2(20f, 0.1f), 2.2f,
                new Vector2(30f, 6.5f), new Vector2(30f, -1.15f),
                new Color(0.55f, 0.86f, 1f),
                "CH3_ROUTE_RANCH_WHEEL", "CH3_ROUTE_RANCH_WHEEL_COMPLETE",
                "A 안전·협력 · 목장 수차 > 맑은 바람");
            CreateRainPuzzle("B", new Vector2(61f, 2.6f), 3.1f,
                new Vector2(70f, 8f), new Vector2(70f, -0.15f),
                new Color(0.62f, 0.72f, 1f), null, null,
                "보조 수차 · 비를 유지하는 선택 복구");
            CreateRainPuzzle("C", new Vector2(110f, 3.4f), 4.2f,
                new Vector2(120f, 9f), new Vector2(120f, 0f),
                new Color(0.72f, 0.55f, 1f),
                "CH3_ROUTE_STORM_RIDGE", "CH3_ROUTE_STORM_RIDGE_COMPLETE",
                "B 위험·탐색 · 폭풍 능선 > 거센 바람");

            CreateFable("floating_mooncake", "떠오르는 거대 달떡", new Vector2(56f, 4.8f),
                new Vector2(1.8f, 0.7f), new Color(1f, 0.76f, 0.35f),
                FableTraits.Floatable | FableTraits.Linkable | FableTraits.Resizable |
                FableTraits.Carryable | FableTraits.MoonCake,
                1.6f, -0.08f);
            CreateFable("storm_fruit", "바람 든 톡톡별 열매", new Vector2(128f, 3.8f),
                new Vector2(0.8f, 0.8f), new Color(1f, 0.25f, 0.42f),
                FableTraits.Floatable | FableTraits.Linkable | FableTraits.Explosive | FableTraits.Carryable,
                0.7f, -0.12f);
        }

        private static void CreateRainPuzzle(string id, Vector2 sourcePosition, float sourceMass,
            Vector2 cloudPosition, Vector2 dockPosition, Color cloudColor,
            string routeId = null, string completionFlag = null, string routeLabel = null)
        {
            FableObject source = CreateFable($"weight_source_{id.ToLowerInvariant()}", $"{id} 목장 하중추",
                sourcePosition, new Vector2(1.15f, 1.15f), new Color(0.52f, 0.48f, 0.62f),
                FableTraits.Floatable | FableTraits.Linkable | FableTraits.WeightReservoir |
                FableTraits.Resizable,
                sourceMass, 2.2f);
            source.gameObject.AddComponent<CloudWeightState>();

            FableObject cloud = CreateFable($"rain_cloud_{id.ToLowerInvariant()}", $"{id} 비구름",
                cloudPosition, new Vector2(2.3f, 1.05f), cloudColor,
                FableTraits.Floatable | FableTraits.Linkable | FableTraits.RainCloud,
                0.72f, 0f);
            cloud.gameObject.AddComponent<CloudWeightState>();
            CreateCloudPuffs(cloud.transform, cloudColor);

            GameObject dock = SpriteBlock($"RainDock_{id} · 비구름 수차", dockPosition,
                new Vector2(2.7f, 1.15f), new Color(0.18f, 0.65f, 0.82f, 0.7f), 22, gameplayRoot);
            CloudRainDock rainDock = dock.AddComponent<CloudRainDock>();
            rainDock.Configure(id, cloud, 1.35f);
            if (!string.IsNullOrWhiteSpace(routeId))
            {
                GateRouteObjective objective = dock.AddComponent<GateRouteObjective>();
                objective.Configure(routeId);
                rainDock.ConfigureRouteObjective(objective, completionFlag);
            }
            WorldText(string.IsNullOrWhiteSpace(routeLabel)
                    ? $"{id} 수차 · 비구름을 무겁게 내려라"
                    : routeLabel,
                dockPosition + Vector2.up * 1.35f,
                1.02f, new Color(0.55f, 0.92f, 1f), 58, labelRoot);
        }

        private static void CreateGuruStory()
        {
            FableObject guru = CreateFable("guru", "큰 구름고래 구루", new Vector2(82f, 0.25f),
                new Vector2(3.4f, 1.55f), new Color(0.82f, 0.93f, 1f),
                FableTraits.Floatable | FableTraits.Linkable | FableTraits.Living |
                FableTraits.CloudWhale,
                5.5f, 0f);
            guru.Body.freezeRotation = true;
            CreateWhaleFeatures(guru.transform);

            GameObject anchor = SpriteBlock("GuruAnchor · 비를 위한 닻", new Vector3(82f, -1.25f, 0f),
                new Vector2(0.48f, 2.5f), new Color(0.42f, 0.22f, 0.28f), 30, gameplayRoot);
            anchor.layer = 7;
            anchor.AddComponent<BoxCollider2D>();
            SpriteBlock("GuruRope", new Vector3(82f, 0.1f, 0f), new Vector2(0.12f, 3.2f),
                new Color(0.74f, 0.18f, 0.28f), 35, gameplayRoot);

            GameObject bell = InteractionBlock("GuruBell · 잠든 고래의 방울",
                new Vector2(77.8f, -0.2f), new Color(1f, 0.76f, 0.25f), gameplayRoot);
            GateRouteObjective breathObjective = bell.AddComponent<GateRouteObjective>();
            breathObjective.Configure("CH3_ROUTE_GURU_BREATH");
            bell.AddComponent<CloudGuruBell>().ConfigureRouteObjective(breathObjective);
            GameObject lullaby = InteractionBlock("GuruLullaby · 자장가 하중 분산 장치",
                new Vector2(73.8f, -0.2f), new Color(0.42f, 0.9f, 0.78f), gameplayRoot);
            lullaby.AddComponent<CloudGuruRestStation>();

            GameObject release = InteractionBlock("GuruRelease · 닻 해제 레버",
                new Vector2(86f, -0.25f), new Color(1f, 0.38f, 0.46f), gameplayRoot);
            CloudGuruDecision releaseDecision = release.AddComponent<CloudGuruDecision>();
            releaseDecision.Configure(GuruDecisionMode.ReleaseAnchor, guru, anchor);

            GameObject rebuild = InteractionBlock("RainRebuild · 이동식 수차 조립대",
                new Vector2(99f, -0.4f), new Color(0.38f, 0.92f, 0.72f), gameplayRoot);
            CloudGuruDecision rebuildDecision = rebuild.AddComponent<CloudGuruDecision>();
            rebuildDecision.Configure(GuruDecisionMode.RebuildRainSystem, guru, anchor);

            WorldText("구루는 떠나 달라고 하지 않았다", new Vector3(82f, 3.6f, 0f),
                1.15f, new Color(1f, 0.82f, 0.38f), 60, labelRoot);
            WorldText("C 빠름·개입 · 방울 3회 > 구루의 숨결 / GateReady 전 다시 재우기",
                new Vector3(78f, 5f, 0f), 0.94f,
                new Color(1f, 0.7f, 0.36f), 60, labelRoot);
            WorldText("낙서 · 다음에는 더 먼 별에 가자 / 긁혀 지운 흔적", new Vector3(91f, 5.2f, 0f),
                1.0f, new Color(0.95f, 0.62f, 1f), 60, labelRoot);
        }

        private static void CreateInheritedConsequences()
        {
            FableObject replacement = CreateFable("magpie_replacement_weight", "까치 문양 대체 하중추",
                new Vector2(9f, -0.5f), new Vector2(1.25f, 1.25f), new Color(0.72f, 0.48f, 0.3f),
                FableTraits.Floatable | FableTraits.WeightReservoir | FableTraits.Linkable,
                3f, 2.2f);
            GameObject safetyNet = SpriteBlock("MagpieSafetyNet · 까치 구조망",
                new Vector3(106f, -0.9f, 0f), new Vector2(5.4f, 0.18f),
                new Color(0.95f, 0.24f, 0.42f), 20, gameplayRoot);
            CloudRanchInheritedSupply inherited = new GameObject("P1 Consequence Display")
                .AddComponent<CloudRanchInheritedSupply>();
            inherited.transform.SetParent(gameplayRoot);
            inherited.Configure(replacement, safetyNet);
            WorldText("옛 다리가 끊겼다면 이 상자만 남는다", new Vector3(9f, 1.6f, 0f),
                0.95f, new Color(1f, 0.64f, 0.35f), 58, labelRoot);
        }

        private static void CreateDeparture()
        {
            InstantiateArt(Station + "Core.prefab", "WindshipCore",
                new Vector3(149f, -0.2f, 0f), 0.72f, 10, new Color(0.55f, 0.9f, 1f));
            GameObject gate = InteractionBlock("DepartureGate · 별 우체국행 바람선",
                new Vector2(151f, -0.45f), new Color(1f, 0.78f, 0.3f), gameplayRoot);
            gate.transform.localScale = new Vector3(1.3f, 1.7f, 1f);
            gate.GetComponent<CircleCollider2D>().radius = 1.5f;
            gate.AddComponent<CloudRanchDepartureGate>();
            WorldText("별문 가동 후 즉시 출항 · 구루의 닻은 별도 선택", new Vector3(149f, 3.4f, 0f),
                1.25f, new Color(1f, 0.82f, 0.35f), 58, labelRoot);

            GameObject starGate = InteractionBlock("StarGateHub · 구름 목장 별문",
                new Vector2(144f, -0.2f), new Color(0.42f, 0.9f, 1f), gameplayRoot);
            starGate.transform.localScale = new Vector3(1.4f, 1.8f, 1f);
            starGate.AddComponent<StarGateController>();
            TMP_Text status = WorldText("구름 목장 별문 · 바람 0/2",
                new Vector3(143f, 2.1f, 0f), 1.5f,
                new Color(0.5f, 0.92f, 1f), 63, labelRoot);
            starGate.AddComponent<StarGateWorldStatus>().Configure(
                status, "구름 목장 별문", "바람");
        }

        private static void CreateCheckpoints()
        {
            Vector2[] points =
            {
                new(2f, -1.4f), new(38f, -0.8f), new(76f, -1.2f),
                new(112f, 0f), new(143f, 0f)
            };
            for (int i = 0; i < points.Length; i++)
            {
                GameObject lamp = SpriteBlock($"CloudLantern_{i + 1}", points[i],
                    new Vector2(0.42f, 0.72f), new Color(0.42f, 0.9f, 1f), 36, gameplayRoot);
                CircleCollider2D trigger = lamp.AddComponent<CircleCollider2D>();
                trigger.isTrigger = true;
                trigger.radius = 1.15f;
                StarNightCheckpoint checkpoint = lamp.AddComponent<StarNightCheckpoint>();
                checkpoint.Configure($"구름등불 {i + 1}");
            }
        }

        private static void CreateGuide()
        {
            WorldText("제3장 · 구름고래 목장", new Vector3(2f, 4.7f, 0f),
                1.9f, new Color(1f, 0.82f, 0.3f), 62, labelRoot);
            WorldText("출항 돛에 서로 다른 바람 2개 채우기: 0/2",
                new Vector3(20f, 5.3f, 0f), 1.4f,
                new Color(1f, 0.82f, 0.3f), 62, labelRoot);
            WorldText("무게는 사라지지 않는다", new Vector3(20f, 4.4f, 0f),
                1.35f, new Color(0.55f, 0.92f, 1f), 62, labelRoot);
            WorldText("R 도구 전환 · E 첫 대상에서 담기 · E 두 번째 대상에 남기기",
                new Vector3(20f, 3.5f, 0f), 0.95f, Color.white, 62, labelRoot);
            WorldText("가벼운 물건은 바람과 마루에게 더 잘 잡힌다",
                new Vector3(112f, 6.2f, 0f), 1.05f, new Color(1f, 0.52f, 0.72f), 62, labelRoot);
            WorldText("A 목장 수차 · B 폭풍 능선 · C 구루의 숨결",
                new Vector3(45f, 5.2f, 0f), 1.05f,
                new Color(0.72f, 0.9f, 1f), 62, labelRoot);
        }

        private static void CreateWorldBounds()
        {
            CreateCollisionPlatform("FallCatch", new Vector2(76f, -10.5f), new Vector2(176f, 0.8f), collisionRoot);
            CreateCollisionPlatform("LeftBoundary", new Vector2(-10f, 2f), new Vector2(0.8f, 28f), collisionRoot);
            CreateCollisionPlatform("RightBoundary", new Vector2(164f, 2f), new Vector2(0.8f, 28f), collisionRoot);
        }

        private static FableObject CreateFable(string id, string label, Vector2 position, Vector2 size,
            Color color, FableTraits traits, float mass, float gravity)
        {
            GameObject item = SpriteBlock($"{label} [{id}]", position, size, color, 34, gameplayRoot);
            item.AddComponent<BoxCollider2D>();
            Rigidbody2D body = item.AddComponent<Rigidbody2D>();
            body.mass = Mathf.Max(0.1f, mass);
            body.gravityScale = gravity;
            body.angularDamping = 1.1f;
            FableObject fable = item.AddComponent<FableObject>();
            fable.Configure(id, label, StarItemKind.General, traits, Mathf.Max(0.5f, mass * 0.38f));
            return fable;
        }

        private static void CreateCloudPuffs(Transform parent, Color color)
        {
            Vector2[] offsets =
            {
                new(-0.72f, 0.2f), new(0f, 0.38f), new(0.74f, 0.16f), new(0.2f, -0.2f)
            };
            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject puff = SpriteBlock($"Puff_{i}", Vector3.zero,
                    new Vector2(0.82f, 0.62f), color, 36, parent);
                puff.transform.localPosition = offsets[i];
                puff.transform.localRotation = Quaternion.Euler(0f, 0f, i * 9f);
            }
        }

        private static void CreateWhaleFeatures(Transform parent)
        {
            GameObject tail = SpriteBlock("GuruTail", Vector3.zero, new Vector2(0.9f, 0.65f),
                new Color(0.65f, 0.84f, 1f), 36, parent);
            tail.transform.localPosition = new Vector3(-1.9f, 0f, 0f);
            tail.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
            GameObject eye = SpriteBlock("GuruEye", Vector3.zero, new Vector2(0.12f, 0.12f),
                new Color(0.08f, 0.12f, 0.24f), 38, parent);
            eye.transform.localPosition = new Vector3(1.2f, 0.24f, 0f);
            GameObject fin = SpriteBlock("GuruFin", Vector3.zero, new Vector2(0.85f, 0.26f),
                new Color(0.58f, 0.8f, 1f), 35, parent);
            fin.transform.localPosition = new Vector3(0.1f, -0.72f, 0f);
        }

        private static GameObject InteractionBlock(string name, Vector2 position, Color color,
            Transform parent = null)
        {
            GameObject block = SpriteBlock(name, position, new Vector2(0.8f, 1f), color, 40,
                parent != null ? parent : gameplayRoot);
            CircleCollider2D trigger = block.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.25f;
            return block;
        }

        private static void CreateWindVolume(string name, Vector2 position, Vector2 size,
            Vector2 force, Transform parent)
        {
            GameObject wind = new(name);
            wind.transform.SetParent(parent);
            wind.transform.position = position;
            BoxCollider2D trigger = wind.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = size;
            CloudWindVolume volume = wind.AddComponent<CloudWindVolume>();
            volume.Configure(force);
        }

        private static void CreateDiscovery(string id, string label, Vector2 position, bool optional)
        {
            GameObject zone = new($"Discovery · {label}");
            zone.transform.SetParent(gameplayRoot);
            zone.transform.position = position;
            BoxCollider2D trigger = zone.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(7.5f, 6f);
            StarNightDiscoveryZone discovery = zone.AddComponent<StarNightDiscoveryZone>();
            discovery.Configure(id, label, optional);
        }

        private static void CreateCollisionPlatform(string name, Vector2 position, Vector2 size, Transform parent)
        {
            GameObject platform = new(name);
            platform.transform.SetParent(parent);
            platform.transform.position = position;
            platform.layer = 7;
            BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
            collider.size = size;
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
            tmp.rectTransform.sizeDelta = new Vector2(12f, 2f);
            return tmp;
        }

        private static GameObject InstantiateArt(string path, string name, Vector3 position, float scale,
            int sortingOffset, Color tint, Transform parent = null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[Star Night P2] Missing bundle art: {path}");
                GameObject missing = new($"MISSING · {name}");
                missing.transform.SetParent(parent != null ? parent : artRoot);
                return missing;
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
                new Color(1f, 0.24f, 0.48f), 51, parent);
            ear.transform.localPosition = new Vector3(x, 0.66f, 0f);
            ear.transform.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? -18f : 18f);
        }

        private static void AddToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(scene => scene.path == path);
            int p1Index = scenes.FindIndex(scene =>
                scene.path == "Assets/Scenes/StarNight/StarNight_MagpieBridge.unity");
            int insert = p1Index >= 0 ? p1Index + 1 : scenes.Count;
            scenes.Insert(insert, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
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
