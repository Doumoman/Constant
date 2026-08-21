#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StarNight.Campaign.P10;
using StarNight.Campaign.P11;
using StarNight.Campaign.P12;
using StarNight.Debugging;
using StarNight.Explosions;
using StarNight.Grid;
using StarNight.Maru.P8;
using StarNight.Objects;
using StarNight.Player;
using StarNight.Stages.P5;
using StarNight.Tiles;
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
    public static class P12StarlessSeaChallengeBuilder
    {
        public const string ProductionScenePath =
            "Assets/StarNight/Scenes/Game/"
            + "P12_StarlessSea_DawnVoyage.unity";
        public const string CatalogPath =
            "Assets/StarNight/Data/P12/"
            + "P12_StarlessSeaChallengeCatalog.asset";

        private const string ReinforcedTilePath =
            "Assets/StarNight/Data/P4/RoomTiles/"
            + "P4_MoonReinforced.asset";
        private const string ReinforcedDefinitionPath =
            "Assets/StarNight/Data/P5/Tiles/"
            + "P5_MoonReinforced_Definition.asset";
        private const string SoftSoilDefinitionPath =
            "Assets/StarNight/Data/P5/Tiles/"
            + "P5_MoonSoftSoil_Definition.asset";
        private const string SquareSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/"
            + "Square.png";

        [MenuItem("StarNight/P12/Rebuild Starless Sea Challenge")]
        public static void Rebuild()
        {
            P11CommonRegionsCampaignBuilder.Rebuild();
            EnsureFolder(
                Path.GetDirectoryName(ProductionScenePath)
                    ?.Replace('\\', '/'));
            EnsureFolder(
                Path.GetDirectoryName(CatalogPath)
                    ?.Replace('\\', '/'));

            Scene p11Scene = SceneManager.GetActiveScene();
            if (p11Scene.path
                != P11CommonRegionsCampaignBuilder.ProductionScenePath)
            {
                p11Scene = EditorSceneManager.OpenScene(
                    P11CommonRegionsCampaignBuilder.ProductionScenePath,
                    OpenSceneMode.Single);
            }

            if (!EditorSceneManager.SaveScene(
                    p11Scene,
                    ProductionScenePath,
                    true))
            {
                throw new InvalidOperationException(
                    "Failed to copy the cumulative P11 production scene.");
            }

            Scene scene = EditorSceneManager.OpenScene(
                ProductionScenePath,
                OpenSceneMode.Single);
            P12ChallengeCatalog catalog = RebuildCatalog();
            BuildAssets assets = LoadAssets();
            ValidateAssets(assets);
            BuildProductionChallenge(scene, catalog, assets);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    ProductionScenePath))
            {
                throw new InvalidOperationException(
                    "Failed to save the P12 production scene.");
            }

            AddSceneToBuildSettings(ProductionScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[StarNight P12] Starless Sea challenge rebuilt: "
                + "twelve staged environments over four segments, "
                + "known mechanics only, record-only rewards.");
        }

        [MenuItem("StarNight/P12/Validate Starless Sea Challenge")]
        public static void Validate()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ProductionScenePath)
            {
                scene = EditorSceneManager.OpenScene(
                    ProductionScenePath,
                    OpenSceneMode.Single);
            }

            P12StarlessSeaChallengeContract[] contracts =
                UnityEngine.Object.FindObjectsByType<
                    P12StarlessSeaChallengeContract>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            if (contracts.Length != 1)
            {
                throw new InvalidOperationException(
                    "P12 production requires exactly one challenge "
                    + "contract.");
            }

            contracts[0].ValidateOrThrow();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(
                "[StarNight P12] Structural validation PASS. "
                + "Skilled completion (5-15%) and unfair-death gates "
                + "remain human playtests.");
        }

        private static void BuildProductionChallenge(
            Scene scene,
            P12ChallengeCatalog catalog,
            BuildAssets assets)
        {
            P11CommonRegionsCampaignContract p11Contract =
                FindSingle<P11CommonRegionsCampaignContract>();
            P11CampaignDirector2D p11Director =
                FindSingle<P11CampaignDirector2D>();
            P11StoryState2D story = FindSingle<P11StoryState2D>();
            PlayerMotor2D player = FindSingle<PlayerMotor2D>();
            Camera camera = FindSingle<Camera>();
            P5RunState2D runState = FindSingle<P5RunState2D>();
            P8MaruStageController2D maru =
                FindSingle<P8MaruStageController2D>();
            if (p11Contract == null
                || p11Director == null
                || story == null
                || player == null
                || camera == null
                || runState == null
                || maru == null)
            {
                throw new InvalidOperationException(
                    "The cumulative P11 persistent core is incomplete.");
            }

            GameObject root = p11Contract.transform.root.gameObject;
            root.name = "P12_StarlessSea_DawnVoyage";
            Transform persistent =
                FindTransform(root.transform, "PersistentCampaignCore");
            if (persistent == null)
            {
                throw new InvalidOperationException(
                    "The persistent campaign core is missing.");
            }

            GameObject systems = CreateChild(
                persistent,
                "P12Systems_StarlessSeaChallenge");
            systems.AddComponent<P12AccessibilityOptions2D>();
            P12ChallengeTelemetry2D telemetry =
                systems.AddComponent<P12ChallengeTelemetry2D>();
            telemetry.ConfigureMinimumSkilledSamples(
                P12ChallengeTelemetry2D.DefaultMinimumSkilledSamples);
            systems.AddComponent<P12PerformanceProbe2D>();
            P12ChallengeDirector2D director =
                systems.AddComponent<P12ChallengeDirector2D>();
            director.Configure(catalog, p11Director, story, telemetry);
            P12StageFlowController2D flow =
                systems.AddComponent<P12StageFlowController2D>();

            GameObject environmentsRoot = CreateChild(
                root.transform,
                "P12StageEnvironments_12");
            GameObject registryRoot = CreateChild(
                root.transform,
                "P12StageRegistry_12");
            var environments = new List<P12StageEnvironment2D>(12);
            for (int index = 0; index < catalog.Stages.Count; index++)
            {
                environments.Add(
                    CreateStageEnvironment(
                        environmentsRoot.transform,
                        catalog.Stages[index],
                        assets,
                        player,
                        runState,
                        maru));
            }

            var nodes = new P12StageNode2D[environments.Count];
            for (int index = 0; index < environments.Count; index++)
            {
                P12StageEnvironment2D environment =
                    environments[index];
                P12StageDefinition definition =
                    catalog.Find(environment.StageId);
                GameObject nodeObject = CreateChild(
                    registryRoot.transform,
                    $"Node_{definition.StageId}");
                P12StageNode2D node =
                    nodeObject.AddComponent<P12StageNode2D>();
                node.Configure(definition, director, environment);
                nodes[index] = node;
            }

            GameObject entryObject = CreateChild(
                root.transform,
                "P12ChallengeEntry_StarlessSea");
            entryObject.transform.position = new Vector3(4f, 3f, 0f);
            P12ChallengeEntry2D entry =
                entryObject.AddComponent<P12ChallengeEntry2D>();
            entry.Configure(director, flow);

            flow.Configure(
                director,
                nodes,
                player.transform,
                camera,
                assets.BombPrefab,
                maru,
                entry);
            for (int index = 0; index < nodes.Length; index++)
            {
                CreateStageExit(nodes[index], flow);
            }

            GameObject charmObject = CreateChild(
                systems.transform,
                "P12SmallBellCharm");
            P12SmallBellCharm2D charm =
                charmObject.AddComponent<P12SmallBellCharm2D>();
            charm.Configure(maru.Timeline, director);

            CreateEpilogue(
                nodes.First(node =>
                    node.StageId == P12StageId.StarlessSea12),
                director,
                assets);
            CreatePendingHumanGateMarkers(persistent);

            P12StarlessSeaChallengeContract contract =
                systems.AddComponent<P12StarlessSeaChallengeContract>();
            var statues = new List<P8HomecomingStatue2D>();
            var launchers = new List<P11ParcelLauncher2D>();
            for (int index = 0; index < environments.Count; index++)
            {
                statues.AddRange(
                    environments[index]
                        .GetComponentsInChildren<P8HomecomingStatue2D>(true));
                launchers.AddRange(
                    environments[index]
                        .GetComponentsInChildren<P11ParcelLauncher2D>(true));
            }

            contract.Configure(
                catalog,
                director,
                flow,
                nodes,
                telemetry,
                statues.ToArray(),
                launchers.ToArray());

            for (int index = 0; index < environments.Count; index++)
            {
                environments[index].SetEnvironmentActive(false);
            }

            Physics2D.SyncTransforms();
            if (!contract.RefreshValidation())
            {
                throw new InvalidOperationException(
                    "P12 production rebuild failed validation:"
                    + Environment.NewLine
                    + contract.LastValidation);
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static P12ChallengeCatalog RebuildCatalog()
        {
            EnsureFolder(
                Path.GetDirectoryName(CatalogPath)
                    ?.Replace('\\', '/'));
            P12ChallengeCatalog catalog =
                AssetDatabase.LoadAssetAtPath<P12ChallengeCatalog>(
                    CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject
                    .CreateInstance<P12ChallengeCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.Configure(
                P12ChallengeCatalogDefaults.CreateStandardDefinitions());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static P12StageEnvironment2D CreateStageEnvironment(
            Transform parent,
            P12StageDefinition definition,
            BuildAssets assets,
            PlayerMotor2D player,
            P5RunState2D runState,
            P8MaruStageController2D maru)
        {
            int rooms = definition.RoomCountMin;
            int width = rooms * 6 + 8;
            int height = 18;
            var entryCellPosition = new Vector2Int(3, 2);
            var exitCellPosition = new Vector2Int(width - 4, 2);

            GameObject envRoot = CreateChild(
                parent,
                $"Environment_{definition.StageId}"
                + $"_{definition.DisplayName}");
            envRoot.SetActive(true);

            GameObject gridObject = CreateChild(
                envRoot.transform,
                "Grid");
            UnityEngine.Grid grid =
                gridObject.AddComponent<UnityEngine.Grid>();
            grid.cellSize = Vector3.one;
            Tilemap terrain = CreateTilemapLayer(
                gridObject.transform,
                "Terrain",
                0,
                true,
                out TilemapCollider2D terrainCollider,
                out CompositeCollider2D composite);
            Tilemap decoration = CreateTilemapLayer(
                gridObject.transform,
                "Decoration",
                1,
                false,
                out _,
                out _);
            Tilemap hazard = CreateTilemapLayer(
                gridObject.transform,
                "Hazard",
                2,
                false,
                out _,
                out _);

            SetRect(terrain, assets.ReinforcedTile, 0, 0, width, 2);
            SetRect(terrain, assets.ReinforcedTile, 0, 0, 2, 9);
            SetRect(
                terrain,
                assets.ReinforcedTile,
                width - 2,
                0,
                2,
                9);
            for (int room = 0; room < rooms; room++)
            {
                SetRect(
                    terrain,
                    assets.ReinforcedTile,
                    5 + room * 6,
                    5,
                    3,
                    1);
            }

            terrain.CompressBounds();
            terrain.RefreshAllTiles();
            terrainCollider.ProcessTilemapChanges();
            composite.GenerateGeometry();

            GridWorld world = gridObject.AddComponent<GridWorld>();
            world.Configure(
                grid,
                terrain,
                hazard,
                Vector2Int.zero,
                new Vector2Int(width, height));

            Transform entry = CreateAnchor(
                envRoot.transform,
                "StageEntry",
                CellWorld(entryCellPosition));
            Transform exit = CreateAnchor(
                envRoot.transform,
                "StageExit",
                CellWorld(exitCellPosition));
            Transform cameraAnchor = CreateAnchor(
                envRoot.transform,
                "CameraAnchor",
                new Vector2(width * 0.5f, height * 0.5f));
            Transform maruEntry = CreateAnchor(
                envRoot.transform,
                "MaruEntryAnchor",
                (Vector2)exit.position + Vector2.left * 1.5f);

            GameObject systems = CreateChild(
                envRoot.transform,
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

            GridPos entryCell = world.WorldToCell(entry.position);
            GridPos exitCell = world.WorldToCell(exit.position);
            mutation.Configure(
                world,
                terrain,
                decoration,
                terrainCollider,
                composite,
                player.GetComponent<Collider2D>(),
                assets.TileDefinitions,
                entryCell,
                exitCell,
                ProtectedExitCells(exitCellPosition));

            P8MaruRoomGraph2D roomGraph = BuildMaruRoomGraph(
                systems.transform,
                rooms);
            P8ReturnPile2D returnPile = BuildReturnPile(
                systems.transform,
                entry.position + new Vector3(1.2f, 0.4f, 0f));

            P12StageEnvironment2D environment =
                envRoot.AddComponent<P12StageEnvironment2D>();
            environment.Configure(
                definition.StageId,
                envRoot,
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
                returnPile);

            P12ReturnCrystalConverter2D converter =
                systems.AddComponent<P12ReturnCrystalConverter2D>();
            converter.Configure(maru.Pursuer, entry, runState);

            CreateStageMechanics(
                environment,
                definition,
                assets,
                maru,
                runState,
                player.transform);
            CreateStageMarkers(envRoot.transform, definition);
            Physics2D.SyncTransforms();
            return environment;
        }

        private static void CreateStageMechanics(
            P12StageEnvironment2D environment,
            P12StageDefinition definition,
            BuildAssets assets,
            P8MaruStageController2D maru,
            P5RunState2D runState,
            Transform player)
        {
            Transform parent = CreateChild(
                environment.EnvironmentRoot.transform,
                "RegionalPhysicalMechanics").transform;
            GridWorld world = environment.GridWorld;
            Vector2 center = environment.CameraAnchor.position;
            float slotX = 8f;
            Vector2 Next()
            {
                var slot = new Vector2(slotX, 6.2f);
                slotX += 5f;
                return slot;
            }

            P12StageMechanics mechanics = definition.Mechanics;
            bool Has(P12StageMechanics flag) =>
                (mechanics & flag) == flag;

            if (Has(P12StageMechanics.Crosswind))
            {
                CreateForceZone(
                    parent,
                    "Crosswind_ForceZone",
                    Next(),
                    Vector2.right,
                    6f,
                    true,
                    1.6f,
                    new Color(0.72f, 0.86f, 1f, 0.35f),
                    assets);
            }

            if (Has(P12StageMechanics.Riptide))
            {
                CreateForceZone(
                    parent,
                    "Riptide_ForceZone",
                    Next(),
                    Vector2.right,
                    9f,
                    false,
                    1f,
                    new Color(0.28f, 0.62f, 0.94f, 0.45f),
                    assets);
            }

            if (Has(P12StageMechanics.SwayingPlatform))
            {
                GameObject platform = CreateSpritePart(
                    parent,
                    "SwayingPlatform",
                    assets.Square,
                    Next(),
                    new Vector2(2.6f, 0.5f),
                    new Color(0.92f, 0.75f, 1f, 1f),
                    18);
                platform.AddComponent<BoxCollider2D>();
                Rigidbody2D body =
                    platform.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                P10SwayingPlatform2D swaying =
                    platform.AddComponent<P10SwayingPlatform2D>();
                swaying.Configure(Vector2.right, 2.2f, 3.2f);
            }

            if (Has(P12StageMechanics.TimedPlatform))
            {
                GameObject platform = CreateSpritePart(
                    parent,
                    "TimedPlatform",
                    assets.Square,
                    Next(),
                    new Vector2(2.8f, 0.35f),
                    new Color(0.82f, 0.66f, 1f, 0.95f),
                    18);
                platform.AddComponent<BoxCollider2D>();
                P10TimedPlatform2D timed =
                    platform.AddComponent<P10TimedPlatform2D>();
                timed.Configure(2.6f, 1.25f, 0f);
            }

            if (Has(P12StageMechanics.Floodgate))
            {
                GameObject gate = CreateSpritePart(
                    parent,
                    "Floodgate",
                    assets.Square,
                    Next(),
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

            if (Has(P12StageMechanics.FallingObject))
            {
                Vector2 slot = Next();
                GameObject faller = CreateSpritePart(
                    parent,
                    "FallingPestle",
                    assets.Square,
                    new Vector2(slot.x, 10f),
                    new Vector2(0.9f, 0.9f),
                    new Color(0.78f, 0.70f, 0.58f, 1f),
                    20);
                faller.AddComponent<Rigidbody2D>();
                faller.AddComponent<BoxCollider2D>();
                FallingObject2D falling =
                    faller.AddComponent<FallingObject2D>();
                falling.Configure(
                    world,
                    environment.MutationService);
            }

            if (Has(P12StageMechanics.ParcelConveyor))
            {
                GameObject conveyor = CreateSpritePart(
                    parent,
                    "ParcelConveyor",
                    assets.Square,
                    Next(),
                    new Vector2(4.5f, 0.7f),
                    new Color(0.44f, 0.70f, 0.82f, 1f),
                    35);
                BoxCollider2D trigger =
                    conveyor.AddComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                trigger.size = new Vector2(4.5f, 1f);
                P11ParcelConveyor2D runtime =
                    conveyor.AddComponent<P11ParcelConveyor2D>();
                runtime.Configure(
                    Vector2.right,
                    2.4f,
                    conveyor.GetComponent<SpriteRenderer>());
            }

            if (Has(P12StageMechanics.ParcelLauncher))
            {
                GameObject launcherObject = CreateSpritePart(
                    parent,
                    "ParcelLauncher_CameraVisible",
                    assets.Square,
                    new Vector2(center.x - 3f, 6.2f),
                    new Vector2(1.4f, 0.7f),
                    new Color(0.74f, 0.92f, 1f, 1f),
                    44);
                BoxCollider2D launcherTrigger =
                    launcherObject.AddComponent<BoxCollider2D>();
                launcherTrigger.isTrigger = true;
                launcherTrigger.size = new Vector2(1.8f, 1.4f);
                P11ParcelLauncher2D launcher =
                    launcherObject.AddComponent<P11ParcelLauncher2D>();
                launcher.Configure(
                    Vector2.right,
                    8f,
                    P11ParcelLabel.Star,
                    launcherObject.GetComponent<SpriteRenderer>());
            }

            if (Has(P12StageMechanics.ReturnStamp))
            {
                GameObject stamp = CreateSpritePart(
                    parent,
                    "ReturnStamp",
                    assets.Square,
                    Next(),
                    new Vector2(2f, 0.65f),
                    new Color(0.84f, 0.18f, 0.34f, 0.65f),
                    40);
                BoxCollider2D stampTrigger =
                    stamp.AddComponent<BoxCollider2D>();
                stampTrigger.isTrigger = true;
                Transform safe = CreateAnchor(
                    stamp.transform,
                    "SafeReturnAnchor",
                    new Vector2(-3f, 1f),
                    true);
                P11ReturnStamp2D returnStamp =
                    stamp.AddComponent<P11ReturnStamp2D>();
                returnStamp.Configure(
                    safe,
                    stamp.GetComponent<SpriteRenderer>());
            }

            if (Has(P12StageMechanics.ReturnField))
            {
                GameObject returnField = CreateSpritePart(
                    parent,
                    "ReturnField",
                    assets.Square,
                    Next(),
                    new Vector2(2.2f, 2.2f),
                    new Color(0.52f, 0.72f, 1f, 0.4f),
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
            }

            P11RotatingSunRay2D sunRay = null;
            if (Has(P12StageMechanics.RotatingSunRay))
            {
                Vector2 slot = Next();
                GameObject platform = CreateSpritePart(
                    parent,
                    "LightReactivePlatform",
                    assets.Square,
                    slot + new Vector2(2.2f, -1f),
                    new Vector2(2.5f, 0.5f),
                    new Color(1f, 0.92f, 0.62f, 1f),
                    43);
                BoxCollider2D platformCollider =
                    platform.AddComponent<BoxCollider2D>();
                platformCollider.size = new Vector2(2.5f, 0.5f);
                P11LightReactivePlatform2D reactive =
                    platform.AddComponent<
                        P11LightReactivePlatform2D>();
                reactive.Configure(
                    platformCollider,
                    platform.GetComponent<SpriteRenderer>(),
                    true);
                GameObject ray = CreateSpritePart(
                    parent,
                    "RotatingSunRay",
                    assets.Square,
                    slot,
                    new Vector2(3.8f, 0.28f),
                    new Color(1f, 0.78f, 0.28f, 0.72f),
                    47);
                sunRay = ray.AddComponent<P11RotatingSunRay2D>();
                sunRay.Configure(
                    14f,
                    ray.GetComponent<SpriteRenderer>(),
                    new[] { reactive },
                    22f,
                    0.8f);
            }

            if (Has(P12StageMechanics.GrowingVine))
            {
                GameObject vine = CreateChild(
                    parent,
                    "GrowingVine_WaterThenLight");
                vine.transform.localPosition = Next();
                BoxCollider2D vineCollider =
                    vine.AddComponent<BoxCollider2D>();
                vineCollider.size = new Vector2(1.1f, 1f);
                SpriteRenderer dryVine = CreateSpritePart(
                        vine.transform,
                        "DrySeed",
                        assets.Square,
                        Vector2.zero,
                        new Vector2(0.55f, 0.45f),
                        new Color(0.52f, 0.36f, 0.22f, 1f),
                        47)
                    .GetComponent<SpriteRenderer>();
                SpriteRenderer grownVine = CreateSpritePart(
                        vine.transform,
                        "GrownVine",
                        assets.Square,
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
                    world,
                    world.WorldToCell(vine.transform.position),
                    vineCollider,
                    dryVine,
                    grownVine,
                    sunRay,
                    4,
                    1.75f);
            }

            if (Has(P12StageMechanics.OverheatedPlatform))
            {
                GameObject heatPlatform = CreateSpritePart(
                    parent,
                    "OverheatedPlatform_WaterSafe",
                    assets.Square,
                    Next(),
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
                    world,
                    world.WorldToCell(
                        heatPlatform.transform.position),
                    supportCollider,
                    heatTrigger,
                    heatPlatform.GetComponent<SpriteRenderer>(),
                    false);
            }

            if (Has(P12StageMechanics.OrbitPlatform))
            {
                Vector2 slot = Next();
                Transform orbitCenter = CreateAnchor(
                    parent,
                    "OrbitCenter",
                    slot + Vector2.up * 2f);
                GameObject orbit = CreateSpritePart(
                    parent,
                    "OrbitPlatform",
                    assets.Square,
                    (Vector2)orbitCenter.position
                    + Vector2.right * 3f,
                    new Vector2(1.4f, 0.5f),
                    new Color(0.68f, 0.92f, 1f, 1f),
                    45);
                orbit.AddComponent<BoxCollider2D>();
                P11OrbitPlatform2D orbitPlatform =
                    orbit.AddComponent<P11OrbitPlatform2D>();
                orbitPlatform.Configure(orbitCenter, 3f, 7f, 0f);
            }

            if (Has(P12StageMechanics.GravityDial))
            {
                GameObject dialObject = CreateSpritePart(
                    parent,
                    "GravityDial_Cardinal",
                    assets.Square,
                    Next(),
                    new Vector2(1.1f, 1.1f),
                    new Color(0.76f, 0.88f, 1f, 1f),
                    47);
                CircleCollider2D dialVolume =
                    dialObject.AddComponent<CircleCollider2D>();
                dialVolume.isTrigger = true;
                dialVolume.radius = 2.2f;
                P11GravityDial2D gravityDial =
                    dialObject.AddComponent<P11GravityDial2D>();
                gravityDial.Configure(
                    Vector2.down,
                    9.81f,
                    dialObject.transform,
                    2.2f);
            }

            if (Has(P12StageMechanics.ConstellationBridge))
            {
                Vector2 slot = Next();
                GameObject bridgeRoot = CreateChild(
                    parent,
                    "ConstellationBridge");
                bridgeRoot.transform.localPosition =
                    slot + Vector2.up * 3f;
                var segments = new GameObject[4];
                for (int index = 0; index < segments.Length; index++)
                {
                    segments[index] = CreateSpritePart(
                        bridgeRoot.transform,
                        $"StarSegment_{index}",
                        assets.Square,
                        new Vector2(index * 1.25f, 0f),
                        Vector2.one * 0.42f,
                        new Color(0.62f, 0.86f, 1f, 0.85f),
                        46);
                    BoxCollider2D segmentCollider =
                        segments[index]
                            .AddComponent<BoxCollider2D>();
                    segmentCollider.size = new Vector2(2.8f, 0.65f);
                }

                P11ConstellationBridge2D bridge =
                    bridgeRoot.AddComponent<
                        P11ConstellationBridge2D>();
                bridge.Configure(segments);
                for (int index = 0; index < segments.Length; index++)
                {
                    GameObject receiverObject = CreateSpritePart(
                        parent,
                        $"ConstellationReceiver_{index}",
                        assets.Square,
                        slot + new Vector2(
                            (index - 1.5f) * 1.8f,
                            -1.5f),
                        Vector2.one * 0.62f,
                        new Color(0.28f, 0.38f, 0.58f, 0.82f),
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

            if (Has(P12StageMechanics.InvariantStarBlock))
            {
                Vector2 slot = Next();
                Transform invariantAnchor = CreateAnchor(
                    parent,
                    "InvariantStarBlock_Anchor",
                    slot);
                GameObject invariantObject = CreateSpritePart(
                    parent,
                    "InvariantStarBlock_ReturnFieldImmune",
                    assets.Square,
                    slot,
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
                invariant.Configure(invariantBody, invariantAnchor);
            }

            if (definition.Segment == P12ChallengeSegment.ThirdSea)
            {
                CreateHomecomingStatue(
                    parent,
                    definition.StageId,
                    Next(),
                    maru.Timeline,
                    assets);
            }

            if (Has(P12StageMechanics.HomecomingStatueEconomy))
            {
                Vector2 slot = Next();
                for (int index = 0; index < 2; index++)
                {
                    GameObject gold = CreateSpritePart(
                        parent,
                        $"BigGold_ForReturnCrystal_{index}",
                        assets.Square,
                        slot + new Vector2(index * 1.4f, -1.8f),
                        Vector2.one * 0.5f,
                        new Color(1f, 0.84f, 0.28f, 1f),
                        45);
                    CircleCollider2D goldTrigger =
                        gold.AddComponent<CircleCollider2D>();
                    goldTrigger.isTrigger = true;
                    goldTrigger.radius = 0.4f;
                    P5GoldPickup2D pickup =
                        gold.AddComponent<P5GoldPickup2D>();
                    pickup.Configure(
                        runState,
                        P5GoldPickup2D.BigGoldValue,
                        player,
                        gold.GetComponent<SpriteRenderer>());
                }
            }
        }

        private static P8HomecomingStatue2D CreateHomecomingStatue(
            Transform parent,
            P12StageId stageId,
            Vector2 position,
            P8MaruTimeline2D timeline,
            BuildAssets assets)
        {
            GameObject statue = CreateChild(
                parent,
                $"HomecomingStatue_{stageId}_1x2");
            statue.transform.localPosition = position;
            Rigidbody2D body = statue.AddComponent<Rigidbody2D>();
            body.gravityScale = 1f;
            body.freezeRotation = true;
            BoxCollider2D statueCollider =
                statue.AddComponent<BoxCollider2D>();
            statueCollider.size = new Vector2(0.82f, 1.72f);
            GameObject intact = CreateSpritePart(
                statue.transform,
                "Intact",
                assets.Square,
                Vector2.zero,
                new Vector2(0.95f, 1.9f),
                new Color(0.72f, 0.78f, 0.92f, 1f),
                108);
            GameObject cracked = CreateSpritePart(
                statue.transform,
                "Cracked",
                assets.Square,
                Vector2.zero,
                new Vector2(1.05f, 1.8f),
                new Color(0.66f, 0.54f, 0.82f, 1f),
                109);
            GameObject destroyed = CreateSpritePart(
                statue.transform,
                "Destroyed",
                assets.Square,
                new Vector2(0f, -0.7f),
                new Vector2(1.2f, 0.55f),
                new Color(0.48f, 0.42f, 0.62f, 1f),
                109);
            cracked.SetActive(false);
            destroyed.SetActive(false);
            SpriteRenderer glow = CreateSpritePart(
                    statue.transform,
                    "VisibleStarTearGlow",
                    assets.Square,
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
                statueCollider,
                intact,
                cracked,
                destroyed,
                glow,
                true);
            return component;
        }

        private static void CreateForceZone(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 direction,
            float acceleration,
            bool oscillates,
            float halfCycleSeconds,
            Color color,
            BuildAssets assets)
        {
            GameObject zone = CreateSpritePart(
                parent,
                name,
                assets.Square,
                position,
                new Vector2(4f, 3f),
                color,
                4);
            BoxCollider2D trigger =
                zone.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            GameObject indicator = CreateSpritePart(
                zone.transform,
                "FlowIndicator",
                assets.Square,
                Vector2.zero,
                new Vector2(0.12f, 0.05f),
                new Color(0.72f, 1f, 1f, 0.85f),
                5);
            P10TraversalForceZone2D force =
                zone.AddComponent<P10TraversalForceZone2D>();
            force.Configure(
                direction,
                acceleration,
                oscillates,
                halfCycleSeconds,
                indicator.GetComponent<SpriteRenderer>());
        }

        private static void CreateStageMarkers(
            Transform envRoot,
            P12StageDefinition definition)
        {
            if (definition.MercyCorridor)
            {
                CreateChild(envRoot, "MercyCorridor_Guaranteed");
            }

            if (definition.FirstPairingSafeDemonstration)
            {
                CreateChild(
                    envRoot,
                    "SafeDemonstrationZone_FirstPairing");
            }

            if (definition.IsBossEchoStage)
            {
                CreateChild(
                    envRoot,
                    $"BossEchoMarker_{definition.BossEcho}");
            }

            if (definition.IsFinalArrival)
            {
                CreateChild(
                    envRoot,
                    "FinalArrival_DawnStarAnchorage");
            }
        }

        private static void CreateStageExit(
            P12StageNode2D node,
            P12StageFlowController2D flow)
        {
            GameObject exitObject = CreateChild(
                node.Environment.EnvironmentRoot.transform,
                $"StageExit_{node.StageId}");
            exitObject.transform.position =
                node.Environment.ExitAnchor.position;
            P12StageExit2D exit =
                exitObject.AddComponent<P12StageExit2D>();
            exit.Configure(node, flow);
        }

        private static void CreateEpilogue(
            P12StageNode2D finalNode,
            P12ChallengeDirector2D director,
            BuildAssets assets)
        {
            GameObject epilogue = CreateChild(
                finalNode.Environment.EnvironmentRoot.transform,
                "P12Epilogue_DawnStarAnchorage");
            epilogue.transform.position =
                finalNode.Environment.ExitAnchor.position
                + new Vector3(0f, 2f, 0f);
            SpriteRenderer signal = CreateSpritePart(
                    epilogue.transform,
                    "NaraeSignal",
                    assets.Square,
                    new Vector2(-0.6f, 0f),
                    Vector2.one * 0.5f,
                    new Color(1f, 0.87f, 0.55f, 0f),
                    95)
                .GetComponent<SpriteRenderer>();
            SpriteRenderer response = CreateSpritePart(
                    epilogue.transform,
                    "RaniLanternResponse",
                    assets.Square,
                    new Vector2(0.6f, 0f),
                    Vector2.one * 0.5f,
                    new Color(0.55f, 0.8f, 1f, 0f),
                    95)
                .GetComponent<SpriteRenderer>();
            P12EpiloguePresentation2D presentation =
                epilogue.AddComponent<P12EpiloguePresentation2D>();
            presentation.Configure(director, signal, response);
        }

        private static void CreatePendingHumanGateMarkers(
            Transform persistent)
        {
            GameObject root = CreateChild(
                persistent,
                "P12_HUMAN_GATES_PENDING_SKILLED_COMPLETION_5_15");
            CreateChild(
                root.transform,
                "Followup_P12_SkilledPlayerCompletionPlaytest"
                + "_REQUIRED");
        }

        private static P8MaruRoomGraph2D BuildMaruRoomGraph(
            Transform parent,
            int rooms)
        {
            GameObject graphObject = CreateChild(
                parent,
                "P8_TerrainIgnoringRoomGraph");
            P8MaruRoomGraph2D graph =
                graphObject.AddComponent<P8MaruRoomGraph2D>();
            var nodes = new P8MaruRoomNode[rooms];
            for (int index = 0; index < rooms; index++)
            {
                var center = new Vector2(6.5f + index * 6f, 4.5f);
                var size = new Vector2(6f, 9f);
                var adjacency = new List<int>(2);
                if (index > 0)
                {
                    adjacency.Add(index - 1);
                }

                if (index + 1 < rooms)
                {
                    adjacency.Add(index + 1);
                }

                nodes[index] = new P8MaruRoomNode(
                    index,
                    new Rect(center - size * 0.5f, size),
                    center,
                    adjacency.ToArray());
            }

            graph.Configure(nodes);
            return graph;
        }

        private static P8ReturnPile2D BuildReturnPile(
            Transform parent,
            Vector3 position)
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
            return pile;
        }

        private static Tilemap CreateTilemapLayer(
            Transform parent,
            string name,
            int sortingOrder,
            bool compositeSolid,
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
            if (!compositeSolid)
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
            tilemapCollider.compositeOperation =
                Collider2D.CompositeOperation.Merge;
            composite = layer.AddComponent<CompositeCollider2D>();
            composite.geometryType =
                CompositeCollider2D.GeometryType.Polygons;
            composite.generationType =
                CompositeCollider2D.GenerationType.Synchronous;
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
                        new Vector3Int(x + offsetX, y + offsetY, 0),
                        tile);
                }
            }
        }

        private static Vector2 CellWorld(Vector2Int cell)
        {
            return new Vector2(cell.x + 0.5f, cell.y + 0.5f);
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

        private static GameObject CreateChild(
            Transform parent,
            string name)
        {
            var child = new GameObject(name);
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

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .FirstOrDefault();
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

        private static BuildAssets LoadAssets()
        {
            GameObject bombObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    P5MoonPalaceSliceBuilder.BombPrefabPath);
            return new BuildAssets(
                LoadSprite(SquareSpritePath),
                AssetDatabase.LoadAssetAtPath<TileBase>(
                    ReinforcedTilePath),
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
                    "P12 square sprite, reinforced tile, tile "
                    + "definitions, or bomb prefab are incomplete.");
            }
        }

        private readonly struct BuildAssets
        {
            public readonly Sprite Square;
            public readonly TileBase ReinforcedTile;
            public readonly TileDefinition[] TileDefinitions;
            public readonly Bomb2D BombPrefab;

            public BuildAssets(
                Sprite square,
                TileBase reinforcedTile,
                TileDefinition[] tileDefinitions,
                Bomb2D bombPrefab)
            {
                Square = square;
                ReinforcedTile = reinforcedTile;
                TileDefinitions =
                    tileDefinitions ?? Array.Empty<TileDefinition>();
                BombPrefab = bombPrefab;
            }

            public bool IsComplete =>
                Square != null
                && ReinforcedTile != null
                && TileDefinitions.Length >= 2
                && TileDefinitions.All(item => item != null)
                && BombPrefab != null;
        }
    }
}

#endif
