#if LEGACY_DISABLED
using System.Collections;
using StarNight.Stage.CameraSystem;
using StarNight.Stage.Rooms;
using UnityEngine;

namespace StarNight.Stage.Transitions
{
    [DisallowMultipleComponent]
    public sealed class RoomCameraController : MonoBehaviour
    {
        public const float DefaultTransitionSeconds = 0.22f;
        public const float InstantAccessibilityFadeSeconds = 0.08f;

        [SerializeField] private Camera roomCamera;
        [SerializeField] private CameraTileProfile tileProfile = new CameraTileProfile();
        [SerializeField] private float transitionSeconds = DefaultTransitionSeconds;

        private Transform followTarget;
        private bool isMoving;
        private bool hasFollowSample;
        private Vector2 lastFollowPosition;
        private Vector2 followSmoothVelocity;

        public Camera RoomCamera => roomCamera;
        public CameraTileProfile TileProfile => tileProfile;
        public float TransitionSeconds => transitionSeconds;
        public RoomRuntime CurrentRoom { get; private set; }

        public void Configure(
            Camera targetCamera,
            float duration = DefaultTransitionSeconds,
            CameraTileProfile profile = null)
        {
            roomCamera = targetCamera;
            tileProfile = profile ?? tileProfile ?? new CameraTileProfile();
            transitionSeconds = Mathf.Max(0f, duration);
            ApplyCameraContract();
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
            hasFollowSample = false;
            followSmoothVelocity = Vector2.zero;
        }

        public void SnapToRoom(RoomRuntime room)
        {
            if (roomCamera == null || room == null)
            {
                return;
            }

            roomCamera.transform.position = CalculateRoomCameraPosition(room);
            CurrentRoom = room;
        }

        public void SnapToRoom(RoomRuntime room, Vector2 focusPosition)
        {
            if (roomCamera == null || room == null)
            {
                return;
            }

            roomCamera.transform.position = CalculateRoomCameraPosition(room, focusPosition);
            CurrentRoom = room;
        }

        public IEnumerator MoveToRoom(RoomRuntime room)
        {
            Vector2 focus = room != null && room.CameraAnchor != null
                ? room.CameraAnchor.position
                : room != null ? room.WorldBounds.center : Vector2.zero;
            yield return MoveToRoom(room, focus);
        }

        public IEnumerator MoveToRoom(RoomRuntime room, Vector2 focusPosition)
        {
            if (roomCamera == null || room == null)
            {
                yield break;
            }

            Vector3 target = CalculateRoomCameraPosition(room, focusPosition);
            float duration = transitionSeconds;
            if (duration <= 0f)
            {
                roomCamera.transform.position = target;
                CurrentRoom = room;
                yield break;
            }

            isMoving = true;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            roomCamera.transform.position = target;
            CurrentRoom = room;
            isMoving = false;
        }

        public bool AreCriticalTargetsInside(RoomRuntime room)
        {
            if (roomCamera == null || room == null || CurrentRoom != room)
            {
                return false;
            }
            CameraCriticalFrame frame = roomCamera.GetComponent<CameraCriticalFrame>();
            if (frame == null)
            {
                return false;
            }
            CameraCriticalTarget[] targets = room.GetComponentsInChildren<CameraCriticalTarget>(true);
            for (int index = 0; index < targets.Length; index++)
            {
                if (targets[index] != null && !targets[index].IsInside(frame.CriticalFrame))
                {
                    return false;
                }
            }
            return true;
        }

        public Vector3 CalculateRoomCameraPosition(RoomRuntime room)
        {
            Vector3 anchor = room.CameraAnchor != null
                ? room.CameraAnchor.position
                : new Vector3(room.WorldBounds.center.x, room.WorldBounds.center.y, 0f);
            return CalculateRoomCameraPosition(room, anchor);
        }

        public Vector3 CalculateRoomCameraPosition(RoomRuntime room, Vector2 focusPosition)
        {
            Vector3 anchor = room.CameraAnchor != null
                ? room.CameraAnchor.position
                : new Vector3(room.WorldBounds.center.x, room.WorldBounds.center.y, 0f);
            RoomCameraMode mode = room.CameraMode;
            Vector2 desired = new Vector2(
                MovesOnX(mode) ? focusPosition.x : anchor.x,
                MovesOnY(mode) ? focusPosition.y : anchor.y);
            if (UsesAnchorBounds(mode) && room.TryGetCameraAnchorBounds(out Rect anchorBounds))
            {
                if (UsesAnchorBoundsX(mode)) desired.x = Mathf.Clamp(desired.x, anchorBounds.xMin, anchorBounds.xMax);
                if (UsesAnchorBoundsY(mode)) desired.y = Mathf.Clamp(desired.y, anchorBounds.yMin, anchorBounds.yMax);
            }
            float z = roomCamera != null ? roomCamera.transform.position.z : -10f;
            if (roomCamera == null || !roomCamera.orthographic)
            {
                return new Vector3(desired.x, desired.y, z);
            }

            float halfHeight = tileProfile.visibleHeightTiles * 0.5f;
            float halfWidth = tileProfile.VisibleWidthTiles * 0.5f;
            float x = ClampAxis(desired.x, room.WorldBounds.xMin, room.WorldBounds.xMax, halfWidth);
            float y = ClampAxis(desired.y, room.WorldBounds.yMin, room.WorldBounds.yMax, halfHeight);
            return new Vector3(x, y, z);
        }

        private void LateUpdate()
        {
            if (isMoving || roomCamera == null || CurrentRoom == null || followTarget == null)
            {
                return;
            }

            RoomCameraMode mode = CurrentRoom.CameraMode;
            if (MovesOnX(mode) || MovesOnY(mode))
            {
                Vector2 followPosition = followTarget.position;
                Vector2 movement = hasFollowSample ? followPosition - lastFollowPosition : Vector2.zero;
                lastFollowPosition = followPosition;
                hasFollowSample = true;

                Vector2 focus = followPosition + new Vector2(
                    Mathf.Sign(movement.x) * tileProfile.lookAheadHorizontalTiles,
                    Mathf.Sign(movement.y) * tileProfile.lookAheadVerticalTiles);
                Vector2 deadZoneTarget = ResolveDeadZoneTarget(roomCamera.transform.position, focus, mode);
                Vector2 smoothed = Vector2.SmoothDamp(
                    roomCamera.transform.position,
                    deadZoneTarget,
                    ref followSmoothVelocity,
                    tileProfile.positionSmoothTime,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime);
                roomCamera.transform.position = CalculateRoomCameraPosition(CurrentRoom, smoothed);
            }
        }

        public bool IsViewportInside(RoomRuntime room, float tolerance = 0.01f)
        {
            if (roomCamera == null || room == null || !roomCamera.orthographic)
            {
                return false;
            }

            Vector3 position = roomCamera.transform.position;
            float halfHeight = tileProfile.visibleHeightTiles * 0.5f;
            float halfWidth = tileProfile.VisibleWidthTiles * 0.5f;
            return IsAxisInside(position.x, room.WorldBounds.xMin, room.WorldBounds.xMax, halfWidth, tolerance) &&
                   IsAxisInside(position.y, room.WorldBounds.yMin, room.WorldBounds.yMax, halfHeight, tolerance);
        }

        private void ApplyCameraContract()
        {
            tileProfile ??= new CameraTileProfile();
            tileProfile.ApplyTo(roomCamera);
            if (roomCamera == null)
            {
                return;
            }

            CameraCriticalFrame frame = roomCamera.GetComponent<CameraCriticalFrame>();
            if (frame == null)
            {
                frame = roomCamera.gameObject.AddComponent<CameraCriticalFrame>();
            }
            frame.Configure(tileProfile);
        }

        private Vector2 ResolveDeadZoneTarget(Vector2 cameraPosition, Vector2 focus, RoomCameraMode mode)
        {
            Vector2 target = cameraPosition;
            if (MovesOnX(mode))
            {
                float halfWidth = tileProfile.deadZoneWidthTiles * 0.5f;
                if (focus.x < cameraPosition.x - halfWidth) target.x = focus.x + halfWidth;
                if (focus.x > cameraPosition.x + halfWidth) target.x = focus.x - halfWidth;
            }

            if (MovesOnY(mode))
            {
                float halfHeight = tileProfile.deadZoneHeightTiles * 0.5f;
                if (focus.y < cameraPosition.y - halfHeight) target.y = focus.y + halfHeight;
                if (focus.y > cameraPosition.y + halfHeight) target.y = focus.y - halfHeight;
            }
            return target;
        }

        private static bool MovesOnX(RoomCameraMode mode)
        {
            return mode == RoomCameraMode.BoundedX || mode == RoomCameraMode.BoundedXY ||
                   mode == RoomCameraMode.BoundedXAnchors || mode == RoomCameraMode.BoundedXYAnchors;
        }

        private static bool MovesOnY(RoomCameraMode mode)
        {
            return mode == RoomCameraMode.BoundedY || mode == RoomCameraMode.BoundedXY ||
                   mode == RoomCameraMode.BoundedYAnchors || mode == RoomCameraMode.BoundedXYAnchors;
        }

        private static bool UsesAnchorBounds(RoomCameraMode mode)
        {
            return mode == RoomCameraMode.BoundedXAnchors ||
                   mode == RoomCameraMode.BoundedYAnchors ||
                   mode == RoomCameraMode.BoundedXYAnchors;
        }

        private static bool UsesAnchorBoundsX(RoomCameraMode mode)
        {
            return mode == RoomCameraMode.BoundedXAnchors || mode == RoomCameraMode.BoundedXYAnchors;
        }

        private static bool UsesAnchorBoundsY(RoomCameraMode mode)
        {
            return mode == RoomCameraMode.BoundedYAnchors || mode == RoomCameraMode.BoundedXYAnchors;
        }

        private static float ClampAxis(float value, float minimum, float maximum, float extent)
        {
            float lower = minimum + extent;
            float upper = maximum - extent;
            return lower <= upper ? Mathf.Clamp(value, lower, upper) : (minimum + maximum) * 0.5f;
        }

        private static bool IsAxisInside(float center, float minimum, float maximum, float extent, float tolerance)
        {
            if (maximum - minimum < extent * 2f)
            {
                return Mathf.Abs(center - (minimum + maximum) * 0.5f) <= tolerance;
            }
            return center - extent >= minimum - tolerance && center + extent <= maximum + tolerance;
        }
    }
}

#endif
