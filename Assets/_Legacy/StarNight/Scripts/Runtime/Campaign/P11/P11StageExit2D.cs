#if LEGACY_DISABLED
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    public sealed class P11StageExit2D : P5ContextInteractable2D
    {
        [SerializeField] private P11StageNode2D stageNode;
        [SerializeField] private P11StageFlowController2D flow;
        [SerializeField] private GameObject readyVisual;
        [SerializeField] private GameObject lockedVisual;

        public P11StageNode2D StageNode => stageNode;
        public bool ExitAvailable =>
            stageNode != null && stageNode.ExitAvailable;
        public bool MainPathExit => true;

        public void Configure(
            P11StageNode2D node,
            P11StageFlowController2D stageFlow,
            GameObject ready,
            GameObject locked)
        {
            stageNode = node;
            flow = stageFlow;
            readyVisual = ready;
            lockedVisual = locked;
            ConfigureInteraction(transform, 1.6f, 90);
            RefreshVisuals();
        }

        public bool TryDepart()
        {
            RefreshVisuals();
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
