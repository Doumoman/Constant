using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarNightHUD : MonoBehaviour
    {
        public static StarNightHUD Instance { get; private set; }

        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Color ink = new(0.92f, 0.91f, 0.78f);
        [SerializeField] private Color nightPanel = new(0.035f, 0.04f, 0.105f, 0.9f);
        [SerializeField] private Color starGold = new(1f, 0.76f, 0.25f);
        [SerializeField] private Color dangerPink = new(1f, 0.22f, 0.48f);

        private TMP_Text chapterText;
        private TMP_Text objectiveText;
        private TMP_Text healthText;
        private TMP_Text toolText;
        private TMP_Text inventoryText;
        private TMP_Text promptText;
        private TMP_Text toastText;
        private TMP_Text endingText;
        private TMP_Text scentLabel;
        private Image scentFill;
        private Image endingPanel;
        private StarNightRunState run;
        private StarNightPlayerAgent player;
        private Coroutine toastRoutine;

        public void SetFont(TMP_FontAsset value) => font = value;

        private void Awake()
        {
            Instance = this;
            Build();
        }

        private void Start()
        {
            run = StarNightRunState.Ensure();
            player = FindFirstObjectByType<StarNightPlayerAgent>();
            run.Chapter.ScentChanged += OnScentChanged;
            run.Chapter.DepartureProgressChanged += OnDepartureProgress;
            run.Chapter.GateAlertChanged += OnGateAlertChanged;
            run.Chapter.BellPhaseChanged += OnBellPhaseChanged;
            run.ChapterLoop.StateChanged += OnLoopStateChanged;
            run.ChapterLoop.RouteChanged += OnRouteChanged;
            run.GateContributions.Changed += RefreshStatic;
            if (player != null)
            {
                player.HealthChanged += OnHealthChanged;
                player.SelectionChanged += RefreshStatic;
                player.Inventory.Changed += RefreshStatic;
            }
            RefreshStatic();
            OnScentChanged(run.Chapter.Scent, run.Chapter.ScentStage);
            OnDepartureProgress(run.Chapter.DepartureProgress, run.Chapter.RequiredDepartureProgress);
        }

        private void OnDestroy()
        {
            if (run != null)
            {
                run.Chapter.ScentChanged -= OnScentChanged;
                run.Chapter.DepartureProgressChanged -= OnDepartureProgress;
                run.Chapter.GateAlertChanged -= OnGateAlertChanged;
                run.Chapter.BellPhaseChanged -= OnBellPhaseChanged;
                run.ChapterLoop.StateChanged -= OnLoopStateChanged;
                run.ChapterLoop.RouteChanged -= OnRouteChanged;
                run.GateContributions.Changed -= RefreshStatic;
            }
            if (player != null)
            {
                player.HealthChanged -= OnHealthChanged;
                player.SelectionChanged -= RefreshStatic;
                player.Inventory.Changed -= RefreshStatic;
            }
        }

        private void Update()
        {
            if (player != null)
            {
                promptText.text = player.CurrentPrompt();
            }
        }

        private void Build()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            gameObject.AddComponent<GraphicRaycaster>();

            RectTransform top = Panel("TopPanel", transform, new Vector2(0.02f, 0.81f), new Vector2(0.43f, 0.98f), nightPanel);
            chapterText = Label("Chapter", top, new Vector2(0.04f, 0.68f), new Vector2(0.96f, 0.94f), 30, starGold, TextAlignmentOptions.Left);
            objectiveText = Label("Objective", top, new Vector2(0.04f, 0.35f), new Vector2(0.96f, 0.67f), 24, ink, TextAlignmentOptions.Left);
            healthText = Label("Health", top, new Vector2(0.04f, 0.05f), new Vector2(0.32f, 0.32f), 26, dangerPink, TextAlignmentOptions.Left);

            RectTransform scentPanel = Panel("ScentPanel", transform, new Vector2(0.58f, 0.88f), new Vector2(0.98f, 0.98f), nightPanel);
            scentLabel = Label("ScentLabel", scentPanel, new Vector2(0.04f, 0.56f), new Vector2(0.96f, 0.94f), 22, ink, TextAlignmentOptions.Left);
            scentLabel.text = "별냄새";
            RectTransform barBack = Panel("ScentBar", scentPanel, new Vector2(0.04f, 0.16f), new Vector2(0.96f, 0.46f), new Color(0.12f, 0.12f, 0.2f, 1f));
            GameObject fillObject = new("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(barBack, false);
            scentFill = fillObject.GetComponent<Image>();
            scentFill.color = starGold;
            scentFill.type = Image.Type.Filled;
            scentFill.fillMethod = Image.FillMethod.Horizontal;
            scentFill.fillOrigin = 0;
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            RectTransform ticketPanel = Panel("TravelTicketPanel", transform,
                new Vector2(0.68f, 0.49f), new Vector2(0.98f, 0.86f), nightPanel);
            TMP_Text ticketRoute = Label("TicketRoute", ticketPanel, new Vector2(0.06f, 0.24f),
                new Vector2(0.94f, 0.95f), 18, ink, TextAlignmentOptions.TopLeft);
            TMP_Text playerMarker = Label("PlayerMarker", ticketPanel, new Vector2(0f, 0.11f),
                new Vector2(0.16f, 0.22f), 17, starGold, TextAlignmentOptions.Center);
            TMP_Text maruMarker = Label("MaruMarker", ticketPanel, new Vector2(0f, 0.02f),
                new Vector2(0.16f, 0.13f), 17, dangerPink, TextAlignmentOptions.Center);
            TMP_Text ticketFooter = Label("TicketFooter", ticketPanel, new Vector2(0.05f, 0.0f),
                new Vector2(0.95f, 0.08f), 14, new Color(0.72f, 0.8f, 1f), TextAlignmentOptions.Center);
            TravelTicketPresenter ticket = gameObject.AddComponent<TravelTicketPresenter>();
            ticket.Configure(ticketPanel, ticketRoute, playerMarker, maruMarker, ticketFooter);

            RectTransform bottom = Panel("BottomPanel", transform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.18f), nightPanel);
            toolText = Label("Tool", bottom, new Vector2(0.02f, 0.52f), new Vector2(0.34f, 0.92f), 24, starGold, TextAlignmentOptions.Left);
            inventoryText = Label("Inventory", bottom, new Vector2(0.35f, 0.52f), new Vector2(0.98f, 0.92f), 22, ink, TextAlignmentOptions.Left);
            promptText = Label("Prompt", bottom, new Vector2(0.02f, 0.08f), new Vector2(0.98f, 0.48f), 23, Color.white, TextAlignmentOptions.Center);

            toastText = Label("Toast", transform, new Vector2(0.18f, 0.68f), new Vector2(0.82f, 0.78f), 27, Color.white, TextAlignmentOptions.Center);
            toastText.gameObject.SetActive(false);

            RectTransform ending = Panel("EndingPanel", transform, new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f), new Color(0.025f, 0.02f, 0.08f, 0.97f));
            endingPanel = ending.GetComponent<Image>();
            endingText = Label("EndingText", ending, new Vector2(0.07f, 0.07f), new Vector2(0.93f, 0.93f), 30, ink, TextAlignmentOptions.Center);
            ending.gameObject.SetActive(false);
        }

        private RectTransform Panel(string objectName, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            GameObject panel = new(objectName, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = color;
            return rect;
        }

        private TMP_Text Label(string objectName, Transform parent, Vector2 min, Vector2 max, float size, Color color, TextAlignmentOptions alignment)
        {
            GameObject label = new(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(parent, false);
            RectTransform rect = label.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private void RefreshStatic()
        {
            if (run == null || player == null)
            {
                return;
            }
            chapterText.text = run.Chapter.Definition?.displayName ?? "별을 물어오는 밤";
            healthText.text = $"별빛 {new string('◆', player.Health)}{new string('◇', player.MaximumHealth - player.Health)}";
            string intent = player.ResizeIntent == ResizeIntent.Enlarge ? "크게" : "작게";
            if (player.SelectedTool == FableVerb.Link)
            {
                string endpoint = run.RedThread.PendingEndpoint != null
                    ? $"첫 끝: {run.RedThread.PendingEndpoint.DisplayName}"
                    : "첫 끝 미지정";
                toolText.text = $"까치의 붉은 실 · {endpoint}  [E 선택/연결 · R 도구]";
            }
            else if (player.SelectedTool == FableVerb.Float)
            {
                string bottle = run.CloudBottle.HeldWeight > 0f
                    ? $"{run.CloudBottle.Source?.DisplayName}의 무게 {run.CloudBottle.HeldWeight:0.0}"
                    : "빈 병";
                toolText.text = $"구름병 · {bottle}  [E 무게 담기/옮기기 · R 도구]";
            }
            else if (player.SelectedTool == FableVerb.Deliver)
            {
                string parcel = run.Delivery.PendingParcel != null
                    ? $"소포: {run.Delivery.PendingParcel.DisplayName}"
                    : "소포 미지정";
                toolText.text = $"별 우편 도장 · {parcel}  [E 소포/주소 · R 도구]";
            }
            else if (player.SelectedTool == FableVerb.Awaken)
            {
                toolText.text =
                    $"햇빛 씨앗 · 저장 {run.SunSeeds.Charges} / 희귀 {run.SunSeeds.RareCharges} · 정원 열 {run.Heat.Heat:0}  [E 깨우기 · R 도구]";
            }
            else
            {
                toolText.text = $"달토끼의 절구 · {intent}  [Q 크기 · E 사용 · R 도구]";
            }

            StringBuilder bag = new("가방 ");
            for (int i = 0; i < 6; i++)
            {
                if (i < player.Inventory.GeneralItems.Count)
                {
                    string marker = i == player.Inventory.SelectedIndex ? "◆" : "·";
                    bag.Append($" {marker}{i + 1}:{player.Inventory.GeneralItems[i].DisplayName}");
                }
                else
                {
                    bag.Append($" ·{i + 1}:—");
                }
            }
            bag.Append($"   빌린 물건 {player.Inventory.ResidentItems.Count}/2");
            if (run.Chapter.GateLoopEnabled && run.GateContributions.Count > 0)
            {
                bag.Append("   별문 ");
                foreach (GateContribution contribution in run.GateContributions.Pending)
                {
                    bag.Append($"◆{contribution.displayName} ");
                }
            }
            inventoryText.text = bag.ToString();
            RefreshObjective();
        }

        private void OnScentChanged(float value, StarScentStage stage)
        {
            RefreshScentPanel(value, stage);
        }

        private void OnDepartureProgress(int current, int required)
        {
            RefreshObjective();
        }

        private void OnLoopStateChanged(ChapterLoopState state)
        {
            RefreshObjective();
        }

        private void OnGateAlertChanged(float alert)
        {
            RefreshScentPanel(run.Chapter.Scent, run.Chapter.ScentStage);
            RefreshObjective();
        }

        private void OnBellPhaseChanged(StarBellPhase phase)
        {
            RefreshScentPanel(run.Chapter.Scent, run.Chapter.ScentStage);
            RefreshObjective();
        }

        private void OnRouteChanged(GateRouteRuntimeState route)
        {
            RefreshObjective();
            RefreshStatic();
        }

        private void RefreshObjective()
        {
            if (run == null)
            {
                return;
            }

            StarChapterDefinition definition = run?.Chapter.Definition;
            if (run.CurrentChapter == StarChapterId.PolarisObservatory &&
                run.GetComponent<PolarisFinaleState>() is { } finale)
            {
                objectiveText.text = finale.BuildObjectiveText();
                return;
            }
            if (run.Chapter.GateLoopEnabled)
            {
                StringBuilder objective = new();
                if (run.Chapter.GateActivated)
                {
                    switch (run.Chapter.BellPhase)
                    {
                        case StarBellPhase.First:
                            objective.Append("첫 번째 방울 · 지금 출항 가능\n")
                                .Append("마루는 아직 흔적뿐 · 남은 경로는 선택 탐험");
                            break;
                        case StarBellPhase.Second:
                            objective.Append("두 번째 방울 · 마루가 같은 정거장에 진입\n")
                                .Append("물건과 주민을 먼저 노린다 · 지금 출항 가능");
                            break;
                        case StarBellPhase.Third:
                            objective.Append("세 번째 방울 · 별문이 닫히는 중\n")
                                .Append("마루가 플레이어를 직접 추격한다 · 즉시 출항");
                            break;
                    }
                }
                else if (run.Chapter.GateReady)
                {
                    string gateNoun = definition?.objectiveNoun ?? "별문 재료";
                    objective.Append($"{gateNoun} {run.Chapter.GateContributions}/{run.Chapter.GateRequired} · 별문 준비 완료\n")
                        .Append("별문 허브의 손잡이를 직접 당기세요");
                }
                else
                {
                    string gateNoun = definition?.objectiveNoun ?? "별문 재료";
                    objective.Append($"{gateNoun}을 별문에 넣으세요: {run.Chapter.GateContributions}/{run.Chapter.GateRequired}");
                    foreach (GateRouteRuntimeState route in run.ChapterLoop.Routes)
                    {
                        string marker = route.state switch
                        {
                            GateRouteState.Contributed => "◆",
                            GateRouteState.Complete => "◆",
                            GateRouteState.Invalidated => "×",
                            _ => "○"
                        };
                        objective.Append($"\n{marker} {route.displayName}");
                    }
                }
                objectiveText.text = objective.ToString();
                return;
            }

            string noun = definition?.objectiveNoun ?? "출항 물품";
            string instruction = definition?.objectiveInstruction;
            int current = run.Chapter.DepartureProgress;
            int required = run.Chapter.RequiredDepartureProgress;
            objectiveText.text = current >= required
                ? $"{noun} 준비 완료 · 출항 지점으로 이동"
                : $"{noun}  {current}/{required}\n{instruction}";
        }

        private void RefreshScentPanel(float value, StarScentStage stage)
        {
            if (scentFill == null || scentLabel == null)
            {
                return;
            }

            if (run != null && run.Chapter.GateLoopEnabled && run.Chapter.GateActivated)
            {
                StarBellPhase phase = run.Chapter.BellPhase;
                string bells = phase switch
                {
                    StarBellPhase.First => "● ○ ○",
                    StarBellPhase.Second => "● ● ○",
                    StarBellPhase.Third => "● ● ●",
                    _ => "○ ○ ○"
                };
                string trace = phase switch
                {
                    StarBellPhase.First when run.Chapter.PostGateAlert >= 15f => "흔적이 번진다",
                    StarBellPhase.First => "희미한 발자국",
                    StarBellPhase.Second when run.Chapter.PostGateAlert >= 45f => "냄새가 플레이어에게 모인다",
                    StarBellPhase.Second => "정거장 수색 중",
                    StarBellPhase.Third => "직접 추격",
                    _ => "고요"
                };
                scentLabel.text = $"마루의 방울 · {bells}  {trace}";
                scentFill.fillAmount = Mathf.Clamp01(run.Chapter.PostGateAlert /
                                                     StarGateAlertRules.ThirdBellThreshold);
                scentFill.color = phase >= StarBellPhase.Second ? dangerPink : starGold;
                return;
            }

            scentFill.fillAmount = value / StarScentRules.MaxScent;
            scentFill.color = stage >= StarScentStage.Bell ? dangerPink : starGold;
            scentLabel.text =
                $"별냄새 · {StarScentRules.DisplayName(stage)}  {Mathf.RoundToInt(value)}";
        }

        private void OnHealthChanged(int current, int maximum)
        {
            healthText.text = $"별빛 {new string('◆', current)}{new string('◇', maximum - current)}";
        }

        public void Toast(string message, float duration = 2.6f)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            if (toastRoutine != null)
            {
                StopCoroutine(toastRoutine);
            }
            toastRoutine = StartCoroutine(ShowToast(message, duration));
        }

        private IEnumerator ShowToast(string message, float duration)
        {
            toastText.text = message;
            toastText.gameObject.SetActive(true);
            yield return new WaitForSeconds(duration);
            toastText.gameObject.SetActive(false);
        }

        public void ShowEnding(string title, string body)
        {
            run ??= StarNightRunState.Instance;
            string sequence = JourneyIntermissionFormatter.Build(run, body);
            endingText.text = $"<size=42><color=#FFD15C>{title}</color></size>\n\n<size=22>{sequence}</size>";
            endingPanel.gameObject.SetActive(true);
        }

        public void ShowFinalEnding(string title, string body)
        {
            endingText.text =
                $"<size=46><color=#FFD15C>{title}</color></size>\n\n{body}\n\n" +
                "<size=22>《별을 물어오는 밤》 · 여행 완료</size>";
            endingPanel.gameObject.SetActive(true);
        }
    }
}
