#if LEGACY_DISABLED
using StarNight.Stages.P5;
using StarNight.Tools;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    public sealed class P11CrowNestEvent2D :
        P5ContextInteractable2D
    {
        [SerializeField] private P11StoryState2D storyState;
        [SerializeField] private GameObject overheatedRootVisual;
        [SerializeField] private GameObject sealedRingVisual;
        [SerializeField] private GameObject restoredNestVisual;
        [SerializeField] private GameObject bossSupportVisual;

        public bool RequiresWaterAndHook => true;
        public bool MainExitRemainsAvailable => true;

        public void Configure(
            P11StoryState2D state,
            GameObject hotRoot,
            GameObject sealedRing,
            GameObject restoredNest,
            GameObject supportVisual)
        {
            BindStoryState(state);
            overheatedRootVisual = hotRoot;
            sealedRingVisual = sealedRing;
            restoredNestVisual = restoredNest;
            bossSupportVisual = supportVisual;
            ConfigureInteraction(transform, 2.1f, 83);
            RefreshVisuals();
        }

        public bool ApplyWater()
        {
            bool changed = storyState != null
                && storyState.CoolNestRoot();
            TryFinish();
            RefreshVisuals();
            return changed;
        }

        public bool ApplyHook()
        {
            bool changed = storyState != null
                && storyState.ReleaseNestSeal();
            TryFinish();
            RefreshVisuals();
            return changed;
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return storyState != null
                && storyState.YoungCrowTrustedRani
                && !storyState.CrowNestRestored
                && context.ToolInventory != null
                && context.ToolInventory.HasHeldTool
                && (context.ToolInventory.HeldTool.Kind
                        == HandToolKind.WateringCan
                    || context.ToolInventory.HeldTool.Kind
                        == HandToolKind.Grapple);
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            if (context.ToolInventory.HeldTool.Kind
                == HandToolKind.WateringCan)
            {
                return ApplyWater();
            }

            return ApplyHook();
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

        private void TryFinish()
        {
            storyState?.TryRestoreCrowNest();
        }

        private void RefreshVisuals()
        {
            if (overheatedRootVisual != null)
            {
                overheatedRootVisual.SetActive(
                    storyState == null
                    || !storyState.NestRootCooled);
            }

            if (sealedRingVisual != null)
            {
                sealedRingVisual.SetActive(
                    storyState == null
                    || !storyState.NestSealReleased);
            }

            bool restored = storyState != null
                && storyState.CrowNestRestored;
            if (restoredNestVisual != null)
            {
                restoredNestVisual.SetActive(restored);
            }

            if (bossSupportVisual != null)
            {
                bossSupportVisual.SetActive(restored);
            }
        }
    }
}

#endif
