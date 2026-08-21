#if LEGACY_DISABLED
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Folklore.P9
{
    [DisallowMultipleComponent]
    public sealed class P9BranchRelicPickup2D
        : P5ContextInteractable2D
    {
        [SerializeField] private P9BranchKind branch;
        [SerializeField] private P9FolkloreChainState2D chainState;
        [SerializeField] private bool collected;

        public P9BranchKind Branch => branch;
        public P9FolkloreItemKind RelicKind =>
            branch == P9BranchKind.MagpieBridge
                ? P9FolkloreItemKind.RedWeaverThread
                : P9FolkloreItemKind.DragonPalaceOrb;
        public bool Collected => collected;
        public bool ImportantItemCannotBePermanentlyLost => true;

        public void Configure(
            P9BranchKind relicBranch,
            P9FolkloreChainState2D state)
        {
            branch = relicBranch;
            chainState = state;
            collected = false;
            ConfigureInteraction(transform, 1.4f, 50);
        }

        public bool Collect()
        {
            if (collected
                || chainState == null
                || !chainState.TryGrantBranchRelic(branch))
            {
                return false;
            }

            collected = true;
            gameObject.SetActive(false);
            return true;
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            P9CorrespondenceEventKind requiredEvent =
                branch == P9BranchKind.MagpieBridge
                    ? P9CorrespondenceEventKind.HungryMagpie
                    : P9CorrespondenceEventKind.InjuredTurtle;
            return !collected
                && chainState != null
                && chainState.IsEventResolved(requiredEvent);
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            return Collect();
        }
    }
}

#endif
