using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class PolarisRecordEcho : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private StarChapterId chapter;
        [SerializeField] private SpriteRenderer marker;

        public StarChapterId Chapter => chapter;
        public string Prompt => StarNightRunState.Instance?.GetFlag($"POLARIS_RECORD_{chapter}_SEEN") == true
            ? $"{RunRouteMap.GetStationName(RunRouteMap.GetGateIndex(chapter))} 기록 다시 읽기"
            : $"{RunRouteMap.GetStationName(RunRouteMap.GetGateIndex(chapter))} 대표 행동 재생";

        public void Configure(StarChapterId value, SpriteRenderer targetMarker)
        {
            chapter = value;
            marker = targetMarker;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            ExecuteForTests();
        }

        public bool ExecuteForTests()
        {
            PolarisFinaleState finale = StarNightRunState.Ensure().GetComponent<PolarisFinaleState>();
            if (finale == null)
            {
                return false;
            }

            string detail = finale.GetRepresentativeAction(chapter);
            bool added = finale.RegisterRecord(chapter);
            if (added && marker != null)
            {
                marker.color = new Color(1f, 0.78f, 0.28f);
                marker.transform.localScale *= 1.25f;
            }
            StarNightHUD.Instance?.Toast(
                added ? $"기록 별자리 · {detail}" : $"이미 본 기록 · {detail}", 5f);
            return added;
        }
    }
}
