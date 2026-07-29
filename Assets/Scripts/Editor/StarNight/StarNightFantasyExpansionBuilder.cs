#if UNITY_EDITOR
using System;
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
    public static class StarNightFantasyExpansionBuilder
    {
        private const string SceneFolder = "Assets/Scenes/StarNight";
        private const string ScenePath = SceneFolder + "/StarNight_MoonMill.unity";
        private const string SquarePath = "Assets/Resources/Sprites/Square.png";
        private const string FontPath = "Assets/TextMesh Pro/Fonts/NeoDunggeunmoPro-Regular.asset";

        private const string Forest = "Assets/2D Fantasy sprite bundle/Forest  V2.0/Prefabs/";
        private const string OldForest = "Assets/2D Fantasy sprite bundle/Old Forest pack/Prefabs/";
        private const string Crystal = "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/Crystal prefabs/";
        private const string Station = "Assets/2D Fantasy sprite bundle/Abandoned station/Prefabs/";
        private const string Desert = "Assets/2D Fantasy sprite bundle/Desert pack/Prefabs/";
        private const string Island = "Assets/2D Fantasy sprite bundle/Island pack/Prefabs/";
        private const string Spring = "Assets/2D Fantasy sprite bundle/Spring forest/Prefabs/";
        private const string Chains = "Assets/2D Fantasy sprite bundle/Bonus/Climbing elements/Chains/";

        private static Sprite square;
        private static TMP_FontAsset font;
        private static Transform world;
        private static Transform artRoot;
        private static Transform collisionRoot;
        private static Transform labelRoot;

        private enum RoomTheme
        {
            Forest,
            Mill,
            Crystal,
            OldForest
        }

        private sealed class RoomSpec
        {
            public readonly string id;
            public readonly string label;
            public readonly float x;
            public readonly float y;
            public readonly RoomTheme theme;
            public readonly bool optional;

            public RoomSpec(string id, string label, float x, float y, RoomTheme theme, bool optional = false)
            {
                this.id = id;
                this.label = label;
                this.x = x;
                this.y = y;
                this.theme = theme;
                this.optional = optional;
            }

            public float FloorY => y - 2.65f;
        }

        [MenuItem("Tools/Star Night/Build Expanded Fantasy Moon Mill")]
        public static void Build()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == ScenePath && activeScene.isDirty)
            {
                EditorSceneManager.SaveScene(activeScene);
            }

            EnsureFolder(SceneFolder);
            square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            ValidateAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "StarNight_MoonMill";
            world = new GameObject("WORLD · 달토끼 방앗간 확장 구역").transform;
            artRoot = new GameObject("ART · 2D Fantasy Bundle").transform;
            collisionRoot = new GameObject("COLLISION · Stable Platform Route").transform;
            labelRoot = new GameObject("ROOM TITLES").transform;
            artRoot.SetParent(world);
            collisionRoot.SetParent(world);
            labelRoot.SetParent(world);

            Camera camera = CreateCamera();
            CreateMainLight();
            CreateBackdropArt();
            List<RoomSpec> rooms = CreateRoomSpecs();
            CreateMainRoute(rooms);
            CreateSkyRoute();
            CreateDeepCellarRoute();
            CreateTemptationAnnex();
            CreateLandmarks();
            GameObject player = CreatePlayer();
            CreateChapterSystems(camera, player.transform);
            CreateGameplayObjects();
            CreateCheckpoints();
            CreateWorldGuide();
            CreateWorldBoundaries();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            Selection.activeGameObject = player;
            SceneView.lastActiveSceneView?.FrameSelected();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Star Night] Expanded 2D Fantasy Moon Mill built: 22 main rooms + 3 branch routes.");
        }

        private static List<RoomSpec> CreateRoomSpecs()
        {
            return new List<RoomSpec>
            {
                new("arrival", "달토끼 도착 마당", 0f, 0f, RoomTheme.Forest),
                new("moon_well", "달가루 우물", 8.5f, 0f, RoomTheme.Forest),
                new("rabbit_market", "토끼 장터", 17f, 0.7f, RoomTheme.Forest),
                new("shrinking_trail", "작아지는 오솔길", 25.5f, 0f, RoomTheme.Forest),
                new("mill_front", "방앗간 앞뜰", 34f, 0f, RoomTheme.Mill),
                new("broken_wheel", "부서진 물레방", 42.5f, 0.8f, RoomTheme.Mill),
                new("steam_chimney", "달김 굴뚝", 51f, 2f, RoomTheme.Mill),
                new("dust_attic", "달가루 다락", 59.5f, 3.2f, RoomTheme.Mill, true),
                new("sack_walk", "매달린 자루길", 68f, 4.3f, RoomTheme.Mill, true),
                new("clock_rafters", "멈춘 시계 서까래", 76.5f, 4.9f, RoomTheme.Mill, true),
                new("upper_hopper", "별보리 깔때기", 85f, 3.5f, RoomTheme.Mill),
                new("flour_shaft", "달가루 수직갱", 93.5f, 1.8f, RoomTheme.Mill),
                new("cake_warehouse", "달떡 창고", 102f, 0f, RoomTheme.Mill),
                new("fruit_house", "톡톡별 온실", 110.5f, -0.8f, RoomTheme.Forest, true),
                new("crystal_cellar", "별가루 결정고", 119f, -1.9f, RoomTheme.Crystal),
                new("parcel_tunnel", "잃어버린 소포굴", 127.5f, -3f, RoomTheme.Crystal, true),
                new("frost_store", "겨울 달떡 저장고", 136f, -1.9f, RoomTheme.Crystal),
                new("scent_bell", "별냄새 방울방", 144.5f, -0.8f, RoomTheme.Crystal),
                new("back_gate", "달 뒤편 창고문", 153f, 0f, RoomTheme.OldForest, true),
                new("moon_lift", "달빛 승강장", 161.5f, 1.1f, RoomTheme.OldForest),
                new("bell_roof", "방울지붕", 170f, 2.2f, RoomTheme.OldForest, true),
                new("moon_pier", "달배 선착장", 178.5f, 1.1f, RoomTheme.Crystal)
            };
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(2.5f, 0.5f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.7f;
            camera.backgroundColor = new Color(0.018f, 0.025f, 0.075f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateMainLight()
        {
            GameObject lightObject = new("Moonlight · Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.7f, 0.78f, 1f);
            light.intensity = 0.62f;
            lightObject.transform.rotation = Quaternion.Euler(40f, -25f, 0f);
        }

        private static void CreateBackdropArt()
        {
            for (int i = 0; i < 4; i++)
            {
                InstantiateArt(Forest + "Mounts and sky.prefab", $"ForestSky_{i}", new Vector3(i * 58f, 2f, 8f),
                    1.15f, -140, new Color(0.34f, 0.4f, 0.62f, 0.75f));
            }

            InstantiateArt(Spring + "SunLight.prefab", "MoonHalo", new Vector3(92f, 9f, 7f),
                1.1f, -130, new Color(0.75f, 0.82f, 1f, 0.48f));
            InstantiateArt(Crystal + "Background blur.prefab", "CrystalSkyBlur", new Vector3(128f, -1f, 6f),
                2.3f, -125, new Color(0.43f, 0.52f, 0.8f, 0.65f));
            InstantiateArt(Crystal + "Background normal.prefab", "CrystalSky", new Vector3(140f, -1f, 5f),
                2.1f, -120, new Color(0.62f, 0.65f, 0.92f, 0.72f));
            InstantiateArt(Crystal + "Trees Layer B.prefab", "CrystalDistantTrees", new Vector3(125f, -5f, 4f),
                1.8f, -110, new Color(0.3f, 0.35f, 0.68f, 0.7f));
            InstantiateArt(Crystal + "Stars Particle.prefab", "CrystalStars", new Vector3(130f, 2f, 0f),
                1.5f, -100, Color.white);
            InstantiateArt(OldForest + "Fog.prefab", "TemptationFog", new Vector3(158f, -1f, 2f),
                1.8f, -60, new Color(0.48f, 0.22f, 0.58f, 0.58f));
        }

        private static void CreateMainRoute(IReadOnlyList<RoomSpec> rooms)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                RoomSpec room = rooms[i];
                GameObject roomRoot = new($"ROOM {i:00} · {room.label}");
                roomRoot.transform.SetParent(world);
                CreateFloor(room, roomRoot.transform, i);
                CreateRoomPlatforms(room, roomRoot.transform, i);
                CreateDiscoveryZone(room.id, room.label, new Vector2(room.x, room.y), new Vector2(8f, 6f), room.optional);
                WorldText(room.label, new Vector3(room.x, room.y + 3.6f, 0f), 2.2f,
                    room.optional ? new Color(1f, 0.38f, 0.65f) : new Color(1f, 0.78f, 0.34f), 42);
                WorldText(i < 4 ? "달숲 외곽" : i < 13 ? "방앗간 내부" : i < 18 ? "별가루 지하" : "달 뒤편",
                    new Vector3(room.x, room.y + 3.05f, 0f), 1.25f, new Color(0.62f, 0.72f, 0.95f), 41);

                if (i < rooms.Count - 1)
                {
                    CreateConnector(room, rooms[i + 1], i);
                }
            }
        }

        private static void CreateFloor(RoomSpec room, Transform parent, int index)
        {
            CreateCollisionPlatform($"FloorCollider_{room.id}", new Vector2(room.x, room.FloorY - 0.15f), new Vector2(8.25f, 0.55f), parent);
            string path;
            float visualY;
            float scale;
            switch (room.theme)
            {
                case RoomTheme.Mill:
                    path = Station + (index % 2 == 0 ? "Platform A.prefab" : "Platform B.prefab");
                    visualY = room.FloorY - 0.48f;
                    scale = index % 2 == 0 ? 0.9f : 1.15f;
                    break;
                case RoomTheme.Crystal:
                    path = Crystal + $"Crystal platform {(char)('A' + index % 4)}.prefab";
                    visualY = room.FloorY - 0.55f;
                    scale = 1.85f;
                    break;
                case RoomTheme.OldForest:
                    path = OldForest + "Old ground 9slice.prefab";
                    visualY = room.FloorY - 3.2f;
                    scale = 0.84f;
                    break;
                default:
                    path = Forest + $"Platform {(char)('A' + index % 3)}.prefab";
                    visualY = room.FloorY - 3.75f;
                    scale = 0.9f;
                    break;
            }

            InstantiateArt(path, $"FloorArt_{room.id}", new Vector3(room.x, visualY, 0f), scale, -8, Color.white, parent);
        }

        private static void CreateRoomPlatforms(RoomSpec room, Transform parent, int index)
        {
            float direction = index % 2 == 0 ? 1f : -1f;
            Vector2 lower = new(room.x + direction * 2.25f, room.FloorY + 1.7f);
            Vector2 upper = new(room.x - direction * 1.8f, room.FloorY + 3.3f);
            string smallPath = room.theme switch
            {
                RoomTheme.Mill => Station + "Platform B.prefab",
                RoomTheme.Crystal => Crystal + "Crystal platform A.prefab",
                RoomTheme.OldForest => OldForest + "box.prefab",
                _ => Forest + "Platform C.prefab"
            };
            float smallScale = room.theme == RoomTheme.Forest ? 0.33f : room.theme == RoomTheme.Crystal ? 0.78f : 0.45f;
            float visualOffset = room.theme == RoomTheme.Forest ? -1.25f : -0.15f;

            CreateCollisionPlatform($"Step_{room.id}_A", lower, new Vector2(2.4f, 0.32f), parent);
            InstantiateArt(smallPath, $"StepArt_{room.id}_A", new Vector3(lower.x, lower.y + visualOffset, 0f),
                smallScale, 2, Color.white, parent);
            if (index % 3 != 0)
            {
                CreateCollisionPlatform($"Step_{room.id}_B", upper, new Vector2(2.1f, 0.3f), parent);
                InstantiateArt(smallPath, $"StepArt_{room.id}_B", new Vector3(upper.x, upper.y + visualOffset, 0f),
                    smallScale * 0.9f, 3, new Color(0.86f, 0.9f, 1f), parent);
            }
        }

        private static void CreateConnector(RoomSpec from, RoomSpec to, int index)
        {
            float startY = from.FloorY + 0.8f;
            float endY = to.FloorY + 0.8f;
            for (int step = 1; step <= 2; step++)
            {
                float t = step / 3f;
                Vector2 position = new(Mathf.Lerp(from.x, to.x, t), Mathf.Lerp(startY, endY, t));
                CreateCollisionPlatform($"Connector_{index:00}_{step}", position, new Vector2(2.5f, 0.28f), collisionRoot);
                string path = index < 4 ? Forest + "Platform C.prefab" :
                    index < 13 ? Station + "Platform B.prefab" :
                    index < 18 ? Crystal + "Crystal platform A.prefab" :
                    OldForest + "box.prefab";
                float scale = index < 4 ? 0.3f : index < 13 ? 0.42f : index < 18 ? 0.72f : 0.34f;
                float yOffset = index < 4 ? -1.15f : -0.12f;
                InstantiateArt(path, $"ConnectorArt_{index:00}_{step}", new Vector3(position.x, position.y + yOffset, 0f),
                    scale, 1, Color.white);
            }
        }

        private static void CreateSkyRoute()
        {
            Vector2[] points =
            {
                new(43f, 6.2f), new(49f, 7.6f), new(56f, 8.8f), new(63f, 9.4f),
                new(70f, 9.2f), new(77f, 8.1f), new(84f, 6.4f), new(90f, 4.8f)
            };
            GameObject root = new("BRANCH · 매달린 자루 지름길");
            root.transform.SetParent(world);
            for (int i = 0; i < points.Length; i++)
            {
                CreateCollisionPlatform($"SkyRoute_{i:00}", points[i], new Vector2(3.2f, 0.35f), root.transform);
                InstantiateArt(Station + (i % 2 == 0 ? "Platform with ropes.prefab" : "Platform B.prefab"),
                    $"SkyRouteArt_{i:00}", new Vector3(points[i].x, points[i].y - 0.2f, 0f),
                    i % 2 == 0 ? 0.62f : 0.55f, 5, new Color(0.88f, 0.92f, 1f), root.transform);
                if (i == 2 || i == 5)
                {
                    InstantiateArt(Chains + "Crystal chain.prefab", $"SkyChain_{i}", new Vector3(points[i].x, points[i].y + 4f, 0f),
                        0.65f, 4, new Color(0.72f, 0.85f, 1f), root.transform);
                }
            }
            CreateDiscoveryZone("sky_shortcut", "매달린 자루 지름길", new Vector2(65f, 9f), new Vector2(32f, 4f), true);
            WorldText("곁길 · 냄새는 짙지만 빠른 길", new Vector3(66f, 11.2f, 0f), 1.7f,
                new Color(0.58f, 0.9f, 1f), 46);
        }

        private static void CreateDeepCellarRoute()
        {
            Vector2[] points =
            {
                new(105f, -5.2f), new(111f, -6.6f), new(118f, -7.6f), new(126f, -8.2f),
                new(134f, -7.7f), new(141f, -6.2f), new(147f, -4.4f)
            };
            GameObject root = new("BRANCH · 별가루 깊은 저장고");
            root.transform.SetParent(world);
            for (int i = 0; i < points.Length; i++)
            {
                CreateCollisionPlatform($"Cellar_{i:00}", points[i], new Vector2(4.2f, 0.4f), root.transform);
                InstantiateArt(Crystal + $"Crystal platform {(char)('A' + i % 4)}.prefab", $"CellarArt_{i:00}",
                    new Vector3(points[i].x, points[i].y - 0.5f, 0f), 1.1f, 6, Color.white, root.transform);
                if (i % 2 == 0)
                {
                    InstantiateArt(Crystal + "Crystal.prefab", $"CellarCrystal_{i:00}",
                        new Vector3(points[i].x + 1.2f, points[i].y + 0.35f, 0f), 0.35f, 8,
                        new Color(0.55f, 0.8f, 1f), root.transform);
                }
            }
            InstantiateArt(Crystal + "Dust Particle.prefab", "CellarDust", new Vector3(126f, -6f, 0f),
                1.2f, 7, Color.white, root.transform);
            CreateDiscoveryZone("deep_cellar", "별가루 깊은 저장고", new Vector2(126f, -7f), new Vector2(38f, 5f), true);
            WorldText("깊은 곁방 · 겨울 달떡이 잠든 곳", new Vector3(126f, -5.2f, 0f), 1.65f,
                new Color(0.62f, 0.88f, 1f), 47);
        }

        private static void CreateTemptationAnnex()
        {
            Vector2[] points =
            {
                new(148f, -4.2f), new(154f, -5.4f), new(160f, -5.2f), new(166f, -3.8f)
            };
            GameObject root = new("BRANCH · 달 뒤편 창고 안쪽");
            root.transform.SetParent(world);
            for (int i = 0; i < points.Length; i++)
            {
                CreateCollisionPlatform($"Temptation_{i:00}", points[i], new Vector2(4.2f, 0.38f), root.transform);
                InstantiateArt(OldForest + "Old ground 9slice.prefab", $"TemptationArt_{i:00}",
                    new Vector3(points[i].x, points[i].y - 1.55f, 0f), 0.38f, 7,
                    new Color(0.6f, 0.45f, 0.72f), root.transform);
            }
            InstantiateArt(OldForest + "TreeB.prefab", "TemptationTree", new Vector3(158f, -5.4f, 0f),
                0.6f, 3, new Color(0.46f, 0.34f, 0.65f), root.transform);
            InstantiateArt(OldForest + "Firefly particle.prefab", "TemptationFireflies", new Vector3(158f, -2f, 0f),
                1.4f, 18, new Color(1f, 0.35f, 0.68f), root.transform);
            CreateDiscoveryZone("temptation_annex", "달 뒤편 창고 안쪽", new Vector2(157f, -4.5f), new Vector2(24f, 5f), true);
            WorldText("돌아갈 수 있는데도 내려가는 길", new Vector3(157f, -2.1f, 0f), 1.7f,
                new Color(1f, 0.35f, 0.68f), 48);
        }

        private static void CreateLandmarks()
        {
            InstantiateArt(Forest + "Entrance.prefab", "RabbitVillageEntrance", new Vector3(-1f, -2.5f, 0f),
                0.62f, -2, new Color(0.85f, 0.88f, 1f));
            InstantiateArt(Forest + "Big Tree A.prefab", "MoonTree_A", new Vector3(11f, -2.8f, 2f),
                0.55f, -22, new Color(0.45f, 0.55f, 0.88f));
            InstantiateArt(Forest + "Big Tree B.prefab", "MoonTree_B", new Vector3(25f, -2.8f, 2f),
                0.48f, -20, new Color(0.55f, 0.5f, 0.82f));
            InstantiateArt(Forest + "Bushes A.prefab", "MoonBushes", new Vector3(18f, -2.4f, 0f),
                0.65f, 4, new Color(0.72f, 0.66f, 0.95f));
            InstantiateArt(Forest + "Fireflys.prefab", "MoonFireflies", new Vector3(18f, 1f, 0f),
                1.2f, 20, new Color(1f, 0.82f, 0.35f));

            InstantiateArt(Station + "Columns A.prefab", "MillColumnsLeft", new Vector3(33f, -2.6f, 0f),
                0.72f, -1, new Color(0.64f, 0.74f, 0.92f));
            InstantiateArt(Station + "Columns B.prefab", "MillColumnsRight", new Vector3(49f, -1.8f, 0f),
                0.72f, -1, new Color(0.64f, 0.72f, 0.9f));
            InstantiateArt(Station + "Mashinery dump.prefab", "MoonMillMachinery", new Vector3(38f, -1.9f, 0f),
                0.72f, 4, new Color(0.68f, 0.78f, 0.96f));
            InstantiateArt(Station + "Cables A.prefab", "MillCables", new Vector3(59f, 1.5f, 0f),
                0.8f, 2, new Color(0.65f, 0.75f, 0.98f));
            for (int i = 0; i < 6; i++)
            {
                InstantiateArt(Station + "Lamp.prefab", $"MillLamp_{i:00}",
                    new Vector3(35f + i * 10f, 2.1f + (i % 2) * 1.5f, 0f),
                    0.48f, 16, new Color(1f, 0.72f, 0.3f));
            }

            InstantiateArt(Crystal + "Trees Layer A.prefab", "CellarCrystalTrees", new Vector3(128f, -8.5f, 1f),
                1.05f, -15, new Color(0.52f, 0.58f, 0.88f));
            InstantiateArt(Crystal + "Crystal walls.prefab", "CellarCrystalWall", new Vector3(139f, -4.5f, 0f),
                0.8f, -3, new Color(0.72f, 0.78f, 1f));
            InstantiateArt(OldForest + "Ground decor.prefab", "BackWarehouseRoots", new Vector3(158f, -2.4f, 0f),
                0.7f, 4, new Color(0.55f, 0.38f, 0.68f));
            InstantiateArt(Island + "platform wt.prefab", "MoonBoatArt", new Vector3(179f, -0.7f, 0f),
                0.8f, 12, new Color(0.55f, 0.86f, 1f));
            InstantiateArt(Island + "Wind.prefab", "MoonBoatWind", new Vector3(179f, 1f, 0f),
                0.8f, 14, new Color(0.72f, 0.9f, 1f));
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = SpriteBlock("Player · 별을 줍는 아이", new Vector3(-2f, -1.45f, 0f),
                new Vector2(0.72f, 1.2f), new Color(1f, 0.78f, 0.3f), 30);
            player.layer = 31;
            GameObject face = SpriteBlock("FaceGlow", Vector3.zero, new Vector2(0.46f, 0.42f),
                new Color(1f, 0.93f, 0.68f), 31);
            face.transform.SetParent(player.transform, false);
            face.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            GameObject scarf = SpriteBlock("StarScarf", Vector3.zero, new Vector2(0.72f, 0.13f),
                new Color(0.96f, 0.25f, 0.48f), 32);
            scarf.transform.SetParent(player.transform, false);
            scarf.transform.localPosition = new Vector3(-0.42f, -0.05f, 0f);
            GameObject umbrella = SpriteBlock("Umbrella", Vector3.zero, new Vector2(1.05f, 0.16f),
                new Color(0.35f, 0.78f, 1f), 29);
            umbrella.transform.SetParent(player.transform, false);
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

        private static void CreateChapterSystems(Camera camera, Transform player)
        {
            GameObject systems = new("@STAR NIGHT · 시스템 재난 런");
            MoonMillChapterBootstrap bootstrap = systems.AddComponent<MoonMillChapterBootstrap>();
            bootstrap.ConfigureGateLoop(true);
            systems.AddComponent<StarNightCombinationResolver>();
            systems.AddComponent<ChapterPlaytestTelemetry>();

            GameObject hudObject = new("@HUD · 밤의 기록");
            StarNightHUD hud = hudObject.AddComponent<StarNightHUD>();
            hud.SetFont(font);

            GameObject atmosphereObject = new("@ATMOSPHERE · 긴 별밤");
            StarNightAtmosphere atmosphere = atmosphereObject.AddComponent<StarNightAtmosphere>();
            atmosphere.Configure(camera, player, square);
            atmosphere.SetWorldBounds(new Vector2(-12f, 192f), new Vector2(-10f, 17f), 180);

            GameObject maru = SpriteBlock("Maru · 돌아갈 시간", new Vector3(187f, 7f, 0f),
                new Vector2(1.5f, 1.15f), new Color(1f, 0.25f, 0.48f), 35);
            CreateEar(maru.transform, -0.42f);
            CreateEar(maru.transform, 0.42f);
            WorldText("마루", new Vector3(187f, 8.2f, 0f), 1.8f, new Color(1f, 0.36f, 0.58f), 50);
            MaruDirector director = systems.AddComponent<MaruDirector>();
            director.Configure(maru.transform, new Vector3(187f, 7f, 0f));

            GameObject firstBellTrace = SpriteBlock("Bell 1 · 지붕의 발자국 흔적",
                new Vector3(174f, 5.25f, 0f), new Vector2(3.2f, 0.18f),
                new Color(1f, 0.62f, 0.28f, 0.55f), 33);
            firstBellTrace.transform.SetParent(world);
            firstBellTrace.SetActive(false);

            GameObject secondBellPresence = SpriteBlock("Bell 2 · 정거장 붉은 그림자",
                new Vector3(174f, 4.75f, 0f), new Vector2(5.6f, 0.28f),
                new Color(1f, 0.22f, 0.48f, 0.62f), 33);
            secondBellPresence.transform.SetParent(world);
            secondBellPresence.SetActive(false);

            GameObject gateClosingVisual = SpriteBlock("Bell 3 · 닫히는 별문 빛",
                new Vector3(181.8f, 0.25f, 0f), new Vector2(0.38f, 5.4f),
                new Color(1f, 0.18f, 0.42f, 0.72f), 38);
            gateClosingVisual.transform.SetParent(world);
            gateClosingVisual.SetActive(false);

            BellChasePresenter bellPresenter = systems.AddComponent<BellChasePresenter>();
            bellPresenter.Configure(director, firstBellTrace, secondBellPresence, gateClosingVisual);
        }

        private static void CreateGameplayObjects()
        {
            CreateFable("gear_smallable", "금 간 톱니", new Vector2(42f, -0.8f), new Vector2(0.9f, 0.9f),
                StarItemKind.ResidentProperty,
                FableTraits.Carryable | FableTraits.Resizable | FableTraits.Linkable | FableTraits.ResidentProperty,
                1f, Station + "Core.prefab", 0.24f, new Color(0.88f, 0.78f, 0.58f));
            CreateFable("moon_cake_01", "따뜻한 달떡", new Vector2(102f, -1.7f), new Vector2(1f, 0.75f),
                StarItemKind.General,
                FableTraits.Carryable | FableTraits.Resizable | FableTraits.Floatable | FableTraits.MoonCake,
                2f, Desert + "Desert Small Cube.prefab", 0.22f, new Color(1f, 0.7f, 0.26f));
            CreateFable("moon_cake_02", "보름 달떡", new Vector2(70f, 10.1f), new Vector2(1.05f, 0.75f),
                StarItemKind.General,
                FableTraits.Carryable | FableTraits.Resizable | FableTraits.Floatable | FableTraits.MoonCake,
                2.5f, Desert + "Desert Small Cube.prefab", 0.24f, new Color(1f, 0.82f, 0.35f));
            CreateFable(MoonMinePathCakePress.OreId, "별가루 광석", new Vector2(126f, -7.25f), new Vector2(0.95f, 0.85f),
                StarItemKind.General,
                FableTraits.Carryable | FableTraits.Resizable | FableTraits.Floatable,
                3f, Crystal + "Crystal.prefab", 0.31f, new Color(0.68f, 0.9f, 1f));
            CreateFable("wood_box_01", "달나무 상자", new Vector2(27f, -1.65f), new Vector2(1.1f, 1.1f),
                StarItemKind.General,
                FableTraits.Carryable | FableTraits.Resizable | FableTraits.Floatable | FableTraits.Breakable,
                0.7f, Station + "Small Box.prefab", 0.7f, new Color(0.72f, 0.5f, 0.35f));
            CreateFable("explosive_fruit_01", "톡톡별 열매", new Vector2(111f, -2.25f), new Vector2(0.85f, 0.85f),
                StarItemKind.General,
                FableTraits.Carryable | FableTraits.Resizable | FableTraits.Floatable | FableTraits.Explosive | FableTraits.Bouncy,
                3.2f, Desert + "Desert Mushroom.prefab", 0.22f, new Color(1f, 0.26f, 0.42f))
                .gameObject.AddComponent<StarNightHazard>();
            CreateFable("rare_bell_seed", "잠든 방울씨", new Vector2(160f, -4.25f), new Vector2(0.8f, 0.95f),
                StarItemKind.RareToy,
                FableTraits.Carryable | FableTraits.Resizable | FableTraits.LightReactive | FableTraits.RareToy,
                4f, Crystal + "Crystal.prefab", 0.3f, new Color(0.76f, 0.42f, 1f));

            GameObject mill = CreateStation("MoonMill · 멈춘 방앗간", new Vector2(38f, -1.25f), new Vector2(3.2f, 3.1f),
                Station + "Mashinery dump.prefab", 0.62f, new Color(0.66f, 0.78f, 0.96f));
            mill.AddComponent<MoonMillRepairStation>();
            WorldText("멈춘 달방앗간", new Vector3(38f, 1.2f, 0f), 1.8f, new Color(0.62f, 0.9f, 1f), 52);

            GameObject millPress = CreateStation("Route A · 새 길떡 틀", new Vector2(46f, -0.15f), new Vector2(1.7f, 1.8f),
                Station + "Core.prefab", 0.32f, new Color(1f, 0.78f, 0.32f));
            GateRouteObjective millObjective = millPress.AddComponent<GateRouteObjective>();
            millObjective.Configure("CH1_ROUTE_MILL");
            millPress.AddComponent<MoonMillPathCakePress>().Configure(millObjective);
            WorldText("A · 방앗간 수리 > 새 길떡", new Vector3(46f, 1.25f, 0f), 1.45f,
                new Color(1f, 0.82f, 0.4f), 52);

            GameObject minePress = CreateStation("Route B · 광산 길떡 틀", new Vector2(119f, -3.35f), new Vector2(1.7f, 1.8f),
                Station + "Core.prefab", 0.32f, new Color(0.5f, 0.82f, 1f));
            GateRouteObjective mineObjective = minePress.AddComponent<GateRouteObjective>();
            mineObjective.Configure("CH1_ROUTE_MINE");
            minePress.AddComponent<MoonMinePathCakePress>().Configure(mineObjective);
            WorldText("B · 깊은 광석 > 광산 길떡", new Vector3(119f, -1.9f, 0f), 1.45f,
                new Color(0.58f, 0.86f, 1f), 52);

            GameObject winterStorage = CreateStation("Route C · 겨울 저장고 장부", new Vector2(136f, -3.35f), new Vector2(2.1f, 1.9f),
                Crystal + "Crystal.prefab", 0.34f, new Color(0.65f, 0.9f, 1f));
            GateRouteObjective storageObjective = winterStorage.AddComponent<GateRouteObjective>();
            storageObjective.Configure("CH1_ROUTE_STORAGE");
            winterStorage.AddComponent<MoonMillWinterStorage>().Configure(storageObjective);
            WorldText("C · 겨울 길떡 차용 · 장착 전 반환 가능", new Vector3(136f, -1.8f, 0f), 1.35f,
                new Color(0.7f, 0.9f, 1f), 52);

            GameObject pedestal = CreateStation("StarGateHub · 달토끼 별문", new Vector2(163f, -0.2f), new Vector2(1.8f, 2.2f),
                Station + "Core.prefab", 0.4f, new Color(1f, 0.62f, 0.2f));
            pedestal.AddComponent<StarGateController>();
            TextMeshPro gateStatus = WorldText("달토끼 별문 · 길떡 0/2", new Vector3(163f, 1.8f, 0f), 2f,
                new Color(1f, 0.82f, 0.35f), 52);
            pedestal.AddComponent<StarGateWorldStatus>().Configure(gateStatus);

            GameObject temptation = CreateStation("TemptationDoor · 달 뒤편 창고", new Vector2(153f, -1.25f), new Vector2(1.8f, 3.2f),
                Station + "Door.prefab", 0.66f, new Color(0.76f, 0.34f, 0.72f));
            GameObject temptationBarrier = SpriteBlock("GateActive Barrier · 달 뒤편 봉인",
                new Vector3(148.2f, -3f, 0f), new Vector2(1.15f, 4.8f),
                new Color(0.75f, 0.2f, 0.65f, 0.68f), 32);
            temptationBarrier.transform.SetParent(world);
            temptationBarrier.layer = 7;
            temptationBarrier.AddComponent<BoxCollider2D>();
            temptation.AddComponent<MoonMillTemptationDoor>().Configure(temptationBarrier);
            WorldText("선택 · 별문 가동 후 출항을 미루고 들어가는 위험 창고", new Vector3(153f, 1.1f, 0f), 1.35f,
                new Color(1f, 0.38f, 0.7f), 52);

            GameObject departure = CreateStation("MoonBoat · 달배", new Vector2(180f, 0f), new Vector2(3.8f, 1.8f),
                Island + "platform wt.prefab", 0.68f, new Color(0.55f, 0.9f, 1f));
            departure.AddComponent<MoonMillDepartureGate>();
            WorldText("달배 · 다음 별로", new Vector3(180f, 1.8f, 0f), 2f, new Color(0.55f, 0.92f, 1f), 54);

            GameObject rabbit = CreateStation("RabbitMiller · 방앗간지기", new Vector2(2.5f, -1.55f), new Vector2(1.1f, 1.55f),
                Forest + "Bushes B.prefab", 0.18f, new Color(0.95f, 0.86f, 0.74f));
            rabbit.AddComponent<MoonRabbitWitness>();
            rabbit.AddComponent<MaruNpcTarget>().Configure("Rabbit_Miller", "방앗간지기 묘월", 12f);
            CreateEar(rabbit.transform, -0.24f);
            CreateEar(rabbit.transform, 0.24f);
            WorldText("방앗간지기 묘월", new Vector3(2.5f, 0.1f, 0f), 1.55f,
                new Color(0.95f, 0.86f, 0.75f), 54);
        }

        private static FableObject CreateFable(string id, string label, Vector2 position, Vector2 colliderSize,
            StarItemKind kind, FableTraits traits, float scent, string artPath, float artScale, Color tint)
        {
            GameObject item = new($"{label} [{id}]");
            item.transform.SetParent(world);
            item.transform.position = position;
            BoxCollider2D collider = item.AddComponent<BoxCollider2D>();
            collider.size = colliderSize;
            Rigidbody2D body = item.AddComponent<Rigidbody2D>();
            body.gravityScale = 1.8f;
            FableObject fable = item.AddComponent<FableObject>();
            fable.Configure(id, label, kind, traits, scent);
            InstantiateArt(artPath, "BundleArt", Vector3.zero, artScale, 24, tint, item.transform, true);
            WorldText(label, new Vector3(position.x, position.y + 1.15f, 0f), 1.25f, Color.white, 55);
            return fable;
        }

        private static GameObject CreateStation(string objectName, Vector2 position, Vector2 size,
            string artPath, float artScale, Color tint)
        {
            GameObject station = new(objectName);
            station.transform.SetParent(world);
            station.transform.position = position;
            BoxCollider2D collider = station.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.isTrigger = true;
            InstantiateArt(artPath, "BundleArt", Vector3.zero, artScale, 20, tint, station.transform, true);
            return station;
        }

        private static void CreateCheckpoints()
        {
            CreateCheckpoint("첫 달등불", new Vector2(1f, -1.55f));
            CreateCheckpoint("방앗간 중심 등불", new Vector2(52f, 0.65f));
            CreateCheckpoint("달떡 창고 등불", new Vector2(101f, -1.45f));
            CreateCheckpoint("별가루 지하 등불", new Vector2(137f, -3.35f));
            CreateCheckpoint("달배 선착장 등불", new Vector2(173f, 0.55f));
        }

        private static void CreateCheckpoint(string label, Vector2 position)
        {
            GameObject checkpoint = new($"Checkpoint · {label}");
            checkpoint.transform.SetParent(world);
            checkpoint.transform.position = position;
            BoxCollider2D trigger = checkpoint.AddComponent<BoxCollider2D>();
            trigger.size = new Vector2(1.5f, 2.5f);
            trigger.isTrigger = true;
            StarNightCheckpoint component = checkpoint.AddComponent<StarNightCheckpoint>();
            component.Configure(label);
            InstantiateArt(Forest + "light.prefab", "MoonLampArt", Vector3.zero, 0.42f, 27,
                new Color(1f, 0.72f, 0.3f), checkpoint.transform, true);
        }

        private static void CreateDiscoveryZone(string id, string label, Vector2 position, Vector2 size, bool optional)
        {
            GameObject zone = new($"Discovery · {label}");
            zone.transform.SetParent(world);
            zone.transform.position = position;
            BoxCollider2D collider = zone.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.isTrigger = true;
            StarNightDiscoveryZone discovery = zone.AddComponent<StarNightDiscoveryZone>();
            discovery.Configure(id, label, optional);
        }

        private static void CreateWorldGuide()
        {
            WorldText("별을 물어오는 밤", new Vector3(0f, 7.25f, 0f), 3.4f,
                new Color(1f, 0.78f, 0.3f), 58);
            WorldText("세 경로 중 두 곳만 선택 · 길떡 2개를 별문에 직접 장착", new Vector3(0f, 6.05f, 0f), 1.45f,
                new Color(0.72f, 0.82f, 1f), 57);
            WorldText("A 안전·협력   B 위험·탐색   C 빠름·차용", new Vector3(0f, 5.15f, 0f), 1.25f,
                new Color(1f, 0.72f, 0.42f), 57);
            WorldText("A/D 이동  Space 점프  X 줍기/상호작용  E 절구  Q 크기  B 우산  G 내려놓기", new Vector3(0f, 4.25f, 0f), 1.15f,
                new Color(0.86f, 0.9f, 1f), 57);
        }

        private static void CreateWorldBoundaries()
        {
            CreateCollisionPlatform("WorldBottom", new Vector2(90f, -12.6f), new Vector2(205f, 1f), collisionRoot);
            CreateCollisionPlatform("LeftBoundary", new Vector2(-7f, 1f), new Vector2(0.6f, 28f), collisionRoot);
            CreateCollisionPlatform("RightBoundary", new Vector2(188f, 1f), new Vector2(0.6f, 28f), collisionRoot);
        }

        private static GameObject CreateCollisionPlatform(string objectName, Vector2 position, Vector2 size, Transform parent)
        {
            GameObject platform = new(objectName);
            platform.transform.SetParent(parent);
            platform.transform.position = position;
            platform.layer = 7;
            BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
            collider.size = size;
            return platform;
        }

        private static GameObject InstantiateArt(string assetPath, string objectName, Vector3 position, float scale,
            int sortingOffset, Color tint, Transform parent = null, bool localPosition = false)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Star Night] Missing bundle art: {assetPath}");
                return new GameObject($"MISSING · {objectName}");
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            instance.name = objectName;
            instance.transform.SetParent(parent != null ? parent : artRoot, true);
            if (localPosition)
            {
                instance.transform.localPosition = position;
                instance.transform.localRotation = Quaternion.identity;
            }
            else
            {
                instance.transform.position = position;
            }
            instance.transform.localScale = Vector3.one * scale;

            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
            }
            foreach (Collider2D collider in instance.GetComponentsInChildren<Collider2D>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
            foreach (Rigidbody2D body in instance.GetComponentsInChildren<Rigidbody2D>(true))
            {
                UnityEngine.Object.DestroyImmediate(body);
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

        private static Color Multiply(Color a, Color b)
        {
            return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
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

        private static void CreateEar(Transform parent, float x)
        {
            GameObject ear = SpriteBlock("Ear", Vector3.zero, new Vector2(0.28f, 0.75f),
                new Color(1f, 0.45f, 0.62f), 36);
            ear.transform.SetParent(parent, false);
            ear.transform.localPosition = new Vector3(x, 0.75f, 0f);
            ear.transform.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? 12f : -12f);
        }

        private static TextMeshPro WorldText(string value, Vector3 position, float size, Color color, int sortingOrder)
        {
            GameObject textObject = new($"Label · {value}");
            textObject.transform.SetParent(labelRoot);
            textObject.transform.position = position;
            TextMeshPro text = textObject.AddComponent<TextMeshPro>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.sortingOrder = sortingOrder;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(11f, 1.2f);
            return text;
        }

        private static void ValidateAssets()
        {
            if (square == null)
            {
                throw new FileNotFoundException("Square sprite was not found.", SquarePath);
            }
            if (font == null)
            {
                throw new FileNotFoundException("Korean TMP font was not found.", FontPath);
            }

            string[] required =
            {
                Forest + "Platform A.prefab",
                Forest + "Big Tree A.prefab",
                Station + "Platform A.prefab",
                Station + "Mashinery dump.prefab",
                Crystal + "Crystal platform A.prefab",
                Crystal + "Background normal.prefab",
                OldForest + "Old ground 9slice.prefab",
                Island + "platform wt.prefab"
            };
            foreach (string path in required)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    throw new FileNotFoundException("Required 2D Fantasy bundle prefab was not found.", path);
                }
            }
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
