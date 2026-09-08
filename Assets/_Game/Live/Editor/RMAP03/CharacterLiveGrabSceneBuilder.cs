using System;
using System.IO;
using System.Text;
using StarNight.Character.Live.Cameras;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Character.Live.Rmap03.Editor
{
    /// <summary>Builds only the isolated RMAP03 physical corner-grab fixture.</summary>
    public static class CharacterLiveGrabSceneBuilder
    {
        public const string ScenePath =
            "Assets/_Game/Map/Scenes/MoonPalace/RMAP03/MoonPalaceCornerGrab_RMAP03.unity";

        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";
        private const string TerrainTilePath = "Assets/_Game/Live/Prefabs/RMAP02/Tiles/RMAP02_Terrain.asset";
        private const string ManifestPath = "MapDesign/MCP/GENERATED/RMAP03/rmap03_fixture_manifest.json";

        [MenuItem("Tools/MoonPalace/RMAP03/Build Physical Corner Grab Fixture")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("MoonPalace_CornerGrab_RMAP03", typeof(Grid));
            CreateStaticSafeTerrain(root.transform);
            CreateSurface(root.transform, "DestructibleSafe", new Vector2(15.5f, 2f),
                new Vector2(1f, 2f), CharacterLiveGrabSurface.SurfaceKind.DestructibleSafe);
            CreateMovingSafe(root.transform);
            CreateSurface(root.transform, "OneWayMarker", new Vector2(25.5f, 2f),
                new Vector2(1f, 2f), CharacterLiveGrabSurface.SurfaceKind.OneWay);
            CreateSurface(root.transform, "HazardMarker", new Vector2(28.5f, 2f),
                new Vector2(1f, 2f), CharacterLiveGrabSurface.SurfaceKind.Hazard);
            CreateSurface(root.transform, "CrushingMarker", new Vector2(31.5f, 2f),
                new Vector2(1f, 2f), CharacterLiveGrabSurface.SurfaceKind.Crushing);
            CreateDecoration(root.transform);

            CharacterLivePlayerRig rig = CreatePlayer(scene);
            CreateCamera(root.transform, rig.transform);
            CreateLabel(root.transform);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            WriteManifest();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void CreateStaticSafeTerrain(Transform parent)
        {
            var terrainObject = new GameObject("StaticSafeTerrain", typeof(Tilemap),
                typeof(TilemapRenderer), typeof(TilemapCollider2D), typeof(Rigidbody2D),
                typeof(CompositeCollider2D), typeof(CharacterLiveGrabSurface));
            terrainObject.transform.SetParent(parent, false);
            Tilemap terrain = terrainObject.GetComponent<Tilemap>();
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
            if (tile == null)
            {
                throw new InvalidOperationException("RMAP02 Terrain tile is missing: " + TerrainTilePath);
            }

            for (var x = 0; x <= 11; x++)
            {
                terrain.SetTile(new Vector3Int(x, 0, 0), tile);
            }

            for (var y = 1; y <= 2; y++)
            {
                terrain.SetTile(new Vector3Int(8, y, 0), tile);
            }

            var collider = terrainObject.GetComponent<TilemapCollider2D>();
            terrainObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            terrainObject.GetComponent<CompositeCollider2D>();
            collider.compositeOperation = Collider2D.CompositeOperation.Merge;
            terrainObject.GetComponent<CharacterLiveGrabSurface>().Configure(
                CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
        }

        private static void CreateMovingSafe(Transform parent)
        {
            var moving = CreateSurface(parent, "MovingSafe", new Vector2(20.5f, 2f),
                new Vector2(1f, 2f), CharacterLiveGrabSurface.SurfaceKind.MovingSafe);
            Rigidbody2D body = moving.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            var mover = moving.AddComponent<CharacterLiveGrabMovingSolid>();
            mover.Configure(body, new Vector2(20.5f, 2f), new Vector2(23.5f, 2f), 1.5f);
        }

        private static GameObject CreateSurface(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            CharacterLiveGrabSurface.SurfaceKind kind)
        {
            var surface = new GameObject(name, typeof(BoxCollider2D), typeof(CharacterLiveGrabSurface));
            surface.transform.SetParent(parent, false);
            surface.transform.position = position;
            surface.GetComponent<BoxCollider2D>().size = size;
            surface.GetComponent<CharacterLiveGrabSurface>().Configure(kind);
            return surface;
        }

        private static void CreateDecoration(Transform parent)
        {
            var decoration = new GameObject("Decoration_NoCollider", typeof(CharacterLiveGrabSurface));
            decoration.transform.SetParent(parent, false);
            decoration.transform.position = new Vector3(34.5f, 2f, 0f);
            decoration.GetComponent<CharacterLiveGrabSurface>().Configure(
                CharacterLiveGrabSurface.SurfaceKind.Decoration);
        }

        private static CharacterLivePlayerRig CreatePlayer(Scene scene)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("RMAP03 Player prefab is missing: " + PlayerPrefabPath);
            }

            var player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            player.name = "RMAP03_Player";
            CharacterLivePlayerRig rig = player.GetComponent<CharacterLivePlayerRig>();
            CharacterLiveMovementDriver movement = player.GetComponent<CharacterLiveMovementDriver>();
            if (rig == null || movement == null)
            {
                throw new InvalidOperationException("RMAP03 Player prefab must contain Rig and MovementDriver.");
            }

            rig.BodyCollider.size = new Vector2(0.4f, 0.8f);
            rig.BodyCollider.offset = new Vector2(0f, 0.4f);
            movement.ConfigureRmap02(1 << LayerMask.NameToLayer("Default"));
            var bootstrap = new GameObject("RMAP03_Bootstrap").AddComponent<CharacterLiveMapRunBootstrap>();
            bootstrap.transform.position = Vector3.zero;
            bootstrap.Configure(rig, movement, new Vector2(3f, 1f));
            return rig;
        }

        private static void CreateCamera(Transform parent, Transform followTarget)
        {
            var cameraObject = new GameObject("RMAP03_FollowCamera", typeof(Camera),
                typeof(CharacterLiveCameraFollowDriver));
            cameraObject.transform.SetParent(parent, false);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.033f, 0.05f, 1f);
            cameraObject.GetComponent<CharacterLiveCameraFollowDriver>().Configure(camera,
                followTarget, new Rect(0f, 0f, 40f, 12f), 12f, 8f, 0.08f);
        }

        private static void CreateLabel(Transform parent)
        {
            var label = new GameObject("RMAP03_Controls");
            label.transform.SetParent(parent, false);
            label.transform.position = new Vector3(1f, 10.5f, -1f);
            var text = label.AddComponent<TextMesh>();
            text.text = "RMAP03  |  A/D: move and leave Grab  |  S/Down: drop  |  Space: jump away\n"
                + "Safe static/destructible/moving corners grab; marked unsafe surfaces do not.";
            text.color = new Color(0.9f, 0.94f, 1f, 1f);
            text.characterSize = 0.24f;
            text.fontSize = 28;
            text.anchor = TextAnchor.UpperLeft;
        }

        private static void WriteManifest()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string absolutePath = Path.Combine(projectRoot,
                ManifestPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            string json = "{\n"
                + "  \"scene_path\": \"" + ScenePath + "\",\n"
                + "  \"player_collider\": \"0.4x0.8 foot-pivot\",\n"
                + "  \"safe_static\": \"Tilemap wall at x=8, y=1..2\",\n"
                + "  \"safe_destructible\": \"BoxCollider at x=15.5, y=2\",\n"
                + "  \"safe_moving\": \"Kinematic BoxCollider x=20.5..23.5, speed=1.5\",\n"
                + "  \"unsafe_markers\": [\"OneWay\", \"Hazard\", \"Crushing\", \"Decoration\"]\n"
                + "}\n";
            File.WriteAllText(absolutePath, json, new UTF8Encoding(false));
        }
    }
}
