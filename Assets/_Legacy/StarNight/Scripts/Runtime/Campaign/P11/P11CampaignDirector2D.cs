#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Campaign.P10;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DefaultExecutionOrder(-910)]
    [DisallowMultipleComponent]
    public sealed class P11CampaignDirector2D : MonoBehaviour
    {
        [SerializeField] private P11CampaignCatalog catalog;
        [SerializeField] private P10CampaignDirector2D p10Director;
        [SerializeField] private P11StoryState2D storyState;
        [SerializeField] private P11ComprehensionTelemetry2D telemetry;
        [SerializeField] private P11CampaignPhase phase =
            P11CampaignPhase.WaitingForCommonRegion;
        [SerializeField] private P11StageId currentStage;
        [SerializeField] private List<P11StageId> completedStages =
            new List<P11StageId>();
        [SerializeField] private bool commonRegionAccepted;
        [SerializeField] private P11EndingKind ending;

        public event Action CommonRegionAccepted;
        public event Action<P11StageId> StageEntered;
        public event Action<P11StageId> StageCompleted;
        public event Action<P11EndingKind> EndingResolved;

        public P11CampaignCatalog Catalog => catalog;
        public P10CampaignDirector2D P10Director => p10Director;
        public P11StoryState2D StoryState => storyState;
        public P11ComprehensionTelemetry2D Telemetry => telemetry;
        public P11CampaignPhase Phase => phase;
        public P11StageId CurrentStage => currentStage;
        public IReadOnlyList<P11StageId> CompletedStages =>
            completedStages;
        public bool CommonRegionAcceptedSuccessfully =>
            commonRegionAccepted;
        public P11EndingKind Ending => ending;
        public bool HasEnded => phase == P11CampaignPhase.Ended;
        public bool MainProgressRequiresOptionalStory => false;
        public bool NormalEndingAvailableWithoutLetterOrEmber => true;

        public void Configure(
            P11CampaignCatalog campaignCatalog,
            P10CampaignDirector2D firstBranchDirector,
            P11StoryState2D commonStoryState,
            P11ComprehensionTelemetry2D comprehensionTelemetry)
        {
            catalog = campaignCatalog;
            p10Director = firstBranchDirector;
            storyState = commonStoryState;
            telemetry = comprehensionTelemetry;
            ResetCampaignForTests(false);
        }

        public bool HasCompleted(P11StageId stageId)
        {
            return completedStages.Contains(stageId);
        }

        public bool TryAcceptP10Handoff()
        {
            if (commonRegionAccepted
                || phase != P11CampaignPhase.WaitingForCommonRegion
                || p10Director == null
                || !p10Director.CommonRegionEnteredSuccessfully)
            {
                return false;
            }

            AcceptCommonRegion();
            return true;
        }

        public bool CanEnterStage(P11StageId stageId)
        {
            if (!commonRegionAccepted
                || catalog == null
                || catalog.Find(stageId) == null
                || currentStage != P11StageId.None
                || HasCompleted(stageId)
                || HasEnded)
            {
                return false;
            }

            switch (stageId)
            {
                case P11StageId.StarPostOffice31:
                    return completedStages.Count == 0
                        && phase == P11CampaignPhase.StarPostOffice;
                case P11StageId.StarPostOffice32:
                    return HasCompleted(P11StageId.StarPostOffice31);
                case P11StageId.StarPostOffice33:
                    return HasCompleted(P11StageId.StarPostOffice32);
                case P11StageId.SunriseGarden41:
                    return HasCompleted(P11StageId.StarPostOffice33)
                        && phase == P11CampaignPhase.SunriseGarden;
                case P11StageId.SunriseGarden42:
                    return HasCompleted(P11StageId.SunriseGarden41);
                case P11StageId.SunriseGarden43:
                    return HasCompleted(P11StageId.SunriseGarden42);
                case P11StageId.PolarisObservatory51:
                    return HasCompleted(P11StageId.SunriseGarden43)
                        && phase == P11CampaignPhase.PolarisObservatory;
                case P11StageId.PolarisObservatory52:
                    return HasCompleted(
                        P11StageId.PolarisObservatory51);
                case P11StageId.PolarisObservatory53:
                    return HasCompleted(
                        P11StageId.PolarisObservatory52);
                default:
                    return false;
            }
        }

        public bool TryEnterStage(P11StageId stageId)
        {
            if (!CanEnterStage(stageId))
            {
                return false;
            }

            currentStage = stageId;
            StageEntered?.Invoke(stageId);
            return true;
        }

        public bool TryCompleteCurrentStage()
        {
            if (currentStage == P11StageId.None
                || currentStage == P11StageId.PolarisObservatory53)
            {
                return false;
            }

            P11StageId completed = currentStage;
            currentStage = P11StageId.None;
            AddCompleted(completed);
            ApplyCompletionTransition(completed);
            StageCompleted?.Invoke(completed);
            return true;
        }

        public bool CanResolveEnding(P11EndingKind candidate)
        {
            if (currentStage != P11StageId.PolarisObservatory53
                || ending != P11EndingKind.None
                || candidate == P11EndingKind.None)
            {
                return false;
            }

            return candidate != P11EndingKind.Memory
                || (storyState != null
                    && storyState.MemoryRouteCompleted);
        }

        public bool TryResolveEnding(P11EndingKind candidate)
        {
            if (!CanResolveEnding(candidate))
            {
                return false;
            }

            ending = candidate;
            P11StageId completed = currentStage;
            currentStage = P11StageId.None;
            AddCompleted(completed);
            phase = P11CampaignPhase.Ended;
            StageCompleted?.Invoke(completed);
            EndingResolved?.Invoke(candidate);
            return true;
        }

        public void ResetCampaignForTests(
            bool commonRegionReady = false)
        {
            completedStages.Clear();
            currentStage = P11StageId.None;
            commonRegionAccepted = false;
            ending = P11EndingKind.None;
            phase = P11CampaignPhase.WaitingForCommonRegion;
            if (commonRegionReady)
            {
                AcceptCommonRegion();
            }
        }

        public void AcceptCommonRegionForTests()
        {
            if (!commonRegionAccepted)
            {
                AcceptCommonRegion();
            }
        }

        private void AcceptCommonRegion()
        {
            commonRegionAccepted = true;
            phase = P11CampaignPhase.StarPostOffice;
            CommonRegionAccepted?.Invoke();
        }

        private void AddCompleted(P11StageId stageId)
        {
            if (!completedStages.Contains(stageId))
            {
                completedStages.Add(stageId);
            }
        }

        private void ApplyCompletionTransition(P11StageId completed)
        {
            switch (completed)
            {
                case P11StageId.StarPostOffice33:
                    phase = P11CampaignPhase.SunriseGarden;
                    break;
                case P11StageId.SunriseGarden43:
                    phase = P11CampaignPhase.PolarisObservatory;
                    break;
                case P11StageId.PolarisObservatory52:
                    phase = P11CampaignPhase.EndingChoice;
                    break;
            }
        }
    }
}

#endif
