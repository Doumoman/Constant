using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using StarNight.Character.Live.Cameras;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.WorldData;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Character.Live.Rmap17.Editor
{
    /// <summary>
    /// Builds only the dedicated RMAP17 scene from the direct RMAP16 snapshot.
    /// The builder removes its own root on re-run and leaves every other scene
    /// root, Player prefab, and future mutable-world concern untouched.
    /// </summary>
    public static class RmapWorldBakeSceneBuilder
    {
        public const string ScenePath =
            "Assets/_Game/Map/Scenes/MoonPalace/RMAP17/MoonPalaceWorldBake_RMAP17.unity";
        public const string GeneratedDirectory = "MapDesign/MCP/GENERATED/RMAP17";
        public const string RootName = "MoonPalace_WorldBake_RMAP17";

        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";
        private const string TerrainTilePath = "Assets/_Game/Live/Prefabs/RMAP02/Tiles/RMAP02_Terrain.asset";
        private const string AffordanceTilePath = "Assets/_Game/Live/Prefabs/RMAP02/Tiles/RMAP02_Affordance.asset";
        private const string TerrainName = "RMAP17_BaseSolid";
        private const string OneWayName = "RMAP17_OneWay";
        private const string LadderName = "RMAP17_LadderOverlay";
        private const string GrabName = "RMAP17_GrabOverlay";
        private const string MarkerName = "RMAP17_MarkerOverlay";

        [MenuItem("Tools/MoonPalace/RMAP17/Build Full Static World Bake")]
        public static void BuildFromMenu()
        {
            BuildAndValidateTwice();
        }

        /// <summary>Batch-safe entry point. A second build demonstrates that the
        /// dedicated root is replaced instead of accumulated.</summary>
        public static void BuildAndValidateTwice()
        {
            BuildReport first = BuildInternal();
            BuildReport second = BuildInternal();
            if (second.RemovedBuilderRootCount != 1)
                throw new InvalidOperationException("RMAP17 builder root replacement was not idempotent.");
            Debug.Log("RMAP17_WORLD_BAKE_COMPLETE plan=" + second.Plan.Digest +
                " source_cells=" + second.Plan.SourceCellDigest +
                " scene=" + ScenePath + " first_replaced=" + first.RemovedBuilderRootCount +
                " ladder_segments=" + second.LadderTriggerSegmentCount);
        }

        private static BuildReport BuildInternal()
        {
            Rmap17WorldBakePlan plan = CreateVerifiedPlan();
            TileBase terrainTile = LoadTile(TerrainTilePath, "terrain");
            TileBase affordanceTile = LoadTile(AffordanceTilePath, "affordance");
            Scene scene = OpenDedicatedScene();
            int removedRoots = RemoveExistingBuilderRoots(scene);
            var root = new GameObject(RootName, typeof(Grid));
            SceneManager.MoveGameObjectToScene(root, scene);

            Tilemap terrain = CreateSolidTerrain(root.transform, terrainTile, plan);
            Tilemap oneWay = CreateOneWayTerrain(root.transform, terrainTile, plan);
            Tilemap ladder = CreateVisualLayer(root.transform, LadderName, 3, new Color(0.32f, 0.95f, 0.86f, 0.82f));
            Bake(ladder, affordanceTile, plan.LadderOverlays.Select(ToCell));
            int ladderSegments = CreateLadderTriggers(root.transform, plan.LadderOverlays);
            Tilemap grab = CreateVisualLayer(root.transform, GrabName, 4, new Color(1f, 0.82f, 0.26f, 0.9f));
            Bake(grab, affordanceTile, plan.GrabOverlays.Select(ToCell));
            ConfigureGrabAffordances(grab.gameObject, plan.GrabOverlays);
            Tilemap marker = CreateVisualLayer(root.transform, MarkerName, 5, new Color(0.96f, 0.36f, 0.66f, 0.9f));
            Bake(marker, affordanceTile, plan.MarkerOverlays.Select(ToCell));
            CharacterLivePlayerRig player = CreatePlayer(scene, root.transform, plan.PlayerStart);
            CharacterLiveCameraFollowDriver camera = CreateCamera(root.transform, player.transform);

            Physics2D.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(scene);
            EnsureSceneDirectory();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Scene reopened = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            BuildReport report = ValidateSavedScene(reopened, plan, removedRoots, ladderSegments);
            WriteArtifacts(report);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return report;
        }

        private static Scene OpenDedicatedScene()
        {
            string absolutePath = Path.Combine(ProjectRoot(), ScenePath.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(absolutePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static int RemoveExistingBuilderRoots(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects().Where(value => value.name == RootName).ToArray();
            foreach (GameObject root in roots) UnityEngine.Object.DestroyImmediate(root);
            return roots.Length;
        }

        private static Tilemap CreateSolidTerrain(Transform parent, TileBase tile, Rmap17WorldBakePlan plan)
        {
            var target = new GameObject(TerrainName, typeof(Tilemap), typeof(TilemapRenderer),
                typeof(TilemapCollider2D), typeof(Rigidbody2D), typeof(CompositeCollider2D));
            target.transform.SetParent(parent, false);
            TilemapRenderer renderer = target.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = 1;
            renderer.mode = TilemapRenderer.Mode.Chunk;
            TilemapCollider2D collider = target.GetComponent<TilemapCollider2D>();
            collider.compositeOperation = Collider2D.CompositeOperation.Merge;
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            Tilemap map = target.GetComponent<Tilemap>();
            Bake(map, tile, plan.SolidCells.Select(ToCell));
            collider.ProcessTilemapChanges();
            return map;
        }

        private static Tilemap CreateOneWayTerrain(Transform parent, TileBase tile, Rmap17WorldBakePlan plan)
        {
            var target = new GameObject(OneWayName, typeof(Tilemap), typeof(TilemapRenderer),
                typeof(TilemapCollider2D), typeof(Rigidbody2D), typeof(PlatformEffector2D),
                typeof(CharacterLiveOneWayPlatform));
            target.transform.SetParent(parent, false);
            TilemapRenderer renderer = target.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = 2;
            renderer.mode = TilemapRenderer.Mode.Chunk;
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            TilemapCollider2D collider = target.GetComponent<TilemapCollider2D>();
            collider.usedByEffector = true;
            target.GetComponent<PlatformEffector2D>().useOneWay = true;
            target.GetComponent<CharacterLiveOneWayPlatform>().Configure(collider);
            Tilemap map = target.GetComponent<Tilemap>();
            Bake(map, tile, plan.OneWayCells.Select(ToCell));
            collider.ProcessTilemapChanges();
            return map;
        }

        private static Tilemap CreateVisualLayer(Transform parent, string name, int sortingOrder, Color color)
        {
            var target = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            target.transform.SetParent(parent, false);
            TilemapRenderer renderer = target.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.mode = TilemapRenderer.Mode.Chunk;
            Tilemap map = target.GetComponent<Tilemap>();
            map.color = color;
            return map;
        }

        private static void Bake(Tilemap map, TileBase tile, IEnumerable<Vector3Int> sourceCells)
        {
            Vector3Int[] cells = (sourceCells ?? Array.Empty<Vector3Int>()).Distinct()
                .OrderBy(value => value.y).ThenBy(value => value.x).ToArray();
            if (cells.Length == 0) throw new InvalidOperationException(map.name + " requires occupied cells.");
            map.ClearAllTiles();
            map.SetTiles(cells, Enumerable.Repeat(tile, cells.Length).ToArray());
            map.CompressBounds();
            if (CountTiles(map) != cells.Length)
                throw new InvalidOperationException(map.name + " did not retain every direct snapshot tile.");
            EditorUtility.SetDirty(map);
        }

        private static int CreateLadderTriggers(Transform parent, IEnumerable<Rmap16Overlay> source)
        {
            var container = new GameObject("RMAP17_LadderTriggers");
            container.transform.SetParent(parent, false);
            int count = 0;
            foreach (IGrouping<int, Rmap16Overlay> column in (source ?? Array.Empty<Rmap16Overlay>())
                .GroupBy(value => value.X + "," + value.Y, StringComparer.Ordinal)
                .Select(value => value.OrderBy(item => item.Id, StringComparer.Ordinal).First())
                .OrderBy(value => value.X).ThenBy(value => value.Y).GroupBy(value => value.X))
            {
                Rmap16Overlay[] ordered = column.OrderBy(value => value.Y).ToArray();
                int offset = 0;
                while (offset < ordered.Length)
                {
                    int minY = ordered[offset].Y;
                    int length = 1;
                    while (offset + length < ordered.Length && ordered[offset + length].Y == minY + length)
                        length++;
                    var target = new GameObject("LadderTrigger_" + column.Key.ToString(CultureInfo.InvariantCulture) +
                        "_" + minY.ToString(CultureInfo.InvariantCulture), typeof(BoxCollider2D), typeof(CharacterLiveClimbSurface));
                    target.transform.SetParent(container.transform, false);
                    BoxCollider2D trigger = target.GetComponent<BoxCollider2D>();
                    trigger.offset = new Vector2(column.Key + 0.5f, minY + (length * 0.5f));
                    trigger.size = new Vector2(0.7f, length);
                    trigger.isTrigger = true;
                    target.GetComponent<CharacterLiveClimbSurface>().Configure(
                        CharacterLiveClimbSurface.SurfaceKind.Ladder, trigger);
                    offset += length;
                    count++;
                }
            }
            if (count == 0) throw new InvalidOperationException("RMAP17 requires at least one concrete ladder trigger.");
            return count;
        }

        private static void ConfigureGrabAffordances(GameObject target, IEnumerable<Rmap16Overlay> source)
        {
            CharacterLiveGrabSurface surface = target.AddComponent<CharacterLiveGrabSurface>();
            surface.Configure(CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            foreach (Rmap16Overlay overlay in source ?? Array.Empty<Rmap16Overlay>())
            {
                BoxCollider2D collider = target.AddComponent<BoxCollider2D>();
                collider.offset = new Vector2(overlay.X + 0.5f, overlay.Y + 0.5f);
                collider.size = new Vector2(0.18f, 1f);
                collider.isTrigger = false;
            }
        }

        private static CharacterLivePlayerRig CreatePlayer(Scene scene, Transform parent, RmapSpecialWorldPoint start)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null) throw new InvalidOperationException("RMAP17 Player prefab is missing: " + PlayerPrefabPath);
            GameObject player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (player == null) throw new InvalidOperationException("RMAP17 could not instantiate the production Player prefab.");
            player.name = "RMAP17_ProductionPlayer";
            player.transform.SetParent(parent, false);
            CharacterLivePlayerRig rig = player.GetComponent<CharacterLivePlayerRig>();
            CharacterLiveMovementDriver movement = player.GetComponent<CharacterLiveMovementDriver>();
            if (rig == null || movement == null || !rig.IsBound)
                throw new InvalidOperationException("RMAP17 requires the bound production Player rig and movement driver.");
            rig.BodyCollider.size = new Vector2(0.4f, 0.8f);
            rig.BodyCollider.offset = new Vector2(0f, 0.4f);
            movement.ConfigureRmap02(1 << LayerMask.NameToLayer("Default"));
            Vector2 spawn = new Vector2(start.X + 0.5f, start.Y);
            player.transform.position = new Vector3(spawn.x, spawn.y, 0f);
            rig.Body.position = spawn;
            movement.ResetMotion();
            return rig;
        }

        private static CharacterLiveCameraFollowDriver CreateCamera(Transform parent, Transform player)
        {
            var target = new GameObject("RMAP17_FollowCamera", typeof(Camera), typeof(CharacterLiveCameraFollowDriver));
            target.transform.SetParent(parent, false);
            Camera camera = target.GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.033f, 0.05f, 1f);
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            CharacterLiveCameraFollowDriver driver = target.GetComponent<CharacterLiveCameraFollowDriver>();
            driver.Configure(camera, player, new Rect(0f, 0f, Rmap17WorldBakePlan.WidthTiles,
                Rmap17WorldBakePlan.HeightTiles), 12f, 8f, 0.08f);
            return driver;
        }

        private static BuildReport ValidateSavedScene(Scene scene, Rmap17WorldBakePlan plan, int removedRoots, int ladderSegments)
        {
            GameObject[] roots = scene.GetRootGameObjects().Where(value => value.name == RootName).ToArray();
            if (roots.Length != 1) throw new InvalidOperationException("RMAP17 saved scene requires exactly one builder root.");
            Transform root = roots[0].transform;
            Tilemap terrain = FindTilemap(root, TerrainName);
            Tilemap oneWay = FindTilemap(root, OneWayName);
            Tilemap ladder = FindTilemap(root, LadderName);
            Tilemap grab = FindTilemap(root, GrabName);
            Tilemap marker = FindTilemap(root, MarkerName);
            CharacterLivePlayerRig player = root.GetComponentsInChildren<CharacterLivePlayerRig>(true).SingleOrDefault();
            CharacterLiveMovementDriver movement = root.GetComponentsInChildren<CharacterLiveMovementDriver>(true).SingleOrDefault();
            CharacterLiveCameraFollowDriver camera = root.GetComponentsInChildren<CharacterLiveCameraFollowDriver>(true).SingleOrDefault();
            if (player == null || movement == null || camera == null || !player.IsBound)
                throw new InvalidOperationException("RMAP17 saved scene is missing its production Player or follow camera.");
            if (CountTiles(terrain) != plan.SolidCellCount || CountTiles(oneWay) != plan.OneWayCellCount ||
                CountTiles(ladder) != UniqueCellCount(plan.LadderOverlays) ||
                CountTiles(grab) != UniqueCellCount(plan.GrabOverlays) ||
                CountTiles(marker) != UniqueCellCount(plan.MarkerOverlays))
                throw new InvalidOperationException("RMAP17 saved Tilemap inventory diverged from the direct RMAP16 snapshot.");
            TilemapCollider2D solidCollider = terrain.GetComponent<TilemapCollider2D>();
            TilemapCollider2D oneWayCollider = oneWay.GetComponent<TilemapCollider2D>();
            CompositeCollider2D solidComposite = terrain.GetComponent<CompositeCollider2D>();
            if (solidCollider == null || terrain.GetComponent<Rigidbody2D>() == null ||
                solidComposite == null || solidComposite.shapeCount == 0 || oneWayCollider == null ||
                !oneWayCollider.usedByEffector || oneWay.GetComponent<PlatformEffector2D>() == null ||
                oneWay.GetComponent<CharacterLiveOneWayPlatform>() == null)
                throw new InvalidOperationException("RMAP17 saved Tilemaps lack the required physical collider conventions.");
            if (root.GetComponentsInChildren<CharacterLiveClimbSurface>(true).Length != ladderSegments ||
                !root.GetComponentsInChildren<CharacterLiveClimbSurface>(true).All(value => value.IsUsable) ||
                root.GetComponentsInChildren<CharacterLiveGrabSurface>(true).Length != 1)
                throw new InvalidOperationException("RMAP17 saved overlay affordances are incomplete.");
            if (!terrain.HasTile(new Vector3Int(plan.PlayerStart.X, plan.PlayerStart.Y - 1, 0)) ||
                player.Body.position != new Vector2(plan.PlayerStart.X + 0.5f, plan.PlayerStart.Y) ||
                player.BodyCollider.size != new Vector2(0.4f, 0.8f) ||
                player.BodyCollider.offset != new Vector2(0f, 0.4f))
                throw new InvalidOperationException("RMAP17 Player start or actual support does not match RMAP15.");
            Rect bounds = camera.WorldBounds;
            if (bounds.x != 0f || bounds.y != 0f || bounds.width != Rmap17WorldBakePlan.WidthTiles ||
                bounds.height != Rmap17WorldBakePlan.HeightTiles || camera.VisibleWorldSize != new Vector2(12f, 8f))
                throw new InvalidOperationException("RMAP17 camera is not continuously bounded to the full static world.");
            return new BuildReport(plan, removedRoots, ladderSegments, CountTiles(terrain), CountTiles(oneWay),
                CountTiles(ladder), CountTiles(grab), CountTiles(marker), root.GetComponentsInChildren<Collider2D>(true).Length,
                solidComposite.shapeCount, oneWayCollider.shapeCount);
        }

        private static Tilemap FindTilemap(Transform root, string name)
        {
            Transform target = root.Find(name);
            if (target == null || target.GetComponent<Tilemap>() == null)
                throw new InvalidOperationException("RMAP17 saved scene is missing " + name + ".");
            return target.GetComponent<Tilemap>();
        }

        private static int CountTiles(Tilemap map) => map.GetTilesBlock(map.cellBounds).Count(value => value != null);
        private static int UniqueCellCount(IEnumerable<Rmap16Overlay> values) => (values ?? Array.Empty<Rmap16Overlay>())
            .Select(value => value.X + "," + value.Y).Distinct(StringComparer.Ordinal).Count();
        private static Vector3Int ToCell(Rmap16TerrainCell cell) => new Vector3Int(cell.X, cell.Y, 0);
        private static Vector3Int ToCell(Rmap16Overlay overlay) => new Vector3Int(overlay.X, overlay.Y, 0);

        private static TileBase LoadTile(string path, string kind)
        {
            TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (tile == null) throw new InvalidOperationException("RMAP17 missing " + kind + " tile: " + path);
            return tile;
        }

        private static void EnsureSceneDirectory()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(ProjectRoot(), ScenePath.Replace('/', Path.DirectorySeparatorChar))));
        }

        private static void WriteArtifacts(BuildReport report)
        {
            string directory = Path.Combine(ProjectRoot(), GeneratedDirectory.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "rmap17_bake_manifest.json"), ManifestJson(report), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(directory, "rmap17_layer_inventory.csv"), LayerInventoryCsv(report), new UTF8Encoding(false));
            File.WriteAllBytes(Path.Combine(directory, "rmap17_terrain_overview.png"), OverviewPng(report.Plan));
            File.WriteAllBytes(Path.Combine(directory, "rmap17_start_enlarged.png"), RegionPng(report.Plan,
                report.Plan.PlayerStart.X, report.Plan.PlayerStart.Y, 28, 20));
            Rmap16Overlay ladder = report.Plan.LadderOverlays.First();
            File.WriteAllBytes(Path.Combine(directory, "rmap17_ladder_enlarged.png"), RegionPng(report.Plan,
                ladder.X, ladder.Y, 28, 20));
        }

        private static string ManifestJson(BuildReport report)
        {
            Rmap17WorldBakePlan plan = report.Plan;
            return "{\n" +
                "  \"format\": \"RMAP17_STATIC_WORLD_BAKE_V1\",\n" +
                "  \"scene_path\": \"" + ScenePath + "\",\n" +
                "  \"source_plan_digest\": \"" + plan.SourcePlanDigest + "\",\n" +
                "  \"source_cell_digest\": \"" + plan.SourceCellDigest + "\",\n" +
                "  \"bake_digest\": \"" + plan.Digest + "\",\n" +
                "  \"world_tiles\": \"624x416\",\n" +
                "  \"base_cells\": { \"solid\": " + plan.SolidCellCount + ", \"air\": " + plan.AirCellCount +
                ", \"one_way\": " + plan.OneWayCellCount + " },\n" +
                "  \"overlay_cells\": { \"ladder\": " + plan.LadderOverlayCount + ", \"grab\": " +
                plan.GrabOverlayCount + ", \"marker\": " + plan.MarkerOverlayCount + " },\n" +
                "  \"overlay_tile_projection\": \"one tile per unique world coordinate; source overlay records remain exact\",\n" +
                "  \"tilemap_inventory\": { \"solid\": " + report.SolidTileCount + ", \"one_way\": " +
                report.OneWayTileCount + ", \"ladder\": " + report.LadderTileCount + ", \"grab\": " +
                report.GrabTileCount + ", \"marker\": " + report.MarkerTileCount + " },\n" +
                "  \"player_start\": \"" + plan.PlayerStart + "\",\n" +
                "  \"player_collider\": \"0.4x0.8 foot_pivot\",\n" +
                "  \"camera_bounds\": \"0,0,624,416\",\n" +
                "  \"camera_viewport\": \"12x8\",\n" +
                "  \"solid_collider\": \"TilemapCollider2D+Rigidbody2D(static)+CompositeCollider2D\",\n" +
                "  \"one_way_collider\": \"TilemapCollider2D+PlatformEffector2D+CharacterLiveOneWayPlatform\",\n" +
                "  \"ladder_trigger_segments\": " + report.LadderTriggerSegmentCount + ",\n" +
                "  \"scene_collider_count\": " + report.SceneColliderCount + ",\n" +
                "  \"solid_composite_shape_count\": " + report.SolidCompositeShapeCount + ",\n" +
                "  \"one_way_shape_count\": " + report.OneWayShapeCount + ",\n" +
                "  \"rebuild_removed_builder_roots\": " + report.RemovedBuilderRootCount + "\n" +
                "}\n";
        }

        private static string LayerInventoryCsv(BuildReport report) =>
            "layer,tile_count,physical_contract\n" +
            "base_solid," + report.SolidTileCount + ",TilemapCollider2D+CompositeCollider2D\n" +
            "one_way," + report.OneWayTileCount + ",TilemapCollider2D+PlatformEffector2D\n" +
            "ladder," + report.LadderTileCount + "," + report.LadderTriggerSegmentCount + "_CharacterLiveClimbSurface_segments\n" +
            "grab," + report.GrabTileCount + ",CharacterLiveGrabSurface_StaticSafe\n" +
            "marker," + report.MarkerTileCount + ",visual_only_static_state_marker\n";

        private static byte[] OverviewPng(Rmap17WorldBakePlan plan)
        {
            const int width = Rmap17WorldBakePlan.WidthTiles;
            const int height = Rmap17WorldBakePlan.HeightTiles;
            Color32[] pixels = plan.SourceCells.Select(value => BaseColor(value.BaseCell)).ToArray();
            foreach (Rmap16Overlay overlay in plan.LadderOverlays) pixels[(overlay.Y * width) + overlay.X] = new Color32(72, 224, 205, 255);
            foreach (Rmap16Overlay overlay in plan.GrabOverlays) pixels[(overlay.Y * width) + overlay.X] = new Color32(250, 214, 88, 255);
            foreach (Rmap16Overlay overlay in plan.MarkerOverlays) pixels[(overlay.Y * width) + overlay.X] = new Color32(238, 88, 158, 255);
            Fill(pixels, width, plan.PlayerStart.X - 2, plan.PlayerStart.Y - 2, 5, 5, new Color32(255, 255, 255, 255));
            for (int x = 0; x < width; x++) { pixels[x] = new Color32(246, 246, 250, 255); pixels[((height - 1) * width) + x] = new Color32(246, 246, 250, 255); }
            for (int y = 0; y < height; y++) { pixels[y * width] = new Color32(246, 246, 250, 255); pixels[(y * width) + width - 1] = new Color32(246, 246, 250, 255); }
            return Encode(width, height, pixels);
        }

        private static byte[] RegionPng(Rmap17WorldBakePlan plan, int centerX, int centerY, int viewportWidth, int viewportHeight)
        {
            const int scale = 12;
            int minX = Math.Max(0, Math.Min(Rmap17WorldBakePlan.WidthTiles - viewportWidth, centerX - (viewportWidth / 2)));
            int minY = Math.Max(0, Math.Min(Rmap17WorldBakePlan.HeightTiles - viewportHeight, centerY - (viewportHeight / 2)));
            var pixels = new Color32[viewportWidth * scale * viewportHeight * scale];
            for (int y = 0; y < viewportHeight; y++)
            for (int x = 0; x < viewportWidth; x++)
                Fill(pixels, viewportWidth * scale, x * scale, y * scale, scale, scale,
                    BaseColor(plan.GetCell(minX + x, minY + y).BaseCell));
            foreach (Rmap16Overlay overlay in plan.LadderOverlays.Where(value => value.X >= minX && value.X < minX + viewportWidth && value.Y >= minY && value.Y < minY + viewportHeight))
                Fill(pixels, viewportWidth * scale, (overlay.X - minX) * scale, (overlay.Y - minY) * scale, scale, scale, new Color32(72, 224, 205, 255));
            foreach (Rmap16Overlay overlay in plan.GrabOverlays.Where(value => value.X >= minX && value.X < minX + viewportWidth && value.Y >= minY && value.Y < minY + viewportHeight))
                Fill(pixels, viewportWidth * scale, (overlay.X - minX) * scale, (overlay.Y - minY) * scale, scale, scale, new Color32(250, 214, 88, 255));
            foreach (Rmap16Overlay overlay in plan.MarkerOverlays.Where(value => value.X >= minX && value.X < minX + viewportWidth && value.Y >= minY && value.Y < minY + viewportHeight))
                Fill(pixels, viewportWidth * scale, (overlay.X - minX) * scale, (overlay.Y - minY) * scale, scale, scale, new Color32(238, 88, 158, 255));
            if (plan.PlayerStart.X >= minX && plan.PlayerStart.X < minX + viewportWidth && plan.PlayerStart.Y >= minY && plan.PlayerStart.Y < minY + viewportHeight)
                Fill(pixels, viewportWidth * scale, (plan.PlayerStart.X - minX) * scale, (plan.PlayerStart.Y - minY) * scale, scale, scale, new Color32(255, 255, 255, 255));
            return Encode(viewportWidth * scale, viewportHeight * scale, pixels);
        }

        private static Color32 BaseColor(RmapPatternBaseCell value)
        {
            switch (value)
            {
                case RmapPatternBaseCell.Solid: return new Color32(67, 61, 81, 255);
                case RmapPatternBaseCell.Air: return new Color32(95, 157, 182, 255);
                case RmapPatternBaseCell.OneWayPlatform: return new Color32(202, 126, 84, 255);
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private static byte[] Encode(int width, int height, Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            byte[] bytes = ImageConversion.EncodeToPNG(texture);
            UnityEngine.Object.DestroyImmediate(texture);
            return bytes;
        }

        private static void Fill(Color32[] pixels, int width, int x, int y, int fillWidth, int fillHeight, Color32 color)
        {
            int height = pixels.Length / width;
            for (int row = Math.Max(0, y); row < Math.Min(height, y + fillHeight); row++)
            for (int column = Math.Max(0, x); column < Math.Min(width, x + fillWidth); column++)
                pixels[(row * width) + column] = color;
        }

        private static Rmap17WorldBakePlan CreateVerifiedPlan()
        {
            WorldGenerationRngStreams streams = RngStreams();
            RmapWorldDefinition definition = RmapWorldDataGenerator.Generate(
                new RmapWorldDataRequest(1304, "CONTENT_V1", "GENERATOR_V1"), streams);
            return RmapWorldBakeExecutor.Build(definition, streams);
        }

        private static WorldGenerationRngStreams RngStreams()
        {
            var definitions = new SortedDictionary<string, RngStreamDefinition>(StringComparer.Ordinal)
            {
                { "RNG_WORLD_SITE", Rng("RNG_WORLD_SITE", "A13C9E0B2F1044D1", "WORLD") },
                { "RNG_BIOME_PATCH", Rng("RNG_BIOME_PATCH", "B7A91D33E40C5F82", "PASS") },
                { "RNG_ROUTE", Rng("RNG_ROUTE", "C00FEE12AB341901", "PASS") },
                { "RNG_TYPE0", Rng("RNG_TYPE0", "D15EA5E007A4C883", "PASS") },
                { "RNG_SECTOR_RECIPE", Rng("RNG_SECTOR_RECIPE", "E9931A70C2D520F4", "SECTOR") },
                { "RNG_POPULATION", Rng("RNG_POPULATION", "F123456789ABCDEF", "SPAWN") },
            };
            var set = (WorldRouteDefinitionSet)FormatterServices.GetUninitializedObject(typeof(WorldRouteDefinitionSet));
            SetAutoProperty(set, "RngStreams", new ReadOnlyDictionary<string, RngStreamDefinition>(definitions));
            return new WorldGenerationRngStreams(set);
        }

        private static RngStreamDefinition Rng(string id, string salt, string scope)
        {
            var definition = (RngStreamDefinition)FormatterServices.GetUninitializedObject(typeof(RngStreamDefinition));
            SetAutoProperty(definition, "RngStreamId", id);
            SetAutoProperty(definition, "SaltHex", CreateHex(salt));
            SetAutoProperty(definition, "ResetScope", scope);
            SetAutoProperty(definition, "DescriptionKo", "RMAP17 verified RMAP16 handoff input");
            SetAutoProperty(definition, "Active", true);
            return definition;
        }

        private static CsvHexValue CreateHex(string value)
        {
            byte[] bytes = Enumerable.Range(0, value.Length / 2).Select(index => byte.Parse(
                value.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            ConstructorInfo constructor = typeof(CsvHexValue).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(string), typeof(IEnumerable<byte>) }, null);
            if (constructor == null) throw new InvalidOperationException("RMAP17 could not construct the verified RNG salt.");
            return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
        }

        private static void SetAutoProperty(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField("<" + name + ">k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new InvalidOperationException("RMAP17 missing verified RNG property: " + name);
            field.SetValue(target, value);
        }

        private static string ProjectRoot() => Directory.GetParent(Application.dataPath).FullName;

        private sealed class BuildReport
        {
            public BuildReport(Rmap17WorldBakePlan plan, int removedBuilderRootCount, int ladderTriggerSegmentCount,
                int solidTileCount, int oneWayTileCount, int ladderTileCount, int grabTileCount, int markerTileCount,
                int sceneColliderCount, int solidCompositeShapeCount, int oneWayShapeCount)
            {
                Plan = plan;
                RemovedBuilderRootCount = removedBuilderRootCount;
                LadderTriggerSegmentCount = ladderTriggerSegmentCount;
                SolidTileCount = solidTileCount;
                OneWayTileCount = oneWayTileCount;
                LadderTileCount = ladderTileCount;
                GrabTileCount = grabTileCount;
                MarkerTileCount = markerTileCount;
                SceneColliderCount = sceneColliderCount;
                SolidCompositeShapeCount = solidCompositeShapeCount;
                OneWayShapeCount = oneWayShapeCount;
            }

            public Rmap17WorldBakePlan Plan { get; }
            public int RemovedBuilderRootCount { get; }
            public int LadderTriggerSegmentCount { get; }
            public int SolidTileCount { get; }
            public int OneWayTileCount { get; }
            public int LadderTileCount { get; }
            public int GrabTileCount { get; }
            public int MarkerTileCount { get; }
            public int SceneColliderCount { get; }
            public int SolidCompositeShapeCount { get; }
            public int OneWayShapeCount { get; }
        }
    }
}
