#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Tools;
using UnityEngine;

namespace StarNight.Debugging
{
    [DisallowMultipleComponent]
    public sealed class P3ToolDiscoveryTelemetry : MonoBehaviour
    {
        [Serializable]
        public sealed class ToolRecord
        {
            [SerializeField] private P3ToolKind kind;
            [SerializeField] private float firstSeenSeconds = -1f;
            [SerializeField] private float firstUseSeconds = -1f;
            [SerializeField] private float firstSuccessSeconds = -1f;

            public ToolRecord(P3ToolKind toolKind)
            {
                kind = toolKind;
            }

            public P3ToolKind Kind => kind;
            public float FirstSeenSeconds => firstSeenSeconds;
            public float FirstUseSeconds => firstUseSeconds;
            public float FirstSuccessSeconds => firstSuccessSeconds;
            public bool UnderThirtySecondSuccess =>
                firstSeenSeconds >= 0f
                && firstSuccessSeconds >= firstSeenSeconds
                && firstSuccessSeconds - firstSeenSeconds <= 30f;

            public void MarkSeen(float elapsed)
            {
                if (firstSeenSeconds < 0f)
                {
                    firstSeenSeconds = elapsed;
                }
            }

            public void MarkUse(float elapsed)
            {
                if (firstUseSeconds < 0f)
                {
                    firstUseSeconds = elapsed;
                }
            }

            public void MarkSuccess(float elapsed)
            {
                if (firstSuccessSeconds < 0f)
                {
                    firstSuccessSeconds = elapsed;
                }
            }
        }

        [SerializeField] private List<ToolRecord> records = new List<ToolRecord>();

        private readonly Dictionary<P3ToolKind, ToolRecord> byKind =
            new Dictionary<P3ToolKind, ToolRecord>();
        private float sessionStartedAt;

        public IReadOnlyList<ToolRecord> Records => records;

        private void Awake()
        {
            sessionStartedAt = Time.unscaledTime;
            RebuildIndex();
        }

        public void MarkSeen(P3ToolKind kind)
        {
            GetOrCreate(kind).MarkSeen(Elapsed);
        }

        public void MarkUse(P3ToolKind kind)
        {
            GetOrCreate(kind).MarkUse(Elapsed);
        }

        public void MarkSuccess(P3ToolKind kind)
        {
            GetOrCreate(kind).MarkSuccess(Elapsed);
        }

        public bool TryGetRecord(P3ToolKind kind, out ToolRecord record)
        {
            if (byKind.Count != records.Count)
            {
                RebuildIndex();
            }

            return byKind.TryGetValue(kind, out record);
        }

        public float CalculateThirtySecondSuccessRate()
        {
            int eligible = 0;
            int successes = 0;
            for (int index = 0; index < records.Count; index++)
            {
                ToolRecord record = records[index];
                if (record.FirstSeenSeconds < 0f)
                {
                    continue;
                }

                eligible++;
                if (record.UnderThirtySecondSuccess)
                {
                    successes++;
                }
            }

            return eligible > 0 ? (float)successes / eligible : 0f;
        }

        public P3ToolDiscoverySessionSnapshot CaptureSessionSnapshot(
            string participantId)
        {
            IReadOnlyList<P3ToolKind> order =
                P3ToolGardenContract.ToolOrder;
            P3ToolDiscoveryOutcome[] outcomes =
                new P3ToolDiscoveryOutcome[order.Count];
            for (int index = 0; index < order.Count; index++)
            {
                P3ToolKind kind = order[index];
                bool found = TryGetRecord(kind, out ToolRecord record);
                outcomes[index] = new P3ToolDiscoveryOutcome(
                    kind,
                    found && record.FirstSeenSeconds >= 0f,
                    found && record.UnderThirtySecondSuccess);
            }

            return new P3ToolDiscoverySessionSnapshot(
                participantId,
                outcomes);
        }

        private float Elapsed => Time.unscaledTime - sessionStartedAt;

        private ToolRecord GetOrCreate(P3ToolKind kind)
        {
            if (byKind.Count != records.Count)
            {
                RebuildIndex();
            }

            if (!byKind.TryGetValue(kind, out ToolRecord record))
            {
                record = new ToolRecord(kind);
                records.Add(record);
                byKind.Add(kind, record);
            }

            return record;
        }

        private void RebuildIndex()
        {
            byKind.Clear();
            for (int index = 0; index < records.Count; index++)
            {
                ToolRecord record = records[index];
                if (record != null && !byKind.ContainsKey(record.Kind))
                {
                    byKind.Add(record.Kind, record);
                }
            }
        }
    }
}

#endif
