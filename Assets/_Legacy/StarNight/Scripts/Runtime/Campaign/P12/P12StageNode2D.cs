#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Campaign.P12
{
    [DisallowMultipleComponent]
    public sealed class P12StageNode2D : MonoBehaviour
    {
        [SerializeField] private P12StageDefinition definition;
        [SerializeField] private P12ChallengeDirector2D director;
        [SerializeField] private P12StageEnvironment2D environment;

        public P12StageDefinition Definition => definition;
        public P12StageId StageId =>
            definition != null
                ? definition.StageId
                : P12StageId.None;
        public P12ChallengeDirector2D Director => director;
        public P12StageEnvironment2D Environment => environment;
        public bool ExitAvailable => definition != null;
        public bool UsesOnlyKnownMechanics =>
            definition != null
            && !definition.IntroducesNewMechanic
            && (definition.Mechanics
                & ~P12ComboRules.AllKnownMechanics)
                == P12StageMechanics.None;

        public void Configure(
            P12StageDefinition stageDefinition,
            P12ChallengeDirector2D challengeDirector,
            P12StageEnvironment2D stageEnvironment)
        {
            definition = stageDefinition;
            director = challengeDirector;
            environment = stageEnvironment;
        }

        public bool TryEnter()
        {
            return director != null
                && director.TryEnterStage(StageId);
        }

        public bool TryComplete()
        {
            return ExitAvailable
                && director != null
                && director.CurrentStage == StageId
                && director.TryCompleteCurrentStage();
        }
    }
}

#endif
