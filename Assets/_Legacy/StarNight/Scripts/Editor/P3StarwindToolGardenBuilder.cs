#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Debugging;
using StarNight.Explosions;
using StarNight.Grid;
using StarNight.Objects;
using StarNight.Player;
using StarNight.Tiles;
using StarNight.Tools;
using StarNight.Tools.Grapple;
using StarNight.Tools.Mining;
using StarNight.Tools.Pestle;
using StarNight.Tools.Rope;
using StarNight.Tools.Umbrella;
using StarNight.Tools.Water;
using StarNight.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Editor
{
    /// <summary>
    /// Rebuilds the P3 Starwind Tool Garden. Imported bundle art is fitted at
    /// render time while every collision and mutation remains on the 1 x 1 grid.
    /// </summary>
    public static class P3StarwindToolGardenBuilder
    {
        public const int Width = P3ToolGardenContract.Width;
        public const int Height = P3ToolGardenContract.Height;
        public const string ScenePath =
            "Assets/StarNight/Scenes/Labs/P3_StarwindToolGarden_72x30.unity";
        public const string PlayerPrefabPath =
            "Assets/StarNight/Prefabs/Gameplay/P3_Player.prefab";
        public const string BombPrefabPath =
            "Assets/StarNight/Prefabs/Gameplay/P3_Bomb.prefab";

        private const string P2PlayerPrefabPath =
            "Assets/StarNight/Prefabs/Gameplay/P2_Player.prefab";
        private const string P2BombPrefabPath =
            "Assets/StarNight/Prefabs/Gameplay/P2_Bomb.prefab";
        private const string MovementTuningPath =
            "Assets/StarNight/Settings/P1_MovementTuning.asset";

        private const string DataFolder = "Assets/StarNight/Data/P3";
        private const string TileFolder = DataFolder + "/Tiles";
        private const string PrefabFolder =
            "Assets/StarNight/Prefabs/Gameplay/P3Tools";

        private const string GroundSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/base main shape fill.png";
        private const string GroundSideSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/base main shape sides.png";
        private const string SkySpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Sky B.png";
        private const string MountainASpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Mounts A.png";
        private const string MountainBSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Mounts B.png";
        private const string CloudSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Clouds small A.png";
        private const string SquareSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Square.png";
        private const string PlatformSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Platforms and doors.png";
        private const string SpringElementsPath =
            "Assets/2D Fantasy sprite bundle/Spring forest/Sprites/elements.png";
        private const string DryVineSpritePath =
            "Assets/2D Fantasy sprite bundle/Bonus/Climbing elements/Climbing plants/Climbing plants small Dry.png";
        private const string GrownVineSpritePath =
            "Assets/2D Fantasy sprite bundle/Bonus/Climbing elements/Climbing plants/Climbing plants small.png";
        private const string StarSpritePath =
            "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/Cristal Sprites/Star particle.png";

        private const string ReinforcedTilePath = TileFolder + "/P3_CloudRoot.asset";
        private const string StoneTilePath = TileFolder + "/P3_Cloudstone.asset";
        private const string DirtTilePath = TileFolder + "/P3_SoftGardenSoil.asset";
        private const string CrackedTilePath = TileFolder + "/P3_CrackedMoonRock.asset";
        private const string ExitTilePath = TileFolder + "/P3_ExitFrame.asset";
        private const string ThinFloorTilePath = TileFolder + "/P3_ThinFloor.asset";
        private const string GlowTilePath = TileFolder + "/P3_CueGlow.asset";

        private const string ReinforcedDefinitionPath =
            TileFolder + "/P3_CloudRoot_Definition.asset";
        private const string StoneDefinitionPath =
            TileFolder + "/P3_Cloudstone_Definition.asset";
        private const string DirtDefinitionPath =
            TileFolder + "/P3_SoftGardenSoil_Definition.asset";
        private const string CrackedDefinitionPath =
            TileFolder + "/P3_CrackedMoonRock_Definition.asset";
        private const string ExitDefinitionPath =
            TileFolder + "/P3_ExitFrame_Definition.asset";
        private const string ThinFloorDefinitionPath =
            TileFolder + "/P3_ThinFloor_Definition.asset";

        private static readonly Vector2 PlayerSpawn = new Vector2(2.5f, 1.45f);

        [MenuItem("StarNight/P3/Rebuild Starwind Tool Garden")]
        public static void RebuildStarwindToolGarden()
        {
            EnsureOutputFolders();
            SourceArt art = LoadSourceArt();
            GeneratedTiles tiles = RebuildGeneratedTiles(art);
            RebuildPlayerPrefab();
            RebuildBombPrefab();
            ToolPrefabs toolPrefabs = RebuildToolPrefabs(art);
            AssetDatabase.SaveAssets();

            BuildScene(
                art,
                tiles,
                toolPrefabs,
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath),
                AssetDatabase.LoadAssetAtPath<GameObject>(BombPrefabPath)
                    ?.GetComponent<Bomb2D>(),
                AssetDatabase.LoadAssetAtPath<P1MovementTuning>(
                    MovementTuningPath));

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[StarNight P3] Starwind Tool Garden rebuilt: {ScenePath}");
        }

        private static SourceArt LoadSourceArt()
        {
            SourceArt art = new SourceArt
            {
                Ground = LoadSprite(GroundSpritePath),
                GroundSide = LoadSprite(GroundSideSpritePath),
                Sky = LoadSprite(SkySpritePath),
                MountainA = LoadSprite(MountainASpritePath),
                MountainB = LoadSprite(MountainBSpritePath),
                Cloud = LoadSprite(CloudSpritePath),
                Square = LoadSprite(SquareSpritePath),
                Anchor = LoadSprite(PlatformSpritePath, "Platforms and doors_4"),
                ButtonSmall = LoadSprite(
                    PlatformSpritePath,
                    "Platforms and doors_5"),
                ButtonLarge = LoadSprite(
                    PlatformSpritePath,
                    "Platforms and doors_6"),
                Branch = LoadSprite(SpringElementsPath, "elements_10"),
                DryVine = LoadSprite(
                    DryVineSpritePath,
                    "Climbing plants small Dry_4"),
                GrownVine = LoadSprite(
                    GrownVineSpritePath,
                    "Climbing plants small_4"),
                Star = LoadSprite(StarSpritePath)
            };

            if (!art.IsComplete)
            {
                throw new InvalidOperationException(
                    "P3 Starwind theme art is missing from the 2D Fantasy Sprite Bundle.");
            }

            return art;
        }

        private static GeneratedTiles RebuildGeneratedTiles(SourceArt art)
        {
            Tile reinforced = RebuildTile(
                ReinforcedTilePath,
                "P3_CloudRoot",
                art.Ground,
                new Color(0.20f, 0.25f, 0.35f, 1f),
                Tile.ColliderType.Grid);
            Tile stone = RebuildTile(
                StoneTilePath,
                "P3_Cloudstone",
                art.Ground,
                new Color(0.48f, 0.56f, 0.66f, 1f),
                Tile.ColliderType.Grid);
            Tile dirt = RebuildTile(
                DirtTilePath,
                "P3_SoftGardenSoil",
                art.Ground,
                new Color(0.48f, 0.31f, 0.25f, 1f),
                Tile.ColliderType.Grid);
            Tile cracked = RebuildTile(
                CrackedTilePath,
                "P3_CrackedMoonRock",
                art.Ground,
                new Color(0.70f, 0.52f, 0.24f, 1f),
                Tile.ColliderType.Grid);
            Tile exit = RebuildTile(
                ExitTilePath,
                "P3_ExitFrame",
                art.Ground,
                new Color(0.42f, 0.84f, 0.94f, 1f),
                Tile.ColliderType.Grid);
            Tile thinFloor = RebuildTile(
                ThinFloorTilePath,
                "P3_ThinFloor",
                art.Ground,
                new Color(0.66f, 0.72f, 0.78f, 1f),
                Tile.ColliderType.Grid,
                new Vector2(1f, 0.34f));
            Tile glow = RebuildTile(
                GlowTilePath,
                "P3_CueGlow",
                art.Star,
                new Color(1f, 0.86f, 0.28f, 0.86f),
                Tile.ColliderType.None,
                new Vector2(0.46f, 0.46f));

            TileDefinition reinforcedDefinition = RebuildDefinition(
                ReinforcedDefinitionPath,
                "P3_CloudRoot_Definition",
                "cloud_root",
                reinforced,
                TileMaterialKind.ReinforcedWall,
                TileBreakMethod.None,
                true);
            TileDefinition stoneDefinition = RebuildDefinition(
                StoneDefinitionPath,
                "P3_Cloudstone_Definition",
                "cloudstone",
                stone,
                TileMaterialKind.Stone,
                TileBreakMethod.Pickaxe);
            TileDefinition dirtDefinition = RebuildDefinition(
                DirtDefinitionPath,
                "P3_SoftGardenSoil_Definition",
                "soft_garden_soil",
                dirt,
                TileMaterialKind.Dirt,
                TileBreakMethod.Bomb | TileBreakMethod.Shovel);
            TileDefinition crackedDefinition = RebuildDefinition(
                CrackedDefinitionPath,
                "P3_CrackedMoonRock_Definition",
                "cracked_moon_rock",
                cracked,
                TileMaterialKind.CrackedWall,
                TileBreakMethod.Bomb | TileBreakMethod.Pickaxe);
            TileDefinition exitDefinition = RebuildDefinition(
                ExitDefinitionPath,
                "P3_ExitFrame_Definition",
                "starwind_exit_frame",
                exit,
                TileMaterialKind.ExitFrame,
                TileBreakMethod.None,
                true);
            TileDefinition thinFloorDefinition = RebuildDefinition(
                ThinFloorDefinitionPath,
                "P3_ThinFloor_Definition",
                "thin_cloud_floor",
                thinFloor,
                TileMaterialKind.ThinFloor,
                TileBreakMethod.System);

            return new GeneratedTiles
            {
                Reinforced = reinforced,
                Stone = stone,
                Dirt = dirt,
                Cracked = cracked,
                Exit = exit,
                ThinFloor = thinFloor,
                Glow = glow,
                Definitions = new[]
                {
                    reinforcedDefinition,
                    stoneDefinition,
                    dirtDefinition,
                    crackedDefinition,
                    exitDefinition,
                    thinFloorDefinition
                }
            };
        }

        private static GeneratedTiles LoadGeneratedTiles()
        {
            return new GeneratedTiles
            {
                Reinforced =
                    AssetDatabase.LoadAssetAtPath<Tile>(ReinforcedTilePath),
                Stone = AssetDatabase.LoadAssetAtPath<Tile>(StoneTilePath),
                Dirt = AssetDatabase.LoadAssetAtPath<Tile>(DirtTilePath),
                Cracked = AssetDatabase.LoadAssetAtPath<Tile>(CrackedTilePath),
                Exit = AssetDatabase.LoadAssetAtPath<Tile>(ExitTilePath),
                ThinFloor =
                    AssetDatabase.LoadAssetAtPath<Tile>(ThinFloorTilePath),
                Glow = AssetDatabase.LoadAssetAtPath<Tile>(GlowTilePath),
                Definitions = new[]
                {
                    AssetDatabase.LoadAssetAtPath<TileDefinition>(
                        ReinforcedDefinitionPath),
                    AssetDatabase.LoadAssetAtPath<TileDefinition>(
                        StoneDefinitionPath),
                    AssetDatabase.LoadAssetAtPath<TileDefinition>(
                        DirtDefinitionPath),
                    AssetDatabase.LoadAssetAtPath<TileDefinition>(
                        CrackedDefinitionPath),
                    AssetDatabase.LoadAssetAtPath<TileDefinition>(
                        ExitDefinitionPath),
                    AssetDatabase.LoadAssetAtPath<TileDefinition>(
                        ThinFloorDefinitionPath)
                }
            };
        }

        private static void RebuildPlayerPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(P2PlayerPrefabPath);
            if (root == null)
            {
                throw new InvalidOperationException(
                    "P2 player prefab is required as the approved player base.");
            }

            try
            {
                root.name = "P3_Player";
                Rigidbody2D body = root.GetComponent<Rigidbody2D>();
                if (root.GetComponent<PlayerToolInventory2D>() == null)
                {
                    root.AddComponent<PlayerToolInventory2D>();
                }

                if (root.GetComponent<PlayerConsumableTools2D>() == null)
                {
                    root.AddComponent<PlayerConsumableTools2D>();
                }

                RopeClimber2D climber = root.GetComponent<RopeClimber2D>();
                if (climber == null)
                {
                    climber = root.AddComponent<RopeClimber2D>();
                }

                climber.Configure(body, 4f, 0.78f);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RebuildBombPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(P2BombPrefabPath);
            if (root == null)
            {
                throw new InvalidOperationException(
                    "P2 bomb prefab is required as the tested bomb base.");
            }

            try
            {
                root.name = "P3_Bomb";
                if (root.GetComponent<RopeExplosionBridge2D>() == null)
                {
                    root.AddComponent<RopeExplosionBridge2D>();
                }

                PrefabUtility.SaveAsPrefabAsset(root, BombPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static ToolPrefabs RebuildToolPrefabs(SourceArt art)
        {
            return new ToolPrefabs
            {
                Pickaxe = RebuildToolPrefab(
                    HandToolKind.Pickaxe,
                    PickaxeTool2D.DefaultDurability,
                    PrefabFolder + "/P3_Pickaxe.prefab",
                    art),
                Shovel = RebuildToolPrefab(
                    HandToolKind.Shovel,
                    ShovelTool2D.DefaultDurability,
                    PrefabFolder + "/P3_Shovel.prefab",
                    art),
                WateringCan = RebuildToolPrefab(
                    HandToolKind.WateringCan,
                    WateringCanTool2D.Capacity,
                    PrefabFolder + "/P3_WateringCan.prefab",
                    art),
                Pestle = RebuildToolPrefab(
                    HandToolKind.Pestle,
                    0,
                    PrefabFolder + "/P3_Pestle.prefab",
                    art),
                Grapple = RebuildToolPrefab(
                    HandToolKind.Grapple,
                    0,
                    PrefabFolder + "/P3_Grapple.prefab",
                    art),
                Umbrella = RebuildToolPrefab(
                    HandToolKind.WindUmbrella,
                    0,
                    PrefabFolder + "/P3_WindUmbrella.prefab",
                    art)
            };
        }

        private static ToolPrefabs LoadToolPrefabs()
        {
            return new ToolPrefabs
            {
                Pickaxe = AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabFolder + "/P3_Pickaxe.prefab"),
                Shovel = AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabFolder + "/P3_Shovel.prefab"),
                WateringCan = AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabFolder + "/P3_WateringCan.prefab"),
                Pestle = AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabFolder + "/P3_Pestle.prefab"),
                Grapple = AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabFolder + "/P3_Grapple.prefab"),
                Umbrella = AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabFolder + "/P3_WindUmbrella.prefab")
            };
        }

        private static GameObject RebuildToolPrefab(
            HandToolKind kind,
            int maximumUses,
            string path,
            SourceArt art)
        {
            GameObject root = new GameObject($"P3_{kind}");
            try
            {
                CircleCollider2D trigger = root.AddComponent<CircleCollider2D>();
                trigger.isTrigger = true;
                trigger.radius = 0.55f;

                SpriteRenderer primary = BuildToolSilhouette(root.transform, kind, art);
                HandToolPickup2D pickup = root.AddComponent<HandToolPickup2D>();
                pickup.Configure(kind, maximumUses, trigger, primary);

                GameObject dotsObject = new GameObject("ChargeDots");
                dotsObject.transform.SetParent(root.transform, false);
                dotsObject.transform.localPosition = new Vector3(0f, 0.78f, 0f);
                ToolChargeDots2D dots = dotsObject.AddComponent<ToolChargeDots2D>();
                dots.Configure(
                    pickup,
                    art.Star,
                    new Color(1f, 0.88f, 0.30f, 1f),
                    new Color(0.16f, 0.20f, 0.30f, 0.66f));

                switch (kind)
                {
                    case HandToolKind.Pickaxe:
                        root.AddComponent<PickaxeTool2D>().Configure(
                            null,
                            null,
                            PickaxeTool2D.DefaultDurability);
                        break;
                    case HandToolKind.Shovel:
                        root.AddComponent<ShovelTool2D>().Configure(
                            null,
                            null,
                            ShovelTool2D.DefaultDurability);
                        break;
                    case HandToolKind.WateringCan:
                        root.AddComponent<WateringCanTool2D>().Configure(
                            null,
                            null,
                            WateringCanTool2D.Capacity);
                        break;
                    case HandToolKind.Pestle:
                        root.AddComponent<PestleTool2D>().Configure(null, null);
                        break;
                    case HandToolKind.Grapple:
                        root.AddComponent<GrappleLauncher2D>();
                        break;
                    case HandToolKind.WindUmbrella:
                        root.AddComponent<WindUmbrellaMotor2D>();
                        break;
                }

                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static SpriteRenderer BuildToolSilhouette(
            Transform root,
            HandToolKind kind,
            SourceArt art)
        {
            Color color = GetToolColor(kind);
            switch (kind)
            {
                case HandToolKind.Pickaxe:
                    CreatePart(root, art.Square, "Handle", new Vector2(0f, -0.05f),
                        new Vector2(0.13f, 0.92f), -25f, color);
                    return CreatePart(root, art.Square, "PickHead",
                        new Vector2(-0.10f, 0.34f),
                        new Vector2(0.78f, 0.13f), -8f, color);

                case HandToolKind.Shovel:
                    CreatePart(root, art.Square, "Handle", new Vector2(0f, 0.05f),
                        new Vector2(0.12f, 0.82f), 0f, color);
                    return CreatePart(root, art.ButtonLarge, "Spade",
                        new Vector2(0f, -0.42f),
                        new Vector2(0.44f, 0.40f), 0f, color);

                case HandToolKind.WateringCan:
                    CreatePart(root, art.Square, "Spout",
                        new Vector2(0.38f, 0.03f),
                        new Vector2(0.62f, 0.11f), 18f, color);
                    return CreatePart(root, art.ButtonLarge, "CanBody",
                        new Vector2(-0.08f, -0.08f),
                        new Vector2(0.58f, 0.50f), 0f, color);

                case HandToolKind.Pestle:
                    CreatePart(root, art.Square, "Shaft",
                        new Vector2(0f, 0.10f),
                        new Vector2(0.18f, 0.92f), 0f, color);
                    return CreatePart(root, art.ButtonLarge, "PestleHead",
                        new Vector2(0f, -0.42f),
                        new Vector2(0.55f, 0.30f), 0f, color);

                case HandToolKind.Grapple:
                    CreatePart(root, art.Square, "Launcher",
                        new Vector2(-0.16f, -0.08f),
                        new Vector2(0.55f, 0.28f), -12f, color);
                    return CreatePart(root, art.Anchor, "Hook",
                        new Vector2(0.30f, 0.18f),
                        new Vector2(0.24f, 0.70f), -54f, color);

                case HandToolKind.WindUmbrella:
                    CreatePart(root, art.Square, "UmbrellaHandle",
                        new Vector2(0f, -0.18f),
                        new Vector2(0.10f, 0.82f), 0f, color);
                    return CreatePart(root, art.Cloud, "UmbrellaCanopy",
                        new Vector2(0f, 0.26f),
                        new Vector2(0.86f, 0.42f), 0f, color);

                default:
                    return CreatePart(root, art.Star, "Tool",
                        Vector2.zero, Vector2.one * 0.5f, 0f, color);
            }
        }

        private static void BuildScene(
            SourceArt art,
            GeneratedTiles tiles,
            ToolPrefabs toolPrefabs,
            GameObject playerPrefab,
            Bomb2D bombPrefab,
            P1MovementTuning tuning)
        {
            if (!art.IsComplete
                || !tiles.IsComplete
                || !toolPrefabs.IsComplete
                || playerPrefab == null
                || bombPrefab == null
                || tuning == null)
            {
                throw new InvalidOperationException(
                    "P3 generated assets failed to reload before scene assembly.");
            }

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            scene.name = "P3_StarwindToolGarden_72x30";

            // Scene changes can invalidate component references obtained from
            // prefab contents. Reload every persistent asset by path.
            art = LoadSourceArt();
            tiles = LoadGeneratedTiles();
            toolPrefabs = LoadToolPrefabs();
            playerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject bombPrefabObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(BombPrefabPath);
            bombPrefab = bombPrefabObject != null
                ? bombPrefabObject.GetComponent<Bomb2D>()
                : null;
            tuning = AssetDatabase.LoadAssetAtPath<P1MovementTuning>(
                MovementTuningPath);

            if (!art.IsComplete
                || !tiles.IsComplete
                || !toolPrefabs.IsComplete
                || playerPrefab == null
                || bombPrefab == null
                || tuning == null)
            {
                throw new InvalidOperationException(
                    "P3 persistent assets did not survive the scene switch.");
            }

            GameObject root = new GameObject("P3_StarwindToolGarden_72x30");
            BuildBackdrop(root.transform, art);

            GameObject gridRoot = new GameObject("GridWorld");
            gridRoot.transform.SetParent(root.transform);
            UnityEngine.Grid grid = gridRoot.AddComponent<UnityEngine.Grid>();
            grid.cellSize = Vector3.one;

            Tilemap terrain = CreateTilemapLayer(
                gridRoot.transform,
                "Terrain",
                0,
                true,
                out TilemapCollider2D terrainCollider,
                out CompositeCollider2D composite);
            Tilemap hazard = CreateTilemapLayer(
                gridRoot.transform,
                "Hazard",
                3,
                false,
                out _,
                out _);
            Tilemap decoration = CreateTilemapLayer(
                gridRoot.transform,
                "Decoration",
                5,
                false,
                out _,
                out _);
            Tilemap logic = CreateTilemapLayer(
                gridRoot.transform,
                "Logic",
                6,
                false,
                out _,
                out _);

            GridWorld world = gridRoot.AddComponent<GridWorld>();
            world.Configure(
                grid,
                terrain,
                hazard,
                Vector2Int.zero,
                new Vector2Int(Width, Height));
            PopulateTerrain(terrain, decoration, logic, tiles);
            terrain.RefreshAllTiles();
            decoration.RefreshAllTiles();
            logic.RefreshAllTiles();
            terrainCollider.ProcessTilemapChanges();
            composite.GenerateGeometry();
            Physics2D.SyncTransforms();

            GameObject systems = new GameObject("P3_Systems");
            systems.transform.SetParent(root.transform);
            P3ToolDiscoveryTelemetry telemetry =
                systems.AddComponent<P3ToolDiscoveryTelemetry>();
            TileMutationService mutation = systems.AddComponent<TileMutationService>();
            GridPos[] protectedExitCells = BuildProtectedExitCells();

            GameObject player = InstantiatePrefab(
                playerPrefab,
                scene,
                root.transform,
                "Player",
                PlayerSpawn);
            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
            CapsuleCollider2D playerCollider =
                player.GetComponent<CapsuleCollider2D>();
            PlayerInputAdapter playerInput =
                player.GetComponent<PlayerInputAdapter>();
            PlayerMotor2D motor = player.GetComponent<PlayerMotor2D>();
            SafeCellTracker safeCells = player.GetComponent<SafeCellTracker>();
            PlayerRecovery recovery = player.GetComponent<PlayerRecovery>();
            CarrySystem carry = player.GetComponent<CarrySystem>();
            Transform holdAnchor = player.transform.Find("CarryAnchor");

            mutation.Configure(
                world,
                terrain,
                decoration,
                terrainCollider,
                composite,
                playerCollider,
                tiles.Definitions,
                P3ToolGardenContract.Start,
                P3ToolGardenContract.Exit,
                protectedExitCells);

            ExplosionService2D explosions =
                systems.AddComponent<ExplosionService2D>();
            explosions.Configure(
                world,
                mutation,
                ExplosionConstants.DefaultChainHardCap,
                7f,
                ~0);

            GameObject installedRopes = new GameObject("InstalledRopes");
            installedRopes.transform.SetParent(root.transform);
            RopeInstaller2D ropeInstaller = systems.AddComponent<RopeInstaller2D>();
            ropeInstaller.Configure(
                world,
                mutation,
                null,
                installedRopes.transform,
                RopePlacementSolver.DefaultMaximumLength,
                art.Anchor);

            WaterInteractionRegistry2D waterRegistry =
                systems.AddComponent<WaterInteractionRegistry2D>();
            PestleInteractionRegistry2D pestleRegistry =
                systems.AddComponent<PestleInteractionRegistry2D>();

            Camera camera = BuildCamera(root.transform);
            GridBoundedCamera2D cameraFollow =
                camera.gameObject.AddComponent<GridBoundedCamera2D>();
            cameraFollow.Configure(camera, player.transform, playerBody, world, recovery);

            playerBody.position = PlayerSpawn;
            safeCells.Configure(world, playerBody, playerCollider, motor, tuning);
            safeCells.SetSpawnFallback(PlayerSpawn);
            recovery.Configure(world, playerBody, motor, safeCells, tuning);
            carry.Configure(playerInput, playerBody, holdAnchor, world);

            GameObject objectsRoot = new GameObject("P3_ToolBays");
            objectsRoot.transform.SetParent(root.transform);
            WaterSource2D[] waterSources = BuildToolBays(
                scene,
                objectsRoot.transform,
                player.transform,
                world,
                mutation,
                waterRegistry,
                pestleRegistry,
                toolPrefabs,
                bombPrefab,
                explosions,
                art,
                tiles);

            PlayerToolInventory2D inventory =
                player.GetComponent<PlayerToolInventory2D>();
            inventory.Configure(
                playerInput,
                carry,
                motor,
                playerBody,
                playerCollider,
                world,
                holdAnchor,
                camera,
                waterRegistry,
                pestleRegistry,
                waterSources,
                telemetry);

            PlayerConsumableTools2D consumables =
                player.GetComponent<PlayerConsumableTools2D>();
            consumables.Configure(
                playerInput,
                playerBody,
                world,
                ropeInstaller,
                explosions,
                bombPrefab,
                objectsRoot.transform,
                telemetry,
                PlayerConsumableTools2D.DefaultRopeStock,
                PlayerConsumableTools2D.DefaultBombStock);

            BuildToolHud(
                root.transform,
                camera,
                consumables,
                inventory,
                art);
            BuildRecoveryVolume(root.transform);
            BuildExitVisual(root.transform, art);
            BuildDirectionalLight(root.transform);

            if (!P3ToolGardenContract.ValidateToolFreeMainRoute(
                    world,
                    out GridPos routeFailure))
            {
                throw new InvalidOperationException(
                    $"P3 tool-free main route contract failed at {routeFailure}.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = player;
        }

        private static WaterSource2D[] BuildToolBays(
            Scene scene,
            Transform parent,
            Transform player,
            GridWorld world,
            TileMutationService mutation,
            WaterInteractionRegistry2D waterRegistry,
            PestleInteractionRegistry2D pestleRegistry,
            ToolPrefabs prefabs,
            Bomb2D bombPrefab,
            ExplosionService2D explosions,
            SourceArt art,
            GeneratedTiles tiles)
        {
            BuildRopeBay(parent, player, world, art);
            BuildBombBay(scene, parent, player, bombPrefab, explosions, art);

            GameObject pickaxe = InstantiatePrefab(
                prefabs.Pickaxe,
                scene,
                parent,
                "03_Pickaxe",
                new Vector2(23.5f, 1.55f));
            pickaxe.GetComponent<PickaxeTool2D>().Configure(
                world,
                mutation,
                PickaxeTool2D.DefaultDurability);
            BuildCue(parent, player, art, P3ToolKind.Pickaxe,
                new Vector2(25.5f, 3.65f), Vector2.right);

            GameObject shovel = InstantiatePrefab(
                prefabs.Shovel,
                scene,
                parent,
                "04_Shovel",
                new Vector2(31.5f, 1.55f));
            ShovelTool2D shovelTool = shovel.GetComponent<ShovelTool2D>();
            shovelTool.Configure(
                world,
                mutation,
                ShovelTool2D.DefaultDurability);
            shovelTool.ConfigureSoftTerrain(new[] { tiles.DirtDefinition });
            BuildCue(parent, player, art, P3ToolKind.Shovel,
                new Vector2(33.5f, 3.65f), Vector2.down);

            GameObject wateringCan = InstantiatePrefab(
                prefabs.WateringCan,
                scene,
                parent,
                "05_WateringCan",
                new Vector2(39.5f, 1.55f));
            wateringCan.GetComponent<WateringCanTool2D>().Configure(
                world,
                waterRegistry,
                WateringCanTool2D.Capacity);
            WaterSource2D source = BuildWaterBay(
                parent,
                player,
                world,
                waterRegistry,
                art);

            GameObject pestle = InstantiatePrefab(
                prefabs.Pestle,
                scene,
                parent,
                "06_Pestle",
                new Vector2(47.5f, 1.55f));
            pestle.GetComponent<PestleTool2D>().Configure(
                world,
                pestleRegistry,
                PestleTool2D.DefaultRecoveryDuration);
            BuildPestleBay(
                parent,
                player,
                world,
                mutation,
                pestleRegistry,
                art);

            GameObject grapple = InstantiatePrefab(
                prefabs.Grapple,
                scene,
                parent,
                "07_Grapple",
                new Vector2(55.5f, 1.55f));
            BuildGrappleBay(parent, player, world, art);

            GameObject umbrella = InstantiatePrefab(
                prefabs.Umbrella,
                scene,
                parent,
                "08_WindUmbrella",
                new Vector2(63.5f, 1.55f));
            BuildUmbrellaBay(parent, player, art);

            return new[] { source };
        }

        private static void BuildRopeBay(
            Transform parent,
            Transform player,
            GridWorld world,
            SourceArt art)
        {
            GameObject anchorObject = new GameObject("01_Rope_Ring");
            anchorObject.transform.SetParent(parent);
            anchorObject.transform.position = new Vector3(7.5f, 7.5f, 0f);
            RopeAnchor2D anchor = anchorObject.AddComponent<RopeAnchor2D>();
            anchor.Configure(world, new GridPos(7, 7), RopeAnchorKind.Ring);
            CreatePart(anchorObject.transform, art.Anchor, "RingVisual",
                Vector2.zero, new Vector2(0.38f, 1.15f), 0f,
                new Color(0.92f, 0.82f, 0.40f, 1f));
            BuildCue(parent, player, art, P3ToolKind.Rope,
                new Vector2(7.5f, 3.6f), Vector2.up);
        }

        private static void BuildBombBay(
            Scene scene,
            Transform parent,
            Transform player,
            Bomb2D bombPrefab,
            ExplosionService2D explosions,
            SourceArt art)
        {
            Bomb2D displayBomb = UnityEngine.Object.Instantiate(
                bombPrefab,
                new Vector2(14.5f, 1.55f),
                Quaternion.identity,
                parent);
            displayBomb.name = "02_Bomb_VisualSample";
            displayBomb.Configure(explosions, Bomb2D.DefaultFuseSeconds, false, true);
            BuildCue(parent, player, art, P3ToolKind.Bomb,
                new Vector2(17.5f, 3.6f), Vector2.right);
        }

        private static WaterSource2D BuildWaterBay(
            Transform parent,
            Transform player,
            GridWorld world,
            WaterInteractionRegistry2D registry,
            SourceArt art)
        {
            GameObject sourceObject = new GameObject("05_WaterSource");
            sourceObject.transform.SetParent(parent);
            sourceObject.transform.position = new Vector3(38.5f, 1.45f, 0f);
            CircleCollider2D sourceTrigger =
                sourceObject.AddComponent<CircleCollider2D>();
            sourceTrigger.isTrigger = true;
            sourceTrigger.radius = 0.6f;
            WaterSource2D source = sourceObject.AddComponent<WaterSource2D>();
            source.Configure(world, new GridPos(38, 1), 2);
            CreatePart(sourceObject.transform, art.ButtonLarge, "WaterGlow",
                Vector2.zero, new Vector2(0.58f, 0.58f), 0f,
                new Color(0.20f, 0.82f, 1f, 0.92f));

            GameObject vine = new GameObject("05_DryGrowableVine");
            vine.transform.SetParent(parent);
            vine.transform.position = new Vector3(42.5f, 1.5f, 0f);
            BoxCollider2D platformCollider = vine.AddComponent<BoxCollider2D>();
            platformCollider.size = new Vector2(3.4f, 0.30f);
            platformCollider.offset = new Vector2(0f, 3.0f);
            SpriteRenderer dry = CreatePart(vine.transform, art.DryVine, "Dry",
                new Vector2(0f, 0.45f), new Vector2(2.8f, 0.72f), 0f,
                new Color(1f, 0.68f, 0.24f, 1f));
            SpriteRenderer grown = CreatePart(vine.transform, art.GrownVine, "Grown",
                new Vector2(0f, 3.0f), new Vector2(3.4f, 0.82f), 0f,
                new Color(0.45f, 1f, 0.42f, 1f));
            GrowableVinePlatform2D growable =
                vine.AddComponent<GrowableVinePlatform2D>();
            growable.Configure(
                registry,
                world,
                new GridPos(42, 1),
                platformCollider,
                dry,
                grown,
                false);
            BuildCue(parent, player, art, P3ToolKind.WateringCan,
                new Vector2(42.5f, 3.65f), Vector2.right);
            return source;
        }

        private static void BuildPestleBay(
            Transform parent,
            Transform player,
            GridWorld world,
            TileMutationService mutation,
            PestleInteractionRegistry2D registry,
            SourceArt art)
        {
            GameObject stakeObject = new GameObject("06_DrivenStake");
            stakeObject.transform.SetParent(parent);
            stakeObject.transform.position = new Vector3(49.5f, 0.55f, 0f);
            SpriteRenderer stakeVisual = CreatePart(
                stakeObject.transform,
                art.Square,
                "RaisedStake",
                new Vector2(0f, 0.42f),
                new Vector2(0.22f, 1.25f),
                0f,
                new Color(1f, 0.75f, 0.28f, 1f));
            DrivenStake2D stake = stakeObject.AddComponent<DrivenStake2D>();
            stake.Configure(
                registry,
                world,
                new GridPos(49, 0),
                stakeVisual.transform,
                stakeVisual,
                0.42f);

            GameObject thinFloorObject =
                new GameObject("06_ThinFloorPestleTarget");
            thinFloorObject.transform.SetParent(parent);
            thinFloorObject.transform.position =
                world.CellToWorldCenter(new GridPos(51, 2));
            thinFloorObject.AddComponent<ThinFloorPestleTarget2D>()
                .Configure(
                    registry,
                    world,
                    new GridPos(51, 2),
                    mutation);
            BuildCue(parent, player, art, P3ToolKind.Pestle,
                new Vector2(49.5f, 3.6f), Vector2.down);
        }

        private static void BuildGrappleBay(
            Transform parent,
            Transform player,
            GridWorld world,
            SourceArt art)
        {
            GameObject anchorObject = new GameObject("07_GrappleAnchor");
            anchorObject.transform.SetParent(parent);
            anchorObject.transform.position = new Vector3(59.5f, 7.5f, 0f);
            CircleCollider2D anchorCollider =
                anchorObject.AddComponent<CircleCollider2D>();
            anchorCollider.radius = 0.52f;
            CreatePart(anchorObject.transform, art.Anchor, "RoundAnchor",
                Vector2.zero, new Vector2(0.42f, 1.25f), 0f,
                new Color(0.34f, 0.94f, 1f, 1f));

            GameObject weight = new GameObject("07_PullableWeight");
            weight.transform.SetParent(parent);
            weight.transform.position = new Vector3(61.5f, 4.0f, 0f);
            Rigidbody2D weightBody = weight.AddComponent<Rigidbody2D>();
            weightBody.mass = 1.4f;
            weightBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            CircleCollider2D weightCollider =
                weight.AddComponent<CircleCollider2D>();
            weightCollider.radius = 0.44f;
            weight.AddComponent<GrapplePullable2D>().Configure(
                weightBody,
                8f,
                WorldObjectTraits.Pullable | WorldObjectTraits.Heavy);
            CreatePart(weight.transform, art.ButtonLarge, "WeightVisual",
                Vector2.zero, new Vector2(0.78f, 0.78f), 0f,
                new Color(0.75f, 0.52f, 0.92f, 1f));
            BuildCue(parent, player, art, P3ToolKind.Grapple,
                new Vector2(58.0f, 3.6f), new Vector2(1f, 1f));
        }

        private static void BuildUmbrellaBay(
            Transform parent,
            Transform player,
            SourceArt art)
        {
            GameObject windObject = new GameObject("08_Updraft");
            windObject.transform.SetParent(parent);
            windObject.transform.position = new Vector3(67f, 6f, 0f);
            BoxCollider2D windCollider = windObject.AddComponent<BoxCollider2D>();
            windCollider.isTrigger = true;
            windCollider.size = new Vector2(7f, 10f);
            windObject.AddComponent<WindZone2D>().Configure(
                windCollider,
                new Vector2(0.22f, 1f),
                7.5f,
                1.75f,
                1);

            for (int index = 0; index < 6; index++)
            {
                CreatePart(
                    windObject.transform,
                    index % 2 == 0 ? art.Cloud : art.Star,
                    $"WindLeaf_{index:00}",
                    new Vector2(
                        -2.2f + (index % 3) * 2.2f,
                        -3.5f + index * 1.4f),
                    index % 2 == 0
                        ? new Vector2(0.9f, 0.34f)
                        : Vector2.one * 0.22f,
                    index * 17f,
                    new Color(0.50f, 0.94f, 1f, 0.66f));
            }

            BuildCue(parent, player, art, P3ToolKind.WindUmbrella,
                new Vector2(66.0f, 3.6f), Vector2.up);
        }

        private static void BuildToolHud(
            Transform parent,
            Camera camera,
            PlayerConsumableTools2D consumables,
            PlayerToolInventory2D inventory,
            SourceArt art)
        {
            GameObject hudObject = new GameObject("P3_ToolHUD");
            hudObject.transform.SetParent(parent);

            SpriteRenderer ropeIcon = CreatePart(
                hudObject.transform,
                art.Anchor,
                "RopeIcon",
                Vector2.zero,
                new Vector2(0.22f, 0.58f),
                0f,
                new Color(1f, 0.86f, 0.32f, 1f));
            SpriteRenderer bombIcon = CreatePart(
                hudObject.transform,
                art.ButtonLarge,
                "BombIcon",
                new Vector2(0f, -0.54f),
                Vector2.one * 0.36f,
                0f,
                new Color(1f, 0.48f, 0.18f, 1f));
            SpriteRenderer heldIcon = CreatePart(
                hudObject.transform,
                art.Star,
                "HeldToolIcon",
                new Vector2(0f, -1.12f),
                Vector2.one * 0.38f,
                0f,
                Color.white);

            SpriteRenderer[] ropeDots = CreateHudDots(
                hudObject.transform,
                art.Star,
                "RopeStock",
                new Vector2(0.42f, 0f),
                4,
                4,
                new Color(1f, 0.88f, 0.34f, 1f));
            SpriteRenderer[] bombDots = CreateHudDots(
                hudObject.transform,
                art.Star,
                "BombStock",
                new Vector2(0.42f, -0.54f),
                4,
                4,
                new Color(1f, 0.56f, 0.26f, 1f));
            SpriteRenderer[] heldDots = CreateHudDots(
                hudObject.transform,
                art.Star,
                "HeldUses",
                new Vector2(0.42f, -1.06f),
                12,
                6,
                new Color(0.36f, 0.92f, 1f, 1f));

            ropeIcon.sortingOrder = 1000;
            bombIcon.sortingOrder = 1000;
            heldIcon.sortingOrder = 1000;
            SetSortingOrder(ropeDots, 1000);
            SetSortingOrder(bombDots, 1000);
            SetSortingOrder(heldDots, 1000);
            hudObject.AddComponent<P3ToolHud2D>().Configure(
                camera,
                consumables,
                inventory,
                ropeIcon,
                bombIcon,
                heldIcon,
                ropeDots,
                bombDots,
                heldDots);
        }

        private static SpriteRenderer[] CreateHudDots(
            Transform parent,
            Sprite sprite,
            string namePrefix,
            Vector2 origin,
            int count,
            int columns,
            Color color)
        {
            SpriteRenderer[] dots = new SpriteRenderer[count];
            for (int index = 0; index < count; index++)
            {
                Vector2 position = origin + new Vector2(
                    (index % columns) * 0.18f,
                    -(index / columns) * 0.18f);
                dots[index] = CreatePart(
                    parent,
                    sprite,
                    $"{namePrefix}_{index + 1:00}",
                    position,
                    Vector2.one * 0.11f,
                    0f,
                    color);
            }

            return dots;
        }

        private static void SetSortingOrder(
            SpriteRenderer[] renderers,
            int sortingOrder)
        {
            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] != null)
                {
                    renderers[index].sortingOrder = sortingOrder;
                }
            }
        }

        private static void PopulateTerrain(
            Tilemap terrain,
            Tilemap decoration,
            Tilemap logic,
            GeneratedTiles tiles)
        {
            for (int x = 0; x < Width; x++)
            {
                terrain.SetTile(new Vector3Int(x, 0, 0), tiles.Reinforced);
                terrain.SetTile(
                    new Vector3Int(x, Height - 1, 0),
                    tiles.Reinforced);
            }

            for (int y = 0; y < Height; y++)
            {
                terrain.SetTile(new Vector3Int(0, y, 0), tiles.Reinforced);
                terrain.SetTile(
                    new Vector3Int(Width - 1, y, 0),
                    tiles.Reinforced);
            }

            // Optional upper experiments; y=1 remains the uninterrupted route.
            for (int x = 6; x <= 9; x++)
            {
                terrain.SetTile(new Vector3Int(x, 8, 0), tiles.Reinforced);
            }

            for (int y = 2; y <= 4; y++)
            {
                terrain.SetTile(new Vector3Int(17, y, 0), tiles.Cracked);
                decoration.SetTile(new Vector3Int(17, y, 0), tiles.Glow);
            }

            terrain.SetTile(new Vector3Int(25, 2, 0), tiles.Stone);
            terrain.SetTile(new Vector3Int(33, 2, 0), tiles.Dirt);
            decoration.SetTile(new Vector3Int(33, 2, 0), tiles.Glow);
            terrain.SetTile(new Vector3Int(51, 2, 0), tiles.ThinFloor);

            for (int x = 58; x <= 62; x++)
            {
                terrain.SetTile(new Vector3Int(x, 3, 0), tiles.Reinforced);
            }

            for (int x = 65; x <= 69; x++)
            {
                terrain.SetTile(new Vector3Int(x, 12, 0), tiles.Reinforced);
            }

            terrain.SetTile(new Vector3Int(68, 11, 0), tiles.ThinFloor);

            for (int x = 4; x < Width - 4; x += 8)
            {
                logic.SetTile(new Vector3Int(x, 4, 0), tiles.Glow);
            }

            GridPos exit = P3ToolGardenContract.Exit;
            terrain.SetTile(new Vector3Int(exit.X + 1, 1, 0), tiles.Exit);
            terrain.SetTile(new Vector3Int(exit.X - 1, 2, 0), tiles.Exit);
            terrain.SetTile(new Vector3Int(exit.X + 1, 2, 0), tiles.Exit);
            terrain.SetTile(new Vector3Int(exit.X, 3, 0), tiles.Exit);
        }

        private static Tilemap CreateTilemapLayer(
            Transform parent,
            string name,
            int sortingOrder,
            bool collision,
            out TilemapCollider2D tilemapCollider,
            out CompositeCollider2D composite)
        {
            GameObject layer = new GameObject(name);
            layer.transform.SetParent(parent);
            Tilemap tilemap = layer.AddComponent<Tilemap>();
            TilemapRenderer renderer = layer.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;

            tilemapCollider = null;
            composite = null;
            if (collision)
            {
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
            }

            return tilemap;
        }

        private static void BuildBackdrop(Transform parent, SourceArt art)
        {
            for (int index = 0; index < 3; index++)
            {
                CreateBackdropSprite(
                    parent,
                    art.Sky,
                    $"Sky_{index:00}",
                    new Vector2(12f + index * 24f, 15f),
                    new Vector2(25f, 31f),
                    -100,
                    Color.white);
                CreateBackdropSprite(
                    parent,
                    index % 2 == 0 ? art.MountainA : art.MountainB,
                    $"Mountains_{index:00}",
                    new Vector2(12f + index * 24f, 8f),
                    new Vector2(25f, 16f),
                    -90,
                    new Color(0.62f, 0.73f, 0.86f, 0.72f));
            }

            for (int index = 0; index < 8; index++)
            {
                CreateBackdropSprite(
                    parent,
                    art.Cloud,
                    $"Cloud_{index:00}",
                    new Vector2(4f + index * 9f, 17f + (index % 3) * 3f),
                    new Vector2(5f, 2.2f),
                    -80,
                    new Color(0.92f, 0.96f, 1f, 0.58f));
            }
        }

        private static Camera BuildCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent);
            cameraObject.transform.position =
                new Vector3(PlayerSpawn.x + 3f, 6.5f, -10f);
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7.25f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.15f, 0.25f, 1f);
            return camera;
        }

        private static void BuildDirectionalLight(Transform parent)
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(parent);
            lightObject.transform.rotation = Quaternion.Euler(40f, -25f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.86f, 0.92f, 1f, 1f);
            light.intensity = 1.1f;
        }

        private static void BuildRecoveryVolume(Transform parent)
        {
            GameObject volume = new GameObject("FallRecovery");
            volume.transform.SetParent(parent);
            volume.transform.position = new Vector3(Width * 0.5f, -4f, 0f);
            BoxCollider2D trigger = volume.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(Width + 8f, 4f);
            volume.AddComponent<RecoveryVolume2D>();
        }

        private static void BuildExitVisual(Transform parent, SourceArt art)
        {
            GameObject exit = new GameObject("ToolFreeExit");
            exit.transform.SetParent(parent);
            exit.transform.position = new Vector3(
                P3ToolGardenContract.Exit.X + 0.5f,
                P3ToolGardenContract.Exit.Y + 0.7f,
                0f);
            CreatePart(exit.transform, art.Anchor, "ExitRing",
                Vector2.zero, new Vector2(0.62f, 1.75f), 0f,
                new Color(0.42f, 0.96f, 1f, 1f));
            CreatePart(exit.transform, art.Star, "ExitStar",
                new Vector2(0f, 0.25f), Vector2.one * 0.55f, 0f,
                new Color(1f, 0.88f, 0.28f, 1f));
        }

        private static void BuildCue(
            Transform parent,
            Transform player,
            SourceArt art,
            P3ToolKind kind,
            Vector2 position,
            Vector2 axis)
        {
            GameObject cueObject = new GameObject($"Cue_{(int)kind:00}_{kind}");
            cueObject.transform.SetParent(parent);
            cueObject.transform.position = position;
            SpriteRenderer visual = CreatePart(
                cueObject.transform,
                art.Star,
                "GestureDot",
                Vector2.zero,
                Vector2.one * 0.32f,
                0f,
                GetCueColor(kind));
            cueObject.AddComponent<NoTextToolCue2D>().Configure(
                kind,
                player,
                visual,
                axis,
                0.48f,
                1.05f + (int)kind * 0.03f,
                6f);
        }

        private static SpriteRenderer CreatePart(
            Transform parent,
            Sprite sprite,
            string name,
            Vector2 localPosition,
            Vector2 worldSize,
            float rotation,
            Color color)
        {
            GameObject visualObject = new GameObject(name);
            visualObject.transform.SetParent(parent, false);
            visualObject.transform.localPosition = localPosition;
            visualObject.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            visualObject.transform.localScale =
                CalculateFitScale(sprite, worldSize);
            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 40;
            return renderer;
        }

        private static void CreateBackdropSprite(
            Transform parent,
            Sprite sprite,
            string name,
            Vector2 position,
            Vector2 worldSize,
            int sortingOrder,
            Color color)
        {
            GameObject visualObject = new GameObject(name);
            visualObject.transform.SetParent(parent);
            visualObject.transform.position = new Vector3(position.x, position.y, 0f);
            visualObject.transform.localScale =
                CalculateFitScale(sprite, worldSize);
            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }

        private static GameObject InstantiatePrefab(
            GameObject prefab,
            Scene scene,
            Transform parent,
            string name,
            Vector2 position)
        {
            GameObject instance =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = name;
            instance.transform.SetParent(parent);
            instance.transform.position = new Vector3(position.x, position.y, 0f);
            return instance;
        }

        private static Tile RebuildTile(
            string path,
            string assetName,
            Sprite sprite,
            Color color,
            Tile.ColliderType colliderType,
            Vector2? fittedSize = null)
        {
            AssetDatabase.DeleteAsset(path);
            Vector2 size = fittedSize ?? Vector2.one;

            // Grid colliders inherit tile.transform, so collider tiles use a
            // 1 x 1 unit sprite and carry the fitted size in the matrix.
            bool normalize =
                colliderType != Tile.ColliderType.None && sprite != null;
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = assetName;
            tile.sprite = sprite;
            tile.color = color;
            tile.colliderType = colliderType;
            tile.transform = normalize
                ? Matrix4x4.TRS(
                    Vector3.zero,
                    Quaternion.identity,
                    new Vector3(size.x, size.y, 1f))
                : CalculateFitMatrix(sprite, size);
            AssetDatabase.CreateAsset(tile, path);
            if (normalize)
            {
                Sprite unitSprite = CreateUnitSprite(
                    sprite,
                    assetName + "_Unit");
                AssetDatabase.AddObjectToAsset(unitSprite, tile);
                tile.sprite = unitSprite;
                EditorUtility.SetDirty(tile);
            }

            return tile;
        }

        private static Sprite CreateUnitSprite(Sprite source, string spriteName)
        {
            Rect rect = source.rect;
            Vector2 pivot = new Vector2(
                rect.width > 0.001f ? source.pivot.x / rect.width : 0.5f,
                rect.height > 0.001f ? source.pivot.y / rect.height : 0.5f);
            Sprite unitSprite = Sprite.Create(
                source.texture,
                rect,
                pivot,
                rect.width > 0.001f ? rect.width : 1f,
                0,
                SpriteMeshType.FullRect);
            unitSprite.name = spriteName;
            return unitSprite;
        }

        private static TileDefinition RebuildDefinition(
            string path,
            string assetName,
            string id,
            Tile tile,
            TileMaterialKind kind,
            TileBreakMethod breakMethods,
            bool sacred = false)
        {
            AssetDatabase.DeleteAsset(path);
            TileDefinition definition =
                ScriptableObject.CreateInstance<TileDefinition>();
            definition.name = assetName;
            definition.Configure(
                id,
                tile,
                kind,
                true,
                breakMethods,
                sacred);
            AssetDatabase.CreateAsset(definition, path);
            return definition;
        }

        private static Sprite LoadSprite(string path, string spriteName = null)
        {
            UnityEngine.Object[] assets =
                AssetDatabase.LoadAllAssetsAtPath(path);
            Sprite fallback = null;
            for (int index = 0; index < assets.Length; index++)
            {
                if (!(assets[index] is Sprite sprite))
                {
                    continue;
                }

                fallback ??= sprite;
                if (string.IsNullOrEmpty(spriteName) || sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            return fallback;
        }

        private static Vector3 CalculateFitScale(Sprite sprite, Vector2 size)
        {
            if (sprite == null)
            {
                return Vector3.one;
            }

            Vector2 spriteSize = sprite.bounds.size;
            return new Vector3(
                spriteSize.x > 0.001f ? size.x / spriteSize.x : 1f,
                spriteSize.y > 0.001f ? size.y / spriteSize.y : 1f,
                1f);
        }

        private static Matrix4x4 CalculateFitMatrix(Sprite sprite, Vector2 size)
        {
            return Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.identity,
                CalculateFitScale(sprite, size));
        }

        private static GridPos[] BuildProtectedExitCells()
        {
            GridPos exit = P3ToolGardenContract.Exit;
            List<GridPos> cells = new List<GridPos>();
            for (int x = exit.X - 2; x <= exit.X + 2; x++)
            {
                for (int y = 0; y <= 4; y++)
                {
                    cells.Add(new GridPos(x, y));
                }
            }

            return cells.ToArray();
        }

        private static Color GetToolColor(HandToolKind kind)
        {
            switch (kind)
            {
                case HandToolKind.Pickaxe:
                    return new Color(0.54f, 0.82f, 1f, 1f);
                case HandToolKind.Shovel:
                    return new Color(0.94f, 0.67f, 0.30f, 1f);
                case HandToolKind.WateringCan:
                    return new Color(0.22f, 0.84f, 1f, 1f);
                case HandToolKind.Pestle:
                    return new Color(1f, 0.56f, 0.72f, 1f);
                case HandToolKind.Grapple:
                    return new Color(0.72f, 0.58f, 1f, 1f);
                case HandToolKind.WindUmbrella:
                    return new Color(0.48f, 1f, 0.82f, 1f);
                default:
                    return Color.white;
            }
        }

        private static Color GetCueColor(P3ToolKind kind)
        {
            float hue = ((int)kind - 1) / 8f;
            Color color = Color.HSVToRGB(hue, 0.48f, 1f);
            color.a = 0.94f;
            return color;
        }

        private static void EnsureOutputFolders()
        {
            EnsureFolder("Assets/StarNight/Scenes");
            EnsureFolder("Assets/StarNight/Scenes/Labs");
            EnsureFolder("Assets/StarNight/Data");
            EnsureFolder(DataFolder);
            EnsureFolder(TileFolder);
            EnsureFolder("Assets/StarNight/Prefabs");
            EnsureFolder("Assets/StarNight/Prefabs/Gameplay");
            EnsureFolder(PrefabFolder);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string folderName = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private sealed class SourceArt
        {
            public Sprite Ground;
            public Sprite GroundSide;
            public Sprite Sky;
            public Sprite MountainA;
            public Sprite MountainB;
            public Sprite Cloud;
            public Sprite Square;
            public Sprite Anchor;
            public Sprite ButtonSmall;
            public Sprite ButtonLarge;
            public Sprite Branch;
            public Sprite DryVine;
            public Sprite GrownVine;
            public Sprite Star;

            public bool IsComplete =>
                Ground != null
                && GroundSide != null
                && Sky != null
                && MountainA != null
                && MountainB != null
                && Cloud != null
                && Square != null
                && Anchor != null
                && ButtonSmall != null
                && ButtonLarge != null
                && Branch != null
                && DryVine != null
                && GrownVine != null
                && Star != null;
        }

        private sealed class GeneratedTiles
        {
            public Tile Reinforced;
            public Tile Stone;
            public Tile Dirt;
            public Tile Cracked;
            public Tile Exit;
            public Tile ThinFloor;
            public Tile Glow;
            public TileDefinition[] Definitions;

            public TileDefinition DirtDefinition =>
                Definitions != null && Definitions.Length > 2
                    ? Definitions[2]
                    : null;

            public bool IsComplete =>
                Reinforced != null
                && Stone != null
                && Dirt != null
                && Cracked != null
                && Exit != null
                && ThinFloor != null
                && Glow != null
                && Definitions != null
                && Array.TrueForAll(Definitions, definition => definition != null);
        }

        private sealed class ToolPrefabs
        {
            public GameObject Pickaxe;
            public GameObject Shovel;
            public GameObject WateringCan;
            public GameObject Pestle;
            public GameObject Grapple;
            public GameObject Umbrella;

            public bool IsComplete =>
                Pickaxe != null
                && Shovel != null
                && WateringCan != null
                && Pestle != null
                && Grapple != null
                && Umbrella != null;
        }
    }
}

#endif
