using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class GardenRestorationAltar : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private bool used;

        public string Prompt => used
            ? "되살아난 정원의 온기 살피기"
            : "희귀 햇빛 씨앗을 포기해 과열된 정원 진화시키기";

        public void Interact(StarNightPlayerAgent player)
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (used)
            {
                StarNightHUD.Instance?.Toast("빛은 한곳에 머물지 않고 정원 전체로 천천히 흐른다.");
                return;
            }
            if (!run.Heat.Overheated && !run.GetFlag("CH5_GARDEN_OVERHEATED") &&
                !run.GetFlag("CH5_GARDEN_FIRE"))
            {
                StarNightHUD.Instance?.Toast("정원은 아직 희귀 씨앗을 포기해야 할 만큼 타지 않았다.");
                return;
            }
            if (!run.SunSeeds.ConsumeCharge(true))
            {
                StarNightHUD.Instance?.Toast("해바라기 뿌리방에 숨은 희귀 햇빛 씨앗이 필요하다.");
                return;
            }

            used = true;
            run.Heat.RestoreGarden(58f,
                "출항에 쓸 수 있던 희귀 햇빛 씨앗을 포기해 정원 전체를 새로운 내열성 생명으로 되살렸다");
            foreach (SunGrowthState growth in FindObjectsByType<SunGrowthState>(FindObjectsSortMode.None))
            {
                if (growth.Stage == SunGrowthStage.Burned &&
                    growth.Kind != SunGrowthKind.StarPathTree)
                {
                    growth.SetStoryStage(SunGrowthStage.Blooming, 2);
                }
            }
            StarNightHUD.Instance?.Toast(
                "희귀 씨앗이 사라지며 정원이 다시 숨을 쉰다. 타 버린 식물은 이전과 다른 모습으로 피었다.", 7f);
        }
    }
}
