#if LEGACY_DISABLED
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Campaign.P11;
using StarNight.Campaign.P12;
using StarNight.Folklore.P9;
using StarNight.Generation.P6;
using StarNight.Maru.P8;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Tests.EditMode
{
    public sealed class P12StarlessSeaChallengeTests
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
        public void Catalog_StandardDefinitionsPassRouteProof()
        {
            P12ChallengeCatalog catalog = CreateP12Catalog();
            P12ChallengeRouteProof proof =
                P12ChallengeRouteProof.Evaluate(catalog);

            Assert.That(catalog.StageCount, Is.EqualTo(12));
            Assert.That(catalog.ValidateCatalog(), Is.Empty);
            Assert.That(proof.Passed, Is.True);
        }

        [Test]
        public void GateA_NoNewRules()
        {
            P12StageDefinition[] definitions =
                P12ChallengeCatalogDefaults.CreateStandardDefinitions();

            Assert.That(
                definitions.All(stage => !stage.IntroducesNewMechanic),
                Is.True);
            Assert.That(
                definitions.All(stage =>
                    (stage.Mechanics & ~P12ComboRules.AllKnownMechanics)
                    == P12StageMechanics.None),
                Is.True);

            P12ChallengeDirector2D director =
                Track("GateADirector")
                    .AddComponent<P12ChallengeDirector2D>();
            Assert.That(director.AddsNewRules, Is.False);
        }

        [Test]
        public void Entry_RequiresAllFourConditions()
        {
            Assert.That(
                P12EntryRequirements.IsSatisfied(
                    true, true, true, true),
                Is.True);
            Assert.That(
                P12EntryRequirements.IsSatisfied(
                    false, true, true, true),
                Is.False);
            Assert.That(
                P12EntryRequirements.IsSatisfied(
                    true, false, true, true),
                Is.False);
            Assert.That(
                P12EntryRequirements.IsSatisfied(
                    true, true, false, true),
                Is.False);
            Assert.That(
                P12EntryRequirements.IsSatisfied(
                    true, true, true, false),
                Is.False);
        }

        [Test]
        public void GateC_SecondFailureEndsChallengeAndPreservesMemoryEnding()
        {
            ChallengeHarness harness = CreateChallengeHarness();
            P12ChallengeDirector2D director = harness.Director;

            Assert.That(director.CanEnterChallenge, Is.True);
            Assert.That(director.TryAcceptP11Handoff(), Is.True);
            Assert.That(
                director.TryEnterStage(P12StageId.StarlessSea01),
                Is.True);
            Assert.That(
                director.TryFailCurrentStage("first"),
                Is.True);
            Assert.That(director.HasEnded, Is.False);
            Assert.That(
                director.TryEnterStage(P12StageId.StarlessSea01),
                Is.True);
            Assert.That(
                director.TryFailCurrentStage("second"),
                Is.True);
            Assert.That(
                director.Outcome,
                Is.EqualTo(P12ChallengeOutcome.EndedBySecondFailure));
            Assert.That(director.HasEnded, Is.True);
            Assert.That(
                director.ChallengeFailurePreservesMainEndings,
                Is.True);
            Assert.That(
                harness.P11Director.Ending,
                Is.EqualTo(P11EndingKind.Memory));
            Assert.That(harness.P11Director.HasEnded, Is.True);
            Assert.That(harness.Story.MemoryRouteCompleted, Is.True);
        }

        [Test]
        public void Crystal_ValueDoubles()
        {
            Assert.That(
                P12ReturnCrystalRules.CrystalValueFor(
                    P12ReturnCrystalRules.BigGoldValue),
                Is.EqualTo(P12ReturnCrystalRules.StandardCrystalValue));
            Assert.That(
                P12ReturnCrystalRules.CrystalValueFor(3),
                Is.EqualTo(6));
            Assert.That(
                P12ReturnCrystalRules.CrystalValueFor(1),
                Is.EqualTo(2));
            Assert.That(
                P12ReturnCrystalRules.CrystalValueFor(0),
                Is.EqualTo(0));
        }

        [Test]
        public void ComboRules_EveryStagePairingIsApproved()
        {
            P12StageDefinition[] definitions =
                P12ChallengeCatalogDefaults.CreateStandardDefinitions();
            foreach (P12StageDefinition stage in definitions)
            {
                bool unapproved =
                    P12ComboRules.TryFindUnapprovedPairing(
                        stage.Mechanics,
                        out P12StageMechanics first,
                        out P12StageMechanics second);
                Assert.That(
                    unapproved,
                    Is.False,
                    $"{stage.StageId}: {first} + {second}");
            }

            Assert.That(
                P12ComboRules.IsApprovedPairing(
                    P12StageMechanics.Floodgate,
                    P12StageMechanics.GravityDial),
                Is.False);
            Assert.That(
                P12ChallengeRouteProof.Evaluate(CreateP12Catalog())
                    .ApprovedPairingsValid,
                Is.True);
        }

        [Test]
        public void Catalog_FirstPairingStagesDemonstrateSafely()
        {
            P12StageDefinition[] definitions =
                P12ChallengeCatalogDefaults.CreateStandardDefinitions();
            P12StageId[] firstPairingStages =
            {
                P12StageId.StarlessSea10,
                P12StageId.StarlessSea11
            };
            foreach (P12StageId stageId in firstPairingStages)
            {
                Assert.That(
                    definitions
                        .Single(stage => stage.StageId == stageId)
                        .FirstPairingSafeDemonstration,
                    Is.True,
                    $"{stageId} shows its pairing for the first time.");
            }

            Assert.That(
                P12ComboRules
                    .RequiresSafeDemonstrationForFirstPairing,
                Is.True);
            Assert.That(
                P12ChallengeRouteProof.Evaluate(CreateP12Catalog())
                    .SafeDemonstrationValid,
                Is.True);
        }

        [Test]
        public void Statue_ChainImpactNeedsTwoStagesWhenOptedIn()
        {
            Assert.That(
                P12ComboRules.ForbidsAutoChainStatueDestruction,
                Is.True);
            GameObject statueObject = Track("HomecomingStatue");
            Rigidbody2D body =
                statueObject.AddComponent<Rigidbody2D>();
            BoxCollider2D statueCollider =
                statueObject.AddComponent<BoxCollider2D>();
            P8HomecomingStatue2D statue =
                statueObject.AddComponent<P8HomecomingStatue2D>();

            statue.Configure(null, null, body, statueCollider);
            Assert.That(
                statue.RequireTwoStagesForChainImpact,
                Is.False);
            Assert.That(
                statue.ApplyImpact(P8StatueImpactKind.Bomb),
                Is.True);
            Assert.That(
                statue.State,
                Is.EqualTo(P8StatueState.Destroyed));

            statue.SetRequireTwoStagesForChainImpact(true);
            statue.ResetStatueForTests();
            Assert.That(
                statue.ApplyImpact(P8StatueImpactKind.Bomb),
                Is.True);
            Assert.That(
                statue.State,
                Is.EqualTo(P8StatueState.Cracked));
            Assert.That(
                statue.ApplyImpact(P8StatueImpactKind.Bomb),
                Is.True);
            Assert.That(
                statue.State,
                Is.EqualTo(P8StatueState.Destroyed));
        }

        [Test]
        public void Store_RoundTripsInIsolatedFolder()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "StarNightP12Tests_"
                + System.Guid.NewGuid().ToString("N"));
            try
            {
                P12PersistentStore.SetStorageRootForTests(root);
                P12ChallengeRecordData record =
                    P12ChallengeRecordData.CreateEmpty();
                record.RegisterAttempt();
                Assert.That(
                    record.RegisterProgress(
                        P12ChallengeSegment.FirstSea,
                        P12StageId.StarlessSea02),
                    Is.True);
                Assert.That(
                    record.RegisterFailure(
                        P12ChallengeFailureCause.MaruCaught),
                    Is.True);
                Assert.That(
                    P12PersistentStore.SaveChallengeRecord(record),
                    Is.True);

                P12ChallengeRecordData loaded =
                    P12PersistentStore.LoadChallengeRecord();
                Assert.That(loaded.AttemptCount, Is.EqualTo(1));
                Assert.That(
                    loaded.BestSegmentReached,
                    Is.EqualTo(P12ChallengeSegment.FirstSea));
                Assert.That(
                    loaded.BestStageReached,
                    Is.EqualTo(P12StageId.StarlessSea02));
                Assert.That(loaded.MaruFailureCount, Is.EqualTo(1));
                Assert.That(loaded.HasCompletion, Is.False);
            }
            finally
            {
                P12PersistentStore.SetStorageRootForTests(null);
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        private ChallengeHarness CreateChallengeHarness()
        {
            P11Harness p11 = CreateP11AtFinalStage();
            ResolveMemoryEnding(p11);
            GameObject root = Track("P12ChallengeHarness");
            P12ChallengeTelemetry2D telemetry =
                root.AddComponent<P12ChallengeTelemetry2D>();
            P12ChallengeDirector2D director =
                root.AddComponent<P12ChallengeDirector2D>();
            director.Configure(
                CreateP12Catalog(),
                p11.Director,
                p11.Story,
                telemetry);
            return new ChallengeHarness(
                director,
                p11.Director,
                p11.Story);
        }

        private static void ResolveMemoryEnding(P11Harness p11)
        {
            Assert.That(
                p11.Folklore.GrantBossRelic(
                    P9BranchKind.MagpieBridge),
                Is.True);
            Assert.That(
                p11.Folklore.GrantBossRelic(
                    P9BranchKind.DragonPalace),
                Is.True);
            p11.Story.GrantMemoryRouteItemsForTests();
            Assert.That(
                p11.Story.TryRevealDawnStarCoordinates(),
                Is.True);
            Assert.That(p11.Story.TryLightFirstMemoryBell(), Is.True);
            Assert.That(
                p11.Story.MarkNaraeBellPatternCompleted(),
                Is.True);
            Assert.That(p11.Story.TryCompleteMemoryRoute(), Is.True);
            Assert.That(
                p11.Director.TryResolveEnding(P11EndingKind.Memory),
                Is.True);
        }

        private P11Harness CreateP11AtFinalStage()
        {
            GameObject root = Track("P11Harness");
            P9FolkloreChainState2D folklore =
                root.AddComponent<P9FolkloreChainState2D>();
            P11StoryState2D story =
                root.AddComponent<P11StoryState2D>();
            P11CampaignDirector2D director =
                root.AddComponent<P11CampaignDirector2D>();
            folklore.Configure(false, false);
            story.Configure(folklore);
            director.Configure(CreateP11Catalog(), null, story, null);
            director.AcceptCommonRegionForTests();
            P11StageId[] route = P11Route();
            for (int index = 0; index < route.Length - 1; index++)
            {
                Assert.That(
                    director.TryEnterStage(route[index]),
                    Is.True);
                Assert.That(
                    director.TryCompleteCurrentStage(),
                    Is.True);
            }

            Assert.That(
                director.TryEnterStage(route[route.Length - 1]),
                Is.True);
            return new P11Harness(director, story, folklore);
        }

        private P12ChallengeCatalog CreateP12Catalog()
        {
            P12ChallengeCatalog catalog =
                ScriptableObject
                    .CreateInstance<P12ChallengeCatalog>();
            created.Add(catalog);
            catalog.Configure(
                P12ChallengeCatalogDefaults
                    .CreateStandardDefinitions());
            return catalog;
        }

        private P11CampaignCatalog CreateP11Catalog()
        {
            P11CampaignCatalog catalog =
                ScriptableObject
                    .CreateInstance<P11CampaignCatalog>();
            created.Add(catalog);
            P11StageId[] ids = P11Route();
            var definitions = new P11StageDefinition[ids.Length];
            for (int index = 0; index < definitions.Length; index++)
            {
                RoomRegion region = index < 3
                    ? RoomRegion.StarPostOffice
                    : index < 6
                        ? RoomRegion.SunriseGarden
                        : RoomRegion.PolarisObservatory;
                var slot = (P6StageSlot)(index % 3 + 1);
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

        private static P11StageId[] P11Route()
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

        private readonly struct P11Harness
        {
            public P11Harness(
                P11CampaignDirector2D director,
                P11StoryState2D story,
                P9FolkloreChainState2D folklore)
            {
                Director = director;
                Story = story;
                Folklore = folklore;
            }

            public P11CampaignDirector2D Director { get; }
            public P11StoryState2D Story { get; }
            public P9FolkloreChainState2D Folklore { get; }
        }

        private readonly struct ChallengeHarness
        {
            public ChallengeHarness(
                P12ChallengeDirector2D director,
                P11CampaignDirector2D p11Director,
                P11StoryState2D story)
            {
                Director = director;
                P11Director = p11Director;
                Story = story;
            }

            public P12ChallengeDirector2D Director { get; }
            public P11CampaignDirector2D P11Director { get; }
            public P11StoryState2D Story { get; }
        }
    }
}

#endif
