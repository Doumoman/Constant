#if LEGACY_DISABLED
using System;
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    public sealed class P11MemoryBellDevice2D :
        P5ContextInteractable2D
    {
        public const float DefaultPulseSeconds = 0.8f;

        [SerializeField] private Transform directionTarget;
        [SerializeField] private SpriteRenderer bellVisual;
        [SerializeField] private GameObject directionReveal;
        [SerializeField, Min(0.05f)] private float pulseSeconds =
            DefaultPulseSeconds;
        [SerializeField] private Color pulseColor =
            new Color(0.72f, 0.94f, 1f, 1f);
        [SerializeField] private bool directionRevealed;
        [SerializeField] private float pulseRemaining;
        [SerializeField] private int ringCount;
        private Color baseColor = Color.white;
        private Vector3 baseScale = Vector3.one;

        public event Action Ringed;

        public Transform DirectionTarget => directionTarget;
        public bool DirectionRevealed => directionRevealed;
        public bool IsPulsing => pulseRemaining > 0f;
        public float PulseRemaining => pulseRemaining;
        public int RingCount => ringCount;
        public bool IsFinalStoryBell => false;
        public Vector2 RevealedDirection
        {
            get
            {
                if (directionTarget == null)
                {
                    return Vector2.zero;
                }

                Vector2 offset = directionTarget.position
                    - transform.position;
                return offset.sqrMagnitude > 0.001f
                    ? offset.normalized
                    : Vector2.zero;
            }
        }
        public bool IsConfigured =>
            directionTarget != null
            && (bellVisual != null || directionReveal != null);

        public void Configure(
            Transform target,
            SpriteRenderer visual,
            GameObject directionVisual,
            float visualPulseSeconds = DefaultPulseSeconds,
            Transform interactionPoint = null,
            float interactionRadius = 1.6f,
            int interactionPriority = 72)
        {
            directionTarget = target;
            bellVisual = visual;
            directionReveal = directionVisual;
            pulseSeconds = Mathf.Max(0.05f, visualPulseSeconds);
            directionRevealed = false;
            pulseRemaining = 0f;
            ringCount = 0;
            if (bellVisual != null)
            {
                baseColor = bellVisual.color;
                baseScale = bellVisual.transform.localScale;
            }

            ConfigureInteraction(
                interactionPoint != null
                    ? interactionPoint
                    : transform,
                interactionRadius,
                interactionPriority);
            RefreshDirectionVisual();
            RefreshPulseVisual();
        }

        public bool Ring()
        {
            if (!IsConfigured)
            {
                return false;
            }

            directionRevealed = true;
            pulseRemaining = pulseSeconds;
            ringCount++;
            RefreshDirectionVisual();
            RefreshPulseVisual();
            Ringed?.Invoke();
            return true;
        }

        public void TickForTests(float deltaSeconds)
        {
            if (pulseRemaining <= 0f)
            {
                return;
            }

            pulseRemaining = Mathf.Max(
                0f,
                pulseRemaining - Mathf.Max(0f, deltaSeconds));
            RefreshPulseVisual();
        }

        public void ResetForTests()
        {
            directionRevealed = false;
            pulseRemaining = 0f;
            ringCount = 0;
            RefreshDirectionVisual();
            RefreshPulseVisual();
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return isActiveAndEnabled && IsConfigured;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            return Ring();
        }

        private void Update()
        {
            TickForTests(Time.deltaTime);
        }

        private void RefreshDirectionVisual()
        {
            if (directionReveal == null)
            {
                return;
            }

            directionReveal.SetActive(directionRevealed);
            Vector2 direction = RevealedDirection;
            if (direction.sqrMagnitude > 0.001f)
            {
                directionReveal.transform.right = direction;
            }
        }

        private void RefreshPulseVisual()
        {
            if (bellVisual == null)
            {
                return;
            }

            bool pulsing = IsPulsing;
            bellVisual.color = pulsing ? pulseColor : baseColor;
            bellVisual.transform.localScale = pulsing
                ? baseScale * 1.15f
                : baseScale;
        }
    }
}

#endif
