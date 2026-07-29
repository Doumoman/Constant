using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarGateController : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private ChapterLoopDirector director;

        public string Prompt
        {
            get
            {
                ChapterLoopDirector loop = ResolveDirector();
                if (loop == null || !loop.Enabled)
                {
                    return "아직 연결되지 않은 별문";
                }
                if (loop.State == ChapterLoopState.GateReady)
                {
                    return "별문 손잡이 당기기";
                }
                if (StarNightRunState.Instance.Chapter.GateActivated)
                {
                    return "가동 중인 별문 살펴보기";
                }
                if (StarNightRunState.Instance.GateContributions.Count > 0)
                {
                    return "별문 기여 물건 장착";
                }
                return $"별문 기여 {StarNightRunState.Instance.Chapter.GateContributions}/" +
                       $"{StarNightRunState.Instance.Chapter.GateRequired}";
            }
        }

        public void Configure(ChapterLoopDirector loop)
        {
            director = loop;
        }

        public bool TryContribute(string routeId)
        {
            return ResolveDirector()?.TryContribute(routeId) == true;
        }

        public bool TryActivate()
        {
            return ResolveDirector()?.TryActivateGate() == true;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            ChapterLoopDirector loop = ResolveDirector();
            StarNightRunState run = StarNightRunState.Instance;
            if (loop == null || run == null || !loop.Enabled)
            {
                StarNightHUD.Instance?.Toast("이 별문은 아직 여행 티켓에 연결되지 않았다.");
                return;
            }

            if (loop.State == ChapterLoopState.GateReady)
            {
                StarNightHUD.Instance?.Toast(loop.TryActivateGate()
                    ? "손잡이가 첫 방울을 울렸다. 지금 출항하거나, 추격을 감수하고 선택 창고에 남을 수 있다."
                    : "별문 손잡이가 움직이지 않는다.");
                return;
            }

            if (run.Chapter.GateActivated)
            {
                StarNightHUD.Instance?.Toast("별문은 이미 켜져 있다.");
                return;
            }

            if (run.GateContributions.Count > 0)
            {
                GateContribution contribution = run.GateContributions.Pending[0];
                bool contributed = loop.TryContribute(contribution.routeId);
                StarNightHUD.Instance?.Toast(!contributed
                    ? "이 기여 물건은 지금 장착할 수 없다."
                    : run.Chapter.GateReady
                        ? $"{contribution.displayName} 장착 · 2/2는 준비 상태다. 아직 출항할 수 없다. 다시 상호작용해 손잡이를 당기면 첫 방울과 추격이 시작된다."
                        : $"{contribution.displayName}을 별문에 장착했다 · 한 경로가 더 필요하다.",
                    run.Chapter.GateReady ? 6f : 3.2f);
                return;
            }

            StarNightHUD.Instance?.Toast(
                $"별문에 넣을 기여 물건이 없다. {run.Chapter.GateContributions}/{run.Chapter.GateRequired}");
        }

        private ChapterLoopDirector ResolveDirector()
        {
            if (director == null && StarNightRunState.Instance != null)
            {
                director = StarNightRunState.Instance.ChapterLoop;
            }
            return director;
        }
    }
}
