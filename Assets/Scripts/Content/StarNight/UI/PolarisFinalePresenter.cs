using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class PolarisFinalePresenter : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset font;
        private TMP_Text statusText;
        private Image panel;
        private PolarisFinaleState finale;
        private int lastShownSecond = -1;

        public string CurrentText => statusText != null ? statusText.text : string.Empty;

        public void SetFont(TMP_FontAsset value)
        {
            font = value;
        }

        private void Awake()
        {
            Build();
        }

        private void Start()
        {
            finale = StarNightRunState.Ensure().GetComponent<PolarisFinaleState>();
            if (finale != null)
            {
                finale.Changed += Refresh;
            }
            Refresh();
        }

        private void OnDestroy()
        {
            if (finale != null)
            {
                finale.Changed -= Refresh;
            }
        }

        private void Update()
        {
            if (finale == null || !finale.CountdownActive)
            {
                return;
            }
            int second = Mathf.CeilToInt(finale.TimeRemaining);
            if (second != lastShownSecond)
            {
                lastShownSecond = second;
                Refresh();
            }
        }

        public void RefreshForTests()
        {
            finale ??= StarNightRunState.Ensure().GetComponent<PolarisFinaleState>();
            Refresh();
        }

        private void Refresh()
        {
            if (finale == null || statusText == null)
            {
                return;
            }

            string countdown = finale.CountdownActive
                ? $"\n중심별까지 {Mathf.CeilToInt(finale.TimeRemaining)}초 · 마루의 역이용: " +
                  PolarisFinaleState.VerbDisplayName(finale.CounterVerb)
                : string.Empty;
            statusText.text = $"<color=#FFD15C>{finale.BuildObjectiveText()}</color>{countdown}\n" +
                              $"별길 조건 · {finale.BuildStarRoadRequirements()}";
            panel.color = finale.CountdownActive && finale.TimeRemaining < 30f
                ? new Color(0.18f, 0.02f, 0.08f, 0.94f)
                : new Color(0.025f, 0.035f, 0.095f, 0.92f);
        }

        private void Build()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 106;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject panelObject = new("PolarisFinalePanel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(transform, false);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.47f, 0.2f);
            rect.anchorMax = new Vector2(0.98f, 0.43f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel = panelObject.GetComponent<Image>();

            GameObject label = new("PolarisFinaleStatus", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(panelObject.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.04f, 0.08f);
            labelRect.anchorMax = new Vector2(0.96f, 0.92f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            statusText = label.GetComponent<TextMeshProUGUI>();
            statusText.font = font;
            statusText.fontSize = 22f;
            statusText.color = new Color(0.92f, 0.91f, 0.78f);
            statusText.alignment = TextAlignmentOptions.TopLeft;
            statusText.textWrappingMode = TextWrappingModes.Normal;
        }
    }
}
