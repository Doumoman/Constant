using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class SunGardenGateBloomPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject firstBloom;
        [SerializeField] private GameObject secondBloom;
        private StarNightChapterState chapter;

        public void Configure(GameObject first, GameObject second)
        {
            firstBloom = first;
            secondBloom = second;
        }

        private void Start()
        {
            chapter = StarNightRunState.Ensure().Chapter;
            chapter.GateContributionChanged += OnContributionChanged;
            Refresh(chapter.GateContributions);
        }

        private void OnDestroy()
        {
            if (chapter != null)
            {
                chapter.GateContributionChanged -= OnContributionChanged;
            }
        }

        private void OnContributionChanged(int current, int required)
        {
            Refresh(current);
        }

        private void Refresh(int current)
        {
            if (firstBloom != null)
            {
                firstBloom.SetActive(current >= 1);
            }
            if (secondBloom != null)
            {
                secondBloom.SetActive(current >= 2);
            }
        }
    }
}
