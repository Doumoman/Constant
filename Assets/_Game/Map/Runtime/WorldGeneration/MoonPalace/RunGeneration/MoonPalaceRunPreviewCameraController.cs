using System;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>Room-bound inspection camera; it deliberately owns neither Cinemachine nor gameplay camera policy.</summary>
    public sealed class MoonPalaceRunPreviewCameraController : MonoBehaviour
    {
        [SerializeField] private Camera previewCamera;
        [SerializeField] private bool useShortLerp = true;
        [SerializeField, Min(0.01f)] private float lerpSeconds = 0.18f;
        private MoonPalaceRunPreviewGrid grid;
        private Vector3 targetPosition;
        private float targetOrthographicSize;

        public string CurrentRoomId { get; private set; }
        public string PreviousRoomId { get; private set; }
        public bool UseShortLerp => useShortLerp;
        public string TransitionMode => useShortLerp ? "ShortLerp" : "Snap";
        public Camera PreviewCamera => previewCamera;

        private void Awake()
        {
            if (previewCamera == null) previewCamera = GetComponent<Camera>();
        }

        private void Start()
        {
            if (grid == null)
            {
                var preview = MoonPalaceRunPreviewGrid.CreateDefault();
                Initialize(preview, preview.GetRoomId(preview.StartTile.x, preview.StartTile.y));
            }
        }

        private void LateUpdate()
        {
            if (previewCamera == null || string.IsNullOrEmpty(CurrentRoomId)) return;
            if (!useShortLerp)
            {
                previewCamera.transform.position = targetPosition;
                previewCamera.orthographicSize = targetOrthographicSize;
                return;
            }
            var factor = Mathf.Clamp01(Time.unscaledDeltaTime / Mathf.Max(lerpSeconds, 0.01f));
            previewCamera.transform.position = Vector3.Lerp(previewCamera.transform.position, targetPosition, factor);
            previewCamera.orthographicSize = Mathf.Lerp(previewCamera.orthographicSize, targetOrthographicSize, factor);
        }

        public void Configure(Camera camera, bool shortLerp, float seconds)
        {
            previewCamera = camera;
            useShortLerp = shortLerp;
            lerpSeconds = Mathf.Max(seconds, 0.01f);
        }

        public void Initialize(MoonPalaceRunPreviewGrid previewGrid, string startRoomId)
        {
            grid = previewGrid ?? throw new ArgumentNullException(nameof(previewGrid));
            PreviousRoomId = string.Empty;
            FocusRoom(startRoomId, true);
        }

        public void FocusRoom(string roomId) => FocusRoom(roomId, false);

        public MoonPalaceRunPreviewRoomFrame GetFrame(string roomId)
        {
            if (grid == null) throw new InvalidOperationException("RUN04 camera must be initialized with a preview grid before resolving a room frame.");
            return grid.GetRoomFrame(roomId);
        }

        private void FocusRoom(string roomId, bool immediate)
        {
            if (grid == null || string.IsNullOrWhiteSpace(roomId)) return;
            var frame = grid.GetRoomFrame(roomId);
            PreviousRoomId = CurrentRoomId ?? string.Empty;
            CurrentRoomId = roomId;
            targetPosition = new Vector3(frame.Center.x, frame.Center.y, -10f);
            targetOrthographicSize = frame.OrthographicSize;
            if (previewCamera != null && (immediate || !useShortLerp))
            {
                previewCamera.transform.position = targetPosition;
                previewCamera.orthographicSize = targetOrthographicSize;
            }
        }
    }
}
