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
    public static class StarNightP3StarPostOfficeBuilder
    {
        private const string SceneFolder = "Assets/Scenes/StarNight";
        private const string ScenePath = SceneFolder + "/StarNight_StarPostOffice.unity";
        private const string SquarePath = "Assets/Resources/Sprites/Square.png";
        private const string FontPath = "Assets/TextMesh Pro/Fonts/NeoDunggeunmoPro-Regular.asset";

        private const string Station = "Assets/2D Fantasy sprite bundle/Abandoned station/Prefabs/";
        private const string Crystal = "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/Crystal prefabs/";
        private const string Dungeon = "Assets/2D Fantasy sprite bundle/Dungeon pack/Prefabs/";
        private const string Island = "Assets/2D Fantasy sprite bundle/Island pack/Prefabs/";
        private const string Desert = "Assets/2D Fantasy sprite bundle/Desert pack/Prefabs/";
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

        [MenuItem("Tools/Star Night/Build P3 Star Post Office")]
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
                Debug.LogError("[Star Night P3] Square sprite or Korean TMP font is missing.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "StarNight_StarPostOffice";
            world = new GameObject("WORLD · 별 우체국").transform;
            artRoot = ChildRoot("ART · 2D Fantasy Postal Halls", world);
            collisionRoot = ChildRoot("COLLISION · Stable Mail Route", world);
            gameplayRoot = ChildRoot("GAMEPLAY · Address And Delivery", world);
            labelRoot = ChildRoot("ROOM TITLES", world);

            Camera camera = CreateCamera();
            CreateMainLight();
            CreateBackdrop();
            List<Room> rooms = CreateRooms();
            CreateMainRoute(rooms);
            Transform shortcut = CreateRainShortcut();
            CreateReturnVault();
            CreatePostalLandmarks();
            GameObject player = CreatePlayer();
            CreateSystems(camera, player.transform);
            CreateAddressesAndTraining();
            CreateLetterStory();
            CreateSorterClimax();
            CreateInheritedConsequences(shortcut);
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
            Debug.Log("[Star Night M3-2] Star Post Office built: 16 rooms, 3 address routes, manual star gate, separated truth vault.");
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
            camera.backgroundColor = new Color(0.025f, 0.018f, 0.08f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateMainLight()
        {
            GameObject lightObject = new("Postal Starlight · Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.85f, 0.8f, 1f);
            light.intensity = 0.68f;
            lightObject.transform.rotation = Quaternion.Euler(34f, -28f, 0f);
        }

        private static void CreateBackdrop()
        {
            for (int i = 0; i < 4; i++)
            {
                InstantiateArt(Station + "-Background-.prefab", $"PostalBackground_{i}",
                    new Vector3(i * 48f, 0f, 8f), 1.25f, -65,
                    new Color(0.42f, 0.46f, 0.82f));
                InstantiateArt(Crystal + "Background blur.prefab", $"AddressNebula_{i}",
                    new Vector3(18f + i * 44f, -1f, 7f), 1.55f, -54,
                    new Color(0.55f, 0.42f, 0.85f));
            }
            InstantiateArt(Crystal + "Stars Particle.prefab", "SortingStars",
                new Vector3(92f, 3f, 0f), 1.8f, -20, new Color(1f, 0.62f, 0.88f));
            InstantiateArt(Station + "Particle System Fog.prefab", "MailRouteFog",
                new Vector3(74f, 1f, 0f), 1.4f, -18, new Color(0.5f, 0.78f, 1f));
        }

        private static List<Room> CreateRooms()
        {
            return new List<Room>
            {
                new("arrival", "바람선 도착 우편대", 0f, 0f),
                new("blank_stamp", "빈 주소 교실", 10f, 0.5f),
                new("moon_box", "달 상자 실습실", 20f, 1.2f),
                new("mailbox_hall", "행성 우체통 회랑", 30f, 0f),
                new("dry_counter", "마른 잉크 창구", 40f, 1.3f),
                new("sorter_yard", "자동 분류기 앞뜰", 50f, 0f),
                new("return_slope", "반송 우편 경사로", 60f, 2f),
                new("lost_parcel", "분실 소포 환승실", 70f, 0.8f),
                new("last_letter", "수신자 없는 편지실", 80f, 0f),
                new("rani_mailbox", "라니 수신함", 90f, 1.8f),
                new("wet_repair", "젖은 주소 복구실", 100f, 0f, true),
                new("dead_letter", "수신자를 잃은 편지 보관소", 110f, 1.2f),
                new("sorter_core", "폭주 분류실", 120f, 0f),
                new("bird_nest", "거대 새 둥지 오배송실", 130f, 2f, true),
                new("route_registry", "북극성 항로 등록소", 140f, 0.8f),
                new("departure", "해님 정원행 발송대", 150f, 0f)
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
                string art = i % 3 == 0 ? "Platform with ropes.prefab" :
                    i % 3 == 1 ? "Platform A.prefab" : "Platform B.prefab";
                InstantiateArt(Station + art, $"FloorArt_{room.id}",
                    new Vector3(room.x, floorY + 0.28f, 0f), 0.62f, -6,
                    room.optional ? new Color(0.85f, 0.5f, 1f) : new Color(0.7f, 0.82f, 1f));
                WorldText(room.label, new Vector3(room.x, room.y + 2.8f, 0f), 1.22f,
                    room.optional ? new Color(0.98f, 0.52f, 0.88f) : new Color(1f, 0.8f, 0.32f),
                    56, labelRoot);
                CreateDiscovery(room.id, room.label, new Vector2(room.x, room.y), room.optional);

                if (i >= rooms.Count - 1)
                {
                    continue;
                }

                Room next = rooms[i + 1];
                Vector2 connector = new((room.x + next.x) * 0.5f,
                    Mathf.Min(floorY, next.y - 2.7f) + 0.4f);
                CreateCollisionPlatform($"Connector_{i:00}", connector, new Vector2(2.8f, 0.45f), collisionRoot);
                InstantiateArt(Station + "Platform B.prefab", $"ConnectorArt_{i:00}",
                    new Vector3(connector.x, connector.y + 0.18f, 0f), 0.34f, -5,
                    new Color(0.62f, 0.74f, 1f));
            }
        }

        private static Transform CreateRainShortcut()
        {
            Transform branch = ChildRoot("BRANCH · 구루의 비구름 특급 통로", world);
            Vector2[] points =
            {
                new(46f, 5.2f), new(53f, 7.2f), new(60f, 9.4f),
                new(68f, 11.4f), new(76f, 13.2f), new(84f, 14.4f)
            };
            for (int i = 0; i < points.Length; i++)
            {
                CreateCollisionPlatform($"RainExpress_{i}", points[i], new Vector2(4.2f, 0.42f), branch);
                InstantiateArt(i % 2 == 0 ? Island + "platform wt.prefab" : Crystal + "Crystal platform B.prefab",
                    $"RainExpressArt_{i}", new Vector3(points[i].x, points[i].y + 0.18f, 0f),
                    i % 2 == 0 ? 0.42f : 0.3f, 6,
                    new Color(0.55f, 0.9f, 1f), branch);
                InstantiateArt(Chains + "Crystal chain.prefab", $"ExpressChain_{i}",
                    new Vector3(points[i].x, points[i].y + 3.2f, 0f), 0.36f, 5,
                    new Color(0.55f, 0.82f, 1f), branch);
            }

            GameObject kiosk = InteractionBlock("RainExpressKiosk · 보관소 특급 주소",
                new Vector2(84f, 15.5f), new Color(0.42f, 0.92f, 1f), branch);
            StarSelfMailKiosk selfMail = kiosk.AddComponent<StarSelfMailKiosk>();
            selfMail.Configure("VAULT", "수신자를 잃은 편지 보관소");
            WorldText("P2 결과 · 비구름 특급 주소", new Vector3(67f, 15.8f, 0f),
                1.12f, new Color(0.55f, 0.92f, 1f), 62, branch);
            return branch;
        }

        private static void CreateReturnVault()
        {
            Transform branch = ChildRoot("BRANCH · 반송 불가 보관소", world);
            Vector2[] points =
            {
                new(116f, 5f), new(121f, 7.6f), new(126f, 10f),
                new(132f, 12.2f), new(138f, 13.8f)
            };
            for (int i = 0; i < points.Length; i++)
            {
                CreateCollisionPlatform($"ReturnVaultStep_{i}", points[i], new Vector2(3.7f, 0.42f), branch);
                InstantiateArt(Dungeon + "Small platform.prefab", $"ReturnVaultArt_{i}",
                    new Vector3(points[i].x, points[i].y + 0.2f, 0f), 0.56f, 6,
                    new Color(0.72f, 0.52f, 0.92f), branch);
            }

            GameObject barrier = SpriteBlock("ReturnVaultBarrier", new Vector3(128f, 10.6f, 0f),
                new Vector2(0.6f, 5.2f), new Color(0.5f, 0.12f, 0.42f), 34, branch);
            barrier.layer = 7;
            barrier.AddComponent<BoxCollider2D>();
            GameObject opener = InteractionBlock("ReturnVaultDoor · 반송 불가 주소",
                new Vector2(124f, 8.7f), new Color(1f, 0.42f, 0.72f), branch);
            StarReturnVaultDoor door = opener.AddComponent<StarReturnVaultDoor>();
            door.Configure(barrier);

            GameObject reward = InteractionBlock("RareReturnStamp · 되돌아오는 희귀 우표",
                new Vector2(138f, 15f), new Color(0.55f, 0.9f, 1f), branch);
            reward.AddComponent<StarRareStampUpgrade>();
            GameObject truth = InteractionBlock("RaniTruthArchive · 라니 명령 전체 기록",
                new Vector2(133f, 13.3f), new Color(1f, 0.48f, 0.76f), branch);
            truth.AddComponent<StarPostTruthArchive>().Configure(StarPostTruthArchiveMode.FullContext);
            WorldText("선택 진실 · 동생 실종 직후 내려진 귀가 명령 원본", new Vector3(132f, 16.2f, 0f),
                1.05f, new Color(1f, 0.52f, 0.82f), 62, branch);
            CreateDiscovery("return_vault", "반송 불가 보관소", new Vector2(132f, 12f), true);
        }

        private static void CreatePostalLandmarks()
        {
            InstantiateArt(Station + "Window.prefab", "GalaxyMailWindow",
                new Vector3(30f, 0.2f, 0f), 0.8f, -2, new Color(0.68f, 0.8f, 1f));
            InstantiateArt(Station + "Core.prefab", "AutomaticSorterCore",
                new Vector3(119f, -0.2f, 0f), 0.82f, 8, new Color(0.88f, 0.48f, 1f));
            InstantiateArt(Dungeon + "fog white.prefab", "DeadLetterFog",
                new Vector3(108f, 1f, 0f), 1.2f, -8, new Color(0.6f, 0.65f, 1f));
            InstantiateArt(Desert + "Clouds.prefab", "MisdeliveryCloud",
                new Vector3(130f, 5f, 2f), 0.85f, -14, new Color(0.75f, 0.62f, 0.9f));
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = SpriteBlock("Player · 별을 줍는 아이", new Vector3(-2f, -1.4f, 0f),
                new Vector2(0.72f, 1.2f), new Color(1f, 0.78f, 0.3f), 44, world);
            player.layer = 31;
            GameObject scarf = SpriteBlock("RedThreadScarf", Vector3.zero, new Vector2(0.9f, 0.12f),
                new Color(0.95f, 0.12f, 0.32f), 46, player.transform);
            scarf.transform.localPosition = new Vector3(-0.5f, 0.05f, 0f);
            GameObject stamp = SpriteBlock("StarPostalStamp", Vector3.zero, new Vector2(0.36f, 0.36f),
                new Color(0.72f, 0.48f, 1f), 47, player.transform);
            stamp.transform.localPosition = new Vector3(0.52f, 0.05f, 0f);

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
            GameObject systems = new("@STAR NIGHT P3 · 주소와 배송");
            StarPostOfficeChapterBootstrap bootstrap = systems.AddComponent<StarPostOfficeChapterBootstrap>();
            bootstrap.ConfigureGateLoop(true);
            systems.AddComponent<StarNightCombinationResolver>();
            systems.AddComponent<ChapterPlaytestTelemetry>();

            GameObject hudObject = new("@HUD · 라니 통신과 마지막 편지");
            StarNightHUD hud = hudObject.AddComponent<StarNightHUD>();
            hud.SetFont(font);

            GameObject atmosphereObject = new("@ATMOSPHERE · 우편 별빛 경로");
            StarNightAtmosphere atmosphere = atmosphereObject.AddComponent<StarNightAtmosphere>();
            atmosphere.Configure(camera, player, square);
            atmosphere.SetWorldBounds(new Vector2(-14f, 166f), new Vector2(-11f, 20f), 190);

            GameObject maru = SpriteBlock("Maru · 편지를 노리는 개", new Vector3(160f, 8f, 0f),
                new Vector2(1.6f, 1.15f), new Color(1f, 0.24f, 0.48f), 52, world);
            CreateEar(maru.transform, -0.42f);
            CreateEar(maru.transform, 0.42f);
            MaruDirector director = systems.AddComponent<MaruDirector>();
            director.Configure(maru.transform, new Vector3(160f, 8f, 0f));

            GameObject firstBellTrace = SpriteBlock("Bell 1 · 번지는 주소 도장",
                new Vector3(141f, 4.9f, 0f), new Vector2(7.8f, 0.18f),
                new Color(0.9f, 0.42f, 1f, 0.68f), 34, world);
            firstBellTrace.SetActive(false);
            GameObject secondBellPresence = SpriteBlock("Bell 2 · 배송을 가로채는 붉은 주소",
                new Vector3(132f, 4.1f, 0f), new Vector2(6.4f, 0.3f),
                new Color(1f, 0.18f, 0.45f, 0.74f), 35, world);
            secondBellPresence.SetActive(false);
            GameObject gateClosingVisual = SpriteBlock("Bell 3 · 이동하는 우체통 별문",
                new Vector3(144f, 0f, 0f), new Vector2(0.42f, 5.8f),
                new Color(1f, 0.16f, 0.48f, 0.8f), 48, world);
            gateClosingVisual.SetActive(false);
            BellChasePresenter presenter = systems.AddComponent<BellChasePresenter>();
            presenter.Configure(director, firstBellTrace, secondBellPresence, gateClosingVisual);
        }

        private static void CreateAddressesAndTraining()
        {
            CreateAddress("MOON", "달 우체통", new Vector2(22f, -0.2f), new Color(0.72f, 0.82f, 1f));
            StarPostalAddress sorting = CreateAddress("SORTING", "자동 분류실", new Vector2(52f, -0.5f),
                new Color(0.76f, 0.5f, 1f));
            StarPostalAddress vault = CreateAddress("VAULT", "수신자를 잃은 편지 보관소", new Vector2(108f, -0.3f),
                new Color(0.52f, 0.88f, 1f));
            CreateAddress("RANI", "라니의 개인 수신함", new Vector2(92f, 0.3f),
                new Color(1f, 0.42f, 0.72f));
            CreateAddress("NEST", "거대 새 둥지", new Vector2(131f, 2.4f),
                new Color(1f, 0.4f, 0.38f), true);
            CreateAddress("ROUTE", "북극성 항로 등록소", new Vector2(142f, -0.2f),
                new Color(1f, 0.82f, 0.32f));

            FableObject regularParcel = CreateParcel("training_moon_box", "달 모양 연습 상자", new Vector2(13f, -0.6f),
                new Color(0.62f, 0.8f, 1f), 1.2f);
            CreateParcel("heavy_switch_parcel", "무거운 레버 소포", new Vector2(34f, -0.5f),
                new Color(0.64f, 0.5f, 0.35f), 2.8f);
            FableObject deadLetterParcel = CreateParcel("dead_letter_route_parcel",
                "수신자 없는 위험 소포", new Vector2(64f, 0.8f),
                new Color(0.92f, 0.48f, 0.3f), 1f);

            CreateDeliveryRouteTracker("Route A · 정규 우편 분류", "CH4_ROUTE_REGULAR_POST",
                regularParcel.ObjectId, "MOON", "CH4_ROUTE_REGULAR_COMPLETE");
            CreateDeliveryRouteTracker("Route B · 반송 불가 주소", "CH4_ROUTE_DEAD_LETTER",
                deadLetterParcel.ObjectId, vault.AddressId, "CH4_ROUTE_DEAD_LETTER_COMPLETE");
            WorldText("A 안전·추론 · 달 모양 상자 > 달 우체통", new Vector3(18f, 5.2f, 0f),
                1.05f, new Color(0.68f, 0.88f, 1f), 64, labelRoot);
            WorldText("B 위험·배송 · 수신자 없는 소포 > 반송 보관소", new Vector3(68f, 5.1f, 0f),
                1.05f, new Color(1f, 0.52f, 0.48f), 64, labelRoot);

            GameObject selfKiosk = InteractionBlock("SelfMailKiosk · 분류실행 사람 소포",
                new Vector2(28f, -0.5f), new Color(0.45f, 0.9f, 1f), gameplayRoot);
            StarSelfMailKiosk selfMail = selfKiosk.AddComponent<StarSelfMailKiosk>();
            selfMail.Configure(sorting.AddressId, sorting.DisplayName);

            GameObject vaultKiosk = InteractionBlock("VaultKiosk · 보관소행 사람 소포",
                new Vector2(72f, -0.3f), new Color(0.55f, 0.72f, 1f), gameplayRoot);
            StarSelfMailKiosk vaultMail = vaultKiosk.AddComponent<StarSelfMailKiosk>();
            vaultMail.Configure(vault.AddressId, vault.DisplayName);
        }

        private static void CreateLetterStory()
        {
            FableObject letter = CreateFable("rani_last_letter", "라니에게 보내진 마지막 편지",
                new Vector2(80f, -0.2f), new Vector2(0.85f, 0.58f),
                new Color(1f, 0.88f, 0.68f),
                FableTraits.Carryable | FableTraits.Deliverable | FableTraits.PostalParcel |
                FableTraits.LastLetter | FableTraits.ResidentProperty,
                0.3f, 1.2f, StarItemKind.ResidentProperty, 5f);
            GameObject seal = SpriteBlock("LetterSeal", Vector3.zero, new Vector2(0.23f, 0.23f),
                new Color(0.82f, 0.12f, 0.3f), 40, letter.transform);
            seal.transform.localPosition = Vector3.zero;
            GateRouteObjective sealObjective = letter.gameObject.AddComponent<GateRouteObjective>();
            sealObjective.Configure("CH4_ROUTE_SEALED_LETTER");

            GameObject open = InteractionBlock("LetterOpen · 봉인 칼", new Vector2(76.5f, -0.4f),
                new Color(1f, 0.46f, 0.4f), gameplayRoot);
            open.AddComponent<StarLetterDecision>().Configure(StarLetterDecisionMode.Open, letter);
            GameObject preserve = InteractionBlock("LetterPreserve · 보존함", new Vector2(83.5f, -0.4f),
                new Color(0.42f, 0.9f, 0.72f), gameplayRoot);
            preserve.AddComponent<StarLetterDecision>().Configure(StarLetterDecisionMode.Preserve, letter);
            GameObject dismantle = InteractionBlock("LetterDismantle · 주소 분해기", new Vector2(87f, -0.4f),
                new Color(0.85f, 0.34f, 1f), gameplayRoot);
            dismantle.AddComponent<StarLetterDecision>().Configure(StarLetterDecisionMode.Dismantle, letter);
            GameObject copySeal = InteractionBlock("Route C Copy · 봉인 주소 복사대",
                new Vector2(79f, 1.4f), new Color(0.42f, 0.92f, 0.82f), gameplayRoot);
            copySeal.AddComponent<StarLetterGateSeal>().Configure(
                StarLetterGateSealMode.CopyAddress, letter, sealObjective);
            GameObject useSeal = InteractionBlock("Route C Fast · 봉인 인장 압착기",
                new Vector2(85f, 1.4f), new Color(1f, 0.36f, 0.5f), gameplayRoot);
            useSeal.AddComponent<StarLetterGateSeal>().Configure(
                StarLetterGateSealMode.UseSeal, letter, sealObjective);

            WorldText("C 빠름·사생활 · 주소만 복사 / 봉인을 직접 찍어 훼손",
                new Vector3(83f, 4.2f, 0f), 1.05f, new Color(1f, 0.68f, 0.84f), 64, labelRoot);

            GameObject fragment = InteractionBlock("RaniCommandFragment · 메인 통신 기록",
                new Vector2(96f, 0.2f), new Color(0.68f, 0.72f, 1f), gameplayRoot);
            fragment.AddComponent<StarPostTruthArchive>().Configure(
                StarPostTruthArchiveMode.CommandFragment);
            WorldText("필수 중반 기록 · “떠난 아이들을 모두 집으로 데려와.”",
                new Vector3(96f, 5.2f, 0f), 0.98f, new Color(0.75f, 0.82f, 1f), 64, labelRoot);
        }

        private static void CreateSorterClimax()
        {
            StarPostalAddress sorting = FindAddressInScene("SORTING");
            StarPostalAddress vault = FindAddressInScene("VAULT");
            StarPostalAddress nest = FindAddressInScene("NEST");
            FableObject[] parcels =
            {
                CreateParcel("sorter_parcel_a", "별비의 분실 상자", new Vector2(115f, 0.5f),
                    new Color(0.5f, 0.82f, 1f), 1.1f),
                CreateParcel("sorter_parcel_b", "젖은 소포", new Vector2(120f, 2.2f),
                    new Color(0.48f, 0.62f, 0.9f), 1.3f),
                CreateParcel("sorter_parcel_c", "되돌아오는 상자", new Vector2(124f, 0.4f),
                    new Color(0.82f, 0.5f, 1f), 1.5f)
            };
            StarPostalAddress[] addresses = { sorting, vault, nest };

            GameObject overload = InteractionBlock("SorterOverload · 강제 분류 레버",
                new Vector2(117f, -0.6f), new Color(1f, 0.34f, 0.5f), gameplayRoot);
            overload.AddComponent<StarSorterController>().Configure(StarSorterMode.Overload, parcels, addresses);
            GameObject repair = InteractionBlock("SorterRepair · 주소 복구대",
                new Vector2(123f, -0.6f), new Color(0.42f, 0.9f, 0.75f), gameplayRoot);
            repair.AddComponent<StarSorterController>().Configure(StarSorterMode.Repair, parcels, addresses);

            FableObject routeStamp = CreateFable("polaris_route_stamp", "북극성 항로 도장",
                new Vector2(110f, 0.6f), new Vector2(0.7f, 0.7f),
                new Color(1f, 0.82f, 0.28f),
                FableTraits.Carryable | FableTraits.Deliverable | FableTraits.PostalParcel |
                FableTraits.RouteStamp,
                0.5f, 1.2f, StarItemKind.DepartureSupply, 2f);
            routeStamp.gameObject.AddComponent<StarRouteStampRecovery>().Configure(routeStamp);
            WorldText("X · 북극성 항로 도장 회수", new Vector3(110f, 2.4f, 0f),
                1.02f, new Color(1f, 0.84f, 0.32f), 64, labelRoot);
        }

        private static void CreateInheritedConsequences(Transform shortcut)
        {
            GameObject dry = SpriteBlock("DryInk · 가뭄으로 굳은 잉크", new Vector3(39f, -0.5f, 0f),
                new Vector2(1.4f, 0.65f), new Color(0.68f, 0.42f, 0.22f), 38, gameplayRoot);
            GameObject wet = SpriteBlock("WetLetters · 폭풍에 젖은 주소표", new Vector3(99f, -0.5f, 0f),
                new Vector2(1.6f, 0.7f), new Color(0.32f, 0.55f, 0.9f), 38, gameplayRoot);
            GameObject stamp = InteractionBlock("CloudStampReward · 목장 복구 구름 우표",
                new Vector2(6f, -0.4f), new Color(0.4f, 0.88f, 1f), gameplayRoot);
            stamp.AddComponent<StarRareStampUpgrade>();

            GameObject displayObject = new("P2 Consequence Display");
            displayObject.transform.SetParent(gameplayRoot);
            StarPostInheritedDisplay display = displayObject.AddComponent<StarPostInheritedDisplay>();
            display.Configure(dry, wet, shortcut.gameObject, stamp);
        }

        private static void CreateDeparture()
        {
            InstantiateArt(Station + "Door.prefab", "SunGardenMailshipDoor",
                new Vector3(151f, -0.2f, 0f), 0.72f, 10, new Color(0.75f, 0.62f, 1f));
            GameObject gate = InteractionBlock("DepartureGate · 해님 정원행 우편선",
                new Vector2(151f, -0.5f), new Color(1f, 0.78f, 0.3f), gameplayRoot);
            gate.transform.localScale = new Vector3(1.3f, 1.7f, 1f);
            gate.GetComponent<CircleCollider2D>().radius = 1.5f;
            gate.AddComponent<StarPostOfficeDepartureGate>();
            WorldText("별문 가동 후 즉시 출항 · 메인 기록 일부는 필수", new Vector3(150f, 3.4f, 0f),
                1.2f, new Color(1f, 0.82f, 0.35f), 62, labelRoot);

            GameObject starGate = InteractionBlock("StarGateHub · 별 우체국 별문",
                new Vector2(144f, -0.2f), new Color(1f, 0.54f, 0.3f), gameplayRoot);
            starGate.transform.localScale = new Vector3(1.4f, 1.8f, 1f);
            starGate.AddComponent<StarGateController>();
            TMP_Text status = WorldText("별 우체국 별문 · 주소 0/2",
                new Vector3(143f, 2.1f, 0f), 1.55f,
                new Color(1f, 0.8f, 0.32f), 63, labelRoot);
            starGate.AddComponent<StarGateWorldStatus>().Configure(status, "별 우체국 별문", "주소");
        }

        private static void CreateDeliveryRouteTracker(string name, string routeId,
            string parcelId, string addressId, string completionFlag)
        {
            GameObject tracker = new(name);
            tracker.transform.SetParent(gameplayRoot);
            GateRouteObjective objective = tracker.AddComponent<GateRouteObjective>();
            objective.Configure(routeId);
            StarPostDeliveryRouteObjective deliveryObjective =
                tracker.AddComponent<StarPostDeliveryRouteObjective>();
            deliveryObjective.Configure(objective, parcelId, addressId, completionFlag);
        }

        private static StarPostalAddress CreateAddress(string id, string label, Vector2 position,
            Color color, bool dangerous = false)
        {
            GameObject mailbox = SpriteBlock($"{label} [{id}]", position,
                new Vector2(0.9f, 1.25f), color, 40, gameplayRoot);
            CircleCollider2D trigger = mailbox.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.15f;
            FableObject fable = mailbox.AddComponent<FableObject>();
            fable.Configure($"address_{id.ToLowerInvariant()}", label, StarItemKind.General,
                FableTraits.PostalAddress, dangerous ? 2f : 0.6f);
            StarPostalAddress address = mailbox.AddComponent<StarPostalAddress>();
            address.Configure(id, label, null, dangerous);
            GameObject slot = SpriteBlock("AddressSlot", Vector3.zero, new Vector2(0.46f, 0.12f),
                new Color(0.08f, 0.06f, 0.16f), 42, mailbox.transform);
            slot.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            WorldText($"{id} · {label}", position + Vector2.up * 1.55f,
                0.92f, color, 62, labelRoot);
            return address;
        }

        private static StarPostalAddress FindAddressInScene(string id)
        {
            return Object.FindObjectsByType<StarPostalAddress>(FindObjectsSortMode.None)
                .First(address => address.AddressId == id);
        }

        private static FableObject CreateParcel(string id, string label, Vector2 position,
            Color color, float mass)
        {
            return CreateFable(id, label, position, new Vector2(1f, 0.82f), color,
                FableTraits.Carryable | FableTraits.Deliverable | FableTraits.PostalParcel |
                FableTraits.Linkable | FableTraits.Floatable,
                mass, 1.8f, StarItemKind.General, Mathf.Max(0.6f, mass * 0.5f));
        }

        private static FableObject CreateFable(string id, string label, Vector2 position, Vector2 size,
            Color color, FableTraits traits, float mass, float gravity, StarItemKind kind, float scent)
        {
            GameObject item = SpriteBlock($"{label} [{id}]", position, size, color, 36, gameplayRoot);
            item.AddComponent<BoxCollider2D>();
            Rigidbody2D body = item.AddComponent<Rigidbody2D>();
            body.mass = Mathf.Max(0.1f, mass);
            body.gravityScale = gravity;
            body.angularDamping = 1.1f;
            FableObject fable = item.AddComponent<FableObject>();
            fable.Configure(id, label, kind, traits, scent);
            return fable;
        }

        private static void CreateCheckpoints()
        {
            Vector2[] points =
            {
                new(2f, -1.4f), new(38f, -0.8f), new(76f, -1.2f),
                new(113f, -0.2f), new(143f, -0.5f)
            };
            for (int i = 0; i < points.Length; i++)
            {
                GameObject lamp = SpriteBlock($"PostalLantern_{i + 1}", points[i],
                    new Vector2(0.42f, 0.72f), new Color(0.72f, 0.48f, 1f), 38, gameplayRoot);
                CircleCollider2D trigger = lamp.AddComponent<CircleCollider2D>();
                trigger.isTrigger = true;
                trigger.radius = 1.15f;
                StarNightCheckpoint checkpoint = lamp.AddComponent<StarNightCheckpoint>();
                checkpoint.Configure($"우편 별등 {i + 1}");
            }
        }

        private static void CreateGuide()
        {
            WorldText("제4장 · 주소를 잃은 별 우체국", new Vector3(2f, 7.1f, 0f),
                2.2f, new Color(1f, 0.82f, 0.3f), 64, labelRoot);
            WorldText("목표 · 세 경로 중 북극성 항로 주소 2조각을 별문에 장착",
                new Vector3(2f, 6.05f, 0f), 1.3f, new Color(0.72f, 0.62f, 1f), 64, labelRoot);
            WorldText("A 정규 분류 · B 반송 위험 · C 봉인 인장",
                new Vector3(2f, 5.1f, 0f), 1.08f, new Color(1f, 0.58f, 0.5f), 64, labelRoot);
            WorldText("R 도구 · E 소포와 주소 · X 상호작용 · 전체 진실은 가동 뒤 선택",
                new Vector3(2f, 4.15f, 0f), 0.95f, Color.white, 64, labelRoot);
            WorldText("마루는 배송 중인 마지막 편지를 가장 먼저 노린다",
                new Vector3(92f, 6.4f, 0f), 1.02f, new Color(1f, 0.48f, 0.72f), 64, labelRoot);
        }

        private static void CreateWorldBounds()
        {
            CreateCollisionPlatform("FallCatch", new Vector2(76f, -10.5f), new Vector2(176f, 0.8f), collisionRoot);
            CreateCollisionPlatform("LeftBoundary", new Vector2(-10f, 2f), new Vector2(0.8f, 28f), collisionRoot);
            CreateCollisionPlatform("RightBoundary", new Vector2(164f, 2f), new Vector2(0.8f, 28f), collisionRoot);
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
            tmp.rectTransform.sizeDelta = new Vector2(12f, 2f);
            return tmp;
        }

        private static GameObject InstantiateArt(string path, string name, Vector3 position, float scale,
            int sortingOffset, Color tint, Transform parent = null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[Star Night P3] Missing bundle art: {path}");
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
            int p2Index = scenes.FindIndex(scene =>
                scene.path == "Assets/Scenes/StarNight/StarNight_CloudWhaleRanch.unity");
            int insert = p2Index >= 0 ? p2Index + 1 : scenes.Count;
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
