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
    public sealed class MoonPalaceSeedRegeneratePublication
    {
        internal MoonPalaceSeedRegeneratePublication(MoonPalaceSeededRunResult active, IEnumerable<MoonPalaceSeededRunResult> gallery, IDictionary<string, string> outputs, string sceneDigest, string digest)
        {
            Active = active; Gallery = new ReadOnlyCollection<MoonPalaceSeededRunResult>((gallery ?? Array.Empty<MoonPalaceSeededRunResult>()).ToList()); OutputContents = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(outputs, StringComparer.Ordinal)); SceneManifestDigest = sceneDigest ?? string.Empty; DigestManifestDigest = digest ?? string.Empty;
        }
        public MoonPalaceSeededRunResult Active { get; }
        public IReadOnlyList<MoonPalaceSeededRunResult> Gallery { get; }
        public IReadOnlyDictionary<string, string> OutputContents { get; }
        public string SceneManifestDigest { get; }
        public string DigestManifestDigest { get; }
    }

    /// <summary>RUN05-only publisher. It replaces one seed preview Scene and creates no Build Settings, Prefab, or prior-Scene changes.</summary>
    public static class MoonPalaceSeedRegenerateSceneBuilder
    {
        public const string SceneRelativePath = "Assets/_Game/Map/Scenes/MoonPalace/RUN05/MoonPalaceSeedRegeneratePreview_RUN05.unity";
        public const string SceneDirectoryRelativePath = "Assets/_Game/Map/Scenes/MoonPalace/RUN05";
        public const string AuthoringDirectoryRelativePath = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN05";
        public const string GeneratedDirectoryRelativePath = "MapDesign/MCP/GENERATED/RUN05";
        public const string SceneRootName = "MoonPalace_SeedRegeneratePreview_RUN05";
        public static readonly int[] GallerySeeds = { 1924737067, 1924737068, 1924737069, 1924737070 };
        private static readonly string[] CsvNames = { "moonpalace_run05_recipe_catalog.csv", "moonpalace_run05_gallery_summary.csv", "moonpalace_run05_active_rooms.csv", "moonpalace_run05_active_connectors.csv", "moonpalace_run05_active_pattern_slots.csv", "moonpalace_run05_active_route_path.csv", "moonpalace_run05_validation_summary.csv" };
        private static readonly string[] JsonNames = { "moonpalace_run05_recipe_catalog.json", "moonpalace_run05_active_generation.json", "moonpalace_run05_gallery_generations.json", "moonpalace_run05_room_graphs.json", "moonpalace_run05_connectors.json", "moonpalace_run05_pattern_placements.json", "moonpalace_run05_validation.json", "moonpalace_run05_scene_manifest.json", "moonpalace_run05_digest_manifest.json" };
        private static readonly TileDefinition[] TileDefinitions =
        {
            new TileDefinition("Solid", new Color(0.055f, 0.065f, 0.085f, 1f)), new TileDefinition("Open", new Color(0.13f, 0.19f, 0.25f, 1f)), new TileDefinition("Main", new Color(0.20f, 0.94f, 0.44f, 0.92f)), new TileDefinition("BranchSplit", new Color(0.16f, 0.66f, 1f, 0.92f)), new TileDefinition("Frame", new Color(0.76f, 0.36f, 1f, 1f)), new TileDefinition("Gate", new Color(1f, 0.34f, 0.72f, 1f)), new TileDefinition("Ghost", new Color(1f, 0.88f, 0.22f, 0.84f)),
        };

        public static MoonPalaceSeedRegeneratePublication LastPublished { get; private set; }
        public static IReadOnlyList<string> GetExpectedOutputRelativePaths() => new ReadOnlyCollection<string>(CsvNames.Select(name => Join(AuthoringDirectoryRelativePath, name)).Concat(JsonNames.Select(name => Join(GeneratedDirectoryRelativePath, name))).OrderBy(path => path, StringComparer.Ordinal).ToList());
        public static IReadOnlyList<string> GetDebugTileAssetRelativePaths() => new ReadOnlyCollection<string>(TileDefinitions.Select(definition => DebugTilePath(definition.Name)).OrderBy(path => path, StringComparer.Ordinal).ToList());
        public static MoonPalaceSeededRunValidation ValidateCurrent(string recipeId, int seed) => MoonPalaceSeededRunValidator.Validate(MoonPalaceSeededRunGenerator.Generate(recipeId, seed));

        public static MoonPalaceSeedRegeneratePublication BuildSnapshot(string projectRoot, string recipeId, int seed)
        {
            if (string.IsNullOrWhiteSpace(projectRoot)) throw new ArgumentException("RUN05 requires a project root.", nameof(projectRoot));
            var active = MoonPalaceSeededRunGenerator.Generate(recipeId, seed);
            var gallery = BuildGallery();
            var sceneDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "RUN05_SCENE", active.CourseDigest, string.Join(";", gallery.Select(result => result.CourseDigest)), SceneRelativePath, SceneRootName });
            var outputs = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                [Join(AuthoringDirectoryRelativePath, CsvNames[0])] = RecipeCatalogCsv(),
                [Join(AuthoringDirectoryRelativePath, CsvNames[1])] = GalleryCsv(gallery),
                [Join(AuthoringDirectoryRelativePath, CsvNames[2])] = RoomsCsv(active),
                [Join(AuthoringDirectoryRelativePath, CsvNames[3])] = ConnectorsCsv(active),
                [Join(AuthoringDirectoryRelativePath, CsvNames[4])] = PlacementsCsv(active),
                [Join(AuthoringDirectoryRelativePath, CsvNames[5])] = RouteCsv(active),
                [Join(AuthoringDirectoryRelativePath, CsvNames[6])] = ValidationCsv(active.Validation),
                [Join(GeneratedDirectoryRelativePath, JsonNames[0])] = RecipeCatalogJson(),
                [Join(GeneratedDirectoryRelativePath, JsonNames[1])] = ActiveJson(active),
                [Join(GeneratedDirectoryRelativePath, JsonNames[2])] = GalleryJson(gallery),
                [Join(GeneratedDirectoryRelativePath, JsonNames[3])] = RoomGraphsJson(active, gallery),
                [Join(GeneratedDirectoryRelativePath, JsonNames[4])] = ConnectorsJson(active),
                [Join(GeneratedDirectoryRelativePath, JsonNames[5])] = PlacementsJson(active),
                [Join(GeneratedDirectoryRelativePath, JsonNames[6])] = ValidationJson(active.Validation),
                [Join(GeneratedDirectoryRelativePath, JsonNames[7])] = SceneManifestJson(active, gallery, sceneDigest),
            };
            var digest = BakingCanonicalDigest.HashCanonicalLines(outputs.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + "|" + BakingCanonicalDigest.HashCanonicalText(pair.Value)));
            outputs.Add(Join(GeneratedDirectoryRelativePath, JsonNames[8]), DigestManifestJson(outputs, sceneDigest, digest));
            return new MoonPalaceSeedRegeneratePublication(active, gallery, outputs, sceneDigest, digest);
        }

        public static MoonPalaceSeedRegeneratePublication Publish(string projectRoot, string recipeId, int seed)
        {
            var publication = BuildSnapshot(projectRoot, recipeId, seed);
            Directory.CreateDirectory(Resolve(projectRoot, SceneDirectoryRelativePath)); Directory.CreateDirectory(Resolve(projectRoot, AuthoringDirectoryRelativePath)); Directory.CreateDirectory(Resolve(projectRoot, GeneratedDirectoryRelativePath));
            foreach (var output in publication.OutputContents) File.WriteAllText(Resolve(projectRoot, output.Key), FinalLf(output.Value), new UTF8Encoding(false));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); CreateScene(publication); AssetDatabase.SaveAssets(); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); LastPublished = publication; return publication;
        }

        private static IReadOnlyList<MoonPalaceSeededRunResult> BuildGallery()
        {
            var recipes = new[] { MoonPalaceSeededRunRecipeCatalog.Get("WIDE_BRANCH_RUN"), MoonPalaceSeededRunRecipeCatalog.Get("TALL_LOOP_RUN"), MoonPalaceSeededRunRecipeCatalog.Get("COMPACT_SPLIT_RUN"), MoonPalaceSeededRunRecipeCatalog.Get("WIDE_BRANCH_RUN") };
            return GallerySeeds.Select((seed, index) => MoonPalaceSeededRunGenerator.Generate(recipes[index], seed)).ToList();
        }

        private static void CreateScene(MoonPalaceSeedRegeneratePublication publication)
        {
            var tiles = CreateDebugTiles();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(SceneRootName, typeof(Grid));
            var activeGroup = new GameObject("ActivePreview"); activeGroup.transform.SetParent(root.transform, false);
            var terrain = CreateTilemap("Terrain", activeGroup.transform, 0); var main = CreateTilemap("MainRoute", activeGroup.transform, 2); var split = CreateTilemap("BranchSplit", activeGroup.transform, 3);
            PaintActive(publication.Active, terrain, main, split, tiles);
            var frames = CreateTilemap("RoomFrames", root.transform, 4); foreach (var frame in publication.Active.RoomFrames) PaintFrame(frames, frame, tiles["Frame"]);
            var gates = CreateTilemap("ConnectorGates", root.transform, 5); foreach (var connector in publication.Active.Connectors) { gates.SetTile(new Vector3Int(connector.FromGateTile.x, connector.FromGateTile.y, 0), tiles["Gate"]); gates.SetTile(new Vector3Int(connector.ToGateTile.x, connector.ToGateTile.y, 0), tiles["Gate"]); }
            var ghostMap = CreateTilemap("RouteGhost", root.transform, 1); foreach (var tile in publication.Active.FindRouteToExit()) ghostMap.SetTile(new Vector3Int(tile.x, tile.y, 0), tiles["Ghost"]); var routeGhost = ghostMap.gameObject.AddComponent<MoonPalaceSeededRunPreviewRouteGhost>(); routeGhost.Initialize(publication.Active, true);
            CreateGallery(root.transform, publication.Gallery, tiles);
            var cameraObject = new GameObject("PreviewCamera", typeof(Camera), typeof(MoonPalaceSeededRunPreviewCameraController)); cameraObject.transform.SetParent(root.transform, false); var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.025f, 0.033f, 0.050f, 1f); camera.nearClipPlane = 0.1f; camera.farClipPlane = 100f; var cameraController = cameraObject.GetComponent<MoonPalaceSeededRunPreviewCameraController>(); cameraController.Configure(camera, true, 0.18f); cameraController.Initialize(publication.Active, publication.Active.GetRoomId(publication.Active.StartTile.x, publication.Active.StartTile.y));
            var labels = new GameObject("Labels"); labels.transform.SetParent(root.transform, false); CreateText("Title", labels.transform, new Vector3(2f, publication.Active.TileHeight - 2f, -3f), "RUN05 Seed Regenerate Preview | " + publication.Active.Recipe.RecipeId + " | seed=" + publication.Active.Seed.ToString(CultureInfo.InvariantCulture), new Color(0.88f, 0.92f, 1f, 1f), 0.32f, TextAnchor.UpperLeft); var roomLabel = CreateText("CurrentRoomLabel", labels.transform, new Vector3(2f, publication.Active.TileHeight - 7f, -3f), "Current room: " + publication.Active.GetRoomId(publication.Active.StartTile.x, publication.Active.StartTile.y), Color.white, 0.30f, TextAnchor.UpperLeft); var tileLabel = CreateText("CurrentTileLabel", labels.transform, new Vector3(2f, publication.Active.TileHeight - 10f, -3f), "Tile: " + Point(publication.Active.StartTile) + " steps=0 blocked=0", Color.white, 0.25f, TextAnchor.UpperLeft); CreateText("Controls", labels.transform, new Vector3(2f, publication.Active.TileHeight - 13f, -3f), "WASD / Arrow move | R reset | G route ghost", new Color(1f, 0.88f, 0.32f, 1f), 0.25f, TextAnchor.UpperLeft); foreach (var frame in publication.Active.RoomFrames) CreateText("Room_" + frame.RoomId, labels.transform, new Vector3(frame.Center.x, frame.TileMinY + frame.TileHeight + 0.8f, -3f), frame.RoomId, new Color(0.85f, 0.56f, 1f, 1f), 0.20f, TextAnchor.MiddleCenter);
            var player = new GameObject("PreviewPlayer", typeof(MoonPalaceSeededRunPreviewController)); player.transform.SetParent(root.transform, false); CreateText("Marker", player.transform, Vector3.zero, "●", new Color(0.48f, 1f, 0.50f, 1f), 0.78f, TextAnchor.MiddleCenter); var playerController = player.GetComponent<MoonPalaceSeededRunPreviewController>(); playerController.Configure(publication.Active.Recipe.RecipeId, publication.Active.Seed, tileLabel, roomLabel, cameraController, routeGhost); playerController.Initialize(publication.Active);
            var metadata = new GameObject("Metadata"); metadata.transform.SetParent(root.transform, false); CreateText("MetadataText", metadata.transform, new Vector3(2f, publication.Active.TileHeight - 18f, -3f), "tile grid=" + publication.Active.TileWidth + "x" + publication.Active.TileHeight + " rooms=" + publication.Active.Rooms.Count + " connectors=" + publication.Active.Connectors.Count + " placements=" + publication.Active.PatternPlacementCount + "\ncourse=" + publication.Active.CourseDigest + "\nvalidation=" + publication.Active.Validation.CanonicalDigest + "\nGallery: four deterministic generated courses below", new Color(0.60f, 0.72f, 0.88f, 1f), 0.18f, TextAnchor.UpperLeft);
            EditorSceneManager.SaveScene(scene, SceneRelativePath);
        }

        private static void PaintActive(MoonPalaceSeededRunResult result, Tilemap terrain, Tilemap main, Tilemap split, IDictionary<string, Tile> tiles)
        {
            for (var y = 0; y < result.TileHeight; y++) for (var x = 0; x < result.TileWidth; x++) { var cell = new Vector3Int(x, y, 0); terrain.SetTile(cell, result.IsOpen(x, y) ? tiles["Open"] : tiles["Solid"]); var ownership = result.GetRouteOwnership(x, y); if (ownership == "MAIN") main.SetTile(cell, tiles["Main"]); else if (ownership == "BRANCH" || ownership == "SPLIT_REJOIN") split.SetTile(cell, tiles["BranchSplit"]); }
        }
        private static void CreateGallery(Transform root, IReadOnlyList<MoonPalaceSeededRunResult> gallery, IDictionary<string, Tile> tiles)
        {
            var group = new GameObject("SeedGallery"); group.transform.SetParent(root, false);
            for (var index = 0; index < gallery.Count; index++)
            {
                var result = gallery[index]; var course = new GameObject("GalleryCourse_" + (index + 1).ToString("00", CultureInfo.InvariantCulture) + "_" + result.Recipe.RecipeId); course.transform.SetParent(group.transform, false); course.transform.localPosition = new Vector3((index % 2) * 105f, -130f - (index / 2) * 42f, 0f); course.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
                var terrain = CreateTilemap("GeneratedCourse", course.transform, 0); var route = CreateTilemap("Route", course.transform, 1); var frame = CreateTilemap("Frames", course.transform, 2); var gates = CreateTilemap("ConnectorGates", course.transform, 3);
                foreach (var placement in result.Placements) terrain.SetTile(new Vector3Int(placement.PatternSlot.X, placement.PatternSlot.Y, 0), placement.Candidate.IsOpen(1, 1) ? tiles["Open"] : tiles["Solid"]);
                foreach (var edge in result.Edges) { route.SetTile(new Vector3Int(edge.From.X, edge.From.Y, 0), edge.Ownership == "MAIN" ? tiles["Main"] : tiles["BranchSplit"]); route.SetTile(new Vector3Int(edge.To.X, edge.To.Y, 0), edge.Ownership == "MAIN" ? tiles["Main"] : tiles["BranchSplit"]); }
                foreach (var room in result.Rooms) PaintPatternFrame(frame, room, tiles["Frame"]);
                foreach (var connector in result.Connectors) { gates.SetTile(new Vector3Int(connector.FromPatternSlot.X, connector.FromPatternSlot.Y, 0), tiles["Gate"]); gates.SetTile(new Vector3Int(connector.ToPatternSlot.X, connector.ToPatternSlot.Y, 0), tiles["Gate"]); }
                CreateText("Label", course.transform, new Vector3(0f, result.Recipe.PatternGridHeight + 2f, -3f), result.Recipe.RecipeId + "\nseed=" + result.Seed.ToString(CultureInfo.InvariantCulture) + "\n" + result.PlacementDigest.Substring(0, 12), Color.white, 1.0f, TextAnchor.UpperLeft);
            }
        }
        private static Dictionary<string, Tile> CreateDebugTiles()
        {
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); if (sprite == null) throw new InvalidOperationException("RUN05 requires the built-in UI sprite for its isolated debug tiles."); var tiles = new Dictionary<string, Tile>(StringComparer.Ordinal);
            foreach (var definition in TileDefinitions) { var path = DebugTilePath(definition.Name); var tile = AssetDatabase.LoadAssetAtPath<Tile>(path); if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); } tile.name = "RUN05_" + definition.Name; tile.sprite = sprite; tile.color = definition.Color; tile.colliderType = Tile.ColliderType.None; EditorUtility.SetDirty(tile); tiles.Add(definition.Name, tile); }
            AssetDatabase.SaveAssets(); return tiles;
        }
        private static Tilemap CreateTilemap(string name, Transform parent, int order) { var gameObject = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)); gameObject.transform.SetParent(parent, false); var renderer = gameObject.GetComponent<TilemapRenderer>(); renderer.sortingOrder = order; renderer.mode = TilemapRenderer.Mode.Chunk; return gameObject.GetComponent<Tilemap>(); }
        private static TextMesh CreateText(string name, Transform parent, Vector3 position, string value, Color color, float size, TextAnchor anchor) { var gameObject = new GameObject(name); gameObject.transform.SetParent(parent, false); gameObject.transform.localPosition = position; var text = gameObject.AddComponent<TextMesh>(); text.text = value; text.color = color; text.characterSize = size; text.fontSize = 28; text.anchor = anchor; return text; }
        private static void PaintFrame(Tilemap map, MoonPalaceSeededRunRoomFrame frame, Tile tile) { var maxX = frame.TileMinX + frame.TileWidth - 1; var maxY = frame.TileMinY + frame.TileHeight - 1; for (var x = frame.TileMinX; x <= maxX; x++) { map.SetTile(new Vector3Int(x, frame.TileMinY, 0), tile); map.SetTile(new Vector3Int(x, maxY, 0), tile); } for (var y = frame.TileMinY; y <= maxY; y++) { map.SetTile(new Vector3Int(frame.TileMinX, y, 0), tile); map.SetTile(new Vector3Int(maxX, y, 0), tile); } }
        private static void PaintPatternFrame(Tilemap map, MoonPalaceSeededRunRoomRecord room, Tile tile) { for (var x = room.MinPatternX; x <= room.MaxPatternX; x++) { map.SetTile(new Vector3Int(x, room.MinPatternY, 0), tile); map.SetTile(new Vector3Int(x, room.MaxPatternY, 0), tile); } for (var y = room.MinPatternY; y <= room.MaxPatternY; y++) { map.SetTile(new Vector3Int(room.MinPatternX, y, 0), tile); map.SetTile(new Vector3Int(room.MaxPatternX, y, 0), tile); } }

        private static string RecipeCatalogCsv() { var lines = new List<string> { "recipe_id,pattern_grid,tile_grid,main_room_target,branch_target,split_rejoin_target,shape,digest" }; lines.AddRange(MoonPalaceSeededRunRecipeCatalog.Recipes.Select(recipe => string.Join(",", recipe.RecipeId, recipe.PatternGridWidth + "x" + recipe.PatternGridHeight, recipe.TileWidth + "x" + recipe.TileHeight, I(recipe.MainRoomTarget), I(recipe.BranchTarget), I(recipe.SplitRejoinTarget), Csv(recipe.ShapeDescription), recipe.CanonicalDigest))); return Lines(lines); }
        private static string GalleryCsv(IEnumerable<MoonPalaceSeededRunResult> gallery) { var lines = new List<string> { "gallery_index,recipe_id,seed,pattern_grid,tile_grid,rooms,connectors,placements,placement_digest,course_digest,bfs_pass" }; lines.AddRange(gallery.Select((result, index) => string.Join(",", I(index), result.Recipe.RecipeId, I(result.Seed), result.Recipe.PatternGridWidth + "x" + result.Recipe.PatternGridHeight, result.TileWidth + "x" + result.TileHeight, I(result.Rooms.Count), I(result.Connectors.Count), I(result.PatternPlacementCount), result.PlacementDigest, result.CourseDigest, B(result.Validation.StartToExitReachable)))); return Lines(lines); }
        private static string RoomsCsv(MoonPalaceSeededRunResult result) { var lines = new List<string> { "room_id,role,route_order,pattern_min_x,pattern_min_y,pattern_max_x,pattern_max_y,tile_min_x,tile_min_y,tile_width,tile_height" }; lines.AddRange(result.Rooms.OrderBy(room => room.RoomId, StringComparer.Ordinal).Select(room => string.Join(",", room.RoomId, room.Role, I(room.RouteOrder), I(room.MinPatternX), I(room.MinPatternY), I(room.MaxPatternX), I(room.MaxPatternY), I(room.TileMinX), I(room.TileMinY), I(room.TileWidth), I(room.TileHeight)))); return Lines(lines); }
        private static string ConnectorsCsv(MoonPalaceSeededRunResult result) { var lines = new List<string> { "connector_id,from_room_id,to_room_id,from_slot,to_slot,from_gate_tile,to_gate_tile,direction,kind,required_socket,is_reciprocal,is_open" }; lines.AddRange(result.Connectors.OrderBy(connector => connector.ConnectorId, StringComparer.Ordinal).Select(connector => string.Join(",", connector.ConnectorId, connector.FromRoomId, connector.ToRoomId, Csv(connector.FromPatternSlot.ToString()), Csv(connector.ToPatternSlot.ToString()), Csv(Point(connector.FromGateTile)), Csv(Point(connector.ToGateTile)), connector.Direction, connector.Kind, I(connector.RequiredSocketBit), B(connector.IsReciprocal), B(connector.IsOpen)))); return Lines(lines); }
        private static string PlacementsCsv(MoonPalaceSeededRunResult result) { var lines = new List<string> { "slot_index,pattern_x,pattern_y,room_id,route_ownership,candidate_id,mask_hex,socket_signature,transform,attempt_count" }; lines.AddRange(result.Placements.OrderBy(placement => placement.SlotIndex).Select(placement => string.Join(",", I(placement.SlotIndex), I(placement.PatternSlot.X), I(placement.PatternSlot.Y), placement.RoomId, placement.RouteOwnership, placement.Candidate.CandidateId, placement.Candidate.MaskU16Hex, placement.Candidate.SocketSignature, placement.Transform, I(placement.AttemptCount)))); return Lines(lines); }
        private static string RouteCsv(MoonPalaceSeededRunResult result) { var lines = new List<string> { "step,tile_x,tile_y,room_id,route_ownership,is_start,is_exit" }; lines.AddRange(result.FindRouteToExit().Select((tile, index) => string.Join(",", I(index), I(tile.x), I(tile.y), result.GetRoomId(tile.x, tile.y), result.GetRouteOwnership(tile.x, tile.y), B(tile.Equals(result.StartTile)), B(tile.Equals(result.ExitTile))))); return Lines(lines); }
        private static string ValidationCsv(MoonPalaceSeededRunValidation value) => Lines(new[] { "recipe_id,seed,start_open,exit_open,bfs,room_bounds,reciprocal,gate_open,main_rooms,branches,splits,fallback_carve,silent_repair,rotation,validation_digest", string.Join(",", value.Result.Recipe.RecipeId, I(value.Result.Seed), B(value.StartOpen), B(value.ExitOpen), B(value.StartToExitReachable), B(value.AllRoomBoundsInside), B(value.AllConnectorsReciprocal), B(value.AllConnectorGateTilesOpen), B(value.AllMainRoomsReachableInOrder), B(value.AllBranchEntriesReachable), B(value.AllSplitRejoinPathsReachable), I(value.FallbackCarveCount), I(value.SilentRepairCount), I(value.RotationCount), value.CanonicalDigest) });

        private static string RecipeCatalogJson() => JsonArray("recipes", MoonPalaceSeededRunRecipeCatalog.Recipes.Select(recipe => "{\"recipe_id\":\"" + Escape(recipe.RecipeId) + "\",\"pattern_grid\":\"" + recipe.PatternGridWidth + "x" + recipe.PatternGridHeight + "\",\"tile_grid\":\"" + recipe.TileWidth + "x" + recipe.TileHeight + "\",\"main_room_target\":" + recipe.MainRoomTarget + ",\"branch_target\":" + recipe.BranchTarget + ",\"split_rejoin_target\":" + recipe.SplitRejoinTarget + ",\"digest\":\"" + recipe.CanonicalDigest + "\"}"));
        private static string ActiveJson(MoonPalaceSeededRunResult result) => Json(new[] { Pair("recipe_id", result.Recipe.RecipeId), Number("seed", result.Seed), Pair("pattern_grid", result.Recipe.PatternGridWidth + "x" + result.Recipe.PatternGridHeight), Pair("tile_grid", result.TileWidth + "x" + result.TileHeight), Number("room_count", result.Rooms.Count), Number("connector_count", result.Connectors.Count), Number("candidate_pool_count", result.CandidatePoolCount), Number("pattern_placement_count", result.PatternPlacementCount), Pair("start_tile", Point(result.StartTile)), Pair("exit_tile", Point(result.ExitTile)), Pair("room_graph_digest", result.RoomGraphDigest), Pair("placement_digest", result.PlacementDigest), Pair("course_digest", result.CourseDigest) });
        private static string GalleryJson(IEnumerable<MoonPalaceSeededRunResult> results) => JsonArray("gallery", results.Select((result, index) => "{\"index\":" + index + ",\"recipe_id\":\"" + result.Recipe.RecipeId + "\",\"seed\":" + result.Seed + ",\"placement_digest\":\"" + result.PlacementDigest + "\",\"course_digest\":\"" + result.CourseDigest + "\",\"bfs_pass\":" + (result.Validation.StartToExitReachable ? "true" : "false") + "}"));
        private static string RoomGraphsJson(MoonPalaceSeededRunResult active, IEnumerable<MoonPalaceSeededRunResult> gallery) => JsonArray("courses", new[] { active }.Concat(gallery).Select(result => "{\"recipe_id\":\"" + result.Recipe.RecipeId + "\",\"seed\":" + result.Seed + ",\"room_graph_digest\":\"" + result.RoomGraphDigest + "\",\"room_count\":" + result.Rooms.Count + "}"));
        private static string ConnectorsJson(MoonPalaceSeededRunResult result) => JsonArray("connectors", result.Connectors.OrderBy(connector => connector.ConnectorId, StringComparer.Ordinal).Select(connector => "{\"connector_id\":\"" + connector.ConnectorId + "\",\"from_room_id\":\"" + connector.FromRoomId + "\",\"to_room_id\":\"" + connector.ToRoomId + "\",\"from_gate_tile\":\"" + Point(connector.FromGateTile) + "\",\"to_gate_tile\":\"" + Point(connector.ToGateTile) + "\",\"kind\":\"" + connector.Kind + "\",\"is_reciprocal\":" + (connector.IsReciprocal ? "true" : "false") + ",\"is_open\":" + (connector.IsOpen ? "true" : "false") + "}"));
        private static string PlacementsJson(MoonPalaceSeededRunResult result) => JsonArray("placements", result.Placements.OrderBy(placement => placement.SlotIndex).Select(placement => "{\"slot_index\":" + placement.SlotIndex + ",\"slot\":\"" + placement.PatternSlot + "\",\"candidate_id\":\"" + placement.Candidate.CandidateId + "\",\"mask_hex\":\"" + placement.Candidate.MaskU16Hex + "\",\"room_id\":\"" + placement.RoomId + "\",\"route_ownership\":\"" + placement.RouteOwnership + "\"}"));
        private static string ValidationJson(MoonPalaceSeededRunValidation value) => Json(new[] { Pair("validation_digest", value.CanonicalDigest), Bool("passed", value.Passed), Bool("start_open", value.StartOpen), Bool("exit_open", value.ExitOpen), Bool("bfs", value.StartToExitReachable), Bool("room_bounds", value.AllRoomBoundsInside), Bool("connectors_reciprocal", value.AllConnectorsReciprocal), Bool("gates_open", value.AllConnectorGateTilesOpen), Bool("main_rooms", value.AllMainRoomsReachableInOrder), Bool("branches", value.AllBranchEntriesReachable), Bool("splits", value.AllSplitRejoinPathsReachable), Number("fallback_carve_count", value.FallbackCarveCount), Number("silent_repair_count", value.SilentRepairCount), Number("rotation_count", value.RotationCount) });
        private static string SceneManifestJson(MoonPalaceSeededRunResult active, IReadOnlyList<MoonPalaceSeededRunResult> gallery, string digest) => Json(new[] { Pair("scene_relative_path", SceneRelativePath), Pair("scene_root", SceneRootName), Pair("scene_manifest_digest", digest), Pair("active_recipe", active.Recipe.RecipeId), Number("active_seed", active.Seed), Number("gallery_course_count", gallery.Count), Pair("required_children", "ActivePreview;SeedGallery;PreviewPlayer;PreviewCamera;RouteGhost;RoomFrames;ConnectorGates;Labels;Metadata") });
        private static string DigestManifestJson(IDictionary<string, string> outputs, string sceneDigest, string digest) => JsonArrayWithFields(new[] { Pair("scene_manifest_digest", sceneDigest), Pair("digest_manifest_digest", digest) }, "artifacts", outputs.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => "{\"relative_path\":\"" + Escape(pair.Key) + "\",\"sha256\":\"" + BakingCanonicalDigest.HashCanonicalText(pair.Value) + "\"}"));
        private static string Json(IEnumerable<string> fields) { var values = fields.ToList(); var lines = new List<string> { "{" }; lines.AddRange(values.Select((value, index) => "  " + value + (index == values.Count - 1 ? string.Empty : ","))); lines.Add("}"); return Lines(lines); }
        private static string JsonArray(string name, IEnumerable<string> values) => JsonArrayWithFields(Array.Empty<string>(), name, values);
        private static string JsonArrayWithFields(IEnumerable<string> fields, string name, IEnumerable<string> values) { var prefix = fields.ToList(); var entries = values.ToList(); var lines = new List<string> { "{" }; lines.AddRange(prefix.Select(value => "  " + value + ",")); lines.Add("  \"" + Escape(name) + "\": ["); lines.AddRange(entries.Select((value, index) => "    " + value + (index == entries.Count - 1 ? string.Empty : ","))); lines.Add("  ]"); lines.Add("}"); return Lines(lines); }
        private static string Pair(string name, string value) => "\"" + Escape(name) + "\": \"" + Escape(value) + "\""; private static string Number(string name, int value) => "\"" + Escape(name) + "\": " + I(value); private static string Bool(string name, bool value) => "\"" + Escape(name) + "\": " + (value ? "true" : "false"); private static string Lines(IEnumerable<string> lines) => string.Join("\n", lines) + "\n"; private static string FinalLf(string value) => BakingCanonicalDigest.NormalizeLineEndingsToLf(value ?? string.Empty).TrimEnd('\n') + "\n"; private static string Csv(string value) { value = value ?? string.Empty; return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value; } private static string I(int value) => value.ToString(CultureInfo.InvariantCulture); private static string B(bool value) => value ? "TRUE" : "FALSE"; private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n"); private static string Join(string left, string right) => left.TrimEnd('/') + "/" + right.TrimStart('/'); private static string Resolve(string root, string relative) => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)); private static string DebugTilePath(string name) => Join(SceneDirectoryRelativePath, "RUN05_" + name + ".asset"); private static string Point(Vector2Int value) => value.x.ToString(CultureInfo.InvariantCulture) + ":" + value.y.ToString(CultureInfo.InvariantCulture);
        private sealed class TileDefinition { public TileDefinition(string name, Color color) { Name = name; Color = color; } public string Name { get; } public Color Color { get; } }
    }
}
