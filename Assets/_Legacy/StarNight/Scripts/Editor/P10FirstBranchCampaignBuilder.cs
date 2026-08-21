#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StarNight.Campaign.P10;
using StarNight.Debugging;
using StarNight.Explosions;
using StarNight.Folklore.P9;
using StarNight.Generation.P6;
using StarNight.Grid;
using StarNight.Objects;
using StarNight.Player;
using StarNight.Rooms;
using StarNight.Stages.P5;
using StarNight.Tiles;
using StarNight.Tools;
using StarNight.Tools.Pestle;
using StarNight.Tools.Rope;
using StarNight.Tools.Water;
using StarNight.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Editor
{
    public static class P10FirstBranchCampaignBuilder
    {
        public const string ProductionScenePath =
            "Assets/StarNight/Scenes/Game/"
            + "P10_MoonPalace_FirstBranches.unity";
        public const string CatalogPath =
            "Assets/StarNight/Data/P10/"
            + "P10_FirstBranchCampaignCatalog.asset";

        private const string P9GuestCatalogPath =
            "Assets/StarNight/Data/P9/P9_RecordGuestCatalog.asset";
        private const string ReinforcedTilePath =
            "Assets/StarNight/Data/P4/RoomTiles/"
            + "P4_MoonReinforced.asset";
        private const string SoftSoilTilePath =
            "Assets/StarNight/Data/P4/RoomTiles/"
            + "P4_MoonSoftSoil.asset";
        private const string OneWayTilePath =
            "Assets/StarNight/Data/P4/RoomTiles/"
            + "P4_MoonOneWay.asset";
        private const string ReinforcedDefinitionPath =
            "Assets/StarNight/Data/P5/Tiles/"
            + "P5_MoonReinforced_Definition.asset";
        private const string SoftSoilDefinitionPath =
            "Assets/StarNight/Data/P5/Tiles/"
            + "P5_MoonSoftSoil_Definition.asset";

        private const int InheritedDoorHeight = 2;
        private const int MaxInheritedDoorSurface = 4;
        private const int InheritedMinimumRoomHeight = 8;
        private const float P5ShopAuthoredCenterX = 69.5f;

        private const string SquareSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/"
            + "Square.png";
        private const string StarSpritePath =
            "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/"
            + "Cristal Sprites/Star particle.png";
        private const string MoonSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/"
            + "Moon A.png";
        private const string TreeSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/"
            + "Mount tree.png";
        private const string BridgeSpritePath =
            "Assets/2D Fantasy sprite bundle/Spring forest/Sprites/"
            + "Ver 1.5/Bridge.png";
        private const string BirdSpritePath =
            "Assets/2D Fantasy sprite bundle/Island pack/Sprites/"
            + "birds.png";
        private const string CloudSpritePath =
            "Assets/2D Fantasy sprite bundle/Island pack/Sprites/"
            + "clouds.png";
        private const string UnderwaterBackgroundPath =
            "Assets/2D Fantasy sprite bundle/Underwater area pack/"
            + "Sprites/Underwater background.png";
        private const string UnderwaterGroundPath =
            "Assets/2D Fantasy sprite bundle/Underwater area pack/"
            + "Sprites/Underwater ground fill.png";
        private const string SeaweedSpritePath =
            "Assets/2D Fantasy sprite bundle/Underwater area pack/"
            + "Sprites/Seaweed.png";
        private const string FishSpritePath =
            "Assets/2D Fantasy sprite bundle/Underwater area pack/"
            + "Sprites/Fishs.png";
        private const string BubbleSpritePath =
            "Assets/2D Fantasy sprite bundle/Underwater area pack/"
            + "Sprites/Bubble particle.png";

        [MenuItem(
            "StarNight/P10/Rebuild Production First Branch Campaign")]
        public static void Rebuild()
        {
            P5MoonPalaceSliceBuilder.Rebuild();
            EnsureFolder(
                Path.GetDirectoryName(ProductionScenePath)
                    ?.Replace('\\', '/'));
            Scene source = SceneManager.GetActiveScene();
            if (!EditorSceneManager.SaveScene(
                    source,
                    ProductionScenePath,
                    true))
            {
                throw new InvalidOperationException(
                    "Failed to copy the P5 production scene for P10.");
            }

            Scene scene = EditorSceneManager.OpenScene(
                ProductionScenePath,
                OpenSceneMode.Single);
            P10CampaignCatalog catalog = RebuildCatalog();
            P9RecordGuestCatalog guestCatalog =
                AssetDatabase.LoadAssetAtPath<P9RecordGuestCatalog>(
                    P9GuestCatalogPath);
            if (guestCatalog == null)
            {
                throw new InvalidOperationException(
                    "P10 requires the completed P9 Record Guest catalog.");
            }

            BuildAssets assets = LoadAssets();
            ValidateAssets(assets);
            GameObject stageRoot = GameObject.Find(
                "P5_MoonPalace_1-1_CraterWorkshop");
            if (stageRoot == null)
            {
                throw new InvalidOperationException(
                    "The P5 production root is missing.");
            }

            stageRoot.name = "P10_MoonPalace_FirstBranches";
            P10FirstBranchCampaignContract contract =
                BuildProductionCampaign(
                    scene,
                    stageRoot,
                    catalog,
                    guestCatalog,
                    assets);
            if (!contract.RefreshValidation())
            {
                throw new InvalidOperationException(
                    "P10 production rebuild failed validation:"
                    + Environment.NewLine
                    + contract.LastValidation);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    ProductionScenePath))
            {
                throw new InvalidOperationException(
                    "Failed to save the P10 production scene.");
            }

            AddSceneToBuildSettings(ProductionScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[StarNight P10] Production campaign rebuilt: "
                + "one persistent core, nine staged environments, "
                + "two normal branches, and two optional cross routes.");
        }

        [MenuItem(
            "StarNight/P10/Validate Production First Branch Campaign")]
        public static void Validate()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path != ProductionScenePath)
            {
                EditorSceneManager.OpenScene(
                    ProductionScenePath,
                    OpenSceneMode.Single);
            }

            P10FirstBranchCampaignContract[] contracts =
                UnityEngine.Object.FindObjectsByType<
                    P10FirstBranchCampaignContract>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            if (contracts.Length != 1)
            {
                throw new InvalidOperationException(
                    "P10 production requires exactly one campaign contract.");
            }

            contracts[0].ValidateOrThrow();
            EditorSceneManager.MarkSceneDirty(
                contracts[0].gameObject.scene);
            EditorSceneManager.SaveScene(
                contracts[0].gameObject.scene);
            Debug.Log(
                "[StarNight P10] Production validation PASS. "
                + "30-40 minute timing and branch-feel gates remain "
                + "human playtests.");
        }

        private static P10FirstBranchCampaignContract
            BuildProductionCampaign(
                Scene scene,
                GameObject root,
                P10CampaignCatalog catalog,
                P9RecordGuestCatalog guestCatalog,
                BuildAssets assets)
        {
            Transform[] originalChildren =
                root.transform.Cast<Transform>().ToArray();
            PlayerMotor2D motor =
                UnityEngine.Object.FindFirstObjectByType<PlayerMotor2D>(
                    FindObjectsInactive.Include);
            PlayerInputAdapter input =
                UnityEngine.Object.FindFirstObjectByType<
                    PlayerInputAdapter>(
                    FindObjectsInactive.Include);
            Camera camera =
                UnityEngine.Object.FindFirstObjectByType<Camera>(
                    FindObjectsInactive.Include);
            Light light =
                UnityEngine.Object.FindObjectsByType<Light>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .FirstOrDefault(item =>
                        item.type == LightType.Directional);
            P5MoonRabbitPestleEvent2D rabbitEvent =
                UnityEngine.Object.FindFirstObjectByType<
                    P5MoonRabbitPestleEvent2D>(
                    FindObjectsInactive.Include);
            GridWorld moonWorld =
                UnityEngine.Object.FindFirstObjectByType<GridWorld>(
                    FindObjectsInactive.Include);
            if (motor == null
                || input == null
                || camera == null
                || light == null
                || rabbitEvent == null
                || moonWorld == null)
            {
                throw new InvalidOperationException(
                    "The copied P5 production core is incomplete.");
            }

            GameObject persistent =
                CreateChild(root.transform, "PersistentCampaignCore");
            MoveUnderPersistent(
                GameObject.Find("CoreStageRoot"),
                persistent.transform);
            MoveUnderPersistent(
                motor.gameObject,
                persistent.transform);
            MoveUnderPersistent(
                camera.gameObject,
                persistent.transform);
            MoveUnderPersistent(
                light.gameObject,
                persistent.transform);

            GameObject environmentsRoot =
                CreateChild(root.transform, "StageEnvironments_9");
            GameObject registryRoot =
                CreateChild(root.transform, "StageRegistry_9");
            GameObject moon11Root =
                CreateChild(
                    environmentsRoot.transform,
                    "Environment_MoonPalace11_CraterWorkshop");
            for (int index = 0;
                 index < originalChildren.Length;
                 index++)
            {
                Transform child = originalChildren[index];
                if (child != null
                    && child.parent == root.transform
                    && child != persistent.transform)
                {
                    child.SetParent(moon11Root.transform, true);
                }
            }

            Transform p8System =
                FindTransform(moon11Root.transform, "P8MaruSystem");
            Transform p8Runtime =
                p8System != null ? p8System.Find("Runtime") : null;
            if (p8Runtime == null)
            {
                throw new InvalidOperationException(
                    "The inherited P8 Maru runtime root is missing.");
            }

            p8Runtime.SetParent(persistent.transform, true);

            P5StageExit2D inheritedExit =
                moon11Root.GetComponentInChildren<P5StageExit2D>(true);
            if (inheritedExit != null)
            {
                inheritedExit.enabled = false;
            }

            GameObject systems =
                CreateChild(persistent.transform, "P10Systems");
            P9FolkloreChainState2D folklore =
                systems.AddComponent<P9FolkloreChainState2D>();
            folklore.Configure(false, false);
            P10RouteTelemetry2D telemetry =
                systems.AddComponent<P10RouteTelemetry2D>();
            P10BranchSupportState2D support =
                systems.AddComponent<P10BranchSupportState2D>();
            P10CampaignDirector2D director =
                systems.AddComponent<P10CampaignDirector2D>();
            director.Configure(catalog, folklore, telemetry, support);
            P10StageFlowController2D flow =
                systems.AddComponent<P10StageFlowController2D>();
            P10MoonGiftBridge2D giftBridge =
                systems.AddComponent<P10MoonGiftBridge2D>();

            List<P10StageEnvironment2D> environments =
                new List<P10StageEnvironment2D>(9);
            P10StageEnvironment2D moon11 =
                ConfigureInheritedMoonEnvironment(
                    moon11Root,
                    moonWorld,
                    assets);
            environments.Add(moon11);
            P10StageId[] remainingStageIds =
            {
                P10StageId.MoonPalace12,
                P10StageId.MoonPalace13,
                P10StageId.MagpieBridge21,
                P10StageId.MagpieBridge22,
                P10StageId.MagpieBridge23,
                P10StageId.DragonPalace21,
                P10StageId.DragonPalace22,
                P10StageId.DragonPalace23
            };
            for (int index = 0;
                 index < remainingStageIds.Length;
                 index++)
            {
                P10StageDefinition definition =
                    catalog.Find(remainingStageIds[index]);
                environments.Add(
                    CreateStageEnvironment(
                        environmentsRoot.transform,
                        definition,
                        assets,
                        motor.GetComponent<Collider2D>()));
            }

            P10StageNode2D[] nodes = new P10StageNode2D[9];
            for (int index = 0; index < environments.Count; index++)
            {
                P10StageEnvironment2D environment =
                    environments[index];
                P10StageDefinition definition =
                    catalog.Find(environment.StageId);
                GameObject nodeObject =
                    CreateChild(
                        registryRoot.transform,
                        $"Node_{definition.StageId}_{definition.DisplayName}");
                P10StageNode2D node =
                    nodeObject.AddComponent<P10StageNode2D>();
                node.Configure(definition, director, environment);
                nodes[index] = node;
            }

            flow.Configure(
                director,
                nodes,
                motor.transform,
                camera,
                assets.BombPrefab);
            giftBridge.Configure(rabbitEvent, folklore, director);
            PopulateCampaignEconomy(
                nodes,
                moon11Root,
                motor.transform,
                assets);

            List<P9CorrespondenceEvent2D> correspondence =
                new List<P9CorrespondenceEvent2D>(2);
            correspondence.Add(
                CreateCorrespondenceEvent(
                    FindNode(nodes, P10StageId.MagpieBridge21),
                    P9CorrespondenceEventKind.HungryMagpie,
                    folklore,
                    assets,
                    new Color(0.92f, 0.72f, 1f, 1f)));
            correspondence.Add(
                CreateCorrespondenceEvent(
                    FindNode(nodes, P10StageId.DragonPalace21),
                    P9CorrespondenceEventKind.InjuredTurtle,
                    folklore,
                    assets,
                    new Color(0.32f, 0.92f, 0.92f, 1f)));

            P10BranchStageEvent2D[] branchEvents =
            {
                CreateBranchStageEvent(
                    FindNode(nodes, P10StageId.MagpieBridge22),
                    P10BranchEventKind.RepairMagpieNest,
                    support,
                    assets),
                CreateBranchStageEvent(
                    FindNode(nodes, P10StageId.DragonPalace22),
                    P10BranchEventKind.RestoreCarpWaterway,
                    support,
                    assets)
            };

            List<P9StarArchive2D> archives =
                new List<P9StarArchive2D>(3);
            List<P9RecordGuestDirector2D> guestDirectors =
                new List<P9RecordGuestDirector2D>(3);
            CreateArchive(
                FindNode(nodes, P10StageId.MoonPalace12),
                guestCatalog,
                motor.transform,
                assets,
                archives,
                guestDirectors);
            CreateArchive(
                FindNode(nodes, P10StageId.MagpieBridge22),
                guestCatalog,
                motor.transform,
                assets,
                archives,
                guestDirectors);
            CreateArchive(
                FindNode(nodes, P10StageId.DragonPalace22),
                guestCatalog,
                motor.transform,
                assets,
                archives,
                guestDirectors);

            P10KungtteokiBoss2D kungtteoki =
                CreateKungtteoki(
                    FindNode(nodes, P10StageId.MoonPalace13),
                    rabbitEvent,
                    assets);
            P10BranchBoss2D knotSpider =
                CreateBranchBoss(
                    FindNode(nodes, P10StageId.MagpieBridge23),
                    P10BossKind.KnotSpider,
                    P9BranchKind.MagpieBridge,
                    support,
                    assets);
            P10BranchBoss2D gatekeeper =
                CreateBranchBoss(
                    FindNode(nodes, P10StageId.DragonPalace23),
                    P10BossKind.DragonGatekeeper,
                    P9BranchKind.DragonPalace,
                    support,
                    assets);

            for (int index = 0; index < nodes.Length; index++)
            {
                ConfigureStageExit(nodes[index], flow, assets);
            }

            List<P10RoutePortal2D> portals =
                new List<P10RoutePortal2D>(6);
            portals.Add(
                CreatePortal(
                    FindNode(nodes, P10StageId.MoonPalace13),
                    P10RoutePortalKind.FirstBranchChoice,
                    P9BranchKind.MagpieBridge,
                    flow,
                    new Vector2(-2.5f, 3.2f),
                    assets));
            portals.Add(
                CreatePortal(
                    FindNode(nodes, P10StageId.MoonPalace13),
                    P10RoutePortalKind.FirstBranchChoice,
                    P9BranchKind.DragonPalace,
                    flow,
                    new Vector2(2.5f, 3.2f),
                    assets));
            AddBranchCompletionPortals(
                FindNode(nodes, P10StageId.MagpieBridge23),
                P9BranchKind.MagpieBridge,
                flow,
                assets,
                portals);
            AddBranchCompletionPortals(
                FindNode(nodes, P10StageId.DragonPalace23),
                P9BranchKind.DragonPalace,
                flow,
                assets,
                portals);

            GameObject followup =
                CreateChild(
                    persistent.transform,
                    "Followup_P6_CorridorDesignReview_REQUIRED");
            followup.SetActive(false);
            GameObject humanGate =
                CreateChild(
                    persistent.transform,
                    "Followup_P10_HumanTimingAndBranchFeelPlaytest_REQUIRED");
            humanGate.SetActive(false);
            GameObject cultural =
                CreateChild(
                    persistent.transform,
                    "Followup_RecordGuest_CulturalReview_REQUIRED");
            cultural.SetActive(false);

            P10FirstBranchCampaignContract contract =
                systems.AddComponent<P10FirstBranchCampaignContract>();
            contract.Configure(
                catalog,
                director,
                flow,
                nodes,
                kungtteoki,
                new[] { knotSpider, gatekeeper },
                folklore,
                correspondence.ToArray(),
                branchEvents,
                archives.ToArray(),
                guestDirectors.ToArray(),
                portals.ToArray(),
                telemetry);

            camera.orthographic = true;
            camera.orthographicSize = 11f;
            camera.transform.rotation = Quaternion.identity;
            WireRoomFocusCamera(camera, motor);
            if (!flow.TryActivateStage(P10StageId.MoonPalace11))
            {
                throw new InvalidOperationException(
                    "P10 failed to activate Moon Palace 1-1.");
            }

            return contract;
        }

        private static P10StageEnvironment2D
            ConfigureInheritedMoonEnvironment(
                GameObject environmentRoot,
                GridWorld world,
                BuildAssets assets)
        {
            Transform entry =
                FindTransform(environmentRoot.transform, "StageEntry");
            Transform inheritedExit =
                FindTransform(environmentRoot.transform, "StageExit");
            if (entry == null || inheritedExit == null)
            {
                throw new InvalidOperationException(
                    "The inherited P5 entry or exit is missing.");
            }

            IReadOnlyList<MoonRoom> inheritedRooms =
                ApplyInheritedMoonRoomStructure(
                    environmentRoot,
                    world,
                    assets);
            Transform cameraAnchor =
                CreateChild(
                    environmentRoot.transform,
                    "P10CameraAnchor").transform;
            cameraAnchor.position =
                inheritedRooms.Count > 0
                    ? (Vector3)inheritedRooms[0].WorldRect.center
                    : (Vector3)(world.WorldBounds.center
                        + Vector2.up * 1.5f);
            Transform maruAnchor =
                CreateChild(
                    environmentRoot.transform,
                    "MaruAnchor").transform;
            maruAnchor.position =
                inheritedExit.position + Vector3.left;
            Transform[] motifs =
            {
                RequireTransform(
                    environmentRoot.transform,
                    "GlobalMoonBackdrop"),
                RequireTransform(
                    environmentRoot.transform,
                    "MoonPalacePresentation"),
                RequireTransform(
                    environmentRoot.transform,
                    "World")
            };
            P10StageEnvironment2D environment =
                environmentRoot.AddComponent<P10StageEnvironment2D>();
            environment.Configure(
                P10StageId.MoonPalace11,
                environmentRoot,
                world,
                entry,
                inheritedExit,
                maruAnchor,
                cameraAnchor,
                environmentRoot.GetComponentInChildren<
                    TileMutationService>(true),
                environmentRoot.GetComponentInChildren<
                    ExplosionService2D>(true),
                environmentRoot.GetComponentInChildren<
                    RopeInstaller2D>(true),
                environmentRoot.GetComponentInChildren<
                    WaterInteractionRegistry2D>(true),
                environmentRoot.GetComponentInChildren<
                    PestleInteractionRegistry2D>(true),
                FindTransform(
                    environmentRoot.transform,
                    "SpawnedConsumables"),
                FindTransform(
                    environmentRoot.transform,
                    "InstalledRopes"),
                motifs);
            return environment;
        }

        private static void PopulateCampaignEconomy(
            P10StageNode2D[] nodes,
            GameObject inheritedMoonEnvironment,
            Transform player,
            BuildAssets assets)
        {
            P5RunState2D runState =
                UnityEngine.Object.FindFirstObjectByType<P5RunState2D>(
                    FindObjectsInactive.Include);
            P5MomoShop2D shopTemplate =
                inheritedMoonEnvironment.GetComponentInChildren<
                    P5MomoShop2D>(true);
            if (runState == null || shopTemplate == null)
            {
                throw new InvalidOperationException(
                    "The inherited P5 economy is incomplete.");
            }

            P10StageNode2D moon12 =
                FindNode(nodes, P10StageId.MoonPalace12);
            GameObject moon12Economy =
                CreateChild(
                    moon12.Environment.EnvironmentRoot.transform,
                    "P10Economy");
            GameObject moon12Shop =
                UnityEngine.Object.Instantiate(
                    shopTemplate.gameObject,
                    moon12Economy.transform,
                    false);
            moon12Shop.name = "MomoShop_MoonPalace12";
            moon12Shop.transform.localPosition =
                MoonShopLocalPosition(moon12.StageId);

            for (int nodeIndex = 0;
                 nodeIndex < nodes.Length;
                 nodeIndex++)
            {
                P10StageNode2D node = nodes[nodeIndex];
                if (node == null
                    || node.StageId == P10StageId.MoonPalace11)
                {
                    continue;
                }

                GameObject economy =
                    node == moon12
                        ? moon12Economy
                        : CreateChild(
                            node.Environment.EnvironmentRoot.transform,
                            "P10Economy");
                Vector2Int size = StageSize(node.Definition);
                bool moonRooms = TryCreateMoonRoomPlan(
                    node.StageId,
                    out MoonStagePlan moonPlan);
                for (int goldIndex = 0; goldIndex < 5; goldIndex++)
                {
                    int value = goldIndex == 4
                        ? P5GoldPickup2D.BigGoldValue
                        : P5GoldPickup2D.SmallGoldValue;
                    Vector2 position = moonRooms
                        ? MoonRoomGoldPoint(moonPlan, goldIndex)
                        : node.Definition.Region
                            == RoomRegion.DragonPalace
                            ? new Vector2(
                                5f + (goldIndex % 2) * 18f,
                                4f + goldIndex * 4.2f)
                            : new Vector2(
                                6f + goldIndex
                                    * ((size.x - 12f) / 4f),
                                3.1f + (goldIndex % 2) * 2.2f);
                    CreateCampaignGold(
                        economy.transform,
                        $"RouteGold_{goldIndex:00}",
                        position,
                        value,
                        runState,
                        player,
                        assets);
                }
            }
        }

        private static void CreateCampaignGold(
            Transform parent,
            string name,
            Vector2 position,
            int value,
            P5RunState2D runState,
            Transform player,
            BuildAssets assets)
        {
            GameObject gold =
                CreateSpritePart(
                    parent,
                    name,
                    assets.Star,
                    position,
                    value == P5GoldPickup2D.BigGoldValue
                        ? Vector2.one * 0.52f
                        : Vector2.one * 0.34f,
                    value == P5GoldPickup2D.BigGoldValue
                        ? new Color(1f, 0.70f, 0.18f, 1f)
                        : new Color(1f, 0.90f, 0.38f, 1f),
                    20);
            CircleCollider2D trigger =
                gold.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            P5GoldPickup2D pickup =
                gold.AddComponent<P5GoldPickup2D>();
            pickup.Configure(
                runState,
                value,
                player,
                gold.GetComponent<SpriteRenderer>(),
                0.8f);
        }

        private static P10StageEnvironment2D CreateStageEnvironment(
            Transform parent,
            P10StageDefinition definition,
            BuildAssets assets,
            Collider2D playerCollider)
        {
            Vector2Int size = StageSize(definition);
            GameObject root =
                CreateChild(
                    parent,
                    $"Environment_{definition.StageId}_{definition.DisplayName}");
            root.SetActive(true);
            Transform[] motifs =
                CreateThemePresentation(root.transform, definition, size, assets);

            GameObject gridObject =
                CreateChild(root.transform, "Grid");
            UnityEngine.Grid grid =
                gridObject.AddComponent<UnityEngine.Grid>();
            grid.cellSize = Vector3.one;
            Tilemap terrain = CreateTilemapLayer(
                gridObject.transform,
                "Terrain",
                0,
                true,
                false,
                out TilemapCollider2D terrainCollider,
                out CompositeCollider2D composite);
            Tilemap oneWay = CreateTilemapLayer(
                gridObject.transform,
                "OneWay",
                1,
                false,
                true,
                out TilemapCollider2D oneWayCollider,
                out _);
            Tilemap decoration = CreateTilemapLayer(
                gridObject.transform,
                "Decoration",
                2,
                false,
                false,
                out _,
                out _);
            Tilemap hazard = CreateTilemapLayer(
                gridObject.transform,
                "Hazard",
                3,
                false,
                false,
                out _,
                out _);
            TintTilemaps(
                definition.Region,
                terrain.GetComponent<TilemapRenderer>(),
                oneWay.GetComponent<TilemapRenderer>());
            bool moonRooms = TryCreateMoonRoomPlan(
                definition.StageId,
                out MoonStagePlan moonPlan);
            if (moonRooms)
            {
                BakeMoonRoomTerrain(
                    moonPlan,
                    terrain,
                    oneWay,
                    assets);
            }
            else
            {
                FillStageGeometry(
                    definition,
                    size,
                    terrain,
                    oneWay,
                    assets);
            }

            terrain.CompressBounds();
            oneWay.CompressBounds();
            terrain.RefreshAllTiles();
            oneWay.RefreshAllTiles();
            terrainCollider.ProcessTilemapChanges();
            composite.GenerateGeometry();
            oneWayCollider.ProcessTilemapChanges();

            GridWorld world = gridObject.AddComponent<GridWorld>();
            world.Configure(
                grid,
                terrain,
                hazard,
                Vector2Int.zero,
                size);
            Vector2Int entryCell = moonRooms
                ? moonPlan.EntryCell
                : new Vector2Int(2, 2);
            Vector2Int exitCell = moonRooms
                ? moonPlan.ExitCell
                : definition.Region == RoomRegion.DragonPalace
                    ? new Vector2Int(size.x - 3, size.y - 3)
                    : new Vector2Int(size.x - 3, 2);
            GameObject entryObject =
                CreateChild(root.transform, "StageEntry");
            entryObject.transform.position =
                new Vector3(
                    entryCell.x + 0.5f,
                    entryCell.y,
                    0f);
            GameObject exitObject =
                CreateChild(root.transform, "StageExit");
            exitObject.transform.position =
                new Vector3(
                    exitCell.x + 0.5f,
                    exitCell.y,
                    0f);
            GameObject maruAnchor =
                CreateChild(root.transform, "MaruAnchor");
            maruAnchor.transform.position =
                exitObject.transform.position + Vector3.left;
            CreateSpritePart(
                exitObject.transform,
                "ExitStar",
                assets.Star,
                Vector2.zero,
                Vector2.one * 0.75f,
                new Color(0.62f, 1f, 0.95f, 1f),
                25);
            GameObject cameraAnchor =
                CreateChild(root.transform, "CameraAnchor");
            cameraAnchor.transform.position = moonRooms
                ? (Vector3)moonPlan.Rooms[0].WorldRect.center
                : new Vector3(
                    size.x * 0.5f,
                    size.y * 0.5f,
                    0f);

            GameObject systems =
                CreateChild(root.transform, "StageSystems");
            GameObject spawned =
                CreateChild(systems.transform, "SpawnedConsumables");
            GameObject ropesRoot =
                CreateChild(systems.transform, "InstalledRopes");
            TileMutationService mutation =
                systems.AddComponent<TileMutationService>();
            mutation.Configure(
                world,
                terrain,
                decoration,
                terrainCollider,
                composite,
                playerCollider,
                assets.TileDefinitions,
                new GridPos(entryCell.x, entryCell.y),
                new GridPos(exitCell.x, exitCell.y),
                ProtectedExitCells(exitCell));
            ExplosionService2D explosions =
                systems.AddComponent<ExplosionService2D>();
            explosions.Configure(
                world,
                mutation,
                ExplosionConstants.DefaultChainHardCap,
                7f,
                ~0);
            RopeInstaller2D ropes =
                systems.AddComponent<RopeInstaller2D>();
            ropes.Configure(
                world,
                mutation,
                null,
                ropesRoot.transform,
                RopePlacementSolver.DefaultMaximumLength,
                assets.Square);
            WaterInteractionRegistry2D water =
                systems.AddComponent<WaterInteractionRegistry2D>();
            PestleInteractionRegistry2D pestle =
                systems.AddComponent<PestleInteractionRegistry2D>();

            P10StageEnvironment2D environment =
                root.AddComponent<P10StageEnvironment2D>();
            environment.Configure(
                definition.StageId,
                root,
                world,
                entryObject.transform,
                exitObject.transform,
                maruAnchor.transform,
                cameraAnchor.transform,
                mutation,
                explosions,
                ropes,
                water,
                pestle,
                spawned.transform,
                ropesRoot.transform,
                motifs);
            CreateRegionTraversalMechanics(
                root.transform,
                definition,
                size,
                assets);
            if (moonRooms)
            {
                CreateMoonRoomBounds(
                    root.transform,
                    moonPlan.Rooms);
            }

            Physics2D.SyncTransforms();
            return environment;
        }

        private static Transform[] CreateThemePresentation(
            Transform parent,
            P10StageDefinition definition,
            Vector2Int size,
            BuildAssets assets)
        {
            Color backgroundColor;
            Sprite backgroundArt;
            Sprite motifA;
            Sprite motifB;
            Sprite motifC;
            switch (definition.Region)
            {
                case RoomRegion.MagpieBridge:
                    backgroundColor =
                        new Color(0.16f, 0.20f, 0.42f, 1f);
                    backgroundArt = assets.Cloud;
                    motifA = assets.Bridge;
                    motifB = assets.Bird;
                    motifC = assets.Star;
                    break;
                case RoomRegion.DragonPalace:
                    backgroundColor =
                        new Color(0.02f, 0.18f, 0.30f, 1f);
                    backgroundArt = assets.UnderwaterBackground;
                    motifA = assets.Fish;
                    motifB = assets.Seaweed;
                    motifC = assets.Bubble;
                    break;
                default:
                    backgroundColor =
                        new Color(0.12f, 0.10f, 0.31f, 1f);
                    backgroundArt = assets.Moon;
                    motifA = assets.Tree;
                    motifB = assets.Star;
                    motifC = assets.Moon;
                    break;
            }

            Transform backdrop = CreateSpritePart(
                parent,
                "ThemeBackdrop",
                assets.Square,
                new Vector2(size.x * 0.5f, size.y * 0.5f),
                new Vector2(size.x, size.y),
                backgroundColor,
                -30).transform;
            Transform art = CreateSpritePart(
                parent,
                "ThemeBackgroundArt",
                backgroundArt,
                new Vector2(size.x * 0.5f, size.y * 0.55f),
                Vector2.one * 3.5f,
                new Color(0.75f, 0.86f, 1f, 0.40f),
                -29).transform;
            Transform first = CreateSpritePart(
                parent,
                "ThemeMotif_A",
                motifA,
                new Vector2(size.x * 0.24f, size.y * 0.62f),
                Vector2.one * 1.8f,
                Color.white,
                10).transform;
            Transform second = CreateSpritePart(
                parent,
                "ThemeMotif_B",
                motifB,
                new Vector2(size.x * 0.54f, size.y * 0.72f),
                Vector2.one * 1.35f,
                new Color(0.72f, 0.96f, 1f, 0.9f),
                11).transform;
            Transform third = CreateSpritePart(
                parent,
                "ThemeMotif_C",
                motifC,
                new Vector2(size.x * 0.78f, size.y * 0.52f),
                Vector2.one,
                new Color(1f, 0.78f, 0.96f, 0.9f),
                12).transform;
            return new[] { backdrop, art, first, second, third };
        }

        private static void FillStageGeometry(
            P10StageDefinition definition,
            Vector2Int size,
            Tilemap terrain,
            Tilemap oneWay,
            BuildAssets assets)
        {
            SetRect(
                terrain,
                assets.ReinforcedTile,
                0,
                0,
                size.x,
                2);
            SetRect(
                terrain,
                assets.ReinforcedTile,
                0,
                0,
                1,
                size.y);
            SetRect(
                terrain,
                assets.ReinforcedTile,
                size.x - 1,
                0,
                1,
                size.y);

            if (definition.Region == RoomRegion.MagpieBridge)
            {
                for (int x = 5; x < size.x - 4; x += 8)
                {
                    SetRect(
                        oneWay,
                        assets.OneWayTile,
                        x,
                        6 + (x / 8 % 2) * 4,
                        5,
                        1);
                }

                SetRect(
                    oneWay,
                    assets.OneWayTile,
                    2,
                    4,
                    7,
                    1);
                SetRect(
                    oneWay,
                    assets.OneWayTile,
                    size.x - 10,
                    4,
                    7,
                    1);
                return;
            }

            if (definition.Region == RoomRegion.DragonPalace)
            {
                int platformY = 4;
                bool left = true;
                while (platformY < size.y - 3)
                {
                    int x = left ? 2 : 10;
                    int width = left
                        ? Mathf.Min(17, size.x - 5)
                        : Mathf.Min(17, size.x - 12);
                    SetRect(
                        oneWay,
                        assets.OneWayTile,
                        x,
                        platformY,
                        width,
                        1);
                    platformY += 3;
                    left = !left;
                }

                SetRect(
                    oneWay,
                    assets.OneWayTile,
                    size.x - 7,
                    size.y - 4,
                    5,
                    1);
                return;
            }

            SetRect(
                oneWay,
                assets.OneWayTile,
                7,
                4,
                7,
                1);
            SetRect(
                oneWay,
                assets.OneWayTile,
                18,
                7,
                8,
                1);
            SetRect(
                oneWay,
                assets.OneWayTile,
                size.x - 13,
                4,
                8,
                1);
            if (definition.StageSlot == P6StageSlot.X2)
            {
                SetRect(
                    oneWay,
                    assets.OneWayTile,
                    size.x / 2 - 4,
                    11,
                    8,
                    1);
            }

            if (definition.StageId == P10StageId.MoonPalace13)
            {
                SetRect(
                    terrain,
                    assets.SoftSoilTile,
                    size.x / 2 - 5,
                    2,
                    10,
                    1);
            }
        }

        private static void CreateRegionTraversalMechanics(
            Transform parent,
            P10StageDefinition definition,
            Vector2Int size,
            BuildAssets assets)
        {
            if (definition.Region == RoomRegion.MagpieBridge)
            {
                CreateTraversalForceZone(
                    parent,
                    "Crosswind_West",
                    new Vector2(size.x * 0.30f, 8f),
                    new Vector2(13f, 6f),
                    Vector2.right,
                    5.5f,
                    true,
                    2.6f,
                    new Color(0.66f, 0.82f, 1f, 0.09f),
                    assets);
                CreateTraversalForceZone(
                    parent,
                    "Crosswind_East",
                    new Vector2(size.x * 0.70f, 11f),
                    new Vector2(14f, 7f),
                    Vector2.left,
                    5f,
                    true,
                    3.1f,
                    new Color(0.82f, 0.66f, 1f, 0.09f),
                    assets);
                CreateSwayingPlatform(
                    parent,
                    "SwayingMagpieBridge",
                    new Vector2(size.x * 0.50f, 12f),
                    new Vector2(6f, 0.35f),
                    Vector2.right,
                    2.5f,
                    4.8f,
                    assets);
                CreateTimedPlatform(
                    parent,
                    "TemporaryMagpiePlatform_A",
                    new Vector2(size.x * 0.39f, 6.5f),
                    0f,
                    assets);
                CreateTimedPlatform(
                    parent,
                    "TemporaryMagpiePlatform_B",
                    new Vector2(size.x * 0.61f, 8.5f),
                    1.8f,
                    assets);
                return;
            }

            if (definition.Region != RoomRegion.DragonPalace)
            {
                return;
            }

            CreateTraversalForceZone(
                parent,
                "CellCurrent_Lower",
                new Vector2(size.x * 0.30f, size.y * 0.32f),
                new Vector2(8f, 11f),
                Vector2.up,
                4.8f,
                false,
                2f,
                new Color(0.18f, 0.92f, 1f, 0.10f),
                assets);
            CreateTraversalForceZone(
                parent,
                "CellCurrent_Upper",
                new Vector2(size.x * 0.68f, size.y * 0.68f),
                new Vector2(8f, 11f),
                new Vector2(0.35f, 1f),
                5.2f,
                false,
                2f,
                new Color(0.22f, 0.70f, 1f, 0.10f),
                assets);
            CreateFloodgate(
                parent,
                "Floodgate_Lower",
                new Vector2(size.x * 0.48f, 9f),
                assets);
            CreateFloodgate(
                parent,
                "Floodgate_Upper",
                new Vector2(size.x * 0.62f, 18f),
                assets);
        }

        private static void CreateTraversalForceZone(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 scale,
            Vector2 direction,
            float acceleration,
            bool oscillates,
            float halfCycleSeconds,
            Color color,
            BuildAssets assets)
        {
            GameObject zone =
                CreateSpritePart(
                    parent,
                    name,
                    assets.Square,
                    position,
                    scale,
                    color,
                    4);
            BoxCollider2D trigger =
                zone.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            GameObject arrow =
                CreateSpritePart(
                    zone.transform,
                    "FlowIndicator",
                    assets.Star,
                    Vector2.zero,
                    new Vector2(0.08f, 0.16f),
                    new Color(0.72f, 1f, 1f, 0.85f),
                    5);
            P10TraversalForceZone2D force =
                zone.AddComponent<P10TraversalForceZone2D>();
            force.Configure(
                direction,
                acceleration,
                oscillates,
                halfCycleSeconds,
                arrow.GetComponent<SpriteRenderer>());
        }

        private static void CreateSwayingPlatform(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 scale,
            Vector2 axis,
            float distance,
            float cycleSeconds,
            BuildAssets assets)
        {
            GameObject platform =
                CreateSpritePart(
                    parent,
                    name,
                    assets.Bridge,
                    position,
                    scale,
                    new Color(0.92f, 0.75f, 1f, 1f),
                    18);
            platform.AddComponent<BoxCollider2D>();
            Rigidbody2D body =
                platform.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            P10SwayingPlatform2D swaying =
                platform.AddComponent<P10SwayingPlatform2D>();
            swaying.Configure(axis, distance, cycleSeconds);
        }

        private static void CreateTimedPlatform(
            Transform parent,
            string name,
            Vector2 position,
            float phaseSeconds,
            BuildAssets assets)
        {
            GameObject platform =
                CreateSpritePart(
                    parent,
                    name,
                    assets.Bridge,
                    position,
                    new Vector2(4.2f, 0.30f),
                    new Color(0.82f, 0.66f, 1f, 0.95f),
                    18);
            platform.AddComponent<BoxCollider2D>();
            P10TimedPlatform2D timed =
                platform.AddComponent<P10TimedPlatform2D>();
            timed.Configure(2.6f, 1.25f, phaseSeconds);
        }

        private static void CreateFloodgate(
            Transform parent,
            string name,
            Vector2 position,
            BuildAssets assets)
        {
            GameObject gate =
                CreateSpritePart(
                    parent,
                    name,
                    assets.Square,
                    position,
                    new Vector2(0.55f, 3.4f),
                    new Color(0.22f, 0.90f, 0.96f, 0.88f),
                    19);
            BoxCollider2D barrier =
                gate.AddComponent<BoxCollider2D>();
            P10Floodgate2D floodgate =
                gate.AddComponent<P10Floodgate2D>();
            floodgate.Configure(
                barrier,
                gate.GetComponent<SpriteRenderer>());
        }

        private static void ConfigureStageExit(
            P10StageNode2D node,
            P10StageFlowController2D flow,
            BuildAssets assets)
        {
            Transform anchor = node.Environment.ExitAnchor;
            if (anchor == null)
            {
                throw new InvalidOperationException(
                    $"P10 stage {node.StageId} has no exit anchor.");
            }

            P5StageExitInteractionGuard2D inheritedGuard =
                anchor.GetComponent<P5StageExitInteractionGuard2D>();
            if (inheritedGuard != null)
            {
                UnityEngine.Object.DestroyImmediate(inheritedGuard);
            }

            GameObject ready =
                CreateSpritePart(
                    anchor,
                    "P10ExitReady",
                    assets.Star,
                    Vector2.zero,
                    Vector2.one * 0.82f,
                    new Color(0.40f, 1f, 0.82f, 1f),
                    31);
            GameObject locked =
                CreateSpritePart(
                    anchor,
                    "P10ExitLocked",
                    assets.Square,
                    Vector2.zero,
                    Vector2.one * 0.72f,
                    new Color(0.85f, 0.24f, 0.48f, 0.9f),
                    30);
            P10StageExit2D exit =
                anchor.gameObject.AddComponent<P10StageExit2D>();
            if (exit == null)
            {
                throw new InvalidOperationException(
                    $"P10 stage {node.StageId} could not create its exit.");
            }

            bool linear =
                node.StageId == P10StageId.MoonPalace11
                || node.StageId == P10StageId.MoonPalace12
                || node.StageId == P10StageId.MagpieBridge21
                || node.StageId == P10StageId.MagpieBridge22
                || node.StageId == P10StageId.DragonPalace21
                || node.StageId == P10StageId.DragonPalace22;
            exit.Configure(node, flow, ready, locked, linear);
        }

        private static P9CorrespondenceEvent2D
            CreateCorrespondenceEvent(
                P10StageNode2D node,
                P9CorrespondenceEventKind kind,
                P9FolkloreChainState2D chain,
                BuildAssets assets,
                Color color)
        {
            Transform parent = node.Environment.EnvironmentRoot.transform;
            GameObject root =
                CreateChild(parent, $"Correspondence_{kind}");
            root.transform.position = new Vector3(9f, 3f, 0f);
            Transform gift = CreateSpritePart(
                root.transform,
                "VisibleGift",
                kind == P9CorrespondenceEventKind.HungryMagpie
                    ? assets.Star
                    : assets.Bubble,
                new Vector2(-0.8f, 0.7f),
                Vector2.one * 0.55f,
                color,
                24).transform;
            Transform silhouette = CreateSpritePart(
                root.transform,
                "MatchingSilhouette",
                assets.Square,
                new Vector2(0.8f, 0.7f),
                new Vector2(0.35f, 0.75f),
                new Color(color.r, color.g, color.b, 0.45f),
                23).transform;
            Transform attention = CreateSpritePart(
                root.transform,
                "NpcAttentionGesture",
                assets.Star,
                new Vector2(0f, 1.45f),
                Vector2.one * 0.35f,
                Color.white,
                25).transform;
            GameObject assistance =
                CreateSpritePart(
                    root.transform,
                    "Assistance",
                    assets.Square,
                    new Vector2(1.5f, -0.5f),
                    new Vector2(3f, 0.18f),
                    color,
                    20);
            assistance.SetActive(false);
            GameObject mainPath =
                CreateSpritePart(
                    root.transform,
                    "AlwaysOpenMainPath",
                    assets.Square,
                    new Vector2(0f, -1f),
                    new Vector2(5f, 0.12f),
                    new Color(0.58f, 1f, 0.92f, 0.8f),
                    19);
            P9CorrespondenceEvent2D stageEvent =
                root.AddComponent<P9CorrespondenceEvent2D>();
            stageEvent.Configure(
                kind,
                chain,
                gift,
                silhouette,
                attention,
                assistance,
                mainPath,
                true);
            return stageEvent;
        }

        private static P10BranchStageEvent2D CreateBranchStageEvent(
            P10StageNode2D node,
            P10BranchEventKind kind,
            P10BranchSupportState2D support,
            BuildAssets assets)
        {
            Transform parent = node.Environment.EnvironmentRoot.transform;
            GameObject root =
                CreateChild(parent, $"BranchEvent_{kind}");
            root.transform.position = new Vector3(14f, 3f, 0f);
            GameObject unresolved =
                CreateSpritePart(
                    root.transform,
                    "Unresolved",
                    kind == P10BranchEventKind.RepairMagpieNest
                        ? assets.Bridge
                        : assets.Fish,
                    Vector2.zero,
                    Vector2.one,
                    new Color(1f, 0.58f, 0.54f, 1f),
                    24);
            GameObject resolved =
                CreateSpritePart(
                    root.transform,
                    "Resolved",
                    assets.Star,
                    Vector2.zero,
                    Vector2.one * 0.75f,
                    new Color(0.45f, 1f, 0.75f, 1f),
                    25);
            resolved.SetActive(false);
            P10BranchStageEvent2D stageEvent =
                root.AddComponent<P10BranchStageEvent2D>();
            stageEvent.Configure(
                kind,
                support,
                unresolved,
                resolved);
            return stageEvent;
        }

        private static void CreateArchive(
            P10StageNode2D node,
            P9RecordGuestCatalog catalog,
            Transform player,
            BuildAssets assets,
            List<P9StarArchive2D> archives,
            List<P9RecordGuestDirector2D> directors)
        {
            Transform parent = node.Environment.EnvironmentRoot.transform;
            GameObject root =
                CreateChild(parent, "GuaranteedStarArchive_X2");
            root.transform.position = MoonRoomAnchorOrDefault(
                node.StageId,
                MoonRoomSlot.Archive,
                new Vector3(20f, 3.2f, 0f));
            Transform cue = CreateSpritePart(
                root.transform,
                "MainRouteVisibleCue",
                assets.Star,
                new Vector2(0f, 1.5f),
                Vector2.one * 0.55f,
                new Color(0.55f, 1f, 0.95f, 1f),
                27).transform;
            GameObject sealedVisual =
                CreateSpritePart(
                    root.transform,
                    "SealedArchive",
                    assets.Square,
                    Vector2.zero,
                    new Vector2(1.8f, 2.1f),
                    new Color(0.05f, 0.31f, 0.40f, 1f),
                    23);
            GameObject openVisual =
                CreateSpritePart(
                    root.transform,
                    "OpenedArchive",
                    assets.Star,
                    Vector2.zero,
                    Vector2.one,
                    new Color(0.80f, 1f, 0.94f, 1f),
                    24);
            openVisual.SetActive(false);
            P9StarArchive2D archive =
                root.AddComponent<P9StarArchive2D>();
            archive.Configure(
                P9ArchiveUnlockMethods.SealLever
                | P9ArchiveUnlockMethods.CrackedOuterWall
                | P9ArchiveUnlockMethods.HookLatch,
                cue,
                sealedVisual,
                openVisual);

            GameObject followerRoot =
                CreateChild(root.transform, "RecordGuestFollower");
            GameObject followerVisual =
                CreateSpritePart(
                    followerRoot.transform,
                    "GuestVisual",
                    assets.Star,
                    Vector2.zero,
                    Vector2.one * 0.72f,
                    new Color(1f, 0.85f, 0.52f, 1f),
                    28);
            P9RecordGuestDefinition definition =
                catalog.FindForRegion(node.Definition.Region);
            P9RecordGuestFollower2D follower =
                followerRoot.AddComponent<P9RecordGuestFollower2D>();
            follower.Configure(
                definition,
                player,
                followerVisual.transform,
                root.transform.position,
                5f);
            P9RecordGuestDirector2D director =
                root.AddComponent<P9RecordGuestDirector2D>();
            director.Configure(
                catalog,
                node.Definition.Region,
                P6StageSlot.X2,
                0f,
                archive,
                follower);
            archives.Add(archive);
            directors.Add(director);
        }

        private static P10KungtteokiBoss2D CreateKungtteoki(
            P10StageNode2D node,
            P5MoonRabbitPestleEvent2D rabbitEvent,
            BuildAssets assets)
        {
            Transform parent = node.Environment.EnvironmentRoot.transform;
            GameObject root =
                CreateChild(parent, "Boss_Kungtteoki");
            root.transform.position = MoonRoomAnchorOrDefault(
                node.StageId,
                MoonRoomSlot.Boss,
                new Vector3(19f, 5f, 0f));
            BoxCollider2D receiverCollider =
                root.AddComponent<BoxCollider2D>();
            receiverCollider.isTrigger = true;
            receiverCollider.size = new Vector2(4.2f, 5.2f);
            CreateSpritePart(
                root.transform,
                "KungtteokiBody",
                assets.Moon,
                Vector2.zero,
                Vector2.one * 2.2f,
                new Color(0.95f, 0.67f, 0.92f, 1f),
                25);
            SpriteRenderer[] knots =
                CreateKnotVisuals(root.transform, assets);
            GameObject[] floors = new GameObject[3];
            for (int index = 0; index < floors.Length; index++)
            {
                floors[index] = CreateSpritePart(
                    root.transform,
                    $"CrackedFloor_{index}",
                    assets.Square,
                    new Vector2(-3f + index * 3f, -2f),
                    new Vector2(2.2f, 0.3f),
                    new Color(0.85f, 0.52f, 0.72f, 1f),
                    21);
            }

            GameObject firstMark =
                CreateSpritePart(
                    root.transform,
                    "RabbitHelp_FirstFloorFlourMark",
                    assets.Star,
                    new Vector2(-3f, -1.5f),
                    Vector2.one * 0.45f,
                    Color.white,
                    26);
            GameObject millWeight =
                CreateSpritePart(
                    root.transform,
                    "RabbitHelp_MillWeight",
                    assets.Square,
                    new Vector2(3f, 1.4f),
                    new Vector2(0.65f, 1.4f),
                    new Color(0.76f, 0.82f, 1f, 1f),
                    24);
            GameObject recoveryCake =
                CreateSpritePart(
                    root.transform,
                    "RabbitHelp_RecoveryMoonCake",
                    assets.Star,
                    new Vector2(0f, -1.2f),
                    Vector2.one * 0.65f,
                    new Color(1f, 0.72f, 0.34f, 1f),
                    27);
            P10KungtteokiBoss2D boss =
                root.AddComponent<P10KungtteokiBoss2D>();
            boss.Configure(
                node,
                rabbitEvent,
                knots,
                floors,
                firstMark,
                millWeight,
                recoveryCake);
            for (int index = 0; index < floors.Length; index++)
            {
                BoxCollider2D targetCollider =
                    floors[index].AddComponent<BoxCollider2D>();
                targetCollider.isTrigger = true;
                floors[index]
                    .AddComponent<P10BossEnvironmentTarget2D>()
                    .Configure(boss, index);
            }

            return boss;
        }

        private static P10BranchBoss2D CreateBranchBoss(
            P10StageNode2D node,
            P10BossKind kind,
            P9BranchKind branch,
            P10BranchSupportState2D support,
            BuildAssets assets)
        {
            Transform parent = node.Environment.EnvironmentRoot.transform;
            GameObject root =
                CreateChild(parent, $"Boss_{kind}");
            root.transform.position = new Vector3(
                node.Definition.Region == RoomRegion.DragonPalace
                    ? 15f
                    : 21f,
                node.Definition.Region == RoomRegion.DragonPalace
                    ? 12f
                    : 5f,
                0f);
            BoxCollider2D receiverCollider =
                root.AddComponent<BoxCollider2D>();
            receiverCollider.isTrigger = true;
            receiverCollider.size = new Vector2(4.2f, 5.2f);
            CreateSpritePart(
                root.transform,
                "BossBody",
                kind == P10BossKind.KnotSpider
                    ? assets.Bird
                    : assets.Fish,
                Vector2.zero,
                Vector2.one * 2f,
                kind == P10BossKind.KnotSpider
                    ? new Color(0.86f, 0.48f, 0.92f, 1f)
                    : new Color(0.28f, 0.90f, 0.92f, 1f),
                25);
            SpriteRenderer[] knots =
                CreateKnotVisuals(root.transform, assets);
            GameObject[] targets = new GameObject[3];
            for (int index = 0; index < targets.Length; index++)
            {
                targets[index] = CreateSpritePart(
                    root.transform,
                    kind == P10BossKind.KnotSpider
                        ? $"SupportThread_{index}"
                        : $"Floodgate_{index}",
                    assets.Square,
                    new Vector2(-3f + index * 3f, -1.8f),
                    new Vector2(0.28f, 3f),
                    new Color(0.45f, 0.92f, 1f, 0.8f),
                    22);
            }

            P10BranchBoss2D boss =
                root.AddComponent<P10BranchBoss2D>();
            boss.Configure(
                kind,
                branch,
                node,
                support,
                knots,
                targets);
            for (int index = 0; index < targets.Length; index++)
            {
                BoxCollider2D targetCollider =
                    targets[index].AddComponent<BoxCollider2D>();
                targetCollider.isTrigger = true;
                targets[index]
                    .AddComponent<P10BossEnvironmentTarget2D>()
                    .Configure(boss, index);
            }

            return boss;
        }

        private static SpriteRenderer[] CreateKnotVisuals(
            Transform parent,
            BuildAssets assets)
        {
            SpriteRenderer[] renderers = new SpriteRenderer[3];
            for (int index = 0; index < renderers.Length; index++)
            {
                GameObject knot =
                    CreateSpritePart(
                        parent,
                        $"StarKnot_{index}",
                        assets.Star,
                        new Vector2(-1f + index, 2.1f),
                        Vector2.one * 0.48f,
                        new Color(1f, 0.82f, 0.38f, 1f),
                        28);
                renderers[index] =
                    knot.GetComponent<SpriteRenderer>();
            }

            return renderers;
        }

        private static P10RoutePortal2D CreatePortal(
            P10StageNode2D node,
            P10RoutePortalKind kind,
            P9BranchKind branch,
            P10StageFlowController2D flow,
            Vector2 localOffset,
            BuildAssets assets)
        {
            Transform parent = node.Environment.EnvironmentRoot.transform;
            GameObject root =
                CreateChild(parent, $"Portal_{kind}_{branch}");
            root.transform.position =
                node.Environment.ExitAnchor.position
                + (Vector3)localOffset;
            Color color =
                branch == P9BranchKind.MagpieBridge
                    ? new Color(0.88f, 0.60f, 1f, 1f)
                    : new Color(0.28f, 0.88f, 1f, 1f);
            if (kind == P10RoutePortalKind.CommonRegion)
            {
                color = new Color(0.58f, 1f, 0.70f, 1f);
            }

            GameObject available =
                CreateSpritePart(
                    root.transform,
                    "Available",
                    assets.Star,
                    Vector2.zero,
                    Vector2.one * 0.9f,
                    color,
                    30);
            GameObject unavailable =
                CreateSpritePart(
                    root.transform,
                    "Unavailable",
                    assets.Square,
                    Vector2.zero,
                    Vector2.one * 0.7f,
                    new Color(0.24f, 0.25f, 0.34f, 0.8f),
                    29);
            P10RoutePortal2D portal =
                root.AddComponent<P10RoutePortal2D>();
            portal.Configure(
                kind,
                branch,
                flow,
                available,
                unavailable);
            return portal;
        }

        private static void AddBranchCompletionPortals(
            P10StageNode2D node,
            P9BranchKind sourceBranch,
            P10StageFlowController2D flow,
            BuildAssets assets,
            List<P10RoutePortal2D> portals)
        {
            portals.Add(
                CreatePortal(
                    node,
                    P10RoutePortalKind.CommonRegion,
                    sourceBranch,
                    flow,
                    new Vector2(-2f, 3f),
                    assets));
            portals.Add(
                CreatePortal(
                    node,
                    P10RoutePortalKind.CrossRoute,
                    sourceBranch,
                    flow,
                    new Vector2(2f, 3f),
                    assets));
        }

        private static void WireRoomFocusCamera(
            Camera camera,
            PlayerMotor2D motor)
        {
            if (camera == null || motor == null)
            {
                throw new InvalidOperationException(
                    "P10 room focus camera requires the persistent "
                    + "camera and player.");
            }

            GridBoundedCamera2D fallback =
                camera.GetComponent<GridBoundedCamera2D>();
            if (fallback == null)
            {
                fallback =
                    camera.gameObject.AddComponent<GridBoundedCamera2D>();
            }

            RoomFocusCamera2D focus =
                camera.GetComponent<RoomFocusCamera2D>();
            if (focus == null)
            {
                focus =
                    camera.gameObject.AddComponent<RoomFocusCamera2D>();
            }

            focus.Configure(
                camera,
                motor.transform,
                fallback,
                motor.GetComponent<PlayerRecovery>());
        }

        private static IReadOnlyList<MoonRoom>
            ApplyInheritedMoonRoomStructure(
                GameObject environmentRoot,
                GridWorld world,
                BuildAssets assets)
        {
            Tilemap terrain = world != null
                ? world.TerrainTilemap
                : null;
            Transform oneWayTransform =
                FindTransform(environmentRoot.transform, "OneWay");
            Tilemap oneWay = oneWayTransform != null
                ? oneWayTransform.GetComponent<Tilemap>()
                : null;
            if (terrain == null)
            {
                throw new InvalidOperationException(
                    "The inherited P5 terrain tilemap is missing.");
            }

            Vector2Int[] origins =
                P5MoonStageContract.RequiredRoomOrigins;
            Vector2Int[] sizes =
                P5MoonStageContract.RequiredRoomSizes;
            string[] ids = P5MoonStageContract.RequiredRoomIds;
            List<MoonRoom> rooms =
                new List<MoonRoom>(origins.Length);
            for (int index = 0; index < origins.Length; index++)
            {
                rooms.Add(
                    new MoonRoom(
                        ids[index],
                        new RectInt(
                            origins[index].x,
                            origins[index].y,
                            sizes[index].x,
                            Mathf.Max(
                                sizes[index].y,
                                InheritedMinimumRoomHeight)),
                        true));
            }

            for (int index = 1; index < rooms.Count; index++)
            {
                BakeInheritedPartitionWall(
                    terrain,
                    oneWay,
                    rooms[index - 1],
                    rooms[index],
                    assets);
            }

            terrain.CompressBounds();
            terrain.RefreshAllTiles();
            TilemapCollider2D terrainCollider =
                terrain.GetComponent<TilemapCollider2D>();
            if (terrainCollider != null)
            {
                terrainCollider.ProcessTilemapChanges();
            }

            CompositeCollider2D composite =
                terrain.GetComponent<CompositeCollider2D>();
            if (composite != null)
            {
                composite.GenerateGeometry();
            }

            if (oneWay != null)
            {
                oneWay.CompressBounds();
                oneWay.RefreshAllTiles();
                TilemapCollider2D oneWayCollider =
                    oneWay.GetComponent<TilemapCollider2D>();
                if (oneWayCollider != null)
                {
                    oneWayCollider.ProcessTilemapChanges();
                }
            }

            CreateMoonRoomBounds(environmentRoot.transform, rooms);
            return rooms;
        }

        private static void BakeInheritedPartitionWall(
            Tilemap terrain,
            Tilemap oneWay,
            MoonRoom left,
            MoonRoom right,
            BuildAssets assets)
        {
            int wallX = right.Cells.xMin;
            int doorBottom = 1;
            for (int y = 1; y <= MaxInheritedDoorSurface + 1; y++)
            {
                bool standable =
                    HasTile(terrain, wallX, y - 1)
                    || HasTile(oneWay, wallX, y - 1);
                bool clear = !HasTile(terrain, wallX, y)
                    && !HasTile(terrain, wallX, y + 1);
                if (standable && clear)
                {
                    doorBottom = y;
                    break;
                }
            }

            for (int offset = 0;
                 offset < InheritedDoorHeight;
                 offset++)
            {
                ClearCell(terrain, wallX, doorBottom + offset);
                ClearCell(oneWay, wallX, doorBottom + offset);
            }

            int wallBottom = doorBottom + InheritedDoorHeight;
            int wallTop = Mathf.Max(
                Mathf.Max(left.Cells.yMax, right.Cells.yMax) - 1,
                wallBottom + 3);
            for (int y = wallBottom; y <= wallTop; y++)
            {
                terrain.SetTile(
                    new Vector3Int(wallX, y, 0),
                    assets.ReinforcedTile);
                ClearCell(oneWay, wallX, y);
            }
        }

        private static bool HasTile(Tilemap tilemap, int x, int y)
        {
            return tilemap != null
                && tilemap.HasTile(new Vector3Int(x, y, 0));
        }

        private static void ClearCell(Tilemap tilemap, int x, int y)
        {
            tilemap?.SetTile(new Vector3Int(x, y, 0), null);
        }

        private static void BakeMoonRoomTerrain(
            MoonStagePlan plan,
            Tilemap terrain,
            Tilemap oneWay,
            BuildAssets assets)
        {
            for (int index = 0; index < plan.Rooms.Length; index++)
            {
                BakeMoonRoomShell(
                    terrain,
                    assets.ReinforcedTile,
                    plan.Rooms[index].Cells,
                    plan.Openings);
            }

            for (int index = 0; index < plan.SoftSoil.Length; index++)
            {
                RectInt patch = plan.SoftSoil[index];
                SetRect(
                    terrain,
                    assets.SoftSoilTile,
                    patch.xMin,
                    patch.yMin,
                    patch.width,
                    patch.height);
            }

            for (int index = 0; index < plan.Ledges.Length; index++)
            {
                RectInt ledge = plan.Ledges[index];
                SetRect(
                    oneWay,
                    assets.OneWayTile,
                    ledge.xMin,
                    ledge.yMin,
                    ledge.width,
                    ledge.height);
            }
        }

        private static void BakeMoonRoomShell(
            Tilemap terrain,
            TileBase tile,
            RectInt room,
            RectInt[] openings)
        {
            for (int y = room.yMin; y < room.yMax; y++)
            {
                for (int x = room.xMin; x < room.xMax; x++)
                {
                    bool ring =
                        x == room.xMin
                        || x == room.xMax - 1
                        || y == room.yMin
                        || y == room.yMax - 1;
                    if (!ring || IsInsideAnyOpening(openings, x, y))
                    {
                        continue;
                    }

                    terrain.SetTile(
                        new Vector3Int(x, y, 0),
                        tile);
                }
            }
        }

        private static bool IsInsideAnyOpening(
            RectInt[] openings,
            int x,
            int y)
        {
            for (int index = 0; index < openings.Length; index++)
            {
                if (openings[index].Contains(new Vector2Int(x, y)))
                {
                    return true;
                }
            }

            return false;
        }

        private static void CreateMoonRoomBounds(
            Transform parent,
            IReadOnlyList<MoonRoom> rooms)
        {
            GameObject root =
                CreateChild(parent, "MapRooms_FocusBounds");
            for (int index = 0; index < rooms.Count; index++)
            {
                MoonRoom room = rooms[index];
                GameObject roomObject = CreateChild(
                    root.transform,
                    $"Room_{index:00}_{room.Id}");
                Rect rect = room.WorldRect;
                roomObject.transform.position =
                    new Vector3(rect.center.x, rect.center.y, 0f);
                roomObject.AddComponent<RoomBounds2D>().Configure(
                    room.Id,
                    rect,
                    room.MainRoute);
            }
        }

        private static Vector3 MoonShopLocalPosition(P10StageId id)
        {
            if (!TryCreateMoonRoomPlan(id, out MoonStagePlan plan)
                || plan.ShopRoomIndex < 0)
            {
                return new Vector3(-47f, 0f, 0f);
            }

            Rect rect = plan.Rooms[plan.ShopRoomIndex].WorldRect;
            return new Vector3(
                rect.center.x - P5ShopAuthoredCenterX,
                rect.yMin,
                0f);
        }

        private static Vector3 MoonRoomAnchorOrDefault(
            P10StageId id,
            MoonRoomSlot slot,
            Vector3 fallback)
        {
            if (!TryCreateMoonRoomPlan(id, out MoonStagePlan plan))
            {
                return fallback;
            }

            int roomIndex = slot == MoonRoomSlot.Boss
                ? plan.BossRoomIndex
                : plan.ArchiveRoomIndex;
            if (roomIndex < 0)
            {
                return fallback;
            }

            Rect rect = plan.Rooms[roomIndex].WorldRect;
            return new Vector3(
                rect.center.x,
                rect.yMin + (slot == MoonRoomSlot.Boss ? 4f : 2.6f),
                0f);
        }

        private static Vector2 MoonRoomGoldPoint(
            MoonStagePlan plan,
            int goldIndex)
        {
            float[] spread = { 0.30f, 0.62f, 0.18f, 0.74f, 0.46f };
            MoonRoom room =
                plan.Rooms[plan.GoldRoomIndices[goldIndex]];
            Rect rect = room.WorldRect;
            return new Vector2(
                Mathf.Lerp(
                    rect.xMin + 2.5f,
                    rect.xMax - 2.5f,
                    spread[goldIndex]),
                rect.yMin + 1.7f);
        }

        private static bool TryCreateMoonRoomPlan(
            P10StageId id,
            out MoonStagePlan plan)
        {
            if (id == P10StageId.MoonPalace12)
            {
                plan = CreateMoonPalace12Plan();
                return true;
            }

            if (id == P10StageId.MoonPalace13)
            {
                plan = CreateMoonPalace13Plan();
                return true;
            }

            plan = default(MoonStagePlan);
            return false;
        }

        private static MoonStagePlan CreateMoonPalace12Plan()
        {
            MoonRoom[] rooms =
            {
                new MoonRoom(
                    "MP12_Start_MillGate",
                    new RectInt(0, 0, 12, 8),
                    true),
                new MoonRoom(
                    "MP12_Teach_SoftSoilBed",
                    new RectInt(12, 0, 12, 8),
                    true),
                new MoonRoom(
                    "MP12_Shop_MomoCorridor",
                    new RectInt(24, 0, 24, 8),
                    true),
                new MoonRoom(
                    "MP12_Exit_CassiaShaft",
                    new RectInt(48, 0, 12, 16),
                    true),
                new MoonRoom(
                    "MP12_Record_StarArchive",
                    new RectInt(0, 8, 12, 8),
                    false),
                new MoonRoom(
                    "MP12_Loop_UpperReturn",
                    new RectInt(12, 8, 12, 8),
                    false),
                new MoonRoom(
                    "MP12_Maru_HomecomingHall",
                    new RectInt(24, 8, 12, 8),
                    false),
                new MoonRoom(
                    "MP12_Treasure_MoonDough",
                    new RectInt(36, 8, 12, 8),
                    false)
            };
            RectInt[] openings =
            {
                new RectInt(11, 1, 2, 3),
                new RectInt(23, 1, 2, 3),
                new RectInt(47, 1, 2, 3),
                new RectInt(47, 9, 2, 3),
                new RectInt(35, 9, 2, 3),
                new RectInt(23, 9, 2, 3),
                new RectInt(11, 9, 2, 3),
                new RectInt(3, 7, 2, 2)
            };
            RectInt[] ledges =
            {
                new RectInt(49, 2, 5, 1),
                new RectInt(54, 4, 5, 1),
                new RectInt(49, 6, 5, 1),
                new RectInt(49, 8, 8, 1)
            };
            RectInt[] softSoil =
            {
                new RectInt(28, 7, 3, 2)
            };
            return new MoonStagePlan(
                new Vector2Int(60, 16),
                rooms,
                openings,
                ledges,
                softSoil,
                new Vector2Int(2, 1),
                new Vector2Int(56, 1),
                2,
                4,
                -1,
                new[] { 0, 1, 2, 6, 7 });
        }

        private static MoonStagePlan CreateMoonPalace13Plan()
        {
            MoonRoom[] rooms =
            {
                new MoonRoom(
                    "MP13_Start_DoughGate",
                    new RectInt(0, 16, 12, 8),
                    true),
                new MoonRoom(
                    "MP13_Teach_CrackedFloor",
                    new RectInt(12, 16, 12, 8),
                    true),
                new MoonRoom(
                    "MP13_Choice_Junction",
                    new RectInt(24, 16, 12, 8),
                    true),
                new MoonRoom(
                    "MP13_Treasure_SoftSoilVault",
                    new RectInt(36, 16, 12, 8),
                    false),
                new MoonRoom(
                    "MP13_Descent_MoonDoughShaft",
                    new RectInt(24, 0, 12, 16),
                    true),
                new MoonRoom(
                    "MP13_Mercy_QuietHollow",
                    new RectInt(12, 8, 12, 8),
                    false),
                new MoonRoom(
                    "MP13_Supply_PreBossCorridor",
                    new RectInt(36, 8, 12, 8),
                    true),
                new MoonRoom(
                    "MP13_Boss_KungtteokiHall",
                    new RectInt(36, 0, 24, 8),
                    true)
            };
            RectInt[] openings =
            {
                new RectInt(11, 17, 2, 3),
                new RectInt(23, 17, 2, 3),
                new RectInt(35, 17, 2, 3),
                new RectInt(27, 15, 4, 2),
                new RectInt(23, 9, 2, 3),
                new RectInt(35, 9, 2, 3),
                new RectInt(40, 7, 2, 2)
            };
            RectInt[] ledges =
            {
                new RectInt(27, 14, 2, 1),
                new RectInt(25, 12, 4, 1),
                new RectInt(27, 10, 4, 1),
                new RectInt(25, 8, 5, 1),
                new RectInt(31, 8, 4, 1),
                new RectInt(25, 6, 5, 1),
                new RectInt(30, 4, 5, 1),
                new RectInt(25, 2, 5, 1)
            };
            RectInt[] softSoil =
            {
                new RectInt(31, 15, 3, 2)
            };
            return new MoonStagePlan(
                new Vector2Int(60, 24),
                rooms,
                openings,
                ledges,
                softSoil,
                new Vector2Int(2, 17),
                new Vector2Int(54, 1),
                -1,
                -1,
                7,
                new[] { 0, 1, 3, 5, 4 });
        }

        private static P10CampaignCatalog RebuildCatalog()
        {
            EnsureFolder(
                Path.GetDirectoryName(CatalogPath)
                    ?.Replace('\\', '/'));
            AssetDatabase.DeleteAsset(CatalogPath);
            P10CampaignCatalog catalog =
                ScriptableObject.CreateInstance<P10CampaignCatalog>();
            catalog.name = "P10_FirstBranchCampaignCatalog";
            P10StageMechanics moonMechanics =
                P10StageMechanics.SoftMoonSoil
                | P10StageMechanics.RollingMoonCake
                | P10StageMechanics.MortarStakeRoots
                | P10StageMechanics.BombPickaxePestleContrast;
            P10StageMechanics magpieMechanics =
                P10StageMechanics.HorizontalLongRooms
                | P10StageMechanics.ReturnableLowerRoute
                | P10StageMechanics.RopeHookUmbrella
                | P10StageMechanics.TemporaryMagpiePlatforms
                | P10StageMechanics.WindAndSwayingBridge;
            P10StageMechanics dragonMechanics =
                P10StageMechanics.VerticalWaterTank
                | P10StageMechanics.CellWaterCurrent
                | P10StageMechanics.FloodgateAndDrain
                | P10StageMechanics.ShovelBombHook
                | P10StageMechanics.CurrentTraversal;
            catalog.Configure(
                new[]
                {
                    Stage(
                        P10StageId.MoonPalace11,
                        "분화구 작업장",
                        RoomRegion.MoonPalace,
                        P6StageSlot.X1,
                        P6StageArchetype.Traverse,
                        P9BranchKind.None,
                        P10TraversalAxis.Mixed,
                        moonMechanics,
                        P10StageGuarantees.None,
                        P10BossKind.None,
                        "빈 절구의 달토끼를 보며 월궁 지형과 절굿공이 사건을 안전하게 익힌다.",
                        2,
                        3),
                    Stage(
                        P10StageId.MoonPalace12,
                        "계수나무 방앗간",
                        RoomRegion.MoonPalace,
                        P6StageSlot.X2,
                        P6StageArchetype.Circuit,
                        P9BranchKind.None,
                        P10TraversalAxis.Mixed,
                        moonMechanics,
                        P10StageGuarantees.LargeJunction
                        | P10StageGuarantees.ClosedLoop
                        | P10StageGuarantees.MomoShop
                        | P10StageGuarantees.HomecomingStatue
                        | P10StageGuarantees.StarArchive,
                        P10BossKind.None,
                        "큰 교차방에서 상점과 기록관을 돌아본 뒤 월궁 선물 연쇄를 완성한다.",
                        3,
                        5),
                    Stage(
                        P10StageId.MoonPalace13,
                        "달반죽 지하",
                        RoomRegion.MoonPalace,
                        P6StageSlot.X3,
                        P6StageArchetype.Descent,
                        P9BranchKind.None,
                        P10TraversalAxis.Mixed,
                        moonMechanics | P10StageMechanics.BossArena,
                        P10StageGuarantees.SupplyCorridor
                        | P10StageGuarantees.Boss
                        | P10StageGuarantees.TwoBranchExits,
                        P10BossKind.Kungtteoki,
                        "금 간 바닥과 세 별매듭을 이용해 쿵떡이를 쓰러뜨리고 첫 갈래를 고른다.",
                        3,
                        5),
                    Stage(
                        P10StageId.MagpieBridge21,
                        "배고픈 까치",
                        RoomRegion.MagpieBridge,
                        P6StageSlot.X1,
                        P6StageArchetype.Traverse,
                        P9BranchKind.MagpieBridge,
                        P10TraversalAxis.Horizontal,
                        magpieMechanics,
                        P10StageGuarantees.None,
                        P10BossKind.None,
                        "달떡이나 구조 행동으로 까치의 다리를 얻되 기본 점프 경로도 유지한다.",
                        2,
                        3),
                    Stage(
                        P10StageId.MagpieBridge22,
                        "끊어진 둥지",
                        RoomRegion.MagpieBridge,
                        P6StageSlot.X2,
                        P6StageArchetype.Circuit,
                        P9BranchKind.MagpieBridge,
                        P10TraversalAxis.Horizontal,
                        magpieMechanics,
                        P10StageGuarantees.ClosedLoop
                        | P10StageGuarantees.HomecomingStatue
                        | P10StageGuarantees.StarArchive,
                        P10BossKind.None,
                        "가로 순환로의 둥지를 수리해 다음 발판과 매듭거미 약점 지원을 준비한다.",
                        3,
                        5),
                    Stage(
                        P10StageId.MagpieBridge23,
                        "실의 탑",
                        RoomRegion.MagpieBridge,
                        P6StageSlot.X3,
                        P6StageArchetype.Ascent,
                        P9BranchKind.MagpieBridge,
                        P10TraversalAxis.Horizontal,
                        magpieMechanics | P10StageMechanics.BossArena,
                        P10StageGuarantees.SupplyCorridor
                        | P10StageGuarantees.Boss
                        | P10StageGuarantees.BranchRelic,
                        P10BossKind.KnotSpider,
                        "매듭거미의 세 지지 실을 끊거나 별매듭을 공격해 붉은 실을 얻는다.",
                        3,
                        5),
                    Stage(
                        P10StageId.DragonPalace21,
                        "자라의 물길",
                        RoomRegion.DragonPalace,
                        P6StageSlot.X1,
                        P6StageArchetype.Ascent,
                        P9BranchKind.DragonPalace,
                        P10TraversalAxis.Vertical,
                        dragonMechanics,
                        P10StageGuarantees.None,
                        P10BossKind.None,
                        "약이나 회복 샘 유도로 자라를 돕고 안전한 급류와 보물방을 연다.",
                        2,
                        3),
                    Stage(
                        P10StageId.DragonPalace22,
                        "용문 폭포",
                        RoomRegion.DragonPalace,
                        P6StageSlot.X2,
                        P6StageArchetype.Ascent,
                        P9BranchKind.DragonPalace,
                        P10TraversalAxis.Vertical,
                        dragonMechanics,
                        P10StageGuarantees.ClosedLoop
                        | P10StageGuarantees.HomecomingStatue
                        | P10StageGuarantees.StarArchive,
                        P10BossKind.None,
                        "세로 수조의 물길을 복구해 잉어와 다음 스테이지 구름용 지원을 잇는다.",
                        3,
                        5),
                    Stage(
                        P10StageId.DragonPalace23,
                        "수문 궁전",
                        RoomRegion.DragonPalace,
                        P6StageSlot.X3,
                        P6StageArchetype.Ascent,
                        P9BranchKind.DragonPalace,
                        P10TraversalAxis.Vertical,
                        dragonMechanics | P10StageMechanics.BossArena,
                        P10StageGuarantees.SupplyCorridor
                        | P10StageGuarantees.Boss
                        | P10StageGuarantees.BranchRelic,
                        P10BossKind.DragonGatekeeper,
                        "물대포를 세 수문에 유도하거나 별매듭을 공격해 여의주를 얻는다.",
                        3,
                        5)
                },
                new[]
                {
                    BranchFeel(
                        P9BranchKind.MagpieBridge,
                        RoomRegion.MagpieBridge,
                        P10TraversalAxis.Horizontal,
                        magpieMechanics,
                        35,
                        0,
                        20,
                        "가로 바람과 낙하 후 복귀가 이어지는 공중 다리 여행."),
                    BranchFeel(
                        P9BranchKind.DragonPalace,
                        RoomRegion.DragonPalace,
                        P10TraversalAxis.Vertical,
                        dragonMechanics,
                        0,
                        30,
                        25,
                        "세로 수조와 물살·수문을 타고 오르는 수중 여행.")
                });
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static P10StageDefinition Stage(
            P10StageId id,
            string name,
            RoomRegion region,
            P6StageSlot slot,
            P6StageArchetype archetype,
            P9BranchKind branch,
            P10TraversalAxis axis,
            P10StageMechanics mechanics,
            P10StageGuarantees guarantees,
            P10BossKind boss,
            string sentence,
            int minMinutes,
            int maxMinutes)
        {
            P10StageDefinition definition =
                new P10StageDefinition();
            definition.Configure(
                id,
                name,
                region,
                slot,
                archetype,
                branch,
                axis,
                mechanics,
                guarantees,
                boss,
                sentence,
                minMinutes,
                maxMinutes,
                true,
                true);
            return definition;
        }

        private static P10BranchFeelDefinition BranchFeel(
            P9BranchKind branch,
            RoomRegion region,
            P10TraversalAxis axis,
            P10StageMechanics mechanics,
            int wide,
            int tall,
            int large,
            string sentence)
        {
            P10BranchFeelDefinition definition =
                new P10BranchFeelDefinition();
            definition.Configure(
                branch,
                region,
                axis,
                mechanics,
                wide,
                tall,
                large,
                sentence);
            return definition;
        }

        private static BuildAssets LoadAssets()
        {
            GameObject bombObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    P5MoonPalaceSliceBuilder.BombPrefabPath);
            return new BuildAssets(
                LoadSprite(SquareSpritePath),
                LoadSprite(StarSpritePath),
                LoadSprite(MoonSpritePath, "Moon A"),
                LoadSprite(TreeSpritePath, "Mount tree_0"),
                LoadSprite(BridgeSpritePath, "Bridge_4"),
                LoadSprite(BirdSpritePath, "birds"),
                LoadSprite(CloudSpritePath, "clouds_2"),
                LoadSprite(
                    UnderwaterBackgroundPath,
                    "Underwater background"),
                LoadSprite(
                    UnderwaterGroundPath,
                    "Underwater ground fill"),
                LoadSprite(SeaweedSpritePath, "Seaweed_4"),
                LoadSprite(FishSpritePath, "Fishs_9"),
                LoadSprite(BubbleSpritePath, "Bubble particle"),
                AssetDatabase.LoadAssetAtPath<TileBase>(
                    ReinforcedTilePath),
                AssetDatabase.LoadAssetAtPath<TileBase>(
                    SoftSoilTilePath),
                AssetDatabase.LoadAssetAtPath<TileBase>(
                    OneWayTilePath),
                new[]
                {
                    AssetDatabase.LoadAssetAtPath<TileDefinition>(
                        ReinforcedDefinitionPath),
                    AssetDatabase.LoadAssetAtPath<TileDefinition>(
                        SoftSoilDefinitionPath)
                },
                bombObject != null
                    ? bombObject.GetComponent<Bomb2D>()
                    : null);
        }

        private static void ValidateAssets(BuildAssets assets)
        {
            if (!assets.IsComplete)
            {
                throw new InvalidOperationException(
                    "P10 Moon/Magpie/Dragon source art or gameplay assets "
                    + "are incomplete.");
            }
        }

        private static Vector2Int StageSize(
            P10StageDefinition definition)
        {
            if (TryCreateMoonRoomPlan(
                    definition.StageId,
                    out MoonStagePlan moonPlan))
            {
                return moonPlan.Size;
            }

            if (definition.Region == RoomRegion.MagpieBridge)
            {
                return new Vector2Int(
                    definition.StageSlot == P6StageSlot.X2 ? 46 : 42,
                    20);
            }

            if (definition.Region == RoomRegion.DragonPalace)
            {
                return new Vector2Int(
                    30,
                    definition.StageSlot == P6StageSlot.X2 ? 30 : 28);
            }

            return new Vector2Int(
                definition.StageSlot == P6StageSlot.X2 ? 44 : 38,
                definition.StageSlot == P6StageSlot.X2 ? 22 : 20);
        }

        private static P10StageNode2D FindNode(
            IEnumerable<P10StageNode2D> nodes,
            P10StageId id)
        {
            return nodes.First(item => item.StageId == id);
        }

        private static void TintTilemaps(
            RoomRegion region,
            TilemapRenderer terrain,
            TilemapRenderer oneWay)
        {
            Color terrainColor;
            Color oneWayColor;
            switch (region)
            {
                case RoomRegion.MagpieBridge:
                    terrainColor =
                        new Color(0.36f, 0.29f, 0.62f, 1f);
                    oneWayColor =
                        new Color(0.82f, 0.72f, 1f, 1f);
                    break;
                case RoomRegion.DragonPalace:
                    terrainColor =
                        new Color(0.08f, 0.42f, 0.55f, 1f);
                    oneWayColor =
                        new Color(0.32f, 0.88f, 0.90f, 1f);
                    break;
                default:
                    terrainColor =
                        new Color(0.42f, 0.34f, 0.64f, 1f);
                    oneWayColor =
                        new Color(0.82f, 0.68f, 0.92f, 1f);
                    break;
            }

            Tilemap terrainTilemap = terrain.GetComponent<Tilemap>();
            Tilemap oneWayTilemap = oneWay.GetComponent<Tilemap>();
            if (terrainTilemap != null)
            {
                terrainTilemap.color = terrainColor;
            }

            if (oneWayTilemap != null)
            {
                oneWayTilemap.color = oneWayColor;
            }
        }

        private static Tilemap CreateTilemapLayer(
            Transform parent,
            string name,
            int sortingOrder,
            bool compositeSolid,
            bool oneWay,
            out TilemapCollider2D tilemapCollider,
            out CompositeCollider2D composite)
        {
            GameObject layer = CreateChild(parent, name);
            Tilemap tilemap = layer.AddComponent<Tilemap>();
            TilemapRenderer renderer =
                layer.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            tilemapCollider = null;
            composite = null;
            if (!compositeSolid && !oneWay)
            {
                return tilemap;
            }

            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                layer.layer = groundLayer;
            }

            Rigidbody2D body = layer.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            tilemapCollider = layer.AddComponent<TilemapCollider2D>();
            if (compositeSolid)
            {
                tilemapCollider.compositeOperation =
                    Collider2D.CompositeOperation.Merge;
                composite = layer.AddComponent<CompositeCollider2D>();
                composite.geometryType =
                    CompositeCollider2D.GeometryType.Polygons;
                composite.generationType =
                    CompositeCollider2D.GenerationType.Synchronous;
            }
            else
            {
                tilemapCollider.usedByEffector = true;
                PlatformEffector2D effector =
                    layer.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.surfaceArc = 180f;
            }

            return tilemap;
        }

        private static void SetRect(
            Tilemap tilemap,
            TileBase tile,
            int x,
            int y,
            int width,
            int height)
        {
            for (int offsetY = 0; offsetY < height; offsetY++)
            {
                for (int offsetX = 0; offsetX < width; offsetX++)
                {
                    tilemap.SetTile(
                        new Vector3Int(
                            x + offsetX,
                            y + offsetY,
                            0),
                        tile);
                }
            }
        }

        private static GridPos[] ProtectedExitCells(
            Vector2Int exit)
        {
            return new[]
            {
                new GridPos(exit.x, exit.y),
                new GridPos(exit.x, exit.y - 1),
                new GridPos(exit.x - 1, exit.y),
                new GridPos(exit.x + 1, exit.y)
            };
        }

        private static GameObject CreateSpritePart(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 localPosition,
            Vector2 scale,
            Color color,
            int sortingOrder)
        {
            GameObject child = CreateChild(parent, name);
            child.transform.localPosition =
                new Vector3(localPosition.x, localPosition.y, 0f);
            child.transform.localScale =
                new Vector3(scale.x, scale.y, 1f);
            SpriteRenderer renderer =
                child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return child;
        }

        private static Sprite LoadSprite(
            string path,
            string exactName = null)
        {
            Sprite[] sprites =
                AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<Sprite>()
                    .ToArray();
            if (sprites.Length == 0)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(exactName))
            {
                Sprite exact = sprites.FirstOrDefault(
                    item => item.name == exactName);
                if (exact != null)
                {
                    return exact;
                }
            }

            return sprites[0];
        }

        private static Transform FindTransform(
            Transform root,
            string name)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == name);
        }

        private static Transform RequireTransform(
            Transform root,
            string name)
        {
            Transform found = FindTransform(root, name);
            if (found == null)
            {
                throw new InvalidOperationException(
                    $"Required P5 object is missing: {name}");
            }

            return found;
        }

        private static void MoveUnderPersistent(
            GameObject target,
            Transform persistent)
        {
            if (target == null)
            {
                throw new InvalidOperationException(
                    "A persistent production object is missing.");
            }

            target.transform.SetParent(persistent, true);
        }

        private static GameObject CreateChild(
            Transform parent,
            string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes =
                EditorBuildSettings.scenes.ToList();
            int index = scenes.FindIndex(
                item => item.path == scenePath);
            if (index < 0)
            {
                scenes.Add(
                    new EditorBuildSettingsScene(scenePath, true));
            }
            else
            {
                scenes[index] =
                    new EditorBuildSettingsScene(scenePath, true);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path)
                || AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent =
                Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private enum MoonRoomSlot
        {
            Archive,
            Boss
        }

        private readonly struct MoonRoom
        {
            public readonly string Id;
            public readonly RectInt Cells;
            public readonly bool MainRoute;

            public MoonRoom(
                string id,
                RectInt cells,
                bool mainRoute)
            {
                Id = id;
                Cells = cells;
                MainRoute = mainRoute;
            }

            public Rect WorldRect => new Rect(
                Cells.xMin,
                Cells.yMin,
                Cells.width,
                Cells.height);
        }

        private readonly struct MoonStagePlan
        {
            public readonly Vector2Int Size;
            public readonly MoonRoom[] Rooms;
            public readonly RectInt[] Openings;
            public readonly RectInt[] Ledges;
            public readonly RectInt[] SoftSoil;
            public readonly Vector2Int EntryCell;
            public readonly Vector2Int ExitCell;
            public readonly int ShopRoomIndex;
            public readonly int ArchiveRoomIndex;
            public readonly int BossRoomIndex;
            public readonly int[] GoldRoomIndices;

            public MoonStagePlan(
                Vector2Int size,
                MoonRoom[] rooms,
                RectInt[] openings,
                RectInt[] ledges,
                RectInt[] softSoil,
                Vector2Int entryCell,
                Vector2Int exitCell,
                int shopRoomIndex,
                int archiveRoomIndex,
                int bossRoomIndex,
                int[] goldRoomIndices)
            {
                Size = size;
                Rooms = rooms;
                Openings = openings;
                Ledges = ledges;
                SoftSoil = softSoil;
                EntryCell = entryCell;
                ExitCell = exitCell;
                ShopRoomIndex = shopRoomIndex;
                ArchiveRoomIndex = archiveRoomIndex;
                BossRoomIndex = bossRoomIndex;
                GoldRoomIndices = goldRoomIndices;
            }
        }

        private readonly struct BuildAssets
        {
            public readonly Sprite Square;
            public readonly Sprite Star;
            public readonly Sprite Moon;
            public readonly Sprite Tree;
            public readonly Sprite Bridge;
            public readonly Sprite Bird;
            public readonly Sprite Cloud;
            public readonly Sprite UnderwaterBackground;
            public readonly Sprite UnderwaterGround;
            public readonly Sprite Seaweed;
            public readonly Sprite Fish;
            public readonly Sprite Bubble;
            public readonly TileBase ReinforcedTile;
            public readonly TileBase SoftSoilTile;
            public readonly TileBase OneWayTile;
            public readonly TileDefinition[] TileDefinitions;
            public readonly Bomb2D BombPrefab;

            public BuildAssets(
                Sprite square,
                Sprite star,
                Sprite moon,
                Sprite tree,
                Sprite bridge,
                Sprite bird,
                Sprite cloud,
                Sprite underwaterBackground,
                Sprite underwaterGround,
                Sprite seaweed,
                Sprite fish,
                Sprite bubble,
                TileBase reinforcedTile,
                TileBase softSoilTile,
                TileBase oneWayTile,
                TileDefinition[] tileDefinitions,
                Bomb2D bombPrefab)
            {
                Square = square;
                Star = star;
                Moon = moon;
                Tree = tree;
                Bridge = bridge;
                Bird = bird;
                Cloud = cloud;
                UnderwaterBackground = underwaterBackground;
                UnderwaterGround = underwaterGround;
                Seaweed = seaweed;
                Fish = fish;
                Bubble = bubble;
                ReinforcedTile = reinforcedTile;
                SoftSoilTile = softSoilTile;
                OneWayTile = oneWayTile;
                TileDefinitions = tileDefinitions;
                BombPrefab = bombPrefab;
            }

            public bool IsComplete =>
                Square != null
                && Star != null
                && Moon != null
                && Tree != null
                && Bridge != null
                && Bird != null
                && Cloud != null
                && UnderwaterBackground != null
                && UnderwaterGround != null
                && Seaweed != null
                && Fish != null
                && Bubble != null
                && ReinforcedTile != null
                && SoftSoilTile != null
                && OneWayTile != null
                && TileDefinitions != null
                && TileDefinitions.Length == 2
                && TileDefinitions.All(item => item != null)
                && BombPrefab != null;
        }
    }
}

#endif
