using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MagpieCallBell : MonoBehaviour, IStarNightInteractable
    {
        private int rings;
        public string Prompt => rings >= 3 ? "지친 까치들을 쉬게 두기" : $"긴급 방울 울리기 {rings}/3";

        public void Interact(StarNightPlayerAgent player)
        {
            if (rings >= 3)
            {
                StarNightHUD.Instance?.Toast("까치들은 이미 숨을 고르고 있다.");
                return;
            }

            rings++;
            foreach (MagpieBridgeAnchor anchor in FindObjectsByType<MagpieBridgeAnchor>(FindObjectsSortMode.None))
            {
                anchor.AssistPull(8f + rings * 3f);
            }

            StarNightRunState run = StarNightRunState.Ensure();
            run.Chapter.AddScent(3f, "긴급 방울이 은하수 위로 울렸다", "MagpieBell");
            if (rings >= 3)
            {
                run.SetFlag("CH2_MAGPIES_FORCED");
                run.SetNpcState("MagpieWorkers", StarNpcState.Tired);
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.MagpiesForced,
                    actorId = "Player",
                    targetId = "MagpieWorkers",
                    detail = "긴급 방울을 반복해 쉬던 까치들을 불러냈다",
                    causedAccident = false,
                    witnessed = true
                });
                StarNightHUD.Instance?.Toast("까치들이 세 매듭을 세게 당겼다. 다리는 빨리 움직이지만 모두 지쳤다.", 5f);
            }
            else
            {
                StarNightHUD.Instance?.Toast("까치들이 잠깐 실을 당겨 주었다.");
            }
        }
    }
}
