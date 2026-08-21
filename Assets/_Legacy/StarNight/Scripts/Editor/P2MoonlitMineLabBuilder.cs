#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Explosions;
using StarNight.Grid;
using StarNight.Objects;
using StarNight.Player;
using StarNight.Tiles;
using StarNight.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Editor
{
    /// <summary>
    /// Builds the fixed P2 mutation lab from the StarNight runtime and the
    /// 2D Fantasy Sprite Bundle. Terrain collision remains an exact 1 x 1 grid;
    /// imported art is fitted only at render time.
    /// </summary>
    public static class P2MoonlitMineLabBuilder
    {
        public const int Width = 48;
        public const int Height = 24;
        public const string ScenePath =
            "Assets/StarNight/Scenes/Labs/P2_MoonlitMineMutationLab_48x24.unity";
        public const string PlayerPrefabPath =
            "Assets/StarNight/Prefabs/Gameplay/P2_Player.prefab";
        public const string CratePrefabPath =
            "Assets/StarNight/Prefabs/Gameplay/P2_Crate.prefab";
        public const string RockPrefabPath =
            "Assets/StarNight/Prefabs/Gameplay/P2_Rock.prefab";
        public const string PressurePlatePrefabPath =
            "Assets/StarNight/Prefabs/Gameplay/P2_PressurePlate.prefab";
        public const string BombPrefabPath =
            "Assets/StarNight/Prefabs/Gameplay/P2_Bomb.prefab";
        public const string FallingRockPrefabPath =
            "Assets/StarNight/Prefabs/Gameplay/P2_FallingRock.prefab";

        private const string P1PlayerPrefabPath =
            "Assets/StarNight/Prefabs/Gameplay/P1_Player.prefab";
        private const string TuningPath =
            "Assets/StarNight/Settings/P1_MovementTuning.asset";

        private const string StoneSpritePath =
            "Assets/2D Fantasy sprite bundle/Dungeon pack/Sprites/stwall fill.png";
        private const string DirtSpritePath =
            "Assets/2D Fantasy sprite bundle/Desert pack/Sprites/Desert pack shape fill.png";
        private const string CrackSpritePath =
            "Assets/2D Fantasy sprite bundle/Ice and snow pack/Sprites/Ice crack.png";
        private const string CrystalSpritePath =
            "Assets/2D Fantasy sprite bundle/Dungeon pack/Sprites/cristals.png";
        private const string CrateSpritePath =
            "Assets/2D Fantasy sprite bundle/Dungeon pack/Sprites/dungeon items 2.png";
        private const string RockSpritePath =
            "Assets/2D Fantasy sprite bundle/Dungeon pack/Sprites/rocks.png";
        private const string PressurePlateSpritePath =
            "Assets/2D Fantasy sprite bundle/Abandoned station/Ancient base Sprites/Panel.png";
        private const string StarSpritePath =
            "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/Cristal Sprites/Star particle.png";
        private const string BackdropSpritePath =
            "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/Cristal Sprites/Background E.png";

        private const string StoneTilePath =
            "Assets/StarNight/Data/Tiles/P2_Moonstone.asset";
        private const string DirtTilePath =
            "Assets/StarNight/Data/Tiles/P2_MoonDirt.asset";
        private const string CrackedWallTilePath =
            "Assets/StarNight/Data/Tiles/P2_CrackedGoldWall.asset";
        private const string ReinforcedTilePath =
            "Assets/StarNight/Data/Tiles/P2_ReinforcedMoonstone.asset";
        private const string ExitFrameTilePath =
            "Assets/StarNight/Data/Tiles/P2_ExitFrame.asset";
        private const string CrackDecorationTilePath =
            "Assets/StarNight/Data/Tiles/P2_GoldCrackDecoration.asset";
        private const string CrystalDecorationTilePath =
            "Assets/StarNight/Data/Tiles/P2_GoldCrystalDecoration.asset";
        private const string LogicGlowTilePath =
            "Assets/StarNight/Data/Tiles/P2_LogicGlow.asset";

        private const string StoneDefinitionPath =
            "Assets/StarNight/Data/Tiles/P2_Moonstone_Definition.asset";
        private const string DirtDefinitionPath =
            "Assets/StarNight/Data/Tiles/P2_MoonDirt_Definition.asset";
        private const string CrackedWallDefinitionPath =
            "Assets/StarNight/Data/Tiles/P2_CrackedGoldWall_Definition.asset";
        private const string ReinforcedDefinitionPath =
            "Assets/StarNight/Data/Tiles/P2_ReinforcedMoonstone_Definition.asset";
        private const string ExitFrameDefinitionPath =
            "Assets/StarNight/Data/Tiles/P2_ExitFrame_Definition.asset";

        private static readonly Vector2 PlayerSpawn = new Vector2(2.5f, 1.45f);
        private static readonly GridPos RequiredStart = new GridPos(2, 1);
        private static readonly GridPos RequiredExit = new GridPos(45, 1);

        [MenuItem("StarNight/P2/Rebuild Moonlit Mine Mutation Lab")]
        public static void RebuildMoonlitMineMutationLab()
        {
            EnsureOutputFolders();

            SourceArt art = LoadSourceArt();
            GeneratedAssets generated = RebuildGeneratedAssets(art);
            RebuildGameplayPrefabs(art);

            AssetDatabase.SaveAssets();

            generated = LoadGeneratedAssets();
            GameObject playerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject cratePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CratePrefabPath);
            GameObject rockPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(RockPrefabPath);
            GameObject pressurePlatePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PressurePlatePrefabPath);
            GameObject bombPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(BombPrefabPath);
            GameObject fallingRockPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(FallingRockPrefabPath);
            P1MovementTuning tuning =
                AssetDatabase.LoadAssetAtPath<P1MovementTuning>(TuningPath);

            if (!generated.IsComplete
                || playerPrefab == null
                || cratePrefab == null
                || rockPrefab == null
                || pressurePlatePrefab == null
                || bombPrefab == null
                || fallingRockPrefab == null
                || tuning == null)
            {
                throw new InvalidOperationException(
                    "P2 generated assets failed to reload before scene assembly.");
            }

            BuildScene(
                art,
                generated,
                tuning,
                playerPrefab,
                cratePrefab,
                rockPrefab,
                pressurePlatePrefab,
                bombPrefab,
                fallingRockPrefab);

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[StarNight P2] Moonlit Mine mutation lab rebuilt: {ScenePath}");
        }

        private static SourceArt LoadSourceArt()
        {
            SourceArt art = new SourceArt
            {
                Stone = LoadSprite(StoneSpritePath),
                Dirt = LoadSprite(DirtSpritePath),
                Crack = LoadSprite(CrackSpritePath, "Ice crack_2"),
                Crystal = LoadSprite(CrystalSpritePath, "cristals_5"),
                Crate = LoadSprite(CrateSpritePath, "dungeon items 2_20"),
                Rock = LoadSprite(RockSpritePath, "rocks_0"),
                FallingRock = LoadSprite(RockSpritePath, "rocks_3"),
                PressurePlate = LoadSprite(PressurePlateSpritePath),
                Star = LoadSprite(StarSpritePath),
                Backdrop = LoadSprite(BackdropSpritePath)
            };

            if (!art.IsComplete)
            {
                throw new InvalidOperationException(
                    "P2 theme art could not be loaded from the 2D Fantasy Sprite Bundle.");
            }

            return art;
        }

        private static GeneratedAssets RebuildGeneratedAssets(SourceArt art)
        {
            Tile stone = RebuildTile(
                StoneTilePath,
                "P2_Moonstone",
                art.Stone,
                new Color(0.31f, 0.38f, 0.58f, 1f),
                Tile.ColliderType.Grid,
                Vector2.one);
            Tile dirt = RebuildTile(
                DirtTilePath,
                "P2_MoonDirt",
                art.Dirt,
                new Color(0.40f, 0.28f, 0.29f, 1f),
                Tile.ColliderType.Grid,
                Vector2.one);
            Tile crackedWall = RebuildTile(
                CrackedWallTilePath,
                "P2_CrackedGoldWall",
                art.Stone,
                new Color(0.46f, 0.34f, 0.24f, 1f),
                Tile.ColliderType.Grid,
                Vector2.one);
            Tile reinforced = RebuildTile(
                ReinforcedTilePath,
                "P2_ReinforcedMoonstone",
                art.Stone,
                new Color(0.22f, 0.25f, 0.42f, 1f),
                Tile.ColliderType.Grid,
                Vector2.one);
            Tile exitFrame = RebuildTile(
                ExitFrameTilePath,
                "P2_ExitFrame",
                art.Stone,
                new Color(0.62f, 0.48f, 0.20f, 1f),
                Tile.ColliderType.Grid,
                Vector2.one);
            Tile crackDecoration = RebuildTile(
                CrackDecorationTilePath,
                "P2_GoldCrackDecoration",
                art.Crack,
                new Color(1f, 0.67f, 0.12f, 0.96f),
                Tile.ColliderType.None,
                new Vector2(0.82f, 0.82f));
            Tile crystalDecoration = RebuildTile(
                CrystalDecorationTilePath,
                "P2_GoldCrystalDecoration",
                art.Crystal,
                new Color(1f, 0.69f, 0.18f, 0.92f),
                Tile.ColliderType.None,
                new Vector2(0.74f, 0.74f));
            Tile logicGlow = RebuildTile(
                LogicGlowTilePath,
                "P2_LogicGlow",
                art.Star,
                new Color(0.44f, 0.96f, 1f, 0.80f),
                Tile.ColliderType.None,
                new Vector2(0.52f, 0.52f));

            TileDefinition stoneDefinition = RebuildDefinition(
                StoneDefinitionPath,
                "P2_Moonstone_Definition",
                "moonstone",
                stone,
                TileMaterialKind.Stone,
                TileBreakMethod.Pickaxe);
            TileDefinition dirtDefinition = RebuildDefinition(
                DirtDefinitionPath,
                "P2_MoonDirt_Definition",
                "moon_dirt",
                dirt,
                TileMaterialKind.Dirt,
                TileBreakMethod.Bomb | TileBreakMethod.Shovel);
            TileDefinition crackedWallDefinition = RebuildDefinition(
                CrackedWallDefinitionPath,
                "P2_CrackedGoldWall_Definition",
                "cracked_gold_wall",
                crackedWall,
                TileMaterialKind.CrackedWall,
                TileBreakMethod.Bomb | TileBreakMethod.Pickaxe);
            TileDefinition reinforcedDefinition = RebuildDefinition(
                ReinforcedDefinitionPath,
                "P2_ReinforcedMoonstone_Definition",
                "reinforced_moonstone",
                reinforced,
                TileMaterialKind.ReinforcedWall,
                TileBreakMethod.None,
                true);
            TileDefinition exitFrameDefinition = RebuildDefinition(
                ExitFrameDefinitionPath,
                "P2_ExitFrame_Definition",
                "exit_frame",
                exitFrame,
                TileMaterialKind.ExitFrame,
                TileBreakMethod.None,
                true);

            return new GeneratedAssets
            {
                Stone = stone,
                Dirt = dirt,
                CrackedWall = crackedWall,
                Reinforced = reinforced,
                ExitFrame = exitFrame,
                CrackDecoration = crackDecoration,
                CrystalDecoration = crystalDecoration,
                LogicGlow = logicGlow,
                Definitions = new[]
                {
                    stoneDefinition,
                    dirtDefinition,
                    crackedWallDefinition,
                    reinforcedDefinition,
                    exitFrameDefinition
                }
            };
        }

        private static GeneratedAssets LoadGeneratedAssets()
        {
            return new GeneratedAssets
            {
                Stone = AssetDatabase.LoadAssetAtPath<Tile>(StoneTilePath),
                Dirt = AssetDatabase.LoadAssetAtPath<Tile>(DirtTilePath),
                CrackedWall =
                    AssetDatabase.LoadAssetAtPath<Tile>(CrackedWallTilePath),
                Reinforced =
                    AssetDatabase.LoadAssetAtPath<Tile>(ReinforcedTilePath),
                ExitFrame =
                    AssetDatabase.LoadAssetAtPath<Tile>(ExitFrameTilePath),
                CrackDecoration =
                    AssetDatabase.LoadAssetAtPath<Tile>(CrackDecorationTilePath),
                CrystalDecoration =
                    AssetDatabase.LoadAssetAtPath<Tile>(CrystalDecorationTilePath),
                LogicGlow =
                    AssetDatabase.LoadAssetAtPath<Tile>(LogicGlowTilePath),
                Definitions = new[]
                {
                    AssetDatabase.LoadAssetAtPath<TileDefinition>(
                        StoneDefinitionPath),
                    AssetDatabase.LoadAssetAtPath<TileDefinition>(
                        DirtDefinitionPath),
                    AssetDatabase.LoadAssetAtPath<TileDefinition>(
                        CrackedWallDefinitionPath),
                    AssetDatabase.LoadAssetAtPath<TileDefinition>(
                        ReinforcedDefinitionPath),
                    AssetDatabase.LoadAssetAtPath<TileDefinition>(
                        ExitFrameDefinitionPath)
                }
            };
        }

        private static Tile RebuildTile(
            string path,
            string assetName,
            Sprite sprite,
            Color color,
            Tile.ColliderType colliderType,
            Vector2 targetSize)
        {
            AssetDatabase.DeleteAsset(path);
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
                    new Vector3(targetSize.x, targetSize.y, 1f))
                : CalculateFitMatrix(sprite, targetSize);
            AssetDatabase.CreateAsset(tile, path);
            if (normalize)
            {
                Sprite unitSprite = CreateUnitSprite(sprite, assetName);
                AssetDatabase.AddObjectToAsset(unitSprite, tile);
                tile.sprite = unitSprite;
                EditorUtility.SetDirty(tile);
            }

            return tile;
        }

        private static Sprite CreateUnitSprite(Sprite source, string assetName)
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
            unitSprite.name = assetName + "_UnitSprite";
            return unitSprite;
        }

        private static TileDefinition RebuildDefinition(
            string path,
            string assetName,
            string id,
            TileBase tile,
            TileMaterialKind material,
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
                material,
                true,
                breakMethods,
                sacred);
            AssetDatabase.CreateAsset(definition, path);
            return definition;
        }

        private static void RebuildGameplayPrefabs(SourceArt art)
        {
            RebuildPlayerPrefab();
            RebuildCarryablePrefab(
                CratePrefabPath,
                "P2_Crate",
                art.Crate,
                new Vector2(0.84f, 0.84f),
                WorldObjectTraits.Carryable
                    | WorldObjectTraits.Pullable
                    | WorldObjectTraits.Breakable,
                1.15f,
                6.25f,
                new Color(0.95f, 0.80f, 0.55f, 1f));
            RebuildCarryablePrefab(
                RockPrefabPath,
                "P2_Rock",
                art.Rock,
                new Vector2(0.78f, 0.78f),
                WorldObjectTraits.Carryable | WorldObjectTraits.Pullable,
                1.45f,
                5.6f,
                new Color(0.64f, 0.72f, 0.92f, 1f));
            RebuildPressurePlatePrefab(art.PressurePlate);
            RebuildBombPrefab(art.Rock, art.Star);
            RebuildFallingRockPrefab(art.FallingRock);
        }

        private static void RebuildPlayerPrefab()
        {
            AssetDatabase.DeleteAsset(PlayerPrefabPath);
            GameObject p1Player =
                AssetDatabase.LoadAssetAtPath<GameObject>(P1PlayerPrefabPath);
            if (p1Player == null)
            {
                throw new InvalidOperationException(
                    "P1 player prefab is required as the preserved P2 player base.");
            }

            GameObject contents =
                PrefabUtility.LoadPrefabContents(P1PlayerPrefabPath);
            try
            {
                contents.name = "P2_Player";
                Transform anchor = contents.transform.Find("CarryAnchor");
                if (anchor == null)
                {
                    GameObject anchorObject = new GameObject("CarryAnchor");
                    anchor = anchorObject.transform;
                    anchor.SetParent(contents.transform, false);
                    anchor.localPosition = new Vector3(0.62f, 0.15f, 0f);
                }

                PlayerInputAdapter input =
                    contents.GetComponent<PlayerInputAdapter>();
                Rigidbody2D body = contents.GetComponent<Rigidbody2D>();
                CarrySystem carry = contents.GetComponent<CarrySystem>();
                if (carry == null)
                {
                    carry = contents.AddComponent<CarrySystem>();
                }

                carry.Configure(input, body, anchor);
                PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void RebuildCarryablePrefab(
            string path,
            string prefabName,
            Sprite sprite,
            Vector2 colliderSize,
            WorldObjectTraits traits,
            float mass,
            float throwImpulse,
            Color tint)
        {
            AssetDatabase.DeleteAsset(path);
            GameObject root = new GameObject(prefabName);
            try
            {
                Rigidbody2D body = ConfigureDynamicBody(root, mass);
                BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
                collider.size = colliderSize;
                CreateSpriteVisual(
                    root.transform,
                    "Visual",
                    sprite,
                    Vector3.zero,
                    colliderSize,
                    tint,
                    10);
                CarryableObject2D carryable =
                    root.AddComponent<CarryableObject2D>();
                carryable.Configure(
                    null,
                    body,
                    collider,
                    traits,
                    mass,
                    throwImpulse);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void RebuildPressurePlatePrefab(Sprite sprite)
        {
            AssetDatabase.DeleteAsset(PressurePlatePrefabPath);
            GameObject root = new GameObject("P2_PressurePlate");
            try
            {
                Rigidbody2D body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                body.freezeRotation = true;

                BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
                trigger.size = new Vector2(0.90f, 0.18f);
                trigger.isTrigger = true;
                SpriteRenderer visual = CreateSpriteVisual(
                    root.transform,
                    "Visual",
                    sprite,
                    Vector3.zero,
                    new Vector2(0.92f, 0.26f),
                    new Color(0.76f, 0.55f, 0.22f, 1f),
                    9);
                PressurePlate2D plate = root.AddComponent<PressurePlate2D>();
                plate.Configure(trigger, visual);
                PrefabUtility.SaveAsPrefabAsset(root, PressurePlatePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void RebuildBombPrefab(Sprite shellSprite, Sprite starSprite)
        {
            AssetDatabase.DeleteAsset(BombPrefabPath);
            GameObject root = new GameObject("P2_Bomb");
            try
            {
                Rigidbody2D body = ConfigureDynamicBody(root, 0.80f);
                CircleCollider2D collider =
                    root.AddComponent<CircleCollider2D>();
                collider.radius = 0.38f;
                CreateSpriteVisual(
                    root.transform,
                    "MoonstoneShell",
                    shellSprite,
                    Vector3.zero,
                    new Vector2(0.76f, 0.76f),
                    new Color(0.16f, 0.18f, 0.32f, 1f),
                    10);
                CreateSpriteVisual(
                    root.transform,
                    "FuseStar",
                    starSprite,
                    new Vector3(0.20f, 0.34f, 0f),
                    new Vector2(0.28f, 0.28f),
                    new Color(1f, 0.58f, 0.12f, 1f),
                    11);

                CarryableObject2D carryable =
                    root.AddComponent<CarryableObject2D>();
                carryable.Configure(
                    null,
                    body,
                    collider,
                    WorldObjectTraits.Carryable
                        | WorldObjectTraits.Pullable
                        | WorldObjectTraits.Breakable,
                    0.80f,
                    6.8f);
                Bomb2D bomb = root.AddComponent<Bomb2D>();
                bomb.Configure(
                    null,
                    ExplosionConstants.BombFuseSeconds,
                    false,
                    true);
                PrefabUtility.SaveAsPrefabAsset(root, BombPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void RebuildFallingRockPrefab(Sprite sprite)
        {
            AssetDatabase.DeleteAsset(FallingRockPrefabPath);
            GameObject root = new GameObject("P2_FallingRock");
            try
            {
                Rigidbody2D body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Static;
                body.gravityScale = 0f;
                body.mass = 2.4f;
                body.freezeRotation = true;
                body.collisionDetectionMode =
                    CollisionDetectionMode2D.Continuous;
                CircleCollider2D collider =
                    root.AddComponent<CircleCollider2D>();
                collider.radius = 0.43f;
                CreateSpriteVisual(
                    root.transform,
                    "Visual",
                    sprite,
                    Vector3.zero,
                    new Vector2(0.92f, 0.92f),
                    new Color(0.60f, 0.68f, 0.88f, 1f),
                    10);
                FallingObject2D falling =
                    root.AddComponent<FallingObject2D>();
                falling.Configure(null, null, body, collider);
                PrefabUtility.SaveAsPrefabAsset(root, FallingRockPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Rigidbody2D ConfigureDynamicBody(
            GameObject root,
            float mass)
        {
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 2.25f;
            body.mass = mass;
            body.linearDamping = 0.05f;
            body.angularDamping = 0.05f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            return body;
        }

        private static void BuildScene(
            SourceArt art,
            GeneratedAssets assets,
            P1MovementTuning tuning,
            GameObject playerPrefab,
            GameObject cratePrefab,
            GameObject rockPrefab,
            GameObject pressurePlatePrefab,
            GameObject bombPrefab,
            GameObject fallingRockPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            scene.name = "P2_MoonlitMineMutationLab_48x24";

            art = LoadSourceArt();
            assets = LoadGeneratedAssets();
            tuning = AssetDatabase.LoadAssetAtPath<P1MovementTuning>(TuningPath);
            playerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            cratePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CratePrefabPath);
            rockPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(RockPrefabPath);
            pressurePlatePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PressurePlatePrefabPath);
            bombPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(BombPrefabPath);
            fallingRockPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    FallingRockPrefabPath);
            if (!art.IsComplete
                || !assets.IsComplete
                || tuning == null
                || playerPrefab == null
                || cratePrefab == null
                || rockPrefab == null
                || pressurePlatePrefab == null
                || bombPrefab == null
                || fallingRockPrefab == null)
            {
                throw new InvalidOperationException(
                    "P2 assets did not survive the scene switch.");
            }

            GameObject labRoot =
                new GameObject("P2_MoonlitMineMutationLab_48x24");
            BuildBackdrop(labRoot.transform, art);

            GameObject gridRoot = new GameObject("GridWorld");
            gridRoot.transform.SetParent(labRoot.transform);
            UnityEngine.Grid grid =
                gridRoot.AddComponent<UnityEngine.Grid>();
            grid.cellSize = Vector3.one;
            grid.cellGap = Vector3.zero;

            Tilemap terrain = CreateTilemapLayer(
                gridRoot.transform,
                "Terrain",
                0,
                true,
                out TilemapCollider2D terrainCollider,
                out CompositeCollider2D composite);
            CreateTilemapLayer(
                gridRoot.transform,
                "OneWay",
                1,
                false,
                out _,
                out _);
            CreateTilemapLayer(
                gridRoot.transform,
                "Fixture",
                2,
                false,
                out _,
                out _);
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

            GridWorld gridWorld = gridRoot.AddComponent<GridWorld>();
            gridWorld.Configure(
                grid,
                terrain,
                hazard,
                Vector2Int.zero,
                new Vector2Int(Width, Height));

            HashSet<Vector3Int> crackedCells =
                PopulateTerrain(terrain, assets);
            PopulateDecoration(
                decoration,
                logic,
                assets,
                crackedCells);

            terrain.RefreshAllTiles();
            decoration.RefreshAllTiles();
            logic.RefreshAllTiles();
            terrainCollider.ProcessTilemapChanges();
            composite.GenerateGeometry();
            EditorUtility.SetDirty(terrain);
            EditorUtility.SetDirty(decoration);
            EditorUtility.SetDirty(logic);
            Physics2D.SyncTransforms();

            if (terrain.GetUsedTilesCount() == 0 || composite.pathCount == 0)
            {
                throw new InvalidOperationException(
                    "P2 terrain or composite collision generation failed.");
            }

            GameObject player = InstantiatePrefab(
                playerPrefab,
                scene,
                labRoot.transform,
                "Player",
                PlayerSpawn);
            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
            CapsuleCollider2D playerCollider =
                player.GetComponent<CapsuleCollider2D>();
            PlayerInputAdapter playerInput =
                player.GetComponent<PlayerInputAdapter>();
            PlayerMotor2D motor = player.GetComponent<PlayerMotor2D>();
            SafeCellTracker safeCellTracker =
                player.GetComponent<SafeCellTracker>();
            PlayerRecovery recovery =
                player.GetComponent<PlayerRecovery>();
            CarrySystem carry = player.GetComponent<CarrySystem>();
            Transform carryAnchor = player.transform.Find("CarryAnchor");

            playerBody.position = PlayerSpawn;
            safeCellTracker.Configure(
                gridWorld,
                playerBody,
                playerCollider,
                motor,
                tuning);
            safeCellTracker.SetSpawnFallback(PlayerSpawn);
            recovery.Configure(
                gridWorld,
                playerBody,
                motor,
                safeCellTracker,
                tuning);
            carry.Configure(
                playerInput,
                playerBody,
                carryAnchor,
                gridWorld);

            GameObject systems = new GameObject("P2_Systems");
            systems.transform.SetParent(labRoot.transform);
            TileMutationService mutationService =
                systems.AddComponent<TileMutationService>();
            GridPos[] protectedExitCells = BuildProtectedExitCells();
            mutationService.Configure(
                gridWorld,
                terrain,
                decoration,
                terrainCollider,
                composite,
                playerCollider,
                assets.Definitions,
                RequiredStart,
                RequiredExit,
                protectedExitCells);

            ExplosionService2D explosionService =
                systems.AddComponent<ExplosionService2D>();
            explosionService.Configure(
                gridWorld,
                mutationService,
                ExplosionConstants.DefaultChainHardCap,
                7f,
                ~0);

            GameObject objectsRoot = new GameObject("P2_Objects");
            objectsRoot.transform.SetParent(labRoot.transform);
            BuildCarryAndPressureZone(
                scene,
                objectsRoot.transform,
                gridWorld,
                cratePrefab,
                rockPrefab,
                pressurePlatePrefab);
            BuildFallingZone(
                scene,
                objectsRoot.transform,
                gridWorld,
                mutationService,
                explosionService,
                pressurePlatePrefab,
                bombPrefab,
                fallingRockPrefab);
            BuildExplosionChainZone(
                scene,
                objectsRoot.transform,
                gridWorld,
                explosionService,
                bombPrefab);
            BuildExitProtectionDemo(
                scene,
                objectsRoot.transform,
                gridWorld,
                mutationService,
                explosionService,
                playerCollider,
                protectedExitCells,
                rockPrefab,
                art.Star);

            BuildRecoveryVolume(labRoot.transform);

            Camera camera = BuildCamera(labRoot.transform);
            GridBoundedCamera2D cameraFollow =
                camera.gameObject.AddComponent<GridBoundedCamera2D>();
            cameraFollow.Configure(
                camera,
                player.transform,
                playerBody,
                gridWorld,
                recovery);

            BuildDirectionalLight(labRoot.transform);
            BuildLabels(labRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = player;
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

        private static HashSet<Vector3Int> PopulateTerrain(
            Tilemap terrain,
            GeneratedAssets assets)
        {
            for (int x = 0; x < Width; x++)
            {
                terrain.SetTile(new Vector3Int(x, 0, 0), assets.Stone);
                terrain.SetTile(
                    new Vector3Int(x, Height - 1, 0),
                    assets.Reinforced);
            }

            for (int y = 0; y < Height; y++)
            {
                terrain.SetTile(new Vector3Int(0, y, 0), assets.Reinforced);
                terrain.SetTile(
                    new Vector3Int(Width - 1, y, 0),
                    assets.Reinforced);
            }

            // Readable one-cell material samples near the start.
            terrain.SetTile(new Vector3Int(5, 1, 0), assets.Stone);
            terrain.SetTile(new Vector3Int(7, 1, 0), assets.Dirt);
            terrain.SetTile(new Vector3Int(9, 1, 0), assets.CrackedWall);

            // Falling-rock support. An adjacent bomb's centered 3 x 3 mask
            // removes this support and converts the rock to a dynamic body.
            terrain.SetTile(new Vector3Int(23, 2, 0), assets.CrackedWall);

            // Chain-reaction material bank.
            terrain.SetTile(new Vector3Int(31, 2, 0), assets.Dirt);
            terrain.SetTile(new Vector3Int(32, 2, 0), assets.CrackedWall);
            terrain.SetTile(new Vector3Int(33, 2, 0), assets.Dirt);
            terrain.SetTile(new Vector3Int(34, 2, 0), assets.CrackedWall);

            // The exit cell (45,1) stays open. Its frame and all nearby cells
            // are also protected by TileMutationService.
            terrain.SetTile(new Vector3Int(46, 1, 0), assets.ExitFrame);
            terrain.SetTile(new Vector3Int(44, 2, 0), assets.ExitFrame);
            terrain.SetTile(new Vector3Int(46, 2, 0), assets.ExitFrame);
            terrain.SetTile(new Vector3Int(45, 3, 0), assets.ExitFrame);
            terrain.SetTile(new Vector3Int(44, 0, 0), assets.ExitFrame);
            terrain.SetTile(new Vector3Int(45, 0, 0), assets.ExitFrame);
            terrain.SetTile(new Vector3Int(46, 0, 0), assets.ExitFrame);

            return new HashSet<Vector3Int>
            {
                new Vector3Int(9, 1, 0),
                new Vector3Int(23, 2, 0),
                new Vector3Int(32, 2, 0),
                new Vector3Int(34, 2, 0)
            };
        }

        private static void PopulateDecoration(
            Tilemap decoration,
            Tilemap logic,
            GeneratedAssets assets,
            HashSet<Vector3Int> crackedCells)
        {
            foreach (Vector3Int cell in crackedCells)
            {
                decoration.SetTile(cell, assets.CrackDecoration);
            }

            Vector3Int[] crystalCells =
            {
                new Vector3Int(3, 1, 0),
                new Vector3Int(12, 1, 0),
                new Vector3Int(19, 1, 0),
                new Vector3Int(27, 1, 0),
                new Vector3Int(38, 1, 0),
                new Vector3Int(42, 1, 0),
                new Vector3Int(17, 5, 0),
                new Vector3Int(28, 7, 0),
                new Vector3Int(40, 4, 0)
            };
            for (int index = 0; index < crystalCells.Length; index++)
            {
                decoration.SetTile(
                    crystalCells[index],
                    assets.CrystalDecoration);
            }

            logic.SetTile(
                new Vector3Int(RequiredStart.X, RequiredStart.Y + 1, 0),
                assets.LogicGlow);
            logic.SetTile(
                new Vector3Int(RequiredExit.X, RequiredExit.Y + 1, 0),
                assets.LogicGlow);
        }

        private static void BuildCarryAndPressureZone(
            Scene scene,
            Transform parent,
            GridWorld world,
            GameObject cratePrefab,
            GameObject rockPrefab,
            GameObject pressurePlatePrefab)
        {
            GameObject zone = new GameObject("Carry_And_Pressure_Zone");
            zone.transform.SetParent(parent);

            GameObject crate = InstantiatePrefab(
                cratePrefab,
                scene,
                zone.transform,
                "Carryable_Crate",
                new Vector2(13.5f, 1.45f));
            crate.GetComponent<CarryableObject2D>().Configure(
                world,
                WorldObjectTraits.Carryable
                    | WorldObjectTraits.Pullable
                    | WorldObjectTraits.Breakable,
                1.15f,
                6.25f);

            GameObject rock = InstantiatePrefab(
                rockPrefab,
                scene,
                zone.transform,
                "Carryable_Rock",
                new Vector2(15.5f, 1.43f));
            rock.GetComponent<CarryableObject2D>().Configure(
                world,
                WorldObjectTraits.Carryable | WorldObjectTraits.Pullable,
                1.45f,
                5.6f);

            GameObject plate = InstantiatePrefab(
                pressurePlatePrefab,
                scene,
                zone.transform,
                "Pressure_Plate",
                new Vector2(17.5f, 1.10f));
            plate.GetComponent<PressurePlate2D>().Configure(
                plate.GetComponent<Collider2D>(),
                plate.GetComponentInChildren<SpriteRenderer>());
        }

        private static void BuildFallingZone(
            Scene scene,
            Transform parent,
            GridWorld world,
            TileMutationService mutationService,
            ExplosionService2D explosionService,
            GameObject pressurePlatePrefab,
            GameObject bombPrefab,
            GameObject fallingRockPrefab)
        {
            GameObject zone = new GameObject("Falling_Rock_Zone");
            zone.transform.SetParent(parent);

            GameObject plate = InstantiatePrefab(
                pressurePlatePrefab,
                scene,
                zone.transform,
                "Falling_Rock_Plate",
                new Vector2(23.5f, 1.10f));
            plate.GetComponent<PressurePlate2D>().Configure(
                plate.GetComponent<Collider2D>(),
                plate.GetComponentInChildren<SpriteRenderer>());

            GameObject fallingRock = InstantiatePrefab(
                fallingRockPrefab,
                scene,
                zone.transform,
                "Falling_Rock",
                new Vector2(23.5f, 3.45f));
            fallingRock.GetComponent<FallingObject2D>().Configure(
                world,
                mutationService,
                fallingRock.GetComponent<Rigidbody2D>(),
                fallingRock.GetComponent<Collider2D>());

            GameObject supportBomb = InstantiatePrefab(
                bombPrefab,
                scene,
                zone.transform,
                "Support_Bomb",
                new Vector2(22.5f, 1.42f));
            supportBomb.GetComponent<CarryableObject2D>().Configure(
                world,
                WorldObjectTraits.Carryable
                    | WorldObjectTraits.Pullable
                    | WorldObjectTraits.Breakable,
                0.80f,
                6.8f);
            supportBomb.GetComponent<Bomb2D>().Configure(
                explosionService,
                ExplosionConstants.BombFuseSeconds,
                false,
                true,
                1);
        }

        private static void BuildExplosionChainZone(
            Scene scene,
            Transform parent,
            GridWorld world,
            ExplosionService2D explosionService,
            GameObject bombPrefab)
        {
            GameObject zone = new GameObject("Explosion_Chain_Zone");
            zone.transform.SetParent(parent);

            for (int index = 0; index < 4; index++)
            {
                GameObject bomb = InstantiatePrefab(
                    bombPrefab,
                    scene,
                    zone.transform,
                    $"Chain_Bomb_{index + 1:00}",
                    new Vector2(31.5f + index, 1.42f));
                bomb.GetComponent<CarryableObject2D>().Configure(
                    world,
                    WorldObjectTraits.Carryable
                        | WorldObjectTraits.Pullable
                        | WorldObjectTraits.Breakable,
                    0.80f,
                    6.8f);
                bomb.GetComponent<Bomb2D>().Configure(
                    explosionService,
                    ExplosionConstants.BombFuseSeconds,
                    false,
                    true,
                    100 + index);
            }
        }

        private static void BuildExitProtectionDemo(
            Scene scene,
            Transform parent,
            GridWorld world,
            TileMutationService mutationService,
            ExplosionService2D explosionService,
            Collider2D playerCollider,
            GridPos[] protectedExitCells,
            GameObject rockPrefab,
            Sprite starSprite)
        {
            GameObject zone = new GameObject("Exit_Protection_Zone");
            zone.transform.SetParent(parent);
            zone.transform.position = new Vector3(45.5f, 2f, 0f);
            BoxCollider2D trigger = zone.AddComponent<BoxCollider2D>();
            trigger.size = new Vector2(3f, 4f);
            trigger.isTrigger = true;

            GameObject relocationAnchor =
                new GameObject("Safe_Relocation_Anchor");
            relocationAnchor.transform.SetParent(parent);
            relocationAnchor.transform.position =
                new Vector3(41.5f, 1.5f, 0f);

            CreateSpriteVisual(
                zone.transform,
                "Exit_Beacon",
                starSprite,
                new Vector3(0f, 0.6f, 0f),
                new Vector2(0.92f, 0.92f),
                new Color(1f, 0.70f, 0.18f, 1f),
                20);

            GameObject blocker = InstantiatePrefab(
                rockPrefab,
                scene,
                parent,
                "Exit_Blocker_Demonstration_Rock",
                new Vector2(42.5f, 1.43f));
            blocker.GetComponent<CarryableObject2D>().Configure(
                world,
                WorldObjectTraits.Carryable | WorldObjectTraits.Pullable,
                1.45f,
                5.6f);

            ExitBlockerResolver2D resolver =
                zone.AddComponent<ExitBlockerResolver2D>();
            resolver.Configure(
                world,
                mutationService,
                explosionService,
                playerCollider,
                RequiredStart,
                RequiredExit,
                protectedExitCells);
        }

        private static void BuildRecoveryVolume(Transform parent)
        {
            GameObject recoveryObject =
                new GameObject("World_Bottom_Recovery_Volume");
            recoveryObject.transform.SetParent(parent);
            recoveryObject.transform.position =
                new Vector3(Width * 0.5f, -1.5f, 0f);
            BoxCollider2D trigger =
                recoveryObject.AddComponent<BoxCollider2D>();
            trigger.size = new Vector2(Width, 2f);
            trigger.isTrigger = true;
            recoveryObject.AddComponent<RecoveryVolume2D>();
        }

        private static void BuildBackdrop(Transform parent, SourceArt art)
        {
            GameObject backdrop = new GameObject("Moonlit_Mine_Backdrop");
            backdrop.transform.SetParent(parent);
            Color[] tints =
            {
                new Color(0.22f, 0.30f, 0.50f, 0.68f),
                new Color(0.26f, 0.22f, 0.48f, 0.72f),
                new Color(0.18f, 0.34f, 0.48f, 0.66f)
            };

            for (int index = 0; index < 3; index++)
            {
                CreateSpriteVisual(
                    backdrop.transform,
                    $"Cavern_Veil_{index + 1}",
                    art.Backdrop,
                    new Vector3(8f + index * 16f, 11f, 0f),
                    new Vector2(19f, 23f),
                    tints[index],
                    -100 + index);
            }

            for (int index = 0; index < 8; index++)
            {
                float x = 3.5f + index * 5.8f;
                float y = index % 2 == 0 ? 5.2f : 8.5f;
                CreateSpriteVisual(
                    backdrop.transform,
                    $"Distant_Crystal_{index + 1:00}",
                    art.Crystal,
                    new Vector3(x, y, 0f),
                    new Vector2(0.8f, 1.25f),
                    new Color(0.45f, 0.62f, 1f, 0.34f),
                    -80);
            }
        }

        private static Camera BuildCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent);
            cameraObject.transform.position =
                new Vector3(6.5f, 5.5f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor =
                new Color(0.018f, 0.024f, 0.075f, 1f);
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void BuildDirectionalLight(Transform parent)
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(parent);
            lightObject.transform.rotation =
                Quaternion.Euler(35f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.color = new Color(0.70f, 0.82f, 1f, 1f);
        }

        private static void BuildLabels(Transform parent)
        {
            CreateLabel(
                parent,
                "Lab_Title",
                "P2  MOONLIT MINE MUTATION LAB  48 x 24",
                new Vector3(8.5f, 6.1f, 0f),
                0.095f);
            CreateLabel(
                parent,
                "Materials_Label",
                "STONE   DIRT   CRACK",
                new Vector3(7.4f, 3.5f, 0f),
                0.060f);
            CreateLabel(
                parent,
                "Carry_Label",
                "CARRY + PRESS",
                new Vector3(15.2f, 4.0f, 0f),
                0.060f);
            CreateLabel(
                parent,
                "Falling_Label",
                "SUPPORT -> FALL -> PRESS",
                new Vector3(23.5f, 5.2f, 0f),
                0.060f);
            CreateLabel(
                parent,
                "Explosion_Label",
                "1.8s   3 x 3   CHAIN",
                new Vector3(33.0f, 4.5f, 0f),
                0.060f);
            CreateLabel(
                parent,
                "Exit_Label",
                "PROTECTED EXIT",
                new Vector3(44.7f, 5.1f, 0f),
                0.060f);
        }

        private static void CreateLabel(
            Transform parent,
            string name,
            string text,
            Vector3 position,
            float characterSize)
        {
            GameObject labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent);
            labelObject.transform.position = position;
            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = characterSize;
            textMesh.color = new Color(0.74f, 0.88f, 1f, 0.82f);
        }

        private static SpriteRenderer CreateSpriteVisual(
            Transform parent,
            string name,
            Sprite sprite,
            Vector3 localPosition,
            Vector2 targetSize,
            Color color,
            int sortingOrder)
        {
            GameObject visual = new GameObject(name);
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale =
                CalculateFitScale(sprite, targetSize);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
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
            instance.transform.position =
                new Vector3(position.x, position.y, 0f);
            Rigidbody2D body = instance.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = position;
            }

            return instance;
        }

        private static GridPos[] BuildProtectedExitCells()
        {
            List<GridPos> cells = new List<GridPos>();
            for (int x = 44; x <= 46; x++)
            {
                for (int y = 0; y <= 2; y++)
                {
                    cells.Add(new GridPos(x, y));
                }
            }

            cells.Add(new GridPos(45, 3));
            return cells.ToArray();
        }

        private static Sprite LoadSprite(
            string assetPath,
            string spriteName = null)
        {
            UnityEngine.Object[] assets =
                AssetDatabase.LoadAllAssetsAtPath(assetPath);
            Sprite fallback = null;
            for (int index = 0; index < assets.Length; index++)
            {
                if (!(assets[index] is Sprite sprite))
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = sprite;
                }

                if (!string.IsNullOrEmpty(spriteName)
                    && string.Equals(
                        sprite.name,
                        spriteName,
                        StringComparison.Ordinal))
                {
                    return sprite;
                }
            }

            return string.IsNullOrEmpty(spriteName)
                ? fallback
                : null;
        }

        private static Vector3 CalculateFitScale(
            Sprite sprite,
            Vector2 targetSize)
        {
            if (sprite == null)
            {
                return Vector3.one;
            }

            Vector2 source = sprite.bounds.size;
            return new Vector3(
                targetSize.x / Mathf.Max(0.001f, source.x),
                targetSize.y / Mathf.Max(0.001f, source.y),
                1f);
        }

        private static Matrix4x4 CalculateFitMatrix(
            Sprite sprite,
            Vector2 targetSize)
        {
            return Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.identity,
                CalculateFitScale(sprite, targetSize));
        }

        private static void EnsureOutputFolders()
        {
            EnsureFolder("Assets/StarNight/Scenes");
            EnsureFolder("Assets/StarNight/Scenes/Labs");
            EnsureFolder("Assets/StarNight/Data");
            EnsureFolder("Assets/StarNight/Data/Tiles");
            EnsureFolder("Assets/StarNight/Prefabs");
            EnsureFolder("Assets/StarNight/Prefabs/Gameplay");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent =
                path.Substring(0, path.LastIndexOf('/'));
            string folderName =
                path.Substring(path.LastIndexOf('/') + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private sealed class SourceArt
        {
            public Sprite Stone;
            public Sprite Dirt;
            public Sprite Crack;
            public Sprite Crystal;
            public Sprite Crate;
            public Sprite Rock;
            public Sprite FallingRock;
            public Sprite PressurePlate;
            public Sprite Star;
            public Sprite Backdrop;

            public bool IsComplete =>
                Stone != null
                && Dirt != null
                && Crack != null
                && Crystal != null
                && Crate != null
                && Rock != null
                && FallingRock != null
                && PressurePlate != null
                && Star != null
                && Backdrop != null;
        }

        private sealed class GeneratedAssets
        {
            public Tile Stone;
            public Tile Dirt;
            public Tile CrackedWall;
            public Tile Reinforced;
            public Tile ExitFrame;
            public Tile CrackDecoration;
            public Tile CrystalDecoration;
            public Tile LogicGlow;
            public TileDefinition[] Definitions;

            public bool IsComplete
            {
                get
                {
                    if (Stone == null
                        || Dirt == null
                        || CrackedWall == null
                        || Reinforced == null
                        || ExitFrame == null
                        || CrackDecoration == null
                        || CrystalDecoration == null
                        || LogicGlow == null
                        || Definitions == null
                        || Definitions.Length != 5)
                    {
                        return false;
                    }

                    for (int index = 0; index < Definitions.Length; index++)
                    {
                        if (Definitions[index] == null)
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
        }
    }
}

#endif
