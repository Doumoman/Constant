#if LEGACY_DISABLED
using StarNight.Stage.Rooms;
using UnityEngine;

namespace StarNight.Stage.Maru
{
    [DisallowMultipleComponent]
    public sealed class MaruLane : MonoBehaviour
    {
        [SerializeField] private RoomRuntime room;
        [SerializeField] private Vector2 leftEntry;
        [SerializeField] private Vector2 rightEntry;

        public RoomRuntime Room => room;
        public Vector2 LeftEntry => leftEntry;
        public Vector2 RightEntry => rightEntry;

        public void Configure(RoomRuntime owner)
        {
            room = owner;
            if (room == null)
            {
                return;
            }

            Rect bounds = room.WorldBounds;
            float y = bounds.yMin + 1.55f;
            leftEntry = new Vector2(bounds.xMin + 0.9f, y);
            rightEntry = new Vector2(bounds.xMax - 0.9f, y);
        }

        public Vector2 GetEntry(Vector2Int approachDirection)
        {
            if (approachDirection.x > 0)
            {
                return leftEntry;
            }
            if (approachDirection.x < 0)
            {
                return rightEntry;
            }
            return leftEntry;
        }

        public Vector2 ClampToLane(Vector2 position)
        {
            float minimum = Mathf.Min(leftEntry.x, rightEntry.x);
            float maximum = Mathf.Max(leftEntry.x, rightEntry.x);
            return new Vector2(Mathf.Clamp(position.x, minimum, maximum), leftEntry.y);
        }
    }
}

#endif
