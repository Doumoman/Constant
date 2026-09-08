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

namespace StarNight.Character.Live.Rmap05.Editor
{
    /// <summary>Builds only the isolated physical RMAP05 fall-consequence fixture.</summary>
    public static class CharacterLiveFallSceneBuilder
    {
        public const string ScenePath =
            "Assets/_Game/Map/Scenes/MoonPalace/RMAP05/MoonPalaceFallDamage_RMAP05.unity";

        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";
        private const string TerrainTilePath = "Assets/_Game/Live/Prefabs/RMAP02/Tiles/RMAP02_Terrain.asset";
        private const string ManifestPath = "MapDesign/MCP/GENERATED/RMAP05/rmap05_fixture_manifest.json";

        [MenuItem("Tools/MoonPalace/RMAP05/Build Physical Fall Damage Fixture")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("MoonPalace_FallDamage_RMAP05", typeof(Grid));
            CreateGround(root.transform);
            CreateClimbAxis(root.transform, "LadderResetAxis", new Vector2(7f, 14f),
                CharacterLiveClimbSurface.SurfaceKind.Ladder);
            CreateClimbAxis(root.transform, "PoleResetAxis", new Vector2(14f, 14f),
                CharacterLiveClimbSurface.SurfaceKind.Pole);
            CreateOneWay(root.transform, new Vector2(23f, 10f));

            CharacterLivePlayerRig rig = CreatePlayer(scene);
            CreateCamera(root.transform, rig.transform);
            CreateLabel(root.transform);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            WriteManifest();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void CreateGround(Transform parent)
        {
            var terrainObject = new GameObject("FallGround", typeof(Tilemap), typeof(TilemapRenderer),
                typeof(TilemapCollider2D), typeof(Rigidbody2D), typeof(CompositeCollider2D));
            terrainObject.transform.SetParent(parent, false);
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
            if (tile == null)
            {
                throw new InvalidOperationException("RMAP02 Terrain tile is missing: " + TerrainTilePath);
            }

            Tilemap tilemap = terrainObject.GetComponent<Tilemap>();
            for (var x = 0; x <= 35; x++)
            {
                tilemap.SetTile(new Vector3Int(x, 0, 0), tile);
            }

            terrainObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            terrainObject.GetComponent<TilemapCollider2D>().compositeOperation =
                Collider2D.CompositeOperation.Merge;
        }

        private static void CreateClimbAxis(
            Transform parent,
            string name,
            Vector2 position,
            CharacterLiveClimbSurface.SurfaceKind kind)
        {
            var axis = new GameObject(name, typeof(BoxCollider2D), typeof(CharacterLiveClimbSurface));
            axis.transform.SetParent(parent, false);
            axis.transform.position = position;
            BoxCollider2D trigger = axis.GetComponent<BoxCollider2D>();
            trigger.size = new Vector2(0.7f, 10f);
            trigger.isTrigger = true;
            axis.GetComponent<CharacterLiveClimbSurface>().Configure(kind, trigger);
        }

        private static void CreateOneWay(Transform parent, Vector2 position)
        {
            var platform = new GameObject("FallOneWay", typeof(BoxCollider2D),
                typeof(PlatformEffector2D), typeof(CharacterLiveOneWayPlatform),
                typeof(CharacterLiveGrabSurface));
            platform.transform.SetParent(parent, false);
            platform.transform.position = position;
            BoxCollider2D collider = platform.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(5f, 0.25f);
            collider.usedByEffector = true;
            platform.GetComponent<PlatformEffector2D>().useOneWay = true;
            platform.GetComponent<CharacterLiveOneWayPlatform>().Configure(collider);
            platform.GetComponent<CharacterLiveGrabSurface>().Configure(
                CharacterLiveGrabSurface.SurfaceKind.OneWay);
        }

        private static CharacterLivePlayerRig CreatePlayer(Scene scene)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("RMAP05 Player prefab is missing: " + PlayerPrefabPath);
            }

            var player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            player.name = "RMAP05_Player";
            CharacterLivePlayerRig rig = player.GetComponent<CharacterLivePlayerRig>();
            CharacterLiveMovementDriver movement = player.GetComponent<CharacterLiveMovementDriver>();
            if (rig == null || movement == null)
            {
                throw new InvalidOperationException("RMAP05 Player prefab must contain Rig and MovementDriver.");
            }

            rig.BodyCollider.size = new Vector2(0.4f, 0.8f);
            rig.BodyCollider.offset = new Vector2(0f, 0.4f);
            if (player.GetComponent<CharacterLiveFallDamageState>() == null)
            {
                player.AddComponent<CharacterLiveFallDamageState>();
            }

            movement.ConfigureRmap02(1 << LayerMask.NameToLayer("Default"));
            movement.ConfigureRmap05Fall();
            var bootstrap = new GameObject("RMAP05_Bootstrap").AddComponent<CharacterLiveMapRunBootstrap>();
            bootstrap.Configure(rig, movement, new Vector2(3f, 32f));
            return rig;
        }

        private static void CreateCamera(Transform parent, Transform followTarget)
        {
            var cameraObject = new GameObject("RMAP05_FollowCamera", typeof(Camera),
                typeof(CharacterLiveCameraFollowDriver));
            cameraObject.transform.SetParent(parent, false);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.033f, 0.05f, 1f);
            cameraObject.GetComponent<CharacterLiveCameraFollowDriver>().Configure(camera,
                followTarget, new Rect(0f, 0f, 36f, 34f), 12f, 8f, 0.08f);
        }

        private static void CreateLabel(Transform parent)
        {
            var label = new GameObject("RMAP05_Controls");
            label.transform.SetParent(parent, false);
            label.transform.position = new Vector3(1f, 32f, -1f);
            var text = label.AddComponent<TextMesh>();
            text.text = "RMAP05  |  6+: 0.5s landing stun  |  10/15/20/25: 1/2/3/4 damage  |  30+: death\n"
                + "Grab and W/S climb entry reset fall distance. One-way landing applies damage before reset.";
            text.color = new Color(0.9f, 0.94f, 1f, 1f);
            text.characterSize = 0.20f;
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
                + "  \"spawn\": \"x=3, y=32 (31-tile fall to terrain top y=1)\",\n"
                + "  \"ladder\": \"trigger center x=7, y=14, size=0.7x10\",\n"
                + "  \"pole\": \"trigger center x=14, y=14, size=0.7x10\",\n"
                + "  \"one_way\": \"PlatformEffector2D BoxCollider center x=23, y=10, size=5x0.25\",\n"
                + "  \"health\": \"max=5\",\n"
                + "  \"fall_table\": \"<6=0, 6-<10=stun, 10/15/20/25=1/2/3/4, >=30=death\"\n"
                + "}\n";
            File.WriteAllText(absolutePath, json, new UTF8Encoding(false));
        }
    }
}
