#if LEGACY_DISABLED
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Interaction.State;
using StarNight.Tools.Rope;
using UnityEditor;
using UnityEngine;

namespace StarNight.ToolAuthoring
{
    public static class RopeAssetBuilder
    {
        public const string DefinitionPath = "Assets/_Game/Tools/Data/RopeDefinition.asset";
        public const string MaterialPath = "Assets/_Game/Tools/VisualProfiles/RopeLine.mat";
        public const string SegmentPath = "Assets/_Game/Tools/Prefabs/Rope_Segment.prefab";
        public const string CeilingAnchorPath = "Assets/_Game/Tools/Prefabs/Rope_Anchor.prefab";
        public const string StarKnotPath = "Assets/_Game/Tools/Prefabs/Rope_StarKnot.prefab";
        public const string InstallationPath = "Assets/_Game/Tools/Prefabs/Rope_Installation.prefab";
        private const string PlayerPrefabPath = "Assets/_Game/Player/Prefabs/Player.prefab";
        private const string PhysicsProfilePath = "Assets/_Game/Interaction/Data/ProjectPhysicsProfile.asset";

        [MenuItem("Tools/Star Night/Build TOOL-04 Rope Assets")]
        public static void Build()
        {
            RopeDefinition definition = AssetDatabase.LoadAssetAtPath<RopeDefinition>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<RopeDefinition>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            Material lineMaterial = BuildLineMaterial();
            BuildSegmentPrefab(lineMaterial);
            BuildAnchorPrefab(lineMaterial, false);
            BuildAnchorPrefab(lineMaterial, true);
            BuildInstallationPrefab();
            WirePlayerPrefab(definition);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("TOOL-04 rope assets and player wiring ready.");
        }

        private static Material BuildLineMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
            {
                return material;
            }
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(shader) { name = "RopeLine" };
            material.color = new Color(1f, 0.78f, 0.28f, 1f);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static void BuildSegmentPrefab(Material material)
        {
            var root = new GameObject("Rope_Segment");
            try
            {
                SetRopeLayer(root);
                BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
                trigger.size = new Vector2(0.28f, 0.96f);
                trigger.isTrigger = true;
                root.AddComponent<RopeSegmentRuntime>();
                LineRenderer line = ConfigureLine(root, material, 2, 0.11f);
                line.SetPosition(0, new Vector3(0f, -0.5f, 0f));
                line.SetPosition(1, new Vector3(0f, 0.5f, 0f));
                PrefabUtility.SaveAsPrefabAsset(root, SegmentPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildAnchorPrefab(Material material, bool starKnot)
        {
            var root = new GameObject(starKnot ? "Rope_StarKnot" : "Rope_Anchor");
            try
            {
                SetRopeLayer(root);
                CircleCollider2D trigger = root.AddComponent<CircleCollider2D>();
                trigger.radius = starKnot ? 0.38f : 0.28f;
                trigger.isTrigger = true;
                root.AddComponent<RopeAnchorRuntime>();
                if (starKnot)
                {
                    const int pointCount = 11;
                    LineRenderer line = ConfigureLine(root, material, pointCount, 0.09f);
                    for (int index = 0; index < pointCount; index++)
                    {
                        float angle = Mathf.PI * 0.5f + index * Mathf.PI * 4f / 5f;
                        float radius = index == pointCount - 1 ? 0.36f : (index % 2 == 0 ? 0.36f : 0.16f);
                        line.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, StarKnotPath);
                }
                else
                {
                    LineRenderer line = ConfigureLine(root, material, 3, 0.1f);
                    line.SetPosition(0, new Vector3(-0.3f, 0.18f, 0f));
                    line.SetPosition(1, new Vector3(0.3f, 0.18f, 0f));
                    line.SetPosition(2, new Vector3(0f, -0.28f, 0f));
                    PrefabUtility.SaveAsPrefabAsset(root, CeilingAnchorPath);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildInstallationPrefab()
        {
            var root = new GameObject("Rope_Installation");
            try
            {
                SetRopeLayer(root);
                root.AddComponent<RopeInstallationRuntime>();
                PrefabUtility.SaveAsPrefabAsset(root, InstallationPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void WirePlayerPrefab(RopeDefinition definition)
        {
            GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                PlayerActionLock actionLock = GetOrAdd<PlayerActionLock>(player);
                PlayerHandSlot handSlot = GetOrAdd<PlayerHandSlot>(player);
                Rigidbody2D body = player.GetComponent<Rigidbody2D>();
                RopeInventoryState inventory = GetOrAdd<RopeInventoryState>(player);
                RopeActionController action = GetOrAdd<RopeActionController>(player);
                RopeClimbController climb = GetOrAdd<RopeClimbController>(player);
                Transform head = player.transform.Find("RopeHeadCheck");
                if (head == null)
                {
                    var headObject = new GameObject("RopeHeadCheck");
                    head = headObject.transform;
                    head.SetParent(player.transform, false);
                    head.localPosition = new Vector3(0f, 0.55f, 0f);
                }

                ProjectPhysicsProfile physicsProfile =
                    AssetDatabase.LoadAssetAtPath<ProjectPhysicsProfile>(PhysicsProfilePath);
                RopeInstallationRuntime installationPrefab =
                    AssetDatabase.LoadAssetAtPath<RopeInstallationRuntime>(InstallationPath);
                GameObject segmentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SegmentPath);
                GameObject ceilingAnchorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CeilingAnchorPath);
                GameObject starKnotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StarKnotPath);
                inventory.Configure(definition);
                SetReference(action, "definition", definition);
                SetReference(action, "inventory", inventory);
                SetReference(action, "installationPrefab", installationPrefab);
                SetReference(action, "segmentPrefab", segmentPrefab);
                SetReference(action, "ceilingAnchorPrefab", ceilingAnchorPrefab);
                SetReference(action, "starKnotPrefab", starKnotPrefab);
                SetReference(action, "actionLock", actionLock);
                SetReference(action, "physicsProfile", physicsProfile);
                SetReference(action, "playerHead", head);
                climb.ConfigureForTests(definition, body, handSlot, actionLock);
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }

        private static LineRenderer ConfigureLine(GameObject target, Material material, int positions, float width)
        {
            LineRenderer line = target.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = positions;
            line.startWidth = width;
            line.endWidth = width;
            line.sharedMaterial = material;
            line.startColor = new Color(1f, 0.78f, 0.28f, 1f);
            line.endColor = new Color(1f, 0.95f, 0.62f, 1f);
            line.sortingOrder = 6;
            return line;
        }

        private static void SetRopeLayer(GameObject target)
        {
            int layer = LayerMask.NameToLayer("Rope");
            if (layer >= 0)
            {
                target.layer = layer;
            }
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}

#endif
