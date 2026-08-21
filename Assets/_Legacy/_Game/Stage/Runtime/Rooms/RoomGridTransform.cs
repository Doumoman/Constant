#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Stage.Rooms
{
    [DisallowMultipleComponent]
    public sealed class RoomGridTransform : MonoBehaviour
    {
        public const float CellSize = 1f;

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return transform.TransformPoint(new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f));
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            Vector3 local = transform.InverseTransformPoint(world);
            return new Vector2Int(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.y));
        }
    }
}

#endif
