#if LEGACY_DISABLED
using StarNight.Rooms;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarNight.Debugging
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class P4RoomGalleryCamera2D : MonoBehaviour
    {
        [SerializeField] private RoomTemplate2D[] rooms =
            System.Array.Empty<RoomTemplate2D>();
        [SerializeField] private Bounds overviewBounds =
            new Bounds(new Vector3(75f, 55f, 0f), new Vector3(150f, 110f, 1f));
        [SerializeField, Min(0f)] private float padding = 2f;
        [SerializeField] private int focusedRoom = -1;

        private Camera galleryCamera;

        public int FocusedRoom => focusedRoom;

        public void Configure(
            RoomTemplate2D[] roomInstances,
            Bounds galleryBounds,
            float viewPadding)
        {
            rooms = roomInstances ?? System.Array.Empty<RoomTemplate2D>();
            overviewBounds = galleryBounds;
            padding = Mathf.Max(0f, viewPadding);
            focusedRoom = -1;
            galleryCamera = GetComponent<Camera>();
            ApplyView();
        }

        private void Awake()
        {
            galleryCamera = GetComponent<Camera>();
            ApplyView();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || rooms.Length == 0)
            {
                return;
            }

            if (keyboard.homeKey.wasPressedThisFrame
                || keyboard.tabKey.wasPressedThisFrame)
            {
                focusedRoom = -1;
                ApplyView();
            }
            else if (keyboard.rightArrowKey.wasPressedThisFrame
                     || keyboard.dKey.wasPressedThisFrame)
            {
                focusedRoom = focusedRoom < 0
                    ? 0
                    : (focusedRoom + 1) % rooms.Length;
                ApplyView();
            }
            else if (keyboard.leftArrowKey.wasPressedThisFrame
                     || keyboard.aKey.wasPressedThisFrame)
            {
                focusedRoom = focusedRoom < 0
                    ? rooms.Length - 1
                    : (focusedRoom - 1 + rooms.Length) % rooms.Length;
                ApplyView();
            }
        }

        private void ApplyView()
        {
            if (galleryCamera == null)
            {
                return;
            }

            Bounds targetBounds = overviewBounds;
            if (focusedRoom >= 0
                && focusedRoom < rooms.Length
                && rooms[focusedRoom] != null)
            {
                RoomTemplate2D room = rooms[focusedRoom];
                Vector3 size = new Vector3(
                    room.LogicalSize.x,
                    room.LogicalSize.y,
                    1f);
                Vector3 center = room.transform.TransformPoint(
                    new Vector3(
                        room.LogicalSize.x * 0.5f,
                        room.LogicalSize.y * 0.5f,
                        0f));
                targetBounds = new Bounds(center, size);
            }

            float aspect = Mathf.Max(0.1f, galleryCamera.aspect);
            float vertical = targetBounds.extents.y + padding;
            float horizontal = (targetBounds.extents.x + padding) / aspect;
            galleryCamera.orthographicSize = Mathf.Max(vertical, horizontal);
            transform.position = new Vector3(
                targetBounds.center.x,
                targetBounds.center.y,
                -10f);
        }
    }
}

#endif
