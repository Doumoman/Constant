using System.Collections;
using TMPro;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class TravelTicketPresenter : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text routeText;
        [SerializeField] private TMP_Text playerMarker;
        [SerializeField] private TMP_Text maruMarker;
        [SerializeField] private TMP_Text footer;

        private StarNightRunState run;
        private Coroutine maruMove;
        private int displayedMaruStation = 1;

        public bool Visible => panel != null && panel.gameObject.activeSelf;
        public string CurrentText => routeText != null ? routeText.text : string.Empty;
        public int TargetMaruStation => run?.RouteMap.MaruStationIndex ?? displayedMaruStation;

        public void Configure(RectTransform targetPanel, TMP_Text targetRouteText,
            TMP_Text targetPlayerMarker, TMP_Text targetMaruMarker, TMP_Text targetFooter)
        {
            panel = targetPanel;
            routeText = targetRouteText;
            playerMarker = targetPlayerMarker;
            maruMarker = targetMaruMarker;
            footer = targetFooter;
            if (playerMarker != null) playerMarker.text = "▲ 나";
            if (maruMarker != null) maruMarker.text = "● 마루";
            if (footer != null) footer.text = "T · 여행 티켓 접기/펼치기";
        }

        private void Start()
        {
            run = StarNightRunState.Ensure();
            displayedMaruStation = run.RouteMap.MaruStationIndex;
            run.RouteMap.Changed += OnRouteChanged;
            run.FlagChanged += OnFlagChanged;
            run.ChapterStarted += OnChapterStarted;
            Refresh(false);
        }

        private void OnDestroy()
        {
            if (run == null)
            {
                return;
            }
            run.RouteMap.Changed -= OnRouteChanged;
            run.FlagChanged -= OnFlagChanged;
            run.ChapterStarted -= OnChapterStarted;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T) && panel != null &&
                (run == null || run.GetFlag("TICKET_MAP_UNLOCKED") || run.CurrentChapter != StarChapterId.Prologue))
            {
                panel.gameObject.SetActive(!panel.gameObject.activeSelf);
            }
        }

        private void OnRouteChanged()
        {
            Refresh(true);
        }

        private void OnFlagChanged(string key, bool value)
        {
            if (key == "TICKET_MAP_UNLOCKED")
            {
                Refresh(false);
            }
        }

        private void OnChapterStarted(StarChapterDefinition definition)
        {
            Refresh(true);
        }

        public void RefreshForTests()
        {
            run ??= StarNightRunState.Ensure();
            Refresh(false);
        }

        private void Refresh(bool animateMaru)
        {
            if (run == null || panel == null || routeText == null)
            {
                return;
            }

            bool unlocked = run.GetFlag("TICKET_MAP_UNLOCKED") ||
                            run.CurrentChapter != StarChapterId.Prologue;
            panel.gameObject.SetActive(unlocked);
            routeText.text = run.RouteMap.BuildTicketText();
            SetMarker(playerMarker, run.RouteMap.PlayerStationIndex);

            int target = run.RouteMap.MaruStationIndex;
            if (animateMaru && Application.isPlaying && target != displayedMaruStation)
            {
                if (maruMove != null)
                {
                    StopCoroutine(maruMove);
                }
                maruMove = StartCoroutine(MoveMaru(displayedMaruStation, target));
            }
            else
            {
                displayedMaruStation = target;
                SetMarker(maruMarker, target);
            }
        }

        private IEnumerator MoveMaru(int from, int to)
        {
            float time = 0f;
            while (time < 1f)
            {
                time += Time.unscaledDeltaTime / 0.8f;
                SetMarker(maruMarker, Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, time)));
                yield return null;
            }
            displayedMaruStation = to;
            SetMarker(maruMarker, to);
            maruMove = null;
        }

        private static void SetMarker(TMP_Text marker, float station)
        {
            if (marker == null)
            {
                return;
            }
            float normalized = Mathf.Clamp01(station / (RunRouteMap.StationCount - 1f));
            RectTransform rect = marker.rectTransform;
            float center = Mathf.Lerp(0.08f, 0.92f, normalized);
            rect.anchorMin = new Vector2(center - 0.08f, rect.anchorMin.y);
            rect.anchorMax = new Vector2(center + 0.08f, rect.anchorMax.y);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
