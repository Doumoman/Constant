#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Interaction.Carry;
using StarNight.Interaction.Reactions;
using StarNight.Interaction.Targeting;
using StarNight.Map;
using StarNight.Tools.Bomb;
using StarNight.Tools.Core;
using StarNight.Tools.HookLauncher;
using StarNight.Tools.Pickaxe;
using StarNight.Tools.Pounder;
using StarNight.Tools.Rope;
using StarNight.Tools.Shovel;
using StarNight.Tools.Umbrella;
using StarNight.Tools.Watering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.ToolAuthoring
{
    public static class ToolInteractionLabBuilder
    {
        public const string ScenePath = "Assets/_Game/Editor/ToolAuthoring/Scenes/02_ToolInteractionLab.unity";
        public const string ToolDataFolder = "Assets/_Game/Tools/Data/HandTools";
        public const string CarryDataFolder = "Assets/_Game/Interaction/Data/Carry";
        public const string HandToolPrefabFolder = "Assets/_Game/Tools/Prefabs/HandTools";
        public const string InteractionPrefabFolder = "Assets/_Game/Tools/Prefabs/Interaction";

        private static readonly string[] StationNames =
        {
            "BombStation", "RopeStation", "PickaxeStation", "ShovelStation",
            "WateringStation", "PounderStation", "HookStation", "UmbrellaStation",
        };

        private static readonly string[] ZoneNames =
        {
            "InteractionPriorityZone", "DropPlacementZone", "ThrowLane", "BombChamber", "RopeTower",
            "SoilGarden", "PoundRoom", "HookLane", "WindTunnel", "PortalCarryZone",
        };

        [MenuItem("Tools/별을 물어오는 밤/Tool Interaction Lab 재생성")]
        [MenuItem("Tools/StarNight/Tool Interaction Lab/Rebuild")]
        public static void Rebuild()
        {
            BuildDataAssets();
            CloseStaleUntitledLabScene();
            Scene previous = SceneManager.GetActiveScene();
            Scene lab = default;
            try
            {
                lab = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(lab);
                BuildSceneHierarchy(lab);
                EditorSceneManager.SaveScene(lab, ScenePath);
            }
            finally
            {
                if (lab.IsValid() && lab.isLoaded)
                {
                    EditorSceneManager.CloseScene(lab, true);
                }
                if (previous.IsValid() && previous.isLoaded)
                {
                    SceneManager.SetActiveScene(previous);
                }
            }
            RemoveFromBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"TOOL-05 lab rebuilt: {ScenePath}");
        }

        private static void CloseStaleUntitledLabScene()
        {
            for (int sceneIndex = SceneManager.sceneCount - 1; sceneIndex >= 0; sceneIndex--)
            {
                Scene candidate = SceneManager.GetSceneAt(sceneIndex);
                if (!candidate.IsValid() || !candidate.isLoaded || !string.IsNullOrEmpty(candidate.path))
                {
                    continue;
                }

                foreach (GameObject root in candidate.GetRootGameObjects())
                {
                    if (root.name == "ToolInteractionLab")
                    {
                        EditorSceneManager.CloseScene(candidate, true);
                        break;
                    }
                }
            }
        }

        [MenuItem("Tools/별을 물어오는 밤/Tool Interaction Lab 씬 열기")]
        public static void Open()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                Rebuild();
            }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        public static void BuildDataAssets()
        {
            EnsureFolder(ToolDataFolder);
            EnsureFolder(CarryDataFolder);
            CreateOrUpdateTool("TOOL_PICKAXE", "곡괭이", ToolTag.Pickaxe | ToolTag.LightImpact,
                ToolResourceMode.Durability, 12, 250, 0.10f, 0.12f, 0.22f,
                ToolAimMode.UpOrFacing, new[] { Vector2Int.right, Vector2Int.up }, 1, 0f, 0.55f);
            CreateOrUpdateTool("TOOL_SHOVEL", "삽", ToolTag.Shovel | ToolTag.LightImpact,
                ToolResourceMode.Durability, 10, 200, 0.14f, 0.15f, 0.25f,
                ToolAimMode.Facing, new[] { Vector2Int.right }, 1, 0f, 1f);
            CreateOrUpdateTool("TOOL_WATERING_CAN", "물뿌리개", ToolTag.Water,
                ToolResourceMode.Charge, 6, 200, 0.08f, 0.42f, 0.15f,
                ToolAimMode.UpOrFacing, new[] { Vector2Int.right, Vector2Int.up }, 3, 0f, 1f, 0.08f);
            CreateOrUpdateTool("TOOL_POUNDER", "달토끼의 절굿공이", ToolTag.Pound | ToolTag.HeavyImpact,
                ToolResourceMode.Durability, 8, 300, 0.16f, 0.18f, 0.28f,
                ToolAimMode.Facing, new[] { Vector2Int.right, Vector2Int.down }, 1, 0f, 0.25f);
            CreateOrUpdateTool("TOOL_HOOK_LAUNCHER", "갈고리 발사기", ToolTag.Hook,
                ToolResourceMode.Infinite, 0, 500, 0.12f, 0.12f, 0.25f,
                ToolAimMode.UpOrFacing, new[] { Vector2Int.right, Vector2Int.up }, 7, 0f, 1f);
            CreateOrUpdateTool("TOOL_WIND_UMBRELLA", "바람 우산", ToolTag.WindGuard,
                ToolResourceMode.Infinite, 0, 300, 0.15f, 0.15f, 0.10f,
                ToolAimMode.Toggle, Array.Empty<Vector2Int>(), 1, 120f, 1f);

            CreateOrUpdateCarry("LAB_CARRY_LIGHT", CarryWeightClass.Light, false);
            CreateOrUpdateCarry("LAB_CARRY_MEDIUM", CarryWeightClass.Medium, false);
            CreateOrUpdateCarry("LAB_CARRY_HEAVY", CarryWeightClass.Heavy, false);
            CreateOrUpdateCarry("LAB_CARRY_CRITICAL", CarryWeightClass.Light, true);
            CreateHandToolPrefabs();
            ConfigurePlayerPrefab();
            ConfigureWaterRechargePrefab();
        }

        private static void BuildSceneHierarchy(Scene scene)
        {
            GameObject root = Create("ToolInteractionLab", null);
            GameObject bootstrap = Create("LabBootstrap", root.transform);
            ToolInteractionLabController controller = bootstrap.AddComponent<ToolInteractionLabController>();
            Create("Tool13Approval", root.transform).AddComponent<Tool13AcceptanceRunner>();

            GameObject testGrid = Create("TestGrid", root.transform);
            testGrid.AddComponent<Grid>();
            CreateTilemap("TerrainCollisionTilemap", testGrid.transform, "TerrainSolid");
            CreateTilemap("OneWayCollisionTilemap", testGrid.transform, "TerrainOneWay");
            CreateTilemap("UnbreakableBoundaryTilemap", testGrid.transform, "UnbreakableBoundary");
            CreateTilemap("LogicTilemap", testGrid.transform, "Default");
            CreateCollisionBox(testGrid.transform, "LowerLabFloor", new Vector2(0f, -2.25f), new Vector2(40f, 0.5f), "TerrainSolid");
            CreateCollisionBox(testGrid.transform, "UpperLabFloor", new Vector2(0f, 5.75f), new Vector2(40f, 0.5f), "TerrainSolid");

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Player/Prefabs/Player.prefab");
            GameObject player = playerPrefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene)
                : new GameObject("PlayerTestRig");
            player.name = "PlayerTestRig";
            player.transform.SetParent(root.transform, false);
            player.transform.localPosition = new Vector3(-16f, -1.25f, 0f);
            controller.Configure(player);

            GameObject toolRack = Create("ToolRack", root.transform);
            UnityEngine.Object[] stationAssets = LoadStationAssets();
            for (int index = 0; index < StationNames.Length; index++)
            {
                GameObject station = Create(StationNames[index], toolRack.transform);
                station.transform.position = new Vector3(-14f + index * 4f, 11.5f, 0f);
                station.AddComponent<ToolInteractionLabStation>().Configure((ToolLabStationKind)index, stationAssets[index]);
                if (stationAssets[index] is HandToolDefinition toolDefinition
                    && toolDefinition.RuntimePrefab != null)
                {
                    GameObject pickup = (GameObject)PrefabUtility.InstantiatePrefab(toolDefinition.RuntimePrefab, scene);
                    pickup.name = "Pickup";
                    pickup.transform.SetParent(station.transform, false);
                    pickup.transform.localPosition = Vector3.zero;
                }
                if ((ToolLabStationKind)index == ToolLabStationKind.WateringCan)
                {
                    CreateWaterRechargeSource(station.transform, scene);
                }
            }

            GameObject carryRack = Create("CarryObjectRack", root.transform);
            string[] carryNames = { "LightObjects", "MediumObjects", "HeavyObjects", "CriticalObjects" };
            for (int index = 0; index < carryNames.Length; index++)
            {
                GameObject rack = Create(carryNames[index], carryRack.transform);
                rack.transform.position = new Vector3(-9f + index * 6f, -5.5f, 0f);
            }

            GameObject testZones = Create("TestZones", root.transform);
            for (int index = 0; index < ZoneNames.Length; index++)
            {
                GameObject zone = Create(ZoneNames[index], testZones.transform);
                int row = index / 5;
                int column = index % 5;
                zone.transform.position = new Vector3(-16f + column * 8f, row == 0 ? 0f : 8f, 0f);
                zone.AddComponent<ToolInteractionLabZone>().Configure((ToolLabZoneKind)index, new Vector2(6f, 4f));
                if ((ToolLabZoneKind)index == ToolLabZoneKind.RopeTower)
                {
                    CreateCollisionBox(zone.transform, "RopeTowerCeiling", new Vector2(0f, 2.8f), new Vector2(5f, 0.4f), "TerrainSolid");
                }
                if ((ToolLabZoneKind)index == ToolLabZoneKind.BombChamber)
                {
                    CreateCollisionBox(zone.transform, "BombSafeBoundary", new Vector2(2.7f, 0f), new Vector2(0.35f, 4f), "UnbreakableBoundary");
                }
                if ((ToolLabZoneKind)index == ToolLabZoneKind.HookLane)
                {
                    CreateHookLaneTargets(zone.transform);
                }
                if ((ToolLabZoneKind)index == ToolLabZoneKind.WindTunnel)
                {
                    CreateWindTunnelTargets(zone.transform);
                }
            }

            GameObject reactionWall = Create("ReactionTargetWall", root.transform);
            reactionWall.transform.position = new Vector3(0f, -9f, 0f);
            for (int index = 0; index < 6; index++)
            {
                GameObject slot = Create($"ReactionTarget_{index:00}", reactionWall.transform);
                slot.transform.localPosition = new Vector3(-7.5f + index * 3f, 0f, 0f);
            }
            PopulateReactionTargets(reactionWall, scene);

            GameObject cameraObject = Create("Camera", root.transform);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 12f;
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 4f, -20f);
            GameObject lightObject = Create("LabLight", root.transform);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Create("LabUI", root.transform);
        }

        private static void CreateOrUpdateTool(
            string id, string displayName, ToolTag tags, ToolResourceMode resourceMode,
            int resource, int price, float windup, float active, float recovery,
            ToolAimMode aim, Vector2Int[] offsets, int range, float angle, float movementMultiplier,
            float impactOverride = -1f)
        {
            string path = $"{ToolDataFolder}/{id}.asset";
            HandToolDefinition asset = AssetDatabase.LoadAssetAtPath<HandToolDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<HandToolDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }
            var action = new ToolActionProfile
            {
                WindupSeconds = windup,
                ImpactSeconds = impactOverride >= 0f ? impactOverride : active,
                ActiveSeconds = active,
                RecoverySeconds = recovery,
                MovementMultiplier = movementMultiplier,
                AimMode = aim,
            };
            var air = new ToolActionProfile
            {
                WindupSeconds = windup,
                ImpactSeconds = impactOverride >= 0f ? impactOverride : active,
                ActiveSeconds = active,
                RecoverySeconds = recovery,
                MovementMultiplier = movementMultiplier,
                AimMode = id == "TOOL_POUNDER" ? ToolAimMode.DownAutomatic : aim,
            };
            asset.Configure(id, displayName, tags, resourceMode, resource, price, action, air, offsets, range, angle);
            EditorUtility.SetDirty(asset);
        }

        private static void CreateOrUpdateCarry(string id, CarryWeightClass weight, bool critical)
        {
            string path = $"{CarryDataFolder}/{id}.asset";
            CarryObjectDefinition asset = AssetDatabase.LoadAssetAtPath<CarryObjectDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CarryObjectDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.ConfigureForTests(id, weight, Vector2Int.one, PrimaryUseMode.Throw, critical);
            EditorUtility.SetDirty(asset);
        }

        private static void CreateHandToolPrefabs()
        {
            EnsureFolder(HandToolPrefabFolder);
            CreateHandToolPrefab<PickaxeRuntime>("Pickaxe", "TOOL_PICKAXE", new Color(0.35f, 0.78f, 0.95f));
            CreateHandToolPrefab<ShovelRuntime>("Shovel", "TOOL_SHOVEL", new Color(0.72f, 0.48f, 0.22f));
            CreateHandToolPrefab<WateringCanRuntime>("WateringCan", "TOOL_WATERING_CAN", new Color(0.25f, 0.68f, 1f));
            CreateHandToolPrefab<PounderRuntime>("Pounder", "TOOL_POUNDER", new Color(0.95f, 0.64f, 0.24f));
            CreateHandToolPrefab<HookLauncherRuntime>("HookLauncher", "TOOL_HOOK_LAUNCHER", new Color(0.72f, 0.82f, 0.92f));
            CreateHandToolPrefab<WindUmbrellaRuntime>("WindUmbrella", "TOOL_WIND_UMBRELLA", new Color(0.48f, 0.92f, 0.72f));
        }

        private static void CreateHandToolPrefab<T>(
            string prefabName,
            string toolId,
            Color color) where T : HandToolRuntime
        {
            HandToolDefinition definition = AssetDatabase.LoadAssetAtPath<HandToolDefinition>(
                $"{ToolDataFolder}/{toolId}.asset");
            if (definition == null)
            {
                return;
            }

            var root = new GameObject(prefabName);
            try
            {
                int interactionLayer = LayerMask.NameToLayer("Interaction");
                if (interactionLayer >= 0) root.layer = interactionLayer;
                Rigidbody2D body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                BoxCollider2D targetCollider = root.AddComponent<BoxCollider2D>();
                targetCollider.isTrigger = true;
                targetCollider.size = new Vector2(0.6f, 0.6f);
                InteractionCandidate candidate = root.AddComponent<InteractionCandidate>();
                candidate.ConfigureForTests(InteractionTargetKind.Pickup, Math.Abs(toolId.GetHashCode()));
                SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                renderer.color = color;
                renderer.transform.localScale = new Vector3(0.42f, 0.72f, 1f);
                T runtime = root.AddComponent<T>();
                runtime.Configure(definition);

                string prefabPath = $"{HandToolPrefabFolder}/{prefabName}.prefab";
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                definition.AssignRuntimePrefab(prefab);
                EditorUtility.SetDirty(definition);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ConfigurePlayerPrefab()
        {
            const string playerPath = "Assets/_Game/Player/Prefabs/Player.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                if (root.GetComponent<ToolReactionDispatcher>() == null)
                {
                    root.AddComponent<ToolReactionDispatcher>();
                }
                if (root.GetComponent<ToolActionController>() == null)
                {
                    root.AddComponent<ToolActionController>();
                }
                if (root.GetComponent<HookActionController>() == null)
                {
                    root.AddComponent<HookActionController>();
                }
                if (root.GetComponent<UmbrellaActionController>() == null)
                {
                    root.AddComponent<UmbrellaActionController>();
                }
                PrefabUtility.SaveAsPrefabAsset(root, playerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureWaterRechargePrefab()
        {
            EnsureFolder(InteractionPrefabFolder);
            var root = new GameObject("WaterRechargeSource");
            try
            {
                int layer = LayerMask.NameToLayer("Interaction");
                if (layer >= 0) root.layer = layer;
                Rigidbody2D body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                BoxCollider2D targetCollider = root.AddComponent<BoxCollider2D>();
                targetCollider.isTrigger = true;
                targetCollider.size = new Vector2(1.1f, 1.1f);
                InteractionCandidate candidate = root.AddComponent<InteractionCandidate>();
                candidate.ConfigureForTests(InteractionTargetKind.RequiredHandSlotReceiver, 71001);
                root.AddComponent<ToolRechargeReceiver>();
                SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                renderer.color = new Color(0.22f, 0.72f, 1f, 0.85f);
                renderer.transform.localScale = new Vector3(0.65f, 0.9f, 1f);
                PrefabUtility.SaveAsPrefabAsset(
                    root,
                    $"{InteractionPrefabFolder}/WaterRechargeSource.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateWaterRechargeSource(Transform parent, Scene scene)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{InteractionPrefabFolder}/WaterRechargeSource.prefab");
            if (prefab == null)
            {
                return;
            }
            GameObject source = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            source.name = "RechargeSource";
            source.transform.SetParent(parent, false);
            source.transform.localPosition = new Vector3(0f, -1.25f, 0f);
        }

        private static void CreateHookLaneTargets(Transform parent)
        {
            GameObject worldAnchor = Create("WorldAnchor", parent);
            worldAnchor.transform.localPosition = new Vector3(2.2f, 1.2f, 0f);
            SetLayerIfDefined(worldAnchor, "Interaction");
            BoxCollider2D worldCollider = worldAnchor.AddComponent<BoxCollider2D>();
            worldCollider.isTrigger = true;
            worldCollider.size = new Vector2(0.5f, 0.5f);
            worldAnchor.AddComponent<HookTarget>().ConfigureForTests(HookResponse.PullPlayerToTarget);

            GameObject pullObject = Create("PullObject", parent);
            pullObject.transform.localPosition = new Vector3(2.2f, -0.2f, 0f);
            SetLayerIfDefined(pullObject, "DynamicObject");
            Rigidbody2D pullBody = pullObject.AddComponent<Rigidbody2D>();
            pullBody.bodyType = RigidbodyType2D.Dynamic;
            pullBody.gravityScale = 0f;
            pullObject.AddComponent<BoxCollider2D>().size = Vector2.one * 0.65f;
            pullObject.AddComponent<HookTarget>().ConfigureForTests(HookResponse.PullToPlayer, pullBody);

            GameObject trigger = Create("RemoteTrigger", parent);
            trigger.transform.localPosition = new Vector3(2.2f, -1.25f, 0f);
            SetLayerIfDefined(trigger, "Interaction");
            BoxCollider2D triggerCollider = trigger.AddComponent<BoxCollider2D>();
            triggerCollider.isTrigger = true;
            trigger.AddComponent<HookTarget>().ConfigureForTests(HookResponse.Trigger);

            CreateCollisionBox(
                parent,
                "PortalLineBlocker",
                new Vector2(-2.4f, 0f),
                new Vector2(0.35f, 3f),
                "PortalBoundary");
        }

        private static void CreateWindTunnelTargets(Transform parent)
        {
            GameObject projectile = Create("DeflectableProjectile", parent);
            projectile.transform.localPosition = new Vector3(2f, 0.5f, 0f);
            SetLayerIfDefined(projectile, "Hazard");
            CircleCollider2D projectileCollider = projectile.AddComponent<CircleCollider2D>();
            projectileCollider.isTrigger = true;
            projectileCollider.radius = 0.16f;
            projectile.AddComponent<CommonElementProjectile>().Configure(
                Vector2Int.left,
                5f,
                1,
                parent.gameObject,
                88001);

            GameObject laser = Create("LaserNonDeflectable", parent);
            laser.transform.localPosition = new Vector3(-2f, 0.5f, 0f);
            SetLayerIfDefined(laser, "Hazard");
            BoxCollider2D laserCollider = laser.AddComponent<BoxCollider2D>();
            laserCollider.isTrigger = true;
            laserCollider.size = new Vector2(0.25f, 2.5f);
        }

        private static void PopulateReactionTargets(GameObject reactionWall, Scene scene)
        {
            string[] prefabPaths =
            {
                "Assets/_Game/Map/Prefabs/Elements/Common/COMMON_Block_Cracked.prefab",
                "Assets/_Game/Map/Prefabs/Elements/Common/COMMON_Floor_Fragile.prefab",
                "Assets/_Game/Map/Prefabs/Elements/Common/COMMON_Block_SoftSoil.prefab",
                "Assets/_Game/Map/Prefabs/Elements/Common/COMMON_Block_Unbreakable.prefab",
            };
            for (int index = 0; index < prefabPaths.Length; index++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[index]);
                Transform slot = reactionWall.transform.Find($"ReactionTarget_{index:00}");
                if (prefab == null || slot == null)
                {
                    continue;
                }
                GameObject target = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                target.name = index switch
                {
                    0 => "CrackedBlock",
                    1 => "FragileFloor",
                    2 => "SoftSoil",
                    _ => "UnbreakableBlock",
                };
                target.transform.SetParent(slot, false);
                target.transform.localPosition = Vector3.zero;
            }

            CreateDamageTarget(
                reactionWall.transform.Find("ReactionTarget_04"),
                "EnemyDummy",
                ToolDamageTargetKind.Enemy,
                "Enemy",
                new Color(1f, 0.3f, 0.3f));
            CreateDamageTarget(
                reactionWall.transform.Find("ReactionTarget_05"),
                "BreakableContainerDummy",
                ToolDamageTargetKind.BreakableContainer,
                "DynamicObject",
                new Color(0.65f, 0.4f, 0.18f));
        }

        private static void CreateDamageTarget(
            Transform parent,
            string name,
            ToolDamageTargetKind kind,
            string layerName,
            Color color)
        {
            if (parent == null)
            {
                return;
            }
            GameObject target = Create(name, parent);
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0) target.layer = layer;
            target.AddComponent<BoxCollider2D>().size = Vector2.one * 0.8f;
            SpriteRenderer renderer = target.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            renderer.color = color;
            target.AddComponent<ToolDamageTarget>().ConfigureForTests(kind, 1);
        }

        private static UnityEngine.Object[] LoadStationAssets()
        {
            return new UnityEngine.Object[]
            {
                AssetDatabase.LoadAssetAtPath<BombDefinition>("Assets/_Game/Tools/Data/BombDefinition.asset"),
                AssetDatabase.LoadAssetAtPath<RopeDefinition>("Assets/_Game/Tools/Data/RopeDefinition.asset"),
                AssetDatabase.LoadAssetAtPath<HandToolDefinition>($"{ToolDataFolder}/TOOL_PICKAXE.asset"),
                AssetDatabase.LoadAssetAtPath<HandToolDefinition>($"{ToolDataFolder}/TOOL_SHOVEL.asset"),
                AssetDatabase.LoadAssetAtPath<HandToolDefinition>($"{ToolDataFolder}/TOOL_WATERING_CAN.asset"),
                AssetDatabase.LoadAssetAtPath<HandToolDefinition>($"{ToolDataFolder}/TOOL_POUNDER.asset"),
                AssetDatabase.LoadAssetAtPath<HandToolDefinition>($"{ToolDataFolder}/TOOL_HOOK_LAUNCHER.asset"),
                AssetDatabase.LoadAssetAtPath<HandToolDefinition>($"{ToolDataFolder}/TOOL_WIND_UMBRELLA.asset"),
            };
        }

        private static GameObject Create(string name, Transform parent)
        {
            var gameObject = new GameObject(name);
            if (parent != null) gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void CreateTilemap(string name, Transform parent, string layerName)
        {
            GameObject tilemap = Create(name, parent);
            tilemap.AddComponent<Tilemap>();
            tilemap.AddComponent<TilemapRenderer>();
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0) tilemap.layer = layer;
        }

        private static void CreateCollisionBox(Transform parent, string name, Vector2 localPosition, Vector2 size, string layerName)
        {
            GameObject box = Create(name, parent);
            box.transform.localPosition = localPosition;
            BoxCollider2D collider = box.AddComponent<BoxCollider2D>();
            collider.size = size;
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0) box.layer = layer;
        }

        private static void SetLayerIfDefined(GameObject target, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
            {
                target.layer = layer;
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void RemoveFromBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                {
                    scenes.Add(scene);
                }
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}

#endif
