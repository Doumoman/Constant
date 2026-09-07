using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.RunGeneration
{
    public sealed class MoonPalaceMicroPatternRunPublication
    {
        internal MoonPalaceMicroPatternRunPublication(MoonPalaceMicroPatternRunConfig config, MoonPalaceMicroPatternRunGraph graph,
            MoonPalaceMicroPatternRunComposition composition, MoonPalaceMicroPatternRunValidation validation,
            IDictionary<string, string> outputContents, string sceneManifestDigest, string digestManifestDigest)
        {
            Config = config; Graph = graph; Composition = composition; Validation = validation;
            OutputContents = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(outputContents, StringComparer.Ordinal));
            SceneManifestDigest = sceneManifestDigest; DigestManifestDigest = digestManifestDigest;
        }
        public MoonPalaceMicroPatternRunConfig Config { get; }
        public MoonPalaceMicroPatternRunGraph Graph { get; }
        public MoonPalaceMicroPatternRunComposition Composition { get; }
        public MoonPalaceMicroPatternRunValidation Validation { get; }
        public IReadOnlyDictionary<string, string> OutputContents { get; }
        public string SceneManifestDigest { get; }
        public string DigestManifestDigest { get; }
    }

    /// <summary>RUN01-only publication boundary for the direct 4x4 MicroPattern reachable run Scene.</summary>
    public static class MoonPalaceMicroPatternRunSceneBuilder
    {
        public const string SceneRelativePath = "Assets/_Game/Map/Scenes/MoonPalace/RUN01/MoonPalaceMicroPatternRun_RUN01.unity";
        public const string SceneDirectoryRelativePath = "Assets/_Game/Map/Scenes/MoonPalace/RUN01";
        public const string AuthoringDirectoryRelativePath = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN01";
        public const string GeneratedDirectoryRelativePath = "MapDesign/MCP/GENERATED/RUN01";
        public const string SceneRootName = "MoonPalace_MicroPatternRun_RUN01";

        private static readonly string[] CsvNames =
        {
            "moonpalace_run01_pattern_slots.csv", "moonpalace_run01_route_edges.csv", "moonpalace_run01_tile_cells.csv",
            "moonpalace_run01_validation_summary.csv", "moonpalace_run01_selection_manifest.csv",
        };
        private static readonly string[] JsonNames =
        {
            "moonpalace_run01_config.json", "moonpalace_run01_graph.json", "moonpalace_run01_composition.json",
            "moonpalace_run01_validation.json", "moonpalace_run01_scene_manifest.json", "moonpalace_run01_digest_manifest.json",
        };
        private static readonly TileDefinition[] TileDefinitions =
        {
            new TileDefinition("Solid", new Color(0.10f, 0.12f, 0.16f, 1f)),
            new TileDefinition("Open", new Color(0.20f, 0.27f, 0.33f, 1f)),
            new TileDefinition("MainRoute", new Color(0.22f, 0.90f, 0.42f, 1f)),
            new TileDefinition("BranchRoute", new Color(0.16f, 0.73f, 0.95f, 1f)),
            new TileDefinition("PatternBoundary", new Color(0.39f, 0.31f, 0.66f, 0.74f)),
            new TileDefinition("Socket", new Color(1.00f, 0.76f, 0.20f, 1f)),
            new TileDefinition("Start", new Color(0.40f, 1.00f, 0.40f, 1f)),
            new TileDefinition("Exit", new Color(1.00f, 0.38f, 0.40f, 1f)),
            new TileDefinition("Protection", new Color(0.71f, 0.40f, 0.96f, 0.72f)),
        };

        [MenuItem("MapDesign/MoonPalace/Build RUN01 MicroPattern Reachable Run Scene")]
        public static void PublishFromMenu() => Publish(ProjectRoot);

        public static IReadOnlyList<string> GetExpectedOutputRelativePaths()
        {
            return new ReadOnlyCollection<string>(CsvNames.Select(name => Join(AuthoringDirectoryRelativePath, name))
                .Concat(JsonNames.Select(name => Join(GeneratedDirectoryRelativePath, name))).Concat(new[] { SceneRelativePath })
                .OrderBy(path => path, StringComparer.Ordinal).ToList());
        }

        public static IReadOnlyList<string> GetDebugTileAssetRelativePaths()
        {
            return new ReadOnlyCollection<string>(TileDefinitions.Select(definition => DebugTilePath(definition.Name))
                .OrderBy(path => path, StringComparer.Ordinal).ToList());
        }

        public static MoonPalaceMicroPatternRunPublication BuildSnapshot(string projectRoot, bool reverseCandidateEnumeration)
        {
            if (string.IsNullOrWhiteSpace(projectRoot)) throw new ArgumentException("Project root is required.", nameof(projectRoot));
            var config = MoonPalaceMicroPatternRunConfig.CreateDefault();
            var graph = MoonPalaceMicroPatternRunGraph.Create(config);
            var composition = MoonPalaceMicroPatternRunComposer.Compose(config, graph, reverseCandidateEnumeration);
            var validation = MoonPalaceMicroPatternRunValidator.Validate(composition);
            if (!validation.Passed) throw new InvalidOperationException("RUN01 refuses to publish an invalid direct MicroPattern run.");
            var sceneManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "RUN01_SCENE_MANIFEST", SceneRelativePath, SceneRootName, config.CanonicalDigest, graph.Digest.Value,
                composition.CompositionDigest, composition.MapDigest, validation.CanonicalDigest,
            }.Concat(GetDebugTileAssetRelativePaths()));
            var outputs = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                [Join(AuthoringDirectoryRelativePath, CsvNames[0])] = PatternSlotsCsv(composition),
                [Join(AuthoringDirectoryRelativePath, CsvNames[1])] = RouteEdgesCsv(graph),
                [Join(AuthoringDirectoryRelativePath, CsvNames[2])] = TileCellsCsv(composition),
                [Join(AuthoringDirectoryRelativePath, CsvNames[3])] = ValidationSummaryCsv(validation),
                [Join(AuthoringDirectoryRelativePath, CsvNames[4])] = SelectionManifestCsv(composition),
                [Join(GeneratedDirectoryRelativePath, JsonNames[0])] = ConfigJson(config),
                [Join(GeneratedDirectoryRelativePath, JsonNames[1])] = GraphJson(graph),
                [Join(GeneratedDirectoryRelativePath, JsonNames[2])] = CompositionJson(composition),
                [Join(GeneratedDirectoryRelativePath, JsonNames[3])] = ValidationJson(validation),
                [Join(GeneratedDirectoryRelativePath, JsonNames[4])] = SceneManifestJson(config, graph, composition, validation, sceneManifestDigest),
            };
            var artifactLines = outputs.Select(pair => pair.Key + "|" + BakingCanonicalDigest.HashCanonicalText(pair.Value)).ToArray();
            var digestManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "RUN01_DIGEST_MANIFEST", config.CanonicalDigest, graph.Digest.Value, composition.CompositionDigest,
                composition.MapDigest, validation.CanonicalDigest, sceneManifestDigest,
            }.Concat(artifactLines));
            outputs[Join(GeneratedDirectoryRelativePath, JsonNames[5])] = DigestManifestJson(config, graph, composition, validation,
                sceneManifestDigest, digestManifestDigest, outputs);
            return new MoonPalaceMicroPatternRunPublication(config, graph, composition, validation, outputs, sceneManifestDigest, digestManifestDigest);
        }

        public static MoonPalaceMicroPatternRunPublication Publish(string projectRoot)
        {
            var root = Path.GetFullPath(projectRoot);
            var publication = BuildSnapshot(root, false);
            Directory.CreateDirectory(Resolve(root, AuthoringDirectoryRelativePath));
            Directory.CreateDirectory(Resolve(root, GeneratedDirectoryRelativePath));
            Directory.CreateDirectory(Resolve(root, SceneDirectoryRelativePath));
            foreach (var output in publication.OutputContents)
                File.WriteAllText(Resolve(root, output.Key), FinalLf(output.Value), BakingCanonicalDigest.Utf8NoBomEncoding);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuildScene(publication);
            AssetDatabase.SaveAssets();
            return publication;
        }

        private static void BuildScene(MoonPalaceMicroPatternRunPublication publication)
        {
            var previous = SceneManager.GetActiveScene();
            var replaceUntitled = previous.IsValid() && string.IsNullOrEmpty(previous.path);
            if (replaceUntitled && previous.isDirty)
                throw new InvalidOperationException("RUN01 refuses to replace a dirty untitled Scene.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, replaceUntitled ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var tiles = CreateDebugTiles();
                var root = new GameObject(SceneRootName);
                var gridObject = new GameObject("Grid", typeof(Grid));
                gridObject.transform.SetParent(root.transform, false);
                var terrain = CreateTilemap("Terrain_Solid_Open", gridObject.transform, 0);
                var routes = CreateTilemap("Route_Main_Branches", gridObject.transform, 10);
                var annotations = CreateTilemap("Pattern_Boundaries_Sockets_Protection", gridObject.transform, 20);
                var mainTiles = new HashSet<MoonPalaceRunTileCoordinate>(publication.Composition.MainRouteTiles);
                var branchTiles = new HashSet<MoonPalaceRunTileCoordinate>(publication.Composition.BranchRouteTiles);
                var branchSlots = new HashSet<PatternSlotCoordinate>(publication.Composition.Placements.Where(placement => placement.IsBranch)
                    .Select(placement => placement.Coordinate));
                var routeSlots = new HashSet<PatternSlotCoordinate>(publication.Composition.Placements.Where(placement => placement.IsMainPath || placement.IsBranch)
                    .Select(placement => placement.Coordinate));
                for (var y = 0; y < publication.Config.TileHeight; y++)
                for (var x = 0; x < publication.Config.TileWidth; x++)
                {
                    var position = new Vector3Int(x, y, 0);
                    var tile = new MoonPalaceRunTileCoordinate(x, y);
                    terrain.SetTile(position, publication.Composition.IsOpen(x, y) ? tiles["Open"] : tiles["Solid"]);
                    if (mainTiles.Contains(tile) && publication.Composition.IsOpen(x, y)) routes.SetTile(position, tiles["MainRoute"]);
                    else if (branchTiles.Contains(tile) && publication.Composition.IsOpen(x, y)) routes.SetTile(position, tiles["BranchRoute"]);
                    var slot = new PatternSlotCoordinate(x / publication.Config.PatternWidth, y / publication.Config.PatternHeight);
                    if (x % publication.Config.PatternWidth == 0 || y % publication.Config.PatternHeight == 0) annotations.SetTile(position, tiles["PatternBoundary"]);
                    if (routeSlots.Contains(slot) && (x % publication.Config.PatternWidth == 1) && (y % publication.Config.PatternHeight == 1))
                        annotations.SetTile(position, branchSlots.Contains(slot) ? tiles["Protection"] : tiles["Socket"]);
                }
                annotations.SetTile(new Vector3Int(publication.Composition.StartTile.X, publication.Composition.StartTile.Y, 0), tiles["Start"]);
                annotations.SetTile(new Vector3Int(publication.Composition.ExitTile.X, publication.Composition.ExitTile.Y, 0), tiles["Exit"]);
                foreach (var tilemap in new[] { terrain, routes, annotations })
                {
                    tilemap.CompressBounds(); tilemap.RefreshAllTiles(); EditorUtility.SetDirty(tilemap);
                }
                if (terrain.cellBounds.size.x != publication.Config.TileWidth || terrain.cellBounds.size.y != publication.Config.TileHeight)
                    throw new InvalidOperationException("RUN01 terrain Tilemap does not contain the complete direct pattern-grid canvas.");
                CreateCamera(root.transform, publication.Config);
                CreateMarker(root.transform, "StartMarker", publication.Composition.StartTile, "START", new Color(0.55f, 1f, 0.55f, 1f));
                CreateMarker(root.transform, "ExitMarker", publication.Composition.ExitTile, "EXIT", new Color(1f, 0.55f, 0.55f, 1f));
                CreateLegend(root.transform, publication.Config);
                CreateMetadata(root.transform, publication);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, SceneRelativePath, false))
                    throw new IOException("Unity did not save the isolated RUN01 Scene.");
            }
            finally
            {
                if (replaceUntitled)
                {
                    if (scene.IsValid() && scene.isLoaded && !scene.isDirty)
                        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
                else
                {
                    if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                    if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                }
            }
        }

        private static Dictionary<string, Tile> CreateDebugTiles()
        {
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (sprite == null) throw new InvalidOperationException("Unity built-in UISprite is required for RUN01 debug tiles.");
            var result = new Dictionary<string, Tile>(StringComparer.Ordinal);
            foreach (var definition in TileDefinitions)
            {
                var path = DebugTilePath(definition.Name);
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    tile.name = "RUN01_" + definition.Name;
                    AssetDatabase.CreateAsset(tile, path);
                }
                tile.name = "RUN01_" + definition.Name;
                tile.sprite = sprite; tile.color = definition.Color; tile.colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile); result.Add(definition.Name, tile);
            }
            AssetDatabase.SaveAssets();
            foreach (var definition in TileDefinitions)
            {
                AssetDatabase.ImportAsset(DebugTilePath(definition.Name), ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(DebugTilePath(definition.Name));
                if (tile == null || !EditorUtility.IsPersistent(tile)) throw new InvalidOperationException("RUN01 debug Tile import failed: " + definition.Name);
                result[definition.Name] = tile;
            }
            return result;
        }

        private static Tilemap CreateTilemap(string name, Transform parent, int sortingOrder)
        {
            var gameObject = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            gameObject.transform.SetParent(parent, false);
            var renderer = gameObject.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder; renderer.mode = TilemapRenderer.Mode.Chunk;
            return gameObject.GetComponent<Tilemap>();
        }

        private static void CreateCamera(Transform parent, MoonPalaceMicroPatternRunConfig config)
        {
            var cameraObject = new GameObject("RUN01_FullRunCamera", typeof(Camera));
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = new Vector3(config.TileWidth * 0.5f, config.TileHeight * 0.5f, -10f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 52f; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.045f, 0.052f, 0.070f, 1f); camera.nearClipPlane = 0.1f; camera.farClipPlane = 100f;
        }

        private static void CreateMarker(Transform parent, string name, MoonPalaceRunTileCoordinate tile, string label, Color color)
        {
            var marker = new GameObject(name); marker.transform.SetParent(parent, false);
            marker.transform.position = new Vector3(tile.X + 0.5f, tile.Y + 1.2f, -1f);
            var text = marker.AddComponent<TextMesh>(); text.text = label; text.color = color; text.characterSize = 0.50f;
            text.fontSize = 32; text.anchor = TextAnchor.MiddleCenter;
        }

        private static void CreateLegend(Transform parent, MoonPalaceMicroPatternRunConfig config)
        {
            var legend = new GameObject("Legend"); legend.transform.SetParent(parent, false);
            var entries = new[] { "Solid / open", "Main route", "Branch route", "4x4 boundary", "Socket / protection", "Start / exit" };
            for (var index = 0; index < entries.Length; index++)
            {
                var entry = new GameObject("Legend_" + index.ToString(CultureInfo.InvariantCulture)); entry.transform.SetParent(legend.transform, false);
                entry.transform.position = new Vector3(2f + (index * 26f), config.TileHeight - 1.4f, -1f);
                var text = entry.AddComponent<TextMesh>(); text.text = entries[index]; text.color = Color.white; text.characterSize = 0.27f;
                text.fontSize = 20; text.anchor = TextAnchor.MiddleLeft;
            }
        }

        private static void CreateMetadata(Transform parent, MoonPalaceMicroPatternRunPublication publication)
        {
            var metadata = new GameObject("Metadata"); metadata.transform.SetParent(parent, false);
            metadata.transform.position = new Vector3(2f, -2f, -1f);
            var text = metadata.AddComponent<TextMesh>();
            text.text = "run_id=" + publication.Config.RunId + " | seed=" + publication.Config.SeedId + "/" +
                publication.Config.SeedValue.ToString(CultureInfo.InvariantCulture) + " | grid=" + publication.Config.PatternGridWidth + "x" +
                publication.Config.PatternGridHeight + " patterns | tiles=" + publication.Config.TileWidth + "x" + publication.Config.TileHeight +
                " | graph=" + publication.Graph.Digest.Value + " | map=" + publication.Composition.MapDigest;
            text.color = Color.white; text.characterSize = 0.25f; text.fontSize = 18; text.anchor = TextAnchor.MiddleLeft;
        }

        private static string PatternSlotsCsv(MoonPalaceMicroPatternRunComposition composition)
        {
            var lines = new List<string> { "slot_index,pattern_x,pattern_y,candidate_id,mask_u16_hex,socket_signature,transform,selection_reason,attempt_count,main_path,branch_index,map21_02_presentation_family_link" };
            lines.AddRange(composition.Placements.OrderBy(placement => placement.SlotIndex).Select(placement => string.Join(",", new[]
            {
                I(placement.SlotIndex), I(placement.Coordinate.X), I(placement.Coordinate.Y), Csv(placement.Candidate.CandidateId), placement.Candidate.MaskU16Hex,
                Csv(placement.Candidate.SocketSignature), placement.Transform, Csv(placement.SelectionReason), I(placement.AttemptCount), B(placement.IsMainPath),
                I(placement.BranchIndex), Csv(placement.PresentationFamilyLink),
            })));
            return Lines(lines);
        }

        private static string RouteEdgesCsv(MoonPalaceMicroPatternRunGraph graph)
        {
            var lines = new List<string> { "edge_index,from_x,from_y,to_x,to_y,direction,main_path,branch_index,required_socket_bit" };
            lines.AddRange(graph.Edges.Select((edge, index) => string.Join(",", new[] { I(index), I(edge.From.X), I(edge.From.Y), I(edge.To.X), I(edge.To.Y),
                edge.Direction.ToString(), B(edge.IsMainPath), I(edge.BranchIndex), I(MoonPalaceMicroPatternRunComposer.RouteSocketBit) })));
            return Lines(lines);
        }

        private static string TileCellsCsv(MoonPalaceMicroPatternRunComposition composition)
        {
            var main = new HashSet<MoonPalaceRunTileCoordinate>(composition.MainRouteTiles);
            var branch = new HashSet<MoonPalaceRunTileCoordinate>(composition.BranchRouteTiles);
            var lines = new List<string> { "x,y,pattern_x,pattern_y,is_open,main_route,branch_route,start,exit" };
            for (var y = 0; y < composition.Config.TileHeight; y++)
            for (var x = 0; x < composition.Config.TileWidth; x++)
            {
                var tile = new MoonPalaceRunTileCoordinate(x, y);
                lines.Add(string.Join(",", new[] { I(x), I(y), I(x / composition.Config.PatternWidth), I(y / composition.Config.PatternHeight),
                    B(composition.IsOpen(x, y)), B(main.Contains(tile)), B(branch.Contains(tile)), B(tile.Equals(composition.StartTile)), B(tile.Equals(composition.ExitTile)) }));
            }
            return Lines(lines);
        }

        private static string ValidationSummaryCsv(MoonPalaceMicroPatternRunValidation validation)
        {
            return Lines(new[]
            {
                "tile_width,tile_height,pattern_grid_width,pattern_grid_height,placements,start_tile,exit_tile,start_open,exit_open,start_to_exit_reachable,required_waypoints,reachable_required_waypoints,branch_entries,reachable_branch_entries,route_socket_mismatches,out_of_bounds_placements,duplicate_placements,unreachable_open_islands,route_failures,fallback_carves,silent_repairs,passed,validation_digest",
                string.Join(",", new[] { I(validation.TileWidth), I(validation.TileHeight), I(validation.PatternGridWidth), I(validation.PatternGridHeight), I(validation.PatternPlacementCount),
                    validation.StartTile.ToString(), validation.ExitTile.ToString(), B(validation.StartOpen), B(validation.ExitOpen), B(validation.StartToExitReachable),
                    I(validation.RequiredWaypointCount), I(validation.ReachableRequiredWaypointCount), I(validation.BranchEntryCount), I(validation.ReachableBranchEntryCount),
                    I(validation.RouteSocketMismatchCount), I(validation.OutOfBoundsPlacementCount), I(validation.DuplicatePlacementCount), I(validation.UnreachableOpenIslandCount),
                    I(validation.RouteFailureCount), I(validation.FallbackCarveCount), I(validation.SilentRepairCount), B(validation.Passed), validation.CanonicalDigest }),
            });
        }

        private static string SelectionManifestCsv(MoonPalaceMicroPatternRunComposition composition)
        {
            var lines = new List<string> { "kind,id,source,owner,detail" };
            lines.Add("CANDIDATE_POOL,500,VIS01_MicroPatternCandidateLibrary,RUN01," + Csv(composition.CandidateSet.CanonicalDigest));
            lines.AddRange(composition.Placements.OrderBy(placement => placement.SlotIndex).Select(placement => "PLACEMENT," + Csv(placement.Candidate.CandidateId) +
                ",VIS01_500_CANDIDATE_POOL,RUN01_SLOT_" + I(placement.SlotIndex) + "," + Csv(placement.SelectionReason)));
            lines.AddRange(composition.RejectionCounts.Select(pair => "REJECTION_COUNT," + Csv(pair.Key) + ",RUN01_COMPOSER,RUN01," + I(pair.Value)));
            return Lines(lines);
        }

        private static string ConfigJson(MoonPalaceMicroPatternRunConfig config)
        {
            return Json(new[] { Pair("run_id", config.RunId), Pair("seed_id", config.SeedId), Number("seed_value", config.SeedValue),
                Pair("pattern_size", config.PatternWidth + "x" + config.PatternHeight), Pair("pattern_grid_size", config.PatternGridWidth + "x" + config.PatternGridHeight),
                Pair("tile_size", config.TileWidth + "x" + config.TileHeight), Number("candidate_count", config.CandidateCount),
                Pair("main_path_steps", config.MainPathMinSteps + ".." + config.MainPathMaxSteps), Pair("branch_count", config.BranchMinCount + ".." + config.BranchMaxCount),
                Bool("allow_90_degree_rotation", config.Allow90DegreeRotation), Bool("allow_silent_carve", config.AllowSilentCarve), Pair("config_digest", config.CanonicalDigest) });
        }

        private static string GraphJson(MoonPalaceMicroPatternRunGraph graph)
        {
            var lines = new List<string> { "{", "  \"graph_digest\": \"" + Escape(graph.Digest.Value) + "\",", "  \"main_path_pattern_steps\": " + graph.MainPathNodes.Count + ",", "  \"branch_count\": " + graph.Branches.Count + ",", "  \"start_slot\": \"" + graph.Start.Slot + "\",", "  \"exit_slot\": \"" + graph.Exit.Slot + "\",", "  \"edges\": [" };
            for (var index = 0; index < graph.Edges.Count; index++)
            {
                var edge = graph.Edges[index]; var suffix = index + 1 == graph.Edges.Count ? string.Empty : ",";
                lines.Add("    {\"from\":\"" + edge.From + "\",\"to\":\"" + edge.To + "\",\"direction\":\"" + edge.Direction + "\",\"main_path\":" + (edge.IsMainPath ? "true" : "false") + ",\"branch_index\":" + edge.BranchIndex + "}" + suffix);
            }
            lines.Add("  ]"); lines.Add("}"); return Lines(lines);
        }

        private static string CompositionJson(MoonPalaceMicroPatternRunComposition composition)
        {
            var lines = new List<string> { "{", "  \"composition_digest\": \"" + composition.CompositionDigest + "\",", "  \"map_digest\": \"" + composition.MapDigest + "\",", "  \"candidate_pool_count\": " + composition.CandidateSet.Candidates.Count + ",", "  \"pattern_placements\": " + composition.PlacementCount + ",", "  \"route_placements\": " + composition.RoutePlacementCount + ",", "  \"branch_placements\": " + composition.BranchPlacementCount + ",", "  \"filler_detail_placements\": " + composition.FillerDetailPlacementCount + ",", "  \"selection_attempt_count\": " + composition.SelectionAttemptCount + ",", "  \"placements\": [" };
            for (var index = 0; index < composition.Placements.Count; index++)
            {
                var placement = composition.Placements[index]; var suffix = index + 1 == composition.Placements.Count ? string.Empty : ",";
                lines.Add("    {\"slot\":" + placement.SlotIndex + ",\"coordinate\":\"" + placement.Coordinate + "\",\"candidate_id\":\"" + Escape(placement.Candidate.CandidateId) + "\",\"mask_u16_hex\":\"" + placement.Candidate.MaskU16Hex + "\",\"socket_signature\":\"" + Escape(placement.Candidate.SocketSignature) + "\",\"transform\":\"" + placement.Transform + "\",\"selection_reason\":\"" + Escape(placement.SelectionReason) + "\",\"attempt_count\":" + placement.AttemptCount + "}" + suffix);
            }
            lines.Add("  ]"); lines.Add("}"); return Lines(lines);
        }

        private static string ValidationJson(MoonPalaceMicroPatternRunValidation validation)
        {
            return Json(new[] { Pair("start_tile", validation.StartTile.ToString()), Pair("exit_tile", validation.ExitTile.ToString()), Bool("start_open", validation.StartOpen),
                Bool("exit_open", validation.ExitOpen), Bool("tile_level_bfs_start_to_exit", validation.StartToExitReachable),
                Pair("reachable_required_waypoints", validation.ReachableRequiredWaypointCount + "/" + validation.RequiredWaypointCount),
                Pair("reachable_branch_entries", validation.ReachableBranchEntryCount + "/" + validation.BranchEntryCount), Number("route_socket_mismatch_count", validation.RouteSocketMismatchCount),
                Number("out_of_bounds_placement_count", validation.OutOfBoundsPlacementCount), Number("duplicate_placement_count", validation.DuplicatePlacementCount),
                Number("unreachable_open_island_count", validation.UnreachableOpenIslandCount), Number("route_failure_count", validation.RouteFailureCount),
                Number("fallback_carve_count", validation.FallbackCarveCount), Number("silent_repair_count", validation.SilentRepairCount), Bool("passed", validation.Passed), Pair("validation_digest", validation.CanonicalDigest) });
        }

        private static string SceneManifestJson(MoonPalaceMicroPatternRunConfig config, MoonPalaceMicroPatternRunGraph graph,
            MoonPalaceMicroPatternRunComposition composition, MoonPalaceMicroPatternRunValidation validation, string sceneManifestDigest)
        {
            return Json(new[] { Pair("scene_path", SceneRelativePath), Pair("scene_root", SceneRootName), Number("generated_scene_count", 1),
                Pair("run_id", config.RunId), Pair("seed_id", config.SeedId), Pair("pattern_grid_size", config.PatternGridWidth + "x" + config.PatternGridHeight),
                Pair("tile_size", config.TileWidth + "x" + config.TileHeight), Pair("graph_digest", graph.Digest.Value), Pair("map_digest", composition.MapDigest),
                Pair("validation_digest", validation.CanonicalDigest), Pair("scene_manifest_digest", sceneManifestDigest),
                Pair("debug_tile_assets", string.Join(";", GetDebugTileAssetRelativePaths())) });
        }

        private static string DigestManifestJson(MoonPalaceMicroPatternRunConfig config, MoonPalaceMicroPatternRunGraph graph,
            MoonPalaceMicroPatternRunComposition composition, MoonPalaceMicroPatternRunValidation validation, string sceneManifestDigest,
            string digestManifestDigest, IEnumerable<KeyValuePair<string, string>> outputs)
        {
            var lines = new List<string> { "{", "  \"config_digest\": \"" + config.CanonicalDigest + "\",", "  \"graph_digest\": \"" + graph.Digest.Value + "\",", "  \"composition_digest\": \"" + composition.CompositionDigest + "\",", "  \"map_digest\": \"" + composition.MapDigest + "\",", "  \"validation_digest\": \"" + validation.CanonicalDigest + "\",", "  \"scene_manifest_digest\": \"" + sceneManifestDigest + "\",", "  \"digest_manifest_digest\": \"" + digestManifestDigest + "\",", "  \"artifacts\": [" };
            var entries = outputs.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToList();
            for (var index = 0; index < entries.Count; index++)
            {
                var suffix = index + 1 == entries.Count ? string.Empty : ",";
                lines.Add("    {\"relative_path\":\"" + Escape(entries[index].Key) + "\",\"sha256\":\"" + BakingCanonicalDigest.HashCanonicalText(entries[index].Value) + "\"}" + suffix);
            }
            lines.Add("  ]"); lines.Add("}"); return Lines(lines);
        }

        private static string Json(IEnumerable<string> fields) => "{\n" + string.Join(",\n", fields.Select(field => "  " + field)) + "\n}\n";
        private static string Pair(string name, string value) => "\"" + Escape(name) + "\": \"" + Escape(value) + "\"";
        private static string Number(string name, int value) => "\"" + Escape(name) + "\": " + value.ToString(CultureInfo.InvariantCulture);
        private static string Bool(string name, bool value) => "\"" + Escape(name) + "\": " + (value ? "true" : "false");
        private static string Lines(IEnumerable<string> lines) => string.Join("\n", lines) + "\n";
        private static string Csv(string value)
        {
            value = value ?? string.Empty;
            return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }
        private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string B(bool value) => value ? "TRUE" : "FALSE";
        private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        private static string FinalLf(string value) => BakingCanonicalDigest.NormalizeLineEndingsToLf(value ?? string.Empty).TrimEnd('\n') + "\n";
        private static string Join(string left, string right) => left.TrimEnd('/') + "/" + right.TrimStart('/');
        private static string Resolve(string root, string relative) => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        private static string DebugTilePath(string name) => Join(SceneDirectoryRelativePath, "RUN01_" + name + ".asset");
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        private sealed class TileDefinition
        {
            public TileDefinition(string name, Color color) { Name = name; Color = color; }
            public string Name { get; }
            public Color Color { get; }
        }
    }
}
