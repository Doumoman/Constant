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

namespace StarNight.Character.Live.Rmap06.Editor
{
    /// <summary>Builds only the isolated RMAP06 movement and observation lab.</summary>
    public static class CharacterLiveMovementLabSceneBuilder
    {
        public const string ScenePath =
            "Assets/_Game/Map/Scenes/MoonPalace/RMAP06/MoonPalaceMovementLab_RMAP06.unity";

        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";
        private const string TerrainTilePath = "Assets/_Game/Live/Prefabs/RMAP02/Tiles/RMAP02_Terrain.asset";
        private const string ManifestPath = "MapDesign/MCP/GENERATED/RMAP06/rmap06_fixture_manifest.json";

        [MenuItem("Tools/MoonPalace/RMAP06/Build Movement And Observation Lab")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("MoonPalace_MovementLab_RMAP06", typeof(Grid));
            CreateTerrain(root.transform);
            CreateGrabSurface(root.transform, "SafeGrabCorner", new Vector2(24f, 2f),
                CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            CreateMovingGrabSurface(root.transform);
            CreateClimbAxis(root.transform, "LadderAxis", new Vector2(38f, 3.5f),
                CharacterLiveClimbSurface.SurfaceKind.Ladder);
            CreateClimbAxis(root.transform, "PoleAxis", new Vector2(45f, 3.5f),
                CharacterLiveClimbSurface.SurfaceKind.Pole);
            CreateOneWay(root.transform, new Vector2(53f, 4f));

            CharacterLivePlayerRig rig = CreatePlayer(scene);
            CreateCamera(root.transform, rig.transform);
            CreateLabel(root.transform);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            WriteManifest();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void CreateTerrain(Transform parent)
        {
            var terrainObject = new GameObject("MovementLabTerrain", typeof(Tilemap),
                typeof(TilemapRenderer), typeof(TilemapCollider2D), typeof(Rigidbody2D),
                typeof(CompositeCollider2D));
            terrainObject.transform.SetParent(parent, false);
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
            if (tile == null)
            {
                throw new InvalidOperationException("RMAP02 Terrain tile is missing: " + TerrainTilePath);
            }

            Tilemap tilemap = terrainObject.GetComponent<Tilemap>();
            for (int x = 0; x <= 62; x++)
            {
                tilemap.SetTile(new Vector3Int(x, 0, 0), tile);
            }

            // One-tile step, one-tile tunnel, and a six-plus-tile fall deck are
            // physical TilemapCollider2D fixture sections, not marker-only art.
            for (int x = 8; x <= 9; x++)
            {
                tilemap.SetTile(new Vector3Int(x, 1, 0), tile);
            }

            for (int x = 14; x <= 16; x++)
            {
                tilemap.SetTile(new Vector3Int(x, 2, 0), tile);
            }

            for (int x = 57; x <= 61; x++)
            {
                tilemap.SetTile(new Vector3Int(x, 8, 0), tile);
            }

            terrainObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            terrainObject.GetComponent<TilemapCollider2D>().compositeOperation =
                Collider2D.CompositeOperation.Merge;
        }

        private static void CreateGrabSurface(
            Transform parent,
            string name,
            Vector2 position,
            CharacterLiveGrabSurface.SurfaceKind kind)
        {
            var surface = new GameObject(name, typeof(BoxCollider2D), typeof(CharacterLiveGrabSurface));
            surface.transform.SetParent(parent, false);
            surface.transform.position = position;
            surface.GetComponent<BoxCollider2D>().size = new Vector2(1f, 3f);
            surface.GetComponent<CharacterLiveGrabSurface>().Configure(kind);
        }

        private static void CreateMovingGrabSurface(Transform parent)
        {
            var surface = new GameObject("MovingSafeGrabSolid", typeof(BoxCollider2D),
                typeof(Rigidbody2D), typeof(CharacterLiveGrabSurface),
                typeof(CharacterLiveGrabMovingSolid));
            surface.transform.SetParent(parent, false);
            BoxCollider2D collider = surface.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 3f);
            Rigidbody2D body = surface.GetComponent<Rigidbody2D>();
            surface.GetComponent<CharacterLiveGrabSurface>().Configure(
                CharacterLiveGrabSurface.SurfaceKind.MovingSafe);
            surface.GetComponent<CharacterLiveGrabMovingSolid>().Configure(body,
                new Vector2(29f, 2f), new Vector2(33f, 2f), 1.5f);
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
            trigger.size = new Vector2(0.7f, 6f);
            trigger.isTrigger = true;
            axis.GetComponent<CharacterLiveClimbSurface>().Configure(kind, trigger);
        }

        private static void CreateOneWay(Transform parent, Vector2 position)
        {
            var platform = new GameObject("OneWayPlatform", typeof(BoxCollider2D),
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
                throw new InvalidOperationException("RMAP06 Player prefab is missing: " + PlayerPrefabPath);
            }

            var player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            player.name = "RMAP06_Player";
            CharacterLivePlayerRig rig = player.GetComponent<CharacterLivePlayerRig>();
            CharacterLiveMovementDriver movement = player.GetComponent<CharacterLiveMovementDriver>();
            if (rig == null || movement == null)
            {
                throw new InvalidOperationException("RMAP06 Player prefab must contain Rig and MovementDriver.");
            }

            rig.BodyCollider.size = new Vector2(0.4f, 0.8f);
            rig.BodyCollider.offset = new Vector2(0f, 0.4f);
            if (player.GetComponent<CharacterLiveFallDamageState>() == null)
            {
                player.AddComponent<CharacterLiveFallDamageState>();
            }

            if (player.GetComponent<CharacterLiveLookModeState>() == null)
            {
                player.AddComponent<CharacterLiveLookModeState>();
            }

            movement.ConfigureRmap02(1 << LayerMask.NameToLayer("Default"));
            movement.ConfigureRmap05Fall();
            var bootstrap = new GameObject("RMAP06_Bootstrap").AddComponent<CharacterLiveMapRunBootstrap>();
            bootstrap.Configure(rig, movement, new Vector2(4f, 1f));
            return rig;
        }

        private static void CreateCamera(Transform parent, Transform followTarget)
        {
            var cameraObject = new GameObject("RMAP06_FollowCamera", typeof(Camera),
                typeof(CharacterLiveCameraFollowDriver));
            cameraObject.transform.SetParent(parent, false);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.033f, 0.05f, 1f);
            cameraObject.GetComponent<CharacterLiveCameraFollowDriver>().Configure(camera,
                followTarget, new Rect(0f, 0f, 64f, 20f), 12f, 8f, 0.08f);
        }

        private static void CreateLabel(Transform parent)
        {
            var label = new GameObject("RMAP06_Controls");
            label.transform.SetParent(parent, false);
            label.transform.position = new Vector3(1f, 18.5f, -1f);
            var text = label.AddComponent<TextMesh>();
            text.text = "RMAP06  |  Tab + A/D/W/S (or arrows): hold 1s to look 3 tiles in 8 directions\n"
                + "Release Tab or direction: return in 0.18s. Look is idle ground / static grab / static climb only.";
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
                + "  \"look\": \"Tab + 8-direction, hold=1.00s, offset=3 tiles, enter=0.24s, return=0.18s\",\n"
                + "  \"camera\": \"continuous 12x8, bounds x=0..64 y=0..20\",\n"
                + "  \"fixtures\": \"flat/run, 1-tile step, 1-tile tunnel, safe+moving grab, ladder, pole, one-way, 8-tile fall deck\"\n"
                + "}\n";
            File.WriteAllText(absolutePath, json, new UTF8Encoding(false));
        }
    }
}
