#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarNight.Maru.P8
{
    public readonly struct P8MaruSessionSnapshot
    {
        public P8MaruSessionSnapshot(
            bool normalCohort,
            float stageClearedAt,
            float maruAppearedAt,
            float statueDestroyedAt,
            float runEndedAt,
            P8RunEndKind actualCause,
            bool hasHumanCauseResponse,
            P8RunEndKind reportedCause)
        {
            NormalCohort = normalCohort;
            StageClearedAt = stageClearedAt;
            MaruAppearedAt = maruAppearedAt;
            StatueDestroyedAt = statueDestroyedAt;
            RunEndedAt = runEndedAt;
            ActualCause = actualCause;
            HasHumanCauseResponse = hasHumanCauseResponse;
            ReportedCause = reportedCause;
        }

        public bool NormalCohort { get; }
        public float StageClearedAt { get; }
        public float MaruAppearedAt { get; }
        public float StatueDestroyedAt { get; }
        public float RunEndedAt { get; }
        public P8RunEndKind ActualCause { get; }
        public bool HasHumanCauseResponse { get; }
        public P8RunEndKind ReportedCause { get; }
        public bool WasCleared => StageClearedAt >= 0f;
        public bool MaruAppearedBeforeClear =>
            WasCleared
            && MaruAppearedAt >= 0f
            && MaruAppearedAt < StageClearedAt;
        public bool StatueWasDestroyed => StatueDestroyedAt >= 0f;
        public bool SurvivedAfterStatue =>
            StatueWasDestroyed
            && WasCleared
            && (RunEndedAt < 0f || StageClearedAt < RunEndedAt);
        public bool CauseResponseCorrect =>
            HasHumanCauseResponse
            && ActualCause != P8RunEndKind.None
            && ReportedCause == ActualCause;
    }

    public readonly struct P8MaruCohortSummary
    {
        public P8MaruCohortSummary(
            int appearanceEligible,
            int appearanceCount,
            int statueEligible,
            int statueSurvivals,
            int causeEligible,
            int causeCorrect)
        {
            AppearanceEligible = appearanceEligible;
            AppearanceCount = appearanceCount;
            StatueEligible = statueEligible;
            StatueSurvivals = statueSurvivals;
            CauseEligible = causeEligible;
            CauseCorrect = causeCorrect;
        }

        public int AppearanceEligible { get; }
        public int AppearanceCount { get; }
        public int StatueEligible { get; }
        public int StatueSurvivals { get; }
        public int CauseEligible { get; }
        public int CauseCorrect { get; }
        public float AppearanceRate => AppearanceEligible > 0
            ? (float)AppearanceCount / AppearanceEligible
            : 0f;
        public float StatueSurvivalRate => StatueEligible > 0
            ? (float)StatueSurvivals / StatueEligible
            : 0f;
        public float CauseUnderstandingRate => CauseEligible > 0
            ? (float)CauseCorrect / CauseEligible
            : 0f;
        public bool AppearanceGatePassed =>
            AppearanceEligible > 0
            && AppearanceRate >= 0.15f
            && AppearanceRate <= 0.30f;
        public bool StatueGatePassed =>
            StatueEligible > 0
            && StatueSurvivalRate >= 0.40f
            && StatueSurvivalRate <= 0.60f;
        public bool CauseGatePassed =>
            CauseEligible > 0
            && CauseUnderstandingRate >= 0.90f;
        public bool Passed =>
            AppearanceGatePassed
            && StatueGatePassed
            && CauseGatePassed;
    }

    public static class P8MaruCohortEvaluator
    {
        public static P8MaruCohortSummary Evaluate(
            IEnumerable<P8MaruSessionSnapshot> sessions)
        {
            P8MaruSessionSnapshot[] values =
                sessions?.ToArray()
                ?? Array.Empty<P8MaruSessionSnapshot>();
            P8MaruSessionSnapshot[] appearance = values
                .Where(value => value.NormalCohort && value.WasCleared)
                .ToArray();
            P8MaruSessionSnapshot[] statue = values
                .Where(value => value.StatueWasDestroyed)
                .ToArray();
            P8MaruSessionSnapshot[] cause = values
                .Where(value =>
                    value.ActualCause != P8RunEndKind.None
                    && value.HasHumanCauseResponse)
                .ToArray();
            return new P8MaruCohortSummary(
                appearance.Length,
                appearance.Count(value => value.MaruAppearedBeforeClear),
                statue.Length,
                statue.Count(value => value.SurvivedAfterStatue),
                cause.Length,
                cause.Count(value => value.CauseResponseCorrect));
        }
    }

    [DisallowMultipleComponent]
    public sealed class P8MaruTelemetry2D : MonoBehaviour
    {
        [SerializeField] private P8MaruTimeline2D timeline;
        [SerializeField] private P8HomecomingStatue2D statue;
        [SerializeField] private bool normalCohort = true;

        public float StageStartedAt { get; private set; } = -1f;
        public float StageClearedAt { get; private set; } = -1f;
        public float MaruAppearedAt { get; private set; } = -1f;
        public float StatueDestroyedAt { get; private set; } = -1f;
        public float RunEndedAt { get; private set; } = -1f;
        public P8DeathCauseReport DeathReport { get; private set; }
        public bool HasHumanCauseResponse { get; private set; }
        public P8RunEndKind ReportedCause { get; private set; }

        public void Configure(
            P8MaruTimeline2D maruTimeline,
            P8HomecomingStatue2D homecomingStatue,
            bool isNormalCohort = true)
        {
            UnsubscribeStatue();
            timeline = maruTimeline;
            statue = homecomingStatue;
            normalCohort = isNormalCohort;
            SubscribeStatue();
            ResetSession();
        }

        private void OnEnable()
        {
            SubscribeStatue();
        }

        private void OnDisable()
        {
            UnsubscribeStatue();
        }

        public void MarkStageStarted()
        {
            if (StageStartedAt < 0f)
            {
                StageStartedAt = CurrentStageSeconds;
            }
        }

        public void MarkStageCleared()
        {
            if (StageClearedAt < 0f)
            {
                StageClearedAt = CurrentStageSeconds;
            }
        }

        public void MarkMaruAppeared()
        {
            if (MaruAppearedAt < 0f)
            {
                MaruAppearedAt = CurrentStageSeconds;
            }
        }

        public void MarkRunEnded(P8DeathCauseReport report)
        {
            if (RunEndedAt >= 0f)
            {
                return;
            }

            RunEndedAt = CurrentStageSeconds;
            DeathReport = report;
        }

        public void RecordDeathCauseHumanResponse(
            P8RunEndKind reportedCause)
        {
            HasHumanCauseResponse = true;
            ReportedCause = reportedCause;
        }

        public P8MaruSessionSnapshot Capture()
        {
            return new P8MaruSessionSnapshot(
                normalCohort,
                StageClearedAt,
                MaruAppearedAt,
                StatueDestroyedAt,
                RunEndedAt,
                DeathReport != null
                    ? DeathReport.RunEndKind
                    : P8RunEndKind.None,
                HasHumanCauseResponse,
                ReportedCause);
        }

        public void ResetSession()
        {
            StageStartedAt = -1f;
            StageClearedAt = -1f;
            MaruAppearedAt = -1f;
            StatueDestroyedAt = -1f;
            RunEndedAt = -1f;
            DeathReport = null;
            HasHumanCauseResponse = false;
            ReportedCause = P8RunEndKind.None;
        }

        private float CurrentStageSeconds =>
            timeline != null ? timeline.ElapsedSeconds : Time.time;

        private void HandleStatueDestroyed(P8StatueImpactKind impact)
        {
            if (StatueDestroyedAt < 0f)
            {
                StatueDestroyedAt = CurrentStageSeconds;
            }
        }

        private void SubscribeStatue()
        {
            if (statue != null)
            {
                statue.Destroyed -= HandleStatueDestroyed;
                statue.Destroyed += HandleStatueDestroyed;
            }
        }

        private void UnsubscribeStatue()
        {
            if (statue != null)
            {
                statue.Destroyed -= HandleStatueDestroyed;
            }
        }
    }
}

#endif
