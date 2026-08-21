#if LEGACY_DISABLED
using System;

namespace StarNight.Core.State
{
    [Serializable]
    public sealed class RunResultSnapshot
    {
        public const string HelpedEventFlagPrefix = "EVENT.HELPED.";
        public const string MemoryTravelerFlagPrefix = "MEM.TRAVELER.";

        public RunPhase phase;
        public string failureReason;
        public string reachedStageId;
        public float runTime;
        public int peakMoney;
        public int helpedEventCount;
        public int memoryTravelerCount;
        public string endingId;

        public bool IsCleared => RunManager.IsClearedPhase(phase);

        public static RunResultSnapshot Capture(RunState run)
        {
            if (run == null)
            {
                return null;
            }

            return new RunResultSnapshot
            {
                phase = run.phase,
                failureReason = run.failureReason ?? string.Empty,
                reachedStageId = run.currentStageId ?? string.Empty,
                runTime = Math.Max(0f, run.runTime),
                peakMoney = Math.Max(run.peakMoney, run.moneyWon),
                helpedEventCount = CountFlags(run, HelpedEventFlagPrefix),
                memoryTravelerCount = CountFlags(run, MemoryTravelerFlagPrefix),
                endingId = run.endingId ?? string.Empty,
            };
        }

        private static int CountFlags(RunState run, string prefix)
        {
            if (run.flags == null)
            {
                return 0;
            }

            int count = 0;
            foreach (string flag in run.flags)
            {
                if (!string.IsNullOrEmpty(flag) && flag.StartsWith(prefix, StringComparison.Ordinal))
                {
                    count++;
                }
            }
            return count;
        }
    }
}

#endif
