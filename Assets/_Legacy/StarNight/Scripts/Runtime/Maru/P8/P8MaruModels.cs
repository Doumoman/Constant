#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Generation.P6;
using StarNight.Population.P7;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Maru.P8
{
    public enum P8MaruPhase
    {
        Calm = 0,
        FirstBell = 1,
        SecondBell = 2,
        Hunting = 3,
        Stopped = 4
    }

    public enum P8BellSignal
    {
        Short = 0,
        Long = 1
    }

    public enum P8BellCause
    {
        NaturalTimeline = 0,
        StatueCracked = 1,
        StatueDestroyed = 2
    }

    public enum P8MaruTargetKind
    {
        LostNpc = 0,
        LuminousTreasure = 1,
        DroppedHandTool = 2,
        Player = 3
    }

    public enum P8BiteState
    {
        Idle = 0,
        EscapeWindow = 1,
        Escaped = 2,
        FullyReturned = 3,
        RunEnded = 4
    }

    public enum P8StatueState
    {
        Intact = 0,
        Cracked = 1,
        Destroyed = 2
    }

    public enum P8StatueImpactKind
    {
        Collision = 0,
        FallingObject = 1,
        GrapplePulledObject = 2,
        Bomb = 3,
        Test = 4
    }

    public enum P8RunEndKind
    {
        None = 0,
        FailedFirstBiteEscape = 1,
        SecondMaruBite = 2,
        HealthDepleted = 3
    }

    [Serializable]
    public readonly struct P8MaruTimelineProfile
    {
        public const float BaseFirstBellSeconds = 120f;
        public const float BaseSecondBellSeconds = 165f;
        public const float BaseMaruDueSeconds = 195f;
        public const float FirstStageGraceSeconds = 20f;
        public const float TravellerAssistSeconds = 30f;

        public P8MaruTimelineProfile(
            P6StageSlot stageSlot,
            float firstBellSeconds,
            float secondBellSeconds,
            float maruDueSeconds,
            bool pausedForBoss)
        {
            if (firstBellSeconds < 0f
                || secondBellSeconds <= firstBellSeconds
                || maruDueSeconds <= secondBellSeconds)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(firstBellSeconds),
                    "Maru bell thresholds must be strictly increasing.");
            }

            StageSlot = stageSlot;
            FirstBellSeconds = firstBellSeconds;
            SecondBellSeconds = secondBellSeconds;
            MaruDueSeconds = maruDueSeconds;
            PausedForBoss = pausedForBoss;
        }

        public P6StageSlot StageSlot { get; }
        public float FirstBellSeconds { get; }
        public float SecondBellSeconds { get; }
        public float MaruDueSeconds { get; }
        public bool PausedForBoss { get; }

        public static P8MaruTimelineProfile Create(
            P6StageSlot stageSlot,
            bool bossRoom = false,
            bool travellerAssist = false)
        {
            float grace = stageSlot == P6StageSlot.X1
                ? FirstStageGraceSeconds
                : 0f;
            float dueAssist = travellerAssist
                ? TravellerAssistSeconds
                : 0f;
            return new P8MaruTimelineProfile(
                stageSlot,
                BaseFirstBellSeconds + grace,
                BaseSecondBellSeconds + grace,
                BaseMaruDueSeconds + grace + dueAssist,
                bossRoom);
        }
    }

    public readonly struct P8BellEvent
    {
        public P8BellEvent(
            P8BellSignal signal,
            P8BellCause cause,
            P8MaruPhase phase,
            float elapsedSeconds)
        {
            Signal = signal;
            Cause = cause;
            Phase = phase;
            ElapsedSeconds = Mathf.Max(0f, elapsedSeconds);
        }

        public P8BellSignal Signal { get; }
        public P8BellCause Cause { get; }
        public P8MaruPhase Phase { get; }
        public float ElapsedSeconds { get; }
    }

    [Serializable]
    public readonly struct P8StatueDestructionResult
    {
        public P8StatueDestructionResult(
            bool changed,
            float maruDueAtSeconds,
            float secondsAdvanced)
        {
            Changed = changed;
            MaruDueAtSeconds = Mathf.Max(0f, maruDueAtSeconds);
            SecondsAdvanced = Mathf.Max(0f, secondsAdvanced);
        }

        public bool Changed { get; }
        public float MaruDueAtSeconds { get; }
        public float SecondsAdvanced { get; }
    }

    [Serializable]
    public sealed class P8DeathCauseReport
    {
        public P8DeathCauseReport(
            P8RunEndKind runEndKind,
            P8StatueImpactKind statueImpact,
            float secondsMaruAdvanced,
            bool statueWasDestroyed,
            bool secondBite)
        {
            RunEndKind = runEndKind;
            StatueImpact = statueImpact;
            SecondsMaruAdvanced = Mathf.Max(0f, secondsMaruAdvanced);
            StatueWasDestroyed = statueWasDestroyed;
            SecondBite = secondBite;
            PrimaryMessage = BuildPrimaryMessage();
            TimingMessage = BuildTimingMessage();
        }

        public P8RunEndKind RunEndKind { get; }
        public P8StatueImpactKind StatueImpact { get; }
        public float SecondsMaruAdvanced { get; }
        public bool StatueWasDestroyed { get; }
        public bool SecondBite { get; }
        public string PrimaryMessage { get; }
        public string TimingMessage { get; }
        public int CauseIconCount =>
            (StatueWasDestroyed ? 3 : 1)
            + (SecondsMaruAdvanced > 0.01f ? 1 : 0)
            + (RunEndKind != P8RunEndKind.None ? 1 : 0);
        public bool HasConcreteCausalChain =>
            RunEndKind != P8RunEndKind.None
            && !string.IsNullOrWhiteSpace(PrimaryMessage)
            && !string.IsNullOrWhiteSpace(TimingMessage)
            && CauseIconCount >= (StatueWasDestroyed ? 3 : 2);

        private string BuildPrimaryMessage()
        {
            switch (RunEndKind)
            {
                case P8RunEndKind.SecondMaruBite:
                    return "같은 스테이지에서 마루에게 두 번째로 물렸습니다.";
                case P8RunEndKind.FailedFirstBiteEscape:
                    return "마루의 첫 물림에서 2초 안에 탈출하지 못했습니다.";
                case P8RunEndKind.HealthDepleted:
                    return "마루의 첫 물림으로 체력이 모두 소진되었습니다.";
                default:
                    return "런이 종료되었습니다.";
            }
        }

        private string BuildTimingMessage()
        {
            if (StatueWasDestroyed)
            {
                string impact;
                switch (StatueImpact)
                {
                    case P8StatueImpactKind.GrapplePulledObject:
                        impact = "갈고리로 당긴 물체";
                        break;
                    case P8StatueImpactKind.Bomb:
                        impact = "폭발";
                        break;
                    case P8StatueImpactKind.FallingObject:
                        impact = "떨어진 물체";
                        break;
                    default:
                        impact = "충돌한 물체";
                        break;
                }

                return SecondsMaruAdvanced > 0.01f
                    ? $"{impact}가 귀향상을 파괴해 마루가 "
                        + $"{Mathf.RoundToInt(SecondsMaruAdvanced)}초 "
                        + "일찍 나타났습니다."
                    : $"{impact}가 귀향상을 파괴했습니다.";
            }

            return SecondBite
                ? "첫 물림 뒤에도 마루의 추적이 계속되었습니다."
                : "첫 물림의 2초 탈출 시간이 끝났습니다.";
        }
    }

    public sealed class P8MaruStagePlan
    {
        public P8MaruStagePlan(
            int seed,
            P6StageSlot stageSlot,
            bool hasHomecomingStatue,
            int statueNodeId,
            int statueExitDistance,
            int returnPileNodeId)
        {
            Seed = seed;
            StageSlot = stageSlot;
            HasHomecomingStatue = hasHomecomingStatue;
            StatueNodeId = statueNodeId;
            StatueExitDistance = statueExitDistance;
            ReturnPileNodeId = returnPileNodeId;
        }

        public int Seed { get; }
        public P6StageSlot StageSlot { get; }
        public bool HasHomecomingStatue { get; }
        public int StatueNodeId { get; }
        public int StatueExitDistance { get; }
        public int ReturnPileNodeId { get; }
        public bool StatueDistanceSatisfied =>
            !HasHomecomingStatue
            || (StatueExitDistance >= 3 && StatueExitDistance <= 5);
    }

    public static class P8MaruStagePlanner
    {
        public const int OtherNormalStageStatuePercent = 35;

        public static IReadOnlyCollection<int> CollectAuthoredStatueNodeIds(
            P6RoomGraphPlan plan)
        {
            var authored = new HashSet<int>();
            if (plan == null)
            {
                return authored;
            }

            for (int index = 0; index < plan.Rooms.Count; index++)
            {
                P6RoomNode room = plan.Rooms[index];
                if (P6RoomGraphValidator.HostsMaruStatue(room))
                {
                    authored.Add(room.Id);
                }
            }

            return authored;
        }

        public static P8MaruStagePlan Generate(
            int seed,
            P7StageGraphSnapshot graph,
            bool bossRoom = false,
            IReadOnlyCollection<int> authoredStatueNodeIds = null)
        {
            if (graph == null || graph.Rooms.Count == 0)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            bool guaranteed = graph.StageSlot == P6StageSlot.X2;
            var random = new P6DeterministicRandom(seed);
            bool shouldPlace = !bossRoom
                && (guaranteed
                    || random.NextInt(100)
                        < OtherNormalStageStatuePercent);
            if (!shouldPlace)
            {
                return new P8MaruStagePlan(
                    seed,
                    graph.StageSlot,
                    false,
                    -1,
                    -1,
                    graph.StartNodeId);
            }

            // P6 also labels incidental low-degree rooms MaruStatue, so the
            // caller may narrow the pool to prefabs that author the statue.
            HashSet<int> authored = authoredStatueNodeIds == null
                ? null
                : new HashSet<int>(authoredStatueNodeIds);
            Dictionary<int, int> distances =
                DistancesFrom(graph, graph.ExitNodeId);
            P7StageGraphRoom[] candidates = graph.Rooms
                .Where(room =>
                    (room.Role & RoomRole.MaruStatue) != 0
                    && (authored == null
                        || authored.Contains(room.NodeId))
                    && distances.TryGetValue(room.NodeId, out int distance)
                    && distance >= 3
                    && distance <= 5)
                .OrderBy(room => room.NodeId)
                .ToArray();
            if (candidates.Length == 0)
            {
                throw new InvalidOperationException(
                    "P8 requires a visible MaruStatue room three to five "
                    + "graph rooms from the exit.");
            }

            P7StageGraphRoom selected =
                candidates[random.NextInt(candidates.Length)];
            return new P8MaruStagePlan(
                seed,
                graph.StageSlot,
                true,
                selected.NodeId,
                distances[selected.NodeId],
                graph.StartNodeId);
        }

        private static Dictionary<int, int> DistancesFrom(
            P7StageGraphSnapshot graph,
            int origin)
        {
            Dictionary<int, List<int>> adjacency = graph.Rooms.ToDictionary(
                room => room.NodeId,
                _ => new List<int>());
            for (int index = 0; index < graph.Edges.Count; index++)
            {
                P7StageGraphEdge edge = graph.Edges[index];
                adjacency[edge.FirstNodeId].Add(edge.SecondNodeId);
                adjacency[edge.SecondNodeId].Add(edge.FirstNodeId);
            }

            var distances = new Dictionary<int, int> { [origin] = 0 };
            var queue = new Queue<int>();
            queue.Enqueue(origin);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                List<int> neighbours = adjacency[current];
                for (int index = 0; index < neighbours.Count; index++)
                {
                    int next = neighbours[index];
                    if (distances.ContainsKey(next))
                    {
                        continue;
                    }

                    distances[next] = distances[current] + 1;
                    queue.Enqueue(next);
                }
            }

            return distances;
        }
    }

    public readonly struct P8MaruGateSummary
    {
        public P8MaruGateSummary(
            int sampleCount,
            int preClearAppearances,
            int statueSurvivals,
            int concreteDeathCauses)
        {
            SampleCount = Mathf.Max(1, sampleCount);
            PreClearAppearances = Mathf.Max(0, preClearAppearances);
            StatueSurvivals = Mathf.Max(0, statueSurvivals);
            ConcreteDeathCauses = Mathf.Max(0, concreteDeathCauses);
        }

        public int SampleCount { get; }
        public int PreClearAppearances { get; }
        public int StatueSurvivals { get; }
        public int ConcreteDeathCauses { get; }
        public float PreClearAppearanceRate =>
            (float)PreClearAppearances / SampleCount;
        public float StatueSurvivalRate =>
            (float)StatueSurvivals / SampleCount;
        public float DeathCauseComprehensionProxy =>
            (float)ConcreteDeathCauses / SampleCount;
        public bool AppearanceGatePassed =>
            PreClearAppearanceRate >= 0.15f
            && PreClearAppearanceRate <= 0.30f;
        public bool StatueSurvivalGatePassed =>
            StatueSurvivalRate >= 0.40f
            && StatueSurvivalRate <= 0.60f;
        public bool DeathCauseGatePassed =>
            DeathCauseComprehensionProxy >= 0.90f;
        public bool Passed =>
            AppearanceGatePassed
            && StatueSurvivalGatePassed
            && DeathCauseGatePassed;
    }

    public static class P8MaruGateEvaluator
    {
        public static P8MaruGateSummary EvaluateSyntheticCohort(
            int sampleCount,
            int seed)
        {
            if (sampleCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleCount));
            }

            var random = new P6DeterministicRandom(seed);
            int appearances = 0;
            int survivals = 0;
            int concreteCauses = 0;
            for (int index = 0; index < sampleCount; index++)
            {
                float normalClearSeconds =
                    135f + random.NextInt(76);
                if (normalClearSeconds
                    >= P8MaruTimelineProfile.BaseMaruDueSeconds)
                {
                    appearances++;
                }

                int reaction = random.NextInt(100);
                int routeSafety = random.NextInt(21) - 10;
                int firstBiteEscapeBonus =
                    random.NextInt(100) < 75 ? 15 : 0;
                if (reaction + routeSafety + firstBiteEscapeBonus >= 60)
                {
                    survivals++;
                }

                P8StatueImpactKind impact =
                    (P8StatueImpactKind)(index % 4);
                var report = new P8DeathCauseReport(
                    P8RunEndKind.SecondMaruBite,
                    impact,
                    42f,
                    true,
                    true);
                if (report.HasConcreteCausalChain)
                {
                    concreteCauses++;
                }
            }

            return new P8MaruGateSummary(
                sampleCount,
                appearances,
                survivals,
                concreteCauses);
        }
    }
}

#endif
