#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Stage.Layout.Authoring
{
    [DisallowMultipleComponent]
    public sealed class StageFullRoomPreviewInstance : MonoBehaviour
    {
        [SerializeField] private StageRoomProxy room;
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private bool fallbackShell;

        public StageRoomProxy Room => room;
        public bool IsVisible => contentRoot != null && contentRoot.activeSelf;
        public bool IsFallbackShell => fallbackShell;
        public Vector3 RoomCenter => room != null
            ? room.transform.position + new Vector3(
                room.SizeCells.x * StageRoomProxy.PreviewCellScale * 0.5f,
                room.SizeCells.y * StageRoomProxy.PreviewCellScale * 0.5f,
                0f)
            : transform.position;

        public void Configure(StageRoomProxy owner, GameObject content, bool usesFallbackShell)
        {
            room = owner;
            contentRoot = content;
            fallbackShell = usesFallbackShell;
        }

        public void SetVisible(bool visible)
        {
            if (contentRoot != null) contentRoot.SetActive(visible);
        }
    }
}

#endif
