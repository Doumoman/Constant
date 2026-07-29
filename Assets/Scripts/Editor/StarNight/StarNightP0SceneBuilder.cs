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
    public static class StarNightP0SceneBuilder
    {
        private const string SceneFolder = "Assets/Scenes/StarNight";
        private const string ScenePath = SceneFolder + "/StarNight_MoonMill.unity";
        private const string SquarePath = "Assets/Resources/Sprites/Square.png";
        private const string FontPath = "Assets/TextMesh Pro/Fonts/NeoDunggeunmoPro-Regular.asset";

        private static Sprite square;
        private static TMP_FontAsset font;
        private static Transform world;

        [MenuItem("Tools/Star Night/Build P0 Moon Mill")]
        public static void Build()
        {
            EditorSceneManager.SaveOpenScenes();
            EnsureFolder(SceneFolder);
            square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (square == null)
            {
                throw new FileNotFoundException("Square sprite was not found.", SquarePath);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "StarNight_MoonMill";
            world = new GameObject("WORLD · 달토끼 방앗간").transform;

            Camera camera = CreateCamera();
            CreateMainLight();
            CreateBackdrop();
            CreateRooms();
            GameObject player = CreatePlayer();
            CreateChapterSystems(camera, player.transform);
            CreateGameplayObjects();
            CreateWorldLegend();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            Selection.activeGameObject = player;
            SceneView.lastActiveSceneView?.FrameSelected();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Star Night] Built playable P0 scene: {ScenePath}");
        }

        private static Camera CreateCamera()
        {
            GameObject objectCamera = new("Main Camera");
            objectCamera.tag = "MainCamera";
            Camera camera = objectCamera.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.3f;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.095f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            objectCamera.transform.position = new Vector3(3f, 1.2f, -10f);
            objectCamera.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateMainLight()
        {
            GameObject lightObject = new("Moonlight · Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.72f, 0.8f, 1f);
            light.intensity = 0.55f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        }

        private static void CreateBackdrop()
        {
            GameObject moon = SpriteBlock("Huge Moon", new Vector3(32f, 7.5f, 7f), new Vector2(10f, 10f), new Color(1f, 0.78f, 0.3f, 0.11f), -90);
            moon.transform.SetParent(world);
            for (int i = 0; i < 12; i++)
            {
                GameObject cloud = SpriteBlock($"MoonDust_{i:00}", new Vector3(i * 7f - 4f, -0.15f + (i % 3) * 0.18f, 2f),
                    new Vector2(6.5f, 0.5f), new Color(0.32f, 0.23f, 0.52f, 0.3f), -20);
                cloud.transform.SetParent(world);
            }
        }

        private static void CreateRooms()
        {
            List<StarRoomNode> rooms = StarNightRoomGraphGenerator.GenerateMoonMill(173, 11);
            Color[] accents =
            {
                new(0.32f, 0.22f, 0.5f), new(0.22f, 0.38f, 0.55f), new(0.48f, 0.23f, 0.38f),
                new(0.2f, 0.47f, 0.42f), new(0.5f, 0.38f, 0.18f)
            };

            for (int i = 0; i < rooms.Count; i++)
            {
                float x = i * 7f;
                GameObject root = new($"Room_{i:00} · {rooms[i].displayName}");
                root.transform.SetParent(world);
                root.transform.position = new Vector3(x, 0f, 0f);

                CreatePlatform(root.transform, "Floor", new Vector2(0f, -2.8f), new Vector2(7f, 0.7f), new Color(0.13f, 0.11f, 0.22f));
                CreatePlatform(root.transform, "Step", new Vector2((i % 2 == 0 ? 1.8f : -1.8f), -0.7f + (i % 3) * 0.5f),
                    new Vector2(2.4f, 0.35f), accents[i % accents.Length] * 0.8f);
                if (i % 3 == 1)
                {
                    CreatePlatform(root.transform, "UpperStep", new Vector2(1.4f, 1.6f), new Vector2(2.2f, 0.3f), accents[(i + 1) % accents.Length]);
                }

                WorldText(root.transform, rooms[i].displayName, new Vector3(0f, 3.5f, 0f), 3.3f,
                    rooms[i].temptation ? new Color(1f, 0.3f, 0.55f) : new Color(1f, 0.78f, 0.32f));
                WorldText(root.transform, rooms[i].guaranteed ? "반드시 지나가는 방" : "이번 밤에 열린 곁방",
                    new Vector3(0f, 2.85f, 0f), 1.6f, new Color(0.65f, 0.7f, 0.88f));
            }

            CreatePlatform(world, "WorldBottom", new Vector2(35f, -5.5f), new Vector2(90f, 1f), new Color(0.035f, 0.03f, 0.08f));
            CreatePlatform(world, "LeftBoundary", new Vector2(-4f, 0f), new Vector2(0.5f, 14f), Color.clear);
            CreatePlatform(world, "RightBoundary", new Vector2(74f, 0f), new Vector2(0.5f, 14f), Color.clear);
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = SpriteBlock("Player", Vector3.zero, new Vector2(0.72f, 1.2f), new Color(1f, 0.78f, 0.3f), 20);
            player.name = "Player · 별을 줍는 아이";
            player.layer = 31;
            player.transform.position = new Vector3(-1.8f, -1.6f, 0f);
            GameObject face = SpriteBlock("FaceGlow", Vector3.zero, new Vector2(0.46f, 0.42f), new Color(1f, 0.93f, 0.68f), 21);
            face.transform.SetParent(player.transform, false);
            face.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            GameObject scarf = SpriteBlock("StarScarf", Vector3.zero, new Vector2(0.7f, 0.13f), new Color(0.96f, 0.25f, 0.48f), 22);
            scarf.transform.SetParent(player.transform, false);
            scarf.transform.localPosition = new Vector3(-0.42f, -0.05f, 0f);
            GameObject umbrella = SpriteBlock("Umbrella", Vector3.zero, new Vector2(1.05f, 0.16f), new Color(0.35f, 0.78f, 1f), 19);
            umbrella.transform.SetParent(player.transform, false);
            umbrella.transform.localPosition = new Vector3(0.5f, 0.2f, 0f);
            umbrella.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 3.2f;
            body.freezeRotation = true;
            CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.82f, 1.1f);
            player.AddComponent<StarNightInventory>();
            player.AddComponent<StarNightSimpleMotor>();
            player.AddComponent<StarNightPlayerAgent>();
            return player;
        }

        private static void CreateChapterSystems(Camera camera, Transform player)
        {
            GameObject systems = new("@STAR NIGHT · 시스템 재난 런");
            systems.AddComponent<MoonMillChapterBootstrap>();
            systems.AddComponent<StarNightCombinationResolver>();

            GameObject hudObject = new("@HUD · 밤의 기록");
            StarNightHUD hud = hudObject.AddComponent<StarNightHUD>();
            hud.SetFont(font);

            GameObject atmosphereObject = new("@ATMOSPHERE · 별내음");
            StarNightAtmosphere atmosphere = atmosphereObject.AddComponent<StarNightAtmosphere>();
            atmosphere.Configure(camera, player, square);

            GameObject maru = SpriteBlock("Maru · 돌아갈 시간", new Vector3(70f, 3f, 0f), new Vector2(1.5f, 1.15f), new Color(1f, 0.25f, 0.48f), 30);
            CreateEar(maru.transform, -0.42f);
            CreateEar(maru.transform, 0.42f);
            WorldText(maru.transform, "마루", new Vector3(0f, 1.1f, 0f), 2f, new Color(1f, 0.35f, 0.55f));
            MaruDirector director = systems.AddComponent<MaruDirector>();
            director.Configure(maru.transform, new Vector3(70f, 3f, 0f));
        }

        private static void CreateGameplayObjects()
        {
            CreateFable("gear_smallable", "금 간 톱니", new Vector2(9f, -1.8f), new Vector2(0.8f, 0.8f),
                new Color(0.75f, 0.7f, 0.62f), StarItemKind.ResidentProperty,
                FableTraits.Carryable | FableTraits.Resizable | FableTraits.Linkable | FableTraits.ResidentProperty, 1f);

            CreateFable("moon_cake_01", "따뜻한 달떡", new Vector2(21f, -1.6f), new Vector2(1f, 0.65f),
                new Color(1f, 0.75f, 0.27f), StarItemKind.DepartureSupply,
                FableTraits.Carryable | FableTraits.Resizable | FableTraits.Floatable | FableTraits.DepartureSupply | FableTraits.MoonCake, 2f);
            CreateFable("moon_cake_02", "보름 달떡", new Vector2(31f, -1.2f), new Vector2(1.1f, 0.7f),
                new Color(1f, 0.64f, 0.22f), StarItemKind.DepartureSupply,
                FableTraits.Carryable | FableTraits.Resizable | FableTraits.Floatable | FableTraits.DepartureSupply | FableTraits.MoonCake, 2.5f);
            CreateFable("moon_cake_winter", "겨울 달떡", new Vector2(43f, -1.7f), new Vector2(0.9f, 0.65f),
                new Color(0.55f, 0.85f, 1f), StarItemKind.DepartureSupply,
                FableTraits.Carryable | FableTraits.Resizable | FableTraits.DepartureSupply | FableTraits.MoonCake, 1.5f);

            CreateFable("wood_box_01", "달나무 상자", new Vector2(14f, -1.8f), new Vector2(1.1f, 1.1f),
                new Color(0.45f, 0.26f, 0.2f), StarItemKind.General,
                FableTraits.Carryable | FableTraits.Resizable | FableTraits.Floatable | FableTraits.Breakable, 0.7f);
            CreateFable("explosive_fruit_01", "톡톡별 열매", new Vector2(27f, 0.2f), new Vector2(0.8f, 0.8f),
                new Color(1f, 0.22f, 0.35f), StarItemKind.General,
                FableTraits.Carryable | FableTraits.Resizable | FableTraits.Floatable | FableTraits.Explosive | FableTraits.Bouncy, 3.2f)
                .gameObject.AddComponent<StarNightHazard>();
            CreateFable("rare_bell_seed", "잠든 방울씨", new Vector2(62f, -1.6f), new Vector2(0.75f, 0.9f),
                new Color(0.7f, 0.38f, 1f), StarItemKind.RareToy,
                FableTraits.Carryable | FableTraits.Resizable | FableTraits.LightReactive | FableTraits.RareToy, 4f);

            GameObject mill = CreateStation("MoonMill · 멈춘 방앗간", new Vector2(17f, -1.35f), new Vector2(2.4f, 2.8f), new Color(0.3f, 0.52f, 0.62f));
            mill.AddComponent<MoonMillRepairStation>();
            WorldText(mill.transform, "멈춘 방앗간", new Vector3(0f, 1.8f, 0f), 2.1f, new Color(0.65f, 0.9f, 1f));

            GameObject pedestal = CreateStation("StarFuelPedestal · 별 연료통", new Vector2(48f, -1.65f), new Vector2(1.5f, 1.8f), new Color(1f, 0.6f, 0.18f));
            pedestal.AddComponent<MoonMillFuelPedestal>();
            WorldText(pedestal.transform, "달떡 0 / 3", new Vector3(0f, 1.3f, 0f), 1.9f, new Color(1f, 0.8f, 0.3f));

            GameObject temptation = CreateStation("TemptationDoor · 달 뒤편 창고", new Vector2(59f, -1.4f), new Vector2(1.2f, 2.6f), new Color(0.55f, 0.16f, 0.45f));
            temptation.AddComponent<MoonMillTemptationDoor>();
            WorldText(temptation.transform, "떠난 뒤에 열리는 문", new Vector3(0f, 1.8f, 0f), 1.75f, new Color(1f, 0.35f, 0.68f));

            GameObject departure = CreateStation("MoonBoat · 달배", new Vector2(69f, -1.4f), new Vector2(3.2f, 1.2f), new Color(0.28f, 0.74f, 0.9f));
            departure.AddComponent<MoonMillDepartureGate>();
            WorldText(departure.transform, "달배 · 출발", new Vector3(0f, 1.25f, 0f), 2.3f, new Color(0.5f, 0.92f, 1f));

            GameObject rabbit = CreateStation("RabbitMiller · 방앗간지기", new Vector2(4f, -1.7f), new Vector2(0.9f, 1.4f), new Color(0.92f, 0.85f, 0.75f));
            rabbit.AddComponent<MoonRabbitWitness>();
            CreateEar(rabbit.transform, -0.24f);
            CreateEar(rabbit.transform, 0.24f);
            WorldText(rabbit.transform, "방앗간지기 묘월", new Vector3(0f, 1.35f, 0f), 1.8f, new Color(0.95f, 0.86f, 0.75f));
        }

        private static FableObject CreateFable(string id, string label, Vector2 position, Vector2 size, Color color,
            StarItemKind kind, FableTraits traits, float scent)
        {
            GameObject item = SpriteBlock($"{label} [{id}]", new Vector3(position.x, position.y, 0f), size, color, 10);
            item.transform.SetParent(world);
            BoxCollider2D collider = item.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            Rigidbody2D body = item.AddComponent<Rigidbody2D>();
            body.gravityScale = 1.8f;
            body.freezeRotation = false;
            FableObject fable = item.AddComponent<FableObject>();
            fable.Configure(id, label, kind, traits, scent);
            WorldText(item.transform, label, new Vector3(0f, 0.9f, 0f), 1.45f, Color.white);
            return fable;
        }

        private static GameObject CreateStation(string objectName, Vector2 position, Vector2 size, Color color)
        {
            GameObject station = SpriteBlock(objectName, new Vector3(position.x, position.y, 0f), size, color, 5);
            station.transform.SetParent(world);
            BoxCollider2D collider = station.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            return station;
        }

        private static void CreateEar(Transform parent, float x)
        {
            GameObject ear = SpriteBlock("Ear", new Vector3(0f, 0f, 0f), new Vector2(0.28f, 0.75f), new Color(1f, 0.45f, 0.62f), 29);
            ear.transform.SetParent(parent, false);
            ear.transform.localPosition = new Vector3(x, 0.75f, 0f);
            ear.transform.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? 12f : -12f);
        }

        private static GameObject SpriteBlock(string objectName, Vector3 position, Vector2 size, Color color, int sortingOrder)
        {
            GameObject block = new(objectName);
            block.transform.position = position;
            block.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return block;
        }

        private static void CreatePlatform(Transform parent, string objectName, Vector2 localPosition, Vector2 size, Color color)
        {
            GameObject platform = SpriteBlock(objectName, Vector3.zero, size, color, -5);
            platform.transform.SetParent(parent, false);
            platform.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            platform.layer = 7;
            BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
        }

        private static void WorldText(Transform parent, string value, Vector3 localPosition, float size, Color color)
        {
            GameObject textObject = new($"Label · {value}");
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;
            TextMeshPro text = textObject.AddComponent<TextMeshPro>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.sortingOrder = 40;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(7f, 1f);
        }

        private static void CreateWorldLegend()
        {
            GameObject legend = new("PLAY GUIDE");
            legend.transform.SetParent(world);
            legend.transform.position = new Vector3(-1f, 4.8f, 0f);
            WorldText(legend.transform, "별을 물어오는 밤", Vector3.zero, 4.2f, new Color(1f, 0.77f, 0.28f));
            WorldText(legend.transform, "이동 A/D · 점프 Space · 줍기 X · 도구 E · 크기 전환 Q · 우산 B · 내려놓기 G",
                new Vector3(0f, -0.9f, 0f), 1.7f, new Color(0.8f, 0.84f, 1f));
        }

        private static void AddToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(scene => scene.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
#endif
