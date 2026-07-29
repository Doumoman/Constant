using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MagpieStarLadderTemptation : MonoBehaviour, IStarNightInteractable
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
                    return "별사다리에서 팽팽해진 붉은 실 듣기";
                }
                return chapter != null && chapter.GateActivated
                    ? "선택: 출항을 미루고 위험한 까마득한 별사다리 오르기"
                    : "별문을 켜야 드러나는 까마득한 별사다리";
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
                    "닻 2개를 장착하고 별문 손잡이를 당겨야 숨은 별사다리가 드러난다.");
                return;
            }
            if (entered)
            {
                StarNightHUD.Instance?.Toast("모든 붉은 실이 같은 방향으로 팽팽해져 있다.");
                return;
            }

            entered = true;
            run.SetFlag("magpie.temptation.open");
            RefreshBlocker();
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.EnteredTemptationRoom,
                actorId = "Player",
                targetId = "EndlessStarLadder",
                detail = "떠날 수 있었지만 까마득한 별사다리를 올랐다",
                witnessed = true
            });
            run.Chapter.AddScent(12f, "높은 별빛과 끊어진 실이 마루에게 길을 보였다", "EndlessStarLadder");
            StarNightHUD.Instance?.Toast(
                "떠날 수 있는데도 별사다리를 열었다. 팽팽해진 실 끝에서 첫 발자국이 흔들린다.", 5f);
        }

        private void OnLoopStateChanged(ChapterLoopState state)
        {
            RefreshBlocker();
        }

        private void RefreshBlocker()
        {
            if (blocker != null)
            {
                bool unlockedByGate = chapter != null &&
                                      (chapter.GateLoopEnabled
                                          ? chapter.GateActivated && chapter.TemptationOpen
                                          : chapter.DepartureReady);
                blocker.SetActive(!unlockedByGate || !entered);
            }
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
