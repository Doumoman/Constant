using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using StarNight.Character.Live.Cameras;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Character.Live.Rmap10.Editor
{
    /// <summary>Publishes only the RMAP10 physical small-run scene and its
    /// builder-owned evidence. It replaces no legacy preview or Player prefab.</summary>
    public static class RmapSmallRunSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Map/Scenes/MoonPalace/RMAP10/MoonPalaceSmallRun_RMAP10.unity";
        public const string GeneratedDirectory = "MapDesign/MCP/GENERATED/RMAP10";
        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";
        private const string TerrainTilePath = "Assets/_Game/Live/Prefabs/RMAP02/Tiles/RMAP02_Terrain.asset";
        private const string OverlayTilePath = "Assets/_Game/Live/Prefabs/RMAP02/Tiles/RMAP02_Affordance.asset";
        private static readonly Output Rmap10Output = new Output("RMAP10", ScenePath, GeneratedDirectory,
            "MoonPalace_SmallRun_RMAP10", "MoonPalaceSmallRun");
        private static readonly Output Rmap11Output = new Output("RMAP11",
            "Assets/_Game/Map/Scenes/MoonPalace/RMAP11/MoonPalacePool500_RMAP11.unity",
            "MapDesign/MCP/GENERATED/RMAP11", "MoonPalace_Pool500_RMAP11", "MoonPalacePool500");
        private static readonly Output Rmap11Fix25Output = new Output("RMAP11_FIX25",
            "Assets/_Game/Map/Scenes/MoonPalace/RMAP11/MoonPalacePool500_FIX25_RMAP11.unity",
            "MapDesign/MCP/GENERATED/RMAP11/FIX25", "MoonPalace_Pool500_FIX25_RMAP11", "MoonPalacePool500_FIX25");

        [MenuItem("Tools/MoonPalace/RMAP10/Build Representative Small Run")]
        public static void BuildRepresentativeScene()
        {
            Build(new RmapSmallRunRequest(1107, 36, 24, RmapSmallRunRecipe.PortGalleryV1));
        }

        public static void Build(RmapSmallRunRequest request)
        {
            Build(request, Rmap10Output);
        }

        /// <summary>Builds an isolated RMAP11 scene without rewriting the historical RMAP10 evidence.</summary>
        public static void BuildPool500Review(RmapSmallRunRequest request)
        {
            Build(request, Rmap11Output);
        }

        /// <summary>Publishes FIX25 evidence without overwriting the pre-revision RMAP11 scene or artifacts.</summary>
        public static void BuildPool500Fix25Review(RmapSmallRunRequest request)
        {
            Build(request, Rmap11Fix25Output);
        }

        private static void Build(RmapSmallRunRequest request, Output output)
        {
            RmapSmallRunPlan plan = RmapSmallRunHarness.Generate(request);
            if (!plan.Success) throw new InvalidOperationException(output.Id + " plan failed: " + plan.FailureSummary);
            Tile terrainTile = LoadTile(TerrainTilePath, "terrain");
            Tile overlayTile = LoadTile(OverlayTilePath, "overlay");
            Directory.CreateDirectory(Path.GetDirectoryName(output.ScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(output.RootName, typeof(Grid), typeof(RmapSmallRunSceneState));
            root.GetComponent<RmapSmallRunSceneState>().Configure(plan);
            Tilemap terrain = CreateTerrain(root.transform, output);
            BakeBase(terrain, terrainTile, plan);
            CreateOneWay(root.transform, terrainTile, plan, output);
            CreateLadders(root.transform, overlayTile, plan, output);
            CharacterLivePlayerRig player = CreatePlayer(scene, plan.StartTile, output);
            CreateExit(root.transform, plan.ExitTile, output);
            CreateCamera(root.transform, player.transform, plan, output);
            CreateLabels(root.transform, plan, output);
            Physics2D.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, output.ScenePath);
            AssetDatabase.SaveAssets();
            WriteArtifacts(plan, output);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static Tile LoadTile(string path, string kind)
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null) throw new InvalidOperationException("RMAP10 missing " + kind + " tile: " + path);
            return tile;
        }

        private static Tilemap CreateTerrain(Transform parent, Output output)
        {
            var target = new GameObject(output.Id + "_BaseTerrain", typeof(Tilemap), typeof(TilemapRenderer),
                typeof(TilemapCollider2D), typeof(Rigidbody2D));
            target.transform.SetParent(parent, false);
            target.GetComponent<TilemapRenderer>().sortingOrder = 1;
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            return target.GetComponent<Tilemap>();
        }

        private static void BakeBase(Tilemap tilemap, Tile tile, RmapSmallRunPlan plan)
        {
            var cells = new List<Vector3Int>();
            foreach (RmapSmallRunChunk chunk in plan.Chunks)
            for (var y = 0; y < RmapPortCatalog.ChunkHeight; y++)
            for (var x = 0; x < RmapPortCatalog.ChunkWidth; x++)
                if (chunk.GetBaseCell(x, y) == RmapPatternBaseCell.Solid)
                    cells.Add(new Vector3Int(chunk.OriginX + x, chunk.OriginY + y, 0));
            tilemap.SetTiles(cells.ToArray(), Enumerable.Repeat<TileBase>(tile, cells.Count).ToArray());
            if (cells.Count == 0 || cells.Any(cell => tilemap.GetTile(cell) == null))
                throw new InvalidOperationException("RMAP10 base Tilemap did not retain the generated solid cells.");
            EditorUtility.SetDirty(tilemap);
        }

        private static void CreateOneWay(Transform parent, Tile tile, RmapSmallRunPlan plan, Output output)
        {
            var target = new GameObject(output.Id + "_OneWayOverlay", typeof(Tilemap), typeof(TilemapRenderer),
                typeof(TilemapCollider2D), typeof(Rigidbody2D), typeof(PlatformEffector2D), typeof(CharacterLiveOneWayPlatform));
            target.transform.SetParent(parent, false);
            target.GetComponent<TilemapRenderer>().sortingOrder = 2;
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            TilemapCollider2D collider = target.GetComponent<TilemapCollider2D>();
            collider.usedByEffector = true;
            target.GetComponent<PlatformEffector2D>().useOneWay = true;
            target.GetComponent<CharacterLiveOneWayPlatform>().Configure(collider);
            Tilemap tilemap = target.GetComponent<Tilemap>();
            Vector3Int[] cells = plan.Chunks.SelectMany(chunk => Enumerable.Range(0, RmapPortCatalog.ChunkCellCount)
                .Where(index => chunk.BaseCells[index] == RmapPatternBaseCell.OneWayPlatform)
                .Select(index => new Vector3Int(chunk.OriginX + (index % RmapPortCatalog.ChunkWidth),
                    chunk.OriginY + (index / RmapPortCatalog.ChunkWidth), 0))).ToArray();
            if (cells.Length == 0) throw new InvalidOperationException(output.Id + " requires selected one-way output.");
            tilemap.SetTiles(cells, Enumerable.Repeat<TileBase>(tile, cells.Length).ToArray());
            EditorUtility.SetDirty(tilemap);
        }

        private static void CreateLadders(Transform parent, Tile tile, RmapSmallRunPlan plan, Output output)
        {
            var target = new GameObject(output.Id + "_LadderOverlay", typeof(Tilemap), typeof(TilemapRenderer),
                typeof(BoxCollider2D), typeof(CharacterLiveClimbSurface));
            target.transform.SetParent(parent, false);
            target.GetComponent<TilemapRenderer>().sortingOrder = 3;
            Vector3Int[] cells = plan.Chunks.SelectMany(chunk => chunk.Overlays.Select(overlay =>
                new Vector3Int(chunk.OriginX + overlay.Cell.X, chunk.OriginY + overlay.Cell.Y, 0))).ToArray();
            if (cells.Length == 0) throw new InvalidOperationException(output.Id + " requires RMAP09's Type3 ladder overlay.");
            target.GetComponent<Tilemap>().SetTiles(cells, Enumerable.Repeat<TileBase>(tile, cells.Length).ToArray());
            float centerX = (float)cells.Average(cell => cell.x) + 0.5f;
            float minY = cells.Min(cell => cell.y);
            BoxCollider2D trigger = target.GetComponent<BoxCollider2D>();
            trigger.offset = new Vector2(centerX, minY + (cells.Length * 0.5f));
            trigger.size = new Vector2(0.7f, cells.Length);
            trigger.isTrigger = true;
            target.GetComponent<CharacterLiveClimbSurface>().Configure(CharacterLiveClimbSurface.SurfaceKind.Ladder, trigger);
        }

        private static CharacterLivePlayerRig CreatePlayer(Scene scene, Vector2Int start, Output output)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null) throw new InvalidOperationException(output.Id + " Player prefab is missing.");
            GameObject player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            player.name = output.Id + "_Player";
            CharacterLivePlayerRig rig = player.GetComponent<CharacterLivePlayerRig>();
            CharacterLiveMovementDriver movement = player.GetComponent<CharacterLiveMovementDriver>();
            if (rig == null || movement == null) throw new InvalidOperationException(output.Id + " requires the Production Player rig and movement driver.");
            rig.BodyCollider.size = new Vector2(0.4f, 0.8f);
            rig.BodyCollider.offset = new Vector2(0f, 0.4f);
            movement.ConfigureRmap02(1 << LayerMask.NameToLayer("Default"));
            rig.Body.position = new Vector2(start.x + 0.5f, start.y);
            movement.ResetMotion();
            return rig;
        }

        private static void CreateExit(Transform parent, Vector2Int exit, Output output)
        {
            var target = new GameObject(output.Id + "_Exit", typeof(BoxCollider2D), typeof(CharacterLiveMapRunExit));
            target.transform.SetParent(parent, false);
            target.transform.position = new Vector3(exit.x + 0.5f, exit.y + 0.7f, 0f);
            BoxCollider2D trigger = target.GetComponent<BoxCollider2D>();
            trigger.size = new Vector2(1f, 1.5f);
            trigger.isTrigger = true;
        }

        private static void CreateCamera(Transform parent, Transform player, RmapSmallRunPlan plan, Output output)
        {
            var target = new GameObject(output.Id + "_FollowCamera", typeof(Camera), typeof(CharacterLiveCameraFollowDriver));
            target.transform.SetParent(parent, false);
            Camera camera = target.GetComponent<Camera>();
            camera.orthographic = true; camera.nearClipPlane = 0.1f; camera.farClipPlane = 100f;
            target.GetComponent<CharacterLiveCameraFollowDriver>().Configure(camera, player,
                new Rect(0f, 0f, plan.Request.Width, plan.Request.Height + 1f), 12f, 8f, 0.08f);
        }

        private static void CreateLabels(Transform parent, RmapSmallRunPlan plan, Output output)
        {
            var target = new GameObject(output.Id + "_Controls", typeof(TextMesh));
            target.transform.SetParent(parent, false);
            target.transform.position = new Vector3(0.25f, plan.Request.Height + 0.6f, -1f);
            TextMesh text = target.GetComponent<TextMesh>();
            text.text = output.Id + " | " + plan.PoolVersion + " | Seed " + plan.Request.Seed.ToString(CultureInfo.InvariantCulture) + " | " +
                plan.Request.Width + "x" + plan.Request.Height + " | " + plan.Request.RecipeId +
                "\nA/D or arrows: move to Exit | ladder/one-way are generated separately | " + plan.PlanDigest.Substring(0, 12);
            text.color = new Color(0.85f, 0.94f, 1f, 1f); text.characterSize = 0.18f; text.fontSize = 26;
            text.anchor = TextAnchor.UpperLeft;
        }

        private static void WriteArtifacts(RmapSmallRunPlan active, Output output)
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(root, output.GeneratedDirectory.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(directory);
            RmapSmallRunPlan[] cases =
            {
                active,
                RmapSmallRunHarness.Generate(new RmapSmallRunRequest(2203, 48, 24, RmapSmallRunRecipe.PortGalleryV1)),
                RmapSmallRunHarness.Generate(new RmapSmallRunRequest(3301, 36, 24, RmapSmallRunRecipe.PortGalleryV1)),
            };
            if (cases.Any(value => !value.Success)) throw new InvalidOperationException(output.Id + " representative generation failed.");
            Write(directory, output.ArtifactPrefix + "_seed_evidence.csv", SeedCsv(cases));
            Write(directory, output.ArtifactPrefix + "_chunk_plan.csv", ChunkCsv(active));
            Write(directory, output.ArtifactPrefix + "_selected_patterns.csv", SelectionCsv(active));
            Write(directory, output.ArtifactPrefix + "_ports.csv", PortCsv(active));
            Write(directory, output.ArtifactPrefix + "_base_cells.csv", BaseCsv(active));
            Write(directory, output.ArtifactPrefix + "_overlay.csv", OverlayCsv(active));
            Write(directory, output.ArtifactPrefix + "_manifest.json", Manifest(active, cases, output));
        }

        private static void Write(string directory, string name, string content)
        {
            File.WriteAllText(Path.Combine(directory, name), content, new UTF8Encoding(false));
        }

        private static string SeedCsv(IEnumerable<RmapSmallRunPlan> plans)
        {
            var text = new StringBuilder("Seed,Width,Height,Recipe,Success,PlanDigest,BaseDigest,Chunks,Ports,Attempts,Start,Exit,ActualPlay\n");
            foreach (RmapSmallRunPlan plan in plans) text.Append(plan.Request.Seed).Append(',').Append(plan.Request.Width).Append(',').Append(plan.Request.Height).Append(',')
                .Append(plan.Request.RecipeId).Append(",true,").Append(plan.PlanDigest).Append(',').Append(plan.BaseDigest).Append(',')
                .Append(plan.Chunks.Count).Append(',').Append(plan.Ports.Count).Append(',').Append(plan.TotalSelectionAttempts).Append(',')
                .Append(plan.StartTile.x).Append(';').Append(plan.StartTile.y).Append(',').Append(plan.ExitTile.x).Append(';').Append(plan.ExitTile.y).Append(',')
                .Append(plan.Request.Seed == 1107 ? "scheduled_playmode" : "not_run_playmode").Append('\n');
            return text.ToString();
        }

        private static string ChunkCsv(RmapSmallRunPlan plan)
        {
            var text = new StringBuilder("InstanceId,Origin,SourceChunk,SpaceState,Type,Selections,BaseCells,Overlays,Attempts\n");
            foreach (RmapSmallRunChunk chunk in plan.Chunks) text.Append(chunk.InstanceId).Append(',').Append(chunk.OriginX).Append(';').Append(chunk.OriginY).Append(',')
                .Append(chunk.Source.ChunkId).Append(',').Append(chunk.Source.SpaceState).Append(',').Append(chunk.Source.ChunkType.HasValue ? chunk.Source.ChunkType.ToString() : "NONE").Append(',')
                .Append(chunk.Selections.Count).Append(',').Append(chunk.BaseCells.Count).Append(',').Append(chunk.Overlays.Count).Append(',').Append(chunk.AttemptCount).Append('\n');
            return text.ToString();
        }

        private static string SelectionCsv(RmapSmallRunPlan plan)
        {
            var text = new StringBuilder("InstanceId,SlotOrigin,CandidateId,Transform,FinalCells16\n");
            foreach (RmapSmallRunChunk chunk in plan.Chunks) foreach (RmapSmallRunPatternSelection selection in chunk.Selections)
                text.Append(chunk.InstanceId).Append(',').Append(selection.SlotX).Append(';').Append(selection.SlotY).Append(',')
                    .Append(selection.CandidateId).Append(',').Append(selection.Transform).Append(',').Append(selection.FinalCells16).Append('\n');
            return text.ToString();
        }

        private static string PortCsv(RmapSmallRunPlan plan)
        {
            var text = new StringBuilder("InstanceId,SourceChunk,PortId,Side,Traversal,Flow,Required,GlobalCells\n");
            foreach (RmapSmallRunPort port in plan.Ports) text.Append(port.ChunkInstanceId).Append(',').Append(port.ChunkId).Append(',').Append(port.PortId).Append(',')
                .Append(port.Side).Append(',').Append(port.TraversalKind).Append(',').Append(port.FlowDirection).Append(',').Append(port.Required ? "true" : "false").Append(',')
                .Append(string.Join(";", port.GlobalCells.Select(cell => cell.x + ":" + cell.y))).Append('\n');
            return text.ToString();
        }

        private static string BaseCsv(RmapSmallRunPlan plan)
        {
            var text = new StringBuilder("X,Y,BaseCell,Chunk\n");
            foreach (RmapSmallRunChunk chunk in plan.Chunks) for (var y = 0; y < 8; y++) for (var x = 0; x < 12; x++)
                text.Append(chunk.OriginX + x).Append(',').Append(chunk.OriginY + y).Append(',').Append(chunk.GetBaseCell(x, y)).Append(',').Append(chunk.InstanceId).Append('\n');
            return text.ToString();
        }

        private static string OverlayCsv(RmapSmallRunPlan plan)
        {
            var text = new StringBuilder("X,Y,Kind,SourceChunk\n");
            foreach (RmapSmallRunChunk chunk in plan.Chunks) foreach (RmapComposerOverlayCell overlay in chunk.Overlays)
                text.Append(chunk.OriginX + overlay.Cell.X).Append(',').Append(chunk.OriginY + overlay.Cell.Y).Append(',').Append(overlay.Kind).Append(',').Append(chunk.Source.ChunkId).Append('\n');
            return text.ToString();
        }

        private static string Manifest(RmapSmallRunPlan active, IEnumerable<RmapSmallRunPlan> plans, Output output)
        {
            return "{\n" +
                "  \"scene_path\": \"" + output.ScenePath + "\",\n" +
                "  \"active_seed\": " + active.Request.Seed + ",\n" +
                "  \"active_size\": \"" + active.Request.Width + "x" + active.Request.Height + "\",\n" +
                "  \"recipe\": \"" + active.Request.RecipeId + "\",\n" +
                "  \"pool_version\": \"" + active.PoolVersion + "\",\n" +
                "  \"plan_digest\": \"" + active.PlanDigest + "\",\n" +
                "  \"profile_digest\": \"" + RmapPortCatalog.BuildFixture().ProfileDigest + "\",\n" +
                "  \"physical_path\": \"" + output.Id + "_BaseTerrain TilemapCollider2D; " + output.Id + "_OneWayOverlay TilemapCollider2D+PlatformEffector2D; " + output.Id + "_LadderOverlay trigger; " + output.Id + "_Player; " + output.Id + "_Exit\",\n" +
                "  \"representative_digests\": \"" + string.Join(";", plans.Select(value => value.Request.Seed + ":" + value.PlanDigest)) + "\",\n" +
                "  \"supported_sizes\": \"36x24;48x24\",\n" +
                "  \"regenerate\": \"Tools/MoonPalace/RMAP10/Small Run Generator\"\n" +
                "}\n";
        }

        private sealed class Output
        {
            public Output(string id, string scenePath, string generatedDirectory, string rootName, string artifactPrefix)
            {
                Id = id; ScenePath = scenePath; GeneratedDirectory = generatedDirectory; RootName = rootName;
                ArtifactPrefix = artifactPrefix;
            }
            public string Id { get; }
            public string ScenePath { get; }
            public string GeneratedDirectory { get; }
            public string RootName { get; }
            public string ArtifactPrefix { get; }
        }
    }
}
