using UnityEngine;

namespace StarFetchingNight
{
    public enum HaechiDecisionMode
    {
        LockDepartureDoor,
        LeaveDepartureOpen
    }

    [DisallowMultipleComponent]
    public sealed class MagpieHaechiDecision : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private HaechiDecisionMode mode;
        public string Prompt => mode == HaechiDecisionMode.LockDepartureDoor
            ? "해치의 출항문 잠그기"
            : "해치가 선택하도록 출항문 열어 두기";

        public void Configure(HaechiDecisionMode value)
        {
            mode = value;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (run.GetFlag("CH2_HAECHI_RESOLVED"))
            {
                StarNightHUD.Instance?.Toast("해치에 대한 행동은 이미 다리의 기억이 되었다.");
                return;
            }

            run.SetFlag("CH2_HAECHI_RESOLVED");
            if (mode == HaechiDecisionMode.LockDepartureDoor)
            {
                run.SetFlag("CH2_HAECHI_FORCED");
                run.SetNpcState("Haechi", StarNpcState.Dependent);
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.NpcForcedReturn,
                    actorId = "Player",
                    targetId = "Haechi",
                    detail = "해치가 떠나지 못하도록 출항문을 잠갔다",
                    witnessed = true
                });
                StarNightHUD.Instance?.Toast("출항문을 잠갔다. 라니는 이것을 '책임 있는 보호'라고 기록할 것이다.", 5f);
            }
            else
            {
                run.SetFlag("CH2_HAECHI_ALLOWED");
                run.SetNpcState("Haechi", StarNpcState.Autonomous);
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.NpcAllowedChoice,
                    actorId = "Player",
                    targetId = "Haechi",
                    detail = "위험을 설명한 뒤 해치가 스스로 고르게 문을 열어 두었다",
                    witnessed = true
                });
                StarNightHUD.Instance?.Toast("출항문을 열어 두었다. 해치는 떠날지 남을지 스스로 정한다.", 5f);
            }
        }
    }
}
