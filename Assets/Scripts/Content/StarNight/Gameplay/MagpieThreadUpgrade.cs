using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MagpieThreadUpgrade : MonoBehaviour, IStarNightInteractable
    {
        private bool collected;
        public string Prompt => collected ? "별사다리의 빈 매듭 살펴보기" : "끊어지지 않는 매듭 가져가기";

        public void Interact(StarNightPlayerAgent player)
        {
            if (collected)
            {
                StarNightHUD.Instance?.Toast("빈 매듭이 방울처럼 흔들린다.");
                return;
            }

            StarNightRunState run = StarNightRunState.Ensure();
            if (run.Chapter.GateLoopEnabled &&
                (!run.Chapter.GateActivated || !run.GetFlag("magpie.temptation.open")))
            {
                StarNightHUD.Instance?.Toast(
                    "별문을 켠 뒤 별사다리 입구에서 위험한 탐험을 직접 선택해야 한다.");
                return;
            }

            collected = true;
            int bonus = run.RedThread.AddConnectionCapacity(1);
            run.SetFlag("CH2_THREAD_LIMIT_UPGRADED");
            run.SetFlag("magpie.temptation.resolved");
            run.Chapter.AddScent(15f, "까마득한 별사다리의 희귀 매듭을 풀었다", "ThreadUpgrade");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.ThreadCapacityUpgraded,
                actorId = "Player",
                targetId = "EndlessKnot",
                detail = $"붉은 실 연결 한도가 {run.RedThread.ConnectionLimit}개로 늘었다",
                witnessed = true
            });
            StarNightHUD.Instance?.Toast($"끊어지지 않는 매듭을 배웠다. 추가 연결 +{bonus}, 하지만 방울 소리가 가까워졌다.", 5f);
        }
    }
}
