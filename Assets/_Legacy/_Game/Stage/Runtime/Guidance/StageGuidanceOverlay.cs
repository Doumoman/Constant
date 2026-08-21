#if LEGACY_DISABLED
using StarNight.Stage.Exit;
using StarNight.Stage.Flow;
using UnityEngine;

namespace StarNight.Stage.Guidance
{
    [DisallowMultipleComponent]
    public sealed class StageGuidanceOverlay : MonoBehaviour
    {
        private StageFlowController flow;
        private float stageNameVisibleUntil;
        private GUIStyle titleStyle;
        private GUIStyle guideStyle;
        private GUIStyle promptStyle;

        public void Bind(StageFlowController stageFlow)
        {
            flow = stageFlow;
            stageNameVisibleUntil = Time.unscaledTime + 1.5f;
        }

        private void OnGUI()
        {
            if (flow == null || flow.RuntimeState == null)
            {
                return;
            }

            EnsureStyles();
            if (Time.unscaledTime <= stageNameVisibleUntil)
            {
                string name = string.IsNullOrWhiteSpace(flow.CurrentDefinition.displayNameKey)
                    ? flow.CurrentDefinition.stageId
                    : flow.CurrentDefinition.displayNameKey;
                GUI.Label(new Rect(Screen.width * 0.5f - 180f, 42f, 360f, 36f), name, titleStyle);
            }

            ExitGuidance guidance = flow.CurrentGuidance;
            if (guidance.IsValid)
            {
                Color previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, flow.RuntimeState.exitDiscovered ? 1f : 0.7f);
                string symbol = guidance.ExitInCurrentRoom ? "▣" : DirectionSymbol(guidance.Direction);
                GUI.Label(new Rect(Screen.width * 0.5f - 40f, 8f, 80f, 36f), symbol, guideStyle);
                GUI.color = previous;
            }

            StageExitDoor exit = flow.CurrentExit;
            if (exit != null && exit.IsPlayerInRange && flow.CanCommitExit)
            {
                string prompt = exit.IsHolding
                    ? $"{StageExitDoor.PromptText}  {Mathf.RoundToInt(exit.HoldProgress * 100f)}%"
                    : StageExitDoor.PromptText;
                GUI.Label(new Rect(Screen.width * 0.5f - 120f, Screen.height - 88f, 240f, 32f), prompt, promptStyle);
            }

            if (flow.FadeOpacity > 0f)
            {
                Color previous = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, flow.FadeOpacity);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = previous;
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.96f, 0.82f, 0.45f, 1f) },
            };
            guideStyle = new GUIStyle(titleStyle) { fontSize = 28 };
            promptStyle = new GUIStyle(titleStyle) { fontSize = 17 };
        }

        private static string DirectionSymbol(Vector2Int direction)
        {
            if (direction.x > 0) return "→";
            if (direction.x < 0) return "←";
            if (direction.y > 0) return "↑";
            return "↓";
        }
    }
}

#endif
