using UnityEngine;

namespace StarFetchingNight
{
    public enum StarSorterMode
    {
        Overload,
        Repair
    }

    [DisallowMultipleComponent]
    public sealed class StarSorterController : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private StarSorterMode mode;
        [SerializeField] private FableObject[] parcels;
        [SerializeField] private StarPostalAddress[] addresses;
        private bool used;

        public string Prompt => mode == StarSorterMode.Overload
            ? "자동 분류기를 강제 가동하기"
            : "북극성 도장으로 분류기 주소 복구하기";

        public void Configure(StarSorterMode sorterMode, FableObject[] sorterParcels,
            StarPostalAddress[] sorterAddresses)
        {
            mode = sorterMode;
            parcels = sorterParcels;
            addresses = sorterAddresses;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            if (used)
            {
                return;
            }

            StarNightRunState run = StarNightRunState.Ensure();
            if (mode == StarSorterMode.Repair)
            {
                if (!run.GetFlag("CH4_SORTER_OVERLOAD"))
                {
                    StarNightHUD.Instance?.Toast("분류기는 아직 조용히 돌고 있다.");
                    return;
                }
                if (!run.GetFlag("CH4_ROUTE_STAMP_RECOVERED"))
                {
                    StarNightHUD.Instance?.Toast("주소를 고치려면 북극성 항로 도장이 필요하다.");
                    return;
                }

                used = true;
                run.SetFlag("CH4_SORTER_REPAIRED");
                run.Chapter.AddScent(-8f, "분류기의 번진 주소를 지웠다", "Sorter");
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.SorterRepaired,
                    actorId = "Player",
                    targetId = "AutomaticSorter",
                    detail = "북극성 항로 도장으로 자동 분류기의 주소를 복구했다",
                    helpedResident = true,
                    witnessed = true
                });
                StarNightHUD.Instance?.Toast("분류기 주소 복구 완료. 폭주 배송 기록이 멈췄다.", 5f);
                return;
            }

            used = true;
            run.SetFlag("CH4_SORTER_OVERLOAD");
            float scent = run.ConsequenceResolver.ModifyScent(18f);
            run.Chapter.AddScent(scent, "자동 분류기가 모든 소포에 무작위 주소를 찍었다", "Sorter");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.SorterOverloaded,
                actorId = "Player",
                targetId = "AutomaticSorter",
                detail = "자동 분류기를 강제로 돌려 소포들을 서로 다른 방으로 흩었다",
                scentDelta = scent,
                causedAccident = true,
                witnessed = true
            });

            int count = Mathf.Min(parcels?.Length ?? 0, addresses?.Length ?? 0);
            for (int i = 0; i < count; i++)
            {
                if (parcels[i] != null && addresses[i] != null)
                {
                    run.Delivery.DeliverDirect(parcels[i], addresses[(i + 1) % count],
                        "AutomaticSorter", false);
                }
            }
            run.AccidentReport.Add("자동 분류기", "주소를 한꺼번에 찍어",
                $"{count}개의 소포를 엉뚱한 방으로 흩었다", run.Actions.LatestSequence);
            StarNightHUD.Instance?.Toast("쾅— 모든 우체통이 동시에 열렸다. 북극성 항로 도장은 폭주실 안에 있다!", 6f);
        }
    }
}
