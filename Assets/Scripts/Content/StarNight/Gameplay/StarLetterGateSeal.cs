using UnityEngine;

namespace StarFetchingNight
{
    public enum StarLetterGateSealMode
    {
        CopyAddress,
        UseSeal
    }

    [DisallowMultipleComponent]
    public sealed class StarLetterGateSeal : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private StarLetterGateSealMode mode;
        [SerializeField] private FableObject letter;
        [SerializeField] private GateRouteObjective routeObjective;

        public string Prompt => mode == StarLetterGateSealMode.CopyAddress
            ? "정규 분류 지식으로 봉인을 뜯지 않고 주소만 복사하기"
            : "봉인을 훼손해 인장을 별문 주소로 즉시 사용하기";

        public void Configure(
            StarLetterGateSealMode sealMode,
            FableObject targetLetter,
            GateRouteObjective objective)
        {
            mode = sealMode;
            letter = targetLetter;
            routeObjective = objective;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (letter == null ||
                run.GetFlag("CH4_LETTER_STATE_DISMANTLED") ||
                run.GetFlag("CH4_LETTER_STATE_DELIVERED") ||
                run.GetFlag("CH4_LETTER_STATE_LOST_TO_MARU"))
            {
                StarNightHUD.Instance?.Toast("봉인 주소를 사용할 수 있는 상태가 아니다.");
                return;
            }

            GateRouteRuntimeState route =
                run.ChapterLoop.FindRoute("CH4_ROUTE_SEALED_LETTER");
            if (route == null || route.state != GateRouteState.Available)
            {
                StarNightHUD.Instance?.Toast("봉인 주소 경로는 이미 다른 선택으로 기록됐다.");
                return;
            }

            if (mode == StarLetterGateSealMode.CopyAddress &&
                !run.GetFlag("CH4_ROUTE_REGULAR_COMPLETE"))
            {
                StarNightHUD.Instance?.Toast(
                    "봉인을 보존하려면 정규 우편 분류에서 주소 격자 규칙을 먼저 배워야 한다.",
                    5f);
                return;
            }

            if (routeObjective == null || !routeObjective.Complete())
            {
                return;
            }

            if (mode == StarLetterGateSealMode.CopyAddress)
            {
                run.SetFlag("CH4_LETTER_STATE_COPIED");
                run.SetFlag("CH4_LETTER_STATE_SEALED");
                run.SetFlag("CH4_LETTER_PRESERVED");
                run.SetFlag("STARPATH_LETTER_PRESERVED");
                run.Chapter.AddScent(
                    run.ConsequenceResolver.ModifyScent(2f),
                    "봉인을 건드리지 않고 주소의 별빛만 베꼈다.",
                    letter.ObjectId);
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.LetterSealCopied,
                    actorId = "Player",
                    targetId = letter.ObjectId,
                    routeId = "CH4_ROUTE_SEALED_LETTER",
                    tool = FableVerb.Deliver,
                    detail = "라니의 마지막 편지를 뜯지 않고 붉은별 주소 인장만 복사했다.",
                    helpedResident = true,
                    witnessed = true
                });
                StarNightHUD.Instance?.Toast(
                    "주소만 복사했다. 봉인은 온전하고 편지 내용은 여전히 수신자의 것이다.",
                    6f);
                return;
            }

            run.SetFlag("CH4_LETTER_SEAL_DAMAGED");
            run.SetFlag("CH4_RANI_ARGUMENT");
            float scent = run.ConsequenceResolver.ModifyScent(8f);
            run.Chapter.AddScent(
                scent,
                "편지 봉인을 별문 주소판에 직접 눌렀다.",
                letter.ObjectId);
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.LetterSealDamaged,
                actorId = "Player",
                targetId = letter.ObjectId,
                routeId = "CH4_ROUTE_SEALED_LETTER",
                tool = FableVerb.Deliver,
                detail = "빠른 길 복구를 위해 마지막 편지의 봉인을 주소 인장으로 사용했다.",
                scentDelta = scent,
                witnessed = true
            });
            StarNightHUD.Instance?.Toast(
                "봉인 주소로 별문을 찾았다. 빠르게 조각을 얻었지만 붉은 밀랍이 갈라졌다.",
                6f);
        }
    }
}
