using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class SunflowerStoppedRoomTemptation : MonoBehaviour, IStarNightInteractable
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
                    return "멈춘 방의 첫 명령 원본 다시 보기";
                }
                return chapter != null && chapter.GateActivated
                    ? "선택: 출항을 미루고 해바라기 너머의 멈춘 방 열기"
                    : "별문을 켜야 열리는 해바라기 너머의 멈춘 방";
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
                    "길꽃 2개를 심고 별문 손잡이를 당겨야 멈춘 방의 해바라기가 열린다.");
                return;
            }
            if (entered)
            {
                StarNightHUD.Instance?.Toast(
                    "원본 명령에는 보호와 감금의 경계가 적혀 있지 않다.");
                return;
            }

            entered = true;
            run.SetFlag("CH5_STOPPED_ROOM_ENTERED");
            run.SetFlag("sun-garden.temptation.open");
            RefreshBlocker();
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.EnteredTemptationRoom,
                actorId = "Player",
                targetId = "SunflowerStoppedRoom",
                detail = "떠날 수 있었지만 해바라기 너머, 시간이 멈춘 방으로 들어갔다",
                witnessed = true
            });
            run.Chapter.AddScent(
                run.ConsequenceResolver.ModifyScent(12f),
                "열린 해바라기의 강한 광원이 마루에게 멈춘 방을 보였다",
                "SunflowerStoppedRoom");
            StarNightHUD.Instance?.Toast(
                "출항을 미루고 멈춘 방을 열었다. 강한 빛 속에서 마루의 두 번째 발자국이 가까워진다.",
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
