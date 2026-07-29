using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class CloudRainbowRanchTemptation : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private GameObject blocker;
        private StarNightChapterState chapter;
        private bool entered;

        public string Prompt
        {
            get
            {
                if (entered)
                {
                    return "무지개 목장의 먼 별 낙서 다시 보기";
                }

                return chapter != null && chapter.GateActivated
                    ? "선택: 출항을 미루고 무지개 위쪽 목장 오르기"
                    : "별문을 켜야 드러나는 무지개 위쪽 목장";
            }
        }

        public void Configure(GameObject gateBlocker)
        {
            blocker = gateBlocker;
        }

        private void Start()
        {
            BindForCurrentChapter();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void BindForCurrentChapter()
        {
            Unbind();
            chapter = StarNightRunState.Ensure().Chapter;
            chapter.LoopStateChanged += OnLoopStateChanged;
            RefreshBlocker();
        }

        public void Interact(StarNightPlayerAgent player)
        {
            StarNightRunState run = StarNightRunState.Ensure();
            bool open = run.Chapter.GateLoopEnabled
                ? run.Chapter.GateActivated && run.Chapter.TemptationOpen
                : run.Chapter.DepartureReady;
            if (!open)
            {
                StarNightHUD.Instance?.Toast(
                    "바람 2개를 장착하고 별문 손잡이를 당겨야 무지개 목장이 드러난다.");
                return;
            }
            if (entered)
            {
                StarNightHUD.Instance?.Toast(
                    "긁힌 낙서 아래에 ‘다음에는 더 먼 별에 가자’가 남아 있다.");
                return;
            }

            entered = true;
            run.SetFlag("CH3_RAINBOW_RANCH_ENTERED");
            run.SetFlag("cloudranch.temptation.open");
            RefreshBlocker();
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.EnteredTemptationRoom,
                actorId = "Player",
                targetId = "RainbowUpperRanch",
                detail = "떠날 수 있었지만 무지개 위쪽 목장으로 올라갔다",
                witnessed = true
            });
            run.Chapter.AddScent(
                run.ConsequenceResolver.ModifyScent(12f),
                "높은 무지개 바람이 마루에게 목장의 위치를 알렸다",
                "RainbowUpperRanch");
            StarNightHUD.Instance?.Toast(
                "출항을 미루고 무지개 목장을 열었다. 높은 바람 속에서 두 번째 발자국이 흔들린다.",
                5f);
        }

        private void OnLoopStateChanged(ChapterLoopState state)
        {
            RefreshBlocker();
        }

        private void RefreshBlocker()
        {
            if (blocker == null)
            {
                return;
            }

            bool unlockedByGate = chapter != null &&
                                  (chapter.GateLoopEnabled
                                      ? chapter.GateActivated && chapter.TemptationOpen
                                      : chapter.DepartureReady);
            blocker.SetActive(!unlockedByGate || !entered);
        }

        private void Unbind()
        {
            if (chapter != null)
            {
                chapter.LoopStateChanged -= OnLoopStateChanged;
            }
            chapter = null;
        }
    }
}
