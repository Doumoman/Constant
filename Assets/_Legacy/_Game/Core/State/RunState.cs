#if LEGACY_DISABLED
using System;
using System.Collections.Generic;

namespace StarNight.Core.State
{
    public enum RunPhase
    {
        None,
        Running,
        Failed,
        ClearedNormal,
        ClearedMemory,
        ClearedChallenge,
    }

    [Serializable]
    public sealed class ActionRecord
    {
        public string actionId;
        public string stageId;
        public float elapsedTime;
    }

    [Serializable]
    public sealed class RunState
    {
        public int seed;
        public RunPhase phase;
        public string currentStageId;
        public string selectedRoute;
        public float runTime;
        public int health;
        public int moneyWon;
        public int peakMoney;
        public int ropes;
        public int bombs;
        public string handToolId;
        public HashSet<string> items;
        public HashSet<string> flags;
        public HashSet<string> visitedStages;
        public List<ActionRecord> actionRecords;
        public bool lanternAvailable;
        public int stageRestartCount;
        public string failureReason;
        public string endingId;

        public static RunState CreateNew(int seedValue)
        {
            return new RunState
            {
                seed = seedValue,
                phase = RunPhase.Running,
                currentStageId = "0-1",
                selectedRoute = string.Empty,
                runTime = 0f,
                health = 4,
                moneyWon = 0,
                peakMoney = 0,
                ropes = 4,
                bombs = 4,
                handToolId = string.Empty,
                items = new HashSet<string>(StringComparer.Ordinal),
                flags = new HashSet<string>(StringComparer.Ordinal),
                visitedStages = new HashSet<string>(StringComparer.Ordinal),
                actionRecords = new List<ActionRecord>(),
                lanternAvailable = true,
                stageRestartCount = 0,
                failureReason = string.Empty,
                endingId = string.Empty,
            };
        }
    }
}

#endif
