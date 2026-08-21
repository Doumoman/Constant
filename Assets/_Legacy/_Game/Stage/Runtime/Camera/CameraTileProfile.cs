#if LEGACY_DISABLED
using System;
using StarNight.Stage.Rooms;
using UnityEngine;

namespace StarNight.Stage.CameraSystem
{
    [Serializable]
    public sealed class CameraTileProfile
    {
        public const int ReferenceWidth = 1920;
        public const int ReferenceHeight = 1080;
        public const float ReferenceAspect = 16f / 9f;
        public const float DefaultVisibleHeightTiles = 11f;
        public const float DefaultVisibleWidthTiles = DefaultVisibleHeightTiles * ReferenceAspect;
        public const float DefaultCriticalWidthTiles = 18f;
        public const float DefaultCriticalHeightTiles = 10f;

        [Min(1f)] public float visibleHeightTiles = DefaultVisibleHeightTiles;
        [Min(1f)] public float criticalWidthTiles = DefaultCriticalWidthTiles;
        [Min(1f)] public float criticalHeightTiles = DefaultCriticalHeightTiles;
        [Min(0f)] public float deadZoneWidthTiles = 4f;
        [Min(0f)] public float deadZoneHeightTiles = 2f;
        [Min(0f)] public float lookAheadHorizontalTiles = 1.25f;
        [Min(0f)] public float lookAheadVerticalTiles = 0.5f;
        [Min(0f)] public float positionSmoothTime = 0.10f;
        [Min(0f)] public float roomTransitionBlendSeconds = 0.22f;
        [Range(1f, 2f)] public float portalPreviewDepthTiles = 2f;

        public float OrthographicSize => visibleHeightTiles * 0.5f;
        public float VisibleWidthTiles => visibleHeightTiles * ReferenceAspect;

        public RoomCameraMode ResolveMode(Vector2Int roomSizeCells)
        {
            bool clampX = roomSizeCells.x > 20;
            bool clampY = roomSizeCells.y > 11;
            if (clampX && clampY)
            {
                return RoomCameraMode.BoundedXY;
            }

            if (clampX)
            {
                return RoomCameraMode.BoundedX;
            }

            return clampY ? RoomCameraMode.BoundedY : RoomCameraMode.Fixed;
        }

        public Rect CalculateViewportRect(float displayAspect)
        {
            if (displayAspect <= 0f || Mathf.Approximately(displayAspect, ReferenceAspect))
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            if (displayAspect > ReferenceAspect)
            {
                float width = ReferenceAspect / displayAspect;
                return new Rect((1f - width) * 0.5f, 0f, width, 1f);
            }

            float height = displayAspect / ReferenceAspect;
            return new Rect(0f, (1f - height) * 0.5f, 1f, height);
        }

        public void ApplyTo(Camera camera, float worldUnitsPerCell = 1f, int pixelWidth = 0, int pixelHeight = 0)
        {
            if (camera == null)
            {
                return;
            }

            camera.orthographic = true;
            camera.orthographicSize = OrthographicSize * Mathf.Max(0.0001f, worldUnitsPerCell);
            int width = pixelWidth > 0 ? pixelWidth : Screen.width;
            int height = pixelHeight > 0 ? pixelHeight : Screen.height;
            float displayAspect = height > 0 ? width / (float)height : ReferenceAspect;
            camera.rect = CalculateViewportRect(displayAspect);
        }
    }
}

#endif
