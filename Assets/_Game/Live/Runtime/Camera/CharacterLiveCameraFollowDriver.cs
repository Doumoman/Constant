using UnityEngine;

namespace StarNight.Character.Live.Cameras
{
    /// <summary>
    /// RMAP02's local gameplay camera. It follows a Player transform smoothly,
    /// letterboxes/pillarboxes to a true 12x8 world viewport, then clamps the
    /// final center to the authored fixture bounds. It has no room-transition,
    /// map-generation, or save ownership.
    /// </summary>
    public sealed class CharacterLiveCameraFollowDriver : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Rect worldBounds = new Rect(0f, 0f, 60f, 40f);
        [SerializeField] private float viewportWidth = 12f;
        [SerializeField] private float viewportHeight = 8f;
        [SerializeField] private float followSeconds = 0.08f;
        [SerializeField] private CharacterLiveLookModeState lookModeState;

        private Vector2 currentLookOffset;

        public Vector2 VisibleWorldSize
        {
            get { return new Vector2(viewportWidth, viewportHeight); }
        }

        public Rect WorldBounds { get { return worldBounds; } }

        /// <summary>Current runtime-only offset after the 0.24/0.18s transition.</summary>
        public Vector2 CurrentLookOffset { get { return currentLookOffset; } }

        public void Configure(
            Camera camera,
            Transform target,
            Rect bounds,
            float widthInTiles,
            float heightInTiles,
            float seconds)
        {
            targetCamera = camera;
            followTarget = target;
            worldBounds = bounds;
            viewportWidth = widthInTiles;
            viewportHeight = heightInTiles;
            followSeconds = Mathf.Max(0f, seconds);
            ResolveLookModeState();
            UpdateViewport();
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (targetCamera == null || followTarget == null)
            {
                return;
            }

            targetCamera.transform.position = ClampPosition(followTarget.position);
            currentLookOffset = Vector2.zero;
        }

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            ResolveLookModeState();
            UpdateViewport();
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (targetCamera == null || followTarget == null)
            {
                return;
            }

            UpdateViewport();
            ResolveLookModeState();
            Vector2 targetLookOffset = lookModeState != null && lookModeState.IsLooking
                ? lookModeState.TargetOffset
                : Vector2.zero;
            float lookSeconds = lookModeState != null && lookModeState.IsLooking
                ? CharacterLiveLookModeState.EnterSeconds
                : CharacterLiveLookModeState.ReturnSeconds;
            float lookSpeed = CharacterLiveLookModeState.OffsetTiles /
                Mathf.Max(0.0001f, lookSeconds);
            currentLookOffset = Vector2.MoveTowards(currentLookOffset, targetLookOffset,
                lookSpeed * Time.unscaledDeltaTime);

            Vector3 desired = ClampPosition(followTarget.position +
                new Vector3(currentLookOffset.x, currentLookOffset.y, 0f));
            float factor = followSeconds <= 0f ? 1f : Mathf.Clamp01(
                Time.unscaledDeltaTime / followSeconds);
            targetCamera.transform.position = Vector3.Lerp(
                targetCamera.transform.position, desired, factor);
            targetCamera.transform.position = ClampPosition(targetCamera.transform.position);
        }

        private void ResolveLookModeState()
        {
            if (lookModeState == null && followTarget != null)
            {
                lookModeState = followTarget.GetComponent<CharacterLiveLookModeState>();
            }
        }

        private void UpdateViewport()
        {
            if (targetCamera == null || viewportWidth <= 0f || viewportHeight <= 0f)
            {
                return;
            }

            float targetAspect = viewportWidth / viewportHeight;
            float screenAspect = Screen.height <= 0 ? targetAspect :
                (float)Screen.width / Screen.height;
            if (screenAspect >= targetAspect)
            {
                // The screen is wider than 3:2: leave pillarbox bars at the
                // sides, keeping the camera pixel rect itself at 12:8.
                float width = targetAspect / screenAspect;
                targetCamera.rect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
            }
            else
            {
                // The screen is narrower than 3:2: leave letterbox bars at
                // the top and bottom, again preserving a 12:8 pixel rect.
                float height = screenAspect / targetAspect;
                targetCamera.rect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
            }

            targetCamera.orthographic = true;
            targetCamera.orthographicSize = viewportHeight * 0.5f;
        }

        private Vector3 ClampPosition(Vector3 position)
        {
            float halfWidth = viewportWidth * 0.5f;
            float halfHeight = viewportHeight * 0.5f;
            float minX = worldBounds.xMin + halfWidth;
            float maxX = worldBounds.xMax - halfWidth;
            float minY = worldBounds.yMin + halfHeight;
            float maxY = worldBounds.yMax - halfHeight;
            position.x = minX > maxX ? worldBounds.center.x : Mathf.Clamp(position.x, minX, maxX);
            position.y = minY > maxY ? worldBounds.center.y : Mathf.Clamp(position.y, minY, maxY);
            position.z = -10f;
            return position;
        }
    }
}
