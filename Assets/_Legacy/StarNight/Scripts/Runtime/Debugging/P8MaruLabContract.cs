#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Generation.P6;
using StarNight.Maru.P8;
using StarNight.Objects;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Debugging
{
    [DisallowMultipleComponent]
    public sealed class P8MaruLabContract : MonoBehaviour
    {
        public const string ContractId =
            "P8_Maru_P6P7_IntegratedLab_v1";

        [Header("Integrated P6/P7 source")]
        [SerializeField] private string contractId = ContractId;
        [SerializeField] private P6RoomGraphLabContract graphContract;
        [SerializeField] private P7PopulationLabContract populationContract;

        [Header("P8 runtime")]
        [SerializeField] private P8MaruRoomGraph2D maruGraph;
        [SerializeField] private P8MaruTimeline2D timeline;
        [SerializeField] private P8MaruPursuer2D pursuer;
        [SerializeField] private P8ReturnPile2D returnPile;
        [SerializeField] private P8HomecomingStatue2D statue;
        [SerializeField] private P8StarTear2D starTear;
        [SerializeField] private P8MaruBellPresenter2D presenter;

        [Header("Validated graph result")]
        [SerializeField] private int statueNodeId = -1;
        [SerializeField] private int exitNodeId = -1;
        [SerializeField] private int statueExitDistance = -1;

        [Header("Synthetic gate summary")]
        [SerializeField, Min(0)] private int gateSampleCount;
        [SerializeField, Min(0)] private int preClearAppearances;
        [SerializeField, Min(0)] private int statueSurvivals;
        [SerializeField, Min(0)] private int concreteDeathCauses;

        [Header("Validation")]
        [SerializeField, TextArea(3, 16)] private string lastValidation =
            "Not validated.";

        public P6RoomGraphLabContract GraphContract => graphContract;
        public P7PopulationLabContract PopulationContract =>
            populationContract;
        public P8MaruRoomGraph2D MaruGraph => maruGraph;
        public P8MaruTimeline2D Timeline => timeline;
        public P8MaruPursuer2D Pursuer => pursuer;
        public P8ReturnPile2D ReturnPile => returnPile;
        public P8HomecomingStatue2D Statue => statue;
        public P8StarTear2D StarTear => starTear;
        public P8MaruBellPresenter2D Presenter => presenter;
        public int StatueNodeId => statueNodeId;
        public int ExitNodeId => exitNodeId;
        public int StatueExitDistance => statueExitDistance;
        public string LastValidation => lastValidation;
        public bool ValidationPassed => lastValidation == "PASS";
        public P8MaruGateSummary GateSummary =>
            new P8MaruGateSummary(
                gateSampleCount,
                preClearAppearances,
                statueSurvivals,
                concreteDeathCauses);

        public void Configure(
            P6RoomGraphLabContract sourceGraphContract,
            P7PopulationLabContract sourcePopulationContract,
            P8MaruRoomGraph2D runtimeGraph,
            P8MaruTimeline2D maruTimeline,
            P8MaruPursuer2D maruPursuer,
            P8ReturnPile2D pile,
            P8HomecomingStatue2D homecomingStatue,
            P8StarTear2D tear,
            P8MaruBellPresenter2D bellPresenter,
            P8MaruGateSummary gate)
        {
            contractId = ContractId;
            graphContract = sourceGraphContract;
            populationContract = sourcePopulationContract;
            maruGraph = runtimeGraph;
            timeline = maruTimeline;
            pursuer = maruPursuer;
            returnPile = pile;
            statue = homecomingStatue;
            starTear = tear;
            presenter = bellPresenter;
            gateSampleCount = gate.SampleCount;
            preClearAppearances = gate.PreClearAppearances;
            statueSurvivals = gate.StatueSurvivals;
            concreteDeathCauses = gate.ConcreteDeathCauses;
            statueNodeId = -1;
            exitNodeId = -1;
            statueExitDistance = -1;
            lastValidation = "Configured; not validated.";
        }

        [ContextMenu("Validate P8 Maru Integrated Lab")]
        public void ValidateOrThrow()
        {
            var issues = new List<string>();
            ValidateIdentity(issues);
            ValidateReferences(issues);
            ValidateIntegratedRoots(issues);
            ValidateGraphSnapshot(issues);
            ValidateTimeline(issues);
            ValidateRuntimeLinks(issues);
            ValidateGateSummary(issues);

            if (issues.Count == 0)
            {
                lastValidation = "PASS";
                return;
            }

            lastValidation = string.Join(Environment.NewLine, issues);
            throw new InvalidOperationException(
                "P8 Maru integrated Lab contract failed:"
                + Environment.NewLine
                + lastValidation);
        }

        private void ValidateIdentity(List<string> issues)
        {
            if (contractId != ContractId)
            {
                issues.Add("P8 Lab contract identity is invalid.");
            }
        }

        private void ValidateReferences(List<string> issues)
        {
            Require(graphContract, nameof(graphContract), issues);
            Require(populationContract, nameof(populationContract), issues);
            Require(maruGraph, nameof(maruGraph), issues);
            Require(timeline, nameof(timeline), issues);
            Require(pursuer, nameof(pursuer), issues);
            Require(returnPile, nameof(returnPile), issues);
            Require(statue, nameof(statue), issues);
            Require(starTear, nameof(starTear), issues);
            Require(presenter, nameof(presenter), issues);
        }

        private void ValidateIntegratedRoots(List<string> issues)
        {
            if (graphContract != null && graphContract.gameObject != gameObject)
            {
                issues.Add("P6 and P8 Lab contracts must share one root.");
            }

            if (populationContract != null
                && populationContract.gameObject != gameObject)
            {
                issues.Add("P7 and P8 Lab contracts must share one root.");
            }

            if (graphContract != null
                && populationContract != null
                && populationContract.GraphContract != graphContract)
            {
                issues.Add("P7 does not reference this P6 graph contract.");
            }

            if (graphContract != null && !graphContract.ValidationPassed)
            {
                issues.Add("The integrated P6 graph contract is not valid.");
            }

            if (populationContract != null
                && !populationContract.ValidationPassed)
            {
                issues.Add(
                    "The integrated P7 population contract is not valid.");
            }

            ValidateUnderRoot(maruGraph, nameof(maruGraph), issues);
            ValidateUnderRoot(timeline, nameof(timeline), issues);
            ValidateUnderRoot(pursuer, nameof(pursuer), issues);
            ValidateUnderRoot(returnPile, nameof(returnPile), issues);
            ValidateUnderRoot(statue, nameof(statue), issues);
            ValidateUnderRoot(starTear, nameof(starTear), issues);
            ValidateUnderRoot(presenter, nameof(presenter), issues);
        }

        private void ValidateGraphSnapshot(List<string> issues)
        {
            statueNodeId = -1;
            exitNodeId = graphContract != null
                ? graphContract.ExitNodeId
                : -1;
            statueExitDistance = -1;
            if (graphContract == null || maruGraph == null)
            {
                return;
            }

            try
            {
                maruGraph.Validate();
            }
            catch (Exception exception)
            {
                issues.Add("P8 graph is invalid: " + exception.Message);
                return;
            }

            if (maruGraph.NodeCount != graphContract.Placements.Count)
            {
                issues.Add(
                    "P8 graph node count must equal P6 placement count.");
            }

            var expectedIds = new HashSet<int>();
            var expectedAdjacency =
                new Dictionary<int, HashSet<int>>();
            int statueCount = 0;
            for (int index = 0;
                 index < graphContract.Placements.Count;
                 index++)
            {
                P6RoomGraphLabPlacement placement =
                    graphContract.Placements[index];
                expectedIds.Add(placement.NodeId);
                expectedAdjacency[placement.NodeId] =
                    new HashSet<int>();
                if ((placement.Role & RoomRole.MaruStatue) != 0)
                {
                    statueCount++;
                    statueNodeId = placement.NodeId;
                }
            }

            for (int index = 0; index < graphContract.Edges.Count; index++)
            {
                P6RoomGraphLabEdge edge = graphContract.Edges[index];
                if (expectedAdjacency.TryGetValue(
                        edge.FirstNodeId,
                        out HashSet<int> first))
                {
                    first.Add(edge.SecondNodeId);
                }

                if (expectedAdjacency.TryGetValue(
                        edge.SecondNodeId,
                        out HashSet<int> second))
                {
                    second.Add(edge.FirstNodeId);
                }
            }

            var actualIds = new HashSet<int>();
            var actualAdjacency =
                new Dictionary<int, HashSet<int>>();
            for (int index = 0; index < maruGraph.Nodes.Count; index++)
            {
                P8MaruRoomNode node = maruGraph.Nodes[index];
                actualIds.Add(node.NodeId);
                actualAdjacency[node.NodeId] =
                    new HashSet<int>(node.NeighbourNodeIds);
            }

            if (!actualIds.SetEquals(expectedIds))
            {
                issues.Add("P8 graph node IDs differ from the P6 graph.");
            }

            foreach (KeyValuePair<int, HashSet<int>> expected
                     in expectedAdjacency)
            {
                if (!actualAdjacency.TryGetValue(
                        expected.Key,
                        out HashSet<int> actual)
                    || !actual.SetEquals(expected.Value))
                {
                    issues.Add(
                        $"P8 graph neighbours differ at node {expected.Key}.");
                }
            }

            if (statueCount != 1)
            {
                issues.Add(
                    "The X-2 P8 Lab requires exactly one MaruStatue node.");
                return;
            }

            statueExitDistance = Distance(
                actualAdjacency,
                statueNodeId,
                exitNodeId);
            if (statueExitDistance < 3 || statueExitDistance > 5)
            {
                issues.Add(
                    "MaruStatue must be three to five graph rooms "
                    + "from Exit.");
            }
        }

        private void ValidateTimeline(List<string> issues)
        {
            if (timeline == null)
            {
                return;
            }

            if (timeline.StageSlot != P6StageSlot.X2
                || !Approximately(
                    timeline.FirstBellSeconds,
                    P8MaruTimelineProfile.BaseFirstBellSeconds)
                || !Approximately(
                    timeline.SecondBellSeconds,
                    P8MaruTimelineProfile.BaseSecondBellSeconds)
                || !Approximately(
                    timeline.NaturalMaruDueSeconds,
                    P8MaruTimelineProfile.BaseMaruDueSeconds)
                || timeline.PausedForBoss)
            {
                issues.Add(
                    "The X-2 Lab timeline must use 120, 165, and "
                    + "195 seconds without boss pause.");
            }
        }

        private void ValidateRuntimeLinks(List<string> issues)
        {
            if (pursuer != null
                && (pursuer.RoomGraph != maruGraph
                    || pursuer.ReturnPile != returnPile))
            {
                issues.Add(
                    "P8 pursuer is not linked to the Lab graph and pile.");
            }

            if (statue != null
                && (statue.Timeline != timeline
                    || statue.StarTear != starTear
                    || statue.CanBeCarried
                    || statue.Traits
                        != (WorldObjectTraits.Heavy
                            | WorldObjectTraits.Breakable
                            | WorldObjectTraits.Pullable)))
            {
                issues.Add("P8 Homecoming Statue contract is invalid.");
            }

            if (starTear != null
                && (starTear.Value != P8StarTear2D.GoldValue
                    || starTear.Value != 12
                    || starTear.Carryable == null))
            {
                issues.Add("P8 Star Tear must be a carryable 12-gold item.");
            }

            if (returnPile != null && returnPile.DepositAnchor == null)
            {
                issues.Add("P8 Return Pile requires a deposit anchor.");
            }

            if (presenter != null
                && (presenter.Timeline != timeline
                    || presenter.FirstBellVisual == null
                    || presenter.SecondBellVisual == null
                    || presenter.HuntVisual == null))
            {
                issues.Add(
                    "P8 bell presenter requires all three visual phases.");
            }
        }

        private void ValidateGateSummary(List<string> issues)
        {
            if (gateSampleCount <= 0)
            {
                issues.Add("P8 gate summary requires a non-empty cohort.");
                return;
            }

            if (!GateSummary.Passed)
            {
                issues.Add(
                    "P8 gate summary must satisfy 15-30% appearance, "
                    + "40-60% statue survival, and at least 90% "
                    + "concrete death causes.");
            }
        }

        private void ValidateUnderRoot(
            Component component,
            string label,
            List<string> issues)
        {
            if (component != null
                && component.transform.root != transform.root)
            {
                issues.Add($"{label} is outside the integrated P8 root.");
            }
        }

        private static int Distance(
            IReadOnlyDictionary<int, HashSet<int>> adjacency,
            int origin,
            int destination)
        {
            if (origin == destination)
            {
                return 0;
            }

            if (!adjacency.ContainsKey(origin)
                || !adjacency.ContainsKey(destination))
            {
                return -1;
            }

            var queue = new Queue<(int Node, int Distance)>();
            var visited = new HashSet<int> { origin };
            queue.Enqueue((origin, 0));
            while (queue.Count > 0)
            {
                (int current, int distance) = queue.Dequeue();
                foreach (int next in adjacency[current])
                {
                    if (!visited.Add(next))
                    {
                        continue;
                    }

                    if (next == destination)
                    {
                        return distance + 1;
                    }

                    queue.Enqueue((next, distance + 1));
                }
            }

            return -1;
        }

        private static bool Approximately(float first, float second)
        {
            return Mathf.Abs(first - second) <= 0.001f;
        }

        private static void Require(
            UnityEngine.Object value,
            string label,
            List<string> issues)
        {
            if (value == null)
            {
                issues.Add($"P8 Lab reference is missing: {label}.");
            }
        }
    }
}

#endif
