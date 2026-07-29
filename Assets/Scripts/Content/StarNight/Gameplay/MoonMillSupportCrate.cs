using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MoonMillSupportCrate : MonoBehaviour, IStarNightInteractable
    {
        private bool opened;
        public string Prompt => opened ? "빈 달토끼 물류상자 살펴보기" : "이전 챕터의 물류상자 열기";

        public void Interact(StarNightPlayerAgent player)
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (opened)
            {
                StarNightHUD.Instance?.Toast("상자 바닥에 '방앗간이 돌아가면 길도 돌아간다'고 적혀 있다.");
                return;
            }
            if (!run.GetFlag("CH1_MILL_REPAIRED"))
            {
                StarNightHUD.Instance?.Toast("상자는 비어 있다. 방앗간에서 물류가 출발하지 못했다.");
                return;
            }

            opened = true;
            run.RedThread.Reinforce(1.35f);
            run.SetFlag("CH2_MOONMILL_SUPPORT_USED");
            run.Chapter.AddScent(-5f, "달가루가 붉은 실의 별냄새를 덮었다", "MoonMillSupport");
            StarNightHUD.Instance?.Toast("달토끼의 매듭가루를 발랐다. 이번 챕터의 붉은 실이 35% 더 버틴다.", 5f);
        }
    }
}
