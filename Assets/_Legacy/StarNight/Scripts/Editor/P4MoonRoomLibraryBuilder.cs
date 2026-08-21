#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.IO;
using StarNight.Debugging;
using StarNight.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Editor
{
    public static class P4MoonRoomLibraryBuilder
    {
        public const string ScenePath =
            "Assets/StarNight/Scenes/Labs/P4_MoonRoomPrefabLibrary.unity";
        public const string PrefabRoot =
            "Assets/StarNight/Prefabs/Rooms/P4";
        public const string LibraryPath =
            "Assets/StarNight/Data/P4/P4_MoonRoomPrefabLibrary.asset";

        private const string TileFolder =
            "Assets/StarNight/Data/P4/RoomTiles";
        private const string ReinforcedTilePath =
            TileFolder + "/P4_MoonReinforced.asset";
        private const string SoftSoilTilePath =
            TileFolder + "/P4_MoonSoftSoil.asset";
        private const string OneWayTilePath =
            TileFolder + "/P4_MoonOneWay.asset";
        private const string FixtureTilePath =
            TileFolder + "/P4_MoonFixture.asset";
        private const string HazardTilePath =
            TileFolder + "/P4_MoonHazard.asset";
        private const string DecorationTilePath =
            TileFolder + "/P4_MoonDecoration.asset";
        private const string LogicTilePath =
            TileFolder + "/P4_MoonLogic.asset";

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
        private const string RockSpritePath =
            "Assets/2D Fantasy sprite bundle/Dungeon pack/Sprites/rocks.png";
        private const string ItemSpritePath =
            "Assets/2D Fantasy sprite bundle/Dungeon pack/Sprites/dungeon items 2.png";
        private const string PanelSpritePath =
            "Assets/2D Fantasy sprite bundle/Abandoned station/Ancient base Sprites/Panel.png";
        private const string StarSpritePath =
            "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/Cristal Sprites/Star particle.png";
        private const string CrystalSpritePath =
            "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/Cristal Sprites/Crystal elements.png";
        private const string SpikeSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Danger spikes.png";

        [MenuItem("StarNight/P4/Rebuild Moon Room Prefab Library")]
        public static void Rebuild()
        {
            EnsureFolder(PrefabRoot);
            EnsureFolder(TileFolder);
            EnsureFolder(Path.GetDirectoryName(LibraryPath)?.Replace('\\', '/'));
            EnsureFolder(Path.GetDirectoryName(ScenePath)?.Replace('\\', '/'));

            SourceArt art = LoadSourceArt();
            GeneratedTiles tiles = RebuildTiles(art);
            P4RoomBuildSpec[] specs = P4RoomSpecCatalog.CreateAll();

            AssetDatabase.DeleteAsset(PrefabRoot);
            EnsureFolder(PrefabRoot);
            EnsureFolder(PrefabRoot + "/Small");
            EnsureFolder(PrefabRoot + "/Standard");
            EnsureFolder(PrefabRoot + "/Wide");
            EnsureFolder(PrefabRoot + "/Tall");
            EnsureFolder(PrefabRoot + "/Large");
            EnsureFolder(PrefabRoot + "/Special");

            List<GameObject> prefabs = new List<GameObject>(specs.Length);
            for (int index = 0; index < specs.Length; index++)
            {
                prefabs.Add(BuildPrefab(specs[index], art, tiles));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            prefabs = ReloadPrefabs(specs);
            RoomPrefabLibrary library = RebuildLibrary(prefabs);

            RoomTemplate2D[] templates = ExtractTemplates(prefabs);
            RoomValidationReport report =
                RoomPrefabValidator.ValidateLibrary(templates);
            if (!report.Passed)
            {
                throw new InvalidOperationException(
                    "P4 prefab validation failed:\n" + report);
            }

            BuildGalleryScene(specs, prefabs, library, art);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[StarNight P4] Moon room library rebuilt: {prefabs.Count} prefabs, validation PASS, {ScenePath}");
        }

        [MenuItem("StarNight/P4/Validate Moon Room Prefab Library")]
        public static void ValidateLibrary()
        {
            RoomPrefabLibrary library =
                AssetDatabase.LoadAssetAtPath<RoomPrefabLibrary>(LibraryPath);
            if (library == null)
            {
                throw new InvalidOperationException(
                    $"P4 room library asset is missing: {LibraryPath}");
            }

            RoomValidationReport report =
                RoomPrefabValidator.ValidateLibrary(library.GetTemplates());
            if (!report.Passed)
            {
                throw new InvalidOperationException(
                    "P4 room library validation failed:\n" + report);
            }

            Debug.Log(
                $"[StarNight P4] Room library validation PASS ({library.RoomPrefabs.Count} prefabs).");
        }

        private static GameObject BuildPrefab(
            P4RoomBuildSpec spec,
            SourceArt art,
            GeneratedTiles tiles)
        {
            GameObject root = new GameObject(spec.Id);
            try
            {
                UnityEngine.Grid grid = root.AddComponent<UnityEngine.Grid>();
                grid.cellSize = Vector3.one;

                BuildLocalBackdrop(root.transform, spec, art);
                Tilemap terrain = CreateTilemapLayer(
                    root.transform,
                    "Terrain",
                    0,
                    true,
                    true,
                    out TilemapCollider2D terrainCollider,
                    out CompositeCollider2D composite);
                Tilemap oneWay = CreateTilemapLayer(
                    root.transform,
                    "OneWay",
                    1,
                    false,
                    true,
                    out _,
                    out _);
                Tilemap fixture = CreateTilemapLayer(
                    root.transform,
                    "Fixture",
                    2,
                    false,
                    true,
                    out _,
                    out _);
                Tilemap hazard = CreateTilemapLayer(
                    root.transform,
                    "Hazard",
                    3,
                    false,
                    true,
                    out _,
                    out _);
                Tilemap decoration = CreateTilemapLayer(
                    root.transform,
                    "Decoration",
                    4,
                    false,
                    true,
                    out _,
                    out _);
                Tilemap logic = CreateTilemapLayer(
                    root.transform,
                    "Logic",
                    5,
                    false,
                    false,
                    out _,
                    out _);

                PaintRoom(
                    spec,
                    terrain,
                    oneWay,
                    fixture,
                    hazard,
                    decoration,
                    logic,
                    tiles);
                RoomSocket2D[] sockets =
                    BuildSockets(root.transform, spec, art, terrain, hazard, tiles);
                RoomContentSlot2D[] slots =
                    BuildContentSlots(
                        root.transform,
                        spec,
                        art,
                        terrain,
                        oneWay,
                        hazard,
                        tiles);
                BuildCoreVisuals(root.transform, spec, art);

                RefreshTilemaps(
                    terrain,
                    oneWay,
                    fixture,
                    hazard,
                    decoration,
                    logic);
                terrainCollider.ProcessTilemapChanges();
                composite.GenerateGeometry();

                RoomTemplate2D room = root.AddComponent<RoomTemplate2D>();
                room.Configure(
                    spec.Id,
                    spec.Archetype,
                    spec.Region,
                    spec.Roles,
                    spec.CoreActionSentence,
                    spec.MacroSize,
                    spec.LogicalSize,
                    spec.ValidationProfile,
                    spec.HazardBudget,
                    spec.EnemyBudget,
                    spec.IntroducedRuleCount,
                    spec.LandmarkCount,
                    spec.MovementLayerCount,
                    spec.ExpectedTraversalSeconds,
                    spec.RewardVisible,
                    spec.ExitBlockProof,
                    grid,
                    terrain,
                    oneWay,
                    fixture,
                    hazard,
                    decoration,
                    logic,
                    sockets,
                    slots);

                RoomValidationReport report = RoomPrefabValidator.Validate(room);
                if (!report.Passed)
                {
                    throw new InvalidOperationException(
                        $"P4 room build validation failed for {spec.Id}: "
                        + string.Join(" | ", report.Issues));
                }

                string folder = GetPrefabFolder(spec);
                string path = $"{folder}/{spec.Id}.prefab";
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (saved == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to save P4 room prefab: {path}");
                }

                return saved;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void PaintRoom(
            P4RoomBuildSpec spec,
            Tilemap terrain,
            Tilemap oneWay,
            Tilemap fixture,
            Tilemap hazard,
            Tilemap decoration,
            Tilemap logic,
            GeneratedTiles tiles)
        {
            FillHorizontal(
                terrain,
                tiles.Reinforced,
                0,
                spec.LogicalSize.x - 1,
                0);

            for (int x = 1; x < spec.LogicalSize.x; x += 3)
            {
                decoration.SetTile(
                    new Vector3Int(x, spec.LogicalSize.y - 1, 0),
                    tiles.Decoration);
            }

            switch (spec.LayoutKind)
            {
                case P4RoomLayoutKind.CrescentStep:
                    FillHorizontal(terrain, tiles.SoftSoil, 5, 6, 1);
                    break;
                case P4RoomLayoutKind.SoftSoilDip:
                    FillHorizontal(terrain, tiles.SoftSoil, 3, 8, 0);
                    FillHorizontal(oneWay, tiles.OneWay, 1, 3, 2);
                    FillHorizontal(oneWay, tiles.OneWay, 8, 10, 2);
                    break;
                case P4RoomLayoutKind.RiceCakeHop:
                    fixture.SetTile(new Vector3Int(6, 2, 0), tiles.Fixture);
                    break;
                case P4RoomLayoutKind.PestlePost:
                    terrain.SetTile(new Vector3Int(6, 1, 0), tiles.Reinforced);
                    fixture.SetTile(new Vector3Int(6, 2, 0), tiles.Fixture);
                    break;
                case P4RoomLayoutKind.RootArch:
                    FillHorizontal(terrain, tiles.SoftSoil, 4, 7, 2);
                    break;
                case P4RoomLayoutKind.CrackedShelf:
                    FillHorizontal(oneWay, tiles.OneWay, 6, 9, 3);
                    FillHorizontal(fixture, tiles.Fixture, 7, 8, 4);
                    break;
                case P4RoomLayoutKind.MortarBowl:
                    terrain.SetTile(new Vector3Int(4, 1, 0), tiles.SoftSoil);
                    terrain.SetTile(new Vector3Int(7, 1, 0), tiles.SoftSoil);
                    FillHorizontal(fixture, tiles.Fixture, 5, 6, 1);
                    break;
                case P4RoomLayoutKind.ReturnStep:
                    PaintLowLoop(oneWay, tiles.OneWay, spec.LogicalSize.x);
                    break;
                case P4RoomLayoutKind.ThreeBeatSteps:
                    FillHorizontal(terrain, tiles.SoftSoil, 3, 4, 1);
                    FillHorizontal(terrain, tiles.SoftSoil, 5, 6, 2);
                    FillHorizontal(terrain, tiles.SoftSoil, 7, 8, 1);
                    break;
                case P4RoomLayoutKind.SafeCrater:
                    FillHorizontal(oneWay, tiles.OneWay, 2, 4, 3);
                    FillHorizontal(oneWay, tiles.OneWay, 7, 9, 3);
                    break;
                case P4RoomLayoutKind.TwinLedge:
                    FillHorizontal(oneWay, tiles.OneWay, 2, 5, 2);
                    FillHorizontal(oneWay, tiles.OneWay, 6, 9, 4);
                    break;
                case P4RoomLayoutKind.OverUnder:
                    FillHorizontal(oneWay, tiles.OneWay, 1, 2, 2);
                    FillHorizontal(oneWay, tiles.OneWay, 3, 9, 4);
                    break;
                case P4RoomLayoutKind.CrescentSwitchback:
                    PaintSwitchback(
                        oneWay,
                        tiles.OneWay,
                        spec.LogicalSize.x,
                        spec.LogicalSize.y);
                    break;
                case P4RoomLayoutKind.RootBridge:
                    FillHorizontal(oneWay, tiles.OneWay, 2, 9, 3);
                    fixture.SetTile(new Vector3Int(5, 3, 0), tiles.Fixture);
                    break;
                case P4RoomLayoutKind.DropLoop:
                    FillHorizontal(oneWay, tiles.OneWay, 2, 5, 3);
                    FillHorizontal(oneWay, tiles.OneWay, 7, 9, 3);
                    break;
                case P4RoomLayoutKind.NarrowGallery:
                    FillHorizontal(terrain, tiles.Reinforced, 2, 8, 2);
                    break;
                case P4RoomLayoutKind.LongBridge:
                    PaintHighBridge(
                        oneWay,
                        tiles.OneWay,
                        spec.LogicalSize.x,
                        Mathf.Min(4, spec.LogicalSize.y - 2),
                        2);
                    break;
                case P4RoomLayoutKind.RollingDoughLane:
                    PaintSlope(
                        terrain,
                        tiles.SoftSoil,
                        spec.LogicalSize.x);
                    break;
                case P4RoomLayoutKind.TwinLayerCauseway:
                    PaintHighBridge(
                        oneWay,
                        tiles.OneWay,
                        spec.LogicalSize.x,
                        Mathf.Min(4, spec.LogicalSize.y - 2),
                        1);
                    break;
                case P4RoomLayoutKind.RootCauseway:
                    FillHorizontal(oneWay, tiles.OneWay, 3, 8, 3);
                    FillHorizontal(oneWay, tiles.OneWay, 11, 16, 3);
                    FillHorizontal(oneWay, tiles.OneWay, 19, 21, 3);
                    break;
                case P4RoomLayoutKind.CrackedShortcut:
                    FillHorizontal(oneWay, tiles.OneWay, 2, 3, 2);
                    FillHorizontal(oneWay, tiles.OneWay, 4, 19, 4);
                    FillHorizontal(fixture, tiles.Fixture, 9, 14, 5);
                    break;
                case P4RoomLayoutKind.CassiaSwitchback:
                    PaintTallSwitchback(
                        oneWay,
                        tiles.OneWay,
                        spec.LogicalSize.x,
                        spec.LogicalSize.y,
                        false);
                    break;
                case P4RoomLayoutKind.ReturnableWell:
                    PaintTallSwitchback(
                        oneWay,
                        tiles.OneWay,
                        spec.LogicalSize.x,
                        spec.LogicalSize.y,
                        true);
                    FillVertical(fixture, tiles.Fixture, 5, 3, 13);
                    break;
                case P4RoomLayoutKind.RopeComparison:
                    PaintTallSwitchback(
                        oneWay,
                        tiles.OneWay,
                        spec.LogicalSize.x,
                        spec.LogicalSize.y,
                        false);
                    FillVertical(fixture, tiles.Fixture, 5, 2, 13);
                    break;
                case P4RoomLayoutKind.HookBalcony:
                    PaintTallSwitchback(
                        oneWay,
                        tiles.OneWay,
                        spec.LogicalSize.x,
                        spec.LogicalSize.y,
                        false);
                    fixture.SetTile(new Vector3Int(8, 12, 0), tiles.Fixture);
                    break;
                case P4RoomLayoutKind.WindShaft:
                    PaintTallSwitchback(
                        oneWay,
                        tiles.OneWay,
                        spec.LogicalSize.x,
                        spec.LogicalSize.y,
                        true);
                    FillVertical(hazard, tiles.Decoration, 5, 5, 12);
                    break;
                case P4RoomLayoutKind.CassiaMillCross:
                    PaintLargeLayers(
                        oneWay,
                        tiles.OneWay,
                        spec.LogicalSize.x,
                        spec.LogicalSize.y,
                        0);
                    break;
                case P4RoomLayoutKind.MortarAtrium:
                    PaintLargeLayers(
                        oneWay,
                        tiles.OneWay,
                        spec.LogicalSize.x,
                        spec.LogicalSize.y,
                        1);
                    break;
                case P4RoomLayoutKind.CrescentTerraces:
                    PaintLargeLayers(
                        oneWay,
                        tiles.OneWay,
                        spec.LogicalSize.x,
                        spec.LogicalSize.y,
                        2);
                    break;
                case P4RoomLayoutKind.MomoShop:
                    FillHorizontal(oneWay, tiles.OneWay, 3, 4, 1);
                    FillHorizontal(oneWay, tiles.OneWay, 8, 9, 1);
                    FillHorizontal(oneWay, tiles.OneWay, 13, 14, 1);
                    break;
                case P4RoomLayoutKind.SupplyRest:
                    FillHorizontal(oneWay, tiles.OneWay, 3, 4, 1);
                    FillHorizontal(oneWay, tiles.OneWay, 7, 8, 1);
                    FillHorizontal(oneWay, tiles.OneWay, 10, 11, 1);
                    break;
                case P4RoomLayoutKind.RecordArchive:
                    FillHorizontal(fixture, tiles.Fixture, 5, 9, 2);
                    break;
                case P4RoomLayoutKind.MaruStatueSanctum:
                    fixture.SetTile(new Vector3Int(8, 2, 0), tiles.Fixture);
                    fixture.SetTile(new Vector3Int(8, 3, 0), tiles.Fixture);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(spec.LayoutKind),
                        spec.LayoutKind,
                        null);
            }

            for (int index = 0; index < spec.Slots.Length; index++)
            {
                P4SlotBuildSpec slot = spec.Slots[index];
                logic.SetTile(
                    new Vector3Int(slot.CellOffset.x, slot.CellOffset.y, 0),
                    tiles.Logic);
            }
        }

        private static RoomSocket2D[] BuildSockets(
            Transform parent,
            P4RoomBuildSpec spec,
            SourceArt art,
            Tilemap terrain,
            Tilemap hazard,
            GeneratedTiles tiles)
        {
            GameObject socketRoot = new GameObject("Sockets");
            socketRoot.transform.SetParent(parent);
            RoomSocket2D[] sockets =
                new RoomSocket2D[spec.Sockets.Length];
            for (int index = 0; index < spec.Sockets.Length; index++)
            {
                P4SocketBuildSpec buildSpec = spec.Sockets[index];
                EnsureStandable(
                    buildSpec.ValidationAnchor,
                    terrain,
                    null,
                    hazard,
                    tiles.Reinforced,
                    tiles.OneWay);

                GameObject socketObject =
                    new GameObject($"Socket_{index:00}_{buildSpec.Id}");
                socketObject.transform.SetParent(socketRoot.transform);
                RoomSocket2D socket =
                    socketObject.AddComponent<RoomSocket2D>();
                socket.Configure(
                    buildSpec.Id,
                    buildSpec.Side,
                    buildSpec.CellOffset,
                    buildSpec.OpeningSize,
                    buildSpec.TraversalType,
                    buildSpec.MainRouteAllowed,
                    buildSpec.ValidationAnchor);
                sockets[index] = socket;

                float flip = buildSpec.Side == RoomSocketSide.Left ? -1f : 1f;
                CreatePart(
                    socketObject.transform,
                    art.Anchor,
                    "CrescentSocket",
                    new Vector2(0.12f * flip, 0f),
                    new Vector2(0.72f, 1.25f),
                    buildSpec.Side == RoomSocketSide.Left ? 180f : 0f,
                    new Color(0.36f, 0.92f, 0.96f, 0.9f),
                    12);
                CreatePart(
                    socketObject.transform,
                    art.Star,
                    "SocketStar",
                    new Vector2(0f, 0.12f),
                    Vector2.one * 0.22f,
                    0f,
                    new Color(1f, 0.88f, 0.32f, 1f),
                    13);
            }

            return sockets;
        }

        private static RoomContentSlot2D[] BuildContentSlots(
            Transform parent,
            P4RoomBuildSpec spec,
            SourceArt art,
            Tilemap terrain,
            Tilemap oneWay,
            Tilemap hazard,
            GeneratedTiles tiles)
        {
            GameObject slotRoot = new GameObject("ContentSlots");
            slotRoot.transform.SetParent(parent);
            RoomContentSlot2D[] slots =
                new RoomContentSlot2D[spec.Slots.Length];
            for (int index = 0; index < spec.Slots.Length; index++)
            {
                P4SlotBuildSpec buildSpec = spec.Slots[index];
                if (RequiresStandableCell(buildSpec.Kind))
                {
                    EnsureVerticalAccess(
                        buildSpec.CellOffset,
                        terrain,
                        oneWay,
                        hazard,
                        tiles);
                }

                GameObject slotObject =
                    new GameObject($"Slot_{index:00}_{buildSpec.Id}");
                slotObject.transform.SetParent(slotRoot.transform);
                RoomContentSlot2D slot =
                    slotObject.AddComponent<RoomContentSlot2D>();
                slot.Configure(
                    buildSpec.Id,
                    buildSpec.Kind,
                    buildSpec.CellOffset,
                    buildSpec.VisibleFromMainRoute);
                slots[index] = slot;
                BuildSlotVisual(slotObject.transform, buildSpec.Kind, art);
            }

            return slots;
        }

        private static void BuildSlotVisual(
            Transform parent,
            RoomContentSlotKind kind,
            SourceArt art)
        {
            Sprite sprite = art.Star;
            Vector2 size = Vector2.one * 0.28f;
            Color color = new Color(0.4f, 0.95f, 0.72f, 0.86f);
            int order = 15;
            switch (kind)
            {
                case RoomContentSlotKind.SafeCell:
                    color = new Color(0.41f, 0.93f, 0.72f, 0.9f);
                    break;
                case RoomContentSlotKind.OptionalReward:
                case RoomContentSlotKind.Treasure:
                    sprite = art.Crystal;
                    size = Vector2.one * 0.52f;
                    color = new Color(1f, 0.84f, 0.24f, 1f);
                    break;
                case RoomContentSlotKind.Trap:
                    sprite = art.Spike;
                    size = new Vector2(0.7f, 0.45f);
                    color = new Color(0.94f, 0.38f, 0.3f, 0.9f);
                    break;
                case RoomContentSlotKind.Enemy:
                    sprite = art.Rock;
                    size = Vector2.one * 0.62f;
                    color = new Color(0.72f, 0.46f, 0.88f, 0.88f);
                    break;
                case RoomContentSlotKind.Shop:
                    sprite = art.Item;
                    size = Vector2.one * 0.65f;
                    color = new Color(1f, 0.72f, 0.28f, 1f);
                    break;
                case RoomContentSlotKind.Supply:
                    sprite = art.Item;
                    size = Vector2.one * 0.58f;
                    color = new Color(0.44f, 0.88f, 1f, 1f);
                    break;
                case RoomContentSlotKind.RecordGuest:
                    sprite = art.Panel;
                    size = new Vector2(1.35f, 1.4f);
                    color = new Color(0.78f, 0.86f, 1f, 1f);
                    break;
                case RoomContentSlotKind.MaruStatue:
                    sprite = art.MoonB;
                    size = new Vector2(1.2f, 2f);
                    color = new Color(0.58f, 0.68f, 0.82f, 1f);
                    break;
                case RoomContentSlotKind.MaruEntry:
                    sprite = art.MoonA;
                    size = Vector2.one * 0.5f;
                    color = new Color(0.48f, 0.82f, 1f, 0.32f);
                    order = 8;
                    break;
                case RoomContentSlotKind.Landmark:
                    sprite = art.Tree;
                    size = new Vector2(3.8f, 6.4f);
                    color = new Color(0.74f, 0.56f, 0.38f, 0.92f);
                    order = 6;
                    break;
                case RoomContentSlotKind.LocalEvent:
                case RoomContentSlotKind.Npc:
                    sprite = art.Anchor;
                    size = new Vector2(0.7f, 1.1f);
                    color = new Color(0.82f, 0.68f, 1f, 1f);
                    break;
            }

            CreatePart(
                parent,
                sprite,
                "SlotVisual",
                Vector2.zero,
                size,
                0f,
                color,
                order);
        }

        private static void BuildCoreVisuals(
            Transform parent,
            P4RoomBuildSpec spec,
            SourceArt art)
        {
            GameObject visuals = new GameObject("ThemeFixtures");
            visuals.transform.SetParent(parent);
            Vector2 center = new Vector2(
                spec.LogicalSize.x * 0.5f,
                Mathf.Max(2f, spec.LogicalSize.y * 0.42f));

            if (spec.LayoutKind == P4RoomLayoutKind.CassiaMillCross)
            {
                CreatePart(
                    visuals.transform,
                    art.Tree,
                    "CassiaLandmark",
                    center,
                    new Vector2(5.2f, 8.5f),
                    0f,
                    new Color(0.62f, 0.46f, 0.31f, 0.88f),
                    5);
            }
            else if (spec.LayoutKind == P4RoomLayoutKind.MortarAtrium)
            {
                CreatePart(
                    visuals.transform,
                    art.MoonB,
                    "MortarLandmark",
                    center + Vector2.down,
                    new Vector2(5.5f, 4.5f),
                    180f,
                    new Color(0.52f, 0.62f, 0.78f, 0.9f),
                    5);
            }
            else if (spec.LayoutKind == P4RoomLayoutKind.CrescentTerraces)
            {
                CreatePart(
                    visuals.transform,
                    art.Anchor,
                    "CrescentGateLandmark",
                    center,
                    new Vector2(4.2f, 7.2f),
                    0f,
                    new Color(0.62f, 0.91f, 0.96f, 0.86f),
                    5);
            }
            else if (spec.LayoutKind == P4RoomLayoutKind.RecordArchive)
            {
                CreatePart(
                    visuals.transform,
                    art.Panel,
                    "ArchivePanel",
                    new Vector2(8.5f, 2.4f),
                    new Vector2(2.2f, 2.1f),
                    0f,
                    new Color(0.72f, 0.82f, 1f, 0.9f),
                    6);
            }
            else if (spec.LayoutKind == P4RoomLayoutKind.MaruStatueSanctum)
            {
                CreatePart(
                    visuals.transform,
                    art.Anchor,
                    "StatueBody",
                    new Vector2(8.5f, 2f),
                    new Vector2(1.4f, 2.8f),
                    0f,
                    new Color(0.36f, 0.43f, 0.58f, 0.96f),
                    6);
                CreatePart(
                    visuals.transform,
                    art.Star,
                    "StarTear",
                    new Vector2(8.5f, 2.35f),
                    Vector2.one * 0.38f,
                    0f,
                    new Color(1f, 0.82f, 0.3f, 1f),
                    16);
            }
            else if (spec.LayoutKind == P4RoomLayoutKind.MomoShop)
            {
                CreatePart(
                    visuals.transform,
                    art.MoonA,
                    "ShopCanopy",
                    new Vector2(spec.LogicalSize.x * 0.5f, 3.9f),
                    new Vector2(6.2f, 1.4f),
                    0f,
                    new Color(0.74f, 0.56f, 0.92f, 0.76f),
                    5);
            }
            else if (spec.LayoutKind == P4RoomLayoutKind.SupplyRest)
            {
                CreatePart(
                    visuals.transform,
                    art.Cloud,
                    "SupplyCanopy",
                    new Vector2(spec.LogicalSize.x * 0.5f, 3.8f),
                    new Vector2(5.4f, 1.6f),
                    0f,
                    new Color(0.58f, 0.86f, 0.95f, 0.72f),
                    5);
            }

            if (spec.LayoutKind == P4RoomLayoutKind.RopeComparison
                || spec.LayoutKind == P4RoomLayoutKind.HookBalcony
                || spec.LayoutKind == P4RoomLayoutKind.WindShaft)
            {
                CreatePart(
                    visuals.transform,
                    art.Anchor,
                    "ToolVista",
                    new Vector2(spec.LogicalSize.x * 0.5f, 12.5f),
                    new Vector2(0.9f, 1.6f),
                    0f,
                    new Color(0.74f, 0.62f, 1f, 0.9f),
                    14);
            }
        }

        private static void BuildLocalBackdrop(
            Transform parent,
            P4RoomBuildSpec spec,
            SourceArt art)
        {
            Color tint = GetRoomTint(spec);
            CreatePart(
                parent,
                art.Square,
                "RoomWash",
                new Vector2(
                    spec.LogicalSize.x * 0.5f,
                    spec.LogicalSize.y * 0.5f),
                new Vector2(spec.LogicalSize.x, spec.LogicalSize.y),
                0f,
                tint,
                -30);
            CreatePart(
                parent,
                art.MoonA,
                "MoonMotif",
                new Vector2(
                    spec.LogicalSize.x - 2f,
                    spec.LogicalSize.y - 1.8f),
                Vector2.one * Mathf.Min(3f, spec.LogicalSize.y * 0.28f),
                0f,
                new Color(0.72f, 0.9f, 0.96f, 0.2f),
                -20);
            CreatePart(
                parent,
                art.Cloud,
                "CloudMotif",
                new Vector2(2.5f, spec.LogicalSize.y - 1.4f),
                new Vector2(3.8f, 1.2f),
                0f,
                new Color(0.82f, 0.9f, 1f, 0.18f),
                -19);
        }

        private static void BuildGalleryScene(
            P4RoomBuildSpec[] specs,
            IReadOnlyList<GameObject> prefabs,
            RoomPrefabLibrary library,
            SourceArt art)
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            GameObject root = new GameObject("P4_MoonRoomPrefabLibrary");
            BuildGalleryBackdrop(root.transform, art);

            GameObject gallery = new GameObject("RoomGallery");
            gallery.transform.SetParent(root.transform);
            RoomTemplate2D[] instances =
                new RoomTemplate2D[prefabs.Count];
            Bounds galleryBounds = default;
            bool hasBounds = false;
            float cursorX = 4f;
            float cursorY = 4f;
            float rowHeight = 0f;
            const float MaximumWidth = 150f;
            const float HorizontalGap = 5f;
            const float VerticalGap = 7f;

            for (int index = 0; index < prefabs.Count; index++)
            {
                Vector2Int size = specs[index].LogicalSize;
                if (cursorX + size.x > MaximumWidth && cursorX > 4f)
                {
                    cursorX = 4f;
                    cursorY += rowHeight + VerticalGap;
                    rowHeight = 0f;
                }

                GameObject instance =
                    (GameObject)PrefabUtility.InstantiatePrefab(
                        prefabs[index],
                        scene);
                instance.name = $"Room_{index + 1:00}_{specs[index].Id}";
                instance.transform.SetParent(gallery.transform);
                instance.transform.position =
                    new Vector3(cursorX, cursorY, 0f);
                instances[index] = instance.GetComponent<RoomTemplate2D>();

                Bounds roomBounds = new Bounds(
                    new Vector3(
                        cursorX + size.x * 0.5f,
                        cursorY + size.y * 0.5f,
                        0f),
                    new Vector3(size.x, size.y, 1f));
                if (!hasBounds)
                {
                    galleryBounds = roomBounds;
                    hasBounds = true;
                }
                else
                {
                    galleryBounds.Encapsulate(roomBounds);
                }

                cursorX += size.x + HorizontalGap;
                rowHeight = Mathf.Max(rowHeight, size.y);
            }

            P4RoomLibraryContract contract =
                root.AddComponent<P4RoomLibraryContract>();
            contract.Configure(library, instances);
            if (contract.LastValidation != "PASS")
            {
                throw new InvalidOperationException(
                    "P4 gallery validation failed:\n" + contract.LastValidation);
            }

            Camera camera = BuildCamera(root.transform, galleryBounds);
            camera.gameObject.AddComponent<P4RoomGalleryCamera2D>().Configure(
                instances,
                galleryBounds,
                3f);
            BuildDirectionalLight(root.transform);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void BuildGalleryBackdrop(Transform parent, SourceArt art)
        {
            for (int index = 0; index < 6; index++)
            {
                CreatePart(
                    parent,
                    art.Sky,
                    $"GallerySky_{index:00}",
                    new Vector2(15f + index * 30f, 55f),
                    new Vector2(31f, 115f),
                    0f,
                    Color.white,
                    -100);
                CreatePart(
                    parent,
                    index % 2 == 0 ? art.MountainA : art.MountainB,
                    $"GalleryMountains_{index:00}",
                    new Vector2(15f + index * 30f, 17f),
                    new Vector2(31f, 28f),
                    0f,
                    new Color(0.28f, 0.34f, 0.52f, 0.55f),
                    -90);
            }

            CreatePart(
                parent,
                art.MoonB,
                "GalleryMoon",
                new Vector2(135f, 95f),
                new Vector2(18f, 18f),
                0f,
                new Color(0.78f, 0.92f, 1f, 0.65f),
                -80);
        }

        private static Camera BuildCamera(
            Transform parent,
            Bounds galleryBounds)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(
                galleryBounds.center.x,
                galleryBounds.center.y,
                -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.07f, 0.16f, 1f);
            camera.orthographicSize =
                Mathf.Max(galleryBounds.extents.y + 3f, 45f);
            return camera;
        }

        private static void BuildDirectionalLight(Transform parent)
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(parent);
            lightObject.transform.rotation =
                Quaternion.Euler(42f, -28f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.78f, 0.9f, 1f, 1f);
            light.intensity = 1.05f;
        }

        private static RoomPrefabLibrary RebuildLibrary(
            IReadOnlyList<GameObject> prefabs)
        {
            AssetDatabase.DeleteAsset(LibraryPath);
            RoomPrefabLibrary library =
                ScriptableObject.CreateInstance<RoomPrefabLibrary>();
            library.name = "P4_MoonRoomPrefabLibrary";
            GameObject[] array = new GameObject[prefabs.Count];
            for (int index = 0; index < prefabs.Count; index++)
            {
                array[index] = prefabs[index];
            }

            library.Configure(RoomRegion.MoonPalace, array);
            AssetDatabase.CreateAsset(library, LibraryPath);
            return library;
        }

        private static List<GameObject> ReloadPrefabs(
            IReadOnlyList<P4RoomBuildSpec> specs)
        {
            List<GameObject> prefabs =
                new List<GameObject>(specs.Count);
            for (int index = 0; index < specs.Count; index++)
            {
                string path =
                    $"{GetPrefabFolder(specs[index])}/{specs[index].Id}.prefab";
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Generated P4 prefab failed to reload: {path}");
                }

                prefabs.Add(prefab);
            }

            return prefabs;
        }

        private static RoomTemplate2D[] ExtractTemplates(
            IReadOnlyList<GameObject> prefabs)
        {
            RoomTemplate2D[] rooms =
                new RoomTemplate2D[prefabs.Count];
            for (int index = 0; index < prefabs.Count; index++)
            {
                rooms[index] = prefabs[index] != null
                    ? prefabs[index].GetComponent<RoomTemplate2D>()
                    : null;
            }

            return rooms;
        }

        private static GeneratedTiles RebuildTiles(SourceArt art)
        {
            return new GeneratedTiles
            {
                Reinforced = RebuildTile(
                    ReinforcedTilePath,
                    "P4_MoonReinforced",
                    art.Ground,
                    new Color(0.20f, 0.25f, 0.35f, 1f),
                    Tile.ColliderType.Grid),
                SoftSoil = RebuildTile(
                    SoftSoilTilePath,
                    "P4_MoonSoftSoil",
                    art.Ground,
                    new Color(0.46f, 0.36f, 0.45f, 1f),
                    Tile.ColliderType.Grid),
                OneWay = RebuildTile(
                    OneWayTilePath,
                    "P4_MoonOneWay",
                    art.Ground,
                    new Color(0.47f, 0.60f, 0.72f, 1f),
                    Tile.ColliderType.Grid,
                    new Vector2(1f, 0.32f)),
                Fixture = RebuildTile(
                    FixtureTilePath,
                    "P4_MoonFixture",
                    art.BaseElement,
                    new Color(0.71f, 0.58f, 0.42f, 0.9f),
                    Tile.ColliderType.None,
                    new Vector2(0.62f, 0.72f)),
                Hazard = RebuildTile(
                    HazardTilePath,
                    "P4_MoonHazard",
                    art.Spike,
                    new Color(0.94f, 0.38f, 0.30f, 0.92f),
                    Tile.ColliderType.None,
                    new Vector2(0.8f, 0.55f)),
                Decoration = RebuildTile(
                    DecorationTilePath,
                    "P4_MoonDecoration",
                    art.Star,
                    new Color(0.78f, 0.92f, 1f, 0.62f),
                    Tile.ColliderType.None,
                    Vector2.one * 0.3f),
                Logic = RebuildTile(
                    LogicTilePath,
                    "P4_MoonLogic",
                    art.Square,
                    new Color(0.25f, 0.9f, 1f, 0.12f),
                    Tile.ColliderType.None,
                    Vector2.one * 0.34f)
            };
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
                MoonA = LoadSprite(MoonASpritePath),
                MoonB = LoadSprite(MoonBSpritePath),
                Square = LoadSprite(SquareSpritePath),
                Anchor = LoadSprite(
                    PlatformSpritePath,
                    "Platforms and doors_4"),
                BaseElement = LoadSprite(
                    BaseElementSpritePath,
                    "base element_0"),
                Tree = LoadSprite(TreeSpritePath, "Mount tree_0"),
                Rock = LoadSprite(RockSpritePath, "rocks_3"),
                Item = LoadSprite(ItemSpritePath, "dungeon items 2_20"),
                Panel = LoadSprite(PanelSpritePath),
                Star = LoadSprite(StarSpritePath),
                Crystal = LoadSprite(
                    CrystalSpritePath,
                    "Crystal elements_3"),
                Spike = LoadSprite(SpikeSpritePath)
            };

            if (!art.IsComplete)
            {
                throw new InvalidOperationException(
                    "P4 Moon Palace theme art is missing from the 2D Fantasy Sprite Bundle.");
            }

            return art;
        }

        private static Tilemap CreateTilemapLayer(
            Transform parent,
            string name,
            int sortingOrder,
            bool collision,
            bool visible,
            out TilemapCollider2D tilemapCollider,
            out CompositeCollider2D composite)
        {
            GameObject layer = new GameObject(name);
            layer.transform.SetParent(parent);
            Tilemap tilemap = layer.AddComponent<Tilemap>();
            TilemapRenderer renderer = layer.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.enabled = visible;

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

        private static void RefreshTilemaps(params Tilemap[] tilemaps)
        {
            for (int index = 0; index < tilemaps.Length; index++)
            {
                tilemaps[index].CompressBounds();
                tilemaps[index].RefreshAllTiles();
            }
        }

        private static void PaintLowLoop(
            Tilemap oneWay,
            TileBase tile,
            int width)
        {
            FillHorizontal(oneWay, tile, 2, width - 3, 2);
            FillHorizontal(oneWay, tile, 4, width - 5, 4);
        }

        private static void PaintSwitchback(
            Tilemap oneWay,
            TileBase tile,
            int width,
            int height)
        {
            for (int y = 2; y < height - 1; y += 2)
            {
                if ((y / 2) % 2 == 1)
                {
                    FillHorizontal(oneWay, tile, 2, width - 5, y);
                }
                else
                {
                    FillHorizontal(oneWay, tile, 4, width - 3, y);
                }
            }
        }

        private static void PaintHighBridge(
            Tilemap oneWay,
            TileBase tile,
            int width,
            int y,
            int gaps)
        {
            FillHorizontal(oneWay, tile, 3, width - 4, y);
            for (int index = 0; index < gaps; index++)
            {
                int gapX = 8 + index * 7;
                oneWay.SetTile(new Vector3Int(gapX, y, 0), null);
            }

            FillHorizontal(oneWay, tile, 2, 4, 2);
            FillHorizontal(oneWay, tile, width - 5, width - 3, 2);
        }

        private static void PaintSlope(
            Tilemap terrain,
            TileBase tile,
            int width)
        {
            for (int x = 4; x < width - 3; x += 4)
            {
                terrain.SetTile(new Vector3Int(x, 1, 0), tile);
            }
        }

        private static void PaintTallSwitchback(
            Tilemap oneWay,
            TileBase tile,
            int width,
            int height,
            bool centralGap)
        {
            int leftEnd = Mathf.Max(4, width - 5);
            int rightStart = Mathf.Clamp(width / 3, 2, width - 4);
            int rightEnd = width - 2;
            int gapX = width / 2;
            for (int y = 2; y <= height - 2; y += 2)
            {
                if ((y / 2) % 2 == 1)
                {
                    FillHorizontal(oneWay, tile, 1, leftEnd, y);
                }
                else
                {
                    FillHorizontal(
                        oneWay,
                        tile,
                        rightStart,
                        rightEnd,
                        y);
                }

                if (centralGap)
                {
                    oneWay.SetTile(
                        new Vector3Int(gapX, y, 0),
                        null);
                }
            }
        }

        private static void PaintLargeLayers(
            Tilemap oneWay,
            TileBase tile,
            int width,
            int height,
            int variant)
        {
            int outerLeft = 2 + variant;
            int outerRight = width - 3 - variant;
            int innerLeft = 6 + variant;
            int innerRight = width - 7 - variant;

            FillHorizontal(oneWay, tile, outerLeft, outerLeft + 4, 2);
            FillHorizontal(oneWay, tile, outerRight - 4, outerRight, 2);
            FillHorizontal(oneWay, tile, innerLeft, width / 2 - 2, 4);
            FillHorizontal(oneWay, tile, width / 2 + 1, innerRight, 4);
            FillHorizontal(
                oneWay,
                tile,
                width / 2 - 2,
                width / 2 + 1,
                6);
            FillHorizontal(oneWay, tile, innerLeft, width / 2 - 2, 8);
            FillHorizontal(oneWay, tile, width / 2 + 1, innerRight, 8);
            FillHorizontal(oneWay, tile, outerLeft, outerLeft + 4, 10);
            FillHorizontal(oneWay, tile, outerRight - 4, outerRight, 10);
        }

        private static void EnsureVerticalAccess(
            Vector2Int target,
            Tilemap terrain,
            Tilemap oneWay,
            Tilemap hazard,
            GeneratedTiles tiles)
        {
            int alternateX = Mathf.Max(0, target.x - 2);
            for (int y = 1; y <= target.y + 2; y++)
            {
                for (int x = alternateX; x <= target.x; x++)
                {
                    Vector3Int shaftCell = new Vector3Int(x, y, 0);
                    terrain.SetTile(shaftCell, null);
                    oneWay?.SetTile(shaftCell, null);
                    hazard.SetTile(shaftCell, null);
                }
            }

            List<Vector2Int> accessCells = new List<Vector2Int> { target };
            Vector2Int current = target;
            bool useAlternate = true;
            while (current.y > 1)
            {
                current = new Vector2Int(
                    useAlternate ? alternateX : target.x,
                    Mathf.Max(1, current.y - 2));
                accessCells.Add(current);
                useAlternate = !useAlternate;
            }

            accessCells.Reverse();
            int groundEndX = accessCells[0].x;
            for (int approachX = 0;
                 approachX <= groundEndX;
                 approachX++)
            {
                EnsureStandable(
                    new Vector2Int(approachX, 1),
                    terrain,
                    oneWay,
                    hazard,
                    tiles.Reinforced,
                    tiles.OneWay);
            }

            for (int index = 0; index < accessCells.Count; index++)
            {
                EnsureStandable(
                    accessCells[index],
                    terrain,
                    oneWay,
                    hazard,
                    tiles.Reinforced,
                    tiles.OneWay);
            }
        }

        private static void EnsureStandable(
            Vector2Int target,
            Tilemap terrain,
            Tilemap oneWay,
            Tilemap hazard,
            TileBase terrainTile,
            TileBase oneWayTile)
        {
            Vector3Int cell = new Vector3Int(target.x, target.y, 0);
            terrain.SetTile(cell, null);
            oneWay?.SetTile(cell, null);
            hazard.SetTile(cell, null);
            Vector3Int support =
                new Vector3Int(target.x, target.y - 1, 0);
            if (target.y > 1 && oneWay != null)
            {
                oneWay.SetTile(support, oneWayTile);
            }
            else
            {
                terrain.SetTile(support, terrainTile);
            }
        }

        private static bool RequiresStandableCell(RoomContentSlotKind kind)
        {
            return kind == RoomContentSlotKind.SafeCell
                || kind == RoomContentSlotKind.OptionalReward
                || kind == RoomContentSlotKind.Treasure
                || kind == RoomContentSlotKind.Shop
                || kind == RoomContentSlotKind.Supply
                || kind == RoomContentSlotKind.RecordGuest
                || kind == RoomContentSlotKind.MaruStatue
                || kind == RoomContentSlotKind.MaruEntry
                || kind == RoomContentSlotKind.Npc
                || kind == RoomContentSlotKind.LocalEvent;
        }

        private static void FillHorizontal(
            Tilemap tilemap,
            TileBase tile,
            int xMin,
            int xMax,
            int y)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        private static void FillVertical(
            Tilemap tilemap,
            TileBase tile,
            int x,
            int yMin,
            int yMax)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
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
            GameObject visualObject = new GameObject(name);
            visualObject.transform.SetParent(parent);
            visualObject.transform.localPosition =
                new Vector3(localPosition.x, localPosition.y, 0f);
            visualObject.transform.localRotation =
                Quaternion.Euler(0f, 0f, rotation);
            visualObject.transform.localScale =
                CalculateFitScale(sprite, size);
            SpriteRenderer renderer =
                visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
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
            bool normalize =
                colliderType != Tile.ColliderType.None && sprite != null;
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = assetName;
            tile.sprite = sprite;
            tile.color = color;
            tile.colliderType = colliderType;
            tile.transform = Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.identity,
                normalize
                    ? new Vector3(size.x, size.y, 1f)
                    : CalculateFitScale(sprite, size));
            AssetDatabase.CreateAsset(tile, path);
            if (normalize)
            {
                Sprite unitSprite =
                    CreateUnitSprite(sprite, assetName + "_Unit");
                AssetDatabase.AddObjectToAsset(unitSprite, tile);
                tile.sprite = unitSprite;
                EditorUtility.SetDirty(tile);
            }

            return tile;
        }

        // Grid colliders inherit tile.transform, so the sprite is rebuilt at
        // one world unit and the scale carries the fitted size instead.
        private static Sprite CreateUnitSprite(Sprite source, string name)
        {
            Rect rect = source.rect;
            Vector2 pivot = new Vector2(
                rect.width > 0.001f ? source.pivot.x / rect.width : 0.5f,
                rect.height > 0.001f ? source.pivot.y / rect.height : 0.5f);
            Sprite unitSprite = Sprite.Create(
                source.texture,
                rect,
                pivot,
                rect.width,
                0,
                SpriteMeshType.FullRect);
            unitSprite.name = name;
            return unitSprite;
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

            Vector2 spriteSize = sprite.bounds.size;
            return new Vector3(
                spriteSize.x > 0.001f
                    ? size.x / spriteSize.x
                    : 1f,
                spriteSize.y > 0.001f
                    ? size.y / spriteSize.y
                    : 1f,
                1f);
        }

        private static Color GetRoomTint(P4RoomBuildSpec spec)
        {
            if ((spec.Roles & RoomRole.ShopCorridor) != 0)
            {
                return new Color(0.24f, 0.14f, 0.34f, 0.94f);
            }

            if ((spec.Roles & RoomRole.SupplyCorridor) != 0)
            {
                return new Color(0.09f, 0.24f, 0.32f, 0.94f);
            }

            if ((spec.Roles & RoomRole.RecordRoom) != 0)
            {
                return new Color(0.13f, 0.17f, 0.34f, 0.96f);
            }

            if ((spec.Roles & RoomRole.MaruStatue) != 0)
            {
                return new Color(0.19f, 0.16f, 0.27f, 0.98f);
            }

            switch (spec.Archetype)
            {
                case RoomArchetype.Small:
                    return new Color(0.07f, 0.13f, 0.25f, 0.92f);
                case RoomArchetype.Standard:
                    return new Color(0.08f, 0.16f, 0.29f, 0.92f);
                case RoomArchetype.Wide:
                    return new Color(0.10f, 0.14f, 0.30f, 0.93f);
                case RoomArchetype.Tall:
                    return new Color(0.08f, 0.18f, 0.25f, 0.93f);
                case RoomArchetype.Large:
                    return new Color(0.14f, 0.13f, 0.29f, 0.94f);
                default:
                    return new Color(0.08f, 0.14f, 0.25f, 0.92f);
            }
        }

        private static string GetPrefabFolder(P4RoomBuildSpec spec)
        {
            bool special =
                (spec.Roles & RoomRole.ShopCorridor) != 0
                || (spec.Roles & RoomRole.SupplyCorridor) != 0
                || (spec.Roles & RoomRole.RecordRoom) != 0
                || (spec.Roles & RoomRole.MaruStatue) != 0;
            if (special)
            {
                return PrefabRoot + "/Special";
            }

            return PrefabRoot + "/" + spec.Archetype;
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path)
                || path == "Assets"
                || AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string normalized = path.Replace('\\', '/');
            string parent = normalized.Substring(
                0,
                normalized.LastIndexOf('/'));
            string name = normalized.Substring(
                normalized.LastIndexOf('/') + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private sealed class SourceArt
        {
            public Sprite Ground;
            public Sprite GroundSide;
            public Sprite Sky;
            public Sprite MountainA;
            public Sprite MountainB;
            public Sprite Cloud;
            public Sprite MoonA;
            public Sprite MoonB;
            public Sprite Square;
            public Sprite Anchor;
            public Sprite BaseElement;
            public Sprite Tree;
            public Sprite Rock;
            public Sprite Item;
            public Sprite Panel;
            public Sprite Star;
            public Sprite Crystal;
            public Sprite Spike;

            public bool IsComplete =>
                Ground != null
                && GroundSide != null
                && Sky != null
                && MountainA != null
                && MountainB != null
                && Cloud != null
                && MoonA != null
                && MoonB != null
                && Square != null
                && Anchor != null
                && BaseElement != null
                && Tree != null
                && Rock != null
                && Item != null
                && Panel != null
                && Star != null
                && Crystal != null
                && Spike != null;
        }

        private sealed class GeneratedTiles
        {
            public Tile Reinforced;
            public Tile SoftSoil;
            public Tile OneWay;
            public Tile Fixture;
            public Tile Hazard;
            public Tile Decoration;
            public Tile Logic;
        }
    }
}

#endif
