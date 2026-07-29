using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MoonMillFuelPedestal : MonoBehaviour, IStarNightInteractable
    {
        public string Prompt => StarNightRunState.Instance != null && StarNightRunState.Instance.Chapter.DepartureReady
            ? "별 연료가 모두 모였다"
            : "별 연료 떡 넣기";

        public void Interact(StarNightPlayerAgent player)
        {
            StarNightChapterState chapter = StarNightRunState.Ensure().Chapter;
            if (chapter.DepartureReady)
            {
                StarNightHUD.Instance?.Toast("떠날 준비가 끝났다. 오른쪽 달배로 가자.");
                return;
            }

            FableObject fuel = player.Inventory.TakeFirstMatching(item =>
                item.HasTrait(FableTraits.DepartureSupply) || item.HasTrait(FableTraits.MoonCake));
            if (fuel == null)
            {
                StarNightHUD.Instance?.Toast("가방에 별 연료로 쓸 달떡이 없다.");
                return;
            }

            fuel.gameObject.SetActive(false);
            chapter.AddDepartureProgress(1, fuel.ObjectId);
            chapter.AddScent(3f, "달떡의 향이 방앗간 굴뚝으로 퍼졌다", fuel.ObjectId);
            StarNightHUD.Instance?.Toast($"달떡을 연료통에 넣었다. {chapter.DepartureProgress}/{chapter.RequiredDepartureProgress}");
        }
    }
}
