#if LEGACY_DISABLED
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P10
{
    [DisallowMultipleComponent]
    public sealed class P10Floodgate2D : P5ContextInteractable2D
    {
        [SerializeField] private Collider2D barrier;
        [SerializeField] private SpriteRenderer barrierRenderer;
        [SerializeField] private bool open;

        public bool IsOpen => open;
        public bool MainPathBlocked => false;
        public bool IsConfigured =>
            barrier != null && barrierRenderer != null;

        public void Configure(
            Collider2D barrierCollider,
            SpriteRenderer visual)
        {
            barrier = barrierCollider;
            barrierRenderer = visual;
            ConfigureInteraction(transform, 2f, 65);
            ResetForTests();
        }

        public bool TryOpen()
        {
            if (open || barrier == null)
            {
                return false;
            }

            open = true;
            RefreshVisuals();
            return true;
        }

        public void ResetForTests()
        {
            open = false;
            RefreshVisuals();
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return !open && barrier != null;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            return TryOpen();
        }

        private void RefreshVisuals()
        {
            if (barrier != null)
            {
                barrier.enabled = !open;
            }

            if (barrierRenderer != null)
            {
                Color color = barrierRenderer.color;
                color.a = open ? 0.18f : 0.88f;
                barrierRenderer.color = color;
            }
        }
    }
}

#endif
