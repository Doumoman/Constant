#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Campaign.P11;
using StarNight.Folklore.P9;
using UnityEngine;

namespace StarNight.Campaign.P12
{
    [DefaultExecutionOrder(-890)]
    [DisallowMultipleComponent]
    public sealed class P12ChallengeDirector2D : MonoBehaviour
    {
        [SerializeField] private P12ChallengeCatalog catalog;
        [SerializeField] private P11CampaignDirector2D p11Director;
        [SerializeField] private P11StoryState2D storyState;
        [SerializeField] private P12ChallengeTelemetry2D telemetry;
        [SerializeField] private P12ChallengePhase phase =
            P12ChallengePhase.WaitingForEntry;
        [SerializeField] private P12StageId currentStage;
        [SerializeField] private List<P12StageId> completedStages =
            new List<P12StageId>();
        [SerializeField] private int failureCount;
        [SerializeField] private int lanternChargesInSegment;
        [SerializeField] private int smallBellCharges;
        [SerializeField] private P12ChallengeOutcome outcome;

        public event Action ChallengeEntered;
        public event Action<P12StageId> StageEntered;
        public event Action<P12StageId> StageCompleted;
        public event Action<P12ChallengeSegment> SegmentCheckpointRestarted;
        public event Action ChallengeFailed;
        public event Action ChallengeCompleted;

        public P12ChallengeCatalog Catalog => catalog;
        public P11CampaignDirector2D P11Director => p11Director;
        public P11StoryState2D StoryState => storyState;
        public P12ChallengeTelemetry2D Telemetry => telemetry;
        public P12ChallengePhase Phase => phase;
        public P12StageId CurrentStage => currentStage;
        public IReadOnlyList<P12StageId> CompletedStages =>
            completedStages;
        public int FailureCount => failureCount;
        public int LanternChargesInSegment => lanternChargesInSegment;
        public int SmallBellCharges => smallBellCharges;
        public P12ChallengeOutcome Outcome => outcome;
        public bool ChallengeAccepted =>
            phase != P12ChallengePhase.WaitingForEntry;
        public bool HasEnded => phase == P12ChallengePhase.Ended;
        public bool IsCompleted =>
            phase == P12ChallengePhase.Completed;
        public bool AddsNewRules => false;
        public bool ChallengeFailurePreservesMainEndings => true;
        public bool EntryRequiresMemoryEnding => true;
        public bool PermanentRewardIsRecordOnly => true;

        public P12ChallengeSegment CurrentSegment
        {
            get
            {
                if (currentStage != P12StageId.None)
                {
                    return SegmentOf(currentStage);
                }

                switch (phase)
                {
                    case P12ChallengePhase.FirstSea:
                        return P12ChallengeSegment.FirstSea;
                    case P12ChallengePhase.SecondSea:
                        return P12ChallengeSegment.SecondSea;
                    case P12ChallengePhase.ThirdSea:
                        return P12ChallengeSegment.ThirdSea;
                    case P12ChallengePhase.DawnPassage:
                        return P12ChallengeSegment.DawnPassage;
                    default:
                        return P12ChallengeSegment.None;
                }
            }
        }

        public bool CanEnterChallenge
        {
            get
            {
                if (phase != P12ChallengePhase.WaitingForEntry
                    || p11Director == null
                    || storyState == null)
                {
                    return false;
                }

                P9FolkloreChainState2D folklore =
                    storyState.FolkloreState;
                return P12EntryRequirements.IsSatisfied(
                    folklore != null && folklore.HasRedWeaverThread,
                    folklore != null && folklore.HasDragonPalaceOrb,
                    storyState.HasDawnStarCoordinates,
                    p11Director.Ending == P11EndingKind.Memory);
            }
        }

        public bool CanConsumeLanternCharge =>
            ChallengeAccepted
            && !HasEnded
            && !IsCompleted
            && lanternChargesInSegment > 0;
        public bool CanConsumeSmallBell =>
            ChallengeAccepted
            && !HasEnded
            && !IsCompleted
            && smallBellCharges > 0;

        public void Configure(
            P12ChallengeCatalog challengeCatalog,
            P11CampaignDirector2D endingDirector,
            P11StoryState2D commonStoryState,
            P12ChallengeTelemetry2D challengeTelemetry)
        {
            catalog = challengeCatalog;
            p11Director = endingDirector;
            storyState = commonStoryState;
            telemetry = challengeTelemetry;
            ResetChallengeForTests(false);
        }

        public bool HasCompleted(P12StageId stageId)
        {
            return completedStages.Contains(stageId);
        }

        public bool TryAcceptP11Handoff()
        {
            if (!CanEnterChallenge)
            {
                return false;
            }

            AcceptChallenge();
            return true;
        }

        public bool CanEnterStage(P12StageId stageId)
        {
            if (!ChallengeAccepted
                || HasEnded
                || IsCompleted
                || catalog == null
                || catalog.Find(stageId) == null
                || currentStage != P12StageId.None
                || HasCompleted(stageId))
            {
                return false;
            }

            switch (stageId)
            {
                case P12StageId.StarlessSea01:
                    return completedStages.Count == 0
                        && phase == P12ChallengePhase.FirstSea;
                case P12StageId.StarlessSea02:
                    return HasCompleted(P12StageId.StarlessSea01);
                case P12StageId.StarlessSea03:
                    return HasCompleted(P12StageId.StarlessSea02);
                case P12StageId.StarlessSea04:
                    return HasCompleted(P12StageId.StarlessSea03)
                        && phase == P12ChallengePhase.SecondSea;
                case P12StageId.StarlessSea05:
                    return HasCompleted(P12StageId.StarlessSea04);
                case P12StageId.StarlessSea06:
                    return HasCompleted(P12StageId.StarlessSea05);
                case P12StageId.StarlessSea07:
                    return HasCompleted(P12StageId.StarlessSea06)
                        && phase == P12ChallengePhase.ThirdSea;
                case P12StageId.StarlessSea08:
                    return HasCompleted(P12StageId.StarlessSea07);
                case P12StageId.StarlessSea09:
                    return HasCompleted(P12StageId.StarlessSea08);
                case P12StageId.StarlessSea10:
                    return HasCompleted(P12StageId.StarlessSea09)
                        && phase == P12ChallengePhase.DawnPassage;
                case P12StageId.StarlessSea11:
                    return HasCompleted(P12StageId.StarlessSea10);
                case P12StageId.StarlessSea12:
                    return HasCompleted(P12StageId.StarlessSea11);
                default:
                    return false;
            }
        }

        public bool TryEnterStage(P12StageId stageId)
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
            if (currentStage == P12StageId.None)
            {
                return false;
            }

            P12StageId completed = currentStage;
            currentStage = P12StageId.None;
            AddCompleted(completed);
            ApplyCompletionTransition(completed);
            telemetry?.MarkStageCompleted(completed);
            StageCompleted?.Invoke(completed);
            if (phase == P12ChallengePhase.Completed)
            {
                ChallengeCompleted?.Invoke();
            }

            return true;
        }

        public bool TryFailCurrentStage(string cause = null)
        {
            if (currentStage == P12StageId.None
                || HasEnded
                || IsCompleted)
            {
                return false;
            }

            P12ChallengeSegment segment = SegmentOf(currentStage);
            currentStage = P12StageId.None;
            failureCount++;
            telemetry?.MarkFailure(cause);
            if (P12FailurePolicy.Evaluate(failureCount)
                == P12FailureConsequence.RestartSegment)
            {
                RemoveCompletedInSegment(segment);
                lanternChargesInSegment =
                    P12ChallengeDifficulty.LanternChargesPerSegment;
                SegmentCheckpointRestarted?.Invoke(segment);
            }
            else
            {
                outcome = P12ChallengeOutcome.EndedBySecondFailure;
                phase = P12ChallengePhase.Ended;
                ChallengeFailed?.Invoke();
            }

            return true;
        }

        public bool TryConsumeLanternCharge()
        {
            if (!CanConsumeLanternCharge)
            {
                return false;
            }

            lanternChargesInSegment--;
            return true;
        }

        public bool TryConsumeSmallBell()
        {
            if (!CanConsumeSmallBell)
            {
                return false;
            }

            smallBellCharges--;
            return true;
        }

        public void ResetChallengeForTests(bool entryReady = false)
        {
            completedStages.Clear();
            currentStage = P12StageId.None;
            failureCount = 0;
            lanternChargesInSegment = 0;
            smallBellCharges = 0;
            outcome = P12ChallengeOutcome.None;
            phase = P12ChallengePhase.WaitingForEntry;
            if (entryReady)
            {
                AcceptChallenge();
            }
        }

        public void AcceptEntryForTests()
        {
            if (!ChallengeAccepted)
            {
                AcceptChallenge();
            }
        }

        public static P12ChallengeSegment SegmentOf(P12StageId stageId)
        {
            if (stageId < P12StageId.StarlessSea01
                || stageId > P12StageId.StarlessSea12)
            {
                return P12ChallengeSegment.None;
            }

            int offset =
                (int)stageId - (int)P12StageId.StarlessSea01;
            return (P12ChallengeSegment)(offset / 3 + 1);
        }

        public static P12StageId FirstStageOf(
            P12ChallengeSegment segment)
        {
            switch (segment)
            {
                case P12ChallengeSegment.FirstSea:
                    return P12StageId.StarlessSea01;
                case P12ChallengeSegment.SecondSea:
                    return P12StageId.StarlessSea04;
                case P12ChallengeSegment.ThirdSea:
                    return P12StageId.StarlessSea07;
                case P12ChallengeSegment.DawnPassage:
                    return P12StageId.StarlessSea10;
                default:
                    return P12StageId.None;
            }
        }

        private void AcceptChallenge()
        {
            failureCount = 0;
            outcome = P12ChallengeOutcome.None;
            lanternChargesInSegment =
                P12ChallengeDifficulty.LanternChargesPerSegment;
            smallBellCharges =
                P12ChallengeDifficulty.SmallMaruBellCharges;
            phase = P12ChallengePhase.FirstSea;
            telemetry?.MarkChallengeEntered();
            ChallengeEntered?.Invoke();
        }

        private void AddCompleted(P12StageId stageId)
        {
            if (!completedStages.Contains(stageId))
            {
                completedStages.Add(stageId);
            }
        }

        private void RemoveCompletedInSegment(
            P12ChallengeSegment segment)
        {
            completedStages.RemoveAll(
                stageId => SegmentOf(stageId) == segment);
        }

        private void ApplyCompletionTransition(P12StageId completed)
        {
            switch (completed)
            {
                case P12StageId.StarlessSea03:
                    phase = P12ChallengePhase.SecondSea;
                    lanternChargesInSegment =
                        P12ChallengeDifficulty.LanternChargesPerSegment;
                    break;
                case P12StageId.StarlessSea06:
                    phase = P12ChallengePhase.ThirdSea;
                    lanternChargesInSegment =
                        P12ChallengeDifficulty.LanternChargesPerSegment;
                    break;
                case P12StageId.StarlessSea09:
                    phase = P12ChallengePhase.DawnPassage;
                    lanternChargesInSegment =
                        P12ChallengeDifficulty.LanternChargesPerSegment;
                    break;
                case P12StageId.StarlessSea12:
                    outcome = P12ChallengeOutcome.Completed;
                    phase = P12ChallengePhase.Completed;
                    break;
            }
        }
    }
}

#endif
