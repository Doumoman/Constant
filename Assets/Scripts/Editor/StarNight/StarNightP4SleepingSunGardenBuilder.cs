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
    public static class StarNightP4SleepingSunGardenBuilder
    {
        private const string SceneFolder = "Assets/Scenes/StarNight";
        private const string ScenePath = SceneFolder + "/StarNight_SleepingSunGarden.unity";
        private const string SquarePath = "Assets/Resources/Sprites/Square.png";
        private const string FontPath = "Assets/TextMesh Pro/Fonts/NeoDunggeunmoPro-Regular.asset";

        private const string Spring = "Assets/2D Fantasy sprite bundle/Spring forest/Prefabs/";
        private const string OldForest = "Assets/2D Fantasy sprite bundle/Old Forest pack/Prefabs/";
        private const string Lava = "Assets/2D Fantasy sprite bundle/Lava dungeon pack/Prefabs/";
        private const string Island = "Assets/2D Fantasy sprite bundle/Island pack/Prefabs/";
        private const string Crystal = "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/Crystal prefabs/";

        private static Sprite square;
        private static TMP_FontAsset font;
        private static Transform world;
        private static Transform artRoot;
        private static Transform collisionRoot;
        private static Transform gameplayRoot;
        private static Transform labelRoot;
        private static StarPathTreeController starPathTree;
        private static MaruDirector maruDirector;
        private static SunGardenStoredLightRoute storedLightRoute;

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

        [MenuItem("Tools/Star Night/Build P4 Sleeping Sun Garden")]
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
                Debug.LogError("[Star Night P4] Square sprite or Korean TMP font is missing.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "StarNight_SleepingSunGarden";
            world = new GameObject("WORLD · 잠든 해님의 정원").transform;
            artRoot = ChildRoot("ART · 2D Fantasy Sleeping Garden", world);
            collisionRoot = ChildRoot("COLLISION · Stable Garden Route", world);
            gameplayRoot = ChildRoot("GAMEPLAY · Light Growth And Heat", world);
            labelRoot = ChildRoot("ROOM TITLES", world);

            Camera camera = CreateCamera();
            CreateMainLight();
            CreateBackdrop();
            List<Room> rooms = CreateRooms();
            CreateMainRoute(rooms);
            CreateGrowthBranches();
            GameObject player = CreatePlayer();
            CreateSystems(camera, player.transform);
            CreateMandatoryCommandEcho();
            CreateStoredSunlight();
            CreateGrowthLessons();
            CreateHaoreumStory();
            CreateStarPathTree();
            CreateTreeDecisions();
            CreateRestorationAndTemptation();
            CreateRaniMemory();
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
            Debug.Log("[Star Night M3-4] Sleeping Sun Garden built: three light routes, path-flower gate 2/2, manual activation, stopped-room temptation, bell chase.");
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
            camera.backgroundColor = new Color(0.055f, 0.045f, 0.11f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateMainLight()
        {
            GameObject lightObject = new("Drowsy Sunlight · Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.73f, 0.35f);
            light.intensity = 0.78f;
            lightObject.transform.rotation = Quaternion.Euler(42f, -30f, 0f);
        }

        private static void CreateBackdrop()
        {
            for (int i = 0; i < 4; i++)
            {
                InstantiateArt(Spring + "Backgrounds.prefab", $"SleepingGardenBackground_{i}",
                    new Vector3(i * 48f, -0.5f, 8f), 1.25f, -65,
                    new Color(0.45f, 0.48f, 0.68f));
                InstantiateArt(OldForest + "Fog.prefab", $"DreamFog_{i}",
                    new Vector3(20f + i * 45f, 0.5f, 5f), 1.3f, -28,
                    new Color(0.46f, 0.54f, 0.72f));
            }
            InstantiateArt(Spring + "SunLight.prefab", "SleepingSunLeak",
                new Vector3(89f, 6f, 1f), 1.35f, -12, new Color(1f, 0.72f, 0.28f));
            InstantiateArt(Spring + "Particle Bugs.prefab", "GardenFireflies",
                new Vector3(60f, 3f, 0f), 1.7f, 6, new Color(1f, 0.75f, 0.32f));
            InstantiateArt(OldForest + "Firefly particle.prefab", "MemoryFireflies",
                new Vector3(118f, 3f, 0f), 1.6f, 7, new Color(0.55f, 0.8f, 1f));
        }

        private static List<Room> CreateRooms()
        {
            return new List<Room>
            {
                new("arrival", "우편선이 닿은 저녁뜰", 0f, 0f),
                new("seed_nursery", "햇빛 씨앗 묘상", 10f, 0.6f),
                new("dark_greenhouse", "빛 없는 어린 온실", 20f, 1.1f),
                new("vine_steps", "잠든 덩굴 계단", 30f, 0f),
                new("sleeping_gate", "잠든 문지기 화단", 40f, 1.4f),
                new("sun_archive", "저장 햇빛 보관소", 50f, 0f),
                new("rani_pot", "시간이 멈춘 작은 화분", 60f, 1.2f, true),
                new("shade_bloom", "그늘꽃 냉각실", 70f, 0f),
                new("waiting_terrace", "기다림의 세 계절", 80f, 1.5f),
                new("haoreum_bed", "해오름의 잠자리", 90f, 0f),
                new("dry_corridor", "말라붙은 불씨 회랑", 100f, 1.3f),
                new("tree_roots", "별길 나무 뿌리", 110f, 0f),
                new("tree_trunk", "자라나는 별가지", 120f, 1.5f),
                new("tree_canopy", "과성장 선택의 수관", 130f, 0f),
                new("sunflower_peak", "해바라기 꼭대기", 140f, 2.4f, true),
                new("departure", "북극성 관측소행 별가지", 152f, 0f)
            };
        }

        private static void CreateMainRoute(List<Room> rooms)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                Room room = rooms[i];
                Color floorColor = i < 5
                    ? new Color(0.22f, 0.42f, 0.27f)
                    : i < 10 ? new Color(0.32f, 0.34f, 0.28f) : new Color(0.38f, 0.25f, 0.22f);
                CreateCollisionPlatform($"GardenFloor_{room.id}", new Vector2(room.x, room.y - 2.2f),
                    new Vector2(9f, 0.8f), collisionRoot);
                SpriteBlock($"GardenFloorVisual_{room.id}", new Vector3(room.x, room.y - 2.2f, 0f),
                    new Vector2(9f, 0.8f), floorColor, 2, artRoot);
                string art = i % 3 == 0 ? "grass field.prefab" : i % 3 == 1 ? "Stones.prefab" : "Tree.prefab";
                InstantiateArt(Spring + art, $"GardenArt_{room.id}",
                    new Vector3(room.x, room.y - 1.8f, 1f), 0.7f, 4,
                    i < 10 ? new Color(0.68f, 0.82f, 0.55f) : new Color(0.76f, 0.52f, 0.36f));
                WorldText(room.label, new Vector3(room.x, room.y + 3.7f, 0f),
                    room.optional ? 0.82f : 0.95f,
                    room.optional ? new Color(1f, 0.55f, 0.25f) : new Color(1f, 0.83f, 0.42f),
                    60, labelRoot);
                CreateDiscovery($"sun-garden.{room.id}", room.label, new Vector2(room.x, room.y), room.optional);

                if (i >= rooms.Count - 1)
                {
                    continue;
                }
                Room next = rooms[i + 1];
                float midX = (room.x + next.x) * 0.5f;
                float midY = Mathf.Min(room.y, next.y) - 1.55f;
                CreateCollisionPlatform($"GardenConnector_{i:00}", new Vector2(midX, midY),
                    new Vector2(3.2f, 0.6f), collisionRoot);
                SpriteBlock($"GardenConnectorVisual_{i:00}", new Vector3(midX, midY, 0f),
                    new Vector2(3.2f, 0.6f), new Color(0.34f, 0.4f, 0.28f), 3, artRoot);
            }
        }

        private static void CreateGrowthBranches()
        {
            Vector2[] lowBranch =
            {
                new(24f, 5.5f), new(29f, 7.1f), new(35f, 8.2f), new(41f, 7.4f)
            };
            for (int i = 0; i < lowBranch.Length; i++)
            {
                CreateCollisionPlatform($"VineBranch_{i}", lowBranch[i], new Vector2(3.8f, 0.55f), collisionRoot);
                SpriteBlock($"VineBranchVisual_{i}", lowBranch[i], new Vector2(3.8f, 0.55f),
                    new Color(0.24f, 0.52f, 0.28f), 8, artRoot);
            }

            Vector2[] peakSteps =
            {
                new(130f, 4.6f), new(134f, 6.6f), new(138f, 8.7f), new(142f, 10.7f)
            };
            for (int i = 0; i < peakSteps.Length; i++)
            {
                CreateCollisionPlatform($"SunflowerPeakStep_{i}", peakSteps[i], new Vector2(3.2f, 0.5f), collisionRoot);
                InstantiateArt(Island + "platform wt.prefab", $"SunflowerIsland_{i}",
                    new Vector3(peakSteps[i].x, peakSteps[i].y - 0.2f, 0f),
                    0.35f, 12, new Color(1f, 0.68f, 0.3f));
            }
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = SpriteBlock("Player · 별을 줍는 아이", new Vector3(-2f, -1.4f, 0f),
                new Vector2(0.72f, 1.2f), new Color(1f, 0.78f, 0.3f), 44, world);
            player.layer = 31;
            GameObject scarf = SpriteBlock("RedThreadScarf", Vector3.zero, new Vector2(0.9f, 0.12f),
                new Color(0.95f, 0.12f, 0.32f), 46, player.transform);
            scarf.transform.localPosition = new Vector3(-0.5f, 0.05f, 0f);
            GameObject seed = SpriteBlock("SunSeedInHand", Vector3.zero, new Vector2(0.28f, 0.38f),
                new Color(1f, 0.82f, 0.22f), 47, player.transform);
            seed.transform.localPosition = new Vector3(0.52f, 0.05f, 0f);

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
            GameObject systems = new("@STAR NIGHT M3-4 · 길꽃과 빛");
            SunGardenChapterBootstrap bootstrap =
                systems.AddComponent<SunGardenChapterBootstrap>();
            bootstrap.ConfigureGateLoop(true);
            systems.AddComponent<StarNightCombinationResolver>();
            systems.AddComponent<ChapterPlaytestTelemetry>();

            GameObject hudObject = new("@HUD · 정원 열과 햇빛 씨앗");
            StarNightHUD hud = hudObject.AddComponent<StarNightHUD>();
            hud.SetFont(font);

            GameObject atmosphereObject = new("@ATMOSPHERE · 잠든 해의 숨");
            StarNightAtmosphere atmosphere = atmosphereObject.AddComponent<StarNightAtmosphere>();
            atmosphere.Configure(camera, player, square);
            atmosphere.SetWorldBounds(new Vector2(-14f, 168f), new Vector2(-11f, 20f), 196);

            GameObject maru = SpriteBlock("Maru · 밝은 곳을 보는 개", new Vector3(163f, 8f, 0f),
                new Vector2(1.6f, 1.15f), new Color(1f, 0.24f, 0.48f), 52, world);
            CreateEar(maru.transform, -0.42f);
            CreateEar(maru.transform, 0.42f);
            CircleCollider2D maruTargetCollider = maru.AddComponent<CircleCollider2D>();
            maruTargetCollider.isTrigger = true;
            maruTargetCollider.radius = 0.72f;
            FableObject maruFable = maru.AddComponent<FableObject>();
            maruFable.Configure("maru_sun_target", "눈을 뜬 마루", StarItemKind.General,
                FableTraits.LightReactive | FableTraits.Living | FableTraits.BrightSource, 4f);
            maruDirector = systems.AddComponent<MaruDirector>();
            maruDirector.Configure(maru.transform, new Vector3(163f, 8f, 0f));
            MaruSunTarget sunTarget = maru.AddComponent<MaruSunTarget>();
            sunTarget.Configure(maruDirector, 5.5f);

            GameObject firstBellTrace = SpriteBlock("Bell 1 · 출구 반대편을 보는 꽃들",
                new Vector3(116f, 4.9f, 0f), new Vector2(10f, 0.24f),
                new Color(0.72f, 0.9f, 0.38f, 0.7f), 32, world);
            firstBellTrace.SetActive(false);
            GameObject secondBellPresence = SpriteBlock("Bell 2 · 빛나는 씨앗을 찾는 마루의 눈",
                new Vector3(133f, 5.2f, 0f), new Vector2(7f, 0.34f),
                new Color(1f, 0.18f, 0.42f, 0.76f), 34, world);
            secondBellPresence.SetActive(false);
            GameObject gateClosingVisual = SpriteBlock("Bell 3 · 모든 광원을 잇는 눈길",
                new Vector3(148f, 1f, 0f), new Vector2(0.5f, 7f),
                new Color(1f, 0.2f, 0.35f, 0.85f), 40, world);
            gateClosingVisual.SetActive(false);
            BellChasePresenter presenter = systems.AddComponent<BellChasePresenter>();
            presenter.Configure(maruDirector, firstBellTrace, secondBellPresence, gateClosingVisual);
        }

        private static void CreateMandatoryCommandEcho()
        {
            GameObject pot = SpriteBlock("ReturnedSunSeedPot · 돌아온 작은 해씨",
                new Vector3(4f, -0.5f, 0f), new Vector2(0.8f, 0.72f),
                new Color(0.7f, 0.38f, 0.22f), 36, gameplayRoot);
            SpriteBlock("ReturnedSunSeed · 길을 잃었던 작은 해씨",
                new Vector3(4f, 0.1f, 0f), new Vector2(0.3f, 0.42f),
                new Color(1f, 0.75f, 0.2f), 40, pot.transform);
            GameObject echo = new("Mandatory Story · 모두 집으로 · 아무도 잃지 않게");
            echo.transform.SetParent(gameplayRoot);
            echo.AddComponent<SunGardenMaruCommandEcho>();
            WorldText("필수 장면 · 마루: “모두 집으로. 아무도 잃지 않게.”",
                new Vector3(6f, 3.2f, 0f), 0.82f,
                new Color(1f, 0.64f, 0.32f), 63, labelRoot);
        }

        private static void CreateStoredSunlight()
        {
            GameObject routeObject = new("Route A · 저장 햇빛 3곳에서 고른 빛");
            routeObject.transform.SetParent(gameplayRoot);
            GateRouteObjective routeObjective = routeObject.AddComponent<GateRouteObjective>();
            routeObjective.Configure("CH5_ROUTE_STORED_SUNLIGHT");
            storedLightRoute = routeObject.AddComponent<SunGardenStoredLightRoute>();
            storedLightRoute.Configure(routeObjective, 3);

            CreateSunSource("arrival_seed", "묘상에 남은 첫 햇빛",
                new Vector2(5f, -0.4f), 1, false, storedLightRoute);
            CreateSunSource("greenhouse_sun", "온실 유리의 작은 햇빛",
                new Vector2(22f, 0.1f), 1);
            CreateSunSource("gate_sun", "문지기 곁 저장 햇빛",
                new Vector2(43f, 0.5f), 1, false, storedLightRoute);
            CreateSunSource("archive_sun", "보관소의 병든 햇빛",
                new Vector2(53f, -0.4f), 1, false, storedLightRoute);
            CreateSunSource("terrace_sun", "기다림 끝에 고인 햇빛", new Vector2(82f, 0.4f), 1);
            CreateSunSource("root_sun", "별길 뿌리의 오래된 햇빛", new Vector2(111f, -0.5f), 1);
            CreateSunSource("rare_evolution_seed", "정원 진화용 희귀 햇빛 씨앗",
                new Vector2(143f, 11.7f), 1, true);

            GameObject greenhouseTop = InteractionBlock(
                "Route B · 온실 꼭대기 반사판에서 높은 빛",
                new Vector2(41f, 8.4f), new Color(1f, 0.58f, 0.15f), gameplayRoot);
            greenhouseTop.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
            GateRouteObjective highRoute = greenhouseTop.AddComponent<GateRouteObjective>();
            highRoute.Configure("CH5_ROUTE_GREENHOUSE_TOP");
            GreenhouseTopLightRoute highLight =
                greenhouseTop.AddComponent<GreenhouseTopLightRoute>();
            highLight.Configure(highRoute, 2);
            SunGrowthState mirrorVine = CreateGrowthTarget(
                    "greenhouse_mirror_vine", "반사판을 감싼 온실 덩굴",
                    new Vector2(36f, 7.6f), new Vector2(0.7f, 1.5f),
                    new Color(0.34f, 0.65f, 0.28f),
                    SunGrowthKind.GardenPlant, 2, 4, null, null, true)
                .GetComponent<SunGrowthState>();
            SunGrowthState glassMoth = CreateGrowthTarget(
                    "greenhouse_glass_moth", "온실 유리에 잠든 빛나방",
                    new Vector2(39f, 8.4f), new Vector2(0.9f, 0.65f),
                    new Color(0.62f, 0.68f, 0.88f),
                    SunGrowthKind.SleepingCreature, 2, 4)
                .GetComponent<SunGrowthState>();
            GameObject escapeBlocker = SpriteBlock(
                "GreenhouseEscapeBlocker · 과성장으로 막힌 귀환 발판",
                new Vector3(37.5f, 6.8f, 0f), new Vector2(0.7f, 4f),
                new Color(0.22f, 0.55f, 0.24f, 0.92f), 30, gameplayRoot);
            escapeBlocker.layer = 7;
            escapeBlocker.AddComponent<BoxCollider2D>();
            highLight.ConfigureHazards(mirrorVine, glassMoth, escapeBlocker);
            WorldText("B 위험·탐색 · 반사 2회로 높은 빛",
                new Vector3(40f, 10.8f, 0f), 0.8f,
                new Color(1f, 0.58f, 0.22f), 63, labelRoot);
        }

        private static void CreateGrowthLessons()
        {
            CreateGrowthTarget("sprout_platform", "잠든 발판 새싹", new Vector2(16f, -0.2f),
                new Vector2(0.7f, 1.1f), new Color(0.32f, 0.62f, 0.34f),
                SunGrowthKind.PlatformPlant, 2, 5,
                CreateGrowthPlatform("SproutPlatform", new Vector2(19f, 3.2f), new Vector2(4.8f, 0.55f)));

            GameObject gateBarrier = SpriteBlock("SleepingGateBarrier", new Vector3(44f, 0.5f, 0f),
                new Vector2(0.8f, 4.7f), new Color(0.25f, 0.3f, 0.35f), 26, gameplayRoot);
            BoxCollider2D barrierCollider = gateBarrier.AddComponent<BoxCollider2D>();
            gateBarrier.layer = 7;
            CreateGrowthTarget("sleeping_gatekeeper", "잠든 문지기 장치", new Vector2(40f, 0.4f),
                new Vector2(0.9f, 1.2f), new Color(0.4f, 0.52f, 0.58f),
                SunGrowthKind.SleepingCreature, 2, 5, null, gateBarrier);

            CreateGrowthTarget("sleeping_moth", "별빛을 먹는 잠든 나방", new Vector2(58f, 0.4f),
                new Vector2(1.1f, 0.7f), new Color(0.42f, 0.48f, 0.62f),
                SunGrowthKind.SleepingCreature, 2, 4);
            CreateGrowthTarget("shade_bloom", "빛을 나누는 그늘꽃", new Vector2(70f, -0.2f),
                new Vector2(0.8f, 1.1f), new Color(0.3f, 0.55f, 0.45f),
                SunGrowthKind.CoolingBloom, 2, 5);
            CreateGrowthTarget("dry_vine", "말라붙은 회랑 덩굴", new Vector2(101f, 0.4f),
                new Vector2(0.75f, 1.6f), new Color(0.45f, 0.32f, 0.24f),
                SunGrowthKind.GardenPlant, 2, 4, null, null, true);
            CreateGrowthTarget("sleeping_beetle", "잠든 정원 갑충", new Vector2(105f, 0.2f),
                new Vector2(1.15f, 0.7f), new Color(0.45f, 0.4f, 0.38f),
                SunGrowthKind.SleepingCreature, 2, 4);
        }

        private static void CreateHaoreumStory()
        {
            GameObject bed = SpriteBlock("HaoreumBed · 작은 해의 담요", new Vector3(90f, -0.1f, 0f),
                new Vector2(2.4f, 0.8f), new Color(0.42f, 0.35f, 0.58f), 34, gameplayRoot);
            GameObject sun = SpriteBlock("Haoreum · 잠든 작은 해", new Vector3(90f, 1f, 0f),
                new Vector2(1.35f, 1.35f), new Color(1f, 0.62f, 0.18f), 39, gameplayRoot);
            CircleCollider2D sunCollider = sun.AddComponent<CircleCollider2D>();
            sunCollider.isTrigger = true;
            sunCollider.radius = 0.75f;
            FableObject sunFable = sun.AddComponent<FableObject>();
            sunFable.Configure("haoreum", "잠든 작은 해 해오름", StarItemKind.ResidentProperty,
                FableTraits.LightReactive | FableTraits.Living | FableTraits.SleepingCreature |
                FableTraits.BrightSource, 4f);
            SunGrowthState sunGrowth = sun.AddComponent<SunGrowthState>();
            sunGrowth.Configure("haoreum", "작은 해 해오름", SunGrowthKind.SleepingCreature, 3, 6);

            GameObject bell = InteractionBlock("HaoreumBell · 강제 기상 종",
                new Vector2(94f, -0.2f), new Color(1f, 0.36f, 0.18f), gameplayRoot);
            GateRouteObjective wakeRoute = bell.AddComponent<GateRouteObjective>();
            wakeRoute.Configure("CH5_ROUTE_HAOREUM_WAKE");
            HaoreumDecision force = bell.AddComponent<HaoreumDecision>();
            force.Configure(starPathTree);
            force.ConfigureRouteObjective(wakeRoute);

            GameObject rest = InteractionBlock("GardenRestBench · 기다림의 돌의자",
                new Vector2(80f, -0.1f), new Color(0.42f, 0.58f, 0.72f), gameplayRoot);
            rest.AddComponent<GardenRestBench>().Configure(3);
            WorldText("C 빠름·개입 · 해오름 즉시 기상으로 해오름 빛", new Vector3(94f, 2.8f, 0f),
                0.82f, new Color(1f, 0.42f, 0.25f), 62, labelRoot);
            WorldText("느린 길: 세 번 기다리기 · 덩굴과 적도 성장", new Vector3(80f, 3.6f, 0f),
                0.78f, new Color(0.55f, 0.82f, 1f), 62, labelRoot);
        }

        private static void CreateStarPathTree()
        {
            GameObject stableRoute = CreateRouteGroup("StableStarPathRoute",
                new[] { new Vector2(121f, 4f), new Vector2(127f, 5.2f), new Vector2(133f, 6.2f) },
                new Color(0.55f, 0.9f, 0.45f));
            GameObject overgrownRoute = CreateRouteGroup("OvergrownStarPathShortcut",
                new[] { new Vector2(120f, 5.5f), new Vector2(127f, 8f), new Vector2(136f, 10f), new Vector2(146f, 8f) },
                new Color(0.3f, 0.72f, 0.28f));
            GameObject burnedRoute = CreateRouteGroup("BurnedStarPathRoute",
                new[] { new Vector2(122f, 2.8f), new Vector2(130f, 2.4f), new Vector2(139f, 2f) },
                new Color(0.26f, 0.16f, 0.12f));

            FableObject treeFable = CreateGrowthTarget("star_path_tree", "북극성 별길 나무",
                new Vector2(120f, 0.1f), new Vector2(1.7f, 3.4f),
                new Color(0.34f, 0.54f, 0.3f), SunGrowthKind.StarPathTree, 3, 5);
            treeFable.Configure("star_path_tree", "북극성 별길 나무", StarItemKind.DepartureSupply,
                FableTraits.LightReactive | FableTraits.Living | FableTraits.GrowthNode |
                FableTraits.StarPathTree | FableTraits.GardenPlant | FableTraits.BrightSource, 3f);
            SunGrowthState growth = treeFable.GetComponent<SunGrowthState>();
            starPathTree = treeFable.gameObject.AddComponent<StarPathTreeController>();
            starPathTree.Configure(growth, stableRoute, overgrownRoute, burnedRoute);

            HaoreumDecision pendingDecision = Object.FindFirstObjectByType<HaoreumDecision>();
            if (pendingDecision != null)
            {
                pendingDecision.Configure(starPathTree);
            }
            InstantiateArt(Spring + "Tree 2.prefab", "StarPathTreeFantasyArt",
                new Vector3(120f, 0.7f, 1f), 0.42f, 18, new Color(0.78f, 0.94f, 0.62f));
        }

        private static void CreateTreeDecisions()
        {
            GameObject stabilize = InteractionBlock("TreeStabilize · 가지 다듬기",
                new Vector2(127f, -0.2f), new Color(0.45f, 0.82f, 0.5f), gameplayRoot);
            stabilize.AddComponent<StarPathTreeDecision>()
                .Configure(StarPathTreeDecisionMode.Stabilize, starPathTree);
            GameObject overgrow = InteractionBlock("TreeOvergrow · 지름길 급성장",
                new Vector2(132f, -0.2f), new Color(1f, 0.44f, 0.22f), gameplayRoot);
            overgrow.AddComponent<StarPathTreeDecision>()
                .Configure(StarPathTreeDecisionMode.Overgrow, starPathTree);
        }

        private static void CreateRestorationAndTemptation()
        {
            GameObject altar = InteractionBlock("EvolutionAltar · 희귀 씨앗 포기",
                new Vector2(108f, -0.3f), new Color(0.38f, 0.86f, 0.7f), gameplayRoot);
            altar.AddComponent<GardenRestorationAltar>();
            GameObject pocketSun = InteractionBlock("PocketSun · 주머니 해님",
                new Vector2(146f, 10.9f), new Color(1f, 0.58f, 0.12f), gameplayRoot);
            pocketSun.AddComponent<PocketSunTemptation>();
            GameObject blocker = SpriteBlock("StoppedRoomBlocker · GateActive 전 봉오리",
                new Vector3(143.5f, 10.7f, 0f), new Vector2(0.75f, 4.4f),
                new Color(0.42f, 0.24f, 0.48f, 0.95f), 41, gameplayRoot);
            blocker.layer = 7;
            blocker.AddComponent<BoxCollider2D>();
            GameObject entrance = InteractionBlock(
                "SunflowerStoppedRoomEntrance · 해바라기 너머의 멈춘 방",
                new Vector2(141.5f, 9.6f), new Color(0.88f, 0.45f, 0.28f), gameplayRoot);
            entrance.AddComponent<SunflowerStoppedRoomTemptation>().Configure(blocker);
            InstantiateArt(Spring + "SunLight.prefab", "PocketSunBeam",
                new Vector3(146f, 12.5f, 0f), 0.7f, 24, new Color(1f, 0.55f, 0.18f));
            WorldText("GateActive 이후 선택 · 최초 명령 원본 + 최종전 빛 보조",
                new Vector3(146f, 14f, 0f), 0.76f,
                new Color(1f, 0.46f, 0.34f), 64, labelRoot);
        }

        private static void CreateRaniMemory()
        {
            GameObject pot = InteractionBlock("RaniSiblingPot · 멈춘 작은 화분",
                new Vector2(62f, 0.2f), new Color(0.55f, 0.48f, 0.78f), gameplayRoot);
            pot.transform.localScale = new Vector3(0.65f, 0.85f, 1f);
            pot.AddComponent<PreservedPotMemory>();
            WorldText("버리지도 새로 심지도 못한 꽃", new Vector3(62f, 2.8f, 0f),
                0.76f, new Color(0.75f, 0.68f, 1f), 62, labelRoot);
        }

        private static void CreateInheritedConsequences()
        {
            GameObject silence = SpriteBlock("RaniSilence · 꺼진 통신등", new Vector3(8f, 1.5f, 0f),
                new Vector2(0.35f, 0.35f), new Color(0.25f, 0.3f, 0.45f), 34, gameplayRoot);
            GameObject argument = SpriteBlock("ArgumentHeat · 열린 편지의 열", new Vector3(48f, 0.3f, 0f),
                new Vector2(2.4f, 0.45f), new Color(1f, 0.28f, 0.18f), 20, gameplayRoot);
            GameObject shortcut = CreateRouteGroup("TeleportCoreShortcut",
                new[] { new Vector2(54f, 4.8f), new Vector2(62f, 5.6f), new Vector2(70f, 4.8f) },
                new Color(0.65f, 0.42f, 1f));
            GameObject shade = SpriteBlock("SealedLetterShade · 봉인의 그늘", new Vector3(72f, 2.7f, 0f),
                new Vector2(6f, 0.5f), new Color(0.32f, 0.5f, 0.68f), 14, gameplayRoot);
            GameObject trail = SpriteBlock("MaruLetterTrail · 편지 냄새", new Vector3(98f, 1f, 0f),
                new Vector2(5f, 0.18f), new Color(1f, 0.2f, 0.45f), 15, gameplayRoot);
            GameObject debris = SpriteBlock("SorterDebris · 마른 배송물", new Vector3(103f, -0.2f, 0f),
                new Vector2(3.2f, 0.5f), new Color(0.58f, 0.34f, 0.2f), 16, gameplayRoot);

            GameObject display = new("@P3 INHERITANCE · 편지와 배송의 흔적");
            SunGardenInheritedDisplay inherited = display.AddComponent<SunGardenInheritedDisplay>();
            inherited.Configure(silence, argument, shortcut, shade, trail, debris);
        }

        private static void CreateDeparture()
        {
            InstantiateArt(Crystal + "Crystal platform B.prefab", "PolarisObservationBranch",
                new Vector3(153f, -1.2f, 1f), 0.7f, 18, new Color(0.72f, 0.82f, 1f));
            GameObject gate = InteractionBlock("DepartureGate · 북극성 관측소행 별가지",
                new Vector2(153f, -0.2f), new Color(0.7f, 0.82f, 1f), gameplayRoot);
            gate.transform.localScale = new Vector3(1.2f, 1.8f, 1f);
            gate.AddComponent<SunGardenDepartureGate>();

            GameObject starGate = InteractionBlock("StarGateHub · 길꽃 별문",
                new Vector2(128f, -0.2f), new Color(0.55f, 0.96f, 0.48f), gameplayRoot);
            starGate.transform.localScale = new Vector3(1.4f, 1.8f, 1f);
            starGate.AddComponent<StarGateController>();
            TMP_Text status = WorldText("길꽃 별문 · 길꽃 0/2",
                new Vector3(128f, 3.2f, 0f), 1.45f,
                new Color(0.62f, 1f, 0.48f), 63, labelRoot);
            starGate.AddComponent<StarGateWorldStatus>().Configure(
                status, "길꽃 별문", "길꽃");

            GameObject firstBloom = SpriteBlock("GateBloom_1 · 첫 길꽃",
                new Vector3(125.5f, 1f, 0f), new Vector2(0.9f, 1.2f),
                new Color(0.5f, 1f, 0.48f), 45, gameplayRoot);
            firstBloom.SetActive(false);
            GameObject secondBloom = SpriteBlock("GateBloom_2 · 둘째 길꽃",
                new Vector3(130.5f, 1f, 0f), new Vector2(0.9f, 1.2f),
                new Color(1f, 0.82f, 0.3f), 45, gameplayRoot);
            secondBloom.SetActive(false);
            starGate.AddComponent<SunGardenGateBloomPresenter>().Configure(firstBloom, secondBloom);
            WorldText("길꽃 2/2 뒤 다시 상호작용 · 첫 방울과 추격 시작",
                new Vector3(135f, 4.1f, 0f), 0.76f,
                new Color(0.72f, 1f, 0.58f), 63, labelRoot);
        }

        private static void CreateCheckpoints()
        {
            Vector2[] points =
            {
                new(2f, -1.4f), new(36f, -0.8f), new(72f, -1.2f),
                new(106f, -0.2f), new(142f, 1f)
            };
            for (int i = 0; i < points.Length; i++)
            {
                GameObject lamp = SpriteBlock($"SunGardenLantern_{i + 1}", points[i],
                    new Vector2(0.42f, 0.72f), new Color(1f, 0.66f, 0.24f), 38, gameplayRoot);
                CircleCollider2D trigger = lamp.AddComponent<CircleCollider2D>();
                trigger.isTrigger = true;
                trigger.radius = 1.15f;
                StarNightCheckpoint checkpoint = lamp.AddComponent<StarNightCheckpoint>();
                checkpoint.Configure($"정원 햇등 {i + 1}");
            }
        }

        private static void CreateGuide()
        {
            WorldText("제5장 · 잠든 해님의 정원", new Vector3(2f, 4.7f, 0f),
                1.75f, new Color(1f, 0.78f, 0.3f), 64, labelRoot);
            WorldText("빛을 받은 것은 깨어나고 자란다 · 너무 많은 빛은 말려 태운다",
                new Vector3(18f, 4.6f, 0f), 1.02f, new Color(0.82f, 1f, 0.58f), 64, labelRoot);
            WorldText("R 도구 전환 · X 저장 햇빛 수집 · E 햇빛 씨앗 심기",
                new Vector3(18f, 3.7f, 0f), 0.88f, Color.white, 64, labelRoot);
            WorldText("A 저장 햇빛 3곳 · B 온실 꼭대기 반사 · C 해오름 강제 기상 중 2개",
                new Vector3(89f, 6.8f, 0f), 0.86f, new Color(1f, 0.62f, 0.28f), 64, labelRoot);
            WorldText("길꽃은 자동 출항하지 않는다 · 별문에 2개를 심고 손잡이를 다시 당기기",
                new Vector3(128f, 6.8f, 0f), 0.82f, new Color(0.62f, 1f, 0.52f), 64, labelRoot);
        }

        private static void CreateWorldBounds()
        {
            CreateCollisionPlatform("FallCatch", new Vector2(78f, -10.5f), new Vector2(184f, 0.8f), collisionRoot);
            CreateCollisionPlatform("LeftBoundary", new Vector2(-10f, 2f), new Vector2(0.8f, 30f), collisionRoot);
            CreateCollisionPlatform("RightBoundary", new Vector2(166f, 2f), new Vector2(0.8f, 30f), collisionRoot);
        }

        private static FableObject CreateSunSource(string id, string label, Vector2 position,
            int amount, bool rare = false, SunGardenStoredLightRoute route = null)
        {
            GameObject source = SpriteBlock($"{label} [{id}]", position,
                rare ? new Vector2(0.58f, 0.72f) : new Vector2(0.42f, 0.58f),
                rare ? new Color(0.48f, 1f, 0.82f) : new Color(1f, 0.76f, 0.24f),
                40, gameplayRoot);
            CircleCollider2D trigger = source.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.15f;
            FableObject fable = source.AddComponent<FableObject>();
            fable.Configure(id, label, rare ? StarItemKind.RareToy : StarItemKind.DepartureSupply,
                FableTraits.SunlightSource | FableTraits.BrightSource, rare ? 3f : 1f);
            StoredSunlightSource stored = source.AddComponent<StoredSunlightSource>();
            stored.Configure(id, label, amount, rare);
            stored.ConfigureRoute(route);
            return fable;
        }

        private static FableObject CreateGrowthTarget(string id, string label, Vector2 position,
            Vector2 size, Color color, SunGrowthKind kind, int bloomAt, int burnAt,
            GameObject controlled = null, GameObject barrier = null, bool flammable = false)
        {
            GameObject target = SpriteBlock($"{label} [{id}]", position, size, color, 38, gameplayRoot);
            CircleCollider2D trigger = target.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.2f;
            FableTraits traits = FableTraits.LightReactive | FableTraits.GrowthNode;
            traits |= kind == SunGrowthKind.SleepingCreature
                ? FableTraits.Living | FableTraits.SleepingCreature
                : FableTraits.Living | FableTraits.GardenPlant;
            if (kind == SunGrowthKind.StarPathTree)
            {
                traits |= FableTraits.StarPathTree;
            }
            if (flammable)
            {
                traits |= FableTraits.Flammable;
            }
            FableObject fable = target.AddComponent<FableObject>();
            fable.Configure(id, label, StarItemKind.General, traits, flammable ? 2f : 1.2f);
            SunGrowthState growth = target.AddComponent<SunGrowthState>();
            growth.Configure(id, label, kind, bloomAt, burnAt, controlled, barrier);
            return fable;
        }

        private static GameObject CreateGrowthPlatform(string name, Vector2 position, Vector2 size)
        {
            GameObject root = new(name);
            root.transform.SetParent(gameplayRoot);
            GameObject visual = SpriteBlock($"{name}Visual", position, size,
                new Color(0.42f, 0.78f, 0.36f), 19, root.transform);
            GameObject collision = new($"{name}Collision");
            collision.transform.SetParent(root.transform);
            collision.transform.position = position;
            collision.layer = 7;
            BoxCollider2D collider = collision.AddComponent<BoxCollider2D>();
            collider.size = size;
            root.SetActive(false);
            return root;
        }

        private static GameObject CreateRouteGroup(string name, IEnumerable<Vector2> points, Color color)
        {
            GameObject group = new(name);
            group.transform.SetParent(gameplayRoot);
            int index = 0;
            foreach (Vector2 point in points)
            {
                SpriteBlock($"{name}Visual_{index}", point, new Vector2(4.4f, 0.5f),
                    color, 21, group.transform);
                GameObject collision = new($"{name}Collision_{index}");
                collision.transform.SetParent(group.transform);
                collision.transform.position = point;
                collision.layer = 7;
                BoxCollider2D collider = collision.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(4.4f, 0.5f);
                index++;
            }
            group.SetActive(false);
            return group;
        }

        private static GameObject InteractionBlock(string name, Vector2 position, Color color, Transform parent)
        {
            GameObject block = SpriteBlock(name, position, new Vector2(0.8f, 1f), color, 42, parent);
            CircleCollider2D trigger = block.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.25f;
            return block;
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
            tmp.rectTransform.sizeDelta = new Vector2(13f, 2f);
            return tmp;
        }

        private static GameObject InstantiateArt(string path, string name, Vector3 position, float scale,
            int sortingOffset, Color tint, Transform parent = null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[Star Night P4] Missing bundle art: {path}");
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
                new Color(1f, 0.24f, 0.48f), 53, parent);
            ear.transform.localPosition = new Vector3(x, 0.66f, 0f);
            ear.transform.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? -18f : 18f);
        }

        private static void AddToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(scene => scene.path == path);
            int p3Index = scenes.FindIndex(scene =>
                scene.path == "Assets/Scenes/StarNight/StarNight_StarPostOffice.unity");
            int insert = p3Index >= 0 ? p3Index + 1 : scenes.Count;
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
