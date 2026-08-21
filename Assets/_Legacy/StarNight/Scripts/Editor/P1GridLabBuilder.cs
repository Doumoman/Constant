#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Debugging;
using StarNight.Grid;
using StarNight.Player;
using StarNight.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Editor
{
    public static class P1GridLabBuilder
    {
        public const string ScenePath = "Assets/StarNight/Scenes/Labs/P1_GridLab_30x18.unity";
        public const string PlayerPrefabPath = "Assets/StarNight/Prefabs/Gameplay/P1_Player.prefab";
        public const string TuningPath = "Assets/StarNight/Settings/P1_MovementTuning.asset";
        public const string TerrainTilePath = "Assets/StarNight/Data/Tiles/P1_LabTerrain.asset";
        public const string HazardTilePath = "Assets/StarNight/Data/Tiles/P1_LabHazard.asset";
        public const string PhysicsMaterialPath = "Assets/StarNight/Art/Materials/P1_Player_NoFriction.physicsMaterial2D";

        private const string TerrainTexturePath = "Assets/StarNight/Art/Materials/P1_LabTerrainTexture.asset";
        private const string HazardTexturePath = "Assets/StarNight/Art/Materials/P1_LabHazardTexture.asset";
        private const string InputActionsPath = "Assets/StarNight/Input/StarNightControls.inputactions";
        private const string PlayerSpritePath = "Assets/StarNight/Art/Player/char_black_full.png";
        private const string PlayerSpriteName = "char_black_full_0";
        private const string PlayerAnimatorPath = "Assets/StarNight/Art/Player/Animations/PlayerAnim.controller";

        [MenuItem("StarNight/P1/Rebuild Grid Lab")]
        public static void RebuildGridLab()
        {
            P1MovementTuning tuning = RebuildTuning();
            PhysicsMaterial2D noFriction = RebuildPhysicsMaterial();
            Tile terrainTile = RebuildTile(
                TerrainTexturePath,
                TerrainTilePath,
                "P1_LabTerrain",
                new Color32(52, 73, 112, 255),
                new Color32(102, 154, 220, 255),
                Tile.ColliderType.Grid);
            Tile hazardTile = RebuildTile(
                HazardTexturePath,
                HazardTilePath,
                "P1_LabHazard",
                new Color32(176, 52, 76, 220),
                new Color32(255, 132, 115, 255),
                Tile.ColliderType.None);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            tuning = AssetDatabase.LoadAssetAtPath<P1MovementTuning>(TuningPath);
            noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(PhysicsMaterialPath);
            terrainTile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
            hazardTile = AssetDatabase.LoadAssetAtPath<Tile>(HazardTilePath);

            GameObject playerPrefab = RebuildPlayerPrefab(tuning, noFriction);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            tuning = AssetDatabase.LoadAssetAtPath<P1MovementTuning>(TuningPath);
            terrainTile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
            hazardTile = AssetDatabase.LoadAssetAtPath<Tile>(HazardTilePath);
            playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

            if (tuning == null || terrainTile == null || hazardTile == null || playerPrefab == null)
            {
                throw new System.InvalidOperationException("P1 generated assets failed to reload before scene assembly.");
            }

            BuildScene(tuning, terrainTile, hazardTile, playerPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[StarNight P1] Grid Lab rebuilt: {ScenePath}");
        }

        private static P1MovementTuning RebuildTuning()
        {
            AssetDatabase.DeleteAsset(TuningPath);
            P1MovementTuning tuning = ScriptableObject.CreateInstance<P1MovementTuning>();
            tuning.name = "P1_MovementTuning";
            AssetDatabase.CreateAsset(tuning, TuningPath);
            return tuning;
        }

        private static PhysicsMaterial2D RebuildPhysicsMaterial()
        {
            AssetDatabase.DeleteAsset(PhysicsMaterialPath);
            PhysicsMaterial2D material = new PhysicsMaterial2D("P1_Player_NoFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
            AssetDatabase.CreateAsset(material, PhysicsMaterialPath);
            return material;
        }

        private static Tile RebuildTile(
            string texturePath,
            string tilePath,
            string name,
            Color32 fill,
            Color32 edge,
            Tile.ColliderType colliderType)
        {
            AssetDatabase.DeleteAsset(tilePath);
            AssetDatabase.DeleteAsset(texturePath);

            const int resolution = 48;
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                name = name + "_Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[resolution * resolution];
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    bool isEdge = x < 2 || y < 2 || x >= resolution - 2 || y >= resolution - 2;
                    bool checker = ((x / 8) + (y / 8)) % 2 == 0;
                    pixels[y * resolution + x] = isEdge
                        ? edge
                        : checker ? fill : Lerp(fill, edge, 0.12f);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            AssetDatabase.CreateAsset(texture, texturePath);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, resolution, resolution),
                new Vector2(0.5f, 0.5f),
                resolution,
                0,
                SpriteMeshType.FullRect);
            sprite.name = name + "_Sprite";
            AssetDatabase.AddObjectToAsset(sprite, texture);

            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            tile.sprite = sprite;
            tile.colliderType = colliderType;
            AssetDatabase.CreateAsset(tile, tilePath);
            return tile;
        }

        private static GameObject RebuildPlayerPrefab(
            P1MovementTuning tuning,
            PhysicsMaterial2D noFriction)
        {
            AssetDatabase.DeleteAsset(PlayerPrefabPath);

            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            Sprite sourceSprite = null;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(PlayerSpritePath))
            {
                if (asset is Sprite sprite && sprite.name == PlayerSpriteName)
                {
                    sourceSprite = sprite;
                    break;
                }
            }

            RuntimeAnimatorController sourceAnimator =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerAnimatorPath);

            GameObject root = new GameObject("P1_Player");
            root.tag = "Player";
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                root.layer = playerLayer;
            }

            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.mass = 1f;
            body.linearDamping = 0f;
            body.angularDamping = 0.05f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            CapsuleCollider2D capsule = root.AddComponent<CapsuleCollider2D>();
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.size = tuning.ColliderSize;
            capsule.offset = Vector2.zero;
            capsule.sharedMaterial = noFriction;

            PlayerInputAdapter input = root.AddComponent<PlayerInputAdapter>();
            input.Configure(inputActions, "Gameplay");

            GroundProbe2D groundProbe = root.AddComponent<GroundProbe2D>();
            int groundLayer = LayerMask.NameToLayer("Ground");
            LayerMask groundMask = groundLayer >= 0 ? 1 << groundLayer : Physics2D.DefaultRaycastLayers;
            groundProbe.Configure(capsule, groundMask, tuning.GroundProbeDistance);

            PlayerMotor2D motor = root.AddComponent<PlayerMotor2D>();
            motor.Configure(body, input, groundProbe, tuning);
            SafeCellTracker safeCellTracker = root.AddComponent<SafeCellTracker>();
            PlayerRecovery recovery = root.AddComponent<PlayerRecovery>();

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 0.72f;
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sourceSprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 0;

            Animator animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = sourceAnimator;

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            PlayerVisual2D playerVisual = root.AddComponent<PlayerVisual2D>();
            playerVisual.Configure(motor, renderer, animator);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        private static void BuildScene(
            P1MovementTuning tuning,
            Tile terrainTile,
            Tile hazardTile,
            GameObject playerPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "P1_GridLab_30x18";

            tuning = AssetDatabase.LoadAssetAtPath<P1MovementTuning>(TuningPath);
            terrainTile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
            hazardTile = AssetDatabase.LoadAssetAtPath<Tile>(HazardTilePath);
            playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (tuning == null || terrainTile == null || hazardTile == null || playerPrefab == null)
            {
                throw new System.InvalidOperationException("P1 assets failed to reload after creating the lab scene.");
            }

            GameObject labRoot = new GameObject("P1_GridLab_30x18");

            GameObject gridRoot = new GameObject("GridWorld");
            gridRoot.transform.SetParent(labRoot.transform);
            UnityEngine.Grid grid = gridRoot.AddComponent<UnityEngine.Grid>();
            grid.cellSize = Vector3.one;
            grid.cellGap = Vector3.zero;

            GameObject terrainObject = new GameObject("Terrain");
            terrainObject.layer = LayerMask.NameToLayer("Ground");
            terrainObject.transform.SetParent(gridRoot.transform);
            Tilemap terrain = terrainObject.AddComponent<Tilemap>();
            TilemapRenderer terrainRenderer = terrainObject.AddComponent<TilemapRenderer>();
            terrainRenderer.sortingOrder = 0;
            Rigidbody2D terrainBody = terrainObject.AddComponent<Rigidbody2D>();
            terrainBody.bodyType = RigidbodyType2D.Static;
            TilemapCollider2D terrainCollider = terrainObject.AddComponent<TilemapCollider2D>();
            terrainCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            CompositeCollider2D composite = terrainObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.generationType = CompositeCollider2D.GenerationType.Synchronous;

            GameObject hazardObject = new GameObject("Hazard");
            hazardObject.transform.SetParent(gridRoot.transform);
            Tilemap hazards = hazardObject.AddComponent<Tilemap>();
            TilemapRenderer hazardRenderer = hazardObject.AddComponent<TilemapRenderer>();
            hazardRenderer.sortingOrder = 1;

            GridWorld gridWorld = gridRoot.AddComponent<GridWorld>();
            gridWorld.Configure(
                grid,
                terrain,
                hazards,
                Vector2Int.zero,
                new Vector2Int(P1GridLabContract.Width, P1GridLabContract.Height));

            PopulateTerrain(terrain, terrainTile);
            for (int x = P1GridLabContract.RecoveryPitMinX; x <= P1GridLabContract.RecoveryPitMaxXInclusive; x++)
            {
                hazards.SetTile(new Vector3Int(x, 0, 0), hazardTile);
            }

            terrain.RefreshAllTiles();
            hazards.RefreshAllTiles();
            terrainCollider.ProcessTilemapChanges();
            composite.GenerateGeometry();
            EditorUtility.SetDirty(terrain);
            EditorUtility.SetDirty(hazards);
            if (terrain.GetUsedTilesCount() == 0
                || hazards.GetUsedTilesCount() == 0
                || composite.pathCount == 0)
            {
                throw new System.InvalidOperationException(
                    "P1 Grid Lab tile population or composite collision generation failed.");
            }

            Physics2D.SyncTransforms();

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "Player";
            player.transform.SetParent(labRoot.transform);
            player.transform.position = P1GridLabContract.PlayerSpawn;

            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
            playerBody.position = P1GridLabContract.PlayerSpawn;
            Physics2D.SyncTransforms();
            CapsuleCollider2D playerCapsule = player.GetComponent<CapsuleCollider2D>();
            PlayerMotor2D motor = player.GetComponent<PlayerMotor2D>();
            SafeCellTracker safeCellTracker = player.GetComponent<SafeCellTracker>();
            safeCellTracker.Configure(gridWorld, playerBody, playerCapsule, motor, tuning);
            safeCellTracker.SetSpawnFallback(playerBody.position);
            PlayerRecovery recovery = player.GetComponent<PlayerRecovery>();
            recovery.Configure(gridWorld, playerBody, motor, safeCellTracker, tuning);

            GameObject recoveryVolumeObject = new GameObject("FallRecoveryVolume");
            recoveryVolumeObject.transform.SetParent(labRoot.transform);
            recoveryVolumeObject.transform.position = new Vector3(26.5f, -0.5f, 0f);
            BoxCollider2D recoveryTrigger = recoveryVolumeObject.AddComponent<BoxCollider2D>();
            recoveryTrigger.size = new Vector2(3f, 1f);
            recoveryTrigger.isTrigger = true;
            recoveryVolumeObject.AddComponent<RecoveryVolume2D>();

            Camera camera = BuildCamera(labRoot.transform);
            GridBoundedCamera2D cameraFollow = camera.gameObject.AddComponent<GridBoundedCamera2D>();
            cameraFollow.Configure(camera, player.transform, playerBody, gridWorld, recovery);

            BuildDirectionalLight(labRoot.transform);
            BuildLabels(labRoot.transform);

            GameObject telemetryObject = new GameObject("P1_Telemetry");
            telemetryObject.transform.SetParent(labRoot.transform);
            P1GridLabTelemetry telemetry = telemetryObject.AddComponent<P1GridLabTelemetry>();
            telemetry.Configure(gridWorld, motor, safeCellTracker, recovery);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = player;
        }

        private static void PopulateTerrain(Tilemap terrain, Tile tile)
        {
            HashSet<Vector3Int> cells = new HashSet<Vector3Int>();

            for (int x = 0; x < P1GridLabContract.Width; x++)
            {
                if (x < P1GridLabContract.RecoveryPitMinX || x > P1GridLabContract.RecoveryPitMaxXInclusive)
                {
                    cells.Add(new Vector3Int(x, 0, 0));
                }
            }

            for (int y = 0; y < P1GridLabContract.Height; y++)
            {
                cells.Add(new Vector3Int(0, y, 0));
                cells.Add(new Vector3Int(P1GridLabContract.Width - 1, y, 0));
            }

            for (int x = P1GridLabContract.TunnelMinX; x < P1GridLabContract.TunnelMaxXExclusive; x++)
            {
                cells.Add(new Vector3Int(x, P1GridLabContract.TunnelCeilingY, 0));
            }

            cells.Add(new Vector3Int(12, 1, 0));
            cells.Add(new Vector3Int(13, 1, 0));
            cells.Add(new Vector3Int(13, 2, 0));
            cells.Add(new Vector3Int(14, 1, 0));
            cells.Add(new Vector3Int(14, 2, 0));
            cells.Add(new Vector3Int(14, 3, 0));

            for (int x = 14; x <= 16; x++)
            {
                cells.Add(new Vector3Int(x, P1GridLabContract.JumpPlatformY, 0));
            }

            for (int x = 20; x <= 24; x++)
            {
                cells.Add(new Vector3Int(x, P1GridLabContract.JumpPlatformY, 0));
            }

            foreach (Vector3Int cell in cells)
            {
                terrain.SetTile(cell, tile);
            }
        }

        private static Camera BuildCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent);
            cameraObject.transform.position = new Vector3(9f, 5.25f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.0625f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.055f, 0.11f, 1f);
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void BuildDirectionalLight(Transform parent)
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(parent);
            lightObject.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.color = new Color(0.82f, 0.9f, 1f);
        }

        private static void BuildLabels(Transform parent)
        {
            CreateLabel(parent, "Title", "P1 GRID LAB  30 x 18", new Vector3(6.5f, 5.2f, 0f), 0.11f);
            CreateLabel(parent, "TunnelLabel", "1-CELL TUNNEL", new Vector3(6.5f, 3.2f, 0f), 0.075f);
            CreateLabel(parent, "JumpLabel", "3-CELL JUMP GAP", new Vector3(18.5f, 7.0f, 0f), 0.075f);
            CreateLabel(parent, "FallLabel", "SAFE FALL", new Vector3(26.5f, 4.7f, 0f), 0.075f);
        }

        private static void CreateLabel(
            Transform parent,
            string name,
            string text,
            Vector3 position,
            float characterSize)
        {
            GameObject labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent);
            labelObject.transform.position = position;
            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = characterSize;
            textMesh.color = new Color(0.72f, 0.86f, 1f, 0.9f);
        }

        private static Color32 Lerp(Color32 left, Color32 right, float t)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(left.r, right.r, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(left.g, right.g, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(left.b, right.b, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(left.a, right.a, t)));
        }
    }
}

#endif
