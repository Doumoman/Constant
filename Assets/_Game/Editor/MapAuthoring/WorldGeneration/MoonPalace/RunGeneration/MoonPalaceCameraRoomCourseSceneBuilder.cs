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
    public sealed class MoonPalaceCameraRoomCoursePublication
    {
        internal MoonPalaceCameraRoomCoursePublication(MoonPalaceCameraRoomCourseConfig config, MoonPalaceCameraRoomGraph graph,
            MoonPalaceCameraRoomConnectorPlan connectors, MoonPalaceCameraRoomPatternComposition composition,
            MoonPalaceCameraRoomCourseValidation validation, IDictionary<string, string> outputs, string sceneManifestDigest, string digestManifestDigest)
        {
            Config = config; Graph = graph; Connectors = connectors; Composition = composition; Validation = validation;
            OutputContents = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(outputs, StringComparer.Ordinal));
            SceneManifestDigest = sceneManifestDigest ?? string.Empty; DigestManifestDigest = digestManifestDigest ?? string.Empty;
        }
        public MoonPalaceCameraRoomCourseConfig Config { get; }
        public MoonPalaceCameraRoomGraph Graph { get; }
        public MoonPalaceCameraRoomConnectorPlan Connectors { get; }
        public MoonPalaceCameraRoomPatternComposition Composition { get; }
        public MoonPalaceCameraRoomCourseValidation Validation { get; }
        public IReadOnlyDictionary<string, string> OutputContents { get; }
        public string SceneManifestDigest { get; }
        public string DigestManifestDigest { get; }
    }

    /// <summary>RUN03-only publisher for a room-frame MicroPattern Scene. It owns no existing Scene, Prefab, or Build Settings entry.</summary>
    public static class MoonPalaceCameraRoomCourseSceneBuilder
    {
        public const string SceneRelativePath = "Assets/_Game/Map/Scenes/MoonPalace/RUN03/MoonPalaceCameraRoomRun_RUN03.unity";
        public const string SceneDirectoryRelativePath = "Assets/_Game/Map/Scenes/MoonPalace/RUN03";
        public const string AuthoringDirectoryRelativePath = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN03";
        public const string GeneratedDirectoryRelativePath = "MapDesign/MCP/GENERATED/RUN03";
        public const string SceneRootName = "MoonPalace_CameraRoomRun_RUN03";

        private static readonly string[] CsvNames =
        {
            "moonpalace_run03_rooms.csv", "moonpalace_run03_connectors.csv", "moonpalace_run03_pattern_slots.csv",
            "moonpalace_run03_tile_cells.csv", "moonpalace_run03_route_paths.csv", "moonpalace_run03_validation_summary.csv",
        };
        private static readonly string[] JsonNames =
        {
            "moonpalace_run03_course_config.json", "moonpalace_run03_room_graph.json", "moonpalace_run03_connectors.json",
            "moonpalace_run03_composition.json", "moonpalace_run03_validation.json", "moonpalace_run03_scene_manifest.json", "moonpalace_run03_digest_manifest.json",
        };
        private static readonly TileDefinition[] TileDefinitions =
        {
            new TileDefinition("Solid", new Color(0.075f, 0.087f, 0.115f, 1f)), new TileDefinition("Open", new Color(0.16f, 0.21f, 0.26f, 1f)),
            new TileDefinition("MainRoute", new Color(0.26f, 0.92f, 0.46f, 1f)), new TileDefinition("BranchSplit", new Color(0.18f, 0.72f, 1f, 1f)),
            new TileDefinition("SplitRoute", new Color(1f, 0.73f, 0.20f, 1f)), new TileDefinition("RoomFrame", new Color(0.78f, 0.42f, 1f, 0.95f)),
            new TileDefinition("ConnectorGate", new Color(1f, 0.42f, 0.82f, 1f)), new TileDefinition("PatternGrid", new Color(0.36f, 0.42f, 0.58f, 0.55f)),
            new TileDefinition("Start", new Color(0.48f, 1f, 0.48f, 1f)), new TileDefinition("Exit", new Color(1f, 0.38f, 0.42f, 1f)),
        };

        [MenuItem("MapDesign/MoonPalace/Build RUN03 Camera Room MicroPattern Course Scene")]
        public static void PublishFromMenu() { Publish(ProjectRoot); }

        public static IReadOnlyList<string> GetExpectedOutputRelativePaths()
        {
            return new ReadOnlyCollection<string>(CsvNames.Select(name => Join(AuthoringDirectoryRelativePath, name)).Concat(JsonNames.Select(name => Join(GeneratedDirectoryRelativePath, name)))
                .Concat(new[] { SceneRelativePath }).OrderBy(path => path, StringComparer.Ordinal).ToList());
        }
        public static IReadOnlyList<string> GetDebugTileAssetRelativePaths()
        {
            return new ReadOnlyCollection<string>(TileDefinitions.Select(definition => DebugTilePath(definition.Name)).OrderBy(path => path, StringComparer.Ordinal).ToList());
        }

        public static MoonPalaceCameraRoomCoursePublication BuildSnapshot(string projectRoot, bool reverseCandidateEnumeration)
        {
            if (string.IsNullOrWhiteSpace(projectRoot)) throw new ArgumentException("Project root is required.", nameof(projectRoot));
            var config = MoonPalaceCameraRoomCourseConfig.CreateDefault();
            var graph = MoonPalaceCameraRoomGraph.Create(config);
            var connectors = MoonPalaceCameraRoomConnectorPlanner.Plan(graph);
            var composition = MoonPalaceCameraRoomPatternComposer.Compose(config, graph, connectors, reverseCandidateEnumeration);
            var validation = MoonPalaceCameraRoomCourseValidator.Validate(composition);
            if (!validation.Passed) throw new InvalidOperationException("RUN03 refuses to publish a blocked camera-room MicroPattern course.");
            var sceneDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "RUN03_SCENE_MANIFEST", SceneRelativePath, SceneRootName, config.CanonicalDigest, graph.Digest.Value, connectors.CanonicalDigest, composition.CompositionDigest, validation.CanonicalDigest }
                .Concat(GetDebugTileAssetRelativePaths()));
            var outputs = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                [Join(AuthoringDirectoryRelativePath, CsvNames[0])] = RoomsCsv(graph),
                [Join(AuthoringDirectoryRelativePath, CsvNames[1])] = ConnectorsCsv(connectors),
                [Join(AuthoringDirectoryRelativePath, CsvNames[2])] = PatternSlotsCsv(composition),
                [Join(AuthoringDirectoryRelativePath, CsvNames[3])] = TileCellsCsv(composition),
                [Join(AuthoringDirectoryRelativePath, CsvNames[4])] = RoutePathsCsv(graph),
                [Join(AuthoringDirectoryRelativePath, CsvNames[5])] = ValidationSummaryCsv(validation),
                [Join(GeneratedDirectoryRelativePath, JsonNames[0])] = ConfigJson(config),
                [Join(GeneratedDirectoryRelativePath, JsonNames[1])] = GraphJson(graph),
                [Join(GeneratedDirectoryRelativePath, JsonNames[2])] = ConnectorsJson(connectors),
                [Join(GeneratedDirectoryRelativePath, JsonNames[3])] = CompositionJson(composition),
                [Join(GeneratedDirectoryRelativePath, JsonNames[4])] = ValidationJson(validation),
                [Join(GeneratedDirectoryRelativePath, JsonNames[5])] = SceneManifestJson(config, graph, connectors, composition, validation, sceneDigest),
            };
            var artifactLines = outputs.Select(item => item.Key + "|" + BakingCanonicalDigest.HashCanonicalText(item.Value)).ToArray();
            var digestManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "RUN03_DIGEST_MANIFEST", validation.CanonicalDigest, sceneDigest }.Concat(artifactLines));
            outputs[Join(GeneratedDirectoryRelativePath, JsonNames[6])] = DigestManifestJson(outputs, digestManifestDigest, sceneDigest);
            return new MoonPalaceCameraRoomCoursePublication(config, graph, connectors, composition, validation, outputs, sceneDigest, digestManifestDigest);
        }

        public static MoonPalaceCameraRoomCoursePublication Publish(string projectRoot)
        {
            var root = Path.GetFullPath(projectRoot); var publication = BuildSnapshot(root, false);
            Directory.CreateDirectory(Resolve(root, AuthoringDirectoryRelativePath)); Directory.CreateDirectory(Resolve(root, GeneratedDirectoryRelativePath)); Directory.CreateDirectory(Resolve(root, SceneDirectoryRelativePath));
            foreach (var output in publication.OutputContents) File.WriteAllText(Resolve(root, output.Key), FinalLf(output.Value), BakingCanonicalDigest.Utf8NoBomEncoding);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); BuildScene(publication); AssetDatabase.SaveAssets(); return publication;
        }

        private static void BuildScene(MoonPalaceCameraRoomCoursePublication publication)
        {
            var previous = SceneManager.GetActiveScene(); var replaceUntitled = previous.IsValid() && string.IsNullOrEmpty(previous.path);
            if (replaceUntitled && previous.isDirty) throw new InvalidOperationException("RUN03 refuses to replace a dirty untitled Scene.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, replaceUntitled ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene); var root = new GameObject(SceneRootName); var tiles = CreateDebugTiles();
                CreateCourseVisual(root.transform, publication, tiles); CreateRoomLabels(root.transform, publication.Graph); CreateConnectorLabels(root.transform, publication.Connectors);
                CreateMarkers(root.transform, publication.Composition); CreateLegend(root.transform); CreateMetadata(root.transform, publication); CreateCamera(root.transform);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, SceneRelativePath, false)) throw new IOException("Unity did not save the isolated RUN03 camera-room Scene.");
            }
            finally
            {
                if (replaceUntitled)
                {
                    if (scene.IsValid() && scene.isLoaded && !scene.isDirty) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
                else
                {
                    if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                    if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                }
            }
        }

        private static void CreateCourseVisual(Transform parent, MoonPalaceCameraRoomCoursePublication publication, IDictionary<string, Tile> tiles)
        {
            var grid = new GameObject("Grid", typeof(Grid)); grid.transform.SetParent(parent, false);
            var terrain = CreateTilemap("Terrain", grid.transform, 0); var main = CreateTilemap("MainRoute", grid.transform, 10);
            var branch = CreateTilemap("BranchSplitRejoin", grid.transform, 11); var frame = CreateTilemap("RoomFrameConnector", grid.transform, 20);
            var composition = publication.Composition; var mainTiles = new HashSet<MoonPalaceRunTileCoordinate>(composition.MainRouteTiles);
            var branchTiles = new HashSet<MoonPalaceRunTileCoordinate>(composition.BranchRouteTiles); var splitTiles = new HashSet<MoonPalaceRunTileCoordinate>(composition.SplitRejoinRouteTiles);
            for (var y = 0; y < composition.Config.TileHeight; y++)
            for (var x = 0; x < composition.Config.TileWidth; x++)
            {
                var position = new Vector3Int(x, y, 0); var tile = new MoonPalaceRunTileCoordinate(x, y);
                terrain.SetTile(position, composition.IsOpen(x, y) ? tiles["Open"] : tiles["Solid"]);
                if (mainTiles.Contains(tile) && composition.IsOpen(x, y)) main.SetTile(position, tiles["MainRoute"]);
                if (branchTiles.Contains(tile) && composition.IsOpen(x, y)) branch.SetTile(position, tiles["BranchSplit"]);
                if (splitTiles.Contains(tile) && composition.IsOpen(x, y)) branch.SetTile(position, tiles["SplitRoute"]);
                if (x % composition.Config.PatternWidth == 0 || y % composition.Config.PatternHeight == 0) frame.SetTile(position, tiles["PatternGrid"]);
            }
            foreach (var room in publication.Graph.Rooms) PaintFrame(frame, room.Bounds, tiles["RoomFrame"]);
            foreach (var gate in publication.Connectors.Gates) { PaintRect(frame, gate.FromTileGateRect, tiles["ConnectorGate"]); PaintRect(frame, gate.ToTileGateRect, tiles["ConnectorGate"]); }
            frame.SetTile(new Vector3Int(composition.StartTile.X, composition.StartTile.Y, 0), tiles["Start"]); frame.SetTile(new Vector3Int(composition.ExitTile.X, composition.ExitTile.Y, 0), tiles["Exit"]);
            foreach (var tilemap in new[] { terrain, main, branch, frame }) { tilemap.CompressBounds(); tilemap.RefreshAllTiles(); EditorUtility.SetDirty(tilemap); }
            if (terrain.cellBounds.size.x != composition.Config.TileWidth || terrain.cellBounds.size.y != composition.Config.TileHeight) throw new InvalidOperationException("RUN03 Terrain layer did not retain the full 320x96 tile course.");
        }

        private static void CreateRoomLabels(Transform parent, MoonPalaceCameraRoomGraph graph)
        {
            var labels = new GameObject("CameraRoomLabels"); labels.transform.SetParent(parent, false);
            foreach (var room in graph.Rooms)
            {
                var label = new GameObject("Room_" + room.RoomId); label.transform.SetParent(labels.transform, false);
                label.transform.localPosition = new Vector3(room.Bounds.TileMinX + room.Bounds.TileWidth * 0.5f, room.Bounds.TileMinY + room.Bounds.TileHeight * 0.5f, -1f);
                var text = label.AddComponent<TextMesh>(); text.text = room.RoomId + "\n" + room.Role + "\n" + room.Bounds.Width.ToString(CultureInfo.InvariantCulture) + "x" + room.Bounds.Height.ToString(CultureInfo.InvariantCulture) + " patterns";
                text.color = room.Role == CameraRoomRole.Branch ? new Color(0.35f, 0.80f, 1f, 1f) : Color.white; text.characterSize = 0.32f; text.fontSize = 24; text.anchor = TextAnchor.MiddleCenter;
            }
        }
        private static void CreateConnectorLabels(Transform parent, MoonPalaceCameraRoomConnectorPlan plan)
        {
            var labels = new GameObject("ConnectorLabels"); labels.transform.SetParent(parent, false);
            foreach (var gate in plan.Gates)
            {
                var label = new GameObject("Connector_" + gate.ConnectorId); label.transform.SetParent(labels.transform, false);
                label.transform.localPosition = new Vector3((gate.FromTileGateRect.X + gate.ToTileGateRect.X) * 0.5f + 0.5f, (gate.FromTileGateRect.Y + gate.ToTileGateRect.Y) * 0.5f + 1.3f, -1f);
                var text = label.AddComponent<TextMesh>(); text.text = gate.ConnectorId + " " + gate.Direction + " " + gate.TransitionKind; text.color = new Color(1f, 0.52f, 0.88f, 1f); text.characterSize = 0.22f; text.fontSize = 18; text.anchor = TextAnchor.MiddleCenter;
            }
        }
        private static void CreateMarkers(Transform parent, MoonPalaceCameraRoomPatternComposition composition)
        {
            CreateMarker(parent, "Start", composition.StartTile, "START", new Color(0.50f, 1f, 0.50f, 1f));
            CreateMarker(parent, "Exit", composition.ExitTile, "EXIT", new Color(1f, 0.48f, 0.48f, 1f));
        }
        private static void CreateMarker(Transform parent, string name, MoonPalaceRunTileCoordinate tile, string value, Color color)
        {
            var marker = new GameObject(name); marker.transform.SetParent(parent, false); marker.transform.localPosition = new Vector3(tile.X + 0.5f, tile.Y + 1.3f, -1f);
            var text = marker.AddComponent<TextMesh>(); text.text = value; text.color = color; text.characterSize = 0.42f; text.fontSize = 30; text.anchor = TextAnchor.MiddleCenter;
        }
        private static void CreateLegend(Transform parent)
        {
            var legend = new GameObject("Legend"); legend.transform.SetParent(parent, false); legend.transform.localPosition = new Vector3(2f, 91f, -1f);
            var text = legend.AddComponent<TextMesh>(); text.text = "RUN03 4x4 MicroPattern camera-room course\nGreen main | Blue branch | Gold split/rejoin | Purple room frame | Pink reciprocal gate\nEvery fine grid cell is a real 4x4 candidate tile; labels show camera-room bounds and connector directions.";
            text.color = Color.white; text.characterSize = 0.28f; text.fontSize = 22; text.anchor = TextAnchor.UpperLeft;
        }
        private static void CreateMetadata(Transform parent, MoonPalaceCameraRoomCoursePublication publication)
        {
            var metadata = new GameObject("Metadata"); metadata.transform.SetParent(parent, false); metadata.transform.localPosition = new Vector3(2f, -5f, -1f);
            var text = metadata.AddComponent<TextMesh>(); text.text = "course_id=" + publication.Config.CourseId + "\nseed=" + publication.Config.SeedId + "/" + publication.Config.SeedValue.ToString(CultureInfo.InvariantCulture) +
                "\npattern_grid=" + publication.Config.CoursePatternGridWidth.ToString(CultureInfo.InvariantCulture) + "x" + publication.Config.CoursePatternGridHeight.ToString(CultureInfo.InvariantCulture) +
                " tile_size=" + publication.Config.TileWidth.ToString(CultureInfo.InvariantCulture) + "x" + publication.Config.TileHeight.ToString(CultureInfo.InvariantCulture) +
                " rooms=" + publication.Graph.CameraRoomCount.ToString(CultureInfo.InvariantCulture) + " connectors=" + publication.Connectors.ConnectorCount.ToString(CultureInfo.InvariantCulture) +
                "\nplacements=" + publication.Composition.PlacementCount.ToString(CultureInfo.InvariantCulture) + " map=" + publication.Composition.MapDigest + "\nroom_graph=" + publication.Graph.Digest.Value + "\nconnector=" + publication.Connectors.CanonicalDigest + "\nvalidation=" + publication.Validation.CanonicalDigest;
            text.color = new Color(0.82f, 0.86f, 0.96f, 1f); text.characterSize = 0.22f; text.fontSize = 18; text.anchor = TextAnchor.UpperLeft;
        }
        private static void CreateCamera(Transform parent)
        {
            var gameObject = new GameObject("RUN03_CourseCamera", typeof(Camera)); gameObject.transform.SetParent(parent, false); gameObject.transform.position = new Vector3(160f, 48f, -10f);
            var camera = gameObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 112f; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.035f, 0.043f, 0.060f, 1f); camera.nearClipPlane = 0.1f; camera.farClipPlane = 100f;
        }

        private static Dictionary<string, Tile> CreateDebugTiles()
        {
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); if (sprite == null) throw new InvalidOperationException("Unity built-in UISprite is required for RUN03 debug tiles.");
            var tiles = new Dictionary<string, Tile>(StringComparer.Ordinal);
            foreach (var definition in TileDefinitions)
            {
                var path = DebugTilePath(definition.Name); var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); tile.name = "RUN03_" + definition.Name; AssetDatabase.CreateAsset(tile, path); }
                tile.name = "RUN03_" + definition.Name; tile.sprite = sprite; tile.color = definition.Color; tile.colliderType = Tile.ColliderType.None; EditorUtility.SetDirty(tile); tiles.Add(definition.Name, tile);
            }
            AssetDatabase.SaveAssets();
            foreach (var definition in TileDefinitions)
            {
                AssetDatabase.ImportAsset(DebugTilePath(definition.Name), ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(DebugTilePath(definition.Name)); if (tile == null || !EditorUtility.IsPersistent(tile)) throw new InvalidOperationException("RUN03 debug Tile import failed: " + definition.Name); tiles[definition.Name] = tile;
            }
            return tiles;
        }
        private static Tilemap CreateTilemap(string name, Transform parent, int sortingOrder)
        {
            var gameObject = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)); gameObject.transform.SetParent(parent, false);
            var renderer = gameObject.GetComponent<TilemapRenderer>(); renderer.sortingOrder = sortingOrder; renderer.mode = TilemapRenderer.Mode.Chunk; return gameObject.GetComponent<Tilemap>();
        }
        private static void PaintFrame(Tilemap map, CameraRoomBoundsPattern bounds, Tile tile)
        {
            var minX = bounds.TileMinX; var minY = bounds.TileMinY; var maxX = minX + bounds.TileWidth - 1; var maxY = minY + bounds.TileHeight - 1;
            for (var x = minX; x <= maxX; x++) { map.SetTile(new Vector3Int(x, minY, 0), tile); map.SetTile(new Vector3Int(x, maxY, 0), tile); }
            for (var y = minY; y <= maxY; y++) { map.SetTile(new Vector3Int(minX, y, 0), tile); map.SetTile(new Vector3Int(maxX, y, 0), tile); }
        }
        private static void PaintRect(Tilemap map, CameraRoomTileGateRect rect, Tile tile)
        {
            for (var y = rect.Y; y < rect.Y + rect.Height; y++) for (var x = rect.X; x < rect.X + rect.Width; x++) map.SetTile(new Vector3Int(x, y, 0), tile);
        }

        private static string RoomsCsv(MoonPalaceCameraRoomGraph graph)
        {
            var lines = new List<string> { "room_id,role,route_order,pattern_min_x,pattern_min_y,pattern_max_x,pattern_max_y,pattern_width,pattern_height,tile_min_x,tile_min_y,tile_width,tile_height" };
            lines.AddRange(graph.Rooms.OrderBy(room => room.RoomId, StringComparer.Ordinal).Select(room => string.Join(",", room.RoomId, room.Role, I(room.RouteOrder), I(room.Bounds.MinX), I(room.Bounds.MinY), I(room.Bounds.MaxX), I(room.Bounds.MaxY), I(room.Bounds.Width), I(room.Bounds.Height), I(room.Bounds.TileMinX), I(room.Bounds.TileMinY), I(room.Bounds.TileWidth), I(room.Bounds.TileHeight)))); return Lines(lines);
        }
        private static string ConnectorsCsv(MoonPalaceCameraRoomConnectorPlan plan)
        {
            var lines = new List<string> { "connector_id,from_room_id,to_room_id,direction,from_pattern_slot,to_pattern_slot,from_tile_gate_rect,to_tile_gate_rect,required_socket_from,required_socket_to,transition_kind,is_required_for_completion" };
            lines.AddRange(plan.Gates.Select(gate => string.Join(",", gate.ConnectorId, gate.FromRoomId, gate.ToRoomId, gate.Direction, Csv(gate.FromPatternSlot.ToString()), Csv(gate.ToPatternSlot.ToString()), Csv(gate.FromTileGateRect.ToString()), Csv(gate.ToTileGateRect.ToString()), I(gate.RequiredSocketFrom), I(gate.RequiredSocketTo), gate.TransitionKind, B(gate.IsRequiredForCompletion)))); return Lines(lines);
        }
        private static string PatternSlotsCsv(MoonPalaceCameraRoomPatternComposition composition)
        {
            var lines = new List<string> { "slot_index,pattern_x,pattern_y,room_id,connector_ids,route_ownership,candidate_id,mask_hex,socket_signature,transform,map21_02_family_link,selection_reason,attempt_count" };
            lines.AddRange(composition.Placements.OrderBy(item => item.SlotIndex).Select(item => string.Join(",", I(item.SlotIndex), I(item.PatternSlot.X), I(item.PatternSlot.Y), item.RoomId, Csv(string.Join(";", item.ConnectorIds)), item.RouteOwnership, item.Candidate.CandidateId, item.Candidate.MaskU16Hex, item.Candidate.SocketSignature, item.Transform, item.PresentationFamilyLink, item.SelectionReason, I(item.AttemptCount)))); return Lines(lines);
        }
        private static string TileCellsCsv(MoonPalaceCameraRoomPatternComposition composition)
        {
            var builder = new StringBuilder("tile_x,tile_y,open,pattern_x,pattern_y,room_id,route_ownership\n");
            for (var y = 0; y < composition.Config.TileHeight; y++) for (var x = 0; x < composition.Config.TileWidth; x++)
            {
                var placement = composition.Placement(new PatternSlotCoordinate(x / composition.Config.PatternWidth, y / composition.Config.PatternHeight));
                builder.Append(I(x)).Append(',').Append(I(y)).Append(',').Append(B(composition.IsOpen(x, y))).Append(',').Append(I(placement.PatternSlot.X)).Append(',').Append(I(placement.PatternSlot.Y)).Append(',').Append(placement.RoomId).Append(',').Append(placement.RouteOwnership).Append('\n');
            }
            return FinalLf(builder.ToString());
        }
        private static string RoutePathsCsv(MoonPalaceCameraRoomGraph graph)
        {
            var lines = new List<string> { "route_ownership,owner_id,from_pattern_slot,to_pattern_slot,direction" };
            lines.AddRange(graph.Edges.OrderBy(edge => edge.RouteOwnership, StringComparer.Ordinal).ThenBy(edge => edge.OwnerId, StringComparer.Ordinal).ThenBy(edge => edge.From.Y).ThenBy(edge => edge.From.X).Select(edge => string.Join(",", edge.RouteOwnership, edge.OwnerId, Csv(edge.From.ToString()), Csv(edge.To.ToString()), edge.Direction))); return Lines(lines);
        }
        private static string ValidationSummaryCsv(MoonPalaceCameraRoomCourseValidation validation)
        {
            return Lines(new[] { "course_id,tile_width,tile_height,placements,start_to_exit,main_rooms_in_order,branch_entries,split_rejoin,connector_gates_open,unreachable_required_rooms,route_socket_mismatches,connector_blocked,fallback_carve,silent_repair,validation_digest",
                string.Join(",", validation.Composition.Config.CourseId, I(validation.Composition.Config.TileWidth), I(validation.Composition.Config.TileHeight), I(validation.PatternPlacementCount), B(validation.StartToExitReachable), B(validation.AllMainRouteRoomsReachableInOrder), B(validation.AllBranchEntriesReachable), B(validation.AllSplitRejoinPathsReachable), B(validation.AllConnectorGatesOpen), I(validation.UnreachableRequiredRoomCount), I(validation.RouteSocketMismatchCount), I(validation.ConnectorBlockedCount), I(validation.FallbackCarveCount), I(validation.SilentRepairCount), validation.CanonicalDigest) });
        }

        private static string ConfigJson(MoonPalaceCameraRoomCourseConfig config) => Json(new[] { Pair("course_id", config.CourseId), Pair("seed_id", config.SeedId), Number("seed_value", config.SeedValue), Pair("pattern_size", config.PatternWidth + "x" + config.PatternHeight), Pair("course_pattern_grid", config.CoursePatternGridWidth + "x" + config.CoursePatternGridHeight), Pair("course_tile_size", config.TileWidth + "x" + config.TileHeight), Number("candidate_count", config.CandidateCount), Bool("allow_90_degree_rotation", config.Allow90DegreeRotation), Bool("allow_silent_carve", config.AllowSilentCarve), Pair("canonical_digest", config.CanonicalDigest) });
        private static string GraphJson(MoonPalaceCameraRoomGraph graph)
        {
            var rooms = string.Join(",\n", graph.Rooms.OrderBy(room => room.RoomId, StringComparer.Ordinal).Select(room => "    {\"room_id\":\"" + Escape(room.RoomId) + "\",\"role\":\"" + room.Role + "\",\"pattern_bounds\":\"" + room.Bounds + "\",\"tile_bounds\":\"" + room.Bounds.TileMinX + ":" + room.Bounds.TileMinY + ":" + room.Bounds.TileWidth + "x" + room.Bounds.TileHeight + "\"}"));
            return Lines(new[] { "{", "  \"room_graph_digest\": \"" + graph.Digest.Value + "\",", "  \"main_route_rooms\": \"" + string.Join(";", graph.MainRoute.RoomIds) + "\",", "  \"vertical_transition_span_pattern_rows\": " + I(graph.VerticalTransitionSpanPatternRows) + ",", "  \"rooms\": [", rooms, "  ]", "}" });
        }
        private static string ConnectorsJson(MoonPalaceCameraRoomConnectorPlan plan)
        {
            var entries = plan.Gates.Select(gate => "    {\"connector_id\":\"" + gate.ConnectorId + "\",\"from_room_id\":\"" + gate.FromRoomId + "\",\"to_room_id\":\"" + gate.ToRoomId + "\",\"direction\":\"" + gate.Direction + "\",\"from_pattern_slot\":\"" + gate.FromPatternSlot + "\",\"to_pattern_slot\":\"" + gate.ToPatternSlot + "\",\"from_tile_gate_rect\":\"" + gate.FromTileGateRect + "\",\"to_tile_gate_rect\":\"" + gate.ToTileGateRect + "\",\"transition_kind\":\"" + gate.TransitionKind + "\"}");
            return Lines(new[] { "{", "  \"connector_digest\": \"" + plan.CanonicalDigest + "\",", "  \"uses_sector_seam_logic\": false,", "  \"connectors\": [", string.Join(",\n", entries), "  ]", "}" });
        }
        private static string CompositionJson(MoonPalaceCameraRoomPatternComposition composition)
        {
            var entries = composition.Placements.OrderBy(item => item.SlotIndex).Select(item => "    {\"slot_index\":" + I(item.SlotIndex) + ",\"slot\":\"" + item.PatternSlot + "\",\"room_id\":\"" + item.RoomId + "\",\"candidate_id\":\"" + item.Candidate.CandidateId + "\",\"mask_hex\":\"" + item.Candidate.MaskU16Hex + "\",\"socket_signature\":\"" + item.Candidate.SocketSignature + "\",\"transform\":\"" + item.Transform + "\",\"selection_reason\":\"" + item.SelectionReason + "\"}");
            return Lines(new[] { "{", "  \"composition_digest\": \"" + composition.CompositionDigest + "\",", "  \"map_digest\": \"" + composition.MapDigest + "\",", "  \"placement_count\": " + I(composition.PlacementCount) + ",", "  \"rotation_count\": 0,", "  \"fallback_carve_count\": 0,", "  \"silent_repair_count\": 0,", "  \"placements\": [", string.Join(",\n", entries), "  ]", "}" });
        }
        private static string ValidationJson(MoonPalaceCameraRoomCourseValidation validation) => Json(new[] { Pair("validation_digest", validation.CanonicalDigest), Bool("start_to_exit_pass", validation.StartToExitReachable), Bool("all_room_bounds_inside", validation.AllRoomBoundsInside), Bool("all_connector_gates_reciprocal", validation.AllConnectorGatesReciprocal), Bool("all_connector_gates_open", validation.AllConnectorGatesOpen), Bool("all_main_route_rooms_reachable_in_order", validation.AllMainRouteRoomsReachableInOrder), Bool("all_branch_entries_reachable", validation.AllBranchEntriesReachable), Bool("all_split_rejoin_paths_reachable", validation.AllSplitRejoinPathsReachable), Number("unreachable_required_room_count", validation.UnreachableRequiredRoomCount), Number("route_socket_mismatch_count", validation.RouteSocketMismatchCount), Number("connector_blocked_count", validation.ConnectorBlockedCount), Number("fallback_carve_count", validation.FallbackCarveCount), Number("silent_repair_count", validation.SilentRepairCount) });
        private static string SceneManifestJson(MoonPalaceCameraRoomCourseConfig config, MoonPalaceCameraRoomGraph graph, MoonPalaceCameraRoomConnectorPlan connectors, MoonPalaceCameraRoomPatternComposition composition, MoonPalaceCameraRoomCourseValidation validation, string digest) => Json(new[] { Pair("scene_relative_path", SceneRelativePath), Pair("scene_root", SceneRootName), Pair("scene_manifest_digest", digest), Pair("course_id", config.CourseId), Number("room_count", graph.CameraRoomCount), Number("connector_count", connectors.ConnectorCount), Number("placement_count", composition.PlacementCount), Pair("validation_digest", validation.CanonicalDigest), Pair("layers", "Terrain;MainRoute;BranchSplitRejoin;RoomFrameConnector") });
        private static string DigestManifestJson(IDictionary<string, string> outputs, string digest, string sceneDigest)
        {
            var entries = outputs.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => "    {\"relative_path\":\"" + Escape(item.Key) + "\",\"sha256\":\"" + BakingCanonicalDigest.HashCanonicalText(item.Value) + "\"}");
            return Lines(new[] { "{", "  \"scene_manifest_digest\": \"" + sceneDigest + "\",", "  \"digest_manifest_digest\": \"" + digest + "\",", "  \"artifacts\": [", string.Join(",\n", entries), "  ]", "}" });
        }

        private static string Json(IEnumerable<string> fields) => Lines(new[] { "{" }.Concat(fields.Select(field => "  " + field)).Select((line, index) => index == 0 ? line : line).ToArray().Select((line, index) => index > 0 && index < fields.Count() ? line + "," : line).Concat(new[] { "}" }));
        private static string Pair(string name, string value) => "\"" + Escape(name) + "\": \"" + Escape(value) + "\"";
        private static string Number(string name, int value) => "\"" + Escape(name) + "\": " + I(value);
        private static string Bool(string name, bool value) => "\"" + Escape(name) + "\": " + (value ? "true" : "false");
        private static string Lines(IEnumerable<string> lines) => string.Join("\n", lines) + "\n";
        private static string FinalLf(string value) => BakingCanonicalDigest.NormalizeLineEndingsToLf(value ?? string.Empty).TrimEnd('\n') + "\n";
        private static string Csv(string value) { value = value ?? string.Empty; return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value; }
        private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string B(bool value) => value ? "TRUE" : "FALSE";
        private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        private static string Join(string left, string right) => left.TrimEnd('/') + "/" + right.TrimStart('/');
        private static string Resolve(string root, string relative) => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        private static string DebugTilePath(string name) => Join(SceneDirectoryRelativePath, "RUN03_" + name + ".asset");
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private sealed class TileDefinition { public TileDefinition(string name, Color color) { Name = name; Color = color; } public string Name { get; } public Color Color { get; } }
    }
}
