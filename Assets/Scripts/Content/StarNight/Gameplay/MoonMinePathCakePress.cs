using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MoonMinePathCakePress : MonoBehaviour, IStarNightInteractable
    {
        public const string OreId = "moon_stardust_ore";

        [SerializeField] private GateRouteObjective objective;

        public string Prompt
        {
            get
            {
                GateRouteRuntimeState route = CurrentRoute;
                if (route?.state == GateRouteState.Contributed)
                {
                    return "별문에 장착한 광산 길떡 확인";
                }
                if (route?.state == GateRouteState.Complete)
                {
                    return "완성된 광산 길떡 확인";
                }
                return "별가루 광석을 광산 길떡으로 뭉치기";
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
                StarNightHUD.Instance?.Toast("광산 길떡은 이미 별문 한쪽을 밝히고 있다.");
                return;
            }
            if (route?.state == GateRouteState.Complete)
            {
                StarNightHUD.Instance?.Toast("광산 길떡을 별문에 가져갈 준비가 끝났다.");
                return;
            }
            if (player == null)
            {
                return;
            }

            FableObject ore = player.Inventory.PeekFirstMatching(item => item.ObjectId == OreId);
            if (ore == null)
            {
                StarNightHUD.Instance?.Toast("결정 지하의 깊은 곁방에서 별가루 광석을 찾아야 한다.");
                return;
            }
            if (objective == null || !objective.Complete())
            {
                StarNightHUD.Instance?.Toast("광산 길떡 틀이 아직 작동하지 않는다.");
                return;
            }

            ore = player.Inventory.TakeFirstMatching(item => item == ore);
            ore.gameObject.SetActive(false);
            run.SetFlag("CH1_ROUTE_MINE_COMPLETE");
            run.SetFlag("CH1_MINE_ORE_USED");
            run.Chapter.AddScent(6f, "별가루 광석을 뭉친 빛이 결정 지하를 울렸다", OreId);
            StarNightHUD.Instance?.Toast(
                "별가루 광석을 광산 길떡으로 뭉쳤다. 별문 기여 칸에 보관된다.", 4.5f);
        }

        private GateRouteRuntimeState CurrentRoute =>
            StarNightRunState.Instance?.ChapterLoop.FindRoute("CH1_ROUTE_MINE");
    }
}
