#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Generation.P6;
using StarNight.Maru.P8;
using StarNight.Objects;
using StarNight.Population.P7;
using StarNight.Rooms;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace StarNight.Tests.EditMode
{
    public sealed class P8MaruSystemTests
    {
        private readonly List<GameObject> created =
            new List<GameObject>();
        private readonly List<P8MaruTarget2D> registeredTargets =
            new List<P8MaruTarget2D>();

        [TearDown]
        public void TearDown()
        {
            for (int index = registeredTargets.Count - 1;
                 index >= 0;
                 index--)
            {
                if (registeredTargets[index] != null)
                {
                    InvokeLifecycle(
                        registeredTargets[index],
                        "OnDisable");
                }
            }

            registeredTargets.Clear();
            for (int index = created.Count - 1; index >= 0; index--)
            {
                if (created[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(created[index]);
                }
            }

            created.Clear();
        }

        [Test]
        public void TimelineProfiles_UseBaseScheduleX1GraceAndBossPause()
        {
            P8MaruTimelineProfile standard =
                P8MaruTimelineProfile.Create(P6StageSlot.X2);
            Assert.That(standard.FirstBellSeconds, Is.EqualTo(120f));
            Assert.That(standard.SecondBellSeconds, Is.EqualTo(165f));
            Assert.That(standard.MaruDueSeconds, Is.EqualTo(195f));
            Assert.That(standard.PausedForBoss, Is.False);

            P8MaruTimelineProfile firstStage =
                P8MaruTimelineProfile.Create(P6StageSlot.X1);
            Assert.That(firstStage.FirstBellSeconds, Is.EqualTo(140f));
            Assert.That(firstStage.SecondBellSeconds, Is.EqualTo(185f));
            Assert.That(firstStage.MaruDueSeconds, Is.EqualTo(215f));

            P8MaruTimelineProfile boss =
                P8MaruTimelineProfile.Create(
                    P6StageSlot.X3,
                    bossRoom: true);
            Assert.That(boss.PausedForBoss, Is.True);
        }

        [Test]
        public void Timeline_LargeAdvanceEmitsShortShortLongExactlyOnce()
        {
            P8MaruTimeline2D timeline = CreateTimeline(
                P8MaruTimelineProfile.Create(P6StageSlot.X2));
            var events = new List<P8BellEvent>();
            timeline.BellRang += value => events.Add(value);

            timeline.StartTimeline();
            timeline.Advance(1000f);

            Assert.That(
                events.Select(value => value.Signal),
                Is.EqualTo(new[]
                {
                    P8BellSignal.Short,
                    P8BellSignal.Short,
                    P8BellSignal.Long
                }));
            Assert.That(
                events.Select(value => value.Phase),
                Is.EqualTo(new[]
                {
                    P8MaruPhase.FirstBell,
                    P8MaruPhase.SecondBell,
                    P8MaruPhase.Hunting
                }));
            Assert.That(
                events.All(value =>
                    value.Cause == P8BellCause.NaturalTimeline),
                Is.True);
            Assert.That(timeline.Phase, Is.EqualTo(P8MaruPhase.Hunting));

            timeline.Advance(1000f);
            Assert.That(events, Has.Count.EqualTo(3));
        }

        [Test]
        public void Timeline_BossPauseNeverAdvancesOrRings()
        {
            P8MaruTimeline2D timeline = CreateTimeline(
                P8MaruTimelineProfile.Create(
                    P6StageSlot.X3,
                    bossRoom: true));
            int bellCount = 0;
            timeline.BellRang += _ => bellCount++;

            timeline.StartTimeline();
            timeline.Advance(1000f);

            Assert.That(timeline.IsRunning, Is.False);
            Assert.That(timeline.ElapsedSeconds, Is.Zero);
            Assert.That(timeline.Phase, Is.EqualTo(P8MaruPhase.Calm));
            Assert.That(bellCount, Is.Zero);
        }

        [TestCase(0f, 20f)]
        [TestCase(120f, 12f)]
        [TestCase(165f, 5f)]
        public void StatueDestruction_UsesTwentyTwelveOrFiveSecondDelay(
            float elapsedBeforeDestruction,
            float expectedDelay)
        {
            P8MaruTimeline2D timeline = CreateTimeline(
                P8MaruTimelineProfile.Create(P6StageSlot.X2));
            var events = new List<P8BellEvent>();
            timeline.BellRang += value => events.Add(value);
            timeline.StartTimeline();
            timeline.Advance(elapsedBeforeDestruction);

            P8StatueDestructionResult result =
                timeline.ApplyStatueDestroyed(P8StatueImpactKind.Test);

            Assert.That(result.Changed, Is.True);
            Assert.That(
                result.MaruDueAtSeconds,
                Is.EqualTo(elapsedBeforeDestruction + expectedDelay)
                    .Within(0.0001f));
            Assert.That(
                timeline.TimeUntilMaru,
                Is.EqualTo(expectedDelay).Within(0.0001f));
            Assert.That(timeline.StatueWasDestroyed, Is.True);
            Assert.That(
                timeline.LastStatueImpact,
                Is.EqualTo(P8StatueImpactKind.Test));
            Assert.That(
                events
                    .Where(value =>
                        value.Cause == P8BellCause.StatueDestroyed)
                    .Select(value => value.Signal),
                Is.EqualTo(new[]
                {
                    P8BellSignal.Short,
                    P8BellSignal.Short,
                    P8BellSignal.Long
                }));

            P8StatueDestructionResult repeated =
                timeline.ApplyStatueDestroyed(P8StatueImpactKind.Bomb);
            Assert.That(repeated.Changed, Is.False);
            Assert.That(
                events.Count(value =>
                    value.Cause == P8BellCause.StatueDestroyed),
                Is.EqualTo(3));

            timeline.Advance(expectedDelay);
            Assert.That(timeline.Phase, Is.EqualTo(P8MaruPhase.Hunting));
        }

        [Test]
        public void RoomGraph_UsesStableShortestPathAndStagePlannerDistance()
        {
            P8MaruRoomGraph2D graph = CreateTrackingGraph();
            Assert.That(graph.Distance(0, 3), Is.EqualTo(2));
            Assert.That(graph.Distance(3, 0), Is.EqualTo(2));
            Assert.That(graph.Distance(0, 0), Is.Zero);
            Assert.That(
                graph.NextWaypoint(Vector2.zero, new Vector2(2f, 1f)),
                Is.EqualTo(new Vector2(1f, 0f)));

            P7StageGraphSnapshot stageGraph = CreateStagePlannerGraph();
            P8MaruStagePlan plan = P8MaruStagePlanner.Generate(
                8808,
                stageGraph);
            Assert.That(plan.HasHomecomingStatue, Is.True);
            Assert.That(plan.StatueNodeId, Is.EqualTo(1));
            Assert.That(plan.StatueExitDistance, Is.EqualTo(3));
            Assert.That(plan.StatueDistanceSatisfied, Is.True);
            Assert.That(plan.ReturnPileNodeId, Is.EqualTo(0));
        }

        [Test]
        public void StagePlanner_KeepsIncidentalMaruStatueLabelsOutOfAuthoredPool()
        {
            P7StageGraphSnapshot stageGraph =
                CreateIncidentalStatueGraph();
            P8MaruStagePlan unfiltered = P8MaruStagePlanner.Generate(
                8808,
                stageGraph);
            Assert.That(
                unfiltered.StatueNodeId,
                Is.EqualTo(1).Or.EqualTo(5),
                "Role labels alone cannot tell an authored statue apart.");

            P8MaruStagePlan filtered = P8MaruStagePlanner.Generate(
                8808,
                stageGraph,
                bossRoom: false,
                authoredStatueNodeIds: new[] { 5 });
            Assert.That(filtered.HasHomecomingStatue, Is.True);
            Assert.That(filtered.StatueNodeId, Is.EqualTo(5));
            Assert.That(filtered.StatueExitDistance, Is.EqualTo(3));
            Assert.That(filtered.StatueDistanceSatisfied, Is.True);

            Assert.Throws<InvalidOperationException>(
                () => P8MaruStagePlanner.Generate(
                    8808,
                    stageGraph,
                    bossRoom: false,
                    authoredStatueNodeIds: Array.Empty<int>()));
        }

        [Test]
        public void Pursuer_SelectsAuthoredPriorityThenStableGraphOrder()
        {
            P8MaruTarget2D[] preexisting =
                P8MaruTarget2D.ActiveTargets
                    .Where(value => value != null && value.IsAvailable)
                    .ToArray();
            for (int index = 0; index < preexisting.Length; index++)
            {
                preexisting[index].SetAvailable(false);
            }

            try
            {
                P8MaruRoomGraph2D graph = CreateTrackingGraph();
                GameObject pursuerObject = Track("P8_Test_Pursuer");
                P8MaruPursuer2D pursuer =
                    pursuerObject.AddComponent<P8MaruPursuer2D>();
                pursuer.Configure(graph, null, null);
                pursuerObject.transform.position = Vector2.zero;

                P8MaruTarget2D player = CreateTarget(
                    "Player",
                    P8MaruTargetKind.Player,
                    new Vector2(1f, 0f),
                    40);
                P8MaruTarget2D tool = CreateTarget(
                    "Tool",
                    P8MaruTargetKind.DroppedHandTool,
                    new Vector2(1f, 0f),
                    30);
                P8MaruTarget2D laterTreasure = CreateTarget(
                    "TreasureLater",
                    P8MaruTargetKind.LuminousTreasure,
                    new Vector2(1f, 0f),
                    20);
                P8MaruTarget2D earlierTreasure = CreateTarget(
                    "TreasureEarlier",
                    P8MaruTargetKind.LuminousTreasure,
                    new Vector2(1f, 0f),
                    10);
                P8MaruTarget2D npc = CreateTarget(
                    "LostNpc",
                    P8MaruTargetKind.LostNpc,
                    new Vector2(2f, 1f),
                    50);

                Assert.That(pursuer.SelectTarget(), Is.SameAs(npc));

                npc.SetAvailable(false);
                Assert.That(
                    pursuer.SelectTarget(),
                    Is.SameAs(earlierTreasure));

                earlierTreasure.SetAvailable(false);
                laterTreasure.SetAvailable(false);
                Assert.That(pursuer.SelectTarget(), Is.SameAs(tool));

                tool.SetAvailable(false);
                Assert.That(pursuer.SelectTarget(), Is.SameAs(player));
                Assert.That(
                    P8MaruPursuer2D.DefaultMoveSpeed,
                    Is.LessThan(3.75f));
            }
            finally
            {
                for (int index = 0; index < preexisting.Length; index++)
                {
                    if (preexisting[index] != null)
                    {
                        preexisting[index].SetAvailable(true);
                    }
                }
            }
        }

        [Test]
        public void ReturnPile_DepositsOnceRecoversAndCanDepositAgain()
        {
            GameObject pileObject = Track("P8_Test_ReturnPile");
            pileObject.transform.position = new Vector2(4f, 2f);
            P8ReturnPile2D pile =
                pileObject.AddComponent<P8ReturnPile2D>();
            pile.Configure(pileObject.transform, configuredRowWidth: 3);
            P8MaruTarget2D target = CreateTarget(
                "ReturnedTreasure",
                P8MaruTargetKind.LuminousTreasure,
                Vector2.zero,
                1);
            int deposits = 0;
            int recoveries = 0;
            pile.ItemDeposited += _ => deposits++;
            pile.ItemRecovered += _ => recoveries++;

            Assert.That(pile.Deposit(target), Is.True);
            Assert.That(pile.Deposit(target), Is.False);
            Assert.That(pile.DepositedCount, Is.EqualTo(1));
            Assert.That(pile.Contains(target), Is.True);
            Assert.That(target.InReturnPile, Is.True);
            Assert.That(
                (Vector2)target.transform.position,
                Is.EqualTo((Vector2)pileObject.transform.position));
            Assert.That(deposits, Is.EqualTo(1));

            target.ReleaseFromPileForTests();
            Assert.That(pile.DepositedCount, Is.Zero);
            Assert.That(pile.Contains(target), Is.False);
            Assert.That(target.InReturnPile, Is.False);
            Assert.That(recoveries, Is.EqualTo(1));

            Assert.That(pile.Deposit(target), Is.True);
            Assert.That(pile.DepositedCount, Is.EqualTo(1));
            Assert.That(deposits, Is.EqualTo(2));

            P8MaruTarget2D player = CreateTarget(
                "PlayerCannotEnterPile",
                P8MaruTargetKind.Player,
                Vector2.zero,
                2);
            Assert.That(pile.Deposit(player), Is.False);
        }

        [Test]
        public void StarTear_ConvertsToTwelveGoldExactlyOnce()
        {
            GameObject walletObject = Track("P8_Test_Wallet");
            P7EconomyWallet2D wallet =
                walletObject.AddComponent<P7EconomyWallet2D>();
            wallet.Configure();

            GameObject tearObject = Track("P8_Test_StarTear");
            Rigidbody2D body = tearObject.AddComponent<Rigidbody2D>();
            BoxCollider2D collider =
                tearObject.AddComponent<BoxCollider2D>();
            CarryableObject2D carryable =
                tearObject.AddComponent<CarryableObject2D>();
            carryable.Configure(
                null,
                body,
                collider,
                WorldObjectTraits.Carryable
                | WorldObjectTraits.Pullable,
                importantItem: true);
            P8StarTear2D tear =
                tearObject.AddComponent<P8StarTear2D>();
            tear.Configure(carryable, targetP7Wallet: wallet);
            int conversions = 0;
            tear.ConvertedAtExit += (_, value) =>
            {
                Assert.That(value, Is.EqualTo(12));
                conversions++;
            };

            Assert.That(P8StarTear2D.FootprintCells, Is.EqualTo(Vector2Int.one));
            Assert.That(tear.Value, Is.EqualTo(12));
            Assert.That(tear.TryConvertAtExit(requireHeld: false), Is.True);
            Assert.That(wallet.Gold, Is.EqualTo(12));
            Assert.That(tear.Converted, Is.True);
            Assert.That(tear.TryConvertAtExit(requireHeld: false), Is.False);
            Assert.That(wallet.Gold, Is.EqualTo(12));
            Assert.That(conversions, Is.EqualTo(1));
        }

        [Test]
        public void DeathCauseReport_PreservesConcreteStructuredCausalChain()
        {
            var report = new P8DeathCauseReport(
                P8RunEndKind.SecondMaruBite,
                P8StatueImpactKind.GrapplePulledObject,
                42f,
                statueWasDestroyed: true,
                secondBite: true);

            Assert.That(
                report.RunEndKind,
                Is.EqualTo(P8RunEndKind.SecondMaruBite));
            Assert.That(
                report.StatueImpact,
                Is.EqualTo(P8StatueImpactKind.GrapplePulledObject));
            Assert.That(report.SecondsMaruAdvanced, Is.EqualTo(42f));
            Assert.That(report.StatueWasDestroyed, Is.True);
            Assert.That(report.SecondBite, Is.True);
            Assert.That(report.PrimaryMessage, Is.Not.Null.And.Not.Empty);
            Assert.That(report.TimingMessage, Does.Contain("42"));
            Assert.That(report.CauseIconCount, Is.EqualTo(5));
            Assert.That(report.HasConcreteCausalChain, Is.True);
        }

        [Test]
        public void CohortEvaluator_UsesIndependentInclusiveGateBoundaries()
        {
            P8MaruCohortSummary lower =
                P8MaruCohortEvaluator.Evaluate(
                    BuildCohort(15, 40, 90));
            AssertGateSummary(
                lower,
                expectedAppearance: 0.15f,
                expectedSurvival: 0.40f,
                expectedCause: 0.90f);
            Assert.That(lower.Passed, Is.True);

            P8MaruCohortSummary upper =
                P8MaruCohortEvaluator.Evaluate(
                    BuildCohort(30, 60, 100));
            AssertGateSummary(
                upper,
                expectedAppearance: 0.30f,
                expectedSurvival: 0.60f,
                expectedCause: 1f);
            Assert.That(upper.Passed, Is.True);

            Assert.That(
                P8MaruCohortEvaluator.Evaluate(
                    BuildCohort(14, 40, 90)).AppearanceGatePassed,
                Is.False);
            Assert.That(
                P8MaruCohortEvaluator.Evaluate(
                    BuildCohort(30, 61, 90)).StatueGatePassed,
                Is.False);
            Assert.That(
                P8MaruCohortEvaluator.Evaluate(
                    BuildCohort(20, 50, 89)).CauseGatePassed,
                Is.False);
            Assert.That(
                P8MaruCohortEvaluator.Evaluate(null).Passed,
                Is.False);
        }

        [Test]
        [Timeout(120000)]
        public void SyntheticOneThousandPlayerGate_IsDeterministicAndPasses()
        {
            const int sampleCount = 1000;
            const int seed = 20260731;
            Stopwatch timer = Stopwatch.StartNew();
            P8MaruGateSummary first =
                P8MaruGateEvaluator.EvaluateSyntheticCohort(
                    sampleCount,
                    seed);
            P8MaruGateSummary repeated =
                P8MaruGateEvaluator.EvaluateSyntheticCohort(
                    sampleCount,
                    seed);
            timer.Stop();

            Assert.That(first.SampleCount, Is.EqualTo(sampleCount));
            Assert.That(
                repeated.PreClearAppearances,
                Is.EqualTo(first.PreClearAppearances));
            Assert.That(
                repeated.StatueSurvivals,
                Is.EqualTo(first.StatueSurvivals));
            Assert.That(
                repeated.ConcreteDeathCauses,
                Is.EqualTo(first.ConcreteDeathCauses));
            Assert.That(first.AppearanceGatePassed, Is.True);
            Assert.That(first.StatueSurvivalGatePassed, Is.True);
            Assert.That(first.DeathCauseGatePassed, Is.True);
            Assert.That(first.Passed, Is.True);
            TestContext.WriteLine(
                $"P8 synthetic cohort PASS in "
                + $"{timer.Elapsed.TotalMilliseconds:F2}ms; "
                + $"appearance={first.PreClearAppearanceRate:P1}, "
                + $"statueSurvival={first.StatueSurvivalRate:P1}, "
                + $"deathCause={first.DeathCauseComprehensionProxy:P1}.");
        }

        private P8MaruTimeline2D CreateTimeline(
            P8MaruTimelineProfile profile)
        {
            P8MaruTimeline2D timeline =
                Track("P8_Test_Timeline")
                    .AddComponent<P8MaruTimeline2D>();
            timeline.Configure(profile);
            return timeline;
        }

        private P8MaruRoomGraph2D CreateTrackingGraph()
        {
            P8MaruRoomGraph2D graph =
                Track("P8_Test_RoomGraph")
                    .AddComponent<P8MaruRoomGraph2D>();
            graph.Configure(new[]
            {
                Node(0, new Vector2(0f, 0f), 1, 2),
                Node(1, new Vector2(1f, 0f), 0, 3),
                Node(2, new Vector2(0f, 1f), 0, 3),
                Node(3, new Vector2(2f, 1f), 1, 2)
            });
            return graph;
        }

        private static P8MaruRoomNode Node(
            int id,
            Vector2 center,
            params int[] neighbours)
        {
            return new P8MaruRoomNode(
                id,
                new Rect(
                    center.x - 0.4f,
                    center.y - 0.4f,
                    0.8f,
                    0.8f),
                center,
                neighbours);
        }

        private static P7StageGraphSnapshot CreateStagePlannerGraph()
        {
            return new P7StageGraphSnapshot(
                P6StageSlot.X2,
                RoomRegion.MoonPalace,
                0,
                4,
                new[]
                {
                    StageRoom(0, RoomRole.Start, true, 0),
                    StageRoom(1, RoomRole.MaruStatue, false, -1),
                    StageRoom(2, RoomRole.Main, true, 1),
                    StageRoom(3, RoomRole.Main, true, 2),
                    StageRoom(4, RoomRole.Exit, true, 3)
                },
                new[]
                {
                    new P7StageGraphEdge(0, 1),
                    new P7StageGraphEdge(1, 2),
                    new P7StageGraphEdge(2, 3),
                    new P7StageGraphEdge(3, 4)
                },
                new[] { 0, 2, 3, 4 });
        }

        private static P7StageGraphSnapshot CreateIncidentalStatueGraph()
        {
            return new P7StageGraphSnapshot(
                P6StageSlot.X2,
                RoomRegion.MoonPalace,
                0,
                4,
                new[]
                {
                    StageRoom(0, RoomRole.Start, true, 0),
                    StageRoom(1, RoomRole.MaruStatue, false, -1),
                    StageRoom(2, RoomRole.Main, true, 1),
                    StageRoom(3, RoomRole.Main, true, 2),
                    StageRoom(4, RoomRole.Exit, true, 3),
                    StageRoom(5, RoomRole.MaruStatue, false, -1)
                },
                new[]
                {
                    new P7StageGraphEdge(0, 1),
                    new P7StageGraphEdge(0, 2),
                    new P7StageGraphEdge(2, 3),
                    new P7StageGraphEdge(3, 4),
                    new P7StageGraphEdge(2, 5)
                },
                new[] { 0, 2, 3, 4 });
        }

        private static P7StageGraphRoom StageRoom(
            int id,
            RoomRole role,
            bool main,
            int mainIndex)
        {
            return new P7StageGraphRoom(
                id,
                $"P8_Test_Room_{id}",
                new RectInt(id, 0, 1, 1),
                role,
                main,
                mainIndex);
        }

        private P8MaruTarget2D CreateTarget(
            string name,
            P8MaruTargetKind kind,
            Vector2 position,
            int stableOrder)
        {
            GameObject targetObject = Track("P8_Test_" + name);
            targetObject.transform.position = position;
            P8MaruTarget2D target =
                targetObject.AddComponent<P8MaruTarget2D>();
            target.Configure(
                kind,
                deterministicOrder: stableOrder);
            InvokeLifecycle(target, "OnEnable");
            registeredTargets.Add(target);
            return target;
        }

        private static void InvokeLifecycle(
            P8MaruTarget2D target,
            string methodName)
        {
            MethodInfo method = typeof(P8MaruTarget2D).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }

        private static IReadOnlyList<P8MaruSessionSnapshot> BuildCohort(
            int appearanceCount,
            int statueSurvivalCount,
            int correctCauseCount)
        {
            var sessions = new List<P8MaruSessionSnapshot>(300);
            for (int index = 0; index < 100; index++)
            {
                sessions.Add(new P8MaruSessionSnapshot(
                    normalCohort: true,
                    stageClearedAt: 200f,
                    maruAppearedAt:
                        index < appearanceCount ? 190f : -1f,
                    statueDestroyedAt: -1f,
                    runEndedAt: -1f,
                    actualCause: P8RunEndKind.None,
                    hasHumanCauseResponse: false,
                    reportedCause: P8RunEndKind.None));
            }

            for (int index = 0; index < 100; index++)
            {
                bool survived = index < statueSurvivalCount;
                sessions.Add(new P8MaruSessionSnapshot(
                    normalCohort: false,
                    stageClearedAt: survived ? 100f : -1f,
                    maruAppearedAt: -1f,
                    statueDestroyedAt: 10f,
                    runEndedAt: survived ? -1f : 80f,
                    actualCause: P8RunEndKind.None,
                    hasHumanCauseResponse: false,
                    reportedCause: P8RunEndKind.None));
            }

            for (int index = 0; index < 100; index++)
            {
                sessions.Add(new P8MaruSessionSnapshot(
                    normalCohort: false,
                    stageClearedAt: -1f,
                    maruAppearedAt: -1f,
                    statueDestroyedAt: -1f,
                    runEndedAt: 90f,
                    actualCause: P8RunEndKind.SecondMaruBite,
                    hasHumanCauseResponse: true,
                    reportedCause:
                        index < correctCauseCount
                            ? P8RunEndKind.SecondMaruBite
                            : P8RunEndKind.HealthDepleted));
            }

            return sessions;
        }

        private static void AssertGateSummary(
            P8MaruCohortSummary summary,
            float expectedAppearance,
            float expectedSurvival,
            float expectedCause)
        {
            Assert.That(summary.AppearanceEligible, Is.EqualTo(100));
            Assert.That(summary.StatueEligible, Is.EqualTo(100));
            Assert.That(summary.CauseEligible, Is.EqualTo(100));
            Assert.That(
                summary.AppearanceRate,
                Is.EqualTo(expectedAppearance).Within(0.0001f));
            Assert.That(
                summary.StatueSurvivalRate,
                Is.EqualTo(expectedSurvival).Within(0.0001f));
            Assert.That(
                summary.CauseUnderstandingRate,
                Is.EqualTo(expectedCause).Within(0.0001f));
        }

        private GameObject Track(string name)
        {
            var value = new GameObject(name);
            created.Add(value);
            return value;
        }
    }
}

#endif
