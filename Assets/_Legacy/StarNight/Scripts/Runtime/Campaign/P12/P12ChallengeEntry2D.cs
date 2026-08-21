#if LEGACY_DISABLED
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P12
{
    [DisallowMultipleComponent]
    public sealed class P12ChallengeEntry2D : P5ContextInteractable2D
    {
        [SerializeField] private P12ChallengeDirector2D director;
        [SerializeField] private P12StageFlowController2D flow;

        public P12ChallengeDirector2D Director => director;
        public P12StageFlowController2D Flow => flow;
        public bool EntryAvailable =>
            director != null && director.CanEnterChallenge;
        public bool GrantsSmallMaruBellOnEntry => true;

        public void Configure(
            P12ChallengeDirector2D challengeDirector,
            P12StageFlowController2D stageFlow)
        {
            director = challengeDirector;
            flow = stageFlow;
            ConfigureInteraction(transform, 1.6f, 90);
        }

        public bool TryEnterChallenge()
        {
            return EntryAvailable
                && flow != null
                && flow.BeginFromP11Handoff();
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return EntryAvailable && flow != null;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            return TryEnterChallenge();
        }
    }
}

#endif
