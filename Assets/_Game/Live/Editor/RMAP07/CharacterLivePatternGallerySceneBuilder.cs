using System;
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

namespace StarNight.Character.Live.Rmap07.Editor
{
    /// <summary>Builds only the RMAP07 48-candidate gallery and physical fixtures.</summary>
    public static class CharacterLivePatternGallerySceneBuilder
    {
        public const string ScenePath =
            "Assets/_Game/Map/Scenes/MoonPalace/RMAP07/MoonPalacePatternGallery_RMAP07.unity";

        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";
        private const string TerrainTilePath = "Assets/_Game/Live/Prefabs/RMAP02/Tiles/RMAP02_Terrain.asset";
        private const string GeneratedDirectory = "MapDesign/MCP/GENERATED/RMAP07";
        private const string DerivedAuthoringDirectory =
            "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RMAP07";
        private const int GalleryWidth = 48;
        private const int GalleryHeight = 56;
        private const int GalleryColumns = 6;

        [MenuItem("Tools/MoonPalace/RMAP07/Build Pattern Gallery")]
        public static void BuildFromMenu() => Build();

        public static void Build()
        {
            RmapPatternCatalogSnapshot catalog = RmapPatternCatalog.BuildInitialPool();
            if (catalog.Candidates.Count != RmapPatternCatalog.InitialPoolCount)
                throw new InvalidOperationException("RMAP07 requires its deterministic 48-candidate first pool.");

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("MoonPalace_PatternGallery_RMAP07", typeof(Grid));
            Tile terrainTile = LoadTerrainTile();
            Tilemap solidGallery = CreateSolidTilemap(root.transform, "RMAP07_SolidGallery");
            Tilemap oneWayGallery = CreateOneWayTilemap(root.transform, "RMAP07_OneWayGallery");
            PopulateGallery(root.transform, solidGallery, oneWayGallery, terrainTile, catalog);
            CreateGround(solidGallery, terrainTile);
            CreateOneWayFixtures(root.transform, terrainTile);

            CharacterLivePlayerRig rig = CreatePlayer(scene);
            CreateCamera(root.transform, rig.transform);
            CreateLabel(root.transform);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            WriteArtifacts(catalog);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static Tile LoadTerrainTile()
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
            if (tile == null) throw new InvalidOperationException("RMAP02 terrain tile is missing: " + TerrainTilePath);
            return tile;
        }

        private static Tilemap CreateSolidTilemap(Transform parent, string name)
        {
            var target = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D),
                typeof(Rigidbody2D));
            target.transform.SetParent(parent, false);
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            target.GetComponent<TilemapRenderer>().sortingOrder = 1;
            return target.GetComponent<Tilemap>();
        }

        private static Tilemap CreateOneWayTilemap(Transform parent, string name)
        {
            var target = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D),
                typeof(Rigidbody2D), typeof(PlatformEffector2D), typeof(CharacterLiveOneWayPlatform),
                typeof(CharacterLiveGrabSurface));
            target.transform.SetParent(parent, false);
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            TilemapCollider2D collider = target.GetComponent<TilemapCollider2D>();
            collider.usedByEffector = true;
            target.GetComponent<PlatformEffector2D>().useOneWay = true;
            target.GetComponent<CharacterLiveOneWayPlatform>().Configure(collider);
            target.GetComponent<CharacterLiveGrabSurface>().Configure(CharacterLiveGrabSurface.SurfaceKind.OneWay);
            target.GetComponent<TilemapRenderer>().sortingOrder = 2;
            return target.GetComponent<Tilemap>();
        }

        private static void PopulateGallery(Transform parent, Tilemap solids, Tilemap oneWays, Tile tile,
            RmapPatternCatalogSnapshot catalog)
        {
            for (var index = 0; index < catalog.Candidates.Count; index++)
            {
                RmapPatternCandidate candidate = catalog.Candidates[index];
                int originX = (index % GalleryColumns) * RmapPatternCatalog.Width;
                int originY = 20 + (index / GalleryColumns) * RmapPatternCatalog.Height;
                for (var y = 0; y < RmapPatternCatalog.Height; y++)
                for (var x = 0; x < RmapPatternCatalog.Width; x++)
                {
                    RmapPatternBaseCell cell = candidate.GetCell(x, y);
                    var position = new Vector3Int(originX + x, originY + y, 0);
                    if (cell == RmapPatternBaseCell.Solid) solids.SetTile(position, tile);
                    if (cell == RmapPatternBaseCell.OneWayPlatform) oneWays.SetTile(position, tile);
                }

                CreateCandidateLabel(parent, candidate, new Vector3(originX, originY - 0.35f, -1f));
            }
        }

        private static void CreateGround(Tilemap solids, Tile tile)
        {
            for (var x = 0; x < GalleryWidth; x++) solids.SetTile(new Vector3Int(x, 0, 0), tile);
        }

        private static void CreateOneWayFixtures(Transform parent, Tile tile)
        {
            RmapPatternTransform[] transforms =
            {
                RmapPatternTransform.R0, RmapPatternTransform.MirrorX,
                RmapPatternTransform.MirrorY, RmapPatternTransform.R180
            };
            for (var index = 0; index < transforms.Length; index++)
            {
                Tilemap fixture = CreateOneWayTilemap(parent, "RMAP07_OneWay_" + transforms[index]);
                // The gallery itself is 1x1-cell Tilemap terrain.  The four
                // isolated verification strips reuse RMAP04's thin top-only
                // physical profile so the baseline Player can pass and land.
                fixture.transform.localScale = new Vector3(1f, 0.20f, 1f);
                int startX = 28 + index * 5;
                for (var x = 0; x < 3; x++) fixture.SetTile(new Vector3Int(startX + x, 10, 0), tile);
            }
        }

        private static CharacterLivePlayerRig CreatePlayer(Scene scene)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null) throw new InvalidOperationException("RMAP07 Player prefab is missing: " + PlayerPrefabPath);
            var player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            player.name = "RMAP07_Player";
            CharacterLivePlayerRig rig = player.GetComponent<CharacterLivePlayerRig>();
            CharacterLiveMovementDriver movement = player.GetComponent<CharacterLiveMovementDriver>();
            if (rig == null || movement == null) throw new InvalidOperationException("RMAP07 Player rig/movement is missing.");
            rig.BodyCollider.size = new Vector2(0.4f, 0.8f);
            rig.BodyCollider.offset = new Vector2(0f, 0.4f);
            movement.ConfigureRmap02(1 << LayerMask.NameToLayer("Default"));
            var bootstrap = new GameObject("RMAP07_Bootstrap").AddComponent<CharacterLiveMapRunBootstrap>();
            bootstrap.Configure(rig, movement, new Vector2(2f, 1f));
            return rig;
        }

        private static void CreateCamera(Transform parent, Transform target)
        {
            var cameraObject = new GameObject("RMAP07_FollowCamera", typeof(Camera),
                typeof(CharacterLiveCameraFollowDriver));
            cameraObject.transform.SetParent(parent, false);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.033f, 0.05f, 1f);
            cameraObject.GetComponent<CharacterLiveCameraFollowDriver>().Configure(camera, target,
                new Rect(0f, 0f, GalleryWidth, GalleryHeight), 12f, 8f, 0.08f);
        }

        private static void CreateCandidateLabel(Transform parent, RmapPatternCandidate candidate, Vector3 position)
        {
            var label = new GameObject("Label_" + candidate.CandidateId, typeof(TextMesh));
            label.transform.SetParent(parent, false);
            label.transform.position = position;
            TextMesh text = label.GetComponent<TextMesh>();
            text.text = candidate.CandidateId.Substring("RMAP07_".Length, 6) + "\n" + candidate.PrimaryRole;
            text.color = new Color(0.9f, 0.94f, 1f, 1f);
            text.characterSize = 0.10f;
            text.fontSize = 20;
            text.anchor = TextAnchor.UpperLeft;
        }

        private static void CreateLabel(Transform parent)
        {
            var label = new GameObject("RMAP07_Controls", typeof(TextMesh));
            label.transform.SetParent(parent, false);
            label.transform.position = new Vector3(1f, 17.5f, -1f);
            TextMesh text = label.GetComponent<TextMesh>();
            text.text = "RMAP07  |  48 typed 4x4 candidates (A=air, S=solid, O=one-way)\n"
                + "A/D or arrows: move  |  Space: jump  |  right-side fixtures: R0/MirrorX/MirrorY/R180 one-way.";
            text.color = new Color(0.9f, 0.94f, 1f, 1f);
            text.characterSize = 0.18f;
            text.fontSize = 24;
            text.anchor = TextAnchor.UpperLeft;
        }

        private static void WriteArtifacts(RmapPatternCatalogSnapshot catalog)
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(root, GeneratedDirectory.Replace('/', Path.DirectorySeparatorChar));
            string authoringDirectory = Path.Combine(root,
                DerivedAuthoringDirectory.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(directory);
            Directory.CreateDirectory(authoringDirectory);
            string firstPool = RmapPatternCatalog.ExportCatalogCsv(catalog);
            string origins = RmapPatternCatalog.ExportOriginsCsv(catalog);
            string characteristics = RmapPatternCatalog.ExportCharacteristicsCsv(catalog);
            WriteDerivedCsv(directory, authoringDirectory, "rmap07_first_pool.csv", firstPool);
            WriteDerivedCsv(directory, authoringDirectory, "rmap07_origin_transform_relations.csv", origins);
            WriteDerivedCsv(directory, authoringDirectory, "rmap07_auto_characteristics.csv", characteristics);
            var placements = new StringBuilder();
            placements.AppendLine("CandidateId,PrimaryRole,OriginX,OriginY,BaseCells16");
            for (var index = 0; index < catalog.Candidates.Count; index++)
            {
                RmapPatternCandidate candidate = catalog.Candidates[index];
                placements.Append(candidate.CandidateId).Append(',')
                    .Append(candidate.PrimaryRole).Append(',')
                    .Append((index % GalleryColumns) * RmapPatternCatalog.Width).Append(',')
                    .Append(20 + (index / GalleryColumns) * RmapPatternCatalog.Height).Append(',')
                    .Append(RmapPatternCatalog.SerializeBaseCells16(candidate.BaseCells)).AppendLine();
            }
            WriteDerivedCsv(directory, authoringDirectory, "rmap07_gallery_placements.csv", placements.ToString());
            File.WriteAllText(Path.Combine(directory, "rmap07_selection_manifest.json"), BuildSelectionManifest(catalog),
                new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(directory, "rmap07_gallery_manifest.json"), "{\n"
                + "  \"scene_path\": \"" + ScenePath + "\",\n"
                + "  \"bounds_tiles\": \"48x56\",\n"
                + "  \"candidate_count\": " + catalog.Candidates.Count + ",\n"
                + "  \"coordinate_contract\": \"lower-left; x-right; y-up; index=y*4+x\",\n"
                + "  \"placements\": \"rmap07_gallery_placements.csv; each row is CandidateId, 4x4 lower-left origin, BaseCells16\",\n"
                + "  \"derived_authoring_csv\": \"" + DerivedAuthoringDirectory + "; regenerated by this builder, never hand-edited\",\n"
                + "  \"player_entry\": \"RMAP07_Player at 2,1; A/D or arrows move; Space jumps\",\n"
                + "  \"physical_fixtures\": \"TilemapCollider2D + PlatformEffector2D R0/MirrorX/MirrorY/R180 at x=28/33/38/43, world top y=2.20\"\n"
                + "}\n", new UTF8Encoding(false));
        }

        private static void WriteDerivedCsv(string generatedDirectory, string authoringDirectory, string fileName,
            string content)
        {
            File.WriteAllText(Path.Combine(generatedDirectory, fileName), content, new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(authoringDirectory, fileName), content, new UTF8Encoding(false));
        }

        private static string BuildSelectionManifest(RmapPatternCatalogSnapshot catalog)
        {
            var roles = catalog.Candidates.GroupBy(value => value.PrimaryRole).OrderBy(value => value.Key)
                .Select(value => "    \"" + value.Key + "\": " + value.Count()).ToArray();
            var firstByRole = catalog.Candidates.GroupBy(value => value.PrimaryRole)
                .ToDictionary(value => value.Key, value => value.First().CandidateId);
            var selections = catalog.Candidates.Select(value => "    {\"candidate_id\": \"" + value.CandidateId +
                "\", \"role\": \"" + value.PrimaryRole + "\", \"reason\": \"" +
                (firstByRole[value.PrimaryRole] == value.CandidateId ? "ROLE_COVERAGE" : "STABLE_ID_FILL") + "\"}");
            return "{\n"
                + "  \"data_version\": \"" + RmapPatternCatalog.DataVersion + "\",\n"
                + "  \"generated_distinct_base_geometries\": " + catalog.GeneratedDistinctBaseGeometryCount + ",\n"
                + "  \"source_transform_relations\": " + catalog.SourceTransformRelationCount + ",\n"
                + "  \"relations_collapsed_by_base_geometry\": " + catalog.RelationsCollapsedByBaseGeometry + ",\n"
                + "  \"selected_first_pool\": " + catalog.Candidates.Count + ",\n"
                + "  \"excluded_after_first_pool\": " + catalog.ExcludedAfterInitialPoolCount + ",\n"
                + "  \"role_distribution\": {\n" + string.Join(",\n", roles) + "\n  },\n"
                + "  \"selection_rule\": \"one stable-id representative per primary role, then stable-id fill to 48; VOID_CLEAR has one deduplicated base geometry\",\n"
                + "  \"selections\": [\n" + string.Join(",\n", selections) + "\n  ]\n"
                + "}\n";
        }
    }
}
