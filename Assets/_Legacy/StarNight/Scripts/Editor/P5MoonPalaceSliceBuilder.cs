#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.IO;
using StarNight.Debugging;
using StarNight.Explosions;
using StarNight.Grid;
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
using StarNight.UI;
using StarNight.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Editor
{
    public static class P5MoonPalaceSliceBuilder
    {
        public const string ScenePath =
            "Assets/StarNight/Scenes/Game/P5_MoonPalace_1-1_CraterWorkshop.unity";
        public const string CoreStageRootPrefabPath =
            "Assets/StarNight/Prefabs/Core/CoreStageRoot.prefab";
        public const string PlayerPrefabPath =
            "Assets/StarNight/Prefabs/Gameplay/P3_Player.prefab";
        public const string StoryPestlePrefabPath =
            "Assets/StarNight/Prefabs/Gameplay/P3Tools/P3_Pestle.prefab";
        public const string BombPrefabPath =
            "Assets/StarNight/Prefabs/Gameplay/P3_Bomb.prefab";
        public const string NarrativeProjectPath =
            "Assets/StarNight/Narrative/StarNightNarrative.yarnproject";
        public const string HudRootName = "HudNumericReadout";
        public const string DialogueRootName = "DialogueMaruRules";
        public const string DialogueLineRootName = "DialogueLine";

        private const string MovementTuningPath =
            "Assets/StarNight/Settings/P1_MovementTuning.asset";
        private const string P4TileRoot =
            "Assets/StarNight/Data/P4/RoomTiles";
        private const string ReinforcedTilePath =
            P4TileRoot + "/P4_MoonReinforced.asset";
        private const string SoftSoilTilePath =
            P4TileRoot + "/P4_MoonSoftSoil.asset";
        private const string OneWayTilePath =
            P4TileRoot + "/P4_MoonOneWay.asset";
        private const string P5DataRoot =
            "Assets/StarNight/Data/P5";
        private const string P5TileDataRoot =
            P5DataRoot + "/Tiles";
        private const string ReinforcedDefinitionPath =
            P5TileDataRoot + "/P5_MoonReinforced_Definition.asset";
        private const string SoftSoilDefinitionPath =
            P5TileDataRoot + "/P5_MoonSoftSoil_Definition.asset";

        private const string SkySpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Sky A.png";
        private const string MountainASpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Mounts A.png";
        private const string MountainBSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Mounts B.png";
        private const string CloudSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Clouds small A.png";
        private const string MoonASpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Moon A.png";
        private const string MoonBSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Moon B.png";
        private const string SquareSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Square.png";
        private const string PlatformSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Platforms and doors.png";
        private const string BaseElementSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/base element.png";
        private const string TreeSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Mount tree.png";
        private const string ItemSpritePath =
            "Assets/2D Fantasy sprite bundle/Dungeon pack/Sprites/dungeon items 2.png";
        private const string StarSpritePath =
            "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/Cristal Sprites/Star particle.png";
        private const string CrystalSpritePath =
            "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/Cristal Sprites/Crystal elements.png";
        private const string RabbitSpritePath =
            "Assets/StarNight/Art/Player/char_black_full.png";

        private static readonly FixedRoomSource[] FixedRooms =
        {
            new FixedRoomSource(
                "P4_Moon_Small_CrescentStep_01",
                "Assets/StarNight/Prefabs/Rooms/P4/Small/P4_Moon_Small_CrescentStep_01.prefab",
                new Vector2Int(0, 0),
                new Vector2Int(12, 8)),
            new FixedRoomSource(
                "P4_Moon_Small_SoftSoilDip_01",
                "Assets/StarNight/Prefabs/Rooms/P4/Small/P4_Moon_Small_SoftSoilDip_01.prefab",
                new Vector2Int(12, 0),
                new Vector2Int(12, 8)),
            new FixedRoomSource(
                "P4_Moon_Wide_RollingDoughLane_01",
                "Assets/StarNight/Prefabs/Rooms/P4/Wide/P4_Moon_Wide_RollingDoughLane_01.prefab",
                new Vector2Int(24, 0),
                new Vector2Int(24, 8)),
            new FixedRoomSource(
                "P4_Moon_Small_PestlePost_01",
                "Assets/StarNight/Prefabs/Rooms/P4/Small/P4_Moon_Small_PestlePost_01.prefab",
                new Vector2Int(48, 0),
                new Vector2Int(12, 8)),
            new FixedRoomSource(
                "P4_Moon_Corridor_MomoShop_01",
                "Assets/StarNight/Prefabs/Rooms/P4/Special/P4_Moon_Corridor_MomoShop_01.prefab",
                new Vector2Int(60, 0),
                new Vector2Int(18, 5)),
            new FixedRoomSource(
                "P4_Moon_Small_CrackedShelf_01",
                "Assets/StarNight/Prefabs/Rooms/P4/Small/P4_Moon_Small_CrackedShelf_01.prefab",
                new Vector2Int(78, 0),
                new Vector2Int(12, 8)),
            new FixedRoomSource(
                "P4_Moon_Standard_ThreeBeatSteps_01",
                "Assets/StarNight/Prefabs/Rooms/P4/Standard/P4_Moon_Standard_ThreeBeatSteps_01.prefab",
                new Vector2Int(90, 0),
                new Vector2Int(12, 8))
        };

        [MenuItem("StarNight/P5/Rebuild Moon Palace 1-1 Production Slice")]
        public static void Rebuild()
        {
            EnsureFolder(Path.GetDirectoryName(ScenePath)?.Replace('\\', '/'));
            EnsureFolder(
                Path.GetDirectoryName(CoreStageRootPrefabPath)
                    ?.Replace('\\', '/'));
            EnsureFolder(P5DataRoot);
            EnsureFolder(P5TileDataRoot);

            SourceArt art = LoadSourceArt();
            PersistentAssets assets = LoadPersistentAssets();
            ValidateDependencies(art, assets);
            TileDefinition[] definitions =
                RebuildP5TileDefinitions(assets);
            GameObject corePrefab = RebuildCoreStageRootPrefab();
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            art = LoadSourceArt();
            assets = LoadPersistentAssets();
            ValidateDependencies(art, assets);
            definitions = new[]
            {
                AssetDatabase.LoadAssetAtPath<TileDefinition>(
                    ReinforcedDefinitionPath),
                AssetDatabase.LoadAssetAtPath<TileDefinition>(
                    SoftSoilDefinitionPath)
            };
            corePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CoreStageRootPrefabPath);
            if (definitions[0] == null
                || definitions[1] == null
                || corePrefab == null)
            {
                throw new InvalidOperationException(
                    "P5 persistent production assets failed to reload after scene transition.");
            }

            GameObject stageRoot =
                new GameObject("P5_MoonPalace_1-1_CraterWorkshop");

            GameObject coreStageRoot = InstantiatePrefab(
                corePrefab,
                scene,
                stageRoot.transform,
                "CoreStageRoot",
                Vector2.zero);
            Transform backdrop =
                BuildGlobalMoonBackdrop(stageRoot.transform, art);
            GlobalWorld world =
                BuildGlobalWorld(stageRoot.transform);
            P5FixedRoomPlacement[] placements =
                BakeFixedRooms(scene, stageRoot.transform, world);
            TileBase oneWayTile =
                AssetDatabase.LoadAssetAtPath<Tile>(
                    OneWayTilePath);
            AddProductionTraversalBridge(world.OneWay, oneWayTile);
            RefreshGlobalTilemaps(world);

            Camera camera = BuildCamera(stageRoot.transform);
            Light mainLight = BuildDirectionalLight(stageRoot.transform);
            GameplayRefs gameplay = BuildGameplay(
                scene,
                stageRoot.transform,
                coreStageRoot.transform,
                world,
                camera,
                art,
                assets,
                definitions);
            PresentationRefs presentation = BuildPresentation(
                scene,
                stageRoot.transform,
                world,
                camera,
                art,
                assets,
                gameplay);

            P5MoonStageContract contract =
                stageRoot.AddComponent<P5MoonStageContract>();
            contract.ConfigureLayout(
                placements,
                world.World,
                world.Grid,
                world.Terrain,
                world.OneWay,
                world.Fixture,
                world.Hazard,
                world.Decoration,
                world.Logic);
            contract.ConfigureContent(
                gameplay.Player.transform,
                presentation.Entry,
                presentation.Exit,
                presentation.OptionalReward,
                presentation.RabbitReturn,
                presentation.MoonRabbit,
                presentation.EmptyMortar,
                presentation.PestleSilhouette,
                presentation.StoryPestle,
                presentation.MoonCakeReward,
                presentation.GuaranteedGold,
                presentation.ShopPedestals,
                presentation.BellCues);
            contract.ConfigurePresentation(
                backdrop,
                presentation.EntryGuidance,
                presentation.BacktrackCue,
                camera,
                mainLight);

            ConfigureP5Runtime(
                coreStageRoot,
                world,
                gameplay,
                presentation,
                contract);
            P8MaruSystemBuilder.DecorateProduction(
                stageRoot.transform,
                contract);
            ConfigureNarrativeUi(
                coreStageRoot,
                stageRoot.transform,
                camera,
                gameplay);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!contract.RefreshValidation())
            {
                throw new InvalidOperationException(
                    "P5 production slice validation failed:\n"
                    + contract.LastValidation);
            }

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    $"Failed to save P5 production scene: {ScenePath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[StarNight P5] Production slice rebuilt and validated: {ScenePath}");
        }

        [MenuItem("StarNight/P5/Validate Moon Palace 1-1 Production Slice")]
        public static void Validate()
        {
            GameObject corePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    CoreStageRootPrefabPath);
            if (corePrefab == null)
            {
                throw new InvalidOperationException(
                    $"CoreStageRoot prefab is missing: {CoreStageRootPrefabPath}");
            }

            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            P5MoonStageContract contract =
                UnityEngine.Object.FindFirstObjectByType<P5MoonStageContract>();
            if (contract == null || !contract.RefreshValidation())
            {
                throw new InvalidOperationException(
                    "P5 production scene contract validation failed:\n"
                    + (contract != null
                        ? contract.LastValidation
                        : "P5MoonStageContract is missing."));
            }

            GameObject player = contract.Player != null
                ? contract.Player.gameObject
                : null;
            UnityEngine.Object playerSource = player != null
                ? PrefabUtility.GetCorrespondingObjectFromSource(player)
                : null;
            string playerSourcePath =
                AssetDatabase.GetAssetPath(playerSource);
            if (playerSourcePath != PlayerPrefabPath)
            {
                throw new InvalidOperationException(
                    "P5 must directly reuse P3_Player.prefab.");
            }

            GameObject coreInstance =
                GameObject.Find("CoreStageRoot");
            string coreSourcePath = coreInstance != null
                ? AssetDatabase.GetAssetPath(
                    PrefabUtility.GetCorrespondingObjectFromSource(
                        coreInstance))
                : string.Empty;
            if (coreSourcePath != CoreStageRootPrefabPath)
            {
                throw new InvalidOperationException(
                    "Production scene must instantiate CoreStageRoot.prefab.");
            }

            if (!scene.path.StartsWith(
                    "Assets/StarNight/Scenes/Game/",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "P5 is a production Game scene, not a Lab scene.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(
                "[StarNight P5] Fixed production slice validation PASS.");
        }

        private static GameObject RebuildCoreStageRootPrefab()
        {
            AssetDatabase.DeleteAsset(CoreStageRootPrefabPath);
            GameObject root = new GameObject("CoreStageRoot");
            try
            {
                GameObject coreLoop =
                    CreateChild(root.transform, "CoreLoop");
                coreLoop.AddComponent<P5StageCoreLoop2D>();

                GameObject runState =
                    CreateChild(root.transform, "RunState");
                runState.AddComponent<P5RunState2D>().Configure(0, 0);

                GameObject telemetry =
                    CreateChild(root.transform, "Telemetry");
                telemetry.AddComponent<P5SliceTelemetry2D>();

                GameObject bellClock =
                    CreateChild(root.transform, "MaruBellClock");
                bellClock.AddComponent<P5MaruBellClock2D>().Configure(
                    P5MoonStageContract.FirstBellSeconds,
                    P5MoonStageContract.SecondBellSeconds,
                    P5MoonStageContract.ArrivalBellSeconds,
                    false);
                bellClock.AddComponent<AudioSource>();
                bellClock.AddComponent<P5MaruBellPresenter2D>();

                GameObject hud =
                    CreateChild(root.transform, HudRootName);
                hud.AddComponent<StarNightUiRoot2D>()
                    .Configure(StarNightUiRootKind.Hud);
                hud.AddComponent<StarNightHudView2D>();

                GameObject dialogue =
                    CreateChild(root.transform, DialogueRootName);
                dialogue.AddComponent<StarNightUiRoot2D>()
                    .Configure(StarNightUiRootKind.Dialogue);
                dialogue.AddComponent<MaruRuleDialogue2D>();
                CreateChild(dialogue.transform, DialogueLineRootName)
                    .AddComponent<MaruRuleLinePresenter2D>();

                GameObject saved =
                    PrefabUtility.SaveAsPrefabAsset(
                        root,
                        CoreStageRootPrefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException(
                        "Failed to save clean reusable CoreStageRoot prefab.");
                }

                return saved;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GlobalWorld BuildGlobalWorld(Transform parent)
        {
            GameObject worldRoot = CreateChild(parent, "World");
            GameObject gridObject = CreateChild(worldRoot.transform, "Grid");
            UnityEngine.Grid grid =
                gridObject.AddComponent<UnityEngine.Grid>();
            grid.cellSize = Vector3.one;

            Tilemap terrain = CreateTilemapLayer(
                gridObject.transform,
                "Terrain",
                0,
                TilemapCollisionKind.CompositeSolid,
                true,
                out TilemapCollider2D terrainCollider,
                out CompositeCollider2D terrainComposite);
            Tilemap oneWay = CreateTilemapLayer(
                gridObject.transform,
                "OneWay",
                1,
                TilemapCollisionKind.OneWay,
                true,
                out TilemapCollider2D oneWayCollider,
                out _);
            Tilemap fixture = CreateTilemapLayer(
                gridObject.transform,
                "Fixture",
                2,
                TilemapCollisionKind.None,
                true,
                out _,
                out _);
            Tilemap hazard = CreateTilemapLayer(
                gridObject.transform,
                "Hazard",
                3,
                TilemapCollisionKind.None,
                true,
                out _,
                out _);
            Tilemap decoration = CreateTilemapLayer(
                gridObject.transform,
                "Decoration",
                4,
                TilemapCollisionKind.None,
                true,
                out _,
                out _);
            Tilemap logic = CreateTilemapLayer(
                gridObject.transform,
                "Logic",
                5,
                TilemapCollisionKind.None,
                false,
                out _,
                out _);

            GridWorld gridWorld = gridObject.AddComponent<GridWorld>();
            gridWorld.Configure(
                grid,
                terrain,
                hazard,
                Vector2Int.zero,
                new Vector2Int(
                    P5MoonStageContract.Width,
                    P5MoonStageContract.Height));

            return new GlobalWorld(
                grid,
                gridWorld,
                terrain,
                oneWay,
                fixture,
                hazard,
                decoration,
                logic,
                terrainCollider,
                terrainComposite,
                oneWayCollider);
        }

        private static P5FixedRoomPlacement[] BakeFixedRooms(
            Scene scene,
            Transform parent,
            GlobalWorld world)
        {
            GameObject roomsRoot = CreateChild(parent, "Rooms");
            P5FixedRoomPlacement[] placements =
                new P5FixedRoomPlacement[FixedRooms.Length];

            for (int index = 0; index < FixedRooms.Length; index++)
            {
                FixedRoomSource source = FixedRooms[index];
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        source.PrefabPath);
                RoomTemplate2D template =
                    prefab != null
                        ? prefab.GetComponent<RoomTemplate2D>()
                        : null;
                if (template == null
                    || template.RoomId != source.Id
                    || template.LogicalSize != source.Size)
                {
                    throw new InvalidOperationException(
                        $"P4 room source does not match fixed P5 placement: {source.Id}");
                }

                Vector3Int offset =
                    new Vector3Int(source.Origin.x, source.Origin.y, 0);
                CopyTilemap(template.Terrain, world.Terrain, offset);
                CopyTilemap(template.OneWay, world.OneWay, offset);
                CopyTilemap(template.Fixture, world.Fixture, offset);
                CopyTilemap(template.Decoration, world.Decoration, offset);
                CopyTilemap(template.Logic, world.Logic, offset);

                GameObject instance = InstantiatePrefab(
                    prefab,
                    scene,
                    roomsRoot.transform,
                    $"Room_{index:00}_{source.Id}",
                    source.Origin);
                PrepareBakedRoomInstance(instance);
                RoomTemplate2D room =
                    instance.GetComponent<RoomTemplate2D>();
                placements[index] = new P5FixedRoomPlacement(
                    source.Id,
                    source.Origin,
                    source.Size,
                    room);
            }

            return placements;
        }

        private static void PrepareBakedRoomInstance(GameObject room)
        {
            string[] bakedLayers =
            {
                "Terrain",
                "OneWay",
                "Fixture",
                "Hazard",
                "Decoration",
                "Logic"
            };
            for (int index = 0; index < bakedLayers.Length; index++)
            {
                Transform layer = room.transform.Find(bakedLayers[index]);
                if (layer != null)
                {
                    layer.gameObject.SetActive(false);
                }
            }

            DisableRenderer(room.transform.Find("RoomWash"));
            DisableRenderer(room.transform.Find("MoonMotif"));
            DisableRenderer(room.transform.Find("CloudMotif"));
            DisableChildRenderers(room.transform.Find("Sockets"));
            DisableChildRenderers(room.transform.Find("ContentSlots"));
        }

        private static void AddProductionTraversalBridge(
            Tilemap oneWay,
            TileBase tile)
        {
            if (oneWay == null || tile == null)
            {
                throw new InvalidOperationException(
                    "P5 optional shelf traversal bridge requires the P4 one-way tile.");
            }

            oneWay.SetTile(new Vector3Int(82, 1, 0), tile);
            oneWay.SetTile(new Vector3Int(83, 2, 0), tile);
            oneWay.SetTile(new Vector3Int(88, 2, 0), tile);
            oneWay.SetTile(new Vector3Int(89, 1, 0), tile);
        }

        private static void CopyTilemap(
            Tilemap source,
            Tilemap destination,
            Vector3Int offset)
        {
            if (source == null || destination == null)
            {
                return;
            }

            foreach (Vector3Int sourceCell in source.cellBounds.allPositionsWithin)
            {
                TileBase tile = source.GetTile(sourceCell);
                if (tile == null)
                {
                    continue;
                }

                Vector3Int targetCell = sourceCell + offset;
                destination.SetTile(targetCell, tile);
                destination.SetTransformMatrix(
                    targetCell,
                    source.GetTransformMatrix(sourceCell));
                destination.SetColor(
                    targetCell,
                    source.GetColor(sourceCell));
            }
        }

        private static Tilemap CreateTilemapLayer(
            Transform parent,
            string name,
            int sortingOrder,
            TilemapCollisionKind collision,
            bool visible,
            out TilemapCollider2D tilemapCollider,
            out CompositeCollider2D composite)
        {
            GameObject layer = CreateChild(parent, name);
            Tilemap tilemap = layer.AddComponent<Tilemap>();
            TilemapRenderer renderer =
                layer.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.enabled = visible;

            tilemapCollider = null;
            composite = null;
            if (collision == TilemapCollisionKind.None)
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

            if (collision == TilemapCollisionKind.CompositeSolid)
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

        private static void RefreshGlobalTilemaps(GlobalWorld world)
        {
            Tilemap[] tilemaps =
            {
                world.Terrain,
                world.OneWay,
                world.Fixture,
                world.Hazard,
                world.Decoration,
                world.Logic
            };
            for (int index = 0; index < tilemaps.Length; index++)
            {
                tilemaps[index].CompressBounds();
                tilemaps[index].RefreshAllTiles();
            }

            world.TerrainCollider.ProcessTilemapChanges();
            world.TerrainComposite.GenerateGeometry();
            world.OneWayCollider.ProcessTilemapChanges();
            Physics2D.SyncTransforms();
        }

        private static Transform BuildGlobalMoonBackdrop(
            Transform parent,
            SourceArt art)
        {
            GameObject backdrop =
                CreateChild(parent, "GlobalMoonBackdrop");
            for (int index = 0; index < 5; index++)
            {
                float centerX = 11f + index * 22f;
                CreatePart(
                    backdrop.transform,
                    art.Sky,
                    $"Sky_{index:00}",
                    new Vector2(centerX, 9f),
                    new Vector2(23f, 20f),
                    0f,
                    Color.white,
                    -100);
                CreatePart(
                    backdrop.transform,
                    index % 2 == 0
                        ? art.MountainA
                        : art.MountainB,
                    $"MoonMountains_{index:00}",
                    new Vector2(centerX, 4.35f),
                    new Vector2(23f, 9.25f),
                    0f,
                    new Color(0.25f, 0.34f, 0.58f, 0.7f),
                    -90);
            }

            for (int index = 0; index < 10; index++)
            {
                CreatePart(
                    backdrop.transform,
                    art.Cloud,
                    $"MoonCloud_{index:00}",
                    new Vector2(
                        4f + index * 10.2f,
                        10f + (index % 3) * 1.8f),
                    new Vector2(5.2f, 1.8f),
                    0f,
                    new Color(0.82f, 0.9f, 1f, 0.34f),
                    -80);
            }

            CreatePart(
                backdrop.transform,
                art.MoonB,
                "ExitCrescentLandmark",
                new Vector2(99f, 13f),
                new Vector2(8f, 8f),
                0f,
                new Color(0.78f, 0.94f, 1f, 0.76f),
                -72);
            CreatePart(
                backdrop.transform,
                art.Star,
                "ExitLandmarkStar",
                new Vector2(99f, 13.2f),
                new Vector2(1.25f, 1.25f),
                0f,
                new Color(1f, 0.95f, 0.72f, 0.95f),
                -71);
            return backdrop.transform;
        }

        private static Camera BuildCamera(Transform parent)
        {
            GameObject cameraObject =
                CreateChild(parent, "Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position =
                new Vector3(P5MoonStageContract.PlayerSpawn.x + 2.5f, 6.25f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor =
                new Color(0.025f, 0.045f, 0.12f, 1f);
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static Light BuildDirectionalLight(Transform parent)
        {
            GameObject lightObject =
                CreateChild(parent, "Directional Light");
            lightObject.transform.rotation =
                Quaternion.Euler(42f, -28f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.78f, 0.9f, 1f, 1f);
            light.intensity = 1.05f;
            return light;
        }

        private static void ConfigureP5Runtime(
            GameObject coreStageRoot,
            GlobalWorld world,
            GameplayRefs gameplay,
            PresentationRefs presentation,
            P5MoonStageContract contract)
        {
            P5StageCoreLoop2D coreLoop =
                RequireComponent<P5StageCoreLoop2D>(
                    coreStageRoot.transform.Find("CoreLoop").gameObject);
            P5RunState2D runState =
                RequireComponent<P5RunState2D>(
                    coreStageRoot.transform.Find("RunState").gameObject);
            P5SliceTelemetry2D telemetry =
                RequireComponent<P5SliceTelemetry2D>(
                    coreStageRoot.transform.Find("Telemetry").gameObject);
            GameObject bellObject =
                coreStageRoot.transform.Find("MaruBellClock").gameObject;
            P5MaruBellClock2D bellClock =
                RequireComponent<P5MaruBellClock2D>(bellObject);
            P5MaruBellPresenter2D bellPresenter =
                RequireComponent<P5MaruBellPresenter2D>(bellObject);

            runState.Configure(0, 0);
            bellClock.Configure(
                P5MoonStageContract.FirstBellSeconds,
                P5MoonStageContract.SecondBellSeconds,
                P5MoonStageContract.ArrivalBellSeconds,
                false);

            GameObject storyPestleObject =
                presentation.Rabbit.StoryPestle.gameObject;
            P5StoryPestle2D storyPestle =
                storyPestleObject.AddComponent<P5StoryPestle2D>();
            storyPestle.Configure(
                RequireComponent<HandToolPickup2D>(
                    storyPestleObject),
                presentation.Rabbit.RecoveryAnchor,
                world.World,
                -8f,
                24f);

            P5MoonRabbitPestleEvent2D rabbitEvent =
                presentation.Rabbit.Root
                    .AddComponent<P5MoonRabbitPestleEvent2D>();
            rabbitEvent.Configure(
                storyPestle,
                runState,
                presentation.Rabbit.RabbitReturn,
                presentation.Rabbit.MoonCakeReward.gameObject,
                presentation.Rabbit.InteractionPoint,
                P5MoonRabbitPestleEvent2D.DefaultMoonCakeReward,
                1.75f);

            P5MomoShop2D shop =
                presentation.Economy.Shop
                    .AddComponent<P5MomoShop2D>();
            P5ShopProductKind[] productKinds =
            {
                P5ShopProductKind.RopeBundle3,
                P5ShopProductKind.MoonCake,
                P5ShopProductKind.BombBundle2
            };
            P5MomoShopOffer2D[] offers =
                new P5MomoShopOffer2D[
                    P5MomoShop2D.RequiredOfferCount];
            for (int index = 0; index < offers.Length; index++)
            {
                offers[index] =
                    presentation.Economy.ShopPedestals[index]
                        .gameObject.AddComponent<P5MomoShopOffer2D>();
                offers[index].Configure(
                    shop,
                    productKinds[index],
                    P5MomoShopOffer2D.DefaultPrice(
                        productKinds[index]),
                    presentation.Economy.OfferInteractions[index],
                    presentation.Economy.ProductVisuals[index].gameObject,
                    1.35f);
            }

            shop.Configure(
                runState,
                gameplay.Consumables,
                offers);

            List<P5GoldPickup2D> goldPickups =
                new List<P5GoldPickup2D>(
                    presentation.GuaranteedGold.Length + 1);
            for (int index = 0;
                index < presentation.GuaranteedGold.Length;
                index++)
            {
                goldPickups.Add(ConfigureGoldPickup(
                    presentation.GuaranteedGold[index],
                    runState,
                    gameplay.Player.transform,
                    P5GoldPickup2D.SmallGoldValue));
            }

            goldPickups.Add(ConfigureGoldPickup(
                presentation.Economy.OptionalBigGold,
                runState,
                gameplay.Player.transform,
                P5GoldPickup2D.BigGoldValue));

            P5StageBacktrackCue2D backtrackCue =
                presentation.ExitRefs.BacktrackCue.gameObject
                    .AddComponent<P5StageBacktrackCue2D>();
            backtrackCue.Configure(
                presentation.ExitRefs.CueIcon,
                presentation.ExitRefs.ShelfHighlight,
                2.5f,
                rabbitEvent);

            P5StageExit2D stageExit =
                presentation.Exit.gameObject
                    .AddComponent<P5StageExit2D>();
            stageExit.Configure(
                gameplay.Input,
                gameplay.Player.transform,
                coreLoop,
                backtrackCue,
                presentation.ExitRefs.InteractionPoint,
                presentation.ExitRefs.Glow,
                P5StageExit2D.RequiredHoldSeconds,
                1.35f);
            presentation.Exit.gameObject
                .AddComponent<P5StageExitInteractionGuard2D>()
                .Configure(
                    stageExit,
                    presentation.ExitRefs.InteractionPoint,
                    1.35f);

            telemetry.Configure(
                rabbitEvent,
                shop,
                stageExit,
                bellClock,
                goldPickups.ToArray());
            coreLoop.Configure(
                runState,
                bellClock,
                stageExit,
                rabbitEvent,
                shop,
                telemetry,
                P5ExitGuidance2D.RequiredGuidanceSeconds,
                false);

            SpriteRenderer[] backdropRenderers =
                contract.GlobalMoonBackdrop != null
                    ? contract.GlobalMoonBackdrop
                        .GetComponentsInChildren<SpriteRenderer>(true)
                    : Array.Empty<SpriteRenderer>();
            List<SpriteRenderer> skyTints = new List<SpriteRenderer>();
            for (int index = 0; index < backdropRenderers.Length; index++)
            {
                if (backdropRenderers[index] != null
                    && backdropRenderers[index].name.StartsWith(
                        "Sky_",
                        StringComparison.Ordinal))
                {
                    skyTints.Add(backdropRenderers[index]);
                }
            }

            bellPresenter.Configure(
                bellClock,
                presentation.BellCues[0].gameObject,
                presentation.BellCues[1].gameObject,
                presentation.BellCues[2].gameObject,
                null,
                bellObject.GetComponent<AudioSource>(),
                null,
                null);
            bellPresenter.SetBackgroundTints(skyTints.ToArray());
            bellPresenter.SetPhaseColors(
                new Color(0.48f, 0.54f, 0.82f, 1f),
                new Color(0.40f, 0.47f, 0.72f, 1f),
                new Color(0.30f, 0.35f, 0.58f, 1f),
                new Color(0.20f, 0.20f, 0.38f, 1f));

            GridBoundedCamera2D cameraFollow =
                contract.StageCamera.GetComponent<GridBoundedCamera2D>();
            Transform edgeIndicator =
                contract.StageCamera.transform.Find(
                    "ScreenEdgeExitIcon");
            P5ExitGuidance2D guidance =
                presentation.EntryGuidance.gameObject
                    .AddComponent<P5ExitGuidance2D>();
            guidance.Configure(
                contract.StageCamera,
                cameraFollow,
                gameplay.Player.transform,
                presentation.Exit,
                edgeIndicator,
                coreLoop,
                telemetry,
                P5ExitGuidance2D.RequiredGuidanceSeconds,
                6f,
                true,
                gameplay.Player.GetComponent<PlayerMotor2D>());

            P5BacktrackProbe2D backtrackProbe =
                presentation.OptionalReward.gameObject
                    .AddComponent<P5BacktrackProbe2D>();
            backtrackProbe.Configure(
                gameplay.Player.transform,
                stageExit,
                telemetry,
                1.5f);

            contract.ConfigureRuntime(
                coreLoop,
                runState,
                rabbitEvent,
                shop,
                stageExit,
                bellClock,
                telemetry);
        }

        private static void ConfigureNarrativeUi(
            GameObject coreStageRoot,
            Transform stageRoot,
            Camera camera,
            GameplayRefs gameplay)
        {
            Transform hudRoot =
                coreStageRoot.transform.Find(HudRootName);
            Transform dialogueRoot =
                coreStageRoot.transform.Find(DialogueRootName);
            if (hudRoot == null || dialogueRoot == null)
            {
                throw new InvalidOperationException(
                    "CoreStageRoot must expose the HUD and dialogue roots.");
            }

            P5RunState2D runState =
                RequireComponent<P5RunState2D>(
                    coreStageRoot.transform.Find("RunState").gameObject);
            PlayerMotor2D motor =
                gameplay.Player.GetComponent<PlayerMotor2D>();
            int maxHealth =
                motor != null && motor.Tuning != null
                    ? motor.Tuning.MaxHealth
                    : 0;

            P8MaruTimeline2D timeline =
                stageRoot.GetComponentInChildren<P8MaruTimeline2D>(true);
            P8HomecomingStatue2D statue =
                stageRoot.GetComponentInChildren<P8HomecomingStatue2D>(true);
            P8MaruBiteController2D bite =
                stageRoot.GetComponentInChildren<P8MaruBiteController2D>(
                    true);

            RequireComponent<StarNightHudView2D>(hudRoot.gameObject)
                .Configure(
                    camera,
                    gameplay.Recovery,
                    gameplay.Consumables,
                    runState,
                    timeline,
                    maxHealth);

            Transform lineRoot =
                dialogueRoot.Find(DialogueLineRootName);
            if (lineRoot != null)
            {
                RequireComponent<MaruRuleLinePresenter2D>(
                        lineRoot.gameObject)
                    .Configure(
                        camera,
                        MaruRuleLinePresenter2D.DefaultHoldSeconds);
            }

            MaruRuleDialogue2D dialogue =
                RequireComponent<MaruRuleDialogue2D>(
                    dialogueRoot.gameObject);
            dialogue.Configure(timeline, statue, bite);

            UnityEngine.Object narrativeProject =
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    NarrativeProjectPath);
            if (narrativeProject == null
                || !dialogue.TryBindProject(narrativeProject))
            {
                Debug.LogWarning(
                    "[StarNight P5] Yarn 프로젝트를 찾지 못해 마루 규칙 대화가 "
                    + $"비어 있습니다: {NarrativeProjectPath}");
            }
        }

        private static P5GoldPickup2D ConfigureGoldPickup(
            Transform pickupTransform,
            P5RunState2D runState,
            Transform player,
            int value)
        {
            P5GoldPickup2D pickup =
                pickupTransform.gameObject
                    .AddComponent<P5GoldPickup2D>();
            pickup.Configure(
                runState,
                value,
                player,
                pickupTransform.GetComponentInChildren<SpriteRenderer>(true),
                0.75f);
            return pickup;
        }

        private enum TilemapCollisionKind
        {
            None = 0,
            CompositeSolid = 1,
            OneWay = 2
        }

        private readonly struct FixedRoomSource
        {
            public readonly string Id;
            public readonly string PrefabPath;
            public readonly Vector2Int Origin;
            public readonly Vector2Int Size;

            public FixedRoomSource(
                string id,
                string prefabPath,
                Vector2Int origin,
                Vector2Int size)
            {
                Id = id;
                PrefabPath = prefabPath;
                Origin = origin;
                Size = size;
            }
        }

        private readonly struct GlobalWorld
        {
            public readonly UnityEngine.Grid Grid;
            public readonly GridWorld World;
            public readonly Tilemap Terrain;
            public readonly Tilemap OneWay;
            public readonly Tilemap Fixture;
            public readonly Tilemap Hazard;
            public readonly Tilemap Decoration;
            public readonly Tilemap Logic;
            public readonly TilemapCollider2D TerrainCollider;
            public readonly CompositeCollider2D TerrainComposite;
            public readonly TilemapCollider2D OneWayCollider;

            public GlobalWorld(
                UnityEngine.Grid grid,
                GridWorld world,
                Tilemap terrain,
                Tilemap oneWay,
                Tilemap fixture,
                Tilemap hazard,
                Tilemap decoration,
                Tilemap logic,
                TilemapCollider2D terrainCollider,
                CompositeCollider2D terrainComposite,
                TilemapCollider2D oneWayCollider)
            {
                Grid = grid;
                World = world;
                Terrain = terrain;
                OneWay = oneWay;
                Fixture = fixture;
                Hazard = hazard;
                Decoration = decoration;
                Logic = logic;
                TerrainCollider = terrainCollider;
                TerrainComposite = terrainComposite;
                OneWayCollider = oneWayCollider;
            }
        }

        private static GameplayRefs BuildGameplay(
            Scene scene,
            Transform stageRoot,
            Transform coreStageRoot,
            GlobalWorld world,
            Camera camera,
            SourceArt art,
            PersistentAssets assets,
            TileDefinition[] definitions)
        {
            GameObject gameplayRoot =
                CreateChild(stageRoot, "Gameplay");
            GameObject systems =
                CreateChild(gameplayRoot.transform, "Systems");
            GameObject spawnedConsumables =
                CreateChild(gameplayRoot.transform, "SpawnedConsumables");
            GameObject installedRopes =
                CreateChild(gameplayRoot.transform, "InstalledRopes");

            GameObject player = InstantiatePrefab(
                assets.PlayerPrefab,
                scene,
                gameplayRoot.transform,
                "Player",
                P5MoonStageContract.PlayerSpawn);
            Rigidbody2D playerBody =
                RequireComponent<Rigidbody2D>(player);
            CapsuleCollider2D playerCollider =
                RequireComponent<CapsuleCollider2D>(player);
            PlayerInputAdapter playerInput =
                RequireComponent<PlayerInputAdapter>(player);
            PlayerMotor2D motor =
                RequireComponent<PlayerMotor2D>(player);
            SafeCellTracker safeCells =
                RequireComponent<SafeCellTracker>(player);
            PlayerRecovery recovery =
                RequireComponent<PlayerRecovery>(player);
            CarrySystem carry =
                RequireComponent<CarrySystem>(player);
            PlayerToolInventory2D inventory =
                RequireComponent<PlayerToolInventory2D>(player);
            PlayerConsumableTools2D consumables =
                RequireComponent<PlayerConsumableTools2D>(player);
            Transform holdAnchor = player.transform.Find("CarryAnchor");
            if (holdAnchor == null)
            {
                throw new InvalidOperationException(
                    "P3_Player requires CarryAnchor.");
            }

            TileMutationService mutation =
                systems.AddComponent<TileMutationService>();
            mutation.Configure(
                world.World,
                world.Terrain,
                world.Decoration,
                world.TerrainCollider,
                world.TerrainComposite,
                playerCollider,
                definitions,
                new GridPos(
                    P5MoonStageContract.StartCell.x,
                    P5MoonStageContract.StartCell.y),
                new GridPos(
                    P5MoonStageContract.ExitSafeCell.x,
                    P5MoonStageContract.ExitSafeCell.y),
                BuildProtectedExitCells());

            ExplosionService2D explosions =
                systems.AddComponent<ExplosionService2D>();
            explosions.Configure(
                world.World,
                mutation,
                ExplosionConstants.DefaultChainHardCap,
                7f,
                ~0);

            RopeInstaller2D ropeInstaller =
                systems.AddComponent<RopeInstaller2D>();
            ropeInstaller.Configure(
                world.World,
                mutation,
                null,
                installedRopes.transform,
                RopePlacementSolver.DefaultMaximumLength,
                art.Anchor);

            WaterInteractionRegistry2D waterRegistry =
                systems.AddComponent<WaterInteractionRegistry2D>();
            PestleInteractionRegistry2D pestleRegistry =
                systems.AddComponent<PestleInteractionRegistry2D>();

            playerBody.position = P5MoonStageContract.PlayerSpawn;
            safeCells.Configure(
                world.World,
                playerBody,
                playerCollider,
                motor,
                assets.MovementTuning);
            safeCells.SetSpawnFallback(
                P5MoonStageContract.PlayerSpawn);
            recovery.Configure(
                world.World,
                playerBody,
                motor,
                safeCells,
                assets.MovementTuning);
            carry.Configure(
                playerInput,
                playerBody,
                holdAnchor,
                world.World);
            inventory.Configure(
                playerInput,
                carry,
                motor,
                playerBody,
                playerCollider,
                world.World,
                holdAnchor,
                camera,
                waterRegistry,
                pestleRegistry,
                Array.Empty<WaterSource2D>(),
                null);
            consumables.Configure(
                playerInput,
                playerBody,
                world.World,
                ropeInstaller,
                explosions,
                assets.BombPrefab,
                spawnedConsumables.transform,
                null,
                PlayerConsumableTools2D.DefaultRopeStock,
                PlayerConsumableTools2D.DefaultBombStock);

            GridBoundedCamera2D cameraFollow =
                camera.gameObject.AddComponent<GridBoundedCamera2D>();
            cameraFollow.Configure(
                camera,
                player.transform,
                playerBody,
                world.World,
                recovery);

            BuildRecoveryVolume(gameplayRoot.transform);
            return new GameplayRefs(
                player,
                playerInput,
                playerBody,
                playerCollider,
                recovery,
                carry,
                inventory,
                consumables,
                pestleRegistry,
                mutation,
                explosions,
                ropeInstaller,
                systems.transform);
        }

        private static PresentationRefs BuildPresentation(
            Scene scene,
            Transform stageRoot,
            GlobalWorld world,
            Camera camera,
            SourceArt art,
            PersistentAssets assets,
            GameplayRefs gameplay)
        {
            Transform entryGuidance =
                BuildEntryGuidance(stageRoot, camera, art);
            RabbitEventRefs rabbit = BuildRabbitEvent(
                scene,
                stageRoot,
                world,
                art,
                assets,
                gameplay);
            EconomyRefs economy = BuildEconomy(
                scene,
                stageRoot,
                art,
                assets);
            ExitRefs exit = BuildExit(
                stageRoot,
                art);
            Transform[] bellCues =
                BuildBellVisualHierarchy(stageRoot, camera, art);
            BuildRollingRiceCakeCue(stageRoot, art);

            GameObject entry = CreateChild(stageRoot, "StageEntry");
            entry.transform.position = new Vector3(
                P5MoonStageContract.PlayerSpawn.x,
                P5MoonStageContract.PlayerSpawn.y,
                0f);
            GameObject optionalReward =
                CreateChild(stageRoot, "OptionalRewardRoute");
            optionalReward.transform.position = CellCenter(
                P5MoonStageContract.StoryPestleCell);

            return new PresentationRefs(
                entry.transform,
                exit.Exit,
                optionalReward.transform,
                rabbit.RabbitReturn,
                rabbit.MoonRabbit,
                rabbit.EmptyMortar,
                rabbit.PestleSilhouette,
                rabbit.StoryPestle,
                rabbit.MoonCakeReward,
                economy.GuaranteedGold,
                economy.ShopPedestals,
                bellCues,
                entryGuidance,
                exit.BacktrackCue,
                rabbit,
                economy,
                exit);
        }

        private static void BuildRecoveryVolume(Transform parent)
        {
            GameObject volume =
                CreateChild(parent, "FallRecovery");
            volume.transform.position =
                new Vector3(P5MoonStageContract.Width * 0.5f, -4f, 0f);
            BoxCollider2D trigger =
                volume.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size =
                new Vector2(P5MoonStageContract.Width + 8f, 4f);
            volume.AddComponent<RecoveryVolume2D>();
        }

        private static GridPos[] BuildProtectedExitCells()
        {
            List<GridPos> cells = new List<GridPos>(12);
            for (int x = 99; x <= 101; x++)
            {
                for (int y = 0; y <= 3; y++)
                {
                    cells.Add(new GridPos(x, y));
                }
            }

            return cells.ToArray();
        }

        private static Transform BuildEntryGuidance(
            Transform parent,
            Camera camera,
            SourceArt art)
        {
            GameObject guidance =
                CreateChild(parent, "EntryGuidance");
            GameObject starFlow =
                CreateChild(guidance.transform, "StarFlowToExit");
            for (int index = 0; index < 16; index++)
            {
                float x = 5f + index * 5.7f;
                CreatePart(
                    starFlow.transform,
                    art.Star,
                    $"FlowStar_{index:00}",
                    new Vector2(x, 8.6f + (index % 3) * 0.6f),
                    Vector2.one * (0.18f + (index % 2) * 0.08f),
                    -12f,
                    new Color(0.76f, 0.94f, 1f, 0.58f),
                    -65);
            }

            GameObject edgeIcon =
                CreateChild(camera.transform, "ScreenEdgeExitIcon");
            edgeIcon.transform.localPosition =
                new Vector3(7.2f, 0.45f, 10f);
            CreatePart(
                edgeIcon.transform,
                art.MoonB,
                "EdgeCrescent",
                Vector2.zero,
                new Vector2(0.72f, 0.72f),
                0f,
                new Color(0.66f, 0.94f, 1f, 0.92f),
                200);
            CreatePart(
                edgeIcon.transform,
                art.Star,
                "EdgeStar",
                new Vector2(0.2f, 0f),
                new Vector2(0.22f, 0.22f),
                0f,
                new Color(1f, 0.94f, 0.64f, 1f),
                201);

            GameObject cameraPan =
                CreateChild(guidance.transform, "EntryCameraPan");
            cameraPan.transform.position =
                new Vector3(P5MoonStageContract.PlayerSpawn.x + 3f, 6f, 0f);
            return guidance.transform;
        }

        private static RabbitEventRefs BuildRabbitEvent(
            Scene scene,
            Transform parent,
            GlobalWorld world,
            SourceArt art,
            PersistentAssets assets,
            GameplayRefs gameplay)
        {
            GameObject eventRoot =
                CreateChild(parent, "RabbitPestleEvent");

            GameObject rabbit =
                CreateChild(eventRoot.transform, "MoonRabbit");
            rabbit.transform.position =
                CellCenter(P5MoonStageContract.RabbitCell);
            CreatePart(
                rabbit.transform,
                art.Rabbit,
                "RabbitBody",
                new Vector2(0f, 0.38f),
                new Vector2(1.35f, 1.55f),
                0f,
                new Color(0.86f, 0.78f, 1f, 1f),
                32);
            CreatePart(
                rabbit.transform,
                art.MoonA,
                "LilacApronCrescent",
                new Vector2(0f, 0.1f),
                new Vector2(0.55f, 0.42f),
                0f,
                new Color(0.74f, 0.54f, 0.92f, 0.72f),
                33);
            CreatePart(
                rabbit.transform,
                art.Star,
                "RabbitGaze",
                new Vector2(0.7f, 0.7f),
                new Vector2(0.18f, 0.18f),
                0f,
                new Color(1f, 0.92f, 0.58f, 0.95f),
                34);

            GameObject mortar =
                CreateChild(eventRoot.transform, "EmptyMortar");
            mortar.transform.position =
                CellCenter(P5MoonStageContract.MortarCell);
            CreatePart(
                mortar.transform,
                art.MoonB,
                "MortarBowl",
                new Vector2(0f, 0.12f),
                new Vector2(1.65f, 0.88f),
                180f,
                new Color(0.74f, 0.66f, 0.82f, 1f),
                27);
            CreatePart(
                mortar.transform,
                art.Disc,
                "MortarRim",
                new Vector2(0f, 0.38f),
                new Vector2(1.35f, 0.25f),
                0f,
                new Color(0.92f, 0.82f, 0.66f, 1f),
                28);

            GameObject returnedAnchor =
                CreateChild(mortar.transform, "ReturnedPestleAnchor");
            returnedAnchor.transform.localPosition =
                new Vector3(0f, 0.72f, 0f);
            GameObject interactionPoint =
                CreateChild(eventRoot.transform, "RabbitInteractionPoint");
            interactionPoint.transform.position =
                new Vector3(54f, 1.65f, 0f);
            BoxCollider2D eventTrigger =
                interactionPoint.AddComponent<BoxCollider2D>();
            eventTrigger.isTrigger = true;
            eventTrigger.size = new Vector2(4.5f, 2.1f);

            GameObject silhouette = InstantiatePrefab(
                assets.StoryPestlePrefab,
                scene,
                eventRoot.transform,
                "PestleSilhouette",
                CellCenter(P5MoonStageContract.PestleSilhouetteCell));
            ConfigurePestleSilhouette(silhouette);

            GameObject storyPestle = InstantiatePrefab(
                assets.StoryPestlePrefab,
                scene,
                eventRoot.transform,
                "StoryPestle",
                CellCenter(P5MoonStageContract.StoryPestleCell));
            PestleTool2D pestleTool =
                RequireComponent<PestleTool2D>(storyPestle);
            pestleTool.Configure(
                world.World,
                gameplay.PestleRegistry);

            GameObject recoveryAnchor =
                CreateChild(eventRoot.transform, "StoryPestleRecoveryAnchor");
            recoveryAnchor.transform.position =
                CellCenter(P5MoonStageContract.StoryPestleCell);

            GameObject reward =
                CreateChild(eventRoot.transform, "MoonCakeReward");
            reward.transform.position =
                CellCenter(P5MoonStageContract.MoonCakeRewardCell);
            CreatePart(
                reward.transform,
                art.Disc,
                "MoonCake",
                new Vector2(0f, 0.22f),
                new Vector2(0.72f, 0.72f),
                0f,
                new Color(1f, 0.88f, 0.64f, 1f),
                35);
            CreatePart(
                reward.transform,
                art.MoonA,
                "MoonCakeStamp",
                new Vector2(0f, 0.22f),
                new Vector2(0.38f, 0.38f),
                0f,
                new Color(0.84f, 0.58f, 0.68f, 0.92f),
                36);
            CircleCollider2D rewardTrigger =
                reward.AddComponent<CircleCollider2D>();
            rewardTrigger.isTrigger = true;
            rewardTrigger.radius = 0.52f;
            reward.SetActive(false);

            BuildPestleNeedMotionCue(
                eventRoot.transform,
                art,
                new Vector2(52.4f, 2.35f));

            return new RabbitEventRefs(
                eventRoot,
                rabbit.transform,
                mortar.transform,
                silhouette.transform,
                storyPestle.transform,
                reward.transform,
                returnedAnchor.transform,
                recoveryAnchor.transform,
                interactionPoint.transform);
        }

        private static void ConfigurePestleSilhouette(GameObject silhouette)
        {
            MonoBehaviour[] behaviours =
                silhouette.GetComponentsInChildren<MonoBehaviour>(true);
            for (int index = 0; index < behaviours.Length; index++)
            {
                behaviours[index].enabled = false;
            }

            Collider2D[] colliders =
                silhouette.GetComponentsInChildren<Collider2D>(true);
            for (int index = 0; index < colliders.Length; index++)
            {
                colliders[index].enabled = false;
            }

            Rigidbody2D[] bodies =
                silhouette.GetComponentsInChildren<Rigidbody2D>(true);
            for (int index = 0; index < bodies.Length; index++)
            {
                bodies[index].simulated = false;
            }

            SpriteRenderer[] renderers =
                silhouette.GetComponentsInChildren<SpriteRenderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                Color color = renderers[index].color;
                renderers[index].color =
                    new Color(0.72f, 0.9f, 1f, 0.28f);
                renderers[index].sortingOrder =
                    Mathf.Max(renderers[index].sortingOrder, 31);
                if (color.a <= 0.001f)
                {
                    renderers[index].enabled = false;
                }
            }
        }

        private static void BuildPestleNeedMotionCue(
            Transform parent,
            SourceArt art,
            Vector2 position)
        {
            GameObject cue = CreateChild(parent, "PestleNeedMotionCue");
            cue.transform.position =
                new Vector3(position.x, position.y, 0f);
            for (int index = 0; index < 3; index++)
            {
                CreatePart(
                    cue.transform,
                    art.Star,
                    $"PoundBeat_{index:00}",
                    new Vector2(index * 0.32f, -index * 0.22f),
                    Vector2.one * (index == 2 ? 0.26f : 0.16f),
                    0f,
                    new Color(0.82f, 0.72f, 1f, 0.8f),
                    31);
            }
        }

        private static EconomyRefs BuildEconomy(
            Scene scene,
            Transform parent,
            SourceArt art,
            PersistentAssets assets)
        {
            GameObject economy =
                CreateChild(parent, "Economy");
            GameObject guaranteed =
                CreateChild(economy.transform, "GuaranteedGold");
            Transform[] gold =
                new Transform[P5MoonStageContract.GuaranteedGoldCells.Length];
            for (int index = 0; index < gold.Length; index++)
            {
                Vector2Int cell =
                    P5MoonStageContract.GuaranteedGoldCells[index];
                gold[index] = BuildGoldPickup(
                    guaranteed.transform,
                    art,
                    $"SmallGold_{index:00}",
                    CellCenter(cell),
                    1,
                    0.42f);
            }

            GameObject optionalGold =
                CreateChild(economy.transform, "OptionalBigGold");
            Transform bigGold = BuildGoldPickup(
                optionalGold.transform,
                art,
                "BigGold_00",
                new Vector2(36.5f, 3.5f),
                3,
                0.62f);

            GameObject shop =
                CreateChild(economy.transform, "MomoShop");
            CreatePart(
                shop.transform,
                art.MoonB,
                "CrescentCanopy",
                new Vector2(69.5f, 4.1f),
                new Vector2(10.5f, 2.4f),
                0f,
                new Color(0.55f, 0.32f, 0.72f, 0.92f),
                21);
            CreatePart(
                shop.transform,
                art.Cloud,
                "CanopyGlow",
                new Vector2(69.5f, 3.9f),
                new Vector2(8.5f, 1.2f),
                0f,
                new Color(0.86f, 0.74f, 1f, 0.34f),
                22);

            Vector2[] positions =
            {
                new Vector2(65.5f, 1.5f),
                new Vector2(69.5f, 1.5f),
                new Vector2(73.5f, 1.5f)
            };
            string[] names =
            {
                "Pedestal_00_RopeBundle3",
                "Pedestal_01_MoonCake",
                "Pedestal_02_BombBundle2"
            };
            int[] prices = { 3, 3, 4 };
            Sprite[] products =
            {
                art.Anchor,
                art.Disc,
                art.Item
            };
            Color[] productColors =
            {
                new Color(0.78f, 0.62f, 0.42f, 1f),
                new Color(1f, 0.84f, 0.58f, 1f),
                new Color(0.68f, 0.52f, 0.76f, 1f)
            };

            Transform[] pedestals =
                new Transform[P5MoonStageContract.RequiredShopPedestalCount];
            Transform[] interactions =
                new Transform[pedestals.Length];
            Transform[] productVisuals =
                new Transform[pedestals.Length];
            for (int index = 0; index < pedestals.Length; index++)
            {
                GameObject pedestal =
                    CreateChild(shop.transform, names[index]);
                pedestal.transform.position =
                    new Vector3(positions[index].x, positions[index].y, 0f);
                pedestals[index] = pedestal.transform;

                CreatePart(
                    pedestal.transform,
                    art.Item,
                    "PhysicalPedestal",
                    new Vector2(0f, 0f),
                    new Vector2(1.25f, 0.72f),
                    0f,
                    new Color(0.48f, 0.34f, 0.58f, 1f),
                    24);
                SpriteRenderer product = CreatePart(
                    pedestal.transform,
                    products[index],
                    "ProductVisual",
                    new Vector2(0f, 0.88f),
                    index == 0
                        ? new Vector2(0.72f, 0.72f)
                        : new Vector2(0.64f, 0.64f),
                    index == 0 ? 90f : 0f,
                    productColors[index],
                    26);
                productVisuals[index] = product.transform;

                GameObject interaction =
                    CreateChild(pedestal.transform, "InteractionPoint");
                interaction.transform.localPosition =
                    new Vector3(0f, 0.65f, 0f);
                BoxCollider2D trigger =
                    interaction.AddComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                trigger.size = new Vector2(1.5f, 2.1f);
                interactions[index] = interaction.transform;
                BuildGoldPriceIcons(
                    pedestal.transform,
                    art,
                    prices[index]);
            }

            return new EconomyRefs(
                economy,
                shop,
                gold,
                bigGold,
                pedestals,
                interactions,
                productVisuals);
        }

        private static Transform BuildGoldPickup(
            Transform parent,
            SourceArt art,
            string name,
            Vector2 position,
            int value,
            float size)
        {
            GameObject pickup = CreateChild(parent, name);
            pickup.transform.position =
                new Vector3(position.x, position.y, 0f);
            pickup.transform.localScale =
                Vector3.one * (value > 1 ? 1.15f : 1f);
            CreatePart(
                pickup.transform,
                art.Crystal,
                "GoldShard",
                Vector2.zero,
                Vector2.one * size,
                0f,
                new Color(1f, 0.77f, 0.16f, 1f),
                29);
            CircleCollider2D trigger =
                pickup.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.5f;
            return pickup.transform;
        }

        private static void BuildGoldPriceIcons(
            Transform parent,
            SourceArt art,
            int price)
        {
            GameObject priceRoot =
                CreateChild(parent, "GoldPriceIcons");
            float startX = -(price - 1) * 0.16f;
            for (int index = 0; index < price; index++)
            {
                CreatePart(
                    priceRoot.transform,
                    art.Crystal,
                    $"Gold_{index:00}",
                    new Vector2(startX + index * 0.32f, 1.62f),
                    new Vector2(0.22f, 0.22f),
                    0f,
                    new Color(1f, 0.76f, 0.18f, 1f),
                    28);
            }
        }

        private static ExitRefs BuildExit(
            Transform parent,
            SourceArt art)
        {
            GameObject exitRoot =
                CreateChild(parent, "Exit");
            GameObject stageExit =
                CreateChild(exitRoot.transform, "StageExit");
            stageExit.transform.position =
                CellCenter(P5MoonStageContract.ExitSafeCell);
            BoxCollider2D exitTrigger =
                stageExit.AddComponent<BoxCollider2D>();
            exitTrigger.isTrigger = true;
            exitTrigger.size = new Vector2(1.35f, 2.4f);

            SpriteRenderer dormant = CreatePart(
                stageExit.transform,
                art.Anchor,
                "DormantExitFrame",
                new Vector2(0.42f, 0.55f),
                new Vector2(1.65f, 2.9f),
                0f,
                new Color(0.34f, 0.68f, 0.78f, 0.9f),
                40);
            CreatePart(
                stageExit.transform,
                art.MoonB,
                "ExitCrescent",
                new Vector2(0.42f, 0.6f),
                new Vector2(1.1f, 1.6f),
                0f,
                new Color(0.72f, 0.94f, 1f, 0.92f),
                41);
            CreatePart(
                stageExit.transform,
                art.Star,
                "ExitStar",
                new Vector2(0.42f, 0.68f),
                new Vector2(0.38f, 0.38f),
                0f,
                new Color(1f, 0.96f, 0.68f, 1f),
                42);
            SpriteRenderer glow = CreatePart(
                stageExit.transform,
                art.MoonA,
                "FirstReachGlow",
                new Vector2(0.42f, 0.55f),
                new Vector2(2.35f, 2.35f),
                0f,
                new Color(0.66f, 0.9f, 1f, 0.08f),
                39);

            GameObject interactionPoint =
                CreateChild(stageExit.transform, "InteractionPoint");
            interactionPoint.transform.localPosition =
                new Vector3(0f, 0.3f, 0f);

            GameObject backtrack =
                CreateChild(exitRoot.transform, "BacktrackCue");
            GameObject icon =
                CreateChild(backtrack.transform, "UnresolvedEventPictogram");
            icon.transform.position =
                new Vector3(99.3f, 4.15f, 0f);
            CreatePart(
                icon.transform,
                art.Rabbit,
                "RabbitFace",
                new Vector2(-0.28f, 0f),
                new Vector2(0.58f, 0.58f),
                0f,
                new Color(0.84f, 0.72f, 1f, 1f),
                55);
            CreatePart(
                icon.transform,
                art.Anchor,
                "PestleGlyph",
                new Vector2(0.35f, 0f),
                new Vector2(0.24f, 0.68f),
                -18f,
                new Color(0.74f, 0.92f, 1f, 0.88f),
                56);

            GameObject highlight =
                CreateChild(backtrack.transform, "OptionalShelfHighlight");
            highlight.transform.position =
                new Vector3(86.5f, 5.25f, 0f);
            for (int index = 0; index < 3; index++)
            {
                float angle = index * 120f * Mathf.Deg2Rad;
                CreatePart(
                    highlight.transform,
                    art.Star,
                    $"PulseStar_{index:00}",
                    new Vector2(
                        Mathf.Cos(angle) * 0.62f,
                        Mathf.Sin(angle) * 0.48f),
                    Vector2.one * 0.28f,
                    0f,
                    new Color(0.82f, 0.58f, 1f, 0.92f),
                    50);
            }

            icon.SetActive(false);
            DisableChildRenderers(highlight.transform);
            return new ExitRefs(
                stageExit.transform,
                interactionPoint.transform,
                dormant,
                glow,
                backtrack.transform,
                icon,
                highlight);
        }

        private static Transform[] BuildBellVisualHierarchy(
            Transform parent,
            Camera camera,
            SourceArt art)
        {
            GameObject timeline =
                CreateChild(camera.transform, "MaruBellTimeline");
            timeline.transform.localPosition =
                new Vector3(0f, 0f, 10f);
            GameObject pattern =
                CreateChild(camera.transform, "BellPattern_ShortShortLong");
            pattern.transform.localPosition =
                new Vector3(-6.7f, 5.1f, 10f);
            float[] sizes = { 0.18f, 0.18f, 0.34f };
            for (int index = 0; index < sizes.Length; index++)
            {
                CreatePart(
                    pattern.transform,
                    art.MoonA,
                    $"BellBeat_{index:00}",
                    new Vector2(index * 0.34f, 0f),
                    Vector2.one * sizes[index],
                    0f,
                    new Color(0.74f, 0.9f, 1f, 0.72f),
                    210);
            }

            GameObject first =
                CreateChild(timeline.transform, "Bell_140_FirstDistantSilhouette");
            CreatePart(
                first.transform,
                art.Rabbit,
                "DistantMaruSilhouette",
                new Vector2(6.6f, -2.1f),
                new Vector2(1.15f, 1.45f),
                0f,
                new Color(0.08f, 0.1f, 0.18f, 0.72f),
                18);
            CreatePart(
                first.transform,
                art.Star,
                "FirstBellColdStar",
                new Vector2(5.65f, -1.85f),
                new Vector2(0.25f, 0.25f),
                0f,
                new Color(0.58f, 0.8f, 1f, 0.72f),
                19);

            GameObject second =
                CreateChild(timeline.transform, "Bell_185_FootstepsAndStarSeep");
            for (int index = 0; index < 7; index++)
            {
                CreatePart(
                    second.transform,
                    art.Star,
                    $"Footstep_{index:00}",
                    new Vector2(-2.2f + index * 0.72f, -4.8f + (index % 2) * 0.16f),
                    new Vector2(0.24f, 0.14f),
                    index % 2 == 0 ? -18f : 18f,
                    new Color(0.44f, 0.68f, 0.92f, 0.76f),
                    20);
            }

            GameObject arrival =
                CreateChild(timeline.transform, "Bell_215_ArrivalRequest");
            CreatePart(
                arrival.transform,
                art.Rabbit,
                "ArrivalMaruSilhouette",
                new Vector2(0f, -1.25f),
                new Vector2(2.5f, 3.1f),
                0f,
                new Color(0.04f, 0.05f, 0.11f, 0.88f),
                22);
            for (int index = 0; index < 5; index++)
            {
                CreatePart(
                    arrival.transform,
                    art.Star,
                    $"ArrivalSeep_{index:00}",
                    new Vector2(-1.5f + index * 0.75f, -3.15f + (index % 2) * 0.45f),
                    Vector2.one * 0.24f,
                    0f,
                    new Color(0.42f, 0.68f, 1f, 0.72f),
                    21);
            }

            first.SetActive(false);
            second.SetActive(false);
            arrival.SetActive(false);
            return new[]
            {
                first.transform,
                second.transform,
                arrival.transform
            };
        }

        private static void BuildRollingRiceCakeCue(
            Transform parent,
            SourceArt art)
        {
            GameObject hazards =
                parent.Find("Gameplay/Hazards") != null
                    ? parent.Find("Gameplay/Hazards").gameObject
                    : CreateChild(parent.Find("Gameplay"), "Hazards");
            GameObject riceCake =
                CreateChild(hazards.transform, "RollingRiceCakeCue");
            riceCake.transform.position =
                new Vector3(43.5f, 2.15f, 0f);
            CreatePart(
                riceCake.transform,
                art.Disc,
                "RiceCakeBody",
                Vector2.zero,
                new Vector2(1.05f, 1.05f),
                0f,
                new Color(1f, 0.88f, 0.7f, 1f),
                24);
            CreatePart(
                riceCake.transform,
                art.MoonA,
                "RiceCakeStamp",
                Vector2.zero,
                new Vector2(0.48f, 0.48f),
                0f,
                new Color(0.82f, 0.58f, 0.68f, 0.92f),
                25);
            CircleCollider2D trigger =
                riceCake.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.52f;

            GameObject telegraph =
                CreateChild(hazards.transform, "RollingRiceCakeTelegraph");
            for (int index = 0; index < 4; index++)
            {
                CreatePart(
                    telegraph.transform,
                    art.Star,
                    $"MotionStar_{index:00}",
                    new Vector2(40.8f + index * 0.52f, 2.35f),
                    Vector2.one * (0.14f + index * 0.03f),
                    -12f,
                    new Color(1f, 0.78f, 0.52f, 0.72f),
                    23);
            }
        }

        private static PersistentAssets LoadPersistentAssets()
        {
            GameObject bombObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    BombPrefabPath);
            return new PersistentAssets(
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PlayerPrefabPath),
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    StoryPestlePrefabPath),
                bombObject != null
                    ? bombObject.GetComponent<Bomb2D>()
                    : null,
                AssetDatabase.LoadAssetAtPath<P1MovementTuning>(
                    MovementTuningPath),
                AssetDatabase.LoadAssetAtPath<TileBase>(
                    ReinforcedTilePath),
                AssetDatabase.LoadAssetAtPath<TileBase>(
                    SoftSoilTilePath),
                AssetDatabase.LoadAssetAtPath<TileBase>(
                    OneWayTilePath));
        }

        private static SourceArt LoadSourceArt()
        {
            SourceArt art = new SourceArt(
                LoadSprite(SkySpritePath),
                LoadSprite(MountainASpritePath),
                LoadSprite(MountainBSpritePath),
                LoadSprite(CloudSpritePath),
                LoadSprite(MoonASpritePath),
                LoadSprite(MoonBSpritePath),
                LoadSprite(SquareSpritePath),
                LoadSprite(
                    PlatformSpritePath,
                    "Platforms and doors_4"),
                LoadSprite(
                    PlatformSpritePath,
                    "Platforms and doors_6"),
                LoadSprite(
                    BaseElementSpritePath,
                    "base element_0"),
                LoadSprite(
                    TreeSpritePath,
                    "Mount tree_0"),
                LoadSprite(
                    ItemSpritePath,
                    "dungeon items 2_20"),
                LoadSprite(StarSpritePath),
                LoadSprite(
                    CrystalSpritePath,
                    "Crystal elements_3"),
                LoadSprite(
                    RabbitSpritePath,
                    "char_black_full_1"));
            return art;
        }

        private static void ValidateDependencies(
            SourceArt art,
            PersistentAssets assets)
        {
            if (!art.IsComplete)
            {
                throw new InvalidOperationException(
                    "P5 Moon Palace source art is incomplete.");
            }

            if (!assets.IsComplete)
            {
                throw new InvalidOperationException(
                    "P5 requires P3_Player, P3 pestle/bomb, P1 tuning, and P4 Moon tiles.");
            }

            for (int index = 0; index < FixedRooms.Length; index++)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(
                        FixedRooms[index].PrefabPath) == null)
                {
                    throw new InvalidOperationException(
                        $"P5 fixed room source is missing: {FixedRooms[index].PrefabPath}");
                }
            }
        }

        private static TileDefinition[] RebuildP5TileDefinitions(
            PersistentAssets assets)
        {
            return new[]
            {
                RebuildTileDefinition(
                    ReinforcedDefinitionPath,
                    "P5_MoonReinforced",
                    assets.ReinforcedTile,
                    TileMaterialKind.ReinforcedWall,
                    TileBreakMethod.None,
                    true),
                RebuildTileDefinition(
                    SoftSoilDefinitionPath,
                    "P5_MoonSoftSoil",
                    assets.SoftSoilTile,
                    TileMaterialKind.Dirt,
                    TileBreakMethod.Shovel | TileBreakMethod.Bomb,
                    false)
            };
        }

        private static TileDefinition RebuildTileDefinition(
            string path,
            string id,
            TileBase tile,
            TileMaterialKind material,
            TileBreakMethod breakMethods,
            bool sacred)
        {
            AssetDatabase.DeleteAsset(path);
            TileDefinition definition =
                ScriptableObject.CreateInstance<TileDefinition>();
            definition.name = id + "_Definition";
            definition.Configure(
                id,
                tile,
                material,
                true,
                breakMethods,
                sacred,
                false);
            AssetDatabase.CreateAsset(definition, path);
            return definition;
        }

        private static GameObject CreateChild(
            Transform parent,
            string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject InstantiatePrefab(
            GameObject prefab,
            Scene scene,
            Transform parent,
            string name,
            Vector2 position)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            GameObject instance =
                (GameObject)PrefabUtility.InstantiatePrefab(
                    prefab,
                    scene);
            if (instance == null)
            {
                throw new InvalidOperationException(
                    $"Failed to instantiate prefab: {AssetDatabase.GetAssetPath(prefab)}");
            }

            instance.name = name;
            instance.transform.SetParent(parent, true);
            instance.transform.position =
                new Vector3(position.x, position.y, 0f);
            return instance;
        }

        private static SpriteRenderer CreatePart(
            Transform parent,
            Sprite sprite,
            string name,
            Vector2 localPosition,
            Vector2 size,
            float rotation,
            Color color,
            int sortingOrder)
        {
            GameObject visual = CreateChild(parent, name);
            visual.transform.localPosition =
                new Vector3(localPosition.x, localPosition.y, 0f);
            visual.transform.localRotation =
                Quaternion.Euler(0f, 0f, rotation);
            visual.transform.localScale =
                CalculateFitScale(sprite, size);
            SpriteRenderer renderer =
                visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static Sprite LoadSprite(
            string path,
            string spriteName = null)
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
                if (string.IsNullOrEmpty(spriteName)
                    || sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            return fallback;
        }

        private static Vector3 CalculateFitScale(
            Sprite sprite,
            Vector2 size)
        {
            if (sprite == null)
            {
                return Vector3.one;
            }

            Vector2 bounds = sprite.bounds.size;
            return new Vector3(
                bounds.x > 0.001f ? size.x / bounds.x : 1f,
                bounds.y > 0.001f ? size.y / bounds.y : 1f,
                1f);
        }

        private static void DisableRenderer(Transform target)
        {
            SpriteRenderer renderer =
                target != null
                    ? target.GetComponent<SpriteRenderer>()
                    : null;
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        private static void DisableChildRenderers(Transform target)
        {
            if (target == null)
            {
                return;
            }

            SpriteRenderer[] renderers =
                target.GetComponentsInChildren<SpriteRenderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                renderers[index].enabled = false;
            }
        }

        private static T RequireComponent<T>(GameObject target)
            where T : Component
        {
            T component = target != null
                ? target.GetComponent<T>()
                : null;
            if (component == null)
            {
                throw new InvalidOperationException(
                    $"{target?.name ?? "null"} requires {typeof(T).Name}.");
            }

            return component;
        }

        private static Vector2 CellCenter(Vector2Int cell)
        {
            return new Vector2(cell.x + 0.5f, cell.y + 0.5f);
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path)
                || AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent =
                Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private readonly struct GameplayRefs
        {
            public readonly GameObject Player;
            public readonly PlayerInputAdapter Input;
            public readonly Rigidbody2D Body;
            public readonly Collider2D Collider;
            public readonly PlayerRecovery Recovery;
            public readonly CarrySystem Carry;
            public readonly PlayerToolInventory2D Inventory;
            public readonly PlayerConsumableTools2D Consumables;
            public readonly PestleInteractionRegistry2D PestleRegistry;
            public readonly TileMutationService Mutation;
            public readonly ExplosionService2D Explosions;
            public readonly RopeInstaller2D RopeInstaller;
            public readonly Transform Systems;

            public GameplayRefs(
                GameObject player,
                PlayerInputAdapter input,
                Rigidbody2D body,
                Collider2D collider,
                PlayerRecovery recovery,
                CarrySystem carry,
                PlayerToolInventory2D inventory,
                PlayerConsumableTools2D consumables,
                PestleInteractionRegistry2D pestleRegistry,
                TileMutationService mutation,
                ExplosionService2D explosions,
                RopeInstaller2D ropeInstaller,
                Transform systems)
            {
                Player = player;
                Input = input;
                Body = body;
                Collider = collider;
                Recovery = recovery;
                Carry = carry;
                Inventory = inventory;
                Consumables = consumables;
                PestleRegistry = pestleRegistry;
                Mutation = mutation;
                Explosions = explosions;
                RopeInstaller = ropeInstaller;
                Systems = systems;
            }
        }

        private readonly struct RabbitEventRefs
        {
            public readonly GameObject Root;
            public readonly Transform MoonRabbit;
            public readonly Transform EmptyMortar;
            public readonly Transform PestleSilhouette;
            public readonly Transform StoryPestle;
            public readonly Transform MoonCakeReward;
            public readonly Transform RabbitReturn;
            public readonly Transform RecoveryAnchor;
            public readonly Transform InteractionPoint;

            public RabbitEventRefs(
                GameObject root,
                Transform moonRabbit,
                Transform emptyMortar,
                Transform pestleSilhouette,
                Transform storyPestle,
                Transform moonCakeReward,
                Transform rabbitReturn,
                Transform recoveryAnchor,
                Transform interactionPoint)
            {
                Root = root;
                MoonRabbit = moonRabbit;
                EmptyMortar = emptyMortar;
                PestleSilhouette = pestleSilhouette;
                StoryPestle = storyPestle;
                MoonCakeReward = moonCakeReward;
                RabbitReturn = rabbitReturn;
                RecoveryAnchor = recoveryAnchor;
                InteractionPoint = interactionPoint;
            }
        }

        private readonly struct EconomyRefs
        {
            public readonly GameObject Root;
            public readonly GameObject Shop;
            public readonly Transform[] GuaranteedGold;
            public readonly Transform OptionalBigGold;
            public readonly Transform[] ShopPedestals;
            public readonly Transform[] OfferInteractions;
            public readonly Transform[] ProductVisuals;

            public EconomyRefs(
                GameObject root,
                GameObject shop,
                Transform[] guaranteedGold,
                Transform optionalBigGold,
                Transform[] shopPedestals,
                Transform[] offerInteractions,
                Transform[] productVisuals)
            {
                Root = root;
                Shop = shop;
                GuaranteedGold = guaranteedGold;
                OptionalBigGold = optionalBigGold;
                ShopPedestals = shopPedestals;
                OfferInteractions = offerInteractions;
                ProductVisuals = productVisuals;
            }
        }

        private readonly struct ExitRefs
        {
            public readonly Transform Exit;
            public readonly Transform InteractionPoint;
            public readonly SpriteRenderer Dormant;
            public readonly SpriteRenderer Glow;
            public readonly Transform BacktrackCue;
            public readonly GameObject CueIcon;
            public readonly GameObject ShelfHighlight;

            public ExitRefs(
                Transform exit,
                Transform interactionPoint,
                SpriteRenderer dormant,
                SpriteRenderer glow,
                Transform backtrackCue,
                GameObject cueIcon,
                GameObject shelfHighlight)
            {
                Exit = exit;
                InteractionPoint = interactionPoint;
                Dormant = dormant;
                Glow = glow;
                BacktrackCue = backtrackCue;
                CueIcon = cueIcon;
                ShelfHighlight = shelfHighlight;
            }
        }

        private readonly struct PresentationRefs
        {
            public readonly Transform Entry;
            public readonly Transform Exit;
            public readonly Transform OptionalReward;
            public readonly Transform RabbitReturn;
            public readonly Transform MoonRabbit;
            public readonly Transform EmptyMortar;
            public readonly Transform PestleSilhouette;
            public readonly Transform StoryPestle;
            public readonly Transform MoonCakeReward;
            public readonly Transform[] GuaranteedGold;
            public readonly Transform[] ShopPedestals;
            public readonly Transform[] BellCues;
            public readonly Transform EntryGuidance;
            public readonly Transform BacktrackCue;
            public readonly RabbitEventRefs Rabbit;
            public readonly EconomyRefs Economy;
            public readonly ExitRefs ExitRefs;

            public PresentationRefs(
                Transform entry,
                Transform exit,
                Transform optionalReward,
                Transform rabbitReturn,
                Transform moonRabbit,
                Transform emptyMortar,
                Transform pestleSilhouette,
                Transform storyPestle,
                Transform moonCakeReward,
                Transform[] guaranteedGold,
                Transform[] shopPedestals,
                Transform[] bellCues,
                Transform entryGuidance,
                Transform backtrackCue,
                RabbitEventRefs rabbit,
                EconomyRefs economy,
                ExitRefs exitRefs)
            {
                Entry = entry;
                Exit = exit;
                OptionalReward = optionalReward;
                RabbitReturn = rabbitReturn;
                MoonRabbit = moonRabbit;
                EmptyMortar = emptyMortar;
                PestleSilhouette = pestleSilhouette;
                StoryPestle = storyPestle;
                MoonCakeReward = moonCakeReward;
                GuaranteedGold = guaranteedGold;
                ShopPedestals = shopPedestals;
                BellCues = bellCues;
                EntryGuidance = entryGuidance;
                BacktrackCue = backtrackCue;
                Rabbit = rabbit;
                Economy = economy;
                ExitRefs = exitRefs;
            }
        }

        private readonly struct PersistentAssets
        {
            public readonly GameObject PlayerPrefab;
            public readonly GameObject StoryPestlePrefab;
            public readonly Bomb2D BombPrefab;
            public readonly P1MovementTuning MovementTuning;
            public readonly TileBase ReinforcedTile;
            public readonly TileBase SoftSoilTile;
            public readonly TileBase OneWayTile;

            public bool IsComplete =>
                PlayerPrefab != null
                && StoryPestlePrefab != null
                && BombPrefab != null
                && MovementTuning != null
                && ReinforcedTile != null
                && SoftSoilTile != null
                && OneWayTile != null;

            public PersistentAssets(
                GameObject playerPrefab,
                GameObject storyPestlePrefab,
                Bomb2D bombPrefab,
                P1MovementTuning movementTuning,
                TileBase reinforcedTile,
                TileBase softSoilTile,
                TileBase oneWayTile)
            {
                PlayerPrefab = playerPrefab;
                StoryPestlePrefab = storyPestlePrefab;
                BombPrefab = bombPrefab;
                MovementTuning = movementTuning;
                ReinforcedTile = reinforcedTile;
                SoftSoilTile = softSoilTile;
                OneWayTile = oneWayTile;
            }
        }

        private readonly struct SourceArt
        {
            public readonly Sprite Sky;
            public readonly Sprite MountainA;
            public readonly Sprite MountainB;
            public readonly Sprite Cloud;
            public readonly Sprite MoonA;
            public readonly Sprite MoonB;
            public readonly Sprite Square;
            public readonly Sprite Anchor;
            public readonly Sprite Disc;
            public readonly Sprite BaseElement;
            public readonly Sprite Tree;
            public readonly Sprite Item;
            public readonly Sprite Star;
            public readonly Sprite Crystal;
            public readonly Sprite Rabbit;

            public bool IsComplete =>
                Sky != null
                && MountainA != null
                && MountainB != null
                && Cloud != null
                && MoonA != null
                && MoonB != null
                && Square != null
                && Anchor != null
                && Disc != null
                && BaseElement != null
                && Tree != null
                && Item != null
                && Star != null
                && Crystal != null
                && Rabbit != null;

            public SourceArt(
                Sprite sky,
                Sprite mountainA,
                Sprite mountainB,
                Sprite cloud,
                Sprite moonA,
                Sprite moonB,
                Sprite square,
                Sprite anchor,
                Sprite disc,
                Sprite baseElement,
                Sprite tree,
                Sprite item,
                Sprite star,
                Sprite crystal,
                Sprite rabbit)
            {
                Sky = sky;
                MountainA = mountainA;
                MountainB = mountainB;
                Cloud = cloud;
                MoonA = moonA;
                MoonB = moonB;
                Square = square;
                Anchor = anchor;
                Disc = disc;
                BaseElement = baseElement;
                Tree = tree;
                Item = item;
                Star = star;
                Crystal = crystal;
                Rabbit = rabbit;
            }
        }
    }
}

#endif
