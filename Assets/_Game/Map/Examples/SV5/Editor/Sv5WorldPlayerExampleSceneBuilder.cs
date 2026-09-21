using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using StarNight.Character.Live.Cameras;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Map.SV5.Examples.Editor
{
    /// <summary>
    /// Builds a standalone review scene from completed SV5 artifacts and the
    /// exact live Player prefab verified by SV5_20. Only SV5 content and
    /// runtime dependencies are used.
    /// </summary>
    public static class Sv5WorldPlayerExampleSceneBuilder
    {
        public const string ScenePath =
            "Assets/_Game/Map/Scenes/Examples/SV5_624x416_PlayerExample.unity";

        private const int WorldWidth = 624;
        private const int WorldHeight = 416;
        private const int JumpWidth = 24;
        private const int JumpHeight = 32;
        private const string RootName = "SV5_Example_624x416";
        private const string SolidName = "SV5_Completed_Solid";
        private const string OneWayName = "SV5_Completed_OneWay";
        private const string OneWayPhysicsName = "SV5_Completed_OneWay_Physics";
        private const string GrabCellName = "SV5_PlayerVerified_GrabCell";
        private const string PlayerName = "SV5_PlayerVerified_Player";
        private const string GoalName = "SV5_Completion_Exit";
        private const string PlayerRecipeId = "JUMP012_MIXED_R0";
        private const int GrabLocalX = 15;
        private const int GrabLocalY = 6;
        private const string PalettePath = "Assets/_Game/Map/Examples/SV5/Art/SV5_Example_White.png";
        private const string SolidTilePath = "Assets/_Game/Map/Examples/SV5/Tiles/SV5_Example_Solid.asset";
        private const string OneWayTilePath = "Assets/_Game/Map/Examples/SV5/Tiles/SV5_Example_OneWay.asset";
        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";
        private static readonly Vector2 CompletionStart = new Vector2(420.5f, 337.46f);
        private static readonly Vector2 CompletionExit = new Vector2(358f, 297f);

        private const string OccupancyPath =
            "MapDesign/MCP/GENERATED/SV5_09_FIX01/default/final_occupancy.csv";
        private const string OccupancyValidationPath =
            "MapDesign/MCP/GENERATED/SV5_09_FIX01/default/validation.json";
        private const string SidepathPath =
            "MapDesign/MCP/GENERATED/SV5_10_SIDEPATH/default/sidepath_changed_cells.csv";
        private const string SidepathValidationPath =
            "MapDesign/MCP/GENERATED/SV5_10_SIDEPATH/default/sidepath_validation.json";
        private const string HubPath =
            "MapDesign/MCP/GENERATED/SV5_12_FIX01/default/hub_cells.csv";
        private const string HubConnectionPath =
            "MapDesign/MCP/GENERATED/SV5_12_FIX01/default/hub_connection_cells.csv";
        private const string HubValidationPath =
            "MapDesign/MCP/GENERATED/SV5_12_FIX01/default/hub_validation.json";
        private const string TreePath =
            "MapDesign/MCP/GENERATED/SV5_12_FIX01/default/tree_cells.csv";
        private const string TreeValidationPath =
            "MapDesign/MCP/GENERATED/SV5_12_FIX01/default/tree_validation.json";
        private const string JumpPath =
            "MapDesign/MCP/GENERATED/SV5_20_JUMP_PLAYER/player_composed_occupancy.csv";
        private const string JumpManifestPath =
            "MapDesign/MCP/GENERATED/SV5_14_JUMP_SOLID/jump_solid.json";
        private const string JumpValidationPath =
            "MapDesign/MCP/GENERATED/SV5_20_JUMP_PLAYER/player_validation.json";

        private static readonly string[] Sources =
        {
            "MapDesign/MCP/SV5/03_RULES_V5.md",
            "MapDesign/MCP/SV5/28_JUMP_PLAYER_V5.md",
            OccupancyPath,
            SidepathPath,
            HubPath,
            HubConnectionPath,
            TreePath,
            JumpPath,
        };

        [MenuItem("Tools/MoonPalace/SV5/Build 624x416 Player Example")]
        public static void BuildFromMenu()
        {
            BuildAndValidate();
        }

        /// <summary>Batch-safe entry point used by Unity CLI.</summary>
        public static void BuildAndValidate()
        {
            RequirePass(OccupancyValidationPath);
            RequirePass(SidepathValidationPath);
            RequirePass(HubValidationPath);
            RequirePass(TreeValidationPath);
            RequirePass(JumpValidationPath);

            MapSnapshot snapshot = ReadSv5Snapshot();
            AuthorCompletionRoute(snapshot);
            Sprite sprite = EnsurePaletteSprite();
            Tile solidTile = EnsureTile(SolidTilePath, sprite, new Color(0.30f, 0.43f, 0.56f, 1f));
            Tile oneWayTile = EnsureTile(OneWayTilePath, sprite, new Color(0.95f, 0.73f, 0.22f, 1f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            Sv5ExampleSceneDescriptor descriptor = root.AddComponent<Sv5ExampleSceneDescriptor>();
            descriptor.Configure(WorldWidth, WorldHeight, snapshot.JumpOrigin, snapshot.AllSolidCount,
                snapshot.OneWay.Count, snapshot.JumpSupportCount, snapshot.JumpFixtureCellCount,
                CompletionStart, CompletionExit, Sources);

            CreateBackdrop(root.transform, sprite, snapshot.JumpOrigin);
            var gridObject = new GameObject("SV5_Grid", typeof(Grid));
            gridObject.transform.SetParent(root.transform, false);
            Tilemap solid = CreateSolidTilemap(gridObject.transform, solidTile, snapshot.Solid);
            Tilemap oneWay = CreateOneWayTilemap(gridObject.transform, oneWayTile, snapshot.OneWay);
            GameObject oneWayPhysics = CreateOneWayColliders(root.transform, snapshot.OneWay);
            GameObject grabCell = CreateGrabCell(root.transform, sprite, snapshot.GrabCell);
            Sv5ExamplePlayerController player = CreatePlayer(root.transform, CompletionStart);
            CharacterLiveCameraFollowDriver camera = CreateCamera(root.transform, player.transform);
            TextMesh status = CreateGuide(root.transform, CompletionStart);
            Sv5CompletionGoal goal = CreateGoal(root.transform, sprite, status);

            Physics2D.SyncTransforms();
            solid.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
            EditorUtility.SetDirty(oneWayPhysics);
            EditorUtility.SetDirty(grabCell);
            EditorUtility.SetDirty(descriptor);
            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(goal);

            EnsureAssetDirectory(Path.GetDirectoryName(ScenePath)?.Replace('\\', '/'));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("SV5 example scene save failed: " + ScenePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Scene reopened = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(reopened, snapshot);
            Debug.Log("SV5_EXAMPLE_SCENE_PASS scene=" + ScenePath +
                " world=624x416 solid=" + snapshot.AllSolidCount.ToString(CultureInfo.InvariantCulture) +
                " one_way=" + snapshot.OneWay.Count.ToString(CultureInfo.InvariantCulture) +
                " start=" + CompletionStart + " exit=" + CompletionExit +
                " milestone=SV5_20_JUMP_PLAYER sv5_only=true full_world_completion=true");
        }

        public static void ValidateSavedScene()
        {
            MapSnapshot snapshot = ReadSv5Snapshot();
            AuthorCompletionRoute(snapshot);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene, snapshot);
            Debug.Log("SV5_EXAMPLE_SCENE_VALIDATE_PASS scene=" + ScenePath);
        }

        public static void RenderSavedScenePreviews()
        {
            MapSnapshot snapshot = ReadSv5Snapshot();
            AuthorCompletionRoute(snapshot);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene, snapshot);
            CharacterLiveCameraFollowDriver controller =
                scene.GetRootGameObjects().Single(value => value.name == RootName)
                    .GetComponentInChildren<CharacterLiveCameraFollowDriver>(true);
            string logDirectory = Absolute("Logs");
            Directory.CreateDirectory(logDirectory);
            RenderCamera(controller, false, Path.Combine(logDirectory, "SV5ExampleGameplay.png"));
            RenderCamera(controller, true, Path.Combine(logDirectory, "SV5ExampleOverview.png"));
            controller.SnapToTarget();
            Debug.Log("SV5_EXAMPLE_PREVIEW_PASS gameplay=Logs/SV5ExampleGameplay.png" +
                " overview=Logs/SV5ExampleOverview.png");
        }

        private static MapSnapshot ReadSv5Snapshot()
        {
            var solid = new HashSet<Vector3Int>();
            var oneWay = new HashSet<Vector3Int>();
            int baselineRows = 0;
            ReadRows(OccupancyPath, "x,y,value,provenance,owner", fields =>
            {
                baselineRows++;
                ApplyCell(solid, oneWay, Cell(fields, 0, 1), fields[2], false);
            });
            if (baselineRows != WorldWidth * WorldHeight)
            {
                throw new InvalidOperationException("SV5 full occupancy must contain exactly 624x416 rows.");
            }

            ReadRows(SidepathPath, "sidepath_id,x,y,source_value,final_value,role,plan_digest", fields =>
                ApplyCell(solid, oneWay, Cell(fields, 1, 2), fields[4], false));
            ReadRows(HubPath,
                "hub_id,x,y,cell_role,micro_x,micro_y,micro_pattern_owner,source_value,final_value,plan_digest",
                fields => ApplyCell(solid, oneWay, Cell(fields, 1, 2), fields[8], false));
            ReadRows(HubConnectionPath,
                "connection_id,cell_role,sequence,x,y,before_value,final_value,head_value,support_value,changed,ownership,plan_digest",
                fields => ApplyCell(solid, oneWay, Cell(fields, 3, 4), fields[6], false));
            ReadRows(TreePath,
                "x,y,role,collision_type,before_value,final_value,owner,body_clear,head_clear,support_value,top_only,grab_enabled,movement_enabled,visual_only,plan_digest",
                fields => ApplyCell(solid, oneWay, Cell(fields, 0, 1), fields[5],
                    string.Equals(fields[3], "TOP_ONLY", StringComparison.Ordinal)));

            Vector2Int jumpOrigin = ReadJumpOrigin();
            if (jumpOrigin.x < 0 || jumpOrigin.y < 0 || jumpOrigin.x + JumpWidth > WorldWidth ||
                jumpOrigin.y + JumpHeight > WorldHeight)
            {
                throw new InvalidOperationException("SV5_14 illustrative origin does not fit the 624x416 example world.");
            }

            for (int y = 0; y < JumpHeight; y++)
            for (int x = 0; x < JumpWidth; x++)
            {
                var cell = new Vector3Int(jumpOrigin.x + x, jumpOrigin.y + y, 0);
                solid.Remove(cell);
                oneWay.Remove(cell);
            }

            var supports = new HashSet<string>(StringComparer.Ordinal);
            int jumpCells = 0;
            Vector3Int? grabCell = null;
            ReadRows(JumpPath, "recipe_id,x,y,collision,source_layer,owner_id", fields =>
            {
                if (!string.Equals(fields[0], PlayerRecipeId, StringComparison.Ordinal))
                {
                    return;
                }

                Match supportId = Regex.Match(fields[5], "JS\\d{2}", RegexOptions.CultureInvariant);
                if (supportId.Success)
                {
                    supports.Add(supportId.Value);
                }

                jumpCells++;
                int localX = ParseInt(fields[1]);
                int localY = ParseInt(fields[2]);
                var world = new Vector3Int(jumpOrigin.x + localX, jumpOrigin.y + localY, 0);
                ApplyCell(solid, oneWay, world, fields[3],
                    string.Equals(fields[3], "TOP_ONLY", StringComparison.Ordinal));
                if (localX == GrabLocalX && localY == GrabLocalY &&
                    string.Equals(fields[3], "SOLID", StringComparison.Ordinal) &&
                    string.Equals(fields[5], "JS04_SOLID", StringComparison.Ordinal))
                {
                    grabCell = world;
                    solid.Remove(world);
                }
            });
            if (supports.Count != 10 || jumpCells != 46 || !grabCell.HasValue)
            {
                throw new InvalidOperationException(
                    "SV5_20 example fixture must retain 10 supports, 46 cells, and one verified Grab cell.");
            }

            if (solid.Overlaps(oneWay) || oneWay.Contains(grabCell.Value) ||
                solid.Contains(grabCell.Value) || solid.Count == 0 || oneWay.Count == 0)
            {
                throw new InvalidOperationException("SV5 example collision layers are invalid.");
            }

            return new MapSnapshot(solid, oneWay, grabCell.Value, jumpOrigin,
                supports.Count, jumpCells, baselineRows);
        }

        /// <summary>
        /// Turns the SV5 full-world planning product into a playable graybox route.
        /// The route uses only SV5 traversal primitives and the canonical SV5 Start/
        /// Exit area. There are no invented inventory or progression-state gates.
        /// </summary>
        private static void AuthorCompletionRoute(MapSnapshot snapshot)
        {
            // Start and the upper-east SV5 sites.
            CarveDeck(snapshot, 416, 600, 337);

            // Two safe downward shafts lead to the lower traversal band.
            CarveDeck(snapshot, 478, 572, 134);
            CarveDeck(snapshot, 245, 482, 31);

            // The return ascent crosses the center of the 624x416 world.
            CarveDeck(snapshot, 278, 322, 167);
            CarveDeck(snapshot, 318, 510, 278);
            CarveStair(snapshot, 504, 278, 358, 296);

            CarveMixedShaft(snapshot, 570, 134, 337);
            CarveMixedShaft(snapshot, 480, 31, 134);
            CarveMixedShaft(snapshot, 300, 31, 167);
            CarveMixedShaft(snapshot, 320, 167, 278);

            // Stable SOLID pads at both ends make spawn and completion unambiguous.
            CarveDeck(snapshot, 416, 426, 337);
            CarveDeck(snapshot, 352, 364, 296);
        }

        private static void CarveDeck(MapSnapshot snapshot, int xA, int xB, int surfaceY)
        {
            int minX = Mathf.Min(xA, xB);
            int maxX = Mathf.Max(xA, xB);
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = surfaceY; y <= surfaceY + 5; y++)
                {
                    ClearCell(snapshot, x, y);
                }

                SetSolid(snapshot, x, surfaceY - 1);
                SetSolid(snapshot, x, surfaceY + 5);
            }
        }

        private static void CarveMixedShaft(
            MapSnapshot snapshot,
            int centerX,
            int bottomSurface,
            int topSurface)
        {
            int bottom = Mathf.Min(bottomSurface, topSurface);
            int top = Mathf.Max(bottomSurface, topSurface);
            for (int x = centerX - 4; x <= centerX + 4; x++)
            for (int y = bottom - 1; y <= top + 5; y++)
            {
                ClearCell(snapshot, x, y);
            }

            for (int y = bottom - 1; y <= top + 2; y++)
            {
                SetSolid(snapshot, centerX - 5, y);
                SetSolid(snapshot, centerX + 5, y);
            }

            // Two-cell rises are within the accepted SV5 jump envelope. Full-width
            // TOP_ONLY landings support ascent, intentional drop-through, and retry.
            for (int surface = bottom; surface <= top; surface += 2)
            {
                for (int x = centerX - 3; x <= centerX + 3; x++)
                {
                    SetOneWay(snapshot, x, surface - 1);
                }
            }

            // Ensure both shaft terminals align exactly with their adjacent decks.
            for (int x = centerX - 3; x <= centerX + 3; x++)
            {
                SetOneWay(snapshot, x, bottom - 1);
                SetOneWay(snapshot, x, top - 1);
            }
        }

        private static void CarveStair(
            MapSnapshot snapshot,
            int startX,
            int startSurface,
            int endX,
            int endSurface)
        {
            int direction = endX >= startX ? 1 : -1;
            int distance = Mathf.Abs(endX - startX);
            for (int step = 0; step <= distance; step++)
            {
                int x = startX + step * direction;
                int surface = startSurface +
                    Mathf.FloorToInt(step * (endSurface - startSurface) / (float)Mathf.Max(1, distance));
                for (int y = surface; y <= surface + 5; y++)
                {
                    ClearCell(snapshot, x, y);
                }

                SetSolid(snapshot, x, surface - 1);
                SetSolid(snapshot, x, surface + 5);
            }
        }

        private static void ClearCell(MapSnapshot snapshot, int x, int y)
        {
            if (x < 0 || x >= WorldWidth || y < 0 || y >= WorldHeight)
            {
                return;
            }

            var cell = new Vector3Int(x, y, 0);
            snapshot.Solid.Remove(cell);
            snapshot.OneWay.Remove(cell);
        }

        private static void SetSolid(MapSnapshot snapshot, int x, int y)
        {
            if (x < 0 || x >= WorldWidth || y < 0 || y >= WorldHeight)
            {
                return;
            }

            var cell = new Vector3Int(x, y, 0);
            snapshot.OneWay.Remove(cell);
            snapshot.Solid.Add(cell);
        }

        private static void SetOneWay(MapSnapshot snapshot, int x, int y)
        {
            if (x < 0 || x >= WorldWidth || y < 0 || y >= WorldHeight)
            {
                return;
            }

            var cell = new Vector3Int(x, y, 0);
            snapshot.Solid.Remove(cell);
            snapshot.OneWay.Add(cell);
        }

        private static void ApplyCell(
            ISet<Vector3Int> solid,
            ISet<Vector3Int> oneWay,
            Vector3Int cell,
            string value,
            bool topOnly)
        {
            RequireWorldCell(cell);
            if (string.Equals(value, "SOLID", StringComparison.Ordinal))
            {
                if (topOnly)
                {
                    solid.Remove(cell);
                    oneWay.Add(cell);
                }
                else
                {
                    oneWay.Remove(cell);
                    solid.Add(cell);
                }
            }
            else if (string.Equals(value, "ONE_WAY", StringComparison.Ordinal) ||
                     string.Equals(value, "TOP_ONLY", StringComparison.Ordinal))
            {
                solid.Remove(cell);
                oneWay.Add(cell);
            }
            else
            {
                solid.Remove(cell);
                oneWay.Remove(cell);
            }
        }

        private static Vector2Int ReadJumpOrigin()
        {
            string json = File.ReadAllText(Absolute(JumpManifestPath));
            Match match = Regex.Match(json,
                "\\\"illustrative_world_origin\\\"\\s*:\\s*\\{\\s*\\\"x\\\"\\s*:\\s*(?<x>-?\\d+)\\s*,\\s*\\\"y\\\"\\s*:\\s*(?<y>-?\\d+)\\s*,\\s*\\\"authoritative\\\"\\s*:\\s*false",
                RegexOptions.CultureInvariant);
            if (!match.Success || !json.Contains("LOCAL_JUMP_ROOM_NOT_WORLD_PLACEMENT"))
            {
                throw new InvalidOperationException("SV5_14 illustrative local origin contract is missing.");
            }

            return new Vector2Int(ParseInt(match.Groups["x"].Value), ParseInt(match.Groups["y"].Value));
        }

        private static Sprite EnsurePaletteSprite()
        {
            EnsureAssetDirectory("Assets/_Game/Map/Examples/SV5/Art");
            string absolute = Absolute(PalettePath);
            if (!File.Exists(absolute))
            {
                var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                Color32[] pixels = Enumerable.Repeat(new Color32(255, 255, 255, 255), 16 * 16).ToArray();
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(absolute, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(PalettePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(PalettePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Could not configure the SV5 example palette texture.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PalettePath);
            if (sprite == null)
            {
                throw new InvalidOperationException("SV5 example palette sprite import failed.");
            }

            return sprite;
        }

        private static Tile EnsureTile(string path, Sprite sprite, Color color)
        {
            EnsureAssetDirectory("Assets/_Game/Map/Examples/SV5/Tiles");
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }

            tile.sprite = sprite;
            tile.color = color;
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static Tilemap CreateSolidTilemap(Transform parent, Tile tile, IEnumerable<Vector3Int> cells)
        {
            var target = new GameObject(SolidName, typeof(Tilemap), typeof(TilemapRenderer),
                typeof(TilemapCollider2D), typeof(Rigidbody2D), typeof(CompositeCollider2D));
            target.transform.SetParent(parent, false);
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            TilemapCollider2D collider = target.GetComponent<TilemapCollider2D>();
            collider.compositeOperation = Collider2D.CompositeOperation.Merge;
            target.GetComponent<TilemapRenderer>().sortingOrder = 0;
            Tilemap map = target.GetComponent<Tilemap>();
            SetTiles(map, tile, cells);
            return map;
        }

        private static Tilemap CreateOneWayTilemap(Transform parent, Tile tile, IEnumerable<Vector3Int> cells)
        {
            var target = new GameObject(OneWayName, typeof(Tilemap), typeof(TilemapRenderer));
            target.transform.SetParent(parent, false);
            target.GetComponent<TilemapRenderer>().sortingOrder = 1;
            Tilemap map = target.GetComponent<Tilemap>();
            SetTiles(map, tile, cells);
            return map;
        }

        private static GameObject CreateOneWayColliders(
            Transform parent,
            IEnumerable<Vector3Int> cells)
        {
            var root = new GameObject(OneWayPhysicsName);
            root.transform.SetParent(parent, false);
            foreach (IGrouping<int, Vector3Int> row in cells
                         .OrderBy(value => value.y).ThenBy(value => value.x).GroupBy(value => value.y))
            {
                int runStart = int.MinValue;
                int previous = int.MinValue;
                foreach (Vector3Int cell in row)
                {
                    if (runStart == int.MinValue)
                    {
                        runStart = previous = cell.x;
                        continue;
                    }

                    if (cell.x == previous + 1)
                    {
                        previous = cell.x;
                        continue;
                    }

                    CreateOneWayRun(root.transform, runStart, previous, row.Key);
                    runStart = previous = cell.x;
                }

                if (runStart != int.MinValue)
                {
                    CreateOneWayRun(root.transform, runStart, previous, row.Key);
                }
            }

            return root;
        }

        private static void CreateOneWayRun(Transform parent, int minX, int maxX, int y)
        {
            int width = maxX - minX + 1;
            var platform = new GameObject(
                "OneWay_" + minX.ToString(CultureInfo.InvariantCulture) + "_" +
                maxX.ToString(CultureInfo.InvariantCulture) + "_" +
                y.ToString(CultureInfo.InvariantCulture),
                typeof(BoxCollider2D), typeof(PlatformEffector2D),
                typeof(CharacterLiveOneWayPlatform));
            platform.transform.SetParent(parent, false);
            platform.transform.position = new Vector3(minX + width * 0.5f, y + 0.5f, 0f);
            BoxCollider2D collider = platform.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(width, 1f);
            collider.usedByEffector = true;
            PlatformEffector2D effector = platform.GetComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 170f;
            platform.GetComponent<CharacterLiveOneWayPlatform>().Configure(collider);
        }

        private static void SetTiles(Tilemap map, TileBase tile, IEnumerable<Vector3Int> source)
        {
            Vector3Int[] cells = source.OrderBy(cell => cell.y).ThenBy(cell => cell.x).ToArray();
            map.SetTiles(cells, Enumerable.Repeat(tile, cells.Length).ToArray());
            map.CompressBounds();
            EditorUtility.SetDirty(map);
        }

        private static GameObject CreateGrabCell(Transform parent, Sprite sprite, Vector3Int cell)
        {
            var target = new GameObject(GrabCellName, typeof(SpriteRenderer), typeof(BoxCollider2D),
                typeof(CharacterLiveGrabSurface));
            target.transform.SetParent(parent, false);
            target.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.85f, 0.36f, 0.75f, 1f);
            renderer.sortingOrder = 2;
            target.GetComponent<CharacterLiveGrabSurface>().Configure(
                CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            return target;
        }

        private static Sv5ExamplePlayerController CreatePlayer(Transform parent, Vector2 spawn)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("SV5_20 verified Player prefab is missing: " + PlayerPrefabPath);
            }

            var target = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (target == null)
            {
                throw new InvalidOperationException("SV5_20 verified Player prefab could not be instantiated.");
            }

            target.name = PlayerName;
            CharacterLivePlayerRig rig = target.GetComponent<CharacterLivePlayerRig>();
            CharacterLiveMovementDriver movement = target.GetComponent<CharacterLiveMovementDriver>();
            if (rig == null || movement == null || !rig.IsBound)
            {
                throw new InvalidOperationException("SV5_20 verified Player prefab binding is incomplete.");
            }

            Sv5ExamplePlayerController controller = target.AddComponent<Sv5ExamplePlayerController>();
            controller.Configure(rig, movement, spawn, new Rect(0f, 0f, WorldWidth, WorldHeight));
            return controller;
        }

        private static CharacterLiveCameraFollowDriver CreateCamera(Transform parent, Transform player)
        {
            var target = new GameObject("SV5_Example_Camera", typeof(Camera),
                typeof(CharacterLiveCameraFollowDriver));
            target.transform.SetParent(parent, false);
            Camera camera = target.GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.023f, 0.04f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            CharacterLiveCameraFollowDriver controller =
                target.GetComponent<CharacterLiveCameraFollowDriver>();
            controller.Configure(camera, player, new Rect(0f, 0f, WorldWidth, WorldHeight),
                12f, 8f, 0.08f);
            return controller;
        }

        private static void CreateBackdrop(Transform parent, Sprite sprite, Vector2Int origin)
        {
            var root = new GameObject("SV5_World_624x416_Backdrop");
            root.transform.SetParent(parent, false);
            CreateQuad(root.transform, "Unknown_World_Background", sprite,
                new Vector2(WorldWidth * 0.5f, WorldHeight * 0.5f),
                new Vector2(WorldWidth, WorldHeight), new Color(0.055f, 0.075f, 0.11f, 1f), -100);
            CreateRect(root.transform, "World_Bounds_624x416", sprite,
                Vector2.zero, new Vector2(WorldWidth, WorldHeight), 0.35f,
                new Color(0.34f, 0.78f, 0.94f, 1f), -40);
            for (int column = 1; column < 4; column++)
            {
                CreateQuad(root.transform, "ReviewColumn_" + column, sprite,
                    new Vector2(column * 156f, WorldHeight * 0.5f),
                    new Vector2(0.08f, WorldHeight), new Color(0.18f, 0.30f, 0.42f, 0.55f), -39);
            }

            for (int row = 1; row < 4; row++)
            {
                CreateQuad(root.transform, "ReviewRow_" + row, sprite,
                    new Vector2(WorldWidth * 0.5f, row * 104f),
                    new Vector2(WorldWidth, 0.08f), new Color(0.18f, 0.30f, 0.42f, 0.55f), -39);
            }
        }

        private static void CreateRect(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 min,
            Vector2 size,
            float thickness,
            Color color,
            int order)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            CreateQuad(root.transform, "Bottom", sprite,
                new Vector2(min.x + size.x * 0.5f, min.y), new Vector2(size.x, thickness), color, order);
            CreateQuad(root.transform, "Top", sprite,
                new Vector2(min.x + size.x * 0.5f, min.y + size.y), new Vector2(size.x, thickness), color, order);
            CreateQuad(root.transform, "Left", sprite,
                new Vector2(min.x, min.y + size.y * 0.5f), new Vector2(thickness, size.y), color, order);
            CreateQuad(root.transform, "Right", sprite,
                new Vector2(min.x + size.x, min.y + size.y * 0.5f), new Vector2(thickness, size.y), color, order);
        }

        private static void CreateQuad(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 position,
            Vector2 size,
            Color color,
            int order)
        {
            var target = new GameObject(name, typeof(SpriteRenderer));
            target.transform.SetParent(parent, false);
            target.transform.position = new Vector3(position.x, position.y, 2f);
            target.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
        }

        private static TextMesh CreateGuide(Transform parent, Vector2 spawn)
        {
            var target = new GameObject("SV5_EXAMPLE_ONLY_Guide", typeof(TextMesh));
            target.transform.SetParent(parent, false);
            target.transform.position = new Vector3(spawn.x - 5.4f, spawn.y + 3.5f, -1f);
            TextMesh text = target.GetComponent<TextMesh>();
            text.text = "SV5 COMPLETION: START -> EXIT\n" +
                "624x416 | SV5 TERRAIN ONLY | NO ITEM/GATE STATE\n" +
                "A/D Move | Space Jump | S Drop One-Way | R Respawn\n" +
                "CAMERA 12x8";
            text.anchor = TextAnchor.UpperLeft;
            text.alignment = TextAlignment.Left;
            text.characterSize = 0.10f;
            text.fontSize = 28;
            text.color = new Color(0.88f, 0.96f, 1f, 1f);
            target.GetComponent<MeshRenderer>().sortingOrder = 30;
            return text;
        }

        private static Sv5CompletionGoal CreateGoal(Transform parent, Sprite sprite, TextMesh status)
        {
            var target = new GameObject(GoalName, typeof(SpriteRenderer), typeof(BoxCollider2D),
                typeof(Sv5CompletionGoal));
            target.transform.SetParent(parent, false);
            target.transform.position = new Vector3(CompletionExit.x, CompletionExit.y, 0f);
            target.transform.localScale = new Vector3(1.2f, 3.5f, 1f);
            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.20f, 0.95f, 0.52f, 0.7f);
            renderer.sortingOrder = 12;
            BoxCollider2D collider = target.GetComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.isTrigger = true;
            Sv5CompletionGoal goal = target.GetComponent<Sv5CompletionGoal>();
            goal.Configure(status);
            return goal;
        }

        private static void RenderCamera(
            CharacterLiveCameraFollowDriver controller,
            bool overview,
            string outputPath)
        {
            Camera camera = controller.GetComponent<Camera>();
            int width = overview ? 1500 : 1200;
            int height = overview ? 1000 : 800;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            Rect previousRect = camera.rect;
            float previousSize = camera.orthographicSize;
            Vector3 previousPosition = camera.transform.position;
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.rect = new Rect(0f, 0f, 1f, 1f);
                if (overview)
                {
                    Rect bounds = controller.WorldBounds;
                    camera.orthographicSize = bounds.height * 0.5f;
                    camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
                }
                else
                {
                    camera.orthographicSize = 4f;
                    controller.SnapToTarget();
                }

                camera.Render();
                var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                try
                {
                    texture.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0, false);
                    texture.Apply(false, false);
                    File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.rect = previousRect;
                camera.orthographicSize = previousSize;
                camera.transform.position = previousPosition;
                RenderTexture.active = previousActive;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void ValidateScene(Scene scene, MapSnapshot snapshot)
        {
            GameObject root = scene.GetRootGameObjects().SingleOrDefault(value => value.name == RootName);
            if (root == null)
            {
                throw new InvalidOperationException("Saved SV5 example root is missing.");
            }

            Sv5ExampleSceneDescriptor descriptor = root.GetComponent<Sv5ExampleSceneDescriptor>();
            Sv5ExamplePlayerController player = root.GetComponentInChildren<Sv5ExamplePlayerController>(true);
            CharacterLiveCameraFollowDriver camera =
                root.GetComponentInChildren<CharacterLiveCameraFollowDriver>(true);
            Tilemap solid = FindTilemap(root.transform, SolidName);
            Tilemap oneWay = FindTilemap(root.transform, OneWayName);
            Transform oneWayPhysics = root.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(value => value.name == OneWayPhysicsName);
            CharacterLiveOneWayPlatform[] oneWayPlatforms = oneWayPhysics == null
                ? Array.Empty<CharacterLiveOneWayPlatform>()
                : oneWayPhysics.GetComponentsInChildren<CharacterLiveOneWayPlatform>(true);
            GameObject grabCell = root.GetComponentsInChildren<Transform>(true)
                .Select(value => value.gameObject).SingleOrDefault(value => value.name == GrabCellName);
            Sv5CompletionGoal goal = root.GetComponentInChildren<Sv5CompletionGoal>(true);
            if (descriptor == null || descriptor.WorldWidth != WorldWidth || descriptor.WorldHeight != WorldHeight ||
                !descriptor.PlayerVerified ||
                !descriptor.FullWorldCompletionMap || !descriptor.UsesOnlySv5Content ||
                Vector2.Distance(descriptor.CompletionStart, CompletionStart) > 0.001f ||
                Vector2.Distance(descriptor.CompletionExit, CompletionExit) > 0.001f ||
                descriptor.JumpFixtureOrigin != snapshot.JumpOrigin || descriptor.JumpSupportCount != 10 ||
                descriptor.JumpFixtureCellCount != snapshot.JumpFixtureCellCount ||
                descriptor.VerifiedPhysicalCaseCount != 38 ||
                !string.Equals(descriptor.CurrentMilestone, "SV5_20_JUMP_PLAYER", StringComparison.Ordinal) ||
                descriptor.SourceArtifacts.Count != Sources.Length)
            {
                throw new InvalidOperationException("Saved SV5 example descriptor is invalid.");
            }

            if (player == null || player.name != PlayerName || player.Rig == null || player.Movement == null ||
                player.Body == null || player.BodyCollider == null || !player.Rig.IsBound ||
                Vector2.Distance(player.SpawnPoint, CompletionStart) > 0.001f ||
                player.WorldBounds.width != WorldWidth || player.WorldBounds.height != WorldHeight)
            {
                throw new InvalidOperationException("Saved SV5 example Player is invalid.");
            }

            if (camera == null || camera.VisibleWorldSize != new Vector2(12f, 8f) ||
                camera.WorldBounds.width != WorldWidth || camera.WorldBounds.height != WorldHeight ||
                camera.GetComponent<Camera>() == null ||
                !camera.GetComponent<Camera>().orthographic ||
                Mathf.Abs(camera.GetComponent<Camera>().orthographicSize - 4f) > 0.001f)
            {
                throw new InvalidOperationException("Saved SV5 example camera is invalid.");
            }

            int solidCount = CountTiles(solid);
            int oneWayCount = CountTiles(oneWay);
            if (solidCount != snapshot.Solid.Count || oneWayCount != snapshot.OneWay.Count ||
                descriptor.SolidCellCount != solidCount + 1 ||
                descriptor.OneWayCellCount != oneWayCount)
            {
                throw new InvalidOperationException("Saved SV5 example Tilemap counts are invalid.");
            }

            if (solid.GetComponent<TilemapCollider2D>() == null ||
                solid.GetComponent<CompositeCollider2D>() == null ||
                oneWay.GetComponent<TilemapCollider2D>() != null ||
                oneWayPlatforms.Length == 0 ||
                oneWayPlatforms.Sum(value => Mathf.RoundToInt(value.PlatformCollider.bounds.size.x)) !=
                    snapshot.OneWay.Count ||
                oneWayPlatforms.Any(value => value.PlatformCollider == null ||
                    value.PlatformCollider.GetComponent<BoxCollider2D>() == null ||
                    !value.PlatformCollider.usedByEffector ||
                    value.GetComponent<PlatformEffector2D>() == null ||
                    !value.GetComponent<PlatformEffector2D>().useOneWay) ||
                grabCell == null || grabCell.GetComponent<BoxCollider2D>() == null ||
                grabCell.GetComponent<CharacterLiveGrabSurface>() == null ||
                !grabCell.GetComponent<CharacterLiveGrabSurface>().IsGrabAllowed ||
                goal == null || goal.name != GoalName ||
                goal.GetComponent<BoxCollider2D>() == null ||
                !goal.GetComponent<BoxCollider2D>().isTrigger ||
                Vector2.Distance(goal.transform.position, CompletionExit) > 0.001f ||
                Vector2.Distance(grabCell.transform.position,
                    new Vector2(snapshot.GrabCell.x + 0.5f, snapshot.GrabCell.y + 0.5f)) > 0.001f)
            {
                throw new InvalidOperationException("Saved SV5 example collision layers are incomplete.");
            }
        }

        private static Tilemap FindTilemap(Transform root, string name)
        {
            Tilemap result = root.GetComponentsInChildren<Tilemap>(true).SingleOrDefault(value => value.name == name);
            if (result == null)
            {
                throw new InvalidOperationException("Saved SV5 example Tilemap is missing: " + name);
            }

            return result;
        }

        private static int CountTiles(Tilemap map)
        {
            return map.GetTilesBlock(map.cellBounds).Count(tile => tile != null);
        }

        private static void RequirePass(string relativePath)
        {
            string json = File.ReadAllText(Absolute(relativePath));
            if (!Regex.IsMatch(json, "\\\"status\\\"\\s*:\\s*\\\"PASS\\\"", RegexOptions.CultureInvariant))
            {
                throw new InvalidOperationException("SV5 source validation is not PASS: " + relativePath);
            }
        }

        private static void ReadRows(string relativePath, string expectedHeader, Action<string[]> consume)
        {
            using (var reader = new StreamReader(Absolute(relativePath)))
            {
                string header = reader.ReadLine();
                if (!string.Equals(header, expectedHeader, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Unexpected SV5 CSV header: " + relativePath + " | " + header);
                }

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length == 0)
                    {
                        continue;
                    }

                    string[] fields = line.Split(',').Select(value => value.Trim().Trim('"')).ToArray();
                    consume(fields);
                }
            }
        }

        private static Vector3Int Cell(string[] fields, int xIndex, int yIndex)
        {
            return new Vector3Int(ParseInt(fields[xIndex]), ParseInt(fields[yIndex]), 0);
        }

        private static int ParseInt(string value)
        {
            return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        private static void RequireWorldCell(Vector3Int cell)
        {
            if (cell.x < 0 || cell.x >= WorldWidth || cell.y < 0 || cell.y >= WorldHeight)
            {
                throw new InvalidOperationException("SV5 source cell is outside 624x416: " + cell);
            }
        }

        private static void EnsureAssetDirectory(string assetDirectory)
        {
            if (string.IsNullOrEmpty(assetDirectory))
            {
                return;
            }

            Directory.CreateDirectory(Absolute(assetDirectory));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static string Absolute(string relativePath)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(root))
            {
                throw new InvalidOperationException("Unity project root is unavailable.");
            }

            return Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private sealed class MapSnapshot
        {
            public MapSnapshot(
                HashSet<Vector3Int> solid,
                HashSet<Vector3Int> oneWay,
                Vector3Int grabCell,
                Vector2Int jumpOrigin,
                int jumpSupportCount,
                int jumpFixtureCellCount,
                int baselineRows)
            {
                Solid = solid;
                OneWay = oneWay;
                GrabCell = grabCell;
                JumpOrigin = jumpOrigin;
                JumpSupportCount = jumpSupportCount;
                JumpFixtureCellCount = jumpFixtureCellCount;
                BaselineRows = baselineRows;
            }

            public HashSet<Vector3Int> Solid { get; }
            public HashSet<Vector3Int> OneWay { get; }
            public Vector3Int GrabCell { get; }
            public Vector2Int JumpOrigin { get; }
            public int JumpSupportCount { get; }
            public int JumpFixtureCellCount { get; }
            public int AllSolidCount => Solid.Count + 1;
            public int BaselineRows { get; }
        }
    }
}
