#if LEGACY_DISABLED
using System;
using StarNight.Maru.P8;
using UnityEngine;

namespace StarNight.Campaign.P12
{
    public enum P12AssistOption
    {
        MaruDelay = 0,
        PostHitInvulnerability = 1,
        AlwaysShowExitMarker = 2,
        GrappleAimAssist = 3,
        BossSpeedReduced = 4
    }

    [DefaultExecutionOrder(-895)]
    [DisallowMultipleComponent]
    public sealed class P12AccessibilityOptions2D : MonoBehaviour
    {
        public const float MaruDelaySeconds =
            P8MaruTimelineProfile.TravellerAssistSeconds;
        public const float PostHitInvulnerabilityBonusSeconds = 0.5f;
        public const float BossAttackSpeedScale = 0.85f;
        public const float GrappleAimAssistScale = 1.35f;

        [SerializeField] private bool maruDelayEnabled;
        [SerializeField] private bool postHitInvulnerabilityEnabled;
        [SerializeField] private bool alwaysShowExitMarker;
        [SerializeField] private bool grappleAimAssist;
        [SerializeField] private bool bossSpeedReduced;
        [SerializeField] private bool loadFromStoreOnAwake = true;

        public event Action<P12AssistOption> OptionsChanged;

        public bool MaruDelayEnabled => maruDelayEnabled;
        public bool PostHitInvulnerabilityEnabled =>
            postHitInvulnerabilityEnabled;
        public bool AlwaysShowExitMarker => alwaysShowExitMarker;
        public bool GrappleAimAssist => grappleAimAssist;
        public bool BossSpeedReduced => bossSpeedReduced;
        public bool AnyAssistEnabled =>
            maruDelayEnabled
            || postHitInvulnerabilityEnabled
            || alwaysShowExitMarker
            || grappleAimAssist
            || bossSpeedReduced;

        public float EffectiveMaruDelaySeconds =>
            maruDelayEnabled ? MaruDelaySeconds : 0f;
        public float EffectivePostHitInvulnerabilityBonusSeconds =>
            postHitInvulnerabilityEnabled
                ? PostHitInvulnerabilityBonusSeconds
                : 0f;
        public bool ExitMarkerAlwaysVisible => alwaysShowExitMarker;
        public float EffectiveGrappleAimAssistScale =>
            grappleAimAssist ? GrappleAimAssistScale : 1f;
        public float EffectiveBossAttackSpeedScale =>
            bossSpeedReduced ? BossAttackSpeedScale : 1f;

        public bool AssistNeverBlocksEndings => true;
        public bool AssistOnlyMarkedOnChallengeRecord => true;

        private void Awake()
        {
            if (loadFromStoreOnAwake)
            {
                LoadFromStore();
            }
        }

        public bool GetOption(P12AssistOption option)
        {
            switch (option)
            {
                case P12AssistOption.MaruDelay:
                    return maruDelayEnabled;
                case P12AssistOption.PostHitInvulnerability:
                    return postHitInvulnerabilityEnabled;
                case P12AssistOption.AlwaysShowExitMarker:
                    return alwaysShowExitMarker;
                case P12AssistOption.GrappleAimAssist:
                    return grappleAimAssist;
                case P12AssistOption.BossSpeedReduced:
                    return bossSpeedReduced;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(option),
                        option,
                        "Unknown P12 assist option.");
            }
        }

        public bool SetOption(P12AssistOption option, bool enabled)
        {
            if (GetOption(option) == enabled)
            {
                return false;
            }

            switch (option)
            {
                case P12AssistOption.MaruDelay:
                    maruDelayEnabled = enabled;
                    break;
                case P12AssistOption.PostHitInvulnerability:
                    postHitInvulnerabilityEnabled = enabled;
                    break;
                case P12AssistOption.AlwaysShowExitMarker:
                    alwaysShowExitMarker = enabled;
                    break;
                case P12AssistOption.GrappleAimAssist:
                    grappleAimAssist = enabled;
                    break;
                case P12AssistOption.BossSpeedReduced:
                    bossSpeedReduced = enabled;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(option),
                        option,
                        "Unknown P12 assist option.");
            }

            OptionsChanged?.Invoke(option);
            return true;
        }

        public P12AccessibilityData CaptureData()
        {
            P12AccessibilityData data =
                P12AccessibilityData.CreateDefault();
            data.Configure(
                maruDelayEnabled,
                postHitInvulnerabilityEnabled,
                alwaysShowExitMarker,
                grappleAimAssist,
                bossSpeedReduced);
            return data;
        }

        public void ApplyData(P12AccessibilityData data)
        {
            if (data == null)
            {
                return;
            }

            SetOption(
                P12AssistOption.MaruDelay,
                data.MaruDelayEnabled);
            SetOption(
                P12AssistOption.PostHitInvulnerability,
                data.PostHitInvulnerabilityEnabled);
            SetOption(
                P12AssistOption.AlwaysShowExitMarker,
                data.AlwaysShowExitMarker);
            SetOption(
                P12AssistOption.GrappleAimAssist,
                data.GrappleAimAssist);
            SetOption(
                P12AssistOption.BossSpeedReduced,
                data.BossSpeedReduced);
        }

        public void LoadFromStore()
        {
            ApplyData(P12PersistentStore.LoadAccessibility());
        }

        public bool SaveToStore()
        {
            return P12PersistentStore.SaveAccessibility(CaptureData());
        }

        public void ResetForTests()
        {
            maruDelayEnabled = false;
            postHitInvulnerabilityEnabled = false;
            alwaysShowExitMarker = false;
            grappleAimAssist = false;
            bossSpeedReduced = false;
        }
    }
}

#endif
