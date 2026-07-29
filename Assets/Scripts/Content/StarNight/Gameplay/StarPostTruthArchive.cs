using UnityEngine;

namespace StarFetchingNight
{
    public enum StarPostTruthArchiveMode
    {
        CommandFragment,
        FullContext
    }

    [DisallowMultipleComponent]
    public sealed class StarPostTruthArchive : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private StarPostTruthArchiveMode mode;
        private bool read;

        public string Prompt => read
            ? "읽은 통신 기록 다시 보기"
            : mode == StarPostTruthArchiveMode.CommandFragment
                ? "메인 통신 기록 읽기"
                : "선택: 라니 명령의 전체 맥락 읽기";

        public void Configure(StarPostTruthArchiveMode archiveMode)
        {
            mode = archiveMode;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (mode == StarPostTruthArchiveMode.FullContext &&
                (!run.Chapter.GateActivated ||
                 !run.GetFlag("CH4_RETURN_VAULT_OPENED")))
            {
                StarNightHUD.Instance?.Toast(
                    "전체 통신은 별문 가동 후 반송 불가 수취 보관실에서만 복원된다.");
                return;
            }

            if (mode == StarPostTruthArchiveMode.CommandFragment)
            {
                if (!read)
                {
                    read = true;
                    run.SetFlag("CH4_RANI_COMMAND_FRAGMENT_READ");
                    run.Actions.Record(new StarActionContext
                    {
                        actionType = StarActionType.ObjectInspected,
                        actorId = "Player",
                        targetId = "RaniCommandFragment",
                        detail =
                            "메인 기록에서 라니의 명령 일부를 확인했다: 떠난 아이들을 모두 집으로.",
                        witnessed = true
                    });
                }

                StarNightHUD.Instance?.Toast(
                    "라니의 과거 명령: “떠난 아이들을 모두 집으로 데려와.”",
                    7f);
                return;
            }

            if (!read)
            {
                read = true;
                run.SetFlag("CH4_RANI_COMMAND_CONTEXT_READ");
                run.SetFlag("STARPATH_RANI_COMMAND_CONTEXT_KNOWN");
                float scent = run.ConsequenceResolver.ModifyScent(6f);
                run.Chapter.AddScent(
                    scent,
                    "동생이 사라진 밤의 통신 원본을 재생했다.",
                    "RaniCommandContext");
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.ObjectInspected,
                    actorId = "Player",
                    targetId = "RaniCommandContext",
                    detail =
                        "라니가 동생 실종 직후 마루에게 그 명령을 내렸다는 전체 맥락을 확인했다.",
                    scentDelta = scent,
                    witnessed = true
                });
            }

            StarNightHUD.Instance?.Toast(
                "전체 기록: 라니는 동생이 사라진 직후, 공포와 거부를 구분하지 못한 채 마루에게 모두 데려오라고 명령했다.",
                8f);
        }
    }
}
