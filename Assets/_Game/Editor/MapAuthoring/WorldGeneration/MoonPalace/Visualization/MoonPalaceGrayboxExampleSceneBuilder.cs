using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MoonPalace.Visualization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.Visualization
{
    public sealed class MoonPalaceGrayboxPublication
    {
        internal MoonPalaceGrayboxPublication(MoonPalaceOneSectorGrayboxResult generation,
            IReadOnlyDictionary<string, string> outputs, string sceneManifestDigest, string digestManifestDigest)
        {
            Generation = generation;
            OutputContents = outputs;
            SceneManifestDigest = sceneManifestDigest;
            DigestManifestDigest = digestManifestDigest;
        }
        public MoonPalaceOneSectorGrayboxResult Generation { get; }
        public IReadOnlyDictionary<string, string> OutputContents { get; }
        public string SceneManifestDigest { get; }
        public string DigestManifestDigest { get; }
    }

    public static class MoonPalaceGrayboxExampleSceneBuilder
    {
        public const string AuthoringDirectoryRelativePath = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/VIS01";
        public const string GeneratedDirectoryRelativePath = "MapDesign/MCP/GENERATED/VIS01";
        public const string SceneDirectoryRelativePath = "Assets/_Game/Map/Scenes/MoonPalace/VIS01";
        public const string SceneRelativePath = "Assets/_Game/Map/Scenes/MoonPalace/VIS01/MoonPalaceGrayboxExample_VIS01.unity";
        public const string SceneRootName = "MoonPalace_Graybox_VIS01";

        private static readonly string[] CsvFileNames =
        {
            "moonpalace_vis01_pattern_candidates_500.csv",
            "moonpalace_vis01_pattern_placements.csv",
            "moonpalace_vis01_microchunks.csv",
            "moonpalace_vis01_cells.csv",
            "moonpalace_vis01_layers.csv",
            "moonpalace_vis01_selection_manifest.csv",
            "moonpalace_vis01_validation_summary.csv",
        };

        private static readonly string[] JsonFileNames =
        {
            "moonpalace_vis01_manifest.json",
            "moonpalace_vis01_catalog_snapshot.json",
            "moonpalace_vis01_pattern_candidates_500.json",
            "moonpalace_vis01_generation_result.json",
            "moonpalace_vis01_scene_manifest.json",
            "moonpalace_vis01_digest_manifest.json",
        };

        [MenuItem("Tools/Map/MoonPalace/Build VIS01 Graybox Example")]
        public static void PublishFromMenu() => Publish(ProjectRoot);

        public static void PublishFromCommandLine()
        {
            var publication = Publish(ProjectRoot);
            Debug.Log("VIS01 scene published: " + SceneRelativePath + " digest=" + publication.Generation.LogicalMapDigest);
        }

        public static IReadOnlyList<string> GetSourceReadRelativePaths() => MoonPalaceRuntimeCatalogSnapshot.GetSourceRelativePaths();
        public static IReadOnlyList<string> GetExpectedOutputRelativePaths() => new ReadOnlyCollection<string>(
            CsvFileNames.Select(name => Join(AuthoringDirectoryRelativePath, name))
                .Concat(JsonFileNames.Select(name => Join(GeneratedDirectoryRelativePath, name)))
                .Concat(new[] { SceneRelativePath }).Concat(GetDebugTileAssetRelativePaths()).ToList());

        public static IReadOnlyList<string> GetDebugTileAssetRelativePaths() => new ReadOnlyCollection<string>(
            DebugTileDefinitions().Select(definition => DebugTilePath(definition.Name)).ToList());

        public static MoonPalaceGrayboxPublication BuildSnapshot(string projectRoot, bool reverseEnumeration)
        {
            var generation = MoonPalaceOneSectorGrayboxGenerator.Generate(projectRoot, reverseEnumeration);
            var outputs = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                [Join(AuthoringDirectoryRelativePath, CsvFileNames[0])] = SerializeCandidatesCsv(generation),
                [Join(AuthoringDirectoryRelativePath, CsvFileNames[1])] = SerializePlacementsCsv(generation),
                [Join(AuthoringDirectoryRelativePath, CsvFileNames[2])] = SerializeChunksCsv(generation),
                [Join(AuthoringDirectoryRelativePath, CsvFileNames[3])] = SerializeCellsCsv(generation),
                [Join(AuthoringDirectoryRelativePath, CsvFileNames[4])] = SerializeLayersCsv(generation),
                [Join(AuthoringDirectoryRelativePath, CsvFileNames[5])] = SerializeSelectionCsv(generation),
                [Join(AuthoringDirectoryRelativePath, CsvFileNames[6])] = SerializeValidationCsv(generation),
                [Join(GeneratedDirectoryRelativePath, JsonFileNames[0])] = SerializeManifestJson(generation),
                [Join(GeneratedDirectoryRelativePath, JsonFileNames[1])] = SerializeCatalogJson(generation.Catalog),
                [Join(GeneratedDirectoryRelativePath, JsonFileNames[2])] = SerializeCandidatesJson(generation),
                [Join(GeneratedDirectoryRelativePath, JsonFileNames[3])] = SerializeGenerationJson(generation),
            };
            var sceneMaterial = string.Join("|", SceneRelativePath, SceneRootName, string.Join(";", GetDebugTileAssetRelativePaths()),
                generation.SeedId, generation.SeedValue.ToString(CultureInfo.InvariantCulture), "6,6", generation.LogicalMapDigest,
                "Grid", "Terrain_Solid_Open", "Route_Recovery_Overlay", "Marker_Protection_Boundaries", "VIS01_Camera", "Legend", "Metadata");
            var sceneManifestDigest = BakingCanonicalDigest.HashCanonicalText(sceneMaterial);
            outputs[Join(GeneratedDirectoryRelativePath, JsonFileNames[4])] = SerializeSceneManifestJson(generation, sceneManifestDigest);

            var artifactLines = outputs.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => pair.Key + "|" + BakingCanonicalDigest.HashCanonicalText(pair.Value)).ToArray();
            var digestManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                generation.Catalog.CanonicalDigest, generation.CandidateSet.CanonicalDigest,
                generation.LogicalMapDigest, sceneManifestDigest,
            }.Concat(artifactLines));
            outputs[Join(GeneratedDirectoryRelativePath, JsonFileNames[5])] = SerializeDigestManifestJson(generation,
                sceneManifestDigest, digestManifestDigest, outputs);
            return new MoonPalaceGrayboxPublication(generation,
                new ReadOnlyDictionary<string, string>(outputs), sceneManifestDigest, digestManifestDigest);
        }

        public static MoonPalaceGrayboxPublication Publish(string projectRoot)
        {
            var root = Path.GetFullPath(projectRoot);
            var publication = BuildSnapshot(root, false);
            Directory.CreateDirectory(Resolve(root, AuthoringDirectoryRelativePath));
            Directory.CreateDirectory(Resolve(root, GeneratedDirectoryRelativePath));
            Directory.CreateDirectory(Resolve(root, SceneDirectoryRelativePath));
            foreach (var output in publication.OutputContents)
                File.WriteAllText(Resolve(root, output.Key), NormalizeFinalLf(output.Value), BakingCanonicalDigest.Utf8NoBomEncoding);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuildScene(publication.Generation, publication.SceneManifestDigest);
            AssetDatabase.SaveAssets();
            return publication;
        }

        private static void BuildScene(MoonPalaceOneSectorGrayboxResult result, string sceneManifestDigest)
        {
            var previous = SceneManager.GetActiveScene();
            var replaceUntitled = previous.IsValid() && string.IsNullOrEmpty(previous.path);
            if (replaceUntitled && previous.isDirty)
                throw new InvalidOperationException("VIS01 refuses to replace a dirty untitled scene.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                replaceUntitled ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var tiles = CreateDebugTiles();
                var root = new GameObject(SceneRootName);
                var gridObject = new GameObject("Grid", typeof(Grid));
                gridObject.transform.SetParent(root.transform, false);
                var terrain = CreateTilemap("Terrain_Solid_Open", gridObject.transform, 0);
                var routes = CreateTilemap("Route_Recovery_Overlay", gridObject.transform, 10);
                var annotations = CreateTilemap("Marker_Protection_Boundaries", gridObject.transform, 20);

                foreach (var cell in result.Cells)
                {
                    var position = new Vector3Int(cell.X, cell.Y, 0);
                    terrain.SetTile(position, tiles[cell.IsOpen ? "Open" : "Solid"]);
                    if (cell.IsRequiredRoute) routes.SetTile(position, tiles["RequiredRoute"]);
                    else if (cell.IsRecoveryRoute) routes.SetTile(position, tiles["RecoveryRoute"]);

                    if (!string.IsNullOrEmpty(cell.MarkerKind)) annotations.SetTile(position, tiles["Marker"]);
                    else if (cell.X % 12 == 0 || cell.Y % 8 == 0) annotations.SetTile(position, tiles["ChunkBoundary"]);
                    else if (cell.X % 4 == 0 || cell.Y % 4 == 0) annotations.SetTile(position, tiles["PatternBoundary"]);
                    else if (cell.IsProtected) annotations.SetTile(position, tiles["Protected"]);
                }
                foreach (var tilemap in new[] { terrain, routes, annotations })
                {
                    tilemap.CompressBounds();
                    tilemap.RefreshAllTiles();
                    EditorUtility.SetDirty(tilemap);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (terrain.cellBounds.size.x != 48 || terrain.cellBounds.size.y != 32)
                    throw new InvalidOperationException("VIS01 terrain Tilemap did not receive the 48x32 generated canvas: bounds=" +
                        terrain.cellBounds + ", cells=" + result.Cells.Count.ToString(CultureInfo.InvariantCulture) +
                        ", probe=" + (terrain.GetTile(Vector3Int.zero) == null ? "null" : terrain.GetTile(Vector3Int.zero).name) +
                        ", solidPersistent=" + EditorUtility.IsPersistent(tiles["Solid"]) +
                        ", openPersistent=" + EditorUtility.IsPersistent(tiles["Open"]) +
                        ", scene=" + terrain.gameObject.scene.name + ", active=" + terrain.gameObject.activeInHierarchy);

                var cameraObject = new GameObject("VIS01_Camera", typeof(Camera));
                cameraObject.transform.SetParent(root.transform, false);
                cameraObject.transform.position = new Vector3(24f, 16f, -10f);
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 20.5f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.06f, 0.075f, 1f);
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 100f;

                CreateLegend(root.transform);
                var metadata = new GameObject("Metadata");
                metadata.transform.SetParent(root.transform, false);
                metadata.transform.position = new Vector3(0f, -2.25f, -1f);
                var metadataText = metadata.AddComponent<TextMesh>();
                metadataText.text = "seed_id=MP_QA_01 | seed_value=1924737067 | sector=6,6 | logical_digest=" +
                                    result.LogicalMapDigest + " | scene_manifest_digest=" + sceneManifestDigest;
                metadataText.color = Color.white;
                metadataText.characterSize = 0.35f;
                metadataText.fontSize = 24;
                metadataText.anchor = TextAnchor.MiddleLeft;

                if (!EditorSceneManager.SaveScene(scene, SceneRelativePath, false))
                    throw new IOException("Unity did not save the isolated VIS01 scene.");
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
            if (sprite == null) throw new InvalidOperationException("Unity built-in UISprite is required for VIS01 debug tiles.");
            var definitions = DebugTileDefinitions();
            foreach (var definition in definitions)
            {
                var existingPath = DebugTilePath(definition.Name);
                if (File.Exists(Resolve(ProjectRoot, existingPath)))
                    AssetDatabase.ImportAsset(existingPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
            var result = new Dictionary<string, Tile>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                var path = DebugTilePath(definition.Name);
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    tile.name = "VIS01_" + definition.Name;
                    AssetDatabase.CreateAsset(tile, path);
                }
                tile.name = "VIS01_" + definition.Name;
                tile.sprite = sprite;
                tile.color = definition.Color;
                tile.colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                result.Add(definition.Name, tile);
            }
            AssetDatabase.SaveAssets();
            foreach (var definition in definitions)
                AssetDatabase.ImportAsset(DebugTilePath(definition.Name),
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var definition in definitions)
            {
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(DebugTilePath(definition.Name));
                if (tile == null)
                    throw new InvalidOperationException("Imported VIS01 debug tile is missing: VIS01_" + definition.Name);
                if (!EditorUtility.IsPersistent(tile))
                    throw new InvalidOperationException("Imported VIS01 debug tile is not persistent: VIS01_" + definition.Name);
                result[definition.Name] = tile;
            }
            return result;
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

        private static void CreateLegend(Transform parent)
        {
            var legend = new GameObject("Legend");
            legend.transform.SetParent(parent, false);
            var entries = new[]
            {
                "Solid_DarkGray", "Open_BlueGray", "RequiredRoute_BrightGreen", "RecoveryRoute_Cyan",
                "Marker_Violet", "Protected_Blue", "MicroChunkBoundary_Dark", "MicroPatternBoundary_Subtle"
            };
            for (var index = 0; index < entries.Length; index++)
            {
                var entry = new GameObject("Legend_" + entries[index]);
                entry.transform.SetParent(legend.transform, false);
                entry.transform.position = new Vector3(index < 4 ? index * 12f : (index - 4) * 12f,
                    index < 4 ? 34f : 32.7f, -1f);
                var text = entry.AddComponent<TextMesh>();
                text.text = entries[index].Replace('_', ' ');
                text.characterSize = 0.28f;
                text.fontSize = 20;
                text.anchor = TextAnchor.MiddleLeft;
                text.color = Color.white;
            }
        }

        private static string SerializeCandidatesCsv(MoonPalaceOneSectorGrayboxResult result)
        {
            var lines = new List<string>
            {
                "rank,candidate_id,mask_u16_hex,open_count,solid_count,density,largest_open_component,open_component_count,north_socket_bits,south_socket_bits,west_socket_bits,east_socket_bits,floor_support_bits,ceiling_gap_bits,left_wall_bits,right_wall_bits,silhouette_signature,socket_signature,mirror_family_signature,density_bucket,edge_socket_bucket,support_affordance_class,hazard_detail_eligibility,role_tags,chunk_path_allowed,score"
            };
            lines.AddRange(result.CandidateSet.Candidates.Select(c => string.Join(",",
                I(c.Rank), c.CandidateId, c.MaskU16Hex, I(c.OpenCount), I(c.SolidCount), c.Density.ToString("0.0000", CultureInfo.InvariantCulture),
                I(c.LargestOpenComponent), I(c.OpenComponentCount), Hex(c.NorthSocketBits), Hex(c.SouthSocketBits), Hex(c.WestSocketBits),
                Hex(c.EastSocketBits), Hex(c.FloorSupportBits), Hex(c.CeilingGapBits), Hex(c.LeftWallBits), Hex(c.RightWallBits),
                c.SilhouetteSignature, c.SocketSignature, c.MirrorFamilySignature, c.DensityBucket, c.EdgeSocketBucket,
                c.SupportAffordanceClass, c.HazardDetailEligibility, Csv(c.RoleTags), B(c.ChunkPathAllowed), I(c.Score))));
            return Lines(lines);
        }

        private static string SerializePlacementsCsv(MoonPalaceOneSectorGrayboxResult result)
        {
            var lines = new List<string> { "placement_index,pattern_x,pattern_y,chunk_id,candidate_id,production_pattern_family_id,transform,biome_id,terrain_cluster_id,spine_variant_id" };
            lines.AddRange(result.PatternPlacements.Select(p => string.Join(",", I(p.PlacementIndex), I(p.PatternX), I(p.PatternY),
                p.ChunkId, p.CandidateId, p.ProductionPatternFamilyId, p.Transform, p.BiomeId, p.TerrainClusterId, Csv(p.SpineVariantId))));
            return Lines(lines);
        }

        private static string SerializeChunksCsv(MoonPalaceOneSectorGrayboxResult result)
        {
            var lines = new List<string> { "chunk_id,chunk_index,chunk_x,chunk_y,candidate_ids,pattern_local_positions,entry_sockets,exit_sockets,connectivity_summary,required_path_cells,recovery_path_cells,protected_cells,marker_slots,composition_attempt_count,reachable,failure_owner,fallback_carve_count,silent_auto_repair_count,chunk_digest" };
            lines.AddRange(result.MicroChunks.Select(chunk => string.Join(",", chunk.ChunkId, I(chunk.ChunkIndex), I(chunk.ChunkX), I(chunk.ChunkY),
                Csv(string.Join(";", chunk.PatternPlacements.Select(p => p.Candidate.CandidateId))),
                Csv(string.Join(";", chunk.PatternPlacements.Select(p => p.PatternX + ":" + p.PatternY))),
                Csv(string.Join(";", chunk.EntrySockets)), Csv(string.Join(";", chunk.ExitSockets)), Csv(chunk.Validation.ConnectivitySummary),
                Csv(string.Join(";", chunk.RequiredPathCells)), Csv(string.Join(";", chunk.RecoveryPathCells)),
                Csv(string.Join(";", chunk.ProtectedCells)), Csv(string.Join(";", chunk.MarkerSlots)), I(chunk.CompositionAttemptCount), B(chunk.Validation.Reachable),
                Csv(chunk.Validation.FailureOwner), I(chunk.Validation.FallbackCarveCount), I(chunk.Validation.SilentAutoRepairCount), chunk.ChunkDigest)));
            return Lines(lines);
        }

        private static string SerializeCellsCsv(MoonPalaceOneSectorGrayboxResult result)
        {
            var lines = new List<string> { "x,y,is_open,tile_code,biome_id,chunk_id,candidate_id,production_pattern_family_id,required_route,recovery_route,protected,marker_kind,marker_source_id,boundary_candidate_id" };
            lines.AddRange(result.Cells.Select(c => string.Join(",", I(c.X), I(c.Y), B(c.IsOpen), c.TileCode, c.BiomeId,
                c.ChunkId, c.CandidateId, c.ProductionPatternFamilyId, B(c.IsRequiredRoute), B(c.IsRecoveryRoute), B(c.IsProtected),
                c.MarkerKind, c.MarkerSourceId, c.BoundaryCandidateId)));
            return Lines(lines);
        }

        private static string SerializeLayersCsv(MoonPalaceOneSectorGrayboxResult result)
        {
            var lines = new List<string> { "layer_index,layer_name,x,y,value" };
            lines.AddRange(result.LogicalLayers.Select(l => string.Join(",", I(l.LayerIndex), l.LayerName, I(l.X), I(l.Y), Csv(l.Value))));
            return Lines(lines);
        }

        private static string SerializeSelectionCsv(MoonPalaceOneSectorGrayboxResult result)
        {
            var lines = new List<string> { "kind,id,source_path,owner_id" };
            lines.AddRange(result.SelectionManifest.Select(s => string.Join(",", s.Kind, Csv(s.Id), Csv(s.SourcePath), Csv(s.OwnerId))));
            return Lines(lines);
        }

        private static string SerializeValidationCsv(MoonPalaceOneSectorGrayboxResult result)
        {
            var v = result.Validation;
            var metrics = new[]
            {
                M("raw_mask_count", 65536), M("accepted_candidate_count", result.CandidateSet.Candidates.Count),
                M("unique_coordinates", v.UniqueCoordinateCount), M("pattern_placements", result.PatternPlacements.Count),
                M("microchunks", result.MicroChunks.Count), M("patterns_per_microchunk", result.MicroChunks.Min(c => c.PatternPlacements.Count)),
                M("cells_per_microchunk", result.MicroChunks.Min(c => c.OpenCells.Count)), M("logical_layer_records", result.LogicalLayers.Count),
                M("route_failure_count", v.RouteFailureCount), M("recovery_failure_count", v.RecoveryFailureCount),
                M("seam_failure_count", v.SeamFailureCount), M("unreachable_microchunk_count", v.UnreachableMicroChunkCount),
                M("fallback_carve_count", v.FallbackCarveCount), M("silent_auto_repair_count", v.SilentAutoRepairCount),
                M("catalog_source_mutation_count", v.CatalogSourceMutationCount), M("full_world_generation_runs", v.FullWorldGenerationRunCount),
            };
            return Lines(new[] { "metric,value,verdict" }.Concat(metrics.Select(m => m.Name + "," + I(m.Value) + "," + (m.Value == m.Expected ? "PASS" : "FAIL"))));
        }

        private static string SerializeManifestJson(MoonPalaceOneSectorGrayboxResult r) => JsonObject(new[]
        {
            J("artifact_name", "MoonPalace Graybox Example Scene v1"), J("seed_id", r.SeedId), N("seed_value", r.SeedValue),
            J("scope", "one_sector"), A("sector", new[] { "6", "6" }), A("logical_size", new[] { "48", "32" }),
            A("micro_pattern_size", new[] { "4", "4" }), N("raw_mask_count", r.CandidateSet.RawMaskCount),
            N("micro_pattern_candidates", r.CandidateSet.Candidates.Count), A("microchunks", new[] { "4", "4" }),
            A("microchunk_size", new[] { "12", "8" }), A("patterns_per_microchunk", new[] { "3", "2" }),
            N("logical_layers", 7), J("catalog_digest", r.Catalog.CanonicalDigest), J("candidate_digest", r.CandidateSet.CanonicalDigest),
            J("logical_map_digest", r.LogicalMapDigest), J("created_utc_policy", "excluded_from_canonical_digest")
        });

        private static string SerializeCatalogJson(MoonPalaceRuntimeCatalogSnapshot c)
        {
            var builder = new StringBuilder();
            builder.Append("{\n  \"counts\": ").Append(JsonObjectInline(new[]
            {
                N("biomes", c.BiomeCount), N("tile_codes", c.TileCodeCount), N("production_micro_patterns", c.ProductionMicroPatternCount),
                N("production_pattern_cells", c.ProductionPatternCellCount), N("terrain_clusters", c.TerrainClusterCount),
                N("cluster_spine_variants", c.ClusterSpineVariantCount), N("activity_profiles", c.ActivityProfileCount),
                N("event_overlay_profiles", c.EventOverlayProfileCount), N("biome_pairs", c.BiomePairCount),
                N("boundary_candidates", c.BoundaryCandidateCount), N("core_resource_regions", c.CoreResourceRegionCount),
                N("tuning_density_windows", c.TuningDensityWindowCount), N("qa_seed_MP_QA_01", c.QaSeedMpQa01)
            })).Append(",\n  \"sources\": [\n");
            for (var index = 0; index < c.Sources.Count; index++)
            {
                var source = c.Sources[index];
                builder.Append("    ").Append(JsonObjectInline(new[] { J("source_file_path", source.RelativePath), N("record_count", source.RecordCount),
                    J("header_hash", source.HeaderHash), J("content_sha256", source.ContentSha256),
                    AS("typed_load_errors", source.TypedLoadErrors), AS("foreign_key_errors", source.ForeignKeyErrors),
                    AS("duplicate_id_errors", source.DuplicateIdErrors) }));
                builder.Append(index + 1 == c.Sources.Count ? "\n" : ",\n");
            }
            builder.Append("  ],\n  \"canonical_catalog_digest\": \"").Append(E(c.CanonicalDigest)).Append("\"\n}\n");
            return builder.ToString();
        }

        private static string SerializeCandidatesJson(MoonPalaceOneSectorGrayboxResult r)
        {
            var builder = new StringBuilder();
            builder.Append("{\n  \"raw_mask_count\": 65536,\n  \"accepted_candidate_count\": 500,\n  \"eligible_mask_count\": ")
                .Append(I(r.CandidateSet.EligibleMaskCount))
                .Append(",\n  \"selection_algorithm\": \"explicit_filter_plus_diversity_bucket_round_robin_v1\",")
                .Append("\n  \"filter_rules\": [\"unique_mask_id\",\"all_solid_rejected\",\"single_quiet_open_exception\",\"open_count_4_to_14\",\"largest_open_component_at_least_4\",\"no_isolated_one_cell_open_pocket\",\"edge_socket_or_support_affordance\",\"coordinate_bounds_4x4\"],")
                .Append("\n  \"rejection_counts\": ")
                .Append(JsonObjectInline(r.CandidateSet.RejectionCounts.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => N(pair.Key, pair.Value))))
                .Append(",\n  \"candidates\": [\n");
            for (var index = 0; index < r.CandidateSet.Candidates.Count; index++)
            {
                var c = r.CandidateSet.Candidates[index];
                builder.Append("    ").Append(JsonObjectInline(new[] { N("rank", c.Rank), J("candidate_id", c.CandidateId), J("mask_u16_hex", c.MaskU16Hex),
                    N("open_count", c.OpenCount), N("solid_count", c.SolidCount), R("density", c.Density), N("largest_open_component", c.LargestOpenComponent),
                    N("open_component_count", c.OpenComponentCount), N("north_socket_bits", c.NorthSocketBits), N("south_socket_bits", c.SouthSocketBits),
                    N("west_socket_bits", c.WestSocketBits), N("east_socket_bits", c.EastSocketBits), N("floor_support_bits", c.FloorSupportBits),
                    N("ceiling_gap_bits", c.CeilingGapBits), N("left_wall_bits", c.LeftWallBits), N("right_wall_bits", c.RightWallBits),
                    J("silhouette_signature", c.SilhouetteSignature), J("socket_signature", c.SocketSignature), J("mirror_family_signature", c.MirrorFamilySignature),
                    J("density_bucket", c.DensityBucket), J("edge_socket_bucket", c.EdgeSocketBucket), J("support_affordance_class", c.SupportAffordanceClass),
                    J("hazard_detail_eligibility", c.HazardDetailEligibility), J("role_tags", c.RoleTags), Z("chunk_path_allowed", c.ChunkPathAllowed), N("score", c.Score) }));
                builder.Append(index + 1 == r.CandidateSet.Candidates.Count ? "\n" : ",\n");
            }
            builder.Append("  ],\n  \"canonical_digest\": \"").Append(E(r.CandidateSet.CanonicalDigest)).Append("\"\n}\n");
            return builder.ToString();
        }

        private static string SerializeGenerationJson(MoonPalaceOneSectorGrayboxResult r)
        {
            var builder = new StringBuilder();
            builder.Append("{\n  \"seed_id\": \"").Append(r.SeedId).Append("\",\n  \"seed_value\": ").Append(I(r.SeedValue))
                .Append(",\n  \"sector\": [6, 6],\n  \"size\": [48, 32],\n  \"pattern_placements\": [\n");
            for (var i = 0; i < r.PatternPlacements.Count; i++)
            {
                var p = r.PatternPlacements[i];
                builder.Append("    ").Append(JsonObjectInline(new[] { N("placement_index", p.PlacementIndex), N("pattern_x", p.PatternX), N("pattern_y", p.PatternY),
                    J("chunk_id", p.ChunkId), J("candidate_id", p.CandidateId), J("production_pattern_family_id", p.ProductionPatternFamilyId),
                    J("transform", p.Transform), J("biome_id", p.BiomeId), J("terrain_cluster_id", p.TerrainClusterId), J("spine_variant_id", p.SpineVariantId) }));
                builder.Append(i + 1 == r.PatternPlacements.Count ? "\n" : ",\n");
            }
            builder.Append("  ],\n  \"microchunks\": [\n");
            for (var i = 0; i < r.MicroChunks.Count; i++)
            {
                var c = r.MicroChunks[i];
                builder.Append("    ").Append(JsonObjectInline(new[] { J("chunk_id", c.ChunkId), N("chunk_index", c.ChunkIndex),
                    A("chunk_coordinate", new[] { I(c.ChunkX), I(c.ChunkY) }), AS("selected_candidate_ids", c.PatternPlacements.Select(p => p.Candidate.CandidateId)),
                    AS("entry_sockets", c.EntrySockets.Select(x => x.ToString())), AS("exit_sockets", c.ExitSockets.Select(x => x.ToString())),
                    J("internal_open_connectivity_summary", c.Validation.ConnectivitySummary), AS("required_path_cells", c.RequiredPathCells.Select(x => x.ToString())),
                    AS("recovery_path_cells", c.RecoveryPathCells.Select(x => x.ToString())), AS("protected_cells", c.ProtectedCells.Select(x => x.ToString())),
                    AS("marker_slots", c.MarkerSlots.Select(x => x.ToString())), N("composition_attempt_count", c.CompositionAttemptCount),
                    Z("reachability_verdict", c.Validation.Reachable),
                    J("failure_owner", c.Validation.FailureOwner), J("chunk_digest", c.ChunkDigest) }));
                builder.Append(i + 1 == r.MicroChunks.Count ? "\n" : ",\n");
            }
            builder.Append("  ],\n  \"final_canvas_cells\": [\n");
            for (var i = 0; i < r.Cells.Count; i++)
            {
                var c = r.Cells[i];
                builder.Append("    ").Append(JsonObjectInline(new[] { N("x", c.X), N("y", c.Y), Z("open", c.IsOpen), J("tile_code", c.TileCode),
                    J("biome_id", c.BiomeId), J("chunk_id", c.ChunkId), J("candidate_id", c.CandidateId),
                    J("production_pattern_family_id", c.ProductionPatternFamilyId), Z("required_route", c.IsRequiredRoute),
                    Z("recovery_route", c.IsRecoveryRoute), Z("protected", c.IsProtected), J("marker_kind", c.MarkerKind),
                    J("marker_source_id", c.MarkerSourceId), J("boundary_candidate_id", c.BoundaryCandidateId) }));
                builder.Append(i + 1 == r.Cells.Count ? "\n" : ",\n");
            }
            builder.Append("  ],\n  \"logical_bake_records\": [\n");
            for (var i = 0; i < r.LogicalLayers.Count; i++)
            {
                var l = r.LogicalLayers[i];
                builder.Append("    ").Append(JsonObjectInline(new[] { N("layer_index", l.LayerIndex), J("layer_name", l.LayerName),
                    N("x", l.X), N("y", l.Y), J("value", l.Value) }));
                builder.Append(i + 1 == r.LogicalLayers.Count ? "\n" : ",\n");
            }
            builder.Append("  ],\n  \"validation\": ").Append(ValidationJson(r)).Append(",\n  \"logical_map_digest\": \"")
                .Append(E(r.LogicalMapDigest)).Append("\"\n}\n");
            return builder.ToString();
        }

        private static string SerializeSceneManifestJson(MoonPalaceOneSectorGrayboxResult r, string digest) => JsonObject(new[]
        {
            J("scene_path", SceneRelativePath), J("scene_root_object", SceneRootName), AS("debug_tile_assets", GetDebugTileAssetRelativePaths()),
            N("generated_unity_scene_count", 1), J("grid_object", "Grid"),
            AS("tilemap_layers", new[] { "Terrain_Solid_Open", "Route_Recovery_Overlay", "Marker_Protection_Boundaries" }),
            J("camera", "VIS01_Camera:orthographic"), J("legend", "Legend"), J("metadata", "Metadata"),
            J("seed_id", r.SeedId), N("seed_value", r.SeedValue), A("sector", new[] { "6", "6" }),
            J("logical_map_digest", r.LogicalMapDigest), J("scene_manifest_digest", digest), Z("included_in_build_settings", false)
        });

        private static string SerializeDigestManifestJson(MoonPalaceOneSectorGrayboxResult r, string sceneDigest,
            string digest, IReadOnlyDictionary<string, string> outputs)
        {
            var builder = new StringBuilder();
            builder.Append("{\n  \"catalog_digest\": \"").Append(r.Catalog.CanonicalDigest).Append("\",\n  \"candidate_digest\": \"")
                .Append(r.CandidateSet.CanonicalDigest).Append("\",\n  \"logical_map_digest\": \"").Append(r.LogicalMapDigest)
                .Append("\",\n  \"scene_manifest_digest\": \"").Append(sceneDigest).Append("\",\n  \"artifacts\": [\n");
            var ordered = outputs.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
            for (var i = 0; i < ordered.Length; i++)
            {
                builder.Append("    ").Append(JsonObjectInline(new[] { J("relative_path", ordered[i].Key),
                    J("sha256", BakingCanonicalDigest.HashCanonicalText(ordered[i].Value)) }));
                builder.Append(i + 1 == ordered.Length ? "\n" : ",\n");
            }
            builder.Append("  ],\n  \"self_reference_policy\": \"digest_manifest_self_excluded\",\n  \"canonical_digest\": \"")
                .Append(digest).Append("\"\n}\n");
            return builder.ToString();
        }

        private static string ValidationJson(MoonPalaceOneSectorGrayboxResult r)
        {
            var v = r.Validation;
            return JsonObjectInline(new[] { N("unique_coordinates", v.UniqueCoordinateCount), N("pattern_placements", r.PatternPlacements.Count),
                N("microchunks", r.MicroChunks.Count), N("patterns_per_microchunk", 6), N("cells_per_microchunk", 96),
                N("logical_layer_records", r.LogicalLayers.Count), N("route_failure_count", v.RouteFailureCount),
                N("recovery_failure_count", v.RecoveryFailureCount), N("seam_failure_count", v.SeamFailureCount),
                N("unreachable_microchunk_count", v.UnreachableMicroChunkCount), N("fallback_carve_count", v.FallbackCarveCount),
                N("silent_auto_repair_count", v.SilentAutoRepairCount), N("catalog_source_mutation_count", v.CatalogSourceMutationCount) });
        }

        private static string JsonObject(IEnumerable<string> fields) => "{\n  " + string.Join(",\n  ", fields) + "\n}\n";
        private static string JsonObjectInline(IEnumerable<string> fields) => "{" + string.Join(",", fields) + "}";
        private static string J(string name, string value) => "\"" + E(name) + "\":\"" + E(value ?? string.Empty) + "\"";
        private static string N(string name, int value) => "\"" + E(name) + "\":" + I(value);
        private static string R(string name, double value) => "\"" + E(name) + "\":" + value.ToString("0.0000", CultureInfo.InvariantCulture);
        private static string Z(string name, bool value) => "\"" + E(name) + "\":" + B(value);
        private static string A(string name, IEnumerable<string> rawValues) => "\"" + E(name) + "\":[" + string.Join(",", rawValues) + "]";
        private static string AS(string name, IEnumerable<string> values) => "\"" + E(name) + "\":[" + string.Join(",", values.Select(value => "\"" + E(value) + "\"")) + "]";
        private static string E(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        private static string Csv(string value)
        {
            value = value ?? string.Empty;
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0 ? value : "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        private static string Lines(IEnumerable<string> lines) => string.Join("\n", lines) + "\n";
        private static string NormalizeFinalLf(string value) => BakingCanonicalDigest.NormalizeLineEndingsToLf(value ?? string.Empty).TrimEnd('\n') + "\n";
        private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string B(bool value) => value ? "true" : "false";
        private static string Hex(int value) => "0x" + value.ToString("X1", CultureInfo.InvariantCulture);
        private static string Join(string left, string right) => left.TrimEnd('/') + "/" + right.TrimStart('/');
        private static string DebugTilePath(string name) => Join(SceneDirectoryRelativePath, "VIS01_" + name + ".asset");
        private static TileDefinition[] DebugTileDefinitions() => new[]
        {
            D("Solid", new Color(0.16f, 0.18f, 0.22f, 1f)),
            D("Open", new Color(0.62f, 0.72f, 0.78f, 1f)),
            D("RequiredRoute", new Color(0.15f, 1f, 0.28f, 0.86f)),
            D("RecoveryRoute", new Color(0.05f, 0.95f, 1f, 0.78f)),
            D("Marker", new Color(0.82f, 0.28f, 1f, 0.92f)),
            D("Protected", new Color(0.12f, 0.35f, 1f, 0.46f)),
            D("ChunkBoundary", new Color(0.01f, 0.01f, 0.015f, 0.58f)),
            D("PatternBoundary", new Color(0.16f, 0.2f, 0.26f, 0.22f)),
        };
        private static string Resolve(string root, string relative) => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static TileDefinition D(string name, Color color) => new TileDefinition(name, color);
        private static Metric M(string name, int value)
        {
            var expected = name == "raw_mask_count" ? 65536 : name == "accepted_candidate_count" ? 500 :
                name == "unique_coordinates" ? 1536 : name == "pattern_placements" ? 96 : name == "microchunks" ? 16 :
                name == "patterns_per_microchunk" ? 6 : name == "cells_per_microchunk" ? 96 :
                name == "logical_layer_records" ? 10752 : 0;
            return new Metric(name, value, expected);
        }

        private struct TileDefinition
        {
            public TileDefinition(string name, Color color) { Name = name; Color = color; }
            public string Name { get; }
            public Color Color { get; }
        }
        private struct Metric
        {
            public Metric(string name, int value, int expected) { Name = name; Value = value; Expected = expected; }
            public string Name { get; }
            public int Value { get; }
            public int Expected { get; }
        }
    }
}
