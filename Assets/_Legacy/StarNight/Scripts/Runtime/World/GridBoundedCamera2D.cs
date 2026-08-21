#if LEGACY_DISABLED
using StarNight.Grid;
using StarNight.Player;
using UnityEngine;

namespace StarNight.World
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class GridBoundedCamera2D : MonoBehaviour
    {
        [SerializeField] private Camera cameraComponent;
        [SerializeField] private Transform target;
        [SerializeField] private Rigidbody2D targetBody;
        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private PlayerRecovery recovery;
        [SerializeField] private Vector2 followOffset = new Vector2(0f, 1.25f);
        [SerializeField] private float horizontalLookAhead = 1.1f;
        [SerializeField] private float smoothTime = 0.16f;
        [SerializeField] private float maximumSpeed = 40f;

        private Vector3 smoothVelocity;

        public void Configure(
            Camera targetCamera,
            Transform followTarget,
            Rigidbody2D followBody,
            GridWorld world,
            PlayerRecovery playerRecovery)
        {
            Unsubscribe();
            cameraComponent = targetCamera;
            target = followTarget;
            targetBody = followBody;
            gridWorld = world;
            recovery = playerRecovery;
            Subscribe();
            SnapNow();
        }

        private void Awake()
        {
            if (cameraComponent == null)
            {
                cameraComponent = GetComponent<Camera>();
            }

            if (gridWorld == null)
            {
                gridWorld = FindFirstObjectByType<GridWorld>();
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void LateUpdate()
        {
            if (cameraComponent == null || target == null || gridWorld == null)
            {
                return;
            }

            float lookAhead = targetBody != null
                ? Mathf.Clamp(targetBody.linearVelocity.x / 3.75f, -1f, 1f) * horizontalLookAhead
                : 0f;
            Vector2 desired = (Vector2)target.position + followOffset + new Vector2(lookAhead, 0f);
            Vector2 clamped = CalculateClampedPosition(desired);
            Vector3 targetPosition = new Vector3(clamped.x, clamped.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref smoothVelocity,
                smoothTime,
                maximumSpeed,
                Time.unscaledDeltaTime);
        }

        public Vector2 CalculateClampedPosition(Vector2 desired)
        {
            if (cameraComponent == null || gridWorld == null)
            {
                return desired;
            }

            Rect world = gridWorld.WorldBounds;
            float halfHeight = cameraComponent.orthographicSize;
            float halfWidth = halfHeight * cameraComponent.aspect;

            float x = ClampAxis(desired.x, world.xMin + halfWidth, world.xMax - halfWidth, world.center.x);
            float y = ClampAxis(desired.y, world.yMin + halfHeight, world.yMax - halfHeight, world.center.y);
            return new Vector2(x, y);
        }

        public void SnapNow()
        {
            if (cameraComponent == null || target == null || gridWorld == null)
            {
                return;
            }

            smoothVelocity = Vector3.zero;
            Vector2 desired = (Vector2)target.position + followOffset;
            Vector2 clamped = CalculateClampedPosition(desired);
            transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
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

        private static float ClampAxis(float value, float minimum, float maximum, float fallback)
        {
            return minimum <= maximum
                ? Mathf.Clamp(value, minimum, maximum)
                : fallback;
        }
    }
}

#endif
