using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.RunGeneration
{
    public sealed class MoonPalaceRunPreviewPublication
    {
        internal MoonPalaceRunPreviewPublication(MoonPalaceRunPreviewConfig config, MoonPalaceRunPreviewGrid grid, MoonPalaceRunPreviewValidation validation,
            IDictionary<string, string> outputContents, string sceneManifestDigest, string digestManifestDigest)
        {
            Config = config;
            Grid = grid;
            Validation = validation;
            OutputContents = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(outputContents, StringComparer.Ordinal));
            SceneManifestDigest = sceneManifestDigest ?? string.Empty;
            DigestManifestDigest = digestManifestDigest ?? string.Empty;
        }

        public MoonPalaceRunPreviewConfig Config { get; }
        public MoonPalaceRunPreviewGrid Grid { get; }
        public MoonPalaceRunPreviewValidation Validation { get; }
        public IReadOnlyDictionary<string, string> OutputContents { get; }
        public string SceneManifestDigest { get; }
        public string DigestManifestDigest { get; }
    }

    /// <summary>RUN04-only publisher. It generates one manually playable preview Scene and never edits RUN03, Prefabs, or Build Settings.</summary>
    public static class MoonPalaceRunPreviewSceneBuilder
    {
        public const string SceneRelativePath = "Assets/_Game/Map/Scenes/MoonPalace/RUN04/MoonPalacePlayableRunPreview_RUN04.unity";
        public const string SceneDirectoryRelativePath = "Assets/_Game/Map/Scenes/MoonPalace/RUN04";
        public const string AuthoringDirectoryRelativePath = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN04";
        public const string GeneratedDirectoryRelativePath = "MapDesign/MCP/GENERATED/RUN04";
        public const string SceneRootName = "MoonPalace_PlayableRunPreview_RUN04";

        private static readonly string[] CsvNames =
        {
            "moonpalace_run04_room_camera_frames.csv", "moonpalace_run04_connector_triggers.csv", "moonpalace_run04_route_ghost_path.csv",
            "moonpalace_run04_preview_controls.csv", "moonpalace_run04_validation_summary.csv",
        };

        private static readonly string[] JsonNames =
        {
            "moonpalace_run04_preview_config.json", "moonpalace_run04_traversal_grid_manifest.json", "moonpalace_run04_room_camera_frames.json",
            "moonpalace_run04_connector_triggers.json", "moonpalace_run04_route_ghost_path.json", "moonpalace_run04_validation.json",
            "moonpalace_run04_scene_manifest.json", "moonpalace_run04_digest_manifest.json",
        };

        private static readonly TileDefinition[] TileDefinitions =
        {
            new TileDefinition("Solid", new Color(0.055f, 0.065f, 0.085f, 1f)), new TileDefinition("Open", new Color(0.13f, 0.18f, 0.23f, 1f)),
            new TileDefinition("MainRoute", new Color(0.20f, 0.95f, 0.45f, 0.92f)), new TileDefinition("BranchSplit", new Color(0.15f, 0.67f, 1f, 0.92f)),
            new TileDefinition("RoomFrame", new Color(0.75f, 0.35f, 1f, 1f)), new TileDefinition("ConnectorGate", new Color(1f, 0.32f, 0.72f, 1f)),
            new TileDefinition("RouteGhost", new Color(1f, 0.90f, 0.25f, 0.86f)),
        };

        public static IReadOnlyList<string> GetExpectedOutputRelativePaths()
        {
            return new ReadOnlyCollection<string>(CsvNames.Select(name => Join(AuthoringDirectoryRelativePath, name)).Concat(JsonNames.Select(name => Join(GeneratedDirectoryRelativePath, name))).OrderBy(path => path, StringComparer.Ordinal).ToList());
        }

        public static IReadOnlyList<string> GetDebugTileAssetRelativePaths()
        {
            return new ReadOnlyCollection<string>(TileDefinitions.Select(definition => DebugTilePath(definition.Name)).OrderBy(path => path, StringComparer.Ordinal).ToList());
        }

        public static MoonPalaceRunPreviewPublication BuildSnapshot(string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot)) throw new ArgumentException("RUN04 requires a project root for deterministic publication.", nameof(projectRoot));
            var config = MoonPalaceRunPreviewConfig.CreateDefault();
            var grid = MoonPalaceRunPreviewGrid.Create(config);
            var validation = MoonPalaceRunPreviewValidator.Validate(grid);
            if (!validation.Passed) throw new InvalidOperationException("RUN04 refuses to publish a preview that failed its derived-grid validation.");
            var sceneDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "RUN04_SCENE", config.CanonicalDigest, grid.CanonicalDigest, validation.CanonicalDigest, SceneRelativePath, SceneRootName });
            var outputs = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                [Join(AuthoringDirectoryRelativePath, CsvNames[0])] = RoomFramesCsv(grid),
                [Join(AuthoringDirectoryRelativePath, CsvNames[1])] = ConnectorTriggersCsv(grid),
                [Join(AuthoringDirectoryRelativePath, CsvNames[2])] = RouteGhostCsv(grid),
                [Join(AuthoringDirectoryRelativePath, CsvNames[3])] = ControlsCsv(),
                [Join(AuthoringDirectoryRelativePath, CsvNames[4])] = ValidationCsv(validation),
                [Join(GeneratedDirectoryRelativePath, JsonNames[0])] = ConfigJson(config),
                [Join(GeneratedDirectoryRelativePath, JsonNames[1])] = TraversalGridJson(grid),
                [Join(GeneratedDirectoryRelativePath, JsonNames[2])] = RoomFramesJson(grid),
                [Join(GeneratedDirectoryRelativePath, JsonNames[3])] = ConnectorTriggersJson(grid),
                [Join(GeneratedDirectoryRelativePath, JsonNames[4])] = RouteGhostJson(grid),
                [Join(GeneratedDirectoryRelativePath, JsonNames[5])] = ValidationJson(validation),
                [Join(GeneratedDirectoryRelativePath, JsonNames[6])] = SceneManifestJson(grid, validation, sceneDigest),
            };
            var digest = BakingCanonicalDigest.HashCanonicalLines(outputs.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => item.Key + "|" + BakingCanonicalDigest.HashCanonicalText(item.Value)));
            outputs.Add(Join(GeneratedDirectoryRelativePath, JsonNames[7]), DigestManifestJson(outputs, sceneDigest, digest));
            return new MoonPalaceRunPreviewPublication(config, grid, validation, outputs, sceneDigest, digest);
        }

        public static MoonPalaceRunPreviewPublication Publish(string projectRoot)
        {
            var publication = BuildSnapshot(projectRoot);
            Directory.CreateDirectory(Resolve(projectRoot, SceneDirectoryRelativePath));
            Directory.CreateDirectory(Resolve(projectRoot, AuthoringDirectoryRelativePath));
            Directory.CreateDirectory(Resolve(projectRoot, GeneratedDirectoryRelativePath));
            foreach (var output in publication.OutputContents) File.WriteAllText(Resolve(projectRoot, output.Key), FinalLf(output.Value), new UTF8Encoding(false));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CreateScene(publication);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return publication;
        }

        private static void CreateScene(MoonPalaceRunPreviewPublication publication)
        {
            var tiles = CreateDebugTiles();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(SceneRootName, typeof(Grid));
            var terrain = CreateTilemap("Terrain", root.transform, 0);
            var main = CreateTilemap("MainRouteOverlay", root.transform, 2);
            var branch = CreateTilemap("BranchSplitOverlay", root.transform, 3);
            var frames = CreateTilemap("RoomFrameOverlay", root.transform, 4);
            var gates = CreateTilemap("ConnectorGateOverlay", root.transform, 5);
            var ghostMap = CreateTilemap("RouteGhost", root.transform, 1);
            for (var y = 0; y < publication.Grid.TileHeight; y++)
            for (var x = 0; x < publication.Grid.TileWidth; x++)
            {
                var position = new Vector3Int(x, y, 0);
                terrain.SetTile(position, publication.Grid.IsOpen(x, y) ? tiles["Open"] : tiles["Solid"]);
                var ownership = publication.Grid.GetRouteOwnership(x, y);
                if (ownership == "MAIN") main.SetTile(position, tiles["MainRoute"]);
                else if (ownership == "BRANCH" || ownership == "SPLIT_REJOIN") branch.SetTile(position, tiles["BranchSplit"]);
            }
            foreach (var frame in publication.Grid.RoomFrames) PaintFrame(frames, frame, tiles["RoomFrame"]);
            foreach (var trigger in publication.Grid.ConnectorTriggers) { gates.SetTile(new Vector3Int(trigger.FromGateTile.x, trigger.FromGateTile.y, 0), tiles["ConnectorGate"]); gates.SetTile(new Vector3Int(trigger.ToGateTile.x, trigger.ToGateTile.y, 0), tiles["ConnectorGate"]); }
            foreach (var tile in publication.Grid.FindRouteToExit()) ghostMap.SetTile(new Vector3Int(tile.x, tile.y, 0), tiles["RouteGhost"]);

            var routeGhost = ghostMap.gameObject.AddComponent<MoonPalaceRunPreviewRouteGhost>();
            routeGhost.Initialize(publication.Grid, publication.Config.AutoRouteGhostEnabled);
            var cameraObject = new GameObject("PreviewCamera", typeof(Camera), typeof(MoonPalaceRunPreviewCameraController));
            cameraObject.transform.SetParent(root.transform, false);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.033f, 0.050f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            var cameraController = cameraObject.GetComponent<MoonPalaceRunPreviewCameraController>();
            cameraController.Configure(camera, true, 0.18f);
            cameraController.Initialize(publication.Grid, publication.Grid.GetRoomId(publication.Grid.StartTile.x, publication.Grid.StartTile.y));

            var labels = new GameObject("Labels");
            labels.transform.SetParent(root.transform, false);
            var previewInfo = CreateText("PreviewInfo", labels.transform, new Vector3(2f, 93f, -3f), "preview=" + publication.Config.PreviewId + " source=" + publication.Config.SourceCourseId + " seed=" + publication.Config.SeedId + "/" + publication.Config.SeedValue.ToString(CultureInfo.InvariantCulture) + "\ntile grid 320x96 | rooms 10 | connectors 9", new Color(0.87f, 0.91f, 1f, 1f), 0.30f, TextAnchor.UpperLeft);
            var roomLabel = CreateText("CurrentRoomLabel", labels.transform, new Vector3(2f, 87f, -3f), "Current room: R01_START", Color.white, 0.34f, TextAnchor.UpperLeft);
            var tileLabel = CreateText("CurrentTileLabel", labels.transform, new Vector3(2f, 84f, -3f), "Tile: " + publication.Grid.StartTile.x + "," + publication.Grid.StartTile.y + "  steps=0 blocked=0", Color.white, 0.28f, TextAnchor.UpperLeft);
            CreateText("ControlsLabel", labels.transform, new Vector3(2f, 81f, -3f), "WASD / Arrow move | R reset | G route ghost", new Color(1f, 0.90f, 0.38f, 1f), 0.28f, TextAnchor.UpperLeft);
            foreach (var frame in publication.Grid.RoomFrames) CreateText("Room_" + frame.RoomId, labels.transform, new Vector3(frame.Center.x, frame.TileMinY + frame.TileHeight + 0.8f, -3f), frame.RoomId, new Color(0.87f, 0.55f, 1f, 1f), 0.24f, TextAnchor.MiddleCenter);
            foreach (var trigger in publication.Grid.ConnectorTriggers) CreateText("Gate_" + trigger.ConnectorId, labels.transform, new Vector3(trigger.FromGateTile.x + 0.5f, trigger.FromGateTile.y + 1.1f, -3f), trigger.ConnectorId + " " + trigger.FromRoomId + "→" + trigger.ToRoomId, new Color(1f, 0.58f, 0.85f, 1f), 0.18f, TextAnchor.LowerCenter);

            var player = new GameObject("PreviewPlayer", typeof(MoonPalaceRunPreviewController));
            player.transform.SetParent(root.transform, false);
            CreateText("PreviewPlayerMarker", player.transform, Vector3.zero, "●", new Color(0.5f, 1f, 0.52f, 1f), 0.80f, TextAnchor.MiddleCenter);
            var playerController = player.GetComponent<MoonPalaceRunPreviewController>();
            playerController.ConfigureVisuals(tileLabel, roomLabel, cameraController, routeGhost);
            playerController.Initialize(publication.Grid);

            var metadata = new GameObject("Metadata");
            metadata.transform.SetParent(root.transform, false);
            CreateText("MetadataText", metadata.transform, new Vector3(2f, 76f, -3f), "RUN04 derived preview only; no production player physics, no Build Settings entry.\nsource composition=" + publication.Grid.Composition.CompositionDigest + "\npreview grid=" + publication.Grid.CanonicalDigest + "\nvalidation=" + publication.Validation.CanonicalDigest, new Color(0.62f, 0.72f, 0.88f, 1f), 0.20f, TextAnchor.UpperLeft);
            EditorSceneManager.SaveScene(scene, SceneRelativePath);
        }

        private static Dictionary<string, Tile> CreateDebugTiles()
        {
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (sprite == null) throw new InvalidOperationException("RUN04 requires Unity's built-in UI sprite for isolated debug tiles.");
            var tiles = new Dictionary<string, Tile>(StringComparer.Ordinal);
            foreach (var definition in TileDefinitions)
            {
                var path = DebugTilePath(definition.Name);
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
                tile.name = "RUN04_" + definition.Name;
                tile.sprite = sprite;
                tile.color = definition.Color;
                tile.colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                tiles.Add(definition.Name, tile);
            }
            AssetDatabase.SaveAssets();
            return tiles;
        }

        private static Tilemap CreateTilemap(string name, Transform parent, int sortingOrder)
        {
            var gameObject = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            gameObject.transform.SetParent(parent, false);
            var renderer = gameObject.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.mode = TilemapRenderer.Mode.Chunk;
            return gameObject.GetComponent<Tilemap>();
        }

        private static TextMesh CreateText(string name, Transform parent, Vector3 localPosition, string value, Color color, float characterSize, TextAnchor anchor)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            var text = gameObject.AddComponent<TextMesh>();
            text.text = value;
            text.color = color;
            text.characterSize = characterSize;
            text.fontSize = 28;
            text.anchor = anchor;
            return text;
        }

        private static void PaintFrame(Tilemap map, MoonPalaceRunPreviewRoomFrame frame, Tile tile)
        {
            var maxX = frame.TileMinX + frame.TileWidth - 1;
            var maxY = frame.TileMinY + frame.TileHeight - 1;
            for (var x = frame.TileMinX; x <= maxX; x++) { map.SetTile(new Vector3Int(x, frame.TileMinY, 0), tile); map.SetTile(new Vector3Int(x, maxY, 0), tile); }
            for (var y = frame.TileMinY; y <= maxY; y++) { map.SetTile(new Vector3Int(frame.TileMinX, y, 0), tile); map.SetTile(new Vector3Int(maxX, y, 0), tile); }
        }

        private static string RoomFramesCsv(MoonPalaceRunPreviewGrid grid)
        {
            var lines = new List<string> { "room_id,role,tile_min_x,tile_min_y,tile_width,tile_height,center_x,center_y,orthographic_size" };
            lines.AddRange(grid.RoomFrames.OrderBy(frame => frame.RoomId, StringComparer.Ordinal).Select(frame => string.Join(",", frame.RoomId, frame.Role, I(frame.TileMinX), I(frame.TileMinY), I(frame.TileWidth), I(frame.TileHeight), F(frame.Center.x), F(frame.Center.y), F(frame.OrthographicSize))));
            return Lines(lines);
        }

        private static string ConnectorTriggersCsv(MoonPalaceRunPreviewGrid grid)
        {
            var lines = new List<string> { "connector_id,from_room_id,to_room_id,from_gate_tile,to_gate_tile,direction,kind,required_socket_bit,is_reciprocal,is_open" };
            lines.AddRange(grid.ConnectorTriggers.OrderBy(trigger => trigger.ConnectorId, StringComparer.Ordinal).Select(trigger => string.Join(",", trigger.ConnectorId, trigger.FromRoomId, trigger.ToRoomId, Csv(trigger.FromGateTile.x + ":" + trigger.FromGateTile.y), Csv(trigger.ToGateTile.x + ":" + trigger.ToGateTile.y), trigger.Direction, trigger.Kind, I(trigger.RequiredSocketBit), B(trigger.IsReciprocal), B(trigger.IsOpen))));
            return Lines(lines);
        }

        private static string RouteGhostCsv(MoonPalaceRunPreviewGrid grid)
        {
            var lines = new List<string> { "step,tile_x,tile_y,room_id,route_ownership,is_start,is_exit" };
            lines.AddRange(grid.FindRouteToExit().Select((tile, index) => string.Join(",", I(index), I(tile.x), I(tile.y), grid.GetRoomId(tile.x, tile.y), grid.GetRouteOwnership(tile.x, tile.y), B(tile.Equals(grid.StartTile)), B(tile.Equals(grid.ExitTile)))));
            return Lines(lines);
        }

        private static string ControlsCsv()
        {
            return Lines(new[] { "input,behavior", "W / UpArrow,attempt cardinal north open-tile step", "A / LeftArrow,attempt cardinal west open-tile step", "S / DownArrow,attempt cardinal south open-tile step", "D / RightArrow,attempt cardinal east open-tile step", "R,reset marker to RUN03 start tile", "G,toggle BFS route ghost" });
        }

        private static string ValidationCsv(MoonPalaceRunPreviewValidation validation)
        {
            return Lines(new[] { "preview_id,tile_grid,rooms,connectors,start_open,exit_open,route_exists,frames_inside,connectors_reciprocal,gate_tiles_open,destinations_resolvable,transitions_reachable,fallback_carve,silent_repair,validation_digest", string.Join(",", validation.Grid.Config.PreviewId, validation.Grid.TileWidth + "x" + validation.Grid.TileHeight, I(validation.Grid.RoomFrames.Count), I(validation.Grid.ConnectorTriggers.Count), B(validation.StartOpen), B(validation.ExitOpen), B(validation.RouteExists), B(validation.AllRoomFramesInside), B(validation.AllConnectorsReciprocal), B(validation.AllConnectorGateTilesOpen), B(validation.AllConnectorDestinationRoomsResolvable), B(validation.AllConnectorTransitionsReachable), I(validation.FallbackCarveCount), I(validation.SilentRepairCount), validation.CanonicalDigest) });
        }

        private static string ConfigJson(MoonPalaceRunPreviewConfig config) => Json(new[] { Pair("preview_id", config.PreviewId), Pair("source_course_id", config.SourceCourseId), Pair("source_task", config.SourceTaskId), Pair("seed_id", config.SeedId), Number("seed_value", config.SeedValue), Pair("pattern_size", config.PatternWidth + "x" + config.PatternHeight), Pair("pattern_grid", config.PatternGridWidth + "x" + config.PatternGridHeight), Pair("tile_grid", config.TileWidth + "x" + config.TileHeight), Number("expected_room_count", config.ExpectedRoomCount), Number("expected_connector_count", config.ExpectedConnectorCount), Pair("movement_mode", config.MovementMode), Pair("camera_mode", config.CameraMode), Bool("auto_route_ghost", config.AutoRouteGhostEnabled), Pair("canonical_digest", config.CanonicalDigest) });
        private static string TraversalGridJson(MoonPalaceRunPreviewGrid grid) => Json(new[] { Pair("source_composition_digest", grid.Composition.CompositionDigest), Pair("traversal_grid_digest", grid.CanonicalDigest), Pair("tile_grid", grid.TileWidth + "x" + grid.TileHeight), Number("cell_count", grid.CellCount), Number("open_cell_count", grid.OpenCellCount), Pair("start_tile", grid.StartTile.x + ":" + grid.StartTile.y), Pair("exit_tile", grid.ExitTile.x + ":" + grid.ExitTile.y), Number("route_length", grid.FindRouteToExit().Count), Bool("sector_primary_input", grid.Config.AllowSectorPrimaryInput), Number("fallback_carve_count", grid.Composition.FallbackCarveCount), Number("silent_repair_count", grid.Composition.SilentRepairCount) });
        private static string RoomFramesJson(MoonPalaceRunPreviewGrid grid) => JsonArray("room_frames", grid.RoomFrames.OrderBy(frame => frame.RoomId, StringComparer.Ordinal).Select(frame => "{\"room_id\":\"" + Escape(frame.RoomId) + "\",\"role\":\"" + frame.Role + "\",\"tile_bounds\":\"" + frame.TileMinX + ":" + frame.TileMinY + ":" + frame.TileWidth + "x" + frame.TileHeight + "\",\"center\":\"" + F(frame.Center.x) + ":" + F(frame.Center.y) + "\",\"orthographic_size\":" + F(frame.OrthographicSize) + "}"));
        private static string ConnectorTriggersJson(MoonPalaceRunPreviewGrid grid) => JsonArray("connector_triggers", grid.ConnectorTriggers.OrderBy(trigger => trigger.ConnectorId, StringComparer.Ordinal).Select(trigger => "{\"connector_id\":\"" + Escape(trigger.ConnectorId) + "\",\"from_room_id\":\"" + Escape(trigger.FromRoomId) + "\",\"to_room_id\":\"" + Escape(trigger.ToRoomId) + "\",\"from_gate_tile\":\"" + trigger.FromGateTile.x + ":" + trigger.FromGateTile.y + "\",\"to_gate_tile\":\"" + trigger.ToGateTile.x + ":" + trigger.ToGateTile.y + "\",\"direction\":\"" + trigger.Direction + "\",\"kind\":\"" + Escape(trigger.Kind) + "\",\"required_socket_bit\":" + trigger.RequiredSocketBit + ",\"is_reciprocal\":" + (trigger.IsReciprocal ? "true" : "false") + ",\"is_open\":" + (trigger.IsOpen ? "true" : "false") + "}"));
        private static string RouteGhostJson(MoonPalaceRunPreviewGrid grid) => JsonArray("route", grid.FindRouteToExit().Select((tile, index) => "{\"step\":" + index + ",\"tile\":\"" + tile.x + ":" + tile.y + "\",\"room_id\":\"" + Escape(grid.GetRoomId(tile.x, tile.y)) + "\"}"));
        private static string ValidationJson(MoonPalaceRunPreviewValidation validation) => Json(new[] { Pair("validation_digest", validation.CanonicalDigest), Bool("passed", validation.Passed), Bool("start_open", validation.StartOpen), Bool("exit_open", validation.ExitOpen), Bool("route_exists", validation.RouteExists), Bool("all_room_frames_inside", validation.AllRoomFramesInside), Bool("all_connectors_reciprocal", validation.AllConnectorsReciprocal), Bool("all_connector_gate_tiles_open", validation.AllConnectorGateTilesOpen), Bool("all_destination_rooms_resolvable", validation.AllConnectorDestinationRoomsResolvable), Bool("all_connector_transitions_reachable", validation.AllConnectorTransitionsReachable), Bool("sector_primary_input", validation.UsesSectorPrimaryInput), Number("fallback_carve_count", validation.FallbackCarveCount), Number("silent_repair_count", validation.SilentRepairCount) });
        private static string SceneManifestJson(MoonPalaceRunPreviewGrid grid, MoonPalaceRunPreviewValidation validation, string sceneDigest) => Json(new[] { Pair("scene_relative_path", SceneRelativePath), Pair("scene_root", SceneRootName), Pair("scene_manifest_digest", sceneDigest), Pair("preview_id", grid.Config.PreviewId), Pair("source_course_id", grid.Config.SourceCourseId), Number("room_count", grid.RoomFrames.Count), Number("connector_count", grid.ConnectorTriggers.Count), Number("route_ghost_tiles", grid.FindRouteToExit().Count), Pair("required_children", "Terrain;MainRouteOverlay;BranchSplitOverlay;RoomFrameOverlay;ConnectorGateOverlay;PreviewPlayer;RouteGhost;PreviewCamera;Labels;Metadata"), Pair("validation_digest", validation.CanonicalDigest) });
        private static string DigestManifestJson(IDictionary<string, string> outputs, string sceneDigest, string digest) => JsonArrayWithFields(new[] { Pair("scene_manifest_digest", sceneDigest), Pair("digest_manifest_digest", digest) }, "artifacts", outputs.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => "{\"relative_path\":\"" + Escape(item.Key) + "\",\"sha256\":\"" + BakingCanonicalDigest.HashCanonicalText(item.Value) + "\"}"));

        private static string Json(IEnumerable<string> fields)
        {
            var values = fields.ToList();
            var lines = new List<string> { "{" };
            lines.AddRange(values.Select((value, index) => "  " + value + (index == values.Count - 1 ? string.Empty : ",")));
            lines.Add("}");
            return Lines(lines);
        }

        private static string JsonArray(string name, IEnumerable<string> entries) => JsonArrayWithFields(Array.Empty<string>(), name, entries);
        private static string JsonArrayWithFields(IEnumerable<string> fields, string name, IEnumerable<string> entries)
        {
            var prefix = fields.ToList();
            var values = entries.ToList();
            var lines = new List<string> { "{" };
            lines.AddRange(prefix.Select(value => "  " + value + ","));
            lines.Add("  \"" + Escape(name) + "\": [");
            lines.AddRange(values.Select((value, index) => "    " + value + (index == values.Count - 1 ? string.Empty : ",")));
            lines.Add("  ]");
            lines.Add("}");
            return Lines(lines);
        }

        private static string Pair(string name, string value) => "\"" + Escape(name) + "\": \"" + Escape(value) + "\"";
        private static string Number(string name, int value) => "\"" + Escape(name) + "\": " + I(value);
        private static string Bool(string name, bool value) => "\"" + Escape(name) + "\": " + (value ? "true" : "false");
        private static string Lines(IEnumerable<string> lines) => string.Join("\n", lines) + "\n";
        private static string FinalLf(string value) => BakingCanonicalDigest.NormalizeLineEndingsToLf(value ?? string.Empty).TrimEnd('\n') + "\n";
        private static string Csv(string value) { value = value ?? string.Empty; return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value; }
        private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
        private static string B(bool value) => value ? "TRUE" : "FALSE";
        private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        private static string Join(string left, string right) => left.TrimEnd('/') + "/" + right.TrimStart('/');
        private static string Resolve(string root, string relative) => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        private static string DebugTilePath(string name) => Join(SceneDirectoryRelativePath, "RUN04_" + name + ".asset");

        private sealed class TileDefinition
        {
            public TileDefinition(string name, Color color) { Name = name; Color = color; }
            public string Name { get; }
            public Color Color { get; }
        }
    }
}
