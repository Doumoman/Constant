using System.Linq;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MoonMillRepairStation : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private bool repaired;
        public string Prompt => repaired ? "고쳐진 방앗간 살펴보기" : "작은 톱니로 방앗간 고치기";

        public void Interact(StarNightPlayerAgent player)
        {
            if (repaired)
            {
                StarNightHUD.Instance?.Toast("방앗간이 달빛을 고르게 빻고 있다.");
                return;
            }

            FableObject gear = player.Inventory.PeekFirstMatching(item => item.ObjectId.Contains("gear"));
            if (gear == null)
            {
                StarNightHUD.Instance?.Toast("맞물릴 만큼 작은 톱니가 필요하다. 절구의 말을 빌려 보자.");
                return;
            }

            if (!gear.Modifications.Contains(FableModification.Small))
            {
                StarNightHUD.Instance?.Toast("이 톱니는 아직 너무 크다.");
                return;
            }

            gear = player.Inventory.TakeFirstMatching(item => item == gear);
            repaired = true;
            gear.gameObject.SetActive(false);
            StarNightRunState run = StarNightRunState.Ensure();
            run.SetFlag("moonmill.mill.repaired");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.DamageRepaired,
                actorId = "Player",
                targetId = "MoonMill",
                detail = "빌린 도구로 방앗간을 다시 움직였다",
                helpedResident = true,
                witnessed = true
            });
            run.Chapter.AddScent(-8f, "고른 달가루가 냄새를 덮었다", "MoonMill");
            StarNightHUD.Instance?.Toast(run.Chapter.GateLoopEnabled
                ? "쿵— 방앗간이 다시 숨을 쉰다. 오른쪽 길떡 틀에서 새 길떡을 빚을 수 있다."
                : "쿵— 방앗간이 다시 숨을 쉰다. 겨울 달떡도 연료가 된다.");
        }
    }
}
