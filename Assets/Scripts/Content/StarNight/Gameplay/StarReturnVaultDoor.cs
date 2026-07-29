using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarReturnVaultDoor : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private GameObject barrier;
        private bool opened;
        public string Prompt
        {
            get
            {
                if (opened)
                {
                    return "반송 불가 심층 보관소가 열려 있다";
                }
                StarNightChapterState chapter = StarNightRunState.Instance?.Chapter;
                return chapter != null && chapter.GateLoopEnabled
                    ? chapter.GateActivated
                        ? "선택: 출항을 미루고 반송 불가 심층 보관소 열기"
                        : "별문을 켜야 드러나는 반송 불가 심층 보관소"
                    : "반송 불가 보관소 열기";
            }
        }

        public void Configure(GameObject doorBarrier)
        {
            barrier = doorBarrier;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            if (opened)
            {
                return;
            }

            StarNightRunState run = StarNightRunState.Ensure();
            bool canOpen = run.Chapter.GateLoopEnabled
                ? run.Chapter.GateActivated && run.Chapter.TemptationOpen
                : run.Chapter.DepartureReady;
            if (!canOpen)
            {
                StarNightHUD.Instance?.Toast(run.Chapter.GateLoopEnabled
                    ? "주소 조각 2개를 장착하고 별문 손잡이를 직접 당겨야 심층 주소가 드러난다."
                    : "북극성 항로 도장을 찾은 뒤에만 이 보관소가 주소를 드러낸다.");
                return;
            }

            opened = true;
            if (barrier != null)
            {
                barrier.SetActive(false);
            }
            run.SetFlag("CH4_RETURN_VAULT_OPENED");
            run.SetFlag("starpost.temptation.open");
            float scent = run.ConsequenceResolver.ModifyScent(9f);
            run.Chapter.AddScent(scent, "반송될 수 없는 오래된 주소들이 깨어났다", "ReturnVault");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.ReturnVaultEntered,
                actorId = "Player",
                targetId = "ReturnVault",
                detail = "떠날 수 있는데도 반송 불가 보관소를 열었다",
                scentDelta = scent,
                witnessed = true
            });
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.EnteredTemptationRoom,
                actorId = "Player",
                targetId = "DeepReturnVault",
                detail = "떠날 수 있었지만 라니의 전체 명령이 남은 심층 보관소를 열었다",
                scentDelta = scent,
                witnessed = true
            });
            StarNightHUD.Instance?.Toast("반송 불가 보관소 개방. 잘못된 배송물이 계속 도착한다.", 5f);
        }
    }
}
