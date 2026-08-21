#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Campaign.P12
{
    public enum P12ChallengeFailureCause
    {
        None = 0,
        MaruCaught = 1,
        HealthDepleted = 2,
        Fall = 3
    }

    [Serializable]
    public sealed class P12ChallengeRecordData
    {
        public const int CurrentSchemaVersion = 1;
        public const float NoCompletionSeconds = -1f;

        [SerializeField] private int schemaVersion = CurrentSchemaVersion;
        [SerializeField] private int attemptCount;
        [SerializeField] private P12ChallengeSegment bestSegmentReached =
            P12ChallengeSegment.None;
        [SerializeField] private P12StageId bestStageReached =
            P12StageId.None;
        [SerializeField] private int completionCount;
        [SerializeField] private float fastestCompletionSeconds =
            NoCompletionSeconds;
        [SerializeField] private int maruFailureCount;
        [SerializeField] private int healthFailureCount;
        [SerializeField] private int fallFailureCount;
        [SerializeField] private int assistedCompletionCount;

        public int SchemaVersion => schemaVersion;
        public int AttemptCount => attemptCount;
        public P12ChallengeSegment BestSegmentReached =>
            bestSegmentReached;
        public P12StageId BestStageReached => bestStageReached;
        public int CompletionCount => completionCount;
        public float FastestCompletionSeconds =>
            fastestCompletionSeconds;
        public bool HasCompletion => completionCount > 0;
        public int MaruFailureCount => maruFailureCount;
        public int HealthFailureCount => healthFailureCount;
        public int FallFailureCount => fallFailureCount;
        public int AssistedCompletionCount => assistedCompletionCount;
        public int UnassistedCompletionCount =>
            Mathf.Max(0, completionCount - assistedCompletionCount);
        public bool AssistUseOnlyAnnotatesRecord => true;

        public static P12ChallengeRecordData CreateEmpty()
        {
            return new P12ChallengeRecordData();
        }

        public void RegisterAttempt()
        {
            attemptCount++;
        }

        public bool RegisterProgress(
            P12ChallengeSegment segment,
            P12StageId stage)
        {
            bool changed = false;
            if (segment > bestSegmentReached)
            {
                bestSegmentReached = segment;
                changed = true;
            }

            if (stage > bestStageReached)
            {
                bestStageReached = stage;
                changed = true;
            }

            return changed;
        }

        public void RegisterCompletion(float seconds, bool assistUsed)
        {
            completionCount++;
            if (assistUsed)
            {
                assistedCompletionCount++;
            }

            bestSegmentReached = P12ChallengeSegment.DawnPassage;
            bestStageReached = P12StageId.StarlessSea12;
            if (seconds > 0f
                && (fastestCompletionSeconds <= 0f
                    || seconds < fastestCompletionSeconds))
            {
                fastestCompletionSeconds = seconds;
            }
        }

        public bool RegisterFailure(P12ChallengeFailureCause cause)
        {
            switch (cause)
            {
                case P12ChallengeFailureCause.MaruCaught:
                    maruFailureCount++;
                    return true;
                case P12ChallengeFailureCause.HealthDepleted:
                    healthFailureCount++;
                    return true;
                case P12ChallengeFailureCause.Fall:
                    fallFailureCount++;
                    return true;
                default:
                    return false;
            }
        }
    }

    [Serializable]
    public sealed class P12AccessibilityData
    {
        [SerializeField] private bool maruDelayEnabled;
        [SerializeField] private bool postHitInvulnerabilityEnabled;
        [SerializeField] private bool alwaysShowExitMarker;
        [SerializeField] private bool grappleAimAssist;
        [SerializeField] private bool bossSpeedReduced;

        public bool MaruDelayEnabled => maruDelayEnabled;
        public bool PostHitInvulnerabilityEnabled =>
            postHitInvulnerabilityEnabled;
        public bool AlwaysShowExitMarker => alwaysShowExitMarker;
        public bool GrappleAimAssist => grappleAimAssist;
        public bool BossSpeedReduced => bossSpeedReduced;
        public bool AnyEnabled =>
            maruDelayEnabled
            || postHitInvulnerabilityEnabled
            || alwaysShowExitMarker
            || grappleAimAssist
            || bossSpeedReduced;

        public static P12AccessibilityData CreateDefault()
        {
            return new P12AccessibilityData();
        }

        public void Configure(
            bool maruDelay,
            bool postHitInvulnerability,
            bool exitMarkerAlwaysShown,
            bool grappleAssist,
            bool bossSlowed)
        {
            maruDelayEnabled = maruDelay;
            postHitInvulnerabilityEnabled = postHitInvulnerability;
            alwaysShowExitMarker = exitMarkerAlwaysShown;
            grappleAimAssist = grappleAssist;
            bossSpeedReduced = bossSlowed;
        }
    }
}

#endif
