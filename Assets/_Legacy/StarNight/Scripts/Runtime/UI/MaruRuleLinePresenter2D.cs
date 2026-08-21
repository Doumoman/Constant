#if LEGACY_DISABLED
using TMPro;
using UnityEngine;
using Yarn.Unity;

namespace StarNight.UI
{
    [DefaultExecutionOrder(96)]
    [DisallowMultipleComponent]
    public sealed class MaruRuleLinePresenter2D : DialoguePresenterBase
    {
        public const float DefaultHoldSeconds = 3.2f;
        public const string LineLabelName = "DialogueLineValue";

        [SerializeField] private Camera targetCamera;
        [SerializeField, Min(0.1f)] private float holdSeconds =
            DefaultHoldSeconds;
        [SerializeField] private Vector2 viewportAnchor =
            new Vector2(0.5f, 0.18f);
        [SerializeField, Min(0.5f)] private float labelFontSize = 3.4f;
        [SerializeField] private Color labelColor =
            new Color(0.94f, 0.94f, 0.86f, 1f);
        [SerializeField] private int sortingOrder = 410;

        private TMP_Text lineLabel;

        public float HoldSeconds => holdSeconds;
        public bool IsShowing { get; private set; }
        public string CurrentLine { get; private set; } = string.Empty;

        public void Configure(
            Camera camera,
            float lineHoldSeconds = DefaultHoldSeconds)
        {
            targetCamera = camera;
            holdSeconds = Mathf.Max(0.1f, lineHoldSeconds);
        }

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            EnsureLabel();
            Hide();
        }

        private void LateUpdate()
        {
            if (targetCamera == null || !IsShowing)
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

        public override YarnTask OnDialogueStartedAsync()
        {
            EnsureLabel();
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            Hide();
            return YarnTask.CompletedTask;
        }

        public override async YarnTask RunLineAsync(
            LocalizedLine line,
            LineCancellationToken token)
        {
            EnsureLabel();
            Show(line != null ? line.TextWithoutCharacterName.Text : string.Empty);

            float elapsed = 0f;
            while (elapsed < holdSeconds
                && !token.IsNextContentRequested)
            {
                elapsed += Time.deltaTime;
                await YarnTask.Yield();
            }

            Hide();
        }

        public void ShowForTests(string text)
        {
            EnsureLabel();
            Show(text);
        }

        public void HideForTests()
        {
            Hide();
        }

        private void Show(string text)
        {
            CurrentLine = text ?? string.Empty;
            IsShowing = true;
            if (lineLabel != null)
            {
                lineLabel.text = CurrentLine;
                lineLabel.enabled = true;
            }
        }

        private void Hide()
        {
            CurrentLine = string.Empty;
            IsShowing = false;
            if (lineLabel != null)
            {
                lineLabel.text = string.Empty;
                lineLabel.enabled = false;
            }
        }

        private void EnsureLabel()
        {
            if (lineLabel != null)
            {
                return;
            }

            Transform existing = transform.Find(LineLabelName);
            GameObject labelObject = existing != null
                ? existing.gameObject
                : new GameObject(LineLabelName);
            TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
            if (label == null)
            {
                label = labelObject.AddComponent<TextMeshPro>();
            }

            label.rectTransform.SetParent(transform, false);
            label.rectTransform.localPosition = Vector3.zero;
            label.rectTransform.localRotation = Quaternion.identity;
            label.rectTransform.localScale = Vector3.one;
            label.rectTransform.sizeDelta = new Vector2(16f, 2.4f);
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.fontSize = labelFontSize;
            label.color = labelColor;
            label.text = string.Empty;

            MeshRenderer meshRenderer =
                labelObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = sortingOrder;
            }

            lineLabel = label;
        }
    }
}

#endif
