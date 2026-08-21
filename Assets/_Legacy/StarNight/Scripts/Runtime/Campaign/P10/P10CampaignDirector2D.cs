#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Folklore.P9;
using UnityEngine;

namespace StarNight.Campaign.P10
{
    [DefaultExecutionOrder(-940)]
    [DisallowMultipleComponent]
    public sealed class P10CampaignDirector2D : MonoBehaviour
    {
        [SerializeField] private P10CampaignCatalog catalog;
        [SerializeField] private P9FolkloreChainState2D folkloreState;
        [SerializeField] private P10RouteTelemetry2D telemetry;
        [SerializeField] private P10BranchSupportState2D branchSupport;
        [SerializeField] private P10CampaignPhase phase =
            P10CampaignPhase.MoonPalace;
        [SerializeField] private P10StageId currentStage;
        [SerializeField] private P9BranchKind selectedFirstBranch;
        [SerializeField] private P9BranchKind completedFirstBranch;
        [SerializeField] private P9BranchKind activeCrossBranch;
        [SerializeField] private P9BranchKind completedCrossBranch;
        [SerializeField] private List<P10StageId> completedStages =
            new List<P10StageId>();
        [SerializeField] private bool commonRegionEntered;

        public event Action<P10StageId> StageEntered;
        public event Action<P10StageId> StageCompleted;
        public event Action<P9BranchKind> FirstBranchChosen;
        public event Action<P9BranchKind> CrossRouteOpened;
        public event Action CommonRegionEntered;

        public P10CampaignCatalog Catalog => catalog;
        public P9FolkloreChainState2D FolkloreState => folkloreState;
        public P10RouteTelemetry2D Telemetry => telemetry;
        public P10BranchSupportState2D BranchSupport => branchSupport;
        public P10CampaignPhase Phase => phase;
        public P10StageId CurrentStage => currentStage;
        public P9BranchKind SelectedFirstBranch =>
            selectedFirstBranch;
        public P9BranchKind CompletedFirstBranch =>
            completedFirstBranch;
        public P9BranchKind ActiveCrossBranch => activeCrossBranch;
        public P9BranchKind CompletedCrossBranch =>
            completedCrossBranch;
        public IReadOnlyList<P10StageId> CompletedStages =>
            completedStages;
        public bool CommonRegionEnteredSuccessfully =>
            commonRegionEntered;
        public bool CanEnterCommonRegion =>
            completedFirstBranch != P9BranchKind.None
            && currentStage == P10StageId.None
            && !commonRegionEntered;
        public bool IsSecondBranchMode =>
            activeCrossBranch != P9BranchKind.None;
        public float SecondBranchShopFrequencyMultiplier =>
            IsSecondBranchMode ? 0.5f : 1f;
        public float SecondBranchBellIntervalMultiplier =>
            IsSecondBranchMode ? 0.82f : 1f;
        public bool MainProgressRequiresCrossRoute => false;

        public void Configure(
            P10CampaignCatalog campaignCatalog,
            P9FolkloreChainState2D chainState,
            P10RouteTelemetry2D routeTelemetry,
            P10BranchSupportState2D supportState)
        {
            catalog = campaignCatalog;
            folkloreState = chainState;
            telemetry = routeTelemetry;
            branchSupport = supportState;
            ResetCampaignForTests();
        }

        public bool HasCompleted(P10StageId stageId)
        {
            return completedStages.Contains(stageId);
        }

        public bool ChooseFirstBranch(P9BranchKind branch)
        {
            if (phase != P10CampaignPhase.BranchChoice
                || selectedFirstBranch != P9BranchKind.None
                || (branch != P9BranchKind.MagpieBridge
                    && branch != P9BranchKind.DragonPalace))
            {
                return false;
            }

            selectedFirstBranch = branch;
            phase = P10CampaignPhase.FirstBranch;
            telemetry?.MarkFirstBranchChosen(branch);
            FirstBranchChosen?.Invoke(branch);
            return true;
        }

        public bool CanEnterStage(P10StageId stageId)
        {
            if (catalog == null
                || catalog.Find(stageId) == null
                || currentStage != P10StageId.None
                || HasCompleted(stageId))
            {
                return false;
            }

            switch (stageId)
            {
                case P10StageId.MoonPalace11:
                    return completedStages.Count == 0
                        && phase == P10CampaignPhase.MoonPalace;
                case P10StageId.MoonPalace12:
                    return HasCompleted(P10StageId.MoonPalace11);
                case P10StageId.MoonPalace13:
                    return HasCompleted(P10StageId.MoonPalace12);
                case P10StageId.MagpieBridge21:
                    return phase == P10CampaignPhase.FirstBranch
                        && selectedFirstBranch
                            == P9BranchKind.MagpieBridge
                        && HasCompleted(P10StageId.MoonPalace13);
                case P10StageId.DragonPalace21:
                    return phase == P10CampaignPhase.FirstBranch
                        && selectedFirstBranch
                            == P9BranchKind.DragonPalace
                        && HasCompleted(P10StageId.MoonPalace13);
                case P10StageId.MagpieBridge22:
                    return CanEnterBranchSecondStage(
                        P9BranchKind.MagpieBridge,
                        P10StageId.MagpieBridge21);
                case P10StageId.DragonPalace22:
                    return CanEnterBranchSecondStage(
                        P9BranchKind.DragonPalace,
                        P10StageId.DragonPalace21);
                case P10StageId.MagpieBridge23:
                    return IsBranchCurrentlyActive(
                            P9BranchKind.MagpieBridge)
                        && HasCompleted(P10StageId.MagpieBridge22);
                case P10StageId.DragonPalace23:
                    return IsBranchCurrentlyActive(
                            P9BranchKind.DragonPalace)
                        && HasCompleted(P10StageId.DragonPalace22);
                default:
                    return false;
            }
        }

        public bool TryEnterStage(P10StageId stageId)
        {
            if (!CanEnterStage(stageId))
            {
                return false;
            }

            currentStage = stageId;
            telemetry?.MarkStageEntered(stageId);
            StageEntered?.Invoke(stageId);
            return true;
        }

        public bool TryCompleteCurrentStage()
        {
            if (currentStage == P10StageId.None)
            {
                return false;
            }

            P10StageId completed = currentStage;
            currentStage = P10StageId.None;
            if (!completedStages.Contains(completed))
            {
                completedStages.Add(completed);
            }

            ApplyCompletionTransition(completed);
            telemetry?.MarkStageCompleted(completed);
            StageCompleted?.Invoke(completed);
            return true;
        }

        public bool CanOpenCrossRouteFrom(P9BranchKind sourceBranch)
        {
            return completedFirstBranch == sourceBranch
                && completedCrossBranch == P9BranchKind.None
                && activeCrossBranch == P9BranchKind.None
                && currentStage == P10StageId.None
                && folkloreState != null
                && folkloreState.CanEnterOppositeBranchAfter(
                    sourceBranch);
        }

        public bool TryOpenCrossRouteFrom(
            P9BranchKind sourceBranch)
        {
            if (!CanOpenCrossRouteFrom(sourceBranch))
            {
                return false;
            }

            P9CorrespondenceEventKind courierEvent =
                sourceBranch == P9BranchKind.MagpieBridge
                    ? P9CorrespondenceEventKind.InjuredTurtle
                    : P9CorrespondenceEventKind.HungryMagpie;
            P9FolkloreItemKind gift =
                sourceBranch == P9BranchKind.MagpieBridge
                    ? P9FolkloreItemKind.JadeRabbitMedicine
                    : P9FolkloreItemKind.MoonCake;
            if (!folkloreState.TryResolveWithGift(
                    courierEvent,
                    gift))
            {
                return false;
            }

            activeCrossBranch =
                sourceBranch == P9BranchKind.MagpieBridge
                    ? P9BranchKind.DragonPalace
                    : P9BranchKind.MagpieBridge;
            phase = P10CampaignPhase.SecondBranch;
            telemetry?.MarkCrossRouteOpened(activeCrossBranch);
            CrossRouteOpened?.Invoke(activeCrossBranch);
            return true;
        }

        public bool TryEnterCommonRegion()
        {
            if (!CanEnterCommonRegion)
            {
                return false;
            }

            commonRegionEntered = true;
            phase = P10CampaignPhase.DepartedToCommonRegion;
            telemetry?.CompleteNormalRouteAtCommonEntry();
            CommonRegionEntered?.Invoke();
            return true;
        }

        public void ResetCampaignForTests()
        {
            phase = P10CampaignPhase.MoonPalace;
            currentStage = P10StageId.None;
            selectedFirstBranch = P9BranchKind.None;
            completedFirstBranch = P9BranchKind.None;
            activeCrossBranch = P9BranchKind.None;
            completedCrossBranch = P9BranchKind.None;
            completedStages.Clear();
            commonRegionEntered = false;
            branchSupport?.ResetForTests();
            telemetry?.ResetTelemetryForTests();
        }

        private bool CanEnterBranchSecondStage(
            P9BranchKind branch,
            P10StageId firstStage)
        {
            if (!IsBranchCurrentlyActive(branch))
            {
                return false;
            }

            if (activeCrossBranch == branch)
            {
                return completedFirstBranch != P9BranchKind.None;
            }

            return HasCompleted(firstStage);
        }

        private bool IsBranchCurrentlyActive(P9BranchKind branch)
        {
            if (phase == P10CampaignPhase.FirstBranch)
            {
                return selectedFirstBranch == branch;
            }

            return phase == P10CampaignPhase.SecondBranch
                && activeCrossBranch == branch;
        }

        private void ApplyCompletionTransition(P10StageId completed)
        {
            if (completed == P10StageId.MoonPalace13)
            {
                phase = P10CampaignPhase.BranchChoice;
                return;
            }

            P9BranchKind completedBossBranch =
                completed == P10StageId.MagpieBridge23
                    ? P9BranchKind.MagpieBridge
                    : completed == P10StageId.DragonPalace23
                        ? P9BranchKind.DragonPalace
                        : P9BranchKind.None;
            if (completedBossBranch == P9BranchKind.None)
            {
                return;
            }

            folkloreState?.GrantBossRelic(completedBossBranch);
            if (completedFirstBranch == P9BranchKind.None)
            {
                completedFirstBranch = completedBossBranch;
                phase = folkloreState != null
                    && folkloreState.CanEnterOppositeBranchAfter(
                        completedBossBranch)
                        ? P10CampaignPhase.CrossRouteOffered
                        : P10CampaignPhase.CommonRegionReady;
                return;
            }

            completedCrossBranch = completedBossBranch;
            activeCrossBranch = P9BranchKind.None;
            phase = P10CampaignPhase.CommonRegionReady;
        }
    }
}

#endif
