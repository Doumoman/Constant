#if LEGACY_DISABLED
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Map
{
    /// <summary>
    /// Runtime half of the editor-only Map Element Lab. It intentionally builds a
    /// disposable preview instead of a baked prefab so MAP-E03 can test authoring data.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class MapElementLabTestRig : MonoBehaviour
    {
        [SerializeField] private MapElementDefinition activeDefinition;
        [SerializeField] private Transform activeAuthoringElement;
        [SerializeField] private MapElementLabPlayerRig playerRig;
        [SerializeField] private bool showRuntimeOverlay = true;
        [SerializeField] private bool rebuildOnEnable = true;
        [SerializeField] private MapElementState previewState = MapElementState.Idle;
        [SerializeField] private string lastSimulationResult = "대기 중";

        private static Texture2D previewTexture;
        private static Sprite previewSprite;

        private Rigidbody2D previewBody;
        private Vector3 authoredLocalPosition;
        private int pathSegmentIndex;
        private int pathDirection = 1;
        private float pathWaitRemaining;
        private MapElementDefinition lastBuiltDefinition;

        public MapElementDefinition ActiveDefinition => activeDefinition;
        public MapElementState PreviewState => previewState;
        public string LastSimulationResult => lastSimulationResult;
        public MapElementLabCollisionProbe CollisionProbe =>
            playerRig != null ? playerRig.CollisionProbe : null;

        private void OnEnable()
        {
            if (rebuildOnEnable && activeAuthoringElement != null && activeDefinition != null)
            {
                RebuildPreview();
            }
        }

        private void FixedUpdate()
        {
            if (!Application.isPlaying || previewState == MapElementState.Broken ||
                activeDefinition == null || activeDefinition.BehaviorProfile == null)
            {
                return;
            }

            var path = activeDefinition.BehaviorProfile.Path;
            if (path == null || path.Nodes == null || path.Nodes.Count < 2 || previewBody == null)
            {
                return;
            }

            if (pathWaitRemaining > 0f)
            {
                pathWaitRemaining = Mathf.Max(0f, pathWaitRemaining - Time.fixedDeltaTime);
                return;
            }

            var nextIndex = Mathf.Clamp(pathSegmentIndex + pathDirection, 0, path.Nodes.Count - 1);
            var parent = activeAuthoringElement.parent;
            var targetLocal = authoredLocalPosition + (Vector3)path.Nodes[nextIndex];
            var targetWorld = parent != null ? parent.TransformPoint(targetLocal) : targetLocal;
            var speed = Mathf.Max(0.01f, path.SpeedCellsPerSecond);
            var nextWorld = Vector2.MoveTowards(previewBody.position, targetWorld, speed * Time.fixedDeltaTime);
            previewBody.MovePosition(nextWorld);

            if (Vector2.SqrMagnitude(nextWorld - (Vector2)targetWorld) > 0.000001f)
            {
                return;
            }

            pathSegmentIndex = nextIndex;
            pathWaitRemaining = Mathf.Max(0f, path.WaitSeconds);
            if (path.PingPong && (pathSegmentIndex == 0 || pathSegmentIndex == path.Nodes.Count - 1))
            {
                pathDirection *= -1;
            }
            else if (!path.PingPong && pathSegmentIndex == path.Nodes.Count - 1)
            {
                pathSegmentIndex = path.ClosedLoop ? 0 : path.Nodes.Count - 2;
                pathDirection = path.ClosedLoop ? 1 : -1;
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || !showRuntimeOverlay)
            {
                return;
            }

            var probe = CollisionProbe;
            GUILayout.BeginArea(new Rect(18f, 18f, 410f, 150f), GUI.skin.box);
            GUILayout.Label("MAP-E03 · Map Element Lab Play Test");
            GUILayout.Label(activeDefinition != null
                ? $"Element: {activeDefinition.DisplayName} ({activeDefinition.ElementId})"
                : "Element: 미선택");
            GUILayout.Label($"State: {previewState} / {lastSimulationResult}");
            GUILayout.Label(probe != null
                ? $"Solid 충돌 {probe.SolidCollisionCount}회 · Trigger 충돌 {probe.TriggerCollisionCount}회 · {probe.LastContactName}"
                : "Player 충돌 Probe 없음");
            GUILayout.Label("A/D 또는 ←/→ 이동 · Space 점프 · SceneView/Workbench에서 요소 교체");
            GUILayout.EndArea();
        }

        public void Configure(
            MapElementDefinition definition,
            Transform authoringElement,
            MapElementLabPlayerRig mapTestPlayer)
        {
            activeDefinition = definition;
            activeAuthoringElement = authoringElement;
            playerRig = mapTestPlayer;
            authoredLocalPosition = activeAuthoringElement != null
                ? activeAuthoringElement.localPosition
                : Vector3.zero;
            RebuildPreview();
        }

        public void SetDefinition(MapElementDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            activeDefinition = definition;
            RebuildPreview();
        }

        public void RebuildPreview()
        {
            if (activeAuthoringElement == null || activeDefinition == null)
            {
                return;
            }

            authoredLocalPosition = activeAuthoringElement.localPosition;
            DestroyGeneratedChildren();
            ConfigurePreviewBody();

            var visualRoot = CreateGeneratedChild(activeAuthoringElement, "VisualRoot");
            var physicsRoot = CreateGeneratedChild(activeAuthoringElement, "PhysicsRoot");
            var triggerRoot = CreateGeneratedChild(activeAuthoringElement, "TriggerRoot");

            BuildVisual(visualRoot);
            BuildCollisionRoot(physicsRoot, activeDefinition.CollisionProfile.SolidShapes, false);
            BuildCollisionRoot(triggerRoot, activeDefinition.CollisionProfile.TriggerShapes, true);

            if (activeDefinition.CollisionProfile.SolidShapes.Count == 0 &&
                activeDefinition.CollisionProfile.IsSolid)
            {
                BuildOccupiedCellColliders(physicsRoot);
            }

            pathSegmentIndex = 0;
            pathDirection = activeDefinition.BehaviorProfile != null &&
                            activeDefinition.BehaviorProfile.Path != null &&
                            !activeDefinition.BehaviorProfile.Path.StartForward
                ? -1
                : 1;
            pathWaitRemaining = 0f;
            previewState = activeDefinition.BehaviorProfile != null
                ? activeDefinition.BehaviorProfile.InitialState
                : MapElementState.Idle;
            lastBuiltDefinition = activeDefinition;
            lastSimulationResult = "Preview 갱신 완료";
            ApplyPreviewState();

            if (Application.isPlaying && playerRig != null)
            {
                StartCollisionDemo();
            }
        }

        public void SetPreviewState(MapElementState state)
        {
            previewState = state;
            lastSimulationResult = $"{state} 강제 적용";
            ApplyPreviewState();
        }

        public void ResetToIdle()
        {
            previewState = MapElementState.Idle;
            lastSimulationResult = "Idle로 초기화";
            RebuildPreview();
        }

        public void SimulateToolReaction(ToolTag tool)
        {
            if (activeDefinition == null || activeDefinition.ToolReactions == null)
            {
                lastSimulationResult = $"{tool}: 반응 없음";
                return;
            }

            var entries = activeDefinition.ToolReactions.Entries;
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (entry == null || (entry.Tool & tool) == 0)
                {
                    continue;
                }

                if (entry.Reaction == ElementReactionType.Break)
                {
                    previewState = MapElementState.Broken;
                }
                else if (entry.Reaction == ElementReactionType.Disable)
                {
                    previewState = MapElementState.Disabled;
                }
                else if (entry.Reaction == ElementReactionType.SetState &&
                         System.Enum.TryParse(entry.ResultState, true, out MapElementState parsedState))
                {
                    previewState = parsedState;
                }

                lastSimulationResult = $"{tool}: {entry.Reaction}";
                ApplyPreviewState();
                return;
            }

            lastSimulationResult = $"{tool}: 등록된 반응 없음";
        }

        public void SimulateHeavyObjectCollision()
        {
            lastSimulationResult = "HeavyObject 충돌 시뮬레이션 완료";
            StartCollisionDemo();
        }

        public void SimulateMaruCollision()
        {
            lastSimulationResult = activeDefinition != null && activeDefinition.MaruReaction.IsTarget
                ? $"Maru: {activeDefinition.MaruReaction.Reaction}"
                : "Maru: 등록된 반응 없음";
        }

        public void RunRepeatedSimulation(int iterations)
        {
            iterations = Mathf.Max(1, iterations);
            var originalState = previewState;
            for (var index = 0; index < iterations; index++)
            {
                previewState = MapElementState.Warning;
                previewState = MapElementState.Active;
                previewState = MapElementState.Cooldown;
                previewState = MapElementState.Idle;
            }

            previewState = originalState;
            lastSimulationResult = $"{iterations}회 상태 반복 완료";
            ApplyPreviewState();
        }

        public void StartCollisionDemo()
        {
            if (playerRig == null || activeAuthoringElement == null || activeDefinition == null)
            {
                return;
            }

            var bounds = activeDefinition.Footprint != null
                ? activeDefinition.Footprint.BoundsSize
                : Vector2Int.one;
            var centerX = (bounds.x - 1) * 0.5f;
            if (activeDefinition.Category == ElementCategory.Platform)
            {
                playerRig.BeginDemo(
                    activeAuthoringElement.TransformPoint(new Vector3(centerX, 1.65f, 0f)),
                    new Vector2(0f, -3f),
                    false);
            }
            else
            {
                playerRig.BeginDemo(
                    activeAuthoringElement.TransformPoint(new Vector3(-2.5f, 0.15f, 0f)),
                    new Vector2(3.25f, 0f),
                    true);
            }
        }

        private void ConfigurePreviewBody()
        {
            previewBody = activeAuthoringElement.GetComponent<Rigidbody2D>();
            if (previewBody == null)
            {
                previewBody = activeAuthoringElement.gameObject.AddComponent<Rigidbody2D>();
                if (!Application.isPlaying)
                {
                    previewBody.hideFlags = HideFlags.DontSaveInEditor;
                }
            }

            previewBody.bodyType = RigidbodyType2D.Kinematic;
            previewBody.gravityScale = 0f;
            previewBody.freezeRotation = true;
            previewBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            previewBody.linearVelocity = Vector2.zero;
            previewBody.angularVelocity = 0f;
        }

        private void BuildVisual(Transform visualRoot)
        {
            var visual = activeDefinition.VisualProfile;
            var previewObject = CreateGeneratedChild(visualRoot, "VisualPreview");
            var renderer = previewObject.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = visual.SingleSprite != null ? visual.SingleSprite : GetPreviewSprite();
            renderer.color = visual.SingleSprite != null ? visual.Tint : GetCategoryColor(activeDefinition.Category);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(
                Mathf.Max(0.05f, visual.VisualSizeCells.x),
                Mathf.Max(0.05f, visual.VisualSizeCells.y));
            renderer.flipX = visual.FlipX;
            renderer.flipY = visual.FlipY;
            renderer.sortingOrder = visual.SortingOrder;
            if (!string.IsNullOrWhiteSpace(visual.SortingLayerName))
            {
                renderer.sortingLayerName = visual.SortingLayerName;
            }

            if (visual.MaterialOverride != null)
            {
                renderer.sharedMaterial = visual.MaterialOverride;
            }

            previewObject.localPosition = visual.VisualOffsetCells;
            previewObject.localScale = Vector3.one;
        }

        private void BuildCollisionRoot(
            Transform root,
            IReadOnlyList<SerializedColliderShape> shapes,
            bool isTrigger)
        {
            if (shapes == null)
            {
                return;
            }

            for (var index = 0; index < shapes.Count; index++)
            {
                var shape = shapes[index];
                if (shape == null)
                {
                    continue;
                }

                var shapeObject = CreateGeneratedChild(root, $"{(isTrigger ? "Trigger" : "Solid")}_{index:00}");
                switch (shape.ShapeType)
                {
                    case SerializedColliderShapeType.Capsule:
                    {
                        var collider = shapeObject.gameObject.AddComponent<CapsuleCollider2D>();
                        collider.offset = shape.OffsetCells;
                        collider.size = MaxSize(shape.SizeCells);
                        collider.direction = collider.size.x >= collider.size.y
                            ? CapsuleDirection2D.Horizontal
                            : CapsuleDirection2D.Vertical;
                        collider.isTrigger = isTrigger;
                        break;
                    }
                    case SerializedColliderShapeType.Polygon when shape.Points != null && shape.Points.Count >= 3:
                    {
                        var collider = shapeObject.gameObject.AddComponent<PolygonCollider2D>();
                        var points = new Vector2[shape.Points.Count];
                        for (var pointIndex = 0; pointIndex < points.Length; pointIndex++)
                        {
                            points[pointIndex] = shape.Points[pointIndex] + shape.OffsetCells;
                        }

                        collider.points = points;
                        collider.isTrigger = isTrigger;
                        break;
                    }
                    default:
                    {
                        var collider = shapeObject.gameObject.AddComponent<BoxCollider2D>();
                        collider.offset = shape.OffsetCells;
                        collider.size = MaxSize(shape.SizeCells);
                        collider.isTrigger = isTrigger;
                        break;
                    }
                }
            }
        }

        private void BuildOccupiedCellColliders(Transform root)
        {
            var footprint = activeDefinition.Footprint;
            if (footprint == null || footprint.OccupiedCells == null)
            {
                return;
            }

            for (var index = 0; index < footprint.OccupiedCells.Count; index++)
            {
                var localCell = footprint.OccupiedCells[index] - footprint.PivotCell;
                var cellObject = CreateGeneratedChild(root, $"Solid_Cell_{index:00}");
                var collider = cellObject.gameObject.AddComponent<BoxCollider2D>();
                collider.offset = localCell;
                collider.size = new Vector2(0.98f, 0.98f);
            }
        }

        private void ApplyPreviewState()
        {
            if (activeAuthoringElement == null)
            {
                return;
            }

            var disabled = previewState == MapElementState.Broken ||
                           previewState == MapElementState.Disabled;
            var colliders = activeAuthoringElement.GetComponentsInChildren<Collider2D>(true);
            for (var index = 0; index < colliders.Length; index++)
            {
                colliders[index].enabled = !disabled;
            }

            var renderers = activeAuthoringElement.GetComponentsInChildren<SpriteRenderer>(true);
            for (var index = 0; index < renderers.Length; index++)
            {
                var color = renderers[index].color;
                color.a = disabled ? 0.28f : 0.9f;
                renderers[index].color = color;
            }
        }

        private void DestroyGeneratedChildren()
        {
            for (var index = activeAuthoringElement.childCount - 1; index >= 0; index--)
            {
                var child = activeAuthoringElement.GetChild(index).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private static Transform CreateGeneratedChild(Transform parent, string childName)
        {
            var child = new GameObject(childName).transform;
            child.SetParent(parent, false);
            child.localScale = Vector3.one;
            if (!Application.isPlaying)
            {
                child.gameObject.hideFlags = HideFlags.DontSaveInEditor;
            }

            return child;
        }

        private static Vector2 MaxSize(Vector2 size)
        {
            return new Vector2(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));
        }

        private static Color GetCategoryColor(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.Hazard:
                    return new Color(0.95f, 0.25f, 0.3f, 0.9f);
                case ElementCategory.Platform:
                    return new Color(0.2f, 0.75f, 0.95f, 0.9f);
                case ElementCategory.Trigger:
                case ElementCategory.Control:
                    return new Color(0.95f, 0.75f, 0.2f, 0.9f);
                default:
                    return new Color(0.55f, 0.75f, 0.9f, 0.9f);
            }
        }

        private static Sprite GetPreviewSprite()
        {
            if (previewSprite != null)
            {
                return previewSprite;
            }

            previewTexture = new Texture2D(16, 16, TextureFormat.RGBA32, false)
            {
                name = "MAP-E03 Preview Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var pixels = new Color[16 * 16];
            for (var index = 0; index < pixels.Length; index++)
            {
                pixels[index] = Color.white;
            }

            previewTexture.SetPixels(pixels);
            previewTexture.Apply(false, true);
            previewSprite = Sprite.Create(
                previewTexture,
                new Rect(0f, 0f, 16f, 16f),
                new Vector2(0.5f, 0.5f),
                16f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(1f, 1f, 1f, 1f));
            previewSprite.name = "MAP-E03 Preview Sprite";
            previewSprite.hideFlags = HideFlags.HideAndDontSave;
            return previewSprite;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(MapElementLabCollisionProbe))]
    public sealed class MapElementLabPlayerRig : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float jumpSpeed = 7f;
        [SerializeField] private bool acceptManualInput = true;

        private Rigidbody2D body;
        private bool autoDrive;

        public MapElementLabCollisionProbe CollisionProbe { get; private set; }

        private void Awake()
        {
            CacheComponents();
        }

        private void FixedUpdate()
        {
            CacheComponents();
            if (autoDrive)
            {
                if (CollisionProbe != null &&
                    (CollisionProbe.SolidCollisionCount > 0 || CollisionProbe.TriggerCollisionCount > 0))
                {
                    autoDrive = false;
                    body.linearVelocity = Vector2.zero;
                }
                return;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (!acceptManualInput)
            {
                return;
            }

            var horizontal = Input.GetAxisRaw("Horizontal");
            body.linearVelocity = new Vector2(horizontal * moveSpeed, body.linearVelocity.y);
            if (Input.GetKey(KeyCode.Space) && Mathf.Abs(body.linearVelocity.y) < 0.05f)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, jumpSpeed);
            }
#endif
        }

        public void BeginDemo(Vector3 worldPosition, Vector2 velocity, bool driveUntilContact)
        {
            CacheComponents();
            transform.position = worldPosition;
            body.position = worldPosition;
            body.linearVelocity = velocity;
            body.angularVelocity = 0f;
            CollisionProbe.ResetCounts();
            autoDrive = driveUntilContact;
        }

        private void CacheComponents()
        {
            body = body != null ? body : GetComponent<Rigidbody2D>();
            CollisionProbe = CollisionProbe != null
                ? CollisionProbe
                : GetComponent<MapElementLabCollisionProbe>();
            if (CollisionProbe == null)
            {
                CollisionProbe = gameObject.AddComponent<MapElementLabCollisionProbe>();
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class MapElementLabCollisionProbe : MonoBehaviour
    {
        public int SolidCollisionCount { get; private set; }
        public int TriggerCollisionCount { get; private set; }
        public string LastContactName { get; private set; } = "접촉 없음";

        private void OnCollisionEnter2D(Collision2D collision)
        {
            SolidCollisionCount++;
            LastContactName = collision.collider != null ? collision.collider.name : "Solid";
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TriggerCollisionCount++;
            LastContactName = other != null ? other.name : "Trigger";
        }

        public void ResetCounts()
        {
            SolidCollisionCount = 0;
            TriggerCollisionCount = 0;
            LastContactName = "접촉 없음";
        }
    }
}

#endif
