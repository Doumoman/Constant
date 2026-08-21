#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Campaign.P12
{
    [DisallowMultipleComponent]
    public sealed class P12PerformanceProbe2D : MonoBehaviour
    {
        public const int FrameWindowSize = 240;
        public const float TargetFrameMilliseconds = 16.7f;
        public const float HardCapMilliseconds = 33.4f;
        public const int MaxActiveEnvironments = 1;
        public const int MaxLiveReturnCrystals = 8;

        [SerializeField] private bool captureFrames = true;

        private float[] samples = new float[FrameWindowSize];
        private int sampleCount;
        private int nextIndex;

        public bool CaptureFrames => captureFrames;
        public int SampleCount => sampleCount;

        public float AverageFrameMilliseconds
        {
            get
            {
                if (sampleCount == 0)
                {
                    return 0f;
                }

                float total = 0f;
                for (int index = 0; index < sampleCount; index++)
                {
                    total += samples[index];
                }

                return total / sampleCount;
            }
        }

        public float WorstFrameMilliseconds
        {
            get
            {
                float worst = 0f;
                for (int index = 0; index < sampleCount; index++)
                {
                    if (samples[index] > worst)
                    {
                        worst = samples[index];
                    }
                }

                return worst;
            }
        }

        public bool BudgetSatisfied =>
            sampleCount > 0
            && AverageFrameMilliseconds <= TargetFrameMilliseconds
            && WorstFrameMilliseconds <= HardCapMilliseconds;

        private void Update()
        {
            if (captureFrames)
            {
                RecordFrame(Time.unscaledDeltaTime * 1000f);
            }
        }

        public void MarkStageTransition()
        {
            ClearSamples();
        }

        public void ResetForTests()
        {
            captureFrames = true;
            ClearSamples();
        }

        public void RecordFrameForTests(float ms)
        {
            RecordFrame(ms);
        }

        private void RecordFrame(float ms)
        {
            if (ms < 0f)
            {
                return;
            }

            samples[nextIndex] = ms;
            nextIndex = (nextIndex + 1) % FrameWindowSize;
            if (sampleCount < FrameWindowSize)
            {
                sampleCount++;
            }
        }

        private void ClearSamples()
        {
            sampleCount = 0;
            nextIndex = 0;
        }
    }
}

#endif
