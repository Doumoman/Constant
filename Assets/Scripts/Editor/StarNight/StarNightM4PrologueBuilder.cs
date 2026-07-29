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
    public static class StarNightM4PrologueBuilder
    {
        private const string SceneFolder = "Assets/Scenes/StarNight";
        private const string ScenePath = SceneFolder + "/StarNight_Prologue.unity";
        private const string SquarePath = "Assets/Resources/Sprites/Square.png";
        private const string FontPath = "Assets/TextMesh Pro/Fonts/NeoDunggeunmoPro-Regular.asset";
        private const string Forest = "Assets/2D Fantasy sprite bundle/Forest  V2.0/Prefabs/";
        private const string Station = "Assets/2D Fantasy sprite bundle/Abandoned station/Prefabs/";
        private const string Crystal = "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/Crystal prefabs/";
        private const string Island = "Assets/2D Fantasy sprite bundle/Island pack/Prefabs/";

        private static Sprite square;
        private static TMP_FontAsset font;
        private static Transform world;
        private static Transform artRoot;
        private static Transform collisionRoot;
        private static Transform gameplayRoot;
        private static Transform labelRoot;

        [MenuItem("Tools/Star Night/Build M4 Prologue")]
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
                Debug.LogError("[Star Night M4] Square sprite or Korean TMP font is missing.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "StarNight_Prologue";
            world = new GameObject("WORLD · 별길을 잃은 밤").transform;
            artRoot = ChildRoot("ART · 2D Fantasy Moon Port", world);
            collisionRoot = ChildRoot("COLLISION · Prologue Route", world);
            gameplayRoot = ChildRoot("GAMEPLAY · Prologue Beats", world);
            labelRoot = ChildRoot("STORY TITLES", world);

            Camera camera = CreateCamera();
            CreateMainLight();
            CreateBackdrop();
            CreateRoute();
            GameObject ship = CreateShip();
            GameObject maru = CreateMaru();
            GameObject guideStar = CreateGuideStar();
            GameObject blackout = CreateBlackout();
            CreateStoryBeats(ship.transform, maru.transform, guideStar, blackout);
            GameObject player = CreatePlayer();
            CreateSystems(camera, player.transform);
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
            Debug.Log("[Star Night M4] Prologue built: 12 story rooms, return-cake incident, Maru rescue, guide-star loss, travel ticket, CH1 departure.");
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
            camera.backgroundColor = new Color(0.01f, 0.018f, 0.06f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateMainLight()
        {
            GameObject lightObject = new("Moon Port Light · Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.68f, 0.78f, 1f);
            light.intensity = 0.72f;
            lightObject.transform.rotation = Quaternion.Euler(38f, -24f, 0f);
        }

        private static void CreateBackdrop()
        {
            for (int i = 0; i < 3; i++)
            {
                InstantiateArt(Forest + "Mounts and sky.prefab", $"MoonSky_{i}",
                    new Vector3(i * 48f, 0f, 8f), 1.4f, -55,
                    new Color(0.32f, 0.42f, 0.82f));
            }
            InstantiateArt(Crystal + "Background blur.prefab", "EmergencyNebula",
                new Vector3(48f, -1f, 6f), 2f, -48, new Color(0.56f, 0.34f, 0.82f));
            InstantiateArt(Crystal + "Stars Particle.prefab", "GuideStarDust",
                new Vector3(74f, 3f, 0f), 1.5f, -20, new Color(1f, 0.62f, 0.28f));
            InstantiateArt(Island + "Wind.prefab", "ReturnCurrent",
                new Vector3(39f, 3f, 1f), 1.25f, -18, new Color(0.62f, 0.82f, 1f));
        }

        private static void CreateRoute()
        {
            string[] rooms =
            {
                "고장 난 여행 우주선", "산소 누출 통로", "달 비상 착륙장", "귀환떡 보관함",
                "귀환 안내 표지판", "라니의 진단대", "임시 엔진실", "폭주 활주로",
                "마루 구조 구역", "사라진 길잡이별", "여행 티켓 승강장", "첫 별문 선착장"
            };

            for (int i = 0; i < rooms.Length; i++)
            {
                float x = i * 9f;
                float y = i is 2 or 8 ? 0.8f : i is 7 or 9 ? 1.5f : 0f;
                float floorY = y - 2.65f;
                CreateCollisionPlatform($"Floor_{i:00}", new Vector2(x, floorY),
                    new Vector2(7.7f, 0.55f), collisionRoot);
                InstantiateArt(Station + (i % 3 == 0 ? "Platform with ropes.prefab" :
                        i % 2 == 0 ? "Platform B.prefab" : "Platform A.prefab"),
                    $"MoonPortFloor_{i:00}", new Vector3(x, floorY + 0.25f, 0f),
                    0.62f, -6, i < 7 ? new Color(0.62f, 0.75f, 1f) : new Color(0.86f, 0.5f, 0.72f));
                WorldText(rooms[i], new Vector3(x, y + 2.7f, 0f), 1.2f,
                    i >= 9 ? new Color(1f, 0.56f, 0.34f) : new Color(1f, 0.78f, 0.3f), 55, labelRoot);

                if (i < rooms.Length - 1)
                {
                    float nextY = (i + 1) is 2 or 8 ? 0.8f : (i + 1) is 7 or 9 ? 1.5f : 0f;
                    CreateCollisionPlatform($"Connector_{i:00}",
                        new Vector2(x + 4.5f, Mathf.Min(floorY, nextY - 2.65f) + 0.32f),
                        new Vector2(2f, 0.4f), collisionRoot);
                }

                if (i % 2 == 0)
                {
                    InstantiateArt(Station + "Lamp.prefab", $"PortLamp_{i:00}",
                        new Vector3(x + 2.3f, floorY + 1.1f, 0f), 0.4f, 18,
                        i < 7 ? new Color(0.55f, 0.78f, 1f) : new Color(1f, 0.42f, 0.58f));
                }
            }
        }

        private static GameObject CreateShip()
        {
            GameObject ship = SpriteBlock("RaniShip · 고장 난 여행 우주선", new Vector3(2f, -0.45f, 0f),
                new Vector2(3.2f, 1.35f), new Color(0.38f, 0.72f, 1f), 38, gameplayRoot);
            SpriteBlock("ShipWindow", Vector3.zero, new Vector2(0.62f, 0.62f),
                new Color(1f, 0.78f, 0.3f), 40, ship.transform).transform.localPosition = new Vector3(0.7f, 0.1f, 0f);
            SpriteBlock("BrokenEngine", Vector3.zero, new Vector2(0.7f, 0.85f),
                new Color(1f, 0.24f, 0.42f), 40, ship.transform).transform.localPosition = new Vector3(-1.5f, -0.05f, 0f);
            InstantiateArt(Station + "Small Box.prefab", "EmergencyCargo",
                new Vector3(10f, -1.1f, 0f), 0.55f, 20, new Color(0.7f, 0.82f, 1f));
            return ship;
        }

        private static GameObject CreateMaru()
        {
            GameObject maru = SpriteBlock("Maru · 별길을 물어오는 개", new Vector3(111f, 7f, 0f),
                new Vector2(1.8f, 1.35f), new Color(1f, 0.22f, 0.46f), 49, gameplayRoot);
            CreateEar(maru.transform, -0.48f);
            CreateEar(maru.transform, 0.48f);
            GameObject muzzle = SpriteBlock("GentleMuzzle", Vector3.zero, new Vector2(0.8f, 0.42f),
                new Color(1f, 0.66f, 0.52f), 50, maru.transform);
            muzzle.transform.localPosition = new Vector3(0.72f, -0.1f, 0f);
            return maru;
        }

        private static GameObject CreateGuideStar()
        {
            GameObject guide = SpriteBlock("GuideStar · 길잡이별", new Vector3(82f, 4.3f, 0f),
                new Vector2(1.15f, 1.15f), new Color(1f, 0.82f, 0.24f), 48, gameplayRoot);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI * 2f / 5f;
                SpriteBlock($"StarRay_{i}", Vector3.zero, new Vector2(0.14f, 1f),
                    new Color(1f, 0.72f, 0.24f, 0.72f), 47, guide.transform)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
            }
            return guide;
        }

        private static GameObject CreateBlackout()
        {
            GameObject blackout = SpriteBlock("Blackout · 꺼진 다섯 항로", new Vector3(83f, 2f, 0f),
                new Vector2(16f, 8f), new Color(0.01f, 0.01f, 0.04f, 0.78f), 44, gameplayRoot);
            blackout.SetActive(false);
            return blackout;
        }

        private static void CreateStoryBeats(Transform ship, Transform maru, GameObject guideStar, GameObject blackout)
        {
            CreateBeat("MoonSign · 귀환 안내 표지판", new Vector2(31f, -0.9f), PrologueBeatMode.CheckSign,
                new Color(1f, 0.76f, 0.28f), ship, maru, guideStar, blackout);
            CreateBeat("RaniConsole · 라니의 진단대", new Vector2(40f, -0.9f), PrologueBeatMode.CheckCompanion,
                new Color(0.42f, 0.82f, 1f), ship, maru, guideStar, blackout);
            CreateBeat("ReturnCakeEngine · 귀환떡 임시 엔진", new Vector2(55f, -0.8f), PrologueBeatMode.ReturnCakeEngine,
                new Color(1f, 0.58f, 0.2f), ship, maru, guideStar, blackout);
            CreateBeat("MaruRescueWitness · 구조 목격점", new Vector2(73f, -0.1f), PrologueBeatMode.MaruRescue,
                new Color(1f, 0.3f, 0.5f), ship, maru, guideStar, blackout);
            CreateBeat("GuideStarWitness · 길잡이별 소실점", new Vector2(82f, 0.6f), PrologueBeatMode.GuideStarLoss,
                new Color(0.84f, 0.46f, 1f), ship, maru, guideStar, blackout);
            CreateBeat("MoonMillDeparture · 첫 별문 출항대", new Vector2(100f, -0.8f), PrologueBeatMode.Departure,
                new Color(0.35f, 0.9f, 1f), ship, maru, guideStar, blackout);

            WorldText("귀환떡에서 우주선 폭주, 그리고 마루의 구조", new Vector3(58f, 5.3f, 0f), 1.35f,
                new Color(1f, 0.55f, 0.38f), 58, labelRoot);
            WorldText("마루는 집으로 돌려보낸다. 그리고 돌아갈 별길까지 물어온다.",
                new Vector3(82f, 6.2f, 0f), 1.25f, new Color(1f, 0.4f, 0.58f), 58, labelRoot);
        }

        private static void CreateBeat(string name, Vector2 position, PrologueBeatMode mode, Color color,
            Transform ship, Transform maru, GameObject guideStar, GameObject blackout)
        {
            GameObject beat = SpriteBlock(name, position, new Vector2(1.05f, 1.35f), color, 42, gameplayRoot);
            CircleCollider2D trigger = beat.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.35f;
            beat.AddComponent<PrologueJourneyBeat>().Configure(mode, ship, maru, guideStar, blackout);
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = SpriteBlock("Player · 별을 줍는 아이", new Vector3(-2f, -1.45f, 0f),
                new Vector2(0.72f, 1.2f), new Color(1f, 0.78f, 0.3f), 45, world);
            player.layer = 31;
            GameObject scarf = SpriteBlock("RaniSignalScarf", Vector3.zero, new Vector2(0.9f, 0.12f),
                new Color(0.95f, 0.16f, 0.36f), 47, player.transform);
            scarf.transform.localPosition = new Vector3(-0.5f, 0.05f, 0f);
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
            GameObject systems = new("@STAR NIGHT M4 · 전체 여행");
            systems.AddComponent<StarNightPrologueBootstrap>();
            systems.AddComponent<StarNightCombinationResolver>();
            systems.AddComponent<ChapterPlaytestTelemetry>();

            GameObject hudObject = new("@HUD · 여행 티켓과 라니의 기록");
            StarNightHUD hud = hudObject.AddComponent<StarNightHUD>();
            hud.SetFont(font);

            GameObject atmosphereObject = new("@ATMOSPHERE · 꺼지는 별길");
            StarNightAtmosphere atmosphere = atmosphereObject.AddComponent<StarNightAtmosphere>();
            atmosphere.Configure(camera, player, square);
            atmosphere.SetWorldBounds(new Vector2(-12f, 112f), new Vector2(-11f, 18f), 150);
        }

        private static void CreateCheckpoints()
        {
            (string label, Vector2 position)[] checkpoints =
            {
                ("우주선 비상등", new Vector2(1f, -1f)),
                ("달 착륙장 등불", new Vector2(22f, -0.3f)),
                ("엔진실 등불", new Vector2(51f, -1f)),
                ("구조 구역 등불", new Vector2(72f, -0.2f)),
                ("티켓 승강장 등불", new Vector2(91f, -1f))
            };
            foreach ((string label, Vector2 position) in checkpoints)
            {
                GameObject checkpoint = SpriteBlock($"Checkpoint · {label}", position,
                    new Vector2(0.38f, 1.05f), new Color(1f, 0.72f, 0.28f), 39, gameplayRoot);
                checkpoint.AddComponent<BoxCollider2D>().isTrigger = true;
                checkpoint.AddComponent<StarNightCheckpoint>().Configure(label);
                InstantiateArt(Forest + "light.prefab", $"{label} Art", position, 0.38f, 24,
                    new Color(1f, 0.52f, 0.64f));
            }
        }

        private static void CreateGuide()
        {
            WorldText("프롤로그 · 별길을 잃은 밤", new Vector3(0f, 7.1f, 0f), 2.5f,
                new Color(1f, 0.78f, 0.3f), 60, labelRoot);
            WorldText("목표 · 귀환떡 사건과 마루의 행동을 끝까지 목격하자", new Vector3(0f, 6f, 0f), 1.4f,
                new Color(0.72f, 0.86f, 1f), 59, labelRoot);
            WorldText("이동 A/D · 점프 Space · 상호작용 X · 여행 티켓 T", new Vector3(0f, 5f, 0f), 1.15f,
                new Color(0.9f, 0.92f, 1f), 59, labelRoot);
        }

        private static void CreateWorldBounds()
        {
            CreateCollisionPlatform("WorldBottom", new Vector2(50f, -12.6f), new Vector2(124f, 1f), collisionRoot);
            CreateCollisionPlatform("LeftBoundary", new Vector2(-7f, 2f), new Vector2(0.6f, 31f), collisionRoot);
            CreateCollisionPlatform("RightBoundary", new Vector2(108f, 2f), new Vector2(0.6f, 31f), collisionRoot);
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
                Debug.LogWarning($"[Star Night M4] Missing bundle art: {path}");
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
            GameObject ear = SpriteBlock("Ear", Vector3.zero, new Vector2(0.38f, 0.68f),
                new Color(1f, 0.24f, 0.48f), 51, parent);
            ear.transform.localPosition = new Vector3(x, 0.7f, 0f);
            ear.transform.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? -18f : 18f);
        }

        private static void AddToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(scene => scene.path != path)
                .ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
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
