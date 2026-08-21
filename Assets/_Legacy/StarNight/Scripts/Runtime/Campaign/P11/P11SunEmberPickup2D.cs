#if LEGACY_DISABLED
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    public sealed class P11SunEmberPickup2D :
        P5ContextInteractable2D
    {
        [SerializeField] private P11StoryState2D storyState;
        [SerializeField] private GameObject emberVisual;
        [SerializeField] private GameObject matchingBellSocketCue;

        public bool SunEmberUseInferenceReady =>
            emberVisual != null && matchingBellSocketCue != null;

        public void Configure(
            P11StoryState2D state,
            GameObject visual,
            GameObject bellSocketCue)
        {
            BindStoryState(state);
            emberVisual = visual;
            matchingBellSocketCue = bellSocketCue;
            ConfigureInteraction(transform, 1.6f, 87);
            RefreshVisuals();
        }

        public bool TryCollect()
        {
            bool changed = storyState != null
                && storyState.TryClaimSunEmber();
            RefreshVisuals();
            return changed;
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return storyState != null
                && storyState.CanClaimSunEmber;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            return TryCollect();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            BindStoryState(storyState);
            RefreshVisuals();
        }

        protected override void OnDisable()
        {
            if (storyState != null)
            {
                storyState.StoryFactChanged -= OnStoryFactChanged;
            }

            base.OnDisable();
        }

        private void BindStoryState(P11StoryState2D state)
        {
            if (storyState != null)
            {
                storyState.StoryFactChanged -= OnStoryFactChanged;
            }

            storyState = state;
            if (isActiveAndEnabled && storyState != null)
            {
                storyState.StoryFactChanged += OnStoryFactChanged;
            }
        }

        private void OnStoryFactChanged(P11StoryFact fact)
        {
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            bool visible = storyState != null
                && storyState.SunFlowerDefeated
                && storyState.CrowNestRestored
                && !storyState.HasSunEmber;
            if (emberVisual != null)
            {
                emberVisual.SetActive(visible);
            }

            if (matchingBellSocketCue != null)
            {
                matchingBellSocketCue.SetActive(
                    storyState != null
                    && storyState.HasSunEmber);
            }
        }
    }
}

#endif
