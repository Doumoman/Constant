#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Core.Flow;
using StarNight.Core.Save;
using StarNight.Interaction.Input;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StarNight.UI.Menus
{
    [DisallowMultipleComponent]
    public sealed class SettingsController : MonoBehaviour
    {
        private static readonly string[] CategoryLabels = { "오디오", "화면", "게임플레이", "접근성", "키 설정" };

        private readonly List<Button> categoryButtons = new();
        private readonly List<Button> activeButtons = new();
        private readonly Dictionary<Button, Action<int>> adjusters = new();
        private readonly List<GameObject> dynamicRows = new();

        [SerializeField] private TMP_FontAsset font;
        private GameplayInputReader inputReader;
        private GameplayInputReader ownedInputReader;
        private InputRebindController rebindController;
        private GameObject screen;
        private RectTransform listRoot;
        private TMP_Text categoryTitle;
        private TMP_Text statusText;
        private Button applyButton;
        private Button cancelButton;
        private SettingsData working;
        private int categoryIndex;
        private int selectedIndex;
        private int previousNavigateSign;
        private bool useGamepadBindings;

        public bool IsOpen => screen != null && screen.activeSelf;
        public SettingsData Working => working;
        public int CategoryIndex => categoryIndex;
        public string Status => statusText == null ? string.Empty : statusText.text;

        public event Action Closed;

        public void Configure(TMP_FontAsset settingsFont, GameplayInputReader reader = null)
        {
            font = settingsFont;
            inputReader = reader;
            if (screen != null && font != null)
            {
                foreach (TMP_Text text in screen.GetComponentsInChildren<TMP_Text>(true))
                {
                    text.font = font;
                }
            }
        }

        private void Awake()
        {
            rebindController = gameObject.AddComponent<InputRebindController>();
            rebindController.BindingChanged += HandleBindingChanged;
            BuildScreen();
            screen.SetActive(false);
        }

        private void OnDestroy()
        {
            if (rebindController != null)
            {
                rebindController.BindingChanged -= HandleBindingChanged;
            }
        }

        private void Update()
        {
            if (!IsOpen || rebindController.IsWaiting || ResolveInputReader() == null)
            {
                return;
            }

            Vector2 navigate = inputReader.MenuNavigate;
            int vertical = Mathf.Abs(navigate.y) > 0.55f ? Math.Sign(navigate.y) : 0;
            if (vertical != 0 && previousNavigateSign == 0)
            {
                SelectOffset(-vertical);
            }
            previousNavigateSign = vertical;

            if (Mathf.Abs(navigate.x) > 0.55f && TryGetSelectedButton(out Button horizontalButton)
                && adjusters.TryGetValue(horizontalButton, out Action<int> adjust))
            {
                int horizontal = Math.Sign(navigate.x);
                if (previousNavigateSign == 0)
                {
                    adjust(horizontal);
                }
                previousNavigateSign = horizontal;
            }

            if (inputReader.ConsumeMenuSubmitPressed() && TryGetSelectedButton(out Button selected))
            {
                selected.onClick.Invoke();
            }
            if (inputReader.ConsumeMenuCancelPressed())
            {
                Close(false);
            }
        }

        public void Open()
        {
            if (IsOpen || !GameBootstrap.IsReady)
            {
                return;
            }

            working = Clone(GameBootstrap.Instance.Settings);
            ResolveInputReader();
            inputReader?.ApplyBindingOverrides(working.inputBindingOverridesJson);
            if (inputReader != null)
            {
                inputReader.SetContext(PlayerInputContext.Menu);
            }
            categoryIndex = 0;
            previousNavigateSign = 0;
            screen.SetActive(true);
            BuildCategory();
        }

        public void Close(bool apply)
        {
            if (!IsOpen)
            {
                return;
            }

            rebindController.Cancel();
            if (apply)
            {
                if (inputReader != null)
                {
                    working.inputBindingOverridesJson = inputReader.SaveBindingOverrides();
                }
                GameBootstrap.Instance.SaveSettings(working);
            }
            else
            {
                GameBootstrap.Instance.RestoreSavedSettingsPreview();
                inputReader?.ApplyBindingOverrides(GameBootstrap.Instance.Settings.inputBindingOverridesJson);
            }

            screen.SetActive(false);
            working = null;
            Closed?.Invoke();
        }

        public void SelectCategory(int index)
        {
            if (!IsOpen)
            {
                return;
            }
            categoryIndex = Mathf.Clamp(index, 0, CategoryLabels.Length - 1);
            BuildCategory();
        }

        private GameplayInputReader ResolveInputReader()
        {
            if (inputReader != null)
            {
                return inputReader;
            }

            inputReader = FindFirstObjectByType<GameplayInputReader>();
            if (inputReader == null)
            {
                GameObject inputObject = new("SettingsInputPreview");
                inputObject.transform.SetParent(transform, false);
                ownedInputReader = inputObject.AddComponent<GameplayInputReader>();
                inputReader = ownedInputReader;
            }
            return inputReader;
        }

        private void BuildCategory()
        {
            foreach (GameObject row in dynamicRows)
            {
                Destroy(row);
            }
            dynamicRows.Clear();
            activeButtons.Clear();
            adjusters.Clear();
            activeButtons.AddRange(categoryButtons);
            categoryTitle.text = CategoryLabels[categoryIndex];
            statusText.text = "X / Enter 변경 · Z / Esc 뒤로 · 적용해야 저장됩니다.";

            switch (categoryIndex)
            {
                case 0: BuildAudio(); break;
                case 1: BuildDisplay(); break;
                case 2: BuildGameplay(); break;
                case 3: BuildAccessibility(); break;
                case 4: BuildBindings(); break;
            }

            activeButtons.Add(applyButton);
            activeButtons.Add(cancelButton);
            selectedIndex = Mathf.Clamp(categoryIndex, 0, activeButtons.Count - 1);
            SelectCurrent();
        }

        private void BuildAudio()
        {
            AddInt("전체 음량", () => working.audio.masterVolume, value => working.audio.masterVolume = value, 0, 10);
            AddInt("배경음", () => working.audio.bgmVolume, value => working.audio.bgmVolume = value, 0, 10);
            AddInt("효과음", () => working.audio.sfxVolume, value => working.audio.sfxVolume = value, 0, 10);
            AddInt("대사 글자음", () => working.audio.dialogueVolume, value => working.audio.dialogueVolume = value, 0, 10);
            AddInt("UI 효과음", () => working.audio.uiVolume, value => working.audio.uiVolume = value, 0, 10);
            AddBool("백그라운드 음소거", () => working.audio.muteInBackground, value => working.audio.muteInBackground = value);
        }

        private void BuildDisplay()
        {
            AddEnum("화면 모드", () => working.display.screenMode, value => working.display.screenMode = value,
                new[] { "전체 화면", "창 모드", "테두리 없음" });
            AddBool("모니터 권장 해상도", () => working.display.useRecommendedResolution, value => working.display.useRecommendedResolution = value);
            AddBool("수직동기화", () => working.display.verticalSync, value => working.display.verticalSync = value);
            int[] frameLimits = { 30, 60, 120, 144 };
            AddChoice("프레임 제한", () => Array.IndexOf(frameLimits, working.display.frameLimit), index => working.display.frameLimit = frameLimits[index],
                new[] { "30", "60", "120", "144" });
            AddPercent("카메라 흔들림", () => working.display.cameraShakePercent, value => working.display.cameraShakePercent = value);
            AddPercent("패럴랙스 강도", () => working.display.parallaxPercent, value => working.display.parallaxPercent = value);
            AddPercent("화면 번쩍임", () => working.display.flashPercent, value => working.display.flashPercent = value);
            AddEnum("방 전환 속도", () => working.display.roomTransitionSpeed, value => working.display.roomTransitionSpeed = value,
                new[] { "느림 · 0.38초", "보통 · 0.28초", "빠름 · 0.18초", "즉시" });
        }

        private void BuildGameplay()
        {
            AddBool("출구 방향 항상 표시", () => working.gameplay.alwaysShowExitDirection, value => working.gameplay.alwaysShowExitDirection = value);
            AddBool("상호작용 버튼 표시", () => working.gameplay.showInteractionPrompt, value => working.gameplay.showInteractionPrompt = value);
            AddBool("튜토리얼 힌트", () => working.gameplay.showTutorialHints, value => working.gameplay.showTutorialHints = value);
            AddEnum("대사 속도", () => working.gameplay.dialogueSpeed, value => working.gameplay.dialogueSpeed = value,
                new[] { "느림 · 20자/초", "보통 · 35자/초", "빠름 · 55자/초" }, new[] { DialogueSpeed.Slow, DialogueSpeed.Normal, DialogueSpeed.Fast });
            AddBool("자동 대사 진행", () => working.gameplay.autoAdvanceDialogue, value => working.gameplay.autoAdvanceDialogue = value);
            AddBool("진동", () => working.gameplay.vibration, value => working.gameplay.vibration = value);
            AddBool("타이머 숫자 표시", () => working.gameplay.showTimerNumbers, value => working.gameplay.showTimerNumbers = value);
        }

        private void BuildAccessibility()
        {
            AddBool("위험 오브젝트 외곽선", () => working.accessibility.hazardOutline, value => working.accessibility.hazardOutline = value);
            AddBool("상호작용 대상 고대비", () => working.accessibility.highContrastInteractions, value => working.accessibility.highContrastInteractions = value);
            AddBool("방울 시각 알림", () => working.accessibility.visualBellAlert, value => working.accessibility.visualBellAlert = value);
            AddBool("저감 카메라 흔들림", () => working.accessibility.reducedCameraShake, value => working.accessibility.reducedCameraShake = value);
            AddBool("저감 번쩍임", () => working.accessibility.reducedFlashing, value => working.accessibility.reducedFlashing = value);
            AddBool("추락 피해 제거", () => working.accessibility.removeFallDamage, value => working.accessibility.removeFallDamage = value);
            AddBool("마루 여유 시간 +25%", () => working.accessibility.extendMaruTime, value => working.accessibility.extendMaruTime = value);
            AddBool("보스 피해 0.5칸", () => working.accessibility.halfBossDamage, value => working.accessibility.halfBossDamage = value);
            AddBool("버튼 유지 대신 토글", () => working.accessibility.holdActionsAsToggle, value => working.accessibility.holdActionsAsToggle = value);
            AddBool("여행자 보조", () => working.accessibility.travelerAssist, value => working.accessibility.travelerAssist = value);
        }

        private void BuildBindings()
        {
            string device = useGamepadBindings ? "게임패드" : "키보드";
            AddRow("입력 장치", device, _ =>
            {
                useGamepadBindings = !useGamepadBindings;
                BuildCategory();
            });

            if (inputReader == null)
            {
                statusText.text = "입력 자산을 찾지 못했습니다.";
                return;
            }

            string group = useGamepadBindings ? "Gamepad" : "Keyboard";
            if (useGamepadBindings)
            {
                AddBinding("이동", "Gameplay", "MoveHorizontal", group, null);
                AddBinding("보기 / 조준", "Gameplay", "LookVertical", group, null);
            }
            else
            {
                AddBinding("이동 ←", "Gameplay", "MoveHorizontal", group, "negative");
                AddBinding("이동 →", "Gameplay", "MoveHorizontal", group, "positive");
                AddBinding("보기 ↓", "Gameplay", "LookVertical", group, "negative");
                AddBinding("보기 ↑", "Gameplay", "LookVertical", group, "positive");
            }
            AddBinding("점프", "Gameplay", "Jump", group, null);
            AddBinding("주 행동", "Gameplay", "PrimaryAction", group, null);
            AddBinding("폭탄", "Gameplay", "PlaceBomb", group, null);
            AddBinding("로프", "Gameplay", "PlaceRope", group, null);
            AddBinding("지도", "Gameplay", "OpenMap", group, null);
            AddBinding("일시정지", "Gameplay", "Pause", group, null);
            AddRow("키 설정 초기화", "기본값", _ =>
            {
                inputReader.ResetBindingOverrides();
                working.inputBindingOverridesJson = string.Empty;
                statusText.text = "방향키 · Space · X/Z/C · Esc 기본값으로 복원했습니다.";
                BuildCategory();
            });
        }

        private void AddBinding(string label, string map, string action, string group, string part)
        {
            int index = inputReader.FindBindingIndex(map, action, group, part);
            string value = inputReader.GetBindingDisplayString(map, action, index);
            AddRow(label, value, _ =>
            {
                if (rebindController.Begin(inputReader, map, action, index, group))
                {
                    statusText.text = rebindController.Status;
                }
            });
        }

        private void HandleBindingChanged()
        {
            if (working != null && inputReader != null)
            {
                working.inputBindingOverridesJson = inputReader.SaveBindingOverrides();
            }
            BuildCategory();
            statusText.text = rebindController.Status;
        }

        private void AddBool(string label, Func<bool> get, Action<bool> set)
        {
            AddRow(label, get() ? "켜기" : "끄기", _ =>
            {
                set(!get());
                PreviewAndRebuild();
            });
        }

        private void AddInt(string label, Func<int> get, Action<int> set, int min, int max)
        {
            AddRow(label, get().ToString(), direction =>
            {
                int step = direction == 0 ? 1 : direction;
                int next = get() + step;
                if (next > max) next = min;
                if (next < min) next = max;
                set(next);
                PreviewAndRebuild();
            });
        }

        private void AddPercent(string label, Func<int> get, Action<int> set)
        {
            AddRow(label, get() + "%", direction =>
            {
                int step = (direction == 0 ? 1 : direction) * 10;
                set(Mathf.Clamp(get() + step, 0, 100));
                PreviewAndRebuild();
            });
        }

        private void AddChoice(string label, Func<int> getIndex, Action<int> setIndex, string[] labels)
        {
            int current = Mathf.Clamp(getIndex(), 0, labels.Length - 1);
            AddRow(label, labels[current], direction =>
            {
                int step = direction == 0 ? 1 : direction;
                setIndex(Wrap(current + step, labels.Length));
                PreviewAndRebuild();
            });
        }

        private void AddEnum<T>(string label, Func<T> get, Action<T> set, string[] labels, T[] values = null) where T : struct, Enum
        {
            T[] options = values ?? (T[])Enum.GetValues(typeof(T));
            int current = Mathf.Max(0, Array.IndexOf(options, get()));
            AddRow(label, labels[Mathf.Clamp(current, 0, labels.Length - 1)], direction =>
            {
                int step = direction == 0 ? 1 : direction;
                set(options[Wrap(current + step, options.Length)]);
                PreviewAndRebuild();
            });
        }

        private void PreviewAndRebuild()
        {
            int keepSelection = selectedIndex;
            GameBootstrap.Instance.PreviewSettings(working);
            BuildCategory();
            selectedIndex = Mathf.Clamp(keepSelection, 0, activeButtons.Count - 1);
            SelectCurrent();
        }

        private Button AddRow(string label, string value, Action<int> adjust)
        {
            Button button = CreateButton(label + "Row", listRoot, label + "     [  " + value + "  ]", 24f, 54f);
            button.onClick.AddListener(() => adjust(0));
            dynamicRows.Add(button.gameObject);
            activeButtons.Add(button);
            adjusters[button] = adjust;
            return button;
        }

        private void SelectOffset(int delta)
        {
            if (activeButtons.Count == 0)
            {
                return;
            }
            selectedIndex = Wrap(selectedIndex + delta, activeButtons.Count);
            SelectCurrent();
        }

        private void SelectCurrent()
        {
            if (activeButtons.Count == 0)
            {
                return;
            }
            EventSystem.current?.SetSelectedGameObject(activeButtons[selectedIndex].gameObject);
        }

        private bool TryGetSelectedButton(out Button button)
        {
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            button = selected == null ? null : selected.GetComponent<Button>();
            int index = button == null ? -1 : activeButtons.IndexOf(button);
            if (index >= 0)
            {
                selectedIndex = index;
                return true;
            }
            button = activeButtons.Count > 0 ? activeButtons[Mathf.Clamp(selectedIndex, 0, activeButtons.Count - 1)] : null;
            return button != null;
        }

        private void BuildScreen()
        {
            screen = new GameObject("SettingsScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            screen.transform.SetParent(transform, false);
            Canvas canvas = screen.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 520;
            CanvasScaler scaler = screen.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Stretch(screen.GetComponent<RectTransform>());

            Image dim = CreateImage("Dim", screen.transform, new Color32(3, 7, 17, 244));
            Stretch(dim.rectTransform);
            Image panel = CreateImage("Panel", screen.transform, new Color32(12, 24, 42, 255));
            SetRect(panel.rectTransform, new Vector2(0.08f, 0.07f), new Vector2(0.92f, 0.93f));
            Image rail = CreateImage("GoldRail", panel.transform, new Color32(216, 180, 91, 255));
            SetRect(rail.rectTransform, new Vector2(0.28f, 0.06f), new Vector2(0.282f, 0.94f));

            TMP_Text title = CreateText("Title", panel.transform, "설정", 48f, new Color32(246, 232, 190, 255));
            SetRect(title.rectTransform, new Vector2(0.04f, 0.86f), new Vector2(0.25f, 0.95f));
            title.alignment = TextAlignmentOptions.Left;
            title.fontStyle = FontStyles.Bold;

            RectTransform categoryRoot = CreateVerticalRoot("Categories", panel.transform, new Vector2(0.035f, 0.22f), new Vector2(0.25f, 0.84f), 12f);
            for (int index = 0; index < CategoryLabels.Length; index++)
            {
                int captured = index;
                Button category = CreateButton("Category" + index, categoryRoot, CategoryLabels[index], 25f, 62f);
                category.onClick.AddListener(() => SelectCategory(captured));
                categoryButtons.Add(category);
            }

            categoryTitle = CreateText("CategoryTitle", panel.transform, string.Empty, 38f, new Color32(171, 214, 219, 255));
            SetRect(categoryTitle.rectTransform, new Vector2(0.32f, 0.86f), new Vector2(0.88f, 0.94f));
            categoryTitle.alignment = TextAlignmentOptions.Left;
            categoryTitle.fontStyle = FontStyles.Bold;

            listRoot = CreateVerticalRoot("Rows", panel.transform, new Vector2(0.32f, 0.2f), new Vector2(0.88f, 0.84f), 8f);

            applyButton = CreateButton("Apply", panel.transform, "적용하고 저장", 24f, 58f);
            SetRect(applyButton.GetComponent<RectTransform>(), new Vector2(0.6f, 0.09f), new Vector2(0.75f, 0.16f));
            applyButton.onClick.AddListener(() => Close(true));
            cancelButton = CreateButton("Cancel", panel.transform, "취소", 24f, 58f);
            SetRect(cancelButton.GetComponent<RectTransform>(), new Vector2(0.77f, 0.09f), new Vector2(0.88f, 0.16f));
            cancelButton.onClick.AddListener(() => Close(false));

            statusText = CreateText("Status", panel.transform, string.Empty, 20f, new Color32(151, 183, 191, 255));
            SetRect(statusText.rectTransform, new Vector2(0.32f, 0.08f), new Vector2(0.58f, 0.17f));
            statusText.alignment = TextAlignmentOptions.Left;
        }

        private Button CreateButton(string name, Transform parent, string label, float size, float preferredHeight)
        {
            GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color32(19, 39, 59, 245);
            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color32(19, 39, 59, 245);
            colors.highlightedColor = new Color32(38, 77, 91, 255);
            colors.selectedColor = new Color32(48, 92, 101, 255);
            colors.pressedColor = new Color32(203, 167, 82, 255);
            button.colors = colors;
            buttonObject.GetComponent<LayoutElement>().preferredHeight = preferredHeight;
            TMP_Text text = CreateText("Label", buttonObject.transform, label, size, new Color32(239, 229, 197, 255));
            Stretch(text.rectTransform, 18f, 18f, 0f, 0f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            return button;
        }

        private TMP_Text CreateText(string name, Transform parent, string value, float size, Color color)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.color = color;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
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

        private static SettingsData Clone(SettingsData source)
        {
            return JsonUtility.FromJson<SettingsData>(JsonUtility.ToJson(source ?? SettingsData.CreateDefault()));
        }

        private static int Wrap(int value, int count)
        {
            return (value % count + count) % count;
        }

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
