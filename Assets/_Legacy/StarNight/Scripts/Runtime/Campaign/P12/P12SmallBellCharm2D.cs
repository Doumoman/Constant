#if LEGACY_DISABLED
using System;
using StarNight.Maru.P8;
using UnityEngine;

namespace StarNight.Campaign.P12
{
    [DisallowMultipleComponent]
    public sealed class P12SmallBellCharm2D : MonoBehaviour
    {
        public const float DelaySeconds = 30f;
        public const float UsedVisualAlpha = 0.25f;
        private const float ReRingStepSeconds = 0.01f;

        [SerializeField] private P8MaruTimeline2D timeline;
        [SerializeField] private P12ChallengeDirector2D director;
        [SerializeField] private SpriteRenderer charmRenderer;
        [SerializeField] private bool used;

        public event Action<P12SmallBellCharm2D> Used;

        public P8MaruTimeline2D Timeline => timeline;
        public bool IsUsed => used;
        public bool CanUse =>
            !used
            && timeline != null
            && director != null
            && timeline.IsRunning
            && timeline.Phase != P8MaruPhase.Hunting;

        public void Configure(
            P8MaruTimeline2D targetTimeline,
            P12ChallengeDirector2D targetDirector,
            SpriteRenderer visual = null)
        {
            timeline = targetTimeline;
            director = targetDirector;
            charmRenderer = visual != null
                ? visual
                : GetComponentInChildren<SpriteRenderer>();
            used = false;
            ApplyVisual();
        }

        private void Awake()
        {
            if (charmRenderer == null)
            {
                charmRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        public bool TryUse()
        {
            if (!CanUse || !director.TryConsumeSmallBell())
            {
                return false;
            }

            ApplyDelay();
            used = true;
            ApplyVisual();
            Used?.Invoke(this);
            return true;
        }

        public void ResetForTests()
        {
            used = false;
            ApplyVisual();
        }

        private void ApplyDelay()
        {
            float elapsed = timeline.ElapsedSeconds;
            float firstRemaining = timeline.FirstBellSeconds - elapsed;
            float secondRemaining = timeline.SecondBellSeconds - elapsed;
            float dueRemaining = timeline.TimeUntilMaru;
            float first = firstRemaining > 0f
                ? firstRemaining + DelaySeconds
                : ReRingStepSeconds;
            float second = secondRemaining > 0f
                ? secondRemaining + DelaySeconds
                : first + ReRingStepSeconds;
            if (second <= first)
            {
                second = first + ReRingStepSeconds;
            }

            float due = Mathf.Max(
                second + ReRingStepSeconds,
                dueRemaining + DelaySeconds);
            var profile = new P8MaruTimelineProfile(
                timeline.StageSlot,
                first,
                second,
                due,
                timeline.PausedForBoss);
            timeline.Configure(profile);
            timeline.StartTimeline(reset: false);
        }

        private void ApplyVisual()
        {
            if (charmRenderer == null)
            {
                return;
            }

            Color color = charmRenderer.color;
            color.a = used ? UsedVisualAlpha : 1f;
            charmRenderer.color = color;
        }
    }
}

#endif
