#if LEGACY_DISABLED
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Stage.Layout.Authoring
{
    [DisallowMultipleComponent]
    public sealed class StageMaruRoutePreview : MonoBehaviour
    {
        [SerializeField] private LineRenderer routeLine;
        [SerializeField] private Transform maruMarker;
        [SerializeField] private List<StageRoomProxy> route = new List<StageRoomProxy>();

        public int RouteRoomCount => route.Count;
        public bool IsVisible => routeLine != null && routeLine.enabled;

        public void Configure(LineRenderer line, Transform marker, IReadOnlyList<StageRoomProxy> mainRoute)
        {
            routeLine = line;
            maruMarker = marker;
            route.Clear();
            if (mainRoute != null)
            {
                for (int index = mainRoute.Count - 1; index >= 0; index--)
                    if (mainRoute[index] != null) route.Add(mainRoute[index]);
            }

            if (routeLine != null)
            {
                routeLine.positionCount = route.Count;
                for (int index = 0; index < route.Count; index++)
                    routeLine.SetPosition(index, GetCenter(route[index]) + Vector3.back * 0.18f);
            }
            SetChasePreview(false, 0f);
        }

        public void SetChasePreview(bool visible, float normalizedProgress)
        {
            if (routeLine != null) routeLine.enabled = visible;
            if (maruMarker == null) return;
            maruMarker.gameObject.SetActive(visible && route.Count > 0);
            if (!visible || route.Count == 0) return;

            float routePosition = Mathf.Clamp01(normalizedProgress) * Mathf.Max(0, route.Count - 1);
            int from = Mathf.FloorToInt(routePosition);
            int to = Mathf.Min(from + 1, route.Count - 1);
            maruMarker.position = Vector3.Lerp(GetCenter(route[from]), GetCenter(route[to]), routePosition - from) + Vector3.back * 0.2f;
        }

        private static Vector3 GetCenter(StageRoomProxy room)
        {
            return room.transform.position + new Vector3(
                room.SizeCells.x * StageRoomProxy.PreviewCellScale * 0.5f,
                room.SizeCells.y * StageRoomProxy.PreviewCellScale * 0.5f,
                0f);
        }
    }
}

#endif
