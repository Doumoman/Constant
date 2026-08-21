#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Rooms
{
    [DisallowMultipleComponent]
    public sealed class RoomSocket2D : MonoBehaviour
    {
        [SerializeField] private string socketId = "Socket";
        [SerializeField] private RoomSocketSide side;
        [SerializeField] private Vector2Int cellOffset;
        [SerializeField] private Vector2Int openingSize = Vector2Int.one;
        [SerializeField] private RoomTraversalType traversalType;
        [SerializeField] private bool mainRouteAllowed = true;
        [SerializeField] private Vector2Int validationAnchor;

        public string SocketId => socketId;
        public RoomSocketSide Side => side;
        public Vector2Int CellOffset => cellOffset;
        public Vector2Int OpeningSize => openingSize;
        public RoomTraversalType TraversalType => traversalType;
        public bool MainRouteAllowed => mainRouteAllowed;
        public Vector2Int ValidationAnchor => validationAnchor;

        public void Configure(
            string id,
            RoomSocketSide socketSide,
            Vector2Int offset,
            Vector2Int size,
            RoomTraversalType traversal,
            bool allowMainRoute,
            Vector2Int anchor)
        {
            socketId = id;
            side = socketSide;
            cellOffset = offset;
            openingSize = new Vector2Int(
                Mathf.Max(1, size.x),
                Mathf.Max(1, size.y));
            traversalType = traversal;
            mainRouteAllowed = allowMainRoute;
            validationAnchor = anchor;
            transform.localPosition = new Vector3(
                offset.x + 0.5f,
                offset.y + 0.5f,
                0f);
        }

        public bool IsOnBoundary(Vector2Int logicalSize)
        {
            switch (side)
            {
                case RoomSocketSide.Left:
                    return cellOffset.x == 0 && openingSize.x == 1;
                case RoomSocketSide.Right:
                    return cellOffset.x == logicalSize.x - 1
                        && openingSize.x == 1;
                case RoomSocketSide.Bottom:
                    return cellOffset.y == 0 && openingSize.y == 1;
                case RoomSocketSide.Top:
                    return cellOffset.y == logicalSize.y - 1
                        && openingSize.y == 1;
                default:
                    return false;
            }
        }

        private void OnDrawGizmos()
        {
            Color color = mainRouteAllowed
                ? new Color(0.25f, 0.95f, 1f, 0.9f)
                : new Color(1f, 0.65f, 0.2f, 0.9f);
            Gizmos.color = color;
            Gizmos.DrawWireCube(
                transform.position,
                new Vector3(openingSize.x * 0.8f, openingSize.y * 0.8f, 0.1f));
        }
    }
}

#endif
