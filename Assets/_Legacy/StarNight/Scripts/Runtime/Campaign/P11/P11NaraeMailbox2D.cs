#if LEGACY_DISABLED
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    public sealed class P11NaraeMailbox2D : P5ContextInteractable2D
    {
        [SerializeField] private P11StoryState2D storyState;
        [SerializeField] private GameObject closedDrawerVisual;
        [SerializeField] private GameObject openDrawerVisual;
        [SerializeField] private GameObject letterVisual;
        [SerializeField] private GameObject dawnMapVisual;
        [SerializeField] private SpriteRenderer redThreadSeal;
        [SerializeField] private SpriteRenderer dragonOrbGlyph;

        public P11StoryState2D StoryState => storyState;
        public bool CanOpenLetterDrawer =>
            storyState != null && storyState.CanOpenLetterDrawer;
        public bool CanRevealDawnCoordinates =>
            storyState != null
            && storyState.CanRevealDawnCoordinates;
        public bool VisualInferenceReady =>
            redThreadSeal != null
            && dragonOrbGlyph != null
            && letterVisual != null;

        public void Configure(
            P11StoryState2D state,
            GameObject closedDrawer,
            GameObject openDrawer,
            GameObject letter,
            GameObject dawnMap,
            SpriteRenderer threadSeal,
            SpriteRenderer orbGlyph)
        {
            BindStoryState(state);
            closedDrawerVisual = closedDrawer;
            openDrawerVisual = openDrawer;
            letterVisual = letter;
            dawnMapVisual = dawnMap;
            redThreadSeal = threadSeal;
            dragonOrbGlyph = orbGlyph;
            ConfigureInteraction(transform, 1.8f, 86);
            RefreshVisuals();
        }

        public bool TryOpen()
        {
            if (storyState == null)
            {
                return false;
            }

            bool changed = storyState.TryOpenNaraeLetterDrawer();
            changed |= storyState.TryRevealDawnStarCoordinates();
            RefreshVisuals();
            return changed;
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return CanOpenLetterDrawer || CanRevealDawnCoordinates;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            return TryOpen();
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
            bool opened = storyState != null
                && storyState.HasNaraeLetter;
            if (closedDrawerVisual != null)
            {
                closedDrawerVisual.SetActive(!opened);
            }

            if (openDrawerVisual != null)
            {
                openDrawerVisual.SetActive(opened);
            }

            if (letterVisual != null)
            {
                letterVisual.SetActive(opened);
            }

            if (dawnMapVisual != null)
            {
                dawnMapVisual.SetActive(
                    storyState != null
                    && storyState.HasDawnStarCoordinates);
            }

            if (redThreadSeal != null)
            {
                redThreadSeal.enabled = storyState != null
                    && storyState.FolkloreState != null
                    && storyState.FolkloreState.HasRedWeaverThread;
            }

            if (dragonOrbGlyph != null)
            {
                dragonOrbGlyph.enabled = storyState != null
                    && storyState.FolkloreState != null
                    && storyState.FolkloreState.HasDragonPalaceOrb;
            }
        }
    }
}

#endif
