using UnityEngine;

namespace StarFetchingNight
{
    public enum StarLetterDecisionMode
    {
        Open,
        Dismantle,
        Preserve
    }

    [DisallowMultipleComponent]
    public sealed class StarLetterDecision : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private StarLetterDecisionMode mode;
        [SerializeField] private FableObject letter;

        public string Prompt => mode switch
        {
            StarLetterDecisionMode.Open => "라니의 마지막 편지 봉인 열기",
            StarLetterDecisionMode.Dismantle => "편지를 순간이동 코어로 분해하기",
            _ => "편지를 열지 않고 보존 표시하기"
        };

        public void Configure(StarLetterDecisionMode decisionMode, FableObject targetLetter)
        {
            mode = decisionMode;
            letter = targetLetter;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (letter == null || run.GetFlag("CH4_LETTER_STATE_DISMANTLED") ||
                run.GetFlag("CH4_LETTER_STATE_DELIVERED") ||
                run.GetFlag("CH4_LETTER_LOST_TO_MARU"))
            {
                StarNightHUD.Instance?.Toast("그 편지는 더 이상 이 보관대에 없다.");
                return;
            }

            switch (mode)
            {
                case StarLetterDecisionMode.Open:
                    OpenLetter(run);
                    break;
                case StarLetterDecisionMode.Dismantle:
                    DismantleLetter(run);
                    break;
                default:
                    PreserveLetter(run);
                    break;
            }
        }

        private void OpenLetter(StarNightRunState run)
        {
            if (run.GetFlag("CH4_LETTER_STATE_OPENED"))
            {
                StarNightHUD.Instance?.Toast("이미 열린 편지다.");
                return;
            }

            run.SetFlag("CH4_LETTER_STATE_OPENED");
            run.SetFlag("CH4_RANI_ARGUMENT");
            run.SetFlag("STARPATH_LETTER_CONTENT_KNOWN");
            run.SetFlag("STARPATH_ROUTE_CLUE");
            run.SetFlag("STARPATH_RANI_CAN_BE_DELIVERED");
            float scent = run.ConsequenceResolver.ModifyScent(12f);
            run.Chapter.AddScent(scent, "봉인된 기억을 열어 보았다", letter.ObjectId);
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.LetterOpened,
                actorId = "Player",
                targetId = letter.ObjectId,
                detail = "수신자가 라니인 마지막 편지를 열어 내용을 읽었다",
                scentDelta = scent,
                witnessed = true
            });
            StarNightHUD.Instance?.Toast(
                "“언니, 무서운 것과 가고 싶지 않은 건 다른 거잖아.”\n라니의 통신 잡음이 거칠어졌다.", 8f);
        }

        private void DismantleLetter(StarNightRunState run)
        {
            run.SetFlag("CH4_LETTER_STATE_DISMANTLED");
            run.SetFlag("CH4_TELEPORT_CORE");
            run.SetFlag("STARPATH_LETTER_DESTROYED");
            run.SetFlag("STARPATH_ROUTE_DIFFICULT");
            float scent = run.ConsequenceResolver.ModifyScent(18f);
            run.Chapter.AddScent(scent, "편지의 귀환 주소를 순간이동 코어로 뜯어냈다", letter.ObjectId);
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.LetterDismantled,
                actorId = "Player",
                targetId = letter.ObjectId,
                detail = "라니의 마지막 편지를 분해해 강한 순간이동 코어를 얻었다",
                scentDelta = scent,
                causedAccident = true,
                witnessed = true
            });
            letter.SetStored(true);
            StarNightHUD.Instance?.Toast("되돌아오는 주소 코어 획득. 편지 내용과 별길의 한 조각은 사라졌다.", 7f);
        }

        private void PreserveLetter(StarNightRunState run)
        {
            if (run.GetFlag("CH4_LETTER_PRESERVED"))
            {
                return;
            }

            run.SetFlag("CH4_LETTER_PRESERVED");
            run.SetFlag("CH4_LETTER_STATE_SEALED");
            run.SetFlag("STARPATH_LETTER_PRESERVED");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.LetterPreserved,
                actorId = "Player",
                targetId = letter.ObjectId,
                detail = "내용을 읽지 않고 마지막 편지의 봉인을 보존했다",
                helpedResident = true,
                witnessed = true
            });
            StarNightHUD.Instance?.Toast("봉인은 그대로다. 편지는 주민 물건 슬롯에 넣거나 라니에게 배송할 수 있다.", 6f);
        }
    }
}
