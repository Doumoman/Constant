#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Maru.P8
{
    [DisallowMultipleComponent]
    public sealed class P8MaruRunFeedback2D : MonoBehaviour
    {
        [SerializeField] private P8MaruBiteController2D biteController;
        [SerializeField] private Color panelColor =
            new Color(0.08f, 0.07f, 0.16f, 0.94f);
        [SerializeField] private Color accentColor =
            new Color(0.58f, 0.88f, 1f, 1f);

        private bool subscribed;
        private float escapedCueUntil;

        public P8MaruBiteController2D BiteController => biteController;
        public P8DeathCauseReport LastReport { get; private set; }
        public bool IsShowingRunEnd => LastReport != null;
        public bool IsShowingEscapeWindow =>
            biteController != null
            && biteController.State == P8BiteState.EscapeWindow;

        public void Configure(P8MaruBiteController2D bite)
        {
            Unsubscribe();
            biteController = bite;
            LastReport = null;
            escapedCueUntil = -1f;
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnGUI()
        {
            if (biteController == null)
            {
                return;
            }

            if (IsShowingEscapeWindow)
            {
                DrawEscapeWindow();
            }

            if (LastReport != null)
            {
                DrawRunEnd();
            }
            else if (Time.unscaledTime < escapedCueUntil)
            {
                DrawEscapedCue();
            }
        }

        public void ResetFeedback()
        {
            LastReport = null;
            escapedCueUntil = -1f;
        }

        private void HandleBiteStarted(int biteCount)
        {
            LastReport = null;
            escapedCueUntil = -1f;
        }

        private void HandleFirstBiteEscaped()
        {
            escapedCueUntil = Time.unscaledTime + 1.5f;
        }

        private void HandleRunEnded(P8DeathCauseReport report)
        {
            LastReport = report;
        }

        private void DrawEscapeWindow()
        {
            const float width = 420f;
            const float height = 92f;
            Rect panel = CenteredRect(width, height, 42f);
            DrawPanel(panel);

            GUIStyle title = CreateLabelStyle(
                20,
                FontStyle.Bold,
                TextAnchor.UpperCenter,
                accentColor);
            GUI.Label(
                new Rect(panel.x + 16f, panel.y + 10f, width - 32f, 28f),
                "마루에게 물렸습니다 — 길게 누르거나 연타",
                title);

            Rect track = new Rect(
                panel.x + 24f,
                panel.y + 51f,
                width - 48f,
                18f);
            DrawSolid(track, new Color(0.17f, 0.16f, 0.28f, 1f));
            float progress =
                1f - biteController.EscapeRemaining
                    / P8MaruBiteController2D.EscapeWindowSeconds;
            DrawSolid(
                new Rect(
                    track.x,
                    track.y,
                    track.width * Mathf.Clamp01(progress),
                    track.height),
                accentColor);
        }

        private void DrawEscapedCue()
        {
            const float width = 340f;
            Rect panel = CenteredRect(width, 54f, 42f);
            DrawPanel(panel);
            GUI.Label(
                panel,
                "물건 하나와 체력 1칸을 잃고 탈출했습니다.",
                CreateLabelStyle(
                    18,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    accentColor));
        }

        private void DrawRunEnd()
        {
            const float width = 620f;
            const float height = 190f;
            Rect panel = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);
            DrawPanel(panel);

            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 18f, width - 48f, 34f),
                "런 종료",
                CreateLabelStyle(
                    26,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    accentColor));
            GUI.Label(
                new Rect(panel.x + 32f, panel.y + 66f, width - 64f, 40f),
                LastReport.PrimaryMessage,
                CreateLabelStyle(
                    20,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Color.white));
            GUI.Label(
                new Rect(panel.x + 32f, panel.y + 112f, width - 64f, 52f),
                LastReport.TimingMessage,
                CreateLabelStyle(
                    17,
                    FontStyle.Normal,
                    TextAnchor.MiddleCenter,
                    new Color(0.78f, 0.84f, 0.96f, 1f)));
        }

        private void DrawPanel(Rect rect)
        {
            DrawSolid(rect, panelColor);
            DrawSolid(
                new Rect(rect.x, rect.y, rect.width, 3f),
                accentColor);
            DrawSolid(
                new Rect(
                    rect.x,
                    rect.yMax - 3f,
                    rect.width,
                    3f),
                new Color(
                    accentColor.r,
                    accentColor.g,
                    accentColor.b,
                    0.55f));
        }

        private static Rect CenteredRect(
            float width,
            float height,
            float top)
        {
            return new Rect(
                (Screen.width - width) * 0.5f,
                top,
                width,
                height);
        }

        private static void DrawSolid(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static GUIStyle CreateLabelStyle(
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Color color)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                alignment = alignment,
                normal = { textColor = color },
                wordWrap = true
            };
        }

        private void Subscribe()
        {
            if (subscribed || biteController == null)
            {
                return;
            }

            biteController.BiteStarted += HandleBiteStarted;
            biteController.FirstBiteEscaped += HandleFirstBiteEscaped;
            biteController.RunEnded += HandleRunEnded;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || biteController == null)
            {
                return;
            }

            biteController.BiteStarted -= HandleBiteStarted;
            biteController.FirstBiteEscaped -= HandleFirstBiteEscaped;
            biteController.RunEnded -= HandleRunEnded;
            subscribed = false;
        }
    }
}

#endif
