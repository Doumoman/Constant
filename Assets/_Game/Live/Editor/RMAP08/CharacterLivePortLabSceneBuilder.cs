using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using StarNight.Character.Live.Cameras;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using StarNight.Map.WorldGeneration.MicroPatterns;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Character.Live.Rmap08.Editor
{
    /// <summary>
    /// Builds the isolated RMAP08 port lab.  This is a fixed evidence fixture,
    /// not RMAP09's automatic 3x2 chunk composer.
    /// </summary>
    public static class CharacterLivePortLabSceneBuilder
    {
        public const string ScenePath =
            "Assets/_Game/Map/Scenes/MoonPalace/RMAP08/MoonPalacePortLab_RMAP08.unity";

        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";
        private const string TerrainTilePath = "Assets/_Game/Live/Prefabs/RMAP02/Tiles/RMAP02_Terrain.asset";
        private const string GeneratedDirectory = "MapDesign/MCP/GENERATED/RMAP08";
        private const string DerivedAuthoringDirectory =
            "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RMAP08";

        private static readonly Dictionary<string, Vector2Int> Origins = new Dictionary<string, Vector2Int>
        {
            { "T1_A", new Vector2Int(0, 0) },
            { "T1_B", new Vector2Int(12, 0) },
            { "T1_BLOCKED_SOURCE", new Vector2Int(28, 0) },
            { "INACTIVE_SOLID_WALL", new Vector2Int(40, 0) },
            { "T2_DROP", new Vector2Int(60, 0) },
            { "T3_CLIMB", new Vector2Int(78, 0) },
            { "T4_SPLIT", new Vector2Int(0, 14) },
            { "T0_SINGLE_ENTRANCE", new Vector2Int(16, 14) },
            { "T0_BREAKABLE_SECRET", new Vector2Int(32, 14) },
            { "SPECIAL_RESERVED_START", new Vector2Int(48, 14) },
        };

        [MenuItem("Tools/MoonPalace/RMAP08/Build Port Lab")]
        public static void BuildFromMenu() => Build();

        public static void Build()
        {
            RmapPortCatalogSnapshot catalog = RmapPortCatalog.BuildFixture();
            RmapPortValidationResult validation = RmapPortCatalog.Validate(catalog);
            if (!validation.IsValid) throw new InvalidOperationException(string.Join(";", validation.Errors));
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Tile terrainTile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
            if (terrainTile == null) throw new InvalidOperationException("RMAP02 terrain tile is missing: " + TerrainTilePath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("MoonPalace_PortLab_RMAP08", typeof(Grid));
            foreach (RmapPortChunk chunk in catalog.Chunks)
            {
                if (!Origins.TryGetValue(chunk.ChunkId, out Vector2Int origin))
                    throw new InvalidOperationException("RMAP08 scene origin is missing: " + chunk.ChunkId);
                Tilemap tilemap = CreateChunkTilemap(root.transform, chunk, origin, terrainTile);
                CreateChunkLabel(root.transform, chunk, origin);
                CreatePortLabels(root.transform, chunk, origin);
                if (chunk.ChunkId == "T3_CLIMB") CreateClimbAxis(root.transform, origin);
                if (chunk.ChunkId == "T0_BREAKABLE_SECRET") CreateBreakableMarker(root.transform, origin);
                if (tilemap.GetComponent<TilemapCollider2D>() == null)
                    throw new InvalidOperationException("Every RMAP08 chunk must have a physical TilemapCollider2D.");
            }

            CreateDropLanding(root.transform, terrainTile);
            CharacterLivePlayerRig rig = CreatePlayer(scene);
            CreateCamera(root.transform, rig.transform);
            CreateLegend(root.transform);
            Physics2D.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            WriteArtifacts(catalog);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static Tilemap CreateChunkTilemap(Transform parent, RmapPortChunk chunk, Vector2Int origin, Tile tile)
        {
            var target = new GameObject("RMAP08_Chunk_" + chunk.ChunkId, typeof(Tilemap), typeof(TilemapRenderer),
                typeof(TilemapCollider2D), typeof(Rigidbody2D));
            target.transform.SetParent(parent, false);
            Tilemap tilemap = target.GetComponent<Tilemap>();
            TilemapRenderer renderer = target.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = 1;
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            Vector3Int[] positions = chunk.SolidCells.Select(cell =>
                new Vector3Int(origin.x + cell.X, origin.y + cell.Y, 0)).ToArray();
            tilemap.SetTiles(positions, Enumerable.Repeat<TileBase>(tile, positions.Length).ToArray());
            if (chunk.SolidCells.Count > 0)
            {
                RmapPortCell first = chunk.SolidCells[0];
                if (tilemap.GetTile(new Vector3Int(origin.x + first.X, origin.y + first.Y, 0)) == null)
                    throw new InvalidOperationException("RMAP08 Tilemap write did not retain a chunk solid cell: " + chunk.ChunkId);
            }
            EditorUtility.SetDirty(tilemap);
            return tilemap;
        }

        private static void CreateClimbAxis(Transform parent, Vector2Int origin)
        {
            var axis = new GameObject("RMAP08_T3_ClimbAxis", typeof(BoxCollider2D), typeof(CharacterLiveClimbSurface));
            axis.transform.SetParent(parent, false);
            axis.transform.position = new Vector2(origin.x + 5.5f, origin.y + 4f);
            BoxCollider2D trigger = axis.GetComponent<BoxCollider2D>();
            trigger.size = new Vector2(0.7f, 8f);
            trigger.isTrigger = true;
            axis.GetComponent<CharacterLiveClimbSurface>().Configure(CharacterLiveClimbSurface.SurfaceKind.Ladder, trigger);
        }

        private static void CreateDropLanding(Transform parent, Tile tile)
        {
            var target = new GameObject("RMAP08_T2_DropLanding", typeof(Tilemap), typeof(TilemapRenderer),
                typeof(TilemapCollider2D), typeof(Rigidbody2D));
            target.transform.SetParent(parent, false);
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            Tilemap tilemap = target.GetComponent<Tilemap>();
            Vector3Int[] positions = Enumerable.Range(65, 8).Select(x => new Vector3Int(x, -3, 0)).ToArray();
            tilemap.SetTiles(positions, Enumerable.Repeat<TileBase>(tile, positions.Length).ToArray());
            EditorUtility.SetDirty(tilemap);
        }

        private static void CreateBreakableMarker(Transform parent, Vector2Int origin)
        {
            var marker = new GameObject("RMAP08_BreakableAccess_NotAnEdgePort", typeof(TextMesh));
            marker.transform.SetParent(parent, false);
            marker.transform.position = new Vector3(origin.x + 11.1f, origin.y + 3f, -1f);
            TextMesh text = marker.GetComponent<TextMesh>();
            text.text = "BREAKABLE\nnot EdgePort";
            text.color = new Color(1f, 0.62f, 0.18f, 1f);
            text.characterSize = 0.12f;
            text.fontSize = 18;
            text.anchor = TextAnchor.MiddleLeft;
        }

        private static CharacterLivePlayerRig CreatePlayer(Scene scene)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null) throw new InvalidOperationException("RMAP08 Player prefab is missing: " + PlayerPrefabPath);
            var player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            player.name = "RMAP08_Player";
            CharacterLivePlayerRig rig = player.GetComponent<CharacterLivePlayerRig>();
            CharacterLiveMovementDriver movement = player.GetComponent<CharacterLiveMovementDriver>();
            if (rig == null || movement == null) throw new InvalidOperationException("RMAP08 Player rig/movement is missing.");
            rig.BodyCollider.size = new Vector2(0.4f, 0.8f);
            rig.BodyCollider.offset = new Vector2(0f, 0.4f);
            movement.ConfigureRmap02(1 << LayerMask.NameToLayer("Default"));
            var bootstrap = new GameObject("RMAP08_Bootstrap").AddComponent<CharacterLiveMapRunBootstrap>();
            bootstrap.Configure(rig, movement, new Vector2(2f, 1f));
            return rig;
        }

        private static void CreateCamera(Transform parent, Transform target)
        {
            var cameraObject = new GameObject("RMAP08_FollowCamera", typeof(Camera),
                typeof(CharacterLiveCameraFollowDriver));
            cameraObject.transform.SetParent(parent, false);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.033f, 0.05f, 1f);
            cameraObject.GetComponent<CharacterLiveCameraFollowDriver>().Configure(camera, target,
                new Rect(0f, -3f, 92f, 25f), 14f, 9f, 0.08f);
        }

        private static void CreateChunkLabel(Transform parent, RmapPortChunk chunk, Vector2Int origin)
        {
            var label = new GameObject("Label_" + chunk.ChunkId, typeof(TextMesh));
            label.transform.SetParent(parent, false);
            label.transform.position = new Vector3(origin.x, origin.y + RmapPortCatalog.ChunkHeight + 0.35f, -1f);
            TextMesh text = label.GetComponent<TextMesh>();
            text.text = chunk.ChunkId + "\nstate=" + chunk.SpaceState + " type=" +
                (chunk.ChunkType.HasValue ? chunk.ChunkType.Value.ToString() : "NONE");
            text.color = chunk.SpaceState == RmapPortSpaceState.InactiveSolid
                ? new Color(0.76f, 0.36f, 0.36f, 1f) : new Color(0.9f, 0.94f, 1f, 1f);
            text.characterSize = 0.11f;
            text.fontSize = 18;
            text.anchor = TextAnchor.LowerLeft;
        }

        private static void CreatePortLabels(Transform parent, RmapPortChunk chunk, Vector2Int origin)
        {
            foreach (RmapEdgePort port in chunk.Ports)
            {
                int coordinate = port.OpenCells[0];
                RmapPortCell cell = RmapPortCatalog.ToChunkCell(port.Side, coordinate);
                var label = new GameObject("Port_" + chunk.ChunkId + "_" + port.PortId, typeof(TextMesh));
                label.transform.SetParent(parent, false);
                label.transform.position = new Vector3(origin.x + cell.X + 0.08f, origin.y + cell.Y + 0.18f, -1f);
                TextMesh text = label.GetComponent<TextMesh>();
                text.text = port.PortId + " " + port.Side + "[" + string.Join(";", port.OpenCells) + "] " +
                    port.FlowDirection;
                text.color = port.Required ? new Color(0.36f, 1f, 0.66f, 1f) : new Color(0.76f, 0.82f, 1f, 1f);
                text.characterSize = 0.075f;
                text.fontSize = 14;
                text.anchor = TextAnchor.LowerLeft;
            }
        }

        private static void CreateLegend(Transform parent)
        {
            var label = new GameObject("RMAP08_Controls", typeof(TextMesh));
            label.transform.SetParent(parent, false);
            label.transform.position = new Vector3(1f, 26f, -1f);
            TextMesh text = label.GetComponent<TextMesh>();
            text.text = "RMAP08 | fixed 12x8 port lab (not RMAP09 composition)\n" +
                "A/D or arrows: move | Space: jump | W/S or Up/Down: Type3 climb\n" +
                "green=required EdgePort; orange=BreakableAccess (not an EdgePort); labels retain every edge coordinate.";
            text.color = new Color(0.9f, 0.94f, 1f, 1f);
            text.characterSize = 0.18f;
            text.fontSize = 24;
            text.anchor = TextAnchor.UpperLeft;
        }

        private static void WriteArtifacts(RmapPortCatalogSnapshot catalog)
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string generated = Path.Combine(root, GeneratedDirectory.Replace('/', Path.DirectorySeparatorChar));
            string derived = Path.Combine(root, DerivedAuthoringDirectory.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(generated);
            Directory.CreateDirectory(derived);
            WriteDerivedCsv(generated, derived, "rmap08_chunk_catalog.csv", RmapPortCatalog.ExportChunksCsv(catalog));
            WriteDerivedCsv(generated, derived, "rmap08_edge_ports.csv", RmapPortCatalog.ExportPortsCsv(catalog));
            WriteDerivedCsv(generated, derived, "rmap08_adjacency_connections.csv", RmapPortCatalog.ExportAdjacencyCsv(catalog));
            WriteDerivedCsv(generated, derived, "rmap08_interior_connections.csv", RmapPortCatalog.ExportInteriorLinksCsv(catalog));
            WriteDerivedCsv(generated, derived, "rmap08_breakable_access.csv", RmapPortCatalog.ExportBreakableAccessCsv(catalog));
            File.WriteAllText(Path.Combine(generated, "rmap08_data_snapshot.json"),
                RmapPortCatalog.ExportSnapshotJson(catalog), new UTF8Encoding(false));
            string manifest = "{\n" +
                "  \"scene_path\": \"" + ScenePath + "\",\n" +
                "  \"chunk_size_tiles\": \"12x8\",\n" +
                "  \"source_rule\": \"" + RmapPortCatalog.Rmap07SourcePlacementRule + "\",\n" +
                "  \"profile_digest\": \"" + catalog.ProfileDigest + "\",\n" +
                "  \"open_boundary\": \"T1_A.R -> T1_B.L at y=1;2\",\n" +
                "  \"blocked_boundary\": \"T1_BLOCKED_SOURCE.R -> INACTIVE_SOLID_WALL (no target port)\",\n" +
                "  \"type2_drop\": \"T2_DROP L->D physical floor opening x=6;7;8;9 and landing y=-3\",\n" +
                "  \"type3_climb\": \"T3_CLIMB L->U uses CharacterLiveClimbSurface in the Tilemap chunk\",\n" +
                "  \"player\": \"RMAP08_Player; 0.4x0.8 foot-pivot; RMAP02 collision setup\",\n" +
                "  \"derived_authoring_csv\": \"" + DerivedAuthoringDirectory + "; builder-owned, never hand-edited\"\n" +
                "}\n";
            File.WriteAllText(Path.Combine(generated, "rmap08_fixture_manifest.json"), manifest, new UTF8Encoding(false));
        }

        private static void WriteDerivedCsv(string generated, string derived, string fileName, string content)
        {
            File.WriteAllText(Path.Combine(generated, fileName), content, new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(derived, fileName), content, new UTF8Encoding(false));
        }
    }
}
