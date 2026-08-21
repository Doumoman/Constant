#if LEGACY_DISABLED
using StarNight.Player;
using UnityEngine;

namespace StarNight.World
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class RoomFocusCamera2D : MonoBehaviour
    {
        public const float DefaultMargin = 0.5f;
        public const float DefaultTransitionTime = 0.28f;
        private const float FallbackAspect = 16f / 9f;

        [SerializeField] private Camera cameraComponent;
        [SerializeField] private Transform target;
        [SerializeField] private GridBoundedCamera2D fallbackCamera;
        [SerializeField] private PlayerRecovery recovery;
        [SerializeField] private float margin = DefaultMargin;
        [SerializeField] private float transitionTime = DefaultTransitionTime;
        [SerializeField] private float maximumPanSpeed = 60f;
        [SerializeField] private float minimumOrthographicSize = 3.5f;
        [SerializeField] private float maximumOrthographicSize = 7f;
        [SerializeField] private float baseOrthographicSize = 7f;

        private Vector3 panVelocity;
        private float zoomVelocity;
        private RoomBounds2D currentRoom;

        public string CurrentRoomId =>
            currentRoom != null ? currentRoom.RoomId : string.Empty;
        public bool IsFraming => currentRoom != null;
        public float TargetOrthographicSize { get; private set; }

        public void Configure(
            Camera targetCamera,
            Transform followTarget,
            GridBoundedCamera2D fallback,
            PlayerRecovery playerRecovery)
        {
            Unsubscribe();
            cameraComponent = targetCamera;
            target = followTarget;
            fallbackCamera = fallback;
            recovery = playerRecovery;
            if (!IsFraming && cameraComponent != null && cameraComponent.orthographic)
            {
                baseOrthographicSize = cameraComponent.orthographicSize;
            }

            Subscribe();
            SnapNow();
        }

        private void Awake()
        {
            if (cameraComponent == null)
            {
                cameraComponent = GetComponent<Camera>();
            }

            if (fallbackCamera == null)
            {
                fallbackCamera = GetComponent<GridBoundedCamera2D>();
            }

            if (cameraComponent != null && cameraComponent.orthographic)
            {
                baseOrthographicSize = cameraComponent.orthographicSize;
            }

            TargetOrthographicSize = baseOrthographicSize;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ReleaseOwnership();
        }

        private void LateUpdate()
        {
            if (cameraComponent == null || target == null)
            {
                ReleaseOwnership();
                return;
            }

            RoomBounds2D room = RoomBounds2D.FindContaining(target.position);
            if (room == null)
            {
                ReleaseOwnership();
                TargetOrthographicSize = baseOrthographicSize;
                ApplyZoom(baseOrthographicSize, Time.unscaledDeltaTime);
                return;
            }

            TakeOwnership(room);
            float size = ResolveOrthographicSize(room.WorldRect);
            TargetOrthographicSize = size;
            Vector2 framed = CalculateFramedPosition(
                room.WorldRect,
                target.position,
                size,
                ResolveAspect());
            float delta = Time.unscaledDeltaTime;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                new Vector3(framed.x, framed.y, transform.position.z),
                ref panVelocity,
                transitionTime,
                maximumPanSpeed,
                delta);
            ApplyZoom(size, delta);
        }

        public void SnapNow()
        {
            if (cameraComponent == null || target == null)
            {
                return;
            }

            panVelocity = Vector3.zero;
            zoomVelocity = 0f;

            RoomBounds2D room = RoomBounds2D.FindContaining(target.position);
            if (room == null)
            {
                ReleaseOwnership();
                TargetOrthographicSize = baseOrthographicSize;
                SetOrthographicSize(baseOrthographicSize);
                return;
            }

            TakeOwnership(room);
            float size = ResolveOrthographicSize(room.WorldRect);
            TargetOrthographicSize = size;
            SetOrthographicSize(size);
            Vector2 framed = CalculateFramedPosition(
                room.WorldRect,
                target.position,
                size,
                ResolveAspect());
            transform.position = new Vector3(
                framed.x,
                framed.y,
                transform.position.z);
        }

        public static float CalculateOrthographicSize(
            Rect room,
            float aspect,
            float margin)
        {
            float safeAspect = aspect > 0.0001f ? aspect : FallbackAspect;
            float verticalHalf = Mathf.Abs(room.height) * 0.5f + margin;
            float horizontalHalf =
                (Mathf.Abs(room.width) * 0.5f + margin) / safeAspect;
            return Mathf.Max(verticalHalf, horizontalHalf);
        }

        public static Vector2 CalculateFramedPosition(
            Rect room,
            Vector2 target,
            float orthographicSize,
            float aspect)
        {
            float safeAspect = aspect > 0.0001f ? aspect : FallbackAspect;
            float halfHeight = orthographicSize;
            float halfWidth = orthographicSize * safeAspect;
            float x = AxisPosition(
                target.x,
                room.xMin,
                room.xMax,
                halfWidth,
                room.center.x);
            float y = AxisPosition(
                target.y,
                room.yMin,
                room.yMax,
                halfHeight,
                room.center.y);
            return new Vector2(x, y);
        }

        private float ResolveOrthographicSize(Rect room)
        {
            float ideal = CalculateOrthographicSize(
                room,
                ResolveAspect(),
                margin);
            float upper = Mathf.Max(minimumOrthographicSize, maximumOrthographicSize);
            return Mathf.Clamp(ideal, minimumOrthographicSize, upper);
        }

        private float ResolveAspect()
        {
            return cameraComponent != null && cameraComponent.aspect > 0.0001f
                ? cameraComponent.aspect
                : FallbackAspect;
        }

        private void ApplyZoom(float desiredSize, float deltaTime)
        {
            if (cameraComponent == null || !cameraComponent.orthographic)
            {
                return;
            }

            cameraComponent.orthographicSize = Mathf.SmoothDamp(
                cameraComponent.orthographicSize,
                desiredSize,
                ref zoomVelocity,
                transitionTime,
                Mathf.Infinity,
                deltaTime);
        }

        private void SetOrthographicSize(float size)
        {
            if (cameraComponent != null && cameraComponent.orthographic)
            {
                cameraComponent.orthographicSize = size;
            }
        }

        private void TakeOwnership(RoomBounds2D room)
        {
            currentRoom = room;
            if (fallbackCamera != null && fallbackCamera.enabled)
            {
                fallbackCamera.enabled = false;
            }
        }

        private void ReleaseOwnership()
        {
            currentRoom = null;
            if (fallbackCamera != null && !fallbackCamera.enabled)
            {
                fallbackCamera.enabled = true;
            }
        }

        private void HandleRecovered(RecoveryReason reason, Vector2 position)
        {
            SnapNow();
        }

        private void Subscribe()
        {
            if (recovery != null)
            {
                recovery.Recovered -= HandleRecovered;
                recovery.Recovered += HandleRecovered;
            }
        }

        private void Unsubscribe()
        {
            if (recovery != null)
            {
                recovery.Recovered -= HandleRecovered;
            }
        }

        private static float AxisPosition(
            float value,
            float minimum,
            float maximum,
            float half,
            float center)
        {
            float lower = minimum + half;
            float upper = maximum - half;
            return lower <= upper ? Mathf.Clamp(value, lower, upper) : center;
        }
    }
}

#endif
