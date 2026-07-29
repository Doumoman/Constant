using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MoonMillWinterStorage : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private GateRouteObjective objective;

        public string Prompt
        {
            get
            {
                GateRouteRuntimeState route = CurrentRoute;
                return route?.state switch
                {
                    GateRouteState.Complete => "빌린 저장 길떡 돌려놓기",
                    GateRouteState.Contributed => "별문에 사용한 저장 길떡 기록 확인",
                    _ => "겨울 저장고 경고를 읽고 길떡 빌리기"
                };
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
            if (route == null)
            {
                StarNightHUD.Instance?.Toast("저장고 장부가 아직 별문과 연결되지 않았다.");
                return;
            }

            if (route.state == GateRouteState.Contributed)
            {
                StarNightHUD.Instance?.Toast(
                    "저장 길떡은 이미 별문에 장착되어 돌려놓을 수 없다. 겨울 식량 사용이 기록됐다.", 4.5f);
                return;
            }

            if (route.state == GateRouteState.Complete)
            {
                if (objective == null || !objective.ReturnContribution(
                        "겨울 저장고의 길떡을 별문에 넣기 전에 돌려놓았다"))
                {
                    StarNightHUD.Instance?.Toast("저장 길떡을 지금은 돌려놓을 수 없다.");
                    return;
                }

                run.AddCounter("CH1_STORAGE_CAKE_RETURNED");
                run.SetFlag("CH1_STORAGE_CAKE_RETURNED");
                run.SetFlag("CH1_STORAGE_CAKE_TAKEN_ACTIVE", false);
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.ObjectReturned,
                    actorId = "Player",
                    targetId = "CH1_PATH_CAKE_STORAGE",
                    routeId = "CH1_ROUTE_STORAGE",
                    detail = "겨울 저장 길떡을 장착 전에 돌려놓았다",
                    helpedResident = true,
                    witnessed = true
                });
                StarNightHUD.Instance?.Toast("저장 길떡을 돌려놓았다. 다른 경로를 선택할 수 있다.", 4f);
                return;
            }

            if (objective == null || !objective.Complete())
            {
                StarNightHUD.Instance?.Toast("저장고 문이 아직 열리지 않는다.");
                return;
            }

            run.AddCounter("CH1_STORAGE_CAKE_TAKEN");
            run.SetFlag("CH1_STORAGE_CAKE_TAKEN_ACTIVE");
            run.SetFlag("CH1_STORAGE_WARNING_HEARD");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.NpcWarningHeard,
                actorId = "WinterStorageLedger",
                targetId = "Player",
                routeId = "CH1_ROUTE_STORAGE",
                detail = "겨울 식량은 별문에 필요한 양보다 넉넉하지 않다는 경고를 읽었다",
                witnessed = true
            });
            run.Chapter.AddScent(1f, "차가운 저장 길떡의 향이 새어 나왔다", "CH1_PATH_CAKE_STORAGE");
            StarNightHUD.Instance?.Toast(
                "저장 길떡을 빌렸다. 가장 빠르지만 별문에 넣으면 겨울 식량 사용이 확정된다.", 5f);
        }

        private GateRouteRuntimeState CurrentRoute =>
            StarNightRunState.Instance?.ChapterLoop.FindRoute("CH1_ROUTE_STORAGE");
    }
}
