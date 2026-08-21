#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Tools;

namespace StarNight.Debugging
{
    [Serializable]
    public readonly struct P3ToolDiscoveryOutcome
    {
        public P3ToolDiscoveryOutcome(
            P3ToolKind kind,
            bool wasSeen,
            bool succeededWithinThirtySeconds)
        {
            Kind = kind;
            WasSeen = wasSeen;
            SucceededWithinThirtySeconds =
                succeededWithinThirtySeconds;
        }

        public P3ToolKind Kind { get; }
        public bool WasSeen { get; }
        public bool SucceededWithinThirtySeconds { get; }
    }

    [Serializable]
    public sealed class P3ToolDiscoverySessionSnapshot
    {
        public P3ToolDiscoverySessionSnapshot(
            string participantId,
            IReadOnlyList<P3ToolDiscoveryOutcome> outcomes)
        {
            ParticipantId = participantId ?? string.Empty;
            Outcomes =
                outcomes ?? Array.Empty<P3ToolDiscoveryOutcome>();
        }

        public string ParticipantId { get; }
        public IReadOnlyList<P3ToolDiscoveryOutcome> Outcomes { get; }

        public bool TryGetOutcome(
            P3ToolKind kind,
            out P3ToolDiscoveryOutcome outcome)
        {
            for (int index = 0; index < Outcomes.Count; index++)
            {
                if (Outcomes[index].Kind == kind)
                {
                    outcome = Outcomes[index];
                    return true;
                }
            }

            outcome = default;
            return false;
        }
    }

    public static class P3ToolDiscoveryCohortEvaluator
    {
        public const float RequiredSuccessRate = 0.80f;

        public static IReadOnlyDictionary<P3ToolKind, float>
            CalculatePerToolParticipantRates(
                IReadOnlyList<P3ToolDiscoverySessionSnapshot> sessions)
        {
            Dictionary<P3ToolKind, float> rates =
                new Dictionary<P3ToolKind, float>();
            IReadOnlyList<P3ToolKind> order =
                P3ToolGardenContract.ToolOrder;
            int participantCount = sessions != null
                ? sessions.Count
                : 0;

            for (int toolIndex = 0;
                toolIndex < order.Count;
                toolIndex++)
            {
                P3ToolKind kind = order[toolIndex];
                int successCount = 0;
                for (int sessionIndex = 0;
                    sessionIndex < participantCount;
                    sessionIndex++)
                {
                    P3ToolDiscoverySessionSnapshot session =
                        sessions[sessionIndex];
                    if (session != null
                        && session.TryGetOutcome(kind, out var outcome)
                        && outcome.WasSeen
                        && outcome.SucceededWithinThirtySeconds)
                    {
                        successCount++;
                    }
                }

                rates[kind] = participantCount > 0
                    ? (float)successCount / participantCount
                    : 0f;
            }

            return rates;
        }

        public static bool MeetsEveryToolGate(
            IReadOnlyList<P3ToolDiscoverySessionSnapshot> sessions,
            float minimumRate = RequiredSuccessRate)
        {
            if (sessions == null || sessions.Count == 0)
            {
                return false;
            }

            float threshold = Math.Max(0f, Math.Min(1f, minimumRate));
            IReadOnlyDictionary<P3ToolKind, float> rates =
                CalculatePerToolParticipantRates(sessions);
            foreach (KeyValuePair<P3ToolKind, float> rate in rates)
            {
                if (rate.Value + 0.0001f < threshold)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

#endif
