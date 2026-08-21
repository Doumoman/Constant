#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    public enum P11HumanComprehensionGate
    {
        LetterUseInference = 0,
        SunEmberUseInference = 1,
        CoreTragedyUnderstanding = 2,
        MaruBasicsUnderstanding = 3
    }

    [Serializable]
    public struct P11ComprehensionSessionRecord
    {
        [SerializeField] private string sessionId;
        [SerializeField] private bool letterUseInferred;
        [SerializeField] private bool sunEmberUseInferred;
        [SerializeField] private bool coreTragedyUnderstood;
        [SerializeField] private bool maruBasicsUnderstood;

        public P11ComprehensionSessionRecord(
            string id,
            bool inferredLetterUse,
            bool inferredSunEmberUse,
            bool understoodCoreTragedy,
            bool understoodMaruBasics)
        {
            sessionId = id ?? string.Empty;
            letterUseInferred = inferredLetterUse;
            sunEmberUseInferred = inferredSunEmberUse;
            coreTragedyUnderstood = understoodCoreTragedy;
            maruBasicsUnderstood = understoodMaruBasics;
        }

        public string SessionId => sessionId;
        public bool LetterUseInferred => letterUseInferred;
        public bool SunEmberUseInferred => sunEmberUseInferred;
        public bool CoreTragedyUnderstood => coreTragedyUnderstood;
        public bool MaruBasicsUnderstood => maruBasicsUnderstood;
    }

    [DisallowMultipleComponent]
    public sealed class P11ComprehensionTelemetry2D : MonoBehaviour
    {
        public const float RequiredHumanComprehensionRate = 0.80f;
        public const int DefaultMinimumHumanSamples = 5;

        [SerializeField, Min(1)] private int minimumHumanSamples =
            DefaultMinimumHumanSamples;
        [SerializeField] private List<P11ComprehensionSessionRecord>
            sessions = new List<P11ComprehensionSessionRecord>();

        public int MinimumHumanSamples => minimumHumanSamples;
        public int SampleCount => sessions != null ? sessions.Count : 0;
        public IReadOnlyList<P11ComprehensionSessionRecord> Sessions =>
            sessions;
        public bool HasMinimumHumanSamples =>
            SampleCount >= minimumHumanSamples;

        public float LetterUseInferenceRate =>
            Rate(P11HumanComprehensionGate.LetterUseInference);
        public float SunEmberUseInferenceRate =>
            Rate(P11HumanComprehensionGate.SunEmberUseInference);
        public float CoreTragedyUnderstandingRate =>
            Rate(P11HumanComprehensionGate.CoreTragedyUnderstanding);
        public float MaruBasicsUnderstandingRate =>
            Rate(P11HumanComprehensionGate.MaruBasicsUnderstanding);

        public bool LetterUseGatePassed =>
            HasMinimumHumanSamples
            && LetterUseInferenceRate
                >= RequiredHumanComprehensionRate;
        public bool SunEmberUseGatePassed =>
            HasMinimumHumanSamples
            && SunEmberUseInferenceRate
                >= RequiredHumanComprehensionRate;
        public bool CoreTragedyGatePassed =>
            HasMinimumHumanSamples
            && CoreTragedyUnderstandingRate
                >= RequiredHumanComprehensionRate;
        public bool MaruBasicsGatePassed =>
            HasMinimumHumanSamples
            && MaruBasicsUnderstandingRate
                >= RequiredHumanComprehensionRate;
        public bool AllHumanGatesPassed =>
            LetterUseGatePassed
            && SunEmberUseGatePassed
            && CoreTragedyGatePassed
            && MaruBasicsGatePassed;
        public bool HumanGatePending => !AllHumanGatesPassed;
        public bool InstrumentationReady => minimumHumanSamples > 0;

        // Synthetic cohorts are intentionally unsupported. A seeded proxy can
        // test rate arithmetic, but cannot prove that a real player understood
        // an affordance or story and must never close a required human gate.
        public bool AutomatedStructureCanSatisfyHumanGate => false;

        public void ConfigureMinimumHumanSamples(int minimumSamples)
        {
            minimumHumanSamples = Mathf.Max(1, minimumSamples);
        }

        public bool TryRecordSession(
            string sessionId,
            bool letterUseInferred,
            bool sunEmberUseInferred,
            bool coreTragedyUnderstood,
            bool maruBasicsUnderstood)
        {
            string normalizedId = NormalizeSessionId(sessionId);
            if (normalizedId.Length == 0
                || HasRecordedSession(normalizedId))
            {
                return false;
            }

            EnsureSessions();
            sessions.Add(
                new P11ComprehensionSessionRecord(
                    normalizedId,
                    letterUseInferred,
                    sunEmberUseInferred,
                    coreTragedyUnderstood,
                    maruBasicsUnderstood));
            return true;
        }

        public bool HasRecordedSession(string sessionId)
        {
            string normalizedId = NormalizeSessionId(sessionId);
            if (normalizedId.Length == 0 || sessions == null)
            {
                return false;
            }

            for (int index = 0; index < sessions.Count; index++)
            {
                if (string.Equals(
                        sessions[index].SessionId,
                        normalizedId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsHumanGatePassed(
            P11HumanComprehensionGate gate)
        {
            switch (gate)
            {
                case P11HumanComprehensionGate.LetterUseInference:
                    return LetterUseGatePassed;
                case P11HumanComprehensionGate.SunEmberUseInference:
                    return SunEmberUseGatePassed;
                case P11HumanComprehensionGate
                    .CoreTragedyUnderstanding:
                    return CoreTragedyGatePassed;
                case P11HumanComprehensionGate
                    .MaruBasicsUnderstanding:
                    return MaruBasicsGatePassed;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(gate),
                        gate,
                        "Unknown P11 human comprehension gate.");
            }
        }

        public bool CanAutomatedStructureSatisfyHumanGate(
            bool automatedStructureValidationPassed)
        {
            _ = automatedStructureValidationPassed;
            return false;
        }

        public void ResetForTests(
            int minimumSamples = DefaultMinimumHumanSamples)
        {
            minimumHumanSamples = Mathf.Max(1, minimumSamples);
            EnsureSessions();
            sessions.Clear();
        }

        private float Rate(P11HumanComprehensionGate gate)
        {
            if (sessions == null || sessions.Count == 0)
            {
                return 0f;
            }

            int successes = 0;
            for (int index = 0; index < sessions.Count; index++)
            {
                if (SessionPassedGate(sessions[index], gate))
                {
                    successes++;
                }
            }

            return successes / (float)sessions.Count;
        }

        private static bool SessionPassedGate(
            P11ComprehensionSessionRecord record,
            P11HumanComprehensionGate gate)
        {
            switch (gate)
            {
                case P11HumanComprehensionGate.LetterUseInference:
                    return record.LetterUseInferred;
                case P11HumanComprehensionGate.SunEmberUseInference:
                    return record.SunEmberUseInferred;
                case P11HumanComprehensionGate
                    .CoreTragedyUnderstanding:
                    return record.CoreTragedyUnderstood;
                case P11HumanComprehensionGate
                    .MaruBasicsUnderstanding:
                    return record.MaruBasicsUnderstood;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(gate),
                        gate,
                        "Unknown P11 human comprehension gate.");
            }
        }

        private void EnsureSessions()
        {
            if (sessions == null)
            {
                sessions =
                    new List<P11ComprehensionSessionRecord>();
            }
        }

        private static string NormalizeSessionId(string sessionId)
        {
            return string.IsNullOrWhiteSpace(sessionId)
                ? string.Empty
                : sessionId.Trim();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            minimumHumanSamples = Mathf.Max(
                1,
                minimumHumanSamples);
            EnsureSessions();
        }
#endif
    }
}

#endif
