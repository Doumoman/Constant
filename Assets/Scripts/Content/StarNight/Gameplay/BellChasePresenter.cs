using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class BellChasePresenter : MonoBehaviour
    {
        [SerializeField] private MaruDirector maruDirector;
        [SerializeField] private GameObject firstBellTrace;
        [SerializeField] private GameObject secondBellPresence;
        [SerializeField] private GameObject gateClosingVisual;

        private StarNightRunState run;
        private StarNightChapterState chapter;
        private bool bound;

        public float Alert => chapter != null ? chapter.PostGateAlert : 0f;
        public StarBellPhase Phase => chapter != null ? chapter.BellPhase : StarBellPhase.None;

        public void Configure(MaruDirector director, GameObject firstTrace,
            GameObject secondPresence, GameObject closingVisual)
        {
            maruDirector = director;
            firstBellTrace = firstTrace;
            secondBellPresence = secondPresence;
            gateClosingVisual = closingVisual;
        }

        private void Start()
        {
            BindForCurrentChapter();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void BindForCurrentChapter(MaruDirector director = null)
        {
            Unbind();
            run = StarNightRunState.Ensure();
            chapter = run.Chapter;
            if (director != null)
            {
                maruDirector = director;
            }
            if (maruDirector == null)
            {
                maruDirector = FindFirstObjectByType<MaruDirector>();
            }

            chapter.GateAlertChanged += OnGateAlertChanged;
            chapter.BellPhaseChanged += OnBellPhaseChanged;
            bound = true;
            ApplyPhase(chapter.BellPhase, false);
            EvaluateAlert(chapter.PostGateAlert);
        }

        public void EvaluateAlert(float alert)
        {
            if (run == null || chapter == null)
            {
                run = StarNightRunState.Instance;
                chapter = run != null ? run.Chapter : null;
            }
            if (run == null || chapter == null || !chapter.GateLoopEnabled || !chapter.GateActivated)
            {
                return;
            }

            if (chapter.BellPhase == StarBellPhase.First &&
                alert >= StarGateAlertRules.SecondBellThreshold)
            {
                run.ChapterLoop.TryAdvanceBell(StarBellPhase.Second);
            }
            if (chapter.BellPhase == StarBellPhase.Second &&
                alert >= StarGateAlertRules.ThirdBellThreshold)
            {
                run.ChapterLoop.TryAdvanceBell(StarBellPhase.Third);
            }
        }

        private void OnGateAlertChanged(float alert)
        {
            EvaluateAlert(alert);
        }

        private void OnBellPhaseChanged(StarBellPhase phase)
        {
            ApplyPhase(phase, true);
        }

        private void ApplyPhase(StarBellPhase phase, bool announce)
        {
            SetActive(firstBellTrace, phase >= StarBellPhase.First);
            SetActive(secondBellPresence, phase >= StarBellPhase.Second);
            SetActive(gateClosingVisual, phase >= StarBellPhase.Third);
            maruDirector?.ApplyBellPhase(phase);

            if (!announce)
            {
                return;
            }

            switch (phase)
            {
                case StarBellPhase.First:
                    StarNightHUD.Instance?.Toast(
                        "첫 번째 방울. 별문은 열렸고, 먼 지붕에 마루의 발자국만 번진다.", 4.5f);
                    break;
                case StarBellPhase.Second:
                    StarNightHUD.Instance?.Toast(
                        "두 번째 방울. 마루가 같은 정거장에 들어와 물건과 주민의 냄새를 먼저 쫓는다.", 5f);
                    break;
                case StarBellPhase.Third:
                    StarNightHUD.Instance?.Toast(
                        "세 번째 방울. 별문이 닫히기 시작했다. 이제 마루가 당신을 직접 쫓는다!", 5.5f);
                    break;
            }
        }

        private void Unbind()
        {
            if (bound && chapter != null)
            {
                chapter.GateAlertChanged -= OnGateAlertChanged;
                chapter.BellPhaseChanged -= OnBellPhaseChanged;
            }
            bound = false;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
