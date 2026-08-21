#if LEGACY_DISABLED
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    public sealed class P11YoungCrowEvent2D :
        P5ContextInteractable2D
    {
        [SerializeField] private P11StoryState2D storyState;
        [SerializeField] private GameObject letterSealCue;
        [SerializeField] private GameObject crowGazeCue;
        [SerializeField] private GameObject trustedGuideVisual;
        [SerializeField] private GameObject nestRouteVisual;

        public bool MainExitRemainsAvailable => true;
        public bool LetterUseInferenceReady =>
            letterSealCue != null
            && crowGazeCue != null
            && nestRouteVisual != null;

        public void Configure(
            P11StoryState2D state,
            GameObject sealCue,
            GameObject gazeCue,
            GameObject guideVisual,
            GameObject nestRoute)
        {
            BindStoryState(state);
            letterSealCue = sealCue;
            crowGazeCue = gazeCue;
            trustedGuideVisual = guideVisual;
            nestRouteVisual = nestRoute;
            ConfigureInteraction(transform, 1.9f, 84);
            RefreshVisuals();
        }

        public bool TryPresentLetter()
        {
            bool changed = storyState != null
                && storyState.TryPresentLetterToYoungCrow();
            RefreshVisuals();
            return changed;
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return storyState != null
                && storyState.CanUseLetterAtCrow;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            return TryPresentLetter();
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
            bool trusted = storyState != null
                && storyState.YoungCrowTrustedRani;
            bool hasLetter = storyState != null
                && storyState.HasNaraeLetter;
            if (letterSealCue != null)
            {
                letterSealCue.SetActive(hasLetter && !trusted);
            }

            if (crowGazeCue != null)
            {
                crowGazeCue.SetActive(hasLetter && !trusted);
            }

            if (trustedGuideVisual != null)
            {
                trustedGuideVisual.SetActive(trusted);
            }

            if (nestRouteVisual != null)
            {
                nestRouteVisual.SetActive(trusted);
            }
        }
    }
}

#endif
