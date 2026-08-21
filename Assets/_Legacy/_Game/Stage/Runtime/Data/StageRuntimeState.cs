#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Core.State;
using StarNight.Stage.Rooms;

namespace StarNight.Stage.Data
{
    public enum StagePhase
    {
        Unloaded,
        Loading,
        Intro,
        Exploration,
        Bell1,
        Bell2,
        MaruChase,
        BossIntro,
        BossBattle,
        BossResolved,
        ExitCommitted,
        Complete,
    }

    public enum BellPhase
    {
        None,
        First,
        Second,
        Maru,
    }

    [Serializable]
    public sealed class StageRuntimeState
    {
        public StageDefinition definition;
        public StagePhase phase;
        public int seed;
        public float elapsedTime;
        public BellPhase bellPhase;
        public bool exitDiscovered;
        public bool bossResolved;
        public string currentRoomId;
        public string maruRoomId;
        public int maruBiteCount;
        public Dictionary<string, RoomPersistentState> rooms;
        public HashSet<string> visitedRoomIds;

        public static StageRuntimeState Create(StageDefinition stageDefinition, int runSeed, string startRoomId)
        {
            var state = new StageRuntimeState
            {
                definition = stageDefinition,
                phase = StagePhase.Loading,
                seed = runSeed,
                elapsedTime = 0f,
                bellPhase = BellPhase.None,
                exitDiscovered = false,
                bossResolved = false,
                currentRoomId = startRoomId ?? string.Empty,
                maruRoomId = string.Empty,
                maruBiteCount = 0,
                rooms = new Dictionary<string, RoomPersistentState>(StringComparer.Ordinal),
                visitedRoomIds = new HashSet<string>(StringComparer.Ordinal),
            };

            if (!string.IsNullOrWhiteSpace(startRoomId))
            {
                state.visitedRoomIds.Add(startRoomId);
            }

            return state;
        }
    }

    public static class StageConnectionEvaluator
    {
        public static bool IsAvailable(StageConnection connection, RunState run, StageRuntimeState stage)
        {
            if (connection == null || connection.target == null)
            {
                return false;
            }

            switch (connection.condition)
            {
                case ConnectionCondition.Always:
                    return true;
                case ConnectionCondition.RequiredFlag:
                    return run?.flags != null &&
                           !string.IsNullOrWhiteSpace(connection.requiredFlag) &&
                           run.flags.Contains(connection.requiredFlag);
                case ConnectionCondition.RequiredItem:
                    return run?.items != null &&
                           !string.IsNullOrWhiteSpace(connection.requiredItem) &&
                           run.items.Contains(connection.requiredItem);
                case ConnectionCondition.BossResolved:
                    return stage != null && stage.bossResolved;
                default:
                    return false;
            }
        }
    }
}

#endif
