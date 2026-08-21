#if LEGACY_DISABLED
using StarNight.Maru.P8;
using StarNight.Player;
using StarNight.Stages.P5;
using StarNight.Tools;
using TMPro;
using UnityEngine;

namespace StarNight.UI
{
    [DefaultExecutionOrder(90)]
    [DisallowMultipleComponent]
    public sealed class StarNightHudView2D : MonoBehaviour
    {
        public const int FallbackMaxHealth = 4;
        public const string HealthLabelName = "HudHealthValue";
        public const string RopeLabelName = "HudRopeValue";
        public const string BombLabelName = "HudBombValue";
        public const string GoldLabelName = "HudGoldValue";
        public const string MaruLabelName = "HudMaruValue";

        [SerializeField] private Camera targetCamera;
        [SerializeField] private PlayerRecovery recovery;
        [SerializeField] private PlayerConsumableTools2D consumables;
        [SerializeField] private P5RunState2D runState;
        [SerializeField] private P8MaruTimeline2D maruTimeline;
        [SerializeField, Min(1)] private int maxHealth = FallbackMaxHealth;
        [SerializeField] private Vector2 viewportAnchor =
            new Vector2(0.028f, 0.965f);
        [SerializeField, Min(0.05f)] private float lineSpacing = 0.62f;
        [SerializeField, Min(0.5f)] private float labelFontSize = 3.6f;
        [SerializeField] private Color labelColor =
            new Color(0.88f, 0.95f, 1f, 0.95f);
        [SerializeField] private int sortingOrder = 400;

        private TMP_Text healthLabel;
        private TMP_Text ropeLabel;
        private TMP_Text bombLabel;
        private TMP_Text goldLabel;
        private TMP_Text maruLabel;
        private bool subscribed;

        public int MaxHealth => maxHealth;
        public bool HasLabels => healthLabel != null;
        public string HealthText =>
            healthLabel != null ? healthLabel.text : string.Empty;
        public string RopeText =>
            ropeLabel != null ? ropeLabel.text : string.Empty;
        public string BombText =>
            bombLabel != null ? bombLabel.text : string.Empty;
        public string GoldText =>
            goldLabel != null ? goldLabel.text : string.Empty;
        public string MaruText =>
            maruLabel != null ? maruLabel.text : string.Empty;

        public void Configure(
            Camera camera,
            PlayerRecovery playerRecovery,
            PlayerConsumableTools2D playerConsumables,
            P5RunState2D targetRunState,
            P8MaruTimeline2D timeline,
            int playerMaxHealth = 0)
        {
            Unsubscribe();
            targetCamera = camera;
            recovery = playerRecovery;
            consumables = playerConsumables;
            runState = targetRunState;
            maruTimeline = timeline;
            maxHealth = playerMaxHealth > 0
                ? playerMaxHealth
                : ResolveMaxHealth();
            if (Application.isPlaying)
            {
                EnsureLabels();
                Subscribe();
                RefreshAll();
                UpdateAnchor();
            }
        }

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (recovery == null)
            {
                recovery = FindFirstObjectByType<PlayerRecovery>();
            }

            if (consumables == null)
            {
                consumables =
                    FindFirstObjectByType<PlayerConsumableTools2D>();
            }

            if (runState == null)
            {
                runState = FindFirstObjectByType<P5RunState2D>();
            }

            if (maruTimeline == null)
            {
                maruTimeline = FindFirstObjectByType<P8MaruTimeline2D>();
            }

            maxHealth = maxHealth > 0 ? maxHealth : ResolveMaxHealth();
            EnsureLabels();
        }

        private void OnEnable()
        {
            EnsureLabels();
            Subscribe();
            RefreshAll();
        }

        private void Start()
        {
            RefreshAll();
            UpdateAnchor();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void LateUpdate()
        {
            UpdateAnchor();
        }

        public void RefreshForTests()
        {
            EnsureLabels();
            RefreshAll();
        }

        private int ResolveMaxHealth()
        {
            PlayerMotor2D motor = recovery != null
                ? recovery.GetComponent<PlayerMotor2D>()
                : null;
            P1MovementTuning tuning =
                motor != null ? motor.Tuning : null;
            return tuning != null && tuning.MaxHealth > 0
                ? tuning.MaxHealth
                : FallbackMaxHealth;
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            subscribed = true;
            if (recovery != null)
            {
                recovery.Damaged -= HandleDamaged;
                recovery.Damaged += HandleDamaged;
                recovery.HealthDepleted -= HandleHealthDepleted;
                recovery.HealthDepleted += HandleHealthDepleted;
                recovery.Recovered -= HandleRecovered;
                recovery.Recovered += HandleRecovered;
            }

            if (consumables != null)
            {
                consumables.RopeStockChanged -= HandleRopeStockChanged;
                consumables.RopeStockChanged += HandleRopeStockChanged;
                consumables.BombStockChanged -= HandleBombStockChanged;
                consumables.BombStockChanged += HandleBombStockChanged;
            }

            if (runState != null)
            {
                runState.GoldChanged -= HandleGoldChanged;
                runState.GoldChanged += HandleGoldChanged;
            }

            if (maruTimeline != null)
            {
                maruTimeline.PhaseChanged -= HandlePhaseChanged;
                maruTimeline.PhaseChanged += HandlePhaseChanged;
                maruTimeline.BellRang -= HandleBellRang;
                maruTimeline.BellRang += HandleBellRang;
            }
        }

        private void Unsubscribe()
        {
            subscribed = false;
            if (recovery != null)
            {
                recovery.Damaged -= HandleDamaged;
                recovery.HealthDepleted -= HandleHealthDepleted;
                recovery.Recovered -= HandleRecovered;
            }

            if (consumables != null)
            {
                consumables.RopeStockChanged -= HandleRopeStockChanged;
                consumables.BombStockChanged -= HandleBombStockChanged;
            }

            if (runState != null)
            {
                runState.GoldChanged -= HandleGoldChanged;
            }

            if (maruTimeline != null)
            {
                maruTimeline.PhaseChanged -= HandlePhaseChanged;
                maruTimeline.BellRang -= HandleBellRang;
            }
        }

        private void HandleDamaged(int applied, int remaining)
        {
            RefreshHealth();
        }

        private void HandleHealthDepleted()
        {
            RefreshHealth();
        }

        private void HandleRecovered(RecoveryReason reason, Vector2 position)
        {
            RefreshHealth();
        }

        private void HandleRopeStockChanged(int stock)
        {
            SetText(ropeLabel, $"로프 {stock}");
        }

        private void HandleBombStockChanged(int stock)
        {
            SetText(bombLabel, $"폭탄 {stock}");
        }

        private void HandleGoldChanged(int gold)
        {
            SetText(goldLabel, $"금 {gold}");
        }

        private void HandlePhaseChanged(P8MaruPhase phase)
        {
            RefreshMaru();
        }

        private void HandleBellRang(P8BellEvent bell)
        {
            RefreshMaru();
        }

        private void RefreshAll()
        {
            RefreshHealth();
            HandleRopeStockChanged(
                consumables != null ? consumables.RopeStock : 0);
            HandleBombStockChanged(
                consumables != null ? consumables.BombStock : 0);
            HandleGoldChanged(runState != null ? runState.Gold : 0);
            RefreshMaru();
        }

        private void RefreshHealth()
        {
            int current = recovery != null ? recovery.CurrentHealth : 0;
            int limit = Mathf.Max(maxHealth, current);
            SetText(healthLabel, $"체력 {current}/{limit}");
        }

        private void RefreshMaru()
        {
            P8MaruPhase phase = maruTimeline != null
                ? maruTimeline.Phase
                : P8MaruPhase.Calm;
            SetText(maruLabel, $"마루 {DescribePhase(phase)}");
        }

        public static string DescribePhase(P8MaruPhase phase)
        {
            switch (phase)
            {
                case P8MaruPhase.FirstBell:
                    return "1차 방울";
                case P8MaruPhase.SecondBell:
                    return "2차 방울";
                case P8MaruPhase.Hunting:
                    return "추격";
                case P8MaruPhase.Stopped:
                    return "정지";
                default:
                    return "고요";
            }
        }

        private void UpdateAnchor()
        {
            if (targetCamera == null)
            {
                return;
            }

            float distance = Mathf.Abs(
                targetCamera.transform.position.z - transform.position.z);
            Vector3 anchored = targetCamera.ViewportToWorldPoint(
                new Vector3(viewportAnchor.x, viewportAnchor.y, distance));
            transform.position =
                new Vector3(anchored.x, anchored.y, 0f);
        }

        private void EnsureLabels()
        {
            if (healthLabel != null)
            {
                return;
            }

            healthLabel = CreateLabel(HealthLabelName, 0);
            ropeLabel = CreateLabel(RopeLabelName, 1);
            bombLabel = CreateLabel(BombLabelName, 2);
            goldLabel = CreateLabel(GoldLabelName, 3);
            maruLabel = CreateLabel(MaruLabelName, 4);
        }

        private TMP_Text CreateLabel(string labelName, int row)
        {
            Transform existing = transform.Find(labelName);
            GameObject labelObject = existing != null
                ? existing.gameObject
                : new GameObject(labelName);
            TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
            if (label == null)
            {
                label = labelObject.AddComponent<TextMeshPro>();
            }

            label.rectTransform.SetParent(transform, false);
            label.rectTransform.localPosition =
                new Vector3(0f, -row * lineSpacing, 0f);
            label.rectTransform.localRotation = Quaternion.identity;
            label.rectTransform.localScale = Vector3.one;
            label.rectTransform.sizeDelta = new Vector2(6f, lineSpacing);
            label.rectTransform.pivot = new Vector2(0f, 1f);
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.fontSize = labelFontSize;
            label.color = labelColor;
            label.text = string.Empty;

            MeshRenderer meshRenderer =
                labelObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = sortingOrder;
            }

            return label;
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null && label.text != value)
            {
                label.text = value;
            }
        }
    }
}

#endif
