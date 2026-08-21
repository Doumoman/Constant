#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using StarNight.Core.Flow;
using StarNight.Core.Save;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace StarNight.UI.Menus
{
    [DisallowMultipleComponent]
    public sealed class TitleMenuController : MonoBehaviour
    {
        private static readonly string[] Labels =
        {
            "새 여행",
            "설정",
            "조작법",
            "기록",
            "크레딧",
            "게임 종료",
        };

        [SerializeField] private TMP_FontAsset titleFont;

        private readonly List<Button> menuButtons = new();
        private GameFlowController gameFlow;
        private TMP_Text statusText;
        private SettingsController settings;
        private bool isStartingRun;

        public int MenuItemCount => menuButtons.Count;
        public bool IsStartingRun => isStartingRun;
        public string CurrentStatus => statusText == null ? string.Empty : statusText.text;

        private void Awake()
        {
            BuildScreen();
            settings = gameObject.GetComponent<SettingsController>();
            if (settings == null)
            {
                settings = gameObject.AddComponent<SettingsController>();
            }
            settings.Configure(titleFont);
            settings.Closed += HandleSettingsClosed;
        }

        private void OnDestroy()
        {
            if (settings != null)
            {
                settings.Closed -= HandleSettingsClosed;
            }
        }

        private IEnumerator Start()
        {
            ResolveGameFlow();
            yield return null;

            if (menuButtons.Count > 0 && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(menuButtons[0].gameObject);
            }
        }

        private void Update()
        {
            if (isStartingRun || settings?.IsOpen == true || menuButtons.Count == 0)
            {
                return;
            }

            bool confirmPressed = Keyboard.current?.xKey.wasPressedThisFrame == true
                || Gamepad.current?.buttonWest.wasPressedThisFrame == true;

            if (!confirmPressed || EventSystem.current == null)
            {
                return;
            }

            GameObject selected = EventSystem.current.currentSelectedGameObject;
            Button button = selected == null ? null : selected.GetComponent<Button>();
            button?.onClick.Invoke();
        }

        public string GetMenuLabel(int index)
        {
            return index >= 0 && index < Labels.Length ? Labels[index] : string.Empty;
        }

        public void InvokeMenuItem(int index)
        {
            if (index >= 0 && index < menuButtons.Count)
            {
                menuButtons[index].onClick.Invoke();
            }
        }

        private void ResolveGameFlow()
        {
            if (GameBootstrap.Instance == null || GameBootstrap.Instance.Services == null)
            {
                SetStatus("공용 서비스를 준비하고 있습니다.");
                return;
            }

            if (!GameBootstrap.Instance.Services.TryGet(out gameFlow))
            {
                SetStatus("여행 흐름 서비스를 찾을 수 없습니다.");
            }
        }

        private void BeginNewJourney()
        {
            if (isStartingRun)
            {
                return;
            }

            ResolveGameFlow();
            if (gameFlow == null || !gameFlow.StartNewRun())
            {
                SetStatus("별길이 열릴 때까지 잠시 기다려 주세요.");
                return;
            }

            isStartingRun = true;
            SetButtonsInteractable(false);
            SetStatus("별길을 여는 중…");
        }

        private void ShowPending(string message)
        {
            SetStatus(message);
        }

        private void RequestQuit()
        {
            SetStatus("여행선을 정박합니다.");
            Application.Quit();
        }

        private void HandleSettingsClosed()
        {
            SetButtonsInteractable(true);
            SetStatus("설정을 반영했습니다.  ·  X 결정  ·  방향키 선택");
            if (menuButtons.Count > 1 && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(menuButtons[1].gameObject);
            }
        }

        private void BuildScreen()
        {
            EnsureEventSystem();

            GameObject canvasObject = new("TitleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = Camera.main == null ? RenderMode.ScreenSpaceOverlay : RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRect);

            Image background = CreateImage("NightGradient", canvasRect, new Color32(7, 13, 31, 255));
            Stretch(background.rectTransform);

            Image glow = CreateImage("MoonGlow", canvasRect, new Color32(24, 60, 83, 180));
            SetRect(glow.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.58f, 0.92f), Vector2.zero, Vector2.zero);

            Image rail = CreateImage("GoldRail", canvasRect, new Color32(223, 188, 104, 230));
            SetRect(rail.rectTransform, new Vector2(0.59f, 0.12f), new Vector2(0.592f, 0.88f), Vector2.zero, Vector2.zero);

            TMP_Text eyebrow = CreateText("Eyebrow", canvasRect, "NIGHT VOYAGE  ·  별길 운항 준비", 24f, new Color32(164, 198, 208, 255));
            SetRect(eyebrow.rectTransform, new Vector2(0.1f, 0.75f), new Vector2(0.55f, 0.82f), Vector2.zero, Vector2.zero);
            eyebrow.alignment = TextAlignmentOptions.Left;

            TMP_Text title = CreateText("GameTitle", canvasRect, "별을\n물어오는 밤", 86f, new Color32(246, 232, 190, 255));
            SetRect(title.rectTransform, new Vector2(0.1f, 0.43f), new Vector2(0.55f, 0.75f), Vector2.zero, Vector2.zero);
            title.alignment = TextAlignmentOptions.Left;
            title.fontStyle = FontStyles.Bold;
            title.lineSpacing = -12f;

            TMP_Text subtitle = CreateText("Subtitle", canvasRect, "세 번째 방울이 울리기 전에, 별가루 출항문으로.", 28f, new Color32(190, 205, 208, 255));
            SetRect(subtitle.rectTransform, new Vector2(0.1f, 0.32f), new Vector2(0.54f, 0.43f), Vector2.zero, Vector2.zero);
            subtitle.alignment = TextAlignmentOptions.Left;

            GameObject menuObject = new("MenuList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            menuObject.transform.SetParent(canvasRect, false);
            RectTransform menuRect = menuObject.GetComponent<RectTransform>();
            SetRect(menuRect, new Vector2(0.65f, 0.22f), new Vector2(0.9f, 0.79f), Vector2.zero, Vector2.zero);

            VerticalLayoutGroup layout = menuObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 13f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            for (int index = 0; index < Labels.Length; index++)
            {
                int captured = index;
                Button button = CreateMenuButton(menuRect, Labels[index]);
                button.onClick.AddListener(() => InvokeAction(captured));
                menuButtons.Add(button);
            }

            statusText = CreateText("Status", canvasRect, "X 결정  ·  Z / Esc 취소  ·  방향키 선택", 22f, new Color32(157, 184, 192, 255));
            SetRect(statusText.rectTransform, new Vector2(0.62f, 0.1f), new Vector2(0.93f, 0.18f), Vector2.zero, Vector2.zero);
            statusText.alignment = TextAlignmentOptions.Center;

            TMP_Text version = CreateText("Version", canvasRect, "CORE 02  ·  첫 항해 준비", 18f, new Color32(94, 120, 132, 255));
            SetRect(version.rectTransform, new Vector2(0.08f, 0.06f), new Vector2(0.32f, 0.11f), Vector2.zero, Vector2.zero);
            version.alignment = TextAlignmentOptions.Left;
        }

        private void InvokeAction(int index)
        {
            switch (index)
            {
                case 0:
                    BeginNewJourney();
                    break;
                case 1:
                    SetButtonsInteractable(false);
                    settings.Open();
                    break;
                case 2:
                    ShowPending("조작법은 입력 기준 확정 후 연결됩니다.");
                    break;
                case 3:
                    ShowRecords();
                    break;
                case 4:
                    ShowPending("크레딧 화면은 후속 UI 분기에서 연결됩니다.");
                    break;
                case 5:
                    RequestQuit();
                    break;
            }
        }

        private void ShowRecords()
        {
            if (!GameBootstrap.IsReady ||
                !GameBootstrap.Instance.Services.TryGet(out RunRecordRepository repository) ||
                repository.Current == null || repository.Current.TotalRunCount == 0)
            {
                ShowPending("아직 저장된 여행 기록이 없습니다.");
                return;
            }

            RunRecordData records = repository.Current;
            string bestTime = records.bestClearedRunTime <= 0f
                ? "--:--"
                : $"{Mathf.FloorToInt(records.bestClearedRunTime) / 60:00}:{Mathf.FloorToInt(records.bestClearedRunTime) % 60:00}";
            ShowPending(
                $"본 엔딩 {records.viewedEndingIds.Count}  ·  최고 도달 {records.highestReachedStage}  ·  " +
                $"최고 기록 {bestTime}  ·  기록 길손 {records.metMemoryTravelerIds.Count}  ·  설화 그림 {records.discoveredFolkloreIds.Count}");
        }

        private Button CreateMenuButton(RectTransform parent, string label)
        {
            GameObject buttonObject = new(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color32(14, 26, 47, 235);

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredHeight = 70f;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color32(14, 26, 47, 235);
            colors.highlightedColor = new Color32(30, 69, 87, 255);
            colors.selectedColor = new Color32(49, 91, 102, 255);
            colors.pressedColor = new Color32(215, 179, 93, 255);
            colors.disabledColor = new Color32(20, 24, 34, 150);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            TMP_Text text = CreateText("Label", buttonObject.GetComponent<RectTransform>(), label, 29f, new Color32(239, 229, 197, 255));
            Stretch(text.rectTransform, 20f, 20f, 0f, 0f);
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;

            return button;
        }

        private TMP_Text CreateText(string objectName, RectTransform parent, string value, float fontSize, Color color)
        {
            GameObject textObject = new(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = titleFont != null ? titleFont : TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.color = color;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(string objectName, RectTransform parent, Color color)
        {
            GameObject imageObject = new(objectName, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private void SetButtonsInteractable(bool value)
        {
            foreach (Button button in menuButtons)
            {
                button.interactable = value;
            }
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
            {
                statusText.text = value;
            }
        }

        private static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float bottom = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}

#endif
