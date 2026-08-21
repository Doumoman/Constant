#if LEGACY_DISABLED
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P10
{
    [DisallowMultipleComponent]
    public sealed class P10BranchStageEvent2D :
        P5ContextInteractable2D
    {
        [SerializeField] private P10BranchEventKind eventKind;
        [SerializeField] private P10BranchSupportState2D supportState;
        [SerializeField] private GameObject unresolvedVisual;
        [SerializeField] private GameObject resolvedVisual;

        public P10BranchEventKind EventKind => eventKind;
        public bool IsResolved =>
            supportState != null
            && (eventKind == P10BranchEventKind.RepairMagpieNest
                ? supportState.MagpieNestRepaired
                : supportState.CarpWaterwayRestored);
        public bool MainProgressBlocked => false;
        public bool NextBossSupportReady => IsResolved;

        public void Configure(
            P10BranchEventKind kind,
            P10BranchSupportState2D state,
            GameObject unresolved,
            GameObject resolved)
        {
            eventKind = kind;
            supportState = state;
            unresolvedVisual = unresolved;
            resolvedVisual = resolved;
            ConfigureInteraction(transform, 1.75f, 55);
            RefreshVisuals();
        }

        public bool Resolve()
        {
            if (supportState == null
                || !supportState.Resolve(eventKind))
            {
                return false;
            }

            RefreshVisuals();
            return true;
        }

        public bool IgnoreAndContinue()
        {
            return true;
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return !IsResolved;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            return Resolve();
        }

        private void RefreshVisuals()
        {
            if (unresolvedVisual != null)
            {
                unresolvedVisual.SetActive(!IsResolved);
            }

            if (resolvedVisual != null)
            {
                resolvedVisual.SetActive(IsResolved);
            }
        }
    }
}

#endif
