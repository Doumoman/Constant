#if LEGACY_DISABLED
using StarNight.Stage.Rooms;
using UnityEngine;

namespace StarNight.Stage.Visuals
{
    [DisallowMultipleComponent]
    public sealed class RoomThemeOcclusionMask : MonoBehaviour
    {
        [SerializeField] private RoomRuntime owner;
        [SerializeField] private SpriteRenderer maskRenderer;

        public RoomRuntime Owner => owner;

        public void Configure(RoomRuntime room, SpriteRenderer renderer)
        {
            owner = room;
            maskRenderer = renderer;
        }

        public bool CoversRoom(float tolerance = 0.01f)
        {
            if (owner == null || maskRenderer == null)
            {
                return false;
            }
            Bounds bounds = maskRenderer.bounds;
            Rect room = owner.WorldBounds;
            return bounds.min.x <= room.xMin + tolerance && bounds.max.x >= room.xMax - tolerance &&
                   bounds.min.y <= room.yMin + tolerance && bounds.max.y >= room.yMax - tolerance;
        }
    }
}

#endif
