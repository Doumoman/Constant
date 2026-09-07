using System;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>Manual-only RUN04 marker controller. It uses deterministic open-cell steps, not production character physics.</summary>
    public sealed class MoonPalaceRunPreviewController : MonoBehaviour
    {
        [SerializeField] private TextMesh currentTileLabel;
        [SerializeField] private TextMesh currentRoomLabel;
        [SerializeField] private MoonPalaceRunPreviewCameraController previewCamera;
        [SerializeField] private MoonPalaceRunPreviewRouteGhost routeGhost;
        private MoonPalaceRunPreviewGrid grid;

        public MoonPalaceRunPreviewGrid Grid => grid;
        public Vector2Int CurrentTile { get; private set; }
        public string CurrentRoomId { get; private set; }
        public string EnteredConnectorId { get; private set; }
        public int SuccessfulStepCount { get; private set; }
        public int BlockedInputCount { get; private set; }
        public bool RouteGhostVisible => routeGhost != null && routeGhost.IsVisible;

        private void Awake()
        {
            if (grid == null) Initialize(MoonPalaceRunPreviewGrid.CreateDefault());
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) TryStep(0, 1);
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) TryStep(0, -1);
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) TryStep(-1, 0);
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) TryStep(1, 0);
            else if (Input.GetKeyDown(KeyCode.R)) ResetToStart();
            else if (Input.GetKeyDown(KeyCode.G)) ToggleRouteGhost();
        }

        public void ConfigureVisuals(TextMesh tileLabel, TextMesh roomLabel, MoonPalaceRunPreviewCameraController cameraController, MoonPalaceRunPreviewRouteGhost ghost)
        {
            currentTileLabel = tileLabel;
            currentRoomLabel = roomLabel;
            previewCamera = cameraController;
            routeGhost = ghost;
            RefreshLabels();
        }

        public void Initialize(MoonPalaceRunPreviewGrid previewGrid)
        {
            grid = previewGrid ?? throw new ArgumentNullException(nameof(previewGrid));
            CurrentTile = grid.StartTile;
            CurrentRoomId = grid.GetRoomId(CurrentTile.x, CurrentTile.y);
            EnteredConnectorId = string.Empty;
            SuccessfulStepCount = 0;
            BlockedInputCount = 0;
            SetMarkerPosition();
            if (previewCamera != null) previewCamera.Initialize(grid, CurrentRoomId);
            if (routeGhost != null) routeGhost.Initialize(grid, grid.Config.AutoRouteGhostEnabled);
            RefreshLabels();
        }

        public bool TryStep(int deltaX, int deltaY)
        {
            if (grid == null) Initialize(MoonPalaceRunPreviewGrid.CreateDefault());
            var next = new Vector2Int(CurrentTile.x + deltaX, CurrentTile.y + deltaY);
            if (!grid.CanStep(CurrentTile.x, CurrentTile.y, next.x, next.y))
            {
                BlockedInputCount++;
                RefreshLabels();
                return false;
            }
            var prior = CurrentTile;
            CurrentTile = next;
            SuccessfulStepCount++;
            EnteredConnectorId = string.Empty;
            if (grid.TryGetConnectorTransition(CurrentRoomId, prior, next, out var trigger, out var targetRoom))
            {
                EnteredConnectorId = trigger.ConnectorId;
                CurrentRoomId = targetRoom;
                if (previewCamera != null) previewCamera.FocusRoom(targetRoom);
            }
            SetMarkerPosition();
            RefreshLabels();
            return true;
        }

        public void ResetToStart()
        {
            if (grid == null) Initialize(MoonPalaceRunPreviewGrid.CreateDefault());
            CurrentTile = grid.StartTile;
            CurrentRoomId = grid.GetRoomId(CurrentTile.x, CurrentTile.y);
            EnteredConnectorId = string.Empty;
            SuccessfulStepCount = 0;
            SetMarkerPosition();
            if (previewCamera != null) previewCamera.FocusRoom(CurrentRoomId);
            RefreshLabels();
        }

        public void ToggleRouteGhost()
        {
            if (routeGhost == null) return;
            routeGhost.SetVisible(!routeGhost.IsVisible);
            RefreshLabels();
        }

        private void SetMarkerPosition()
        {
            transform.position = new Vector3(CurrentTile.x + 0.5f, CurrentTile.y + 0.5f, -2f);
        }

        private void RefreshLabels()
        {
            if (currentTileLabel != null) currentTileLabel.text = "Tile: " + CurrentTile.x + "," + CurrentTile.y + "  steps=" + SuccessfulStepCount + " blocked=" + BlockedInputCount;
            if (currentRoomLabel != null) currentRoomLabel.text = "Current room: " + (CurrentRoomId ?? string.Empty) + (string.IsNullOrEmpty(EnteredConnectorId) ? string.Empty : " via " + EnteredConnectorId);
        }
    }
}
