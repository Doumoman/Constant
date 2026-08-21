#if LEGACY_DISABLED
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Folklore.P9
{
    [DisallowMultipleComponent]
    public sealed class P9FolkloreGiftPickup2D
        : P5ContextInteractable2D
    {
        [SerializeField] private P9FolkloreItemKind itemKind;
        [SerializeField] private P9FolkloreChainState2D chainState;
        [SerializeField] private bool collected;

        public P9FolkloreItemKind ItemKind => itemKind;
        public bool Collected => collected;
        public bool ImportantItemCannotBePermanentlyLost => true;

        public void Configure(
            P9FolkloreItemKind item,
            P9FolkloreChainState2D state)
        {
            itemKind = item;
            chainState = state;
            collected = false;
            ConfigureInteraction(transform, 1.4f, 55);
        }

        public bool Collect()
        {
            if (collected || chainState == null)
            {
                return false;
            }

            bool granted = chainState.GrantItem(itemKind);
            collected = granted || chainState.HasItem(itemKind);
            if (collected)
            {
                gameObject.SetActive(false);
            }

            return collected;
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return !collected
                && chainState != null
                && !chainState.HasItem(itemKind);
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            return Collect();
        }
    }
}

#endif
