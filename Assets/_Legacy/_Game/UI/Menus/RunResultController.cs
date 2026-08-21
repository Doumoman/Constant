#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using StarNight.Core.Flow;
using StarNight.Core.State;
using StarNight.Interaction.Input;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StarNight.UI.Results
{
    [DisallowMultipleComponent]
    public sealed class RunResultController : MonoBehaviour
    {
        private static readonly string[] ButtonLabels = { "다시 시작", "타이틀로" };
        private readonly List<Button> buttons = new();

        private GameFlowController gameFlow;
        private RunManager runManager;
        private GameplayInputReader inputReader;
        private PlayerInputContext previousInputContext = PlayerInputContext.Gameplay;
        private GameObject screen;
        private TMP_Text titleText;
        private TMP_Text failureText;
        private TMP_Text stageText;
        private TMP_Text timeText;
        private TMP_Text moneyText;
        private TMP_Text eventText;
        private TMP_Text memoryText;
        private TMP_Text endingText;
        private TMP_Text statusText;
        private int selectedIndex;
        private int previousNavigateSign;
        private bool transitionRequested;

        public bool IsVisible => screen != null && screen.activeSelf;
        public int ButtonCount => buttons.Count;
        public string FailureDisplay => failureText?.text ?? string.Empty;
        public string ReachedStageDisplay => stageText?.text ?? string.Empty;
        public string RunTimeDisplay => timeText?.text ?? string.Empty;
        public string EndingDisplay => endingText?.text ?? string.Empty;

        private void Awake()
        {
            EnsureEventSystem();
            BuildScreen();
        }

        private IEnumerator Start()
        {
            while (!ResolveServices())
            {
                yield return null;
            }

            gameFlow.StateChanged += HandleGameStateChanged;
            if (gameFlow.State == GameApplicationState.RunResult)
            {
                Open();
            }
        }

        private void OnDestroy()
        {
            if (gameFlow != null)
            {
                gameFlow.StateChanged -= HandleGameStateChanged;
            }
        }

        private void Update()
        {
            if (!IsVisible || transitionRequested)
            {
                return;
            }

            ResolveInputReader();
            float navigation = inputReader?.MenuNavigate.y ?? 0f;
            int navigateSign = Mathf.Abs(navigation) > 0.5f ? (navigation > 0f ? 1 : -1) : 0;
            if (Keyboard.current?.upArrowKey.wasPressedThisFrame == true ||
                Gamepad.current?.dpad.up.wasPressedThisFrame == true)
            {
                navigateSign = 1;
            }
            else if (Keyboard.current?.downArrowKey.wasPressedThisFrame == true ||
                     Gamepad.current?.dpad.down.wasPressedThisFrame == true)
            {
                navigateSign = -1;
            }

            if (navigateSign != 0 && previousNavigateSign == 0)
            {
                selectedIndex = Wrap(selectedIndex + (navigateSign > 0 ? -1 : 1), buttons.Count);
                SelectCurrent();
            }
            previousNavigateSign = navigateSign;

            bool directSubmit = Keyboard.current?.xKey.wasPressedThisFrame == true ||
                                Keyboard.current?.enterKey.wasPressedThisFrame == true ||
                                Gamepad.current?.buttonWest.wasPressedThisFrame == true ||
                                Gamepad.current?.buttonSouth.wasPressedThisFrame == true;
            if ((inputReader?.ConsumeMenuSubmitPressed() ?? false) || directSubmit)
            {
                InvokeButton(selectedIndex);
            }
        }

        public void InvokeButton(int index)
        {
            if (!IsVisible || transitionRequested || index < 0 || index >= buttons.Count)
            {
                return;
            }
            buttons[index].onClick.Invoke();
        }

        private bool ResolveServices()
        {
            if (!GameBootstrap.IsReady)
            {
                return false;
            }

            if (gameFlow == null)
            {
                GameBootstrap.Instance.Services.TryGet(out gameFlow);
            }
            if (runManager == null)
            {
                GameBootstrap.Instance.Services.TryGet(out runManager);
            }
            ResolveInputReader();
            return gameFlow != null && runManager != null;
        }

        private void ResolveInputReader()
        {
            if (inputReader == null)
            {
                inputReader = FindFirstObjectByType<GameplayInputReader>();
            }
        }

        private void HandleGameStateChanged(GameApplicationState state)
        {
            if (state == GameApplicationState.RunResult)
            {
                Open();
            }
            else if (IsVisible)
            {
                Hide(false);
            }
        }

        private void Open()
        {
            if (!ResolveServices())
            {
                return;
            }

            RunResultSnapshot result = gameFlow.LastRunResult ?? RunResultSnapshot.Capture(runManager.Current);
            if (result == null)
            {
                return;
            }

            ResolveInputReader();
            if (inputReader != null)
            {
                previousInputContext = inputReader.Context;
                inputReader.ClearBufferedButtons();
                inputReader.SetContext(PlayerInputContext.Menu);
            }

            transitionRequested = false;
            selectedIndex = 0;
            previousNavigateSign = 0;
            Render(result);
            screen.SetActive(true);
            SetButtonsInteractable(true);
            SelectCurrent();
        }

        private void Hide(bool restoreInput)
        {
            if (screen != null)
            {
                screen.SetActive(false);
            }
            if (restoreInput && inputReader != null && inputReader.Context == PlayerInputContext.Menu)
            {
                inputReader.ClearBufferedButtons();
                inputReader.SetContext(previousInputContext);
            }
        }

        private void Render(RunResultSnapshot result)
        {
            titleText.text = result.IsCleared ? "별길 도착" : "항해 종료";
            failureText.text = "실패 원인  ·  " + ResolveFailure(result);
            stageText.text = "도달 스테이지  ·  " + (string.IsNullOrWhiteSpace(result.reachedStageId) ? "-" : result.reachedStageId);
            timeText.text = "런 시간  ·  " + FormatTime(result.runTime);
            moneyText.text = "최고 소지금  ·  " + result.peakMoney + "원";
            eventText.text = "도운 현지 사건  ·  " + result.helpedEventCount;
            memoryText.text = "발견한 기록 길손  ·  " + result.memoryTravelerCount;
            endingText.text = "엔딩 ID  ·  " + (string.IsNullOrWhiteSpace(result.endingId) ? "없음" : result.endingId);
            statusText.text = "X 결정  ·  방향키 선택";
        }

        private void Restart()
        {
            if (gameFlow == null || !gameFlow.RestartRun())
            {
                statusText.text = "새 여행을 준비할 수 없습니다.";
                return;
            }

            transitionRequested = true;
            SetButtonsInteractable(false);
            statusText.text = "새 별길을 여는 중…";
        }

        private void ReturnToTitle()
        {
            if (gameFlow == null || !gameFlow.ReturnToTitle())
            {
                statusText.text = "타이틀로 돌아갈 수 없습니다.";
                return;
            }

            transitionRequested = true;
            SetButtonsInteractable(false);
            statusText.text = "여행 기록을 정리하는 중…";
        }

        private void BuildScreen()
        {
            TMP_FontAsset font = FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include)?.font ?? TMP_Settings.defaultFontAsset;
            screen = new GameObject("RunResultScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            screen.transform.SetParent(transform, false);
            Canvas canvas = screen.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 650;
            CanvasScaler scaler = screen.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Stretch(screen.GetComponent<RectTransform>());

            Image background = CreateImage("NightResultBackground", screen.transform, new Color32(3, 10, 23, 248));
            Stretch(background.rectTransform);
            Image glow = CreateImage("ResultGlow", screen.transform, new Color32(18, 58, 73, 210));
            SetRect(glow.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.61f, 0.92f));
            Image rail = CreateImage("GoldRail", screen.transform, new Color32(222, 185, 96, 235));
            SetRect(rail.rectTransform, new Vector2(0.61f, 0.11f), new Vector2(0.612f, 0.89f));

            TMP_Text eyebrow = CreateText("Eyebrow", screen.transform, font, "VOYAGE RECORD  ·  항해 보고", 23f, new Color32(151, 201, 207, 255));
            SetRect(eyebrow.rectTransform, new Vector2(0.09f, 0.8f), new Vector2(0.56f, 0.87f));
            titleText = CreateText("ResultTitle", screen.transform, font, string.Empty, 72f, new Color32(246, 232, 190, 255));
            SetRect(titleText.rectTransform, new Vector2(0.09f, 0.65f), new Vector2(0.56f, 0.8f));
            titleText.fontStyle = FontStyles.Bold;

            RectTransform card = CreateImage("RecordCard", screen.transform, new Color32(8, 20, 38, 245)).rectTransform;
            SetRect(card, new Vector2(0.09f, 0.18f), new Vector2(0.56f, 0.64f));
            failureText = CreateRecordLine("Failure", card, font, 0);
            stageText = CreateRecordLine("ReachedStage", card, font, 1);
            timeText = CreateRecordLine("RunTime", card, font, 2);
            moneyText = CreateRecordLine("PeakMoney", card, font, 3);
            eventText = CreateRecordLine("HelpedEvents", card, font, 4);
            memoryText = CreateRecordLine("MemoryTravelers", card, font, 5);
            endingText = CreateRecordLine("EndingId", card, font, 6);

            RectTransform buttonRoot = new GameObject("ResultActions", typeof(RectTransform), typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
            buttonRoot.transform.SetParent(screen.transform, false);
            SetRect(buttonRoot, new Vector2(0.67f, 0.34f), new Vector2(0.91f, 0.62f));
            VerticalLayoutGroup layout = buttonRoot.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            for (int index = 0; index < ButtonLabels.Length; index++)
            {
                Button button = CreateButton(ButtonLabels[index], buttonRoot, font);
                if (index == 0) button.onClick.AddListener(Restart);
                else button.onClick.AddListener(ReturnToTitle);
                buttons.Add(button);
            }

            statusText = CreateText("Status", screen.transform, font, string.Empty, 21f, new Color32(151, 186, 193, 255));
            SetRect(statusText.rectTransform, new Vector2(0.65f, 0.2f), new Vector2(0.93f, 0.29f));
            statusText.alignment = TextAlignmentOptions.Center;
            screen.SetActive(false);
        }

        private static TMP_Text CreateRecordLine(string name, RectTransform parent, TMP_FontAsset font, int index)
        {
            TMP_Text line = CreateText(name, parent, font, string.Empty, 25f, new Color32(219, 224, 210, 255));
            float top = 0.92f - index * 0.125f;
            SetRect(line.rectTransform, new Vector2(0.08f, top - 0.1f), new Vector2(0.92f, top));
            return line;
        }

        private static Button CreateButton(string label, Transform parent, TMP_FontAsset font)
        {
            GameObject instance = new(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            instance.transform.SetParent(parent, false);
            instance.GetComponent<LayoutElement>().preferredHeight = 82f;
            Image image = instance.GetComponent<Image>();
            image.color = new Color32(14, 31, 52, 245);
            Button button = instance.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color32(14, 31, 52, 245);
            colors.highlightedColor = new Color32(35, 76, 90, 255);
            colors.selectedColor = new Color32(52, 99, 105, 255);
            colors.pressedColor = new Color32(218, 181, 92, 255);
            colors.disabledColor = new Color32(22, 27, 37, 170);
            button.colors = colors;
            TMP_Text text = CreateText("Label", instance.transform, font, label, 29f, new Color32(242, 231, 197, 255));
            Stretch(text.rectTransform, 16f, 16f, 0f, 0f);
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            return button;
        }

        private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, string value, float size, Color color)
        {
            GameObject instance = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            instance.transform.SetParent(parent, false);
            TMP_Text text = instance.GetComponent<TMP_Text>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject instance = new(name, typeof(RectTransform), typeof(Image));
            instance.transform.SetParent(parent, false);
            Image image = instance.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static string ResolveFailure(RunResultSnapshot result)
        {
            if (result.IsCleared) return "없음";
            return result.failureReason switch
            {
                "maru_bite" => "마루에게 붙잡힘",
                "health_depleted" => "체력 소진",
                "abandoned" => "여행 중단",
                _ => string.IsNullOrWhiteSpace(result.failureReason) ? "기록되지 않음" : result.failureReason,
            };
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private void SetButtonsInteractable(bool value)
        {
            foreach (Button button in buttons) button.interactable = value;
        }

        private void SelectCurrent()
        {
            if (buttons.Count > 0)
            {
                EventSystem.current?.SetSelectedGameObject(buttons[Mathf.Clamp(selectedIndex, 0, buttons.Count - 1)].gameObject);
            }
        }

        private static void EnsureEventSystem()
        {
            EventSystem events = EventSystem.current;
            if (events == null)
            {
                GameObject instance = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                instance.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                return;
            }

            if (events.GetComponent<BaseInputModule>() == null)
            {
                events.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
        }

        private static int Wrap(int value, int count) => count <= 0 ? 0 : (value % count + count) % count;

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

    internal static class RunResultRuntimeBootstrap
    {
        private static bool installed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            installed = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (installed) return;
            installed = true;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != GameFlowController.RunShellSceneName ||
                Object.FindAnyObjectByType<RunResultController>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            GameObject root = new("[RunResultSystem]");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<RunResultController>();
        }
    }
}

#endif
