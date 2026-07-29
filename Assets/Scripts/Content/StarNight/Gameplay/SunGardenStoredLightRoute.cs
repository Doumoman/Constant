using System.Collections.Generic;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class SunGardenStoredLightRoute : MonoBehaviour
    {
        [SerializeField] private GateRouteObjective routeObjective;
        [SerializeField, Min(1)] private int requiredSources = 3;
        [SerializeField] private List<string> collectedSourceIds = new();

        public int CollectedCount => collectedSourceIds.Count;
        public int RequiredSources => requiredSources;
        public bool Completed => routeObjective != null && routeObjective.Completed;

        public void Configure(GateRouteObjective objective, int required = 3)
        {
            routeObjective = objective;
            requiredSources = Mathf.Max(1, required);
        }

        public bool RegisterCollection(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId) ||
                collectedSourceIds.Contains(sourceId) ||
                Completed)
            {
                return false;
            }

            collectedSourceIds.Add(sourceId);
            StarNightRunState run = StarNightRunState.Ensure();
            run.SetFlag($"CH5_STORED_LIGHT_{sourceId}");
            if (!run.Chapter.GateLoopEnabled || collectedSourceIds.Count < requiredSources)
            {
                return false;
            }

            bool completed = routeObjective != null && routeObjective.Complete();
            if (!completed)
            {
                return false;
            }

            run.SetFlag("CH5_ROUTE_STORED_SUNLIGHT_COMPLETE");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.SunlightCollected,
                actorId = "Player",
                targetId = "StoredSunlightRoute",
                routeId = routeObjective.RouteId,
                detail = "서로 다른 저장 햇빛 세 곳을 고르게 모아 안정적인 길꽃 빛으로 만들었다",
                helpedResident = true,
                witnessed = true
            });
            StarNightHUD.Instance?.Toast(
                "저장 햇빛 3/3 · 고른 빛을 얻었다. 별문 길꽃에 직접 심을 수 있다.", 5f);
            return true;
        }
    }
}
