#if LEGACY_DISABLED
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P12
{
    [DisallowMultipleComponent]
    public sealed class P12StageExit2D : P5ContextInteractable2D
    {
        [SerializeField] private P12StageNode2D stageNode;
        [SerializeField] private P12StageFlowController2D flow;

        public P12StageNode2D StageNode => stageNode;
        public bool ExitAvailable =>
            stageNode != null && stageNode.ExitAvailable;

        public void Configure(
            P12StageNode2D node,
            P12StageFlowController2D stageFlow)
        {
            stageNode = node;
            flow = stageFlow;
            ConfigureInteraction(transform, 1.6f, 90);
        }

        public bool TryDepart()
        {
            return ExitAvailable
                && flow != null
                && flow.ActiveNode == stageNode
                && flow.TryCompleteActiveStage(true);
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return ExitAvailable && flow != null;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            return TryDepart();
        }
    }
}

#endif
