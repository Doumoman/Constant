#if LEGACY_DISABLED
using System;
using System.Collections.Generic;

namespace StarNight.Core.Save
{
    [Serializable]
    public sealed class RunRecordData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public List<string> viewedEndingIds = new();
        public List<string> metMemoryTravelerIds = new();
        public List<string> discoveredFolkloreIds = new();
        public string highestReachedStage = string.Empty;
        public float bestClearedRunTime;
        public int completedRunCount;
        public int failedRunCount;

        public int TotalRunCount => completedRunCount + failedRunCount;

        public static RunRecordData CreateDefault() => new();
    }
}

#endif
