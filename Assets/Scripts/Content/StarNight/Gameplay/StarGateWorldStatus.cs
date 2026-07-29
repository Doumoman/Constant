using TMPro;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarGateWorldStatus : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private string gateDisplayName = "달토끼 별문";
        [SerializeField] private string contributionNoun = "길떡";
        private StarNightRunState run;

        public string CurrentText => label != null ? label.text : string.Empty;

        public void Configure(TMP_Text targetLabel)
        {
            label = targetLabel;
        }

        public void Configure(TMP_Text targetLabel, string gateName, string noun)
        {
            label = targetLabel;
            gateDisplayName = string.IsNullOrWhiteSpace(gateName) ? "별문" : gateName;
            contributionNoun = string.IsNullOrWhiteSpace(noun) ? "기여" : noun;
        }

        private void Start()
        {
            run = StarNightRunState.Ensure();
            run.Chapter.GateContributionChanged += OnContributionChanged;
            run.Chapter.BellPhaseChanged += OnBellChanged;
            run.ChapterLoop.StateChanged += OnLoopStateChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (run == null)
            {
                return;
            }
            run.Chapter.GateContributionChanged -= OnContributionChanged;
            run.Chapter.BellPhaseChanged -= OnBellChanged;
            run.ChapterLoop.StateChanged -= OnLoopStateChanged;
        }

        private void OnContributionChanged(int current, int required)
        {
            Refresh();
        }

        private void OnBellChanged(StarBellPhase phase)
        {
            Refresh();
        }

        private void OnLoopStateChanged(ChapterLoopState state)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (label == null)
            {
                return;
            }
            if (run == null)
            {
                run = StarNightRunState.Instance;
            }
            StarNightChapterState chapter = run?.Chapter;
            if (chapter == null || !chapter.GateLoopEnabled)
            {
                label.text = gateDisplayName;
                return;
            }

            if (chapter.GateActivated)
            {
                string bells = chapter.BellPhase switch
                {
                    StarBellPhase.First => "● ○ ○",
                    StarBellPhase.Second => "● ● ○",
                    StarBellPhase.Third => "● ● ●",
                    _ => "○ ○ ○"
                };
                label.text = $"{gateDisplayName} 가동 · 방울 {bells}";
            }
            else if (chapter.GateReady)
            {
                label.text = $"{contributionNoun} 2/2 · 다시 상호작용해 손잡이 당기기";
            }
            else
            {
                label.text = $"{gateDisplayName} · {contributionNoun} " +
                             $"{chapter.GateContributions}/{chapter.GateRequired}";
            }
        }
    }
}
