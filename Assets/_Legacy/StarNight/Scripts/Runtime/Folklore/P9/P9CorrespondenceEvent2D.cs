#if LEGACY_DISABLED
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Folklore.P9
{
    [DisallowMultipleComponent]
    public sealed class P9CorrespondenceEvent2D
        : P5ContextInteractable2D
    {
        [SerializeField] private P9CorrespondenceEventKind eventKind;
        [SerializeField] private P9FolkloreChainState2D chainState;
        [SerializeField] private Transform giftVisual;
        [SerializeField] private Transform matchingSilhouette;
        [SerializeField] private Transform npcAttentionCue;
        [SerializeField] private GameObject assistanceRoot;
        [SerializeField] private GameObject mainProgressPath;
        [SerializeField] private bool alternativeResolutionAvailable = true;

        public P9CorrespondenceEventKind EventKind => eventKind;
        public P9FolkloreItemKind RequiredGift =>
            chainState != null
                ? chainState.RequiredGift(eventKind)
                : eventKind == P9CorrespondenceEventKind.HungryMagpie
                    ? P9FolkloreItemKind.MoonCake
                    : P9FolkloreItemKind.JadeRabbitMedicine;
        public P9CorrespondenceResolution Resolution =>
            chainState != null
                ? chainState.ResolutionFor(eventKind)
                : P9CorrespondenceResolution.None;
        public bool IsResolved =>
            Resolution != P9CorrespondenceResolution.None;
        public bool AlternativeResolutionAvailable =>
            alternativeResolutionAvailable;
        public bool MainProgressBlocked => false;
        public bool MatchingGiftCreatesAssistance =>
            assistanceRoot != null;
        public int NonTextCueCount =>
            (giftVisual != null ? 1 : 0)
            + (matchingSilhouette != null ? 1 : 0)
            + (npcAttentionCue != null ? 1 : 0);
        public bool GiftPurposeInferenceReady =>
            NonTextCueCount >= 3
            && mainProgressPath != null
            && mainProgressPath.activeSelf;

        public void Configure(
            P9CorrespondenceEventKind kind,
            P9FolkloreChainState2D state,
            Transform visibleGift,
            Transform silhouette,
            Transform attentionCue,
            GameObject assistance,
            GameObject alwaysOpenMainPath,
            bool hasAlternative = true)
        {
            eventKind = kind;
            chainState = state;
            giftVisual = visibleGift;
            matchingSilhouette = silhouette;
            npcAttentionCue = attentionCue;
            assistanceRoot = assistance;
            mainProgressPath = alwaysOpenMainPath;
            alternativeResolutionAvailable = hasAlternative;
            ConfigureInteraction(transform, 1.75f, 45);

            if (assistanceRoot != null)
            {
                assistanceRoot.SetActive(IsResolved);
            }

            if (mainProgressPath != null)
            {
                mainProgressPath.SetActive(true);
            }
        }

        public bool TryOfferGift(P9FolkloreItemKind offeredItem)
        {
            if (chainState == null
                || !chainState.TryResolveWithGift(eventKind, offeredItem))
            {
                return false;
            }

            ApplyResolutionVisuals();
            return true;
        }

        public bool TryResolveAlternative()
        {
            if (!alternativeResolutionAvailable
                || chainState == null
                || !chainState.TryResolveWithAlternative(eventKind))
            {
                return false;
            }

            ApplyResolutionVisuals();
            return true;
        }

        public bool IgnoreAndContinue()
        {
            return mainProgressPath == null
                || mainProgressPath.activeSelf;
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return !IsResolved
                && chainState != null
                && (chainState.HasItem(RequiredGift)
                    || alternativeResolutionAvailable);
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            return chainState != null
                && chainState.HasItem(RequiredGift)
                    ? TryOfferGift(RequiredGift)
                    : TryResolveAlternative();
        }

        private void ApplyResolutionVisuals()
        {
            if (assistanceRoot != null)
            {
                assistanceRoot.SetActive(true);
            }

            if (giftVisual != null)
            {
                giftVisual.gameObject.SetActive(false);
            }

            if (matchingSilhouette != null)
            {
                matchingSilhouette.gameObject.SetActive(false);
            }
        }
    }
}

#endif
