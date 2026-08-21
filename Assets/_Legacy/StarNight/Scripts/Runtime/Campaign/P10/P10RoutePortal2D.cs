#if LEGACY_DISABLED
using StarNight.Folklore.P9;
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P10
{
    [DisallowMultipleComponent]
    public sealed class P10RoutePortal2D : P5ContextInteractable2D
    {
        [SerializeField] private P10RoutePortalKind portalKind;
        [SerializeField] private P9BranchKind branch;
        [SerializeField] private P10StageFlowController2D flow;
        [SerializeField] private GameObject availableVisual;
        [SerializeField] private GameObject unavailableVisual;

        public P10RoutePortalKind PortalKind => portalKind;
        public P9BranchKind Branch => branch;
        public bool MainProgressBlocked => false;
        public bool CrossRouteIsOptional =>
            portalKind != P10RoutePortalKind.CrossRoute
            || MainProgressBlocked == false;
        public bool IsAvailable
        {
            get
            {
                P10CampaignDirector2D director =
                    flow != null ? flow.Director : null;
                if (director == null)
                {
                    return false;
                }

                switch (portalKind)
                {
                    case P10RoutePortalKind.FirstBranchChoice:
                        return director.Phase
                            == P10CampaignPhase.BranchChoice;
                    case P10RoutePortalKind.CrossRoute:
                        return director.CanOpenCrossRouteFrom(branch);
                    case P10RoutePortalKind.CommonRegion:
                        return director.CanEnterCommonRegion;
                    default:
                        return false;
                }
            }
        }

        public void Configure(
            P10RoutePortalKind kind,
            P9BranchKind routeBranch,
            P10StageFlowController2D stageFlow,
            GameObject available,
            GameObject unavailable)
        {
            portalKind = kind;
            branch = routeBranch;
            flow = stageFlow;
            availableVisual = available;
            unavailableVisual = unavailable;
            ConfigureInteraction(transform, 1.7f, 95);
            RefreshVisuals();
        }

        public bool TryUsePortal()
        {
            if (!IsAvailable || flow == null)
            {
                RefreshVisuals();
                return false;
            }

            bool used;
            switch (portalKind)
            {
                case P10RoutePortalKind.FirstBranchChoice:
                    used = flow.TryChooseBranchAndEnter(branch);
                    break;
                case P10RoutePortalKind.CrossRoute:
                    used = flow.TryOpenCrossRouteAndEnter(branch);
                    break;
                case P10RoutePortalKind.CommonRegion:
                    used = flow.TryEnterCommonRegion();
                    break;
                default:
                    used = false;
                    break;
            }

            RefreshVisuals();
            return used;
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return IsAvailable;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            return TryUsePortal();
        }

        private void RefreshVisuals()
        {
            if (availableVisual != null)
            {
                availableVisual.SetActive(IsAvailable);
            }

            if (unavailableVisual != null)
            {
                unavailableVisual.SetActive(!IsAvailable);
            }
        }
    }
}

#endif
