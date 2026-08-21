#if LEGACY_DISABLED
using System;

namespace StarNight.Core.State
{
    public sealed class RunManager
    {
        public const string EndingFlagPrefix = "ENDING.";
        public const string NormalEndingFlag = EndingFlagPrefix + "NORMAL";
        public const string MemoryEndingFlag = EndingFlagPrefix + "MEMORY";
        public const string ChallengeEndingFlag = EndingFlagPrefix + "CHALLENGE";

        private readonly Func<int> seedFactory;

        public RunManager(Func<int> seedFactory = null)
        {
            this.seedFactory = seedFactory ?? CreateSeed;
        }

        public RunState Current { get; private set; }
        public bool HasActiveRun => Current != null && Current.phase == RunPhase.Running;

        public RunState StartNewRun()
        {
            Current = RunState.CreateNew(seedFactory());
            return Current;
        }

        public void AbandonRun()
        {
            Current = null;
        }

        public void TickRun(float unscaledDeltaTime)
        {
            if (!HasActiveRun)
            {
                return;
            }

            Current.runTime += Math.Max(0f, unscaledDeltaTime);
            UpdatePeakMoney();
        }

        public bool CompleteRun(RunPhase completionPhase, string endingId)
        {
            if (!HasActiveRun || !IsClearedPhase(completionPhase))
            {
                return false;
            }

            ClearEndingFlags(Current);
            Current.flags.Add(GetEndingFlag(completionPhase));
            Current.phase = completionPhase;
            Current.failureReason = string.Empty;
            Current.endingId = string.IsNullOrWhiteSpace(endingId)
                ? GetDefaultEndingId(completionPhase)
                : endingId.Trim();
            UpdatePeakMoney();
            return true;
        }

        public bool FailRun(string reason)
        {
            if (!HasActiveRun)
            {
                return false;
            }

            Current.phase = RunPhase.Failed;
            Current.failureReason = reason ?? string.Empty;
            Current.endingId = string.Empty;
            ClearEndingFlags(Current);
            UpdatePeakMoney();
            return true;
        }

        public static bool IsClearedPhase(RunPhase phase)
        {
            return phase == RunPhase.ClearedNormal ||
                   phase == RunPhase.ClearedMemory ||
                   phase == RunPhase.ClearedChallenge;
        }

        public static string GetEndingFlag(RunPhase phase)
        {
            return phase switch
            {
                RunPhase.ClearedMemory => MemoryEndingFlag,
                RunPhase.ClearedChallenge => ChallengeEndingFlag,
                _ => NormalEndingFlag,
            };
        }

        private void UpdatePeakMoney()
        {
            if (Current != null)
            {
                Current.peakMoney = Math.Max(Current.peakMoney, Current.moneyWon);
            }
        }

        private static void ClearEndingFlags(RunState run)
        {
            if (run?.flags == null)
            {
                return;
            }

            run.flags.RemoveWhere(flag =>
                !string.IsNullOrEmpty(flag) &&
                flag.StartsWith(EndingFlagPrefix, StringComparison.Ordinal));
        }

        private static string GetDefaultEndingId(RunPhase phase)
        {
            return phase switch
            {
                RunPhase.ClearedMemory => "memory",
                RunPhase.ClearedChallenge => "challenge",
                _ => "normal",
            };
        }

        private static int CreateSeed()
        {
            int seed = Guid.NewGuid().GetHashCode();
            return seed == 0 ? 1 : seed;
        }
    }
}

#endif
