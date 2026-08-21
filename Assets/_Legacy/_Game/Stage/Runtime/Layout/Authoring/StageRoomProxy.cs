#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Stage.Layout.Authoring
{
    [DisallowMultipleComponent]
    public sealed class StageRoomProxy : MonoBehaviour
    {
        public const float PreviewCellScale = 0.25f;
        [SerializeField] private string nodeGuid;
        [SerializeField] private RoomTemplate roomTemplate;
        [SerializeField] private Vector2Int positionCells;
        [SerializeField] private bool locked;
        [SerializeField] private bool mainRoute;
        [SerializeField] private RoomRole generatedRole;
        [SerializeField] private bool useGeneratedRole;

        public string NodeGuid => nodeGuid;
        public RoomTemplate Template => roomTemplate;
        public Vector2Int PositionCells => positionCells;
        public bool Locked => locked;
        public bool MainRoute => mainRoute;
        public RoomRole Role => useGeneratedRole ? generatedRole : roomTemplate != null ? roomTemplate.Role : RoomRole.Main;
        public Vector2Int SizeCells => roomTemplate != null ? roomTemplate.SizeCells : Vector2Int.zero;
        public RectInt CellRect => StageLayoutGraphUtility.GetCellRect(positionCells, SizeCells);

        public void Configure(string guid, RoomTemplate template, Vector2Int position, bool isLocked, bool isMainRoute)
        {
            nodeGuid = guid;
            roomTemplate = template;
            locked = isLocked;
            mainRoute = isMainRoute;
            useGeneratedRole = false;
            SetPositionCells(position);
        }

        public void ConfigureGenerated(
            string guid,
            RoomTemplate template,
            Vector2Int position,
            RoomRole role,
            bool isLocked,
            bool isMainRoute)
        {
            nodeGuid = guid;
            roomTemplate = template;
            generatedRole = role;
            useGeneratedRole = true;
            locked = isLocked;
            mainRoute = isMainRoute;
            SetPositionCells(position);
        }

        public void SetLocked(bool value)
        {
            locked = value;
        }

        public void SetPositionCells(Vector2Int position)
        {
            positionCells = StageLayoutGraphUtility.SnapToPlacementGrid(position);
            transform.position = new Vector3(positionCells.x * PreviewCellScale, positionCells.y * PreviewCellScale, 0f);
        }

        public bool TryGetSocket(string socketGuid, out RoomSocketDefinition socket)
        {
            socket = null;
            if (roomTemplate == null || roomTemplate.Sockets == null) return false;
            for (int index = 0; index < roomTemplate.Sockets.Count; index++)
            {
                RoomSocketDefinition candidate = roomTemplate.Sockets[index];
                if (candidate != null && string.Equals(candidate.SocketGuid, socketGuid, StringComparison.Ordinal))
                {
                    socket = candidate;
                    return true;
                }
            }
            return false;
        }

        public Vector3 GetSocketWorldPosition(RoomSocketDefinition socket)
        {
            return transform.position + new Vector3(socket.LocalCell.x * PreviewCellScale, socket.LocalCell.y * PreviewCellScale, 0f);
        }

        public void SetSimulationPreview(bool roomRectVisible, bool labelVisible)
        {
            Transform roomRect = transform.Find("RoomRect");
            if (roomRect != null && roomRect.TryGetComponent(out Renderer renderer)) renderer.enabled = roomRectVisible;
            Transform label = transform.Find("RoomLabel");
            if (label != null) label.gameObject.SetActive(labelVisible);
        }

        private void OnValidate() => SetPositionCells(positionCells);

        private void OnDrawGizmos()
        {
            if (roomTemplate == null) return;
            Vector3 size = new Vector3(SizeCells.x * PreviewCellScale, SizeCells.y * PreviewCellScale, 0.06f);
            Vector3 center = transform.position + size * 0.5f;
            Color color = GetRoleColor(Role);
            Color fill = color;
            fill.a = locked ? 0.18f : 0.3f;
            Gizmos.color = fill;
            Gizmos.DrawCube(center, size);
            Gizmos.color = mainRoute ? Color.white : color;
            Gizmos.DrawWireCube(center, size);
            if (roomTemplate.Sockets == null) return;
            Gizmos.color = new Color(0.3f, 1f, 0.65f, 1f);
            foreach (RoomSocketDefinition socket in roomTemplate.Sockets)
                if (socket != null) Gizmos.DrawCube(GetSocketWorldPosition(socket), Vector3.one * 0.15f);
        }

        private static Color GetRoleColor(RoomRole role)
        {
            switch (role)
            {
                case RoomRole.Start: return new Color(0.25f, 0.8f, 0.5f);
                case RoomRole.Branch: return new Color(0.65f, 0.4f, 0.95f);
                case RoomRole.Secret: return new Color(0.2f, 0.9f, 0.95f);
                case RoomRole.Exit: return new Color(0.95f, 0.68f, 0.25f);
                case RoomRole.Boss: return new Color(0.9f, 0.25f, 0.25f);
                default: return new Color(0.3f, 0.55f, 0.9f);
            }
        }
    }
}

#endif
