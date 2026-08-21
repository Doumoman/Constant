#if LEGACY_DISABLED
using System.Collections.Generic;
using System.Linq;
using StarNight.Stage.Transitions;
using UnityEngine;

namespace StarNight.Stage.Layout.Authoring
{
    public enum StageLayoutSimulationPhase
    {
        Exploration,
        Bell1,
        Bell2,
        MaruChase,
        ExitReached,
    }

    [DisallowMultipleComponent]
    public sealed class StageLayoutSimulationController : MonoBehaviour
    {
        public const float Bell1VirtualSeconds = 120f;
        public const float Bell2VirtualSeconds = 165f;
        public const float MaruVirtualSeconds = 195f;

        [SerializeField] private Camera previewCamera;
        [SerializeField] private Transform ghostPlayer;
        [SerializeField] private StageMaruRoutePreview maruRoutePreview;
        [SerializeField] private float transitionSeconds = RoomCameraController.DefaultTransitionSeconds;
        [SerializeField] private float roomDwellSeconds = 0.72f;
        [SerializeField] private float virtualSecondsPerRealSecond = 90f;
        [SerializeField] private bool autoStartOnPlay = true;
        [SerializeField] private List<StageRoomProxy> route = new List<StageRoomProxy>();
        [SerializeField] private List<StageFullRoomPreviewInstance> fullRoomPreviews = new List<StageFullRoomPreviewInstance>();
        [SerializeField] private List<StageLayoutConnectionProxy> connections = new List<StageLayoutConnectionProxy>();

        private int currentRouteIndex = -1;
        private float dwellElapsed;
        private float transitionElapsed;
        private Vector3 cameraStart;
        private Vector3 cameraTarget;
        private Vector3 ghostStart;
        private Vector3 ghostTarget;
        private float cameraSizeStart;
        private float cameraSizeTarget;
        private bool transitionActive;

        public Camera PreviewCamera => previewCamera;
        public Transform GhostPlayer => ghostPlayer;
        public float TransitionSeconds => transitionSeconds;
        public float VirtualElapsedSeconds { get; private set; }
        public float ExitArrivalSeconds { get; private set; } = -1f;
        public bool IsRunning { get; private set; }
        public bool IsTransitioning => transitionActive;
        public StageLayoutSimulationPhase Phase { get; private set; }
        public StageRoomProxy CurrentRoom => currentRouteIndex >= 0 && currentRouteIndex < route.Count ? route[currentRouteIndex] : null;
        public IReadOnlyList<StageRoomProxy> MainRoute => route;
        public int VisibleFullRoomCount => fullRoomPreviews.Count(preview => preview != null && preview.IsVisible);

        public void Configure(
            Camera camera,
            Transform ghost,
            StageMaruRoutePreview maruPreview,
            IEnumerable<StageRoomProxy> rooms,
            IEnumerable<StageLayoutConnectionProxy> edges,
            IEnumerable<StageFullRoomPreviewInstance> fullPreviews)
        {
            previewCamera = camera;
            ghostPlayer = ghost;
            maruRoutePreview = maruPreview;
            connections = edges != null ? edges.Where(edge => edge != null).ToList() : new List<StageLayoutConnectionProxy>();
            fullRoomPreviews = fullPreviews != null ? fullPreviews.Where(preview => preview != null).ToList() : new List<StageFullRoomPreviewInstance>();
            route = BuildMainRoute(rooms, connections);
            transitionSeconds = RoomCameraController.DefaultTransitionSeconds;
            currentRouteIndex = -1;
            VirtualElapsedSeconds = 0f;
            ExitArrivalSeconds = -1f;
            Phase = StageLayoutSimulationPhase.Exploration;
            ShowGraphMode();
        }

        public void BeginSimulation(bool autoRun = true)
        {
            if (route.Count == 0) return;
            IsRunning = autoRun;
            currentRouteIndex = 0;
            dwellElapsed = 0f;
            transitionActive = false;
            VirtualElapsedSeconds = 0f;
            ExitArrivalSeconds = -1f;
            Phase = StageLayoutSimulationPhase.Exploration;
            FocusRoom(route[0], true);
        }

        public void StopSimulation()
        {
            IsRunning = false;
            transitionActive = false;
            dwellElapsed = 0f;
        }

        public bool MoveNextRoom(bool immediate = false)
        {
            if (route.Count == 0) return false;
            if (currentRouteIndex < 0)
            {
                BeginSimulation(false);
                return true;
            }
            if (currentRouteIndex >= route.Count - 1) return false;

            currentRouteIndex++;
            dwellElapsed = 0f;
            FocusRoom(route[currentRouteIndex], immediate);
            if (currentRouteIndex == route.Count - 1)
            {
                ExitArrivalSeconds = VirtualElapsedSeconds;
                Phase = StageLayoutSimulationPhase.ExitReached;
                IsRunning = false;
            }
            return true;
        }

        public void CompleteTransitionImmediate()
        {
            if (!transitionActive) return;
            ApplyTransition(1f);
            transitionActive = false;
        }

        public void SetVirtualPhase(StageLayoutSimulationPhase phase)
        {
            Phase = phase;
            switch (phase)
            {
                case StageLayoutSimulationPhase.Bell1:
                    VirtualElapsedSeconds = Bell1VirtualSeconds;
                    break;
                case StageLayoutSimulationPhase.Bell2:
                    VirtualElapsedSeconds = Bell2VirtualSeconds;
                    break;
                case StageLayoutSimulationPhase.MaruChase:
                    VirtualElapsedSeconds = MaruVirtualSeconds;
                    break;
                case StageLayoutSimulationPhase.Exploration:
                    VirtualElapsedSeconds = 0f;
                    break;
            }
            UpdateMaruPreview();
        }

        public void ShowGraphMode()
        {
            StopSimulation();
            for (int index = 0; index < fullRoomPreviews.Count; index++) fullRoomPreviews[index].SetVisible(false);
            SetProxyVisibility(null, false);
            maruRoutePreview?.SetChasePreview(false, 0f);
        }

        public void ShowRoomPreview(StageRoomProxy room)
        {
            StopSimulation();
            if (room == null) room = route.FirstOrDefault();
            if (room == null) return;
            currentRouteIndex = route.IndexOf(room);
            SetFullRoomVisibility(room);
            SetProxyVisibility(room, false);
            SnapCamera(room);
            if (ghostPlayer != null) ghostPlayer.position = GetRoomCenter(room) + Vector3.back * 0.25f;
            maruRoutePreview?.SetChasePreview(false, 0f);
        }

        public bool IsNeighbor(StageRoomProxy first, StageRoomProxy second)
        {
            if (first == null || second == null || first == second) return false;
            return connections.Any(edge => edge != null &&
                ((edge.SourceRoom == first && edge.TargetRoom == second) ||
                 (edge.SourceRoom == second && edge.TargetRoom == first)));
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (!IsRunning && autoStartOnPlay && currentRouteIndex < 0) BeginSimulation(true);
            if (!IsRunning && !transitionActive) return;

            float deltaTime = Time.unscaledDeltaTime;
            VirtualElapsedSeconds += deltaTime * virtualSecondsPerRealSecond;
            UpdatePhaseFromTime();
            if (transitionActive)
            {
                transitionElapsed += deltaTime;
                ApplyTransition(transitionSeconds <= 0f ? 1f : transitionElapsed / transitionSeconds);
                if (transitionElapsed >= transitionSeconds) transitionActive = false;
            }
            else if (IsRunning)
            {
                dwellElapsed += deltaTime;
                if (dwellElapsed >= roomDwellSeconds) MoveNextRoom();
            }
            UpdateMaruPreview();
        }

        private void FocusRoom(StageRoomProxy room, bool immediate)
        {
            SetFullRoomVisibility(room);
            SetProxyVisibility(room, true);
            cameraTarget = GetCameraTarget(room);
            ghostTarget = GetRoomCenter(room) + Vector3.back * 0.25f;
            cameraSizeTarget = GetCameraSize(room);
            if (immediate || previewCamera == null)
            {
                SnapCamera(room);
                if (ghostPlayer != null) ghostPlayer.position = ghostTarget;
                transitionActive = false;
                return;
            }

            cameraStart = previewCamera.transform.position;
            cameraSizeStart = previewCamera.orthographicSize;
            ghostStart = ghostPlayer != null ? ghostPlayer.position : ghostTarget;
            transitionElapsed = 0f;
            transitionActive = true;
        }

        private void ApplyTransition(float normalized)
        {
            float t = Mathf.Clamp01(normalized);
            float eased = t * t * (3f - 2f * t);
            if (previewCamera != null)
            {
                previewCamera.transform.position = Vector3.LerpUnclamped(cameraStart, cameraTarget, eased);
                previewCamera.orthographicSize = Mathf.LerpUnclamped(cameraSizeStart, cameraSizeTarget, eased);
            }
            if (ghostPlayer != null) ghostPlayer.position = Vector3.LerpUnclamped(ghostStart, ghostTarget, eased);
        }

        private void SnapCamera(StageRoomProxy room)
        {
            if (previewCamera == null) return;
            previewCamera.transform.position = GetCameraTarget(room);
            previewCamera.orthographicSize = GetCameraSize(room);
        }

        private Vector3 GetCameraTarget(StageRoomProxy room)
        {
            float z = previewCamera != null ? previewCamera.transform.position.z : -20f;
            Vector3 center = GetRoomCenter(room);
            return new Vector3(center.x, center.y, z);
        }

        private float GetCameraSize(StageRoomProxy room)
        {
            return CameraSystem.CameraTileProfile.DefaultVisibleHeightTiles *
                   StageRoomProxy.PreviewCellScale * 0.5f;
        }

        private void SetFullRoomVisibility(StageRoomProxy active)
        {
            for (int index = 0; index < fullRoomPreviews.Count; index++)
                fullRoomPreviews[index].SetVisible(fullRoomPreviews[index].Room == active);
        }

        private void SetProxyVisibility(StageRoomProxy active, bool includeNeighbors)
        {
            var allRooms = fullRoomPreviews.Select(preview => preview.Room).Where(room => room != null).Distinct().ToList();
            for (int index = 0; index < allRooms.Count; index++)
            {
                StageRoomProxy room = allRooms[index];
                bool visible = active == null || (includeNeighbors && IsNeighbor(active, room));
                room.SetSimulationPreview(visible, active == null);
            }
        }

        private void UpdatePhaseFromTime()
        {
            if (Phase == StageLayoutSimulationPhase.ExitReached) return;
            Phase = VirtualElapsedSeconds >= MaruVirtualSeconds ? StageLayoutSimulationPhase.MaruChase :
                VirtualElapsedSeconds >= Bell2VirtualSeconds ? StageLayoutSimulationPhase.Bell2 :
                VirtualElapsedSeconds >= Bell1VirtualSeconds ? StageLayoutSimulationPhase.Bell1 :
                StageLayoutSimulationPhase.Exploration;
        }

        private void UpdateMaruPreview()
        {
            bool chasing = Phase == StageLayoutSimulationPhase.MaruChase;
            float progress = route.Count <= 1 ? 1f : Mathf.Clamp01(currentRouteIndex / (float)(route.Count - 1));
            maruRoutePreview?.SetChasePreview(chasing, progress);
        }

        private static Vector3 GetRoomCenter(StageRoomProxy room)
        {
            return room.transform.position + new Vector3(
                room.SizeCells.x * StageRoomProxy.PreviewCellScale * 0.5f,
                room.SizeCells.y * StageRoomProxy.PreviewCellScale * 0.5f,
                0f);
        }

        private static List<StageRoomProxy> BuildMainRoute(
            IEnumerable<StageRoomProxy> rooms,
            IReadOnlyList<StageLayoutConnectionProxy> edges)
        {
            List<StageRoomProxy> roomList = rooms != null ? rooms.Where(room => room != null).ToList() : new List<StageRoomProxy>();
            StageRoomProxy start = roomList.FirstOrDefault(room => room.Role == RoomRole.Start);
            StageRoomProxy exit = roomList.FirstOrDefault(room => room.Role == RoomRole.Exit);
            if (start == null || exit == null) return roomList.Where(room => room.MainRoute).ToList();

            var previous = new Dictionary<StageRoomProxy, StageRoomProxy>();
            var visited = new HashSet<StageRoomProxy> { start };
            var queue = new Queue<StageRoomProxy>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                StageRoomProxy current = queue.Dequeue();
                if (current == exit) break;
                for (int index = 0; index < edges.Count; index++)
                {
                    StageLayoutConnectionProxy edge = edges[index];
                    if (edge == null || edge.VisualKind != StageConnectionVisualKind.MainRoute) continue;
                    StageRoomProxy next = edge.SourceRoom == current ? edge.TargetRoom : edge.TargetRoom == current ? edge.SourceRoom : null;
                    if (next == null || !visited.Add(next)) continue;
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }

            if (!visited.Contains(exit)) return roomList.Where(room => room.MainRoute).ToList();
            var result = new List<StageRoomProxy>();
            for (StageRoomProxy current = exit; current != null; current = previous.TryGetValue(current, out StageRoomProxy prior) ? prior : null)
                result.Add(current);
            result.Reverse();
            return result;
        }
    }
}

#endif
