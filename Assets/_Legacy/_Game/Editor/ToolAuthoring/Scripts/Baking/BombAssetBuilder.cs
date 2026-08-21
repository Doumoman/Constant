#if LEGACY_DISABLED
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Interaction.State;
using StarNight.Interaction.Targeting;
using StarNight.Tools.Bomb;
using UnityEditor;
using UnityEngine;

namespace StarNight.ToolAuthoring
{
    public static class BombAssetBuilder
    {
        public const string DefinitionPath = "Assets/_Game/Tools/Data/BombDefinition.asset";
        public const string PrefabPath = "Assets/_Game/Tools/Prefabs/Bomb_Armed.prefab";
        public const string PlayerPrefabPath = "Assets/_Game/Player/Prefabs/Player.prefab";
        private const string PhysicsProfilePath = "Assets/_Game/Interaction/Data/ProjectPhysicsProfile.asset";

        [MenuItem("Tools/Star Night/Build TOOL-03 Bomb Assets")]
        public static void Build()
        {
            BombDefinition definition = AssetDatabase.LoadAssetAtPath<BombDefinition>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<BombDefinition>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            var root = new GameObject("Bomb_Armed");
            try
            {
                int dynamicLayer = LayerMask.NameToLayer("DynamicObject");
                if (dynamicLayer >= 0)
                {
                    root.layer = dynamicLayer;
                }

                Rigidbody2D body = root.AddComponent<Rigidbody2D>();
                body.mass = 0.6f;
                body.gravityScale = 2f;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
                collider.radius = 0.32f;
                BombExplosionDispatcher dispatcher = root.AddComponent<BombExplosionDispatcher>();
                ProjectPhysicsProfile physicsProfile =
                    AssetDatabase.LoadAssetAtPath<ProjectPhysicsProfile>(PhysicsProfilePath);
                dispatcher.ConfigureForTests(physicsProfile, Vector2.zero, definition.CellSize);
                BombRuntime runtime = root.AddComponent<BombRuntime>();
                runtime.ConfigureForTests(definition, body, dispatcher);
                InteractionCandidate candidate = root.AddComponent<InteractionCandidate>();
                candidate.ConfigureForTests(InteractionTargetKind.Pickup, 30003);

                var interactionTrigger = new GameObject("InteractionTrigger");
                interactionTrigger.transform.SetParent(root.transform, false);
                int interactionLayer = LayerMask.NameToLayer("Interaction");
                if (interactionLayer >= 0)
                {
                    interactionTrigger.layer = interactionLayer;
                }
                CircleCollider2D pickupCollider = interactionTrigger.AddComponent<CircleCollider2D>();
                pickupCollider.radius = 0.52f;
                pickupCollider.isTrigger = true;

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            WirePlayerPrefab(definition);

            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"TOOL-03 bomb assets ready: {DefinitionPath}, {PrefabPath}");
        }

        private static void WirePlayerPrefab(BombDefinition definition)
        {
            GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                ProjectPhysicsProfile physicsProfile =
                    AssetDatabase.LoadAssetAtPath<ProjectPhysicsProfile>(PhysicsProfilePath);
                BombRuntime bombPrefab = AssetDatabase.LoadAssetAtPath<BombRuntime>(PrefabPath);
                PlayerActionLock actionLock = GetOrAdd<PlayerActionLock>(player);
                PlayerActionRouter router = GetOrAdd<PlayerActionRouter>(player);
                InteractionProbe probe = GetOrAdd<InteractionProbe>(player);
                HandSlotPresenter presenter = GetOrAdd<HandSlotPresenter>(player);
                PlayerHandSlot handSlot = GetOrAdd<PlayerHandSlot>(player);
                HandSlotTransferService transfer = GetOrAdd<HandSlotTransferService>(player);
                BombInventoryState inventory = GetOrAdd<BombInventoryState>(player);
                BombActionController bombAction = GetOrAdd<BombActionController>(player);

                Transform carrySocket = player.transform.Find("CarrySocket");
                if (carrySocket == null)
                {
                    var socketObject = new GameObject("CarrySocket");
                    carrySocket = socketObject.transform;
                    carrySocket.SetParent(player.transform, false);
                    carrySocket.localPosition = new Vector3(0f, 0.72f, 0f);
                }

                Collider2D[] playerColliders = player.GetComponentsInChildren<Collider2D>(true);
                presenter.ConfigureForTests(carrySocket, playerColliders);
                handSlot.ConfigureForTests(presenter);
                probe.ConfigureForTests(
                    physicsProfile != null ? physicsProfile.InteractionMask : LayerMask.GetMask("Interaction"),
                    LayerMask.GetMask("TerrainSolid", "UnbreakableBoundary"),
                    true);
                transfer.ConfigureForTests(handSlot, probe, actionLock);
                inventory.Configure(definition);
                bombAction.ConfigureForTests(
                    definition,
                    bombPrefab,
                    inventory,
                    actionLock,
                    null,
                    player.transform);

                SetObjectReference(transfer, "physicsProfile", physicsProfile);
                SetObjectReference(bombAction, "interactionProbe", probe);
                SetObjectReferences(bombAction, "playerColliders", playerColliders);
                SetObjectReference(router, "actionExecutorComponent", transfer);
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetObjectReferences(Object target, string propertyName, Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                return;
            }

            property.arraySize = values != null ? values.Length : 0;
            for (int index = 0; index < property.arraySize; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

#endif
