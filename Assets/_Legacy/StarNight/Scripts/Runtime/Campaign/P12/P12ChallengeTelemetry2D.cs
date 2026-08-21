#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Campaign.P12
{
    [Serializable]
    public struct P12SkilledRunRecord
    {
        [SerializeField] private string sessionId;
        [SerializeField] private bool completed;
        [SerializeField] private float seconds;
        [SerializeField] private bool assistUsed;
        [SerializeField] private bool unfairDeathReported;

        public P12SkilledRunRecord(
            string id,
            bool runCompleted,
            float runSeconds,
            bool runAssistUsed,
            bool runUnfairDeathReported)
        {
            sessionId = id ?? string.Empty;
            completed = runCompleted;
            seconds = Mathf.Max(0f, runSeconds);
            assistUsed = runAssistUsed;
            unfairDeathReported = runUnfairDeathReported;
        }

        public string SessionId => sessionId;
        public bool Completed => completed;
        public float Seconds => seconds;
        public bool AssistUsed => assistUsed;
        public bool UnfairDeathReported => unfairDeathReported;
    }

    [DisallowMultipleComponent]
    public sealed class P12ChallengeTelemetry2D : MonoBehaviour
    {
        public const float RequiredCompletionRateMin = 0.05f;
        public const float RequiredCompletionRateMax = 0.15f;
        public const int DefaultMinimumSkilledSamples = 20;
        public const float UnfairDeathRateMax = 0.10f;

        [SerializeField, Min(1)] private int minimumSkilledSamples =
            DefaultMinimumSkilledSamples;
        [SerializeField] private List<P12SkilledRunRecord> runs =
            new List<P12SkilledRunRecord>();
        [SerializeField] private int challengeEnteredCount;
        [SerializeField] private List<P12StageId> completedStages =
            new List<P12StageId>();
        [SerializeField] private List<string> failureCauses =
            new List<string>();

        public int MinimumSkilledSamples => minimumSkilledSamples;
        public int SampleCount => runs != null ? runs.Count : 0;
        public IReadOnlyList<P12SkilledRunRecord> Runs => runs;
        public bool HasMinimumSkilledSamples =>
            SampleCount >= minimumSkilledSamples;
        public int ChallengeEnteredCount => challengeEnteredCount;
        public IReadOnlyList<P12StageId> CompletedStages =>
            completedStages;
        public IReadOnlyList<string> FailureCauses => failureCauses;
        public int CompletedRunCount => CountRuns(completedOnly: true);
        public int AssistedRunCount => CountAssistedRuns();

        public float CompletionRate => SampleCount > 0
            ? CountRuns(completedOnly: true) / (float)SampleCount
            : 0f;
        public float UnfairDeathRate => SampleCount > 0
            ? CountUnfairDeaths() / (float)SampleCount
            : 0f;

        public bool CompletionRateGateInBand =>
            HasMinimumSkilledSamples
            && CompletionRate >= RequiredCompletionRateMin
            && CompletionRate <= RequiredCompletionRateMax;
        public bool UnfairDeathGatePassed =>
            HasMinimumSkilledSamples
            && UnfairDeathRate < UnfairDeathRateMax;
        public bool HumanGatePending =>
            !(CompletionRateGateInBand && UnfairDeathGatePassed);
        public bool InstrumentationReady => minimumSkilledSamples > 0;

        // Synthetic cohorts are intentionally unsupported. A seeded
        // proxy can test rate arithmetic, but cannot prove skilled
        // completion in the 5-15% band or a fair-death experience,
        // and must never close a required human gate.
        public bool AutomatedStructureCanSatisfyHumanGate => false;

        public void ConfigureMinimumSkilledSamples(int minimumSamples)
        {
            minimumSkilledSamples = Mathf.Max(1, minimumSamples);
        }

        public bool TryRecordSkilledRun(
            string sessionId,
            bool completed,
            float seconds,
            bool assistUsed,
            bool unfairDeathReported)
        {
            string normalizedId = NormalizeSessionId(sessionId);
            if (normalizedId.Length == 0
                || HasRecordedRun(normalizedId))
            {
                return false;
            }

            EnsureLists();
            runs.Add(
                new P12SkilledRunRecord(
                    normalizedId,
                    completed,
                    seconds,
                    assistUsed,
                    unfairDeathReported));
            return true;
        }

        public bool HasRecordedRun(string sessionId)
        {
            string normalizedId = NormalizeSessionId(sessionId);
            if (normalizedId.Length == 0 || runs == null)
            {
                return false;
            }

            for (int index = 0; index < runs.Count; index++)
            {
                if (string.Equals(
                        runs[index].SessionId,
                        normalizedId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public void MarkChallengeEntered()
        {
            challengeEnteredCount++;
        }

        public bool MarkStageCompleted(P12StageId stageId)
        {
            if (stageId == P12StageId.None)
            {
                return false;
            }

            EnsureLists();
            completedStages.Add(stageId);
            return true;
        }

        public bool MarkFailure(string cause)
        {
            if (string.IsNullOrWhiteSpace(cause))
            {
                return false;
            }

            EnsureLists();
            failureCauses.Add(cause.Trim());
            return true;
        }

        public bool CanAutomatedStructureSatisfyHumanGate(
            bool automatedStructureValidationPassed)
        {
            _ = automatedStructureValidationPassed;
            return false;
        }

        public void ResetForTests(
            int minimumSamples = DefaultMinimumSkilledSamples)
        {
            minimumSkilledSamples = Mathf.Max(1, minimumSamples);
            EnsureLists();
            runs.Clear();
            completedStages.Clear();
            failureCauses.Clear();
            challengeEnteredCount = 0;
        }

        private int CountRuns(bool completedOnly)
        {
            if (runs == null)
            {
                return 0;
            }

            int total = 0;
            for (int index = 0; index < runs.Count; index++)
            {
                if (!completedOnly || runs[index].Completed)
                {
                    total++;
                }
            }

            return total;
        }

        private int CountAssistedRuns()
        {
            if (runs == null)
            {
                return 0;
            }

            int total = 0;
            for (int index = 0; index < runs.Count; index++)
            {
                if (runs[index].AssistUsed)
                {
                    total++;
                }
            }

            return total;
        }

        private int CountUnfairDeaths()
        {
            if (runs == null)
            {
                return 0;
            }

            int total = 0;
            for (int index = 0; index < runs.Count; index++)
            {
                if (runs[index].UnfairDeathReported)
                {
                    total++;
                }
            }

            return total;
        }

        private void EnsureLists()
        {
            if (runs == null)
            {
                runs = new List<P12SkilledRunRecord>();
            }

            if (completedStages == null)
            {
                completedStages = new List<P12StageId>();
            }

            if (failureCauses == null)
            {
                failureCauses = new List<string>();
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
            minimumSkilledSamples = Mathf.Max(
                1,
                minimumSkilledSamples);
            EnsureLists();
        }
#endif
    }
}

#endif
