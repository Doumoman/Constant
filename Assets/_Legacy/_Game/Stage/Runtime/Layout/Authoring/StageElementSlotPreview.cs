#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Stage.Layout.Authoring
{
    [DisallowMultipleComponent]
    public sealed class StageElementSlotPreview : MonoBehaviour
    {
        [SerializeField] private StageRoomProxy room;
        [SerializeField] private GeneratedElementSlotKind kind;
        [SerializeField] private Vector2Int localCell;

        public GeneratedElementSlotKind Kind => kind;
        public Vector2Int LocalCell => localCell;

        public void Configure(StageRoomProxy owner, GeneratedElementSlotKind slotKind, Vector2Int cell)
        {
            room = owner;
            kind = slotKind;
            localCell = cell;
            transform.position = room != null
                ? room.transform.position + new Vector3(cell.x * StageRoomProxy.PreviewCellScale, cell.y * StageRoomProxy.PreviewCellScale, -0.02f)
                : transform.position;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = kind == GeneratedElementSlotKind.Threat ? new Color(1f, 0.3f, 0.3f) :
                kind == GeneratedElementSlotKind.Utility ? new Color(0.3f, 0.85f, 1f) :
                kind == GeneratedElementSlotKind.Shop ? new Color(1f, 0.8f, 0.25f) :
                new Color(0.85f, 0.35f, 1f);
            Gizmos.DrawWireSphere(transform.position, 0.14f);
        }
    }
}

#endif
