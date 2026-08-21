#if LEGACY_DISABLED
using UnityEngine;
using UnityEngine.Profiling;

namespace StarNight.Integration
{
    [DisallowMultipleComponent]
    public sealed class GridLabSoakMonitor : MonoBehaviour
    {
        public const float RequiredDurationSeconds = 30f * 60f;
        public const float WarmupSeconds = 60f;
        public const long MaximumManagedGrowthBytes = 32L * 1024L * 1024L;

        private long baselineManagedBytes;
        private float nextSampleAt;

        public float ElapsedSeconds { get; private set; }
        public long ManagedGrowthBytes { get; private set; }
        public bool IsComplete => ElapsedSeconds >= RequiredDurationSeconds;
        public bool IsStable => ManagedGrowthBytes <= MaximumManagedGrowthBytes;

        private void OnEnable()
        {
            RestartObservation();
        }

        private void Update()
        {
            ElapsedSeconds += Time.unscaledDeltaTime;
            if (ElapsedSeconds < nextSampleAt)
            {
                return;
            }

            nextSampleAt = ElapsedSeconds + 10f;
            SampleNow();
        }

        public void RestartObservation()
        {
            ElapsedSeconds = 0f;
            ManagedGrowthBytes = 0L;
            baselineManagedBytes = 0L;
            nextSampleAt = 0f;
        }

        public void SampleNow()
        {
            long current = Profiler.GetMonoUsedSizeLong();
            if (baselineManagedBytes == 0L && ElapsedSeconds >= WarmupSeconds)
            {
                baselineManagedBytes = current;
            }
            if (baselineManagedBytes > 0L)
            {
                ManagedGrowthBytes = current - baselineManagedBytes;
            }
        }
    }
}

#endif
