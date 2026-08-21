#if LEGACY_DISABLED
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P10
{
    [DisallowMultipleComponent]
    public sealed class P10StageExit2D : P5ContextInteractable2D
    {
        [SerializeField] private P10StageNode2D stageNode;
        [SerializeField] private P10StageFlowController2D flow;
        [SerializeField] private GameObject readyVisual;
        [SerializeField] private GameObject lockedVisual;
        [SerializeField] private bool advanceLinear = true;

        public P10StageNode2D StageNode => stageNode;
        public bool ExitAvailable =>
            stageNode != null && stageNode.ExitAvailable;
        public bool MainPathExit => true;

        public void Configure(
            P10StageNode2D node,
            P10StageFlowController2D stageFlow,
            GameObject ready,
            GameObject locked,
            bool advanceToNextLinearStage)
        {
            stageNode = node;
            flow = stageFlow;
            readyVisual = ready;
            lockedVisual = locked;
            advanceLinear = advanceToNextLinearStage;
            ConfigureInteraction(transform, 1.6f, 90);
            RefreshVisuals();
        }

        public bool TryDepart()
        {
            RefreshVisuals();
            return ExitAvailable
                && flow != null
                && flow.ActiveNode == stageNode
                && flow.TryCompleteActiveStage(advanceLinear);
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

        private void RefreshVisuals()
        {
            if (readyVisual != null)
            {
                readyVisual.SetActive(ExitAvailable);
            }

            if (lockedVisual != null)
            {
                lockedVisual.SetActive(!ExitAvailable);
            }
        }
    }
}

#endif
