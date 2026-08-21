#if LEGACY_DISABLED
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarNight.Narrative
{
    public sealed class NarrativeViewLayout
    {
        private static readonly Color32 Panel = new(8, 18, 34, 238);
        private static readonly Color32 Text = new(239, 235, 216, 255);
        private static readonly Color32 Gold = new(239, 205, 118, 255);

        public Canvas Canvas { get; private set; }
        public CanvasGroup ConversationGroup { get; private set; }
        public TMP_Text ConversationName { get; private set; }
        public TMP_Text ConversationBody { get; private set; }
        public Image ConversationPortrait { get; private set; }
        public GameObject ConversationWait { get; private set; }
        public CanvasGroup BubbleGroup { get; private set; }
        public Image BubblePanel { get; private set; }
        public TMP_Text BubbleName { get; private set; }
        public TMP_Text BubbleBody { get; private set; }
        public CanvasGroup NarrationGroup { get; private set; }
        public TMP_Text NarrationBody { get; private set; }
        public CanvasGroup OptionsGroup { get; private set; }
        public TMP_Text[] OptionLabels { get; private set; }

        public static NarrativeViewLayout Build(Transform owner, TMP_FontAsset font)
        {
            var layout = new NarrativeViewLayout();
            GameObject canvasObject = new("GameplayDialogueCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(owner, false);
            layout.Canvas = canvasObject.GetComponent<Canvas>();
            layout.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            layout.Canvas.sortingOrder = 300;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            Stretch(canvasObject.GetComponent<RectTransform>());

            RectTransform root = CreateRect("DialogueSafeRoot", canvasObject.transform);
            Stretch(root, 36f, 36f, 24f, 24f);
            layout.BuildConversation(root, font);
            layout.BuildBubble(root, font);
            layout.BuildNarration(root, font);
            layout.BuildOptions(root, font);
            layout.HideAll();
            return layout;
        }

        public void HideAll()
        {
            SetVisible(ConversationGroup, false);
            SetVisible(BubbleGroup, false);
            SetVisible(NarrationGroup, false);
            SetVisible(OptionsGroup, false);
        }

        public static void SetVisible(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        private void BuildConversation(RectTransform root, TMP_FontAsset font)
        {
            Image panel = CreatePanel("ConversationPanel", root, Panel);
            RectTransform rect = panel.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0.28f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            ConversationGroup = panel.gameObject.AddComponent<CanvasGroup>();

            ConversationPortrait = CreatePanel("Portrait", rect, new Color32(24, 42, 62, 255));
            Anchor(ConversationPortrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(210f, 250f), new Vector2(126f, 0f));
            ConversationPortrait.preserveAspect = true;
            ConversationName = CreateText("CharacterName", rect, font, string.Empty, 27f, Gold, TextAlignmentOptions.BottomLeft);
            SetRect(ConversationName.rectTransform, new Vector2(0.16f, 0.66f), new Vector2(0.94f, 0.94f));
            ConversationBody = CreateText("DialogueBody", rect, font, string.Empty, 31f, Text, TextAlignmentOptions.TopLeft);
            SetRect(ConversationBody.rectTransform, new Vector2(0.16f, 0.12f), new Vector2(0.94f, 0.68f));
            ConversationBody.textWrappingMode = TextWrappingModes.Normal;

            TMP_Text wait = CreateText("WaitGlyph", rect, font, "X", 21f, Gold, TextAlignmentOptions.Center);
            Anchor(wait.rectTransform, Vector2.one, new Vector2(54f, 38f), new Vector2(-36f, -28f));
            ConversationWait = wait.gameObject;
        }

        private void BuildBubble(RectTransform root, TMP_FontAsset font)
        {
            BubblePanel = CreatePanel("FieldBubble", root, new Color32(19, 39, 56, 235));
            Anchor(BubblePanel.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(620f, 164f), Vector2.zero);
            BubbleGroup = BubblePanel.gameObject.AddComponent<CanvasGroup>();
            BubbleName = CreateText("CharacterName", BubblePanel.rectTransform, font, string.Empty, 20f, Gold, TextAlignmentOptions.BottomLeft);
            SetRect(BubbleName.rectTransform, new Vector2(0.06f, 0.58f), new Vector2(0.94f, 0.92f));
            BubbleBody = CreateText("BubbleBody", BubblePanel.rectTransform, font, string.Empty, 25f, Text, TextAlignmentOptions.TopLeft);
            SetRect(BubbleBody.rectTransform, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.62f));
            BubbleBody.textWrappingMode = TextWrappingModes.Normal;
        }

        private void BuildNarration(RectTransform root, TMP_FontAsset font)
        {
            Image panel = CreatePanel("NarrationCard", root, new Color32(3, 10, 22, 210));
            Anchor(panel.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(960f, 210f), Vector2.zero);
            NarrationGroup = panel.gameObject.AddComponent<CanvasGroup>();
            NarrationBody = CreateText("NarrationBody", panel.rectTransform, font, string.Empty, 31f, Text, TextAlignmentOptions.Center);
            Stretch(NarrationBody.rectTransform, 70f, 70f, 30f, 30f);
            NarrationBody.textWrappingMode = TextWrappingModes.Normal;
        }

        private void BuildOptions(RectTransform root, TMP_FontAsset font)
        {
            Image panel = CreatePanel("GameOptions", root, new Color32(8, 18, 34, 248));
            Anchor(panel.rectTransform, new Vector2(0.5f, 0.48f), new Vector2(820f, 360f), Vector2.zero);
            OptionsGroup = panel.gameObject.AddComponent<CanvasGroup>();
            OptionLabels = new TMP_Text[4];
            for (int index = 0; index < OptionLabels.Length; index++)
            {
                TMP_Text label = CreateText("Option_" + index, panel.rectTransform, font, string.Empty, 26f, Text, TextAlignmentOptions.MidlineLeft);
                Anchor(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(700f, 62f), new Vector2(0f, -60f - index * 74f));
                OptionLabels[index] = label;
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject instance = new(name, typeof(RectTransform));
            instance.transform.SetParent(parent, false);
            return instance.GetComponent<RectTransform>();
        }

        private static Image CreatePanel(string name, Transform parent, Color color)
        {
            GameObject instance = new(name, typeof(RectTransform), typeof(Image));
            instance.transform.SetParent(parent, false);
            Image image = instance.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, string value, float size, Color color, TextAlignmentOptions alignment)
        {
            GameObject instance = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            instance.transform.SetParent(parent, false);
            TMP_Text text = instance.GetComponent<TMP_Text>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float bottom = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}

#endif
