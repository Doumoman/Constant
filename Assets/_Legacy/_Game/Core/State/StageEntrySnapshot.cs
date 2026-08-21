#if LEGACY_DISABLED
using System;
using System.Collections.Generic;

namespace StarNight.Core.State
{
    [Serializable]
    public sealed class StageEntrySnapshot
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
        public bool lanternAvailable;
        public HashSet<string> items;
        public HashSet<string> flags;
        public HashSet<string> visitedStages;
        public List<ActionRecord> actionRecords;
        public string failureReason;
        public string endingId;

        public static StageEntrySnapshot Capture(RunState run)
        {
            if (run == null)
            {
                return null;
            }

            return new StageEntrySnapshot
            {
                seed = run.seed,
                phase = run.phase,
                currentStageId = run.currentStageId,
                selectedRoute = run.selectedRoute,
                runTime = run.runTime,
                health = run.health,
                moneyWon = run.moneyWon,
                peakMoney = run.peakMoney,
                ropes = run.ropes,
                bombs = run.bombs,
                handToolId = run.handToolId,
                lanternAvailable = run.lanternAvailable,
                items = CopySet(run.items),
                flags = CopySet(run.flags),
                visitedStages = CopySet(run.visitedStages),
                actionRecords = CopyRecords(run.actionRecords),
                failureReason = run.failureReason,
                endingId = run.endingId,
            };
        }

        public bool RestoreInto(RunState run)
        {
            if (run == null)
            {
                return false;
            }

            run.seed = seed;
            run.phase = phase;
            run.currentStageId = currentStageId;
            run.selectedRoute = selectedRoute;
            run.runTime = runTime;
            run.health = health;
            run.moneyWon = moneyWon;
            run.peakMoney = peakMoney;
            run.ropes = ropes;
            run.bombs = bombs;
            run.handToolId = handToolId;
            run.lanternAvailable = lanternAvailable;
            run.items = CopySet(items);
            run.flags = CopySet(flags);
            run.visitedStages = CopySet(visitedStages);
            run.actionRecords = CopyRecords(actionRecords);
            run.failureReason = failureReason ?? string.Empty;
            run.endingId = endingId ?? string.Empty;
            return true;
        }

        private static HashSet<string> CopySet(HashSet<string> source)
        {
            return source == null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(source, StringComparer.Ordinal);
        }

        private static List<ActionRecord> CopyRecords(List<ActionRecord> source)
        {
            var result = new List<ActionRecord>();
            if (source == null)
            {
                return result;
            }

            foreach (ActionRecord record in source)
            {
                result.Add(new ActionRecord
                {
                    actionId = record.actionId,
                    stageId = record.stageId,
                    elapsedTime = record.elapsedTime,
                });
            }
            return result;
        }
    }
}

#endif
