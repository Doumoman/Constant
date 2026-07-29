using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MoonMillTemptationDoor : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private GameObject blocker;
        private bool entered;
        private StarNightChapterState chapter;

        public string Prompt
        {
            get
            {
                if (entered)
                {
                    return "달 뒤편 창고의 빛 듣기";
                }
                if (chapter != null && chapter.GateLoopEnabled)
                {
                    return chapter.GateActivated
                        ? "선택: 출항을 미루고 위험한 달 뒤편 창고 들어가기"
                        : "별문을 켜야 열리는 달 뒤편 창고";
                }
                return "연료를 모은 뒤에도 창고 열기";
            }
        }

        public void Configure(GameObject gateBlocker)
        {
            blocker = gateBlocker;
        }

        private void Start()
        {
            chapter = StarNightRunState.Ensure().Chapter;
            chapter.LoopStateChanged += OnLoopStateChanged;
            RefreshBlocker();
        }

        private void OnDestroy()
        {
            if (chapter != null)
            {
                chapter.LoopStateChanged -= OnLoopStateChanged;
            }
        }

        public void Interact(StarNightPlayerAgent player)
        {
            StarNightRunState run = StarNightRunState.Ensure();
            bool open = run.Chapter.GateLoopEnabled
                ? run.Chapter.GateActivated && run.Chapter.TemptationOpen
                : run.Chapter.DepartureReady;
            if (!open)
            {
                StarNightHUD.Instance?.Toast(run.Chapter.GateLoopEnabled
                    ? "길떡 2개를 장착한 뒤 별문 손잡이를 직접 당겨야 이 문이 열린다."
                    : "문은 아직 달배의 연료 냄새를 기다린다.");
                return;
            }
            if (entered)
            {
                StarNightHUD.Instance?.Toast("창고 안쪽에서 누군가 달을 두드린다.");
                return;
            }

            entered = true;
            run.SetFlag("moonmill.temptation.open");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.EnteredTemptationRoom,
                actorId = "Player",
                targetId = "MoonBackStorage",
                detail = "떠날 수 있었지만 달 뒤편 창고를 열었다",
                witnessed = true
            });
            run.Chapter.AddScent(15f, "떠난 뒤의 방은 별냄새를 오래 붙잡는다", "MoonBackStorage");
            StarNightHUD.Instance?.Toast("떠날 수 있는데도 문을 열었다. 방울 소리가 가까워진다.");
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

            bool shouldBlock = chapter == null ||
                               (chapter.GateLoopEnabled
                                   ? !chapter.GateActivated || !chapter.TemptationOpen
                                   : !chapter.DepartureReady);
            blocker.SetActive(shouldBlock);
        }
    }
}
