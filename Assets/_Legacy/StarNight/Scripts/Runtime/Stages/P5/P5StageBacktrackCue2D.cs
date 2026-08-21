#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Stages.P5
{
    [DisallowMultipleComponent]
    public sealed class P5StageBacktrackCue2D : MonoBehaviour
    {
        [SerializeField] private P5MoonRabbitPestleEvent2D optionalEvent;
        [SerializeField] private GameObject incompleteChoiceIcon;
        [SerializeField] private GameObject nearestOptionalEntranceHighlight;
        [SerializeField, Min(0.1f)] private float cueDuration = 2.5f;

        private SpriteRenderer[] highlightRenderers =
            Array.Empty<SpriteRenderer>();
        private Color[] highlightBaseColors = Array.Empty<Color>();
        private float elapsed;

        public event Action CueStarted;
        public event Action CueFinished;

        public bool WasPlayed { get; private set; }
        public bool IsPlaying { get; private set; }
        public int PlayCount { get; private set; }

        public void Configure(
            GameObject choiceIcon,
            GameObject entranceHighlight,
            float duration = 2.5f,
            P5MoonRabbitPestleEvent2D targetOptionalEvent = null)
        {
            optionalEvent = targetOptionalEvent;
            incompleteChoiceIcon = choiceIcon;
            nearestOptionalEntranceHighlight = entranceHighlight;
            cueDuration = Mathf.Max(0.1f, duration);
            highlightRenderers = nearestOptionalEntranceHighlight != null
                ? nearestOptionalEntranceHighlight
                    .GetComponentsInChildren<SpriteRenderer>(true)
                : Array.Empty<SpriteRenderer>();
            highlightBaseColors = new Color[highlightRenderers.Length];
            for (int index = 0; index < highlightRenderers.Length; index++)
            {
                highlightBaseColors[index] = highlightRenderers[index].color;
            }

            ResetCueForTests();
        }

        private void Update()
        {
            if (!IsPlaying)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            for (int index = 0; index < highlightRenderers.Length; index++)
            {
                if (highlightRenderers[index] == null)
                {
                    continue;
                }

                Color color = highlightBaseColors[index];
                color.a = highlightBaseColors[index].a
                    * (0.45f + 0.55f * Mathf.Abs(Mathf.Sin(elapsed * 8f)));
                highlightRenderers[index].color = color;
            }

            if (elapsed >= cueDuration)
            {
                FinishCue();
            }
        }

        public bool PlayOnce()
        {
            if (WasPlayed
                || (optionalEvent != null && optionalEvent.IsCompleted))
            {
                return false;
            }

            WasPlayed = true;
            IsPlaying = true;
            PlayCount++;
            elapsed = 0f;
            SetPresentation(true);
            CueStarted?.Invoke();
            return true;
        }

        public void ResetCueForTests()
        {
            WasPlayed = false;
            IsPlaying = false;
            PlayCount = 0;
            elapsed = 0f;
            SetPresentation(false);
        }

        private void FinishCue()
        {
            if (!IsPlaying)
            {
                return;
            }

            IsPlaying = false;
            SetPresentation(false);
            CueFinished?.Invoke();
        }

        private void SetPresentation(bool visible)
        {
            if (incompleteChoiceIcon != null)
            {
                incompleteChoiceIcon.SetActive(visible);
            }

            if (nearestOptionalEntranceHighlight != null)
            {
                nearestOptionalEntranceHighlight.SetActive(visible);
            }

            for (int index = 0; index < highlightRenderers.Length; index++)
            {
                if (highlightRenderers[index] != null)
                {
                    highlightRenderers[index].color =
                        highlightBaseColors[index];
                }
            }
        }
    }
}

#endif
