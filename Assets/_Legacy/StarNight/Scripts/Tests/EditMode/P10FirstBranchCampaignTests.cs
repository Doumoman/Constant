#if LEGACY_DISABLED
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Campaign.P10;
using StarNight.Debugging;
using StarNight.Folklore.P9;
using StarNight.Generation.P6;
using StarNight.Maru.P8;
using StarNight.Rooms;
using UnityEditor;
using UnityEngine;

namespace StarNight.Tests.EditMode
{
    public sealed class P10FirstBranchCampaignTests
    {
        private const string CatalogPath =
            "Assets/StarNight/Data/P10/"
            + "P10_FirstBranchCampaignCatalog.asset";

        private readonly List<GameObject> created =
            new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = created.Count - 1; index >= 0; index--)
            {
                if (created[index] != null)
                {
                    Object.DestroyImmediate(created[index]);
                }
            }

            created.Clear();
        }

        [Test]
        public void Catalog_ContainsNineDistinctRegionalStageContracts()
        {
            P10CampaignCatalog catalog = LoadCatalog();

            Assert.That(catalog.ValidateCatalog(), Is.Empty);
            Assert.That(catalog.Stages.Count, Is.EqualTo(9));
            Assert.That(
                catalog.Stages
                    .Select(stage => stage.StageId)
                    .Distinct()
                    .Count(),
                Is.EqualTo(9));
            Assert.That(
                catalog.Stages.All(stage =>
                    stage != null
                    && stage.StageId != P10StageId.None
                    && stage.MainPathToolFree
                    && stage.OptionalEventsNeverGateExit
                    && !string.IsNullOrWhiteSpace(
                        stage.CoreActionSentence)),
                Is.True);

            AssertRegionStages(
                catalog,
                RoomRegion.MoonPalace,
                P9BranchKind.None);
            AssertRegionStages(
                catalog,
                RoomRegion.MagpieBridge,
                P9BranchKind.MagpieBridge);
            AssertRegionStages(
                catalog,
                RoomRegion.DragonPalace,
                P9BranchKind.DragonPalace);

            Assert.That(
                catalog.Find(P10StageId.MoonPalace13).Boss,
                Is.EqualTo(P10BossKind.Kungtteoki));
            Assert.That(
                catalog.Find(P10StageId.MagpieBridge23).Boss,
                Is.EqualTo(P10BossKind.KnotSpider));
            Assert.That(
                catalog.Find(P10StageId.DragonPalace23).Boss,
                Is.EqualTo(P10BossKind.DragonGatekeeper));
        }

        [Test]
        public void RouteProof_CoversNormalAndBothCrossRoutes()
        {
            P10CampaignCatalog catalog = LoadCatalog();
            P10CampaignRouteProof proof =
                P10CampaignRouteProof.Evaluate(catalog);
            P10BranchFeelDefinition magpie =
                catalog.FindBranch(P9BranchKind.MagpieBridge);
            P10BranchFeelDefinition dragon =
                catalog.FindBranch(P9BranchKind.DragonPalace);

            Assert.That(proof.Passed, Is.True);
            Assert.That(proof.NormalMagpieValid, Is.True);
            Assert.That(proof.NormalDragonValid, Is.True);
            Assert.That(proof.SingleBranchCommonEntryValid, Is.True);
            Assert.That(proof.MagpieToDragonValid, Is.True);
            Assert.That(proof.DragonToMagpieValid, Is.True);
            Assert.That(proof.CrossRoutesSkipOppositeX1, Is.True);

            Assert.That(magpie, Is.Not.Null);
            Assert.That(dragon, Is.Not.Null);
            Assert.That(magpie.IsDistinctFrom(dragon), Is.True);
            Assert.That(
                magpie.PrimaryAxis,
                Is.EqualTo(P10TraversalAxis.Horizontal));
            Assert.That(
                dragon.PrimaryAxis,
                Is.EqualTo(P10TraversalAxis.Vertical));
            Assert.That(
                magpie.SignatureMechanics
                & dragon.SignatureMechanics,
                Is.EqualTo(P10StageMechanics.None));
        }

        [TestCase(P9BranchKind.MagpieBridge)]
        [TestCase(P9BranchKind.DragonPalace)]
        public void NormalRoute_OneCompletedBranchUnlocksCommonRegion(
            P9BranchKind branch)
        {
            CampaignHarness harness =
                CreateCampaignHarness(grantBothGifts: false);

            CompleteMoonPalace(harness.Director);
            Assert.That(
                harness.Director.ChooseFirstBranch(branch),
                Is.True);
            CompleteFirstBranch(harness.Director, branch);

            Assert.That(
                harness.Director.CompletedFirstBranch,
                Is.EqualTo(branch));
            Assert.That(
                harness.Director.CompletedStages.Count,
                Is.EqualTo(6));
            Assert.That(
                harness.Director.CanEnterCommonRegion,
                Is.True);
            Assert.That(
                harness.Director.MainProgressRequiresCrossRoute,
                Is.False);
            Assert.That(
                harness.Director.CanOpenCrossRouteFrom(branch),
                Is.False);

            P9BranchKind opposite = Opposite(branch);
            Assert.That(
                harness.Director.CompletedStages.Any(stage =>
                    StageBelongsToBranch(stage, opposite)),
                Is.False);
            Assert.That(
                harness.Director.TryEnterCommonRegion(),
                Is.True);
            Assert.That(
                harness.Director.CommonRegionEnteredSuccessfully,
                Is.True);
            Assert.That(
                harness.Director.Phase,
                Is.EqualTo(
                    P10CampaignPhase.DepartedToCommonRegion));
            Assert.That(
                harness.Director.TryEnterCommonRegion(),
                Is.False);
        }

        [TestCase(P9BranchKind.MagpieBridge)]
        [TestCase(P9BranchKind.DragonPalace)]
        public void CrossRoute_IsBidirectionalOptionalAndSkipsOppositeX1(
            P9BranchKind firstBranch)
        {
            CampaignHarness harness =
                CreateCampaignHarness(grantBothGifts: true);
            P9BranchKind crossBranch = Opposite(firstBranch);

            CompleteMoonPalace(harness.Director);
            Assert.That(
                harness.Director.ChooseFirstBranch(firstBranch),
                Is.True);
            CompleteFirstBranch(harness.Director, firstBranch);

            Assert.That(
                harness.Director.CanEnterCommonRegion,
                Is.True,
                "The optional cross route must not gate normal progress.");
            Assert.That(
                harness.Director.CanOpenCrossRouteFrom(firstBranch),
                Is.True);
            Assert.That(
                harness.Director.TryOpenCrossRouteFrom(firstBranch),
                Is.True);
            Assert.That(
                harness.Director.ActiveCrossBranch,
                Is.EqualTo(crossBranch));
            Assert.That(
                harness.Director.SecondBranchShopFrequencyMultiplier,
                Is.EqualTo(0.5f));
            Assert.That(
                harness.Director.SecondBranchBellIntervalMultiplier,
                Is.EqualTo(0.82f));

            P10StageId skippedX1 =
                crossBranch == P9BranchKind.MagpieBridge
                    ? P10StageId.MagpieBridge21
                    : P10StageId.DragonPalace21;
            P10StageId crossX2 =
                crossBranch == P9BranchKind.MagpieBridge
                    ? P10StageId.MagpieBridge22
                    : P10StageId.DragonPalace22;
            P10StageId crossX3 =
                crossBranch == P9BranchKind.MagpieBridge
                    ? P10StageId.MagpieBridge23
                    : P10StageId.DragonPalace23;

            Assert.That(
                harness.Director.CanEnterStage(skippedX1),
                Is.False);
            EnterAndComplete(harness.Director, crossX2);
            EnterAndComplete(harness.Director, crossX3);

            Assert.That(
                harness.Director.HasCompleted(skippedX1),
                Is.False);
            Assert.That(
                harness.Director.CompletedCrossBranch,
                Is.EqualTo(crossBranch));
            Assert.That(
                harness.Director.CompletedStages.Count,
                Is.EqualTo(8));
            Assert.That(
                harness.Folklore.HasBothBranchRelics,
                Is.True);
            Assert.That(
                harness.Director.CanEnterCommonRegion,
                Is.True);
            Assert.That(
                harness.Director.TryOpenCrossRouteFrom(firstBranch),
                Is.False);
            Assert.That(
                harness.Director.TryEnterCommonRegion(),
                Is.True);
        }

        [Test]
        public void Kungtteoki_SupportsDirectAndEnvironmentalDefeat()
        {
            P10StageNode2D node = CreateStageNode(
                LoadCatalog().Find(P10StageId.MoonPalace13));
            GameObject bossObject = CreateObject("Kungtteoki");
            P10KungtteokiBoss2D boss =
                bossObject.AddComponent<P10KungtteokiBoss2D>();
            GameObject[] floors = CreateTargets(
                bossObject.transform,
                "CrackedFloor");
            boss.Configure(
                node,
                null,
                System.Array.Empty<SpriteRenderer>(),
                floors,
                CreateChild(bossObject.transform, "FirstFloorMark"),
                CreateChild(bossObject.transform, "MillWeight"),
                CreateChild(bossObject.transform, "RecoveryMoonCake"));

            Assert.That(node.ExitAvailable, Is.False);
            Assert.That(boss.SupportsDirectSolution, Is.True);
            Assert.That(
                boss.SupportsToolFreeEnvironmentalSolution,
                Is.True);
            Assert.That(boss.FirstFiveSecondDemonstrationReady, Is.True);
            Assert.That(boss.BeginEncounter(), Is.True);
            boss.TickEncounter(0.1f);
            Assert.That(boss.SafeDemonstrationPerformed, Is.True);

            P10BossSolutionInput[] directInputs =
            {
                P10BossSolutionInput.BasicWeakPoint,
                P10BossSolutionInput.Bomb,
                P10BossSolutionInput.Pestle
            };
            for (int index = 0; index < directInputs.Length; index++)
            {
                Assert.That(boss.RegisterDownwardSlam(), Is.True);
                Assert.That(
                    boss.TryDirectWeakPointHit(directInputs[index]),
                    Is.True);
            }

            Assert.That(boss.IsDefeated, Is.True);
            Assert.That(node.ExitAvailable, Is.True);

            boss.ResetEncounterForTests();
            Assert.That(node.ExitAvailable, Is.False);
            Assert.That(boss.BeginEncounter(), Is.True);
            boss.TickEncounter(0.1f);
            for (int index = 0;
                 index < P10KungtteokiBoss2D.RequiredStarKnots;
                 index++)
            {
                Assert.That(
                    boss.TryBreakCrackedFloor(index),
                    Is.True);
                Assert.That(
                    boss.TryBreakCrackedFloor(index),
                    Is.False);
            }

            Assert.That(boss.IsDefeated, Is.True);
            Assert.That(node.ExitAvailable, Is.True);
        }

        [TestCase(
            P10BossKind.KnotSpider,
            P9BranchKind.MagpieBridge,
            P10StageId.MagpieBridge23,
            P10BossSolutionInput.Hook)]
        [TestCase(
            P10BossKind.DragonGatekeeper,
            P9BranchKind.DragonPalace,
            P10StageId.DragonPalace23,
            P10BossSolutionInput.Bomb)]
        public void BranchBosses_SupportDirectAndEnvironmentalDefeat(
            P10BossKind bossKind,
            P9BranchKind branch,
            P10StageId stageId,
            P10BossSolutionInput directInput)
        {
            P10StageNode2D node =
                CreateStageNode(LoadCatalog().Find(stageId));
            GameObject supportObject = CreateObject("BranchSupport");
            P10BranchSupportState2D support =
                supportObject.AddComponent<P10BranchSupportState2D>();
            GameObject bossObject = CreateObject(bossKind.ToString());
            P10BranchBoss2D boss =
                bossObject.AddComponent<P10BranchBoss2D>();
            GameObject[] targets =
                CreateTargets(bossObject.transform, "EnvironmentTarget");
            boss.Configure(
                bossKind,
                branch,
                node,
                support,
                System.Array.Empty<SpriteRenderer>(),
                targets);

            Assert.That(node.ExitAvailable, Is.False);
            Assert.That(boss.BeginEncounter(), Is.True);
            for (int index = 0;
                 index < P10BranchBoss2D.RequiredStarKnots;
                 index++)
            {
                Assert.That(boss.ExposeWeakPoint(), Is.True);
                Assert.That(
                    boss.TryDirectWeakPointHit(directInput),
                    Is.True);
            }

            Assert.That(boss.IsDefeated, Is.True);
            Assert.That(node.ExitAvailable, Is.True);

            boss.ResetEncounterForTests();
            Assert.That(node.ExitAvailable, Is.False);
            Assert.That(boss.BeginEncounter(), Is.True);
            for (int index = 0;
                 index < P10BranchBoss2D.RequiredStarKnots;
                 index++)
            {
                Assert.That(
                    boss.TryEnvironmentTarget(index),
                    Is.True);
                Assert.That(
                    boss.TryEnvironmentTarget(index),
                    Is.False);
            }

            Assert.That(boss.IsDefeated, Is.True);
            Assert.That(node.ExitAvailable, Is.True);
        }

        [Test]
        public void RouteTelemetry_UsesInclusiveThirtyToFortyMinuteGate()
        {
            Assert.That(
                P10RouteTelemetry2D.IsWithinNormalRouteTarget(1799f),
                Is.False);
            Assert.That(
                P10RouteTelemetry2D.IsWithinNormalRouteTarget(1800f),
                Is.True);
            Assert.That(
                P10RouteTelemetry2D.IsWithinNormalRouteTarget(2400f),
                Is.True);
            Assert.That(
                P10RouteTelemetry2D.IsWithinNormalRouteTarget(2401f),
                Is.False);

            GameObject telemetryObject = CreateObject("P10Telemetry");
            P10RouteTelemetry2D telemetry =
                telemetryObject.AddComponent<P10RouteTelemetry2D>();
            Assert.That(telemetry.InstrumentationReady, Is.True);
            Assert.That(telemetry.HasHumanTimingSample, Is.False);
            Assert.That(
                telemetry.HumanTimingGateRequiresPlaytest,
                Is.True);
            Assert.That(
                telemetry.HumanBranchFeelGateRequiresPlaytest,
                Is.True);

            telemetry.BeginNormalRoute();
            telemetry.RecordBreakdownForTests(
                stages: 1500f,
                transitions: 100f,
                shops: 100f,
                backtracking: 100f,
                crossRoute: 450f);
            Assert.That(telemetry.ActiveGameplaySeconds, Is.EqualTo(1800f));
            Assert.That(telemetry.CrossRouteSeconds, Is.EqualTo(450f));
            Assert.That(telemetry.LastNormalRouteWithinTarget, Is.False);
            telemetry.CompleteNormalRouteAtCommonEntry();
            Assert.That(telemetry.HasHumanTimingSample, Is.True);
            Assert.That(telemetry.LastNormalRouteWithinTarget, Is.True);

            for (int index = 0; index < 5; index++)
            {
                telemetry.RecordBranchFeelSurvey(index < 4);
            }

            Assert.That(
                telemetry.BranchFeelClearlyDifferentRate,
                Is.EqualTo(0.8f));
        }

        [Test]
        public void Contract_KeepsCorridorAndHumanGatesPending()
        {
            GameObject root = CreateObject("P10Contract");
            P10FirstBranchCampaignContract contract =
                root.AddComponent<P10FirstBranchCampaignContract>();

            Assert.That(contract.CorridorReviewPending, Is.True);
            Assert.That(
                contract.CorridorReviewNote,
                Is.EqualTo(
                    P10FirstBranchCampaignContract.CorridorReviewText));
            Assert.That(
                P10FirstBranchCampaignContract.CorridorReviewText,
                Does.Contain("corridor"));
            Assert.That(
                P10FirstBranchCampaignContract.CorridorReviewText,
                Does.Contain("follow-up"));
            Assert.That(contract.HumanTimingPlaytestPending, Is.True);
            Assert.That(
                contract.HumanBranchFeelPlaytestPending,
                Is.True);
            Assert.That(contract.CulturalReviewPending, Is.True);
        }

        [Test]
        public void StageFlow_MaruRuntimePersistsAcrossMoon11ToMoon12()
        {
            CampaignHarness campaign =
                CreateCampaignHarness(grantBothGifts: false);
            P10StageEnvironment2D moon11Environment =
                CreateStageEnvironment(P10StageId.MoonPalace11);
            P10StageEnvironment2D moon12Environment =
                CreateStageEnvironment(P10StageId.MoonPalace12);
            P10StageNode2D moon11Node =
                CreateStageNode(
                    LoadCatalog().Find(P10StageId.MoonPalace11),
                    campaign.Director,
                    moon11Environment);
            P10StageNode2D moon12Node =
                CreateStageNode(
                    LoadCatalog().Find(P10StageId.MoonPalace12),
                    campaign.Director,
                    moon12Environment);

            GameObject persistent = CreateObject("PersistentCore");
            GameObject runtime =
                CreateObject("PersistentMaruRuntime");
            runtime.transform.SetParent(persistent.transform);
            P8MaruTimeline2D timeline =
                runtime.AddComponent<P8MaruTimeline2D>();
            P8MaruPursuer2D pursuer =
                runtime.AddComponent<P8MaruPursuer2D>();
            P8MaruBiteController2D bite =
                runtime.AddComponent<P8MaruBiteController2D>();
            P8MaruStageController2D maru =
                runtime.AddComponent<P8MaruStageController2D>();
            maru.Configure(timeline, pursuer, bite, null);

            GameObject flowObject = CreateObject("P10StageFlow");
            P10StageFlowController2D flow =
                flowObject.AddComponent<P10StageFlowController2D>();
            flow.Configure(
                campaign.Director,
                new[] { moon11Node, moon12Node },
                null,
                null,
                null);

            Assert.That(
                flow.TryActivateStage(P10StageId.MoonPalace11),
                Is.True);
            Assert.That(moon11Environment.gameObject.activeSelf, Is.True);
            Assert.That(
                flow.TryCompleteActiveStage(),
                Is.True);
            Assert.That(
                flow.ActiveNode.StageId,
                Is.EqualTo(P10StageId.MoonPalace12));
            Assert.That(moon11Environment.gameObject.activeSelf, Is.False);
            Assert.That(moon12Environment.gameObject.activeSelf, Is.True);
            Assert.That(flow.MaruController, Is.SameAs(maru));
            Assert.That(flow.MaruRuntimeActiveInHierarchy, Is.True);
            Assert.That(
                flow.MaruLifecyclePersistsAcrossStages,
                Is.True);
            Assert.That(timeline.IsRunning, Is.True);

            float elapsedBeforeTick = timeline.ElapsedSeconds;
            timeline.Advance(0.25f);
            Assert.That(
                timeline.ElapsedSeconds,
                Is.GreaterThan(elapsedBeforeTick));
            pursuer.BeginHunt();
            Assert.That(pursuer.IsHunting, Is.True);
            pursuer.StopHunt();

            runtime.SetActive(false);

            Assert.That(flow.MaruRuntimeActiveInHierarchy, Is.False);
            Assert.That(
                flow.MaruLifecyclePersistsAcrossStages,
                Is.False);
        }

        private P10CampaignCatalog LoadCatalog()
        {
            P10CampaignCatalog catalog =
                AssetDatabase.LoadAssetAtPath<P10CampaignCatalog>(
                    CatalogPath);
            Assert.That(
                catalog,
                Is.Not.Null,
                $"P10 catalog is missing: {CatalogPath}");
            return catalog;
        }

        private CampaignHarness CreateCampaignHarness(
            bool grantBothGifts)
        {
            GameObject root = CreateObject("P10CampaignHarness");
            P9FolkloreChainState2D folklore =
                root.AddComponent<P9FolkloreChainState2D>();
            folklore.Configure(grantBothGifts, grantBothGifts);
            P10RouteTelemetry2D telemetry =
                root.AddComponent<P10RouteTelemetry2D>();
            P10BranchSupportState2D support =
                root.AddComponent<P10BranchSupportState2D>();
            P10CampaignDirector2D director =
                root.AddComponent<P10CampaignDirector2D>();
            director.Configure(
                LoadCatalog(),
                folklore,
                telemetry,
                support);
            return new CampaignHarness(director, folklore);
        }

        private P10StageNode2D CreateStageNode(
            P10StageDefinition definition)
        {
            return CreateStageNode(definition, null, null);
        }

        private P10StageNode2D CreateStageNode(
            P10StageDefinition definition,
            P10CampaignDirector2D director,
            P10StageEnvironment2D environment)
        {
            Assert.That(definition, Is.Not.Null);
            GameObject root =
                CreateObject($"Node_{definition.StageId}");
            P10StageNode2D node =
                root.AddComponent<P10StageNode2D>();
            node.Configure(definition, director, environment);
            return node;
        }

        private P10StageEnvironment2D CreateStageEnvironment(
            P10StageId stageId)
        {
            GameObject root =
                CreateObject($"Environment_{stageId}");
            P10StageEnvironment2D environment =
                root.AddComponent<P10StageEnvironment2D>();
            environment.Configure(
                stageId,
                root,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                System.Array.Empty<Transform>());
            return environment;
        }

        private GameObject CreateObject(string name)
        {
            GameObject value = new GameObject(name);
            created.Add(value);
            return value;
        }

        private static GameObject CreateChild(
            Transform parent,
            string name)
        {
            GameObject value = new GameObject(name);
            value.transform.SetParent(parent);
            return value;
        }

        private static GameObject[] CreateTargets(
            Transform parent,
            string prefix)
        {
            GameObject[] targets =
                new GameObject[P10BranchBoss2D.RequiredStarKnots];
            for (int index = 0; index < targets.Length; index++)
            {
                targets[index] =
                    CreateChild(parent, $"{prefix}_{index}");
            }

            return targets;
        }

        private static void AssertRegionStages(
            P10CampaignCatalog catalog,
            RoomRegion region,
            P9BranchKind branch)
        {
            P10StageDefinition[] stages = catalog.Stages
                .Where(stage => stage.Region == region)
                .ToArray();
            Assert.That(stages.Length, Is.EqualTo(3));
            CollectionAssert.AreEquivalent(
                new[]
                {
                    P6StageSlot.X1,
                    P6StageSlot.X2,
                    P6StageSlot.X3
                },
                stages.Select(stage => stage.StageSlot));
            Assert.That(
                stages.All(stage => stage.Branch == branch),
                Is.True);
            Assert.That(
                stages.All(stage =>
                    stage.RecommendedMinutesMin > 0
                    && stage.RecommendedMinutesMax
                        >= stage.RecommendedMinutesMin),
                Is.True);
        }

        private static void CompleteMoonPalace(
            P10CampaignDirector2D director)
        {
            EnterAndComplete(director, P10StageId.MoonPalace11);
            EnterAndComplete(director, P10StageId.MoonPalace12);
            EnterAndComplete(director, P10StageId.MoonPalace13);
            Assert.That(
                director.Phase,
                Is.EqualTo(P10CampaignPhase.BranchChoice));
        }

        private static void CompleteFirstBranch(
            P10CampaignDirector2D director,
            P9BranchKind branch)
        {
            if (branch == P9BranchKind.MagpieBridge)
            {
                EnterAndComplete(
                    director,
                    P10StageId.MagpieBridge21);
                EnterAndComplete(
                    director,
                    P10StageId.MagpieBridge22);
                EnterAndComplete(
                    director,
                    P10StageId.MagpieBridge23);
            }
            else
            {
                EnterAndComplete(
                    director,
                    P10StageId.DragonPalace21);
                EnterAndComplete(
                    director,
                    P10StageId.DragonPalace22);
                EnterAndComplete(
                    director,
                    P10StageId.DragonPalace23);
            }
        }

        private static void EnterAndComplete(
            P10CampaignDirector2D director,
            P10StageId stageId)
        {
            Assert.That(
                director.TryEnterStage(stageId),
                Is.True,
                $"Failed to enter {stageId}.");
            Assert.That(
                director.TryCompleteCurrentStage(),
                Is.True,
                $"Failed to complete {stageId}.");
        }

        private static P9BranchKind Opposite(P9BranchKind branch)
        {
            return branch == P9BranchKind.MagpieBridge
                ? P9BranchKind.DragonPalace
                : P9BranchKind.MagpieBridge;
        }

        private static bool StageBelongsToBranch(
            P10StageId stage,
            P9BranchKind branch)
        {
            if (branch == P9BranchKind.MagpieBridge)
            {
                return stage == P10StageId.MagpieBridge21
                    || stage == P10StageId.MagpieBridge22
                    || stage == P10StageId.MagpieBridge23;
            }

            return stage == P10StageId.DragonPalace21
                || stage == P10StageId.DragonPalace22
                || stage == P10StageId.DragonPalace23;
        }

        private readonly struct CampaignHarness
        {
            public CampaignHarness(
                P10CampaignDirector2D director,
                P9FolkloreChainState2D folklore)
            {
                Director = director;
                Folklore = folklore;
            }

            public P10CampaignDirector2D Director { get; }
            public P9FolkloreChainState2D Folklore { get; }
        }
    }
}

#endif
