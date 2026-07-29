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
    public static class StarNightM5PolarisBuilder
    {
        private const string SceneFolder = "Assets/Scenes/StarNight";
        private const string ScenePath = SceneFolder + "/StarNight_PolarisObservatory.unity";
        private const string SquarePath = "Assets/Resources/Sprites/Square.png";
        private const string FontPath = "Assets/TextMesh Pro/Fonts/NeoDunggeunmoPro-Regular.asset";
        private const string Station = "Assets/2D Fantasy sprite bundle/Abandoned station/Prefabs/";
        private const string Crystal = "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/Crystal prefabs/";
        private const string Island = "Assets/2D Fantasy sprite bundle/Island pack/Prefabs/";
        private const string Spring = "Assets/2D Fantasy sprite bundle/Spring forest/Prefabs/";

        private static Sprite square;
        private static TMP_FontAsset font;
        private static Transform world;
        private static Transform artRoot;
        private static Transform collisionRoot;
        private static Transform gameplayRoot;
        private static Transform labelRoot;

        [MenuItem("Tools/Star Night/Build M5 Polaris Observatory")]
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
                Debug.LogError("[Star Night M5] Square sprite or Korean TMP font is missing.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "StarNight_PolarisObservatory";
            world = new GameObject("WORLD · 북극성 관측소").transform;
            artRoot = ChildRoot("ART · 2D Fantasy Polaris", world);
            collisionRoot = ChildRoot("COLLISION · Final Route", world);
            gameplayRoot = ChildRoot("GAMEPLAY · Four Endings", world);
            labelRoot = ChildRoot("ROOM TITLES", world);

            Camera camera = CreateCamera();
            CreateMainLight();
            CreateBackdrop();
            CreateMainRoute();
            CreateRecordCorridor();
            CreateObservatory();
            CreateRestorationRun();
            SpriteRenderer centerStar = CreateCenterStar();
            Transform maru = CreateMaru();
            CreateEndingHall();
            CreateInheritedResults();
            GameObject player = CreatePlayer();
            CreateSystems(camera, player.transform, maru, centerStar);
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
            Debug.Log("[Star Night M5] Polaris Observatory built: 23 rooms, 5 record echoes, 5 tool restorations, center-star pursuit, 4 endings.");
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
            camera.orthographicSize = 6.9f;
            camera.backgroundColor = new Color(0.008f, 0.012f, 0.045f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateMainLight()
        {
            GameObject lightObject = new("Polaris Light · Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.72f, 0.84f, 1f);
            light.intensity = 0.78f;
            lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
        }

        private static void CreateBackdrop()
        {
            for (int i = 0; i < 5; i++)
            {
                InstantiateArt(Crystal + "Background blur.prefab", $"DeepConstellation_{i}",
                    new Vector3(i * 46f, -1f, 7f), 2.15f, -55,
                    i < 3 ? new Color(0.32f, 0.45f, 0.88f) : new Color(0.6f, 0.32f, 0.78f));
                InstantiateArt(Crystal + "Stars Particle.prefab", $"ArchiveStars_{i}",
                    new Vector3(i * 45f + 10f, 4f, 1f), 1.3f, -28,
                    new Color(1f, 0.72f, 0.34f));
            }
            InstantiateArt(Island + "Wind.prefab", "CollapsingStarCurrent",
                new Vector3(150f, 3f, 2f), 1.8f, -18, new Color(0.62f, 0.82f, 1f));
            InstantiateArt(Spring + "SunLight.prefab", "PolarisCoreLight",
                new Vector3(171f, 3f, 3f), 1.25f, -10, new Color(1f, 0.62f, 0.28f));
        }

        private static void CreateMainRoute()
        {
            string[] rooms =
            {
                "다섯 도장의 입구", "달의 기록", "까치의 기록", "구름의 기록", "우편의 기록",
                "정원의 기록", "라니의 별자리", "닫힌 관측실", "최초 임무 기록", "중심별 추격 시작",
                "깨진 별 복원대", "늘어난 파편로", "별자리 연결대", "흔들리는 실 통로",
                "부유 천구", "역풍의 돔", "반송 항로", "주소가 바뀌는 복도",
                "재점화 정원", "무너지는 중심별", "중심별 선점대", "네 갈래 선택실", "여행의 끝"
            };

            for (int i = 0; i < rooms.Length; i++)
            {
                float x = i * 9f;
                float y = i is 6 or 7 or 8 ? 1.2f : i is 13 or 15 or 17 ? 1.5f : 0f;
                float floorY = y - 2.65f;
                CreateCollisionPlatform($"Floor_{i:00}", new Vector2(x, floorY),
                    new Vector2(7.7f, 0.58f), collisionRoot);
                string art = i % 4 == 0 ? "Platform with ropes.prefab" :
                    i % 2 == 0 ? "Platform B.prefab" : "Platform A.prefab";
                InstantiateArt(Station + art, $"ObservatoryFloor_{i:00}",
                    new Vector3(x, floorY + 0.25f, 0f), 0.62f, -6,
                    i < 9 ? new Color(0.58f, 0.76f, 1f) :
                    i < 20 ? new Color(0.78f, 0.5f, 1f) : new Color(1f, 0.62f, 0.34f));
                WorldText(rooms[i], new Vector3(x, y + 2.7f, 0f), 1.15f,
                    i < 9 ? new Color(0.65f, 0.86f, 1f) :
                    i < 20 ? new Color(0.9f, 0.6f, 1f) : new Color(1f, 0.78f, 0.3f), 55, labelRoot);

                if (i < rooms.Length - 1)
                {
                    float nextY = (i + 1) is 6 or 7 or 8 ? 1.2f :
                        (i + 1) is 13 or 15 or 17 ? 1.5f : 0f;
                    CreateCollisionPlatform($"Connector_{i:00}",
                        new Vector2(x + 4.5f, Mathf.Min(floorY, nextY - 2.65f) + 0.32f),
                        new Vector2(2.1f, 0.42f), collisionRoot);
                }

                if (i % 2 == 0)
                {
                    InstantiateArt(Station + "Lamp.prefab", $"ArchiveLamp_{i:00}",
                        new Vector3(x + 2.3f, floorY + 1.15f, 0f), 0.4f, 18,
                        i < 9 ? new Color(0.42f, 0.82f, 1f) : new Color(1f, 0.42f, 0.68f));
                }
            }
        }

        private static void CreateRecordCorridor()
        {
            (StarChapterId chapter, float x, Color color)[] records =
            {
                (StarChapterId.MoonRabbitMill, 9f, new Color(1f, 0.78f, 0.28f)),
                (StarChapterId.MagpieBridge, 18f, new Color(1f, 0.3f, 0.48f)),
                (StarChapterId.CloudWhaleRanch, 27f, new Color(0.42f, 0.82f, 1f)),
                (StarChapterId.StarPostOffice, 36f, new Color(0.72f, 0.5f, 1f)),
                (StarChapterId.SleepingSunGarden, 45f, new Color(0.55f, 1f, 0.55f))
            };

            foreach ((StarChapterId chapter, float x, Color color) in records)
            {
                GameObject echo = InteractionBlock($"RecordEcho · {chapter}", new Vector2(x, -0.8f),
                    color, new Vector2(0.9f, 1.25f));
                GameObject star = SpriteBlock($"Constellation · {chapter}", new Vector3(x, 4.2f, 0f),
                    new Vector2(0.72f, 0.72f), new Color(0.25f, 0.34f, 0.58f), 42, gameplayRoot);
                echo.AddComponent<PolarisRecordEcho>().Configure(chapter, star.GetComponent<SpriteRenderer>());

                for (int ray = 0; ray < 3; ray++)
                {
                    GameObject line = SpriteBlock($"RecordLine_{chapter}_{ray}", Vector3.zero,
                        new Vector2(0.09f, 1.6f + ray * 0.25f), new Color(color.r, color.g, color.b, 0.42f),
                        37, star.transform);
                    line.transform.localRotation = Quaternion.Euler(0f, 0f, ray * 58f - 50f);
                }
            }

            WorldText("행동은 지울 수 없다. 대신 수습과 문맥을 함께 남길 수 있다.",
                new Vector3(27f, 6.4f, 0f), 1.35f, new Color(0.72f, 0.86f, 1f), 59, labelRoot);
        }

        private static void CreateObservatory()
        {
            GameObject archive = InteractionBlock("TruthArchive · 닫힌 관측실", new Vector2(66f, -0.2f),
                new Color(0.86f, 0.48f, 1f), new Vector2(1.35f, 1.9f));
            archive.AddComponent<PolarisTruthArchive>();
            InstantiateArt(Crystal + "Crystal platform B.prefab", "ClosedObservatoryDais",
                new Vector3(66f, -1.05f, 0f), 0.76f, 16, new Color(0.62f, 0.72f, 1f));

            WorldText("라니의 평가 별자리", new Vector3(57f, 6.1f, 0f), 1.65f,
                new Color(1f, 0.58f, 0.76f), 60, labelRoot);
            WorldText("“왜 붙잡았는지는 이해해요. 하지만 놓아주는 말은 당신이 해야 해요.”",
                new Vector3(66f, 5f, 0f), 1.15f, new Color(1f, 0.78f, 0.3f), 59, labelRoot);
        }

        private static void CreateRestorationRun()
        {
            (FableVerb verb, float x, Color color, string label)[] nodes =
            {
                (FableVerb.Resize, 90f, new Color(1f, 0.72f, 0.25f), "깨진 별을 원래 크기로"),
                (FableVerb.Link, 108f, new Color(1f, 0.25f, 0.48f), "별자리를 하나의 길로"),
                (FableVerb.Float, 126f, new Color(0.35f, 0.82f, 1f), "무거워진 별을 하늘로"),
                (FableVerb.Deliver, 144f, new Color(0.72f, 0.48f, 1f), "길 잃은 별을 원래 행성으로"),
                (FableVerb.Awaken, 162f, new Color(0.52f, 1f, 0.55f), "식은 중심별을 다시 점화")
            };

            for (int i = 0; i < nodes.Length; i++)
            {
                (FableVerb verb, float x, Color color, string label) = nodes[i];
                GameObject node = InteractionBlock($"FinalToolNode {i + 1} · {verb}",
                    new Vector2(x, -0.6f), color, new Vector2(1.05f, 1.55f));
                GameObject marker = SpriteBlock($"RestorationStar {i + 1}", new Vector3(x, 3.8f, 0f),
                    new Vector2(0.82f, 0.82f), new Color(color.r * 0.35f, color.g * 0.35f, color.b * 0.35f),
                    43, gameplayRoot);
                node.AddComponent<PolarisFinalToolNode>().Configure(verb, marker.GetComponent<SpriteRenderer>());
                WorldText($"{i + 1}. {PolarisFinaleState.VerbDisplayName(verb)} · {label}",
                    new Vector3(x, 5.2f, 0f), 1.05f, color, 58, labelRoot);
            }

            for (int i = 0; i < 8; i++)
            {
                float x = 84f + i * 10f;
                GameObject shard = SpriteBlock($"FallingStarShard_{i}", new Vector3(x, 1.6f + i % 3, 0f),
                    new Vector2(0.34f + (i % 2) * 0.18f, 0.34f), new Color(1f, 0.38f, 0.62f, 0.75f),
                    34, gameplayRoot);
                shard.transform.rotation = Quaternion.Euler(0f, 0f, i * 23f);
            }
        }

        private static SpriteRenderer CreateCenterStar()
        {
            GameObject center = SpriteBlock("PolarisCore · 중심별", new Vector3(180f, 2.4f, 0f),
                new Vector2(2.1f, 2.1f), new Color(1f, 0.78f, 0.25f), 52, gameplayRoot);
            for (int i = 0; i < 8; i++)
            {
                GameObject ray = SpriteBlock($"CoreRay_{i}", Vector3.zero, new Vector2(0.13f, 2.8f),
                    new Color(1f, 0.62f, 0.25f, 0.58f), 50, center.transform);
                ray.transform.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
            }
            WorldText("마루보다 먼저 중심별에 도달했다", new Vector3(180f, 6.1f, 0f), 1.7f,
                new Color(1f, 0.78f, 0.3f), 61, labelRoot);
            return center.GetComponent<SpriteRenderer>();
        }

        private static Transform CreateMaru()
        {
            GameObject maru = SpriteBlock("Maru · 중심별을 향하는 개", new Vector3(198f, 4.2f, 0f),
                new Vector2(2f, 1.45f), new Color(1f, 0.2f, 0.46f), 54, gameplayRoot);
            CreateEar(maru.transform, -0.52f);
            CreateEar(maru.transform, 0.52f);
            GameObject coreScent = SpriteBlock("PolarisScent", Vector3.zero, new Vector2(1.25f, 0.18f),
                new Color(1f, 0.72f, 0.25f), 55, maru.transform);
            coreScent.transform.localPosition = new Vector3(1.1f, -0.1f, 0f);
            return maru.transform;
        }

        private static void CreateEndingHall()
        {
            (PolarisEndingType ending, float x, Color color, string subtitle)[] endings =
            {
                (PolarisEndingType.PathCutter, 183f, new Color(1f, 0.38f, 0.34f), "마루를 없애고 항로 복구"),
                (PolarisEndingType.NewLeash, 189f, new Color(1f, 0.26f, 0.54f), "안전을 위해 새 명령자가 됨"),
                (PolarisEndingType.ClosedUniverse, 195f, new Color(0.46f, 0.55f, 0.72f), "아무도 떠나지 않는 안전"),
                (PolarisEndingType.StarRoad, 201f, new Color(0.42f, 1f, 0.78f), "라니가 직접 명령을 거둠")
            };

            foreach ((PolarisEndingType ending, float x, Color color, string subtitle) in endings)
            {
                GameObject choice = InteractionBlock($"EndingChoice · {ending}", new Vector2(x, -0.6f),
                    color, new Vector2(1.25f, 1.65f));
                choice.AddComponent<PolarisEndingChoice>().Configure(ending);
                WorldText(PolarisFinaleState.EndingTitle(ending), new Vector3(x, 3.6f, 0f), 1.25f,
                    color, 61, labelRoot);
                WorldText(subtitle, new Vector3(x, 2.7f, 0f), 0.82f,
                    new Color(0.86f, 0.9f, 1f), 60, labelRoot);
            }
        }

        private static void CreateInheritedResults()
        {
            GameObject root = new("INHERITED · Garden Consequences");
            root.transform.SetParent(gameplayRoot);
            GameObject stableSun = ResultVisual("StableSun · 안정 광원", new Vector3(76f, 3f, 0f),
                new Vector2(2.8f, 0.24f), new Color(0.55f, 1f, 0.62f), root.transform);
            GameObject tiredSun = ResultVisual("TiredSun · 과열 주기", new Vector3(76f, 3f, 0f),
                new Vector2(2.8f, 0.24f), new Color(1f, 0.32f, 0.28f), root.transform);
            GameObject fireDamage = ResultVisual("FireDamage · 불탄 기록", new Vector3(72f, 5.7f, 0f),
                new Vector2(5f, 0.18f), new Color(0.35f, 0.1f, 0.12f), root.transform);
            GameObject restoredPot = ResultVisual("RestoredPot · 되살아난 화분", new Vector3(73f, -0.4f, 0f),
                new Vector2(0.75f, 0.9f), new Color(0.45f, 1f, 0.58f), root.transform);
            GameObject stableRoute = ResultVisual("StableRoute · 다듬은 별가지", new Vector3(82f, 1f, 0f),
                new Vector2(7f, 0.18f), new Color(0.48f, 0.9f, 1f), root.transform);
            GameObject overgrownRoute = ResultVisual("OvergrownRoute · 불안정 지름길", new Vector3(82f, 1f, 0f),
                new Vector2(7f, 0.32f), new Color(0.72f, 0.38f, 1f), root.transform);
            GameObject burnedRoute = ResultVisual("BurnedRoute · 끊긴 별가지", new Vector3(82f, 1f, 0f),
                new Vector2(7f, 0.12f), new Color(0.24f, 0.1f, 0.14f), root.transform);

            stableSun.SetActive(false);
            tiredSun.SetActive(false);
            fireDamage.SetActive(false);
            restoredPot.SetActive(false);
            stableRoute.SetActive(false);
            overgrownRoute.SetActive(false);
            burnedRoute.SetActive(false);
            root.AddComponent<PolarisInheritedDisplay>().Configure(stableSun, tiredSun, fireDamage,
                restoredPot, stableRoute, overgrownRoute, burnedRoute);
        }

        private static GameObject ResultVisual(string name, Vector3 position, Vector2 size, Color color, Transform parent)
        {
            return SpriteBlock(name, position, size, color, 31, parent);
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = SpriteBlock("Player · 별을 줍는 아이", new Vector3(-2f, -1.45f, 0f),
                new Vector2(0.72f, 1.2f), new Color(1f, 0.78f, 0.3f), 45, world);
            player.layer = 31;
            GameObject scarf = SpriteBlock("FiveGateScarf", Vector3.zero, new Vector2(0.95f, 0.12f),
                new Color(0.95f, 0.15f, 0.36f), 47, player.transform);
            scarf.transform.localPosition = new Vector3(-0.52f, 0.05f, 0f);
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

        private static void CreateSystems(Camera camera, Transform player, Transform maru, SpriteRenderer centerStar)
        {
            GameObject systems = new("@STAR NIGHT M5 · 북극성 최종전");
            systems.AddComponent<PolarisChapterBootstrap>();
            systems.AddComponent<StarNightCombinationResolver>();

            GameObject hudObject = new("@HUD · 라니의 최종 기록");
            StarNightHUD hud = hudObject.AddComponent<StarNightHUD>();
            hud.SetFont(font);

            GameObject finaleHudObject = new("@HUD · 중심별 추격");
            PolarisFinalePresenter finaleHud = finaleHudObject.AddComponent<PolarisFinalePresenter>();
            finaleHud.SetFont(font);

            GameObject atmosphereObject = new("@ATMOSPHERE · 무너지는 별자리");
            StarNightAtmosphere atmosphere = atmosphereObject.AddComponent<StarNightAtmosphere>();
            atmosphere.Configure(camera, player, square);
            atmosphere.SetWorldBounds(new Vector2(-12f, 215f), new Vector2(-11f, 19f), 210);

            PolarisFinaleWorldPresenter worldPresenter = systems.AddComponent<PolarisFinaleWorldPresenter>();
            worldPresenter.Configure(maru, centerStar, new Vector3(198f, 4.2f, 0f), new Vector3(180f, 2.4f, 0f));
        }

        private static void CreateCheckpoints()
        {
            (string label, Vector2 position)[] checkpoints =
            {
                ("다섯 도장 등불", new Vector2(1f, -1f)),
                ("기록 회랑 등불", new Vector2(45f, -1f)),
                ("관측실 등불", new Vector2(69f, 0f)),
                ("도구 회랑 등불", new Vector2(108f, -1f)),
                ("재점화 등불", new Vector2(159f, -1f)),
                ("중심별 등불", new Vector2(179f, -1f))
            };
            foreach ((string label, Vector2 position) in checkpoints)
            {
                GameObject checkpoint = SpriteBlock($"Checkpoint · {label}", position,
                    new Vector2(0.38f, 1.05f), new Color(1f, 0.72f, 0.28f), 39, gameplayRoot);
                checkpoint.AddComponent<BoxCollider2D>().isTrigger = true;
                checkpoint.AddComponent<StarNightCheckpoint>().Configure(label);
                InstantiateArt(Station + "Lamp.prefab", $"{label} Art", position, 0.38f, 24,
                    new Color(0.62f, 0.82f, 1f));
            }
        }

        private static void CreateGuide()
        {
            WorldText("제6장 · 북극성 관측소", new Vector3(0f, 7.2f, 0f), 2.55f,
                new Color(1f, 0.78f, 0.3f), 62, labelRoot);
            WorldText("최종 목표 · 마루보다 먼저 중심별에 도달하고 우주의 길을 결정하자",
                new Vector3(0f, 6.05f, 0f), 1.35f, new Color(0.72f, 0.86f, 1f), 61, labelRoot);
            WorldText("다섯 기록 확인 · R 도구 순환 · X 복구/선택 · T 여행 티켓",
                new Vector3(0f, 5f, 0f), 1.1f, new Color(0.9f, 0.92f, 1f), 60, labelRoot);
        }

        private static GameObject InteractionBlock(string name, Vector2 position, Color color, Vector2 size)
        {
            GameObject station = SpriteBlock(name, position, size, color, 44, gameplayRoot);
            CircleCollider2D trigger = station.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.35f;
            return station;
        }

        private static void CreateWorldBounds()
        {
            CreateCollisionPlatform("WorldBottom", new Vector2(101f, -12.6f), new Vector2(220f, 1f), collisionRoot);
            CreateCollisionPlatform("LeftBoundary", new Vector2(-7f, 2f), new Vector2(0.6f, 32f), collisionRoot);
            CreateCollisionPlatform("RightBoundary", new Vector2(209f, 2f), new Vector2(0.6f, 32f), collisionRoot);
        }

        private static GameObject CreateCollisionPlatform(string name, Vector2 position, Vector2 size, Transform parent)
        {
            GameObject platform = new(name);
            platform.transform.SetParent(parent);
            platform.transform.position = position;
            platform.layer = 7;
            platform.AddComponent<BoxCollider2D>().size = size;
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
            tmp.rectTransform.sizeDelta = new Vector2(13f, 2f);
            return tmp;
        }

        private static GameObject InstantiateArt(string path, string name, Vector3 position, float scale,
            int sortingOffset, Color tint)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[Star Night M5] Missing bundle art: {path}");
                GameObject missing = new($"MISSING · {name}");
                missing.transform.SetParent(artRoot);
                return missing;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = name;
            instance.transform.SetParent(artRoot, true);
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
            GameObject ear = SpriteBlock("Ear", Vector3.zero, new Vector2(0.4f, 0.72f),
                new Color(1f, 0.24f, 0.48f), 56, parent);
            ear.transform.localPosition = new Vector3(x, 0.74f, 0f);
            ear.transform.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? -18f : 18f);
        }

        private static void AddToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(scene => scene.path != path)
                .ToList();
            int gardenIndex = scenes.FindIndex(scene =>
                scene.path == "Assets/Scenes/StarNight/StarNight_SleepingSunGarden.unity");
            int insert = gardenIndex >= 0 ? gardenIndex + 1 : scenes.Count;
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
