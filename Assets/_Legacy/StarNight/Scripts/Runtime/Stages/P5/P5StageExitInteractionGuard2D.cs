#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Stages.P5
{
    [DisallowMultipleComponent]
    public sealed class P5StageExitInteractionGuard2D :
        P5ContextInteractable2D
    {
        [SerializeField] private P5StageExit2D stageExit;

        public P5StageExit2D StageExit => stageExit;

        public void Configure(
            P5StageExit2D targetExit,
            Transform interactionPoint,
            float interactionRadius = 1.35f)
        {
            stageExit = targetExit;
            ConfigureInteraction(
                interactionPoint,
                interactionRadius,
                200);
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return stageExit != null
                && stageExit.State != P5StageExitState.Departed;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            // The held state is sampled continuously by P5StageExit2D.
            // This guard only consumes the initial press so CarrySystem does
            // not drop the player's current hand object at the exit.
            return true;
        }
    }
}

#endif
