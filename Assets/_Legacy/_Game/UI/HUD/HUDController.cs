#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Interaction.HandSlot;
using StarNight.Stage.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarNight.UI.HUD
{
    [DisallowMultipleComponent]
    public sealed class HUDController : MonoBehaviour
    {
        private static readonly Color32 PanelColor = new Color32(8, 18, 34, 220);
        private static readonly Color32 Gold = new Color32(239, 205, 118, 255);
        private static readonly Color32 Cyan = new Color32(149, 218, 221, 255);
        private static readonly Color32 TextColor = new Color32(239, 235, 216, 255);

        [SerializeField] private TMP_FontAsset hudFont;

        private HUDModelSource modelSource;
        private HUDInputCoordinator inputCoordinator;
        private Canvas hudCanvas;
        private CanvasScaler canvasScaler;
        private SafeAreaFitter safeAreaFitter;
        private CanvasGroup hudContent;
        private RectTransform healthRect;
        private readonly Image[] healthSlots = new Image[4];
        private Sprite heartSprite;
        private string healthDisplay = string.Empty;
        private TMP_Text lanternText;
        private TMP_Text routeText;
        private TMP_Text bellText;
        private TMP_Text moneyText;
        private TMP_Text moneyDeltaText;
        private TMP_Text consumableText;
        private GameObject handPanel;
        private RectTransform handRect;
        private Image handIcon;
        private TMP_Text handText;
        private RectTransform equipmentBeltRoot;
        private readonly GameObject[] equipmentSlotObjects = new GameObject[5];
        private readonly Image[] equipmentSlotPanels = new Image[5];
        private readonly Image[] equipmentSlotIcons = new Image[5];
        private readonly TMP_Text[] equipmentSlotLabels = new TMP_Text[5];
        private readonly Image[] equipmentDurabilityFills = new Image[5];
        private TMP_Text equipmentFeedbackText;
        private GameObject actionPanel;
        private TMP_Text actionGlyphText;
        private TMP_Text actionLabelText;
        private TMP_Text dropHintText;
        private GameObject mapOverlay;
        private RectTransform mapGraphRoot;
        private TMP_Text mapHintText;
        private TMP_Text stageNameText;
        private Image fadeCurtain;
        private GameObject maruWarningPanel;
        private TMP_Text maruWarningText;
        private GameObject maruEscapePanel;
        private TMP_Text maruEscapeText;
        private CanvasGroup bellEdgePulse;
        private int renderedMapVersion = -1;
        private int lastHealth = 4;
        private int lastBellPhase;
        private float healthShakeUntil;
        private float healthBlinkStarted;
        private float healthBlinkUntil;
        private float bellShakeUntil;
        private float handShakeUntil;
        private float handNameVisibleUntil;
        private float bellEdgePulseUntil;
        private string currentHandItemId = string.Empty;
        private string currentHandDisplayName = string.Empty;
        private bool currentHandResourceVisible;
        private int currentHandResource;
        private int currentHandResourceMaximum;
        private int lastHandResource = -1;
        private Vector2 healthBasePosition;
        private Vector2 bellBasePosition;
        private Vector2 handBasePosition;
        private RectTransform bellRect;

        public HUDModelSource ModelSource => modelSource;
        public HUDInputCoordinator InputCoordinator => inputCoordinator;
        public Canvas HudCanvas => hudCanvas;
        public CanvasScaler Scaler => canvasScaler;
        public SafeAreaFitter SafeArea => safeAreaFitter;
        public string HealthDisplay => healthDisplay;
        public string MoneyDisplay => moneyText?.text ?? string.Empty;
        public string ConsumableDisplay => consumableText?.text ?? string.Empty;
        public string PrimaryGlyphDisplay => actionGlyphText?.text ?? string.Empty;
        public bool IsMapVisible => mapOverlay != null && mapOverlay.activeSelf;
        public int MapNodeCount => mapGraphRoot == null ? 0 : CountNamedChildren("MapNode_");
        public bool IsMaruWarningVisible => maruWarningPanel != null && maruWarningPanel.activeSelf;
        public bool IsMaruEscapeVisible => maruEscapePanel != null && maruEscapePanel.activeSelf;
        public string EquipmentFeedbackDisplay => equipmentFeedbackText?.text ?? string.Empty;

        private void Awake()
        {
            BuildScreen();
            modelSource = GetComponent<HUDModelSource>();
            if (modelSource == null)
            {
                modelSource = gameObject.AddComponent<HUDModelSource>();
            }

            inputCoordinator = GetComponent<HUDInputCoordinator>();
            if (inputCoordinator == null)
            {
                inputCoordinator = gameObject.AddComponent<HUDInputCoordinator>();
            }
            inputCoordinator.Configure(modelSource);
            modelSource.ModelChanged += ApplyModel;
            ApplyModel(modelSource.Model);
        }

        private void OnDestroy()
        {
            if (modelSource != null)
            {
                modelSource.ModelChanged -= ApplyModel;
            }

            if (heartSprite != null)
            {
                Texture2D texture = heartSprite.texture;
                Destroy(heartSprite);
                if (texture != null)
                {
                    Destroy(texture);
                }
            }
        }

        private void Update()
        {
            AnimateShake(healthRect, healthBasePosition, healthShakeUntil, 5f);
            AnimateShake(bellRect, bellBasePosition, bellShakeUntil, 3f);
            AnimateShake(handRect, handBasePosition, handShakeUntil, 4f);
            AnimateHeartBlink();
            UpdateHandLabel();
            AnimateBellEdgePulse();
        }

        public void ConfigureFont(TMP_FontAsset font)
        {
            hudFont = font;
        }

        private void BuildScreen()
        {
            GameObject canvasObject = new GameObject("GameplayHUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            hudCanvas = canvasObject.GetComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 200;

            canvasScaler = canvasObject.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            Stretch(canvasObject.GetComponent<RectTransform>());

            GameObject safeObject = new GameObject("SafeAreaRoot", typeof(RectTransform), typeof(SafeAreaFitter));
            safeObject.transform.SetParent(canvasObject.transform, false);
            Stretch(safeObject.GetComponent<RectTransform>());
            safeAreaFitter = safeObject.GetComponent<SafeAreaFitter>();

            GameObject contentObject = new GameObject("HUDContent", typeof(RectTransform), typeof(CanvasGroup));
            contentObject.transform.SetParent(safeObject.transform, false);
            Stretch(contentObject.GetComponent<RectTransform>());
            hudContent = contentObject.GetComponent<CanvasGroup>();
            RectTransform content = contentObject.GetComponent<RectTransform>();

            BuildTopLeft(content);
            BuildTopCenter(content);
            BuildTopRight(content);
            BuildBottomLeft(content);
            BuildEquipmentBelt(content);
            BuildBottomRight(content);
            BuildMap(content);
            BuildMaruStatus(content);

            stageNameText = CreateText("StageName", safeObject.GetComponent<RectTransform>(), string.Empty, 31f, Gold, TextAlignmentOptions.Center);
            SetAnchored(stageNameText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520f, 64f), new Vector2(0f, 250f));
            stageNameText.fontStyle = FontStyles.Bold;

            fadeCurtain = CreateImage("StageFadeCurtain", canvasObject.GetComponent<RectTransform>(), Color.clear);
            Stretch(fadeCurtain.rectTransform);
            fadeCurtain.transform.SetAsLastSibling();
        }

        private void BuildTopLeft(RectTransform parent)
        {
            RectTransform panel = CreatePanel("StatusCluster", parent, PanelColor);
            SetAnchored(panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(420f, 78f), new Vector2(30f, -26f));
            GameObject healthObject = new GameObject("HealthSlots", typeof(RectTransform));
            healthObject.transform.SetParent(panel, false);
            healthRect = healthObject.GetComponent<RectTransform>();
            SetRect(healthRect, new Vector2(0.04f, 0.08f), new Vector2(0.68f, 0.92f));
            heartSprite = CreateHeartSprite();
            for (int index = 0; index < healthSlots.Length; index++)
            {
                Image heart = CreateImage("HeartSlot_" + (index + 1), healthRect, new Color32(239, 113, 127, 255));
                heart.sprite = heartSprite;
                heart.type = Image.Type.Simple;
                heart.preserveAspect = true;
                SetAnchored(heart.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 38f), new Vector2(index * 58f, 0f));
                healthSlots[index] = heart;
            }
            healthBasePosition = healthRect.anchoredPosition;

            lanternText = CreateText("Lantern", panel, "등불 ◆", 20f, Gold, TextAlignmentOptions.MidlineRight);
            SetRect(lanternText.rectTransform, new Vector2(0.64f, 0f), new Vector2(0.96f, 1f));
        }

        private void BuildTopCenter(RectTransform parent)
        {
            RectTransform panel = CreatePanel("RouteCluster", parent, new Color32(8, 18, 34, 205));
            SetAnchored(panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(360f, 94f), new Vector2(0f, -24f));
            routeText = CreateText("ExitDirection", panel, "출구", 27f, Gold, TextAlignmentOptions.Center);
            SetRect(routeText.rectTransform, new Vector2(0f, 0.43f), new Vector2(1f, 1f));
            routeText.fontStyle = FontStyles.Bold;
            bellText = CreateText("BellSlots", panel, "○ ○ ○", 23f, Cyan, TextAlignmentOptions.Center);
            SetRect(bellText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f));
            bellRect = bellText.rectTransform;
            bellBasePosition = bellRect.anchoredPosition;
        }

        private void BuildTopRight(RectTransform parent)
        {
            RectTransform panel = CreatePanel("ResourceCluster", parent, PanelColor);
            SetAnchored(panel, Vector2.one, Vector2.one, new Vector2(500f, 78f), new Vector2(-30f, -26f));
            moneyText = CreateText("Money", panel, "0원", 24f, Gold, TextAlignmentOptions.MidlineLeft);
            SetRect(moneyText.rectTransform, new Vector2(0.04f, 0f), new Vector2(0.38f, 1f));
            moneyDeltaText = CreateText("MoneyDelta", panel, string.Empty, 18f, Cyan, TextAlignmentOptions.BottomLeft);
            SetRect(moneyDeltaText.rectTransform, new Vector2(0.04f, -0.25f), new Vector2(0.38f, 0.25f));
            consumableText = CreateText("Consumables", panel, "로프 4   폭탄 4", 22f, TextColor, TextAlignmentOptions.MidlineRight);
            SetRect(consumableText.rectTransform, new Vector2(0.36f, 0f), new Vector2(0.96f, 1f));
        }

        private void BuildBottomLeft(RectTransform parent)
        {
            RectTransform panel = CreatePanel("HandSlot", parent, PanelColor);
            SetAnchored(panel, Vector2.zero, Vector2.zero, new Vector2(420f, 86f), new Vector2(30f, 28f));
            handPanel = panel.gameObject;
            handRect = panel;
            handBasePosition = panel.anchoredPosition;
            handIcon = CreateImage("HandItemIcon", panel, Color.white);
            SetAnchored(handIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(56f, 56f), new Vector2(18f, 0f));
            handIcon.preserveAspect = true;
            handText = CreateText("HandTool", panel, string.Empty, 22f, TextColor, TextAlignmentOptions.MidlineLeft);
            Stretch(handText.rectTransform, 88f, 20f, 0f, 0f);
        }

        private void BuildEquipmentBelt(RectTransform parent)
        {
            RectTransform root = CreatePanel("EquipmentBelt", parent, new Color32(5, 13, 25, 205));
            SetAnchored(root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(650f, 108f), new Vector2(0f, 22f));
            equipmentBeltRoot = root;

            for (int index = 0; index < equipmentSlotObjects.Length; index++)
            {
                Image panel = CreateImage("EquipmentSlot_" + (index + 1), root, new Color32(20, 35, 52, 235));
                SetAnchored(panel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(112f, 82f), new Vector2(21f + index * 127f, 0f));
                equipmentSlotObjects[index] = panel.gameObject;
                equipmentSlotPanels[index] = panel;

                Image icon = CreateImage("Icon", panel.rectTransform, Color.white);
                SetAnchored(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(52f, 52f), new Vector2(0f, 8f));
                icon.preserveAspect = true;
                equipmentSlotIcons[index] = icon;

                TMP_Text label = CreateText("Status", panel.rectTransform, string.Empty, 15f, TextColor, TextAlignmentOptions.Center);
                SetRect(label.rectTransform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.31f));
                equipmentSlotLabels[index] = label;

                Image durabilityTrack = CreateImage("DurabilityTrack", panel.rectTransform, new Color32(48, 56, 66, 230));
                SetRect(durabilityTrack.rectTransform, new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.09f));
                Image durabilityFill = CreateImage("DurabilityFill", durabilityTrack.rectTransform, Cyan);
                durabilityFill.rectTransform.anchorMin = Vector2.zero;
                durabilityFill.rectTransform.anchorMax = Vector2.one;
                durabilityFill.rectTransform.pivot = new Vector2(0f, 0.5f);
                durabilityFill.rectTransform.offsetMin = Vector2.zero;
                durabilityFill.rectTransform.offsetMax = Vector2.zero;
                equipmentDurabilityFills[index] = durabilityFill;
            }
            equipmentFeedbackText = CreateText(
                "EquipmentFeedback",
                parent,
                string.Empty,
                24f,
                Gold,
                TextAlignmentOptions.Center);
            SetAnchored(
                equipmentFeedbackText.rectTransform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(520f, 42f),
                new Vector2(0f, 145f));
            equipmentFeedbackText.fontStyle = FontStyles.Bold;
            equipmentFeedbackText.gameObject.SetActive(false);
            equipmentBeltRoot.gameObject.SetActive(false);
        }

        private void BuildBottomRight(RectTransform parent)
        {
            RectTransform panel = CreatePanel("ActionPrompt", parent, PanelColor);
            SetAnchored(panel, Vector2.right, Vector2.right, new Vector2(440f, 98f), new Vector2(-30f, 28f));
            actionPanel = panel.gameObject;

            RectTransform glyphBadge = CreatePanel("PrimaryGlyphBadge", panel, new Color32(223, 188, 104, 245));
            SetAnchored(glyphBadge, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(94f, 54f), new Vector2(18f, 0f));
            actionGlyphText = CreateText("Glyph", glyphBadge, "X", 19f, new Color32(7, 15, 28, 255), TextAlignmentOptions.Center);
            Stretch(actionGlyphText.rectTransform);
            actionGlyphText.fontStyle = FontStyles.Bold;

            actionLabelText = CreateText("ActionLabel", panel, "출항하기", 23f, TextColor, TextAlignmentOptions.MidlineLeft);
            SetRect(actionLabelText.rectTransform, new Vector2(0.28f, 0.32f), new Vector2(0.96f, 0.96f));
            dropHintText = CreateText("DropHint", panel, string.Empty, 16f, Cyan, TextAlignmentOptions.TopLeft);
            SetRect(dropHintText.rectTransform, new Vector2(0.28f, 0.02f), new Vector2(0.96f, 0.38f));
        }

        private void BuildMap(RectTransform parent)
        {
            Image overlay = CreateImage("RoomMapOverlay", parent, new Color32(2, 8, 20, 225));
            Stretch(overlay.rectTransform);
            mapOverlay = overlay.gameObject;

            RectTransform panel = CreatePanel("RoomGraphPanel", overlay.rectTransform, new Color32(11, 27, 46, 250));
            SetAnchored(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(820f, 520f), Vector2.zero);
            TMP_Text title = CreateText("MapTitle", panel, "방 지도", 34f, Gold, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0f, 0.84f), new Vector2(1f, 0.98f));
            title.fontStyle = FontStyles.Bold;

            GameObject graphObject = new GameObject("VisitedRoomGraph", typeof(RectTransform));
            graphObject.transform.SetParent(panel, false);
            mapGraphRoot = graphObject.GetComponent<RectTransform>();
            SetRect(mapGraphRoot, new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.82f));

            mapHintText = CreateText("MapHint", panel, "[TAB] 닫기  ·  지도는 게임을 멈추지 않습니다", 18f, Cyan, TextAlignmentOptions.Center);
            SetRect(mapHintText.rectTransform, new Vector2(0f, 0.04f), new Vector2(1f, 0.17f));
            mapOverlay.SetActive(false);
        }

        private void BuildMaruStatus(RectTransform parent)
        {
            GameObject pulseObject = new GameObject("BellVisualAlert", typeof(RectTransform), typeof(CanvasGroup));
            pulseObject.transform.SetParent(parent, false);
            RectTransform pulseRect = pulseObject.GetComponent<RectTransform>();
            Stretch(pulseRect);
            bellEdgePulse = pulseObject.GetComponent<CanvasGroup>();
            bellEdgePulse.alpha = 0f;
            bellEdgePulse.interactable = false;
            bellEdgePulse.blocksRaycasts = false;
            Color edgeColor = new Color32(239, 205, 118, 150);
            RectTransform top = CreatePanel("TopWave", pulseRect, edgeColor);
            SetRect(top, new Vector2(0f, 0.985f), Vector2.one);
            RectTransform bottom = CreatePanel("BottomWave", pulseRect, edgeColor);
            SetRect(bottom, Vector2.zero, new Vector2(1f, 0.015f));
            RectTransform left = CreatePanel("LeftWave", pulseRect, edgeColor);
            SetRect(left, Vector2.zero, new Vector2(0.01f, 1f));
            RectTransform right = CreatePanel("RightWave", pulseRect, edgeColor);
            SetRect(right, new Vector2(0.99f, 0f), Vector2.one);

            RectTransform warning = CreatePanel("MaruApproachWarning", parent, new Color32(44, 24, 54, 235));
            SetAnchored(warning, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(470f, 54f), new Vector2(0f, -132f));
            maruWarningPanel = warning.gameObject;
            maruWarningText = CreateText("Warning", warning, string.Empty, 22f, new Color32(246, 187, 158, 255), TextAlignmentOptions.Center);
            Stretch(maruWarningText.rectTransform, 14f, 14f, 0f, 0f);
            maruWarningText.fontStyle = FontStyles.Bold;
            maruWarningPanel.SetActive(false);

            RectTransform escape = CreatePanel("MaruEscapeGauge", parent, new Color32(13, 20, 37, 248));
            SetAnchored(escape, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(580f, 164f), new Vector2(0f, 24f));
            maruEscapePanel = escape.gameObject;
            maruEscapeText = CreateText("EscapeText", escape, string.Empty, 27f, TextColor, TextAlignmentOptions.Center);
            Stretch(maruEscapeText.rectTransform, 28f, 28f, 12f, 12f);
            maruEscapeText.fontStyle = FontStyles.Bold;
            maruEscapePanel.SetActive(false);
        }

        private void ApplyModel(HUDModel model)
        {
            bool visible = model.Visibility != HUDVisibility.Hidden;
            hudContent.alpha = model.Visibility == HUDVisibility.Dimmed ? 0.45f : visible ? 1f : 0f;
            hudContent.interactable = false;
            hudContent.blocksRaycasts = false;

            if (model.Health < lastHealth)
            {
                healthShakeUntil = Time.unscaledTime + 0.18f;
                healthBlinkStarted = Time.unscaledTime;
                healthBlinkUntil = healthBlinkStarted + 0.4f;
            }
            lastHealth = model.Health;
            healthDisplay = HUDFormatting.Health(model.Health, model.MaxHealth);
            for (int index = 0; index < healthSlots.Length; index++)
            {
                healthSlots[index].color = index < model.Health
                    ? new Color32(239, 113, 127, 255)
                    : new Color32(76, 86, 99, 210);
            }
            lanternText.text = model.LanternAvailable ? "등불 ◆" : "등불 ◇";
            lanternText.color = model.LanternAvailable ? Gold : new Color32(112, 120, 126, 255);

            if ((int)model.BellPhase > lastBellPhase)
            {
                bellShakeUntil = Time.unscaledTime + 0.22f;
                if (model.VisualBellAlert)
                {
                    bellEdgePulseUntil = Time.unscaledTime + 0.65f;
                }
            }
            lastBellPhase = (int)model.BellPhase;
            bellText.text = HUDFormatting.Bells(model.BellPhase) +
                            (model.ShowMaruTimer && model.BellPhase != BellPhase.Maru
                                ? "  ·  " + model.MaruRemainingSeconds + "초"
                                : string.Empty);

            maruWarningPanel.SetActive(visible && model.MaruChasing && !model.MaruEscapeActive);
            if (maruWarningPanel.activeSelf)
            {
                maruWarningText.text = "마루 접근  " + DirectionArrow(model.MaruApproachDirection);
            }
            maruEscapePanel.SetActive(visible && model.MaruEscapeActive);
            if (maruEscapePanel.activeSelf)
            {
                maruEscapeText.text = "마루에게 물렸습니다\n[" + model.PrimaryGlyph + "] 탈출  " +
                                      ProgressBar(model.MaruEscapeProgress) + "  " +
                                      model.MaruEscapeRemainingSeconds.ToString("0.0") + "초";
            }

            if (model.BossActive)
            {
                routeText.gameObject.SetActive(true);
                routeText.text = "별매듭  ◆ ◆ ◆";
                routeText.color = Gold;
            }
            else
            {
                routeText.gameObject.SetActive(model.ExitGuidanceValid);
                routeText.text = model.ExitGuidanceValid
                    ? "출구  " + HUDFormatting.Direction(model.ExitDirection, model.ExitInCurrentRoom)
                    : string.Empty;
                Color routeColor = Gold;
                routeColor.a = model.ExitDiscovered ? 1f : 0.7f;
                routeText.color = routeColor;
            }

            moneyText.text = HUDFormatting.Money(model.MoneyWon);
            moneyDeltaText.gameObject.SetActive(model.MoneyDelta != 0);
            moneyDeltaText.text = HUDFormatting.MoneyDelta(model.MoneyDelta);
            consumableText.text = HUDFormatting.Consumable("로프", model.Ropes) + "   " + HUDFormatting.Consumable("폭탄", model.Bombs);

            bool hasHandItem = model.HandSlotOccupied;
            string handItemId = !string.IsNullOrWhiteSpace(model.HandToolId)
                ? model.HandToolId
                : model.HandDisplayName;
            if (hasHandItem && handItemId != currentHandItemId)
            {
                handNameVisibleUntil = Time.unscaledTime + 1.2f;
            }
            if (model.HandResourceVisible
                && lastHandResource > 0
                && model.HandResourceCurrent == 0)
            {
                handShakeUntil = Time.unscaledTime + 0.12f;
            }
            lastHandResource = model.HandResourceVisible ? model.HandResourceCurrent : -1;
            currentHandItemId = handItemId;
            currentHandDisplayName = model.HandDisplayName;
            currentHandResourceVisible = model.HandResourceVisible;
            currentHandResource = model.HandResourceCurrent;
            currentHandResourceMaximum = model.HandResourceMaximum;
            handPanel.SetActive(hasHandItem);
            handIcon.sprite = model.HandIcon;
            handIcon.gameObject.SetActive(model.HandIcon != null);
            UpdateHandLabel();
            ApplyEquipmentBelt(model);
            equipmentFeedbackText.text = model.EquipmentFeedbackMessage;
            equipmentFeedbackText.gameObject.SetActive(
                visible && model.EquipmentFeedbackVisible);

            bool showAction = visible && !model.MapOpen && (model.ShowActionPrompt || hasHandItem);
            actionPanel.SetActive(showAction);
            bool selectedSpringJump = !model.ShowActionPrompt && HasSelectedSpringJump(model);
            actionGlyphText.text = selectedSpringJump ? "SPACE" : model.PrimaryGlyph;
            string label = model.ShowActionPrompt
                ? model.ActionLabel
                : selectedSpringJump ? "강화 점프" : model.HandPrimaryActionLabel;
            if (model.ShowActionPrompt && model.ActionProgress > 0f)
            {
                label += "  " + Mathf.RoundToInt(model.ActionProgress * 100f) + "%";
            }
            actionLabelText.text = label;
            dropHintText.text = hasHandItem ? "[" + model.DownPrimaryGlyph + "] 내려놓기" : string.Empty;

            mapHintText.text = "[" + model.MapGlyph + "] 닫기  ·  지도는 게임을 멈추지 않습니다";
            mapOverlay.SetActive(visible && model.MapOpen);
            if (renderedMapVersion != model.MapVersion)
            {
                renderedMapVersion = model.MapVersion;
                RebuildMap(model);
            }

            stageNameText.text = model.StageName;
            stageNameText.gameObject.SetActive(visible && model.StageNameVisible);
            fadeCurtain.color = new Color(0f, 0f, 0f, Mathf.Clamp01(model.FadeOpacity));
            fadeCurtain.gameObject.SetActive(model.FadeOpacity > 0.001f);
        }

        private void ApplyEquipmentBelt(HUDModel model)
        {
            int count = model.Equipment.Count;
            equipmentBeltRoot.gameObject.SetActive(count > 0);
            if (count == 0)
            {
                return;
            }

            int selectedIndex = 0;
            for (int index = 0; index < count; index++)
            {
                if (model.Equipment[index].IsSelected)
                {
                    selectedIndex = index;
                    break;
                }
            }
            int firstIndex = count <= equipmentSlotObjects.Length
                ? 0
                : Mathf.Clamp(selectedIndex - 2, 0, count - equipmentSlotObjects.Length);

            for (int slotIndex = 0; slotIndex < equipmentSlotObjects.Length; slotIndex++)
            {
                int entryIndex = firstIndex + slotIndex;
                bool visible = entryIndex < count;
                equipmentSlotObjects[slotIndex].SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                EquipmentInventoryHudEntry entry = model.Equipment[entryIndex];
                equipmentSlotIcons[slotIndex].sprite = entry.Icon;
                equipmentSlotIcons[slotIndex].color = entry.IsBroken
                    ? new Color32(226, 91, 91, 255)
                    : Color.white;
                equipmentSlotIcons[slotIndex].gameObject.SetActive(entry.Icon != null);
                string useGlyph = entry.IsSelected
                    ? entry.UseKind == EquipmentInventoryUseKind.Jump ? "[SPACE] "
                        : entry.UseKind == EquipmentInventoryUseKind.Primary ? "[X] "
                        : string.Empty
                    : string.Empty;
                string overflowLeft = firstIndex > 0 && slotIndex == 0 ? "... " : string.Empty;
                string overflowRight = firstIndex + equipmentSlotObjects.Length < count
                    && slotIndex == equipmentSlotObjects.Length - 1 ? " ..." : string.Empty;
                equipmentSlotLabels[slotIndex].text = overflowLeft + useGlyph
                    + (entry.IsBroken
                        ? "BROKEN"
                        : entry.MaximumDurability > 0
                            ? entry.CurrentDurability + "/" + entry.MaximumDurability
                            : entry.DisplayName)
                    + overflowRight;
                bool showDurability = entry.MaximumDurability > 0;
                equipmentDurabilityFills[slotIndex].transform.parent.gameObject.SetActive(showDurability);
                if (showDurability)
                {
                    float ratio = Mathf.Clamp01((float)entry.CurrentDurability / entry.MaximumDurability);
                    equipmentDurabilityFills[slotIndex].rectTransform.anchorMax = new Vector2(ratio, 1f);
                    equipmentDurabilityFills[slotIndex].color = entry.CurrentDurability <= 0
                        ? new Color32(226, 91, 91, 255)
                        : Cyan;
                }

                equipmentSlotPanels[slotIndex].color = entry.IsSelected ? new Color32(83, 71, 40, 245) : new Color32(20, 35, 52, 235);
                equipmentSlotObjects[slotIndex].transform.localScale = entry.IsSelected ? Vector3.one * 1.2f : Vector3.one;
                equipmentSlotObjects[slotIndex].transform.SetSiblingIndex(entry.IsSelected ? equipmentSlotObjects.Length : slotIndex);
            }
        }

        private static bool HasSelectedSpringJump(HUDModel model)
        {
            for (int index = 0; index < model.Equipment.Count; index++)
            {
                EquipmentInventoryHudEntry entry = model.Equipment[index];
                if (entry.IsSelected
                    && !entry.IsBroken
                    && entry.UseKind == EquipmentInventoryUseKind.Jump)
                {
                    return true;
                }
            }
            return false;
        }

        private void RebuildMap(HUDModel model)
        {
            for (int index = mapGraphRoot.childCount - 1; index >= 0; index--)
            {
                GameObject child = mapGraphRoot.GetChild(index).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }

            if (model.Rooms.Count == 0)
            {
                return;
            }

            float minX = model.Rooms[0].Center.x;
            float maxX = minX;
            float minY = model.Rooms[0].Center.y;
            float maxY = minY;
            for (int index = 1; index < model.Rooms.Count; index++)
            {
                Vector2 center = model.Rooms[index].Center;
                minX = Mathf.Min(minX, center.x);
                maxX = Mathf.Max(maxX, center.x);
                minY = Mathf.Min(minY, center.y);
                maxY = Mathf.Max(maxY, center.y);
            }

            var positions = new Dictionary<string, Vector2>(model.Rooms.Count);
            for (int index = 0; index < model.Rooms.Count; index++)
            {
                HUDMapRoomModel room = model.Rooms[index];
                float x = Mathf.Approximately(minX, maxX) ? 0f : Mathf.Lerp(-270f, 270f, Mathf.InverseLerp(minX, maxX, room.Center.x));
                float y = Mathf.Approximately(minY, maxY) ? 0f : Mathf.Lerp(-100f, 100f, Mathf.InverseLerp(minY, maxY, room.Center.y));
                positions[room.RoomId] = new Vector2(x, y);
            }

            for (int index = 0; index < model.Connections.Count; index++)
            {
                HUDMapConnectionModel connection = model.Connections[index];
                if (!positions.TryGetValue(connection.From, out Vector2 from) || !positions.TryGetValue(connection.To, out Vector2 to))
                {
                    continue;
                }

                Image line = CreateImage("MapLine_" + index, mapGraphRoot, new Color32(89, 130, 143, 255));
                RectTransform rect = line.rectTransform;
                Vector2 delta = to - from;
                SetAnchored(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(delta.magnitude, 5f), (from + to) * 0.5f);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }

            for (int index = 0; index < model.Rooms.Count; index++)
            {
                HUDMapRoomModel room = model.Rooms[index];
                Color color = room.IsCurrent ? Gold : new Color32(51, 91, 106, 255);
                RectTransform node = CreatePanel("MapNode_" + room.RoomId, mapGraphRoot, color);
                SetAnchored(node, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(132f, 62f), positions[room.RoomId]);
                string label = room.RoomId + (room.IsExit ? "  [출구]" : string.Empty);
                TMP_Text text = CreateText("Label", node, label, 18f, room.IsCurrent ? new Color32(8, 18, 34, 255) : TextColor, TextAlignmentOptions.Center);
                Stretch(text.rectTransform);
                text.fontStyle = FontStyles.Bold;
            }
        }

        private int CountNamedChildren(string prefix)
        {
            int count = 0;
            for (int index = 0; index < mapGraphRoot.childCount; index++)
            {
                if (mapGraphRoot.GetChild(index).name.StartsWith(prefix)) count++;
            }
            return count;
        }

        private void AnimateShake(RectTransform target, Vector2 basePosition, float until, float amplitude)
        {
            if (target == null) return;
            if (Time.unscaledTime < until)
            {
                target.anchoredPosition = basePosition + Vector2.right * (Mathf.Sin(Time.unscaledTime * 90f) * amplitude);
            }
            else if (target.anchoredPosition != basePosition)
            {
                target.anchoredPosition = basePosition;
            }
        }

        private void AnimateHeartBlink()
        {
            bool blinking = Time.unscaledTime < healthBlinkUntil;
            int phase = blinking ? Mathf.FloorToInt((Time.unscaledTime - healthBlinkStarted) / 0.1f) : -1;
            bool hidden = blinking && phase % 2 == 0;
            for (int index = 0; index < healthSlots.Length; index++)
            {
                if (healthSlots[index] == null)
                {
                    continue;
                }

                Color color = healthSlots[index].color;
                color.a = hidden ? 0.12f : index < lastHealth ? 1f : 210f / 255f;
                healthSlots[index].color = color;
            }
        }

        private void AnimateBellEdgePulse()
        {
            if (bellEdgePulse == null)
            {
                return;
            }

            float remaining = bellEdgePulseUntil - Time.unscaledTime;
            bellEdgePulse.alpha = remaining > 0f
                ? Mathf.Clamp01(remaining / 0.65f) * (0.55f + Mathf.Sin(Time.unscaledTime * 35f) * 0.2f)
                : 0f;
        }

        private static string ProgressBar(float progress)
        {
            int filled = Mathf.Clamp(Mathf.RoundToInt(progress * 4f), 0, 4);
            return new string('◆', filled) + new string('◇', 4 - filled);
        }

        private static string DirectionArrow(Vector2Int direction)
        {
            if (direction.x < 0) return "<< 왼쪽";
            if (direction.x > 0) return "오른쪽 >>";
            if (direction.y > 0) return "^^ 위쪽";
            if (direction.y < 0) return "vv 아래쪽";
            return "가까이";
        }

        private void UpdateHandLabel()
        {
            if (handText == null || string.IsNullOrWhiteSpace(currentHandItemId))
            {
                if (handText != null)
                {
                    handText.text = string.Empty;
                }
                return;
            }

            string resource = currentHandResourceVisible
                ? currentHandResource + "/" + currentHandResourceMaximum
                : string.Empty;
            string name = Time.unscaledTime < handNameVisibleUntil
                ? currentHandDisplayName
                : string.Empty;
            handText.text = string.IsNullOrEmpty(name)
                ? resource
                : string.IsNullOrEmpty(resource) ? name : name + "  " + resource;
        }

        private TMP_Text CreateText(string objectName, RectTransform parent, string value, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = hudFont != null ? hudFont : TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreatePanel(string objectName, RectTransform parent, Color color)
        {
            Image image = CreateImage(objectName, parent, color);
            return image.rectTransform;
        }

        private static Image CreateImage(string objectName, RectTransform parent, Color color)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Sprite CreateHeartSprite()
        {
            const int width = 28;
            const int height = 26;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "RuntimeHudHeart",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float normalizedY = ((y + 0.5f) / height) * 2f - 1f;
                for (int x = 0; x < width; x++)
                {
                    float normalizedX = ((x + 0.5f) / width) * 2f - 1f;
                    bool leftLobe = (normalizedX + 0.36f) * (normalizedX + 0.36f) +
                                    (normalizedY - 0.32f) * (normalizedY - 0.32f) < 0.48f * 0.48f;
                    bool rightLobe = (normalizedX - 0.36f) * (normalizedX - 0.36f) +
                                     (normalizedY - 0.32f) * (normalizedY - 0.32f) < 0.48f * 0.48f;
                    bool lowerPoint = normalizedY <= 0.32f && normalizedY >= -0.94f &&
                                      Mathf.Abs(normalizedX) < (normalizedY + 0.94f) * 0.79f;
                    pixels[y * width + x] = leftLobe || rightLobe || lowerPoint
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 28f);
            sprite.name = "RuntimeHudHeart";
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float bottom = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}

#endif
