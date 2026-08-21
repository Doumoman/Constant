#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Core.Flow;
using StarNight.Interaction.Input;
using StarNight.Stage.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StarNight.UI.Menus
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuController : MonoBehaviour
    {
        private static readonly string[] Labels =
        {
            "계속하기",
            "조작법",
            "설정",
            "현재 스테이지 다시 시작",
            "현재 여행 포기",
            "타이틀로",
        };

        [SerializeField] private TMP_FontAsset menuFont;

        private readonly List<Button> buttons = new();
        private GameFlowController gameFlow;
        private StageFlowController stageFlow;
        private GameplayInputReader inputReader;
        private PlayerInputContext previousContext = PlayerInputContext.Gameplay;
        private SettingsController settings;
        private GameObject screen;
        private GameObject mainPanel;
        private GameObject messagePanel;
        private TMP_Text messageTitle;
        private TMP_Text messageBody;
        private TMP_Text statusText;
        private Action confirmedAction;
        private int selectedIndex;
        private int previousNavigateSign;
        private bool pauseRequested;

        public bool IsOpen => screen != null && screen.activeSelf;
        public int MenuItemCount => buttons.Count;
        public string CurrentStatus => statusText == null ? string.Empty : statusText.text;
        public SettingsController Settings => settings;

        public void Configure(TMP_FontAsset font)
        {
            menuFont = font;
            if (screen != null && font != null)
            {
                foreach (TMP_Text text in screen.GetComponentsInChildren<TMP_Text>(true))
                {
                    text.font = font;
                }
                settings?.Configure(font, inputReader);
            }
        }

        private void Awake()
        {
            EnsureEventSystem();
            BuildScreen();
            settings = gameObject.AddComponent<SettingsController>();
            settings.Configure(menuFont, null);
            settings.Closed += HandleSettingsClosed;
            screen.SetActive(false);
        }

        private void Start()
        {
            ResolveServicesAndInput();
        }

        private void OnDestroy()
        {
            if (inputReader != null)
            {
                inputReader.PauseRequested -= HandlePauseRequested;
            }
            if (settings != null)
            {
                settings.Closed -= HandleSettingsClosed;
            }
        }

        private void Update()
        {
            ResolveServicesAndInput();
            if (pauseRequested)
            {
                pauseRequested = false;
                if (!IsOpen)
                {
                    Open();
                }
            }

            if (!IsOpen || settings.IsOpen || inputReader == null || inputReader.Context != PlayerInputContext.Menu)
            {
                return;
            }

            if (messagePanel.activeSelf)
            {
                if (inputReader.ConsumeMenuSubmitPressed())
                {
                    Action action = confirmedAction;
                    HideMessage();
                    action?.Invoke();
                }
                else if (inputReader.ConsumeMenuCancelPressed())
                {
                    HideMessage();
                }
                return;
            }

            int navigate = Mathf.Abs(inputReader.MenuNavigate.y) > 0.55f ? Math.Sign(inputReader.MenuNavigate.y) : 0;
            if (navigate != 0 && previousNavigateSign == 0)
            {
                selectedIndex = Wrap(selectedIndex - navigate, buttons.Count);
                SelectCurrent();
            }
            previousNavigateSign = navigate;

            if (inputReader.ConsumeMenuSubmitPressed())
            {
                buttons[selectedIndex].onClick.Invoke();
            }
            if (inputReader.ConsumeMenuCancelPressed())
            {
                Resume();
            }
        }

        public string GetMenuLabel(int index)
        {
            return index >= 0 && index < Labels.Length ? Labels[index] : string.Empty;
        }

        public void InvokeMenuItem(int index)
        {
            if (index >= 0 && index < buttons.Count)
            {
                buttons[index].onClick.Invoke();
            }
        }

        public bool Open()
        {
            ResolveServicesAndInput();
            if (IsOpen || gameFlow == null || inputReader == null || !gameFlow.TryPause())
            {
                return false;
            }

            previousContext = inputReader.Context;
            inputReader.ClearBufferedButtons();
            inputReader.SetContext(PlayerInputContext.Menu);
            selectedIndex = 0;
            previousNavigateSign = 0;
            statusText.text = "X / Enter 결정 · Z / Esc 뒤로";
            mainPanel.SetActive(true);
            messagePanel.SetActive(false);
            screen.SetActive(true);
            SelectCurrent();
            return true;
        }

        public bool Resume()
        {
            if (!IsOpen || gameFlow == null || !gameFlow.TryResume())
            {
                return false;
            }

            CloseVisuals(true);
            return true;
        }

        private void ResolveServicesAndInput()
        {
            if (GameBootstrap.IsReady)
            {
                GameBootstrap.Instance.Services.TryGet(out gameFlow);
                GameBootstrap.Instance.Services.TryGet(out stageFlow);
            }

            GameplayInputReader resolved = FindFirstObjectByType<GameplayInputReader>();
            if (resolved == null || resolved == inputReader)
            {
                return;
            }

            if (inputReader != null)
            {
                inputReader.PauseRequested -= HandlePauseRequested;
            }
            inputReader = resolved;
            inputReader.ApplyBindingOverrides(GameBootstrap.Instance?.Settings?.inputBindingOverridesJson);
            inputReader.PauseRequested += HandlePauseRequested;
            settings?.Configure(menuFont, inputReader);
        }

        private void HandlePauseRequested()
        {
            pauseRequested = true;
        }

        private void InvokeAction(int index)
        {
            switch (index)
            {
                case 0:
                    Resume();
                    break;
                case 1:
                    ShowMessage("조작법", "방향키  이동 / 보기\nSpace  점프\nX  대화 · 조사 · 집기 · 사용\n↓ + X  내려놓기\nZ  폭탄\nC  로프\nTab  방 지도\nEsc  일시정지", null);
                    break;
                case 2:
                    mainPanel.SetActive(false);
                    settings.Open();
                    break;
                case 3:
                    RestartStage();
                    break;
                case 4:
                    ShowMessage(
                        "현재 여행 포기",
                        "현재 여행의 진행 상황은 저장되지 않습니다.\n처음부터 다시 떠나시겠습니까?",
                        RestartJourney);
                    break;
                case 5:
                    ReturnToTitle();
                    break;
            }
        }

        private void RestartStage()
        {
            if (stageFlow == null || !stageFlow.CanRestartCurrentStage)
            {
                statusText.text = "지금은 스테이지를 다시 시작할 수 없습니다.";
                return;
            }

            CloseVisuals(true);
            if (!stageFlow.RequestRestartCurrentStage())
            {
                Open();
                statusText.text = "스테이지 재시작 요청을 처리하지 못했습니다.";
            }
        }

        private void RestartJourney()
        {
            CloseVisuals(true);
            if (gameFlow == null || !gameFlow.RestartRun())
            {
                Open();
                statusText.text = "새 여행을 준비할 수 없습니다.";
            }
        }

        private void ReturnToTitle()
        {
            CloseVisuals(true);
            if (gameFlow == null || !gameFlow.ReturnToTitle())
            {
                Open();
                statusText.text = "타이틀로 돌아갈 수 없습니다.";
            }
        }

        private void ShowMessage(string title, string body, Action action)
        {
            confirmedAction = action;
            messageTitle.text = title;
            messageBody.text = body + (action == null ? "\n\nZ / Esc로 돌아가기" : "\n\nX / Enter 확인  ·  Z / Esc 취소");
            mainPanel.SetActive(false);
            messagePanel.SetActive(true);
        }

        private void HideMessage()
        {
            confirmedAction = null;
            messagePanel.SetActive(false);
            mainPanel.SetActive(true);
            SelectCurrent();
        }

        private void HandleSettingsClosed()
        {
            if (!IsOpen)
            {
                return;
            }
            mainPanel.SetActive(true);
            inputReader?.SetContext(PlayerInputContext.Menu);
            SelectCurrent();
        }

        private void CloseVisuals(bool restoreInput)
        {
            confirmedAction = null;
            messagePanel.SetActive(false);
            mainPanel.SetActive(false);
            screen.SetActive(false);
            if (restoreInput && inputReader != null)
            {
                inputReader.ClearBufferedButtons();
                inputReader.SetContext(previousContext);
            }
        }

        private void SelectCurrent()
        {
            if (buttons.Count > 0)
            {
                EventSystem.current?.SetSelectedGameObject(buttons[Mathf.Clamp(selectedIndex, 0, buttons.Count - 1)].gameObject);
            }
        }

        private void BuildScreen()
        {
            screen = new GameObject("PauseScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            screen.transform.SetParent(transform, false);
            Canvas canvas = screen.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            CanvasScaler scaler = screen.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Stretch(screen.GetComponent<RectTransform>());

            Image dim = CreateImage("DimmedGameplay", screen.transform, new Color32(3, 7, 17, 210));
            Stretch(dim.rectTransform);
            Image goldRail = CreateImage("GoldRail", screen.transform, new Color32(217, 181, 90, 235));
            SetRect(goldRail.rectTransform, new Vector2(0.605f, 0.12f), new Vector2(0.607f, 0.88f));

            mainPanel = new GameObject("PauseMenu", typeof(RectTransform));
            mainPanel.transform.SetParent(screen.transform, false);
            Stretch(mainPanel.GetComponent<RectTransform>());

            TMP_Text eyebrow = CreateText("Eyebrow", mainPanel.transform, "VOYAGE SUSPENDED  ·  별길 정박", 23f, new Color32(153, 198, 205, 255));
            SetRect(eyebrow.rectTransform, new Vector2(0.1f, 0.76f), new Vector2(0.55f, 0.83f));
            eyebrow.alignment = TextAlignmentOptions.Left;
            TMP_Text title = CreateText("Title", mainPanel.transform, "잠시 쉬어가기", 70f, new Color32(246, 232, 190, 255));
            SetRect(title.rectTransform, new Vector2(0.1f, 0.59f), new Vector2(0.56f, 0.76f));
            title.alignment = TextAlignmentOptions.Left;
            title.fontStyle = FontStyles.Bold;
            TMP_Text note = CreateText("Note", mainPanel.transform, "물리 · 타이머 · 대사가 함께 멈춰 있습니다.", 25f, new Color32(179, 199, 203, 255));
            SetRect(note.rectTransform, new Vector2(0.1f, 0.49f), new Vector2(0.55f, 0.58f));
            note.alignment = TextAlignmentOptions.Left;

            RectTransform list = CreateVerticalRoot("MenuList", mainPanel.transform, new Vector2(0.65f, 0.2f), new Vector2(0.9f, 0.82f), 10f);
            for (int index = 0; index < Labels.Length; index++)
            {
                int captured = index;
                Button button = CreateButton(Labels[index], list, Labels[index]);
                button.onClick.AddListener(() => InvokeAction(captured));
                buttons.Add(button);
            }

            statusText = CreateText("Status", mainPanel.transform, string.Empty, 21f, new Color32(150, 183, 191, 255));
            SetRect(statusText.rectTransform, new Vector2(0.63f, 0.1f), new Vector2(0.92f, 0.17f));
            statusText.alignment = TextAlignmentOptions.Center;

            messagePanel = new GameObject("MessagePanel", typeof(RectTransform));
            messagePanel.transform.SetParent(screen.transform, false);
            Stretch(messagePanel.GetComponent<RectTransform>());
            Image card = CreateImage("Card", messagePanel.transform, new Color32(12, 25, 43, 255));
            SetRect(card.rectTransform, new Vector2(0.25f, 0.22f), new Vector2(0.75f, 0.78f));
            messageTitle = CreateText("MessageTitle", card.transform, string.Empty, 42f, new Color32(246, 232, 190, 255));
            SetRect(messageTitle.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.9f));
            messageTitle.alignment = TextAlignmentOptions.Center;
            messageTitle.fontStyle = FontStyles.Bold;
            messageBody = CreateText("MessageBody", card.transform, string.Empty, 27f, new Color32(205, 219, 215, 255));
            SetRect(messageBody.rectTransform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.7f));
            messageBody.enableWordWrapping = true;
            messageBody.alignment = TextAlignmentOptions.Center;
            messagePanel.SetActive(false);
        }

        private Button CreateButton(string name, Transform parent, string label)
        {
            GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<LayoutElement>().preferredHeight = 67f;
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color32(15, 30, 51, 245);
            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color32(15, 30, 51, 245);
            colors.highlightedColor = new Color32(35, 72, 88, 255);
            colors.selectedColor = new Color32(49, 93, 102, 255);
            colors.pressedColor = new Color32(211, 175, 87, 255);
            button.colors = colors;
            TMP_Text text = CreateText("Label", buttonObject.transform, label, 26f, new Color32(239, 229, 197, 255));
            Stretch(text.rectTransform, 14f, 14f, 0f, 0f);
            text.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private TMP_Text CreateText(string name, Transform parent, string value, float size, Color color)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = menuFont != null ? menuFont : TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.color = color;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform CreateVerticalRoot(string name, Transform parent, Vector2 min, Vector2 max, float spacing)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetRect(rect, min, max);
            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return rect;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                new GameObject("EventSystem", typeof(EventSystem));
            }
        }

        private static int Wrap(int value, int count) => (value % count + count) % count;

        private static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float bottom = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

#endif
