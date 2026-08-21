#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.UI.HUD
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            Refresh();
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea || lastWidth != Screen.width || lastHeight != Screen.height)
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));
        }

        public void Apply(Rect safeArea, Vector2 screenSize)
        {
            rectTransform ??= GetComponent<RectTransform>();
            CalculateAnchors(safeArea, screenSize, out Vector2 minimum, out Vector2 maximum);
            rectTransform.anchorMin = minimum;
            rectTransform.anchorMax = maximum;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            lastSafeArea = safeArea;
            lastWidth = Mathf.RoundToInt(screenSize.x);
            lastHeight = Mathf.RoundToInt(screenSize.y);
        }

        public static void CalculateAnchors(Rect safeArea, Vector2 screenSize, out Vector2 minimum, out Vector2 maximum)
        {
            float width = Mathf.Max(1f, screenSize.x);
            float height = Mathf.Max(1f, screenSize.y);
            minimum = new Vector2(Mathf.Clamp01(safeArea.xMin / width), Mathf.Clamp01(safeArea.yMin / height));
            maximum = new Vector2(Mathf.Clamp01(safeArea.xMax / width), Mathf.Clamp01(safeArea.yMax / height));
            maximum.x = Mathf.Max(minimum.x, maximum.x);
            maximum.y = Mathf.Max(minimum.y, maximum.y);
        }
    }

    public static class HUDLayoutContract
    {
        public static readonly Rect TopLeft = new Rect(0f, 0.84f, 0.34f, 0.16f);
        public static readonly Rect TopCenter = new Rect(0.34f, 0.82f, 0.32f, 0.18f);
        public static readonly Rect TopRight = new Rect(0.66f, 0.84f, 0.34f, 0.16f);
        public static readonly Rect BottomLeft = new Rect(0f, 0f, 0.36f, 0.18f);
        public static readonly Rect BottomRight = new Rect(0.64f, 0f, 0.36f, 0.18f);

        public static bool FitsNormalizedSafeArea(Rect region)
        {
            return region.xMin >= 0f && region.yMin >= 0f && region.xMax <= 1f && region.yMax <= 1f;
        }
    }
}

#endif
