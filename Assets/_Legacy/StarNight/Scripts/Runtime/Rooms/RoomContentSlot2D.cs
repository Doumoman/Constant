#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Rooms
{
    [DisallowMultipleComponent]
    public sealed class RoomContentSlot2D : MonoBehaviour
    {
        [SerializeField] private string slotId = "Slot";
        [SerializeField] private RoomContentSlotKind kind;
        [SerializeField] private Vector2Int cellOffset;
        [SerializeField] private bool visibleFromMainRoute;

        public string SlotId => slotId;
        public RoomContentSlotKind Kind => kind;
        public Vector2Int CellOffset => cellOffset;
        public bool VisibleFromMainRoute => visibleFromMainRoute;

        public void Configure(
            string id,
            RoomContentSlotKind slotKind,
            Vector2Int offset,
            bool preVisible)
        {
            slotId = id;
            kind = slotKind;
            cellOffset = offset;
            visibleFromMainRoute = preVisible;
            transform.localPosition = new Vector3(
                offset.x + 0.5f,
                offset.y + 0.5f,
                0f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = kind == RoomContentSlotKind.SafeCell
                ? new Color(0.3f, 1f, 0.55f, 0.75f)
                : new Color(1f, 0.82f, 0.25f, 0.75f);
            Gizmos.DrawWireSphere(transform.position, 0.24f);
        }
    }
}

#endif
