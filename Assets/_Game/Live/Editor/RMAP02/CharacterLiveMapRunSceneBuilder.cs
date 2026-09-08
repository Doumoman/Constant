using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using StarNight.Character.Live.Cameras;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using StarNight.Map.WorldGeneration.Baking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Character.Live.Rmap02.Editor
{
    /// <summary>Creates only the isolated RMAP02 physical Player fixture and its evidence manifest.</summary>
    public static class CharacterLiveMapRunSceneBuilder
    {
        public const string ScenePath =
            "Assets/_Game/Map/Scenes/MoonPalace/RMAP02/MoonPalacePlayerTilemapRun_RMAP02.unity";
        private const string TileDirectory = "Assets/_Game/Live/Prefabs/RMAP02/Tiles";
        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";
        private const string ManifestPath = "MapDesign/MCP/GENERATED/RMAP02/rmap02_fixture_manifest.json";

        [MenuItem("Tools/MoonPalace/RMAP02/Build Physical Player Tilemap Run")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Directory.CreateDirectory(TileDirectory);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("MoonPalace_PlayerTilemapRun_RMAP02", typeof(Grid));
            var tiles = CreateTiles();
            var bindings = new List<GeneratedUnityTilemapLayerBinding>();
            foreach (GeneratedTilemapLayerId layer in System.Enum.GetValues(
                typeof(GeneratedTilemapLayerId)))
            {
                bindings.Add(new GeneratedUnityTilemapLayerBinding(layer,
                    CreateLayer(root.transform, layer), tiles[layer]));
            }

            Tilemap terrain = bindings.Find(value => value.LayerId ==
                GeneratedTilemapLayerId.Terrain).Tilemap;
            ConfigureSolidCollider(terrain.gameObject);
            var applier = root.AddComponent<GeneratedUnityTilemapApplier>();
            applier.Configure(bindings, true);
            GeneratedUnityTilemapApplyReport report = applier.ApplyFixture();

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
            {
                throw new InvalidOperationException("RMAP02 Player prefab is missing: " + PlayerPrefabPath);
            }

            var player = PrefabUtility.InstantiatePrefab(playerPrefab, scene) as GameObject;
            player.name = "RMAP02_Player";
            CharacterLivePlayerRig rig = player.GetComponent<CharacterLivePlayerRig>();
            CharacterLiveMovementDriver movement = player.GetComponent<CharacterLiveMovementDriver>();
            if (rig == null || movement == null)
            {
                throw new InvalidOperationException("RMAP02 Player prefab must contain Rig and MovementDriver.");
            }

            // The shared Player prefab keeps its established collider.  This
            // isolated scene instance adopts the RMAP02 foot-pivot contract
            // without changing any existing Player scene or prefab consumer.
            rig.BodyCollider.size = new Vector2(0.4f, 0.8f);
            rig.BodyCollider.offset = new Vector2(0f, 0.4f);
            movement.ConfigureRmap02(1 << LayerMask.NameToLayer("Default"));
            var bootstrap = root.AddComponent<CharacterLiveMapRunBootstrap>();
            bootstrap.Configure(rig, movement, new Vector2(
                Rmap02TilemapFixturePlan.SpawnX, Rmap02TilemapFixturePlan.SpawnY));

            CreateExit(root.transform, Rmap02TilemapFixturePlan.ExitX,
                Rmap02TilemapFixturePlan.ExitY);
            CreateCamera(root.transform, rig.transform);
            CreateLabel(root.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            WriteManifest(report);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static Dictionary<GeneratedTilemapLayerId, Tile> CreateTiles()
        {
            var result = new Dictionary<GeneratedTilemapLayerId, Tile>();
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            foreach (GeneratedTilemapLayerId layer in System.Enum.GetValues(
                typeof(GeneratedTilemapLayerId)))
            {
                string path = TileDirectory + "/RMAP02_" + layer + ".asset";
                Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, path);
                }

                tile.name = "RMAP02_" + layer;
                tile.sprite = sprite;
                tile.color = ColorFor(layer);
                tile.colliderType = layer == GeneratedTilemapLayerId.Terrain
                    ? Tile.ColliderType.Grid : Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                result.Add(layer, tile);
            }

            return result;
        }

        private static Tilemap CreateLayer(Transform parent, GeneratedTilemapLayerId layer)
        {
            var gameObject = new GameObject(layer.ToString(), typeof(Tilemap), typeof(TilemapRenderer));
            gameObject.transform.SetParent(parent, false);
            var renderer = gameObject.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = (int)layer;
            renderer.mode = TilemapRenderer.Mode.Chunk;
            return gameObject.GetComponent<Tilemap>();
        }

        private static void ConfigureSolidCollider(GameObject terrain)
        {
            terrain.AddComponent<TilemapCollider2D>();
            var body = terrain.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            terrain.AddComponent<CompositeCollider2D>();
            terrain.GetComponent<TilemapCollider2D>().compositeOperation =
                Collider2D.CompositeOperation.Merge;
        }

        private static void CreateExit(Transform parent, int x, int y)
        {
            var exit = new GameObject("Exit_RMAP02", typeof(BoxCollider2D),
                typeof(CharacterLiveMapRunExit));
            exit.transform.SetParent(parent, false);
            exit.transform.position = new Vector3(x + 0.5f, y + 0.9f, 0f);
            var collider = exit.GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1f, 1.5f);
        }

        private static void CreateCamera(Transform parent, Transform followTarget)
        {
            var cameraObject = new GameObject("RMAP02_FollowCamera", typeof(Camera),
                typeof(CharacterLiveCameraFollowDriver));
            cameraObject.transform.SetParent(parent, false);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.033f, 0.05f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            cameraObject.GetComponent<CharacterLiveCameraFollowDriver>().Configure(camera,
                followTarget, new Rect(0f, 0f, Rmap02TilemapFixturePlan.WidthInTiles,
                Rmap02TilemapFixturePlan.HeightInTiles), 12f, 8f, 0.08f);
        }

        private static void CreateLabel(Transform parent)
        {
            var label = new GameObject("RMAP02_Controls");
            label.transform.SetParent(parent, false);
            label.transform.position = new Vector3(1f, 37.5f, -1f);
            var text = label.AddComponent<TextMesh>();
            text.text = "RMAP02  |  A/D or Arrows: run  |  Shift: walk  |  Space: jump\n"
                + "Physical 60x40 Tilemap course  |  exit at x=42";
            text.color = new Color(0.9f, 0.94f, 1f, 1f);
            text.characterSize = 0.24f;
            text.fontSize = 28;
            text.anchor = TextAnchor.UpperLeft;
        }

        private static Color ColorFor(GeneratedTilemapLayerId layer)
        {
            switch (layer)
            {
                case GeneratedTilemapLayerId.Terrain: return new Color(0.35f, 0.48f, 0.64f, 1f);
                case GeneratedTilemapLayerId.Affordance: return new Color(0.26f, 0.94f, 0.54f, 0.32f);
                case GeneratedTilemapLayerId.Material: return new Color(0.96f, 0.74f, 0.22f, 0.55f);
                case GeneratedTilemapLayerId.Hazard: return new Color(0.94f, 0.23f, 0.28f, 0.55f);
                case GeneratedTilemapLayerId.Marker: return new Color(0.9f, 0.25f, 0.95f, 0.9f);
                case GeneratedTilemapLayerId.Protection: return new Color(0.25f, 0.8f, 1f, 0.20f);
                default: return new Color(0.75f, 0.75f, 0.75f, 0.15f);
            }
        }

        private static void WriteManifest(GeneratedUnityTilemapApplyReport report)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string absolutePath = Path.Combine(projectRoot, ManifestPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            string json = "{\n"
                + "  \"scene_path\": \"" + ScenePath + "\",\n"
                + "  \"fixture_size_tiles\": \"60x40\",\n"
                + "  \"viewport_tiles\": \"12x8\",\n"
                + "  \"spawn\": \"3,1\",\n"
                + "  \"exit\": \"42,1\",\n"
                + "  \"logical_layers\": 7,\n"
                + "  \"applied_tiles\": " + report.AppliedTileCount + "\n"
                + "}\n";
            File.WriteAllText(absolutePath, json, new UTF8Encoding(false));
        }
    }
}
