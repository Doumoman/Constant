using System.Text;
using StarNight.Rewrite.Core;
using StarNight.Rewrite.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarNight.Rewrite.UI
{
    [DisallowMultipleComponent]
    public sealed class Rw1HudPresenter : MonoBehaviour
    {
        [SerializeField]
        private RunContext runContext;

        [SerializeField]
        private PlayerHealth playerHealth;

        [SerializeField]
        private RaniLampController raniLamp;

        [SerializeField]
        private PlayerInteractor interactor;

        private TMP_Text vitalsText;
        private TMP_Text loadoutText;
        private TMP_Text promptText;

        private void Awake()
        {
            BuildHud();
        }

        private void Start()
        {
            ResolveSources();
            Refresh();
        }

        private void Update()
        {
            if (runContext == null || playerHealth == null)
            {
                ResolveSources();
            }

            Refresh();
        }

        private void ResolveSources()
        {
            runContext ??= FindFirstObjectByType<RunContext>();
            playerHealth ??= FindFirstObjectByType<PlayerHealth>();
            raniLamp ??= FindFirstObjectByType<RaniLampController>();
            interactor ??= FindFirstObjectByType<PlayerInteractor>();
        }

        private void BuildHud()
        {
            if (transform.Find("RW1 HUD") != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject(
                "RW1 HUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            vitalsText = CreateHudBlock(
                canvasRect,
                "Vitals",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -24f),
                new Vector2(430f, 108f),
                TextAlignmentOptions.TopLeft);

            loadoutText = CreateHudBlock(
                canvasRect,
                "Loadout",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-24f, -24f),
                new Vector2(690f, 108f),
                TextAlignmentOptions.TopRight);

            promptText = CreateHudBlock(
                canvasRect,
                "Prompt",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 28f),
                new Vector2(980f, 58f),
                TextAlignmentOptions.Center);
            promptText.fontSize = 22f;
        }

        private void Refresh()
        {
            if (vitalsText == null || loadoutText == null || promptText == null)
            {
                return;
            }

            int current = playerHealth != null ? playerHealth.Current : 4;
            int maximum = playerHealth != null ? playerHealth.Maximum : 4;

            StringBuilder hearts = new StringBuilder();
            for (int index = 0; index < maximum; index++)
            {
                hearts.Append(index < current ? "[+]" : "[-]");
                if (index < maximum - 1)
                {
                    hearts.Append(' ');
                }
            }

            string lamp = raniLamp == null || raniLamp.IsAvailable
                ? "<color=#FFE58A>충전</color>"
                : "<color=#777777>소진</color>";
            vitalsText.text = $"체력  {hearts}\n라니 등불  {lamp}";

            RunLoadout loadout = runContext != null ? runContext.Loadout : null;
            int gold = loadout?.Gold ?? 0;
            int ropes = loadout?.Ropes ?? RunLoadout.StartingRopes;
            int bombs = loadout?.Bombs ?? RunLoadout.StartingBombs;
            HandToolId handTool = loadout?.HandTool ?? HandToolId.None;
            string promise = loadout != null && loadout.HasPromiseItem
                ? loadout.PromiseItemId
                : "없음";

            loadoutText.text =
                $"금화  {gold}    밧줄  {ropes}/{RunLoadout.RopeCapacity}    " +
                $"폭탄  {bombs}/{RunLoadout.BombCapacity}\n" +
                $"손도구  {HandToolPickup.GetKoreanName(handTool)}    약속물  {promise}";

            string prompt = interactor?.CurrentPrompt;
            promptText.text = string.IsNullOrWhiteSpace(prompt)
                ? "A/D 이동 · Space 점프 · E 상호작용 · J 손도구 · Q 밧줄 · R 폭탄"
                : prompt;

            vitalsText.alpha =
                playerHealth != null && playerHealth.IsInvulnerable ? 0.65f : 1f;
        }

        private static TMP_Text CreateHudBlock(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            TextAlignmentOptions alignment)
        {
            GameObject panelObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panelObject.transform.SetParent(parent, false);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = anchorMin;
            panelRect.anchorMax = anchorMax;
            panelRect.pivot = pivot;
            panelRect.anchoredPosition = anchoredPosition;
            panelRect.sizeDelta = size;

            Image panel = panelObject.GetComponent<Image>();
            panel.color = new Color(0.035f, 0.045f, 0.09f, 0.82f);
            panel.raycastTarget = false;

            GameObject textObject = new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panelObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 8f);
            textRect.offsetMax = new Vector2(-16f, -8f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 25f;
            text.color = new Color(0.95f, 0.97f, 1f, 1f);
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.richText = true;
            text.raycastTarget = false;
            return text;
        }
    }
}
