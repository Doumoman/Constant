#if LEGACY_DISABLED
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Campaign.P11;
using StarNight.Folklore.P9;
using StarNight.Generation.P6;
using StarNight.Maru.P8;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Tests.EditMode
{
    public sealed class P11CommonRegionsTests
    {
        private readonly List<Object> created = new List<Object>();

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
        public void Catalog_HasNineStagesThreeRegionTripletsAndRouteProof()
        {
            P11CampaignCatalog catalog = CreateCatalog();
            P11CampaignRouteProof proof =
                P11CampaignRouteProof.Evaluate(catalog);

            Assert.That(catalog.ValidateCatalog(), Is.Empty);
            Assert.That(catalog.StageCount, Is.EqualTo(9));
            Assert.That(
                catalog.Stages.Select(stage => stage.StageId).Distinct()
                    .Count(),
                Is.EqualTo(9));
            Assert.That(
                catalog.CountForRegion(RoomRegion.StarPostOffice),
                Is.EqualTo(3));
            Assert.That(
                catalog.CountForRegion(RoomRegion.SunriseGarden),
                Is.EqualTo(3));
            Assert.That(
                catalog.CountForRegion(RoomRegion.PolarisObservatory),
                Is.EqualTo(3));
            Assert.That(proof.AllStagesPresent, Is.True);
            Assert.That(proof.RegionTripletsValid, Is.True);
            Assert.That(proof.StageRhythmValid, Is.True);
            Assert.That(proof.MainPathToolFree, Is.True);
            Assert.That(
                proof.OptionalStoryNeverGatesExit,
                Is.True);
            Assert.That(proof.EndingsReachable, Is.True);
            Assert.That(proof.Passed, Is.True);
        }

        [Test]
        public void Director_AdvancesLinearlyWithoutOptionalStory()
        {
            CampaignHarness harness = CreateCampaignHarness();
            harness.Director.AcceptCommonRegionForTests();

            P11StageId[] route = Route();
            for (int index = 0; index < route.Length - 1; index++)
            {
                Assert.That(
                    harness.Director.TryEnterStage(route[index]),
                    Is.True,
                    $"Could not enter {route[index]}.");
                Assert.That(
                    harness.Director.TryCompleteCurrentStage(),
                    Is.True,
                    $"Could not complete {route[index]}.");
            }

            Assert.That(
                harness.Director.TryEnterStage(route[route.Length - 1]),
                Is.True);
            Assert.That(harness.Story.HasNaraeLetter, Is.False);
            Assert.That(harness.Story.HasSunEmber, Is.False);
            Assert.That(
                harness.Director.MainProgressRequiresOptionalStory,
                Is.False);
            Assert.That(
                harness.Director
                    .NormalEndingAvailableWithoutLetterOrEmber,
                Is.True);
            Assert.That(
                harness.Director.CanResolveEnding(
                    P11EndingKind.Normal),
                Is.True);
            Assert.That(
                harness.Director.CanResolveEnding(
                    P11EndingKind.EnvironmentalDisarm),
                Is.True);
            Assert.That(
                harness.Director.CanResolveEnding(
                    P11EndingKind.Memory),
                Is.False);
        }

        [TestCase(P9BranchKind.MagpieBridge)]
        [TestCase(P9BranchKind.DragonPalace)]
        public void Story_LetterNeedsPopoAndOneRelic_CoordinatesNeedBoth(
            P9BranchKind firstRelic)
        {
            StoryHarness harness = CreateStoryHarness();

            Assert.That(
                harness.Story.TryOpenNaraeLetterDrawer(),
                Is.False);
            Assert.That(
                harness.Folklore.GrantBossRelic(firstRelic),
                Is.True);
            Assert.That(harness.Story.HasAnyBranchRelic, Is.True);
            Assert.That(
                harness.Story.TryOpenNaraeLetterDrawer(),
                Is.False,
                "A relic cannot replace defeating Popo.");

            Assert.That(harness.Story.MarkPopoDefeated(), Is.True);
            Assert.That(
                harness.Story.TryOpenNaraeLetterDrawer(),
                Is.True);
            Assert.That(harness.Story.HasNaraeLetter, Is.True);
            Assert.That(
                harness.Story.TryOpenNaraeLetterDrawer(),
                Is.False);
            Assert.That(
                harness.Story.TryRevealDawnStarCoordinates(),
                Is.False,
                "One branch relic must not reveal the coordinates.");

            Assert.That(
                harness.Folklore.GrantBossRelic(
                    Opposite(firstRelic)),
                Is.True);
            Assert.That(harness.Story.HasBothBranchRelics, Is.True);
            Assert.That(
                harness.Story.TryRevealDawnStarCoordinates(),
                Is.True);
            Assert.That(
                harness.Story.HasDawnStarCoordinates,
                Is.True);
        }

        [Test]
        public void CrowNest_UsesLetterWaterAndHook_ThenUnlocksEmber()
        {
            StoryHarness harness = CreateStoryHarness();
            P11YoungCrowEvent2D crow =
                Track("YoungCrow").AddComponent<P11YoungCrowEvent2D>();
            P11CrowNestEvent2D nest =
                Track("CrowNest").AddComponent<P11CrowNestEvent2D>();
            P11SunEmberPickup2D ember =
                Track("SunEmber").AddComponent<P11SunEmberPickup2D>();
            crow.Configure(
                harness.Story,
                Track("LetterSealCue"),
                Track("CrowGazeCue"),
                Track("TrustedGuide"),
                Track("NestRoute"));
            nest.Configure(
                harness.Story,
                Track("HotRoot"),
                Track("SealedRing"),
                Track("RestoredNest"),
                Track("BossSupport"));
            ember.Configure(
                harness.Story,
                Track("EmberVisual"),
                Track("BellSocketCue"));

            Assert.That(crow.TryPresentLetter(), Is.False);
            Assert.That(nest.RequiresWaterAndHook, Is.True);
            Assert.That(harness.Story.CrowNestRestored, Is.False);

            harness.Folklore.GrantBossRelic(
                P9BranchKind.MagpieBridge);
            harness.Story.MarkPopoDefeated();
            Assert.That(
                harness.Story.TryOpenNaraeLetterDrawer(),
                Is.True);
            Assert.That(crow.TryPresentLetter(), Is.True);
            Assert.That(
                harness.Story.YoungCrowTrustedRani,
                Is.True);

            Assert.That(nest.ApplyWater(), Is.True);
            Assert.That(harness.Story.NestRootCooled, Is.True);
            Assert.That(harness.Story.CrowNestRestored, Is.False);
            Assert.That(ember.TryCollect(), Is.False);

            Assert.That(nest.ApplyHook(), Is.True);
            Assert.That(harness.Story.NestSealReleased, Is.True);
            Assert.That(harness.Story.CrowNestRestored, Is.True);
            Assert.That(ember.TryCollect(), Is.False);

            Assert.That(
                harness.Story.MarkSunFlowerDefeated(),
                Is.True);
            Assert.That(ember.TryCollect(), Is.True);
            Assert.That(harness.Story.HasSunEmber, Is.True);
            Assert.That(ember.TryCollect(), Is.False);
        }

        [Test]
        public void BellPattern_RequiresExactShortShortLongSequence()
        {
            var pattern = new P11BellPattern();

            Assert.That(pattern.Push(P8BellSignal.Short), Is.False);
            Assert.That(pattern.Progress, Is.EqualTo(1));
            Assert.That(pattern.Push(P8BellSignal.Long), Is.False);
            Assert.That(pattern.Progress, Is.Zero);
            Assert.That(pattern.Completed, Is.False);

            Assert.That(pattern.Push(P8BellSignal.Short), Is.False);
            Assert.That(pattern.Push(P8BellSignal.Short), Is.False);
            Assert.That(pattern.Push(P8BellSignal.Long), Is.True);
            Assert.That(pattern.Completed, Is.True);
            CollectionAssert.AreEqual(
                new[]
                {
                    P8BellSignal.Short,
                    P8BellSignal.Short,
                    P8BellSignal.Long
                },
                pattern.Pattern);
        }

        [TestCase(P11EndingKind.Normal)]
        [TestCase(P11EndingKind.EnvironmentalDisarm)]
        public void Director_NormalAndEnvironmentalEndingsNeedNoStory(
            P11EndingKind ending)
        {
            CampaignHarness harness = CreateCampaignHarness();
            AdvanceDirectorToFinal(harness.Director);

            Assert.That(harness.Story.HasNaraeLetter, Is.False);
            Assert.That(harness.Story.HasSunEmber, Is.False);
            Assert.That(
                harness.Director.TryResolveEnding(ending),
                Is.True);
            Assert.That(harness.Director.Ending, Is.EqualTo(ending));
            Assert.That(harness.Director.HasEnded, Is.True);
            Assert.That(
                harness.Director.CompletedStages.Count,
                Is.EqualTo(9));
        }

        [Test]
        public void Director_MemoryEndingNeedsCompletedMemoryRoute()
        {
            CampaignHarness harness = CreateCampaignHarness();
            AdvanceDirectorToFinal(harness.Director);

            Assert.That(
                harness.Director.TryResolveEnding(
                    P11EndingKind.Memory),
                Is.False);
            harness.Story.GrantMemoryRouteItemsForTests();
            Assert.That(
                harness.Story.TryLightFirstMemoryBell(),
                Is.True);
            Assert.That(
                harness.Story.MarkNaraeBellPatternCompleted(),
                Is.True);
            Assert.That(
                harness.Story.TryCompleteMemoryRoute(),
                Is.True);
            Assert.That(
                harness.Director.TryResolveEnding(
                    P11EndingKind.Memory),
                Is.True);
            Assert.That(
                harness.Director.Ending,
                Is.EqualTo(P11EndingKind.Memory));
        }

        [TestCase(
            P11BossKind.Popo,
            P11BossSolutionInput.ParcelReflection)]
        [TestCase(
            P11BossKind.SunFlower,
            P11BossSolutionInput.CrossedLightAndShadow)]
        public void RegionalBosses_SupportThreeKnotDirectAndEnvironment(
            P11BossKind bossKind,
            P11BossSolutionInput environmentInput)
        {
            StoryHarness story = CreateStoryHarness();
            P11RegionalBoss2D boss =
                Track($"{bossKind}_Boss")
                    .AddComponent<P11RegionalBoss2D>();
            GameObject[] targets =
            {
                Track("EnvironmentTarget_0"),
                Track("EnvironmentTarget_1"),
                Track("EnvironmentTarget_2")
            };
            boss.Configure(
                bossKind,
                null,
                story.Story,
                System.Array.Empty<SpriteRenderer>(),
                targets);

            Assert.That(boss.BeginEncounter(), Is.True);
            Assert.That(boss.AdvanceSafeDemonstration(), Is.True);
            for (int index = 0;
                 index < P11RegionalBoss2D.RequiredStarKnots;
                 index++)
            {
                Assert.That(boss.ExposeWeakPoint(), Is.True);
                Assert.That(
                    boss.TryDirectWeakPointHit(
                        P11BossSolutionInput.BasicWeakPoint),
                    Is.True);
            }

            Assert.That(boss.IsDefeated, Is.True);
            Assert.That(boss.RemainingStarKnots, Is.Zero);

            boss.ResetEncounterForTests();
            Assert.That(boss.BeginEncounter(), Is.True);
            for (int index = 0;
                 index < P11RegionalBoss2D.RequiredStarKnots;
                 index++)
            {
                Assert.That(
                    boss.TryEnvironmentTarget(
                        index,
                        environmentInput),
                    Is.True);
                Assert.That(
                    boss.TryEnvironmentTarget(
                        index,
                        environmentInput),
                    Is.False);
            }

            Assert.That(boss.IsDefeated, Is.True);
            Assert.That(
                boss.EnvironmentProgress,
                Is.EqualTo(P11RegionalBoss2D.RequiredStarKnots));
        }

        [TestCase(P11EndingKind.Normal)]
        [TestCase(P11EndingKind.EnvironmentalDisarm)]
        public void Maru_RequiresFiveKnotsOrFivePillars(
            P11EndingKind ending)
        {
            FinalBossHarness harness = CreateFinalBossHarness();

            Assert.That(harness.Boss.BeginEncounter(), Is.True);
            Assert.That(
                harness.Boss.AdvanceSafeDemonstration(),
                Is.True);
            if (ending == P11EndingKind.Normal)
            {
                for (int index = 0;
                     index < P11MaruFinalBoss2D.RequiredStarKnots;
                     index++)
                {
                    Assert.That(
                        harness.Boss.ExposeWeakPoint(),
                        Is.True);
                    Assert.That(
                        harness.Boss.TryDirectStarKnotHit(),
                        Is.True);
                }
            }
            else
            {
                for (int index = 0;
                     index
                        < P11MaruFinalBoss2D.RequiredCommandPillars;
                     index++)
                {
                    Assert.That(
                        harness.Boss.TryBreakCommandPillar(index),
                        Is.True);
                    Assert.That(
                        harness.Boss.TryBreakCommandPillar(index),
                        Is.False);
                }
            }

            Assert.That(harness.Boss.IsDefeated, Is.True);
            Assert.That(
                harness.Director.Ending,
                Is.EqualTo(ending));
            Assert.That(harness.Director.HasEnded, Is.True);
        }

        [Test]
        public void Maru_MemoryRouteNeedsItemsAndExactBellPattern()
        {
            FinalBossHarness harness = CreateFinalBossHarness();

            Assert.That(harness.Boss.TryLightFirstMemoryBell(), Is.False);
            Assert.That(
                harness.Boss.TryCompleteMemoryRoute(),
                Is.False);
            harness.Story.GrantMemoryRouteItemsForTests();
            Assert.That(harness.Boss.TryLightFirstMemoryBell(), Is.True);
            Assert.That(
                harness.Boss.TryRingMemoryBell(P8BellSignal.Short),
                Is.False);
            Assert.That(
                harness.Boss.TryRingMemoryBell(P8BellSignal.Long),
                Is.False);
            Assert.That(
                harness.Boss.MemoryPatternProgress,
                Is.Zero);
            Assert.That(
                harness.Boss.TryRingMemoryBell(P8BellSignal.Short),
                Is.False);
            Assert.That(
                harness.Boss.TryRingMemoryBell(P8BellSignal.Short),
                Is.False);
            Assert.That(
                harness.Boss.TryRingMemoryBell(P8BellSignal.Long),
                Is.True);
            Assert.That(
                harness.Story.NaraeBellPatternCompleted,
                Is.True);
            Assert.That(
                harness.Boss.TryCompleteMemoryRoute(),
                Is.True);
            Assert.That(harness.Boss.IsDefeated, Is.True);
            Assert.That(
                harness.Director.Ending,
                Is.EqualTo(P11EndingKind.Memory));
        }

        [Test]
        public void Telemetry_RequiresFiveUniqueHumanSamplesAtEightyPercent()
        {
            P11ComprehensionTelemetry2D telemetry =
                Track("Telemetry")
                    .AddComponent<P11ComprehensionTelemetry2D>();

            Assert.That(
                telemetry.MinimumHumanSamples,
                Is.EqualTo(5));
            for (int index = 0; index < 4; index++)
            {
                Assert.That(
                    telemetry.TryRecordSession(
                        $"human-{index}",
                        true,
                        true,
                        true,
                        true),
                    Is.True);
            }

            Assert.That(telemetry.SampleCount, Is.EqualTo(4));
            Assert.That(telemetry.HasMinimumHumanSamples, Is.False);
            Assert.That(telemetry.LetterUseGatePassed, Is.False);
            Assert.That(telemetry.SunEmberUseGatePassed, Is.False);
            Assert.That(telemetry.CoreTragedyGatePassed, Is.False);
            Assert.That(telemetry.MaruBasicsGatePassed, Is.False);
            Assert.That(telemetry.HumanGatePending, Is.True);
            Assert.That(
                telemetry.TryRecordSession(
                    " human-0 ",
                    true,
                    true,
                    true,
                    true),
                Is.False);
            Assert.That(telemetry.SampleCount, Is.EqualTo(4));

            Assert.That(
                telemetry.TryRecordSession(
                    "human-4",
                    false,
                    false,
                    false,
                    false),
                Is.True);
            Assert.That(telemetry.SampleCount, Is.EqualTo(5));
            Assert.That(
                telemetry.LetterUseInferenceRate,
                Is.EqualTo(0.8f));
            Assert.That(
                telemetry.SunEmberUseInferenceRate,
                Is.EqualTo(0.8f));
            Assert.That(
                telemetry.CoreTragedyUnderstandingRate,
                Is.EqualTo(0.8f));
            Assert.That(
                telemetry.MaruBasicsUnderstandingRate,
                Is.EqualTo(0.8f));
            Assert.That(telemetry.AllHumanGatesPassed, Is.True);
            Assert.That(telemetry.HumanGatePending, Is.False);
            Assert.That(
                telemetry.AutomatedStructureCanSatisfyHumanGate,
                Is.False);
            Assert.That(
                telemetry.CanAutomatedStructureSatisfyHumanGate(true),
                Is.False);

            telemetry.ResetForTests(6);
            Assert.That(telemetry.SampleCount, Is.Zero);
            Assert.That(telemetry.MinimumHumanSamples, Is.EqualTo(6));
            Assert.That(telemetry.HumanGatePending, Is.True);
        }

        private CampaignHarness CreateCampaignHarness()
        {
            GameObject root = Track("P11CampaignHarness");
            P11StoryState2D story =
                root.AddComponent<P11StoryState2D>();
            P11ComprehensionTelemetry2D telemetry =
                root.AddComponent<P11ComprehensionTelemetry2D>();
            P11CampaignDirector2D director =
                root.AddComponent<P11CampaignDirector2D>();
            story.Configure(null);
            director.Configure(
                CreateCatalog(),
                null,
                story,
                telemetry);
            return new CampaignHarness(director, story, telemetry);
        }

        private StoryHarness CreateStoryHarness()
        {
            GameObject root = Track("P11StoryHarness");
            P9FolkloreChainState2D folklore =
                root.AddComponent<P9FolkloreChainState2D>();
            P11StoryState2D story =
                root.AddComponent<P11StoryState2D>();
            folklore.Configure(false, false);
            story.Configure(folklore);
            return new StoryHarness(story, folklore);
        }

        private FinalBossHarness CreateFinalBossHarness()
        {
            CampaignHarness campaign = CreateCampaignHarness();
            P11StageNode2D[] nodes =
                new P11StageNode2D[campaign.Director.Catalog.StageCount];
            for (int index = 0; index < nodes.Length; index++)
            {
                P11StageDefinition definition =
                    campaign.Director.Catalog.Stages[index];
                GameObject nodeObject =
                    Track($"Node_{definition.StageId}");
                nodes[index] =
                    nodeObject.AddComponent<P11StageNode2D>();
                nodes[index].Configure(
                    definition,
                    campaign.Director,
                    null);
            }

            P11StageFlowController2D flow =
                Track("P11Flow")
                    .AddComponent<P11StageFlowController2D>();
            flow.Configure(
                campaign.Director,
                nodes,
                null,
                null,
                null,
                null);
            Assert.That(flow.BeginAtCommonRegionForTests(), Is.True);
            for (int index = 0; index < nodes.Length - 1; index++)
            {
                P11StageId completingStage = flow.ActiveNode.StageId;
                if (flow.ActiveNode.Definition.IsBossStage)
                {
                    flow.ActiveNode.MarkBossDefeated();
                }

                Assert.That(
                    flow.TryCompleteActiveStage(),
                    Is.True,
                    $"Flow failed after {completingStage}.");
            }

            Assert.That(
                flow.ActiveNode.StageId,
                Is.EqualTo(P11StageId.PolarisObservatory53));
            P11MaruFinalBoss2D boss =
                Track("MaruFinalBoss")
                    .AddComponent<P11MaruFinalBoss2D>();
            boss.Configure(
                flow,
                campaign.Story,
                System.Array.Empty<SpriteRenderer>(),
                System.Array.Empty<GameObject>(),
                null,
                null,
                null);
            return new FinalBossHarness(
                campaign.Director,
                campaign.Story,
                flow,
                boss);
        }

        private P11CampaignCatalog CreateCatalog()
        {
            P11CampaignCatalog catalog =
                ScriptableObject.CreateInstance<P11CampaignCatalog>();
            created.Add(catalog);
            P11StageId[] ids = Route();
            var definitions = new P11StageDefinition[ids.Length];
            for (int index = 0; index < definitions.Length; index++)
            {
                RoomRegion region = index < 3
                    ? RoomRegion.StarPostOffice
                    : index < 6
                        ? RoomRegion.SunriseGarden
                        : RoomRegion.PolarisObservatory;
                P6StageSlot slot =
                    (P6StageSlot)(index % 3 + 1);
                P11BossKind boss = index == 2
                    ? P11BossKind.Popo
                    : index == 5
                        ? P11BossKind.SunFlower
                        : index == 8
                            ? P11BossKind.Maru
                            : P11BossKind.None;
                definitions[index] = new P11StageDefinition();
                definitions[index].Configure(
                    ids[index],
                    ids[index].ToString(),
                    region,
                    slot,
                    slot == P6StageSlot.X3
                        ? P6StageArchetype.Chase
                        : P6StageArchetype.Traverse,
                    region == RoomRegion.PolarisObservatory
                        ? P11TraversalAxis.Circuit
                        : P11TraversalAxis.Horizontal,
                    P11StageMechanics.None,
                    boss == P11BossKind.None
                        ? P11StageGuarantees.Landmark
                        : P11StageGuarantees.Landmark
                            | P11StageGuarantees.Boss,
                    boss,
                    $"Reach the exit of {ids[index]}.",
                    2,
                    5);
            }

            catalog.Configure(definitions);
            return catalog;
        }

        private GameObject Track(string name)
        {
            var value = new GameObject(name);
            created.Add(value);
            return value;
        }

        private static void AdvanceDirectorToFinal(
            P11CampaignDirector2D director)
        {
            director.AcceptCommonRegionForTests();
            P11StageId[] route = Route();
            for (int index = 0; index < route.Length - 1; index++)
            {
                Assert.That(director.TryEnterStage(route[index]), Is.True);
                Assert.That(director.TryCompleteCurrentStage(), Is.True);
            }

            Assert.That(
                director.TryEnterStage(route[route.Length - 1]),
                Is.True);
        }

        private static P11StageId[] Route()
        {
            return new[]
            {
                P11StageId.StarPostOffice31,
                P11StageId.StarPostOffice32,
                P11StageId.StarPostOffice33,
                P11StageId.SunriseGarden41,
                P11StageId.SunriseGarden42,
                P11StageId.SunriseGarden43,
                P11StageId.PolarisObservatory51,
                P11StageId.PolarisObservatory52,
                P11StageId.PolarisObservatory53
            };
        }

        private static P9BranchKind Opposite(P9BranchKind branch)
        {
            return branch == P9BranchKind.MagpieBridge
                ? P9BranchKind.DragonPalace
                : P9BranchKind.MagpieBridge;
        }

        private readonly struct CampaignHarness
        {
            public CampaignHarness(
                P11CampaignDirector2D director,
                P11StoryState2D story,
                P11ComprehensionTelemetry2D telemetry)
            {
                Director = director;
                Story = story;
                Telemetry = telemetry;
            }

            public P11CampaignDirector2D Director { get; }
            public P11StoryState2D Story { get; }
            public P11ComprehensionTelemetry2D Telemetry { get; }
        }

        private readonly struct StoryHarness
        {
            public StoryHarness(
                P11StoryState2D story,
                P9FolkloreChainState2D folklore)
            {
                Story = story;
                Folklore = folklore;
            }

            public P11StoryState2D Story { get; }
            public P9FolkloreChainState2D Folklore { get; }
        }

        private readonly struct FinalBossHarness
        {
            public FinalBossHarness(
                P11CampaignDirector2D director,
                P11StoryState2D story,
                P11StageFlowController2D flow,
                P11MaruFinalBoss2D boss)
            {
                Director = director;
                Story = story;
                Flow = flow;
                Boss = boss;
            }

            public P11CampaignDirector2D Director { get; }
            public P11StoryState2D Story { get; }
            public P11StageFlowController2D Flow { get; }
            public P11MaruFinalBoss2D Boss { get; }
        }
    }
}

#endif
