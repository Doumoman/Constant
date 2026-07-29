using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MoonMillPathCakePress : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private GateRouteObjective objective;

        public string Prompt
        {
            get
            {
                GateRouteRuntimeState route = CurrentRoute;
                if (route?.state == GateRouteState.Contributed)
                {
                    return "별문에 장착한 새 길떡 확인";
                }
                if (route?.state == GateRouteState.Complete)
                {
                    return "빚어 둔 새 길떡 확인";
                }
                return StarNightRunState.Instance?.GetFlag("moonmill.mill.repaired") == true
                    ? "고친 방앗간으로 새 길떡 빚기"
                    : "멈춘 길떡 틀 살펴보기";
            }
        }

        public void Configure(GateRouteObjective routeObjective)
        {
            objective = routeObjective;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            StarNightRunState run = StarNightRunState.Ensure();
            GateRouteRuntimeState route = CurrentRoute;
            if (route?.state == GateRouteState.Contributed)
            {
                StarNightHUD.Instance?.Toast("새 길떡은 이미 별문 한쪽을 밝히고 있다.");
                return;
            }
            if (route?.state == GateRouteState.Complete)
            {
                StarNightHUD.Instance?.Toast("새 길떡을 별문에 가져갈 준비가 끝났다.");
                return;
            }
            if (!run.GetFlag("moonmill.mill.repaired"))
            {
                StarNightHUD.Instance?.Toast("먼저 작은 톱니로 방앗간을 수리해야 한다.");
                return;
            }
            if (objective == null || !objective.Complete())
            {
                StarNightHUD.Instance?.Toast("길떡 틀이 아직 반죽을 받아들이지 않는다.");
                return;
            }

            run.SetFlag("CH1_ROUTE_MILL_COMPLETE");
            run.Chapter.AddScent(2f, "새 길떡의 따뜻한 냄새가 별문까지 번졌다", "CH1_PATH_CAKE_MILL");
            StarNightHUD.Instance?.Toast(
                "새 길떡을 빚었다. 일반 가방이 아니라 별문 기여 칸에 보관된다.", 4.5f);
        }

        private GateRouteRuntimeState CurrentRoute =>
            StarNightRunState.Instance?.ChapterLoop.FindRoute("CH1_ROUTE_MILL");
    }
}
