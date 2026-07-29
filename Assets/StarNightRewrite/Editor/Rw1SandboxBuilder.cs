using System.Linq;
using StarNight.Rewrite.Core;
using StarNight.Rewrite.Player;
using StarNight.Rewrite.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.Rewrite.Editor
{
    public static class Rw1SandboxBuilder
    {
        private const string ScenePath =
            "Assets/Scenes/StarNightRewrite/RW_CH1_Bootstrap.unity";
        private const string SquarePath =
            "Assets/Resources/Sprites/Square.png";
        private const string Forest =
            "Assets/2D Fantasy sprite bundle/Forest  V2.0/Prefabs/";

        [MenuItem("Star Night Rewrite/RW1/Rebuild Movement Sandbox")]
        public static void Rebuild()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            Sprite square = AssetDatabase.LoadAllAssetsAtPath(SquarePath)
                .OfType<Sprite>()
                .FirstOrDefault();
            if (square == null)
            {
                throw new MissingReferenceException(
                    $"RW1 square sprite not found at {SquarePath}.");
            }

            GameObject previous = GameObject.Find("RW1 Sandbox");
            if (previous != null)
            {
                Object.DestroyImmediate(previous);
            }

            SetupSceneServices();

            GameObject sandbox = new GameObject("RW1 Sandbox");
            GameObject environment = new GameObject("Environment");
            environment.transform.SetParent(sandbox.transform, false);
            GameObject interactions = new GameObject("Interactions");
            interactions.transform.SetParent(sandbox.transform, false);

            CreateBackdrop(environment.transform, square);
            CreateFantasyDecorations(environment.transform);
            CreateMovementCourse(environment.transform, square);

            GameObject player = CreatePlayer(sandbox.transform, square);
            CreateSafeAnchor(interactions.transform, square, new Vector2(-6f, -1.4f));
            CreateConsumablePickup(
                interactions.transform,
                square,
                "Rope Pickup",
                new Vector2(-3.7f, -1.55f),
                ConsumablePickup.ConsumableKind.Rope,
                new Color(0.35f, 0.92f, 1f));
            CreateConsumablePickup(
                interactions.transform,
                square,
                "Bomb Pickup",
                new Vector2(1.2f, -1.5f),
                ConsumablePickup.ConsumableKind.Bomb,
                new Color(1f, 0.42f, 0.38f));
            CreateToolPickup(
                interactions.transform,
                square,
                "Pickaxe Pickup",
                new Vector2(-1.8f, 0.75f),
                HandToolId.Pickaxe,
                new Color(0.76f, 0.84f, 0.94f));
            CreateToolPickup(
                interactions.transform,
                square,
                "Umbrella Pickup",
                new Vector2(4.6f, 0.85f),
                HandToolId.Umbrella,
                new Color(0.45f, 0.78f, 1f));
            CreateCarryable(
                interactions.transform,
                square,
                new Vector2(-4.8f, -1.55f));
            CreateHazard(
                interactions.transform,
                square,
                new Vector2(6.4f, -1.55f));
            CreateHealingPickup(
                interactions.transform,
                square,
                new Vector2(5.3f, 1.15f));

            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(
                "<b><color=#76D7FF>STAR NIGHT REWRITE</color></b>: " +
                "RW1 movement sandbox rebuilt.");
        }

        private static void SetupSceneServices()
        {
            GameObject sceneRoot = GameObject.Find("__RewriteScene");
            if (sceneRoot == null)
            {
                sceneRoot = new GameObject("__RewriteScene");
                sceneRoot.AddComponent<RewriteSceneRoot>();
            }

            GetOrAdd<RunContext>(sceneRoot);
            GetOrAdd<Rw1HudPresenter>(sceneRoot);

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.orthographic = true;
                camera.orthographicSize = 5.6f;
                camera.backgroundColor = new Color(0.018f, 0.025f, 0.075f, 1f);
                GetOrAdd<SideScrollCamera2D>(camera.gameObject);
            }
        }

        private static GameObject CreatePlayer(Transform parent, Sprite square)
        {
            GameObject player = new GameObject("Player · 별을 줍는 아이");
            player.transform.SetParent(parent, false);
            player.transform.position = new Vector3(-6f, -1.35f, 0f);
            player.layer = RequireLayer("Player");

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.mass = 1f;
            body.gravityScale = 3.2f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CapsuleCollider2D capsule = player.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.72f, 1.1f);
            capsule.direction = CapsuleDirection2D.Vertical;

            player.AddComponent<PlayerInputReader>();
            player.AddComponent<PlayerMotor2D>();
            player.AddComponent<SafeAnchorService>();
            player.AddComponent<RaniLampController>();
            player.AddComponent<PlayerHealth>();
            player.AddComponent<PlayerFallRecovery>();
            player.AddComponent<PlayerCarry>();
            player.AddComponent<PlayerInteractor>();
            player.AddComponent<ConsumableInventory>();
            player.AddComponent<PlayerToolController>();

            CreateVisual(
                player.transform,
                square,
                "Body",
                Vector2.zero,
                new Vector2(0.7f, 1.1f),
                new Color(1f, 0.76f, 0.28f),
                30);
            CreateVisual(
                player.transform,
                square,
                "Face Glow",
                new Vector2(0f, 0.18f),
                new Vector2(0.3f, 0.27f),
                new Color(1f, 0.94f, 0.72f),
                31);
            CreateVisual(
                player.transform,
                square,
                "Star Scarf",
                new Vector2(-0.33f, -0.03f),
                new Vector2(0.58f, 0.1f),
                new Color(0.98f, 0.24f, 0.5f),
                32);
            return player;
        }

        private static void CreateMovementCourse(Transform parent, Sprite square)
        {
            CreateGround(parent, square, "Left Ground", new Vector2(-5f, -2.75f),
                new Vector2(8f, 1.35f));
            CreateGround(parent, square, "Right Ground", new Vector2(4f, -2.75f),
                new Vector2(8f, 1.35f));
            CreateGround(parent, square, "Step A", new Vector2(-1.9f, -0.15f),
                new Vector2(2.8f, 0.38f));
            CreateGround(parent, square, "Step B", new Vector2(1.6f, 1.15f),
                new Vector2(2.4f, 0.38f));
            CreateGround(parent, square, "Step C", new Vector2(4.9f, 0.05f),
                new Vector2(2.8f, 0.38f));
        }

        private static void CreateGround(
            Transform parent,
            Sprite sprite,
            string name,
            Vector2 position,
            Vector2 size)
        {
            GameObject ground = new GameObject(name);
            ground.transform.SetParent(parent, false);
            ground.transform.position = position;
            ground.transform.localScale = new Vector3(size.x, size.y, 1f);
            ground.layer = RequireLayer("Ground");

            SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.18f, 0.29f, 0.48f, 1f);
            renderer.sortingLayerName = "Ground";
            renderer.sortingOrder = 2;
            ground.AddComponent<BoxCollider2D>();
        }

        private static void CreateBackdrop(Transform parent, Sprite square)
        {
            CreateVisual(
                parent,
                square,
                "Night Gradient",
                new Vector2(0f, 1.5f),
                new Vector2(30f, 13f),
                new Color(0.025f, 0.04f, 0.12f),
                -100,
                "Background");

            Vector2[] stars =
            {
                new Vector2(-8f, 3.7f), new Vector2(-6.2f, 2.4f),
                new Vector2(-4.5f, 4.5f), new Vector2(-2.2f, 3.2f),
                new Vector2(0.2f, 4.1f), new Vector2(2.5f, 2.8f),
                new Vector2(4.8f, 4.4f), new Vector2(7.1f, 3.3f),
                new Vector2(8.4f, 1.9f)
            };

            for (int index = 0; index < stars.Length; index++)
            {
                float size = index % 3 == 0 ? 0.13f : 0.08f;
                CreateVisual(
                    parent,
                    square,
                    $"Star {index + 1:00}",
                    stars[index],
                    new Vector2(size, size),
                    index % 2 == 0
                        ? new Color(1f, 0.9f, 0.55f)
                        : new Color(0.65f, 0.88f, 1f),
                    -80,
                    "Background");
            }
        }

        private static void CreateFantasyDecorations(Transform parent)
        {
            GameObject artRoot = new GameObject("2D Fantasy Bundle Art");
            artRoot.transform.SetParent(parent, false);
            InstantiateDecoration(
                Forest + "Mounts and sky.prefab",
                artRoot.transform,
                new Vector3(0f, -2.2f, 4f),
                0.75f);
            InstantiateDecoration(
                Forest + "Big Tree A.prefab",
                artRoot.transform,
                new Vector3(-7.2f, -2.1f, 2f),
                0.38f);
            InstantiateDecoration(
                Forest + "Big Tree B.prefab",
                artRoot.transform,
                new Vector3(7.1f, -2.1f, 2f),
                0.38f);
            InstantiateDecoration(
                Forest + "Bushes A.prefab",
                artRoot.transform,
                new Vector3(0f, -2.05f, 1f),
                0.35f);
            InstantiateDecoration(
                Forest + "Fireflys.prefab",
                artRoot.transform,
                new Vector3(0f, 0.8f, 0f),
                0.75f);
        }

        private static void InstantiateDecoration(
            string path,
            Transform parent,
            Vector3 position,
            float scale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"RW1 decoration missing: {path}");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = prefab.name;
            instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            instance.transform.localScale = Vector3.one * scale;

            foreach (Collider2D collider in
                instance.GetComponentsInChildren<Collider2D>(true))
            {
                collider.enabled = false;
            }

            foreach (SpriteRenderer renderer in
                instance.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.sortingLayerName = "Background";
                renderer.sortingOrder = Mathf.Min(renderer.sortingOrder, -20);
            }
        }

        private static void CreateSafeAnchor(
            Transform parent,
            Sprite square,
            Vector2 position)
        {
            GameObject anchor = new GameObject("Safe Anchor · 시작점");
            anchor.transform.SetParent(parent, false);
            anchor.transform.position = position;
            CircleCollider2D trigger = anchor.AddComponent<CircleCollider2D>();
            trigger.radius = 0.65f;
            trigger.isTrigger = true;
            anchor.AddComponent<SafeAnchor>();
            CreateVisual(
                anchor.transform,
                square,
                "Anchor Glow",
                Vector2.zero,
                new Vector2(0.25f, 0.08f),
                new Color(0.3f, 1f, 0.72f, 0.85f),
                10);
        }

        private static void CreateConsumablePickup(
            Transform parent,
            Sprite square,
            string name,
            Vector2 position,
            ConsumablePickup.ConsumableKind kind,
            Color color)
        {
            GameObject pickup = CreateTriggerObject(parent, name, position);
            ConsumablePickup component = pickup.AddComponent<ConsumablePickup>();
            SerializedObject serialized = new SerializedObject(component);
            serialized.FindProperty("kind").enumValueIndex = (int)kind;
            serialized.FindProperty("amount").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            CreateVisual(
                pickup.transform,
                square,
                "Pickup Glow",
                Vector2.zero,
                new Vector2(0.38f, 0.38f),
                color,
                15);
        }

        private static void CreateToolPickup(
            Transform parent,
            Sprite square,
            string name,
            Vector2 position,
            HandToolId tool,
            Color color)
        {
            GameObject pickup = CreateTriggerObject(parent, name, position);
            HandToolPickup component = pickup.AddComponent<HandToolPickup>();
            SerializedObject serialized = new SerializedObject(component);
            serialized.FindProperty("tool").enumValueIndex = (int)tool;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            CreateVisual(
                pickup.transform,
                square,
                "Tool Glow",
                Vector2.zero,
                new Vector2(0.48f, 0.22f),
                color,
                16);
        }

        private static void CreateCarryable(
            Transform parent,
            Sprite square,
            Vector2 position)
        {
            GameObject crate = new GameObject("Carryable Moon Crate");
            crate.transform.SetParent(parent, false);
            crate.transform.position = position;
            crate.layer = RequireLayer("Holdable");

            Rigidbody2D body = crate.AddComponent<Rigidbody2D>();
            body.mass = 1f;
            body.gravityScale = 3.2f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            BoxCollider2D collider = crate.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.72f, 0.72f);
            crate.AddComponent<Carryable2D>();
            CreateVisual(
                crate.transform,
                square,
                "Crate Visual",
                Vector2.zero,
                new Vector2(0.7f, 0.7f),
                new Color(0.62f, 0.4f, 0.25f),
                14);
        }

        private static void CreateHazard(
            Transform parent,
            Sprite square,
            Vector2 position)
        {
            GameObject hazard = new GameObject("Damage Test Thorns");
            hazard.transform.SetParent(parent, false);
            hazard.transform.position = position;
            BoxCollider2D trigger = hazard.AddComponent<BoxCollider2D>();
            trigger.size = new Vector2(1.25f, 0.55f);
            trigger.isTrigger = true;
            hazard.AddComponent<DamageHazard2D>();
            CreateVisual(
                hazard.transform,
                square,
                "Thorn Glow",
                Vector2.zero,
                new Vector2(1.2f, 0.5f),
                new Color(0.92f, 0.18f, 0.3f, 0.9f),
                12);
        }

        private static void CreateHealingPickup(
            Transform parent,
            Sprite square,
            Vector2 position)
        {
            GameObject pickup = CreateTriggerObject(
                parent,
                "Healing Moon Rice Cake",
                position);
            pickup.AddComponent<HealingPickup>();
            CreateVisual(
                pickup.transform,
                square,
                "Rice Cake Glow",
                Vector2.zero,
                new Vector2(0.42f, 0.28f),
                new Color(1f, 0.9f, 0.72f),
                15);
        }

        private static GameObject CreateTriggerObject(
            Transform parent,
            string name,
            Vector2 position)
        {
            GameObject result = new GameObject(name);
            result.transform.SetParent(parent, false);
            result.transform.position = position;
            CircleCollider2D trigger = result.AddComponent<CircleCollider2D>();
            trigger.radius = 0.6f;
            trigger.isTrigger = true;
            return result;
        }

        private static SpriteRenderer CreateVisual(
            Transform parent,
            Sprite sprite,
            string name,
            Vector2 localPosition,
            Vector2 localScale,
            Color color,
            int sortingOrder,
            string sortingLayer = "Objects")
        {
            GameObject visual = new GameObject(name);
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale =
                new Vector3(localScale.x, localScale.y, 1f);

            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static int RequireLayer(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                throw new MissingReferenceException(
                    $"Required Unity layer is missing: {layerName}");
            }

            return layer;
        }

        private static T GetOrAdd<T>(GameObject owner) where T : Component
        {
            T component = owner.GetComponent<T>();
            return component != null ? component : owner.AddComponent<T>();
        }
    }
}
