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
    public sealed class MoonPalaceRunVariantPublication
    {
        internal MoonPalaceRunVariantPublication(IEnumerable<MoonPalaceRunVariantConfig> configs, IEnumerable<RunVariantGraph> graphs,
            IEnumerable<MoonPalaceRunVariantComposition> compositions, MoonPalaceRunVariantAggregateValidation validation,
            IDictionary<string, string> outputContents, string sceneManifestDigest, string digestManifestDigest)
        {
            Configs = ReadOnly(configs); Graphs = ReadOnly(graphs); Compositions = ReadOnly(compositions); Validation = validation;
            OutputContents = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(outputContents, StringComparer.Ordinal));
            SceneManifestDigest = sceneManifestDigest ?? string.Empty; DigestManifestDigest = digestManifestDigest ?? string.Empty;
        }
        public IReadOnlyList<MoonPalaceRunVariantConfig> Configs { get; }
        public IReadOnlyList<RunVariantGraph> Graphs { get; }
        public IReadOnlyList<MoonPalaceRunVariantComposition> Compositions { get; }
        public MoonPalaceRunVariantAggregateValidation Validation { get; }
        public IReadOnlyDictionary<string, string> OutputContents { get; }
        public string SceneManifestDigest { get; }
        public string DigestManifestDigest { get; }
        private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) => new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).ToList());
    }

    /// <summary>RUN02-only publisher for the isolated comparison Scene of direct 4x4 MicroPattern run variants.</summary>
    public static class MoonPalaceRunVariantSceneBuilder
    {
        public const string SceneRelativePath = "Assets/_Game/Map/Scenes/MoonPalace/RUN02/MoonPalaceRunVariants_RUN02.unity";
        public const string SceneDirectoryRelativePath = "Assets/_Game/Map/Scenes/MoonPalace/RUN02";
        public const string AuthoringDirectoryRelativePath = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN02";
        public const string GeneratedDirectoryRelativePath = "MapDesign/MCP/GENERATED/RUN02";
        public const string SceneRootName = "MoonPalace_RunVariants_RUN02";

        private static readonly string[] CsvNames =
        {
            "moonpalace_run02_variants.csv", "moonpalace_run02_graph_nodes.csv", "moonpalace_run02_graph_edges.csv",
            "moonpalace_run02_pattern_slots.csv", "moonpalace_run02_tile_cells.csv", "moonpalace_run02_validation_summary.csv",
        };
        private static readonly string[] JsonNames =
        {
            "moonpalace_run02_config.json", "moonpalace_run02_graphs.json", "moonpalace_run02_compositions.json",
            "moonpalace_run02_validation.json", "moonpalace_run02_scene_manifest.json", "moonpalace_run02_digest_manifest.json",
        };
        private static readonly TileDefinition[] TileDefinitions =
        {
            new TileDefinition("Solid", new Color(0.10f, 0.12f, 0.16f, 1f)), new TileDefinition("Open", new Color(0.20f, 0.27f, 0.33f, 1f)),
            new TileDefinition("MainRoute", new Color(0.22f, 0.90f, 0.42f, 1f)), new TileDefinition("BranchRoute", new Color(0.16f, 0.73f, 0.95f, 1f)),
            new TileDefinition("SplitRoute", new Color(1.00f, 0.76f, 0.20f, 1f)), new TileDefinition("PatternBoundary", new Color(0.45f, 0.34f, 0.72f, 0.75f)),
            new TileDefinition("Socket", new Color(0.90f, 0.56f, 0.98f, 1f)), new TileDefinition("Start", new Color(0.40f, 1.00f, 0.40f, 1f)),
            new TileDefinition("Exit", new Color(1.00f, 0.38f, 0.40f, 1f)),
        };

        [MenuItem("MapDesign/MoonPalace/Build RUN02 MicroPattern Run Variant Comparison Scene")]
        public static void PublishFromMenu() => Publish(ProjectRoot);

        public static IReadOnlyList<string> GetExpectedOutputRelativePaths()
        {
            return new ReadOnlyCollection<string>(CsvNames.Select(name => Join(AuthoringDirectoryRelativePath, name))
                .Concat(JsonNames.Select(name => Join(GeneratedDirectoryRelativePath, name))).Concat(new[] { SceneRelativePath })
                .OrderBy(path => path, StringComparer.Ordinal).ToList());
        }

        public static IReadOnlyList<string> GetDebugTileAssetRelativePaths()
        {
            return new ReadOnlyCollection<string>(TileDefinitions.Select(definition => DebugTilePath(definition.Name)).OrderBy(path => path, StringComparer.Ordinal).ToList());
        }

        public static MoonPalaceRunVariantPublication BuildSnapshot(string projectRoot, bool reverseCandidateEnumeration)
        {
            if (string.IsNullOrWhiteSpace(projectRoot)) throw new ArgumentException("Project root is required.", nameof(projectRoot));
            var configs = MoonPalaceRunVariantConfig.CreateDefaults().ToList();
            var graphs = configs.Select(RunVariantGraph.Create).ToList();
            var compositions = graphs.Select(graph => MoonPalaceRunVariantComposer.Compose(graph.Config, graph, reverseCandidateEnumeration)).ToList();
            var validation = MoonPalaceRunVariantValidator.ValidateAll(compositions);
            if (!validation.Passed) throw new InvalidOperationException("RUN02 refuses to publish an invalid direct MicroPattern run-variant comparison.");
            var sceneManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "RUN02_SCENE_MANIFEST", SceneRelativePath, SceneRootName, validation.CanonicalDigest }
                .Concat(configs.Select(config => config.CanonicalDigest)).Concat(graphs.Select(graph => graph.Digest.Value)).Concat(compositions.Select(composition => composition.MapDigest)).Concat(GetDebugTileAssetRelativePaths()));
            var outputs = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                [Join(AuthoringDirectoryRelativePath, CsvNames[0])] = VariantsCsv(configs, graphs, compositions),
                [Join(AuthoringDirectoryRelativePath, CsvNames[1])] = GraphNodesCsv(graphs),
                [Join(AuthoringDirectoryRelativePath, CsvNames[2])] = GraphEdgesCsv(graphs),
                [Join(AuthoringDirectoryRelativePath, CsvNames[3])] = PatternSlotsCsv(compositions),
                [Join(AuthoringDirectoryRelativePath, CsvNames[4])] = TileCellsCsv(compositions),
                [Join(AuthoringDirectoryRelativePath, CsvNames[5])] = ValidationSummaryCsv(validation),
                [Join(GeneratedDirectoryRelativePath, JsonNames[0])] = ConfigJson(configs),
                [Join(GeneratedDirectoryRelativePath, JsonNames[1])] = GraphsJson(graphs),
                [Join(GeneratedDirectoryRelativePath, JsonNames[2])] = CompositionsJson(compositions),
                [Join(GeneratedDirectoryRelativePath, JsonNames[3])] = ValidationJson(validation),
                [Join(GeneratedDirectoryRelativePath, JsonNames[4])] = SceneManifestJson(configs, graphs, compositions, validation, sceneManifestDigest),
            };
            var artifactLines = outputs.Select(pair => pair.Key + "|" + BakingCanonicalDigest.HashCanonicalText(pair.Value)).ToArray();
            var digestManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "RUN02_DIGEST_MANIFEST", validation.CanonicalDigest, sceneManifestDigest }.Concat(artifactLines));
            outputs[Join(GeneratedDirectoryRelativePath, JsonNames[5])] = DigestManifestJson(configs, graphs, compositions, validation, sceneManifestDigest, digestManifestDigest, outputs);
            return new MoonPalaceRunVariantPublication(configs, graphs, compositions, validation, outputs, sceneManifestDigest, digestManifestDigest);
        }

        public static MoonPalaceRunVariantPublication Publish(string projectRoot)
        {
            var root = Path.GetFullPath(projectRoot); var publication = BuildSnapshot(root, false);
            Directory.CreateDirectory(Resolve(root, AuthoringDirectoryRelativePath)); Directory.CreateDirectory(Resolve(root, GeneratedDirectoryRelativePath)); Directory.CreateDirectory(Resolve(root, SceneDirectoryRelativePath));
            foreach (var output in publication.OutputContents) File.WriteAllText(Resolve(root, output.Key), FinalLf(output.Value), BakingCanonicalDigest.Utf8NoBomEncoding);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); BuildScene(publication); AssetDatabase.SaveAssets(); return publication;
        }

        private static void BuildScene(MoonPalaceRunVariantPublication publication)
        {
            var previous = SceneManager.GetActiveScene(); var replaceUntitled = previous.IsValid() && string.IsNullOrEmpty(previous.path);
            if (replaceUntitled && previous.isDirty) throw new InvalidOperationException("RUN02 refuses to replace a dirty untitled Scene.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, replaceUntitled ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene); var tiles = CreateDebugTiles(); var root = new GameObject(SceneRootName);
                var origins = new[] { new Vector2Int(0, 0), new Vector2Int(0, 78), new Vector2Int(0, 174) };
                for (var index = 0; index < publication.Compositions.Count; index++) CreateVariantVisual(root.transform, publication.Compositions[index], tiles, origins[index], index);
                CreateCamera(root.transform); CreateLegend(root.transform); CreateMetadata(root.transform, publication);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, SceneRelativePath, false)) throw new IOException("Unity did not save the isolated RUN02 comparison Scene.");
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

        private static void CreateVariantVisual(Transform parent, MoonPalaceRunVariantComposition composition, IDictionary<string, Tile> tiles, Vector2Int origin, int index)
        {
            var variantRoot = new GameObject("Variant_" + (index == 0 ? "A" : index == 1 ? "B" : "C")); variantRoot.transform.SetParent(parent, false); variantRoot.transform.localPosition = new Vector3(origin.x, origin.y, 0f);
            var grid = new GameObject("Grid", typeof(Grid)); grid.transform.SetParent(variantRoot.transform, false);
            var terrain = CreateTilemap("Terrain", grid.transform, 0); var routes = CreateTilemap("RouteBranch", grid.transform, 10); var annotations = CreateTilemap("PatternBoundarySocketMarker", grid.transform, 20);
            var mainTiles = new HashSet<MoonPalaceRunTileCoordinate>(composition.MainRouteTiles); var branchTiles = new HashSet<MoonPalaceRunTileCoordinate>(composition.BranchRouteTiles); var splitTiles = new HashSet<MoonPalaceRunTileCoordinate>(composition.SplitRejoinRouteTiles);
            var routeSlots = new HashSet<PatternSlotCoordinate>(composition.Graph.SocketRequirements.Select(requirement => requirement.Coordinate));
            for (var y = 0; y < composition.Config.TileHeight; y++)
            for (var x = 0; x < composition.Config.TileWidth; x++)
            {
                var position = new Vector3Int(x, y, 0); var tile = new MoonPalaceRunTileCoordinate(x, y);
                terrain.SetTile(position, composition.IsOpen(x, y) ? tiles["Open"] : tiles["Solid"]);
                if (mainTiles.Contains(tile) && composition.IsOpen(x, y)) routes.SetTile(position, tiles["MainRoute"]);
                else if (splitTiles.Contains(tile) && composition.IsOpen(x, y)) routes.SetTile(position, tiles["SplitRoute"]);
                else if (branchTiles.Contains(tile) && composition.IsOpen(x, y)) routes.SetTile(position, tiles["BranchRoute"]);
                var slot = new PatternSlotCoordinate(x / composition.Config.PatternWidth, y / composition.Config.PatternHeight);
                if (x % composition.Config.PatternWidth == 0 || y % composition.Config.PatternHeight == 0) annotations.SetTile(position, tiles["PatternBoundary"]);
                if (routeSlots.Contains(slot) && x % composition.Config.PatternWidth == 1 && y % composition.Config.PatternHeight == 1) annotations.SetTile(position, tiles["Socket"]);
            }
            annotations.SetTile(new Vector3Int(composition.StartTile.X, composition.StartTile.Y, 0), tiles["Start"]); annotations.SetTile(new Vector3Int(composition.ExitTile.X, composition.ExitTile.Y, 0), tiles["Exit"]);
            foreach (var tilemap in new[] { terrain, routes, annotations }) { tilemap.CompressBounds(); tilemap.RefreshAllTiles(); EditorUtility.SetDirty(tilemap); }
            if (terrain.cellBounds.size.x != composition.Config.TileWidth || terrain.cellBounds.size.y != composition.Config.TileHeight) throw new InvalidOperationException("RUN02 Terrain layer did not contain the complete pattern-grid canvas.");
            CreateMarker(variantRoot.transform, "Start", composition.StartTile, "START", new Color(0.55f, 1f, 0.55f, 1f)); CreateMarker(variantRoot.transform, "Exit", composition.ExitTile, "EXIT", new Color(1f, 0.55f, 0.55f, 1f));
            var label = new GameObject("Label"); label.transform.SetParent(variantRoot.transform, false); label.transform.localPosition = new Vector3(1f, composition.Config.TileHeight + 2f, -1f);
            var text = label.AddComponent<TextMesh>(); text.text = composition.Config.VariantId + " | " + composition.Config.TileWidth + "x" + composition.Config.TileHeight + " tiles | branches=" + composition.Graph.BranchCount + " | split-rejoin=" + composition.Graph.SplitRejoinCount + " | map=" + composition.MapDigest;
            text.color = Color.white; text.characterSize = 0.30f; text.fontSize = 20; text.anchor = TextAnchor.MiddleLeft;
        }

        private static Dictionary<string, Tile> CreateDebugTiles()
        {
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); if (sprite == null) throw new InvalidOperationException("Unity built-in UISprite is required for RUN02 debug tiles.");
            var result = new Dictionary<string, Tile>(StringComparer.Ordinal);
            foreach (var definition in TileDefinitions)
            {
                var path = DebugTilePath(definition.Name); var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); tile.name = "RUN02_" + definition.Name; AssetDatabase.CreateAsset(tile, path); }
                tile.name = "RUN02_" + definition.Name; tile.sprite = sprite; tile.color = definition.Color; tile.colliderType = Tile.ColliderType.None; EditorUtility.SetDirty(tile); result.Add(definition.Name, tile);
            }
            AssetDatabase.SaveAssets();
            foreach (var definition in TileDefinitions)
            {
                AssetDatabase.ImportAsset(DebugTilePath(definition.Name), ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(DebugTilePath(definition.Name)); if (tile == null || !EditorUtility.IsPersistent(tile)) throw new InvalidOperationException("RUN02 debug Tile import failed: " + definition.Name); result[definition.Name] = tile;
            }
            return result;
        }

        private static Tilemap CreateTilemap(string name, Transform parent, int sortingOrder)
        {
            var gameObject = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)); gameObject.transform.SetParent(parent, false);
            var renderer = gameObject.GetComponent<TilemapRenderer>(); renderer.sortingOrder = sortingOrder; renderer.mode = TilemapRenderer.Mode.Chunk; return gameObject.GetComponent<Tilemap>();
        }
        private static void CreateCamera(Transform parent)
        {
            var gameObject = new GameObject("RUN02_ComparisonCamera", typeof(Camera)); gameObject.transform.SetParent(parent, false); gameObject.transform.position = new Vector3(120f, 120f, -10f);
            var camera = gameObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 132f; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.045f, 0.052f, 0.070f, 1f); camera.nearClipPlane = 0.1f; camera.farClipPlane = 100f;
        }
        private static void CreateMarker(Transform parent, string name, MoonPalaceRunTileCoordinate tile, string textValue, Color color)
        {
            var marker = new GameObject(name); marker.transform.SetParent(parent, false); marker.transform.localPosition = new Vector3(tile.X + 0.5f, tile.Y + 1.2f, -1f);
            var text = marker.AddComponent<TextMesh>(); text.text = textValue; text.color = color; text.characterSize = 0.42f; text.fontSize = 30; text.anchor = TextAnchor.MiddleCenter;
        }
        private static void CreateLegend(Transform parent)
        {
            var legend = new GameObject("Legend"); legend.transform.SetParent(parent, false); legend.transform.localPosition = new Vector3(2f, -7f, -1f);
            var entries = new[] { "terrain: solid/open", "green: main", "blue: branch", "gold: split-rejoin", "purple: 4x4 boundary", "pink: socket", "start/exit" };
            for (var index = 0; index < entries.Length; index++)
            {
                var entry = new GameObject("Legend_" + index.ToString(CultureInfo.InvariantCulture)); entry.transform.SetParent(legend.transform, false); entry.transform.localPosition = new Vector3(index * 34f, 0f, 0f);
                var text = entry.AddComponent<TextMesh>(); text.text = entries[index]; text.color = Color.white; text.characterSize = 0.25f; text.fontSize = 18; text.anchor = TextAnchor.MiddleLeft;
            }
        }
        private static void CreateMetadata(Transform parent, MoonPalaceRunVariantPublication publication)
        {
            var metadata = new GameObject("Metadata"); metadata.transform.SetParent(parent, false); metadata.transform.localPosition = new Vector3(2f, 247f, -1f);
            var text = metadata.AddComponent<TextMesh>(); text.text = "RUN02 aggregate | candidates=500 | placements=" + publication.Validation.TotalPatternPlacements + " | graph=" + string.Join(";", publication.Graphs.Select(graph => graph.Digest.Value)) + " | validation=" + publication.Validation.CanonicalDigest;
            text.color = Color.white; text.characterSize = 0.24f; text.fontSize = 18; text.anchor = TextAnchor.MiddleLeft;
        }

        private static string VariantsCsv(IReadOnlyList<MoonPalaceRunVariantConfig> configs, IReadOnlyList<RunVariantGraph> graphs, IReadOnlyList<MoonPalaceRunVariantComposition> compositions)
        {
            var lines = new List<string> { "variant_id,seed_id,seed_value,pattern_width,pattern_height,pattern_grid_width,pattern_grid_height,tile_width,tile_height,main_path_steps,branch_count,split_rejoin_count,vertical_span_rows,room_region_count,config_digest,graph_digest,map_digest" };
            for (var index = 0; index < configs.Count; index++) lines.Add(string.Join(",", new[] { Csv(configs[index].VariantId), Csv(configs[index].SeedId), I(configs[index].SeedValue), "4", "4", I(configs[index].PatternGridWidth), I(configs[index].PatternGridHeight), I(configs[index].TileWidth), I(configs[index].TileHeight), I(graphs[index].MainPathNodes.Count), I(graphs[index].BranchCount), I(graphs[index].SplitRejoinCount), I(graphs[index].VerticalSpanRows), I(graphs[index].RoomRegions.Count), configs[index].CanonicalDigest, graphs[index].Digest.Value, compositions[index].MapDigest }));
            return Lines(lines);
        }
        private static string GraphNodesCsv(IEnumerable<RunVariantGraph> graphs)
        {
            var lines = new List<string> { "variant_id,node_kind,pattern_x,pattern_y,main_path,branch_index,room_region" };
            foreach (var graph in graphs) lines.AddRange(graph.Nodes.OrderBy(node => node.Coordinate.Y).ThenBy(node => node.Coordinate.X).Select(node => string.Join(",", new[] { Csv(graph.Config.VariantId), node.Kind, I(node.Coordinate.X), I(node.Coordinate.Y), B(node.IsMainPath), I(node.BranchIndex), Csv(node.RegionId) })));
            return Lines(lines);
        }
        private static string GraphEdgesCsv(IEnumerable<RunVariantGraph> graphs)
        {
            var lines = new List<string> { "variant_id,edge_index,from_x,from_y,to_x,to_y,direction,edge_kind,main_path,owner_index,required_socket_bit" };
            foreach (var graph in graphs) lines.AddRange(graph.Edges.Select((edge, index) => string.Join(",", new[] { Csv(graph.Config.VariantId), I(index), I(edge.From.X), I(edge.From.Y), I(edge.To.X), I(edge.To.Y), edge.Direction.ToString(), edge.Kind, B(edge.IsMainPath), I(edge.OwnerIndex), I(MoonPalaceRunVariantComposer.RouteSocketBit) })));
            return Lines(lines);
        }
        private static string PatternSlotsCsv(IEnumerable<MoonPalaceRunVariantComposition> compositions)
        {
            var lines = new List<string> { "variant_id,slot_index,pattern_x,pattern_y,candidate_id,mask_u16_hex,socket_signature,transform,selection_reason,attempt_count,main_route,branch_route,split_rejoin_route,map21_02_presentation_family_link" };
            foreach (var composition in compositions) lines.AddRange(composition.Placements.OrderBy(placement => placement.SlotIndex).Select(placement => string.Join(",", new[] { Csv(composition.Config.VariantId), I(placement.SlotIndex), I(placement.Coordinate.X), I(placement.Coordinate.Y), Csv(placement.Candidate.CandidateId), placement.Candidate.MaskU16Hex, Csv(placement.Candidate.SocketSignature), placement.Transform, Csv(placement.SelectionReason), I(placement.AttemptCount), B(placement.IsMainRoute), B(placement.IsBranchRoute), B(placement.IsSplitRejoinRoute), Csv(placement.PresentationFamilyLink) })));
            return Lines(lines);
        }
        private static string TileCellsCsv(IEnumerable<MoonPalaceRunVariantComposition> compositions)
        {
            var lines = new List<string> { "variant_id,x,y,pattern_x,pattern_y,is_open,main_route,branch_route,split_rejoin_route,start,exit" };
            foreach (var composition in compositions)
            {
                var main = new HashSet<MoonPalaceRunTileCoordinate>(composition.MainRouteTiles); var branch = new HashSet<MoonPalaceRunTileCoordinate>(composition.BranchRouteTiles); var split = new HashSet<MoonPalaceRunTileCoordinate>(composition.SplitRejoinRouteTiles);
                for (var y = 0; y < composition.Config.TileHeight; y++) for (var x = 0; x < composition.Config.TileWidth; x++)
                {
                    var tile = new MoonPalaceRunTileCoordinate(x, y); lines.Add(string.Join(",", new[] { Csv(composition.Config.VariantId), I(x), I(y), I(x / 4), I(y / 4), B(composition.IsOpen(x, y)), B(main.Contains(tile)), B(branch.Contains(tile)), B(split.Contains(tile)), B(tile.Equals(composition.StartTile)), B(tile.Equals(composition.ExitTile)) }));
                }
            }
            return Lines(lines);
        }
        private static string ValidationSummaryCsv(MoonPalaceRunVariantAggregateValidation validation)
        {
            var lines = new List<string> { "variant_id,tile_width,tile_height,pattern_grid_width,pattern_grid_height,placements,start_open,exit_open,start_to_exit_reachable,branch_entries,reachable_branch_entries,split_rejoin_waypoints,reachable_split_rejoin_waypoints,route_socket_mismatches,out_of_bounds_placements,duplicate_placements,unreachable_route_branch_islands,route_failures,fallback_carves,silent_repairs,passed,validation_digest" };
            lines.AddRange(validation.Variants.Select(result => string.Join(",", new[] { Csv(result.VariantId), I(result.TileWidth), I(result.TileHeight), I(result.PatternGridWidth), I(result.PatternGridHeight), I(result.PatternPlacementCount), B(result.StartOpen), B(result.ExitOpen), B(result.StartToExitReachable), I(result.BranchEntryCount), I(result.ReachableBranchEntryCount), I(result.SplitRejoinWaypointCount), I(result.ReachableSplitRejoinWaypointCount), I(result.RouteSocketMismatchCount), I(result.OutOfBoundsPlacementCount), I(result.DuplicatePlacementCount), I(result.UnreachableRouteBranchIslandCount), I(result.RouteFailureCount), I(result.FallbackCarveCount), I(result.SilentRepairCount), B(result.Passed), result.CanonicalDigest })));
            lines.Add("AGGREGATE,,,,," + I(validation.TotalPatternPlacements) + ",,,,,,,,," + I(validation.TotalRouteSocketMismatchCount) + ",,,,," + I(validation.TotalFallbackCarveCount) + "," + I(validation.TotalSilentRepairCount) + "," + B(validation.Passed) + "," + validation.CanonicalDigest);
            return Lines(lines);
        }

        private static string ConfigJson(IEnumerable<MoonPalaceRunVariantConfig> configs) => JsonArray("variants", configs.Select(config => "{\"variant_id\":\"" + Escape(config.VariantId) + "\",\"seed_id\":\"" + Escape(config.SeedId) + "\",\"seed_value\":" + I(config.SeedValue) + ",\"pattern_size\":\"4x4\",\"pattern_grid_size\":\"" + config.PatternGridWidth + "x" + config.PatternGridHeight + "\",\"tile_size\":\"" + config.TileWidth + "x" + config.TileHeight + "\",\"allow_90_degree_rotation\":false,\"allow_silent_carve\":false,\"config_digest\":\"" + config.CanonicalDigest + "\"}"));
        private static string GraphsJson(IEnumerable<RunVariantGraph> graphs) => JsonArray("graphs", graphs.Select(graph => "{\"variant_id\":\"" + Escape(graph.Config.VariantId) + "\",\"main_path_steps\":" + I(graph.MainPathNodes.Count) + ",\"branch_count\":" + I(graph.BranchCount) + ",\"split_rejoin_count\":" + I(graph.SplitRejoinCount) + ",\"vertical_span_rows\":" + I(graph.VerticalSpanRows) + ",\"room_region_count\":" + I(graph.RoomRegions.Count) + ",\"graph_digest\":\"" + graph.Digest.Value + "\"}"));
        private static string CompositionsJson(IEnumerable<MoonPalaceRunVariantComposition> compositions)
        {
            var lines = new List<string> { "{", "  \"compositions\": [" }; var list = compositions.ToList();
            for (var index = 0; index < list.Count; index++)
            {
                var composition = list[index]; lines.Add("    {\"variant_id\":\"" + Escape(composition.Config.VariantId) + "\",\"placement_count\":" + I(composition.PlacementCount) + ",\"main_route_placements\":" + I(composition.MainRoutePlacementCount) + ",\"branch_placements\":" + I(composition.BranchPlacementCount) + ",\"split_rejoin_placements\":" + I(composition.SplitRejoinPlacementCount) + ",\"filler_detail_placements\":" + I(composition.FillerDetailPlacementCount) + ",\"candidate_pool_count\":500,\"composition_digest\":\"" + composition.CompositionDigest + "\",\"map_digest\":\"" + composition.MapDigest + "\",\"placements\":[");
                for (var slot = 0; slot < composition.Placements.Count; slot++)
                {
                    var placement = composition.Placements[slot]; lines.Add("      {\"slot\":" + I(placement.SlotIndex) + ",\"coordinate\":\"" + placement.Coordinate + "\",\"candidate_id\":\"" + Escape(placement.Candidate.CandidateId) + "\",\"mask_u16_hex\":\"" + placement.Candidate.MaskU16Hex + "\",\"socket_signature\":\"" + Escape(placement.Candidate.SocketSignature) + "\",\"transform\":\"" + placement.Transform + "\",\"selection_reason\":\"" + Escape(placement.SelectionReason) + "\",\"attempt_count\":" + I(placement.AttemptCount) + "}" + (slot + 1 == composition.Placements.Count ? string.Empty : ","));
                }
                lines.Add("    ]}" + (index + 1 == list.Count ? string.Empty : ","));
            }
            lines.Add("  ]"); lines.Add("}"); return Lines(lines);
        }
        private static string ValidationJson(MoonPalaceRunVariantAggregateValidation validation) => JsonArray("variants", validation.Variants.Select(result => "{\"variant_id\":\"" + Escape(result.VariantId) + "\",\"tile_level_bfs_start_to_exit\":" + (result.StartToExitReachable ? "true" : "false") + ",\"branch_entries_reachable\":\"" + I(result.ReachableBranchEntryCount) + "/" + I(result.BranchEntryCount) + "\",\"split_rejoin_waypoints_reachable\":\"" + I(result.ReachableSplitRejoinWaypointCount) + "/" + I(result.SplitRejoinWaypointCount) + "\",\"route_socket_mismatch_count\":" + I(result.RouteSocketMismatchCount) + ",\"route_failure_count\":" + I(result.RouteFailureCount) + ",\"fallback_carve_count\":0,\"silent_repair_count\":0,\"validation_digest\":\"" + result.CanonicalDigest + "\"}"), "aggregate_validation_digest", validation.CanonicalDigest);
        private static string SceneManifestJson(IEnumerable<MoonPalaceRunVariantConfig> configs, IEnumerable<RunVariantGraph> graphs, IEnumerable<MoonPalaceRunVariantComposition> compositions, MoonPalaceRunVariantAggregateValidation validation, string digest) => Json(new[] { Pair("scene_path", SceneRelativePath), Pair("scene_root", SceneRootName), Number("generated_scene_count", 1), Number("variant_count", 3), Number("candidate_pool_accepted_count", 500), Number("total_pattern_placements", validation.TotalPatternPlacements), Pair("variant_ids", string.Join(";", configs.Select(config => config.VariantId))), Pair("graph_digests", string.Join(";", graphs.Select(graph => graph.Digest.Value))), Pair("map_digests", string.Join(";", compositions.Select(composition => composition.MapDigest))), Pair("validation_digest", validation.CanonicalDigest), Pair("scene_manifest_digest", digest), Pair("debug_tile_assets", string.Join(";", GetDebugTileAssetRelativePaths())) });
        private static string DigestManifestJson(IEnumerable<MoonPalaceRunVariantConfig> configs, IEnumerable<RunVariantGraph> graphs, IEnumerable<MoonPalaceRunVariantComposition> compositions, MoonPalaceRunVariantAggregateValidation validation, string sceneDigest, string digest, IEnumerable<KeyValuePair<string, string>> outputs)
        {
            var lines = new List<string> { "{", "  \"config_digests\": \"" + string.Join(";", configs.Select(config => config.CanonicalDigest)) + "\",", "  \"graph_digests\": \"" + string.Join(";", graphs.Select(graph => graph.Digest.Value)) + "\",", "  \"composition_digests\": \"" + string.Join(";", compositions.Select(composition => composition.CompositionDigest)) + "\",", "  \"validation_digest\": \"" + validation.CanonicalDigest + "\",", "  \"scene_manifest_digest\": \"" + sceneDigest + "\",", "  \"digest_manifest_digest\": \"" + digest + "\",", "  \"artifacts\": [" };
            var entries = outputs.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToList();
            for (var index = 0; index < entries.Count; index++) lines.Add("    {\"relative_path\":\"" + Escape(entries[index].Key) + "\",\"sha256\":\"" + BakingCanonicalDigest.HashCanonicalText(entries[index].Value) + "\"}" + (index + 1 == entries.Count ? string.Empty : ","));
            lines.Add("  ]"); lines.Add("}"); return Lines(lines);
        }

        private static string JsonArray(string name, IEnumerable<string> entries, string extraName = null, string extraValue = null)
        {
            var values = entries.ToList(); var lines = new List<string> { "{", "  \"" + Escape(name) + "\": [" };
            for (var index = 0; index < values.Count; index++) lines.Add("    " + values[index] + (index + 1 == values.Count ? string.Empty : ","));
            lines.Add(extraName == null ? "  ]" : "  ],"); if (extraName != null) lines.Add("  \"" + Escape(extraName) + "\": \"" + Escape(extraValue) + "\""); lines.Add("}"); return Lines(lines);
        }
        private static string Json(IEnumerable<string> fields) => "{\n" + string.Join(",\n", fields.Select(field => "  " + field)) + "\n}\n";
        private static string Pair(string name, string value) => "\"" + Escape(name) + "\": \"" + Escape(value) + "\"";
        private static string Number(string name, int value) => "\"" + Escape(name) + "\": " + I(value);
        private static string Lines(IEnumerable<string> lines) => string.Join("\n", lines) + "\n";
        private static string Csv(string value) { value = value ?? string.Empty; return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value; }
        private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string B(bool value) => value ? "TRUE" : "FALSE";
        private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        private static string FinalLf(string value) => BakingCanonicalDigest.NormalizeLineEndingsToLf(value ?? string.Empty).TrimEnd('\n') + "\n";
        private static string Join(string left, string right) => left.TrimEnd('/') + "/" + right.TrimStart('/');
        private static string Resolve(string root, string relative) => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        private static string DebugTilePath(string name) => Join(SceneDirectoryRelativePath, "RUN02_" + name + ".asset");
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private sealed class TileDefinition { public TileDefinition(string name, Color color) { Name = name; Color = color; } public string Name { get; } public Color Color { get; } }
    }
}
