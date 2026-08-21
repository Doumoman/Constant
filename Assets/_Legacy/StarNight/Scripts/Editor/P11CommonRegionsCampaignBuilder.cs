#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StarNight.Campaign.P10;
using StarNight.Campaign.P11;
using StarNight.Debugging;
using StarNight.Explosions;
using StarNight.Folklore.P9;
using StarNight.Generation.P6;
using StarNight.Grid;
using StarNight.MapHarness.P11;
using StarNight.Maru.P8;
using StarNight.Objects;
using StarNight.Player;
using StarNight.Rooms;
using StarNight.Stages.P5;
using StarNight.Tiles;
using StarNight.Tools;
using StarNight.Tools.Pestle;
using StarNight.Tools.Rope;
using StarNight.Tools.Water;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Editor
{
    public static class P11CommonRegionsCampaignBuilder
    {
        public const string ProductionScenePath =
            "Assets/StarNight/Scenes/Game/"
            + "P11_FullJourney_CommonEndings.unity";
        public const string CatalogPath =
            "Assets/StarNight/Data/P11/"
            + "P11_CommonRegionCampaignCatalog.asset";

        private const string GuestCatalogPath =
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

        private const string SquareSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/"
            + "Square.png";
        private const string StarSpritePath =
            "Assets/2D Fantasy sprite bundle/"
            + "Cristal Dungeon sprite pack/Cristal Sprites/"
            + "Star particle.png";
        private const string BirdSpritePath =
            "Assets/2D Fantasy sprite bundle/Island pack/Sprites/"
            + "birds.png";
        private const string ArrowSpritePath =
            "Assets/2D Fantasy sprite bundle/Bonus/--CONCEPT--/"
            + "Concept sprites/arrow.png";

        private const string PostSkyPath =
            "Assets/2D Fantasy sprite bundle/Abandoned station/"
            + "Ancient base Sprites/Sky.png";
        private const string PostCorePath =
            "Assets/2D Fantasy sprite bundle/Abandoned station/"
            + "Ancient base Sprites/Base Core.png";
        private const string PostBoxesPath =
            "Assets/2D Fantasy sprite bundle/Abandoned station/"
            + "Ancient base Sprites/Base boxes.png";
        private const string PostDoorPath =
            "Assets/2D Fantasy sprite bundle/Abandoned station/"
            + "Ancient base Sprites/Base door.png";

        private const string GardenBackgroundPath =
            "Assets/2D Fantasy sprite bundle/Spring forest/Sprites/"
            + "Background trees.png";
        private const string GardenSunPath =
            "Assets/2D Fantasy sprite bundle/Spring forest/Sprites/"
            + "SunLight.png";
        private const string GardenMonolithPath =
            "Assets/2D Fantasy sprite bundle/Spring forest/Sprites/"
            + "Ver 1.5/Monolith with tree.png";
        private const string GardenFlowersPath =
            "Assets/2D Fantasy sprite bundle/Spring forest/Sprites/"
            + "Flowers.png";

        private const string ObservatoryBackgroundPath =
            "Assets/2D Fantasy sprite bundle/"
            + "Cristal Dungeon sprite pack/Cristal Sprites/"
            + "Background A.png";
        private const string ObservatoryCrystalsPath =
            "Assets/2D Fantasy sprite bundle/"
            + "Cristal Dungeon sprite pack/Cristal Sprites/"
            + "Crystals.png";
        private const string ObservatoryTreesPath =
            "Assets/2D Fantasy sprite bundle/"
            + "Cristal Dungeon sprite pack/Cristal Sprites/"
            + "Crystal trees.png";
        private const string ObservatorySkyPath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/"
            + "Sky A.png";

        private const string PreviewRootName =
            "__P11_PREVIEW_StarPostOffice31_DONT_SAVE__";

        [MenuItem(
            "StarNight/P11/Rebuild Full Journey Common Endings")]
        public static void Rebuild()
        {
            P10FirstBranchCampaignBuilder.Rebuild();
            EnsureFolder(
                Path.GetDirectoryName(ProductionScenePath)
                    ?.Replace('\\', '/'));
            EnsureFolder(
                Path.GetDirectoryName(CatalogPath)
                    ?.Replace('\\', '/'));

            Scene p10Scene = SceneManager.GetActiveScene();
            if (p10Scene.path
                != P10FirstBranchCampaignBuilder.ProductionScenePath)
            {
                p10Scene = EditorSceneManager.OpenScene(
                    P10FirstBranchCampaignBuilder.ProductionScenePath,
                    OpenSceneMode.Single);
            }

            if (!EditorSceneManager.SaveScene(
                    p10Scene,
                    ProductionScenePath,
                    true))
            {
                throw new InvalidOperationException(
                    "Failed to copy the cumulative P10 production scene.");
            }

            Scene scene = EditorSceneManager.OpenScene(
                ProductionScenePath,
                OpenSceneMode.Single);
            P11CampaignCatalog catalog = RebuildCatalog();
            P9RecordGuestCatalog guestCatalog =
                AssetDatabase.LoadAssetAtPath<P9RecordGuestCatalog>(
                    GuestCatalogPath);
            if (guestCatalog == null)
            {
                throw new InvalidOperationException(
                    "P11 requires the P9 record guest catalog.");
            }

            BuildAssets assets = LoadAssets();
            ValidateAssets(assets);
            BuildProductionCampaign(
                scene,
                catalog,
                guestCatalog,
                assets);
            ValidateSceneOrThrow(scene, false);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    ProductionScenePath))
            {
                throw new InvalidOperationException(
                    "Failed to save the cumulative P11 production scene.");
            }

            AddSceneToBuildSettings(ProductionScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[StarNight P11] Full journey production rebuilt: "
                + "one persistent core, nine P10 environments, "
                + "nine variable P11 environments, and three endings.");
        }

        [MenuItem(
            "StarNight/P11/Validate Full Journey Common Endings")]
        public static void Validate()
        {
            DestroyPreviewIfPresent();
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ProductionScenePath)
            {
                scene = EditorSceneManager.OpenScene(
                    ProductionScenePath,
                    OpenSceneMode.Single);
            }

            ValidateSceneOrThrow(scene, true);
            Debug.Log(
                "[StarNight P11] Structural validation PASS. "
                + "The three 80% comprehension gates remain "
                + "real-player tests.");
        }

        [MenuItem(
            "StarNight/P11/Preview Star Post Office 3-1")]
        public static void PreviewStarPostOffice31()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ProductionScenePath)
            {
                scene = EditorSceneManager.OpenScene(
                    ProductionScenePath,
                    OpenSceneMode.Single);
            }

            DestroyPreviewIfPresent();
            P11StageEnvironment2D source =
                FindP11Environment(P11StageId.StarPostOffice31);
            if (source == null || source.EnvironmentRoot == null)
            {
                throw new InvalidOperationException(
                    "P11 Star Post Office 3-1 is missing.");
            }

            GameObject preview = UnityEngine.Object.Instantiate(
                source.EnvironmentRoot);
            preview.name = PreviewRootName;
            preview.hideFlags =
                HideFlags.DontSaveInEditor
                | HideFlags.DontSaveInBuild;
            preview.SetActive(true);
            Selection.activeGameObject = preview;

            SceneView view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                Vector3 center = source.CameraAnchor != null
                    ? source.CameraAnchor.position
                    : source.GridWorld.WorldBounds.center;
                view.LookAt(
                    center,
                    Quaternion.identity,
                    14f,
                    false,
                    false);
                view.Repaint();
            }

            Debug.Log(
                "[StarNight P11] Showing a DontSave preview clone of "
                + "Star Post Office 3-1. Production save state is unchanged.");
        }

        private static void BuildProductionCampaign(
            Scene scene,
            P11CampaignCatalog catalog,
            P9RecordGuestCatalog guestCatalog,
            BuildAssets assets)
        {
            P10FirstBranchCampaignContract p10Contract =
                FindSingle<P10FirstBranchCampaignContract>();
            P10CampaignDirector2D p10Director =
                FindSingle<P10CampaignDirector2D>();
            P10StageFlowController2D p10Flow =
                FindSingle<P10StageFlowController2D>();
            P9FolkloreChainState2D folklore =
                FindSingle<P9FolkloreChainState2D>();
            PlayerMotor2D player =
                FindSingle<PlayerMotor2D>();
            Camera camera = FindSingle<Camera>();
            P8MaruStageController2D maru =
                p10Flow.MaruController
                ?? FindSingle<P8MaruStageController2D>();
            if (p10Contract == null
                || p10Director == null
                || p10Flow == null
                || folklore == null
                || player == null
                || camera == null
                || maru == null)
            {
                throw new InvalidOperationException(
                    "The cumulative P10 persistent core is incomplete.");
            }

            GameObject root = p10Contract.transform.root.gameObject;
            root.name = "P11_FullJourney_CommonEndings";
            Transform persistent =
                FindTransform(root.transform, "PersistentCampaignCore");
            if (persistent == null)
            {
                throw new InvalidOperationException(
                    "The P10 persistent campaign core is missing.");
            }

            GameObject systems = CreateChild(
                persistent,
                "P11Systems_CommonRegionsAndEndings");
            P11StoryState2D story =
                systems.AddComponent<P11StoryState2D>();
            story.Configure(folklore);
            P11ComprehensionTelemetry2D telemetry =
                systems.AddComponent<P11ComprehensionTelemetry2D>();
            telemetry.ConfigureMinimumHumanSamples(5);
            P11CampaignDirector2D director =
                systems.AddComponent<P11CampaignDirector2D>();
            director.Configure(
                catalog,
                p10Director,
                story,
                telemetry);
            P11StageFlowController2D flow =
                systems.AddComponent<P11StageFlowController2D>();

            GameObject environmentsRoot = CreateChild(
                root.transform,
                "P11StageEnvironments_9");
            GameObject registryRoot = CreateChild(
                root.transform,
                "P11StageRegistry_9");
            List<P11StageEnvironment2D> environments =
                new List<P11StageEnvironment2D>(9);
            for (int index = 0; index < catalog.Stages.Count; index++)
            {
                environments.Add(
                    CreateStageEnvironment(
                        environmentsRoot.transform,
                        catalog.Stages[index],
                        assets,
                        player.GetComponent<Collider2D>(),
                        story));
            }

            P11StageNode2D[] nodes = new P11StageNode2D[9];
            for (int index = 0; index < environments.Count; index++)
            {
                P11StageEnvironment2D environment =
                    environments[index];
                P11StageDefinition definition =
                    catalog.Find(environment.StageId);
                GameObject nodeObject = CreateChild(
                    registryRoot.transform,
                    $"Node_{definition.StageId}");
                P11StageNode2D node =
                    nodeObject.AddComponent<P11StageNode2D>();
                node.Configure(definition, director, environment);
                nodes[index] = node;
            }

            flow.Configure(
                director,
                nodes,
                player.transform,
                camera,
                assets.BombPrefab,
                maru);
            p10Flow.ConfigureCommonRegionContinuation(flow);
            for (int index = 0; index < nodes.Length; index++)
            {
                CreateStageExit(nodes[index], flow, assets);
            }

            CreateX2Shops(
                nodes,
                p10Contract,
                assets);
            CreateArchives(
                nodes,
                guestCatalog,
                player.transform,
                assets);
            P11NaraeMailbox2D mailbox =
                CreateNaraeMailbox(
                    FindNode(nodes, P11StageId.StarPostOffice33),
                    story,
                    assets);
            P11YoungCrowEvent2D youngCrow =
                CreateYoungCrow(
                    FindNode(nodes, P11StageId.SunriseGarden41),
                    story,
                    assets);
            P11CrowNestEvent2D nest =
                CreateCrowNest(
                    FindNode(nodes, P11StageId.SunriseGarden42),
                    story,
                    assets);
            P11RegionalBoss2D popo =
                CreateRegionalBoss(
                    FindNode(nodes, P11StageId.StarPostOffice33),
                    P11BossKind.Popo,
                    story,
                    assets);
            P11RegionalBoss2D sunFlower =
                CreateRegionalBoss(
                    FindNode(nodes, P11StageId.SunriseGarden43),
                    P11BossKind.SunFlower,
                    story,
                    assets);
            P11SunEmberPickup2D sunEmber =
                CreateSunEmber(
                    FindNode(nodes, P11StageId.SunriseGarden43),
                    story,
                    assets);
            P11MaruStoryTableau2D tableau =
                CreateMaruTableau(
                    FindNode(nodes, P11StageId.PolarisObservatory51),
                    assets);
            P11MaruFinalBoss2D maruBoss =
                CreateMaruFinalBoss(
                    FindNode(nodes, P11StageId.PolarisObservatory53),
                    flow,
                    story,
                    assets);
            P11EndingPresentation2D endingPresentation =
                CreateEndingPresentation(
                    FindNode(nodes, P11StageId.PolarisObservatory53),
                    director,
                    assets);

            P10StageEnvironment2D[] p10Environments =
                UnityEngine.Object.FindObjectsByType<
                        P10StageEnvironment2D>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .OrderBy(item => (int)item.StageId)
                    .ToArray();
            RetrofitP10DirectionCues(p10Environments, assets);
            P10MapDirectionalityAudit2D p10Audit =
                systems.AddComponent<P10MapDirectionalityAudit2D>();
            p10Audit.Configure(p10Environments);

            P11CommonRegionsCampaignContract contract =
                systems.AddComponent<P11CommonRegionsCampaignContract>();
            contract.Configure(
                root,
                catalog,
                p10Director,
                p10Flow,
                director,
                story,
                flow,
                nodes,
                new[] { popo, sunFlower },
                maruBoss,
                mailbox,
                youngCrow,
                nest,
                sunEmber,
                tableau,
                endingPresentation,
                telemetry);

            CreatePendingHumanGateMarkers(persistent);
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            camera.transform.rotation = Quaternion.identity;
            for (int index = 0; index < environments.Count; index++)
            {
                environments[index].SetEnvironmentActive(false);
            }

            Physics2D.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static P11CampaignCatalog RebuildCatalog()
        {
            EnsureFolder(
                Path.GetDirectoryName(CatalogPath)
                    ?.Replace('\\', '/'));
            P11CampaignCatalog catalog =
                AssetDatabase.LoadAssetAtPath<P11CampaignCatalog>(
                    CatalogPath);
            if (catalog == null)
            {
                catalog =
                    ScriptableObject.CreateInstance<P11CampaignCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            P11StageMechanics post31 =
                P11StageMechanics.ParcelConveyor
                | P11StageMechanics.SortingArm
                | P11StageMechanics.InkLabel
                | P11StageMechanics.ParcelLauncher;
            P11StageMechanics post32 =
                P11StageMechanics.ParcelConveyor
                | P11StageMechanics.MailTube
                | P11StageMechanics.ReturnStamp
                | P11StageMechanics.InkLabel;
            P11StageMechanics gardenCore =
                P11StageMechanics.RotatingSunRay
                | P11StageMechanics.SunFlowerPlatform;
            P11StageMechanics observatory51 =
                P11StageMechanics.ReturnField
                | P11StageMechanics.InvariantStarBlock
                | P11StageMechanics.MemoryBell;
            P11StageMechanics observatory52 =
                P11StageMechanics.OrbitPlatform
                | P11StageMechanics.GravityDial
                | P11StageMechanics.ConstellationBridge;

            catalog.Configure(
                new[]
                {
                    Stage(
                        P11StageId.StarPostOffice31,
                        "분실물 분류소",
                        RoomRegion.StarPostOffice,
                        P6StageSlot.X1,
                        P6StageArchetype.Circuit,
                        P11TraversalAxis.Circuit,
                        post31,
                        P11StageGuarantees.Landmark
                        | P11StageGuarantees.LocalEvent,
                        P11BossKind.None,
                        "같은 그림의 소포를 분류하며 고정된 출구 흐름을 따른다."),
                    Stage(
                        P11StageId.StarPostOffice32,
                        "반송 복도",
                        RoomRegion.StarPostOffice,
                        P6StageSlot.X2,
                        P6StageArchetype.Circuit,
                        P11TraversalAxis.Circuit,
                        post32,
                        P11StageGuarantees.Landmark
                        | P11StageGuarantees.ClosedLoop
                        | P11StageGuarantees.MomoShop
                        | P11StageGuarantees.HomecomingStatue
                        | P11StageGuarantees.StarArchive,
                        P11BossKind.None,
                        "반송 도장과 우편관을 이용해 순환 복도를 욕심내어 탐색한다."),
                    Stage(
                        P11StageId.StarPostOffice33,
                        "중앙 반송기",
                        RoomRegion.StarPostOffice,
                        P6StageSlot.X3,
                        P6StageArchetype.Traverse,
                        P11TraversalAxis.Horizontal,
                        post31
                        | post32
                        | P11StageMechanics.BossArena,
                        P11StageGuarantees.SupplyCorridor
                        | P11StageGuarantees.Boss
                        | P11StageGuarantees.LetterStateDisplay
                        | P11StageGuarantees.DawnMapStateDisplay,
                        P11BossKind.Popo,
                        "유도 소포와 반송 도장을 포포에게 되돌린다."),
                    Stage(
                        P11StageId.SunriseGarden41,
                        "어린 삼족오",
                        RoomRegion.SunriseGarden,
                        P6StageSlot.X1,
                        P6StageArchetype.Traverse,
                        P11TraversalAxis.Vertical,
                        gardenCore
                        | P11StageMechanics.ShadowSeed,
                        P11StageGuarantees.Landmark
                        | P11StageGuarantees.LocalEvent
                        | P11StageGuarantees.LetterStateDisplay,
                        P11BossKind.None,
                        "밝은 출구 길에서 회전 해살과 그림자 씨앗을 읽는다."),
                    Stage(
                        P11StageId.SunriseGarden42,
                        "꺼진 태양 둥지",
                        RoomRegion.SunriseGarden,
                        P6StageSlot.X2,
                        P6StageArchetype.Ascent,
                        P11TraversalAxis.Vertical,
                        gardenCore
                        | P11StageMechanics.GrowingVine
                        | P11StageMechanics.OverheatedPlatform,
                        P11StageGuarantees.Landmark
                        | P11StageGuarantees.ClosedLoop
                        | P11StageGuarantees.MomoShop
                        | P11StageGuarantees.HomecomingStatue
                        | P11StageGuarantees.StarArchive
                        | P11StageGuarantees.LocalEvent,
                        P11BossKind.None,
                        "물을 주고 봉인 고리를 풀어 삼족오 둥지를 복구한다."),
                    Stage(
                        P11StageId.SunriseGarden43,
                        "멈춘 하루",
                        RoomRegion.SunriseGarden,
                        P6StageSlot.X3,
                        P6StageArchetype.Ascent,
                        P11TraversalAxis.Vertical,
                        gardenCore
                        | P11StageMechanics.ShadowSeed
                        | P11StageMechanics.GrowingVine
                        | P11StageMechanics.OverheatedPlatform
                        | P11StageMechanics.BossArena,
                        P11StageGuarantees.SupplyCorridor
                        | P11StageGuarantees.Boss
                        | P11StageGuarantees.SunEmberStateDisplay,
                        P11BossKind.SunFlower,
                        "빛과 그림자를 교차시켜 해꽃의 별매듭을 드러낸다."),
                    Stage(
                        P11StageId.PolarisObservatory51,
                        "귀환 기록실",
                        RoomRegion.PolarisObservatory,
                        P6StageSlot.X1,
                        P6StageArchetype.Circuit,
                        P11TraversalAxis.Circuit,
                        observatory51,
                        P11StageGuarantees.Landmark
                        | P11StageGuarantees.LocalEvent
                        | P11StageGuarantees.MaruStoryTableau,
                        P11BossKind.None,
                        "귀환장과 불변 별블록 사이에서 마루의 귀환 기록을 본다."),
                    Stage(
                        P11StageId.PolarisObservatory52,
                        "북극성 승강로",
                        RoomRegion.PolarisObservatory,
                        P6StageSlot.X2,
                        P6StageArchetype.Ascent,
                        P11TraversalAxis.Vertical,
                        observatory52,
                        P11StageGuarantees.Landmark
                        | P11StageGuarantees.ClosedLoop
                        | P11StageGuarantees.MomoShop
                        | P11StageGuarantees.HomecomingStatue
                        | P11StageGuarantees.StarArchive
                        | P11StageGuarantees.LetterStateDisplay
                        | P11StageGuarantees.SunEmberStateDisplay
                        | P11StageGuarantees.DawnMapStateDisplay,
                        P11BossKind.None,
                        "궤도 발판과 별자리 다리를 따라 북극성으로 상승한다."),
                    Stage(
                        P11StageId.PolarisObservatory53,
                        "마루의 별목걸이",
                        RoomRegion.PolarisObservatory,
                        P6StageSlot.X3,
                        P6StageArchetype.Ascent,
                        P11TraversalAxis.Vertical,
                        observatory51
                        | observatory52
                        | P11StageMechanics.BossArena,
                        P11StageGuarantees.SupplyCorridor
                        | P11StageGuarantees.Boss
                        | P11StageGuarantees.MaruStoryTableau,
                        P11BossKind.Maru,
                        "별매듭·명령 기둥·기억 방울 중 한 방식으로 마루를 끝낸다.")
                });
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static P11StageDefinition Stage(
            P11StageId id,
            string name,
            RoomRegion region,
            P6StageSlot slot,
            P6StageArchetype archetype,
            P11TraversalAxis axis,
            P11StageMechanics mechanics,
            P11StageGuarantees guarantees,
            P11BossKind boss,
            string sentence)
        {
            var definition = new P11StageDefinition();
            definition.Configure(
                id,
                name,
                region,
                slot,
                archetype,
                axis,
                mechanics,
                guarantees,
                boss,
                sentence,
                slot == P6StageSlot.X1 ? 2 : 3,
                slot == P6StageSlot.X1 ? 3 : 5,
                true,
                true);
            return definition;
        }

        private static P11StageEnvironment2D CreateStageEnvironment(
            Transform parent,
            P11StageDefinition definition,
            BuildAssets assets,
            Collider2D playerCollider,
            P11StoryState2D story)
        {
            StageLayout layout = LayoutFor(definition);
            GameObject root = CreateChild(
                parent,
                $"Environment_{definition.StageId}_{definition.DisplayName}");
            root.SetActive(true);
            Transform[] motifs = CreateThemePresentation(
                root.transform,
                definition,
                layout,
                assets);

            GameObject gridObject = CreateChild(root.transform, "Grid");
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
            FillStageGeometry(
                definition,
                layout,
                terrain,
                oneWay,
                assets);
            TintTilemaps(
                definition.Region,
                terrain.GetComponent<TilemapRenderer>(),
                oneWay.GetComponent<TilemapRenderer>());
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
                layout.Size);

            Transform entry = CreateAnchor(
                root.transform,
                "StageEntry",
                CellWorld(layout.Entry));
            Transform exit = CreateAnchor(
                root.transform,
                "StageExit",
                CellWorld(layout.Exit));
            Transform cameraAnchor = CreateAnchor(
                root.transform,
                "CameraAnchor",
                new Vector2(
                    layout.Size.x * 0.5f,
                    layout.Size.y * 0.5f));
            Transform maruEntry = CreateAnchor(
                root.transform,
                "MaruEntryAnchor",
                (Vector2)exit.position + Vector2.left * 1.5f);
            CreateSpritePart(
                exit,
                "ExitStar",
                assets.Star,
                Vector2.zero,
                Vector2.one * 0.85f,
                new Color(0.64f, 1f, 0.94f, 1f),
                70);

            GameObject systems = CreateChild(
                root.transform,
                "StageSystems");
            Transform spawned = CreateChild(
                systems.transform,
                "SpawnedConsumables").transform;
            Transform ropesRoot = CreateChild(
                systems.transform,
                "InstalledRopes").transform;
            TileMutationService mutation =
                systems.AddComponent<TileMutationService>();
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
                ropesRoot,
                RopePlacementSolver.DefaultMaximumLength,
                assets.Square);
            WaterInteractionRegistry2D water =
                systems.AddComponent<WaterInteractionRegistry2D>();
            PestleInteractionRegistry2D pestle =
                systems.AddComponent<PestleInteractionRegistry2D>();

            P11MapStageHarness2D mapHarness =
                BuildStageHarness(
                    root.transform,
                    systems.transform,
                    definition,
                    layout,
                    entry,
                    exit,
                    maruEntry,
                    motifs[1],
                    assets,
                    out P11MapRoom2D[] authoredRooms,
                    out P8MaruRoomGraph2D roomGraph);
            GridPos entryCell = world.WorldToCell(entry.position);
            GridPos exitCell = world.WorldToCell(exit.position);
            mutation.Configure(
                world,
                terrain,
                decoration,
                terrainCollider,
                composite,
                playerCollider,
                assets.TileDefinitions,
                entryCell,
                exitCell,
                ProtectedExitCells(
                    new Vector2Int(exitCell.X, exitCell.Y)));
            BakeHarnessTerrain(
                terrain,
                mapHarness,
                entry,
                exit,
                layout,
                assets);
            terrain.CompressBounds();
            terrain.RefreshAllTiles();
            terrainCollider.ProcessTilemapChanges();
            composite.GenerateGeometry();
            P8ReturnPile2D returnPile =
                BuildReturnPile(
                    systems.transform,
                    entry.position + new Vector3(1.2f, 0.4f, 0f),
                    assets);

            P11StageEnvironment2D environment =
                root.AddComponent<P11StageEnvironment2D>();
            environment.Configure(
                definition.StageId,
                root,
                world,
                entry,
                exit,
                cameraAnchor,
                maruEntry,
                mutation,
                explosions,
                ropes,
                water,
                pestle,
                spawned,
                ropesRoot,
                roomGraph,
                returnPile,
                mapHarness,
                motifs);
            CreateRegionalMechanics(
                environment,
                definition,
                layout,
                assets,
                story);
            Physics2D.SyncTransforms();
            return environment;
        }

        private static P11MapStageHarness2D BuildStageHarness(
            Transform stageRoot,
            Transform systemsRoot,
            P11StageDefinition definition,
            StageLayout layout,
            Transform entry,
            Transform exit,
            Transform maruEntry,
            Transform landmark,
            BuildAssets assets,
            out P11MapRoom2D[] authoredRooms,
            out P8MaruRoomGraph2D roomGraph)
        {
            Transform harnessRoot = CreateChild(
                stageRoot,
                "MapHarness_VariableRooms").transform;
            List<RoomAuthoring> specifications =
                CreateRoomSpecifications(definition, layout);
            authoredRooms =
                new P11MapRoom2D[specifications.Count];
            for (int index = 0;
                 index < specifications.Count;
                 index++)
            {
                authoredRooms[index] = CreateAuthoredRoom(
                    harnessRoot,
                    specifications[index],
                    index,
                    assets);
            }
            AlignStageAnchorsToHarnessRooms(
                authoredRooms,
                entry,
                exit,
                maruEntry,
                definition.Region);
            ApplyMeasuredMaruEscapeReadability(
                authoredRooms,
                maruEntry.position);
            CreatePhysicalRoomConnections(
                harnessRoot,
                authoredRooms,
                assets);
            roomGraph = BuildMaruRoomGraph(
                systemsRoot,
                authoredRooms);
            bool physicalMainRouteReady =
                HasCompletePhysicalMainRoute(
                    harnessRoot,
                    authoredRooms);
            P11MapRoom2D startRoom = authoredRooms
                .FirstOrDefault(room =>
                    room != null
                    && (room.Roles & P11MapRoomRole.Start) != 0);
            P11MapRoom2D exitRoom = authoredRooms
                .Where(room =>
                    room != null
                    && (room.Roles & P11MapRoomRole.Exit) != 0)
                .OrderByDescending(room => room.MainRouteOrder)
                .FirstOrDefault();
            bool startToExitReachable =
                physicalMainRouteReady
                && IsReachableBetweenRooms(
                    roomGraph,
                    startRoom,
                    exitRoom);
            Vector2 maruSafeTarget = exit.position;
            if (exitRoom != null
                && exitRoom.TryGetZone(
                    P11MapRoomZone.ExitSafe,
                    out P11MapRoomZoneDefinition exitSafe)
                && exitSafe.Anchor != null)
            {
                maruSafeTarget = exitSafe.Anchor.position;
            }

            bool maruEscapeReachable =
                physicalMainRouteReady
                && IsReachableOnRoomGraph(
                    roomGraph,
                    maruEntry.position,
                    maruSafeTarget);

            List<P11MapDirectionCue2D> cues =
                new List<P11MapDirectionCue2D>();
            cues.Add(
                CreateDirectionCue(
                    harnessRoot,
                    "MainExit_StardustFlow",
                    P11MapCueKind.MainExit,
                    P11MapRouteKind.Main,
                    exit,
                    assets.Star,
                    (Vector2)entry.position + Vector2.up * 2.2f,
                    new Color(0.54f, 1f, 0.94f, 1f),
                    true));
            Vector2 exitDirection =
                ((Vector2)exit.position - (Vector2)entry.position)
                    .normalized;
            P11MapDirectionCue2D arrowCue =
                CreateDirectionCue(
                    harnessRoot,
                    "ExitEdge_Arrow",
                    P11MapCueKind.ExitEdge,
                    P11MapRouteKind.Main,
                    exit,
                    assets.Arrow,
                    (Vector2)entry.position
                    + exitDirection * 2.8f
                    + Vector2.up * 1.1f,
                    new Color(0.80f, 1f, 0.76f, 1f),
                    true);
            arrowCue.transform.right = exitDirection;
            cues.Add(arrowCue);
            CreateStardustFlowTrail(
                harnessRoot,
                entry.position,
                exitDirection,
                assets.Star);
            cues.Add(
                CreateDirectionCue(
                    harnessRoot,
                    "Landmark_Anchor",
                    P11MapCueKind.Landmark,
                    P11MapRouteKind.Main,
                    landmark,
                    assets.Star,
                    (Vector2)entry.position + Vector2.up * 4f,
                    new Color(0.72f, 0.86f, 1f, 1f),
                    true));

            AddOptionalCueForRole(
                harnessRoot,
                authoredRooms,
                P11MapRoomRole.Choice
                | P11MapRoomRole.Treasure,
                P11MapCueKind.OptionalReward,
                "OptionalReward_GoldSparkle",
                assets.Star,
                new Color(1f, 0.76f, 0.24f, 1f),
                cues);
            AddOptionalCueForRole(
                harnessRoot,
                authoredRooms,
                P11MapRoomRole.LocalEvent
                | P11MapRoomRole.RelicRoom,
                P11MapCueKind.Folklore,
                "OptionalFolklore_Symbol",
                assets.Bird,
                new Color(1f, 0.67f, 0.82f, 1f),
                cues);
            AddOptionalCueForRole(
                harnessRoot,
                authoredRooms,
                P11MapRoomRole.MaruRoom,
                P11MapCueKind.Homecoming,
                "OptionalHomecoming_Bell",
                assets.Star,
                new Color(0.76f, 0.70f, 1f, 1f),
                cues);
            AddOptionalCueForRole(
                harnessRoot,
                authoredRooms,
                P11MapRoomRole.RecordRoom,
                P11MapCueKind.Archive,
                "OptionalArchive_Light",
                assets.Star,
                new Color(0.48f, 1f, 0.92f, 1f),
                cues);

            GameObject harnessObject = CreateChild(
                harnessRoot,
                "StageHarness");
            P11MapStageHarness2D harness =
                harnessObject.AddComponent<P11MapStageHarness2D>();
            harness.Configure(
                definition.StageId.ToString(),
                MapRegion(definition.Region),
                MapSlot(definition.StageSlot),
                MapArchetype(definition),
                definition.Region == RoomRegion.StarPostOffice
                    ? P11MapMainDirection.Right
                    : P11MapMainDirection.Up,
                landmark,
                authoredRooms,
                cues.ToArray(),
                definition.StageSlot == P6StageSlot.X2 ? 2 : 0,
                definition.Region == RoomRegion.PolarisObservatory
                    && definition.StageSlot == P6StageSlot.X2
                        ? 1
                        : definition.StageSlot == P6StageSlot.X2
                            ? 2
                            : 0,
                startToExitReachable,
                maruEscapeReachable,
                P11MapStageHarness2D.EstimateMaruEscapeSeconds(
                    authoredRooms,
                    P8MaruPursuer2D.DefaultMoveSpeed),
                true,
                true,
                1f,
                true);
            P11MapValidationReport report = harness.ValidateNow();
            if (!report.Passed)
            {
                throw new InvalidOperationException(
                    $"Map harness failed for {definition.StageId}:"
                    + Environment.NewLine
                    + report);
            }

            if (report.WarningCount > 0)
            {
                Debug.LogWarning(
                    $"[StarNight P11] {definition.StageId} map harness "
                    + "passed with measured-contract warnings:"
                    + Environment.NewLine
                    + report);
            }

            return harness;
        }

        private static void ApplyMeasuredMaruEscapeReadability(
            IReadOnlyList<P11MapRoom2D> rooms,
            Vector2 maruEntryPosition)
        {
            for (int index = 0; index < rooms.Count; index++)
            {
                P11MapRoom2D room = rooms[index];
                if (room == null)
                {
                    continue;
                }

                room.ApplyMaruEscapeReadability(
                    P11MapStageHarness2D
                        .IsRoomReadableDuringMaruEscape(
                            room,
                            maruEntryPosition));
            }
        }

        private static void AlignStageAnchorsToHarnessRooms(
            IReadOnlyList<P11MapRoom2D> rooms,
            Transform entry,
            Transform exit,
            Transform maruEntry,
            RoomRegion region)
        {
            P11MapRoom2D startRoom = rooms
                .Where(room =>
                    room != null
                    && (room.Roles & P11MapRoomRole.Start) != 0)
                .OrderBy(room => room.MainRouteOrder)
                .FirstOrDefault();
            P11MapRoom2D exitRoom = rooms
                .Where(room =>
                    room != null
                    && (room.Roles & P11MapRoomRole.Exit) != 0)
                .OrderByDescending(room => room.MainRouteOrder)
                .FirstOrDefault();

            if (startRoom != null
                && startRoom.TryGetZone(
                    P11MapRoomZone.EntrySafe,
                    out P11MapRoomZoneDefinition entryZone)
                && entryZone.Anchor != null)
            {
                entry.position = entryZone.Anchor.position;
            }

            if (exitRoom != null
                && exitRoom.TryGetZone(
                    P11MapRoomZone.ExitSafe,
                    out P11MapRoomZoneDefinition exitZone)
                && exitZone.Anchor != null)
            {
                exit.position = exitZone.Anchor.position;
            }

            Vector2 reverseDirection =
                region == RoomRegion.StarPostOffice
                    ? Vector2.left
                    : Vector2.down;
            maruEntry.position =
                (Vector2)exit.position + reverseDirection * 1.5f;
        }

        private static List<RoomAuthoring>
            CreateRoomSpecifications(
                P11StageDefinition definition,
                StageLayout layout)
        {
            int mainNonBoss;
            int optionalNonBoss;
            switch (definition.StageSlot)
            {
                case P6StageSlot.X1:
                    mainNonBoss = 7;
                    optionalNonBoss = 2;
                    break;
                case P6StageSlot.X2:
                    mainNonBoss = 9;
                    optionalNonBoss = 4;
                    break;
                default:
                    mainNonBoss = 6;
                    optionalNonBoss = 1;
                    break;
            }

            var rooms = new List<RoomAuthoring>(
                mainNonBoss
                + optionalNonBoss
                + (definition.IsBossStage ? 1 : 0));
            for (int index = 0;
                 index < mainNonBoss;
                 index++)
            {
                P11MapRoomRole role = MainRole(
                    definition.StageSlot,
                    index,
                    mainNonBoss);
                rooms.Add(
                    new RoomAuthoring(
                        $"Main_{index:00}_{role}",
                        CoreSentence(role),
                        MacroSize(
                            definition.Region,
                            definition.StageSlot,
                            index,
                            false),
                        role,
                        RoutePoint(
                            definition,
                            layout,
                            index,
                            mainNonBoss),
                        true,
                        index,
                        index < 2
                        && definition.StageSlot == P6StageSlot.X1
                            ? 1
                            : 2,
                        definition.StageSlot != P6StageSlot.X3
                        && (role & P11MapRoomRole.Teach) != 0));
            }

            for (int index = 0;
                 index < optionalNonBoss;
                 index++)
            {
                P11MapRoomRole role = OptionalRole(
                    definition.StageSlot,
                    index);
                rooms.Add(
                    new RoomAuthoring(
                        $"Optional_{index:00}_{role}",
                        CoreSentence(role),
                        MacroSize(
                            definition.Region,
                            definition.StageSlot,
                            index,
                            true),
                        role,
                        OptionalPoint(
                            definition,
                            layout,
                            index),
                        false,
                        0,
                        role == P11MapRoomRole.MercyRoom ? 0 : 1,
                        false));
            }

            if (definition.IsBossStage)
            {
                Vector2 bossCenter =
                    BossArenaCenter(definition, layout);
                rooms.Add(
                    new RoomAuthoring(
                        $"Boss_{definition.Boss}",
                        CoreSentence(
                            P11MapRoomRole.Boss
                            | P11MapRoomRole.Exit),
                        new Vector2Int(2, 2),
                        P11MapRoomRole.Boss
                        | P11MapRoomRole.Exit,
                        bossCenter,
                        true,
                        mainNonBoss,
                        2,
                        false));
            }

            return rooms;
        }

        private static P11MapRoom2D CreateAuthoredRoom(
            Transform parent,
            RoomAuthoring authoring,
            int index,
            BuildAssets assets)
        {
            GameObject roomObject = CreateChild(
                parent,
                $"Room_{index:00}_{authoring.Id}");
            roomObject.transform.position = authoring.Center;
            float halfWidth = authoring.MacroSize.x * 6f;
            float halfHeight = authoring.MacroSize.y * 4f;
            float safeFloorY = -halfHeight + 1.5f;
            var zones = new P11MapRoomZoneDefinition[5];
            P11MapRoomZone[] zoneKinds =
            {
                P11MapRoomZone.EntrySafe,
                P11MapRoomZone.Telegraph,
                P11MapRoomZone.Action,
                P11MapRoomZone.Recovery,
                P11MapRoomZone.ExitSafe
            };
            for (int zoneIndex = 0;
                 zoneIndex < zoneKinds.Length;
                 zoneIndex++)
            {
                float normalized = zoneIndex / 4f;
                Transform anchor = CreateAnchor(
                    roomObject.transform,
                    $"Zone_{zoneKinds[zoneIndex]}",
                    new Vector2(
                        Mathf.Lerp(-2f, 2f, normalized),
                        zoneKinds[zoneIndex]
                            == P11MapRoomZone.Telegraph
                            ? safeFloorY + 0.5f
                            : safeFloorY),
                    true);
                zones[zoneIndex] =
                    new P11MapRoomZoneDefinition();
                int safeCells =
                    zoneKinds[zoneIndex]
                        == P11MapRoomZone.EntrySafe
                    || zoneKinds[zoneIndex]
                        == P11MapRoomZone.ExitSafe
                        ? 2
                        : zoneKinds[zoneIndex]
                            == P11MapRoomZone.Recovery
                            ? 1
                            : 0;
                zones[zoneIndex].Configure(
                    zoneKinds[zoneIndex],
                    anchor,
                    safeCells,
                    true);
            }

            List<P11MapSocketDefinition> sockets =
                new List<P11MapSocketDefinition>();
            if (authoring.OnMainRoute)
            {
                bool oneSocket =
                    (authoring.Roles
                        & (P11MapRoomRole.Start
                           | P11MapRoomRole.Exit
                           | P11MapRoomRole.Boss))
                    != 0;
                sockets.Add(
                    CreateSocket(
                        roomObject.transform,
                        "Main_A",
                        new Vector2(-halfWidth + 0.5f, 0f),
                        P11MapRouteKind.Main,
                        P11MapSocketTraversal.Walk,
                        true,
                        false));
                if (!oneSocket)
                {
                    sockets.Add(
                        CreateSocket(
                            roomObject.transform,
                            "Main_B",
                            new Vector2(halfWidth - 0.5f, 0f),
                            P11MapRouteKind.Main,
                            P11MapSocketTraversal.JumpSafe,
                            true,
                            false));
                }

                if ((authoring.Roles
                        & P11MapRoomRole.Choice) != 0)
                {
                    sockets.Add(
                        CreateSocket(
                            roomObject.transform,
                            "Optional_Return",
                            new Vector2(0f, halfHeight - 0.5f),
                            P11MapRouteKind.Optional,
                            P11MapSocketTraversal.HookOptional,
                            true,
                            true));
                }
            }
            else
            {
                sockets.Add(
                    CreateSocket(
                        roomObject.transform,
                        "Optional_Return",
                        new Vector2(-halfWidth + 0.5f, 0f),
                        P11MapRouteKind.Optional,
                        P11MapSocketTraversal.ReturnableDrop,
                        true,
                        false));
            }

            P11MapRoom2D room =
                roomObject.AddComponent<P11MapRoom2D>();
            room.Configure(
                authoring.Id,
                authoring.CoreSentence,
                authoring.MacroSize,
                authoring.MechanicsCount,
                authoring.Roles,
                zones,
                sockets.ToArray(),
                authoring.OnMainRoute,
                authoring.MainOrder,
                authoring.AddsNewMechanic,
                false);
            P11MapValidationReport report = room.ValidateNow();
            if (!report.Passed)
            {
                throw new InvalidOperationException(
                    $"Room metadata failed for {authoring.Id}:"
                    + Environment.NewLine
                    + report);
            }

            CreatePhysicalRoomShell(
                roomObject.transform,
                authoring,
                assets);
            return room;
        }

        private static P11MapSocketDefinition CreateSocket(
            Transform parent,
            string id,
            Vector2 localPosition,
            P11MapRouteKind route,
            P11MapSocketTraversal traversal,
            bool returnable,
            bool toolRequired)
        {
            Transform anchor = CreateAnchor(
                parent,
                $"Socket_{id}",
                localPosition,
                true);
            var socket = new P11MapSocketDefinition();
            socket.Configure(
                id,
                anchor,
                route,
                traversal,
                returnable,
                toolRequired);
            return socket;
        }

        private static void CreatePhysicalRoomShell(
            Transform room,
            RoomAuthoring authoring,
            BuildAssets assets)
        {
            float width = authoring.MacroSize.x * 12f;
            float height = authoring.MacroSize.y * 8f;
            GameObject shell = CreateChild(
                room,
                $"PhysicalRoom_{authoring.MacroSize.x}x"
                + $"{authoring.MacroSize.y}");
            Color color = authoring.OnMainRoute
                ? new Color(0.28f, 0.56f, 0.72f, 0.32f)
                : new Color(0.82f, 0.58f, 0.24f, 0.28f);
            CreateSpritePart(
                shell.transform,
                "FloorVisual",
                assets.Square,
                new Vector2(0f, -height * 0.5f + 0.25f),
                new Vector2(width, 0.35f),
                color,
                8);
            CreateSpritePart(
                shell.transform,
                "LeftBoundary",
                assets.Square,
                new Vector2(
                    -width * 0.5f + 0.2f,
                    -height * 0.18f),
                new Vector2(0.22f, height * 0.62f),
                color,
                7);
            CreateSpritePart(
                shell.transform,
                "RightBoundary",
                assets.Square,
                new Vector2(
                    width * 0.5f - 0.2f,
                    -height * 0.18f),
                new Vector2(0.22f, height * 0.62f),
                color,
                7);
            GameObject floorCollider = CreateChild(
                shell.transform,
                "PhysicalFloorCollider");
            floorCollider.transform.localPosition =
                new Vector3(0f, -height * 0.5f + 0.25f, 0f);
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                floorCollider.layer = groundLayer;
            }

            BoxCollider2D collider =
                floorCollider.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(width, 0.5f);
        }

        private static void CreatePhysicalRoomConnections(
            Transform parent,
            IReadOnlyList<P11MapRoom2D> rooms,
            BuildAssets assets)
        {
            P11MapRoom2D[] main = rooms
                .Where(room => room != null && room.OnMainRoute)
                .OrderBy(room => room.MainRouteOrder)
                .ToArray();
            GameObject root = CreateChild(
                parent,
                "PhysicalRoomConnections_ToolFreeMain");
            for (int index = 0; index + 1 < main.Length; index++)
            {
                CreatePhysicalConnection(
                    root.transform,
                    main[index],
                    main[index + 1],
                    $"Main_{index:00}_{index + 1:00}",
                    assets,
                    true);
            }

            P11MapRoom2D[] optional = rooms
                .Where(room => room != null && !room.OnMainRoute)
                .ToArray();
            for (int index = 0; index < optional.Length; index++)
            {
                P11MapRoom2D nearest = main
                    .OrderBy(room => Vector2.SqrMagnitude(
                        (Vector2)room.transform.position
                        - (Vector2)optional[index]
                            .transform.position))
                    .FirstOrDefault();
                if (nearest == null)
                {
                    continue;
                }

                CreatePhysicalConnection(
                    root.transform,
                    nearest,
                    optional[index],
                    $"OptionalReturn_{index:00}",
                    assets,
                    false);
            }
        }

        private static void CreatePhysicalConnection(
            Transform parent,
            P11MapRoom2D startRoom,
            P11MapRoom2D endRoom,
            string name,
            BuildAssets assets,
            bool toolFree)
        {
            Vector2 startCenter =
                startRoom.transform.position;
            Vector2 endCenter =
                endRoom.transform.position;
            CreatePhysicalConnectionBetweenPoints(
                parent,
                RoomConnectionStandingPoint(
                    startRoom,
                    endCenter),
                RoomConnectionStandingPoint(
                    endRoom,
                    startCenter),
                name,
                assets,
                toolFree);
        }

        private static void CreatePhysicalConnectionBetweenPoints(
            Transform parent,
            Vector2 start,
            Vector2 end,
            string name,
            BuildAssets assets,
            bool toolFree)
        {
            GameObject connection = CreateChild(
                parent,
                name);
            Vector2 delta = end - start;
            int steps = Mathf.Max(
                2,
                Mathf.CeilToInt(delta.magnitude / 2.2f));
            for (int index = 1; index < steps; index++)
            {
                float t = index / (float)steps;
                Vector2 point = Vector2.Lerp(start, end, t);
                Vector2 platformPoint =
                    point + Vector2.down * 0.65f;
                GameObject platform = CreateSpritePart(
                    connection.transform,
                    $"Step_{index:00}",
                    assets.Square,
                    platformPoint,
                    toolFree
                        ? new Vector2(2.4f, 0.20f)
                        : new Vector2(0.38f, 0.38f),
                    toolFree
                        ? new Color(0.50f, 0.84f, 0.94f, 0.52f)
                        : new Color(1f, 0.72f, 0.28f, 0.64f),
                    9);
                GameObject colliderObject = CreateChild(
                    connection.transform,
                    $"StepCollider_{index:00}");
                colliderObject.transform.position = platformPoint;
                int groundLayer = LayerMask.NameToLayer("Ground");
                if (groundLayer >= 0)
                {
                    colliderObject.layer = groundLayer;
                }

                BoxCollider2D collider =
                    colliderObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(
                    toolFree ? 2.4f : 1.8f,
                    0.30f);
            }
        }

        private static Vector2 RoomFloorStandingPoint(
            P11MapRoom2D room)
        {
            Vector2 center = room.transform.position;
            return center
                   + Vector2.down
                   * (room.MacroSize.y * 4f - 1.5f);
        }

        private static Vector2 RoomConnectionStandingPoint(
            P11MapRoom2D room,
            Vector2 toward)
        {
            Vector2 center = room.transform.position;
            Vector2 point = RoomFloorStandingPoint(room);
            float horizontalDirection = toward.x - center.x;
            if (Mathf.Abs(horizontalDirection) < 0.01f)
            {
                horizontalDirection =
                    toward.y >= center.y ? 1f : -1f;
            }

            float halfWidth = room.MacroSize.x * 6f;
            point.x = center.x
                      + Mathf.Sign(horizontalDirection)
                      * (halfWidth - 1f);
            return point;
        }

        private static void BakeHarnessTerrain(
            Tilemap terrain,
            P11MapStageHarness2D harness,
            Transform entry,
            Transform exit,
            StageLayout layout,
            BuildAssets assets)
        {
            for (int index = 0;
                 index < harness.Rooms.Count;
                 index++)
            {
                P11MapRoom2D room = harness.Rooms[index];
                if (room == null)
                {
                    continue;
                }

                Vector2 center = room.transform.position;
                int width = room.MacroSize.x * 12;
                int floorY = Mathf.FloorToInt(
                    center.y - room.MacroSize.y * 4f);
                int floorX = Mathf.FloorToInt(
                    center.x - room.MacroSize.x * 6f);
                SetRectClipped(
                    terrain,
                    room.OnMainRoute
                        ? assets.ReinforcedTile
                        : assets.SoftSoilTile,
                    floorX,
                    floorY,
                    width,
                    1,
                    layout.Size);
            }

            P11MapRoom2D[] main = harness.Rooms
                .Where(room => room != null && room.OnMainRoute)
                .OrderBy(room => room.MainRouteOrder)
                .ToArray();
            for (int index = 0; index + 1 < main.Length; index++)
            {
                Vector2 startCenter =
                    main[index].transform.position;
                Vector2 endCenter =
                    main[index + 1].transform.position;
                BakeConnectionTerrain(
                    terrain,
                    RoomConnectionStandingPoint(
                        main[index],
                        endCenter),
                    RoomConnectionStandingPoint(
                        main[index + 1],
                        startCenter),
                    assets.ReinforcedTile,
                    layout.Size);
            }

            P11MapRoom2D[] optional = harness.Rooms
                .Where(room => room != null && !room.OnMainRoute)
                .ToArray();
            for (int index = 0; index < optional.Length; index++)
            {
                P11MapRoom2D nearest = main
                    .OrderBy(room => Vector2.SqrMagnitude(
                        (Vector2)room.transform.position
                        - (Vector2)optional[index]
                            .transform.position))
                    .FirstOrDefault();
                if (nearest == null)
                {
                    continue;
                }

                BakeConnectionTerrain(
                    terrain,
                    RoomConnectionStandingPoint(
                        nearest,
                        optional[index].transform.position),
                    RoomConnectionStandingPoint(
                        optional[index],
                        nearest.transform.position),
                    assets.SoftSoilTile,
                    layout.Size);
            }

            BakeSupportAtAnchor(
                terrain,
                entry.position,
                assets.ReinforcedTile,
                layout.Size);
            BakeSupportAtAnchor(
                terrain,
                exit.position,
                assets.ReinforcedTile,
                layout.Size);
            ReserveHarnessSafeZones(
                terrain,
                harness,
                assets.ReinforcedTile,
                layout.Size);
        }

        private static void ReserveHarnessSafeZones(
            Tilemap terrain,
            P11MapStageHarness2D harness,
            TileBase supportTile,
            Vector2Int size)
        {
            P11MapRoomZone[] kinds =
            {
                P11MapRoomZone.EntrySafe,
                P11MapRoomZone.Recovery,
                P11MapRoomZone.ExitSafe
            };
            for (int roomIndex = 0;
                 roomIndex < harness.Rooms.Count;
                 roomIndex++)
            {
                P11MapRoom2D room = harness.Rooms[roomIndex];
                if (room == null)
                {
                    continue;
                }

                for (int kindIndex = 0;
                     kindIndex < kinds.Length;
                     kindIndex++)
                {
                    P11MapRoomZone kind = kinds[kindIndex];
                    if (!room.TryGetZone(
                            kind,
                            out P11MapRoomZoneDefinition zone)
                        || zone.Anchor == null)
                    {
                        continue;
                    }

                    GridPos anchor =
                        new GridPos(
                            Mathf.FloorToInt(zone.Anchor.position.x),
                            Mathf.FloorToInt(zone.Anchor.position.y));
                    int horizontal =
                        kind == P11MapRoomZone.EntrySafe
                            ? -1
                            : kind == P11MapRoomZone.ExitSafe
                                ? 1
                                : 0;
                    int safeCellCount =
                        Mathf.Max(1, zone.SafeCellCount);
                    for (int safeIndex = 0;
                         safeIndex < safeCellCount;
                         safeIndex++)
                    {
                        int x = anchor.X + horizontal * safeIndex;
                        if (x < 0
                            || x >= size.x
                            || anchor.Y < 1
                            || anchor.Y >= size.y)
                        {
                            continue;
                        }

                        terrain.SetTile(
                            new Vector3Int(x, anchor.Y, 0),
                            null);
                        if (anchor.Y + 1 < size.y)
                        {
                            terrain.SetTile(
                                new Vector3Int(x, anchor.Y + 1, 0),
                                null);
                        }

                        terrain.SetTile(
                            new Vector3Int(x, anchor.Y - 1, 0),
                            supportTile);
                    }
                }
            }
        }

        private static void BakeConnectionTerrain(
            Tilemap terrain,
            Vector2 start,
            Vector2 end,
            TileBase tile,
            Vector2Int size)
        {
            float distance = Vector2.Distance(start, end);
            int steps = Mathf.Max(
                1,
                Mathf.CeilToInt(distance / 2f));
            for (int index = 0; index <= steps; index++)
            {
                Vector2 point = Vector2.Lerp(
                    start,
                    end,
                    index / (float)steps);
                int supportX = Mathf.FloorToInt(point.x) - 1;
                int supportY = Mathf.FloorToInt(point.y) - 1;
                SetRectClipped(
                    terrain,
                    tile,
                    supportX,
                    supportY,
                    3,
                    1,
                    size);
            }
        }

        private static void BakeSupportAtAnchor(
            Tilemap terrain,
            Vector2 anchor,
            TileBase tile,
            Vector2Int size)
        {
            SetRectClipped(
                terrain,
                tile,
                Mathf.FloorToInt(anchor.x) - 1,
                Mathf.FloorToInt(anchor.y) - 1,
                3,
                1,
                size);
        }

        private static void SetRectClipped(
            Tilemap tilemap,
            TileBase tile,
            int x,
            int y,
            int width,
            int height,
            Vector2Int bounds)
        {
            int minimumX = Mathf.Clamp(x, 0, bounds.x);
            int minimumY = Mathf.Clamp(y, 0, bounds.y);
            int maximumX = Mathf.Clamp(x + width, 0, bounds.x);
            int maximumY = Mathf.Clamp(y + height, 0, bounds.y);
            if (maximumX <= minimumX || maximumY <= minimumY)
            {
                return;
            }

            SetRect(
                tilemap,
                tile,
                minimumX,
                minimumY,
                maximumX - minimumX,
                maximumY - minimumY);
        }

        private static void CreateStardustFlowTrail(
            Transform parent,
            Vector2 entry,
            Vector2 direction,
            Sprite star)
        {
            GameObject flow = CreateChild(
                parent,
                "MainExit_StardustFlowTrail_4");
            for (int index = 0; index < 4; index++)
            {
                float normalized = (index + 1) / 4f;
                CreateSpritePart(
                    flow.transform,
                    $"FlowStar_{index}",
                    star,
                    entry
                    + direction * (1.2f + index * 0.85f)
                    + Vector2.up * 2.1f,
                    Vector2.one
                    * Mathf.Lerp(0.22f, 0.52f, normalized),
                    new Color(
                        0.58f,
                        1f,
                        0.92f,
                        Mathf.Lerp(0.35f, 0.95f, normalized)),
                    81);
            }
        }

        private static void AddOptionalCueForRole(
            Transform parent,
            IReadOnlyList<P11MapRoom2D> rooms,
            P11MapRoomRole roles,
            P11MapCueKind cueKind,
            string name,
            Sprite sprite,
            Color color,
            ICollection<P11MapDirectionCue2D> cues)
        {
            P11MapRoom2D target = null;
            for (int index = 0; index < rooms.Count; index++)
            {
                if (rooms[index] != null
                    && (rooms[index].Roles & roles) != 0)
                {
                    target = rooms[index];
                    break;
                }
            }

            if (target == null)
            {
                return;
            }

            cues.Add(
                CreateDirectionCue(
                    parent,
                    name,
                    cueKind,
                    P11MapRouteKind.Optional,
                    target.transform,
                    sprite,
                    target.transform.position + Vector3.up * 1.8f,
                    color,
                    true));
        }

        private static P11MapDirectionCue2D CreateDirectionCue(
            Transform parent,
            string name,
            P11MapCueKind kind,
            P11MapRouteKind route,
            Transform target,
            Sprite sprite,
            Vector2 worldPosition,
            Color color,
            bool visible)
        {
            GameObject cueObject = CreateSpritePart(
                parent,
                name,
                sprite,
                Vector2.zero,
                Vector2.one * 0.55f,
                color,
                82);
            cueObject.transform.position = new Vector3(
                worldPosition.x,
                worldPosition.y,
                0f);
            P11MapDirectionCue2D cue =
                cueObject.AddComponent<P11MapDirectionCue2D>();
            cue.Configure(
                kind,
                route,
                target,
                cueObject.GetComponent<SpriteRenderer>(),
                visible,
                true,
                true,
                route == P11MapRouteKind.Main ? 1.4f : 2f,
                0.12f,
                0.18f);
            return cue;
        }

        private static P8MaruRoomGraph2D BuildMaruRoomGraph(
            Transform parent,
            IReadOnlyList<P11MapRoom2D> rooms)
        {
            GameObject graphObject = CreateChild(
                parent,
                "P8_TerrainIgnoringRoomGraph");
            P8MaruRoomGraph2D graph =
                graphObject.AddComponent<P8MaruRoomGraph2D>();
            P8MaruRoomNode[] nodes =
                new P8MaruRoomNode[rooms.Count];
            var adjacency = new List<int>[rooms.Count];
            var indices =
                new Dictionary<P11MapRoom2D, int>();
            for (int index = 0; index < rooms.Count; index++)
            {
                adjacency[index] = new List<int>();
                if (rooms[index] != null)
                {
                    indices[rooms[index]] = index;
                }
            }

            P11MapRoom2D[] main = rooms
                .Where(room => room != null && room.OnMainRoute)
                .OrderBy(room => room.MainRouteOrder)
                .ToArray();
            for (int index = 0; index + 1 < main.Length; index++)
            {
                ConnectRoomGraph(
                    indices[main[index]],
                    indices[main[index + 1]],
                    adjacency);
            }

            P11MapRoom2D[] optional = rooms
                .Where(room => room != null && !room.OnMainRoute)
                .ToArray();
            for (int index = 0; index < optional.Length; index++)
            {
                P11MapRoom2D nearest = main
                    .OrderBy(room => Vector2.SqrMagnitude(
                        (Vector2)room.transform.position
                        - (Vector2)optional[index]
                            .transform.position))
                    .FirstOrDefault();
                if (nearest != null)
                {
                    ConnectRoomGraph(
                        indices[optional[index]],
                        indices[nearest],
                        adjacency);
                }
            }

            for (int index = 0; index < rooms.Count; index++)
            {
                P11MapRoom2D room = rooms[index];
                Vector2 center = room.transform.position;
                Vector2 size = new Vector2(
                    room.MacroSize.x * 12f,
                    room.MacroSize.y * 8f);
                nodes[index] = new P8MaruRoomNode(
                    index,
                    new Rect(center - size * 0.5f, size),
                    center,
                    adjacency[index].ToArray());
            }

            graph.Configure(nodes);
            return graph;
        }

        private static void ConnectRoomGraph(
            int first,
            int second,
            IReadOnlyList<List<int>> adjacency)
        {
            if (!adjacency[first].Contains(second))
            {
                adjacency[first].Add(second);
            }

            if (!adjacency[second].Contains(first))
            {
                adjacency[second].Add(first);
            }
        }

        private static bool IsReachableBetweenRooms(
            P8MaruRoomGraph2D graph,
            P11MapRoom2D origin,
            P11MapRoom2D destination)
        {
            return graph != null
                   && origin != null
                   && destination != null
                   && IsReachableOnRoomGraph(
                       graph,
                       origin.transform.position,
                       destination.transform.position);
        }

        private static bool IsReachableOnRoomGraph(
            P8MaruRoomGraph2D graph,
            Vector2 origin,
            Vector2 destination)
        {
            if (graph == null || graph.NodeCount == 0)
            {
                return false;
            }

            int originNode = graph.FindNodeId(origin);
            int destinationNode = graph.FindNodeId(destination);
            return originNode >= 0
                   && destinationNode >= 0
                   && graph.Distance(originNode, destinationNode) >= 0;
        }

        private static bool HasCompletePhysicalMainRoute(
            Transform harnessRoot,
            IReadOnlyList<P11MapRoom2D> rooms)
        {
            Transform connections = harnessRoot.Find(
                "PhysicalRoomConnections_ToolFreeMain");
            int mainRoomCount = rooms.Count(room =>
                room != null && room.OnMainRoute);
            if (connections == null || mainRoomCount == 0)
            {
                return false;
            }

            int expectedConnectionCount =
                Mathf.Max(0, mainRoomCount - 1);
            int readyConnectionCount = 0;
            for (int index = 0;
                 index < connections.childCount;
                 index++)
            {
                Transform connection = connections.GetChild(index);
                bool belongsToMainRoute =
                    connection.name.StartsWith(
                        "Main_",
                        StringComparison.Ordinal);
                if (!belongsToMainRoute)
                {
                    continue;
                }

                BoxCollider2D[] colliders =
                    connection.GetComponentsInChildren<BoxCollider2D>(
                        true);
                if (colliders.Length > 0
                    && colliders.All(collider =>
                        collider != null
                        && collider.enabled
                        && !collider.isTrigger))
                {
                    readyConnectionCount++;
                }
            }

            return readyConnectionCount == expectedConnectionCount;
        }

        private static P8ReturnPile2D BuildReturnPile(
            Transform parent,
            Vector3 position,
            BuildAssets assets)
        {
            GameObject pileObject = CreateChild(
                parent,
                "P8_ReturnPile");
            pileObject.transform.position = position;
            Transform deposit = CreateAnchor(
                pileObject.transform,
                "DepositAnchor",
                new Vector2(0f, 0.35f),
                true);
            P8ReturnPile2D pile =
                pileObject.AddComponent<P8ReturnPile2D>();
            pile.Configure(
                deposit,
                new Vector2(0.48f, 0.22f),
                3);
            CreateSpritePart(
                pileObject.transform,
                "PileVisual",
                assets.PostBoxes,
                Vector2.zero,
                new Vector2(1.1f, 0.7f),
                new Color(0.78f, 0.84f, 1f, 0.9f),
                55);
            return pile;
        }

        private static void CreateRegionalMechanics(
            P11StageEnvironment2D environment,
            P11StageDefinition definition,
            StageLayout layout,
            BuildAssets assets,
            P11StoryState2D story)
        {
            Transform root = CreateChild(
                environment.EnvironmentRoot.transform,
                "RegionalPhysicalMechanics").transform;
            switch (definition.Region)
            {
                case RoomRegion.StarPostOffice:
                    CreatePostOfficeMechanics(
                        root,
                        definition,
                        layout,
                        assets,
                        story);
                    break;
                case RoomRegion.SunriseGarden:
                    CreateGardenMechanics(
                        root,
                        environment,
                        definition,
                        layout,
                        assets);
                    break;
                case RoomRegion.PolarisObservatory:
                    CreateObservatoryMechanics(
                        root,
                        environment,
                        definition,
                        layout,
                        assets);
                    break;
            }
        }

        private static void CreatePostOfficeMechanics(
            Transform parent,
            P11StageDefinition definition,
            StageLayout layout,
            BuildAssets assets,
            P11StoryState2D story)
        {
            if (HasMechanic(
                    definition,
                    P11StageMechanics.ParcelConveyor))
            {
                for (int index = 0; index < 2; index++)
                {
                    GameObject conveyor = CreateSpritePart(
                        parent,
                        $"ParcelConveyor_{index}",
                        assets.PostBoxes,
                        new Vector2(10f + index * 9f, 3.1f),
                        new Vector2(4.5f, 0.7f),
                        new Color(0.44f, 0.70f, 0.82f, 1f),
                        35);
                    BoxCollider2D trigger =
                        conveyor.AddComponent<BoxCollider2D>();
                    trigger.isTrigger = true;
                    trigger.size = new Vector2(4.5f, 1f);
                    P11ParcelConveyor2D runtime =
                        conveyor.AddComponent<
                            P11ParcelConveyor2D>();
                    runtime.Configure(
                        index == 0
                            ? Vector2.right
                            : Vector2.left,
                        2.4f,
                        conveyor.GetComponent<SpriteRenderer>());
                }
            }

            if (HasMechanic(
                    definition,
                    P11StageMechanics.SortingArm))
            {
                GameObject arm = CreateSpritePart(
                    parent,
                    "SortingArm_BirdAddress",
                    assets.PostDoor,
                    new Vector2(23.5f, 5.2f),
                    new Vector2(1.3f, 2.2f),
                    Color.white,
                    42);
                BoxCollider2D armTrigger =
                    arm.AddComponent<BoxCollider2D>();
                armTrigger.isTrigger = true;
                armTrigger.size = new Vector2(2f, 3f);
                P11SortingArm2D sorting =
                    arm.AddComponent<P11SortingArm2D>();
                sorting.Configure(
                    P11ParcelLabel.Bird,
                    Vector2.right,
                    Vector2.left,
                    2.8f,
                    arm.GetComponent<SpriteRenderer>());
            }

            if (HasMechanic(
                    definition,
                    P11StageMechanics.InkLabel))
            {
                GameObject parcel = CreateSpritePart(
                    parent,
                    "AddressableParcel_Unstamped",
                    assets.PostBoxes,
                    new Vector2(10f, 4.2f),
                    Vector2.one * 0.65f,
                    Color.white,
                    45);
                Rigidbody2D parcelBody =
                    parcel.AddComponent<Rigidbody2D>();
                parcelBody.gravityScale = 1.5f;
                BoxCollider2D parcelCollider =
                    parcel.AddComponent<BoxCollider2D>();
                parcelCollider.size = new Vector2(0.8f, 0.8f);
                P11AddressableParcel2D address =
                    parcel.AddComponent<P11AddressableParcel2D>();
                address.Configure(
                    P11ParcelLabel.None,
                    parcel.GetComponent<SpriteRenderer>());

                GameObject inkPool = CreateSpritePart(
                    parent,
                    "InkPool_BirdAddress",
                    assets.Square,
                    new Vector2(15f, 3.1f),
                    new Vector2(2.4f, 0.5f),
                    P11AddressableParcel2D.LabelColor(
                        P11ParcelLabel.Bird),
                    41);
                BoxCollider2D inkTrigger =
                    inkPool.AddComponent<BoxCollider2D>();
                inkTrigger.isTrigger = true;
                inkTrigger.size = new Vector2(2.6f, 1.2f);
                P11InkPool2D ink =
                    inkPool.AddComponent<P11InkPool2D>();
                ink.Configure(
                    P11ParcelLabel.Bird,
                    inkPool.GetComponent<SpriteRenderer>());
            }

            if (HasMechanic(
                    definition,
                    P11StageMechanics.ParcelLauncher))
            {
                GameObject launcherObject = CreateSpritePart(
                    parent,
                    "ParcelLauncher_ToBirdGoal",
                    assets.Arrow,
                    new Vector2(19f, 3.7f),
                    new Vector2(1.4f, 0.7f),
                    new Color(0.74f, 0.92f, 1f, 1f),
                    44);
                BoxCollider2D launcherTrigger =
                    launcherObject.AddComponent<BoxCollider2D>();
                launcherTrigger.isTrigger = true;
                launcherTrigger.size = new Vector2(1.8f, 1.4f);
                P11ParcelLauncher2D launcher =
                    launcherObject.AddComponent<
                        P11ParcelLauncher2D>();
                launcher.Configure(
                    Vector2.right,
                    8f,
                    P11ParcelLabel.Bird,
                    launcherObject.GetComponent<SpriteRenderer>());
            }

            if (HasMechanic(
                    definition,
                    P11StageMechanics.SortingArm))
            {
                GameObject sortingGoalObject = CreateSpritePart(
                    parent,
                    "SortingGoal_BirdAddress",
                    assets.PostDoor,
                    new Vector2(29f, 4.2f),
                    new Vector2(1.4f, 2.2f),
                    P11AddressableParcel2D.LabelColor(
                        P11ParcelLabel.Bird),
                    43);
                BoxCollider2D goalTrigger =
                    sortingGoalObject.AddComponent<BoxCollider2D>();
                goalTrigger.isTrigger = true;
                goalTrigger.size = new Vector2(2.2f, 3f);
                Transform sortedArrival = CreateAnchor(
                    sortingGoalObject.transform,
                    "SortedArrival",
                    new Vector2(0f, 0.35f),
                    true);
                P11SortingGoal2D sortingGoal =
                    sortingGoalObject.AddComponent<
                        P11SortingGoal2D>();
                sortingGoal.Configure(
                    P11ParcelLabel.Bird,
                    story,
                    sortedArrival,
                    sortingGoalObject.GetComponent<
                        SpriteRenderer>());
            }

            if (HasMechanic(
                    definition,
                    P11StageMechanics.MailTube))
            {
                CreateMailTubePair(
                    parent,
                    new Vector2(8f, 9f),
                    new Vector2(
                        Mathf.Min(layout.Size.x - 8f, 38f),
                        14f),
                    assets);
            }

            if (HasMechanic(
                    definition,
                    P11StageMechanics.ReturnStamp))
            {
                GameObject stamp = CreateSpritePart(
                    parent,
                    "ReturnStamp",
                    assets.PostDoor,
                    new Vector2(35f, 3.1f),
                    new Vector2(2f, 0.65f),
                    new Color(0.84f, 0.18f, 0.34f, 0.65f),
                    40);
                BoxCollider2D stampTrigger =
                    stamp.AddComponent<BoxCollider2D>();
                stampTrigger.isTrigger = true;
                P11ReturnStamp2D returnStamp =
                    stamp.AddComponent<P11ReturnStamp2D>();
                Transform safe = CreateAnchor(
                    stamp.transform,
                    "SafeReturnAnchor",
                    new Vector2(-3f, 1f),
                    true);
                returnStamp.Configure(
                    safe,
                    stamp.GetComponent<SpriteRenderer>());
            }
        }

        private static void CreateMailTubePair(
            Transform parent,
            Vector2 firstPosition,
            Vector2 secondPosition,
            BuildAssets assets)
        {
            GameObject first = CreateSpritePart(
                parent,
                "MailTube_A",
                assets.PostDoor,
                firstPosition,
                new Vector2(1.1f, 1.7f),
                new Color(0.48f, 0.92f, 1f, 0.9f),
                43);
            GameObject second = CreateSpritePart(
                parent,
                "MailTube_B",
                assets.PostDoor,
                secondPosition,
                new Vector2(1.1f, 1.7f),
                new Color(0.48f, 0.92f, 1f, 0.9f),
                43);
            BoxCollider2D firstTrigger =
                first.AddComponent<BoxCollider2D>();
            BoxCollider2D secondTrigger =
                second.AddComponent<BoxCollider2D>();
            firstTrigger.isTrigger = true;
            secondTrigger.isTrigger = true;
            P11MailTube2D firstTube =
                first.AddComponent<P11MailTube2D>();
            P11MailTube2D secondTube =
                second.AddComponent<P11MailTube2D>();
            Transform firstArrival = CreateAnchor(
                first.transform,
                "Arrival",
                Vector2.up,
                true);
            Transform secondArrival = CreateAnchor(
                second.transform,
                "Arrival",
                Vector2.up,
                true);
            firstTube.Configure(
                secondTube,
                firstArrival,
                P11ParcelLabel.Star);
            secondTube.Configure(
                firstTube,
                secondArrival,
                P11ParcelLabel.Star);
        }

        private static void CreateGardenMechanics(
            Transform parent,
            P11StageEnvironment2D environment,
            P11StageDefinition definition,
            StageLayout layout,
            BuildAssets assets)
        {
            GameObject platform = CreateSpritePart(
                parent,
                "SunFlower_LightPlatform",
                assets.GardenFlowers,
                new Vector2(
                    layout.Size.x * 0.55f,
                    layout.Size.y * 0.45f),
                new Vector2(2.5f, 0.8f),
                Color.white,
                43);
            BoxCollider2D platformCollider =
                platform.AddComponent<BoxCollider2D>();
            platformCollider.size = new Vector2(2.5f, 0.5f);
            P11LightReactivePlatform2D reactive =
                platform.AddComponent<P11LightReactivePlatform2D>();
            reactive.Configure(
                platformCollider,
                platform.GetComponent<SpriteRenderer>(),
                true);

            GameObject ray = CreateSpritePart(
                parent,
                "RotatingSunRay",
                assets.GardenSun,
                new Vector2(
                    layout.Size.x * 0.45f,
                    layout.Size.y * 0.55f),
                new Vector2(3.8f, 0.28f),
                new Color(1f, 0.78f, 0.28f, 0.72f),
                47);
            P11RotatingSunRay2D sunRay =
                ray.AddComponent<P11RotatingSunRay2D>();
            sunRay.Configure(
                14f,
                ray.GetComponent<SpriteRenderer>(),
                new[] { reactive },
                22f,
                0.8f);

            bool usesShadowSeed =
                definition.StageSlot == P6StageSlot.X1
                || definition.StageSlot == P6StageSlot.X3;
            bool usesGrowthAndHeat =
                definition.StageSlot == P6StageSlot.X2
                || definition.StageSlot == P6StageSlot.X3;
            if (usesShadowSeed)
            {
                P11MapRoom2D seedRoom = FindNearestMainRoom(
                    environment,
                    ray.transform.position);
                GameObject seed = CreateSpritePart(
                    seedRoom.transform,
                    "ShadowSeed_CarryThrowSlow",
                    assets.GardenFlowers,
                    RoomContentLocalPoint(seedRoom, -1.2f, 1.35f),
                    Vector2.one * 0.72f,
                    new Color(0.20f, 0.16f, 0.34f, 1f),
                    49);
                Rigidbody2D seedBody =
                    seed.AddComponent<Rigidbody2D>();
                seedBody.gravityScale = 1.4f;
                seedBody.freezeRotation = true;
                BoxCollider2D seedCollider =
                    seed.AddComponent<BoxCollider2D>();
                seedCollider.size = new Vector2(0.85f, 0.85f);
                CarryableObject2D carryable =
                    seed.AddComponent<CarryableObject2D>();
                carryable.Configure(
                    environment.GridWorld,
                    seedBody,
                    seedCollider,
                    WorldObjectTraits.Carryable
                    | WorldObjectTraits.Pullable,
                    0.65f,
                    6.5f,
                    true);
                GameObject slowVolume = CreateChild(
                    seed.transform,
                    "ShadowSlowTrigger_NoDamage");
                CircleCollider2D slowTrigger =
                    slowVolume.AddComponent<CircleCollider2D>();
                slowTrigger.isTrigger = true;
                slowTrigger.radius = 1.15f;
                P11ShadowSeed2D shadowSeed =
                    seed.AddComponent<P11ShadowSeed2D>();
                shadowSeed.Configure(
                    carryable,
                    slowTrigger,
                    sunRay,
                    reactive,
                    seed.GetComponent<SpriteRenderer>(),
                    P11ShadowSeed2D.DefaultSlowMultiplier);
            }

            if (usesGrowthAndHeat)
            {
                P11MapRoom2D vineRoom = FindMainRoomByOrder(
                    environment,
                    2);
                GameObject vine = CreateChild(
                    vineRoom.transform,
                    "GrowingVine_WaterThenLight");
                vine.transform.localPosition =
                    RoomContentLocalPoint(vineRoom, 1.8f, 1.1f);
                BoxCollider2D vineCollider =
                    vine.AddComponent<BoxCollider2D>();
                vineCollider.size = new Vector2(1.1f, 1f);
                SpriteRenderer dryVine = CreateSpritePart(
                        vine.transform,
                        "DrySeed",
                        assets.GardenFlowers,
                        Vector2.zero,
                        new Vector2(0.55f, 0.45f),
                        new Color(0.52f, 0.36f, 0.22f, 1f),
                        47)
                    .GetComponent<SpriteRenderer>();
                SpriteRenderer grownVine = CreateSpritePart(
                        vine.transform,
                        "GrownVine",
                        assets.GardenFlowers,
                        new Vector2(0f, 0.5f),
                        new Vector2(0.72f, 0.8f),
                        new Color(0.38f, 0.92f, 0.42f, 1f),
                        48)
                    .GetComponent<SpriteRenderer>();
                vine.AddComponent<GrowableVinePlatform2D>();
                P11GrowingVine2D growingVine =
                    vine.AddComponent<P11GrowingVine2D>();
                growingVine.Configure(
                    environment.WaterRegistry,
                    environment.GridWorld,
                    environment.GridWorld.WorldToCell(
                        vine.transform.position),
                    vineCollider,
                    dryVine,
                    grownVine,
                    sunRay,
                    4,
                    1.75f);

                P11MapRoom2D heatRoom = FindMainRoomByOrder(
                    environment,
                    4);
                GameObject heatPlatform = CreateSpritePart(
                    heatRoom.transform,
                    "OverheatedPlatform_WaterSafe",
                    assets.GardenMonolith,
                    RoomContentLocalPoint(heatRoom, 0f, 0.9f),
                    new Vector2(2.6f, 0.55f),
                    new Color(1f, 0.30f, 0.16f, 1f),
                    46);
                Rigidbody2D heatBody =
                    heatPlatform.AddComponent<Rigidbody2D>();
                heatBody.bodyType = RigidbodyType2D.Static;
                BoxCollider2D supportCollider =
                    heatPlatform.AddComponent<BoxCollider2D>();
                supportCollider.size = new Vector2(3f, 0.45f);
                GameObject heatVolume = CreateChild(
                    heatPlatform.transform,
                    "HeatTrigger_OneHeart");
                heatVolume.transform.localPosition =
                    new Vector3(0f, 0.65f, 0f);
                BoxCollider2D heatTrigger =
                    heatVolume.AddComponent<BoxCollider2D>();
                heatTrigger.isTrigger = true;
                heatTrigger.size = new Vector2(3f, 1.25f);
                heatPlatform.AddComponent<OverheatedDevice2D>();
                P11OverheatedPlatform2D overheated =
                    heatPlatform.AddComponent<
                        P11OverheatedPlatform2D>();
                overheated.Configure(
                    environment.WaterRegistry,
                    environment.GridWorld,
                    environment.GridWorld.WorldToCell(
                        heatPlatform.transform.position),
                    supportCollider,
                    heatTrigger,
                    heatPlatform.GetComponent<SpriteRenderer>(),
                    false);
            }

            GameObject nestSilhouette = CreateSpritePart(
                parent,
                "VisibleCrowNestSilhouette",
                assets.Bird,
                new Vector2(
                    layout.Size.x * 0.72f,
                    layout.Size.y * 0.72f),
                new Vector2(1.8f, 1.2f),
                new Color(0.24f, 0.14f, 0.28f, 0.92f),
                44);
            nestSilhouette.transform.rotation =
                Quaternion.Euler(0f, 0f, -8f);
        }

        private static void CreateObservatoryMechanics(
            Transform parent,
            P11StageEnvironment2D environment,
            P11StageDefinition definition,
            StageLayout layout,
            BuildAssets assets)
        {
            if (HasMechanic(
                    definition,
                    P11StageMechanics.ReturnField)
                || HasMechanic(
                    definition,
                    P11StageMechanics.InvariantStarBlock))
            {
                GameObject returnField = CreateSpritePart(
                    parent,
                    "ReturnField",
                    assets.ObservatoryCrystals,
                    new Vector2(
                        layout.Size.x * 0.32f,
                        layout.Size.y * 0.28f),
                    new Vector2(2.2f, 2.2f),
                    new Color(0.52f, 0.72f, 1f, 0.75f),
                    43);
                CircleCollider2D returnVolume =
                    returnField.AddComponent<CircleCollider2D>();
                returnVolume.isTrigger = true;
                returnVolume.radius = 2.4f;
                P11ReturnField2D field =
                    returnField.AddComponent<P11ReturnField2D>();
                field.Configure(
                    5f,
                    returnField.GetComponent<SpriteRenderer>());

                Vector2 invariantPosition =
                    (Vector2)returnField.transform.localPosition
                    + new Vector2(1.1f, 0.2f);
                Transform invariantAnchor = CreateAnchor(
                    parent,
                    "InvariantStarBlock_Anchor",
                    invariantPosition);
                GameObject invariantObject = CreateSpritePart(
                    parent,
                    "InvariantStarBlock_ReturnFieldImmune",
                    assets.Star,
                    invariantPosition,
                    Vector2.one * 0.8f,
                    new Color(0.94f, 0.96f, 1f, 1f),
                    48);
                Rigidbody2D invariantBody =
                    invariantObject.AddComponent<Rigidbody2D>();
                invariantBody.gravityScale = 1f;
                BoxCollider2D invariantCollider =
                    invariantObject.AddComponent<BoxCollider2D>();
                invariantCollider.size = Vector2.one;
                P11InvariantStarBlock2D invariant =
                    invariantObject.AddComponent<
                        P11InvariantStarBlock2D>();
                invariant.Configure(
                    invariantBody,
                    invariantAnchor);
            }

            if (HasMechanic(
                    definition,
                    P11StageMechanics.GravityDial))
            {
                GameObject gravityDialObject = CreateSpritePart(
                    parent,
                    "GravityDial_Cardinal",
                    assets.ObservatoryCrystals,
                    new Vector2(
                        layout.Size.x * 0.46f,
                        layout.Size.y * 0.36f),
                    new Vector2(1.1f, 1.1f),
                    new Color(0.76f, 0.88f, 1f, 1f),
                    47);
                CircleCollider2D dialVolume =
                    gravityDialObject.AddComponent<
                        CircleCollider2D>();
                dialVolume.isTrigger = true;
                dialVolume.radius = 2.2f;
                P11GravityDial2D gravityDial =
                    gravityDialObject.AddComponent<
                        P11GravityDial2D>();
                gravityDial.Configure(
                    Vector2.down,
                    9.81f,
                    gravityDialObject.transform,
                    2.2f);
            }

            if (HasMechanic(
                    definition,
                    P11StageMechanics.OrbitPlatform))
            {
                Transform center = CreateAnchor(
                    parent,
                    "OrbitCenter",
                    new Vector2(
                        layout.Size.x * 0.55f,
                        layout.Size.y * 0.56f));
                GameObject orbit = CreateSpritePart(
                    parent,
                    "OrbitPlatform",
                    assets.ObservatoryCrystals,
                    center.position + Vector3.right * 3f,
                    new Vector2(1.4f, 0.5f),
                    new Color(0.68f, 0.92f, 1f, 1f),
                    45);
                orbit.AddComponent<BoxCollider2D>();
                P11OrbitPlatform2D orbitPlatform =
                    orbit.AddComponent<P11OrbitPlatform2D>();
                orbitPlatform.Configure(center, 3f, 7f, 0f);
            }

            if (HasMechanic(
                    definition,
                    P11StageMechanics.ConstellationBridge))
            {
                GameObject bridgeRoot = CreateChild(
                    parent,
                    "ConstellationBridge");
                GameObject[] segments = new GameObject[4];
                for (int index = 0; index < segments.Length; index++)
                {
                    segments[index] = CreateSpritePart(
                        bridgeRoot.transform,
                        $"StarSegment_{index}",
                        assets.Star,
                        new Vector2(index * 1.25f, 0f),
                        Vector2.one * 0.42f,
                        new Color(0.62f, 0.86f, 1f, 0.85f),
                        46);
                    BoxCollider2D segmentCollider =
                        segments[index].AddComponent<
                            BoxCollider2D>();
                    segmentCollider.size =
                        new Vector2(2.8f, 0.65f);
                }

                bridgeRoot.transform.position = new Vector3(
                    layout.Size.x * 0.42f,
                    layout.Size.y * 0.72f,
                    0f);
                P11ConstellationBridge2D bridge =
                    bridgeRoot.AddComponent<
                        P11ConstellationBridge2D>();
                bridge.Configure(segments);

                P11MapRoom2D bridgeRoom = FindNearestMainRoom(
                    environment,
                    bridgeRoot.transform.position);
                for (int index = 0;
                     index < segments.Length;
                     index++)
                {
                    GameObject receiverObject = CreateSpritePart(
                        bridgeRoom.transform,
                        $"ConstellationReceiver_{index}",
                        assets.ObservatoryCrystals,
                        RoomContentLocalPoint(
                            bridgeRoom,
                            (index - 1.5f) * 1.8f,
                            1.1f),
                        Vector2.one * 0.62f,
                        new Color(
                            0.28f,
                            0.38f,
                            0.58f,
                            0.82f),
                        49);
                    CircleCollider2D receiverTrigger =
                        receiverObject.AddComponent<
                            CircleCollider2D>();
                    receiverTrigger.isTrigger = true;
                    receiverTrigger.radius = 1f;
                    P11ConstellationReceiver2D receiver =
                        receiverObject.AddComponent<
                            P11ConstellationReceiver2D>();
                    receiver.Configure(
                        bridge,
                        index,
                        receiverObject.GetComponent<
                            SpriteRenderer>(),
                        receiverObject.transform,
                        1.5f,
                        70 + index);
                }
            }

            if (definition.StageSlot == P6StageSlot.X1
                && HasMechanic(
                    definition,
                    P11StageMechanics.MemoryBell))
            {
                P11MapRoom2D bellRoom =
                    environment.MapHarness.Rooms
                        .FirstOrDefault(room =>
                            room != null
                            && room.HasRole(
                                P11MapRoomRole.Landmark))
                    ?? FindMainRoomByOrder(environment, 4);
                GameObject bellObject = CreateSpritePart(
                    bellRoom.transform,
                    "MemoryBell_Introduced_5_1",
                    assets.Star,
                    RoomContentLocalPoint(bellRoom, 0f, 1.5f),
                    Vector2.one * 0.82f,
                    new Color(0.66f, 0.88f, 1f, 1f),
                    52);
                CircleCollider2D bellTrigger =
                    bellObject.AddComponent<CircleCollider2D>();
                bellTrigger.isTrigger = true;
                bellTrigger.radius = 1.1f;
                GameObject directionReveal = CreateSpritePart(
                    bellObject.transform,
                    "BellDirectionReveal_ToExit",
                    assets.Arrow,
                    new Vector2(0f, 1.4f),
                    new Vector2(1.1f, 0.55f),
                    new Color(0.72f, 0.94f, 1f, 1f),
                    53);
                P11MemoryBellDevice2D bell =
                    bellObject.AddComponent<
                        P11MemoryBellDevice2D>();
                bell.Configure(
                    environment.ExitAnchor,
                    bellObject.GetComponent<SpriteRenderer>(),
                    directionReveal,
                    P11MemoryBellDevice2D.DefaultPulseSeconds,
                    bellObject.transform,
                    1.6f,
                    72);
            }
        }

        private static StageLayout LayoutFor(
            P11StageDefinition definition)
        {
            switch (definition.Region)
            {
                case RoomRegion.StarPostOffice:
                    switch (definition.StageSlot)
                    {
                        case P6StageSlot.X1:
                            return new StageLayout(
                                new Vector2Int(96, 32),
                                new Vector2Int(5, 5),
                                new Vector2Int(90, 5));
                        case P6StageSlot.X2:
                            return new StageLayout(
                                new Vector2Int(144, 40),
                                new Vector2Int(5, 7),
                                new Vector2Int(126, 7));
                        default:
                            return new StageLayout(
                                new Vector2Int(108, 32),
                                new Vector2Int(5, 5),
                                new Vector2Int(95, 19));
                    }

                case RoomRegion.SunriseGarden:
                    switch (definition.StageSlot)
                    {
                        case P6StageSlot.X1:
                            return new StageLayout(
                                new Vector2Int(60, 88),
                                new Vector2Int(29, 5),
                                new Vector2Int(29, 53));
                        case P6StageSlot.X2:
                            return new StageLayout(
                                new Vector2Int(60, 84),
                                new Vector2Int(29, 5),
                                new Vector2Int(29, 77));
                        default:
                            return new StageLayout(
                                new Vector2Int(60, 60),
                                new Vector2Int(29, 5),
                                new Vector2Int(47, 51));
                    }

                default:
                    switch (definition.StageSlot)
                    {
                        case P6StageSlot.X1:
                            return new StageLayout(
                                new Vector2Int(60, 88),
                                new Vector2Int(11, 5),
                                new Vector2Int(11, 53));
                        case P6StageSlot.X2:
                            return new StageLayout(
                                new Vector2Int(60, 84),
                                new Vector2Int(11, 5),
                                new Vector2Int(11, 77));
                        default:
                            return new StageLayout(
                                new Vector2Int(60, 60),
                                new Vector2Int(11, 5),
                                new Vector2Int(29, 51));
                    }
            }
        }

        private static Transform[] CreateThemePresentation(
            Transform parent,
            P11StageDefinition definition,
            StageLayout layout,
            BuildAssets assets)
        {
            GameObject presentation = CreateChild(
                parent,
                $"Theme_{definition.Region}");
            Vector2 center = new Vector2(
                layout.Size.x * 0.5f,
                layout.Size.y * 0.5f);
            var motifs = new List<Transform>(5);
            switch (definition.Region)
            {
                case RoomRegion.StarPostOffice:
                    motifs.Add(
                        CreateSpritePart(
                            presentation.transform,
                            "AbandonedStationSky",
                            assets.PostSky,
                            center + Vector2.up * 1.5f,
                            new Vector2(4.8f, 3.2f),
                            new Color(0.32f, 0.48f, 0.68f, 0.72f),
                            -80).transform);
                    motifs.Add(
                        CreateSpritePart(
                            presentation.transform,
                            "Landmark_CentralMailCore",
                            assets.PostCore,
                            new Vector2(
                                layout.Size.x * 0.58f,
                                layout.Size.y * 0.56f),
                            new Vector2(2.4f, 2.4f),
                            new Color(0.62f, 0.88f, 1f, 1f),
                            -12).transform);
                    motifs.Add(
                        CreateSpritePart(
                            presentation.transform,
                            "ParcelSilhouette",
                            assets.PostBoxes,
                            new Vector2(
                                layout.Size.x * 0.23f,
                                3.2f),
                            new Vector2(2.2f, 1.4f),
                            Color.white,
                            12).transform);
                    motifs.Add(
                        CreateSpritePart(
                            presentation.transform,
                            "ExitSortingDoor",
                            assets.PostDoor,
                            CellWorld(layout.Exit),
                            new Vector2(1.7f, 2.4f),
                            new Color(0.48f, 1f, 0.90f, 0.9f),
                            14).transform);
                    break;

                case RoomRegion.SunriseGarden:
                    motifs.Add(
                        CreateSpritePart(
                            presentation.transform,
                            "SpringForestBackground",
                            assets.GardenBackground,
                            center,
                            new Vector2(4.2f, 3.2f),
                            new Color(0.62f, 0.86f, 0.74f, 0.78f),
                            -80).transform);
                    motifs.Add(
                        CreateSpritePart(
                            presentation.transform,
                            "Landmark_SunMonolith",
                            assets.GardenMonolith,
                            new Vector2(
                                layout.Size.x * 0.62f,
                                layout.Size.y * 0.62f),
                            new Vector2(2.4f, 2.8f),
                            Color.white,
                            -10).transform);
                    motifs.Add(
                        CreateSpritePart(
                            presentation.transform,
                            "SunDirection",
                            assets.GardenSun,
                            new Vector2(
                                layout.Size.x * 0.76f,
                                layout.Size.y * 0.84f),
                            new Vector2(1.6f, 1.6f),
                            new Color(1f, 0.88f, 0.48f, 0.95f),
                            -18).transform);
                    motifs.Add(
                        CreateSpritePart(
                            presentation.transform,
                            "FlowerRoute",
                            assets.GardenFlowers,
                            new Vector2(
                                layout.Size.x * 0.30f,
                                3.1f),
                            new Vector2(2.8f, 1.2f),
                            Color.white,
                            13).transform);
                    break;

                default:
                    motifs.Add(
                        CreateSpritePart(
                            presentation.transform,
                            "MountSky",
                            assets.ObservatorySky,
                            center + Vector2.up * 3f,
                            new Vector2(4.2f, 4.2f),
                            new Color(0.28f, 0.42f, 0.70f, 0.78f),
                            -90).transform);
                    motifs.Add(
                        CreateSpritePart(
                            presentation.transform,
                            "Landmark_PolarisCrystal",
                            assets.ObservatoryCrystals,
                            new Vector2(
                                layout.Size.x * 0.5f,
                                layout.Size.y * 0.78f),
                            new Vector2(2.8f, 3.4f),
                            new Color(0.60f, 0.92f, 1f, 1f),
                            -8).transform);
                    motifs.Add(
                        CreateSpritePart(
                            presentation.transform,
                            "CrystalDungeonBackground",
                            assets.ObservatoryBackground,
                            center,
                            new Vector2(4f, 4f),
                            new Color(0.42f, 0.52f, 0.82f, 0.66f),
                            -82).transform);
                    motifs.Add(
                        CreateSpritePart(
                            presentation.transform,
                            "CrystalTrees",
                            assets.ObservatoryTrees,
                            new Vector2(
                                layout.Size.x * 0.22f,
                                layout.Size.y * 0.38f),
                            new Vector2(2f, 2.6f),
                            new Color(0.72f, 0.86f, 1f, 1f),
                            -5).transform);
                    motifs.Add(
                        CreateSpritePart(
                            presentation.transform,
                            "NorthStarExit",
                            assets.Star,
                            CellWorld(layout.Exit) + Vector2.up * 2f,
                            Vector2.one * 1.3f,
                            new Color(0.78f, 0.96f, 1f, 1f),
                            18).transform);
                    break;
            }

            return motifs.ToArray();
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

        private static void FillStageGeometry(
            P11StageDefinition definition,
            StageLayout layout,
            Tilemap terrain,
            Tilemap oneWay,
            BuildAssets assets)
        {
            int width = layout.Size.x;
            int height = layout.Size.y;
            SetRect(terrain, assets.ReinforcedTile, 0, 0, width, 2);
            SetRect(terrain, assets.ReinforcedTile, 0, 0, 2, 7);
            SetRect(
                terrain,
                assets.ReinforcedTile,
                width - 2,
                0,
                2,
                Mathf.Min(height, layout.Exit.y + 3));

            switch (definition.Region)
            {
                case RoomRegion.StarPostOffice:
                    FillPostOfficeGeometry(
                        definition.StageSlot,
                        layout,
                        terrain,
                        oneWay,
                        assets);
                    break;
                case RoomRegion.SunriseGarden:
                    FillGardenGeometry(
                        definition.StageSlot,
                        layout,
                        terrain,
                        oneWay,
                        assets);
                    break;
                default:
                    FillObservatoryGeometry(
                        definition.StageSlot,
                        layout,
                        terrain,
                        oneWay,
                        assets);
                    break;
            }
        }

        private static void FillPostOfficeGeometry(
            P6StageSlot slot,
            StageLayout layout,
            Tilemap terrain,
            Tilemap oneWay,
            BuildAssets assets)
        {
            int width = layout.Size.x;
            SetRect(terrain, assets.SoftSoilTile, 8, 2, 7, 2);
            SetRect(terrain, assets.ReinforcedTile, 18, 2, 3, 5);
            SetRect(oneWay, assets.OneWayTile, 22, 6, 7, 1);
            SetRect(terrain, assets.SoftSoilTile, 31, 2, 5, 3);
            SetRect(oneWay, assets.OneWayTile, 38, 8, 6, 1);
            SetRect(
                terrain,
                assets.ReinforcedTile,
                width - 8,
                2,
                6,
                Mathf.Max(2, layout.Exit.y - 1));
            if (slot == P6StageSlot.X2)
            {
                SetRect(oneWay, assets.OneWayTile, 9, 11, 8, 1);
                SetRect(terrain, assets.SoftSoilTile, 19, 8, 4, 7);
                SetRect(oneWay, assets.OneWayTile, 25, 15, 9, 1);
                SetRect(terrain, assets.ReinforcedTile, 37, 9, 4, 6);
                SetRect(oneWay, assets.OneWayTile, 43, 13, 8, 1);
                SetRect(oneWay, assets.OneWayTile, 14, 20, 9, 1);
            }
            else if (slot == P6StageSlot.X3)
            {
                SetRect(terrain, assets.ReinforcedTile, 24, 2, 4, 8);
                SetRect(oneWay, assets.OneWayTile, 29, 10, 8, 1);
                SetRect(oneWay, assets.OneWayTile, 12, 14, 8, 1);
            }
            else
            {
                SetRect(oneWay, assets.OneWayTile, 12, 11, 7, 1);
                SetRect(oneWay, assets.OneWayTile, 27, 14, 8, 1);
            }
        }

        private static void FillGardenGeometry(
            P6StageSlot slot,
            StageLayout layout,
            Tilemap terrain,
            Tilemap oneWay,
            BuildAssets assets)
        {
            int width = layout.Size.x;
            int height = layout.Size.y;
            SetRect(terrain, assets.SoftSoilTile, 6, 2, 6, 3);
            SetRect(oneWay, assets.OneWayTile, 12, 7, 7, 1);
            SetRect(terrain, assets.SoftSoilTile, 20, 2, 4, 9);
            SetRect(oneWay, assets.OneWayTile, 25, 12, 7, 1);
            SetRect(terrain, assets.SoftSoilTile, 33, 2, 4, 14);
            SetRect(
                oneWay,
                assets.OneWayTile,
                Mathf.Max(5, width - 10),
                Mathf.Min(height - 5, layout.Exit.y - 3),
                7,
                1);
            if (slot == P6StageSlot.X2)
            {
                SetRect(oneWay, assets.OneWayTile, 8, 17, 8, 1);
                SetRect(terrain, assets.SoftSoilTile, 18, 16, 4, 8);
                SetRect(oneWay, assets.OneWayTile, 23, 25, 7, 1);
                SetRect(terrain, assets.ReinforcedTile, 32, 21, 3, 8);
                SetRect(oneWay, assets.OneWayTile, 12, 31, 9, 1);
            }
            else if (slot == P6StageSlot.X3)
            {
                SetRect(oneWay, assets.OneWayTile, 7, 18, 8, 1);
                SetRect(terrain, assets.ReinforcedTile, 17, 15, 3, 8);
                SetRect(oneWay, assets.OneWayTile, 22, 24, 8, 1);
            }
            else
            {
                SetRect(oneWay, assets.OneWayTile, 8, 17, 7, 1);
                SetRect(oneWay, assets.OneWayTile, 24, 20, 7, 1);
            }
        }

        private static void FillObservatoryGeometry(
            P6StageSlot slot,
            StageLayout layout,
            Tilemap terrain,
            Tilemap oneWay,
            BuildAssets assets)
        {
            int width = layout.Size.x;
            int height = layout.Size.y;
            SetRect(terrain, assets.ReinforcedTile, 7, 2, 4, 5);
            SetRect(oneWay, assets.OneWayTile, 12, 8, 7, 1);
            SetRect(terrain, assets.SoftSoilTile, 20, 2, 3, 11);
            SetRect(oneWay, assets.OneWayTile, 24, 14, 7, 1);
            SetRect(terrain, assets.ReinforcedTile, 32, 2, 3, 17);
            SetRect(
                oneWay,
                assets.OneWayTile,
                Mathf.Max(4, width / 2 - 4),
                Mathf.Min(height - 5, layout.Exit.y - 4),
                8,
                1);
            if (slot == P6StageSlot.X2)
            {
                SetRect(oneWay, assets.OneWayTile, 8, 20, 7, 1);
                SetRect(terrain, assets.SoftSoilTile, 17, 19, 3, 8);
                SetRect(oneWay, assets.OneWayTile, 21, 28, 8, 1);
                SetRect(terrain, assets.ReinforcedTile, 31, 26, 3, 8);
                SetRect(oneWay, assets.OneWayTile, 14, 36, 8, 1);
            }
            else if (slot == P6StageSlot.X3)
            {
                SetRect(oneWay, assets.OneWayTile, 8, 20, 7, 1);
                SetRect(terrain, assets.SoftSoilTile, 17, 18, 3, 8);
                SetRect(oneWay, assets.OneWayTile, 22, 27, 8, 1);
            }
            else
            {
                SetRect(oneWay, assets.OneWayTile, 7, 19, 7, 1);
                SetRect(oneWay, assets.OneWayTile, 22, 24, 7, 1);
            }
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
                case RoomRegion.StarPostOffice:
                    terrainColor = new Color(0.24f, 0.40f, 0.52f, 1f);
                    oneWayColor = new Color(0.48f, 0.84f, 0.92f, 1f);
                    break;
                case RoomRegion.SunriseGarden:
                    terrainColor = new Color(0.28f, 0.54f, 0.30f, 1f);
                    oneWayColor = new Color(0.84f, 0.78f, 0.35f, 1f);
                    break;
                default:
                    terrainColor = new Color(0.25f, 0.32f, 0.60f, 1f);
                    oneWayColor = new Color(0.58f, 0.80f, 1f, 1f);
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

        private static Vector2 CellWorld(Vector2Int cell)
        {
            return new Vector2(cell.x + 0.5f, cell.y + 0.5f);
        }

        private static Transform CreateAnchor(
            Transform parent,
            string name,
            Vector2 position,
            bool local = false)
        {
            GameObject anchor = CreateChild(parent, name);
            if (local)
            {
                anchor.transform.localPosition =
                    new Vector3(position.x, position.y, 0f);
            }
            else
            {
                anchor.transform.position =
                    new Vector3(position.x, position.y, 0f);
            }

            return anchor.transform;
        }

        private static GridPos[] ProtectedExitCells(Vector2Int exit)
        {
            return new[]
            {
                new GridPos(exit.x, exit.y),
                new GridPos(exit.x, exit.y - 1),
                new GridPos(exit.x - 1, exit.y),
                new GridPos(exit.x + 1, exit.y)
            };
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

        private static P11MapRegion MapRegion(RoomRegion region)
        {
            switch (region)
            {
                case RoomRegion.StarPostOffice:
                    return P11MapRegion.StarPostOffice;
                case RoomRegion.SunriseGarden:
                    return P11MapRegion.SunriseGarden;
                default:
                    return P11MapRegion.PolarisObservatory;
            }
        }

        private static P11MapStageSlot MapSlot(P6StageSlot slot)
        {
            switch (slot)
            {
                case P6StageSlot.X1:
                    return P11MapStageSlot.X1;
                case P6StageSlot.X2:
                    return P11MapStageSlot.X2;
                default:
                    return P11MapStageSlot.X3;
            }
        }

        private static P11MapStageArchetype MapArchetype(
            P11StageDefinition definition)
        {
            if (definition.IsBossStage)
            {
                return P11MapStageArchetype.FixedBoss;
            }

            switch (definition.Archetype)
            {
                case P6StageArchetype.Ascent:
                    return P11MapStageArchetype.Ascent;
                case P6StageArchetype.Descent:
                    return P11MapStageArchetype.Descent;
                case P6StageArchetype.Circuit:
                    return P11MapStageArchetype.Circuit;
                case P6StageArchetype.Fork:
                    return P11MapStageArchetype.Fork;
                default:
                    return P11MapStageArchetype.Traverse;
            }
        }

        private static P11MapRoomRole MainRole(
            P6StageSlot slot,
            int index,
            int count)
        {
            if (slot == P6StageSlot.X1)
            {
                if (index == 0)
                {
                    return P11MapRoomRole.Start
                           | P11MapRoomRole.Teach
                           | P11MapRoomRole.Main;
                }

                if (index == 1)
                {
                    return P11MapRoomRole.Teach
                           | P11MapRoomRole.Main;
                }

                if (index == count - 1)
                {
                    return P11MapRoomRole.Exit
                           | P11MapRoomRole.Main;
                }

                if (index == 3)
                {
                    return P11MapRoomRole.Choice
                           | P11MapRoomRole.Main;
                }

                if (index == 4)
                {
                    return P11MapRoomRole.Landmark
                           | P11MapRoomRole.Main;
                }

                return P11MapRoomRole.Main;
            }

            if (slot == P6StageSlot.X2)
            {
                if (index == 0)
                {
                    return P11MapRoomRole.Start
                           | P11MapRoomRole.Main;
                }

                if (index == count - 1)
                {
                    return P11MapRoomRole.Exit
                           | P11MapRoomRole.Main;
                }

                switch (index)
                {
                    case 2:
                        return P11MapRoomRole.Landmark
                               | P11MapRoomRole.Main;
                    case 3:
                        return P11MapRoomRole.Choice
                               | P11MapRoomRole.Main;
                    case 4:
                        return P11MapRoomRole.ShopCorridor
                               | P11MapRoomRole.Main;
                    case 6:
                        return P11MapRoomRole.LoopConnector
                               | P11MapRoomRole.Main;
                    default:
                        return P11MapRoomRole.Main;
                }
            }

            if (index == 0)
            {
                return P11MapRoomRole.Start
                       | P11MapRoomRole.Main;
            }

            if (index == count - 1)
            {
                return P11MapRoomRole.SupplyCorridor
                       | P11MapRoomRole.Main;
            }

            return P11MapRoomRole.Main;
        }

        private static P11MapRoomRole OptionalRole(
            P6StageSlot slot,
            int index)
        {
            if (slot == P6StageSlot.X2)
            {
                switch (index)
                {
                    case 0:
                        return P11MapRoomRole.MaruRoom;
                    case 1:
                        return P11MapRoomRole.RecordRoom;
                    case 2:
                        return P11MapRoomRole.LocalEvent;
                    default:
                        return P11MapRoomRole.Treasure
                               | P11MapRoomRole.RelicRoom;
                }
            }

            if (slot == P6StageSlot.X1)
            {
                return index == 0
                    ? P11MapRoomRole.LocalEvent
                    : P11MapRoomRole.Treasure;
            }

            return P11MapRoomRole.LocalEvent;
        }

        private static string CoreSentence(P11MapRoomRole role)
        {
            if ((role & P11MapRoomRole.Boss) != 0)
            {
                return "읽힌 지역 장치를 다시 사용해 보스의 별매듭을 푼다.";
            }

            if ((role & P11MapRoomRole.Start) != 0)
            {
                return "출구 별빛과 랜드마크를 한눈에 확인한다.";
            }

            if ((role & P11MapRoomRole.Teach) != 0)
            {
                return "안전한 한 가지 장치를 보고 그대로 따라 한다.";
            }

            if ((role & P11MapRoomRole.ShopCorridor) != 0)
            {
                return "주 경로에서 모모 상점을 지나며 보급 여부를 고른다.";
            }

            if ((role & P11MapRoomRole.SupplyCorridor) != 0)
            {
                return "보스 전 안전 복도에서 자원과 출구를 확인한다.";
            }

            if ((role & P11MapRoomRole.RecordRoom) != 0)
            {
                return "기록관 불빛을 따라 길손 기록을 선택해 읽는다.";
            }

            if ((role & P11MapRoomRole.MaruRoom) != 0)
            {
                return "귀환 더미와 방울 표식으로 마루의 귀환 규칙을 읽는다.";
            }

            if ((role & P11MapRoomRole.LocalEvent) != 0)
            {
                return "지역 설화 표식을 조사하고 원래 길로 돌아온다.";
            }

            if ((role & P11MapRoomRole.RelicRoom) != 0)
            {
                return "갈래 유물과 같은 문양의 선택 보상을 확인한다.";
            }

            if ((role & P11MapRoomRole.Choice) != 0)
            {
                return "밝은 출구 길과 보상 샛길을 시각적으로 비교한다.";
            }

            if ((role & P11MapRoomRole.Exit) != 0)
            {
                return "출구 별빛을 향해 안전 셀을 건너간다.";
            }

            return "지역 장치 하나와 기본 이동을 조합해 다음 안전 셀로 간다.";
        }

        private static Vector2Int MacroSize(
            RoomRegion region,
            P6StageSlot slot,
            int index,
            bool optional)
        {
            if (optional)
            {
                return OptionalMacroSize(region, slot, index);
            }

            if (slot == P6StageSlot.X2)
            {
                if (IsHorizontalRegion(region))
                {
                    return Vector2Int.one;
                }

                if (index == 2)
                {
                    return new Vector2Int(2, 1);
                }

                return index == 5
                    ? new Vector2Int(2, 2)
                    : Vector2Int.one;
            }

            return index == 2
                ? new Vector2Int(2, 1)
                : Vector2Int.one;
        }

        private static Vector2Int OptionalMacroSize(
            RoomRegion region,
            P6StageSlot slot,
            int index)
        {
            if (!IsHorizontalRegion(region)
                || slot != P6StageSlot.X2)
            {
                return Vector2Int.one;
            }

            if (index == 0)
            {
                return new Vector2Int(2, 2);
            }

            return index == 1
                ? new Vector2Int(1, 2)
                : Vector2Int.one;
        }

        private static bool IsHorizontalRegion(RoomRegion region)
        {
            return region == RoomRegion.StarPostOffice;
        }

        private static float VerticalRouteStepPadding(
            P11StageDefinition definition)
        {
            return !IsHorizontalRegion(definition.Region)
                   && definition.StageSlot == P6StageSlot.X1
                ? 4f
                : 0f;
        }

        private static Vector2 RoutePoint(
            P11StageDefinition definition,
            StageLayout layout,
            int index,
            int count)
        {
            if (IsHorizontalRegion(definition.Region))
            {
                float packedX =
                    MacroSize(
                        definition.Region,
                        definition.StageSlot,
                        0,
                        false).x * 6f;
                for (int roomIndex = 1;
                     roomIndex <= index;
                     roomIndex++)
                {
                    Vector2Int previous = MacroSize(
                        definition.Region,
                        definition.StageSlot,
                        roomIndex - 1,
                        false);
                    Vector2Int current = MacroSize(
                        definition.Region,
                        definition.StageSlot,
                        roomIndex,
                        false);
                    packedX += previous.x * 6f
                               + current.x * 6f;
                }

                return new Vector2(
                    packedX,
                    definition.StageSlot == P6StageSlot.X2
                        ? 8f
                        : 6f);
            }

            float verticalPadding =
                VerticalRouteStepPadding(definition);
            float vertical =
                MacroSize(
                    definition.Region,
                    definition.StageSlot,
                    0,
                    false).y * 4f + 2f;
            for (int roomIndex = 1;
                 roomIndex <= index;
                 roomIndex++)
            {
                Vector2Int previous = MacroSize(
                    definition.Region,
                    definition.StageSlot,
                    roomIndex - 1,
                    false);
                Vector2Int current = MacroSize(
                    definition.Region,
                    definition.StageSlot,
                    roomIndex,
                    false);
                vertical += previous.y * 4f
                            + current.y * 4f
                            + verticalPadding;
            }

            return new Vector2(
                definition.Region == RoomRegion.PolarisObservatory
                    ? 12f
                    : layout.Size.x * 0.5f,
                vertical);
        }

        private static Vector2 OptionalPoint(
            P11StageDefinition definition,
            StageLayout layout,
            int index)
        {
            if (IsHorizontalRegion(definition.Region))
            {
                if (definition.StageSlot == P6StageSlot.X2)
                {
                    return new Vector2(16f + index * 26f, 26f);
                }

                return new Vector2(
                    6f + index * 12f,
                    definition.StageSlot == P6StageSlot.X3
                        ? 20f
                        : layout.Size.y - 6f);
            }

            if (definition.Region == RoomRegion.PolarisObservatory)
            {
                return new Vector2(
                    layout.Size.x - 6f,
                    12f + index * 12f);
            }

            return new Vector2(
                index % 2 == 0
                    ? 6f
                    : layout.Size.x - 6f,
                12f + (index / 2) * 16f);
        }

        private static Vector2 BossArenaCenter(
            P11StageDefinition definition,
            StageLayout layout)
        {
            if (definition.Region == RoomRegion.StarPostOffice)
            {
                return new Vector2(layout.Size.x - 12f, 20f);
            }

            if (definition.Region == RoomRegion.PolarisObservatory)
            {
                return new Vector2(
                    layout.Size.x * 0.5f,
                    layout.Size.y - 8f);
            }

            return new Vector2(
                layout.Size.x - 12f,
                layout.Size.y - 8f);
        }

        private static void CreateStageExit(
            P11StageNode2D node,
            P11StageFlowController2D flow,
            BuildAssets assets)
        {
            Transform parent =
                node.Environment.EnvironmentRoot.transform;
            GameObject root = CreateChild(
                parent,
                $"MainExit_{node.StageId}");
            root.transform.position =
                node.Environment.ExitAnchor.position;
            BoxCollider2D trigger =
                root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.8f, 2.8f);
            GameObject ready = CreateSpritePart(
                root.transform,
                "Ready_ExitStar",
                assets.Star,
                Vector2.zero,
                Vector2.one * 1.1f,
                new Color(0.56f, 1f, 0.88f, 1f),
                91);
            GameObject locked = CreateSpritePart(
                root.transform,
                "Locked_StarKnot",
                assets.Square,
                Vector2.zero,
                new Vector2(0.8f, 1.2f),
                new Color(0.78f, 0.22f, 0.36f, 0.9f),
                92);
            P11StageExit2D exit =
                root.AddComponent<P11StageExit2D>();
            exit.Configure(node, flow, ready, locked);
        }

        private static void CreateX2Shops(
            IReadOnlyList<P11StageNode2D> nodes,
            P10FirstBranchCampaignContract p10Contract,
            BuildAssets assets)
        {
            P5MomoShop2D template = p10Contract.StageNodes
                .Where(node =>
                    node != null
                    && node.Environment != null
                    && node.Environment.EnvironmentRoot != null)
                .Select(node =>
                    node.Environment.EnvironmentRoot
                        .GetComponentInChildren<P5MomoShop2D>(true))
                .FirstOrDefault(shop => shop != null);
            P8MaruTimeline2D timeline =
                p10Contract.StageFlow != null
                    && p10Contract.StageFlow.MaruController != null
                        ? p10Contract.StageFlow.MaruController.Timeline
                        : null;
            if (template == null || timeline == null)
            {
                throw new InvalidOperationException(
                    "P11 requires the inherited Momo shop and persistent "
                    + "Maru timeline.");
            }

            for (int index = 0; index < nodes.Count; index++)
            {
                P11StageNode2D node = nodes[index];
                if (node.Definition.StageSlot != P6StageSlot.X2)
                {
                    continue;
                }

                P11MapRoom2D shopRoom = RequireRoleRoom(
                    node,
                    P11MapRoomRole.ShopCorridor);
                GameObject economy = CreateChild(
                    shopRoom.transform,
                    "GuaranteedMomoShop_X2");
                GameObject shop = UnityEngine.Object.Instantiate(
                    template.gameObject,
                    economy.transform,
                    false);
                shop.name = $"MomoShop_{node.StageId}";
                shop.transform.localPosition =
                    RoomContentLocalPoint(shopRoom, 0f, 2f);
                CreateSpritePart(
                    economy.transform,
                    "ShopOptionalCue",
                    assets.Star,
                    new Vector2(0f, 2f),
                    Vector2.one * 0.48f,
                    new Color(1f, 0.78f, 0.32f, 1f),
                    61);

                P11MapRoom2D maruRoom = RequireRoleRoom(
                    node,
                    P11MapRoomRole.MaruRoom);
                CreateHomecomingStatue(
                    maruRoom,
                    node.StageId,
                    timeline,
                    assets);
            }
        }

        private static P8HomecomingStatue2D CreateHomecomingStatue(
            P11MapRoom2D room,
            P11StageId stageId,
            P8MaruTimeline2D timeline,
            BuildAssets assets)
        {
            GameObject statue = CreateChild(
                room.transform,
                $"HomecomingStatue_{stageId}_1x2");
            statue.transform.localPosition =
                RoomContentLocalPoint(room, 0f, 1.35f);
            Rigidbody2D body = statue.AddComponent<Rigidbody2D>();
            body.gravityScale = 1f;
            body.freezeRotation = true;
            BoxCollider2D collider =
                statue.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.82f, 1.72f);

            GameObject intact = CreateSpritePart(
                statue.transform,
                "Intact",
                assets.PostBoxes,
                Vector2.zero,
                new Vector2(0.95f, 1.9f),
                new Color(0.72f, 0.78f, 0.92f, 1f),
                108);
            GameObject cracked = CreateSpritePart(
                statue.transform,
                "Cracked",
                assets.ObservatoryCrystals,
                Vector2.zero,
                new Vector2(1.05f, 1.8f),
                new Color(0.66f, 0.54f, 0.82f, 1f),
                109);
            GameObject destroyed = CreateSpritePart(
                statue.transform,
                "Destroyed",
                assets.ObservatoryCrystals,
                new Vector2(0f, -0.7f),
                new Vector2(1.2f, 0.55f),
                new Color(0.48f, 0.42f, 0.62f, 1f),
                109);
            cracked.SetActive(false);
            destroyed.SetActive(false);
            SpriteRenderer glow = CreateSpritePart(
                    statue.transform,
                    "VisibleStarTearGlow",
                    assets.Star,
                    new Vector2(0f, 0.25f),
                    new Vector2(0.72f, 0.72f),
                    new Color(0.58f, 0.94f, 1f, 0.75f),
                    110)
                .GetComponent<SpriteRenderer>();

            P8HomecomingStatue2D component =
                statue.AddComponent<P8HomecomingStatue2D>();
            component.Configure(
                timeline,
                null,
                body,
                collider,
                intact,
                cracked,
                destroyed,
                glow);
            return component;
        }

        private static void CreateArchives(
            IReadOnlyList<P11StageNode2D> nodes,
            P9RecordGuestCatalog catalog,
            Transform player,
            BuildAssets assets)
        {
            for (int index = 0; index < nodes.Count; index++)
            {
                P11StageNode2D node = nodes[index];
                if (node.Definition.StageSlot != P6StageSlot.X2)
                {
                    continue;
                }

                P9RecordGuestDefinition definition =
                    catalog.FindForRegion(node.Definition.Region);
                if (definition == null)
                {
                    throw new InvalidOperationException(
                        $"P9 record guest is missing for "
                        + $"{node.Definition.Region}.");
                }

                P11MapRoom2D recordRoom = RequireRoleRoom(
                    node,
                    P11MapRoomRole.RecordRoom);
                Transform parent = recordRoom.transform;
                GameObject root = CreateChild(
                    parent,
                    $"GuaranteedStarArchive_{node.StageId}");
                root.transform.localPosition =
                    RoomContentLocalPoint(recordRoom, 0f, 2f);
                Transform cue = CreateSpritePart(
                    root.transform,
                    "ArchiveLightCue",
                    assets.Star,
                    new Vector2(0f, 1.6f),
                    Vector2.one * 0.55f,
                    new Color(0.48f, 1f, 0.92f, 1f),
                    72).transform;
                GameObject sealedVisual = CreateSpritePart(
                    root.transform,
                    "SealedArchive",
                    assets.Square,
                    Vector2.zero,
                    new Vector2(1.8f, 2.1f),
                    new Color(0.10f, 0.34f, 0.48f, 1f),
                    68);
                GameObject openVisual = CreateSpritePart(
                    root.transform,
                    "OpenedArchive",
                    assets.Star,
                    Vector2.zero,
                    Vector2.one,
                    new Color(0.78f, 1f, 0.94f, 1f),
                    69);
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

                GameObject followerRoot = CreateChild(
                    root.transform,
                    "RecordGuestFollower");
                GameObject followerVisual = CreateSpritePart(
                    followerRoot.transform,
                    "GuestVisual",
                    assets.Star,
                    Vector2.zero,
                    Vector2.one * 0.72f,
                    new Color(1f, 0.84f, 0.50f, 1f),
                    73);
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
            }
        }

        private static P11NaraeMailbox2D CreateNaraeMailbox(
            P11StageNode2D node,
            P11StoryState2D story,
            BuildAssets assets)
        {
            P11MapRoom2D eventRoom = RequireRoleRoom(
                node,
                P11MapRoomRole.LocalEvent);
            GameObject root = CreateChild(
                eventRoom.transform,
                "NaraeMailbox_TextFreeInference");
            root.transform.localPosition =
                RoomContentLocalPoint(eventRoom, 0f, 1.8f);
            BoxCollider2D trigger =
                root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(2.4f, 2.6f);
            GameObject closed = CreateSpritePart(
                root.transform,
                "ClosedDrawer",
                assets.PostDoor,
                Vector2.zero,
                new Vector2(1.6f, 1.8f),
                Color.white,
                76);
            GameObject open = CreateSpritePart(
                root.transform,
                "OpenDrawer",
                assets.PostBoxes,
                Vector2.zero,
                new Vector2(1.8f, 1.2f),
                new Color(0.62f, 0.92f, 1f, 1f),
                77);
            GameObject letter = CreateSpritePart(
                root.transform,
                "Letter_WithBirdSeal",
                assets.Bird,
                new Vector2(0f, 0.45f),
                new Vector2(0.7f, 0.55f),
                new Color(1f, 0.82f, 0.72f, 1f),
                80);
            GameObject dawnMap = CreateSpritePart(
                root.transform,
                "DawnStarCoordinates",
                assets.Star,
                new Vector2(0.7f, 0.65f),
                Vector2.one * 0.48f,
                new Color(1f, 0.88f, 0.36f, 1f),
                81);
            SpriteRenderer threadSeal = CreateSpritePart(
                    root.transform,
                    "RedThreadSeal",
                    assets.Square,
                    new Vector2(-0.65f, 0.65f),
                    new Vector2(0.25f, 0.8f),
                    new Color(0.92f, 0.16f, 0.28f, 1f),
                    82)
                .GetComponent<SpriteRenderer>();
            SpriteRenderer orbGlyph = CreateSpritePart(
                    root.transform,
                    "DragonOrbGlyph",
                    assets.Star,
                    new Vector2(0.65f, 0.65f),
                    Vector2.one * 0.38f,
                    new Color(0.34f, 0.88f, 1f, 1f),
                    82)
                .GetComponent<SpriteRenderer>();
            P11NaraeMailbox2D mailbox =
                root.AddComponent<P11NaraeMailbox2D>();
            mailbox.Configure(
                story,
                closed,
                open,
                letter,
                dawnMap,
                threadSeal,
                orbGlyph);
            return mailbox;
        }

        private static P11YoungCrowEvent2D CreateYoungCrow(
            P11StageNode2D node,
            P11StoryState2D story,
            BuildAssets assets)
        {
            P11MapRoom2D eventRoom = RequireRoleRoom(
                node,
                P11MapRoomRole.LocalEvent);
            GameObject root = CreateChild(
                eventRoom.transform,
                "YoungCrow_LetterInference");
            root.transform.localPosition =
                RoomContentLocalPoint(eventRoom, 0f, 1.8f);
            CircleCollider2D trigger =
                root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.4f;
            CreateSpritePart(
                root.transform,
                "YoungCrow",
                assets.Bird,
                Vector2.zero,
                new Vector2(1.2f, 1f),
                new Color(0.18f, 0.16f, 0.24f, 1f),
                78);
            GameObject sealCue = CreateSpritePart(
                root.transform,
                "MatchingLetterSeal",
                assets.Square,
                new Vector2(-0.6f, 1.15f),
                new Vector2(0.22f, 0.65f),
                new Color(0.94f, 0.18f, 0.28f, 1f),
                82);
            GameObject gazeCue = CreateSpritePart(
                root.transform,
                "CrowGazeToSeal",
                assets.Star,
                new Vector2(0.3f, 1.1f),
                Vector2.one * 0.32f,
                new Color(1f, 0.86f, 0.40f, 1f),
                82);
            GameObject guide = CreateSpritePart(
                root.transform,
                "TrustedGuide",
                assets.Bird,
                new Vector2(1.1f, 0.4f),
                new Vector2(0.8f, 0.7f),
                new Color(0.42f, 0.92f, 1f, 1f),
                80);
            GameObject nestRoute = CreateSpritePart(
                root.transform,
                "VisibleNestRoute",
                assets.GardenFlowers,
                new Vector2(2f, 1.3f),
                new Vector2(1.2f, 0.5f),
                new Color(1f, 0.72f, 0.32f, 1f),
                79);
            P11YoungCrowEvent2D crow =
                root.AddComponent<P11YoungCrowEvent2D>();
            crow.Configure(
                story,
                sealCue,
                gazeCue,
                guide,
                nestRoute);
            return crow;
        }

        private static P11CrowNestEvent2D CreateCrowNest(
            P11StageNode2D node,
            P11StoryState2D story,
            BuildAssets assets)
        {
            P11MapRoom2D eventRoom = RequireRoleRoom(
                node,
                P11MapRoomRole.LocalEvent);
            GameObject root = CreateChild(
                eventRoom.transform,
                "CrowNest_WaterAndHookOptional");
            root.transform.localPosition =
                RoomContentLocalPoint(eventRoom, 0f, 2f);
            CircleCollider2D trigger =
                root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.8f;
            GameObject hotRoot = CreateSpritePart(
                root.transform,
                "OverheatedRoot_WaterCue",
                assets.GardenFlowers,
                new Vector2(-0.8f, -0.4f),
                new Vector2(1.7f, 0.8f),
                new Color(1f, 0.30f, 0.18f, 1f),
                76);
            GameObject sealedRing = CreateSpritePart(
                root.transform,
                "SealedRing_HookCue",
                assets.Star,
                new Vector2(0.7f, 0.5f),
                Vector2.one * 0.8f,
                new Color(0.62f, 0.68f, 0.80f, 1f),
                78);
            GameObject restored = CreateSpritePart(
                root.transform,
                "RestoredCrowNest",
                assets.Bird,
                Vector2.zero,
                new Vector2(1.8f, 1.4f),
                new Color(0.42f, 0.92f, 0.54f, 1f),
                79);
            GameObject support = CreateSpritePart(
                root.transform,
                "BossSupportSunFlight",
                assets.GardenSun,
                new Vector2(1.7f, 1.3f),
                Vector2.one * 0.8f,
                new Color(1f, 0.88f, 0.34f, 1f),
                80);
            P11CrowNestEvent2D nest =
                root.AddComponent<P11CrowNestEvent2D>();
            nest.Configure(
                story,
                hotRoot,
                sealedRing,
                restored,
                support);
            return nest;
        }

        private static P11RegionalBoss2D CreateRegionalBoss(
            P11StageNode2D node,
            P11BossKind kind,
            P11StoryState2D story,
            BuildAssets assets)
        {
            P11MapRoom2D bossRoom = RequireRoleRoom(
                node,
                P11MapRoomRole.Boss);
            GameObject root = CreateChild(
                bossRoom.transform,
                $"Boss_{kind}_DirectAndEnvironment");
            root.transform.localPosition = Vector3.zero;
            BoxCollider2D receiver =
                root.AddComponent<BoxCollider2D>();
            receiver.isTrigger = true;
            receiver.size = new Vector2(4.6f, 4.8f);
            Sprite bodySprite = kind == P11BossKind.Popo
                ? assets.PostCore
                : assets.GardenMonolith;
            CreateSpritePart(
                root.transform,
                "BossBody",
                bodySprite,
                Vector2.zero,
                new Vector2(2.4f, 2.4f),
                kind == P11BossKind.Popo
                    ? new Color(0.58f, 0.82f, 1f, 1f)
                    : new Color(1f, 0.68f, 0.30f, 1f),
                72);

            SpriteRenderer[] knots =
                new SpriteRenderer[
                    P11RegionalBoss2D.RequiredStarKnots];
            GameObject[] targets =
                new GameObject[
                    P11RegionalBoss2D.RequiredStarKnots];
            for (int index = 0; index < knots.Length; index++)
            {
                float angle = index * Mathf.PI * 2f / knots.Length;
                Vector2 offset = new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)) * 1.8f;
                knots[index] = CreateSpritePart(
                        root.transform,
                        $"StarKnot_{index}",
                        assets.Star,
                        offset,
                        Vector2.one * 0.48f,
                        new Color(1f, 0.40f, 0.66f, 1f),
                        79)
                    .GetComponent<SpriteRenderer>();
                targets[index] = CreateSpritePart(
                    root.transform,
                    $"EnvironmentTarget_{index}",
                    kind == P11BossKind.Popo
                        ? assets.PostBoxes
                        : assets.GardenFlowers,
                    new Vector2(
                        (index - 1) * 2.4f,
                        -2.6f),
                    new Vector2(0.8f, 0.65f),
                    new Color(0.58f, 1f, 0.82f, 1f),
                    78);
                CircleCollider2D targetTrigger =
                    targets[index].AddComponent<CircleCollider2D>();
                targetTrigger.isTrigger = true;
                targetTrigger.radius = 0.8f;
            }

            P11RegionalBoss2D boss =
                root.AddComponent<P11RegionalBoss2D>();
            boss.Configure(kind, node, story, knots, targets);
            for (int index = 0; index < targets.Length; index++)
            {
                P11BossSolutionInput input;
                if (kind == P11BossKind.Popo)
                {
                    input = index == 1
                        ? P11BossSolutionInput.ReturnStamp
                        : P11BossSolutionInput.ParcelReflection;
                }
                else
                {
                    input = index == 1
                        ? P11BossSolutionInput.GrowingVine
                        : P11BossSolutionInput.CrossedLightAndShadow;
                }

                P11RegionalBossEnvironmentTarget2D target =
                    targets[index].AddComponent<
                        P11RegionalBossEnvironmentTarget2D>();
                target.Configure(boss, index, input);
            }

            return boss;
        }

        private static P11SunEmberPickup2D CreateSunEmber(
            P11StageNode2D node,
            P11StoryState2D story,
            BuildAssets assets)
        {
            P11MapRoom2D bossRoom = RequireRoleRoom(
                node,
                P11MapRoomRole.Boss);
            GameObject root = CreateChild(
                bossRoom.transform,
                "SunEmber_MatchingBellSocket");
            root.transform.localPosition =
                RoomContentLocalPoint(bossRoom, -3f, 2f);
            CircleCollider2D trigger =
                root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.2f;
            GameObject ember = CreateSpritePart(
                root.transform,
                "SunEmber",
                assets.GardenSun,
                Vector2.zero,
                Vector2.one * 0.9f,
                new Color(1f, 0.58f, 0.18f, 1f),
                84);
            GameObject socketCue = CreateSpritePart(
                root.transform,
                "MatchingMemoryBellSocket",
                assets.Star,
                new Vector2(1.3f, 0f),
                Vector2.one * 0.72f,
                new Color(1f, 0.58f, 0.18f, 0.9f),
                83);
            P11SunEmberPickup2D pickup =
                root.AddComponent<P11SunEmberPickup2D>();
            pickup.Configure(story, ember, socketCue);
            return pickup;
        }

        private static P11MaruStoryTableau2D CreateMaruTableau(
            P11StageNode2D node,
            BuildAssets assets)
        {
            P11MapRoom2D landmarkRoom = RequireRoleRoom(
                node,
                P11MapRoomRole.Landmark);
            GameObject root = CreateChild(
                landmarkRoom.transform,
                "MaruStoryTableau_MainRoute_4Beats");
            root.transform.localPosition =
                RoomContentLocalPoint(landmarkRoom, 0f, 2f);
            GameObject[] beats = new GameObject[4];
            Sprite[] beatSprites =
            {
                assets.Bird,
                assets.Star,
                assets.PostBoxes,
                assets.Square
            };
            Color[] colors =
            {
                new Color(0.80f, 0.72f, 1f, 1f),
                new Color(1f, 0.88f, 0.42f, 1f),
                new Color(0.58f, 0.92f, 1f, 1f),
                new Color(0.92f, 0.24f, 0.42f, 1f)
            };
            for (int index = 0; index < beats.Length; index++)
            {
                beats[index] = CreateSpritePart(
                    root.transform,
                    $"Beat_{index}_{(P11MaruStoryBeat)index}",
                    beatSprites[index],
                    new Vector2((index - 1.5f) * 1.5f, 0f),
                    Vector2.one * 0.82f,
                    colors[index],
                    75);
            }

            P11MaruStoryTableau2D tableau =
                root.AddComponent<P11MaruStoryTableau2D>();
            tableau.Configure(beats, true);
            return tableau;
        }

        private static P11MaruFinalBoss2D CreateMaruFinalBoss(
            P11StageNode2D node,
            P11StageFlowController2D flow,
            P11StoryState2D story,
            BuildAssets assets)
        {
            P11MapRoom2D bossRoom = RequireRoleRoom(
                node,
                P11MapRoomRole.Boss);
            GameObject root = CreateChild(
                bossRoom.transform,
                "Boss_Maru_ThreeEndingSolutions");
            root.transform.localPosition = Vector3.zero;
            BoxCollider2D receiver =
                root.AddComponent<BoxCollider2D>();
            receiver.isTrigger = true;
            receiver.size = new Vector2(5f, 5f);
            CreateSpritePart(
                root.transform,
                "MaruCommandCollarBody",
                assets.ObservatoryTrees,
                Vector2.zero,
                new Vector2(2.5f, 2.5f),
                new Color(0.38f, 0.34f, 0.58f, 1f),
                74);

            SpriteRenderer[] knots =
                new SpriteRenderer[P11MaruFinalBoss2D.RequiredStarKnots];
            for (int index = 0; index < knots.Length; index++)
            {
                float angle = index * Mathf.PI * 2f / knots.Length;
                knots[index] = CreateSpritePart(
                        root.transform,
                        $"MaruStarKnot_{index}",
                        assets.Star,
                        new Vector2(
                            Mathf.Cos(angle),
                            Mathf.Sin(angle)) * 2.2f,
                        Vector2.one * 0.45f,
                        new Color(1f, 0.34f, 0.62f, 1f),
                        81)
                    .GetComponent<SpriteRenderer>();
            }

            GameObject[] pillars =
                new GameObject[
                    P11MaruFinalBoss2D.RequiredCommandPillars];
            for (int index = 0; index < pillars.Length; index++)
            {
                pillars[index] = CreateSpritePart(
                    root.transform,
                    $"CommandPillar_{index}",
                    assets.ObservatoryCrystals,
                    new Vector2((index - 2) * 1.7f, -3.2f),
                    new Vector2(0.65f, 1.5f),
                    new Color(0.48f, 0.76f, 1f, 1f),
                    77);
                BoxCollider2D collider =
                    pillars[index].AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
            }

            GameObject unlitBell = CreateSpritePart(
                root.transform,
                "MemoryBell_Unlit_SunEmberSocket",
                assets.Star,
                new Vector2(-4.7f, 2.8f),
                Vector2.one * 0.9f,
                new Color(0.32f, 0.38f, 0.58f, 1f),
                86);
            GameObject litBell = CreateSpritePart(
                root.transform,
                "MemoryBell_Lit",
                assets.Star,
                new Vector2(-4.7f, 2.8f),
                Vector2.one,
                new Color(1f, 0.62f, 0.20f, 1f),
                87);
            GameObject memoryRoute = CreateSpritePart(
                root.transform,
                "MemoryRoute_AfterShortShortLong",
                assets.Star,
                new Vector2(4.8f, 2.8f),
                Vector2.one * 1.2f,
                new Color(0.66f, 1f, 0.92f, 1f),
                88);

            P11MaruFinalBoss2D boss =
                root.AddComponent<P11MaruFinalBoss2D>();
            boss.Configure(
                flow,
                story,
                knots,
                pillars,
                unlitBell,
                litBell,
                memoryRoute);
            for (int index = 0; index < pillars.Length; index++)
            {
                P11MaruCommandPillar2D pillar =
                    pillars[index].AddComponent<
                        P11MaruCommandPillar2D>();
                pillar.Configure(boss, index);
            }

            CircleCollider2D lightTrigger =
                unlitBell.AddComponent<CircleCollider2D>();
            lightTrigger.isTrigger = true;
            P11MemoryBellInput2D lightInput =
                unlitBell.AddComponent<P11MemoryBellInput2D>();
            lightInput.Configure(boss, P8BellSignal.Short, true);
            P8BellSignal[] pattern =
            {
                P8BellSignal.Short,
                P8BellSignal.Short,
                P8BellSignal.Long
            };
            for (int index = 0; index < pattern.Length; index++)
            {
                GameObject bell = CreateSpritePart(
                    root.transform,
                    $"PatternBell_{index}_{pattern[index]}",
                    assets.Star,
                    new Vector2(-1.8f + index * 1.8f, 3.6f),
                    Vector2.one
                    * (pattern[index] == P8BellSignal.Long
                        ? 0.85f
                        : 0.62f),
                    new Color(0.72f, 0.90f, 1f, 1f),
                    86);
                CircleCollider2D bellTrigger =
                    bell.AddComponent<CircleCollider2D>();
                bellTrigger.isTrigger = true;
                P11MemoryBellInput2D input =
                    bell.AddComponent<P11MemoryBellInput2D>();
                input.Configure(boss, pattern[index], false);
            }

            CircleCollider2D routeTrigger =
                memoryRoute.AddComponent<CircleCollider2D>();
            routeTrigger.isTrigger = true;
            P11MemoryRouteExit2D routeExit =
                memoryRoute.AddComponent<P11MemoryRouteExit2D>();
            routeExit.Configure(boss);
            return boss;
        }

        private static P11EndingPresentation2D
            CreateEndingPresentation(
                P11StageNode2D node,
                P11CampaignDirector2D director,
                BuildAssets assets)
        {
            P11MapRoom2D bossRoom = RequireRoleRoom(
                node,
                P11MapRoomRole.Boss);
            GameObject root = CreateChild(
                bossRoom.transform,
                "EndingPresentation_Normal_Environmental_Memory");
            root.transform.localPosition = new Vector3(0f, 3f, 0f);
            GameObject normal = CreateSpritePart(
                root.transform,
                "NormalEnding_ReleasedStarKnot",
                assets.Star,
                new Vector2(-2f, 0f),
                Vector2.one * 1.2f,
                new Color(0.72f, 0.88f, 1f, 1f),
                96);
            GameObject environmental = CreateSpritePart(
                root.transform,
                "EnvironmentalEnding_BrokenCommand",
                assets.ObservatoryCrystals,
                new Vector2(2f, 0f),
                new Vector2(1.2f, 1.5f),
                new Color(0.62f, 1f, 0.86f, 1f),
                96);
            GameObject memoryRoot = CreateChild(
                root.transform,
                "MemoryEnding_CoreTragedy_5Beats");
            GameObject[] beats = new GameObject[5];
            for (int index = 0; index < beats.Length; index++)
            {
                beats[index] = CreateSpritePart(
                    memoryRoot.transform,
                    $"MemoryBeat_{index}_{(P11MemoryEndingBeat)index}",
                    index == 2 ? assets.Bird : assets.Star,
                    new Vector2((index - 2) * 1.3f, 0f),
                    Vector2.one * 0.66f,
                    index == 3
                        ? new Color(0.92f, 0.24f, 0.42f, 1f)
                        : new Color(0.72f, 0.90f, 1f, 1f),
                    97);
            }

            P11EndingPresentation2D presentation =
                root.AddComponent<P11EndingPresentation2D>();
            presentation.Configure(
                director,
                normal,
                environmental,
                memoryRoot,
                beats);
            return presentation;
        }

        private static P11MapRoom2D RequireRoleRoom(
            P11StageNode2D node,
            P11MapRoomRole role)
        {
            P11MapRoom2D room =
                node.Environment.MapHarness.Rooms
                    .FirstOrDefault(item =>
                        item != null && item.HasRole(role));
            if (room == null)
            {
                throw new InvalidOperationException(
                    $"{node.StageId} lacks authored role room {role}.");
            }

            return room;
        }

        private static P11MapRoom2D FindMainRoomByOrder(
            P11StageEnvironment2D environment,
            int preferredOrder)
        {
            P11MapRoom2D room =
                environment.MapHarness.Rooms
                    .Where(item =>
                        item != null
                        && item.OnMainRoute
                        && (item.Roles
                            & P11MapRoomRole.Main) != 0)
                    .OrderBy(item =>
                        Mathf.Abs(
                            item.MainRouteOrder
                            - preferredOrder))
                    .FirstOrDefault();
            if (room == null)
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} has no main-route room.");
            }

            return room;
        }

        private static P11MapRoom2D FindNearestMainRoom(
            P11StageEnvironment2D environment,
            Vector2 worldPosition)
        {
            P11MapRoom2D room =
                environment.MapHarness.Rooms
                    .Where(item =>
                        item != null
                        && item.OnMainRoute
                        && (item.Roles
                            & P11MapRoomRole.Main) != 0)
                    .OrderBy(item => Vector2.SqrMagnitude(
                        (Vector2)item.transform.position
                        - worldPosition))
                    .FirstOrDefault();
            if (room == null)
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} has no main-route room.");
            }

            return room;
        }

        private static Vector3 RoomContentLocalPoint(
            P11MapRoom2D room,
            float horizontalOffset,
            float heightAboveFloor)
        {
            return new Vector3(
                horizontalOffset,
                -room.MacroSize.y * 4f
                + Mathf.Max(0.5f, heightAboveFloor),
                0f);
        }

        private static void RetrofitP10DirectionCues(
            IReadOnlyList<P10StageEnvironment2D> environments,
            BuildAssets assets)
        {
            for (int index = 0; index < environments.Count; index++)
            {
                P10StageEnvironment2D environment =
                    environments[index];
                if (environment == null
                    || environment.EnvironmentRoot == null
                    || environment.EntryAnchor == null
                    || environment.ExitAnchor == null)
                {
                    continue;
                }

                Transform root = CreateChild(
                    environment.EnvironmentRoot.transform,
                    "P11_DirectionCueRetrofit_PendingRoomRebuild")
                    .transform;
                Vector2 entry = environment.EntryAnchor.position;
                Vector2 exit = environment.ExitAnchor.position;
                Vector2 direction = (exit - entry).normalized;
                SpriteRenderer landmarkRenderer =
                    environment.EnvironmentRoot
                        .GetComponentsInChildren<SpriteRenderer>(true)
                        .Where(renderer =>
                            renderer != null
                            && renderer.sprite != null
                            && !renderer.transform.IsChildOf(root))
                        .OrderByDescending(renderer =>
                            renderer.bounds.size.sqrMagnitude)
                        .FirstOrDefault();
                Transform landmark = landmarkRenderer != null
                    ? landmarkRenderer.transform
                    : environment.ExitAnchor;
                CreateDirectionCue(
                    root,
                    "MainExit_Stardust",
                    P11MapCueKind.MainExit,
                    P11MapRouteKind.Main,
                    environment.ExitAnchor,
                    assets.Star,
                    entry + Vector2.up * 2f,
                    new Color(0.56f, 1f, 0.90f, 1f),
                    true);
                P11MapDirectionCue2D arrow =
                    CreateDirectionCue(
                    root,
                    "ExitEdge_Arrow",
                    P11MapCueKind.ExitEdge,
                    P11MapRouteKind.Main,
                    environment.ExitAnchor,
                    assets.Arrow,
                    entry + direction * 2.4f + Vector2.up,
                    new Color(0.78f, 1f, 0.68f, 1f),
                    true);
                arrow.transform.right = direction;
                CreateStardustFlowTrail(
                    root,
                    entry,
                    direction,
                    assets.Star);
                CreateDirectionCue(
                    root,
                    "Landmark_ExistingTheme",
                    P11MapCueKind.Landmark,
                    P11MapRouteKind.Main,
                    landmark,
                    assets.Star,
                    entry + Vector2.up * 3.5f,
                    new Color(0.66f, 0.82f, 1f, 1f),
                    true);
            }
        }

        private static void CreatePendingHumanGateMarkers(
            Transform persistent)
        {
            GameObject root = CreateChild(
                persistent,
                "P11_HUMAN_GATES_PENDING_REAL_PLAYER_SAMPLES");
            CreateChild(
                root.transform,
                "Letter_And_SunEmber_Use_Inference_Target_80Percent");
            CreateChild(
                root.transform,
                "MemoryEnding_CoreTragedy_Understanding_Target_80Percent");
            CreateChild(
                root.transform,
                "Maru_Basic_Circumstances_Understanding_Pending");
        }

        private static void ValidateSceneOrThrow(
            Scene scene,
            bool requireSavedProductionScene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException(
                    "The P11 production scene is not loaded.");
            }

            if (requireSavedProductionScene
                && scene.path != ProductionScenePath)
            {
                throw new InvalidOperationException(
                    "Validation must run against the saved P11 "
                    + "production scene.");
            }

            P11CommonRegionsCampaignContract contract =
                FindSingle<P11CommonRegionsCampaignContract>();
            if (contract == null)
            {
                throw new InvalidOperationException(
                    "P11 production contract is missing.");
            }

            contract.ValidateOrThrow();
            if (contract.Catalog == null
                || contract.Catalog.Stages.Count != 9)
            {
                throw new InvalidOperationException(
                    "P11 catalog must contain exactly nine stages.");
            }

            string[] catalogIssues =
                contract.Catalog.ValidateCatalog();
            if (catalogIssues.Length > 0)
            {
                throw new InvalidOperationException(
                    string.Join(Environment.NewLine, catalogIssues));
            }

            if (contract.StageNodes.Count != 9)
            {
                throw new InvalidOperationException(
                    "P11 requires exactly nine stage nodes.");
            }

            ValidateProgressiveMechanicsOrThrow(
                contract.StageNodes);

            for (int index = 0;
                 index < contract.StageNodes.Count;
                 index++)
            {
                P11StageNode2D node = contract.StageNodes[index];
                if (node == null
                    || node.Environment == null
                    || node.Environment.EnvironmentRoot == null)
                {
                    throw new InvalidOperationException(
                        $"P11 stage index {index} is incomplete.");
                }

                if (node.Environment.EnvironmentRoot.activeSelf)
                {
                    throw new InvalidOperationException(
                        $"{node.StageId} must be initially inactive.");
                }

                P11MapStageHarness2D harness =
                    node.Environment.MapHarness;
                P11MapValidationReport report =
                    harness != null ? harness.ValidateNow() : null;
                if (report == null || !report.Passed)
                {
                    throw new InvalidOperationException(
                        $"P11 map harness failed for {node.StageId}:"
                        + Environment.NewLine
                        + (report != null
                            ? report.ToString()
                            : "missing harness"));
                }

                ValidatePhysicalPackingOrThrow(
                    node.Environment,
                    harness,
                    node.Definition);
            }

            P10MapDirectionalityAudit2D p10Audit =
                FindSingle<P10MapDirectionalityAudit2D>();
            if (p10Audit == null)
            {
                throw new InvalidOperationException(
                    "The P10 directionality audit marker is missing.");
            }

            p10Audit.RefreshAudit();
            if (!p10Audit.AuditHonestAboutRemainingMapWork)
            {
                throw new InvalidOperationException(
                    "P10 direction cues must be complete while variable "
                    + "room and physical corridor reconstruction remains "
                    + "explicitly pending.");
            }

            GameObject preview = FindSceneObjectByName(
                PreviewRootName);
            if (preview != null)
            {
                throw new InvalidOperationException(
                    "A DontSave preview clone must not be present during "
                    + "production validation.");
            }
        }

        private static void ValidateProgressiveMechanicsOrThrow(
            IReadOnlyList<P11StageNode2D> nodes)
        {
            RoomRegion[] regions =
            {
                RoomRegion.StarPostOffice,
                RoomRegion.SunriseGarden,
                RoomRegion.PolarisObservatory
            };
            for (int index = 0; index < regions.Length; index++)
            {
                RoomRegion region = regions[index];
                P11StageDefinition x1 = nodes
                    .Select(node => node.Definition)
                    .First(definition =>
                        definition.Region == region
                        && definition.StageSlot
                        == P6StageSlot.X1);
                P11StageDefinition x2 = nodes
                    .Select(node => node.Definition)
                    .First(definition =>
                        definition.Region == region
                        && definition.StageSlot
                        == P6StageSlot.X2);
                P11StageDefinition x3 = nodes
                    .Select(node => node.Definition)
                    .First(definition =>
                        definition.Region == region
                        && definition.StageSlot
                        == P6StageSlot.X3);
                P11StageMechanics learned =
                    (x1.Mechanics | x2.Mechanics)
                    & ~P11StageMechanics.BossArena;
                P11StageMechanics bossReuse =
                    x3.Mechanics
                    & ~P11StageMechanics.BossArena;
                if (learned != bossReuse
                    || !HasMechanic(
                        x3,
                        P11StageMechanics.BossArena))
                {
                    throw new InvalidOperationException(
                        $"{region} X-3 must recombine exactly the "
                        + "mechanics introduced in X-1 and X-2, plus "
                        + "its boss arena, with no new device.");
                }
            }
        }

        private static void ValidatePhysicalPackingOrThrow(
            P11StageEnvironment2D environment,
            P11MapStageHarness2D harness,
            P11StageDefinition definition)
        {
            int footprint = 0;
            int physicalShells = 0;
            Rect bounds = environment.GridWorld.WorldBounds;
            var packedRooms = new List<PackedRoomRect>();
            for (int index = 0;
                 index < harness.Rooms.Count;
                 index++)
            {
                P11MapRoom2D room = harness.Rooms[index];
                if (room == null)
                {
                    continue;
                }

                footprint += room.MacroSize.x * room.MacroSize.y;
                bool hasPhysicalShell =
                    room.GetComponentsInChildren<Transform>(true)
                        .Any(item => item.name.StartsWith(
                            "PhysicalRoom_",
                            StringComparison.Ordinal));
                if (hasPhysicalShell)
                {
                    physicalShells++;
                }

                Vector2 center = room.transform.position;
                Vector2 half = new Vector2(
                    room.MacroSize.x * 6f,
                    room.MacroSize.y * 4f);
                packedRooms.Add(
                    new PackedRoomRect(
                        room.RoomId,
                        Rect.MinMaxRect(
                            center.x - half.x,
                            center.y - half.y,
                            center.x + half.x,
                            center.y + half.y)));
                if (center.x - half.x < bounds.xMin - 0.01f
                    || center.x + half.x > bounds.xMax + 0.01f
                    || center.y - half.y < bounds.yMin - 0.01f
                    || center.y + half.y > bounds.yMax + 0.01f)
                {
                    throw new InvalidOperationException(
                        $"{environment.StageId} room {room.RoomId} "
                        + "extends outside its physical grid.");
                }

                P11MapRoomZone[] safeZones =
                {
                    P11MapRoomZone.EntrySafe,
                    P11MapRoomZone.Recovery,
                    P11MapRoomZone.ExitSafe
                };
                for (int zoneIndex = 0;
                     zoneIndex < safeZones.Length;
                     zoneIndex++)
                {
                    if (!room.TryGetZone(
                            safeZones[zoneIndex],
                            out P11MapRoomZoneDefinition zone)
                        || zone.Anchor == null)
                    {
                        throw new InvalidOperationException(
                            $"{environment.StageId} room "
                            + $"{room.RoomId} lacks "
                            + $"{safeZones[zoneIndex]}.");
                    }

                    GridPos cell =
                        environment.GridWorld.WorldToCell(
                            zone.Anchor.position);
                    if (!environment.GridWorld.CanStandAt(
                            cell,
                            new Vector2(0.72f, 0.90f)))
                    {
                        throw new InvalidOperationException(
                            $"{environment.StageId} room "
                            + $"{room.RoomId} zone "
                            + $"{safeZones[zoneIndex]} is metadata-only "
                            + $"at {cell}.");
                    }
                }
            }

            Vector2Int size = environment.GridWorld.Size;
            int capacity =
                Mathf.FloorToInt(size.x / 12f)
                * Mathf.FloorToInt(size.y / 8f);
            if (footprint > capacity)
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} authored room footprint "
                    + $"{footprint} exceeds macro capacity {capacity}.");
            }

            if (physicalShells != harness.Rooms.Count)
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} has metadata-only rooms; "
                    + $"physical={physicalShells}, "
                    + $"authored={harness.Rooms.Count}.");
            }

            GridPos entryCell =
                environment.GridWorld.WorldToCell(
                    environment.EntryAnchor.position);
            GridPos exitCell =
                environment.GridWorld.WorldToCell(
                    environment.ExitAnchor.position);
            if (!environment.GridWorld.CanStandAt(
                    entryCell,
                    new Vector2(0.72f, 0.90f))
                || !environment.GridWorld.CanStandAt(
                    exitCell,
                    new Vector2(0.72f, 0.90f)))
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} entry or exit anchor lacks "
                    + "terrain-backed safe support.");
            }

            const float allowedContactMargin = 0.35f;
            for (int first = 0;
                 first < packedRooms.Count;
                 first++)
            {
                for (int second = first + 1;
                     second < packedRooms.Count;
                     second++)
                {
                    Rect a = packedRooms[first].Rect;
                    Rect b = packedRooms[second].Rect;
                    float overlapX =
                        Mathf.Min(a.xMax, b.xMax)
                        - Mathf.Max(a.xMin, b.xMin);
                    float overlapY =
                        Mathf.Min(a.yMax, b.yMax)
                        - Mathf.Max(a.yMin, b.yMin);
                    if (overlapX > allowedContactMargin
                        && overlapY > allowedContactMargin)
                    {
                        throw new InvalidOperationException(
                            $"{environment.StageId} physical rooms "
                            + $"{packedRooms[first].Id} and "
                            + $"{packedRooms[second].Id} overlap "
                            + $"internally ({overlapX:0.##}x"
                            + $"{overlapY:0.##}).");
                    }
                }
            }

            ValidateDerivedReachabilityOrThrow(
                environment,
                harness);
            ValidateRoleContainmentOrThrow(
                environment,
                harness);
            ValidateRegionalMechanicsOrThrow(
                environment,
                harness,
                definition);
            ValidateMechanicsFlagsOrThrow(
                environment,
                definition);
        }

        private static void ValidateDerivedReachabilityOrThrow(
            P11StageEnvironment2D environment,
            P11MapStageHarness2D harness)
        {
            if (!harness.BaseStartToExitReachable
                || !harness.MaruEscapeReachable)
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} stores a failed derived "
                    + "room-graph reachability result.");
            }

            P8MaruRoomGraph2D graph = environment.MaruRoomGraph;
            P11MapRoom2D start = harness.Rooms
                .FirstOrDefault(room =>
                    room != null
                    && (room.Roles & P11MapRoomRole.Start) != 0);
            P11MapRoom2D exit = harness.Rooms
                .Where(room =>
                    room != null
                    && (room.Roles & P11MapRoomRole.Exit) != 0)
                .OrderByDescending(room => room.MainRouteOrder)
                .FirstOrDefault();
            bool graphRouteReady =
                IsReachableBetweenRooms(graph, start, exit);
            bool maruRouteReady =
                environment.MaruEntryAnchor != null
                && exit != null
                && IsReachableOnRoomGraph(
                    graph,
                    environment.MaruEntryAnchor.position,
                    exit.transform.position);
            bool physicalRouteReady =
                HasCompletePhysicalMainRoute(
                    harness.transform.parent,
                    harness.Rooms);
            if (!graphRouteReady
                || !maruRouteReady
                || !physicalRouteReady)
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} reachability is not backed "
                    + "by the Maru room graph and physical main-route "
                    + "connections.");
            }
        }

        private static void ValidateRoleContainmentOrThrow(
            P11StageEnvironment2D environment,
            P11MapStageHarness2D harness)
        {
            bool isX2 = harness.StageSlot == P11MapStageSlot.X2;
            ValidateComponentsInsideRoleOrThrow<P5MomoShop2D>(
                environment,
                harness,
                P11MapRoomRole.ShopCorridor,
                isX2);
            ValidateComponentsInsideRoleOrThrow<P9StarArchive2D>(
                environment,
                harness,
                P11MapRoomRole.RecordRoom,
                isX2);
            ValidateComponentsInsideRoleOrThrow<
                P8HomecomingStatue2D>(
                environment,
                harness,
                P11MapRoomRole.MaruRoom,
                isX2);
            ValidateComponentsInsideRoleOrThrow<P11NaraeMailbox2D>(
                environment,
                harness,
                P11MapRoomRole.LocalEvent,
                false);
            ValidateComponentsInsideRoleOrThrow<P11YoungCrowEvent2D>(
                environment,
                harness,
                P11MapRoomRole.LocalEvent,
                false);
            ValidateComponentsInsideRoleOrThrow<P11CrowNestEvent2D>(
                environment,
                harness,
                P11MapRoomRole.LocalEvent,
                false);
            ValidateComponentsInsideRoleOrThrow<P11RegionalBoss2D>(
                environment,
                harness,
                P11MapRoomRole.Boss,
                false);
            ValidateComponentsInsideRoleOrThrow<P11MaruFinalBoss2D>(
                environment,
                harness,
                P11MapRoomRole.Boss,
                false);
            ValidateComponentsInsideRoleOrThrow<P11SunEmberPickup2D>(
                environment,
                harness,
                P11MapRoomRole.Boss,
                false);
            ValidateComponentsInsideRoleOrThrow<
                P11EndingPresentation2D>(
                environment,
                harness,
                P11MapRoomRole.Boss,
                false);
            ValidateComponentsInsideRoleOrThrow<
                P11MaruStoryTableau2D>(
                environment,
                harness,
                P11MapRoomRole.Landmark,
                false);
            ValidateComponentsInsideRoleOrThrow<P11ShadowSeed2D>(
                environment,
                harness,
                P11MapRoomRole.Main,
                false);
            ValidateComponentsInsideRoleOrThrow<P11GrowingVine2D>(
                environment,
                harness,
                P11MapRoomRole.Main,
                false);
            ValidateComponentsInsideRoleOrThrow<
                P11OverheatedPlatform2D>(
                environment,
                harness,
                P11MapRoomRole.Main,
                false);
            ValidateComponentsInsideRoleOrThrow<
                P11ConstellationReceiver2D>(
                environment,
                harness,
                P11MapRoomRole.Main,
                false);
            ValidateComponentsInsideRoleOrThrow<
                P11MemoryBellDevice2D>(
                environment,
                harness,
                P11MapRoomRole.Landmark,
                false);
            ValidateComponentsInsideRoleOrThrow<P11MemoryBellInput2D>(
                environment,
                harness,
                P11MapRoomRole.Boss,
                false);
        }

        private static void ValidateComponentsInsideRoleOrThrow<T>(
            P11StageEnvironment2D environment,
            P11MapStageHarness2D harness,
            P11MapRoomRole role,
            bool requireAtLeastOne)
            where T : Component
        {
            T[] components =
                environment.EnvironmentRoot
                    .GetComponentsInChildren<T>(true);
            if (requireAtLeastOne && components.Length == 0)
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} requires "
                    + $"{typeof(T).Name} inside its {role} room.");
            }

            for (int index = 0;
                 index < components.Length;
                 index++)
            {
                Vector2 position = components[index]
                    .transform.position;
                bool contained = harness.Rooms.Any(room =>
                    room != null
                    && room.HasRole(role)
                    && RoomContains(room, position));
                if (!contained)
                {
                    throw new InvalidOperationException(
                        $"{environment.StageId} "
                        + $"{typeof(T).Name} is not physically inside "
                        + $"an authored {role} room.");
                }
            }
        }

        private static bool RoomContains(
            P11MapRoom2D room,
            Vector2 worldPosition)
        {
            Vector2 center = room.transform.position;
            Vector2 half = new Vector2(
                room.MacroSize.x * 6f,
                room.MacroSize.y * 4f);
            const float margin = 0.05f;
            return worldPosition.x >= center.x - half.x + margin
                   && worldPosition.x <= center.x + half.x - margin
                   && worldPosition.y >= center.y - half.y + margin
                   && worldPosition.y <= center.y + half.y - margin;
        }

        private static void ValidateRegionalMechanicsOrThrow(
            P11StageEnvironment2D environment,
            P11MapStageHarness2D harness,
            P11StageDefinition definition)
        {
            switch (harness.Region)
            {
                case P11MapRegion.StarPostOffice:
                    ValidatePostOfficeMechanicsOrThrow(
                        environment,
                        definition);
                    break;
                case P11MapRegion.SunriseGarden:
                    ValidateGardenMechanicsOrThrow(environment);
                    break;
                case P11MapRegion.PolarisObservatory:
                    ValidateObservatoryMechanicsOrThrow(
                        environment,
                        definition);
                    break;
            }
        }

        private static void ValidateMechanicsFlagsOrThrow(
            P11StageEnvironment2D environment,
            P11StageDefinition definition)
        {
            ValidateMechanicFlag<P11ParcelConveyor2D>(
                environment,
                definition,
                P11StageMechanics.ParcelConveyor);
            ValidateMechanicFlag<P11SortingArm2D>(
                environment,
                definition,
                P11StageMechanics.SortingArm);
            ValidateMechanicFlag<P11MailTube2D>(
                environment,
                definition,
                P11StageMechanics.MailTube);
            ValidateMechanicFlag<P11ReturnStamp2D>(
                environment,
                definition,
                P11StageMechanics.ReturnStamp);
            ValidateMechanicFlag<P11InkPool2D>(
                environment,
                definition,
                P11StageMechanics.InkLabel);
            ValidateMechanicFlag<P11ParcelLauncher2D>(
                environment,
                definition,
                P11StageMechanics.ParcelLauncher);
            ValidateMechanicFlag<P11RotatingSunRay2D>(
                environment,
                definition,
                P11StageMechanics.RotatingSunRay);
            ValidateMechanicFlag<P11ShadowSeed2D>(
                environment,
                definition,
                P11StageMechanics.ShadowSeed);
            ValidateMechanicFlag<P11LightReactivePlatform2D>(
                environment,
                definition,
                P11StageMechanics.SunFlowerPlatform);
            ValidateMechanicFlag<P11GrowingVine2D>(
                environment,
                definition,
                P11StageMechanics.GrowingVine);
            ValidateMechanicFlag<P11OverheatedPlatform2D>(
                environment,
                definition,
                P11StageMechanics.OverheatedPlatform);
            ValidateMechanicFlag<P11ReturnField2D>(
                environment,
                definition,
                P11StageMechanics.ReturnField);
            ValidateMechanicFlag<P11InvariantStarBlock2D>(
                environment,
                definition,
                P11StageMechanics.InvariantStarBlock);
            ValidateMechanicFlag<P11OrbitPlatform2D>(
                environment,
                definition,
                P11StageMechanics.OrbitPlatform);
            ValidateMechanicFlag<P11GravityDial2D>(
                environment,
                definition,
                P11StageMechanics.GravityDial);
            ValidateMechanicFlag<P11ConstellationBridge2D>(
                environment,
                definition,
                P11StageMechanics.ConstellationBridge);

            bool memoryBellDeclared =
                (definition.Mechanics
                 & P11StageMechanics.MemoryBell) != 0;
            bool memoryBellPresent =
                environment.EnvironmentRoot
                    .GetComponentInChildren<
                        P11MemoryBellDevice2D>(true) != null
                || environment.EnvironmentRoot
                    .GetComponentInChildren<
                        P11MemoryBellInput2D>(true) != null;
            if (memoryBellDeclared != memoryBellPresent)
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} mechanic flag "
                    + $"{P11StageMechanics.MemoryBell} does not match "
                    + "a generic or final-story bell component.");
            }

            bool bossDeclared =
                (definition.Mechanics
                 & P11StageMechanics.BossArena) != 0;
            bool bossPresent =
                environment.EnvironmentRoot
                    .GetComponentInChildren<P11RegionalBoss2D>(true)
                != null
                || environment.EnvironmentRoot
                    .GetComponentInChildren<P11MaruFinalBoss2D>(true)
                != null;
            if (bossDeclared != bossPresent)
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} mechanic flag "
                    + $"{P11StageMechanics.BossArena} does not match "
                    + "its physical boss component.");
            }
        }

        private static void ValidateMechanicFlag<T>(
            P11StageEnvironment2D environment,
            P11StageDefinition definition,
            P11StageMechanics mechanic)
            where T : Component
        {
            bool declared =
                (definition.Mechanics & mechanic) != 0;
            bool present =
                environment.EnvironmentRoot
                    .GetComponentInChildren<T>(true) != null;
            if (declared != present)
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} mechanic flag {mechanic} "
                    + $"does not match {typeof(T).Name}.");
            }
        }

        private static bool HasMechanic(
            P11StageDefinition definition,
            P11StageMechanics mechanic)
        {
            return definition != null
                   && (definition.Mechanics & mechanic) != 0;
        }

        private static void ValidatePostOfficeMechanicsOrThrow(
            P11StageEnvironment2D environment,
            P11StageDefinition definition)
        {
            P11AddressableParcel2D parcel =
                RequireSingleStageComponent<
                    P11AddressableParcel2D>(environment);
            P11InkPool2D ink =
                RequireSingleStageComponent<P11InkPool2D>(
                    environment);
            if (parcel.Body == null
                || parcel.GetComponent<Collider2D>() == null
                || parcel.Label != P11ParcelLabel.None
                || ink.AppliedLabel != P11ParcelLabel.Bird
                || !HasReadyTrigger(ink))
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} requires a physical "
                    + "unstamped parcel and bird-address ink pool.");
            }

            if (HasMechanic(
                    definition,
                    P11StageMechanics.ParcelLauncher))
            {
                P11ParcelLauncher2D launcher =
                    RequireSingleStageComponent<
                        P11ParcelLauncher2D>(environment);
                if (launcher.AcceptedLabel
                    != P11ParcelLabel.Bird
                    || launcher.LaunchDirection.x <= 0f
                    || !HasReadyTrigger(launcher)
                    || ink.transform.position.x
                    >= launcher.transform.position.x)
                {
                    throw new InvalidOperationException(
                        $"{environment.StageId} parcel launcher must "
                        + "accept bird-address parcels after the ink "
                        + "pool and launch toward the goal.");
                }
            }

            P11SortingGoal2D[] goals =
                environment.EnvironmentRoot
                    .GetComponentsInChildren<P11SortingGoal2D>(true);
            bool needsSortingGoal = HasMechanic(
                definition,
                P11StageMechanics.SortingArm);
            if (goals.Length != (needsSortingGoal ? 1 : 0))
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} sorting goal count does "
                    + "not match its sorting-arm mechanic.");
            }

            if (needsSortingGoal)
            {
                P11SortingGoal2D goal = goals[0];
                P11ParcelLauncher2D launcher =
                    RequireSingleStageComponent<
                        P11ParcelLauncher2D>(environment);
                if (goal.ExpectedLabel != P11ParcelLabel.Bird
                    || !goal.IsConfigured
                    || !HasReadyTrigger(goal)
                    || launcher.transform.position.x
                    >= goal.transform.position.x)
                {
                    throw new InvalidOperationException(
                        $"{environment.StageId} lost-parcel path must "
                        + "finish at a persistent-story bird sorting "
                        + "goal.");
                }
            }

            if (HasMechanic(
                    definition,
                    P11StageMechanics.MailTube))
            {
                P11MailTube2D[] tubes =
                    environment.EnvironmentRoot
                        .GetComponentsInChildren<P11MailTube2D>(true);
                if (tubes.Length != 2
                    || tubes.Any(tube =>
                        !HasReadyTrigger(tube)))
                {
                    throw new InvalidOperationException(
                        $"{environment.StageId} needs one linked "
                        + "physical mail-tube pair.");
                }
            }

            if (HasMechanic(
                    definition,
                    P11StageMechanics.ReturnStamp))
            {
                P11ReturnStamp2D stamp =
                    RequireSingleStageComponent<
                        P11ReturnStamp2D>(environment);
                if (!HasReadyTrigger(stamp))
                {
                    throw new InvalidOperationException(
                        $"{environment.StageId} return stamp lacks "
                        + "its trigger volume.");
                }
            }
        }

        private static void ValidateGardenMechanicsOrThrow(
            P11StageEnvironment2D environment)
        {
            P11RotatingSunRay2D ray =
                RequireSingleStageComponent<P11RotatingSunRay2D>(
                    environment);
            P11LightReactivePlatform2D platform =
                RequireSingleStageComponent<
                    P11LightReactivePlatform2D>(
                    environment);
            if (!ray.Active
                || ray.ReceiverCount < 1
                || !platform.IsConfigured
                || platform.PlatformCollider == null
                || platform.PlatformCollider.isTrigger)
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} rotating sun ray is not "
                    + "wired to a physical light-reactive platform.");
            }

            P11ShadowSeed2D[] seeds =
                environment.EnvironmentRoot
                    .GetComponentsInChildren<P11ShadowSeed2D>(true);
            if (seeds.Any(seed =>
                    seed == null
                    || !seed.IsConfigured
                    || !seed.CarryThrowCompatible
                    || seed.DealsDamage
                    || seed.RotatingRay != ray
                    || seed.FlowerPlatform != platform))
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} shadow seed is not wired "
                    + "as a carry/throw slow object consumed by the "
                    + "rotating light.");
            }

            P11GrowingVine2D[] vines =
                environment.EnvironmentRoot
                    .GetComponentsInChildren<P11GrowingVine2D>(true);
            if (vines.Any(vine =>
                    vine == null
                    || !vine.IsConfigured
                    || vine.WaterReactive == null
                    || vine.GrowthCells
                    < P11GrowingVine2D.MinimumGrowthCells
                    || vine.GrowthCells
                    > P11GrowingVine2D.MaximumGrowthCells))
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} growing vine must be a "
                    + "water-reactive 3-6 cell physical platform.");
            }

            P11OverheatedPlatform2D[] heated =
                environment.EnvironmentRoot
                    .GetComponentsInChildren<
                        P11OverheatedPlatform2D>(true);
            if (heated.Any(item =>
                    item == null
                    || !item.IsConfigured
                    || item.WaterReactive == null
                    || item.SafeToStand))
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} overheated platform must "
                    + "start hot, deal one-heart standing damage, and "
                    + "be coolable by water.");
            }
        }

        private static void ValidateObservatoryMechanicsOrThrow(
            P11StageEnvironment2D environment,
            P11StageDefinition definition)
        {
            P11ConstellationReceiver2D[] receivers =
                environment.EnvironmentRoot
                    .GetComponentsInChildren<
                        P11ConstellationReceiver2D>(true);
            if (HasMechanic(
                    definition,
                    P11StageMechanics.ReturnField))
            {
                P11ReturnField2D field =
                    RequireSingleStageComponent<
                        P11ReturnField2D>(environment);
                Collider2D fieldVolume =
                    field.GetComponent<Collider2D>();
                if (!HasReadyTrigger(field)
                    || !ColliderHasUsableArea(fieldVolume))
                {
                    throw new InvalidOperationException(
                        $"{environment.StageId} return field requires "
                        + "a sized trigger volume.");
                }
            }

            if (HasMechanic(
                    definition,
                    P11StageMechanics.InvariantStarBlock))
            {
                P11InvariantStarBlock2D invariant =
                    RequireSingleStageComponent<
                        P11InvariantStarBlock2D>(environment);
                if (!invariant.IsConfigured
                    || invariant.Body == null
                    || invariant.GetComponent<Collider2D>() == null)
                {
                    throw new InvalidOperationException(
                        $"{environment.StageId} invariant star block "
                        + "must have configured rigidbody and collider.");
                }
            }

            if (HasMechanic(
                    definition,
                    P11StageMechanics.OrbitPlatform))
            {
                P11OrbitPlatform2D orbit =
                    RequireSingleStageComponent<
                        P11OrbitPlatform2D>(environment);
                if (orbit.GetComponent<Collider2D>() == null)
                {
                    throw new InvalidOperationException(
                        $"{environment.StageId} orbit platform lacks "
                        + "its physical collider.");
                }
            }

            if (HasMechanic(
                    definition,
                    P11StageMechanics.GravityDial))
            {
                P11GravityDial2D dial =
                    RequireSingleStageComponent<
                        P11GravityDial2D>(environment);
                Collider2D dialVolume =
                    dial.GetComponent<Collider2D>();
                if (!HasReadyTrigger(dial)
                    || !ColliderHasUsableArea(dialVolume)
                    || dial.Acceleration <= 0f)
                {
                    throw new InvalidOperationException(
                        $"{environment.StageId} gravity dial requires "
                        + "a sized trigger and positive acceleration.");
                }
            }

            if (HasMechanic(
                    definition,
                    P11StageMechanics.ConstellationBridge))
            {
                P11ConstellationBridge2D bridge =
                    RequireSingleStageComponent<
                        P11ConstellationBridge2D>(environment);
                int bridgeColliderCount =
                    bridge.GetComponentsInChildren<Collider2D>(true)
                        .Length;
                bool receiversReady =
                    receivers.Length == bridge.SegmentCount
                    && receivers.All(receiver =>
                        receiver != null
                        && receiver.IsConfigured
                        && receiver.Bridge == bridge
                        && HasReadyTrigger(receiver))
                    && receivers
                        .Select(receiver => receiver.ReceiverIndex)
                        .Distinct()
                        .Count() == bridge.SegmentCount;
                if (bridge.SegmentCount != 4
                    || bridgeColliderCount < bridge.SegmentCount
                    || !receiversReady)
                {
                    throw new InvalidOperationException(
                        $"{environment.StageId} constellation bridge "
                        + "needs four colliders and one configured "
                        + "receiver for every segment.");
                }
            }
            else if (receivers.Length != 0)
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} has constellation "
                    + "receivers without a declared bridge.");
            }

            P11MemoryBellDevice2D[] genericBells =
                environment.EnvironmentRoot
                    .GetComponentsInChildren<
                        P11MemoryBellDevice2D>(true);
            P11MemoryBellInput2D[] finalBellInputs =
                environment.EnvironmentRoot
                    .GetComponentsInChildren<
                        P11MemoryBellInput2D>(true);
            bool genericBellsConfigured =
                genericBells.All(bell =>
                    bell != null
                    && bell.IsConfigured
                    && bell.DirectionTarget
                    == environment.ExitAnchor
                    && HasReadyTrigger(bell));
            bool memoryBellStageReady;
            switch (environment.StageId)
            {
                case P11StageId.PolarisObservatory51:
                    memoryBellStageReady =
                        genericBells.Length == 1
                        && finalBellInputs.Length == 0
                        && genericBellsConfigured;
                    break;
                case P11StageId.PolarisObservatory52:
                    memoryBellStageReady =
                        genericBells.Length == 0
                        && finalBellInputs.Length == 0;
                    break;
                default:
                    memoryBellStageReady =
                        genericBells.Length == 0
                        && finalBellInputs.Length == 4
                        && finalBellInputs.All(HasReadyTrigger);
                    break;
            }

            if (!memoryBellStageReady)
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} memory bell presentation "
                    + "does not match its 5-1 introduction, 5-2 "
                    + "absence, or 5-3 final pattern contract.");
            }
        }

        private static T RequireSingleStageComponent<T>(
            P11StageEnvironment2D environment)
            where T : Component
        {
            T[] components =
                environment.EnvironmentRoot
                    .GetComponentsInChildren<T>(true);
            if (components.Length != 1)
            {
                throw new InvalidOperationException(
                    $"{environment.StageId} requires exactly one "
                    + $"{typeof(T).Name}; found {components.Length}.");
            }

            return components[0];
        }

        private static bool HasReadyTrigger(Component component)
        {
            Collider2D collider =
                component != null
                    ? component.GetComponent<Collider2D>()
                    : null;
            return collider != null
                   && collider.enabled
                   && collider.isTrigger;
        }

        private static bool ColliderHasUsableArea(
            Collider2D collider)
        {
            if (collider is CircleCollider2D circle)
            {
                return circle.radius >= 1f;
            }

            if (collider is BoxCollider2D box)
            {
                return box.size.x >= 1f && box.size.y >= 1f;
            }

            return collider != null;
        }

        private static BuildAssets LoadAssets()
        {
            GameObject bombObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    P5MoonPalaceSliceBuilder.BombPrefabPath);
            return new BuildAssets(
                LoadSprite(SquareSpritePath),
                LoadSprite(StarSpritePath),
                LoadSprite(BirdSpritePath),
                LoadSprite(ArrowSpritePath),
                LoadSprite(PostSkyPath),
                LoadSprite(PostCorePath),
                LoadSprite(PostBoxesPath),
                LoadSprite(PostDoorPath),
                LoadSprite(GardenBackgroundPath),
                LoadSprite(GardenSunPath),
                LoadSprite(GardenMonolithPath),
                LoadSprite(GardenFlowersPath),
                LoadSprite(ObservatoryBackgroundPath),
                LoadSprite(ObservatoryCrystalsPath),
                LoadSprite(ObservatoryTreesPath),
                LoadSprite(ObservatorySkyPath),
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
                    "P11 Abandoned Station, Spring Forest, Crystal "
                    + "Dungeon/Mount, arrow, tile, or bomb assets are "
                    + "incomplete.");
            }
        }

        private static P11StageEnvironment2D FindP11Environment(
            P11StageId id)
        {
            return UnityEngine.Object.FindObjectsByType<
                    P11StageEnvironment2D>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(item => item.StageId == id);
        }

        private static P11StageNode2D FindNode(
            IEnumerable<P11StageNode2D> nodes,
            P11StageId id)
        {
            return nodes.First(item => item.StageId == id);
        }

        private static T FindSingle<T>() where T : Component
        {
            T[] found =
                UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            if (typeof(T) == typeof(Camera))
            {
                T main = found.FirstOrDefault(item =>
                    ((Camera)(Component)item)
                        .CompareTag("MainCamera"));
                if (main != null)
                {
                    return main;
                }
            }

            return found.Length == 1 ? found[0] : null;
        }

        private static Transform FindTransform(
            Transform root,
            string name)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == name);
        }

        private static void DestroyPreviewIfPresent()
        {
            GameObject preview = FindSceneObjectByName(
                PreviewRootName);
            if (preview != null)
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static GameObject FindSceneObjectByName(string name)
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(item =>
                    item != null
                    && item.scene.IsValid()
                    && item.name == name);
        }

        private static GameObject CreateChild(
            Transform parent,
            string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
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

        private readonly struct PackedRoomRect
        {
            public readonly string Id;
            public readonly Rect Rect;

            public PackedRoomRect(string id, Rect rect)
            {
                Id = id;
                Rect = rect;
            }
        }

        private readonly struct StageLayout
        {
            public readonly Vector2Int Size;
            public readonly Vector2Int Entry;
            public readonly Vector2Int Exit;

            public StageLayout(
                Vector2Int size,
                Vector2Int entry,
                Vector2Int exit)
            {
                Size = size;
                Entry = entry;
                Exit = exit;
            }
        }

        private readonly struct RoomAuthoring
        {
            public readonly string Id;
            public readonly string CoreSentence;
            public readonly Vector2Int MacroSize;
            public readonly P11MapRoomRole Roles;
            public readonly Vector2 Center;
            public readonly bool OnMainRoute;
            public readonly int MainOrder;
            public readonly int MechanicsCount;
            public readonly bool AddsNewMechanic;

            public RoomAuthoring(
                string id,
                string coreSentence,
                Vector2Int macroSize,
                P11MapRoomRole roles,
                Vector2 center,
                bool onMainRoute,
                int mainOrder,
                int mechanicsCount,
                bool addsNewMechanic)
            {
                Id = id;
                CoreSentence = coreSentence;
                MacroSize = macroSize;
                Roles = roles;
                Center = center;
                OnMainRoute = onMainRoute;
                MainOrder = mainOrder;
                MechanicsCount = mechanicsCount;
                AddsNewMechanic = addsNewMechanic;
            }
        }

        private readonly struct BuildAssets
        {
            public readonly Sprite Square;
            public readonly Sprite Star;
            public readonly Sprite Bird;
            public readonly Sprite Arrow;
            public readonly Sprite PostSky;
            public readonly Sprite PostCore;
            public readonly Sprite PostBoxes;
            public readonly Sprite PostDoor;
            public readonly Sprite GardenBackground;
            public readonly Sprite GardenSun;
            public readonly Sprite GardenMonolith;
            public readonly Sprite GardenFlowers;
            public readonly Sprite ObservatoryBackground;
            public readonly Sprite ObservatoryCrystals;
            public readonly Sprite ObservatoryTrees;
            public readonly Sprite ObservatorySky;
            public readonly TileBase ReinforcedTile;
            public readonly TileBase SoftSoilTile;
            public readonly TileBase OneWayTile;
            public readonly TileDefinition[] TileDefinitions;
            public readonly Bomb2D BombPrefab;

            public BuildAssets(
                Sprite square,
                Sprite star,
                Sprite bird,
                Sprite arrow,
                Sprite postSky,
                Sprite postCore,
                Sprite postBoxes,
                Sprite postDoor,
                Sprite gardenBackground,
                Sprite gardenSun,
                Sprite gardenMonolith,
                Sprite gardenFlowers,
                Sprite observatoryBackground,
                Sprite observatoryCrystals,
                Sprite observatoryTrees,
                Sprite observatorySky,
                TileBase reinforcedTile,
                TileBase softSoilTile,
                TileBase oneWayTile,
                TileDefinition[] tileDefinitions,
                Bomb2D bombPrefab)
            {
                Square = square;
                Star = star;
                Bird = bird;
                Arrow = arrow;
                PostSky = postSky;
                PostCore = postCore;
                PostBoxes = postBoxes;
                PostDoor = postDoor;
                GardenBackground = gardenBackground;
                GardenSun = gardenSun;
                GardenMonolith = gardenMonolith;
                GardenFlowers = gardenFlowers;
                ObservatoryBackground = observatoryBackground;
                ObservatoryCrystals = observatoryCrystals;
                ObservatoryTrees = observatoryTrees;
                ObservatorySky = observatorySky;
                ReinforcedTile = reinforcedTile;
                SoftSoilTile = softSoilTile;
                OneWayTile = oneWayTile;
                TileDefinitions =
                    tileDefinitions ?? Array.Empty<TileDefinition>();
                BombPrefab = bombPrefab;
            }

            public bool IsComplete =>
                Square != null
                && Star != null
                && Bird != null
                && Arrow != null
                && PostSky != null
                && PostCore != null
                && PostBoxes != null
                && PostDoor != null
                && GardenBackground != null
                && GardenSun != null
                && GardenMonolith != null
                && GardenFlowers != null
                && ObservatoryBackground != null
                && ObservatoryCrystals != null
                && ObservatoryTrees != null
                && ObservatorySky != null
                && ReinforcedTile != null
                && SoftSoilTile != null
                && OneWayTile != null
                && TileDefinitions.Length >= 2
                && TileDefinitions.All(item => item != null)
                && BombPrefab != null;
        }
    }
}

#endif
