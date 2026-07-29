using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class CloudBottleUpgrade : MonoBehaviour, IStarNightInteractable
    {
        private bool claimed;
        public string Prompt => claimed ? "큰 구름병은 이미 비어 있다" : "무지개 구름을 큰 병에 담기";

        public void Interact(StarNightPlayerAgent player)
        {
            if (claimed)
            {
                return;
            }

            StarNightRunState run = StarNightRunState.Ensure();
            if (run.Chapter.GateLoopEnabled &&
                !run.GetFlag("CH3_RAINBOW_RANCH_ENTERED"))
            {
                StarNightHUD.Instance?.Toast(
                    "무지개 목장 입구에서 출항을 미룰지 먼저 결정해야 한다.");
                return;
            }

            claimed = true;
            run.CloudBottle.AddCapacity(2);
            run.SetFlag("CH3_RAINBOW_BOTTLE");
            float scent = run.ConsequenceResolver.ModifyScent(13f);
            run.Chapter.AddScent(scent, "무지개 위의 냄새가 목장 전체로 퍼졌다", "RainbowBottle");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.ToolApplied,
                actorId = "Player",
                targetId = "RainbowBottle",
                tool = FableVerb.Float,
                detail = "구름병 용량을 2만큼 늘렸다",
                scentDelta = scent
            });
            gameObject.SetActive(false);
            StarNightHUD.Instance?.Toast("큰 구름병 · 용량 +2. 높은 곳의 별냄새가 빠르게 퍼진다.", 5f);
        }
    }
}
