using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MoonRabbitWitness : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private string npcId = "Rabbit_Miller";
        [SerializeField, TextArea] private string calmLine = "달떡은 세 개면 돼. 더 크게 만들 필요는 없단다.";
        [SerializeField, TextArea] private string alarmLine = "그 냄새… 마루가 길을 찾겠어. 이제 그만 떠나렴.";
        public string Prompt => "달토끼와 이야기하기";

        public void Interact(StarNightPlayerAgent player)
        {
            StarNightRunState run = StarNightRunState.Ensure();
            bool alarmed = run.Chapter.ScentStage >= StarScentStage.Footprints;
            string line = run.Chapter.GateLoopEnabled && !alarmed
                ? "꺼진 별문에는 길떡 두 개면 돼. 방앗간, 달광산, 겨울 저장고 중 두 곳을 골라 보렴."
                : alarmed ? alarmLine : calmLine;
            StarNightHUD.Instance?.Toast(line, 4f);
            if (alarmed)
            {
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.NpcWarningHeard,
                    actorId = npcId,
                    targetId = "Player",
                    detail = "달토끼의 귀가 방울 소리를 먼저 들었다",
                    witnessed = true
                });
            }
        }
    }
}
